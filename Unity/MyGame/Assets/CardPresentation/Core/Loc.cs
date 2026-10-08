// Loc.cs — 语言表 + `T(key)` + 当前语言（**全工程唯一一份**）
//
// ============================ 为什么要有它 ============================
// 用户 2026-09-28 立项：「**卡组编辑按钮文案中文化。游戏的各个地方都做中文和英文两个语言。**」
// ⇒ 判据全文 = `资料/待办判据_卡面卡池与双语.md` §23；本文件是那一步「**1. 基础设施**」。
//
// ============================ 照原版的部分（逐条有出处） ============================
//  · **语言 12 种、枚举名与取值** = 原版 `AvailableLanguages`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/
//    AvailableLanguages.cs` **逐条照抄**：`English = 0, Spanish = 10, …, Chinese = 110` —— 取值是**十进制步进 10**，
//    ⛔ 别改成 0..11：那会与我们写进 `PlayerPrefs` 的存档值不兼容）。
//  · **控件** = 原版 `LanguageSelector : MonoBehaviour`，**唯一字段 `private TMP_Dropdown languagesDropdown`**
//    （`Assembly-CSharp/LanguageSelector.cs`）⇒ 语言选择是**下拉框**，不是左右箭头、不是一排按钮。
//  · **选项文本的键** = 原版 `LanguageSelector.ResetLanguagesDropdown`（`d:/2/tools/decomp_full/`，逐句实读）：
//    遍历 `GameStaticData` 那张语言名表 → `String.Concat("<前缀>", 名字)` → `LocalizationManager.GetTermTranslation(...)`
//    ⇒ 键 = **`MainMenu/Settings/LanguageName/<枚举名>`**。
//    🔴 前缀那个字面量**本地可核**：`global-metadata.dat` 里 `MainMenu/Settings/LanguageName/` 与
//    `MainMenu/Settings/ButtonLabel/Exit_Game` **相邻**（偏移 561102 / 561120，两条都在）。
//    ⇒ 12 个键写全在下面 `Names` 那张表里（**这是 12 项下拉的文档来源**）。
//  · **词条键（`mTerm`）** = 每一条都**照原版 prefab 里那顆 `Localize.mTerm` 原文抄**
//    （`bundle_menus_assets_all/MonoBehaviour/*.json` 实读；逐条列在下面 `Table` 的注释里）。
//    ⛔ 不许自拟键名（除非原版真没有 —— 本文件只有 `Settings/Online/Title` 一条是自拟，见它那行注释）。
//    🆕 2026-10-17（D5）加的那一族 `Card_Race/<race>` **也不是自拟**，出处是**原版代码里的字面量**
//    （`GameStaticData.CardRaceToString` / `MinionRaceToString` 两个方法体 + `.rdata` 里读出来的
//     `"Card_Race/"` / `"Card_Race/Warlord"`）—— 逐条判据写在那一节的注释里。
//  · **英文那一列** = 原版 prefab 里 TMP 的 `m_text` **原文逐字符照抄**（含大小写：`Touch input` 那个小写 i、
//    `Auto zoom` 那个小写 z）—— 这是本地能拿到的**最硬的一份英文**。
//  · 🔴 **中文那一列是我们自己译的**（**不是原版文案**）：原版的中文在**远端 I2 语言表**里
//    （84 个本地 bundle 里没有 `localization_assets_all.bundle`、246,807 个文件扫中文串零命中
//    —— 见 §23 与 `资料/待办判据_卡面卡池与双语.md`）。这一列照项目口径**如实标注为「我们译的」**，
//    与 `数据/卡牌翻译/zh_cards.json`（卡名/卡面那批）是同一种东西。
//
// ============================ 🔴 只有中文/英文两套文案 ============================
// 用户 2026-09-28 拍板：**下拉照原版列 12 项**，但**本地只有中文（我们译的）与英文（原版 TMP 原文）两套**。
// ⇒ **选到没有文案的语言（西/法/德/葡/俄/意/韩/日/捷/波）一律【回退英文】并 `Debug.Log` 出声**
//    （⛔ **不许在 UI 上加原版没有的提示行** —— 出声只走日志）。见 `T()` 与 `Effective`。
//
// ============================ 🔴 落盘 ============================
//  · 键名 = **`"Language"`**（用户口径）；风格照 `Core/WarpforgeAudio.cs` 的 `MusicPrefKey`（工程里唯一的原版键先例）。
//  · 读/写都 `PlayerPrefs.Save()`（同 `WarpforgeAudio`）。
//  · **默认 = 中文**（用户 2026-09-28 口径：原版实拍是中文客户端、用户是中文用户）。
//  · ⚠️ 原版**不存 `PlayerPrefs`** —— 它存玩家存档（服务器那一侧），我们没有存档系统 ⇒
//    这是**我们挑的**（同 `AutoZoom` / `SuperSampling` 那两条如实标注）。
//
// ============================ 忘了接的地方不会静默 ============================
//  · `T()` 拿到**表里没有**的键 ⇒ 返回**键名本身** + `Debug.LogWarning`（键名会直接显示在界面上 = 看得见），
//    并把 `MissingCount` / `LastMissingKey` 记下来（自检有一条钉它）。
//  · 对话语言回退 ⇒ 计数记在 `FallbackCount`（自检的第 ④ 条就断这个 + 回退后的文本）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `AvailableLanguages`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/AvailableLanguages.cs`
    /// **逐条照抄，含取值**）。⚠️ 取值是 **0/10/20/…/110**，不是 0..11。</summary>
    public enum AvailableLanguages
    {
        English = 0,
        Spanish = 10,
        French = 20,
        German = 30,
        Portuguese = 40,
        Russian = 50,
        Italian = 60,
        Korean = 70,
        Japanese = 80,
        Czech = 90,
        Polish = 100,
        Chinese = 110,
    }

    /// <summary>
    /// 语言表 + `T(key)` + 当前语言。
    /// <para>用法：界面文案写 `Loc.T("Settings/General/DisableBots")`；换语言 = `Loc.SetLanguage(语言)`，
    /// 调用方**自己**把已经画出来的字重设一遍（本类**不发事件** —— 见 `SetLanguage` 的注释）。</para>
    /// </summary>
    public static class Loc
    {
        // ============================================================ 常量 / 状态

        /// <summary>落盘键。🔴 用户 2026-09-28 口径就是这五个字符（⛔ 别改成 `LanguagePref` 之类）。</summary>
        public const string PrefKey = "Language";

        /// <summary>出厂语言 = **中文**（用户口径：原版实拍是中文客户端）。</summary>
        public const AvailableLanguages Default = AvailableLanguages.Chinese;

        /// <summary>没有文案时回退到的那一种（= 本地唯一那份**原版**文案）。</summary>
        public const AvailableLanguages Fallback = AvailableLanguages.English;

        /// <summary>**本地真的有两套文案**的语言（= 下拉 12 项里只有这两个能选到「不是英文」的文本）。</summary>
        public static bool HasOwnText(AvailableLanguages l)
        {
            return l == AvailableLanguages.Chinese || l == AvailableLanguages.English;
        }

        /// <summary>下拉里的 12 项 —— **原版 `AvailableLanguages` 的声明序**（= 原版下拉的选项序）。</summary>
        public static readonly AvailableLanguages[] Languages =
        {
            AvailableLanguages.English, AvailableLanguages.Spanish, AvailableLanguages.French,
            AvailableLanguages.German, AvailableLanguages.Portuguese, AvailableLanguages.Russian,
            AvailableLanguages.Italian, AvailableLanguages.Korean, AvailableLanguages.Japanese,
            AvailableLanguages.Czech, AvailableLanguages.Polish, AvailableLanguages.Chinese,
        };

        /// <summary>🔴 **自检注入点**：true ⇒ `SetLanguage` 只改内存、**不写 `PlayerPrefs`**
        /// （本工程规矩：自检不许动玩家的真设置 —— 同 `SmallScreenUI.PersistOverride` /
        /// `AutoZoom.PersistOverride` / `SuperSampling.PersistOverride`）。</summary>
        public static bool PersistOverride;

        static bool _loaded;
        static AvailableLanguages _current = Default;

        /// <summary>当前语言（第一次读时从 `PlayerPrefs` 载入）。</summary>
        public static AvailableLanguages Current { get { Load(); return _current; } }

        /// <summary>**真正生效**的那一种 —— 没有文案的语言在这里被折成 <see cref="Fallback"/>。
        /// 界面按它决定「拉丁大写高还是汉字高」这类与文本语种有关的尺寸（见 <see cref="HasCjk"/>）。</summary>
        public static AvailableLanguages Effective
        {
            get { var c = Current; return HasOwnText(c) ? c : Fallback; }
        }

        // ---- 自检/诊断口（⛔ 别为了好看藏起来：自检拿不到就只能瞎猜）----

        /// <summary>表里没有的键**被问到**的次数（`T()` 计数）。</summary>
        public static int MissingCount { get; private set; }
        /// <summary>最后一个没查到的键（自检/日志要能说出是哪一个）。</summary>
        public static string LastMissingKey { get; private set; }
        /// <summary>「当前语言没有文案 ⇒ 回退英文」发生的次数（自检第 ④ 条的判据之一）。</summary>
        public static int FallbackCount { get; private set; }
        /// <summary>最后一个因回退而被查的键。</summary>
        public static string LastFallbackKey { get; private set; }
        /// <summary>表里一共几条词条（自检那条「表非空」用）。</summary>
        public static int EntryCount { get { return Table.Count; } }

        // ---- 🆕 2026-10-18（第四轮 · 审查 S2）：**缺键出声**的**去重**表 ----

        /// <summary>已经为哪些**表里没有的键**出过声（`T()` 的兜底那一支）。
        /// 🔴 **必须按键去重**：这些键多半是被**每帧重画**的调用点问到的（`RefreshXxx` / `Build` 那条链），
        /// 不去重就会**刷屏**，把「抓日志当判据」型自检（本工程已有先例）淹掉。
        /// ⚠️ 只影响**日志**：`MissingCount` / `LastMissingKey` 记账照旧**每次 +1**（别把两件事混了）。</summary>
        static readonly HashSet<string> _warnedMissing = new HashSet<string>();
        /// <summary>出过声的**不同键**的个数（自检读它；去重那条断言的主判据）。</summary>
        public static int MissingWarnedCount { get { return _warnedMissing.Count; } }
        /// <summary>自检用：这个键**出过声没有**。</summary>
        public static bool HasWarnedMissing(string key)
        { return !string.IsNullOrEmpty(key) && _warnedMissing.Contains(key); }
        /// <summary>自检用：清空去重表（⛔ 不动 `PlayerPrefs`、不动任何别的状态）——
        /// 让「同一个键**第一次**问 ⇒ 必出声」这类断言**不依赖跑了几遍**。</summary>
        public static void ResetMissingWarnedForTest() { _warnedMissing.Clear(); }

        // ============================================================ 语言表
        //
        // 键 = 原版 `Localize.mTerm` **原文**（逐条出处见每条自己的注释）；值 = (中文, 英文)。
        // 🔴 英文那一列逐字符照抄原版 TMP 的 `m_text`；中文那一列**是我们译的**（见文件头）。

        struct Entry
        {
            public string Zh, En;
            public Entry(string zh, string en) { Zh = zh; En = en; }
        }

        static readonly Dictionary<string, Entry> Table = new Dictionary<string, Entry>
        {
            // ---- 设置窗 · General 页（`bundle_menus_assets_all` 的 `General Tab` 子树逐节点实读）----
            // 页标题：`General Tab > Tab Title`，TMP `m_text = "General"` · mTerm `Settings/General/Title`
            { "Settings/General/Title",               new Entry("通用",       "General") },
            // `General Tab > Checkboxes > Disable Bots`（TMP `m_text = "Disable Bots"`，fs42）
            { "Settings/General/DisableBots",         new Entry("禁用机器人", "Disable Bots") },
            // 同上 · `Disable Notifications`
            { "Settings/General/DisableNotifications",new Entry("禁用通知",   "Disable Notifications") },
            // 同上 · `Touch Input`（⚠️ 原版 TMP 印的是**小写 i** 的 `Touch input`，照抄）
            { "Settings/General/TouchInput",          new Entry("触摸输入",   "Touch input") },
            // `General Tab > Bottom Buttons > Redeem Code`（TMP `m_text = "Redeem Code"`，fs40）
            { "Settings/General/RedeemCode",          new Entry("兑换码",     "Redeem Code") },
            // `General Tab > Bottom Buttons > Close Game Button`（TMP `m_text = "Exit Game"`，fs38）
            // 🔴 键名里的下划线是**原版的**（`Exit_Game`），⛔ 别改成 `ExitGame`
            { "MainMenu/Settings/ButtonLabel/Exit_Game", new Entry("退出游戏", "Exit Game") },
            // 语言那一行的标签。**两个设置窗共用这一个键**（主菜单 General 页的 `SelectLanguageText`
            // 与 13 个战场里 `BattleSettingsPanel/Language Selector/SelectLanguageText` 挂的是同一条词条
            // —— bundle 里这条 mTerm 出现 43 次，逐条都指向这两族）。TMP `m_text = "Select Language"`，fs42
            { "MainMenu/Settings/ButtonLabel/SelectLanguage", new Entry("选择语言", "Select Language") },

            // ---- ⚠️ 自拟键（原版没有这一页）----
            // 「联机」那一页**原版没有**（`Shell/SettingsWindow.cs` 文件头 ①：搜过
            // Online/Network/Server/Connect/Region/Ping/Multiplayer/Matchmak，设置窗里一个都没有）
            // ⇒ 这一条的键名与两列文案**都是我们起的**（照原版那一族的命名形状写）。
            { "Settings/Online/Title",                new Entry("联机",       "Online") },

            // ---- 语言名（下拉的 12 项）----
            // 🔴 键 = `MainMenu/Settings/LanguageName/<枚举名>`（原版 `ResetLanguagesDropdown` 的拼法，
            //    前缀字面量已在 `global-metadata.dat` 核到）。英文那一列 = **枚举名本身**
            //    （原版本地 TMP 拿不到正式选项名 —— 远端 I2 表）；中文那一列是我们译的。
            { "MainMenu/Settings/LanguageName/English",    new Entry("英语",     "English") },
            { "MainMenu/Settings/LanguageName/Spanish",    new Entry("西班牙语", "Spanish") },
            { "MainMenu/Settings/LanguageName/French",     new Entry("法语",     "French") },
            { "MainMenu/Settings/LanguageName/German",     new Entry("德语",     "German") },
            { "MainMenu/Settings/LanguageName/Portuguese", new Entry("葡萄牙语", "Portuguese") },
            { "MainMenu/Settings/LanguageName/Russian",    new Entry("俄语",     "Russian") },
            { "MainMenu/Settings/LanguageName/Italian",    new Entry("意大利语", "Italian") },
            { "MainMenu/Settings/LanguageName/Korean",     new Entry("韩语",     "Korean") },
            { "MainMenu/Settings/LanguageName/Japanese",   new Entry("日语",     "Japanese") },
            { "MainMenu/Settings/LanguageName/Czech",      new Entry("捷克语",   "Czech") },
            { "MainMenu/Settings/LanguageName/Polish",     new Entry("波兰语",   "Polish") },
            { "MainMenu/Settings/LanguageName/Chinese",    new Entry("中文",     "Chinese") },

            // ============================================================ 卡组编辑窗（原版 `Deck Editing Menu`）
            //
            // 键 = **原版 prefab 上那颗 `Localize` 的 `mTerm` 原文**（逐条实读；节点出处写在每行上）。
            // 英文那一列 = **那颗 TMP 的 `m_text` 原文逐字符照抄**（组内人肉核过：`Filters` / `Cards` /
            //   `Deck info` / `Cosmetics` / `Done` —— 与本工程原来写死的那五个英文**逐字相同**，
            //   所以「我们对过的英文」与「原版 prefab」在这五条上是同一串）。
            // 🔴 中文那一列 = **原版实拍**（**不是我们译的** —— 本表其余部分才是，见文件头）：
            //   用户 2026-10-17 的中文客户端截图 `资料/原版参照图/用户实拍_1017/卡组编辑界面参考.png`
            //   上逐字读到的那几个词（过滤器 / 张牌 / 卡组信息 / 美容品 / 完成），已逐字复核过。
            //
            // ⚠️ **键名分属两个前缀、那是原版如此**：`张牌` 与 `美容品` 两颗页签用的是
            //   `MenuShop/ShopItemType/*`（商城那族的「物品种类」词条，原版这两处**共用同一条**），
            //   而 `卡组信息` 与 `完成` 在 `MenuDeck/*` 下。
            //   ⛔ 别为了「看着整齐」把键名统一前缀 —— 改了 = 查不到 = 界面上印键名。
            // 消费侧 = `Deck/DeckRuntime.cs` 的 `BuildHeader`（过滤器钮）· `BuildSidebar`（三颗页签）·
            //   `BuildFooter`（`完成`）；断言 → `Editor/DeckScene.cs` 的 G1 节 ⑭。
            // 实读办法（本工程自己的尺子）：`工具/menu_rect.Bundle` 遍历 prefab 树 →
            //   `bundle_menus_assets_all/MonoBehaviour/` 里那颗 `Localize` 的 `mTerm`。
            // ------------- D2 顶栏过滤器钮（`…/Header/Filters/Label`）-------------
            { "MenuDeck/Filters/Filters",                     new Entry("过滤器",   "Filters") },
            // ------------- D3 左栏三颗页签（`…/Sidebar/Window Options/Buttons/{Cards,Info,Cosmetics}/Label/Text`）-------------
            { "MenuShop/ShopItemType/Cards",                  new Entry("张牌",     "Cards") },
            { "MenuDeck/HUD/DeckDescription/DeckInfo",        new Entry("卡组信息", "Deck info") },
            { "MenuShop/ShopItemType/Cosmetics",              new Entry("美容品",   "Cosmetics") },
            // ------------- D4 左栏底部钮（`…/Sidebar/Footer/Done/Button Text`，那颗上面**两颗** `Localize`，
            //   本条取**第一颗** `MenuDeck/MenuButtons/Done`；第二颗 `MenuLogin/Login/DoneButton` 是登录页共用的）-------------
            { "MenuDeck/MenuButtons/Done",                    new Entry("完成",     "Done") },

            // ------------- 🆕 2026-10-17（F2 · 补 D6 漏填的词条）：选卡组弹窗的**标题** -------------
            // 节点 = `Deck Selection Popup with Tabs/Generic Window Red Background Big/Deck Display/Header/
            //   Instructions 2`。键名 = 那颗 `Localize` 的 `mTerm` 原文（**2026-10-17 F2 实读**：
            //   `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_4152179270747796863.json` 的
            //   `mTerm = "MenuDeck/Tip/SelectDeckAgainst"`；它的 `m_GameObject.m_PathID = -3286650894701775489`
            //   ⇒ 按组件反查 = `bundle_menus_assets_all/GameObject/Instructions 2.json` 的第 4 个组件）。
            // 🔴 **为什么必须补**：`Loc.T` 对**没有的键**是「返回键名本身 + 出声」（见下面 `T()` 的 doc）
            //   ⇒ 两语档下那扇窗的标题**在界面上真的印着 `MenuDeck/Tip/SelectDeckAgainst`**
            //   （`collection.log` 里那行 `[Loc] 语言表里**没有**这个词条：…` 出现多次）。
            // 中文那一列 = **原版实拍**（用户 2026-10-17 的中文客户端截图
            //   `资料/原版参照图/用户实拍_1017/更换卡组的参考.png` 右上角那四个字，D6 那轮已逐字核过）。
            // ⚠️ **英文那一列【不是】原版 prefab 的原文 —— 是【我们自己】的那一句**（如实记）：
            //   本键那颗 TMP（`MonoBehaviour_925819347406796159.json`）`m_text = ''`（**F2 实读**，
            //   与 `Shell/DeckSelectionPopup.cs` 那句「取不到文案」一致）⇒ 本地**没有**这条键的英文原文
            //   （原版那套在远端 I2 表里，84 个本地 bundle 里没有 `localization_assets_all.bundle`）。
            //   `Select deck` 的**唯一来源** = 同窗**兄弟**节点 `Instructions`（键 `MenuDeck/Tip/SelectDeck`）
            //   那颗 TMP 的 `m_text = 'Select deck'`（同一份 `menu_dump` 实读）—— 那是**旁证、不是原文**
            //   ⇒ ⛔ 别写成「照抄原版」。将来若拿到 I2 表，**先改这一列**。
            // 消费侧 = `Shell/DeckSelectionPopup.cs` 的 `TitleTerm`（`:170`）+ 建那一格时取词（`:381`）；
            //   断言 → `Editor/CollectionScene.cs` 的 D6 那一节（两语档各断一个**字面量**）。
            // 🧨 改坏法：把这条删掉 ⇒ 界面上印键名、那条断言立刻红（`Loc.T` 返回键名）。
            { "MenuDeck/Tip/SelectDeckAgainst",               new Entry("选择卡组", "Select deck") },

            // ------------- 🆕 2026-10-17（B11 · A884）：导入窗（`Import Deck Popup`）的**占位符** -------------
            // 节点 = `Import Deck Popup/Window/Input Field/Text Area/Placeholder` —— 键名 = 那颗 `Localize`
            //   的 `mTerm` 原文（`bundle_menus_assets_all/MonoBehaviour_6085227748672536722.json` 实读；
            //   🔴 全库**只此一颗**用这个键：`grep -rl` 扫 24.7 万文件命中 1）。
            // 英文那一列 = **那颗 TMP 的 `m_text` 原文逐字符照抄**（含末尾那三个点）。
            // 🔴 中文那一列 = **不是实拍**（原版中文在远端 I2 表里；本地 84 个 bundle + 24.7 万文件扫中文串零命中
            //   —— 见文件头）⇒ 取**我们自己那份译表** `数据/本地化/i18n/zh_CN.csv:102`
            //   （该行 = `Enter text...,~,输入文字...`）⇒ ⚠️ **是我们译的**，与 `Card_Race/*` 那一族同一口径。
            // 消费侧 = `Shell/ImportDeckPopup.cs` 的 `PlaceholderTerm` + `RefreshInputText`；
            //   断言 → `Editor/ShellScene.cs` 的 B11/A884 那一节（两语档各断一个**字面量**）。
            // ⚠️ 兄弟节点 `Main Search message` 挂的是**另一条**词条 `MenuDeck/Share/PasteDeck`
            //   （`MonoBehaviour_8528767437303251090.json`）—— **那条见下面它自己的条目**；
            //   同窗那颗 `Error msg` **一颗 `Localize` 都没有**（原版本来就没有词条 ⇒ **永远不接**，
            //   属于「判据是空的」那一档，⛔ 别替它编一条）。
            //   🔴 **2026-10-17（A891）三处消费点全接上了**：
            //   · `PasteDeck` / `EnterText` 在 **`Deck/DeckRuntime.cs`** 的导入窗（`imp_title` + `imp_input` 的
            //     **建 / 刷新两个入口**）；
            //   · `PasteDeck` / `EnterText` / `Confirm` 在 **`Shell/ImportDeckPopup.cs`** 的导入窗
            //     （`TitleTerm` / `PlaceholderTerm` / `ConfirmTerm`，各自那颗常量上写着判据）。
            //   ⚠️ 唯一**仍然没接**的是那颗 `Error msg`：原版**根本没给它词条**（引擎按根脚本的三个 term 字段
            //     写进去的）⇒ 我们那行错误走自己的文案（`CollectionData.ImportDeck`），**这不缺东西、别去接**。
            { "MenuDeck/HUD/EnterText",                       new Entry("输入文字...", "Enter text...") },

            // ------------- 🆕 **2026-10-17（A891）：导入窗的【标题】词条** -------------
            // 节点 = `Import Deck Popup/Window/**Main Search message**`（rect `610,280 700×60` ——
            //   与 `DeckRuntime.BuildImportPopup` 里 `imp_title` 那行实参逐位对上 ⇒ 同一颗）。
            // 键名 = 那颗 `Localize.mTerm` 的**原文** `MenuDeck/Share/PasteDeck`
            //   （`d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/
            //     MonoBehaviour_8528767437303251090.json` 实读 `mTerm`；同文件里它的兄弟 `Error msg` 没有 `Localize`）。
            // 英文那一列 = 同 prefab 里那颗 TMP 的 `m_text` **原文**（`menu_dump` 实读：`Main Search message 'Paste your deck'`）。
            // 🔴 中文那一列 = **不是实拍**（同 `EnterText` 那条：原版中文在远端 I2 表）⇒ 取我们自己的译表
            //   `数据/本地化/i18n/zh_CN.csv:143`（该行 = `Paste your deck,~,粘贴你的卡组`）。
            // 消费侧 = `Deck/DeckRuntime.cs` 的 `imp_title`（那行注释里写着判据）。
            { "MenuDeck/Share/PasteDeck",                     new Entry("粘贴你的卡组", "Paste your deck") },

            // ------------- 🆕 **2026-10-17（A891）：导入窗那个 `Confirm` 钮的词条** -------------
            // 节点 = `Import Deck Popup/Window/Buttons/Generic UI Button/**Button Text**`（同一份 prefab）。
            // 键名 = 那颗 `Localize.mTerm` 的**原文** `MainMenu/General/Confirm`
            //   （⚠️ **不是 `MenuDeck/` 族** —— 原版自己复用了主菜单那条通用按钮词条；另一处记着它的 =
            //    `Shell/ReferralPopupWindow.cs` 的 `TxtBtn` 注释）。
            // 英文那一列 = 同节点那颗 TMP 的 `m_text` **原文**（`menu_dump … "Import Deck Popup" --depth 8` 实读 `Button Text 'Confirm' fs45`）。
            // 🔴 中文那一列 = **不是实拍**（同这一族其它条）⇒ 取我们自己的译表 `数据/本地化/i18n/zh_CN.csv:83`
            //   （该行 = `Confirm,~,确认`）。
            // 消费侧 = `Shell/ImportDeckPopup.cs` 的 `ConfirmTerm`（那一行注释里写着判据）。
            { "MainMenu/General/Confirm",                     new Entry("确认", "Confirm") },

            // ============================================================ 🆕 **2026-10-18（A891 的续）：卡组线那一批 `MenuDeck/` 词条**
            //
            // 键名 = **原版 prefab 上那颗 `Localize` 的 `mTerm` 原文**（本批**按 pid 亲读**：
            //   `bundle_menus_assets_all/MonoBehaviour/*.json` 的 `mTerm` ＋ **同一 GameObject** 上
            //   TMP 的 `m_text`；父链逐级走 `RectTransform.m_Father` 复核过，26 颗的落点见 W1 的交件报告）。
            // 英文那一列 = **那颗 TMP 的 `m_text` 原文逐字符照抄**（例外只有 `Random` —— 见它那一条）。
            // 中文那一列 = **我们自己那份译表** `数据/本地化/i18n/zh_CN.csv`（⚠️ 那份表按**英文源串**索引，
            //   **不是**按 `mTerm`）—— 逐行把 `csv:<行>` 写在右边；CSV 里没有的那两条**是我们自拟的**，
            //   如实标注（同本文件 `Settings/Online/Title` / `MenuDeck/Tip/SelectDeckAgainst` 的先例）。
            // 消费侧 = `Deck/DeckRuntime.cs`（`hdr_clear_t` / `cosmoflt_title`）·
            //   `Shell/CollectionWindow.cs`（`Clear filters Text` / `Create Text` / `Import Text` / Deck 页 `Title`）·
            //   `Core/FilterPanelModel.cs`（占位符）· `Shell/DeckSelectionPopup.cs`（`Random Text`）·
            //   `Shell/LiveOpsEventWindow.cs`（`Button Text`）；断言 → `Editor/{DeckScene,CollectionScene}.cs`
            //   （本批已同批改成**随语档**）。🧨 改坏法：把某条删掉 ⇒ 界面上印键名本身 + 那条断言立刻红。
            // ------------- `Clear filters`（5 颗，节点名一律 `Button Text`；卡组编辑窗 1 + 收藏窗 4）-------------
            { "MenuDeck/Filters/ClearFilters",                new Entry("清除筛选", "Clear filters") },  // TMP 原文 `Clear filters`；zh_CN.csv:9
            // ------------- `Army`（6 颗，节点名一律 `Title`；两扇窗的卡牌栏 + 卡背抽屉 + 收藏窗卡组页）-------------
            { "MenuDeck/Filters/Army",                        new Entry("军队",     "Army") },          // TMP 原文 `Army`；zh_CN.csv:11
            // ------------- `Search`（11 颗 = `Placeholder`×6 + `Button Text`×5；⚠️ 其中 **6 颗**属 `SocialMenu/` / `Draft Mode` 族
            //   = 1 颗 `Alliances Tab > … > Search Field/Text Area/Placeholder` + **5 颗** `Join Alliances Button/Button Text`）-------------
            { "MenuDeck/HUD/SearchFilter",                    new Entry("搜索",     "Search") },        // TMP 原文 `Search`；zh_CN.csv:10
            // ------------- `Create Deck`（1 颗 = `…/Control Buttons/Create/Button Text`）-------------
            //   ⚠️ TMP 原文是 **`Create Deck`（大写 D）**，与下面 `GoToCreateDeck` 的 `Create deck` **不同串**。
            { "MenuDeck/MenuButtons/CreateDeck",              new Entry("创建卡组", "Create Deck") },   // TMP 原文 `Create Deck`；zh_CN.csv:7
            // ------------- `Import Deck`（1 颗 = 上面那颗的兄弟 `…/Control Buttons/Import/Button Text`）-------------
            //   🔴 中文列**是我们自拟的**：`zh_CN.csv` 里**没有 `Import Deck` 这个键**
            //      （本批按英文源串精确查过，0 命中）⇒ 照原版 TMP 的英文取了「导入卡组」这四个字。
            { "MenuDeck/MenuButtons/ImportDeck",              new Entry("导入卡组", "Import Deck") },   // TMP 原文 `Import Deck`；中文自拟
            // ------------- `Random`（1 颗 = `Deck Selection Popup with Tabs > … > Practice buttons > …`）-------------
            //   🔴 **英文原文取不到** —— 那颗 TMP（`MonoBehaviour_-725143033836620417.json` 所指的
            //      GameObject `Button Text_7450249239293310335`）的 `m_text = ''`（**本批实读**）
            //      ⇒ 原版那套在远端 I2 表里 ⇒ **中英两列都是我们拟的**
            //      （形状照 `MenuDeck/Tip/SelectDeckAgainst` 那条的先例：⛔ 别写成「照抄原版」；
            //        `zh_CN.csv` 里也没有 `Random`）。将来拿到 I2 表 ⇒ **先改这一条**。
            { "MenuDeck/Button/Random",                       new Entry("随机",     "Random") },
            // ------------- 🆕 **活动窗那颗「还没有卡组」时的钮**（3 颗 = 三扇活动窗各一）-------------
            //   🔴 键名**不是**上面那条 `MenuDeck/MenuButtons/CreateDeck`（那条的父链是
            //      `Collection Menu Variant > … > Control Buttons/Create`，**另一棵树**、也是另一串英文）。
            //      本键的父链（本批按 pid 亲读）= `{RankedEventWindowV2 | SkirmishModeEventWindow |
            //      Ranked Deck Selection} > Ranked Deck Selection > No Deck Text > Generic Simplified
            //      UI Button > Button Text`；`Localize.mTerm` = `MainMenu/Ranked/GoToCreateDeck`。
            //   TMP 原文 = **`Create deck`（小写 d）** —— 与 `Create Deck` 是两个串，⛔ 别合并成一条。
            //   它的父节点 `No Deck Text` 才是那句**西语占位串**（`Tienes …{0} {1} Comandante(s)…`，
            //   见 `Shell/LiveOpsEventWindow.cs` 那段注释）—— 两件事别混。
            //   消费侧 = `Shell/LiveOpsEventWindow.cs:507`；中文列 = `zh_CN.csv:230`（`Create deck,~,创建卡组`）。
            { "MainMenu/Ranked/GoToCreateDeck",               new Entry("创建卡组", "Create deck") },

            // ============================================================ 🆕 **2026-10-18（A891 的续 · 第三轮整改）：审查 P1 + P4 那 9 条**
            //
            // 🔴 **P1 的教训（写在这里给下一个会话看）**：本文件原来那条「`Energy Cost` 原版没有 `Localize`」
            //   是**错的** —— 当时「全库」**只扫了 `bundle_menus_assets_all` 一个包**。
            //   审查复跑（`d:/2/新解包资源/assets_full` **全部 80 个 bundle**）：`m_text == "Energy Cost"`
            //   的 TMP 共 **17 颗 / 15 个 bundle**，**每一颗所在 GO 都挂着 `Localize.mTerm = "Battle/Tips/EnergyCost"`**；
            //   其中 3 颗就是卡牌页/卡组编辑窗/异画页那三行 `Cost Filter/Title`。
            //   ⇒ 正本口径：**报「原版没有」之前必须打出「搜过哪几个包」**（`资料/已知的坑.md`）。
            // ------------- 四行小标题里的另两行（`Army` 在上面的那一族块里）-------------
            { "MenuDeck/HUD/Rarity",                          new Entry("稀有度",   "Rarity") },    // TMP 原文 `Rarity`（3 颗 · `… > Rarity FIlter/Title`）；zh_CN.csv:150
            { "MenuDeck/Filters/Type",                        new Entry("类型",     "Type") },      // TMP 原文 `Type`（3 颗 · `… > Type Filter/Title`）；zh_CN.csv:182
            // ------------- `Energy Cost`（P1：原说「没有」，实为 17 颗都有）-------------
            //   ⚠️ **中文列的不确定性（如实记）**：值取自我们那份译表 `数据/本地化/i18n/zh_CN.csv:98`
            //   （`Energy Cost,~,能量费用`），但**那份 CSV 是按英文源串索引的** —— 它到底对应
            //   `Battle/Tips/EnergyCost` 还是别的同字面串，**没查清**（要拿到远端 I2 表才能钉死）。
            { "Battle/Tips/EnergyCost",                       new Entry("能量费用", "Energy Cost") }, // 17 颗 · 含 3 颗 `Cost Filter/Title`；zh_CN.csv:98（英文串索引，见上）
            // ------------- 空态那三行（`MenuCollection/*`，四个页各一份 `Empty Collection Warning`）-------------
            { "MenuCollection/NoCardsFound",                  new Entry("没有符合当前筛选的卡牌", "There are no cards in your collection for the selected filters") },   // TMP 原文逐字符；zh_CN.csv:177
            //   🔴 中文列**是我们自拟的**：`zh_CN.csv` 里**没有 `There are no cardbacks…` 这个键**（精确查 0 命中）
            { "MenuCollection/NoCardbackFound",               new Entry("没有符合当前筛选的卡背", "There are no cardbacks in your collection for the selected filters") }, // TMP 原文逐字符；中文自拟
            //   🔴 **英文列是原版 TMP 的原文，含原版自己的语病**（`There are no **deck** …` 单数）——
            //   照抄（本表英文列的口径 = 那颗 TMP 的 `m_text` 原文逐字符；⛔ 别「顺手修正」成 `decks`，
            //   那会与 `Shell/ShopWindow.cs:287` 的 `TxtEmpty`（同一句原文）分叉）。
            //   ⚠️ 中文取自 `zh_CN.csv:178`，但那条的英文串是 `…for the selected **filter**`（单数 filter），
            //   与我们这一串（复数 filters）**差一个字母** ⇒ 属「最近邻」，**不是精确命中**（如实记）。
            { "MenuCollection/NoDecksFound",                  new Entry("没有符合当前筛选的卡组", "There are no deck in your collection for the selected filters") }, // TMP 原文（含原版语病）；zh_CN.csv:178 近邻
            // ------------- 收藏窗那两处页头 / 通用钮 --------------
            { "MenuCollection/Label/Cosmetics",               new Entry("你的装饰收藏", "Your cosmetics collection") }, // TMP 原文；`label < Header < Cardback Tab`；zh_CN.csv:25
            //   `Back`：同键 8 颗，收藏窗那颗 = `Button Text < Close Button < Shared < Tabs < Content Area < Collection Menu Variant`
            { "MainMenu/MainButtons/ButtonLabel/Back",        new Entry("返回",     "Back") },        // TMP 原文 `Back`；zh_CN.csv:5
            // ------------- `Deck Name` 那颗输入框的占位符 --------------
            { "MenuDeck/HUD/EditDeckName",                    new Entry("点击编辑卡组名", "Tap to edit deck name") }, // TMP 原文；`Placeholder < Text Area < Deck Name < …>`；zh_CN.csv:172

            // ============================================================ 🆕 **2026-10-18（第四轮）：商店「刷新于」那一颗**
            // 键 = 原版那颗 TMP 的 `Localize.mTerm` 原文 **`MenuShop/RefreshCounter`**（4 颗同键，全部在
            // `bundle_menus_assets_all`；本批按 pid 亲读）。
            // 节点 = `{Card Shop Tab | Daily Shop Tab | Item Shop Tab}/daily shop header/TimeCounter/RefreshText`。
            // ⚠️ **两颗 TMP 的 `m_text` 不一样**（原版自己的开发痕迹，照抄英文那一颗）：
            //   · `Card Shop Tab` → **`Refreshes in:`**（英文，本条取它）；
            //   · `Daily Shop Tab` → `Atualiza em:`（**葡语占位串** —— 同 `Shell/ShopData.cs:104` 那条注释说的）。
            //   ⇒ 原版两页**共用这一条 mTerm**（运行期走 I2 表），所以我们两页也用同一条键（形状照原版）。
            // 中文列 = 「刷新于：」(`zh_CN.csv:303`，按英文源串精确命中)。
            // 消费侧 = `Shell/ShopWindow.cs` 的 `BuildTimeCounter`（原来读 `ShopData.RefreshText` 那个写死的英文常量）。
            { "MenuShop/RefreshCounter",                      new Entry("刷新于：", "Refreshes in:") },

            // ============================================================ 🆕 **2026-10-18（第六轮）：社交窗 `SocialMenu/` 那五条 + 一条跨窗共用的按钮**
            //
            // 键名 = 原版 prefab 上那颗 `Localize.mTerm` 的**原文**（本批按 pid 亲读：`Localize` 的
            //   `m_Script == 8610481073976370760` + `mTerm` + **同 GO/同族 TMP 的 `m_text`**；父链沿 `m_Father` 逐级）。
            // 英文那一列 = 那颗 TMP 的 `m_text` 原文逐字符照抄（⚠️ `Members:`/`Ranking:`/`Open alliances:`
            //   **原版串自带冒号** —— 别按「看着整齐」把冒号去掉）。
            // 中文那一列 = `数据/本地化/i18n/zh_CN.csv`（按**英文源串**索引）**精确命中**才写「取自」，
            //   否则写「近邻」或「自拟」——逐条写在下面。
            // 消费侧 = `Shell/AlliancesTab.cs`（`Open Alliances>Title` / 行的 `Members Header`·`Ranking Header` /
            //   `Create Alliance Text`）· `Shell/AllianceEventScorePanel.cs`（`Join Alliance text` /
            //   `View Leaderboard Button`·`Leaderboard Button` 的 `Button Text`）；
            //   断言 → `Editor/MainMenuScene.cs`（本批同批补的 5 组）。
            // ⛔ **`Alliances invitations:` 不在这里**：全库那颗文案的 TMP **1 颗、一颗 `Localize` 都没挂**
            //   ⇒ 原版没有词条（铁律 11 例外①）。⚠️ 表里**另有一条** `SocialMenu/Alliances/Invitations`
            //   —— 它挂在**别的节点**上，⛔ **别按名字硬套**（那是 P1 那类错）。
            // ------------- 联盟页（`Alliances Tab` 下那三处）-------------
            //   1 颗 · `Title < Open Alliances < List Area < List View < AllianceNotMemberVariant < Alliances Tab`
            { "SocialMenu/Alliances/OpenAlliances",  new Entry("开放联盟：", "Open alliances:") },   // TMP 原文；中文**自拟**（CSV 无此英文串）
            //   3 颗 · `Members Header < {Entry | Invitation List Entry | Alliance List Entry}`（两列行族共用这一条键）
            { "SocialMenu/Alliances/Members",        new Entry("成员：",     "Members:") },         // TMP 原文（**带冒号**）；中文 = `zh_CN.csv:273`「成员」+ 全角冒号
            //   4 颗 · `Ranking Header < …`（同上，两列行族共用）
            { "SocialMenu/Alliances/Ranking",        new Entry("排名：",     "Ranking:") },         // TMP 原文（**带冒号**）；中文**自拟**（CSV 里没有 `Ranking` 这个英文串）
            //   1 颗 · `Create Alliance Text < TopAnchor < Create Alliance View < … < Alliances Tab`
            { "SocialMenu/Alliances/CreateAlliance", new Entry("创建联盟",   "Create alliance") },   // TMP 原文；中文**自拟**（⚠️ `zh_CN.csv:229` 是 `Create Alliance (1000 Gold)`，**另一个串**）
            // ------------- 联盟面板（`Alliance Event Score Panel` 那两处）-------------
            //   2 颗 · `Join Alliance text < No Alliance < Alliance Event Score Panel < …`
            { "SocialMenu/Alliances/JointToEarnRewards", new Entry("加入联盟以获得额外奖励", "Join an alliance to gain additional rewards") }, // TMP 原文；中文取 `zh_CN.csv:263` 的**近邻**（那条的英文串是 `Join an Alliance for extra Rewards`，**与我们这串不同**）
            // ------------- 🔴 **跨窗共用**的一条（不是排位窗专属！）-------------
            //   6 颗，横跨三处：排位窗（`Ranked Division Info/RankedEventWindow[*]` 的 `LeaderboardButton`）·
            //   **联盟面板**（`View Leaderboard Button` / `Leaderboard Button` 的 `Button Text` —— 就是我们要接的那两颗）·
            //   头像页（`Profile Tab > Ranking > Current Rank/Highest Rank` 的 `LeaderboardButton`）。
            //   ⛔ 别以为它只属于排位窗（本批就是靠逐颗父链才认出联盟面板那两颗的）。
            { "MainMenu/RankedWindow/Leaderboard",   new Entry("排行榜",     "Leaderboard") },      // TMP 原文；中文 = `zh_CN.csv:119`（精确命中）

            // ============================================================ 🆕 **2026-10-18（第七轮）：社交窗那一族的最后一批**
            // 键名 / 英文列的口径同上一块（按 pid 亲读 `mTerm` + 同族 TMP 的 `m_text`；父链沿 `m_Father`）。
            // ------------- 好友页（`Friends Tab`）-------------
            //   2 颗（另一颗在 `Friends Menu Demo`）· `Placeholder < Text Area < Search Field < Find players panel < Header < Friends Tab`
            { "Demo/FriendsMenu/EnterPlayerName", new Entry("输入玩家名", "Enter player name") },   // TMP 原文；中文 = `zh_CN.csv:101`（精确命中）
            //   2 颗 · `Friends Title < Friends List < Friends Tab`
            { "Demo/FriendsMenu/YourFriends",     new Entry("你的好友：", "Your friends:") },       // TMP 原文（**带冒号**）；中文 = `zh_CN.csv:342`（精确命中）
            // ------------- 聊天窗（`ChatPanel`）-------------
            //   1 颗 · `Placeholder < Text Area < InputField (TMP) < Enter Text < Chat < Holder < ChatPanel`
            { "MainMenu/Chat/TypeMessage",        new Entry("输入消息",   "Type message") },        // TMP 原文；中文 = `zh_CN.csv:183`（精确命中）
            // ------------- 社交窗左栏那两颗页签（`TabBtnSpec.Label`）-------------
            //   各 1 颗 · `TabButtonLabel < Label < {Alliances,Friends} Tab Button < Tab Buttons < Content Area < Social Submenu Variant`
            //   ⚠️🔴 **渲染时会被基类 `ToUpperInvariant()` 转成大写**（`Shell/MenuWindowBase.cs` 的 `Text(b, (spec.Label ?? "").ToUpperInvariant(), …)`）
            //   ⇒ 英文档印的是 `ALLIANCES` / `FRIENDS`（**原版也这样**，那条 `Text` 就是照原版的 `TabButtonLabel` 建的）；
            //     中文（CJK）**不受 `ToUpperInvariant` 影响** ⇒ 中文档印「联盟」/「好友」。⛔ 别把 EN 列写成大写（改了反而与上面那条口径打架）。
            { "SocialMenu/Alliances",             new Entry("联盟",       "Alliances") },          // TMP 原文；中文**自拟**（CSV 无 `Alliances`；近邻 `zh_CN.csv:52` 是 `Alliance` 单数）
            { "SocialMenu/Friends",               new Entry("好友",       "Friends") },            // TMP 原文；中文**自拟**（CSV 无 `Friends`）
            // ------------- 联盟成员页那两颗页签里的第二颗（`AllianceMemberVariant`）-------------
            //   1 颗 · `Button Text < Generic Tab UI Button Trophies < Tab buttons < Alliance Header Buttons (1) < AllianceMemberVariant < Alliances Tab`
            { "SocialMenu/Alliances/Trophies",    new Entry("奖杯",       "Trophies") },           // TMP 原文；中文**自拟**（CSV 没有 `Trophies` 这个英文串）
            // ⚠️ 同排第一颗 `Generic Tab UI Button Info/Button Text` 的 `mTerm` 是 **`Settings/General/Title`**
            //   —— 原版**自己复用了设置窗那条通用词条**（本表 `:150` 那条，**不用新增**）。
            //   ⇐ 这是「**同一条 term 挂在两个窗**」的又一例（同 `MainMenu/General/Confirm` / `MainMenu/RankedWindow/Leaderboard`）。

            // ============================================================ 🆕 **2026-10-18（第八轮）：联盟页的最后 4 条**
            // 键名 / 英文列的口径同上一块（按 pid 亲读 `mTerm` + 同 GO 的 TMP `m_text`；父链沿 `m_Father`）。
            // ------------- 联盟页顶部那两颗页签键 --------------
            //   各 1 颗 · `Button Text < Generic Tab UI Button {Search,Create} < Tab buttons < Alliance Header Buttons
            //   < AllianceNotMemberVariant < Alliances Tab < Content Area < Social Submenu Variant`
            //   🔴 `Join` 这一条**运行期会被 `SetJoinLabel` 重设**（详情态换成 `Back`、回来再换回本键）
            //   ⇒ 消费侧做成了**属性**（`Shell/AlliancesTab.cs` 的 `LabelJoin`），⛔ 不是 `const`。
            { "SocialMenu/Alliances/Join",        new Entry("加入",       "Join") },               // TMP 原文 `Join`；中文**自拟**（CSV 无 `Join`）· 全库 6 颗（含两处列表行钮）
            { "SocialMenu/Alliances/Create",      new Entry("创建",       "Create") },             // TMP 原文 `Create`；中文**自拟**（CSV 无 `Create`）
            // ------------- 建盟页那两行输入框标题 --------------
            //   各 1 颗 · `{Name,Desc} input title < TopAnchor < Create Alliance View < AllianceNotMemberVariant < …`
            { "SocialMenu/Alliances/NameInput",        new Entry("联盟名称", "Alliance Name") },   // TMP 原文 `Alliance Name`（与我们原来写死的**逐字符相同**）；中文**自拟**
            { "SocialMenu/Alliances/DescriptionInput", new Entry("联盟简介", "Alliance Description") }, // TMP 原文 `Alliance Description`（同上）；中文**自拟**

            // ============================================================ 🆕 **2026-10-18（第十轮 · B 类做完）：最后 3 条**
            // 键名 / 英文列的口径同上一块（按 pid 亲读 `mTerm` + 同 GO 的 TMP `m_text`；父链沿 `m_Father`）。
            // ⚠️ 这一块的三条中文列**全部是我们自拟的** —— `zh_CN.csv` 里 `Dismiss` / `Select language` /
            //   `Select privacy` **三个英文串一条都没有**（按第一列精确查过）。
            // ------------- 邀请行那两颗钮里的第二颗（`Reject`）-------------
            //   3 颗同键（`Invitation List Entry` 的两个实例 + 它的 `(1)` 模板）·
            //   `Button Text < Reject < Invitation List Entry < List < Invitations < List Area < List View <
            //    AllianceNotMemberVariant < Alliances Tab` ⇒ **节点名 `Reject` 与我们逐字相同**
            { "SocialMenu/Alliances/Dismiss",       new Entry("拒绝",     "Dismiss") },              // TMP 原文 `Dismiss`；中文**自拟**
            // ------------- 建盟页那两个下拉的标题 --------------
            //   各 1 颗 · `{Select Language,Select Privacy} < TopAnchor < Create Alliance View <
            //   AllianceNotMemberVariant < Alliances Tab` ⇒ **节点名与我们逐字相同**（`Shell/AlliancesTab.cs` 的 `Dropdown(...)`）
            // ⚠️ 与本表 `MainMenu/Settings/ButtonLabel/SelectLanguage`（「选择语言」/`Select Language`）**是两条不同的键**
            //   —— 原版在设置窗用前者、在建盟页用这两条；⛔ 别以为中文一样就合并（键名照原版 `mTerm`）。
            { "SocialMenu/Alliances/SelectLanguage", new Entry("选择语言", "Select language") },     // TMP 原文 `Select language`；中文**自拟**（与本表设置窗那条同词，但**不同键**）
            { "SocialMenu/Alliances/SelectPrivacy",  new Entry("选择隐私", "Select privacy") },      // TMP 原文 `Select privacy`；中文**自拟**

            // ============================================================ 🆕 **2026-10-18（第十一轮）：B 类最后 6 条**
            // ------------- 成员操作弹窗（`Member Options Panel`）那四颗钮 --------------
            //   各 1 颗 · `Button Text < {Promote,Demote,Kick,Quit} < Buttons < Member Options Panel`
            //   ⇒ **节点名与我们逐字相同**（`Shell/AllianceMemberOptionsPopup.cs` 的 `Buttons[i].Node`）。
            //   🔴🔴 **英文列有两种情况，别一刀切**（这是本表里**唯一**一部分「EN 列不是原版 TMP 原文」的条目）：
            //     · `Promote` 那颗的原版 TMP **就是英文 `Promote`** ⇒ EN 列 = **原版原文** ✅
            //     · 另三颗的原版 TMP 是**西语占位串**（`Degradar` / `Expulsar Jugador` / `Abandonar Alianza`）
            //       ⇒ **英文原文取不到**（原版那套在远端 I2 表）⇒ **这一列是我们自拟的英文**
            //       （措辞 = 直接取词条名的末段，同 `MenuDeck/Button/Random` / `MenuDeck/MenuButtons/ImportDeck` 那两条先例）。
            //       ⛔ **别把西语当英文抄进来**（那会让人以为原版英文就是这个）。
            { "SocialMenu/Alliances/Promote",        new Entry("提升",     "Promote") },            // TMP 原文 `Promote`（**英文**）✅；中文自拟
            { "SocialMenu/Alliances/Demote",         new Entry("降级",     "Demote") },             // 🔴 EN **我们自拟**（原版 TMP = 西语占位串 `Degradar`）；中文自拟
            { "SocialMenu/Alliances/KickPlayer",     new Entry("踢出玩家", "Kick Player") },        // 🔴 EN **我们自拟**（原版 TMP = 西语占位串 `Expulsar Jugador`）；中文自拟
            { "SocialMenu/Alliances/Quit",           new Entry("退出联盟", "Quit") },              // 🔴 EN **我们自拟**（原版 TMP = 西语占位串 `Abandonar Alianza`）；中文自拟
            // ------------- 奖杯详情窗（`Alliance Trophy Info Popup`）那两行 --------------
            //   各 1 颗 · `Next Tier < Progress < Controls < RightSide < window`（TMP `Next Tier:`）·
            //   `Label < Checkbox < selectButton < Controls < RightSide < window`（TMP `Alliance featured trophy`）
            //   ⚠️ 与 `Shell/AllianceMemberTab.cs` 里那三处**样例串**（`Featured: Trophy Name` 等，**原版不挂 `Localize`**）
            //      **是两回事**（那三处⛔不接）；这两条是**另一扇窗里挂着词条**的两颗。
            { "SocialMenu/Alliances/Trophies/NextTier",           new Entry("下一档：",   "Next Tier:") },            // TMP 原文 `Next Tier:`（**带冒号**）；中文**自拟**（CSV 无该串）
            { "SocialMenu/Alliances/Trophies/FeaturedTrophyLabel", new Entry("联盟精选奖杯", "Alliance featured trophy") }, // TMP 原文；中文 = **`zh_CN.csv:53` 精确命中**

            // ============================================================ 卡面「兵种行」（原版 `RaceText`）
            //
            // 🔴 **键 = `Card_Race/<race>`** —— 这一族**不是自拟的**，是原版自己拼出来的：
            //   · `GameStaticData.CardRaceToString`（`d:/2/tools/decomp_full/GameStaticData__CardRaceToString.c`）
            //     —— Warlord 那一支是 `GetTranslation(<整条字面量>)`；类型不认识时它也**照原版出声**：
            //     `LogWarning("CardRaceToString undefined for " + 类型名)` 然后**印空串**（我们 `SubtypeLine`
            //     的 `default: return null` 就是它）。
            //   · `GameStaticData.MinionRaceToString`（同目录 `.c`）—— `String.Concat(<前缀>, 种族名)`
            //     再喂 I2 的 `GetTranslation` ⇒ **前缀 + 值** 两段拼。
            //   · 两条 `.rdata` 字面量**已读出来**（`工具` 那套 RVA 映射；与 `d:/2/tools/all_strings.txt`
            //     的表偏移**逐位对上**）：`0x1842cdf40` = **`"Card_Race/"`**（前缀）、
            //     `0x1842ce040` = **`"Card_Race/Warlord"`**（Warlord 那一支的整条）。
            //   · 同族旁证：原版 prefab 的类型筛选器 `locKey.mTerm = "Card_Race/Warlord"`
            //     （`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-3311978293714882780.json`，`option = 10`）。
            //   ⛔ **别改成 `Card/Subtype/…`** —— 那是自拟的；这一族有原版的键。
            //
            // ⚠️ **中文那一列只有 3 个是【实拍】**（用户 2026-10-17 的中文客户端截图
            //    `资料/原版参照图/用户实拍_1017/卡组编辑界面参考.png` 上逐字读到的）：`战将`（督军卡那一行）·
            //    `步兵`（守护者防御小队 / 风暴守护者 / 战巫 / 游侠）· `载具`（武器平台）。
            //    **其余 40 个是我们自己的中文表** `数据/本地化/i18n/zh_CN.csv` 的**既有译法**
            //    （⚠️ 那份表**也是我们译的**、不是原版 —— 见 `资料/全量反编译复核_靠推断的清单.md` §2.2；
            //     用它只图「全工程译法一贯」）。逐条出处写在各行的 `zh_CN.csv:<行>` 上。
            //    ⚠️ **`Card_Race/Warlord` 的实拍值是「战将」**，而 `zh_CN.csv:15` 那个**通用词**原来写的是「督军」
            //    —— ⚠️ **2026-10-18 订正（A877）**：这里原来写「两者**并存**：卡面这一行照**实拍**（战将），
            //      **效果文字里的「你的督军」是另一条线、不动**」—— 用户 2026-10-18 明确了
            //      「**游戏里改为战将。其他地方不动**」⇒ **效果文字那条线也要改**（`数据/卡牌翻译/zh_cards.json`
            //      的 58 处 + 生成物 `cards_engine.json`），`zh_CN.csv:15` 那条通用词也已一并改成「战将」。
            //      🔴 **唯一仍然照旧的是 `Card_Race/Warlord Ability`**（见下面那一行的注释）。
            //
            // 英文那一列 = **原版 `+600` 那个原始种族串**（也就是我们数据里 `subtype` 的原值）
            //   ——原版的英文 TMP 文案在远端 I2 表里、本地拿不到，所以**照原值**，不自拟。
            //
            // ⚠️ 下面 43 条 = `RuleEngine/Resources/cards_engine.json` 里**出现过的全部非空 `subtype`**
            //    （1126 张卡；其中**只有 19 个会真的印到卡面上**、共 722 张 —— 判据 `CardView.SubtypeLine`
            //     与 `CardView.TacticSubtypeShown`）。剩下 24 个**现在不印**，列在这里是为了
            //     「哪天白名单长一条，卡面不会突然掉回英文」。⛔ 这张表**不是**「引擎支持的 subtype 列表」。
            // ------------- 会印到卡面上的 19 个（★ = 实拍读到的那 3 个）-------------
            { "Card_Race/Infantry",            new Entry("步兵",       "Infantry") },           // ★实拍；zh_CN.csv:6363
            { "Card_Race/Vehicle",             new Entry("载具",       "Vehicle") },            // ★实拍；zh_CN.csv:6365
            { "Card_Race/Warlord",             new Entry("战将",       "Warlord") },            // ★实拍；⚠️ zh_CN.csv:15 那条通用词 2026-10-18 起也是「战将」（A877，原来写「督军」）
            { "Card_Race/Defence",             new Entry("防御",       "Defence") },            // zh_CN.csv:6367
            { "Card_Race/Monster",             new Entry("怪兽",       "Monster") },            // zh_CN.csv:6369
            { "Card_Race/Beast",               new Entry("野兽",       "Beast") },              // zh_CN.csv:6372
            { "Card_Race/Battlesuit",          new Entry("战甲",       "Battlesuit") },         // zh_CN.csv:6374
            { "Card_Race/Drone",               new Entry("无人机",     "Drone") },              // zh_CN.csv:6377
            { "Card_Race/Psychic Power",       new Entry("灵能",       "Psychic Power") },      // zh_CN.csv:6375
            { "Card_Race/Combat Elixir",       new Entry("战斗药剂",   "Combat Elixir") },      // zh_CN.csv:6391
            { "Card_Race/Daemon",              new Entry("恶魔",       "Daemon") },             // zh_CN.csv:6381
            { "Card_Race/Secret",              new Entry("隐秘",       "Secret") },             // zh_CN.csv:404
            { "Card_Race/Dark Pact",           new Entry("黑暗契约",   "Dark Pact") },          // zh_CN.csv:378（`CardText.KeywordZhNames` 的 darkpact 同值）
            { "Card_Race/Sabotage",            new Entry("破坏",       "Sabotage") },           // zh_CN.csv:403（…的 sabotage 同值）
            { "Card_Race/Codicil",             new Entry("法典",       "Codicil") },            // zh_CN.csv:6408
            { "Card_Race/Genomic Enhancement", new Entry("基因强化",   "Genomic Enhancement") },// zh_CN.csv:387
            // 🔴 **2026-10-18（A879）就地改值：中文列 `计策` → **`祈唤`**** —— 三条判据（主对话已亲核）：
            //   ① **原值 `计策` 与「卡类别」撞名**：`type='tactic'` 那类卡在 PnP 目录里就叫 `4计策/`
            //      （`d:/2/Warpforge部队卡片/Tau/4计策/Warpforge_52_Sense-of-Stone.png` 那一族），
            //      而本键是**卡面兵种行**（卡面那行橙字）—— 那张成品卡的兵种行印的是英文 **`Invocation`**。
            //   ② 我们那份词表 `数据/本地化/i18n/zh_CN.csv:6386` = `Invocation,~,祈唤`。
            //   ③ 本键**会真印到卡面上**（`Core/CardView.cs` 的 `TacticSubtypeShown` 白名单含 `Invocation`，
            //      共 3 张卡）⇒ 改它 = **玩家可见**。
            //   ⛔ 别去改 `zh_CN.csv`（它是 `工具/sync_from_d2.py` 从 `d:/warpforge/data/i18n` 同步来的产物，
            //      改了会被下次同步覆盖）—— 它**已经**是 `祈唤`，本条只是让 C# 侧跟上。
            { "Card_Race/Invocation",          new Entry("祈唤",       "Invocation") },         // zh_CN.csv:6386（A879：原值「计策」与卡类别 `4计策/` 撞名）
            { "Card_Race/Overlord Power",      new Entry("霸主之力",   "Overlord Power") },     // zh_CN.csv:6384
            { "Card_Race/Rune",                new Entry("符文",       "Rune") },               // zh_CN.csv:6385
            // ------------- 现在**不印**的 24 个（列全，白名单哪天长了不用回头补）-------------
            { "Card_Race/Ability",             new Entry("能力",       "Ability") },            // zh_CN.csv:6368
            { "Card_Race/Action",              new Entry("行动",       "Action") },             // zh_CN.csv:6376
            { "Card_Race/Astra Militarum",     new Entry("星界军",     "Astra Militarum") },    // zh_CN.csv:6407
            { "Card_Race/Battle Ability",      new Entry("战斗能力",   "Battle Ability") },     // zh_CN.csv:6389
            { "Card_Race/Card",                new Entry("卡牌",       "Card") },               // zh_CN.csv:6380
            { "Card_Race/Command",             new Entry("指挥",       "Command") },            // zh_CN.csv:6379
            { "Card_Race/Ephemeral",           new Entry("临时",       "Ephemeral") },          // zh_CN.csv:382
            { "Card_Race/Event",               new Entry("事件",       "Event") },              // zh_CN.csv:6371
            { "Card_Race/Mission",             new Entry("任务",       "Mission") },            // zh_CN.csv:6390
            { "Card_Race/Order",               new Entry("命令",       "Order") },              // zh_CN.csv:6387
            { "Card_Race/Pact",                new Entry("契约",       "Pact") },               // zh_CN.csv:6410
            { "Card_Race/Relic",               new Entry("圣物",       "Relic") },              // zh_CN.csv:6411
            { "Card_Race/Spell",               new Entry("咒语",       "Spell") },              // zh_CN.csv:6364
            // 🔴 **2026-10-18（A879）就地改值：中文列 `战术` → **`策略`**** —— 判据两条：
            //   ① **原值 `战术` 与 `Card_Race/Tactic`（下面那一行、58 张卡）撞名** ⇒ 两条不同的 `subtype`
            //      在中文界面里长得一模一样，分不出是哪一条。
            //   ② 我们那份词表 `数据/本地化/i18n/zh_CN.csv:6378` = `Stratagem,~,策略`
            //      （`:6366` 那条 `Tactic,~,战术` 不动 —— 它才是「战术」）。
            //   ⚠️ 本键**今天不印**（`CardView.SubtypeLine` 的白名单不含它）⇒ 无玩家可见差异；
            //      仍按 A879 与上面那条一起改（两条是同一笔账）。
            { "Card_Race/Stratagem",           new Entry("策略",       "Stratagem") },          // zh_CN.csv:6378（A879：原值「战术」与 Tactic 撞名）
            { "Card_Race/Structure",           new Entry("建筑",       "Structure") },          // zh_CN.csv:6382
            { "Card_Race/Support",             new Entry("支持",       "Support") },            // zh_CN.csv:325
            { "Card_Race/T'au Empire",         new Entry("钛帝国",     "T'au Empire") },        // zh_CN.csv:6412
            { "Card_Race/Tactic",              new Entry("战术",       "Tactic") },             // zh_CN.csv:6366
            { "Card_Race/Talent",              new Entry("天赋",       "Talent") },             // zh_CN.csv:422
            { "Card_Race/Trick",               new Entry("诡计",       "Trick") },              // zh_CN.csv:6413
            { "Card_Race/Upgrade",             new Entry("升级",       "Upgrade") },            // zh_CN.csv:185
            { "Card_Race/Warlord Ability",     new Entry("督军能力",   "Warlord Ability") },    // zh_CN.csv:6414（沿用该表原译，⛔ 别按上面那条改成「战将」）
            { "Card_Race/mission",             new Entry("任务",       "mission") },            // zh_CN.csv:6415（数据里小写那种写法）
            { "Card_Race/spell",               new Entry("咒语",       "spell") },              // zh_CN.csv:6416（同上）

            // ============================================================ 🆕 **2026-10-18（第十二轮）：`Battle/` 那一族（战斗侧收口）**
            //
            // 🔴 **读这一块之前先读这三句 —— 它们是这一族的总钥匙**（也是本批推翻前一轮普查的地方）：
            //   `Battle/` 的词条键在本地有**两种载体**，**只查一种 = 无效否定**（`资料/已知的坑.md` #20）：
            //   ① **prefab 上那颗 `I2.Loc.Localize` 的 `mTerm`**（`m_Script.m_PathID == 8610481073976370760`）
            //      —— 13 个战场场景 + `battlesharedresources` / `battleprefabs_vfxandmisc` / `menus` 里那一批。
            //      查得到 **28 条 `Battle/` 键**（`grep '"mTerm": "Battle/'  */MonoBehaviour/*.json | sort -u`，2026-10-18 实测）。
            //   ② 🔴 **代码里的字面量**（`d:/2/tools/il2cpp_out/stringliteral.json`，`RVA = 地址 − 0x180000000`）
            //      —— 这一族 prefab 上**一颗 `Localize` 都没有**，原版走的是
            //      `I2.Loc.LocalizationManager.GetTermTranslation(<字面量>, ...)`（逐条见下面各行的 `.c` 出处）。
            //      **实测：二进制里 `Battle/` 前缀的字面量共 93 条**（本批逐条 dump 过），
            //      而载波①只扫到 28 条 ⇒ 差额 **65 条**全在这一支里（`Battle/Cemetery/*` 19 条 ·
            //      `Battle/Effect/*` 25 条 · `Battle/Tips/*` 24 条 · `Battle/HUD/*` 8 条 …）。
            //   ⇒ **报「原版没有这个词条」之前，两条载体都要查**（本批就是靠载波②把
            //      「两张计数节点无词条」「战斗日志 0 词条」两条结论推翻的）。
            //
            // 英文那一列 = **本地能拿到的最硬的那份**（逐行写清是哪一份）；⚠️ **取不到的那几条如实标「自拟」**。
            // 中文那一列 = `数据/本地化/i18n/zh_CN.csv`（按**英文源串**精确命中才写 `csv:<行>`，否则写「自拟」）。
            // 消费侧 = `Battle/` 那一族（`EndPanel` · `MulliganPanel` · `ChoosePanel` · `SettingsPanel` ·
            //   `WaitBanner` · `CardDisplayWindow` · `MultiCardDisplay` · `BattleDriver`）；
            // 断言 → `Editor/BattleScene.cs` §W6（**逐键 `Loc.HasEntry` 一条**，坑表 #18：原版有 ≠ 表里有）。
            // 🧨 改坏法：删掉任一键 ⇒ `Loc.T` 返回**键名本身**（界面上印 `Battle/HUD/CardsLeft`）且 §W6 立刻红。
            // ------------------------------------------------------------------------------
            // ---- 载波①：prefab 上那颗 `Localize.mTerm`（本批逐颗实读；TMP 原文 = 同族的 `m_text`）----
            //   `Battle/Overtime/Title`：`bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4745.json`
            //   （`mTerm`）；同族 TMP（`OVERTIME!`）在该件 `Text` 子节点。13 个 arena 每场 1 颗。
            { "Battle/Overtime/Title",             new Entry("加时！",   "OVERTIME!") },   // TMP 原文 `OVERTIME!`；中文**自拟**
            //   `Battle/HUD/AffectedBy`：`…battlearena1/MonoBehaviour_4578.json`；TMP 原文 `Affected by:`（带冒号）
            { "Battle/HUD/AffectedBy",             new Entry("受到以下影响：", "Affected by:") }, // TMP 原文；中文 = 我们原来写死的那句
            //   `Battle/ChooseCard/Instructions`：`…battlearena1/MonoBehaviour_4871.json`；TMP 原文 `Choose one card`
            { "Battle/ChooseCard/Instructions",    new Entry("选择一张牌", "Choose one card") }, // TMP 原文（`ChooseCardMenu/ChooseText`）；中文 = 我们原来写死的那句
            //   `Battle/Mulligan/ButtonDone`：`…battlearena1/MonoBehaviour_{4185,4400,5289}.json`（**3 颗同键**）
            //   · TMP 原文 `Continue`。⚠️ `bundle_battlesharedresources_assets_all` 的
            //     `MonoBehaviour_-7805435555962860294.json` 同键但 TMP 是**西语占位 `Continuar`**（原版换牌那颗）
            //     —— 原版两处共用这一条 mTerm（运行期走 I2 表）⇒ 我们**共用一条键**（形状照原版），英文取英文那颗。
            { "Battle/Mulligan/ButtonDone",        new Entry("继续",     "Continue") },     // TMP 原文 `Continue`；中文 = `zh_CN.csv:84`（精确命中）
            //   `Battle/Prebattle/SelectButton`：`bundle_battlesharedresources_assets_all/
            //   MonoBehaviour_-7067688171680277716.json`，父链 `CardChooseCardButtonFrame < Generic Simplified
            //   UI Button_updated < Button Text`；TMP 原文 `Select`（**全库 4 颗同键**）。
            //   🔴 **推翻上一轮的「中文词条查不到 ⇒ 留英文」**：键**就在本地**（当时只扫了 `menus` 一个包）。
            { "Battle/Prebattle/SelectButton",     new Entry("选择",     "Select") },       // TMP 原文 `Select`；中文 = `zh_CN.csv:13`（精确命中）
            //   `Battle/Mulligan/secondTurn`：`…battlearena1/MonoBehaviour_5183.json`，TMP 原文 `You go second`
            { "Battle/Mulligan/secondTurn",        new Entry("你后手",   "You go second") },// TMP 原文；中文 = `zh_CN.csv:198`（精确命中）
            //   `Battle/Mulligan/Instructions`：`…battlearena1/MonoBehaviour_5197.json`，TMP 原文 `Choose cards to replace in first hand`
            { "Battle/Mulligan/Instructions",      new Entry("选择首局替换的卡牌", "Choose cards to replace in first hand") }, // TMP 原文；中文 = `zh_CN.csv:75`（精确命中）
            //   `Battle/Mulligan/Replace`：**全库只此 1 颗** —— `bundle_battleprefabs_vfxandmisc_assets_all/
            //   MonoBehaviour_4126337295248883513.json`（那颗 `ReplaceText`），TMP 原文 `Replace`
            { "Battle/Mulligan/Replace",           new Entry("换",       "Replace") },      // TMP 原文；中文**自拟**（csv 无 `Replace` 这个英文串）
            //   🆕 **2026-10-18（第十三轮 · G2b）`Battle/Mulligan/Undo`**：**同一颗钮的第二档字**
            //   （牌被标记要换时印这条）。判据 = `MulliganFrame.{ChangeCardButtonOnClick,SetupMulligan,
            //   UpdateButtonText}` 三处逐字相同的
            //   `term = (card+0x228 == 0xe) ? "Battle/Mulligan/Undo" : "Battle/Mulligan/Replace"`。
            //   ⚠️ 这条键**只在代码字面量里**（载波②；prefab 上零 `Localize`）⇒ 本地**没有**它的
            //      显示串 ⇒ EN/ZH **都自拟**（EN 取词条名末段，同 `MenuDeck/Button/Random` 的先例；
            //      `zh_CN.csv` 里 `Undo` 这个英文串**不存在**，2026-10-18 按第一列精确查过）。
            { "Battle/Mulligan/Undo",              new Entry("撤销",     "Undo") },        // 🔴 EN/ZH **都自拟**（值在远端 I2 表）
            //   `Battle/Mulligan/WaitEnemy`：`…battlearena1/MonoBehaviour_4804.json`（`BackCanvas/WaitText`），
            //   TMP 原文 **`Waiting for enemy`**。🔴 **推翻上一轮**「原版 `WaitText` 文案没查到 ⇒ 这条是我们加的」：
            //   节点有字也有键（当年只看了 dump 里那个空的 `Text`）。我们表里那句中文仍是我们的译法。
            { "Battle/Mulligan/WaitEnemy",         new Entry("等待对手", "Waiting for enemy") }, // TMP 原文；中文 = `zh_CN.csv:35`（精确命中）
            //   `MainMenu/Settings/SettingLabel/{Music,SoundFx}` + `Settings/Media/VoiceOvers`：
            //   三根音量滑块各 1 颗（13 个 arena + `menus` 各一份，共 14 颗/条），父链
            //   `BattleSettingsPanel < Volume Sliders < {Music,FX,Voiceover} Container < Text`；TMP 逐字同值。
            { "MainMenu/Settings/SettingLabel/Music",   new Entry("音乐", "Music") },        // TMP 原文；中文 = `zh_CN.csv:4`（`MUSIC` 大小写不同 ⇒ 近邻）
            { "MainMenu/Settings/SettingLabel/SoundFx", new Entry("音效", "Sound Effects") },// TMP 原文；中文**自拟**（csv 无 `Sound Effects`）
            { "Settings/Media/VoiceOvers",              new Entry("语音", "Voice-overs") },  // TMP 原文；中文**自拟**（csv 无 `Voice-overs`）
            //   `Battle/Settings/ResignButton`：`…battlearena1/MonoBehaviour_4196.json`，TMP 原文 `Resign`
            { "Battle/Settings/ResignButton",      new Entry("认输",     "Resign") },        // TMP 原文；中文 = `zh_CN.csv:454`（精确命中）
            //   `Battle/Settings/SkipTutorial`：`…battlearena1/MonoBehaviour_4307.json`，TMP 原文 `Skip tutorial`
            { "Battle/Settings/SkipTutorial",      new Entry("跳过教程", "Skip tutorial") }, // TMP 原文（小写 t）；中文 = `zh_CN.csv:162`（那条英文串是 `Skip Tutorial`，大小写不同 ⇒ **近邻**）
            //   `Battle/Tips/Continue`：`…menus/MonoBehaviour_-1010790197081052438.json`，TMP 原文 `Continue`
            { "Battle/Tips/Continue",              new Entry("继续",     "Continue") },     // TMP 原文；中文 = `zh_CN.csv:84`（精确命中）
            // 🆕 2026-10-18（第三会话 · `A985⑦`）：**抽牌被弃的提示**（手牌满 ⇒ 抽到那张直接进弃牌堆）。
            //   原版那支 = `PlayerHand._AddDrawnCardToHand_d__37:129-141` ⇒ `Hand.Count < MaxCardsInHand`
            //   才进手牌，否则**进墓地 + `ShowHeadsUpMessage(GetTermTranslation(0x428A218))`**。
            //   🔴 **这一条的值【不是原版的】** —— `0x428A218` 查出来的那个键的**文案在远端 I2 表**，本地拿不到；
            //   下面是**我们自己写的兜底**（`BattleDriver.HandFullText()` 的文档里已如实标注）。
            //   ⚠️ **本工程的规矩是不许静默失败**：宁可出声说一句我们的话，也不让玩家看着牌凭空消失。
            //   ⛔ 将来若远端值到位，**直接改这一行**即可（代码不用动：`Loc.HasEntry` 有就走它）。
            { "Battle/Tips/HandFull",              new Entry("手牌已满，抽到的牌直接进弃牌堆", "Hand full - the drawn card goes to the discard pile") },
            //   `Battle/Tips/{MeleeAttack,RangeAttack,HealthPoints}`：13 个 arena 各一颗，父链
            //   `Card Display Window < Card Display < TutorialObjs < UnitObjs < {Melee,Ranged,Health}Text`
            //   ⇒ TMP 原文 **`Melee Attack` / `Ranged Attack` / `Health Points`**。
            //   🔴 **这三条 + 下面 `EnergyCost` = `BattleDriver.cs` 教学校注那四块** ——
            //   该处原来的注写「本地没有 ⇒ 这四句是我们按卡面语义写的」**不成立**（四块逐块找得到），已就地订正。
            { "Battle/Tips/MeleeAttack",           new Entry("近战攻击", "Melee Attack") }, // TMP 原文；中文 = 我们原来那句（`Phrases["MELEE"]`）
            { "Battle/Tips/RangeAttack",           new Entry("远程攻击", "Ranged Attack") }, // TMP 原文；中文 = 我们原来那句（`Phrases["RANGED"]`）
            { "Battle/Tips/HealthPoints",          new Entry("生命值",   "Health Points") },// TMP 原文；中文**自拟**（csv 无 `Health Points`；`Health` 那条 csv:31 是另一个串）
            //   `Battle/Tips/GoFirst`：**没有 `Localize`（只此载波②）** —— `d:/2/tools/il2cpp_out/stringliteral.json`
            //   `0x428A128` = `Battle/Tips/GoFirst`。消费侧判据 = `MulliganManager.ActivateMulligan` 的先手/后手二选一
            //   （`MulliganPanel` 的 `TurnFirst`）——⚠️ **它的英文原文本地取不到**（远端 I2 表）⇒ EN/ZH 都**自拟**。
            { "Battle/Tips/GoFirst",               new Entry("你先手",   "You go first") }, // 🔴 EN/ZH **都自拟**（键名本地读得到，文案在远端）
            //   `Battle/BattleEnd/{Victory,Defeat,Draw}`：**载波①** —— `bundle_menus_assets_all` 的
            //   `BattleLogItem`（`MonoBehaviour_-7028880557435028942.json` / `…_8607776031950241599.json`）
            //   那两个 `LocalizedString` 字段 `victoryKey` / `defeatKey` / `drawKey`（脚本类 = `BattleLogItem`，
            //   `d:/2/Warpforge_code/Scripts/Assembly-CSharp/BattleLogItem.cs`）。
            //   🔴 **落点要说清**：它们挂在**战报条目**上，**不是** `EndBattlePanel` ——
            //   原版结算面板子树**没有标题节点**（胜负靠 `Video Image` 播视频）⇒
            //   **我们结算面板那两句标题本身是自加的**（如实标着），这里取的是**语义相同**的原版键。
            //   英文：`Victory` 有本地实据（那两颗 `result` TMP 的 `m_text = 'Victory'`，fs 65.35）；
            //   `Defeat` / `Draw` **本地取不到** ⇒ 自拟（取词条名末段，同 `MenuDeck/Button/Random` 那两条先例）。
            { "Battle/BattleEnd/Victory",          new Entry("胜利",     "Victory") },      // EN = 原版 TMP 原文 `Victory`；中文 = `zh_CN.csv:332`（精确命中）
            { "Battle/BattleEnd/Defeat",           new Entry("失败",     "Defeat") },       // 🔴 EN **自拟**（原版那颗 TMP 是 `Victory` 样例）；中文**自拟**
            { "Battle/BattleEnd/Draw",             new Entry("平局",     "Draw") },         // 🔴 EN **自拟**（同上）；中文**自拟**
            //   `TutorialData/Lesson/{AttackLabel,HealthLabel}`：`…battlearena1/MonoBehaviour_{5023,5248}.json`，
            //   父链 `Tutorial < InitTutorialTip < TipText / TipText (1)` —— ⚠️ **TMP 原文本地是空的**
            //   （两行文字运行期由 `tutorial_stages.json` 喂）⇒ 词条**只给键**、文案仍取数据。
            //   ⇒ 这两条 EN/ZH **都自拟**（如实标），消费侧在**下一批**（`Battle/TutorialOverlay.cs`，本批不动）。
            { "TutorialData/Lesson/AttackLabel",   new Entry("攻击",     "Attack") },       // 🔴 EN/ZH 都自拟（TMP 空）
            { "TutorialData/Lesson/HealthLabel",   new Entry("生命",     "Health") },       // 🔴 EN/ZH 都自拟（TMP 空）
            // ---- 载波②：**键在代码里**（prefab 上零 `Localize`，原版走 `LocalizationManager.GetTermTranslation`）----
            //   `Battle/HUD/CardsLeft` ←→ `Battle/HUD/CardsInHand`（**两颗 HUD 计数节点**）：
            //   · `DeckManager.DisplayDeckSize` → `GetTermTranslation(<0x4288490>)` + `": "` + `n.ToString()`
            //     （`d:/2/tools/decomp_full/DeckManager__DisplayDeckSize.c`；分隔符那个 `_DAT_1842c7df8`
            //      在 `stringliteral.json` 里 = **`": "`**，`0x42C7DF8`）；
            //   · `PlayerHand.ShowHandSize` → `GetTermTranslation(<0x4288398>)` + `": "` + 手牌数
            //     （`d:/2/tools/decomp_full/PlayerHand__ShowHandSize.c`）。
            //   英文列 = 那两颗节点的**静态 TMP 原文**（`bundle_scenes_scenes_battlearena1`：
            //     `CardsInHandText` → `Cards left: XX`；`Player Deck Size Tex` → `Cards left: 2/5`）
            //     ⇒ 词条值 = **`Cards left`**（代码拼的是 `<词条>: <数量>`）。
            //   ⚠️ 两条英文一样、中文**分开写**（原版是两个不同的键、挂两处不同节点）。
            { "Battle/HUD/CardsInHand",            new Entry("手牌剩余", "Cards left") },   // EN = 节点静态 TMP；中文**自拟**
            { "Battle/HUD/CardsLeft",              new Entry("牌库剩余", "Cards left") },   // EN = 节点静态 TMP；中文**自拟**
            // ============================================================ 🆕 **2026-10-18（第十三轮 · G2b）：`Battle/HUD/` 那四条**
            //
            // 🔴 **本批要办的事**：`END TURN` / `YOUR TURN` / `ENEMY TURN` / `N available` 这四条
            //   **原版都有确凿 `mTerm`**，而我们这边**一直走的是另一条路**
            //   （`Core/CardText.cs` 的 `Phrases` 表，键 = 英文原文）⇒ 正是工程红线
            //   「**两处写同一条规则 = 迟早不一致**」的一个现存实例。本批把这四条统一到本表，
            //   `Phrases` 那三个键**降级成「表里没有这个键」时的兜底**（转发见 `CardText.PhraseTerms`）。
            //
            // 载波 = **②（代码里的字面量）**：`ClockManager` / `BattleManager` / `SupportMethods` 三处的
            //   `LocalizationManager.GetTranslation(<字面量>)`（prefab 上**零 `Localize`** ⇒「按 `mTerm` 扫」
            //   对它们是**无效否定**，见 `资料/已知的坑.md` #20）。逐条方法体：
            // ── `Battle/HUD/{EndTurn,EnemyTurn}` ──────────────────────────────────────────
            //   `ClockManager.SetEndTurnText(bool isMyTurn)`（`d:/2/tools/decomp_full/ClockManager__SetEndTurnText.c`，
            //   **2026-10-18 亲读**）：按那个 bool **二选一**取词条（`0x4288678` = `Battle/HUD/EndTurn`、
            //   `0x4288768` = `Battle/HUD/EnemyTurn`）→ `GetTranslation(词条)` → **`System.String.ToUpper`**
            //   → 写进 `TurnBtn/TurnText` 那颗 TMP。
            //   🔴 **英文列如实标**：词条**原文**在远端 I2 表（本地取不到），这里存的是**原版【显示】出来的那串**
            //   （= `ToUpper` 之后）—— 那也正是我们该印的字。本表**不做 `ToUpper`**，⛔ 别再补一层
            //   （本表存的已经是显示形态；原始 TMP 样例见 `battlearena1` 的 `TurnText` = `END TURN`，无 `Localize`）。
            //   ⚠️ 那个 bool 的**语义**是本批按「自己的回合才显示 END TURN」推的（`param_2 != 0 ⇒ EndTurn`），
            //      调用点没找到 ⇒ 只取「这两条键各对应哪个词」这一层，**不搬那个二选一的行为**。
            { "Battle/HUD/EndTurn",                new Entry("结束回合", "END TURN") },     // ZH = `zh_CN.csv:34`（`END TURN,~,结束回合`，**精确命中**）
            { "Battle/HUD/EnemyTurn",              new Entry("对手回合", "ENEMY TURN") },   // EN/ZH **都自拟**（`zh_CN.csv:446` 是**近邻**：`Enemy turn...,~,敌方回合……`）
            // ── `Battle/HUD/YourTurn` ────────────────────────────────────────────────────
            //   `BattleManager.NextTurn`（`BattleManager._NextTurn_d__395__MoveNext.c:480-483`）：
            //   `BattleTipController.ShowHeadsUpMessage(GetTranslation(词条), …)` = **回合开始那句抬头提示**
            //   （`0x4288a48`）。⇒ 它**不是** HUD 里那颗常驻节点的字（原版 HUD 的 `TurnText` 只有 `END TURN`
            //   那一个样例），所以本表这两条只提供**文案**；我们的回合行本身是自加的（见 `BattleDriver` 的注释）。
            { "Battle/HUD/YourTurn",               new Entry("你的回合", "YOUR TURN") },    // ZH = `zh_CN.csv:450`（`Your turn,~,你的回合`，**值相同**、键大小写不同 ⇒ 近邻）；EN 自拟（键名本地读得到、值在远端）
            // ── `Battle/HUD/TargetsAvailable` ────────────────────────────────────────────
            //   `SupportMethods.GetTargetsAvailableText(int n)`
            //   （`SupportMethods__GetTargetsAvailableText.c`，2026-10-18 亲读）= `GetTranslation(词条)`
            //   之后 **`System.String.Replace(词条值, "{0}", n.ToString())`** —— 那个占位符字面量
            //   `0x4265d10` 在 `stringliteral.json` 里就是 **`"{0}"`**（本批逐条读出）。
            //   ⇒ 词条值 = **`{0} available`**（占位符形态）；本地实据 = 那颗节点 `TargetsAvailableText`
            //     的静态 TMP **`0 available`**（= 拿 n=0 渲出来的那一实例）。
            //   🔴 中文列 = `zh_CN.csv:49`（`0 available,~,可用 0`）—— 同样是**带替换值**的实例
            //     ⇒ 模式 = **`可用 {0}`**（比我们原来那句 `可选目标 3` 更贴原版）。
            { "Battle/HUD/TargetsAvailable",       new Entry("可用 {0}",   "{0} available") },
            // ── `Battle/HUD/CreatedBy` ───────────────────────────────────────────────────
            //   🆕 **2026-10-18（第三会话 · `A985④` 收口）**：卡面上「**由谁造出来的**」那行字
            //   （原版节点 `CreatedByText`；表现层在 `Core/CardView.cs` 的 `SetCreatedBy`）。
            //   载波 = **②（代码里的字面量）**：字面量 `0x4288580` = `Battle/HUD/CreatedBy`、
            //   `0x4265D10` = `{0}`（`d:/2/tools/il2cpp_out/stringliteral.json`，本批逐条读出）。
            //   消费点 = `SupportMethods.GetCreatedByText(创建者卡名)`
            //   （`d:/2/tools/decomp_full/SupportMethods__GetCreatedByText.c` 亲读）
            //   = `GetTranslation(该键).Replace("{0}", 名字)` —— 与上一条 `TargetsAvailable` **同一个替换式**。
            //   ⚠️ 键名里的 `HUD` 是**原版自己的拼法**（那一族 8 条 `Battle/HUD/*` 都挂在卡面/抬头条上，
            //      ⛔ 别按我们 `Resources/.../HUD` 那种目录去理解它）。
            //   🔴 **英文列**：值在**远端 I2 表**（84 个本地 bundle 里没有 `localization_assets_all`）⇒
            //      本地拿得到的最硬的是**原版预制体里那颗 TMP 的占位串** `Created by someone fancy`
            //      （`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-1229881635839611202.json` 的 `m_text`，
            //      同类共 3 颗）⇒ **模板形态 = `Created by {0}`**。⛔ 别写成「原版印的就是这一句」——
            //      那一句是**样例值**，不是词条值。
            //   🔴 **中文列 = 我们把原来写死的那句原样搬进来的**（`Core/CardView.cs` 的 `CreatedByLine`）——
            //      **不是原版中文**（原版客户端无中文表；`zh_CN.csv` 按英文源串 `Created by` 精确查过，**0 命中**）。
            //   🔴 **加本行【不会】自动改掉界面上的字**（2026-10-18 亲读）：`Core/CardView.cs` 的
            //      `CreatedByLine` 是**自己按 `Loc.Current` 二选一拼的**（`$"由 {creatorName} 创建"` /
            //      `$"Created by {creatorName}"`），**它没有 `Loc.HasEntry` 这一跳** ⇒ 本行今天是**惰性的**。
            //      📌 要让它生效，得在 `Core/CardView.cs` 里把 `CreatedByLine` 改成
            //      `Loc.T("Battle/HUD/CreatedBy")` + `Replace("{0}", 名字)`（同族先例 = `HandFullText` /
            //      `CardText.TargetsAvailable` 那种 `HasEntry` 兜底写法）—— ⛔ 那一处在**别的笔的白名单**里，本笔不动。
            //      ⚠️ 本行取值与那两句**逐字相同**（`由 {0} 创建` / `Created by {0}`）⇒ 换过去那一笔**不会改变界面**。
            { "Battle/HUD/CreatedBy",              new Entry("由 {0} 创建", "Created by {0}") },

            // ============================================================ 🆕 **2026-10-18（第十五轮 · `G5`）：`Battle/Tips/` 两条 + `Battle/Effect/` 四条**
            //
            // 🔴 **本块的共同出处口径**（两条都是**载波②**：键在**代码字面量**里，prefab 上零 `Localize`
            //   ⇒ 「按 `mTerm` 扫」对它们是**无效否定**，`资料/已知的坑.md` #20）：
            //   地址表 = `d:/2/tools/il2cpp_out/stringliteral.json`（`RVA = 地址 − 0x180000000`），
            //   消费点 = `d:/2/tools/decomp_full/` 的方法体。**两列文案都是我们自拟的**
            //   （原版显示串在**远端 I2 语言表**，本地 84 个 bundle 里没有 `localization_assets_all.bundle`）。
            // --------------------------------------------------------------------------------------
            // ── `Battle/Tips/DragToTarget`（`0x428A030`）──────────────────────────────────────────
            //   消费点 = **两处**，都在 `BattleManager.Update`（`BattleManager__Update.c`）：
            //     · `:642-672`（`case 4`）：从手牌拖出去、松手时指针下**没有**卡（`*plVar25 == 0`）
            //       或那张卡不是合法目标（`IsValidSpellTarget` 假）⇒ `CardScript.MoveBackToHand`
            //       + `PlayerHand.HandCardReleased` + `BattleTipController.NotifyCantDoAction(GetTermTranslation(该键))`；
            //     · `:1196-1213`（`case 10`）：**瞄准型主动技能**松手落空 ⇒ 同一条键 +
            //       `BattleManager.CancelAbilityTargeting`。
            //   ⇒ 语义 = **「拖出去、松手时没落到有效目标」**（落到空白处 **和** 落到非法目标 **都算**）。
            //   ⚠️ `NotifyCantDoAction(text, flag)`（`BattleTipController__NotifyCantDoAction.c:5-14`）
            //      = **先**播 `ChatMessage.ICantDoThat`（= 我们的 `SpeakCantDo`）**再** `ShowHeadsUpMessage(text)`
            //      —— 语音与这行**文字**是**同一次**调用的两半；我们过去只做了语音那一半（= 复刻缺漏）。
            //   🔴 EN/ZH **都自拟**（值在远端 I2 表；`zh_CN.csv` 按英文源串精确查过，0 命中）。
            { "Battle/Tips/DragToTarget",          new Entry("拖到目标上再松手", "Drag to a target") },
            // ── `Battle/Tips/UnitNotReady`（`0x428AAB0`）─────────────────────────────────────────
            //   消费点 = `BattleManager.CanUseActiveAbility`（`BattleManager__CanUseActiveAbility.c:116-132`）：
            //   判据是 `card + 0x230` = **`CardScript.canAct`（`ObscuredBool`）**（`dump.cs` 的字段表实读）
            //   ——`canAct == false` 且调用方要求出声时 ⇒ 这一条 + `NotifyCantDoAction`。
            //   ⇒ 语义 = **「这个单位现在动不了」**（本回合已行动 / 不能行动）。
            //   ⚠️ 我们过去这一档走的是 `CardText.Phrase("THIS UNIT ALREADY ACTED")` ——
            //      `Phrases` 里那条**没有对应 `mTerm`**（`G2b` §3.2 那 17 条之一），
            //      **本批把这条键找出来了** ⇒ 那一处改用本键（`CardText.Phrases` 那条降级成兜底、不删）。
            //   🔴 EN/ZH **都自拟**（值在远端 I2 表）。
            { "Battle/Tips/UnitNotReady",          new Entry("这个单位已经行动过了", "This unit is not ready") },

            // ── `Battle/Effect/*` 四条（🔴 **消费面本批已钉死**）──────────────────────────────────
            //   消费链（逐条方法体亲读）：`CardEffectItem.LoadEffect(effect, isPlayer)`
            //     （`CardEffectItem__LoadEffect.c:98-117`）→ `GameStaticData.GetEffectDesc(effect)`
            //     （`GameStaticData__GetEffectDesc.c`，`case 1` 那一支）→ 写进 `CardEffectItem.effectText`
            //     （`+0x28`；`+0x20` 是 `enchanterText` = 「谁给的」）。
            //   上层调用点 = `CardEffectsService.EnableEffects` / `CardDisplayWindow.DisplayCardEffects`
            //     ⇒ **就是我们的 `Battle/CardDisplayWindow.RowsOf`**（卡面展示窗那一块「受到以下影响：」
            //     的两行 —— `谁给的` + `给了什么`）。⚠️ `G2b` 曾把消费面记成「攻方卡亮相」，**那是错的**
            //     （那一步是 `BattleDriver` 的 reveal 动画，跟 `CardEffectItem` 无关）⇒ 本批就地订正。
            //   🔴 **只用 `…OneTurn` 那四条**：我们引擎里这一类是 `UnitState.TempBuff`（**限时**增益，
            //     到回合边界必撤）⇒ 对应原版的 `…OneTurn` 档；原版无后缀那四条是**永久**改属性，
            //     我们这边不走 `TempBuff`（直接进单位数值）⇒ 那四条**今天没有消费点**，⛔ 先不加空键。
            //   ⚠️ 占位符 = **`{0}`**（`0x4265d10`，`stringliteral.json` 实读；`GetEffectDesc` 里那句
            //     `System_String.Replace(lVar5, DAT_184265d10, GetStatParam(...))`）——
            //     🔴 **不是** `{[0]}`：`{[0]}`（`0x42735c8`）只出在 `ChangeAbilityCost` / `SetCost`
            //     那两条上（同一个方法体的另外两个 `case`）⇒ 两套占位符**并存**，别互相顶替。
            //   中文列 = **改之前 `CardDisplayWindow.AttrZh` 的那几个词** ⇒ **今天中文档零变化**
            //     （`"{0} 近战"` 里 `{0}` 填 `+2` ⇒ 逐字等于原来的 `"+2 近战"`）。
            { "Battle/Effect/ChangeMeleeAttackOneTurn",  new Entry("{0} 近战",  "{0} Melee Attack") },
            { "Battle/Effect/ChangeRangedAttackOneTurn", new Entry("{0} 远程",  "{0} Ranged Attack") },
            { "Battle/Effect/ChangeHealthOneTurn",       new Entry("{0} 生命",  "{0} Health") },
            { "Battle/Effect/ChangeArmourOneTurn",       new Entry("{0} 护甲",  "{0} Armour") },

            // ============================================================ 🆕 **2026-10-18（第十四轮 · `G8`）：卡组存档那两条**
            //
            // 🔴 **这一族的口径**：`RuleEngine/Data/DeckStore.cs` 的 `LoadNote` / `SaveAll` **不再产出
            //   「给人看的整句中文」** —— 它出**错误码 + 诊断串**；给人看的两句在**这里**。
            //   消费侧 = `Deck/DeckRuntime.cs` 的 `SaveFailReason()`（`MenuDeck/Error/SaveFailed`）·
            //   `DeckLibrary.LastLoadIssueTerm`（`CustomErrors/ErrorLoadDeck`，今天那个值挂在
            //   `Battle/BattleDriver.PickSavedDeck` 的 `note` 上）。
            //   断言 → `Editor/DeckScene.cs` 的 `G8` 那一节（**两语档各断一次** + 单独钉「表里有这个键」）。
            // ------------- ① 读存档失败 —— **原版键**（载体 = 代码字面量）-------------
            // 键名 `CustomErrors/ErrorLoadDeck`，三种载体（⛔ 报「原版没有」之前要把载体打全）：
            //   · **代码字面量**：`d:/2/tools/il2cpp_out/stringliteral.json` 的 `0x425D0A8`；
            //   · **消费点**：`d:/2/tools/decomp_full/SearchOpponentManager__SearchOpponent.c:89-90`
            //     （`I2.Loc.LocalizationManager.GetTermTranslation(<该地址>, …)`）；
            //   · **枚举**：`CustomError.ErrorLoadDeck = 170`
            //     （`d:/2/Warpforge_code/Scripts/Assembly-CSharp/CustomError.cs:24`；同族键 = `"CustomErrors/" + 名字`）。
            // 🔴 **英文/中文两列都是我们自拟的** —— 原版显示串在**远端 I2 语言表**里，本地取不到：
            //   ① 载波①（prefab `Localize.mTerm`）里**没有**这一条（全库扫 `"mTerm": "CustomErrors/…"`
            //      只命中 **2** 条：`DuplicateConnection` / `ErrorSavingMatch`，都在 13 个战场场景那颗
            //      `BattleErrorUIManager` 上）；② `数据/本地化/i18n/zh_CN.csv` 里**没有**对应的英文源串
            //      （按第一列精确查过，0 命中）。⇒ 照 `MenuDeck/Button/Random` 那条的先例如实标「自拟」。
            // ⚠️ 它**不是**「把写死的中文搬个家」：原来那句是 `DeckStore` 拼的**整句**、而且
            //   `DeckLibrary` 曾经拿 `Contains("失败")` 把它**当类型用**；现在这里只是**同一件事的显示形态**，
            //   判据在 `DeckLibrary.LastLoadIssue` / `LastLoadCode`（枚举）。
            { "CustomErrors/ErrorLoadDeck",             new Entry("卡组存档读取失败", "Could not load your decks") },
            // ------------- ② 落盘失败 —— 🔴 **自拟键（原版没有这一条）** -------------
            // 原版卡组存在**服务器**上（`CardDeck.syncedToServer` / `deckId`），「本机写不进存档文件」
            //   这件事在原版里**不存在** ⇒ 四种载体逐条查过、**都没有**：
            //   `CustomErrors/*` **13** 条 · `MenuDeck/Error/*` **12** 条（只有 `FailedToDelete` 那种）
            //   · `MainMenu/General/*` **13** 条 · `decomp_full` 的 `CustomError` 枚举里最接近的是
            //   `ErrorSavingMatch` = **保存对局**（语义不同 ⇒ ⛔ 不借它）。
            //   ⇒ 键名与两列文案都是我们起的（形状照 `MenuDeck/Error/*` 那一族）。
            // 🔴 **中文那一列 = 改之前写死在 `DeckRuntime.SaveFailReason()` 里的原话** ⇒ **中文档零变化**；
            //   英文那一列是我们译的。
            // ⚠️ 同族那一半 `Shell/CollectionData.SaveFailReason()`（A503）**还是写死的中文**
            //   （`Shell/**` 不在 `G8` 的白名单里）⇒ 两半在**英文档**下会分叉，已记进交件报告。
            { "MenuDeck/Error/SaveFailed",              new Entry("写不进存档文件", "Could not write the save file") },

            // ============================================================ 🆕 **2026-10-18（`G9`）：`DeckRules.Describe` 那一族**
            //
            // 🔴 **为什么有这一段**：`RuleEngine/Core/DeckRules.cs` 的 `Describe(DeckError)` 原来直接
            //   造**中文整句**（引擎侧唯一一处产人话的地方），而它同时被显示层和断言当文案用
            //   ⇒ 换语言时**静默不跟**。`G9` 把它改成**只出词条键**（`"MenuDeck/Error/" + 枚举名`），
            //   人话搬到这里 —— 显示层走 `Loc.T(键)`（唯一出口 = `DeckRuntime.DeckErrorText`）。
            //
            // 🔴 **键名的判据**：原版两族键**并存**（见 `DeckRules.Describe` 的整段注释）——
            //   · **具名族** `"MenuDeck/Error/" + 名字`（`CardDeck.CanAddCard/CanAddHero/CanAddDefensive`
            //     三个方法体实读；地址表 `0x42CF1F0…0x42CFAF0`）；
            //   · **数字族** `"MenuDeck/Error/{0}"`（`0x42CFBF0`，`{0}` = 原版号；`DeckUtility.ToRawLocalizationString`）。
            //   我们这 13 条**仿具名族的形状**，但**名字是我们枚举的名字**（原版具名族只有 8 条 + 2 条 `Short`，
            //   ⛔ 一个都不重名 ⇒ 不会和原版实有字面量混淆）。
            // ⚠️ **两列文案都是我们起的**（原版那套在**远端 I2 语言表**，本地 84 个 bundle 里没有
            //   `localization_assets_all.bundle`）—— **中文那一列 = 改之前 `Describe` 里的原话**
            //   ⇒ **今天中文档零变化**；英文那一列是我们译的。
            // 断言 → `Editor/DeckScene.cs`（键形状 + 表里有键 + **中英两档各断一次**）
            //       与 `RuleEngine/Editor/DeckRulesTest.cs`（`Describe` 的形状）。
            // ------------- 13 条（顺序照 `DeckError` 的枚举声明序）-------------
            { "MenuDeck/Error/UnknownCard",             new Entry("卡组里有卡不在卡池中", "A card in this deck is not in the card pool") },
            { "MenuDeck/Error/NoWarlord",               new Entry("还没有选战将", "No warlord selected yet") },
            { "MenuDeck/Error/WarlordNotHero",          new Entry("战将位放的不是战将", "The warlord slot does not hold a warlord") },
            { "MenuDeck/Error/DefensiveMissing",        new Entry("还缺 1 张防御卡", "Missing 1 defence card") },
            { "MenuDeck/Error/DefensiveNotDefence",     new Entry("防御卡位放的不是防御卡", "The defence slot does not hold a defence card") },
            { "MenuDeck/Error/WrongFaction",            new Entry("有卡和战将不同阵营", "Some cards are not from the warlord's faction") },
            { "MenuDeck/Error/TooManyCards",            new Entry("卡组张数超了", "Too many cards in this deck") },
            { "MenuDeck/Error/TooFewCards",             new Entry("卡组张数不够", "Not enough cards in this deck") },
            { "MenuDeck/Error/CopyLimitExceeded",       new Entry("有卡超过了同名上限（传说 1 张，其余 2 张）", "A card exceeds its copy limit (1 for legendary, 2 for the rest)") },
            { "MenuDeck/Error/WarlordInCards",          new Entry("战将/防御卡不能放在普通卡位里", "Warlord / defence cards cannot go in the normal card slots") },
            { "MenuDeck/Error/WarlordAlreadySet",       new Entry("已经选过战将了（换战将要先把原来的撤掉）", "A warlord is already selected (remove the old one to swap)") },
            { "MenuDeck/Error/DefensiveAlreadySet",     new Entry("已经有防御卡了（不能带两张）", "A defence card is already assigned (only one is allowed)") },
            { "MenuDeck/Error/EffectOnlyCard",          new Entry("这张是**效果生成的卡**（药剂/破坏/秘仪），不能放进卡组",
                                                                "This is an **effect-generated card** (Elixir / Sabotage / Ritual) and cannot go into a deck") },
        };

        // ============================================================ 取值

        /// <summary>按**当前语言**取一条文案。没有这个键 ⇒ 返回**键名本身** + 出声（⛔ 不返回空串：
        /// 空串在界面上是「什么都没画」，那正是本工程最忌讳的静默失败）。
        /// <para>当前语言没有这一套文案（西/法/德/…）⇒ **回退英文** + 出声，见文件头。</para>
        /// <para>🔴 **2026-10-18（第四轮 · 审查 S2）**：缺键那声 `LogWarning` **按键去重**（`_warnedMissing`）——
        /// 同一条键**只出声一次**（不然被每帧调用时会刷屏，把抓日志型自检淹掉）。
        /// ⚠️ 记账（<see cref="MissingCount"/> / <see cref="LastMissingKey"/>）**照旧每次 +1**，不受去重影响。</para></summary>
        public static string T(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            Entry e;
            if (!Table.TryGetValue(key, out e))
            {
                MissingCount++;
                LastMissingKey = key;
                // 🔴 按键去重地出声（第一次问到这个键才出声；见 `_warnedMissing` 的 doc）
                if (_warnedMissing.Add(key))
                    Debug.LogWarning($"[Loc] 语言表里**没有**这个词条：`{key}`"
                                   + "（⇒ 界面上现在印的是**键名本身**，不是英文、更不是中文 —— 不许静默）。"
                                   + "加词条的规矩见 `Core/Loc.cs` 文件头（键名照原版 `Localize.mTerm`）；"
                                   + "⛔ 同一条键**只出声这一次**（去重表见 `_warnedMissing`）。");
                return key;
            }

            var cur = Current;
            if (cur == AvailableLanguages.Chinese && !string.IsNullOrEmpty(e.Zh)) return e.Zh;
            if (cur == AvailableLanguages.English && !string.IsNullOrEmpty(e.En)) return e.En;

            // 走到这里 = 当前语言没有这一套文案（或那一条在这个语言里是空的）⇒ 回退英文
            FallbackCount++;
            LastFallbackKey = key;
            string why = HasOwnText(cur)
                ? $"`{cur}` 里这条是空的"
                : $"`{cur}` 本地没有文案（原版那套在远端 I2 语言表里 —— 84 个本地 bundle 里没有 "
                + "`localization_assets_all.bundle`）";
            Debug.LogWarning($"[Loc] {why} ⇒ **回退英文**：`{key}` → \"{e.En}\"。"
                           + "（⛔ 不在 UI 上加原版没有的提示行 —— 这条日志就是全部提示）");
            return string.IsNullOrEmpty(e.En) ? key : e.En;
        }

        /// <summary>下拉里显示的语言名 = `T("MainMenu/Settings/LanguageName/&lt;枚举名&gt;")`。
        /// 🔴 **刻意不走回退**：选项名就是要显示「这一项叫什么」——
        /// 英语那一项在中文界面下显示「英语」、在英文界面下显示「English」，两者都对
        /// （原版也是这样：选项名按**当前界面语言**翻）。所以这里直接取表里那条的对应列。</summary>
        public static string LanguageName(AvailableLanguages lang)
        {
            return T(KeyOf(lang));
        }

        /// <summary>语言 → 它在表里的键（`MainMenu/Settings/LanguageName/&lt;枚举名&gt;`，原版拼法）。</summary>
        public static string KeyOf(AvailableLanguages lang)
        {
            return "MainMenu/Settings/LanguageName/" + lang;
        }

        /// <summary>本地有没有这个键的**任一**语言的文案（自检用：「表真的填了」那一条）。
        ///
        /// <para>🔴 **2026-10-18（`G9`）：`key == null` 要【出声】后返回 false，⛔ 不许丢给
        /// `Dictionary.ContainsKey`** —— 那会抛 `ArgumentNullException`，而上游只要有一处把
        /// 「本该非空的键」算成 `null`（例如某个 `LastLoadIssueTerm` / 某个 `Describe` 漏判），
        /// **整条自检会当场崩掉**：崩了既没有「期望 X / 实得 Y」，还容易被跑批的人当成
        /// 「这条环境不好」放过去 —— 那正是本工程最忌讳的静默失败。</para>
        /// <para>返回 **false**（不是 true）：本函数的语义一律是「表里有没有这条」，
        /// 键算成 null 就是**没有**。（`T(null)` 照旧返回空串、不出声 —— 那一条不变。）</para>
        /// <para>⚠️ 出声**只一次**（`_warnedNullKey`）：卡面/界面每帧刷新都可能走这条路，
        /// 逐次出声会把日志刷爆（同 `T()` 那条「缺键去重」的理由）。</para></summary>
        public static bool HasEntry(string key)
        {
            if (key == null)
            {
                if (!_warnedNullKey)
                {
                    _warnedNullKey = true;
                    Debug.LogWarning("[Loc] `HasEntry(null)` —— **上游把一个本该非空的词条键算成了 null**。"
                                   + "本函数返回 false（⛔ 不抛异常：抛了会让整条自检**崩掉**、而不是给一条红），"
                                   + "但这通常意味着上游有一处判空漏了 ⇒ 去把那处补上。"
                                   + "（同一条只出声这一次；去重位 = `_warnedNullKey`）");
                }
                return false;
            }
            return Table.ContainsKey(key);
        }

        static bool _warnedNullKey;

        /// <summary>取这条键的**英文列**（**不看当前语言**）。表里没有这个键 ⇒ 返回 <c>null</c>。
        /// <para>🆕 **2026-10-18（第十三轮 · G2b）为什么要开这个口**：`Core/CardText.cs` 有一道
        /// **字体闸**（拿不到中文字体资产时一律回英文，防方块 —— 见那个文件头）。那道闸要在
        /// **当前语言 = 中文但字体缺失**时拿到**英文那一列**，而 `T()` 只会按当前语言取、给不出英文。
        /// ⇒ 这个口只给那道闸用（`CardText.Term`）。⛔ **别拿它当第二条取值路** ——
        /// 界面上正常取文案**一律走 `T()`**（它才管回退、缺键出声、`FallbackCount` 那些账）。</para></summary>
        public static string EnOf(string key)
        {
            Entry e;
            return (!string.IsNullOrEmpty(key) && Table.TryGetValue(key, out e)) ? e.En : null;
        }

        /// <summary>
        /// 本表**所有中文列**（建中文字体资产时统计「要烘哪些字」的语料 —— 见
        /// `CardText.AllChinese()` 与 `Editor/TmpSetup.Corpus()`）。
        ///
        /// <para>🔴 **2026-10-18（第十五轮 · `G5`）为什么要开这个口**：`Editor/TmpSetup.cs:500` 的语料
        /// 原来只收 `CardText.AllChinese()`，而那一份收的是 `CardText` **自己那几张表**
        /// （`Names` / `KeywordNames` / `Phrases` / `FactionNames` + 技能卡面板那几个构件）
        /// —— **⛔ 不含本表**。后果：`W6` / `G2b` / `G8` / `G9` 新加的那一大批战斗侧中文
        /// （`手牌剩余` / `牌库剩余` / `结束回合` / `可用 {0}` / `撤销` …）**全都不在「要烘哪些字」的语料里**
        /// ⇒ `AllChinese()` 那条自检**覆盖不到它们**（今天不会真缺字，因为字体资产是 TMP **Dynamic**、
        /// 运行期按需补字形；但「改了文案忘了重烘」这一族就再也拦不住了）。</para>
        ///
        /// <para>⚠️ **和 `CardText.Zh` 那道语言闸无关**：本函数**永远给中文**（建资产那会儿字体还不存在，
        /// 语言闸必然是假）⇒ ⛔ 别把它也闸上（那个口径写在 `CardText.cs` 文件头）。</para>
        /// </summary>
        public static IEnumerable<string> AllChinese()
        {
            foreach (var kv in Table)
                if (!string.IsNullOrEmpty(kv.Value.Zh)) yield return kv.Value.Zh;
        }

        /// <summary>这一段文本里有没有汉字（决定按**汉字墨高**还是**拉丁大写高**定字号）。
        /// <para>判据 = `Battle/Label.SetCapHeight` / `SetGlyphHeight` 的 doc：拉丁大写约占 **0.72 em**、
        /// 汉字约占 **1 em** ⇒ 拿 `SetCapHeight` 喂中文会**小 28%**（`TmpFont` 文件头也写着同一件事）。
        /// 🔴 本方法是**唯一一份**这个判据：各调用点自己 `if (Loc.HasCjk(t)) … SetGlyphHeight … else … SetCapHeight …`
        /// （`Label` 不归本文件管，所以没法收成一个调用）。</para></summary>
        public static bool HasCjk(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                // CJK 统一表意文字 + 扩展 A + 兼容表意文字 + 中文标点（全角）
                if ((c >= 0x4E00 && c <= 0x9FFF) || (c >= 0x3400 && c <= 0x4DBF) ||
                    (c >= 0xF900 && c <= 0xFAFF) || (c >= 0x3000 && c <= 0x303F) ||
                    (c >= 0xFF00 && c <= 0xFFEF))
                    return true;
            }
            return false;
        }

        // ============================================================ 换语言 / 落盘

        /// <summary>换语言。返回「变了没有」。⚠️ **本类不发事件** —— 调用方自己把已经画出来的字重设一遍
        /// （两个设置窗各有一个 `RefreshTexts()`；我们这两个窗**不会同时开着** ⇒ 不需要广播，
        /// 而且静态事件在这个工程里是个漏：`Build()` 每次开窗都跑）。
        /// <para>落盘 = `PlayerPrefs`（键见 <see cref="PrefKey"/>）；<see cref="PersistOverride"/> 为真时只改内存。</para></summary>
        public static bool SetLanguage(AvailableLanguages lang)
        {
            Load();
            if (lang == _current) return false;
            _current = lang;
            if (!PersistOverride)
            {
                PlayerPrefs.SetInt(PrefKey, (int)lang);
                PlayerPrefs.Save();                        // 同 `WarpforgeAudio`：写完立刻落盘
            }
            Debug.Log($"[Loc] 语言 → `{lang}`（{LanguageName(lang)}）"
                    + (HasOwnText(lang) ? "" : " —— ⚠️ 本地没有这一套文案，界面文字**回退英文**（见 `Loc.T`）"));
            return true;
        }

        static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            int v = PlayerPrefs.GetInt(PrefKey, (int)Default);
            if (System.Enum.IsDefined(typeof(AvailableLanguages), v)) _current = (AvailableLanguages)v;
            else
            {
                // 存档里是个不认识的值（手改注册表 / 旧版本）⇒ 回出厂值并出声，⛔ 不静默
                Debug.LogWarning($"[Loc] `PlayerPrefs[\"{PrefKey}\"]` = {v} 不是合法的 `AvailableLanguages` 取值 ⇒ "
                               + $"回到出厂语言 `{Default}`（原版枚举取值是 0/10/20/…/110）。");
                _current = Default;
            }
        }

        // ============================================================ 自检

        /// <summary>自检用：**重新从 `PlayerPrefs` 读一次**（= 模拟「重开游戏」）—— 用来验落盘/回读那条链。
        /// ⛔ 不写盘。</summary>
        public static void ReloadForTest() { _loaded = false; Load(); }

        /// <summary>自检用：把内存态放回**出厂值**（⛔ 不动 `PlayerPrefs`）。</summary>
        public static void ResetForTest()
        {
            _loaded = true;
            _current = Default;
            MissingCount = 0; LastMissingKey = null; FallbackCount = 0; LastFallbackKey = null;
        }

        /// <summary>自检用：**按给定值**放回内存态（`ResetForTest` 放不回玩家的原值）。⛔ 不动 `PlayerPrefs`。</summary>
        public static void RestoreForTest(AvailableLanguages lang)
        {
            _loaded = true;
            _current = lang;
        }
    }
}
