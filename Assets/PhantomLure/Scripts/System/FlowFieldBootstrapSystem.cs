using Unity.Burst;
using Unity.Entities;

namespace PhantomLure.ECS
{
    [BurstCompile]
    public partial struct FlowFieldBootstrapSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FlowFieldGrid>();
            state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var gridEntity = SystemAPI.GetSingletonEntity<FlowFieldGrid>();

            if (state.EntityManager.HasComponent<FlowFieldRuntime>(gridEntity))
            {
                return;
            }

            var grid = SystemAPI.GetSingleton<FlowFieldGrid>();
            int cellCount = grid.GridSize.x * grid.GridSize.y;

            var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // 構造変更はECBで遅延
            ecb.AddComponent(gridEntity, new FlowFieldRuntime
            {
                TargetCellIndex = -1,
                NextRebuildTime = 0f
            });

            ecb.AddBuffer<CellBaseCost>(gridEntity).ResizeUninitialized(cellCount);
            ecb.AddBuffer<CellDensity>(gridEntity).ResizeUninitialized(cellCount);
            ecb.AddBuffer<CellDistance>(gridEntity).ResizeUninitialized(cellCount);
            ecb.AddBuffer<CellFlowDir>(gridEntity).ResizeUninitialized(cellCount);

            // 初期値の書き込みは別Systemに任せる
            ecb.AddComponent(gridEntity, new FlowFieldNeedsInit
            {
                CellCount = cellCount
            });
        }
    }

    /// <summary>バッファ中身の初期化が必要であることを示すフラグ</summary>
    public struct FlowFieldNeedsInit : IComponentData
    {
        public int CellCount;
    }
}
