#pragma once
#pragma managed(push, off)

#include "CTypes.h"
#include "CQuadRegress.h"
#include <span>
#include <cmath>

class CDiscontinuityAnalyzer
{
public:
    struct Result {
        bool valid{ false };

        // Fit results from each end
        CQuadRegress::Result _left;
        CQuadRegress::Result right;

        // Discontinuity metrics
        double deltaY{ 0.0 };       // offset difference at junction
        double deltaSlope{ 0.0 };   // slope mismatch
        double deltaCurvature{ 0.0 }; // curvature mismatch
        double score{ 0.0 };        // optional combined score

        // Optional helper for debugging
        std::string ToString() const;
    };

    // Perform analysis on a given data window
    static Result Analyze(std::span<const XY> data, size_t edgeCount = 4) noexcept;

	static void DoTest();
};

#pragma managed(pop)
