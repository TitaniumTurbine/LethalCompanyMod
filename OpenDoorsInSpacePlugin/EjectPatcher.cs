using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

namespace OpenDoorsInSpacePlugin
{
    internal class EjectPatcher
    {
        public static Harmony harmony = new Harmony("EjectPatcher");

        [HarmonyPatch(typeof(MonoBehaviour), "StartCoroutine", typeof(IEnumerator))]
        [HarmonyPrefix]
        static bool StartCoroutine(ref MonoBehaviour __instance)
        {
            harmony.UnpatchSelf();
            __instance.StartCoroutine(Eject());

            return false;
        }

        private static IEnumerator Eject()
        {
            var s = StartOfRound.Instance;
            var endGame = OpenDoorsInSpacePlugin.EndGame;

            s.shipDoorsAnimator.SetBool("OpenInOrbit", true);
            s.shipDoorAudioSource.PlayOneShot(s.airPressureSFX);
            s.starSphereObject.SetActive(value: true);
            s.starSphereObject.transform.position = GameNetworkManager.Instance.localPlayerController.transform.position;
            yield return new WaitForSeconds(0.25f);
            s.suckingPlayersOutOfShip = true;
            s.suckingFurnitureOutOfShip = true;
            PlaceableShipObject[] array = UnityEngine.Object.FindObjectsOfType<PlaceableShipObject>();
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].parentObject == null)
                {
                    Debug.Log("Error! No parentObject for placeable object: " + s.unlockablesList.unlockables[array[i].unlockableID].unlockableName);
                }

                array[i].parentObject.StartSuckingOutOfShip();
                if (s.unlockablesList.unlockables[array[i].unlockableID].spawnPrefab)
                {
                    Collider[] componentsInChildren = array[i].parentObject.GetComponentsInChildren<Collider>();
                    for (int j = 0; j < componentsInChildren.Length; j++)
                    {
                        componentsInChildren[j].enabled = false;
                    }
                }
            }

            GameNetworkManager.Instance.localPlayerController.inSpecialInteractAnimation = true;
            GameNetworkManager.Instance.localPlayerController.DropAllHeldItems();
            HUDManager.Instance.UIAudio.PlayOneShot(s.suckedIntoSpaceSFX);
            yield return new WaitForSeconds(6f);
            SoundManager.Instance.SetDiageticMixerSnapshot(3, 2f);

            if (endGame)
            {
                HUDManager.Instance.ShowPlayersFiredScreen(show: true);
            }

            yield return new WaitForSeconds(2f);
            s.starSphereObject.SetActive(value: false);
            s.shipDoorAudioSource.Stop();
            s.speakerAudioSource.Stop();
            s.suckingFurnitureOutOfShip = false;
            if (s.IsServer)
            {
                if (endGame)
                {
                    GameNetworkManager.Instance.ResetSavedGameValues();

                    Debug.Log("Calling reset ship!");
                    s.ResetShip();
                }
                else
                {
                    var resetFurniture = s.GetType().GetMethod("ResetShipFurniture", BindingFlags.NonPublic
                | BindingFlags.Instance);
                    resetFurniture.Invoke(s, new object[] {false, false});
                }
            }

            if (endGame)
            {
                UnityEngine.Object.FindObjectOfType<Terminal>().SetItemSales();
            }
            
            yield return new WaitForSeconds(6f);
            s.shipAnimatorObject.gameObject.GetComponent<Animator>().SetBool("AlarmRinging", false);
            GameNetworkManager.Instance.localPlayerController.TeleportPlayer(s.playerSpawnPositions[GameNetworkManager.Instance.localPlayerController.playerClientId].position);
            s.shipDoorsAnimator.SetBool("OpenInOrbit", false);
            s.currentPlanetPrefab.transform.position = s.planetContainer.transform.position;
            s.suckingPlayersOutOfShip = false;

            var prop = s.GetType().GetField("choseRandomFlyDirForPlayer", BindingFlags.NonPublic
                | BindingFlags.Instance);
            prop.SetValue(s, false);

            //s.choseRandomFlyDirForPlayer = false;
            s.suckingPower = 0f;
            s.shipRoomLights.SetShipLightsOnLocalClientOnly(setLightsOn: true);
            yield return new WaitForSeconds(2f);
            if (s.IsServer)
            {
                var prop2 = s.GetType().GetField("playersRevived", BindingFlags.NonPublic
                | BindingFlags.Instance);
                prop2.SetValue(s, (int)prop2.GetValue(s) + 1);

                //s.playersRevived++;
                yield return new WaitUntil(() => (int)prop2.GetValue(s) >= GameNetworkManager.Instance.connectedPlayers);
                prop2.SetValue(s, 0);

                //var oldBool = s.isChallengeFile;
                //s.isChallengeFile = true;
                s.EndPlayersFiredSequenceClientRpc();
                //s.isChallengeFile = oldBool;
            }
            else
            {
                s.PlayerHasRevivedServerRpc();
            }
        }
    }
}
