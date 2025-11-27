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

        property String^ PortName {
            String^ get()              { return SerialHelper::PortName; }
            void    set(String^ value) { Open(value);                   }
		}

        bool Open(String^ portName)
        {
            if (m_handshakeTask != nullptr && !m_handshakeTask->IsCompleted) { RaiseErrorOccurredEvent(gcnew System::Exception("Previous handshake still running")); return false; }
            if (!SerialHelper::Open(portName, BAUDRATE))                     { RaiseErrorOccurredEvent(gcnew System::Exception("Failed to open port")); return false; }

            m_handshakeTask = Task::Run(gcnew Func<Task^>(this, &TeensySerial::PerformHandshake));
            return true;
		}

    protected: 
        property String^ DeviceVersion { String^ get() { return m_deviceVersion; } }

    private:
		Task^ m_handshakeTask;
		Task^ PerformHandshake();

		String^ m_programVersion;
		String^ m_deviceVersion;
        CancellationTokenSource^ m_handshakeCts;

		static const int BAUDRATE = 115200*8;
        static array<Byte>^ HOST_ACKNOWLEDGE = System::Text::Encoding::UTF8->GetBytes("HOST_ACK"); 
		static array<Byte>^ DEVICE_ACKNOWLEDGE = System::Text::Encoding::UTF8->GetBytes("DEVICE_ACK");
    };

}