// WFModuleEvent.cs — 原版 `AnimFXModuleEvent` 的对应物
//   **1 实例**（`VanguardIdleEffect`，`eventsToFire[0].whereToFire = 5 (Exit)`）
//
// 语义出处：`资料/AnimFX_18类方法体_块2.md` **§4**（3 个方法体：`.ctor` 空 / `Initialize` / `Exit`；
//   `FireEvent.TryFireEvent` 被内联进这两个里）
//   · 两个方法体**同构**：遍历 `eventsToFire(+0x38)`，对每个 `FireEvent`
//     当 `whereToFire(+0x18) == 时机` **且** `eventToFire(+0x10) != null` 时 `UnityEvent.Invoke()`
//   · `Initialize` 匹配 **0**（`ActionStart.Initialize`）、`Exit` 匹配 **5**（`ActionStart.Exit`）
//   · ⚠️ **只有这两个时机有方法体** ⇒ `DoDestroy(10)` / `Manual(15)` 在原版**没有分支**
//     （照原样不实现；真出现那两个值会打一次性警告 —— 见下）
//   · ⚠️ 它读的是**每条自己的 `whereToFire`**，不是基类那个 `actionStart`（本类不读基类那个字段）
//   · 数据里 `whereToFire` **只有 `5` 这一个取值**（出货数据）
//
// ============================================================================
//  ✅ 2026-10-23（特效线 A1 · R25）：**这个 UnityEvent 到底调什么 —— 查清了，也接上了**
// ============================================================================
// 以前这里写的是「`m_Calls` 被 dump 截断 ⇒ 查不到」。那句话**半对半错**：
//   · 对的那半：`数据/游戏数据/animfx_components.json` 里确实是 `"m_Calls": "<深>"`；
//   · 错的那半：**不是原版没有，是我们的 `工具/dump_animfx.py` 深度护栏把它截断了**
//     （`_depth_limit()` 默认 6，只对 `collisionEvent` / `references` 两条路径单独放宽过，
//      `eventToFire` 不在名单里 ⇒ 整块写成 `<深>`，`gen_animfx_modules.py` 见到就跳过）。
//
// 现在从 **prefab 原始 JSON** 逐条读出来了（判据 → `bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/
// MonoBehaviour_-3048259455998257043.json`；全库扫 84 个包 / 66,459 个 MonoBehaviour）：
// `AnimFXModuleEvent` **只有 1 个实例、`m_Calls` 只有 2 条**：
//
//   | # | 目标（相对效果根）              | 原版组件                        | 方法              | 参数 | CallState          |
//   |---|---------------------------------|---------------------------------|-------------------|------|--------------------|
//   | 0 | `""`（= 效果根自己）            | `AnimFXModuleChangeMaterial`    | `ToggleMaterial`  | true | 1 EditorAndRuntime |
//   | 1 | `Vanguard Frame Animated VAT`   | `StoryProgramming.VATGPUPlayer` | `UpdateMaterials` | true | 2 RuntimeOnly      |
//
//   · 两条 `m_Mode = 6`（`PersistentListenerMode.Bool`）+ `m_BoolArgument = 1`，
//     与目标方法签名 `ToggleMaterial(bool)` / `UpdateMaterials(bool)` **逐一吻合**（签名出处：
//     `Warpforge_code/Scripts/Assembly-CSharp/AnimFXModuleChangeMaterial.cs:109` 与
//     `…/Assembly-CSharp-firstpass/StoryProgramming/VATGPUPlayer.cs:63`）。
//   · 数据落盘 → `数据/游戏数据/module_events.json`（`工具/gen_module_events.py` 生成 · **幂等**，
//     跑两遍 md5 相同；`--check` 会比对下面那张表与 json 是否一致）。
//   · 语义上这条链是「**Exit 时把 VAT 分解材质换上、并把 VAT 驱动要写的那份材质重新绑一遍**」：
//     `ToggleMaterial(true)` 走 `useCustomRenderers` 那支 ⇒ `new Material(material)` 换到
//     `Vanguard Frame Animated VAT` 的 MeshRenderer 上，再按 `fade` 播 `_DissolveAmount` 0→1。
//
// **接法（为什么用反射、不用编译期 `AddListener(m.ToggleMaterial)`）**：
//   · 表里记的是**原版类名**（`AnimFXModuleChangeMaterial` / `StoryProgramming.VATGPUPlayer`），
//     我们工程里对应 `WarpforgeVFX.WFModuleChangeMaterial` / `WarpforgeVFX.WarpforgeVatDriver` ——
//     **名字不一样**（映射表在 `gen_module_events.py` 的 `OUR_COMPONENT`，只用于记录/诊断）。
//   · 若在这里直接 `AddListener(m.ToggleMaterial)`，就得把这两种具体组件**硬编码**进这个通用模块；
//     而 `WarpforgeVatDriver` 在 `CardPresentation/` 下 ⇒ 通用模块反过来依赖玩法件，
//     以后任何效果再挂 `AnimFXModuleEvent` 都要改这个文件。
//   · ⇒ 改成「**在原版那个目标节点上按「方法名 + 参数表」找组件**」：
//     `GetMethod(name, new[] { typeof(bool) })`。**0 个就报、多于 1 个也报（认输，不猜）**。
//   · 找到的方法包成 `UnityAction` 塞进 `UnityEvent.AddListener` ⇒ **`Invoke()` 仍然只有一处**
//     （`Fire()` 里那句），调用路径与原版一致；`FiredCount` 的语义也不变。
//   · 限制（如实写，别当没有）：
//     ① 每次触发走一次 `MethodInfo.Invoke`（+ 一次装箱）。本效果一辈子 1 次，可忽略；
//        换 `Delegate.CreateDelegate` 也可以，但它对非 public / 泛型更挑，收益不成比例。
//     ② IL2CPP 裁剪：仅被 `Invoke` 引用的方法**不保证保留**。这两个方法在别处都有**直接调用点**
//        （`WarpforgeEffectBinder` / `WFModuleChangeMaterial` 自身），不会被裁；
//        将来新增行时，若目标方法**只**被本表调到，需要在 `link.xml` 里保住它。
//     ③ 只认 **public 实例方法**；④ 只实现 `Void` / `Bool` 两种参数模式
//        （原版数据里只出现过 `Bool`；别的模式**报错不猜**）。
//
// 🔴 **仍然不猜**：表里查不到的（effect / index / whereToFire 对不上）、目标节点解析不出来的、
//    方法签名对不上的、同名候选 >1 的、`m_CallState == 0 (Off)` 的 —— 一律
//    **打警告 + 计入 `UnresolvedCount`，绝不 Invoke 一个空壳**（空壳 Invoke = 静默失败，本项目红线）。
//
// 我们定的（显式标出）：
//   · 原版数组元素为 null 会走 IL2CPP null 检查**抛异常**（块2 §4：「没有静默跳过」）。
//     我们改成**警告 + 跳过** —— 本项目的红线是「不许静默失败」，不是「必须崩」。
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

namespace WarpforgeVFX
{
    [WFModuleKind("AnimFXModuleEvent")]
    public class WFModuleEvent : WFEffectModule
    {
        /// <summary>原版内嵌类 `FireEvent`。</summary>
        [Serializable]
        public class FireEvent
        {
            [Tooltip("原版 `whereToFire`：到哪个时机触发。原版只实现了 Initialize(0) 与 Exit(5) 两档")]
            public WFActionStart whereToFire = WFActionStart.Initialize;

            [Tooltip("原版 `eventToFire`。**数据装出来的条目这里是 null** —— 到点会按 " +
                     "`module_events.json` 那张表**按原版 `m_Calls` 现接**（见文件头）；" +
                     "手挂的模块直接在 Inspector 里接")]
            public UnityEvent eventToFire = new UnityEvent();

            /// <summary>这条是不是从原版数据装出来的（= 内容不在 `WFModuleDef` 里，要靠表补）。</summary>
            [NonSerialized] public bool fromData;

            /// <summary>这条有没有接上（表里查到并成功挂上至少一个真方法）。</summary>
            [NonSerialized] public bool wired;
        }

        // ==================================================================
        //  原版 `m_Calls` 表（**生成区** —— 由 `工具/gen_module_events.py` 写，
        //  ⛔ 别手改；`python -I gen_module_events.py --check` 会比对本区与 json）
        // ==================================================================

        /// <summary>`PersistentListenerMode` 里我们**实现了**的那几档。
        /// 原版出货数据里只出现过 `Bool`；其余模式在 `TryWireOne` 里**报错不猜**。</summary>
        public enum ArgMode { Void = 0, Bool = 1 }

        /// <summary>`m_Calls[j]` 一条。字段名与 `数据/游戏数据/module_events.json` 一一对应。</summary>
        [Serializable]
        public class TableCall
        {
            /// <summary>效果名（= 原版 prefab 名 = `WarpforgeEffectPlayer.EffectName`）。</summary>
            public string effect;
            /// <summary>`eventsToFire[i]` 里的 i。</summary>
            public int index;
            /// <summary>原版 `whereToFire`（0=Initialize / 5=Exit；10、15 原版无方法体）。</summary>
            public int whereToFire;
            /// <summary>`m_Calls[j]` 里的 j（只用于诊断与稳定排序）。</summary>
            public int callIndex;
            /// <summary>目标节点，**相对效果根**的名字路径（`""` = 根自己）。逐字比，含空格。</summary>
            public string targetPath;
            /// <summary>最后一段在同名兄弟里的下标（`-1` = 不消歧）。</summary>
            public int targetSiblingIndex;
            /// <summary>原版 `m_TargetAssemblyTypeName` 的类名（诊断用，不参与匹配）。</summary>
            public string targetComponent;
            /// <summary>原版 `m_MethodName`。</summary>
            public string methodName;
            /// <summary>原版 `m_Mode`（`PersistentListenerMode`）折算。</summary>
            public ArgMode argMode;
            /// <summary>原版 `m_Arguments.m_BoolArgument`。</summary>
            public bool argBool;
            /// <summary>原版 `m_CallState`：0=Off / 1=EditorAndRuntime / 2=RuntimeOnly（1、2 运行期都会跑）。</summary>
            public int callState;

            public TableCall(string effect, int index, int whereToFire, int callIndex,
                             string targetPath, int targetSiblingIndex, string targetComponent,
                             string methodName, ArgMode argMode, bool argBool, int callState)
            {
                this.effect = effect; this.index = index; this.whereToFire = whereToFire;
                this.callIndex = callIndex; this.targetPath = targetPath;
                this.targetSiblingIndex = targetSiblingIndex; this.targetComponent = targetComponent;
                this.methodName = methodName; this.argMode = argMode; this.argBool = argBool;
                this.callState = callState;
            }
        }

        // >>> gen_module_events.py
        static readonly TableCall[] Table =
        {
            new TableCall("VanguardIdleEffect", 0, 5, 0, "", -1, "AnimFXModuleChangeMaterial", "ToggleMaterial", ArgMode.Bool, true, 1),
            new TableCall("VanguardIdleEffect", 0, 5, 1, "Vanguard Frame Animated VAT", 1, "StoryProgramming.VATGPUPlayer", "UpdateMaterials", ArgMode.Bool, true, 2),
        };
        // <<< gen_module_events.py

        /// <summary>表里一共几条（自检用）。</summary>
        public static int TableCount { get { return Table.Length; } }
        /// <summary>表里有没有覆盖某个 (效果, 条目, 时机)（自检用，不碰 Unity 对象）。</summary>
        public static bool TableCovers(string effect, int index, WFActionStart when)
        {
            var nm = BaseEffectName(effect);
            for (int i = 0; i < Table.Length; i++)
            {
                var r = Table[i];
                if (r != null && r.effect == nm && r.index == index && (WFActionStart)r.whereToFire == when)
                    return true;
            }
            return false;
        }

        /// <summary>去掉 Unity `Instantiate` 自动加的后缀 `<c>(Clone)</c>`。
        /// 为什么需要：`WarpforgeEffectPlayer.EffectName` 在**没有走效果库**（`_entry == null`，
        /// 例如直接把 prefab 拖进场景 / 手工 `Instantiate`）时退回 `gameObject.name`，
        /// 而那上面带着 `(Clone)` ⇒ 逐字比会查不到表、白白丢掉这条事件。
        /// ⚠️ 只剥这一个**由引擎加的**后缀，别的名字一律逐字比（⛔ 不做模糊匹配）。</summary>
        public static string BaseEffectName(string n)
        {
            if (string.IsNullOrEmpty(n)) return n;
            const string k = "(Clone)";
            if (n.Length > k.Length && n.EndsWith(k, StringComparison.Ordinal))
                return n.Substring(0, n.Length - k.Length).TrimEnd();
            return n;
        }

        public FireEvent[] eventsToFire = new FireEvent[0];

        // ---- 诊断计数（自检用；全是累计值，`ResetDiagnostics()` 清零）----
        /// <summary>真正 `Invoke()` 过几次。</summary>
        public static int FiredCount;
        /// <summary>因为「表里查不到 / 目标解析不了 / 签名对不上」而丢掉的次数（&gt;0 = 有行为没还原）。</summary>
        public static int UnknownCount;
        /// <summary>从表里**成功挂上**的真方法调用条数（累计）。</summary>
        public static int WiredCount;
        /// <summary>表里点名了、但**没接上**的条数（累计）。</summary>
        public static int UnresolvedCount;
        /// <summary>接上了、但 `Invoke` 时抛了异常的次数。</summary>
        public static int InvokeErrors;

        /// <summary>接不上的具体原因（每条只留前 20 条，防刷屏）。</summary>
        public static readonly List<string> WiringProblems = new List<string>();

        public static void ResetDiagnostics()
        {
            FiredCount = 0; UnknownCount = 0; WiredCount = 0; UnresolvedCount = 0; InvokeErrors = 0;
            WiringProblems.Clear();
        }

        bool _warnedUnknown, _warnedNoBranch;

        public override void Configure(WFModuleDef def)
        {
            base.Configure(def);                  // actionStart（本类不读它，但基类字段要填上）
            eventsToFire = ReadEvents(def);
        }

        public override void Initialize(WarpforgeEffectPlayer controller)
        {
            base.Initialize(controller);
            WarnAboutUnbranchableEntries();
            Fire(WFActionStart.Initialize, "Initialize");
        }

        public override void Exit()
        {
            Fire(WFActionStart.Exit, "Exit");
        }

        /// <summary>给**从数据装出来**的那条补一个 UnityEvent（覆盖表接出来的那一份）。
        /// 调过之后这条就不再算「未还原」，到点会真的 `Invoke()`。</summary>
        public void WireDataEvent(int index, UnityEvent e)
        {
            if (eventsToFire == null || index < 0 || index >= eventsToFire.Length) return;
            if (e == null) return;
            eventsToFire[index].eventToFire = e;
            eventsToFire[index].fromData = false;
            eventsToFire[index].wired = true;
            _warnedUnknown = false;      // 让人还能看到别的条目的未还原警告
        }

        /// <summary>原版 `FireEvent.TryFireEvent` 内联出来的那段。</summary>
        void Fire(WFActionStart when, string from)
        {
            if (eventsToFire == null) return;
            for (int i = 0; i < eventsToFire.Length; i++)
            {
                var e = eventsToFire[i];
                if (e == null)
                {
                    // 原版这里没有保护（会 NRE）；我们警告 + 跳过（见文件头「我们定的」）
                    Debug.LogWarning($"[WarpforgeVFX] 《{EffectName}》的 AnimFXModuleEvent：`eventsToFire[{i}]` 是空的" +
                                     "（原版会在这里抛 NullReferenceException，我们跳过）");
                    continue;
                }
                if (e.whereToFire != when) continue;

                if (e.eventToFire == null)
                {
                    // 数据装出来的条目：按表把原版 `m_Calls` 现接成真的 UnityEvent。
                    // **接不上就走「未还原」那条（警告 + 计数），绝不 Invoke 空壳。**
                    string why = null;
                    bool wiredNow = e.fromData && TryWire(i, when, out why);
                    if (!wiredNow)
                    {
                        UnknownCount++;
                        if (!_warnedUnknown)
                        {
                            _warnedUnknown = true;
                            Debug.LogWarning($"🔴 未还原：《{EffectName}》的 AnimFXModuleEvent 到 `{from}` 时机要触发的" +
                                             " UnityEvent **接不上**" + (why != null ? $"：{why}" : "") +
                                             " ⇒ 这条事件被丢掉。" +
                                             "判据表 = `数据/游戏数据/module_events.json`，" +
                                             "要人工接就调 `WireDataEvent(i, 你的 UnityEvent)`。");
                        }
                        continue;
                    }
                }

                e.eventToFire.Invoke();
                FiredCount++;
            }
        }

        /// <summary>把 `eventsToFire[entryIndex]` 在 `when` 时机该跑的那几条 `m_Calls` 接成一个 UnityEvent。
        /// 成功（至少挂上 1 个真方法）⇒ 写回并返回 true。</summary>
        bool TryWire(int entryIndex, WFActionStart when, out string why)
        {
            why = null;
            string eff = BaseEffectName(EffectName);
            var rows = new List<TableCall>();
            for (int k = 0; k < Table.Length; k++)
            {
                var r = Table[k];
                if (r == null || r.effect != eff || r.index != entryIndex) continue;
                if ((WFActionStart)r.whereToFire != when) continue;
                rows.Add(r);
            }
            if (rows.Count == 0)
            {
                why = $"表里没有《{eff}》`eventsToFire[{entryIndex}]` 在 `{when}` 的动作";
                return false;
            }

            var ev = new UnityEvent();
            int wired = 0;
            for (int k = 0; k < rows.Count; k++)
            {
                string problem;
                UnityAction a = TryWireOne(rows[k], out problem);
                if (a != null) { ev.AddListener(a); wired++; }
                else if (problem != null) Note(problem);
            }

            if (wired == 0) { why = "表里有条目，但一条都没接上（见上面各条警告）"; return false; }

            SetWired(entryIndex, ev);
            WiredCount += wired;
            return true;
        }

        void SetWired(int entryIndex, UnityEvent ev)
        {
            eventsToFire[entryIndex].eventToFire = ev;
            eventsToFire[entryIndex].fromData = false;
            eventsToFire[entryIndex].wired = true;
        }

        /// <summary>接一条 `m_Calls`。接不上返回 null 并给出 `problem`（**不猜、不静默**）。</summary>
        UnityAction TryWireOne(TableCall r, out string problem)
        {
            problem = null;

            if (r.callState == 0)
            {
                problem = $"[{r.effect}#{r.index}.{r.callIndex}] `m_CallState = 0 (Off)` —— 原版编辑期就关掉了，" +
                          $"运行期不会调 ⇒ 不接（{r.targetComponent}.{r.methodName}）";
                return null;
            }
            if (r.argMode != ArgMode.Void && r.argMode != ArgMode.Bool)
            {
                problem = $"[{r.effect}#{r.index}.{r.callIndex}] 参数模式 `{r.argMode}` —— 本类只实现 Void/Bool" +
                          $"（原版出货数据里只出现过 Bool）⇒ 不猜，跳过（{r.targetComponent}.{r.methodName}）";
                return null;
            }

            var root = Controller != null ? Controller.transform : transform;
            string why;
            var target = ResolveTarget(root, r.targetPath, r.targetSiblingIndex, out why);
            if (target == null)
            {
                problem = $"[{r.effect}#{r.index}.{r.callIndex}] 目标节点解析不了：{why}";
                return null;
            }

            // 在**原版那个目标节点**上找「方法名 + 参数表」都对得上的组件。
            // ⚠️ 按方法签名找而不是按类型名找：原版类名（`AnimFXModuleChangeMaterial` /
            //    `StoryProgramming.VATGPUPlayer`）在我们这边叫别的名字，类型名匹配不上。
            var comps = target.GetComponents<MonoBehaviour>();
            Component hit = null;
            MethodInfo hitMethod = null;
            int candidates = 0;
            for (int i = 0; i < comps.Length; i++)
            {
                var c = comps[i];
                if (c == null) continue;
                var mi = FindMethod(c.GetType(), r.methodName, r.argMode);
                if (mi == null) continue;
                candidates++;
                if (hit == null) { hit = c; hitMethod = mi; }
            }
            if (candidates == 0)
            {
                problem = $"[{r.effect}#{r.index}.{r.callIndex}] 目标 `{PathLabel(r.targetPath)}` 上没有 " +
                          $"`{r.methodName}({(r.argMode == ArgMode.Bool ? "bool" : "")})` —— " +
                          $"原版挂在 `{r.targetComponent}` 上，我们这边那个组件要么没装配、要么方法名/签名不一样";
                return null;
            }
            if (candidates > 1)
            {
                problem = $"[{r.effect}#{r.index}.{r.callIndex}] 目标 `{PathLabel(r.targetPath)}` 上有 {candidates} 个组件" +
                          $"都带 `{r.methodName}({(r.argMode == ArgMode.Bool ? "bool" : "")})` —— **认输，不猜**";
                return null;
            }

            // 闭包捕获的 comp / mi 只在「这一次 Fire」里用；但 eventsToFire 会被复用，
            // 目标若中途被销毁就在调用点现查（Unity 的伪 null ⇒ `hit == null` 为真）。
            var capturedComp = hit;
            var capturedMethod = hitMethod;
            bool isBool = r.argMode == ArgMode.Bool;
            bool arg = r.argBool;
            string label = $"{r.effect}#{r.index}.{r.callIndex} {r.targetComponent}.{r.methodName}";
            return () =>
            {
                if (capturedComp == null)
                {
                    InvokeErrors++;
                    Debug.LogWarning($"[WarpforgeVFX] 《{EffectName}》事件 {label}：目标组件已经没了 ⇒ 这次没调成。");
                    return;
                }
                try
                {
                    capturedMethod.Invoke(capturedComp, isBool ? new object[] { arg } : null);
                }
                catch (Exception ex)
                {
                    InvokeErrors++;
                    Debug.LogError($"[WarpforgeVFX] 《{EffectName}》事件 {label} 调 `{capturedMethod.Name}` 抛了异常：{ex}");
                }
            };
        }

        static string PathLabel(string p) { return string.IsNullOrEmpty(p) ? "（效果根）" : p; }

        /// <summary>找「公开实例方法、返回 void、参数表与 `mode` 一致」的那个。
        /// 找不到 / 有多个同签名重载 ⇒ 返回 null（**认输不猜**）。</summary>
        static MethodInfo FindMethod(Type t, string name, ArgMode mode)
        {
            if (t == null || string.IsNullOrEmpty(name)) return null;
            Type[] args = mode == ArgMode.Void ? Type.EmptyTypes
                        : mode == ArgMode.Bool ? new[] { typeof(bool) }
                        : null;
            if (args == null) return null;
            try
            {
                var mi = t.GetMethod(name, BindingFlags.Public | BindingFlags.Instance, null, args, null);
                if (mi == null) return null;
                if (mi.ReturnType != typeof(void)) return null;     // UnityEvent 只接 void 方法
                return mi;
            }
            catch (AmbiguousMatchException) { return null; }
            catch (Exception) { return null; }
        }

        /// <summary>`targetPath`（相对效果根的名字路径，`""` = 根）→ Transform。
        /// `sibIdx >= 0` 时**最后一段**优先按下标取（同名兄弟消歧）。
        /// 名字**逐字比（含空格）**，与 `WFEffectModule.ResolvePath` 同口径；重名且没给下标 ⇒ 认输。</summary>
        static Transform ResolveTarget(Transform root, string path, int sibIdx, out string why)
        {
            why = null;
            if (root == null) { why = "没有效果根节点（Controller 还没注入？）"; return null; }
            if (string.IsNullOrEmpty(path)) return root;

            var segs = path.Split('/');
            var cur = root;
            for (int s = 0; s < segs.Length; s++)
            {
                string seg = segs[s];
                bool last = s == segs.Length - 1;
                Transform next = null;

                if (last && sibIdx >= 0 && sibIdx < cur.childCount && cur.GetChild(sibIdx).name == seg)
                {
                    next = cur.GetChild(sibIdx);
                }
                else
                {
                    int n = 0;
                    for (int i = 0; i < cur.childCount; i++)
                    {
                        var ch = cur.GetChild(i);
                        if (ch == null || ch.name != seg) continue;
                        next = ch; n++;
                    }
                    if (n > 1) { why = $"`{cur.name}` 下有 {n} 个同名子物体 `{seg}`，没给下标 ⇒ 认输"; return null; }
                }
                if (next == null) { why = $"`{cur.name}` 下找不到子物体 `{seg}`（逐字比，含空格）"; return null; }
                cur = next;
            }
            return cur;
        }

        static void Note(string problem)
        {
            UnresolvedCount++;
            if (WiringProblems.Count < 20) WiringProblems.Add(problem);
            Debug.LogWarning("[WarpforgeVFX] AnimFXModuleEvent 事件动作没接上：" + problem);
        }

        /// <summary>原版**没有** `DoDestroy(10)` / `Manual(15)` 两档的方法体（块2 §4）——
        /// 数据里每条都在这几档时，这条事件永远不会被触发，报一声（不静默）。</summary>
        void WarnAboutUnbranchableEntries()
        {
            if (eventsToFire == null || _warnedNoBranch) return;
            for (int i = 0; i < eventsToFire.Length; i++)
            {
                var e = eventsToFire[i];
                if (e == null) continue;
                if (e.whereToFire == WFActionStart.Initialize || e.whereToFire == WFActionStart.Exit) continue;
                _warnedNoBranch = true;
                Debug.LogWarning($"[WarpforgeVFX] 《{EffectName}》的 AnimFXModuleEvent 有条目 `eventsToFire[{i}].whereToFire=" +
                                 $"{e.whereToFire}` —— 原版**只有 Initialize(0) 与 Exit(5) 两个方法体**（块2 §4）" +
                                 "⇒ 这一档永远不会被触发（我们照原版不实现）。");
                return;
            }
        }

        /// <summary>把 `eventsToFire[i].whereToFire` 装出来。
        /// ⚠️ 只能按 `keys` 里的下标扫，不能用 `GetList("eventsToFire")` ——
        ///    扁平化后的键是 `eventsToFire[0].whereToFire`，**没有 `eventsToFire[0]` 这个键**。
        ///    `eventToFire` 的**内容**不在 `WFModuleDef` 里（那份 dump 被 `<深>` 截断了）⇒
        ///    先置 null，到点由 `TryWire` 按上面的表现接。</summary>
        static FireEvent[] ReadEvents(WFModuleDef def)
        {
            if (def == null || def.keys == null) return new FireEvent[0];
            const string pre = "eventsToFire[";
            var idxs = new SortedSet<int>();
            foreach (var k in def.keys)
            {
                if (string.IsNullOrEmpty(k) || !k.StartsWith(pre, StringComparison.Ordinal)) continue;
                int close = k.IndexOf(']', pre.Length);
                if (close < 0) continue;
                int i;
                if (int.TryParse(k.Substring(pre.Length, close - pre.Length), out i)) idxs.Add(i);
            }
            if (idxs.Count == 0) return new FireEvent[0];

            var arr = new FireEvent[idxs.Count];
            int n = 0;
            foreach (var i in idxs)
            {
                var e = new FireEvent
                {
                    whereToFire = (WFActionStart)def.GetInt(pre + i + "].whereToFire"),
                    fromData = true,
                };
                e.eventToFire = null;          // 内容要等 TryWire 按表现接
                arr[n++] = e;
            }
            return arr;
        }

        // ==================================================================
        //  自检（不需要 Unity 对象，任何宿主一行就能跑）
        // ==================================================================

        /// <summary>表本身的自洽检查：**只查数据结构**，不碰场景。
        /// 返回 `""` = 通过；否则是问题列表（` | ` 分隔）。
        /// ⚠️ 这条**不能**代替「接上了没有」—— 那条要看 `WiredCount` / `UnresolvedCount` /
        ///    `FiredCount`（见 `DescribeCounters()`）。</summary>
        public static string SelfCheck()
        {
            var bad = new List<string>();
            if (Table.Length == 0) bad.Add("表是空的（生成区没写进来？）");
            var seen = new HashSet<string>();
            for (int i = 0; i < Table.Length; i++)
            {
                var r = Table[i];
                if (r == null) { bad.Add($"Table[{i}] 是 null"); continue; }
                if (string.IsNullOrEmpty(r.effect)) bad.Add($"Table[{i}].effect 为空");
                if (string.IsNullOrEmpty(r.methodName)) bad.Add($"Table[{i}].methodName 为空");
                if (r.argMode != ArgMode.Void && r.argMode != ArgMode.Bool)
                    bad.Add($"Table[{i}].argMode = {r.argMode}（只实现 Void/Bool）");
                if (r.callState < 0 || r.callState > 2) bad.Add($"Table[{i}].callState = {r.callState} 越界");
                if ((WFActionStart)r.whereToFire != WFActionStart.Initialize &&
                    (WFActionStart)r.whereToFire != WFActionStart.Exit)
                    bad.Add($"Table[{i}].whereToFire = {r.whereToFire} —— 原版只有 0 / 5 两档有方法体");
                string key = r.effect + "|" + r.index + "|" + r.callIndex;
                if (!seen.Add(key)) bad.Add($"Table[{i}] 重复：{key}");
            }
            return bad.Count == 0 ? "" : string.Join(" | ", bad);
        }

        /// <summary>诊断一行（自检/日志用）：表几条、接上几条、没接上几条、真触发几次。</summary>
        public static string DescribeCounters()
        {
            return $"表 {Table.Length} 条 · 接上 {WiredCount} · 没接上 {UnresolvedCount} · " +
                   $"触发 {FiredCount} 次 · 未还原丢弃 {UnknownCount} 次 · 调用异常 {InvokeErrors} 次";
        }
    }
}
