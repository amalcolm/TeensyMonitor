import { CircuitScene } from "./scene/CircuitScene.js";
import { Model } from "./model/Model.js";
import { SUPPLY_VOLTAGE } from "./scene/voltage.js";
import { WebView } from "./WebView.js";

const SETTINGS_STORAGE_KEY = "caldera:circuit-settings:v1";
const WIPER_IDS = ["top", "bot", "mid", "offset", "gain"];
const model = new Model();
const webView = new WebView(model);
const storedSettings = readStoredSettings();

document.querySelector("#app").innerHTML = `
  <div class="temporary-controls">
    <label for="photodiode-voltage">Photodiode</label>
    <input
      id="photodiode-voltage"
      data-photodiode-voltage
      type="number"
      min="0"
      max="${SUPPLY_VOLTAGE}"
      step="0.001"
      value="${formatInputValue(storedSettings?.photodiodeVoltage ?? 0.444)}"
    />
    <span>V</span>
  </div>
  <button
    class="webview-freeze"
    type="button"
    data-webview-freeze
    aria-pressed="false"
  >
    Freeze inputs
  </button>
  <div class="scene-stage" data-scene></div>
  <div class="wiper-debug" data-wiper-debug>
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
const photodiodeInput = document.querySelector("[data-photodiode-voltage]");
const webViewFreezeButton = document.querySelector("[data-webview-freeze]");
const wiperDebugStatus = document.querySelector("[data-wiper-debug-status]");
const wiperDebugKeys = document.querySelector("[data-wiper-debug-keys]");
const incomingDebugById = new Map(
  WIPER_IDS.map((id) => [id, document.querySelector(`[data-wiper-debug-incoming="${id}"]`)]),
);
const modelDebugById = new Map(
  WIPER_IDS.map((id) => [id, document.querySelector(`[data-wiper-debug-model="${id}"]`)]),
);
let wiperMessageCount = 0;
let webViewInputsFrozen = false;
const circuitScene = new CircuitScene(sceneRoot, model, {
  onSettingsChange: saveStoredSettings,
});

circuitScene.applySettings(storedSettings);
photodiodeInput.value = formatInputValue(circuitScene.getPhotoDiodeVoltage());
circuitScene.start();

photodiodeInput.addEventListener("input", () => {
  circuitScene.setPhotoDiodeVoltage(photodiodeInput.valueAsNumber);
});

photodiodeInput.addEventListener("change", () => {
  photodiodeInput.value = formatInputValue(circuitScene.getPhotoDiodeVoltage());
});

webViewFreezeButton.addEventListener("click", () => {
  webViewInputsFrozen = !webViewInputsFrozen;
  updateFreezeButton();
});

webView.on("setPhotodiodeVoltage", ({ value }) => {
  if (webViewInputsFrozen) {
    return;
  }

  const wasApplied = circuitScene.setPhotoDiodeVoltage(value, { notify: false });

  if (wasApplied) {
    photodiodeInput.value = formatInputValue(circuitScene.getPhotoDiodeVoltage());
  }
});

webView.on("wipersChanged", ({ wipers }) => {
  if (webViewInputsFrozen) {
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
  if (webViewInputsFrozen) {
    return;
  }

  if (circuitScene.applyPhysicalVoltages(voltages)) {
    circuitScene.render();
  }
});

updateFreezeButton();
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

function formatInputValue(value) {
  const number = Number(value);

  return Number.isFinite(number) ? String(number) : "0.444";
}

function updateFreezeButton() {
  webViewFreezeButton.textContent = webViewInputsFrozen
    ? "Resume inputs"
    : "Freeze inputs";
  webViewFreezeButton.setAttribute("aria-pressed", String(webViewInputsFrozen));
  webViewFreezeButton.dataset.frozen = String(webViewInputsFrozen);
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
