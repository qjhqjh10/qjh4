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

        /// <summary>表里没有的键**被问到**的次数（`T()` 计数）。
        /// <para>🔴 **2026-10-19（`A1143`）语义漂了，如实记在这里（⛔ 只是记，行为一个字没改）**：
        /// 它原本读作「**缺词条【键】**数」，但从 `A1012` 起 **`Shell/PopUpGameWindow.Term()` 也会把
        /// 【明文站点】传进来的整句明文字符串喂给 `T()`**（壳侧 20+ 个调用点传的是明文、不是键，
        /// 见 `Shell/WindowsManager.cs` 里那两处「它们各自传的都是【明文】」）
        /// ⇒ 今天的口径是「**被问到的、而表里没有的【字符串】**数」——**不一定都是词条键**。
        /// 判据：同一个明文字符串第一次被问到也会 +1（`_warnedMissing` 只去重**日志**、不去重记账）。</para>
        /// <para>⚠️ **今天没有任何断言读它的【绝对量】**（全是「进来时存一份、出去比增量」，逐条核过：
        /// `Editor/BattleScene.cs` 的「提示码那节」· `Editor/DeckScene.cs` 的去重那两条 ·
        /// `Editor/SettingsScene.cs` 的 `ResetMissingWarnedForTest` 那节）⇒ 漂了也不红；
        /// 🔴 但**将来若有谁写「`MissingCount` 必须为 0」这类绝对断言，会被明文站点这一族【假红】**——
        /// 要用就用**增量**写法（同那三条先例）。</para></summary>
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

            // ---- 图像页 / 音频页 的**页标题**（2 条 · 🆕 2026-10-08 波 0b3 补）----
            //   🔴 键名 = **原版 `mTerm` 原文**（查到就用它，⛔ 没自拟）—— 两张表都搜过：
            //     · 表① `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_5215720994356428710.json`
            //       与 `…_8895938149081907110.json`：`"mTerm": "Settings/Graphics/Title"`（**同键两处**：
            //       页标题 fs55 + 页签 fs35；两者的 GO 名 = `Graphics Tab`）。
            //     · 表① `…/MonoBehaviour_-3908738614376169562.json` 与 `…_5584938879264653222.json`：
            //       `"mTerm": "Settings/Media/Title"`（同样两处，GO 名 = `Media Tab`）。
            //     · 表② `d:/2/tools/il2cpp_out/stringliteral.json` 同一族有 `Settings/Graphics/`
            //       （`0x4244CC8`）· `Settings/Media/WindowMode/{Fullscreen,Windowed}`
            //       （`0x4244DC8` / `0x4244EC8`）⇒ 这一族**真存在**（不是我们编的前缀）。
            //     · 搜过的词：`Settings/Graphics` · `Settings/Media` · `Settings/Audio` · `Graphics/`
            //       · `Media/` · `/Title`（表②逐词 0 命中除上面三条；表①按 `"mTerm": "Settings/` 全树扫）。
            //   ⚠️ **EN 列拿不到原版英文（如实记，不是猜）**：那四颗 TMP 的 `m_text` 就是**西班牙语**
            //     （`Gráficos` / `Multimedia` —— 本包 prefab 被本地化成西语了；英文在**远端 I2 表**）
            //     ⇒ EN 取**我们界面今天写死印的那串**（`Shell/SettingsWindow` 里
            //     `PageTitle(page, "Graphics")` / `PageTitle(page, "Audio")` 那两处字面量）——
            //     这条**是我们挑的**、⛔ 不是原版原文。ZH 同样**自拟**（`资料/待办判据_卡面卡池与双语.md` §23）。
            //   📌 若调度台要照原版那一族的英文：`Settings/Media/Title` 应写成 `Media`
            //     （依据 = 键名后缀 + GO 名 `Media Tab` + 西语译文 `Multimedia`）—— 那会**改英文档的可见字样**
            //     （页签 / 页标题从 `Audio` 变 `Media`）⇒ 属**需拍板项**，本件按「英文档零变化」取 `Audio`。
            { "Settings/Graphics/Title",              new Entry("图像", "Graphics") },
            { "Settings/Media/Title",                 new Entry("媒体", "Media") },

            // ---- ⚠️ 自拟键（原版没有这一页）----
            // 「联机」那一页**原版没有**（`Shell/SettingsWindow.cs` 文件头 ①：搜过
            // Online/Network/Server/Connect/Region/Ping/Multiplayer/Matchmak，设置窗里一个都没有）
            // ⇒ 这一条的键名与两列文案**都是我们起的**（照原版那一族的命名形状写）。
            { "Settings/Online/Title",                new Entry("联机",       "Online") },

            // ============================================================ 🆕 2026-10-19（A1185）账号页 / 登录弹窗
            //
            // 键 = **原版 `Localize.mTerm` 原文**（逐颗实读：`python -I d:/tmp/wf_w4probe/w4probe.py
            //   bundle_menus_assets_all "Account Tab" 8` 把 `Account Tab` 整棵（含它子树里那扇
            //   `Login Window`）的 `mTerm` 全扫了一遍 —— 去重后**正好这 16 条**）。
            // 英文那一列 = **那颗 TMP 的 `m_text` 原文逐字符照抄**（含 `E-mail` 那个连字符、
            //   `Log in` 那个空格、`Delete account` 那个小写 a）。
            // 🔴 中文那一列**是我们自己译的**（同本文件其余部分 —— 原版中文在**远端 I2 表**里，
            //   本地 84 个 bundle 无本地化包，见文件头）。措辞照本文件既有风格（`退出游戏`/`选择语言` 那一族）。
            // ⚠️ 五条社交那条的**英文列没有原版出处**：原版那五颗 `Button Text` 的 `m_text` 是**空串**
            //   （`m_IsActive = 0`，只为无障碍留的）⇒ EN 取**节点名那一档**（`Discord`/`Instagram`/…），
            //   与 `Shell/SettingsWindow.cs` 的 `AcSocEn` **同一份口径**（⛔ 那一份别另外再写一遍）。
            { "Settings/Account/Title",                       new Entry("账号",         "Account") },
            { "MainMenu/Login/Email",                         new Entry("电子邮箱",     "E-mail") },
            { "MainMenu/Login/Password",                      new Entry("密码",         "Password") },
            { "Settings/Account/ResetPassword",               new Entry("重置密码",     "Reset Password") },
            { "Settings/Account/ForgotPassword",              new Entry("忘记密码",     "Forgot Password") },
            { "Settings/Account/SubscribeToTheNewsletter",    new Entry("订阅新闻通讯？", "Subscribe to the Newsletter?") },
            { "MainMenu/Login/SignInButton",                  new Entry("登录",         "Log in") },
            { "MainMenu/Login/Register",                      new Entry("注册",         "Register") },
            // 🔴 键名**中间真有一个空格**（原版 `mTerm` 原文就是 `Settings/Account/Switch Account`）——
            //    逐字照抄，⛔ 别「顺手」改成 `SwitchAccount`（改了就取不到词条、界面上印键名）。
            { "Settings/Account/Switch Account",              new Entry("切换账号",     "Switch Account") },
            { "Settings/Account/Logout",                      new Entry("登出",         "Logout") },
            { "MainMenu/Settings/ButtonLabel/DeleteAccount",  new Entry("删除账号",     "Delete account") },
            // ⚠️ 原版那颗 `Twitch Button` 的 `mTerm` **指的就是 Youtube 这条**（prefab copy-paste 残留，
            //    prefab 里印的是 `Link Twitch`）—— 照抄原版（铁律 11），⛔ 不另造一条 Twitch 键。
            { "MainMenu/Settings/ButtonLabel/Youtube",        new Entry("YouTube",      "Youtube") },
            { "MainMenu/Settings/ButtonLabel/Discord",        new Entry("Discord",      "Discord") },
            { "MainMenu/Settings/ButtonLabel/Instagram",      new Entry("Instagram",    "Instagram") },
            { "MainMenu/Settings/ButtonLabel/Facebook",       new Entry("Facebook",     "Facebook") },
            { "MainMenu/Settings/ButtonLabel/Twitter",        new Entry("Twitter",      "Twitter") },

            // ---- 🆕 2026-10-19（A1183）支持页（原版 `Support Tab`）----
            // 键 = 原版 `Localize.mTerm` 原文（`w4probe … "Support Tab" 4` 逐颗实读；
            //   `Settings/Support/Title` 在原版里挂了**三处**：页签 `Tab Toggle Title` ·
            //   页标题 `Tab Title` · `Support Button > Button Text`）。
            { "Settings/Support/Title",            new Entry("支持",       "Support") },
            { "Settings/Support/FAQText",          new Entry("对游戏有疑问？请先看常见问题解答",
                                                               "Questions about the game? Visit the Frequent Asked Questions") },
            { "Settings/Support/FAQButton",        new Entry("常见问题",   "FAQ") },
            { "Settings/Support/MiddleText",       new Entry("需要我们帮忙吗？", "Do you need help from us?") },
            { "Settings/Support/ContactButton",    new Entry("联系我们",   "Contact") },
            // ⚠️ 英文列里那个换行是**原版 `m_text` 里的真换行**（照抄），不是我们排的版。
            { "Settings/Support/ContactText",      new Entry("也可以用 support@everguild.com 联系我们\n我们会尽力帮你！",
                                                               "You can also contact us at support@everguild.com\nWe'll do our best to help you!") },
            { "Settings/Support/TermsOfService",   new Entry("服务条款",   "Terms of Service") },
            { "Settings/Support/FAQTextMobile",    new Entry("对游戏有疑问？请看常见问题解答，或联系客服",
                                                               "Questions about the game? Check out the Frequent Asked Questions or contact Support") },
            // ⚠️ 这条**不在** `Settings/Support/` 前缀下（原版就那么写的）：底部两颗链里 `Privacy Policy`
            //    那颗 `Button Text` 挂的是 `MainMenu/Settings/ButtonLabel/PrivacyPolicy`，而
            //    `Terms of Service` 那颗挂的是 `Settings/Support/TermsOfService`（两条前缀不同，⛔ 别统一）。
            { "MainMenu/Settings/ButtonLabel/PrivacyPolicy", new Entry("隐私政策", "Privacy Policy") },

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

            // ------------- 🆕 **2026-10-19（`A1050`）：分享成功那条 toast 的词条** -------------
            // 键名 = **原版字符串常量表实读**（**不是自拟**）：`d:/2/tools/il2cpp_out/stringliteral.json:98759`
            //   的 `"MenuDeck/Share/ExportSuccesful"`（`address` `0x42D07E0`）；同族三条
            //   `MenuDeck/Share/{ShareOnAlliance,ShareOnGlobal,Title}` **紧邻**（`0x42D08E0/0x42D09E0/0x42D0AE0`），
            //   这一条就是它们四个里的第一个。
            //   🔴 **原版把 `Successful` 拼成 `Succesful`（一个 `s`）—— 一个字都别改**：
            //      改了就等于换了一条键，永远查不到（那一族的地址是连号的、拼写照抄才自洽）。
            // ⚠️ **这一条的「值」在远端 I2 语言表里，本地【没有】**（84 个本地 bundle 无本地化包、
            //   24.6 万文件扫中文串零命中 —— 同本文件头那段）⇒ **两列文案都是我们自拟的**，
            //   如实标注（先例 = `MenuDeck/Button/Random` / `MenuDeck/MenuButtons/ImportDeck` 那一族）。
            //   措辞取**现有兜底句去掉字符数那一截**（`DeckRuntime.ShareCopiedText`），
            //   让接上之后界面文案的**变化面最小**。
            //   ⚠️ **但那一版把「（N 字符）」一起丢了**（`A1050` 的选择）⇒ `A1153` 已把它接回来
            //      （本条目现在的值**带 `{0}`**，见下面那段更新）。
            // 🔴 **2026-10-19（第九会话 · `A1153`）就地更新（铁律 5）：下面那段「只报不改」已过期。**
            //   ⚠️ **原来写 X**：「本键一落表，那条 `（N 字符）` 就不再显示了 …… 要保住字符数得改调用点
            //      （把 N 拼进文案，或走带参数的口）—— 这一笔**只报不改**，留给调度台派活」。
            //   ⚠️ **实际是 Y**：字符数那一截**已经接回来了**，做法 = 本条的值改成**带 `{0}` 的模板**
            //      （`{0}` = 卡组串长度）+ 调用点（`Shell/DeckInfoPopup.cs` 的 `ShareDeck`）用
            //      `string.Format(Loc.T(键), s.Length)` 取词 ⇒ **中文档与改前【逐字相同】**
            //      （改前走兜底句 `DeckRuntime.ShareCopiedText(len)`，它拼的就是「卡组串已复制到剪贴板（N 字符）」；
            //       那句兜底句**一个字没动**，仍是「表里没这条键」那一支的文案）。
            //   ⚠️ **错因 Z**：`A1050` 落值时按「去掉字符数、变化面最小」选了措辞，那一步**把字符数一起丢了**
            //      ⇒ 现在用模板把它接回来。顺带修好英文档：改前 EN 档看到的是**中文**兜底句，现在是英文模板。
            //   ⚠️ 本表里带 `{0}` 的模板本来就有不少（`MenuDeck/Error/NoDeckForMode` ·
            //      `Settings/Online/Lobby/PeerLostHint` …）；🔴 但**取词与格式化必须是同一处** ——
            //      这条键的 `String.Format` 只在 `ShareDeck` 那一处做，⛔ 别在别处再折一次。
            // 消费侧 = `Shell/DeckInfoPopup.cs` 的 `ShareDeck`（`ShareSuccessTerm` 那个常量上写着键的判据）。
            { "MenuDeck/Share/ExportSuccesful",               new Entry("卡组串已复制到剪贴板（{0} 字符）", "Deck code copied to your clipboard ({0} characters)") },

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
            //   消费侧 = `Shell/LiveOpsEventWindow` 里那处；中文列 = `zh_CN.csv:230`（`Create deck,~,创建卡组`）。
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
            //   那会与 `Shell/ShopWindow` 的 `TxtEmpty`（同一句原文）分叉）。
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
            //   · `Daily Shop Tab` → `Atualiza em:`（**葡语占位串** —— 同 `Shell/ShopData` 那条注释说的）。
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

            // ============================================================ 🆕 **2026-10-08（第六会话 · `Core双语-第一步`）：`Tips/` 那一族**
            //
            // 🔴 **为什么单开一族**：`Core/Tooltip.cs` 的 `TipText` 那 10 条**全是我们写死的中文**
            //   （`A1013` 普查点名的 ①类 13 处里占 10 处）⇒ **英文档下悬停出来的还是中文**。
            //   本批把它们接上本表；另有 2 条 `Tips/Trait/*` 是**我们自己加的括注**（见那两条的注释）。
            //
            // 🔴 **键名的唯一判据 = 原版那颗 `EverguildTooltipTrigger` 的 `text` 字段**。
            //   ⚠️ **载体要说全**（坑表 #20）—— 这个族在**另两条载体上都是 0 命中**，
            //   只搜 `mTerm` / 只搜代码字面量都会得出「原版没有」的**无效否定**：
            //     · 表①（prefab `Localize.mTerm`）0 命中 —— 触发器**不是 `Localize`**；
            //     · 表②（`d:/2/tools/il2cpp_out/stringliteral.json`）0 命中 —— 这些串**不在代码里**。
            //   本批亲扫命令（`d:/2/新解包资源/assets_full`，4.4 GB / 24.7 万文件）：
            //     `grep -rho '"text": "Tips/[^"]*"' --include=*.json . | sort | uniq -c`
            //   ⇒ 全库**只有这 14 个 key**（逐条计数 = 挂了几颗触发器）：
            //     `Tips/CostTip` **305** · `Tips/RangedAttackTip` **152** · `Tips/MeleeAttackTip` **152**
            //     · `Tips/HealthTip` **152** · `Tips/OvertimeTip` 13 · `Tips/Hud/Skulls` **13**
            //     · `Tips/Hud/{Player,Opponent}{Energy,Faith,SpiritStone,QP}Count` 各 **13**
            //     （13 = 13 个战场场景各一颗；那 152/305 = 13 场 + 卡面展示窗 + 卡组编辑那几份）。
            //   🔴 **两个字段逐字吻合 = 键与挂点是同一处的硬证据**（本批亲读
            //     `bundle_staticgeneralassets_assets_all/MonoBehaviour/MonoBehaviour_-4253307515847882232.json`）：
            //     `text = Tips/HealthTip` + `tooltipAnchor = 10` + `offset.x = 73.05`，
            //     而我们那一条 `Battle/BattleDriver` 里出疲劳横幅那条链写的正是
            //     `Tooltip.Show(TipText.Health, at, 10, new Vector3(73.05f / TipPx, 0f, 0f))`
            //     —— **anchor 与 offset 一致**；`Cost`(10 / 52.6) · `Melee`(15 / −49.33) ·
            //     `Ranged`(15 / −53.54) 同样逐字对上（四个数在 `资料/tooltip_原版规格与实现.md` §一 的挂点表里也有）。
            //   ⚠️ **`Armour` 不在那 14 个 key 里** —— 原版**根本没有护甲 tooltip**
            //     （`子代理读报_2dcard_0827.md:95`：「`Armour Container` **此容器无任何脚本/无 tooltip**」）
            //     ⇒ 那一条的键名是本表**自拟**的（照原版这一族的命名形状 `Tips/<东西>Tip` 起）。
            //
            // 🔴 **两列文案【都自拟】—— 含键名照抄原版的那几条**（照 `A1013` 的判定，本批**复核过**）：
            //   原版**显示串在远端 I2 语言表**（84 个本地 bundle 里没有 `localization_assets_all`；
            //   `assets_full` 全库 `mTerm` 488 条里一条 `Tips/` 都没有）⇒ **只证到键名、没证到值**。
            //   · **ZH 列 = 改之前 `Core/Tooltip.cs` 里写死的那句逐字** ⇒ **中文档零变化**
            //     （这几条的值全是我们照规则书写的，行号就在句子里；⛔ 别当成「原版这么说」）。
            //   · **EN 列 = 我们照 ZH 列译的**（⛔ 不含汉字、不含全角括号 —— `Loc.HasCjk` 那片区间含
            //     `U+3000-303F` / `U+FF00-FFEF`）。
            //   ✅ **`Tips/Hud/*Count` 原版敌我【各一条键】已照原版拆开**（2026-10-09 · `A1086②`）：
            //     下面 `Player*` 那四条 + 紧随其后补的四条 `Opponent*`。消费点 = `BattleDriver.TickTooltipAt`
            //     （我方图标取 `TipText.{Energy,QuestPoints,Faith,SpiritStone}`、
            //      对面图标取 `TipText.{FoeEnergy,FoeQuestPoints,FoeFaith,FoeSpiritStone}`）。
            //     📌 键名的唯一判据仍是那颗 `EverguildTooltipTrigger.text`，全库逐条计数（亲扫命令见本节上面）：
            //     `Tips/Hud/Player{Energy,Faith,SpiritStone,QP}Count` 与 `Tips/Hud/Opponent{…}Count`
            //     **各 13 颗**（本批实读 `bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4394.json`
            //      = `text = Tips/Hud/OpponentEnergyCount` + `tooltipAnchor = 15` + `offset = (−42.38, −21.40)`，
            //      与同场我方那颗 `MonoBehaviour_4086.json` 的 **anchor 与 offset 逐字相同** ⇒
            //      原版**只靠键**区分敌我，值都从远端 I2 表取，见下面那两句）。
            //     🔴 **`Tips/Hud/Skulls` 全库只有一条**（**没有** `OpponentSkulls`）—— 双方共用同一颗
            //     里程碑计数 ⇒ ⛔ 别硬造一条对面键。
            // ------------------------------------------------------------------------------------------
            // 卡面四个数值 + 费用那条（`TipText.{Melee,Ranged,Armour,Health,Cost}`）
            { "Tips/MeleeAttackTip",               new Entry("近战攻击力（规则书 :78）", "Melee Attack (rulebook :78)") },   // 键名 = 原版（152 颗触发器）· 值两列自拟
            { "Tips/RangedAttackTip",              new Entry("远程攻击力（规则书 :78）", "Ranged Attack (rulebook :78)") }, // 键名 = 原版（152 颗）· 值两列自拟
            { "Tips/HealthTip",                    new Entry("扣完护甲后扣生命，归零进弃牌堆（:147）", "Health is lost after armour; at zero the card goes to the discard pile (:147)") }, // 键名 = 原版（152 颗）· 值两列自拟
            { "Tips/CostTip",                      new Entry("打出去要花的能量（规则书 :78）", "Energy you must spend to play this card (rulebook :78)") }, // 键名 = 原版（305 颗）· 值两列自拟
            // 🔴 这一条**键名自拟**（原版无护甲 tooltip，见本节开头那条）+ **我们这边零消费点**
            //   （`BattleDriver.TickTooltipAt` 明确不挂它、`Editor/BattleScene.cs` 还有一条
            //   「**护甲上没有 tooltip**（原版那个容器就没有触发器）」的断言）—— 保留它只为
            //   「同一族五条形状一致」+ 谁哪天要挂的时候不会印出键名。⛔ **别拿它当「原版有」的证据**。
            { "Tips/ArmourTip",                    new Entry("受任何来源的伤害都减这么多，最低减到 1（:167）", "Reduces damage from any source by this much, to a minimum of 1 (:167)") }, // 键名 + 值**全自拟**（原版无）
            // HUD 计数那条（`TipText.{Energy,Skulls,QuestPoints,Faith,SpiritStone}`）
            { "Tips/Hud/Skulls",                   new Entry("本局拿到的战功骷髅数", "Skulls earned in this match") }, // 键名 = 原版（13 颗 · `Milestones`）· 值两列自拟
            { "Tips/Hud/PlayerEnergyCount",        new Entry("每回合恢复，用来打出手牌", "Refills every turn; spend it to play cards from your hand") }, // 键名 = 原版（13 颗）· 值两列自拟
            { "Tips/Hud/PlayerQPCount",            new Entry("暗黑天使的任务点进度（0/3）", "Dark Angels quest point progress (0/3)") }, // 键名 = 原版（13 颗）· 值两列自拟
            { "Tips/Hud/PlayerFaithCount",         new Entry("战斗修女的阵营资源", "The Battle Sisters faction resource") }, // 键名 = 原版（13 颗）· 值两列自拟
            { "Tips/Hud/PlayerSpiritStoneCount",   new Entry("灵族的阵营资源；在场也算单位（1 血），点击收集", "The Aeldari faction resource; it also counts as a unit (1 health) on the board - click it to collect") }, // 键名 = 原版（13 颗）· 值两列自拟
            // ---- 🔴 **对面那四条**（`A1086②`，2026-10-09 补；原版键名各 **13** 颗，判据同上）----
            //   ⚠️ **两列值【全自拟】** —— 同上一段：原版那两个触发器 `localize = 1`，显示串在**远端 I2 表**
            //      （本地两张表都 0 命中，本节开头那两句已交代）⇒ 下面写的是**照 `Player*` 那一族的对面措辞**，
            //      ⛔ **别当成「原版这么说」**。`Tips/Hud/Skulls` 没有对面那一条（双方共用，理由见上）。
            { "Tips/Hud/OpponentEnergyCount",      new Entry("对面每回合恢复，用来打出手牌", "Your opponent's; refills every turn and is spent on cards") }, // 键名 = 原版（13 颗）· 值两列自拟
            { "Tips/Hud/OpponentQPCount",          new Entry("对面暗黑天使的任务点进度（0/3）", "Your opponent's Dark Angels quest point progress (0/3)") }, // 键名 = 原版（13 颗）· 值两列自拟
            { "Tips/Hud/OpponentFaithCount",       new Entry("对面战斗修女的阵营资源", "Your opponent's Battle Sisters faction resource") }, // 键名 = 原版（13 颗）· 值两列自拟
            { "Tips/Hud/OpponentSpiritStoneCount", new Entry("对面灵族的阵营资源；在场也算单位（1 血）", "Your opponent's Aeldari faction resource; it also counts as a unit (1 health) on the board") }, // 键名 = 原版（13 颗）· 值两列自拟
            // ---- `Tips/Trait/*`：🔴 **两条都是【我们自己加的】、原版没有**（`Core/Tooltip.cs` 的 `TipText.Trait`）----
            //   原版查不到描述时**就只有「图标 + 标题」、没有正文**（`EverguildTraitTooltipItem`，
            //   见 `资料/tooltip_原版规格与实现.md` §五）；这两句是我们替它补的**如实说明**：
            //     · `NoRulebookEntry`：规则书 61 条（`:161-225`）里没有这个词 ⇒ 明写「没有条目」、**不编解释**；
            //     · `RulebookLine`：`{0}` = 规则书行号（**那是我们自己的判据行，原版没有**）。
            //   🔴 两列**全自拟**（原版没有这两句）。🚨 它们**会上屏**：`Editor/BattleScene.cs` 的两条断言
            //     断的就是这两句（`:11007` 的 `arm.Contains(":167")` 与「没有这个词的条目」那条）。
            { "Tips/Trait/NoRulebookEntry",        new Entry("（规则书里没有这个词的条目）", "(no entry for this term in the rulebook)") }, // ⚠️ 我们加的，不许当成原版
            { "Tips/Trait/RulebookLine",           new Entry("（规则书 :{0}）", "(rulebook :{0})") }, // ⚠️ 我们加的；`{0}` = 规则书行号（我们自己的判据行）
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
            { "MenuDeck/Error/EffectOnlyCard",          new Entry("这张是效果生成的卡（药剂/破坏/秘仪），不能放进卡组",
                                                                "This is an effect-generated card (Elixir / Sabotage / Ritual) and cannot go into a deck") },

            // ============================================================ 🆕 **2026-10-19（第九会话 · `A1133`）：原版【数字族】那三条键**
            //
            // 🔴 **为什么要补**：原版 `DeckUtility.ToRawLocalizationString(err)`（`DeckUtility__ToRawLocalizationString.c`）
            //   拼的是**数字键** —— 格式串 `"MenuDeck/Error/{0}"`（`DAT_1842cfbf0` 地址表读数，见
            //   `资料/普查产出_1011/WB1_A330.md` §2.4 那四行表；我们这边的同名常量 = `DeckRuntime.MenuDeckErrorKeyFmt`），
            //   `{0}` = **原版 `DeckError` 的号**（**本件现读** `d:/2/Warpforge_code/Scripts/Assembly-CSharp/DeckError.cs:1-10`：
            //   `None=0 · CardsNotOwned=1 · MissingHero=2 · InvalidDeckBannedCards=3 · InvalidDeck=4 · IncompleteDeck=5`）。
            //   ⚠️ 而本表原来**只有具名族**（上面那 13 条 `"MenuDeck/Error/" + 枚举名`，是我们 `G9` 仿形状起的名字）
            //   ⇒ `DeckRuntime.MenuDeckErrorKey` 现读走 `Loc.HasEntry(数字键)` **恒假** ⇒ 那扇窗**永远退成兜底键**
            //   `MenuDeck/Error/InvalidDeck` ⇒ **永远只说「卡组不合法」，说不出到底是缺战将 / 别的不合法 / 张数不对**。
            // ⛔ **别拿具名族的文案去「顶替」这两族**：`MenuDeck/Error/TooFewCards`（具名，我们起的名）与
            //   `MenuDeck/Error/5`（原版数字键）**是两条不同的键** —— **键名必须照原版**，两族并存（原版自己也是两族并存）。
            // 🔴 **两列文案都是我们自拟的**（⛔ 不许写成「原版文案」）：原版那套值在**远端 I2 语言表**里，
            //   本地取不到（84 个本地 bundle 无 `localization_assets_all.bundle`，同本文件头那段）——
            //   按本仓口径（先例 = `MenuDeck/Button/Random`）**如实标「自拟」**。
            //   措辞**取我们具名族里同义的那条**（只为让中文档读起来与今天一致，**不是**「原版这么写」）：
            //   2 ↔ `NoWarlord`（原版 `MissingHero`）· 4 ↔ 兜底键 `InvalidDeck`（原版 catch-all）· 5 ↔ `TooFew/TooManyCards`（原版 `IncompleteDeck`）。
            // 🔴 **号→义的映射只有一处** = `DeckRuntime.MenuDeckErrorNumber`（那张表上面写着逐条出处）——
            //   本表只按那三个号落键，⛔ 别在这儿再推一遍。
            // ⚠️ **1（`CardsNotOwned`）与 3（`InvalidDeckBannedCards`）不补**：我们**永远不回**这两个号
            //   （前者是持有数、单机全解锁 ⇒ 恒真；后者是 LiveOps 远端下发的禁卡表、本地没有那个数据源）
            //   ⇒ 补了就是两条**恒不会被问到**的空键。判据同上段那张表。
            // 账 → `项目任务.md` §29·b 的 `A1133`；判据原文 → `资料/普查产出_第八会话/E14_A1012后半中心修.md` §④·C。
            // 消费侧 = `Deck/DeckRuntime.cs` 的 `MenuDeckErrorKey`（选键）+ `Shell/PopUpGameWindow.cs` 的 `Term`（取字）。
            { "MenuDeck/Error/2",                       new Entry("卡组还没有选战将", "This deck has no warlord yet") },              // 原版号 2 = MissingHero · ZH/EN **自拟**
            { "MenuDeck/Error/4",                       new Entry("卡组不合法", "Invalid deck") },                                    // 原版号 4 = InvalidDeck（catch-all）· ZH/EN **自拟**
            { "MenuDeck/Error/5",                       new Entry("卡组张数不对", "This deck has the wrong number of cards") },        // 原版号 5 = IncompleteDeck · ZH/EN **自拟**

            // ============================================================ 🆕 **2026-10-18（第四会话 · 双语③ 波 0）：现读扫出的缺键**
            //
            // 🔴 **这一批不是新功能，是「键早就写在代码里、表里却一直没有」** —— `Loc.T` 对缺键的行为是
            //   **返回键名本身**（见下面 `T()` 的 doc）⇒ 这些键**今天正在把键名印到界面上**
            //   （`Missions/Completed`、`MenuDeck/HUD/DiscardChanges`、`MenuShop/ExtraLegendaryWarning`、
            //     `MainMenu/PurchasePremium/Description` 四处是**已经上屏的真缺陷**）。
            // 出处 = `资料/普查产出_第四会话/施工单_双语③逐处换key.md` §⑤ / §⑥（逐条**现读**复核，非照抄文档）。
            // ⚠️ **本批只加表、一个调用点都没改**（调用点在别的白名单里，由波 1 去换）。
            // 🔴 **施工单 §⑤ 的正文写「真缺口 = 11 条」，而它自己那张表实际列了 16 条** —— 本批按**表**
            //   逐条补齐（那张表才是「键名与 EN 列逐字符」的判据来源），差额已如实记进交件报告。
            // --------------------------------------------------------------------------------------------
            // ---- ① 引擎拒绝码 → 原版提示词条（4 条 · `A985⑧` 的落地）----
            //   载波 = **②（代码里的字面量）**：`RuleCodes.TermKey` 那四条映射的地址表实读见
            //   `RuleEngine/Core/RuleCodes.cs` 的 `Terms`；消费点 = `BattleDriver.HintForCode`
            //   （`BattleDriver.HintForCodeWithKey`：`Loc.HasEntry(键) ? Loc.T(键) : RuleCodes.Describe(rc)`）。
            //   🔴 **EN 列是我们自拟的**（原版值在**远端 I2 表**；本地 84 个 bundle 一个 value 都没有）——
            //      措辞取词条名末段，同 `Battle/Mulligan/Undo` / `MenuDeck/Button/Random` 的先例。
            //   🔴 **中文列 = 改之前 `RuleCodes.Describe` 印出来的那句原话**（`RuleCodes.Names`）⇒ **中文档零变化**。
            //   ✅ **`RuleCodes.cs` 头注与 `BattleDriver.HintForCode` / `HintForCodeWithKey` 那两处**
            //      原来写的「这四条键一条都不在 `Loc` 表里」**已过期、本批已就地订正**（`A1018①`，2026-10-09）。
            //      ⛔ 本表不再写任何 `文件:行号` 引用（行号会漂，一律按符号名认）。
            //   ⚠️ 自检两态是**自适应**的（`Editor/BattleScene` 里那条按 `Loc.HasEntry` 自动选期望值的断言，
            //      §3b ③ 那条只断「不许含 `Battle/Tips/` 前缀、不许为空」）⇒ 本批加键**不会让它们变红**。
            { "Battle/Tips/NotEnoughMana",     new Entry("能量不足",                     "Not enough mana") },   // EN 自拟；ZH = `Describe(ErrCost)` 原话
            { "Battle/Tips/NotYourTurn",       new Entry("不是你的回合",                 "It is not your turn") }, // EN 自拟；ZH = `Describe(ErrNotTurn)` 原话
            { "Battle/Tips/NoTargetAvailable", new Entry("这张战术卡没有可选的合法目标", "No valid target for this tactic") }, // EN 自拟；ZH = `Describe(ErrNoTargetAvailable)` 原话
            { "Battle/Tips/NotEnoughRoom",     new Entry("棋盘上已经放不下了",           "No room left on the board") }, // EN 自拟；ZH = `Describe(ErrNotEnoughRoom)` 原话
            // ---- ② 设置窗 / 战斗内设置面板那一行开关（1 条）----
            //   键名 = 原版 prefab 那颗 `Localize.mTerm`（13 个战场各一颗 + `menus` 一颗，共 **14 颗**；
            //   本批实读 `bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_5032.json`）。
            //   EN 列 = **同一颗 GO 上 TMP 的 `m_text` 原文逐字符**（`MonoBehaviour_3977.json` = `Auto zoom`，
            //   小写 z）—— 与 `Battle/SettingsPanel.AutoZoomLabelEn` **逐字相同**（那边也是照它抄的）。
            //   ZH 列 = 我们那份译表 `数据/本地化/i18n/zh_CN.csv:57`（`Auto Zoom,~,自动缩放`）
            //   ⚠️ 那条的英文串是 `Auto Zoom`（**大写 Z**），与本条（小写 z）**差大小写** ⇒ 属**近邻**，如实记。
            { "Settings/Graphics/AutoZoom",    new Entry("自动缩放",                     "Auto zoom") },
            // ---- ③ 通用弹窗的三颗钮（原版 `PopUpGameWindow` / `WindowsManager.ShowPopUp`；3 条）----
            //   🔴 **`MainMenu/General/OK` 是本批【查证】的重点**（施工单 §⑥·B 的「需裁决」那一条）：
            //     施工单写的是 `MainMenu/General/Ok`（小写 k）—— **原版没有那个拼法**。三条现读判据：
            //       ① `d:/2/tools/il2cpp_out/stringliteral.json` 全表里 `MainMenu/General/*` 共 **15** 条，
            //          其中只有 **`MainMenu/General/OK`**（`0x42BE718`）；
            //       ② 原版**真有**一颗 `Localize.mTerm = "MainMenu/General/OK"`
            //          （`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_2304394373469816941.json`；
            //           另有 14 处 `okKey` 字段也用它）；
            //       ③ 它的**同 GO TMP** 印的就是英文 **`OK`**
            //          （同包 `MonoBehaviour_-4687468603810985875.json` 的 `m_text`）。
            //   🔴 **施工单 §⑥·B 让「确定」落到表里已有的 `MainMenu/General/Confirm`（中文「确认」）那条
            //     —— 那一步【不需要做】**：原版那条键**查到了**（就是 `MainMenu/General/OK`），
            //     按调度台「查到 ⇒ 照它」用它；⛔ **没有另立任何一条键**。
            //     实据：`d:/2/tools/decomp_full/CatalogItemContainer__TryPurchase.c:37,41,43`
            //     （商店那个「传奇重复购买」确认框）传的两颗钮就是 `0x42BE120`（`MainMenu/General/Cancel`）
            //     与 `0x42BE718`（`MainMenu/General/OK`）。
            //   🔴 `OK` / `Cancel` 是**中英两列都拿得到**的少数几条：`Cancel` 那颗同 GO TMP 的 `m_text`
            //     = `Cancel`（`MonoBehaviour_1806065181838074239.json`），`OK` 那颗 = `OK`（见上 ③）。
            //   ⚠️ `MainMenu/General/Discard` **本地没有 TMP 原文**（全库只出现在代码地址表 `0x42BE418`）
            //     ⇒ EN **自拟**（取词条名末段，同 `MenuDeck/Button/Random` 的先例）；ZH 也自拟。
            //   ⚠️ **本表里 `OK` 与 `Confirm` 是两条不同的原版键**（`0x42BE718` / `0x42BE318`），
            //     各有各的挂点（`Confirm` 已由 A891 收进表）—— ⛔ **别合并**（键名照原版 `mTerm`）。
            //
            //   🔴 **2026-10-18 留痕（`A1019`）—— 这一格的 ZH 有【玩家可见】的副作用，别把它当成随手改的**：
            //     ⚠️ **原来写 X**：那 **13 处**弹窗钮在源码里**逐处写死中文字面量「知道了」**
            //        （`Shell/{SettingsWindow,DeckInfoPopup,LeaderboardWindow,LiveOpsEventWindow,
            //          PracticeModePopup,RankedEventWindow}.cs` + `Net/NetRuntime.cs` 那两颗）。
            //     ⚠️ **实际是 Y**：按铁律 11「查到原版键 ⇒ 照它」把它们**全部**改走本条
            //        ⇒ **中文档下玩家看到的钮字由「知道了」变成「确定」**（英文档不变，一直是 `OK`）。
            //     ⚠️ **错因 Z**：那 13 个「知道了」是**我们自写的死串、不是照任何键抄的**；
            //        而原版这颗钮的键是 `MainMenu/General/OK`（见上 ①②③，原版**没有** `…/Ok` 小写 k 那个拼法）
            //        ⇒ 一旦按原版键收口，措辞必然跟着这条键走 —— 中文列是我们自拟的「确定」，
            //        所以**副作用就在这里**（不是接线那一步造成的）。
            //     ⇒ **结论：保留，不改**（`资料/待办判据_第四会话.md` §A1019：判据 = 照原版键；
            //        只要求**留痕**，⛔ 不要求另立键、也不要求改回去）。
            //     📌 **现核（2026-10-18）**：本条消费者 = **14 处**（`Loc.T("MainMenu/General/OK")` **12 处**
            //        + `Loc.T(lkOk)` **2 处**，`Shell/SettingsWindow.cs` 那两颗经常量）；其中 **13 处**原本写「知道了」，
            //        第 14 处 = `Shell/ShopWindow.cs` 的「传奇重复购买」确认框 —— 它本来就带
            //        `MainMenu/General/Cancel`（原版同一处两钮，`CatalogItemContainer__TryPurchase.c:37,41,43`），
            //        **原写就不是「知道了」**（`git log -S'"知道了"' -- Shell/ShopWindow.cs` 零命中）
            //        ⇒ 14 − 1 = **13**，与 §A1019 的「13 处」逐数吻合。
            //     📌 全仓现读**只剩 1 处**写死 `"知道了"`：`Editor/ShellScene.cs` 的 `PromptPopup` **自检样例正文**
            //        （不是消费者、不上屏）—— 别把它当成「还有一处没接」。
            //     ⚠️ 下一条 `MainMenu/General/Confirm`（ZH「确认」）**原封未动**。
            { "MainMenu/General/OK",           new Entry("确定",     "OK") },      // EN = 原版同 GO TMP `m_text` 逐字符；ZH **自拟**（⚠️ 有可见副作用，见上「留痕」块）
            { "MainMenu/General/Cancel",       new Entry("取消",     "Cancel") },  // EN = 原版同 GO TMP `m_text` 逐字符；ZH = `zh_CN.csv:21`（`Cancel,~,取消`，**精确命中**）
            { "MainMenu/General/Discard",      new Entry("丢弃",     "Discard") }, // 🔴 EN **自拟**（本地无 TMP 原文）；ZH **自拟**
            // ---- ④ ~~退出游戏那两句~~ → 🔴 **2026-10-08（波 0b3）已删**（两条自拟键成了死键）----
            //   原委：这两条是波 0 的**自拟键**（原版没有 —— 同族真键只有 `MainMenu/Settings/ButtonLabel/Exit_Game`）。
            //   后来 `A1026` 把三处调用点**全部改指原版真键** `Demo/MainMenu/{ExitGame,ExitButton,CancelButton}`
            //   （见上面 `:10xx` 那一节，波 1b §③ 落地、秒级类型检查 0 错）⇒ 这两条**再没有调用点**。
            //   核法（只读，零调用点）：全仓 `*.cs` 逐字 grep `MainMenu/Settings/ExitGame/{Confirm,Ok}`
            //   ⇒ 除本文件自己那两行定义外 **0 命中**（`Shell/SettingsWindow.cs` 现读用的是 `Demo/MainMenu/*`）。
            //   ⚠️ 它们与 `Demo/MainMenu/ExitGame` / `ExitButton` **值逐字相同** ⇒ 留着就是两条同值空键
            //   （`交件_波1b_P2b_设置与联机.md` §③ 也点名建议清掉）⇒ 删。
            // ---- ⑤ 卡组编辑 / 商店那两条（**今天真上屏的缺陷** · 2 条）----
            //   · `MenuDeck/HUD/DiscardChanges` = 原版 `DeckEditingWindow.ConfirmDiscard` 那扇窗的**正文键**
            //     （`DeckRuntime__ConfirmDiscard.c:54` 的 `DAT_1842d00e8` → 地址表实读 = 本条；
            //      消费点 = `Deck/DeckRuntime` 里那个消费点与 `Shell/PopUpGameWindow.Terms`）。
            //     🔴 **本地既没有 prefab 也没有 TMP 原文**（全库扫 `MenuDeck/HUD/DiscardChanges` **0 命中**）
            //     ⇒ EN / ZH **都自拟**；ZH 照我们那句动作说明（`DeckRuntime` 里 `Say("已丢弃未保存的改动")`）。
            //   · `MenuShop/ExtraLegendaryWarning` = 原版 `CatalogItemContainer.TryPurchase` 那扇确认框的正文键
            //     （`0x42D24E0` → 地址表实读 = 本条；消费点 `Shell/ShopWindow` + `Shell/ShopData` 里那条）。
            //     🔴 EN / ZH **都自拟**（同上 **0 命中**）；ZH 一列 = 改之前在
            //     `Shell/ShopData.LegendaryWarnText` 里写死的那句原话 ⇒ **中文档零变化**。
            { "MenuDeck/HUD/DiscardChanges",    new Entry("丢弃未保存的改动？", "Discard unsaved changes?") },
            { "MenuShop/ExtraLegendaryWarning", new Entry("你已经有 1 张传奇品质的这一件了。\n确定还要再买一张吗？",
                                                          "You already own 1 legendary copy of this item.\nAre you sure you want to buy another one?") },
            // ---- ⑥ 卡组不合法那条兜底键（1 条）----
            //   键名出处 = 原版 `DeckUtility.ToRawLocalizationString` 查不到词条时的兜底（地址表实读，
            //   见 `资料/普查产出_1011/WB1_A330.md` §2.4；消费点 `Deck/DeckRuntime` 里那个消费点）。
            //   🔴 本地**没有**任何 prefab 用这条 mTerm（全库唯一那条形近的是
            //   `MenuDeck/Error/InvalidDeckBannedCards`，**另一条键**，⛔ 别混）⇒ EN / ZH **都自拟**；
            //   形状照 `MenuDeck/Error/*` 那一族（本表上面那 13 条）。
            { "MenuDeck/Error/InvalidDeck",     new Entry("卡组不合法", "Invalid deck") },
            // ---- ⑦ 任务页「进度到顶」那一行（1 条）----
            //   键名 = 原版 `MissionCounterDisplay.completedMessage` 的**出厂字面值**（真包 MB
            //   `3476392019656054992` 实读：`displayCompletedMessage=1` · `progressTextFormat="{0}/{1}"`）。
            //   🔴 **它是一个 I2 词条【键】**（原版在它外面套 `GetTranslation`，`MissionCounterDisplay__Setup.c:74`）
            //   ⇒ 本地**没有**它的文案（全库 20 处引用全是同一个字符串字段，没有一颗 TMP 印它）⇒ EN / ZH **都自拟**。
            //   消费点 = `Shell/DailyData.DailyCounterText` → `Shell/MissionsTab.cs` 那一行
            //   （🔴 **2026-10-18 之后·第六会话就地订正（铁律 5）**：本行原写「消费点 = `DailyData.DailyCounterText:175`
            //     → `MissionsTab` 里那处（**今天那一行印的就是键名** —— 本行加进去就恢复正常）」—— **两句都已过期**：
            //     两处现读都在 `DailyData.DailyCounterText` / `MissionsTab` 那一行（行号会漂 ⇒ ⛔ 别再写）；而「印键名」也不成立了
            //     —— 「键进了表就恢复」那个判断**是错的**（显示点当时根本没查表）⇒ 真缺陷由 **`WCoreDeck`** 修掉：
            //     `DailyData.DailyCounterText` 现为 `return Loc.T(CompletedMessage);`。**错因**：这句写的时候假定
            //     「`Loc.T` 缺键返回键名」那一端是唯一的病，没看显示点走的是 `MenuWindowBase.Text` 的**原样吃串**。）
            { "Missions/Completed",             new Entry("已完成",   "Completed") },
            // ---- ⑧ 高级战役说明那段（1 条）----
            //   键名 = 那颗 `Localize.mTerm`（`bundle_menus_assets_all/MonoBehaviour/
            //   MonoBehaviour_-2264248093995116487.json` 实读）。
            //   🔴 **EN 列 = 同一颗 GO 上 TMP 的 `m_text` 原文逐字符**（同包
            //     `MonoBehaviour_-5372889589424334791.json`；含两处 `\n• ` 与那个 `{0}`）——
            //     与 `Shell/PurchasePremiumWindow.TxtInfoBody` **逐字相同**（那边也是照它抄的）
            //     ⇒ 它既是 prefab 原文、也是我们该印的那句（`{0}` = 每日登录给的战役点数）。
            //   🔴 ZH 列 = **我们自译**（原版中文在远端 I2 表；`zh_CN.csv` 里没有这条英文串）。
            //   消费点 = `Shell/PurchasePremiumWindow` 里那句 `string.Format(TermInfoBody, points)`
            //   （今天那段**印的是键名 + `{0}`** —— 本行加进去就恢复正常）。
            { "MainMenu/PurchasePremium/Description", new Entry(
                "• 解锁本阵营战役里的高级奖励，第一个节点就含一张传奇万能卡。\n" +
                "• 每天登录时，为本阵营提供 {0} 点战役点数；若你有多个高级战役，这份奖励会全部给你！\n" +
                "• 这是一次性购买，永久为本阵营提供收益 —— 因为最后一个战役节点可以反复领取！",
                "• Unlock the Premium rewards in this faction's Campaign, including a Legendary Wildcard in the first node.\n" +
                "• {0} Campaign Points for this faction in the Daily Login Bonus, every day, just for logging in. If you have several Premium Campaigns, you get this bonus for ALL of them!\n" +
                "• This one-time purchase will provide benefits for this faction forever, as the last campaign node can be claimed repeatedly!") },
            // ---- ⑨ 联盟成员操作弹窗那颗会换字的钮（1 条）----
            //   键名 = **代码字面量**实读（`0x184253cb0`，见 `Shell/AllianceMemberOptionsPopup` 的头注）：
            //   原版按 `role == Admin` 在 `SocialMenu/Alliances/{TransferLeadership,Promote}` 之间二选一。
            //   🔴 本地全库 **0 命中**（无 prefab、无 TMP 原文）⇒ EN / ZH **都自拟**。
            //   ⚠️ 施工单 §⑤ 自己写着「**只用作出声与断言，不换字**」⇒ **本键今天是惰性的**（不改变界面）；
            //     加它只是让那条 `const` 有表可查、波 1/2 真要接时不必回头补。
            { "SocialMenu/Alliances/TransferLeadership", new Entry("移交盟主", "Transfer Leadership") },
            // ---- ⑩ 战斗里那扇「玩家档案」面板的四个词（4 条）----
            //   键名 = 原版 `BattleAlliancePanel` 的四颗 `Localize.mTerm`（13 个战场各一份；本批逐颗实读
            //   `bundle_scenes_scenes_battlearena1/MonoBehaviour/{5137,4243,4495}.json`）。
            //   🔴 **EN 列 = 同一颗 GO 上 TMP 的 `m_text` 原文逐字符**（`MonoBehaviour_{4073,4646,4098,4475}.json`）
            //     = `Name:` / `Title:` / `Alliance:` / `This player is is still not part of an Alliance`
            //     —— 最后那条的 **`is is` 双 is 是原版笔误，照抄勿改**（同 `Shell/AlliancePanelWindow` 里那处）。
            //   ZH 列 = **我们自译**（远端 I2 表）。
            //   消费点 = `Shell/AlliancePanelWindow.LocOr`（`:152-159`：`Loc.HasEntry ? Loc.T : prefab 英文`）
            //   ⇒ 本批加键之后，**中文档不再印英文**。
            { "Battle/AlliancePanel/PlayerLabel",      new Entry("姓名：",   "Name:") },
            { "Battle/AlliancePanel/TitleLabel",       new Entry("称号：",   "Title:") },
            { "Battle/AlliancePanel/AllianceLabel",    new Entry("联盟：",   "Alliance:") },
            { "Battle/AlliancePanel/NotInAnAlliance",  new Entry("这名玩家还没有加入任何联盟",
                                                                  "This player is is still not part of an Alliance") },
            // ---- ⑪ 默认 / 演示卡组名（3 条）----
            //   🔴 **口径（调度台 2026-10-18 已裁）**：它们**会被画上屏** ⇒ 算 ①、**建键**；
            //     ⛔ **玩家自己命名的卡组名【绝不进表】**（那是玩家数据，不是文案）。
            //   🔴 原版**没有**这三个名（全库扫 `mTerm` 无 `*DeckName` 一族、也无 `New deck`/`My deck`
            //     这类 TMP 文本；唯一同族的真键是 `MenuDeck/HUD/EditDeckName`）⇒ 键名与两列**都自拟**。
            //   ZH 三列 = 改之前写死在 `Deck/DeckRuntime` 与 `Shell/CollectionData`
            //   的那三个原话 ⇒ **中文档零变化**。
            //   ⚠️ **同一件事只留一条键**：`DeckRuntime` 里那处与 `DeckEditorState` 里那处（在附件
            //     `…_附_BattleDeck.md` #48/#50）与 `CollectionData` 里那处（附件 `…_附_Shell.md` #32 建议
            //     写成 `MenuDeck/HUD/NewDeckName`）**是同一个串** ⇒ 三处**一律**用本表的 `MenuDeck/NewDeckName`
            //     （铁律 6：同义两键 = 迟早不一致）。⚠️ 这一处两份附件打架，已记进交件报告。
            //   ⚠️ `Editor/DeckScene` 有一条断言按**字面量**断「名字被写成「新卡组」」——
            //     波 1/2 把那行换成 `Loc.T` 之后，它只在**中文档**成立（如实记）。
            { "MenuDeck/DefaultDeckName",       new Entry("我的卡组",   "My deck") },
            { "MenuDeck/NewDeckName",           new Entry("新卡组",     "New deck") },
            { "MenuDeck/DemoDeckName",          new Entry("复仇者之刃", "Avenger's Blade") },

            // ============================================================ ⑫ 双语③ 波 0b · 四份交件报告的【并集键】（新增）
            //
            //  🔴 **这一节是谁、为什么**：波 0 只补了「代码里已经在引用」的键，**四批写手真正要用的新键一条都没进表**
            //     （P1 只换得动 11/17、P2 8/22、P4 3/45）⇒ 四批各把键名备在交件报告里，
            //     **由本节一次性合并**（`Core/Loc.cs` 一个时刻只能一个写手 —— 施工单 §⑦）。
            //     逐条依据见 `资料/普查产出_第四会话/交件_波0b_补并集键.md`（那份总键表的「依据」列指回四份报告）。
            //
            //  **查证口径（两张表都搜过 —— P2 那次踩的坑就是只搜了一张）**：
            //    · `d:/2/新解包资源/assets_full/**/MonoBehaviour/*.json` —— 按 `mTerm` 全文搜（哪颗 `Localize` 挂哪条键）
            //    · `d:/2/tools/il2cpp_out/stringliteral.json`（26,507 条 `{value,address}`）—— 原版二进制里的字面量
            //    查到 ⇒ **用原版键名**（注释里给出处）；两张都搜过仍没有 ⇒ **自拟**，并在该条注释里**明写「自拟」**。
            //
            //  ⚠️ **EN 列**：有原版 TMP `m_text` 的**逐字符照抄**；查不到的**明写「EN 自拟」**。
            //  ⚠️ **ZH 列**：三个来源逐条注明 —— ①`数据/本地化/i18n/zh_CN.csv` ②改之前**写死在调用点**的原话（⇒ 中文档零变化）③自拟。
            //  ⚠️ **本节只加键、不改任何调用点**（那是波 1b 的事）⇒ 今天界面**一个字都不变**。

            // ---------------------------------------------------------- ⑫·A P1（卡组编辑）· 14 条
            //   🔴 判据 = `资料/普查产出_第四会话/交件_波1_P1_卡组编辑.md` §②/§⑤（走闸门的那 14 条）。
            //   · **原版键名**（prefab 里那顆 `Localize.mTerm`，本件现读复核过 pid）：
            //     `MenuDeck/HUD/DragCardsTip` · `Card_Rarity/*` · `MenuDeck/HUD/DeckDescription/{Minions,Spells}` ·
            //     `MenuDeck/Filters/{ShowOwnedOnly,ShowUpgradableOnly}`
            //   · **自拟**（两张表都 0 命中）：`MenuDeck/Error/Import{Empty,BadString,NotPersisted}`
            //     ⚠️ **2026-10-18 之后·第四会话删掉了原同族的 `MenuDeck/HUD/DefaultCardback`** —— 它的唯一消费者
            //     （`Deck/DeckRuntime.cs` 那颗自加的卡背名标签）已被 `A1032` 删掉 ⇒ 成了**零调用点的孤儿键**，
            //     按「⛔ 不留孤儿键」清掉。⛔ **别再建回来**（原版那棵子树里一个 TMP 都没有）。
            //   · `MenuDeck/HUD/DragCardsTip` 的 EN **自拟**：同 GO TMP（`MonoBehaviour_8903026703374348068.json`）印的是
            //     **葡语占位串** `Arraste as cartas aqui para criar seu deck` ⇒ ⛔ 照抄会让人以为原版英文是葡语（P1 已点名）。
            //   · `Card_Rarity/*` 的键名只以**字符串**活在 `CardRarityFilter.options[].locKey.mTerm` 里
            //     （`MonoBehaviour_627867653050920740.json`，`useLocalization=1`）—— 原版**没给它们挂 `Localize` 组件**，
            //     这是「两种载体都要查」的又一例。EN = 同一份 `alternativeText`（`Common`…`Special`，与该词条同字）。
            //     ZH = `zh_CN.csv` 抽包那行（`Rarity: Common 60% / …` → 「稀有度: 普通 60% / 稀有 25% / 史诗 10% / 传奇 5%」）。
            //     ⚠️ 同一份表里还有一条**单独的** `Legendary,~,传说`，与本条的「传奇」**打架** ⇒ 取**有稀有度上下文**的那一行
            //     （也与表里既有的 `MenuShop/ExtraLegendaryWarning`「传奇品质」一致）。**如实记，别当没看见。**
            //   · `MenuDeck/Filters/{ShowOwnedOnly,ShowUpgradableOnly}` 的 EN = 同 GO TMP `m_text`
            //     （`MonoBehaviour_-8093783194357141724.json` = `Owned only`；`Upgradable only` 三份同字）；
            //     ZH = `zh_CN.csv`（`Owned only,~,仅显示已拥有`）；可升级那条 `zh_CN.csv` **没有** ⇒ **ZH 自拟**。
            { "MenuDeck/HUD/DragCardsTip",             new Entry("把卡拖到这里", "Drag cards here") },
            { "MenuDeck/Error/ImportEmpty",            new Entry("先粘贴卡组串", "Paste a deck string first") },
            { "MenuDeck/Error/ImportBadString",        new Entry("这不是一条合法的卡组串", "This is not a valid deck string") },
            // ⚠️ `{0}` = 落盘失败的原因；消费点用 `Replace("{0}", …)`（不是 `string.Format` —— 文案里有 `**`，先例 `Battle/HUD/CreatedBy`）。
            // 🔴 这一条**必须与 `Shell/CollectionData` 里那句共用**（施工单 §③ 要求逐字一致）—— 两处措辞原来不同，
            // 本表取 `Deck/DeckRuntime.cs` 那一版（带「导入失败：」前缀 + 「（重启就没了）」尾）；波 1b 把 CollectionData 那处对齐过来。
            { "MenuDeck/Error/ImportNotPersisted",     new Entry("导入失败：卡组串读出来了，但没写进存档——{0}（重启就没了）",
                                                                  "Import failed: the deck string was read, but was not written to the save — {0} (it is gone after a restart)") },
            { "Card_Rarity/Common",                    new Entry("普通", "Common") },
            { "Card_Rarity/Rare",                      new Entry("稀有", "Rare") },
            { "Card_Rarity/Epic",                      new Entry("史诗", "Epic") },
            { "Card_Rarity/Legendary",                 new Entry("传奇", "Legendary") },
            { "Card_Rarity/Special",                   new Entry("特殊", "Special") },
            // ⚠️ EN 是原版的**复数** `Stratagems` ⇒ 英文档会从我们原来的 `Stratagem` 变成它（P1 §⑤·4 已如实记）。
            { "MenuDeck/HUD/DeckDescription/Minions",  new Entry("部队", "Troops") },
            { "MenuDeck/HUD/DeckDescription/Spells",   new Entry("策略", "Stratagems") },
            { "MenuDeck/Filters/ShowOwnedOnly",        new Entry("仅显示已拥有", "Owned only") },
            { "MenuDeck/Filters/ShowUpgradableOnly",   new Entry("仅显示可升级", "Upgradable only") },

            // ---------------------------------------------------------- ⑫·B P2（设置 + 联机）· 46 条
            //   🔴 判据 = `…/交件_波1_P2_设置与联机.md` §④（那一整张「建议键」表）+ §⑦·1。
            //   ⚠️ **这一族原版都没有**（联机页整页是我们自己加的，`Settings/Online/*` 在两张表里 **0 命中**）
            //      ⇒ 键名 / EN **全自拟**；ZH 一律 = 调用点原话（⇒ 中文档零变化）。
            //   ⚠️ `Settings/Online/HowToConnect/*` 那张弹窗正文原来是一整块拼串（23 个字面量）⇒ 本表**按句拆成 12 条**；
            //      运行期值（IPv6 地址 / 网卡名）留成 `{0}`，拼法由波 1b 改（**本表不改调用点**）。
            //   ⚠️ `Demo/MainMenu/{ExitGame,ExitButton,CancelButton}` = **原版真键**（`stringliteral.json` 三条，
            //      `0x4277558 / 0x4277460 / 0x4277360`）—— 波 0 只搜了解包资源、**漏了 binary 表**才写成「原版无此键」。
            //      三条的**角色**有 in-code 判据（`Shell/MainMenuRuntime` 里那段老注释逐条写着：
            //      `ExitGame` = 正文 · `ExitButton` = 右钮(`secondButton`) · `CancelButton` = 左钮(`primaryButton`)）
            //      ⇒ **值沿用**表里那三条（逐字相同），波 1b 把 `MainMenuRuntime` 那 5 行改指过来即可。
            //      ⛔ `Demo/MainMenu/OK` **没建**：没有消费点，且与已在表的 `MainMenu/General/OK` 同值（建了就是空键）。
            { "Settings/General/RedeemCodeUnavailable", new Entry("兑换码要走原版的服务器（`GeneralTab.RedeemCode` → `TryRedeemCode` → 远端校验），这个项目没有那台服务器 ⇒ 这里兑不了。",
                                                                  "Redeem codes go through the original server (`GeneralTab.RedeemCode` → `TryRedeemCode` → remote check); this project has no such server ⇒ redeeming does not work here.") },
            { "Settings/Media/AudioMixerNote",         new Entry("音量走 AudioMixer（与对局内设置面板同一套）",
                                                                  "Volume is handled by the AudioMixer (the same one the in-battle settings panel uses)") },
            { "Settings/Online/TitleNote",             new Entry("这一页不是原版（原版是联网游戏，没有「当主机」这回事）。",
                                                                  "This page is not in the original game (the original is an online title; hosting a match is not a thing there).") },
            { "Settings/Online/TitleNoteBody",         new Entry("IP 直连 —— 公网怎么走 / 路由器要不要放开端口：点这一行看",
                                                                  "Direct IP — getting through the internet / whether the router needs a port opened: tap this line") },
            { "Settings/Online/ProbingPublicAddress",  new Entry("正在探测「外网看到的地址」…（几秒，不影响别的操作）",
                                                                  "Probing the address the internet sees… (a few seconds; nothing else is affected)") },
            { "Settings/Online/PublicAddress/Title",   new Entry("外网看到的地址（刚探的）：", "The address the internet sees (just probed):") },
            { "Settings/Online/PublicAddress/V4",      new Entry("· IPv4：", "· IPv4:") },
            { "Settings/Online/PublicAddress/V6",      new Entry("· IPv6：", "· IPv6:") },
            { "Settings/Online/PublicAddress/LocalV6", new Entry("本机网卡上的公网 IPv6：", "Public IPv6 on this machine's adapter:") },
            { "Settings/Online/PublicAddress/NotFound", new Entry("（没探到）", "(not found)") },
            { "Settings/Online/PublicAddress/None",    new Entry("没有", "none") },
            { "Settings/Online/PublicAddress/Mismatch", new Entry("⚠️ 两个不一样 ⇒ 上面那个 IPv6 是【路由器的】（它在做 IPv6 NAT）：\n　 外面看得到它，但别人连不到你这台机器 ⇒ IPv6 直连这条路走不了。",
                                                                  "⚠️ The two differ ⇒ that IPv6 above is the [router's] (it is doing IPv6 NAT):\n  the outside world can see it, but nobody can reach this machine ⇒ direct IPv6 is not an option.") },
            { "Settings/Online/PublicAddress/BothOk",  new Entry("✅ 两边都有公网 IPv6 ⇒ 「IPv6 直连」这条路可行（要求对面也有）。",
                                                                  "✅ Both sides have a public IPv6 ⇒ direct IPv6 is viable (the other side needs one too).") },
            { "Settings/Online/IpPlaceholder",         new Entry("例如 192.168.1.10", "e.g. 192.168.1.10") },
            { "Settings/Online/PasswordPlaceholder",   new Entry("留空 = 不校验", "empty = no password check") },
            { "Settings/Online/HostReady",             new Entry("✅ 主机已就绪，等着对面连进来。", "✅ Host is ready, waiting for the other side to connect.") },
            { "Settings/Online/HostReadyToFriend",     new Entry("把这行给朋友 → ", "Give this line to your friend → ") },
            { "Settings/Online/HostReadyNoPassword",   new Entry("（没设密码）", "(no password set)") },
            { "Settings/Online/HostFailed",            new Entry("主机没起来：", "The host did not start: ") },
            { "Settings/Online/NetRuntimeMissing",     new Entry("NetRuntime 不在（自检里要自己建）", "NetRuntime is absent (self-checks have to create one)") },
            { "Settings/Online/NoNicFound",            new Entry("⚠️ 一块可用网卡都没找到 —— 只能手填地址",
                                                                  "⚠️ No usable network adapter was found — the address has to be typed in by hand") },
            { "Settings/Online/LocalAddr",             new Entry("本机地址 {0}/{1}：{2}", "Local address {0}/{1}: {2}") },
            { "Settings/Online/IsV6",                  new Entry("（IPv6）", "(IPv6)") },
            { "Settings/Online/ClickAgain",            new Entry("　—— 再点一下换下一个", " —— tap again for the next one") },
            { "Settings/Online/VirtualNic",            new Entry("\n⚠️ 这是「虚拟网卡」的地址（VPN / 虚拟局域网工具建的那张）。\n　 对面也装了同一个工具的话，直接用这个 —— 穿透由那个工具负责。",
                                                                  "\n⚠️ This is a \"virtual adapter\" address (the one a VPN / virtual-LAN tool created).\n  If the other side runs the same tool, just use this — that tool handles getting through.") },
            { "Settings/Online/StatusNoSession",       new Entry("（会话还没建 —— 点一下主机的保存，或客机的检查连接）",
                                                                  "(no session yet — press Save under Host, or Check Connection under Client)") },
            { "Settings/Online/HowToConnect/Intro",    new Entry("三条路，从最省事开始：", "Three routes, easiest first:") },
            { "Settings/Online/HowToConnect/Lan",      new Entry("① 同一个局域网 ⇒ 直接填主机那台机器的地址。",
                                                                  "① Same LAN ⇒ just fill in the host machine's address.") },
            { "Settings/Online/HowToConnect/VirtualLan", new Entry("② 不在一起 ⇒ 两边装同一个虚拟局域网工具\n　（Tailscale / ZeroTier / 蒲公英 之类），填它给的地址。",
                                                                  "② Not in the same place ⇒ install the same virtual-LAN tool on both sides\n  (Tailscale / ZeroTier / Pgyvpn and the like), then fill in the address it gives you.") },
            { "Settings/Online/HowToConnect/PublicDirect", new Entry("③ 公网直连 ⇒ 主机点【保存】时会自动向路由器要一个端口（UPnP）；\n　成没成会弹一条告诉你 —— 没成就是路由器不支持 / 关着 UPnP，\n　那就在路由器管理页手动把那个端口转发到主机这台机器。\n　（主机自己有公网 IPv6 的话填 IPv6 更省事，连映射都不用。）",
                                                                  "③ Direct internet ⇒ when the host presses [Save] it asks the router for a port automatically (UPnP);\n  you get a pop-up either way — no port means the router does not support UPnP or has it off,\n  so forward that port to the host machine by hand in the router's admin page.\n  (If the host itself has a public IPv6, filling in the IPv6 is easier — no forwarding needed.)") },
            { "Settings/Online/HowToConnect/DontUseTestSite", new Entry("⚠️ 别拿「IPv6 测试网站」当判据：那里显示的是【外网看到的地址】，\n　它有可能是路由器的（有些路由器在做 IPv6 NAT）⇒ 外面看得到，\n　但别人连不到你这台机器。本机到底能不能被连上，看下面「本机检测」，\n　或者点【测外网】把两者摆在一起对照。",
                                                                  "⚠️ Do not use an \"IPv6 test site\" as the criterion: it shows the address the [outside world sees],\n  which may be the router's (some routers do IPv6 NAT) ⇒ visible from outside,\n  yet nobody can reach this machine. Whether this machine is reachable is what \"On this machine\" below is for,\n  or press [Test Public IP] to put the two side by side.") },
            { "Settings/Online/HowToConnect/NoHolePunching", new Entry("我们不做打洞（那要一台公网上的会合点 + 服务器，本项目没有）。",
                                                                  "We do not do hole punching (that needs a rendezvous point on the public internet plus a server, which this project does not have).") },
            { "Settings/Online/HowToConnect/LocalCheckTitle", new Entry("本机检测：", "On this machine:") },
            { "Settings/Online/HowToConnect/PublicV6Yes", new Entry("· 公网 IPv6：有（{0}）\n  第 ③ 条路能用 —— 只要路由器放行那个 TCP 端口",
                                                                  "· Public IPv6: yes ({0})\n  route ③ works — as long as the router lets that TCP port through") },
            { "Settings/Online/HowToConnect/PublicV6No", new Entry("· 公网 IPv6：没有 ⇒ 本机网卡上没有全局 IPv6\n  （⚠️ 这与「测试网站看得到 IPv6」不矛盾 —— 那个多半是路由器的）\n  ⇒ 第 ③ 条只能靠端口映射，或者走 ① ②",
                                                                  "· Public IPv6: none ⇒ this machine has no global IPv6 on its adapters\n  (⚠️ this does not contradict \"a test site shows an IPv6\" — that one is most likely the router's)\n  ⇒ route ③ only via port forwarding, or take ① ②") },
            { "Settings/Online/HowToConnect/VirtualNicYes", new Entry("\n· 虚拟局域网工具：装了（网卡「{0}」）\n  点【刷新】能切到它给的地址",
                                                                  "\n· Virtual-LAN tool: installed (adapter \"{0}\")\n  press [Refresh] to switch to the address it gives") },
            { "Settings/Online/HowToConnect/VirtualNicNo", new Entry("\n· 虚拟局域网工具：没检测到（想走 ② 就两边各装一个，Tailscale / ZeroTier 都免费）",
                                                                  "\n· Virtual-LAN tool: not detected (for route ② install one on each side; Tailscale / ZeroTier are both free)") },
            { "Settings/Online/HowToConnect/UpnpNote", new Entry("\n· 路由器自动开端口（UPnP）：主机点【保存】时自动试 —— 成没成都会弹一条说出来",
                                                                  "\n· Router opens the port automatically (UPnP): tried when the host presses [Save] — a pop-up tells you either way") },
            // ---------------------------------------------------------- ⑫·B⁺ P2c（联机页那 8 颗**写死英文**的钮）· 8 条
            //   🔴 判据 = `资料/普查产出_第四会话/交件_波1b_P2b_设置与联机.md` §④（8 颗逐颗列了文件:行号）
            //     + **本件现读** `Shell/SettingsWindow.cs`（`:2430/:2431/:2470/:2531/:2533/:2558/:2562/:2580`）。
            //   🔴 **原版查证（两张表都搜过 ⇒ 这 8 条是【我们起的】键名）**：
            //     · 表① `assets_full/**/MonoBehaviour/*.json` 按 `"mTerm": "<字面量>"` 全树扫
            //       `Host` · `Client` · `Test Public IP` · `IP address` · `Password` · `Refresh` · `Save`
            //       · `Check Connection` ⇒ **0 命中**；再按前缀扫 `Settings/(Online|Audio|Network)*`
            //       ⇒ **0 命中**（`bundle_menus_assets_all` 全包 308 条 `mTerm` 里也没有这一族）。
            //     · 表② `d:/2/tools/il2cpp_out/stringliteral.json`（26,507 条）逐词扫同一批
            //       ⇒ **0 命中**（正对照：`Demo/MainMenu` 7 条 · `MainMenu/Settings` 4 条 · `Battle/HUD` 8 条
            //       · `MenuDeck/` 33 条 —— 扫描有效，不是无效否定）。
            //     ⇒ 联机页整页**原版没有**（同 `Settings/Online/Title` 那条的先例）⇒ 键名照该族形状**自拟**，
            //       EN = 调用点今天写死的那 8 个串（**逐字**，⇒ 英文档零变化），ZH = **我们译的**。
            //   ⚠️ ZH 取词与表内既有文案**对齐**（不是另立一套；逐条现读本表核对）：
            //     `主机` = `…/HostReady`「✅ 主机已就绪…」 · `客机` = `…/St/ClientLobby`「连上主机了 —— …」
            //     · `保存` = `…/St/ConnRefused`「…主机那边要先点「保存」…」 · `刷新` = `…/HowToConnect/VirtualNicYes`
            //     「点【刷新】能切到它给的地址」 · `检查连接` = `…/StatusNoSession`「…或 Client 的检查连接」。
            //   ✅ **2026-10-18 之后 · 第五会话（波 1b 接线之后 · `A1063`）：那两条【已改】** ——
            //     `…/StatusNoSession` 的 ZH「点一下 Host 的保存，或 Client 的检查连接」
            //       → 「点一下**主机**的保存，或**客机**的检查连接」；
            //     `…/HowToConnect/DontUseTestSite` 的 ZH「点【Test Public IP】」→「点【**测外网**】」。
            //     ⚠️ **EN 列【不动】**：`Host` / `Client` / `[Test Public IP]` 与英文档那 8 颗钮的字**逐字相同**
            //     ⇒ 英文档零变化。⚠️ 改这两条的**前置条件**就是「8 颗钮已经接线」（否则引用会先失配）。
            { "Settings/Online/RoleHost",              new Entry("主机", "Host") },                   // `SettingsWindow`
            { "Settings/Online/RoleClient",            new Entry("客机", "Client") },                 // `SettingsWindow`
            { "Settings/Online/TestPublicIp",          new Entry("测外网", "Test Public IP") },       // `:2470`
            { "Settings/Online/IpLabel",               new Entry("IP 地址", "IP address") },          // `:2531`
            { "Settings/Online/PasswordLabel",         new Entry("密码", "Password") },               // `:2533`
            { "Settings/Online/Refresh",               new Entry("刷新", "Refresh") },                // `:2558`
            { "Settings/Online/Save",                  new Entry("保存", "Save") },                   // `:2562`
            { "Settings/Online/CheckConnection",       new Entry("检查连接", "Check Connection") },   // `:2580`
            { "MainMenu/RankedWindow/LeaderboardOfflineNote", new Entry("上一赛季的榜单在服务器上。\n本地版没有赛季数据，所以这里只能看看界面。",
                                                                  "Last season's leaderboard lives on the server.\nThe local build has no season data, so all you can do here is look at the UI.") },
            // 🔴 **2026-10-08（波 0b3）订正（铁律 5）**：原来**中英两列**都带着「（将来做 P2P）」/「(P2P later)」
            //    —— **已过期**（P2P 2026-09-26 就做完了）⇒ 那半句**删掉**（两列一起，只改值、⛔ 没改调用点）。
            //    追溯：旧注解写着「本表照原话抄、建议波 1b 顺手订正」；波 1b 只接了线、**值在 `Loc.cs`、它无权改**
            //    ⇒ 由本件收口。
            { "MainMenu/Ranked/OfflineNote",           new Entry("排位赛需要服务器连接。\n本地版没有联机，所以这里只能看看界面。",
                                                                  "Ranked play needs a server connection.\nThe local build has no online play, so all you can do here is look at the UI.") },
            // `{0}` = 玩家名 ⇒ 消费点要用 `string.Format(Loc.T(…), who)`。
            { "MainMenu/Social/ProfileTitle",          new Entry("「{0}」的档案", "{0}'s profile") },
            { "MainMenu/Social/ProfileOfflineNote",    new Entry("服务器数据 —— 本地版只有你自己那一份（原版这一页由服务器填）",
                                                                  "Server data — the local build only has your own (the original fills this page from the server)") },
            // ⚠️ 这条是**默认玩家名**（`Shell/ProfileData.DefaultPlayerName`）—— P2 §④ 自己标了「也可判 ③ 玩家数据不翻、需裁决」。
            //   本表**按「默认值也是文案」建**（英文档下不该给玩家一个中文名）；若裁决「数据不翻」⇒ 删这一条即可（零连带）。
            { "MainMenu/Profile/DefaultPlayerName",    new Entry("玩家123", "Player123") },
            { "Demo/MainMenu/ExitGame",                new Entry("确定要退出游戏吗？", "Are you sure you want to exit the game?") },
            { "Demo/MainMenu/ExitButton",              new Entry("退出游戏", "Exit game") },
            { "Demo/MainMenu/CancelButton",            new Entry("取消", "Cancel") },

            // ---------------------------------------------------------- ⑫·C P3（社交 + 弹窗 + 商店）· 16 条
            //   🔴 判据 = `…/交件_波1_P3_社交与商店.md` §③·A（9 条）+ §③·B（1 条需裁决）+ §⑤·D（1 条）+ §⑥·4（1 条）+ 交件表。
            //   · 卡组串那三句**与 ⑫·A 的 `MenuDeck/Error/Import*` 同一族**（施工单 §③：两处逐字一致）⇒ **不在这里重复建**。
            //   · `MenuDeck/Error/CantStartNoWarlord` = **整句**（调度台当场拍板，交件 §④）：
            //     `MenuDeck/Error/NoWarlord`（中文「还没有选战将」）那条**短键留着**给 4 处「未选战将」用；
            //     弹窗正文原来那句「这套卡组还没有选战将，开不了局。」**不许缩水** ⇒ 另立本条（两处：`LiveOpsEventWindow` / `PracticeModePopup`）。
            //   · `MenuDeck/Error/WrongGameMode` / `…Deck` = **两句不同的整句**（P3 只报了一个键名，但代码里那两处是两句话）
            //     ⇒ 拆成两条，键名照 P3 那条 + `Deck` 后缀。
            //   · `MenuDeck/GameMode/{Skirmish,Classic}` 与 `{Skirmish,Classic}Tag` = 同样是**两种形状**
            //     （`:243` 的「遭遇战（Skirmish · 12 张）」vs `:280/:292` 内嵌的「遭遇 · 12 张」）⇒ 各两条。
            //   · `MenuDeck/CantImportDeck` 的**键名是原版**：`stringliteral.json` `0x42CEBF0` ——
            //     正是 P3 §⑥·4 写的那个 `DAT_1842cebf0`（VA − ImageBase 0x180000000 = RVA 0x42CEBF0）⇒ **查到就用它**。
            { "Settings/Online/MatchCancelled",        new Entry("已经取消这一局的联机匹配 —— 对面会收到通知，双方都没有开局。\n想再打一次：两边各自重新点一次 `Battle!`。",
                                                                  "The online match for this battle was cancelled — the other side gets a notice and neither side has started.\nTo try again: both sides press `Battle!` once more.") },
            { "Settings/Online/MatchCancelFailed",     new Entry("取消不了这一局：{0}", "Could not cancel this battle: {0}") },
            { "MenuDeck/GameMode/Skirmish",            new Entry("遭遇战（Skirmish · 12 张）", "Skirmish (12 cards)") },
            { "MenuDeck/GameMode/Classic",             new Entry("经典（Classic · 30 张）", "Classic (30 cards)") },
            { "MenuDeck/GameMode/SkirmishTag",         new Entry("遭遇 · 12 张", "Skirmish · 12 cards") },
            { "MenuDeck/GameMode/ClassicTag",          new Entry("经典 · 30 张", "Classic · 30 cards") },
            { "MenuDeck/Error/WrongGameMode",          new Entry("这副预组是「{0}」的，不能用在{1}里 —— 换一副。",
                                                                  "This prebuilt deck is \"{0}\", so it cannot be used in {1} — pick another one.") },
            { "MenuDeck/Error/WrongGameModeDeck",      new Entry("「{0}」是「{1}」的卡组，不能用在{2}里 —— 换一副，或点 `Create deck` 建一副新的（照原版：模式在建组那一刻定，之后改不了）。",
                                                                  "\"{0}\" is a \"{1}\" deck, so it cannot be used in {2} — pick another one, or press `Create deck` to build a new one (as in the original: the mode is fixed the moment the deck is built, it cannot be changed later).") },
            { "MenuDeck/Error/NoDeckForMode",          new Entry("还没有可用的卡组 —— 先点 `Create deck` 建一副{0}的。",
                                                                  "No usable deck yet — press `Create deck` to build a {0} one first.") },
            { "MenuDeck/Error/HiddenCards",            new Entry("这套卡组里有隐藏卡，开不了练习赛。",
                                                                  "This deck contains hidden cards, so a practice match cannot start.") },
            // ---- 🔴 2026-10-08（波 0b3）：~~`MenuDeck/Share/{ChatUnavailable,PlatformShare}`~~ **两条已删** ----
            //   判据 = `资料/普查产出_第四会话/交件_分享卡组收口.md` §⑦·1：`A1040` 把「分享」收口成
            //   **真写系统剪贴板**（`Deck/DeckRuntime.CopyDeckToClipboard`）之后，那两条**再没有调用点**；
            //   而且 `PlatformShare` 的**中英兜底都是错的**（写着「原版是**平台分享**」，而判据
            //   `d:/2/tools/decomp_full/DeckInfoPopup__ShareDeck.c` 读出来是 `GUIUtility.systemCopyBuffer` = **写剪贴板**）。
            //   核法（只读，零调用点）：全仓 `*.cs` 逐字 grep 两个键名 ⇒ 除本文件自己那两行定义外 **0 命中**
            //   ⇒ 删（铁律 6：死键留着只会误导下一个会话）。
            // ⚠️ **2026-10-08（波 0b3）**：EN 列原来逐字抄了 ZH 里的路径 `python 工具/gen_prebuilt_decks.py`
            //   —— 那个 `工具`（U+5DE5/5177）会让「英文列不许含 CJK」那条断言（`Loc.HasCjk`，区间含 `0x4E00`）
            //   直接红。⛔ 路径没删掉，只是**改成英文描述**（脚本名照旧、目录名写成 `the project's tools folder`）。
            //   ZH 列**不动**（它本来就该是中文）。
            { "MenuDeck/Error/PrebuiltMissing",        new Entry("预组卡组的数据读不到（Resources/prebuilt_decks.json）⇒ 先如实留空；跑 `python 工具/gen_prebuilt_decks.py` 重新生成",
                                                                  "Prebuilt-deck data cannot be read (Resources/prebuilt_decks.json) ⇒ left honestly empty for now; run the prebuilt-deck generator (`python gen_prebuilt_decks.py`, in the project's tools folder) to regenerate it") },
            { "MenuDeck/Error/NoUsablePrebuilt",       new Entry("这一页一副可用的都没有（拼不齐的按原版口径整副不显示）",
                                                                  "Not a single usable deck on this page (as in the original, decks that cannot be completed are not shown at all)") },
            // 调度台当场拍板补的一条（P3 §③·B 标「需裁决」未自拟）：`LiveOpsEventWindow` 里那顆 `No Deck Text`。
            { "MenuDeck/HUD/NoWarlordText",            new Entry("这套卡组还没有战将 —— 去卡组编辑里选一个再来。",
                                                                  "This deck has no Warlord yet — go pick one in the deck editor and come back.") },
            // 调度台当场拍板补的一条（P3 §⑤·D 首条：两个附件源都漏了）—— `PracticeModePopup` 的 `SelectedArmyName()` 兜底，**会画上屏**。
            { "MenuDeck/HUD/NoArmySelected",           new Entry("（未选阵营）", "(no faction selected)") },
            { "MenuDeck/CantImportDeck",               new Entry("这副卡组导不进来。", "This deck can't be imported.") },
            { "MenuDeck/Error/CantStartNoWarlord",     new Entry("这套卡组还没有选战将，开不了局。",
                                                                  "This deck has no Warlord chosen yet, so the battle cannot start.") },

            // ---------------------------------------------------------- ⑫·D P4（战斗 HUD / 各窗口）· 33 条
            //   🔴 判据 = `…/交件_波1_P4_战斗HUD与窗口.md` §⑤（那张 ready-to-paste 的键表）。
            //   ⛔ **两族【故意没建】**（不是漏，逐条写清理由）：
            //     · **战斗日志那 11 句模板**（`BattleDriver` 里那一族）—— 同文件那段（`G5`）的 in-code 判据写着
            //       「它们没有原版 `mTerm`，接的时候要和这次重构一起决定键名，**⛔ 别先自造一批键**」；P4 也「建议【先不加】」
            //       ⇒ 等重构那一轮（**要做，只是先后** —— 铁律 11）。
            //     · **`Battle/CardWindow/TapToClose`** —— in-code 判据写着「原版子树没这个节点、`Battle/` 93 条字面量里也没有键
            //       ⇒ 铁律 11 例外① ⇒ 保留中文不改」（`Battle/CardDisplayWindow` 里那处；附件把它列成 ① 是错的）。
            //   ⚠️ `Battle/{Hint,Log,Chat,MultiCard,ChooseCard,CardWindow}/` 整族在 `stringliteral.json` 里**不存在**
            //     （P4 逐前缀核过：`Battle/` 93 条 · `MainMenu/` 86 · `Settings/` 10 · `Tips/` 8 · `MenuShop/` 13）⇒ **这一族全自拟**。
            //   ⚠️ `Battle/Chat/*` 那 6 条：原版标签来自**远端 I2**、序列化兜底 6 个**全是 `Greetings`** ⇒ 本地没有任何一处能印真标签
            //     （`Battle/ChatPopupPanel` 里那 6 个钮那段头注）；本表的 EN = 调用点现用的那 6 个英文标签，ZH 是**自拟**。
            //   ⚠️ `Battle/BattleEnd/Rounds` 带**三个尾空格**（照 `EndPanel` 里那句 `$"{rounds} 回合   "` 的拼法）；
            //     `RoundsOnly` / `RoundsMinFoeHealth` 是另外两种整句（同文件那三行）—— ⛔ 三条别合并（值不同）。
            { "Battle/Hint/MulliganDone",              new Entry("换牌完成，开打", "Mulligan done — battle on") },
            { "Battle/Hint/Reconnected",               new Entry("已重连并追平", "Reconnected and caught up") },
            { "Battle/Hint/DeckLoadFailed",            new Entry("卡组存档读不出来（{0}）—— 本局自动凑了一副",
                                                                  "The deck save could not be read ({0}) — a deck was put together automatically for this battle") },
            { "Battle/Hint/MulliganPick",              new Entry("换牌中：点牌上的「换」标记要替换的牌，然后点「完成换牌」",
                                                                  "Mulligan: tap the swap mark on the cards to replace, then tap Done") },
            { "Battle/Hint/MulliganSent",              new Entry("换牌已提交，等主机定序…", "Mulligan submitted, waiting for the host to order it…") },
            { "Battle/Hint/ReplacedCount",             new Entry("换掉了 {0} 张", "Replaced {0} card(s)") },
            // 附件只列了 `:4086`/`:4107`，漏了紧随其后那两句 `SetHint`（P4 §⑥·1）—— 两句各一条键。
            { "Battle/Hint/OffensivePhase",            new Entry("选择进攻卡（先手）—— 选完点「继续」",
                                                                  "Pick an offensive card (you go first) — tap Continue when done") },
            { "Battle/Hint/DefensivePhase",            new Entry("选择防御卡（后手）—— 选完点「继续」",
                                                                  "Pick a defensive card (you go second) — tap Continue when done") },
            { "Battle/Hint/ChooseCard",                new Entry("选一张牌，然后点「继续」", "Pick a card, then tap Continue") },
            { "Battle/Hint/ChooseOption",              new Entry("选一项，然后点「继续」", "Pick an option, then tap Continue") },
            { "Battle/Hint/NoRestartOnline",           new Entry("联机局不能自己重开 —— 对面还在这一局里",
                                                                  "An online battle cannot be restarted on your own — the other side is still in it") },
            { "Battle/ChooseCard/Offensive",           new Entry("选择进攻卡", "Choose an offensive card") },
            { "Battle/ChooseCard/Defensive",           new Entry("选择防御卡", "Choose a defensive card") },
            { "Battle/MultiCard/Title",                new Entry("你的牌库", "Your deck") },
            { "Battle/MultiCard/TapToClose",           new Entry("点「继续」或再点一下牌堆关闭", "Tap Continue or tap the deck again to close") },
            { "Battle/Log/SideMe",                     new Entry("我方", "Our side") },
            { "Battle/Log/SideFoe",                    new Entry("敌方", "Enemy") },
            // 🔴 **2026-10-08（波 0b3）**：EN 列原来那个**全角空格**（U+3000）会被 `Loc.HasCjk`（区间 `0x3000-0x303F`，
            //   见 `Loc.cs` 本文件 `HasCjk`）判成「含汉字」 ⇒ 换成**半角**（1:1，与 `Settings/Online/*` 那 6 条同法）。
            //   ⚠️ **行为影响 = 零**（本件现读判据）：全仓 `*.cs` grep `Battle/Log/TurnPrefix` ⇒ **0 个调用点**
            //   （只有本行的定义）⇒ 今天没有任何地方取它。ZH 列那个全角空格**不动**（中文档本来就该有）。
            //   ⚠️ `Battle/Log/*` 另外 12 句按 `Battle/BattleDriver` 里那段 in-code 判据**仍未建**
            //   （等日志重构那一轮）——那一条与本行无关，本行只是把已存在的键修干净。
            { "Battle/Log/TurnPrefix",                 new Entry("回合 {0}　{1}", "Round {0} {1}") },
            { "Battle/Log/EffectFallback",             new Entry("效果", "effect") },
            { "Battle/BattleEnd/ForfeitMe",            new Entry("我方投降", "we forfeited") },
            { "Battle/BattleEnd/ForfeitFoe",           new Entry("对方投降", "the opponent forfeited") },
            { "Battle/BattleEnd/Rounds",               new Entry("{0} 回合   ", "{0} rounds   ") },
            { "Battle/BattleEnd/RoundsMinFoeHealth",   new Entry("{0} 回合   敌方战将最低生命 {1}",
                                                                  "{0} rounds   enemy Warlord's lowest health {1}") },
            { "Battle/BattleEnd/RoundsOnly",           new Entry("{0} 回合", "{0} rounds") },
            { "Battle/Chat/Greet",                     new Entry("问候", "Greet") },
            { "Battle/Chat/Threat",                    new Entry("威胁", "Threat") },
            { "Battle/Chat/WellPlayed",                new Entry("打得好", "Well Played") },
            { "Battle/Chat/Taunt",                     new Entry("嘲讽", "Taunt") },
            { "Battle/Chat/Sorry",                     new Entry("抱歉", "Sorry") },
            { "Battle/Chat/Oops",                      new Entry("哎呀", "Oops") },
            { "Battle/Settings/Title",                 new Entry("设置", "Settings") },
            { "Battle/Settings/AiDifficulty",          new Entry("AI 难度", "AI Difficulty") },
            { "MainMenu/Campaign/Points",              new Entry("战役点数：{0}", "Points: {0}") },
            // ============================================================ ⑬ 双语③ 波 0b2 · P6（`Net/` 整片）· 76 条
            //
            //  判据 = `资料/普查产出_第四会话/施工单_双语_Net整片_P6.md` §③ 那张缺键表（逐条照建）。
            //  范围 = `Net/` 那 9 份 `.cs` 里**玩家看得见**的字（弹窗 / 提示行 / 设置窗「联机」页那一行）；
            //    ⛔ 走线文案（`Wire/*` 7 条 · 穿 TCP 到**对端屏幕**，`bye`/`ack`/`reject` 的 `reason`）**本轮不建** ——
            //       接 `Loc.T` 等于把发送方的语言灌到接收方界面上 ⇒ 单开 `P6d`（发键标识、接收侧取词）；
            //    ⛔ `Battle/Log/*` 12 句**也不建**（`Battle/BattleDriver` 里那段 in-code 判据：
            //       「原版记动作、我们记后果，接的时候和日志重构一起定键名 —— ⛔ 别先自造一批键」；要做、只是先后）。
            //
            //  🔴 查证口径（**两张表都搜过** —— 出处 = P6 §⑥·3）：
            //    · `d:/2/新解包资源/assets_full/**/MonoBehaviour/*.json` 的 `mTerm` 全库扫
            //    · `d:/2/tools/il2cpp_out/stringliteral.json`（26,507 条字面量）
            //    ⇒ `Settings/Online/*` **0 命中**（原版联机走 PlayFab，根本没有这套流程）
            //      ⇒ **键名 + 英文列全自拟**；**中文列 = 调用点原话逐字**（⇒ 切成这些键之后**中文档零变化**）。
            //    ⚠️ 键名族照先例 `Settings/Online/Title`（本文件 `:187`）。
            //
            //  ⚠️ `\n` = 真换行；`{0}`/`{1}`/`{2}` = 调用点的运行期值（消费方用 `Replace` 或 `string.Format`）。
            //  ⚠️ 中文列里的 `**` 是**调用点字面量里就有的强调符**（`Net/*.cs` 源码逐字如此），
            //     **照抄保留** —— 去掉它就等于悄悄改了中文档（本表既有 14 条同款，例如 `MenuDeck/Error/EffectOnlyCard`）。
            //
            //  ---------------------------------------------------------- ⑬·A `Settings/Online/St/*` · 45 条
            //  出处：`NetSession.cs` `:102/:573` `:133` `:143` `:177` `:181/:354` `:210/:294` `:211/:295` `:256`
            //    `:289` `:311/:315/:319` `:361/:363/:367/:380` `:387/:391` `:405/:478/:420/:424/:454/:464` `:435/:443` `:550`
            //    · `NetTransport.cs` `:420/:422/:424/:425` `:210/:226/:282` `:307/:308/:311/:315/:316/:319/:372/:393` `:176/:182`
            //  ⚠️ `St/Handshaking` **一条键两处**（`NetSession` 里那两处）：两处的原文不同（后一处少了「连上了，」）
            //     ⇒ 本表取前一版（与 P6 §③ 的英文列同形）；**后一处那句会跟着变**（P6a 落键时留意）。
            //  ⚠️ `St/PeerClosed`/`St/BadFrame`/`St/BadEnvelope`/`St/NotConnected`/`St/SendFailed`/`St/ReadAbort`
            //     是**间接**上屏的（经 `NetTransport._lastError` → `NetSession.LastError` → 设置窗 `_flash`），照样是 ①类。
            //  ⚠️ 与 P2 已在表的 `Settings/Online/HostFailed`（「主机没起来：」）**形状不同**（那条不带 `{0}`）⇒ 两条并存，不合并。
            { "Settings/Online/St/Off",                     new Entry("未连接", "Not connected") },
            { "Settings/Online/St/HostFailed",              new Entry("主机没起来：{0}", "Host failed: {0}") },
            { "Settings/Online/St/Listening",               new Entry("主机已就绪，在 {0} 端口等客机（把本机 IP 告诉对方）",
                                                            "Host ready — waiting for a client on port {0} (give your IP to the other player)") },
            { "Settings/Online/St/Connecting",              new Entry("正在连 {0}:{1} …", "Connecting to {0}:{1} …") },
            { "Settings/Online/St/Handshaking",             new Entry("连上了，正在核对协议版本与密码…",
                                                            "Connected — checking protocol version and password…") },
            { "Settings/Online/St/PeerLostInBattle",        new Entry("对手掉线了，正在等他回来…（对局已暂停）",
                                                            "Opponent disconnected — waiting for them (the battle is paused)") },
            { "Settings/Online/St/Disconnected",            new Entry("连接断了：{0}", "Connection lost: {0}") },
            { "Settings/Online/St/PeerBack",                new Entry("有连接进来，正在核对是不是刚才那个人…",
                                                            "A connection came in — checking whether it is the same player…") },
            { "Settings/Online/St/PeerJoined",              new Entry("有客机连进来了，正在核对…", "A client connected — checking…") },
            { "Settings/Online/St/SilentTimeout",           new Entry("{0} 秒没收到对面的任何消息",
                                                            "No message from the opponent for {0} seconds") },
            { "Settings/Online/St/Reconnecting",            new Entry("正在重连主机…", "Reconnecting to the host…") },
            { "Settings/Online/St/CaughtUp",                new Entry("连上了，正在补上这一局的进度…",
                                                            "Connected — catching up on this match…") },
            { "Settings/Online/St/ReconnectFailed",         new Entry("重连失败，稍后再试：{0}", "Reconnect failed, will retry: {0}") },
            { "Settings/Online/St/BadHello",                new Entry("对面发来的握手包解不出来",
                                                            "The opponent's handshake packet could not be parsed") },
            { "Settings/Online/St/VersionMismatch",         new Entry("两边版本不一样（对面协议 v{0}，本机 v{1}）—— 要用同一份构建",
                                                            "Different versions (opponent on protocol v{0}, this build on v{1}) — both sides need the same build") },
            { "Settings/Online/St/WrongPassword",           new Entry("密码不对", "Wrong password") },
            { "Settings/Online/St/Refused",                 new Entry("拒绝了这次连接：{0}", "This connection was refused: {0}") },
            { "Settings/Online/St/PeerBackWaitReport",      new Entry("「{0}」连回来了，正在等他报进度…",
                                                            "`{0}` is back — waiting for their progress…") },
            { "Settings/Online/St/PeerInLobby",             new Entry("「{0}」进来了 —— 各自选好卡组就能开战",
                                                            "`{0}` joined — pick your decks and you can fight") },
            { "Settings/Online/St/PeerRefused",             new Entry("对面拒绝了连接", "The opponent refused the connection") },
            { "Settings/Online/St/PeerLeft",                new Entry("对面退出了", "The opponent left") },
            { "Settings/Online/St/ResumedWaitProgress",     new Entry("已经连上主机，正在等他补这一局的进度…",
                                                            "Connected to the host — waiting for this match's progress…") },
            { "Settings/Online/St/ClientLobby",             new Entry("连上主机了 —— 各自选好卡组就能开战",
                                                            "Connected to the host — pick your decks and you can fight") },
            //  🆕 **2026-10-19（P6d · A1079①）**：`NetSession` 收 `Ack`（ok）后给【检查连接】回的那一句，
            //    经 `OnCheckDone` → `Shell/SettingsWindow` 的 `SetFlash(() => (ok ? "✅ " : "❌ ") + why)` 上屏。
            //    两张原版表都搜过、**0 命中**（原版联机走 PlayFab、没有「检查连接」这套流程）⇒ **键名 + 两列全自拟**，
            //    ZH 列 = 调用点原话逐字（⇒ 中文档零变化）。
            { "Settings/Online/St/CheckOk",                 new Entry("连接成功 —— 可以直接开战了",
                                                            "Connected — you can start the battle now") },
            { "Settings/Online/St/ResumeSent",              new Entry("「{0}」回来了 —— 已把这一局的 {1} 条动作发过去",
                                                            "`{0}` is back — sent {1} actions from this match") },
            { "Settings/Online/St/ResumeCaughtUp",          new Entry("追上了 —— 重放这一局的 {0} 条动作",
                                                            "Caught up — replaying {0} actions from this match") },
            { "Settings/Online/St/RejectBadKey",            new Entry("重连被拒：钥匙对不上",
                                                            "Reconnect refused: the key does not match") },
            { "Settings/Online/St/RejectNoLog",             new Entry("重连被拒：主机没有权威动作流",
                                                            "Reconnect refused: the host has no authoritative action log") },
            { "Settings/Online/St/InBattle",                new Entry("对局中", "In battle") },
            { "Settings/Online/St/HostSide",                new Entry("（本机是主机，动作由本机定序）",
                                                            " (this machine is the host — it orders the actions)") },
            { "Settings/Online/St/ClientSide",              new Entry("（客机：操作由主机确认）",
                                                            " (client: actions are confirmed by the host)") },
            { "Settings/Online/St/PortBusy",                new Entry("端口 {0} 已被占用 —— 换一个端口，或先关掉已经在跑的那个实例",
                                                            "Port {0} is already in use — pick another port, or close the other running instance") },
            { "Settings/Online/St/ConnRefused",             new Entry("对面拒绝了连接（{0} 端口没人在听）—— 主机那边要先点「保存」并保持游戏开着",
                                                            "The opponent refused (nothing is listening on port {0}) — the host must press Save and keep the game open") },
            { "Settings/Online/St/HostNotFound",            new Entry("这个 IP 地址找不到——检查一下有没有抄错",
                                                            "This IP address was not found — check for a typo") },
            { "Settings/Online/St/SocketError",             new Entry("网络错误：{0}", "Network error: {0}") },
            { "Settings/Online/St/NoIp",                    new Entry("没有填 IP 地址", "No IP address filled in") },
            { "Settings/Online/St/ConnectTimeout",          new Entry("连接 {0}:{1} 超时（{2} 毫秒）—— 对面没开主机，或防火墙挡住了",
                                                            "Timed out connecting to {0}:{1} ({2} ms) — no host there, or a firewall is blocking") },
            { "Settings/Online/St/NoStream",                new Entry("连上了但拿不到流：{0}",
                                                            "Connected but could not get the stream: {0}") },
            { "Settings/Online/St/ReadAbort",               new Entry("读取中断：{0}", "Read interrupted: {0}") },
            { "Settings/Online/St/PeerClosed",              new Entry("对面关掉了连接", "The opponent closed the connection") },
            { "Settings/Online/St/BadFrame",                new Entry("帧长度不合理（{0} 字节）—— 对面发的不是本协议的帧",
                                                            "Bad frame length ({0} bytes) — what the opponent sent is not a frame of this protocol") },
            { "Settings/Online/St/BadEnvelope",             new Entry("收到的帧解不出信封",
                                                            "The received frame has no parseable envelope") },
            { "Settings/Online/St/NotConnected",            new Entry("还没连上，发不出去", "Not connected yet, cannot send") },
            { "Settings/Online/St/SendFailed",              new Entry("发送失败：{0}", "Send failed: {0}") },
            { "Settings/Online/St/StackDual",               new Entry("（双栈）", "(dual stack)") },
            { "Settings/Online/St/StackV4Only",             new Entry("（仅 IPv4）", "(IPv4 only)") },

            //  ---------------------------------------------------------- ⑬·B `Settings/Online/Lobby/*` · 17 条
            //  出处：`NetMatchmaking.cs` `:197`(+`:198-200`) · `:212`(+`:213-215`) · `:249` · `:233` · `:360/:362/:363`
            //    · `:402-406` · `:427` · `:466-467` · `:502-503` · `:509-510` · `:539-540` · `:587`
            //  ⚠️ 这 19 处里 `PeerLostHint`/`PeerLeftHint` 是**提示行**（尺子 = `Shell/SearchingMatchPopup` 的
            //     `HintLineWidth(句) > HintLineMaxWidth`，**80 个半宽字位**；其中 `HintLineMaxChars = 40` 是
            //     **中文档**那一档的上限 —— ⛔ 别再写成「40 字」的预算（那是 `A1083` 改尺子**之前**的口径）。
            //     超了会 `LogWarning`）⇒ P6 §⑤ 要求自检额外断 `Loc.T(键).Length <= SearchingMatchPopup.HintLineMaxChars`
            //     （**只量中文列**：字符数 ≤ 40 ⇔ 全宽 ≤ 80 位，是同一条尺子的**更严**半边，见那条常量的 doc）。
            //  ⚠️ `{0}`/`{1}` 的含义逐条不同（`PeerLeftHint` 的 `{0}` = 对方报的离开理由、`{1}` = 本地撤销那半句）⇒ 拼法见调用点。
            { "Settings/Online/Lobby/PeerLostHint",         new Entry("对面掉线了，{0}（两边回来各点一次 `Battle!`）",
                                                            "Opponent disconnected, {0} (both of you press Battle! again after they return)") },
            { "Settings/Online/Lobby/PeerLost",             new Entry("对面掉线了 —— 联机断开。\n{0}，回到大厅。\n（对面回来之后，两边各自重新点一次 `Battle!`。原版那一刻走的是 `SearchOpponentManager.CancelSearchForDisconnect`：弹窗 + 取消搜索。）",
                                                            "Opponent disconnected — the connection is gone.\n{0}, back to the lobby.\n(After the opponent returns, both of you press Battle! once more. At that moment the original ran SearchOpponentManager.CancelSearchForDisconnect: popup + cancel search.)") },
            { "Settings/Online/Lobby/PeerLeftHint",         new Entry("联机结束：{0} —— {1}", "Match ended: {0} — {1}") },
            { "Settings/Online/Lobby/PeerLeft",             new Entry("联机结束：{0}\n{1}，回到大厅。\n（要再打一局：两边重新各点一次 `Battle!`。原版那一刻走的是 `SearchOpponentManager.CancelSearchForDisconnect`：弹窗 + 取消搜索。）",
                                                            "Match ended: {0}\n{1}, back to the lobby.\n(To play again: both of you press Battle! once more. At that moment the original ran SearchOpponentManager.CancelSearchForDisconnect: popup + cancel search.)") },
            //  🆕 **2026-10-19（P6d · A1079②）**：`NetMatchmaking.DeferToBattleLayer(what)` 的两个 **`what` 碎片**
            //    —— 它们喂 `Lobby/DeferToBattle` 的 `{0}`，原来是**裸中文字面量**（英文档下会冒中文）。
            //    🔴 **为什么不改父键形状**（把 `DeferToBattle` 拆成两条整句键）：父键在
            //    `Editor/NetSelfTest.cs` 的键清单里、也在**提示行尺子**的账里（尺子 = `Shell/SearchingMatchPopup` 的
            //    `HintLineWidth(句) > HintLineMaxWidth`，**80 个半宽字位**；`NetMatchmaking` 的注释逐字引它）
            //    ⇒ 拆了要动**别的格**；**碎片键的代价只落在这一处**。⇒ 选「碎片建成独立键」。
            //    ⚠️ **两张原版表都搜过、0 命中** ⇒ 键名 + 两列全自拟；ZH 列 = 调用点原话逐字（中文档零变化）。
            //    ⚠️ 这两个碎片**只**用在 `DeferToBattle` 的 `{0}` 上；`Lobby/PeerLostHint`/`PeerLeftHint`
            //      那两条的 `{0}` 走的是**别的东西**（`tail` / `body`）⇒ ⛔ 别把它们对调过去。
            { "Settings/Online/Lobby/PeerLostFrag",         new Entry("对面掉线了",     "Opponent disconnected") },
            { "Settings/Online/Lobby/PeerLeftFrag",         new Entry("对面离开了：",   "Opponent left: ") },
            { "Settings/Online/Lobby/MatchRevoked",         new Entry("这一局的匹配已经撤销", "This match's setup has been revoked") },
            { "Settings/Online/Lobby/NotMatchingThisGame",  new Entry("（本机本来就没在匹配这一局）",
                                                            "(this machine was not matching this match anyway)") },
            { "Settings/Online/Lobby/DeferToBattle",        new Entry("{0} —— 已开局、正在进战场（后面由对局那一层说）",
                                                            "{0} — the match has already started, entering the arena (the battle layer will continue)") },
            { "Settings/Online/Lobby/BotNoLink",            new Entry("联机没连上", "Not connected") },
            { "Settings/Online/Lobby/BotSessionNotReady",   new Entry("联机会话现在是 `{0}`（还没握手完）",
                                                            "The session is now `{0}` (handshake not finished)") },
            { "Settings/Online/Lobby/BotEmptyDeck",         new Entry("这副牌是空的", "This deck is empty") },
            { "Settings/Online/Lobby/PlayedVsBot",          new Entry("这一局打的是电脑，不是联机。\n原因：{0}。\n你在设置里配过联机了 —— 请到「设置 → 联机」点一次{1}，再回来点 `Battle!`。",
                                                            "This match is against the AI, not online.\nReason: {0}.\nYou have configured online play — go to Settings → Online and press {1} once, then press `Battle!` again.") },
            { "Settings/Online/Lobby/LobbyRestored",        new Entry("对面回来了 —— 联机已恢复。要开这一局，两边重新各点一次 `Battle!`",
                                                            "The opponent is back — online play restored. Both sides press `Battle!` once more to start.") },
            { "Settings/Online/Lobby/StartAfterCancel",     new Entry("对面在你取消之后开局了 —— 这一局没有进。\n对面那边会停在等待界面上，请重新约一次。",
                                                            "The opponent started the match after you cancelled — this one did not go through.\nThe other side will stay on the waiting screen; please arrange it again.") },
            { "Settings/Online/Lobby/MissedCancel",         new Entry("对面在你开局之后才点了取消 —— 这一局照旧开始。\n对面那边会看到「已经开局、取消不了」，要退出只能在对局里投降。",
                                                            "The opponent cancelled after you started — this match starts anyway.\nThey will see \"already started, cannot cancel\"; the only way out is to resign during the battle.") },
            { "Settings/Online/Lobby/PeerCancelled",        new Entry("对面取消了这一局的匹配 —— 双方都没有开局，退回大厅。\n可以各自重新点一次 `Battle!`。",
                                                            "The opponent cancelled this match — neither side started, back to the lobby.\nYou can each press `Battle!` again.") },
            { "Settings/Online/Lobby/ModeMismatch",         new Entry("两边选的模式不一样：本机是「{0}」，对面是「{1}」。\n这一局没有开成 —— 请两位换成同一个模式，再各自点一次 `Battle!`。",
                                                            "You picked different modes: this machine chose \"{0}\", the opponent chose \"{1}\".\nThe match did not start — please pick the same mode and each press `Battle!` again.") },
            { "Settings/Online/Lobby/StartParseFailed",     new Entry("开局参数没能解析出来，这一局开不了。\n请两边都退回主菜单，重新点一次 `Battle!`。",
                                                            "The match parameters could not be parsed, so this match cannot start.\nBoth of you go back to the main menu and press `Battle!` again.") },

            //  ---------------------------------------------------------- ⑬·C `Settings/Online/Cancel/*` · 3 条
            //  出处：`NetMatchmaking` 里那三处 `why = Loc.T("Settings/Online/Cancel/…")`（`WhyNoLink` / `WhyNotMatching` / `WhyStarted`）。
            { "Settings/Online/Cancel/WhyNoLink",           new Entry("联机没连上（这一局本来就没走联机）",
                                                            "Not connected online (this match was never an online one)") },
            { "Settings/Online/Cancel/WhyNotMatching",      new Entry("这一局还没进入联机匹配",
                                                            "This match has not entered online matchmaking yet") },
            { "Settings/Online/Cancel/WhyStarted",          new Entry("这一局已经开局了（开局包已经发出/收到）—— 取消不了；要退出请在对局里投降。",
                                                            "This match has already started (the start packet was sent/received) — it cannot be cancelled; to leave, resign during the battle.") },

            //  ---------------------------------------------------------- ⑬·D `Settings/Online/Echo/*` · 2 条
            //  出处：`NetConfig` 里那两处（`NoEcho` / `ProbeError`）；两处进 `SettingsWindow.EchoText()` 末尾。
            //  ⚠️ **2026-10-18 之后·第六会话就地订正（铁律 5，`A1064` 末条）**：那句函数名**已过期** ——
            //     `EchoText` 在 `A1064` 那一轮已改名 **`EchoFlash`**、且**返回值从 `string` 改成 `Func<string>`**
            //     （理由见 `SettingsWindow.SetFlash` 的 doc：`_flash` 改成「现算工厂」）。落点 =
            //     `Shell/SettingsWindow.cs` 的 `Func<string> EchoFlash(NetConfig.ExternalAddrs r)`。
            { "Settings/Online/Echo/NoEcho",                new Entry("两个方向都没探到 —— 可能是回显站被网络挡了（不是「你没有公网地址」）。",
                                                            "Neither direction got a response — the echo sites may be blocked by your network (this does not mean \"you have no public address\").") },
            { "Settings/Online/Echo/ProbeError",            new Entry("探测出错：{0}", "Probe error: {0}") },

            //  ---------------------------------------------------------- ⑬·E `Settings/Online/Upnp/*` · 9 条
            //  出处：`UpnpPortMapper.cs:108`（`Busy`）· `:122`（`Error`）· `:164`（`NoResponse`）· `:178`（`NoService`）
            //    · `:216`（`PortTaken`）· `:219`（`NotPermitted`）· `:221`（`Rejected`）· `:230`（`Cgnat`）· `:237`（`Ok`）
            //    —— 九条都是 `Result.message`，由 `:125 NetRuntime.Notice(r.message)` 弹给玩家。
            //  ⚠️ `{0}` = 端口号 / 错误码 / 外网地址（逐条不同）；`Ok` 的 `{1}` = 有外网地址时那句「，你家的外网地址是 X」，没有时为空串。
            //  ⚠️ 英文列把 P6 §③ 提案里的 `【怎么联机】` 的**中文括号改名成 `[How to connect]`**
            //     —— `【】` 是 U+3010/U+3011，落在 `Loc.HasCjk` 的 `0x3000-0x303F` 区间里 ⇒
            //     照抄会让「英文列不许含汉字」那条断言（灭自证 C1）直接红（P6 §③ 自己定的规矩）。
            { "Settings/Online/Upnp/Busy",                  new Entry("上一次「向路由器要端口」还没跑完 —— 这次先跳过。",
                                                            "The previous \"ask the router for a port\" has not finished — skipping this time.") },
            { "Settings/Online/Upnp/Error",                 new Entry("向路由器要端口时出错：{0}",
                                                            "Error while asking the router for a port: {0}") },
            { "Settings/Online/Upnp/NoResponse",            new Entry("没能从路由器那里问到端口映射（UPnP 没开、或路由器不支持）。\n→ 想让网友连进来：① 去路由器管理页把 UPnP 打开 再点一次【保存】；② 或者两边装同一个虚拟局域网工具（Tailscale / ZeroTier 这类，见【怎么联机】）。",
                                                            "Could not get a port mapping from the router (UPnP is off, or the router does not support it).\n→ To let a friend connect: ① turn UPnP on in the router admin page and press Save again; ② or both install the same virtual-LAN tool (Tailscale / ZeroTier, see [How to connect]).") },
            { "Settings/Online/Upnp/NoService",             new Entry("路由器回应了，但它没有提供端口映射服务（不是常见的家用路由器固件）。\n→ 这条只能走虚拟局域网工具那条路（见【怎么联机】）。",
                                                            "The router answered, but it does not offer a port-mapping service (not a typical home router firmware).\n→ This one can only go through a virtual-LAN tool (see [How to connect]).") },
            { "Settings/Online/Upnp/PortTaken",             new Entry("路由器说 {0} 这个端口上已经有别的映射了 ⇒ 换一个端口再来（或者去路由器管理页把那条旧映射删掉）。",
                                                            "The router says port {0} already has another mapping ⇒ try a different port (or delete that old mapping in the router admin page).") },
            { "Settings/Online/Upnp/NotPermitted",          new Entry("路由器不接受「永久」映射（错误码 725）—— 这一台得手动在路由器上做端口映射。",
                                                            "The router does not accept \"permanent\" mappings (error 725) — this one has to be mapped manually on the router.") },
            { "Settings/Online/Upnp/Rejected",              new Entry("路由器拒绝了端口映射请求（错误码 {0}）。\n→ 有些固件即使开着 UPnP 也不放行入站映射，这条只能走别的路（见【怎么联机】）。",
                                                            "The router refused the port-mapping request (error {0}).\n→ Some firmwares block inbound mappings even with UPnP on; this one has to take another route (see [How to connect]).") },
            { "Settings/Online/Upnp/Cgnat",                 new Entry("✅ 端口映射要到了，但你这台大概率在「大内网」(CGNAT) 里 ——\n路由器自己的外网地址是 {0}（私网段）⇒ 外面照样连不进来。\n→ 这种情况打客服电话要「公网 IP」才有用，或走虚拟局域网工具。",
                                                            "✅ The port mapping was granted, but this machine is very likely behind a carrier-grade NAT (CGNAT) —\nthe router's own WAN address is {0} (a private range) ⇒ outside connections still will not get in.\n→ Call your ISP and ask for a \"public IP\", or use a virtual-LAN tool.") },
            { "Settings/Online/Upnp/Ok",                    new Entry("✅ 已经在路由器上开好了 {0} 端口（TCP）{1} —— 把外网地址 + 端口给朋友就能连进来。",
                                                            "✅ Port {0} (TCP) is now open on the router{1} — give the public address + port to a friend and they can connect.") },
            //  🆕 **2026-10-19（P6d · A1079③）**：上面那条 `{1}` 的**碎片**（有外网地址时才填）。
            //    ⚠️ 原来是 `UpnpPortMapper.Map` 里的**裸中文字面量**「，你家的外网地址是 」+ wan
            //    ⇒ 英文档下会冒中文（上一轮已如实停手记下，本件解掉）。**碎片建成独立键**（⛔ 没改父键形状）。
            //    ⚠️ **两张原版表都搜过、0 命中** ⇒ 键名 + 两列全自拟；ZH 列 = 调用点原话逐字。
            //    ⚠️ EN 列的行首是 `, `（逗号 + 空格）—— 接在 `router` 后面读得通；⛔ 别把逗号挪到父键末尾
            //      （那会让「没有外网地址」那一档也多出一个逗号）。
            { "Settings/Online/Upnp/OkWanSuffix",           new Entry("，你家的外网地址是 {0}",
                                                            ", your public address is {0}") },

            // ============================================================ ⑭ 双语④ 波 0b4 · 设置窗图形/通用页 + `Wire/*`（新增）
            //
            //  🔴 **这一节是谁、为什么**：波 1b 要把 `Shell/SettingsWindow.cs` 图形页/通用页那几行
            //     与 `Net/*` 那 7 处**走线**的理由串接上表 —— 而 `Loc.T` 对**没建的键**是
            //     「返回键名本身 + 出声」⇒ **必须先建键、后接线**（施工单 §⑦ 冲突 1）。本节**只加键**，
            //     ⛔ **一个调用点都没改**（那是波 1b 的事）⇒ 今天界面**一个字都不变**。
            //     逐条依据 = `资料/普查产出_第五会话/查证_23双语键盘点.md` 表 A（A1 / A2 / A4 三组）。
            //
            //  **查证口径（两张表都搜过 —— 只搜一张 = 无效否定）**：
            //    · 表① `d:/2/新解包资源/assets_full/**/MonoBehaviour/*.json` 的 `mTerm`
            //      （`Settings/` 前缀 **47 条**）—— ⑭·A 那 6 条**在这里查到了原版键名**（逐条出处见下）。
            //    · 表② `d:/2/tools/il2cpp_out/stringliteral.json`（26,507 条；`Settings/` 前缀 **10 条**）
            //      —— `SmallScreen|IncreaseUI|SuperSampl|VSync|FrameLimit|Unlimited|HiFPS|ExtendedCompat|SelectQuality`
            //      **逐词 0 命中** ⇒ ⑭·A 那 6 条**只活在表①里**。
            //    · ⑭·B / ⑭·C 两族：**两张表都搜过、都没有** ⇒ 键名 + 两列文案**全自拟**（逐条注明）。
            //
            //  ⚠️ **⑭·A / ⑭·B 的 EN 列拿不到原版英文**：本地 prefab **整包被本地化成西班牙语**
            //     （`Seleccionar Calidad` / `Aumentar tamaño de UI` / `Sobremuestreo` / `Límite de FPS` / `Ilimitado`），
            //     英文原文在**远端 I2 表** ⇒ EN 取**我们界面今天写死印的那串**（`Shell/SettingsWindow.cs` 调用点原文）
            //     —— 这条**是我们挑的**、⛔ **不是原版原文**（与 `Settings/{Graphics,Media}/Title` 同一口径）。
            //     🔴 **唯一一条例外 = `Settings/Graphics/Vsync`**：原版那颗 TMP 的 `m_text` 就是英文 `VSync`
            //     （本包唯一一条非西语）⇒ 它的 EN 列**是原版原文**。
            //  ⚠️ **⑭·B 的 ZH 列 = `Shell/SettingsWindow.cs` 调用点的原话逐字**（⇒ 接上之后**中文档零变化**）；
            //     其中 `Flash/{SmallScreenUI,AutoZoom,SuperSampling}` 与 `FpsText/*` 那几条**调用点本来就写英文**
            //     ⇒ 它们的 ZH 列**照抄英文原文**（**不是漏译** —— 与表 A 的 ZH 列逐字一致，⛔ 别顺手改成中文）。
            //  ⚠️ `{0}`/`{1}`/`{2}` = 调用点的运行期值（消费方用 `Replace` 或 `string.Format`）；`\n` = 真换行。
            //  ⚠️ ZH 列里的 `**` 是**调用点字面量里就有的强调符**，**照抄保留**（同 `⑬` 那条）。
            //  ⚠️ 🔴 **EN 列不许含汉字**：`Loc.HasCjk` 的区间**含 `U+3000-303F` 与 `U+FF00-FFEF`**
            //     ⇒ 全角括号 `（）`、全角空格 `　`、`【】` **都不能进 EN 列** ⇒ EN 里一律用 **ASCII 括号**。

            //  ---------------------------------------------------------- ⑭·A `Settings/Graphics/*` 图形页那 6 行的**标签** · A1 · 6 条
            //  🔴 **键名 = 原版 `Localize.mTerm` 原文**（表 A 逐条标「原版【有】键」，⛔ **没自拟**）——
            //     出处 = `bundle_menus_assets_all` 的 `Main Menu Settings Window/…/Graphics Tab/Content/…` 子树里
            //     那颗 `Localize` 的 `mTerm`（表①实读；同 GO 的 TMP `m_text` 是**西班牙语**，见本节开头那条）。
            //  ⚠️ 消费点（归波 1b 接，本件**不动**）：`Shell/SettingsWindow.cs`
            //     `:1743`（画质）· `:1874`（小屏 UI）· `:1894`（超采样）· `:1897`（VSync）· `:2215`（帧率上限标题）
            //     · `:2243`（滑块刻度 `FpsTickText[2]`）；⚠️ 那几处的**节点名照原版不动**、只有显示字走表。
            //  📌 表 A 记：`FpsTickText[0]/[1]` = `30`/`60` 是**纯数字** ⇒ **不建键**。
            { "Settings/Graphics/SelectQuality",       new Entry("画质",     "Quality") },            // 节点 `…/Quality  Selector/Quality selector text`（TMP = `Seleccionar Calidad`，西语）· EN 自拟
            { "Settings/Graphics/IncreaseUISize",      new Entry("小屏 UI",  "Small Screen UI") },    // 节点 `…/Content/Small Screen Size Toggle/Label`（TMP = `Aumentar tamaño de UI`）· EN 自拟
            { "Settings/Graphics/EnableSuperSampling", new Entry("超采样",   "Use super sampling") }, // 节点 `…/Content/Use super sampling/Label`（TMP = `Sobremuestreo`）· EN 自拟
            { "Settings/Graphics/Vsync",               new Entry("VSync",    "VSync") },              // 节点 `…/Content/Vsync/Label`；🔴 EN **逐字 = 原版 TMP**（本包唯一一条非西语）
            { "Settings/Graphics/FrameLimit",          new Entry("帧率上限", "FPS limit") },          // 节点 `…/Content/FPS Limit/Title`（TMP = `Límite de FPS`）· EN 自拟
            { "Settings/Graphics/UnlimitedFPS",        new Entry("不限帧",   "Unlimited") },          // 节点 `…/Content/FPS Limit/FPS Slider/Unlimited`（TMP = `Ilimitado`）· EN 自拟

            //  ---------------------------------------------------------- ⑭·B 图形/通用页那几条 `_flash` 状态行 · A2 · 12 条（全自拟）
            //  🔴 **为什么算「要建」**：`Shell/SettingsWindow.RefreshOnline()` =
            //     `_statusLabel.SetText((_flash ?? "") + "\n" + 状态)` ⇒ 图形/通用页那几条 `_flash`
            //     **会印到联机页的状态行上**（`A1020` 的订正已推翻「图形页 `_flash` 不上屏」）⇒ 它们**是①类**（会上屏）。
            //  ⚠️ **原版无对等**（两张表都搜过）⇒ **键名与两列文案全自拟**；ZH 列 = 调用点原话逐字（见本节开头）。
            //  📌 **键名族的形状**：`Settings/General/Flash/*` 与 `Settings/Graphics/Flash/*`（照 `Settings/Online/St/*`
            //     那一族的「父键/子键」写法）。⚠️ 表 A 留了一条**待裁口径**：「`_flash` 前缀要不要复用 ⑭·A 的标签键」
            //     （例 `Loc.T("Settings/Graphics/Vsync") + " → " + On`）—— 本件按表 A 给的**最小改动版**（各起一条 `Flash/*`），
            //     ⛔ **两种只能选一种**（铁律 6）；若调度台改口径，删掉 `Flash/*` 那一族即可（一条消费点都还没有）。
            { "Settings/General/Flash/Language",       new Entry("语言 → {0}（{1}）", "Language → {0} ({1})") },  // `:1589`；`{0}` = 语言名、`{1}` = 枚举值（`Loc.Current`）
            { "Settings/General/LangHasNoTable",       new Entry("　⚠️ 本地没有这一套文案 ⇒ 界面文字回退英文",
                                                                     "⚠️ No text for this language locally ⇒ the UI text falls back to English") }, // `:1590`；⚠️ ZH 那个**行首全角空格 U+3000 是调用点原文**
            { "Settings/Graphics/Flash/Quality",       new Entry("画质档 → {0}", "Quality → {0}") },               // `:1987`（`CycleQuality`）
            { "Settings/Graphics/Flash/Vsync",         new Entry("VSync → {0}", "VSync → {0}") },                 // `:1995`（`ToggleVsync`）
            { "Settings/Graphics/Flash/SmallScreenUI", new Entry("Small Screen UI → {0}", "Small Screen UI → {0}") }, // `:2006`；ZH 列照抄调用点（那串**本来就叫英文**，⛔ 别改成中文）
            { "Settings/Graphics/Flash/AutoZoom",      new Entry("Auto Zoom → {0}", "Auto Zoom → {0}") },         // `:2029`；同上（调用点写 `Auto Zoom`，大写 Z）
            { "Settings/Graphics/Flash/SuperSampling", new Entry("Use super sampling → {0}", "Use super sampling → {0}") }, // `:2051`；同上
            { "Settings/Graphics/Flash/Fps",           new Entry("帧率上限 → {0}", "FPS limit → {0}") },           // `:2396`；`{0}` = `FpsText()`（见下面两条）
            //  👇 这两条 = `FpsText()`（`:1970`）那半句；🔴 **ZH 列就是调用点那两个字符串原文**（`"unlimited"` / `f + " fps"`）
            //     —— 与表 A 的 ZH 列逐字一致（⇒ 中文档今天印 `帧率上限 → unlimited`，接上之后**仍是**它）。
            //     ⚠️ 表 A 只写了「待建（1 条带 `{0}`）」、**键名留空**（标 `—`）⇒ 键名 = 本件照 `Flash/*` 族的形状自拟。
            { "Settings/Graphics/FpsText/Unlimited",   new Entry("unlimited", "unlimited") },                     // `:1970`：`Application.targetFrameRate <= 0`
            { "Settings/Graphics/FpsText/Value",       new Entry("{0} fps",   "{0} fps") },                       // `:1970`：`{0}` = `Application.targetFrameRate`（>0 那一支）
            //  👇 「开 / 关」两颗 **共用**（`:1995/:2006/:2029/:2051` 四处的 `{0}` 都由它们填）。
            //     🔴 **两张表都搜过、没有这一对**：`MainMenu/General/*` 只有
            //     `{OK,Cancel,Confirm,Select,Claim,Retry,Update,Rarity,melee,ranged,DontHaveDeckForGameMode}`；
            //     唯一带 On/Off 的是 `MainMenu/Settings/ButtonLabel/MuteChat_{On,Off}` —— **语义不同、⛔ 不复用**。
            { "MainMenu/General/On",                   new Entry("开", "On") },
            { "MainMenu/General/Off",                  new Entry("关", "Off") },

            //  ---------------------------------------------------------- ⑭·C `Settings/Online/Wire/*` · 8 条（全自拟）
            //  出处 = 调用点原话逐字；这 8 处**都是「走线的理由串」**（`MsgBye.reason` / `MsgReject.reason`）：
            //    · `Net/NetRuntime.cs`  `Reset()` 里 `Session.Close(true, …)`
            //    · `Net/NetSession.cs`  收 `Reconnect` 那两个拒绝支（坏钥匙 / 没有记录）
            //    · `Net/NetSession.cs`  收 `Resume` 解不出来那支（`Close(true, …)`）
            //    · `Net/NetSession.cs`  `Close(say, reason)` 的**兜底**（`reason == null` 那一档）
            //    · `Net/NetBattle.cs`   `Dispatch` 的 `case NetKind.Action`（`MsgReject.reason`）
            //    · `Net/NetBattle.cs`   `ReconnectCountdownExpired` 主机那一支（`Close(true, …)`）
            //    · `Battle/BattleDriver.cs` `LeaveNetRoom()`（`Close(true, …)`）← 🆕 第 8 条
            //  🔴 **两张表都搜过 = 0 命中**（同 `⑬` 那条：原版联机走 PlayFab，没有这套流程）⇒ **键名 + 英文列全自拟**；
            //     ⛔ **不是** `Settings/Online/St/*` 那一族（那族是**本机显示**的状态串，键与值都已建好，见 `⑬·A`）
            //     —— 这两族**语义不同、并存**（例：`St/RejectBadKey`「重连被拒：钥匙对不上」是本机显示，
            //     而 `Wire/BadKey`「这把钥匙对不上这一局」是**发给对面**的那一句）。
            //
            //  ✅ **2026-10-19（P6d）就地订正（铁律 5）**：这一节原来写着「本件**只建键、不接线**」、
            //     并交代「接线时两端要同批更新（否则对面收到的是**发送方语言**的那一串）」。
            //     **P6d 已经把线接完了** —— 做法**不是**发那串已渲染的中文，而是**线上发词条键**、
            //     收侧 `NetProtocol.NetWireText.Unpack` 按**它自己的语言**取词
            //     （编码、兼容面与「为什么不 + 协议版本号」→ `NetWireText` 的类注释）。
            //     ⇒ 「两端要同批更新」这句话**不再成立**：**不 +版本号**、旧端收键名照印（键名是纯 ASCII、
            //        且长度 ≤ `NetProtocol.MaxPeerTextChars`(40) ⇒ `ClampPeerText` 那一闸不会截它）。
            //
            //  🔴 **2026-10-19（P6d）另一处就地订正（铁律 5）：上面那句「`St/*` 与 `Wire/*` 是两族」【部分作废】。**
            //     分族的真正判据不是「本机显示 vs 发给对面」，而是「**两个角色下要印的那句话措辞是否相同**」：
            //       · **措辞【不同】** ⇒ 两条键（本族 `Wire/*` 存在的理由）；
            //       · **措辞【相同】** ⇒ **一条键两用**，⛔ 别另开 —— 另开只会多出一份迟早会漂的副本（铁律 6）。
            //     落在第二类的有三条：`St/BadHello` · `St/VersionMismatch` · `St/WrongPassword`
            //     （`NetSession` 收 `Proof` 那一段的握手失败理由，**同时**是本机 `StatusText` 与
            //      `MsgAck.reason` 的走线载荷）⇒ 它们**留在 `St/*`**，走线时发的是**它们自己的键**
            //     （键是 ASCII、语言无关 ⇒ 对面按对面语言取词，正是要的效果）。
            //     ⚠️ 所以「凡走线者必在 `Wire/*`」是**错的**；判据只有上面那一条。
            { "Settings/Online/Wire/HostRestarted",    new Entry("对面重开了联机",           "The opponent restarted the session") },
            { "Settings/Online/Wire/BadKey",           new Entry("这把钥匙对不上这一局",     "This key does not match this match") },
            { "Settings/Online/Wire/NoRecord",         new Entry("主机这边没有这一局的记录", "The host has no record of this match") },
            { "Settings/Online/Wire/BadResume",        new Entry("重连包解不出来",           "The reconnect packet could not be parsed") },
            { "Settings/Online/Wire/PeerDone",         new Entry("对面结束了这一局",         "The opponent ended this match") },
            { "Settings/Online/Wire/PeerLeftMatch",    new Entry("对面离开了这一局",         "The opponent left this match") },
            { "Settings/Online/Wire/NotYourTurn",      new Entry("不是你的回合",             "Not your turn") },
            { "Settings/Online/Wire/RoomGone",         new Entry("这一局的联机房间已经散了", "This match's online room is gone") },
        };
        //  ⚠️ **`Wire/HostRestarted` 的 ZH/EN 是本轮【故意改过的】**（`A1038` 顺手项，同 `NetBattle` 那条
        //     R7 中性化纪律）：原来 ZH = 调用点原话「主机重开了」，而那个调用点（`NetRuntime.Reset()`）
        //     **两端都会走**（主机点【保存】/ 客机点【检查连接】）⇒ 客机捎给主机的是一句**语义颠倒**的话。
        //     现措辞对**收方**恒成立（它看到的确实是「对面」重开了这一条）。⇒ 这里**不是**「中文档零变化」，
        //     如实标出（这是本族唯一一条 ZH 列 ≠ 调用点原文的）。
        //  ⚠️ `Wire/NotYourTurn` 与 `Battle/Tips/NotYourTurn`（本文件 `Battle/` 那一节）**ZH 同值、语义不同**
        //     —— 后者是**本机 HUD 的提示行**、前者是**回给对面的拒绝码**（判据 → `NetBattle.Dispatch`
        //     那段注释的判据 ②）⇒ **两族并存，⛔ 别合并**。

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
        /// <para>🔴 **2026-10-18（第十五轮 · `G5`）为什么要开这个口**：`Editor/TmpSetup.Corpus()` 的语料
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
