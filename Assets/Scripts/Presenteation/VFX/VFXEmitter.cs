using UnityEngine;
using Cysharp.Threading.Tasks;

namespace VFX{
    public class VFXEmitter : MonoBehaviour 
    {
        public static VFXEmitter Instance { get; private set; }

        [SerializeField]
        private VFXPool _vfxPool;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void Emit(VFXKinds kind, Vector3 position)
        {
            EmitAsync(kind, position).Forget();
        }

        private async UniTask EmitAsync(VFXKinds kind, Vector3 position)
        {
            if (_vfxPool == null) return;

            var pooled = await _vfxPool.GetPooledObject();
            if (pooled is not VFXObject vfxObject)
            {
                pooled.Release();
                return;
            }

            vfxObject.VFXAppear(kind, position);
        }
    }
}