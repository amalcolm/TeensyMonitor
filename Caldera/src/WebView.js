import { COMMAND_FLAGS } from "./helpers/CommandFlags.js";

const DEFAULT_HOST_CONFIG = {
  postSettingsChanges: false,
};

export class WebView {
  constructor(model) {
    this.model = model;
    this.hostConfig = {
      ...DEFAULT_HOST_CONFIG,
      ...window.calderaHost,
    };
    this.handlersByType = new Map();
    this.webview = window.chrome?.webview ?? null;

    window.calderaHost = this.hostConfig;

    this.handleMessage = this.handleMessage.bind(this);
    this.webview?.addEventListener("message", this.handleMessage);
  }

  on(type, handler) {
    if (!this.handlersByType.has(type)) {
      this.handlersByType.set(type, new Set());
    }

    const handlers = this.handlersByType.get(type);
    handlers.add(handler);

    return () => handlers.delete(handler);
  }

  postReady() {
    return this.postMessage({
      type: "ready",
    });
  }

  postSettingsChange(settings) {
    if (!this.hostConfig.postSettingsChanges) {
      return false;
    }

    return this.postMessage({
      type: "settingsChange",
      value: settings,
    });
  }

  postSetWipers(wipers, { flags = COMMAND_FLAGS.HOLD_WIPERS } = {}) {
    return this.postMessage({
      type: "setWipers",
      wipers,
      flags,
    });
  }

  postGetWipers() {
    return this.postMessage({
      type: "getWipers",
    });
  }

  postSetState({ flags = COMMAND_FLAGS.NONE, state }) {
    return this.postMessage({
      type: "setState",
      state,
      flags,
    });
  }

  postSetDebugFlags(debugFlags) {
    return this.postMessage({
      type: "setDebugFlags",
      debugFlags,
    });
  }

  postMessage(message) {
    if (!this.webview) {
      return false;
    }

    this.webview.postMessage(message);
    return true;
  }

  handleMessage(event) {
    const message = parseMessage(event.data);

    if (!message?.type) {
      return;
    }

    if (message.type === "hostConfig") {
      this.applyHostConfig(message);
    }

    const handlers = this.handlersByType.get(message.type);
    handlers?.forEach((handler) => handler(message));
  }

  applyHostConfig(message) {
    if ("postSettingsChanges" in message) {
      this.hostConfig.postSettingsChanges = message.postSettingsChanges === true;
    }
  }
}

function parseMessage(data) {
  if (data && typeof data === "object") {
    return data;
  }

  if (typeof data !== "string") {
    return null;
  }

  try {
    const parsed = JSON.parse(data);
    return parsed && typeof parsed === "object" ? parsed : null;
  } catch {
    return null;
  }
}
