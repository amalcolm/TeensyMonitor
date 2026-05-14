import { getModelWipers, normaliseWipers } from "./Wipers.js";

const MID_SWEEP_START = 16;
const MID_SWEEP_END = 240;
const MID_SWEEP_STEP = 8;
const SWEEP_SETTLE_MS = 100;
const SWEEP_SAMPLE_INTERVAL_MS = 50;
const SWEEP_FILTER_SAMPLE_COUNT = 10;
const SWEEP_FILTER_T = 1 / SWEEP_FILTER_SAMPLE_COUNT;
const GAIN_SWEEP_WIPERS = Object.freeze([0, 1, 2, 4, 8, 16, 32]);

export class Sweep {
  constructor({
    button,
    canClear = null,
    circuitScene,
    clearButton,
    freezeVoltages,
    freezeWipers,
    getHardwareWiperRevision = null,
    getHardwareWipers = null,
    model,
    onClear = null,
    onStart = null,
    onSample = null,
    requireWiperAck = false,
    status,
    updateWiperDebug,
    webView,
  }) {
    this.button = button;
    this.canClear = canClear;
    this.circuitScene = circuitScene;
    this.clearButton = clearButton;
    this.freezeVoltages = freezeVoltages;
    this.freezeWipers = freezeWipers;
    this.getHardwareWiperRevision = getHardwareWiperRevision;
    this.getHardwareWipers = getHardwareWipers;
    this.model = model;
    this.onClear = onClear;
    this.onStart = onStart;
    this.onSample = onSample;
    this.requireWiperAck = requireWiperAck;
    this.status = status;
    this.updateWiperDebug = updateWiperDebug;
    this.webView = webView;
    this.currentMid = MID_SWEEP_START;
    this.filteredVoltages = null;
    this.sampleCount = 0;
    this.targetWipers = null;
    this.timer = null;
    this.wasVoltagesFrozen = false;
    this.wasWipersFrozen = false;
    this.wiperAcknowledged = false;

    this.button?.addEventListener("click", () => {
      if (this.timer) {
        this.stop("stopped");
      } else {
        this.start();
      }
    });

    this.clearButton?.addEventListener("click", () => this.clear());

    this.updateButton();
  }

  start() {
    this.onStart?.(this);
    this.wasWipersFrozen = this.freezeWipers.frozen;
    this.wasVoltagesFrozen = this.freezeVoltages.frozen;
    this.freezeWipers.setFrozen(true);

    if (this.freezeVoltages.frozen) {
      this.freezeVoltages.setFrozen(false);
    }

    this.onClear?.();
    this.currentMid = MID_SWEEP_START;
    this.beginCurrentPoint();
  }

  runStep() {
    if (!this.timer) {
      return;
    }

    const captureResult = this.captureFilterSample();

    if (captureResult === "settling") {
      this.scheduleStep(SWEEP_SETTLE_MS);
      return;
    }

    if (!captureResult || this.sampleCount < SWEEP_FILTER_SAMPLE_COUNT) {
      this.scheduleStep(SWEEP_SAMPLE_INTERVAL_MS);
      return;
    }

    this.addFilteredSample();
    this.advanceSweep();
  }

  stop(status = "idle") {
    const wasRunning = Boolean(this.timer);

    if (this.timer) {
      window.clearTimeout(this.timer);
      this.timer = null;
    }

    if (wasRunning) {
      if (!this.wasWipersFrozen) {
        this.freezeWipers.setFrozen(false);
      }

      if (this.wasVoltagesFrozen) {
        this.freezeVoltages.setFrozen(true);
      }
    }

    this.updateStatus(status);
    this.updateButton();
  }

  clear() {
    if (this.timer || this.canClear?.() === false) {
      return;
    }

    this.onClear?.();
    this.updateStatus("idle");
  }

  beginCurrentPoint() {
    this.filteredVoltages = null;
    this.sampleCount = 0;
    this.targetWipers = null;
    this.wiperAcknowledged = !this.requireWiperAck;
    this.applyCurrentWipers();
    this.updateStatus(this.getSweepStatus());
    this.scheduleStep(this.wiperAcknowledged ? SWEEP_SETTLE_MS : SWEEP_SAMPLE_INTERVAL_MS);
  }

  scheduleStep(delayMs) {
    this.timer = window.setTimeout(() => this.runStep(), delayMs);
    this.updateButton();
  }

  captureFilterSample() {
    if (!this.wiperAcknowledged) {
      if (!this.hasHardwareAppliedTargetWipers()) {
        this.updateStatus(`${this.getSweepStatus()} waiting for wipers`);
        return false;
      }

      this.wiperAcknowledged = true;
      this.updateStatus(`${this.getSweepStatus()} settling`);
      return "settling";
    }

    this.filteredVoltages = filterVoltages(
      this.filteredVoltages,
      this.readVoltages(),
    );
    this.sampleCount += 1;
    this.updateStatus(
      `${this.getSweepStatus()} sample ${this.sampleCount}/${SWEEP_FILTER_SAMPLE_COUNT}`,
    );
    return true;
  }

  readVoltages() {
    return {
      sensor1: getKnownVoltage(this.model?.sensor1Voltage)
        ?? getKnownVoltage(this.circuitScene?.getSceneSensor1Voltage?.()),
      sensor2: getKnownVoltage(this.model?.sensor2Voltage),
    };
  }

  addFilteredSample() {
    this.onSample?.({
      circuitScene: this.circuitScene,
      model: this.model,
      sensorVoltages: this.filteredVoltages ? { ...this.filteredVoltages } : null,
      source: this.getSampleSource(),
    });
  }

  advanceSweep() {
    if (this.currentMid >= MID_SWEEP_END) {
      this.stop("done");
      return;
    }

    this.currentMid = Math.min(this.currentMid + MID_SWEEP_STEP, MID_SWEEP_END);
    this.beginCurrentPoint();
  }

  applyCurrentWipers() {
    this.applyMidWiper(this.currentMid);
  }

  applyMidWiper(mid) {
    const wipers = normaliseWipers({
      ...getModelWipers(this.model),
      mid,
    });

    this.targetWipers = wipers;
    this.model.applyWiperValues(wipers);
    this.updateWiperDebug(wipers, { applied: true });
    this.circuitScene.render();
    this.webView.postSetWipers(wipers);
  }

  hasHardwareAppliedTargetWipers() {
    if (!this.targetWipers) {
      return true;
    }

    const revision = Number(this.getHardwareWiperRevision?.());

    if (!Number.isFinite(revision) || revision <= 0) {
      return false;
    }

    return areWipersEqual(this.getHardwareWipers?.(), this.targetWipers);
  }

  getSweepStatus() {
    return `mid ${this.currentMid}`;
  }

  getSampleSource() {
    return "mid-sweep";
  }

  updateStatus(status) {
    this.status.textContent = status;
  }

  updateButton() {
    if (!this.button) {
      return;
    }

    this.button.textContent = this.timer ? "Stop sweep" : "Sweep mid";
    this.button.dataset.running = String(Boolean(this.timer));
  }
}

export class GainSweep extends Sweep {
  constructor(options) {
    super(options);
    this.currentGainIndex = 0;
    this.updateButton();
  }

  start() {
    this.onStart?.(this);
    this.wasWipersFrozen = this.freezeWipers.frozen;
    this.wasVoltagesFrozen = this.freezeVoltages.frozen;
    this.freezeWipers.setFrozen(true);

    if (this.freezeVoltages.frozen) {
      this.freezeVoltages.setFrozen(false);
    }

    this.onClear?.();
    this.currentGainIndex = 0;
    this.currentMid = MID_SWEEP_START;
    this.beginCurrentPoint();
  }

  advanceSweep() {
    if (this.currentMid < MID_SWEEP_END) {
      this.currentMid = Math.min(this.currentMid + MID_SWEEP_STEP, MID_SWEEP_END);
      this.beginCurrentPoint();
      return;
    }

    if (this.currentGainIndex < GAIN_SWEEP_WIPERS.length - 1) {
      this.currentGainIndex += 1;
      this.currentMid = MID_SWEEP_START;
      this.beginCurrentPoint();
      return;
    }

    this.stop("done");
  }

  applyCurrentWipers() {
    this.applyGainMidWipers();
  }

  applyGainMidWipers() {
    const gain = GAIN_SWEEP_WIPERS[this.currentGainIndex];
    const wipers = normaliseWipers({
      ...getModelWipers(this.model),
      gain,
      mid: this.currentMid,
    });

    this.targetWipers = wipers;
    this.model.applyWiperValues(wipers);
    this.updateWiperDebug(wipers, { applied: true });
    this.circuitScene.render();
    this.webView.postSetWipers(wipers);
  }

  getSweepStatus() {
    const gain = GAIN_SWEEP_WIPERS[this.currentGainIndex];

    return `gain ${gain} mid ${this.currentMid}`;
  }

  getSampleSource() {
    return "gain-mid-sweep";
  }

  updateButton() {
    if (!this.button) {
      return;
    }

    this.button.textContent = this.timer ? "Stop gain" : "Sweep gain";
    this.button.dataset.running = String(Boolean(this.timer));
  }
}

function filterVoltages(oldVoltages, newVoltages) {
  return {
    sensor1: filterVoltage(oldVoltages?.sensor1, newVoltages?.sensor1),
    sensor2: filterVoltage(oldVoltages?.sensor2, newVoltages?.sensor2),
  };
}

function areWipersEqual(actual, expected) {
  if (!actual || !expected) {
    return false;
  }

  return Object.entries(expected).every(([id, value]) => Number(actual[id]) === Number(value));
}

function filterVoltage(oldValue, newValue) {
  const newVoltage = getKnownVoltage(newValue);

  if (!Number.isFinite(newVoltage)) {
    return getKnownVoltage(oldValue);
  }

  const oldVoltage = getKnownVoltage(oldValue);

  if (!Number.isFinite(oldVoltage)) {
    return newVoltage;
  }

  return (1 - SWEEP_FILTER_T) * oldVoltage + SWEEP_FILTER_T * newVoltage;
}

function getKnownVoltage(value) {
  const voltage = Number(value);

  return Number.isFinite(voltage) ? voltage : null;
}
