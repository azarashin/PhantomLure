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

            bool hasGrid = SystemAPI.HasSingleton<GridConfig>();
            GridConfig grid = default;
            DynamicBuffer<GridCell> gridCells = default;

            if (hasGrid)
            {
                Entity gridEntity = SystemAPI.GetSingletonEntity<GridConfig>();
                grid = SystemAPI.GetComponent<GridConfig>(gridEntity);
                gridCells = SystemAPI.GetBuffer<GridCell>(gridEntity);
            }

            foreach ((
                RefRW<LocalTransform> localTransform,
                RefRW<MoveTarget> moveTarget,
                RefRW<MoveState> moveState,
                RefRO<MoveSpeed> moveSpeed,
                RefRO<FormationIndex> formationIndex,
                RefRO<FormationMember> formationMember)
                in SystemAPI.Query<
                    RefRW<LocalTransform>,
                    RefRW<MoveTarget>,
                    RefRW<MoveState>,
                    RefRO<MoveSpeed>,
                    RefRO<FormationIndex>,
                    RefRO<FormationMember>>()
                .WithAll<MainForceTag>())
            {
                Entity anchorEntity = formationMember.ValueRO.AnchorEntity;

                if (!SystemAPI.Exists(anchorEntity))
                {
                    moveState.ValueRW.IsMoving = false;
                    continue;
                }

                if (!SystemAPI.HasComponent<MainForceFormationAnchor>(anchorEntity))
                {
                    moveState.ValueRW.IsMoving = false;
                    continue;
                }

                MainForceFormationAnchor anchor = SystemAPI.GetComponent<MainForceFormationAnchor>(anchorEntity);

                float3 anchorForward = anchor.Forward;
                anchorForward.y = 0.0f;

                if (math.lengthsq(anchorForward) < 0.0001f)
                {
                    anchorForward = new float3(0.0f, 0.0f, 1.0f);
                }
                else
                {
                    anchorForward = math.normalize(anchorForward);
                }

                float3 anchorRight = math.normalize(math.cross(new float3(0.0f, 1.0f, 0.0f), anchorForward));

                float3 slotPosition = CalculateSlotWorldPosition(
                    anchor,
                    formationIndex.ValueRO.Value,
                    anchorForward,
                    anchorRight);

                moveTarget.ValueRW.Position = slotPosition;

                float3 currentPosition = localTransform.ValueRO.Position;
                float3 toSlot = slotPosition - currentPosition;
                toSlot.y = 0.0f;

                float distanceToSlot = math.length(toSlot);
                float stoppingDistance = math.max(0.01f, moveTarget.ValueRO.StoppingDistance);

                if (distanceToSlot <= stoppingDistance)
                {
                    moveState.ValueRW.IsMoving = false;
                    continue;
                }

                float catchUpMultiplier = CalculateCatchUpMultiplier(anchor, distanceToSlot);
                float step = moveSpeed.ValueRO.Value * catchUpMultiplier * deltaTime;

                float3 resolvedNextPosition;
                bool canMove = TryResolveMovement(
                    hasGrid,
                    grid,
                    gridCells,
                    currentPosition,
                    toSlot,
                    step,
                    out resolvedNextPosition);

                if (!canMove)
                {
                    moveState.ValueRW.IsMoving = false;
                    continue;
                }

                float3 moveDelta = resolvedNextPosition - currentPosition;
                moveDelta.y = 0.0f;

                if (math.lengthsq(moveDelta) <= 0.000001f)
                {
                    moveState.ValueRW.IsMoving = false;
                    continue;
                }

                localTransform.ValueRW.Position = new float3(
                    resolvedNextPosition.x,
                    currentPosition.y,
                    resolvedNextPosition.z);

                quaternion targetRotation = quaternion.LookRotationSafe(math.normalize(moveDelta), math.up());
                float rotationT = math.saturate(deltaTime * 10.0f);
                localTransform.ValueRW.Rotation = math.slerp(localTransform.ValueRO.Rotation, targetRotation, rotationT);

                moveState.ValueRW.IsMoving = true;
            }
        }

        [BurstCompile]
        private static float3 CalculateSlotWorldPosition(
            in MainForceFormationAnchor anchor,
            int formationIndex,
            float3 anchorForward,
            float3 anchorRight)
        {
            int columns = math.max(1, anchor.ColumnCount);
            int index = math.max(0, formationIndex);

            int column = index % columns;
            int row = index / columns;

            float centeredColumn = column - ((columns - 1) * 0.5f);

            float3 offset =
                (anchorRight * (centeredColumn * anchor.SpacingSide)) -
                (anchorForward * (row * anchor.SpacingBack));

            return anchor.Position + offset;
        }

        [BurstCompile]
        private static float CalculateCatchUpMultiplier(
            in MainForceFormationAnchor anchor,
            float distanceToSlot)
        {
            float slotCatchUpDistance = math.max(0.01f, anchor.SlotCatchUpDistance);
            float slowDownDistance = math.max(0.01f, anchor.SlowDownDistance);
            float maxCatchUpMultiplier = math.max(1.0f, anchor.MaxCatchUpMultiplier);

            if (distanceToSlot <= slowDownDistance)
            {
                float t = math.saturate(distanceToSlot / slowDownDistance);
                return math.lerp(0.35f, 1.0f, t);
            }

            if (distanceToSlot >= slotCatchUpDistance)
            {
                return maxCatchUpMultiplier;
            }

            float t2 = math.saturate(
                (distanceToSlot - slowDownDistance) /
                math.max(0.01f, slotCatchUpDistance - slowDownDistance));

            return math.lerp(1.0f, maxCatchUpMultiplier, t2);
        }

        [BurstCompile]
        private static bool TryResolveMovement(
            bool hasGrid,
            in GridConfig grid,
            DynamicBuffer<GridCell> gridCells,
            float3 currentPosition,
            float3 toTarget,
            float moveDistance,
            out float3 resolvedNextPosition)
        {
            resolvedNextPosition = currentPosition;

            float targetDistance = math.length(toTarget);
            if (targetDistance <= 0.0001f)
            {
                return false;
            }

            float step = math.min(moveDistance, targetDistance);
            if (step <= 0.0001f)
            {
                return false;
            }

            float3 forward = toTarget / targetDistance;

            if (!hasGrid)
            {
                resolvedNextPosition = currentPosition + (forward * step);
                resolvedNextPosition.y = currentPosition.y;
                return true;
            }

            float3 right = new float3(forward.z, 0.0f, -forward.x);

            float3 forwardCandidate = currentPosition + (forward * step);
            forwardCandidate.y = currentPosition.y;

            if (IsWalkableWorld(grid, gridCells, forwardCandidate))
            {
                resolvedNextPosition = forwardCandidate;
                return true;
            }

            float3 slideRight = math.normalize((forward * 0.35f) + (right * 0.65f));
            float3 slideLeft = math.normalize((forward * 0.35f) - (right * 0.65f));

            float3 rightCandidate = currentPosition + (slideRight * step);
            rightCandidate.y = currentPosition.y;

            if (IsWalkableWorld(grid, gridCells, rightCandidate))
            {
                resolvedNextPosition = rightCandidate;
                return true;
            }

            float3 leftCandidate = currentPosition + (slideLeft * step);
            leftCandidate.y = currentPosition.y;

            if (IsWalkableWorld(grid, gridCells, leftCandidate))
            {
                resolvedNextPosition = leftCandidate;
                return true;
            }

            float shortStep = step * 0.5f;
            if (shortStep > 0.02f)
            {
                float3 shortForwardCandidate = currentPosition + (forward * shortStep);
                shortForwardCandidate.y = currentPosition.y;

                if (IsWalkableWorld(grid, gridCells, shortForwardCandidate))
                {
                    resolvedNextPosition = shortForwardCandidate;
                    return true;
                }

                float3 shortRightCandidate = currentPosition + (slideRight * shortStep);
                shortRightCandidate.y = currentPosition.y;

                if (IsWalkableWorld(grid, gridCells, shortRightCandidate))
                {
                    resolvedNextPosition = shortRightCandidate;
                    return true;
                }

                float3 shortLeftCandidate = currentPosition + (slideLeft * shortStep);
                shortLeftCandidate.y = currentPosition.y;

                if (IsWalkableWorld(grid, gridCells, shortLeftCandidate))
                {
                    resolvedNextPosition = shortLeftCandidate;
                    return true;
                }
            }

            return false;
        }

        [BurstCompile]
        private static bool IsWalkableWorld(
            in GridConfig grid,
            DynamicBuffer<GridCell> gridCells,
            float3 worldPosition)
        {
            int2 cell = GridUtility.ToCell(grid, worldPosition);
            return GridUtility.IsWalkable(grid, gridCells, cell);
        }
    }
}