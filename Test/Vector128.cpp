#include <emmintrin.h>
#include <cstdint>
#include <type_traits>

template<typename T>
struct Vector128
{
private:
	__m128i vector_;

	Vector128(const __m128i  vector) : vector_(vector)
	{
	}

public:
	// Constructors
	explicit Vector128(const int32_t value) requires(std::is_same_v<T, int32_t>) : vector_(_mm_set1_epi32(value))
	{
	}

	explicit Vector128(const int64_t value) requires(std::is_same_v<T, int64_t>) : vector_(_mm_set1_epi64x(value))
	{
	}



	// Arithmetic Operators
	Vector128 operator+(const Vector128& vector2) const requires(std::is_same_v<T, int32_t>)
	{
		return _mm_add_epi32(this->vector_, vector2.vector_);
	}

	Vector128 operator+(const Vector128& vector2) const requires(std::is_same_v<T, int64_t>)
	{
		return _mm_add_epi64(this->vector_, vector2.vector_);
	}
};

