using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Harmony;
using UnityEngine;

namespace AutoWorldGen
{
    [HarmonyPatch(typeof(MainMenu), "OnSpawn")]
    public static class StartNewGame
    {
        private static bool firstRun = true;

        public static void Postfix(MainMenu __instance)
        {
            Debug.Log("AWG: MainMenu.OnSpawn.Postfix");

            if (firstRun)
            {
                ScreenPrefabs.Instance.StartCoroutine(DelayedCoroutine(__instance));
                firstRun = false;
            }
            else
            {
                Traverse.Create(__instance).Method("NewGame").GetValue();
            }
        }

        // for some reason it runs properly only the first time - doesn't get past yield second time, results in being stuck in main meny
        private static IEnumerator DelayedCoroutine(MainMenu mainMenu)
        {
            Debug.Log("AWG: NewGameCoroutine before wait");

            yield return new WaitForSeconds(1);
        
            Debug.Log("AWG: NewGameCoroutine past wait");

            Traverse.Create(mainMenu).Method("NewGame").GetValue();
        }
    }

    // crashes on auto-start without delay
    [HarmonyPatch(typeof(ModeSelectScreen), "OnSpawn")]
    public static class StartSurvivalGame
    {
        public static void Postfix(ModeSelectScreen __instance)
        {
            Debug.Log($"AWG: ModeSelectScreen.OnSpawn.Postfix");
            Traverse.Create(__instance).Method("OnClickSurvival").GetValue();
        }
    }

    [HarmonyPatch(typeof(NewGameSettingsScreen), "OnSpawn")]
    public static class NewGameSettings
    {
        public static void Postfix(NewGameSettingsScreen __instance)
        {
            Debug.Log($"AWG: NewGameSettingsScreen.OnSpawn.Postfix");
            Traverse.Create(__instance).Method("NewGame").GetValue();
        }
    }

    [HarmonyPatch(typeof(NewGameSettingsScreen), "SetSeedSetting")]
    public static class SetSeed
    {
        public static void Prefix(ref string input)
        {
            // TODO: get seed from file OR leave it alone for random
            string newSeed = "123456";
            Debug.Log("Setting seed: " + newSeed);
            input = newSeed;
        }
    }

    [HarmonyPatch(typeof(NewGameSettingsScreen), "SetGameTypeToggle")]
    public static class SetCustomWorld
    {
        public static void Prefix(ref bool custom_game)
        {
            custom_game = true;
        }
    }

    [HarmonyPatch(typeof(MinionSelectScreen), "OnSpawn")]
    public static class Embark
    {
        public static void Postfix(MinionSelectScreen __instance)
        {
            Debug.Log($"AWG: MinionSelectScreen.OnSpawn");
            __instance.Deactivate();

            WorldInit();

            // probably should wait a bit then send the seed from here

            GoToMainMenu();
        }

        // not sure if all of this stuff is required for proper world gen
        // some testing needed
        private static void WorldInit()
        {
            Game.Instance.Trigger(-838649377, null);

            Game.Instance.UpdateGameActiveRegion(0, 0, Grid.WidthInCells, Grid.HeightInCells);
            SaveGame.Instance.worldGenSpawner.SpawnEverything();
        }

        private static void GoToMainMenu()
        {
            LoadScreen.ForceStopGame();
            ProcGenGame.WorldGen.Reset();
            App.LoadScene("frontend");
        }
    }
}
