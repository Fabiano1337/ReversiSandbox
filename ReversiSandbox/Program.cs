using System;

namespace ReversiSandbox // Note: actual namespace depends on the project name.
{
    class Program
    {
        static void Main(string[] args)
        {
            ReversiGame game = new ReversiGame(); // X = Human, O = Bot
            while (true)
            {
                game.printGameField();
                game.printPossibleMoves();

                int xScore = ReversiGame.count_stones(game, Player.Human);
                int oScore = ReversiGame.count_stones(game, Player.Bot);
                Console.WriteLine("X's Score : " + (xScore-oScore).ToString());
                
                Console.WriteLine(game.curPlayer.ToString() + "'s Turn");
                Console.Write("Input Position : ");
                string cmd = Console.ReadLine();
                Console.WriteLine();
                Position pos = cmdToPos(cmd);

                game.move(pos);
                game.switchPlayer();

                Console.Clear();
            }
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