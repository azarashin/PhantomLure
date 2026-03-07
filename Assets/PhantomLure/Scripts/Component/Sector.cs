using Unity.Entities;
using Unity.Mathematics;

// -----------------------------
// Sector
// -----------------------------

namespace PhantomLure.ECS
{
    public struct SectorBounds : IComponentData
    {
        public float3 Center;
        public float3 Size;
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

    public struct SectorTags : IComponentData
    {
        public uint Value;
    }

    public struct AlertLevel : IComponentData
    {
        public int Value;
    }

    public struct SpawnBudget : IComponentData
    {
        public int Value;
    }
}