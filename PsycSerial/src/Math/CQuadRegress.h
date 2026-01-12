#pragma once
#pragma managed(push, off)

// QuadraticRegression.hpp
#pragma once
#include "CMatrix3x3.h"
#include "CLinearRegress.h"
#include <vector>

class QuadraticRegression {
public:
    struct Result {
        double a{}, b{}, c{};
        bool valid{ false };
    };

    static Result Fit(const std::vector<double>& x, const std::vector<double>& y) noexcept {
        const size_t n = x.size();
        if (n < 3 || n != y.size())
            return {};

        double sumX = 0, sumX2 = 0, sumX3 = 0, sumX4 = 0;
        double sumY = 0, sumXY = 0, sumX2Y = 0;

        for (size_t i = 0; i < n; ++i) {
            const double xi = x[i], yi = y[i];
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
        if (!solved) {
			auto linResult = CLinearRegress::Fit(x, y);  // singular, fallback to linear
            return {linResult.slope, linResult.intercept, 0.0, linResult.valid };
        }

        return { (*solved)[0], (*solved)[1], (*solved)[2], true };
    }
};

#pragma managed(pop)