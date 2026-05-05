import { POT_MAX, POT_MIDPOINT } from "./DigiPotModel.js";

export function fineTuning(controller) {
  const sensorDeadband = 100;
  const sensorLow = controller.constructor.SENSOR_MIDPOINT - sensorDeadband / 2;
  const sensorHigh = controller.constructor.SENSOR_MIDPOINT + sensorDeadband / 2;
  const midStep = 150;
  const wiperLow = Math.trunc((POT_MIDPOINT - midStep / 2) / 2);
  const wiperHigh = POT_MAX - wiperLow;

  if (controller.zone !== controller.Zone.inZone) {
    controller.inZone = false;
    controller.phase = controller.Phase.SEARCH;
    return;
  }

  let direction = 0;
  const wiperLevel = controller.mid.getLevel();

  if (wiperLevel < wiperLow) direction = 1;
  else if (wiperLevel > wiperHigh) direction = -1;

  if (direction !== 0) {
    controller.top.offsetLevel(direction);
    controller.bot.offsetLevel(direction);
    controller.mid.offsetLevel(direction * midStep);
    return;
  }

  const sensorValue = controller.lastSensorValue();

  if (sensorValue < sensorLow) controller.mid.offsetLevel(1);
  else if (sensorValue > sensorHigh) controller.mid.offsetLevel(-1);
  else controller.inZone = true;
}
