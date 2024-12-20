using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReversiSandbox
{
    public class BotMasking : Bot 
    {
        // Value Moves based on a weighted mask multiplied by the amount of tiles flipped

        float[,] weights =
            {   {16f,8f,8f,8f,8f,8f,8f,16f},
                {8f,1f,1f,1f,1f,1f,1f,8f},
                {8f,1f,4f,4f,4f,4f,1f,8f},
                {8f,1f,4f,1f,1f,4f,1f,8f},
                {8f,1f,4f,1f,1f,4f,1f,8f},
                {8f,1f,4f,4f,4f,4f,1f,8f},
                {8f,1f,1f,1f,1f,1f,1f,8f},
                {16f,8f,8f,8f,8f,8f,8f,16f}};

        public BotMasking()
        {
            
        }

        public BotMasking(float[,] weights)
        {
            this.weights = weights;
        }

        public override Position generateMove(ReversiGame game)
        {
            var moves = game.getPossibleMoves();
            //Dictionary<Position, int> scoreList = new Dictionary<Position, int>();
            Position bestMove = moves[0];
            float bestScore = 0;
            foreach (var move in moves)
            {
                float score = weights[move.x, move.y] * ReversiGame.getTilesCaptured(game.gameField, move, game.curPlayer);
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
