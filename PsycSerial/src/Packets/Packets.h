#pragma once
#pragma managed(push, on)

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


    ref struct DataPacket : Packet
    {
		static ConcurrentQueue<DataPacket^>^ s_dataPool = gcnew ConcurrentQueue<DataPacket^>();
		static DataPacket^ Rent();

        ~DataPacket();
		!DataPacket();

		void Reset();

        property UInt32 State;
        property UInt32 TimeStamp;
        property UInt32 HardwareState;
        property array<UInt32>^ Channel;

    protected:
		DataPacket();
    };

    ref struct BlockPacket : Packet
    {
		static ConcurrentQueue<BlockPacket^>^ s_blockPool = gcnew ConcurrentQueue<BlockPacket^>();
		static BlockPacket^ Rent();

		~BlockPacket();
		!BlockPacket();

		void Reset();

        property UInt32 State;
        property UInt32 TimeStamp;
        property UInt32 Count;
        property array<DataPacket^>^ BlockData;

	protected:
		BlockPacket();
    };
}

#pragma managed(pop)