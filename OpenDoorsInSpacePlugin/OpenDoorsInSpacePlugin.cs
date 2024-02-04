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
        private void Awake()
        {
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
                EjectPatcher.Harmony.PatchAll(typeof(EjectPatcher));
                s.ManuallyEjectPlayersServerRpc();
            }
            
            return true;
        }
    }
}
