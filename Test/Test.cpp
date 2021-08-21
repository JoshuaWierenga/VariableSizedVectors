#include <iostream>
#include "Vector.h"
#include "VectorHelpers.h"

void simple128test()
{
	constexpr int array[4] = { 4, 7, -2, 9 };
	constexpr int value = 5, adjustment1 = 10, adjustment2 = 3;

	auto vArray = vector<int, 128>(array[0], array[1], array[2], array[3]);

	vArray += vector<int, 128>::Blend(vArray > static_cast<vector<int, 128>>(value), adjustment2, adjustment1);

	std::cout << "simple 128 bit test:" << std::endl << vArray << std::endl << std::endl;
}

void simple256test()
{
	constexpr int array[8] = { 4, -2, 9, 7, 3, 2, 4, 6 };
	constexpr int value = 5, adjustment = 3;

	auto vArray = vector<int, 256>(array[0], array[1], array[2], array[3], array[4], array[5], array[6], array[7]);

	vArray += vector<int, 256>::Blend(vArray > static_cast<vector<int, 256>>(value), adjustment, 0);

	std::cout << "simple 256 bit test" << std::endl << vArray << std::endl << std::endl;
}

int main()
{
	simple128test();
	simple256test();
}
