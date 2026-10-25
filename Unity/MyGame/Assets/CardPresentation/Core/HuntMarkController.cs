// HuntMarkController.cs — 原版 `HuntMarkController`（39 行 · `huntMark` / DefinedTrait 1260 的计数器）
//
// 判据正本 → `Unity/资料/完全复刻/判据/TraitEffectController_钩子层.md` §1.1 / §3.2 / §6.1② / §6.5
// 原版一手出处（**逐字读过全文**）：
//   `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/HuntMarkController.cs`
//     :7-9   private TextMeshPro counter;                        ← **唯一**序列化字段
//     :11-13 protected override void Start() { }                 ← 空覆写
//     :15-17 public override void Toggle(bool option) { }        ← 空体
//     :19-29 public override void UpdateValue(int newValue)      ← 本类的全部意义
//     :31-37 public override void AimingAttack(bool option)      ← **直接 throw NotImplementedException**
//   方法体常量（ILSpy 把常量丢成 `default`，真值从反编译 `.c` + `.rdata` 实读）：
//   `D:/2/tools/decomp_full/HuntMarkController__UpdateValue.c:13-30`
//       counter.text = newValue.ToString();                       // :17（`0x558` = TMP `set_text`）
//       DOPunchScale(counter.transform, /*punch*/ …, /*duration*/ 0.5f, /*vibrato*/ 10, /*elasticity*/ 1.0f);
//         · 0.5 = `DAT_1834b2bb4`（`.rdata` 实读）· 1.0 = `DAT_1834b2bb8`（`.rdata` 实读）
//         · vibrato = 10（`mov` 立即数，`:29`）
//         · 0.1 = `DAT_1834b2dc4`（`.rdata` 实读）
//         · punch 向量 = UnityEngine 静态 Vector3 表的 **+0x0C 项 = `Vector3.one`**
//           （同表 +0x00 项是 `zero`，`DisplayStatusAnim` 拿它当 `localPosition`）
//
//   ⚠️ **置信度如实分级**（判据 §3.2 原话；⛔ 别把推断写成实读）：
//     · `0.5` / `10` / `1.0` —— **实读**（`.rdata` 常量表 + 指令立即数）。
//     · `0.1` —— **实读**（`.rdata` 常量池里的那个 float）。
//     · **`Vector3.one * 0.1f` 这个「表项 × 常量」的乘法** —— **高置信推断**：
//       反编译只读到「取静态 Vector3 表 +0x0C 项」与「`.rdata` 里有 0.1」，两者组合成 punch 向量
//       是按「punch 的缩放必须相对 1 才有意义 + 同族 `DOPunchScale` 的写法」推出来的。
//
// 生命周期（判据 §1.2 —— 决定了本文件里 `Awake` 的存在）：
//   `Instantiate` → `localScale = prefab.localScale` → **invoke 回调（此拍就调 `UpdateValue`）**
//   → `AddComponent<CardAnimController>() + Initialize(...)`；`Start` 是**下一拍**才跑。
//   ⇒ 绑定不能只放在 `Start` 里（第一拍 `UpdateValue` 会拿到 null）⇒ 见 `BindIfMissing()`。
//
// 输出边界（判据 §3.3）：⛔ 不写引擎状态 / 不发事件 / 不管「框什么时候消失」。
using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>
    /// `huntMark`（1260）那个**计数器**：`Counter`（TMP）写当前层数 + `DOPunchScale` 弹一下。
    /// 全库只有 `HuntMark_IdleEffect` 这一个 prefab 带它（判据 §1.3）。
    /// </summary>
    public class HuntMarkController : TraitEffectController
    {
        // ── 原版数值（全部有出处，见文件头；⛔ 别按手感调）────────────────────────────
        /// <summary>`DAT_1834b2dc4`（`.rdata` 实读）。⚠️ 「× `Vector3.one`」那一跳是**高置信推断**（见文件头）。</summary>
        public const float PunchScale = 0.1f;
        /// <summary>`DAT_1834b2bb4`（`.rdata` 实读）= 0.5 s。</summary>
        public const float PunchDuration = 0.5f;
        /// <summary>`HuntMarkController__UpdateValue.c:29` 的 `mov` 立即数 = 10。</summary>
        public const int PunchVibrato = 10;
        /// <summary>`DAT_1834b2bb8`（`.rdata` 实读）= 1.0。</summary>
        public const float PunchElasticity = 1.0f;

        /// <summary>原版 `:9` 的**唯一**序列化字段（原版是 `private TextMeshPro counter`）。
        /// ⚠️ 我们导入的 prefab 上没有这个组件（原版组件只活在 bundle 里），由
        /// `TraitEffectControllers.Attach` 追加本组件后**按名字**绑到 `Counter` 子节点。</summary>
        [SerializeField] TextMeshPro counter;

        // ── 观测量（判据 §6.5；**我们的**，⛔ 不是原版字段；只读，给自检用）────────────────
        /// <summary>实际弹了几次（每次成功的 `UpdateValue` = 1 次 punch）。</summary>
        public int PunchCount { get; private set; }
        /// <summary>累计被调 `UpdateValue` 的次数（含「绑不上 `Counter`」的那些 —— 证「调用确实发生了」）。</summary>
        public int UpdateCalls { get; private set; }
        /// <summary>最近一次写进去的数（原版没有这个概念，是我们给断言用的）。</summary>
        public int LastValue { get; private set; }
        /// <summary>`Counter.text` 的当前值；`Counter` 没绑上时为 null。</summary>
        public string TextForTest { get { return counter != null ? counter.text : null; } }
        /// <summary>`Counter`（TMP）绑上了没有。</summary>
        public bool Bound { get { return counter != null; } }

        // ── 生命周期 ────────────────────────────────────────────────────────────
        /// <summary>⚠️ **我们的**（原版没有 `Awake`）：原版靠 prefab 序列化拿 `counter`，我们靠名字绑。
        /// 必须在 `Awake` 里就绑好 —— 原版 `UpdateValue` 在 `Instantiate` 那一拍就会被调，`Start` 是下一拍。</summary>
        void Awake() { BindIfMissing(); }

        /// <summary>原版 `:11-13` 是**空覆写**；我们这里只补一句绑定（幂等）。</summary>
        protected override void Start() { BindIfMissing(); }

        // ── 三个抽象方法 ────────────────────────────────────────────────────────
        /// <summary>原版 `:15-17` —— **空体**（本 build 0 调用点，判据 §八·A）。⛔ 别赋予语义。</summary>
        public override void Toggle(bool option) { }

        /// <summary>原版 `:19-29` 逐字：
        /// `counter.text = newValue.ToString();` 然后
        /// `DOPunchScale(counter.transform, Vector3.one * 0.1f, 0.5f, 10, 1.0f)`。</summary>
        public override void UpdateValue(int newValue)
        {
            UpdateCalls++;
            LastValue = newValue;
            BindIfMissing();
            if (counter == null) return;   // 绑不上已经 `LogError` 过（⛔ 不静默），这里别 NRE

            counter.text = newValue.ToString();

            PunchCount++;
            // 原版是裸 `DOPunchScale`；我们包一层 `CardTween.Use` **只为两件事**：
            //   ① `SetUpdate(CardTween.Mode)` —— 批处理自检里没有帧循环，要把补间切成 Manual 才能推进；
            //   ② `SetLink(counter.transform.gameObject)` —— 框被 `Destroy` 时补间一起死（本工程硬规矩）。
            // ⚠️ 四个参数**一个不改**（0.1 / 0.5 / 10 / 1.0）；`Ease.Linear` 对 punch 不生效（punch 用自己的曲线）。
            CardTween.Use(
                counter.transform.DOPunchScale(
                    Vector3.one * PunchScale, PunchDuration, PunchVibrato, PunchElasticity),
                Ease.Linear, counter.transform);
        }

        /// <summary>原版 `:31-37` —— **直接 `throw new NotImplementedException()`**（照抄，⛔ 别写个空 body 假装实现）。
        /// 本 build 没有任何调用点会走到这里（判据 §八·A：三处 `GetComponent` 调的都是 `UpdateValue`）。</summary>
        public override void AimingAttack(bool option)
        {
            throw new NotImplementedException(
                "HuntMarkController.AimingAttack —— 原版此处就是 `throw new NotImplementedException()`"
                + "（`HuntMarkController.cs:31-37`）。保留同样的语义：本方法不该被调用。");
        }

        // ── 绑定 ────────────────────────────────────────────────────────────────
        /// <summary>
        /// 按名字绑 prefab 上的 `Counter`（TMP）。
        ///
        /// ⚠️ **必须出声**：`transform.Find` 只找**直接子件**（CLAUDE.md 已知的坑），prefab 层级一变就
        /// 静默拿到 null ⇒ 这里 `LogError`（铁律「不许静默失败」）。
        /// 本机实测路径 = `['Counter','HuntMark_IdleEffect']`（直接子件，判据 §1.3 / §6.2 已核）。
        /// 组件类型实测 = `TMPro.TextMeshPro`（3D 版，脚本 guid `9541d86e2fd84c1d9990edf0852d74ab`），
        /// 与原版字段类型一致。
        /// </summary>
        void BindIfMissing()
        {
            if (counter != null) return;

            Transform t = transform.Find("Counter");
            if (t != null) counter = t.GetComponent<TextMeshPro>();

            if (counter == null)
                Debug.LogError("[HuntMarkController] 绑不上 `Counter`（TMPro.TextMeshPro）—— 原版这是 prefab 的"
                             + "序列化引用，我们按名字绑（实测路径 ['Counter','HuntMark_IdleEffect']）。"
                             + "prefab 层级变了就会走到这里 ⇒ 计数器不会更新、也不会弹。"
                             + "判据 → 资料/完全复刻/判据/TraitEffectController_钩子层.md §1.3 / §6.2");
        }
    }
}
