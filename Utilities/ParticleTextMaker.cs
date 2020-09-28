using System;
using System.Collections.Generic;
using XRL.World;
using XRL.Core;
using QudUX.Concepts;

namespace QudUX.Utilities
{
    public static class ParticleTextMaker
    {
        //Called from Harmony patch (ran into wall trying to move to adjacent zone)
        public static void EmitFromPlayerIfBarrierInDifferentZone(GameObject barrier)
        {
            GameObject player = XRLCore.Core?.Game?.Player?.Body;
            if (player != null)
            {
                if (barrier.CurrentCell.ParentZone.ZoneID != player.CurrentCell.ParentZone.ZoneID)
                {
                    List<string> words = new List<string>(3)
                    {
                        "{{w|OUCH!}}",
                        "{{w|Blocked!",
                        barrier.IsWall() ? "{{w|Wall!}}" : "{{w|Blocked!}}"
                    };
                    EmitText(player, words);
                }
            }
        }

        //Called from Harmony patch (confirmation message prevented entering deep or dangerous liquid)
        public static void EmitFromPlayerIfLiquid(GameObject liquidObject, bool isDeepLiquid)
        {
            bool isDangerousLiquid = liquidObject.IsDangerousOpenLiquidVolume();
            if (!isDangerousLiquid && !isDeepLiquid)
            {
                return;
            }
            GameObject player = XRLCore.Core?.Game?.Player?.Body;
            if (player != null)
            {
                List<string> words = new List<string>(1);
                if (isDeepLiquid)
                {
                    words.Add(isDangerousLiquid ? "{{r|Deep and dangerous!}}" : "{{b|Deep!}}");
                }
                else
                {
                    words.Add("{{r|Dangerous liquid!}}");
                }
                EmitText(player, words, null, liquidObject, 75);
            }
        }

        //Called from Harmony patch (prevented from moving outside edge of JoppaWorld)
        public static void EmitFromPlayer(string commaDelimitedFormattedTextOptions)
        {
            GameObject player = XRLCore.Core?.Game?.Player?.Body;
            List<string> outputOptions = new List<string>(commaDelimitedFormattedTextOptions.Split(','));
            if (player != null && outputOptions.Count > 0)
            {
                EmitText(player, outputOptions);
            }
        }

        public static void EmitText(GameObject fromObject, List<string> formattedTextOptions, GameObject towardsObject = null, GameObject awayFromObject = null, int degreeVariance = 30)
        {
            if (!Options.Exploration.ParticleText)
            {
                return; //player has disabled this feature
            }
            string direction;
            if (towardsObject == null && awayFromObject == null) //emit toward center of zone by default
            {
                Cell centerCell = fromObject.CurrentCell.ParentZone.GetCell(40, 12);
                direction = fromObject.CurrentCell.GetDirectionFromCell(centerCell);
            }
            else if (towardsObject != null)
            {
                direction = fromObject.CurrentCell.GetDirectionFromCell(towardsObject.CurrentCell);
            }
            else //awayFromObject != null
            {
                direction = awayFromObject.CurrentCell.GetDirectionFromCell(fromObject.CurrentCell);
            }
            Cell effectOriginCell = fromObject.CurrentCell.GetCellFromDirection(direction, true) ?? fromObject.CurrentCell;
            int effectX = effectOriginCell.X;
            int effectY = effectOriginCell.Y;
            int randIndex = QudUX_Random.Next(0, formattedTextOptions.Count);
            string text = formattedTextOptions[randIndex];

            //adjust starting position of text if it would overflow off right side of the screen
            int phraseLength = ConsoleLib.Console.ColorUtility.StripFormatting(text).Length;
            if ((80 - effectX) < phraseLength)
            {
                effectX = Math.Max(10, 80 - phraseLength);
            }

            float num = (float)GetDegreesForVisualEffect(direction, degreeVariance) / 58f;
            float xDel = (float)Math.Sin((double)num) / 4f;
            float yDel = (float)Math.Cos((double)num) / 4f;
            XRLCore.ParticleManager.Add(text, (float)effectX, (float)effectY, xDel, yDel, 22, 0f, 0f);
        }

        private static int GetDegreesForVisualEffect(string direction, int degreeVariance = 30)
        {
            int startDegree = direction == "S" ? 0
                            : direction == "SW" ? 315
                            : direction == "W" ? 270
                            : direction == "NW" ? 225
                            : direction == "N" ? 180
                            : direction == "NE" ? 135
                            : direction == "E" ? 90
                            : direction == "SE" ? 45
                            : (degreeVariance = 359); //random if direction didn't match
            int boundA = (startDegree - degreeVariance);
            int boundB = (startDegree + degreeVariance);
            return QudUX_Random.NextInclusive(boundA, boundB) % 360;
        }
    }
}
