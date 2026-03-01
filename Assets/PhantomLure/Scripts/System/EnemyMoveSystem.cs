using System.Security.Principal;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PhantomLure.ECS
{
    [BurstCompile]
    [UpdateAfter(typeof(FlowFieldBootstrapSystem))]
    public partial struct EnemyMoveSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FlowFieldGrid>();
            state.RequireForUpdate<FlowFieldRuntime>(); // 遅延実行が完了していること
            state.RequireForUpdate<EnemyTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;

            var gridEntity = SystemAPI.GetSingletonEntity<FlowFieldGrid>();
            var grid = SystemAPI.GetSingleton<FlowFieldGrid>();

            var densityBuf = state.EntityManager.GetBuffer<CellDensity>(gridEntity);
            var dirBuf = state.EntityManager.GetBuffer<CellFlowDir>(gridEntity);

            // エージェント配列化（プロトタイプ用。最適化は後で）
            var query = SystemAPI.QueryBuilder()
                .WithAll<EnemyTag, LocalTransform, EnemyAgent>()
                .Build();

            var agentEntities = query.ToEntityArray(state.WorldUpdateAllocator);
            var agentTransforms = query.ToComponentDataArray<LocalTransform>(state.WorldUpdateAllocator);

            int agentCount = agentEntities.Length;

            // セル→エージェントインデックスのマップ
            int cellCount = grid.GridSize.x * grid.GridSize.y;
            var map = new NativeParallelMultiHashMap<int, int>(agentCount, Allocator.Temp);

            for (int i = 0; i < agentCount; i++)
            {
                int idx = GridUtil.WorldToCellIndex(grid, agentTransforms[i].Position);
                if (idx >= 0)
                {
                    map.Add(idx, i);
                }
            }

            for (int i = 0; i < agentCount; i++)
            {
                Entity e = agentEntities[i];
                var tr = state.EntityManager.GetComponentData<LocalTransform>(e);
                var enemyAgent = state.EntityManager.GetComponentData<EnemyAgent>(e);

                int2 cell = GridUtil.WorldToCell(grid, tr.Position);

                if (!GridUtil.InBounds(grid, cell))
                {
                    continue;
                }

                int cellIdx = GridUtil.ToIndex(grid, cell);

                float2 flow = dirBuf[cellIdx].Dir;
                float3 desired = new float3(flow.x, 0f, flow.y);

                // 3×3セル（自セル＋近傍8セル）で近接（分離）計算
                float3 sep = float3.zero;
                int neighborCount = 0;

                for (int dz = -1; dz <= 1; dz++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int2 ncell = new int2(cell.x + dx, cell.y + dz);
                        if (!GridUtil.InBounds(grid, ncell))
                        {
                            continue;
                        }

                        int nIdx = GridUtil.ToIndex(grid, ncell);

                        if (map.TryGetFirstValue(nIdx, out int otherI, out var it))
                        {
                            do
                            {
                                if (otherI == i)
                                {
                                    continue;
                                }

                                float3 op = agentTransforms[otherI].Position;
                                float3 d = tr.Position - op;
                                float dist = math.length(d);

                                if (dist > 0.0001f && dist < enemyAgent.SeparationRadius)
                                {
                                    sep += d / (dist * dist + 0.01f);
                                    neighborCount++;
                                }
                            }
                            while (map.TryGetNextValue(out otherI, ref it));
                        }
                    }
                }

                if (neighborCount > 0)
                {
                    sep = math.normalizesafe(sep) * enemyAgent.SeparationWeight;
                }

                float3 moveDir = math.normalizesafe(desired + sep);

                // 密度による速度低下（Fundamental Diagram）
                int count = densityBuf[cellIdx].Count;
                float rho = count; // 近似：セル内個体数
                float speedScale = 1f;

                if (rho > grid.RhoMax)
                {
                    speedScale = math.clamp(1f - (rho / math.max(0.0001f, grid.RhoStop)), 0f, 1f);
                }

                if (rho >= grid.RhoStop)
                {
                    speedScale *= 0.1f;
                }

                float speed = enemyAgent.BaseSpeed * speedScale;

                tr.Position += moveDir * speed * dt;

                if (math.lengthsq(moveDir) > 1e-4f)
                {
                    tr.Rotation = quaternion.LookRotationSafe(moveDir, math.up());
                }

                state.EntityManager.SetComponentData(e, tr);
            }

            map.Dispose();
        }
    }
}
