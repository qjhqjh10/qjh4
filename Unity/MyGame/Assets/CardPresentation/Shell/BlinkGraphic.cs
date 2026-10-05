// BlinkGraphic.cs — 原版 **`BlinkGraphic`**（「会呼吸的提示 / 底图」那颗件的**公共件**）
//
// ============================ 出处（唯一正本） ============================
//  · 反编译（逐句）：`d:/2/tools/decomp_full/BlinkGraphic__{ctor,Start,Update,OnValidate}.c`
//  · 字段表 / 出厂值：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/BlinkGraphic.cs`
//  · 实例普查（36 个逐个实读）：`d:/4/Unity/资料/普查产出_1012/V8_判据补查.md` **§A315**
//  · 我们现行的逐字实现（抽件就是把它抬出来）：`Shell/RewardWindow.cs` 的
//    `BlinkT` / `BlinkAlpha` / `BlinkClock` / `BlinkTick`（2026-10-11 批次2 · A355 落地）
//
// ---- 🔴 原版是什么（照 `Update.c` 逐句，**别的都不算**）----
//   字段（`dump.cs` 的偏移 + `.ctor` 立即数 + `Update/Start` 实读）：
//     `graphic`        `[SerializeField] Graphic`（`OnValidate()` 里为 null 则 `GetComponent<Graphic>()`）
//     `blinkSpeed`     出厂 `0x3f800000` = **1.0**
//     `colorVariation` 出厂 `0x3f000000` = **0.5**
//     `currentTime`    `.ctor` 不初始化 ⇒ **0**
//     `originalColor`  `Start()` 从 `graphic.color` 拷（vtable `0x298` getter）
//   `Update()` 每帧（**顺序就是语义**，见下）：
//     ① `t = Clamp01(|**Mathf.Cos**(blinkSpeed × currentTime)|)`   （`FUN_180488130` = `Mathf.Cos`，
//        上界字面量 `0x1834b2bb8` 实读 = 1.0f；`0x1834b2e60` = `0x7FFFFFFF` = 取绝对值的位掩码）
//     ② **只改 alpha 那一位**：`a ← Lerp(original.a, original.a × colorVariation, t)`
//        （新值写在 `Color` 的**高 32 位**，低 32 位原样 `CONCAT44` 带过去；写色走 vtable `0x2a8`）
//     ③ **算完才** `currentTime += Time.deltaTime`（`UnityEngine_Time__get_deltaTime` ⇒ **scaled**）
//   `graphic == null` 那一支：走 `FUN_1803f47a0()`（**抛 NRE**，反编译标「不返回」）
//     ⇒ ③ 那句写在 `if (graphic != null)` **之内** ⇒ **时钟一次都不会走**。
//   🔴 **是 `cos` 不是 `sin`** —— 三条独立判据（`Easing.g__inOutSine_0_30` / `outSine_0_29` /
//     `UIFlippableAndRotableUVs.RotatePointAroundPivotUVs` 的标准旋转式）→ V8 §A315 ②(b) 全文。
//     差别是**相位差 90°**：开窗第一刻原版 α = `Lerp(a, 0.5a, |cos 0| = 1)` = **0.5a（最暗）**。
//
// ---- 🔴 我们这一份与原版的两处**有意**不同（逐条如实标注，⛔ 别当成「抄漏了」）----
//   ① **不是「挂到目标节点上」的组件，而是挂在窗口自己身上**：
//      原版把 `BlinkGraphic` 挂在那颗 `Graphic` 的 GameObject 上；我们的窗**每次重开都重建子树**
//      （`RewardWindow.Build()` 会 `DestroySafe` 掉所有子件），挂子件上会随节点一起死 ⇒
//      **时钟会跟着断**（而且自检那条「目标不在 ⇒ 时钟一步都不走」就分不出「断了」与「本来就没走」）。
//      挂在**窗口根**上之后：目标被销毁 ⇒ `Target == null`（Unity 的伪 null）⇒ **正好就是原版
//      `graphic == null` 那一支**，而时钟本体还在 ⇒ 读数可查。语义不但没变弱，反而更可验。
//   ② **`graphic == null` 那一支我们【不抛 NRE】**（原版抛）：批处理里抛一下整条自检就没了。
//      改成「**不推时钟、不写色**（= 逐字对齐那句推进的语义）＋ **出声一次**」—— 红线：不许静默失败。
//   ③ **目标节点隐藏（`SetActive(false)`）时我们照推时钟**，原版那一支 `Update()` 根本不会被调
//      （组件挂在那个节点上 ⇒ 跟着一起停）。差**只在读 `Clock` 时看得出来**，而且写进去的是**不可见的**标签
//      ⇒ 没有可见后果；换来的好处是「时钟不住在会被销毁的节点上」（①）。判据里**别指望这一档**。
//      ⚠️ 附带一条实的：`RewardWindow` 预览态把 `Tap To Continue` 整颗关着（`Open.c:189`）——
//      原版那一档闪烁确实不跑，我们这一档**跑**（`Editor/RewardsScene.cs` §九(f) 只断「节点关着」，
//      没断时钟 ⇒ 现有断言不受影响）。
//
// ---- 🔴 实例面（为什么它值得是一个公共件）----
//   原版全库 **36 个实例**（全在 `bundle_menus_assets_all`），`(blinkSpeed, colorVariation)` **36/36 = (1.0, 0.5)**
//   （与 `.ctor` 的两个立即数**逐位相同** ⇒ 一个 prefab 都没覆盖过）。宿主名分布：
//   `background`×22 · `Tap To continue`×9 · `Tap To Continue`×2 · `Glow`×2 · `Tap to close`×1。
//   我们生产上**只做了 1 处**（`RewardWindow` 的 `Tap To Continue`）⇒ 这是一张**内容面欠账**，
//   本文件就是让后续那些宿主**接一处、少一处**，⛔ 别在第三处再抄一份公式
//   （`CLAUDE.md` §三「两处写同一条规则 = 迟早不一致」）。
//
// ---- 用法 ----
//   var b = go.GetComponent<BlinkGraphic>() ?? go.AddComponent<BlinkGraphic>();
//   b.LogTag = "[RewardWindow]";                       // 出声时用哪个前缀（可省）
//   b.Bind(targetGo, () => lb.color, c => lb.SetColor(c));   // 目标节点 + 读/写那一位色
//   b.Restart();                                       // 每次重建都归零 + 重新取原色（= 原版 `Start()`）
//   b.Tick(dt);                                        // 帧循环里由宿主调（批处理下自检直调）
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `BlinkGraphic` —— 「会呼吸的提示 / 底图」那颗件的公共件。
    /// <para>🔴 **本类不实现 `Update()`**：时钟由宿主推（`Tick(dt)`）。理由是宿主那一侧
    /// （`RewardWindow.Tick` / `BoosterInfoPopup.Tick`）本来就是**两样东西共用一个时钟**
    /// （开场揭示 + 逐件 punch + 本件都在同一拍里推），组件自己也 `Update` 会**走两倍速**
    /// （同 `WarpforgeEffectPlayer.autoTick` 那条教训）。</para></summary>
    public class BlinkGraphic : MonoBehaviour
    {
        // ============================================================ 出厂值（= 原版 `.ctor` 的两个立即数）

        /// <summary>`blinkSpeed` 出厂值 —— 原版 `.ctor` 立即数 `0x3f800000`（实读 **1.0**）。</summary>
        public const float DefaultSpeed = 1f;
        /// <summary>`colorVariation` 出厂值 —— 原版 `.ctor` 立即数 `0x3f000000`（实读 **0.5**）。
        /// 语义 = 「最暗那一档 = 原色 × 0.5」（`Lerp(a, a×0.5, t)` 在 `t = 1` 处的值）。</summary>
        public const float DefaultVariation = 0.5f;

        /// <summary>`blinkSpeed`（`+0x28`）。36 个实例**全是 1.0**。</summary>
        public float blinkSpeed = DefaultSpeed;
        /// <summary>`colorVariation`（`+0x2c`）。36 个实例**全是 0.5**。</summary>
        public float colorVariation = DefaultVariation;

        /// <summary>出声时用的前缀（默认 `[BlinkGraphic]`）。宿主可以改成自己的标签
        /// （`RewardWindow` 用 `[RewardWindow]` —— 那条警告的既有文案在别处被引用过）。</summary>
        public string LogTag = "[BlinkGraphic]";

        /// <summary>`true` = **这一轮没有可闪的目标是已知的**（例如图取不到），别出声 ——
        /// 那种情况下宿主自己已经报过一条，这里再报只是刷屏。**时钟照样不走**。</summary>
        public bool Silent;

        // ============================================================ 目标（= 原版那颗 `graphic`）

        /// <summary>= 原版 `graphic`（那颗 `Graphic` 所在的 GameObject）。
        /// 🔴 **目标被销毁时 Unity 的 `!= null` 会变假** ⇒ `Bound` 自动变假 = 原版 `graphic == null` 那一支。</summary>
        public GameObject Target { get; private set; }
        /// <summary>读目标当前色（= 原版 `Start()` 里的 `graphic.get_color()`）。</summary>
        System.Func<Color> _read;
        /// <summary>写目标色（= 原版 vtable `0x2a8` 的 `graphic.set_color`）。**只改 alpha 一位**由本类保证。</summary>
        System.Action<Color> _write;

        // ============================================================ 状态（= 原版 `currentTime` / `originalColor`）

        float _clock;                       // = `currentTime`（`+0x30`）
        Color _base = Color.white;          // = `originalColor`（`+0x34..0x43`）
        bool _hasBase;
        bool _unboundWarned;

        /// <summary>`true` = 这一拍**写得出色**（目标在、读写都接上了、原色取过了）。
        /// 为假就是原版 `graphic == null` 那一支（那边抛 NRE）。</summary>
        public bool Bound { get { return Target != null && _write != null && _read != null && _hasBase; } }

        /// <summary>= 原版 `BlinkGraphic.currentTime`。**目标没了它也不回零**（原版那边压根不推，
        /// 但值还留在字段里）—— 自检按它认「时钟到底走没走」。</summary>
        public float Clock { get { return _clock; } }

        /// <summary>= 原版 `originalColor`（`Start()` 那一刻抄下来的色；没抄过 = 白）。</summary>
        public Color BaseColor { get { return _base; } }

        // ============================================================ 两条公式（**只此一份**）

        /// <summary>`t = Clamp01(|cos(blinkSpeed × clock)|)` —— 原版 `Update()` 的第 ① 步**逐字**。
        /// 🔴 **是 cos，不是 sin**（判据见文件头）；相位差 90° 是**看得见**的：
        /// `clock = 0` 时 cos 给 1（最暗），sin 给 0（原色）。</summary>
        public static float T(float clock, float speed)
        {
            return Mathf.Clamp01(Mathf.Abs(Mathf.Cos(speed * clock)));
        }

        /// <summary>`a = Lerp(baseA, baseA × variation, t)` —— 原版 `Update()` 的第 ② 步**逐字**
        /// （反编译里是 `(a×v − a)×t + a`，与 `Lerp` 同式）。</summary>
        public static float AlphaOf(float baseA, float t, float variation)
        {
            return Mathf.Lerp(baseA, baseA * variation, t);
        }

        /// <summary>把新 alpha 填进原色（其余三位**原样带过去** = 反编译里那个 `CONCAT44`）。</summary>
        public static Color Tinted(Color baseColor, float t, float variation)
        {
            var c = baseColor;
            c.a = AlphaOf(baseColor.a, t, variation);
            return c;
        }

        /// <summary>此刻的 `t`（自检直接读它）。</summary>
        public float T01 { get { return T(_clock, blinkSpeed); } }

        /// <summary>此刻该写进去的 alpha（= `Lerp(原色.a, 原色.a × colorVariation, T01)`）。</summary>
        public float Alpha { get { return AlphaOf(_base.a, T01, colorVariation); } }

        // ============================================================ 接口

        /// <summary>= 原版 `OnValidate()` 那一句（`graphic == null ⇒ GetComponent&lt;Graphic>()`）的**我们的写法**：
        /// 目标节点 + 读色 + 写色三件一起接。传 `null` = 解绑。
        /// ⚠️ **只接不算原色** —— 原版也是 `Start()` 才拷；要归零/取原色再调 <see cref="Restart"/>。</summary>
        public void Bind(GameObject target, System.Func<Color> read, System.Action<Color> write)
        {
            Target = target;
            _read = read;
            _write = write;
            _hasBase = false;
        }

        /// <summary>= 原版 `Start()`：`originalColor = graphic.color`，并把时钟归零。
        /// 🔴 **一帧都不写色**（原版 `Start()` 也不写）—— 自检有一条「建完还没 Tick ⇒ 颜色还是出厂原色」盯着它。
        /// ⚠️ **不清 `_unboundWarned`**（同 `RewardWindow` 既有行为：出声只响一次）。</summary>
        public void Restart()
        {
            _clock = 0f;
            if (_read != null && Target != null)
            {
                _base = _read();
                _hasBase = true;
            }
            else
            {
                _hasBase = false;
            }
        }

        /// <summary>= 原版 `BlinkGraphic.Update()`（**逐句同序**）。
        /// <para>① 按**此刻**的时钟算色 → ② 只改 alpha 写进去 → ③ **算完才** `clock += dt`。</para>
        /// <para>🔴 目标不在（= 原版 `graphic == null`）⇒ **直接 return、既不写色也不推时钟**
        /// （原版那一支抛 NRE，所以 `+= dt` 压根执行不到）；我们**只出声一次**，不抛。</para></summary>
        public void Tick(float dt)
        {
            if (!Bound)
            {
                if (!Silent && !_unboundWarned)
                {
                    _unboundWarned = true;
                    Debug.LogWarning(LogTag + " `BlinkGraphic` 没有可闪的目标"
                                     + "（= 原版 `BlinkGraphic.graphic == null`）⇒ 原版在这一支**抛 NRE、"
                                     + "时钟一次都不走**（`BlinkGraphic__Update.c:20-30` 的推进写在 "
                                     + "`if (graphic != null)` 之内）；我们**不抛**、**也不推时钟、不写色**"
                                     + "（逐字对齐那一句的语义）。");
                }
                return;
            }
            _write(Tinted(_base, T01, colorVariation));    // 原版 `:17` 读时钟算 `t` ⇒ `:24-26` 只改 alpha 那一位
            _clock += dt;                                  // **算完才推**（原版 `:31`）—— ⛔ 别挪到写色之前
        }
    }
}
