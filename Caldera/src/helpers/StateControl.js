import { COMMAND_FLAGS } from "./CommandFlags.js";

export const STATE_LED_ROWS = Object.freeze([
  makeStateLedRow("red", "RED", 16),
  makeStateLedRow("ir", "IR", 0),
]);

const STATE_BITS = Object.freeze(
  Object.fromEntries(
    STATE_LED_ROWS.flatMap((row) => row.map(({ bit, id }) => [id, bit])),
  ),
);

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
      flags: COMMAND_FLAGS.HOLD_WIPERS,
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

function makeStateLedRow(kind, labelPrefix, bitOffset) {
  return Object.freeze(
    Array.from({ length: 8 }, (_, index) => {
      const channel = index + 1;

      return Object.freeze({
        bit: 1 << (bitOffset + index),
        id: `${kind}${channel}`,
        kind,
        label: `${labelPrefix}${channel}`,
      });
    }),
  );
}
