using Unity.Entities;
using UnityEngine;

namespace PhantomLure.ECS
{
    public class ObjectiveAuthoring : MonoBehaviour
    {
        public float Value = 10f; // –Ú“I‰¿’l
    }

    public struct Objective : IComponentData
    {
        public float Value;
    }

    public class ObjectiveBaker : Baker<ObjectiveAuthoring>
    {
        public override void Bake(ObjectiveAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<ObjectiveTag>(e);
            AddComponent(e, new Objective { Value = authoring.Value });
        }
    }
}
