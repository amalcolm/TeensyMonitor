#include "HWforState.h"
#include "CNoiseSample.h"
#include "CUSB.h"
void HWforState::testGetNoiseSample() {
  static constexpr int readPeriod_uS = 20;  // microseconds (uS)
  static constexpr int totalTime_mS  =  4;  // milliseconds (mS)

  static constexpr int totalTime_uS = totalTime_mS * 1000;
  static constexpr double readPeriod_S = readPeriod_uS / 1'000'000.0;

  static constexpr int numSamples = totalTime_uS / readPeriod_uS;
  
  DebugType* dbg = USB.getDebugBuffer();  if (dbg == nullptr) return; 

  dbg->count = numSamples;
  FillBufferWithNoise(dbg->data, dbg->count, readPeriod_S);
  USB.buffer(dbg);
}
