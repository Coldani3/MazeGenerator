using System.Collections.Generic;
using System;

namespace MazeGenerator
{
    public class MazeGeneration
    {
        public MazeGrid Grid;
        private Stack<int[]> Visited = new Stack<int[]>();
        private Random RNG;
        private byte CurrentCell;
        private bool AllowNonWallEntrance;
        private bool AllowNonWallExit;
        public int[] MazeEntrance;
        public int[] MazeExit;

        public MazeGeneration(MazeGrid grid, int? seed = null, bool allowNonWallEntrance=false, bool allowNonWallExit=false)
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

        public void Generate()
        {
            //pick entrance and exit
            if (this.AllowNonWallEntrance)
            {
                this.MazeEntrance = new int[] {this.RNG.Next(this.Grid.Width), this.RNG.Next(this.Grid.Height)}; 
            }
            else
            {
                this.MazeEntrance = SelectRandomEdgeOfMaze();   
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
                int[] coords = SelectRandomEdgeOfMaze();

                //prevent maze entrance from being the same as the exit
                while (coords[0] == this.MazeEntrance[0] && coords[1] == this.MazeEntrance[1])
                {
                    coords = SelectRandomEdgeOfMaze();
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

                //check and make sure coords are not out of the grid
                while ((currentCell[0] + change[0]) < 0 || (currentCell[1] + change[1]) < 0 || 
                    (currentCell[0] + change[0]) > this.Grid.Width - 1 || (currentCell[1] + change[1]) > this.Grid.Height - 1 ||
                    this.Grid.IsVisited(currentCell[0] + change[0], currentCell[1] + change[1]))
                {
                    direction = MazeGrid.Directions[this.RNG.Next(MazeGrid.Directions.Length - 1)];
                    change = MazeGrid.GetXYChangeForDirection(direction);
                    failedAttempts += 1;
                    if (failedAttempts >= 4)
                    {
                        this.Backtrack(currentCell[0], currentCell[1]);
                    }
                }

                int[] nextCellCoords = new int[] {currentCell[0] + change[0], currentCell[1] + change[1]};
                this.Visit(nextCellCoords[0], nextCellCoords[1]);
                this.Grid.SetWallsToOffAndUpdateAdjacent(currentCell[0], currentCell[1], (byte) direction);
                
            }
        }

        public void Backtrack(int xFrom, int yFrom)
        {

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

        public int[] SelectRandomEdgeOfMaze()
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

            int linePos = this.RNG.Next((this.Grid.Width * 2) + (this.Grid.Height * 2) - 4);
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
            // x = Clamp(linePos, 0, width - 1) - Clamp((linePos - (width + (height - 1))), 0, width - 1)
            // y starts as 3
            // until end of bottom line
            // y = ((height - 1) - Clamp((linePos - (width - 1)), 0, height - 1)) + (Clamp((linePos - (((width - 1) * 2) + (height - 1))), 0, height - 1))
            //

            int[] outCoords = new int[] {
                //x
                Math.Clamp(linePos, 0, this.Grid.Width - 1) - Math.Clamp(
                                                                    linePos - (this.Grid.Width + (this.Grid.Height - 1)), 
                                                                    0, 
                                                                    this.Grid.Width - 1),
                //y
                ((this.Grid.Height - 1) - Math.Clamp(
                    linePos - (this.Grid.Width - 1),
                    0,
                    this.Grid.Height - 1
                )) + (Math.Clamp(
                    linePos - (
                        (
                            (this.Grid.Width - 1) * 2
                        ) + 
                        (this.Grid.Height - 1)
                    ),
                    0,
                    this.Grid.Height - 1
                ))
            };

            return outCoords;
        }
    }
}