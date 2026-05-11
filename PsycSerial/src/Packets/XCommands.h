#pragma once
#include <stdint.h>

using namespace System;
using namespace System::Runtime::InteropServices;

namespace PsycSerial::Packets
{
	static const uint8_t XCMD_MAGIC[4] = { 0x58, 0x43, 0x00, 0xFF };

    public interface class IXCommand
    {
        property Byte CommandID { Byte get(); }
    };

	[StructLayoutAttribute(LayoutKind::Sequential, Pack = 1)]
    public ref struct XCMD_SetWipers : IXCommand
    {
        literal Byte ID = 0x01;        virtual property Byte CommandID { Byte get() { return ID; } }
        literal Byte FLAG_HOLD = 0x01;

        Byte top;
        Byte bot;
        Byte mid;

        Byte offset;
        Byte gain;

        Byte flags;  // bit 0 = hold
        Byte _reserved2;
        Byte _reserved3;
    };

    [StructLayoutAttribute(LayoutKind::Sequential, Pack = 1)]
    public ref struct XCMD_SetState : IXCommand
    {
        literal Byte ID = 0x02;        virtual property Byte CommandID { Byte get() { return ID; } }

        uint32_t state;
        uint32_t flags;  // bit 0 = hold
    };
}
