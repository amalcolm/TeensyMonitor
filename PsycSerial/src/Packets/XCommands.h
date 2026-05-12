#pragma once
#include <stdint.h>

using namespace System;
using namespace System::Runtime::InteropServices;

namespace PsycSerial::Packets
{
	static const uint8_t XCMD_MAGIC[4] = { 0x58, 0x43, 0x00, 0xFF };

    [FlagsAttribute]
    public enum class CommandFlags : System::UInt32
    {
        None       = 0,
        HoldWipers = 0x01,
    };

    [FlagsAttribute]
    public enum class DebugFlags : System::UInt32
    {
        None   = 0,
        Update = 0x01,
    };

    public interface class IXCommand
    {
        property Byte CommandID { Byte get(); }
    };

    [StructLayoutAttribute(LayoutKind::Sequential, Pack = 1)]
    public value struct XCMD_Header
    {
        Byte magic0;
        Byte magic1;
        Byte id;
        Byte magic3;
        CommandFlags flags;
    };

	[StructLayoutAttribute(LayoutKind::Sequential, Pack = 1)]
    public ref struct XCMD_SetWipers : IXCommand
    {
        literal Byte ID = 0x01;        virtual property Byte CommandID { Byte get() { return ID; } }
        XCMD_Header header;

        Byte top;
        Byte bot;
        Byte mid;

        Byte offset;
        Byte gain;

        Byte _reserved1;
        Byte _reserved2;
        Byte _reserved3;

        property CommandFlags flags
        {
            CommandFlags get() { return header.flags; }
            void set(CommandFlags value) { header.flags = value; }
        }
    };

    [StructLayoutAttribute(LayoutKind::Sequential, Pack = 1)]
    public ref struct XCMD_SetState : IXCommand
    {
        literal Byte ID = 0x02;        virtual property Byte CommandID { Byte get() { return ID; } }
        XCMD_Header header;

        uint32_t state;

        property CommandFlags flags
        {
            CommandFlags get() { return header.flags; }
            void set(CommandFlags value) { header.flags = value; }
        }
    };

    [StructLayoutAttribute(LayoutKind::Sequential, Pack = 1)]
    public ref struct XCMD_SetDebugFlags : IXCommand
    {
        literal Byte ID = 0x03;        virtual property Byte CommandID { Byte get() { return ID; } }
        XCMD_Header header;

        DebugFlags debugFlags;

        property CommandFlags flags
        {
            CommandFlags get() { return header.flags; }
            void set(CommandFlags value) { header.flags = value; }
        }
    };
}
