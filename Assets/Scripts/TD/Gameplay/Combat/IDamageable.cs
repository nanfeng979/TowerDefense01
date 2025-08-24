namespace TD.Gameplay.Combat
{
    /// <summary>
    /// 可受伤害接口：由可被攻击的对象实现。
    /// </summary>
    public interface IDamageable
    {
        void Damage(float amount);
    }
}
