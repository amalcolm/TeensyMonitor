export const DEBUG_FLAGS = Object.freeze({
  NONE: 0,
  UPDATE: 0x01,
});

const DEBUG_FLAG_BY_ID = Object.freeze({
  update: DEBUG_FLAGS.UPDATE,
});

export class DebugFlagsControl {
  constructor({ inputs, status, webView }) {
    this.inputs = Array.from(inputs ?? []);
    this.status = status;
    this.webView = webView;

    this.inputs.forEach((input) => {
      input.addEventListener("change", () => this.sendDebugFlags());
    });

    this.updateStatus(this.getDebugFlags());
  }

  getDebugFlags() {
    return this.inputs.reduce((flags, input) => {
      if (!input.checked) {
        return flags;
      }

      return flags | (DEBUG_FLAG_BY_ID[input.dataset.debugFlag] ?? DEBUG_FLAGS.NONE);
    }, DEBUG_FLAGS.NONE) >>> 0;
  }

  sendDebugFlags() {
    const debugFlags = this.getDebugFlags();

    this.webView.postSetDebugFlags(debugFlags);
    this.updateStatus(debugFlags);
  }

  updateStatus(debugFlags) {
    if (this.status) {
      this.status.textContent = formatDebugFlags(debugFlags);
    }
  }
}

function formatDebugFlags(debugFlags) {
  return `0x${debugFlags.toString(16).toUpperCase().padStart(8, "0")}`;
}
