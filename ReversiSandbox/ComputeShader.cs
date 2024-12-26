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
        public readonly partial struct getMatchResults(ReadWriteBuffer<int> gameField, ReadWriteBuffer<int> winners, ReadWriteBuffer<int> loosers, ReadWriteBuffer<int> winnerScore, ReadWriteBuffer<int> looserScore, ReadWriteBuffer<int> starterBuffer) : IComputeShader
        {
            public void Execute()
            {
                int gameIndex = ThreadIds.X;

                int me = starterBuffer[gameIndex];

                int score1 = count_stones(me, gameIndex);
                int score2 = count_stones(ReversiGame.getOppositePlayer(me), gameIndex);

                if (score1 > score2)
                {
                    winners[gameIndex] = 1;
                    loosers[gameIndex] = 2;
                    winnerScore[gameIndex] = score1;
                    looserScore[gameIndex] = score2;
                }
                else if (score1 < score2)
                {
                    winners[gameIndex] = 2;
                    loosers[gameIndex] = 1;
                    winnerScore[gameIndex] = score2;
                    looserScore[gameIndex] = score1;
                }
                else if (score1 == score2)
                {
                    winners[gameIndex] = 0;
                    loosers[gameIndex] = 0;
                    winnerScore[gameIndex] = score1;
                    looserScore[gameIndex] = score2;
                }
            }

            int count_stones(int p, int gameIndex)
            {
                int count = 0;
                for (int y = 0; y < ReversiGame.gameSize; y++)
                {
                    for (int x = 0; x < ReversiGame.gameSize; x++)
                    {
                        if (gameField[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] == p)
                            count++;
                    }
                }

                return count;
            }
        }

        [ThreadGroupSize(DefaultThreadGroupSizes.X)]
        [GeneratedComputeShaderDescriptor]
        public readonly partial struct botMasking(ReadWriteBuffer<int> gameField, ReadWriteBuffer<int> players, ReadWriteBuffer<int> moves, ReadWriteBuffer<int> movesLength, ReadWriteBuffer<float> weights1, ReadWriteBuffer<float> weights2, ReadWriteBuffer<int> move, ReadWriteBuffer<int> startingPlayers) : IComputeShader
        {
            public void Execute()
            {
                int gameIndex = ThreadIds.X;
                int player = players[gameIndex];

                Algorithm(player, gameIndex);
            }

            public void Algorithm(int player, int gameIndex)
            {
                int bestMoveX = 0;
                int bestMoveY = 0;
                float bestScore = 0;

                for (int i = 0; i < movesLength[gameIndex]; i++)
                {
                    var x = moves[i * 2 + gameIndex * ReversiGame.gameSize * ReversiGame.gameSize * 2];
                    var y = moves[i * 2 + 1 + gameIndex * ReversiGame.gameSize * ReversiGame.gameSize * 2];

                    //change
                    float score = 0;

                    if ((player == ReversiGame.Human && startingPlayers[gameIndex] == 1) || (player == ReversiGame.Bot && startingPlayers[gameIndex] == 2)) // starting
                        score = weights1[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] * getTilesCaptured(x, y, player, gameIndex);
                    if ((player == ReversiGame.Human && startingPlayers[gameIndex] == 2) || (player == ReversiGame.Bot && startingPlayers[gameIndex] == 1)) // second
                        score = weights2[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] * getTilesCaptured(x, y, player, gameIndex);


                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMoveX = x;
                        bestMoveY = y;
                    }
                }
                move[gameIndex * 2] = bestMoveX;
                move[gameIndex * 2 + 1] = bestMoveY;
            }

            int getTilesCaptured(int x, int y, int p, int gameIndex)
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

                    if (out_of_bounds(x, y, gameIndex))
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
                    if (out_of_bounds(x, y, gameIndex)) return false;

                    if (gameField[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] == ReversiGame.getOppositePlayer(player)) stoneBetween = true;
                    if (gameField[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] == player) break;
                    if (gameField[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] == ReversiGame.Empty) return false;
                }

                return stoneBetween;
            }

            ComputeSharp.Bool out_of_bounds(int x, int y, int gameIndex)
            {
                if (x < 0 || x >= ReversiGame.gameSize) return true;
                if (y < 0 || y >= ReversiGame.gameSize) return true;

                return false;
            }
        }

        [ThreadGroupSize(DefaultThreadGroupSizes.X)]
        [GeneratedComputeShaderDescriptor]
        public readonly partial struct botRandom(ReadWriteBuffer<int> gameField, ReadWriteBuffer<int> players, ReadWriteBuffer<int> moves, ReadWriteBuffer<int> movesLength, ReadWriteBuffer<int> randomNumbers, ReadWriteBuffer<int> move, ReadWriteBuffer<int> randomStop, int iteration) : IComputeShader
        {
            public void Execute()
            {
                int gameIndex = ThreadIds.X;
                int player = players[gameIndex];

                if (iteration > randomStop[gameIndex])
                {
                    players[gameIndex] = ReversiGame.getOppositePlayer(player);
                    return;
                }

                Algorithm(player, gameIndex);
            }

            public void Algorithm(int player, int gameIndex)
            {
                int moveIndex = randomNumbers[gameIndex] % movesLength[gameIndex];

                move[gameIndex * 2] = moves[gameIndex * ReversiGame.gameSize * ReversiGame.gameSize * 2 + moveIndex * 2];
                move[gameIndex * 2 + 1] = moves[gameIndex * ReversiGame.gameSize * ReversiGame.gameSize * 2 + moveIndex * 2 + 1];
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

                    if (out_of_bounds(x, y, gameIndex))
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
                    if (out_of_bounds(x, y, gameIndex)) return false;

                    if (gameField[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] == ReversiGame.getOppositePlayer(player)) stoneBetween = true;
                    if (gameField[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] == player) break;
                    if (gameField[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] == ReversiGame.Empty) return false;
                }

                return stoneBetween;
            }

            ComputeSharp.Bool out_of_bounds(int x, int y, int gameIndex)
            {
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
                    if (out_of_bounds(x, y, gameIndex)) return false;

                    if (gameField[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] == ReversiGame.getOppositePlayer(player)) stoneBetween = true;
                    if (gameField[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] == player) break;
                    if (gameField[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] == Empty) return false;
                }

                return stoneBetween;
            }

            ComputeSharp.Bool out_of_bounds(int x, int y, int gameIndex)
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
                int moveX = moves[gameIndex * 2];
                int moveY = moves[gameIndex * 2 + 1];

                if (!isMoveValid(player, moveX, moveY, gameIndex))
                    return;

                reverse(player, moveX, moveY, gameIndex);
            }

            void reverse(int curPlayer, int x, int y, int gameIndex)
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

                    if (out_of_bounds(x, y, gameIndex))
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
                    if (out_of_bounds(x, y, gameIndex)) return false;

                    if (gameField[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] == ReversiGame.getOppositePlayer(player)) stoneBetween = true;
                    if (gameField[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] == player) break;
                    if (gameField[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] == Empty) return false;
                }

                return stoneBetween;
            }

            ComputeSharp.Bool out_of_bounds(int x, int y, int gameIndex)
            {
                if (x < 0 || x >= ReversiGame.gameSize) return true;
                if (y < 0 || y >= ReversiGame.gameSize) return true;

                return false;
            }
        }

        const bool randomizedPlayfield = true;

        static ReadWriteBuffer<int>[] startingPlayersBufferStack, gameFieldBufferStack, playerBufferStack, possibleMovesBufferStack, possibleMovesLengthBufferStack, movesBufferStack;
        static ReadWriteBuffer<float>[] weightsBot1BufferStack, weightsBot2BufferStack;
        static int stackCounter = 0;
        static int incommingBuffers = 0;
        const int bufferSize = 1;
        static int[] gameFieldsGenerated;

        static ReadWriteBuffer<int> startingPlayerBuffer, gameFieldBuffer, playerBuffer, possibleMovesBuffer, possibleMovesLengthBuffer, movesBuffer;
        static ReadWriteBuffer<float> weightsBot1Buffer, weightsBot2Buffer;

        static float[] startWeights =
               {16f,8f,8f,8f,8f,8f,8f,16f,
                8f,1f,1f,1f,1f,1f,1f,8f,
                8f,1f,4f,4f,4f,4f,1f,8f,
                8f,1f,4f,1f,1f,4f,1f,8f,
                8f,1f,4f,1f,1f,4f,1f,8f,
                8f,1f,4f,4f,4f,4f,1f,8f,
                8f,1f,1f,1f,1f,1f,1f,8f,
                16f,8f,8f,8f,8f,8f,8f,16f};

        static void bufferThreadAllocator(int parallelCount)
        {
            while (true)
            {
                while (bufferSize <= (stackCounter + incommingBuffers)) { }

                Thread t;
                t = new Thread(() => calculateBuffer(parallelCount));
                t.Start();

                incommingBuffers++;

                Thread.Sleep(200);
            }
        }

        static float[] weight1Cache;
        static float[] weight2Cache;

        static void calculateBuffer(int parallelCount)
        {
            //Reserve Memory for Games
            int[] gameFields = new int[parallelCount * ReversiGame.gameSize * ReversiGame.gameSize];
            int[] players = new int[parallelCount];
            int[] moves = new int[parallelCount * 2];
            int[] possibleMoves = new int[parallelCount * ReversiGame.gameSize * ReversiGame.gameSize * 2];
            int[] possibleMovesLength = new int[parallelCount];

            ReversiGame[] games = new ReversiGame[parallelCount];

            for (int i = 0; i < parallelCount; i++)
            {
                if (!randomizedPlayfield)
                    games[i] = new ReversiGame();
            }

            //Copy Game states
            for (int i = 0; i < parallelCount; i++)
            {
                if (!randomizedPlayfield)
                {
                    ReversiGame game = games[i];
                    players[i] = game.curPlayer;
                    Array.Copy(game.gameField, 0, gameFields, i * ReversiGame.gameSize * ReversiGame.gameSize, ReversiGame.gameSize * ReversiGame.gameSize);
                }
                else
                {
                    players[i] = randomizedPlayers[i];
                    Array.Copy(randomGameFields, i* ReversiGame.gameSize * ReversiGame.gameSize, gameFields, i * ReversiGame.gameSize * ReversiGame.gameSize, ReversiGame.gameSize * ReversiGame.gameSize);
                }
            }

            //Create Buffers
            lock (possibleMovesBufferStack)
                lock (possibleMovesLengthBufferStack)
                    lock (playerBufferStack)
                        lock (gameFieldBufferStack)
                            lock (movesBufferStack)
                                lock (weightsBot1BufferStack)
                                    lock (weightsBot2BufferStack)
                                        lock(startingPlayersBufferStack)
                                        {
                                            possibleMovesBufferStack[stackCounter] = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(possibleMoves);

                                            possibleMovesLengthBufferStack[stackCounter] = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(possibleMovesLength);

                                            playerBufferStack[stackCounter] = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(players);

                                            gameFieldBufferStack[stackCounter] = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(gameFields);

                                            movesBufferStack[stackCounter] = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(moves);

                                            lock (weight1Cache)
                                                lock (weight2Cache)
                                                {
                                                    weightsBot1BufferStack[stackCounter] = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(weight1Cache);

                                                    weightsBot2BufferStack[stackCounter] = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(weight2Cache);
                                                }

                                            startingPlayersBufferStack[stackCounter] = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(randomizedPlayers);

                                            stackCounter++;

                                            incommingBuffers--;
                                        }
        }

        static void popBufferStack()
        {
            while (stackCounter == 0)
            {
                //Console.WriteLine("Waiting for Buffer generation...");
                Thread.Sleep(100);
            }

            lock (possibleMovesBufferStack)
                lock (possibleMovesLengthBufferStack)
                    lock (playerBufferStack)
                        lock (gameFieldBufferStack)
                            lock (movesBufferStack)
                                lock (weightsBot1BufferStack)
                                    lock (weightsBot2BufferStack)
                                        lock (startingPlayersBufferStack)
                                        {
                                            possibleMovesBuffer = possibleMovesBufferStack[stackCounter - 1];

                                            possibleMovesLengthBuffer = possibleMovesLengthBufferStack[stackCounter - 1];

                                            playerBuffer = playerBufferStack[stackCounter - 1];

                                            gameFieldBuffer = gameFieldBufferStack[stackCounter - 1];

                                            movesBuffer = movesBufferStack[stackCounter - 1];

                                            weightsBot1Buffer = weightsBot1BufferStack[stackCounter - 1];

                                            weightsBot2Buffer = weightsBot2BufferStack[stackCounter - 1];

                                            startingPlayerBuffer = startingPlayersBufferStack[stackCounter - 1];

                                            stackCounter--;
                                        }
        }

        static void clearBuffers()
        {
            gameFieldBufferStack = new ReadWriteBuffer<int>[bufferSize];
            playerBufferStack = new ReadWriteBuffer<int>[bufferSize];
            possibleMovesBufferStack = new ReadWriteBuffer<int>[bufferSize];
            possibleMovesLengthBufferStack = new ReadWriteBuffer<int>[bufferSize];
            movesBufferStack = new ReadWriteBuffer<int>[bufferSize];
            weightsBot1BufferStack = new ReadWriteBuffer<float>[bufferSize];
            weightsBot2BufferStack = new ReadWriteBuffer<float>[bufferSize];
            startingPlayersBufferStack = new ReadWriteBuffer<int>[bufferSize];

            incommingBuffers = 0;
            stackCounter = 0;
        }

        static void updateWeightBuffers(float[] weight1, float[] weight2)
        {
            lock(weight1Cache)
                lock(weight2Cache)
                {
                    weight1Cache = weight1;
                    weight2Cache = weight2;
                }
        }

        static Thread t;
        static Match[] simulateMatches(Mutation bot1, Mutation bot2, int parallelCount = 30000, int seriesCount = 5)
        {
            Match[] matchResults = new Match[parallelCount * seriesCount];

            for (int i = 0; i < parallelCount * seriesCount; i++)
            {
                Match m = new Match();
                matchResults[i] = m;
            }

            Stopwatch fullTimer, stepTimer;

            updateWeightBuffers(bot1.weights, bot2.weights);
            clearBuffers();

            /*if (t==null)
            {
                t = new Thread(() => bufferThreadAllocator(parallelCount));
                t.Start();
            }*/
            //calculateBuffer();

            fullTimer = Stopwatch.StartNew();
            stepTimer = Stopwatch.StartNew();

            for (int j = 0; j < seriesCount; j++)
            {
                for (int i = 0; i < parallelCount; i++)
                {
                    matchResults[i + j * parallelCount].starter = randomizedPlayers[i];
                }

                //Create Buffers
                calculateBuffer(parallelCount);
                popBufferStack();

                //Console.WriteLine("Initialization : " + stepTimer.ElapsedMilliseconds + "ms");
                stepTimer.Restart();

                int[] gameFieldsFull = new int[parallelCount * ReversiGame.gameSize * ReversiGame.gameSize];
                int[] gameFieldLocal = new int[parallelCount * ReversiGame.gameSize * ReversiGame.gameSize];

                for (int z = 0; z < (ReversiGame.gameSize * ReversiGame.gameSize - 4) / 2; z++)
                {
                    //Get all Possible moves
                    GraphicsDevice.GetDefault().For(parallelCount, new getPossibleMoves(gameFieldBuffer, playerBuffer, possibleMovesBuffer, possibleMovesLengthBuffer));

                    //Generate bot1 movements
                    GraphicsDevice.GetDefault().For(parallelCount, new botMasking(gameFieldBuffer, playerBuffer, possibleMovesBuffer, possibleMovesLengthBuffer, weightsBot1Buffer, weightsBot2Buffer, movesBuffer, startingPlayerBuffer));

                    // Run one game tick
                    GraphicsDevice.GetDefault().For(parallelCount, new simulateMoves(gameFieldBuffer, playerBuffer, movesBuffer));

                    // Flip Players
                    GraphicsDevice.GetDefault().For(parallelCount, new FlipPlayers(playerBuffer));

                    //Get all Possible moves
                    GraphicsDevice.GetDefault().For(parallelCount, new getPossibleMoves(gameFieldBuffer, playerBuffer, possibleMovesBuffer, possibleMovesLengthBuffer));

                    //Generate bot2 movements
                    GraphicsDevice.GetDefault().For(parallelCount, new botMasking(gameFieldBuffer, playerBuffer, possibleMovesBuffer, possibleMovesLengthBuffer, weightsBot1Buffer, weightsBot2Buffer, movesBuffer, startingPlayerBuffer));;

                    // Run one game tick
                    GraphicsDevice.GetDefault().For(parallelCount, new simulateMoves(gameFieldBuffer, playerBuffer, movesBuffer));

                    // Flip Players
                    GraphicsDevice.GetDefault().For(parallelCount, new FlipPlayers(playerBuffer));
                }

                int[] winners = new int[parallelCount];
                int[] loosers = new int[parallelCount];
                int[] winnerScore = new int[parallelCount];
                int[] looserScore = new int[parallelCount];
                int[] bot1Start = new int[parallelCount];
                int[] bot2Start = new int[parallelCount];

                ReadWriteBuffer<int> winnersBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(winners);
                ReadWriteBuffer<int> loosersBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(loosers);
                ReadWriteBuffer<int> winnerScoreBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(winnerScore);
                ReadWriteBuffer<int> looserScoreBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(looserScore);

                GraphicsDevice.GetDefault().For(parallelCount, new getMatchResults(gameFieldBuffer, winnersBuffer, loosersBuffer, winnerScoreBuffer, looserScoreBuffer, startingPlayerBuffer));

                winnersBuffer.CopyTo(winners);
                loosersBuffer.CopyTo(loosers);
                winnerScoreBuffer.CopyTo(winnerScore);
                looserScoreBuffer.CopyTo(looserScore);

                for (int i = 0; i < parallelCount; i++)
                {
                    if (winners[i] == 1)
                        matchResults[i + j * parallelCount].Winner = ReversiGame.Human;

                    if (winners[i] == 2)
                        matchResults[i + j * parallelCount].Winner = ReversiGame.Bot;

                    if (winners[i] == 0)
                        matchResults[i + j * parallelCount].Winner = 0;
                }

                int[] gameFields = new int[parallelCount * ReversiGame.gameSize * ReversiGame.gameSize];
                gameFieldBuffer.CopyTo(gameFields);

                /*while (stackCounter==0)
                {
                    //Console.WriteLine("Waiting for Buffer generation...");
                    Thread.Sleep(100);
                }*/

                //Console.WriteLine("done cycle");
            }

            //Console.WriteLine("done " + parallelCount*seriesCount + " matches in " + fullTimer.ElapsedMilliseconds + "ms");

            return matchResults;
        }

        static int[] randomGameFields;
        static int[] randomizedPlayers;
        //static int[] startingPlayers;

        public static float[] mutateWeights(float[] weights)
        {
            Random r = new Random();

            float[] newArr = new float[weights.Length];

            Array.Copy(weights,0, newArr, 0, weights.Length);

            newArr = normalizeArray(newArr);

            for (int i = 0; i < newArr.Length; i++)
            {
                float mutate = 0.3333f;
                float amplitude = 0.5f;

                if(r.NextSingle() <= mutate)
                {
                    int direction = r.Next(0,2) == 0?-1:1;
                    newArr[i] += r.NextSingle() * amplitude * direction;
                }
            }

            newArr = normalizeArray(newArr);

            //Console.WriteLine();

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    //Console.Write(newArr[x + y * 8] + " ");
                }
                //Console.WriteLine();
            }

            //Console.WriteLine();

            return newArr;
        }

        public static float[] normalizeArray(float[] arr)
        {
            float max = -9999;
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] > max)
                {
                    max = arr[i];
                }
            }

            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = arr[i] / max;
            }

            return arr;
        }

        static EvaluatedMatches evaluateMatches(Match[] results)
        {
            EvaluatedMatches evaluatedMatches = new EvaluatedMatches();

            int matchCount = results.Length;

            int wins = 0;
            int tie = 0;
            int wins2 = 0;
            int start = 0;
            int start2 = 0;

            for (int i = 0; i < results.Length; i++)
            {
                if (results[i].Winner == 0) tie++;
                if (results[i].Winner == 1) wins++;
                if (results[i].Winner == 2) wins2++;
                if (results[i].starter == 1) start++;
                if (results[i].starter == 2) start2++;
            }

            evaluatedMatches.wins = wins;
            evaluatedMatches.looses = wins2;
            evaluatedMatches.ties = tie;
            evaluatedMatches.startCount = start;
            evaluatedMatches.secondCount = start2;

            evaluatedMatches.winRate = (wins * 100f / matchCount);
            evaluatedMatches.looseRate = (wins2 * 100f / matchCount);
            evaluatedMatches.tieRate = (tie * 100f / matchCount);

            return evaluatedMatches;
        }

        static public void printMatchResults(Match[] results)
        {
            int matchCount = results.Length;

            int wins = 0;
            int tie = 0;
            int wins2 = 0;
            int start = 0;
            int start2 = 0;

            for (int i = 0; i < results.Length; i++)
            {
                if (results[i].Winner == 0) tie++;
                if (results[i].Winner == 1) wins++;
                if (results[i].Winner == 2) wins2++;
                if (results[i].starter == 1) start++;
                if (results[i].starter == 2) start2++;
            }
            //Console.WriteLine(tie);
            //Console.WriteLine(wins);
            //Console.WriteLine(wins2);
            //Console.WriteLine(start);
            //Console.WriteLine(start2);

            Console.WriteLine();

            Console.WriteLine("Tie : " + (tie * 100f / matchCount) + "%");
            Console.WriteLine("Starter Wins : " + (wins * 100f / matchCount) + "%");
            Console.WriteLine("Second Wins : " + (wins2 * 100f / matchCount) + "%");

            Console.WriteLine();
        }

        static ReadWriteBuffer<int> randomBuffer, randomStopBuffer;

        static void generateNewRandomness()
        {
            Random r = new Random();

            int minRandomCount = 8;
            int maxRandomCount = 16;

            int[] randomStop = new int[parallelCount];
            int start1 = 0, start2 = 0;
            for (int i = 0; i < parallelCount; i++)
            {
                int rand = r.Next(minRandomCount, maxRandomCount);
                if (start2 < start1) rand++;
                randomStop[i] = rand;

                if (rand % 2 == 0)
                    start1++;
                else
                    start2++;
            }

            ReversiGame[] games = new ReversiGame[parallelCount];

            for (int i = 0; i < parallelCount; i++)
            {
                games[i] = new ReversiGame();
            }

            //Copy Game states
            for (int i = 0; i < parallelCount; i++)
            {
                ReversiGame game = games[i];
                randomizedPlayers[i] = game.curPlayer;
                Array.Copy(game.gameField, 0, randomGameFields, i * ReversiGame.gameSize * ReversiGame.gameSize, ReversiGame.gameSize * ReversiGame.gameSize);
            }


            randomStopBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(randomStop);

            playerBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(randomizedPlayers);

            gameFieldBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(randomGameFields);

            for (int z = 0; z < ReversiGame.gameSize * ReversiGame.gameSize - 4; z++)
            {
                int[] randomNumbers = new int[parallelCount];

                for (int i = 0; i < parallelCount; i++)
                {
                    randomNumbers[i] = r.Next(0, 10000);
                }

                randomBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(randomNumbers);

                //Get all Possible moves
                GraphicsDevice.GetDefault().For(parallelCount, new getPossibleMoves(gameFieldBuffer, playerBuffer, possibleMovesBuffer, possibleMovesLengthBuffer));

                //Generate bot movements
                GraphicsDevice.GetDefault().For(parallelCount, new botRandom(gameFieldBuffer, playerBuffer, possibleMovesBuffer, possibleMovesLengthBuffer, randomBuffer, movesBuffer, randomStopBuffer, z));

                // Run one game tick
                GraphicsDevice.GetDefault().For(parallelCount, new simulateMoves(gameFieldBuffer, playerBuffer, movesBuffer));

                // Flip Players
                GraphicsDevice.GetDefault().For(parallelCount, new FlipPlayers(playerBuffer));

            }

            gameFieldBuffer.CopyTo(randomGameFields);
            playerBuffer.CopyTo(randomizedPlayers);
        }

        static int parallelCount = 250000;
        static Mutation baseBot;
        public static void testComputeShader()
        {
            startWeights = normalizeArray(startWeights);

            clearBuffers();

            int matchCount = 1000000;
            int seriesCount = 1;

            weight1Cache = new float[parallelCount * ReversiGame.gameSize * ReversiGame.gameSize];
            weight2Cache = new float[parallelCount * ReversiGame.gameSize * ReversiGame.gameSize];


            gameFieldsGenerated = new int[parallelCount * ReversiGame.gameSize * ReversiGame.gameSize];

            //Reserve Memory for Games
            randomGameFields = new int[parallelCount * ReversiGame.gameSize * ReversiGame.gameSize];
            randomizedPlayers = new int[parallelCount];
            int[] moves = new int[parallelCount * 2];
            int[] possibleMoves = new int[parallelCount * ReversiGame.gameSize * ReversiGame.gameSize * 2];
            int[] possibleMovesLength = new int[parallelCount];

            //Create Buffers
            possibleMovesBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(possibleMoves);

            possibleMovesLengthBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(possibleMovesLength);

            movesBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(moves);

            generateNewRandomness();
            
            //possibleMovesBuffer.CopyTo(possibleMoves);

            // Print Playfields
            /*for (int i = 0; i < 20; i++)
            {
                int[] gameField = new int[ReversiGame.gameSize * ReversiGame.gameSize];
                Array.Copy(randomGameFields, ReversiGame.gameSize * ReversiGame.gameSize * i, gameField, 0, ReversiGame.gameSize * ReversiGame.gameSize);

                int[] possible = new int[ReversiGame.gameSize * ReversiGame.gameSize*2];
                Array.Copy(possibleMoves, ReversiGame.gameSize * ReversiGame.gameSize * i * 2, possible, 0, ReversiGame.gameSize * ReversiGame.gameSize*2);

                Console.WriteLine(randomizedPlayers[i]);
                //ReversiGame.printGameField(gameField);
                Console.WriteLine("--------");
            }*/

            Console.WriteLine("Done generating random gamestates.");
            
            int generation = 1;
            int keepCount = 2;

            float[][] fullWeights = new float[evolutionSize][];
            float[][] bestWeights = new float[evolutionSize][];

            for (int i = 0; i < evolutionSize; i++)
            {
                bestWeights[i] = startWeights;
            }

            baseBot = new Mutation();
            baseBot.weights = getFullWeights(startWeights);

            while (true)
            {
                Console.WriteLine("Running Generation " + generation);
                Console.WriteLine();

                for (int i = 0; i < evolutionSize; i++)
                {
                    fullWeights[i] = getFullWeights(bestWeights[i]);
                }

                EvaluatedMatchGrouping matches = runEvolution(fullWeights);

                int[] bestBots = matches.getBestBots(keepCount);

                for (int i = 0; i < keepCount; i++)
                {
                    int botId = bestBots[i];
                    int wins = matches.getWins(botId);
                    bestWeights[i] = bots[botId].weights;

                    Console.WriteLine("Bot " + botId);
                    Console.WriteLine("Wins : " + wins);

                    EvaluatedMatches[] evaluatedMatches = new EvaluatedMatches[2];

                    Match[] results1 = simulateMatches(bots[botId], baseBot, parallelCount, seriesCount);
                    Match[] results2 = simulateMatches(baseBot, bots[botId], parallelCount, seriesCount);

                    evaluatedMatches[0] = evaluateMatches(results1);
                    evaluatedMatches[0].bot1Index = 0;
                    evaluatedMatches[0].bot2Index = 1;
                    evaluatedMatches[0].gameCount = parallelCount;

                    evaluatedMatches[1] = evaluateMatches(results2);
                    evaluatedMatches[1].bot1Index = 1;
                    evaluatedMatches[1].bot2Index = 0;
                    evaluatedMatches[1].gameCount = parallelCount;

                    EvaluatedMatchGrouping matchGrouping = new EvaluatedMatchGrouping(evaluatedMatches);

                    Console.Write("Dominance : ");
                    Console.WriteLine(matchGrouping.getWinDiff(0, 1));

                    Console.WriteLine("Weights");
                    printWeights(bestWeights[i]);
                    Console.WriteLine();
                }

                Random r = new Random();

                for (int i = keepCount; i < evolutionSize; i++)
                {
                    bestWeights[i] = mutateWeights(bestWeights[r.Next(0,keepCount+1)]);
                }

                Console.WriteLine("-------------------");
                generation++;
            }

            

            //seriesCount games will result in same outcome due to same random maps!!
            //Match[] results = simulateMatches(bots[0], bots[1], parallelCount, seriesCount);

            //printMatchResults(bots[0], bots[1]);
        }

        static int evolutionSize = 8;
        public static Mutation[] bots = new Mutation[evolutionSize];

        public static void printWeights(float[] weights)
        {
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    Console.Write(weights[x + y * 8] + " ");
                }
                Console.WriteLine();
            }
        }

        public static float[] getFullWeights(float[] weights)
        {
            float[] fullWeights = new float[ReversiGame.gameSize * ReversiGame.gameSize * parallelCount];

            for (int i = 0; i < parallelCount; i++)
            {
                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        fullWeights[(x + y * 8) + (i * 8 * 8)] = weights[x + y * 8]; //mutateWeights(startWeights[0])
                    }
                }
            }

            return fullWeights;
        }

        public static EvaluatedMatchGrouping runEvolution(float[][] startWeights)
        {
            Stopwatch sw = Stopwatch.StartNew();
            int seriesCount = 1;

            for (int i = 0; i < evolutionSize; i++)
            {
                bots[i] = new Mutation();
                bots[i].weights = startWeights[i];
            }

            EvaluatedMatches[] evaluatedMatches = new EvaluatedMatches[evolutionSize*evolutionSize];

            //Since i can't find the source of bias towards bot1 we just run every constilation to equal it out lol
            for (int i = 0; i < evolutionSize; i++)
            {
                Console.WriteLine(i + "/" + evolutionSize);
                Mutation bot1 = bots[i];
                for (int j = 0; j < evolutionSize; j++)
                {
                    if (i == j) continue;

                    generateNewRandomness();
                    //Console.WriteLine("Bot" + i + " vs Bot" + j);
                    Mutation bot2 = bots[j];

                    Match[] results = simulateMatches(bot1, bot2, parallelCount, seriesCount);

                    evaluatedMatches[j+i*evolutionSize] = evaluateMatches(results);
                    evaluatedMatches[j + i * evolutionSize].bot1Index = i;
                    evaluatedMatches[j + i * evolutionSize].bot2Index = j;
                    evaluatedMatches[j + i * evolutionSize].gameCount = parallelCount;
                }
            }

            EvaluatedMatchGrouping matchGrouping = new EvaluatedMatchGrouping(evaluatedMatches);

            Console.WriteLine("Done Evolution in " + sw.ElapsedMilliseconds + "ms");

            return matchGrouping;
        }

        public class Mutation()
        {
            public float[] weights;
        }
    }
}
