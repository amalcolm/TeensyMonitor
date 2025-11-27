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
	constexpr uint8_t kFrameEndByte         = 0xEA; // Common end  byte of all frame types

    bool readU32(const uint8_t* p, uint32_t& v) noexcept;
    bool readDataPayload (const uint8_t* payload, CDecodedPacket& out) noexcept;
    bool readBlockPayload(const uint8_t* payload, size_t payloadBytes, CDecodedPacket& out, size_t& consumed) noexcept;
    void readTextPayload(const uint8_t* payload, size_t payloadBytes, CDecodedPacket& out, size_t& consumed) noexcept;

}

PacketKind CDecoder::quickFrameCheck(const uint8_t* buf, size_t n, CDecodedPacket& out, size_t& usedBytes) noexcept
{
    usedBytes = 0;
    switch (classify(buf, n))
    {
        case PacketKind::Data :  if (tryParseDataFrame (buf, n, out, usedBytes)) return out.kind; else break;
        case PacketKind::Block:  if (tryParseBlockFrame(buf, n, out, usedBytes)) return out.kind; else break;
    }
	return PacketKind::Unknown;
}

PacketKind CDecoder::process(const CPacket& in, CDecodedPacket& out) noexcept
{
    const uint8_t* buf = in.data.data();
    const size_t   n = static_cast<size_t>(in.bytesRead);
	size_t usedBytes = 0;

    if (m_buf.empty())
        if (quickFrameCheck(buf, n, out, usedBytes) != PacketKind::Unknown) 
            return out.kind;

	pushAndExtract(in, out);
    return out.kind;
}

bool CDecoder::pushAndExtract(const CPacket& in, CDecodedPacket& out)
{
    out.kind = PacketKind::Unknown;

    const uint8_t* data = in.data.data();
    const size_t   dataLen = static_cast<size_t>(in.bytesRead);
    const size_t   oldSize = m_buf.size();

    // append new data to internal buffer
    if (dataLen != 0 && data != nullptr) {
        m_buf.resize(oldSize + dataLen);
        memcpy(m_buf.data() + oldSize, data, dataLen);
    }
    
    if (m_buf.empty())
		return false;

    size_t   usedBytes = 0;

	bool isFrame = (m_buf[0] == kFrameStartByte);

    if (isFrame) {
        if (quickFrameCheck(m_buf.data(), m_buf.size(), out, usedBytes) != PacketKind::Unknown)
        {
            m_buf.erase(m_buf.begin(), m_buf.begin() + usedBytes);
            return true;
        }
		return false; // still waiting for full frame
    }


	// Do not try to cache m_buf.data() or m_buf.size() as m_buf.erase() would invalidate it

	// scan for newline character
    uint8_t *pNL = std::find(m_buf.data(), m_buf.data() + m_buf.size(), '\n');

    if (pNL < m_buf.data() + m_buf.size()) {
        size_t lineBytes = static_cast<size_t>(pNL - m_buf.data());   // bytes before '\n'
		readTextPayload(m_buf.data(), lineBytes + 1, out, usedBytes);  // include '\n'

        if (out.kind == PacketKind::Text && out.text.timeStamp == 0)
			out.text.timeStamp = static_cast<uint32_t>(in.timestamp);

        m_buf.erase(m_buf.begin(), m_buf.begin() + usedBytes);
        return true; 
    }

	uint8_t* pStart = std::find(m_buf.data(), m_buf.data() + m_buf.size(), kFrameStartByte);
    if (pStart < m_buf.data() + m_buf.size()) {
        // start found: discard leading garbage
        m_buf.erase(m_buf.begin(), m_buf.begin() + (pStart - m_buf.data()));

        if (quickFrameCheck(m_buf.data(), m_buf.size(), out, usedBytes) != PacketKind::Unknown)
        {
            m_buf.erase(m_buf.begin(), m_buf.begin() + usedBytes);
            return true;
        }
    }

	return false;
}



bool CDecoder::tryParseDataFrame(const uint8_t* buf, size_t n, CDecodedPacket& out, size_t& usedBytes) noexcept
{   usedBytes = 0;
 
    constexpr size_t need = kFrameSize + sizeof(CDataPacket) + kFrameSize;
    if (n < need) return false;

    uint32_t start = 0; readU32(buf + 0, start);                                                    if (start != CDataPacket::frameStart    ) return false;
    uint32_t end   = 0; readU32(buf + (kFrameSize + sizeof(CDataPacket)), end);                     if (end   != CDataPacket::frameEnd      ) return false;

    readDataPayload(buf + kFrameSize, out);
    usedBytes = need;
    return true;
}

bool CDecoder::tryParseBlockFrame(const uint8_t* buf, size_t n, CDecodedPacket& out, size_t& usedBytes) noexcept
{   usedBytes = 0;

    constexpr size_t minNeed = kFrameSize + kHeaderSize + kFrameSize;                               if (n < minNeed) return false;

    uint32_t start = 0; readU32(buf + 0, start);                                                    if (start != CBlockPacket::frameStart    ) return false;
    uint32_t count = 0; readU32(buf + kFrameSize + kHeaderCountOffset, count);                      if (count >  CBlockPacket::MAX_BLOCK_SIZE) return false;

    const size_t payloadBytes = kHeaderSize + static_cast<size_t>(count) * sizeof(CDataPacket);
    const size_t need = kFrameSize + payloadBytes + kFrameSize;                                     if (n < need) return false;

    uint32_t end   = 0; readU32(buf + (need - kFrameSize), end);                                    if (end   != CBlockPacket::frameEnd      ) return false;

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

    void readTextPayload(const uint8_t* payload, size_t payloadBytes, CDecodedPacket& out, size_t& consumed) noexcept
    {
        CTextPacket tp{};
        size_t len = min(payloadBytes, CTextPacket::MAX_TEXT_SIZE - 1u);

        memcpy_s(tp.utf8Bytes, CTextPacket::MAX_TEXT_SIZE, payload, len);
        tp.utf8Bytes[len] = '\0';
        tp.length = static_cast<uint32_t>(len-1);  // ignore terminator

        consumed = len;
        out.text = tp;
        out.kind = PacketKind::Text;
    }


}

#pragma managed(pop)