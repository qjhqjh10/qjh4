// WFEffectModule.cs — AnimFX 模块的**运行时基类**（原版 `AnimFXModuleBase` 的对应物）
//
// 原版那 18 个类（`AnimFXModule*`）是挂着 prefab 上的 MonoBehaviour，控制特效里每个发射器
// 「什么时候动、动成什么样」。我们的 prefab 从 bundle 导出时**这些组件被当缺失脚本剥掉了**，
// 所以按「**数据 → 运行时装配**」还原（见 `数据/游戏数据/animfx_modules.json`）。
//
// 🔴 形态是 MonoBehaviour，不是纯 C# 对象，原因有两条：
//   ① **自制特效可以手挂**（在编辑器里拖引用、调参数，和原版工作流一样）；
//   ② 需要收 Unity 物理消息的模块（`OnParticleCollision` 之类）只有组件收得到。
//   原版那 958 个走「运行时 `AddComponent` + 按数据填字段」，两者共用同一套类。
//
// 🔴 **谁驱动**：`WarpforgeEffectPlayer`（它是这条线上**唯一的生命周期拥有者**）——
//    不要再写第二个 controller，否则两套销毁计时会互相打架。
//    原版 `AnimFXController` 对**所有模块无条件广播** Initialize / Exit / DoDestroy，
//    **它一处都不读 `actionStart`** —— 要不要响应是**各模块自己读自己那个字段**决定的
//    （实据：`DestroyInTime`、`ChangeMaterial`、`Event.whereToFire`，见
//    `资料/AnimFX_18类方法体_块1.md` §附F）。**别把 actionStart 做成调度开关。**
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WarpforgeVFX
{
    /// <summary>原版 `AnimFXModuleBase.ActionStart`。**它不是调度表**（见文件头）。</summary>
    public enum WFActionStart { Initialize = 0, Exit = 5, DoDestroy = 10, Manual = 15 }

    /// <summary>
    /// 一个模块的**数据**。JsonUtility 解析不了多态，所以这里用「键值两条平行数组」的
    /// 自描述形式（由 `工具/gen_animfx_modules.py` 把原版嵌套字段**拍平**成
    /// `particleSystems[0]` / `collisionAndParticles[0].collisionPlane` 这种点号键）。
    /// 好处：**每个模块类只需要问自己要的字段名**，不用在 python 与 C# 两边各写一份 schema。
    /// </summary>
    [Serializable]
    public class WFModuleDef
    {
        public string kind = "";
        public string[] keys = new string[0];
        public string[] values = new string[0];

        public bool Has(string key) { return IndexOf(key) >= 0; }

        int IndexOf(string key)
        {
            for (int i = 0; i < keys.Length; i++) if (keys[i] == key) return i;
            return -1;
        }

        public string GetString(string key, string dflt = "")
        {
            int i = IndexOf(key);
            return i >= 0 ? values[i] : dflt;
        }

        public float GetFloat(string key, float dflt = 0f)
        {
            int i = IndexOf(key);
            float v;
            if (i >= 0 && float.TryParse(values[i], System.Globalization.NumberStyles.Float,
                                         System.Globalization.CultureInfo.InvariantCulture, out v))
                return v;
            return dflt;
        }

        public int GetInt(string key, int dflt = 0)
        {
            int i = IndexOf(key);
            int v;
            if (i >= 0 && int.TryParse(values[i], out v)) return v;
            return dflt;
        }

        public bool GetBool(string key, bool dflt = false)
        {
            int i = IndexOf(key);
            if (i < 0) return dflt;
            return values[i] == "1" || values[i].Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>有没有以某前缀开头的键。**读列表要用它** ——
        /// 数据里**不存在** `cameraShakes[0]` 这么一个键，只有 `cameraShakes[0].amplitude` 这种，
        /// 所以 `Has("cameraShakes[0]")` 恒为假（踩过：列表全读成空，屏震一次都不播）。</summary>
        public bool HasPrefix(string prefix)
        {
            for (int i = 0; i < keys.Length; i++)
                if (keys[i] != null && keys[i].StartsWith(prefix, StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>有序列表的条数：数 `key[0]`、`key[1]`… 直到缺号。**用它，别用 `Has(key[n])`。**</summary>
        public int CountList(string key)
        {
            int n = 0;
            while (HasPrefix(key + "[" + n + "]")) n++;
            return n;
        }

        /// <summary>取 `key[0]`、`key[1]`… 直到缺号为止（有序列表都这么存）。</summary>
        public List<string> GetList(string key)
        {
            var outp = new List<string>();
            for (int n = 0; n < CountList(key); n++) outp.Add(GetString(key + "[" + n + "]"));
            return outp;
        }

        /// <summary>一个引用值的两种形状（由 `dump_animfx.py` 的 `ptr_ref` 产出）：
        /// · 节点 → `@node:ParticleSystem:Root/Card#0/Glow`（同 prefab 内部，要按路径解析）
        /// · 资产 → `@asset:UnitTweenSO:Impact Light Tween`（外部资产，按类型+名字找）
        /// 解析不出来的写成空串，**由调用方决定报不报**。</summary>
        public static void SplitRef(string v, out string kind, out string type, out string rest)
        {
            kind = type = rest = "";
            if (string.IsNullOrEmpty(v) || v[0] != '@') return;
            var a = v.Split(new[] { ':' }, 3);
            if (a.Length < 3) return;
            kind = a[0].Substring(1);
            type = a[1];
            rest = a[2];
        }
    }

    /// <summary>
    /// 模块基类。**所有模块都必须是 MonoBehaviour**（见文件头），由 `WarpforgeEffectPlayer` 广播。
    /// 覆盖 `Initialize` 时**必须调 `base.Initialize`**（和原版一样，基类要把 controller 记下来）。
    /// </summary>
    public abstract class WFEffectModule : MonoBehaviour
    {
        [Tooltip("原版 `AnimFXModuleBase.actionStart`。⚠️ 它不是调度开关 —— 要不要响应由本模块自己读它。")]
        public WFActionStart actionStart = WFActionStart.Initialize;

        /// <summary>原版 `AnimFXModuleBase.animFXController`。由 player 广播 `Initialize` 时注入。</summary>
        public WarpforgeEffectPlayer Controller { get; private set; }

        /// <summary>`Controller` 还没注入就读了 `EffectName` —— 每个模块只报一次（见下）。</summary>
        bool _warnedNoController;

        /// <summary>本模块属于哪个效果（诊断 / **拼键**用）。
        ///
        /// 🔴 2026-10-23（特效线 A1）：`Controller` 还没注入时**返回空串**，⛔ **不再退回 `name`**。
        ///   `name` 是**本模块所在 GameObject** 的名字（例如 `Glow bottom R` / `Vanguard Frame Animated VAT`），
        ///   **永远不是效果名**。拿它去拼 `<效果名>|<状态名>`（`WFModuleAnimation` 的片段别名）
        ///   会得到一个**取不到但看起来像真的**键 —— 静默失效；空串至少让调用点/日志一眼看得见。
        ///
        /// 什么时候会读到空串：`WFModuleFactory.Create()` 是**先 `Configure(def)`、后**由播放器广播
        /// `Initialize`（`WarpforgeEffectPlayer.BuildModules`）⇒ `Configure` / `Awake` / `OnEnable` /
        /// `OnValidate` 里读到的都是空串（并打一次警告）。**目前所有调用点都在 `Initialize` 之后**
        /// （逐个核过，见 `资料/普查产出_1023/E3_护栏落盘与EffectName收口.md` §②-4）。</summary>
        public string EffectName
        {
            get
            {
                if (Controller != null) return Controller.EffectName;
                if (!_warnedNoController)
                {
                    _warnedNoController = true;
                    Debug.LogWarning($"[WarpforgeVFX] 模块 `{GetType().Name}`（节点 `{name}`）在 `Initialize` 之前" +
                                     "就读了 `EffectName` ⇒ 拿不到效果名（返回**空串**）。" +
                                     "⛔ 别在这个时机拼「效果名 + …」的键 —— `Controller` 要等播放器广播 " +
                                     "`Initialize` 才注入；也⛔ 别退回 `name`（那是节点名，不是效果名）。");
                }
                return "";
            }
        }

        // ---- 生命周期：与控制器一一对应，基类默认都是空实现 ----
        // 🔴 原版 `AnimFXModuleBase.Exit()` / `DoDestroy()` **就是空实现** —— 18 类里只有
        //    **5 个重写 Exit、1 个重写 DoDestroy** ⇒「每个模块都会在 Exit 做事」不成立。

        public virtual void Initialize(WarpforgeEffectPlayer controller) { Controller = controller; }
        public virtual void Exit() { }
        public virtual void DoDestroy() { }

        /// <summary>🆕 2026-10-23（A1 R23）：**这个模块会不会自己接管销毁**。
        ///
        /// 原版 `AnimFXController.DoDestroy()`：`foreach (modules) module.DoDestroy()`；
        /// **若一个模块都没有 → 兜底 `Destroy(gameObject)`；有模块时 controller 不销毁**
        /// （判据 → `资料/AnimFX_18类方法体_块1.md` §2 `DoDestroy` 那一行）。
        /// 18 类里**只有 `AnimFXModuleDoDestroyAnimation` 重写了 `DoDestroy`**，它会
        /// `Destroy(gameObject, timeToDestroy)` ⇒ 只有它该返回 `true`。
        /// ⚠️ 返回 `true` 的模块**必须**真的会在 `ModuleTick` / 延迟销毁里把对象收掉，否则就是泄漏。</summary>
        public virtual bool TakesOverDestroy { get { return false; } }

        /// <summary>每帧。原版各模块自己写 `Update()`；这里收口成控制器广播 ——
        /// 这样 `autoTick=false`（批处理 / 确定性测试）时能整体停，不会漏掉某个模块。</summary>
        public virtual void ModuleTick(float dt) { }

        /// <summary>按数据装配。手挂的模块不走这里（参数在 Inspector 里就是真的）。</summary>
        public virtual void Configure(WFModuleDef def)
        {
            if (def != null) actionStart = (WFActionStart)def.GetInt("actionStart", 0);
        }

        // ---- 路径解析：把 dump 出来的 `Root/Card#0/Glow#1` 落成真实对象 ----
        //
        // 约定（与 `工具/dump_animfx.py` 的 `_path_of` 严格一致）：
        //   段 = `名字` 或 `名字#N`；**N 是它在父节点全部子物体里的下标**（GetSiblingIndex），
        //   为 0 时省略。所以解析就是 `GetChild(N)` 再看名字对不对 —— **不按名字筛**，
        //   名字对不上就返回 null（**宁可认输，不猜**）。

        static int _resolveFailed;
        /// <summary>路径解析失败过的次数（自检用；**失败一定打警告**，不静默）。</summary>
        public static int ResolveFailed { get { return _resolveFailed; } }
        public static void ResetResolveFailed() { _resolveFailed = 0; }

        /// <summary>把路径落成 Transform。失败返回 null（**并打一次警告**）。</summary>
        public static Transform ResolvePath(Transform root, string path, string who = null)
        {
            if (root == null || string.IsNullOrEmpty(path)) return null;
            var segs = path.Split('/');
            int start = 0;
            Transform cur = root;

            // 路径第一段通常是 prefab 根（导出时 `inst.name = src.name`，名字应当对得上）；
            // 对不上就退一步，把整条路径当成「根之下」处理，别直接失败。
            //
            // 🆕 2026-10-22：**第一段按 `Trim()` 后比（只放宽这一段）**。模块数据里的 `__node` 记的是
            // **原版 prefab 名** —— 例如 `"Orbital 5 repeat "` **带尾随空格**（原版 `m_Name` 就这么写的），
            // 而工程里那件 prefab 叫 `"Orbital 5 repeat"`（Windows 文件名落不了尾随空格）⇒
            // 逐字比会在这里就判「根对不上」，路径退化成「根之下」，整条解析不出来
            // （打警告、模块 host 退回根 —— 屏震仍然会挂到根上，但位置/朝向就跟着根走了）。
            // 判据 → `资料/普查产出_1022/P4_M5与audit工具.md`（同一处尾随空格在 join 那一层的实据
            // 见 `EffectLibraryBuilder.JoinKey`）。
            //
            // ⚠️ **只放宽第一段**：后面各段是**子件名**，逐字比是**判据**（`Whip ` / `BulletImpact `
            // 这类**名字带空格的子件**正是靠逐字比才命中）⇒ ⛔ 别把整条路径的语义也改成 Trim。
            var s0 = SplitIndex(segs[0]);
            var n0 = s0.name.Trim();
            if (n0 == root.name.Trim()) start = 1;
            else if (root.parent != null && n0 == root.parent.name.Trim()) { cur = root.parent; start = 1; }

            for (int i = start; i < segs.Length; i++)
            {
                var s = SplitIndex(segs[i]);
                if (s.idx >= cur.childCount)
                {
                    Warn(who, path, $"第 {i} 段 `{segs[i]}` 越界（{cur.name} 只有 {cur.childCount} 个子物体）");
                    return null;
                }
                var next = cur.GetChild(s.idx);
                if (next.name != s.name)
                {
                    Warn(who, path, $"第 {i} 段 `{segs[i]}` 对不上（{cur.name} 的第 {s.idx} 个子物体叫 `{next.name}`）");
                    return null;
                }
                cur = next;
            }
            return cur;
        }

        /// <summary>按 `@node:类型:路径` 解析出一个组件。不是节点引用 / 解析失败 → null（打警告）。</summary>
        public static T ResolveNode<T>(Transform root, string value, string who = null) where T : Component
        {
            string kind, type, rest;
            WFModuleDef.SplitRef(value, out kind, out type, out rest);
            if (kind != "node")
            {
                if (!string.IsNullOrEmpty(value))
                    Warn(who, value, $"这不是节点引用（kind=`{kind}`），用不了 `{typeof(T).Name}`");
                return null;
            }
            var t = ResolvePath(root, rest, who);
            if (t == null) return null;
            var c = t.GetComponent<T>();
            if (c == null) Warn(who, rest, $"那个节点上没有 {typeof(T).Name}");
            return c;
        }

        static void Warn(string who, string path, string why)
        {
            _resolveFailed++;
            Debug.LogWarning($"[WarpforgeVFX] 模块 {who ?? "?"} 的引用解析不了：{why}｜路径 `{path}`");
        }

        static (string name, int idx) SplitIndex(string seg)
        {
            int h = seg.LastIndexOf('#');
            if (h > 0)
            {
                int n;
                if (int.TryParse(seg.Substring(h + 1), out n)) return (seg.Substring(0, h), n);
            }
            return (seg, 0);
        }

        // ---- 给子类用的小工具 ----
        protected IEnumerable<ParticleSystem> AllParticleSystems()
        {
            return GetComponentsInChildren<ParticleSystem>(true);
        }
    }
}
