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
            while (true)
            {
                Buffer.Clear();
                Buffer.SingleBox();
                Buffer.Title("Games statistics");
                Buffer.EscOr5ToExit();

                QudUXTextBlock tb = new QudUXTextBlock(10,10,15,10);
                tb.DrawBorder = true;
                tb.Text = GameDetails;
                tb.Display(Buffer);

                Console.DrawBuffer(Buffer);

                Keys keys = Keyboard.getvk(Options.MapDirectionsToKeypad);

                if (keys == Keys.Escape || keys == Keys.NumPad5)
                {
                    GameManager.Instance.PopGameView();
                    return ScreenReturn.Exit;
                }

            }
        }
    }
}