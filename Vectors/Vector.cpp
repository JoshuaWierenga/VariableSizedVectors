//TODO Merge string info arrays into a single structure
#include <intrin.h>
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

	//TODO Generate automatically?
	vector(const __m128i vector) : vector_128_(vector)
	{
	}

	vector(const __m256i vector) : vector_256_(vector)
	{
	}

public:
	//TODO Check if these can be merged
	//ToString Functions
	[[nodiscard]] std::string ToString128i() const
	{
		std::stringstream sstr;
		T values[16 / sizeof(T)];
		std::memcpy(values, &this->vector_128_, sizeof(values));

		for (T v : values)
		{
			sstr << v << " ";
		}

		return sstr.str();
	}

	[[nodiscard]] std::string ToString256i() const
	{
		std::stringstream sstr;
		T values[32 / sizeof(T)];
		std::memcpy(values, &this->vector_256_, sizeof(values));

		for (T v : values)
		{
			sstr << v << " ";
		}

		return sstr.str();
	}

	#pragma region vector<int32_t, 128>

	// Constructors
	vector(const int32_t value) requires(std::is_same_v<T, int32_t> && is_simd_128<X>) : vector_128_(_mm_set1_epi32(value))
	{
	}

	explicit vector(const int32_t v1, const int32_t v2, const int32_t v3, const int32_t v4) requires(std::is_same_v<T, int32_t> && is_simd_128<X>) : vector_128_(_mm_setr_epi32(v1, v2, v3, v4))
	{
	}

	//TODO Figure out how to make the types of the parameters enough to figure out which version to use and not require specifying when accessing vector
	vector<int32_t, 128> static Blend(const vector<int32_t, 128> comparision, const vector<int32_t, 128> falseValue, const vector<int32_t, 128> trueValue)
	{
		return _mm_blendv_epi8(falseValue.vector_128_, trueValue.vector_128_, comparision.vector_128_);
	}

	// Assignment Operators
	vector& operator+=(const vector<int32_t, 128>& rhs)
	{
		this->vector_128_ = _mm_add_epi32(this->vector_128_, rhs.vector_128_);
		return *this;
	}

	// Comparision Operators
	//TODO Fix this not working allow implicit casts when more than one version exist
	vector<int32_t, 128> operator>(const vector<int32_t, 128>& vector2) const
	{
		return _mm_cmpgt_epi32(this->vector_128_, vector2.vector_128_);
	}
	#pragma endregion
	#pragma region vector<int32_t, 256>

	// Constructors
	vector(const int32_t value) requires(std::is_same_v<T, int32_t> && is_simd_256<X>) : vector_256_(_mm256_set1_epi32(value))
	{
	}

	explicit vector(const int32_t v1, const int32_t v2, const int32_t v3, const int32_t v4, const int32_t v5, const int32_t v6, const int32_t v7, const int32_t v8) requires(std::is_same_v<T, int32_t> && is_simd_256<X>) : vector_256_(_mm256_setr_epi32(v1, v2, v3, v4, v6, v6, v7, v8))
	{
	}

	//TODO Figure out how to make the types of the parameters enough to figure out which version to use and not require specifying when accessing vector
	vector<int32_t, 256> static Blend(const vector<int32_t, 256> comparision, const vector<int32_t, 256> falseValue, const vector<int32_t, 256> trueValue)
	{
		return _mm256_blendv_epi8(falseValue.vector_256_, trueValue.vector_256_, comparision.vector_256_);
	}

	// Assignment Operators
	vector& operator+=(const vector<int32_t, 256>& rhs)
	{
		this->vector_256_ = _mm256_add_epi32(this->vector_256_, rhs.vector_256_);
		return *this;
	}

	// Comparision Operators
	//TODO Fix this not working allow implicit casts when more than one version exist
	vector<int32_t, 256> operator>(const vector<int32_t, 256>& vector2) const
	{
		return _mm256_cmpgt_epi32(this->vector_256_, vector2.vector_256_);
	}
	#pragma endregion
};

//TODO Fix when adding non 128i types
// String/Stream Operators
inline std::ostream& operator<<(std::ostream& stream, const vector<int32_t, 128>& vector)
{
	return stream << vector.ToString128i();
}
inline std::ostream& operator<<(std::ostream& stream, const vector<int32_t, 256>& vector)
{
	return stream << vector.ToString256i();
}
