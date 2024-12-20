using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReversiSandbox
{
    public enum Player
    {
        Empty,
        Human,
        Bot
    }
    public class ReversiGame
    {
        const int gameSize = 8;
        public Player curPlayer;
        Player[,] gameField;

        public ReversiGame()
        {
            gameField = new Player[gameSize, gameSize];
            for (int x = 0; x < gameSize; x++)
            {
                for (int y = 0; y < gameSize; y++)
                {
                    gameField[x, y] = Player.Empty;
                }
            }

            gameField[gameSize / 2 - 1,gameSize / 2 - 1] = Player.Bot;
            gameField[gameSize / 2 - 1,gameSize / 2] = Player.Human;
            gameField[gameSize / 2,gameSize / 2 - 1] = Player.Human;
            gameField[gameSize / 2,gameSize / 2] = Player.Bot;

            curPlayer = Player.Human;
        }

        public void setPlayer(Player p)
        {
            curPlayer = p;
        }

        public void switchPlayer()
        {
            curPlayer = getOppositePlayer(this);
        }

        public void move(Position pos)
        {
            reverse(pos.x, pos.y);
        }

        void reverse_dir(int x, int y, int dx, int dy)
        {
            if (!legal_dir(this, x, y, dx, dy)) return;

            while (true)
            {
                x += dx;
                y += dy;

                if (gameField[x,y] == Player.Empty) return;
                if (out_of_bounds(x, y)) return;
                if (gameField[x, y] == curPlayer) return;

                if (gameField[x, y] == Player.Human)
                    gameField[x, y] = Player.Bot;
                else if (gameField[x, y] == Player.Bot)
                    gameField[x, y] = Player.Human;
            }
        }

        void reverse(int x, int y)
        {
            Position pos = new Position { x = x, y = y };
            if (!isMoveValid(this, this.curPlayer, pos)) return;

            gameField[x, y] = curPlayer;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    reverse_dir(x, y, dx, dy);
                }
            }
        }

        public void printGameField()
        {
            Console.WriteLine(" |A|B|C|D|E|F|G|H|");
            for (int y = 0; y < gameSize; y++)
            {
                Console.Write((y + 1).ToString() + "|");
                for (int x = 0; x < gameSize; x++)
                {
                    if (gameField[x, y] == Player.Human)
                        Console.Write("X");
                    if (gameField[x, y] == Player.Bot)
                        Console.Write("O");
                    if (gameField[x, y] == Player.Empty)
                        Console.Write("_");

                    Console.Write("|");

                }
                Console.WriteLine();
            }
        }

        List<Position> getPossibleMoves()
        {
            List<Position> moves = new List<Position>();
            for (int y = 0; y < gameSize; y++)
            {
                for (int x = 0; x < gameSize; x++)
                {
                    if (isMoveValid(this, this.curPlayer, new Position() { x=x, y=y } ))
                    {
                        Position pos = new Position() { x = x, y = y };
                        moves.Add(pos);
                    }
                }
            }
            return moves;
        }

        public void printPossibleMoves()
        {
            List<Position> moves = getPossibleMoves();
            Console.WriteLine(" |A|B|C|D|E|F|G|H|");
            for (int y = 0; y < gameSize; y++)
            {
                Console.Write((y + 1).ToString() + "|");
                for (int x = 0; x < gameSize; x++)
                {
                    string output = "//";
                    if (gameField[x, y] == Player.Human)
                        output = "X";
                    else if (gameField[x, y] == Player.Bot)
                        output = "O";
                    else if (gameField[x, y] == Player.Empty)
                        output = "_";

                    for (int i = 0; i < moves.Count; i++)
                    {
                        if (moves[i].x == x && moves[i].y == y) output = "*";
                    }

                    Console.Write(output+"|");

                }
                Console.WriteLine();
            }
        }

        public static int count_stones(ReversiGame game, Player p)
        {
            int count = 0;
            for (int y = 0; y < gameSize; y++)
            {
                for (int x = 0; x < gameSize; x++)
                {
                    if (game.gameField[x,y] == p) count++;
                }
            }

            return count;
        }

        public static bool isMoveValid(ReversiGame game, Player player, Position pos)
        {
            Player[,] gameField = game.gameField;
            if (gameField[pos.x,pos.y] != Player.Empty) return false;

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    if (legal_dir(game, pos.x, pos.y, dx, dy)) return true;
                }
            }
            return false;
        }

        private static Player getOppositePlayer(ReversiGame game)
        {
            if (game.curPlayer == Player.Human) return Player.Bot;
            if (game.curPlayer == Player.Bot) return Player.Human;

            throw new Exception();
        }

        private static bool legal_dir(ReversiGame game, int x, int y, int dx, int dy)
        {
            bool stoneBetween = false;
            while (true)
            {
                x += dx;
                y += dy;
                if (out_of_bounds(x, y)) return false;

                if (game.gameField[x,y] == getOppositePlayer(game)) stoneBetween = true;
                if (game.gameField[x,y] == game.curPlayer) break;
                if (game.gameField[x,y] == Player.Empty) return false;
            }
            return stoneBetween;
        }

        private static bool out_of_bounds(int x, int y)
        {
            if (x < 0 || x >= gameSize) return true;
            if (y < 0 || y >= gameSize) return true;

            return false;
        }
    }
    public class Position
    {
        public int x;
        public int y;
    }
}
