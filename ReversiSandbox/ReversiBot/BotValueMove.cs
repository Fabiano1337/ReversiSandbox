using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReversiSandbox.ReversiBot
{
    public class BotValueMove : Bot
    {
        // Value Moves based on number of enemy tiles captured

        public override Position generateMove(int[] gameField, int[] moves, int player)
        {
            Position bestMove = new Position() { x = moves[0], y = moves[1] };
            int bestScore = 0;

            for (int i = 0; i < moves.Length / 2; i++)
            {
                var x = moves[i * 2];
                var y = moves[i * 2 + 1];
                Position move = new Position() { x = x, y = y };

                int score = ReversiGame.getTilesCaptured(gameField, move, player);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }

            return bestMove;
        }
    }
}
