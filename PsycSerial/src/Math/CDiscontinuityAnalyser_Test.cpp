#include "CDiscontinuityAnalyzer.h"
#include <iostream>
#include <random>
#include <vector>

void CDiscontinuityAnalyzer::DoTest()
{
    constexpr size_t N = 10;       // total samples
    constexpr size_t EDGE = 4;     // edge regression window
    constexpr double noiseLevel = 0.5;  // random noise amplitude

    std::vector<XY> samples;
    samples.reserve(N);

    // Set up RNG for noise
    std::mt19937 rng(1234);
    std::normal_distribution<double> noise(0.0, noiseLevel);

    // Generate test signal:
    // y = x + noise for first half, y = x + 50 + noise for second half
    for (size_t i = 0; i < N; ++i)
    {
        double x = static_cast<double>(i);
        double y = x + noise(rng);
        if (i >= N / 2)
            y += 50.0; // discontinuity
        samples.push_back({ x, y });
    }

    // Analyze the full window
    auto result = Analyze(samples, EDGE);

    // Show output
    std::cout << "=== Discontinuity Test ===\n";
    std::cout << "Samples: " << N << "  EdgeCount: " << EDGE << "\n";
    std::cout << result.ToString() << "\n\n";

    if (result.valid)
    {
        std::cout << "Left fit:  a=" << result._left.a
            << "  b=" << result._left.b
            << "  c=" << result._left.c << "\n";
        std::cout << "Right fit: a=" << result.right.a
            << "  b=" << result.right.b
            << "  c=" << result.right.c << "\n";
    }
}
