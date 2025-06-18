using R2API;
using RainrotSharedUtils.Components;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static MoreStats.OnHit;
using static R2API.RecalculateStatsAPI;
using static SwanSongExtended.Modules.Language.Styling;

namespace SwanSongExtended.Items
{
    class ShieldAmp : ItemBase<ShieldAmp>
    {
        public static float shieldPercentBase = 0.12f;
        public static float shieldFlatBase = 40f;
        public static float shieldDrainFraction = 0.3f;
        public static float amplifyDamageIncreaseBase = 5f;
        public static float amplifyDamageIncreaseStack = 3f;
        public static float amplifyDamageMultiplierForFullShield = 1f;
        public override string ItemName => "Jellyfish Necklace";

        public override string ItemLangTokenName => "SHIELDAMP";

        public override string ItemPickupDesc => "While shields are full, dealing damage drains shield and creates energizing sparks.";

        public override string ItemFullDescription => $"Gain {HealingColor(shieldFlatBase + " shield")}. " +
            $"While shields are full, damage from your next skill " +
            $"{DamageColor($"drains {Tools.ConvertDecimal(shieldDrainFraction)}")} of your shield, " +
            $"{DamageColor("amplifying")} damage dealt by {DamageColor(ConvertDecimal(amplifyDamageIncreaseBase))} " +
            $"{StackText($"+{ConvertDecimal(amplifyDamageIncreaseStack)}")}. " +
            $"{DamageColor("Amplified")} hits create {UtilityColor("Energizing Sparks")}, " +
            $"temporarily increasing {DamageColor("attack speed")} by {DamageColor(ConvertDecimal(RainrotSharedUtils.Assets.sparkBoosterAspdBonus))}.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier2;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Damage };

        public override GameObject ItemModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlJellyfishNecklace.prefab");

        public override Sprite ItemIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/shieldamp.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.HealthComponent.TakeDamageProcess += ShieldAmpOnTakeDamage;
            //GetHitBehavior += ShieldAmpOnHit;
            GetStatCoefficients += ShieldAmpStats;
        }

        private void ShieldAmpStats(CharacterBody sender, StatHookEventArgs args)
        {
            int stack = GetCount(sender);
            if(stack > 0)
            {
                args.baseShieldAdd += shieldFlatBase;// sender.maxHealth * shieldPercentBase;//(shieldPercentBase + (shieldPercentStack * (itemCount - 1)));
            }
        }

        private void ShieldAmpOnTakeDamage(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, HealthComponent self, DamageInfo damageInfo)
        {
            if (damageInfo.attacker && damageInfo.damageType.IsDamageSourceSkillBased && NetworkServer.active)
            {
                CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                if(attackerBody != null)
                {
                    int stack = GetCount(attackerBody);
                    if (stack > 0)
                    {
                        HealthComponent healthComponent = attackerBody.healthComponent;
                        float maxShield = attackerBody.maxShield;
                        if (healthComponent.shield >= maxShield - 1)
                        {
                            float amplifyBaseDamage = amplifyDamageIncreaseBase + amplifyDamageIncreaseStack * (stack - 1);
                            float amplifyDamageScale = Mathf.Lerp(1, amplifyDamageMultiplierForFullShield, maxShield / healthComponent.fullCombinedHealth);
                            damageInfo.damage += attackerBody.damage * amplifyBaseDamage * amplifyDamageScale;

                            DrainShield(healthComponent, maxShield * shieldDrainFraction);

                            NebulaPickup.CreateBoosterPickup(damageInfo.position, attackerBody.teamComponent.teamIndex, RainrotSharedUtils.Assets.sparkBoosterObject, 2);
                        }
                    }
                }
            }
            orig(self, damageInfo);
        }

        public static void DrainShield(HealthComponent healthComponent, float shieldToDrain)
        {
            if (NetworkServer.active)
            {
                float barrier = healthComponent.barrier;
                healthComponent.Networkbarrier = 0;

                DamageInfo damageInfo = new DamageInfo();
                damageInfo.damage = shieldToDrain;
                damageInfo.attacker = healthComponent.gameObject;
                damageInfo.damageType = new DamageTypeCombo(DamageType.NonLethal, DamageTypeExtended.Generic, DamageSource.NoneSpecified);
                damageInfo.damageColorIndex = DamageColorIndex.Fragile;
                damageInfo.procCoefficient = 0;
                damageInfo.position = healthComponent.transform.position;


                TeamDef teamDef = TeamCatalog.GetTeamDef(healthComponent.body.teamComponent.teamIndex);
                if (teamDef != null)
                {
                    damageInfo.damage /= teamDef.friendlyFireScaling;
                }
                healthComponent.TakeDamage(damageInfo);

                healthComponent.Networkbarrier = barrier;
            }
        }

        private void ShieldAmpOnHit(CharacterBody attackerBody, DamageInfo damageInfo, CharacterBody victimBody)
        {
        }
    }
}
