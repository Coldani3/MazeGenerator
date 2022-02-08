using System;
using System.Linq;
using System.Collections.Generic;

namespace MazeGenerator
{
    class Program
    {
        //there are unfortunately no fancy bit tricks to get the right ones, so we're doing a lookup thing
        public static Dictionary<uint, char> MazeChars = new Dictionary<uint, char>() {
            {0, '■'},
            {ConstructWallDirectionsAvailableFlag(CellWallFlag.North), '╵'},
            {ConstructWallDirectionsAvailableFlag(CellWallFlag.South), '╷'},
            {ConstructWallDirectionsAvailableFlag(CellWallFlag.East), '╶'},
            {ConstructWallDirectionsAvailableFlag(CellWallFlag.West), '╴'},
            {ConstructWallDirectionsAvailableFlag(CellWallFlag.North, CellWallFlag.South), '│'},
            {ConstructWallDirectionsAvailableFlag(CellWallFlag.North, CellWallFlag.South, CellWallFlag.West), '┤'},
            {ConstructWallDirectionsAvailableFlag(CellWallFlag.North, CellWallFlag.South, CellWallFlag.East), '├'},
            {ConstructWallDirectionsAvailableFlag(CellWallFlag.East, CellWallFlag.West), '─'},
            {ConstructWallDirectionsAvailableFlag(CellWallFlag.East, CellWallFlag.West, CellWallFlag.North), '┴'},
            {ConstructWallDirectionsAvailableFlag(CellWallFlag.East, CellWallFlag.West, CellWallFlag.South), '┬'},
            {ConstructWallDirectionsAvailableFlag(CellWallFlag.West, CellWallFlag.North), '┘'},
            {ConstructWallDirectionsAvailableFlag(CellWallFlag.West, CellWallFlag.South), '┐'},
            {ConstructWallDirectionsAvailableFlag(CellWallFlag.South, CellWallFlag.East), '┌'},
            {ConstructWallDirectionsAvailableFlag(CellWallFlag.North, CellWallFlag.East), '└'},
            {ConstructWallDirectionsAvailableFlag(CellWallFlag.North, CellWallFlag.South, CellWallFlag.East, CellWallFlag.West), '┼'}
        };
        public static uint AllDirections = 0;
        public static int Dimensions = 2;
        public static int MazeWidth = 10;
        public static int MazeHeight = 10;
        public static bool Debugging = false;
        public static bool StepThrough = true;
        public static Maze CurrentMaze;

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Testing();
            Console.ReadKey(true);

            //get total of all directions
            MazeGrid.Directions.Take(Dimensions * 2).ToList().ForEach(x => {AllDirections += (uint) x; });

            CurrentMaze = new Maze(new MazeGrid(MazeWidth, MazeHeight));
            if (StepThrough)
            {
                CurrentMaze = CurrentMaze.SetRenderer(() => {
                    Console.Clear();
                    Render(); 
                    Console.ReadKey(true);
                    System.Threading.Thread.Sleep(100);
                }).Generate();
            }
            else
            {
                CurrentMaze = CurrentMaze.Generate();
            }
        }

        static void Render()
        {
            Console.Write("╔" + new String('═', CurrentMaze.Grid.Width) + "╗\n║");
            
            char currChar;

            for (int y = 0; y < CurrentMaze.Grid.Height; y++)
            {
                for (int x = 0; x < CurrentMaze.Grid.Width; x++)
                {
                    if (CurrentMaze.MazeEntrance[0] == x && CurrentMaze.MazeEntrance[1] == y)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }

                    if (CurrentMaze.MazeExit[0] == x && CurrentMaze.MazeExit[1] == y)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }

                    char mazeChar = MazeChars[CurrentMaze.Grid[x, y] & (0b11110)];

                    //FIXME: console is upside down to the coordinates.
                    Console.Write(mazeChar);
                    //Console.Write(GetMazeChar(x, y, CurrentMaze));
                    Console.ForegroundColor = ConsoleColor.White;
                }

                Console.Write("║\n║");
            }

            (int currX, int currY) = Console.GetCursorPosition();

            Console.SetCursorPosition(0, currY);
            Console.Write("╚" + new String('═', CurrentMaze.Grid.Width) + "╝");
        }

        static void Testing()
        {
            MazeGrid testGrid = new MazeGrid(1, 1);

            //visited, north wall, west wall
            // _
            //| 
            // 
            uint twoDCell = ConstructWallDirectionsAvailableFlag(CellWallFlag.South, CellWallFlag.East) + 1;//0b01101;
            uint threeDCell = 0b1101101;

            uint direction = (uint) CellWallFlag.East;
            uint direction2 = (uint) CellWallFlag.West;

            uint testNorthOrEastDNE = ConstructWallDirectionsAvailableFlag(CellWallFlag.North, CellWallFlag.East);
            uint testSouthEastDNE = ConstructWallDirectionsAvailableFlag(CellWallFlag.South, CellWallFlag.East);

            testGrid.SetDirectionsToAvailable(0, 0, twoDCell);

            Console.WriteLine($"testNOrEDNE: {Convert.ToString(testNorthOrEastDNE, 2)}");
            Console.WriteLine($"testSEDNE: {Convert.ToString(testSouthEastDNE, 2)}");

            Console.WriteLine($"Testing if either the north or east walls do not exist (are 1): {testGrid.AreAnyDirectionsAvailable(0, 0, testNorthOrEastDNE)}, should be True");
            Console.WriteLine($"Testing if both the south and east walls do not exist (are 1): {testGrid.AreAllDirectionsAvailable(0, 0, testSouthEastDNE)}, should be True");
            Console.WriteLine($"Testing if either the north and west walls do not exist (are 1): {testGrid.AreAnyDirectionsAvailable(0, 0, ~testSouthEastDNE - 1)}, should be False");
            Console.WriteLine($"Testing if both the north and west walls do not exist (are 1): {testGrid.AreAllDirectionsAvailable(0, 0, ~testSouthEastDNE - 1)}, should be False");
            Console.WriteLine($"Testing if opposite directions are gotten properly: Opposite of {Convert.ToString(direction, 2)} is {Convert.ToString(MazeGrid.GetOppositeSide(direction), 2)}, should be {Convert.ToString(direction2, 2)}");
            Console.WriteLine($"Testing if opposite directions are gotten properly: Opposite of {Convert.ToString(direction2, 2)} is {Convert.ToString(MazeGrid.GetOppositeSide(direction2), 2)}, should be {Convert.ToString(direction, 2)}");
        }

        static uint ConstructWallDirectionsAvailableFlag(params CellWallFlag[] walls)
        {
            uint output = 0;

            foreach (CellWallFlag flag in walls)
            {
                output |= (uint) flag;
            }

            return output;
        }

        public static void Debug(string message)
        {
            if (Debugging)
            {
                Console.WriteLine(message);
            }
            // (int currLeft, int currTop) = Console.GetCursorPosition();

            // Console.SetCursorPosition(Console.WindowWidth - message.Length, Console.WindowHeight);
        }
    }
}
