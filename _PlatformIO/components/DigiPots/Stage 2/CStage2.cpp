#include "CStage2.h"
#include "Arduino.h"
#include "HWforState.h"
#include "CMasterTimer.h"
#include "CA2D.h"

constexpr int OFFSET_WINDOW_SIZE = 300; // 280 normal
constexpr int   GAIN_WINDOW_SIZE = 300; // 100 normal

constexpr int SAMPLES_TO_AVERAGE = 50;

CStage2::CStage2(int csPinOffset, int csPinGain, int sensorPin)
: CAutoPot(-1, sensorPin, SAMPLES_TO_AVERAGE)
, offsetPot(csPinOffset, sensorPin, OFFSET_WINDOW_SIZE)
, gainPot  (csPinGain  , sensorPin,   GAIN_WINDOW_SIZE) {}

void CStage2::begin() { gainPot.invert();
  offsetPot.begin(128); gainPot.begin(  0);
}
void CStage2::reset() {
  offsetPot.reset(128); gainPot.reset(  0);
}
void CStage2::set() {
  offsetPot.writeCurrentToPot();
    gainPot.writeCurrentToPot();
}



void CStage2::update() { if (HW->flags.holdStage2) { _lastSensorValue = gainPot.readSensor(); _updateZone(false); return; }

  offsetPot.update();   

  if (offsetPot.inZone == false) {
    inZone = false;
    _updateZone();
    return;
  }

  gainPot.update();
  inZone = gainPot.inZone;

  if (Timer.sampleReady) return;

  if (HW && HW->flags.offsetsChanged) {
     HW->flags.offsetsChanged = false;
    _lastV = static_cast<double>(analogRead(getSensorPin()));
  }

  if (inZone == false) {
    _updateZone();
    return;
  }

  
    if (Timer.getStateTime() > 0.001) {
      filterSensor(SAMPLES_TO_AVERAGE, 0.002);
      Timer.sampleReady = true;
      A2D.storeNewData();
    } else {
      filterSensor(1, 0.01);
    }
  
}

void CStage2::filterSensor(int numSamples, double t) {
  double tInv = 1.0 - t;
  int sensor = getSensorPin();

  double v = _lastV;

  for (int i = 0; i < numSamples; ++i)
    v = t * static_cast<double>(analogRead(sensor)) + tInv * v;

  _lastV = v;
  _lastSensorValue = static_cast<int>(v);

  _updateZone(false);
}



void CStage2::_updateZone(bool readSensor) {
  int sensor  = readSensor ? analogRead(getSensorPin()) : lastSensorValue();

  if (sensor < CAutoPot::SENSOR_MIDPOINT)
    zone = Zone::Low;
  else
    zone = Zone::High;
}
