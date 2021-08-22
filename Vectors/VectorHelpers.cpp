//TODO Merge into Vector.cpp

#include <string>
#include <sstream>
#include "VectorHelpers.h"

class VectorHelpers
{
public:
	//Vector ToString Functions
	//TODO Check if these can be merged
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

// Vector String/Stream Operators
//TODO Fix when adding non 128/256i types
std::ostream& operator<<(std::ostream& stream, const Vector<int32_t, 128>& vector)
{
	return stream << VectorHelpers::ToString128i(vector);
}

std::ostream& operator<<(std::ostream& stream, const Vector<int32_t, 256>& vector)
{
	return stream << VectorHelpers::ToString256i(vector);
}

// Vector Blend Functions
Vector<int32_t, 128> Blend(const Vector<int32_t, 128> comparision, const Vector<int32_t, 128> falseValue, const Vector<int32_t, 128> trueValue)
{
	return VectorHelpers::GetVector128i<int32_t>(_mm_blendv_epi8(VectorHelpers::GetInternal128i(falseValue), VectorHelpers::GetInternal128i(trueValue), VectorHelpers::GetInternal128i(comparision)));
}

Vector<int32_t, 256> Blend(const Vector<int32_t, 256> comparision, const Vector<int32_t, 256> falseValue, const Vector<int32_t, 256> trueValue)
{
	return VectorHelpers::GetVector256i<int32_t>(_mm256_blendv_epi8(VectorHelpers::GetInternal256i(falseValue), VectorHelpers::GetInternal256i(trueValue), VectorHelpers::GetInternal256i(comparision)));
}