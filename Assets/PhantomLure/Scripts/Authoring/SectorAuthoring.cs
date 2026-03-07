using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PhantomLure.ECS
{
    [Flags]
    public enum SectorTagFlags : uint
    {
        None = 0,
        Indoor = 1 << 0,
        Outdoor = 1 << 1,
        HighTraffic = 1 << 2,
        Restricted = 1 << 3,
        Spawnable = 1 << 4,
        Objective = 1 << 5,
    }

    public class SectorAuthoring : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private int sectorId = 0;

        [Header("Bounds")]
        [SerializeField] private Vector3 boundsCenter = Vector3.zero;
        [SerializeField] private Vector3 boundsSize = new Vector3(10f, 3f, 10f);

        [Header("Sector Settings")]
        [SerializeField] private SectorTagFlags sectorTags = SectorTagFlags.None;
        [SerializeField] private int alertLevel = 0;
        [SerializeField] private int spawnBudget = 0;

        public int SectorId => sectorId;
        public Vector3 BoundsCenter => boundsCenter;
        public Vector3 BoundsSize => boundsSize;
        public SectorTagFlags SectorTags => sectorTags;
        public int AlertLevel => alertLevel;
        public int SpawnBudget => spawnBudget;
    }

    public class SectorAuthoringBaker : Baker<SectorAuthoring>
    {
        public override void Bake(SectorAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new SectorId
            {
                Value = authoring.SectorId
            });

            AddComponent(entity, new SectorBounds
            {
                Center = (float3)authoring.BoundsCenter,
                Size = (float3)authoring.BoundsSize
            });

            AddComponent(entity, new SectorTags
            {
                Value = (uint)authoring.SectorTags
            });

            AddComponent(entity, new AlertLevel
            {
                Value = authoring.AlertLevel
            });

            AddComponent(entity, new SpawnBudget
            {
                Value = authoring.SpawnBudget
            });
        }
    }
}