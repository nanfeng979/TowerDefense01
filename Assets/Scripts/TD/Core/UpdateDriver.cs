using System.Collections.Generic;
using UnityEngine;

namespace TD.Core
{
    /// <summary>
    /// 集中驱动接口，避免在多个 MonoBehaviour 中分散 Update。
    /// </summary>
    public class UpdateDriver : MonoBehaviour
    {
        private readonly List<IUpdatable> _updates = new List<IUpdatable>();
        private readonly List<ILateUpdatable> _lateUpdates = new List<ILateUpdatable>();
        private readonly List<IFixedUpdatable> _fixedUpdates = new List<IFixedUpdatable>();

        public void Register(IUpdatable u)
        {
            if (u != null && !_updates.Contains(u)) _updates.Add(u);
        }
        public void Unregister(IUpdatable u) { if (u != null) _updates.Remove(u); }

        public void Register(ILateUpdatable u)
        {
            if (u != null && !_lateUpdates.Contains(u)) _lateUpdates.Add(u);
        }
        public void Unregister(ILateUpdatable u) { if (u != null) _lateUpdates.Remove(u); }

        public void Register(IFixedUpdatable u)
        {
            if (u != null && !_fixedUpdates.Contains(u)) _fixedUpdates.Add(u);
        }
        public void Unregister(IFixedUpdatable u) { if (u != null) _fixedUpdates.Remove(u); }

        private void Update()
        {
            for (int i = 0; i < _updates.Count; i++) _updates[i]?.OnUpdate(Time.deltaTime);
        }
        private void LateUpdate()
        {
            for (int i = 0; i < _lateUpdates.Count; i++) _lateUpdates[i]?.OnLateUpdate(Time.deltaTime);
        }
        private void FixedUpdate()
        {
            for (int i = 0; i < _fixedUpdates.Count; i++) _fixedUpdates[i]?.OnFixedUpdate(Time.fixedDeltaTime);
        }
    }

    public interface IUpdatable { void OnUpdate(float dt); }
    public interface ILateUpdatable { void OnLateUpdate(float dt); }
    public interface IFixedUpdatable { void OnFixedUpdate(float fdt); }
}
