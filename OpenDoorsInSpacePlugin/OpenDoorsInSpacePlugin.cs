using BepInEx;
using HarmonyLib;
using System.Linq;
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
            var lever = SceneManager.GetActiveScene().GetRootGameObjects().ToList().Find(x => x.name == "Environment").GetComponentInChildren<StartMatchLever>();
            var s = StartOfRound.Instance;
            var noQuota = (TimeOfDay.Instance.quotaFulfilled - TimeOfDay.Instance.profitQuota) <= 0;
            var aboutToFire = noQuota && (TimeOfDay.Instance.daysUntilDeadline <= 0 && s.shipIsLeaving || TimeOfDay.Instance.timeUntilDeadline <= 0);
            if (!door.buttonsEnabled && closed == false && !aboutToFire && !lever.leverHasBeenPulled)
            {
                var daysSpent = s.gameStats.daysSpent;
                var scrapValueCollected = s.gameStats.scrapValueCollected;
                var deaths = s.gameStats.deaths;
                var allStepsTaken = s.gameStats.allStepsTaken;
                EjectPatcher.harmony.PatchAll(typeof(EjectPatcher));
                if(!EndGame) FirstDayPatcher.harmony.PatchAll(typeof(FirstDayPatcher));
                s.ManuallyEjectPlayersServerRpc();

                if (!EndGame)
                {
                    s.gameStats.daysSpent = daysSpent;
                    s.gameStats.scrapValueCollected = scrapValueCollected;
                    s.gameStats.deaths = deaths;
                    s.gameStats.allStepsTaken = allStepsTaken;
                }
            }
            
            return true;
        }
    }
}
