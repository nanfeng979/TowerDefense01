using System.Threading.Tasks;
using UnityEngine;

namespace TD.Assets
{
    /// <summary>
    /// IAssetProvider：统一的资源加载抽象。当前阶段提供最小实现，后续可切换 Addressables 并加入加载队列与优先级。
    /// </summary>
    public interface IAssetProvider
    {
        Task<GameObject> LoadPrefabAsync(string key);
        void Release(object handleOrKey);
    }
}
