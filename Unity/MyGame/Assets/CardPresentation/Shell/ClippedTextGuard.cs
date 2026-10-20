// ClippedTextGuard.cs —— 从 `Shell/MenuDraw.cs` 里拆出来的（A1506：MonoBehaviour 的类名【必须】与文件名相同，
// 否则 Unity 建不出 MonoScript ⇒ 存场景时被烘成内联 `!u!115` 存根 ⇒ player 加载即崩）。
// ⚠️ 它本来就是**命名空间顶层**（原来只是恰好写在了 `MenuDraw.cs` 里）⇒ 引用处一律不用改。零行为改动。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>🆕 **2026-10-04（A38②）：让「文字裁切」跟着之后的每一次重排走。**
    ///
    /// 🔴 **缺口是什么**：`MenuDraw.ClipText` 是**建的时候**裁一刀（TMP 逐字夹顶点 + 按同一仿射改 uv）。
    ///    之后任何一次 `Label.SetText` / 改字号 / 自动适配带来的重排，TMP 都会**重算 mesh**
    ///    ⇒ 我们那一刀被抹掉、压在视口边上的字**又画出去了**（**静默**：画面不对、断言也不会响）。
    ///
    /// 🔴 **判据（订的是 TMP 自己的事件，不是猜的）**：TMP 在**每次重排完、并且已经把新 mesh 写进 `Mesh` 之后**
    ///    发一条 `TMPro_EventManager.ON_TEXT_CHANGED(this)`（`Library/PackageCache/com.unity.ugui@…/
    ///    Runtime/TMP/TextMeshPro.cs:5047-5063`，源码原注释就是 *“Event indicating the text has been regenerated.”*）
    ///    ⇒ 我们订阅它、只认自己那一个 TMP，收到就**再裁一刀**。
    ///   ⚠️ **正因为事件是在重排【之后】发的**，重裁拿到的永远是**原始 mesh** ⇒ **不会把软边 alpha 一遍遍乘下去**。
    ///   ⛔ 别改成「`LateUpdate` 里无脑重裁」—— 那正是会越裁越暗的写法（静默、且只在软边区现形）。
    ///   🔴 **2026-10-08（A225-②）就地订正（铁律 5）：上面那两句「不会越裁越暗」的理由已经不必再靠它撑了** ——
    ///     `ClipTmpMesh` 现在写的是**绝对值**（基准 = `TMP_CharacterInfo.vertex_*.color`，
    ///     见 `BaseCornerAlpha`），**同一代 mesh 里裁几刀结果都一样**（幂等）。
    ///     ⇒ 「无脑重裁会变暗」这条隐患**从机制上没了**；下面 `Reclip()` 正是靠它才敢随手调。
    ///     ⛔ 但**别把幂等当许可证**去每帧重裁：那还是白跑一遍逐字循环（没有帧循环的批处理里更没意义）。
    ///
    /// 🔴 **2026-10-12（A435①）：本组件不再存「解析后的快照」，存的是【取状态的两个实参】。**
    ///    裁切状态现在**可以长在视口节点上**（`ViewportClip`）⇒ 光存 `Arm` 那一刻的 `PxRect` 是**取早了**：
    ///      · 那一格里 `MenuDraw.ClipText` 拿到的形参本来就是 `null`（= 「不显式覆盖」）⇒ 框是**当场解析**出来的，
    ///        把它冻成快照 = 从此**只认那一刻的父链**；
    ///      · 于是「保住快照」这条路有两个错法，**都不出声**：节点后挂/后迁 ⇒ **永远不裁**；
    ///        节点挪了/改尺寸 ⇒ **拿旧框裁**（字被切在错误的位置上）。
    ///     ⇒ 现在存 `_clipArg` / `_softArg`（= `ClipText` 的**调用方原样传进来**的那两份）+
    ///       重裁时**从 `_lb` 自己重新 `ViewportClip.Resolve`**（`Resolve` 是纯函数、不返 `null`）。
    ///     ⚠️ **不是为了「跟新框」**：框的来源**照旧只在两处**（显式形参 / 解析结果），本组件不引入第三份；
    ///        `Resolve` 的优先级（显式形参非空 ⇒ 赢、连父链都不走）原样保留 ⇒ 旧路径重裁出来的**逐位还是旧框**。
    ///     ⚠️ 节点态下重裁的结果**只有在节点真的动过**时才与 `Arm` 那一刻不同 —— 那正是要补的那一格。
    ///
    /// ⚠️ **本组件的边界（如实写）**：只覆盖 **TMP 那条后端**（`Label` 正常走的那条）。
    ///    点阵兜底那条**没有事件可订** ⇒ 它仍然只能靠「建完别再改」（那一档只在 TMP 资源缺失时才出现）。
    /// ⚠️ 批处理里没有帧循环 ⇒ **没有重排事件**：自检别拿它自证（要验就在 `Play` 里点一次，或直调 `MenuDraw.ClipTextNow`）。</summary>
    public class ClippedTextGuard : MonoBehaviour
    {
        Label _lb;
        TMPro.TextMeshPro _tmp;
        /// <summary>`ClipText` 的**调用方原样那一份** `clip`（`null` = **不显式覆盖** ⇒ 重裁时解析父链）。
        /// ⛔ 别改回「`Arm` 那一刻解析出来的 `PxRect`」—— 理由 → 类注释（A435①）。</summary>
        PxRect? _clipArg;
        /// <summary>同上，那份软边（解析时由 `Resolve` 决定最终值）。</summary>
        Vector2 _softArg;
        bool _on;

        /// <summary>记下「**按什么取裁切状态**」，等下一次重排重新解析 + 重裁。重复 `Arm` 只更新参数、**不重复订阅**。
        /// 🔴 **参数是 `ClipText` 收到的那两份原样**（`clip` 可空 = 不显式覆盖）；⛔ 别在这里先解析 —— 解析归 `CurClip`。
        /// ⚠️ 本方法**不碰**网格：重裁与建时那一刀走的是同一个 `ClipTextNow`（幂等，见类注释）。</summary>
        public void Arm(Label lb, PxRect? clip, Vector2 softPx)
        {
            _lb = lb; _clipArg = clip; _softArg = softPx;
            _tmp = lb != null ? lb.GetComponentInChildren<TMPro.TextMeshPro>() : null;
            if (_on) return;
            // 🔴 **2026-10-20（W58-S4FIX）**：不再逐守卫直挂 TMP 的 `FastAction`。
            //   成因：`OnTextChanged` 在回调里 `OnDisable()` 自我摘除，而 `FastAction.Call` 是
            //   `node = node.Next` 迭代、`LinkedList.Remove` 会把被摘节点的 `Next` 置 null
            //   ⇒ **只要有一颗 `_lb` 已销毁却仍订阅的守卫，排在它后面的订阅者这一趟全收不到且静默**
            //   （实测：探针守卫排在最后 ⇒ `entry=0`、计数停在 70）。
            //   修法：整类共用**一个**静态派发器 + 一份我们自己的列表；派发时遍历**副本**，
            //   守卫在回调里摘自己只动列表、不动正在遍历的 `FastAction` 链。
            if (!Live.Contains(this)) Live.Add(this);
            EnsureHook();
            _on = true;
        }

        static readonly List<ClippedTextGuard> Live = new List<ClippedTextGuard>();
        static bool _hooked;

        /// <summary>把唯一派发器挂到 TMP 的事件上（只挂一次，**永不在回调里摘**）。</summary>
        static void EnsureHook()
        {
            if (_hooked) return;
            TMPro.TMPro_EventManager.TEXT_CHANGED_EVENT.Add(Dispatch);
            _hooked = true;
        }

        /// <summary>遍历**副本**：回调里 `OnDisable()` 摘自己只改 `Live`，不影响这一趟。</summary>
        static void Dispatch(Object obj)
        {
            var arr = Live.ToArray();
            for (int i = 0; i < arr.Length; i++)
            {
                var g = arr[i];
                if (g == null) continue;
                g.OnTextChanged(obj);
            }
            Live.RemoveAll(g => g == null);
        }

        /// <summary>🔴 **按「现在」的裁切状态**算这一刀该落在哪个框里（= 重新走一遍 `ViewportClip.Resolve`）。
        /// 实参 = `Arm` 记下的那两份（**不是** `Arm` 那一刻解析出来的框）⇒ 显式覆盖仍然覆盖、
        /// 「不显式覆盖」那一档跟**当下的**父链（节点后挂/挪动都跟得上，见类注释 A435①）。
        /// 返回 `false` = 这一刻**没有裁切**（没有显式形参、父链上也没有节点）⇒ 调用方**跳过**，
        /// ⛔ 别拿一个「空矩形」顶上（`ClipTextNow` 的 `PxRect` 是非空的 ⇒ 顶上就等于**把字全裁没**）。</summary>
        bool CurClip(out PxRect clip, out Vector2 soft)
        {
            clip = default(PxRect); soft = default(Vector2);
            if (_lb == null) return false;
            var st = ViewportClip.Resolve(_lb.transform, _clipArg, _softArg, default(Vector4));
            if (!st.RenderClip.HasValue) return false;
            clip = st.RenderClip.Value; soft = st.Softness;
            return true;
        }

        /// <summary>🆕 **2026-10-08（A225-②）**：**按「现在」的位置重裁一刀**（幂等，见类注释的订正）。
        ///
        /// <para>🔴 **为什么光有事件不够**：那一刀要落在**文字真正被画的位置**上，而 TMP 的重排与
        /// 「把文字摆到该在的地方」是**两件事** —— `Label.RefreshBounds()` 会在重排**之后**再挪一次
        /// TMP 子节点。事件驱动的重裁只能发生在 `ForceMeshUpdate()` 里（= **挪之前**）
        /// ⇒ 裁出来的渐隐带整体偏「新旧位置之差」（`资料/待办判据_1007.md` 的 A206 记的就是它），
        /// 实测症状：练习窗卡组格的名字条**一个顶点都没被削**（`MainMenuScene.Run` 那条「卡组名也吃软边」红）。
        /// ⇒ `Label.RefreshBounds()` 的**末句**调本函数（每一条定版面的路都从那儿收口）。</para>
        /// <para>⚠️ 它同时是「重排之后**没收到事件**」那种情况的兜底（快照/订阅出任何岔子都不会静默）。</para></summary>
        public void Reclip()
        {
            if (_lb == null) { OnDisable(); return; }      // 标签先没了 ⇒ 自己下岗（同 `OnTextChanged`）
            // 🔴 A435①：框**当场重新解析**（`CurClip`），⛔ 不是 `Arm` 那一刻冻下来的快照。
            PxRect c; Vector2 s;
            if (!CurClip(out c, out s)) return;            // 这一刻没有裁切 ⇒ 没什么可重裁的
            if (MenuDraw.ClipTextNow(_lb, c, s)) MenuDraw.TextReclipAfterPlace++;
        }

        /// <summary>🆕 2026-10-20（W58-S4FIX）：自检读口 —— 守卫**实际**认的那一颗 TMP。
        /// `_tmp` 在 `Arm` 时解析；若那一刻 TMP 子件还没建好，`OnTextChanged` 会按 `_lb` 重新解析。
        /// ⛔ 生产路径不读它。</summary>
        public TMPro.TextMeshPro TmpForTest
        {
            get { return _tmp != null ? _tmp : (_lb != null ? _lb.GetComponentInChildren<TMPro.TextMeshPro>() : null); }
        }

        /// <summary>摘订阅。⚠️ `DestroyImmediate` 也会走到这里（`MenuDraw.ClearChildren` 批处理下就是它）
        /// —— 忘了摘的话，静态事件表会一直攥着这块内存（而且它已经不在场景里了）。</summary>
        void OnDisable()
        {
            if (!_on) return;
            Live.Remove(this);
            _on = false;
        }

        void OnTextChanged(Object obj)
        {
            if (_lb == null) { OnDisable(); return; }     // 标签先没了 ⇒ 自己下岗
            // 🆕 2026-10-20（W58-S4FIX）：`Arm` 那一刻 TMP 子件可能还没建好（`_tmp == null`）
            // ⇒ 这里按 `_lb` 重新解析一次；仍取不到就保持订阅（点阵后端本来就没有事件）。
            if (_tmp == null) _tmp = _lb.GetComponentInChildren<TMPro.TextMeshPro>();
            if (_tmp == null) return;
            if (obj != _tmp) return;                      // 全局事件：只认自己那一个 TMP
            PxRect c; Vector2 s;
            if (!CurClip(out c, out s)) return;           // 🔴 A435①：同 `Reclip()` —— 框当场重解析
            if (MenuDraw.ClipTextNow(_lb, c, s)) MenuDraw.TextClipReapplied++;
        }
    }
}
