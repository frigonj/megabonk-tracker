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
        self.clients: set[WebSocket] = set()

    def to_dict(self) -> dict:
        return {
            "run_active": self.run_id is not None,
            "character": self.character,
            "picks": self.picks,
            "total_damage": self.total_damage,
            "sources": sorted(self.sources, key=lambda s: s["damage"], reverse=True),
            "damage_history": self.damage_history,
            "weapon_stats": self.weapon_stats,
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

    elif etype == "upgrade_picked":
        if state.run_id is None:
            return
        seq = len(state.picks)
        stat_changes = evt.get("statChanges", [])
        pick = {
            "name": evt["name"], "level": evt["level"], "rarity": evt["rarity"],
            "ts": evt["ts"], "stat_changes": stat_changes,
        }
        state.picks.append(pick)
        db.add_pick(state.run_id, seq, pick["name"], pick["level"], pick["rarity"], pick["ts"], stat_changes)

    elif etype == "weapon_stats_snapshot":
        state.weapon_stats = evt.get("weapons", [])
        if state.run_id is not None and state.run_started_at is not None:
            t = (datetime.fromisoformat(evt["ts"]) - state.run_started_at).total_seconds()
            db.add_weapon_stats_snapshot(state.run_id, t, state.weapon_stats)

    elif etype == "damage_snapshot":
        state.total_damage = evt.get("totalDamage", 0.0)
        state.sources = evt.get("sources", [])
        if state.run_id is not None and state.run_started_at is not None:
            t = (datetime.fromisoformat(evt["ts"]) - state.run_started_at).total_seconds()
            point = {"t": t, "total_damage": state.total_damage, "sources": state.sources}
            state.damage_history.append(point)
            db.add_damage_snapshot(state.run_id, t, state.total_damage, state.sources)

    elif etype == "run_ended":
        if state.run_id is None:
            return
        duration = 0
        if state.run_started_at is not None:
            ended_at = datetime.fromisoformat(evt["ts"])
            duration = int((ended_at - state.run_started_at).total_seconds())
        db.end_run(state.run_id, evt.get("outcome", ""), duration, state.total_damage, state.sources)
        state.run_id = None
        state.run_started_at = None


async def tail_events_loop() -> None:
    LIVE_EVENTS_PATH.parent.mkdir(parents=True, exist_ok=True)
    if not LIVE_EVENTS_PATH.exists():
        LIVE_EVENTS_PATH.touch()

    last_size = 0
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
