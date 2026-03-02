// -----------------------------
// Drone（索敵ドローン）
// -----------------------------

using Unity.Entities;
using Unity.Mathematics;

namespace PhantomLure.ECS
{
    public struct DroneTag : IComponentData { }

    public enum DroneMode : byte
    {
        Follow = 0,
        Patrol,
        Investigate
    }

    /// <summary>Drone の最低限</summary>
    public struct DroneData : IComponentData
    {
        public Entity Owner;          // Player/Enemy どちらでも
        public DroneMode Mode;
        public float MoveSpeed;      // Drone専用（共通MoveSpeedでもOK）
        public float SensorRadius;   // 索敵範囲
        public float3 PatrolCenter;
        public float PatrolRadius;
        public Entity Interest;       // 調査対象（Alert/Decoyなど）
    }
}