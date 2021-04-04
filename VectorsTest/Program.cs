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

            Console.WriteLine("Byte single value vector");
            Vector<byte> firstByte = new(6);
            Vector<byte> secondByte = new(2);
            
            Console.WriteLine($"{firstByte} + {secondByte} = {firstByte + secondByte}");
            Console.WriteLine($"{firstByte} - {secondByte} = {firstByte - secondByte}");
            Console.WriteLine($"{firstByte} * {secondByte} = {firstByte * secondByte}");
            Console.WriteLine($"{firstByte} * {2} = {firstByte * 2}");
            Console.WriteLine($"{6} * {secondByte} = {6 * secondByte}");
            Console.WriteLine($"{firstByte} / {secondByte} = {firstByte / secondByte}");
            //Console.WriteLine($"-{firstByte} = {-firstByte}");
            Console.WriteLine($"{firstByte} == {secondByte} = {firstByte == secondByte}");
            Console.WriteLine($"{firstByte} == {firstByte} = {firstByte == firstByte}");
            Console.WriteLine($"{firstByte} != {secondByte} = {firstByte != secondByte}");

            Console.WriteLine();

            Console.WriteLine("Ulong double value vector");
            Vector<ulong> firstUlong = new(new ulong[] { 6, 9 });
            Vector<ulong> secondUlong = new(new ulong[] { 2, 3 });

            Console.WriteLine($"{firstUlong} + {secondUlong} = {firstUlong + secondUlong}");
            Console.WriteLine($"{firstUlong} - {secondUlong} = {firstUlong - secondUlong}");
            Console.WriteLine($"{firstUlong} * {secondUlong} = {firstUlong * secondUlong}");
            Console.WriteLine($"{firstUlong} * {2} = {firstUlong * 2}");
            Console.WriteLine($"{6} * {secondUlong} = {6 * secondUlong}");
            Console.WriteLine($"{firstUlong} / {secondUlong} = {firstUlong / secondUlong}");
            //Console.WriteLine($"-{firstUlong} = {-firstUlong}");
            Console.WriteLine($"{firstUlong} == {secondUlong} = {firstUlong == secondUlong}");
            Console.WriteLine($"{firstUlong} == {firstUlong} = {firstUlong == firstUlong}");
            Console.WriteLine($"{firstUlong} != {secondUlong} = {firstUlong != secondUlong}");

            Console.WriteLine();

            Console.WriteLine("Int triple value vector");
            Vector<int> firstInt = new(new[] { 6, 9, -20 });
            Vector<int> secondInt = new(new[] { 2, 3, 10 });

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

            Console.WriteLine();

            Console.WriteLine("UShort quad value vector");
            Vector<ushort> firstUShort = new(new ushort[] { 6, 9, 20, 100 });
            Vector<ushort> secondUShort = new(new ushort[] { 2, 3, 10, 50 });

            Console.WriteLine($"{firstUShort} + {secondUShort} = {firstUShort + secondUShort}");
            Console.WriteLine($"{firstUShort} - {secondUShort} = {firstUShort - secondUShort}");
            Console.WriteLine($"{firstUShort} * {secondUShort} = {firstUShort * secondUShort}");
            Console.WriteLine($"{firstUShort} * {2} = {firstUShort * 2}");
            Console.WriteLine($"{6} * {secondUShort} = {6 * secondUShort}");
            Console.WriteLine($"{firstUShort} / {secondUShort} = {firstUShort / secondUShort}");
            //Console.WriteLine($"-{firstUShort} = {-firstUShort}");
            Console.WriteLine($"{firstUShort} == {secondUShort} = {firstUShort == secondUShort}");
            Console.WriteLine($"{firstUShort} == {firstUShort} = {firstUShort == firstUShort}");
            Console.WriteLine($"{firstUShort} != {secondUShort} = {firstUShort != secondUShort}");

            Console.WriteLine();

            Console.WriteLine("Double 7 value vector");
            Vector<double> firstDouble = new(new double[] { 6, 9, 20, 100, 36, 88, 11 });
            Vector<double> secondDouble = new(new double[] { 2, 3, 10, 50, 8, 45, 11 });

            Console.WriteLine($"{firstDouble} + {secondDouble} = {firstDouble + secondDouble}");
            Console.WriteLine($"{firstDouble} - {secondDouble} = {firstDouble - secondDouble}");
            Console.WriteLine($"{firstDouble} * {secondDouble} = {firstDouble * secondDouble}");
            Console.WriteLine($"{firstDouble} * {2} = {firstDouble * 2}");
            Console.WriteLine($"{6} * {secondDouble} = {6 * secondDouble}");
            Console.WriteLine($"{firstDouble} / {secondDouble} = {firstDouble / secondDouble}");
            //Console.WriteLine($"-{firstDouble} = {-firstDouble}");
            Console.WriteLine($"{firstDouble} == {secondDouble} = {firstDouble == secondDouble}");
            Console.WriteLine($"{firstDouble} == {firstDouble} = {firstDouble == firstDouble}");
            Console.WriteLine($"{firstDouble} != {secondDouble} = {firstDouble != secondDouble}");
        }
    }
}
