using Vectors;
using System;

namespace VectorsTest
{
    class Program
    {
        static void Main(string[] args)
        {
            TestVectorDouble();
        }

        static void TestVectorDouble()
        {
            VectorDouble first = new(6);
            VectorDouble second = new(2);

            Console.WriteLine($"{first} + {second} = {first + second}");
            Console.WriteLine($"{first} - {second} = {first - second}");
            Console.WriteLine($"{first} * {second} = {first * second}");
            Console.WriteLine($"{first} * {2} = {first * 2}");
            Console.WriteLine($"{6} * {second} = {6 * second}");
            Console.WriteLine($"{first} / {second} = {first / second}");
            Console.WriteLine($"-{first} = {-first}");
            Console.WriteLine($"{first} == {second} = {first == second}");
            Console.WriteLine($"{first} == {first} = {first == first}");
            Console.WriteLine($"{first} != {second} = {first != second}");

            Console.WriteLine();

            first = new VectorDouble(new[] { (double)6, 9 });
            second = new VectorDouble(new[] { (double)2, 3 });

            Console.WriteLine($"{first} + {second} = {first + second}");
            Console.WriteLine($"{first} - {second} = {first - second}");
            Console.WriteLine($"{first} * {second} = {first * second}");
            Console.WriteLine($"{first} * {2} = {first * 2}");
            Console.WriteLine($"{6} * {second} = {6 * second}");
            Console.WriteLine($"{first} / {second} = {first / second}");
            Console.WriteLine($"-{first} = {-first}");
            Console.WriteLine($"{first} == {second} = {first == second}");
            Console.WriteLine($"{first} == {first} = {first == first}");
            Console.WriteLine($"{first} != {second} = {first != second}");

            Console.WriteLine();

            first = new VectorDouble(new[] { (double)6, 9, 20 });
            second = new VectorDouble(new[] { (double)2, 3, 10 });

            Console.WriteLine($"{first} + {second} = {first + second}");
            Console.WriteLine($"{first} - {second} = {first - second}");
            Console.WriteLine($"{first} * {second} = {first * second}");
            Console.WriteLine($"{first} * {2} = {first * 2}");
            Console.WriteLine($"{6} * {second} = {6 * second}");
            Console.WriteLine($"{first} / {second} = {first / second}");
            Console.WriteLine($"-{first} = {-first}");
            Console.WriteLine($"{first} == {second} = {first == second}");
            Console.WriteLine($"{first} == {first} = {first == first}");
            Console.WriteLine($"{first} != {second} = {first != second}");

            Console.WriteLine();

            first = new VectorDouble(new[] { (double)6, 9, 20, 100 });
            second = new VectorDouble(new[] { (double)2, 3, 10, 50 });

            Console.WriteLine($"{first} + {second} = {first + second}");
            Console.WriteLine($"{first} - {second} = {first - second}");
            Console.WriteLine($"{first} * {second} = {first * second}");
            Console.WriteLine($"{first} * {2} = {first * 2}");
            Console.WriteLine($"{6} * {second} = {6 * second}");
            Console.WriteLine($"{first} / {second} = {first / second}");
            Console.WriteLine($"-{first} = {-first}");
            Console.WriteLine($"{first} == {second} = {first == second}");
            Console.WriteLine($"{first} == {first} = {first == first}");
            Console.WriteLine($"{first} != {second} = {first != second}");

            first = new VectorDouble(new[] { (double)6, 9, 20, 100, 36 });
            second = new VectorDouble(new[] { (double)2, 3, 10, 50, 8 });

            Console.WriteLine();

            Console.WriteLine($"{first} + {second} = {first + second}");
            Console.WriteLine($"{first} - {second} = {first - second}");
            Console.WriteLine($"{first} * {second} = {first * second}");
            Console.WriteLine($"{first} * {2} = {first * 2}");
            Console.WriteLine($"{6} * {second} = {6 * second}");
            Console.WriteLine($"{first} / {second} = {first / second}");
            Console.WriteLine($"-{first} = {-first}");
            Console.WriteLine($"{first} == {second} = {first == second}");
            Console.WriteLine($"{first} == {first} = {first == first}");
            Console.WriteLine($"{first} != {second} = {first != second}");
        }
    }
}
