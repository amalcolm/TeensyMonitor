#pragma once
#include "CRunningAverage.h"
#include <utility>
#include <deque>

class CAutoPot {
public:
  bool inZone = false;
  enum class Zone { Low = -1, inZone = 0, High = +1, Placeholder = 255} ;
  Zone zone = Zone::Placeholder;
  static constexpr int SENSOR_MIDPOINT = 512;
  static constexpr int POT_MIN = 0;
  static constexpr int POT_MAX = 255;
  static constexpr int POT_MIDPOINT = (POT_MAX + POT_MIN) / 2;

  CAutoPot(int csPin, int sensorPin, int samplesToAverage);
  virtual ~CAutoPot(); // Needed to call destructor of subclass

  void begin(int initialLevel = 128);
  void reset(int level);
  void invert();
  void invertSensor();
  virtual void update() = 0; // must be overridden

  uint16_t   readSensor(int samplesToAverage = -1);

  inline int getLevel()        const { return _currentLevel;    }
  inline int lastSensorValue() const { return _lastSensorValue; }
  inline int getSensorPin()    const { return _sensorPin;       }

  inline virtual void writeCurrentToPot() { _writeToPot(_currentLevel); }
  
  CRunningAverageMinMax<uint16_t>& getRunningAverage() { return _runningAverage; }


protected:

  void _setLevel(int newLevel);
  void _offsetLevel(int offset);
  Zone _updateZone();

  int _csPin; 
  int _sensorPin;
  int _samplesToAverage;
  int _currentLevel = -1;
  int _lastSensorValue = 0;

  bool _inverted = false;
  bool _invertedSensor = false;
  CRunningAverageMinMax<uint16_t> _runningAverage;

private:
  void _writeToPot(int value);
};

// =================================================================
class CDigiPot : public CAutoPot {
public:
  CDigiPot(int csPin) : CAutoPot(csPin, -1, 1) {}
  CDigiPot(int csPin, int sensorPin, int samplesToAverage) : CAutoPot(csPin, sensorPin, samplesToAverage) {}
  CDigiPot& operator=(const CDigiPot&) = default;
  void update() override { } // no update needed but override is required to be non-abstract
  void setLevel(int level) { _setLevel(level); }              // expose set and offset level
  void offsetLevel(int offset) { _offsetLevel(offset); }
};
// =================================================================


#include "Stage 1/CStage1.h"
#include "Stage 2/CStage2.h"
