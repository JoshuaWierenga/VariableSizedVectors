#ifndef VECTOR_HELPERS_H
#define VECTOR_HELPERS_H
#include "Vector.h"

// String/Stream Operators
//TODO Fix when adding non 128i types
std::ostream& operator<<(std::ostream& stream, const vector<int32_t, 128>& vector);
std::ostream& operator<<(std::ostream& stream, const vector<int32_t, 256>& vector);

#endif //VECTOR_HELPERS_H
