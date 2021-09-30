namespace MazeGenerator
{
    //all cells have all walls initially - the walls are not rendered unless Visited is true
    public class MazeGrid
    {
        //int is a flag
        //Bits:
        //1 - Visited
        //2 - Has no North wall
        //4 - Has no East wall
        //8 - Has no South wall
        //16 - Has no West wall
        public byte[,] Grid;

        public MazeGrid(int width, int height)
        {
            this.Grid = new byte[width, height];
        }

        public void SetWallsToOff(int x, int y, byte wall)
        {
            this.Grid[x, y] |= wall;

            //TODO set wall of adjacent cell in that direction to off too
        }

        public void SetWallsToOn(int x, int y, byte wall)
        {
            //why do I need to cast this to a byte first? does the invert operator only return ints or something?
            this.Grid[x, y] &= (byte) (~wall);
        }

        public bool DoWallsNotExist(int x, int y, byte wall)
        {
            return (this.Grid[x, y] & wall) > 0;
        }

        public byte GetOppositeSide(byte side)
        {
            if (side > 4)
            {
                return (byte) (side << 2);
            }
            else
            {
                return (byte) (side >> 2);
            }
        }
    }
}