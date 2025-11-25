#pragma once

using namespace System;
using namespace System::Collections::Concurrent;
using namespace System::Text;

namespace PsycSerial
{
    
    // Pool of char buffers (System::Char = wchar_t in C++/CLI)
    public ref class CharPool abstract sealed
    {
    private:
        static ConcurrentBag<array<wchar_t>^>^ s_pool;

    public:
        literal int BufferSize = 128;
        literal int InitialBagSize = 8192;

        // Static constructor
        static CharPool();

        static array<wchar_t>^ Rent();
        static void Return(array<wchar_t>^ buffer);
    };

    // Pooled, reusable string-like wrapper with no System::String^
    public ref class AString sealed
    {
    private:
        array<wchar_t>^ _buffer;
        int             _length;
        double          _time;

        static ConcurrentBag<AString^>^ s_pool;

        // Only Rent() can create instances
        AString();

    public:
        property int Length
        {
            int get() { return _length; }
        }

        property double Time
        {
            double get() { return _time; }
            void   set(double value) { _time = value; }
        }

        property array<wchar_t>^ Buffer
        {
            array<wchar_t>^ get() { return _buffer; }
        }

        property bool IsEmpty
        {
            bool get() { return _buffer == nullptr || _length == 0; }
        }

        // Indexer: aString[i]
         property wchar_t default[int]
            {
                wchar_t get(int index)
                {
                    if (index < 0 || index >= _length)
                        throw gcnew ArgumentOutOfRangeException("index");
                    return _buffer[index];
                }
            }

            // --- Pooling API -----------------------------------------------------

            // Rent/reuse AString instances
        static AString^ Rent();

        // Reset fields but keep buffer for reuse
        void Reset();

        // --- Factories to fill the buffer -----------------------------------

        // Copy from existing char[] slice
        static AString^ FromChars(array<wchar_t>^ chars, int offset, int count, double time);

        // Decode UTF-8 bytes directly into the internal buffer
        static AString^ FromUtf8(array<Byte>^ bytes, int offset, int count, double time);

        // Destructor returns buffer and object to pools (called via 'delete')
        ~AString();

    protected:
        // Finalizer: just return the buffer if GC collects without 'delete'
        !AString();
    };
}
