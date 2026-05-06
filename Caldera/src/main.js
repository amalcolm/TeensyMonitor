import { CircuitScene } from "./scene/CircuitScene.js";
import { Model } from "./model/Model.js";
import { SUPPLY_VOLTAGE } from "./scene/voltage.js";
import { WebView } from "./WebView.js";

const SETTINGS_STORAGE_KEY = "caldera:circuit-settings:v1";
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
  <div class="scene-stage" data-scene></div>
`;

const sceneRoot = document.querySelector("[data-scene]");
const photodiodeInput = document.querySelector("[data-photodiode-voltage]");
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

webView.on("setPhotodiodeVoltage", ({ value }) => {
  const wasApplied = circuitScene.setPhotoDiodeVoltage(value, { notify: false });

  if (wasApplied) {
    photodiodeInput.value = formatInputValue(circuitScene.getPhotoDiodeVoltage());
  }
});

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
