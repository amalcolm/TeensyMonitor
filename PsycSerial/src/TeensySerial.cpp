#include "TeensySerial.h"

namespace PsycSerial
{
	TeensySerial::TeensySerial(String^ version) : m_programVersion(version + "\n"), SerialHelper(CallbackPolicy::ThreadPool)
	{
	}



	TeensySerial::~TeensySerial()
	{

	}

	TeensySerial::!TeensySerial()
	{
		if (m_handshakeTask != nullptr && !m_handshakeTask->IsCompleted)
		{
			m_handshakeCts->Cancel();
			m_handshakeTask->Wait(1000);
		}
	}

	Task^ TeensySerial::PerformHandshake()
	{
		m_connectionState = ConnectionState::HandshakeInProgress;
		
		m_handshakeCts = gcnew CancellationTokenSource();
		CancellationToken token = m_handshakeCts->Token;
		try
		{
			// Simulate handshake process
			while (!token.IsCancellationRequested)
			{
				Clear();
				Write(HOST_ACKNOWLEDGE);
				auto received = m_handshakeEvent->WaitOne(500);
				if (received) {
					if (TestHandshakeResponse(DEVICE_ACKNOWLEDGE)) {

						Clear();
						Write(m_programVersion);
						received = m_handshakeEvent->WaitOne(500);
						if (received)
						{
							m_deviceVersion = GetHandshakeResponse();
 							m_connectionState = ConnectionState::HandshakeSuccessful;

							Clear();
							RaiseConnectionChangedEvent(m_connectionState);

							return Task::FromResult(true);
						}
					}
				}
				else
					OutputDebugString(L"No response after sending host acknowledge.\r\n");

				Task::Delay(2000, token)->Wait();
			}

			m_connectionState = ConnectionState::Disconnected;

			return Task::FromResult(true);
		}
		catch (Exception^ ex)
		{
			RaiseErrorOccurredEvent(ex);
			return Task::FromResult(false);
		}
	} 
}