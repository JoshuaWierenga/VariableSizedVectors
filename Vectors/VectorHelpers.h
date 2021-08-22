//TODO Merge into Vector.h

#ifndef VECTOR_HELPERS_H
#define VECTOR_HELPERS_H
#include <ostream>
#include "Vector.h"

// Vector String/Stream Operators
//TODO Fix when adding non 128/256i types
std::ostream& operator<<(std::ostream& stream, const Vector<int32_t, 128>& vector);

std::ostream& operator<<(std::ostream& stream, const Vector<int32_t, 256>& vector);

// Vector Blend Functions
Vector<int32_t, 128> Blend(Vector<int32_t, 128> comparision, Vector<int32_t, 128> falseValue, Vector<int32_t, 128> trueValue);

Vector<int32_t, 256> Blend(Vector<int32_t, 256> comparision, Vector<int32_t, 256> falseValue, Vector<int32_t, 256> trueValue);



#endif //VECTOR_HELPERS_H
