#include "HWforState.h"
#include "CNoiseSample.h"
#include "CUSB.h"
void HWforState::testGetNoiseSample() {
  
  DebugType* dbg = USB.getDebugBuffer();  if (dbg == nullptr) return; 

  FillBufferWithNoise(dbg->data, DebugType::DEBUG_BLOCKSIZE);
  dbg->count = DebugType::DEBUG_BLOCKSIZE;
  USB.buffer(dbg);
}
