using BepInEx;
using DunGen;
using HarmonyLib;
using System.Linq;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Collections.Generic;

namespace EnemySpawnerPlugin
{
    [BepInPlugin("TitaniumTurbine.EnemySpawner", "EnemySpawner.Plugin", "1.0.0")]
    public class EnemySpawnerPlugin : BaseUnityPlugin
    {
        private static int outsideMinCount;
        private static int outsideMaxCount;
        private static string[] outsideNames;
        private static float outsideSpawnChance;
        private static int insideMinCount;
        private static int insideMaxCount;
        private static string[] insideNames;
        private static float insideSpawnChance;
        private static bool insideSpawned = false;

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
                                        "Centipede,SandSpider,HoarderBug,Flowerman,Crawler,Blob,DressGirl,Puffer,Nutcracker,RedLocustBees,Doublewing,DocileLocustBees,MouthDog,ForestGiant,SandWorm,BaboonHawk,SpringMan,Jester,LassoMan,MaskedPlayerEnemy", // The default value
                                        "The IDs of the enemy types to spawn outside, separated by commas"); // Description of the option to show in the config file

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
                                        "Centipede,SandSpider,HoarderBug,Flowerman,Crawler,Blob,DressGirl,Puffer,Nutcracker,RedLocustBees,Doublewing,DocileLocustBees,MouthDog,ForestGiant,SandWorm,BaboonHawk,SpringMan,Jester,LassoMan,MaskedPlayerEnemy", // The default value
                                        "The IDs of the enemy types to spawn inside, separated by commas"); // Description of the option to show in the config file

            var configInsideSpawnChance = Config.Bind("Inside Enemies",      // The section under which the option is shown
                                        "InsideSpawnChance",  // The key of the configuration option in the configuration file
                                        0.0f, // The default value
                                        "Chance of spawning enemies inside when you land"); // Description of the option to show in the config file

            outsideMinCount = configOutsideMinCount.Value;
            outsideMaxCount = configOutsideMaxCount.Value;
            outsideSpawnChance = configOutsideSpawnChance.Value;
            insideMinCount = configInsideMinCount.Value;
            insideMaxCount = configInsideMaxCount.Value;
            insideSpawnChance = configInsideSpawnChance.Value;

            outsideNames = configOutsideNames.Value.Split(",");
            insideNames = configInsideNames.Value.Split(",");

            Harmony.CreateAndPatchAll(typeof(EnemySpawnerPlugin));
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

            Debug.Log(enemies.Count);
            return enemies;
        }

        [HarmonyPatch(typeof(EnemyVent), "Start")]
        [HarmonyPostfix]
        static void SpawnInsideEnemies()
        {
            var random = new System.Random();
            if (!insideSpawned && random.NextDouble() < insideSpawnChance)
            {
                var rm = RoundManager.Instance;
                var spawns = GameObject.FindGameObjectsWithTag("EnemySpawn");

                for (int i = 0; i < random.Next(insideMinCount, insideMaxCount); i++)
                {
                    SpawnEnemyFromVent(spawns[i % spawns.Length].GetComponent<EnemyVent>(), rm, insideNames[random.Next(insideNames.Length)]);
                }

                insideSpawned = true;
            }
            
        }

        [HarmonyPatch(typeof(DungeonGenerator), "Generate")]
        [HarmonyPostfix]
        static void SpawnOutsideEnemies()
        {
            var random = new System.Random();
            if(random.NextDouble() < outsideSpawnChance)
            {
                var rm = RoundManager.Instance;

                foreach (var enemy in GetEnemies())
                {
                    Debug.Log(enemy.enemyType.name);
                }

                for (int i = 0; i < random.Next(outsideMinCount, outsideMaxCount); i++)
                {
                    SpawnEnemyOutside(rm, outsideNames[random.Next(outsideNames.Length)]);
                }
            }
        }

        private static void SpawnEnemyFromVent(EnemyVent vent, RoundManager rm, string enemyName)
        {
            Vector3 position = vent.floorNode.position;
            float y = vent.floorNode.eulerAngles.y;

            var enemyType = GetEnemies().Find(x => x.enemyType.name == enemyName).enemyType;
            enemyType.isOutsideEnemy = false;

            rm.SpawnEnemyGameObject(position, y, vent.enemyTypeIndex, enemyType);
        }

        private static void SpawnEnemyOutside(RoundManager rm, string enemyName)
        {
            GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("OutsideAINode");
            Vector3 position = spawnPoints[rm.AnomalyRandom.Next(0, spawnPoints.Length)].transform.position;
            position = rm.GetRandomNavMeshPositionInRadius(position, 4f);
            Debug.Log($"Anomaly random 4: {position.x}, {position.y}, {position.z}");

            var enemyType = GetEnemies().Find(x => x.enemyType.name == enemyName).enemyType;
            enemyType.isOutsideEnemy = true;
            rm.SpawnEnemyGameObject(position, 0, 0, enemyType);
        }
    }
}
