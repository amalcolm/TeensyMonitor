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

    public enum class FieldEnum
    {
        Timestamp,
        C0,
        Events,
        Offset1,
        Offset1_Hi,
		Offset1_Lo,
        Offset2,
        Gain,
        preGainSensor,
        postGainSensor,
    };

	public ref class DataPacket : IPacket, IDisposable
    {
    private:
        static constexpr System::UInt64 WordMask = 0xFFFFull;
		static constexpr System::UInt64 ByteMask = 0x00FFull;

    public:
		static DataPacket^ Rent();
		virtual void Cleanup();

        ~DataPacket();
		!DataPacket();

		void Reset();

        virtual property HeadState State;
        virtual property double    TimeStamp;

		property double         StateTime;
        property System::UInt64 HardwareState;
        property int            SensorState;

        property array<unsigned int>^ Channel;


        property int Offset1        { int get() { return (int)((HardwareState >> 56) & ByteMask);  } }
        property int Offset1_Hi     { int get() { return (int)((HardwareState >> 48) & ByteMask);  } }
        property int Offset1_Lo     { int get() { return (int)((HardwareState >> 40) & ByteMask);  } }
        property int SequenceNumber { int get() { return (int)((HardwareState >> 32) & ByteMask);  } }
        property int Offset2        { int get() { return (int)((HardwareState >> 24) & ByteMask);  } }
        property int Gain           { int get() { return (int)((HardwareState >> 16) & ByteMask);  } }
        
        property int _Reserved      { int get() { return (int)((HardwareState      ) & WordMask);  } }
       

        property int  preGainSensor { int get() { return (int)((SensorState   >> 16) & WordMask);  } }
		property int postGainSensor { int get() { return (int)((SensorState        ) & WordMask);  } }

        double get(FieldEnum field) {
            switch (field) {
                case FieldEnum::Timestamp:      return StateTime;
                case FieldEnum::C0:             return Channel[0];
                case FieldEnum::Offset1:        return Offset1;
				case FieldEnum::Offset1_Hi:     return Offset1_Hi;
				case FieldEnum::Offset1_Lo:     return Offset1_Lo;
                case FieldEnum::Offset2:        return Offset2;
                case FieldEnum::Gain:           return Gain;
                case FieldEnum::preGainSensor:  return preGainSensor;
                case FieldEnum::postGainSensor: return postGainSensor;
                default:                        return Double::NaN;
			}
        }
    
    protected:
		DataPacket();
        static ConcurrentQueue<DataPacket^>^ s_pool = gcnew ConcurrentQueue<DataPacket^>();
    };


    public enum class EventKind : System::UInt32
    {
        NONE = 0,

        A2D_DATA_READY     = 0x11,
		A2D_READ_START     = 0x12,
		A2D_READ_COMPLETE  = 0x13,
   
        HW_UPDATE_START    = 0x21,
        HW_UPDATE_COMPLETE = 0x22,
   
        SPI_DMA_START      = 0x31,
        SPI_DMA_COMPLETE   = 0x32,
    
        RESERVED = 255
    };

    public ref class EventPacket : IDisposable
    {
    public:
        static EventPacket^ Rent();
		virtual void Cleanup();

        ~EventPacket();
        !EventPacket();

        void Reset();
        property EventKind Kind;
        property double    StateTime;

    protected:
        EventPacket();

        static ConcurrentQueue<EventPacket^>^ s_pool = gcnew ConcurrentQueue<EventPacket^>();
    };

	public ref class BlockPacket : IPacket, IDisposable
    {
    public:
		static BlockPacket^ Rent();
        virtual void Cleanup();

		~BlockPacket();
		!BlockPacket();

		void Reset();

        virtual property HeadState    State;
        virtual property double       TimeStamp;

        property int                  Count;
		property int				  NumEvents;
        property array<DataPacket^>^  BlockData;
		property array<EventPacket^>^ EventData;

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