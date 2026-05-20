#pragma once
#include <cmath>

double CentreOfMagnification(double sensorGain0,
                             double sensorGain8,
                             double sensorGain16)
{
    // Actual amplifier multipliers for the lookup values.
    constexpr double g0  = 1.20;
    constexpr double g8  = 4.34;
    constexpr double g16 = 7.74;

    // We model:
    //
    //     sensor = centre + actualGain * (input - centre)
    //
    // Rearranged, this is just a straight line:
    //
    //     sensor = intercept + slope * actualGain
    //
    // The centre of magnification is the value when actualGain == 0,
    // which is the fitted intercept.

    constexpr int n = 3;

    const double sumG  = g0 + g8 + g16;
    const double sumY  = sensorGain0 + sensorGain8 + sensorGain16;

    const double sumGG =
        g0 * g0 +
        g8 * g8 +
        g16 * g16;

    const double sumGY =
        g0  * sensorGain0 +
        g8  * sensorGain8 +
        g16 * sensorGain16;

    const double denom = n * sumGG - sumG * sumG;

    if (std::abs(denom) < 1e-12)
        return NAN;

    const double slope =
        (n * sumGY - sumG * sumY) / denom;

    const double centre =
        (sumY - slope * sumG) / n;

    return centre;
}