#include "Setup.h"

namespace PsycSerial
{

	void Setup::ParseHandshakeResponse(String^ response)
	{
	
		array<String^>^ parts = response->TrimStart('<')->Split(L':');

		for each (String^ part in parts)
		{
			array<String^>^ kv = part->Split('=');
			if (kv->Length != 2)
				continue;
		
			String^ key = kv[0]->Trim();
			String^ value = kv[1]->Trim();

			if (key->Equals("DEVICE_VERSION", StringComparison::InvariantCultureIgnoreCase))
			{
				DeviceVersion = value;
			}

			if (key->Equals("LOOP_MS", StringComparison::InvariantCultureIgnoreCase))
			{
				int loopMs = 0;
				if (Int32::TryParse(value, loopMs))
				{
					LoopMS = loopMs;
				}
			}

		}
	}
}