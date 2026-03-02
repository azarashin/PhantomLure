using PhantomLure.ECS;
using Unity.Entities;
using UnityEngine;

namespace PhantomLure.ECS.DebugGroup
{
    public partial struct DebugCountSystem : ISystem
    {
        EntityQuery _playerQ, _enemyQ, _sectorQ, _coreQ;

        public void OnCreate(ref SystemState state)
        {
            _playerQ = state.GetEntityQuery(ComponentType.ReadOnly<PlayerTag>());
            _enemyQ = state.GetEntityQuery(ComponentType.ReadOnly<EnemyTag>());
            _sectorQ = state.GetEntityQuery(ComponentType.ReadOnly<SectorTag>());
            _coreQ = state.GetEntityQuery(ComponentType.ReadOnly<CoreTag>());

            state.RequireForUpdate<DebugCountGate>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var pc = _playerQ.CalculateEntityCount();
            var ec = _enemyQ.CalculateEntityCount();
            var sc = _sectorQ.CalculateEntityCount();
            var cc = _coreQ.CalculateEntityCount();


            if ((Time.frameCount % 60) != 0)
            {
                return;
            }

            Debug.Log($"Count P:{pc},E:{ec}, S:{sc}, C:{cc}");

        }
    }
}