#pragma once

#include "CDiscontinuityFixer.h"
#include "CDiscontinuityAnalyzer.h"

using namespace System::Collections::Generic;

namespace PsycSerial::Math
{
	public ref class ZFixer {

	private:
		Dictionary<System::String^, double>^ m_telemetry = nullptr;
		CDiscontinuityFixer* m_fixer = nullptr;
		 
		static initonly System::String^ keyDeltaY         = "DeltaY";
		static initonly System::String^ keyDeltaSlope     = "DeltaSlope";
		static initonly System::String^ keyDeltaCurvature = "DeltaCurvature";
		static initonly System::String^ keyScore          = "Score";
	public:
		ZFixer();
	   ~ZFixer();


		bool Fix(double% x, double% y);
		void Predict(double% x, double% y);

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
