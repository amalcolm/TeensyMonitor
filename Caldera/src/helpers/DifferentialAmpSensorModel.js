import { isKnownVoltage } from "../model/voltage.js";
import { DIGIPOT_MAX, getPoweredDigipotTerminalVoltages } from "../model/components/DigiPot.js";
import { OFFSET_RAILS } from "../model/Model.js";

const DEFAULT_OFFSET_TERMINALS = getPoweredDigipotTerminalVoltages(OFFSET_RAILS);
const DEFAULT_OFFSET_LOW_V = DEFAULT_OFFSET_TERMINALS.bottom;
const DEFAULT_OFFSET_HIGH_V = DEFAULT_OFFSET_TERMINALS.top;
const DEFAULT_OFFSET_TRIM_V = 0.08195121809338102;
const DEFAULT_WIPER_MIN = 0;
const DEFAULT_WIPER_MAX = DIGIPOT_MAX;
const DEFAULT_FIXED_GAIN_RATIO = 1.825648453550745;
const DEFAULT_GAIN_RATIO_PER_WIPER = 0.39590669654443916;
const DEFAULT_VARIABLE_GAIN_RATIO = DEFAULT_GAIN_RATIO_PER_WIPER * DEFAULT_WIPER_MAX;

export class DifferentialAmpSensorModel {
  constructor({
    fixedGainRatio = DEFAULT_FIXED_GAIN_RATIO,
    offsetHighV = DEFAULT_OFFSET_HIGH_V,
    offsetLowV = DEFAULT_OFFSET_LOW_V,
    offsetTrimV = DEFAULT_OFFSET_TRIM_V,
    variableGainRatio = DEFAULT_VARIABLE_GAIN_RATIO,
    wiperMax = DEFAULT_WIPER_MAX,
    wiperMin = DEFAULT_WIPER_MIN,
  } = {}) {
    this.fixedGainRatio = fixedGainRatio;
    this.offsetHighV = offsetHighV;
    this.offsetLowV = offsetLowV;
    this.offsetTrimV = offsetTrimV;
    this.variableGainRatio = variableGainRatio;
    this.wiperMax = wiperMax;
    this.wiperMin = wiperMin;
  }

  getSensorErrorVoltages(model) {
    const readings = this.getSensorErrorReadings(model);

    return {
      sensor1: readings.sensor1.errorVoltage,
      sensor2: readings.sensor2.errorVoltage,
    };
  }

  getSensorErrorReadings(model) {
    const gainWiper = model.gain?.wiper;
    const offsetWiper = model.offset?.wiper;
    const sensor1Voltage = model.sensor1Voltage;
    const sensor2Voltage = model.sensor2Voltage;
    const sensor1PredictedVoltage = this.sensor1FromSensor2(
      sensor2Voltage,
      gainWiper,
      offsetWiper,
    );
    const sensor2PredictedVoltage = this.sensor2FromSensor1(
      sensor1Voltage,
      gainWiper,
      offsetWiper,
    );

    return {
      sensor1: this.getSensorErrorReading(sensor1PredictedVoltage, sensor1Voltage),
      sensor2: this.getSensorErrorReading(sensor2PredictedVoltage, sensor2Voltage),
    };
  }

  getSensorErrorReading(modeledVoltage, measuredVoltage) {
    return {
      errorVoltage: this.getSensorErrorVoltage(modeledVoltage, measuredVoltage),
      predictedVoltage: modeledVoltage,
    };
  }

  getSensorErrorVoltage(modeledVoltage, measuredVoltage) {
    if (!isKnownVoltage(modeledVoltage) || !isKnownVoltage(measuredVoltage)) {
      return null;
    }

    return modeledVoltage - measuredVoltage;
  }

  offsetVoltageFromWiper(offsetWiper) {
    offsetWiper = this.clampWiper(Number(offsetWiper));

    return this.offsetLowV
      + offsetWiper * (this.offsetHighV - this.offsetLowV) / this.wiperMax
      + this.offsetTrimV;
  }

  gainRatioFromWiper(gainWiper) {
    gainWiper = this.clampWiper(Number(gainWiper));

    return this.fixedGainRatio + gainWiper * this.variableGainRatio / this.wiperMax;
  }

  sensor2FromSensor1(sensor1V, gainWiper, offsetWiper) {
    if (!isKnownVoltage(sensor1V)) {
      return null;
    }

    const offsetV = this.offsetVoltageFromWiper(offsetWiper);
    const gainRatio = this.gainRatioFromWiper(gainWiper);

    return offsetV + gainRatio * (offsetV - sensor1V);
  }

  sensor1FromSensor2(sensor2V, gainWiper, offsetWiper) {
    if (!isKnownVoltage(sensor2V)) {
      return null;
    }

    const offsetV = this.offsetVoltageFromWiper(offsetWiper);
    const gainRatio = this.gainRatioFromWiper(gainWiper);

    return offsetV - (sensor2V - offsetV) / gainRatio;
  }

  clampWiper(value) {
    return Math.max(this.wiperMin, Math.min(this.wiperMax, value));
  }
}
