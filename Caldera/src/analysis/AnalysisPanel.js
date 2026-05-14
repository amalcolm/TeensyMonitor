import Plotly from "plotly.js-dist-min";
import { AnalysisDataset } from "./AnalysisDataset.js";

const EMPTY_AXIS_RANGE = [0, 3.3];

export class AnalysisPanel {
  constructor({
    dataset = new AnalysisDataset(),
    root,
  } = {}) {
    this.dataset = dataset;
    this.root = root;
    this.badge = root?.querySelector("[data-analysis-badge]");
    this.chartRoot = root?.querySelector("[data-analysis-chart]");
    this.copyButton = root?.querySelector("[data-analysis-copy-csv]");
    this.gainBreakdown = root?.querySelector("[data-analysis-gain-breakdown]");
    this.rmsMetric = root?.querySelector("[data-analysis-rms]");
    this.samplesMetric = root?.querySelector("[data-analysis-samples]");
    this.slopeMetric = root?.querySelector("[data-analysis-slope]");
    this.slopeRatioMetric = root?.querySelector("[data-analysis-slope-ratio]");
    this.resizeObserver = null;

    this.copyButton?.addEventListener("click", () => this.copyCsv());
    this.render();
  }

  addSampleFromModel({ circuitScene, model, sensorVoltages, source }) {
    const sample = this.dataset.addSampleFromModel({
      circuitScene,
      model,
      sensorVoltages,
      source,
    });

    this.render();

    return sample;
  }

  clear() {
    this.dataset.clear();
    this.render();
  }

  async copyCsv() {
    const csv = this.dataset.toCsv();

    try {
      await copyText(csv);
      this.setBadge(this.dataset.samples.length ? "CSV copied" : "CSV header copied");
    } catch {
      this.setBadge("copy failed");
    }
  }

  render() {
    const samples = this.dataset.getSensorComparisonSamples();
    const plottableSamples = samples.filter((sample) => sample.isPlottable);
    const predictedSamples = plottableSamples.filter((sample) => (
      Number.isFinite(sample.sensorPredicted.sensor2)
    ));
    const fit = getLinearFit(plottableSamples);
    const axisRanges = getAxisRanges({ fit, plottableSamples, predictedSamples });

    this.updateMetrics({ fit, plottableSamples, samples });
    this.renderGainBreakdown(getGainBreakdownRows(this.dataset, plottableSamples));
    this.renderChart({ axisRanges, fit, plottableSamples, predictedSamples });
  }

  updateMetrics({ fit, plottableSamples, samples }) {
    if (this.samplesMetric) {
      this.samplesMetric.textContent = String(samples.length);
    }

    if (this.slopeMetric) {
      this.slopeMetric.textContent = Number.isFinite(fit.slope)
        ? fit.slope.toFixed(3)
        : "-";
    }

    if (this.rmsMetric) {
      this.rmsMetric.textContent = formatMillivolts(fit.rms);
    }

    if (this.slopeRatioMetric) {
      this.slopeRatioMetric.textContent = formatRatio(
        getSlopeMultiplierRatio(this.dataset, fit, plottableSamples),
      );
    }

    this.setBadge(samples.length
      ? `${plottableSamples.length}/${samples.length} plotted`
      : "empty dataset");
  }

  setBadge(text) {
    if (this.badge) {
      this.badge.textContent = text;
    }
  }

  renderGainBreakdown(rows) {
    if (!this.gainBreakdown) {
      return;
    }

    const title = document.createElement("span");
    title.className = "analysis-gain-breakdown__title";
    title.textContent = "By gain";

    const grid = document.createElement("div");
    grid.className = "analysis-gain-table";

    ["wiper", "model x", "slope/gain", "n"].forEach((label) => {
      const cell = document.createElement("span");
      cell.className = "analysis-gain-table__head";
      cell.textContent = label;
      grid.append(cell);
    });

    if (rows.length) {
      rows.forEach((row) => {
        [
          formatGainWiper(row.gain),
          formatMultiplier(row.multiplier),
          formatRatio(row.slopeMultiplierRatio),
          String(row.samples),
        ].forEach((value) => {
          const cell = document.createElement("span");
          cell.textContent = value;
          grid.append(cell);
        });
      });
    } else {
      const empty = document.createElement("span");
      empty.className = "analysis-gain-table__empty";
      empty.textContent = "-";
      grid.append(empty);
    }

    this.gainBreakdown.replaceChildren(title, grid);
  }

  renderChart({ axisRanges, fit, plottableSamples, predictedSamples }) {
    if (!this.chartRoot) {
      return;
    }

    Plotly.react(this.chartRoot, [
      {
        customdata: plottableSamples.map((sample) => [
          sample.wipers.top,
          sample.wipers.bot,
          sample.wipers.mid,
          sample.wipers.offset,
          sample.wipers.gain,
        ]),
        hovertemplate: [
          "Sensor1 %{x:.4f} V",
          "actual Sensor2 %{y:.4f} V",
          "mid %{customdata[2]}",
          "offset %{customdata[3]}",
          "gain %{customdata[4]}",
          "<extra></extra>",
        ].join("<br>"),
        marker: {
          color: plottableSamples.map((sample) => sample.wipers.mid),
          colorscale: [
            [0, "#35c2ff"],
            [0.5, "#7ee787"],
            [1, "#ffcf5a"],
          ],
          line: { color: "rgba(255, 255, 255, 0.72)", width: 0.8 },
          opacity: 0.92,
          size: 10,
        },
        mode: "markers",
        name: "Sensor2 actual",
        type: "scatter",
        x: plottableSamples.map((sample) => sample.sensorActual.sensor1),
        y: plottableSamples.map((sample) => sample.sensorActual.sensor2),
      },
      {
        hoverinfo: "skip",
        line: { color: "rgba(255, 255, 255, 0.48)", dash: "dot", width: 2 },
        mode: "lines",
        name: "linear fit",
        type: "scatter",
        x: fit.lineX,
        y: fit.lineY,
      },
      {
        customdata: predictedSamples.map((sample) => [
          sample.wipers.top,
          sample.wipers.bot,
          sample.wipers.mid,
          sample.wipers.offset,
          sample.wipers.gain,
        ]),
        hovertemplate: [
          "Sensor1 %{x:.4f} V",
          "predicted Sensor2 %{y:.4f} V",
          "mid %{customdata[2]}",
          "offset %{customdata[3]}",
          "gain %{customdata[4]}",
          "<extra></extra>",
        ].join("<br>"),
        marker: {
          color: "#ff7b72",
          line: { color: "rgba(255, 255, 255, 0.52)", width: 0.8 },
          opacity: 0.62,
          size: 8,
          symbol: "diamond-open",
        },
        mode: "markers",
        name: "Sensor2 predicted",
        type: "scatter",
        x: predictedSamples.map((sample) => sample.sensorActual.sensor1),
        y: predictedSamples.map((sample) => sample.sensorPredicted.sensor2),
      },
    ], getChartLayout(axisRanges), {
      displaylogo: false,
      responsive: true,
    });

    if (!this.resizeObserver) {
      this.resizeObserver = new ResizeObserver(() => Plotly.Plots.resize(this.chartRoot));
      this.resizeObserver.observe(this.chartRoot);
    }
  }
}

function getAxisRanges({ fit, plottableSamples, predictedSamples }) {
  if (!plottableSamples.length) {
    return {
      x: EMPTY_AXIS_RANGE,
      y: EMPTY_AXIS_RANGE,
    };
  }

  return {
    x: getPaddedRange(plottableSamples.map((sample) => sample.sensorActual.sensor1)),
    y: getPaddedRange([
      ...plottableSamples.map((sample) => sample.sensorActual.sensor2),
      ...predictedSamples.map((sample) => sample.sensorPredicted.sensor2),
      ...fit.lineY,
    ]),
  };
}

function getPaddedRange(values) {
  const knownValues = values.filter(Number.isFinite);
  const min = Math.min(...knownValues);
  const max = Math.max(...knownValues);
  const padding = Math.max((max - min) * 0.08, 0.025);

  return [min - padding, max + padding];
}

function getLinearFit(samples) {
  if (samples.length < 2) {
    return {
      intercept: null,
      lineX: [],
      lineY: [],
      rms: null,
      slope: null,
    };
  }

  const points = samples.map((sample) => ({
    x: sample.sensorActual.sensor1,
    y: sample.sensorActual.sensor2,
  }));
  const meanX = getMean(points.map((point) => point.x));
  const meanY = getMean(points.map((point) => point.y));
  const varianceX = points.reduce((sum, point) => sum + (point.x - meanX) ** 2, 0);

  if (!Number.isFinite(varianceX) || varianceX === 0) {
    return {
      intercept: null,
      lineX: [],
      lineY: [],
      rms: null,
      slope: null,
    };
  }

  const covariance = points.reduce(
    (sum, point) => sum + (point.x - meanX) * (point.y - meanY),
    0,
  );
  const slope = covariance / varianceX;
  const intercept = meanY - slope * meanX;
  const residuals = points.map((point) => point.y - (slope * point.x + intercept));
  const lineX = getPaddedRange(points.map((point) => point.x));
  const lineY = lineX.map((x) => slope * x + intercept);

  return {
    intercept,
    lineX,
    lineY,
    rms: getRms(residuals),
    slope,
  };
}

function getChartLayout(axisRanges) {
  return {
    autosize: true,
    legend: {
      font: { color: "#d7dde8", size: 12 },
      orientation: "h",
      x: 0,
      y: 1.08,
    },
    margin: { b: 54, l: 64, r: 28, t: 42 },
    paper_bgcolor: "rgba(0, 0, 0, 0)",
    plot_bgcolor: "rgba(8, 20, 28, 0.72)",
    xaxis: {
      color: "#b8c2d6",
      gridcolor: "rgba(184, 194, 214, 0.14)",
      range: axisRanges.x,
      title: { font: { color: "#d7dde8" }, text: "Sensor1 (V)" },
      zeroline: false,
    },
    yaxis: {
      color: "#b8c2d6",
      gridcolor: "rgba(184, 194, 214, 0.14)",
      range: axisRanges.y,
      title: { font: { color: "#d7dde8" }, text: "Sensor2 (V)" },
      zeroline: false,
    },
  };
}

function getSlopeMultiplierRatio(dataset, fit, samples) {
  if (!Number.isFinite(fit.slope)) {
    return null;
  }

  const multipliers = samples
    .map((sample) => dataset.sensorModel.gainRatioFromWiper(sample.wipers.gain))
    .filter(Number.isFinite);
  const multiplier = getMean(multipliers);

  return Number.isFinite(multiplier) && multiplier !== 0
    ? fit.slope / multiplier
    : null;
}

function getGainBreakdownRows(dataset, samples) {
  const samplesByGain = new Map();

  samples.forEach((sample) => {
    const gain = Number(sample.wipers.gain);

    if (!Number.isFinite(gain)) {
      return;
    }

    if (!samplesByGain.has(gain)) {
      samplesByGain.set(gain, []);
    }

    samplesByGain.get(gain).push(sample);
  });

  return Array.from(samplesByGain, ([gain, gainSamples]) => {
    const fit = getLinearFit(gainSamples);
    const multipliers = gainSamples
      .map((sample) => dataset.sensorModel.gainRatioFromWiper(sample.wipers.gain))
      .filter(Number.isFinite);

    return {
      gain,
      multiplier: getMean(multipliers),
      samples: gainSamples.length,
      slopeMultiplierRatio: getSlopeMultiplierRatio(dataset, fit, gainSamples),
    };
  }).sort((a, b) => a.gain - b.gain);
}

function getMean(values) {
  const knownValues = values.filter(Number.isFinite);

  if (!knownValues.length) {
    return null;
  }

  return knownValues.reduce((sum, value) => sum + value, 0) / knownValues.length;
}

function getRms(values) {
  const knownValues = values.filter(Number.isFinite);

  if (!knownValues.length) {
    return null;
  }

  return Math.sqrt(
    knownValues.reduce((sum, value) => sum + value ** 2, 0) / knownValues.length,
  );
}

function formatMillivolts(value) {
  return Number.isFinite(value)
    ? `${(value * 1000).toFixed(1)} mV`
    : "-";
}

function formatRatio(value) {
  return Number.isFinite(value) ? value.toFixed(3) : "-";
}

function formatGainWiper(value) {
  return Number.isFinite(value) ? String(value) : "-";
}

function formatMultiplier(value) {
  return Number.isFinite(value) ? `x${value.toFixed(3)}` : "-";
}

async function copyText(text) {
  if (navigator.clipboard?.writeText) {
    await navigator.clipboard.writeText(text);
    return;
  }

  const textarea = document.createElement("textarea");

  textarea.value = text;
  textarea.setAttribute("readonly", "");
  textarea.style.position = "fixed";
  textarea.style.top = "-9999px";
  document.body.append(textarea);
  textarea.select();

  try {
    if (!document.execCommand("copy")) {
      throw new Error("Copy command failed");
    }
  } finally {
    textarea.remove();
  }
}
