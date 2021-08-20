//TODO Merge string info arrays into a single structure
#include <intrin.h>

template <uint_fast16_t X>
_INLINE_VAR constexpr bool is_simd_128 = X == 128;

template<typename T, uint_fast16_t X>
struct vector
{
private:
	__m128i vector_128_{};

	//TODO Generate automatically?
	vector(const __m128i vector) : vector_128_(vector)
	{
	}

public:
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
	vector<int32_t, 128> operator>(const vector<int32_t, 128>& vector2) const
	{
		return _mm_cmpgt_epi32(this->vector_128_, vector2.vector_128_);
	}
	#pragma endregion
};

//TODO Fix when adding non 128i types
// String/Stream Operators
inline std::ostream& operator<<(std::ostream& stream, const vector<int32_t, 128>& vector)
{
	return stream << vector.ToString128i();
}
