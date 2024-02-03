using BepInEx;
using BepInEx.Configuration;
using DunGen;
using HarmonyLib;
using System.Linq;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Collections.Generic;

namespace LethalCompanyMod.EnemySpawnerPlugin
{
    [BepInPlugin("LethalCompanyMod.EnemySpawner", "EnemySpawner.Plugin", "0.1.0")]
    public class EnemySpawnerPlugin : BaseUnityPlugin
    {
        private static ConfigEntry<int> outsideCount;
        private static ConfigEntry<string> outsideName;
        private static ConfigEntry<int> insideCount;
        private static ConfigEntry<string> insideName;
        private static bool insideSpawned = false;

        private void Awake()
        {
            outsideCount = Config.Bind("Outside Enemies",      // The section under which the option is shown
                                        "OutsideCount",  // The key of the configuration option in the configuration file
                                        1, // The default value
                                        "How many enemies to spawn outside when you land"); // Description of the option to show in the config file

            outsideName = Config.Bind("Outside Enemies",      // The section under which the option is shown
                                        "OutsideName",  // The key of the configuration option in the configuration file
                                        "Flowerman", // The default value
                                        "The ID of the enemy type to spawn"); // Description of the option to show in the config file

            

            insideCount = Config.Bind("Inside Enemies",      // The section under which the option is shown
                                        "InsideCount",  // The key of the configuration option in the configuration file
                                        0, // The default value
                                        "How many enemies to spawn inside when you land"); // Description of the option to show in the config file

            insideName = Config.Bind("Inside Enemies",      // The section under which the option is shown
                                        "InsideName",  // The key of the configuration option in the configuration file
                                        "Flowerman", // The default value
                                        "The ID of the enemy type to spawn"); // Description of the option to show in the config file

            Harmony.CreateAndPatchAll(typeof(EnemySpawnerPlugin));
        }

        private static RoundManager GetRoundManager()
        {
            var gameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            RoundManager rm = null;
            foreach (var gameObject in gameObjects)
            {
                if (gameObject.name == "Systems")
                {
                    rm = gameObject.GetComponentInChildren<RoundManager>();
                }
            }
            return rm;
        }

        private static List<SpawnableEnemyWithRarity> GetEnemies()
        {
            var gameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            List<SpawnableEnemyWithRarity> enemies = null;
            foreach (var gameObject in gameObjects)
            {
                if (gameObject.name == "Environment")
                {
                    enemies = gameObject.GetComponentInChildren<Terminal>().moonsCatalogueList.SelectMany(x => x.Enemies.Concat(x.DaytimeEnemies).Concat(x.OutsideEnemies)).GroupBy(x => x.enemyType.name, (k, v) => v.First()).ToList();
                }
            }
            return enemies;
        }

        [HarmonyPatch(typeof(EnemyVent), "Start")]
        [HarmonyPostfix]
        static void SpawnInsideEnemies()
        {
            if (!insideSpawned)
            {
                var rm = GetRoundManager();
                var spawns = GameObject.FindGameObjectsWithTag("EnemySpawn");

                for (int i = 0; i < insideCount.Value; i++)
                {
                    Debug.Log("index: " + (i % spawns.Length));
                    SpawnEnemyFromVent(spawns[i % spawns.Length].GetComponent<EnemyVent>(), rm, insideName.Value);
                }

                insideSpawned = true;
            }
            
        }

        [HarmonyPatch(typeof(DungeonGenerator), "Generate")]
        [HarmonyPostfix]
        static void SpawnOutsideEnemies()
        {
            var rm = GetRoundManager();

            foreach (var enemy in GetEnemies())
            {
                Debug.Log(enemy.enemyType.name);
            }

            for (int i = 0; i < outsideCount.Value; i++)
            {
                SpawnEnemyOutside(rm, outsideName.Value);
            }
        }

        private static void SpawnEnemyFromVent(EnemyVent vent, RoundManager rm, string enemyName)
        {
            Vector3 position = vent.floorNode.position;
            float y = vent.floorNode.eulerAngles.y;

            var enemyType = GetEnemies().Find(x => x.enemyType.name == enemyName).enemyType;
            enemyType.isOutsideEnemy = false;

            rm.SpawnEnemyGameObject(position, y, vent.enemyTypeIndex, enemyType);

            Debug.Log("Spawned enemy from vent");
            vent.OpenVentClientRpc();
            vent.occupied = false;
        }

        private static void SpawnEnemyOutside(RoundManager rm, string enemyName)
        {
            GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("OutsideAINode");
            Vector3 position = spawnPoints[rm.AnomalyRandom.Next(0, spawnPoints.Length)].transform.position;
            position = rm.GetRandomNavMeshPositionInRadius(position, 4f);
            Debug.Log($"Anomaly random 4: {position.x}, {position.y}, {position.z}");
            int num3 = 0;
            bool flag = false;
            for (int j = 0; j < spawnPoints.Length - 1; j++)
            {
                for (int k = 0; k < rm.spawnDenialPoints.Length; k++)
                {
                    flag = true;
                    if (Vector3.Distance(position, rm.spawnDenialPoints[k].transform.position) < 16f)
                    {
                        num3 = (num3 + 1) % spawnPoints.Length;
                        position = spawnPoints[num3].transform.position;
                        position = rm.GetRandomNavMeshPositionInRadius(position, 4f);
                        flag = false;
                        break;
                    }
                }

                if (flag)
                {
                    break;
                }
            }

            var enemyType = GetEnemies().Find(x => x.enemyType.name == enemyName).enemyType;
            enemyType.isOutsideEnemy = true;
            GameObject enemy = Instantiate(enemyType.enemyPrefab, position, Quaternion.Euler(Vector3.zero));
            enemy.gameObject.GetComponentInChildren<NetworkObject>().Spawn(true);

            if (enemy != null)
            {
                var ai = enemy.GetComponent<EnemyAI>();
                rm.SpawnedEnemies.Add(ai);
                enemy.GetComponent<EnemyAI>().enemyType.numberSpawned++;
            }
        }
    }
}
