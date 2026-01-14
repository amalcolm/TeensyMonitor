#pragma once

#pragma managed(push, off)

#include "CTypes.h"
#include "CDiscontinuityAnalyzer.h"
#include <span>
#include <vector>

class CDiscontinuityFixer {
	private:
		static inline constexpr size_t ZFIXER_BUFFER_SIZE = 4096;
		static inline constexpr size_t ZFIXER_WINDOW_SIZE =   10;
		static inline constexpr size_t ZFIXER_WINDOW_EDGE =    4;

		std::vector<XY> m_data{};

		double currentOffsetY{ 0.0 };

	public:
		CDiscontinuityFixer();
		struct Result {
			public:
			bool valid{ false };

			bool changed{ false };
			XY output{ 0.0, 0.0, 0.0 };

			RegressResult left;
			RegressResult right;

			// Discontinuity metrics
			double deltaY        { 0.0 };       // offset difference at junction
			double deltaSlope    { 0.0 };       // slope mismatch
			double deltaCurvature{ 0.0 };       // curvature mismatch
			double score         { 0.0 };       // optional combined score

			Result() = default;

			Result( const CDiscontinuityAnalyzer::Result& r ) :
				valid          ( r.valid          ),
				left           ( r.left           ),
				right          ( r.right          ),
				deltaY         ( r.deltaY         ),
				deltaSlope     ( r.deltaSlope     ),
				deltaCurvature ( r.deltaCurvature ),
				score          ( r.score          )
			{}

			static Result FromFail(double x, double y, double offsetY) {
				Result r{};  // all false/zero
				r.output = XY(x, y, offsetY);
				return r;
			}
		};

		Result Fix(double x, double y) noexcept;
		Result Process(std::span<XY> workingData, CDiscontinuityAnalyzer::Result analysis) noexcept;

};

#pragma managed(pop)