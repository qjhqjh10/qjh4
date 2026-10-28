// ProgressBar.cs —— 原版 `ProgressBar`（MonoBehaviour：`Slider` + 一颗 TMP + `IsFilled`）
//
// ============================ 这是什么 ============================
// 「当前 / 上限」的线性进度条：写 `Slider` 的三个值 + 一颗 `"当前/上限"` 文字 + 一个「满了吗」标志。
// ⚠️ 它**不是**账号级里程碑进度（那个是 `MilestoneScoreBar` 的 `InterpolateMilestones`，**按档位等分**）——
//    两个 prefab 家族各用各的，⛔ 别互相顶替（`AllianceEventScoreInfo.ScoreProgressBar` 里同一条叮嘱）。
//
// ============================ 判据（三源现读，2026-10-29 B17）============================
//   · 字段/签名 = `d:/2/tools/il2cpp_out/dump.cs:96888-96922`
//       `[SerializeField] private Slider slider;                  // 0x20`
//       `[SerializeField] private EverguildTextMeshPro text;      // 0x28`
//       `[CompilerGenerated] private bool <IsFilled>k__BackingField; // 0x30`
//       `public void SetProgress(int value, int maxValue = 100, string format = "{0}/{1}", bool useWholeNumbers = True)`
//       `public void SetProgress(ScoringDataInfo scoringDataInfo)`
//   · 内层方法体 = **VA 反汇编** `ProgressBar$$SetProgress(int,int,string,bool)`（VA `0x1807BA970`）：
//       `movzx edx, byte ptr [rsp + 0x60]` → `call set_wholeNumbers`
//         ⇒ `slider.wholeNumbers = **useWholeNumbers 这个形参**`
//         🔴 ILSpy 把这一句写成 `bool wholeNumbers = default;` —— 那是**反编译丢的局部**，
//            ⛔ 别以为原版恒 `false`（这条与 `AllianceEventScoreInfo.ScoreProgressBar` 头注释里那条
//            「ILSpy 丢常量」是同一族，但结论不同：**这里丢的是形参、不是常量**）。
//       `cvtdq2ps xmm1, esi` → `call 0x1833c8730`（`Slider.set_maxValue`）
//       `cvtdq2ps xmm1, ebp` → `call [rax+0x428]`（`Slider.set_value`）
//       `text(+0x28)` 非空 ⇒ `String.Format(format, value, maxValue)` → `TMP.text`
//       收尾 `cmp ebp, esi; setge al; mov byte ptr [rdi + 0x30], al`
//         ⇒ **`IsFilled = value >= maxValue`**（`+0x30` = 那个 backing field；**等号算满**）
//   · 外层重载 = `d:/2/tools/decomp_full/ProgressBar__SetProgress.c`：只做一次转调
//       `SetProgress(d.CurrentScore /*+0x10*/, d.NextLevelScore /*+0x18*/, "{0}/{1}", true)`
//       🔴 `PreviousLevelScore`（`+0x14`）**整条链上一次都没用** —— 铁律 5·c 那种「看着该参与」的格子。
//   · 格式串默认值 = `stringliteral.json` 地址 `0x426de28` = `"{0}/{1}"`
//
// ⚠️ 差异（如实标）：
//   ① `text` 用 `Label`（原版 `EverguildTextMeshPro`），`slider` **保留 uGUI `UnityEngine.UI.Slider`**
//      —— 与 `dump.cs` 同类型；本工程菜单线没有 uGUI 运行时（A92 普查），所以网格那一档的宿主应改走
//      `AllianceEventScoreInfo.SetProgress(float)`（既有实现，⛔ 别再写第三份）。
//   ② 原版 `slider`/`text` 为 null 时是 **NRE**；我们**出声 + 继续**（全工程口径：坏引用不让游戏崩）。
//      注意 `IsFilled` 仍然照写 —— 它是纯数据，不该因为 UI 没绑就不更新。
using UnityEngine;
using UnityEngine.UI;

namespace CardPresentation.ScoreBars
{
    /// <summary>原版 `ProgressBar`：写 `Slider` 三个值 + `"当前/上限"` 文字 + `IsFilled`。</summary>
    public class ProgressBar : MonoBehaviour
    {
        /// <summary>`SetProgress` 的格式串默认值（判据 = `stringliteral.json` 地址 `0x426de28`）。</summary>
        public const string DefaultFormat = "{0}/{1}";

        // 🔴 字段名逐字照 `dump.cs`（断言按反射钉死）。
        [SerializeField] private Slider slider;      // 原版 `+0x20`
        [SerializeField] private Label text;         // 原版 `+0x28`

        /// <summary>原版 `public bool IsFilled { get; private set; }`
        /// —— C# 自动属性生成的 backing field 名恰好是原版的 `<IsFilled>k__BackingField`（`+0x30`）。</summary>
        public bool IsFilled { get; private set; }

        /// <summary>绑宿主建好的两件（原版由 prefab 序列化给）。</summary>
        public void Bind(Slider bar, Label label)
        {
            slider = bar;
            text = label;
        }

        public Slider Slider { get { return slider; } }
        public Label Text { get { return text; } }

        /// <summary>原版 `public void SetProgress(int value, int maxValue = 100,
        /// string format = "{0}/{1}", bool useWholeNumbers = true)` —— 五句，逐句实读（见文件头）。</summary>
        public void SetProgress(int value, int maxValue = 100, string format = DefaultFormat,
                                bool useWholeNumbers = true)
        {
            if (slider != null)
            {
                slider.wholeNumbers = useWholeNumbers;   // ① 形参，不是常量（VA 反汇编结的案）
                slider.maxValue = maxValue;              // ②
                slider.value = value;                    // ③（`Slider` 自己 clamp 到 [min,max]）
            }
            else
            {
                Debug.LogWarning("[ProgressBar] `slider` 没绑 ⇒ 进度条不动（原版这一跳是 NRE；我们出声继续）");
            }

            // ④ `if (text) text.text = String.Format(format, value, maxValue)`
            if (text != null) text.SetText(string.Format(format ?? DefaultFormat, value, maxValue));
            else Debug.LogWarning("[ProgressBar] `text` 没绑 ⇒ `\"当前/上限\"` 不写（原版这一跳是 NRE；我们出声继续）");

            // ⑤ `IsFilled = value >= maxValue`（`cmp/setge`，**等号算满**）
            IsFilled = value >= maxValue;
        }

        /// <summary>原版 `public void SetProgress(ScoringDataInfo scoringDataInfo)`：
        /// `SetProgress(CurrentScore, NextLevelScore, "{0}/{1}", true)` —— **只此一句**。
        /// <para>🔴 `PreviousLevelScore` **不参与**（三个偏移 `+0x10 / +0x14 / +0x18` 里中间那个没被读）。</para>
        /// <para>`d == null` ⇒ 原版 NRE；我们出声 + 返回（⛔ 不假装有进度）。</para></summary>
        public void SetProgress(ScoringDataInfo scoringDataInfo)
        {
            if (scoringDataInfo == null)
            {
                Debug.LogWarning("[ProgressBar] `SetProgress(ScoringDataInfo)` 收到 null（原版这一跳是 NRE）⇒ 不改任何值");
                return;
            }
            SetProgress(scoringDataInfo.CurrentScore, scoringDataInfo.NextLevelScore, DefaultFormat, true);
        }
    }
}
