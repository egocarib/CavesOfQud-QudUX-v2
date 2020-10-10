using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Steamworks;
using XRL;
using XRL.Core;
using ConsoleLib.Console;
using System.Text.RegularExpressions;
using QudUX.Utilities;

namespace QudUX.ScreenExtenders
{

    [Serializable]
    public class EnhancedScoreboard : Scoreboard
    {
        public List<EnhancedScoreEntry> EnhancedScores = new List<EnhancedScoreEntry>();

        public static EnhancedScoreboard Load()
        {
            EnhancedScoreboard instance = new EnhancedScoreboard();
            try
            {
                using (Stream stream = File.OpenRead(DataManager.SavePath("HighScores.dat")))
                {
                    var inst = (((IFormatter)new BinaryFormatter()).Deserialize(stream) as Scoreboard);
                    stream.Close();
                    instance.Scores = inst.Scores;
                    instance.EnhancedScores = inst.Scores.Select(parent => new EnhancedScoreEntry(parent)).ToList();
                }
            }
            catch (Exception ex)
            {
				
				//Logger.Log("ERROR :" + ex.Message);
                instance = new EnhancedScoreboard();
            }
            return instance;
        }
    }

    public class EnhancedScoreEntry : ScoreEntry
    {
        public EnhancedScoreEntry(ScoreEntry scoreEntry) : base(scoreEntry.Score, scoreEntry.Description, scoreEntry.Details)
        {
            CopyFields(scoreEntry);

            var details = scoreEntry.Details.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            int line = 0;

            int posGsf = details[line].IndexOf("Game summary for");
            if (posGsf > 0)
            {
                Version = "0";
            }

            try
            {
                // Get Character Name
                var charLine = details[line].Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                var charLine2 = charLine.Skip(1).Take(charLine.Length - 2);
                CharacterName = string.Join(" ", charLine2.Skip(3));

                // Get Date of death
                line++;
                string date = details[line].Substring(details[line].IndexOf(",") + 2);
                string time = date.Substring(date.IndexOf("at") + 3, date.Length - date.IndexOf("at") - 4);
                date = date.Substring(0, date.IndexOf("at") - 1);
                DateTime deathDate;
                if (!DateTime.TryParseExact(date + " " + time, "dd MMMM yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out deathDate))
                {
                    if (!DateTime.TryParseExact(date + " " + time, "MMMM dd, yyyy h:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out deathDate))
                    {
                        deathDate = DateTime.MinValue;
                    }
                }
                DeathDate = deathDate;
                // get cause of death
                line++;
                int posKb = details[line].IndexOf(" by ");
                string kb = "";


                if (posKb > -1)
                {
                    // killed by
                    kb = details[line].Substring(posKb + 4);
                    if (kb.StartsWith("a "))
                    {
                        kb = kb.Substring(2, kb.Length - 2);
                    }
                    if (kb.StartsWith("an "))
                    {
                        kb = kb.Substring(3, kb.Length - 3);
                    }
                }
                else
                {
                    // from ?
                    int posFrom = details[line].IndexOf(" from ");
                    if (posFrom > -1)
                    {
                        kb = details[line].Substring(posFrom + 6);
                        if (kb.StartsWith("a "))
                        {
                            kb = kb.Substring(2, kb.Length - 2);
                        }
                        if (kb.StartsWith("an "))
                        {
                            kb = kb.Substring(3, kb.Length - 3);
                        }
                    }
                    else
                    {
                        if (details[line].StartsWith("You were"))
                        {
                            kb = details[line].Substring(9);
                        }
                        else
                        {
                            if (details[line].StartsWith("You "))
                            {
                                kb = details[line].Substring(4);
                            }
                        }
                    }
                }

                if (kb.EndsWith("."))
                {
                    kb = kb.Remove(kb.Length - 1);
                }
                KilledBy = ColorUtility.StripFormatting(RemoveEffect(kb)).Trim();

                Abandoned = KilledBy.StartsWith("abandoned");

                // get Level
                line++;
                var elts = details[line].Split(' ');
                Regex rgx = new Regex("[^0-9]");
                string lvl = rgx.Replace(elts[3], "");
                Level = int.Parse(lvl);

                // get Turns
                line++;
                line++;
                elts = details[line].Split(' ');
                string turns = rgx.Replace(elts[2], "");
                Turns = int.Parse(turns);
            }
            catch (Exception)
            {
               // throw new Exception("Exception line " + line.ToString() + " : " + details[line] );
				//Logger.Log("Exception line " + line.ToString() + " : " + details[line] );
            }
        }

        private string RemoveEffect(string part)
        {
            string[] effects = new string[] { "bloody","slimmy" ,"tarred", "salty"};
            foreach(var e in effects)
            {
                part = part.Replace(e,"");
            }
            return part;
        }

        public EnhancedScoreEntry(int _Score, string _Description, string _Details) : this(new ScoreEntry(_Score, _Details, _Description))
        {
        }

        public string CharacterName { get; set; }
        public DateTime DeathDate { get; set; }
        public string KilledBy { get; set; }
        public int Level { get; set; }
        public int Turns { get; set; }
        public string Version { get; set; }
        public bool Abandoned{get ; set;} 

        private void CopyFields(ScoreEntry scoreEntry)
        {
            foreach (PropertyInfo prop in scoreEntry.GetType().GetProperties())
                GetType().GetProperty(prop.Name).SetValue(this, prop.GetValue(scoreEntry, null), null);
        }
    }
}
