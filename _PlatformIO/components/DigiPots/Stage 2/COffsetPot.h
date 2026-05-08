#pragma once
#include "CAutoPot.h"

class COffsetPot : public CDigiPot {

  private:
    int _lowThreshold;
    int _highThreshold;

  public:
    COffsetPot(int csPin, int sensorPin, int windowSize)
      : CDigiPot(csPin, sensorPin, 1)
      ,  _lowThreshold(SENSOR_MIDPOINT - windowSize)
      , _highThreshold(SENSOR_MIDPOINT + windowSize) {}


    void update() override {
      auto val =  readSensor();
      return;
      //  val = _runningAverage.GetAverage();

      if (val > _highThreshold) _offsetLevel(+1);
      else
      if (val <  _lowThreshold) _offsetLevel(-1);
    }


};


static_assert(std::is_copy_constructible_v<COffsetPot>);
static_assert(std::is_copy_assignable_v   <COffsetPot>);
