using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PhantomLure.ECS
{
    public class SpawnPointAuthoring : MonoBehaviour
    {
        [SerializeField] private SpawnPointTagId[] _tags;
        [SerializeField] private float _weight = 1f;

        public SpawnPointTagId[] Tags => _tags;
        public float Weight => _weight;
    }

    public class SpawnPointBaker : Baker<SpawnPointAuthoring>
    {
        public override void Bake(SpawnPointAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent<SpawnPointTag>(entity);

            AddComponent(entity, new SpawnArea
            {
                Center = authoring.transform.position,
                HalfExtents = float3.zero
            });

            AddComponent(entity, new SpawnPointWeight
            {
                Value = Mathf.Max(0f, authoring.Weight)
            });

            var sectorAuthoring = authoring.GetComponentInParent<SectorAuthoring>();
            if (sectorAuthoring != null)
            {
                var sectorEntity = GetEntity(sectorAuthoring, TransformUsageFlags.None);
                AddComponent(entity, new SpawnPointSector
                {
                    Value = sectorEntity
                });
            }

            DynamicBuffer<SpawnPointTags> buffer = AddBuffer<SpawnPointTags>(entity);
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