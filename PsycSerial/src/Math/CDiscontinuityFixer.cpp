#include "CDiscontinuityFixer.h"
#include "CDiscontinuityAnalyzer.h"
#include "CQuadRegress.h"

CDiscontinuityFixer::CDiscontinuityFixer() {
	m_data.reserve(ZFIXER_BUFFER_SIZE);
}

CDiscontinuityFixer::Result CDiscontinuityFixer::Fix(double x, double y) noexcept
{

	m_data.emplace_back(x, y, currentOffsetY);   if (m_data.size() > ZFIXER_BUFFER_SIZE) m_data.erase(m_data.begin(), m_data.end() - ZFIXER_WINDOW_SIZE);
	if (m_data.size() < ZFIXER_WINDOW_SIZE) return Result::FromFail(x, y, currentOffsetY);

	auto start = m_data.size() - ZFIXER_WINDOW_SIZE;  // analyze the most recent window
	auto span = std::span<const XY>(m_data.data() + start, ZFIXER_WINDOW_SIZE);

	auto analysis = CDiscontinuityAnalyzer::Analyze(span, ZFIXER_WINDOW_EDGE);

	if (analysis.valid == false) return Result::FromFail(x, y, currentOffsetY);

	return Process(analysis);
}

CDiscontinuityFixer::Result CDiscontinuityFixer::Process(CDiscontinuityAnalyzer::Result analysis) noexcept
{
	static constexpr double THRESHOLD_SCORE = 10.0;
	Result result(analysis);

	// only process when score passes test
	if (std::abs(analysis.score) <= THRESHOLD_SCORE)
		return result;

	auto start = m_data.size() - ZFIXER_WINDOW_SIZE + ZFIXER_WINDOW_EDGE;
	
	for (size_t i = start; i < m_data.size(); i++)
		m_data[i].adjustOffsetY(-analysis.deltaY);


	auto  leftCurve = analysis.left;
	auto rightCurve = CQuadRegress::Fit(std::span<const XY>{m_data.data() + m_data.size() - ZFIXER_WINDOW_SIZE, ZFIXER_WINDOW_EDGE});


	for (int i = start; i < m_data.size() - ZFIXER_WINDOW_EDGE; i++)
	{
		double x = m_data[i].x();
	
		double yL = leftCurve.EvaluateAt(x);
		double yR = rightCurve.EvaluateAt(x);
		double averageY = 0.5 * (yL + yR);

		m_data[i].adjustOffsetY(averageY - m_data[i].y());  // adjust offsetY to align curves
	}

	result.output = m_data[m_data.size() - ZFIXER_WINDOW_EDGE];
	result.changed = true;

	currentOffsetY += -analysis.deltaY;
	return result;
}