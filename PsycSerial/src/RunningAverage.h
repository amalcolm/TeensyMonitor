// RunningAverageMinMaxCli.h
#pragma once

#include <cstdint>
#include "CRunningAverage.h"   // contains RunningAverageMinMax

using namespace System;

namespace YourNamespace
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
        RunningAverage(int windowSize) { _p = new Native(ToSizeT(windowSize)); }
        

        // Deterministic cleanup
        ~RunningAverage() { this->!RunningAverage(); }

        // Finalizer (in case user forgets to Dispose)
        !RunningAverage() { delete _p; _p = nullptr; }

        void Reset(int windowSize)
        {
            if (!_p) throw gcnew ObjectDisposedException("RunningAverage");
            _p->Reset(ToSizeT(windowSize));
        }

        void Add(double value)
        {
            if (!_p) throw gcnew ObjectDisposedException("RunningAverage");
            _p->Add(value);
        }

        property double Min
        {
            double get()
            {
                if (!_p) throw gcnew ObjectDisposedException("RunningAverage");
                return _p->GetMin();
            }
        }

        property double Max
        {
            double get()
            {
                if (!_p) throw gcnew ObjectDisposedException("RunningAverage");
                return _p->GetMax();
            }
        }
    };
}
