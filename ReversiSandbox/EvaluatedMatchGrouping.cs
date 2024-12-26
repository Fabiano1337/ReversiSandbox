using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReversiSandbox
{
    class EvaluatedMatchGrouping
    {
        EvaluatedMatches[] evaluatedMatches;
        public EvaluatedMatchGrouping(EvaluatedMatches[] evMatches)
        {
            evaluatedMatches = evMatches;
        }

        public int getWins(int botId)
        {
            int wins = 0;
            int dimSize = (int)Math.Sqrt(evaluatedMatches.Length);
            for (int y = 0; y < dimSize; y++)
            {
                for (int x = 0; x < dimSize; x++)
                {
                    if (evaluatedMatches[x + y * dimSize] == null) continue;

                    int bot1Id = x;
                    int bot2Id = y;

                    if ((bot1Id != botId) && (bot2Id != botId)) continue;

                    if (bot1Id == botId)
                        wins += evaluatedMatches[x + y * dimSize].wins;

                    if (bot2Id == botId)
                        wins += evaluatedMatches[x + y * dimSize].looses;
                }
            }

            return wins;
        }

        public int getLooses(int botId)
        {
            int looses = 0;
            int dimSize = (int)Math.Sqrt(evaluatedMatches.Length);
            for (int y = 0; y < dimSize; y++)
            {
                for (int x = 0; x < dimSize; x++)
                {
                    if (evaluatedMatches[x + y * dimSize] == null) continue;

                    int bot1Id = x;
                    int bot2Id = y;

                    if ((bot1Id != botId) && (bot2Id != botId)) continue;

                    if (bot1Id == botId)
                        looses += evaluatedMatches[x + y * dimSize].looses;

                    if (bot2Id == botId)
                        looses += evaluatedMatches[x + y * dimSize].wins;
                }
            }

            return looses;
        }

        public int getGameCount(int botId)
        {
            int games = 0;
            int dimSize = (int)Math.Sqrt(evaluatedMatches.Length);
            for (int y = 0; y < dimSize; y++)
            {
                for (int x = 0; x < dimSize; x++)
                {
                    if (evaluatedMatches[x + y * dimSize] == null) continue;

                    int bot1Id = x;
                    int bot2Id = y;

                    if ((bot1Id != botId) && (bot2Id != botId)) continue;

                    if (bot1Id == botId)
                        games += evaluatedMatches[x + y * dimSize].gameCount;

                    if (bot2Id == botId)
                        games += evaluatedMatches[x + y * dimSize].gameCount;
                }
            }

            return games;
        }

        public int getWinDiff(int bot1, int bot2)
        {
            int bot1Wins = getWins(bot1);
            int bot2Wins = getWins(bot2);

            return bot1Wins - bot2Wins;
        }

        public int[] getBestBots(int count)
        {
            int dimSize = (int)Math.Sqrt(evaluatedMatches.Length);

            List<int> scores = new List<int>();

            for (int i = 0; i < dimSize; i++)
            {
                int wins = getWins(i);

                scores.Add(wins);
            }

            var sorted = scores
                .Select((x, i) => new KeyValuePair<int, int>(x, i))
                .OrderBy(x => x.Key)
                .ToList();

            int[] bestScores = new int[count];

            for (int i = 0; i < count; i++)
            {
                bestScores[i] = sorted[sorted.Count-i-1].Value;
            }

            return bestScores;
        }
    }
}
