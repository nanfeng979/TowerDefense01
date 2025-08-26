// Copyright (c) 2025
// JSON 数据模型（只包含字段，与加载/业务逻辑分离）

using System;
using System.Collections.Generic;
using UnityEngine;

namespace TD.Config
{
    [Serializable]
    public class ElementsConfig
    {
        public int version;
        public List<string> elements;
        public List<ElementMultiplier> multipliers;
        public float @default;
        public float countered;
    }

    [Serializable]
    public class ElementMultiplier
    {
        public string attacker;
        public string defender;
        public float mult;
    }

    [Serializable]
    public class TowersConfig
    {
        public int version;
        public List<TowerDef> towers;
    }

    [Serializable]
    public class TowerDef
    {
        public string id;
        public string name;
        public string element; // metal/wood/water/fire/earth
        public int cost;
        public float range;
        public float fireRate;
        public BulletDef bullet;
        public string targeting; // first/last/closest/strongest
    }

    [Serializable]
    public class BulletDef
    {
        public float speed;
        public float damage;
        public float lifeTime;
    }

    [Serializable]
    public class EnemiesConfig
    {
        public int version;
        public List<EnemyDef> enemies;
    }

    [Serializable]
    public class EnemyDef
    {
        public string id;
        public string name;
        public string element;
        public float hp;
        public float moveSpeed;
        public int bounty;
    }

    [Serializable]
    public class LevelConfig
    {
        public int version;
        public string levelId;
        public string displayName;
        public GridConfig grid;
        public PathConfig path;
        public List<BuildSlot> buildSlots;
        public RoundsContainer rounds; // 回合配置（含全局与每回合列表）
        public int lives;
        public int startMoney;
    }

    [Serializable]
    public class GridConfig
    {
        public float cellSize;
        public int width;
        public int height;
        public bool showGizmos;
        public string gizmoColor; // #RRGGBBAA
    }

    [Serializable]
    public class PathConfig
    {
        public string id;
        public List<Vec3> waypoints;
    }

    [Serializable]
    public class Vec3
    {
        public float x;
        public float y;
        public float z;

        public Vector3 ToVector3() => new Vector3(x, y, z);
    }

    [Serializable]
    public class BuildSlot
    {
        public float x;
        public float y;
        public float z;
        public string type; // ground
    }

    [Serializable]
    public class RoundsContainer
    {
        public RoundsGlobal global;
        public List<RoundConfig> list; // 每回合配置数组
    }

    [Serializable]
    public class RoundsGlobal
    {
        public float spawnInterval; // 敌人与敌人之间默认间隔（秒）
        public float roundInterval; // 回合之间默认间隔（秒）
    }

    [Serializable]
    public class RoundConfig
    {
        public int round;        // 回合序号
        public int reward;       // 该回合奖励
        public List<string> enemies; // 按顺序生成的敌人类型数组，例如 ["grunt","grunt","tank"]
        public float spawnInterval;  // 可选：覆盖本回合的生成间隔（0 表示使用 global.spawnInterval）
    }
}
