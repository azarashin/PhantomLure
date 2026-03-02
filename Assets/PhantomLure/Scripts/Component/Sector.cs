using Unity.Entities;
using Unity.Mathematics;

// -----------------------------
// Sector
// -----------------------------

namespace PhantomLure.ECS
{
    public struct SectorTag : IComponentData { }
    public struct SectorBounds : IComponentData
    {
        public float2 Min;
        public float2 Max;
        public float3 Center;
        public float3 HalfExtents; // (x,y,z) 半径
    }

    /// <summary>Sector の識別（ログ/デバッグ用に便利）</summary>
    public struct SectorId : IComponentData
    {
        public int Value;
    }

    public struct SpawnPoint : IBufferElementData
    {
        public float3 Position;
    }
}