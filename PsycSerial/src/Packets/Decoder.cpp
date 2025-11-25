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

				pkt->TimeStamp     = DateTime::Today.AddMilliseconds(nativePacket.data.timeStamp);
				pkt->State         = nativePacket.data.state;
				pkt->HardwareState = nativePacket.data.hardwareState;
				for (size_t i = 0; i < CDataPacket::A2D_NUM_CHANNELS; ++i)
				{
					pkt->Channel[i] = nativePacket.data.channel[i];
				}
				return pkt;
			}

			case PacketKind::Block:
			{
				BlockPacket^ blockPkt = BlockPacket::Rent();

				blockPkt->TimeStamp = DateTime::Today.AddMilliseconds(nativePacket.block.timeStamp);
				blockPkt->State     = nativePacket.block.state;

				blockPkt->Count		= nativePacket.block.count;

				for (size_t i = 0; i < nativePacket.block.count; ++i)
				{
					DataPacket^ dataPkt = blockPkt->BlockData[i];
					if (dataPkt == nullptr)
						dataPkt = blockPkt->BlockData[i] = DataPacket::Rent();
					else
						dataPkt->Reset();

					dataPkt->TimeStamp     = DateTime::Today.AddMilliseconds(nativePacket.block.blockData[i].timeStamp);
					dataPkt->State         = nativePacket.block.blockData[i].state;
					dataPkt->HardwareState = nativePacket.block.blockData[i].hardwareState;
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
				textPkt->TimeStamp  = DateTime::Today.AddMilliseconds(nativePacket.text.timeStamp);
				textPkt->State      = 0;
				textPkt->Text       = ConvertStdString(nativePacket.text.text);
				textPkt->Length		= textPkt->Text->Length;

				return textPkt;
			}

		default:
			// Unknown packet type
			return nullptr;
		}
	}
}
