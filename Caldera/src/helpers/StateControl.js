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

const ACTIVE_CLASS = "state-panel__button--active";
const RED_ACTIVE_CLASS = "state-panel__button--red-active";
const IR_ACTIVE_CLASS = "state-panel__button--ir-active";

export class StateControl {
  constructor({
    buttons,
    freezeWipers,
    freezeWipersOnLedChangeInput = null,
    initialFreezeWipersOnLedChange = true,
    initialSetSearchPhaseOnStateChange = false,
    onSettingsChange = null,
    setSearchPhaseInput = null,
    webView,
  }) {
    this.buttons = Array.from(buttons ?? []);
    this.freezeWipers = freezeWipers;
    this.freezeWipersOnLedChangeInput = freezeWipersOnLedChangeInput;
    this.freezeWipersOnLedChange = initialFreezeWipersOnLedChange !== false;
    this.setSearchPhaseInput = setSearchPhaseInput;
    this.setSearchPhaseOnStateChange = initialSetSearchPhaseOnStateChange === true;
    this.onSettingsChange = onSettingsChange;
    this.webView = webView;
    this.activeById = new Map(this.buttons.map((button) => [button.dataset.stateToggle, false]));
    this.lastHostState = null;
    this.pendingState = null;

    this.buttons.forEach((button) => {
      button.addEventListener("click", (event) => (
        this.handleManualToggle(button.dataset.stateToggle, event)
      ));
    });

    this.freezeWipersOnLedChangeInput?.addEventListener("change", () => {
      this.freezeWipersOnLedChange = this.freezeWipersOnLedChangeInput.checked;
      this.updateFreezeWipersOnLedChangeInput();
      this.onSettingsChange?.();
    });

    this.setSearchPhaseInput?.addEventListener("change", () => {
      this.setSearchPhaseOnStateChange = this.setSearchPhaseInput.checked;
      this.updateSetSearchPhaseInput();
      this.onSettingsChange?.();
    });

    this.updateButtons();
    this.updateFreezeWipersOnLedChangeInput();
    this.updateSetSearchPhaseInput();
  }

  handleManualToggle(id, event = null) {
    event?.preventDefault();
    event?.currentTarget?.blur?.();
    this.sendHoldState(this.getToggledState(id), {
      freezeWipers: this.freezeWipersOnLedChange,
    });
  }

  applyHostState(state) {
    const hostState = normaliseState(state);

    if (hostState === null) {
      return false;
    }

    if (hostState === this.pendingState) {
      this.pendingState = null;
    }

    const changed = hostState !== this.lastHostState;

    this.lastHostState = hostState;
    this.setState(hostState);

    return changed;
  }

  setState(state) {
    this.activeById.forEach((_, id) => {
      this.activeById.set(id, Boolean(state & (STATE_BITS[id] ?? 0)));
    });

    this.updateButtons();
  }

  setActiveIds(activeIds, { send = true } = {}) {
    const state = getStateForActiveIds(activeIds);

    if (!send) {
      this.setState(state);
      return state;
    }

    return this.sendHoldState(state);
  }

  sendHoldState(state = this.getState(), { freezeWipers = true } = {}) {
    const holdState = normaliseState(state) ?? 0;
    const shouldSetSearchPhase = this.setSearchPhaseOnStateChange;
    const shouldFreezeWipers = !shouldSetSearchPhase && freezeWipers === true;

    if (shouldSetSearchPhase && this.freezeWipers.frozen) {
      this.freezeWipers.setFrozen(false, { requestWipers: false });
    }

    if (shouldFreezeWipers) {
      this.freezeWipers.setFrozen(true);
    }

    const shouldHoldWipers = !shouldSetSearchPhase && this.freezeWipers.frozen;
    const flags = (
      (shouldHoldWipers ? COMMAND_FLAGS.HOLD_WIPERS : COMMAND_FLAGS.NONE)
      | (shouldSetSearchPhase ? COMMAND_FLAGS.SET_SEARCH_PHASE : COMMAND_FLAGS.NONE)
    );

    const posted = this.webView.postSetState({
      cmdFlags: flags,
      state: holdState,
    });

    this.pendingState = posted ? holdState : null;

    if (!posted) {
      this.lastHostState = holdState;
      this.setState(holdState);
    }

    return holdState;
  }

  getState() {
    return Array.from(this.activeById)
      .reduce((state, [id, active]) => (
        active ? state | (STATE_BITS[id] ?? 0) : state
      ), 0) >>> 0;
  }

  getLastHostState() {
    return this.lastHostState;
  }

  getSettings() {
    return {
      freezeWipersOnLedChange: this.freezeWipersOnLedChange,
      setSearchPhaseOnStateChange: this.setSearchPhaseOnStateChange,
    };
  }

  getToggledState(id) {
    const bit = STATE_BITS[normaliseStateId(id)] ?? 0;
    const baseState = this.pendingState
      ?? this.lastHostState
      ?? this.getState();

    return (baseState ^ bit) >>> 0;
  }

  updateButtons() {
    this.buttons.forEach((button) => {
      const isActive = this.activeById.get(button.dataset.stateToggle) === true;

      button.dataset.active = String(isActive);
      button.setAttribute("aria-pressed", String(isActive));
      button.classList.toggle(ACTIVE_CLASS, isActive);
      button.classList.toggle(
        RED_ACTIVE_CLASS,
        isActive && button.dataset.stateKind === "red",
      );
      button.classList.toggle(
        IR_ACTIVE_CLASS,
        isActive && button.dataset.stateKind === "ir",
      );
    });
  }

  updateFreezeWipersOnLedChangeInput() {
    if (!this.freezeWipersOnLedChangeInput) {
      return;
    }

    this.freezeWipersOnLedChangeInput.checked = this.freezeWipersOnLedChange;
  }

  updateSetSearchPhaseInput() {
    if (!this.setSearchPhaseInput) {
      return;
    }

    this.setSearchPhaseInput.checked = this.setSearchPhaseOnStateChange;
  }
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

function normaliseState(value) {
  const state = Number(value);

  return Number.isFinite(state) && state >= 0
    ? Math.trunc(state) >>> 0
    : null;
}

function normaliseStateId(id) {
  return String(id ?? "").trim().toLowerCase();
}

function getStateForActiveIds(activeIds) {
  const activeSet = new Set(
    Array.from(activeIds ?? [], normaliseStateId),
  );

  return Object.entries(STATE_BITS)
    .reduce((state, [id, bit]) => (
      activeSet.has(normaliseStateId(id)) ? state | bit : state
    ), 0) >>> 0;
}
