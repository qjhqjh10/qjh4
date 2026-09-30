// EnvironmentConditions.cs — **战场环境（4 环境）的数据入口**（§三 第 30 条 · 本线交付物的那一半）
//
// 数据来源：`Resources/EnvironmentConditions.json` —— 由 `工具/gen_environment_conditions.py`
// 从**原版 SO**（`ScenarioEnvironmentConditionSO`，55 条 = 13 个 Default + 42 个效果）直读得来，
// **判据与逐条证据全在 `数据/游戏数据/environment_conditions.json`**，这里一个判断都不加。
//
// 原版那条链（判据 → `资料/加时与冲突模式_原版规格.md` 的「进攻卡生效时会整场换掉战场环境」那一节）：
//   `GetEnviromentalEffect(卡)` → `AssetReferenceTyped<ScenarioEnvironmentConditionSO>.Load()`
//   → `ScenarioEnvironmentConditionsManager.ApplyEnvironment(so, instant:false)`
//   → `SO.EnableEnvironment(instant)`：**① 补间环境光混合（`blendTime` 秒）② 补间雾 ③ 实例化 `scenarioObjects` prefab**
//   ⚠️ `CurrentEnvironment == so` 就**直接 return（不重播）**；`Awake` 里先按 Default 走一遍。
//
// 🆕 **2026-09-30 晚：那族混合组件已经接了** —— `Battle/ScenarioBlendables.cs`（四个类）
//   ＋ 旁挂 `Resources/EnvBlendables.json`（`工具/gen_env_blendables.py`）
//   ＋ `EnvironmentApplier` 的两半驱动（实例侧 direction=true · 场景侧 direction=`SO.defaultScenarioObjectsState`）。
//   判据（逐句读方法体）→ `资料/加时与冲突模式_原版规格.md` 的 2026-09-30 那一节。
//   **仍然没接的两个**（如实记着，别当成已做）：
//   ① `ScenarioParticleSpawnerBlender`（4 个实例；要原版的 `ParticleSystemAreaSpawner*`，我们工程里没有对应物）
//   ② `ScenarioBlendOptions.FilterOptions` 的**分组**（实测 55 条 SO 里 **54 条是空的**
//      ⇒ 在这一批里本来就是恒等；唯一例外 = GSC 的 `Sump Overspill`）
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    public static class EnvironmentConditions
    {
        [Serializable]
        public class Item
        {
            public string so;                     // SO 名（= `ApplyEnvironment` 要的那一条）
            public string army;                   // 阵营
            public string kind;                   // "default" | "effect"
            public float blendTime;               // 补间时长（原版 `+0x18`）
            public float[] ambientColor;          // `RenderSettings.ambientLight`（原版 `ApplyAmbientColor` 第①件）
            public float ambientBlend;            // `_AmbientColorBlend`（第②件，**补间就是补它**）
            public float[] fogColor;
            public float fogDensity;
            public string scenarioObjects_GUID;   // 原版那个 prefab 的 GUID（**我们按名字取，见下**）
            public string prefabName;             // 归一化后的 prefab 名（`WarpforgeVFX/Prefabs/<它>.prefab`）
            public int defaultScenarioObjectsState;
        }

        [Serializable] class File { public Item[] items; }

        static Dictionary<string, Item> _map;
        static Item[] _all;

        static void Load()
        {
            if (_map != null) return;
            _map = new Dictionary<string, Item>();
            var ta = Resources.Load<TextAsset>("EnvironmentConditions");
            if (ta == null)
            {
                Debug.LogError("[Env] 找不到 `Resources/EnvironmentConditions.json` ⇒ 环境切换做不了。"
                             + "跑一次 `python 工具/gen_environment_conditions.py` 生成。");
                _all = new Item[0];
                return;
            }
            var f = JsonUtility.FromJson<File>(ta.text);
            _all = (f != null && f.items != null) ? f.items : new Item[0];
            foreach (var it in _all)
                if (it != null && !string.IsNullOrEmpty(it.so) && !_map.ContainsKey(it.so))
                    _map[it.so] = it;
        }

        /// <summary>全部条目（含 13 个 Default）。</summary>
        public static Item[] All { get { Load(); return _all; } }

        /// <summary>按 SO 名查一条；查不到返回 null（调用方**要出声**，别静默）。</summary>
        public static Item Find(string soName)
        {
            Load();
            if (string.IsNullOrEmpty(soName)) return null;
            Item it;
            return _map.TryGetValue(soName, out it) ? it : null;
        }

        /// <summary>本条要不要换 prefab（`scenarioObjects` 有值）。空 = 只补间雾/环境光、不加物件。</summary>
        public static bool HasPrefab(Item it)
            => it != null && !string.IsNullOrEmpty(it.prefabName);
    }
}
