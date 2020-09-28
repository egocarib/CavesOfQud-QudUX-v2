using System;
using System.Collections.Generic;
using ConsoleLib.Console;
using XRL;
using XRL.UI;
using XRL.Core;
using XRL.World.Parts;
using XRL.World.Skills;
using XRL.World.Parts.Mutation;
using GameObject = XRL.World.GameObject;
using QudUX.Utilities;
using static QudUX.Utilities.Logger;
using Options = QudUX.Concepts.Options;

namespace QudUX.ScreenExtenders
{
    public static class AbilityManagerExtender
    {
        /// <summary>
        /// Called from our Harmony patch. Uses the UI's AbilityNode to form a better description
        /// if possible, then adds that description to the TextBlock that the AbilityManager will
        /// write to the screen.
        /// </summary>
        public static void UpdateAbilityText(AbilityNode node, TextBlock textBlock)
        {
            if (Options.UI.ShowAbilityDescriptions)
            {
                AbilityNarrator abilityHandler = new AbilityNarrator();
                List<string> computedAbilityDescription = abilityHandler.MakeDescription(node);
                if (computedAbilityDescription.Count > 0)
                {
                    textBlock.Lines = computedAbilityDescription;
                    textBlock._Width = -1;
                }
            }
        }

        /// <summary>
        /// A structure for holding extended ability information provided by this mod and loaded from
        /// AbilityExtenderData.xml. The reason we need to load external data is because there's often
        /// no clear association in code between the player's activated ability and the source of that
        /// ability (such as a mutation). So we need to define those connections more explicitly in
        /// order to generate meaningful descriptions for activated abilities.
        /// </summary>
        public class AbilityXmlInfo
        {
            public string Name;
            public string Class;
            public string Command;
            public string MutationName;
            public string SkillName;
            public string BaseCooldown;
            public string CustomDescription;
            public string DeleteLines;
            public string DeletePhrases;
            public string NoCooldownReduction;
            public string CooldownChangeSkills;
        }

        /// <summary>
        /// Provides ability descriptions for the AbilityManager UI. Consumes AbilityXmlInfo and uses
        /// it in conjunction with game data, using both sources to produce a suitable description.
        /// </summary>
        public class AbilityNarrator
        {
            private static Dictionary<string, List<AbilityXmlInfo>> _Categories = new Dictionary<string, List<AbilityXmlInfo>>();
            private static readonly HashSet<Guid> AbilitiesWithoutRealDescriptions = new HashSet<Guid>();
            private static bool _bStaticInitialized = false;
            private static bool? _bLoadedXMLData = null;
            public readonly List<BaseMutation> PlayerMutations;
            public Dictionary<string, List<AbilityXmlInfo>> Categories
            {
                get
                {
                    AbilityNarrator.InitializeStaticData();
                    return AbilityNarrator._Categories;
                }
            }

            /// <summary>
            /// Instantiates a new AbilityNarrator instance, and loads the latest player mutation data
            /// </summary>
            public AbilityNarrator()
            {
                Mutations playerMutationData = XRLCore.Core?.Game?.Player?.Body?.GetPart<Mutations>();
                if (playerMutationData != null)
                {
                    this.PlayerMutations = playerMutationData.ActiveMutationList;
                }
                else
                {
                    this.PlayerMutations = new List<BaseMutation>(0);
                }
            }

            /// <summary>
            /// Initializes the AbilityNarrator by loading data from AbilityExtenderData.xml.
            /// </summary>
            public static bool InitializeStaticData()
            {
                if (!AbilityNarrator._bStaticInitialized)
                {
                    AbilityNarrator._bStaticInitialized = true;
                    AbilityNarrator._Categories.Clear();
                    AbilityNarrator._Categories = FileHandler.LoadCategorizedAbilityDataEntries();
                    AbilityNarrator._bLoadedXMLData = (AbilityNarrator._Categories.Count > 0);
                }
                return (bool)AbilityNarrator._bLoadedXMLData;
            }

            /// <summary>
            /// Determines what kind of ability this is and whether it needs to be handed off to another
            /// method in order to generate a better ability description.
            /// </summary>
            public List<string> MakeDescription(AbilityNode abilityNode)
            {
                string category = abilityNode?.Ability?.Class;
                ActivatedAbilityEntry ability = abilityNode?.Ability;
                if (ability != null && !string.IsNullOrEmpty(category))
                {
                    try
                    {
                        if (category == "Mental Mutation" || category == "Mutation" || category == "Physical Mutation")
                        {
                            return this.MakeMutationAbilityDescription(category, ability);
                        }
                        //handle other ability descriptions
                        else
                        {
                            bool hasMeaningfulDescription;
                            if (AbilityNarrator.AbilitiesWithoutRealDescriptions.Contains(ability.ID))
                            {
                                hasMeaningfulDescription = false;
                            }
                            else
                            {
                                string vanillaDescription = ability.Description;
                                string simplifiedName = SimplifiedAbilityName(ability.DisplayName);
                                hasMeaningfulDescription = !string.IsNullOrEmpty(vanillaDescription)
                                    && ability.DisplayName != vanillaDescription
                                    && simplifiedName != vanillaDescription
                                    && !simplifiedName.StartsWith(vanillaDescription);
                            }
                            if (!hasMeaningfulDescription)
                            {
                                AbilityNarrator.AbilitiesWithoutRealDescriptions.Add(ability.ID);
                                return this.MakeNonMutationAbilityDescription(category, ability);
                            }
                            else //already has a meaningful description. Append cooldown info to the existing description.
                            {
                                return this.AddCooldownToPrexistingAbilityDescription(category, ability);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogUnique("(Error) Unable to update ability description for "
                            + $"ability '{ability?.DisplayName}' [{ability?.Command}].\n  Exception: {ex}");
                    }
                }
                try
                {
                    return SpecialFormatDescription(ability?.Description, SourceAbility: ability);
                }
                catch (Exception ex)
                {
                    LogUnique($"(Warning) Couldn't recognize ability '{ability?.DisplayName}' "
                        + $"[{ability?.Command}], so it's description was not updated.\n  Exception: {ex}");
                }
                return new List<string>();
            }

            /// <summary>
            /// Called if an ability already has a description defined in game - this will generally
            /// only add cooldown information to that existing description (unless we've explicitly
            /// defined a CustomDescription override for this ability in AbilityExtenderData.xml
            /// </summary>
            public List<string> AddCooldownToPrexistingAbilityDescription(string category, ActivatedAbilityEntry ability)
            {
                return this.MakeNonMutationAbilityDescription(category, ability, true);
            }

            /// <summary>
            /// Creates an ability description for a non-mutation ability. Generally these are loaded
            /// from a combination of AbilityExtenderData.xml, SkillFactory skill/power descriptions,
            /// or the hard-coded activated ability description when one exists. Cooldown information
            /// is also appended to the description.
            /// </summary>
            public List<string> MakeNonMutationAbilityDescription(string category, ActivatedAbilityEntry ability, bool bAddCooldownOnly = false)
            {
                if (!this.Categories.ContainsKey(category))
                {
                    LogUnique($"(Warning) Activated ability description for '{SimplifiedAbilityName(ability?.DisplayName)}'"
                        + $" won't be updated. (Couldn't find any data for activated ability category '{category}')");
                    return SpecialFormatDescription(ability?.Description, SourceAbility: ability);
                }
                List<AbilityXmlInfo> abilityData = this.Categories[category];
                foreach (AbilityXmlInfo abilityDataEntry in abilityData)
                {
                    if (abilityDataEntry.Name == SimplifiedAbilityName(ability.DisplayName))
                    {
                        string description = string.Empty;
                        string deleteLines = abilityDataEntry.DeleteLines;
                        string deletePhrases = abilityDataEntry.DeletePhrases;
                        if (!string.IsNullOrEmpty(abilityDataEntry.CustomDescription))
                        {
                            description = abilityDataEntry.CustomDescription;
                        }
                        else if (bAddCooldownOnly)
                        {
                            description = ability.Description;
                        }
                        else if (!string.IsNullOrEmpty(abilityDataEntry.SkillName))
                        {
                            if (SkillFactory.Factory.PowersByClass.ContainsKey(abilityDataEntry.SkillName))
                            {
                                PowerEntry skill = SkillFactory.Factory.PowersByClass[abilityDataEntry.SkillName];
                                description = skill.Description;
                            }
                        }
                        description = description.TrimEnd('\r', '\n', ' ');
                        if (string.IsNullOrEmpty(description) && !string.IsNullOrEmpty(ability.Description)) //just in case
                        {
                            description = ability.Description;
                            description = description.TrimEnd('\r', '\n', ' ');
                        }
                        if (!string.IsNullOrEmpty(description))
                        {
                            if (!string.IsNullOrEmpty(abilityDataEntry.BaseCooldown))
                            {
                                if (string.IsNullOrEmpty(abilityDataEntry.NoCooldownReduction) || abilityDataEntry.NoCooldownReduction != "true")
                                {
                                    string baseCooldown = GetAdjustedBaseCooldown(abilityDataEntry);
                                    string cooldownString = this.GetCooldownString(baseCooldown);
                                    if (!string.IsNullOrEmpty(cooldownString))
                                    {
                                        description += "\n\n" + cooldownString;
                                    }
                                }
                            }
                        }
                        return SpecialFormatDescription(description, deleteLines, deletePhrases, ability);
                    }
                }
                return SpecialFormatDescription(ability?.Description, SourceAbility: ability);
            }

            /// <summary>
            /// Creates an ability description for a mutation. The description is constructed from the
            /// mutation description and the GetLevelText() for the mutation at its current level.
            /// Cooldown information is also appended to the description.
            /// </summary>
            /// TODO: Determine if the Level logic has changed with the new chimera rapid advancements.
            ///       For example, will level return 6 if that's my actual level, or will it return
            ///       5 if my mutation level is restricted based on my character level?
            public List<string> MakeMutationAbilityDescription(string category, ActivatedAbilityEntry ability)
            {
                if (!this.Categories.ContainsKey(category))
                {
                    LogUnique($"(FYI) Activated ability description for '{SimplifiedAbilityName(ability?.DisplayName)}'"
                        + $" won't be updated because QudUX didn't recognize it's activated ability category, '{category}'");
                    return SpecialFormatDescription(ability?.Description, SourceAbility: ability);
                }
                List<AbilityXmlInfo> abilityData = this.Categories[category];
                foreach (AbilityXmlInfo abilityDataEntry in abilityData)
                {
                    if (abilityDataEntry.Name == SimplifiedAbilityName(ability.DisplayName))
                    {
                        //match AbilityDataEntry to the Ability name
                        BaseMutation abilitySourceMutation = null;
                        BaseMutation secondaryMatch = null;
                        foreach (BaseMutation playerMutation in this.PlayerMutations)
                        {
                            MutationEntry mutationEntry = playerMutation.GetMutationEntry();
                            if (mutationEntry != null && mutationEntry.DisplayName == abilityDataEntry.MutationName)
                            {
                                abilitySourceMutation = playerMutation;
                                break;
                            }
                            if (playerMutation.DisplayName == abilityDataEntry.MutationName)
                            {
                                secondaryMatch = playerMutation; //less desirable match method, but necessary for some NPC mutations that don't have a MutationEntry
                            }
                        }
                        if (abilitySourceMutation == null && secondaryMatch != null)
                        {
                            abilitySourceMutation = secondaryMatch;
                        }
                        if (abilitySourceMutation == null)
                        {
                            LogUnique($"(FYI) Mutation activated ability '{SimplifiedAbilityName(ability?.DisplayName)}'"
                                + $" in category '{category}' has no description available in game and no backup description"
                                + " provided by QudUX, so a description won't be shown on the Manage Abilities screen.");
                            break;
                        }

                        string abilityDescription = abilitySourceMutation.GetDescription() + "\n\n" + abilitySourceMutation.GetLevelText(abilitySourceMutation.Level);
                        abilityDescription = abilityDescription.TrimEnd('\r', '\n', ' ');
                        //updated Cooldown based on wisdom:
                        if (abilityDescription.Contains("Cooldown:") || !string.IsNullOrEmpty(abilityDataEntry.BaseCooldown))
                        {
                            if (string.IsNullOrEmpty(abilityDataEntry.NoCooldownReduction) || abilityDataEntry.NoCooldownReduction != "true")
                            {
                                string updatedDescription = string.Empty;
                                string extractedCooldownString = GetAdjustedBaseCooldown(abilityDataEntry);
                                string[] descriptionParts = abilityDescription.Split('\n');
                                foreach (string descriptionPart in descriptionParts)
                                {
                                    if (descriptionPart.Contains("Cooldown:"))
                                    {
                                        string[] words = descriptionPart.Split(' ');
                                        foreach (string word in words)
                                        {
                                            int o;
                                            if (int.TryParse(word, out o))
                                            {
                                                extractedCooldownString = this.GetCooldownString(word);
                                                break;
                                            }
                                        }
                                        if (string.IsNullOrEmpty(extractedCooldownString))
                                        {
                                            updatedDescription += (updatedDescription != string.Empty ? "\n" : string.Empty) + descriptionPart; //restore line in case we didn't find the number (should never happen)
                                        }
                                    }
                                    else
                                    {
                                        updatedDescription += (updatedDescription != string.Empty ? "\n" : string.Empty) + descriptionPart;
                                    }
                                }
                                abilityDescription = updatedDescription + (!string.IsNullOrEmpty(extractedCooldownString) ? "\n\n" + extractedCooldownString : string.Empty);
                            }
                        }
                        return SpecialFormatDescription(abilityDescription, abilityDataEntry.DeleteLines, abilityDataEntry.DeletePhrases, ability);
                    }
                }
                return SpecialFormatDescription(ability?.Description, SourceAbility: ability);
            }

            public string GetAdjustedBaseCooldown(AbilityXmlInfo abilityDataEntry)
            {
                if (string.IsNullOrEmpty(abilityDataEntry.BaseCooldown))
                {
                    return string.Empty;
                }
                if (string.IsNullOrEmpty(abilityDataEntry.CooldownChangeSkills))
                {
                    return abilityDataEntry.BaseCooldown;
                }
                GameObject player = XRLCore.Core?.Game?.Player?.Body;
                if (player == null)
                {
                    return abilityDataEntry.BaseCooldown;
                }
                int baseCooldown = int.Parse(abilityDataEntry.BaseCooldown);
                foreach (string entry in abilityDataEntry.CooldownChangeSkills.Split(','))
                {
                    string skill = entry.Split(':')[0];
                    int adjustAmount = int.Parse(entry.Split(':')[1]);
                    if (player.HasSkill(skill))
                    {
                        baseCooldown += adjustAmount;
                    }
                }
                return baseCooldown.ToString();
            }

            /// <summary>
            /// Gets the standard QudUX Cooldown description string for an ability. This string
            /// includes the actual cooldown and, if relevant, also a note about how the cooldown
            /// has been adjusted based on Willpower and what the Base Cooldown is.
            /// </summary>
            public string GetCooldownString(string baseCooldown)
            {
                string cooldownString = string.Empty;
                int number;
                bool isNumber = int.TryParse(baseCooldown, out number);
                if (isNumber)
                {
                    int newCooldown = this.GetAdjustedCooldown(number);
                    string changePhrase = (newCooldown > number) ? " (increased due to your Willpower)" : (newCooldown < number) ? " (decreased due to your Willpower)" : string.Empty;
                    cooldownString = "{{y|Cooldown: {{C|" + newCooldown.ToString() + "}}";
                    if (!string.IsNullOrEmpty(changePhrase))
                    {
                        cooldownString += changePhrase + "}}\n{{K|Base Cooldown: " + baseCooldown;
                    }
                    cooldownString += "}}";
                }
                return cooldownString;
            }

            /// <summary>
            /// Calculates the willpower-adjusted cooldown for an ability.
            /// </summary>
            public int GetAdjustedCooldown(int baseCooldown)
            {
                GameObject player = XRLCore.Core?.Game?.Player?.Body;
                if (player == null || !player.HasStat("Willpower"))
                {
                    return baseCooldown;
                }
                int internalCooldown = baseCooldown * 10;
                int val = (int)((double)internalCooldown * (100.0 - (double)((player.Stat("Willpower", 0) - 16) * 5))) / 100;
                int calculatedCooldown = Math.Max(val, ActivatedAbilities.MinimumValueForCooldown(internalCooldown));
                baseCooldown = (int)Math.Ceiling((double)((float)calculatedCooldown / 10f));
                return baseCooldown;
            }

            /// <summary>
            /// Strips off extra info at the end of an activated ability display name. For example,
            /// converts "Lase [4 charges]" to "Lase".
            /// </summary>
            public static string SimplifiedAbilityName(string name)
            {
                if (name == null)
                {
                    return string.Empty;
                }
                if (name.IndexOf('(') >= 0)
                {
                    name = name.Split('(')[0].Trim();
                }
                if (name.IndexOf('[') >= 0)
                {
                    name = name.Split('[')[0].Trim();
                }
                return name;
            }

            /// <summary>
            /// Converts a description string into a list of lines that can be written to the Console
            /// UI. Also does a few special things:
            ///  * Adds a vertical line (|) to the beginning of each line, which connects to form a
            ///    visual separator between the ability list and the descriptions written to the screen
            ///  * Deletes lines or phrases that were manually marked in AbilityExtenderData.xml for
            ///    deletion (typically because they either aren't relevant to the activated ability,
            ///    or because the description would otherwise be too long to fit on screen)
            /// </summary>
            public static List<string> SpecialFormatDescription(string description, string lineDeletionClues = null, string phraseDeletionClues = null, ActivatedAbilityEntry SourceAbility = null)
            {
                if (string.IsNullOrEmpty(description))
                {
                    if (SourceAbility != null)
                    {
                        LogUnique($"(FYI) Could not find a description for activated ability '{SimplifiedAbilityName(SourceAbility.DisplayName)}'."
                            + " A description won't be shown on the Manage Abilities screen.");
                    }
                    return new List<string>();
                }
                if (!string.IsNullOrEmpty(lineDeletionClues) || !string.IsNullOrEmpty(phraseDeletionClues) || description.Contains(" reputation "))
                {
                    string cleansedDescription = string.Empty;
                    foreach (string _line in description.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
                    {
                        string line = _line;
                        bool shouldDeleteLine = false;
                        //remove any custom lines from the description that were configured in AbilityExtenderData.xml
                        if (!string.IsNullOrEmpty(lineDeletionClues))
                        {
                            foreach (string deletionClue in lineDeletionClues.Split('~'))
                            {
                                if (line.StartsWith(deletionClue))
                                {
                                    shouldDeleteLine = true;
                                    break;
                                }
                            }
                        }
                        //remove any custom phrases from the description that were configured in AbilityExtenderData.xml
                        if (!string.IsNullOrEmpty(phraseDeletionClues))
                        {
                            foreach (string deletionClue in phraseDeletionClues.Split('~'))
                            {
                                if (line.Contains(deletionClue))
                                {
                                    line = line.Replace(deletionClue, "");
                                }
                            }
                        }
                        //remove reputation lines, because they aren't relevant to activated ability descriptions
                        if (!shouldDeleteLine && line.Contains(" reputation "))
                        {
                            if (line[0] == '+' || line[0] == '-')
                            {
                                string[] words = line.Split(' ');
                                if (words.Length >= 2 && words[1] == "reputation")
                                {
                                    shouldDeleteLine = true;
                                }
                            }
                        }
                        if (shouldDeleteLine == false)
                        {
                            cleansedDescription += (string.IsNullOrEmpty(cleansedDescription) ? "" : "\n") + line;
                        }
                    }
                    description = cleansedDescription;
                }

                List<string> descriptionLines = StringFormat.ClipTextToArray(description, MaxWidth: 32, KeepNewlines: true);
                if (descriptionLines.Count > 22)
                {
                    LogUnique($"(Warning) Part of the activated ability description for '{SimplifiedAbilityName(SourceAbility?.DisplayName)}'"
                        + " was discarded because it didn't fit on the Manage Abilities screen.");
                    descriptionLines = descriptionLines.GetRange(0, 22);
                }
                bool foundTextEnd = false;
                for (int i = descriptionLines.Count - 1; i >= 0; i--)
                {
                    if (!foundTextEnd) //get rid of any blank lines at the end
                    {
                        if (ColorUtility.StripFormatting(descriptionLines[i]).Trim().Length < 1)
                        {
                            descriptionLines.RemoveAt(i);
                            continue;
                        }
                        else
                        {
                            foundTextEnd = true;
                        }
                    }
                    descriptionLines[i] = "{{K|\u00b3}}" + descriptionLines[i];
                    int length = ColorUtility.StripFormatting(descriptionLines[i]).Length;
                    int padNeeded = 34 - length;
                    if (padNeeded > 0)
                    {
                        descriptionLines[i] = descriptionLines[i] + "{{y|" + string.Empty.PadRight(padNeeded) + "}}";
                    }
                }
                return descriptionLines;
            }
        }
    }
}
