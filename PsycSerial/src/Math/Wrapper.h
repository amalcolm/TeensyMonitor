#pragma once

#include "CDiscontinuityAnalyzer.h"

using namespace System::Collections::Generic;

namespace PsycSerial::Math
{
	public ref class ZFixer {

	private:
		
		Dictionary<System::String^, double>^ m_telemetry = nullptr;

		static System::String^ keyDeltaY         = gcnew System::String("DeltaY");
		static System::String^ keyDeltaSlope     = gcnew System::String("DeltaSlope");
		static System::String^ keyDeltaCurvature = gcnew System::String("DeltaCurvature");
		static System::String^ keyScore          = gcnew System::String("Score");

	public:
		double Fix(double x, double y);

		void Close();

		property Dictionary<System::String^, double>^ Telemetry {
			Dictionary<System::String^, double>^ get() {return m_telemetry; }
			void set(Dictionary<System::String^, double>^ value) { m_telemetry = value; }
		}

		static void DoTest() { CDiscontinuityAnalyzer::DoTest(); }

		static void GetTestValue( double% x, double% y ) {
			auto v = CDiscontinuityAnalyzer::GetTestValue();
			x = v.x();
			y = v.y();
		}
	};
}
