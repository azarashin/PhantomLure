using PhantomLure.ECS;
using Unity.Entities;
using Unity.Mathematics;
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

        [Header("隊列のユニット間の距離")]
        [SerializeField]
        float _spacingX = 1.5f;

        [Header("隊列のユニット間の距離")]
        [SerializeField]
        float _spacingZ = 1.5f;

        [Header("隊列の幅")]
        [SerializeField]
        int _columnCount = 3;

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

            Vector3 cameraForward = _camera.transform.forward;
            cameraForward.y = 0.0f;

            if (cameraForward.sqrMagnitude < 0.0001f)
            {
                cameraForward = Vector3.forward;
            }

            cameraForward.Normalize();

            Entity commandEntity = _entityManager.CreateEntity(typeof(MainForceMoveCommand));
            _entityManager.SetComponentData(commandEntity, new MainForceMoveCommand
            {
                Destination = hit.point,
                Forward = cameraForward,
                StoppingDistance = _stoppingDistance,
                SpacingX = _spacingX,
                SpacingZ = _spacingZ,
                ColumnCount = math.max(1, _columnCount)
            });
        }
    }
}
