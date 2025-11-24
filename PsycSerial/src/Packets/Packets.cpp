#include "Packets.h"

namespace PsycSerial
{
    Packet::Packet()
    {
		Data = gcnew array<Byte>(4096);
        Reset();
	}

    Packet^ Packet::Rent()
    {
        Packet^ p; if (s_pool->TryDequeue(p)) return p;
        return gcnew Packet();
    }

    Packet::~Packet()
    {
        // Deterministic cleanup path (Dispose)
        Reset();
        s_pool->Enqueue(this);
        GC::SuppressFinalize(this);
    }

    Packet::!Packet()
    {
        // Avoid resurrecting the object or touching arbitrary managed graph.
        // Only free unmanaged stuff here if you ever add any.
        // No pooling here—finalization indicates caller didn't Dispose.
    }

    void Packet::Reset()
    {
        Timestamp = DateTime::MaxValue;
		// Data array is reused, no need to clean it.
        BytesRead = 0;
    }




    DataPacket::DataPacket()
    {
        Channel = gcnew array<UInt32>(8);
        Reset();
    }

    DataPacket^ DataPacket::Rent()
    {
        DataPacket^ p; if (s_dataPool->TryDequeue(p)) return p;
        return gcnew DataPacket();
	}

    DataPacket::~DataPacket()
    {
        // Deterministic cleanup path (Dispose)
        Reset();
        s_dataPool->Enqueue(this);
        GC::SuppressFinalize(this);
	}

    DataPacket::!DataPacket() {}

    void DataPacket::Reset()
    {
        Packet::Reset();
        State = 0;
        TimeStamp = 0;
        HardwareState = 0;
		// Channel array is reused, no need to clean it.
    }



    BlockPacket::BlockPacket()
    {
        BlockData = gcnew array<DataPacket^>(16);
        Reset();
	}
    BlockPacket^ BlockPacket::Rent()
    {
        BlockPacket^ p; if (s_blockPool->TryDequeue(p)) return p;
        return gcnew BlockPacket();
	}
    BlockPacket::~BlockPacket()
    {
        // Deterministic cleanup path (Dispose)
        Reset();
        s_blockPool->Enqueue(this);
        GC::SuppressFinalize(this);
	}
	BlockPacket::!BlockPacket() {}
    void BlockPacket::Reset()
    {
        Packet::Reset();
        State = 0;
        TimeStamp = 0;
        Count = 0;
        // BlockData array is reused, no need to clean it.
	}
}
