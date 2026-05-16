export const GROUND_VOLTAGE = 0;
export const SUPPLY_VOLTAGE = 3.3;
export const SENSOR_RAIL_MARGIN_V = 0.35;
export const VALID_SENSOR_MIN_V = GROUND_VOLTAGE + SENSOR_RAIL_MARGIN_V;
export const VALID_SENSOR_MAX_V = SUPPLY_VOLTAGE - SENSOR_RAIL_MARGIN_V;

export function clampVoltage(value, min = GROUND_VOLTAGE, max = SUPPLY_VOLTAGE) {
  if (!isKnownVoltage(value)) {
    return null;
  }

  return Math.min(Math.max(value, min), max);
}

export function formatVoltage(value) {  if (!isKnownVoltage(value)) return "? V";
  
  return `${value.toFixed(3)} V`;
}

export function isKnownVoltage(value) { return Number.isFinite(value); }

export function isValidSensorVoltage(value) {
  return isKnownVoltage(value)
    && value >= VALID_SENSOR_MIN_V
    && value <= VALID_SENSOR_MAX_V;
}
