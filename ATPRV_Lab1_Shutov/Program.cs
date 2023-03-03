using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Diagnostics;

namespace ATPRV_Shutov_Lab1;

class Program
{
    public static void MatrixMultiply(int[,] matrix1, int[,] matrix2, int[,] result)
    {
        int y = matrix1.GetLength(0);
        int x = matrix2.GetLength(1);
        
        for (int row = 0; row < y; ++row)
        {
            for (int col = 0; col < x; ++col)
            {
                for (int index = 0; index < x; ++index)
                {
                    result[row, col] += matrix1[row, index] * matrix2[index, col];
                }
            }
        }
    }

    public static void ThreadMatrixMultiply(int[,] matrix1, int[,] matrix2, int[,] result, int threadsCount)
    {
        int y = matrix1.GetLength(0);
        int x = matrix2.GetLength(1);

        var threads = new List<Thread>();

        int rowsOnThread = y / threadsCount;
        for (int threadIndex = 0; threadIndex < threadsCount; ++threadIndex)
        {
            int start = threadIndex * rowsOnThread;
            int end = threadIndex == threadsCount - 1 ? y : threadIndex * rowsOnThread + rowsOnThread;

            Thread t = new Thread(() =>
            {
                for (int row = start; row < end; ++row)
                {
                    for (int col = 0; col < x; ++col)
                    {
                        for (int index = 0; index < x; ++index)
                        {
                            result[row, col] += matrix1[row, index] * matrix2[index, col];
                        }
                    }
                }
            });
            threads.Add(t);
            t.Start();
        }
        
        threads.ForEach(thread => thread.Join());
    }

    public static void ChaosMatrixMultiply(int[,] matrix1, int[,] matrix2, int[,] result)
    {
        int y = matrix1.GetLength(0);
        int x = matrix2.GetLength(1);

        var threads = new List<Thread>();
        
        for (int row = 0; row < y; ++row)
        {
            for (int col = 0; col < x; ++col)
            {
                for (int ind = 0; ind < x; ++ind)
                {
                    var rowCopy = row;
                    var colCopy = col;
                    var indCopy = ind;
                    
                    Thread t = new Thread(() =>
                    {
                        result[rowCopy, colCopy] += matrix1[rowCopy, indCopy] * matrix2[indCopy, colCopy]; 
                    });
                    
                    t.Start();
                    threads.Add(t);
                }
            }
        }
        
        threads.ForEach(thread => thread.Join());
    }

    public static void PrintMatrix(int[,] matrix)
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

    public static int[,] MakeMatrix(int rows, int cols, int @base, Func<int, int> func)
    {
        int[,] result = new int[rows, cols];
        int prev = @base;
            
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

    public static void MatrixToZero(int[,] matrix)
    {
        int y = matrix.GetLength(0);
        int x = matrix.GetLength(1);

        for (int i = 0; i < y; ++i)
        {
            for (int j = 0; j < x; ++j)
            {
                matrix[i, j] = 0;
            }
        }
    } 

    public static void Main(string[] args)
    {
        Stopwatch watch;

        KeyValuePair<int[,], int[,]> a;
        
        int[] matrixSizes = { 10, 50, 100, 200, 300, 500, /* 1000 */ };
        var matrixes = new List<KeyValuePair<int[,], int[,]>>();
        foreach (var size in matrixSizes)
        {
            matrixes.Add(new KeyValuePair<int[,], int[,]>(
                MakeMatrix(size, size, 1, x => x + 1),  // Multiplied matrix
                MakeMatrix(size, size, 0, x => 0))      // Result template matrix
            );

        }
            
            
        // CLASSIC:
        Console.WriteLine("Classic");
        matrixes.ForEach(matrix =>
        {
            watch = Stopwatch.StartNew();
            MatrixMultiply(matrix.Key, matrix.Key, matrix.Value);
            watch.Stop();
            Console.WriteLine($"    Ms: {watch.ElapsedMilliseconds}; size: {matrix.Key.GetLength(0)}");
        });
        matrixes.ForEach(matrix => MatrixToZero(matrix.Value));
        
        
        // PARALLEL 1:
        Console.WriteLine("Parallel 1");
        int[] threadsCount = { 2, 4, 8, 16 };

        foreach (var threadCount in threadsCount)
        {
            Console.WriteLine($"    Threads: {threadCount}");
            matrixes.ForEach(matrix =>
            {
                watch = Stopwatch.StartNew();
                ThreadMatrixMultiply(matrix.Key, matrix.Key, matrix.Value, threadCount);
                watch.Stop();
                Console.WriteLine($"        Ms: {watch.ElapsedMilliseconds}; size: {matrix.Key.GetLength(0)}");
            });
            matrixes.ForEach(matrix => MatrixToZero(matrix.Value));
        }

        // PARALLEL 2:
        Console.WriteLine("Parallel 2");
        matrixes.ForEach(matrix =>
        {
            watch = Stopwatch.StartNew();
            ChaosMatrixMultiply(matrix.Key, matrix.Key, matrix.Value);
            watch.Stop();
            Console.WriteLine($"    Ms: {watch.ElapsedMilliseconds}; size: {matrix.Value.GetLength(0)}");
        });

        // PrintMatrix(MultiplyMatrix(MakeMatrix(2, 2, 1, x => x + 1), MakeMatrix(2, 2, 1, x => x)));
        // Console.ReadKey();
    }
}