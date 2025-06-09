using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;

namespace RainrotSharedUtils.Components
{
    public class ProjectileOverlapDecayDamage : ProjectileOverlapLimitHits
    {
        internal float initialDamageCoefficient = 1.0f;
        internal float initialProcCoefficient = 1.0f;
        public float firstHitDamageMultiplier = 1.0f;
        public float onHitDamageMultiplier = 1.0f;
    }
}
