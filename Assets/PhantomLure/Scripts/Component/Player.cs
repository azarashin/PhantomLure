// -----------------------------
// Player
// -----------------------------

using Unity.Entities;

using Unity.Mathematics;

namespace PhantomLure.ECS
{
    /// <summary>Player 本体タグ</summary>
    public struct PlayerTag : IComponentData { }

    /// <summary>プレイヤーの移動/操作入力（最小）</summary>
    public struct PlayerInput : IComponentData
    {
        public float2 Move;     // WASD 等：-1..1
        public bool Action;    // デコイ設置等の1ボタン
    }

    /// <summary>移動パラメータ（Player/Enemy/Droneでも流用可）</summary>
    public struct MoveSpeed : IComponentData
    {
        public float Value; // m/s
    }
    public struct PlayerCommandData : IComponentData
    {
        public float2 Move;                 // -1 ～ 1
        public byte ClickRequested;         // 1フレームだけ立つ
        public float3 ClickWorldPosition;   // クリック地点
    }
    public struct PlayerInputConfig : IComponentData
    {
        public int GroundLayerMask;
        public float FallbackGroundY;
    }
}