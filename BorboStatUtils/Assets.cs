using BepInEx;
using R2API;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RainrotSharedUtils
{
    public static class Assets
    {
        #region interfacing
        public static EffectDef RegisterEffect(GameObject effect)
        {
            if (effect == null)
            {
                Debug.LogError("Effect prefab was null");
                return null;
            }

            var effectComp = effect.GetComponent<EffectComponent>();
            if (effectComp == null)
            {
                Debug.LogErrorFormat("Effect prefab: \"{0}\" does not have an EffectComponent.", effect.name);
                return null;
            }

            var vfxAttrib = effect.GetComponent<VFXAttributes>();
            if (vfxAttrib == null)
            {
                Debug.LogErrorFormat("Effect prefab: \"{0}\" does not have a VFXAttributes component.", effect.name);
                return null;
            }
            R2API.ContentAddition.AddEffect(effect);

            var def = new EffectDef
            {
                prefab = effect,
                prefabEffectComponent = effectComp,
                prefabVfxAttributes = vfxAttrib,
                prefabName = effect.name,
                spawnSoundEventName = effectComp.soundName
            };
            return def;
        }
        private static Texture2D CreateNewRampTex(Gradient grad)
        {
            var tex = new Texture2D(256, 8, TextureFormat.RGBA32, false);

            Color tempC;
            var tempCs = new Color[8];

            for (Int32 i = 0; i < 256; i++)
            {
                tempC = grad.Evaluate(i / 255f);
                for (Int32 j = 0; j < 8; j++)
                {
                    tempCs[j] = tempC;
                }

                tex.SetPixels(i, 0, 1, 8, tempCs);
            }
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            return tex;
        }
        #endregion
        public static void Init()
        {
            CreateFrostNovaAssets();
        }

        #region chill rework
        internal static GameObject iceDelayBlastPrefab;

        public static GameObject iceNovaEffectStrong;
        public static GameObject iceNovaEffectWeak;
        public static GameObject iceNovaEffectLowPriority;

        public static Texture2D iceNovaRamp;
        public static Texture2D iceNovaRampPersistent;
        private static void CreateFrostNovaAssets()
        {
            iceNovaRamp = GetIceRemap(1.1f);
            iceNovaRampPersistent = GetIceRemap(0.4f, 0.1f);

            iceNovaEffectStrong = CreateSingleIceNova(iceNovaRamp, "Strong", 1.2f);
            iceNovaEffectWeak = CreateSingleIceNova(iceNovaRamp, "Weak", 0.85f);
            iceNovaEffectLowPriority = CreateSingleIceNova(iceNovaRamp, "LowPriority", 0.3f);

            iceDelayBlastPrefab = CreateIceDelayBlastPrefab();

            Texture2D GetIceRemap(float alphaMod = 1, float alphaAdd = 0.0f)
            {
                Gradient iceGrad = new Gradient
                {
                    mode = GradientMode.Blend,
                    alphaKeys = new GradientAlphaKey[8]
                    {
                        new GradientAlphaKey( 0f * alphaMod + alphaAdd, 0f ),
                        new GradientAlphaKey( 0f * alphaMod + alphaAdd, 0.14f ),
                        new GradientAlphaKey( 0.22f * alphaMod + alphaAdd, 0.46f ),
                        new GradientAlphaKey( 0.22f * alphaMod + alphaAdd, 0.61f),
                        new GradientAlphaKey( 0.72f * alphaMod + alphaAdd, 0.63f ),
                        new GradientAlphaKey( 0.72f * alphaMod + alphaAdd, 0.8f ),
                        new GradientAlphaKey( 0.87f * alphaMod + alphaAdd, 0.81f ),
                        new GradientAlphaKey( 0.87f * alphaMod + alphaAdd, 1f )
                    },
                    colorKeys = new GradientColorKey[8]
                    {
                        new GradientColorKey( new Color( 0f + alphaAdd, 0 + alphaAdd, 0f + alphaAdd ), 0f ),
                        new GradientColorKey( new Color( 0f + alphaAdd, 0f + alphaAdd, 0f + alphaAdd ), 0.14f ),
                        new GradientColorKey( new Color( 0.179f + alphaAdd , 0.278f + alphaAdd, 0.250f + alphaAdd ), 0.46f ),
                        new GradientColorKey( new Color( 0.179f + alphaAdd, 0.278f + alphaAdd, 0.250f + alphaAdd ), 0.61f ),
                        new GradientColorKey( new Color( 0.5f + alphaAdd, 0.8f + alphaAdd, 0.75f + alphaAdd ), 0.63f ),
                        new GradientColorKey( new Color( 0.5f + alphaAdd, 0.8f + alphaAdd, 0.75f + alphaAdd ), 0.8f ),
                        new GradientColorKey( new Color( 0.6f + alphaAdd, 0.9f + alphaAdd, 0.85f + alphaAdd ), 0.81f ),
                        new GradientColorKey( new Color( 0.6f + alphaAdd, 0.9f + alphaAdd, 0.85f + alphaAdd ), 1f )
                    }
                };
                return CreateNewRampTex(iceGrad);
            }
        }

        private static GameObject CreateSingleIceNova(Texture2D remapTex, string s, float alphaMod)
        {
            GameObject obj = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/AffixWhiteExplosion").InstantiateClone("IceExplosion" + s, false);
            ParticleSystemRenderer sphere = obj.transform.Find("Nova Sphere").GetComponent<ParticleSystemRenderer>();

            Material mat = UnityEngine.Object.Instantiate<Material>(sphere.material);

            mat.SetTexture("_RemapTex", remapTex);
            Color c = mat.GetColor("_TintColor");
            c.a *= alphaMod;
            mat.SetColor("_TintColor", c);

            sphere.material = mat;
            RegisterEffect(obj);

            return obj;
        }

        private static GameObject CreateIceDelayBlastPrefab()
        {
            GameObject blast = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/GenericDelayBlast").InstantiateClone("IceDelayBlast", false);
            DelayBlast component = blast.GetComponent<DelayBlast>();
            component.crit = false;
            component.procCoefficient = 1.0f;
            component.maxTimer = 0.2f;
            component.falloffModel = BlastAttack.FalloffModel.None;
            component.explosionEffect = iceNovaEffectWeak;
            component.delayEffect = CreateIceDelayEffect();
            component.damageType = DamageType.Freeze2s;
            component.baseForce = 250f;

            ProjectileController pc = blast.AddComponent<ProjectileController>();

            //AltArtiPassive.iceBlast = blast;
            //projectilePrefabs.Add(blast);

            R2API.ContentAddition.AddProjectile(blast);

            return blast;
        }
        //called by CreateIceDelayBlastPrefab
        private static GameObject CreateIceDelayEffect()
        {
            GameObject obj = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/AffixWhiteDelayEffect").InstantiateClone("iceDelay", false);
            obj.GetComponent<DestroyOnTimer>().duration = 0.2f;

            ParticleSystemRenderer sphere = obj.transform.Find("Nova Sphere").GetComponent<ParticleSystemRenderer>();
            Material mat = UnityEngine.Object.Instantiate<Material>(sphere.material);
            mat.SetTexture("_RemapTex", iceNovaRamp);
            sphere.material = mat;

            RegisterEffect(obj);

            return obj;
        }
        #endregion

        #region shock rework

        #endregion
    }
}
