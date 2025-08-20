// 仅定义加载接口，具体实现后续加入
using System;
using System.Threading.Tasks;

namespace TD.Config
{
    /// <summary>
    /// 通用 JSON 加载器接口；路径以 StreamingAssets/TD 为根。
    /// </summary>
    public interface IJsonLoader
    {
        Task<T> LoadAsync<T>(string relativePath);
    }
}
