using PhantomLure.ECS;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PhantomLure.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(MainForceUnitSocialForceSystem))]
    public partial struct MainForceUnitMoveResolveSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MainForceTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            bool hasGrid = SystemAPI.HasSingleton<GridConfig>();
            GridConfig grid = default;
            DynamicBuffer<GridCell> gridCells = default;

            if (hasGrid)
            {
                Entity gridEntity = SystemAPI.GetSingletonEntity<GridConfig>();
                grid = SystemAPI.GetComponent<GridConfig>(gridEntity);
                gridCells = SystemAPI.GetBuffer<GridCell>(gridEntity);
            }

            foreach ((RefRW<LocalTransform> localTransform,
                      RefRO<DesiredVelocity> desiredVelocity,
                      RefRO<AvoidanceVelocity> avoidanceVelocity,
                      RefRW<MoveState> moveState)
                in SystemAPI.Query<RefRW<LocalTransform>,
                                   RefRO<DesiredVelocity>,
                                   RefRO<AvoidanceVelocity>,
                                   RefRW<MoveState>>()
                    .WithAll<MainForceTag>())
            {
                float3 velocity = desiredVelocity.ValueRO.Value + avoidanceVelocity.ValueRO.Value;
                velocity.y = 0.0f;

                if (math.lengthsq(velocity) <= 0.000001f)
                {
                    moveState.ValueRW.IsMoving = false;
                    continue;
                }

                float3 currentPosition = localTransform.ValueRO.Position;
                float3 nextPosition = currentPosition + (velocity * deltaTime);
                nextPosition.y = currentPosition.y;

                if (hasGrid)
                {
                    int2 nextCell = GridUtility.ToCell(grid, nextPosition);
                    if (!GridUtility.IsWalkable(grid, gridCells, nextCell))
                    {
                        moveState.ValueRW.IsMoving = false;
                        continue;
                    }
                }

                localTransform.ValueRW.Position = nextPosition;

                quaternion targetRotation = quaternion.LookRotationSafe(math.normalize(velocity), math.up());
                float rotationT = math.saturate(deltaTime * 10.0f);
                localTransform.ValueRW.Rotation = math.slerp(localTransform.ValueRO.Rotation, targetRotation, rotationT);

                moveState.ValueRW.IsMoving = true;
            }
        }
    }
}
