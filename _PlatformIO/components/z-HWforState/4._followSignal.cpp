#include "HWforState.h"

void HWforState::_followSignal() {
  readCheck(); if (phase != Phase::FOLLOW) return; // check if signal is lost before attempting to follow


//  if (measureTimer.passed())
//    USB.printf("Following, difference: %.2f\n", sensor2.lastV() - SENSOR2_TARGET);  
  

}