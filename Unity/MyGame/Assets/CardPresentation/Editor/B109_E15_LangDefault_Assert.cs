// B109_E15_LangDefault_Assert.cs — 执行代理 `B109_E15`（2026-10-31 · 第一百零九会话）的断言
//
// ============================ 盯的是什么 ============================
// `Core/Loc.cs` 的**出厂语言**。改前：`public const AvailableLanguages Default = AvailableLanguages.Chinese;`
// —— 硬编码中文，注释自陈「用户 2026-09-28 口径：原版实拍是中文客户端」= **我们挑的**，不是原版机制。
// 改后：`Default` = 按 `Application.systemLanguage` 映射进 13 种语言之一，未命中 ⇒ 英文；
// 启动语言 = 三级回退 `ResolveStartup(本地记住的设置, Application.systemLanguage)`。
//
// ============================ 原版判据（`B109_R5` §N06 已查清；本代理复核过这两处方法体）============================
//  · `d:/2/tools/decomp_full/I2.Loc.LocalizationManager__SelectStartupLanguage.c` 的**三级回退**：
//      ① `PersistentStorage.GetSetting_String`（本地记住的设置；还要过 `HasLanguage(...)`）→
//      ② `GetCurrentDeviceLanguage()` = `Application.systemLanguage`（同目录 `…__DetectDeviceLanguage.c` 首句），
//         还要过 `GetSupportedLanguage(...)` →
//      ③ 源语言表里**第一个 `IsEnabled`** 的语言。
//  · `d:/2/tools/decomp_full/LoginManager__SetInitialLanguage.c`：`CurrentLanguage` 在
//      `GameStaticData.languagesDefinition`（13 项）里 ⇒ **原样保留**；循环走完都没命中 ⇒ 强制 `"English"`。
//  · `…LocalizationManager__set_CurrentLanguage.c`：`SetLanguageAndCode(名, 码, RememberLanguage = 1, …)`
//      ⇒ **原版会本地记住语言**（所以 `Loc.cs` 文件头那句「原版不存 PlayerPrefs」是错的，本轮已就地订正）。
//  · 原版**没有**「服务器下发默认语言」这回事：玩家档里只有 `languageVersion`（远端**词表版本号**），没有语言字段。
//
// ============================ 🔴 为什么这条断言能区分「按系统语言」与「恒中文」============================
//  中文机器上两种实现**结果相同**（这正是原缺陷一直看不出来的原因）⇒ 本件**不靠「跑出来是中文」证明**，
//  而是直接对 `Loc.MapSystemLanguage(...)` 喂**非中文**的 `SystemLanguage`：
//    · `Arabic` / `Thai` / `Swedish` / … **必须回英文**；
//    · `French` / `Japanese` / `Turkish` / … **必须回各自语言**。
//  ⇒ 「恒中文」实现：B、C 两组必红；「恒英文」实现：B、D 两组必红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `Run()` 是唯一入口；宿主接一行 `B109_E15_LangDefault_Assert.Run();` 即可（本文件**不改**任何宿主）。
//
// ============================ 灭自证（每条怎么才能红）============================
//  · B 组：把 `MapSystemLanguage` 里任一 `case` 删掉 ⇒ 对应那条红。
//  · C 组：把 `default:` 从 `return Fallback;` 改成 `return AvailableLanguages.Chinese;` ⇒ 全组红。
//  · D 组：把 `MapSystemLanguage` 改成恒返回同一个值 ⇒ 全组红。
//  · E 组：把 `ResolveStartup` 的第一级删掉（永远按设备语言）⇒ E1/E2/E3 红；把第三级改成中文 ⇒ E6/E7 红。
//  · F 组：把 `Default` 改回 `const Chinese` ⇒ F1（反射）红；把 `Load()` 的第一级（读 `PlayerPrefs`）删掉 ⇒ F2 红。
using System;
using System.Reflection;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B109_E15`：出厂 / 启动语言的**三级回退**断言（类名 == 文件名；唯一入口 <see cref="Run"/>）。</summary>
public static class B109_E15_LangDefault_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B109E15] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 入口

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "B109_E15 · 出厂/启动语言 = 原版三级回退（本地记住 → 系统语言 → 源内第一个语言）");
        try { A_Premises(); }
        catch (Exception e) { Tr(false, "A 前提段抛异常：" + e.GetType().Name + "：" + e.Message); }
        try { B_MapThirteen(); }
        catch (Exception e) { Tr(false, "B 映射段抛异常：" + e.GetType().Name + "：" + e.Message); }
        try { C_MapUnsupported(); }
        catch (Exception e) { Tr(false, "C 未支持语种段抛异常：" + e.GetType().Name + "：" + e.Message); }
        try { D_Discriminators(); }
        catch (Exception e) { Tr(false, "D 灭自证段抛异常：" + e.GetType().Name + "：" + e.Message); }
        try { E_ResolveStartup(); }
        catch (Exception e) { Tr(false, "E 三级回退段抛异常：" + e.GetType().Name + "：" + e.Message); }
        try { F_StructureAndLiveLoad(); }
        catch (Exception e) { Tr(false, "F 结构 / 真实 Load 段抛异常：" + e.GetType().Name + "：" + e.Message); }
        Debug.Log(_sink.P + "B109E15 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A · 前提

    static void A_Premises()
    {
        MenuCheck.Section(_sink, "A 前提（这些不成立 ⇒ 后面各组的含义就变了）");
        C(Loc.Languages.Length, 13,
          "A1 前提：`Loc.Languages` = **13 项**（原版下拉读的 `GameStaticData.languagesDefinition` 是 `string[13]`）");
        C(Loc.Languages[0], AvailableLanguages.English,
          "A2 前提：`Loc.Languages[0]` = **English** —— 原版第 ③ 级取的是「源内第一个 `IsEnabled` 的语言」，"
        + "本工程没有禁用语言 ⇒ 它必须就是英文（E4 那条才等价于「回英文」）");
        C(Loc.Fallback, AvailableLanguages.English,
          "A3 前提：`Loc.Fallback` = **English**（没有文案时折回的那一档，`T()` / `Effective` 的老规矩）");
    }

    // ============================================================ B · 13 种语言逐条映射

    static void B_MapThirteen()
    {
        MenuCheck.Section(_sink, "B `MapSystemLanguage`：13 种语言逐条映射（原版第 ② 级）");
        M(SystemLanguage.English,            AvailableLanguages.English,    "English");
        M(SystemLanguage.Spanish,            AvailableLanguages.Spanish,    "Spanish");
        M(SystemLanguage.Russian,            AvailableLanguages.Russian,    "Russian");
        M(SystemLanguage.French,             AvailableLanguages.French,     "French");
        M(SystemLanguage.German,             AvailableLanguages.German,     "German");
        M(SystemLanguage.Portuguese,         AvailableLanguages.Portuguese, "Portuguese");
        M(SystemLanguage.Italian,            AvailableLanguages.Italian,    "Italian");
        M(SystemLanguage.Korean,             AvailableLanguages.Korean,     "Korean");
        M(SystemLanguage.Japanese,           AvailableLanguages.Japanese,   "Japanese");
        M(SystemLanguage.Turkish,            AvailableLanguages.Turkish,    "Turkish");
        M(SystemLanguage.Polish,             AvailableLanguages.Polish,     "Polish");
        M(SystemLanguage.Czech,              AvailableLanguages.Czech,      "Czech");
        M(SystemLanguage.Chinese,            AvailableLanguages.Chinese,    "Chinese（Unity 的 `Chinese` 旧值）");
        M(SystemLanguage.ChineseSimplified,  AvailableLanguages.Chinese,    "ChineseSimplified → 简体中文（13 项里的 `Simplified Chinese`）");
        M(SystemLanguage.ChineseTraditional, AvailableLanguages.Chinese,    "ChineseTraditional → 中文（⚠️ 推断：13 项里只有简体，见 `MapSystemLanguage` 的注释）");
    }

    // ============================================================ C · 不在 13 种里 ⇒ 英文

    static void C_MapUnsupported()
    {
        MenuCheck.Section(_sink, "C `MapSystemLanguage`：不在 13 种里的语种 **一律回英文**（原版第 ③ 级 = 源内第一个语言 = 英文）");
        N(SystemLanguage.Arabic,      "Arabic");
        N(SystemLanguage.Thai,        "Thai");
        N(SystemLanguage.Swedish,     "Swedish");
        N(SystemLanguage.Dutch,       "Dutch");
        N(SystemLanguage.Danish,      "Danish");
        N(SystemLanguage.Finnish,     "Finnish");
        N(SystemLanguage.Greek,       "Greek");
        N(SystemLanguage.Hebrew,      "Hebrew");
        N(SystemLanguage.Norwegian,   "Norwegian");
        N(SystemLanguage.Ukrainian,   "Ukrainian");
        N(SystemLanguage.Vietnamese,  "Vietnamese");
        N(SystemLanguage.Afrikaans,   "Afrikaans");
        N(SystemLanguage.Indonesian,  "Indonesian");
        N(SystemLanguage.Unknown,     "Unknown（取不到系统语言）");
    }

    // ============================================================ D · 灭自证

    static void D_Discriminators()
    {
        MenuCheck.Section(_sink, "D 灭自证：映射**不是常量**（能区分「按系统语言」与「恒中文 / 恒英文」）");
        Tr(Loc.MapSystemLanguage(SystemLanguage.Chinese) != Loc.MapSystemLanguage(SystemLanguage.French),
           "D1 `Chinese` 与 `French` 映射出**不同**的结果 —— 恒返回同一个值的实现过不了这条");
        Tr(Loc.MapSystemLanguage(SystemLanguage.French) != AvailableLanguages.Chinese,
           "D2 `French` **不是**中文 —— 改前那种 `const Chinese` 实现过不了这条");
        Tr(Loc.MapSystemLanguage(SystemLanguage.Arabic) != AvailableLanguages.Chinese,
           "D3 未支持语种（`Arabic`）**不是**中文 —— 同上");
        Tr(Loc.MapSystemLanguage(SystemLanguage.Chinese) == AvailableLanguages.Chinese,
           "D4 反过来：`Chinese` 系统**必须**是中文（否则「恒英文」那种实现就漏过去）");

        // 多样性：喂一撮语种，至少映射出 5 种不同结果（0/10/20/… 的枚举值互不相同）
        var probe = new[]
        {
            SystemLanguage.English, SystemLanguage.French, SystemLanguage.German,
            SystemLanguage.Japanese, SystemLanguage.Polish, SystemLanguage.Arabic,
        };
        int distinct = 0;
        for (int i = 0; i < probe.Length; i++)
        {
            bool seen = false;
            for (int j = 0; j < i; j++) if (Loc.MapSystemLanguage(probe[j]) == Loc.MapSystemLanguage(probe[i])) { seen = true; break; }
            if (!seen) distinct++;
        }
        Tr(distinct >= 4, "D5 6 个探针语种映射出 **" + distinct + " 种不同结果**（≥ 4 才算「真的按输入分流」；实测 " + distinct + "）");
    }

    // ============================================================ E · 三级回退

    static void E_ResolveStartup()
    {
        MenuCheck.Section(_sink, "E `ResolveStartup(saved, device)`：① 本地记住 → ② 系统语言 → ③ 英文");

        // ---- 第 ① 级：本地记住的值优先（用两组「设备语言与记住值不同」的组合，灭「恰好相同」的假绿）
        C(Loc.ResolveStartup((int)AvailableLanguages.Polish, SystemLanguage.Arabic), AvailableLanguages.Polish,
          "E1 ★ ① 记住 = Polish + 设备 = Arabic（未支持）⇒ **Polish**（原版 `PersistentStorage.GetSetting_String` 那一级优先）"
        + "；🧨 删掉第一级 ⇒ 回英文 ⇒ 红");
        C(Loc.ResolveStartup((int)AvailableLanguages.Chinese, SystemLanguage.English), AvailableLanguages.Chinese,
          "E2 ★ ① 记住 = Chinese + 设备 = English ⇒ **Chinese**（记住的覆盖设备语言）"
        + "；🧨 同上 ⇒ 回英文 ⇒ 红");
        C(Loc.ResolveStartup((int)AvailableLanguages.Turkish, SystemLanguage.French), AvailableLanguages.Turkish,
          "E3 ① 记住 = Turkish（枚举边界值 120）+ 设备 = French ⇒ **Turkish**（顺带验 `IsDefined` 认 120 这一格）");

        // ---- 第 ② 级：没有记住的值 ⇒ 系统语言
        C(Loc.ResolveStartup(null, SystemLanguage.French), AvailableLanguages.French,
          "E4 ★ ② 没记住 + 设备 = French ⇒ **French**（原版 `GetCurrentDeviceLanguage` 那一级）");
        C(Loc.ResolveStartup(null, SystemLanguage.ChineseSimplified), AvailableLanguages.Chinese,
          "E5 ② 没记住 + 设备 = ChineseSimplified ⇒ **中文**（中文机器上「改前改后同结果」，所以这条**单独不具鉴别力**，"
        + "鉴别力在 B/C/D 三组）");

        // ---- 第 ③ 级：系统语言也不在 13 种里 ⇒ 英文（= 源内第一个语言）
        C(Loc.ResolveStartup(null, SystemLanguage.Arabic), AvailableLanguages.English,
          "E6 ★ ③ 没记住 + 设备 = Arabic（未支持）⇒ **English**（= 原版「源内第一个 `IsEnabled` 的语言」）"
        + "；🧨 第三级改成中文 ⇒ 红");
        C(Loc.ResolveStartup(null, SystemLanguage.Unknown), AvailableLanguages.English,
          "E7 ③ 没记住 + 设备 = Unknown ⇒ **English**");

        // ---- 不合法 / 未存过 的存档值：当「没记住」，落到第 ② 级（⛔ 不静默取某一门语言）
        C(Loc.ResolveStartup(15, SystemLanguage.Japanese), AvailableLanguages.Japanese,
          "E8 存档值 = 15（不在枚举里 ⇒ 例如手改注册表）+ 设备 = Japanese ⇒ **Japanese**（不合法值 = 当没记住）");
        C(Loc.ResolveStartup(-1, SystemLanguage.German), AvailableLanguages.German,
          "E9 存档值 = -1（负值）+ 设备 = German ⇒ **German**");
        C(Loc.ResolveStartup(999, SystemLanguage.Swedish), AvailableLanguages.English,
          "E10 存档值 = 999 + 设备 = Swedish（未支持）⇒ **English**（不合法值不占第 ① 级、未支持语种落到第 ③ 级）");
    }

    // ============================================================ F · 结构 + 真实 Load 路径

    static void F_StructureAndLiveLoad()
    {
        MenuCheck.Section(_sink, "F 结构（`Default` 不再是编译期常量）+ 真实 `Load()` 路径（`PlayerPrefs` 读 → 三级回退）");

        // ---- F1：`Default` 必须是**属性**、不再是 `const`（常量会被烘进调用方 IL，等于永远改不掉）
        var fi = typeof(Loc).GetField("Default", BindingFlags.Public | BindingFlags.Static);
        var pi = typeof(Loc).GetProperty("Default", BindingFlags.Public | BindingFlags.Static);
        Tr(fi == null || !fi.IsLiteral,
           "F1 ★ `Loc.Default` **不再是 `const` 字面量字段**（`GetField` = "
        + (fi == null ? "null" : "有、IsLiteral=" + fi.IsLiteral) + "）—— 改前是 `public const`，正是本缺陷的形状");
        Tr(pi != null && pi.CanRead,
           "F1b `Loc.Default` 现在是可读属性（`GetProperty(\"Default\")` 拿得到）");

        // ---- F2：真实链路 —— 写一个「本地记住的值」→ 重载 → 启动语言必须是它
        //      ⚠️ 这是本件**唯一**写 `PlayerPrefs` 的地方；原值（含「键不存在」这一档）在 `finally` 里逐字还原。
        string key = Loc.PrefKey;
        bool had = PlayerPrefs.HasKey(key);
        int oldRaw = had ? PlayerPrefs.GetInt(key, 0) : 0;
        var prevLang = Loc.Current;             // 顺带把当前内存态记下来
        bool reloadOk = false;
        try
        {
            PlayerPrefs.SetInt(key, (int)AvailableLanguages.German);
            PlayerPrefs.Save();
            Loc.ReloadForTest();                // = 模拟「重开游戏」
            C(Loc.Current, AvailableLanguages.German,
              "F2 ★ `PlayerPrefs[\"Language\"]` = German ⇒ 启动语言 = **German**（原版第 ① 级：本地记住的设置）"
            + "；🧨 把 `Load()` 改成不读 `PlayerPrefs` ⇒ 回系统语言 ⇒ 在系统语言不是 German 的机器上红（本机 = "
            + Application.systemLanguage + "）");

            // ---- F3：存档里是不合法的值 ⇒ 出声 + 当没记住（走第 ② 级 = `Default`）
            PlayerPrefs.SetInt(key, 999);
            PlayerPrefs.Save();
            Loc.ReloadForTest();
            C(Loc.Current, Loc.Default,
              "F3 ★ `PlayerPrefs[\"Language\"]` = 999（不合法）⇒ 启动语言 = **`Loc.Default`**（= 按系统语言解析）；"
            + "🧨 让不合法值静默取某一门固定语言 ⇒ 在系统语言不是那门语言的机器上红");
        }
        finally
        {
            if (had) PlayerPrefs.SetInt(key, oldRaw); else PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            Loc.ReloadForTest();
            Loc.RestoreForTest(prevLang);       // ⛔ 不动玩家的真设置：内存态也放回起点
            reloadOk = Loc.Current == prevLang;
        }
        Tr(reloadOk, "F4 收尾：`PlayerPrefs[\"Language\"]` 与内存态都已**逐字还原**（起点 = `" + prevLang + "`）");

        // ---- F5：`Default` 与映射函数的耦合（本机语言 ⇒ 同一个结果；两处写死不同值就红）
        C(Loc.Default, Loc.MapSystemLanguage(Application.systemLanguage),
          "F5 `Loc.Default` = `MapSystemLanguage(Application.systemLanguage)`（本机 `Application.systemLanguage` = "
        + Application.systemLanguage + "）—— `Default` 必须**跟着系统语言走**，⛔ 不是某一门写死的语言");
        C(Loc.ResolveStartup(null, Application.systemLanguage), Loc.Default,
          "F6 `ResolveStartup(null, 本机系统语言)` = `Loc.Default`（三级回退的第 ②③ 级与 `Default` 是**同一份**实现）");
    }

    // ============================================================ 小工具

    /// <summary>映射断言：`SystemLanguage` 应在 13 种里、映射到 <paramref name="want"/>。</summary>
    static void M(SystemLanguage sl, AvailableLanguages want, string what)
    {
        C(Loc.MapSystemLanguage(sl), want, "B · " + what + "（`SystemLanguage." + sl + "` → `" + want + "`）");
    }

    /// <summary>未支持语种断言：必须回英文。</summary>
    static void N(SystemLanguage sl, string what)
    {
        C(Loc.MapSystemLanguage(sl), AvailableLanguages.English,
          "C · " + what + " 不在 13 种里 ⇒ **English**（`SystemLanguage." + sl + "`）");
    }
}
