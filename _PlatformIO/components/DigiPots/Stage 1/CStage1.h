#pragma once
#include "CAutoPot.h"

class CStage1 : public CDigiPot {

  public:
    enum class Phase { SEARCH = 0, NORMAL = 1, placeholder = 255} phase = Phase::placeholder;


    static constexpr int HISTORY_SIZE =  4;
    static constexpr int GAP_TOPBOT   = 24;
    static inline    int MID_STEP     = 17;  // depends on GAP_TOPBOT - set in contructor


    CStage1(int csPinTop, int csPinBot, int csPinMid, int sensorPin);

    CDigiPot  top;
    CDigiPot  bot;
    CDigiPot& mid; // also the CAutoPot instance

    bool inZone = false;

    struct State {
      Phase       phase = Phase::placeholder;
      uint16_t   sensor = 0;
      int      topLevel = 0;
      int      botLevel = 0;
      int      midLevel = 0;
    } state{};

    State history[HISTORY_SIZE]{}; // history[0] = newest
    void clearHistory();
    void storeOldState();
    void setState();
    
    void begin(int initialLevel = CAutoPot::POT_MIDPOINT) {
      top.invert();
      bot.invert();
      mid.invertSensor();

      top.begin(CAutoPot::POT_MAX);
      bot.begin(CAutoPot::POT_MIN);
      mid.begin(initialLevel);
    }

    void update();

    void findSignal(); 
    void fineTuning();

    inline void set() {
      top.writeCurrentToPot();
      bot.writeCurrentToPot();
      mid.writeCurrentToPot();
    }

    void printDebug(bool signalFound);
};
