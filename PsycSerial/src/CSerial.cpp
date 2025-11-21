#include "CSerial.h"
#pragma managed(push, off)

#include <chrono>
#include <thread>
#include <iostream>
#include <memory>
#include <atomic>
#include <sstream>
#include <stdexcept> // Include for std::exception

// Error handling macro for Windows API calls
#define CHECK(expr, handle, errorMsg) \
    if (!(expr)) { \
        DWORD error = GetLastError(); \
        if (handle != INVALID_HANDLE_VALUE) CloseHandle(handle); \
        std::ostringstream os; \
        os << errorMsg << " Error: " << error; \
        std::cerr << os.str() << std::endl; \
        InvokeErrorOccurred(std::runtime_error(os.str())); \
        return false; \
    }

CSerial::CSerial()
    : m_hSerial(INVALID_HANDLE_VALUE)
    , m_isOpen(false)
    , m_baudRate(DEFAULT_BAUDRATE)
    , m_stopReadLoop(false)
	, m_dataHandler(nullptr)
	, m_connectionHandler(nullptr)
	, m_errorHandler(nullptr)
	, m_readThread()
	, m_mutex()

{ }


CSerial::~CSerial() {
    Close();  // Ensure proper cleanup
}


void CSerial::InvokeConnectionChanged(bool state) {
    ConnectionHandler handler = nullptr;
    void* context = nullptr;
    {
        std::lock_guard<std::mutex> lock(m_mutex);
        handler = m_connectionHandler;
        context = m_userData; // Get user data under lock
    }

    // Invoke outside the lock
    if (handler) {
        try {
            handler(context, this, state); // Pass user data
        }
        catch (const std::exception& e) {
            std::cerr << "CSerial: Exception caught during ConnectionChanged callback: " << e.what() << std::endl;
            // Avoid calling InvokeErrorOccurred from here to prevent potential infinite loops
        }
        catch (...) {
            std::cerr << "CSerial: Unknown exception caught during ConnectionChanged callback." << std::endl;
        }
    }
}


void CSerial::InvokeErrorOccurred(const std::exception& ex) {
    ErrorHandler handler = nullptr;
    void* context = nullptr;
    {
        std::lock_guard<std::mutex> lock(m_mutex);
        handler = m_errorHandler;
        context = m_userData; // Get user data under lock
    }

    // Invoke outside the lock
    if (handler) {
        try {
            handler(context, this, ex); // Pass user data
        }
        catch (const std::exception& e) {
            std::cerr << "CSerial: Exception caught during ErrorOccurred callback: " << e.what() << std::endl;
        }
        catch (...) {
            std::cerr << "CSerial: Unknown exception caught during ErrorOccurred callback." << std::endl;
        }
    }
}

void CSerial::InvokeDataReceived(const Packet& packet) {
    DataHandler handler = nullptr;
    void* context = nullptr;
    {
        std::lock_guard<std::mutex> lock(m_mutex);
        handler = m_dataHandler;
        context = m_userData; // Get user data under lock
    }

    // Invoke outside the lock
    if (handler) {
        try {
            handler(context, this, packet); // Pass user data
        }
        catch (const std::exception& e) {
            std::cerr << "CSerial: Exception caught during DataReceived callback: " << e.what() << std::endl;
            // Optionally invoke error handler here if desired, carefully
            // InvokeErrorOccurred(std::runtime_error("Exception in DataReceived callback: " + std::string(e.what())));
        }
        catch (...) {
            std::cerr << "CSerial: Unknown exception caught during DataReceived callback." << std::endl;
            // InvokeErrorOccurred(std::runtime_error("Unknown exception in DataReceived callback"));
        }
    }
}

bool CSerial::SetPort(const std::string& portName, DataHandler dataHandler, void* userData, int baudRate) {
    // Close any existing connection first (this is fine outside the lock)
    if (IsOpen()) {
        Close();
    }

    HANDLE hSerial = INVALID_HANDLE_VALUE; // Declare outside lock scope
    const char* failStage = nullptr;

    while (true)
    {
        // --- Port Opening and Configuration ---

        std::string fullPortName = "\\\\.\\" + portName;

        hSerial = CreateFileA(
            fullPortName.c_str(),
            GENERIC_READ | GENERIC_WRITE,
            0,
            NULL,
            OPEN_EXISTING,
            FILE_FLAG_OVERLAPPED,
            NULL
        );

        if (hSerial == INVALID_HANDLE_VALUE) {
            failStage = "CreateFileA failed to open serial port.";
            break;
        }


        DCB dcbSerialParams = { 0 };
        dcbSerialParams.DCBlength = sizeof(dcbSerialParams);
        if (!GetCommState(hSerial, &dcbSerialParams)) {
            failStage = "GetCommState failed.";
            break;
        }

        dcbSerialParams.BaudRate = baudRate;
        dcbSerialParams.ByteSize = 8;
        dcbSerialParams.StopBits = ONESTOPBIT;
        dcbSerialParams.Parity = NOPARITY;
        dcbSerialParams.fBinary = TRUE;

        if (!SetCommState(hSerial, &dcbSerialParams)) {
            failStage = "SetCommState failed.";
            break;
        }

        COMMTIMEOUTS timeouts = { 0 };
        timeouts.ReadIntervalTimeout         =    0;
        timeouts.ReadTotalTimeoutConstant    =    0;
        timeouts.ReadTotalTimeoutMultiplier  =    0;
        timeouts.WriteTotalTimeoutConstant   = 1500;
        timeouts.WriteTotalTimeoutMultiplier =    0;

        if (!SetCommTimeouts(hSerial, &timeouts)) {
            failStage = "SetCommTimeouts failed.";
            break;
        }

        SetupComm(hSerial, 1 << 16, 1 << 16);           // 64K in/out buffers
        PurgeComm(hSerial, PURGE_RXCLEAR | PURGE_TXCLEAR | PURGE_RXABORT | PURGE_TXABORT);

		break; // Success
    }

    if (failStage != nullptr) {
        DWORD errorCode = GetLastError();
        if (hSerial != INVALID_HANDLE_VALUE) CloseHandle(hSerial);
        std::ostringstream os; os << failStage << ". Error: " << errorCode;
        std::cerr << os.str() << std::endl;
        InvokeErrorOccurred(std::runtime_error(os.str()));
        return false;
    }


    {
        std::lock_guard<std::mutex> g(m_mutex);
        m_userData = userData;
        m_dataHandler = dataHandler;
        m_baudRate = baudRate;
        m_hSerial.reset(hSerial);              // take ownership
        hSerial = INVALID_HANDLE_VALUE;        // prevent double-close
        m_isOpen = true;
        m_stopReadLoop = false;
    }

    // --- Phase 3: start thread and raise events out of the lock ---
    if (m_readThread.joinable()) {
        try { m_readThread.join(); }
        catch (...) {}
    }

    m_readThread = std::thread(&CSerial::ReadLoop, this);
    InvokeConnectionChanged(true);
    return true;
}


void CSerial::ReadLoop() {
    HandleGuard eventHandle(CreateEvent(NULL, TRUE, FALSE, NULL));
    if (eventHandle.get() == NULL) {
        std::cerr << "CreateEvent failed in ReadLoop. Error: " << GetLastError() << std::endl;
        InvokeErrorOccurred(std::runtime_error("CreateEvent failed in ReadLoop."));
        {
            std::lock_guard<std::mutex> lock(m_mutex);
            m_isOpen = false; // Mark as closed internally
        }
        InvokeConnectionChanged(false); // Notify closed state
        return;
    }

    OVERLAPPED overlapped = { 0 };
    overlapped.hEvent = eventHandle.get();

    BYTE buffer[READ_BUFFER_SIZE] = { 0 };
    DWORD bytesRead = 0; // Initialize bytesRead
	
    {
		std::lock_guard<std::mutex> lock(m_mutex);
		if (!m_isOpen || m_hSerial.get() == INVALID_HANDLE_VALUE) {
			std::cerr << "ReadLoop: Serial port is not open or handle is invalid." << std::endl;
			return; // Exit if port is not open
		}

		if (!PurgeComm(m_hSerial, PURGE_RXCLEAR | PURGE_TXCLEAR)) {
			DWORD error = GetLastError();
			std::cerr << "PurgeComm failed. Error: " << error << std::endl;
			InvokeErrorOccurred(std::runtime_error("PurgeComm failed."));
			return; // Exit if purge fails
		}
    }

    while (!m_stopReadLoop.load()) {
        {
            // Check if port is still valid under lock
            std::lock_guard<std::mutex> lock(m_mutex);
            if (!m_isOpen || m_hSerial.get() == INVALID_HANDLE_VALUE) {
                break; // Exit loop if port closed or handle invalid
            }
        }

        ResetEvent(overlapped.hEvent); // Reset before each ReadFile

        // Local copy of the HANDLE to use outside the lock
        HANDLE hSerialLocal;
        {
            std::lock_guard<std::mutex> lock(m_mutex);
            hSerialLocal = m_hSerial.get(); // Get handle under lock
        }
        // Check if handle became invalid between check and get (unlikely but possible)
        if (hSerialLocal == INVALID_HANDLE_VALUE) {
            break;
        }

		// Perform the read operation
        BOOL readResult = ReadFile(hSerialLocal, buffer, READ_BUFFER_SIZE, NULL, &overlapped); // Pass NULL for sync bytes read

        if (!readResult) {
            DWORD error = GetLastError();
            if (error == ERROR_IO_PENDING) {
                DWORD waitResult = WaitForSingleObject(overlapped.hEvent, IO_OPERATION_TIMEOUT);
                if (waitResult == WAIT_OBJECT_0) {
                    // Need to get handle again in case it changed (e.g., Close called)
                    HANDLE currentSerialHandle;
                    {
                        std::lock_guard<std::mutex> lock(m_mutex);
                        currentSerialHandle = m_hSerial.get();
                    }

                    if (currentSerialHandle == INVALID_HANDLE_VALUE) {
                        std::cerr << "ReadLoop: Serial handle became invalid while waiting for I/O." << std::endl;
                        break; // Exit loop
                    }

                    // Use current handle for GetOverlappedResult
                    if (!GetOverlappedResult(currentSerialHandle, &overlapped, &bytesRead, FALSE)) { // Get bytesRead here
                        DWORD overlappedError = GetLastError();
                        // Don't report error if operation was aborted (e.g., by Close/CancelIo)
                        if (overlappedError != ERROR_OPERATION_ABORTED) {
                            std::cerr << "GetOverlappedResult failed after wait. Error: " << overlappedError << std::endl;
                            InvokeErrorOccurred(std::runtime_error("GetOverlappedResult failed. Error: " + std::to_string(overlappedError)));
                        }
                        // Continue or break based on error? Continue for now, might recover.
                        // If ERROR_HANDLE_EOF or similar, maybe break.
                        continue;
                    }
                    // Successfully read 'bytesRead' bytes
                }
                else if (waitResult == WAIT_TIMEOUT) {
                    std::cerr << "WaitForSingleObject timed out in ReadLoop after " << IO_OPERATION_TIMEOUT << "ms" << std::endl;
                    InvokeErrorOccurred(std::runtime_error("Read I/O operation timed out"));
                    // Cancel pending I/O on timeout
                    {
                        std::lock_guard<std::mutex> lock(m_mutex);
                        if (m_hSerial.get() != INVALID_HANDLE_VALUE) CancelIo(m_hSerial.get());
                    }
                    continue; // Try reading again
                }
                else {
                    // Other wait failure
                    DWORD waitError = GetLastError();
                    std::cerr << "WaitForSingleObject failed in ReadLoop. WaitResult: " << waitResult << " Error: " << waitError << std::endl;
                    InvokeErrorOccurred(std::runtime_error("WaitForSingleObject failed. Error: " + std::to_string(waitError)));
                    // Cancel pending I/O on failure
                    {
                        std::lock_guard<std::mutex> lock(m_mutex);
                        if (m_hSerial.get() != INVALID_HANDLE_VALUE) CancelIo(m_hSerial.get());
                    }
                    continue; // Try reading again
                }
            }
            else if (error == ERROR_OPERATION_ABORTED) {
                // Operation was cancelled, likely due to Close() calling CancelIo. Normal shutdown.
                std::cout << "ReadLoop: I/O operation aborted. Exiting loop." << std::endl;
                break; // Exit the loop cleanly
            }
            else {
                // Other ReadFile error
                std::cerr << "ReadFile failed directly. Error: " << error << std::endl;
                InvokeErrorOccurred(std::runtime_error("ReadFile failed. Error: " + std::to_string(error)));
                
                // Consider if we should break or continue after an error
                std::this_thread::sleep_for(std::chrono::milliseconds(100));
                continue;
            }
        }
        else {
            // ReadFile completed synchronously (unlikely with OVERLAPPED but possible)
            // We still need GetOverlappedResult to get the number of bytes read
            HANDLE currentSerialHandle;
            {
                std::lock_guard<std::mutex> lock(m_mutex);
                currentSerialHandle = m_hSerial.get();
            }
            if (currentSerialHandle == INVALID_HANDLE_VALUE) {
                std::cerr << "ReadLoop: Serial handle became invalid after sync ReadFile." << std::endl;
                break; // Exit loop
            }
            if (!GetOverlappedResult(currentSerialHandle, &overlapped, &bytesRead, FALSE)) {
                DWORD overlappedError = GetLastError();
                if (overlappedError != ERROR_OPERATION_ABORTED) {
                    std::cerr << "GetOverlappedResult failed after sync read. Error: " << overlappedError << std::endl;
                    InvokeErrorOccurred(std::runtime_error("GetOverlappedResult failed (sync). Error: " + std::to_string(overlappedError)));
                }
                continue;
            }
            // Successfully read 'bytesRead' bytes synchronously
        }


        // Process data if bytes were read
        if (bytesRead > 0) {
            Packet packet;
            GetSystemTime(&packet.timestamp); // Get current timestamp
            packet.data.assign(buffer, buffer + bytesRead);  // Copy data
            packet.bytesRead = bytesRead;

            InvokeDataReceived(packet); // Use the safe invocation helper
            bytesRead = 0; // Reset for next read
        }
        else {
            // Read completed successfully but returned 0 bytes (e.g., timeout occurred internally?)
            // Can happen if ReadIntervalTimeout is used differently, but with MAXDWORD it shouldn't timeout this way.
            // Could indicate EOF on some devices. For serial ports, often just means no data available yet.
            // Add a small sleep to prevent busy-waiting if reads return 0 frequently.
//            std::this_thread::sleep_for(std::chrono::milliseconds(1));
        }
    } // end while (!m_stopReadLoop)

    std::cout << "ReadLoop: Exiting." << std::endl;
}


bool CSerial::Write(const std::string& data) {
    // Forward to byte array version
    return Write(reinterpret_cast<const BYTE*>(data.c_str()), 0, static_cast<DWORD>(data.length()));
}

bool CSerial::Write(const BYTE* data, DWORD offset, DWORD count) {
    // Check if open under lock
    HANDLE hSerialLocal;
    {
        std::lock_guard<std::mutex> lock(m_mutex);
        if (!m_isOpen || m_hSerial.get() == INVALID_HANDLE_VALUE) {
            InvokeErrorOccurred(std::runtime_error("Write attempted on closed or invalid port."));
            return false;
        }
        hSerialLocal = m_hSerial.get(); // Get handle under lock
    }


    DWORD bytesWritten = 0; // Initialize
    OVERLAPPED overlapped = { 0 };

    HandleGuard eventHandle(CreateEvent(NULL, TRUE, FALSE, NULL));
    // Use CHECK macro for event creation failure
    CHECK(eventHandle.get() != NULL, INVALID_HANDLE_VALUE, "CreateEvent failed in Write bytes.");
    overlapped.hEvent = eventHandle.get();


    BOOL writeResult = WriteFile(hSerialLocal, data + offset, count, NULL, &overlapped); // Pass NULL for sync bytes written

    if (!writeResult) {
        DWORD error = GetLastError();
        if (error == ERROR_IO_PENDING) {
            DWORD waitResult = WaitForSingleObject(overlapped.hEvent, IO_OPERATION_TIMEOUT);
            if (waitResult == WAIT_OBJECT_0) {
                // Get handle again in case it changed
                HANDLE currentSerialHandle;
                {
                    std::lock_guard<std::mutex> lock(m_mutex);
                    currentSerialHandle = m_hSerial.get();
                }
                if (currentSerialHandle == INVALID_HANDLE_VALUE) {
                    std::cerr << "Write: Serial handle became invalid while waiting for I/O." << std::endl;
                    InvokeErrorOccurred(std::runtime_error("Write failed: Port closed during operation."));
                    return false;
                }

                if (!GetOverlappedResult(currentSerialHandle, &overlapped, &bytesWritten, FALSE)) { // Get bytes written
                    DWORD overlappedError = GetLastError();
                    if (overlappedError != ERROR_OPERATION_ABORTED) {
                        InvokeErrorOccurred(std::runtime_error("GetOverlappedResult failed in Write. Error: " + std::to_string(overlappedError)));
                    }
                    return false; // Write failed
                }
                // Write completed successfully, bytesWritten contains the count
                if (bytesWritten != count) {
                    // Partial write occurred? Or error?
                    InvokeErrorOccurred(std::runtime_error("Write operation wrote fewer bytes than requested."));
                    // Decide if this is a failure or just informational
                    return false; // Treat as failure for now
                }
            }
            else {
                // Wait failed (timeout or other error)
                DWORD waitError = (waitResult == WAIT_TIMEOUT) ? ERROR_TIMEOUT : GetLastError();
                std::cerr << "WaitForSingleObject failed in Write. WaitResult: " << waitResult << " Error: " << waitError << std::endl;
                // Cancel the pending I/O
                {
                    std::lock_guard<std::mutex> lock(m_mutex);
                    if (m_hSerial.get() != INVALID_HANDLE_VALUE) CancelIo(m_hSerial.get());
                }
                InvokeErrorOccurred(std::runtime_error("Write operation failed (wait). Error: " + std::to_string(waitError)));
                return false; // Write failed
            }
        }
        else {
            // Other WriteFile error
            InvokeErrorOccurred(std::runtime_error("WriteFile failed directly. Error: " + std::to_string(error)));
            return false; // Write failed
        }
    }
    else {
        // Write completed synchronously
        HANDLE currentSerialHandle;
        {
            std::lock_guard<std::mutex> lock(m_mutex);
            currentSerialHandle = m_hSerial.get();
        }
        if (currentSerialHandle == INVALID_HANDLE_VALUE) {
            std::cerr << "Write: Serial handle became invalid after sync WriteFile." << std::endl;
            InvokeErrorOccurred(std::runtime_error("Write failed: Port closed during operation."));
            return false;
        }
        if (!GetOverlappedResult(currentSerialHandle, &overlapped, &bytesWritten, FALSE)) {
            DWORD overlappedError = GetLastError();
            if (overlappedError != ERROR_OPERATION_ABORTED) {
                InvokeErrorOccurred(std::runtime_error("GetOverlappedResult failed (sync write). Error: " + std::to_string(overlappedError)));
            }
            return false;
        }
        if (bytesWritten != count) {
            InvokeErrorOccurred(std::runtime_error("Synchronous write wrote fewer bytes than requested."));
            return false;
        }
    }

    return true; // Write succeeded
}


bool CSerial::Close() {
    // Signal the read thread to stop *before* taking the lock
    m_stopReadLoop.store(true); // Use store() for atomic write

    std::thread threadToJoin; // Local variable to hold the thread for joining

    { // Lock scope
        std::lock_guard<std::mutex> lock(m_mutex);

        if (!m_isOpen) {
            // Already closed or never opened, ensure thread is joined if it exists
            if (m_readThread.joinable()) {
                // Move to local variable inside lock, join outside
                threadToJoin = std::move(m_readThread);
            }
            return true; // Nothing more to do
        }

        // Best effort: Send Halt command (ignore errors, might fail if port already broken)
        if (m_hSerial.get() != INVALID_HANDLE_VALUE) {
            DWORD bytesWritten;
            std::string haltCmd = "Halt"; // Example command
            // Use non-overlapped write for simplicity during close, or short timeout overlapped
            // Using NULL overlapped structure for synchronous behavior here:
            WriteFile(m_hSerial.get(), haltCmd.c_str(), (DWORD)haltCmd.length(), &bytesWritten, NULL);
            // Ignore result of WriteFile during close
        }

        // Cancel any pending I/O operations on the handle *before* closing it
        // This helps the ReadLoop exit cleanly if blocked on ReadFile/WaitForSingleObject
        if (m_hSerial.get() != INVALID_HANDLE_VALUE) {
            if (!CancelIoEx(m_hSerial.get(), NULL)) { // Cancel all I/O for this handle
                DWORD cancelError = GetLastError();
                // ERROR_NOT_FOUND means no pending I/O, which is fine.
                if (cancelError != ERROR_NOT_FOUND) {
                    std::cerr << "Close: CancelIoEx failed. Error: " << cancelError << std::endl;
                    // Don't invoke error handler during close? Or maybe do?
                    // InvokeErrorOccurred(std::runtime_error("CancelIoEx failed during close. Error: " + std::to_string(cancelError)));
                }
            }
        }

        // Move the thread handle to the local variable *before* resetting m_hSerial
        if (m_readThread.joinable()) {
            threadToJoin = std::move(m_readThread);
        }

        // Reset the HandleGuard, which calls CloseHandle via RAII
        m_hSerial.reset(); // Closes the underlying HANDLE

        m_isOpen = false; // Mark as closed

		m_dataHandler = nullptr; // Clear data handler

    } // Mutex unlocked here

    // Wait briefly *after* CancelIo and CloseHandle for thread to potentially exit I/O calls
    // This is heuristic, joining is the guarantee.
    std::this_thread::sleep_for(std::chrono::milliseconds(50)); // Reduced delay

    // Join the read thread *outside* the lock to prevent deadlock
    if (threadToJoin.joinable()) {
        try {
            threadToJoin.join();
            std::cout << "Close: Read thread joined successfully." << std::endl;
        }
        catch (const std::system_error& e) {
            std::cerr << "Close: Exception caught while joining read thread: " << e.what() << " (" << e.code() << ")" << std::endl;
            // This might happen if join() is called twice, etc.
        }
    }
    else {
        std::cout << "Close: Read thread was not joinable (already joined or never started?)." << std::endl;
    }


    // Invoke state change after everything is done and lock released
    InvokeConnectionChanged(false); // State is now false

    return true;
}

// GetBaudRate implementation
int CSerial::GetBaudRate() const {
	std::lock_guard<std::mutex> lock(m_mutex);
	return m_baudRate;
}

// IsOpen implementation
bool CSerial::IsOpen() const {
	std::lock_guard<std::mutex> lock(m_mutex);
	return m_isOpen;
}

#pragma managed(pop)