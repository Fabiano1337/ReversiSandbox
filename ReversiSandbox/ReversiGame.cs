using ComputeSharp;
using ReversiSandbox.ReversiBot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReversiSandbox
{

    public class ReversiGame
    {
        public const int gameSize = 8;
        public int curPlayer;
        public int[] gameField;

        public const int Empty = 0;
        public const int Human = 1;
        public const int Bot = 2;

        public ReversiGame()
        {
            gameField = new int[gameSize*gameSize];
            for (int x = 0; x < gameSize; x++)
            {
                for (int y = 0; y < gameSize; y++)
                {
                    gameField[x+y*gameSize] = Empty;
                }
            }

            gameField[gameSize / 2 - 1 + (gameSize / 2 - 1)*gameSize] = Bot;
            gameField[gameSize / 2 - 1 + (gameSize / 2)* gameSize] = Human;
            gameField[gameSize / 2 + (gameSize / 2 - 1)* gameSize] = Human;
            gameField[gameSize / 2 + (gameSize / 2)* gameSize] = Bot;

            curPlayer = Human;
        }

        public static int[] generateRandomGameField()
        {
            int[] field = new int[gameSize * gameSize];
            for (int x = 0; x < gameSize; x++)
            {
                for (int y = 0; y < gameSize; y++)
                {
                    field[x+y*gameSize] = Empty;
                }
            }

            ReversiGame game = new ReversiGame();
            Random r = new Random();
            BotRandom botRandom = new BotRandom();
            int randomCount = r.Next(0, 30);

            for (int i = 0; i < randomCount; i++)
            {
                Position botMove = botRandom.generateMove(game.gameField,ReversiGame.getPossibleMoves(game.gameField,game.curPlayer),game.curPlayer);
                game.move(botMove);
                game.switchPlayer();
                if (game.isEnded()) return generateRandomGameField();

                game.switchPlayer();
                if (game.isEnded()) return generateRandomGameField();

                game.switchPlayer();
            }

            field = game.gameField;

            return field;
        }

        public ReversiGame(int[] gameField)
        {
            this.gameField = gameField;

            curPlayer = Human;
        }

        public void setint(int p)
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

                if (gameField[x+y*gameSize] == Empty) return;
                if (out_of_bounds(x, y)) return;
                if (gameField[x+y*gameSize] == curPlayer) return;

                if (gameField[x+y*gameSize] == Human)
                    gameField[x+y*gameSize] = Bot;
                else if (gameField[x+y*gameSize] == Bot)
                    gameField[x+y*gameSize] = Human;
            }
        }

        static int[] reverse_dir(int[] gameField, int curPlayer, int x, int y, int dx, int dy)
        {
            if (!legal_dir(gameField,curPlayer, x, y, dx, dy)) return gameField;

            while (true)
            {
                x += dx;
                y += dy;

                if (gameField[x+y*gameSize] == Empty) return gameField;
                if (out_of_bounds(x, y)) return gameField;
                if (gameField[x+y*gameSize] == curPlayer) return gameField;

                if (gameField[x+y*gameSize] == Human)
                    gameField[x+y*gameSize] = Bot;
                else if (gameField[x+y*gameSize] == Bot)
                    gameField[x+y*gameSize] = Human;
            }
        }

        static int count_dir(int[] gameField, int curPlayer,int x, int y, int dx, int dy)
        {
            int count = 0;
            if (!legal_dir(gameField, curPlayer, x, y, dx, dy)) return 0;

            while (true)
            {
                x += dx;
                y += dy;

                if (gameField[x+y*gameSize] == Empty) return count;
                if (out_of_bounds(x, y)) return count;
                if (gameField[x+y*gameSize] == curPlayer) return count;

                if (gameField[x+y*gameSize] == Human)
                    count++;
                else if (gameField[x+y*gameSize] == Bot)
                    count++;
            }
        }

        public static int[] reverse(int[] gameField, int curPlayer,int x, int y)
        {
            Position pos = new Position { x = x, y = y };
            if (!isMoveValid(gameField, curPlayer, pos))
                throw new Exception();

            gameField[x+y*gameSize] = curPlayer;
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

            gameField[x+y*gameSize] = curPlayer;
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
            if (getPossibleMoves(curPlayer).Length == 0) return true;

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
                    if (gameField[x + y * gameSize] == Human)
                        Console.Write("X");
                    if (gameField[x + y * gameSize] == Bot)
                        Console.Write("O");
                    if (gameField[x + y * gameSize] == Empty)
                        Console.Write("_");

                    Console.Write("|");

                }
                Console.WriteLine();
            }
        }

        public static void printGameField(int[] gameField)
        {
            Console.WriteLine(" |A|B|C|D|E|F|G|H|");
            for (int y = 0; y < gameSize; y++)
            {
                Console.Write((y + 1).ToString() + "|");
                for (int x = 0; x < gameSize; x++)
                {
                    if (gameField[x + y * gameSize] == Human)
                        Console.Write("X");
                    if (gameField[x + y * gameSize] == Bot)
                        Console.Write("O");
                    if (gameField[x + y * gameSize] == Empty)
                        Console.Write("_");

                    Console.Write("|");

                }
                Console.WriteLine();
            }
        }

        public static int getTilesCaptured(int[] gameField, Position pos, int p)
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

        public int[] getPossibleMoves()
        {
            return getPossibleMoves(curPlayer);
        }

        public int[] getPossibleMoves(int player)
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

            int[] movesArray = new int[moves.Count * 2];

            for (int i = 0; i < moves.Count; i++)
            {
                movesArray[i * 2] = moves[i].x;
                movesArray[i * 2 + 1] = moves[i].y;
            }

            return movesArray;
        }

        public static int[] getPossibleMoves(int[] gameField, int player)
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

            int[] movesArray = new int[moves.Count * 2];

            for (int i = 0; i < moves.Count; i++)
            {
                movesArray[i * 2] = moves[i].x;
                movesArray[i * 2 + 1] = moves[i].y;
            }

            return movesArray;
        }

        public void printPossibleMoves()
        {
            int[] moves = getPossibleMoves();
            Console.WriteLine(" |A|B|C|D|E|F|G|H|");
            for (int y = 0; y < gameSize; y++)
            {
                Console.Write((y + 1).ToString() + "|");
                for (int x = 0; x < gameSize; x++)
                {
                    string output = "//";
                    if (gameField[x+y*gameSize] == Human)
                        output = "X";
                    else if (gameField[x+y*gameSize] == Bot)
                        output = "O";
                    else if (gameField[x+y*gameSize] == Empty)
                        output = "_";

                    for (int i = 0; i < moves.Length/2; i++)
                    {
                        if (moves[i*2] == x && moves[i*2+1] == y) output = "*";
                    }

                    Console.Write(output+"|");

                }
                Console.WriteLine();
            }
        }

        public static int count_stones(ReversiGame game, int p)
        {
            int count = 0;
            for (int y = 0; y < gameSize; y++)
            {
                for (int x = 0; x < gameSize; x++)
                {
                    if (game.gameField[x+y*gameSize] == p) count++;
                }
            }

            return count;
        }

        public static bool isMoveValid(ReversiGame game, int player, Position pos)
        {
            int[] gameField = game.gameField;
            if (gameField[pos.x + pos.y * gameSize] != Empty) return false;

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

        public static bool isMoveValid(int[] gameField, int player, Position pos)
        {
            if (gameField[pos.x + pos.y * gameSize] != Empty) return false;

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

        public static int getOppositePlayer(ReversiGame game)
        {
            if (game.curPlayer == Human) return Bot;
            if (game.curPlayer == Bot) return Human;

            throw new Exception();
        }

        public static int getOppositePlayer(int p)
        {
            if (p == Human) return Bot;
            if (p == Bot) return Human;

            return -1;
        }

        private static bool legal_dir(ReversiGame game, int x, int y, int dx, int dy)
        {
            bool stoneBetween = false;
            while (true)
            {
                x += dx;
                y += dy;
                if (out_of_bounds(x, y)) return false;

                if (game.gameField[x+y*gameSize] == getOppositePlayer(game)) stoneBetween = true;
                if (game.gameField[x+y*gameSize] == game.curPlayer) break;
                if (game.gameField[x+y*gameSize] == Empty) return false;
            }
            return stoneBetween;
        }

        private static bool legal_dir(int[] gameField, int curPlayer, int x, int y, int dx, int dy)
        {
            bool stoneBetween = false;
            while (true)
            {
                x += dx;
                y += dy;
                if (out_of_bounds(x, y)) return false;

                if (gameField[x+y*gameSize] == getOppositePlayer(curPlayer)) stoneBetween = true;
                if (gameField[x+y*gameSize] == curPlayer) break;
                if (gameField[x+y*gameSize] == Empty) return false;
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
