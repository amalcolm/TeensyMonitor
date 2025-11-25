#pragma once

#include "ManagedCallbacks.h"
#include "CSerial.h"
#include "Packets/Packets.h"

using namespace System;
using namespace System::Diagnostics;
using namespace System::Runtime::InteropServices; // For GCHandle, Marshal
using namespace System::Threading;

namespace PsycSerial {


    public delegate void DataEventHandler(IPacket^ packet);
    public delegate void ErrorEventHandler(Exception^ exception);
    public delegate void ConnectionEventHandler(bool isOpen);


    public ref class SerialHelper : IDisposable {

    private:
        // Pointer to the native C++ serial port implementation
        CSerial* m_nativeSerial;
        String^ m_portName; // Store the port name for debugging

        // Managed callback dispatcher (if needed for threading policies other than Direct)
        ManagedCallbacks^ m_managedCallbacks;

        // --- GCHandle ---
        // Handle to this managed object to safely pass to native code
        GCHandle m_selfHandle;

        // --- IDisposable Pattern ---

        // Internal Dispose implementation
        void Disposer(bool disposing);

        // Checks if disposed and throws ObjectDisposedException
        const void ThrowIfDisposed();

        // --- Static Callback Bridges (Native -> Managed) ---
        // These functions are called directly by the native CSerial instance.
        // They MUST be static and match the Native*Handler function pointer types.
        static void StaticDataHandler(void* userData, CSerial* pSender, const CDecodedPacket& packet);
        static void StaticErrorHandler(void* userData, CSerial* pSender, const std::exception& ex);
        static void StaticConnectionHandler(void* userData, CSerial* pSender, bool state);

        // --- Instance Callback Handlers (Called by Static Bridges) ---
        // These methods execute in the managed world and raise the public events.
        void OnDataReceived(const CDecodedPacket& packet);
        void OnErrorOccurred(const std::exception& ex);
        void OnConnectionChanged(bool state);


    public:
        // --- Public Events ---
        event DataEventHandler^ DataReceived;
        event ErrorEventHandler^ ErrorOccurred;
        event ConnectionEventHandler^ ConnectionChanged;

        bool m_disposed;

        // --- Constructor ---
        // Takes port name and the desired callback execution policy
        SerialHelper(CallbackPolicy policy); // Removed portName from constructor, pass in Open

        // --- Destructor & Finalizer (for IDisposable) ---
        virtual ~SerialHelper(); // Dispose managed & unmanaged
        !SerialHelper();       // Finalizer (dispose unmanaged only)

        // --- Public Methods ---
        bool Open(String^ portName);
        bool Open(String^ portName, int baudRate);
        bool Close();
        bool Write(String^ data);
        bool Write(array<Byte>^ data);
        bool Write(array<Byte>^ data, int offset, int count);

        // --- Public Properties ---

		property String^ PortName {
			String^ get() { return m_portName; }
            void set(String^ value) { Open(value, BaudRate); }
		}

        property bool IsOpen {
            bool get();
        }

        property int BaudRate {
            int get();
        }

        property int PendingCallbacks {
            int get(); // Returns queue size from ManagedCallbacks
        }

        property CallbackPolicy CurrentCallbackPolicy {
            CallbackPolicy get() { return m_managedCallbacks->Policy; }
        }

        static array<String^>^ GetUSBSerialPorts();


    private:
        // Delegate types matching native function pointers
        delegate void NativeDataCallbackDelegate(void* userData, CSerial* pSender, const CDecodedPacket& packet);
        delegate void NativeErrorCallbackDelegate(void* userData, CSerial* pSender, const std::exception& ex);
        delegate void NativeConnectionCallbackDelegate(void* userData, CSerial* pSender, bool state);

        // Store delegate instances to prevent GC
        NativeDataCallbackDelegate^ m_delegateDataHandler;
        NativeErrorCallbackDelegate^ m_delegateErrorHandler;
        NativeConnectionCallbackDelegate^ m_delegateConnectionHandler;

    public:
        void RaiseDataReceivedEvent(IPacket^ packet) { DataReceived(packet);     }
        void RaiseErrorOccurredEvent(Exception^ ex)  { ErrorOccurred(ex);        }
        void RaiseConnectionChangedEvent(bool state) { ConnectionChanged(state); }

    };


} // End namespace PsycSerial
