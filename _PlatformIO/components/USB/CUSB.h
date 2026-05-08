#pragma once
#include "CSerialWrapper.h"
#include "CrashReport.h"
#include "CBuffer.h"
#include <array>

class CUSB : public CSerialWrapper {
  private:
    static constexpr size_t      READ_BUFFER_SIZE = 1024;  // holds raw bytes read from USB    (read)  (1024 bytes)
    static constexpr size_t  DATATYPE_BUFFER_SIZE =   64;  // holds data for buffer(dataType); (write) (64 * sizeof(DataType) = 1.5KB)
    static constexpr size_t TELEMETRY_BUFFER_SIZE =  128;  // holds pointers to CTelemetry items to write

    CBufferType<DataType>  m_dataBuffer = CBufferType<DataType>(DATATYPE_BUFFER_SIZE);
    BlockType* m_pBlock     = nullptr;  // single block is buffered, (see implementation in CA2D - swapps between two blocks)

    CBufferType<CTelemetry*> m_telemetryBuffer = CBufferType<CTelemetry*>(TELEMETRY_BUFFER_SIZE);  // stores pointers, not actual items

    std::array<uint8_t, READ_BUFFER_SIZE> m_readBuffer;  // temporary buffer for reading raw bytes from USB
    int m_numBuffered = 0;  // number of bytes currently buffered in m_readBuffer


  public:
    CUSB() {};

    CUSB& begin()
    { 
      CSerialWrapper::begin(); 
      return *this;
    }
    
    inline void buffer(DataType    data     ) { m_dataBuffer.write(data); }
    inline void buffer(BlockType*  block    ) { m_pBlock = block; }
    inline void buffer(CTelemetry* telemetry) { m_telemetryBuffer.write(telemetry); }
    
    void update() { 

      CSerialWrapper::update();

      do_read();
      do_write();

    };

    void clearBuffers() {
      m_dataBuffer.clear();
      m_telemetryBuffer.clear();

      m_readBuffer.fill(0);
      m_numBuffered = 0;
    }

    inline void waitForHandshake() {
      while (!m_handshakeComplete) {
        update();
        yield();
      }
    }

  private:
    void do_read();
   
    void do_write();
    void do_write_Data();
    void do_write_Block();
    void do_write_Text();
    void do_write_Telemetry();
    
};

extern CUSB USB;
