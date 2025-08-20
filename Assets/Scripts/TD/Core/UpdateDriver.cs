using System.Collections.Generic;
using UnityEngine;
using TD.Common;

namespace TD.Core
{
    /// <summary>
    /// 统一生命周期驱动器：集中调用所有注册服务的 Update/LateUpdate/FixedUpdate。
    /// 单例 MonoBehaviour，避免多个 Update 回调。
    /// </summary>
    public class UpdateDriver : MonoBehaviour
    {
        private static UpdateDriver _instance;
        public static UpdateDriver Instance => _instance;

        private readonly List<IUpdatable> _updatables = new List<IUpdatable>();
        private readonly List<ILateUpdatable> _lateUpdatables = new List<ILateUpdatable>();
        private readonly List<IFixedUpdatable> _fixedUpdatables = new List<IFixedUpdatable>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 注册 IUpdatable 服务。
        /// </summary>
        public void RegisterUpdatable(IUpdatable updatable)
        {
            if (!_updatables.Contains(updatable))
                _updatables.Add(updatable);
        }

        /// <summary>
        /// 注册 ILateUpdatable 服务。
        /// </summary>
        public void RegisterLateUpdatable(ILateUpdatable lateUpdatable)
        {
            if (!_lateUpdatables.Contains(lateUpdatable))
                _lateUpdatables.Add(lateUpdatable);
        }

        /// <summary>
        /// 注册 IFixedUpdatable 服务。
        /// </summary>
        public void RegisterFixedUpdatable(IFixedUpdatable fixedUpdatable)
        {
            if (!_fixedUpdatables.Contains(fixedUpdatable))
                _fixedUpdatables.Add(fixedUpdatable);
        }

        /// <summary>
        /// 移除服务。
        /// </summary>
        public void Unregister(object service)
        {
            if (service is IUpdatable u) _updatables.Remove(u);
            if (service is ILateUpdatable lu) _lateUpdatables.Remove(lu);
            if (service is IFixedUpdatable fu) _fixedUpdatables.Remove(fu);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _updatables.Count; i++)
            {
                _updatables[i].OnUpdate(dt);
            }
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _lateUpdatables.Count; i++)
            {
                _lateUpdatables[i].OnLateUpdate(dt);
            }
        }

        private void FixedUpdate()
        {
            float fdt = Time.fixedDeltaTime;
            for (int i = 0; i < _fixedUpdatables.Count; i++)
            {
                _fixedUpdatables[i].OnFixedUpdate(fdt);
            }
        }

        /// <summary>
        /// 清空所有注册的服务。
        /// </summary>
        public void ClearAll()
        {
            _updatables.Clear();
            _lateUpdatables.Clear();
            _fixedUpdatables.Clear();
        }
    }
}
