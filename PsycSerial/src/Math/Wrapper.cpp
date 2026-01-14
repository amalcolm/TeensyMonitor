#include "Wrapper.h"
#include "CDiscontinuityFixer.h"

using namespace PsycSerial::Math;



double ZFixer::Fix(double x, double y) {
	static CDiscontinuityFixer fixer{};

	auto result = fixer.Fix(x,y);


    if (m_telemetry != nullptr) {
        m_telemetry[ZFixer::keyDeltaY         ] = 512 + 0.0001 * result.deltaY;
        m_telemetry[ZFixer::keyDeltaSlope     ] = 512 + 0.0001 * result.deltaSlope;
        m_telemetry[ZFixer::keyDeltaCurvature ] = 512 + 0.0001 * result.deltaCurvature;
        m_telemetry[ZFixer::keyScore          ] = 512 + 0.0001 * result.score;
	}

    return result.output.y();
}


void ZFixer::Close() {
    // close any file if doing diagnostics
	m_telemetry = nullptr;
}