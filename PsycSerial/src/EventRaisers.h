#pragma once

#include "SerialHelper.h"

using namespace System;
using namespace System::Diagnostics;

namespace PsycSerial
{
    // --- Define Private Helper Structs for Event Raising ---

    // Helper struct for raising DataReceived event
    private ref struct DataEventRaiser {
        SerialHelper^ m_target;
        IPacket^ m_packet;

        DataEventRaiser(SerialHelper^ target, IPacket^ packet) : m_target(target), m_packet(packet) {
            if (m_target == nullptr) throw gcnew System::ArgumentNullException("target");
            if (m_packet == nullptr) throw gcnew System::ArgumentNullException("packet");
        }

        void Raise() {
            if (m_target->m_disposed) return;

            try { m_target->RaiseDataReceivedEvent(m_packet); }
            catch (Exception^ ex) 
            {
                if (Debugger::IsAttached || IsDebuggerPresent())
                    Debugger::Break();

                Debug::WriteLine(String::Format("DataEventRaiser::Raise Exception: {0}", ex->Message));
                Debug::WriteLine(ex->StackTrace);
            }
                
        }
    };

    // Helper struct for raising ErrorOccurred event
    private ref struct ErrorEventRaiser {
        SerialHelper^ m_target;
        Exception^ m_exception;

        ErrorEventRaiser(SerialHelper^ target, Exception^ ex) : m_target(target), m_exception(ex) {
            if (m_target    == nullptr) throw gcnew System::ArgumentNullException("target");
            if (m_exception == nullptr) throw gcnew System::ArgumentNullException("ex");
        }

        void Raise() {
            if (m_target->m_disposed) return;

            try { m_target->RaiseErrorOccurredEvent(m_exception); }
            catch (Exception^ ex) { Debug::WriteLine(String::Format("ErrorEventRaiser::Raise Exception in ErrorOccurred handler itself: {0}", ex->Message)); }
        }
    };

    // Helper struct for raising ConnectionChanged event
    private ref struct ConnectionEventRaiser {
        SerialHelper^ m_target;
        ConnectionState m_state;

        ConnectionEventRaiser(SerialHelper^ target, ConnectionState state) : m_target(target), m_state(state) {
            if (m_target == nullptr) throw gcnew System::ArgumentNullException("target");
        }

        void Raise() {
            if (m_target->m_disposed) return;

            try { m_target->RaiseConnectionChangedEvent(m_state); }
            catch (Exception^ ex) {
                try {
                    m_target->RaiseErrorOccurredEvent(gcnew Exception(String::Format("Exception in ConnectionChanged handler: {0}", ex->Message), ex));
                }
                catch (...) {}
            }
        }
    };

}