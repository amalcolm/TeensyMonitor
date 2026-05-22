#pragma once
#include "DataTypes.h"
#include <stddef.h>


void FillBufferWithNoise(TimedSample* buffer, size_t size, double period = 0.0);