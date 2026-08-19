from __future__ import annotations

import asyncio
import json
import os
from datetime import datetime
from pathlib import Path

from fastapi import WebSocket

import db

LIVE_EVENTS_PATH = Path(os.environ["APPDATA"]) / "MegabonkTracker" / "live_events.ndjson"
POLL_INTERVAL_SECONDS = 0.5


class LiveState:
    def __init__(self) -> None:
        self.run_id: int | None = None
        self.run_started_at: datetime | None = None
        self.character: str = ""
        self.picks: list[dict] = []
        self.total_damage: float = 0.0
        self.sources: list[dict] = []
        self.damage_history: list[dict] = []
        self.weapon_stats: list[dict] = []
        self.player_stats: dict = {"base_stats": [], "current_stats": []}
        self.effects: list[dict] = []
        self.run_counters: dict = {}
        self.performance_history: list[dict] = []
        self.latest_performance: dict = {}
        self.latest_dps: float = 0.0
        self._last_damage_t: float | None = None
        self.enemy_health: dict = {}
        self.enemy_health_history: list[dict] = []
        self.progression_limits: dict = {}
        self.items_granted: list[dict] = []
        self.is_paused: bool = False
        self.paused_at: datetime | None = None
        self.total_paused_seconds: float = 0.0
        self.clients: set[WebSocket] = set()

    def elapsed_seconds(self, event_ts: datetime) -> float:
        """Wall-clock time since run start, minus time spent paused - keeps the chart timeline
        and duration/DPS figures reflecting actual play time, not real-world pause length."""
        if self.run_started_at is None:
            return 0.0
        return (event_ts - self.run_started_at).total_seconds() - self.total_paused_seconds

    def to_dict(self) -> dict:
        return {
            "run_active": self.run_id is not None,
            "character": self.character,
            "picks": self.picks,
            "total_damage": self.total_damage,
            "sources": sorted(self.sources, key=lambda s: s["damage"], reverse=True),
            "damage_history": self.damage_history,
            "weapon_stats": self.weapon_stats,
            "player_stats": self.player_stats,
            "effects": self.effects,
            "run_counters": self.run_counters,
            "performance_history": self.performance_history,
            "latest_performance": self.latest_performance,
            "latest_dps": self.latest_dps,
            "enemy_health": self.enemy_health,
            "enemy_health_history": self.enemy_health_history,
            "progression_limits": self.progression_limits,
            "items_granted": self.items_granted,
            "is_paused": self.is_paused,
        }


state = LiveState()


async def broadcast() -> None:
    if not state.clients:
        return
    payload = json.dumps(state.to_dict())
    dead = set()
    for ws in state.clients:
        try:
            await ws.send_text(payload)
        except Exception:
            dead.add(ws)
    state.clients -= dead


def _handle_event(evt: dict) -> None:
    etype = evt.get("type")

    if etype == "run_started":
        state.run_id = db.start_run(evt.get("character", ""))
        state.run_started_at = datetime.fromisoformat(evt["ts"])
        state.character = evt.get("character", "")
        state.picks = []
        state.total_damage = 0.0
        state.sources = []
        state.damage_history = []
        state.weapon_stats = []
        state.player_stats = {"base_stats": [], "current_stats": []}
        state.effects = []
        state.run_counters = {}
        state.performance_history = []
        state.latest_performance = {}
        state.latest_dps = 0.0
        state._last_damage_t = None
        state.enemy_health = {}
        state.enemy_health_history = []
        state.progression_limits = {}
        state.items_granted = []
        state.is_paused = False
        state.paused_at = None
        state.total_paused_seconds = 0.0

    elif etype == "game_paused":
        if not state.is_paused:
            state.is_paused = True
            state.paused_at = datetime.fromisoformat(evt["ts"])

    elif etype == "game_resumed":
        if state.is_paused and state.paused_at is not None:
            state.total_paused_seconds += (datetime.fromisoformat(evt["ts"]) - state.paused_at).total_seconds()
        state.is_paused = False
        state.paused_at = None

    elif etype == "upgrade_picked":
        if state.run_id is None:
            return
        seq = len(state.picks)
        stat_changes = evt.get("statChanges", [])
        pick = {
            "name": evt["name"], "level": evt["level"], "max_level": evt.get("maxLevel"), "rarity": evt["rarity"],
            "ts": evt["ts"], "stat_changes": stat_changes,
        }
        state.picks.append(pick)
        db.add_pick(state.run_id, seq, pick["name"], pick["level"], pick["max_level"], pick["rarity"], pick["ts"], stat_changes)

    elif etype == "weapon_stats_snapshot":
        state.weapon_stats = evt.get("weapons", [])
        if state.run_id is not None and state.run_started_at is not None:
            t = state.elapsed_seconds(datetime.fromisoformat(evt["ts"]))
            db.add_weapon_stats_snapshot(state.run_id, t, state.weapon_stats)

    elif etype == "effect_applied":
        state.effects.append(evt)
        if state.run_id is not None and state.run_started_at is not None:
            t = state.elapsed_seconds(datetime.fromisoformat(evt["ts"]))
            db.add_effect_applied(state.run_id, t, evt)

    elif etype == "player_stats_snapshot":
        max_xp_multiplier = evt.get("maxXpMultiplier")
        state.player_stats = {
            "base_stats": evt.get("baseStats", []), "current_stats": evt.get("currentStats", []),
            "max_xp_multiplier": max_xp_multiplier,
        }
        if state.run_id is not None and state.run_started_at is not None:
            t = state.elapsed_seconds(datetime.fromisoformat(evt["ts"]))
            db.add_player_stats_snapshot(
                state.run_id, t, evt.get("baseStats", []), evt.get("currentStats", []), max_xp_multiplier
            )

    elif etype == "run_counters_snapshot":
        state.run_counters = {
            "gold": evt.get("gold"), "character_level": evt.get("characterLevel"),
            "banishes_used": evt.get("banishesUsed"), "refreshes_used": evt.get("refreshesUsed"),
            "skips_used": evt.get("skipsUsed"),
        }
        if state.run_id is not None and state.run_started_at is not None:
            t = state.elapsed_seconds(datetime.fromisoformat(evt["ts"]))
            db.add_run_counters_snapshot(state.run_id, t, evt)

    elif etype == "damage_snapshot":
        new_total = evt.get("totalDamage", 0.0)
        if state.run_id is not None and state.run_started_at is not None:
            t = state.elapsed_seconds(datetime.fromisoformat(evt["ts"]))
            # Instantaneous DPS (delta since the last tick), not cumulative avg_dps - a build
            # that's falling behind needs to show up quickly, not get smoothed out over the run.
            dt = t - state._last_damage_t if state._last_damage_t is not None else 0.0
            state.latest_dps = (new_total - state.total_damage) / dt if dt > 0 else 0.0
            state._last_damage_t = t

            point = {"t": t, "total_damage": new_total, "sources": evt.get("sources", [])}
            state.damage_history.append(point)
            db.add_damage_snapshot(state.run_id, t, new_total, evt.get("sources", []))
        state.total_damage = new_total
        state.sources = evt.get("sources", [])

    elif etype == "enemy_health_snapshot":
        total_hp = evt.get("totalHp", 0.0)
        avg_hp = evt.get("avgHp", 0.0)
        enemy_count = evt.get("enemyCount", 0)
        state.enemy_health = {
            "total_hp": total_hp, "avg_hp": avg_hp, "enemy_count": enemy_count,
            "dps_to_hp_ratio": (state.latest_dps / total_hp) if total_hp > 0 else None,
        }
        if state.run_id is not None and state.run_started_at is not None:
            t = state.elapsed_seconds(datetime.fromisoformat(evt["ts"]))
            state.enemy_health_history.append({"t": t, **state.enemy_health})
            db.add_enemy_health_snapshot(state.run_id, t, total_hp, avg_hp, enemy_count, state.latest_dps)

    elif etype == "progression_limits_snapshot":
        was_final_swarm = state.progression_limits.get("is_final_swarm", False)
        state.progression_limits = {
            "max_weapon_level_base": evt.get("maxWeaponLevelBase"),
            "max_tome_level_base": evt.get("maxTomeLevelBase"),
            "weapon_max_level": evt.get("weaponMaxLevel"),
            "tome_max_level": evt.get("tomeMaxLevel"),
            "num_extra_weapon_levels": evt.get("numExtraWeaponLevels"),
            "num_extra_tome_levels": evt.get("numExtraTomeLevels"),
            "num_available_weapon_slots": evt.get("numAvailableWeaponSlots"),
            "num_max_weapon_slots": evt.get("numMaxWeaponSlots"),
            "num_available_tome_slots": evt.get("numAvailableTomeSlots"),
            "num_max_tome_slots": evt.get("numMaxTomeSlots"),
            "can_unlock_weapons": evt.get("canUnlockWeapons"),
            "can_unlock_tomes": evt.get("canUnlockTomes"),
            "weapons_maxed": evt.get("weaponsMaxed"),
            "tomes_maxed": evt.get("tomesMaxed"),
            "num_max_enemies": evt.get("numMaxEnemies"),
            "has_max_enemies": evt.get("hasMaxEnemies"),
            "is_final_swarm": evt.get("isFinalSwarm", False),
        }
        # Only persist the moment Final Swarm actually starts - everything else in this event is
        # cheap to keep live-only, re-derivable from the next tick, and would bloat the DB with
        # near-duplicate rows every second for values that rarely change.
        if not was_final_swarm and state.progression_limits["is_final_swarm"]:
            if state.run_id is not None and state.run_started_at is not None:
                t = state.elapsed_seconds(datetime.fromisoformat(evt["ts"]))
                db.add_final_swarm_started(state.run_id, t)

    elif etype == "item_granted":
        item = {"source": evt.get("source", ""), "item": evt.get("item", ""), "rarity": evt.get("rarity", "")}
        state.items_granted.append(item)
        if state.run_id is not None and state.run_started_at is not None:
            t = state.elapsed_seconds(datetime.fromisoformat(evt["ts"]))
            db.add_item_granted(state.run_id, t, item["source"], item["item"], item["rarity"])

    elif etype == "performance_snapshot":
        point = {
            "avg_fps": evt.get("avgFps", 0.0), "min_fps": evt.get("minFps", 0.0),
            "frame_count": evt.get("frameCount", 0), "spike_count": evt.get("spikeCount", 0),
        }
        state.latest_performance = point
        if state.run_id is not None and state.run_started_at is not None:
            t = state.elapsed_seconds(datetime.fromisoformat(evt["ts"]))
            point_with_t = {"t": t, **point}
            state.performance_history.append(point_with_t)
            db.add_performance_snapshot(state.run_id, t, point["avg_fps"], point["min_fps"], point["frame_count"], point["spike_count"])

    elif etype == "run_ended":
        if state.run_id is None:
            return
        duration = 0
        if state.run_started_at is not None:
            duration = int(state.elapsed_seconds(datetime.fromisoformat(evt["ts"])))
        db.end_run(state.run_id, evt.get("outcome", ""), duration, state.total_damage, state.sources)
        state.run_id = None
        state.run_started_at = None
        state.is_paused = False
        state.paused_at = None
        state.total_paused_seconds = 0.0


async def tail_events_loop() -> None:
    LIVE_EVENTS_PATH.parent.mkdir(parents=True, exist_ok=True)
    if not LIVE_EVENTS_PATH.exists():
        LIVE_EVENTS_PATH.touch()

    # Start at the current end of the file, not byte 0 - the plugin's NDJSON file persists across
    # dashboard restarts (it's only truncated on a fresh in-game run), so replaying from scratch on
    # every dashboard restart would re-ingest whatever run was already durably saved to the DB last
    # time, creating a fresh duplicate `runs` row each restart. Only genuinely new lines written
    # after this process starts should ever be processed.
    last_size = LIVE_EVENTS_PATH.stat().st_size
    while True:
        try:
            size = LIVE_EVENTS_PATH.stat().st_size
            if size < last_size:
                # File was truncated (new run started by the plugin) - re-read from scratch.
                last_size = 0
            if size > last_size:
                with LIVE_EVENTS_PATH.open("r", encoding="utf-8") as f:
                    f.seek(last_size)
                    new_lines = f.readlines()
                last_size = size
                changed = False
                for line in new_lines:
                    line = line.strip()
                    if not line:
                        continue
                    try:
                        evt = json.loads(line)
                    except json.JSONDecodeError:
                        continue
                    _handle_event(evt)
                    changed = True
                if changed:
                    await broadcast()
        except FileNotFoundError:
            last_size = 0
        await asyncio.sleep(POLL_INTERVAL_SECONDS)
