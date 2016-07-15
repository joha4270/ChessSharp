using System;
using System.Linq;

namespace Framework
{
    internal static class ChessUtil
    {
        internal static string[] AlgebraicNotation;
        static ChessUtil()
        {
            AlgebraicNotation = new string[120];

            for (int i = 0; i < 120; i++)
            {
                if (i < 20 || i > 99 || i % 10 == 0 || i % 10 == 9)
                {
                    AlgebraicNotation[i] = "++";
                }
                else
                {
                    int letter = (i % 10) - 1;
                    int number = (i / 10) - 1;

                    AlgebraicNotation[i] = ((char)('a' + letter)).ToString() + number;
                }
            }
        }

        internal static bool IsBeteen(int firstPos, int secondPos, int between)
        {
            //If they are overlapping it cannot be said to be between, and also it makes it useless for testing if moving to a speace blocks something
            if (firstPos == between || secondPos == between) return false; 
            int difference = firstPos - secondPos;

            int[] angles = { 1, 10, 9, 11 };
            //find "angle" 

            int angle = angles.FirstOrDefault(x => difference % x == 0 && Math.Abs(difference / x) <= 8);
            if (angle == 0) throw new ArgumentException("firstPos and lastPos should share a straight line on the chessboard");

            int diff2 = firstPos - between;

            if (diff2 % angle == 0)
            {
                int min = Math.Min(firstPos, secondPos);
                int max = Math.Max(firstPos, secondPos);

                return max > between && between > min;

            }
            return false;
        }

        internal static int AlgebaricToInternal(string alge)
        {
            if (alge == AlgebraicNotation[0]) return -1;

            for (int i = 0; i < 120; i++)
            {
                if (AlgebraicNotation[i] == alge) return i;
            }

            return -1;
        }
    }
}
