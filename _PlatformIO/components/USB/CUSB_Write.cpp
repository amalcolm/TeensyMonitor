#include "CUSB.h"
#include "CA2D.h"
#include "Setup.h"
#include "CTelemetry.h"


void CUSB::do_write() {
  // Write data based on current mode
  switch (getMode())
  {
    case CSerialWrapper::ModeType::RAWDATA:   do_write_Data();  break;
    case CSerialWrapper::ModeType::BLOCKDATA: do_write_Block(); break;
    case CSerialWrapper::ModeType::TEXT:      do_write_Text();  break;
    default: break;
  }

  // Always write (and clear) any buffered telemetry
  do_write_Telemetry();
}

// Sends all buffered DataType items over USB
void CUSB::do_write_Data() {
  while (m_dataBuffer.isEmpty() == false) {//  if (firstOut == 0) firstOut = m_buffer[readIndex].timestamp;

    DataType* pData = m_dataBuffer.read();

    if (pData == nullptr) break;
    
    if (m_handshakeComplete)
      pData->writeSerial();
    else
      pData->debugSerial();
  }
}



// Sends the buffered BlockType over USB
void CUSB::do_write_Block() {
  if (m_pBlock == NULL) return;
  
  if (m_handshakeComplete)
    m_pBlock->writeSerial();
  else
    if (A2D.outputDebugBlock)
      m_pBlock->debugSerial();

  m_pBlock = NULL;
}


// if in TEXT mode, output the debugSerial of block
void CUSB::do_write_Text() {
  if (m_pBlock == NULL) return;
    m_pBlock->debugSerial();

  m_pBlock = NULL;
}


// Sends all buffered Telemetry items over USB
void CUSB::do_write_Telemetry() {

  while (m_telemetryBuffer.isEmpty() == false) {
      CTelemetry** telemetry = m_telemetryBuffer.read();
      if (telemetry == nullptr) break;

      if (m_handshakeComplete)
          (*telemetry)->writeSerial();
        
      CTelemetry::Return(*telemetry);
  }
}

