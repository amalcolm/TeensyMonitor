import { COMPONENT_BLUE, COMPONENT_STROKE_WIDTH, makeLine, makeLineLoop } from "../drawing.js";
import { SUPPLY_VOLTAGE, clampVoltage, isKnownVoltage } from "../voltage.js";
import { addPowerRailMarkers } from "./PowerRailMarkers.js";
import { RESISTOR_LEAD_HALF_WIDTH, Resistor } from "./Resistor.js";
import { Shape } from "./Shape.js";
import { Slider, formatSliderValue } from "./Slider.js";
import { TextLabel } from "./TextLabel.js";
import { VariableResistor } from "./VariableResistor.js";
import { STANDARD_OUTPUT_LEAD_LENGTH, Wire } from "./Wire.js";

const SOURCE_RESISTOR_CURRENT = 100e-6;

export class DifferentialAmp extends Shape {
  constructor({
    color = COMPONENT_BLUE,
    feedbackResistance = "10.0K",
    hasMultiplierInput = false,
    multiplier = 1,
    position = [0, 0, 0],
    scale = 1,
    sourceResistance = "1.0K",
  } = {}) {
    super({ name: "DifferentialAmp", position, scale });

    const height = 2.0;
    const width = height * Math.sqrt(3) / 2;

    const leftX = -width / 2;
    const signX = leftX * 0.75;
    const rightX = width / 2;
    const inputY = height / 2 * 0.6;
    const multiplierInputX = 0;
    const multiplierInputStartY = height / 4;
    const multiplierInputEndY = height / 2 + 0.16;
    const leadLength = STANDARD_OUTPUT_LEAD_LENGTH;
    const rightLeadLength = leadLength;
    const sourceResistorX = leftX - leadLength * 2 - RESISTOR_LEAD_HALF_WIDTH;
    const feedbackResistorX = leftX + leadLength;
    const variableResistorX = rightX  - RESISTOR_LEAD_HALF_WIDTH;
    const variableResistorY = height / 2 + 0.78;
    const feedbackSliderY = variableResistorY + 1.36;
    const verticalThenHorizontalRoute = ({ end, start }) => [start, [start.x, end.y, 0], end];
    const horizontalThenVerticalRoute = ({ end, start }) => [start, [end.x, start.y, 0], end];

    this.hasMultiplierInput = hasMultiplierInput;
    this.multiplier = multiplier;
    this.opAmpInvertingPort = this.addPort("opAmpInverting", [leftX - leadLength, inputY], {
      kind: "input",
      signal: "negative",
    });
    this.nonInvertingPort = this.addPort("nonInverting", [leftX - leadLength, -inputY], {
      kind: "input",
      signal: "positive",
    });
    this.outputPort = this.addPort("output", [rightX + rightLeadLength, 0], { kind: "output" });
    this.multiplierInputPort = hasMultiplierInput
      ? this.addPort("multiplier", [multiplierInputX, multiplierInputEndY], {
        direction: [0, 1, 0],
        kind: "input",
        signal: "multiplier",
      })
      : null;
    this.sourceResistor = new Resistor({
      color,
      position: [sourceResistorX, inputY, 0],
      value: sourceResistance,
    });
    this.feedbackResistor = new Resistor({
      color,
      inputSide: "right",
      position: [feedbackResistorX, variableResistorY, 0],
      value: "1K2",
    });
    this.variableResistor = new VariableResistor({
      color,
      inputSide: "right",
      position: [variableResistorX, variableResistorY, 0],
      value: feedbackResistance,
    });
    this.feedbackSlider = new Slider({
      color,
      label: "gain",
      leftValue: 0,
      position: [variableResistorX, feedbackSliderY, 0],
      rightValue: 255,
      signal: "wiper",
      value: 255,
      wiperHalfWidth: 0.14,
    });
    this.feedbackOutputWire = new Wire({
      from: this.outputPort,
      route: verticalThenHorizontalRoute,
      to: this.variableResistor.port("input"),
    });
    this.feedbackControlWire = new Wire({
      formatValue: formatSliderValue,
      from: this.feedbackSlider.port("output"),
      hideVoltageLabel: true,
      route: verticalThenHorizontalRoute,
      to: this.variableResistor.port("topInput"),
    });
    this.feedbackReturnWire = new Wire({
      from: this.feedbackResistor.port("output"),
      propagateVoltage: false,
      route: horizontalThenVerticalRoute,
      to: this.opAmpInvertingPort,
    });
    this.feedbackJoinWire = new Wire({
      from: this.feedbackResistor.port("input"),
      hideVoltageLabel: true,
      propagateVoltage: false,
      to: this.variableResistor.port("output"),
    });
    this.internalWires = [
      this.feedbackOutputWire,
      this.feedbackControlWire,
      this.feedbackReturnWire,
      this.feedbackJoinWire,
    ];
    this.invertingPort = this.sourceResistor.port("input");
    this.ports.set("inverting", this.invertingPort);

    this.add(makeLineLoop([[leftX, height / 2.0], [rightX, 0], [leftX, -height / 2.0]], {
      color,
      width: COMPONENT_STROKE_WIDTH,
    }));
    this.add(makeLine([[leftX - leadLength, inputY], [leftX, inputY]], { color, width: COMPONENT_STROKE_WIDTH }));
    this.add(makeLine([[leftX - leadLength, -inputY], [leftX, -inputY]], { color, width: COMPONENT_STROKE_WIDTH }));
    this.add(makeLine([[rightX, 0], [rightX + rightLeadLength, 0]], { color, width: COMPONENT_STROKE_WIDTH }));
    this.add(makeLine([[sourceResistorX + RESISTOR_LEAD_HALF_WIDTH, inputY], [leftX - leadLength, inputY]], { color, width: COMPONENT_STROKE_WIDTH }));
    this.addPlusSign(signX, -inputY, color);
    this.addMinusSign(signX, inputY, color);
    if (hasMultiplierInput) {
      this.add(makeLine([
        [multiplierInputX, multiplierInputStartY],
        [multiplierInputX, multiplierInputEndY],
      ], { color, width: COMPONENT_STROKE_WIDTH }));
    }
    this.multiplierLabel = new TextLabel(this.formatMultiplierLabel(this.getEffectiveMultiplier(1)), {
      color,
      height: 0.18,
      position: [0.02, 0, 0],
      width: 0.48,
    });
    this.powerRailMarkers = addPowerRailMarkers(this, {
      bottomY: -height / 4,
      color,
      topY: height / 4,
    });
    this.add(
      this.sourceResistor,
      this.feedbackResistor,
      this.variableResistor,
      this.feedbackSlider,
      ...this.internalWires,
      this.multiplierLabel,
    );
    this.updateMultiplierLabel(1);
  }

  addPlusSign(x, y, color) {
    const size = 0.13;

    this.add(makeLine([[x - size, y], [x + size, y]], { color, width: COMPONENT_STROKE_WIDTH }));
    this.add(makeLine([[x, y - size], [x, y + size]], { color, width: COMPONENT_STROKE_WIDTH }));
  }

  addMinusSign(x, y, color) {
    const size = 0.13;

    this.add(makeLine([[x - size, y], [x + size, y]], { color, width: COMPONENT_STROKE_WIDTH }));
  }

  update() {
    this.internalWires.forEach((wire) => wire.update());
  }

  evaluateVoltage() {
    this.updateVariableResistance();

    const nonInvertingVoltage = this.nonInvertingPort.voltage;
    const sourceVoltage = this.sourceResistor.port("input").voltage;
    const sourceResistorDrop = this.getSourceResistorDrop();
    const invertingVoltage = isKnownVoltage(sourceVoltage) && isKnownVoltage(sourceResistorDrop)
      ? sourceVoltage - sourceResistorDrop
      : null;
    const secondaryMultiplier = this.multiplierInputPort?.voltage ?? 1;
    const effectiveMultiplier = this.getEffectiveMultiplier(secondaryMultiplier);

    this.setSummingNodeVoltage(invertingVoltage);
    this.updateMultiplierLabel(secondaryMultiplier);

    if (
      !isKnownVoltage(nonInvertingVoltage)
      || !isKnownVoltage(invertingVoltage)
      || !isKnownVoltage(effectiveMultiplier)
    ) {
      this.outputPort.voltage = null;
      this.updateFeedbackVoltage();
      return;
    }

    this.outputPort.voltage = clampVoltage(
      SUPPLY_VOLTAGE / 2 + (nonInvertingVoltage - invertingVoltage) * effectiveMultiplier,
    );
    this.updateFeedbackVoltage();
  }

  setSummingNodeVoltage(voltage) {
    this.sourceResistor.port("output").voltage = voltage;
    this.opAmpInvertingPort.voltage = voltage;
    this.feedbackResistor.port("output").voltage = voltage;
  }

  updateVariableResistance() {
    this.feedbackSlider.evaluateVoltage();
    this.feedbackControlWire.setVoltage(this.feedbackSlider.port("output").voltage);
    this.variableResistor.updateResistanceFromControl();
  }

  updateFeedbackVoltage() {
    this.feedbackOutputWire.setVoltage(this.outputPort.voltage);

    const summingNodeVoltage = this.opAmpInvertingPort.voltage;
    const feedbackJoinVoltage = this.getFeedbackJoinVoltage();

    this.feedbackReturnWire.setVoltage(summingNodeVoltage);
    this.variableResistor.port("output").voltage = feedbackJoinVoltage;
    this.feedbackResistor.port("input").voltage = feedbackJoinVoltage;
    this.feedbackJoinWire.setVoltage(feedbackJoinVoltage);
  }

  getFeedbackJoinVoltage() {
    const outputVoltage = this.outputPort.voltage;
    const summingNodeVoltage = this.opAmpInvertingPort.voltage;
    const variableFeedbackResistance = this.variableResistor.ohms;
    const fixedFeedbackResistance = this.feedbackResistor.ohms;
    const feedbackResistance = this.getFeedbackResistance();

    if (
      !isKnownVoltage(outputVoltage)
      || !isKnownVoltage(summingNodeVoltage)
      || !Number.isFinite(variableFeedbackResistance)
      || !Number.isFinite(fixedFeedbackResistance)
      || !Number.isFinite(feedbackResistance)
      || feedbackResistance === 0
    ) {
      return null;
    }

    return outputVoltage
      + (summingNodeVoltage - outputVoltage) * (variableFeedbackResistance / feedbackResistance);
  }

  updateMultiplierLabel(secondaryMultiplier) {
    const effectiveMultiplier = this.getEffectiveMultiplier(secondaryMultiplier);

    if (!isKnownVoltage(effectiveMultiplier)) {
      this.multiplierLabel.setText("x?");
      return;
    }

    this.multiplierLabel.setText(this.formatMultiplierLabel(effectiveMultiplier));
  }

  getEffectiveMultiplier(secondaryMultiplier) {
    const resistanceMultiplier = this.getResistanceMultiplier();

    if (!isKnownVoltage(resistanceMultiplier) || !isKnownVoltage(secondaryMultiplier)) {
      return null;
    }

    return resistanceMultiplier * this.multiplier * secondaryMultiplier;
  }

  getResistanceMultiplier() {
    const sourceResistance = this.sourceResistor.ohms;
    const feedbackResistance = this.getFeedbackResistance();

    if (!Number.isFinite(sourceResistance) || sourceResistance === 0 || !Number.isFinite(feedbackResistance)) {
      return null;
    }

    return feedbackResistance / sourceResistance;
  }

  getSourceResistorDrop() {
    const sourceResistance = this.sourceResistor.ohms;

    if (!Number.isFinite(sourceResistance)) {
      return null;
    }

    return sourceResistance * SOURCE_RESISTOR_CURRENT;
  }

  getFeedbackResistance() {
    const fixedFeedbackResistance = this.feedbackResistor.ohms;
    const variableFeedbackResistance = this.variableResistor.ohms;

    if (!Number.isFinite(fixedFeedbackResistance) || !Number.isFinite(variableFeedbackResistance)) {
      return null;
    }

    return fixedFeedbackResistance + variableFeedbackResistance;
  }

  formatMultiplierLabel(value) {
    if (!isKnownVoltage(value)) {
      return "x?";
    }

    const formattedValue = Number.isInteger(value)
      ? value.toFixed(0)
      : value.toFixed(2).replace(/\.?0+$/, "");

    return `x${formattedValue}`;
  }
}
