//TODO Merge string info arrays into a single structure
//TODO Automate generation of typeVectorConstructorArguments
#ifndef VECTOR_H
#define VECTOR_H
#include <intrin.h>
#include <sstream>

//TODO Check if these are needed in the header, same for the constraints in general										
template <uint_fast16_t X>
_INLINE_VAR constexpr bool is_simd_128 = X == 128;

template <uint_fast16_t X>
_INLINE_VAR constexpr bool is_simd_256 = X == 256;

template<typename T, uint_fast16_t X>
struct vector
{
	friend class VectorHelpers;

private:
	__m128i vector_128_{};
	__m256i vector_256_{};

	//TODO Generate automatically?
	vector(__m128i vector);

	vector(__m256i vector);

public:
	#pragma region vector<int32_t, 128>

	// Constructors
	vector(int32_t value) requires(std::is_same_v<T, int32_t> && is_simd_128<X>);

	explicit vector(int32_t v1, int32_t v2, int32_t v3, int32_t v4) requires(std::is_same_v<T, int32_t> && is_simd_128<X>);

	//TODO Figure out how to make the types of the parameters enough to figure out which version to use and not require specifying when accessing vector
	vector<int32_t, 128> static Blend(vector<int32_t, 128> comparision, vector<int32_t, 128> falseValue, vector<int32_t, 128> trueValue);

	// Assignment Operators
	vector& operator+=(const vector<int32_t, 128>& rhs);

	// Comparision Operators
	//TODO Fix this not working allow implicit casts when more than one version exist
	vector<int32_t, 128> operator>(const vector<int32_t, 128>& vector2) const;
	
	#pragma endregion
	#pragma region vector<int32_t, 256>

	// Constructors
	vector(int32_t value) requires(std::is_same_v<T, int32_t> && is_simd_256<X>);

	explicit vector(int32_t v1, int32_t v2, int32_t v3, int32_t v4, int32_t v5, int32_t v6, int32_t v7, int32_t v8) requires(std::is_same_v<T, int32_t> && is_simd_256<X>);

	//TODO Figure out how to make the types of the parameters enough to figure out which version to use and not require specifying when accessing vector
	vector<int32_t, 256> static Blend(vector<int32_t, 256> comparision, vector<int32_t, 256> falseValue, vector<int32_t, 256> trueValue);

	// Assignment Operators
	vector& operator+=(const vector<int32_t, 256>& rhs);

	// Comparision Operators
	//TODO Fix this not working allow implicit casts when more than one version exist
	vector<int32_t, 256> operator>(const vector<int32_t, 256>& vector2) const;
	
	#pragma endregion
};

#ifndef _DEVELOPMENT
#include "Vector.cpp"
#endif
#endif //VECTOR_H