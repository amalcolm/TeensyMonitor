#include "Wrapper.h"

using namespace PsycSerial::Math;

constexpr size_t ZFIXER_BUFFER_SIZE = 4096;
constexpr size_t ZFIXER_WINDOW_SIZE = 8;

ZFixer::ZFixer() {
    m_regressor = new CQuadRegress();
    
	m_data = new std::vector<XY>(ZFIXER_BUFFER_SIZE);
}

ZFixer::~ZFixer() {
    delete m_regressor;
    delete m_data;
}

double ZFixer::fix(double x, double y) {

    m_data->emplace_back( (x, y) );    if (m_data->size() > ZFIXER_WINDOW_SIZE) m_data->erase(m_data->begin(), m_data->end() - ZFIXER_WINDOW_SIZE);

	if (m_data->size() < ZFIXER_WINDOW_SIZE) return y;

    auto span = std::span<const XY>( m_data->data() + (m_data->size() - ZFIXER_WINDOW_SIZE), ZFIXER_WINDOW_SIZE);



    return 0.0;
}

