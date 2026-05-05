import { COMPONENT_BLUE } from "../drawing.js";
import { Digipot } from "./Digipot.js";
import { PoweredDigipot } from "./PoweredDigipot.js";
import { Shape } from "./Shape.js";
import { Wire } from "./Wire.js";

export class ThreePot extends Shape {
  constructor({ color = COMPONENT_BLUE, position = [0, 0, 0] } = {}) {
    super({ name: "ThreePot", position });

    this.topPot = new PoweredDigipot({
      color,
      groundResistance: null,
      label: "top",
      position: [0, 2.1, 0],
      supplyResistance: "10K",
    });
    this.botPot = new PoweredDigipot({
      color,
      groundResistance: null,
      label: "bot",
      position: [0, -2.1, 0],
      supplyResistance: "10K",
    });
    this.midPot = new Digipot({ color, label: "mid", position: [1.4, 0, 0] });

    this.topDigipot = this.topPot.digipot;
    this.botDigipot = this.botPot.digipot;
    this.midDigipot = this.midPot;

    this.internalWires = [
      new Wire({ from: this.topPot.port("output"), to: this.midPot.port("topInput") }),
      new Wire({ from: this.botPot.port("output"), to: this.midPot.port("bottomInput") }),
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
