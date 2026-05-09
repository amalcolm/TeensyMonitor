#include "CSensor.h"
#include <Arduino.h>

CSensor::CSensor(int pin): _pin(pin) {}

void CSensor::begin() {}

void CSensor::invert() { _inverted = !_inverted; }


uint16_t CSensor::read(int samplesToAverage) {  if (_pin < 0) return 0; // No sensor pin defined
  
  analogRead(_pin); // discard first reading as it can be inaccurate right after writing to pot


  if (samplesToAverage <= 1) {
    int rawValue = analogRead(_pin);

    _lastValue = _inverted ? 1023 - rawValue : rawValue;
  }
  else
  {
    int totalValue = 0;
    for (int i = 0; i < samplesToAverage; i++)
        totalValue += analogRead(_pin);

    if (_inverted)
        totalValue = (samplesToAverage * 1023) - totalValue;

    _lastValue = totalValue / samplesToAverage;

    _ra.add(_lastValue);
  }

  _updateZone();

  return static_cast<uint16_t>(_lastValue);
}



CSensor::Zone CSensor::_updateZone() {
 static constexpr int       DEADZONE = 64;

 static constexpr int  LOW_THRESHOLD =        DEADZONE;
 static constexpr int HIGH_THRESHOLD = 1023 - DEADZONE;

 if (_lastValue <  LOW_THRESHOLD) zone = Zone::Low;
 else
 if (_lastValue > HIGH_THRESHOLD) zone = Zone::High;
 else
   zone = Zone::inZone;

 inZone = (zone == Zone::inZone);
 return zone;
}



void CSensor::filter(int numSamples, double t) {
  double tInv = 1.0 - t;
  int sensor = getPin();

  double v = _lastV < 0 ? static_cast<double>(analogRead(sensor)) : _lastV; 

  for (int i = 0; i < numSamples; ++i)
    v = t * static_cast<double>(analogRead(sensor)) + tInv * v;

  _lastV = v;
  _lastValue = static_cast<uint16_t>(v);

  _updateZone();
}