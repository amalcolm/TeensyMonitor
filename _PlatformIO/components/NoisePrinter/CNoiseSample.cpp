#include "CNoiseSample.h"
#include <Arduino.h>
#include "C32bitTimer.h"
#include <deque>

C32bitTimer& getTimer(double period);

void FillBufferWithNoise(TimedSample* buffer, size_t size, double period) {
  C32bitTimer& noiseTimer = getTimer(period);

//  analogReadResolution(10);
//  analogReadAveraging(0);
 
  noiseTimer.reset();
  bool wait = period > 0.0;
  for (size_t i = 0; i < size; i++) {
    buffer[i].startTick = noiseTimer.getTicks();
    buffer[i].sample = analogRead(A0);
    buffer[i].endTick = noiseTimer.getTicks();

    if (wait) noiseTimer.wait();
  }

//  analogReadAveraging(4); // restore default averaging
}


C32bitTimer& getTimer(double period) {
  static std::deque<std::pair<double, C32bitTimer>> s_timers;
  
  for (auto& [p, timer] : s_timers) if (p == period) return timer;

  s_timers.emplace_back(period, C32bitTimer::From_S(period));
  return s_timers.back().second;
}