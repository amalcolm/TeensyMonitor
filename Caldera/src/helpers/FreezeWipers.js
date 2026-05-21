export class FreezeWipers {
  constructor({
    button,
    getWipers = null,
    initialFrozen = false,
    normaliseWipers,
    onSettingsChange = null,
    webView,
  }) {
    this.button = button;
    this.getWipers = getWipers;
    this.normaliseWipers = normaliseWipers;
    this.onSettingsChange = onSettingsChange;
    this.webView = webView;
    this.frozen = initialFrozen === true;
    this.lastManualWiperCommandKey = null;

    this.button?.addEventListener("click", () => {
      const shouldFreeze = !this.frozen;
      this.setFrozen(shouldFreeze, { postCurrent: shouldFreeze });
    });

    this.updateButton();
  }

  setFrozen(isFrozen, { notify = true, postCurrent = false, requestWipers = true } = {}) {
    if (this.frozen === isFrozen) {
      if (this.frozen && postCurrent) {
        this.postCurrentWipers();
      }

      if (notify) {
        this.notifySettingsChange();
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
      if (requestWipers) {
        this.webView.postGetWipers();
      }
    }

    this.updateButton();

    if (notify) {
      this.notifySettingsChange();
    }
  }

  applySettings(settings) {
    if (!settings || typeof settings !== "object") {
      return;
    }

    if (settings.frozen !== undefined) {
      this.setFrozen(settings.frozen === true, { notify: false });
    }
  }

  getSettings() {
    return {
      frozen: this.frozen,
    };
  }

  notifySettingsChange() {
    this.onSettingsChange?.(this.getSettings());
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
