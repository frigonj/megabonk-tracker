// Minimal SVG line chart for damage-over-time: total damage + one line per damage source.
// Palette: validated 8-hue categorical set from the dataviz skill (dark surface #1a1a19) -
// validated via scripts/validate_palette.js, all adjacent pairs pass CVD/normal-vision/contrast
// gates. Every source gets its own line (no "Other" fold) per explicit user request; past 8
// sources in a single run, colors repeat with an alternating dash pattern so no two lines are
// visually identical even without using the hover tooltip to disambiguate.
const SERIES_COLORS = ["#3987e5", "#d95926", "#199e70", "#c98500", "#d55181", "#008300", "#9085e9", "#e66767"];
const TOTAL_COLOR = "#e8e9ec"; // primary ink - the total line is not a "series", it's the headline

// The game's own damage-attribution fallback key (confirmed via decompiled Assembly-CSharp.dll:
// Assets.Scripts.Actors.DamageContainer.unknownDamageSource, typo included) is relabeled for
// display only - matching/lookup logic elsewhere in this file still keys on the raw "Unkown"
// string from the event stream, so this must only be applied at render time.
export function displaySourceName(source) {
    return source === "Unkown" ? "Unattributed" : source;
}

function niceMax(v) {
  if (v <= 0) return 1;
  const pow = Math.pow(10, Math.floor(Math.log10(v)));
  const n = v / pow;
  const step = n <= 1 ? 1 : n <= 2 ? 2 : n <= 5 ? 5 : 10;
  return step * pow;
}

// The axis max tracks the latest point directly (plus a small margin) rather than snapping to
// coarse fixed steps - fixed steps kept the line from visually compressing every second, but also
// left the plotted line ending well short of the right edge for most of a run's duration (e.g. a
// 650s run would sit at ~54% width until crossing the 1200s step). A tiny fixed margin still means
// existing points barely move between redraws (no visible per-second scroll/compression), while
// the line consistently reaches close to the plot's right edge.
const TIME_AXIS_MARGIN_SECONDS = 5;

function niceTimeMax(t) {
  return Math.max(t + TIME_AXIS_MARGIN_SECONDS, 10);
}

// Every distinct source across the run, ordered by peak damage (highest-impact first) so the
// legend and color assignment are stable and meaningful, not insertion-order-dependent.
function allSourceNames(points) {
    const maxBySource = new Map();
    for (const p of points) {
        for (const s of p.sources) {
            maxBySource.set(s.source, Math.max(maxBySource.get(s.source) || 0, s.damage));
        }
    }
    return [...maxBySource.entries()]
        .sort((a, b) => b[1] - a[1])
        .map(([name]) => name);
}

export function renderDamageChart(container, points, opts = {}) {
    container.innerHTML = "";
    if (!points || points.length < 2) {
        const empty = document.createElement("div");
        empty.className = "empty-state";
        empty.textContent = "Not enough data yet to plot a curve.";
        container.appendChild(empty);
        return;
    }

    const width = opts.width || container.clientWidth || 600;
    const height = opts.height || 280;
    const padding = { top: 16, right: 16, bottom: 28, left: 56 };
    const plotW = width - padding.left - padding.right;
    const plotH = height - padding.top - padding.bottom;

    const tMax = niceTimeMax(points[points.length - 1].t || 0);
    const totalMax = niceMax(Math.max(...points.map((p) => p.total_damage)));

    const seriesNames = allSourceNames(points);

    const x = (t) => padding.left + (t / tMax) * plotW;
    const y = (v) => padding.top + plotH - (v / totalMax) * plotH;

    function lineFor(valueFn) {
        return points.map((p, i) => `${i === 0 ? "M" : "L"}${x(p.t).toFixed(1)},${y(valueFn(p)).toFixed(1)}`).join(" ");
    }

    const svgNS = "http://www.w3.org/2000/svg";
    const svg = document.createElementNS(svgNS, "svg");
    svg.setAttribute("viewBox", `0 0 ${width} ${height}`);
    svg.setAttribute("width", "100%");
    svg.setAttribute("height", height);
    svg.classList.add("damage-chart");

    // Gridlines (hairline, recessive) + y-axis ticks
    const gridSteps = 4;
    for (let i = 0; i <= gridSteps; i++) {
        const v = (totalMax / gridSteps) * i;
        const gy = y(v);
        const line = document.createElementNS(svgNS, "line");
        line.setAttribute("x1", padding.left);
        line.setAttribute("x2", width - padding.right);
        line.setAttribute("y1", gy);
        line.setAttribute("y2", gy);
        line.setAttribute("stroke", "#2b2f3a");
        line.setAttribute("stroke-width", "1");
        svg.appendChild(line);

        const label = document.createElementNS(svgNS, "text");
        label.setAttribute("x", padding.left - 8);
        label.setAttribute("y", gy + 4);
        label.setAttribute("text-anchor", "end");
        label.setAttribute("fill", "#898781");
        label.setAttribute("font-size", "11");
        label.textContent = Math.round(v).toLocaleString();
        svg.appendChild(label);
    }

    // X-axis (time) ticks - 5 even divisions of the current tMax.
    const timeTickCount = 5;
    for (let i = 0; i <= timeTickCount; i++) {
        const t = (tMax / timeTickCount) * i;
        const tx = x(t);
        const label = document.createElementNS(svgNS, "text");
        label.setAttribute("x", tx);
        label.setAttribute("y", height - padding.bottom + 18);
        label.setAttribute("text-anchor", i === timeTickCount ? "end" : i === 0 ? "start" : "middle");
        label.setAttribute("fill", "#898781");
        label.setAttribute("font-size", "11");
        label.textContent = `${Math.round(t)}s`;
        svg.appendChild(label);
    }

    // Per-source lines - every source gets its own line, no aggregation. Colors cycle through
    // the validated 8-hue set; a second pass through the palette (9th+ source) switches to a
    // dashed stroke so no two lines are ever visually identical, even beyond the validated set.
    seriesNames.forEach((name, i) => {
        const colorIndex = i % SERIES_COLORS.length;
        const cyclePass = Math.floor(i / SERIES_COLORS.length);
        const path = document.createElementNS(svgNS, "path");
        path.setAttribute("d", lineFor((p) => p.sources.find((s) => s.source === name)?.damage || 0));
        path.setAttribute("fill", "none");
        path.setAttribute("stroke", SERIES_COLORS[colorIndex]);
        path.setAttribute("stroke-width", "2");
        path.setAttribute("stroke-linejoin", "round");
        path.setAttribute("stroke-linecap", "round");
        if (cyclePass > 0) path.setAttribute("stroke-dasharray", "6,3");
        svg.appendChild(path);
    });

    // Total line (headline series - drawn last so it stays on top)
    const totalPath = document.createElementNS(svgNS, "path");
    totalPath.setAttribute("d", lineFor((p) => p.total_damage));
    totalPath.setAttribute("fill", "none");
    totalPath.setAttribute("stroke", TOTAL_COLOR);
    totalPath.setAttribute("stroke-width", "2");
    totalPath.setAttribute("stroke-linejoin", "round");
    totalPath.setAttribute("stroke-linecap", "round");
    svg.appendChild(totalPath);

    // Hover crosshair + tooltip
    const crosshair = document.createElementNS(svgNS, "line");
    crosshair.setAttribute("y1", padding.top);
    crosshair.setAttribute("y2", padding.top + plotH);
    crosshair.setAttribute("stroke", "#9aa0ab");
    crosshair.setAttribute("stroke-width", "1");
    crosshair.setAttribute("visibility", "hidden");
    svg.appendChild(crosshair);

    const hitArea = document.createElementNS(svgNS, "rect");
    hitArea.setAttribute("x", padding.left);
    hitArea.setAttribute("y", padding.top);
    hitArea.setAttribute("width", plotW);
    hitArea.setAttribute("height", plotH);
    hitArea.setAttribute("fill", "transparent");
    svg.appendChild(hitArea);

    const tooltip = document.createElement("div");
    tooltip.className = "chart-tooltip";
    tooltip.style.display = "none";
    container.style.position = "relative";

    hitArea.addEventListener("mousemove", (ev) => {
        const rect = svg.getBoundingClientRect();
        const scaleX = width / rect.width;
        const mx = (ev.clientX - rect.left) * scaleX;
        const t = ((mx - padding.left) / plotW) * tMax;
        let nearest = points[0];
        for (const p of points) if (Math.abs(p.t - t) < Math.abs(nearest.t - t)) nearest = p;

        crosshair.setAttribute("x1", x(nearest.t));
        crosshair.setAttribute("x2", x(nearest.t));
        crosshair.setAttribute("visibility", "visible");

        const lines = [`<strong>${Math.round(nearest.t)}s</strong>`, `Total: ${Math.round(nearest.total_damage).toLocaleString()}`];
        for (const name of seriesNames) {
            const s = nearest.sources.find((s) => s.source === name);
            if (s) lines.push(`${displaySourceName(name)}: ${Math.round(s.damage).toLocaleString()}`);
        }
        tooltip.innerHTML = lines.join("<br>");
        tooltip.style.display = "block";
        tooltip.style.left = `${(ev.clientX - rect.left) + 12}px`;
        tooltip.style.top = `${(ev.clientY - rect.top) + 12}px`;
    });
    hitArea.addEventListener("mouseleave", () => {
        crosshair.setAttribute("visibility", "hidden");
        tooltip.style.display = "none";
    });

    container.appendChild(svg);
    container.appendChild(tooltip);

    // Legend
    const legend = document.createElement("div");
    legend.className = "chart-legend";
    const addLegendItem = (color, label) => {
        const item = document.createElement("span");
        item.className = "legend-item";
        item.innerHTML = `<span class="legend-swatch" style="background:${color}"></span>${label}`;
        legend.appendChild(item);
    };
    addLegendItem(TOTAL_COLOR, "Total");
    seriesNames.forEach((name, i) => addLegendItem(SERIES_COLORS[i % SERIES_COLORS.length], displaySourceName(name)));
    container.appendChild(legend);
}
