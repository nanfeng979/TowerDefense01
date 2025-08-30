using System.Threading.Tasks;
using UnityEngine;

namespace TD.Assets
{
    /// <summary>
    /// ResourcesAssetProvider：基于 Resources 的最小加载实现（当前阶段占位）。
    /// </summary>
    public class ResourcesAssetProvider : IAssetProvider
    {
        public Task<GameObject> LoadPrefabAsync(string key)
        {
            var prefab = Resources.Load<GameObject>(key);
            return Task.FromResult(prefab);
        }

        public void Release(object handleOrKey)
        {
            // Resources 模式下无需显式释放；保留接口兼容。
        }
    }
}
