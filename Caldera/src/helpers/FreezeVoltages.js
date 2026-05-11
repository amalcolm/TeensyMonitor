export class FreezeVoltages {
  constructor({ button, circuitScene }) {
    this.button = button;
    this.circuitScene = circuitScene;
    this.frozen = false;
    this.lastLiveVoltages = null;

    this.button?.addEventListener("click", () => {
      this.setFrozen(!this.frozen);
    });

    this.updateButton();
  }

  handleLiveVoltages(voltages) {
    this.lastLiveVoltages = voltages;

    if (this.frozen) {
      return;
    }

    this.applyLiveVoltages(voltages);
  }

  setFrozen(isFrozen) {
    if (this.frozen === isFrozen) {
      return;
    }

    this.frozen = isFrozen;
    this.circuitScene.setPhysicalVoltagesFrozen(this.frozen);

    if (!this.frozen && this.lastLiveVoltages) {
      this.applyLiveVoltages(this.lastLiveVoltages);
    } else {
      this.circuitScene.render();
    }

    this.updateButton();
  }

  applyLiveVoltages(voltages) {
    if (this.circuitScene.applyPhysicalVoltages(voltages)) {
      this.circuitScene.render();
    }
  }

  updateButton() {
    if (!this.button) {
      return;
    }

    this.button.textContent = this.frozen ? "Resume voltages" : "Freeze voltages";
    this.button.setAttribute("aria-pressed", String(this.frozen));
    this.button.dataset.frozen = String(this.frozen);
  }
}
