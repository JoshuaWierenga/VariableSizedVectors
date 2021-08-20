#include <cstdint>
#include <intrin.h>
#include <type_traits>
#include <ostream>
#include <sstream>

template <uint_fast16_t X>
_INLINE_VAR constexpr bool is_simd_128 = X == 128;

template <uint_fast16_t X>
_INLINE_VAR constexpr bool is_simd_256 = X == 256;

template<typename T, uint_fast16_t X>
struct vector
{
private:
	__m128i vector_128_{};
	__m256i vector_256_{};

	vector(const __m128i vector) : vector_128_(vector)
	{
	}

public:
#pragma region vector<int32_t, 128>
	// Constructors
	vector(const int32_t value) requires(std::is_same_v<T, int32_t> && is_simd_128<X>) : vector_128_(_mm_set1_epi32(value))
	{
	}

	explicit vector(const int32_t v1, const int32_t v2, const int32_t v3, const int32_t v4) requires(std::is_same_v<T, int32_t>&& is_simd_128<X>) : vector_128_(_mm_setr_epi32(v1, v2, v3, v4))
	{
	}

	// From https://stackoverflow.com/a/26012188
	[[nodiscard]] std::string ToString() const
	{
		std::stringstream sstr;
		T values[16 / sizeof(T)];
		std::memcpy(values, &this->vector_128_, sizeof(values));
		if (sizeof(T) == 1) {
			for (unsigned int i = 0; i < sizeof(__m128i); i++) { //C++11: Range for also possible
				sstr << static_cast<int>(values[i]) << " ";
			}
		}
		else {
			for (unsigned int i = 0; i < sizeof(__m128i) / sizeof(T); i++) { //C++11: Range for also possible
				sstr << values[i] << " ";
			}
		}
		return sstr.str();
	}

	// Assignment Operators
	vector& operator=(const vector<int32_t, 128>& rhs)
	{
		this->vector_128_ = rhs.vector_128_;
		return *this;
	}

	vector& operator+=(const vector<int32_t, 128>& rhs)
	{
		this->vector_128_ = _mm_add_epi32(this->vector_128_, rhs.vector_128_);
		return *this;
	}

	// Arithmetic Operators
	vector<int32_t, 128> operator+(const vector<int32_t, 128>& vector2) const
	{
		return _mm_add_epi32(this->vector_128_, vector2.vector_128_);
	}

	// Comparision Operators
	vector<int32_t, 128> operator>(const vector<int32_t, 128>& vector2) const
	{
		return _mm_cmpgt_epi32(this->vector_128_, vector2.vector_128_);
	}

	// Blend Functions
	vector<int32_t, 128> static Blend(const vector<int32_t, 128> comparision, const vector<int32_t, 128> falseValue, const vector<int32_t, 128> trueValue)
	{
		return _mm_blendv_epi8(falseValue.vector_128_, trueValue.vector_128_, comparision.vector_128_);
	}
#pragma endregion

/*#pragma region vector<int32_t, 256>
	// Constructors
	vector(const int32_t value) requires(std::is_same_v<T, int32_t>&& is_simd_256<X>) : vector_256_(_mm256_set1_epi32(value))
	{
	}

	explicit vector(const int32_t v1, const int32_t v2, const int32_t v3, const int32_t v4, const int32_t v5, const int32_t v6, const int32_t v7, const int32_t v8) requires(std::is_same_v<T, int32_t>&& is_simd_256<X>) : vector_128_(_mm_setr_epi64(v1, v2, v3, v4, v6, v6, v7, v8))
	{
	}

	// From https://stackoverflow.com/a/26012188
	/*[[nodiscard]] std::string ToString() const
	{
		std::stringstream sstr;
		T values[16 / sizeof(T)];
		std::memcpy(values, &this->vector_128_, sizeof(values));
		if (sizeof(T) == 1) {
			for (unsigned int i = 0; i < sizeof(__m128i); i++) { //C++11: Range for also possible
				sstr << static_cast<int>(values[i]) << " ";
			}
		}
		else {
			for (unsigned int i = 0; i < sizeof(__m128i) / sizeof(T); i++) { //C++11: Range for also possible
				sstr << values[i] << " ";
			}
		}
		return sstr.str();
	}*//*

	// Assignment Operators
	vector& operator=(const vector<int32_t, 256>& rhs)
	{
		this->vector_256_ = rhs.vector_256_;
		return *this;
	}

	vector& operator+=(const vector<int32_t, 256>& rhs)
	{
		this->vector_256_ = _mm256_add_epi32(this->vector_256_, rhs.vector_256_);
		return *this;
	}

	// Arithmetic Operators
	vector<int32_t, 256> operator+(const vector<int32_t, 256>& vector2) const
	{
		return _mm256_add_epi32(this->vector_256_, vector2.vector_256_);
	}

	// Comparision Operators
	vector<int32_t, 256> operator>(const vector<int32_t, 256>& vector2) const requires(is_simd_256<X>)
	{
		return _mm_cmpgt_epi32(this->vector_256_, vector2.vector_256_);
	}

	// Blend Functions
	vector<int32_t, 256> static Blend(const vector<int32_t, 256> comparision, const vector<int, 256> falseValue, const vector<int, 256> trueValue)
	{
		return _mm256_blendv_epi8(falseValue.vector_256_, trueValue.vector_256_, comparision.vector_256_);
	}
#pragma endregion*/
};

// String/Stream Operators
inline std::ostream& operator<<(std::ostream& Str, const vector<int32_t, 128>& vector)
{
	return Str << vector.ToString();
}

/*
	const union vector_128
	{
		__m128i i;
		//__m128 s;
	} vector_128_{};

	//const __m256i vector_256_{};

	vector(const __m128i vector) : vector_128_(vector)
	{
	}

	/*vector(const __m256i vector) : vector_256_(vector)
	{
	}
	
	explicit vector(const int64_t value) requires(std::is_same_v<T, int64_t>&& is_simd_128<X>) : vector_128_{ .i = _mm_set1_epi64x(value) }
	{
	}

	explicit vector(const float value) requires(std::is_same_v<T, float>&& is_simd_128<X>) : vector_128_{ .s = _mm_set1_ps(value) }
	{
	}

	explicit vector(const int32_t value) requires(std::is_same_v<T, int32_t>&& is_simd_256<X>) : vector_256_(_mm256_set1_epi32(value))
	{
	}

	explicit vector(const int64_t value) requires(std::is_same_v<T, int64_t>&& is_simd_256<X>) : vector_256_(_mm256_set1_epi64x(value))
	{
	}

	vector<int64_t, 256> operator+(const vector<int64_t, 256>& vector2) const
	{
		return _mm256_add_epi64(this->vector_256_, vector2.vector_256_);
	}
 */
