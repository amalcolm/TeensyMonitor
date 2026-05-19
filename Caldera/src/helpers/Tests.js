import Constants from '../constants/Constants';
import { isValidSensorVoltage } from './Voltages';

export default class Tests {
    
    static findMinMax(wiper, sensor) {

        let min = Constants.DIGIPOT_MIN;
        let max = Constants.DIGIPOT_MAX;
        
        min = Tests.findBoundary(wiper, sensor, min, max, "max");
        max = Tests.findBoundary(wiper, sensor, min, max, "min");

        return { min, max };
    }

    static findBoundary(wiper, sensor, min, max, type) {
        let rangeMin = min, rangeMax = max;

        let valid = false;

        switch (type) {
            case "min": wiper.setLevel(min); valid = isValidSensorVoltage(sensor.getVoltage()); if (valid) return min; break;
            case "max": wiper.setLevel(max); valid = isValidSensorVoltage(sensor.getVoltage()); if (valid) return max; break;
        }

        while (rangeMax - rangeMin > 8) {
            const mid = Math.floor((rangeMin + rangeMax) / 2);

            wiper.setLevel(mid);
            const v = sensor.getVoltage();;

            valid = isValidSensorVoltage(v);
            switch (type) {
                case "min": if (valid) rangeMin = mid; break;
                case "max": if (valid) rangeMax = mid; break;
            }
        }

        return type === "min" ? rangeMin : rangeMax;
    };
};

// here is the version for th _PlatformIO codef - working - please remove when done
/*
  while (Wtop - Wbot > GAP_TOPBOT*2) {
    if (sensor1.read() < HWforState::SENSOR1_TARGET) {
      Wbot = wiper;
      wiper = (wiper + Wtop) / 2;
    } else {
      Wtop = wiper;
      wiper = (wiper + Wbot) / 2;
    }
    mid.setLevel(wiper);
    delayMicroseconds(10);
  }
*/