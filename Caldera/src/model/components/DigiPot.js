import { Digipot as DigiPotShape } from "../../scene/shapes/Digipot.js";
import { clampInt } from "../utils.js";
import { isKnownVoltage } from "../voltage.js";

export const DIGIPOT_MIN = 0;
export const DIGIPOT_MAX = 255;
export const DIGIPOT_MIDPOINT = 128;

export class DigiPot {
  constructor({
    createShape = false,
    max = DIGIPOT_MAX,
    min = DIGIPOT_MIN,
    onChange = null,
    shape = null,
    wiper,
    ...shapeOptions
  } = {}) {
    this.max = max;
    this.min = min;
    this.onChange = onChange;
    this.bottomInputVoltage = null;
    this.outputVoltage = null;
    this.shape = null;
    this.topInputVoltage = null;
    this.value = clampInt(wiper ?? shapeOptions.value ?? DIGIPOT_MIDPOINT, this.min, this.max);

    if (shape) {
      this.connectShape(shape);
    } else if (createShape) {
      this.shape = new DigiPotShape({
        ...shapeOptions,
        model: this,
        value: this.value,
      });
    }
  }

  get wiper() {
    return this.value;
  }

  get topVoltage() {
    return this.topInputVoltage;
  }

  get bottomVoltage() {
    return this.bottomInputVoltage;
  }

  get wiperVoltage() {
    return this.outputVoltage;
  }

  connectShape(shape) {
    this.shape = shape;

    if (shape.model !== this) {
      shape.setModel(this);
    } else {
      this.syncShape();
      this.syncShapeVoltages();
    }

    return this;
  }

  setInputVoltages({ bottom = this.bottomInputVoltage, top = this.topInputVoltage } = {}) {
    this.bottomInputVoltage = normaliseVoltage(bottom);
    this.topInputVoltage = normaliseVoltage(top);
    this.syncShapeVoltages();

    return this;
  }

  setOutputVoltage(voltage) {
    this.outputVoltage = normaliseVoltage(voltage);
    this.syncShapeVoltages();

    return this.outputVoltage;
  }

  setWiper(value, { emit = true, syncShape = true } = {}) {
    const previousValue = this.value;

    this.value = clampInt(value, this.min, this.max);

    if (syncShape) {
      this.syncShape();
    }

    if (emit && this.value !== previousValue) {
      this.emitChange(previousValue);
    }

    return this.value;
  }

  setWiperValue(value, options = {}) {
    return this.setWiper(value, options);
  }

  evaluateVoltage() {
    const topVoltage = this.topVoltage;
    const bottomVoltage = this.bottomVoltage;

    if (!isKnownVoltage(topVoltage) || !isKnownVoltage(bottomVoltage)) {
      this.setOutputVoltage(null);
    } else {
      const travel = this.value / this.max;
      this.setOutputVoltage(bottomVoltage + (topVoltage - bottomVoltage) * travel);
    }

    return this.outputVoltage;
  }

  syncShape() {
    this.shape?.syncWiperFromModel();
  }

  syncShapeVoltages() {
    if (!this.shape) {
      return;
    }

    this.shape.topInputPort.voltage = this.topInputVoltage;
    this.shape.bottomInputPort.voltage = this.bottomInputVoltage;
    this.shape.wiperPort.voltage = this.outputVoltage;
  }

  emitChange(previousValue) {
    this.onChange?.({
      model: this,
      previousValue,
      value: this.value,
    });
  }
}

function normaliseVoltage(value) {
  return isKnownVoltage(value) ? value : null;
}
