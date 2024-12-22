using ComputeSharp;
using ComputeSharp.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
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

        [ThreadGroupSize(DefaultThreadGroupSizes.X)]
        [GeneratedComputeShaderDescriptor]
        public readonly partial struct getPossibleMoves(ReadWriteBuffer<int> gameField, ReadWriteBuffer<int> players, ReadWriteBuffer<int> moves, ReadWriteBuffer<int> movesLength) : IComputeShader
        {
            private const int Empty = 0;
            private const int Human = 1;
            private const int Bot = 2;

            public void Execute()
            {
                int gameIndex = ThreadIds.X;
                int index = 0;
                int player = players[gameIndex];
                movesLength[gameIndex] = 0;

                for (int x = 0; x < ReversiGame.gameSize; x++)
                {
                    for (int y = 0; y < ReversiGame.gameSize; y++)
                    {
                        if (isMoveValid(player, x, y, gameIndex))
                        {
                            moves[gameIndex * ReversiGame.gameSize * ReversiGame.gameSize * 2 + index] = x;
                            moves[gameIndex * ReversiGame.gameSize * ReversiGame.gameSize * 2 + index + 1] = y;
                            index += 2;
                            movesLength[gameIndex] += 1;
                        }
                    }
                }
            }

            ComputeSharp.Bool isMoveValid(int player, int posx, int posy, int gameIndex)
            {
                if (gameField[(posx + posy * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] != Empty)
                    return false;

                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        if (legal_dir(player, posx, posy, dx, dy, gameIndex)) return true;
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

                    if (gameField[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] == ReversiGame.getOppositePlayer(player)) stoneBetween = true;
                    if (gameField[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] == player) break;
                    if (gameField[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] == Empty) return false;
                }

                return stoneBetween;
            }

            ComputeSharp.Bool out_of_bounds(int x, int y)
            {
                x = x % ReversiGame.gameSize;
                y = y % ReversiGame.gameSize;

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
                gameField[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] = curPlayer;

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

                    if (gameField[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] == Empty)
                        return;

                    if (out_of_bounds(x, y))
                        return;

                    if (gameField[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] == curPlayer)
                        return;

                    if (gameField[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] == Human)
                        gameField[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] = Bot;
                    else if (gameField[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] == Bot)
                        gameField[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] = Human;
                }
            }

            ComputeSharp.Bool isMoveValid(int player, int x, int y, int gameIndex)
            {
                if (gameField[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] != Empty)
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

                    if (gameField[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] == ReversiGame.getOppositePlayer(player)) stoneBetween = true;
                    if (gameField[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] == player) break;
                    if (gameField[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] == Empty) return false;
                }

                return stoneBetween;
            }

            ComputeSharp.Bool out_of_bounds(int x, int y)
            {
                x = x % ReversiGame.gameSize;
                y = y % ReversiGame.gameSize;

                if (x < 0 || x >= ReversiGame.gameSize) return true;
                if (y < 0 || y >= ReversiGame.gameSize) return true;

                return false;
            }
        }

        const bool randomizedPlayfield = false;

        static Match simulateMatch(Bot bot1, Bot bot2, int matchCount = 100000)
        {
            ReversiGame[] games = new ReversiGame[matchCount];

            for (int i = 0; i < matchCount; i++)
            {
                if (randomizedPlayfield)
                    games[i] = new ReversiGame(ReversiGame.generateRandomGameField());
                else
                    games[i] = new ReversiGame();
            }

            //Reserve Memory for Games
            int[] gameFields = new int[matchCount * ReversiGame.gameSize * ReversiGame.gameSize];
            int[] players = new int[matchCount];
            int[] moves = new int[matchCount * 2];
            int[] possibleMoves = new int[matchCount * ReversiGame.gameSize * ReversiGame.gameSize * 2];
            int[] possibleMovesLength = new int[matchCount];

            //Define Buffers
            ReadWriteBuffer<int> gameFieldBuffer, playerBuffer, possibleMovesBuffer, possibleMovesLengthBuffer;

            //Copy Game states
            for (int i = 0; i < matchCount; i++)
            {
                ReversiGame game = games[i];
                players[i] = game.curPlayer;
                Array.Copy(game.gameField,0,gameFields, i * ReversiGame.gameSize * ReversiGame.gameSize, ReversiGame.gameSize* ReversiGame.gameSize);
            }

            for (int j = 0; j < 30; j++)
            {
                //Create Buffers
                gameFieldBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(gameFields);
                playerBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(players);
                possibleMovesBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(possibleMoves);
                possibleMovesLengthBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(possibleMovesLength);

                //Get all Possible moves
                GraphicsDevice.GetDefault().For(matchCount, new getPossibleMoves(gameFieldBuffer, playerBuffer, possibleMovesBuffer, possibleMovesLengthBuffer));

                // Get the data back
                possibleMovesBuffer.CopyTo(possibleMoves);
                possibleMovesLengthBuffer.CopyTo(possibleMovesLength);

                //Generate bot movements
                for (int i = 0; i < matchCount; i++)
                {
                    int movesLength = possibleMovesLength[i];
                    int[] movesGame = new int[movesLength * 2];
                    Array.Copy(possibleMoves, ReversiGame.gameSize * ReversiGame.gameSize * 2 * i, movesGame, 0, movesLength * 2);
                    int[] gameField = new int[ReversiGame.gameSize * ReversiGame.gameSize];
                    Array.Copy(gameFields, ReversiGame.gameSize * ReversiGame.gameSize * i, gameField, 0, ReversiGame.gameSize * ReversiGame.gameSize);

                    if (movesLength == 0) continue;

                    Position move = new Position();

                    if (players[i] == ReversiGame.Human)
                        move = bot1.generateMove(gameField, movesGame, players[i]);

                    if (players[i] == ReversiGame.Bot)
                        move = bot2.generateMove(gameField, movesGame, players[i]);

                    moves[i * 2] = move.x;
                    moves[i * 2 + 1] = move.y;
                }

                // Create Buffer
                ReadWriteBuffer<int> movesBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(moves);

                // Run one game tick
                GraphicsDevice.GetDefault().For(matchCount, new simulateMoves(gameFieldBuffer, playerBuffer, movesBuffer));

                // Get the data back
                gameFieldBuffer.CopyTo(gameFields);

                // Flip Players
                for (int i = 0; i < matchCount; i++)
                    players[i] = ReversiGame.getOppositePlayer(players[i]);
            }
            



            // Print Playfields
            /*for (int i = 0; i < matchCount; i++)
            {
                int[] gameField = new int[ReversiGame.gameSize * ReversiGame.gameSize];
                Array.Copy(gameFields, ReversiGame.gameSize * ReversiGame.gameSize * i, gameField, 0, ReversiGame.gameSize * ReversiGame.gameSize);
                
                ReversiGame.printGameField(gameField);
                Console.WriteLine("--------");
            }*/

            Console.WriteLine("done");

            while (true) { }

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
