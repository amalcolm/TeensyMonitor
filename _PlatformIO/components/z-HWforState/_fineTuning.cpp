#include "HWforState.h"
#include "CUSB.h"
#include "CMasterTimer.h"

void HWforState::_fineTuning() {
  static constexpr int SENSOR_DEADBAND = 8;
  static constexpr int SENSOR_LOW      = CSensor::MIDPOINT - SENSOR_DEADBAND/2;
  static constexpr int SENSOR_HIGH     = CSensor::MIDPOINT + SENSOR_DEADBAND/2;

  for (int count = 0; count < 100 && phase == Phase::FINETUNE; ++count) {

    int WIPER_LOW  = CDigiPot::POT_MIDPOINT - MID_STEP;
    int WIPER_HIGH = CDigiPot::POT_MIDPOINT + MID_STEP;


    int direction = 0;
    int wiperLevel = mid.getLevel();

    if (wiperLevel < WIPER_LOW ) direction = +1;
    else
    if (wiperLevel > WIPER_HIGH) direction = -1;

    if (direction != 0) {
      top.offsetLevel(direction);
      bot.offsetLevel(direction);
      mid.offsetLevel(direction * MID_STEP);
      delayMicroseconds(10);
      continue;
    }

    
    int sensorValue = sensor1.read();
    if (sensor1.inZone == false) phase = Phase::SEARCH;
    else
    if (sensorValue < SENSOR_LOW ) mid.offsetLevel(+1);
    else  
    if (sensorValue > SENSOR_HIGH) mid.offsetLevel(-1);
    else
      phase = Phase::NORMAL;
  }

}