using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MazeGenerator
{
    class Program
    {
        public static uint AllDirections = 0;
        public static int Dimensions = 3;
        public static int MazeWidth = 15;
        public static int MazeHeight = 15;
        public static int MazeDepth = 4;
        public static int MazeHyperDepth = 4;
        public static int MinDistanceBetweenEntranceAndExit = 5;
        public static bool Running = true;
        public static bool MazeDoneGenning = false;
        public static int[] HigherDimCoords = new int[] {0, 0};
        public static Maze CurrentMaze;
        public static bool SavedMazeThisSession = false;
        

#region Debug and Rendering stuff
        public static bool Debugging = true;
        public static int DebugLogSize = 15;
        public static bool StepThrough = true;
        public static bool WaitForInput = true;
        public static bool InputThreadActive = false;
        public static int WaitTime = 10;
        private static List<string> DebugLog = new List<string>();
        //there are unfortunately no fancy bit tricks to get the right ones, so we're doing a lookup thing
        public static Dictionary<uint, char> MazeChars = new Dictionary<uint, char>() {
            {0, ' '},
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
            {0b001000000, ConsoleColor.Cyan},
            {0b001100000, ConsoleColor.DarkCyan},
            {0b010000000, ConsoleColor.DarkRed},
            {0b100000000, ConsoleColor.DarkGreen},
            {0b110000000, ConsoleColor.Yellow},
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
        private static string[] Controls = new string[] {
                "↑ - +y",
                "↓ - -y",
                "→ - +w",
                "← - -w",
                "Enter - step through maze gen",
                "a - do not wait for Enter",
                "s (once done) - save"
            };
#endregion

        static void Main(string[] args)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Clear();
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.CursorVisible = false;
            // Testing();
            // Console.ReadKey(true);

            Console.WriteLine($"Input dimension (leave blank to stick with default {Dimensions}): ");
            string dimensions = Console.ReadLine();

            Console.WriteLine("\nStep through generation process with Enter? (Y/n - Yes/No)");
            if (Console.ReadKey().KeyChar.ToString().ToLower() == "n")
            {
                StepThrough = false;
            }

            Console.Clear();

            if (dimensions != "")
            {
                Dimensions = Int32.Parse(dimensions);
            }

            Debug("dimensions: " + Dimensions);

            //get total of all directions
            MazeGrid.AllDirections.Take(Dimensions * 2).ToList().ForEach(x => {AllDirections += (uint) x; });

            Task inputTask = new Task(InputThread);
            inputTask.Start();

            int[] sizes = new int[] {MazeWidth, MazeHeight, MazeDepth, MazeHyperDepth};

            CurrentMaze = new Maze(new MazeGrid(sizes.Take(Dimensions).ToArray()));

            CurrentMaze.SetDebug(Debug);
            
            if (StepThrough)
            {
                CurrentMaze = CurrentMaze.SetRenderer(HandleRender).SetUpdateRendererHigherDim(UpdateRendererHigherDim).Generate();
            }
            else
            {
                CurrentMaze = CurrentMaze.Generate();
            }

            Render();

            MazeDoneGenning = true;
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

        static void ClearMazeDisplay()
        {
            Console.SetCursorPosition(0, 0);
            string clearString = new string(' ', CurrentMaze.Grid.Width);

            for (int i = 0; i < CurrentMaze.Grid.Height - 1; i++)
            {
                Console.SetCursorPosition(1, i + 1);
                Console.WriteLine(clearString);
            }
        }

        static async void Render()
        {
            //Console.Clear();
            //ClearMazeDisplay();

            int max = Controls.Max(x => x.Length);

            for (int i = 0; i < Controls.Length; i++)
            {
                Console.SetCursorPosition(Console.WindowWidth - 1 - max, i);
                Console.Write(Controls[i]);
            }

            Console.SetCursorPosition(0, 0);

            Console.Write("╔" + new String('═', CurrentMaze.Grid.Width) + "╗\n");
            int[] currCoords = new int[Dimensions];

            for (int y = 0; y < CurrentMaze.Grid.Height; y++)
            {
                Console.SetCursorPosition(0, (CurrentMaze.Grid.Height) - y);
                Console.Write('║');

                for (int x = 0; x < CurrentMaze.Grid.Width; x++)
                {
                    currCoords = new int[] {x, y}.Concat(HigherDimCoords).ToArray();
                    
                    uint mazeBit = CurrentMaze.Grid[currCoords] & 0b111111110;
                    uint mazeShapeBit = mazeBit & 0b11110;

                    char mazeChar = MazeChars[mazeShapeBit];

                    if (Maze.CoordsMatch(CurrentMaze.MazeEntrance, currCoords))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }

                    if (Maze.CoordsMatch(CurrentMaze.MazeExit, currCoords))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }

                    Console.ForegroundColor = ColoursForHigherDims[mazeBit & 0b001100000];
                    ConsoleColor background = ColoursForHigherDims[mazeBit & 0b110000000];
                    Console.BackgroundColor = (background == ConsoleColor.White ? ConsoleColor.Black : background);

                    if (Maze.CoordsMatch(CurrentMaze.MazeEntrance, currCoords))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }

                    if (Maze.CoordsMatch(CurrentMaze.MazeExit, currCoords))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }

                    Console.Write(mazeChar);
                    
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.Black;
                }

                Console.Write("║\n");
            }

            Console.SetCursorPosition(0, (CurrentMaze.Grid.Height + 1));
            Console.Write("╚" + new String('═', CurrentMaze.Grid.Width) + "╝");

            Console.SetCursorPosition(MazeWidth + 4, MazeHeight / 2);

            if (Dimensions > 2)
            {
                Console.Write($"Coords: Z: {HigherDimCoords[0]}");

                if (Dimensions > 3)
                {
                    Console.Write($" W: {HigherDimCoords[1]}");
                }
            }

            Console.SetCursorPosition(MazeWidth + 4, MazeHeight / 2 + 1);
            Console.Write($"Entrance: {String.Join(", ", CurrentMaze.MazeEntrance)}, Exit: {String.Join(", ", CurrentMaze.MazeExit)}");

            ConsoleColor foregroundDefault = Console.ForegroundColor;
            ConsoleColor backgroundDefault = Console.BackgroundColor;
            int height = (MazeHeight / 2) + 3;
            int width = MazeWidth + 4;

            Console.SetCursorPosition(width, height);
            Console.ForegroundColor = ColoursForHigherDims[(int) CellWallFlag.Up];
            Console.Write("+y");
            Console.SetCursorPosition(width, height + 1);
            Console.ForegroundColor = ColoursForHigherDims[(int) CellWallFlag.Down];
            Console.Write("-y");
            Console.SetCursorPosition(width, height + 2);
            Console.ForegroundColor = ColoursForHigherDims[0b001100000];
            Console.Write("+/-y");

            Console.ForegroundColor = ConsoleColor.Black;

            Console.SetCursorPosition(width, height + 4);
            Console.BackgroundColor = ColoursForHigherDims[(int) CellWallFlag.Ana];
            Console.Write("+w");
            Console.SetCursorPosition(width, height + 5);
            Console.BackgroundColor = ColoursForHigherDims[(int) CellWallFlag.Kata];
            Console.Write("-w");
            Console.SetCursorPosition(width, height + 6);
            Console.BackgroundColor = ColoursForHigherDims[0b110000000];
            Console.Write("+/-w");

            Console.BackgroundColor = backgroundDefault;
            Console.ForegroundColor = foregroundDefault;

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
            //uint threeDCell = 0b1101101;

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

            int[] zeroCoords = new int[] {4, 0};
            int[] change = new int[] {1, 1};
            int[] changeNeg = new int[] {-1, -1};

            Func<int[], string> asString = (x) => String.Join(", ", x); 
            int[] summedCoords1 = Maze.AddCoords(zeroCoords, change);
            int[] summedCoords2 = Maze.AddCoords(zeroCoords, changeNeg);

            Console.WriteLine($"Testing adding coords: {asString(summedCoords1)} length: - {asString(Maze.AddCoords(zeroCoords, changeNeg))}");

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

        public static void UpdateRendererHigherDim(params int[] coords)
        {
            if (!Maze.CoordsMatch(HigherDimCoords, coords))
            {
                HigherDimCoords = coords;
                //ClearMazeDisplay();
                Render();
            }
        }

        public static void Debug(string message) 
        {
            Debug(message, false);
        }

        static void ClearDebugLogDisplay()
        {
            string clearString = new String(' ', Console.WindowWidth - CurrentMaze.Grid.Width);

            for (int i = 0; i < DebugLogSize; i++)
            {
                Console.SetCursorPosition(CurrentMaze.Grid.Width + 2, (Console.WindowHeight - 1 - DebugLogSize) + i);
                Console.Write(clearString);
            }
        }

        public static void DisplayDebugLog()
        {
            ClearDebugLogDisplay();
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

                    if (newCoord < MazeDepth && newCoord >= 0)
                    {
                        HigherDimCoords[1] = newCoord;
                        Render();
                    }
                    break;

                case ConsoleKey.LeftArrow:
                    newCoord = (HigherDimCoords[1] - 1);

                    if (newCoord < MazeDepth && newCoord >= 0)
                    {
                        HigherDimCoords[1] = newCoord;
                        Render();
                    }

                    break;

                case ConsoleKey.UpArrow:
                    newCoord = (HigherDimCoords[0] + 1);

                    if (newCoord < MazeDepth && newCoord >= 0)
                    {
                        HigherDimCoords[0] = newCoord;
                        Render();
                    }

                    break;

                case ConsoleKey.DownArrow:
                    newCoord = (HigherDimCoords[0] - 1);

                    if (newCoord < MazeDepth && newCoord >= 0)
                    {
                        HigherDimCoords[0] = newCoord;
                        Render();
                    }

                    break;
                
                case ConsoleKey.S:
                    if (MazeDoneGenning && !SavedMazeThisSession)
                    {
                        string fileName = $"maze{CurrentMaze.Grid.Dimensions + "D"}.cd3maz";

                        int alreadyExists = 0;

                        while (System.IO.File.Exists(fileName))
                        {
                            alreadyExists++;
                            fileName = $"maze{CurrentMaze.Grid.Dimensions + "D"}{alreadyExists}.cd3maz";
                        }

                        Debug($"Saving to {fileName}...");

                        CurrentMaze.WriteToFile(fileName);

                        Debug($"Done saving.");

                        SavedMazeThisSession = true;
                        Render();
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
