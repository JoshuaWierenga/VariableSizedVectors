
## Arbitrary Sized Performant Vectors in C#
This project is an attempt at adding arbitrary sized vectors to C# with x86 vector extensions used to improve computation speed whenever possible.
Currently 64, 128, 192 and 256 bit double vectors use Sse2 and Avx with a software fallback included for when both are not supported. Arbitrary sized double vectors are partially supported but a software fallback is always used. 

### Todo
* Use Sse2 and Avx for arbitray sized vectors.
* Support types other than double.
* If possible support arm vector exensions as well.
