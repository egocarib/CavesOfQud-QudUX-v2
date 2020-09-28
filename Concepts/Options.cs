using static XRL.UI.Options;

namespace QudUX.Concepts
{
    public static class Options
    {
        public static class Conversations
        {
            public static bool FindQuestGivers => GetOption("QudUX_OptionAskToFindQuestGivers").EqualsNoCase("Yes");
            public static bool AskAboutRestock => GetOption("QudUX_OptionAskAboutRestock").EqualsNoCase("Yes");
        }

        public static class UI
        {
            public static bool UseQudUXCookMenus => GetOption("QudUX_OptionUseCookMenus").EqualsNoCase("Yes");
            public static bool UseQudUXInventory => GetOption("QudUX_OptionUseInventoryMenu").EqualsNoCase("Yes");
            public static bool UseQudUXBuildLibrary => GetOption("QudUX_OptionUseBuildLibrary").EqualsNoCase("Yes");
            public static bool UseSpriteMenu => GetOption("QudUX_OptionCustomSpriteMenu").EqualsNoCase("Yes");
            public static bool ViewItemValues => GetOption("QudUX_OptionValPerLbInInventory").EqualsNoCase("Yes");
            public static bool ViewInventoryTiles => GetOption("QudUX_OptionShowInventoryTiles").EqualsNoCase("Yes");
            public static bool CollapsibleTradeUI => GetOption("QudUX_OptionCollapseInTradeMenu").EqualsNoCase("Yes");
            public static bool AddConversationTiles => GetOption("QudUX_OptionTileConversationUI").EqualsNoCase("Yes");
            public static bool ShowAbilityDescriptions => GetOption("QudUX_OptionAbilityDescriptions").EqualsNoCase("Yes");
            public static bool EnableAutogetExclusions => GetOption("QudUX_OptionAutogetExclusions").EqualsNoCase("Yes");
        }

        public static class Exploration
        {
            public static bool ParticleText => GetOption("QudUX_OptionParticleText").EqualsNoCase("Yes");
            public static bool RenameRuins => GetOption("QudUX_OptionRenameRuins").EqualsNoCase("Yes");
            public static bool TrackLocations => GetOption("QudUX_OptionTrackLocations").EqualsNoCase("Yes");
            public static bool UseArrowSprite => GetOption("QudUX_OptionReplaceArrowSprite").EqualsNoCase("Yes");
            public static bool DisableMagnets => GetOption("QudUX_OptionDisableMagnets").EqualsNoCase("Yes");
            public static class OptionStrings
            {
                public static string DisableMagnets => "QudUX_OptionDisableMagnets";
            }
        }
    }
}
