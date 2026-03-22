using PhantomLure.ECS;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PhantomLure.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(MainForceSlotAssignmentSystem))]
    public partial struct MainForceUnitRepathDecisionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MainForceTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float elapsedTime = (float)SystemAPI.Time.ElapsedTime;
            float deltaTime = SystemAPI.Time.DeltaTime;

            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach ((RefRO<LocalTransform> localTransform,
                      RefRO<AssignedSlot> assignedSlot,
                      RefRO<UnitRepathSettings> repathSettings,
                      RefRW<UnitRepathState> repathState,
                      RefRW<UnitStuckState> stuckState,
                      Entity entity)
                in SystemAPI.Query<RefRO<LocalTransform>,
                                   RefRO<AssignedSlot>,
                                   RefRO<UnitRepathSettings>,
                                   RefRW<UnitRepathState>,
                                   RefRW<UnitStuckState>>()
                    .WithAll<MainForceTag>()
                    .WithEntityAccess())
            {
                if (assignedSlot.ValueRO.IsValid == 0)
                {
                    continue;
                }

                float movedDistance = math.distance(localTransform.ValueRO.Position, repathState.ValueRO.LastPosition);
                float goalMovedDistance = math.distance(repathState.ValueRO.LastRequestedGoal, assignedSlot.ValueRO.WorldPosition);

                if (movedDistance <= repathSettings.ValueRO.StuckDistanceThreshold)
                {
                    stuckState.ValueRW.AccumulatedTime += deltaTime;
                }
                else
                {
                    stuckState.ValueRW.AccumulatedTime = 0.0f;
                    stuckState.ValueRW.IsStuck = 0;
                }

                if (stuckState.ValueRO.AccumulatedTime >= repathSettings.ValueRO.StuckTimeThreshold)
                {
                    stuckState.ValueRW.IsStuck = 1;
                }

                bool repathCooldownElapsed =
                    (elapsedTime - repathState.ValueRO.LastRepathTime) >= repathSettings.ValueRO.RepathInterval;

                bool goalMoved = goalMovedDistance >= repathSettings.ValueRO.SlotMoveThreshold;
                bool shouldRepath = repathCooldownElapsed && (goalMoved || stuckState.ValueRO.IsStuck == 1);

                if (repathState.ValueRO.LastRepathTime < -100.0f)
                {
                    shouldRepath = true;
                }

                if (shouldRepath && !SystemAPI.HasComponent<NeedsUnitRepathTag>(entity))
                {
                    ecb.AddComponent<NeedsUnitRepathTag>(entity);
                }

                repathState.ValueRW.LastPosition = localTransform.ValueRO.Position;
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
