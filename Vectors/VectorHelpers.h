//TODO Merge into Vector.h

#ifndef VECTOR_HELPERS_H
#define VECTOR_HELPERS_H
#include <ostream>
#include "Vector.h"

// String/Stream Operators
//TODO Fix when adding non 128/256i types
std::ostream& operator<<(std::ostream& stream, const Vector<int32_t, 128>& vector);

std::ostream& operator<<(std::ostream& stream, const Vector<int32_t, 256>& vector);

#endif //VECTOR_HELPERS_H
