using System;
using TD.Gameplay.Enemy;

namespace TD.Core
{
    /// <summary>
    /// 全局游戏事件：用于解耦回合、敌人生成、奖励与符文系统之间的通信。
    /// </summary>
    public static class GameEvents
    {
        public static event Action<int> RoundEnded;             // 参数：round 序号
        public static event Action<int> RoundRewardGranted;     // 参数：reward 金额/积分
        public static event Action<EnemyAgent> EnemySpawned;    // 参数：新生成的敌人

        public static void RaiseRoundEnded(int round) => RoundEnded?.Invoke(round);
        public static void RaiseRoundRewardGranted(int reward) => RoundRewardGranted?.Invoke(reward);
        public static void RaiseEnemySpawned(EnemyAgent agent) => EnemySpawned?.Invoke(agent);
    }
}
