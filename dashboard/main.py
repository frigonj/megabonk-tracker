from __future__ import annotations

import asyncio
import json
import os
from contextlib import asynccontextmanager

from fastapi import FastAPI, Request, WebSocket, WebSocketDisconnect
from fastapi.responses import HTMLResponse
from fastapi.staticfiles import StaticFiles
from fastapi.templating import Jinja2Templates

import db
import tracker

HOST = os.environ.get("MEGABONK_TRACKER_HOST", "127.0.0.1")
PORT = int(os.environ.get("MEGABONK_TRACKER_PORT", "8420"))


@asynccontextmanager
async def lifespan(app: FastAPI):
    db.init_db()
    task = asyncio.create_task(tracker.tail_events_loop())
    yield
    task.cancel()


app = FastAPI(title="Megabonk Tracker", lifespan=lifespan)
app.mount("/static", StaticFiles(directory="static"), name="static")
templates = Jinja2Templates(directory="templates")


@app.get("/", response_class=HTMLResponse)
async def index(request: Request):
    return templates.TemplateResponse(request, "index.html", {})


@app.websocket("/ws")
async def ws_endpoint(websocket: WebSocket):
    await websocket.accept()
    tracker.state.clients.add(websocket)
    await websocket.send_text(json.dumps(tracker.state.to_dict()))
    try:
        while True:
            await websocket.receive_text()
    except WebSocketDisconnect:
        tracker.state.clients.discard(websocket)


@app.get("/history", response_class=HTMLResponse)
async def history(request: Request, sort: str = "id", dir: str = "desc"):
    runs = db.list_runs(sort_by=sort, sort_dir=dir)
    per_item = db.per_item_stats()
    pairs = db.combo_stats(combo_size=2)
    triples = db.combo_stats(combo_size=3)
    return templates.TemplateResponse(
        request,
        "history.html",
        {"runs": runs, "per_item": per_item, "pairs": pairs, "triples": triples, "sort": sort, "dir": dir},
    )


@app.get("/history/{run_id}", response_class=HTMLResponse)
async def run_detail(request: Request, run_id: int):
    run = db.get_run(run_id)
    history_points = db.get_damage_history(run_id)
    picks = db.get_picks_with_stat_changes(run_id)
    final_weapon_stats = db.get_final_weapon_stats(run_id)
    effects = db.get_effects_applied(run_id)
    final_player_stats = db.get_final_player_stats(run_id)
    final_run_counters = db.get_final_run_counters(run_id)
    performance_history = db.get_performance_history(run_id)
    enemy_health_history = db.get_enemy_health_history(run_id)
    ratios = [p["dps"] / p["total_hp"] for p in enemy_health_history if p["total_hp"] > 0]
    avg_dps_to_hp_ratio = sum(ratios) / len(ratios) if ratios else None
    final_swarm_started_at = db.get_final_swarm_started(run_id)
    items_granted = db.get_items_granted(run_id)
    return templates.TemplateResponse(
        request,
        "run_detail.html",
        {
            "run": run,
            "history_points": history_points,
            "picks": picks,
            "final_weapon_stats": final_weapon_stats,
            "effects": effects,
            "final_player_stats": final_player_stats,
            "final_run_counters": final_run_counters,
            "performance_history": performance_history,
            "enemy_health_history": enemy_health_history,
            "avg_dps_to_hp_ratio": avg_dps_to_hp_ratio,
            "final_swarm_started_at": final_swarm_started_at,
            "items_granted": items_granted,
        },
    )


@app.get("/api/history/{run_id}")
async def run_detail_json(run_id: int):
    return db.get_damage_history(run_id)


if __name__ == "__main__":
    import uvicorn

    uvicorn.run(app, host=HOST, port=PORT)
