import { SUPPLY_VOLTAGE } from "../scene/voltage.js";
import { DigiPotModel, POT_MAX, POT_MIDPOINT, POT_MIN, clampInt } from "./DigiPotModel.js";
import { findSignal } from "./C3Pot_FindSignal.js";
import { fineTuning } from "./C3Pot_FineTuning.js";

export const Phase = Object.freeze({
  NORMAL: "NORMAL",
  SEARCH: "SEARCH",
  placeholder: "placeholder",
});

export const Zone = Object.freeze({
  High: "High",
  Low: "Low",
  inZone: "inZone",
});

export class C3Pot {
  static DIGIPOT_MAX_FOR_PHOTODIODE = 48;
  static GAP_NORMAL = 4;
  static HISTORY_SIZE = 4;
  static SENSOR_MAX = 1023;
  static SENSOR_MIDPOINT = 512;
  static SENSOR_MIN = 0;
  static ZONE_HIGH = 960;
  static ZONE_LOW = 64;

  constructor({
    botDigipot = null,
    inputSignalVoltage = 0,
    midDigipot = null,
    sensorReader = defaultSensorReader,
    topDigipot = null,
  } = {}) {
    this.top = new DigiPotModel({ digipot: topDigipot });
    this.bot = new DigiPotModel({ digipot: botDigipot });
    this.mid = new DigiPotModel({ digipot: midDigipot });
    this.inputSignalVoltage = inputSignalVoltage;
    this.sensorReader = sensorReader;
    this.Phase = Phase;
    this.Zone = Zone;
    this.phase = Phase.SEARCH;
    this.zone = Zone.inZone;
    this.inZone = false;
    this.sensorInverted = false;
    this.sensor = 0;
    this.state = this.makeState();
    this.history = Array.from({ length: C3Pot.HISTORY_SIZE }, () => ({ ...this.state }));
  }

  begin(initialLevel = POT_MIDPOINT) {
    this.top.invert();
    this.bot.invert();
    this.invertSensor();

    this.top.begin(C3Pot.DIGIPOT_MAX_FOR_PHOTODIODE);
    this.bot.begin(POT_MIN);
    this.mid.begin(initialLevel);
    this.captureState();
  }

  connectDigipots({ bot = null, mid = null, top = null } = {}) {
    if (top) this.top.connectDigipot(top);
    if (bot) this.bot.connectDigipot(bot);
    if (mid) this.mid.connectDigipot(mid);
  }

  update(inputSignalVoltage = this.inputSignalVoltage) {
    this.inputSignalVoltage = inputSignalVoltage;
    this.history.pop();
    this.history.unshift({ ...this.state });

    this.readSensor();
    this.updateZone();

    switch (this.phase) {
      case Phase.SEARCH:
        this.findSignal();
        break;
      case Phase.NORMAL:
        this.fineTuning();
        break;
      default:
        break;
    }

    return this.captureState();
  }

  findSignal() {
    findSignal(this);
  }

  fineTuning() {
    fineTuning(this);
  }

  invertSensor() {
    this.sensorInverted = true;
  }

  lastSensorValue() {
    return this.sensor;
  }

  readSensor() {
    const rawSensor = this.sensorReader(this);
    const sensor = this.sensorInverted ? C3Pot.SENSOR_MAX - rawSensor : rawSensor;

    this.sensor = clampInt(sensor, C3Pot.SENSOR_MIN, C3Pot.SENSOR_MAX);
    return this.sensor;
  }

  set() {
    this.top.writeCurrentToPot();
    this.bot.writeCurrentToPot();
    this.mid.writeCurrentToPot();
  }

  setInputSignalVoltage(inputSignalVoltage) {
    this.inputSignalVoltage = inputSignalVoltage;
  }

  snapshot() {
    return {
      botLevel: this.bot.getLevel(),
      inZone: this.inZone,
      inputSignalVoltage: this.inputSignalVoltage,
      midLevel: this.mid.getLevel(),
      phase: this.phase,
      sensor: this.sensor,
      topLevel: this.top.getLevel(),
      zone: this.zone,
    };
  }

  captureState() {
    this.state = this.makeState();
    return this.state;
  }

  makeState() {
    return {
      botLevel: this.bot.getLevel(),
      midLevel: this.mid.getLevel(),
      phase: this.phase,
      sensor: this.lastSensorValue(),
      topLevel: this.top.getLevel(),
    };
  }

  updateZone() {
    if (this.sensor <= C3Pot.ZONE_LOW) {
      this.zone = Zone.Low;
    } else if (this.sensor >= C3Pot.ZONE_HIGH) {
      this.zone = Zone.High;
    } else {
      this.zone = Zone.inZone;
    }
  }
}

export function defaultSensorReader(controller) {
  const topVoltage = levelToVoltage(controller.top.getLevel());
  const botVoltage = levelToVoltage(controller.bot.getLevel());
  const blend = controller.mid.getLevel() / POT_MAX;
  const referenceVoltage = botVoltage + (topVoltage - botVoltage) * blend;
  const deltaVoltage = controller.inputSignalVoltage - referenceVoltage;

  return C3Pot.SENSOR_MIDPOINT + (deltaVoltage / SUPPLY_VOLTAGE) * C3Pot.SENSOR_MAX;
}

function levelToVoltage(level) {
  return (level / POT_MAX) * SUPPLY_VOLTAGE;
}
