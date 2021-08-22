//TODO Merge string info arrays into a single structure
//TODO Automate generation of typeVectorConstructorArguments
#ifndef VECTOR_H
#define VECTOR_H
#include <array>
#include <cstdint>
#include <intrin.h>
#include <type_traits>

template <typename T, uint_fast16_t X>
struct Vector
{
	friend class VectorHelpers;

private:
	__m128i vector_128_{};
	__m256i vector_256_{};

	//TODO Generate automatically?
	Vector(__m128i vector) : vector_128_(vector)
	{
	}

	Vector(__m256i vector) : vector_256_(vector)
	{
	}

public:
	// Constructors
	Vector(T value);

	explicit Vector(std::array<T, X / (8 * sizeof(T))> values);

	template<class ...T2, std::enable_if_t<sizeof...(T2) == X / (8 * sizeof(T)) && std::conjunction_v<std::is_same<T, T2>...>, int> = 0>
	explicit Vector(T2... args) : Vector(std::array<T, sizeof...(T2)>{std::forward<T2>(args)...})
	{
	}

	Vector<T, X> static Blend(Vector<T, X> comparision, Vector<T, X> falseValue, Vector<T, X> trueValue);

	// Assignment Operators
	Vector<T, X>& operator+=(const Vector<T, X>& rhs);

	// Comparision Operators
	Vector<T, X> operator>(const Vector<T, X>& vector2) const;
};

//TODO Move into struct?
#pragma region Vector<int32_t, 128>
// Constructors
template <> Vector<int32_t, 128>::Vector(int32_t value);
template <> Vector<int32_t, 128>::Vector(std::array<int32_t, 4> values);

template <> Vector<int32_t, 128> Vector<int32_t, 128>::Blend(Vector<int32_t, 128> comparision, Vector<int32_t, 128> falseValue, Vector<int32_t, 128> trueValue);

// Assignment Operators
template <> Vector<int32_t, 128>& Vector<int32_t, 128>::operator+=(const Vector<int32_t, 128>& rhs);

// Comparision Operators
template <> Vector<int32_t, 128> Vector<int32_t, 128>::operator>(const Vector<int32_t, 128>& vector2) const;
#pragma endregion

#pragma region Vector<int32_t, 256>
// Constructors
template <> Vector<int32_t, 256>::Vector(int32_t value);
template <> Vector<int32_t, 256>::Vector(std::array<int32_t, 8> values);

template <> Vector<int32_t, 256> Vector<int32_t, 256>::Blend(Vector<int32_t, 256> comparision, Vector<int32_t, 256> falseValue, Vector<int32_t, 256> trueValue);

// Assignment Operators
template <> Vector<int32_t, 256>& Vector<int32_t, 256>::operator+=(const Vector<int32_t, 256>& rhs);

// Comparision Operators
template <> Vector<int32_t, 256> Vector<int32_t, 256>::operator>(const Vector<int32_t, 256>& vector2) const;
#pragma endregion

#endif //VECTOR_H