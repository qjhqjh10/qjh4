// MissionInfoDisplays.cs — 原版「任务卡上那几颗显示件」那一族（`MissionInfoDisplay` 及它的子类）的**同名复刻**。
//
// ============================ 判据（第一权威，逐份现读；⛔ 别另找） ============================
// · 可读源码（**优先**，逐句清楚）：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/MissionInfoDisplay.cs`
//   · `MissionBackgroundHighlighter.cs` · `MissionCounterDisplay.cs` · `MissionHeaderDisplay.cs`
//   · `MissionReRollButton.cs` · `MissionMilestonesDisplay.cs` · `MissionRewardsDisplay.cs`
//   · `MissionTimerDisplay.cs` · `MissionRewardItem.cs` · `MissionMilestoneStep.cs`
// · 交叉（C 伪码，方法体第一权威）：`d:/2/tools/decomp_full/Mission*__*.c`（每个方法一个文件）
// · 类桩（字段序 / 出厂值）：`d:/2/tools/il2cpp_out/dump.cs:61638-61690`
// · prefab 实例真值：`d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/`（逐颗实读，见各成员注释）
//
// ============================ 三条硬约束（本项目踩过，别再踩） ============================
// 🔴 ① **一律普通 C# 类，⛔ 不做 MonoBehaviour。** 编辑模式下 `AddComponent<T>()` **不触发**
//    `Awake/OnEnable/Start/Update`（本项目那条坑：`ShellScene.Run` 因此整月没跑过断言）——
//    而这些件在原版**全部**是 MonoBehaviour（靠 `Awake` 取子件）。我们的等价物是
//    「建树那一刻由 `MissionsTab` 把节点交给类」，**没有生命周期**，所以不需要、也不该是组件。
// 🔴 ② **`ImageQuad.Tint` 是只读属性**（`Battle/ImageQuad.cs:410` 只有 getter）⇒ 写色一律
//    `ImageQuad.SetTint(Color)`（`:398`）。
// 🔴 ③ 每一个 `Setup` 遇到**该有的节点是 null** 时**出声**（`Debug.LogWarning`），
//    ⛔ 不许静默返回 —— 原版那一支是空引用抛出（`FUN_1803f47a0` = 抛 NullReference），我们不出抛但要吼。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `IMission` + `MissionChallengeProgress` 上**被这几个显示件读过的那五个量**
    /// （每一个都标了原版读点；⛔ 这不是「我们的数据模型」—— 数据仍在 `DailyData`，本结构只是**读口**）：
    /// <list type="bullet">
    /// <item><see cref="CurrentValue"/> / <see cref="MaxValue"/> —— `progress.currentValue(+0x40)` /
    ///   `progress.MaxValue`（`MissionCounterDisplay__Setup.c:22,31` + getter `MissionChallengeProgress__get_MaxValue.c`）。
    ///   ⚠️ `MaxValue` **不是**字段直读：它 = `AvailableRewards()` 里**最后一个**里程碑的 `targetValue`
    ///   （getter 体：`LastOrDefault(b__19_0)`）⇒ 我们传 `Target`（最后一档阈值）与它同义。</item>
    /// <item><see cref="CanCollect"/> —— `progress.CanCollect()`，虚槽 **6**（`+0x198`）；
    ///   体 = `canCollect(+0x48) &amp;&amp; (loopable ? true : RewardsToCollect().All(逐奖励 CanCollect))`。</item>
    /// <item><see cref="IsComplete"/> —— `progress.IsComplete()`，**只读领取域**
    ///   （`Count(AvailableRewards()) &lt;= collectedRewards(+0x10)`）⇒ 语义 = **奖励已领取**，**不是**「进度到顶」。</item>
    /// <item><see cref="Army"/> —— `MissionChallenge.Army`（`MissionCounterDisplay__Setup.c` 里那个虚槽
    ///   `+0x1a8` = slot 8 = `get_Army`）。**`0` = `CardArmy.Neutral`** ⇒ 原版把那一格整格关掉。</item>
    /// </list>
    /// <para>🔴 **为什么是 struct**：原版这两个实参都**不允许为 null**（`progress == null` 时
    /// `MissionInfoDisplay__Initialize` / `MissionBackgroundHighlighter__Setup` 走到 `FUN_1803f47a0` =
    /// 抛空引用）—— 用值类型把那一支**从结构上消掉**，比到处判 null 更接近原版。</para></summary>
    internal readonly struct MissionProgressView
    {
        public readonly int CurrentValue;
        public readonly int MaxValue;
        public readonly bool CanCollect;
        public readonly bool IsComplete;
        public readonly int Army;

        internal MissionProgressView(int currentValue, int maxValue, bool canCollect, bool isComplete, int army)
        {
            CurrentValue = currentValue; MaxValue = maxValue;
            CanCollect = canCollect; IsComplete = isComplete; Army = army;
        }
    }

    /// <summary>原版 `MissionType`（`dump.cs:60414-60421`：`Challenge = 0` · `DailyMission = 1` · `WeeklyMission = 2`）。
    /// 用在 `MissionsTab.InstantiateNormalMissions(..., MissionType)` —— 原版按它**选 holder**。</summary>
    internal enum MissionType
    {
        Challenge = 0,
        DailyMission = 1,
        WeeklyMission = 2,
    }

    /// <summary>原版 `MissionInfoDisplay`（`cpp2il/out_cpp2il_dev_ilspy/MissionInfoDisplay.cs:3-62`；C 伪码
    /// `decomp_full/MissionInfoDisplay__Initialize.c`）。
    /// <para>它在这套件里的作用只有一条：**按 `displayRule` 决定这颗件显不显示，显示才 `Setup`**。</para></summary>
    internal abstract class MissionInfoDisplay
    {
        /// <summary>原版 `[Flags] enum DisplayRule { WhenActive = 1, WhenComplete = 2 }`（ILSpy `:5-9`）。</summary>
        [System.Flags]
        internal enum DisplayRule { WhenActive = 1, WhenComplete = 2 }

        /// <summary>原版出厂 `(DisplayRule)(-1)` = **每一位都置 1** ⇒ 恒显示（`MissionInfoDisplay.cs:11`）。
        /// ⚠️ 本页 prefab 里绝大多数件的实例值是 **1**（`WhenActive`），`timer` 是 **2**（见 `MissionsTab.BuildDailyRow`）。</summary>
        internal DisplayRule displayRule = (DisplayRule)(-1);

        /// <summary>原版 `MissionInfoDisplay.Initialize` 里那个布尔（`MissionInfoDisplay__Initialize.c`，逐句）：
        /// <code>
        /// if ((byte)((uint)displayRule >> 1) &amp; isComplete &amp; 1) != 0)  show = true;      // WhenComplete &amp;&amp; IsComplete
        /// else if ((displayRule &amp; 1) == 0)                         show = false;
        /// else                                                       show = !isComplete;  // WhenActive &amp;&amp; !IsComplete
        /// </code>
        /// ⚠️ 中间那一支**不是可省的 else**：`displayRule = 0` 时两条都不命中 ⇒ 恒 `false`（藏）。</summary>
        internal bool ShouldShow(bool isComplete)
        {
            int dr = (int)displayRule;
            if ((((uint)dr >> 1) & (isComplete ? 1u : 0u) & 1u) != 0u) return true;
            if ((dr & 1) == 0) return false;
            return !isComplete;
        }

        /// <summary>原版 `Initialize(mission, progress)`：算 `show` → `SetActive(show)` → **`if (show)` 才调 `Setup`**。
        /// ⚠️ 我们的节点是**建树时按 `if (!claimed)` 决定建不建**（等价于 `SetActive(false)`：原版藏起来的那颗
        /// 我们根本不建）—— 这条规则**只有一处**（`MissionsTab.BuildDailyRow` 的 `claimed`），
        /// 本方法用于**自检与将来若要「恒建 + 开关」**的场合，⛔ 别在两处各判一遍。</summary>
        internal bool Initialize(MissionProgressView p)
        {
            bool show = ShouldShow(p.IsComplete);
            if (show) Setup(p);
            return show;
        }

        /// <summary>原版虚方法（vtable slot `+0x178`）。</summary>
        internal abstract void Setup(MissionProgressView p);
    }

    /// <summary>原版 `MissionBackgroundHighlighter`（`MissionBackgroundHighlighter.cs:6-43`；C 伪码
    /// `MissionBackgroundHighlighter__Setup.c` 35 行全是 body）。
    /// <para>**方法体只有一句语义**：`background.color = progress.CanCollect() ? collectableColor : normalColor;`
    /// —— 那 35 行里除了取 `progress` 与两个 `Color`（`this+0x30..0x3C` / `this+0x40..0x4C`）
    /// 就只有一次 `Image.set_color`（slot `+0x2a8`）。</para>
    /// <para>🔴 **两套颜色的真值 = prefab 实读，全库同值**（8 份 MB 逐份同值，例
    /// `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-443721620838148353.json`）：
    /// `normalColor = (0.4941176, 0.5686275, 0.9176471, 1)` ·
    /// `collectableColor = (1, 0.6666667, 0.3450980, 1)`。
    /// ⚠️ 我们原来那组四舍五入值 `(0.4941, 0.5686, 0.9176, 1)` / `(1, 0.6667, 0.3451, 1)` 与之**逐位只差舍入**
    /// （`0.4941176→0.4941` 等），本类改用**原版全精度**，屏幕上看不出差别、但断言以后可以按全精度写。</para>
    /// <para>⚠️ **`OnValidate()` 不复刻（player 不需要）**：它的语义是「编辑器里改了 Inspector 之后，
    /// 若 `normalColor` 还是默认值就取 `background.color`」——`OnValidate` **只在编辑器改 Inspector 时被调**，
    /// 运行期没有这条路径（`MissionBackgroundHighlighter__OnValidate.c`）。写在这里是为了让下个会话
    /// **别再把它当缺口查一遍**。</para></summary>
    internal sealed class MissionBackgroundHighlighter : MissionInfoDisplay
    {
        /// <summary>原版 `normalColor`（MB `-443721620838148353` 实读）。</summary>
        internal static readonly Color NormalColor = new Color(0.4941176f, 0.5686275f, 0.9176471f, 1f);
        /// <summary>原版 `collectableColor`（同上一份 MB 实读）。</summary>
        internal static readonly Color CollectableColor = new Color(1f, 0.6666667f, 0.3450980f, 1f);

        internal Color normalColor = NormalColor;
        internal Color collectableColor = CollectableColor;
        /// <summary>原版 `Image background`（`this+0x28`）。</summary>
        internal ImageQuad background;

        /// <summary>原版那句三元的**唯一实现**（`RowTint` / 断言 / `Setup` 全走它 ⇒ 判据只有一份）。
        /// ⚠️ **`CanCollect` 在我们这一侧的来源是 `DailyData.CanCollectDaily`**（= 三态守卫那一份布尔），
        /// 而不是原版三层合取本身 —— 那三层（`canCollect` + `loopable` + 逐奖励 `CanCollect`）的**前两层在服务端**，
        /// 第三层我们没有「逐奖励可领」这个概念 ⇒ 我们的 `St == Collectable` 是这三层在**我们数据模型里的合取结果**。
        /// 这一句就是 `DailyData.cs:1254-1280` 那段注释的复述，⛔ 不要假装逐句一致。</summary>
        internal static Color ColorFor(MissionProgressView p) { return p.CanCollect ? CollectableColor : NormalColor; }

        internal override void Setup(MissionProgressView p)
        {
            if (background == null)
            {
                // 原版这一支会抛空引用（`MissionBackgroundHighlighter__Setup.c` 末句 `FUN_1803f47a0`）。
                Debug.LogWarning("[MissionInfoDisplays] `MissionBackgroundHighlighter.Setup` 的 `background` 是 null"
                               + " ⇒ **那一行底图不会换色**（原版在这里会抛空引用；红线：不许静默失败）");
                return;
            }
            background.SetTint(p.CanCollect ? collectableColor : normalColor);
        }
    }

    /// <summary>原版 `MissionCounterDisplay`（`MissionCounterDisplay.cs:7-73`；C 伪码 `MissionCounterDisplay__Setup.c`）。
    /// <para>**方法体逐句**（ILSpy `:21-53`）：</para>
    /// <code>
    /// int maxValue = progress.MaxValue;
    /// if (progress.currentValue >= maxValue &amp;&amp; displayCompletedMessage) {
    ///     progressText.text = GetTranslation(completedMessage, FixForRTL: true, 0, ignoreRTLnumbers: true, …);
    ///     return;                                    // ← 注意是【早退】，图标那一段不跑
    /// }
    /// progressText.text = string.Format(progressTextFormat, progress.currentValue, progress.MaxValue);
    /// if ((bool)armyIcon) {                          // Unity Object 的隐式 bool
    ///     var asset = AssetLocator.GetAsset&lt;MissionChallenge&gt;(progress.challengeId);
    ///     CardArmy army = asset.Army;
    ///     armyIcon.sprite = ArmyUtilities.Instance.GetArmyIcon(army);
    ///     armyIcon.gameObject.SetActive(army != CardArmy.Neutral);   // Neutral(0) ⇒ 整格关
    /// }
    /// </code>
    /// <para>**出厂值**（ILSpy ctor `:55-73`；`dump.cs:61661-61684` 的字段序
    /// `progressTextFormat(+0x28) · progressText(+0x30) · displayCompletedMessage(+0x38) ·
    /// completedMessage(+0x40) · targetIcon(+0x48) · armyIcon(+0x50)`）：
    /// ctor **只写两个** —— `progressTextFormat = "{0}/{1}"` · `completedMessage = "Missions/Completed"`
    /// ⇒ `displayCompletedMessage` 的[C#]出厂值是 **false**（[C++]零初始化）。
    /// ⚠️ **判据文档 `判据/B93_Q2_日常190.md` §2.2 ④ 把这一格写成 `= true`** —— 那是**实例值**
    /// （每日行 MB `3476392019656054992` = **1**），**不是出厂值**；两处口径不同，按本行留痕（铁律 5）。</para>
    /// <para>**两个实例的真值**（prefab 实读）：
    /// 每日行 `MonoBehaviour_3476392019656054992` = `{0}/{1}` + `displayCompletedMessage 1`；
    /// 骷髅卡 `MonoBehaviour_-3349471971615803659`（另 `-7021215977654184193` / `1064684212320868610` …）
    /// = `x{0}` + **0** ⇒ **到顶也不换词条**。两类的 `targetIcon` / `armyIcon` **都是 null**。</para>
    /// <para>🔴 **`armyIcon` 那一支在原版是【死支】**（prefab 两颗图标槽恒 null）—— 按「原版含死支也照抄」的口径
    /// **照抄不删**，并用断言锚住「槽位为 null ⇒ 不建图标格」（`B93_Q2` §4·3 已闭合这一格）。</para></summary>
    internal sealed class MissionCounterDisplay : MissionInfoDisplay
    {
        /// <summary>原版 `progressTextFormat`（出厂 `"{0}/{1}"`；骷髅卡实例 = `"x{0}"`）。</summary>
        internal string progressTextFormat = FormatDaily;

        /// <summary>每日任务行的 `progressTextFormat` —— **prefab 实读**，MB `3476392019656054992`（`displayCompletedMessage = 1`）。</summary>
        internal const string FormatDaily = "{0}/{1}";
        /// <summary>骷髅卡的 `progressTextFormat` —— **prefab 实读**，MB `-3349471971615803659`
        /// （另 `-7021215977654184193` / `1064684212320868610` 同值；`displayCompletedMessage = 0` ⇒ 到顶也不换词条）。</summary>
        internal const string FormatSkulls = "x{0}";
        /// <summary>原版 `displayCompletedMessage`（[C++]零初始化 ⇒ false；每日行实例 = 1）。</summary>
        internal bool displayCompletedMessage;
        /// <summary>原版 `completedMessage`（出厂 `"Missions/Completed"` —— **这是 I2 词条【键】**，不是文案）。
        /// ⚠️ 实际取值请用 `DailyData.CompletedMessage`（同值、只有一处），本字段只是复刻字段本身。</summary>
        internal string completedMessage = DailyData.CompletedMessage;
        /// <summary>原版 `TextMeshProUGUI progressText`（`this+0x30`）。</summary>
        internal Label progressText;
        /// <summary>原版 `Image targetIcon`（`this+0x48`）—— 这两个槽在**本页 prefab 里恒 null**（两实例实读）。</summary>
        internal ImageQuad targetIcon;
        /// <summary>原版 `Image armyIcon`（`this+0x50`）—— 恒 null（见上）。</summary>
        internal ImageQuad armyIcon;
        /// <summary>`armyIcon` 自己的 `gameObject`（原版 `SetActive` 打在它上面）。</summary>
        internal GameObject armyIconGo;

        /// <summary>原版 `Setup` 里**产生那个串**的那一段（纯函数 ⇒ `DailyData.DailyCounterText` /
        /// `SkullsCounter` / 断言可以共用同一份判据）。
        /// ⚠️ 原版第二条分支走 `string.Format(progressTextFormat, currentValue, MaxValue)` ——
        /// `"x{0}"` 这种只吃第一个占位符的格式串**多传一个实参是合法的**（`string.Format` 忽略多余的）。</summary>
        internal static string Text(MissionProgressView p, string progressTextFormat, bool displayCompletedMessage,
                                    string completedMessage)
        {
            if (p.CurrentValue >= p.MaxValue && displayCompletedMessage) return Loc.T(completedMessage);
            return string.Format(progressTextFormat, p.CurrentValue, p.MaxValue);
        }

        internal override void Setup(MissionProgressView p)
        {
            string text = Text(p, progressTextFormat, displayCompletedMessage, completedMessage);
            if (progressText != null) progressText.SetText(text);
            else Debug.LogWarning("[MissionInfoDisplays] `MissionCounterDisplay.Setup` 的 `progressText` 是 null"
                                + " ⇒ 进度那一格**不会显示**（原版在这里会抛空引用）");

            // 原版 `if ((bool)armyIcon)` —— Unity `Object` 的隐式 bool ⇒ null 即 false。
            if (armyIcon == null) return;
            // 原版这一支 = ① 按 `challenge.Army` 换 sprite ② `army != Neutral(0)` 才显示。
            // 我们的 sprite 在**建树那一刻**由调用点选好（`ImageQuad` 的图不可换）⇒ 这里只复刻**显隐那一半**，
            // 换图那一半的等价物在 `MissionsTab.BuildSkullsCard`（`SkullsArmy()` ⇒ 图标名）。
            if (armyIconGo != null) armyIconGo.SetActive(p.Army != 0);
        }
    }

    /// <summary>原版 `MissionHeaderDisplay`（`MissionHeaderDisplay.cs:8-45`；C 伪码 `MissionHeaderDisplay__Setup.c`）。
    /// <para>**方法体**（ILSpy `:18-38`，其中三处被 ILSpy 降级，见下）：
    /// `title.text = LiveOpsAssetUtility.GetLabel(mission, EventLabelReferenceType.Title)`；
    /// `if ((bool)tooltip) tooltip.Setup(GetLabel(mission, EventLabelReferenceType.Description))`；
    /// 末尾那一支 = **`mission.IsPremium` 为真时 `gradientHeader.EffectGradient = premiumColor`** ——
    /// ✅ **2026-10-28（`B93_N4`）判据闭合**：`.c:34` 是 `FUN_1800021f0(10, DAT_1842a8580, mission)`
    /// = 一次 **IMission 接口调用且结果为 bool**（同 helper 在别处确实解成接口调用）⇒ 只能是 `get_IsPremium`。</para>
    /// <para>🔴 **我们只落地「标题」那一半**：`tooltip`（内容 = 这条任务的描述）与 `gradientHeader`
    /// （premium 渐变）**两支都没有落点**（tooltip 那颗节点我们**有** = `info`，差的是文案来源；
    /// premium 渐变要补得改 `MissionsTab.BuildMissionHeader`）。🔴 **2026-10-29 `B94_B4` 更正**：原文写
    /// 「我们那颗 ⓘ **不是**原版 `header.tooltip`」**不成立** —— prefab 实读 `tooltip` 就指向 `info` 节点。</para></summary>
    internal sealed class MissionHeaderDisplay : MissionInfoDisplay
    {
        /// <summary>原版 `TextMeshProUGUI title`（`this+0x28`）。</summary>
        internal Label title;

        /// <summary>原版 `GetLabel(mission, Title)` 在我们这一侧的等价物：词条键 → 文案。
        /// ⚠️ 原版的 label 是**每条任务各带一份**（服务端下发）；我们的是**每个卡头固定一个键**
        /// （`Missions/Title/DailyMissions` / `DailySkulls` / `LoginBonus` …）⇒ 调用点传键，本方法取文案。</summary>
        internal static string TitleText(string term)
        {
            if (string.IsNullOrEmpty(term)) return "";
            return Loc.T(term);
        }

        internal override void Setup(MissionProgressView p)
        {
            // 原版这一支只写 title（`title` 的文案由 `GetLabel` 给出、与 progress 无关）
            // ⇒ 我们这一侧没有任何随 progress 变的量；**这一句注释就是判据**，⛔ 别为了「看起来在做」编一个。
            if (title == null)
                Debug.LogWarning("[MissionInfoDisplays] `MissionHeaderDisplay.Setup` 的 `title` 是 null ⇒ 卡头标题不写");
        }
    }

    /// <summary>原版 `MissionReRollButton`（`MissionReRollButton.cs:7-125`；C 伪码 `MissionReRollButton__Setup.c`）。
    /// <para>**显隐那一句**（ILSpy `:51-96`，其中三段被降级成 `Unmanaged memory load`）：
    /// `gameObject.SetActive(progress 的价格.Reference.amount &gt; 0)`；
    /// 点击那一句 = `WindowsManager.OpenWindow(reRollPopup, new MissionReRollPopupContext(mission, challenge))`。</para>
    /// <para>🔴 **完整规则 = `!IsComplete &amp;&amp; Price.amount &gt; 0`**：
    /// 前半由**基类** `displayRule = 1 (WhenActive)` 给（`MissionReRollButton : MissionInfoDisplay`，
    /// 全包 8 个实例实测都是 1），后半是上面那一句；
    /// ⚠️ **后半我们恒真**：`Price` 是服务端下发（`MissionData.get_RerollPrice` 是错桩）⇒ 本地判不了，
    /// 如实恒真、⛔ 不假装判过（`MissionsTab.BuildDailyRow` 垃圾桶那一段已有同样的留痕）。</para></summary>
    internal sealed class MissionReRollButton : MissionInfoDisplay
    {
        /// <summary>原版 `Setup` 里 `SetActive(...)` 那个布尔的**我们这一侧**的口径。
        /// <para>⚠️ 原版那一句里**没有** `!IsComplete` —— 那半在基类 `Initialize` 里（`displayRule`）。
        /// 之所以在这里**显式带上**，是因为我们的建树代码用的是 `if (!claimed)` 一处判断（没有 `SetActive` 层）
        /// ⇒ 两半必须同时写在同一个谓词里，否则重摇钮会在「已领取」那一行又冒出来。
        /// 判据不变、只是**合并点换了地方**，并写在这里留痕。</para></summary>
        internal static bool ShouldShow(MissionProgressView p)
        {
            if (p.IsComplete) return false;      // = 基类 displayRule=1 (WhenActive) 的 `!IsComplete`
            return PriceAmountPositive;          // = 原版 `Price.amount > 0`（本地恒真，见 summary）
        }

        /// <summary>原版 `Price.amount &gt; 0` 在本地**判不了**（服务端下发）⇒ 恒真。**这一句就是那个恒真**，
        /// ⛔ 别在别处再写一个字面量 `true`（要改判据只改这里）。</summary>
        internal const bool PriceAmountPositive = true;

        internal override void Setup(MissionProgressView p)
        {
            // 原版这一支只有两句：`SetActive(...)` 与 `button.onClick.AddListener(...)`。
            // 前者 = `ShouldShow`（调用点在 `MissionsTab.BuildDailyRow`）；后者 = 那颗命中区（`AddHit` → `OpenReroll`）。
            if (!ShouldShow(p))
                Debug.LogWarning("[MissionInfoDisplays] `MissionReRollButton.Setup`：这一行不该显示重摇钮"
                               + "（调用点应当已经跳过它 —— 走到这里说明两处判据打架了）");
        }
    }

    /// <summary>原版 `MissionMilestoneStep`（`MissionMilestoneStep.cs:7-135`；C 伪码 `MissionMilestoneStep__Setup.c`）。
    /// <para>**`Setup` 四件**（ILSpy `:51-100`，逐句）：
    /// ① `text.text = value.ToString()`（**阈值数字**，不是当前值）；
    /// ② `if (actionOnActive &amp; ChangeSprite) image.sprite = value &lt;= currentValue ? activeSprite : disabledSprite`；
    /// ③ `if ((actionOnActive &amp; ChangeOutline) &amp;&amp; outline != null) outline.effectColor = 达成 ? activeColor : disabledColor`；
    /// ④ 勾：`sprite = (actionOnActive &amp; DisplayCheckMark) &amp;&amp; 达成 ? activeCheckmark : disabledCheckmark`，
    ///    `checkMark.gameObject.SetActive(sprite != null)`，再 `checkMark.image.sprite = sprite`；
    ///    末尾 `if ((actionOnActive &amp; DisplayCheckMark) != 0) DisplayCheckmark(...)`（预览点击）。</para>
    /// <para>「达成」= **`value &lt;= currentValue`**（`obj = value - currentValue` 那串无符号比较）——
    /// 它正是 `MissionChallengeProgress.&lt;CurrentMilestone&gt;b__15_0`（`currentValue &lt; targetValue`）的**取反**
    /// ⇒ 全工程只留一份判据（见 <see cref="MissionChallengeProgress.CurrentMilestonePredicate"/>）。</para></summary>
    internal sealed class MissionMilestoneStep
    {
        /// <summary>原版 `[Flags] enum OnReach { ChangeOutline = 1, ChangeSprite = 2, DisplayCheckMark = 4 }`（ILSpy `:9-14`）。</summary>
        [System.Flags]
        internal enum OnReach { None = 0, ChangeOutline = 1, ChangeSprite = 2, DisplayCheckMark = 4 }

        /// <summary>原版 ①：格子里印的那个数 = **阈值本身**（`text.text = value.ToString()`）。</summary>
        internal static string ThresholdText(int value) { return value.ToString(); }

        /// <summary>原版那个「达成」谓词 = `value &lt;= currentValue`。
        /// ⚠️ 它是 <see cref="MissionChallengeProgress.CurrentMilestonePredicate"/> 的取反（**同一份判据的两面**）。</summary>
        internal static bool Reached(int value, int currentValue)
        {
            return !MissionChallengeProgress.CurrentMilestonePredicate(currentValue, value);
        }

        /// <summary>原版 ④ 的显隐：`active = sprite != null`，而
        /// `sprite = (flag &amp;&amp; reached) ? activeCheckmark : disabledCheckmark`。
        /// ⚠️ 注意「没置 flag」时用的是 **disabledCheckmark**（不是「不画」）—— 我们 `disabledCheckmark = null`
        /// （判据 = `MissionsTab.BuildMilestone` 里「未达成整件不显示」那一段），所以两支在本地同解。</summary>
        internal static bool CheckMarkVisible(OnReach actionOnActive, bool reached,
                                              bool hasActive, bool hasDisabled)
        {
            bool useActive = (actionOnActive & OnReach.DisplayCheckMark) != 0 && reached;
            return useActive ? hasActive : hasDisabled;
        }

        /// <summary>原版 ③ 的两个颜色（`MissionMilestoneStep.activeColor` / `disabledColor`，prefab 实读：
        /// 绿 `(0.3312554, 1, 0)` / 米黄 `(0.9176471, 0.7686275, 0.4823530)`）——
        /// 与 `MissionsTab.OutlineActive` / `OutlineDisabled` 是同一组值；那两个常量仍留在 `MissionsTab`
        /// （它们被其它断言按名字读），本方法只是把「按达成选哪一个」那一条**收成一处**。</summary>
        internal static Color OutlineColorFor(bool reached)
        {
            return reached ? new Color(0.3312554f, 1f, 0f, 1f) : new Color(0.9176471f, 0.7686275f, 0.4823530f, 1f);
        }
    }

    /// <summary>原版 `MissionRewardItem`（`MissionRewardItem.cs:4-27`；C 伪码 `MissionRewardItem__Setup.c`）。
    /// <para>**Setup 三句**：`amountText.text = info.Quantity.ToString()` →
    /// `SupportMethods.DestroyAllChildren(drawerHolder)` →
    /// `ItemDrawer.Draw(drawerHolder, info.Item, info.Quantity, DrawerOverride.Icon).ToggleShowInfoOnClick(false)`。
    /// ⚠️ **数量那一格【没有】** `"x"` 前缀（`Quantity.ToString()` 原文）；我们的等价物 = `BuildRewardCell` 的 `countText`。</para></summary>
    internal sealed class MissionRewardItem
    {
        /// <summary>原版 `amountText.text = info.Quantity.ToString()`。</summary>
        internal static string AmountText(int quantity) { return quantity.ToString(); }

        /// <summary>原版 `itemDrawer.ToggleShowInfoOnClick(toggle: false)` —— 奖励格**点不开道具详情**。
        /// 我们那几格本来就没有命中区（等价），这个常量只用来把口径写死在这里。</summary>
        internal const bool ShowInfoOnClick = false;
    }

    /// <summary>原版 `MissionRewardsDisplay`（`MissionRewardsDisplay.cs:7-273`；C 伪码 `MissionRewardsDisplay__Setup.c` 175 行）。
    /// <para>**Setup 的形状**：`DestroyAllChildren(transform)` → `progress.AvailableRewards()` →
    /// `CurrentMilestone`（`FirstOrDefault(t =&gt; currentValue &lt; t.targetValue) ?? LastOrDefault()`）→
    /// `RewardEventHandler.ApplyExtraRewards(mission, null)` →
    /// **`GroupBy(r =&gt; r.ItemId)`** → 逐组 `Instantiate(RewardsPrefab)` + `MissionRewardItem.Setup`。</para>
    /// <para>⇒ 我们要复刻的**判断**只有两件：① `CurrentMilestone` 那条谓词（见
    /// <see cref="MissionChallengeProgress"/>）② **同 `ItemId` 归并求和**（`GroupBy` + `Sum(Quantity)`）。
    /// 逐格 `Instantiate` 的**几何**在我们这一侧是 `MissionsTab.BuildRewardCell`（那是另一个类
    /// `MissionRewardItem.Setup` 的等价物），⛔ 别把版式塞进本类。</para></summary>
    internal sealed partial class MissionRewardsDisplay
    {
        /// <summary>一格奖励（原版一组 = 一个 `MissionRewardItem`）。</summary>
        internal struct Cell
        {
            internal string ItemId;
            internal int Quantity;
        }

        /// <summary>原版那句 `GroupBy(x =&gt; x.ItemId)` 的等价物：**同 `ItemId` 归并求和**，顺序 = **首次出现序**
        /// （LINQ `GroupBy` 保证按首次出现的顺序产出分组 ⇒ 我们的格子次序与产物顺序一致）。
        /// <para>⚠️ 我们的等价物用的是 `CampaignData.RewardSpec.Id`（= 图名）；原版是 `RewardInfo.ItemId`。
        /// 两者在「同一件东西 → 同一个字符串」这一点上同义。</para></summary>
        internal static List<Cell> MergeSameItem(List<Cell> rewards)
        {
            var outp = new List<Cell>();
            if (rewards == null) return outp;
            var at = new Dictionary<string, int>();
            for (int i = 0; i < rewards.Count; i++)
            {
                var r = rewards[i];
                if (string.IsNullOrEmpty(r.ItemId)) continue;   // 原版 `x.ItemId` 为 null 会抛空引用；我们跳过并保留这一句留痕
                int idx;
                if (at.TryGetValue(r.ItemId, out idx)) { outp[idx] = new Cell { ItemId = r.ItemId, Quantity = outp[idx].Quantity + r.Quantity }; }
                else { at[r.ItemId] = outp.Count; outp.Add(r); }
            }
            return outp;
        }
    }

    /// <summary>原版 `MissionTimerDisplay`（`MissionTimerDisplay.cs:8-111`；C 伪码 `MissionTimerDisplay__Update.c` 116 行）。
    /// <para>**`Update` 的文本部分**（ILSpy `:20-95`，逐句；这三段就是账 `:510` 的缺口）：
    /// <code>
    /// if (!isActiveAndEnabled || mission == null) return;
    /// // 目标时刻：
    /// //   willEndAt  &gt; 0  ⇒ MillisSinceEpochToDateTime(willEndAt)
    /// //   completedAt &gt; 0 ⇒ progress.GetChallengeRefillTime(mission)
    /// //   两者都没有      ⇒ （`Method not found @1800021F0` —— 那一支没闭合，见下）
    /// ts = target - PlayerDataManager.CurrentServerDateTime();
    /// if (ts.TotalSeconds &gt; 0) {
    ///     text = onlyShowNumber ? "{0}"
    ///          : (progress.IsComplete() ? GetTranslation("Missions/CompleteCounter", …)
    ///                                   : GetTranslation("Missions/CollectCounter", …));
    /// } else text = "";
    /// timeString = SupportMethods.GetTimeString(ts, eventTimer: true);
    /// this.text.text = string.Format(text, timeString);
    /// if ((bool)extraIcon) extraIcon.gameObject.SetActive(text.Contains("{0}"));
    /// </code>
    /// 🔴 **这份判据原来是「未闭合」的**（`B93_Q2` §3 第 7 条：两个 `DAT_` 键名未解、`+0x38` bool 未定）——
    /// **2026-10-28（`B93_N4`）现读已把四样全解出来**：两个键 = `Missions/CollectCounter` / `Missions/CompleteCounter`；
    /// `+0x38` bool = **`onlyShowNumber`**（字段序 `mission(+0x28) progress(+0x30) onlyShowNumber(+0x38)
    /// extraIcon(+0x40) text(+0x48)`）；`{0}` 就是 `data[this+0x40]` 那个「只有数字」档；
    /// `willEndAt==0 &amp;&amp; completedAt==0` 那一支 = **`mission.GetNextRefreshTime()`**（判断链见文末 `B93_N4` 块）。
    /// ⇒ 账 `:510` 的 `Update` **三段全闭合**：格式串 = <see cref="Description"/>、`ts&lt;=0 ⇒ ""` ＋
    /// 拼装 = `TimerDisplay.UpdateTimer()`（**同一份判据，⛔ 别在本类再写一遍**）、图标 = <see cref="ExtraIconVisible"/>。</para></summary>
    internal sealed class MissionTimerDisplay : MissionInfoDisplay
    {
        /// <summary>原版 `Missions/CollectCounter`（官方表 en `Ready to collect in {0}` / zh `准备在 {0} 收集`）。</summary>
        internal const string CollectCounterKey = "Missions/CollectCounter";
        /// <summary>原版 `Missions/CompleteCounter`（官方表 en `Available in {0}` / zh `在 {0} 可用`）。</summary>
        internal const string CompleteCounterKey = "Missions/CompleteCounter";

        /// <summary>原版 `onlyShowNumber`（`this+0x38`）—— 为真时描述串恒 `"{0}"`（只剩时间串）。</summary>
        internal bool onlyShowNumber;
        /// <summary>原版 `extraIcon`（`this+0x40`）—— 显隐 = `text.Contains("{0}")`。</summary>
        internal ImageQuad extraIcon;
        internal GameObject extraIconGo;

        /// <summary>原版「选哪条描述串」那一段（**纯函数** ⇒ `MissionsTab` 的四处倒计时与断言共用）。
        /// ⚠️ `IsComplete` 的语义 = **奖励已领取**（不是进度到顶）—— 所以本页 `timer`（`displayRule = 2`）
        /// 那颗件一旦显示，走的就是 **`Missions/CompleteCounter`** 那一支（"Available in …"）。
        /// 🔴 **这条落地会改屏上文案**：我们此前三处倒计时**一律**写死 `Missions/CollectCounter`
        /// （`MissionsTab.cs` 的每日行 / 登录卡 / 骷髅卡）⇒ 现在按 `IsComplete` 分档。</summary>
        internal static string Description(bool onlyShowNumber, bool isComplete)
        {
            if (onlyShowNumber) return "{0}";
            return Loc.T(isComplete ? CompleteCounterKey : CollectCounterKey);
        }

        /// <summary>原版 `extraIcon.gameObject.SetActive(text.Contains("{0}"))`。
        /// 我们这三颗倒计时**没有** `extraIcon` 这个节点（prefab 实读：那三处只有一颗 TMP）⇒ 默认不建。</summary>
        internal static bool ExtraIconVisible(string text) { return text != null && text.Contains("{0}"); }

        internal override void Setup(MissionProgressView p)
        {
            // 原版 `Setup` 只有两句：`this.mission = mission; this.progress = progress;`（ILSpy `:97-104`）
            // —— 文本在**每帧的 `Update`** 里算。我们这一侧的等价物 = `TimerDisplay`（它自己每秒重算），
            // 所以这里不需要保存任何东西；保留本重写只为让**方法名对齐**（判据可核）。
        }
    }

    // ============================================================ 追加到既有原版类的那两个成员
    //
    // 🔴 **为什么用 partial 另开一段而不是改原文件**：`MissionChallenge` / `MissionChallengeProgress`
    //    两个类**早就在** `Shell/MissionEvent.cs`（`public abstract partial class` / `public partial class`，
    //    连字段与 `IsComplete()` / `MaxValue` / `AvailableRewards()` 都齐）——
    //    账里那两条「缺同名方法」是**按 `DailyData` / `MissionsTab` 两个文件现读**得出的，
    //    没往 `MissionEvent.cs` 看（铁律：「判我们有没有这条 ⛔ 别只按原版方法名 grep」的反面：
    //    这里**同名类一直在**，只是不在本批白名单的文件里）。
    //    ⇒ 用 `partial` 在**本文件**补成员：不改 `MissionEvent.cs` 一个字节，也不与任何写手撞车。

    /// <summary>原版 `MissionChallenge` 的 `DisableChallenge()`（`decomp_full/MissionChallenge__DisableChallenge.c`，
    /// 逐句；虚槽编号见下）。
    /// <code>
    /// if (!initialized /*+0x50*/) return;                       // ← 一次性闩：没初始化过 ⇒ 什么都不做
    /// OnReset();                                                // 虚槽（`(**(code**)(*this + 0x1b8))(...)`）
    /// OnProgressUpdate /*+0x48*/ = &lt;静态空委托&gt;;               // 摘订阅
    /// initialized = false;
    /// </code>
    /// <para>🔴 **槽号口径（2026-10-28 现读，留痕）**：`B93_Q2` §5 把 `+0x1b8` 判成 `OnReset`；
    /// 而 `dump.cs:59613-59750` 的槽表里 `get_Army` = **Slot 7**（调用点实测 `+0x1A8`）⇒ 基准 `0x170`、
    /// 于是 `OnReset`(Slot 8) 落在 **`0x1B0`**、`OnInitialize`(Slot 9) 在 `0x1B8`。
    /// **两种算法差 8 字节**，而 `.c` 里那一跳**没有第二个可辨识的线索**（两次调用点都只出现在
    /// `DisableChallenge` 一处）⇒ 本类**不押槽号**，按 §5 已闭合的**语义**（摘订阅 + 复位闩）落地，
    /// 并把这条不一致写在这里。要坐实得反汇编 `0x1804B8D90` 附近那一段 vtable。</para></summary>
    public abstract partial class MissionChallenge
    {
        /// <summary>原版 `public void DisableChallenge()`（见类注释的逐句体）。</summary>
        public void DisableChallenge()
        {
            if (!initialized) return;                   // +0x50
            OnReset();
            OnProgressUpdate = null;                    // 原版赋的是**缓存的静态空委托**（`Action<object>___ctor(静态)`）；
                                                        // 我们 `RaiseProgress` 本来就是 null 检查 ⇒ `null` 与之等价（不改变任何可观察行为）
            initialized = false;
        }

        /// <summary>原版 `protected abstract void OnReset();`（`dump.cs:59693` **Slot 8**）。
        /// ⚠️ **这里只能是 `virtual` 空实现，不能照抄 `abstract`**：`MissionChallenge` 的两个子类
        /// （`DailyLoginChallenge` / `MissionComplete`，都在 `Shell/MissionEvent.cs`）**没有** `OnReset` 重写，
        /// 而那个文件不在本批白名单 ⇒ 加 `abstract` 会直接编不过（`CS0534`）。
        /// 🔴 **行为等价性**：本仓**没有任何子类重写它** ⇒ 这一跳恒空，与「原版两个子类的 `OnReset` 体」
        /// 之间的**差值就是将来要补的那一条**（⛔ 别把这条留痕删掉，删了就等于宣称已经逐句一致）。</summary>
        protected virtual void OnReset() { }
    }

    /// <summary>原版 `MissionChallengeProgress` 里**被任务卡用到的那两条判据**
    /// （类本体 + 字段 + `MaxValue` / `AvailableRewards()` / `IsComplete()` 都在 `Shell/MissionEvent.cs`）。
    /// <para>① `&lt;CurrentMilestone&gt;b__15_0`（`decomp_full/MissionChallengeProgress___CurrentMilestone_b__15_0.c`）：
    /// `return *(int*)(this+0x40) &lt; *(int*)(milestone+0x14)` ⇒ **`currentValue &lt; targetValue`**。
    /// ② `get_CurrentMilestone`（`decomp_full/MissionChallengeProgress__CurrentMilestone.c`）：
    /// `Enumerable.FirstOrDefault(谓词)`；取不到再 `LastOrDefault()` ⇒ **第一个「还没到」的档；全到了就是最后一档**。</para></summary>
    public partial class MissionChallengeProgress
    {
        /// <summary>原版闭包谓词本体（`currentValue &lt; targetValue`）。
        /// ⚠️ 语义是**「还没到这一档」**（不是「到了」）—— 用到「达成」的地方一律取反，见 `MissionMilestoneStep.Reached`。</summary>
        internal static bool CurrentMilestonePredicate(int currentValue, int targetValue)
        {
            return currentValue < targetValue;
        }

        /// <summary>原版 `get_CurrentMilestone`：在 `AvailableRewards()` 上取**第一个「还没到」的里程碑**，
        /// 一个都没有（全到了 / 空表）⇒ **最后一个**；空表 ⇒ `null`（原版 `LastOrDefault()` 同）。
        /// ⚠️ 原版那一支 `lVar3 = FirstOrDefault(...); if (lVar3 == 0) LastOrDefault(...)` ——
        /// **注意它是「取到就返回、取不到才回落」**，不是 `First ?? Last` 的短路写法（等价）。</summary>
        public ChallengeMilestone CurrentMilestone()
        {
            ChallengeMilestone last = null;
            foreach (var m in AvailableRewards())
            {
                if (m == null) continue;
                if (CurrentMilestonePredicate(currentValue, m.targetValue)) return m;
                last = m;
            }
            return last;
        }
    }

    // ============================================================ 追加区 · 2026-10-28（`B93_N4`）
    //
    // 🔴 **为什么追加在【文件尾】而不是插进各自的类体**：本文件有多条账按 `文件:行号` 钉着
    //    （`:125` / `:144` / `:256` / `:287` / `:363` / `:392` / `:452` / `:462` / `:497` / `:512` / `:534`）
    //    ⇒ 往中间插一行就整片漂移。这里用 `partial` 块追加，**既有行号一个都不动**。

    /// <summary>原版 `MissionRewardsDisplay.Setup(IMission, MissionChallengeProgress)`
    /// （`cpp2il/out_cpp2il_dev_ilspy/MissionRewardsDisplay.cs:56-260`；C 伪码
    /// `decomp_full/MissionRewardsDisplay__Setup.c` 175 行）—— 三段：
    /// <list type="number">
    /// <item>`SupportMethods.DestroyAllChildren(base.transform)` —— **重建**（⛔ 不是增量刷新）。</item>
    /// <item>奖励源 = **`LiveOpsManager.GetHandler&lt;RewardEventHandler&gt;().ApplyExtraRewards(mission, null)`**
    ///   （ILSpy `:110-114`）—— ⚠️ **不是**从 `progress.AvailableRewards()` 里取奖励：那两条
    ///   （`AvailableRewards()` + `CurrentMilestone()`）在那份 `.c` 里算完就**没再被读**（`obj2` 全程未用）。</item>
    /// <item>`GroupBy(r =&gt; r.ItemId)` → 逐组一个格子：`Count == 1` ⇒ 直接用那一份 `RewardInfo`；
    ///   `&gt; 1` ⇒ `new RewardInfo(item, Sum(Quantity), RewardTier.Basic, …)`（**同 id 求和**）。</item>
    /// </list>
    /// <para>🔴 **我们这一侧的对应关系（如实，⛔ 不是「已经一样」）**：
    /// ① = 建树期一次（`MissionsTab` / `RewardsWindow` 建那几格，没有 `DestroyAllChildren` 这一步）；
    /// ② = **没有落点**（`RewardEventHandler` 属账 676 `RewardService`，落点在 `DailyData`，不在本文件）
    ///    ⇒ 我们的奖励源是调用点给的 `Cell` 表（`DailyData.LoginRewardCellList`）；
    /// ③ = <see cref="MergeSameItem"/>（**同一份判据，本方法只是转调，⛔ 别写第二份**）。</para>
    /// <para>✅ **本方法已不再休眠**（2026-10-29 `B94_S2` 把 `DailyData.LoginRewardCellList` 的最后一行从
    /// `MergeSameItem(src)` 改成 `Setup(src)`，账 `:508`）。语义与 `MergeSameItem` **完全相同**
    /// （`Setup` 就是它 + ② 那一段说明）—— 现在的差别只是 ② 那句缺口**挂在生产路径上**了。</para></summary>
    internal sealed partial class MissionRewardsDisplay
    {
        /// <summary>原版 `Setup` 的第 ②③ 段（第 ① 段 = 建树期，见类注释）。
        /// ⛔ `MergeSameItem` 是唯一实现 —— 本方法只负责把「原版这里还有 `ApplyExtraRewards` 一跳」写死在一处。</summary>
        internal static List<Cell> Setup(List<Cell> rewardSource)
        {
            // 原版第 ② 段（`ApplyExtraRewards(mission, null)`）本仓没有落点 ⇒
            // 直接吃调用点给的奖励表。这一句注释就是那个缺口的落痕（账 508 的「差异」列同此）。
            return MergeSameItem(rewardSource);
        }
    }

    // ============================================================ `B93_N4` · 两处判据闭合（只记不写码）
    //
    // ① `MissionTimerDisplay.Update` 的 `willEndAt==0 && completedAt==0` 支（账 `:510` 最后一块）：
    //    `decomp_full/MissionTimerDisplay__Update.c:44` 现读 = `uVar4 = FUN_1800021f0(4, DAT_1842a8580, lVar6)`，
    //    `lVar6` = `*(this+0x28)` = **`mission`**，随后 `System_DateTime__op_Subtraction(uVar4, Now)` ⇒ **结果是一个 DateTime**。
    //    `DAT_1842a8580` = **`IMission`**（旁证：同 helper 在 `MissionHeaderDisplay__Setup.c:34` 用的是
    //    `FUN_1800021f0(10, DAT_1842a8580, mission)`，结果是 bool = `get_IsPremium`）⇒ 这是一次 **IMission 接口调用**。
    //    `IMission`（`cpp2il/out_cpp2il_dev_ilspy/IMission.cs`）里**唯一**返回 `DateTime` 的成员是
    //    **`GetNextRefreshTime()`** ⇒ 语义与「既没有结束时刻、也没有完成时刻 ⇒ 数到下次刷新」自洽。
    //    ⚠️ **槽号未坐实**：首参 4 与 10 落不进同一张「扁平化接口槽表」的任何一种朴素枚举（两种排法都对不上）
    //    ⇒ 成员身份**按返回类型排除法成立、槽号未坐实**（⛔ 别把这句话删掉说成「逐位解出」）。
    //    ⚠️ 我们这一侧**不需要**这一段代码：三处倒计时的目标时刻一律由调用点给（自建日界 `NextResetAt`）⇒ 该支恒不命中。
    //
    // ② `MissionHeaderDisplay.Setup` 的 premium 支（账 `:492`）：判据同上（`FUN_1800021f0(10, …)` = `get_IsPremium`），
    //    动作 = `gradientHeader.EffectGradient = premiumColor`。**节点我们没建** ⇒ 无落点
    //    （要补得在 `MissionsTab.BuildMissionHeader` 里多建一层 `Gradient2`，那在 `N1` 的文件）。

    // ============================================================ 判据更正 · 2026-10-29（`B94_B4`）
    //
    // ① 🔴 **`MissionHeaderDisplay` 的 `tooltip` 就是页头那颗 `info` 节点**（上面 `MissionHeaderDisplay`
    //    类注释里「我们卡头右边那颗 ⓘ …**不是**原版 `header.tooltip`」**已过期**，就地订正 —— 铁律 5）：
    //    prefab 实读 `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-6158290902691785974.json` 的
    //    `tooltip` = pathID `5316849761747358474` → `GameObject/info_-8160186231220055286.json`
    //    ⇒ `MissionsTab.BuildMissionHeader` 的 `AddInfoTooltip(inf, "Missions/Info/DailyMissionsHelp")` 挂的是
    //    **同一个节点**。⇒ 这一跳的残差**不是「节点没建」**，而是**文案来源**：原版 = `GetLabel(mission, Description)`
    //    （每任务一份、服务端下发），我们 = 固定帮助词条。
    // ② **premium 渐变的六键已读出**（同一份 JSON，`ExpansionPass`/`premiumColor` 那组）：
    //    key0 = `(0.4745, 0.3059, 0.1529, 1)` · key1 = 同 rgb、α `0.098` · key2..5 = 白/黑 α `0`。
    //    ⇒ 将来有 `IsPremium` 数据时**不必再回解一遍 prefab**；今天仍**无落点**（`MissionProgressView` 没有那一格）。
    //
    // ⚠️ 这两条不改代码，只改「判据/记录」——`MissionHeaderDisplay.Setup` 的行为**本来就只有写 title 一句**
    //    （见它的 `Setup` 方法体），上面两条是**审计口径**的更正。
}
