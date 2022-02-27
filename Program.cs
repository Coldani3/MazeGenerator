using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MazeGenerator
{
    class Program
    {
        public static uint AllDirections = 0;
        public static int Dimensions = 2;
        public static int MazeWidth = 10;
        public static int MazeHeight = 10;
        public static int MazeDepth = 4;
        public static int MazeHyperDepth = 4;
        public static int MinDistanceBetweenEntranceAndExit = 5;
        public static bool Running = true;
        public static int[] HigherDimCoords = new int[] {0, 0};
        public static Maze CurrentMaze;
        

#region Debug and Rendering stuff
        public static bool Debugging = true;
        public static int DebugLogSize = 15;
        public static bool StepThrough = true;
        public static bool WaitForInput = true;
        public static bool InputThreadActive = false;
        public static int WaitTime = 50;
        private static List<string> DebugLog = new List<string>();
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

        public static Dictionary<uint, ConsoleColor> ColoursForHigherDims = new Dictionary<uint, ConsoleColor>() {
            {0b000100000, ConsoleColor.Blue},
            {0b001000000, ConsoleColor.DarkBlue},
            {0b001100000, ConsoleColor.Yellow},
            {0b010000000, ConsoleColor.Gray},
            {0b100000000, ConsoleColor.DarkGray},
            {0b110000000, ConsoleColor.Magenta},
            {0, ConsoleColor.White}
        };
        public static Dictionary<CellWallFlag, string> DirectionNames = new Dictionary<CellWallFlag, string>() {
            {CellWallFlag.Up, "Up"},
            {CellWallFlag.Down, "Down"},
            {CellWallFlag.West, "West"},
            {CellWallFlag.East, "East"},
            {CellWallFlag.North, "North"},
            {CellWallFlag.South, "South"},
            {CellWallFlag.Ana, "Ana"},
            {CellWallFlag.Kata, "Kata"},
        };
#endregion

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.CursorVisible = false;
            // Testing();
            // Console.ReadKey(true);

            Console.WriteLine($"Input dimension (leave blank to stick with default {Dimensions}): ");
            string dimensions = Console.ReadLine();

            if (dimensions != "")
            {
                Dimensions = Int32.Parse(dimensions);
            }

            //get total of all directions
            MazeGrid.AllDirections.Take(Dimensions * 2).ToList().ForEach(x => {AllDirections += (uint) x; });

            Task inputTask = new Task(InputThread);
            inputTask.Start();

            CurrentMaze = new Maze(new MazeGrid(MazeWidth, MazeHeight));

            CurrentMaze.SetDebug(Debug);
            
            if (StepThrough)
            {
                CurrentMaze = CurrentMaze.SetRenderer(HandleRender).Generate();
            }
            else
            {
                CurrentMaze = CurrentMaze.Generate();
            }

            InputThreadActive = true;

            while (Running)
            {
                HandleInput(Console.ReadKey(true));
                Render();
            }
        }

        static void HandleRender()
        {
            Render();

            if (WaitForInput)
            {
                HandleInput(Console.ReadKey(true));
            }
            else if (!InputThreadActive)
            {
                InputThreadActive = true;
            }

            System.Threading.Thread.Sleep(WaitTime);
        }

        static void Render()
        {
            Console.Clear();
            Console.Write("╔" + new String('═', CurrentMaze.Grid.Width) + "╗\n");
            
            char currChar;

            for (int y = 0; y < CurrentMaze.Grid.Height; y++)
            {
                Console.SetCursorPosition(0, (CurrentMaze.Grid.Height) - y);
                Console.Write('║');

                for (int x = 0; x < CurrentMaze.Grid.Width; x++)
                {
                    uint mazeBit = CurrentMaze.Grid[x, y, HigherDimCoords[0], HigherDimCoords[1]] & (0b111111110);

                    char mazeChar = MazeChars[mazeBit];

                    if (CurrentMaze.MazeEntrance[0] == x && CurrentMaze.MazeEntrance[1] == y)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }

                    if (CurrentMaze.MazeExit[0] == x && CurrentMaze.MazeExit[1] == y)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }

                    Console.ForegroundColor = ColoursForHigherDims[mazeBit & 0b001100000];
                    ConsoleColor background = ColoursForHigherDims[mazeBit & 0b110000000];
                    Console.BackgroundColor = (background == ConsoleColor.White ? ConsoleColor.Black : background);

                    if (CurrentMaze.MazeEntrance[0] == x && CurrentMaze.MazeEntrance[1] == y)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }

                    if (CurrentMaze.MazeExit[0] == x && CurrentMaze.MazeExit[1] == y)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }

                    Console.Write(mazeChar);
                    
                    Console.ForegroundColor = ConsoleColor.White;
                }

                Console.Write("║\n");
            }

            Console.SetCursorPosition(0, (CurrentMaze.Grid.Height + 1));
            Console.Write("╚" + new String('═', CurrentMaze.Grid.Width) + "╝");

            if (DebugLog.Count > 0) DisplayDebugLog();
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

            testGrid.SetDirectionsToAvailable(twoDCell, 0, 0);

            Console.WriteLine($"testNOrEDNE: {Convert.ToString(testNorthOrEastDNE, 2)}");
            Console.WriteLine($"testSEDNE: {Convert.ToString(testSouthEastDNE, 2)}");

            Console.WriteLine($"Testing if either the north or east walls do not exist (are 1): {testGrid.AreAnyDirectionsAvailable(testNorthOrEastDNE, 0, 0)}, should be True");
            Console.WriteLine($"Testing if both the south and east walls do not exist (are 1): {testGrid.AreAllDirectionsAvailable(testSouthEastDNE, 0, 0)}, should be True");
            Console.WriteLine($"Testing if either the north and west walls do not exist (are 1): {testGrid.AreAnyDirectionsAvailable(~testSouthEastDNE - 1, 0, 0)}, should be False");
            Console.WriteLine($"Testing if both the north and west walls do not exist (are 1): {testGrid.AreAllDirectionsAvailable(~testSouthEastDNE - 1, 0, 0)}, should be False");
            Console.WriteLine($"Testing if opposite directions are gotten properly: Opposite of {Convert.ToString(direction, 2)} is {Convert.ToString(MazeGrid.GetOppositeSide(direction), 2)}, should be {Convert.ToString(direction2, 2)}");
            Console.WriteLine($"Testing if opposite directions are gotten properly: Opposite of {Convert.ToString(direction2, 2)} is {Convert.ToString(MazeGrid.GetOppositeSide(direction2), 2)}, should be {Convert.ToString(direction, 2)}");

            int[] coords1 = new int[] {1, 2, 3, 4};
            int[] coords2 = new int[] {4, 3, 2, 1};

            bool addsProperly = true;

            foreach (int num in Maze.AddCoords(coords1, coords2))
            {
                if (num != 5)
                {
                    addsProperly = false;
                }
            }

            Console.WriteLine($"Testing coord adding works properly: {addsProperly}");
            Console.WriteLine($"Testing if coord comparison works: {Maze.CoordsMatch(coords1, coords1)}");

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

        public static void Debug(string message, bool displayDebugLog=false)
        {
            DebugLog.Add(message);

            if (Debugging && displayDebugLog)
            {   
                DisplayDebugLog();
            }
        }

        public static void Debug(string message) 
        {
            Debug(message, false);
        }

        public static void DisplayDebugLog()
        {
            string[] toPrint = DebugLog.TakeLast(DebugLogSize).ToArray();
            int maxLength = toPrint.Max(x => x.Length);

            (int currLeft, int currTop) = Console.GetCursorPosition();

            for (int i = 0; i < toPrint.Length; i++)
            {
                Console.SetCursorPosition(Math.Clamp(Console.WindowWidth - maxLength, MazeWidth + 3, Console.WindowWidth), Console.WindowHeight - i - 2);

                string trimmed = toPrint[toPrint.Length - 1 - i].Substring(0, Math.Clamp(Console.WindowWidth - (MazeWidth + 3), 0, toPrint[toPrint.Length - 1 - i].Length));

                Console.Write(trimmed + new String(' ', (Console.WindowWidth - (MazeWidth + 3)) - trimmed.Length));
            }

            Console.SetCursorPosition(currLeft, currTop);
        }

        public static void InputThread()
        {
            while (true)
            {
                if (InputThreadActive) HandleInput(Console.ReadKey(true));
            }
        }

        public static void HandleInput(ConsoleKeyInfo input)
        {
            int newCoord = 0;
            switch (input.Key)
            {
                case ConsoleKey.A:
                    WaitForInput = !WaitForInput;
                    break;
                
                case ConsoleKey.RightArrow:
                    newCoord = (HigherDimCoords[1] + 1);

                    if (newCoord <= MazeDepth && newCoord >= 0)
                    {
                        HigherDimCoords[1] = newCoord;
                    }
                    break;

                case ConsoleKey.LeftArrow:
                    newCoord = (HigherDimCoords[1] - 1);

                    if (newCoord <= MazeDepth && newCoord >= 0)
                    {
                        HigherDimCoords[1] = newCoord;
                    }

                    break;

                case ConsoleKey.UpArrow:
                    newCoord = (HigherDimCoords[0] + 1);

                    if (newCoord <= MazeDepth && newCoord >= 0)
                    {
                        HigherDimCoords[0] = newCoord;
                    }

                    break;

                case ConsoleKey.DownArrow:
                    newCoord = (HigherDimCoords[0] - 1);

                    if (newCoord <= MazeDepth && newCoord >= 0)
                    {
                        HigherDimCoords[0] = newCoord;
                    }

                    break;
            }
        }

        public static string DirectionName(CellWallFlag direction)
        {
            return DirectionNames[direction];
        }
    }
}
