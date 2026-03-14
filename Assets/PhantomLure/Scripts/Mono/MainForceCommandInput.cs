using PhantomLure.ECS;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PhantomLure.Presentation
{
    public class MainForceCommandInput : MonoBehaviour
    {
        [SerializeField]
        Camera _camera;

        [SerializeField]
        LayerMask _groundMask = ~0;

        [SerializeField]
        float _stoppingDistance = 0.1f;

        [SerializeField]
        float _rayDistance = 1000.0f;

        private EntityManager _entityManager;

        private void Start()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (World.DefaultGameObjectInjectionWorld == null)
            {
                Debug.LogError("Default World が見つかりません。");
                enabled = false;
                return;
            }

            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        private void Update()
        {
            if (_camera == null)
            {
                return;
            }

            if (Mouse.current == null)
            {
                return;
            }

            if (!Mouse.current.rightButton.wasPressedThisFrame)
            {
                return;
            }

            Vector2 screenPosition = Mouse.current.position.ReadValue();
            Ray ray = _camera.ScreenPointToRay(screenPosition);

            if (!Physics.Raycast(ray, out RaycastHit hit, _rayDistance, _groundMask))
            {
                return;
            }

            Entity commandEntity = _entityManager.CreateEntity(typeof(MainForceMoveCommand));
            _entityManager.SetComponentData(commandEntity, new MainForceMoveCommand
            {
                Destination = hit.point,
                StoppingDistance = _stoppingDistance
            });


            // for Debug
            Entity debugEntity = _entityManager.CreateEntity(typeof(MainForceCommandDebug));
            _entityManager.SetComponentData(debugEntity, new MainForceCommandDebug
            {
                Position = hit.point,
                LifeTime = 1.0f
            });
        }
    }
}
