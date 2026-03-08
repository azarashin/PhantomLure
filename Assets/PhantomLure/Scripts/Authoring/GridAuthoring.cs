using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PhantomLure.ECS
{
    public class GridAuthoring : MonoBehaviour
    {
        public int Width = 32;
        public int Height = 32;
        public float CellSize = 1f;
        public Vector3 Origin = Vector3.zero;

        [Tooltip("true = 通れる, false = 通れない")]
        public bool DefaultWalkable = true;
        public float DefaultCost = 1f;

        class Baker : Baker<GridAuthoring>
        {
            public override void Bake(GridAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new GridConfig
                {
                    Width = authoring.Width,
                    Height = authoring.Height,
                    CellSize = authoring.CellSize,
                    Origin = authoring.Origin
                });

                var buffer = AddBuffer<GridCell>(entity);
                int total = authoring.Width * authoring.Height;
                byte walkable = (byte)(authoring.DefaultWalkable ? 1 : 0);

                for (int i = 0; i < total; i++)
                {
                    buffer.Add(new GridCell
                    {
                        Walkable = walkable,
                        Cost = authoring.DefaultCost
                    });
                }

                AddComponent<SystemHandleMarker>(entity);
            }
        }
    }
}
