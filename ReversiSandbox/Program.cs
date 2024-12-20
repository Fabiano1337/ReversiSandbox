using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading;

namespace ReversiSandbox // Note: actual namespace depends on the project name.
{
    class Program
    {
        const bool randomizedPlayfield = true;
        static void Main(string[] args)                 // Todo : Add Training for Masking
        {
            trainMask();
            while (true) { };
            //humanMatch(new BotRandom());
            //return;
            int sampleSize = 100000;
            List<Match> matches = new List<Match>();
            int threadCount = 32;
            Console.WriteLine("Starting " + threadCount.ToString() + " Threads.");

            List<Thread> threads = new List<Thread>();

            for (int i = 0; i < threadCount; i++) // Favored for bot starting
            {
                Bot bot1 = new BotMasking();
                //Bot bot2 = new BotNeo(5);
                Bot bot2 = new BotValueMove();
                Thread t = new Thread(() => threadMatch(matches,bot1,bot2, sampleSize / threadCount));
                t.Start();
                threads.Add(t);

                //matches.Add(simulateMatch(bot1, bot2));
            }
            while(true)
            {
                bool running = false;

                foreach (Thread t in threads)
                {
                    if (t.IsAlive) running = true;
                }

                if (!running)
                    break;
            }
            evaluateMatches(matches);

            //Console.WriteLine(match.Winner);
            //Console.WriteLine(match.bot1Score);
            //Console.WriteLine(match.bot2Score);
        }

        static void trainMask()
        {
            int evolutionSize = 4;
            int sampleSize = 4096;
            int threadCount = 32;

            List<Bot> bots = new List<Bot>();
            List<Thread> threads = new List<Thread>();
            List<Match> matches = new List<Match>();

            for (int i = 0; i < evolutionSize; i++)
            {
                float[,] mask = new float[ReversiGame.gameSize, ReversiGame.gameSize];
                mask = mutateMask(mask);
                Bot bot = new BotMasking(mask);

                bots.Add(bot);
            }

            for (int i = 0; i < evolutionSize; i++)
            {
                for (int j = 0; j < evolutionSize; j++)
                {
                    for (int x = 0; x < threadCount; x++)
                    {
                        Bot bot1 = bots[i];
                        Bot bot2 = bots[j];
                        Thread t = new Thread(() => threadMatch(matches, bot1, bot2, sampleSize / threadCount));
                        t.Start();
                        threads.Add(t);
                    }
                }
            }
            waitForThreads(threads);
            Dictionary<Bot,int> weights = new Dictionary<Bot, int>();

            foreach (var bot in bots)
            {
                weights[bot] = 0;
            }

            foreach (Match m in matches)
            {
                if (m.Winner == null)
                    continue;

                if (m.Looser == null)
                    continue;

                weights[m.Winner]++;
                weights[m.Looser]++;
            }

            foreach (var bot in weights)
            {
                Console.WriteLine(bot.Value);
            }

            Console.WriteLine("Done Evolution");
            //evaluateMatches(matches);

        }

        static void waitForThreads(List<Thread> threads)
        {
            while (true)
            {
                bool running = false;

                foreach (Thread t in threads)
                {
                    if (t.IsAlive) running = true;
                }

                if (!running)
                    break;
            }
        }

        static float[,] mutateMask(float[,] oldMask)
        {
            float randomness = 0.1f;
            float amplitude = 2;

            Random r = new Random();

            float[,] randomWeights = new float[ReversiGame.gameSize, ReversiGame.gameSize];

            for (int x = 0; x < ReversiGame.gameSize; x++)
            {
                for (int y = 0; y < ReversiGame.gameSize; y++)
                {
                    float weight = (float)(r.NextDouble() * amplitude * 2);
                    bool negative = r.Next(0, 2) == 1;

                    if (negative)
                        weight = -weight;

                    if (r.NextDouble() < randomness)
                        randomWeights[x, y] = weight;
                    else
                        randomWeights[x, y] = 0;
                }
            }

            for (int x = 0; x < ReversiGame.gameSize; x++)
            {
                for (int y = 0; y < ReversiGame.gameSize; y++)
                {
                    oldMask[x,y] = oldMask[x,y]+randomWeights[x,y];
                }
            }

            return oldMask;
        }

        static void threadMatch(List<Match> matches, Bot bot1, Bot bot2, int sampleSize)
        {
            for (int i = 0; i < sampleSize; i++)
            {
                Match m = simulateMatch(bot1, bot2);

                lock (matches)
                    matches.Add(m);
            }

            //Console.WriteLine("Thread Done.");
        }

        static void evaluateMatches(List<Match> matches)
        {
            Console.WriteLine(matches[0].bot1.ToString() + " vs " + matches[0].bot2.ToString() + " in " + matches.Count + " matches"+(randomizedPlayfield?", randomized":", default")+".");
            Dictionary<Type, int> wins = new Dictionary<Type, int>();
            float tie = 0;
            float gameCount = matches.Count;
            float tiePercent, winPercent;

            var type = typeof(Bot);
            var bots = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p));

            foreach (var bot in bots)
            {
                if (bot.IsAbstract) continue;
                wins[bot]=0;
            }

            foreach (var match in matches)
            {
                foreach (var bot in bots)
                {
                    if (bot.IsAbstract)
                        continue;

                    if (match.Winner == null)
                    {
                        tie++;
                        break;
                    }

                    if (bot != match.Winner.GetType())
                        continue;

                    wins[bot]++;
                }
            }

            tiePercent = tie * 100f / gameCount;
            Console.WriteLine("Tie: " + tiePercent.ToString()+"%");
            foreach (var item in wins)
            {
                winPercent = item.Value*100f / gameCount;
                Console.WriteLine(item.Key.ToString() + " wins : " + winPercent + "%");
            }

            Console.WriteLine("---------------------------------------------------------------------");
        }

        static void humanMatch(Bot bot)
        {
            ReversiGame game = new ReversiGame(); // X = Human, O = Bot

            game.printGameField();
            game.printPossibleMoves();
            while (true)
            {

                int xScore = ReversiGame.count_stones(game, Player.Human);
                int oScore = ReversiGame.count_stones(game, Player.Bot);
                Console.WriteLine("X's Score : " + (xScore-oScore).ToString());
                
                Console.WriteLine(game.curPlayer.ToString() + "'s Turn");
                Console.Write("Input Position : ");
                string cmd = Console.ReadLine();
                Console.WriteLine();
                Position pos = cmdToPos(cmd);
                //Console.WriteLine(ReversiGame.getTilesCaptured(game.gameField, pos, game.curPlayer));

                game.move(pos);
                game.printGameField();

                game.switchPlayer();

                Position botMove = bot.generateMove(game);
                Console.WriteLine("Bots move : " + positionToString(botMove));
                game.move(botMove);

                game.printGameField();

                game.switchPlayer();

                game.printPossibleMoves();
            }
        }

        static Match simulateMatch(Bot bot1, Bot bot2)
        {
            ReversiGame game;
            if(randomizedPlayfield)
                game = new ReversiGame(ReversiGame.generateRandomGameField());
            else
                game = new ReversiGame();

            Position botMove;

            Random r = new Random();
            bool reverse = r.Next(0,2)==1?true:false;

            if(reverse)
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
                //Console.WriteLine("Bot1 Won!");
                match.Winner = bot1;
                match.Looser = bot2;
            }
            else if (bot2Score > bot1Score)
            {
                //Console.WriteLine("Bot2 Won!");
                match.Winner = bot2;
                match.Looser = bot1;
            }
            else if (bot1Score == bot2Score)
            {
                //Console.WriteLine("Tie!");
                match.Winner = null;
                match.Looser = null;
            }

            return match;
        } 

        static string positionToString (Position pos)
        {
            string output = "";

            switch(pos.x)
            {
                case 0:
                    output += "A";
                    break;
                case 1:
                    output += "B";
                    break;
                case 2:
                    output += "C";
                    break;
                case 3:
                    output += "D";
                    break;
                case 4:
                    output += "E";
                    break;
                case 5:
                    output += "F";
                    break;
                case 6:
                    output += "G";
                    break;
                case 7:
                    output += "H";
                    break;
            }

            output += (pos.y+1).ToString();

            return output;
        }

        static Position cmdToPos(string cmd)
        {
            string xChar = cmd.Substring(0, 1).ToLower();
            int x = -1;
            int y = Int32.Parse(cmd.Substring(1,1))-1;

            switch (xChar)
            {
                case "a":
                    x = 0;
                    break;
                case "b":
                    x = 1;
                    break;
                case "c":
                    x = 2;
                    break;
                case "d":
                    x = 3;
                    break;
                case "e":
                    x = 4;
                    break;
                case "f":
                    x = 5;
                    break;
                case "g":
                    x = 6;
                    break;
                case "h":
                    x = 7;
                    break;
            }

            return new Position() { x = x, y = y };
        }
    }
}