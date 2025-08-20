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
        public List<PathConfig> paths;
        public List<BuildSlot> buildSlots;
        public List<WaveConfig> waves;
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
    public class WaveConfig
    {
        public int wave;
        public float startTime;
        public int reward;
        public List<SpawnGroup> groups;
    }

    [Serializable]
    public class SpawnGroup
    {
        public string enemyId;
        public int count;
        public float spawnInterval;
        public float delay; // optional, JsonUtility 默认值 0 即未设置
        public string pathId;
    }
}
