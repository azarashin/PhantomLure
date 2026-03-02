using Unity.Entities;
using Unity.Mathematics;

namespace PhantomLure.ECS
{
    public struct SectorBounds : IComponentData
    {
        public float2 Min;
        public float2 Max;
    }

    public struct SpawnPoint : IBufferElementData
    {
        public float3 Position;
    }
}