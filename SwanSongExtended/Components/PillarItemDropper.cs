using RoR2;
using SwanSongExtended.Items;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using static SwanSongExtended.SwanSongPlugin;

namespace SwanSongExtended.Components
{
    public class PillarItemDropper : MonoBehaviour
    {
        public enum PillarType
        {
            None = -1,
            Blood,
            Design,
            Mass,
            Soul
        }
        public PillarType pillarType = PillarType.None;
        public bool shouldDropItem = false;

        void OnEnable()
        {
            if (TryGetComponent(out HoldoutZoneController holdout) && shouldDropItem)
            {
                holdout.onCharged.AddListener(new UnityAction<HoldoutZoneController>(this.DropPillarItemFromHoldout));
            }
        }
        void OnDisable()
        {
            if (TryGetComponent(out HoldoutZoneController holdout))
            {
                holdout.onCharged.RemoveListener(new UnityAction<HoldoutZoneController>(this.DropPillarItemFromHoldout));
            }
        }
        public void DropPillarItemFromHoldout(HoldoutZoneController holdoutZone)
        {
            int participatingPlayerCount = Run.instance ? Run.instance.participatingPlayerCount : 0;
            if (participatingPlayerCount == 0)
                return;
            Vector3 dropPosition = holdoutZone.gameObject.transform.position + Vector3.up * pillarDropOffset;
            PickupIndex pickupIndex = GetPickupIndexFromPillarType(this.pillarType);

            if (pickupIndex != PickupIndex.none)
            {
                int num = baseRewardCount;
                if (scaleRewardsByPlayerCount)
                {
                    num *= participatingPlayerCount;
                }

                float angle = 360f / (float)num;
                Vector3 vector = Quaternion.AngleAxis((float)UnityEngine.Random.Range(0, 360), Vector3.up)
                    * (Vector3.up * pillarDropForce + Vector3.forward * 5f);
                Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
                int i = 0;
                while (i < num)
                {
                    PickupDropletController.CreatePickupDroplet(pickupIndex, dropPosition, vector);
                    i++;
                    vector = rotation * vector;
                }
            }
        }

        public static PickupIndex GetPickupIndexFromPillarType(PillarType pillarType)
        {
            ItemBase pickup = null;
            switch (pillarType)
            {
                default:
                    break;
                case PillarType.Mass:
                    pickup = (MassAnomaly.instance);
                    break;
                case PillarType.Design:
                    pickup = (DesignAnomaly.instance);
                    break;
                case PillarType.Blood:
                    pickup = (BloodAnomaly.instance);
                    break;
                case PillarType.Soul:
                    pickup = (SoulAnomaly.instance);
                    break;
            }
            if (pickup != null)
            {
                ItemIndex itemsIndex = pickup.ItemsDef.itemIndex;
                return PickupCatalog.FindPickupIndex(itemsIndex);
            }
            Debug.Log("No pickup index found!");
            return PickupIndex.none;
        }
    }
}
