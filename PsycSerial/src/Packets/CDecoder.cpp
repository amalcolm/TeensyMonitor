#include "CDecoder.h"
#include <cstring>   // memcpy
#include <algorithm> // min

#pragma managed(push, off)

namespace
{
    constexpr size_t kFrameSize = sizeof(Frame);

	constexpr size_t kHeaderSize            = 12u; // state(4) + timeStamp(4) + count(4)
	constexpr size_t kHeaderStateOffset     =  0u; // offset of 'state' field in block header
	constexpr size_t kHeaderTimeStampOffset =  4u; // offset of 'timeStamp' field in block header
	constexpr size_t kHeaderCountOffset     =  8u; // offset of 'count' field in block header

	constexpr uint8_t kFrameStartByte       = 0xBA; // Common first byte of all frame types
	constexpr uint8_t kFrameEndByte         = 0xEA; // '\n' line feed byte

    bool readU32(const uint8_t* p, uint32_t& v) noexcept;
    bool readDataPayload (const uint8_t* payload, CDecodedPacket& out) noexcept;
    bool readBlockPayload(const uint8_t* payload, size_t payloadBytes, CDecodedPacket& out, size_t& consumed) noexcept;
}


PacketKind CDecoder::process(const CPacket& in, CDecodedPacket& out) noexcept
{
    const uint8_t* buf = in.data.data();
    const size_t   n = static_cast<size_t>(in.bytesRead);
	size_t usedBytes = 0;

    if (m_buf.size() == 0)
        switch (classify(buf, n))
        {
            case PacketKind::Data :  if (tryParseDataFrame (buf, n, out, usedBytes)) return out.kind; else break;
            case PacketKind::Block:  if (tryParseBlockFrame(buf, n, out, usedBytes)) return out.kind; else break;
        }
    

	pushAndExtract(in, out);
    return out.kind;
}

// only called if buffer is non-empty, ie. contains partial frame or partial string
bool CDecoder::pushAndExtract(const CPacket& in, CDecodedPacket& out)
{
    out.kind = PacketKind::Unknown;
    const uint8_t* data = in.data.data();
    const size_t   len  = static_cast<size_t>(in.bytesRead);

    if (len == 0 || data == nullptr) return false;

	// append new data to internal buffer
    const uint8_t* buf = m_buf.data();
    const auto oldSize = m_buf.size();
    m_buf.resize(oldSize + len);
    memcpy(m_buf.data() + oldSize, data, len);
    
    bool isUnfinishedFrame = *buf == kFrameStartByte;

	const uint8_t* rP = buf + oldSize;
	const uint8_t* endP = rP + len;
    const uint8_t  target = isUnfinishedFrame ? kFrameEndByte : '\n';

	// scan for either end of frame or newline character
    while (rP < endP && *rP != target)
        ++rP;

	// didn't find anything yet so wait until next push
    if (rP >= endP) return false;

    size_t dataLen = rP - buf;
    size_t usedBytes = 0;

	// if we are parsing a frame and found the end marker, try to parse the frame now
    if (isUnfinishedFrame)
    {
        switch (classify(buf, dataLen))
        {
            case PacketKind::Data:  if (!tryParseDataFrame (buf, dataLen, out, usedBytes)) out.kind = PacketKind::Unknown;  break;
            case PacketKind::Block: if (!tryParseBlockFrame(buf, dataLen, out, usedBytes)) out.kind = PacketKind::Unknown;  break;
        }
    }

    if (out.kind == PacketKind::Unknown)
    {
        // we have a text line; output as a TextPacket
        CTextPacket tp{};
        tp.timeStamp = in.timestamp;
        tp.length = static_cast<uint32_t>(dataLen);
        memcpy_s(tp.text, CTextPacket::MAX_TEXT_SIZE, buf, tp.length);
        out.text = tp;
        out.kind = PacketKind::Text;

        usedBytes = tp.length;
    }


    m_buf.erase(m_buf.begin(), m_buf.begin() + usedBytes);

    return true;
}



bool CDecoder::tryParseDataFrame(const uint8_t* buf, size_t n, CDecodedPacket& out, size_t& usedBytes) noexcept
{   usedBytes = 0;
 
    constexpr size_t need = kFrameSize + sizeof(CDataPacket) + kFrameSize;
    if (n < need) return false;

    uint32_t start = 0; readU32(buf + 0, start);                                                    if (start != CDataPacket::frameStart) return false;
    uint32_t end   = 0; readU32(buf + (kFrameSize + sizeof(CDataPacket)), end);                     if (end   != CDataPacket::frameEnd) return false;

    readDataPayload(buf + kFrameSize, out);
    usedBytes = need;
    return true;
}

bool CDecoder::tryParseBlockFrame(const uint8_t* buf, size_t n, CDecodedPacket& out, size_t& usedBytes) noexcept
{   usedBytes = 0;

    constexpr size_t minNeed = kFrameSize + kHeaderSize + kFrameSize;                               if (n < minNeed) return false;

    uint32_t start = 0; readU32(buf + 0, start);                                                    if (start != CBlockPacket::frameStart) return false;

    uint32_t count = 0; readU32(buf + kFrameSize + kHeaderCountOffset, count);                      if (count > CBlockPacket::MAX_BLOCK_SIZE) return false;

    const size_t payloadBytes = kHeaderSize + static_cast<size_t>(count) * sizeof(CDataPacket);
    const size_t need = kFrameSize + payloadBytes + kFrameSize;                                     if (n < need) return false;

    uint32_t end   = 0; readU32(buf + (need - kFrameSize), end);                                    if (end != CBlockPacket::frameEnd) return false;

    // Copy payload into out
    size_t consumed = 0;
    if (!readBlockPayload(buf + kFrameSize, payloadBytes, out, consumed))
        return false;

    usedBytes = need;
    return true;
}

PacketKind CDecoder::classify(const uint8_t* buf, size_t n) noexcept
{
    if (n < kFrameSize) return PacketKind::Unknown;

    uint32_t start = 0; readU32(buf, start);

    switch (start)
    {
        case CDataPacket::frameStart : return PacketKind::Data;
        case CBlockPacket::frameStart: return PacketKind::Block;
        default: return PacketKind::Unknown;
    }
}





void CDecoder::reset() noexcept
{
    m_buf.clear();
}

namespace
{
    bool readU32(const uint8_t* payload, uint32_t& out) noexcept               { memcpy(&out, payload, sizeof(uint32_t));    return true;  }
    bool readDataPayload(const uint8_t* payload, CDecodedPacket& out) noexcept
    {
		CDataPacket dp{}; memcpy(&dp, payload, sizeof(CDataPacket));
        
        out.data = dp;
        out.kind = PacketKind::Data;
        return true;
    }

    bool readBlockPayload(const uint8_t* payload, size_t payloadBytes, CDecodedPacket& out, size_t& consumed) noexcept
    {
        if (payloadBytes < kHeaderSize) return false;

		uint32_t state = 0; readU32(payload + kHeaderStateOffset    , state);
        uint32_t ts    = 0; readU32(payload + kHeaderTimeStampOffset, ts   );
        uint32_t count = 0; readU32(payload + kHeaderCountOffset    , count);                       if (count > CBlockPacket::MAX_BLOCK_SIZE) return false;

        const size_t itemsBytes = static_cast<size_t>(count) * sizeof(CDataPacket);
        const size_t need = kHeaderSize + itemsBytes;                                               if (payloadBytes < need) return false;

		CBlockPacket bp{};

        bp.state     = state;
        bp.timeStamp = ts;
        bp.count     = count;

        // Copy the packed Data items
        const uint8_t* rP = payload + kHeaderSize;
        for (uint32_t i = 0; i < count; ++i)
        {
            memcpy(&bp.blockData[i], rP, sizeof(CDataPacket));
			rP += sizeof(CDataPacket);
        }

        consumed = need;
        out.block = bp;
        out.kind = PacketKind::Block;

        return true;
    }


}

#pragma managed(pop)