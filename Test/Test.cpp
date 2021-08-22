#include <iostream>
#include "Vector.h"

void simple128test()
{
	constexpr int array[4] = { 4, 7, -2, 9 };
	constexpr int value = 5, adjustment1 = 10, adjustment2 = 3;

	Vector<int32_t, 128> vArray(array[0], array[1], array[2], array[3]);

	vArray += Blend(vArray > value, adjustment2, adjustment1);

	std::cout << "simple 128 bit test:" << std::endl << vArray << std::endl << std::endl;
}

void simple256test()
{
	constexpr int array[8] = { 4, -2, 9, 7, 3, 2, 4, 6 };
	constexpr int value = 5, adjustment = 3;

	Vector<int, 256> vArray(array[0], array[1], array[2], array[3], array[4], array[5], array[6], array[7]);

	vArray += Blend(vArray > value, adjustment, 0);

	std::cout << "simple 256 bit test" << std::endl << vArray << std::endl << std::endl;
}

int main()
{
	simple128test();
	simple256test();
}
