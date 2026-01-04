// RunningAverageMinMaxCli.h
#pragma once

#include <cstdint>
#include "CRunningAverage.h"   // contains RunningAverageMinMax

using namespace System;

namespace PsycSerial
{
    public ref class RunningAverage sealed
    {
    private:
        // If your native is templated, change this to e.g.:
        // using Native = RunningAverageMinMax<uint32_t>;
        using Native = CRunningAverageMinMax;

        Native* _p = nullptr;

        static size_t ToSizeT(int v)
        {
            if (v <= 0) throw gcnew ArgumentOutOfRangeException("windowSize");
            return static_cast<size_t>(v);
        }

    public:
        RunningAverage(int windowSize);
        ~RunningAverage();
		!RunningAverage();


        void Reset(int windowSize);
        void Add(double value);

        property double Min { double get(); }
        property double Max { double get(); }
    };
}
