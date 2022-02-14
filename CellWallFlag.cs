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
        //4D - apparently these are what they're called?
        Ana = 128,
        Kata = 256
    }
}