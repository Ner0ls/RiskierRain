using BepInEx;
using HG;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RiskierRain.CoreModules;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskierRain
{
    internal partial class RiskierRainPlugin : BaseUnityPlugin
    {
        float softEliteHealthBoostCoefficient = 2f; //3
        float rareEliteHealthBoostCoefficient = 4f; //5
        float baseEliteHealthBoostCoefficient = 3f; //4
        float T2EliteHealthBoostCoefficient = 9; //18
        float rareEliteDamageBoostCoefficient = 2f; //2.5f
        float baseEliteDamageBoostCoefficient = 1.5f; //2
        float T2EliteDamageBoostCoefficient = 4f; //6
        public static float overloadingBombDamage = 1.5f; //0.5f

        public static int Tier2EliteMinimumStageDefault = 5;
        public static int Tier2EliteMinimumStageDrizzle = 10;
        public static int Tier2EliteMinimumStageRainstorm = 5;
        public static int Tier2EliteMinimumStageMonsoon = 3;
        public static int Tier2EliteMinimumStageEclipse = 3;

        static string Tier2EliteName = "Tier 2";

        void ChangeEliteStats()
        {
            if(Tier2EliteMinimumStageDrizzle != Tier2EliteMinimumStageDefault 
                || Tier2EliteMinimumStageMonsoon != Tier2EliteMinimumStageDefault
                || Tier2EliteMinimumStageEclipse != Tier2EliteMinimumStageDefault)
            {
                drizzleDesc += $"\n>{Tier2EliteName} Elites appear starting on <style=cIsHealing>Stage {Tier2EliteMinimumStageDrizzle + 1}</style>";
                rainstormDesc += $"\n>{Tier2EliteName} Elites appear starting on Stage {Tier2EliteMinimumStageRainstorm + 1}";
                monsoonDesc += $"\n>{Tier2EliteName} Elites appear starting on <style=cIsHealth>Stage {Tier2EliteMinimumStageMonsoon + 1}</style>";
            }

            RoR2Application.onLoad += ChangeEliteTierStats;
        }

        private void ChangeEliteTierStats()
        {
            foreach (CombatDirector.EliteTierDef etd in EliteAPI.VanillaEliteTiers)//CombatDirector.eliteTiers)
            {
                //Debug.Log(etd.eliteTypes[0].name);
                if (etd.eliteTypes[0] == RoR2Content.Elites.Poison || etd.eliteTypes[0] == RoR2Content.Elites.Haunted)
                {
                    //Debug.LogError("gwagwag");
                    foreach (EliteDef elite in etd.eliteTypes)
                    {
                        elite.healthBoostCoefficient = Mathf.Pow(baseEliteHealthBoostCoefficient, 2); //18
                        elite.damageBoostCoefficient = 4.5f; //6
                    }

                    etd.isAvailable = (SpawnCard.EliteRules rules) =>
                    (Run.instance.stageClearCount >= Tier2EliteMinimumStageDrizzle && rules == SpawnCard.EliteRules.Default && Run.instance.selectedDifficulty <= DifficultyIndex.Easy)
                    || (Run.instance.stageClearCount >= Tier2EliteMinimumStageRainstorm && rules == SpawnCard.EliteRules.Default && Run.instance.selectedDifficulty == DifficultyIndex.Normal)
                    || (Run.instance.stageClearCount >= Tier2EliteMinimumStageMonsoon && rules == SpawnCard.EliteRules.Default && Run.instance.selectedDifficulty == DifficultyIndex.Hard)
                    || (Run.instance.stageClearCount >= Tier2EliteMinimumStageEclipse && rules == SpawnCard.EliteRules.Default && Run.instance.selectedDifficulty > DifficultyIndex.Hard);
                }
            }
        }

    }
}
