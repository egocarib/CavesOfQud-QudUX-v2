using System;
using XRL;
using XRL.Core;
using XRL.Rules;

namespace QudUX.Utilities
{
    [HasGameBasedStaticCache]
    public static class QudUX_Random
    {
        private static Random _random;
        public static Random Random
        {
            get
            {
                if (_random == null)
                {
                    if (XRLCore.Core?.Game == null)
                    {
                        throw new Exception("QudUX Attempted to retrieve Random, but Game is not created yet.");
                    }
                    else if (XRLCore.Core.Game.IntGameState.ContainsKey("QudUX:Random"))
                    {
                        int seed = XRLCore.Core.Game.GetIntGameState("QudUX:Random");
                        _random = new Random(seed);
                    }
                    else
                    {
                        _random = Stat.GetSeededRandomGenerator("QudUX");
                    }
                    XRLCore.Core.Game.SetIntGameState("QudUX:Random", _random.Next());
                }
                return _random;
            }
        }

        [GameBasedCacheInit]
        public static void ResetRandom()
        {
            _random = null;
        }

        public static int Next(int minInclusive, int maxExclusive)
        {
            return Random.Next(minInclusive, maxExclusive);
        }

        public static int NextInclusive(int minInclusive, int maxInclusive)
        {
            return Random.Next(minInclusive, maxInclusive + 1);
        }
    }
}
