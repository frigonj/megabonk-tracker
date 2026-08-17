from __future__ import annotations

import itertools
import json
import sqlite3
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path

DB_PATH = Path(__file__).parent / "runs.db"

SCHEMA = """
CREATE TABLE IF NOT EXISTS runs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    started_at TEXT NOT NULL,
    ended_at TEXT,
    character TEXT,
    outcome TEXT,
    duration_seconds INTEGER,
    total_damage REAL,
    avg_dps REAL
);

CREATE TABLE IF NOT EXISTS picks (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    run_id INTEGER NOT NULL REFERENCES runs(id),
    seq INTEGER NOT NULL,
    name TEXT NOT NULL,
    level INTEGER,
    rarity TEXT,
    ts TEXT
);

CREATE TABLE IF NOT EXISTS damage_by_source (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    run_id INTEGER NOT NULL REFERENCES runs(id),
    source_name TEXT NOT NULL,
    total_damage REAL,
    level INTEGER
);

CREATE TABLE IF NOT EXISTS damage_history (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    run_id INTEGER NOT NULL REFERENCES runs(id),
    t_seconds REAL NOT NULL,
    total_damage REAL NOT NULL,
    sources_json TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS pick_stat_changes (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    pick_id INTEGER NOT NULL REFERENCES picks(id),
    stat TEXT NOT NULL,
    modify_type TEXT NOT NULL,
    amount REAL NOT NULL
);

CREATE TABLE IF NOT EXISTS weapon_stats_history (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    run_id INTEGER NOT NULL REFERENCES runs(id),
    t_seconds REAL NOT NULL,
    weapon TEXT NOT NULL,
    level INTEGER,
    base_stats_json TEXT NOT NULL,
    current_stats_json TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_picks_run ON picks(run_id);
CREATE INDEX IF NOT EXISTS idx_damage_run ON damage_by_source(run_id);
CREATE INDEX IF NOT EXISTS idx_history_run ON damage_history(run_id);
CREATE INDEX IF NOT EXISTS idx_pick_stat_changes_pick ON pick_stat_changes(pick_id);
CREATE INDEX IF NOT EXISTS idx_weapon_stats_run ON weapon_stats_history(run_id);
"""


def get_conn() -> sqlite3.Connection:
    conn = sqlite3.connect(DB_PATH)
    conn.row_factory = sqlite3.Row
    conn.execute("PRAGMA foreign_keys = ON")
    return conn


def init_db() -> None:
    with get_conn() as conn:
        conn.executescript(SCHEMA)


def now_iso() -> str:
    return datetime.now(timezone.utc).astimezone().isoformat(timespec="seconds")


def start_run(character: str) -> int:
    with get_conn() as conn:
        cur = conn.execute(
            "INSERT INTO runs (started_at, character) VALUES (?, ?)",
            (now_iso(), character),
        )
        return cur.lastrowid


def add_pick(run_id: int, seq: int, name: str, level: int, rarity: str, ts: str, stat_changes: list[dict] | None = None) -> int:
    with get_conn() as conn:
        cur = conn.execute(
            "INSERT INTO picks (run_id, seq, name, level, rarity, ts) VALUES (?, ?, ?, ?, ?, ?)",
            (run_id, seq, name, level, rarity, ts),
        )
        pick_id = cur.lastrowid
        if stat_changes:
            conn.executemany(
                "INSERT INTO pick_stat_changes (pick_id, stat, modify_type, amount) VALUES (?, ?, ?, ?)",
                [(pick_id, c["stat"], c["modifyType"], c["amount"]) for c in stat_changes],
            )
        return pick_id


def add_weapon_stats_snapshot(run_id: int, t_seconds: float, weapons: list[dict]) -> None:
    with get_conn() as conn:
        conn.executemany(
            """INSERT INTO weapon_stats_history (run_id, t_seconds, weapon, level, base_stats_json, current_stats_json)
               VALUES (?, ?, ?, ?, ?, ?)""",
            [
                (run_id, t_seconds, w["weapon"], w.get("level", 0), json.dumps(w["baseStats"]), json.dumps(w["currentStats"]))
                for w in weapons
            ],
        )


def get_weapon_stats_history(run_id: int) -> list[dict]:
    with get_conn() as conn:
        rows = conn.execute(
            """SELECT t_seconds, weapon, level, base_stats_json, current_stats_json
               FROM weapon_stats_history WHERE run_id = ? ORDER BY t_seconds""",
            (run_id,),
        ).fetchall()
        return [
            {
                "t": r["t_seconds"],
                "weapon": r["weapon"],
                "level": r["level"],
                "base_stats": json.loads(r["base_stats_json"]),
                "current_stats": json.loads(r["current_stats_json"]),
            }
            for r in rows
        ]


def get_final_weapon_stats(run_id: int) -> list[dict]:
    """Latest snapshot per weapon this run - base stats vs where the weapon ended up."""
    with get_conn() as conn:
        rows = conn.execute(
            """
            SELECT weapon, level, base_stats_json, current_stats_json, MAX(t_seconds) AS t_seconds
            FROM weapon_stats_history
            WHERE run_id = ?
            GROUP BY weapon
            ORDER BY weapon
            """,
            (run_id,),
        ).fetchall()
        return [
            {
                "weapon": r["weapon"],
                "level": r["level"],
                "base_stats": json.loads(r["base_stats_json"]),
                "current_stats": json.loads(r["current_stats_json"]),
            }
            for r in rows
        ]


def get_picks_with_stat_changes(run_id: int) -> list[dict]:
    with get_conn() as conn:
        picks = conn.execute(
            "SELECT id, seq, name, level, rarity, ts FROM picks WHERE run_id = ? ORDER BY seq",
            (run_id,),
        ).fetchall()
        result = []
        for p in picks:
            changes = conn.execute(
                "SELECT stat, modify_type, amount FROM pick_stat_changes WHERE pick_id = ?",
                (p["id"],),
            ).fetchall()
            result.append({**dict(p), "stat_changes": [dict(c) for c in changes]})
        return result


def add_damage_snapshot(run_id: int, t_seconds: float, total_damage: float, sources: list[dict]) -> None:
    with get_conn() as conn:
        conn.execute(
            "INSERT INTO damage_history (run_id, t_seconds, total_damage, sources_json) VALUES (?, ?, ?, ?)",
            (run_id, t_seconds, total_damage, json.dumps(sources)),
        )


def get_damage_history(run_id: int) -> list[dict]:
    with get_conn() as conn:
        rows = conn.execute(
            "SELECT t_seconds, total_damage, sources_json FROM damage_history WHERE run_id = ? ORDER BY t_seconds",
            (run_id,),
        ).fetchall()
        return [
            {"t": r["t_seconds"], "total_damage": r["total_damage"], "sources": json.loads(r["sources_json"])}
            for r in rows
        ]


def end_run(run_id: int, outcome: str, duration_seconds: int, total_damage: float, sources: list[dict]) -> None:
    avg_dps = total_damage / duration_seconds if duration_seconds > 0 else 0.0
    with get_conn() as conn:
        conn.execute(
            """UPDATE runs SET ended_at = ?, outcome = ?, duration_seconds = ?,
               total_damage = ?, avg_dps = ? WHERE id = ?""",
            (now_iso(), outcome, duration_seconds, total_damage, avg_dps, run_id),
        )
        conn.executemany(
            "INSERT INTO damage_by_source (run_id, source_name, total_damage, level) VALUES (?, ?, ?, ?)",
            [(run_id, s["source"], s["damage"], s.get("level", 0)) for s in sources],
        )


@dataclass
class RunSummary:
    id: int
    started_at: str
    ended_at: str | None
    character: str | None
    outcome: str | None
    duration_seconds: int | None
    total_damage: float | None
    avg_dps: float | None


def get_run(run_id: int) -> RunSummary | None:
    with get_conn() as conn:
        row = conn.execute("SELECT * FROM runs WHERE id = ?", (run_id,)).fetchone()
        return RunSummary(**dict(row)) if row else None


def list_runs(limit: int = 50) -> list[RunSummary]:
    with get_conn() as conn:
        rows = conn.execute(
            "SELECT * FROM runs WHERE ended_at IS NOT NULL ORDER BY id DESC LIMIT ?",
            (limit,),
        ).fetchall()
        return [RunSummary(**dict(r)) for r in rows]


def per_item_stats() -> list[dict]:
    """Avg damage contribution and pick rate per item name, across all completed runs it appeared in."""
    with get_conn() as conn:
        total_runs = conn.execute("SELECT COUNT(*) c FROM runs WHERE ended_at IS NOT NULL").fetchone()["c"]
        if total_runs == 0:
            return []
        rows = conn.execute(
            """
            SELECT p.name AS name,
                   COUNT(DISTINCT p.run_id) AS runs_appeared,
                   AVG(r.avg_dps) AS avg_dps_when_present,
                   AVG(d.total_damage) AS avg_damage_contribution
            FROM picks p
            JOIN runs r ON r.id = p.run_id AND r.ended_at IS NOT NULL
            LEFT JOIN damage_by_source d ON d.run_id = p.run_id AND d.source_name = p.name
            GROUP BY p.name
            ORDER BY avg_dps_when_present DESC
            """
        ).fetchall()
        return [
            {
                **dict(r),
                "pick_rate": r["runs_appeared"] / total_runs,
            }
            for r in rows
        ]


def combo_stats(combo_size: int = 2, min_sample: int = 2) -> list[dict]:
    """For every pair/triple of items that co-occur across >= min_sample runs, compare avg DPS
    of runs containing that combo vs the overall baseline average DPS."""
    with get_conn() as conn:
        baseline_row = conn.execute(
            "SELECT AVG(avg_dps) b FROM runs WHERE ended_at IS NOT NULL"
        ).fetchone()
        baseline = baseline_row["b"] or 0.0

        run_rows = conn.execute(
            "SELECT id, avg_dps FROM runs WHERE ended_at IS NOT NULL"
        ).fetchall()
        run_dps = {r["id"]: r["avg_dps"] or 0.0 for r in run_rows}

        pick_rows = conn.execute(
            "SELECT DISTINCT run_id, name FROM picks WHERE run_id IN ({})".format(
                ",".join("?" * len(run_dps))
            ),
            list(run_dps.keys()),
        ).fetchall() if run_dps else []

        picks_by_run: dict[int, set[str]] = {}
        for row in pick_rows:
            picks_by_run.setdefault(row["run_id"], set()).add(row["name"])

        combo_runs: dict[tuple[str, ...], list[int]] = {}
        for run_id, names in picks_by_run.items():
            for combo in itertools.combinations(sorted(names), combo_size):
                combo_runs.setdefault(combo, []).append(run_id)

        results = []
        for combo, ids in combo_runs.items():
            if len(ids) < min_sample:
                continue
            avg = sum(run_dps[i] for i in ids) / len(ids)
            results.append(
                {
                    "combo": combo,
                    "sample_size": len(ids),
                    "avg_dps": avg,
                    "baseline_dps": baseline,
                    "delta_pct": ((avg - baseline) / baseline * 100) if baseline else 0.0,
                }
            )
        results.sort(key=lambda r: r["avg_dps"], reverse=True)
        return results
