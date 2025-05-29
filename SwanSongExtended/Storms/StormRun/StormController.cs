using EntityStates;
using RainrotSharedUtils.Shelters;
using RoR2;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static SwanSongExtended.Storms.StormRunBehavior;
using static SwanSongExtended.Storms.StormsCore;

namespace SwanSongExtended.Storms
{
    /// <summary>
     /// Handles storm event timing and hazards during storms
     /// </summary>
    [RequireComponent(typeof(EntityStateMachine), typeof(CombatDirector))]
    public class StormController : MonoBehaviour
    {
        public enum StormState
        {
            Idle,
            Approaching,
            ApproachWarning,
            Active
        }
        public StormState stormState
        {
            get
            {
                if (this.currentState == null)
                    return StormState.Idle;
                return currentState.stormState;
            }
        }
        public CombatDirector combatDirector;
        public EntityStateMachine mainStateMachine;
        private StormController.BaseStormState currentState
        {
            get
            {
                return this.mainStateMachine.state as StormController.BaseStormState;
            }
        }
        protected List<HoldoutZoneController> holdoutZones => StormRunBehavior.holdoutZones;
        internal float stormDelayTime = 0;
        internal float stormWarningTime = 0;
        bool shelterObjectiveActive = false;


        public void Awake()
        {
            combatDirector = GetComponent<CombatDirector>();
            combatDirector.enabled = false;
            mainStateMachine = GetComponent<EntityStateMachine>();
        }

        public void SetShelterObjective(bool enable)
        {
            if (enable)
            {
                if (!shelterObjectiveActive)
                {
                    ObjectivePanelController.collectObjectiveSources += this.OnCollectObjectiveSources;
                }
            }
            else
            {
                if (shelterObjectiveActive)
                {
                    ObjectivePanelController.collectObjectiveSources -= this.OnCollectObjectiveSources;
                }
            }
        }
        private void OnCollectObjectiveSources(CharacterMaster master, List<ObjectivePanelController.ObjectiveSourceDescriptor> objectiveSourcesList)
        {
            objectiveSourcesList.Add(new ObjectivePanelController.ObjectiveSourceDescriptor
            {
                master = master,
                objectiveType = typeof(StormObjectiveTracker),
                source = base.gameObject
            });
        }

        public void BeginStormApproach(float stormDelayTime, float stormWarningTime)
        {
            this.stormDelayTime = stormDelayTime * 60;
            this.stormWarningTime = stormWarningTime * 60;
            Log.Debug("Starting storm approach");
            mainStateMachine.SetNextState(new StormApproach());
        }
        public void ForceBeginStorm()
        {
            if (this.stormState < StormState.ApproachWarning)
            {
                mainStateMachine.SetNextState(new StormWarning());
            }
        }
        internal abstract class BaseStormState : BaseState
        {
            public abstract StormState stormState { get; }
            private protected StormType stormType => StormRunBehavior.instance.stormType;
            private protected StormController stormController { get; private set; }
            public override void OnEnter()
            {
                base.OnEnter();
                this.stormController = base.GetComponent<StormController>();
            }

            public void EnableDirector()
            {
                //stormController.combatDirector.enabled = true;
            }
        }
        internal class StormActive : BaseStormState
        {
            public override StormState stormState => StormState.Active;

            //all the projectile/prefab stuff

            private List<MeteorStormController.Meteor> meteorsToDetonate;
            private List<MeteorStormController.MeteorWave> meteorWaves;
            private float waveTimer;

            public override void OnEnter()
            {
                base.OnEnter();

                if (!Run.instance)
                {
                    outer.SetNextState(new StormController.IdleState());
                    return;
                }
                this.meteorsToDetonate = new List<MeteorStormController.Meteor>();
                this.meteorWaves = new List<MeteorStormController.MeteorWave>();

                //On.RoR2.MeteorStormController.MeteorWave.GetNextMeteor += MeteorWave_GetNextMeteor;
                EnableDirector();
            }
            public override void OnExit()
            {
                base.OnExit();
                stormController.SetShelterObjective(false);
                //On.RoR2.MeteorStormController.MeteorWave.GetNextMeteor -= MeteorWave_GetNextMeteor;
            }
            public override void FixedUpdate()
            {
                base.FixedUpdate();
                bool teleporterActive = TeleporterInteraction.instance && TeleporterInteraction.instance.isCharging;
                stormController.SetShelterObjective(!teleporterActive);

                //thisa is just for meteor stuff; we can make it work for the other storsm when they start existing lol.
                this.waveTimer -= Time.fixedDeltaTime;
                if (this.waveTimer <= 0f)
                {
                    this.waveTimer = UnityEngine.Random.Range(waveMinInterval, waveMaxInterval);
                    MeteorStormController.MeteorWave item =
                        new MeteorStormController.MeteorWave(
                            CharacterBody.readOnlyInstancesList
                                .Where(body => /*!ShelterUtilsModule.IsBodySheltered(body) &&*/
                                (body.teamComponent.teamIndex == TeamIndex.Player && !body.isFlying) 
                                || IsCharacterStormElite(body) || Util.CheckRoll(meteorTargetEnemyChance))
                                .ToArray<CharacterBody>(),
                            TeleporterInteraction.instance ? TeleporterInteraction.instance.transform.position : base.transform.position);
                    item.hitChance = 1 - waveMissChance;
                    this.meteorWaves.Add(item);
                    this.meteorWaves.Add(item);
                }

                for (int i = this.meteorWaves.Count - 1; i >= 0; i--)
                {
                    MeteorStormController.MeteorWave meteorWave = this.meteorWaves[i];
                    meteorWave.timer -= Time.fixedDeltaTime;
                    if (meteorWave.timer <= 0f)
                    {
                        meteorWave.timer = UnityEngine.Random.Range(0.05f, 1f);
                        MeteorStormController.Meteor nextMeteor = meteorWave.GetNextMeteor(); // getnextmeteor handles some stuff here, we can look into canibalizing it for more adaptable stuff
                        bool meteorViable = GetMeteorViable(nextMeteor);

                        if (!meteorViable)
                        {
                            this.meteorWaves.RemoveAt(i);
                        }
                        else
                        {
                            this.meteorsToDetonate.Add(nextMeteor);
                            EffectManager.SpawnEffect(meteorWarningEffectPrefab, new EffectData
                            {
                                origin = nextMeteor.impactPosition,
                                scale = meteorBlastRadius
                            }, true);
                        }
                    }
                }

                float num = float.PositiveInfinity;
                if (Run.instance)
                    num = Run.instance.time - meteorImpactDelay;
                float num2 = num - meteorTravelEffectDuration;
                for (int j = this.meteorsToDetonate.Count - 1; j >= 0; j--)
                {
                    MeteorStormController.Meteor meteor = this.meteorsToDetonate[j];
                    if (meteor.startTime < num)
                    {
                        this.meteorsToDetonate.RemoveAt(j);
                        this.DetonateMeteor(meteor);
                    }
                }
            }

            private bool GetMeteorViable(MeteorStormController.Meteor nextMeteor)
            {
                if (nextMeteor == null)
                    return false;
                if (!nextMeteor.valid)
                    return false;

                Vector3 impactPosition = (Vector3)nextMeteor.GetType().GetField("impactPosition", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public
                    ).GetValue(nextMeteor);
                return !ShelterUtilsModule.IsPositionSheltered(impactPosition, meteorBlastRadius);
            }

            private void DetonateMeteor(MeteorStormController.Meteor meteor)
            {
                EffectData effectData = new EffectData
                {
                    origin = meteor.impactPosition
                };
                EffectManager.SpawnEffect(meteorImpactEffectPrefab, effectData, true);
                new BlastAttack
                {
                    inflictor = base.gameObject,
                    baseDamage = meteorBlastDamageCoefficient * (1 + meteorBlastDamageScalarPerLevel * Run.instance.ambientLevel),//multiplies by ambient level. if this is unsatisfactory change later
                    baseForce = meteorBlastForce,
                    attackerFiltering = AttackerFiltering.Default,
                    crit = false,
                    falloffModel = meteorFalloffModel,
                    attacker = this.gameObject,//this.teleporter ,
                    bonusForce = Vector3.zero,
                    damageColorIndex = DamageColorIndex.Fragile,
                    position = meteor.impactPosition,
                    procChainMask = default(ProcChainMask),
                    procCoefficient = 0f,
                    teamIndex = TeamIndex.Monster,// | TeamIndex.Void | TeamIndex.Neutral,
                    radius = meteorBlastRadius
                }.Fire();
            }

            //teleporter safe zone
            private object MeteorWave_GetNextMeteor(On.RoR2.MeteorStormController.MeteorWave.orig_GetNextMeteor orig, object self)
            {
                object meteor = orig.Invoke(self);
                if (stormController.holdoutZones.Count == 0)
                    return meteor;

                try
                {
                    foreach (HoldoutZoneController holdoutZone in stormController.holdoutZones)
                    {
                        if (holdoutZone == null)
                        {
                            stormController.holdoutZones.Remove(holdoutZone);
                            continue;
                        }
                        if (!holdoutZone.isActiveAndEnabled || holdoutZone.charge <= 0)
                        {
                            continue;
                        }

                        //i have no goddamn clue what this does lmao
                        //this uses reflection to find the impact position of the meteor that was spawned -borbo
                        Vector3 impactPosition = (Vector3)meteor.GetType()
                            .GetField("impactPosition", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).GetValue(meteor);

                        if (IsInRange(impactPosition, holdoutZone.transform.position, holdoutZone.currentRadius + meteorBlastRadius))
                        {
                            meteor = null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                }

                return meteor;

                bool IsInRange(Vector3 a, Vector3 b, float dist)
                {
                    return (a - b).sqrMagnitude <= dist * dist;
                }
            }
            public override InterruptPriority GetMinimumInterruptPriority()
            {
                return InterruptPriority.Death;
            }
        }
        internal class StormWarning : BaseStormState
        {
            private Dictionary<HUD, GameObject> hudPanels;
            public override StormState stormState => StormState.ApproachWarning;
            public override void OnEnter()
            {
                hudPanels = new Dictionary<HUD, GameObject>();
                base.OnEnter();

                foreach (HUD hud in HUD.readOnlyInstanceList)
                {
                    SetHudCountdownEnabled(hud, hud.targetBodyObject != null);
                }
                SetCountdownTime(Mathf.Max(0, stormController.stormWarningTime - base.fixedAge));

                string warningMessage = "";
                switch (stormType)
                {
                    case StormType.MeteorDefault:
                        warningMessage = "<style=cIsUtility>A meteor storm is approaching...</style>";
                        break;
                    case StormType.Lightning:
                        warningMessage = "A storm approaches...";
                        break;
                    case StormType.Fire:
                        warningMessage = "A meteor storm is approaching...";
                        break;
                    case StormType.Cold:
                        warningMessage = "The air around you begins to freeze...";
                        break;
                }

                stormController.SetShelterObjective(true);
                RoR2.Chat.AddMessage(warningMessage);
            }
            public override void OnExit()
            {
                base.OnExit();
                foreach (HUD hud in HUD.readOnlyInstanceList)
                {
                    SetHudCountdownEnabled(hud, false);
                }
            }
            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (base.fixedAge >= stormController.stormWarningTime && NetworkServer.active)
                {
                    string warningMessage = "";
                    switch (stormType)
                    {
                        case StormType.MeteorDefault:
                            warningMessage = "<style=cIsUtility>A shower of meteors begins to fall...</style>";
                            break;
                        case StormType.Lightning:
                            warningMessage = "A meteor storm is approaching...";
                            break;
                        case StormType.Fire:
                            warningMessage = "A meteor storm is approaching...";
                            break;
                        case StormType.Cold:
                            warningMessage = "A meteor storm is approaching...";
                            break;
                    }

                    RoR2.Chat.AddMessage(warningMessage);

                    outer.SetNextState(new StormActive());
                }

                bool teleporterActive = TeleporterInteraction.instance && TeleporterInteraction.instance.isCharging;
                stormController.SetShelterObjective(!teleporterActive);

                if (stormType == StormType.None || !Run.instance)
                {
                    if (this.hudPanels.Count > 0)
                    {
                        foreach (HUD hud in HUD.readOnlyInstanceList)
                        {
                            SetHudCountdownEnabled(hud, false);
                        }
                    }
                    return;
                }
                foreach (HUD hud in HUD.readOnlyInstanceList)
                {
                    SetHudCountdownEnabled(hud, hud.targetBodyObject != null);
                }
                SetCountdownTime(Mathf.Max(0, stormController.stormWarningTime - base.fixedAge));
            }


            private void SetHudCountdownEnabled(HUD hud, bool shouldEnableCountdownPanel)
            {
                shouldEnableCountdownPanel &= outer.enabled;
                if (hudPanels.TryGetValue(hud, out GameObject gameObject) != shouldEnableCountdownPanel)
                {
                    if (shouldEnableCountdownPanel && stormType != StormType.None)
                    {
                        RectTransform rectTransform = hud.GetComponent<ChildLocator>().FindChild("TopCenterCluster") as RectTransform;
                        if (rectTransform)
                        {
                            GameObject value = UnityEngine.Object.Instantiate<GameObject>(LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/HudModules/HudCountdownPanel"), rectTransform);
                            LanguageTextMeshController ltmc = value.GetComponentInChildren<LanguageTextMeshController>();
                            ltmc._token = $"OBJECTIVE_{stormType.ToString().ToUpper()}_2R4R";
                            ltmc.token = $"OBJECTIVE_{stormType.ToString().ToUpper()}_2R4R";
                            this.hudPanels[hud] = value;
                            return;
                        }
                    }
                    else
                    {
                        UnityEngine.Object.Destroy(gameObject);
                        this.hudPanels.Remove(hud);
                    }
                }
            }
            private void SetCountdownTime(double secondsRemaining)
            {
                foreach (KeyValuePair<HUD, GameObject> keyValuePair in this.hudPanels)
                {
                    keyValuePair.Value.GetComponent<TimerText>().seconds = secondsRemaining;
                }
                //AkSoundEngine.SetRTPCValue("EscapeTimer", Util.Remap((float)secondsRemaining, 0f, this.countdownDuration, 0f, 100f));
            }

            public override void Update()
            {
                base.Update();
            }
            public override InterruptPriority GetMinimumInterruptPriority()
            {
                return InterruptPriority.Death;
            }
        }
        internal class StormApproach : BaseStormState
        {
            public override StormState stormState => StormState.Approaching;
            public override void OnEnter()
            {
                base.OnEnter();
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (base.fixedAge >= stormController.stormDelayTime && NetworkServer.active)
                {
                    if (stormType > StormType.None)
                    {
                        outer.SetNextState(new StormWarning());
                    }
                }
            }
            public override InterruptPriority GetMinimumInterruptPriority()
            {
                return InterruptPriority.Death;
            }
        }
        internal class IdleState : BaseStormState
        {
            public override StormState stormState => StormState.Idle;
        }
    }
}
