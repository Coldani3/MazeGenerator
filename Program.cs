using System;

namespace MazeGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            new Maze(new MazeGrid(10, 10)).Generate();
        }
    }
}
