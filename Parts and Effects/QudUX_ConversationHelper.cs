using System;
using System.Collections.Generic;
using UnityEngine;
using Qud.API;
using XRL.UI;
using XRL.Core;
using XRL.Rules;
using XRL.Language;
using XRL.World.Effects;
using XRL.World.Conversations;
using Options = QudUX.Concepts.Options;
using QudUXLogger = QudUX.Utilities.Logger;
using Battlehub.UIControls;
using XRL.Messages;

namespace XRL.World.Parts
{
    [Serializable]
    [HasConversationDelegate]
    public class QudUX_ConversationHelper : IPart
    {
        public static GameObject PlayerBody = null;
        public static GameObject ConversationPartner = null;
        public static List<GameObject> NewQuestHolders = new List<GameObject>();
        public static List<GameObject> ActiveQuestHolders = new List<GameObject>();
        public static List<GameObject> ZoneTradersTradedWith = new List<GameObject>();
        public const string QudUX_RestockDiscussionNodeID = "*QudUX_RestockDiscussionNode";
        public const string QudUX_RestockerDiscussionStartChoiceID = "*QudUX_RestockerDiscussionStartChoice";
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
                    //QudUXLogger.Log("conversation is " + (convo != null ? "not null" : "null"));
                    
                    if (speaker.HasPart("GenericInventoryRestocker") || speaker.HasPart("Restocker"))
                    {
                        try
                        {
                            //QudUXLogger.Log("Started Conversation with restocker, adding conversation choices");
                            bool res = QudUX_ConversationHelper.AddChoiceToRestockers(convo, speaker);
                            //QudUXLogger.Log("Choices addition " + (res ? "Succeeded" : "failed"));

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
            ZoneTradersTradedWith.Add(trader);
            bool res = QudUX_ConversationHelper.AddChoiceToRestockers();
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
                    speaker = ConversationUI.Speaker;
                    if(speaker == null)
                    {
                        return false;
                    }
                }

                _debugSegmentCounter = 1;

                if(convo == null)
                {
                    convo = ConversationUI.CurrentConversation;
                    if (convo == null)
                    {
                        return false;
                    }
                }


                _debugSegmentCounter = 2;

                //you must view a trader's goods before the new conversation options become available.
                if (!QudUX_ConversationHelper.ZoneTradersTradedWith.Contains(speaker))
                {
                    return false;
                }
                _debugSegmentCounter = 3;
                

                //clean up old versions of the conversation if they exist
                if(convo.GetStart() != null)
                {
                    Node start = convo.GetStart();

                    foreach (Choice choice in start.QudUX_GetChoices())
                    {
                        if (choice.ID == QudUX_RestockerDiscussionStartChoiceID)
                        {
                            start.Elements.Remove(choice);
                        }
                    }
                }

                if (convo.GetElementByID(QudUX_RestockDiscussionNodeID) is Node restrockDialog && restrockDialog != null)
                {
                    _debugSegmentCounter = 4;
                    convo.Elements.Remove(restrockDialog);
                }

                _debugSegmentCounter = 6;

                long ticksRemaining;
                bool bChanceBasedRestock = false;
                if (speaker.HasPart("GenericInventoryRestocker"))
                {
                    _debugSegmentCounter = 9;
                    GenericInventoryRestocker r = speaker.GetPart<GenericInventoryRestocker>();
                    ticksRemaining = r.RestockFrequency - (XRLCore.CurrentTurn - r.LastRestockTick);
                    bChanceBasedRestock = true;
                    _debugSegmentCounter = 10;
                }
                else
                {
                    return false;
                }
                _debugSegmentCounter = 11;

                //build some dialog based on the time until restock and related parameters. TraderDialogGenData ensures the dialog options
                //stay the same for a single trader during the entire time that trader is waiting for restock
                TraderDialogGenData dialogGen = TraderDialogGenData.GetData(speaker, ticksRemaining);
                _debugSegmentCounter = 12;
                double daysTillRestock = (double)ticksRemaining / Calendar.turnsPerDay;
                string restockDialog;
                if (daysTillRestock >= 9)
                {
                    _debugSegmentCounter = 13;
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
                    _debugSegmentCounter = 14;
                    if (speaker.Blueprint == "Sparafucile")
                    {
                        _debugSegmentCounter = 15;
                        if (daysTillRestock < 0.5)
                        {
                            _debugSegmentCounter = 16;
                            restockDialog = "\n&w*Sparafucile nods eagerly, as if to convey that =pronouns.subjective= is expecting something very soon.*&y\n ";
                        }
                        else
                        {
                            int fingers = Math.Max(1, (int)daysTillRestock);
                            restockDialog = "\n&w*Smiling, Sparafucile gives a slight nod.*&y\n\n"
                                + $"&w*{speaker.it} purses {speaker.its} lips thoughtfully for a moment, then raises " + fingers + " thin finger" + (fingers > 1 ? "s" : "") + ".*&y\n ";
                        }
                    }
                    else
                    {
                        _debugSegmentCounter = 17;
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
                    _debugSegmentCounter = 18;
                }

                //DEBUG ONLY
                _debugSegmentCounter = 19;

                //add options related to restock 
                if (convo.GetStart() != null)
                {
                    //Adding new choice in conversation
                    _debugSegmentCounter = 20;
                    Node startNode = convo.GetStart();
                    int rand = Stat.Random(1, 3);

                    string choiceText =
                            (rand == 1) ? "Any new wares on the way?" :
                            (rand == 2) ? "Do you have anything else to sell?" : 
                            "Can you let me know if you get any new stock?"
                    ;

                    Choice askRestockChoice = new Choice()
                    {
                        ID = QudUX_RestockerDiscussionStartChoiceID,
                        Text = choiceText,
                        Target = QudUX_RestockDiscussionNodeID,
                        Parent = startNode,
                        Ordinal = 991
                    };
                    
                    startNode.Elements.Add(askRestockChoice);

                    //Creating a node to display restocking info
                    //and adding it to conversation
                    _debugSegmentCounter = 21;
                    Node restockDataNode = new Node()
                    {
                        ID = QudUX_RestockDiscussionNodeID,
                        Text = restockDialog,
                        Parent = convo
                    };

                    restockDataNode.AddChoice(null, "I have more to ask", "Start");
                    restockDataNode.AddChoice(null, "Live and drink.", "End");

                    convo.Elements.Add(restockDataNode);

                    _debugSegmentCounter = 22;
                    startNode.Elements.Sort();
                    _debugSegmentCounter = 23;
                }
                _debugSegmentCounter = 24;
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
            if (ActiveQuestHolders.Count > 0 && convo.GetElementByID("Start") != null)
            {
                string nameList = this.BuildQuestGiverNameList(ActiveQuestHolders);
                Node cNode = convo.GetElementByID("Start") as Node;
                this.RemoveOldQudUXChoices(cNode);

                Choice c = cNode.AddChoice(null, this.StatementLocationOf(nameList), "End");
                c.Parent = cNode;
                c.Actions = new Dictionary<string, string>{{ "ApplyNewQuestGiverEffect", null}};
            }
            if (NewQuestHolders.Count > 0 && convo.GetElementByID("*DynamicQuestSignpostConversationIntro") != null)
            {
                string nameList = this.BuildQuestGiverNameList(NewQuestHolders);
                Node cNode = convo.GetElementByID("*DynamicQuestSignpostConversationIntro") as Node;
                this.RemoveOldQudUXChoices(cNode);

                Choice c = cNode.AddChoice(null, this.QuestionLocationOf(nameList, NewQuestHolders.Count > 1), "End");
                c.Parent = cNode;
                c.Actions = new Dictionary<string, string>{{ "ApplyNewQuestGiverEffect", null}};
            }
            return true;
        }

        public void RemoveOldQudUXChoices(Node cNode)
        {
            List<Choice> choices = cNode.QudUX_GetChoices();
            if (cNode == null || choices == null)
            {
                return;
            }
            for (int i = choices.Count - 1; i >= 0; i--)
            {
                Choice cChoice = choices[i];
                if (cChoice != null && cChoice.Actions != null && cChoice.Actions.ContainsKey("ApplyActiveQuestGiverEffect"))
                {
                    choices.RemoveAt(i);
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
                nameList += questGiverList[i].DisplayNameOnly.Split(',')[0];
            }
            return ConsoleLib.Console.ColorUtility.StripFormatting(nameList);
        }

                /*  VÉ.AISSE BUGFIXES

            Not sure if this delegate was supposed to be a simple action or a predicate
            Qud API will make it a predicate (see XLR.World.Conversations.ConversationDelegates -> LoadDelegates)
            But I don't know if it's supposed to work that way since the way conversations work seems to have
            changed a lot since QudUX development

            I changed the return type so that Qud API makes it an action. I don't see why this should be anything
            but an action.

            I also had to change signature to match ActionReceiver format 

        */

        //Called dynamically by game from ConversationChoice.Execute string:
        [ConversationDelegate]
        public static void ApplyNewQuestGiverEffect(DelegateContext context)
        {
            ApplyQuestGiverEffect(QudUX_ConversationHelper.NewQuestHolders);
        }

        //Called dynamically by game from ConversationChoice.Execute string:

        /*

            Same as above

        */

        [ConversationDelegate]
        public static void ApplyActiveQuestGiverEffect(DelegateContext context)
        {
            ApplyQuestGiverEffect(QudUX_ConversationHelper.ActiveQuestHolders);
        }

        public static bool ApplyQuestGiverEffect(List<GameObject> QuestGivers)
        {
            if (QudUX_ConversationHelper.ConversationPartner != null)
            {
                int randNum = Stat.Random(1, 3);
                Popup.Show(QudUX_ConversationHelper.ConversationPartner.The
                          + QudUX_ConversationHelper.ConversationPartner.DisplayNameOnly + "&y "
                          + ((randNum == 1) ? "points you in the right direction."
                            : (randNum == 2) ? "gives you a rough layout of the area."
                            : "gestures disinterestedly, sending you on your way."));
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
                            questGiver.RemoveEffect(questGiver.GetEffect<QudUX_QuestGiverVision>());
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
