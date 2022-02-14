namespace MazeGenerator
{
    public enum CellWallFlag
    {
        //2D
        North = 2,
        South = 4,
        East = 8,
        West = 16,
        //3D
        Up = 32,
        Down = 64,
        //4D - apparently these are what they're called? Ana is treated as +w and Kata as -w as Ana means *up* toward and Kata means *down* from
        Ana = 128,
        Kata = 256
    }
}