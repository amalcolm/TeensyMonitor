#pragma once
#include "Setup.h"
#include "Helpers/CDigiPot.h"
#include "Helpers/CSensor.h"
#include "XCommands.h"

struct HWforState {
  public:
    StateType state;
    HWforState(StateType state);

    struct HWflags {
      bool begun = false;
      bool holdWipers = false;
      bool wipersChanged = true;

      bool inZone = false;
      double lastV = 0.0;

      void dbg();
    } flags;


    CDigiPot       top{CS.Top};
    CDigiPot       bot{CS.Bot};
    CDigiPot       mid{CS.Mid};

    CDigiPot       offset{CS.offset2};
    CDigiPot       gain{CS.gain};

    CSensor        sensor1{SP.Sensor1};
    CSensor        sensor2{SP.Final};
    
    void begin(); // ensure hardware is configured
    // update hardware instances based on current sensor readings, and write to hardware if needed
    void update() { if (!Ready) return; else if (!flags.begun) begin();

      Timer.addEvent(EventKind::HW_UPDATE_START);

      _update();
//    flags.dbg();  // defined in _DBG.cpp
      
      Timer.addEvent(EventKind::HW_UPDATE_COMPLETE);
    }

    // write current state of hardware instances to hardware devices
    void set();

    void setWipers(XCMD_SetWipers& cmd);

  private:
    void _update(); 
    void _findSignal();
    void _fineTuning();

    enum class Phase { SEARCH = 0, NORMAL = 1, placeholder = 255} phase = Phase::placeholder;


    static constexpr int HISTORY_SIZE =  4;
    static constexpr int GAP_TOPBOT   = 24;
    static inline    int MID_STEP     = 17;  // depends on GAP_TOPBOT - set in contructor

    using Zone = CSensor::Zone;
};

extern HWforState* HW;
