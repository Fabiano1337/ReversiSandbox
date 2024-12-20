using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReversiSandbox
{
    public class BotRandom : Bot
    {
        public override Position generateMove(ReversiGame game)
        {
            var moves = game.getPossibleMoves();
            Random r = new Random();
            int moveIndex = r.Next(0, moves.Count);

            return moves[moveIndex];
        }
    }
}
