using System;
using System.Linq;

namespace MazeGenerator
{
    class Program
    {
        //there are unfortunately no fancy bit tricks to get the right ones, so we're doing a lookup thing
        public static char[][] MazeChars = new char[4][] {
            //north
            new char[] {'╣', '║', '╝', '╚', '╩', '╠'},
            //south
            new char[] {'╣', '║', '╠', '╗', '╔', '╦'},
            //east
            new char[] {'╚', '╩', '╠', '╔', '╦', '═'},
            //west
            new char[] {'╣', '╝', '╩', '╦', '═', '╗'}
        };
        public static uint AllDirections = 0;
        public static int Dimensions = 2;
        public static int MazeWidth = 10;
        public static int MazeHeight = 10;

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            //get total of all directions
            MazeGrid.Directions.Take(Dimensions * 2).ToList().ForEach(x => {AllDirections += (uint) x; });

            Maze maze = new Maze(new MazeGrid(MazeWidth, MazeHeight)).Generate();

            for (int y = 0; y < maze.Grid.Height; y++)
            {
                for (int x = 0; x < maze.Grid.Width; x++)
                {
                    if (maze.MazeEntrance[0] == x && maze.MazeEntrance[1] == y)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }

                    Console.Write(GetMazeChar(x, y, maze));
                    Console.ForegroundColor = ConsoleColor.White;
                }

                Console.Write('\n');
            }
        }

        static char GetMazeChar(int x, int y, Maze maze)
        {
            char[] possibles = new char[] {'╝',	'╗', '╔', '╚', '╣', '╩', '╦', '╠', '═', '║' };
            uint walls = maze.Grid[x, y];

            if (maze.Grid.DoAllWallsNotExist(x, y, AllDirections))
            {
                return '╬';
            }
            // //if all of the walls exist
            // else if (!maze.Grid.DoAnyWallsNotExist(x, y, AllDirections))
            // {
            //     return '■';
            // }

            //narrow down char from directions as a workaround to there being no funny bit tricks
            for (int i = 0; i < Dimensions * 2; i++)
            {
                if (maze.Grid.DoAllWallsNotExist(x, y, (uint) MazeGrid.Directions[i]))
                {
                    possibles = possibles.Intersect(MazeChars[i]).ToArray();
                }
            }

            Console.Write("[" + possibles + "]");

            return possibles[0];
        }
    }
}
