using System;
using System.Linq;

namespace MazeGenerator
{
    class Program
    {
        //there are unfortunately no fancy bit tricks to get the right ones, so we're doing a lookup thing
        public static char[][] MazeChars = new char[4][] {
            //north
            new char[] {'╣', '║', '╝', '╚', '╩', '╠', '╬'},
            //south
            new char[] {'╣', '║', '╠', '╬', '╗', '╔', '╦'},
            //east
            new char[] {'╚', '╩', '╠', '╬', '╔', '╦', '═'},
            //west
            new char[] {'╣', '╝', '╩', '╬', '╦', '═', '╗'}
        };
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            Maze maze = new Maze(new MazeGrid(10, 10)).Generate();

            for (int x = 0; x < maze.Grid.Width; x++)
            {
                for (int y = 0; y < maze.Grid.Height; y++)
                {

                }

                Console.Write('\n');
            }
        }

        static char GetMazeChar(int x, int y, Maze maze)
        {
            char[] possibles = new char[] {'╝',	'╗', '╔', '╚', '╣', '╩', '╦', '╠', '═', '║', '╬'};
            uint walls = maze.Grid[x, y];

            for (int i = 0; i < 4; i++)
            {
                if (maze.Grid.DoWallsNotExist(x, y, (uint) MazeGrid.Directions[i]))
                {
                    possibles = possibles.Intersect(MazeChars[i]).ToArray();
                    break;
                }
                else
                {
                    
                }
            }
        }
    }
}
