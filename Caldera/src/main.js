import { AnalysisPanel } from "./analysis/AnalysisPanel.js";
import { CircuitScene } from "./scene/CircuitScene.js";
import { DebugFlagsControl } from "./helpers/DebugFlagsControl.js";
import { FreezeVoltages } from "./helpers/FreezeVoltages.js";
import { FreezeWipers } from "./helpers/FreezeWipers.js";
import { Model } from "./model/Model.js";
import { STATE_LED_ROWS, StateControl } from "./helpers/StateControl.js";
import { GainSweep, Sweep } from "./helpers/Sweep.js";
import { WebView } from "./WebView.js";
import { WIPER_IDS, getModelWipers, normaliseWipers } from "./helpers/Wipers.js";

const SETTINGS_STORAGE_KEY = "caldera:circuit-settings:v1";
const model = new Model();
const webView = new WebView(model);
const storedSettings = readStoredSettings();

document.querySelector("#app").innerHTML = `
  <div class="app-layout">
    <section class="analysis-div" data-analysis-div>
      <div class="analysis-panel">
        <div class="analysis-panel__header">
          <div>
            <span class="analysis-panel__eyebrow">Caldera modelling</span>
            <h1>Data analysis</h1>
          </div>
          <div class="analysis-panel__header-actions">
            <button
              class="analysis-panel__button"
              type="button"
              data-analysis-copy-csv
            >
              Copy CSV
            </button>
            <span class="analysis-panel__badge" data-analysis-badge>empty dataset</span>
          </div>
        </div>
        <div class="analysis-panel__body">
          <aside class="analysis-panel__sidebar">
            <div class="analysis-metric analysis-metric--primary">
              <span class="analysis-metric__label">Linear slope</span>
              <strong data-analysis-slope>-</strong>
            </div>
            <div class="analysis-metric">
              <span class="analysis-metric__label">Fit RMS</span>
              <strong data-analysis-rms>-</strong>
            </div>
            <div class="analysis-metric">
              <span class="analysis-metric__label">Slope / gain</span>
              <strong data-analysis-slope-ratio>-</strong>
            </div>
            <div class="analysis-metric">
              <span class="analysis-metric__label">Samples</span>
              <strong data-analysis-samples>0</strong>
            </div>
            <div class="analysis-gain-breakdown" data-analysis-gain-breakdown></div>
            <div class="analysis-todo analysis-panel__disabled">
              <span class="analysis-todo__title">Next calibration passes</span>
              <span>Fit offset endpoint voltages</span>
              <span>Fit gain intercept and slope</span>
              <span>Reject clipped output samples</span>
            </div>
          </aside>
          <div class="analysis-chart" data-analysis-chart></div>
        </div>
      </div>
    </section>
    <section class="circuit-div" data-circuit-div>
      <div class="scene-stage" data-scene></div>
      <div class="state-panel">
        <div class="state-panel__grid">
          ${STATE_LED_ROWS.map((row) => `
            <div class="state-panel__row">
              ${row.map(({ id, kind, label }) => `
                <button
                  class="state-panel__button"
                  type="button"
                  data-state-toggle="${id}"
                  data-state-kind="${kind}"
                >
                  ${label}
                </button>
              `).join("")}
            </div>
          `).join("")}
        </div>
      </div>
      <div class="webview-freeze-controls">
        <button
          class="webview-freeze"
          type="button"
          data-webview-freeze-wipers
          aria-pressed="false"
        >
          Freeze wipers
        </button>
        <button
          class="webview-freeze"
          type="button"
          data-webview-freeze-voltages
          aria-pressed="false"
        >
          Freeze voltages
        </button>
      </div>
      <div class="debug-panel" data-debug-panel>
        <div class="debug-panel__header">
          <span>Debug flags</span>
          <span data-debug-flags-status>0x00000000</span>
        </div>
        <label class="debug-panel__option">
          <input type="checkbox" data-debug-flag="update" />
          <span>Update</span>
        </label>
      </div>
      <div class="test-panel">
        <div class="test-panel__header">
          <span>Tests</span>
          <span data-test-status>idle</span>
        </div>
        <div class="test-panel__actions">
          <button class="test-panel__button" type="button" data-mid-sweep-button>
            Sweep mid
          </button>
          <button class="test-panel__button" type="button" data-gain-sweep-button>
            Sweep gain
          </button>
          <button class="test-panel__button" type="button" data-test-clear-button>
            Clear
          </button>
        </div>
      </div>
      <div class="wiper-debug" data-wiper-debug hidden>
        <div class="wiper-debug__header">
          <span>WebView wipers</span>
          <span data-wiper-debug-status>idle</span>
        </div>
        <div class="wiper-debug__keys" data-wiper-debug-keys>keys: -</div>
        <div class="wiper-debug__grid">
          <span></span>
          <span>web</span>
          <span>model</span>
          ${WIPER_IDS.map((id) => `
            <span>${id}</span>
            <span data-wiper-debug-incoming="${id}">-</span>
            <span data-wiper-debug-model="${id}">${formatDebugValue(model[id]?.wiper)}</span>
          `).join("")}
        </div>
      </div>
    </section>
  </div>
`;

const analysisRoot = document.querySelector("[data-analysis-div]");
const sceneRoot = document.querySelector("[data-scene]");
const freezeWipersButton = document.querySelector("[data-webview-freeze-wipers]");
const freezeVoltagesButton = document.querySelector("[data-webview-freeze-voltages]");
const midSweepButton = document.querySelector("[data-mid-sweep-button]");
const gainSweepButton = document.querySelector("[data-gain-sweep-button]");
const stateButtons = document.querySelectorAll("[data-state-toggle]");
const debugFlagInputs = document.querySelectorAll("[data-debug-flag]");
const debugFlagsStatus = document.querySelector("[data-debug-flags-status]");
const testClearButton = document.querySelector("[data-test-clear-button]");
const testStatus = document.querySelector("[data-test-status]");
const wiperDebugStatus = document.querySelector("[data-wiper-debug-status]");
const wiperDebugKeys = document.querySelector("[data-wiper-debug-keys]");
const incomingDebugById = new Map(
  WIPER_IDS.map((id) => [id, document.querySelector(`[data-wiper-debug-incoming="${id}"]`)]),
);
const modelDebugById = new Map(
  WIPER_IDS.map((id) => [id, document.querySelector(`[data-wiper-debug-model="${id}"]`)]),
);
let wiperMessageCount = 0;
let midSweep = null;
let gainSweep = null;
let liveWiperRevision = 0;
let liveWipers = null;
const hasHostTelemetry = Boolean(window.chrome?.webview);
const analysisPanel = new AnalysisPanel({ root: analysisRoot });
const circuitScene = new CircuitScene(sceneRoot, model, {
  onManualWiperInput: handleManualWiperInput,
  onSettingsChange: saveStoredSettings,
});
const freezeWipers = new FreezeWipers({
  button: freezeWipersButton,
  getWipers: () => getModelWipers(model),
  normaliseWipers,
  webView,
});
const freezeVoltages = new FreezeVoltages({
  button: freezeVoltagesButton,
  circuitScene,
});
new StateControl({
  buttons: stateButtons,
  freezeWipers,
  webView,
});
new DebugFlagsControl({
  inputs: debugFlagInputs,
  status: debugFlagsStatus,
  webView,
});
midSweep = new Sweep({
  button: midSweepButton,
  canClear: () => !gainSweep?.timer,
  circuitScene,
  clearButton: testClearButton,
  freezeVoltages,
  freezeWipers,
  getHardwareWiperRevision: () => liveWiperRevision,
  getHardwareWipers: () => liveWipers,
  model,
  onClear: () => analysisPanel.clear(),
  onStart: () => gainSweep?.stop("idle"),
  onSample: (sampleContext) => analysisPanel.addSampleFromModel(sampleContext),
  requireWiperAck: hasHostTelemetry,
  status: testStatus,
  updateWiperDebug,
  webView,
});
gainSweep = new GainSweep({
  button: gainSweepButton,
  circuitScene,
  clearButton: null,
  freezeVoltages,
  freezeWipers,
  getHardwareWiperRevision: () => liveWiperRevision,
  getHardwareWipers: () => liveWipers,
  model,
  onClear: () => analysisPanel.clear(),
  onStart: () => midSweep?.stop("idle"),
  onSample: (sampleContext) => analysisPanel.addSampleFromModel(sampleContext),
  requireWiperAck: hasHostTelemetry,
  status: testStatus,
  updateWiperDebug,
  webView,
});

circuitScene.applySettings(storedSettings);
circuitScene.start();

webView.on("setPhotodiodeVoltage", ({ value }) => {
  circuitScene.setPhotoDiodeVoltage(value, { notify: false });
});

webView.on("wipersChanged", ({ wipers }) => {
  liveWipers = normaliseWipers(wipers);
  liveWiperRevision += 1;

  if (freezeWipers.frozen) {
    updateWiperDebug(wipers, { frozen: true });
    return;
  }

  const applied = model.applyWiperValues(wipers);

  updateWiperDebug(wipers, { applied });

  if (applied) {
    circuitScene.render();
  }
});

webView.on("voltagesChanged", ({ voltages }) => {
  freezeVoltages.handleLiveVoltages(voltages);
});

updateWiperDebug(null, { applied: false });

function readStoredSettings() {
  try {
    return JSON.parse(localStorage.getItem(SETTINGS_STORAGE_KEY));
  } catch {
    return null;
  }
}

function saveStoredSettings(settings) {
  try {
    localStorage.setItem(SETTINGS_STORAGE_KEY, JSON.stringify(settings));
  } catch {
    // Storage is a convenience here; the circuit still works without it.
  }

  webView.postSettingsChange(settings);
}

function handleManualWiperInput({ phase, wipers }) {
  freezeWipers.handleManualInput({ phase, wipers });
}

function updateWiperDebug(wipers, { applied = false, frozen = false } = {}) {
  if (wipers && typeof wipers === "object") {
    wiperMessageCount += 1;
  }

  WIPER_IDS.forEach((id) => {
    const incomingValue = wipers && typeof wipers === "object" ? wipers[id] : undefined;

    incomingDebugById.get(id).textContent = formatDebugValue(incomingValue);
    modelDebugById.get(id).textContent = formatDebugValue(model[id]?.wiper);
  });

  if (!wiperDebugStatus) {
    return;
  }

  if (!wipers || typeof wipers !== "object") {
    wiperDebugStatus.textContent = "idle";
    wiperDebugKeys.textContent = "keys: -";
    return;
  }

  wiperDebugKeys.textContent = `keys: ${Object.keys(wipers).join(", ") || "-"}`;
  wiperDebugStatus.textContent = frozen
    ? `#${wiperMessageCount} frozen`
    : applied
    ? `#${wiperMessageCount} applied`
    : `#${wiperMessageCount} ignored`;
}

function formatDebugValue(value) {
  const number = Number(value);

  return Number.isFinite(number) ? String(number) : "-";
}
