import { POT_MIDPOINT, POT_MIN, clampInt } from "./DigiPotModel.js";

export function findSignal(controller) {
  const maxIterations = 100;
  const Model = controller.constructor;

  controller.top.setLevel(Model.DIGIPOT_MAX_FOR_PHOTODIODE);
  controller.bot.setLevel(POT_MIN);
  controller.mid.setLevel(POT_MIDPOINT);

  let signalFound = false;
  const initialHilo = controller.readSensor() < Model.SENSOR_MIDPOINT ? -1 : 1;
  let hilo = 0;

  for (
    let i = 0;
    controller.top.getLevel() - controller.bot.getLevel() > Model.GAP_NORMAL && i < maxIterations;
    i += 1
  ) {
    hilo = controller.readSensor() < Model.SENSOR_MIDPOINT ? -1 : 1;

    if (!signalFound) {
      if (hilo === initialHilo) {
        controller.mid.offsetLevel(-hilo);
      } else {
        signalFound = true;
      }
    } else {
      if (hilo === -1) controller.top.offsetLevel(-1);
      if (hilo === 1) controller.bot.offsetLevel(1);

      if (controller.mid.getLevel() !== POT_MIDPOINT) {
        const offset = controller.mid.getLevel() - POT_MIDPOINT;
        const sign = Math.sign(offset);
        const delta = sign * clampInt(Math.trunc(Math.abs(offset) / 4), 1, 3);

        controller.mid.offsetLevel(-delta);
      }
    }
  }

  const secondInitialHilo = controller.readSensor() < Model.SENSOR_MIDPOINT ? -1 : 1;
  let lastDelta = 0;
  let delta = 0;

  for (let i = 0; i < maxIterations; i += 1) {
    lastDelta = delta;
    controller.readSensor();

    delta = Math.abs(controller.lastSensorValue() - Model.SENSOR_MIDPOINT);
    hilo = controller.lastSensorValue() < Model.SENSOR_MIDPOINT ? -1 : 1;

    if (hilo !== secondInitialHilo) {
      break;
    }

    controller.mid.offsetLevel(-hilo);
  }

  if (delta < lastDelta) {
    controller.mid.offsetLevel(hilo);
    controller.readSensor();
  }

  controller.phase = controller.Phase.NORMAL;
}
