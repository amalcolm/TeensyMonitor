#pragma once
#include "CAutoPot.h"

class CGainPot : public CDigiPot {

  private:
    int _lowThreshold;
    int _highThreshold;

  public:
    CGainPot(int csPin, int sensorPin, int windowSize)
      : CDigiPot(csPin, sensorPin, 1)
      ,  _lowThreshold(SENSOR_MIDPOINT - windowSize)
      , _highThreshold(SENSOR_MIDPOINT + windowSize) {};

    
    void update() override {
      readSensor();
      
      bool boostSignal = (_lastSensorValue >= _lowThreshold && _lastSensorValue <= _highThreshold);

      if (zone != Zone::inZone) _offsetLevel(-1);
      else
      if (boostSignal         ) _offsetLevel(+1);

    }

};


static_assert(std::is_copy_constructible_v<CGainPot  >);
static_assert(std::is_copy_assignable_v   <CGainPot  >);
