// MaterialFader.cs — 原版 `MaterialFader`（MonoBehaviour：4 个序列化字段 + 5 个运行时字段 + 9 个方法）
//
// 判据正本 → `Unity/资料/完全复刻/判据/TraitEffectController_钩子层.md` **§6.6（P2）**
// 账正本 → `Unity/资料/完全复刻/账/特效动画.tsv` **第 237–244 行**（`MaterialFader` 那 8 行）
// 原版一手出处（**逐字读过全文 + 方法体反汇编**）：
//   `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/MaterialFader.cs`（全文）
//   `D:/2/tools/decomp_full/MaterialFader__{Update,Toggle,CacheOriginalMaterials,RestoreOriginalMaterials,
//                                        SetTargetFade,ToggleRenderers,FadeIn,FadeOut}.c`
//   字段偏移（反汇编实读）：`+0x20 renderers` · `+0x28 restoreOriginalMaterialsOnFadeOut` ·
//     `+0x29 restoreOriginalMaterialsOnFadeIN` · `+0x2a disableOnFadeOut` · `+0x30 originalMaterials` ·
//     `+0x38 doFade` · `+0x3c targetFade` · `+0x40 blendTime` · `+0x44 currentBlendTime`
//
// 🔴 **它是不是「死组件」？—— 分清两件事（本轮一手核过）：**
//   · **组件本体不是死的**：全库 **28 个实例**（`grep -rl restoreOriginalMaterialsOnFadeOut */MonoBehaviour/`：
//     `bundle_battleprefabs_vfxandmisc` 17 · `battlearenaleviathan` 4 · `battlesharedresources` 3 ·
//     `tauviorla` / `darkangels` / `aeldari` / `arena3` 各 1）⇒ 它是**通用 prefab 组件**，不是 Markerlight 专用。
//   · **唯一的活驱动者 = `MarkerlightController.AimingAttack`**（`:83` 按 `alreadyAttacked` 选
//     normal/expiring 那一个 → `Toggle(!option, fadeTime)`）；`MarkerlightController` 由
//     `CardScript.cs:11272/11290/11309/11327` 那四处 `GetComponent` 驱动 ⇒ **这条链在运行期是活的**。
//   · 类里的 `FadeIn()` / `FadeOut()` / `SetTargetFade()` / `ToggleRenderers()` 在本 build **零调用**
//     （`grep -rn "\.FadeIn()\|\.FadeOut()"` 命中的都是别的类；`SetTargetFade`/`ToggleRenderers` 是 private 且类内无调用；
//     全库 UnityEvent **没有任何** `m_MethodName: FadeIn|FadeOut|Toggle`）⇒ 这 4 个是**死代码**，按形状保留。
//     ⚠️ 「**调用方是死的**」≠「**组件是死的**」：`Toggle` 与 `Update` 都是活的。
//
// 🔴 **材质必须用 `Renderer.material`（实例副本），⛔ 不是 `sharedMaterial`**（N13 已用 il2cpp dump 决定性证明
//    原版 `Renderer.GetMaterial()` = `renderer.material`；`decomp_full/MaterialFader__Update.c:28` 也正是调
//    `UnityEngine_Renderer__GetMaterial`）。用 `sharedMaterial` 会让**多张卡之间互相写死材质属性**。
//    ⚠️ 唯一的 `sharedMaterial` 用途是**存档/还原**（`CacheOriginalMaterials` 记 sharedMaterial，
//    `RestoreOriginalMaterials` 还 sharedMaterial）—— 这一条原版就是如此（`Toggle.c:30-31` / `Update.c` 的还原支）。
//
// 🔴 **`Renderer.material` vs `sharedMaterial` vs `GetMaterial()` 这套口径，与 `MarkerlightController.Start`
//    里给 `Grid floor` 设 `_RippleScale` 用的是同一套**（那边也是 `r.material`）。
//
// ⚠️ **两个 prefab 覆写值（实读，⚠️ 与 ILSpy 的字段默认值不同 —— 铁律 5·c「一个值 ≠ 全部情况」）：**
//   `MarkerlightIdle` 的两个实例（`MonoBehaviour_-4433243589834689967` / `MonoBehaviour_-1453127347361795503`）：
//     `restoreOriginalMaterialsOnFadeOut = **0**`（C# 字段默认是 `true` ⇒ **prefab 把它关掉了**）
//     `restoreOriginalMaterialsOnFadeIN  = 0`
//     `disableOnFadeOut                 = 1`
//   ⇒ 我们是**运行时 `AddComponent`**（没有序列化值可用）⇒ 字段初值按**原版 C# 默认值**（true/false/true），
//     **接线时（W10）必须显式调 `ApplyPrefabFlags(false, false, true)`**，否则「淡出后还原材质」这一步会多出来。
//   两个实例的节点（本批按 Transform 父链解析）：`MarkerlightIdle > Normal State > Fixed` 与
//     `MarkerlightIdle > Expiring State > Fixed`（⚠️ 账第 244 行写的「都在 Expiring State > Fixed」**错了一半**，
//     已在报告里订正）。各自的 `renderers[]`（4 条，实读）：
//       normal  → `Base VFX` / `Ground Effect` / `Ground glow pulse` / `Grid floor`
//       expiring → `Base Vfx` / `Ground Effect` / **`null`（PathID 0）** / `Grid floor`
//
// 🔴 **唯一一处【我们的】改动（原版会抛异常，我们改成跳过 + 出声）**：
//   原版 `Toggle` / `Update` 的循环里，**`renderers[i] == null` 会走到抛异常的分支**
//   （`Toggle.c:30-32`（cache 循环）、`Toggle.c:73-74`、`Update.c:28`、`Update.c:59-60` → 都 `goto` 到
//    `FUN_1803f47a0()` = **NullReferenceException** 的抛出桩）。
//   而**原版 expiring fader 的 `renderers[2]` 就是 null**（上面实读）⇒ 照抄会在切到 expiring 那一支时崩掉
//   整条攻击链。⇒ 我们**跳过 null 条目并 `LogError` 一次**（铁律「不许静默失败」；⛔ 不静默、也⛔ 不崩）。
//   ⚠️ 这条差异已记进报告，**若要严格照抄（改成抛）请由调度台拍板**。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>
    /// 「把一组 renderer 的材质 α 在 `time` 秒内淡到 0 / 1，并按开关收尾」的通用组件。
    /// ⚠️ 原版在**全局命名空间**（`out_cpp2il_dev_ilspy/MaterialFader.cs:7`）；我们按本目录惯例放进 `CardPresentation`
    /// （与 `TraitEffectController` / `TraitFrames` 一致）—— ⛔ 这不改变类型，只是命名空间归属。
    /// <para>输出行为（判据 §6.6 / 账 237–244）：逐帧 `Mathf.MoveTowards(material.color.a, targetFade, dt/blendTime)`；
    /// 到期后按两个 `restoreOriginal*` 开关还原材质、按 `disableOnFadeOut` 关渲染器。</para>
    /// <para>⛔ 它不改引擎状态、不发事件（和 `TraitEffectController` 一样，纯表现层）。</para>
    /// </summary>
    public class MaterialFader : MonoBehaviour
    {
        // ── 原版 4 个**序列化**字段（名字逐字对应；prefab JSON 里也只有这 4 个）────────────
        /// <summary>原版 `+0x20`。要淡的那一组 renderer（`MarkerlightIdle` 的两个实例各 4 条，见类头）。</summary>
        [SerializeField] Renderer[] renderers;
        /// <summary>原版 `+0x28`，C# 默认 **true** —— ⚠️ `MarkerlightIdle` 两个实例覆写成 **false**（见类头）。</summary>
        [SerializeField] bool restoreOriginalMaterialsOnFadeOut = true;
        /// <summary>原版 `+0x29`，默认 false。</summary>
        [SerializeField] bool restoreOriginalMaterialsOnFadeIN;
        /// <summary>原版 `+0x2a`，默认 true（`MarkerlightIdle` 两个实例也是 1）。</summary>
        [SerializeField] bool disableOnFadeOut = true;

        // ── 原版 5 个**运行时**字段（prefab JSON 里没有 ⇒ 没序列化）─────────────────────────
        /// <summary>原版 `+0x30`：第一次 `Toggle` 时把 `renderers[i].sharedMaterial` 记下来（还原用）。</summary>
        Material[] originalMaterials;
        /// <summary>原版 `+0x38`。⚠️ `Toggle` 里是 **取反**（`doFade = !doFade`），不是置 true。</summary>
        bool doFade;
        /// <summary>原版 `+0x3c`：1 = 淡入到不透明，0 = 淡出到全透明。</summary>
        float targetFade;
        /// <summary>原版 `+0x40`：本次淡入淡出的总时长（`Update` 拿它当分母）。</summary>
        float blendTime;
        /// <summary>原版 `+0x44`：剩余时长；`<= 0` 的那一帧做收尾。</summary>
        float currentBlendTime;

        // ── 观测量（**我们的**，判据 §6.5 同款口径；只读，给自检用）───────────────────────
        public int ToggleCalls { get; private set; }
        /// <summary>`Update` 里**真的走了淡变逻辑**（`doFade == true`）的帧数。</summary>
        public int UpdateCalls { get; private set; }
        public int RestoreCalls { get; private set; }
        public int BoundCount { get { return renderers != null ? renderers.Length : 0; } }
        public int OriginalMaterialCount { get { return originalMaterials != null ? originalMaterials.Length : 0; } }
        public float TargetFadeForTest { get { return targetFade; } }
        public float BlendTimeForTest { get { return blendTime; } }
        public float CurrentBlendTimeForTest { get { return currentBlendTime; } }
        public bool DoFadeForTest { get { return doFade; } }
        public Renderer[] RenderersForTest { get { return renderers; } }

        /// <summary>⚠️ 只出声一次（`renderers` 没绑时每帧 `Update` 会刷屏）。</summary>
        bool _warnedNoRenderers;

        // ── 绑定（**我们的**：原版靠 prefab 序列化，我们运行时 AddComponent ⇒ 必须显式绑）────────
        /// <summary>
        /// 绑 renderer 数组。⚠️ **不自动按子件绑**：原版 `renderers[]` 是**精确的 4 条**
        /// （见类头；含诱饵外的精确集合），按 `GetComponentsInChildren` 自动绑会**多绑**
        /// （比如把 `Base Vfx` 之类也吃进来）⇒ 宁可显式传。
        /// 绑的同时**清掉材质缓存**（下一次 `Toggle` 重新记 `sharedMaterial`）。
        /// </summary>
        public void BindRenderers(Renderer[] rs)
        {
            renderers = rs;
            originalMaterials = null;
            _warnedNoRenderers = false;
        }

        /// <summary>
        /// 设三个开关 —— 因为我们**没有 prefab 序列化值**可用（运行时 `AddComponent`）。
        /// 🔴 **W10 接线 `MarkerlightIdle` 时必须调** `ApplyPrefabFlags(false, false, true)`
        /// （实读的两个实例序列化值，见类头）。
        /// </summary>
        public void ApplyPrefabFlags(bool restoreOnFadeOut, bool restoreOnFadeIn, bool disableOnFadeOut_)
        {
            restoreOriginalMaterialsOnFadeOut = restoreOnFadeOut;
            restoreOriginalMaterialsOnFadeIN = restoreOnFadeIn;
            disableOnFadeOut = disableOnFadeOut_;
        }

        // ════════════════════════════════════════════════════════════════════════════
        // 原版方法体（顺序与 `MaterialFader.cs` 一致）
        // ════════════════════════════════════════════════════════════════════════════

        /// <summary>原版 `MaterialFader.cs:26` / `MaterialFader__Update.c`：
        /// `if (!doFade) return;` → 逐 renderer `MoveTowards(material.color.a, targetFade, dt / blendTime)` →
        /// `currentBlendTime <= 0` 那一帧按开关**还原材质** / **关渲染器**、`doFade = false` →
        /// 最后 `currentBlendTime -= Time.deltaTime`（**每帧都减**，包括收尾那一帧 —— `Update.c:36/65-68`）。</summary>
        void Update()
        {
            if (!doFade) return;
            UpdateCalls++;

            float dt = Time.deltaTime;
            // ⚠️ 原版是裸的 `dt / blendTime`。`blendTime == 0` 时它是 ±Infinity（= 当帧到位）；
            //    而 `dt == 0`（暂停/首帧）时是 0/0 = **NaN** ⇒ α 变 NaN（静默画坏）。
            //    我们写成显式分支：`blendTime <= 0` ⇒ 当帧到位（等价于原版 Infinity 的**结果**，且不出 NaN）。
            float stepAlpha = blendTime > 0f ? dt / blendTime : 1f;

            if (renderers != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer r = renderers[i];
                    if (r == null) { WarnNullRenderer(i); continue; }   // ⚠️ 原版这里抛 NRE（见类头）
                    Material m = r.material;                            // 原版 `GetMaterial()` = `renderer.material`
                    if (m == null) continue;
                    Color c = m.color;
                    c.a = Mathf.MoveTowards(c.a, targetFade, stepAlpha);
                    m.color = c;
                }
            }

            if (currentBlendTime <= 0f)
            {
                bool restoreOut = restoreOriginalMaterialsOnFadeOut && targetFade == 0f;
                bool restoreIn = restoreOriginalMaterialsOnFadeIN && targetFade == 1f;
                if (restoreOut || restoreIn) RestoreOriginalMaterials();   // 原版 `Update.c:37-39/49-48`

                if (disableOnFadeOut && targetFade == 0f && renderers != null)
                {
                    for (int i = 0; i < renderers.Length; i++)
                        if (renderers[i] != null) renderers[i].enabled = false;   // 原版 `Update.c:50-62`
                }
                doFade = false;                                                // 原版 `Update.c:64`
            }

            currentBlendTime -= dt;                                            // 原版 `Update.c:66-68`
        }

        /// <summary>原版 `:143`（private）—— **本 build 零调用**（全库无 UnityEvent 指向它），按形状保留。</summary>
        void FadeOut() { Toggle(false, 1f); }

        /// <summary>原版 `:148`（private）—— **本 build 零调用**，按形状保留。</summary>
        void FadeIn() { Toggle(true, 1f); }

        /// <summary>原版 `:153` / `MaterialFader__Toggle.c` 逐句：
        /// ① `originalMaterials == null` ⇒ 先 `CacheOriginalMaterials()`；
        /// ② 逐 renderer `enabled = true`（**无条件**，不管 option）；
        /// ③ `blendTime = currentBlendTime = time`；
        /// ④ `targetFade = option ? 1 : 0`；
        /// ⑤ **`doFade = !doFade`**（`Toggle.c:60/64` 是 `doFade == 0` 的比较 ⇒ 取反；⛔ **不是**置 true）。
        /// ⚠️ ⑤ 这个取反是原版就有的怪癖：**同一帧内连调两次 `Toggle` ⇒ 第二次把淡变取消**
        ///    （`ScenarioMaterialFader` 那边刻意没抄，等实测；这里按原版照抄 ——
        ///    本组件的两个调用点「攻击开始 / 攻击结束」隔着一整次攻击，收尾时 `Update` 早已把 `doFade` 置 false）。
        /// </summary>
        public void Toggle(bool option, float time)
        {
            ToggleCalls++;

            if (renderers == null || renderers.Length == 0)
            {
                if (!_warnedNoRenderers)
                {
                    _warnedNoRenderers = true;
                    Debug.LogError("[MaterialFader] `renderers` 没绑（原版靠 prefab 序列化；我们运行时 AddComponent ⇒ "
                                 + "接线时必须显式 `BindRenderers(...)`）。本次 `Toggle` **不生效**。"
                                 + "判据 → 资料/完全复刻/判据/TraitEffectController_钩子层.md §6.6");
                }
                return;
            }

            if (originalMaterials == null) CacheOriginalMaterials();   // 原版 `Toggle.c:11-48`

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null) { WarnNullRenderer(i); continue; }      // ⚠️ 原版这里抛 NRE（见类头）
                r.enabled = true;                                      // 原版 `Toggle.c:73-75`
            }

            blendTime = time;                 // 原版 `Toggle.c:56`
            currentBlendTime = time;          // 原版 `Toggle.c:57`
            targetFade = option ? 1f : 0f;    // 原版 `Toggle.c:58-65`（0x3f800000 = 1f）
            doFade = !doFade;                 // 原版 `Toggle.c:60/64`：`doFade = (doFade == 0)`
        }

        /// <summary>原版 `:317`（private）—— **本 build 零调用 = 死代码**（`Toggle` 里内联了同样三句）。
        /// 按形状保留，⛔ 别当接线点。</summary>
        void SetTargetFade(float targetValue, float blendTime)
        {
            this.blendTime = blendTime;
            currentBlendTime = blendTime;
            targetFade = targetValue;
        }

        /// <summary>原版 `:324` / `Toggle.c:11-48`：`originalMaterials = new Material[renderers.Length];
        /// originalMaterials[i] = renderers[i].sharedMaterial;`（**shared**，不是实例 —— 它记的是「原来用哪份材质」）。</summary>
        void CacheOriginalMaterials()
        {
            if (renderers == null) { originalMaterials = null; return; }   // 原版这里抛 NRE
            originalMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                originalMaterials[i] = renderers[i] != null ? renderers[i].sharedMaterial : null;
        }

        /// <summary>原版 `:422` / `Update.c:70-88`：逐 renderer
        /// `Destroy(renderer.material)` 然后 `SetMaterial(originalMaterials[i])`。
        /// ⚠️ `Destroy` 在**批处理/编辑模式不生效** ⇒ 我们按本工程惯例（`Battle/ScenarioMaterialFader.cs`）
        ///    分 `Application.isPlaying ? Destroy : DestroyImmediate`（**我们的**写法，行为等价）。
        /// </summary>
        void RestoreOriginalMaterials()
        {
            RestoreCalls++;
            if (renderers == null || originalMaterials == null) return;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null) { WarnNullRenderer(i); continue; }      // ⚠️ 原版这里抛 NRE（见类头）
                Material inst = r.material;                            // 原版 `GetMaterial()`
                if (inst != null)
                {
                    if (Application.isPlaying) Destroy(inst);
                    else DestroyImmediate(inst);
                }
                if (i < originalMaterials.Length) r.sharedMaterial = originalMaterials[i];   // 原版 `SetMaterial(...)`
            }
        }

        /// <summary>原版 `:459`（private）—— **本 build 零调用 = 死代码**，按形状保留。</summary>
        void ToggleRenderers(bool option)
        {
            if (renderers == null) return;
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].enabled = option;
        }

        /// <summary>⚠️ 原版这里会抛 NRE（见类头「唯一一处我们的改动」）；我们跳过一次、每个下标只出声一次。</summary>
        void WarnNullRenderer(int index)
        {
            if (_nullWarned != null && index < _nullWarned.Length && _nullWarned[index]) return;
            if (_nullWarned == null || _nullWarned.Length != (renderers != null ? renderers.Length : 0))
                _nullWarned = new bool[renderers != null ? renderers.Length : 0];
            if (index < _nullWarned.Length) _nullWarned[index] = true;
            Debug.LogError("[MaterialFader] `renderers[" + index + "]` 是 null —— **跳过**（原版在这里抛 "
                         + "NullReferenceException）。⚠️ 原版 `MarkerlightIdle` 的 expiring 实例 `renderers[2]` "
                         + "**本来就是 null**（PathID 0，实读）⇒ 照抄会崩掉攻击链。已记进报告。");
        }
        bool[] _nullWarned;
    }
}
