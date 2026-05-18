#include "CSensor.h"
#include "HWforState.h"
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
 static constexpr int       DEADZONE = 128;

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



double CSensor::resetFilter() {
  _lastV = static_cast<double>(read());
  return _lastV;
}



void CSensor::filter(int numSamples, double t) {
  double tInv = 1.0 - t;
  int sensor = getPin();

  if (HW->flags.wipersChanged) { _lastV = -1; HW->flags.wipersChanged = false; }
  
  read(); // update _lastValue and zone
  if (inZone == false) {_lastV = -1; return; }
   
  uint16_t rawValue = _inverted ? 1023 - _lastValue : _lastValue;

  if (numSamples <= 1) {
    if (_lastV < 0)
      _lastV = static_cast<double>(rawValue);
    else
      _lastV = t * static_cast<double>(rawValue) + tInv * _lastV;
    
    return;
  }

  _lastV = _lastV < 0 ? static_cast<double>(_lastValue) : _lastV; 

  for (int i = 1; i < numSamples; ++i)
    _lastV = t * static_cast<double>(analogRead(sensor)) + tInv * _lastV;

  uint16_t quantised = static_cast<uint16_t>(_lastV + 0.5); 
  _lastValue = _inverted ? 1023 - quantised : quantised;

  _updateZone();
}
