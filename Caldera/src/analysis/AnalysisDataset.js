import { DifferentialAmpSensorModel } from "../helpers/DifferentialAmpSensorModel.js";
import { getModelWipers } from "../helpers/Wipers.js";
import { isValidSensorVoltage } from "../model/voltage.js";

export class AnalysisDataset {
  constructor({
    sensorModel = new DifferentialAmpSensorModel(),
  } = {}) {
    this.sensorModel = sensorModel;
    this.samples = [];
  }

  clear() {
    this.samples = [];
  }

  addSampleFromModel({
    circuitScene,
    ledLabel = null,
    leds = null,
    ledState = null,
    model,
    sampleCount = null,
    sampleIndex = null,
    sensorVoltages = null,
    source = "manual",
    test = null,
  }) {
    const wipers = getModelWipers(model);
    const sensor1Actual = getKnownVoltage(sensorVoltages?.sensor1)
      ?? getKnownVoltage(model.sensor1Voltage)
      ?? circuitScene?.getSceneSensor1Voltage?.()
      ?? null;
    const sensor2Actual = getKnownVoltage(sensorVoltages?.sensor2)
      ?? getKnownVoltage(model.sensor2Voltage);
    const sensor2Predicted = getKnownVoltage(this.sensorModel.sensor2FromSensor1(
      sensor1Actual,
      wipers.gain,
      wipers.offset,
    ));
    const sensor1Predicted = getKnownVoltage(this.sensorModel.sensor1FromSensor2(
      sensor2Actual,
      wipers.gain,
      wipers.offset,
    ));
    const sample = {
      ledLabel,
      leds: normaliseLedMap(leds),
      ledState: getKnownState(ledState),
      sampleCount: getKnownCount(sampleCount),
      sampleIndex: getKnownCount(sampleIndex),
      source,
      timestamp: Date.now(),
      test,
      wipers,
      sensorActual: {
        sensor1: sensor1Actual,
        sensor2: sensor2Actual,
      },
      sensorPredicted: {
        sensor1: sensor1Predicted,
        sensor2: sensor2Predicted,
      },
      residuals: {
        sensor1: subtractKnown(sensor1Actual, sensor1Predicted),
        sensor2: subtractKnown(sensor2Actual, sensor2Predicted),
      },
    };

    this.samples.push(sample);

    return sample;
  }

  getSensorComparisonSamples() {
    return this.samples.map((sample) => ({
      ...sample,
      isPlottable: isValidSensorVoltage(sample.sensorActual.sensor1)
        && isValidSensorVoltage(sample.sensorActual.sensor2),
    }));
  }

  toCsv() {
    return [
      CSV_HEADER,
      ...this.samples.map((sample) => [
        new Date(sample.timestamp).toISOString(),
        sample.source,
        sample.test ?? "",
        formatCsvNumber(sample.ledState, 0),
        sample.ledLabel ?? "",
        sample.wipers.top,
        sample.wipers.bot,
        sample.wipers.mid,
        sample.wipers.offset,
        sample.wipers.gain,
        formatCsvNumber(sample.sensorActual.sensor1),
        formatCsvNumber(sample.sensorActual.sensor2),
        formatCsvNumber(sample.sensorPredicted.sensor1),
        formatCsvNumber(sample.sensorPredicted.sensor2),
        formatCsvNumber(sample.residuals.sensor1),
        formatCsvNumber(sample.residuals.sensor2),
      ].join(",")),
    ].join("\n");
  }
}

const CSV_HEADER = [
  "timestamp",
  "source",
  "test",
  "ledState",
  "leds",
  "topWiper",
  "botWiper",
  "midWiper",
  "offsetWiper",
  "gainWiper",
  "sensor1ActualV",
  "sensor2ActualV",
  "sensor1PredictedV",
  "sensor2PredictedV",
  "sensor1ResidualV",
  "sensor2ResidualV",
].join(",");

function getKnownVoltage(value) {
  if (value === null || value === undefined || value === "") {
    return null;
  }

  const voltage = Number(value);

  return Number.isFinite(voltage) ? voltage : null;
}

function getKnownState(value) {
  const state = Number(value);

  return Number.isFinite(state) && state >= 0
    ? Math.trunc(state) >>> 0
    : null;
}

function getKnownCount(value) {
  const count = Number(value);

  return Number.isFinite(count) && count > 0
    ? Math.trunc(count)
    : null;
}

function subtractKnown(actual, predicted) {
  return Number.isFinite(actual) && Number.isFinite(predicted)
    ? actual - predicted
    : null;
}

function normaliseLedMap(leds) {
  if (!leds || typeof leds !== "object") {
    return null;
  }

  return Object.fromEntries(
    Object.entries(leds).map(([id, active]) => [id, active === true]),
  );
}

function formatCsvNumber(value, fractionDigits = 9) {
  if (value === null || value === undefined || value === "") {
    return "";
  }

  const number = Number(value);

  return Number.isFinite(number) ? number.toFixed(fractionDigits) : "";
}
