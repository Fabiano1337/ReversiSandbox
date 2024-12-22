using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReversiSandbox
{
    public struct Move
    {
        public Position pos;
        public int score;
    }
    public class BotNeo : Bot
    {
        // Value Moves based on number of enemy tiles captured and looks x iterations into the future

        int iterations;

        public BotNeo(int i)
        {
            iterations = i;
        }

        public override Position generateMove(int[] gameField, int[] moves, int player)
        {
            return new Position() { x = 0, y = 0 };
            /*var moves = game.getPossibleMoves();
            //if (moves.Count == 1) return moves[0];
            Player p = game.curPlayer;
            Player[,] gameField = game.gameField;

            //Player[,] gameFields;

            Move m = (Move)getBestMove(game.gameField, game.curPlayer, iterations, true);

            //Console.WriteLine(m.score.ToString());
            //Console.WriteLine(moves.Count.ToString());

            return m.pos;
            /*while (true)
            {
                for (int i = 0; i < moves.Count; i++)
                {
                    int score = ReversiGame.getTilesCaptured(gameField, moves[i], game.curPlayer);
                    Player[,] newGameField = ReversiGame.reverse(gameField, p, moves[i].x, moves[i].y);
                }
            }*/

            //return bestMove;
        }

        /*private Player[,] cloneArray(Player[,] toClone)
        {
            Player[,] newArray = new Player[ReversiGame.gameSize, ReversiGame.gameSize];

            for (int x = 0; x < ReversiGame.gameSize; x++)
            {
                for (int y = 0; y < ReversiGame.gameSize; y++)
                {
                    newArray[x,y] = toClone[x,y];
                }
            }

            return newArray;
        }

        private Move? getBestMove(Player[,] gameField, Player p, int itr, bool self)
        {
            if (itr == 0) return null;

            var moves = ReversiGame.getPossibleMoves(gameField, p);

            if (moves.Count==0) return null;

            int bestScore = 0;
            Move bestMove = new Move();
            bestMove.pos = moves[0];

            Player[,] gameFieldBackup = gameField;
            Player playerBackup = p;
            bool selfBackup = self;

            for (int i = 0; i < moves.Count; i++)
            {
                Move m;
                gameField = cloneArray(gameFieldBackup);
                p = playerBackup;
                self = selfBackup;

                int score = ReversiGame.getTilesCaptured(gameField, moves[i], p);
                Player[,] newGameField = ReversiGame.reverse(gameField, p, moves[i].x, moves[i].y);

                p = ReversiGame.getOppositePlayer(p);
                self = !self;

                Move? mPos = getBestMove(newGameField, p, itr - 1,self);

                if (mPos == null) continue;

                m = (Move)mPos;

                if(!self)
                {
                    score += m.score;
                    if (score > bestScore)
                    {
                        m.pos = moves[i];
                        m.score = score;
                        bestScore = score;
                        bestMove = m;
                    }
                }
                else
                {
                    score -= m.score;
                    if (score < bestScore)
                    {
                        m.pos = moves[i];
                        m.score = score;
                        bestScore = score;
                        bestMove = m;
                    }
                }
            }

            return bestMove;
        }*/
    }

}
