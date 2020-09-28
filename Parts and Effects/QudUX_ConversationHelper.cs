using System;
using System.Collections.Generic;
using UnityEngine;
using Qud.API;
using XRL.UI;
using XRL.Core;
using XRL.Rules;
using XRL.Language;
using XRL.World.Effects;
using XRL.World.Encounters.EncounterObjectBuilders;
using Options = QudUX.Concepts.Options;

namespace XRL.World.Parts
{
    [Serializable]
    public class QudUX_ConversationHelper : IPart
    {
        public static GameObject PlayerBody = null;
        public static GameObject ConversationPartner = null;
        public static List<GameObject> NewQuestHolders = new List<GameObject>();
        public static List<GameObject> ActiveQuestHolders = new List<GameObject>();
        public static List<GameObject> ZoneTradersTradedWith = new List<GameObject>();
        public static string CurrentInteractionZoneID = string.Empty;

        public override bool AllowStaticRegistration()
        {
            return true;
        }

        public override void Register(GameObject Object)
        {
            Object.RegisterPartEvent(this, "PlayerBeginConversation");
            base.Register(Object);
        }

        public override bool FireEvent(Event E)
        {
            if (E.ID == "PlayerBeginConversation")
            {
                QudUX_ConversationHelper.PlayerBody = XRLCore.Core.Game.Player.Body;
                GameObject speaker = E.GetGameObjectParameter("Speaker");
                if (speaker != null)
                {
                    QudUX_ConversationHelper.ConversationPartner = speaker;
                    if (QudUX_ConversationHelper.CurrentInteractionZoneID != speaker.CurrentCell.ParentZone.ZoneID)
                    {
                        QudUX_ConversationHelper.ZoneTradersTradedWith.Clear();
                        QudUX_ConversationHelper.CurrentInteractionZoneID = speaker.CurrentCell.ParentZone.ZoneID;
                    }
                    string questID = speaker.GetStringProperty("GivesDynamicQuest", string.Empty);
                    Conversation convo = E.GetParameter<Conversation>("Conversation");
                    if (speaker.HasPart("GenericInventoryRestocker") || speaker.HasPart("Restocker"))
                    {
                        try
                        {
                            QudUX_ConversationHelper.AddChoiceToRestockers(convo, speaker);
                        }
                        catch (Exception ex)
                        {
                            Debug.Log("QudUX: (Error) Encountered exception while adding conversation choice to merchant to ask about restock duration.\nException details: \n" + ex.ToString());
                        }
                    }
                    if (questID == string.Empty || XRLCore.Core.Game.FinishedQuests.ContainsKey(questID)) //speaker has no dynamic quests
                    {
                        try
                        {
                            this.AddChoiceToIdentifyQuestGivers(convo, speaker);
                        }
                        catch (Exception ex)
                        {
                            Debug.Log("QudUX: (Error) Encountered exception while adding conversation choices to identify village quest givers.\nException details: \n" + ex.ToString());
                        }
                    }
                }
            }
            return base.FireEvent(E);
        }

        public static void SetTraderInteraction(GameObject trader)
        {
            QudUX_ConversationHelper.ZoneTradersTradedWith.Add(trader);
            QudUX_ConversationHelper.AddChoiceToRestockers();
        }

        public static bool AddChoiceToRestockers(Conversation convo = null, GameObject speaker = null)
        {
            if (!Options.Conversations.AskAboutRestock)
            {
                return false;
            }
            int _debugSegmentCounter = 0;
            try
            {
                if (speaker == null)
                {
                    if (QudUX_ConversationHelper.ConversationPartner == null)
                    {
                        return false;
                    }
                    speaker = QudUX_ConversationHelper.ConversationPartner;
                    convo = speaker.GetPart<ConversationScript>()?.customConversation;
                    if (convo == null)
                    {
                        return false;
                    }
                }
                _debugSegmentCounter = 1;

                //you must view a trader's goods before the new conversation options become available.
                if (!QudUX_ConversationHelper.ZoneTradersTradedWith.Contains(speaker))
                {
                    return false;
                }
                _debugSegmentCounter = 2;

                //clean up old versions of the conversation if they exist
                if (convo.NodesByID.ContainsKey("*QudUX_RestockDiscussionNode"))
                {
                    _debugSegmentCounter = 3;
                    convo.NodesByID.Remove("*QudUX_RestockDiscussionNode");
                    if (convo.NodesByID.ContainsKey("Start"))
                    {
                        _debugSegmentCounter = 4;
                        for (int i = 0; i < convo.NodesByID["Start"].Choices.Count; i++)
                        {
                            if (convo.NodesByID["Start"].Choices[i].ID == "*QudUX_RestockerDiscussionStartChoice")
                            {
                                convo.NodesByID["Start"].Choices.RemoveAt(i);
                                break;
                            }
                        }
                    }
                }
                _debugSegmentCounter = 5;

                long ticksRemaining;
                bool bChanceBasedRestock = false;
                if (speaker.HasPart("Restocker"))
                {
                    _debugSegmentCounter = 6;
                    Restocker r = speaker.GetPart<Restocker>();
                    ticksRemaining = r.NextRestockTick - XRLCore.CurrentTurn;
                    _debugSegmentCounter = 7;
                }
                else if (speaker.HasPart("GenericInventoryRestocker"))
                {
                    _debugSegmentCounter = 8;
                    GenericInventoryRestocker r = speaker.GetPart<GenericInventoryRestocker>();
                    ticksRemaining = r.RestockFrequency - (XRLCore.CurrentTurn - r.LastRestockTick);
                    bChanceBasedRestock = true;
                    _debugSegmentCounter = 9;
                }
                else
                {
                    return false;
                }
                _debugSegmentCounter = 10;

                //build some dialog based on the time until restock and related parameters. TraderDialogGenData ensures the dialog options
                //stay the same for a single trader during the entire time that trader is waiting for restock
                TraderDialogGenData dialogGen = TraderDialogGenData.GetData(speaker, ticksRemaining);
                _debugSegmentCounter = 11;
                double daysTillRestock = (double)ticksRemaining / Calendar.turnsPerDay;
                string restockDialog;
                if (daysTillRestock >= 9)
                {
                    _debugSegmentCounter = 12;
                    if (speaker.Blueprint == "Sparafucile")
                    {
                        restockDialog = "\n&w*Sparafucile pokes at a few of =pronouns.possessive= wares and then gazes up at you, squinting, as if to question the basis of your inquiry.*&y\n ";
                    }
                    else
                    {
                        restockDialog = (dialogGen.Random2 == 1)
                            ? "Business is booming, friend.\n\nI'm pretty satisfied with what I've got for sale right now; maybe you should look for another "
                                + "vendor if you need something I'm not offering. I'll think about acquiring more goods eventually, but it won't be anytime soon."
                            : "Don't see anything that catches your eye?\n\nWell, you're in the minority. My latest shipment has been selling well and "
                                + "it'll be a while before I think about rotating my stock.";
                    }
                }
                else
                {
                    _debugSegmentCounter = 13;
                    if (speaker.Blueprint == "Sparafucile")
                    {
                        _debugSegmentCounter = 14;
                        if (daysTillRestock < 0.5)
                        {
                            _debugSegmentCounter = 15;
                            restockDialog = "\n&w*Sparafucile nods eagerly, as if to convey that =pronouns.subjective= is expecting something very soon.*&y\n ";
                        }
                        else
                        {
                            int fingers = Math.Max(1, (int)daysTillRestock);
                            restockDialog = "\n&w*Smiling, Sparafucile gives a slight nod.*&y\n\n"
                                + "&w*=pronouns.Subjective= purses =pronouns.possessive= lips thoughtfully for a moment, then raises " + fingers + " thin finger" + (fingers > 1 ? "s" : "") + ".*&y\n ";
                        }
                    }
                    else
                    {
                        _debugSegmentCounter = 16;
                        string daysTillRestockPhrase = (daysTillRestock < 0.5) ? "in a matter of hours"
                                    : (daysTillRestock < 1) ? "by this time tomorrow"
                                    : (daysTillRestock < 1.8) ? "within a day or two"
                                    : (daysTillRestock < 2.5) ? "in about two days' time"
                                    : (daysTillRestock < 3.5) ? "in about three days' time"
                                    : (daysTillRestock < 5.5) ? "in four or five days"
                                    : "in about a week, give or take";
                        string pronounObj = (dialogGen.Random3 == 1 ? "him" : (dialogGen.Random3 == 2 ? "her" : "them"));
                        string pronounSubj = (dialogGen.Random3 == 1 ? "he" : (dialogGen.Random3 == 2 ? "she" : "they"));
                        restockDialog =
                              (dialogGen.Random4 == 1) ? "There are rumors of a well-stocked dromad caravan moving through the area.\n\nMy sources tell me the caravan "
                                                    + "should be passing through " + daysTillRestockPhrase + ". I'll likely able to pick up some new trinkets at that time."
                                                    + (bChanceBasedRestock ? "\n\nOf course, they are only rumors, and dromads tend to wander. I can't make any guarantees." : string.Empty)
                            : (dialogGen.Random4 == 2) ? "My friend, a water baron is coming to visit this area soon. I sent " + pronounObj + " a list of my requests and should "
                                                    + "have some new stock available after " + pronounSubj + " arrive" + (pronounSubj == "they" ? "" : "s") + ".\n\n"
                                                    + "By the movements of the Beetle Moon, I predict " + pronounSubj + " should be here " + daysTillRestockPhrase + "."
                                                    + (bChanceBasedRestock ? "\n\nIn honesty, though, " + pronounSubj + (pronounSubj == "they" ? " are" : " is") + " not the most "
                                                    + "reliable friend. I can't make any guarantees." : string.Empty)
                            : (dialogGen.Random4 == 3) ? "It just so happens my apprentice has come upon a new source of inventory, and is negotiating with the merchant in a "
                                                    + "nearby village.\n\nThose talks should wrap up soon and I expect to have some new stock " + daysTillRestockPhrase + "."
                                                    + (bChanceBasedRestock ? "\n\nOf course, negotiations run like water through the salt. I can't make any guarantees." : string.Empty)
                            : "I'm glad you asked, friend. Arconauts have been coming in droves from a nearby ruin that was recently unearthed. "
                                                    + "They've been selling me trinkets faster than I can sort them, to be honest. After I manage to get things organized "
                                                    + "I'll have more inventory to offer.\n\nCheck back with me " + daysTillRestockPhrase + ", and I'll show you what I've got."
                                                    + (bChanceBasedRestock ? "\n\nThat is... assuming any of the junk is actually resellable. I can't make any guarantees." : string.Empty);
                    }
                    _debugSegmentCounter = 17;
                }

                //DEBUG ONLY
                _debugSegmentCounter = 18;

                //add options to ask location of quest givers for whom the quest has already started
                if (convo.NodesByID.ContainsKey("Start"))
                {
                    _debugSegmentCounter = 19;
                    //create node with info about trading
                    string restockNodeID = "*QudUX_RestockDiscussionNode";
                    ConversationNode restockNode = ConversationsAPI.newNode(restockDialog, restockNodeID);
                    _debugSegmentCounter = 20;
                    restockNode.AddChoice("I have more to ask.", "Start", null);
                    restockNode.AddChoice("Live and drink.", "End", null);
                    convo.AddNode(restockNode);
                    _debugSegmentCounter = 21;
                    ConversationNode startNode = convo.NodesByID["Start"];
                    int rand = Stat.Random(1, 3);
                    _debugSegmentCounter = 22;
                    ConversationChoice askRestockChoice = new ConversationChoice
                    {
                        ID = "*QudUX_RestockerDiscussionStartChoice",
                        Text = (rand == 1) ? "Any new wares on the way?"
                            : (rand == 2) ? "Do you have anything else to sell?"
                            : "Can you let me know if you get any new stock?",
                        GotoID = restockNodeID,
                        ParentNode = startNode,
                        Ordinal = 991 //set to make this appear immediately after the trade option
                    };
                    _debugSegmentCounter = 23;
                    startNode.Choices.Add(askRestockChoice);
                    _debugSegmentCounter = 24;
                    startNode.SortEndChoicesToEnd();
                    _debugSegmentCounter = 25;
                }
                _debugSegmentCounter = 26;
                return true;
            }
            catch (Exception ex)
            {
                Debug.Log("QudUX: (Error) Encountered exception in AddChoiceToRestockers (debugSegment: " + _debugSegmentCounter + ", Exception: " + ex.ToString() + ")");
                return false;
            }
        }

        public bool AddChoiceToIdentifyQuestGivers(Conversation convo, GameObject speaker)
        {
            if (!Options.Conversations.FindQuestGivers)
            {
                return false;
            }

            NewQuestHolders.Clear();
            ActiveQuestHolders.Clear();

            //determine which quest givers are in the area, using similar logic to DynamicQuestSignpostConversation.cs
            foreach (GameObject go in speaker.CurrentCell.ParentZone.GetObjectsWithProperty("GivesDynamicQuest"))
            {
                if (go != speaker && !go.HasEffect("QudUX_QuestGiverVision"))
                {
                    string questID = go.GetStringProperty("GivesDynamicQuest", null);
                    if (questID != null)
                    {
                        if (!XRLCore.Core.Game.HasQuest(questID))
                        {
                            NewQuestHolders.Add(go);
                        }
                        else if (!XRLCore.Core.Game.FinishedQuests.ContainsKey(questID))
                        {
                            ActiveQuestHolders.Add(go);
                        }
                    }
                }
            }
            if ((NewQuestHolders.Count + ActiveQuestHolders.Count) < 1) //No quest givers
            {
                return false;
            }

            //add options to ask location of quest givers for whom the quest has already started
            if (ActiveQuestHolders.Count > 0 && convo.NodesByID.ContainsKey("Start"))
            {
                string nameList = this.BuildQuestGiverNameList(ActiveQuestHolders);
                ConversationNode cNode = convo.NodesByID["Start"];
                this.RemoveOldQudUXChoices(cNode);
                ConversationChoice cChoice = new ConversationChoice
                {
                    Text = this.StatementLocationOf(nameList),
                    GotoID = "End",
                    ParentNode = cNode,
                    Execute = "XRL.World.Parts.QudUX_ConversationHelper:ApplyActiveQuestGiverEffect" //function to execute when this choice is selected.
                };
                cNode.Choices.Add(cChoice);
            }
            if (NewQuestHolders.Count > 0 && convo.NodesByID.ContainsKey("*DynamicQuestSignpostConversationIntro"))
            {
                string nameList = this.BuildQuestGiverNameList(NewQuestHolders);
                ConversationNode cNode = convo.NodesByID["*DynamicQuestSignpostConversationIntro"];
                this.RemoveOldQudUXChoices(cNode);
                ConversationChoice cChoice = new ConversationChoice
                {
                    Text = this.QuestionLocationOf(nameList, NewQuestHolders.Count > 1),
                    GotoID = "End",
                    ParentNode = cNode,
                    Execute = "XRL.World.Parts.QudUX_ConversationHelper:ApplyNewQuestGiverEffect" //function to execute when this choice is selected.
                };
                cNode.Choices.Add(cChoice);
            }
            return true;
        }

        public void RemoveOldQudUXChoices(ConversationNode cNode)
        {
            if (cNode == null || cNode.Choices == null)
            {
                return;
            }
            for (int i = cNode.Choices.Count - 1; i >= 0; i--)
            {
                ConversationChoice cChoice = cNode.Choices[i];
                if (cChoice != null && cChoice.Execute != null && cChoice.Execute.Contains(":"))
                {
                    string executeType = cChoice.Execute.Split(':')[0];
                    if (executeType == "XRL.World.Parts.QudUX_ConversationHelper")
                    {
                        cNode.Choices.RemoveAt(i);
                    }
                }
            }
        }

        public string StatementLocationOf(string nameList)
        {
            return "I'm looking for " + nameList + ".";
        }
        public string QuestionLocationOf(string nameList, bool multiple)
        {
            int randVal = Stat.Random(1, 3);
            string qText = randVal == 1 ? "How can I find " + nameList + "?"
                         : randVal == 2 ? "Can you help me track down " + nameList + "?"
                         : "Do you know where " + nameList + (multiple ? " are" : " is") + " located?";
            return qText;
        }

        public string BuildQuestGiverNameList(List<GameObject> questGiverList, string conjunction = "or")
        {
            //build quest giver name list
            string nameList = string.Empty;
            for (int i = 0; i < questGiverList.Count; i++)
            {
                if (i > 0)
                {
                    nameList += (i == questGiverList.Count - 1) ? (" " + conjunction + " ") : ", ";
                }
                nameList += Grammar.ShortenName(questGiverList[i].DisplayNameOnly);
            }
            return ConsoleLib.Console.ColorUtility.StripFormatting(nameList);
        }

        //Called dynamically by game from ConversationChoice.Execute string:
        public static bool ApplyNewQuestGiverEffect()
        {
            return ApplyQuestGiverEffect(QudUX_ConversationHelper.NewQuestHolders);
        }

        //Called dynamically by game from ConversationChoice.Execute string:
        public static bool ApplyActiveQuestGiverEffect()
        {
            return ApplyQuestGiverEffect(QudUX_ConversationHelper.ActiveQuestHolders);
        }

        public static bool ApplyQuestGiverEffect(List<GameObject> QuestGivers)
        {
            if (QudUX_ConversationHelper.ConversationPartner != null)
            {
                int randNum = Stat.Random(1, 3);
                Popup.Show(QudUX_ConversationHelper.ConversationPartner.The
                          + QudUX_ConversationHelper.ConversationPartner.DisplayNameOnly  + "&y "
                          + ((randNum == 1) ? "points you in the right direction."
                            : (randNum == 2) ? "gives you a rough layout of the area."
                            : "gestures disinterestedly, sending you on your way.") );
            }
            if (QudUX_ConversationHelper.PlayerBody != null && QudUX_ConversationHelper.PlayerBody == XRLCore.Core.Game.Player.Body)
            {
                string playerZoneID = QudUX_ConversationHelper.PlayerBody.CurrentCell.ParentZone.ZoneID;
                foreach (GameObject questGiver in QuestGivers)
                {
                    if (questGiver.CurrentCell.ParentZone.ZoneID == playerZoneID)
                    {
                        if (questGiver.HasEffect("QudUX_QuestGiverVision"))
                        {
                            questGiver.RemoveEffect("QudUX_QuestGiverVision");
                        }
                        questGiver.ApplyEffect(new QudUX_QuestGiverVision(QudUX_ConversationHelper.PlayerBody));
                    }
                }
            }
            return true;
        }
    }

    public class TraderDialogGenData
    {
        private static readonly Dictionary<GameObject, TraderDialogGenData> _Data = new Dictionary<GameObject, TraderDialogGenData>();
        readonly long ExpirationTick;
        readonly public int Random2;
        readonly public int Random3;
        readonly public int Random4;

        public TraderDialogGenData(long ticksRemaining)
        {
            this.ExpirationTick = ZoneManager.Ticker + ticksRemaining;
            this.Random2 = Stat.Random(1, 2);
            this.Random3 = Stat.Random(1, 3);
            this.Random4 = Stat.Random(1, 4);
        }

        public static TraderDialogGenData GetData(GameObject trader, long ticksRemaining)
        {
            if (!_Data.ContainsKey(trader) || _Data[trader].ExpirationTick <= ZoneManager.Ticker)
            {
                _Data.Remove(trader);
                _Data.Add(trader, new TraderDialogGenData(ticksRemaining));
            }
            return _Data[trader];
        }
    }
}
