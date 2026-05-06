import { DigiPot } from "./components/DigiPot.js";
import { SUPPLY_VOLTAGE, isKnownVoltage } from "./voltage.js";

const GROUND_VOLTAGE = 0;
const DIGIPOT_RESISTANCE_OHMS = 5000;
const THREE_POT_RAILS = Object.freeze({
  digipotResistanceOhms: DIGIPOT_RESISTANCE_OHMS,
  groundResistanceOhms: 0,
  groundVoltage: GROUND_VOLTAGE,
  supplyResistanceOhms: 22000,
  supplyVoltage: SUPPLY_VOLTAGE,
});
const OFFSET_RAILS = Object.freeze({
  digipotResistanceOhms: DIGIPOT_RESISTANCE_OHMS,
  groundResistanceOhms: 79600,
  groundVoltage: GROUND_VOLTAGE,
  supplyResistanceOhms: 80600,
  supplyVoltage: SUPPLY_VOLTAGE,
});

export class Model {
  constructor({ onChange = null } = {}) {
    this.onChange = onChange;

    this.top = this.makeDigiPot("top");
    this.bot = this.makeDigiPot("bot");
    this.mid = this.makeDigiPot("mid");

    this.TIA = null;

    this.offset = this.makeDigiPot("offset");
    this.gain = null;

    this.diffAmp = null;

    this.evaluate();
  }

  evaluate() {
    const threePotTerminals = getPoweredDigipotTerminalVoltages(THREE_POT_RAILS);

    this.top.setInputVoltages(threePotTerminals);
    this.top.evaluateVoltage();

    this.bot.setInputVoltages(threePotTerminals);
    this.bot.evaluateVoltage();

    this.mid.setInputVoltages({
      bottom: this.bot.wiperVoltage,
      top: this.top.wiperVoltage,
    });
    this.mid.evaluateVoltage();

    this.offset.setInputVoltages(getPoweredDigipotTerminalVoltages(OFFSET_RAILS));
    this.offset.evaluateVoltage();

    return this.snapshot();
  }

  snapshot() {
    return {
      bot: this.snapshotDigiPot(this.bot),
      mid: this.snapshotDigiPot(this.mid),
      offset: this.snapshotDigiPot(this.offset),
      top: this.snapshotDigiPot(this.top),
    };
  }

  makeDigiPot(id) {
    return new DigiPot({
      onChange: (event) => this.handleDigiPotChange(id, event),
    });
  }

  handleDigiPotChange(id, event) {
    this.evaluate();
    this.onChange?.({
      ...event,
      component: event.model,
      id,
      model: this,
    });
  }

  snapshotDigiPot(digipot) {
    return {
      bottomVoltage: digipot.bottomVoltage,
      outputVoltage: digipot.wiperVoltage,
      topVoltage: digipot.topVoltage,
      wiper: digipot.wiper,
    };
  }
}

export function getPoweredDigipotTerminalVoltages({
  digipotResistanceOhms,
  groundResistanceOhms,
  groundVoltage,
  supplyResistanceOhms,
  supplyVoltage,
}) {
  const totalResistance = supplyResistanceOhms + digipotResistanceOhms + groundResistanceOhms;

  if (!isKnownVoltage(supplyVoltage) || !isKnownVoltage(groundVoltage) || totalResistance <= 0) {
    return { bottom: null, top: null };
  }

  const current = (supplyVoltage - groundVoltage) / totalResistance;
  const bottom = groundVoltage + current * groundResistanceOhms;
  const top = bottom + current * digipotResistanceOhms;

  return { bottom, top };
}
