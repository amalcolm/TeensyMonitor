import { GainSweep, OffsetSweep, Sweep, Test1Sweep } from "../helpers/Sweep.js";

export const testWipers = Object.freeze({ top: 73, bot: 61, mid: 255, offset: 0, gain: 4 });
export const test1Wipers = testWipers;
export const LEDsToTest = Object.freeze(["RED1", "IR2"]);

export const TEST_PANEL_HTML = `
  <div class="test-panel" data-test-panel>
    <div class="test-panel__header">
      <span>Analysis triggers</span>
      <span data-test-status>idle</span>
    </div>
    <div class="test-panel__groups">
      <section class="test-panel__group">
        <h2 class="test-panel__group-title">Single</h2>
        <div class="test-panel__actions">
          <button class="test-panel__button" type="button" data-mid-sweep-button>
            Sweep mid
          </button>
        </div>
      </section>
      <section class="test-panel__group">
        <h2 class="test-panel__group-title">Stacked</h2>
        <div class="test-panel__actions">
          <button class="test-panel__button" type="button" data-offset-sweep-button>
            Sweep offset
          </button>
          <button class="test-panel__button" type="button" data-gain-sweep-button>
            Sweep gain
          </button>
        </div>
      </section>
      <section class="test-panel__group">
        <h2 class="test-panel__group-title">Custom</h2>
        <div class="test-panel__actions">
          <button class="test-panel__button" type="button" data-test1-button>
            Test1
          </button>
        </div>
      </section>
    </div>
  </div>
`;

export class TestPanel {
  constructor({
    analysisPanel,
    circuitScene,
    freezeVoltages,
    freezeWipers,
    getHardwareWiperRevision = null,
    getHardwareWipers = null,
    model,
    requireWiperAck = false,
    root,
    setLedState = null,
    updateWiperDebug,
    webView,
  }) {
    this.analysisPanel = analysisPanel;
    this.root = root;
    this.status = root?.querySelector("[data-test-status]");
    this.sweeps = [];

    const commonOptions = {
      circuitScene,
      freezeVoltages,
      freezeWipers,
      getHardwareWiperRevision,
      getHardwareWipers,
      model,
      onSample: (sampleContext) => this.analysisPanel.addSampleFromModel(sampleContext),
      requireWiperAck,
      status: this.status,
      updateWiperDebug,
      webView,
    };

    this.midSweep = new Sweep({
      ...commonOptions,
      button: root?.querySelector("[data-mid-sweep-button]"),
      onClear: () => this.analysisPanel.clear(),
      onStart: () => this.stopOtherSweeps(this.midSweep),
    });
    this.offsetSweep = new OffsetSweep({
      ...commonOptions,
      button: root?.querySelector("[data-offset-sweep-button]"),
      onClear: () => this.analysisPanel.clear(),
      onStart: () => this.stopOtherSweeps(this.offsetSweep),
    });
    this.gainSweep = new GainSweep({
      ...commonOptions,
      button: root?.querySelector("[data-gain-sweep-button]"),
      onClear: () => this.analysisPanel.clear({ panel: "gain" }),
      onStart: () => this.stopOtherSweeps(this.gainSweep),
    });
    this.test1Sweep = new Test1Sweep({
      ...commonOptions,
      button: root?.querySelector("[data-test1-button]"),
      ledsToTest: LEDsToTest,
      onClear: () => this.analysisPanel.clear({ panel: "test1" }),
      onStart: () => this.stopOtherSweeps(this.test1Sweep),
      setLedState,
      testWipers,
    });
    this.sweeps = [this.midSweep, this.offsetSweep, this.gainSweep, this.test1Sweep];
  }

  stopOtherSweeps(activeSweep) {
    this.sweeps.forEach((sweep) => {
      if (sweep && sweep !== activeSweep) {
        sweep.stop("idle");
      }
    });
  }
}
