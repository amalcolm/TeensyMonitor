#pragma once

#include "CQuadRegress.h"
#include <vector>

namespace PsycSerial::Math
{
	public ref class ZFixer {

	private:
		CQuadRegress* m_regressor = nullptr;
		
		std::vector<XY>* m_data = nullptr;
	public:
		double fix(double x, double y);

		ZFixer();
		~ZFixer();
	};
}
