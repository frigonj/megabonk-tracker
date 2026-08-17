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
async def history(request: Request):
    runs = db.list_runs()
    per_item = db.per_item_stats()
    pairs = db.combo_stats(combo_size=2)
    triples = db.combo_stats(combo_size=3)
    return templates.TemplateResponse(
        request,
        "history.html",
        {"runs": runs, "per_item": per_item, "pairs": pairs, "triples": triples},
    )


@app.get("/history/{run_id}", response_class=HTMLResponse)
async def run_detail(request: Request, run_id: int):
    run = db.get_run(run_id)
    history_points = db.get_damage_history(run_id)
    picks = db.get_picks_with_stat_changes(run_id)
    final_weapon_stats = db.get_final_weapon_stats(run_id)
    return templates.TemplateResponse(
        request,
        "run_detail.html",
        {
            "run": run,
            "history_points": history_points,
            "picks": picks,
            "final_weapon_stats": final_weapon_stats,
        },
    )


@app.get("/api/history/{run_id}")
async def run_detail_json(run_id: int):
    return db.get_damage_history(run_id)


if __name__ == "__main__":
    import uvicorn

    uvicorn.run(app, host=HOST, port=PORT)
