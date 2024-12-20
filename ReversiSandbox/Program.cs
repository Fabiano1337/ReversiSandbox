using System;

namespace ReversiSandbox // Note: actual namespace depends on the project name.
{
    class Program
    {
        static void Main(string[] args)
        {
            ReversiGame game = new ReversiGame(); // X = Human, O = Bot

            Bot bot1 = new BotRandom();
            Bot bot2 = new BotRandom();

            Match match = simulateMatch(bot1, bot2);
            Console.WriteLine(match.Winner);
            Console.WriteLine(match.bot1Score);
            Console.WriteLine(match.bot2Score);
            /*game.printGameField();
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

                game.move(pos);
                game.printGameField();

                game.switchPlayer();

                Position botMove = bot.generateMove(game);
                Console.WriteLine("Bots move : " + positionToString(botMove));
                game.move(botMove);

                game.printGameField();

                game.switchPlayer();

                game.printPossibleMoves();
                //Console.Clear();
            }*/
        }

        static Match simulateMatch(Bot bot1, Bot bot2)
        {
            ReversiGame game = new ReversiGame();

            Position botMove;
            while (true)
            {
                botMove = bot1.generateMove(game);
                game.move(botMove);
                if (game.isEnded()) break;
                game.switchPlayer();

                botMove = bot2.generateMove(game);
                game.move(botMove);
                if (game.isEnded()) break;
                game.switchPlayer();
            }
            int bot1Score = ReversiGame.count_stones(game, Player.Human);
            int bot2Score = ReversiGame.count_stones(game, Player.Bot);
            Console.WriteLine("Bot1's Score : " + (bot1Score - bot2Score).ToString());
            Console.WriteLine("Bot2's Score : " + (bot2Score - bot1Score).ToString());

            Match match = new Match();
            match.bot1Score = bot1Score;
            match.bot2Score = bot2Score;

            if (bot1Score > bot2Score)
            {
                Console.WriteLine("Bot1 Won!");
                match.Winner = bot1;
            }
            else if (bot2Score > bot1Score)
            {
                Console.WriteLine("Bot2 Won!");
                match.Winner = bot2;
            }
            else if (bot1Score == bot2Score)
            {
                Console.WriteLine("Tie!");
                match.Winner = null;
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