import { INK } from "../drawing.js";
import { formatVoltage } from "../voltage.js";
import { Shape } from "./Shape.js";
import { TextLabel } from "./TextLabel.js";

export class VoltageReadout extends Shape {
  constructor({
    color = INK,
    label = "OUT",
    position = [0, 0, 0],
  } = {}) {
    super({ name: "VoltageReadout", position });

    this.label = label;
    this.inputPort = this.addPort("input", [-0.48, 0], {
      direction: [-1, 0, 0],
      kind: "input",
    });
    this.readout = new TextLabel(this.formatReadout(null), {
      color,
      height: 0.38,
      renderOrder: 5,
      width: 1.65,
    });

    this.add(this.readout);
  }

  evaluateVoltage() {
    this.readout.setText(this.formatReadout(this.inputPort.voltage));
  }

  formatReadout(voltage) {
    return formatVoltage(voltage);
  }
}
