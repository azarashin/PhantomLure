// -----------------------------
// Enemy
// -----------------------------

using Unity.Entities;

namespace PhantomLure.ECS
{
    public struct EnemyTag : IComponentData { }

    public enum EnemyState : byte
    {
        Idle = 0,
        Patrol,
        Chase,
        Search,
        Return
    }

    /// <summary>Enemy の状態（最小）</summary>
    public struct EnemyAI : IComponentData
    {
        public EnemyState State;
        public Entity Target;        // 追跡対象（Player/Decoyなど）
        public float Suspicion01;    // 0..1
    }

    /// <summary>Enemy の知覚（最小）</summary>
    public struct Sensor : IComponentData
    {
        public float ViewRadius;
        public float HearRadius;
    }
}
