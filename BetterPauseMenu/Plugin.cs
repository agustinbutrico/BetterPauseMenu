using BepInEx;
using HarmonyLib;

namespace BetterPauseMenu
{
    [BepInPlugin("AgusBut.BetterPauseMenu", "BetterPauseMenu", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        public static BepInEx.Logging.ManualLogSource Log { get; private set; }

        private void Awake()
        {
            Instance = this;
            Log = base.Logger;

            Logger.LogInfo("Loading [BanishCards 1.0.1]");

            var harmony = new Harmony("AgusBut.BetterPauseMenu");
            harmony.PatchAll();
        }
    }
}
