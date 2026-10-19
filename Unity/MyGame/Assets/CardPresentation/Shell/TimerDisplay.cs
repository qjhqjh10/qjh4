// TimerDisplay.cs —— 原版 `TimerDisplay` 的等效件（全工程**唯一一份**倒计时实现）
//
// ============================ 判据（唯一来源，别再另找） ============================
// · 反编译（第一权威）：
//   `d:/2/tools/decomp_full/TimerDisplay__{Awake,Update,UpdateTimer,Initialize,InitializeWitLocKey,
//   SetTimerDescriptionText,LocalizationChange}.c`
//   类桩（字段顺序 / 默认值）：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/TimerDisplay.cs`
// · 格式化（逐句移植）：`d:/2/tools/decomp_full/SupportMethods__GetTimeString.c`
//   签名（类桩 `SupportMethods.cs:126`）：
//   `GetTimeString(TimeSpan ts, bool eventTimer = false, bool clampTo0 = false, bool hide0Values = false)`
//   `TimerDisplay.UpdateTimer` 的调用点实读是 `GetTimeString(remaining, 1, 0, 0, 0)`
//   ⇒ `eventTimer = true` · `clampTo0 = false` · `hide0Values = false`。
// · 词条正文：`d:/4/Unity/数据/游戏数据/官方本地化_en+zh-CN.json`（官方表，2026-10-14 解出）。
//   `MainMenu/Time/{DH,HM,MS}` 两列原文（**逐字**）：
//     DH  en `{0:0}d {1:0}h`      zh-CN `{0:0}天 {1:0}小时`
//     HM  en `{0:0}h {1:0}m`      zh-CN `{0:0}小时 {1:0}分钟`
//     MS  en `{0:0}m {1:0}s`      zh-CN `{0:0}分钟 {1:0}秒`
//   ⚠️ `D_Only` / `H_Only` / `M_Only` 三条**官方表里没有**（表里只有 `MainMenu/Time/D`、`H`、`M`，
//      而那三条是 `{0:0}d ago` / `…ago` —— **不是**倒计时用的那一族）。⇒ 下面这三条格式是
//      按 DH/HM/MS 里出现的单位**机械拼出来**的，**我们挑的**（不是原版文案），改文案时别把它们当正本。
// · `CardShopTab.CheckTimer`（9 点规则）：`d:/2/tools/decomp_full/CardShopTab__CheckTimer.c:20-35`
//   （常量 `DAT_1834b32c0` 实读 = **9.0**）；施工图 `资料/阶段二_商店_原版规格.md` §四。
//
// ============================ 行为（照 `TimerDisplay.UpdateTimer`） ============================
// · 每秒重算一次。原版 `Update` 是 `frameCount % Application.targetFrameRate == 0`
//   ⇒ 约每秒一次；我们按 `Time.unscaledDeltaTime` 累计 1 秒。**批处理没有帧循环**
//   ⇒ `Attach` 当场先算一次（存盘的场景里就是对的文本，不依赖 `Update`）。
// · 剩余 = `target − now`。原版取 `PlayerDataManager.CurrentServerDateTime`；
//   **我们没有服务器** ⇒ 统一用**本机时钟** `DateTime.Now`（本项目允许的偏离，每个消费点都标了）。
// · 文本 = 描述串 + `GetTimeString(剩余)`。描述串里含 `{0}` 时走 `string.Format`，否则直接拼接
//   （原版那一对分支：`String.Contains(desc, "{0}")`）。
// · 描述串有两档（原版 `useExtraText` / `useLastMinutesText`）：
//   ① 常档 = `prefix`（原版 `extraText` 或 `Initialize` 的 `timerDescription`；商店那条是空串）；
//   ② 最后 5 分钟档 = `lastMinutesPrefix`（原版 `lastMinutesText`，阈值的常量 `DAT_1834b3380`
//      **实读 = 5.0 分钟**）。只有 `useLastMinutesText = 1` 的实例才有这一档 —— 我们接的消费点里
//      **商店**（`MonoBehaviour_-5363483528564307958`）与**单机能源活动窗**
//      （`MonoBehaviour_666837755613493937`，键 `Event_Description/Generic/EndsSoon`）是 1；
//      🔴 **2026-10-19（界面线 P05 · 铁律 5 就地订正）**：这里原来写「能源窗那一档**暂未接**
//      （该键正文不在官方本地化快照里）」—— **已过期**：当前官方表（10,854 条）里
//      `Event_Description/Generic/EndsSoon` = en `Ends soon` / zh `即将结束`，且
//      **能源窗与遭遇战窗两处都已接上** `lastMinutesPrefix`；其余为 0 或不使用。
// · 到点（剩余 <= 0）：原版把 timer 那颗 TMP 的 GameObject `SetActive(false)` 并**只触发一次**
//   `OnRunOutOfTime`。我们把**同一个 `Label` 的文本清空**（视觉等价；组件继续每秒 tick，
//   目标滚到下一档时会自己把字写回来 —— 原版那条路把 GameObject 关了，Update 也就停了）。
// · 目标时刻**取不到**：`Debug.LogWarning` **一次**（按 `what` 去重）+ 显示 `<prefix>--`。
//   ⛔ 绝不编一个日期顶上（用户口径：不许静默给假数）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `TimerDisplay` 的等效件。挂在**文字自己的 GameObject** 上（不是单独的 Timer 根），
    /// 由它每秒把 <see cref="Label"/> 的文本重算一遍。</summary>
    public class TimerDisplay : MonoBehaviour
    {
        // ============================================================ 常量（照原版）

        /// <summary>原版 `TimeSpan.FromMinutes((int)DAT_1834b3380)` —— 常量实读 = **5.0**。
        /// 语义 = `useLastMinutesText` 的阈值：剩余**小于** 5 分钟时，描述串换成 `lastMinutesText`。</summary>
        public const float LastMinutesThreshold = 5f;

        /// <summary>每秒重算一次（原版 `Update` 的 `frameCount % targetFrameRate == 0`）。</summary>
        const float TickSeconds = 1f;

        // ============================================================ 状态

        Label _label;
        System.Func<System.DateTime?> _target;
        string _prefix = "";
        string _lastMinutesPrefix;        // null = 没有这一档（原版 `useLastMinutesText = 0`）
        string _what = "TimerDisplay";    // 出声时用（哪个消费点）
        string _unknownReason;
        bool _expired;
        float _acc;

        /// <summary>到点那一拍触发一次（原版 `TimerDisplay.OnRunOutOfTime`）。</summary>
        public event System.Action OnRunOutOfTime;

        /// <summary>自检/诊断口：这一件当前是否“到点”。</summary>
        public bool Expired { get { return _expired; } }
        /// <summary>自检/诊断口：上一次写进 Label 的文本。</summary>
        public string LastText { get; private set; }

        // ============================================================ 挂接

        /// <summary>把一件倒计时挂到某个已经建好的文字上，并**当场算一次**。
        /// <para><paramref name="target"/> 每次 tick 都会重新取 —— 商店那种「过了 9 点就滚到次日」、
        /// 日常那种「过了日界就滚到明天」的，不能只算一次。</para>
        /// <para><paramref name="target"/> 为 <c>null</c>（或它返回 <c>null</c>）= **目标时刻未知**
        /// ⇒ 出声一次 + `<paramref name="prefix"/>--`。⛔ 不许自己编一个日期。</para>
        /// <para><paramref name="lastMinutesPrefix"/> 非空时启用原版那一档：剩余 &lt; 5 分钟时描述串换成它。</para></summary>
        public static TimerDisplay Attach(Label label, System.Func<System.DateTime?> target,
                                          string prefix, string what, string unknownReason,
                                          string lastMinutesPrefix = null)
        {
            if (label == null)
            {
                Debug.LogWarning("[TimerDisplay] " + (what ?? "?") + "：文字没建出来（Label = null）⇒ 倒计时没挂上");
                return null;
            }
            var td = label.GetComponent<TimerDisplay>();
            if (td == null) td = label.gameObject.AddComponent<TimerDisplay>();
            td._label = label;
            td._target = target;
            td._prefix = prefix ?? "";
            td._what = string.IsNullOrEmpty(what) ? label.gameObject.name : what;
            td._unknownReason = unknownReason;
            td._lastMinutesPrefix = lastMinutesPrefix;
            td._expired = false;
            td.Refresh();
            return td;
        }

        // ============================================================ 每帧

        void Update()
        {
            _acc += Time.unscaledDeltaTime;
            if (_acc < TickSeconds) return;
            _acc = 0f;                       // 原版是「每 N 帧一次」，不需要补差；这里也不补
            Refresh();
        }

        /// <summary>重算一次并写进 Label。`Attach` 会调一次；每秒由 `Update` 再调。</summary>
        public void Refresh()
        {
            if (_label == null) return;
            var now = System.DateTime.Now;   // ⚠️ 本机时钟（我们没有服务器时间 —— 见文件头）
            var target = _target != null ? _target() : null;

            if (!target.HasValue)
            {
                WarnUnknownOnce(_what, _unknownReason);
                _expired = false;
                SetText(Compose(_prefix, "--"));   // 🆕 2026-10-18：同 `TextFor` —— 带 `{0}` 的词条要填参
                return;
            }

            var remain = target.Value - now;
            if (remain <= System.TimeSpan.Zero)
            {
                // 原版：`timer.gameObject.SetActive(false)` + `OnRunOutOfTime` 只发一次。
                // 我们清空同一颗 Label 的字（见文件头「行为」那一段）。
                if (!_expired)
                {
                    _expired = true;
                    SetText("");
                    var cb = OnRunOutOfTime;
                    if (cb != null) cb();
                }
                return;
            }

            _expired = false;
            string desc = _prefix;
            if (_lastMinutesPrefix != null && remain < System.TimeSpan.FromMinutes(LastMinutesThreshold))
                desc = _lastMinutesPrefix;

            string time = FormatTimeString(remain, eventTimer: true);
            SetText(Compose(desc, time));
        }

        void SetText(string text)
        {
            LastText = text ?? "";
            _label.SetText(LastText);
        }

        // ============================================================ 文本

        /// <summary>描述串 + 时间串。原版：`desc.Contains("{0}") ? string.Format(desc, time) : desc + time`。
        /// ⚠️ 拼接那条**不加空格** —— 原版就是 `String.Concat`；带空格的写法由词条本身带（如 `Next: `）。</summary>
        public static string Compose(string description, string timeString)
        {
            if (string.IsNullOrEmpty(description)) return timeString ?? "";
            return description.Contains("{0}")
                 ? string.Format(description, timeString)
                 : description + timeString;
        }

        /// <summary>`SupportMethods.GetTimeString` 的**逐句移植**（`decomp_full/SupportMethods__GetTimeString.c`）。
        /// <para>分支（按原版实读，`eventTimer` / `clampTo0` / `hide0Values` 三个开关）：</para>
        /// <list type="bullet">
        /// <item>`clampTo0` 且 ts &lt; 0 ⇒ ts = `TimeSpan.Zero`。</item>
        /// <item>`ts.TotalDays &gt; 1` **且** `eventTimer` ⇒ 天档：`DH(Days, Hours)`；
        ///       `hide0Values` 且 `Hours == 0` ⇒ `D_Only(Days)`。</item>
        /// <item>否则 `ts.TotalHours &gt; 1` ⇒ 小时档：`hide0Values == false` 且 `Minutes == 0` ⇒
        ///       `H_Only((int)TotalHours)`；其余 `HM((int)TotalHours, Minutes)`。</item>
        /// <item>否则 ⇒ 分钟档：`hide0Values == false` ⇒ `MS(Minutes, Seconds)`；
        ///       否则 `Seconds == 0` ⇒ `M_Only(Minutes)`，其余 `MS(Minutes, Seconds)`。</item>
        /// </list>
        /// <para>⚠️ 边界照原版**一个都不改**：`TotalDays &gt; 1` 和 `TotalHours &gt; 1` 都是**严格大于** ⇒
        /// 正好 24h 会落到小时档、画成 `24h 0m`；`Minutes == 0` 只在「小时档」才折成 `H_Only`。</para></summary>
        public static string FormatTimeString(System.TimeSpan ts, bool eventTimer = false,
                                              bool clampTo0 = false, bool hide0Values = false)
        {
            if (clampTo0 && ts < System.TimeSpan.Zero) ts = System.TimeSpan.Zero;

            if (ts.TotalDays > 1d && eventTimer)
            {
                if (hide0Values && ts.Hours == 0) return string.Format(FmtDOnly(), ts.Days);
                return string.Format(FmtDH(), ts.Days, ts.Hours);
            }

            if (ts.TotalHours > 1d)
            {
                int hours = (int)ts.TotalHours;
                if (!hide0Values && ts.Minutes == 0) return string.Format(FmtHOnly(), hours);
                return string.Format(FmtHM(), hours, ts.Minutes);
            }

            if (!hide0Values) return string.Format(FmtMS(), ts.Minutes, ts.Seconds);
            if (ts.Seconds == 0) return string.Format(FmtMOnly(), ts.Minutes);
            return string.Format(FmtMS(), ts.Minutes, ts.Seconds);
        }

        // ---- 六条格式（en = 官方表原文；zh = 官方表原文；`_Only` 三条 = 我们按单位机械拼的）----

        static bool Zh { get { return Loc.Current == AvailableLanguages.Chinese; } }

        static string FmtDH() { return Zh ? "{0:0}天 {1:0}小时" : "{0:0}d {1:0}h"; }      // 官方
        static string FmtHM() { return Zh ? "{0:0}小时 {1:0}分钟" : "{0:0}h {1:0}m"; }    // 官方
        static string FmtMS() { return Zh ? "{0:0}分钟 {1:0}秒" : "{0:0}m {1:0}s"; }      // 官方
        static string FmtDOnly() { return Zh ? "{0:0}天" : "{0:0}d"; }                    // ⚠️ 我们挑的
        static string FmtHOnly() { return Zh ? "{0:0}小时" : "{0:0}h"; }                  // ⚠️ 我们挑的
        static string FmtMOnly() { return Zh ? "{0:0}分钟" : "{0:0}m"; }                  // ⚠️ 我们挑的

        /// <summary>原版 `MainMenu/LastMinutes` 的正文（官方表：en `Last Minutes` · zh-CN `最后时刻`）。
        /// 我们接的消费点里，`useLastMinutesText = 1` 的是**商店**
        /// （`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-5363483528564307958.json` 实读，
        /// 键就是 `MainMenu/LastMinutes`）与**单机能源活动窗**（`MonoBehaviour_666837755613493937`，
        /// 键 `Event_Description/Generic/EndsSoon` —— 🔴 **2026-10-19（界面线 P05）就地订正**：这里原来写
        /// 「**正文不在官方表快照里** ⇒ 那一档暂未接」，**已过期**：当前官方表里该键 =
        /// en `Ends soon` / zh `即将结束`，能源窗与遭遇战窗两处**都已接上**）。</summary>
        public static string LastMinutesText()
        { return Zh ? "最后时刻" : "Last Minutes"; }

        // ============================================================ 目标时刻

        /// <summary>商店刷新时刻 = **`CardShopTab.CheckTimer` 的 9 点规则**：
        /// `< 9 点取当天、否则次日`，再 `AddHours(9)`（`decomp_full/CardShopTab__CheckTimer.c:20-35`；
        /// 常量 `DAT_1834b32c0` 实读 = 9.0）。
        /// <para>⚠️ 原版那个 `now` 是 `PlayerDataManager.CurrentServerDateTime`；**我们没有服务器** ⇒
        /// 传进来的 `now` 一律用本机时钟（调用点写的是 `DateTime.Now`）。</para></summary>
        public static System.DateTime ShopRefreshAt(System.DateTime now)
        {
            return now.Date.AddDays(now.Hour < 9 ? 0 : 1).AddHours(9);
        }

        /// <summary>商店 `Time` 那一格的文本（只剩时间串；描述串是单独的 `RefreshText` 节点）。
        /// ⚠️ Items 页原版走 `ItemShopTab.CheckTimer` 的 `LiveOpsUtility.IsTimedEvent(store)`，
        /// 我们没有那套 store 数据 ⇒ **三页统一按 Cards 页的 9 点规则**（**我们挑的**，见报告）。</summary>
        public static string ShopRefreshText()
        {
            var now = System.DateTime.Now;
            return FormatTimeString(ShopRefreshAt(now) - now, eventTimer: true);
        }

        /// <summary>一次性算好的文本（给**没有挂组件**的静态口用，如 `DailyData.RewardTimerText()`）。
        /// <para>`target == null` ⇒ 出声一次 + `<prefix>--`；已到点 ⇒ 空串（同原版 `SetActive(false)` 的观感）。</para></summary>
        public static string TextFor(System.DateTime? target, string prefix, string what, string unknownReason)
        {
            if (!target.HasValue)
            {
                WarnUnknownOnce(what, unknownReason);
                // 🔴 **2026-10-18（第五十一会话 · 真机抓到 `在 {0} 结束--`）**：
                //   这一支原来直接 `prefix + "--"`，**没走 `Compose`** ⇒ 带 `{0}` 的词条
                //   （例 `Missions/ActiveCounter` =「在 {0} 结束」）会把 `{0}` 原样印出来。
                //   ⇒ 与有目标时刻那一支同一条路。
                return Compose(prefix, "--");
            }
            var remain = target.Value - System.DateTime.Now;
            if (remain <= System.TimeSpan.Zero) return "";
            return Compose(prefix, FormatTimeString(remain, eventTimer: true));
        }

        // ============================================================ 出声（按 what 去重）

        static readonly HashSet<string> _warnedUnknown = new HashSet<string>();

        /// <summary>「目标时刻取不到」的出声 —— **同一个 <paramref name="what"/> 只出一声**。
        /// 红线：不许静默给假数；也不许每秒刷屏。</summary>
        public static void WarnUnknownOnce(string what, string reason)
        {
            if (string.IsNullOrEmpty(what)) what = "TimerDisplay";
            if (!_warnedUnknown.Add(what)) return;
            Debug.LogWarning("[TimerDisplay] " + what + "：**目标时刻取不到**"
                             + (string.IsNullOrEmpty(reason) ? "" : "（" + reason + "）")
                             + " ⇒ 显示 `--` 兜底（**不编日期**；原版这一格由服务端下发）。");
        }
    }
}
