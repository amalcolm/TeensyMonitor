import { isKnownVoltage } from "../model/voltage.js";
import {
  DEFAULT_FIXED_FEEDBACK_RESISTANCE_OHMS,
  DEFAULT_SOURCE_RESISTANCE_OHMS,
  DEFAULT_VARIABLE_FEEDBACK_RESISTANCE_OHMS,
} from "../model/components/DifferentialAmp.js";
import { DIGIPOT_MAX, getPoweredDigipotTerminalVoltages } from "../model/components/DigiPot.js";
import { OFFSET_RAILS } from "../model/Model.js";

const DEFAULT_OFFSET_TERMINALS = getPoweredDigipotTerminalVoltages(OFFSET_RAILS);
const DEFAULT_OFFSET_LOW_V = DEFAULT_OFFSET_TERMINALS.bottom;
const DEFAULT_OFFSET_HIGH_V = DEFAULT_OFFSET_TERMINALS.top;
const DEFAULT_WIPER_MIN = 0;
const DEFAULT_WIPER_MAX = DIGIPOT_MAX;
const DEFAULT_FIXED_GAIN_RATIO = DEFAULT_FIXED_FEEDBACK_RESISTANCE_OHMS
  / DEFAULT_SOURCE_RESISTANCE_OHMS;
const DEFAULT_VARIABLE_GAIN_RATIO = DEFAULT_VARIABLE_FEEDBACK_RESISTANCE_OHMS
  / DEFAULT_SOURCE_RESISTANCE_OHMS;

export class DifferentialAmpSensorModel {
  constructor({
    fixedGainRatio = DEFAULT_FIXED_GAIN_RATIO,
    offsetHighV = DEFAULT_OFFSET_HIGH_V,
    offsetLowV = DEFAULT_OFFSET_LOW_V,
    variableGainRatio = DEFAULT_VARIABLE_GAIN_RATIO,
    wiperMax = DEFAULT_WIPER_MAX,
    wiperMin = DEFAULT_WIPER_MIN,
  } = {}) {
    this.fixedGainRatio = fixedGainRatio;
    this.offsetHighV = offsetHighV;
    this.offsetLowV = offsetLowV;
    this.variableGainRatio = variableGainRatio;
    this.wiperMax = wiperMax;
    this.wiperMin = wiperMin;
  }

  getSensorErrorVoltages(model) {
    const gainWiper = model.gain?.wiper;
    const offsetWiper = model.offset?.wiper;
    const sensor1Voltage = model.sensor1Voltage;
    const sensor2Voltage = model.sensor2Voltage;

    return {
      sensor1: this.getSensorErrorVoltage(
        this.sensor1FromSensor2(sensor2Voltage, gainWiper, offsetWiper),
        sensor1Voltage,
      ),
      sensor2: this.getSensorErrorVoltage(
        this.sensor2FromSensor1(sensor1Voltage, gainWiper, offsetWiper),
        sensor2Voltage,
      ),
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
      + offsetWiper * (this.offsetHighV - this.offsetLowV) / this.wiperMax;
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
