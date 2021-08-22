//TODO Merge string info arrays into a single structure
//TODO Automate generation of typeVectorConstructorArguments
#include <sstream>
#include <string>
#include "Vector.h"

struct VectorHelpers
{
	//Vector ToString Functions
	//TODO Check if these can be merged, doubt it though given use of vector_{128, 256}_, only works if GetInternal{128, 256} can be merged as well
	template <typename T>
	static std::string ToString128i(const Vector<T, 128> vector)
	{
		std::stringstream sstr;
		T values[16 / sizeof(T)];
		std::memcpy(values, &vector.vector_128_, sizeof(values));

		for (T v : values)
		{
			sstr << v << " ";
		}

		return sstr.str();
	}

	template <typename T>
	static std::string ToString256i(const Vector<T, 256> vector) 
	{
		std::stringstream sstr;
		T values[32 / sizeof(T)];
		std::memcpy(values, &vector.vector_256_, sizeof(values));

		for (T v : values)
		{
			sstr << v << " ";
		}

		return sstr.str();
	}

	//TODO Generate automatically?
	template <typename T>
	static __m128i GetInternal128i(const Vector<T, 128> vector)
	{
		return vector.vector_128_;
	}

	template <typename T>
	static __m256i GetInternal256i(const Vector<T, 256> vector)
	{
		return vector.vector_256_;
	}

	template <typename T>
	static Vector<T, 128> GetVector128i(const __m128i vector)
	{
		return vector;
	}

	template <typename T>
	static Vector<T, 256> GetVector256i(const __m256i vector)
	{
		return vector;
	}
};

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

//TODO Fix for floating point types
std::ostream& operator<<(std::ostream& stream, const Vector<int32_t, 128>& vector)
{
	return stream << VectorHelpers::ToString128i(vector);
}

//TODO Fix for floating point types
Vector<int32_t, 128> Blend(const Vector<int32_t, 128> comparision, const Vector<int32_t, 128> falseValue, const Vector<int32_t, 128> trueValue)
{
	return VectorHelpers::GetVector128i<int32_t>(_mm_blendv_epi8(VectorHelpers::GetInternal128i(falseValue), VectorHelpers::GetInternal128i(trueValue), VectorHelpers::GetInternal128i(comparision)));
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

//TODO Fix for floating point types
std::ostream& operator<<(std::ostream& stream, const Vector<int32_t, 256>& vector)
{
	return stream << VectorHelpers::ToString256i(vector);
}

//TODO Fix for floating point types
Vector<int32_t, 256> Blend(const Vector<int32_t, 256> comparision, const Vector<int32_t, 256> falseValue, const Vector<int32_t, 256> trueValue)
{
	return VectorHelpers::GetVector256i<int32_t>(_mm256_blendv_epi8(VectorHelpers::GetInternal256i(falseValue), VectorHelpers::GetInternal256i(trueValue), VectorHelpers::GetInternal256i(comparision)));
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

