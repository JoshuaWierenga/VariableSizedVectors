#include <iostream>
//TODO Add Vector.h, does that also need a tt file?
#include "Vector.cpp"

void branches1()
{
	constexpr int array[4] = { 4, 7, -2, 9 };
	constexpr int value = 5, adjustment1 = 10, adjustment2 = 3;

	auto vArray = vector<int, 128>(array[0], array[1], array[2], array[3]);

	vArray += vector<int, 128>::Blend(vArray > value, adjustment2, adjustment1);

	std::cout << "branches1 vector:" << std::endl << vArray << std::endl << std::endl;
}

/*void branches2()
{
	constexpr int array[8] = { 4, -2, 9, 7, 3, 2, 4, 6 };
	constexpr int value = 5, adjustment = 3;

	auto vArray = vectorold<int, 256>(array[0], array[1], array[2], array[3], array[4], array[5], array[6], array[7]);
	

	const auto vComparision = vArray > value;
	//const __m256i vResult = _mm256_blendv_epi8(vAdjustment1, vAdjustment2, vComparision);
	//vArray = _mm256_add_epi32(vArray, vResult);

	//printf("branches2 scalar:\n%d %d %d %d %d %d %d %d\n", array[0], array[1], array[2], array[3], array[4], array[5], array[6], array[7]);
	//printf("branches2 vector:\n%d %d %d %d %d %d %d %d\n\n", vArray.m256i_i32[0], vArray.m256i_i32[1], vArray.m256i_i32[2], vArray.m256i_i32[3], vArray.m256i_i32[4], vArray.m256i_i32[5], vArray.m256i_i32[6], vArray.m256i_i32[7]);
}*/

int main()
{
	branches1();
	//branches2();
}
