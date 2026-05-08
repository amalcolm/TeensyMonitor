#pragma once
#include "CAutoPot.h"
#include "COffsetPot.h"
#include "CGainPot.h"

class CStage2 : public CAutoPot {
public:
  CStage2(int csPinOffset, int csPinGain, int sensorPin);
  CStage2& operator=(const CStage2&) = default;

  COffsetPot offsetPot;
  CGainPot   gainPot;

  void begin();
  void reset();
  void update() override;

  void set();

  void filterSensor(int numSamples, double t);
  double _lastV = -1.0;

private:
  void _updateZone(bool readSensor = true);
};