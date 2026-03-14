using PhantomLure.ECS;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PhantomLure.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ApplyMainForceMoveCommandSystem))]
    public partial struct MainForceMoveSystem : ISystem
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

            foreach ((
                RefRW<LocalTransform> localTransform,
                RefRO<MoveTarget> moveTarget,
                RefRW<MoveState> moveState,
                RefRO<MoveSpeed> moveSpeed)
                in SystemAPI.Query<
                    RefRW<LocalTransform>,
                    RefRO<MoveTarget>,
                    RefRW<MoveState>,
                    RefRO<MoveSpeed>>().WithAll<MainForceTag>())
            {
                if (!moveState.ValueRO.IsMoving)
                {
                    continue;
                }

                float3 currentPosition = localTransform.ValueRO.Position;
                float3 targetPosition = moveTarget.ValueRO.Position;
                float3 toTarget = targetPosition - currentPosition;
                float distance = math.length(toTarget);

                if (distance <= moveTarget.ValueRO.StoppingDistance)
                {
                    moveState.ValueRW.IsMoving = false;
                    continue;
                }

                float3 direction = toTarget / math.max(distance, 0.0001f);
                float step = moveSpeed.ValueRO.Value * deltaTime;

                if (step >= distance)
                {
                    localTransform.ValueRW.Position = targetPosition;
                    moveState.ValueRW.IsMoving = false;
                }
                else
                {
                    localTransform.ValueRW.Position = currentPosition + direction * step;
                    localTransform.ValueRW.Rotation = quaternion.LookRotationSafe(direction, math.up());
                }
            }
        }
    }
}