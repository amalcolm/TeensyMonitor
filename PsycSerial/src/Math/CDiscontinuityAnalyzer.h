#pragma once
#pragma managed(push, off)

#include "CTypes.h"
#include "CLinearRegress.h"
#include <span>
#include <cmath>

class CDiscontinuityAnalyzer
{
public:
    struct Result {
        bool valid{ false };

        // Fit results from each end
        RegressResult left;
        RegressResult right;

        // Discontinuity metrics
		std::span<const XY> dataSpan; // original data span analyzed
        double deltaY{ 0.0 };       // offset difference at junction
        double deltaSlope{ 0.0 };   // slope mismatch
        double deltaCurvature{ 0.0 }; // curvature mismatch
        double score{ 0.0 };        // optional combined score

        // Optional helper for debugging
        std::string ToString() const;

        Result(std::span<const XY> span) {
            dataSpan = span;
        };

    };

    // Perform analysis on a given data window
    static Result Analyze(std::span<const XY> data, size_t edgeCount = 0) noexcept;

	static void DoTest();
	static XY GetTestValue();
};


#pragma managed(pop)
