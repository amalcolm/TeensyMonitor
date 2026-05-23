#include "XCommands.h"
#include "HWforState.h"
#include "Config.h"
void XCommand::honour() {
  
   CFG::commandFlags = header.cmdFlags;

  if (this->hasFlag(CommandFlags::HoldWipers))
    HW->flags.holdWipers = true;

  if (this->hasFlag(CommandFlags::SetSearchPhase))
    HW->setPhase( HWforState::Phase::SEARCH );












  if (this->hasFlag(CommandFlags::Test_MidOffset))
    HW->testMidOffset();

  if (this->hasFlag(CommandFlags::Test_NoiseSample))
    HW->testGetNoiseSample();
}


bool XCommand::hasFlag(CommandFlags flag) const { return ::hasFlag(header.cmdFlags, flag); }
