#include "CUSB.h"
#include "XCommands.h"
#include "HWforState.h"
#include <algorithm>
#include <cstring>
#include "Setup.h"

struct PayloadInfo { uint8_t id; size_t size; };

const std::array<PayloadInfo, 2> s_payloads = {{
  {XCMD_SetWipers::ID, sizeof(XCMD_SetWipers)},
  {XCMD_SetState::ID,  sizeof(XCMD_SetState)}
}};

void CUSB::do_read() {
  uint32_t nAvailable = Serial.available();
  if (nAvailable == 0) return;

  if (m_handshakeComplete == false) {
    doHandshake();
    return;
  }


  uint32_t bufferSize = static_cast<uint32_t>(m_readBuffer.size());

  uint32_t nBytesToRead = std::min(nAvailable, bufferSize - m_numBuffered);

  uint8_t* pBuffer = m_readBuffer.data();
  uint8_t* pWrite = pBuffer + m_numBuffered;
  while (nBytesToRead > 0 && Serial.available() > 0) {

    int charsRead = Serial.readBytes(reinterpret_cast<char*>(pWrite), nBytesToRead);
    if (charsRead <= 0) break;
    pWrite += charsRead;
    nBytesToRead -= charsRead;
  }

  uint8_t* pRead = pBuffer;

  while (pWrite - pRead >= 4) { // need at least 4 bytes to read a header

    if (pRead[0] != XCMD_MAGIC[0] || pRead[1] != XCMD_MAGIC[1] || pRead[3] != XCMD_MAGIC[3]) {
      pRead++;
      continue;
    }

    uint8_t id = pRead[2];
    uint32_t packetSize = 0;
    for (const auto& [payloadId, size] : s_payloads)
      if (payloadId == id) packetSize = size;

    if (packetSize == 0) {
      pRead += 4;
      continue;
    }

    if (pWrite - pRead < packetSize) break; // wait for more data

    XCMD_Header header;
    std::memcpy(&header, pRead, sizeof(header));
    bool handledCommand = true;
  

    switch (id) {
      case XCMD_SetWipers::ID: { XCMD_SetWipers cmd; std::memcpy(&cmd, pRead, packetSize);

        HW->setWipers(cmd);
        break;
      }

      case XCMD_SetState::ID:  { XCMD_SetState cmd; std::memcpy(&cmd, pRead, packetSize);

        LED.writeState(cmd.state);
        break;
      }

      default:
        handledCommand = false;
        break;
    }

    if (handledCommand && hasFlag(header.flags, CommandFlags::HoldWipers))
      HW->flags.holdWipers = true;

    pRead += packetSize;
  }

  m_numBuffered = pWrite - pRead;
  if (m_numBuffered > 0 && pRead != pBuffer)
    std::memmove(pBuffer, pRead, m_numBuffered);
}
