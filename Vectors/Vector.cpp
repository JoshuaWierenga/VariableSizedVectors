//TODO Merge string info arrays into a single structure
//TODO Automate generation of typeVectorConstructorArguments
#ifdef _DEVELOPMENT
#include <intrin.h>
#include <sstream>
#include "Vector.h"
#endif

//TODO Generate automatically?
template<typename T, uint_fast16_t X>
vector<T, X>::vector(const __m128i vector) : vector_128_(vector)
{
}

template<typename T, uint_fast16_t X>
vector<T, X>::vector(const __m256i vector) : vector_256_(vector)
{
}

#pragma region vector<int32_t, 128>

// Constructors
template<typename T, uint_fast16_t X>
vector<T, X>::vector(const int32_t value) requires(std::is_same_v<T, int32_t> && is_simd_128<X>) : vector_128_(_mm_set1_epi32(value))
{
}

template<typename T, uint_fast16_t X>
vector<T, X>::vector(const int32_t v1, const int32_t v2, const int32_t v3, const int32_t v4) requires(std::is_same_v<T, int32_t> && is_simd_128<X>) : vector_128_(_mm_setr_epi32(v1, v2, v3, v4))
{
}

//TODO Figure out how to make the types of the parameters enough to figure out which version to use and not require specifying when accessing vector
template<typename T, uint_fast16_t X>
vector<int32_t, 128> vector<T, X>::Blend(const vector<int32_t, 128> comparision, const vector<int32_t, 128> falseValue, const vector<int32_t, 128> trueValue)
{
	return _mm_blendv_epi8(falseValue.vector_128_, trueValue.vector_128_, comparision.vector_128_);
}

// Assignment Operators
template<typename T, uint_fast16_t X>
vector<T, X>& vector<T, X>::operator+=(const vector<int32_t, 128>& rhs)
{
	this->vector_128_ = _mm_add_epi32(this->vector_128_, rhs.vector_128_);
	return *this;
}

// Comparision Operators
//TODO Fix this not working allow implicit casts when more than one version exist
template<typename T, uint_fast16_t X>
vector<int32_t, 128> vector<T, X>::operator>(const vector<int32_t, 128>& vector2) const
{
	return _mm_cmpgt_epi32(this->vector_128_, vector2.vector_128_);
}
#pragma endregion
#pragma region vector<int32_t, 256>

// Constructors
template<typename T, uint_fast16_t X>
vector<T, X>::vector(const int32_t value) requires(std::is_same_v<T, int32_t> && is_simd_256<X>) : vector_256_(_mm256_set1_epi32(value))
{
}

template<typename T, uint_fast16_t X>
vector<T, X>::vector(const int32_t v1, const int32_t v2, const int32_t v3, const int32_t v4, const int32_t v5, const int32_t v6, const int32_t v7, const int32_t v8) requires(std::is_same_v<T, int32_t> && is_simd_256<X>) : vector_256_(_mm256_setr_epi32(v1, v2, v3, v4, v6, v6, v7, v8))
{
}

//TODO Figure out how to make the types of the parameters enough to figure out which version to use and not require specifying when accessing vector
template<typename T, uint_fast16_t X>
vector<int32_t, 256> vector<T, X>::Blend(const vector<int32_t, 256> comparision, const vector<int32_t, 256> falseValue, const vector<int32_t, 256> trueValue)
{
	return _mm256_blendv_epi8(falseValue.vector_256_, trueValue.vector_256_, comparision.vector_256_);
}

// Assignment Operators
template<typename T, uint_fast16_t X>
vector<T, X>& vector<T, X>::operator+=(const vector<int32_t, 256>& rhs)
{
	this->vector_256_ = _mm256_add_epi32(this->vector_256_, rhs.vector_256_);
	return *this;
}

// Comparision Operators
//TODO Fix this not working allow implicit casts when more than one version exist
template<typename T, uint_fast16_t X>
vector<int32_t, 256> vector<T, X>::operator>(const vector<int32_t, 256>& vector2) const
{
	return _mm256_cmpgt_epi32(this->vector_256_, vector2.vector_256_);
}
#pragma endregion
