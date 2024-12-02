using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;

namespace LargeLobby;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    private void Awake()
    {
        Logger = base.Logger;
        Harmony.CreateAndPatchAll(typeof(JoinGameRowPatch));
        Harmony.CreateAndPatchAll(typeof(JoinGameRowButtonPatch));
        Harmony.CreateAndPatchAll(typeof(FriendInfoPatch));
        Logger.LogWarning("LargeLobby is now enabled.");
    }

    [HarmonyPatch(typeof(JoinGameRow), "OnJoinGame")]
    public class JoinGameRowPatch
    {
        private static bool Prefix(JoinGameRow __instance)
        {
            FriendLobbyInfo value = Traverse.Create(__instance).Field<FriendLobbyInfo>("m_lobby").Value;
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
        public static void Postfix(JoinGameRow __instance, FriendLobbyInfo lobbyInfo)
        {
            TMP_Text value = Traverse.Create(__instance).Field<TMP_Text>("m_playersInGameText").Value;
            ButtonWidget value2 = Traverse.Create(__instance).Field<ButtonWidget>("m_joinGameButton").Value;
            value.text = lobbyInfo.PlayersInGame.ToString() + " Players";
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
}
