import { DifferentialAmpSensorModel } from "../helpers/DifferentialAmpSensorModel.js";
import { getModelWipers } from "../helpers/Wipers.js";

const VALID_SENSOR_MIN_V = 0.21;
const VALID_SENSOR_MAX_V = 3.1;

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
    model,
    sensorVoltages = null,
    source = "manual",
  }) {
    const wipers = getModelWipers(model);
    const sensor1Actual = getKnownVoltage(sensorVoltages?.sensor1)
      ?? getKnownVoltage(model.sensor1Voltage)
      ?? circuitScene?.getSceneSensor1Voltage?.()
      ?? null;
    const sensor2Actual = getKnownVoltage(sensorVoltages?.sensor2)
      ?? getKnownVoltage(model.sensor2Voltage);
    const sensor2Predicted = this.sensorModel.sensor2FromSensor1(
      sensor1Actual,
      wipers.gain,
      wipers.offset,
    );
    const sensor1Predicted = this.sensorModel.sensor1FromSensor2(
      sensor2Actual,
      wipers.gain,
      wipers.offset,
    );
    const sample = {
      source,
      timestamp: Date.now(),
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
  const voltage = Number(value);

  return Number.isFinite(voltage) ? voltage : null;
}

function isValidSensorVoltage(value) {
  return Number.isFinite(value)
    && value >= VALID_SENSOR_MIN_V
    && value <= VALID_SENSOR_MAX_V;
}

function subtractKnown(actual, predicted) {
  return Number.isFinite(actual) && Number.isFinite(predicted)
    ? actual - predicted
    : null;
}

function formatCsvNumber(value) {
  const number = Number(value);

  return Number.isFinite(number) ? number.toFixed(9) : "";
}
