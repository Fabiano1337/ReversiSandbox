﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReversiSandbox.ReversiBot;
namespace ReversiSandbox
{
    class Match
    {
        public int bot1Score;
        public int bot2Score;
        public Bot? bot1;
        public Bot? bot2;
        public int Winner;
        public int Looser;
        public float[] winnerWeight;
        public float[] looserWeight;
        public int starter;
    }
}
