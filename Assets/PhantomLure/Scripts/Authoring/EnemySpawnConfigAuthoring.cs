using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

namespace PhantomLure.ECS
{
    public enum EnemySpawnPattern
    {
        Circle,
        FrontLine,
        RandomRect
    }

    public sealed class EnemySpawnConfigAuthoring : MonoBehaviour
    {
        [Header("Prefab (Enemy)")]
        public GameObject EnemyPrefab;

        [Header("Common")]
        public EnemySpawnPattern Pattern = EnemySpawnPattern.Circle;
        public int TotalCount = 200;
        public int SquadCount = 3;
        public int RandomSeed = 12345;

        [Header("Center / Base Transform")]
        public Vector3 Center = Vector3.zero;

        [Header("Circle")]
        public float CircleRadius = 12f;
        public float CircleSquadRadiusStep = 4f;   // squadごとに半径をずらす

        [Header("FrontLine")]
        public float FrontLineLength = 40f;        // X方向の長さ
        public float FrontLineSpacing = 1.2f;      // 兵の間隔
        public float FrontLineZOffset = 0f;        // 前線のZ位置（Center.z + offset）
        public float FrontLineSquadZStep = 3.5f;   // squadごとにZをずらす（縦列）

        [Header("Random Rect")]
        public float2 RandomRectSize = new float2(40f, 40f); // XZサイズ
        public float RandomMinDistanceJitter = 0f;           // 0なら純ランダム、>0で少し散らす補助

        [Header("Attribs")]
        public SpawnPointTagId[] Tags;
        public float Weight = 1f;


    }

    public struct EnemySpawnConfig : IComponentData
    {
        public Entity EnemyPrefab;

        public EnemySpawnPattern Pattern;
        public int TotalCount;
        public int SquadCount;
        public uint RandomSeed;

        public float3 Center;

        public float CircleRadius;
        public float CircleSquadRadiusStep;

        public float FrontLineLength;
        public float FrontLineSpacing;
        public float FrontLineZOffset;
        public float FrontLineSquadZStep;

        public float2 RandomRectSize;
        public float RandomMinDistanceJitter;
    }

    public sealed class EnemySpawnConfigBaker : Baker<EnemySpawnConfigAuthoring>
    {
        public override void Bake(EnemySpawnConfigAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.None);

            var prefabEntity = GetEntity(authoring.EnemyPrefab, TransformUsageFlags.Dynamic);

            AddComponent(e, new EnemySpawnConfig
            {
                EnemyPrefab = prefabEntity,

                Pattern = authoring.Pattern,
                TotalCount = math.max(0, authoring.TotalCount),
                SquadCount = math.max(1, authoring.SquadCount),
                RandomSeed = (uint)math.max(1, authoring.RandomSeed),

                Center = authoring.Center,

                CircleRadius = authoring.CircleRadius,
                CircleSquadRadiusStep = authoring.CircleSquadRadiusStep,

                FrontLineLength = authoring.FrontLineLength,
                FrontLineSpacing = math.max(0.01f, authoring.FrontLineSpacing),
                FrontLineZOffset = authoring.FrontLineZOffset,
                FrontLineSquadZStep = authoring.FrontLineSquadZStep,

                RandomRectSize = authoring.RandomRectSize,
                RandomMinDistanceJitter = math.max(0f, authoring.RandomMinDistanceJitter)
            });

            AddComponent<SpawnPointTag>(e);

            AddComponent(e, new SpawnArea
            {
                Center = authoring.transform.position,
                HalfExtents = float3.zero
            });

            AddComponent(e, new SpawnPointWeight
            {
                Value = Mathf.Max(0f, authoring.Weight)
            });


            var sectorAuthoring = authoring.GetComponentInParent<SectorAuthoring>();
            if (sectorAuthoring != null)
            {
                var sectorEntity = GetEntity(sectorAuthoring, TransformUsageFlags.None);
                AddComponent(e, new SpawnPointSector
                {
                    Value = sectorEntity
                });
            }

            DynamicBuffer<SpawnPointTags> buffer = AddBuffer<SpawnPointTags>(e);
            SpawnPointTagId[] tags = authoring.Tags;
            if (tags != null)
            {
                for (int i = 0; i < tags.Length; i++)
                {
                    buffer.Add(new SpawnPointTags
                    {
                        Value = (int)tags[i]
                    });
                }
            }
        }
    }
}
