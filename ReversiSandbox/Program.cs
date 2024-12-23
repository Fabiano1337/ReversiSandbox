using ComputeSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading;
using ReversiSandbox.ReversiBot;

namespace ReversiSandbox // Note: actual namespace depends on the project name.
{
    class Program
    {
        const bool randomizedPlayfield = true;
        static void Main(string[] args)                 // Todo : Add Training for Masking
        {
            ComputeShader.testComputeShader();
            //trainMask();
            while (true) { };
            //humanMatch(new BotRandom());
            //return;
            int sampleSize = 100000;
            List<Match> matches = new List<Match>();
            int threadCount = 32;
            Console.WriteLine("Starting " + threadCount.ToString() + " Threads.");

            List<Thread> threads = new List<Thread>();

            for (int i = 0; i < threadCount; i++) // Favored for Bot starting
            {
                Bot Bot1 = new BotRandom();
                //Bot Bot2 = new BotNeo(5);
                Bot Bot2 = new BotRandom();
                Thread t = new Thread(() => threadMatch(matches,Bot1,Bot2, sampleSize / threadCount));
                t.Start();
                threads.Add(t);

                //matches.Add(simulateMatch(Bot1, Bot2));
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
            //Console.WriteLine(match.Bot1Score);
            //Console.WriteLine(match.Bot2Score);
        }

        static void trainMask() // 2Do : https://github.com/Sergio0694/ComputeSharp/wiki/3.-Getting-started-%F0%9F%93%96 📖
        {
            int evolutionSize = 4;
            int sampleSize = 4096; // Get stuff to mutate more in the beninning 👍
            int threadCount = 32;

            List<Bot> Bots = new List<Bot>();
            List<Thread> threads = new List<Thread>();
            List<Match> matches = new List<Match>();

            for (int i = 0; i < evolutionSize; i++)
            {
                float[,] mask = new float[ReversiGame.gameSize, ReversiGame.gameSize];
                mask = mutateMask(mask);
                Bot Bot = new BotMasking(mask);

                Bots.Add(Bot);
            }

            for (int i = 0; i < evolutionSize; i++)
            {
                for (int j = 0; j < evolutionSize; j++)
                {
                    for (int x = 0; x < threadCount; x++)
                    {
                        Bot Bot1 = Bots[i];
                        Bot Bot2 = Bots[j];
                        Thread t = new Thread(() => threadMatch(matches, Bot1, Bot2, sampleSize / threadCount));
                        t.Start();
                        threads.Add(t);
                    }
                }
            }
            waitForThreads(threads);
            Dictionary<Bot,int> weights = new Dictionary<Bot, int>();

            foreach (var Bot in Bots)
            {
                weights[Bot] = 0;
            }

            foreach (Match m in matches)
            {
                if (m.Winner == 0)
                    continue;

                if (m.Looser == 0)
                    continue;

                //weights[m.Winner]++;
                //weights[m.Looser]++;
            }

            foreach (var Bot in weights)
            {
                Console.WriteLine(Bot.Value);
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

        static void threadMatch(List<Match> matches, Bot Bot1, Bot Bot2, int sampleSize)
        {
            for (int i = 0; i < sampleSize; i++)
            {
                Match m = simulateMatch(Bot1, Bot2);

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
            float tiePercent, bot1Percent, bot2Percent;
            int bot1Wins = 0, bot2Wins  = 0;

            for (int i = 0; i < matches.Count; i++)
            {
                if (matches[i].Winner == matches[i].starter)
                    bot1Wins++;

                if (matches[i].Winner != matches[i].starter)
                    bot2Wins++;

                if (matches[i].Winner == 0)
                    tie++;
            }

            /*var type = typeof(Bot);
            var Bots = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p));

            foreach (var Bot in Bots)
            {
                if (Bot.IsAbstract) continue;
                wins[Bot]=0;
            }

            foreach (var match in matches)
            {
                foreach (var Bot in Bots)
                {
                    if (Bot.IsAbstract)
                        continue;

                    if (match.Winner == 0)
                    {
                        tie++;
                        break;
                    }

                    if (Bot != match.Winner.GetType())
                        continue;

                    wins[Bot]++;
                }
            }*/

            bot1Percent = bot1Wins * 100f / gameCount;
            bot2Percent = bot2Wins * 100f / gameCount;
            tiePercent = tie * 100f / gameCount;
            Console.WriteLine("Bot1: " + bot1Percent.ToString() + "%");
            Console.WriteLine("Bot2: " + bot2Percent.ToString() + "%");
            Console.WriteLine("Tie: " + tiePercent.ToString() + "%");

            Console.WriteLine("---------------------------------------------------------------------");
        }

        static void humanMatch(Bot Bot)
        {
            ReversiGame game = new ReversiGame(); // X = Human, O = Bot

            game.printGameField();
            game.printPossibleMoves();
            while (true)
            {

                int xScore = ReversiGame.count_stones(game, ReversiGame.Human);
                int oScore = ReversiGame.count_stones(game, ReversiGame.Bot);
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

                Position BotMove = Bot.generateMove(game.gameField, game.getPossibleMoves(), game.curPlayer);
                Console.WriteLine("Bots move : " + positionToString(BotMove));
                game.move(BotMove);

                game.printGameField();

                game.switchPlayer();

                game.printPossibleMoves();
            }
        }

        static Match simulateMatch(Bot Bot1, Bot Bot2)
        {
            ReversiGame game;
            if(randomizedPlayfield)
                game = new ReversiGame(ReversiGame.generateRandomGameField());
            else
                game = new ReversiGame();

            Position BotMove;

            Random r = new Random();
            bool reverse = r.Next(0,2)==1?true:false;

            if(reverse)
            {
                Bot buffer = Bot1;
                Bot1 = Bot2;
                Bot2 = buffer;
            }

            while (true)
            {
                BotMove = Bot1.generateMove(game.gameField,game.getPossibleMoves(),game.curPlayer);
                game.move(BotMove);
                game.switchPlayer();
                //Console.WriteLine(Bot1.ToString() + " move : " + positionToString(BotMove));
                //game.printGameField();
                if (game.isEnded()) break;

                BotMove = Bot2.generateMove(game.gameField, game.getPossibleMoves(), game.curPlayer);
                game.move(BotMove);
                game.switchPlayer();
                //Console.WriteLine(Bot2.ToString() + " move : " + positionToString(BotMove));
                //game.printGameField();
                if (game.isEnded()) break;
            }
            int Bot1Score = ReversiGame.count_stones(game, ReversiGame.Human);
            int Bot2Score = ReversiGame.count_stones(game, ReversiGame.Bot);
            //Console.WriteLine("Bot1's Score : " + (Bot1Score - Bot2Score).ToString());
            //Console.WriteLine("Bot2's Score : " + (Bot2Score - Bot1Score).ToString());

            Match match = new Match();
            match.bot1 = Bot1;
            match.bot2 = Bot2;
            match.bot1Score = Bot1Score;
            match.bot2Score = Bot2Score;

            if (Bot1Score > Bot2Score)
            {
                //Console.WriteLine("Bot1 Won!");
                match.Winner = ReversiGame.Human;
                match.Looser = ReversiGame.Bot;
            }
            else if (Bot2Score > Bot1Score)
            {
                //Console.WriteLine("Bot2 Won!");
                match.Winner = ReversiGame.Bot;
                match.Looser = ReversiGame.Human;
            }
            else if (Bot1Score == Bot2Score)
            {
                //Console.WriteLine("Tie!");
                match.Winner = 0;
                match.Looser = 0;
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