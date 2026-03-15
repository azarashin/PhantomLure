using PhantomLure.ECS;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PhantomLure.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(FormationAnchorMoveSystem))]
    public partial struct MainForceSlotFollowSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MainForceFormationAnchor>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach ((
                RefRW<LocalTransform> localTransform,
                RefRO<MoveSpeed> moveSpeed,
                RefRO<FormationIndex> formationIndex,
                RefRO<FormationMember> formationMember)
                in SystemAPI.Query<
                    RefRW<LocalTransform>,
                    RefRO<MoveSpeed>,
                    RefRO<FormationIndex>,
                    RefRO<FormationMember>>().WithAll<MainForceTag>())
            {
                if (!SystemAPI.Exists(formationMember.ValueRO.AnchorEntity))
                {
                    continue;
                }

                RefRO<MainForceFormationAnchor> anchor =
                    SystemAPI.GetComponentRO<MainForceFormationAnchor>(formationMember.ValueRO.AnchorEntity);

                RefRO<MainForceFormationSettings> settings =
                    SystemAPI.GetComponentRO<MainForceFormationSettings>(formationMember.ValueRO.AnchorEntity);

                float3 forward = anchor.ValueRO.Forward;
                forward.y = 0.0f;

                if (math.lengthsq(forward) < 0.0001f)
                {
                    forward = new float3(0.0f, 0.0f, 1.0f);
                }
                else
                {
                    forward = math.normalize(forward);
                }

                float3 right = math.normalize(math.cross(new float3(0.0f, 1.0f, 0.0f), forward));

                int columnCount = math.max(1, settings.ValueRO.ColumnCount);
                int index = math.max(0, formationIndex.ValueRO.Value);

                int column = index % columnCount;
                int row = index / columnCount;

                float centeredColumn = column - ((columnCount - 1) * 0.5f);

                float3 slotOffset =
                    (right * (centeredColumn * settings.ValueRO.SpacingSide)) -
                    (forward * (row * settings.ValueRO.SpacingBack));

                float3 slotPosition = anchor.ValueRO.Position + slotOffset;
                float3 currentPosition = localTransform.ValueRO.Position;

                float3 toSlot = slotPosition - currentPosition;
                toSlot.y = 0.0f;

                float distanceToSlot = math.length(toSlot);

                if (distanceToSlot <= 0.02f)
                {
                    continue;
                }

                float3 direction = toSlot / math.max(distanceToSlot, 0.0001f);

                float forwardOffset = math.dot(currentPosition - slotPosition, forward);
                float speedMultiplier = 1.0f;

                if (forwardOffset > 0.0f)
                {
                    speedMultiplier *= 0.75f;
                }
                else
                {
                    float catchUpT = math.saturate(distanceToSlot / math.max(0.01f, settings.ValueRO.SlotCatchUpDistance));
                    float catchUpMultiplier = math.lerp(1.0f, settings.ValueRO.MaxCatchUpMultiplier, catchUpT);
                    speedMultiplier *= catchUpMultiplier;
                }

                float slowDownT = math.saturate(distanceToSlot / settings.ValueRO.SlowDownDistance);
                float slowDownMultiplier = math.lerp(0.15f, 1.0f, slowDownT);
                speedMultiplier *= slowDownMultiplier;

                float step = moveSpeed.ValueRO.Value * speedMultiplier * deltaTime;

                if (step >= distanceToSlot)
                {
                    localTransform.ValueRW.Position = slotPosition;
                }
                else
                {
                    localTransform.ValueRW.Position = currentPosition + (direction * step);
                }

                float3 flatVelocity = direction;
                flatVelocity.y = 0.0f;

                if (math.lengthsq(flatVelocity) > 0.0001f)
                {
                    localTransform.ValueRW.Rotation = quaternion.LookRotationSafe(math.normalize(flatVelocity), math.up());
                }
            }
        }
    }
}
