const HOLD_WIPERS_FLAG = 0x01;
const STATE_BITS = Object.freeze({
  ir1: 0b00000000000000000000000000000001,
  red1: 0b00000000000000010000000000000000,
});

export class StateControl {
  constructor({ buttons, freezeWipers, status, webView }) {
    this.buttons = Array.from(buttons ?? []);
    this.freezeWipers = freezeWipers;
    this.status = status;
    this.webView = webView;
    this.activeById = new Map(this.buttons.map((button) => [button.dataset.stateToggle, false]));

    this.buttons.forEach((button) => {
      button.addEventListener("click", () => this.toggle(button.dataset.stateToggle));
    });

    this.updateButtons();
  }

  toggle(id) {
    if (!this.activeById.has(id)) {
      return;
    }

    this.activeById.set(id, !this.activeById.get(id));
    this.updateButtons();
    this.sendHoldState();
  }

  sendHoldState() {
    const state = this.getState();

    this.freezeWipers.setFrozen(true);
    this.webView.postSetState({
      flags: 0, // HOLD_WIPERS_FLAG,
      state,
    });
    this.updateStatus(`sent ${formatStateHex(state)}`);
  }

  getState() {
    return Array.from(this.activeById)
      .reduce((state, [id, active]) => (
        active ? state | (STATE_BITS[id] ?? 0) : state
      ), 0) >>> 0;
  }

  updateButtons() {
    this.buttons.forEach((button) => {
      const isActive = this.activeById.get(button.dataset.stateToggle) === true;
      button.dataset.active = String(isActive);
    });
  }

  updateStatus(status) {
    if (this.status) {
      this.status.textContent = status;
    }
  }
}

function formatStateHex(state) {
  return `0x${state.toString(16).toUpperCase().padStart(8, "0")}`;
}
