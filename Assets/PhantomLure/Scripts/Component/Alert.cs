// -----------------------------
// Alert（警戒/アラート情報）
// -----------------------------

using Unity.Entities;
using Unity.Mathematics;

namespace PhantomLure.ECS
{
    public struct AlertTag : IComponentData { }

    public enum AlertKind : byte
    {
        Unknown = 0,
        Sight,
        Sound,
        Decoy
    }

    /// <summary>アラートイベント（最小）</summary>
    public struct AlertEvent : IComponentData
    {
        public AlertKind Kind;
        public float3 Position;
        public float Radius;
        public float Strength;     // 0..1 など
        public float ExpireTime;   // Time.ElapsedTime と比較（秒）
        public Entity Source;       // 発生源（Decoy/Player/Drone等）
        public Entity Sector;       // 属するSector（必要なら）
    }
}
