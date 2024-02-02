using BepInEx;
using BepInEx.Configuration;
using DunGen;
using HarmonyLib;
using System.Linq;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace LethalCompanyMod.EnemySpawnerPlugin
{
    public class EnemySpawnerPlugin : BaseUnityPlugin
    {
        private static ConfigEntry<int> outsideCount;
        private static ConfigEntry<string> outsideName;
        private static ConfigEntry<int> insideCount;
        private static ConfigEntry<string> insideName;

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

        [HarmonyPatch(typeof(DungeonGenerator), "Generate")]
        [HarmonyPostfix]
        static void SpawnEnemies()
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

            var names = rm.currentLevel.Enemies.Select(x => x.enemyType.name);
            foreach (var name in names)
            {
                Debug.Log(name);
            }

            for (int i = 0; i < outsideCount.Value; i++)
            {
                //Flowerman
                SpawnEnemyOutside(rm, outsideName.Value);
            }

            for (int i = 0; i < insideCount.Value; i++)
            {
                var spawns = rm.allEnemyVents;
                Debug.LogError(spawns.Length);
                var altSpawns = GameObject.FindGameObjectsWithTag("EnemySpawn");
                Debug.LogError(altSpawns.Length);
                SpawnEnemyFromVent(spawns[i / spawns.Length].GetComponent<EnemyVent>(), rm, insideName.Value);
            }
        }

        private static void SpawnEnemyFromVent(EnemyVent vent, RoundManager rm, string enemyName)
        {
            Vector3 position = vent.floorNode.position;
            float y = vent.floorNode.eulerAngles.y;

            var enemyType = rm.currentLevel.Enemies.Find(x => x.enemyType.name == enemyName).enemyType;
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


            var enemyType = rm.currentLevel.Enemies.Find(x => x.enemyType.name == enemyName).enemyType;
            enemyType.isOutsideEnemy = true;
            GameObject enemy = Instantiate(enemyType.enemyPrefab, position, Quaternion.Euler(Vector3.zero));
            enemy.gameObject.GetComponentInChildren<NetworkObject>().Spawn(true);

            if (enemy != null)
            {
                var ai = enemy.GetComponent<EnemyAI>();
                Debug.LogError(ai.enemyType.isOutsideEnemy);
                Debug.LogError(ai.allAINodes.Length);
                rm.SpawnedEnemies.Add(ai);
                enemy.GetComponent<EnemyAI>().enemyType.numberSpawned++;
            }
        }
    }
}
