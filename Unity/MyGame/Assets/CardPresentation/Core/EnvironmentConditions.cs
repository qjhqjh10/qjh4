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
//   ✅ 2026-10-06 战-A：`ScenarioParticleSpawnerBlender`（4 个实例）**已经接上** —— 与它要的原版
//      `ParticleSystemAreaSpawner` / `…Controller` / `ParticleSystemPoolable` 一起，见
//      `Battle/ScenarioBlendables.cs` 末尾那一段（那 6 个序列化字段走 `EnvBlendables.Target.fields`）。
//   🔴 **仍然没接的**（如实记着，别当成已做）：
//      ① ✅ **2026-10-07 波9离线已落地**：`ScenarioBlendOptions.FilterOptions` 的**分组**
//         —— 消费方 `ScenarioGenericMaterialBlend` 已复刻（见 `Battle/ScenarioBlendables.cs`），
//         两个字段（`filterCode` / `filterEnabled`）已进本文件与 `Resources/EnvironmentConditions.json`
//         （生成器 `工具/gen_environment_conditions.py` 摊平那一段补写）。判据与那一对实例 →
//         `资料/普查产出_1006/战A_第3_4条.md`「第 4 条」+ `资料/普查产出_1007/波9离线_A136_A135.md`。
//      ② ✅ **同上已收**：原版 `IScenarioEnvironmentBlendeable` 的另 4 个实现类
//         （`FlareScenarioToggler` 6 实例 · `ScenarioAnimationBlend` 2 · `ScenarioGenericMaterialBlend` 1 ·
//         `TauCannonAnimationStopper` 2）已进旁挂（`gen_env_blendables.py` 的 `CLASSES`）与运行时工厂。
//         ⚠️ **其中两个的【数据】还有缺口**（如实记着，不是「已做」）：
//         · `ScenarioAnimationBlend`：原版那两条 clip（`AssetReferenceTyped<AnimationClip>`，按 GUID）
//           ✅ **2026-10-07 更正：原来写「**不在**我们工程里」，实际是「**已经收进包了**」** ——
//           A192 把 `LightAnimationOrbit` / `Dark Angels Void Combat animations` 连同 **GUID 容器别名**
//           打进了 `wf_prefabs_extra.bundle`，运行时 `ScenarioAnimationBlend.AnimationClipByGuid` 按 GUID
//           就能取到原件（判据 → `资料/普查产出_1007/波9_A192_两个clip进包.md`；
//           ⚠️ 那一跳**没在 Unity 里实跑过**，见该报告 §3.2）。
//           🔴 **但那两条【暂时都还不会真播】**，原因不在取不到：`LightAnimationOrbit` 是**原版自己**
//           那条 `filterCode` 差一个 `al`（`LightAnimationOrbital`）⇒ 照抄原版数据 = 原版自己这一对也配不上；
//         · ✅ **2026-10-11（A191）已补**（原来这两句写的是「宿主我们工程里没建」/「类我们工程里没有」——
//           两类**都过期了**）：两个类 A196 已移植；四个宿主对象（`Battle Arena Dark Angels baked` ·
//           `Railgun Turret 1/2` · `Railgun turret` · `Railgun Turret N Target`）A191 已照原版建场
//           （`ArenaBuilder.ApplyGroupNodes` + 旁挂 `<场>_groups.json`），而且
//           **`Animation` 组件也按原版补上了**（`Directional Light` 那颗同样）⇒ 上一句「13 件 arena prefab
//           里一个 `Animation` 组件都没有」**同样不成立**。
//           ⚠️ 仍按原版照抄的两条**不是缺陷**：`animFXController != null` 那支、以及差一个 `al` 的 filterCode。
//         两处都在上面留了档（`animationsToChange[].clip` = GUID）+ 运行时出声，
//         缺口清单 → `资料/普查产出_1007/波9离线_A136_A135.md`「没查清的部分」。
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
            /// <summary>🆕 2026-10-07 波9离线（A135）：原版 `SO.filterOptions.FilterCode`（`+0x58` → `+0x10`）。
            /// 空串 = 该 SO **不做 filter 分组**。它唯一的消费方是 `ScenarioGenericMaterialBlend`
            /// （判据 `decomp_full/ScenarioGenericMaterialBlend__DoScenarioBlend.c:19-27`：
            /// 组件自己 `filterCode` 非空时，与这一条**不等就整条 return**）。
            /// 全库 55 条里只有 `EnvironmentalCondition GSC Sump Overspill` 非空（= `"SumpOverspill"`）。</summary>
            public string filterCode;
            /// <summary>原版 `SO.filterOptions.isEnabled`（`+0x58` → `+0x18`）。**命中之后它覆盖那一程的
            /// `direction`**（判据 `…DoScenarioBlend.c:52-53`）—— `1` 会把该组件
            /// `defaultScenarioObjectsState` 的方向**翻过来**。</summary>
            public int filterEnabled;
            /// <summary>🆕 原版 `SO.animationsToChange[]`（`+0x60`）—— **另一套 filter**，
            /// 与 `filterCode` 无关：`ScenarioAnimationBlend.DoScenarioBlend` 拿**组件自己**的 `filterCode`
            /// 去 `AnimationsToChange.TryGetClip(code)` 取 clip 再 `AddClip`/`CrossFade`。
            /// ⚠️ 那两个 clip **是 `AssetReferenceTyped&lt;AnimationClip>`（按 GUID 取）**。
            /// ✅ **2026-10-07 更正**：原来这里写「我们工程里**没有这两个 clip 资产**」—— **不成立了**：
            /// A192 起它们已进 `wf_prefabs_extra.bundle`（并各登记了一条 GUID 容器别名）⇒ 取得到原件
            /// （判据 → `资料/普查产出_1007/波9_A192_两个clip进包.md`）。
            /// 这里这个字段 = **原版 SO 自己那个 GUID**（消费方拿它去按 GUID 取）；
            /// 🔴 **GUID→名字的映射全仓只有一处** = `数据/游戏数据/animator_controllers.json` 的
            /// `clipsByGuid`（打包器与运行时都读它，别在这儿抄第二份）。</summary>
            public AnimToChange[] animationsToChange;
        }

        /// <summary>原版 `ScenarioEnvironmentConditionSO.AnimationsToChange`（TypeDefIndex 747）：
        /// `filterCode`(+0x10) · `clip`(+0x18，`AssetReferenceTyped&lt;AnimationClip>`)。</summary>
        [Serializable]
        public class AnimToChange
        {
            public string filterCode;   // 原版字段名照抄
            public string clip;         // 原版是 AssetReference ⇒ 这里记它的 **assetGUID**
                                        // ⚠️ **2026-10-07 更正**：原来这句尾巴还写着「（我们没导入这个 clip）」——
                                        // **不成立了**：A192 起这两条 clip 已进 `wf_prefabs_extra.bundle`
                                        // （各带一条 GUID 容器别名），运行时 `AnimationClipByGuid` 按 GUID
                                        // 就能取到原件（判据 → `资料/普查产出_1007/波9_A192_两个clip进包.md`）。
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
