using ComputeSharp;
using ComputeSharp.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using ReversiSandbox.ReversiBot;
using System.Security.Cryptography;

namespace ReversiSandbox
{
    partial class ComputeShader
    {
        [ThreadGroupSize(DefaultThreadGroupSizes.X)]
        [GeneratedComputeShaderDescriptor]
        public readonly partial struct FlipPlayers(ReadWriteBuffer<int> players) : IComputeShader
        {
            public void Execute()
            {
                players[ThreadIds.X] = ReversiGame.getOppositePlayer(players[ThreadIds.X]);
            }
        }

        [ThreadGroupSize(DefaultThreadGroupSizes.X)]
        [GeneratedComputeShaderDescriptor]
        public readonly partial struct botMasking(ReadWriteBuffer<int> gameField, ReadWriteBuffer<int> players, ReadWriteBuffer<int> moves, ReadWriteBuffer<int> movesLength, ReadWriteBuffer<float> weights, ReadWriteBuffer<int> move) : IComputeShader
        {
            public void Execute()
            {
                int gameIndex = ThreadIds.X;
                int player = players[gameIndex];

                Algorithm(player,gameIndex);
            }

            public void Algorithm(int player,int gameIndex)
            {
                int bestMoveX = 0;
                int bestMoveY = 0;
                float bestScore = 0;

                for (int i = 0; i < movesLength[gameIndex] / 2; i++)
                {
                    var x = moves[i * 2 + gameIndex * ReversiGame.gameSize * ReversiGame.gameSize * 2];
                    var y = moves[i * 2 + 1 + gameIndex * ReversiGame.gameSize * ReversiGame.gameSize * 2];

                    float score = weights[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] * getTilesCaptured(x, y, player,gameIndex);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMoveX = x;
                        bestMoveY = y;
                    }
                }
                move[gameIndex*2] = bestMoveX;
                move[gameIndex*2+1] = bestMoveY;
            }

            int getTilesCaptured(int x,int y, int p, int gameIndex)
            {
                int count = 0;

                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        count += count_dir(p, x, y, dx, dy, gameIndex);
                    }
                }

                return count;
            }

            int count_dir(int curPlayer, int x, int y, int dx, int dy, int gameIndex)
            {
                int count = 0;
                if (!legal_dir(curPlayer, x, y, dx, dy, gameIndex)) return 0;

                while (true)
                {
                    x += dx;
                    y += dy;

                    if (gameField[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] == ReversiGame.Empty)
                        return count;

                    if (out_of_bounds(x, y))
                        return count;

                    if (gameField[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] == curPlayer)
                        return count;

                    if (gameField[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] == ReversiGame.Human)
                        count++;
                    else if (gameField[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] == ReversiGame.Bot)
                        count++;
                }
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
                    if (gameField[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] == ReversiGame.Empty) return false;
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

        static ReadWriteBuffer<int>[] gameFieldBufferStack, playerBufferStack, possibleMovesBufferStack, possibleMovesLengthBufferStack, movesBufferStack;
        static ReadWriteBuffer<float>[] weightsBufferStack;
        static int stackCounter = 0;
        static int incommingBuffers = 0;
        const int bufferSize = 3;

        static ReadWriteBuffer<int> gameFieldBuffer, playerBuffer, possibleMovesBuffer, possibleMovesLengthBuffer, movesBuffer;
        static ReadWriteBuffer<float> weightsBuffer;
        
        static void bufferThreadAllocator(int parallelCount)
        {
            while (true)
            {
                while (bufferSize < (stackCounter + incommingBuffers + 1)) { }

                Thread t;
                t = new Thread(() => calculateBuffer(parallelCount));
                t.Start();

                incommingBuffers++;
            }
        }

        static void calculateBuffer(int parallelCount)
        {
            //Reserve Memory for Games
            int[] gameFields = new int[parallelCount * ReversiGame.gameSize * ReversiGame.gameSize];
            int[] players = new int[parallelCount];
            int[] moves = new int[parallelCount * 2];
            int[] possibleMoves = new int[parallelCount * ReversiGame.gameSize * ReversiGame.gameSize * 2];
            int[] possibleMovesLength = new int[parallelCount];
            float[] weights = new float[parallelCount * ReversiGame.gameSize * ReversiGame.gameSize];

            ReversiGame[] games = new ReversiGame[parallelCount];

            for (int i = 0; i < parallelCount; i++)
            {
                if (randomizedPlayfield)
                    games[i] = new ReversiGame(ReversiGame.generateRandomGameField());
                else
                    games[i] = new ReversiGame();
            }

            //Copy Game states
            for (int i = 0; i < parallelCount; i++)
            {
                ReversiGame game = games[i];
                players[i] = game.curPlayer;
                Array.Copy(game.gameField, 0, gameFields, i * ReversiGame.gameSize * ReversiGame.gameSize, ReversiGame.gameSize * ReversiGame.gameSize);
            }

            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = 1f;
            }

            //Create Buffers
            lock (possibleMovesBufferStack)
                lock (possibleMovesLengthBufferStack)
                    lock (playerBufferStack)
                        lock (gameFieldBufferStack)
                            lock (movesBufferStack)
                                lock (weightsBufferStack)
                                {
                                    possibleMovesBufferStack[stackCounter] = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(possibleMoves);

                                    possibleMovesLengthBufferStack[stackCounter] = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(possibleMovesLength);

                                    playerBufferStack[stackCounter] = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(players);

                                    gameFieldBufferStack[stackCounter] = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(gameFields);

                                    movesBufferStack[stackCounter] = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(moves);

                                    weightsBufferStack[stackCounter] = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(weights);
                                }
            stackCounter++;
            incommingBuffers--;
        }

        static void popBufferStack()
        {
            lock (possibleMovesBufferStack)
                lock (possibleMovesLengthBufferStack)
                    lock (playerBufferStack)
                        lock (gameFieldBufferStack)
                            lock (movesBufferStack)
                                lock (weightsBufferStack)
                                {
                                    possibleMovesBuffer = possibleMovesBufferStack[stackCounter - 1];

                                    possibleMovesLengthBuffer = possibleMovesLengthBufferStack[stackCounter - 1];

                                    playerBuffer = playerBufferStack[stackCounter - 1];

                                    gameFieldBuffer = gameFieldBufferStack[stackCounter - 1];

                                    movesBuffer = movesBufferStack[stackCounter - 1];

                                    weightsBuffer = weightsBufferStack[stackCounter - 1];
                                }
            stackCounter--;
        }

        static Match simulateMatch(Bot bot1, Bot bot2, int parallelCount = 3000000, int seriesCount = 5)
        {
            Stopwatch fullTimer, stepTimer;
            fullTimer = Stopwatch.StartNew();
            stepTimer = Stopwatch.StartNew();

            Thread t;
            t = new Thread(() => bufferThreadAllocator(parallelCount));
            t.Start();

            while (stackCounter == 0)
            {
                //Console.WriteLine("Waiting for Buffer generation...");
                Thread.Sleep(100);
            }

            for (int j = 0; j < seriesCount; j++)
            {
                //Create Buffers
                popBufferStack();

                //Console.WriteLine("Initialization : " + stepTimer.ElapsedMilliseconds + "ms");
                stepTimer.Restart();

                for (int z = 0; z < ReversiGame.gameSize * ReversiGame.gameSize - 4; z++)
                {

                    //Get all Possible moves
                    GraphicsDevice.GetDefault().For(parallelCount, new getPossibleMoves(gameFieldBuffer, playerBuffer, possibleMovesBuffer, possibleMovesLengthBuffer));

                    //Console.WriteLine("Find Possible Moves : " + stepTimer.ElapsedMilliseconds + "ms");
                    stepTimer.Restart();

                    //Console.WriteLine("Data Copying : " + stepTimer.ElapsedMilliseconds + "ms");
                    stepTimer.Restart();

                    //Generate bot movements
                    GraphicsDevice.GetDefault().For(parallelCount, new botMasking(gameFieldBuffer, playerBuffer, possibleMovesBuffer, possibleMovesLengthBuffer, weightsBuffer, movesBuffer));

                    //Console.WriteLine("Bot Decision : " + stepTimer.ElapsedMilliseconds + "ms");
                    stepTimer.Restart();

                    // Run one game tick
                    GraphicsDevice.GetDefault().For(parallelCount, new simulateMoves(gameFieldBuffer, playerBuffer, movesBuffer));

                    // Flip Players
                    GraphicsDevice.GetDefault().For(parallelCount, new FlipPlayers(playerBuffer));

                    //Console.WriteLine("Game Tick : " + stepTimer.ElapsedMilliseconds + "ms");
                    stepTimer.Restart();
                }

                int[] gameFields = new int[parallelCount * ReversiGame.gameSize * ReversiGame.gameSize];
                // Get the data back
                gameFieldBuffer.CopyTo(gameFields);

                // Print Playfields
                /*for (int i = 0; i < 5; i++)
                {
                    int[] gameField = new int[ReversiGame.gameSize * ReversiGame.gameSize];
                    Array.Copy(gameFields, ReversiGame.gameSize * ReversiGame.gameSize * i, gameField, 0, ReversiGame.gameSize * ReversiGame.gameSize);

                    ReversiGame.printGameField(gameField);
                    Console.WriteLine("--------");
                }*/

                while (stackCounter==0)
                {
                    Console.WriteLine("Waiting for Buffer generation...");
                    Thread.Sleep(100);
                }

                Console.WriteLine("done cycle");
            }
            Console.WriteLine("done " + parallelCount*seriesCount + " matches in " + fullTimer.ElapsedMilliseconds + "ms");

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

            gameFieldBufferStack = new ReadWriteBuffer<int>[bufferSize];
            playerBufferStack = new ReadWriteBuffer<int>[bufferSize];
            possibleMovesBufferStack = new ReadWriteBuffer<int>[bufferSize];
            possibleMovesLengthBufferStack = new ReadWriteBuffer<int>[bufferSize];
            movesBufferStack = new ReadWriteBuffer<int>[bufferSize];
            weightsBufferStack = new ReadWriteBuffer<float>[bufferSize];

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
