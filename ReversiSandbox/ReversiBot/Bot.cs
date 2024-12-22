using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReversiSandbox.ReversiBot
{
    public abstract class Bot
    {
        public abstract Position generateMove(int[] gameField, int[] moves, int player);
    }
}
