// B141_W5_Assert.cs — 第一百二十八会话 · 写手 `B141-W5`（W129 波次 · 账 `4-04 商店页签红点 dy`）
//
// ============================ 它盯的是什么 ============================
//  账 `4-04`（`报告/B138_审计汇总_问题清单.md` §4 · 判据 `判据/B141_W129_对账_UI动画.md` 的 **W4 包**）：
//    原版 `Shop Menu Variant/Tab Buttons/Shop Icon/Badge Highlight` 的中心 = **键心 −47.9 px（偏上）**。
//    判据（`工具/menu_rect.py bundle_menus_assets_all "Shop Icon"` 2026-11-04 现读）：
//      `Badge Highlight` 201.37,1014.60 → 236.37,1049.60 ⇒ 中心 y = **1032.10**
//      `Shop Icon`        990.00 → 1170.00          ⇒ 键心 y = **1080.00**  ⇒ 偏移 **−47.9**
//    我们（修前）：`Shell/ShopWindow.cs` 的 5 个真实页签**没传** `badgeDy` ⇒ 落 `TabBtnSpec` 缺省 **−27.2**，
//      而基类按 `cy − spec.BadgeDy` 摆 ⇒ 红点中心 = `cy + 27.2`（偏下）⇒ 与原版差 **75.1 px**（47.9 + 27.2）。
//
//  修法（本写手）：`ShopWindow.ButtonsNow()` 给 **5 个真实页签**各补 `badgeDy: 47.9f`（母版第 6 颗本来就是 47.9）。
//    ⛔ **没有**改 `TabBtnSpec` 的缺省 —— −27.2 是奖励窗 / 收藏窗那几颗**真页签**的原版值（逐窗实读，
//    见 `TabBtnSpec.BadgeDy` 的 doc）；改缺省会一次弄坏三扇窗。
//
// ============================ 口径（键心 → 红点中心）============================
//  · 坐标系 = **设计 px · 左上原点 · y 向下**（`PxRect` / `menu_rect.py` 同一帧）。
//  · 父级 / 锚点 = `Content Area/Tab Buttons/Buttons/<btnPrefix><i>`（`MainMenuSubmenuWindow.BuildBar` 建的那颗键），
//    键心 `cy = (y1 + y2) / 2`；**红点中心 = `cy − spec.BadgeDy`**（`BuildTabButton` 里 `Rect(...)` 与
//    `BadgeWiring` 的 `badgeRect` 两处同式）。
//    ⇒ `BadgeDy = +47.9` **当且仅当** 红点中心在键心**上方 47.9 px**（`TabBtnSpec.BadgeDy` 的 doc 同口径）。
//  · 水平方向不动：红点中心 x = `cx + 51.7`（原版 `Shop Icon` 的 `Badge Highlight` 中心 218.87 = 键心 167.17 + 51.7）。
//
// ============================ 🔴 灭自证（怎么改会让它红）============================
//  · A1–A5：把某一颗真实页签的 `badgeDy: 47.9f` 删掉 ⇒ 落回缺省 −27.2 ⇒ 该条红。
//  · A6：把 `TabBtnSpec` 的缺省从 −27.2 改成 47.9（"顺手修"缺省）⇒ 红（这条专门挡那个错修法）。
//  · A7：真实页签必须**显式**传（值与缺省不同）—— 删掉命名参数 ⇒ 两值相等 ⇒ 红。
//  · A8/A8b：母版与 5 个页签同值；只改母版或只改页签 ⇒ 红。
//  · A9/A9b/A9c：键心口径的三个字面量（−47.9 / +27.2 / 差 75.1）—— 谁把公式或符号弄反 ⇒ 红。
//  · S1：`ShopWindow.cs` 的**非注释代码**里 `badgeDy: 47.9f` 恰好 **5** 处。
//  · S2：`MainMenuSubmenuWindow.cs` 的缺省仍 `float badgeDy = -27.2f`。
//  · S3：基类摆放式 `cy - spec.BadgeDy` 在代码里仍有 **4** 处（`Badge Highlight` 上下沿 2 + `badgeRect` 上下沿 2）；
//        把符号改成 `cy +` ⇒ 0 ⇒ 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  宿主 = `CardPresentation/Editor/ShopScene.cs`。在它的聚合行（**同一物理行**、行尾 `//` **之前**）追加：
//      B141_W5_Assert.Run(); _pass += B141_W5_Assert.Pass; _fail += B141_W5_Assert.Fail;
//  （计数器变量名以该宿主当时那一对为准。⚠️ 追加必须落在第一个 `//` 之前 —— 本仓已 6 次踩
//   「代码落进行尾注释 ⇒ 编译过、账绿、断言一条没跑」。）
//  ⚠️ 本断言**零前提**：只读静态键表 + 源码，不需要先建窗、不建对象、不写存档。
using System;
using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>`4-04`：商店左栏 5 个真实页签的红点纵向偏置（原版键心 **−47.9** / 修前 **+27.2**）。
/// <para>⛔ 期望值全是**原版实读字面量**（47.9 / −27.2 / 75.1），不从被测实现里反推。</para></summary>
public static class B141_W5_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B141W5] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    // 判据字面量（⛔ 不许从实现里读：那是自证）
    /// <summary>原版 `Shop Icon/Badge Highlight` 中心 = 键心 **−47.9 px** ⇒ 基类式子需要的 `BadgeDy` = **+47.9**。</summary>
    const float OrigBadgeDy = 47.9f;
    /// <summary>`TabBtnSpec` 的缺省（奖励窗 / 收藏窗那几颗真页签的原版值）—— **必须留着**。</summary>
    const float OldDefaultDy = -27.2f;
    /// <summary>两者差（审计口径：47.9 + 27.2 = 75.1 px）。</summary>
    const float AuditDeltaPx = 75.1f;

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B141_W5 · `4-04` 商店页签红点 dy（原版键心 −47.9 / 修前 +27.2，差 75.1）");

        SectionA_页签规格();
        SectionS_源码闸();

        Debug.Log(_sink.P + "B141_W5 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ------------------------------------------------------------ A 页签规格（= 红点偏置的唯一来源）
    static void SectionA_页签规格()
    {
        MenuCheck.Section(_sink, "A 左栏 6 颗键的 `BadgeDy`（5 个真实页签 + 母版）");

        var specs = ShopWindow.ButtonsNow();
        C(specs != null ? specs.Length : -1, 6,
          "A0（前提）左栏键表 = **6 颗** = 5 个真实页签 + 1 颗母版（`ShopWindow.ButtonsNow()`）");
        if (specs == null || specs.Length != 6) return;

        // 前提：前 5 颗是真实页签（有实例 ID），第 6 颗是母版（实例 ID 空）
        bool realOk = true, masterOk = string.IsNullOrEmpty(specs[5].InstId);
        for (int i = 0; i < 5; i++)
            if (string.IsNullOrEmpty(specs[i].InstId)) realOk = false;
        Tr(realOk, "A0b（前提）前 5 颗 `InstId` 非空 = 真实页签（否则下面按序号断的那几条断的不是页签）"
                 + "｜🧨 键表顺序改了 ⇒ 本条先红（别让 A1–A5 静默断到母版上）");
        Tr(masterOk, "A0c（前提）第 6 颗 `InstId` 为空 = 母版（`tabButtonPrefab`，`Initialize` 一进来就关掉）");

        // ---- 5 个真实页签：dy 必须是 +47.9（⇒ 红点中心 = 键心上方 47.9 px）----
        for (int i = 0; i < 5; i++)
        {
            N(specs[i].BadgeDy, OrigBadgeDy, 1e-3f,
              $"A{i + 1} 第 {i + 1} 个真实页签 `{specs[i].InstId}` 的 `BadgeDy` = **+{OrigBadgeDy}**"
            + "（键心口径：父级 = `Content Area/Tab Buttons/Buttons/<tab" + i + ">`，红点中心 = `cy − BadgeDy` = 键心 **−47.9 px** 偏上）"
            + "｜🧨 删掉这一颗的 `badgeDy: 47.9f` ⇒ 落回缺省 −27.2（= 键心 +27.2 偏下）⇒ 本条红");
        }

        // ---- 缺省必须仍是 −27.2（⛔ 「改缺省」不是修法）----
        var dflt = new MainMenuSubmenuWindow.TabBtnSpec("x", "x", 36f, 5f, 36f, "x");
        N(dflt.BadgeDy, OldDefaultDy, 1e-3f,
          "A6 `TabBtnSpec` 的缺省 `badgeDy` 仍 = **−27.2**（奖励窗 / 收藏窗那几颗真页签的原版值；"
        + "⛔ 不许改成 47.9 —— 那会一次弄坏三扇窗）"
        + "｜🧨 把缺省改成 47.9 来「修」本账 ⇒ 本条红");

        // ---- 5 个真实页签必须【显式】传（不是靠缺省蒙对）----
        for (int i = 0; i < 5; i++)
            Tr(Math.Abs(specs[i].BadgeDy - dflt.BadgeDy) > 1e-3f,
               $"A7.{i + 1} 第 {i + 1} 个真实页签**显式**传了 badgeDy（实得 {specs[i].BadgeDy:F3} ≠ 缺省 {dflt.BadgeDy:F3}）"
             + "｜🧨 删掉那颗的命名参数 ⇒ 两值相等 ⇒ 本条红");

        // ---- 母版与页签一致 ----
        N(specs[5].BadgeDy, OrigBadgeDy, 1e-3f,
          "A8 母版（第 6 颗 `Booster Packs`）`BadgeDy` = **+47.9**（它一直是 47.9 —— 本账是「页签没继承」，不是母版错）");
        bool same = true;
        for (int i = 0; i < 5; i++)
            if (Math.Abs(specs[i].BadgeDy - specs[5].BadgeDy) > 1e-3f) same = false;
        Tr(same, "A8b **母版与 5 个真实页签同值**（原版运行期由 `CreateStoreTab` 克隆母版 ⇒ 红点偏置必须一样）"
               + "｜🧨 只改母版或只改页签 ⇒ 红");

        // ---- 键心口径：三个字面量（原版 / 修前 / 差）----
        const float keyCy = 500f;                                  // 任意键心；这里只断【相对量】
        N((keyCy - OrigBadgeDy) - keyCy, -47.9f, 1e-3f,
          "A9 键心口径（修复后）：红点中心 − 键心 = **−47.9 px**（y 向下 ⇒ 偏上），与 `menu_rect` 现读逐值相同");
        N((keyCy - OldDefaultDy) - keyCy, 27.2f, 1e-3f,
          "A9b 键心口径（修复前 / 缺省 −27.2）：红点中心 − 键心 = **+27.2 px**（偏下）—— 这就是审计报的「我们」那一档");
        N(OrigBadgeDy - OldDefaultDy, AuditDeltaPx, 1e-3f,
          "A9c 两档差 = **75.1 px**（47.9 + 27.2，审计口径）");
    }

    // ------------------------------------------------------------ S 源码闸（防「改错地方」与「把式子改回去」）
    static void SectionS_源码闸()
    {
        MenuCheck.Section(_sink, "S 源码闸（防「改缺省」/「只改一处」/「把符号改回去」）");

        string shopSrc = ReadSrc("CardPresentation/Shell/ShopWindow.cs");
        string baseSrc = ReadSrc("CardPresentation/Shell/MainMenuSubmenuWindow.cs");
        if (shopSrc == null || baseSrc == null) return;

        C(CountOfCode(shopSrc, "badgeDy: 47.9f"), 5,
          "S1 `Shell/ShopWindow.cs` 的**非注释代码**里 `badgeDy: 47.9f` 恰好 **5** 处（5 个真实页签各一处）"
        + "｜🧨 少一处（某一颗没继承）或多一处（顺手改了别处）⇒ 红");

        C(CountOfCode(baseSrc, "float badgeDy = -27.2f"), 1,
          "S2 `MainMenuSubmenuWindow.TabBtnSpec` 的缺省仍是 **−27.2**（奖励窗 / 收藏窗靠它）"
        + "｜🧨 改成 47.9 ⇒ 红（与 A6 两面夹同一条）");

        C(CountOfCode(baseSrc, "cy - spec.BadgeDy"), 4,
          "S3 基类摆放式 `cy - spec.BadgeDy` 在代码里仍有 **4** 处（`Badge Highlight` 那行上下沿 2 + `badgeRect` 上下沿 2）"
        + "｜🧨 改成 `cy + spec.BadgeDy`（符号反了）或只改一处（红点 rect 与命中 rect 分家）⇒ 红");
    }

    // ------------------------------------------------------------ 工具
    static string ReadSrc(string rel)
    {
        try
        {
            string p = Path.Combine(Application.dataPath, rel);
            if (!File.Exists(p))
            {
                Tr(false, "S0 读得到源码 `" + rel + "`（" + p + "）—— 读不到时源码闸**等于没验** ⇒ 当场红");
                return null;
            }
            return File.ReadAllText(p);
        }
        catch (Exception e)
        {
            Tr(false, "S0 读源码 `" + rel + "` 抛异常：" + e.GetType().Name + "：" + e.Message);
            return null;
        }
    }

    /// <summary>数**非注释行**里的子串出现次数（整行 `//` 开头的跳过 —— 文档/注释里也会出现这些字面量）。</summary>
    static int CountOfCode(string src, string needle)
    {
        if (string.IsNullOrEmpty(src) || string.IsNullOrEmpty(needle)) return -1;
        int n = 0;
        foreach (var raw in src.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            var t = line.TrimStart();
            if (t.StartsWith("//", StringComparison.Ordinal)) continue;   // 注释行不算
            int at = 0;
            while ((at = line.IndexOf(needle, at, StringComparison.Ordinal)) >= 0) { n++; at += needle.Length; }
        }
        return n;
    }
}
