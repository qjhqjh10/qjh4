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
            { "Card_Race/Invocation",          new Entry("计策",       "Invocation") },         // zh_CN.csv:6386
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
            { "Card_Race/Stratagem",           new Entry("战术",       "Stratagem") },          // zh_CN.csv:6378（与 Tactic 同译，两份都在）
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
        };

        // ============================================================ 取值

        /// <summary>按**当前语言**取一条文案。没有这个键 ⇒ 返回**键名本身** + 出声（⛔ 不返回空串：
        /// 空串在界面上是「什么都没画」，那正是本工程最忌讳的静默失败）。
        /// <para>当前语言没有这一套文案（西/法/德/…）⇒ **回退英文** + 出声，见文件头。</para></summary>
        public static string T(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            Entry e;
            if (!Table.TryGetValue(key, out e))
            {
                MissingCount++;
                LastMissingKey = key;
                Debug.LogWarning($"[Loc] 语言表里**没有**这个词条：`{key}`（界面上会显示键名本身 —— 不许静默）。"
                               + "加词条的规矩见 `Core/Loc.cs` 文件头（键名照原版 `Localize.mTerm`）。");
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

        /// <summary>下拉里显示的语言名 = `T("MainMenu/Settings/LanguageName/<枚举名>")`。
        /// 🔴 **刻意不走回退**：选项名就是要显示「这一项叫什么」——
        /// 英语那一项在中文界面下显示「英语」、在英文界面下显示「English」，两者都对
        /// （原版也是这样：选项名按**当前界面语言**翻）。所以这里直接取表里那条的对应列。</summary>
        public static string LanguageName(AvailableLanguages lang)
        {
            return T(KeyOf(lang));
        }

        /// <summary>语言 → 它在表里的键（`MainMenu/Settings/LanguageName/<枚举名>`，原版拼法）。</summary>
        public static string KeyOf(AvailableLanguages lang)
        {
            return "MainMenu/Settings/LanguageName/" + lang;
        }

        /// <summary>本地有没有这个键的**任一**语言的文案（自检用：「表真的填了」那一条）。</summary>
        public static bool HasEntry(string key) { return Table.ContainsKey(key); }

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
