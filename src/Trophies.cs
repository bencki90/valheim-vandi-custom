using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Vandi
{
    /// <summary>
    /// A boss summoned with stars drops one more trophy for each star.
    ///
    /// The stars are the difficulty, and they are already per boss and per summoner: Summon gives
    /// the boss one star per kill its summoner has of that boss, held at BossStarCap, and writes
    /// it into the creature's level. So the level read at death is the tracked difficulty, and
    /// nothing new has to be stored. A boss that was not summoned through an altar has no stars
    /// and drops what the game gives.
    ///
    /// Done in GenerateDropList for the reason Metal gives: it runs on the owner of the corpse
    /// for both the CharacterDrop and the Ragdoll path. Only an entry whose item is a trophy is
    /// raised, so a boss's other loot is left as the game made it.
    /// </summary>
    internal static class Trophies
    {
        [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
        [HarmonyPostfix]
        private static void BossList(CharacterDrop __instance, List<KeyValuePair<GameObject, int>> __result)
        {
            if (__result == null || __result.Count == 0) return;

            try
            {
                if (!VandiConfig.Enabled.Value || !VandiConfig.ExtraTrophies.Value) return;

                Character character = __instance == null ? null : __instance.GetComponent<Character>();
                if (character == null || !Bosses.IsBossKey(Bosses.KeyOf(character))) return;

                int extra = Math.Min(character.GetLevel() - 1, Math.Max(0, VandiConfig.BossStarCap.Value));
                if (extra <= 0) return;

                for (int i = 0; i < __result.Count; i++)
                {
                    KeyValuePair<GameObject, int> entry = __result[i];
                    if (entry.Key == null || !IsTrophy(entry.Key)) continue;

                    __result[i] = new KeyValuePair<GameObject, int>(entry.Key, entry.Value + extra);

                    if (VandiConfig.Verbose.Value)
                        VandiPlugin.Log.LogInfo(entry.Key.name + " drops " + extra + " extra for a "
                            + extra + " star boss.");
                }
            }
            catch (Exception error)
            {
                VandiPlugin.LogOnce("Adding a boss's extra trophies failed, so its drops were left as "
                    + "the game made them: " + error);
            }
        }

        private static bool IsTrophy(GameObject prefab)
        {
            ItemDrop drop;
            return prefab.TryGetComponent(out drop) && drop.m_itemData != null
                && drop.m_itemData.m_shared != null
                && drop.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy;
        }
    }
}
