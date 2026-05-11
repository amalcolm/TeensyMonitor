#pragma once
#include <stdint.h>

static const uint8_t XCMD_MAGIC[4] = {0x58, 0x43, 0x00, 0xFF};

struct XCMD_SetWipers {
  static constexpr uint8_t ID = 0x01;
  static constexpr uint8_t FLAG_HOLD = 0x01;
  
  uint8_t top;
  uint8_t bot;
  uint8_t mid;

  uint8_t offset;
  uint8_t gain;

  uint8_t flags; // bit 0 = hold
  uint8_t _reserved2;
  uint8_t _reserved3;
};

struct XCMD_SetState {
  static constexpr uint8_t ID = 0x02;

  uint32_t state; // bitfield for LEDs

  uint32_t flags; // bit 0 = hold

};
