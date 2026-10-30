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
//  · 🎉 🔴 **2026-10-14（第二十七会话）就地更正：中文那一列【不再是「我们译的」】—— 官方 zh-CN 表已到手。**
//    原文写「🔴 **中文那一列是我们自己译的**（**不是原版文案**）：原版的中文在**远端 I2 语言表**里
//    （84 个本地 bundle 里没有 `localization_assets_all.bundle`、246,807 个文件扫中文串零命中
//    —— 见 §23 与 `资料/待办判据_卡面卡池与双语.md`）。这一列照项目口径**如实标注为「我们译的」**，
//    与 `数据/卡牌翻译/zh_cards.json`（卡名/卡面那批）是同一种东西。」
//    —— **后半那句实测是对的，前半那句结论是错的**：原版**有**官方中文表
//    （2025-07-30 旧快照 9,440/9,447 条非空；🔴 **现用表 = 2026 build，10,854 条**，见 `_count`），
//    只是**不在客户端安装目录里**（在远端 Addressables 包 `localization_assets_all` 里）。
//    该包 2026-10-13 从旧 Windows 用户缓存里抢救出来、2026-10-14 **已解开**
//    （**逐字节重复密钥 XOR**，不是压缩 —— 前任试的 gzip/zlib/deflate/lzma/bz2 方向是错的）。
//    数据件 → `数据/游戏数据/官方本地化_en+zh-CN.json`（`工具/gen_official_loc.py` 生成）；
//    逐条清单与脚本 → `资料/普查产出_第二十七会话/新发现_待立账.md` 的 `A1788`。
//
//    🔴 **现状（现读口径 · 2026-10-23「官方中文全量收口」`C1`）** —— ⚠️ 原来这里那几组数
//       （「官方表有键 266 / 换掉 100 / 逐字相同 127 / 官方没键 245」= 2026-10-14 快照；
//        「780 / 446 / 46 / 7」= 2026-10-19 快照）⛔ **别当现值引用**：
//      · 本表共 **750 条**（复算：`grep -c 'new Entry(' MyGame/Assets/CardPresentation/Core/Loc.cs`
//        现读 **752** —— 其中 **2 行是注释里引用的这个字样**（本行与下面那行复算说明），实际条目 = **750**；
//        逐条解析表行（`{ "键", … }` 那种，**跨行 / 用 `+` 拼接的也算一条**）正则看见 **749** —— 那条
//        **拼接写法**（`MainMenu/PurchasePremium/Description`）正则读不出，表里算 1 条）。
//        🔴 **2026-10-24 就地订正（铁律 5 · `A5` 现读 `w4_verify.py` 36/0）**：上面那行原来写「752 条 /
//        `grep` 754」—— 那是 **2026-10-23** 补 `Battle/MatchType/{FastMode,FastModeRanked}` 之后、**当天晚些**
//        `45dd42d4` **有意删掉 2 条自造文案**（`MenuDeck/GameMode/Skirmish` / `…/Classic`，值里带
//        「（Skirmish · 12 张）」那种）**之前**的读数 ⇒ 现读 **750**。⛔ 别再引用 752/754。
//      · 其中**官方表 `terms`（10,854 条）里有同名键的 447 条**、官方表**没有**这个键的 303 条；
//      · 🔴 **ZH 值与官方不同的：0 条**（2026-10-23 一次换完 **42** 条 · 见下面那段）；
//        **EN 值不同的 7 条**（本批按调度台口径**只碰中文值**，EN 未动 —— 清单在下面那段）。
//      · 复算口径：逐条取 `new Entry(` 前的键去
//        `数据/游戏数据/官方本地化_en+zh-CN.json` 的 `terms[键]`（`[0]`=en · `[1]`=zh-CN）比对；
//        ⚠️ 该表里 **391 条是「纯字符串」形态**（en == zh 的语言中立值，如语言名 / `VSync` /
//        `Unranked Classic`）⇒ 比对时按 **en = zh = 该串**。⛔ **别拿它当数组取 `[0]`/`[1]`** ——
//        那会读出「E / n」「V / S」这种**假差异**（2026-10-23 本批踩过一轮，差点当成「官方值坏了」）。
//        本表命中这种条目 **14 条**。
//      · 该表当前 = **10,854 条**（2026 build；见文件自己的 `_count`，⛔ 别再引用 2025-07-30 旧快照的 9,447）。
//
//    ✅ **那批「暂缓换回」（2026-10-14 记 33 条）2026-10-23 已全部消化。**
//      当时的判据 = **旧值在卡面 `descZh`/`nameZh` 里出现过**（脚本 `_tmp_a1070/loc_revert_clash.py`），
//      所以整族留着旧译、等卡面文字一起换。**调度台 2026-10-23 改口径**：
//      **一切以官方本地化表为准；官方表内部自相矛盾时照官方表原样显示，⛔ 不做「措辞统一」**
//      ⇒ 这一次把 **42 条 ZH 差异全换完**（`Card_Trait/*` 28 条 · `Armies/Sororitas` 也换：
//      战斗修女 → **修女会**；卡名 `Card_Name/SOR5` 官方本身就是「战斗修女」—— **两套词并存 = 原版如此**）。
//      ⚠️ **换完仍会看到的「两套说法」**：卡面 `descZh` 里**还有一部分卡是我们早期的自译**（不是官方值），
//      那些卡的正文仍印旧词（如「护甲」），而关键词章印官方词（「盔甲」）。那是**卡牌线 `descZh` 迁移**
//      的余额 —— 2026-10-23 现读 `cards_engine.json`（全池 1,127 张）：`护甲` 46 处 / `盔甲` 0 处 ·
//      `潜行` 13 / `隐身` 1 · `爆裂` 21 / `爆炸` 0 … ⛔ **不是本表的活**，
//      也⛔ **不能拿它当「本表该留旧译」的理由**（口径已改）。
//      ⚠️ **EN 列仍与官方不同的 5 条**（只碰中文值 ⇒ EN 未动；要不要改由下一批定）：
//      `Demo/ToBattle`（Battle → `Battle!`）· `MenuCollection/NoDecksFound`（官方多 `or game mode`）·
//      `MainMenu/PurchasePremium/Description`（官方少 `Starter Deck and`）·
//      `Settings/Graphics/FrameLimit`（`FPS limit` → `FPS Limit`·只是大小写）·
//      `MainMenu/Chat/ChallengeConfirmation`（官方带 `<b><color=#FCDEBB>…</color></b>`）。
//      🔴 **2026-10-24 就地订正（铁律 5）**：这里原来列 **7 条**，其中 `Battle/MatchType/Ranked`
//      （`Ranked` → `Classic`）与 `Battle/MatchType/Unranked`（→ `Unranked Classic`）**2026-10-23 已按官方表补齐**
//      ⇒ 从本清单销掉（现读 **5 条**；判据 = `官方本地化_en+zh-CN.json` 的 `terms` 逐键比对，
//      `w4_verify.py` ④「官方 Battle 键 156 条 EN 全一致」同批复核）。
//      逐条判据 → `资料/普查产出_1023c/官方中文全量收口_执行报告.md`。
//
//    ⚠️ **本文件里凡是还写着「原版中文在远端 I2 表 / 中文那一列是我们译的 / 本地没有中文表」的旧注，
//       一律以本条为准**（逐处已于同日加注，见各处 `2026-10-14`）。
//       🔴 **2026-10-14（第二十八会话 · `A1803`）就地订正上面那句「逐处已于同日加注」—— 不成立**：
//       本文件里**仍有约 35 处**旧注写着「值在远端 I2 表 / 本地取不到」（`grep -n "远端 I2\|远端 CCD"`
//       现读）。★ **那批不是「值取不到」，是「当时还没接」** —— 官方 zh-CN 表 2026-10-14 已在本地。
//       本会话（`A1803`）只按账**逐条接了 `Battle/` 那一族 + 关键词 tooltip + 6 条中文**，
//       **没有做全量注释扫**；其余各处**仍按文件头这一条理解**（⛔ 别照那些旧注当判据）。
//    ✅ **2026-10-17（界面线第三批 · `A1951`）已完成全量注释扫**：33 行旧注逐行订正
//       （`:2880` 那条**运行时日志**也改了 —— 它原来印「原版那套在远端 I2 表、84 个本地 bundle 里没有
//       `localization_assets_all.bundle`」）。现在 `grep -n "远端 I2\|远端 CCD"` 剩下的命中**全部是
//       订正注里引述的旧话**（`原写「…」`），不再有把旧话当结论的行。
//    ✅ **2026-10-14（`A1803`）本轮到底动了什么（现读口径，别抄别处的数）**：
//       · **`Battle/*` 补进 105 条**（官方有、我们表里没有 ⇒ 全按官方两列录入）；
//       · **EN 列换成官方值的 43 条**（`A1794` 说的那一批的另一半；含 `Tips/*` 13 ·
//         `Tips/Hud/*` 8 · `MainMenu/Settings/LanguageName/*` 9 —— 语言名下拉在英文档会印**本族语言名**
//         （`Español`/`Français`…，那就是原版的做法），⛔ 不再是枚举名）；
//       · **两列都换的 1 条**（`MenuShop/ExtraLegendaryWarning`）。
//       ⇒ 换完之后 `Loc` 里「官方有这个键」的 **382 条**（⚠️ **2026-10-14 当时的历史快照**；现值见文件头 →
//         现读 **446** 条）中，**只剩 33 条 ZH + 1 条 EN 与官方不同**，
//         而那 33 条**就是文件头说的那批「暂缓换回」**（`Card_Trait` 25 · `Armies` 4 · `Card_Race` 2
//         · `MenuDeck/HUD/DeckDescription/Minions`）+ 那条要改调用点的 `MenuDeck/CantImportDeck`。
//         判据脚本（可复算）：`工具/_loc_official/w4_verify.py`。
//    ⚠️ **两条中文没换、要另办**：
//       ✅ **② `MainMenu/PurchasePremium/Description` —— 2026-10-14（`A1803`）【已换】**（它确实是拼接形态，
//          但拼接只是**写法**：把官方那三段照写进去即可，见那条自己的注释）。本行原来写着「要单独处理」—— 已办。
//       ✅ **① `MenuDeck/CantImportDeck` —— 2026-10-17（界面线 `A1852`）【已换】**：两列换成官方原文（带 `{0}`），
//          调用点同批改成 `string.Format(Loc.T(键), CardText.Faction(wl.Faction))`（`Shell/DeckInfoPopup.cs`）。
//          `{0}` = **督军阵营名**，判据 = `decomp_full/DeckInfoPopup__EditDeck.c`。
//          ⚠️ 原文写「**仍未换（本轮也没换）**……官方文案带 `{0}` 而我们的调用点**不传参**」——
//          那句**已过期**（当时调用点不在白名单）；留痕（铁律 5），⛔ 别再照它当日志。

//
// ============================ 🔴 文案：12 项下拉 · 13 语言数据 ============================
// 用户 2026-09-28 拍板：**下拉照原版列 12 项**。
// 🔴 **2026-10-17（界面线 T08 `A1812`）起 12 项全部能真工作**：官方 I2 表的 **13 种真语言全列**
//   落进 `Resources/official_loc_13lang.json`（生成器 `工具/gen_official_loc_13lang.py`），
//   `T()` / `BulkOr` / `Lore` 按 `Current` **各取各的列**；中文/英文仍以手写 `Table` 为权威。
//   🆕 **2026-10-18（界面线 `N71`）**：中/英档在 `Table` + 数据面 bulk **都没命中**时，也从这份官方全表
//   **兜底**（`T()` 的 doc 里写着三档顺序）—— 只补「手写表里根本没有」的键，⛔ 不覆盖任何已有键的值。
//   `Turkish` 在官方表里有、但**不在 `AvailableLanguages`**（原版枚举就 12 项）⇒ 只当数据，不进下拉。
//   只有「官方表里没有这条键」（我们自拟的那批）或资源文件读不到时才**回退英文**并 `Debug.Log` 出声
//   （⛔ **不许在 UI 上加原版没有的提示行** —— 出声只走日志）。见 `T()` 与 `Effective`。
//
// ============================ 🔴 落盘 ============================
//  · 键名 = **`"Language"`**（用户口径）；风格照 `Core/WarpforgeAudio.cs` 的 `MusicPrefKey`（工程里唯一的原版键先例）。
//  · 读/写都 `PlayerPrefs.Save()`（同 `WarpforgeAudio`）。
//  · **默认 = 中文**（用户 2026-09-28 口径：原版实拍是中文客户端、用户是中文用户）。
//  · ⚠️ 原版**不存 `PlayerPrefs`** —— 它存玩家存档（服务器那一侧），我们没有存档系统 ⇒
//    这是**我们挑的**（同 `AutoZoom` / `SuperSampling` 那两条如实标注）。
//
// ============================ 忘了接的地方不会静默 ============================
//  · `T()` 拿到**表里没有**的键 ⇒ 返回**键名本身** + `Debug.LogWarning`，并把 `MissingCount` / `LastMissingKey` 记下来（自检有一条钉它）。
//    ⚠️ **2026-10-14（第二十九会话 · `A1924`）更正**：原来这里补了一句「（键名会直接显示在界面上 = 看得见）」——
//    **只对【直接吃返回值】的调用点成立**；`CardText` 里那几族走的是「先 `HasEntry`、缺键时调一次 `T` 只为出声/记账、
//    然后回自己那条兜底」（`TermOr` `Core/CardText.cs:547-553` · `KeywordZh`/`KeywordDisplay`/`KeywordEn` 同文件
//    `:112`/`:125`/`:140`）⇒ **界面照旧印兜底**（卡名/阵营/兵种/关键词那几族都在此列）⇒ 「出声」是**无条件**的，
//    「界面印什么」**按调用点分**。详见 `T()` 的 doc。
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

        /// <summary>这个语言**有没有自己的文案**（= `T()` 能取到不是英文的文本）。
        /// <para>🆕 **2026-10-17（界面线 · T08 `A1812`）**：官方 I2 表的 **13 种真语言全列**已落进
        /// `Resources/official_loc_13lang.json` ⇒ 下拉那 12 项**全部**为 true（原来只有中/英两项）。
        /// 只有「资源文件读不到」或「这个值不在官方表里」（如 `Turkish`，它不在枚举中）才为 false ——
        /// 那种时候 `T()` 仍按老规矩回退英文并出声。</para></summary>
        public static bool HasOwnText(AvailableLanguages l)
        {
            if (l == AvailableLanguages.Chinese || l == AvailableLanguages.English) return true;
            if (Lang13Column(l) < 0) return false;
            LoadLang13();
            return _lang13Idx != null && _lang13Idx.Count > 0;
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
            //   ⚠️ 那四颗 TMP 的 `m_text` 就是**西班牙语**（`Gráficos` / `Multimedia` —— 本包 prefab 被本地化成西语了）。
            //   🔴 **2026-10-17（`A1951`）就地订正（铁律 5）**：这里原来写「英文在**远端 I2 表** ⇒ EN/ZH 都自拟」
            //     并留了一段「`Audio` 还是 `Media` 需拍板」—— **两句都过期了**：官方 en/zh 表 2026-10-14 已解出
            //     （`数据/游戏数据/官方本地化_en+zh-CN.json`），`Settings/Graphics/Title` = `Graphics`/`图形`、
            //     `Settings/Media/Title` = `Media`/`媒体`，**下面两行的两列已与官方逐字相同** ⇒ 无需再拍板。
            { "Settings/Graphics/Title",              new Entry("图形", "Graphics") },
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
            // 🔴 **2026-10-17（`A1951`）就地订正**：原来这里写「中文那一列是我们自己译的（原版中文在远端 I2 表里、
            //   本地 84 个 bundle 无本地化包）」—— 官方 en/zh 表 2026-10-14 已在本机
            //   （`数据/游戏数据/官方本地化_en+zh-CN.json`）；本块**在官方表里的那些键**
            //   （`Settings/Account/*` · `MainMenu/Login/*`）两列已与官方逐字相同。
            // ⚠️ 五条社交那条的**英文列没有原版出处**：原版那五颗 `Button Text` 的 `m_text` 是**空串**
            //   （`m_IsActive = 0`，只为无障碍留的）⇒ EN 取**节点名那一档**（`Discord`/`Instagram`/…），
            //   与 `Shell/SettingsWindow.cs` 的 `AcSocEn` **同一份口径**（⛔ 那一份别另外再写一遍）。
            { "Settings/Account/Title",                       new Entry("账户",         "Account") },
            { "MainMenu/Login/Email",                         new Entry("电子邮件",     "E-mail") },
            { "MainMenu/Login/Password",                      new Entry("密码",         "Password") },
            { "Settings/Account/ResetPassword",               new Entry("重置密码",     "Reset Password") },
            { "Settings/Account/ForgotPassword",              new Entry("忘记密码",     "Forgot Password") },
            { "Settings/Account/SubscribeToTheNewsletter",    new Entry("订阅新闻通讯？", "Subscribe to the Newsletter?") },
            { "MainMenu/Login/SignInButton",                  new Entry("登录",         "Log in") },
            { "MainMenu/Login/Register",                      new Entry("注册",         "Register") },
            // 🔴 键名**中间真有一个空格**（原版 `mTerm` 原文就是 `Settings/Account/Switch Account`）——
            //    逐字照抄，⛔ 别「顺手」改成 `SwitchAccount`（改了就取不到词条、界面上印键名）。
            { "Settings/Account/Switch Account",              new Entry("切换账户",     "Switch Account") },
            { "Settings/Account/Logout",                      new Entry("登出",         "Log Out") },
            { "MainMenu/Settings/ButtonLabel/DeleteAccount",  new Entry("删除账户",     "Delete account") },
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
            { "Settings/Support/FAQText",          new Entry("对游戏有疑问？请访问常见问题解答",
                                                               "Questions about the game? Visit the Frequent Asked Questions") },
            { "Settings/Support/FAQButton",        new Entry("常见问题",   "FAQ") },
            { "Settings/Support/MiddleText",       new Entry("您需要我们的帮助吗？", "Do you need help from us?") },
            { "Settings/Support/ContactButton",    new Entry("联系",   "Contact") },
            // ⚠️ 英文列里那个换行是**原版 `m_text` 里的真换行**（照抄），不是我们排的版。
            { "Settings/Support/ContactText",      new Entry("您也可以通过support@everguild.com与我们联系 我们将尽力帮助您！",
                                                               "You can also contact us at support@everguild.com\nWe'll do our best to help you!") },
            { "Settings/Support/TermsOfService",   new Entry("服务条款",   "Terms of Service") },
            { "Settings/Support/FAQTextMobile",    new Entry("对游戏有疑问？查看常见问题或联系客服",
                                                               "Questions about the game? Check out the Frequent Asked Questions or contact Support") },
            // ⚠️ 这条**不在** `Settings/Support/` 前缀下（原版就那么写的）：底部两颗链里 `Privacy Policy`
            //    那颗 `Button Text` 挂的是 `MainMenu/Settings/ButtonLabel/PrivacyPolicy`，而
            //    `Terms of Service` 那颗挂的是 `Settings/Support/TermsOfService`（两条前缀不同，⛔ 别统一）。
            { "MainMenu/Settings/ButtonLabel/PrivacyPolicy", new Entry("隐私政策", "Privacy Policy") },

            // ---- 语言名（下拉的 12 项）----
            // 🔴 键 = `MainMenu/Settings/LanguageName/<枚举名>`（原版 `ResetLanguagesDropdown` 的拼法，
            //    前缀字面量已在 `global-metadata.dat` 核到）。英文那一列 = **本族语言名**（`Español`/`Français`…）。
            // 🔴 **2026-10-17（`A1951`）就地订正**：原写「本地 TMP 拿不到正式选项名 —— 远端 I2 表；中文那一列是
            //    我们译的」—— 官方表 2026-10-14 已在本机，本族共 **13 条**（含 `Turkish`，不进 12 项下拉）。
            //    ✅ **2026-10-17（`A1812` · `N36`）已对齐官方键名**：官方的两条是 `…/Portuguese (Brazil)`
            //    与 `…/Simplified Chinese`（值分别是 `Português` / `中文`，**13 列逐字相同**）——
            //    我们这两条原来写成 `…/Portuguese`（`Portuguese` / `葡萄牙语`）与 `…/Chinese`（`Chinese` / `中文`），
            //    现已改成官方键名 + 官方值（`KeyOf` 跟着改；其余 11 条与官方逐字相同）。
            { "MainMenu/Settings/LanguageName/English",    new Entry("English",     "English") },
            { "MainMenu/Settings/LanguageName/Spanish",    new Entry("Español", "Español") },
            { "MainMenu/Settings/LanguageName/French",     new Entry("Français",     "Français") },
            { "MainMenu/Settings/LanguageName/German",     new Entry("Deutsch",     "Deutsch") },
            { "MainMenu/Settings/LanguageName/Portuguese (Brazil)", new Entry("Português", "Português") },
            { "MainMenu/Settings/LanguageName/Russian",    new Entry("Русский",     "Русский") },
            { "MainMenu/Settings/LanguageName/Italian",    new Entry("Italiano", "Italiano") },
            { "MainMenu/Settings/LanguageName/Korean",     new Entry("한국어",     "한국어") },
            { "MainMenu/Settings/LanguageName/Japanese",   new Entry("日本語",     "日本語") },
            { "MainMenu/Settings/LanguageName/Czech",      new Entry("Čeština",   "Čeština") },
            { "MainMenu/Settings/LanguageName/Polish",     new Entry("Polski",   "Polski") },
            { "MainMenu/Settings/LanguageName/Simplified Chinese", new Entry("中文", "中文") },

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
            // 🔴 **2026-10-18（第五十一会话 · 真机抽查抓到的回归）**：商店左栏页签在 `N59` 改成官方键后，
            //   `BoosterPacks` / `Gold` **没登记进本表** ⇒ `Loc.T` 缺键时把**键名**印在页签上
            //   （真机实测：`MENUSHOP…BOOSTERPACKS` / `MENUSHOP…GOLD`，只有 `Cosmetics` 是中文）。
            //   值逐字照官方表（`数据/游戏数据/官方本地化_en+zh-CN.json`：
            //   `BoosterPacks = Booster Packs / 增强包` · `Gold = Gold / 金币`）。
            { "MenuShop/ShopItemType/BoosterPacks",           new Entry("增强包",   "Booster Packs") },
            { "MenuShop/ShopItemType/BoosterPack",            new Entry("增强群体", "Booster Pack") },
            { "MenuShop/ShopItemType/Gold",                   new Entry("金币",     "Gold") },
            // 🔴 **2026-10-18（第五十一会话 · 真机抽查）**：FTUE「选军队」窗的 `Selected Text`
            //   预制体上挂着 `Localize.mTerm = MainMenu/General/Selected`（MB `1188082097155858280`），
            //   而我们的标签当时用的是字面量 `"Selected"` ⇒ 中文档下印英文。
            //   值逐字照官方表（`Selected / 已选中`）。
            { "MainMenu/General/Selected",                    new Entry("已选中",   "Selected") },
            // 🔴 **2026-10-18（第五十一会话 · 真机抽查「中英混排」）**：奖励/布道所页的标题与三条日常
            //   在中文档里全是英文（`Daily Missions` / `Weekly Challenge` / `Daily Login Bonus` /
            //   `0 Disponible` / `Deal 45 damage…`）。这些**官方表里都有键**，只是没进本表 ⇒ `Loc.T` 拿不到。
            //   值逐字照 `数据/游戏数据/官方本地化_en+zh-CN.json`（括号里是官方英文原文）。
            { "Missions/Title/LoginBonus",                    new Entry("每日奖励",     "Daily Bonus") },
            { "Missions/Title/DailyMissions",                 new Entry("每日布道所",   "Daily Missions") },
            { "Missions/Title/DailySkulls",                   new Entry("每日骷髅头",   "Daily Skulls") },
            { "Missions/Title/WeeklyChallenge",               new Entry("每周挑战",     "Weekly Challenge") },
            { "Missions/RefillCount",                         new Entry("{0} 可用",     "{0} Available") },
            { "Missions/CollectCounter",                      new Entry("准备在 {0} 收集", "Ready to collect in {0}") },
            { "Missions/ActiveCounter",                       new Entry("在 {0} 结束",  "Ends in {0}") },
            { "Missions/Daily_5",                             new Entry("对敌方战将造成 {0} 点伤害", "Deal {0} damage to the enemy Warlord") },
            { "Missions/Daily_Generic_8",                     new Entry("攻击单位 {0} 次", "Attack with units {0} times") },
            { "Missions/Daily_10",                            new Entry("[[ranged]] 攻击 {0} 次", "Attack [[ranged]] {0} times") },
            // 🔴 **2026-10-18（同批普查：这些键只在官方全表里、`Table`/bulk 都取不到 ⇒ 界面会印键名）**
            { "Demo/FriendsMenu/SearchPlayer",                new Entry("搜索玩家", "Search player") },
            { "SocialMenu/Alliances/Invitations",             new Entry("联盟邀请：", "Alliances invitations:") },
            { "Demo/Feedback/Thanks",                         new Entry("您的意见对我们非常重要。感谢您的反馈！", "Your opinion is really important to us. Thanks for your feedback!") },
            // 🔴 **2026-10-18**：官方标记 `[[ranged]]` / `[[melee]]` 在卡面上是**图标**（无词条）；
            //   任务行那两条要用「词」⇒ 补两条概念词（与 `Battle/Tips/RangeAttack`「远程攻击」同一族口径）。
            { "Card_Trait/ranged",                            new Entry("远程", "Ranged") },
            { "Card_Trait/melee",                             new Entry("近战", "Melee") },
            // 🔴 **2026-10-18**：奖励窗左栏四颗页签（原版 TMP 装成 `MISSIONS/CAMPAIGN/FORGE/DAILY STREAK`）
            //   —— 官方表都有键，之前没用 ⇒ 中文档印英文。值逐字照官方表。
            { "Rewards Menu/Missions",                        new Entry("布道所", "Missions") },
            { "MainMenu/General/Campaign",                    new Entry("活动", "Campaign") },
            { "MainMenu/General/Forge",                       new Entry("锻造厂", "Forge") },
            { "Rewards Menu/DailyStreak/Title",               new Entry("每日连胜", "Daily Streak") },
            // ------------- D4 左栏底部钮（`…/Sidebar/Footer/Done/Button Text`，那颗上面**两颗** `Localize`，
            //   本条取**第一颗** `MenuDeck/MenuButtons/Done`；第二颗 `MenuLogin/Login/DoneButton` 是登录页共用的）-------------
            { "MenuDeck/MenuButtons/Done",                    new Entry("完成",     "Done") },

            // ============================================================ 🆕 **2026-10-11（界面复刻·主菜单）：
            //   左竖导航那五颗的文案。**
            // 键名 = 原版那颗 `Localize.mTerm` 的**原文**（只读判据代理 2026-10-11 现读
            //   `bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/` 的 MB2500/2564/2563/2562/2565 逐颗实读）：
            //   `MainMenu/NavigationPanel/**HomeButton**` · `…/Collection` · `…/Shop` · `…/Rewards` ·
            //   `…/**Friends**`（⚠️ **第五颗的键名是 `Friends`、不是 `Social`** —— 屏上英文才是 `SOCIAL`）
            // 英文列 = 那五颗 TMP 自己的 `m_text`（`PLAY`/`COLLECTION`/`SHOP`/`REWARDS`/`SOCIAL`）。
            // 🔴 **中文列的出处分两档，别混**：
            //   · `游玩` / `收藏` = **官方词条表** `d:/4/Unity/数据/本地化/i18n/zh_CN.csv:2,3`
            //     （`PLAY,~,游玩` / `COLLECTION,~,收藏`）—— 这是**第一档判据**。
            //   · `商店` / `奖励` / `社交` = **本地词条表里没有这三条**（全表 6424 行已 grep，零命中）
            //     ⇒ 取**用户给的原版客户端实拍** `C:/Users/qjh36/Desktop/warpforge参考图/主界面参考图.png`
            //     左竖导航上逐字读到的三个词（仓库副本 `资料/原版参照图/用户实拍_1017/` 里**没有**这张，
            //     它是 2026-10-10 才加进去的）—— 这是**第二档判据**，已在卡片行标注。
            // 消费侧 = `Shell/MainMenuRuntime.cs` 的 `NavButton`（原来硬编码英文）。
            // ------------- E1 主菜单左竖导航五颗 -------------
            { "MainMenu/NavigationPanel/HomeButton",          new Entry("玩",     "PLAY") },        // 中文 = zh_CN.csv:2
            { "MainMenu/NavigationPanel/Collection",          new Entry("收藏",     "COLLECTION") },  // 中文 = zh_CN.csv:3
            { "MainMenu/NavigationPanel/Shop",                new Entry("商店",     "SHOP") },        // 中文 = 用户实拍（表里没有）
            { "MainMenu/NavigationPanel/Rewards",             new Entry("奖励",     "REWARDS") },     // 中文 = 用户实拍（表里没有）
            { "MainMenu/NavigationPanel/Friends",             new Entry("社交",     "SOCIAL") },      // 中文 = 用户实拍（表里没有）

            // ============================================================ 🆕 **2026-10-11（界面复刻·模式卡标题）**
            // 🔴 **先说清这五条不是「原版词条」**：原版模式卡那颗 `Event Title` 的字是
            //   `LiveOpsAssetUtility.GetLabel(event)` 灌的（= **liveop 事件自己的 label**，服务端数据），
            //   本地**查不到**（`资料/主菜单_原版规格.md` §9·3）。⇒ 我们这五张卡的标题本来就是**自定的**。
            // 中文列按本仓既定优先级取（实拍 > `zh_CN.csv` 精确英文键 > 自译并如实标注）：
            //   · **三个有实拍出处**：**用户给的原版客户端实拍**
            //     `C:/Users/qjh36/Desktop/warpforge参考图/主界面参考图.png` 上逐字读到的三个
            //     （教学 / 练习 / 小冲突）。✅ **用户 2026-10-11 拍板**：「把那三个改成中文」。
            //   · **另两张（Draft / Ranked）实拍上读不到** ⇒ 退第二档、**精确英文键命中**
            //     `d:/4/Unity/数据/本地化/i18n/zh_CN.csv`：**第 19 行 `Draft,~,选秀`** ·
            //     **第 149 行 `Ranked,~,排位`** —— **不是我们自译的**。
            // ⚠️ **2026-10-13 就地更正（铁律 5）**：这一节原来写「另外两张（Draft / Ranked）参考图上没有
            //   ⇒ ⛔ **不自己译**，调用点直接传英文原文（`"DRAFT MODE"`/`"RANKED"`），**故意不在这里留空条目**」
            //   —— **那个理由已经不成立**（用户 2026-10-13 明确要求「模式卡需要中文」，
            //   且中文出处已按上面第二档查到）⇒ 那两条已补进本表，调用点改走 `Loc.T`。
            // 消费侧 = `Shell/MainMenuRuntime.cs` 的 `BuildGameModes` 五个调用点。
            // ------------- E2 模式卡标题（五张全走 `Loc.T`）------------
            { "MainMenu/GameModes/Tutorial",                  new Entry("教学",     "TUTORIAL") },    // 中文 = 参考图「教学」
            { "MainMenu/GameModes/Practice",                  new Entry("练习",     "PRACTICE") },    // 中文 = 参考图「练习」
            { "MainMenu/GameModes/Skirmish",                  new Entry("小冲突",   "SKIRMISH") },    // 中文 = 参考图「小冲突」
            { "MainMenu/GameModes/Draft",                     new Entry("选秀",     "DRAFT MODE") },  // 中文 = zh_CN.csv:19（`Draft,~,选秀`）
            { "MainMenu/GameModes/Ranked",                    new Entry("排位",     "RANKED") },      // 中文 = zh_CN.csv:149（`Ranked,~,排位`）
            // 🆕 **2026-10-13（`A1755`）**：`选秀`那张卡点下去的那句提示。
            // 🔴 **这一条【没有原版出处】**：原版 `Draft Game Mode Container 1x2` 点下去开哪扇窗由 **liveop 事件数据**给
            //   （`资料/主菜单_原版规格.md` §9·3 本地 `?`），而本地既没有服务器也没有活动数据 ⇒ 照项目边界③
            //   「**入口照做、点了如实说「暂无服务器」**」——**这句话本身是我们写的**。
            //   原来调用点直接把英文串写死在代码里（`"Draft mode is not available offline."`）⇒ **中文档下玩家看到英文**
            //   （用户 2026-10-13 点名「模式卡需要中文」的同一条线上）。⇒ 收进本表，两边都走 `Loc.T`。
            //   EN = 我们原来那串（逐字保留，英文档行为不变）· ZH = **我们自译**（⛔ 不是原版词条）。
            //   钮上那两个字走已有的 `MainMenu/General/OK`（中文「确定」）—— ⛔ 别在这里再立一条。
            { "MainMenu/GameModes/DraftOffline",              new Entry("选秀模式在离线版里不可用。", "Draft mode is not available offline.") },

            // ============================================================ 🆕 **2026-10-19（第十七会话 · `A1498`）：
            //   收藏窗（`Collection Menu Variant`）左栏四颗页签的文案。**
            //
            // 键名 = 原版那四颗 `TabButtonLabel` 上 `Localize.mTerm` 的**原文**（本轮现读
            //   `bundle_menus_assets_all`，四颗都是 `…/Tab Buttons/{Deck,Cards,CardBacks,Alternate Art}/Label/TabButtonLabel`；
            //   逐颗的 MB 出处写在每行括号里）；英文列 = 那四颗 TMP 的 `m_text` **原文逐字符照抄**
            //   （含第一颗的**单数** `Deck` —— 它不是 `Decks`，屏上是 `m_fontStyle = 16`(UpperCase) 画出来的 `DECK`）。
            // 🔴 中文列 = **原版中文客户端实拍**（**不是我们译的**）：用户 2026-10-10 的截图
            //   `C:/Users/qjh36/Desktop/warpforge参考图/收藏—卡组的参考图.png`
            //   左栏自上而下逐字读到 **卡组 / 张牌 / 美容品 / 风格** 四个词（本行即那四个）。
            //   ⇒ 与 `d:/4/Unity/数据/本地化/i18n/zh_CN.csv` 的通用词条**互证**：该表 `Deck`→`卡组`、`Styles`→`风格`；
            //     而 `Cards` 那一栏该表给的是「卡牌」、`Cosmetics` 是「装饰」——
            //     ★ **按实拍取「张牌 / 美容品」**（实拍是第一档判据，见文件头的判据优先级）。
            // ⚠️ **`Cards` / `Cosmetics` 两条【不再重复添加】** —— 原版这两颗页签挂的就是
            //   `MenuShop/ShopItemType/*`（**与卡组编辑窗左栏那两颗共用同一条词条**），
            //   表里已有（本文件 D3 那一节，中文取自用户 2026-10-17 的卡组编辑实拍）
            //   ⇒ ⛔ 别为了「看着整齐」另起一条 —— 两条同义键 = 迟早不一致。
            // 消费侧 = `Shell/CollectionWindow.cs` 的 `Buttons` 表（新增的 `TabBtnSpec.Term` 那一格）
            //   经 `Shell/MenuWindowBase.cs` 的 `BuildTabButton` 取词（全族唯一一份取词口）。
            // ------------- F1 收藏窗左栏四颗页签 -------------
            { "MainMenu/MainButtons/ButtonLabel/Deck",        new Entry("卡组",     "Deck") },        // TMP 'Deck'（MB 6937732152595899093）· 中文 = 实拍
            // `MenuShop/ShopItemType/Cards` / `…/Cosmetics` —— **见本文件 D3 那一节**（共用，不重复）
            { "MenuShop/AlternateArtStyle",                   new Entry("风格",     "Styles") },      // TMP 'Styles'（MB 8988648794445374165）· 中文 = 实拍

            // ============================================================ 🆕 **2026-10-19（第十七会话 · `A1501`）：
            //   练习窗（`Practice Mode Menu` = `PracticeModePopup`）那六段字的词条。**
            //
            // 键名 = 原版那六颗 TMP 上 `Localize.mTerm` 的**原文**（本轮现读：把 `Practice Mode Menu` 整棵
            //   129 个节点走了一遍、逐颗打印 `mTerm` 与 `m_text`；节点路径写在每行后面）。
            // 英文列 = 同一次实读的那颗 TMP 的 `m_text` **原文逐字符照抄**。
            // 🔴 **2026-10-17（`A1951`）就地订正**：原写「中文列不是实拍（原版中文在远端 I2 表里）⇒ 取
            //   `zh_CN.csv` 的精确英文键」—— 官方 en/zh 表 2026-10-14 已在本机：
            //   `Demo/ToBattle` · `Demo/DeckSelectionDemo/SelectDeck` · `Demo/CardCostBalance` ·
            //   `MainMenu/RankedWindow/ChangeDeck` **四条两列已与官方逐字相同**
            //   （`Battle`/`战斗` · `Select deck to play`/`选择要玩的卡组` · `Card / Energy cost`/`卡/能量成本` ·
            //    `Change Deck`/`更换卡组`）。
            //   ⚠️ 只有 `Event_Description/Generic/GameMode` **不在官方表快照里** ⇒ 那一条（`游戏模式`/`Game mode`）
            //   仍是自拟。原「`开战！` 还是 `战斗！`」那句讨论随本订正一并作废。
            // ⚠️ `Back` 那一颗**已有**（`MainMenu/MainButtons/ButtonLabel/Back`，中文 `返回`）—— 见本文件
            //   `MainMenu/MainButtons/` 那一族，⛔ 不重复添加。
            // 消费侧 = `Shell/PracticeModePopup.cs` 的六处 `Txt(...)` 调用点。
            // ------------- F2 练习窗 -------------
            // `Back button ▸ Text`（TMP 'Back'）→ 见上面的 `MainMenu/MainButtons/ButtonLabel/Back`（共用）
            { "Demo/ToBattle",                                new Entry("战斗！",         "Battle") },              // `DeckSelectionContinueButton ▸ Text`
            { "Demo/DeckSelectionDemo/SelectDeck",            new Entry("选择要玩的卡组", "Select deck to play") },   // `Deck Selector ▸ tooltip`
            { "Demo/CardCostBalance",                         new Entry("卡/能量成本", "Card / Energy cost") },     // `Deck info ▸ General container ▸ Deck Information Cost/balance text`
            { "MainMenu/RankedWindow/ChangeDeck",             new Entry("更换卡组",       "Change Deck") },           // `… ▸ General container ▸ Generic Simplified UI Button ▸ Button Text`
            { "Event_Description/Generic/GameMode",           new Entry("游戏模式",       "Game mode") },            // `Toggle ▸ Label`

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
            // ⚠️ **英文那一列【不是】原版 prefab 的原文**：本键那颗 TMP
            //   （`MonoBehaviour_925819347406796159.json`）`m_text = ''`（**F2 实读**，
            //   与 `Shell/DeckSelectionPopup.cs` 那句「取不到文案」一致）⇒ prefab 里没有这个值。
            //   🔴 **2026-10-17（`A1951`）就地订正**：原写「原版那套在远端 I2 表里 ⇒ 本地没有英文原文、
            //   将来拿到表先改这一列」—— 官方表 2026-10-14 已在本机，
            //   `MenuDeck/Tip/SelectDeckAgainst` = `Select a deck to play against` / `选择一个卡组进行对战`，
            //   **与本行两列逐字相同** ⇒ 这一列现在就是官方值（不是我们拟的）。
            //   `Select deck` 的**同窗旁证** = 兄弟节点 `Instructions`（键 `MenuDeck/Tip/SelectDeck`，
            //   那颗 TMP 的 `m_text = 'Select deck'`）—— 仍只是旁证。
            // 消费侧 = `Shell/DeckSelectionPopup.cs` 的 `TitleTerm`（`:170`）+ 建那一格时取词（`:381`）；
            //   断言 → `Editor/CollectionScene.cs` 的 D6 那一节（两语档各断一个**字面量**）。
            // 🧨 改坏法：把这条删掉 ⇒ 界面上印键名、那条断言立刻红（`Loc.T` 返回键名）。
            { "MenuDeck/Tip/SelectDeckAgainst",               new Entry("选择一个卡组进行对战", "Select a deck to play against") },
            // ------------- 🆕 2026-10-17（T24）：选卡组弹窗信息窗里那颗**选定钮** -------------
            // 节点 = `Deck info Popup ▸ … ▸ Select Deck`（`DeckInfoControls` 的 `SelectButton`）。
            // 键名 = prefab `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_9087721988209816959.json`
            //   的 `selectString.mTerm = "MenuDeck/Tip/SelectDeck"`（写手 F 2026-10-17 实读）。
            // 正文 = **官方本地化表** `Unity/数据/游戏数据/官方本地化_en+zh-CN.json`
            //   → `terms["MenuDeck/Tip/SelectDeck"] = ["Select deck", "选择卡组"]`（EN / ZH 都非空）。
            // 消费侧 = `Shell/DeckInfoPopup.cs` 的 `SelectButtonText`（T24 新增）；缺这条时按钮会退成英文原值 + 出声。
            { "MenuDeck/Tip/SelectDeck",                      new Entry("选择卡组", "Select deck") },

            // ------------- 🆕 2026-10-17（B11 · A884）：导入窗（`Import Deck Popup`）的**占位符** -------------
            // 节点 = `Import Deck Popup/Window/Input Field/Text Area/Placeholder` —— 键名 = 那颗 `Localize`
            //   的 `mTerm` 原文（`bundle_menus_assets_all/MonoBehaviour_6085227748672536722.json` 实读；
            //   🔴 全库**只此一颗**用这个键：`grep -rl` 扫 24.7 万文件命中 1）。
            // 英文那一列 = **那颗 TMP 的 `m_text` 原文逐字符照抄**（含末尾那三个点）。
            // 🔴 **2026-10-17（`A1951`）就地订正**：原写「中文那一列不是实拍（原版中文在远端 I2 表里；本地
            //   84 个 bundle + 24.7 万文件扫中文串零命中）⇒ 取我们自己那份译表 `zh_CN.csv:102`」——
            //   官方表 2026-10-14 已在本机，`MenuDeck/HUD/EnterText` = `Enter text...` / `输入文本...`，
            //   **与本行两列逐字相同**（原 `zh_CN.csv:102` 的说法作废）。
            // 消费侧 = `Shell/ImportDeckPopup.cs` 的 `PlaceholderTerm` + `RefreshInputText`；
            //   断言 → `Editor/ShellScene.cs` 的 B11/A884 那一节（两语档各断一个**字面量**）。
            // ⚠️ 兄弟节点 `Main Search message` 挂的是**另一条**词条 `MenuDeck/Share/PasteDeck`
            //   （`MonoBehaviour_8528767437303251090.json`）—— **那条见下面它自己的条目**；
            //   同窗那颗 `Error msg` **一颗 `Localize` 都没有**（原版本来就没有词条 ⇒ **永远不接**，
            //   属于「判据是空的」那一档，⛔ 别替它编一条）。
            //   🔴 **2026-10-17（A891）消费点全接上了**：
            //   · `PasteDeck` / `EnterText` / `Confirm` 在 **`Shell/ImportDeckPopup.cs`** 的导入窗
            //     （`TitleTerm` / `PlaceholderTerm` / `ConfirmTerm`，各自那颗常量上写着判据）。
            //   · ⚠️ **2026-10-17 就地订正（铁律 5）**：原文还列了 `Deck/DeckRuntime.cs` 的导入窗
            //     （`imp_title` / `imp_input`）—— 那棵树是**只给自检用的重复实现**，当天已整段删除（`T23`）
            //     ⇒ 现在**只剩 `ImportDeckPopup.cs` 一处消费**。
            //   ⚠️ 唯一**仍然没接**的是那颗 `Error msg`：原版**根本没给它词条**（引擎按根脚本的三个 term 字段
            //     写进去的）⇒ 我们那行错误走自己的文案（`CollectionData.ImportDeck`），**这不缺东西、别去接**。
            { "MenuDeck/HUD/EnterText",                       new Entry("输入文本...", "Enter text...") },

            // ------------- 🆕 **2026-10-17（A891）：导入窗的【标题】词条** -------------
            // 节点 = `Import Deck Popup/Window/**Main Search message**`（rect `610,280 700×60` ——
            //   与 `DeckRuntime.BuildImportPopup` 里 `imp_title` 那行实参逐位对上 ⇒ 同一颗）。
            // 键名 = 那颗 `Localize.mTerm` 的**原文** `MenuDeck/Share/PasteDeck`
            //   （`d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/
            //     MonoBehaviour_8528767437303251090.json` 实读 `mTerm`；同文件里它的兄弟 `Error msg` 没有 `Localize`）。
            // 英文那一列 = 同 prefab 里那颗 TMP 的 `m_text` **原文**（`menu_dump` 实读：`Main Search message 'Paste your deck'`）。
            // 🔴 **2026-10-17（`A1951`）就地订正**：原写「中文那一列不是实拍（原版中文在远端 I2 表）⇒ 取
            //   `zh_CN.csv:143`」—— 官方表 2026-10-14 已在本机，
            //   `MenuDeck/Share/PasteDeck` = `Paste your deck` / `粘贴您的卡组`，**与本行两列逐字相同**。
            // 消费侧 = `Shell/ImportDeckPopup.cs` 的 `TitleTerm`。
            //   ⚠️ **2026-10-17 就地订正（`T23`）**：原文写「消费侧 = `DeckRuntime` 的 `imp_title`」——
            //     那棵树是只给自检用的重复实现、当天已删，现只剩壳侧这一处。
            { "MenuDeck/Share/PasteDeck",                     new Entry("粘贴您的卡组", "Paste your deck") },

            // ------------- 🆕 **2026-10-19（`A1050`）：分享成功那条 toast 的词条** -------------
            // 键名 = **原版字符串常量表实读**（**不是自拟**）：`d:/2/tools/il2cpp_out/stringliteral.json:98759`
            //   的 `"MenuDeck/Share/ExportSuccesful"`（`address` `0x42D07E0`）；同族三条
            //   `MenuDeck/Share/{ShareOnAlliance,ShareOnGlobal,Title}` **紧邻**（`0x42D08E0/0x42D09E0/0x42D0AE0`），
            //   这一条就是它们四个里的第一个。
            //   🔴 **原版把 `Successful` 拼成 `Succesful`（一个 `s`）—— 一个字都别改**：
            //      改了就等于换了一条键，永远查不到（那一族的地址是连号的、拼写照抄才自洽）。
            // 🔴 **2026-10-17（`A1951`）就地订正**：原写「这一条的『值』在远端 I2 语言表里、本地【没有】
            //   ⇒ 两列文案都是我们自拟的」—— 官方表 2026-10-14 已在本机，
            //   `MenuDeck/Share/ExportSuccesful` = `Deck copied to clipboard` / `卡组已复制到剪贴板`，
            //   **与本行两列逐字相同** ⇒ 不是自拟。
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
            { "MenuDeck/Share/ExportSuccesful",               new Entry("卡组已复制到剪贴板", "Deck copied to clipboard") },

            // ------------- 🆕 **2026-10-12（`A1045`）：`Share On Chat` 那扇两键面板的三条词条** -------------
            // 键名 = **原版字符串常量表实读**（**不是自拟**）：`d:/2/tools/il2cpp_out/stringliteral.json` 的
            //   `MenuDeck/Share/{ShareOnAlliance,ShareOnGlobal,Title}`，地址 `0x42D08E0 / 0x42D09E0 / 0x42D0AE0`
            //   —— 与上面那条 `ExportSuccesful`（`0x42D07E0`）**连号**（同一族四条，见它那条注释）。
            // 🔴 **哪条键挂在哪颗钮上 = 反编译实读**（`d:/2/tools/decomp_full/DeckInfoPopup__ShareDeckOnChat.c`）：
            //   `DAT_1842d08e0 = ShareOnAlliance` **先建** ⇒ **第 0 颗 = 群组**；
            //   `DAT_1842d09e0 = ShareOnGlobal` **后建** ⇒ **第 1 颗 = 全局**；
            //   `DAT_1842d0ae0 = Title` 走 `Context___ctor` 的第 3 个实参 = 面板标题。⛔ 别把前两条对调
            //   （对调之后界面文案仍「看着像对的」，只有拿原版那一份逐位比才看得出来）。
            // 🔴 **2026-10-17（`A1951`）就地订正**：原写「两列文案都是我们自拟的（原版那套值在远端 I2
            //   语言表）」—— 官方表已在本机，这**三条**（`Title` / `ShareOnAlliance` / `ShareOnGlobal`）
            //   两列与官方逐字相同。
            //   ⚠️ 而且这一族的**英文列拿不到「原版 TMP 原文」**：那扇面板的标题在 prefab 里是**出厂占位串**
            //   `Pepito el de siempre`、两颗钮的字是**模板串** `Template`（`Shell/GenericOptionsPanel.cs` 的
            //   `DefTitle` / `TemplateText`）⇒ 别指望从 prefab 抄英文，如实标注「我们译的」。
            // 消费侧 = `Shell/DeckInfoPopup.cs` 的 `ShareOnChat()`（那三个 `ShareChat*Term` 常量上写着判据）。
            { "MenuDeck/Share/Title",                         new Entry("分享卡组", "Share Deck") },
            { "MenuDeck/Share/ShareOnAlliance",               new Entry("在联盟聊天中分享", "Share on Alliance chat") },
            { "MenuDeck/Share/ShareOnGlobal",                 new Entry("在全球聊天中分享", "Share on Global chat") },

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
            { "MenuDeck/Filters/ClearFilters",                new Entry("清除过滤器", "Clear filters") },  // TMP 原文 `Clear filters`；zh_CN.csv:9
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
            //   ⚠️ prefab 那颗 TMP（`MonoBehaviour_-725143033836620417.json` 所指的
            //      GameObject `Button Text_7450249239293310335`）的 `m_text = ''`（**本批实读**）。
            //   🔴 **2026-10-17（`A1951`）就地订正**：原写「原版那套在远端 I2 表里 ⇒ 中英两列都是我们拟的、
            //      将来拿到表先改这一条」—— 官方表已在本机，`MenuDeck/Button/Random` = `Random` / `随机`，
            //      **与本行两列逐字相同**。
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
            //   🔴 **2026-10-17（`A1951`）就地订正**：原写「中文列取自 `zh_CN.csv:98`、对应哪条键没查清
            //   （要拿到远端 I2 表才能钉死）」—— 官方表已在本地，`Battle/Tips/EnergyCost` =
            //   `Energy Cost` / `能量花费`，**与本行两列逐字相同** ⇒ 已钉死。
            { "Battle/Tips/EnergyCost",                       new Entry("能量花费", "Energy Cost") }, // 17 颗 · 含 3 颗 `Cost Filter/Title`；zh_CN.csv:98（英文串索引，见上）
            // ------------- 空态那三行（`MenuCollection/*`，四个页各一份 `Empty Collection Warning`）-------------
            { "MenuCollection/NoCardsFound",                  new Entry("在您选择的过滤器中，您的收藏里没有任何张牌", "There are no cards in your collection for the selected filters") },   // TMP 原文逐字符；zh_CN.csv:177
            //   🔴 中文列**是我们自拟的**：`zh_CN.csv` 里**没有 `There are no cardbacks…` 这个键**（精确查 0 命中）
            { "MenuCollection/NoCardbackFound",               new Entry("在您选择的过滤器中，您的收藏里没有任何卡背", "There are no cardbacks in your collection for the selected filters") }, // TMP 原文逐字符；中文自拟
            //   🔴 **英文列是原版 TMP 的原文，含原版自己的语病**（`There are no **deck** …` 单数）——
            //   照抄（本表英文列的口径 = 那颗 TMP 的 `m_text` 原文逐字符；⛔ 别「顺手修正」成 `decks`，
            //   那会与 `Shell/ShopWindow` 的 `TxtEmpty`（同一句原文）分叉）。
            //   ⚠️ 中文取自 `zh_CN.csv:178`，但那条的英文串是 `…for the selected **filter**`（单数 filter），
            //   与我们这一串（复数 filters）**差一个字母** ⇒ 属「最近邻」，**不是精确命中**（如实记）。
            { "MenuCollection/NoDecksFound",                  new Entry("在您选择的过滤器或游戏模式下，您的收藏中没有卡组", "There are no decks in your collection for the selected filters") }, // 🔴 2026-10-23：**ZH = 官方值**（原取自 `zh_CN.csv:178` 近邻）；EN 仍是 TMP 原文（含原版语病，官方多 `or game mode` —— EN 未动）
            // ------------- 收藏窗那两处页头 / 通用钮 --------------
            { "MenuCollection/Label/Cosmetics",               new Entry("你的化妆品收藏", "Your cosmetics collection") }, // TMP 原文；`label < Header < Cardback Tab`；zh_CN.csv:25
            //   `Back`：同键 8 颗，收藏窗那颗 = `Button Text < Close Button < Shared < Tabs < Content Area < Collection Menu Variant`
            { "MainMenu/MainButtons/ButtonLabel/Back",        new Entry("返回",     "Back") },        // TMP 原文 `Back`；zh_CN.csv:5
            // ------------- `Deck Name` 那颗输入框的占位符 --------------
            { "MenuDeck/HUD/EditDeckName",                    new Entry("点击以编辑卡组名称", "Tap to edit deck name") }, // TMP 原文；`Placeholder < Text Area < Deck Name < …>`；zh_CN.csv:172

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
            { "MenuShop/RefreshCounter",                      new Entry("可用的", "Refreshes in:") },

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
            { "SocialMenu/Alliances/JointToEarnRewards", new Entry("加入联盟以获得额外奖励", "Join an Alliance to gain additional rewards") }, // TMP 原文；中文取 `zh_CN.csv:263` 的**近邻**（那条的英文串是 `Join an Alliance for extra Rewards`，**与我们这串不同**）
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
            { "Demo/FriendsMenu/EnterPlayerName", new Entry("输入玩家名字", "Enter player name") },   // TMP 原文；中文 = `zh_CN.csv:101`（精确命中）
            //   2 颗 · `Friends Title < Friends List < Friends Tab`
            { "Demo/FriendsMenu/YourFriends",     new Entry("你的朋友：", "Your friends:") },       // TMP 原文（**带冒号**）；中文 = `zh_CN.csv:342`（精确命中）
            // ------------- 聊天窗（`ChatPanel`）-------------
            //   1 颗 · `Placeholder < Text Area < InputField (TMP) < Enter Text < Chat < Holder < ChatPanel`
            { "MainMenu/Chat/TypeMessage",        new Entry("输入消息",   "Type message") },        // TMP 原文；中文 = `zh_CN.csv:183`（精确命中）
            // 🆕 **2026-10-19（T17）补登**：`Player Options Panel` 那五颗钮的**官方词条**
            //   （`bundle_menus_assets_all` 的 `Localize.mTerm` 逐颗实读：Add / Challenge / Report / Block 四颗有，
            //    `Profile` 那颗 prefab 上**没有 `Localize`** ⇒ 保留原 TMP 文本 `Profile`，见 `Shell/ChatPanel.cs`）。
            //   值 = 官方 zh-CN / EN 逐字。消费点 = `Shell/ChatPanel.Build` 的 `actTerms`。
            { "MainMenu/Chat/Add",                new Entry("添加为朋友", "Add as a friend") },
            { "MainMenu/Chat/Challenge",          new Entry("挑战",       "Challenge") },
            { "MainMenu/Chat/Report",             new Entry("举报信息",   "Report message") },
            { "MainMenu/Chat/Block",              new Entry("屏蔽玩家",   "Block player") },
            // ------------- 社交窗左栏那两颗页签（`TabBtnSpec.Label`）-------------
            //   各 1 颗 · `TabButtonLabel < Label < {Alliances,Friends} Tab Button < Tab Buttons < Content Area < Social Submenu Variant`
            //   ⚠️🔴 **渲染时会被基类 `ToUpperInvariant()` 转成大写**（`Shell/MenuWindowBase.cs` 的 `Text(b, (spec.Label ?? "").ToUpperInvariant(), …)`）
            //   ⇒ 英文档印的是 `ALLIANCES` / `FRIENDS`（**原版也这样**，那条 `Text` 就是照原版的 `TabButtonLabel` 建的）；
            //     中文（CJK）**不受 `ToUpperInvariant` 影响** ⇒ 中文档印「联盟」/「好友」。⛔ 别把 EN 列写成大写（改了反而与上面那条口径打架）。
            { "SocialMenu/Alliances",             new Entry("联盟",       "Alliances") },          // TMP 原文；中文**自拟**（CSV 无 `Alliances`；近邻 `zh_CN.csv:52` 是 `Alliance` 单数）
            { "SocialMenu/Friends",               new Entry("朋友",       "Friends") },            // TMP 原文；中文**自拟**（CSV 无 `Friends`）
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
            { "SocialMenu/Alliances/DescriptionInput", new Entry("联盟描述", "Alliance Description") }, // TMP 原文 `Alliance Description`（同上）；中文**自拟**

            // ============================================================ 🆕 **2026-10-18（第十轮 · B 类做完）：最后 3 条**
            // 键名 / 英文列的口径同上一块（按 pid 亲读 `mTerm` + 同 GO 的 TMP `m_text`；父链沿 `m_Father`）。
            // ⚠️ 这一块的三条中文列**全部是我们自拟的** —— `zh_CN.csv` 里 `Dismiss` / `Select language` /
            //   `Select privacy` **三个英文串一条都没有**（按第一列精确查过）。
            // ------------- 邀请行那两颗钮里的第二颗（`Reject`）-------------
            //   3 颗同键（`Invitation List Entry` 的两个实例 + 它的 `(1)` 模板）·
            //   `Button Text < Reject < Invitation List Entry < List < Invitations < List Area < List View <
            //    AllianceNotMemberVariant < Alliances Tab` ⇒ **节点名 `Reject` 与我们逐字相同**
            { "SocialMenu/Alliances/Dismiss",       new Entry("解散",     "Dismiss") },              // TMP 原文 `Dismiss`；中文**自拟**
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
            //     · 另三颗的原版 TMP 是**西语占位串**（`Degradar` / `Expulsar Jugador` / `Abandonar Alianza`）。
            //       🔴 **2026-10-17（`A1951`）就地订正**：原写「英文原文取不到（原版那套在远端 I2 表）
            //       ⇒ 这一列是我们自拟的英文」—— 官方表已在本机，这四条（`Promote`/`Demote`/`KickPlayer`/
            //       同族）两列与官方逐字相同。
            //       ⛔ **别把西语当英文抄进来**（那会让人以为原版英文就是这个）。
            { "SocialMenu/Alliances/Promote",        new Entry("提升",     "Promote") },            // TMP 原文 `Promote`（**英文**）✅；中文自拟
            { "SocialMenu/Alliances/Demote",         new Entry("降级",     "Demote") },             // 🔴 EN **我们自拟**（原版 TMP = 西语占位串 `Degradar`）；中文自拟
            { "SocialMenu/Alliances/KickPlayer",     new Entry("赶走玩家", "Kick Player") },        // 🔴 EN **我们自拟**（原版 TMP = 西语占位串 `Expulsar Jugador`）；中文自拟
            { "SocialMenu/Alliances/Quit",           new Entry("退出联盟", "Quit Alliance") },              // 🔴 EN **我们自拟**（原版 TMP = 西语占位串 `Abandonar Alianza`）；中文自拟
            // ------------- 奖杯详情窗（`Alliance Trophy Info Popup`）那两行 --------------
            //   各 1 颗 · `Next Tier < Progress < Controls < RightSide < window`（TMP `Next Tier:`）·
            //   `Label < Checkbox < selectButton < Controls < RightSide < window`（TMP `Alliance featured trophy`）
            //   ⚠️ 与 `Shell/AllianceMemberTab.cs` 里那三处**样例串**（`Featured: Trophy Name` 等，**原版不挂 `Localize`**）
            //      **是两回事**（那三处⛔不接）；这两条是**另一扇窗里挂着词条**的两颗。
            { "SocialMenu/Alliances/Trophies/NextTier",           new Entry("下一层：",   "Next Tier:") },            // TMP 原文 `Next Tier:`（**带冒号**）；中文**自拟**（CSV 无该串）
            { "SocialMenu/Alliances/Trophies/FeaturedTrophyLabel", new Entry("联盟特色战利品", "Alliance featured trophy") }, // TMP 原文；中文 = **`zh_CN.csv:53` 精确命中**

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
            // 英文那一列 = **原版 `+600` 那个原始种族串**（也就是我们数据里 `subtype` 的原值）。
            //   🔴 **2026-10-17（`A1951`）就地订正**：原写「原版英文 TMP 文案在远端 I2 表里、本地拿不到」——
            //   官方表 2026-10-14 已在本机（`Card_Race/*` 21 条），本族两列与官方逐字相同。
            //   🔴 **2026-10-19（`W57` 界面线 UI-8）现读订正**：官方 `Card_Race/*` 现为 **23 条**
            //   （2026-10-18 并入当前 build 后 +2）；「21 条」是 2026-10-14 旧快照的读数。
            //   ⚠️ 「本族两列与官方逐字相同」**只对大部分键成立**：现核有 2 条 ZH 不同
            //   （`Secret` 隐秘/秘密 · `Codicil` 法典/附则，都是保留下来的旧译）。
            //   🔴 **2026-10-23（卡牌线 `G2`）**：原为 3 条 —— `Battlesuit` 的 ZH 已由「战甲」换成官方 **「战斗服」**
            //   （判据：旧词在 `cards_engine.json` 的 `nameZh`/`descZh` 里 **0 处**，全池 1,127 张现读 ⇒ 不会出现
            //   「卡面章 = 官方词、卡面正文 = 我们的旧词」那种两套说法）。
            //
            // ⚠️ 下面 43 条 = `RuleEngine/Resources/cards_engine.json` 里**出现过的全部非空 `subtype`**
            //    （🔴 2026-10-19 现读：全池 **1,127** 张卡、其中 **1,116** 张有非空 `subtype` —— 原写
            //     「1126 张卡」已过期。其中**只有 19 个会真的印到卡面上**、共 722 张 —— 判据 `CardView.SubtypeLine`
            //     与 `CardView.TacticSubtypeShown`）。剩下 24 个**现在不印**，列在这里是为了
            //     「哪天白名单长一条，卡面不会突然掉回英文」。⛔ 这张表**不是**「引擎支持的 subtype 列表」。
            // ------------- 会印到卡面上的 19 个（★ = 实拍读到的那 3 个）-------------
            { "Card_Race/Infantry",            new Entry("步兵",       "Infantry") },           // ★实拍；zh_CN.csv:6363
            { "Card_Race/Vehicle",             new Entry("载具",       "Vehicle") },            // ★实拍；zh_CN.csv:6365
            { "Card_Race/Warlord",             new Entry("战将",       "Warlord") },            // ★实拍；⚠️ zh_CN.csv:15 那条通用词 2026-10-18 起也是「战将」（A877，原来写「督军」）
            // 🔴 **2026-10-25（`E` 卡牌面/渲染域 · `K4` §②·D9 值那一半）**：下面这 6 条的**键**改成原版
            //    `SpellType` 枚举名、**值**改成官方 zh-CN 表（`数据/游戏数据/官方本地化_en+zh-CN.json`）——
            //    判据（一手）：`GameStaticData__CardRaceToString.c` = `GetTranslation("Card_Race/" + Enum.ToString(spellType))`；
            //    官方值现读：`PsychicPower = 心灵力量` · `CombatElixir = 战斗灵药` · `Genomic = 基因增强` ·
            //    `OverlordPower = 霸主力量` · `DefensiveCard = 防守` · `MarkOfChaos = 黑暗契约`。
            //    ⛔ 原来的带空格键（`Card_Race/Psychic Power` …）**已删** —— 同一件事留两份写法 = 迟早不一致；
            //    卡表写法 → 枚举名的那一步只在 `CardView.RaceKeyBySubtype`（唯一一处）里做。
            //    （这批共 **57 张卡**的卡面底部那行字因此与官方对齐；英文档的值与官方原本相同。）
            { "Card_Race/DefensiveCard",       new Entry("防守",       "Defence") },            // 2026-10-25 D9：官方 zh（原「防御」）
            { "Card_Race/Monster",             new Entry("怪物",       "Monster") },            // zh_CN.csv:6369
            { "Card_Race/Beast",               new Entry("野兽",       "Beast") },              // zh_CN.csv:6372
            { "Card_Race/Battlesuit",          new Entry("战斗服",     "Battlesuit") },         // zh_CN.csv:6374
            { "Card_Race/Drone",               new Entry("无人机",     "Drone") },              // zh_CN.csv:6377
            { "Card_Race/PsychicPower",        new Entry("心灵力量",   "Psychic Power") },      // 2026-10-25 D9：官方 zh（原「灵能」）
            { "Card_Race/CombatElixir",        new Entry("战斗灵药",   "Combat Elixir") },      // 2026-10-25 D9：官方 zh（原「战斗药剂」）
            { "Card_Race/Daemon",              new Entry("恶魔",       "Daemon") },             // zh_CN.csv:6381
            { "Card_Race/Secret",              new Entry("秘密",       "Secret") },             // 🔴 2026-10-23：ZH = 官方值（原「隐秘」取自 zh_CN.csv:404）
            { "Card_Race/MarkOfChaos",         new Entry("黑暗契约",   "Dark Pact") },          // 2026-10-25 D9：键 = 枚举名（值同官方）
            { "Card_Race/Sabotage",            new Entry("破坏",       "Sabotage") },           // zh_CN.csv:403（…的 sabotage 同值）
            { "Card_Race/Codicil",             new Entry("附则",       "Codicil") },            // 🔴 2026-10-23：ZH = 官方值（原「法典」取自 zh_CN.csv:6408）
            { "Card_Race/Genomic",             new Entry("基因增强",   "Genomic Enhancement") },// 2026-10-25 D9：官方 zh（原「基因强化」）
            // 🔴 **2026-10-18（A879）就地改值：中文列 `计策` → **`祈唤`**** —— 三条判据（主对话已亲核）：
            //   ① **原值 `计策` 与「卡类别」撞名**：`type='tactic'` 那类卡在 PnP 目录里就叫 `4计策/`
            //      （`d:/2/Warpforge部队卡片/Tau/4计策/Warpforge_52_Sense-of-Stone.png` 那一族），
            //      而本键是**卡面兵种行**（卡面那行橙字）—— 那张成品卡的兵种行印的是英文 **`Invocation`**。
            //   ② 我们那份词表 `数据/本地化/i18n/zh_CN.csv:6386` = `Invocation,~,祈唤`。
            //   ③ 本键**会真印到卡面上**（`Core/CardView.cs` 的 `TacticSubtypeShown` 白名单含 `Invocation`，
            //      共 3 张卡）⇒ 改它 = **玩家可见**。
            //   ⛔ 别去改 `zh_CN.csv` —— 它**不是**权威表（见下）。
            //   🔴 **2026-10-15 就地订正（铁律 5）**：本行原文写「（它是 `工具/sync_from_d2.py` 从
            //      `d:/warpforge/data/i18n` 同步来的产物，**改了会被下次同步覆盖**）」—— **两句都要改**：
            //      ① **不会被覆盖**：`sync_from_d2.py` 的语义是「目标已存在同名 ⇒ 跳过」，
            //         而且 d:/4 这份**确实已经被本地改过**（现读 384,408 B / 2026-10-15
            //         vs `d:/warpforge/data/i18n/zh_CN.csv` 384,393 B / 2026-08-27，两边**已分叉**）；
            //      ② **它本身降级了**：它是**我们自译的 Godot 时代表**，**权威 = 原版官方 zh-CN 表**
            //         （`数据/游戏数据/官方本地化_en+zh-CN.json`）。用户 2026-10-15 口径：
            //         「我们直接使用解包资料，不要被 Godot 时代的账这些干扰」。
            //      ⇒ 引用它时按「**人工参考表**」看；要换官方值走 `A1762`/`A1763` 那批迁移。
            { "Card_Race/Invocation",          new Entry("祈祷",       "Invocation") },         // zh_CN.csv:6386（A879：原值「计策」与卡类别 `4计策/` 撞名）
            { "Card_Race/OverlordPower",       new Entry("霸主力量",   "Overlord Power") },     // 2026-10-25 D9：官方 zh（原「霸主之力」）
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
            { "Battle/Overtime/Title",             new Entry("加时赛！",   "OVERTIME!") },   // TMP 原文 `OVERTIME!`；中文**自拟**
            //   `Battle/HUD/AffectedBy`：`…battlearena1/MonoBehaviour_4578.json`；TMP 原文 `Affected by:`（带冒号）
            { "Battle/HUD/AffectedBy",             new Entry("受到影响：", "Affected by:") }, // TMP 原文；中文 = 我们原来写死的那句
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
            //   🔴 **2026-10-10（`A1103`）中文列改成原版运行期真渲的那一句（铁律 5 / 铁律 11）**：
            //     原写「中文 = `zh_CN.csv:198`（精确命中）」⇒ 值是 **`你后手`** —— 那是**我们自己 2026-08-27 译的**
            //     （`zh_CN.csv` 的来源定案：`资料/全量反编译复核_靠推断的清单.md` §2.2 那格）。
            //     🔴 **2026-10-17（`A1951`）就地订正**：这里原写「原版客户端**没有中文表**、I2 词条在远端 CCD」——
            //     只对**离线安装目录**成立：官方中文表在远端 Addressables 包 `localization_assets_all` 里，
            //     2026-10-14 已解出并落进 `数据/游戏数据/官方本地化_en+zh-CN.json`
            //     ⇒ `Battle/Mulligan/secondTurn` 等本族键的两列与官方逐字相同。
            //     🔑 **原版判据 = 现存【唯一】一份原版真渲中文的记录**：
            //     `资料/原版参照图/Unity参照管线_0825/data/panel_0914b/p3_mulligan_tree.tsv:15`
            //     （2026-09-14 运行期 dump，`/MulliganText/TurnText` 的 TMP `text` = **`你是第二个行动`**，fs 41.8）。
            //     「就是这一行」的绑定 = 那颗 `Localize.mTerm` 正是本键（`MulliganManager.mulliganTurnTextLocalize`
            //     → MB 5183；判据全文写在 `Battle/MulliganPanel.cs` 的 `TurnText` 那段 doc 里）。
            //     ⛔ **别删那份 dump**（同批查出：`localization_assets_all.bundle` 本机没有、远端 URL 钉在
            //     `catalog_main.json` 里；那两句今天已不在盘上）。
            //   ✅ **2026-10-13：同一对的另一半（`Battle/Tips/GoFirst`）【有原版实拍判据了】** —— 用户给的
            //     🔴 **两张**原版实拍：`战斗—替换手卡参考图（先手）.png` 上那行白字逐字印着 **「你先行」**，
            //     `战斗—替换手卡参考图（后手）.png` 上印的是 **「你是第二个行动」**（与下面那条 `Entry` 的中文列
            //     **逐字相同** ✅）—— 而且**后手那张确实是 4 张起手牌** ⇒ 「先手 3 / 后手 4」这条算式
            //     （`startingHand` + `secondaryExtraCards`，见 `RuleCore.StartHand`）**与用户实拍互证**。
            //     ⇒ 把自拟的「你先手」改掉（见下面那条 `Entry`）。
            //     ⛔ 仍不许**照这一句的形状替它编**（例如「你是第一个行动」）—— 有实拍就照实拍。
            { "Battle/Mulligan/secondTurn",        new Entry("你是第二个行动", "You go second") }, // TMP 原文；🔴 中文 = **原版运行期真渲**（`p3_mulligan_tree.tsv:15`），⛔ 不是 `zh_CN.csv`
            //   `Battle/Mulligan/Instructions`：`…battlearena1/MonoBehaviour_5197.json`，TMP 原文 `Choose cards to replace in first hand`
            //   🔴 **2026-10-10（`A1103`）中文列同上改成原版运行期真渲的那一句**：原写 `选择首局替换的卡牌`
            //     （= `zh_CN.csv:75`，我们自己译的）⇒ 现 = **`选择要在首轮替换的牌`**
            //     （`p3_mulligan_tree.tsv:13`，`/MulliganText/Text`，fs 53.7；同一棵树上绑 `mulliganTextLocalize` → MB 5197）。
            //   ✅ **同族第三条对得上、没动**：`…/MulliganContinueButton/Text` 原版真渲 = `继续`
            //     ⇔ 本表 `Battle/Mulligan/ButtonDone` 的 ZH 列**逐字相同**（`panel_0914b` 那棵树 :8）。
            { "Battle/Mulligan/Instructions",      new Entry("选择要在首轮替换的牌", "Choose cards to replace in first hand") }, // TMP 原文；🔴 中文 = **原版运行期真渲**（`p3_mulligan_tree.tsv:13`）
            //   `Battle/Mulligan/Replace`：**全库只此 1 颗** —— `bundle_battleprefabs_vfxandmisc_assets_all/
            //   MonoBehaviour_4126337295248883513.json`（那颗 `ReplaceText`），TMP 原文 `Replace`
            //   ⚠️ 中文**一直是自拟的** —— 🔴 **2026-10-13 订正：原版印的是「替换」**（判据 = 用户给的原版实拍
            //   `C:/Users/qjh36/Desktop/warpforge参考图/战斗—替换手卡参考图（先手）.png`：每张起手牌下面那颗钮上
            //   逐字印着「替换」）⇒ 把我们自拟的「换」改掉。⛔ 别退回「换」。
            { "Battle/Mulligan/Replace",           new Entry("替换",     "Replace") },      // TMP 原文 `Replace`；🔴 中文 = **原版实拍**（2026-10-13 用户参考图）
            //   🆕 **2026-10-18（第十三轮 · G2b）`Battle/Mulligan/Undo`**：**同一颗钮的第二档字**
            //   （牌被标记要换时印这条）。判据 = `MulliganFrame.{ChangeCardButtonOnClick,SetupMulligan,
            //   UpdateButtonText}` 三处逐字相同的
            //   `term = (card+0x228 == 0xe) ? "Battle/Mulligan/Undo" : "Battle/Mulligan/Replace"`。
            //   ⚠️ 这条键**只在代码字面量里**（载波②；prefab 上零 `Localize`）⇒ 本地**没有**它的
            //      显示串 ⇒ EN/ZH **都自拟**（EN 取词条名末段，同 `MenuDeck/Button/Random` 的先例；
            //      `zh_CN.csv` 里 `Undo` 这个英文串**不存在**，2026-10-18 按第一列精确查过）。
            // 🔴 **2026-10-14（`A1803`）就地订正**：上面那句「EN/ZH 都自拟」**不成立** —— 官方
            //   `Battle/Mulligan/Undo` = `["Undo", "撤销"]`，与本条两列**逐字相同**（错因：当时表还没解开）。
            { "Battle/Mulligan/Undo",              new Entry("撤销",     "Undo") },        // ✅ 两列 = 官方值（2026-10-14 核）
            //   `Battle/Mulligan/WaitEnemy`：`…battlearena1/MonoBehaviour_4804.json`（`BackCanvas/WaitText`），
            //   TMP 原文 **`Waiting for enemy`**。🔴 **推翻上一轮**「原版 `WaitText` 文案没查到 ⇒ 这条是我们加的」：
            //   节点有字也有键（当年只看了 dump 里那个空的 `Text`）。我们表里那句中文仍是我们的译法。
            { "Battle/Mulligan/WaitEnemy",         new Entry("等待敌人", "Waiting for enemy") }, // TMP 原文；中文 = `zh_CN.csv:35`（精确命中）
            //   `MainMenu/Settings/SettingLabel/{Music,SoundFx}` + `Settings/Media/VoiceOvers`：
            //   三根音量滑块各 1 颗（13 个 arena + `menus` 各一份，共 14 颗/条），父链
            //   `BattleSettingsPanel < Volume Sliders < {Music,FX,Voiceover} Container < Text`；TMP 逐字同值。
            { "MainMenu/Settings/SettingLabel/Music",   new Entry("音乐", "Music") },        // TMP 原文；中文 = `zh_CN.csv:4`（`MUSIC` 大小写不同 ⇒ 近邻）
            { "MainMenu/Settings/SettingLabel/SoundFx", new Entry("音效", "Sound Effects") },// TMP 原文；中文**自拟**（csv 无 `Sound Effects`）
            { "Settings/Media/VoiceOvers",              new Entry("语音配音", "Voice-overs") },  // TMP 原文；中文**自拟**（csv 无 `Voice-overs`）
            //   `Battle/Settings/ResignButton`：`…battlearena1/MonoBehaviour_4196.json`，TMP 原文 `Resign`
            { "Battle/Settings/ResignButton",      new Entry("放弃",     "Resign") },        // TMP 原文；中文 = `zh_CN.csv:454`（精确命中）
            //   `Battle/Settings/SkipTutorial`：`…battlearena1/MonoBehaviour_4307.json`，TMP 原文 `Skip tutorial`
            { "Battle/Settings/SkipTutorial",      new Entry("跳过教程", "Skip tutorial") }, // TMP 原文（小写 t）；中文 = `zh_CN.csv:162`（那条英文串是 `Skip Tutorial`，大小写不同 ⇒ **近邻**）
            // 🆕 2026-10-16（`T01` · 静音对手那颗开关 `ChatToggle`）：三颗各一条，**键 = 原版 `Localize.mTerm` 原文**。
            //   出处 = `…battlearena2/MonoBehaviour/{5295,5163,5396}.json`（各自挂在 `ChatToggle` 的
            //   `Label` / `Background On/Checkmark/OnText` / `Background/Checkmark/OffText` 三颗的 GameObject 上）。
            //   🔴 **两列都是官方值**（2026-10-14 解开的那张表里逐字命中）：`官方本地化_en+zh-CN.json`
            //   ⇒ `MuteChatLabel=["Mute opponent","静音对手"]` · `MuteChat_On=["ON","开"]` · `MuteChat_Off=["OFF","关"]`。
            { "MainMenu/Settings/ButtonLabel/MuteChatLabel", new Entry("静音对手", "Mute opponent") },
            { "MainMenu/Settings/ButtonLabel/MuteChat_On",   new Entry("开",       "ON") },
            { "MainMenu/Settings/ButtonLabel/MuteChat_Off",  new Entry("关",       "OFF") },
            //   `Battle/Tips/Continue`：`…menus/MonoBehaviour_-1010790197081052438.json`，TMP 原文 `Continue`
            { "Battle/Tips/Continue",              new Entry("继续",     "Continue") },     // TMP 原文；中文 = `zh_CN.csv:84`（精确命中）
            // 🆕 2026-10-18（第三会话 · `A985⑦`）：**抽牌被弃的提示**（手牌满 ⇒ 抽到那张直接进弃牌堆）。
            //   原版那支 = `PlayerHand._AddDrawnCardToHand_d__37:129-141` ⇒ `Hand.Count < MaxCardsInHand`
            //   才进手牌，否则**进墓地 + `ShowHeadsUpMessage(GetTermTranslation(0x428A218))`**。
            //   🔴 **2026-10-14（`A1803`）就地订正**：这一段原来写「这一条的值**不是原版的** —— 文案在远端 I2 表、
            //   本地拿不到」—— **两句都不成立**：官方 `Battle/Tips/HandFull` = `["Hand is full", "手牌已满"]`，
            //   表里这两列**就是官方值**（ZH 早已对上；EN 本会话也换成官方那句了）。
            //   ⚠️ 仍**不是**原版的只有 `BattleDriver.HandFullText()` 里那句兜底副本（`Hand full - the drawn
            //   card goes to the discard pile`）—— 它今天走不到（`Loc.HasEntry` 为真），但要跟着改。
            //   ⚠️ **本工程的规矩是不许静默失败**：宁可出声说一句我们的话，也不让玩家看着牌凭空消失。
            //   ✅ **官方值已到位（2026-10-14）**：本行两列就是官方值 ⇒ 这一条不需要再改（`Loc.HasEntry` 有就走它）。
            { "Battle/Tips/HandFull",              new Entry("手牌已满", "Hand is full") },   // 🔴 EN 2026-10-14（`A1803`）改官方；⚠️ 我们自写的那句「…goes to the discard pile」现在只剩 `BattleDriver.HandFullText()` 里的兜底副本（那份要跟着改）
            //   `Battle/Tips/{MeleeAttack,RangeAttack,HealthPoints}`：13 个 arena 各一颗，父链
            //   `Card Display Window < Card Display < TutorialObjs < UnitObjs < {Melee,Ranged,Health}Text`
            //   ⇒ TMP 原文 **`Melee Attack` / `Ranged Attack` / `Health Points`**。
            //   🔴 **这三条 + 下面 `EnergyCost` = `BattleDriver.cs` 教学校注那四块** ——
            //   该处原来的注写「本地没有 ⇒ 这四句是我们按卡面语义写的」**不成立**（四块逐块找得到），已就地订正。
            { "Battle/Tips/MeleeAttack",           new Entry("近战攻击", "Melee Attack") }, // TMP 原文；中文 = 我们原来那句（`Phrases["MELEE"]`）
            { "Battle/Tips/RangeAttack",           new Entry("远程攻击", "Ranged Attack") }, // TMP 原文；中文 = 我们原来那句（`Phrases["RANGED"]`）
            { "Battle/Tips/HealthPoints",          new Entry("生命点数", "Health Points") },   // TMP 原文；🔴 ZH 2026-10-14（`A1803`）改官方（原「中文自拟」作废）
            //   `Battle/Tips/GoFirst`：**没有 `Localize`（只此载波②）** —— `d:/2/tools/il2cpp_out/stringliteral.json`
            //   `0x428A128` = `Battle/Tips/GoFirst`。消费侧判据 = `MulliganManager.ActivateMulligan` 的先手/后手二选一
            //   （`MulliganPanel` 的 `TurnFirst`）。🔴 **2026-10-17（`A1951`）就地订正**：原写「英文原文本地取不到
            //   （远端 I2 表）⇒ EN/ZH 都自拟」—— 官方表已有这条键（见下一行行尾注，两列都是官方值）。
            { "Battle/Tips/GoFirst",               new Entry("你先行",   "You go first") }, // 🔴 中文 = **原版实拍**（2026-10-13 用户参考图）；✅ **2026-10-14（`A1803`）订正**：EN 那句「值在远端 I2 表」**不成立** —— 官方 = `["You go first", "你先行"]`，**两列都是官方值**

            // ============================================================ 🆕 **2026-10-14（第二十八会话 · `A1803`）：原版 `Battle/` 那一族【剩下的 105 条】**
            //
            // 🔴 **这一块为什么今天才填**：这 105 条键原来只在**代码字面量**那一支上（载波②，见 `Battle/` 那一族开头那三句），
            //   当时**值还只在远端 I2 表里**（2026-10-14 之前）；那张表已解出，本块两列都照官方录入（见下一行）。
            //   2026-10-14 官方 zh-CN 表解开（`数据/游戏数据/官方本地化_en+zh-CN.json`）
            //   ⇒ **两列都照官方表逐字录入**（⛔ 不是我们译的、⛔ 不是兜底句）。
            //
            // ⚠️ **值里带记号的那几条**（`I2` 占位符，`gen_official_loc.py` 的文件头写着）：
            //   `[[vanguard]]` / `[[regeneration]]` / `[[camouflage]]` / `<sprite name=Atlas_trait_icon_armour>`
            //   —— 那是**原版运行时才替换**的记号。我们这边：`<sprite name=Atlas_…>` **能直接解析**
            //   （TMP 精灵图集 `Resources/Fonts/Warpforge Trait TextSprites.asset` 里 70 个 `Atlas_trait_icon_*` 全在），
            //   而 `[[..]]` 还**没有**渲染层（那是 `A1763` 的活）⇒ 这几条照写官方值，**接线之前先补那一层**。
            //
            // ⚠️ **消费侧现状（如实记，⛔ 别以为填了就会上屏）**：
            //   · `Battle/Tips/*`：`BattleDriver.HintForCodeWithKey` 走 `RuleCodes.TermKey(rc)` → `Loc.HasEntry(键)` 两道闸；
            //     `TermKey` 今天**只映射 4 条**（`ErrCost`/`ErrNotTurn`/`ErrNoTargetAvailable`/`ErrNotEnoughRoom`）
            //     —— 那 4 条**已在表内**；本块新加的 Tips 键**要 `TermKey` 也跟着映射才会上屏**（`RuleEngine/Core/RuleCodes.cs`，⛔ 不在本笔白名单）。
            //   · `Battle/Effect/*`：`Battle/CardDisplayWindow.cs` 的 `AttrTerm` 只取 `…OneTurn` 那 4 条（本块的 `ChangeArmour` 等**无后缀**那批= 永久改属性档，今天无消费点）。
            //   · `Battle/Cemetery/*`（20 条）：消费侧要**先加 3 种引擎事件 + 给 `Play`/`Ability` 补目标与伏击标记**
            //     （判据全文在 `Battle/BattleDriver.cs` 的 `RefreshBattleLog` 那段注释）⇒ **本块只是把值备好**，⛔ 不是「已接」。
            //   · `Battle/HUD/{LostConnectionMsg,MatchAlreadyFinished,PleaseWaitConnection,WaitOpponentConnectionMsg}`：
            //     联机那几处今天走的是 `NetRuntime.Notice` 自己的串 —— 换成词条要动 `Battle/` 与 `Net/`。
            //   · 其余（`BattleEnd/` 8 · `MatchType/` 5 · `Prebattle/` 4 · `ChooseCard/` 3 · `Settings/` 2 + 4 条裸 `Battle/`）：**今天无消费点**，值备好。
            // 判据逐条 → `资料/普查产出_第二十八会话/W4_官方文案族.md`。

            // ---- Battle/BattleEnd（8 条）----
            { "Battle/BattleEnd/CantLoseRating",                       new Entry("在此级别无法失去评级", "Can't lose rating in this division") },
            { "Battle/BattleEnd/DamageDone",                           new Entry("造成伤害", "Damage dealt") },
            { "Battle/BattleEnd/Location",                             new Entry("位置", "Location") },
            { "Battle/BattleEnd/TapToContinue",                        new Entry("点击以继续", "Tap to continue") },
            { "Battle/BattleEnd/TapToContinueMobile",                  new Entry("点击以继续", "Tap to continue") },
            { "Battle/BattleEnd/Time",                                 new Entry("持续时间", "Duration") },
            { "Battle/BattleEnd/Title",                                new Entry("战斗报告", "Battle Report") },
            { "Battle/BattleEnd/TroopsDead",                           new Entry("敌方伤亡", "Enemy casualties") },
            // ---- Battle/（裸键）（4 条）----
            { "Battle/BattleStartError",                               new Entry("已有游戏正在进行中。请在继续之前重新加载游戏。如果问题仍然存在，请联系 support@everguild.com", "There is already a game in progress. Please reload game before continuing.\nIf the problem persists, please contact support@everguild.com") },
            // ---- Battle/Cemetery（20 条）----
            { "Battle/Cemetery/ActionAbility",                         new Entry("{0} 使用技能", "{0} uses Ability") },
            { "Battle/Cemetery/ActionAttack",                          new Entry("{0} 攻击 {1}", "{0} attacks {1}") },
            { "Battle/Cemetery/ActionAttackMelee",                     new Entry("{0} 对 {1} 进行近战攻击", "{0} does a Melee attack against {1}") },
            { "Battle/Cemetery/ActionAttackRanged",                    new Entry("{0} 对 {1} 进行远程攻击", "{0} does a Ranged attack against {1}") },
            { "Battle/Cemetery/ActionDisplayEnemyCardInHand",          new Entry("你展示了敌人手中的 {0}", "You reveal {0} in enemy hand") },
            { "Battle/Cemetery/ActionDisplayYourCardInHand",           new Entry("对手展示了你手中的 {0}", "Opponent reveals {0} in your hand") },
            { "Battle/Cemetery/ActionEnemyCollectsSpiritStone",        new Entry("对手收集灵魂石", "Opponent collects Spirit Stone") },
            { "Battle/Cemetery/ActionExitAmbush",                      new Entry("{0} 从伏击中出现", "{0} exits from Ambush") },
            { "Battle/Cemetery/ActionOpponentDraws",                   new Entry("对手抽取 {0}", "Opponent draws {0}") },
            { "Battle/Cemetery/ActionOpponentPlays",                   new Entry("对手出牌 {0}", "Opponent plays {0}") },
            { "Battle/Cemetery/ActionOpponentPlaysAmbush",             new Entry("对手打出一支具有伏击的军队", "Opponent plays a troop with Ambush") },
            { "Battle/Cemetery/ActionSecretOrder",                     new Entry("{0} 执行 {1}", "{0} executes {1}") },
            { "Battle/Cemetery/ActionTargetedAbility",                 new Entry("{0} 对 {1} 使用技能", "{0} uses Ability on {1}") },
            { "Battle/Cemetery/ActionTargetedOpponentPlay",            new Entry("对手对 {1} 出牌 {0}", "Opponent plays {0} on {1}") },
            { "Battle/Cemetery/ActionTargetedYouPlay",                 new Entry("你对 {1} 出牌 {0}", "You play {0} on {1}") },
            { "Battle/Cemetery/ActionYouCollectSpiritStone",           new Entry("你收集灵魂石", "You collect Spirit Stone") },
            { "Battle/Cemetery/ActionYouDraw",                         new Entry("你抽取 {0}", "You draw {0}") },
            { "Battle/Cemetery/ActionYouPlay",                         new Entry("你出牌 {0}", "You play {0}") },
            { "Battle/Cemetery/ActionYouPlayAmbush",                   new Entry("你打出一支具有伏击的军队", "You play a troop with Ambush") },
            { "Battle/Cemetery/AmbushedTroop",                         new Entry("具有伏击的军队", "troop with Ambush") },
            // ---- Battle/Chat（1 条）----
            { "Battle/Chat/Greetings",                                 new Entry("问候", "Greetings") },
            // ---- Battle/ChooseCard（3 条）----
            { "Battle/ChooseCard/Instructions-52ecd898b5a42c941afdc891e82d963f", new Entry("选择成为哪个战将", "Choose which Warlord to become") },
            { "Battle/ChooseCard/Instructions-defensive",              new Entry("选择防御张牌", "Choose defensive card") },
            { "Battle/ChooseCard/Instructions-offensive",              new Entry("选择进攻张牌", "Choose offensive card") },
            // ---- Battle/Effect（24 条）----
            { "Battle/Effect/ChangeAbilityCost",                       new Entry("{[0]} 技能费用", "{[0]} cost of ability") },
            { "Battle/Effect/ChangeAllStats",                          new Entry("获得 {0} 近战攻击，{1} 远程攻击和 {2} 生命值", "Get {0} Melee Attack, {1} Ranged Attack and {2} Health") },
            { "Battle/Effect/ChangeAllStatsOneTurn",                   new Entry("获得 {0} 近战攻击，{1} 远程攻击和 {2} 生命值一回合", "Get {0} Melee Attack, {1} Ranged Attack and {2} Health one turn") },
            { "Battle/Effect/ChangeArmour",                            new Entry("获得 {0} <sprite name=Atlas_trait_icon_armour> 盔甲", "Get {0} <sprite name=Atlas_trait_icon_armour> Armour") },
            { "Battle/Effect/ChangeBothAttacks",                       new Entry("获得 {0} 近战攻击和 {1} 远程攻击", "Get {0} Melee Attack and {1} Ranged Attack") },
            { "Battle/Effect/ChangeBothAttacksOneTurn",                new Entry("获得 {0} 近战攻击和 {1} 远程攻击一回合", "Get {0} Melee Attack and {1} Ranged Attack one turn") },
            { "Battle/Effect/ChangeCost",                              new Entry("{0} 费用", "{0} Cost") },
            { "Battle/Effect/ChangeHealth",                            new Entry("获得 {0} 生命值", "Get {0} Health") },
            { "Battle/Effect/ChangeMeleeAndHealth",                    new Entry("获得 {0} 近战攻击和 {1} 生命值", "Get {0} Melee Attack and {1} Health") },
            { "Battle/Effect/ChangeMeleeAttack",                       new Entry("获得 {0} 近战攻击", "Get {0} Melee Attack") },
            { "Battle/Effect/ChangeRangedAndHealth",                   new Entry("获得 {0} 远程攻击和 {1} 生命值", "Get {0} Ranged Attack and {1} Health") },
            { "Battle/Effect/ChangeRangedAttack",                      new Entry("获得 {0} 远程攻击", "Get {0} Ranged Attack") },
            { "Battle/Effect/GetTrait",                                new Entry("获得 {0}", "Get {0}") },
            { "Battle/Effect/GetTraitOneTurn",                         new Entry("获得 {0} 一回合", "Get {0} one turn") },
            { "Battle/Effect/Khorne",                                  new Entry("赋予+2近战攻击和[[先锋]]", "Give +2 Melee Attack and [[vanguard]]") },
            { "Battle/Effect/Nurgle",                                  new Entry("赋予+1生命值和[[再生]] 1", "Give +1 Health and [[regeneration]] 1") },
            { "Battle/Effect/RemoveTrait",                             new Entry("移除", "Remove") },
            { "Battle/Effect/SetCost",                                 new Entry("将费用设为 {[0]}", "Set Cost to {[0]}") },
            { "Battle/Effect/SetHealth",                               new Entry("将生命值设为 {[0]}", "Set Health to {[0]}") },
            { "Battle/Effect/SetMeleeAttack",                          new Entry("将近战攻击设为 {[0]}", "Set Melee Attack to {[0]}") },
            { "Battle/Effect/SetRangeAttack",                          new Entry("将远程攻击设为 {[0]}", "Set Range Attack to {[0]}") },
            { "Battle/Effect/Slaanesh",                                new Entry("赋予+2近战攻击和+2远程攻击", "Give +2 Melee Attack and +2 Ranged Attack") },
            { "Battle/Effect/SwapStats",                               new Entry("交换攻击力和生命值", "Swap attack & health") },
            { "Battle/Effect/Tzeentch",                                new Entry("赋予+2生命值和[[伪装]]", "Give +2 Health and [[camouflage]]") },
            // ---- Battle/（裸键）（4 条）----
            { "Battle/EnemySyncError",                                 new Entry("你的对手已离开游戏", "Your opponent has left the game") },
            // ---- Battle/HUD（8 条）----
            { "Battle/HUD/BannerNoGuild",                              new Entry("无公会", "No lodge") },
            { "Battle/HUD/Defeat",                                     new Entry("失败", "Defeat") },
            { "Battle/HUD/LostConnectionMsg",                          new Entry("你已与服务器断开连接。正在尝试重新连接", "You've lost connection to the server. Trying to reconnect") },
            { "Battle/HUD/MatchAlreadyFinished",                       new Entry("无法重新连接。比赛已经结束。", "Unable to reconnect. The match has already finished.") },
            { "Battle/HUD/PleaseWaitConnection",                       new Entry("请等待", "Please wait") },
            { "Battle/HUD/Replay",                                     new Entry("重播", "Replay") },
            { "Battle/HUD/ShowingCards",                               new Entry("张牌", "Cards") },
            { "Battle/HUD/WaitOpponentConnectionMsg",                  new Entry("等待你的对手重新连接", "Waiting for your opponent to reconnect") },
            // ---- Battle/（裸键）（4 条）----
            { "Battle/Jammed",                                         new Entry("卡住了", "Jammed") },
            // ---- Battle/MatchType（7 条）----
            // 🔴 2026-10-23（`C1`）：`Ranked`/`Unranked` 两条 **ZH 换成官方值**（`经典` / `Unranked Classic`）。
            //   ⚠️ 官方表自己就是「键叫 Ranked、值是 Classic」（`Battle/MatchType/Ranked` = `Classic`/`经典`，
            //   `Unranked` = `Unranked Classic`，且后者是**纯字符串形态** en=zh）—— **照原样复刻，⛔ 不修**。
            //   ⚠️ 本族**全仓无消费点**（`grep Battle/MatchType` 在 `Loc.cs` 之外 0 命中）⇒ 纯对齐、不动行为。
            // 🔴 2026-10-23（`D2` · `A2066` ②③）：`C1` 那轮**只换了 ZH、EN 没跟**，而且官方有 2 键我们**没有** ——
            //   `w4_verify.py` ④ 因此红了 2 条（现读：缺 `FastMode` / `FastModeRanked`；`Ranked` 我们 `Ranked`
            //   vs 官方 `Classic`、`Unranked` 我们 `Unranked` vs 官方 `Unranked Classic`）。这一轮按官方表补齐：
            //   判据 = `Unity/数据/游戏数据/官方本地化_en+zh-CN.json` 的 `terms`（`_count` 10,854），现读：
            //   `FastMode` = `Unranked Skirmish` / `未排名的小冲突` · `FastModeRanked` = `Skirmish` / `小规模战斗`。
            //   ⛔ 别把它们并进 `Draft`/`Practice` 之类 —— 原版是 7 个独立键。
            { "Battle/MatchType/ClosedDeck",                           new Entry("选秀", "Draft") },
            { "Battle/MatchType/Draft",                                new Entry("选秀", "Draft") },
            { "Battle/MatchType/FastMode",                             new Entry("未排名的小冲突", "Unranked Skirmish") },
            { "Battle/MatchType/FastModeRanked",                       new Entry("小规模战斗", "Skirmish") },
            { "Battle/MatchType/Practice",                             new Entry("练习", "Practice") },
            { "Battle/MatchType/Ranked",                               new Entry("经典", "Classic") },
            { "Battle/MatchType/Unranked",                             new Entry("Unranked Classic", "Unranked Classic") },
            // ---- Battle/Mulligan（1 条）----
            { "Battle/Mulligan/extraWarlord",                          new Entry("战将技能", "Warlord ability") },
            // ---- Battle/Prebattle（4 条）----
            { "Battle/Prebattle/CasualString",                         new Entry("加载中...", "Loading...") },
            { "Battle/Prebattle/RandomAIString",                       new Entry("随机AI", "Random AI") },
            { "Battle/Prebattle/RandomPlayerString",                   new Entry("随机玩家", "Random Player") },
            { "Battle/Prebattle/RankedSearchString",                   new Entry("搜索中", "Searching") },
            // ---- Battle/Settings（2 条）----
            { "Battle/Settings/Exit",                                  new Entry("退出", "Exit") },
            { "Battle/Settings/LoginButton",                           new Entry("登录", "Log In") },
            // ---- Battle/（裸键）（4 条）----
            { "Battle/SyncError",                                      new Entry("由于连接问题，比赛被取消。不会记录分数。", "Match cancelled due to a connection problem. No score will be recorded.") },
            // ---- Battle/Tips（25 条）----
            { "Battle/Tips/AttackBlock",                               new Entry("你必须攻击先锋单位", "You must attack the Vanguard unit") },
            { "Battle/Tips/AttackBlockFlying",                         new Entry("你不能用近战攻击飞行单位", "You can't attack a flying unit with Melee") },
            { "Battle/Tips/AttackBlockStealth",                        new Entry("你不能针对隐身单位", "You can't target a Stealth unit") },
            { "Battle/Tips/AttackGiantKiller",                         new Entry("你必须攻击战将或前线单位", "You must attack the Warlord or a Front Line unit") },
            { "Battle/Tips/CantAttackDestroyer",                       new Entry("这个军队必须攻击它能摧毁的单位", "This troop must attack the unit it can destroy") },
            { "Battle/Tips/CantAttackFlank",                           new Entry("不是侧翼攻击的有效目标", "Not a valid target for a Flank attack") },
            { "Battle/Tips/CantAttackWithZeroMeleeAttack",             new Entry("这个单位不能进行近战攻击", "This unit cannot attack in Melee mode") },
            { "Battle/Tips/CantAttackWithZeroRangedAttack",            new Entry("这个单位不能进行远程攻击", "This unit cannot attack in Ranged mode") },
            { "Battle/Tips/CantLoseRank",                              new Entry("在当前级别无法失去评级", "Can't lose rating in current division") },
            { "Battle/Tips/DamageFatigue",                             new Entry("你的卡组已空。你因疲劳受到{0}点伤害。", "Your deck is empty. You take {0} damage from fatigue.") },
            // 🆕 **2026-10-19（T17）补登**：`Battle/Tips/DamageFatigueEnemy` —— 官方表两列都在，
            //   而本表**一直没有这一条** ⇒ `BattleDriver.DamageFatigueText(false, n)` 那条**走不到词条**
            //   （`Loc.HasEntry` 假 ⇒ 回我们那两句兜底）。消费点与判据 → `BattleDriver.DamageFatigueTextWithKey`
            //   （原版 `BattleManager._ResolveFatigue_d__587__MoveNext.c` 按敌我二选一）。
            //   值 = 官方 zh-CN / EN 逐字（`官方本地化_en+zh-CN.json` 的 `terms`）。
            { "Battle/Tips/DamageFatigueEnemy",                        new Entry("敌人的卡组已空。你的敌人因疲劳受到{0}点伤害。", "Enemy deck is empty. Your enemy takes {0} damage from fatigue.") },
            { "Battle/Tips/FatigueDamage",                             new Entry("伤害", "damage") },
            { "Battle/Tips/GoSecond",                                  new Entry("敌人先行", "Enemy goes first") },
            { "Battle/Tips/HighELOOpponent",                           new Entry("对手的名字在3000评级以上不会显示。", "The opponent's name isn't shown above 3000 rating.") },
            { "Battle/Tips/InvalidTarget",                             new Entry("无效的目标", "Not a valid target") },
            { "Battle/Tips/MuteEnemyChat",                             new Entry("你可以在设置中静音敌人聊天", "You can mute enemy chat in settings") },
            { "Battle/Tips/NoActions",                                 new Entry("让战斗开始。", "Let the battle begin.") },
            { "Battle/Tips/NoCards",                                   new Entry("没有牌！", "No cards!") },
            { "Battle/Tips/PendingBerzerk",                            new Entry("狂战士单位必须攻击", "Berzerk units must attack") },
            { "Battle/Tips/PleaseWait",                                new Entry("请等待！", "Please wait!") },
            { "Battle/Tips/TutorialAttackDesc",                        new Entry("在战斗中对敌方单位造成伤害", "Damage dealt to enemy unit in combat") },
            { "Battle/Tips/TutorialAttackTitle",                       new Entry("攻击", "Attack") },
            { "Battle/Tips/TutorialCostDesc",                          new Entry("花费法力点数来出这张牌", "Mana points spent to play this card") },
            { "Battle/Tips/TutorialCostTitle",                         new Entry("法力花费", "Mana Cost") },
            { "Battle/Tips/TutorialHealthDesc",                        new Entry("单位如果生命值降至0则死亡", "Unit dies if health reaches 0") },
            { "Battle/Tips/TutorialHealthTitle",                       new Entry("生命值", "Health") },


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
            // 🔴 **2026-10-17（`A1951`）就地订正**：这里原来写「两列文案【都自拟】—— 原版显示串在远端 I2
            //   语言表（本地 84 个本地 bundle 里没有 `localization_assets_all`；`assets_full` 全库 `mTerm`
            //   488 条里一条 `Tips/` 都没有）⇒ 只证到键名、没证到值」—— 官方表 2026-10-14 已在本机：
            //   `Tips/*` 22 条、`Tips/Hud/*` 若干条，**本族凡官方表里有的键，两列都与官方逐字相同**
            //   （🔴 **2026-10-19 现读**：官方 `Tips/*` 现为 **23 条**；「22 条」是 2026-10-14 快照的读数）
            //   （下列各条已逐条核对；官方表里没有的键仍自拟，见各条行尾注）。
            //   · **ZH 列 / EN 列** = 官方两列（原「ZH = `Core/Tooltip.cs` 写死那句、EN 照 ZH 译」的说法作废）。
            //   ✅ **`Tips/Hud/*Count` 原版敌我【各一条键】已照原版拆开**（2026-10-09 · `A1086②`）：
            //     下面 `Player*` 那四条 + 紧随其后补的四条 `Opponent*`。消费点 = `BattleDriver.TickTooltipAt`
            //     （我方图标取 `TipText.{Energy,QuestPoints,Faith,SpiritStone}`、
            //      对面图标取 `TipText.{FoeEnergy,FoeQuestPoints,FoeFaith,FoeSpiritStone}`）。
            //     📌 键名的唯一判据仍是那颗 `EverguildTooltipTrigger.text`，全库逐条计数（亲扫命令见本节上面）：
            //     `Tips/Hud/Player{Energy,Faith,SpiritStone,QP}Count` 与 `Tips/Hud/Opponent{…}Count`
            //     **各 13 颗**（本批实读 `bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4394.json`
            //      = `text = Tips/Hud/OpponentEnergyCount` + `tooltipAnchor = 15` + `offset = (−42.38, −21.40)`，
            //      与同场我方那颗 `MonoBehaviour_4086.json` 的 **anchor 与 offset 逐字相同** ⇒
            //      原版**只靠键**区分敌我，值都从 I2 表取；那张表 2026-10-14 已解出并落进官方本地化 JSON）。
            //     🔴 **`Tips/Hud/Skulls` 全库只有一条**（**没有** `OpponentSkulls`）—— 双方共用同一颗
            //     里程碑计数 ⇒ ⛔ 别硬造一条对面键。
            // ------------------------------------------------------------------------------------------
            // 卡面四个数值 + 费用那条（`TipText.{Melee,Ranged,Armour,Health,Cost}`）
            //  🔴 **2026-10-12（`A1660`）**：这几条的**值**原来在末尾带出处（「（规则书 :78）」「（:147）」），
            //    与我们自加的**关键词 tooltip 出处行同族**、原版没有 ⇒ **值里的出处一并删掉**。
            //    用户口径：「**只写该词条的效果就行，不要多余的出处**」。
            //    ⚠️ **只改值、不删键** —— 这几个键是数值 tooltip 的**正文**（`BattleDriver.TickTooltipAt`
            //       与 `DeckRuntime` 的 TickTooltip 都取它），删键会整条丢正文（`Loc.T` 回键名本身）。
            //    ⚠️ 用途不止 tooltip：`TipText.ByLink` 还把近战/远程那两条用于**效果正文里的行内图标**。
            //    判据 → `资料/普查产出_第二十二会话/DIAG_tooltip出处行.md`；回归见 `BattleScene.cs` §15c。
            { "Tips/MeleeAttackTip",               new Entry("进行或接受近战攻击时所造成的伤害。", "Damage dealt when doing or receiving a Melee attack.") },   // 键名 = 原版（152 颗触发器）· 值 = 官方两列（2026-10-17 订正）
            { "Tips/RangedAttackTip",              new Entry("进行或接受远程攻击时所造成的伤害。", "Damage dealt when doing or receiving a Ranged attack.") }, // 键名 = 原版（152 颗）· 值 = 官方两列（2026-10-17 订正）
            { "Tips/HealthTip",                    new Entry("这个单位在死亡前可以承受的伤害量。杀死敌方战将以赢得比赛！", "Amount of damage this unit can take before dying. Kill the enemy warlord to win the match!") }, // 键名 = 原版（152 颗）· 值 = 官方两列（2026-10-17 订正）
            { "Tips/CostTip",                      new Entry("用来出这张牌的能量。每回合能量会恢复并增加1。", "Energy spent to play this card. Energy is replenished and increases by 1 each turn.") }, // 键名 = 原版（305 颗）· 值 = 官方两列（2026-10-17 订正）
            // 🔴 这一条**键名自拟**（原版无护甲 tooltip，见本节开头那条）+ **我们这边零消费点**
            //   （`BattleDriver.TickTooltipAt` 明确不挂它、`Editor/BattleScene.cs` 还有一条
            //   「**护甲上没有 tooltip**（原版那个容器就没有触发器）」的断言）—— 保留它只为
            //   「同一族五条形状一致」+ 谁哪天要挂的时候不会印出键名。⛔ **别拿它当「原版有」的证据**。
            { "Tips/ArmourTip",                    new Entry("受任何来源的伤害都减这么多，最低减到 1", "Reduces damage from any source by this much, to a minimum of 1") }, // 键名 + 值**全自拟**（原版无）
            // HUD 计数那条（`TipText.{Energy,Skulls,QuestPoints,Faith,SpiritStone}`）
            { "Tips/Hud/Skulls",                   new Entry("减少你对手的生命值以赢得更多的头骨。", "Reduce your opponent's health to win more skulls.") }, // 键名 = 原版（13 颗 · `Milestones`）· 值 = 官方两列（2026-10-17 订正）
            { "Tips/Hud/PlayerEnergyCount",        new Entry("你可用来出牌的能量。", "Your Energy available to play cards.") }, // 键名 = 原版（13 颗）· 值 = 官方两列（2026-10-17 订正）
            { "Tips/Hud/PlayerQPCount",            new Entry("你的任务进度，每获得3个任务进度就向你的卡组中添加一个秘密。", "Your Quest Progress, add a Secret to your deck for each 3 Quest Progress earned.") }, // 键名 = 原版（13 颗）· 值 = 官方两列（2026-10-17 订正）
            { "Tips/Hud/PlayerFaithCount",         new Entry("你的信仰，当你达到他们所需的数量时，可以赋予某些张牌力量。", "Your Faith, which empowers certain cards when your reach their required amount.") }, // 键名 = 原版（13 颗）· 值 = 官方两列（2026-10-17 订正）
            { "Tips/Hud/PlayerSpiritStoneCount",   new Entry("你的灵魂石，用于强化某些牌的消耗品。", "Your Spirit Stones, which are spent to empower certain cards") }, // 键名 = 原版（13 颗）· 值 = 官方两列（2026-10-17 订正）
            // ---- 🔴 **对面那四条**（`A1086②`，2026-10-09 补；原版键名各 **13** 颗，判据同上）----
            //   🔴 **2026-10-17（`A1951`）就地订正**：原写「两列值【全自拟】，原版显示串在远端 I2 表」——
            //      官方表已在本地，`Tips/Hud/Opponent{EnergyCount,QPCount,FaithCount,SpiritStoneCount}`
            //      两列与官方逐字相同。`Tips/Hud/Skulls` 没有对面那一条（双方共用，理由见上）。
            { "Tips/Hud/OpponentEnergyCount",      new Entry("你的对手的能量。", "Your opponent's Energy.") }, // 键名 = 原版（13 颗）· 值 = 官方两列（2026-10-17 订正）
            { "Tips/Hud/OpponentQPCount",          new Entry("你的对手的任务进度", "Your opponent's Quest Progress") }, // 键名 = 原版（13 颗）· 值 = 官方两列（2026-10-17 订正）
            { "Tips/Hud/OpponentFaithCount",       new Entry("你的对手的信仰", "Your opponent's Faith") }, // 键名 = 原版（13 颗）· 值 = 官方两列（2026-10-17 订正）
            { "Tips/Hud/OpponentSpiritStoneCount", new Entry("你对手的灵魂石。", "Your opponent's Spirit Stones") }, // 键名 = 原版（13 颗）· 值 = 官方两列（2026-10-17 订正）
            // ---- `Tips/Trait/*`：🔴 **我们自加的、原版没有**（`Core/Tooltip.cs` 的 `TipText.Trait`）----
            //   原版查不到描述时**就只有「图标 + 标题」、没有正文**（`EverguildTraitTooltipItem`，
            //   见 `资料/tooltip_原版规格与实现.md` §五）；这一句是我们替它补的**如实说明**：
            //     · `NoRulebookEntry`：规则书 61 条（`:161-225`）里没有这个词 ⇒ 明写「没有条目」、**不编解释**。
            //   🔴 两列**全自拟**（原版没有这句）。🚨 它**会上屏**：`Editor/BattleScene.cs` §15c 那条断言
            //     断的就是它（「没有这个词的条目」；⛔ 旧注释写的 `:11007` 是**过期行号**，已按现读订正）。
            //   🔴 **2026-10-12（`A1660`）删掉同族的键 `Tips/Trait/RulebookLine`（原值「（规则书 :{0}）」）** ——
            //     它是我们自加的**出处行**、原版没有（判据：反编译 `TextTooltipController__GetTraitTooltip.c`
            //     只凑标题 + 描述两段；解包预制体 `Trait Tooltip` 的正文只有 `Description` 一个节点，
            //     `Line` 是纯 Image 分隔线）⇒ 用户真机看到「多印一段」后要求「只写该词条的效果」。
            //     **键 / 生成物的 `line` 字段 / 渲染那一句三处一起删**，⛔ 不是只在渲染处 `if` 掉。
            //     判据与依赖顺序 → `资料/普查产出_第二十二会话/DIAG_tooltip出处行.md`。
            { "Tips/Trait/NoRulebookEntry",        new Entry("（规则书里没有这个词的条目）", "(no entry for this term in the rulebook)") }, // ⚠️ 我们加的，不许当成原版
            //   🆕 **2026-10-10（`A1084` 第二步 · 原 `A1086③`）`Tips/Trait/TitleWithEn`**：关键词 tooltip 的
            //   **标题那一行** = 「中文名（英文名）」。🔴 **这是我们加的**（原版只有一条 I2 词条；
            //   2026-10-17 订正：官方表里**没有** `Tips/Trait/TitleWithEn` 这条键 —— 不是「本地取不到」）⇒ 两列自拟。
            //   **为什么单开一条键**：那对全角括号 `（` `）` 原来是 `Core/Tooltip.cs:504` 里的**裸中文字面量**
            //   ⇒ 英文档下标题会印成 `Armour（Armour）` 这种「中文标点 + 重复词」。
            //   ⇒ 形状 = **`{0}（{1}）`** / **`{0} ({1})`**（`{0}` = 显示名、`{1}` = 英文名）；
            //      **两个名字相同（英文档）时调用点【不套这个模板】**，直接给一个名字（见 `TipText.Trait`）。
            { "Tips/Trait/TitleWithEn",            new Entry("{0}（{1}）", "{0} ({1})") }, // ⚠️ 我们加的（原版没有这一层括注）
            //   `Battle/BattleEnd/{Victory,Defeat,Draw}`：**载波①** —— `bundle_menus_assets_all` 的
            //   `BattleLogItem`（`MonoBehaviour_-7028880557435028942.json` / `…_8607776031950241599.json`）
            //   那两个 `LocalizedString` 字段 `victoryKey` / `defeatKey` / `drawKey`（脚本类 = `BattleLogItem`，
            //   `d:/2/Warpforge_code/Scripts/Assembly-CSharp/BattleLogItem.cs`）。
            //   🔴 **落点要说清**：它们挂在**战报条目**上，**不是** `EndBattlePanel` ——
            //   原版结算面板子树**没有标题节点**（胜负靠 `Video Image` 播视频）⇒
            //   **我们结算面板那两句标题本身是自加的**（如实标着），这里取的是**语义相同**的原版键。
            //   英文：`Victory` 有本地实据（那两颗 `result` TMP 的 `m_text = 'Victory'`，fs 65.35）；
            //   🔴 **2026-10-17 订正**：原写「`Defeat` / `Draw` 本地取不到 ⇒ 自拟」—— 官方表已有三条
            //   （`Victory` / `Defeat` / `Draw`），本表三行两列与官方逐字相同（`Draw` 的官方中文就是「领带」）。
            { "Battle/BattleEnd/Victory",          new Entry("胜利",     "Victory") },      // EN = 原版 TMP 原文 `Victory`；中文 = `zh_CN.csv:332`（精确命中）
            { "Battle/BattleEnd/Defeat",           new Entry("失败",     "Defeat") },       // 值 = 官方两列（2026-10-17 订正；原「EN/中文自拟」作废）
            { "Battle/BattleEnd/Draw",             new Entry("领带",     "Draw") },         // 值 = 官方两列（2026-10-17 订正；原「EN/中文自拟」作废）
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
            { "Battle/HUD/CardsInHand",            new Entry("手牌", "Cards in hand") },   // 🔴 2026-10-14（`A1803`）EN 改官方；原写 `Cards left` = **与同族 `CardsLeft` 抄串了**（节点静态 TMP 是 `Cards left: XX`，但**词条真值**才是权威）
            { "Battle/HUD/CardsLeft",              new Entry("剩余牌", "Cards left") },   // EN = 节点静态 TMP；中文**自拟**
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
            //   🔴 **英文列如实标**：这里存的是**原版【显示】出来的那串**（= `ToUpper` 之后）——
            //   🔴 **2026-10-17（`A1951`）就地订正**：原写「词条原文在远端 I2 表、本地取不到」—— 官方表已在本地
            //   （`Battle/HUD/{EndTurn,EnemyTurn,YourTurn}` 两列与官方逐字相同）。本表**不做 `ToUpper`**，⛔ 别再补一层
            //   （本表存的已经是显示形态；原始 TMP 样例见 `battlearena1` 的 `TurnText` = `END TURN`，无 `Localize`）。
            //   ⚠️ 那个 bool 的**语义**是本批按「自己的回合才显示 END TURN」推的（`param_2 != 0 ⇒ EndTurn`），
            //      调用点没找到 ⇒ 只取「这两条键各对应哪个词」这一层，**不搬那个二选一的行为**。
            { "Battle/HUD/EndTurn",                new Entry("结束回合", "End Turn") },     // 🔴 2026-10-14（`A1803`）EN 改官方；⚠️ 卡面那条路走 `CardText.Phrase("END TURN")`（**英文档照原样回大写原文**）⇒ 改这里不影响卡面/`END TURN` 钮的断言
            { "Battle/HUD/EnemyTurn",              new Entry("敌方回合", "Enemy turn") },   // 🔴 2026-10-14（`A1803`）**EN 与 ZH 都改成官方值**（原注写「EN/ZH 都自拟」—— 官方 zh-CN 表里 ZH 逐字就是「敌方回合」，故 ZH 未动）
            // ── `Battle/HUD/YourTurn` ────────────────────────────────────────────────────
            //   `BattleManager.NextTurn`（`BattleManager._NextTurn_d__395__MoveNext.c:480-483`）：
            //   `BattleTipController.ShowHeadsUpMessage(GetTranslation(词条), …)` = **回合开始那句抬头提示**
            //   （`0x4288a48`）。⇒ 它**不是** HUD 里那颗常驻节点的字（原版 HUD 的 `TurnText` 只有 `END TURN`
            //   那一个样例）。🔴 **2026-10-22 补**：我们那条常驻「回合行」已按用户口径**删掉**
            //   （原版没有那个节点）；本条词条**保留** —— 原版确实拿它做**回合开始的抬头提示**
            //   （`BattleTipController.ShowHeadsUpMessage`）；🔴 **2026-10-22 已接**（`BattleDriver.ShowYourTurnHeadsUp`，
            //   不透明底那一档；教程局第 1 回合按原版跳过）。
            { "Battle/HUD/YourTurn",               new Entry("你的回合", "Your turn") },    // 🔴 2026-10-14（`A1803`）EN 改官方（原注「值在远端」**作废** —— 官方 zh-CN 表已到手，两列都逐字对上了）
            // ── `MenuLogin/Guest` ────────────────────────────────────────────────────────
            //   🆕 **2026-10-22**：名牌（`PlayerNameText`/`EnemyNameText`）在**名字为空**时用它 ——
            //   判据 = 原版 `PlayerProfileUIController.SetProfileName` 对空串调
            //   `GetTranslation(0x42D0DE0)`，那个常量在 `stringliteral.json` 里就是 `MenuLogin/Guest`
            //   （两列 = `New player` / `新玩家`）⇒ **这是原版自己的兜底，不是我们编的称呼**。
            //   ⚠️ 消费点 = `BattleDriver.PlateName`（单机局的敌方没有名字数据源 ⇒ 走这一条）。
            { "MenuLogin/Guest",                   new Entry("新玩家", "New player") },
            // ── `Battle/HUD/TargetsAvailable` ────────────────────────────────────────────
            //   `SupportMethods.GetTargetsAvailableText(int n)`
            //   （`SupportMethods__GetTargetsAvailableText.c`，2026-10-18 亲读）= `GetTranslation(词条)`
            //   之后 **`System.String.Replace(词条值, "{0}", n.ToString())`** —— 那个占位符字面量
            //   `0x4265d10` 在 `stringliteral.json` 里就是 **`"{0}"`**（本批逐条读出）。
            //   ⇒ 词条值 = **`{0} available`**（占位符形态）；本地实据 = 那颗节点 `TargetsAvailableText`
            //     的静态 TMP **`0 available`**（= 拿 n=0 渲出来的那一实例）。
            //   🔴 中文列 = `zh_CN.csv:49`（`0 available,~,可用 0`）—— 同样是**带替换值**的实例
            //     ⇒ 模式 = **`可用 {0}`**（比我们原来那句 `可选目标 3` 更贴原版）。
            { "Battle/HUD/TargetsAvailable",       new Entry("{0} 可用",   "{0} available") },
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
            //   🔴 **2026-10-17（`A1951`）就地订正**：原写「英文列的值在远端 I2 表（84 个本地 bundle 里没有
            //      `localization_assets_all`）」—— 官方表已在本地，`Battle/HUD/CreatedBy` 两列与官方逐字相同。
            //      此前本地拿得到的最硬的是**原版预制体里那颗 TMP 的占位串** `Created by someone fancy`
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
            //   消费点 = `d:/2/tools/decomp_full/` 的方法体。
            //   🔴 **2026-10-17（`A1951`）就地订正**：原写「两列文案都是我们自拟的（原版显示串在远端 I2
            //   语言表）」—— 官方表已在本地；本族凡官方表里有的键（`DragToTarget` / `UnitNotReady` …）
            //   两列都与官方逐字相同。
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
            //   🔴 **2026-10-17（`A1951`）就地订正**：原写「EN/ZH 都自拟（值在远端 I2 表；`zh_CN.csv` 按英文源串
            //   精确查过，0 命中）」—— 官方表已在本地，`Battle/Tips/DragToTarget` 两列与官方逐字相同（见下一行行尾注）。
            { "Battle/Tips/DragToTarget",          new Entry("拖动到有效目标", "Drag to a valid target") },   // 🔴 EN 2026-10-14（`A1803`）改官方
            // ── `Battle/Tips/UnitNotReady`（`0x428AAB0`）─────────────────────────────────────────
            //   消费点 = `BattleManager.CanUseActiveAbility`（`BattleManager__CanUseActiveAbility.c:116-132`）：
            //   判据是 `card + 0x230` = **`CardScript.canAct`（`ObscuredBool`）**（`dump.cs` 的字段表实读）
            //   ——`canAct == false` 且调用方要求出声时 ⇒ 这一条 + `NotifyCantDoAction`。
            //   ⇒ 语义 = **「这个单位现在动不了」**（本回合已行动 / 不能行动）。
            //   ⚠️ 我们过去这一档走的是 `CardText.Phrase("THIS UNIT ALREADY ACTED")` ——
            //      `Phrases` 里那条**没有对应 `mTerm`**（`G2b` §3.2 那 17 条之一），
            //      **本批把这条键找出来了** ⇒ 那一处改用本键（`CardText.Phrases` 那条降级成兜底、不删）。
            //   🔴 **2026-10-17（`A1951`）就地订正**：原写「EN/ZH 都自拟（值在远端 I2 表）」—— 官方表已在本地，
            //   `Battle/Tips/UnitNotReady` 两列与官方逐字相同（见下一行行尾注）。
            { "Battle/Tips/UnitNotReady",          new Entry("单位未准备好", "Unit isn't ready") },   // 🔴 EN 2026-10-14（`A1803`）改官方

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
            { "Battle/Effect/ChangeMeleeAttackOneTurn",  new Entry("获得 {0} 近战攻击一回合",  "Get {0} Melee Attack one turn") },   // 🔴 EN 2026-10-14（`A1803`）改官方
            { "Battle/Effect/ChangeRangedAttackOneTurn", new Entry("获得 {0} 远程攻击一回合",  "Get {0} Ranged Attack one turn") },   // 🔴 EN 2026-10-14（`A1803`）改官方
            { "Battle/Effect/ChangeHealthOneTurn",       new Entry("获得 {0} 生命值一回合",  "Get {0} Health one turn") },   // 🔴 EN 2026-10-14（`A1803`）改官方
            { "Battle/Effect/ChangeArmourOneTurn",       new Entry("获得 {0} <sprite name=Atlas_trait_icon_armour> 盔甲一回合",  "Get {0} <sprite name=Atlas_trait_icon_armour> Armour one turn") },   // 🔴 EN 2026-10-14（`A1803`）改官方；那个 `<sprite>` 名在 TMP 图集里**存在**（`Resources/Fonts/Warpforge Trait TextSprites.asset`）⇒ 能解析

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
            // 🔴 **2026-10-17（`A1951`）就地订正**：原写「英文/中文两列都是我们自拟的 —— 原版显示串在远端 I2
            //   语言表里、本地取不到」—— 官方表 2026-10-14 已在本机（`CustomErrors/*` 41 条），
            //   `CustomErrors/ErrorLoadDeck` 两列与官方逐字相同。下面 ①② 两句只说明**prefab 与 zh_CN.csv 里没有**，
            //   不再等于「取不到」：
            //   ① 载波①（prefab `Localize.mTerm`）里**没有**这一条（全库扫 `"mTerm": "CustomErrors/…"`
            //      只命中 **2** 条：`DuplicateConnection` / `ErrorSavingMatch`，都在 13 个战场场景那颗
            //      `BattleErrorUIManager` 上）；② `数据/本地化/i18n/zh_CN.csv` 里**没有**对应的英文源串
            //      （按第一列精确查过，0 命中）。⇒ 照 `MenuDeck/Button/Random` 那条的先例如实标「自拟」。
            // ⚠️ 它**不是**「把写死的中文搬个家」：原来那句是 `DeckStore` 拼的**整句**、而且
            //   `DeckLibrary` 曾经拿 `Contains("失败")` 把它**当类型用**；现在这里只是**同一件事的显示形态**，
            //   判据在 `DeckLibrary.LastLoadIssue` / `LastLoadCode`（枚举）。
            { "CustomErrors/ErrorLoadDeck",             new Entry("加载玩家卡组出错。", "Error loading player deck.") },
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

            // ============================================================ 🆕 **2026-10-25（`E`）：官方 `InvalidDeck` 两族（含两条 `/Short`）**
            //
            // 🔴 **判据（一手）**：官方本地化表 `数据/游戏数据/官方本地化_en+zh-CN.json` 现读
            //   （`MenuDeck/Error/*` 共 **15** 条；其中**只有 2 条**带 `/Short`）：
            //     · `InvalidDeck`                = en `The deck selected is not valid, please edit it or choose another deck.`
            //                                      / zh `所选卡组无效，请编辑或选择其他卡组。`
            //     · `InvalidDeck/Short`          = en `Invalid Deck` / zh `无效卡组`
            //     · `InvalidDeckBannedCards`     = en `The deck has banned cards` / zh `卡组有禁用的张牌`
            //     · `InvalidDeckBannedCards/Short` = **与上一条同值**（官方两列都是同一句）
            // ⚠️ **这两条长键原来不在本表里** ⇒ `RuleEngine.DeckRules.Describe(InvalidDeck)` /
            //   `EffectOnlyCard`（`A1872` 起归 `InvalidDeck`）走到显示层时 `Loc.T` 会把**键名**印出来
            //   （`DeckInfoPopup` 的校验提示就是这条链）。⇒ 这次一并补齐（不是只为 `/Short`）。
            // 🔴 **短档的消费点 = 遭遇事件窗左列那一行**（原版 `SkirmishEventWindow.OnDeckSelected.c:499-505`：
            //   `bannedCardInDeckText.text = DeckUtility.ToShortLocalizedString(deckError)`；
            //   `DeckUtility.ToShortLocalizedString` = `MenuDeck/Error/<枚举名>/Short`、缺键回
            //   `MenuDeck/Error/InvalidDeck/Short`）。引擎侧 = `DeckRules.DescribeShort`（A 2026-10-25 加）。
            // ⛔ **不要给其它 13 条补 `/Short`** —— 官方没有，原版对它们一律回长档。
            // 断言 → `Editor/CardBaseDemo.cs` 的 `AssertDeckErrorShortKeys`（键解析结果 ≠ 键名；删词条即红）。
            // 🔴 **长键 `MenuDeck/Error/InvalidDeck` 不在本段定义** —— 它已在本表 §⑥「卡组不合法那条兜底键」里（见下方
            //   `MenuShop/ExtraLegendaryWarning` 之后那节）。同一张表**不许出现两次同一个键**：这里是字典初始化器，
            //   重复键会抛 `ArgumentException` ⇒ **`Loc` 静态构造失败 ⇒ 任何走本地化的路径全部崩**，而**编译期查不出来**。
            //   （2026-10-25 踩过一次：`CardFaceProbe.Run` 当场 `TypeInitializationException`。）
            { "MenuDeck/Error/InvalidDeck/Short",            new Entry("无效卡组", "Invalid Deck") },
            { "MenuDeck/Error/InvalidDeckBannedCards",       new Entry("卡组有禁用的张牌", "The deck has banned cards") },
            { "MenuDeck/Error/InvalidDeckBannedCards/Short", new Entry("卡组有禁用的张牌", "The deck has banned cards") },

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
            // ⚠️ **这 13 条键名是我们枚举的名字**（原版具名族只有 9 条、名字都不同 ⇒ 官方表里**没有**这些键）
            //   —— **中文那一列 = 改之前 `Describe` 里的原话** ⇒ **今天中文档零变化**；英文那一列是我们译的。
            //   🔴 **2026-10-17（`A1951`）就地订正**：原写「原版那套在远端 I2 语言表、本地 84 个 bundle 里没有
            //   `localization_assets_all.bundle`」—— 官方表已在本地，**问题不是取不到，是这些键原版就没有**。
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
            // 🔴🔴 **2026-10-17（`A1868` + `A1872`）就地订正（铁律 5）**：这一段原来有**两条自造的东西**，现已删除：
            //   ① `MenuDeck/Error/EffectOnlyCard` —— **原版没有这个键**（官方 `MenuDeck/Error/*` 现读 **15 条**
            //      = 13 条具名 + 2 条 `/Short`；原写「只有 9 条具名键」是 2026-10-14 快照的读数，
            //      🔴 2026-10-19 现核订正（铁律 5）），
            //      见 `数据/游戏数据/官方本地化_en+zh-CN.json` 现读）。按 `A1872` 的对位表它归**原版 catch-all
            //      `InvalidDeck`** ⇒ 原版显示的是「所选卡组无效，请编辑或选择其他卡组。」（⛔ 不是「这是效果卡」）。
            //      ✅ **2026-10-25 已收口**（铁律 5 就地订正，原来这句写的是「消费侧还差一半」）：
            //      ① `DeckRules.Describe` 已把 `EffectOnlyCard` 归到 catch-all `MenuDeck/Error/InvalidDeck`；
            //      ② `MenuDeck/Error/InvalidDeck` 键**已进本表**（§⑥ 那条「卡组不合法那条兜底键」，`Loc.cs:1869` 一带）
            //      ⇒ `DeckRuntime.DeckErrorText` / `DeckInfoPopup` 那条链不再印键名。
            //   ② **数字族三条** `MenuDeck/Error/{2,4,5}`（`A1133` 那一批）—— 判据被推翻：
            //      `DeckUtility.ToRawLocalizationString(this DeckError error)` 的 `{0}` 装的是**枚举**，
            //      `ToString()` 印**名字**；且官方 I2 表**一条数字键都没有**。判据全文 = `A1872` 那一格
            //      （`资料/待办判据_第三十三会话.md`）⇒ 换成官方名字族：
            //      `NoWarlord → MissingHero` · `TooFew/TooMany → IncompleteDeck` · 其余 catch-all → `InvalidDeck`
            //      （`InvalidDeck` 早已在表里，见 `MenuDeck/Error/InvalidDeck` 那一条）。
            //      ⚠️ **改键那一半**（`DeckRuntime.MenuDeckErrorNumber` → 出名字）在 `Deck/DeckRuntime.cs`，
            //      本批不动。在它落地之前 `MenuDeckErrorKey` 会退成兜底键 `MenuDeck/Error/InvalidDeck`
            //      （那条在表里 ⇒ **不会印键名**，只会少一级细分）。
            //   两列文案 = **官方值**（现读官方表；⛔ 不是自拟）：
            { "MenuDeck/Error/MissingHero",             new Entry("请为您的卡组选择一个战将，将其拖入您的卡组列表。", "Please choose a Warlord for your deck, by dragging it into your deck list.") },
            { "MenuDeck/Error/IncompleteDeck",          new Entry("您的卡组不完整。您需要将30张牌添加到您的卡组中，将它们拖入左侧面板的卡组列表。", "Your deck is incomplete. You need to add 30 cards to your deck by dragging them into the deck list on the left panel.") },
            // 🆕 **2026-10-25（`E1` · 第六段）：卡组编辑的「整副传奇张数」那一档** —— 
            //   键名里的 **`Legenday`（少一个 r）是原版自己的拼写**，⛔ 别修正（改了 `Loc.T` 就取不到）。
            //   引用点 = `RuleEngine/Core/DeckRules.cs` 的 `Describe`（`DeckError.LegendaryTotalExceeded` 那一档）。
            //   两列文案 = **官方值，逐字取自** `数据/游戏数据/官方本地化_en+zh-CN.json`（2026-10-25 现读）：
            //   EN `Decks in this game mode can contain only 4 legendary cards.` /
            //   ZH「在这个游戏模式中，卡组只能包含4张传奇牌。」（⚠️ 官方中文在「卡组」后有个逗号，照抄）
            { "MenuDeck/Error/MaxLegendayCopiesReached", new Entry("在这个游戏模式中，卡组只能包含4张传奇牌。", "Decks in this game mode can contain only 4 legendary cards.") },

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
            //   🔴 **2026-10-17（`A1951`）就地订正**：原写「EN 列是我们自拟的（原版值在远端 I2 表）」——
            //      官方表已在本地，这四条（`Battle/Tips/{NotEnoughMana,NotYourTurn,NoTargetAvailable,NotEnoughRoom}`）
            //      两列与官方逐字相同（各条目行尾的「改官方」注即此）。
            //   🔴 **中文列 = 改之前 `RuleCodes.Describe` 印出来的那句原话**（`RuleCodes.Names`）⇒ **中文档零变化**。
            //   ✅ **`RuleCodes.cs` 头注与 `BattleDriver.HintForCode` / `HintForCodeWithKey` 那两处**
            //      原来写的「这四条键一条都不在 `Loc` 表里」**已过期、本批已就地订正**（`A1018①`，2026-10-09）。
            //      ⛔ 本表不再写任何 `文件:行号` 引用（行号会漂，一律按符号名认）。
            //   ⚠️ 自检两态是**自适应**的（`Editor/BattleScene` 里那条按 `Loc.HasEntry` 自动选期望值的断言，
            //      §3b ③ 那条只断「不许含 `Battle/Tips/` 前缀、不许为空」）⇒ 本批加键**不会让它们变红**。
            { "Battle/Tips/NotEnoughMana",     new Entry("能量不足",                     "Not enough Energy") },   // 🔴 EN 2026-10-14（`A1803`）改官方（原「EN 自拟」作废；ZH 与官方逐字相同，未动）
            { "Battle/Tips/NotYourTurn",       new Entry("现在不是你的回合",                 "It is not your turn") }, // EN 自拟；ZH = `Describe(ErrNotTurn)` 原话
            { "Battle/Tips/NoTargetAvailable", new Entry("没有有效的目标", "No valid target available") }, // 🔴 EN 2026-10-14（`A1803`）改官方；原「EN 自拟」作废
            { "Battle/Tips/NotEnoughRoom",     new Entry("战场上没有空位",           "No room on the battlefield") }, // 🔴 EN 2026-10-14（`A1803`）改官方；原「EN 自拟」作废
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
            { "MainMenu/General/OK",           new Entry("好的",     "OK") },      // EN = 原版同 GO TMP `m_text` 逐字符；ZH **自拟**（⚠️ 有可见副作用，见上「留痕」块）
            { "MainMenu/General/Cancel",       new Entry("取消",     "Cancel") },  // EN = 原版同 GO TMP `m_text` 逐字符；ZH = `zh_CN.csv:21`（`Cancel,~,取消`，**精确命中**）
            { "MainMenu/General/Discard",      new Entry("放弃更改",     "Discard changes") }, // 🔴 EN **自拟**（本地无 TMP 原文）；ZH **自拟**
            // ---- 🆕 2026-10-17（界面线 T20 · 战役 Header / Premium Panel 三态；3 条）----
            //   三条**官方表实读**（`数据/游戏数据/官方本地化_en+zh-CN.json` 的 `terms`，逐字）：
            //     `Rewards Menu/Points`             = ["Points: {0}", "积分：{0}"]
            //     `MainMenu/General/Collect`        = ["Collect", "收集"]
            //     `MainMenu/PurchasePremium/Premium` = ["Premium", "高级"]
            //   ⚠️ `Rewards Menu/Points` 带 `{0}` 占位 —— 调用方 `CampaignTab` 用 `Replace("{0}", 余额)` 填，
            //      ⛔ 别在这一层做格式化（`Loc.T` 只查表）。
            { "Rewards Menu/Points",            new Entry("积分：{0}", "Points: {0}") },
            { "MainMenu/General/Collect",       new Entry("收集", "Collect") },
            { "MainMenu/PurchasePremium/Premium", new Entry("高级", "Premium") },
            // ---- 🆕 2026-10-17（界面线 T17 · 设置窗三个下拉；7 条官方词条，逐字取自官方表 `terms`）----
            //   `Settings/Media/WindowMode` = ["Window Mode","窗口模式"]
            //   `…/Fullscreen` = ["Full screen","全屏"] · `…/Windowed` = ["Windowed","窗口化"]
            //   `Settings/CardTextInHand/{Default,OnlyDescription,OnlyName}` = ["Full text"/"Only description"/"Only name",
            //                                                                  "全文"/"仅描述"/"仅名称"]
            //   `Settings/Graphics/SelectTextOfCardsInHand` = ["Text for cards in hand","手中的张牌文本"]
            //   ⚠️ 末条 ZH 读着别扭（官方原文如此）—— **逐字照抄，别润色**（先例：错在真值那边 ⇒ 保留原样）。
            { "Settings/Media/WindowMode",              new Entry("窗口模式", "Window Mode") },
            { "Settings/Media/WindowMode/Fullscreen",   new Entry("全屏", "Full screen") },
            { "Settings/Media/WindowMode/Windowed",     new Entry("窗口化", "Windowed") },
            { "Settings/CardTextInHand/Default",        new Entry("全文", "Full text") },
            { "Settings/CardTextInHand/OnlyDescription", new Entry("仅描述", "Only description") },
            { "Settings/CardTextInHand/OnlyName",       new Entry("仅名称", "Only name") },
            { "Settings/Graphics/SelectTextOfCardsInHand", new Entry("手中的张牌文本", "Text for cards in hand") },
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
            //     🔴 **2026-10-24（`A11` · 铁律 5 就地订正）：原来这里写「EN / ZH 都自拟（同上 0 命中）」—— 【错】。**
            //     **官方本地化表两列都在**，且与本 Entry **逐字相同**（`数据/游戏数据/官方本地化_en+zh-CN.json`
            //     的 `terms["MenuShop/ExtraLegendaryWarning"]`，2026-10-24 现读；键名解法 → `资料/全量反编译_入口与用法.md` ⑤）。
            //     ⇒ 这一条的**值一个字不用改**（本来就已经是官方原文），只是**来源**要说清：**官方表实读**，不是我们译的。
            //     错因 = 当年「本地全库扫这个键 0 命中」被读成了「官方没有这条文案」，实际是
            //     **它由远端 Addressables 的本地化包下发、不在客户端安装目录里**（同族教训 → `CLAUDE.md` 铁律 2
            //     那条 2026-10-14 更正：**「不在离线包里」≠「原版没有」**）。
            { "MenuDeck/HUD/DiscardChanges",    new Entry("放弃更改", "Discard changes") },
            { "MenuShop/ExtraLegendaryWarning", new Entry("请记住，您的卡组中只能使用每张传奇牌的一份副本。您仍然可以创建此张牌的更多副本，但它们只能用于升级该张牌。您想要继续吗？",
                                                          "You can only use one copy of each legendary card in your deck. You can buy or craft more copies of this card, but they are only usable to level up the card. Do you wish to continue?") },
            // ---- ⑥ 卡组不合法那条兜底键（1 条）----
            //   键名出处 = 原版 `DeckUtility.ToRawLocalizationString` 查不到词条时的兜底（地址表实读，
            //   见 `资料/普查产出_1011/WB1_A330.md` §2.4；消费点 `Deck/DeckRuntime` 里那个消费点）。
            //   🔴 本地**没有**任何 prefab 用这条 mTerm（全库唯一那条形近的是
            //   `MenuDeck/Error/InvalidDeckBannedCards`，**另一条键**，⛔ 别混）⇒ EN / ZH **都自拟**；
            //   形状照 `MenuDeck/Error/*` 那一族（本表上面那 13 条）。
            { "MenuDeck/Error/InvalidDeck",     new Entry("所选卡组无效，请编辑或选择其他卡组。", "The deck selected is not valid, please edit it or choose another deck.") },
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
            //   🔴 ~~ZH 列 = 我们自译（原版中文在远端 I2 表；`zh_CN.csv` 里没有这条英文串）~~ ——
            //      **2026-10-17（`A1951`）就地订正：这一句已过期，见下面那两行**（两列都换成官方值）。
            //   🔴 **2026-10-14（第二十八会话 · `A1803`）两列都换成官方值**（上面两条「与 prefab 逐字相同」「ZH 我们自译」
            //     的说法**作废**）：官方 zh-CN 表里这条是
            //     `["• Unlock the Premium rewards … a Starter Deck and a Legendary Card in the first node. …"
            //       "• <color=#E98A00FF>{0}</color> Campaign Points …",  "• 解锁此派系的Campaign中的Premium奖励…"]`。
            //     ⚠️ 与我们原来的写法有 **3 处实质差异**：① `Legendary Wildcard` → **`Starter Deck and a Legendary Card`**；
            //     ② `{0}` 外面套了 **`<color=#E98A00FF>…</color>`**（TMP 认，⛔ 别删）；③ ZH 那句整个不同。
            //     ⚠️ **这一条是「字符串拼接形态」**（官方 ZH 用真换行、我们这里写成 `"…\n" + "…"` 三段拼接）——
            //     拼接**只是写法**，运行期取到的仍是同一串（`Loc.T` 返回拼好的那一串）。
            //   消费点 = `Shell/PurchasePremiumWindow` 里那句 `string.Format(TermInfoBody, points)`。
            { "MainMenu/PurchasePremium/Description", new Entry(
                "• 解锁这个派系战役中的高级奖励，包括第一个节点的传奇张牌。\n• <color=#E98A00FF>{0}</color> 每天只需登录，就能在每日登录奖励中为此派系获得Campaign积分。如果你有多个Premium Campaign，那么所有的都可以获得这个奖励！\n• 这个一次性购买将为此派系永久提供利益，因为最后一个Campaign节点可以反复领取！",
                "• Unlock the Premium rewards in this faction's Campaign, including a Starter Deck and a Legendary Card in the first node.\n" +
                "• <color=#E98A00FF>{0}</color> Campaign Points for this faction in the Daily Login Bonus, every day, just for logging in. If you have several Premium Campaigns, you get this bonus for ALL of them!\n" +
                "• This one-time purchase will provide benefits for this faction forever, as the last Campaign node can be claimed repeatedly!") },
            // ---- ⑨ 联盟成员操作弹窗那颗会换字的钮（1 条）----
            //   键名 = **代码字面量**实读（`0x184253cb0`，见 `Shell/AllianceMemberOptionsPopup` 的头注）：
            //   原版按 `role == Admin` 在 `SocialMenu/Alliances/{TransferLeadership,Promote}` 之间二选一。
            //   🔴 本地全库 **0 命中**（无 prefab、无 TMP 原文）⇒ EN / ZH **都自拟**。
            //   ⚠️ 施工单 §⑤ 自己写着「**只用作出声与断言，不换字**」⇒ **本键今天是惰性的**（不改变界面）；
            //     加它只是让那条 `const` 有表可查、波 1/2 真要接时不必回头补。
            { "SocialMenu/Alliances/TransferLeadership", new Entry("转移领导权", "Transfer Leadership") },
            // ---- ⑩ 战斗里那扇「玩家档案」面板的四个词（4 条）----
            //   键名 = 原版 `BattleAlliancePanel` 的四颗 `Localize.mTerm`（13 个战场各一份；本批逐颗实读
            //   `bundle_scenes_scenes_battlearena1/MonoBehaviour/{5137,4243,4495}.json`）。
            //   🔴 **EN 列 = 同一颗 GO 上 TMP 的 `m_text` 原文逐字符**（`MonoBehaviour_{4073,4646,4098,4475}.json`）
            //     = `Name:` / `Title:` / `Alliance:` / `This player is is still not part of an Alliance`
            //     —— 最后那条的 **`is is` 双 is 是原版笔误，照抄勿改**（同 `Shell/AlliancePanelWindow` 里那处）。
            //   🔴 **2026-10-17（`A1951`）就地订正**：原写「ZH 列 = 我们自译（远端 I2 表）」—— 官方表已在本地，
            //   `Battle/AlliancePanel/*` 四条两列与官方逐字相同。
            //   消费点 = `Shell/AlliancePanelWindow.LocOr`（`:152-159`：`Loc.HasEntry ? Loc.T : prefab 英文`）
            //   ⇒ 本批加键之后，**中文档不再印英文**。
            { "Battle/AlliancePanel/PlayerLabel",      new Entry("名称：",   "Name:") },
            { "Battle/AlliancePanel/TitleLabel",       new Entry("标题：",   "Title:") },
            { "Battle/AlliancePanel/AllianceLabel",    new Entry("联盟：",   "Alliance:") },
            { "Battle/AlliancePanel/NotInAnAlliance",  new Entry("该玩家仍未加入联盟",
                                                                  "This player is still not part of an Alliance") },
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
            { "MenuDeck/HUD/DragCardsTip",             new Entry("将牌拖到这里以创建您的卡组", "Drag cards here to create your deck") },
            { "MenuDeck/Error/ImportEmpty",            new Entry("先粘贴卡组串", "Paste a deck string first") },
            { "MenuDeck/Error/ImportBadString",        new Entry("这不是一条合法的卡组串", "This is not a valid deck string") },
            // ⚠️ `{0}` = 落盘失败的原因；消费点用 `Replace("{0}", …)`（不是 `string.Format` —— 文案里有 `**`，先例 `Battle/HUD/CreatedBy`）。
            // 🔴 这一条**必须与 `Shell/CollectionData` 里那句共用**（施工单 §③ 要求逐字一致）—— 两处措辞原来不同，
            // 本表取 `Deck/DeckRuntime.cs` 那一版（带「导入失败：」前缀 + 「（重启就没了）」尾）；波 1b 把 CollectionData 那处对齐过来。
            { "MenuDeck/Error/ImportNotPersisted",     new Entry("导入失败：卡组串读出来了，但没写进存档——{0}（重启就没了）",
                                                                  "Import failed: the deck string was read, but was not written to the save — {0} (it is gone after a restart)") },
            { "Card_Rarity/Common",                    new Entry("常见", "Common") },
            { "Card_Rarity/Rare",                      new Entry("稀有", "Rare") },
            { "Card_Rarity/Epic",                      new Entry("史诗", "Epic") },
            { "Card_Rarity/Legendary",                 new Entry("传奇", "Legendary") },
            { "Card_Rarity/Special",                   new Entry("特别", "Special") },
            // ⚠️ EN 是原版的**复数** `Stratagems` ⇒ 英文档会从我们原来的 `Stratagem` 变成它（P1 §⑤·4 已如实记）。
            { "MenuDeck/HUD/DeckDescription/Minions",  new Entry("军队", "Troops") },
            { "MenuDeck/HUD/DeckDescription/Spells",   new Entry("计谋", "Stratagems") },
            { "MenuDeck/Filters/ShowOwnedOnly",        new Entry("只显示拥有的", "Owned only") },
            { "MenuDeck/Filters/ShowUpgradableOnly",   new Entry("仅显示可升级的", "Upgradable only") },

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
            // 🔴 **2026-10-22（W62-D4）删掉 `Settings/Media/AudioMixerNote`**：原版 `Media Tab`
            //    只有 3 个子件（`Tab Title` / `Audio Settings` / `Visual Settings`），没有那一行说明
            //    ⇒ 键与文案一起删，⛔ 别留死词条（判据 → `资料/普查产出_1022/W62_缺陷诊断.md` §D4）。
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
            { "Demo/MainMenu/ExitGame",                new Entry("退出游戏？", "Exit game?") },
            { "Demo/MainMenu/ExitButton",              new Entry("退出", "Exit") },
            { "Demo/MainMenu/CancelButton",            new Entry("取消", "Cancel") },

            // ---------------------------------------------------------- ⑫·C P3（社交 + 弹窗 + 商店）· 16 条
            //   🔴 判据 = `…/交件_波1_P3_社交与商店.md` §③·A（9 条）+ §③·B（1 条需裁决）+ §⑤·D（1 条）+ §⑥·4（1 条）+ 交件表。
            //   · 卡组串那三句**与 ⑫·A 的 `MenuDeck/Error/Import*` 同一族**（施工单 §③：两处逐字一致）⇒ **不在这里重复建**。
            //   · `MenuDeck/Error/CantStartNoWarlord` = **整句**（调度台当场拍板，交件 §④）：
            //     `MenuDeck/Error/NoWarlord`（中文「还没有选战将」）那条**短键留着**给 4 处「未选战将」用；
            //     弹窗正文原来那句「这套卡组还没有选战将，开不了局。」**不许缩水** ⇒ 另立本条（两处：`LiveOpsEventWindow` / `PracticeModePopup`）。
            //   · `MenuDeck/Error/WrongGameMode` / `…Deck` = **两句不同的整句**（P3 只报了一个键名，但代码里那两处是两句话）
            //     ⇒ 拆成两条，键名照 P3 那条 + `Deck` 后缀。
            //   · `MenuDeck/GameMode/{Skirmish,Classic}Tag` = 内嵌的「遭遇 · 12 张」（`:280/:292`）。
            //     🔴 **2026-10-23 订正（铁律 5）**：这里原来还写着「与 `{Skirmish,Classic}` 各两条」——
            //     那两条**不带 `Tag`** 的**全仓零引用**、已删（见下面那一行注释）。
            //   · `MenuDeck/CantImportDeck` 的**键名是原版**：`stringliteral.json` `0x42CEBF0` ——
            //     正是 P3 §⑥·4 写的那个 `DAT_1842cebf0`（VA − ImageBase 0x180000000 = RVA 0x42CEBF0）⇒ **查到就用它**。
            { "Settings/Online/MatchCancelled",        new Entry("已经取消这一局的联机匹配 —— 对面会收到通知，双方都没有开局。\n想再打一次：两边各自重新点一次 `Battle!`。",
                                                                  "The online match for this battle was cancelled — the other side gets a notice and neither side has started.\nTo try again: both sides press `Battle!` once more.") },
            { "Settings/Online/MatchCancelFailed",     new Entry("取消不了这一局：{0}", "Could not cancel this battle: {0}") },
            // 🔴 2026-10-23（卡牌线 `A8` · 主对话已裁）：这里原来还有两条**不带 `Tag`** 的
            //   `MenuDeck/GameMode/{Skirmish,Classic}`（「遭遇战（Skirmish · 12 张）」/「经典（Classic · 30 张）」）——
            //   **全仓零引用**，已删。判据：`grep -rn "MenuDeck/GameMode" --include=*.cs .` 只剩下面两条 `…Tag`
            //   （消费点 = `Shell/LiveOpsEventWindow.cs` 那两处：`Menus/Play/Event/…` 的模式名）＋ 注释。
            //   ⛔ **`{Skirmish,Classic}Tag` 这两条必须留着**（它们是活的）。
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
            // 🔴 **2026-10-17（界面线 `A1852`）两列换成官方原文（带 `{0}`）** —— 判据 =
            //   `Resources/official_loc_13lang.json` 现读（键 `MenuDeck/CantImportDeck`）；
            //   `{0}` 的语义 = **这副卡组督军的阵营名**
            //   （`decomp_full/DeckInfoPopup__EditDeck.c`：`string.Format(GetTranslation("MenuDeck/CantImportDeck"),
            //   GameStaticData.CardArmyToString(deck.Warlord.Army))`）。
            //   ⚠️ 唯一拼装点 = `Shell/DeckInfoPopup.cs` 的 `CantImportDeckMessage`（⛔ 别在别处再拼一遍）。
            { "MenuDeck/CantImportDeck",               new Entry("无法导入卡组。你没有足够的{0}张牌。", "Can't import deck. You don't have enough {0} cards.") },
            { "MenuDeck/Error/CantStartNoWarlord",     new Entry("这套卡组还没有选战将，开不了局。",
                                                                  "This deck has no Warlord chosen yet, so the battle cannot start.") },

            // ---------------------------------------------------------- ⑫·D P4（战斗 HUD / 各窗口）· 33 条
            //   🔴 判据 = `…/交件_波1_P4_战斗HUD与窗口.md` §⑤（那张 ready-to-paste 的键表）。
            //   ⛔ **两族【故意没建】**（不是漏，逐条写清理由）：
            //     · **战斗日志那 11 句模板**（`BattleDriver` 里那一族）—— 同文件那段（`G5`）的 in-code 判据写着
            //       「它们没有原版 `mTerm`，接的时候要和这次重构一起决定键名，**⛔ 别先自造一批键**」；P4 也「建议【先不加】」
            //       ⇒ 等重构那一轮（**要做，只是先后** —— 铁律 11）。
            //       ✅ **2026-10-14（`A985⑥③` · L 波）就地订正（铁律 5）：这一格【已办】—— 那一轮就是本会话。**
            //          日志重构（`A985⑥` 的 ③ 半）曾把那 12 句模板改走 `Battle/Log/*`。
            //          🔴 **2026-10-22 再订正（用户口径「战斗里原版没有的设计不要」）**：那一族**整族删了** ——
            //          原版 `CemeteryActionType` 只有九种**动作**（无伤害/治疗/阵亡/回手/触发/部署），
            //          日志行只有 `Battle/Cemetery/*` 那 18 条 ⇒ 12 条自拟键与它们的调用分支一起移除。
            //     · **`Battle/CardWindow/TapToClose`** —— in-code 判据写着「原版子树没这个节点、`Battle/` 93 条字面量里也没有键
            //       ⇒ 铁律 11 例外①」⇒ 当年**没有建**这条键。🔴 **2026-10-22**：那颗自拟提示
            //       （`cdw_hint`）**连节点一起删了** ⇒ 这条键**永远不需要建**（不是「漏建」）。
            //   ⚠️ `Battle/{Hint,Log,Chat,MultiCard,ChooseCard,CardWindow}/` 整族在 `stringliteral.json` 里**不存在**
            //     （P4 逐前缀核过：`Battle/` 93 条 · `MainMenu/` 86 · `Settings/` 10 · `Tips/` 8 · `MenuShop/` 13）⇒ **这一族全自拟**。
            //   🔴 **2026-10-14（`A1803`）就地订正**：这一段原来写「原版标签来自远端 I2、本地没有任何一处能印真标签」
            //     —— **不成立**：官方表里 `Battle/Chat/{Greetings,Threat,WellPlayed,Taunt,Sorry,Oops}` 六条**都在**
            //     （`Greetings` 那条已随本会话的 105 条进表，本表原来那条键名写错的 `Battle/Chat/Greet` 已删）。
            //     ⛔ **但今天仍印不出真标签** —— 钮上的字走 `Battle/ChatPopupPanel` 里那颗**硬编码数组**
            //     `{ "Greet", "Threat", "Well Played", … }`，**没接本表** ⇒ 那一步要改 `Battle/ChatPopupPanel.cs`
            //     （⛔ 不在本笔白名单，已记进 W4 报告）。
            //     ⚠️ 同时订正：下面那 5 条里除 `WellPlayed` 的 EN（本会话已换成官方的 `Well played`）外，
            //     ZH **本来就是官方值**（原来标「自拟」是错的）。
            //   ⚠️ `Battle/BattleEnd/Rounds` 带**三个尾空格**（照 `EndPanel` 里那句 `$"{rounds} 回合   "` 的拼法）；
            //     `RoundsOnly` / `RoundsMinFoeHealth` 是另外两种整句（同文件那三行）—— ⛔ 三条别合并（值不同）。
            // 🔴 **2026-10-22（用户口径「战斗里原版没有的设计不要」）：`Battle/Hint/*` 一族【整族删除】。**
            //    原来那 11 条（`MulliganDone` / `Reconnected` / `DeckLoadFailed` / `MulliganPick` /
            //    `MulliganSent` / `ReplacedCount` / `OffensivePhase` / `DefensivePhase` / `ChooseCard` /
            //    `ChooseOption` / `NoRestartOnline`）是**我们自拟的提示句**，没有原版权威
            //    （官方表 `Battle/Hint/*` 一族 **0 条**）⇒ 句子与词条一起删（调用点 → `BattleDriver.SetHint` 的 doc）。
            //    ⚠️ 官方那条**提示通道**照旧（`Battle/Tips/*` / `Battle/Codes/*` 等），只是不再有自拟句。
            { "Battle/ChooseCard/Offensive",           new Entry("选择进攻卡", "Choose an offensive card") },
            { "Battle/ChooseCard/Defensive",           new Entry("选择防御卡", "Choose a defensive card") },
            { "Battle/MultiCard/Title",                new Entry("你的牌库", "Your deck") },
            // 🔴 **2026-10-22**：`Battle/MultiCard/TapToClose`（「点「继续」或再点一下牌堆关闭」）**已删** ——
            //   那是我们自拟的底部提示（原版 `Generic Multi Card Display Combat` 子树里没有这一行），
            //   句子与词条一起删（调用点 → `Battle/MultiCardDisplay.cs`）。
            // 🔴 **2026-10-22（用户口径「战斗里原版没有的设计不要」）：`Battle/Log/*` 那一族【整族删除】。**
            //    原来那 16 条（`SideMe` / `SideFoe` / `TurnPrefix` / `EffectFallback` /
            //    `Play` / `Deploy` / `Attack` / `Melee` / `Ranged` / `Heal` / `Damage` / `Death` /
            //    `Return` / `Ability` / `Trigger` / `Bare`）是**我们自拟的日志措辞**；
            //    判据 = 原版 `CemeteryActionType` 只有九种**动作**（无伤害/治疗/阵亡/回手/触发/部署，
            //    `dump.cs:45490-45503`），行文字就是 `CemeteryManager.GetActionText` 的返回值
            //    （外面不套「回合 N」/「我方·敌方」）⇒ 这些键与它们的调用分支一并删。
            //    ⚠️ 现存的日志行**全部**走官方键 `Battle/Cemetery/*`（本节上面那一族）。
            // 🔴 **2026-10-19（T17）核过：下面这 5 条 `Battle/BattleEnd/*` 是【我们加的，非原版】** ——
            //   官方表 `Battle/BattleEnd/` 只有 11 条（`CantLoseRating` · `DamageDone` · `Defeat` · `Draw` ·
            //   `Location` · `TapToContinue` · `TapToContinueMobile` · `Time` · `Title` · `TroopsDead` · `Victory`），
            //   **没有** `ForfeitMe` / `ForfeitFoe` / `Rounds` / `RoundsMinFoeHealth` / `RoundsOnly`；
            //   原版 `EndBattlePanel` 子树里也**没有**标题/副标题节点（W22 已查实）⇒ 这 5 条只服务于
            //   `Battle/EndPanel` 自己拼的那行副标题。**保留**（铁律 3：原版没有的，如实标「我们加的」），
            //   ⛔ 别把它们当成「官方有、我们抄漏」。
            { "Battle/BattleEnd/ForfeitMe",            new Entry("我方投降", "we forfeited") },          // ⚠️ 我们加的，非原版
            { "Battle/BattleEnd/ForfeitFoe",           new Entry("对方投降", "the opponent forfeited") }, // ⚠️ 我们加的，非原版
            { "Battle/BattleEnd/Rounds",               new Entry("{0} 回合   ", "{0} rounds   ") },      // ⚠️ 我们加的，非原版
            { "Battle/BattleEnd/RoundsMinFoeHealth",   new Entry("{0} 回合   敌方战将最低生命 {1}",
                                                                  "{0} rounds   enemy Warlord's lowest health {1}") }, // ⚠️ 我们加的，非原版
            { "Battle/BattleEnd/RoundsOnly",           new Entry("{0} 回合", "{0} rounds") },            // ⚠️ 我们加的，非原版
                        { "Battle/Chat/Threat",                    new Entry("威胁", "Threat") },
            { "Battle/Chat/WellPlayed",                new Entry("打得好", "Well played") },   // 🔴 EN 2026-10-14（`A1803`）改官方（小写 p）
            { "Battle/Chat/Taunt",                     new Entry("嘲讽", "Taunt") },
            { "Battle/Chat/Sorry",                     new Entry("抱歉", "Sorry") },
            { "Battle/Chat/Oops",                      new Entry("哎呀", "Oops") },
            // 🔴 **2026-10-22（用户口径「战斗里原版没有的设计不要」）**：这两条**已删** ——
            //    · `Battle/Settings/Title`（「设置 / Settings」）：原版 `BattleSettingsPanel` **没有标题节点**
            //      （判据 → `资料/普查产出_1022/W62_战斗非原版元素审计.md` C 表第 3 行）；我们那颗自拟标题已删。
            //    · `Battle/Settings/AiDifficulty`（「AI 难度」）：原版**没有玩家可见的难度开关**（C 表第 4 行）；
            //      那一行已整行删。
            //    ⚠️ 官方 `Battle/Settings/` 只有 `Exit` / `ResignButton` / `SkipTutorial` 三条（本表前两条在上面）。
            { "MainMenu/Campaign/Points",              new Entry("战役点数：{0}", "Points: {0}") },
            // ============================================================ ⑬ 双语③ 波 0b2 · P6（`Net/` 整片）· 76 条
            //
            //  判据 = `资料/普查产出_第四会话/施工单_双语_Net整片_P6.md` §③ 那张缺键表（逐条照建）。
            //  范围 = `Net/` 那 9 份 `.cs` 里**玩家看得见**的字（弹窗 / 提示行 / 设置窗「联机」页那一行）；
            //    ⛔ 走线文案（`Wire/*` 7 条 · 穿 TCP 到**对端屏幕**，`bye`/`ack`/`reject` 的 `reason`）**本轮不建** ——
            //       接 `Loc.T` 等于把发送方的语言灌到接收方界面上 ⇒ 单开 `P6d`（发键标识、接收侧取词）；
            //    ⛔ `Battle/Log/*` 12 句**也不建**（`Battle/BattleDriver` 里那段 in-code 判据：
            //       「原版记动作、我们记后果，接的时候和日志重构一起定键名 —— ⛔ 别先自造一批键」；要做、只是先后）。
            //       ✅ **2026-10-14（`A985⑥③` · L 波）就地订正（铁律 5）：这半句【已过期】—— 日志重构那一轮就是本会话。**
            //          那 12 句**已建**：键 = `Battle/Log/*`，两列值见 §⑫·D 那一小节（`EffectFallback` 之后那 12 行）——
            //          **中文列 = 调用点原话逐字**（⇒ 中文档显示零变化）、英文列 = 调用点那句英文的逐字副本；
            //          ⛔ **不是「先自造一批键」**：键名出处 = `…/施工单_双语③逐处换key_附_BattleDeck.md:44-54`。
            //       🔴 **同一段上面那半句（`Wire/*` 7 条）【仍然成立】**（那 7 条**今天仍不建**，理由照旧）⇒ ⛔ 别一起当成过期。
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
            //     **照抄保留** —— 去掉它就等于悄悄改了中文档。
            //     🔴 **2026-10-17 订正**：这条原来举的例子 `MenuDeck/Error/EffectOnlyCard` 已按 `A1868` 删除
            //     ⇒ **例子作废**；同句里的「本表既有 14 条同款」本轮**没复核**（照抄时以调用点原文为准）。
            //
            //  ---------------------------------------------------------- ⑬·A `Settings/Online/St/*` · **46 条**
            //  🔴 2026-10-19 现核订正（铁律 5）：原写「45 条」，现读块内 **46 条**（本块后续有增补）。
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

            //  ---------------------------------------------------------- ⑬·B `Settings/Online/Lobby/*` · **19 条**
            //  🔴 2026-10-19 现核订正（铁律 5）：原写「17 条」，现读块内 **19 条**（与下面「这 19 处」一致）。
            //  出处：`NetMatchmaking.cs` `:197`(+`:198-200`) · `:212`(+`:213-215`) · `:249` · `:233` · `:360/:362/:363`
            //    · `:402-406` · `:427` · `:466-467` · `:502-503` · `:509-510` · `:539-540` · `:587`
            //  ⚠️ 这 19 处里 `PeerLostHint`/`PeerLeftHint` 是**提示行**（尺子 = `Shell/SearchingMatchPopup` 的
            //     `HintLineWidth(句) > HintLineMaxWidth`，**80 个半宽字位**；其中 `HintLineMaxChars = 40` 是
            //     **中文档**那一档的上限 —— ⛔ 别再写成「40 字」的预算（那是 `A1083` 改尺子**之前**的口径）。
            //     超了会 `LogWarning`）⇒ P6 §⑤ 要求自检额外断 `Loc.T(键).Length <= SearchingMatchPopup.HintLineMaxChars`
            //     （**只量中文列**：字符数 ≤ 40 ⇔ 全宽 ≤ 80 位，是同一条尺子的**更严**半边，见那条常量的 doc）。
            //  ⚠️ `{0}`/`{1}` 的含义逐条不同（`PeerLeftHint` 的 `{0}` = 对方报的离开理由、`{1}` = 本地撤销那半句）⇒ 拼法见调用点。
            //  ==========================================================================================
            //  🔴 **2026-10-10（`A1085`）：本块这几条的【英文列】整体压短 —— 判据 + 逐路径长度账**
            //
            //  **为什么**：提示行那把尺子（`Shell/SearchingMatchPopup.HintLineWidth(句) > HintLineMaxWidth`
            //   = **80 个半宽字位**，`A1083` 定的）**只对中文列算过**。`A1083` 换尺子之后**英文档仍然全超**：
            //   按原英文列实测 5 条路径 = **91 / 105 / 106 / 110~124 / 140 位**，**一条都装不进 80**
            //   ⇒ 英文档下 `ShowHint` **每次都喊**（那是一个「只会喊、且喊得准」的告警，等于没有告警）。
            //  ⇒ `A1085` 二选一，本笔选 **「改英文列的文案长度」**（⛔ 不改窗口布局、⛔ 不截断）：
            //   · ⛔ **不能改布局**：那行字的框（700×148）与自适应档（4~50px）是**照原版 prefab 读的**
            //     （`Shell/SearchingMatchPopup` 那颗 `Main Search message`）—— 改它就是偏离原版；
            //   · ⛔ **不能截断**：整句就是「告诉玩家刚才发生了什么」，截了 = 把该玩家看的话吃掉
            //     （`A1083` 已把这条判据写死在那扇窗的头部注释里）。
            //   · 而**这两列文案都是我们自拟的**（键族 `Lobby/*` 两张原版表都 0 命中）⇒ 改它**不碰原版**。
            //
            //  **改法（一句话）：让 EN 列的宽度 ≈ ZH 列的宽度** —— 中文那几条本来就都在 80 位以内，
            //   英文原来同样是「同一句话」却宽出 40~70%，那部分是**英文写得啰嗦**，不是信息多。
            //   ⇒ 逐条压到与中文列**同量级**，⛔ 没有删掉任何一条信息（掉线/离开是谁 · 这一局撤没撤 ·
            //     两边都要再点一次 `Battle!` · 已经开局正在进战场）。
            //
            //  **逐条（改前 → 改后，单位 = 半宽字位；中文列不动）**：
            //   | 键（`Lobby/…`） | ZH | EN 改前 | EN 改后 |
            //   |---|---|---|---|
            //   | `PeerLostHint`        | 45 | 78 | **54** |
            //   | `DeferToBattle`       | 47 | 88 | **47** |
            //   | `LobbyRestored`       | 64 | 91 | **77** |
            //   | `MatchRevoked`        | 20 | 35 | **19** |
            //   | `NotMatchingThisGame` | 28 | 49 | **25** |
            //   （`PeerLeftHint` 20/22 · `PeerLostFrag` 10/21 · `PeerLeftFrag` 12/15 **本来就够短，没动**。）
            //  **改后逐路径实测（`HintLineWidth` 那套区间算的，可复算）**：
            //   | 路径 | 改前 EN | 改后 EN |
            //   |---|---|---|
            //   | `_started` 掉线 `DeferToBattle{PeerLostFrag}`        | 106 | **65** ✅ |
            //   | `_started` 离开 `DeferToBattle{PeerLeftFrag+body}`   | 140 | **59**（`body` = 0）· **79**（`body` = 20 位）✅ |
            //   | 未开局掉线 `PeerLostHint{MatchRevoked}`              | 110 | **70** ✅ |
            //   | 未开局掉线 `PeerLostHint{NotMatchingThisGame}`       | 124 | **76** ✅ |
            //   | 未开局离开 `PeerLeftHint{body,tail}`                 | 105 | **41**（`body` = 0）✅ |
            //   | `Reset` 后对面回来 `LobbyRestored`                   |  91 | **77** ✅ |
            //  ⚠️ **仍然超的两格（如实记，不是没改完）**：夹着**对端原样文本**的那两条 ——
            //   本机收侧把 `body` **钳到 40 个字符**（`NetProtocol.MaxPeerTextChars` / `NetSession.ClampPeerText`），
            //   若对端真报满 40 个 ASCII ⇒ `DeferToBattle{PeerLeftFrag+body}` = **99** ·
            //   `PeerLeftHint{body,NotMatching}` = **81**。🔴 **这一格我们改不了**：`body` 的长度是
            //   **协议层的钳**、不在本表的白名单里；而且**中文档同一格也一样超**（`body` 满 40 汉字 = 80 位）
            //   ⇒ 这是「那一行本来就装不下一条 40 字的对端理由」，**与语档无关**、也不是英文列写得啰嗦。
            //   ⚠️ **改完这两条 EN 列宽度已 ≈ ZH 列**（47 vs 47 · 54 vs 45 · 77 vs 64 …）⇒ **英文档的相对劣势已经消掉**；
            //     剩下的是那条**共有的**结构性问题（要真收口只能改 `MaxPeerTextChars`／改框，两者都不在本笔白名单）。
            //  ✅ **中文档零变化**（本笔一个 ZH 列都没动）⇒ `Editor/NetSelfTest.cs` 那两条按 `Loc.T(...).Length`
            //    ≤ `HintLineMaxChars`(40) 断**中文列**的检查**一字不用改**。
            //  ==========================================================================================
            { "Settings/Online/Lobby/PeerLostHint",         new Entry("对面掉线了，{0}（两边回来各点一次 `Battle!`）",
                                                            "Opponent disconnected, {0} (press `Battle!` once back)") },
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
            { "Settings/Online/Lobby/MatchRevoked",         new Entry("这一局的匹配已经撤销", "Match setup revoked") },   // EN 压短（A1085：35 → 19 位）
            { "Settings/Online/Lobby/NotMatchingThisGame",  new Entry("（本机本来就没在匹配这一局）",
                                                            "(not matching this match)") },                              // EN 压短（A1085：49 → 25 位）
            { "Settings/Online/Lobby/DeferToBattle",        new Entry("{0} —— 已开局、正在进战场（后面由对局那一层说）",
                                                            "{0} — match already started, entering the arena") },        // EN 压短（A1085：88 → 47 位）
            { "Settings/Online/Lobby/BotNoLink",            new Entry("联机没连上", "Not connected") },
            { "Settings/Online/Lobby/BotSessionNotReady",   new Entry("联机会话现在是 `{0}`（还没握手完）",
                                                            "The session is now `{0}` (handshake not finished)") },
            { "Settings/Online/Lobby/BotEmptyDeck",         new Entry("这副牌是空的", "This deck is empty") },
            { "Settings/Online/Lobby/PlayedVsBot",          new Entry("这一局打的是电脑，不是联机。\n原因：{0}。\n你在设置里配过联机了 —— 请到「设置 → 联机」点一次{1}，再回来点 `Battle!`。",
                                                            "This match is against the AI, not online.\nReason: {0}.\nYou have configured online play — go to Settings → Online and press {1} once, then press `Battle!` again.") },
            { "Settings/Online/Lobby/LobbyRestored",        new Entry("对面回来了 —— 联机已恢复。要开这一局，两边重新各点一次 `Battle!`",
                                                            "Opponent is back — online play restored. Both press `Battle!` again to start.") },   // EN 压短（A1085：91 → 77 位）
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
            //  ⚠️ **⑭·A / ⑭·B 的 EN 列来源**：本地 prefab **整包被本地化成西班牙语**
            //     （`Seleccionar Calidad` / `Aumentar tamaño de UI` / `Sobremuestreo` / `Límite de FPS` / `Ilimitado`）。
            //     🔴 **2026-10-17（`A1951`）就地订正**：原写「英文原文在远端 I2 表 ⇒ EN 取我们界面写死的那串、
            //     ⛔ 不是原版原文」—— 官方表已在本地（`Settings/Graphics/*` 14 条），下列各条两列与官方逐字相同。
            //     🔴 **2026-10-19 现读**：官方 `Settings/Graphics/*` 现为 **17 条**（「14 条」是 2026-10-14 快照读数）。
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
            { "Settings/Graphics/SelectQuality",       new Entry("选择质量",     "Select Quality") },            // 节点 `…/Quality  Selector/Quality selector text`（TMP = `Seleccionar Calidad`，西语）· EN 自拟
            { "Settings/Graphics/IncreaseUISize",      new Entry("增大用户界面尺寸",  "Large font size") },    // 节点 `…/Content/Small Screen Size Toggle/Label`（TMP = `Aumentar tamaño de UI`）· EN 自拟
            { "Settings/Graphics/EnableSuperSampling", new Entry("超采样",   "Supersampling") }, // 节点 `…/Content/Use super sampling/Label`（TMP = `Sobremuestreo`）· EN 自拟
            { "Settings/Graphics/Vsync",               new Entry("VSync",    "VSync") },              // 节点 `…/Content/Vsync/Label`；🔴 EN **逐字 = 原版 TMP**（本包唯一一条非西语）
            // 🔴 2026-10-23（`C1`）：**ZH 两列换成官方值**（`FPS限制` / `无限制`）；EN 仍自拟（官方 = `FPS Limit` / `Unlimited`，
            //   只有大小写差）—— 本批只碰中文值 ⇒ EN 未动。
            { "Settings/Graphics/FrameLimit",          new Entry("FPS限制", "FPS limit") },          // 节点 `…/Content/FPS Limit/Title`（TMP = `Límite de FPS`）· EN 自拟
            { "Settings/Graphics/UnlimitedFPS",        new Entry("无限制",   "Unlimited") },          // 节点 `…/Content/FPS Limit/FPS Slider/Unlimited`（TMP = `Ilimitado`）· EN 自拟

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
            { "Settings/Graphics/Flash/Quality",       new Entry("画质档 → {0}", "Quality → {0}") },               // `:1987`（画质下拉选中那一档）
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

            // ============================================================ 🆕 **2026-10-10（`A1084`）：卡面/HUD 那一族中文进表**
            //
            // 🔴 **为什么**：这几族原来全在 `Core/CardText.cs` 的**静态字典**里（键 = 英文原文或 canonical 键），
            //   与 `Loc` 是**两条并行的取值路径** —— 工程红线「两处写同一条规则 = 迟早不一致」。
            //   搬进本表之后：**判据只此一份**，`CardText` 那边一律转发（口径 = `TermOr`：表里有 ⇒ 按语档取，
            //   表里没有 ⇒ 出声一次 + 回原来的兜底）。
            //
            // ⛔ **不许「只搬字面量」** —— 同批必须处理两件（普查 §5.3·1 点名）：
            //   ① `CardText.AllChinese()`（**字体语料**的唯一来源，`Editor/TmpSetup.Corpus()` 读它）
            //      ⇒ 搬完它改成内转 `Loc.AllChinese()`（本表的中文列**天然进语料**）；
            //   ② `CardText.Zh`（**字体闸 + 语言闸**）—— 它**留着**，转发一律经 `Term`/`TermOr` 这两道闸。
            //   ⚠️ **`AllChinese()` 那两条自检（`TmpSetup.CheckCoverage` + `BattleScene` §14b4c）读的仍是
            //     `CardText.AllChinese()`** ⇒ 它必须继续把这几族**全覆盖**（本批已逐条核过：改成内转之后
            //     语料只多不少，见 `Editor/CardBaseDemo.cs` 的那条判别式断言）。
            // ⚠️ **`KeywordZhAliases`（`装甲`/`爆破`）【没搬】** —— 它不上屏、只参与「卡面这一段印过没有」的比对，
            //   搬走会让 5 张卡（`AM13/AM28/AM34/AM37/AM54`）**重复印一遍关键词**（判据 → `CardText.cs:205-214`）。
            //
            // **载体/键名的出处，逐族**：
            //   · `Card_Name/*`：**族名照原版**（原版那 8 条 `Card_Name/{EC21,EC57,…}` 是异画/特例），
            //     **这 26 条键名自拟** —— 我们自造的 26 张起始卡在原版两张表里**都查不到**（普查 §④）。
            //   · `Card_Trait/*`：**族 = 原版**（`GameStaticData.TraitNameToString` 拼的就是 `"Card_Trait/" + 枚举名`，
            //     转述见 `资料/普查产出_1018/V-OATH_Oath徽记与记号.md:130-136`；**裸前缀 `Card_Trait/` 在表②**）。
            //     🔴 **2026-10-17（`A1951`）就地订正**：原写「值取不到（原版 I2 词条在远端 CCD；`Card_Trait/`
            //     在 `assets_full` 全库 0 命中）」—— 官方表已在本地（`Card_Trait/*` 77 条；🔴 **2026-10-19
            //     现读 = 83 条**，77 是 2026-10-14 快照的读数）。⚠️ 我们的值
            //     **仍是我们自己的写法**：其中 `Card_Trait` 那 **28** 条属于文件头那批「**暂缓换回**」
            //     （🔴 2026-10-19 现核：与官方 ZH 不同的 `Card_Trait` 共 28 条、逐条等于换官方前的旧值；
            //       原写「25 条」已过期；旧值还出现在卡面 `descZh` 里）⇒ 不是「取不到」。
            //     🔴 **2026-10-23（卡牌线 `C1`）那 28 条已全部换成官方值**（护甲→盔甲 · 潜行→隐身 ·
            //     爆裂→爆炸 · 天赋→才能 · 典籍→圣典 · 猛击→攻击 · 临时→短暂的 · 护盾→盾牌 · 路标石→灵魂石 …）；
            //     ⇒ **本族 ZH 列现在逐条 = 官方 `Card_Trait/*`**（`bloodthirst`/`cantattack`/`huntmark`/
            //     `spiritstone` 那 4 条**大小写不同**的键仍是我们自己的写法、官方表里没有同名键 ⇒ 保留，见报告 §五）。
            //     ⚠️ 卡面正文里那批**自译 `descZh`** 还没迁完 ⇒ 会看到章/正文两套词，
            //     那是卡牌线的余额（判据 → `资料/普查产出_1023c/官方中文全量收口_执行报告.md`）。
            //   · `Armies/*`：**族 = 原版**（那 14 条 `mTerm` 在表① 全在；`Ember`/`Tide`/`Neutral` 原版无 ⇒ 自拟）。
            //     ⚠️ **载体没核**（普查 §⑦·2）：那 14 颗 `Localize` 挂在**主菜单阵营选择页**，
            //        **不一定是卡面那行阵营字**（卡面那行按 `CardText.cs:459` 的注释来自 `factions.json` 的 `cn`）
            //        —— 如实标「键名可用、载体待核」，⛔ 别写成「原版卡面就从这儿取」。
            //   · `Battle/Phrase/*` 与 `CardEffect/*`：**两族都是自拟**（原版两张表 0 命中）。
            //     ⚠️ `Battle/Phrase/*` **不复用近邻**（`Battle/BattleEnd/{Victory,Defeat}` · `Battle/Tips/{MeleeAttack,RangeAttack}`）
            //        —— 那些是**结算面板标题**与**数值格标签**，语义不同；复用会让「改一处文案另一处跟着变」（铁律 6）。
            //     ⚠️ `Battle/Phrase/*` 的 **EN 列 = 原来 `CardText.Phrases` 的字典键（英文原文）逐字**
            //        ⇒ 英文档的显示**逐字不变**（它原来就是回那个键）。
            // ==========================================================================================

            // ---- 卡名：我们自造的 26 张起始卡（`CardDef.Name` = 键尾）----
            { "Card_Name/Ember Warlord", new Entry("余烬督军", "Ember Warlord") },
            { "Card_Name/Scavenger", new Entry("拾荒者", "Scavenger") },
            { "Card_Name/Bulwark", new Entry("壁垒", "Bulwark") },
            { "Card_Name/Falcon", new Entry("猎鹰", "Falcon") },
            { "Card_Name/Ember Archer", new Entry("余烬弓手", "Ember Archer") },
            { "Card_Name/Veteran", new Entry("老兵", "Veteran") },
            { "Card_Name/Ironclad", new Entry("铁甲兵", "Ironclad") },
            { "Card_Name/Longbowman", new Entry("长弓手", "Longbowman") },
            { "Card_Name/Flamecaller", new Entry("焰唤者", "Flamecaller") },
            { "Card_Name/Shadowblade", new Entry("影刃", "Shadowblade") },
            { "Card_Name/Battering Ram", new Entry("攻城槌", "Battering Ram") },
            { "Card_Name/War Drake", new Entry("战龙", "War Drake") },
            { "Card_Name/Molten Colossus", new Entry("熔岩巨像", "Molten Colossus") },
            { "Card_Name/Tide Warlord", new Entry("潮汐督军", "Tide Warlord") },
            { "Card_Name/Tide Minion", new Entry("潮汐仆从", "Tide Minion") },
            { "Card_Name/Wave Rider", new Entry("踏浪者", "Wave Rider") },
            { "Card_Name/Reef Guard", new Entry("珊瑚卫", "Reef Guard") },
            { "Card_Name/Siren", new Entry("海妖", "Siren") },
            { "Card_Name/Coral Archer", new Entry("珊瑚弓手", "Coral Archer") },
            { "Card_Name/Shellback", new Entry("贝壳兽", "Shellback") },
            { "Card_Name/Ballista", new Entry("弩炮", "Ballista") },
            { "Card_Name/Deep Hunter", new Entry("深渊猎手", "Deep Hunter") },
            { "Card_Name/Iron Shell", new Entry("铁壳", "Iron Shell") },
            { "Card_Name/Storm Priest", new Entry("风暴祭司", "Storm Priest") },
            { "Card_Name/Leviathan", new Entry("利维坦", "Leviathan") },
            { "Card_Name/Abyss Titan", new Entry("深渊泰坦", "Abyss Titan") },

            // ---- 关键词：canonical 键 → 显示名（`CardText.KeywordZh` / `KeywordEn` / `Keyword` 三条路都读这里）----
            { "Card_Trait/ability", new Entry("技能", "Ability") },
            { "Card_Trait/agenda", new Entry("议程", "Agenda") },
            { "Card_Trait/ambush", new Entry("伏击", "Ambush") },
            { "Card_Trait/armour", new Entry("盔甲", "Armour") },
            { "Card_Trait/artifice", new Entry("工匠", "Artifice") },
            { "Card_Trait/backlash", new Entry("反击", "Backlash") },
            { "Card_Trait/blast", new Entry("爆炸", "Blast") },
            { "Card_Trait/blind", new Entry("失明的", "Blind") },
            { "Card_Trait/bloodthirst", new Entry("嗜血", "Blood Thirst") },
            { "Card_Trait/camouflage", new Entry("伪装", "Camouflage") },
            { "Card_Trait/cantattack", new Entry("无法攻击", "Can't Attack") },
            { "Card_Trait/codex", new Entry("圣典", "Codex") },
            { "Card_Trait/companion", new Entry("伙伴", "Companion") },
            { "Card_Trait/concussion", new Entry("震荡", "Concussion") },
            { "Card_Trait/cruelty", new Entry("残忍", "Cruelty") },
            { "Card_Trait/darkpact", new Entry("黑暗契约", "Dark Pacts") },
            { "Card_Trait/destroyer", new Entry("毁灭者", "Destroyer") },
            { "Card_Trait/duty", new Entry("职责", "Duty") },
            { "Card_Trait/ecstasy", new Entry("狂喜", "Ecstasy") },
            { "Card_Trait/ephemeral", new Entry("短暂的", "Ephemeral") },
            { "Card_Trait/faith", new Entry("信仰", "Faith") },
            { "Card_Trait/fast", new Entry("快速", "Fast") },
            { "Card_Trait/ferocity", new Entry("凶残", "Ferocity") },
            { "Card_Trait/flank", new Entry("侧翼", "Flank") },
            { "Card_Trait/flying", new Entry("飞行", "Flying") },
            { "Card_Trait/huntmark", new Entry("猎杀标记", "Hunt Mark") },
            { "Card_Trait/invulnerable", new Entry("无懈可击", "Invulnerable") },
            { "Card_Trait/longrange", new Entry("远程", "Long Range") },
            { "Card_Trait/markerlight", new Entry("标记灯", "Markerlight") },
            { "Card_Trait/mob", new Entry("暴群", "Mob") },
            { "Card_Trait/oath", new Entry("誓言", "Oath") },
            { "Card_Trait/pack", new Entry("群体", "Pack") },
            { "Card_Trait/penitence", new Entry("忏悔", "Penitence") },
            { "Card_Trait/pindown", new Entry("压制", "Pin Down") },
            { "Card_Trait/pray", new Entry("祈祷", "Pray") },
            { "Card_Trait/quest", new Entry("任务", "Quest") },
            { "Card_Trait/rally", new Entry("集结", "Rally") },
            { "Card_Trait/regeneration", new Entry("再生", "Regeneration") },
            { "Card_Trait/regiment", new Entry("团", "Regiment") },
            { "Card_Trait/remnant", new Entry("残余", "Remnant") },
            { "Card_Trait/sabotage", new Entry("破坏", "Sabotage") },
            { "Card_Trait/sentry", new Entry("哨兵", "Sentry") },
            { "Card_Trait/shield", new Entry("盾牌", "Shield") },
            { "Card_Trait/shuriken", new Entry("星镖", "Shuriken") },
            { "Card_Trait/slay", new Entry("消灭", "Slay") }, { "Card_Trait/sneakattack", new Entry("潜行攻击", "Sneak Attack") },   // 别名键（不是官方键名）：canonical 是 `sneakattack`（`CardDef.cs:7317`）⇒ `CardText.TraitTerm` 拼出 `Card_Trait/sneakattack`；官方那条叫 `Card_Trait/sneak`（`官方本地化_en+zh-CN.json`）⇒ 两列值逐字照抄官方，⛔ 别当成「我们自造的词」
            { "Card_Trait/sniper", new Entry("狙击手", "Sniper") },
            { "Card_Trait/spiritstone", new Entry("灵魂石", "Spirit Stone") },
            { "Card_Trait/stealth", new Entry("隐身", "Stealth") },
            { "Card_Trait/stimulation", new Entry("刺激", "Stimulation") },
            { "Card_Trait/stomp", new Entry("践踏", "Stomp") },
            { "Card_Trait/strike", new Entry("攻击", "Strike") },
            { "Card_Trait/stun", new Entry("眩晕", "Stun") },
            { "Card_Trait/swarm", new Entry("虫群", "Swarm") },
            { "Card_Trait/synapse", new Entry("突触节点", "Synapse") },
            { "Card_Trait/talent", new Entry("才能", "Talent") },
            { "Card_Trait/teleport", new Entry("传送", "Teleport") },
            { "Card_Trait/tide", new Entry("潮汐", "Tide") },
            { "Card_Trait/unstable", new Entry("不稳定", "Unstable") },
            { "Card_Trait/uprising", new Entry("起义", "Uprising") },
            { "Card_Trait/vanguard", new Entry("先锋", "Vanguard") },
            { "Card_Trait/vulnerable", new Entry("易受伤害", "Vulnerable") },
            { "Card_Trait/waystone", new Entry("灵魂石", "Waystone") },

            // ---- 阵营：`CardDef.Faction` = 键尾（14 条键名 = 原版 `Armies/<Faction>`；`Ember`/`Tide`/`Neutral` 自拟）----
            { "Armies/Ember", new Entry("余烬", "Ember") },        // 我们自造的阵营（原版 `Armies/` 里没有）
            { "Armies/Tide", new Entry("潮汐", "Tide") },          // 同上
            { "Armies/Neutral", new Entry("中立", "Neutral") },    // ⚠️ `Armies/` 里**没有 `Neutral`**（普查 §④）⇒ 一并自拟
            { "Armies/Ultramarines", new Entry("极限战士", "Ultramarines") },
            { "Armies/Goff", new Entry("欧克蛮人", "Orks") },
            { "Armies/SaimHann", new Entry("萨姆-罕", "Saim-Hann") },
            { "Armies/Sautekh", new Entry("索泰克王朝", "Sautekh Dynasty") },
            { "Armies/BlackLegion", new Entry("黑色军团", "Black Legion") },
            { "Armies/Leviathan", new Entry("利维坦虫巢舰队", "Hive Fleet Leviathan") },
            { "Armies/TauEmpire", new Entry("钛帝国", "T'au Empire") },
            // 🔴 2026-10-23（`C1`）：**换回官方值「修女会」**（原「战斗修女」= 卡面用词）。判据 = 调度台口径
            //   「官方表内部自相矛盾时照官方表原样显示」——官方 `Armies/Sororitas` = 修女会 而
            //   `Card_Name/SOR5` = 战斗修女，**原版中文客户端本来就是两套词并存**（`G2` 的「暂缓」理由已作废）。
            { "Armies/Sororitas", new Entry("修女会", "Adepta Sororitas") },
            { "Armies/Genestealers", new Entry("基因窃取者教团", "Genestealer Cults") },
            { "Armies/AstraMilitarum", new Entry("星界军", "Astra Militarum") },
            { "Armies/DarkAngels", new Entry("黑暗天使", "Dark Angels") },
            { "Armies/EmperorsChildren", new Entry("帝皇的孩子们", "Emperor's Children") },
            { "Armies/SpaceWolves", new Entry("太空野狼", "Space Wolves") },

            // ---- `CardText.Phrases` 那 11 条（+ `TurnLabel`）：**另立键、不复用近邻**（见上面那条判据）----
            //   ⚠️ 同族另 4 条（`END TURN` / `YOUR TURN` / `ENEMY TURN` / `DRAW`）**本表早已有键**
            //      （`Battle/HUD/*` / `Battle/BattleEnd/Draw`）⇒ 这里**不重复建**，`CardText.PhraseTerms` 直接把那两个
            //      英文原文转过去（`END TURN` 那三条是 `A1084` 之前就转好的）。
            //   ⚠️ 另有 4 条（`HAND` / `DECK` / `DISC` / `HP`）**全仓零消费点 ⇒ 本批删掉，不建键**（普查 §5.4）。
            { "Battle/Phrase/GameOver", new Entry("对局结束", "GAME OVER") },
            { "Battle/Phrase/YouWin", new Entry("你赢了", "YOU WIN") },
            { "Battle/Phrase/YouLose", new Entry("你输了", "YOU LOSE") },
            { "Battle/Phrase/ChooseAction", new Entry("选择行动", "CHOOSE ACTION") },
            { "Battle/Phrase/PickTarget", new Entry("选择目标", "PICK A TARGET") },
            { "Battle/Phrase/NoLegalTarget", new Entry("没有合法目标", "NO LEGAL TARGET") },
            { "Battle/Phrase/Melee", new Entry("近战", "MELEE") },
            { "Battle/Phrase/Ranged", new Entry("远程", "RANGED") },
            { "Battle/Phrase/Ability", new Entry("技能", "ABILITY") },
            { "Battle/Phrase/Stunned", new Entry("眩晕中", "STUNNED") },
            { "Battle/Phrase/CannotAct", new Entry("这个单位无法行动", "THIS UNIT CANNOT ACT") },
            { "Battle/Phrase/TurnLabel", new Entry("第 {0} 回合", "TURN {0}") },

            // ---- 效果小字 / 技能卡面板整句的构件（`CardText.Effect` · `EffectSentence`）----
            //   ⚠️ `CardEffect/Sentence/Draw{Many,One}` 的 **ZH 两列同值是有意的**：中文不分单复数，
            //      英文才分（EN 列 = 原来代码里 `n > 1 ? " cards" : " card"` 那两句逐字）。
            //   ⚠️ `CardEffect/Sentence/{Heal,Damage}` 的 `{1}` = 目标词（= 同族 `CardEffect/Target/*` 取出来的那串），
            //      `{0}` = 数量 —— **枚举顺序不同**（中文「为{1}…{0}…」/ 英文「… {0} … to {1}」）⇒ 两个占位符都在表里。
            { "CardEffect/Short/Damage", new Entry("伤害", "DMG") },
            { "CardEffect/Short/Heal", new Entry("治疗", "HEAL") },
            { "CardEffect/Short/Draw", new Entry("抽牌", "DRAW") },
            { "CardEffect/Target/Self", new Entry("自身", "self") },
            { "CardEffect/Target/OwnWarlord", new Entry("己方战将", "your warlord") },
            { "CardEffect/Target/EnemyWarlord", new Entry("敌方战将", "the enemy warlord") },
            { "CardEffect/Target/EnemyUnit", new Entry("敌方单位", "an enemy unit") },
            { "CardEffect/Sentence/DrawMany", new Entry("抽 {0} 张牌", "Draw {0} cards") },
            { "CardEffect/Sentence/DrawOne", new Entry("抽 {0} 张牌", "Draw {0} card") },
            { "CardEffect/Sentence/Heal", new Entry("为{1}回复 {0} 点生命", "Restore {0} health to {1}") },
            { "CardEffect/Sentence/Damage", new Entry("对{1}造成 {0} 点伤害", "Deal {0} damage to {1}") },
            // ============================================================ 🆕 **2026-10-14（第二十八会话 · `A1803`）：
            //   「代码引用了、表里没值」那 10 条（原账写 12 条，逐条现核后是 10 条 —— 见下）**
            //
            // 🔴 **现核订正（铁律 5）**：账 `I2_全表可挖盘点.md:103-107` 写「代码**已经引用**了这个键、表里却没值 = 12 条」，
            //   逐条 grep 之后**不成立**：那 12 条里
            //     · `Battle/Tips/DamageFatigue` —— **真有活引用**（`BattleDriver.DamageFatigueTerm`）⇒ 已随上面 `Battle/` 那 105 条一起进表；
            //     · `MainMenu/PurchasePremium/Description` —— **本来就在表里**（本表 `:1624`，**多行写法** ⇒
            //       上一轮那个「表里没有」的普查脚本按单行正则扫的，**漏读了它**）；
            //     · 剩下 **10 条**：全仓 `grep` 下来，它们**只出现在文档注释里**
            //       （`TutorialModePopup.cs:182` / `ForgeTab.cs:549` / `CampaignRewardWindow.cs:42` /
            //        `ForgeData.cs:18,168` / `ItemDrawer.cs:940` / `BoosterInfoPopup.cs:532` /
            //        `ReferralPopupWindow.cs:44-45` / `DailyData.cs:1083`）——
            //       **一个运行期取值点都没有**（`grep '"<键>"' Assets --include=*.cs` 逐条现读）。
            //   ⇒ 本块的口径 = **「值备好」**：键与两列都照官方表逐字录入，⛔ **不是「已接」**
            //     （接线要动上面那些文件，⛔ 不在本笔白名单）。
            //   🔴 **2026-10-19（`W57`·TOOL-6 就地订正，铁律 5）**：本段原文写
            //     「⚠️ 副作用：其中 `ForgeTab.HelpTipBody = " "`（**故意留空**，配一条 `ShownBody.Trim() == ""` 的断言）
            //     一旦改成读本表，那条断言会红 —— 接线时一起办」——**已过期**：
            //     两个属性都已改读本表（`Loc.T` 取官方词条，见下一条 UI-4 订正），
            //     `Editor/RewardsScene.cs` 那两条「正文空」断言也**同日（`W57`·TOOL-5）改成「非空 + == 官方词条值」**。
            //   🔴 **2026-10-19（`W57` 界面线 UI-4 · 铁律 5）就地订正**：上面那段**原文**（`HelpTipBody = " "` 那两行）**前半已过期** ——
            //     · `ForgeTab.HelpTipBody` 自 **2026-10-19（`N73`，UI-3）** 起 = `Loc.T("MainMenu/Forge/Help")`
            //       （`Shell/ForgeTab.cs:568`，**不再是空格**）；`BoosterInfoPopup.TipBody` 同日同笔 =
            //       `Loc.T("MenuShop/BoosterInfo/LegendaryTooltip")`（`Shell/BoosterInfoPopup.cs:524`）。
            //       ⇒ 上面那份「只出现在文档注释里」的 10 条**现为 8 条**（划掉的就是这两个文件的旧行号）。
            //     · **后半**：`Editor/RewardsScene.cs` 那两条锻造 tooltip 断言（旧行号 `:4412` 的 `Trim() == ""` /
            //       `:4427` 的「正文为空」）当时本会红 —— 🔴 **2026-10-19（`W57`·TOOL-5）已办**：已改成
            //       「`Loc.HasEntry` 前提 + 正文非空 + == 官方词条值（测试侧独立取 `Loc.T`，不是同义反复）」。
            //     其余 8 条（`TutorialModePopup` / `CampaignRewardWindow` / `ForgeData` ×2 / `ItemDrawer` /
            //     `ReferralPopupWindow` ×2 / `DailyData`）**未核现状**，引用条数一律以现读为准（⛔ 别抄本条）。
            // 判据逐条 → `资料/普查产出_第二十八会话/W4_官方文案族.md`。
            { "Demo/Tutorial",                          new Entry("教程", "Tutorial") },   // 原版 `String.Format(词条, 完成数, 总关数)`
            { "MainMenu/Forge/Help",                    new Entry("在你的收藏中升级张牌，通过每个军队的锻造厂轨迹进行进步并获得大奖。", "Upgrade cards in your Collection to progress through the Forge track for each faction and earn great rewards.") },
            { "MainMenu/General/Claim",                 new Entry("索取", "Claim") },
            { "MainMenu/Level",                         new Entry("等级", "Level") },      // 原版拼法 `"{0} {1}/{2}"` 套它
            { "MainMenu/RewardWindow/AlreadyOwned",     new Entry("已拥有", "Already Owned") },
            { "MenuShop/BoosterInfo/LegendaryTooltip",  new Entry("在最多每30个打开的增强包中，都保证有一张传奇张牌。", "A legendary card is guaranteed in at most every 30 opened booster packs.") },
            // ============================================================ 🆕 **2026-10-17（界面线第三批 · `A1756`）：
            //   开包窗（`BoosterPackOpenWindow`）两段提示字的词条。**
            // 键名 = 官方表里这一族的两条（`TapToDiscover` 的判据由 `A1756` 给出：原版那颗 `Localize.mTerm`；
            //   `TapToClose` 是**同族官方键**（键名与 prefab 出厂串 `Tap to close` 同文；⚠️ 它那颗的 `mTerm`
            //   本轮**没逐颗核**，只核了「官方表里有这条键、值逐字相同」）。
            // 两列 = **官方值**（`数据/游戏数据/官方本地化_en+zh-CN.json` 现读，⛔ 不是自拟）：
            //   `TapToDiscover` = `Tap on the cards to discover` / `点击卡片进行探索`
            //   `TapToClose`    = `Tap to close` / `点击关闭`
            // 消费侧 = `Shell/BoosterPackOpenWindow.cs` 的 `DiscoverText` / `CloseText`（2026-10-17 起是属性）。
            { "MenuShop/BoosterPackOpening/TapToDiscover", new Entry("点击卡片进行探索", "Tap on the cards to discover") },
            { "MenuShop/BoosterPackOpening/TapToClose",    new Entry("点击关闭", "Tap to close") },
            // ---- 同一笔（`A1756`）的另外三个站点：档案窗 / 头像页 / 挑战确认框 ----
            // 两列 = **官方值**（现读 `数据/游戏数据/官方本地化_en+zh-CN.json`）：
            //   `MainMenu/Register/PleaseChooseName` = `Choose your player name` / `选择你的玩家名字`
            //     ← `Shell/ProfileTab.cs` 的 `ChooseNameWindow/MessageText`（出厂英文同值）。
            //   `Demo/AvatarMenu/SelectAvatar`       = `Select your avatar` / `选择你的头像`
            //     ← `Shell/AvatarTab.cs` 的 `Item Display Panel/Select Item`（`PlayerProfile/SelectAvatarTip` 同值）。
            //   `MainMenu/Chat/ChallengeConfirmation`= `Do you want to challenge {0:0}?` / `你想挑战{0:0}吗？`
            //     ← `Shell/DuelPopupWindow.cs` 的 `MessageText`。
            //     🔴 **2026-10-23（`C1`）就地订正**：原写「官方串**不带 HTML**」—— **半错**：
            //     官方 **EN 列也不带**（`Do you want to challenge {0:0}?`），但**官方 ZH 列带**
            //     `<b><color=#FCDEBB>{0}</color></b>`（= 玩家名加粗染色），本批已把 ZH 换成官方那一串。
            //     错因 = 只看了 prefab 那颗 TMP 的英文 `m_text` 就外推到两列。
            { "MainMenu/Register/PleaseChooseName",        new Entry("选择你的玩家名字", "Choose your player name") },
            { "Demo/AvatarMenu/SelectAvatar",              new Entry("选择你的头像", "Select your avatar") },
            // 🆕 **2026-10-17（`N37①`）**：`Select Avatar Button/Button Text` 的官方键（值 = `Select` / `选择`）。
            //   ⚠️ **原版 prefab 里那颗 TMP 没有 `Localize`/`mTerm`**（三处 `m_text = "Selecionar"` 的节点上只有
            //   TMP + `EverguildTextMeshPro` + `EverguildButtonMaterialModifier`）⇒ **「原版用的是这条键」是推断**，
            //   不是 prefab 证据。补进来是为了让 `Shell/AvatarTab.cs` 的本地兜底自动切回 `Loc.T`
            //   （它在那里先判 `Loc.HasEntry`，没有就走本地两列 + `Lang13Current`）。
            { "Demo/AvatarMenu/SelectAvatarButton",        new Entry("选择", "Select") },
            { "MainMenu/Chat/ChallengeConfirmation",       new Entry("你想挑战<b><color=#FCDEBB>{0}</color></b>吗？", "Do you want to challenge {0:0}?") },
            { "MenuShop/referral/counter",              new Entry("你已收集了{0}个推荐奖励！", "You have collected {0} referral rewards!") },
            { "MenuShop/referral/counterMaximum",       new Entry("您已收集到最多的推荐奖励。", "You have collected the maximum number of referral rewards.") },
            { "Missions/CollectingRewards",             new Entry("正在收集奖励...", "Collecting Rewards...") },
            { "Rewards Menu/Claimed",                   new Entry("已认领", "Claimed") },

            // ============================================================ 🆕 **2026-10-17（界面线 · 事件说明弹窗族）：
            //   `BaseEventPopup` 那六份 prefab 里 `Tap To continue` 那一颗的词条。**
            //
            // 键名 = 原版那颗 TMP 上 `Localize.mTerm` 的**原文**（`mTerm = "Demo/Welcome/TapToContinue"`，
            //   现读 `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_6361195102692256584.json`；
            //   该实例挂在 `Liveop Event Help Variant/Texts/Tap To continue`。
            //   六个变体各自的 `Tap To continue` **逐颗实读、mTerm 全同** —— 一条键、六处共用）。
            // 两列 = 官方本地化表（`d:/4/Unity/数据/游戏数据/官方本地化_en+zh-CN.json` 的
            //   `Demo/Welcome/TapToContinue` = `["Click to continue", "点击以继续"]`，2026-10-17 实读）。
            // 🔴 **为什么必须进 `Table`**：`Loc.BulkOr` 走的是 `Resources/official_names.json` 那 **9** 族
            //   （2026-10-19 现读 = 9 族 / 3,156 条；原文写「6 族」是 `A1803` 时的口径），`Demo/Welcome/*` **不在里面**；而 `Loc.T` 对缺键是「返回键名本身 + 出声」
            //   ⇒ 不补这一条，弹窗底部会**在界面上真的印出 `Demo/Welcome/TapToContinue`**（静默坏掉的样子）。
            // 消费侧 = `Shell/BaseEventPopup.cs` 的 `BuildTexts`（唯一调用点）。
            { "Demo/Welcome/TapToContinue",              new Entry("点击以继续", "Click to continue") },

            // ============================================================ 🆕 **2026-10-17（界面线 T18/T25）：物品展示窗的 `Description` 词条**
            //  键 = 原版 `CosmeticItem.itemDescriptionKey`（`bundle_cosmeticsso_assets_all` 的 243 个卡背 SO 里
            //        **237 个**是这一键；另 6 个（Ranked/Skirmish Tier1–3）是 `Item_Description/CardbackSeasson`
            //        —— **那一键官方表里没有**，也无从按卡背查到是哪 6 个（`Resources/Cardbacks.json` 不存这一列））。
            //  值 = 官方本地化表 `d:/4/Unity/数据/游戏数据/官方本地化_en+zh-CN.json` 的同键两列，**逐字**（2026-10-17 现读）。
            //  消费侧 = `Shell/ItemDisplayWindow.ForCardback`（唯一调用点）。
            { "Item_Description/CardbackGeneric",        new Entry("卡背装饰。将其分配给你的卡组，以在战斗中使用它。", "Cosmetic item. Assign it to your deck to use it as reverse image for your cards in battle.") },
        };
        //  ⚠️ **`Wire/HostRestarted` 的 ZH/EN 是本轮【故意改过的】**（`A1038` 顺手项，同 `NetBattle` 那条
        //     R7 中性化纪律）：原来 ZH = 调用点原话「主机重开了」，而那个调用点（`NetRuntime.Reset()`）
        //     **两端都会走**（主机点【保存】/ 客机点【检查连接】）⇒ 客机捎给主机的是一句**语义颠倒**的话。
        //     现措辞对**收方**恒成立（它看到的确实是「对面」重开了这一条）。⇒ 这里**不是**「中文档零变化」，
        //     如实标出（这是本族唯一一条 ZH 列 ≠ 调用点原文的）。
        //  ⚠️ `Wire/NotYourTurn` 与 `Battle/Tips/NotYourTurn`（本文件 `Battle/` 那一节）**ZH 同值、语义不同**
        //     —— 后者是**本机 HUD 的提示行**、前者是**回给对面的拒绝码**（判据 → `NetBattle.Dispatch`
        //     那段注释的判据 ②）⇒ **两族并存，⛔ 别合并**。

        // ============================================================ 🆕 2026-10-17（界面线 · T08 `A1812`）：官方 13 语言全表
        //
        // 🔴 **判据**：原版语言下拉 = `AvailableLanguages` 12 项，而官方 I2 表里 **13 种真语言**
        //   每种 **10,839–10,854 / 10,862** 条非空（`Turkish` 不在枚举里 ⇒ **只当数据、不进下拉**）。
        //   ⚠️ **2026-10-18（`K07`）现读订正**：原来这里写「9,438–9,441 / 9,447」—— 那是
        //     **2025-07-30 旧快照**的读数；当前 build 已并入（`gen_official_loc_13lang.py --input`）。
        //   数据件 = `Resources/official_loc_13lang.json`，生成器 = `工具/gen_official_loc_13lang.py`
        //   （原件与解码算法 → `工具/_i2_decode_probe.py` 文件头；全量人读版 =
        //   `Unity/数据/游戏数据/官方本地化_13lang.json`，**同一份数据**）。
        //
        // ⚠️ **载入时机（2026-10-18 界面线 `N71` 起）**：非中英档**必然**载入；中文/英文档只在
        //   「`Table` + 数据面 bulk 都没命中」的键上**兜底**时才载入（约 **9.2 MB** / **10,862** 键 × 13 列）。
        //   中文/英文档的**权威仍是上面那张手写 `Table`**（我们自拟的键 + 2026-10-23 起**逐条等于官方值**的
        //   那 447 条都在里面；🔴 原来这里写的「33 条刻意保留的旧译」已作废 —— 那一族当天全换完了），
        //   官方全表只补「`Table`/bulk 里根本没有」的键（⛔ 不覆盖任何已有键的值 —— 见 `T()` 那三档顺序）。
        // ⚠️ 官方表里**没有**的键（例如自拟的 `Settings/Online/Title`）⇒ 回退英文 + 出声一次/键。
        [System.Serializable] class LangFile
        {
            public string[] langs;
            public string[] keys;
            public string[] en, es, ru, fr, de, pt, it, ko, zh, ja, pl, cs, tr;
        }

        /// <summary>`Resources/official_loc_13lang.json` 的路径。</summary>
        public const string Lang13ResourcePath = "official_loc_13lang";

        /// <summary>13 列的顺序（= 生成器里的 `LANGS`；`Lang13Of` 按码查列用）。</summary>
        static readonly string[] Lang13Codes = { "en", "es", "ru", "fr", "de", "pt", "it", "ko", "zh", "ja", "pl", "cs", "tr" };

        static Dictionary<string, int> _lang13Idx;    // 键 → 平行数组下标
        static string[][] _lang13Cols;                 // [列] → 值数组（列序 = Lang13Codes）
        static bool _lang13Loaded;
        static readonly HashSet<string> _warnedLangFallback = new HashSet<string>();

        static void LoadLang13()
        {
            if (_lang13Loaded) return;
            _lang13Loaded = true;
            _lang13Idx = new Dictionary<string, int>();
            _lang13Cols = null;
            var ta = Resources.Load<TextAsset>(Lang13ResourcePath);
            if (ta == null)
            {
                // **不静默**：没有它 ⇒ 非中英语言一律回退英文（与 `A1812` 之前的行为相同）
                Debug.LogError("[Loc] 读不到 `Resources/" + Lang13ResourcePath + ".json` ⇒ "
                             + "**非中英语言全部回退英文**（下拉里那 10 种语言取不到自己的文案）。"
                             + "生成器：`工具/gen_official_loc_13lang.py`");
                return;
            }
            var d = JsonUtility.FromJson<LangFile>(ta.text);
            if (d == null || d.keys == null) return;
            _lang13Cols = new[] { d.en, d.es, d.ru, d.fr, d.de, d.pt, d.it, d.ko, d.zh, d.ja, d.pl, d.cs, d.tr };
            for (int i = 0; i < d.keys.Length; i++)
                if (!string.IsNullOrEmpty(d.keys[i])) _lang13Idx[d.keys[i]] = i;
        }

        /// <summary>某个枚举语言在官方 13 列里是第几列（`-1` = 不在表里，如 `Turkish` / 非法值）。</summary>
        static int Lang13Column(AvailableLanguages l)
        {
            switch (l)
            {
                case AvailableLanguages.English:    return 0;
                case AvailableLanguages.Spanish:    return 1;
                case AvailableLanguages.Russian:    return 2;
                case AvailableLanguages.French:     return 3;
                case AvailableLanguages.German:     return 4;
                case AvailableLanguages.Portuguese: return 5;
                case AvailableLanguages.Italian:    return 6;
                case AvailableLanguages.Korean:     return 7;
                case AvailableLanguages.Chinese:    return 8;
                case AvailableLanguages.Japanese:   return 9;
                case AvailableLanguages.Polish:     return 10;
                case AvailableLanguages.Czech:      return 11;
            }
            return -1;
        }

        /// <summary>官方 13 语言表里取一条（**不看当前语档**）。缺键 / 该列空 ⇒ `null`。</summary>
        static string Lang13Text(string key, AvailableLanguages lang)
        {
            if (string.IsNullOrEmpty(key)) return null;
            LoadLang13();
            int col = Lang13Column(lang);
            if (col < 0 || _lang13Idx == null) return null;
            int i;
            if (!_lang13Idx.TryGetValue(key, out i)) return null;
            var a = _lang13Cols[col];
            return (a != null && i < a.Length) ? a[i] : null;
        }

        /// <summary>官方 13 语言表一共载入几条（0 = 文件没读到；自检用）。</summary>
        public static int Lang13Count { get { LoadLang13(); return _lang13Idx.Count; } }

        /// <summary>自检用：按**语言码**取一条官方词条（`"tr"` 这类**不在枚举里**的列也取得出 ——
        /// `A1812` 的判据要求「Turkish 只在数据里」）。缺键 / 未知码 ⇒ `null`。⛔ 不改任何状态。</summary>
        public static string Lang13Of(string key, string langCode)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(langCode)) return null;
            LoadLang13();
            int col = System.Array.IndexOf(Lang13Codes, langCode);
            int i;
            if (col < 0 || _lang13Idx == null || !_lang13Idx.TryGetValue(key, out i)) return null;
            var a = _lang13Cols[col];
            return (a != null && i < a.Length) ? a[i] : null;
        }

        /// <summary>自检用：清空「非中英语言回退英文」的去重表（同 <see cref="ResetMissingWarnedForTest"/>）。</summary>
        public static void ResetLangFallbackWarnedForTest() { _warnedLangFallback.Clear(); }

        /// <summary>🆕 2026-10-17（`N42`）：按**当前语档**取官方 13 语言表里的那一列。
        /// <para>**中文 / 英文档恒返回 `null`** —— 那两档以手写 `Table` 为权威（见文件头），
        /// 调用方拿到 `null` 就继续走自己那条老路（英文列 / 兜底），**行为逐字节不变**。</para>
        /// <para>其余 10 种语言：命中就返回**那一列**；**缺键 / 那一列是空 ⇒ 返回 `null` + 出声一次/键**
        /// （⛔ 不静默、⛔ 不返回键名 —— 回退英文由调用方做，各自有自己的兜底）。</para>
        /// <para>🔴 **这是「按当前语档取 13 语言列」的唯一一口**（工程红线：两处写同一条规则 = 迟早不一致）——
        /// `CardText.Term/TermOr/Name/Keyword` 与 `CardIcons.OfficialTerm` 都走它，
        /// ⛔ 别在别处再写一遍 `Lang13Column` + `Current` 的组合。</para></summary>
        public static string Lang13Current(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            var cur = Current;
            if (cur == AvailableLanguages.Chinese || cur == AvailableLanguages.English) return null;
            string v = Lang13Text(key, cur);
            if (!string.IsNullOrEmpty(v)) return v;
            // 缺键 / 缺列 ⇒ 回退英文（由调用方做）+ 出声一次（去重表与 `T()` 共用 ⇒ 一条键总共只响一次）
            if (_warnedLangFallback.Add(key))
                Debug.LogWarning($"[Loc] `{cur}` 里没有 `{key}` 的文案（官方 13 语言表缺这条键、或那一列是空）"
                               + " ⇒ **回退英文**（⛔ 不印键名、⛔ 不静默；同一条键只出声这一次）。");
            return null;
        }

        /// <summary>🆕 **2026-10-18（界面线 `N71`）：官方全表里按【当前语档】取一条 —— 含中/英两档**。
        /// <para>口径与 <see cref="T"/> 新加的中/英兜底**逐字一致**：中文档取 `zh` 列，**该列为空 ⇒ 回退 `en` 列**；
        /// 英文档取 `en` 列；缺键 / 两列都空 ⇒ `null`（⛔ 不返回键名、**不出声、不记账** ——
        /// 出声与 `MissingCount` / `FallbackCount` 那些账由 `T()` / `CardText.TermOr` 各自的调用点负责）。</para>
        /// <para>非中英档**恒 `null`**：那 10 种走 <see cref="Lang13Current"/>（它的回退与出声口径不一样，⛔ 两条别合并）。</para>
        /// <para>🔴 **现行两处显示消费点（2026-10-19 · `A1945` 方案 A 起）**：
        /// ① `T()` 的中/英兜底；② **卡面正文** —— `BattleDriver.FaceTextFull` 用
        /// `OfficialCurrent("Card_Description/&lt;卡 id&gt;")` 取官方 en/zh（`desc`/`descZh` 数据不动）。
        /// ⛔ 除此之外别再开第三条显示取值路 —— 界面文案正常一律走 `T()` / `CardText.Term`。</para></summary>
        public static string OfficialCurrent(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            var cur = Current;
            if (cur == AvailableLanguages.Chinese)
            {
                string zh = Lang13Text(key, AvailableLanguages.Chinese);
                return !string.IsNullOrEmpty(zh) ? zh : Lang13Text(key, AvailableLanguages.English);
            }
            if (cur == AvailableLanguages.English) return Lang13Text(key, AvailableLanguages.English);
            return null;
        }

        /// <summary>🆕 **2026-10-18（RV 复核·中）**：官方全表的**英文列**（无视当前语档）。
        /// 用途（2026-10-19 · `A1945` 起共两处）：
        /// ① `CardText.TermOr` 在「`Loc.Current == 中文`、但中文字体资产缺失（`CardText.Zh == false`）」
        /// 时避免印中文方块 —— 那一路卡面必须回英文（`CardText.cs:553-561` 的字体闸红线）；
        /// ② **卡面正文** `BattleDriver.FaceTextFull` 的同一档（中文档但字体缺失 ⇒ 取官方英文列，
        /// 与 ① 同一条口径）。
        /// ⛔ 别拿它当正常的显示取值路。</summary>
        public static string OfficialEnglish(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            return Lang13Text(key, AvailableLanguages.English);
        }

        /// <summary>🆕 2026-10-17（`N42`）：官方 13 语言表里以 <paramref name="prefix"/> 开头的键
        /// （**按序数排序**，结果是确定的）。
        /// <para>用途**只有一处**：`Core/Tooltip.cs` 派生「官方枚举名 ↔ 我们的规范键」的映射
        /// （关键词 tooltip 的正文要按官方枚举名取 `CardTraitDescription/*`）。
        /// ⚠️ 这是**读数据**的口，⛔ 不参与任何显示取值 —— 显示一律走 <see cref="Lang13Current"/> / <see cref="T"/>。</para></summary>
        public static string[] Lang13Keys(string prefix)
        {
            LoadLang13();
            var list = new List<string>();
            if (_lang13Idx == null) return list.ToArray();
            foreach (var k in _lang13Idx.Keys)
                if (k.StartsWith(prefix, System.StringComparison.Ordinal)) list.Add(k);
            list.Sort(System.StringComparer.Ordinal);
            return list.ToArray();
        }

        // ============================================================ 🆕 **2026-10-14（第二十八会话 · `A1803`）：数据面词条（bulk）**
        //
        // 🔴 **为什么单开这一层，⛔ 不写进上面那张 `Table`**：下面这几族的键**太多、而且键是数据面的**
        //   （GUID / SO 名，不是手写词条）—— 手写进 `Table` 会把那份文件变成一片 GUID：
        //     `Titles/*` **459**（**键 = 32 位 GUID**）· `Avatars/*` **498** · `Achievements/*` **212** ·
        //     `Cardbacks/*` **181** · `PrebuiltDecks/*` **95** · `Event_Description/*` **144**。
        //     🔴 **2026-10-17（`A2029`）就地订正**：`PrebuiltDecks` 原来写 **97**，现读
        //     `Resources/official_names.json` = **74**（其余五族 368 / 419 / 198 / 157 / 111 逐个复核**全对**；
        //     那一份表共 2,758 条）。行尾数只是注释、零行为。
        //     🔴 **2026-10-19（`W57` 界面线 UI-8）就地现核订正（铁律 5）**：上面那一组**全部过期** ——
        //     现读 `Resources/official_names.json` = **3,156 条 / 9 族**（键唯一；文件自己的 `count` 也是 3,156）：
        //     `Card_Name/*` **1198** · `Avatars/*` **498** · `Titles/*` **459** · `Modular_Description/*` **342** ·
        //     `Achievements/*` **212** · `Cardbacks/*` **181** · `PrebuiltDecks/*` **95** ·
        //     `Event_Description/*` **144** · `Card_Trait/*` **27**（合计 3,156）。
        //     2026-10-17 那组 368 / 419 / 198 / 157 / 74 / 111 与 2,758 是**当时快照**，⛔ 别当现值引用。
        //     ⚠️ **2026-10-23 现读订正（铁律 5 · 特效线 `A1·R28` 收口时同步）**：上面那组**又过期了** ——
        //     现读 `Resources/official_names.json` = **3,240 条 / 9 族**：
        //     `PrebuiltDecks/*` **101 → 179**（`gen_official_names.py` 2026-10-23 起把**池外那 78 副**
        //     练习窗卡组也 join 进来，见界面线 `W65`），其余八族逐值不变
        //     （1198 / 498 / 459 / 342 / 212 / 181 / 144 / 27 + 179 = **3,240**）。
        //     复算 = 按 `entries[].k` 的 `/` 前缀分组计数（`d:/4/Unity/MyGame/Assets/CardPresentation/Resources/official_names.json`）。
        //   ✅ 也⛔ **别让各 `Shell/*Data.cs` 各抄一份** —— 「两处写同一条规则 = 迟早不一致」（工程红线）。
        // 产物 = `Resources/official_names.json`，生成器 = `工具/_loc_official/gen_official_names.py`
        //   （**键 = 原版词条键原样**，拼法逐族写在那个脚本的文件头）。
        // ⚠️ 取值**一律走 `BulkOr`**（缺键出声 + 兜底），⛔ 别自己 `Resources.Load` 读那份 json。

        [System.Serializable] class BulkRow { public string k; public string en; public string zh; }
        [System.Serializable] class BulkFile { public BulkRow[] entries; }

        /// <summary>`Resources/official_names.json` 的路径。</summary>
        public const string BulkResourcePath = "official_names";

        static Dictionary<string, BulkRow> _bulk;
        static readonly HashSet<string> _warnedBulk = new HashSet<string>();

        /// <summary>🆕 **2026-10-15（`AC` 波）：`BulkOr` 里那次记号渲染的重入闩**。
        /// true = 本口这次被调用，是**从另一次词条取值的渲染里**进来的（路径固定是
        /// `CardIcons.RewriteMarks` → ①/②/③ → `CardIcons.OfficialTerm` → 本口）⇒ 那一路**不再渲染**。
        /// <para>两个理由，缺一不可：① **与原版同口径** —— 原版四支（`ModifyLocalization.c`）是
        /// **一遍扫过去、不回头再看自己插进来的串**（「①→③ 的顺序是承重的」就是这件事），
        /// 词条里再嵌一层 `((K))` 原版**不会**再展开；② **结构上杜绝无限递归** —— 词条 A 的值引 `((A))`
        /// 这种数据今天不存在，但「今天不存在」挡不住明天，而 `StackOverflow` 在 Unity 里是**硬崩**、
        /// 不是一条红。**判据/现数 → `资料/普查产出_第三十一会话/AC_Loc记号闸.md`**。</para>
        /// <para>⚠️ 只在本口这一处读/写；`Loc` 本来就是主线程用的（同 `_bulk` / `_warnedBulk`），
        /// ⛔ 不为它引锁。</para></summary>
        static bool _inBulkRender;

        static void LoadBulk()
        {
            if (_bulk != null) return;
            _bulk = new Dictionary<string, BulkRow>();
            var ta = Resources.Load<TextAsset>(BulkResourcePath);
            if (ta == null)
            {
                // **不静默**（缺了它，称号/头像/成就/卡背/预组那几页会回落成资源名反推的串）
                Debug.LogError("[Loc] 读不到 `Resources/" + BulkResourcePath + ".json` ⇒ "
                             + "Titles/Avatars/Achievements/Cardbacks/PrebuiltDecks 那几族的**官方名取不到**，"
                             + "界面会回落成资源名反推的串。生成器：`工具/_loc_official/gen_official_names.py`");
                return;
            }
            var d = JsonUtility.FromJson<BulkFile>(ta.text);
            if (d == null || d.entries == null) return;
            foreach (var r in d.entries)
                if (r != null && !string.IsNullOrEmpty(r.k)) _bulk[r.k] = r;
        }

        /// <summary>数据面词条一共几条（自检用）。</summary>
        public static int BulkCount { get { LoadBulk(); return _bulk.Count; } }

        /// <summary>自检用：这条键在数据面表里有没有（⛔ 不改任何状态）。</summary>
        public static bool HasBulk(string term)
        {
            if (string.IsNullOrEmpty(term)) return false;
            LoadBulk();
            return _bulk.ContainsKey(term);
        }

        /// <summary>自检用：清空「数据面缺键」的去重表（同 <see cref="ResetMissingWarnedForTest"/>）。</summary>
        public static void ResetBulkWarnedForTest() { _warnedBulk.Clear(); }

        /// <summary>
        /// 按**当前语言**取一条**数据面词条**；取不到（或取到的是**渲染不出来的官方记号**）⇒ 回 `fallback` + 出声一次。
        ///
        /// <para>🔴 **为什么返回兜底而不是键名**：调用方那几族的兜底**改前就有**（资源名反推的显示名），
        /// 而「印 GUID 键名」是退化。⇒ 本口**先出声、再给兜底**（本工程要的是「不静默」而不是「印键名」；
        /// 与 `CardText.TermOr` 同一口径，⛔ 两条别合并 —— 那条走 `Table`、这条走 bulk）。</para>
        ///
        /// <para>🆕 **2026-10-15（`AC` 波）：原「记号闸」已拆，改成「**先渲染、渲染不掉才回兜底**」** ——
        /// 原来本口判「值里含 `[[` / `((` ⇒ 当取不到」，理由写的是「要真接上，先做 `A1763` 的记号渲染层」。
        /// **那个层已经有了**（`Core/CardIcons.cs` 的 <see cref="CardIcons.RewriteMarks"/>，
        /// 公开口、按原版四支实现、自带「词条缺就留着 + 出声」的闸）⇒ 现在分三路走：
        /// <list type="number">
        ///  <item>值**不带**记号 ⇒ **原样返回**（与拆闸前逐字相同）。</item>
        ///  <item>值**带**记号 ⇒ 过一遍 `RewriteMarks(v, 该值所在的列)`；渲完**记号没了** ⇒ 返回**渲染后的串**
        ///        （这就是要治的病：以前这批被整条当「取不到」，卡面会原样印出 `((Stun_an_enemy))`）。</item>
        ///  <item>渲完**还留着记号** ⇒ 那是**渲染不了**的（词条缺 / 枚举名认不出）⇒ 照旧当「取不到」、
        ///        回兜底 + 出声（⛔ 不许把 `[[maintenance]]` 原样印到界面上 —— 那正是本闸要治的病；
        ///        也⛔ 不许静默丢）。</item>
        /// </list>
        /// ⚠️ **三路各有多少、逐条是哪些，一律看指针**（⛔ 不在这里抄第二份数）：
        /// `资料/普查产出_第三十一会话/AC_Loc记号闸.md`。</para>
        ///
        /// <para>⚠️ **渲染那一路按「值在哪一列」传语档**（`r.zh` ⇒ 中文档、回退到 `r.en` ⇒ 英文档）——
        /// `RewriteMarks` 的 `zh` 决定它去取哪一列的**子词条**（`Card_Trait/*` 那种）。</para>
        ///
        /// <para>⚠️ **重入**：值是在**另一次渲染里**被取出来的（①/②/③ 取词条那三支就是）⇒ 那一层
        /// **不再渲染**（原版四支不回头 + 结构上不可能无限递归），见 <see cref="_inBulkRender"/>。</para>
        /// </summary>
        public static string BulkOr(string term, string fallback)
        {
            if (string.IsNullOrEmpty(term)) return fallback;
            LoadBulk();
            BulkRow r;
            if (!_bulk.TryGetValue(term, out r))
            {
                if (_warnedBulk.Add(term))
                    Debug.LogWarning("[Loc] 数据面词条表里**没有**这条键：`" + term + "` ⇒ 回兜底「" + fallback + "」");
                return fallback;
            }
            var cur = Current;
            string v = null;
            bool zhCol = false;                                                        // 这个值落在哪一列（渲染记号时要按它取子词条）
            if (cur == AvailableLanguages.Chinese && !string.IsNullOrEmpty(r.zh)) { v = r.zh; zhCol = true; }
            else if (cur == AvailableLanguages.English && !string.IsNullOrEmpty(r.en)) v = r.en;
            else if (cur != AvailableLanguages.Chinese && cur != AvailableLanguages.English)
            {
                // 🆕 2026-10-17（`A1812`）：非中英语言 ⇒ 官方 13 语言表按列取（`BulkRow` 仍只有 en/zh 两列）
                v = Lang13Text(term, cur);
            }
            if (string.IsNullOrEmpty(v))                                               // 回退英文（同 `T`）
            {
                zhCol = string.IsNullOrEmpty(r.en);
                v = zhCol ? r.zh : r.en;
            }
            if (string.IsNullOrEmpty(v)) return fallback;

            // 记号渲染（拆掉的那道闸原来就在这个位置）。三路见上面那段 doc。
            if (CardIcons.HasOfficialMarks(v))
            {
                // 这一层若已经在另一次渲染里 ⇒ 不重入（原版四支不回头；也保证不会无限递归）→ 见 `_inBulkRender`
                if (_inBulkRender) return v;

                string rendered;
                _inBulkRender = true;
                try { rendered = CardIcons.RewriteMarks(v, zhCol); }
                finally { _inBulkRender = false; }

                // 渲完还留着记号 = 渲染不掉（词条缺 / 枚举名认不出）⇒ 当「取不到」。⛔ 不原样印、⛔ 不静默丢
                if (CardIcons.HasOfficialMarks(rendered))
                {
                    if (_warnedBulk.Add(term))
                        Debug.LogWarning("[Loc] 数据面词条 `" + term + "` 的值里有**渲染不出来的官方记号**"
                                       + "（`RewriteMarks` 过完仍留着 `[[..]]`/`((..))`/`$$..$$` —— 多半是子词条缺）"
                                       + " ⇒ 回兜底「" + fallback + "」。缺在哪，看 `Core/CardIcons.cs` 各支那声 "
                                       + "`[CardIcons]` 告警（逐条记在 `资料/普查产出_第三十一会话/AC_Loc记号闸.md`）");
                    return fallback;
                }
                v = rendered;
            }
            return v;
        }

        // ============================================================ 🆕 2026-10-17（`A1802` + `A1500①`）：卡面风味文本（Lore）
        //
        // 🔴 **判据（第一权威 = 反编译）**：原版 `SetCardLore` 取 `GetLocalizedFlavourText()`，
        //   **空就 `SetActive(loreObjectBG, false)`** —— 所以「没有 lore」的正确画面是**整块底板不亮**，
        //   ⛔ 不是「拿卡面的效果文字顶上」（那正是本笔要治的病：
        //   `Shell/CardDetailPopup.cs:716` 与 `Battle/CardDisplayWindow.cs` 原来都是拿效果文字当 lore）。
        //
        // 数据 = `Resources/lore_zh+en.json`（生成器 `工具/gen_lore.py`，只从官方表**抽取**、不造词）：
        //   · `Warlord_Lore/<卡 id>` —— 键**就是**我们 `cards_engine.json` 的 `id`（样本 `Warlord_Lore/GOF28`）。
        //     2026-10-17 写 **1042 条**；🔴 **2026-10-19 现读 = 1,157 条**；覆盖我们卡池 **1,111 / 1,127**
        //     （逐类：unit 586/586 · tactic 430/446 · hero 56/56 · defence 39/39）—— 缺的 **16** 张全是 tactic
        //     （id 为 32 位哈希的那批），官方 `Warlord_Lore` 表里没有它们的键 ⇒ 按原版「无 lore」处理。
        //   · `Cosmetic_Lore/<uniqueId>` —— 键 = `Resources/Cardbacks.json` 每条的 `uniqueId`；
        //     现读 **179 条**，233 张卡背里命中 **160**（🔴 2026-10-19 现核；原写「154 条 / 138」已过期）。✅ **首个消费点 = `Shell/ItemDisplayWindow.ForCardback`**
        //     （2026-10-17 界面线 T18/T25：卡背展示窗的 `FlavourTextBG/LoreText`；没有 lore ⇒ 整块关）。
        //     ⚠️ 卡背**格**（收藏页）仍然不显示它（那两格只有图、没有文字位）—— ⛔ 别顺手塞进格子里。
        //
        // ⚠️ 两扇窗（`Shell/CardDetailPopup` / `Battle/CardDisplayWindow`）**共用这一份判据** ——
        //   工程红线：「两处写同一条规则 = 迟早不一致」。⛔ 别在窗里自己 `Resources.Load`。
        [System.Serializable] class LoreRow { public string k; public string en; public string zh; }
        [System.Serializable] class LoreFile { public LoreRow[] entries; }

        /// <summary>`Resources/lore_zh+en.json` 的路径。</summary>
        public const string LoreResourcePath = "lore_zh+en";

        static Dictionary<string, LoreRow> _lore;
        static readonly HashSet<string> _warnedLore = new HashSet<string>();

        static void LoadLore()
        {
            if (_lore != null) return;
            _lore = new Dictionary<string, LoreRow>();
            var ta = Resources.Load<TextAsset>(LoreResourcePath);
            if (ta == null)
            {
                // **不静默**（缺了它，两扇窗的 lore 条会全部不亮 —— 那是可见的退化）
                Debug.LogError("[Loc] 读不到 `Resources/" + LoreResourcePath + ".json` ⇒ "
                             + "**卡面风味文本（Lore）一条都取不到**，两扇详情窗的 `FlavourTextBG` 会整块不亮。"
                             + "生成器：`工具/gen_lore.py`");
                return;
            }
            var d = JsonUtility.FromJson<LoreFile>(ta.text);
            if (d == null || d.entries == null) return;
            foreach (var r in d.entries)
                if (r != null && !string.IsNullOrEmpty(r.k)) _lore[r.k] = r;
        }

        /// <summary>风味文本一共几条（两族合计；自检用）。</summary>
        public static int LoreCount { get { LoadLore(); return _lore.Count; } }

        /// <summary>这张卡有没有官方 Lore（`Warlord_Lore/<卡 id>`；自检用，⛔ 不改任何状态）。</summary>
        public static bool HasLore(string cardId) { return !string.IsNullOrEmpty(cardId) && HasLoreTerm("Warlord_Lore/" + cardId); }

        /// <summary>这枚卡背有没有官方 Lore（`Cosmetic_Lore/<uniqueId>`；自检用）。</summary>
        public static bool HasLoreByUniqueId(string uniqueId) { return !string.IsNullOrEmpty(uniqueId) && HasLoreTerm("Cosmetic_Lore/" + uniqueId); }

        /// <summary>按**当前语言**取这张卡的官方风味文本；**没有 ⇒ 返回 null**（不是空串、⛔ 不是键名、⛔ 不是效果文字）。
        /// <para>调用方拿到 null/空 时**必须照原版 `SetCardLore` 把整块 `FlavourTextBG` 关掉**。</para></summary>
        public static string Lore(string cardId)
        {
            return string.IsNullOrEmpty(cardId) ? null : LoreTerm("Warlord_Lore/" + cardId);
        }

        /// <summary>按**卡背的 `uniqueId`** 取官方风味文本；规则同 <see cref="Lore"/>。
        /// ✅ **消费点 = `Shell/ItemDisplayWindow.ForCardback`**（2026-10-17 T18/T25：物品展示窗底部那条
        /// `Card Display/FlavourTextBG/LoreText`）—— 原版那一格喂的是 `ObtainableItem.itemLoreKey`
        /// （= `Cosmetic_Lore/&lt;uniqueId&gt;`，`ItemDisplayWindow__Open.c` 现读）。</summary>
        public static string LoreByUniqueId(string uniqueId)
        {
            return string.IsNullOrEmpty(uniqueId) ? null : LoreTerm("Cosmetic_Lore/" + uniqueId);
        }

        static bool HasLoreTerm(string term)
        {
            LoadLore();
            return _lore.ContainsKey(term);
        }

        /// <summary>`Lore` / `LoreByUniqueId` 的共用取值口（**只此一处**）。
        /// 语档规则与 `BulkOr` 一致：中文档取 `zh`、其余回退 `en`；两列都空 ⇒ null。
        /// 缺键 ⇒ **null + 出声一次**（按键去重）：那多半是 **2025-07-30 快照之后**的新卡，
        /// 原版对它们同样「没有风味文本」⇒ 两扇窗关底板，**⛔ 不用效果文字回填**。</summary>
        static string LoreTerm(string term)
        {
            LoadLore();
            LoreRow r;
            if (!_lore.TryGetValue(term, out r))
            {
                if (_warnedLore.Add(term))
                    Debug.LogWarning("[Loc] 官方 Lore 表里**没有**这条键：`" + term + "`"
                                   + "（多为 2025-07-30 快照之后新增的卡）⇒ 按原版「无 lore」处理："
                                   + "两扇详情窗会把 `FlavourTextBG` 整块关掉（⛔ 不拿效果文字回填）。");
                return null;
            }
            if (Current == AvailableLanguages.Chinese && !string.IsNullOrEmpty(r.zh)) return r.zh;
            // 🆕 2026-10-17（`A1812`）：非中英语言 ⇒ 官方 13 语言表按列取（`LoreRow` 仍只有 en/zh 两列）
            if (Current != AvailableLanguages.English)
            {
                string v13 = Lang13Text(term, Current);
                if (!string.IsNullOrEmpty(v13)) return v13;
            }
            if (!string.IsNullOrEmpty(r.en)) return r.en;
            return string.IsNullOrEmpty(r.zh) ? null : r.zh;      // 回退中文（同 `BulkOr` 的两列兜底）
        }

        // ============================================================ 取值

        /// <summary>按**当前语言**取一条文案。没有这个键 ⇒ 返回**键名本身** + 出声（⛔ 不返回空串：
        /// 空串在界面上是「什么都没画」，那正是本工程最忌讳的静默失败）。
        /// <para>当前语言没有这一套文案（西/法/德/…）⇒ **回退英文** + 出声，见文件头。</para>
        /// <para>🆕 **2026-10-18（界面线 `N71`）：三档顺序 = 手写 `Table`（权威）→ 数据面 bulk →
        /// 官方全表 → 缺键档。** 中文/英文档此前只查前两层，官方表里有的 UI 键在中英档取不到
        /// （B 报告 §一 #54：`MainMenu/Campaign/Help` · `Missions/Info/*` · `Rewards Menu/DailyStreak/*`
        /// · `Missions/FreeTrack`/`PremiumTrack` · `MainMenu/General/NextWithTwoPoints` …
        /// 全部「官方表有、`Table` 没有」）⇒ 直接吃返回值的调用点会印**键名**。
        /// 官方全表那层读 `Resources/official_loc_13lang.json` 的 `zh` / `en` 列（现读 10,862 键）；
        /// ⚠️ **只接住「前两层都没命中」的那一档** ⇒ 已有键的返回值**逐字节不变**，
        /// <see cref="HasEntry"/> 的语义也**没动**（仍只认手写 `Table`，各处
        /// `HasEntry ? T : 兜底` 的调用点行为不变）。判据 →
        /// `资料/普查产出_1018/R_界面审查B_商店设置奖励战役.md` §一 #54。</para>
        /// <para>🔴 **2026-10-18（第四轮 · 审查 S2）**：缺键那声 `LogWarning` **按键去重**（`_warnedMissing`）——
        /// 同一条键**只出声一次**（不然被每帧调用时会刷屏，把抓日志型自检淹掉）。
        /// ⚠️ 记账（<see cref="MissingCount"/> / <see cref="LastMissingKey"/>）**照旧每次 +1**，不受去重影响。</para>
        /// <para>⚠️ **2026-10-14（第二十九会话 · `A1924`）更正**：本条**返回键名**这一半是对的（下面 `return key;`），
        /// 但**旧告警文案把它写成了「界面上现在印的是键名本身」** —— **不成立**：「界面印什么」由**调用方**决定，
        /// 而本族的调用点**没有一处吃这个返回值**（形状都是「先 `Loc.HasEntry` 判、缺了才调一次 `Loc.T` **只为出声/记账**、
        /// 然后回自己那条兜底」）：`CardText.TermOr`（`Core/CardText.cs:547-553`，回调用方给的英文 id ——
        /// 卡名/阵营/兵种那几族）与 `KeywordZh`/`KeywordDisplay`/`KeywordEn`（同文件 `:112`/`:125`/`:140`，
        /// 分别回 `null`/`null`/首字母大写的英文 —— `Card_Trait/*` 那一族）⇒ 那 14 条（12 `Card_Name/*`
        /// + 2 `Card_Trait/*`）在界面上**都不是键名**，卡面没坏。**错因** = 只读 `T` 自己的实现就下了
        /// 「界面会印键名」的结论（违反铁律 5·c：同一个函数在不同调用点，界面上看到的不一样）。
        /// 告警文案已按此改准（⛔ 出声照旧、没删）。
        /// 判据 → `资料/普查产出_第二十八会话/DIAG_battle六条红.md` §顺手发现 1（那一行的调用链是 `log:58204`）。</para></summary>
        public static string T(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            var cur = Current;
            Entry e;
            bool inTable = Table.TryGetValue(key, out e);

            // 中文 / 英文：手写 `Table` 是**权威**（我们自拟的键 + 官方值逐条对齐的那 447 条都在里面；
            //   原「33 条刻意保留的旧译」2026-10-23 已全部换官方值）
            if (cur == AvailableLanguages.Chinese && inTable && !string.IsNullOrEmpty(e.Zh)) return e.Zh;
            if (cur == AvailableLanguages.English && inTable && !string.IsNullOrEmpty(e.En)) return e.En;

            // 🆕 2026-10-17（`A1812`）：其余 10 种语言 ⇒ 官方 13 语言表**各取各的列**；
            //   中/英档里「`Table` 有键但那一列是空」的条目也从这里兜一下（官方表有就用官方的）。
            // 🔴 **2026-10-19（`N71` 现核 · 铁律 5）就地订正**：上一行那半句**不成立** —— 下面这个 `if`
            //   **排除了中/英档**；中/英档在「`Table` 有键但那一列是空」时走**函数末尾的「回退英文」**
            //   （`e.En`，必要时再 `Lang13Text(英文)`）⇒ **不查官方表的当前语档列**。
            //   这是 `N80` 的刻意口径（「已有键的返回值逐字节不变」），⛔ 本波不改逻辑，只把注释改准。
            if (cur != AvailableLanguages.Chinese && cur != AvailableLanguages.English)
            {
                string v13 = Lang13Text(key, cur);
                if (!string.IsNullOrEmpty(v13)) return v13;
            }

            if (!inTable)
            {
                // 🆕 **2026-10-18（第五十一会话 · 真机抓到「模式卡底部印键名」）**：
                //   `Table` 没登记、但**官方数据面表（bulk）里有**这条键 ⇒ 用 bulk 的当前语言列。
                //   触发实例：`Event_Description/General/EndsIn`（`N61` 接到模式卡倒计时行；
                //   本函数**不查 bulk**，`N6` 早就记过这条）⇒ 界面上印的是键名。
                //   ⛔ 不是自拟：值逐字来自 `Resources/official_names.json`（官方表）。
                //   ⚠️ 这一步只接住「本来会返回键名」的那一档 ⇒ 对已登记的词条零影响。
                LoadBulk();
                BulkRow br;
                if (_bulk != null && _bulk.TryGetValue(key, out br))
                {
                    string bv = cur == AvailableLanguages.Chinese ? br.zh
                              : cur == AvailableLanguages.English ? br.en
                              : Lang13Text(key, cur);
                    if (string.IsNullOrEmpty(bv)) bv = br.en;
                    if (!string.IsNullOrEmpty(bv)) return bv;
                }

                // 🆕 **2026-10-18（界面线 `N71`）：中文 / 英文档也从官方全表兜底。**
                //   本条之前这两档**只查手写 `Table` + 数据面 bulk** ⇒ 官方表里有、而 `Table`/bulk 里没有的
                //   UI 键（`MainMenu/Campaign/Help` · `Missions/Info/*` · `Rewards Menu/DailyStreak/*` ·
                //   `Missions/FreeTrack`/`PremiumTrack` · `MainMenu/General/NextWithTwoPoints` ·
                //   `Event_Description/TwoSidesEvent/*` …）在**中英档取不到** —— 直接吃返回值的调用点
                //   会印**键名**（B 报告 §一 #54 点名的机制性偏差）。
                //   数据源 = `Resources/official_loc_13lang.json` 的 `zh` / `en` 两列（= 官方 I2 全表，
                //   现读 **10,862** 键；⛔ 不另落一份 en+zh 表 —— 那份数据已经在这个文件里，两处存同一份
                //   迟早不一致）。三档顺序 = `Table`（权威）→ bulk（数据面族）→ **官方全表** → 缺键档，
                //   ⚠️ **已在 `Table` / bulk 里的键一律不会走到这里 ⇒ 已有键的返回值逐字节不变**。
                //   ⚠️ 值原样返回（与上面非中英档的 `Lang13Text` 同口径）：记号渲染在
                //   `CardIcons.Rewrite` / `Loc.BulkOr` 那两层做，本口**不重复实现**。
                //   ⚠️ 取值走 <see cref="OfficialCurrent"/>（「官方全表按当前语档取值」的唯一一份）。
                if (cur == AvailableLanguages.Chinese || cur == AvailableLanguages.English)
                {
                    string voff = OfficialCurrent(key);
                    if (!string.IsNullOrEmpty(voff))
                    {
                        // 中文列是空的那 21 条（13 列各 **8–23** 条空列；🔴 2026-10-19 现读，原文「8–21」已过期）
                        // ⇒ `OfficialCurrent` 已回退英文列；
                        // 这里照旧记一次「回退英文」+ 出声（与 `T()` 对「`Table` 有键但 Zh 空」的记账口径一致）。
                        // 仍空（官方表也没有这条键）⇒ 落到下面的缺键档（返回键名 + 出声，⛔ 不静默）
                        if (cur == AvailableLanguages.Chinese
                            && string.IsNullOrEmpty(Lang13Text(key, AvailableLanguages.Chinese)))
                        {
                            FallbackCount++;
                            LastFallbackKey = key;
                            if (_warnedLangFallback.Add(key))
                                Debug.LogWarning($"[Loc] `{cur}` 里 `{key}` 那一列是空的 ⇒ **回退英文**："
                                               + $"\"{voff}\"。（官方 13 语言表里这条的其他列有值；"
                                               + "⛔ 同一条键只出声这一次）");
                        }
                        return voff;
                    }
                }

                // 非中英档：官方表里**有这条键**、只是这一列是空的（13 列各 **10,839–10,854 / 10,862** 非空；
                // 🔴 2026-10-19 现读订正：原文「9,438–9,441/9,447」是 2025-07-30 旧快照的读数）
                // ⇒ 回退英文（同一条词条的其他列），⛔ **不许把键名印到界面上**
                if (cur != AvailableLanguages.Chinese && cur != AvailableLanguages.English)
                {
                    string en13 = Lang13Text(key, AvailableLanguages.English);
                    if (!string.IsNullOrEmpty(en13))
                    {
                        FallbackCount++;
                        LastFallbackKey = key;
                        if (_warnedLangFallback.Add(key))
                            Debug.LogWarning($"[Loc] `{cur}` 里 `{key}` 那一列是空的 ⇒ **回退英文**："
                                           + $"\"{en13}\"。（官方 13 语言表里这条的其他列有值；"
                                           + "⛔ 同一条键只出声这一次）");
                        return en13;
                    }
                }

                MissingCount++;
                LastMissingKey = key;
                // 🔴 按键去重地出声（第一次问到这个键才出声；见 `_warnedMissing` 的 doc）
                if (_warnedMissing.Add(key))
                    Debug.LogWarning($"[Loc] 语言表里**没有**这个词条：`{key}`"
                                   + "（⇒ 本函数返回的是**键名本身**。⚠️ **它会不会印到界面上，由【调用方】决定**："
                                   + "直接吃返回值的 ⇒ 界面印的就是键名（那才是「不许静默」要抓的那种）；"
                                   + "先 `Loc.HasEntry` 判过、缺了就回自己那条兜底的 ⇒ 界面**照旧印兜底**，本条日志"
                                   + "只是那声「表里少一条」的出声。两处例子都在 `Core/CardText.cs`：`TermOr`"
                                   + "（`:547-553`，缺键回调用方给的英文 id/原值 —— 卡名/阵营/兵种那几族走它）"
                                   + "与 `KeywordZh`/`KeywordDisplay`/`KeywordEn`（`:112`/`:125`/`:140`，缺键分别回"
                                   + " `null`/`null`/首字母大写的英文 —— `Card_Trait/*` 那一族走它们））。"
                                   + "加词条的规矩见 `Core/Loc.cs` 文件头（键名照原版 `Localize.mTerm`）；"
                                   + "⛔ 同一条键**只出声这一次**（去重表见 `_warnedMissing`）。");
                return key;
            }

            // 走到这里 = 当前语言这一条是空的 / 官方 13 语言表里没有这条（自拟键）⇒ 回退英文
            FallbackCount++;
            LastFallbackKey = key;
            string en = e.En;
            if (string.IsNullOrEmpty(en)) en = Lang13Text(key, AvailableLanguages.English);
            if (_warnedLangFallback.Add(key))
                Debug.LogWarning($"[Loc] `{cur}` 里没有 `{key}` 的文案（官方 13 语言表里也没有这一列）"
                               + $" ⇒ **回退英文**：`{key}` → \"{en}\"。"
                               + "（⛔ 不在 UI 上加原版没有的提示行 —— 这条日志就是全部提示；"
                               + "⛔ 同一条键只出声这一次，去重表见 `_warnedLangFallback`）");
            return string.IsNullOrEmpty(en) ? key : en;
        }

        /// <summary>下拉里显示的语言名 = `T("MainMenu/Settings/LanguageName/&lt;枚举名&gt;")`。
        /// 🔴 **刻意不走回退**：选项名就是要显示「这一项叫什么」——
        /// 英语那一项在中文界面下显示「英语」、在英文界面下显示「English」，两者都对
        /// （原版也是这样：选项名按**当前界面语言**翻）。所以这里直接取表里那条的对应列。</summary>
        public static string LanguageName(AvailableLanguages lang)
        {
            return T(KeyOf(lang));
        }

        /// <summary>语言 → 它在表里的键（`MainMenu/Settings/LanguageName/&lt;官方名&gt;`，原版拼法）。
        /// <para>🔴 **2026-10-17（`A1812` · `N36`）**：官方表里这两条**不叫枚举名** ——
        /// `Portuguese` → `…/Portuguese (Brazil)`、`Chinese` → `…/Simplified Chinese`
        /// （官方值分别是 `Português` / `中文`，13 列逐字相同；现读 `官方本地化_13lang.json`）。
        /// 其余 10 条与枚举名一致。</para></summary>
        public static string KeyOf(AvailableLanguages lang)
        {
            switch (lang)
            {
                case AvailableLanguages.Portuguese: return "MainMenu/Settings/LanguageName/Portuguese (Brazil)";
                case AvailableLanguages.Chinese:    return "MainMenu/Settings/LanguageName/Simplified Chinese";
            }
            return "MainMenu/Settings/LanguageName/" + lang;
        }

        /// <summary>**手写 `Table` 里**有没有这个键（自检用：「表真的填了」那一条）。
        ///
        /// <para>🔴 **2026-10-25（`E1` · 第七段）就地订正：这个函数【只认手写 `Table`】，
        /// ⛔ 不能当成「运行时没有这条文案」。** `Loc.T` 有**三档兜底**：
        /// ① 手写 `Table`（权威）→ ② 数据面 bulk（`Resources/official_names.json`，3,263 条）
        /// → ③ 官方全表（`Resources/official_loc_13lang.json`，10,862 键）→ ④ 缺键档（返回键名 + 出声）。
        /// ⇒ `HasEntry(key) == false` 只说明**手写表没登记**，运行时**照样可能取得到**。
        /// 📌 判据实例：`MenuDeck/Error/MaxLegendayCopiesReached` 手写表没有、官方全表有 ⇒ `T` 正常出文案；
        /// 而 `HasEntry` 对它返回 false。**上一轮就是把这句读成「会印键名」而误报了一次**（铁律 5）。
        /// ⚠️ 因此 `HasEntry ? T(key) : 兜底` 这种写法**只适合「想在缺登记时换一条兜底」**，
        /// **不适合**用来判「运行时有没有文案」（那要看 `T` 的返回值是否等于键名 / `MissingCount`）。</para>
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

        /// <summary>取**中文那一列** —— <see cref="EnOf"/> 的对称件。**恒中文、不看当前语档**。
        /// <para>🔴 **2026-10-10（`A1084`）为什么开这个口**：`CardText.KeywordZh` 的契约是
        /// 「**查不到返回 `null`**、查到就给中文」（`KeywordSegment` 判「这个词认不认得」、
        /// `TipText.Trait` 拿它当**规则书文案表的键**——那张表按中文名索引）—— 这两处都**不能**用
        /// `T()`（`T` 按语档取：英文档下会给英文列 ⇒ 关键词段在英文档会印出中文名的替代品、
        /// tooltip 也查不到表）。⇒ 需要一条**与语档无关**的「只要中文列」的路。</para>
        /// <para>⛔ **别拿它当第二条显示取值路** —— 界面上正常显示**一律走 `T()`/`CardText.Term`**
        /// （它们才管语言闸、字体闸、缺键出声那些账）。⛔ 也别拿它去覆盖/写入（本表只读）。</para></summary>
        public static string ZhOf(string key)
        {
            Entry e;
            return (!string.IsNullOrEmpty(key) && Table.TryGetValue(key, out e)) ? e.Zh : null;
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
            NotifyLanguageChanged();      // 🆕 B89-E30（账 `L412`）：真的改了值之后发一次（订户见类尾那个广播口）
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

        // ============================================================ 目标提示文案（`Card_Targeted/*`）
        // 🔴 2026-10-27（执行代理 `B9_W61TARG` · 账 `资料/完全复刻/账/战斗逻辑驱动.tsv:1241`）：
        //  补齐原版 `RawCardScript.GetLocalizedTargetted()` —— **我们此前完全没实现**
        //  （改前 `grep -rn "Card_Targeted" Assets --include=*.cs` = **0 命中**；只有
        //   `Resources/official_loc_13lang.json` 数据侧那 31 条键，**一个消费者都没有**）。
        //
        //  · **原版方法体**（逐句；`cpp2il/out_cpp2il_dev_ilspy/RawCardScript.cs:2448-2458`。
        //    `decomp_full/RawCardScript__GetLocalizedTargetted.c` 是同一份体，只是把返回值丢了）：
        //      string text = string.IsNullOrEmpty(targetingTextReferenceId) ? uniqueId : targetingTextReferenceId;
        //      string term = "Card_Targeted/" + text;
        //      string translation = LocalizationManager.GetTranslation(term, FixForRTL: true, 0,
        //                              ignoreRTLnumbers: true, applyParameters: false, …);
        //      return SupportMethods.ModifyLocalization(translation);
        //    ⇒ **键 = `Card_Targeted/` + (`targetingTextReferenceId` 非空 ? 它 : `uniqueId`)**。
        //  · **唯一活调用点**（三源合判）：`out_cpp2il_dev_ilspy` 全树只有 `BattleManager.cs:1784`；
        //    `decomp_full` 全树只有 `BattleManager._ChooseUnitTarget_d__780__MoveNext.c`。
        //    那一段（`.c:37-62`）= `StartTrackingFrom(卡位,3)` → 取本方法 → `Get2DPosOfCard`
        //    → `EverguildTooltipManager.Show(battleTooltip, 标题="", 本方法, 卡位)` → `ShowPotentialTargets(card,1)`
        //    → 等点完；离开那一态（`.c:84-91`）`StopTracking` + `Hide`。
        //    ⇒ ⛔ **它不是卡面、也不是技能面板**（`Battle/SkillPanel.cs` 是 `ActiveSkillDesc` 那条，无关），
        //      是「**请你点一个目标**」的浮动提示。
        //  · `ModifyLocalization` 那一跳在我们侧 = `Core/CardIcons.RewriteMarks`（同四族记号：
        //    `((…))` / `$$…$$` / `[[…]]` / 费用记号）⇒ **本口只返回原样词条**，⛔ 不在这里重复实现记号渲染
        //    （与 `T` / `BulkOr` 同口径）。
        //  · **可达性（现数，⛔ 别抄成死数；口径 = `资料/完全复刻/报告/B9_W61TARG_卡牌目标文本.md`）**：
        //    数据在 `资料/普查产出_1018/allcards_1350/cards_raw.json` 的 **`full_raw.targetingTextReferenceId`**
        //    （1,351 张卡 SO 里 **51 张非空 / 18 种取值**）；其中「单位卡（`cardType==0`）∧ `targeted==1`」
        //    = **50 张 / 17 种** —— 只有这一档会走进 `ChooseUnitTarget`
        //    （闸 = `BattleManager__NeedToChooseMinionTarget.c:12-18`：`isPlayer ∧ cardType==0 ∧ targeted ∧ matchType!=0xA0`）。
        //    官方表 `Card_Targeted/*` **31 条** ⇒ **17 条可达 · 14 条在原版数据里没有任何卡引用**。
        //    ⚠️ **`uniqueId` 那条回退支在本 build 里 0 次命中**（「单位卡 ∧ targeted ∧ ref 空」= 0 张）
        //    —— 但它是原版的一支，**照抄**，⛔ 别因为「今天走不到」删掉。

        /// <summary>原版 `GetLocalizedTargetted` 的键前缀（`RawCardScript.cs:2451` 的字面量 `"Card_Targeted/"`）。</summary>
        public const string TargetedTermPrefix = "Card_Targeted/";

        /// <summary>原版 `GetLocalizedTargetted()` 的**键拼法**（唯一一处，逐字照抄 `RawCardScript.cs:2450-2451`）：
        /// `Card_Targeted/` + （`targetingTextReferenceId` 非空 ⇒ **它**；否则 ⇒ `uniqueId`）。
        /// <para>⚠️ `uniqueId` 那一支在本 build 现读 **0 次命中**（`cardType==0 ∧ targeted==1 ∧ ref 空` = 0 张），
        /// 但原版有这一支 ⇒ 照抄，⛔ 别删。</para></summary>
        public static string TargetedKey(string uniqueId, string targetingTextReferenceId)
        {
            string id = string.IsNullOrEmpty(targetingTextReferenceId) ? uniqueId : targetingTextReferenceId;
            return TargetedTermPrefix + id;
        }

        /// <summary>按**当前语言**取「请点一个目标」那句提示（原版 `RawCardScript.GetLocalizedTargetted()`）。
        /// <para>返回的是**原样词条**（可能含 `((…))` / `[[…]]` 记号）⇒ 调用方上屏前要过
        /// <c>CardIcons.RewriteMarks</c>，与卡面正文同一条链（原版对应的是 `SupportMethods.ModifyLocalization`）。</para>
        /// <para>缺键 ⇒ **走 <see cref="T"/> 的缺键档（返回键名 + 出声一次）** —— 与原版 I2「取不到就回 term」
        /// 同形，⛔ 不在这里改判成空串（空串 = 一块空白 tooltip，那是本项目最忌讳的静默失败）。
        /// 本 build 现读 **0 张卡**落这一档（见上面那段「可达性」）。</para></summary>
        public static string TargetedText(string uniqueId, string targetingTextReferenceId)
        {
            return T(TargetedKey(uniqueId, targetingTextReferenceId));
        }

        /// <summary>这条「目标提示文案」**本地到底有没有**（自检 / 调用方判断用）。
        /// ⚠️ 原版**没有**这道闸（`GetLocalizedTargetted` 取不到就回 term）⇒ ⛔ 别拿它当生产路径的分支；
        /// 它只回答「本地数据里有不有这条键」。判据 = 官方 13 语言表的**英文列**（现读 10,854/10,862 非空，最全），
        /// 所以**与当前语档无关**。⛔ 不改任何状态。</summary>
        public static bool HasTargetedText(string uniqueId, string targetingTextReferenceId)
        {
            string key = TargetedKey(uniqueId, targetingTextReferenceId);
            return !string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(OfficialEnglish(key));
        }

        /// <summary>自检用：`Card_Targeted/*` 这条链的**灭自证**检查。
        /// <returns>**空列表 = 全过**；每条失败是一句人话（直接丢进 `Check(...)` 的文案里即可）。</returns>
        /// <para>⛔ 不改任何状态（**不会**让 `MissingCount` +1：只走已存在的键，不制造缺键）。</para>
        /// <para>为什么这几条能防自证（`CLAUDE.md` §三「灭自证」那条）：</para>
        /// <list type="number">
        ///  <item>② 要求 `TargetedText` 对官方表里**每一条** `Card_Targeted/*` 都与 `T(key)` **逐字相等** ——
        ///        31 条 = 31 个不同的值 ⇒ 「返回 id / 返回常量 / 不查表」的桩全部过不了。</item>
        ///  <item>① 单独钉住「`ref` **压过** `uniqueId`」这一支（原版 `:2450` 的三元）：删掉它，
        ///        这里会第一个红，报错能指到那一行。</item>
        ///  <item>③ 反向对照：**本地不存在的 ref 必须取不到** —— 证明「能取到文案」不是本函数自己编的。</item>
        /// </list></summary>
        public static List<string> CheckTargetedText()
        {
            var bad = new List<string>();

            // ① 键拼法两支（原版 `RawCardScript.cs:2450-2451`）
            string kUid = TargetedKey("ZZTest", "");
            string kRef = TargetedKey("ZZTest", "deal3Dmg");
            if (kUid != TargetedTermPrefix + "ZZTest")
                bad.Add("① `ref` 为空时应拼 `" + TargetedTermPrefix + "ZZTest`，实得 `" + kUid + "`");
            if (kRef != TargetedTermPrefix + "deal3Dmg")
                bad.Add("① `ref` 非空时应**压过** `uniqueId`（原版 `:2450` 的三元）拼 `"
                        + TargetedTermPrefix + "deal3Dmg`，实得 `" + kRef + "`");

            // ② 官方表里每一条 `Card_Targeted/*`：按 ref 走 `TargetedText` 必须取到**同一条**词条
            var keys = Lang13Keys(TargetedTermPrefix);
            if (keys.Length == 0)
                bad.Add("② 官方 13 语言表里 `" + TargetedTermPrefix + "*` **一条都没有** ⇒ 本条链没有数据可吃");
            foreach (string key in keys)
            {
                string refId = key.Substring(TargetedTermPrefix.Length);
                string viaRef = TargetedText("__no_such_card_id__", refId);
                string direct = T(key);
                if (string.IsNullOrEmpty(viaRef))
                    bad.Add("② `" + key + "` 取到**空串**");
                else if (viaRef == key)
                    bad.Add("② `" + key + "` 取到的仍是**键名**（= 根本没取到词条）");
                else if (viaRef != direct)
                    bad.Add("② `ref=\"" + refId + "\"` 走 `TargetedText` 实得「" + viaRef
                            + "」，直取 `" + key + "` 是「" + direct + "」⇒ 拼键走岔了");
            }

            // ③ 反向对照（**灭自证**）：本地不存在的 ref 必须**取不到**
            if (HasTargetedText("__no_such_card_id__", "zzz_this_targeted_ref_does_not_exist"))
                bad.Add("③ 反向对照失败：本地不存在的 ref 被判成「有文案」⇒ `HasTargetedText` 只会回 true");
            if (!HasTargetedText("__no_such_card_id__", "deal3Dmg"))
                bad.Add("③ 反向对照失败：真实存在的 `deal3Dmg` 被判成「没有文案」");

            return bad;
        }

        // ============================================================ 🆕 B89-E30（账 `L412`）：换语言广播口
        //
        // 原版 `LiveopMenuContainer.OnLocalize()`（`decomp_full/LiveopMenuContainer.cs:27` 那一族）是
        // `LocalizationManager.OnLocalizeCallback` 打到每颗容器上的：换语言 ⇒ **容器自己把标题/时间重设一遍**。
        // 我们这边 `SetLanguage` 原本「**不发事件**」（理由写在它的 doc 里：两个设置窗不会同时开着）——
        // 但**主菜单**那棵树（左栏 6 颗导航 + 模式卡标题/倒计时）在设置窗后面**一直活着**，它没人通知。
        // ⇒ 这里补的正是原版那一条广播：`SetLanguage` 真改了值之后调 <see cref="NotifyLanguageChanged"/>。
        //
        // 🔴 订户形态照 `Battle/BattleHudLanguage.cs`（同一个问题在战斗 HUD 上的解法）：
        //    静态 `event` + `-=`/`+=` 的幂等订法 + 销毁时摘掉 + `ClearForTest`；
        //    ⛔ 别做成「自动满地找组件」—— 我们这侧要重设的不是 UGUI 子件，是一棵我们自己建的树。
        // ⚠️ **订户为 0 是合法的**（大部分宿主没有菜单树）⇒ 本方法**不报警**，由调用方按自己的需要看
        //    <see cref="LanguageChangedSubscribers"/>。

        /// <summary>语言真的变了（`SetLanguage` 返回 `true` 之后）发一次。
        /// <para>⚠️ 值没变（`SetLanguage` 早退那条）**不发** —— 与原版「只有 `ChangedLanguage` 才广播」同义。</para></summary>
        public static event System.Action LanguageChanged;

        /// <summary>发一次广播，返回**订户个数**（0 = 没人听，这不是错误）。
        /// <para>自检可以直接 `LanguageChanged += probe;` 再调本方法（同 `BattleHudLanguage.Notify` 的用法）。</para></summary>
        public static int NotifyLanguageChanged()
        {
            var h = LanguageChanged;
            if (h == null) { RelocalizeAll(); return 0; }   // B103_A03（AUD03 #94）：**没有 event 订户时，已建 `Label` 也要重取**（登记表 = 类尾 `_localizedLabels`，不依附 event 订户个数）
            int n = h.GetInvocationList().Length;
            try { h(); }
            catch (System.Exception e)
            {
                // 红线：不许静默失败。一个订户抛异常不能连带其它订户（原版 `BroadcastMessage` 也不连带）。
                Debug.LogError("[Loc] 换语言广播里有一家订户抛了异常（其它订户照常收到）：" + e);
            }
            RelocalizeAll(); return n;   // B103_A03（AUD03 #94）：广播之后把登记过的 `Label` 全部按各自来源重取（对标 I2 `OnLocalize`；⛔ 不重建窗、不动滚动位）
        }

        /// <summary>现在有几个订户（自检读口）。</summary>
        public static int LanguageChangedSubscribers
        {
            get { var h = LanguageChanged; return h == null ? 0 : h.GetInvocationList().Length; }
        }

        /// <summary>清空订户（自检用；⛔ 生产别调 —— 会把主菜单那条订阅一起摘掉）。</summary>
        public static void ClearLanguageChangedForTest() { LanguageChanged = null; }
    // ==========================================================================================
    //  战吼文案（`Chat_Messages/warcry-*`）—— 原版 `BattleManager.GetWarcry(bool isPlayer)`
    //  🆕 2026-10-28（`B93_F5` · 账 `战斗逻辑驱动:392`）
    // ==========================================================================================
    //  **原版方法体**（🥇 `cpp2il/out_cpp2il_dev_ilspy/BattleManager.cs:16188-16240`：
    //  `GetWarcry(bool)`；`decomp_full/BattleManager__GetWarcry.c:36,57,60,69,73-77` 同体）：
    //    DeckAndWarlordData d = isPlayer ? matchData.playerRawBattleData : matchData.enemyRawBattleData;
    //    CosmeticItem ci = PlayerDataManager.singletonManager.GetCosmeticItem(d.warcry);
    //    if (ci == null) return LocalizationManager.GetTranslation("Chat_Messages/warcry-default");
    //    return LocalizationManager.GetTranslation("Chat_Messages/warcry-" + ci.uniqueId);
    //  唯一调用点 = 结算界面 `SpecificPlayerResult..ctor`（`decomp_full/SpecificPlayerResult__.ctor.c:38-40`）。
    //
    //  🔴 **数据现读（2026-10-28 复核，与 `资料/完全复刻/报告/B92_B2.md` §4·1 第 3 条逐字一致）**：
    //    官方 13 语言表 `Resources/official_loc_13lang.json` **10,862 键**、旧 en+zh 表 **10,854 键** 里
    //    **`Chat_Messages/warcry*` = 0 条**（而 `Chat_Messages/*` 共 **1,736** 条 ⇒ 表本身是全的）。
    //    ⇒ 原版在本 build **两条分支都只能印缺键串**（I2 取不到就回 term 本身）。
    //    **照抄即正确** —— ⛔ 别把「印出 `Chat_Messages/warcry-default`」当缺陷去补文案。
    //  ⚠️ 账里那句「本地化表现在有官方全表（10,862 键）⇒ 判据可得」**已就地订正**：
    //    键族不在表里 ⇒ 能拿到的判据只有「键拼法 + 兜底档」，不是文案内容。
    //
    //  🔶 自己设计（非原版）：`GetCosmeticItem` 那道「你持不持有这个战吼」的闸。
    //    持有清单在服务器（PlayFab / `PlayerDataManager` 由远端灌；关服后本机 0 命中）⇒ 本机**只能**
    //    把「非空 warcryId」当成持有。为什么必须自己定：判据在远端、不是我们没查到 ·
    //    调度台 2026-10-28 裁定（依据 = `报告/B92_B2.md` W10 施工项）。
    // ==========================================================================================

    /// <summary>原版 `GetWarcry` 的键前缀（`"Chat_Messages/warcry-"`）与兜底键。</summary>
    public const string WarcryTermPrefix = "Chat_Messages/warcry-";

    /// <summary>没持有任何战吼 / `warcry` 为空时那一档（原版 `GetCosmeticItem` 回 null 那一支）。</summary>
    public const string WarcryDefaultTerm = "Chat_Messages/warcry-default";

    /// <summary>原版 `GetWarcry` 的**键拼法**（唯一一处）：
    /// `warcryId` 空 ⇒ <see cref="WarcryDefaultTerm"/>；否则 `Chat_Messages/warcry-` + id。
    /// <para>⚠️ 原版拼的是 `cosmeticItem.uniqueId`（回到 `GetCosmeticItem(warcry)` 的那个对象）；
    /// 我们把它记在 `PlayerDeck.WarcryId` 里（= 同一个装饰 id）⇒ 等价，见文件头 🔶 那条。
    /// ⛔ 别在这里查表 / 回退语言 —— 只拼键（自检靠它，不需要正本数据）。</para></summary>
    public static string WarcryKey(string warcryId)
    {
        return string.IsNullOrEmpty(warcryId) ? WarcryDefaultTerm : WarcryTermPrefix + warcryId;
    }

    /// <summary>按**当前语言**取这句战吼（原版 `BattleManager.GetWarcry(bool isPlayer)`）。
    /// <para>缺键 ⇒ 走 <see cref="T"/> 的缺键档（返回键名 + 出声一次）—— 与原版 I2「取不到就回 term」同形，
    /// ⛔ 不改判成空串（空串 = 一块空白，静默失败）。本 build 现读 **该键族 0 条** ⇒ 必然落这一档（原版同）。</para></summary>
    public static string Warcry(string warcryId)
    {
        return T(WarcryKey(warcryId));
    }

    /// <summary>「运行时到底**能不能取到**这条文案」——**三档一起算**（`Table` → bulk → 官方 13 语言全表）。
    /// <para>🔴 **2026-10-30（`B101_W1` 同步点 β2）为什么要开这个口**：`HasEntry` 的语义是「**手写 `Table`** 里有没有」
    /// （754 条），它**不等于**「取不到文案」——`T()` 的取值是**四段**的：
    /// ① 手写 `Table` → ② 数据面 bulk（`official_names.json`）→ ③ **官方 13 语言全表（`official_loc_13lang.json`，
    /// 现读 10,862 键）** → ④ 缺键档（回键名 + 出声）。只查 ① 就把「②③ 里明明有」的键误判成缺键
    /// ⇒ 调用方无辜回落 + 多打一条警告。**判据 = `CLAUDE.md` §三「判『会不会把键名印到屏幕上』必须**三档一起算**」**。
    /// 实例：`CustomErrors/InternetUnreachable` 在 ③ 里（现读 True）、不在 ① 里 ⇒ `Shell/ErrorPopupWindowController`
    /// 的两处 `HasEntry` 闸判 false ⇒ 真回落（`Shop` 的 S5 断言就是这么红的）。</para>
    /// <para>⚠️ **本口只管「有没有」、不改任何取值**：取文案照旧走 <see cref="T"/>（三档顺序与回退/出声都归它）。
    /// ⛔ 别拿它替代 `HasEntry` 的既有调用点 —— 那条的语义（「手写表里有没有」）在别处**是对的**
    /// （例如「这条是不是我们自己登记的键」那种判断）；只有「**怕印键名**」这一族才该换成本口。</para>
    /// <para>⚠️ 第 ③ 档按 <c>"en"</c> 列判（与自检里 `Lang13Of(键, "en")` 的先例同口径：那一列官方全表恒有值）；
    /// `HasBulk` / `HasEntry` 都自带 null 守卫，本口再加一道（`Lang13Of(null,…)` 返回 null，不会抛）。</para></summary>
    public static bool HasAnyTerm(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        return HasEntry(key) || HasBulk(key) || !string.IsNullOrEmpty(Lang13Of(key, "en"));
    }

    // ==========================================================================================
    //  🆕 B103_A03（AUD03 #94 · 2026-10-30）：换语言 ⇒ **已建 `Label` 按自己的来源重取文本**
    //
    //  原版形状 = I2 `LocalizationManager.OnLocalizeCallback`：每颗 `Localize` 组件自己登记 **term**，
    //    换语言时由本地化系统**广播**，组件按自己那条 term **重取一次**（`Localize.OnLocalize`），
    //    ⛔ **不重建整窗**。本仓已有一份同形先例 = `Shell/TimerDisplay.Awake/OnDestroy/LocalizationChange`
    //    （`TimerDisplay.cs:287-288` 订 `Loc.LanguageChanged`，回调里只重取描述串）。
    //  我们原来是「`OnLanguageChanged(){ Open(); }` = **整窗重建**」—— 可见差异 = 滚动位回顶、页签状态重算，
    //    那不是原版那一跳在做的事（判据 = `d:/2/tools/decomp_full/LiveopMenuContainer__OnLocalize.c` 那一族：
    //    容器只把标题/时间**重设一遍**）。
    //
    //  ⚠️ 为什么登记表放在 `Loc`（而不是让每颗 `Label` 自己去 `+= LanguageChanged`）：
  //    ① 广播源就是这里 ⇒ 只有一条遍历、一个 prune 口（「两处写同一条规则 = 迟早不一致」）；
  //    ② **批量/编辑模式下 `DestroyImmediate` 不派 `OnDestroy`**（本仓已多次踩，见 `资料/已知的坑.md`）
  //       ⇒ 自订自摘那种写法会留一堆**已销毁的订户**在 event 上（`NotifyLanguageChanged` 的 try/catch 裹的是
  //       整个组播列表 ⇒ 一家抛异常会把后面的全掐掉）。登记表这种写法能在广播前用 `lb == null` 把它们剔掉。
  //  ⚠️ 只重取**显式声明了来源**的那些（`Label.BindText`）；一次性 `SetText` 的**不碰**
  //    （否则会把倒计时/数值这类动态文本在换语言那一拍改成别的东西）。
  //  ⚠️ 登记者为 0 是合法的（绝大多数窗没有本地化标签）⇒ **不报警**（同 `NotifyLanguageChanged` 那条口径）。
    // ==========================================================================================

    /// <summary>「文本跟着语言走」的 `Label` 登记表（⚠️ 只由 <see cref="Label.BindText"/> / <see cref="UnbindLocalized"/> 维护）。</summary>
    static readonly System.Collections.Generic.List<Label> _localizedLabels = new System.Collections.Generic.List<Label>();

    /// <summary>登记每多少颗 prune 一次（见 <see cref="BindLocalized"/> 的注释）。</summary>
    static int _bindSincePrune;

    /// <summary>登记一颗「文本要跟着语言走」的 `Label`（幂等）。由 <see cref="Label.BindText"/> 调，⛔ 别直接调。</summary>
    /// <para>⚠️ 会做**摊还 prune**：滚动重建那种每次 `Build` 都新造一批 `Label` 的路径里，旧的那批已经销毁、
    /// 但还没被广播剔掉 ⇒ 不 prune 的话这张表会随重建次数无界增长（每 128 次登记扫一遍，摊到每次是常数）。</para>
    public static void BindLocalized(Label lb)
    {
        if (lb == null) return;
        if (!_localizedLabels.Contains(lb)) _localizedLabels.Add(lb);
        if ((++_bindSincePrune & 0x7F) == 0) PruneLocalized();
    }

    /// <summary>退登（幂等）。`Label.BindText(null)` 会调；已销毁的也会在下次 prune 时被剔掉。</summary>
    public static void UnbindLocalized(Label lb)
    {
        if (lb == null) return;
        _localizedLabels.Remove(lb);
    }

    /// <summary>现读还登记着几颗（先剔掉已销毁的）。</summary>
    public static int LocalizedBindingCount { get { PruneLocalized(); return _localizedLabels.Count; } }

    /// <summary>把登记着的标签全部按各自来源重取一次；返回**真重取了几颗**。
    /// <para>⛔ 不重建任何窗、不动滚动位 —— 这就是与旧 `OnLanguageChanged(){ Open(); }` 的区别。</para></summary>
    public static int RelocalizeAll()
    {
        PruneLocalized();
        if (_localizedLabels.Count == 0) return 0;
        var snapshot = _localizedLabels.ToArray();   // 重取途中可能有 `BindText` / 销毁 ⇒ 遍历快照
        int n = 0;
        for (int i = 0; i < snapshot.Length; i++)
        {
            var lb = snapshot[i];
            if (lb == null) continue;                 // 快照之后被销毁（Unity 假 null）
            try { lb.Relocalize(); n++; }
            catch (System.Exception e)
            {
                // 红线：不许静默失败；**一家抛异常不能把后面的全掐掉**
                // （`NotifyLanguageChanged` 的 try/catch 裹的是整个组播列表 ⇒ 这里必须逐家兜住）。
                Debug.LogError("[Loc] 换语言重取时有一颗 `Label` 抛了异常（其余照常重取）：" + e);
            }
        }
        return n;
    }

    static void PruneLocalized()
    {
        for (int i = _localizedLabels.Count - 1; i >= 0; i--)
            if (_localizedLabels[i] == null) _localizedLabels.RemoveAt(i);
    }

    /// <summary>自检用：清空登记表（⛔ 生产别调）。</summary>
    public static void ClearLocalizedForTest() { _localizedLabels.Clear(); _bindSincePrune = 0; }
    }
}
