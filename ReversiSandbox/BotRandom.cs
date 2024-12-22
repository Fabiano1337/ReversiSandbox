using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReversiSandbox
{
    public class BotRandom : Bot
    {
        public override Position generateMove(int[] gameField, int[] moves, int player)
        {
            Position move;

            Random r = new Random();
            int moveIndex = r.Next(0, moves.Count() / 2);

            move = new Position();
            move.x = moves[moveIndex*2];
            move.y = moves[moveIndex*2+1];

            return move;
        }
    }
}
