using Unity.Entities;
using Unity.Mathematics;

namespace PhantomLure.ECS
{
    // -----------------------------
    // 移動
    // -----------------------------

    public struct MoveTarget : IComponentData
    {
        public float3 Position;
        public float StoppingDistance;
    }

    public struct MoveSpeed : IComponentData
    {
        public float Value;
    }

    public struct MoveState : IComponentData
    {
        public bool IsMoving;
    }

    // -----------------------------
    // 本隊
    // -----------------------------

    public struct MainForceTag : IComponentData
    {
    }

    public struct FormationIndex : IComponentData
    {
        public int Value;
    }

    public struct FormationMember : IComponentData
    {
        public Entity AnchorEntity;
    }

    /// <summary>
    /// 本隊全体の中心情報と隊列設定
    /// </summary>
    public struct MainForceFormationAnchor : IComponentData
    {
        public float3 Position;
        public float3 Forward;
        public float3 Destination;
        public float MoveSpeed;
        public float ArriveDistance;
        public bool IsMoving;

        public int ColumnCount;
        public float SpacingSide;
        public float SpacingBack;
        public float SlotCatchUpDistance;
        public float SlowDownDistance;
        public float MaxCatchUpMultiplier;
    }

    /// <summary>
    /// アンカーの経路追従状態
    /// </summary>
    public struct MainForcePathState : IComponentData
    {
        public int CurrentPathIndex;
        public float WaypointReachDistance;
        public byte WaitingForPath;
    }

    /// <summary>
    /// ユニットの局所障害物回避設定
    /// </summary>
    public struct MainForceAvoidanceSettings : IComponentData
    {
        public float BlockProbeDistance;
        public float ObstacleRepulsionRadius;
        public float ObstacleRepulsionWeight;
        public float LateralProbeDistance;
        public float LateralProbeWeight;
    }

    /// <summary>
    /// 本隊ユニット同士の社会力モデル風反発設定
    /// </summary>
    public struct MainForceSocialForceAgent : IComponentData
    {
        public float PersonalSpaceRadius;
        public float NeighborRadius;
        public float RepulsionStrength;
        public float FalloffDistance;
        public float MaxRepulsionSpeed;
    }

    /// <summary>
    /// 本隊全体への移動命令
    /// </summary>
    public struct MainForceMoveCommand : IComponentData
    {
        public float3 Destination;
    }

    public struct MainForceCommandDebug : IComponentData
    {
        public float3 Position;
        public float LifeTime;
    }

    // -----------------------------
    // 経路
    // -----------------------------

    public struct PathNode : IBufferElementData
    {
        public int2 Cell;
        public float3 World;
    }

    public struct FormationGridAnchorRef : IComponentData
    {
        public Entity Value;
    }

}