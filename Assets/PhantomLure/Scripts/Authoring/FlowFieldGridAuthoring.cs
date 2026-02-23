using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PhantomLure.ECS
{
    public class FlowFieldGridAuthoring : MonoBehaviour
    {
        [Header("Grid")]
        public int2 GridSize = new int2(128, 128);   // X,Z
        public float CellSize = 1.0f;
        public Vector3 Origin = Vector3.zero;

        [Header("Flow Field Update")]
        public float RebuildIntervalSeconds = 0.5f;

        [Header("Density Slowdown")]
        public float RhoMax = 6f;     // ρ_max (agents per cell)
        public float RhoStop = 10f;   // ρ_stop
    }

    public struct FlowFieldGrid : IComponentData
    {
        public int2 GridSize;
        public float CellSize;
        public float3 Origin; // フィールドの原点（左下隅）この範囲を外れているときは流れない
        public float RebuildIntervalSeconds;

        public float RhoMax;
        public float RhoStop;
    }

    public class FlowFieldGridBaker : Baker<FlowFieldGridAuthoring>
    {
        public override void Bake(FlowFieldGridAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.None);
            AddComponent(e, new FlowFieldGrid
            {
                GridSize = authoring.GridSize,
                CellSize = authoring.CellSize,
                Origin = authoring.Origin,
                RebuildIntervalSeconds = authoring.RebuildIntervalSeconds,
                RhoMax = authoring.RhoMax,
                RhoStop = authoring.RhoStop
            });
        }
    }
}
