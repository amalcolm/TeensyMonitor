#include "HWforState.h"
#include "DataTypes.h"
#include "Hardware.h"
#include "CUSB.h"

const int midLevel = 512;

void HWforState::_findSignal()
{
  static constexpr int MAX_ITERATIONS = 400;
  top.setLevel(CDigiPot::POT_MAX);
  bot.setLevel(CDigiPot::POT_MIN);
  mid.setLevel(midLevel);

  delayMicroseconds(10);

  int wiper = mid.getLevel();
  int Wtop = 255, Wbot = 0;

  while (Wtop - Wbot > GAP_TOPBOT*2) {
    if (sensor1.read() < midLevel) {
      Wbot = wiper;
      wiper = (wiper + Wtop) / 2;
    } else {
      Wtop = wiper;
      wiper = (wiper + Wbot) / 2;
    }
    mid.setLevel(wiper);
    delayMicroseconds(10);
  }



  bool signalFound = false;

  int initialHILO = sensor1.read() < midLevel ? -1 : +1;
  int HILO = 0;

  for (int i = 0; top.getLevel() - bot.getLevel() > GAP_TOPBOT && i < MAX_ITERATIONS; i++) {

    HILO = (sensor1.read() < midLevel) ? -1 : +1;

    switch (signalFound)
    {
      case false:
        if (HILO == initialHILO)
          mid.offsetLevel( -HILO );
        else
          signalFound = true;
        break;


      case true:  // called once signal is found

        switch (HILO) {
          case -1: top.offsetLevel(-1); break;
          case +1: bot.offsetLevel(+1); break;
        }


        if (mid.getLevel() != midLevel) {

          int delta = mid.getLevel() - midLevel;
          int sign = (delta > 0) - (delta < 0);
          delta = sign * std::clamp(abs(delta) * 1/4, 1, 3);

          mid.offsetLevel( -delta  );  // drag mid to centre
        }
        break;
    }

    delayMicroseconds(5); // signalFound ? 500 : 50 );
  }

  initialHILO = sensor1.read() < midLevel ? -1 : +1;
  double lastDelta = 0, delta = 0;
  
  for (int i = 0; i < MAX_ITERATIONS; i++) { 
    lastDelta = delta;
    sensor1.read();
    
    delta = abs(sensor1.lastValue() - midLevel);
    HILO = (sensor1.lastValue() < midLevel) ? -1 : +1;

    if (HILO != initialHILO)
      break;

    mid.offsetLevel( -HILO );
    delayMicroseconds(5);
  }

  if (delta < lastDelta)  // if we just crossed over the optimal point, step back
  { 
    mid.offsetLevel( +HILO );
    delayMicroseconds(5);
    sensor1.read();
  }

  phase = Phase::NORMAL;
  offset.reset(128);
  gain.reset(0);
}