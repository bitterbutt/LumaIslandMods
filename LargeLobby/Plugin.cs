using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;

namespace LargeLobby
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = base.Logger;
            Harmony.CreateAndPatchAll(typeof(JoinGameRowPatch));
            Harmony.CreateAndPatchAll(typeof(JoinGameRowButtonPatch));
            Harmony.CreateAndPatchAll(typeof(FriendInfoPatch));
            Harmony.CreateAndPatchAll(typeof(SteamLobbyControllerPatch));
            Log.LogWarning("LargeLobby is now enabled.");
        }

        [HarmonyPatch(typeof(JoinGameRow), "OnJoinGame")]
        public class JoinGameRowPatch
        {
            private static readonly FieldInfo m_lobby_Field = AccessTools.Field(typeof(JoinGameRow), "m_lobby");

            private static bool Prefix(JoinGameRow __instance)
            {
                FriendLobbyInfo value = (FriendLobbyInfo)m_lobby_Field.GetValue(__instance);
                if (value != null)
                {
                    value.JoinLobby();
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(JoinGameRow), "Initialize")]
        public class JoinGameRowButtonPatch
        {
            private static readonly FieldInfo m_playersInGameText_Field = AccessTools.Field(typeof(JoinGameRow), "m_playersInGameText");
            private static readonly FieldInfo m_joinGameButton_Field = AccessTools.Field(typeof(JoinGameRow), "m_joinGameButton");

            public static void Postfix(JoinGameRow __instance, FriendLobbyInfo lobbyInfo)
            {
                TMP_Text value = (TMP_Text)m_playersInGameText_Field.GetValue(__instance);
                ButtonWidget value2 = (ButtonWidget)m_joinGameButton_Field.GetValue(__instance);
                value.text = $"{lobbyInfo.PlayersInGame} Players";
                value2.Interactable = true;
            }
        }

        [HarmonyPatch(typeof(FriendInfo), "CanInvite")]
        public class FriendInfoPatch
        {
            private static void Postfix(ref bool __result, FriendInfo __instance)
            {
                __result = !__instance.IsInGameWithHost;
            }
        }

        [HarmonyPatch(typeof(SteamLobbyController), "OnReceiveLobbyData")]
        class SteamLobbyControllerPatch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                bool patched = false;
                for (int i = 0; i <= codes.Count - 6; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldloc_S && codes[i + 1].opcode == OpCodes.Ldc_I4_4 &&
                        codes[i + 2].opcode == OpCodes.Clt && codes[i + 3].opcode == OpCodes.Ldc_I4_0 &&
                        codes[i + 4].opcode == OpCodes.Ceq && codes[i + 5].opcode == OpCodes.Stloc_S)
                    {
                        var flag2Local = codes[i + 5].operand;
                        codes.RemoveRange(i, 6);
                        codes.Insert(i, new CodeInstruction(OpCodes.Ldc_I4_0));
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Stloc_S, flag2Local));
                        patched = true;
                        break;
                    }
                }

                if (!patched)
                {
                    Log.LogError("Failed to patch SteamLobbyController.OnReceiveLobbyData.");
                }

                return codes.AsEnumerable();
            }
        }
    }
}
