#pragma once
#pragma managed(push, off)

#include <cstdint>
#include <cstddef>
#include <vector>
#include "CPackets.h"

// Native (/clr- OFF) frame decoder for CDataPacket and CBlockPacket.
class CDecoder
{
public:
    // --- One-shot parsers on a raw byte span (no allocations) ----------------

    // Try to parse a Data/Block frame at the start of [buf, buf+n).
    // Returns true on success and sets 'out' and 'usedBytes' to the consumed length.
    static bool tryParseDataFrame (const uint8_t* buf, size_t n, CDecodedPacket& out, size_t& usedBytes) noexcept;
    static bool tryParseBlockFrame(const uint8_t* buf, size_t n, CDecodedPacket& out, size_t& usedBytes) noexcept;

    // Classify the frame at the start of the buffer (without fully parsing).
    // Returns PacketKind::Unknown if start marker doesn't match either type
    // or there aren't enough bytes to decide yet.
    static PacketKind classify(const uint8_t* buf, size_t n) noexcept;

    // --- Convenience wrappers for your existing CPacket carrier ---------------

    // Tries Data first, then Block. On success returns kind and fills the matching out param.
    PacketKind process(const CPacket& in, CDecodedPacket& out) noexcept;

    void reset() noexcept;

private:

    // Append bytes and try to extract exactly one complete frame.
    // On success, 'out' is filled, returns true, and the consumed bytes are removed from the buffer.
    // If false, the data is retained until a full frame arrives.
    bool pushAndExtract(const CPacket& in, CDecodedPacket& out);

    // For testing/introspection
    size_t bufferedSize() const noexcept { return m_buf.size(); }


    std::vector<uint8_t> m_buf;
};

#pragma managed(pop)