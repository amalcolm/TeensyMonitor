#include "HWforState.h"
#include "CNoiseSample.h"
#include "CUSB.h"
#include <algorithm>

void HWforState::_zoomSignal() { 

   if (flags.zoomLevel == -1) {
    flags.zoomLevel = 15;
    gain.setLevel(flags.zoomLevel);
    // mid is assumed to be near sensor1Target at this point, from SEARCH
    offset.setLevel(128);
    delayMicroseconds(10);

  
    centreMid(sensor1); if (phase != Phase::ZOOM) goto exit;
    centreOffset(sensor2); if (phase != Phase::ZOOM) goto exit;
    // starting point with stability, 
  }


  flags.zoomLevel += 32;
  gain.setLevel(flags.zoomLevel);
  delayMicroseconds(10);
  
  if (quickNoiseTest(40, sensor1.getPin()) > 20) {
    gain.setLevel(flags.zoomLevel-32);
    delayMicroseconds(10);
    phase = Phase::MEASURE;
  }
  else
  if (gain.getLevel() == CDigiPot::POT_MAX) phase = Phase::MEASURE;

  centreMid(sensor1); if (phase != Phase::ZOOM) goto exit;
  centreOffset(sensor2); if (phase != Phase::ZOOM) goto exit;

  if (phase == Phase::ZOOM) return;

exit:
  flags.reset();
  phase = Phase::MEASURE;
  
}
