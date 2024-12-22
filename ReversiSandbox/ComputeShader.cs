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
        public readonly partial struct getMatchResults(ReadWriteBuffer<int> gameField, ReadWriteBuffer<int> winners, ReadWriteBuffer<int> loosers, ReadWriteBuffer<int> winnerScore, ReadWriteBuffer<int> looserScore) : IComputeShader
        {
            public void Execute()
            {
                int gameIndex = ThreadIds.X;
                int score1 = count_stones(ReversiGame.Human, gameIndex);
                int score2 = count_stones(ReversiGame.Bot, gameIndex);

                if (score1 > score2)
                {
                    winners[gameIndex] = ReversiGame.Human;
                    loosers[gameIndex] = ReversiGame.Bot;
                    winnerScore[gameIndex] = score1;
                    looserScore[gameIndex] = score2;
                }

                if (score1 < score2)
                {
                    winners[gameIndex] = ReversiGame.Bot;
                    loosers[gameIndex] = ReversiGame.Human;
                    winnerScore[gameIndex] = score2;
                    looserScore[gameIndex] = score1;
                }

                if (score1 == score2)
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
        public readonly partial struct botMasking(ReadWriteBuffer<int> gameField, ReadWriteBuffer<int> players, ReadWriteBuffer<int> moves, ReadWriteBuffer<int> movesLength, ReadWriteBuffer<float> weights, ReadWriteBuffer<int> move) : IComputeShader
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

                    float score = weights[(x + y * ReversiGame.gameSize) + (gameIndex * ReversiGame.gameSize * ReversiGame.gameSize)] * getTilesCaptured(x, y, player, gameIndex);
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
                    return;

                Algorithm(player, gameIndex);
            }

            public void Algorithm(int player, int gameIndex)
            {
                int moveIndex = randomNumbers[gameIndex] % movesLength[gameIndex];

                move[gameIndex * 2] = moves[gameIndex * ReversiGame.gameSize * ReversiGame.gameSize * 2 + moveIndex * 2];
                move[gameIndex * 2 + 1] = moves[gameIndex * ReversiGame.gameSize * ReversiGame.gameSize * 2 + moveIndex * 2 + 1];
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

        static ReadWriteBuffer<int>[] gameFieldBufferStack, playerBufferStack, possibleMovesBufferStack, possibleMovesLengthBufferStack, movesBufferStack;
        static ReadWriteBuffer<float>[] weightsBufferStack;
        static int stackCounter = 0;
        static int incommingBuffers = 0;
        const int bufferSize = 3;
        static int[] gameFieldsGenerated;

        static ReadWriteBuffer<int> gameFieldBuffer, playerBuffer, possibleMovesBuffer, possibleMovesLengthBuffer, movesBuffer;
        static ReadWriteBuffer<float> weightsBuffer;
        
        static void bufferThreadAllocator(int parallelCount)
        {
            while (true)
            {
                while (bufferSize <= (stackCounter + incommingBuffers + 1)) { }

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

        static Match[] simulateMatches(Bot bot1, Bot bot2, int parallelCount = 30000, int seriesCount = 5)
        {
            Match[] matchResults = new Match[parallelCount * seriesCount];
            int matchIndex = 0;

            Stopwatch fullTimer, stepTimer;

            Thread t;
            t = new Thread(() => bufferThreadAllocator(parallelCount));
            t.Start();

            while (stackCounter == 0)
            {
                //Console.WriteLine("Waiting for Buffer generation...");
                Thread.Sleep(100);
            }

            fullTimer = Stopwatch.StartNew();
            stepTimer = Stopwatch.StartNew();

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

                int[] winners = new int[parallelCount];
                int[] loosers = new int[parallelCount];
                int[] winnerScore = new int[parallelCount];
                int[] looserScore = new int[parallelCount];

                ReadWriteBuffer<int> winnersBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(winners);
                ReadWriteBuffer<int> loosersBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(loosers);
                ReadWriteBuffer<int> winnerScoreBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(winnerScore);
                ReadWriteBuffer<int> looserScoreBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(looserScore);

                GraphicsDevice.GetDefault().For(parallelCount, new getMatchResults(gameFieldBuffer, winnersBuffer, loosersBuffer, winnerScoreBuffer, looserScoreBuffer));

                winnersBuffer.CopyTo(winners);
                loosersBuffer.CopyTo(loosers);
                winnerScoreBuffer.CopyTo(winnerScore);
                looserScoreBuffer.CopyTo(looserScore);

                for (int i = 0; i < parallelCount; i++)
                {
                    Match m = new Match();

                    switch (winners[i])
                    {
                        case ReversiGame.Human:
                            m.Winner = bot1;
                            m.bot1Score = winnerScore[i];
                            break;

                        case ReversiGame.Bot:
                            m.Winner = bot2;
                            m.bot2Score = winnerScore[i];
                            break;

                        case 0:
                            m.Winner = null;
                            m.bot1Score = winnerScore[i];
                            break;
                    }

                    switch (loosers[i])
                    {
                        case ReversiGame.Human:
                            m.Looser = bot1;
                            m.bot1Score = looserScore[i];
                            break;

                        case ReversiGame.Bot:
                            m.Looser = bot2;
                            m.bot2Score = looserScore[i];
                            break;

                        case 0:
                            m.Looser = null;
                            m.bot1Score = looserScore[i];
                            break;
                    }

                    matchResults[matchIndex++] = m;
                }

                int[] gameFields = new int[parallelCount * ReversiGame.gameSize * ReversiGame.gameSize];
                gameFieldBuffer.CopyTo(gameFields);

                // Print Playfields
                /*for (int i = 0; i < 100; i++)
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

            /*for (int i = 0; i < matchIndex; i++)
            {
                Console.WriteLine(matchResults[i].bot1Score.ToString());
            }*/

            Console.WriteLine("done " + parallelCount*seriesCount + " matches in " + fullTimer.ElapsedMilliseconds + "ms");

            return matchResults;
        }

        static int[] randomGameFields;
        static int[] randomizedPlayers;
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
            
            int parallelCount = 2000000;
            int maxRandomCount = 45;

            Random r = new Random();

            int[] randomNumbers = new int[parallelCount];
            int[] randomStop = new int[parallelCount];
            for (int i = 0; i < parallelCount; i++)
            {
                randomNumbers[i] = r.Next(0, 1000);
                randomStop[i] = r.Next(0, maxRandomCount);
            }

            gameFieldsGenerated = new int[parallelCount * ReversiGame.gameSize * ReversiGame.gameSize];

            ReadWriteBuffer<int> randomBuffer, randomStopBuffer;

            //Reserve Memory for Games
            randomGameFields = new int[parallelCount * ReversiGame.gameSize * ReversiGame.gameSize];
            randomizedPlayers = new int[parallelCount];
            int[] moves = new int[parallelCount * 2];
            int[] possibleMoves = new int[parallelCount * ReversiGame.gameSize * ReversiGame.gameSize * 2];
            int[] possibleMovesLength = new int[parallelCount];

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

            //Create Buffers
            possibleMovesBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(possibleMoves);

            possibleMovesLengthBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(possibleMovesLength);

            playerBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(randomizedPlayers);

            gameFieldBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(randomGameFields);

            movesBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(moves);

            randomBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(randomNumbers);

            randomStopBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(randomStop);

            for (int z = 0; z < ReversiGame.gameSize * ReversiGame.gameSize - 4; z++)
            {

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
            possibleMovesBuffer.CopyTo(possibleMoves);

            /*// Print Playfields
            for (int i = 0; i < 100; i++)
            {
                int[] gameField = new int[ReversiGame.gameSize * ReversiGame.gameSize];
                Array.Copy(randomGameFields, ReversiGame.gameSize * ReversiGame.gameSize * i, gameField, 0, ReversiGame.gameSize * ReversiGame.gameSize);

                int[] possible = new int[ReversiGame.gameSize * ReversiGame.gameSize*2];
                Array.Copy(possibleMoves, ReversiGame.gameSize * ReversiGame.gameSize * i * 2, possible, 0, ReversiGame.gameSize * ReversiGame.gameSize*2);

                for (int q = 0; q < possible.Length; q++)
                {
                    Console.WriteLine(possible[q]);
                }
                ReversiGame.printGameField(gameField);
                Console.WriteLine("--------");
            }*/

            Console.WriteLine("Done generating random gamestates.");
            //while (true) { }
            simulateMatches(bot1, bot2, parallelCount, 5);
        }
    }
}
