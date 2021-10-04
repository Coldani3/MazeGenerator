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

            }
            else
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
                //|      | [4]
                //|      | [5]
                //9 8 7 6
                //9 7 5 3
                //BOTTOM LINE
                //3 = height - 1
                // if linePos > width + (height - 1)
                // x -= linePos - (width + (height - 1))
                // x = Clamp(linePos, 0, width - 1) - Clamp((linePos - (width + (height - 1))), 0, width - 1)
                for (int i = linePos; i > 0; i--)
                {
                    
                }

            }
        }

        public int[] PickRandomCoord()
        {
            return new int[] {this.RNG.Next(this.Grid.Width - 1), this.RNG.Next(this.Grid.Height - 1)};
        }
    }
}