#pragma once

#include "CPackets.h"
#include "Packets.h"

namespace PsycSerial
{

	ref class Decoder
	{
		public:
			static IPacket^ Convert(const CDecodedPacket& nativePacket);
	};

}