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
    , m_userData(nullptr)
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


void CSerial::ReadLoop()
{
    // Create an auto-reset event for the read OVERLAPPED
    HANDLE hEvent = CreateEvent(nullptr, /*bManualReset*/ FALSE, /*bInitialState*/ FALSE, nullptr);
    if (hEvent == nullptr) {
        const DWORD le = GetLastError();
        std::ostringstream os; os << "CreateEvent failed in ReadLoop. Error: " << le;
        InvokeErrorOccurred(std::runtime_error(os.str()));
        return;
    }

    OVERLAPPED ov{}; // will be re-initialised before each ReadFile

    std::vector<BYTE> buffer(READ_BUFFER_SIZE);

    for (;;) {
        // Cooperative shutdown
        if (m_stopReadLoop.load(std::memory_order_acquire)) {
            break;
        }

        // Snapshot the handle under lock, then operate lock-free
        HANDLE h = INVALID_HANDLE_VALUE;
        bool   isOpen = false;
        {
            std::lock_guard<std::mutex> g(m_mutex);
            isOpen = m_isOpen;
            h = m_hSerial.get();
        }
        if (!isOpen || h == INVALID_HANDLE_VALUE) {
            break; // port closed while running
        }

        COMSTAT comStat{};
        DWORD   errors = 0;
        if (!ClearCommError(h, &errors, &comStat)) {
            const DWORD le = GetLastError();
            std::ostringstream os; os << "ClearCommError failed in ReadLoop. Error: " << le;
            InvokeErrorOccurred(std::runtime_error(os.str()));
            std::this_thread::sleep_for(std::chrono::milliseconds(1));
            continue;
        }

        DWORD queued = comStat.cbInQue;
        if (queued == 0) {
            // Nothing buffered right now – short pause to avoid busy-spin
            std::this_thread::sleep_for(std::chrono::milliseconds(1));
            continue;
        }

        // We know there is data. Decide how much to ask for this time.
        DWORD requestSize = (std::min)(queued, static_cast<DWORD>(READ_BUFFER_SIZE));

        DWORD bytesRead = 0;

        // Re-initialise OVERLAPPED for this operation
        ZeroMemory(&ov, sizeof(ov));
        ov.hEvent = hEvent;

        // Issue the overlapped read
        BOOL issued = ReadFile(
            h,
            buffer.data(),
            requestSize,
            &bytesRead,
            &ov);

        if (!issued) {
            const DWORD err = GetLastError();
            if (err == ERROR_IO_PENDING) {
                // Wait in short slices so we can notice a stop request and cancel the specific I/O.
                for (;;) {
                    if (m_stopReadLoop.load(std::memory_order_acquire)) {
                        CancelIoEx(h, &ov); // cancel just this read
                    }
                    DWORD w = WaitForSingleObject(ov.hEvent, 100); // 100 ms slice (tune if you like)
                    if (w == WAIT_OBJECT_0) break;     // completed
                    if (w == WAIT_FAILED) {
                        const DWORD we = GetLastError();
                        std::ostringstream os; os << "WaitForSingleObject failed. Error: " << we;
                        InvokeErrorOccurred(std::runtime_error(os.str()));
                        // Best effort: cancel and try again
                        CancelIoEx(h, &ov);
                        bytesRead = 0;
                        break;
                    }
                    // WAIT_TIMEOUT ? loop and re-check stop flag
                }

                BOOL ok = GetOverlappedResult(h, &ov, &bytesRead, FALSE);
                if (!ok) {
                    const DWORD ge = GetLastError();
                    if (ge == ERROR_OPERATION_ABORTED) {
                        // Normal during Close(); exit
                        break;
                    }
                    // Transient failure — report and continue
                    std::ostringstream os; os << "GetOverlappedResult failed. Error: " << ge;
                    InvokeErrorOccurred(std::runtime_error(os.str()));
                    std::this_thread::sleep_for(std::chrono::milliseconds(1));
                    continue;
                }
            }
            else if (err == ERROR_OPERATION_ABORTED) {
                // Close() canceled us
                break;
            }
            else {
                // Immediate ReadFile failure unrelated to pending I/O
                std::ostringstream os; os << "ReadFile failed. Error: " << err;
                InvokeErrorOccurred(std::runtime_error(os.str()));
                std::this_thread::sleep_for(std::chrono::milliseconds(1));
                continue;
            }
        }

        // If we got here with bytesRead set (sync or async paths)
        if (bytesRead == 0) {
            // Nothing to report; try again
            continue;
        }

        // Build and dispatch the packet (no locks held during callback)
        Packet pkt{};
        GetSystemTime(&pkt.timestamp); // For higher precision, consider GetSystemTimePreciseAsFileTime
        pkt.bytesRead = bytesRead;
        pkt.data.assign(buffer.begin(), buffer.begin() + bytesRead);

        InvokeDataReceived(pkt);
    }

    // Silent exit: SetPort/Close handle connection state notifications
    CloseHandle(hEvent);
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