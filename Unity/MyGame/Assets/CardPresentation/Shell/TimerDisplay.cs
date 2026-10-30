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
//   ② 最后 5 分钟档 = `lastMinutesPrefix`（原版 `lastMinutesText`，阈值的常量 `DAT_1834b3380` **实读 = 5.0 分钟**）。
//      🔴 **2026-10-28（B92_N8）按 `.c` 订正**：原版把**时间那一格直接写成 `lastMinutesText` 译文本身**（不带数字）—— 我们原来当期前缀拼成「即将结束2分钟」，已改。只有 `useLastMinutesText = 1` 的实例才有这一档 —— 我们接的消费点里
//      **商店**（`MonoBehaviour_-5363483528564307958`）与**单机能源活动窗**
//      （`MonoBehaviour_666837755613493937`，键 `Event_Description/Generic/EndsSoon`）是 1；
//      🔴 **2026-10-19（界面线 P05 · 铁律 5 就地订正）**：这里原来写「能源窗那一档**暂未接**
//      （该键正文不在官方本地化快照里）」—— **已过期**：当前官方表（10,854 条）里
//      `Event_Description/Generic/EndsSoon` = en `Ends soon` / zh `即将结束`，且
//      **能源窗与遭遇战窗两处都已接上** `lastMinutesPrefix`；其余为 0 或不使用。
// · 到点（剩余 <= 0）：原版把 timer 那颗 TMP 的 GameObject `SetActive(false)` 并**只触发一次** `OnRunOutOfTime`。
//   🔴 **2026-10-28（B92_N8）按 `.c` 订正**：我们原来只清字不关 GameObject（「视觉等价」的自我说服）——
//   已改成**同一条路**：清字 + `SetActive(false)` + 一次性回调（GameObject 一关，`Update` 自然停）。
// · 目标时刻**取不到**：**`return;` —— 一个字都不写、不动 GameObject**（原版 `TimerDisplay__UpdateTimer.c` 第一条就是 `if (*(char*)(this+0x78) == '\0') return;`）。
//   🔴 **2026-10-30（`B99_A4` · 铁律 5 + §18·0 就地订正）**：原文写「`Debug.LogWarning` 一次 + 显示 `<prefix>--`」——**已过期**：`--` 是**我们做的、原版没有**的占位（反向清单 §五 #1）⇒ 已删；仍**出声一次**（⛔ 不静默失败），但⛔ 不编日期、也⛔ 不印 `--`。
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
        /// ⇒ **一个字都不写**（原版 `UpdateTimer` 那一支是 `return;`），只出声一次。⛔ 不印 `--`、⛔ 不编日期（2026-10-30 `B99_A4`）。</para>
        /// <para><paramref name="lastMinutesPrefix"/> 非空时启用原版那一档：剩余 &lt; 5 分钟时描述串换成它。</para></summary>
        public static TimerDisplay Attach(Label label, System.Func<System.DateTime?> target,
                                          string prefix, string what, string unknownReason,
                                          string lastMinutesPrefix = null, string prefixKey = null)
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
            td._prefix = prefix ?? ""; td._prefixKey = prefixKey;   // 同一行写完：插行会让本文件下游十几条账落点漂移（铁律 18·b）
            td._what = string.IsNullOrEmpty(what) ? label.gameObject.name : what;
            td._unknownReason = unknownReason;
            td._lastMinutesPrefix = lastMinutesPrefix;
            td._expired = false;
            td.UpdateTimer();
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

        /// <summary>重算一次并写进 Label。`Attach` 会调一次；每秒由 `Update` 再调。
        /// 🔴 **2026-10-28（B92_N8）**：本体已搬进与原型同名的 <see cref="UpdateTimer"/>（逐句对
        /// `decomp_full/TimerDisplay__UpdateTimer.c`），这里只留一个转发口（历史调用点还在用它）。
        /// ⚠️ 搬家时**顺带按 `.c` 改了两处行为**（原来是我们自己挑的，与 `.c` 不符）：
        ///   ① 最后 5 分钟档：原版把**时间那一格直接写成 `lastMinutesText` 译文本身**（不带数字），
        ///      我们原来把它当期前缀拼成「即将结束2分钟」；
        ///   ② 到点：原版除了清字还 `gameObject.SetActive(false)`，我们原来只清字。</summary>
        public void Refresh() { UpdateTimer(); }

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
        /// <para>`target == null` ⇒ 出声一次 + **只印描述串**（`Compose(prefix, "")`，⛔ 不印 `--`）；已到点 ⇒ 空串（同原版 `SetActive(false)` 的观感）。</para></summary>
        public static string TextFor(System.DateTime? target, string prefix, string what, string unknownReason)
        {
            if (!target.HasValue)
            {
                WarnUnknownOnce(what, unknownReason);
                // 🔴 **2026-10-30（`B99_A4` · 铁律 5 + §18·0 就地订正）**：这一支原来返回 `Compose(prefix, "--")`。
                //   原版 `TimerDisplay__UpdateTimer.c` 取不到目标时刻是 **`return;` 静默**（一个字都不印）——
                //   `--` 是**我们做的、原版没有**的占位 ⇒ 已删（反向清单 §五 #1）。⛔ 不许自己编一个日期。
                //   🔶 自己设计（非原版）：`TextFor(null, …)` 返回 `Compose(prefix, "")`（`{0}` 填空、模板照印、⛔ 不印 `--`、⛔ 不编日期）· 为什么必须自定：截止时刻来自 LiveServer，关服后判据在远端（原版这一格永远有服务端时间）⇒ 按「不给假数」只印模板 · 调度台 2026-10-30 裁定
                return Compose(prefix, "");
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
                             + " ⇒ **这一件不写任何字**（原版这一支是 `return;`；⛔ 不印 `--`、⛔ 不编日期）。");
        }

        // ============================================================ 换语言重解析（原版 LocalizationChange）
        // 🔴 声明在**类尾**（不是和别的状态字段排在一起）：`TimerDisplay.cs` 在账里被按 `文件:行号`
        //    引用了十几处（`账/日常_奖励.tsv` 那一族），往前面插行会让它们整段漂移（铁律 18·b）。
        //    C# 允许字段 / 方法声明在类体任意处。

        /// <summary>描述串的**本地化键**（原版 `TimerDisplay._timerDescription` @+0x88）。
        /// 非空 ⇒ 换语言时重取译文；为空 ⇒ 与原版同（`IsNullOrEmpty` 那一支什么都不做）。</summary>
        string _prefixKey;

        /// <summary>原版 **`TimerDisplay.LocalizationChange()`**，逐句
        /// （`decomp_full/TimerDisplay__LocalizationChange.c` 全文）：
        /// <code>if (!String.IsNullOrEmpty(_timerDescription))
        ///     SetTimerDescriptionText(LocalizationManager.GetTranslation(_timerDescription), false);</code>
        /// <para>⇒ 我们同一跳：键非空 ⇒ `Loc.T(键)` 重取译文写回描述串、立刻重算一次。</para>
        /// <para>⚠️ 原版由 `LocalizationManager` 在 `ChangedLanguage` 时回调订阅者；
        /// 我们的广播口 = `Loc.LanguageChanged`（`Core/Loc.cs` 类尾，`SetLanguage` **真改了值**才发）。
        /// 订阅在 <see cref="Awake"/>、注销在 <see cref="OnDestroy"/>（🔴 **2026-10-31（B104_P26）就地订正**：
        /// 原来这里写「`OnEnable` / `OnDisable`」—— 与本文件实际那两行不符）。</para></summary>
        public void LocalizationChange()
        {
            // 🆕 **2026-10-31（B104_P26 · P6）**：`Loc.LanguageChanged` 是静态事件，编辑模式下
            //   `DestroyImmediate` 不派 `OnDestroy` ⇒ 死实例照样被换语言广播打到（读 `_prefixKey`/`Refresh` 碰已销毁的 Label）。
            //   入口先判「我还活着」：死了 ⇒ 出声 + **当场退订** + return。
            if (this == null)
            {
                Debug.LogWarning("[TimerDisplay] 已销毁的实例仍被 `Loc.LanguageChanged` 打到 ⇒ **当场退订**并 return（不许静默）");
                Loc.LanguageChanged -= LocalizationChange;
                return;
            }
            if (string.IsNullOrEmpty(_prefixKey)) return;
            _prefix = Loc.T(_prefixKey);
            Refresh();
        }

        void Awake() { Loc.LanguageChanged += LocalizationChange; }        // 原版 `TimerDisplay__Awake.c` = add_OnLocalizeEvent
        void OnDestroy() { Loc.LanguageChanged -= LocalizationChange; }    // 原版 `TimerDisplay__OnDestroy.c` = remove_OnLocalizeEvent

        // ============================================================ 原版同名方法落点（B92_N8 · 2026-10-28）
        // 🔴 **为什么单开这一节**：账里 `TimerDisplay` 的那三行原来是「声明级等价」（落点 = `Attach` /
        //    `Compose` / `Refresh`），不满足 A 尺子（要求 **方法体逐句对过 + 落点真实存在**）。
        //    这里按 `decomp_full/TimerDisplay__{Initialize,SetTimerDescriptionText,UpdateTimer}.c`
        //    **逐句**落成三个与原型同名的方法，`Attach` / `Update` / `Refresh` 的路由改到它们上面。
        // ⚠️ 全部追加在**类尾**（本文件在账里被按 `文件:行号` 引用 4 处：92 / 125 / 173 / 231，
        //    都在本节之前）⇒ 零行号漂移（铁律 18·b）。

        /// <summary>原版 `TimerDisplay` 的**固定目标**（`+0x78` = `Nullable&lt;DateTime&gt;`，由 `Initialize` 写）。
        /// 为 `null` ⇒ 走 `Attach` 的 delegate 路径（每 tick 现取；见 <see cref="ResolveTarget"/>）。</summary>
        System.DateTime? _targetFixed;

        /// <summary>原版的**描述那一件**（`+0x28`，一颗**独立的** TMP）。当前 15 个消费点只给了一颗 Label
        /// ⇒ 一律 `null`，`SetTimerDescriptionText` 落到「单 Label 合并档」（那一支标了 `🔶 自己设计`）。</summary>
        Label _descLabel;

        /// <summary>原版 `TimerDisplay.Initialize(DateTime target, string timerDescription)` 的逐句等价。
        /// <para>判据全文（`decomp_full/TimerDisplay__Initialize.c`，逐句读过）：</para>
        /// <code>
        /// target = SupportMethods.MillisSinceEpochToDateTime(millis);   // 我们收 DateTime，省掉这一跳
        /// this._target = target;
        /// if (target - PlayerDataManager.CurrentServerDateTime &lt;= TimeSpan.Zero) this._hasRunOutOfTime = true;
        /// SetTimerDescriptionText(timerDescription);
        /// UpdateTimer();
        /// </code>
        /// <para>⚠️ 原版第一个实参是 **Unix 毫秒**（`+0x78` 存 `Nullable&lt;DateTime&gt;`）；我们收 `DateTime`
        /// —— 调用方本来就有 `DateTime`，多一跳转换没有意义（⭐ 这不是行为差异）。</para>
        /// <para>⚠️ 原版那口是 `PlayerDataManager.CurrentServerDateTime`；**我们没有服务器** ⇒ 本机时钟
        /// （本项目允许的偏离，每个消费点都标了）。</para></summary>
        public void Initialize(System.DateTime target, string timerDescription)
        {
            _target = null;                 // 固定目标优先（原版没有 delegate 这一档，见 `ResolveTarget`）
            _targetFixed = target;
            if (target - System.DateTime.Now <= System.TimeSpan.Zero) _expired = true;
            SetTimerDescriptionText(timerDescription);
            UpdateTimer();
        }

        /// <summary>原版 `TimerDisplay.SetTimerDescriptionText(string timerDescription)` 的逐句等价。
        /// <para>判据全文（`decomp_full/TimerDisplay__SetTimerDescriptionText.c`）：</para>
        /// <code>
        /// if (!this._descriptionText) return;                       // 组件不存在 ⇒ 什么都不做
        /// this._descriptionText.enabled = !String.IsNullOrEmpty(timerDescription);
        /// this._descriptionText.text    = timerDescription;
        /// </code>
        /// <para>⚠️ **原版描述是独立的一颗 TMP**（`+0x28`）。我们接的消费点只给了一颗 Label
        /// ⇒ 没有描述件时走「单 Label 合并档」：把它当 `extraText` 前缀、由 `UpdateTimer` 拼进同一格。</para></summary>
        public void SetTimerDescriptionText(string timerDescription)
        {
            if (_descLabel == null)
            {
                // 🔶 自己设计（非原版）：单 Label 合并档 · 原版描述是独立的一颗 TMP（`+0x28`），
                //   我们 15 个消费点只给了一颗 Label，没有描述件可 enable/disable ⇒ 把描述串存成前缀、
                //   由 `UpdateTimer` 拼进同一格（描述为空时只剩时间串，与原版把描述件 `enabled=false` 观感等价）·
                //   调度台 2026-10-28 裁定（B92_N8）
                _prefix = timerDescription ?? "";
                return;
            }
            _descLabel.enabled = !string.IsNullOrEmpty(timerDescription);
            _descLabel.SetText(timerDescription ?? "");
        }

        /// <summary>原版 `TimerDisplay.UpdateTimer()` 的逐句等价（`decomp_full/TimerDisplay__UpdateTimer.c`，
        /// 80 条语句 / 26 个分支 —— 这是本组件**唯一的**文本计算口）。
        /// <para>分支（逐句对过 `.c`）：</para>
        /// <list type="number">
        /// <item><c>if (target == null) return;</c> —— 原版**静默返回**；我们**照做**（不写字、不动 GameObject），只多一次出声（⛔ 不再印 `--`；2026-10-30 `B99_A4`）。</item>
        /// <item><b>最后 5 分钟档</b>（只有 `useLastMinutesText = 1` 的实例才有）：`0 &lt; remain &lt; 5min`
        ///       ⇒ **时间那一格直接写成 `lastMinutesText` 译文本身**（`.c` 尾部 `timeText.text = loc(lastMinutesText)`，
        ///       **不带倒计时数字**）。⚠️ 这一档原来我们把它当前缀用（拼成 `即将结束2分钟`），**与原版不符**，本轮按 `.c` 改。</item>
        /// <item>`remain &gt; 0` ⇒ `gameObject.SetActive(true)`，再写
        ///       `desc.Contains("{0}") ? String.Format(desc, t) : String.Concat(desc, t)`（`t = GetTimeString(remain,1,0,0,0)`）。</item>
        /// <item>`remain &lt;= 0` ⇒ `text = ""` → `gameObject.SetActive(false)` → **只发一次** `OnRunOutOfTime`
        ///       （原版 `+0x98` 那个闩；`Initialize` 里「已经到点」也会先把它置上）。</item>
        /// <item>尾部：原版若挂了 `LayoutGroupContentFixer`（`+0x70`）就 `FixLayout` —— **本工程没有这个组件**（如实记，不编）。</item>
        /// </list></summary>
        public void UpdateTimer()
        {
            // 老 `Refresh()` 的同一条守卫（原版那一格是 `+0x20` 的时间 TMP，取不到时原版走 throw）。
            if (_label == null) return;
            var target = ResolveTarget();
            if (!target.HasValue)
            {
                // 原版 `TimerDisplay__UpdateTimer.c` = `if (*(char *)(this+0x78) == '\0') return;`（静默：不写字、不动 GameObject）。
                WarnUnknownOnce(_what, _unknownReason);   // 只出声一次（按 `what` 去重）—— ⛔ 不再印 `--`（反向清单 §五 #1 · 2026-10-30 `B99_A4`）
                _expired = false;
                // ⛔ 不 `ShowLabel(true)`、⛔ 不 `SetText(...)`：调用方建 Label 时给的串原样留着（原版就是这条语义）。
                //   ⚠️ 原来这里是 `ShowLabel(true)` + `SetText(Compose(_prefix, "--"))` —— 那个 `--` 是**我们做的、原版没有**的占位（铁律 18·0）；有截止时刻时 `_targetFixed`/delegate 照样走到下面那几支，⛔ 不会把真倒计时冻住。
                return;
            }

            var remain = target.Value - System.DateTime.Now;   // ⚠️ 本机时钟（没有服务器，见文件头）

            // ---- 最后 5 分钟档（原版 `+0x50 != 0` 那一支）----
            if (_lastMinutesPrefix != null
                && remain > System.TimeSpan.Zero
                && remain < System.TimeSpan.FromMinutes(LastMinutesThreshold))
            {
                ShowLabel(true);
                SetText(_lastMinutesPrefix);
                return;
            }

            if (remain > System.TimeSpan.Zero)
            {
                ShowLabel(true);
                _expired = false;
                string time = FormatTimeString(remain, eventTimer: true);
                SetText(_prefix.Contains("{0}") ? string.Format(_prefix, time) : _prefix + time);
                return;
            }

            // ---- 到点（原版 `remain <= 0` 那一支）----
            SetText("");
            ShowLabel(false);
            if (_expired) return;
            _expired = true;
            var cb = OnRunOutOfTime;
            if (cb != null) cb();
        }

        /// <summary>目标时刻：`Initialize` 写死的那个优先，否则每 tick 问 `Attach` 交进来的 delegate。
        /// 🔶 **自己设计（非原版）**：原版目标在 `Initialize` 时**固定**（`+0x78` 一个 `Nullable&lt;DateTime&gt;`），
        /// 我们的窗比原版活得久（外壳常驻）⇒ 商店「过 9 点滚次日」/ 日常「过日界滚明天」必须**现取** ·
        /// 调度台 2026-10-28 裁定（B92_N8）。</summary>
        System.DateTime? ResolveTarget()
        {
            if (_targetFixed.HasValue) return _targetFixed;
            return _target != null ? _target() : (System.DateTime?)null;
        }

        void ShowLabel(bool on)
        {
            if (_label == null) return;
            var go = _label.gameObject;
            if (go == null) return;
            if (go.activeSelf != on) go.SetActive(on);
        }
    }

    // ============================================================ 原版 `Countdown` 组件（B94_S3 · 2026-10-29）
    //
    // 判据（逐句实读，别再另找）：
    //   · `d:/2/tools/decomp_full/Countdown__{Activate,Update,ChangeSpeed,Stop,GetSecondsLeft}.c`
    //   · 字段名/类型：`cpp2il/out_cpp2il_dev_ilspy/Countdown.cs`
    //     （`timerText` · `lastStop` · `timeElapsedBeforeStop` · `speedAdjustedTimeElapsed` · `running` ·
    //      `speedFactor = 1f` · `timeAvailable` · `soundPlayedFlag`）
    //   · 两个字面量：`"0x4264910" = "{0:0}:{1:00}"`（`il2cpp_out/stringliteral.json`，= `Activate`/`Update` 的
    //     `String.Format` 格式串）· `DAT_1834b2df4 = 5.0`（剩 5 秒那一档）· `DAT_1834b2f60 = 1.0`
    //     （`TimeSpan.FromSeconds(1.0)`，读数前补的那一下）
    //
    // 🔴 **为什么不是 `MonoBehaviour`**：原版它挂在 GameObject 上、`Update()` 里 `DateTime.Now` 现取（**不可复现**）。
    //    这里做成**普通类 + 注入时钟**：`Tick(DateTime now)` 给自检一个确定的时刻（`Tick()` 那一档才是
    //    `DateTime.Now` = 原版形状）。文本写口也做成委托（原版是 `GetComponent<TextMesh>().text`，
    //    我们这一层没有 `TextMesh`；消费点给 `Label.SetText` 即可）。
    //
    // ⚠️ **一处如实标**：
    //   原版 `timerText`/`AudioSource` 为 null 时是 throw（`FUN_1803f47a0`）；我们出声 + 跳过（全工程口径）。
    //   （2026-10-29 `B94_B6` 已把原来那条「乘式未拿到逐字节判据」**结案**：VA 反汇编坐实
    //    「**先 `cvttss2si` 截断 `speedFactor`、再 `imul` 乘 ticks**」—— 判据全文见 `Scale` 的注释。）
    //   ⚠️ 那条旧记录写的是「`Locale__GetText` 是 `TimeSpan.FromTicks` 的误识别」，**方向对**；
    //      新判据只纠正了**运算次序**（先截断 vs 先相乘），其余一字未变。
    /// <summary>原版 `Countdown`（`MonoBehaviour`：把一个总秒数按 `speedFactor` 倍速倒数，`mm:ss` 写进 `TextMesh`）。
    /// <para>⚠️ **本工程没有消费点**（原版那边也是挂在少数 prefab 上；我们接的倒计时全走 <see cref="TimerDisplay"/>）
    /// ⇒ 这一类是「判据齐、行为齐、无调用方」，由 `Editor/B93S3Assert.cs` 的断言钉住。</para></summary>
    public sealed class Countdown
    {
        /// <summary>格式串（判据：`stringliteral.json` 地址 `0x4264910` = `{0:0}:{1:00}`）。
        /// ⚠️ 是 `{0:0}`（至少 1 位）**不是** `{0:00}` —— 分钟那一格原版就是 1 位起。</summary>
        public const string TextFormat = "{0:0}:{1:00}";
        /// <summary>`Update` 里判「快到了」的那一档（`DAT_1834b2df4` = **5.0**）。</summary>
        public const float LowSeconds = 5f;
        /// <summary>读数前补上的那一段（`DAT_1834b2f60` = **1.0 秒**，`TimeSpan.FromSeconds(1.0)`）。</summary>
        public static readonly System.TimeSpan RoundUp = System.TimeSpan.FromSeconds(1.0);

        System.TimeSpan _timeAvailable;          // 原版 +0x48
        System.TimeSpan _elapsedBeforeStop;      // +0x30
        System.TimeSpan _elapsed;                // +0x38（speedAdjustedTimeElapsed）
        System.DateTime _lastStop;               // +0x28
        bool _running;                           // +0x40
        float _speedFactor = 1f;                 // +0x44
        bool _soundPlayed;                       // +0x50

        /// <summary>文本写口（原版 `TextMesh.text`；⛔ 不静默：为 null 时出声一次）。</summary>
        public System.Action<string> WriteText;
        /// <summary>「剩 &lt; 5 秒」那一下的音效（原版 `GetComponent&lt;AudioSource&gt;().Play()`）。</summary>
        public System.Action PlayAlert;
        /// <summary>时钟（原版 `DateTime.Now`；自检注入固定时刻）。</summary>
        public System.Func<System.DateTime> Clock;

        static bool _said;

        public Countdown(System.Action<string> writeText = null, System.Action playAlert = null,
                         System.Func<System.DateTime> clock = null)
        {
            WriteText = writeText; PlayAlert = playAlert; Clock = clock;
        }

        public bool Running { get { return _running; } }
        public float SpeedFactor { get { return _speedFactor; } }
        /// <summary>总时长（原版 `timeAvailable` `+0x48`）。</summary>
        public System.TimeSpan TimeAvailable { get { return _timeAvailable; } }
        /// <summary>已走掉的「倍速后」时长（原版 `speedAdjustedTimeElapsed` `+0x38`）。</summary>
        public System.TimeSpan Elapsed { get { return _elapsed; } }
        /// <summary>上一次写出去的文本（自检读口）。</summary>
        public string LastText { get; private set; }

        /// <summary>原版 `Countdown.Activate(float speed, int secondsDuration)` —— 逐句：
        /// `timerText = GetComponent&lt;TextMesh&gt;()` ·
        /// `timeAvailable = new TimeSpan(0, 0, secondsDuration)`（**3 实参 ctor = 时/分/秒** ⇒ 真的 `secondsDuration` 秒）·
        /// `text = Format(timeAvailable)` 先写一次 · `speedFactor = speed` · `lastStop = Now` ·
        /// 两格 elapsed 归零 · `running = true` · `soundPlayedFlag = false`。
        /// <para>⚠️ `.c` 那行看起来是 4 实参（`TimeSpan___ctor(this,0,0,param_3,0)`），末位那个 `0` 是
        /// **MethodInfo 指针**不是第 4 个实参 ⇒ 仍是 3 实参 ctor（ILSpy 同结论）。</para></summary>
        public void Activate(float speed, int secondsDuration)
        {
            _timeAvailable = new System.TimeSpan(0, 0, secondsDuration);
            Write(Fmt(_timeAvailable));
            _speedFactor = speed;
            _lastStop = Now();
            _elapsedBeforeStop = System.TimeSpan.Zero;
            _elapsed = System.TimeSpan.Zero;
            _running = true;
            _soundPlayed = false;
        }

        /// <summary>原版 `Countdown.ChangeSpeed(float newSpeed)`：`timeElapsedBeforeStop = speedAdjustedTimeElapsed` ·
        /// `speedFactor = newSpeed` · `lastStop = Now` · `running = true`。</summary>
        public void ChangeSpeed(float newSpeed)
        {
            _elapsedBeforeStop = _elapsed;
            _speedFactor = newSpeed;
            _lastStop = Now();
            _running = true;
        }

        /// <summary>原版 `Countdown.Stop()` = `running = false`。</summary>
        public void Stop() { _running = false; }

        /// <summary>原版 `Countdown.GetSecondsLeft()` = `(timeAvailable − speedAdjustedTimeElapsed).TotalSeconds`
        /// （**不**带 `RoundUp` 那 1 秒）。</summary>
        public double GetSecondsLeft() { return (_timeAvailable - _elapsed).TotalSeconds; }

        /// <summary>原版 `Countdown.Update()`（用 `DateTime.Now`）。</summary>
        public void Tick() { Tick(Now()); }

        /// <summary>原版 `Countdown.Update()` 的可复现版（注入时刻）。逐句：
        /// `if (!running) return;` · `elapsed = (now − lastStop) × speedFactor + elapsedBeforeStop` ·
        /// `remaining = timeAvailable − elapsed + 1s` · `text = Format(remaining)` ·
        /// `if (!soundPlayed && remaining.Seconds &lt; 5) { play; soundPlayed = true; }`。</summary>
        public void Tick(System.DateTime now)
        {
            if (!_running) return;
            _elapsed = Scale(now - _lastStop, _speedFactor) + _elapsedBeforeStop;
            var remaining = _timeAvailable - _elapsed + RoundUp;
            Write(Fmt(remaining));
            if (!_soundPlayed && remaining.Seconds < LowSeconds)
            {
                _soundPlayed = true;
                if (PlayAlert != null) PlayAlert();
            }
        }

        /// <summary>原版那一下「乘倍速」—— **逐字节判据（2026-10-29 `B94_B6` · VA 反汇编）**：
        /// `Countdown$$Update`（VA `0x180623EA0`）里就三条：
        /// `movss xmm6,[rdi+0x44]` → **`cvttss2si rcx,xmm6`**（**先把 `speedFactor` 截断成 long**）→
        /// **`imul rcx,rbx`**（再乘 `(now − lastStop).Ticks`）→ `call 0x180A89E90`；
        /// 那个 callee 全文只有 `mov rax,rcx; ret` = **`TimeSpan.FromTicks(long)`**（= `new TimeSpan(ticks)`）。
        /// ⇒ **先截断、再相乘**（⛔ 不是「乘完再截断」）：`speedFactor = 1.5` ⇒ 按 **1** 算；
        /// `0 &lt; speedFactor &lt; 1`（例 0.5）⇒ `(long)0.5 = 0` ⇒ 计时**冻住**。**原版就是这个行为，⛔ 别"修"**。
        /// ⚠️ `.c` 那行 `Locale__GetText((longlong)fVar1 * lVar5)` 是同一形状（callee 被误识别成
        /// `Locale.GetText`）；ILSpy 把整条乘式丢了、只留 `cvttss2si rcx,xmm6` 的 note ⇒ 本条**只能靠指令流定**。
        /// `imul` 溢出按补码回绕、`cvttss2si` 越界给 `long.MinValue` —— 两处都照原版，**不加** NaN/Inf 闸。</summary>
        static System.TimeSpan Scale(System.TimeSpan ts, float factor)
        {
            return System.TimeSpan.FromTicks(unchecked((long)factor * ts.Ticks));
        }

        /// <summary>原版那两处 `String.Format("{0:0}:{1:00}", ts.Minutes, ts.Seconds)`。
        /// ⚠️ 只印 **Minutes/Seconds 两格** ⇒ 超过 1 小时的部分（Hours）**丢掉**（原版就是这样，⛔ 别"修"）。</summary>
        public static string Fmt(System.TimeSpan ts) { return string.Format(TextFormat, ts.Minutes, ts.Seconds); }

        System.DateTime Now() { return Clock != null ? Clock() : System.DateTime.Now; }

        void Write(string text)
        {
            LastText = text ?? "";
            if (WriteText == null)
            {
                if (!_said)
                {
                    _said = true;
                    Debug.LogWarning("[Countdown] 文本写口没给（原版 = `GetComponent<TextMesh>().text`，取不到时原版会 throw）"
                                   + " ⇒ 只记账、不写屏。");
                }
                return;
            }
            WriteText(LastText);
        }
    }
}
