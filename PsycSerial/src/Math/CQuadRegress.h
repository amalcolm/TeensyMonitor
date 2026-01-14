#pragma once
#pragma managed(push, off)

// QuadraticRegression.hpp
#pragma once
#include "CMatrix3x3.h"
#include "CLinearRegress.h"

class CQuadRegress {
public:

    static RegressResult Fit(std::span<const XY> p) noexcept {
        const size_t n = p.size();
        if (n < 3)
            return {};

        double sumX = 0, sumX2 = 0, sumX3 = 0, sumX4 = 0;
        double sumY = 0, sumXY = 0, sumX2Y = 0;

        for (size_t i = 0; i < n; ++i) {
            const double xi = p[i].x(), yi = p[i].y();
            const double xi2 = xi * xi;
            sumX   += xi;
            sumX2  += xi2;
            sumX3  += xi2 * xi;
            sumX4  += xi2 * xi2;
            sumY   += yi;
            sumXY  += xi * yi;
            sumX2Y += xi2 * yi;
        }

        Mat3 A = { {
            { sumX4, sumX3, sumX2 },
            { sumX3, sumX2, sumX  },
            { sumX2, sumX,  static_cast<double>(n) }
        } };
        Vec3 B = { sumX2Y, sumXY, sumY };

        auto solved = CMatrix3x3::TrySolve(A, B);
        if (!solved) 
			return CLinearRegress::Fit(p);  // singular, fallback to linear
        
        return RegressResult::GetQuadraticResult(solved->at(0), solved->at(1), solved->at(2), true, p);
    }
};

#pragma managed(pop)