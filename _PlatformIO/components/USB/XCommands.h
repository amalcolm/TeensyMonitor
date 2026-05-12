#pragma once
#include <stdint.h>

static const uint8_t XCMD_MAGIC[4] = {0x58, 0x43, 0x00, 0xFF};

enum class CommandFlags : uint32_t {
  None = 0,
  HoldWipers = 0x01
};

inline bool hasFlag(CommandFlags flags, CommandFlags flag) {
  return (static_cast<uint32_t>(flags) & static_cast<uint32_t>(flag)) != 0;
}
struct XCMD_Header
{
  uint8_t magic0;
  uint8_t magic1;
  uint8_t id;
  uint8_t magic3;
  CommandFlags flags;
};


struct XCMD_SetWipers {
  static constexpr uint8_t ID = 0x01;
  XCMD_Header header; // must be first field
  
  uint8_t top;
  uint8_t bot;
  uint8_t mid;

  uint8_t offset;
  uint8_t gain;

  uint8_t _reserved1;
  uint8_t _reserved2;
  uint8_t _reserved3;
};

static_assert(sizeof(XCMD_Header) == 8);
static_assert(sizeof(XCMD_SetWipers) == 16);

struct XCMD_SetState {
  static constexpr uint8_t ID = 0x02;
  XCMD_Header header; // must be first field

  uint32_t state; // bitfield for LEDs


};

static_assert(sizeof(XCMD_SetState) == 12);

struct XCMD_SetDebugFlags {
  static constexpr uint8_t ID = 0x03;
  XCMD_Header header; // must be first field

  uint32_t debugFlags; // bitfield for various debug options
};

static_assert(sizeof(XCMD_SetDebugFlags) == 12);
