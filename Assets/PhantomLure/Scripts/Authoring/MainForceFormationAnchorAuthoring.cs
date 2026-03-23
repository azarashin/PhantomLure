using PhantomLure.ECS;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PhantomLure.Authoring
{
    public class MainForceFormationAnchorAuthoring : MonoBehaviour
    {
        [SerializeField]
        float _moveSpeed = 4.0f;

        [SerializeField]
        float _arriveDistance = 0.25f;

        [SerializeField]
        int _columnCount = 3;

        [SerializeField]
        float _spacingSide = 1.6f;

        [SerializeField]
        float _spacingBack = 2.0f;

        [SerializeField]
        float _slotCatchUpDistance = 3.0f;

        [SerializeField]
        float _slowDownDistance = 1.25f;

        [SerializeField]
        float _maxCatchUpMultiplier = 1.75f;

        [SerializeField]
        float _waypointReachDistance = 0.3f;

        [SerializeField]
        float _blockProbeDistance = 0.8f;

        [SerializeField]
        float _obstacleRepulsionRadius = 1.25f;

        [SerializeField]
        float _obstacleRepulsionWeight = 1.2f;

        [SerializeField]
        float _lateralProbeDistance = 0.9f;

        [SerializeField]
        float _lateralProbeWeight = 1.35f;

        private class Baker : Baker<MainForceFormationAnchorAuthoring>
        {
            public override void Bake(MainForceFormationAnchorAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new MainForceFormationAnchor
                {
                    Position = authoring.transform.position,
                    Forward = new float3(0.0f, 0.0f, 1.0f),
                    Destination = authoring.transform.position,
                    MoveSpeed = math.max(0.01f, authoring._moveSpeed),
                    ArriveDistance = math.max(0.01f, authoring._arriveDistance),
                    IsMoving = false,
                    ColumnCount = math.max(1, authoring._columnCount),
                    SpacingSide = math.max(0.1f, authoring._spacingSide),
                    SpacingBack = math.max(0.1f, authoring._spacingBack),
                    SlotCatchUpDistance = math.max(0.01f, authoring._slotCatchUpDistance),
                    SlowDownDistance = math.max(0.01f, authoring._slowDownDistance),
                    MaxCatchUpMultiplier = math.max(1.0f, authoring._maxCatchUpMultiplier)
                });

                AddComponent(entity, new MainForcePathState
                {
                    CurrentPathIndex = 0,
                    WaypointReachDistance = math.max(0.05f, authoring._waypointReachDistance),
                    WaitingForPath = 0
                });

                AddComponent(entity, new MainForceAvoidanceSettings
                {
                    BlockProbeDistance = math.max(0.05f, authoring._blockProbeDistance),
                    ObstacleRepulsionRadius = math.max(0.1f, authoring._obstacleRepulsionRadius),
                    ObstacleRepulsionWeight = math.max(0.0f, authoring._obstacleRepulsionWeight),
                    LateralProbeDistance = math.max(0.05f, authoring._lateralProbeDistance),
                    LateralProbeWeight = math.max(0.0f, authoring._lateralProbeWeight)
                });

                AddBuffer<PathNode>(entity);
            }
        }
    }
}