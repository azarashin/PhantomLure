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
    [UpdateAfter(typeof(MainForceSlotFollowSystem))]
    public partial struct MainForceSocialForceSystem : ISystem
    {
        private EntityQuery _unitQuery;
        private EntityQuery _gridQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _unitQuery = SystemAPI.QueryBuilder()
                .WithAll<MainForceTag, LocalTransform, MainForceSocialForceAgent>()
                .Build();

            _gridQuery = SystemAPI.QueryBuilder()
                .WithAll<GridConfig, GridCell>()
                .Build();

            state.RequireForUpdate(_unitQuery);
            state.RequireForUpdate(_gridQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            Entity gridEntity = _gridQuery.GetSingletonEntity();
            GridConfig grid = state.EntityManager.GetComponentData<GridConfig>(gridEntity);
            DynamicBuffer<GridCell> gridCells = state.EntityManager.GetBuffer<GridCell>(gridEntity);

            NativeArray<Entity> entities = _unitQuery.ToEntityArray(Allocator.Temp);
            NativeArray<LocalTransform> transforms = _unitQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            NativeArray<MainForceSocialForceAgent> agents = _unitQuery.ToComponentDataArray<MainForceSocialForceAgent>(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                float3 repulsionVelocity = CalculateRepulsionVelocity(i, transforms, agents);
                repulsionVelocity.y = 0.0f;

                if (math.lengthsq(repulsionVelocity) <= 0.000001f)
                {
                    continue;
                }

                LocalTransform transform = transforms[i];
                float3 currentPosition = transform.Position;

                float3 nextPosition = currentPosition + (repulsionVelocity * deltaTime);
                nextPosition.y = currentPosition.y;

                if (!IsWalkableWorld(grid, gridCells, nextPosition))
                {
                    float3 slideVelocity = ResolveWallSlideVelocity(grid, gridCells, currentPosition, repulsionVelocity, deltaTime);

                    if (math.lengthsq(slideVelocity) <= 0.000001f)
                    {
                        continue;
                    }

                    nextPosition = currentPosition + (slideVelocity * deltaTime);
                    nextPosition.y = currentPosition.y;
                    repulsionVelocity = slideVelocity;
                }

                transform.Position = nextPosition;

                float3 flatVelocity = repulsionVelocity;
                flatVelocity.y = 0.0f;

                if (math.lengthsq(flatVelocity) > 0.0001f)
                {
                    quaternion targetRotation = quaternion.LookRotationSafe(math.normalize(flatVelocity), math.up());
                    float rotationT = math.saturate(deltaTime * 12.0f);
                    transform.Rotation = math.slerp(transform.Rotation, targetRotation, rotationT);
                }

                state.EntityManager.SetComponentData(entities[i], transform);
            }

            entities.Dispose();
            transforms.Dispose();
            agents.Dispose();
        }

        [BurstCompile]
        private static float3 CalculateRepulsionVelocity(
            int selfIndex,
            NativeArray<LocalTransform> transforms,
            NativeArray<MainForceSocialForceAgent> agents)
        {
            float3 selfPosition = transforms[selfIndex].Position;
            MainForceSocialForceAgent selfAgent = agents[selfIndex];

            float3 totalForce = float3.zero;

            for (int otherIndex = 0; otherIndex < transforms.Length; otherIndex++)
            {
                if (otherIndex == selfIndex)
                {
                    continue;
                }

                float3 delta = selfPosition - transforms[otherIndex].Position;
                delta.y = 0.0f;

                float distanceSq = math.lengthsq(delta);
                float distance = math.sqrt(math.max(distanceSq, 0.000001f));

                float3 normal;
                if (distanceSq <= 0.000001f)
                {
                    float angle = (selfIndex + 1) * 2.39996323f;
                    normal = new float3(math.cos(angle), 0.0f, math.sin(angle));
                }
                else
                {
                    normal = delta / distance;
                }

                float otherPersonalSpace = agents[otherIndex].PersonalSpaceRadius;
                float combinedRadius = selfAgent.PersonalSpaceRadius + otherPersonalSpace;
                float interactionRadius = math.max(selfAgent.NeighborRadius, combinedRadius);

                if (distance > interactionRadius)
                {
                    continue;
                }

                float distanceFromBoundary = distance - combinedRadius;
                float exponentialTerm = math.exp(-distanceFromBoundary / math.max(0.01f, selfAgent.FalloffDistance));
                float rangeAttenuation = 1.0f - math.saturate(distance / math.max(0.01f, interactionRadius));
                float magnitude = selfAgent.RepulsionStrength * exponentialTerm * math.max(0.05f, rangeAttenuation);

                totalForce += normal * magnitude;
            }

            totalForce.y = 0.0f;

            float maxSpeed = math.max(0.0f, selfAgent.MaxRepulsionSpeed);
            float forceLengthSq = math.lengthsq(totalForce);

            if (maxSpeed > 0.0f && forceLengthSq > (maxSpeed * maxSpeed))
            {
                totalForce = math.normalize(totalForce) * maxSpeed;
            }

            return totalForce;
        }

        [BurstCompile]
        private static float3 ResolveWallSlideVelocity(
            in GridConfig grid,
            DynamicBuffer<GridCell> gridCells,
            float3 currentPosition,
            float3 desiredVelocity,
            float deltaTime)
        {
            float speed = math.length(desiredVelocity);
            if (speed <= 0.0001f || deltaTime <= 0.000001f)
            {
                return float3.zero;
            }

            float3 desiredDirection = desiredVelocity / speed;
            float3 right = new float3(desiredDirection.z, 0.0f, -desiredDirection.x);
            float step = speed * deltaTime;

            float3 slideRightDirection = math.normalize((desiredDirection * 0.2f) + (right * 0.8f));
            float3 slideLeftDirection = math.normalize((desiredDirection * 0.2f) - (right * 0.8f));

            float3 rightCandidate = currentPosition + (slideRightDirection * step);
            rightCandidate.y = currentPosition.y;

            if (IsWalkableWorld(grid, gridCells, rightCandidate))
            {
                return slideRightDirection * speed;
            }

            float3 leftCandidate = currentPosition + (slideLeftDirection * step);
            leftCandidate.y = currentPosition.y;

            if (IsWalkableWorld(grid, gridCells, leftCandidate))
            {
                return slideLeftDirection * speed;
            }

            float shortStep = step * 0.5f;
            if (shortStep > 0.02f)
            {
                float3 shortRightCandidate = currentPosition + (slideRightDirection * shortStep);
                shortRightCandidate.y = currentPosition.y;

                if (IsWalkableWorld(grid, gridCells, shortRightCandidate))
                {
                    return slideRightDirection * speed;
                }

                float3 shortLeftCandidate = currentPosition + (slideLeftDirection * shortStep);
                shortLeftCandidate.y = currentPosition.y;

                if (IsWalkableWorld(grid, gridCells, shortLeftCandidate))
                {
                    return slideLeftDirection * speed;
                }
            }

            return float3.zero;
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