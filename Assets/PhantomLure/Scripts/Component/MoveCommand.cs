using Unity.Entities;
using Unity.Mathematics;

namespace PhantomLure.ECS
{
    /// <summary>
    /// 本隊ユニットタグ
    /// </summary>
    public struct MainForceTag : IComponentData
    {
    }

    /// <summary>移動パラメータ（Player/Enemy/Droneでも流用可）</summary>
    public struct MoveSpeed : IComponentData
    {
        public float Value; // m/s
    }

    /// <summary>
    /// 現在の移動先
    /// </summary>
    public struct MoveTarget : IComponentData
    {
        public float3 Position;
        public float StoppingDistance;
    }

    /// <summary>
    /// 移動状態
    /// </summary>
    public struct MoveState : IComponentData
    {
        public bool IsMoving;
    }

    /// <summary>
    /// 本隊内での隊列順
    /// </summary>
    public struct FormationIndex : IComponentData
    {
        public int Value;
    }

    /// <summary>
    /// 本隊アンカー参照
    /// </summary>
    public struct FormationMember : IComponentData
    {
        public Entity AnchorEntity;
    }

    /// <summary>
    /// 本隊全体の中心情報
    /// </summary>
    public struct MainForceFormationAnchor : IComponentData
    {
        public float3 Position;
        public float3 Forward;
        public float3 Destination;
        public float MoveSpeed;
        public float ArriveDistance;
        public bool IsMoving;
    }

    /// <summary>
    /// 隊列設定
    /// </summary>
    public struct MainForceFormationSettings : IComponentData
    {
        public int ColumnCount;
        public float SpacingSide;
        public float SpacingBack;
        public float SlotCatchUpDistance;
        public float SlowDownDistance;
        public float MaxCatchUpMultiplier;
    }

    /// 本隊全体への移動命令
    /// 本隊全体への移動命令
    /// </summary>
    public struct MainForceMoveCommand : IComponentData
    {
        public float3 Destination;
        public float3 Forward;
        public float StoppingDistance;
        public float SpacingX;
        public float SpacingZ;
        public int ColumnCount;
    }

    public struct MainForceCommandDebug : IComponentData
    {
        public float3 Position;
        public float LifeTime;
    }

}