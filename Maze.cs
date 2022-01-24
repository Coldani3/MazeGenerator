using System.Collections.Generic;
using System.Linq;
using System;

namespace MazeGenerator
{
    public class Maze
    {
        public MazeGrid Grid;
        private Stack<int[]> Visited = new Stack<int[]>();
        private Random RNG;
        private byte CurrentCell;
        private bool AllowNonWallEntrance;
        private bool AllowNonWallExit;
        public int[] MazeEntrance;
        public int[] MazeExit;
        //TODO: higher and lower dimensions
        public int DirectionsCount = 4;

        public Maze(MazeGrid grid, int? seed = null, bool allowNonWallEntrance=false, bool allowNonWallExit=false)
        {
            this.Grid = grid;
            this.AllowNonWallEntrance = allowNonWallEntrance;
            this.AllowNonWallExit = allowNonWallExit;

            if (seed != null)
            {
                this.RNG = new Random((int) seed);
            }
            else
            {
                this.RNG = new Random();
            }
        }

        public Maze Generate()
        {
            //pick entrance and exit
            if (this.AllowNonWallEntrance)
            {
                this.MazeEntrance = new int[] {this.RNG.Next(this.Grid.Width), this.RNG.Next(this.Grid.Height)}; 
            }
            else
            {
                this.MazeEntrance = this.SelectRandomEdgeOfMaze(this.Grid.Width, this.Grid.Height);   
            }

            if (this.AllowNonWallExit)
            {
                int[] coords = new int[] {this.RNG.Next(this.Grid.Width), this.RNG.Next(this.Grid.Height)}; 

                //prevent maze entrance from being the same as the exit
                while (coords[0] == this.MazeEntrance[0] && coords[1] == this.MazeEntrance[1])
                {
                    coords = new int[] {this.RNG.Next(this.Grid.Width), this.RNG.Next(this.Grid.Height)}; 
                }

                this.MazeExit = coords;
            }
            else
            {
                int[] coords = this.SelectRandomEdgeOfMaze(this.Grid.Width, this.Grid.Height);

                //prevent maze entrance from being the same as the exit
                while (coords[0] == this.MazeEntrance[0] && coords[1] == this.MazeEntrance[1])
                {
                    coords = this.SelectRandomEdgeOfMaze(this.Grid.Width, this.Grid.Height);
                }

                this.MazeExit = coords;
            }

            this.Visit(this.MazeEntrance[0], this.MazeEntrance[1]);

            //begin generation

            int[] currentCell = this.MazeEntrance;

            while (this.Visited.Count > 0)
            {
                CellWallFlag direction = MazeGrid.Directions[this.RNG.Next(MazeGrid.Directions.Length - 1)];
                int[] change = MazeGrid.GetXYChangeForDirection(direction);

                int failedAttempts = 0;

                int[][] shuffledDirections = this.GetShuffledDirections();

                //check and make sure coords are not out of the grid
                while (!this.CoordInBounds(currentCell[0] + change[0], currentCell[1] + change[1]) ||
                    this.Grid.IsVisited(currentCell[0] + change[0], currentCell[1] + change[1]))
                {
                    change = shuffledDirections[failedAttempts];
                    failedAttempts++;

                    if (failedAttempts >= this.DirectionsCount)
                    {
                        int[] backtracked = this.Backtrack();

                        if (backtracked.Length < 2)
                        {
                            goto done;
                        }

                        currentCell = backtracked;
                        failedAttempts = 0;
                    }
                }

                int[] nextCellCoords = new int[] {currentCell[0] + change[0], currentCell[1] + change[1]};
                this.Visit(nextCellCoords[0], nextCellCoords[1]);
                this.Grid.SetWallsToOffAndUpdateAdjacent(currentCell[0], currentCell[1], (byte) direction);
                
            }

            done:
                ;



            return this;
        }

        public int[] Backtrack()
        {
            Random rng = new Random();
            int[] prevCoords;
            int[] coords = new int[2];

            //check to see if visited still has cells in it - if it's empty, we're done generating the maze
            if (this.Visited.Count > 0)
            {
                prevCoords = this.Visited.Pop();

                int[][] shuffledDirections = this.GetShuffledDirections();

                for (int i = 0; i < MazeGrid.Directions.Length; i++)
                {
                    coords = new int[] {prevCoords[0] + shuffledDirections[i][0], prevCoords[1] + shuffledDirections[i][1]};

                    if (this.CoordInBounds(coords[0], coords[1]) && !this.Grid.IsVisited(coords[0], coords[1])) 
                    {
                        return coords;
                    }
                }
            }
            else
            {
                //1D mazes just kinda... don't exist, so return a single value to indicate the maze is done.
                //TODO: something better and less hacky than this?
                return new int[] {-1};
            }

            return this.Backtrack();
        }

        public void Visit(int x, int y)
        {
            this.Grid.MarkVisited(x, y);
            this.Visited.Push(new int[] {x, y});
        }

        public int[] PickRandomCoord()
        {
            return new int[] {this.RNG.Next(this.Grid.Width - 1), this.RNG.Next(this.Grid.Height - 1)};
        }

        public int[] SelectRandomEdgeOfMaze(int width, int height)
        {
            //treat outside wall as a single continous line of length [height * 2 + width * 2 - 4] and cycle across it
            /*
            e.g width 4 and height 4
                   OO
              > O |--| O
                O |  | O
                O |  | O
                O |--| O
                   OO
            NOTE: each line is itself a cell
            -4 is due to the corners
            */

            int linePos = this.RNG.Next((width * 2) + (height * 2) - 4);
            //starting from top left corner increment x, after [width] then [height] is decreased

            //if i = 0 then cell is top left corner. if i = (width - 1) then top right
            //if i = (width - 1) + (height - 1) then bottom right

            //DEV NOTES: some way of taking the value, splitting it in half and if it goes over then x and y go opposite operation?
            //width 4 height 4
            //TOP LINE
            //0 1 2 3
            //|     | [4]
            //|     | [5]
            //9 8 7 6
            //9 7 5 3
            //BOTTOM LINE
            //3 = height - 1
            // if linePos > width + (height - 1)
            // x -= linePos - (width + (height - 1))
            //x = (linePos on the x) - (linePos minus the sum of the width and height of the grid) clamped between the width and zero
            // x = Clamp(linePos, 0, width - 1) - Clamp((linePos - (width + (height - 1))), 0, width - 1)
            // y starts as 3
            // until end of bottom line
            // y = ((height - 1) - Clamp((linePos - (width - 1)), 0, height - 1)) + (Clamp((linePos - (((width - 1) * 2) + (height - 1))), 0, height - 1))
            //

            int gridHeight = height - 1;
            int gridWidth = width - 1;
            
            int[] outCoords = new int[] {
                //x
                Math.Clamp(linePos, 0, gridWidth) - Math.Clamp(
                                                                    linePos - (width + gridHeight), 
                                                                    0, 
                                                                    gridWidth),
                //y
                (gridHeight - Math.Clamp(linePos - gridWidth, 0, gridHeight)) + (Math.Clamp(
                    linePos - (
                        (
                            gridWidth * 2
                        ) + 
                        gridHeight
                    ),
                    0,
                    gridHeight
                ))
            };

            return outCoords;
        }

        public bool CoordInBounds(int x, int y)
        {
            return x >= 0 || y >= 0 || x < this.Grid.Width - 1 || y < this.Grid.Height - 1;
        }

        public int[][] GetShuffledDirections()
        {
            return MazeGrid.Directions.OrderBy((x) => RNG.Next(2)).Select((x) => MazeGrid.GetXYChangeForDirection(x)).ToArray();
        }
    }
}