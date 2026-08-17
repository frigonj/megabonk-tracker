// Minimal SVG line chart for damage-over-time: total damage + top per-source lines.
// Palette: validated categorical set from the dataviz skill (dark surface #1a1d24).
const SERIES_COLORS = ["#3987e5", "#d95926", "#199e70", "#c98500"];
const TOTAL_COLOR = "#e8e9ec"; // primary ink - the total line is not a "series", it's the headline
const MAX_DIRECT_SERIES = 4;

function niceMax(v) {
  if (v <= 0) return 1;
  const pow = Math.pow(10, Math.floor(Math.log10(v)));
  const n = v / pow;
  const step = n <= 1 ? 1 : n <= 2 ? 2 : n <= 5 ? 5 : 10;
  return step * pow;
}

function topSourceNames(points, limit) {
    const maxBySource = new Map();
    for (const p of points) {
        for (const s of p.sources) {
            maxBySource.set(s.source, Math.max(maxBySource.get(s.source) || 0, s.damage));
        }
    }
    return [...maxBySource.entries()]
        .sort((a, b) => b[1] - a[1])
        .slice(0, limit)
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

    const tMax = points[points.length - 1].t || 1;
    const totalMax = niceMax(Math.max(...points.map((p) => p.total_damage)));

    const seriesNames = topSourceNames(points, MAX_DIRECT_SERIES);
    const otherNames = new Set();
    for (const p of points) for (const s of p.sources) if (!seriesNames.includes(s.source)) otherNames.add(s.source);

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

    // Per-source lines (top N direct-labeled series)
    seriesNames.forEach((name, i) => {
        const path = document.createElementNS(svgNS, "path");
        path.setAttribute("d", lineFor((p) => p.sources.find((s) => s.source === name)?.damage || 0));
        path.setAttribute("fill", "none");
        path.setAttribute("stroke", SERIES_COLORS[i]);
        path.setAttribute("stroke-width", "2");
        path.setAttribute("stroke-linejoin", "round");
        path.setAttribute("stroke-linecap", "round");
        svg.appendChild(path);
    });

    // "Other" line, if any sources fell outside the top N
    if (otherNames.size > 0) {
        const path = document.createElementNS(svgNS, "path");
        path.setAttribute(
            "d",
            lineFor((p) => p.sources.filter((s) => otherNames.has(s.source)).reduce((sum, s) => sum + s.damage, 0))
        );
        path.setAttribute("fill", "none");
        path.setAttribute("stroke", "#9aa0ab");
        path.setAttribute("stroke-width", "2");
        path.setAttribute("stroke-dasharray", "1,0");
        path.setAttribute("opacity", "0.6");
        svg.appendChild(path);
    }

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
            if (s) lines.push(`${name}: ${Math.round(s.damage).toLocaleString()}`);
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
    seriesNames.forEach((name, i) => addLegendItem(SERIES_COLORS[i], name));
    if (otherNames.size > 0) addLegendItem("#9aa0ab", "Other");
    container.appendChild(legend);
}
