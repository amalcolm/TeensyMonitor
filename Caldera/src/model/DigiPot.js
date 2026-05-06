import { Digipot as DigiPotShape } from "../scene/shapes/Digipot.js";
import { isKnownVoltage } from "../scene/voltage.js";

export const DIGIPOT_MIN = 0;
export const DIGIPOT_MAX = 255;
export const DIGIPOT_MIDPOINT = 128;

export class DigiPot {
  constructor({
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
    this.outputVoltage = null;
    this.shape = null;
    this.value = clampInt(wiper ?? shapeOptions.value ?? DIGIPOT_MIDPOINT, this.min, this.max);

    if (shape) {
      this.connectShape(shape);
    } else {
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
    return this.shape?.topInputPort.voltage ?? null;
  }

  get bottomVoltage() {
    return this.shape?.bottomInputPort.voltage ?? null;
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
    }

    return this;
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
      this.outputVoltage = null;
    } else {
      const travel = this.value / this.max;
      this.outputVoltage = bottomVoltage + (topVoltage - bottomVoltage) * travel;
    }

    if (this.shape) {
      this.shape.wiperPort.voltage = this.outputVoltage;
    }

    return this.outputVoltage;
  }

  syncShape() {
    this.shape?.syncWiperFromModel();
  }

  emitChange(previousValue) {
    this.onChange?.({
      model: this,
      previousValue,
      value: this.value,
    });
  }
}

export function clampInt(value, min, max) {
  return Math.min(Math.max(Math.round(value), min), max);
}
