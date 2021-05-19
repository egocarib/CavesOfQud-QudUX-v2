using static XRL.UI.Options;

namespace QudUX.Concepts
{
    public static class Options
    {
        // All of the OR IsNullOrEmpty bits below are added to temporarily address this bug:
        // https://bitbucket.org/bbucklew/cavesofqud-public-issue-tracker/issues/4118
        // They should be removed after that is fixed.
        public static class Conversations
        {
            public static bool FindQuestGivers => GetOption("QudUX_OptionAskToFindQuestGivers").EqualsNoCase("Yes") || string.IsNullOrEmpty(GetOption("QudUX_OptionAskToFindQuestGivers"));
            public static bool AskAboutRestock => GetOption("QudUX_OptionAskAboutRestock").EqualsNoCase("Yes") || string.IsNullOrEmpty(GetOption("QudUX_OptionAskAboutRestock"));
        }

        public static class UI
        {
            public static bool UseQudUXCookMenus => GetOption("QudUX_OptionUseCookMenus").EqualsNoCase("Yes") || string.IsNullOrEmpty(GetOption("QudUX_OptionUseCookMenus"));
            public static bool UseQudUXInventory => GetOption("QudUX_OptionUseInventoryMenu").EqualsNoCase("Yes") || string.IsNullOrEmpty(GetOption("QudUX_OptionUseInventoryMenu"));
            public static bool UseQudUXBuildLibrary => GetOption("QudUX_OptionUseBuildLibrary").EqualsNoCase("Yes") || string.IsNullOrEmpty(GetOption("QudUX_OptionUseBuildLibrary"));
            public static bool UseSpriteMenu => GetOption("QudUX_OptionCustomSpriteMenu").EqualsNoCase("Yes") || string.IsNullOrEmpty(GetOption("QudUX_OptionCustomSpriteMenu"));
            public static bool ViewItemValues => GetOption("QudUX_OptionValPerLbInInventory").EqualsNoCase("Yes") || string.IsNullOrEmpty(GetOption("QudUX_OptionValPerLbInInventory"));
            public static bool ViewInventoryTiles => GetOption("QudUX_OptionShowInventoryTiles").EqualsNoCase("Yes") || string.IsNullOrEmpty(GetOption("QudUX_OptionShowInventoryTiles"));
            public static bool CollapsibleTradeUI => GetOption("QudUX_OptionCollapseInTradeMenu").EqualsNoCase("Yes") || string.IsNullOrEmpty(GetOption("QudUX_OptionCollapseInTradeMenu"));
            public static bool AddConversationTiles => GetOption("QudUX_OptionTileConversationUI").EqualsNoCase("Yes") || string.IsNullOrEmpty(GetOption("QudUX_OptionTileConversationUI"));
            public static bool ShowAbilityDescriptions => GetOption("QudUX_OptionAbilityDescriptions").EqualsNoCase("Yes") || string.IsNullOrEmpty(GetOption("QudUX_OptionAbilityDescriptions"));
            public static bool EnableAutogetExclusions => GetOption("QudUX_OptionAutogetExclusions").EqualsNoCase("Yes") || string.IsNullOrEmpty(GetOption("QudUX_OptionAutogetExclusions"));
        }

        public static class Exploration
        {
            public static bool ParticleText => GetOption("QudUX_OptionParticleText").EqualsNoCase("Yes") || string.IsNullOrEmpty(GetOption("QudUX_OptionParticleText"));
            public static bool RenameRuins => GetOption("QudUX_OptionRenameRuins").EqualsNoCase("Yes") || string.IsNullOrEmpty(GetOption("QudUX_OptionRenameRuins"));
            public static bool TrackLocations => GetOption("QudUX_OptionTrackLocations").EqualsNoCase("Yes") || string.IsNullOrEmpty(GetOption("QudUX_OptionTrackLocations"));
            public static bool DisableMagnets => GetOption("QudUX_OptionDisableMagnets").EqualsNoCase("Yes");
            public static class OptionStrings
            {
                public static string DisableMagnets => "QudUX_OptionDisableMagnets";
            }
        }
    }
}
