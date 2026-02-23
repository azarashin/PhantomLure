using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PhantomLure.ECS
{
    [BurstCompile]
    public partial struct FlowFieldBuildSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FlowFieldGrid>();
            state.RequireForUpdate<EnemyTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            float time = (float)SystemAPI.Time.ElapsedTime;

            var gridEntity = SystemAPI.GetSingletonEntity<FlowFieldGrid>();
            var grid = SystemAPI.GetSingleton<FlowFieldGrid>();
            if (!state.EntityManager.HasComponent<FlowFieldRuntime>(gridEntity))
            {
                return;
            }
            var runtimeRW = SystemAPI.GetComponentRW<FlowFieldRuntime>(gridEntity);

            if (time < runtimeRW.ValueRO.NextRebuildTime)
            {
                return;
            }

            // 代表1体のターゲット（最小版）
            Entity target = Entity.Null;
            foreach (var dec in SystemAPI.Query<RefRO<EnemyDecision>>().WithAll<EnemyTag>())
            {
                target = dec.ValueRO.CurrentTarget;

                if (target != Entity.Null)
                {
                    break;
                }
            }

            if (target == Entity.Null)
            {
                return;
            }

            if (!state.EntityManager.HasComponent<LocalTransform>(target))
            {
                return;
            }

            float3 tpos = state.EntityManager.GetComponentData<LocalTransform>(target).Position;
            int targetIdx = GridUtil.WorldToCellIndex(grid, tpos);

            if (targetIdx < 0)
            {
                return;
            }

            runtimeRW.ValueRW.TargetCellIndex = targetIdx;
            runtimeRW.ValueRW.NextRebuildTime = time + grid.RebuildIntervalSeconds;

            var baseCost = state.EntityManager.GetBuffer<CellBaseCost>(gridEntity);
            var density = state.EntityManager.GetBuffer<CellDensity>(gridEntity);
            var dist = state.EntityManager.GetBuffer<CellDistance>(gridEntity);
            var dir = state.EntityManager.GetBuffer<CellFlowDir>(gridEntity);

            int cellCount = dist.Length;

            for (int i = 0; i < cellCount; i++)
            {
                dist[i] = new CellDistance { Value = int.MaxValue };
            }

            var q = new NativeQueue<int>(Allocator.Temp);

            dist[targetIdx] = new CellDistance { Value = 0 };
            q.Enqueue(targetIdx);

            // BFS（4近傍）
            while (q.TryDequeue(out int cur))
            {
                int curD = dist[cur].Value;
                int2 c = GridUtil.ToCell(grid, cur);

                Relax(c.x - 1, c.y);
                Relax(c.x + 1, c.y);
                Relax(c.x, c.y - 1);
                Relax(c.x, c.y + 1);

                void Relax(int nx, int ny)
                {
                    int2 n = new int2(nx, ny);

                    if (!GridUtil.InBounds(grid, n))
                    {
                        return;
                    }

                    int ni = GridUtil.ToIndex(grid, n);

                    if (baseCost[ni].Value > 9999f)
                    {
                        return;
                    }

                    int add = 1;
                    int dens = density[ni].Count;

                    if (dens > 0)
                    {
                        add += math.min(5, dens / 4);
                    }

                    int nd = curD + add;

                    if (nd < dist[ni].Value)
                    {
                        dist[ni] = new CellDistance { Value = nd };
                        q.Enqueue(ni);
                    }
                }
            }

            q.Dispose();

            // 方向場：距離が小さい隣へ
            for (int i = 0; i < cellCount; i++)
            {
                if (dist[i].Value == int.MaxValue)
                {
                    dir[i] = new CellFlowDir { Dir = float2.zero };
                    continue;
                }

                int2 c = GridUtil.ToCell(grid, i);

                int best = i;
                int bestD = dist[i].Value;

                Pick(c.x - 1, c.y);
                Pick(c.x + 1, c.y);
                Pick(c.x, c.y - 1);
                Pick(c.x, c.y + 1);

                void Pick(int nx, int ny)
                {
                    int2 n = new int2(nx, ny);

                    if (!GridUtil.InBounds(grid, n))
                    {
                        return;
                    }

                    int ni = GridUtil.ToIndex(grid, n);
                    int nd = dist[ni].Value;

                    if (nd < bestD)
                    {
                        bestD = nd;
                        best = ni;
                    }
                }

                if (best == i)
                {
                    dir[i] = new CellFlowDir { Dir = float2.zero };
                }
                else
                {
                    int2 bc = GridUtil.ToCell(grid, best);
                    float2 v = math.normalizesafe(new float2(bc.x - c.x, bc.y - c.y));
                    dir[i] = new CellFlowDir { Dir = v };
                }
            }
        }
    }
}
