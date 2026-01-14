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

	std::vector<XY> workingData(ZFIXER_WINDOW_SIZE);
	std::span<XY> workingSpan = std::span<XY>(workingData.data(), ZFIXER_WINDOW_SIZE);
	XY::CentreX(span, workingSpan);

	auto analysis = CDiscontinuityAnalyzer::Analyze(workingSpan, ZFIXER_WINDOW_EDGE);

	if (analysis.valid == false) return Result::FromFail(x, y, currentOffsetY);

	return Process(workingSpan, analysis);
}

CDiscontinuityFixer::Result CDiscontinuityFixer::Process(std::span<const XY> workingData, CDiscontinuityAnalyzer::Result analysis) noexcept
{
	static constexpr double THRESHOLD_SCORE = 10.0;
	Result result(analysis);

	// only process when score passes test
	if (std::abs(analysis.score) <= THRESHOLD_SCORE)
		return result;

	auto start = m_data.size() - ZFIXER_WINDOW_SIZE + ZFIXER_WINDOW_EDGE;
	
	// move right edge down by deltaY
	for (size_t i = start; i < m_data.size(); i++)
		m_data[i].adjustOffsetY(-analysis.deltaY);

	// create working copy of right edge for fitting
	std::vector<XY> workingEdge(ZFIXER_WINDOW_EDGE);
	auto span = std::span<const XY>{ m_data.data() + m_data.size() - ZFIXER_WINDOW_EDGE, ZFIXER_WINDOW_EDGE };
	XY::CentreX(span, workingEdge);

	// get fitted curves
	auto  leftCurve = analysis.left;
	auto rightCurve = CQuadRegress::Fit(workingEdge);

	// adjust all non-edge points to average of curves
	for (size_t i = start; i < m_data.size() - ZFIXER_WINDOW_EDGE; i++)
	{
		double x = m_data[i].x();
	
		double yL = leftCurve.EvaluateAt(x);
		double yR = rightCurve.EvaluateAt(x);
		double averageY = 0.5 * (yL + yR);

		m_data[i].adjustOffsetY(averageY - m_data[i].y());  // adjust offsetY to align curves
	}

	// the XY returned is last non-edge point
	result.output = m_data[m_data.size() - ZFIXER_WINDOW_EDGE - 1];
	result.changed = true;

	currentOffsetY += -analysis.deltaY;
	return result;
}