using System;
using System.Xml;
using System.Collections.Generic;
using XRL;
using QudUX.Concepts;
using static QudUX.Utilities.Logger;
using static QudUX.ScreenExtenders.AbilityManagerExtender;

namespace QudUX.Utilities
{
    public static class FileHandler
    {
        public static Dictionary<string, List<AbilityXmlInfo>> LoadCategorizedAbilityDataEntries()
        {
            Dictionary<string, List<AbilityXmlInfo>> CategorizedData = new Dictionary<string, List<AbilityXmlInfo>>();
            ModManager.ForEachFile(Constants.AbilityDataFileName, (file, mod) =>
            {
                if (mod.IsApproved)
                {
                    string fromString = mod.ID != "QudUX" ? $" from {mod.ID}" : string.Empty;
                    Log($"Loading {Constants.AbilityDataFileName}{fromString}...");
                    foreach (var abilityCategoryData in LoadAbilityDataFile(file))
                    {
                        string category = abilityCategoryData.Key;
                        var abilityDefs = abilityCategoryData.Value;
                        if (CategorizedData.ContainsKey(category))
                        {
                            foreach (var abilityXmlInfo in abilityDefs)
                            {
                                int idx = CategorizedData[category].FindIndex(item => item.Name == abilityXmlInfo.Name);
                                if (idx >= 0)
                                {
                                    CategorizedData[category][idx] = abilityXmlInfo; //overwrite if existing entry found
                                }
                                else
                                {
                                    CategorizedData[category].Add(abilityXmlInfo);
                                }
                            }
                        }
                        else
                        {
                            CategorizedData.Add(category, abilityDefs);
                        }
                    }
                }
            });
            return CategorizedData;
        }

        public static Dictionary<string, List<AbilityXmlInfo>> LoadAbilityDataFile(string filePath)
        {
            Dictionary<string, List<AbilityXmlInfo>> ModCategorizedData = new Dictionary<string, List<AbilityXmlInfo>>();
            try
            {
                using (XmlTextReader stream = new XmlTextReader(filePath))
                {
                    stream.WhitespaceHandling = WhitespaceHandling.None;
                    while (stream.Read())
                    {
                        if (stream.Name == "abilityEntries")
                        {
                            while (stream.Read())
                            {
                                if (stream.Name == "category")
                                {
                                    string categoryName = stream.GetAttribute("Name");
                                    List<AbilityXmlInfo> categoryEntries = new List<AbilityXmlInfo>();
                                    while (stream.Read())
                                    {
                                        if (stream.Name == "abilityEntry")
                                        {
                                            AbilityXmlInfo thisEntry = new AbilityXmlInfo
                                            {
                                                Name = stream.GetAttribute("Name"),
                                                Class = stream.GetAttribute("Class"),
                                                Command = stream.GetAttribute("Command"),
                                                MutationName = stream.GetAttribute("MutationName"),
                                                SkillName = stream.GetAttribute("SkillName"),
                                                BaseCooldown = stream.GetAttribute("BaseCooldown"),
                                                CustomDescription = stream.GetAttribute("CustomDescription"),
                                                DeleteLines = stream.GetAttribute("DeleteLines"),
                                                DeletePhrases = stream.GetAttribute("DeletePhrases"),
                                                NoCooldownReduction = stream.GetAttribute("NoCooldownReduction"),
                                                CooldownChangeSkills = stream.GetAttribute("CooldownChangeSkills")
                                            };
                                            categoryEntries.Add(thisEntry);
                                        }
                                        if (stream.NodeType == XmlNodeType.EndElement && (stream.Name == string.Empty || stream.Name == "category"))
                                        {
                                            break;
                                        }
                                    }
                                    if (categoryEntries.Count > 0)
                                    {
                                        if (ModCategorizedData.ContainsKey(categoryName))
                                        {
                                            ModCategorizedData[categoryName].AddRange(categoryEntries);
                                        }
                                        else
                                        {
                                            ModCategorizedData.Add(categoryName, categoryEntries);
                                        }
                                    }
                                }
                                if (stream.NodeType == XmlNodeType.EndElement && (stream.Name == string.Empty || stream.Name == "abilityEntries"))
                                {
                                    break;
                                }
                            }
                        }
                        if (stream.NodeType == XmlNodeType.EndElement && (stream.Name == string.Empty || stream.Name == "abilityEntries"))
                        {
                            break;
                        }
                    }
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                Log($"Error trying to load data from {Constants.AbilityDataFileName} ({ex})");
                ModCategorizedData.Clear();
            }
            return ModCategorizedData;
        }
    }
}