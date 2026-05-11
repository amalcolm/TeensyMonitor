import { getModelWipers, normaliseWipers } from "./Wipers.js";

const MID_SWEEP_STEP = 16;
const MID_SWEEP_INTERVAL_MS = 50;
const MID_SWEEP_HEADER = "top,bot,mid,mid-outV,Sensor1";

export class Sweep {
  constructor({
    button,
    circuitScene,
    clearButton,
    copyButton,
    freezeVoltages,
    freezeWipers,
    model,
    output,
    status,
    updateWiperDebug,
    webView,
  }) {
    this.button = button;
    this.circuitScene = circuitScene;
    this.clearButton = clearButton;
    this.copyButton = copyButton;
    this.freezeVoltages = freezeVoltages;
    this.freezeWipers = freezeWipers;
    this.model = model;
    this.output = output;
    this.status = status;
    this.updateWiperDebug = updateWiperDebug;
    this.webView = webView;
    this.currentMid = 0;
    this.timer = null;
    this.wasVoltagesFrozen = false;
    this.wasWipersFrozen = false;

    this.button?.addEventListener("click", () => {
      if (this.timer) {
        this.stop("stopped");
      } else {
        this.start();
      }
    });

    this.copyButton?.addEventListener("click", () => this.copyOutput());
    this.clearButton?.addEventListener("click", () => this.clear());

    this.setOutput("");
    this.updateButton();
  }

  start() {
    this.wasWipersFrozen = this.freezeWipers.frozen;
    this.wasVoltagesFrozen = this.freezeVoltages.frozen;
    this.freezeWipers.setFrozen(true);

    if (this.freezeVoltages.frozen) {
      this.freezeVoltages.setFrozen(false);
    }

    this.setOutput(`${MID_SWEEP_HEADER}\n`);
    this.currentMid = 0;
    this.applyMidWiper(this.currentMid);
    this.updateStatus(`mid ${this.currentMid}`);

    this.timer = window.setInterval(() => this.runStep(), MID_SWEEP_INTERVAL_MS);
    this.updateButton();
  }

  runStep() {
    this.appendRow();

    if (this.currentMid >= 255) {
      this.stop("done");
      return;
    }

    this.currentMid = Math.min(this.currentMid + MID_SWEEP_STEP, 255);
    this.applyMidWiper(this.currentMid);
    this.updateStatus(`mid ${this.currentMid}`);
  }

  stop(status = "idle") {
    if (this.timer) {
      window.clearInterval(this.timer);
      this.timer = null;
    }

    if (!this.wasWipersFrozen) {
      this.freezeWipers.setFrozen(false);
    }

    if (this.wasVoltagesFrozen) {
      this.freezeVoltages.setFrozen(true);
    }

    this.updateStatus(status);
    this.updateButton();
  }

  clear() {
    if (this.timer) {
      return;
    }

    this.setOutput("");
    this.updateStatus("idle");
  }

  applyMidWiper(mid) {
    const wipers = normaliseWipers({
      ...getModelWipers(this.model),
      mid,
    });

    this.model.applyWiperValues(wipers);
    this.updateWiperDebug(wipers, { applied: true });
    this.circuitScene.render();
    this.webView.postSetWipers(wipers);
  }

  appendRow() {
    const wipers = getModelWipers(this.model);
    const row = [
      wipers.top,
      wipers.bot,
      wipers.mid,
      this.formatCsvNumber(this.model.mid?.wiperVoltage),
      this.formatCsvNumber(this.getSensor1Voltage()),
    ].join(",");

    this.output.value += `${row}\n`;
    this.output.scrollTop = this.output.scrollHeight;
  }

  getSensor1Voltage() {
    return Number.isFinite(this.model.sensor1Voltage)
      ? this.model.sensor1Voltage
      : this.circuitScene.getSceneSensor1Voltage();
  }

  setOutput(value) {
    this.output.value = value;
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

  async copyOutput() {
    const text = this.output.value;

    if (!text) {
      return;
    }

    try {
      if (!navigator.clipboard?.writeText) {
        throw new Error("Clipboard unavailable");
      }

      await navigator.clipboard.writeText(text);
      this.updateStatus("copied");
    } catch {
      this.output.focus();
      this.output.select();
      this.updateStatus("selected");
    }
  }

  formatCsvNumber(value) {
    const number = Number(value);

    return Number.isFinite(number) ? number.toFixed(6) : "";
  }
}
