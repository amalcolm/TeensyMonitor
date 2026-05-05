export const POT_MIN = 0;
export const POT_MAX = 255;
export const POT_MIDPOINT = 128;

export class DigiPotModel {
  constructor({ digipot = null, level = POT_MIN, max = POT_MAX, min = POT_MIN } = {}) {
    this.digipot = digipot;
    this.level = POT_MIN;
    this.max = max;
    this.min = min;
    this.inverted = false;

    this.setLevel(level);
  }

  begin(level = POT_MIDPOINT) {
    this.setLevel(level);
  }

  connectDigipot(digipot) {
    this.digipot = digipot;
    this.writeCurrentToPot();
  }

  getLevel() {
    return this.level;
  }

  getHardwareLevel() {
    return this.inverted ? POT_MAX - this.level : this.level;
  }

  invert() {
    this.inverted = true;
  }

  offsetLevel(delta) {
    this.setLevel(this.level + delta);
  }

  setLevel(level) {
    this.level = clampInt(level, this.min, this.max);
    this.writeCurrentToPot();
  }

  writeCurrentToPot() {
    this.digipot?.setWiperValue(this.level);
  }
}

export function clampInt(value, min, max) {
  return Math.min(Math.max(Math.round(value), min), max);
}
