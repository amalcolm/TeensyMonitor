#include "CDiscontinuityAnalyzer.h"
#include <sstream>

CDiscontinuityAnalyzer::Result CDiscontinuityAnalyzer::Analyze(std::span<const XY> data, size_t edgeCount) noexcept
{
    Result r{};
    const size_t n = data.size();

    if (n < 2 * edgeCount)
        return r; // invalid / insufficient data

    // Left edge
    std::span<const XY> leftEdge(data.data(), edgeCount);

    // Right edge
    std::span<const XY> rightEdge(data.data() + n - edgeCount, edgeCount);

    // Fit both ends
    r._left = CQuadRegress::Fit(leftEdge);
    r.right = CQuadRegress::Fit(rightEdge);

    if (!r._left.valid || !r.right.valid)
        return r; // fallback - can’t compare

    
    // Evaluate each quadratic at the midpoint between segments
    double xMid = 0.5 * (leftEdge.back().x + rightEdge.front().x);

    double yL = r._left.a * xMid * xMid + r._left.b * xMid + r._left.c;
    double yR = r.right.a * xMid * xMid + r.right.b * xMid + r.right.c;

    r.deltaY = yL - yR;
    r.deltaSlope = r._left.b - r.right.b;
    r.deltaCurvature = r._left.a - r.right.a;

    // Combine into a single continuity score (tweakable)
    constexpr double kSlopeWeight = 0.05;
    constexpr double kCurvWeight = 0.01;

    r.score = std::abs(r.deltaY) + kSlopeWeight * std::abs(r.deltaSlope) + kCurvWeight * std::abs(r.deltaCurvature);
    r.valid = true;
    return r;
}

// Optional for debugging/logging
std::string CDiscontinuityAnalyzer::Result::ToString() const
{
    std::ostringstream oss;
    if (!valid)
        return "Invalid fit";

    oss << "dY=" << deltaY
        << " dSlope=" << deltaSlope
        << " dCurv=" << deltaCurvature
        << " Score=" << score;
    return oss.str();
}
