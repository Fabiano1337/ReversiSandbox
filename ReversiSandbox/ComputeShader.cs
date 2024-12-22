using ComputeSharp;
using ComputeSharp.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ReversiSandbox
{
    partial class ComputeShader
    {
        [ThreadGroupSize(DefaultThreadGroupSizes.X)]
        [GeneratedComputeShaderDescriptor]
        public readonly partial struct MultiplyByTwo(ReadWriteBuffer<int> buffer) : IComputeShader
        {
            public void Execute()
            {
                buffer[ThreadIds.X] *= 2;
            }
        }

        [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
        [GeneratedComputeShaderDescriptor]
        public readonly partial struct getPossibleMoves(ReadWriteBuffer<int> gameBuffer, int player, ReadWriteBuffer<int> moves) : IComputeShader
        {
            private const int Empty = 0;
            private const int Human = 1;
            private const int Bot = 2;

            public void Execute()
            {
                int gameX = ThreadIds.X % 8;
                int gameY = ThreadIds.Y % 8;
                if (isMoveValid(player, gameX, gameY))
                {
                    moves[ThreadIds.X + ThreadIds.Y * ReversiGame.gameSize * 2] = ThreadIds.X;
                    moves[ThreadIds.X + ThreadIds.Y * ReversiGame.gameSize * 2 + 1] = ThreadIds.Y;
                }
            }

            ComputeSharp.Bool isMoveValid(int player, int posx, int posy)
            {
                if (gameBuffer[posx + posy * ReversiGame.gameSize] != Empty)
                    return false;

                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        if (legal_dir(player, posx, posy, dx, dy)) return true;
                    }
                }
                return false;
            }

            ComputeSharp.Bool legal_dir(int player, int x, int y, int dx, int dy)
            {
                ComputeSharp.Bool stoneBetween = false;

                while (true)
                {
                    x += dx;
                    y += dy;
                    if (out_of_bounds(x, y)) return false;

                    if (gameBuffer[x + y * ReversiGame.gameSize] == ReversiGame.getOppositePlayer(player)) stoneBetween = true;
                    if (gameBuffer[x + y * ReversiGame.gameSize] == player) break;
                    if (gameBuffer[x + y * ReversiGame.gameSize] == Empty) return false;
                }
                return stoneBetween;
            }

            ComputeSharp.Bool out_of_bounds(int x, int y)
            {
                if (x < 0 || x >= ReversiGame.gameSize) return true;
                if (y < 0 || y >= ReversiGame.gameSize) return true;

                return false;
            }
        }

        [ThreadGroupSize(DefaultThreadGroupSizes.X)]
        [GeneratedComputeShaderDescriptor]
        public readonly partial struct simulateMoves(ReadWriteBuffer<int> gameField, ReadWriteBuffer<int> players, ReadWriteBuffer<int> moves) : IComputeShader
        {
            private const int Empty = 0;
            private const int Human = 1;
            private const int Bot = 2;

            public void Execute()
            {
                int gameIndex = ThreadIds.X;
                int player = players[gameIndex];
                int moveX = moves[gameIndex*2];
                int moveY = moves[gameIndex*2+1];

                if (!isMoveValid(player, moveX, moveY, gameIndex))
                    return;

                reverse(player,moveX,moveY,gameIndex);
            }

            void reverse(int curPlayer, int x, int y,int gameIndex)
            {
                gameField[(x + y * ReversiGame.gameSize) * (gameIndex+1)] = curPlayer;

                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        reverse_dir(curPlayer, x, y, dx, dy, gameIndex);
                    }
                }
            }

            void reverse_dir(int curPlayer, int x, int y, int dx, int dy, int gameIndex)
            {
                if (!legal_dir(curPlayer, x, y, dx, dy, gameIndex)) return;

                while (true)
                {
                    x += dx;
                    y += dy;

                    if (gameField[(x + y * ReversiGame.gameSize) * (gameIndex + 1)] == Empty) return;
                    if (out_of_bounds(x, y)) return;
                    if (gameField[(x + y * ReversiGame.gameSize) * (gameIndex + 1)] == curPlayer) return;

                    if (gameField[(x + y * ReversiGame.gameSize) * (gameIndex + 1)] == Human)
                        gameField[(x + y * ReversiGame.gameSize) * (gameIndex + 1)] = Bot;
                    else if (gameField[(x + y * ReversiGame.gameSize) * (gameIndex + 1)] == Bot)
                        gameField[(x + y * ReversiGame.gameSize) * (gameIndex + 1)] = Human;
                }
            }

            ComputeSharp.Bool isMoveValid(int player, int x, int y, int gameIndex)
            {
                if (gameField[(x + y * ReversiGame.gameSize) * (gameIndex + 1)] != Empty)
                    return false;

                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        if (legal_dir(player, x, y, dx, dy, gameIndex)) return true;
                    }
                }
                return false;
            }

            ComputeSharp.Bool legal_dir(int player, int x, int y, int dx, int dy, int gameIndex)
            {
                ComputeSharp.Bool stoneBetween = false;

                while (true)
                {
                    x += dx;
                    y += dy;
                    if (out_of_bounds(x, y)) return false;

                    if (gameField[(x + y * ReversiGame.gameSize) * (gameIndex + 1)] == ReversiGame.getOppositePlayer(player)) stoneBetween = true; // This Line is not working...
                    if (gameField[(x + y * ReversiGame.gameSize) * (gameIndex + 1)] == player) break;
                    if (gameField[(x + y * ReversiGame.gameSize) * (gameIndex + 1)] == Empty) return false;
                }

                return stoneBetween;
            }

            ComputeSharp.Bool out_of_bounds(int x, int y)
            {
                x = x % 8;
                y = y % 8;

                if (x < 0 || x >= ReversiGame.gameSize) return true;
                if (y < 0 || y >= ReversiGame.gameSize) return true;

                return false;
            }
        }

        static Match simulateMatch(Bot bot1, Bot bot2)
        {
            int matchCount = 1;
            ReversiGame[] games = new ReversiGame[matchCount];

            for (int i = 0; i < matchCount; i++)
            {
                games[i] = new ReversiGame();
            }

            int[] gameFields = new int[matchCount * ReversiGame.gameSize * ReversiGame.gameSize];
            int[] players = new int[matchCount];
            int[] moves = new int[matchCount*2];

            for (int i = 0; i < matchCount; i++)
            {
                ReversiGame game = games[i];
                players[i] = game.curPlayer;
                Array.Copy(game.gameField,0,gameFields, i * ReversiGame.gameSize * ReversiGame.gameSize, ReversiGame.gameSize* ReversiGame.gameSize);
            }

            for (int i = 0; i < matchCount; i++)
            {
                moves[i * 2] = 3;
                moves[i * 2 + 1] = 2;
            }

            //int[] gameField = new int[ReversiGame.gameSize * ReversiGame.gameSize];
            //int[] moves = new int[ReversiGame.gameSize * ReversiGame.gameSize * 2 * matchCount];

            using ReadWriteBuffer<int> gameFieldBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(gameFields);
            using ReadWriteBuffer<int> playerBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(players);
            using ReadWriteBuffer<int> movesBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(moves);

            // Launch the shader
            GraphicsDevice.GetDefault().For(matchCount, new simulateMoves(gameFieldBuffer, playerBuffer, movesBuffer));

            // Get the data back
            gameFieldBuffer.CopyTo(gameFields);

            for (int i = 0; i < matchCount*8*8; i++)
            {
                Console.WriteLine(gameFields[i]);
            }

            Console.WriteLine("done");

            const bool randomizedPlayfield = true;

            

            /*if (randomizedPlayfield)
                game = new ReversiGame(ReversiGame.generateRandomGameField());
            else
                game = new ReversiGame();

            Position botMove;

            Random r = new Random();
            bool reverse = r.Next(0, 2) == 1 ? true : false;

            if (reverse)
            {
                Bot buffer = bot1;
                bot1 = bot2;
                bot2 = buffer;
            }

            while (true)
            {
                botMove = bot1.generateMove(game);
                game.move(botMove);
                game.switchPlayer();
                //Console.WriteLine(bot1.ToString() + " move : " + positionToString(botMove));
                //game.printGameField();
                if (game.isEnded()) break;

                botMove = bot2.generateMove(game);
                game.move(botMove);
                game.switchPlayer();
                //Console.WriteLine(bot2.ToString() + " move : " + positionToString(botMove));
                //game.printGameField();
                if (game.isEnded()) break;
            }

            int bot1Score = ReversiGame.count_stones(game, Player.Human);
            int bot2Score = ReversiGame.count_stones(game, Player.Bot);
            //Console.WriteLine("Bot1's Score : " + (bot1Score - bot2Score).ToString());
            //Console.WriteLine("Bot2's Score : " + (bot2Score - bot1Score).ToString());

            Match match = new Match();
            match.bot1 = bot1;
            match.bot2 = bot2;
            match.bot1Score = bot1Score;
            match.bot2Score = bot2Score;

            if (bot1Score > bot2Score)
            {
                Console.WriteLine("Bot1 Won!");
                match.Winner = bot1;
                match.Looser = bot2;
            }
            else if (bot2Score > bot1Score)
            {
                Console.WriteLine("Bot2 Won!");
                match.Winner = bot2;
                match.Looser = bot1;
            }
            else if (bot1Score == bot2Score)
            {
                Console.WriteLine("Tie!");
                match.Winner = null;
                match.Looser = null;
            }*/

            return new Match();
        }

        public static void testComputeShader()
        {
            Bot bot1 = new BotRandom();
            Bot bot2 = new BotRandom();

            simulateMatch(bot1, bot2);

            /*
            // Get some sample data
            int[] array = [.. Enumerable.Range(1, 8*8*100000)];

            Console.WriteLine("------------------------");

            // Allocate a GPU buffer and copy the data to it.
            // We want the shader to modify the items in-place, so we
            // can allocate a single read-write buffer to work on.
            using ReadWriteBuffer<int> gameField = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(array);
            using ReadWriteBuffer<int> buffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(array);

            // Launch the shader
            GraphicsDevice.GetDefault().For(buffer.Length, new MultiplyByTwo(gameField));

            // Get the data back
            buffer.CopyTo(array);

            Console.WriteLine("done");

            /*foreach (int i in array)
            {
                Console.WriteLine(i);

            }*/
        }
    }
}
