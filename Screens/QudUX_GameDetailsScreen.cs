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
  
	[UIView("QudUX:GameDetails", ForceFullscreen: true, NavCategory: "Menu,Nocancelescape")]
	public class QudUX_GameDetailsScreen : IScreen, IWantsTextConsoleInit
	{			 
		private static TextConsole Console;
		private static ScreenBuffer Buffer;
         public string GameDetails { get; set; }
        public void Init(TextConsole console, ScreenBuffer buffer)
		{
			Console = console;
			Buffer = buffer;
		}
        public ScreenReturn Show(GameObject GO)
        {
            GameManager.Instance.PushGameView("QudUX:GameDetails");
            QudUXTextBlock tb = new QudUXTextBlock(0,1,78,23);
            tb.DrawBorder = true;
            tb.Text = GameDetails;

            while (true)
            {
                Buffer.Clear();
                Buffer.SingleBox();
                Buffer.Title("Game Detail");
                Buffer.EscOr5ToExit();
                Buffer.Goto(2, 24);
                Buffer.Write("[{{W|8}}-Up {{W|2}}-Down {{W|9}}-Pg.Up {{W|3}}-Pg.Down]");

                tb.Display(Buffer);

                Console.DrawBuffer(Buffer);

                Keys keys = Keyboard.getvk(Options.MapDirectionsToKeypad);

                if (keys == Keys.Escape || keys == Keys.NumPad5)
                {
                    GameManager.Instance.PopGameView();
                    return ScreenReturn.Exit;
                }

                if (keys == Keys.NumPad3 || keys == Keys.Next)
                {
                    tb.ScrollPage(1);
                }

                if (keys == Keys.NumPad9 || keys == Keys.Prior)
                {
                    tb.ScrollPage(-1);                    
                }

                if (keys == Keys.NumPad2)
                {
                    tb.Scroll(1);
                }

                if (keys == Keys.NumPad8)
                {
                    tb.Scroll(-1);
                }

            }
        }
    }
}