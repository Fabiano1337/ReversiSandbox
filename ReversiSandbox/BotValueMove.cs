using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReversiSandbox
{
    public class BotValueMove : Bot 
    {
        // Value Moves based on number of enemy tiles captured

        public override Position generateMove(ReversiGame game)
        {
            var moves = game.getPossibleMoves();
            //Dictionary<Position, int> scoreList = new Dictionary<Position, int>();
            Position bestMove = moves[0];
            int bestScore = 0;
            foreach (var move in moves)
            {
                int score = ReversiGame.getTilesCaptured(game.gameField, move, game.curPlayer);
                if(score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }

            return bestMove;
        }
    }
}
