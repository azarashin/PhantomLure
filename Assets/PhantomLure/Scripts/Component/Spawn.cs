using Unity.Entities;
using Unity.Mathematics;

// -----------------------------
// Spawn（スポーン制御）
// -----------------------------

namespace PhantomLure.ECS
{
    public struct SpawnTag : IComponentData { }

    /// <summary>スポーン設定（最小）</summary>
    public struct SpawnConfig : IComponentData
    {
        public Entity Prefab;
        public int CountPerWave;
        public float Interval;     // 秒
        public float NextTime;     // 次回スポーン時刻（秒）
    }

    /// <summary>スポーン範囲（簡易）</summary>
    public struct SpawnArea : IComponentData
    {
        public float3 Center;
        public float3 HalfExtents;
    }
}