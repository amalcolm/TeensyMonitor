#pragma once

#include "CDiscontinuityAnalyzer.h"
#include <vector>

using namespace System::Collections::Generic;

namespace PsycSerial::Math
{
	public ref class ZFixer {

	private:
		std::vector<XY>* m_data = nullptr;
		
		Dictionary<System::String^, double>^ m_telemetry = nullptr;
	public:
		double Fix(double x, double y);

		ZFixer();
		~ZFixer();

		void Reset();

		property Dictionary<System::String^, double>^ Telemetry {
			Dictionary<System::String^, double>^ get() {return m_telemetry; }
			void set(Dictionary<System::String^, double>^ value) { m_telemetry = value; }
		}

		static void DoTest() { CDiscontinuityAnalyzer::DoTest(); }
	};
}
