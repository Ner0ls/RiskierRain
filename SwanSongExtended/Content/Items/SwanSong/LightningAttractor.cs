using R2API;
using RoR2;
using SwanSongExtended.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static SwanSongExtended.Modules.Language.Styling;
using static MoreStats.OnHit;
using UnityEngine.Networking;
using RoR2.Orbs;

namespace SwanSongExtended.Items
{
    class LightningAttractor : ItemBase<LightningAttractor>
    {
        public static BuffDef forkReadyBuff;
        public static BuffDef forkRechargeBuff;
        public static BuffDef forkRepeatHitBuff;
        public static float forkRecharge = 10;
        public static float forkDuration = 5;
        public static float forkAttackRequirement = 4;
        public static float forkTotalDamageBase = 1.5f;
        public static float forkTotalDamageStack = 1f;
        public static float forkStrikeRange = 25;
        public override string ItemName => "Copper Fork";

        public override string ItemLangTokenName => "LIGHTNINGATTRACTOR";

        public override string ItemPickupDesc => "Attract lightning on repeated hits.";

        public override string ItemFullDescription => 
            $"Damage from skills or equipment also sticks the enemy with " +
            $"a copper fork for {forkDuration} seconds. " +
            $"Repeatedly attacking a forked enemy resets the fork's duration " +
            $"and attracts lightning, Stunning a nearby enemy " +
            $"for {ConvertDecimal(forkTotalDamageStack)} TOTAL damage " +
            $"{StackText($"+{ConvertDecimal(forkTotalDamageStack)}")}. " +
            $"1 max. Recharges {forkRecharge} after the fork expires.";

        public override string ItemLore => "The System's Finest Copperware!" +
            "\n\nNote: Please don't try eating in the rain. You don't want this to corrode.";

        public override ItemTier Tier => ItemTier.Tier2;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Damage, ItemTag.AIBlacklist };

        public override GameObject ItemModel => LoadDropPrefab();

        public override Sprite ItemIcon => Addressables.LoadAssetAsync<Sprite>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_Common_MiscIcons.texAttackIcon_png).WaitForCompletion();

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return new ItemDisplayRuleDict();
        }
        public override void Init()
        {
            base.Init();
            forkReadyBuff = Content.CreateAndAddBuff("bdForkReady",
                Addressables.LoadAssetAsync<Sprite>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_Common_MiscIcons.texAttackIcon_png).WaitForCompletion(),
                Color.yellow, false, false);
            forkRechargeBuff = Content.CreateAndAddBuff("bdForkRecharge",
                Addressables.LoadAssetAsync<Sprite>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_Common_MiscIcons.texAttackIcon_png).WaitForCompletion(),
                Color.gray, false, false);
            forkRepeatHitBuff = Content.CreateAndAddBuff("bdForkStack",
                Addressables.LoadAssetAsync<Sprite>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_Common_MiscIcons.texAttackIcon_png).WaitForCompletion(),
                new Color32(255, 125, 0,255), true, true);
            forkRepeatHitBuff.flags |= BuffDef.Flags.ExcludeFromNoxiousThorns;
        }

        public override void Hooks()
        {
            GetHitBehavior += ForkOnHit;
            On.RoR2.CharacterBody.OnInventoryChanged += AddForkItemBehavior;
        }

        private void AddForkItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                self.AddItemBehavior<LightningAttractorBehavior>(GetCount(self));
            }
        }

        private void ForkOnHit(CharacterBody attackerBody, DamageInfo damageInfo, CharacterBody victimBody)
        {
            int itemCount = GetCount(attackerBody);
            if (itemCount <= 0 || !NetworkServer.active)
                return;

            if (!victimBody.healthComponent.alive)
                return;

            if (!damageInfo.damageType.IsDamageSourceSkillBased && damageInfo.damageType.damageSource != DamageSource.Equipment)
                return;

            int forkHits = victimBody.GetBuffCount(forkRepeatHitBuff);
            bool forkReady = attackerBody.HasBuff(forkReadyBuff);
            //if the attacker can fork or if the victim is already forked
            if(forkReady || forkHits > 0)
            {
                //if the attacker can fork, take the fork
                if (forkReady)
                {
                    attackerBody.RemoveBuff(forkReadyBuff);
                }
                //refresh fork cooldown always
                attackerBody.AddTimedBuff(forkRechargeBuff, forkRecharge);

                //if the next hit goes over the attack requirement, do lightning
                //otherwise, extend all fork hit counts
                //i do it this way so the fork attack count always stays at or above 1
                if(forkHits >= forkAttackRequirement)
                {
                    victimBody.ClearTimedBuffs(forkRepeatHitBuff);
                    //do lightning
                    DoForkLightningStrike(attackerBody, damageInfo, victimBody, itemCount);
                }
                else
                { 
                    victimBody.ExtendTimedBuffIfPresent(forkRepeatHitBuff, forkDuration, forkDuration);
                }

                //add a fork hit
                victimBody.AddTimedBuff(forkRepeatHitBuff, forkDuration);
            }
        }

        private static void DoForkLightningStrike(CharacterBody attackerBody, DamageInfo damageInfo, CharacterBody victimBody, int itemCount)
        {
            float range = forkStrikeRange;// overloadingSmiteRangeBase + victimBody.radius * overloadingSmiteRangePerRadius;
            float baseDamage = damageInfo.damage;
            float smiteDamageCoefficient = forkTotalDamageBase + forkTotalDamageStack * (itemCount - 1);
            ProcChainMask procChainMask6 = damageInfo.procChainMask;
            //procChainMask6.AddProc(ProcType.LightningStrikeOnHit);

            SphereSearch sphereSearch = new SphereSearch
            {
                mask = LayerIndex.entityPrecise.mask,
                origin = victimBody.transform.position,
                queryTriggerInteraction = QueryTriggerInteraction.Collide,
                radius = range
            };

            TeamMask teamMask = TeamMask.GetEnemyTeams(attackerBody.teamComponent.teamIndex);
            List<HurtBox> hurtBoxesList = new List<HurtBox>();

            sphereSearch.RefreshCandidates().FilterCandidatesByHurtBoxTeam(teamMask).FilterCandidatesByDistinctHurtBoxEntities().GetHurtBoxes(hurtBoxesList);

            int i = UnityEngine.Random.Range(0, hurtBoxesList.Count);
            HurtBox targetHurtBox = hurtBoxesList[i];
            SetStateOnHurt component = targetHurtBox.healthComponent.GetComponent<SetStateOnHurt>();
            component.SetStun(1);

            OrbManager.instance.AddOrb(new SimpleLightningStrikeOrb
            {
                attacker = attackerBody.gameObject,
                damageColorIndex = DamageColorIndex.Default,
                damageValue = baseDamage * smiteDamageCoefficient,
                isCrit = damageInfo.crit,
                procChainMask = procChainMask6,
                procCoefficient = 1f,
                target = targetHurtBox,
                damageType = DamageType.Stun1s
            });
        }
    }
    public class LightningAttractorBehavior : CharacterBody.ItemBehavior
    {

        private void FixedUpdate()
        {
            if (!NetworkServer.active)
                return;
            int buffCount = body.GetBuffCount(LightningAttractor.forkReadyBuff);
            if (!body.HasBuff(LightningAttractor.forkRechargeBuff) && !body.HasBuff(LightningAttractor.forkReadyBuff))
            {
                body.AddBuff(LightningAttractor.forkReadyBuff);
            }
        }

        private void OnDisable()
        {
            this.body.RemoveBuff(LightningAttractor.forkReadyBuff);
            this.body.ClearTimedBuffs(LightningAttractor.forkRechargeBuff);
        }
    }
}
