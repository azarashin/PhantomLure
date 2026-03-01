using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PhantomLure.ECS
{
    [BurstCompile]
    public partial struct EnemyTargetScoringSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnemyTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            float time = (float)SystemAPI.Time.ElapsedTime;

            // Objective
            var objQuery = SystemAPI.QueryBuilder()
                .WithAll<ObjectiveTag, LocalTransform, Objective>()
                .Build();

            var objEntities = objQuery.ToEntityArray(state.WorldUpdateAllocator);
            var objTransforms = objQuery.ToComponentDataArray<LocalTransform>(state.WorldUpdateAllocator);
            var objData = objQuery.ToComponentDataArray<Objective>(state.WorldUpdateAllocator);

            // Lure
            var lureQuery = SystemAPI.QueryBuilder()
                .WithAll<LureTag, LocalTransform, Lure>()
                .Build();

            var lureEntities = lureQuery.ToEntityArray(state.WorldUpdateAllocator);
            var lureTransforms = lureQuery.ToComponentDataArray<LocalTransform>(state.WorldUpdateAllocator);
            var lureData = lureQuery.ToComponentDataArray<Lure>(state.WorldUpdateAllocator);

            foreach (var (tr, decision, squad, entity) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRW<EnemyDecision>, RefRW<SquadMember>>()
                         .WithAll<EnemyTag>()
                         .WithEntityAccess())
            {
                if (time < decision.ValueRO.NextDecisionTime)
                {
                    continue;
                }

                decision.ValueRW.NextDecisionTime = time + decision.ValueRO.DecisionInterval;

                float3 pos = tr.ValueRO.Position;
                float sight = decision.ValueRO.SightRadius;

                // 重み（調整ポイント）
                const float wValue = 1.0f; // 本物の目標地点に対して増加するスコアの重み
                const float wLure = 1.0f; // 偽の目標地点に対して増加するスコアの重み
                const float wInfo = 1.0f; // 偽の目標地点の信頼度(半減期)に対して増加するスコアの重み
                const float wDist = 0.25f; // 距離が離れていた時に減らされるスコアの重み

                Entity bestTarget = Entity.Null;
                byte bestIsLure = 0;
                float bestScore = float.NegativeInfinity;

                // Objective候補
                for (int i = 0; i < objEntities.Length; i++)
                {
                    float3 tpos = objTransforms[i].Position;
                    float d = math.distance(pos, tpos);

                    if (d > sight)
                    {
                        continue;
                    }

                    float score = 0f;
                    score += wValue * objData[i].Value;
                    score -= wDist * d;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestTarget = objEntities[i];
                        bestIsLure = 0;
                    }
                }

                // Lure候補
                for (int i = 0; i < lureEntities.Length; i++)
                {
                    float3 tpos = lureTransforms[i].Position;
                    float d = math.distance(pos, tpos);

                    if (d > sight)
                    {
                        continue;
                    }

                    float inRadius = 0.25f;
                    if (d <= lureData[i].Radius)
                    {
                        inRadius = 1f;
                    }

                    // 信頼度（半減期）
                    float age = lureData[i].Age;
                    float halfLife = math.max(0.001f, lureData[i].ReliabilityHalfLife);
                    float reliability = math.exp(-math.log(2f) * (age / halfLife));

                    float score = 0f;
                    score += wLure * lureData[i].Attract * inRadius;
                    score += wInfo * reliability * 5f;
                    score -= wDist * d;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestTarget = lureEntities[i];
                        bestIsLure = 1;
                    }
                }

                decision.ValueRW.CurrentTarget = bestTarget;
                decision.ValueRW.TargetIsLure = bestIsLure;

                // 分遣（簡易）
                if (bestIsLure == 1)
                {
                    uint h = (uint)(entity.Index * 1103515245 + (int)(time * 10f) * 12345);
                    float r01 = (h & 0xFFFF) / 65535f; // 疑似乱数

                    if (r01 < decision.ValueRO.DetachmentRatio)
                    {
                        squad.ValueRW.IsDetached = 1;
                    }
                    else
                    {
                        squad.ValueRW.IsDetached = 0;
                    }
                }
                else
                {
                    squad.ValueRW.IsDetached = 0;
                }
            }
        }
    }
}
