using BepInEx;
using GameNetcodeStuff;
using HarmonyLib;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenDoorsInSpacePlugin
{
    [BepInPlugin("TitaniumTurbine.OpenDoorsInSpace", "OpenDoorsInSpace.Plugin", "1.0.0")]
    public class OpenDoorsInSpacePlugin : BaseUnityPlugin
    {
        public static bool EndGame = false;

        private void Awake()
        {
            var configEndGame = Config.Bind("General",      // The section under which the option is shown
                                        "EndGameOnEject",  // The key of the configuration option in the configuration file
                                        false, // The default value
                                        "Whether to end the game when you open the doors, the same as when you get fired"); // Description of the option to show in the config file

            EndGame = configEndGame.Value;
            Harmony.CreateAndPatchAll(typeof(OpenDoorsInSpacePlugin));
        }

        [HarmonyPatch(typeof(HangarShipDoor), "PlayDoorAnimation")]
        [HarmonyPrefix]
        static bool SetDoorsClosed (bool closed)
        {
            var door = SceneManager.GetActiveScene().GetRootGameObjects().ToList().Find(x => x.name == "Environment").GetComponentInChildren<HangarShipDoor>();
            if (!door.buttonsEnabled && closed == false)
            {
                var s = StartOfRound.Instance;
                int[] endGameStats = new int[4] { s.gameStats.daysSpent, s.gameStats.scrapValueCollected, s.gameStats.deaths, s.gameStats.allStepsTaken };
                EjectPatcher.harmony.PatchAll(typeof(EjectPatcher));
                if(!EndGame) FirstDayPatcher.harmony.PatchAll(typeof(FirstDayPatcher));
                s.ManuallyEjectPlayersServerRpc();
            }
            
            return true;
        }
    }
}
