#pragma once
#pragma managed(push, on)

#include "..\AString.h"

using namespace System;
using namespace System::Collections::Concurrent;

namespace PsycSerial
{
    ref struct Packet
    {
    internal:
        static ConcurrentQueue<Packet^>^ s_pool = gcnew ConcurrentQueue<Packet^>();

        // Rent from pool or allocate a new one.
        static Packet^ Rent();

        // C++/CLI destructor == IDisposable.Dispose
        ~Packet();         // deterministic return to pool
        !Packet();         // finalizer (avoid doing anything heavy)

        // Reset clears instance state before reusing
        void Reset();

        // Payload
        property DateTime Timestamp;
        property array<Byte>^ Data;
        property UInt32 BytesRead;

    protected:
        Packet();        // real constructor is protected
    };


    public interface class IPacket
    {
        property DateTime TimeStamp;
        property UInt32   State;
    };

    public ref struct DataPacket : IPacket
    {
	public:
		static DataPacket^ Rent();

        ~DataPacket();
		!DataPacket();

		void Reset();

        virtual property UInt32 State;
        virtual property DateTime TimeStamp;

        property UInt32         HardwareState;
        property array<UInt32>^ Channel;

    protected:
		DataPacket();
        static ConcurrentQueue<DataPacket^>^ s_pool = gcnew ConcurrentQueue<DataPacket^>();
    };

    public ref struct BlockPacket : IPacket
    {
    public:
		static BlockPacket^ Rent();

		~BlockPacket();
		!BlockPacket();

		void Reset();

        virtual property UInt32      State;
        virtual property DateTime    TimeStamp;

        property UInt32              Count;
        property array<DataPacket^>^ BlockData;

	protected:
		BlockPacket();

		static ConcurrentQueue<BlockPacket^>^ s_pool = gcnew ConcurrentQueue<BlockPacket^>();
    };

	public ref struct TextPacket : IPacket
	{
    public:
        static TextPacket^ Rent();

        ~TextPacket();
        !TextPacket();
        
        void Reset();
        
        virtual property UInt32   State;
        virtual property DateTime TimeStamp;

        property AString^   Text;
		property UInt32    Length;

    protected:
        TextPacket();
        static ConcurrentQueue<TextPacket^>^ s_pool = gcnew ConcurrentQueue<TextPacket^>();
	};
}

#pragma managed(pop)