export class FreezeWipers {
  constructor({ button, getWipers = null, normaliseWipers, webView }) {
    this.button = button;
    this.getWipers = getWipers;
    this.normaliseWipers = normaliseWipers;
    this.webView = webView;
    this.frozen = false;
    this.lastManualWiperCommandKey = null;

    this.button?.addEventListener("click", () => {
      const shouldFreeze = !this.frozen;
      this.setFrozen(shouldFreeze, { postCurrent: shouldFreeze });
    });

    this.updateButton();
  }

  setFrozen(isFrozen, { postCurrent = false } = {}) {
    if (this.frozen === isFrozen) {
      if (this.frozen && postCurrent) {
        this.postCurrentWipers();
      }
      return;
    }

    this.frozen = isFrozen;

    if (this.frozen) {
      if (postCurrent) {
        this.postCurrentWipers();
      }
    } else {
      this.lastManualWiperCommandKey = null;
      this.webView.postGetWipers();
    }

    this.updateButton();
  }

  handleManualInput({ phase, wipers }) {
    this.setFrozen(true);

    if (phase === "start") {
      this.lastManualWiperCommandKey = null;
      return;
    }

    if (phase !== "change") {
      return;
    }

    const commandWipers = this.normaliseWipers(wipers);
    const commandKey = JSON.stringify(commandWipers);

    if (commandKey === this.lastManualWiperCommandKey) {
      return;
    }

    this.lastManualWiperCommandKey = commandKey;
    this.webView.postSetWipers(commandWipers);
  }

  postCurrentWipers() {
    if (!this.getWipers) {
      return false;
    }

    const commandWipers = this.normaliseWipers(this.getWipers());
    this.lastManualWiperCommandKey = JSON.stringify(commandWipers);
    return this.webView.postSetWipers(commandWipers);
  }

  updateButton() {
    if (!this.button) {
      return;
    }

    this.button.textContent = this.frozen ? "Resume wipers" : "Freeze wipers";
    this.button.setAttribute("aria-pressed", String(this.frozen));
    this.button.dataset.frozen = String(this.frozen);
  }
}
