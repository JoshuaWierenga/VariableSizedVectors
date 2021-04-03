using System;
using Vectors;

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
            Vector<double> first = new(6);
            Vector<double> second = new(2);

            Console.WriteLine($"{first} + {second} = {first + second}");
            Console.WriteLine($"{first} - {second} = {first - second}");
            Console.WriteLine($"{first} * {second} = {first * second}");
            Console.WriteLine($"{first} * {2} = {first * 2}");
            Console.WriteLine($"{6} * {second} = {6 * second}");
            Console.WriteLine($"{first} / {second} = {first / second}");
            //Console.WriteLine($"-{first} = {-first}");
            Console.WriteLine($"{first} == {second} = {first == second}");
            Console.WriteLine($"{first} == {first} = {first == first}");
            Console.WriteLine($"{first} != {second} = {first != second}");

            Console.WriteLine();

            first = new Vector<double>(new[] { (double)6, 9 });
            second = new Vector<double>(new[] { (double)2, 3 });

            Console.WriteLine($"{first} + {second} = {first + second}");
            Console.WriteLine($"{first} - {second} = {first - second}");
            Console.WriteLine($"{first} * {second} = {first * second}");
            Console.WriteLine($"{first} * {2} = {first * 2}");
            Console.WriteLine($"{6} * {second} = {6 * second}");
            Console.WriteLine($"{first} / {second} = {first / second}");
            //Console.WriteLine($"-{first} = {-first}");
            Console.WriteLine($"{first} == {second} = {first == second}");
            Console.WriteLine($"{first} == {first} = {first == first}");
            Console.WriteLine($"{first} != {second} = {first != second}");

            Console.WriteLine();

            first = new Vector<double>(new[] { (double)6, 9, 20 });
            second = new Vector<double>(new[] { (double)2, 3, 10 });

            Console.WriteLine($"{first} + {second} = {first + second}");
            Console.WriteLine($"{first} - {second} = {first - second}");
            Console.WriteLine($"{first} * {second} = {first * second}");
            Console.WriteLine($"{first} * {2} = {first * 2}");
            Console.WriteLine($"{6} * {second} = {6 * second}");
            Console.WriteLine($"{first} / {second} = {first / second}");
            //Console.WriteLine($"-{first} = {-first}");
            Console.WriteLine($"{first} == {second} = {first == second}");
            Console.WriteLine($"{first} == {first} = {first == first}");
            Console.WriteLine($"{first} != {second} = {first != second}");

            Console.WriteLine();

            first = new Vector<double>(new[] { (double)6, 9, 20, 100 });
            second = new Vector<double>(new[] { (double)2, 3, 10, 50 });

            Console.WriteLine($"{first} + {second} = {first + second}");
            Console.WriteLine($"{first} - {second} = {first - second}");
            Console.WriteLine($"{first} * {second} = {first * second}");
            Console.WriteLine($"{first} * {2} = {first * 2}");
            Console.WriteLine($"{6} * {second} = {6 * second}");
            Console.WriteLine($"{first} / {second} = {first / second}");
            //Console.WriteLine($"-{first} = {-first}");
            Console.WriteLine($"{first} == {second} = {first == second}");
            Console.WriteLine($"{first} == {first} = {first == first}");
            Console.WriteLine($"{first} != {second} = {first != second}");

            Console.WriteLine();

            Vector<long> firstInt = new(new[] { (long)6, 9, 20, 100 });
            Vector<long> secondInt = new(new[] { (long)2, 3, 10, 50 });

            Console.WriteLine($"{firstInt} + {secondInt} = {firstInt + secondInt}");
            Console.WriteLine($"{firstInt} - {secondInt} = {firstInt - secondInt}");
            Console.WriteLine($"{firstInt} * {secondInt} = {firstInt * secondInt}");
            Console.WriteLine($"{firstInt} * {2} = {firstInt * 2}");
            Console.WriteLine($"{6} * {secondInt} = {6 * secondInt}");
            Console.WriteLine($"{firstInt} / {secondInt} = {firstInt / secondInt}");
            //Console.WriteLine($"-{firstInt} = {-firstInt}");
            Console.WriteLine($"{firstInt} == {secondInt} = {firstInt == secondInt}");
            Console.WriteLine($"{firstInt} == {firstInt} = {firstInt == firstInt}");
            Console.WriteLine($"{firstInt} != {secondInt} = {firstInt != secondInt}");

            first = new Vector<double>(new[] { (double)6, 9, 20, 100, 36, 88, 11 });
            second = new Vector<double>(new[] { (double)2, 3, 10, 50, 8, 45, 11 });

            Console.WriteLine();

            Console.WriteLine($"{first} + {second} = {first + second}");
            Console.WriteLine($"{first} - {second} = {first - second}");
            Console.WriteLine($"{first} * {second} = {first * second}");
            Console.WriteLine($"{first} * {2} = {first * 2}");
            Console.WriteLine($"{6} * {second} = {6 * second}");
            Console.WriteLine($"{first} / {second} = {first / second}");
            //Console.WriteLine($"-{first} = {-first}");
            Console.WriteLine($"{first} == {second} = {first == second}");
            Console.WriteLine($"{first} == {first} = {first == first}");
            Console.WriteLine($"{first} != {second} = {first != second}");
        }
    }
}
