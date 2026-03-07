using Unity.Entities;
using Unity.Mathematics;

// -----------------------------
// Spawn（スポーン制御）
// -----------------------------

namespace PhantomLure.ECS
{
    public enum SpawnPointTagId
    {
        None = 0,
        Default = 1,
        Ambush = 2,
        Safe = 3,
        Patrol = 4,
        NearEntrance = 5,
        NearObjective = 6
    }

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

    /// <summary>点スポーン地点であることを示すタグ</summary>
    public struct SpawnPointTag : IComponentData { }

    /// <summary>この SpawnPoint が属する Sector</summary>
    public struct SpawnPointSector : IComponentData
    {
        public Entity Value;
    }

    /// <summary>SpawnPoint の抽選重み</summary>
    public struct SpawnPointWeight : IComponentData
    {
        public float Value;
    }

    /// <summary>SpawnPoint に付与する任意タグ</summary>
    public struct SpawnPointTags : IBufferElementData
    {
        public int Value;
    }

}