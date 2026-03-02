// -----------------------------
// Decoy（おとり）
// -----------------------------

using Unity.Entities;

namespace PhantomLure.ECS
{
    public struct DecoyTag : IComponentData { }

    /// <summary>Decoy の最低限</summary>
    public struct DecoyData : IComponentData
    {
        public Entity Owner;       // Player など
        public float Strength;    // アラート強度
        public float ExpireTime;  // 寿命（秒）
        public float PulseInterval; // 何秒ごとにAlert出すか（0なら単発）
        public float NextPulseTime;
    }
}