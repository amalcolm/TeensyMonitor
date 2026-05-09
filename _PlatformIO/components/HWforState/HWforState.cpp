#include "HWforState.h"
#include "CMasterTimer.h"

constexpr int SAMPLES_IN_LONGREAD = 50;

HWforState::HWforState(StateType state) : state(state) {
  static std::tuple<int, int> knownGaps[] = { {2, 107}, {4, 70}, {8, 40}, {12, 28}, {16, 22}, {24, 17} };

  for (const auto& [gap, midStep] : knownGaps) 
    if (gap == GAP_TOPBOT) MID_STEP = midStep;

  if (MID_STEP >= CDigiPot::POT_MIDPOINT) ERROR("MID_STEP is too large");

  phase = Phase::SEARCH;
}


void HWforState::_update() {

  if (HW->flags.holdWipers) return;

  switch (phase) {
    case Phase::SEARCH: _findSignal(); break;
    case Phase::NORMAL: _fineTuning(); break;
    default: break;
  }

  if (flags.wipersChanged) {
    flags.wipersChanged = false;
    flags.lastV = static_cast<double>(sensor2.read());
  }

  if (sensor2.inZone == false) 
    return;


  if (Timer.getStateTime() > 0.001) {
    sensor2.filter(SAMPLES_IN_LONGREAD, 0.002);
    Timer.sampleReady = true;
    A2D.storeNewData();
  } else {
    sensor2.filter(1, 0.01);
  }

}


void HWforState::begin() {
  top    .invert();
  bot    .invert();
  sensor1.invert(); 
  gain   .invert();

  top    .begin(255);
  bot    .begin(  0);
  mid    .begin(128);

  offset .begin(128);
  gain   .begin(  0);

  sensor1.begin();
  sensor2.begin();
  
  flags.begun = true;
}


void HWforState::set() {
  if (!Ready) return; else if (!flags.begun) begin();   // ensure ready and begun

  top   .writeCurrentToPot();
  bot   .writeCurrentToPot();
  mid   .writeCurrentToPot();
  offset.writeCurrentToPot();
  gain  .writeCurrentToPot();
}


void HWforState::setWipers(XCMD_SetWipers& cmd) {
      
      if (cmd.top == 0 && cmd.bot == 0) { // release hold
        flags.holdWipers = false;
        return;
      }

      top   .setLevel(cmd.top);
      bot   .setLevel(cmd.bot);
      mid   .setLevel(cmd.mid);
      offset.setLevel(cmd.offset);
      gain  .setLevel(cmd.gain);

      flags.holdWipers = true; // release hold when wipers are set manually
    }