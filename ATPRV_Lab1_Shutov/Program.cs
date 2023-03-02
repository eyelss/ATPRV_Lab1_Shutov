using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Diagnostics;
namespace ATPRV_Shutov_Lab1
{
    public static class ArrayExt {
        public static IEnumerable<T> SliceColumn<T>(this T[,] array, int column)
        {
            for (var i = 0; i < array.GetLength(0); ++i)
                yield return array[i, column];
        }

        public static IEnumerable<T> SliceRow<T>(this T[,] array, int row)
        {
            // var o1 = array.GetLength(0);
            // var o2 = array.GetLength(1);
            for (var i = 0; i < array.GetLength(1); ++i)
                yield return array[row, i];
        }
    }

    class Program
    {
        public static double[,] MultiplyMatrix(double[,] matrix1, double[,] matrix2, int from = -1, int to = -1, double[,]? templ = null)
        {
            int xLength = matrix1.GetLength(0);
            
            if (from < 0)
                from = 0;
            
            if (to < xLength)
                to = xLength;
            
            if (from > xLength || to > xLength)
                throw new IndexOutOfRangeException("Out of diapason of matrix.");
            
            double[,] result = templ ?? new double[to - from, matrix2.GetLength(1)];
            for (int i = from; i < to; ++i)
            {
                for (int j = 0; j < matrix2.GetLength(1); ++j)
                {
                    result[j, i] = MultiplyRow(matrix1.SliceRow(i).ToArray(), matrix2.SliceColumn(j).ToArray());
                }
            }

            return result;
        }

        public static void PrintMatrix(double[,] matrix)
        {
            for (int i = 0; i < matrix.GetLength(0); ++i)
            {
                for (int j = 0; j < matrix.GetLength(1); ++j)
                {
                    Console.Write($"{matrix[j, i]} ");
                }
                Console.WriteLine();
            }
        }

        public static double[,] ThreadMatrixMultiply(double[,] matrix1, double[,] matrix2, int threadCount, double[,]? templ = null)
        {
            Thread[] pool = new Thread[threadCount];
            int currentThreadIndex = 0;
            
            var lenRows = matrix1.GetLength(0);
            var threadPerRowsLen = lenRows / (float)threadCount;
            int baseRowsLen = (int)threadPerRowsLen;

            double[,] result = templ ?? new double[matrix1.GetLength(0), matrix2.GetLength(1)];
            
            for (int i = 0; i < threadCount; ++i)
            {
                int subRowsCount = (int)threadPerRowsLen;
                if (threadCount - 1 == i)
                {
                    subRowsCount = (int)Math.Ceiling(threadPerRowsLen);
                }


                var i1 = i;
                pool[currentThreadIndex] = new Thread(() =>
                    MultiplyMatrix(matrix1, matrix2, i1 * baseRowsLen, i1 * baseRowsLen + subRowsCount, result));
                pool[currentThreadIndex++].Start();
            }
            
            foreach (var thread in pool)
            {
                thread.Join();
            }

            return result;
        }

        public static double[,] ChaosThreadMultiplyMatrix(double[,] matrix1, double[,] matrix2, double[,]? templ = null)
        {
            List<Thread> threads = new List<Thread>();

            double[,] result = templ ?? new double[matrix1.GetLength(0), matrix2.GetLength(1)];
            for (int i = 0; i < matrix1.GetLength(0); ++i)
            {
                for (int j = 0; j < matrix2.GetLength(1); ++j)
                {
                    var jCopy = j;
                    var iCopy = i;
                    var thread = new Thread(() =>
                    {
                        result[jCopy, iCopy] = MultiplyRow(matrix1.SliceRow(iCopy).ToArray(), matrix2.SliceColumn(jCopy).ToArray()); 
                    });
                    thread.Start();
                    threads.Add(thread);
                }
            }

            threads.ForEach(thread => thread.Join());
            return result;
        }

        public static double MultiplyRow(double[] vector1, double[] vector2)
        {
            return vector1.Zip(vector2, (first, second) => first * second).Sum();
        }

        public static double[,] MakeMatrix(int rows, int cols, double @base, Func<double, double> func)
        {
            double[,] result = new double[rows, cols];
            double prev = @base;
            
            for (int i = 0; i < rows; ++i)
            {
                for (int j = 0; j < cols; j++)
                {
                    result[j, i] = prev;
                    prev = func(prev);
                }
            }

            return result;
        }

        public static void Main(string[] args)
        {
            Stopwatch watch;

            int[] matrixSizes = { 10, 50, 100 /*, 200, 500, 1000 */ };
            List<double[,]> matrixes = new List<double[,]>();
            
            foreach (var size in matrixSizes)
            {
                matrixes.Add(MakeMatrix(size, size, 1.0, (x) => x + 0.1));
            }
            
            // CLASSIC:
            Console.WriteLine("Classic");
            matrixes.ForEach(matrix =>
            {
                watch = Stopwatch.StartNew();
                MultiplyMatrix(matrix, matrix);
                watch.Stop();
                Console.WriteLine($"    Ms: {watch.ElapsedMilliseconds}; size: {matrix.GetLength(0)}");
            });

            // PARALLEL 1:
            Console.WriteLine("Parallel 1");
            int[] threadsCount = { 2, 4, 8, 16 };

            foreach (var threadCount in threadsCount)
            {
                Console.WriteLine($"    Threads: {threadCount}");
                matrixes.ForEach(matrix =>
                {
                    watch = Stopwatch.StartNew();
                    ThreadMatrixMultiply(matrix, matrix, threadCount);
                    watch.Stop();
                    Console.WriteLine($"        Ms: {watch.ElapsedMilliseconds}; size: {matrix.GetLength(0)}");
                });
            }

            // PARALLEL 2:
            Console.WriteLine("Parallel 2");
            matrixes.ForEach(matrix =>
            {
                watch = Stopwatch.StartNew();
                ChaosThreadMultiplyMatrix(matrix, matrix);
                watch.Stop();
                Console.WriteLine($"    Ms: {watch.ElapsedMilliseconds}; size: {matrix.GetLength(0)}");
            });

            Console.ReadKey();
        }
    }
}
