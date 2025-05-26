using RoR2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using UnityEngine;

namespace RainrotSharedUtils.Shelters
{
    public class ShelterProviderBehavior : MonoBehaviour
    {
        private static List<ShelterProviderBehavior> instancesList = new List<ShelterProviderBehavior>();
        public static ReadOnlyCollection<ShelterProviderBehavior> readOnlyInstancesList = new ReadOnlyCollection<ShelterProviderBehavior>(ShelterProviderBehavior.instancesList);


        internal bool isSuperShelter = false;
        public float fallbackRadius;
        public IZone zoneBehavior;

        public bool IsInBounds(Vector3 position, float radius = 0)
        {
            Vector3 adjustedPosition = position;
            if(radius > 0)
            {
                Vector3 dir = position - base.transform.position;
                adjustedPosition = position - (dir.normalized * radius);
            }

            if (zoneBehavior != null)
            {
                return zoneBehavior.IsInBounds(adjustedPosition);
            }

            if (fallbackRadius < 1)
                return false;
            Vector3 vector = adjustedPosition - base.transform.position;
            return vector.sqrMagnitude <= this.fallbackRadius * this.fallbackRadius;
        }

        void OnEnable()
        {
            instancesList.Add(this);
        }
        void OnDisable()
        {
            instancesList.Remove(this);
        }
    }
}
