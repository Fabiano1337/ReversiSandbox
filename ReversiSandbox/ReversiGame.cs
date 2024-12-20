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
        public const int gameSize = 8;
        public Player curPlayer;
        public Player[,] gameField;

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
            if (!isMoveValid(this, curPlayer, pos)) throw new Exception();

            reverse(pos.x, pos.y);
        }

        void reverse_dir(int x, int y, int dx, int dy)
        {
            if (!legal_dir(this, x, y, dx, dy)) return;

            while (true)
            {
                x += dx;
                y += dy;

                if (gameField[x, y] == Player.Empty) return;
                if (out_of_bounds(x, y)) return;
                if (gameField[x, y] == curPlayer) return;

                if (gameField[x, y] == Player.Human)
                    gameField[x, y] = Player.Bot;
                else if (gameField[x, y] == Player.Bot)
                    gameField[x, y] = Player.Human;
            }
        }

        static Player[,] reverse_dir(Player[,] gameField, Player curPlayer, int x, int y, int dx, int dy)
        {
            if (!legal_dir(gameField,curPlayer, x, y, dx, dy)) return gameField;

            while (true)
            {
                x += dx;
                y += dy;

                if (gameField[x, y] == Player.Empty) return gameField;
                if (out_of_bounds(x, y)) return gameField;
                if (gameField[x, y] == curPlayer) return gameField;

                if (gameField[x, y] == Player.Human)
                    gameField[x, y] = Player.Bot;
                else if (gameField[x, y] == Player.Bot)
                    gameField[x, y] = Player.Human;
            }
        }

        static int count_dir(Player[,] gameField, Player curPlayer,int x, int y, int dx, int dy)
        {
            int count = 0;
            if (!legal_dir(gameField, curPlayer, x, y, dx, dy)) return 0;

            while (true)
            {
                x += dx;
                y += dy;

                if (gameField[x, y] == Player.Empty) return count;
                if (out_of_bounds(x, y)) return count;
                if (gameField[x, y] == curPlayer) return count;

                if (gameField[x, y] == Player.Human)
                    count++;
                else if (gameField[x, y] == Player.Bot)
                    count++;
            }
        }

        public static Player[,] reverse(Player[,] gameField, Player curPlayer,int x, int y)
        {
            Position pos = new Position { x = x, y = y };
            if (!isMoveValid(gameField, curPlayer, pos))
                throw new Exception();

            gameField[x, y] = curPlayer;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    gameField = reverse_dir(gameField, curPlayer,x, y, dx, dy);
                }
            }

            return gameField;
        }

        void reverse(int x, int y)
        {
            Position pos = new Position { x = x, y = y };
            if (!isMoveValid(this, curPlayer, pos)) return;

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

        public bool isEnded()
        {
            if (getPossibleMoves(curPlayer).Count == 0) return true;

            //if (getPossibleMoves(getOppositePlayer(this)).Count == 0) return true;

            return false;
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

        public static int getTilesCaptured(Player[,] gameField, Position pos, Player p)
        {
            if (!isMoveValid(gameField, p, pos)) 
                throw new Exception();

            int count = 0;

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    count += count_dir(gameField,p,pos.x, pos.y, dx, dy);
                }
            }

            return count;
        }

        public List<Position> getPossibleMoves()
        {
            return getPossibleMoves(curPlayer);
        }

        public List<Position> getPossibleMoves(Player player)
        {
            List<Position> moves = new List<Position>();
            for (int y = 0; y < gameSize; y++)
            {
                for (int x = 0; x < gameSize; x++)
                {
                    if (isMoveValid(this, player, new Position() { x = x, y = y }))
                    {
                        Position pos = new Position() { x = x, y = y };
                        moves.Add(pos);
                    }
                }
            }
            return moves;
        }

        public static List<Position> getPossibleMoves(Player[,] gameField, Player player)
        {
            List<Position> moves = new List<Position>();
            for (int y = 0; y < gameSize; y++)
            {
                for (int x = 0; x < gameSize; x++)
                {
                    if (isMoveValid(gameField, player, new Position() { x = x, y = y }))
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
            if (gameField[pos.x, pos.y] != Player.Empty) return false;

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

        public static bool isMoveValid(Player[,] gameField, Player player, Position pos)
        {
            if (gameField[pos.x, pos.y] != Player.Empty) return false;

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    if (legal_dir(gameField, player, pos.x, pos.y, dx, dy)) return true;
                }
            }
            return false;
        }

        public static Player getOppositePlayer(ReversiGame game)
        {
            if (game.curPlayer == Player.Human) return Player.Bot;
            if (game.curPlayer == Player.Bot) return Player.Human;

            throw new Exception();
        }

        public static Player getOppositePlayer(Player p)
        {
            if (p == Player.Human) return Player.Bot;
            if (p == Player.Bot) return Player.Human;

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

                if (game.gameField[x, y] == getOppositePlayer(game)) stoneBetween = true;
                if (game.gameField[x, y] == game.curPlayer) break;
                if (game.gameField[x, y] == Player.Empty) return false;
            }
            return stoneBetween;
        }

        private static bool legal_dir(Player[,] gameField, Player curPlayer, int x, int y, int dx, int dy)
        {
            bool stoneBetween = false;
            while (true)
            {
                x += dx;
                y += dy;
                if (out_of_bounds(x, y)) return false;

                if (gameField[x, y] == getOppositePlayer(curPlayer)) stoneBetween = true;
                if (gameField[x, y] == curPlayer) break;
                if (gameField[x, y] == Player.Empty) return false;
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
