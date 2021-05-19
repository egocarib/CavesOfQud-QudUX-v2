using HarmonyLib;
using XRL.World;

namespace QudUX.HarmonyPatches
{
    [HarmonyPatch(typeof(BeginConversationEvent))]
    class Patch_XRL_World_BeginConversationEvent
    {
        //Used to ensure our conversation helper can get an event AFTER the game applies its own
        //dynamic quest giver conversation (which strips out the conversation and rebuilds it)
        [HarmonyPostfix]
        [HarmonyPatch("Check")]
        static void Postfix(GameObject Actor, GameObject SpeakingWith, Conversation Conversation)
        {
            if (GameObject.validate(ref Actor) && Actor.IsPlayer() && Actor.HasRegisteredEvent("PlayerBeginConversation"))
            {
                Actor.FireEvent(Event.New("PlayerBeginConversation", "Conversation", Conversation, "Speaker", SpeakingWith));
            }
        }
    }
}