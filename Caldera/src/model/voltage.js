export const SUPPLY_VOLTAGE = 3.3;

export function clampVoltage(value, min = 0, max = SUPPLY_VOLTAGE) {
  if (!isKnownVoltage(value)) {
    return null;
  }

  return Math.min(Math.max(value, min), max);
}

export function formatVoltage(value) {
  if (!isKnownVoltage(value)) {
    return "? V";
  }

  return `${value.toFixed(3)} V`;
}

export function isKnownVoltage(value) {
  return Number.isFinite(value);
}
