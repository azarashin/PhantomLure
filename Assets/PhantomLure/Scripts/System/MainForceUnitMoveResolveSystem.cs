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
                      RefRW<MoveState> moveState,
                      Entity entity)
                in SystemAPI.Query<RefRW<LocalTransform>,
                                   RefRO<DesiredVelocity>,
                                   RefRO<AvoidanceVelocity>,
                                   RefRW<MoveState>>()
                    .WithAll<MainForceTag>()
                    .WithEntityAccess())
            {
                float3 desired = desiredVelocity.ValueRO.Value;
                float3 avoidance = avoidanceVelocity.ValueRO.Value;

                desired.y = 0.0f;
                avoidance.y = 0.0f;

                float3 combinedVelocity = desired + avoidance;
                combinedVelocity.y = 0.0f;

                if (math.lengthsq(combinedVelocity) <= 0.000001f)
                {
                    moveState.ValueRW.IsMoving = false;
                    continue;
                }

                float3 currentPosition = localTransform.ValueRO.Position;
                float3 resolvedStep = ResolveStep(
                    hasGrid,
                    grid,
                    gridCells,
                    currentPosition,
                    combinedVelocity,
                    deltaTime);

                if (math.lengthsq(resolvedStep) <= 0.0000001f)
                {
                    moveState.ValueRW.IsMoving = false;
                    continue;
                }

                float3 nextPosition = currentPosition + resolvedStep;
                nextPosition.y = currentPosition.y;
                localTransform.ValueRW.Position = nextPosition;

                float3 moveDirection = resolvedStep;
                moveDirection.y = 0.0f;

                if (math.lengthsq(moveDirection) > 0.000001f)
                {
                    quaternion targetRotation = quaternion.LookRotationSafe(math.normalize(moveDirection), math.up());
                    float rotationT = math.saturate(deltaTime * 10.0f);
                    localTransform.ValueRW.Rotation = math.slerp(localTransform.ValueRO.Rotation, targetRotation, rotationT);
                }

                moveState.ValueRW.IsMoving = true;
            }
        }

        [BurstCompile]
        private static float3 ResolveStep(
            bool hasGrid,
            GridConfig grid,
            DynamicBuffer<GridCell> gridCells,
            float3 currentPosition,
            float3 velocity,
            float deltaTime)
        {
            float3 fullStep = velocity * deltaTime;
            fullStep.y = 0.0f;

            if (math.lengthsq(fullStep) <= 0.0000001f)
            {
                return float3.zero;
            }

            if (!hasGrid)
            {
                return fullStep;
            }

            if (IsWalkable(grid, gridCells, currentPosition + fullStep))
            {
                return fullStep;
            }

            float stepLength = math.length(fullStep);

            if (stepLength <= 0.00001f)
            {
                return float3.zero;
            }

            float3 forward = fullStep / stepLength;

            float3 bestStep = float3.zero;
            float bestScore = -1.0f;

            // path 方向を優先しつつ、少しだけ左右に振って試す
            float angle15 = math.radians(15.0f);
            float angle30 = math.radians(30.0f);
            float angle45 = math.radians(45.0f);
            float angle60 = math.radians(60.0f);

            TryCandidate(grid, gridCells, currentPosition, RotateY(forward, angle15) * stepLength, forward, ref bestStep, ref bestScore);
            TryCandidate(grid, gridCells, currentPosition, RotateY(forward, -angle15) * stepLength, forward, ref bestStep, ref bestScore);
            TryCandidate(grid, gridCells, currentPosition, RotateY(forward, angle30) * stepLength, forward, ref bestStep, ref bestScore);
            TryCandidate(grid, gridCells, currentPosition, RotateY(forward, -angle30) * stepLength, forward, ref bestStep, ref bestScore);
            TryCandidate(grid, gridCells, currentPosition, RotateY(forward, angle45) * stepLength, forward, ref bestStep, ref bestScore);
            TryCandidate(grid, gridCells, currentPosition, RotateY(forward, -angle45) * stepLength, forward, ref bestStep, ref bestScore);
            TryCandidate(grid, gridCells, currentPosition, RotateY(forward, angle60) * stepLength, forward, ref bestStep, ref bestScore);
            TryCandidate(grid, gridCells, currentPosition, RotateY(forward, -angle60) * stepLength, forward, ref bestStep, ref bestScore);

            if (bestScore >= 0.0f)
            {
                return bestStep;
            }

            // 長さを半分にして正面だけ最後に試す
            float3 halfStep = forward * (stepLength * 0.5f);

            if (IsWalkable(grid, gridCells, currentPosition + halfStep))
            {
                return halfStep;
            }

            return float3.zero;
        }

        [BurstCompile]
        private static void TryCandidate(
            GridConfig grid,
            DynamicBuffer<GridCell> gridCells,
            float3 currentPosition,
            float3 candidateStep,
            float3 preferredDirection,
            ref float3 bestStep,
            ref float bestScore)
        {
            candidateStep.y = 0.0f;

            if (math.lengthsq(candidateStep) <= 0.0000001f)
            {
                return;
            }

            float3 candidatePosition = currentPosition + candidateStep;

            if (!IsWalkable(grid, gridCells, candidatePosition))
            {
                return;
            }

            float3 candidateDirection = math.normalize(candidateStep);
            float alignmentScore = math.dot(candidateDirection, preferredDirection);
            float distanceScore = math.lengthsq(candidateStep) * 0.01f;
            float score = alignmentScore + distanceScore;

            if (score > bestScore)
            {
                bestScore = score;
                bestStep = candidateStep;
            }
        }

        [BurstCompile]
        private static float3 RotateY(float3 vector, float radians)
        {
            float s = math.sin(radians);
            float c = math.cos(radians);

            return new float3(
                (vector.x * c) - (vector.z * s),
                0.0f,
                (vector.x * s) + (vector.z * c));
        }

        [BurstCompile]
        private static bool IsWalkable(GridConfig grid, DynamicBuffer<GridCell> gridCells, float3 worldPosition)
        {
            int2 cell = GridUtility.ToCell(grid, worldPosition);
            return GridUtility.IsWalkable(grid, gridCells, cell);
        }
    }
}
