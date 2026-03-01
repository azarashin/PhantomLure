using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.Rendering.VirtualTexturing;

namespace PhantomLure.ECS
{
    [BurstCompile]
    public partial struct GridDensitySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FlowFieldGrid>();
            state.RequireForUpdate<EnemyTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var gridEntity = SystemAPI.GetSingletonEntity<FlowFieldGrid>();
            var grid = SystemAPI.GetSingleton<FlowFieldGrid>();

            var density = state.EntityManager.GetBuffer<CellDensity>(gridEntity);

            // クリア
            for (int i = 0; i < density.Length; i++)
            {
                density[i] = new CellDensity { Count = 0 };
            }

            // 集計
            foreach (var tr in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<EnemyTag>())
            {
                int idx = GridUtil.WorldToCellIndex(grid, tr.ValueRO.Position);
                if (idx >= 0)
                {
                    density[idx] = new CellDensity { Count = density[idx].Count + 1 };
                }
            }
        }
    }

    public static class GridUtil
    {
        public static int WorldToCellIndex(in FlowFieldGrid grid, float3 worldPos)
        {
            float3 local = worldPos - grid.Origin;
            int x = (int)math.floor(local.x / grid.CellSize);
            int z = (int)math.floor(local.z / grid.CellSize);
            if (x < 0 || z < 0 || x >= grid.GridSize.x || z >= grid.GridSize.y)
            {
                return -1;
            }
            return x + z * grid.GridSize.x;
        }

        public static int2 WorldToCell(in FlowFieldGrid grid, float3 worldPos)
        {
            float3 local = worldPos - grid.Origin;
            return new int2((int)math.floor(local.x / grid.CellSize), (int)math.floor(local.z / grid.CellSize));
        }

        public static float3 CellCenter(in FlowFieldGrid grid, int2 cell)
        {
            return grid.Origin + new float3((cell.x + 0.5f) * grid.CellSize, 0f, (cell.y + 0.5f) * grid.CellSize);
        }

        public static bool InBounds(in FlowFieldGrid grid, int2 c)
        {
            return (uint)c.x < (uint)grid.GridSize.x && (uint)c.y < (uint)grid.GridSize.y;
        }

        public static int ToIndex(in FlowFieldGrid grid, int2 c) => c.x + c.y * grid.GridSize.x;
        public static int2 ToCell(in FlowFieldGrid grid, int idx) => new int2(idx % grid.GridSize.x, idx / grid.GridSize.x);
    }
}
