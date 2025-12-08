#include "Decoder.h"
#include "../Utilities.h"


namespace PsycSerial
{
	IPacket^ Decoder::Convert(const CDecodedPacket& nativePacket)
	{

		switch (nativePacket.kind)
		{
			case PacketKind::Data:
			{
				DataPacket^ pkt = DataPacket::Rent();

				pkt->TimeStamp     = nativePacket.data.timeStamp;
				pkt->State         = static_cast<HeadState>(nativePacket.data.state);
				pkt->HardwareState = nativePacket.data.hardwareState;
				pkt->SensorState   = nativePacket.data.sensorState;
				for (size_t i = 0; i < CDataPacket::A2D_NUM_CHANNELS; ++i)
				{
					pkt->Channel[i] = nativePacket.data.channel[i];
				}
				return pkt;
			}

			case PacketKind::Block:
			{
				BlockPacket^ blockPkt = BlockPacket::Rent();

				blockPkt->TimeStamp = nativePacket.block.timeStamp;
				blockPkt->State     = static_cast<HeadState>(nativePacket.block.state);

				blockPkt->Count		= nativePacket.block.count;

				for (size_t i = 0; i < nativePacket.block.count; ++i)
				{
					DataPacket^ dataPkt = blockPkt->BlockData[i];
					if (dataPkt == nullptr)
						dataPkt = blockPkt->BlockData[i] = DataPacket::Rent();
					else
						dataPkt->Reset();

					dataPkt->TimeStamp     = nativePacket.block.blockData[i].timeStamp;
					dataPkt->State         = static_cast<HeadState>(nativePacket.block.blockData[i].state);
					dataPkt->HardwareState = nativePacket.block.blockData[i].hardwareState;
					dataPkt->SensorState   = nativePacket.block.blockData[i].sensorState;

					for (size_t ch = 0; ch < CDataPacket::A2D_NUM_CHANNELS; ++ch)
					{
						dataPkt->Channel[ch] = nativePacket.block.blockData[i].channel[ch];
					}
					blockPkt->BlockData[i] = dataPkt;
				}

				return blockPkt;
			}

			case PacketKind::Text:
			{
				TextPacket^ textPkt = TextPacket::Rent();
				const uint8_t* utf8Bytes = nativePacket.text.utf8Bytes;

				textPkt->TimeStamp  = nativePacket.text.timeStamp;
				textPkt->State      = HeadState::None;
				textPkt->Text       = AString::FromUtf8(utf8Bytes, 0, nativePacket.text.length);
				textPkt->Length		= textPkt->Text->Length;

				return textPkt;
			}

		default:
			// Unknown packet type
			return nullptr;
		}
	}
}
