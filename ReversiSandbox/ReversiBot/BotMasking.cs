using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReversiSandbox.ReversiBot
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

        public override Position generateMove(int[] gameField, int[] moves, int player)
        {
            Position bestMove = new Position() { x = moves[0], y = moves[1] };
            float bestScore = 0;

            for (int i = 0; i < moves.Length / 2; i++)
            {
                var x = moves[i * 2];
                var y = moves[i * 2 + 1];
                Position move = new Position() { x = x, y = y };

                float score = weights[x, y] * ReversiGame.getTilesCaptured(gameField, move, player);
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
