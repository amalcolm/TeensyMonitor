#pragma once
#pragma managed(push, on)

#include "..\AString.h"

using namespace System;
using namespace System::Collections::Concurrent;

namespace PsycSerial
{
    public enum class HeadState : System::UInt32
    {
        None = 0,
		UNSET = 1U << 31,
    };

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
        property double Timestamp;
        property array<Byte>^ Data;
        property UInt32 BytesRead;

    protected:
        Packet();        // real constructor is protected
    };


	public interface class IPacket
    {
        property double    TimeStamp;
        property HeadState State;

        virtual void Cleanup();
    };

	public ref class DataPacket : IPacket, IDisposable
    {
	public:
		static DataPacket^ Rent();
		virtual void Cleanup();

        ~DataPacket();
		!DataPacket();

		void Reset();

        virtual property HeadState State;
        virtual property double    TimeStamp;

		property double         StateTime;
        property int            HardwareState;
        property int            SensorState;

        property array<unsigned int>^ Channel;

        property int SequenceNumber { int get() { return (HardwareState >> 24);        }}
		property int Offset1        { int get() { return (HardwareState >> 16) & 0xFF; }}
		property int Offset2        { int get() { return (HardwareState >>  8) & 0xFF; }}
		property int Gain           { int get() { return (HardwareState      ) & 0xFF; }}

        property int  preGainSensor { int get() { return (SensorState   >> 16) & 0xFFFF; }}
		property int postGainSensor { int get() { return (SensorState        ) & 0xFFFF; }}
    
    protected:
		DataPacket();
        static ConcurrentQueue<DataPacket^>^ s_pool = gcnew ConcurrentQueue<DataPacket^>();
    };

	public ref class BlockPacket : IPacket, IDisposable
    {
    public:
		static BlockPacket^ Rent();
        virtual void Cleanup();

		~BlockPacket();
		!BlockPacket();

		void Reset();

        virtual property HeadState State;
        virtual property double    TimeStamp;

        property int                 Count;
        property array<DataPacket^>^ BlockData;

	protected:
		BlockPacket();

		static ConcurrentQueue<BlockPacket^>^ s_pool = gcnew ConcurrentQueue<BlockPacket^>();
    };

	public ref class TextPacket : IPacket, IDisposable
	{
    public:
        static TextPacket^ Rent();
        virtual void Cleanup();

        ~TextPacket();
        !TextPacket();
        
        void Reset();

        virtual property HeadState State;
        virtual property double    TimeStamp;

        property AString^   Text;
		property int        Length;

    protected:
        TextPacket();
        static ConcurrentQueue<TextPacket^>^ s_pool = gcnew ConcurrentQueue<TextPacket^>();
	};

    public ref class TelemetryPacket : IPacket, IDisposable
	{
    public:
        enum class TeleGroup : System::Byte
        {
			NONE     = 0x00,
            Program  = 0x01,
			Hardware = 0x02,

            A2D      = 0x11,
            DigiPots = 0x12,
            USB      = 0x13,
            Head     = 0x14,
            Timer    = 0x15,

			UNSET    = 0xFF,
		};

        static TelemetryPacket^ Rent();
        virtual void Cleanup();

        ~TelemetryPacket();
        !TelemetryPacket();
        
        void Reset();
        virtual property HeadState State;
        virtual property double    TimeStamp;

		property TeleGroup Group;
        property int       SubGroup;
        property int       ID;
        property float     Value;

        property UInt32    Key;
    protected:
        TelemetryPacket();
        static ConcurrentQueue<TelemetryPacket^>^ s_pool = gcnew ConcurrentQueue<TelemetryPacket^>();
	};
}

#pragma managed(pop)