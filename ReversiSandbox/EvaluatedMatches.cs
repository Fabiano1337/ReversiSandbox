using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReversiSandbox
{
    class EvaluatedMatches
    {
        public float winRate;
        public float looseRate;
        public float tieRate;
        public int gameCount;
        public int startCount;
        public int secondCount;
        public int wins;
        public int looses;
        public int ties;

        public int bot1Index;
        public int bot2Index;
    }
}
