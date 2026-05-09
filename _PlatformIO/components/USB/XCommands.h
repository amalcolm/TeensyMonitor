#pragma once
#include <stdint.h>

static const uint8_t XCMD_MAGIC[4] = {0x58, 0x43, 0x00, 0xFF};

struct XCMD_SetWipers {
  static constexpr uint8_t ID = 0x01;
  
  uint8_t top;
  uint8_t bot;
  uint8_t mid;

  uint8_t offset;
  uint8_t gain;

  uint8_t _reserved1;
  uint8_t _reserved2;
  uint8_t _reserved3;
};