#pragma once
#include <stdint.h>

using namespace System;
using namespace System::Runtime::InteropServices;

namespace PsycSerial
{
	static const uint8_t XCMD_MAGIC[4] = { 0x58, 0x43, 0x00, 0xFF };

    public interface class IXCommand
    {
        property Byte CommandID { Byte get(); }
    };

	[StructLayoutAttribute(LayoutKind::Sequential, Pack = 1)]
    public ref struct XCMD_SetWipers : IXCommand
    {
        literal Byte ID = 0x01;

        virtual property Byte CommandID { Byte get() { return ID; } }

        Byte top;
        Byte bot;
        Byte mid;

        Byte offset;
        Byte gain;

        Byte _reserved1;
        Byte _reserved2;
        Byte _reserved3;
    };
};
