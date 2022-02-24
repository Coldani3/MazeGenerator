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
        private bool AllowNonWallEntrance;
        private bool AllowNonWallExit;
        public int[] MazeEntrance;
        public int[] MazeExit;
        //TODO: higher and lower dimensions
        public int DirectionsCount = 4;
        public Action Render = null;

        public Maze(MazeGrid grid, int? seed=null, bool allowNonWallEntrance=false, bool allowNonWallExit=false)
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

        public Maze SetRenderer(Action render)
        {
            this.Render = render;
            return this;
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
                CellWallFlag direction = this.RandomDirection();
                int[] change = MazeGrid.GetXYChangeForDirection(direction);

                int failedAttempts = 0;

                CellWallFlag[] shuffledDirections = this.GetShuffledDirections();
                int[][] shuffledDirectionChanges = this.GetDirectionChangesFromDirections(shuffledDirections);

                int changedX = currentCell[0] + change[0];
                int changedY = currentCell[1] + change[1];

                //TODO: redo this, probably just iterate through every direction until you find a valid one.
                //check and make sure coords are not out of the grid
                // while (!this.Grid.CoordInBounds(changedX, changedY) ||
                //     this.Grid.IsVisited(changedX, changedY))
                // {
                //     if (failedAttempts >= this.DirectionsCount)
                //     {
                //         int[] backtracked = this.Backtrack();

                //         if (backtracked.Length < 2)
                //         {
                //             goto done;
                //         }

                //         currentCell[0] = backtracked[0];
                //         currentCell[1] = backtracked[1];

                //         Program.Debug($"joining direction {Program.DirectionName((CellWallFlag) backtracked[4])}, coords: {backtracked[0]}, {backtracked[1]} and {backtracked[2]}, {backtracked[3]}");

                //         this.Grid.SetDirectionsAvailableBetweenTwo(MazeGrid.GetOppositeSide((uint) backtracked[4]), new int[] {backtracked[0], backtracked[1]}, new int[] {backtracked[2], backtracked[3]});
                //         failedAttempts = 0;
                //     }

                //     direction = shuffledDirections[failedAttempts];
                //     change = shuffledDirectionChanges[failedAttempts];
                //     changedX = currentCell[0] + change[0];
                //     changedY = currentCell[1] + change[1];
                //     failedAttempts++;
                // }

                if (!this.Grid.IsValidAndNotVisited(changedX, changedY))
                {
                    bool found = false;

                    do
                    {
                        found = false;

                        for (int i = 0; i < shuffledDirections.Length && !found; i++)
                        {
                            direction = shuffledDirections[i];
                            change = shuffledDirectionChanges[i];
                            changedX = currentCell[0] + change[0];
                            changedY = currentCell[1] + change[1];

                            if (this.Grid.IsValidAndNotVisited(changedX, changedY))
                            {
                                Program.Debug($"found valid coord at {changedX}, {changedY} (I:{this.Grid.CoordInBounds(changedX, changedY)} V:{this.Grid.IsVisited(changedX, changedY)})");
                                found = true;
                            }
                        }

                        if (!found)
                        {
                            int[] backtracked = this.Backtrack();

                            if (backtracked.Length < 2)
                            {
                                goto done;
                            }

                            changedX = backtracked[0];
                            changedY = backtracked[1];
                            currentCell[0] = backtracked[2];
                            currentCell[1] = backtracked[3];

                            Program.Debug($"backtracked to X:{currentCell[0]} Y:{currentCell[1]}");

                            if (this.Grid.IsValidAndNotVisited(changedX, changedY))
                            {
                                Program.Debug($"backtracked and found valid coord at {changedX}, {changedY} (I:{this.Grid.CoordInBounds(changedX, changedY)} V:{this.Grid.IsVisited(changedX, changedY)})");
                                direction = (CellWallFlag) backtracked[4];
                                found = true;
                            }
                        }
                    }
                    while (!found);
                }

                int[] nextCellCoords = new int[] {changedX, changedY};
                Program.Debug($"current: X: {currentCell[0]} Y: {currentCell[1]} (V: {this.Grid.IsVisited(currentCell)}, I: {this.Grid.CoordInBounds(currentCell)} (!V)I: {this.Grid.IsValidAndNotVisited(currentCell)}); next: X: {changedX} Y: {changedY} (V: {this.Grid.IsVisited(currentCell)}, I: {this.Grid.CoordInBounds(currentCell)} (!V)I: {this.Grid.IsValidAndNotVisited(currentCell)}), direction: {Program.DirectionName(direction)}".Replace("True", "Y").Replace("False", "N"));
                this.Visit(nextCellCoords);
                //this.Grid.SetDirectionsAvailableAndUpdateAdjacent(currentCell[0], currentCell[1], (uint) direction);
                this.Grid.SetDirectionsAvailableBetweenTwo((uint) direction, currentCell, nextCellCoords);

                currentCell[0] = changedX;
                currentCell[1] = changedY;
                this?.Render();
            }

            done:
                ;
            
            Program.Debug("done genning");
            Program.Debug($"entrance: {String.Join(", ", this.MazeEntrance)} exit: {String.Join(", ", this.MazeExit)}");

            return this;
        }

        public CellWallFlag RandomDirection()
        {
            return MazeGrid.Directions[this.RNG.Next(MazeGrid.Directions.Length - 1)];
        }

        public int[] Backtrack()
        {
            Random rng = new Random();
            int[] prevCoords;
            int[] coords = new int[Program.Dimensions];

            //check to see if visited still has cells in it - if it's empty, we're done generating the maze
            if (this.Visited.Count > 0)
            {
                prevCoords = this.Visited.Pop();

                CellWallFlag[] directions = this.GetShuffledDirections();
                int[][] shuffledDirections = this.GetDirectionChangesFromDirections(directions);

                for (int i = 0; i < directions.Length; i++)
                {
                    coords = new int[] {prevCoords[0] + shuffledDirections[i][0], prevCoords[1] + shuffledDirections[i][1], prevCoords[0], prevCoords[1], (int) directions[i]};

                    if (this.Grid.IsValidAndNotVisited(coords[0], coords[1])) 
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

        public void Visit(params int[] coords)
        {
            this.Grid.MarkVisited(coords);
            this.Visited.Push(coords);
        }

        public int[] PickRandomCoord()
        {
            return new int[] {this.RNG.Next(this.Grid.Width - 1), this.RNG.Next(this.Grid.Height - 1)};
        }

        //well if this code isn't just an example of why you should follow KISS, I don't know what is
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

        
        public CellWallFlag[] GetShuffledDirections()
        {
            return MazeGrid.Directions.OrderBy((x) => RNG.Next(2)).ToArray();
        }

        public int[][] GetDirectionChangesFromDirections(CellWallFlag[] flags)
        {
            return flags.Select((x) => MazeGrid.GetXYChangeForDirection(x)).ToArray();
        }

        public int[][] GetShuffledDirectionChanges()
        {
            return MazeGrid.Directions.OrderBy((x) => RNG.Next(2)).Select((x) => MazeGrid.GetXYChangeForDirection(x)).ToArray();
        }
    }
}