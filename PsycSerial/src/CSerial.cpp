#include "CSerial.h"
#pragma managed(push, off)
#include "Packets/CDecoder.h"

#include <chrono>
#include <thread>
#include <iostream>
#include <memory>
#include <atomic>
#include <sstream>

static CDecoder decoder;

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

void CSerial::InvokeDataReceived(const CPacket& packet) {
    DataHandler handler = nullptr;
    void* context = nullptr;
    {
        std::lock_guard<std::mutex> lock(m_mutex);
        handler = m_dataHandler;
        context = m_userData; // Get user data under lock
    }

    for (;;) {
		CDecodedPacket dataPacket;
        auto kind = decoder.process(packet, dataPacket);
        if (kind == PacketKind::Unknown)
            break;

        // Invoke outside the lock
        if (handler) {
            try {
               handler(context, this, dataPacket); // Pass user data
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

    const auto start = std::chrono::steady_clock::now();

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
        CPacket pkt{};
        auto now = std::chrono::steady_clock::now();
        pkt.timestamp = std::chrono::duration_cast<std::chrono::milliseconds>(now - start).count();
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

bool CSerial::Write(const BYTE* data, DWORD offset, DWORD count)
{
    if (count == 0) {
        return true; // nothing to do
    }

    // Snapshot handle under lock, but don't call callbacks while holding it
    HANDLE hSerialLocal = INVALID_HANDLE_VALUE;
    {
        std::lock_guard<std::mutex> lock(m_mutex);
        if (m_isOpen && m_hSerial.get() != INVALID_HANDLE_VALUE) {
            hSerialLocal = m_hSerial.get();
        }
    }

    if (hSerialLocal == INVALID_HANDLE_VALUE) {
        InvokeErrorOccurred(std::runtime_error("Write attempted on closed or invalid port."));
        return false;
    }

    const BYTE* p = data + offset;
    DWORD       remaining = count;

    // Event for this write sequence (auto-reset is fine; kernel manages it for overlapped I/O)
    HandleGuard eventHandle(CreateEvent(nullptr, FALSE, FALSE, nullptr));
    if (!eventHandle.get()) {
        DWORD le = GetLastError();
        std::ostringstream os; os << "CreateEvent failed in Write. Error: " << le;
        InvokeErrorOccurred(std::runtime_error(os.str()));
        return false;
    }

    OVERLAPPED overlapped{};
    overlapped.hEvent = eventHandle.get();

    while (remaining > 0) {
        DWORD bytesWritten = 0;

        // Issue overlapped write
        BOOL writeResult = WriteFile(
            hSerialLocal,
            p,
            remaining,
            nullptr,         // ignored for overlapped
            &overlapped);

        if (!writeResult) {
            DWORD error = GetLastError();

            if (error == ERROR_IO_PENDING) {
                // Wait for completion with a timeout
                DWORD waitResult = WaitForSingleObject(overlapped.hEvent, IO_OPERATION_TIMEOUT);
                if (waitResult == WAIT_OBJECT_0) {
                    // Operation completed; fetch the result
                    if (!GetOverlappedResult(hSerialLocal, &overlapped, &bytesWritten, FALSE)) {
                        DWORD overlappedError = GetLastError();
                        if (overlappedError != ERROR_OPERATION_ABORTED) {
                            std::ostringstream os;
                            os << "GetOverlappedResult failed in Write. Error: " << overlappedError;
                            InvokeErrorOccurred(std::runtime_error(os.str()));
                        }
                        return false;
                    }
                }
                else {
                    // Timeout or wait error
                    DWORD waitError = (waitResult == WAIT_TIMEOUT) ? ERROR_TIMEOUT : GetLastError();
                    CancelIoEx(hSerialLocal, &overlapped);

                    std::ostringstream os;
                    os << "WaitForSingleObject failed in Write. WaitResult: "
                        << waitResult << " Error: " << waitError;
                    InvokeErrorOccurred(std::runtime_error(os.str()));
                    return false;
                }
            }
            else if (error == ERROR_OPERATION_ABORTED) {
                // Port was closed while write in flight
                InvokeErrorOccurred(std::runtime_error("Write canceled because the port was closed."));
                return false;
            }
            else {
                std::ostringstream os;
                os << "WriteFile failed directly in Write. Error: " << error;
                InvokeErrorOccurred(std::runtime_error(os.str()));
                return false;
            }
        }
        else {
            // Completed synchronously (even with OVERLAPPED, lpBytesWritten is ignored)
            if (!GetOverlappedResult(hSerialLocal, &overlapped, &bytesWritten, FALSE)) {
                DWORD overlappedError = GetLastError();
                if (overlappedError != ERROR_OPERATION_ABORTED) {
                    std::ostringstream os;
                    os << "GetOverlappedResult failed (sync write). Error: " << overlappedError;
                    InvokeErrorOccurred(std::runtime_error(os.str()));
                }
                return false;
            }
        }

        if (bytesWritten == 0) {
            InvokeErrorOccurred(std::runtime_error("WriteFile wrote zero bytes."));
            return false;
        }

        // Advance pointer and reduce remaining count
        remaining -= bytesWritten;
        p += bytesWritten;

        // Prepare OVERLAPPED for the next chunk
        ZeroMemory(&overlapped, sizeof(overlapped));
        overlapped.hEvent = eventHandle.get();
    }

    return true;
}


bool CSerial::Close()
{
    // Tell the read thread to stop as soon as possible
    m_stopReadLoop.store(true, std::memory_order_release);

    std::thread threadToJoin;    // thread we will join outside the lock
    bool        wasOpen = false; // track whether we were actually open

    {
        std::lock_guard<std::mutex> lock(m_mutex);

        wasOpen = m_isOpen;

        // If there's a read thread, always move it to the local variable so we can join it.
        if (m_readThread.joinable()) {
            threadToJoin = std::move(m_readThread);
        }

        if (m_isOpen) {
            // We are currently open: cancel any pending I/O before closing the handle
            HANDLE h = m_hSerial.get();
            if (h != INVALID_HANDLE_VALUE) {
                if (!CancelIoEx(h, nullptr)) {
                    DWORD cancelError = GetLastError();
                    if (cancelError != ERROR_NOT_FOUND) {
                        std::cerr << "Close: CancelIoEx failed. Error: "
                            << cancelError << std::endl;
                    }
                }
            }

            // Close the underlying handle via RAII
            m_hSerial.reset();
            m_isOpen = false;
            m_dataHandler = nullptr; // no more data callbacks after close
        }
    } 

    // Small delay to let any in-flight I/O settle before join (heuristic only)
    std::this_thread::sleep_for(std::chrono::milliseconds(50));

    // Join the read thread outside the lock to avoid deadlocks
    if (threadToJoin.joinable()) {
        try {
            threadToJoin.join();
        }
        catch (const std::system_error& e) {
            std::cerr << "Close: Exception while joining read thread: "
                << e.what() << " (" << e.code() << ")" << std::endl;
        }
    }

    // Only signal a connection state change if we were actually open
    if (wasOpen) InvokeConnectionChanged(false);
    

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