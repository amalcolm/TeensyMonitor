#pragma once

namespace PsycSerial::Math
{
	public ref class ZFixer {

	private:
		CRegress m_left;
		CRegress m_right;

	public:
		double fix(double x, double y);
	};
}