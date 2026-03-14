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
    /// 本隊全体への移動命令
    /// 1フレームだけ存在する命令エンティティ
    /// </summary>
    public struct MainForceMoveCommand : IComponentData
    {
        public float3 Destination;
        public float StoppingDistance;
    }

    public struct MainForceCommandDebug : IComponentData
    {
        public float3 Position;
        public float LifeTime;
    }

}