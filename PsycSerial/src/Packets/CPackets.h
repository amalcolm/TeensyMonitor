#pragma once
#pragma managed(push, off)

#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
#include <cstdint>
#include <cstddef>
#include <type_traits>
#include <vector>

using Frame = uint32_t;

// ----------------------------- Native carrier --------------------------------
struct CPacket {
    double                timestamp{};  // arrival time (optional)
    std::vector<BYTE>     data{};       // raw bytes from device
    uint32_t              bytesRead{};  // valid byte count in data

    inline bool isEmpty() const { return bytesRead == 0; }
};

// ----------------------------- Wire structs ----------------------------------
#pragma pack(push, 1)

struct CDataPacket
{
    static constexpr uint8_t A2D_NUM_CHANNELS = 8;

    static constexpr Frame frameStart = 0xEDD1FAB4;
    static constexpr Frame frameEnd   = 0xEDD2FAB4; // "RD-" end   marker

    uint32_t state{};
    double   timeStamp{};
    uint32_t hardwareState{};
	uint32_t sensorState{};
    uint32_t channel[A2D_NUM_CHANNELS]{};

    static constexpr uint32_t STATE_UNSET = 0b10000000000000000000000000000000;

};

struct CBlockPacket
{
    static constexpr size_t MAX_BLOCK_SIZE = 64;

    static constexpr Frame frameStart = 0xEDB1FAB4; // "QK+" start marker
    static constexpr Frame frameEnd   = 0xEDB2FAB4; // "QK-" end   marker

    uint32_t state{};
    double   timeStamp{};
    uint32_t count{}; // number of valid entries in blockData
    CDataPacket   blockData[MAX_BLOCK_SIZE]{};
};

struct CTextPacket
{
    static constexpr size_t MAX_TEXT_SIZE = 4096;

	uint32_t timeStamp{};
	uint32_t length{}; // number of valid bytes in text
    uint8_t  utf8Bytes[MAX_TEXT_SIZE]{};
};

#pragma pack(pop)

// Sizes & sanity checks (same endianness is assumed by design)
static_assert(std::is_trivially_copyable_v<CDataPacket> , "CDataPacket must be POD");
static_assert(std::is_trivially_copyable_v<CBlockPacket>, "CBlockPacket must be POD");

static_assert(sizeof(CDataPacket) ==
    sizeof(uint32_t) + sizeof(double) + sizeof(uint32_t) + sizeof(uint32_t) + CDataPacket::A2D_NUM_CHANNELS * sizeof(uint32_t),
    "Unexpected CDataPacket layout/packing");

static_assert(offsetof(CBlockPacket, blockData) ==
	sizeof(uint32_t) + sizeof(double) + sizeof(uint32_t),
    "Unexpected CBlockPacket header layout");

// ----------------------------- Tagged result ---------------------------------
enum class PacketKind : uint8_t { Unknown = 0, Data = 1, Block = 2, Text = 3 };

struct CDecodedPacket
{
    PacketKind kind{ PacketKind::Unknown };
    union {
        CDataPacket  data;
        CBlockPacket block;
        CTextPacket  text;
    };

    CDecodedPacket() noexcept {} // POD; union members are zero-inited by caller when used
};

// ----------------------------- Helpers ---------------------------------------
constexpr size_t DataFramePayloadSize() noexcept {
    return sizeof(CDataPacket); // 1 x data wire struct
}

constexpr size_t DataFrameSize() noexcept {
    return sizeof(Frame) + DataFramePayloadSize() + sizeof(Frame);
}

constexpr size_t BlockFramePayloadSize(uint32_t count) noexcept {
    return sizeof(uint32_t) * 3u + sizeof(CDataPacket) * count; // state,timestamp,count + items
}

constexpr size_t BlockFrameSize(uint32_t count) noexcept {
    return sizeof(Frame) + BlockFramePayloadSize(count) + sizeof(Frame);
}

#pragma managed(pop)
