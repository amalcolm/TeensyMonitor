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
	XY::CentreX(span, workingData);

	auto analysis = CDiscontinuityAnalyzer::Analyze(workingData, ZFIXER_WINDOW_EDGE);

	if (analysis.valid == false) return Result::FromFail(x, y, currentOffsetY);

	return Process(workingData, analysis);
}

CDiscontinuityFixer::Result CDiscontinuityFixer::Process(std::span<XY> workingData, CDiscontinuityAnalyzer::Result analysis) noexcept
{
	static constexpr double THRESHOLD_SCORE = 10.0;
	Result result(analysis);

	// only process when score passes test
	if (std::abs(analysis.score) <= THRESHOLD_SCORE)
		return result;

	// adjust right edge of WORKINGDATA down by deltaY
	auto workingEdge = std::span<XY>(workingData.data() + (workingData.size() - ZFIXER_WINDOW_EDGE), ZFIXER_WINDOW_EDGE);
	
	for (size_t i = 0; i < ZFIXER_WINDOW_EDGE; i++)
		workingEdge[i].adjustOffsetY(-analysis.deltaY);

	
	// get fitted curves
	auto  leftCurve = analysis.left;
	auto rightCurve = CQuadRegress::Fit(workingEdge);




	// move right edge of M_DATA down by deltaY
	for (size_t i = m_data.size() - ZFIXER_WINDOW_EDGE; i < m_data.size(); i++)
		m_data[i].adjustOffsetY(-analysis.deltaY);


	size_t m_dataIndexOffset = m_data.size() - workingData.size();

	// adjust all non-edge points to average of curves
	for (size_t i = ZFIXER_WINDOW_EDGE; i < workingData.size() - ZFIXER_WINDOW_EDGE; i++)
	{
		double x = workingData[i].x();
	
		double yL = leftCurve.EvaluateAt(x);
		double yR = rightCurve.EvaluateAt(x);
		double averageY = 0.5 * (yL + yR);

		size_t m_dataIndex = i + m_dataIndexOffset;
		m_data[m_dataIndex].adjustOffsetY(averageY - m_data[m_dataIndex].y());  // adjust offsetY to align curves
	}

	// the XY returned is last non-edge point
	result.output = m_data[m_data.size() - ZFIXER_WINDOW_EDGE - 1];
	result.changed = true;

	currentOffsetY += -analysis.deltaY;
	return result;
}