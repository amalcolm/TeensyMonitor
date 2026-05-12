import { CircuitScene } from "./scene/CircuitScene.js";
import { DebugFlagsControl } from "./helpers/DebugFlagsControl.js";
import { FreezeVoltages } from "./helpers/FreezeVoltages.js";
import { FreezeWipers } from "./helpers/FreezeWipers.js";
import { Model } from "./model/Model.js";
import { STATE_LED_ROWS, StateControl } from "./helpers/StateControl.js";
import { Sweep } from "./helpers/Sweep.js";
import { WebView } from "./WebView.js";
import { WIPER_IDS, getModelWipers, normaliseWipers } from "./helpers/Wipers.js";

const SETTINGS_STORAGE_KEY = "caldera:circuit-settings:v1";
const model = new Model();
const webView = new WebView(model);
const storedSettings = readStoredSettings();

document.querySelector("#app").innerHTML = `
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
    <span data-state-status>idle</span>
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
  <div class="scene-stage" data-scene></div>
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
      <button class="test-panel__button" type="button" data-test-copy-button>
        Copy
      </button>
      <button class="test-panel__button" type="button" data-test-clear-button>
        Clear
      </button>
    </div>
    <textarea
      class="test-panel__output"
      data-test-output
      readonly
      spellcheck="false"
    ></textarea>
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
`;

const sceneRoot = document.querySelector("[data-scene]");
const freezeWipersButton = document.querySelector("[data-webview-freeze-wipers]");
const freezeVoltagesButton = document.querySelector("[data-webview-freeze-voltages]");
const midSweepButton = document.querySelector("[data-mid-sweep-button]");
const stateButtons = document.querySelectorAll("[data-state-toggle]");
const stateStatus = document.querySelector("[data-state-status]");
const debugFlagInputs = document.querySelectorAll("[data-debug-flag]");
const debugFlagsStatus = document.querySelector("[data-debug-flags-status]");
const testCopyButton = document.querySelector("[data-test-copy-button]");
const testClearButton = document.querySelector("[data-test-clear-button]");
const testOutput = document.querySelector("[data-test-output]");
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
  status: stateStatus,
  webView,
});
new DebugFlagsControl({
  inputs: debugFlagInputs,
  status: debugFlagsStatus,
  webView,
});
new Sweep({
  button: midSweepButton,
  circuitScene,
  clearButton: testClearButton,
  copyButton: testCopyButton,
  freezeVoltages,
  freezeWipers,
  model,
  output: testOutput,
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
