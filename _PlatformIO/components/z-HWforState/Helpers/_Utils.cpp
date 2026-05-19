#include "HWforState.h"

void HWforState::adjustTopBot() {
 
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
  }

}


int16_t HWforState::readCheck() {

  uint16_t s2 = sensor2.read();
  if (sensor2.inZone == false) {
    gain.setLevel(8);
    delayMicroseconds(10);
    sensor1.read();
    phase = sensor1.inZone ? Phase::ZOOM : Phase::SEARCH;
  }
  return static_cast<int16_t>(s2);
}
