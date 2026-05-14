import { isKnownVoltage } from "../model/voltage.js";

const DEFAULT_PLUS_WIDTH = 0.082;
const DEFAULT_MINUS_WIDTH = 0.056;

export class SensorErrorReadouts {
  constructor({
    minusWidth = DEFAULT_MINUS_WIDTH,
    plusWidth = DEFAULT_PLUS_WIDTH,
    unknownText = "? V",
  } = {}) {
    this.minusWidth = minusWidth;
    this.plusWidth = plusWidth;
    this.readoutById = new Map();
    this.baseXById = new Map();
    this.unknownText = unknownText;

    this.formatSignedVoltage = this.formatSignedVoltage.bind(this);
  }

  register(id, readout, baseX) {
    this.readoutById.set(id, readout);
    this.baseXById.set(id, baseX);

    return readout;
  }

  getPosition(baseX, y, { value = 0, z = 0 } = {}) {
    return [this.getX(baseX, value), y, z];
  }

  setVoltage(id, voltage) {
    const readout = this.readoutById.get(id);
    const baseX = this.baseXById.get(id);

    if (!readout) {
      return;
    }

    if (Number.isFinite(baseX)) {
      readout.position.x = this.getX(baseX, voltage);
    }

    readout.setDisplayVoltage(voltage);
  }

  getX(baseX, value) {
    return baseX - this.getPrefixWidth(value);
  }

  getPrefixWidth(value) {
    return Number(value) < 0
      ? this.minusWidth
      : this.plusWidth;
  }

  formatSignedVoltage(value) {
    if (!isKnownVoltage(value)) {
      return this.unknownText;
    }

    return `${value >= 0 ? "+" : ""}${value.toFixed(3)} V`;
  }
}
