import { COMPONENT_BLUE } from "../drawing.js";
import { Digipot } from "./Digipot.js";
import { PoweredDigipot } from "./PoweredDigipot.js";
import { Shape } from "./Shape.js";
import { STANDARD_OUTPUT_LEAD_LENGTH, Wire } from "./Wire.js";

export const DIGIPOT_OUTPUT_LEAD_LENGTH = STANDARD_OUTPUT_LEAD_LENGTH / 3;

export class ThreePot extends Shape {
  constructor({ color = COMPONENT_BLUE, model = null, position = [0, 0, 0] } = {}) {
    super({ name: "ThreePot", position });

    this.topPot = new PoweredDigipot({
      color,
      groundResistance: null,
      label: "top",
      model: model?.top ?? null,
      position: [0, 2.1, 0],
      supplyResistance: "22K",
    });
    this.botPot = new PoweredDigipot({
      color,
      groundResistance: null,
      label: "bot",
      model: model?.bot ?? null,
      position: [0, -2.1, 0],
      supplyResistance: "22K",
    });
    this.midPot = new Digipot({
      color,
      label: "mid",
      model: model?.mid ?? null,
      position: [1.4, 0, 0],
    });

    this.topDigipot = this.topPot.digipot;
    this.botDigipot = this.botPot.digipot;
    this.midDigipot = this.midPot;

    this.internalWires = [
      new Wire({
        from: this.topPot.port("output"),
        hideVoltageLabels: "start",
        outputLeadLength: DIGIPOT_OUTPUT_LEAD_LENGTH,
        to: this.midPot.port("topInput"),
      }),
      new Wire({
        from: this.botPot.port("output"),
        hideVoltageLabels: "start",
        outputLeadLength: DIGIPOT_OUTPUT_LEAD_LENGTH,
        to: this.midPot.port("bottomInput"),
      }),
    ];

    this.ports.set("output", this.midPot.port("wiper"));
    this.add(this.topPot, this.botPot, this.midPot, ...this.internalWires);
  }

  update() {
    this.topPot.update();
    this.botPot.update();
    this.internalWires.forEach((wire) => wire.update());
  }

  evaluateVoltage() {
    this.topPot.evaluateVoltage();
    this.botPot.evaluateVoltage();
    this.internalWires[0].setVoltage(this.topPot.port("output").voltage);
    this.internalWires[1].setVoltage(this.botPot.port("output").voltage);
    this.midPot.evaluateVoltage();
  }
}
