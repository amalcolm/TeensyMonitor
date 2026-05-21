#include "HWforState.h"
#include "CMAsterTimer.h"
#include <algorithm>
#include "CUSB.h"

struct Zoomflags { StateType state;
   int test = -1;
  };
struct Zoomflags& getZoomflags(StateType state);

void HWforState::_zoomSignal() { 
///  auto& [_, test] = getZoomflags(state);

   if (flags.zoomLevel == -1) {
    flags.zoomLevel = 15;
    gain.setLevel(flags.zoomLevel);
    // mid is assumed to be near sensor1Target at this point, from SEARCH
    offset.setLevel(128);
    delayMicroseconds(10);

  
    centreMid(sensor1); if (phase != Phase::ZOOM) return; 
    centreOffset(sensor2); if (phase != Phase::ZOOM) return;
    // starting point with stability, 
  }

  flags.zoomLevel = 100;
  gain.setLevel(flags.zoomLevel);
  delayMicroseconds(10);


  phase = Phase::FOLLOW;
}





// Zoomflags management
#include <deque>

std::deque<Zoomflags> s_zoomFlags;

Zoomflags& getZoomflags(StateType state) {
  for (auto& flags : s_zoomFlags) if (flags.state == state) return flags;

  s_zoomFlags.push_back({state});
  return s_zoomFlags.back();
}
