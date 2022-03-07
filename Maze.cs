using System.Collections.Generic;
using System.Linq;
using System.IO;
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
        //TODO: higher and lower dimensionsW
        public Action RenderMethod = null;
        public Action<string> DebugMethod = null;
        public Action<int[]> UpdateRendererHigherDim = null;

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
            this.RenderMethod = render;
            return this;
        }

        public Maze SetDebug(Action<string> debug)
        {
            this.DebugMethod = debug;
            return this;
        }

        public void Debug(string message)
        {
            if (this.DebugMethod != null)
            {
                this.DebugMethod(message);
            }
        }

        public void Render()
        {
            if (this.RenderMethod != null)
            {
                this.RenderMethod();
            }
        }

        public Maze SetUpdateRendererHigherDim(Action<int[]> updateRenderer)
        {
            this.UpdateRendererHigherDim = updateRenderer;
            return this;
        }

        public Maze Generate()
        {
            //pick entrance and exit
            if (this.AllowNonWallEntrance)
            {
                this.MazeEntrance = this.RandomCoords(this.Grid.Dimensions); 
            }
            else
            {
                this.MazeEntrance = this.SelectRandomEdgeOfMaze();
            }

            if (this.AllowNonWallExit)
            {
                int[] coords = this.RandomCoords(this.Grid.Dimensions); 

                //prevent maze entrance from being the same as the exit
                while (CoordsMatch(coords, this.MazeEntrance)) //coords[0] == this.MazeEntrance[0] && coords[1] == this.MazeEntrance[1] && DistanceBetween(this.MazeEntrance, coords) < Program.MinDistanceBetweenEntranceAndExit)
                {
                    coords = RandomCoords(this.Grid.Dimensions); 
                }

                this.MazeExit = coords;
            }
            else
            {
                int[] coords = this.SelectRandomEdgeOfMaze();

                //prevent maze entrance from being the same as the exit
                while (CoordsMatch(coords, this.MazeEntrance))
                {
                    coords = this.SelectRandomEdgeOfMaze();
                }

                this.MazeExit = coords;
            }

            this.Visit(this.MazeEntrance);

            //begin generation

            int[] currentCell = new int[this.Grid.Dimensions];
            Array.Copy(this.MazeEntrance, currentCell, this.Grid.Dimensions);

            int[] changedCoords = new int[this.Grid.Dimensions];

            while (this.Visited.Count > 0)
            {
                CellWallFlag direction = this.RandomDirection();
                int[] change = MazeGrid.GetXYChangeForDirection(direction, this.Grid.Dimensions).Take(this.Grid.Dimensions).ToArray();

                CellWallFlag[] shuffledDirections = this.GetShuffledDirections();
                int[][] shuffledDirectionChanges = this.GetDirectionChangesFromDirections(shuffledDirections);

                changedCoords = AddCoords(currentCell, change);

                //check to make sure that the coordinates are not visited and in bound
                if (!this.Grid.IsValidAndNotVisited(changedCoords))
                {
                    bool found = false;

                    do
                    {
                        found = false;

                        //iterate through all directions until we find a valid direction
                        for (int i = 0; i < shuffledDirections.Length && !found; i++)
                        {
                            direction = shuffledDirections[i];
                            change = shuffledDirectionChanges[i];
                            changedCoords = AddCoords(currentCell, change);

                            if (this.Grid.IsValidAndNotVisited(changedCoords))
                            {
                                this?.Debug($"found valid coord at {String.Join(", ", changedCoords)} (I:{this.Grid.CoordInBounds(changedCoords)} V:{this.Grid.IsVisited(changedCoords)})");
                                found = true;
                            }
                        }

                        //if there are no valid directions, backtrack and find a valid direction
                        if (!found)
                        {
                            int[] backtracked = this.Backtrack();

                            //if we receive the special "done" result from backtrack, exit
                            if (backtracked.Length < 2)
                            {
                                this?.Debug($"nowhere left to backtrack to: {String.Join(", ", backtracked)}");
                                goto done;
                            }

                            //otherwise, set current cell to the new branch off point and update changed coords to match

                            changedCoords = backtracked.Take(this.Grid.Dimensions).ToArray();
                            currentCell = backtracked.Skip(this.Grid.Dimensions).Take(this.Grid.Dimensions).ToArray();

                            this?.Debug($"backtracked to X:{currentCell[0]} Y:{currentCell[1]}");

                            //check if this is valid too and if so, update the direction and leave the loop
                            if (this.Grid.IsValidAndNotVisited(changedCoords))
                            {
                                this?.Debug($"backtracked and found valid coord at {changedCoords[0]}, {changedCoords[1]} (I:{this.Grid.CoordInBounds(changedCoords)} V:{this.Grid.IsVisited(changedCoords)})");
                                direction = (CellWallFlag) backtracked.Last();
                                found = true;
                            }
                        }
                    }
                    while (!found);
                }

                //visit the new cell and update the wall
                int[] nextCellCoords = changedCoords;
                this?.Debug($"current: {String.Join(", ", currentCell)} (V: {this.Grid.IsVisited(currentCell)}, I: {this.Grid.CoordInBounds(currentCell)} (!V)I: {this.Grid.IsValidAndNotVisited(currentCell)}); next: X: {changedCoords[0]} Y: {changedCoords[1]} (V: {this.Grid.IsVisited(currentCell)}, I: {this.Grid.CoordInBounds(currentCell)} (!V)I: {this.Grid.IsValidAndNotVisited(currentCell)}), direction: {Program.DirectionName(direction)}".Replace("True", "Y").Replace("False", "N"));
                this.Visit(nextCellCoords);
                this.Grid.SetDirectionsAvailableBetweenTwo((uint) direction, currentCell, nextCellCoords);

                if (nextCellCoords.Length > 2 && this.UpdateRendererHigherDim != null)
                {
                    this?.UpdateRendererHigherDim(nextCellCoords.Skip(2).ToArray());
                }

                //set the current cell to the new changed coords
                currentCell = changedCoords;
                this?.Render();
            }

            done:
                ;

            if (this.AllowNonWallEntrance && CoordsMatch(changedCoords, this.MazeEntrance))
            {
                //this popped up as a bug but it's actually unintentionally genius for making sure the exit and entrance aren't near each other so I'm keeping it here
                this.MazeEntrance = changedCoords;
            }
            
            this?.Debug("done genning");
            this?.Debug($"entrance: {String.Join(", ", this.MazeEntrance)} exit: {String.Join(", ", this.MazeExit)}");

            return this;
        }

        public CellWallFlag RandomDirection()
        {
            return this.Grid.Directions[this.RNG.Next(this.Grid.Directions.Length - 1)];
        }

        public int[] Backtrack()
        {
            Random rng = new Random();
            int[] prevCoords;
            List<int> coords = new List<int>();

            //check to see if visited still has cells in it - if it's empty, we're done generating the maze
            if (this.Visited.Count > 0)
            {
                prevCoords = this.Visited.Pop();

                CellWallFlag[] directions = this.GetShuffledDirections();
                int[][] shuffledDirections = this.GetDirectionChangesFromDirections(directions);

                for (int i = 0; i < directions.Length; i++)
                {
                    int[] changedCoords = AddCoords(prevCoords, shuffledDirections[i]);

                    coords.AddRange(changedCoords);
                    coords.AddRange(prevCoords);
                    coords.Add((int) directions[i]);

                    if (this.Grid.IsValidAndNotVisited(coords.Take(this.Grid.Dimensions).ToArray())) 
                    {
                        return coords.ToArray();
                    }
                    else
                    {
                        coords.Clear();
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

        public void WriteToFile(string fileName)
        {
            this?.Debug($"saving to file {fileName}");

            //https://github.com/Coldani3/MazeFormat
            //TODO: figure out data size dynamically
            int size = 1;
            int counter = 0;

            for (int i = 0; i < this.Grid.Sizes.Length; i++)
            {
                size *= this.Grid.Sizes[i];
            }

            //data size definition
            size += this.Grid.Sizes.Sum(x => x.ToString().Length) + (this.Grid.Sizes.Length - 1) + 3;

            //maze entrance and exit definition
            for (int i = 0; i < this.MazeEntrance.Length; i++)
            {
                size += this.MazeEntrance[i].ToString().Length;
            }

            for (int i = 0; i < this.MazeExit.Length; i++)
            {
                size += this.MazeExit[i].ToString().Length;
            }

            //commas
            size += (this.MazeEntrance.Length - 1) + (this.MazeExit.Length - 1) + 6;

            int sliceSize = this.Grid.Width.ToString().Length + this.Grid.Height.ToString().Length + 3;

            //higher dimension coordinates definition
            if (this.Grid.Depth > 0)
            {
                for (int i = 0; i < this.Grid.Depth; i++)
                {
                    size += (i.ToString().Length) + 2 + sliceSize;

                    if (this.Grid.HyperDepth > 0)
                    {
                        for (int j = 0; j < this.Grid.HyperDepth; j++)
                        {
                            size += (j.ToString().Length) + 1 + sliceSize;
                        }
                    }
                }
            }

            byte[] data = new byte[size + 50];

            this.PushStringAsBytes(ref data, ref counter, "8{" + String.Join(',', this.Grid.Sizes) + "}");

            int w = 0;
            int z = 0;

            do
            {
                do
                {
                    //the sizes are stored as ascii instead of raw data in order to allow for large mazes (ie. larger than 255)
                    this.PushStringAsBytes(ref data, ref counter, $"[{this.Grid.Width}|{this.Grid.Height}]");
                    //we never do anything with offset (all mazes are cubes/hypercubes/etc.) so we omit it

                    if (this.Grid.Depth > 0)
                    {
                        string higherDimCoords = $"\\{z}";

                        if (this.Grid.HyperDepth > 0)
                        {
                            higherDimCoords += $"|{w}";
                        }

                        higherDimCoords += "/";

                        this.PushStringAsBytes(ref data, ref counter, higherDimCoords);
                    }

                    this.PushByteArray(ref data, ref counter, DataForHigherDimCoords(z, w));
                    z++;
                }
                while (z < this.Grid.Depth);

                z = 0;

                w++;
            }
            while (w < this.Grid.HyperDepth);

            this.PushStringAsBytes(ref data, ref counter, $"+({String.Join(',', this.MazeEntrance)})({String.Join(',', this.MazeExit)})+");

            File.WriteAllBytes(fileName, data);
        }

        private void PushByte(ref byte[] data, ref int counter, byte toPush)
        {
            data[counter] = toPush;
            counter++;
        }

        private void PushByteArray(ref byte[] data, ref int counter, byte[] toPush)
        {
            foreach (byte toPushByte in toPush)
            {
                this.PushByte(ref data, ref counter, toPushByte);
            }
        }

        private void PushCharAsByte(ref byte[] data, ref int counter, char toPush)
        {
            this.PushByte(ref data, ref counter, (byte) toPush);
        }

        private void PushStringAsBytes(ref byte[] data, ref int counter, string toPush)
        {
            foreach (char character in toPush)
            {
                this.PushCharAsByte(ref data, ref counter, character);
            }
        }

        public byte[] DataForHigherDimCoords(params int[] higherDimCoords)
        {
            byte[] outBytes = new byte[this.Grid.Width * this.Grid.Height];
            int counter = 0;
            int[] coords = new int[higherDimCoords.Length + 2];

            for (int i = 2; i < higherDimCoords.Length; i++)
            {
                coords[i] = higherDimCoords[i - 2];
            }

            for (int x = 0; x < this.Grid.Width; x++)
            {
                coords[0] = x;

                for (int y = 0; y < this.Grid.Height; y++)
                {
                    coords[1] = y;

                    //once the visited byte is stripped, 4 dimensions or less will always fit each cell in a byte
                    if (this.Grid.Dimensions <= 4)
                    {
                        //we don't care about the visited byte here, so get rid of it
                        outBytes[counter] = (byte) ((this.Grid[coords] >> 1) & 0b11111111);
                    }
                    else
                    {
                        /*
                          you will have to split the data between multiple bytes here somehow. I'd recommend 
                          multiples of 8 for your overall maze data size for simplicity's sake but
                          you can calculate it between bytes here if you really need to save space.
                        */

                    }
                    
                    counter++;
                }
            }

            return outBytes;
        }

        public int[] PickRandomCoord()
        {
            return new int[] {this.RNG.Next(this.Grid.Width - 1), this.RNG.Next(this.Grid.Height - 1)};
        }

        public int[] SelectRandomEdgeOfMaze()
        {
            int y = this.RNG.Next(2) == 0 ? 0 : this.Grid.Height - 1;

            int[] outCoords = new int[this.Grid.Dimensions];
            outCoords[1] = y;
            
            for (int i = 0; i < this.Grid.Dimensions; i++)
            {
                if (i != 1)
                {
                    outCoords[i] = this.RNG.Next(this.Grid.Sizes[i]);
                }
            }

            return outCoords;
        }

        
        public CellWallFlag[] GetShuffledDirections()
        {
            return this.Grid.Directions.OrderBy((x) => RNG.Next(2)).ToArray();
        }

        public int[][] GetDirectionChangesFromDirections(CellWallFlag[] flags)
        {
            return flags.Select((x) => MazeGrid.GetXYChangeForDirection(x, this.Grid.Dimensions).Take(this.Grid.Dimensions).ToArray()).ToArray();
        }

        public int[][] GetShuffledDirectionChanges()
        {
            return this.Grid.Directions.OrderBy((x) => RNG.Next(2)).Select((x) => MazeGrid.GetXYChangeForDirection(x, this.Grid.Dimensions).Take(this.Grid.Dimensions).ToArray()).ToArray();
        }

        public static int DistanceBetween(int[] coords1, int[] coords2)
        {
            int pythagoreanSum = 0;

            for (int i = 0; i < coords1.Length; i++)
            {
                pythagoreanSum += Math.Abs(coords1[i] - coords2[i])^2;
            }

            return (int) Math.Floor(Math.Sqrt(pythagoreanSum));
        }

        public int[] RandomCoords(int arrayLength)
        {
            int[] coords = new int[arrayLength];

            for (int i = 0; i < arrayLength; i++)
            {
                coords[i] = this.RNG.Next(this.Grid.Sizes[i]); 
            }

            return coords;
        }

        public static bool CoordsMatch(int[] coords1, int[] coords2)
        {
            if (coords1.Length != coords2.Length)
            {
                return false;
            }
            
            for (int i = 0; i < coords1.Length; i++)
            {
                if (coords1[i] != coords2[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static int[] AddCoords(int[] coords1, int[] coords2)
        {
            
            int minLength = new int[] {coords1.Length, coords2.Length}.Min();
            int[] added = new int[minLength];

            for (int i = 0; i < coords2.Length; i++)
            {
                added[i] = coords1[i] + coords2[i];
            }

            return added;
        }
    }
}