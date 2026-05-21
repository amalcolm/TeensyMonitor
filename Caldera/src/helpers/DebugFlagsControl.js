import { COMMAND_FLAGS } from "./CommandFlags.js";

export const DEBUG_FLAGS = Object.freeze({
  NONE: 0,
  UPDATE: COMMAND_FLAGS.RUN_DEBUG_UPDATE,
});

const DEBUG_FLAG_BY_ID = Object.freeze({
  update: DEBUG_FLAGS.UPDATE,
});

const DEBUG_TEST_COMMAND_FLAG_BY_ID = Object.freeze({
  midOffset: COMMAND_FLAGS.RUN_TEST_MID_OFFSET,
});

export class DebugFlagsControl {
  constructor({
    inputs,
    status,
    testButtons,
    testStatus = null,
    webView,
  }) {
    this.inputs = Array.from(inputs ?? []);
    this.status = status;
    this.testButtons = Array.from(testButtons ?? []);
    this.testStatus = testStatus;
    this.webView = webView;

    this.inputs.forEach((input) => {
      input.addEventListener("change", () => this.sendDebugFlags());
    });

    this.testButtons.forEach((button) => {
      button.addEventListener("click", () => this.runDebugTest(button.dataset.debugTest));
    });

    this.updateStatus(this.getDebugFlags());
    this.updateTestStatus("ready");
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
    const cmdFlags = this.getDebugFlags();

    this.webView.postSetDebugFlags({ cmdFlags });
    this.updateStatus(cmdFlags);
  }

  updateStatus(debugFlags) {
    if (this.status) {
      this.status.textContent = formatDebugFlags(debugFlags);
    }
  }

  runDebugTest(id) {
    const commandFlags = DEBUG_TEST_COMMAND_FLAG_BY_ID[id] ?? COMMAND_FLAGS.NONE;

    if (commandFlags === COMMAND_FLAGS.NONE) {
      return;
    }

    const posted = this.webView.postSetDebugFlags({
      cmdFlags: this.getDebugFlags() | commandFlags,
    });

    this.updateTestStatus(posted ? `sent ${formatDebugFlags(commandFlags)}` : "offline");
  }

  updateTestStatus(text) {
    if (this.testStatus) {
      this.testStatus.textContent = text;
    }
  }
}

function formatDebugFlags(debugFlags) {
  return `0x${debugFlags.toString(16).toUpperCase().padStart(8, "0")}`;
}
