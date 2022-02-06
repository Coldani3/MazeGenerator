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
            Testing();
            Console.ReadKey(true);

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
            //if all of the walls exist
            else if (!maze.Grid.DoAnyWallsNotExist(x, y, AllDirections))
            {
                return '■';
            }

            //narrow down char from directions as a workaround to there being no funny bit tricks
            for (int i = 0; i < Dimensions * 2; i++)
            {
                if (!maze.Grid.DoAllWallsNotExist(x, y, (uint) MazeGrid.Directions[i]))
                {
                    possibles = possibles.Intersect(MazeChars[i]).ToArray();
                }
                // else
                // {
                //     possibles = possibles.Except(MazeChars[i]).ToArray();
                // }
            }

            if (possibles.Length > 1)
            {
                Console.Write("{");
                possibles.ToList().ForEach(x => Console.Write("," + x));
                Console.Write("[ " + possibles.Length + " ]");
                Console.Write("}");
            }

            return possibles[0];
        }

        static void Testing()
        {
            MazeGrid testGrid = new MazeGrid(1, 1);

            //visited, north wall, west wall
            // _
            //| 
            // 
            uint twoDCell = ConstructWallDNEFlag(CellWallFlag.South, CellWallFlag.East) + 1;//0b01101;
            uint threeDCell = 0b1101101;

            uint testNorthOrEastDNE = ConstructWallDNEFlag(CellWallFlag.North, CellWallFlag.East);
            uint testSouthEastDNE = ConstructWallDNEFlag(CellWallFlag.South, CellWallFlag.East);

            testGrid.SetWallsToOff(0, 0, twoDCell);

            Console.WriteLine($"testNOrEDNE: {Convert.ToString(testNorthOrEastDNE, 2)}");
            Console.WriteLine($"testSEDNE: {Convert.ToString(testSouthEastDNE, 2)}");

            Console.WriteLine($"Testing if either the north or east walls do not exist (are 1): {testGrid.DoAnyWallsNotExist(0, 0, testNorthOrEastDNE)}, should be True");
            Console.WriteLine($"Testing if both the south and east walls do not exist (are 1): {testGrid.DoAllWallsNotExist(0, 0, testSouthEastDNE)}, should be True");
            Console.WriteLine($"Testing if either the north and west walls do not exist (are 1): {testGrid.DoAnyWallsNotExist(0, 0, ~testSouthEastDNE - 1)}, should be False");
            Console.WriteLine($"Testing if both the north and west walls do not exist (are 1): {testGrid.DoAllWallsNotExist(0, 0, ~testSouthEastDNE - 1)}, should be False");




            

        }

        static uint ConstructWallDNEFlag(params CellWallFlag[] walls)
        {
            uint output = 0;

            foreach (CellWallFlag flag in walls)
            {
                output |= (uint) flag;
            }

            return output;
        }
    }
}
