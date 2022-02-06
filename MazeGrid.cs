using System.Linq;

namespace MazeGenerator
{
    //all cells have all walls initially - the walls are not rendered unless Visited is true
    //NOTE: +x is right on the screen and +y is up on the screen. y is inverted when each cell is drawn
    public class MazeGrid
    {
        public static CellWallFlag[] Directions = new CellWallFlag[] {CellWallFlag.North, CellWallFlag.South, CellWallFlag.East, CellWallFlag.West, CellWallFlag.Up, CellWallFlag.Down};
        //each uint is a bit flag corresponding to if something is visited and if it lacks walls in that particular bit.
        //could be done with bytes and stuff for memory efficiency but that limits what this can do
        //Bits:
        //1 - Visited
        //2 - Has no North wall (+0, +1)
        //4 - Has no South wall (+0, -1)
        //8 - Has no East wall (+1, +0)
        //16 - Has no West wall (-1, +0)
        //[UNIMPLEMENTED]
        //32 - Has no Up wall [3D]
        //64 - Has no Down Wall [3D]
        //128 - Has no +w wall [4D]
        //256 - Has no -w wall [4D]
        private uint[,] Grid;
        public int[] Sizes;
        public int Width {get => this.Sizes[0];}
        public int Height {get => this.Sizes[1];}

        //where sizes[n] is the dimension size of dimension 3+n given sizes is 0 indexed (first in sizes is 3D, second is 4D, etc.)
        public MazeGrid(int width, int height, params int[] sizes)
        {
            this.Sizes = new int[sizes.Length + 2];

            this.Sizes[0] = width;
            this.Sizes[1] = height;

            for (int i = 0; i < sizes.Length; i++)
            {
                this.Sizes[i + 2] = sizes[i];
            }

            //TODO: higher dimensions
            this.Grid = new uint[width, height];
        }

        public void MarkVisited(int x, int y)
        {
            this.Grid[x, y] |= 1;
        }

        public void SetWallsToOff(int x, int y, uint wall)
        {
            this.Grid[x, y] |= wall;
        }

        public void SetWallsToOffAndUpdateAdjacent(int x, int y, uint wall)
        {
            SetWallsToOff(x, y, wall);

            foreach (CellWallFlag direction in Directions)
            {
                //if the wall is being removed here
                if ((wall & (uint) direction) > 0)
                {
                    //get the cell in that direction
                    int[] directionArr = GetXYChangeForDirection(direction);

                    //don't let it generate off the grid because that will cause errors
                    if (directionArr[0] < 0 || directionArr[1] < 0) continue;

                    //and remove the wall opposite to the wall that was removed at x, y
                    //e.g if I remove the north one first, the one north to this cell will lose the south wall
                    SetWallsToOff(x + directionArr[0], y + directionArr[1], GetOppositeSide((uint) direction));
                }
            }
        }

        public void SetWallsToOn(int x, int y, uint wall)
        {
            //why do I need to cast this to a uint first? does the invert operator only return ints or something?
            this.Grid[x, y] &= (uint) (~wall);
        }

        public bool DoAnyWallsNotExist(int x, int y, uint wall)
        {
            return (this.Grid[x, y] & wall) > 0;
        }

        public bool DoAllWallsNotExist(int x, int y, uint wall)
        {
            //TODO: confirm if all of multiple bytes in wall exist
            return (this.Grid[x, y] & wall) == wall; // > 0
        }

        public bool CoordInBounds(int x, int y, params int[] zWAndUp)
        {
            if (x < 0 || x >= this.Width || y < 0 || y >= this.Height)
            {
                return false;
            }

            for (int i = 0; i < zWAndUp.Length; i++)
            {
                if (zWAndUp[i] < 0 || zWAndUp[i] >= this.Sizes[i])
                {
                    return false;
                }
            }

            return true;
        }
        
        public bool IsVisited(int x, int y)
        {
            return (this.Grid[x, y] & (uint) 1) > 0;
        }

        public static uint GetOppositeSide(uint side)
        {
            //roughly 10101010101010101010101 repeated for 64 bits (in case I make it a long for some insane reason). if you
            //want the opposite it's literally the same but A instead of 5.

            //check if it's a certain set of bits
            if ((side & 0x5555555555555555) > 0)
            {
                //if it is, shift one way, getting the opposite
                return side >> 1;
            }
            else
            {
                //otherwise shift the other way
                return side << 1;
            }
        }

        public static int[] GetXYChangeForDirection(CellWallFlag flag)
        {
            //TODO: higher dimensions
            //x, y
            int[] change = new int[2] {0, 0};

            switch (flag)
            {
                case CellWallFlag.North:
                    change[1] = 1;
                    break;
                
                case CellWallFlag.East:
                    change[0] = 1;
                    break;
                
                case CellWallFlag.South:
                    change[1] = -1;
                    break;

                case CellWallFlag.West:
                    change[0] = -1;
                    break;
            }

            return change;
        }

        public uint this[int x, int y]
        {
            get {return this.Grid[x, y];}
        }
    }
}