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

	bool TeensySerial::Open(String^ portName)
	{
		if (m_handshakeTask != nullptr && !m_handshakeTask->IsCompleted) { RaiseErrorOccurredEvent(gcnew System::Exception("Previous handshake still running")); return false; }
		if (!SerialHelper::Open(portName, BAUDRATE)) { RaiseErrorOccurredEvent(gcnew System::Exception("Failed to open port")); return false; }


		m_handshakeTask = Task::Run(gcnew Func<Task^>(this, &TeensySerial::PerformHandshake));
		return true;
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

	
	Task<bool>^ TeensySerial::OpenAsync(String^ portName)
	{
		if (m_handshakeTask != nullptr && !m_handshakeTask->IsCompleted) {
			RaiseErrorOccurredEvent(gcnew System::Exception("Previous handshake still running"));
			return Task::FromResult(false);
		}

		this->PortName = portName; 

		return Task::Run(gcnew Func<bool>(this, &TeensySerial::PerformAsyncConnectionSequence));
	}

	bool TeensySerial::PerformAsyncConnectionSequence()
	{
		// 1. Open Physical Port (Uses the member variable m_portName we just set)
		if (!SerialHelper::Open()) {
			RaiseErrorOccurredEvent(gcnew System::Exception("Failed to open port"));
			return false;
		}

		// 2. Perform Handshake (Blocking wait is safe here strictly because we are in a Task)
		try {
			m_handshakeTask = this->PerformHandshake();
			m_handshakeTask->Wait(); // Wait for the handshake task to finish
		}
		catch (Exception^ ex) {
			RaiseErrorOccurredEvent(ex);
			return false;
		}

		return (m_connectionState == ConnectionState::HandshakeSuccessful);
	}
}