#pragma once
#pragma managed(push, off)

#pragma once
#include <vector>
#include <numeric>
#include <stdexcept>

class CLinearRegress {
public:
    struct Result {
        double slope{};
        double intercept{};
        bool valid{ false };
    };

    static Result Fit(const std::vector<double>& x, const std::vector<double>& y) noexcept {
        const size_t n = x.size();
        if (n < 2 || n != y.size())
            return {};

        double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
        for (size_t i = 0; i < n; ++i) {
            sumX  += x[i];
            sumY  += y[i];
            sumXY += x[i] * y[i];
            sumX2 += x[i] * x[i];
        }

        const double denom = n * sumX2 - sumX * sumX;
        if (std::abs(denom) < 1e-12)
            return {};

        const double slope = (n * sumXY - sumX * sumY) / denom;
        const double intercept = (sumY - slope * sumX) / n;

        return { slope, intercept, true };
    }
};

#pragma managed(pop)