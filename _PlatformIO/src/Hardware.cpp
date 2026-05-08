#include "Setup.h"
#include "Hardware.h"
#include "HWforState.h"
#include "Helpers.h"
#include "CUSB.h"
#include "CAutoPot.h"
#include "CA2D.h"
#include "CHead.h"
#include "CTimer.h"
#include "CTelemetry.h"
#include <map>

void Hardware::begin() {
    SPI .begin();  // initialise SPI

    // Initialize USB
    USB .begin().printf("CPU Frequency: %.0f Mhz\r\n", F_CPU / 1000000.0f);


    // initialize buttons and LEDs
    BUT .begin();
    LED .begin();

    // Initialize hardware components
    Head.begin();
    A2D .begin();

    delay(1); // Allow time for hardware to stabilize (ample)

    // ensure A2D has a valid getLastDataTime();
    while (A2D.poll() == false)
      delayMicroseconds(5);
    

    Timer.restart();
}

CTeleCounter TC_Update{TeleGroup::HARDWARE, 1};
CTelePeriod  TP_Update{TeleGroup::HARDWARE, 2};
CRunningAverage<double> _raUpdateDurations;

static double    STATE_DURATION = 1.0 * CFG::STATE_DURATION_uS   * 0.000'001; // convert to seconds
static double A2D_POLL_DURATION = 1.5 * CFG::A2D_READING_PERIOD_uS * 0.000'001; // convert to seconds

bool Hardware::canUpdate() {

  return (Timer.getStateTime() + A2D_POLL_DURATION < STATE_DURATION );
}

void Hardware::update() {

  TP_Update.measure();
  TC_Update.increment();

  if (A2D.poll() == false) { yield(); return; }

  if (Timer.sampleReady) return;

  double stateTime = Timer.getStateTime();

  if (stateTime + A2D_POLL_DURATION < STATE_DURATION) {

    double updateStart = Timer.getStateTime();
        HW->update();  // update digital pots based on current state
    double updateEnd = Timer.getStateTime();

    _raUpdateDurations.add(updateEnd - updateStart);

  }


}