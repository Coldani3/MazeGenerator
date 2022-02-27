using System.Linq;
using System;
using System.Collections.Generic;

namespace MazeGenerator
{
    //all cells have all walls initially - the walls are not rendered unless Visited is true
    //NOTE: +x is right on the screen and +y is up on the screen. y is inverted when each cell is drawn
    /**
     * The grid that actually stores the information. When dealing with coordinate array params, remember 
     * that it will always go x, y, z, w, and then incrementally higher dimensions from there
     */
    public class MazeGrid
    {
        public static CellWallFlag[] AllDirections = new CellWallFlag[] {CellWallFlag.North, CellWallFlag.South, CellWallFlag.East, CellWallFlag.West, CellWallFlag.Up, CellWallFlag.Down, CellWallFlag.Ana, CellWallFlag.Kata};
        public CellWallFlag[] Directions = new CellWallFlag[] {CellWallFlag.North, CellWallFlag.South, CellWallFlag.East, CellWallFlag.West/*, CellWallFlag.Up, CellWallFlag.Down*/};
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
        //indexing is computed as it goes so we can choose between dimensions
        public uint[] Grid;
        public int[] Sizes;
        public int Dimensions {get => this.Sizes.Length;}
        public int Width {get => this.Sizes[0];}
        public int Height {get => this.Sizes[1];}
        public int Depth {
            get {
                if (this.Sizes.Length > 2) 
                {
                    return this.Sizes[2];
                }
                else
                {
                    return 0;
                }
            }
        }
        public int HyperDepth {
            get {
                if (this.Sizes.Length > 3) 
                {
                    return this.Sizes[3];
                }
                else
                {
                    return 0;
                }
            }
        }

        //where sizes[n] is the dimension size of dimension 3+n given sizes is 0 indexed (first in sizes is 3D, second is 4D, etc.)
        public MazeGrid(params int[] sizes)
        {
            if (sizes.Length < 2)
            {
                throw new ArgumentException("Insufficient dimensions passed (you can't have a 1D maze)");
            }

            this.Sizes = sizes;

            this.Directions = AllDirections.Take(sizes.Length * 2).ToArray();

            int size = 1;

            this.Sizes.ToList().ForEach(x => {
                if (x > 0)
                {
                    size *= x;
                }
            });
            
            this.Grid = new uint[size];
        }

        public void MarkVisited(params int[] coords)
        {
            this[coords] |= 1;
        }

        public void SetDirectionsToAvailable(uint wall, params int[] coords)
        {
            this[coords] |= wall;
        }

        public void SetDirectionsAvailableBetweenTwo(uint direction1, int[] coords1, int[] coords2)
        {
            SetDirectionsToAvailable(direction1, coords1);
            SetDirectionsToAvailable(GetOppositeSide(direction1), coords2);
        }

        public void SetWallsToOn(uint wall, params int[] coords)
        {
            //why do I need to cast this to a uint first? does the invert operator only return ints or something?
            this[coords] &= (uint) (~wall);
        }

        public bool AreAnyDirectionsAvailable(uint wall, params int[] coords)
        {
            return (this[coords] & wall) > 0;
        }

        public bool AreNoDirectionsAvailable(params int[] coords)
        {
            return (this[coords] >> 1) == 0;
        }

        public bool AreAllDirectionsAvailable(uint wall, params int[] coords)
        {
            return (this[coords] & wall) == wall; // > 0
        }

        public bool CoordInBounds(params int[] coords)
        {
            for (int i = 0; i < coords.Length; i++)
            {
                if (coords[i] < 0 || coords[i] >= this.Sizes[i])
                {
                    return false;
                }
            }

            return true;
        }
        
        public bool IsVisited(params int[] coords)
        {
            return (this[coords] & (uint) 1) > 0;
        }

        public bool IsValidAndNotVisited(params int[] coords)
        {
            return this.CoordInBounds(coords) && !this.IsVisited(coords);
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
            int[] change = ArrayOfMinSize(4);//new int[4] {0, 0, 0, 0};

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
                
                case CellWallFlag.Up:
                    change[2] = 1;
                    break;

                case CellWallFlag.Down:
                    change[2] = -1;
                    break;
                
                case CellWallFlag.Ana:
                    change[3] = 1;
                    break;

                case CellWallFlag.Kata:
                    change[3] = -1;
                    break;
            }

            return change.Take((int) Math.Floor(Math.Log2((int) flag))).ToArray();
        }

        public uint this[params int[] coords]
        {
            get {
                int[] fixedCoords = ArrayOfMinSize(4, coords);

                return this.Grid[(this.Height * this.Width * this.Depth * fixedCoords[3]) + (this.Height * this.Width * fixedCoords[2]) + (this.Height * fixedCoords[1]) + fixedCoords[0]];
            }

            set {
                int[] fixedCoords = ArrayOfMinSize(4, coords);

                this.Grid[(this.Height * this.Width * this.Depth * fixedCoords[3]) + (this.Height * this.Width * fixedCoords[2]) + (this.Height * fixedCoords[1]) + fixedCoords[0]] = value;
            }
        }

        public static int[] ArrayOfMinSize(int size, params int[] values)
        {
            int[] ints = new int[size];

            for (int i = 0; i < size; i++)
            {
                if (i < values.Length)
                {
                    ints[i] = values[i];
                }
                else
                {
                    ints[i] = 0;
                }
            }

            return ints;
        }
    }
}