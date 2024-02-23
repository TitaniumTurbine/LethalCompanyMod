using BepInEx;
using DunGen;
using HarmonyLib;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Collections.Generic;
using GameNetcodeStuff;

namespace EnemySpawnerPlugin
{
    [BepInPlugin("TitaniumTurbine.EnemySpawner", "EnemySpawner.Plugin", "1.0.0")]
    public class EnemySpawnerPlugin : BaseUnityPlugin
    {
        private static int outsideMinCount;
        private static int outsideMaxCount;
        private static List<string> outsideNames;
        private static float outsideSpawnChance;
        private static int insideMinCount;
        private static int insideMaxCount;
        private static List<string> insideNames;
        private static float insideSpawnChance;
        private static bool insideSpawned = false;
        private static float playerCountScaling;
        private static bool onlyScaleMax;

        private void Awake()
        {
            var configOutsideMinCount = Config.Bind("Outside Enemies",      // The section under which the option is shown
                                        "OutsideMinCount",  // The key of the configuration option in the configuration file
                                        1, // The default value
                                        "Minimum enemies to spawn outside when you land"); // Description of the option to show in the config file

            var configOutsideMaxCount = Config.Bind("Outside Enemies",      // The section under which the option is shown
                                        "OutsideMaxCount",  // The key of the configuration option in the configuration file
                                        1, // The default value
                                        "Maximum enemies to spawn outside when you land"); // Description of the option to show in the config file

            var configOutsideNames = Config.Bind("Outside Enemies",      // The section under which the option is shown
                                        "OutsideNames",  // The key of the configuration option in the configuration file
                                        "", // The default value
                                        "The IDs of the enemy types to spawn outside, separated by commas. Leave blank to select all enemies. All enemy IDs will be printed to the BepInEx console when loading a moon.\n" +
                                        "Example: Flowerman,Crawler,MouthDog"); // Description of the option to show in the config file

            var configOutsideSpawnChance = Config.Bind("Outside Enemies",      // The section under which the option is shown
                                        "OutsideSpawnChance",  // The key of the configuration option in the configuration file
                                        1.0f, // The default value
                                        "Chance of spawning enemies outside when you land"); // Description of the option to show in the config file


            var configInsideMinCount = Config.Bind("Inside Enemies",      // The section under which the option is shown
                                        "InsideMinCount",  // The key of the configuration option in the configuration file
                                        0, // The default value
                                        "Minimum enemies to spawn inside when you land"); // Description of the option to show in the config file

            var configInsideMaxCount = Config.Bind("Inside Enemies",      // The section under which the option is shown
                                        "InsideMaxCount",  // The key of the configuration option in the configuration file
                                        0, // The default value
                                        "Maximum enemies to spawn inside when you land"); // Description of the option to show in the config file

            var configInsideNames = Config.Bind("Inside Enemies",      // The section under which the option is shown
                                        "InsideNames",  // The key of the configuration option in the configuration file
                                        "", // The default value
                                        "The IDs of the enemy types to spawn outside, separated by commas. Leave blank to select all enemies. All enemy IDs will be printed to the BepInEx console when loading a moon.\n" +
                                        "Example: Flowerman,Crawler,MouthDog"); // Description of the option to show in the config file


            var configInsideSpawnChance = Config.Bind("Inside Enemies",      // The section under which the option is shown
                                        "InsideSpawnChance",  // The key of the configuration option in the configuration file
                                        0.0f, // The default value
                                        "Chance of spawning enemies inside when you land"); // Description of the option to show in the config file

            var configPlayerCountScaling = Config.Bind("General",      // The section under which the option is shown
                                        "PlayerCountScaling",  // The key of the configuration option in the configuration file
                                        0.0f, // The default value
                                        "Multiplier for the min and max counts to scale with number of players.\n" +
                                        "count = count * playerCountScaling * numberOfPlayers\n" +
                                        "Set to 0 to disable scaling with player count."); // Description of the option to show in the config file

            var configOnlyScaleMax = Config.Bind("General",      // The section under which the option is shown
                                        "OnlyScaleMax",  // The key of the configuration option in the configuration file
                                        false, // The default value
                                        "Only apply player count scaling to the max enemy counts"); // Description of the option to show in the config file

            outsideMinCount = configOutsideMinCount.Value;
            outsideMaxCount = configOutsideMaxCount.Value;
            outsideSpawnChance = configOutsideSpawnChance.Value;
            insideMinCount = configInsideMinCount.Value;
            insideMaxCount = configInsideMaxCount.Value;
            insideSpawnChance = configInsideSpawnChance.Value;
            playerCountScaling = configPlayerCountScaling.Value;
            onlyScaleMax = configOnlyScaleMax.Value;

            outsideNames = configOutsideNames.Value.Split(",").ToList();
            insideNames = configInsideNames.Value.Split(",").ToList();

            Harmony.CreateAndPatchAll(typeof(EnemySpawnerPlugin));
        }

        private static List<SpawnableEnemyWithRarity> GetEnemies()
        {
            Debug.Log("GETTING ENEMIES");
            List<SpawnableEnemyWithRarity> enemies = new List<SpawnableEnemyWithRarity>();
            enemies = GameObject.Find("Terminal")
                .GetComponentInChildren<Terminal>()
                .moonsCatalogueList
                .SelectMany(x => x.Enemies.Concat(x.DaytimeEnemies).Concat(x.OutsideEnemies))
                .Where(x => x != null && x.enemyType != null && x.enemyType.name != null)
                .GroupBy(x => x.enemyType.name, (k, v) => v.First())
                .ToList();

            Debug.Log($"Enemy types: {enemies.Count}");
            return enemies;
        }

        [HarmonyPatch(typeof(EnemyAI), "PlayerIsTargetable")]
        [HarmonyPrefix]
        static bool PatchTargetable(object[] __args, ref bool __result, ref EnemyAI __instance)
        {
            var playerScript = (PlayerControllerB)__args[0];
            var cannotBeInShip = (bool)__args[1];
            //var overrideInsideFactoryCheck = (bool)__args[2];
            var overrideInsideFactoryCheck = true;

            if (cannotBeInShip && playerScript.isInHangarShipRoom)
            {
                __result = false;
                return false;
            }

            if (playerScript.isPlayerControlled && !playerScript.isPlayerDead && playerScript.inAnimationWithEnemy == null && (overrideInsideFactoryCheck || playerScript.isInsideFactory != __instance.isOutside) && playerScript.sinkingValue < 0.73f)
            {
                if (__instance.isOutside && StartOfRound.Instance.hangarDoorsClosed)
                {
                    __result = playerScript.isInHangarShipRoom == __instance.isInsidePlayerShip;
                    return false;
                }

                __result = true;
                return false;
            }

            __result = false;
            return false;
        }


        [HarmonyPatch(typeof(EnemyVent), "Start")]
        [HarmonyPostfix]
        static void SpawnInsideEnemies()
        {
            var random = new System.Random();
            var rm = RoundManager.Instance;
            var playerCount = GameNetworkManager.Instance.connectedPlayers;
            var scaling = playerCountScaling > 0 ? playerCount * playerCountScaling : 1;
            if (!insideSpawned && random.NextDouble() < insideSpawnChance && (rm.NetworkManager.IsServer || rm.NetworkManager.IsHost))
            {
                var spawns = GameObject.FindGameObjectsWithTag("EnemySpawn");
                var enemies = GetEnemies();
                var enemyNames = enemies.Select(x => x.enemyType.name).ToList();

                var min = onlyScaleMax ? insideMinCount : (int)(insideMinCount * scaling);
                var max = (int)(insideMaxCount * scaling);
                var validNames = insideNames.Intersect(enemyNames);
                var names = validNames.Count() > 0 ? insideNames : enemyNames;
                for (int i = 0; i < random.Next(min, max); i++)
                {
                    var name = names.ElementAt(random.Next(insideNames.Count()));
                    Debug.Log($"Spawning a {name} inside");
                    SpawnEnemyFromVent(spawns[i % spawns.Length].GetComponent<EnemyVent>(), rm, name);
                }

                insideSpawned = true;
            }
            
        }

        [HarmonyPatch(typeof(RoundManager), "SpawnScrapInLevel")]
        [HarmonyPostfix]
        static void SpawnOutsideEnemies()
        {
            var random = new System.Random();
            var rm = RoundManager.Instance;
            var playerCount = GameNetworkManager.Instance.connectedPlayers;
            var scaling = playerCountScaling > 0 ? playerCount * playerCountScaling : 1;
            if (random.NextDouble() < outsideSpawnChance && (rm.NetworkManager.IsServer || rm.NetworkManager.IsHost))
            {
                var enemies = GetEnemies();
                var enemyNames = enemies.Select(x => x.enemyType.name).ToList();
                foreach (var name in enemyNames)
                {
                    Debug.Log(name);
                }

                var min = onlyScaleMax ? outsideMinCount : (int)(outsideMinCount * scaling);
                var max = (int)(outsideMaxCount * scaling);
                var validNames = outsideNames.Intersect(enemyNames);
                var names = validNames.Count() > 0 ? outsideNames : enemyNames;

                for (int i = 0; i < random.Next(min, max); i++)
                {
                    var name = names.ElementAt(random.Next(names.Count()));
                    Debug.Log($"Spawning a {name} outside");
                    SpawnEnemyOutside(rm, name);
                }
            }
        }

        private static void SpawnEnemyFromVent(EnemyVent vent, RoundManager rm, string enemyName)
        {
            Debug.Log("SpawnEnemyFromVent");
            Vector3 position = vent.floorNode.position;
            float y = vent.floorNode.eulerAngles.y;

            var enemyType = GetEnemies().Find(x => x.enemyType.name == enemyName).enemyType;
            enemyType.isOutsideEnemy = false;

            var enemy = rm.SpawnEnemyGameObject(position, y, vent.enemyTypeIndex, enemyType);
        }

        private static void SpawnEnemyOutside(RoundManager rm, string enemyName)
        {
            Debug.Log("SpawnEnemyOutside");
            GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("OutsideAINode");
            Vector3 position = spawnPoints[rm.AnomalyRandom.Next(0, spawnPoints.Length)].transform.position;
            position = rm.GetRandomNavMeshPositionInRadius(position, 4f);
            Debug.Log($"Anomaly random 4: {position.x}, {position.y}, {position.z}");

            var enemyType = GetEnemies().Find(x => x.enemyType.name == enemyName).enemyType;
            enemyType.isOutsideEnemy = true;

            var enemy = rm.SpawnEnemyGameObject(position, 0, 0, enemyType);
        }
    }

    #region debugging
    /*
        [HarmonyPatch(typeof(EnemyAI), "PlayerIsTargetable")]
        [HarmonyPrefix]
        static bool PatchTargetable(object[] __args, ref bool __result, ref EnemyAI __instance)
        {
            var playerScript = (PlayerControllerB)__args[0];
            var cannotBeInShip = (bool)__args[1];
            var overrideInsideFactoryCheck = (bool)__args[2];

            if (cannotBeInShip && playerScript.isInHangarShipRoom)
            {
                Debug.LogError("cannotBeInShip && playerScript.isInHangarShipRoom");
                __result = false;
                return false;
            }

            Debug.Log("Player controlled: " + playerScript.isPlayerControlled);
            Debug.Log("Player dead: " + playerScript.isPlayerDead);
            Debug.Log("Player in animation with enemy is null: " + (playerScript.inAnimationWithEnemy == null));
            Debug.Log("Override factory check: " + overrideInsideFactoryCheck);
            Debug.Log("Player inside: " + playerScript.isInsideFactory);
            Debug.Log("Enemy outside: " + __instance.isOutside);
            Debug.Log("Player sinking value: " + playerScript.sinkingValue);

            if (playerScript.isPlayerControlled && !playerScript.isPlayerDead && playerScript.inAnimationWithEnemy == null && (overrideInsideFactoryCheck || playerScript.isInsideFactory != __instance.isOutside) && playerScript.sinkingValue < 0.73f)
            {
                if (__instance.isOutside && StartOfRound.Instance.hangarDoorsClosed)
                {
                    Debug.LogError("isOutside && StartOfRound.Instance.hangarDoorsClosed");
                    __result = playerScript.isInHangarShipRoom == __instance.isInsidePlayerShip;
                    return false;
                }

                Debug.Log("Player targetable");
                __result = true;
                return false;
            }

            Debug.LogError("Failed all targeting conditions");
            __result = false;
            return false;
        }

        [HarmonyPatch(typeof(CrawlerAI), "OnCollideWithPlayer")]
        [HarmonyPrefix]
        static bool PatchCollide(Collider other, ref CrawlerAI __instance)
        {
            var t = __instance.GetType().GetField("timeSinceHittingPlayer", BindingFlags.NonPublic | BindingFlags.Instance);
            if (!((float)t.GetValue(__instance) < 0.65f))
            {
                PlayerControllerB playerControllerB = __instance.MeetsStandardPlayerCollisionConditions(other);
                Debug.Log("Crawler collide with player: " + playerControllerB);
                if (playerControllerB != null)
                {
                    t.SetValue(__instance, 0f);
                    playerControllerB.DamagePlayer(40, hasDamageSFX: true, callRPC: true, CauseOfDeath.Mauling);
                    __instance.agent.speed = 0f;
                    __instance.HitPlayerServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
                    GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(1f);
                }
            }
            return false;
        }

        [HarmonyPatch(typeof(PlayerControllerB), "DamagePlayer")]
        [HarmonyPrefix]
        static bool PatchDamage(ref PlayerControllerB __instance)
        {
            Debug.Log("Owner: " + __instance.IsOwner);
            Debug.Log("Dead: " + __instance.isPlayerDead);
            Debug.Log("Allow death: " + __instance.AllowPlayerDeath());
            Debug.Log("Hit? " + !(!__instance.IsOwner || __instance.isPlayerDead || !__instance.AllowPlayerDeath()));
            return true;
        }

        [HarmonyPatch(typeof(EnemyAI), "MeetsStandardPlayerCollisionConditions")]
        [HarmonyPrefix]
        static bool PatchMeetsStandardPlayerCollisionConditions(object[] __args, ref EnemyAI __instance, ref PlayerControllerB __result)
        {
            var other = (Collider)__args[0];
            var inKillAnimation = (bool)__args[1];
            var overrideIsInsideFactoryCheck = (bool)__args[2];
            var r = false;
            if (__instance.isEnemyDead)
            {
                Debug.LogError("isEnemyDead");
                __result = null;
                return r;
            }

            if (!__instance.ventAnimationFinished)
            {
                Debug.LogError("!ventAnimationFinished");
                __result = null;
                return r;
            }

            if (inKillAnimation)
            {
                Debug.LogError("inKillAnimation");
                __result = null;
                return r;
            }

            if (__instance.stunNormalizedTimer >= 0f)
            {
                Debug.LogError("stunNormalizedTimer >= 0f");
                __result = null;
                return r;
            }

            PlayerControllerB component = other.gameObject.GetComponent<PlayerControllerB>();
            if (component == null)
            {
                Debug.LogError("Null player component");
                __result = null;
                return r;
            }
            Debug.Log(component);
            Debug.Log(GameNetworkManager.Instance.localPlayerController);
            if(component != GameNetworkManager.Instance.localPlayerController)
            {
                Debug.LogError("Not local player");
                __result = null;
                return r;
            }

            if (!__instance.PlayerIsTargetable(component, cannotBeInShip: false, overrideIsInsideFactoryCheck))
            {
                Debug.LogError("player not targetable");
                __result = null;
                return r;
            }

            Debug.Log("Returning non-null component");
            __result = component;
            return r;
        }
        */
    #endregion
}
