
## Arbitrary Sized Performant Vectors in C#
This project is an attempt at adding arbitrary sized vectors to C# with x86 vector extensions used to improve computation speed whenever possible.
Sse2 and Avx are used if supported with software fallbacks included for when they are not. There are hardcoded, unrolled cases for 64, 128, 192 and 256 bit vectors for efficiency since Avx and Sse2 can deal with them directly.

### Todo
* Support types other than double.
* Potentially use smt for very large vectors.
* Extend support to matrices? If so then rename project. Does smt make more sense here for specific operations like matrix multiplication where only a single row/column of each matrix is needed at a time.
* If possible support arm vector exensions as well.
