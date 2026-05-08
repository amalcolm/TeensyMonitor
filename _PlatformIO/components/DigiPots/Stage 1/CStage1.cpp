#include "CStage1.h"
#include "CTelemetry.h"
#include "C32bitTimer.h"
#include "CUSB.h"
#include "CBuffer.h"
#include <tuple>
#include "CMasterTimer.h"
#include "HWforState.h"

CStage1::CStage1(int csPinTop, int csPinBot, int csPinMid, int sensorPin) 
      : CDigiPot(csPinMid, sensorPin, 1)  // pass csPinMid to CAutoPot constructor
      , top(csPinTop)
      , bot(csPinBot)
      , mid(static_cast<CDigiPot&>(*this)) {

  static std::tuple<int, int> knownGaps[] = { {2, 107}, {4, 70}, {8, 40}, {12, 28}, {16, 22}, {24, 17} };

  for (const auto& [gap, midStep] : knownGaps) 
    if (gap == GAP_TOPBOT) MID_STEP = midStep;

  if (MID_STEP >= POT_MIDPOINT) ERROR("MID_STEP is too large");

  clearHistory();

  phase = Phase::SEARCH;
}


void CStage1::update() {
  storeOldState();

  readSensor(); _updateZone();

  if (HW->flags.holdStage1) return;

  switch (phase) {
    case Phase::SEARCH: findSignal(); break;
    case Phase::NORMAL: fineTuning(); break;
    default: break;
  }

  setState();

};













#pragma region State management and debugging utilities
void CStage1::printDebug(bool signalFound) {
     int zoneValue = (zone == Zone::Low) ? 200 : (zone == Zone::High) ? 254 : 225;
 
    USB.printf("signal:%d\tzone:%d\tsensor:%d\ttop:%d\tbot:%d\tmid:%d\tmin:0\tmax:256\n",
       signalFound ? 199 : 150, zoneValue, lastSensorValue()/4, top.getLevel(), bot.getLevel(), mid.getLevel());

}


void CStage1::clearHistory () { for (int i = HISTORY_SIZE - 1; i >= 0; --i) history[i] = State{}; }
void CStage1::storeOldState() { for (int i = HISTORY_SIZE - 1; i >  0; --i) history[i] = history[i - 1]; history[0] = state; }
void CStage1::setState() {
  state.phase = phase; 
  state.sensor = lastSensorValue(); 
  state.topLevel = top.getLevel(); 
  state.botLevel = bot.getLevel();
  state.midLevel = mid.getLevel(); 
}
#pragma endregion