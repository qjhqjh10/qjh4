// EnvBlendables.cs — **环境混合组件（blendable）的旁挂数据入口**（§三 第 30 条 · 4 环境）
//
// 数据来源：`Resources/EnvBlendables.json` —— 由 `工具/gen_env_blendables.py` 从**原始 bundle** 直读生成
//   （判据与逐条证据全在 `数据/游戏数据/env_blendables.json`，这里一个判断都不加）。
//
// 原版那条链（判据 → `资料/加时与冲突模式_原版规格.md` 的 2026-09-30 那一节，方法体在
// `d:/2/tools/decomp_full/Scenario*__*.c`）：
//   `ScenarioEnvironmentConditionsManager.ApplyEnvironment(so, instant)`
//     · `CurrentEnvironment == so` ⇒ **直接 return**（不重播）
//     · 否则 `DisableEnvironment(旧)` → 记下新的 → `SO.EnableEnvironment(instant)` → `SetRegisteredBlendeablesState(返回值)`
//   `SO.EnableEnvironment(instant)`：
//     ① 补间 `ambientColor` / 雾（时长 `SO.blendTime`，`instant` 时置 0）
//     ② `scenarioObjects` 有值 ⇒ `Instantiate(prefab, Camera.main.pos/rot)`，然后对它子树里**每一个**
//        `IScenarioEnvironmentBlendeable` 调 `DoScenarioBlend( lVar7 )`——`lVar7` = **direction=true、
//        targetValue=1.0**、时长 = `blendTime`
//     ③ **返回** `lVar8` = **direction = targetValue = `SO.defaultScenarioObjectsState`**
//        ⇒ 管理器拿它去刷**场景里已登记**的那批（= 13 场战场**自己**挂的那些）
//   `SO.DisableEnvironment(instant)`：对**旧实例**的每个 blendable 调
//        `DoScenarioBlend{ duration, direction=false, onComplete=计数回调 }`，
//        **全部回调到齐**（计数归零）才 `Destroy` 那个实例 + `Release` 资产引用。
//
// ⚠️ 我们的落点（与 `EnvironmentApplier` 分工）：本文件**只读数据**，不认识 Unity 对象；
//    「按路径/名字把组件挂到对象上、按方向驱动」在 `Battle/EnvironmentApplier.cs` + `Battle/ScenarioBlendables.cs`。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    public static class EnvBlendables
    {
        /// <summary>**目标组件自己的**序列化字段（`k` = 原版字段名，别改写）。
        /// 为什么单开一组（不复用 `Item.fields` 那套）：那套的 `v` 是 `int`，只装得下 0/1 与枚举，
        /// 而 `ParticleSystemAreaSpawner` 那 6 个字段里有**真小数**（`spawnRate 0.13` / `chances 0.7`）
        /// 与一个 **Vector3**、还有**一条对象引用** ⇒ 这里 `f` = 数值（bool 写 0/1 · int/float 原值）、
        /// `s` = 字符串：**引用型字段** = 落点的层级路径（相对 prefab 根，与 `Target.path` 同一套写法）；
        /// **普通字符串字段**（如 `ScenarioGenericMaterialBlend.filterCode`） = 原值本身。
        /// ⚠️ 只有旁挂**真的记了**的字段才在数组里（查不到 = 该组件没这个字段，别拿 0 当默认值去用）。
        /// 🆕 2026-10-07：这一套现在**三处共用**（`Target.fields` · `Item.floats` · 查找器 `EnvBlendables.GetF/GetS`）。</summary>
        [Serializable]
        public class TargetField { public string k; public float f; public string s; }

        /// <summary>`TargetField[]` 那两套查找的**唯一实现** —— `Target.fields`（目标组件的字段）
        /// 与 `Item.floats`（组件自己的小数字段）**共用这一份**，别在两处各写一遍。</summary>
        public static float GetF(TargetField[] a, string k, float dflt)
        {
            if (a == null) return dflt;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != null && a[i].k == k) return a[i].f;
            return dflt;
        }

        /// <summary>引用型那一条（值 = 落点的层级路径；没记返回空串）。</summary>
        public static string GetS(TargetField[] a, string k)
        {
            if (a == null) return "";
            for (int i = 0; i < a.Length; i++)
                if (a[i] != null && a[i].k == k) return a[i].s ?? "";
            return "";
        }

        /// <summary>一个目标（粒子系统 / 渲染器 / GameObject / 生成器组件 / 耀斑 / Animation / Transform）。
        /// · prefab 侧：`path` = **相对 prefab 根的层级路径**（我们导入的 prefab 保层级 ⇒ 按路径找）
        /// · 场景侧：`leaf` + `pos` = **名字 + 世界位置**（我们的战场是平铺建的、没有父链 ⇒ 同名取最近）
        /// · `fields` = 目标组件自己的序列化字段（见 `TargetField`；空 = 该 kind 不用它）
        /// · 🆕 2026-10-07 波9离线（A135）：`blendProps` / `customMaterial` = 原版
        ///   `ScenarioGenericMaterialBlend.RendererMaterialBlender` 的 `propertiesToBlend` / `customMaterial`。
        ///   ⚠️ 只有 `cls == "ScenarioGenericMaterialBlend"` 的 `kind == "renderer"` 目标才带这两个键
        ///   （其余目标两者都是 null ⇒ `JsonUtility` 不写这个键、也不占体积）。</summary>
        [Serializable]
        public class Target
        {
            public string path;
            public string leaf;
            /// <summary>`"ps"` 粒子 · `"renderer"` 渲染器 · `"go"` GameObject · `"spawner"` 生成器组件 ·
            /// `"controller"` 生成器调度器 · 🆕 `"flare"` `LensFlareComponentSRP` · 🆕 `"animation"` 旧版 `Animation` ·
            /// 🆕 `"transform"` Transform · 🆕 `"lookat"` 原版 `LookAtConstrainWIP`（✅ 2026-10-11 A196 起
            /// **我们工程里有这个类了**：`Battle/LookAtConstrainWIP.cs`；它的字段在 `fields` 里）·
            /// 🆕 `"animfx"` 原版 `AnimFXController`（✅ 同上：`Battle/AnimFXController.cs`）。</summary>
            public string kind;
            public float[] pos;
            public TargetField[] fields;
            /// <summary>原版 `RendererMaterialBlender.propertiesToBlend`（要补间的材质属性名，例 `["_Blend"]`）。
            /// 空 = 这个目标不是「材质补间」那种。</summary>
            public string[] blendProps;
            /// <summary>原版 `RendererMaterialBlender.customMaterial` 那条引用的**资产名**（例
            /// `Battle Arena Genestealers Water Floor`）。⚠️ 那是原版 bundle 里的一个 Material 资产，
            /// **我们工程里没有它**（我们的 arena 材质是按清单 `props` 现建的）⇒ 这里只留档、运行时不换材质
            /// （判据与理由见 `Battle/ScenarioBlendables.cs` 的 `RendererMaterialBlender` 注释）。</summary>
            public string customMaterial;

            /// <summary>取目标组件的一个数值字段（**没记**返回 `dflt`）。</summary>
            public float GetF(string k, float dflt = 0f) { return EnvBlendables.GetF(fields, k, dflt); }
            public bool GetB(string k, bool dflt = false) { return GetF(k, dflt ? 1f : 0f) != 0f; }
            public int GetI(string k, int dflt = 0) { return (int)GetF(k, dflt); }

            /// <summary>取目标组件的一条**引用型**字段（值 = 那条引用落点的层级路径；没记返回空串）。</summary>
            public string GetS(string k) { return EnvBlendables.GetS(fields, k); }
        }

        /// <summary>该类自己的序列化字段（`k` = 原版字段名，`v` = 0/1 或整数）。</summary>
        [Serializable]
        public class Field { public string k; public int v; }

        [Serializable]
        public class Item
        {
            public string cls;          // `ScenarioParticleSystemBlender` / …Toggler / …MaterialFader / …GenericObjectToggler / …ParticleSpawnerBlender / (A136) 另 4 个类
            public string owner;        // 挂它的那个对象（原版数据，留档用）
            public string ownerLeaf;
            public float[] ownerPos;
            public Field[] fields;
            /// <summary>🆕 2026-10-07 波9离线（A136）：**本组件自己**的小数字段，形制与 `Target.fields`
            /// **同一套**（`TargetField`：`k` 原版字段名 · `f` 数值 · `s` 引用落点路径）。
            /// 为什么必须另开一组、不能塞进上面那个 `Field[] fields`：那套的 `v` 是 **int**，
            /// 而这里要装的是 `ScenarioAnimationBlend.blendTime = 0.3` 与
            /// `TauCannonAnimationStopper.finalRotation`（Vector3，拆 x/y/z 三条）—— 塞进去会被截成 `0`
            /// （**静默错**，本仓红线）。空 = 该类没有这类字段（**别拿 0 当默认值去用**）。</summary>
            public TargetField[] floats;
            public Target[] targets;

            /// <summary>取一个原版字段（查不到返回 `dflt`）。⚠️ 字段名照原版，别改写成我们自己的。</summary>
            public int Get(string k, int dflt = 0)
            {
                if (fields == null) return dflt;
                for (int i = 0; i < fields.Length; i++)
                    if (fields[i] != null && fields[i].k == k) return fields[i].v;
                return dflt;
            }
            public bool GetBool(string k, bool dflt = false) => Get(k, dflt ? 1 : 0) != 0;

            /// <summary>取**本组件自己**的一个小数字段（`floats`；没记返回 `dflt`）。</summary>
            public float GetF(string k, float dflt = 0f) { return EnvBlendables.GetF(floats, k, dflt); }
            public bool GetFB(string k, bool dflt = false) { return GetF(k, dflt ? 1f : 0f) != 0f; }
            /// <summary>取**本组件自己**的一条引用型字段（值 = 落点的层级路径；没记返回空串）。</summary>
            public string GetS(string k) { return EnvBlendables.GetS(floats, k); }
        }

        [Serializable]
        public class Group { public string root; public Item[] items; }

        [Serializable] class File { public Group[] prefabs; public Group[] scene; }

        static Dictionary<string, Item[]> _prefabs, _scene;
        static bool _loaded;

        static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            _prefabs = new Dictionary<string, Item[]>();
            _scene = new Dictionary<string, Item[]>();
            var ta = Resources.Load<TextAsset>("EnvBlendables");
            if (ta == null)
            {
                Debug.LogError("[EnvBlend] 找不到 `Resources/EnvBlendables.json` ⇒ 环境混合组件挂不上。"
                             + "跑一次 `python 工具/gen_env_blendables.py` 生成。");
                return;
            }
            var f = JsonUtility.FromJson<File>(ta.text);
            if (f == null) { Debug.LogError("[EnvBlend] `EnvBlendables.json` 解析失败"); return; }
            if (f.prefabs != null)
                foreach (var g in f.prefabs)
                    if (g != null && !string.IsNullOrEmpty(g.root)) _prefabs[g.root] = g.items ?? new Item[0];
            if (f.scene != null)
                foreach (var g in f.scene)
                    if (g != null && !string.IsNullOrEmpty(g.root)) _scene[g.root] = g.items ?? new Item[0];
        }

        /// <summary>某件环境 prefab 上挂的 blendable（按 prefab 根的**原名**查，如
        /// `EnvironmentalCondition Saim Hann Webway Rift`）。查不到 = 空数组（这件本来就没有）。</summary>
        public static Item[] ForPrefab(string prefabRootName)
        {
            Load();
            Item[] v;
            return (prefabRootName != null && _prefabs.TryGetValue(prefabRootName, out v)) ? v : Empty;
        }

        /// <summary>某一场战场**自己**挂的 blendable（键 = `battlearena3` 这种场名）。</summary>
        public static Item[] ForArena(string arenaKey)
        {
            Load();
            Item[] v;
            return (arenaKey != null && _scene.TryGetValue(arenaKey, out v)) ? v : Empty;
        }

        static readonly Item[] Empty = new Item[0];

        /// <summary>两边的总条数（自检用：判「数据到底load进来了没有」）。</summary>
        public static void Counts(out int prefabGroups, out int sceneGroups)
        {
            Load();
            prefabGroups = _prefabs.Count;
            sceneGroups = _scene.Count;
        }
    }
}
