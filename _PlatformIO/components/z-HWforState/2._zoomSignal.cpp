#include "HWforState.h"
#include "CMAsterTimer.h"
#include <algorithm>


void HWforState::_zoomSignal() {
  static constexpr int MAX_CHANGE = 200;
  bool gainMaxed = false;
  gain.setLevel(8);
  delayMicroseconds(10);

  readCheck(); if (phase != Phase::ZOOM) return; // check if signal is lost before attempting to zoom

  while (gainMaxed == false) {
  
    if (gain.getLevel() == 255) { gainMaxed = true; break; }
    
    gain.offsetLevel(+2);
    delayMicroseconds(10); 

    int16_t v1, v2 = sensor2.read();
    int16_t d1, d2 = v2 - SENSOR2_TARGET;

    while (true) {

      v1 = v2; d1 = d2;
      
      int direction = (v1 < SENSOR2_TARGET) ? +1 : -1;

      mid.offsetLevel(direction);
      delayMicroseconds(10);
      
      v2 = sensor2.read(); 
      d2 = v2 - SENSOR2_TARGET;

      if (abs(v2 - v1) > MAX_CHANGE) {
        gainMaxed = true;
        break;
      }

      if (abs(d2) > abs(d1)) { 
        mid.offsetLevel(-direction); // revert if it made it worse
        delayMicroseconds(10); readCheck(); if (phase != Phase::ZOOM) return;
        break;
      }

      adjustTopBot();


      if (std::signbit(d1) != std::signbit(d2)) break; // if it crossed the target, stop
    
    }
  }
  
  phase = Phase::MEASURE;
}
