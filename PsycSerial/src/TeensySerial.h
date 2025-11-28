#pragma once

#include "SerialHelper.h"

using namespace System;
using namespace System::Threading::Tasks;

namespace PsycSerial
{

    public ref class TeensySerial : public SerialHelper
    {
    public:
        TeensySerial(String^ version);
        
        virtual ~TeensySerial();
        !TeensySerial();

		bool Open() { return Open(PortName); }
        bool Open(String^ portName);

		Task<bool>^ OpenAsync() { return OpenAsync(PortName); }
        Task<bool>^ OpenAsync(String^ portName);


		property String^ DeviceVersion { String^ get() { return m_deviceVersion; } }


    private:
		Task^ m_handshakeTask;
		Task^ PerformHandshake();
        bool PerformAsyncConnectionSequence();

		String^ m_programVersion;
		String^ m_deviceVersion;
        CancellationTokenSource^ m_handshakeCts;

		static const int BAUDRATE = 115200*8;
        static array<Byte>^ HOST_ACKNOWLEDGE = System::Text::Encoding::UTF8->GetBytes("HOST_ACK"); 
		static array<Byte>^ DEVICE_ACKNOWLEDGE = System::Text::Encoding::UTF8->GetBytes("DEVICE_ACK");
    };

}