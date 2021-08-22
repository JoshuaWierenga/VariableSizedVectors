//TODO Merge into Vector.cpp

#include <string>
#include <sstream>
#include "VectorHelpers.h"

class VectorHelpers
{
public:
	//Vector ToString Functions
	//TODO Check if these can be merged
	template<typename T>
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

	template<typename T>
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
};

// String/Stream Operators
//TODO Fix when adding non 128/256i types
std::ostream& operator<<(std::ostream& stream, const Vector<int32_t, 128>& vector)
{
	return stream << VectorHelpers::ToString128i(vector);
}

std::ostream& operator<<(std::ostream& stream, const Vector<int32_t, 256>& vector)
{
	return stream << VectorHelpers::ToString256i(vector);
}