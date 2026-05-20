#include "HWforState.h"
#include "CMAsterTimer.h"
#include "CGainAnalysis.h"
#include <algorithm>
#include "CUSB.h"

void HWforState::_zoomSignal() {
  gain.setLevel(8);
  offset.setLevel(128);
  delayMicroseconds(10);

  centreMid(sensor1); if (phase != Phase::ZOOM) return;
  centreMid(sensor2); if (phase != Phase::ZOOM) return;

  centreOffset(sensor2); if (phase != Phase::ZOOM) return;

  gain.setLevel(255);
  delayMicroseconds(10);
  centreOffset(sensor2); if (phase != Phase::ZOOM) return;  
  phase = Phase::FOLLOW;
}
