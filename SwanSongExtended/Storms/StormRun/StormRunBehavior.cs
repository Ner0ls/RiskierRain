using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static SwanSongExtended.Storms.StormsCore;

namespace SwanSongExtended.Storms
{
    /// <summary>
    /// Creates a StormController for each stage with appropriate properties
    /// </summary>
    public class StormRunBehavior : MonoBehaviour
    {
        public static List<HoldoutZoneController> holdoutZones = new List<HoldoutZoneController>();

        public static StormRunBehavior instance;
        public StormController stormControllerInstance;
        public StormType stormType { get; private set; } = StormType.None;


        private static StormType GetStormType()
        {
            SceneDef currentScene = SceneCatalog.GetSceneDefForCurrentScene();
            StormType st = StormType.None;
            if (currentScene.sceneType == SceneType.Stage && !currentScene.isFinalStage)
            {
                switch (currentScene.baseSceneName)
                {
                    default:
                        st = StormType.MeteorDefault;
                        break;
                }
            }

            return st;
        }
        public bool hasBegunStorm
        {
            get
            {
                if (stormControllerInstance == null)
                    return false;
                if (stormControllerInstance.stormState >= StormController.StormState.Active)
                    return true;
                return false;
            }
        }

        public void Start()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }
            instance = this;

            RoR2.Stage.onServerStageBegin += OnServerStageBegin;
        }

        private void OnServerStageBegin(Stage obj)
        {

            stormType = GetStormType();
            if (stormType == StormType.None)
                return;

            GameObject stormControllerObject = Instantiate(StormsCore.StormsControllerPrefab);
            stormControllerInstance = stormControllerObject.GetComponent<StormController>();

            float a = drizzleStormDelayMinutes;
            float b = drizzleStormWarningMinutes;
            if (Run.instance.selectedDifficulty >= DifficultyIndex.Hard)
            {
                a = monsoonStormDelayMinutes;
                b = monsoonStormWarningMinutes;
            }
            else if (Run.instance.selectedDifficulty == DifficultyIndex.Normal)
            {
                a = rainstormStormDelayMinutes;
                b = rainstormStormWarningMinutes;
            }
            stormControllerInstance.BeginStormApproach(a + Run.instance.stageRng.RangeInt(0, 1), b);
        }

        #region hooks
        public void OnDestroy()
        {
            if(instance == this)
            {
                RoR2.Stage.onServerStageBegin -= OnServerStageBegin;
            }
        }
        #endregion
    }
}
