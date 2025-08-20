namespace TD.Common.Pooling
{
    /// <summary>
    /// 可选接口：对象从池中取出/回收时的生命周期回调。
    /// </summary>
    public interface IPoolable
    {
        void OnSpawned();
        void OnDespawned();
    }
}
