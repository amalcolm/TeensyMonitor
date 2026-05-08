#include "CAutoPot.h"
#include "Arduino.h"
#include "SPI.h"
#include "HWforState.h"
#include "Setup.h"

static std::array<int, 48> _potValueCache = {-2};

CAutoPot::CAutoPot(int csPin, int sensorPin, int samplesToAverage) {
  _csPin = csPin;
  _sensorPin = sensorPin;
  _samplesToAverage = samplesToAverage > 0 ? samplesToAverage : 1;

  if (_potValueCache[0] == -2)
    _potValueCache.fill(-1); // fill cache with invalid values
}

// The virtual destructor definition is required.
CAutoPot::~CAutoPot() {}

void CAutoPot::begin(int initialLevel) {
  pinMode(_csPin, OUTPUT);
  digitalWrite(_csPin, HIGH);
  reset(initialLevel);
}

void    CAutoPot::reset(int level) {
  _setLevel(level);
  _runningAverage.reset(SENSOR_MIDPOINT); 
  delayMicroseconds(10);
  readSensor();
}

void    CAutoPot::invert()         { _inverted       = !_inverted;       }
void    CAutoPot::invertSensor()   { _invertedSensor = !_invertedSensor; }

void CAutoPot::_setLevel(int newLevel) { 
  newLevel = std::clamp(newLevel, POT_MIN, POT_MAX);
  if (newLevel == _currentLevel) return; 

  if (HW) HW->flags.offsetsChanged = true;
  _currentLevel = newLevel;
  _writeToPot(_currentLevel);
};

void CAutoPot::_offsetLevel(int offset) {
  int newLevel = std::clamp(_currentLevel + offset, POT_MIN, POT_MAX);
  if (newLevel == _currentLevel) return;

  if (HW) HW->flags.offsetsChanged = true;
  _currentLevel = newLevel;
  _writeToPot(_currentLevel);
};


CAutoPot::Zone CAutoPot::_updateZone() {
 static constexpr int       DEADZONE = 64;

 static constexpr int  LOW_THRESHOLD =        DEADZONE;
 static constexpr int HIGH_THRESHOLD = 1023 - DEADZONE;

 if (_lastSensorValue <  LOW_THRESHOLD) zone = Zone::Low;
 else
 if (_lastSensorValue > HIGH_THRESHOLD) zone = Zone::High;
 else
   zone = Zone::inZone;

 inZone = (zone == Zone::inZone);
 return zone;
}

uint16_t CAutoPot::readSensor(int samplesToAverage) {  if (_sensorPin < 0) return 0; // No sensor pin defined
  
  analogRead(_sensorPin); // discard first reading as it can be inaccurate right after writing to pot

  if (samplesToAverage <= 0) // use default if not specified
   samplesToAverage = _samplesToAverage;

  if (samplesToAverage <= 1) {
    int rawValue = analogRead(_sensorPin);

    _lastSensorValue = _invertedSensor ? 1023 - rawValue : rawValue;
  }
  else
  {
    int totalValue = 0;
    for (int i = 0; i < samplesToAverage; i++)
        totalValue += analogRead(_sensorPin);

    if (_invertedSensor)
        totalValue = (samplesToAverage * 1023) - totalValue;

    _lastSensorValue = totalValue / samplesToAverage;
    _runningAverage.add(_lastSensorValue);
  }

  _updateZone();

  return static_cast<uint16_t>(_lastSensorValue);
}

void CAutoPot::_writeToPot(int value) { if (_csPin < 0) return;
  static const SPISettings settings{8'000'000, MSBFIRST, SPI_MODE0};


  if (value < 0 || value > 255) return;
  
  if (_potValueCache[_csPin] == value) return; // No change — avoid redundant SPI write
  _potValueCache[_csPin] = value; // Update cache with new value


  uint8_t potValue = _inverted ? static_cast<uint8_t>(255-value) 
                               : static_cast<uint8_t>(    value);

  SPI.beginTransaction(settings);
  {
      digitalWrite(_csPin, LOW);
      delayMicroseconds(2);

      SPI.transfer(0x00);  // Address for wiper
      SPI.transfer(potValue);

      digitalWrite(_csPin, HIGH);
      delayMicroseconds(2);
  }
  SPI.endTransaction();
} 
