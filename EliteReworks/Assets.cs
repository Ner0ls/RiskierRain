using EntityStates.ClayBoss;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Linq;
using static MoreStats.StatHooks;

namespace EliteReworks
{
    public static class Assets
    {
        //#region AssetBundles
        //public static string GetAssetBundlePath(string bundleName)
        //{
        //    return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(RiskierRainPlugin.PInfo.Location), bundleName);
        //}
        //
        //private static AssetBundle _mainAssetBundle;
        //public static AssetBundle mainAssetBundle
        //{
        //    get
        //    {
        //        if (_mainAssetBundle == null)
        //            _mainAssetBundle = AssetBundle.LoadFromFile(GetAssetBundlePath("itmightbebad"));
        //        return _mainAssetBundle;
        //    }
        //    set
        //    {
        //        _mainAssetBundle = value;
        //    }
        //}
        //private static AssetBundle _orangeAssetBundle;
        //public static AssetBundle orangeAssetBundle
        //{
        //    get
        //    {
        //        if (_orangeAssetBundle == null)
        //            _orangeAssetBundle = AssetBundle.LoadFromFile(GetAssetBundlePath("orangecontent"));
        //        return _orangeAssetBundle;
        //    }
        //    set
        //    {
        //        _orangeAssetBundle = value;
        //    }
        //}
        //public static string dropPrefabsPath = "Assets/Models/DropPrefabs";
        //public static string iconsPath = "Assets/Textures/Icons/";
        //public static string eliteMaterialsPath = "Assets/Textures/Materials/Elite/";
        //#endregion
        //public static bool RegisterEntityState(Type entityState)
        //{
        //    //Check if the entity state has already been registered, is abstract, or is not a subclass of the base EntityState
        //    if (entityStates.Contains(entityState) || !entityState.IsSubclassOf(typeof(EntityStates.EntityState)) || entityState.IsAbstract)
        //    {
        //        //LogCore.LogE(entityState.AssemblyQualifiedName + " is either abstract, not a subclass of an entity state, or has already been registered.");
        //        //LogCore.LogI("Is Abstract: " + entityState.IsAbstract + " Is not Subclass: " + !entityState.IsSubclassOf(typeof(EntityState)) + " Is already added: " + EntityStateDefinitions.Contains(entityState));
        //        return false;
        //    }
        //    //If not, add it to our EntityStateDefinitions
        //    entityStates.Add(entityState);
        //    return true;
        //}
        //public static EffectDef CreateEffect(GameObject effect)
        //{
        //
        //    if (effect == null)
        //    {
        //        Debug.LogError("Effect prefab was null");
        //        return null;
        //    }
        //
        //    var effectComp = effect.GetComponent<EffectComponent>();
        //    if (effectComp == null)
        //    {
        //        Debug.LogErrorFormat("Effect prefab: \"{0}\" does not have an EffectComponent.", effect.name);
        //        return null;
        //    }
        //
        //    var vfxAttrib = effect.GetComponent<VFXAttributes>();
        //    if (vfxAttrib == null)
        //    {
        //        Debug.LogErrorFormat("Effect prefab: \"{0}\" does not have a VFXAttributes component.", effect.name);
        //        return null;
        //    }
        //
        //    var def = new EffectDef
        //    {
        //        prefab = effect,
        //        prefabEffectComponent = effectComp,
        //        prefabVfxAttributes = vfxAttrib,
        //        prefabName = effect.name,
        //        spawnSoundEventName = effectComp.soundName
        //    };
        //
        //    effectDefs.Add(def);
        //    return def;
        //}

        public static void Init()
        {
            CreateVoidtouchedSingularity();
        }

        public static GameObject voidtouchedSingularityDelay;
        public static GameObject voidtouchedSingularity;
        private static void CreateVoidtouchedSingularity()
        {
            float singularityRadius = 8; //15
            GameObject singularity = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/ElementalRingVoid/ElementalRingVoidBlackHole.prefab").WaitForCompletion();
            voidtouchedSingularity = singularity.InstantiateClone("VoidtouchedSingularity", true);

            ProjectileFuse singularityPf = voidtouchedSingularity.GetComponent<ProjectileFuse>();
            if (singularityPf)
            {
                singularityPf.fuse = 3;
            }
            RadialForce singularityRF = voidtouchedSingularity.GetComponent<RadialForce>();
            if (singularityRF)
            {
                singularityRF.radius = singularityRadius;
                voidtouchedSingularity.transform.localScale *= (singularityRadius / 15);
            }
            R2API.ContentAddition.AddProjectile(voidtouchedSingularity);

            GameObject willowispDelay = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ExplodeOnDeath/WilloWispDelay.prefab").WaitForCompletion();
            voidtouchedSingularityDelay = willowispDelay.InstantiateClone("VoidtouchedDelayBlast", true);

            DelayBlast singularityDelayDB = voidtouchedSingularityDelay.GetComponent<DelayBlast>();
            if (singularityDelayDB)
            {
                singularityDelayDB.explosionEffect = voidtouchedSingularity;
            }

            R2API.ContentAddition.AddNetworkedObject(voidtouchedSingularityDelay);
        }

        #region shaders lol

        public static void SwapShadersFromMaterialsInBundle(AssetBundle bundle)
        {
            if (bundle.isStreamedSceneAssetBundle)
            {
                Debug.LogWarning($"Cannot swap material shaders from a streamed scene assetbundle.");
                return;
            }

            Material[] assetBundleMaterials = bundle.LoadAllAssets<Material>().Where(mat => mat.shader.name.StartsWith("Stubbed")).ToArray();

            for (int i = 0; i < assetBundleMaterials.Length; i++)
            {
                var material = assetBundleMaterials[i];
                if (!material.shader.name.StartsWith("Stubbed"))
                {
                    Debug.LogWarning($"The material {material} has a shader which's name doesnt start with \"Stubbed\" ({material.shader.name}), this is not allowed for stubbed shaders for MSU. not swapping shader.");
                    continue;
                }
                try
                {
                    SwapShader(material);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to swap shader of material {material}: {ex}");
                }
            }
        }
        private static void SwapShader(Material material)
        {
            var shaderName = material.shader.name.Substring("Stubbed".Length);
            var adressablePath = $"{shaderName}.shader";
            Shader shader = Addressables.LoadAssetAsync<Shader>(adressablePath).WaitForCompletion();
            material.shader = shader;            
            MaterialsWithSwappedShaders.Add(material);
        }
        public static List<Material> MaterialsWithSwappedShaders { get; } = new List<Material>();
        #endregion
    }
}
