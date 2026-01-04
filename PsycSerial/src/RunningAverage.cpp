#include "RunningAverage.h"

namespace PsycSerial
{


    static size_t ToSizeT(int v)
    {
        if (v <= 0) throw gcnew ArgumentOutOfRangeException("windowSize");
        return static_cast<size_t>(v);
    }

    // Deterministic cleanup
    RunningAverage::~RunningAverage() { this->!RunningAverage(); }

    // Finalizer (in case user forgets to Dispose)
    RunningAverage::!RunningAverage() { delete _p; _p = nullptr; }


    RunningAverage::RunningAverage(int windowSize)
    {
	    _p = new Native(ToSizeT(windowSize));
    }

    void RunningAverage::Reset(int windowSize)
    {
        if (!_p) throw gcnew ObjectDisposedException("RunningAverage");
        _p->Reset(ToSizeT(windowSize));
    }

    void RunningAverage::Add(double value)
    {
        if (!_p) throw gcnew ObjectDisposedException("RunningAverage");
        _p->Add(value);
    }

    double RunningAverage::Min::get()
    {
        if (!_p) throw gcnew ObjectDisposedException("RunningAverage");
        return _p->GetMin();
    }
    
    double RunningAverage::Max::get()
    {
        if (!_p) throw gcnew ObjectDisposedException("RunningAverage");
        return _p->GetMax();
	}

} // namespace PsycSerial
