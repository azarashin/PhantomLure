using Unity.Entities;
using Unity.Mathematics;

namespace PhantomLure.ECS
{
    public struct AssignedSlot : IComponentData
    {
        public float3 WorldPosition;
        public int2 SlotCell;
        public byte IsValid;

        public float3 NavigationTargetWorld;
        public int2 NavigationTargetCell;
        public byte IsSlotWalkable;
    }

    public struct UnitPathState : IComponentData
    {
        public int CurrentPathIndex;
        public float WaypointReachDistance;
        public byte WaitingForPath;
    }

    public struct UnitRepathSettings : IComponentData
    {
        public float RepathInterval;
        public float SlotMoveThreshold;
        public float StuckDistanceThreshold;
        public float StuckTimeThreshold;
    }

    public struct UnitRepathState : IComponentData
    {
        public float LastRepathTime;
        public float3 LastRequestedGoal;
        public float3 LastPosition;
    }

    public struct UnitStuckState : IComponentData
    {
        public float AccumulatedTime;
        public byte IsStuck;
    }

    public struct DesiredVelocity : IComponentData
    {
        public float3 Value;
    }

    public struct AvoidanceVelocity : IComponentData
    {
        public float3 Value;
    }

    public struct NeedsUnitRepathTag : IComponentData
    {
    }
}
