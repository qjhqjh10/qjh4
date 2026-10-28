// AllianceRatingDisplay.cs —— 原版 `AllianceRatingDisplay`（MonoBehaviour：一颗 TMP + 一颗 Image）
//
// ============================ 这是什么 ============================
// 一颗「评级」显示件：左边一个小图标（`Secondary Icon`）、中间一个大图标（`Main Icon`）、
// 右边一串数字（`Individual rating value`）。`FactionScore` 里那两行、联盟成员行、
// 排位窗的 `Global Rating` 都是它。
//
// ============================ 判据（三源现读，2026-10-29 B17）============================
//   · 字段/签名 = `d:/2/tools/il2cpp_out/dump.cs:97290-97310`：
//       `[SerializeField] private EverguildTextMeshPro ratingText; // 0x20`
//       `[SerializeField] private Image ratingIcon;                // 0x28`
//       `public void Initialize(int value, Sprite overrideIcon, bool disableIfZero = False,
//                              bool useSealSystem = False, bool zeroBasedRank = True)`
//   · 方法体 = `d:/2/tools/decomp_full/AllianceRatingDisplay__Initialize.c`（VA 级，**逐分支实读**）：
//       ① `if (disableIfZero && value == 0) { gameObject.SetActive(false); return; }`
//       ② `handler = LiveOpsManager.GetHandler<GameModes>()`；
//          `ev = handler == null ? null : handler.GetActiveEvent<IRankedEvent>()`；
//          **`ev == null` ⇒ `ratingText.text = value.ToString()`**（`LAB_1807ac910` 那一段）
//       ③ `ev != null` 时还有两支「段位格式串」/「`value - rankOffset`」，用的是**远端活动数据**
//          （`GetActiveEvent` 的返回值 + `FeatureConfig.Check`）—— 见下面 ⚠️
//       ④ 末尾：`if (overrideIcon != null) ratingIcon.sprite = overrideIcon`（**只覆盖、不清空**）
//   · 交叉 = `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/AllianceRatingDisplay.cs`
//
// ⚠️ 差异（如实标，⛔ 不是「已经一样」）：
//   ① 原版 `EverguildTextMeshPro` 我们没有 ⇒ `ratingText` 用 `Label`（本工程唯一文字件，`Label.Text` 可回读）。
//   ② 原版 `Image` 是 uGUI ⇒ 我们用 `ImageQuad`（本工程唯一图片件；`Texture` 可回读）。
//      原版 `Sprite overrideIcon` 对应我们的 `Texture2D`（本工程的图一路都是 Texture2D）。
//   ③ **③ 那两支（印章 `useSealSystem` / 段位格式 `zeroBasedRank`）没有落地** —— 它们要
//      `LiveOpsManager` 的**活动数据**（P3 远端，关服后本地永远拿不到）。⚠️ 这不是「漏做」：
//      **原版关服后同样走 ②**（`GetActiveEvent` 返回 null）⇒ 我们的行为 = 原版**离线态**的行为。
//      真拿到活动数据那天，落点就是本文件的 `FormatRanked()`（现在只出声 + 直写 `value`）。
//   ④ `ratingIcon` / `ratingText` 由**宿主**建好之后 `Bind()` 进来（原版是 prefab 序列化）。
//      那颗 icon 的三个子节点（`Secondary Icon` / `Main Icon` / `Individual rating value`）的
//      几何**逐实例不同**（铁律 5·c：同一份 `FactionScoreSmall` 三个宿主三套宽度，见
//      `资料/完全复刻/报告/B94_B17.md` §五那张表）⇒ ⛔ 这里**不写死**，由宿主按自己的框建。
using UnityEngine;

namespace CardPresentation.ScoreBars
{
    /// <summary>原版 `AllianceRatingDisplay`：写一颗评级数字 + 可选覆盖图标。
    /// <para>⚠️ 本类在 <c>CardPresentation.ScoreBars</c> 命名空间下，**不是** `CardPresentation.AllianceRatingDisplay`
    /// —— 本仓此前没有这个类，新建时特意收进子命名空间，免得与将来别处同名件撞车
    /// （`CardPresentation` 下已有一个同名的**非 MonoBehaviour** `MilestoneScoreBar`，先例见那个文件）。</para></summary>
    public class AllianceRatingDisplay : MonoBehaviour
    {
        // 🔴 字段名**逐字照 `dump.cs`**（断言 `<see cref="AssertFieldNames"/>` 会按反射钉死）——
        //    ⛔ 别改名，也别把 `ratingText` / `ratingIcon` 合并成一个。
        [SerializeField] private Label ratingText;          // 原版 `+0x20`（`EverguildTextMeshPro`）
        [SerializeField] private ImageQuad ratingIcon;      // 原版 `+0x28`（`Image`）

        /// <summary>绑宿主建好的两件（原版由 prefab 序列化给）。`null` 是允许的：那一半就跳过（会出声）。</summary>
        public void Bind(Label text, ImageQuad icon)
        {
            ratingText = text;
            ratingIcon = icon;
        }

        public Label RatingText { get { return ratingText; } }
        public ImageQuad RatingIcon { get { return ratingIcon; } }

        /// <summary>原版 `public void Initialize(int value, Sprite overrideIcon = null,
        /// bool disableIfZero = false, bool useSealSystem = false, bool zeroBasedRank = true)`。
        /// <para>分支逐条（判据见文件头 ①–④）：</para>
        /// <list type="number">
        /// <item>`disableIfZero && value == 0` ⇒ **整颗 `SetActive(false)` 并返回**（原版第一句，
        ///   注意它**不会**在别的分支把父件重新 `SetActive(true)` —— 照抄这个「单向」行为）。</item>
        /// <item>否则写 `ratingText.text = value.ToString()`。</item>
        /// <item>`overrideIcon != null` ⇒ `ratingIcon.sprite = overrideIcon`（原版不清空、只覆盖）。</item>
        /// </list>
        /// <para>⚠️ `useSealSystem` / `zeroBasedRank` 两个形参**只出声、不改变输出**（③ 那两支要远端活动数据，
        /// 见文件头 ⚠️④）—— ⛔ 别把它们「按直觉」实现成某种编号偏移，那是猜的。</para></summary>
        public void Initialize(int value, Texture2D overrideIcon = null, bool disableIfZero = false,
                               bool useSealSystem = false, bool zeroBasedRank = true)
        {
            // ① 原版第一支：`if (param_4 != '\0' && value == 0) { gameObject.SetActive(false); return; }`
            if (disableIfZero && value == 0)
            {
                gameObject.SetActive(false);
                return;
            }

            // ③ 那两支（印章 / 段位格式）要 LiveOps 活动数据 ⇒ 见文件头 ⚠️③。出声一次，⛔ 不静默。
            if (useSealSystem || !zeroBasedRank)
                WarnOnce("[AllianceRatingDisplay] `useSealSystem=" + useSealSystem
                       + "` / `zeroBasedRank=" + zeroBasedRank
                       + "` 这两支要 **LiveOps 活动数据**（P3 远端，关服后本地拿不到）⇒ 按原版离线态直写 `value.ToString()`");

            // ② 原版 `LAB_1807ac910`：`ratingText.text = value.ToString()`
            if (ratingText != null) ratingText.SetText(value.ToString());
            else WarnOnce("[AllianceRatingDisplay] `ratingText` 没绑 ⇒ 评级数字不写（原版写 `EverguildTextMeshPro.text`）");

            // ④ 原版末句：`if (overrideIcon != null) ratingIcon.sprite = overrideIcon`
            if (overrideIcon != null)
            {
                if (ratingIcon != null) ratingIcon.SetTexture(overrideIcon);
                else WarnOnce("[AllianceRatingDisplay] `ratingIcon` 没绑 ⇒ 传进来的 `overrideIcon` 没画（原版写 `Image.sprite`）");
            }
        }

        [System.NonSerialized] private bool _warned;

        /// <summary>出声**只出一次**（一颗评级件在一次开窗里会被刷很多遍，每遍 Log 会刷屏）。</summary>
        private void WarnOnce(string msg)
        {
            if (_warned) return;
            _warned = true;
            Debug.LogWarning(msg);
        }
    }
}
