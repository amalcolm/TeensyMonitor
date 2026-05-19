#include "HWforState.h"

void HWforState::_followSignal() {
  readCheck(); if (phase != Phase::FOLLOW) return; // check if signal is lost before attempting to follow


  

}