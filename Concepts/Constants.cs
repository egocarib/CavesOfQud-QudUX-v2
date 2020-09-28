using System;
using System.Reflection;
using System.Collections.Generic;
using XRL;
using XRL.UI;
using XRL.Core;
using XRL.Messages;
using XRL.World;
using XRL.World.Parts;
using XRL.World.Skills.Cooking;
using QudUX.ScreenExtenders;
using ConsoleLib.Console;
using QudUX.Utilities;
using static HarmonyLib.SymbolExtensions;
using static HarmonyLib.AccessTools;
using System.IO;
using System.Linq;

namespace QudUX.Concepts
{
    public static class Constants
    {
        public static string AbilityDataFileName => "QudUX_AbilityData.xml";

        public static string AutogetDataFileName => "QudUX_AutogetSettings.json";

        public static string AutogetDataFilePath => Path.Combine(ModDirectory, AutogetDataFileName);

        private static string _modDirectory = null;
        public static string ModDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(_modDirectory))
                {
                    ModManager.ForEachMod(delegate (ModInfo mod)
                    {
                        if (mod?.manifest?.id == "QudUX" || mod?.workshopInfo?.Title == "QudUX")
                        {
                            _modDirectory = mod.Path;
                            return;
                        }
                    });
                }
                return _modDirectory;
            }
        }

        public static class MethodsAndFields
        {
            public static ConstructorInfo TextBlock_ctor
            {
                get
                {
                    ConstructorInfo ret = null;
                    List<ConstructorInfo> ctorsFromTextBlock = GetDeclaredConstructors(typeof(TextBlock));
                    foreach (ConstructorInfo ctor in ctorsFromTextBlock)
                    {
                        ret = ctor;
                        ParameterInfo[] argumentTypes = ctor.GetParameters();
                        if (argumentTypes.Length > 0 && argumentTypes[0].ParameterType == typeof(string))
                        {
                            return ret;
                        }
                    }
                    return ret;
                }
            }
            public static ConstructorInfo List_Bool_ctor
            {
                get { return typeof(List<bool>).GetConstructor(Type.EmptyTypes); }
            }
            public static ConstructorInfo List_GameObject_ctor
            {
                get { return typeof(List<GameObject>).GetConstructor(Type.EmptyTypes); }
            }
            public static MethodInfo List_AbilityNode_get_Item
            {
                get { return typeof(List<AbilityNode>).GetMethod("get_Item", new Type[] { typeof(int) }); }
            }
            public static MethodInfo List_GameObjectBlueprint_get_Count
            {
                get { return typeof(List<GameObjectBlueprint>).GetMethod("get_Count"); }
            }
            public static MethodInfo AbilityManagerExtender_UpdateAbilityText
            {
                get { return GetMethodInfo(() => AbilityManagerExtender.UpdateAbilityText(default(AbilityNode), default(TextBlock))); }
            }
            public static MethodInfo Campfire_GetValidCookingIngredients
            {
                get { return GetMethodInfo(() => Campfire.GetValidCookingIngredients()); } //variant with no parameters
            }
            public static MethodInfo GameObject_HasSkill
            {
                //get { return GetMethodInfo((GameObject g) => g.HasSkill(default(string))); }
                get { return typeof(GameObject).GetMethod("HasSkill", new Type[] { typeof(string) }); }
            }
            public static MethodInfo Popup_ShowOptionList
            {
                get { return typeof(Popup).GetMethod("ShowOptionList"); }
            }
            public static MethodInfo QudUX_IngredientSelectionScreen_Static_Show
            {
                get { return GetMethodInfo(() => QudUX_IngredientSelectionScreen.Static_Show(default(List<GameObject>), default(List<bool>))); }
            }
            public static MethodInfo QudUX_RecipeSelectionScreen_Static_Show
            {
                get { return GetMethodInfo(() => QudUX_RecipeSelectionScreen.Static_Show(default(List<Tuple<string, CookingRecipe>>))); }
            }
            public static MethodInfo ScreenBuffer_Write
            {
                get { return typeof(ScreenBuffer).GetMethod("Write", new Type[] { typeof(string), typeof(bool) }); }
            }
            public static MethodInfo ConversationUIExtender_DrawConversationSpeakerTile
            {
                get { return GetMethodInfo(() => ConversationUIExtender.DrawConversationSpeakerTile(default(ScreenBuffer), default(GameObject))); }
            }
            public static MethodInfo GameObject_RemoveFromContext
            {
                get { return typeof(XRL.World.GameObject).GetMethod("RemoveFromContext"); }
            }
            public static MethodInfo Stat_Random
            {
                get { return GetMethodInfo(() => XRL.Rules.Stat.Random(default(int), default(int))); }
            }
            public static MethodInfo MessageQueue_AddPlayerMessage
            {
                get { return typeof(MessageQueue).GetMethod("AddPlayerMessage", new Type[] { typeof(string) }); }
            }
            public static MethodInfo ParticleTextMaker_EmitFromPlayer
            {
                get { return GetMethodInfo(() => ParticleTextMaker.EmitFromPlayer(default(string))); }
            }
            public static MethodInfo ParticleTextMaker_EmitFromPlayerIfLiquid
            {
                get { return GetMethodInfo(() => ParticleTextMaker.EmitFromPlayerIfLiquid(default(GameObject), default(bool))); }
            }
            public static MethodInfo ParticleTextMaker_EmitFromPlayerIfBarrierInDifferentZone
            {
                get { return GetMethodInfo(() => ParticleTextMaker.EmitFromPlayerIfBarrierInDifferentZone(default(GameObject))); }
            }
            public static MethodInfo IPart_get_ParentObject //XRL.World.GameObject XRL.World.IPart::get_ParentObject()
            {
                get { return typeof(IPart).GetMethod("get_ParentObject"); }
            }
            public static MethodInfo IComponent_GameObject_AddPlayerMessage
            {
                get { return typeof(IComponent<GameObject>).GetMethod("AddPlayerMessage", new Type[] { typeof(string) }); }
            }
            public static MethodInfo Cell_HasBridge
            {
                get { return typeof(Cell).GetMethod("HasBridge"); }
            }
            public static MethodInfo GameObject_IsDangerousOpenLiquidVolume
            {
                get { return typeof(GameObject).GetMethod("IsDangerousOpenLiquidVolume"); }
            }
            public static MethodInfo Cell_GetDangerousOpenLiquidVolume
            {
                get { return typeof(Cell).GetMethod("GetDangerousOpenLiquidVolume"); }
            }
            public static MethodInfo GameObject_get_ShortDisplayName
            {
                get { return typeof(GameObject).GetMethod("get_ShortDisplayName"); }
            }
            public static MethodInfo GameObject_get_the
            {
                get { return typeof(GameObject).GetMethod("get_the"); }
            }
            public static MethodInfo TextConsole_DrawBuffer
            {
                get { return typeof(TextConsole).GetMethod("DrawBuffer"); }
            }
            public static MethodInfo ImposterManager_getImposterUpdateFrame
            {
                get { return typeof(ImposterManager).GetMethod("getImposterUpdateFrame"); }
            }
            public static MethodInfo Popup_ShowConversation
            {
                get { return typeof(Popup).GetMethod("ShowConversation"); }
            }
            public static MethodInfo CreateCharacter_BuildLibraryManagement
            {
                get { return typeof(CreateCharacter).GetMethod("BuildLibraryManagement"); }
            }
            public static MethodInfo QudUX_BuildLibraryScreen_Show
            {
                get { return GetMethodInfo(() => QudUX_BuildLibraryScreen.Show()); }
            }
            public static MethodInfo CreateCharacterExtender_WriteCharCreateSpriteOptionText
            {
                get { return GetMethodInfo(() => CreateCharacterExtender.WriteCharCreateSpriteOptionText(default(ScreenBuffer))); }
            }
            public static MethodInfo CreateCharacterExtender_PickCharacterTile
            {
                get { return GetMethodInfo(() => CreateCharacterExtender.PickCharacterTile(default(CharacterTemplate))); }
            }
            public static MethodInfo Events_EmbarkEvent
            {
                get { return GetMethodInfo(() => QudUX.Concepts.Events.EmbarkEvent()); }
            }
            public static MethodInfo Events_SaveLoadEvent
            {
                get { return GetMethodInfo(() => QudUX.Concepts.Events.SaveLoadEvent()); }
            }
            public static MethodInfo Events_OnLoadAlwaysEvent
            {
                get { return GetMethodInfo(() => QudUX.Concepts.Events.OnLoadAlwaysEvent()); }
            }
            public static MethodInfo LookExtender_AddMarkLegendaryOptionToLooker
            {
                get { return GetMethodInfo(() => LookExtender.AddMarkLegendaryOptionToLooker(default(ScreenBuffer), default(GameObject))); }
            }
            public static MethodInfo LookExtender_CheckKeyPress
            {
                get { return GetMethodInfo(() => LookExtender.CheckKeyPress(default(Keys), default(GameObject), default(bool))); }
            }
            public static FieldInfo AbilityNode_Ability
            {
                get { return typeof(AbilityNode).GetField("Ability"); }
            }
            public static FieldInfo XRLCore_MoveConfirmDirection
            {
                get { return typeof(XRLCore).GetField("MoveConfirmDirection"); }
            }
            public static FieldInfo ConversationUI__ScreenBuffer
            {
                get { return typeof(ConversationUI).GetField("_ScreenBuffer"); }
            }
            public static FieldInfo ConversationUI__TextConsole
            {
                get { return typeof(ConversationUI).GetField("_TextConsole"); }
            }
            public static FieldInfo CreateCharacter__Console
            {
                get { return DeclaredField(typeof(CreateCharacter), "_Console"); }
            }
            public static FieldInfo CreateCharacter_Template
            {
                get { return DeclaredField(typeof(CreateCharacter), "Template"); }
            }
            public static FieldInfo CharacterTemplate_PlayerBody
            {
                get { return DeclaredField(typeof(CharacterTemplate), "PlayerBody"); }
            }
            public static FieldInfo Look_Buffer //class ConsoleLib.Console.ScreenBuffer XRL.UI.Look::Buffer
            {
                get { return DeclaredField(typeof(Look), "Buffer"); }
            }
        }
    }
}
