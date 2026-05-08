#pragma once
#include <Arduino.h>
#include "PinHelpers.h"
#include "Helpers.h"
#include "CMasterTimer.h"
#include <string>


extern bool Ready;                 // set to true once setup() is complete 


extern struct ChipSelectPins    CS; 
extern struct SensorPins        SP;
extern struct ButtonPins        BUT;
extern class  LEDpins           LED;

extern class  CMasterTimer      Timer;
extern class  CA2D              A2D;
extern class  CHead             Head;
extern class  CUSB              USB;


struct ChipSelectPins {
  static constexpr int Stage1_TOP = 17;
  static constexpr int Stage1_BOT = 16;
  static constexpr int Stage1_MID = 23;

  static constexpr int offset2    = 21;
  static constexpr int gain       = 22;
  
  static constexpr int A2D        = 20;
};

struct SensorPins {
  static constexpr int Stage1 = PIN_A1;
  static constexpr int Final  = PIN_A0;
};

struct ButtonPins {
//  InputPin halt{17};

  void begin() const {
//    halt.begin();
  }
};
extern OutputPin activityLED;  // set in Helpers.cpp (4)


