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
        Timestamp = 0.0;
		// Data array is reused, and no need to clean it.
        BytesRead = 0;
    }




    DataPacket::DataPacket()
    {
        Channel = gcnew array<unsigned int>(8);
        Reset();
    }

    DataPacket^ DataPacket::Rent()
    {
        DataPacket^ p; if (s_pool->TryDequeue(p)) return p;
        return gcnew DataPacket();
	}

    DataPacket::~DataPacket()
    {
        Reset();
        s_pool->Enqueue(this);
        GC::SuppressFinalize(this);
	}

    DataPacket::!DataPacket() {}

    void DataPacket::Reset()
    {
        State = HeadState::None;
        TimeStamp = 0.0;
        HardwareState = 0;
		// Channel array is reused, and no need to clean it.
    }



    BlockPacket::BlockPacket()
    {
        BlockData = gcnew array<DataPacket^>(320);
        Reset();
	}
    
    BlockPacket^ BlockPacket::Rent()
    {
        BlockPacket^ p; if (s_pool->TryDequeue(p)) return p;
        return gcnew BlockPacket();
	}
    
    BlockPacket::~BlockPacket()
    {
        // Deterministic cleanup path (Dispose)
        Reset();
        s_pool->Enqueue(this);
        GC::SuppressFinalize(this);
	}
	
    BlockPacket::!BlockPacket() {}

    void BlockPacket::Reset()
    {
        State = HeadState::None;
        TimeStamp = 0.0;
        Count = 0;
        // BlockData array is reused, no need to clean it.
	}


    TextPacket::TextPacket()
    {
		Text = AString::Rent();
        Reset();
    }
    
    TextPacket^ TextPacket::Rent()
    {
        TextPacket^ p; if (s_pool->TryDequeue(p)) return p;
        return gcnew TextPacket();
    }
    
    TextPacket::~TextPacket()
    {
        // Deterministic cleanup path (Dispose)
        Reset();
        s_pool->Enqueue(this);
        GC::SuppressFinalize(this);
    }
    
    TextPacket::!TextPacket() {}
    
    void TextPacket::Reset()
    {
        State = HeadState::None;
		Length = 0;
        TimeStamp = 0.0;
        // release Text
    }

    

    TelemetryPacket::TelemetryPacket()
    {
        Reset();
	}
    TelemetryPacket^ TelemetryPacket::Rent()
    {
        TelemetryPacket^ p; if (s_pool->TryDequeue(p)) return p;
        return gcnew TelemetryPacket();
    }
    
    TelemetryPacket::~TelemetryPacket()
    {
        // Deterministic cleanup path (Dispose)
        Reset();
        s_pool->Enqueue(this);
        GC::SuppressFinalize(this);
    }
    
    TelemetryPacket::!TelemetryPacket() {}

    void TelemetryPacket::Reset()
    {
        State = HeadState::None;
        TimeStamp = 0.0;
        Group = TeleGroup::NONE;
		SubGroup = 0;
        ID = 0;
        Value = 0.0f;
        Key = 0;
	}
}
