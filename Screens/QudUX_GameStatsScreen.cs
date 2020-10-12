using System;
using System.Linq;
using System.Collections.Generic;
using ConsoleLib.Console;
using XRL.World;
using XRL.World.Parts;
using QudUX.Utilities;
using QudUX.ScreenExtenders;

namespace XRL.UI
{
  
	[UIView("QudUX:GameStats", ForceFullscreen: true, NavCategory: "Menu,Nocancelescape")]
	public class QudUX_GameStatsScreen : IScreen, IWantsTextConsoleInit
	{			 
		private static TextConsole Console;
		private static ScreenBuffer Buffer;
		public List<EnhancedScoreEntry> ScoreList;
		private enum StatsScreen
		{
			GamesList = 1,
			LevelStats,
			DeathStats,
		} 

		private StatsScreen currentScreen;
		public void Init(TextConsole console, ScreenBuffer buffer)
		{
			Console = console;
			Buffer = buffer;
		}
		public ScreenReturn Show(GameObject GO)
        {
            currentScreen = StatsScreen.GamesList;

            //Logger.Log("Loading scoreboard");
            Table scoreTable, levelsTable, deathCauseTable;
			bool showAbandonned = true;

            FillTables(out scoreTable, out levelsTable, out deathCauseTable, showAbandonned );


            while (true)
            {
                DisplayScreenFrame();
                Buffer.Goto(55, 0);
                Buffer.Write(((int)currentScreen).ToString() + "/3");

                Table currentTable = null;
                if (currentScreen == StatsScreen.GamesList)
                {
                    scoreTable.Display(Buffer, 1, 1);
                    currentTable = scoreTable;
                }
                else if (currentScreen == StatsScreen.LevelStats)
                {
                    levelsTable.Display(Buffer, 1, 1);
                    currentTable = levelsTable;

                }
                else if (currentScreen == StatsScreen.DeathStats)
                {
                    deathCauseTable.Display(Buffer, 1, 1);
                    currentTable = deathCauseTable;
                }

                Buffer.Goto(2, 23);
                Buffer.Write(ScoreList.Count.ToString() + " {{O|games}}");
                /*
				// debug table
				Buffer.Goto(1,24);
				Buffer.Write($"si:{scoreTable.SelectedIndex} of:{scoreTable.Offset} mvr:{scoreTable.MaxVisibleRows} cvr:{scoreTable.CurrentVisibleRows} ");
				*/

                Console.DrawBuffer(Buffer);

                Keys keys = Keyboard.getvk(Options.MapDirectionsToKeypad);

                if (keys == Keys.Escape || keys == Keys.NumPad5)
                {
                    GameManager.Instance.PopGameView();
                    return ScreenReturn.Exit;
                }

                if (currentTable != null)
                {
                    if (keys == Keys.NumPad3 || keys == Keys.Next)
                    {
                        currentTable.ScrollPage(1);
                    }

                    if (keys == Keys.NumPad9 || keys == Keys.Prior)
                    {
                        currentTable.ScrollPage(-1);
                    }

                    if (keys == Keys.NumPad2)
                    {
                        currentTable.MoveSelection(1);
                    }

                    if (keys == Keys.NumPad8)
                    {
                        currentTable.MoveSelection(-1);
                    }

					if (keys == Keys.Space)
					{
						showAbandonned = !showAbandonned;
            			FillTables(out scoreTable, out levelsTable, out deathCauseTable, showAbandonned );
					}
                }

                if (keys == Keys.NumPad6)
                {
                    if (currentScreen == StatsScreen.DeathStats)
                    {
                        currentScreen = StatsScreen.GamesList;
                    }
                    else
                    {
                        currentScreen++;
                    }
                }

                if (keys == Keys.NumPad4)
                {
                    if (currentScreen == StatsScreen.GamesList)
                    {
                        currentScreen = StatsScreen.DeathStats;
                    }
                    else
                    {
                        currentScreen--;
                    }
                }

            }
        }

        private void FillTables(out Table scoreTable, out Table levelsTable, out Table deathCauseTable, bool showAbandonned = true )
        {
            EnhancedScoreboard scoreboard = EnhancedScoreboard.Load();

            ScoreList = (from s in scoreboard.EnhancedScores
                        where (showAbandonned ||  !s.Abandoned)
                         orderby s.Score descending
                         select s).ToList();

            //Logger.Log(ScoreList.Count.ToString() + " games found");

            var StatsByLevel = from score in ScoreList
                               group score by score.Level into levelGroup
                               orderby levelGroup.Count() descending
                               select new { Level = levelGroup.Key, Nb = levelGroup.Count() ,  Pc= (float) levelGroup.Count() * 100 / ScoreList.Count };

            var StatsByDeathCause = from score in ScoreList
                                    group score by score.KilledBy into DeathCauseGroup
                                    orderby DeathCauseGroup.Count() descending
                                    select new { DeathCause = DeathCauseGroup.Key, Nb = DeathCauseGroup.Count(),  Pc = (float) DeathCauseGroup.Count() * 100 / ScoreList.Count  };

            //
            // games list table initialization			
            //

            scoreTable = new Table(
                new List<Table.ColumnDefinition>
                {
                    new Table.ColumnDefinition {Header="Name",Width=18},
                    new Table.ColumnDefinition {Header="Date",Width=10},
                    new Table.ColumnDefinition {Header="Score",Width=8},
                    new Table.ColumnDefinition {Header="Lvl",Width=5},
                    new Table.ColumnDefinition {Header="Killed by",Width=31},
                }
            );
            foreach (var sb in ScoreList)
            {
                string game = sb.CharacterName + " " + sb.DeathDate.ToString("yyyy-MM-dd") + "  " + sb.Score.ToString() + " " + sb.Level.ToString() + " ";
                //Logger.Log(game);
                scoreTable.Rows.Add(new List<string> { sb.CharacterName, sb.DeathDate.ToString("yyyy-MM-dd"), sb.Score.ToString(), sb.Level.ToString(), sb.KilledBy });

            }

            //
            // levels stats table initialization			
            //

            levelsTable = new Table(
                new List<Table.ColumnDefinition>
                {
                    new Table.ColumnDefinition {Header="Level",Width=10},
                    new Table.ColumnDefinition {Header="# Games",Width=10},
					new Table.ColumnDefinition {Header="% Overall",Width=10},
                }
            );
            foreach (var sb in StatsByLevel)
            {
                levelsTable.Rows.Add(new List<string> { sb.Level.ToString(), sb.Nb.ToString() ,sb.Pc.ToString("0.0")});
            }


            //
            // death cause stats table initialization			
            //

            deathCauseTable = new Table(
                new List<Table.ColumnDefinition>
                {
                    new Table.ColumnDefinition {Header="Death Cause",Width=40},
                    new Table.ColumnDefinition {Header="# Games",Width=10},
					new Table.ColumnDefinition {Header="% Overall",Width=10},
                }
            );
            foreach (var sb in StatsByDeathCause)
            {
                deathCauseTable.Rows.Add(new List<string> { sb.DeathCause, sb.Nb.ToString(),sb.Pc.ToString("0.0") });
            }
        }

        private static void DisplayScreenFrame()
        {
            Buffer.Clear();
            Buffer.SingleBox();
            Buffer.Title("Games statistics");
            Buffer.EscOr5ToExit();
            Buffer.Goto(45, 24);
            Buffer.Write("< {{W|4}} Prev. screen | Next Screen {{W|6}} >");
            Buffer.Goto(2, 24);
            Buffer.Write("[{{W|8}}-Sel.Up {{W|2}}-Sel.Down {{W|9}}-Pg.Up {{W|3}}-Pg.Down]");
        }
    }
}
