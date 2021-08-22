//TODO Merge string info arrays into a single structure
//TODO Automate generation of typeVectorConstructorArguments
#include "Vector.h"

#pragma region Vector<int32_t, 128>

// Constructors
template <>
Vector<int32_t, 128>::Vector(const int32_t value) : vector_128_(_mm_set1_epi32(value))
{
}

template <>
Vector<int32_t, 128>::Vector(const std::array<int32_t, 4> values) : vector_128_(_mm_setr_epi32(values[0], values[1], values[2], values[3]))
{
}

// Assignment Operators
template <>
Vector<int32_t, 128>& Vector<int32_t, 128>::operator+=(const Vector<int32_t, 128>& rhs)
{
	this->vector_128_ = _mm_add_epi32(this->vector_128_, rhs.vector_128_);
	return *this;
}

// Comparision Operators
template <>
Vector<int32_t, 128> Vector<int32_t, 128>::operator>(const Vector<int32_t, 128>& vector2) const
{
	return _mm_cmpgt_epi32(this->vector_128_, vector2.vector_128_);
}
#pragma endregion

#pragma region Vector<int32_t, 256>

// Constructors
template <>
Vector<int32_t, 256>::Vector(const int32_t value) : vector_256_(_mm256_set1_epi32(value))
{
}

template <>
Vector<int32_t, 256>::Vector(const std::array<int32_t, 8> values) : vector_256_(_mm256_setr_epi32(values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7]))
{
}

// Assignment Operators
template <>
Vector<int32_t, 256>& Vector<int32_t, 256>::operator+=(const Vector<int32_t, 256>& rhs)
{
	this->vector_256_ = _mm256_add_epi32(this->vector_256_, rhs.vector_256_);
	return *this;
}

// Comparision Operators
template <>
Vector<int32_t, 256> Vector<int32_t, 256>::operator>(const Vector<int32_t, 256>& vector2) const
{
	return _mm256_cmpgt_epi32(this->vector_256_, vector2.vector_256_);
}
#pragma endregion

