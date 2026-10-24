// ModeWindowAssertE11.cs — 模式窗（经典 / 小冲突）**空态与卡组栏判据**的断言（执行代理 E11，2026-10-24）
//
// ============================ 它盯的是什么（判据出处）============================
// 用户 2026-10-24 点名三处「与原版不符」：① 左边栏不对 ② 中间多了一个大阵营图标 ③ 没有该阵营卡组时
// 不该画督军立绘（现在恒是灵族）。审查与判据正本 → `资料/普查产出_1024b/R1_模式窗审查_经典与小冲突.md`。
// 第一权威 = 反编译 `d:/2/tools/decomp_full/RankedDeckSelector*.c`：
//   · `ChangeDeck.c:64-87` —— `deck != null` ⇒ `warlordImage/warlordName/deckName/deckQuantity` 开、
//     `factionIconImage` + `noDeckText` 关；`deck == null` ⇒ **反过来**，且徽记喂 `GetArmyIcon(currentArmy)`；
//   · `ChangeDeck.c:105-119` —— 张数文本 = `String.Format("{0}/{1}", 表.IndexOf(deck)+1, 表.Count)`；
//   · `ChangeArmy.c` —— 表 = 自有卡组里「阵营 ∧ 模式」的那几套；四颗圆钮按表长开关
//     （`0x20/0x28 previous/next = SetActive(1 < Count)` · `0x30/0x38 select/view = SetActive(0 < Count)`）；
//   · `Initialize.c:15-22` —— `currentArmy` 初值 = 当前卡组督军的阵营、没有卡组 ⇒ **10 = Ultramarines**。
//   ⛔ 粉丝规则书不是官方（`CLAUDE.md` 二·2 那条判据）。
//
// ============================ 怎么用（宿主）============================
// `ModeWindowAssertE11.Run();` —— 只读全局状态 + 自建临时存档（`DeckStore.OverridePath` 指到
// `_tmp_view/e11/`），**不碰玩家真存档**，跑完自己复原。宿主建议挂 `Editor/MainMenuScene.cs` 的
// 遭遇战 / 排位那一节之后（那里已经有 `SkirmishEventWindow` / `RankedEventWindow` 的现场）。
//
// 🔴 **灭自证**（`CLAUDE.md` 三·「灭自证断言」）：A2 断「空态**显示**徽记」、B2 断「有卡组**关着**徽记」——
//    两条形状相反 ⇒ 「恒画徽记」或「恒不画」都必有一条红。同理 A1（空态 `DeckIndex = -1`）与
//    B1（有卡组 `DeckIndex >= 0`）。🧨 改坏法写在每一条的文案里。
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 E11：**模式窗（经典 / 小冲突）卡组栏**的断言。
/// <para>计数口径照 `CheckSink`（`Editor/MenuCheck.cs`，⛔ 不用全局 static —— 见那里的判据）。</para></summary>
public static class ModeWindowAssertE11
{
    static readonly CheckSink _sink = new CheckSink("[E11] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读，与 `Fail` 一起决定这条自检算不算绿）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void Check<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void CheckTrue(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void CheckNear(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    // ------------------------------------------------------------ 原版 prefab 的几何（⛔ 不读被测常量）
    /// <summary>`Deck Buttons` 那颗 HLG 的矩形 = `640.84,816.03 → 1265.16,1033.97`
    /// ⇒ 栏心 x = **953.00**；每颗钮宽 **160.87**（原版 prefab 实读，同 `Editor/MainMenuScene.cs` 里
    /// `Dk Buttons/Previous` 那一条用的同一组数）。布局组**跳过 `SetActive(false)` 的子件** ⇒
    /// 可见的那几颗从左到右紧排、整排以栏心居中。
    const float BarCx = 953.00f, BtnW = 160.87f;
    static readonly string[] BtnNames =
    { "Previous Deck Button", "Change Deck Button", "View Deck Button", "Next Deck Button" };

    static Transform Find(Transform root, string name) { return MenuCheck.FindChild(root, name); }

    static string TextOf(Transform t)
    {
        if (t == null) return null;
        var lb = t.GetComponent<Label>();
        if (lb == null) lb = t.GetComponentInChildren<Label>(true);
        return lb != null ? lb.Text : null;
    }

    static ImageQuad QuadOf(Transform t)
    {
        if (t == null) return null;
        var qs = t.GetComponentsInChildren<ImageQuad>(true);   // **含关着的**（空态那些件整棵是关的）
        return qs.Length > 0 ? qs[0] : null;
    }

    /// <summary>四颗圆钮里有几颗 `activeSelf`（原版靠 `SetActive` 关，不是不建）。</summary>
    static int VisibleButtons(Transform root)
    {
        int n = 0;
        foreach (var nm in BtnNames)
        {
            var t = Find(root, nm);
            if (t != null && t.gameObject.activeSelf) n++;
        }
        return n;
    }

    /// <summary>某颗钮的设计帧左沿（px）。量不到 ⇒ 哨兵（`-99999`，同 `MenuCheck.DesignPx` 的纪律）。</summary>
    static float BtnLeft(Transform root, string name)
    {
        var t = Find(root, name);
        return t != null ? MenuCheck.DesignPx(t).x - BtnW * 0.5f : -99999f;
    }

    /// <summary>节点在不在（**取不到 ⇒ false**，⛔ 不抛 —— `Run()` 里抛异常会把宿主的整条自检打断）。</summary>
    static bool IsActive(Transform root, string name)
    {
        var t = Find(root, name);
        return t != null && t.gameObject.activeSelf;
    }

    /// <summary>同名子件计数（**P3 那两颗重复的 `Generic Simplified UI Button`** 靠它分得出 1 份 / 2 份）。</summary>
    static int CountNamed(Transform root, string name)
    {
        if (root == null) return 0;
        int n = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) n++;
        return n;
    }

    /// <summary>阵营名 → `Factions()` 下标（**独立取一份**：不读 `LiveOpsEventWindow._facs` 那个私有字段）。</summary>
    static int IdxOf(List<string> facs, string faction)
    {
        for (int i = 0; i < facs.Count; i++) if (DeckRules.SameFaction(facs[i], faction)) return i;
        return -1;
    }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "E11 模式窗（经典 / 小冲突）—— 空态 / 圆钮显隐 / 张数 / 选中阵营");
        string keepPath = DeckStore.OverridePath;
        string tmp = "d:/4/_tmp_view/e11/_mode_window_decks.json";
        var keepLast = LiveOpsEventWindow.LastOpened;
        SkirmishEventWindow sk = null;
        RankedEventWindow rk = null;
        try
        {
            System.IO.Directory.CreateDirectory("d:/4/_tmp_view/e11");
            DeckStore.OverridePath = tmp;
            try { System.IO.File.Delete(tmp); } catch { }
            CollectionData.ResetForTest();

            // ---------------------------------------------------------- 夹具：三套卡组
            string umHero = null, goffHero = null;
            foreach (var c in CardDatabase.Load())
            {
                if (c.Type != "hero") continue;
                if (umHero == null && DeckRules.SameFaction(c.Faction, "Ultramarines")) umHero = c.Id;
                if (goffHero == null && DeckRules.SameFaction(c.Faction, "Goff")) goffHero = c.Id;
                if (umHero != null && goffHero != null) break;
            }
            CheckTrue(umHero != null && goffHero != null,
                      "（夹具）卡池里有 Ultramarines / Goff 的督军（下面各条全靠它们才有阵营可判）");
            if (umHero == null || goffHero == null) return;

            var lib = DeckLibrary.Load();
            var dClassic = lib.Create("E11 经典-Ultramarines", (int)GameMode.Classic);
            dClassic.WarlordId = umHero;
            var dSk1 = lib.Create("E11 小冲突-Goff-1", (int)GameMode.Skirmish);
            dSk1.WarlordId = goffHero;
            var dSk2 = lib.Create("E11 小冲突-Goff-2", (int)GameMode.Skirmish);
            dSk2.WarlordId = goffHero;
            lib.Save();

            var facs = CollectionWindow.CardsState.Factions();
            int umIdx = IdxOf(facs, "Ultramarines"), goffIdx = IdxOf(facs, "Goff");
            CheckTrue(umIdx >= 0 && goffIdx >= 0, "（夹具）`Factions()` 里 UM 与 Goff 两格都在");

            // ========================================================== 场景 A：小冲突窗 + 当前是经典卡组 ⇒ **空态**
            // 这正是用户报的那个现场：本机只有一套经典卡组，小冲突窗以前会把它画出来。
            CheckTrue(CollectionData.Select(0), "（夹具）当前选中 = 第 0 套（经典 / Ultramarines）");
            sk = SkirmishEventWindow.Create(null);
            sk.Open();

            Check(sk.DeckIndex, -1,
                  "★ A1 当前卡组是**经典**那套、本窗模式（小冲突）**一套都没有** ⇒ `DeckIndex = -1`（空态）"
                + "｜🧨 两种改坏法都会红：① `FirstDeckInMode` 加回 `return (prefer>=0&&prefer<n) ? prefer : 0;`"
                + "（拿别的模式的卡组顶上）⇒ 0；② 改成「按模式跨阵营吸附」（本夹具里就有一套 Goff 小冲突卡组）"
                + "⇒ 1 —— 原版 `Initialize → ChangeArmy` 的表**带阵营条件**，不会吸到别的阵营去");
            Check(sk.HasEffectiveDeck, false,
                  "★ A1b `HasEffectiveDeck = false`（预组没选 + 当前那套不在「阵营 ∧ 模式」那张表里）");
            Check(sk.SelectedArmyFaction, "Ultramarines",
                  "★ A1c 选中阵营 = **当前卡组督军的阵营**（UM）—— 原版 `Initialize.c:15-22`，"
                + "**只有当前卡组为空**才回落 `10`；🧨 改成「回落 UM 但被别的阵营吸附走」⇒ 红");

            var fac = Find(sk.transform, "Faction Icon Image");
            CheckTrue(fac != null && fac.gameObject.activeSelf,
                      "★ A2 空态 ⇒ `Faction Icon Image` **显示**（原版 `ChangeDeck.c:80-83` 的 `SetActive(deck == null)`）");
            var facQ = QuadOf(fac);
            Check(facQ != null ? facQ.Texture.name : null, ArmyIcons.IconName("Ultramarines"),
                  "★ A2b 空态徽记 = **选中阵营的 `ArmyUtilities.GetArmyIcon`** 那一张（UM 徽记）"
                + "｜🧨 改回 `DeckRuntime.FactionIcon(ShownFaction)`（空串 ⇒ `40k_collection_bt_decks` 卡组图标）⇒ 红");

            CheckTrue(!IsActive(sk.transform, "Warlod Image"),
                      "★ A3 空态 ⇒ `Warlod Image` **关着**（用户点名「没有该阵营卡组不该画督军立绘」）"
                    + "｜🧨 改回 `var wl = ShownWarlord;`（有督军就画）+ 不 SetActive ⇒ 红");
            var dnN = Find(sk.transform, "Deck Name");
            CheckTrue(!IsActive(sk.transform, "Deck Name"), "★ A3b 空态 ⇒ `Deck Name` 关（原版 `deckName`）");
            CheckTrue(!IsActive(sk.transform, "Deck Warlord"), "★ A3c 空态 ⇒ `Deck Warlord` 关（原版 `warlordName`）");
            var cntN = Find(sk.transform, "Numer Of Army Decks");
            CheckTrue(!IsActive(sk.transform, "Numer Of Army Decks"),
                      "★ A3d 空态 ⇒ `Numer Of Army Decks` 关（原版 `deckQuantity`，连图标一起不显示）");

            var none = Find(sk.transform, "No Deck");
            CheckTrue(none != null && none.gameObject.activeSelf, "★ A4 空态 ⇒ `No Deck`（提示 + `Create deck`）显示");
            Check(TextOf(Find(none, "No Deck Text")), Loc.T("MainMenu/Ranked/NoWarlordMessage"),
                  "★ A4b 该阵营 **0 套** ⇒ 提示 = 官方 `MainMenu/Ranked/NoWarlordMessage`"
                + "（原版 `ChangeDeck.c` 的 `count < 1` 支；反编译只给字符串指针 ⇒ 键属推断，见代码注释）");
            var cbtn = Find(none, "Generic Simplified UI Button");
            CheckTrue(cbtn != null && Find(cbtn, "Button Text") != null,
                      "★ A5 空态唯一的出路 `Create deck` 钮在（`Button Text` 挂在钮底下）");
            Check(CountNamed(none, "Generic Simplified UI Button"), 1,
                  "★ A5b `No Deck` 底下 `Generic Simplified UI Button` **只有一份**"
                + "｜🧨 改坏法：把 P3 删掉的那句 `MenuDraw.Rect(none, …, \"Generic Simplified UI Button\", …)` 加回来 ⇒ 2 份、红");

            Check(VisibleButtons(sk.transform), 0,
                  "★ A6 空态（表长 0）⇒ **四颗圆钮全关**（原版 `0 < Count` / `1 < Count` 两条都假）"
                + "｜🧨 改回「恒画 4 颗」⇒ 4，红");
            CheckTrue(sk.ArmyIndex >= 0,
                      "★ A7 开窗 `ArmyIndex` **不再恒 −1**（原版 `currentArmy` 一定是个具体阵营；参考图右栏左上 UM 格是按下态）");
            Check(sk.ArmyIndex, umIdx, "★ A7b …且正好是 UM 那一格（下标 " + umIdx + "）");

            // ========================================================== 场景 B：小冲突窗 + 当前是小冲突卡组 ⇒ **有卡组**
            CheckTrue(CollectionData.Select(1), "（夹具）当前选中 = 第 1 套（小冲突 / Goff）");
            sk.Open();

            Check(sk.DeckIndex, 1, "★ B1 吸附到本窗模式的第一套（第 1 套 Goff 那套）（有卡组一侧，与 A1 形状相反）");
            Check(sk.HasEffectiveDeck, true, "★ B1b `HasEffectiveDeck = true`");
            fac = Find(sk.transform, "Faction Icon Image");
            CheckTrue(fac != null && !fac.gameObject.activeSelf,
                      "★ B2 **有卡组 ⇒ 徽记关着**（原版 `SetActive(deck == null)` ⇒ false）"
                    + "｜🧨 这条与 A2 一起构成灭自证：恒画 / 恒不画都必红一条");
            CheckTrue(IsActive(sk.transform, "Warlod Image"), "★ B3 有卡组 ⇒ `Warlod Image` 显示");
            dnN = Find(sk.transform, "Deck Name");
            CheckTrue(IsActive(sk.transform, "Deck Name"), "★ B3b 有卡组 ⇒ `Deck Name` 显示");
            Check(TextOf(dnN), "E11 小冲突-Goff-1", "★ B3c …而且画的是**当前那套**的名字");
            none = Find(sk.transform, "No Deck");
            CheckTrue(!IsActive(sk.transform, "No Deck"), "★ B4 有卡组 ⇒ `No Deck` 关");

            cntN = Find(sk.transform, "Numer Of Army Decks");
            CheckTrue(IsActive(sk.transform, "Numer Of Army Decks"), "★ B5 有卡组 ⇒ 张数显示");
            Check(TextOf(cntN), "1/2",
                  "★ B5b 张数 = **`{该套在表里的序号+1}/{该阵营该模式套数}`** = `1/2`"
                + "｜🧨 改回 `ArmyDeckCount() + \"/\" + MaxDecksPerArmy`（旧写法 = `2/20`）⇒ 红");

            Check(VisibleButtons(sk.transform), 4,
                  "★ B6 表长 2 ⇒ **四颗圆钮全开**（原版 `1 < Count` 真）");
            CheckNear(BtnLeft(sk.transform, "Previous Deck Button"), BarCx - BtnW * 2f, 0.6f,
                      "★ B6b 四颗时第 1 颗左沿 = 栏心 953.00 − 4×160.87/2 = **631.26**（原版 HLG 居中）");
            var pressed = QuadOf(Find(sk.ArmyCells.Count > goffIdx ? sk.ArmyCells[goffIdx] : null, "Background"));
            Check(pressed != null ? pressed.Texture.name : null, "UI_Army_Selection_Back_Pressed",
                  "★ B7 右栏**选中阵营那一格**用按下图（`onSprite`；原版 `EverguildToggle` 的选中态）"
                + "｜🧨 改回 `ArmyIndex = -1`（一格都不亮）⇒ 这里是 `UI_Army_Selection_Back`、红");

            // ========================================================== 场景 C：点一个「本模式没有卡组」的阵营 ⇒ 进空态
            // = 原版 `ChangeArmy` 语义：不是「找不到就原地不动」，而是「表空 ⇒ 空态」。
            if (sk.ArmyCells.Count > umIdx && umIdx >= 0)
            {
                var cellHit = Find(sk.ArmyCells[umIdx], "Hit");
                var cellBtn = cellHit != null ? cellHit.GetComponent<WindowButton>() : null;
                CheckTrue(cellBtn != null, "（前提·C）UM 那一格有点击区（下面两条靠真点击）");
                if (cellBtn != null)
                {
                    cellBtn.Click();
                    Check(sk.SelectedArmyFaction, "Ultramarines",
                          "★ C1 点 UM 那格 ⇒ 选中阵营换成 UM（原版 `ChangeArmy`）");
                    Check(sk.DeckIndex, -1,
                          "★ C1b …而且**画面真的变了**：UM 没有小冲突卡组 ⇒ `DeckIndex` 1 → **−1**（空态）"
                        + "｜🧨 改回旧 `PickArmy`（找不到就什么都不动）⇒ 这里还是 1、红");
                    CheckTrue(IsActive(sk.transform, "No Deck"), "★ C1c 空态提示跟着出现");
                    Check(VisibleButtons(sk.transform), 0, "★ C1d …四颗圆钮跟着全关（表长 0）");
                    var fac2 = Find(sk.transform, "Faction Icon Image");
                    var facQ2 = QuadOf(fac2);
                    CheckTrue(fac2 != null && fac2.gameObject.activeSelf
                              && facQ2 != null && facQ2.Texture.name == ArmyIcons.IconName("Ultramarines"),
                              "★ C1e …徽记重新显示、且是**新选中阵营**（UM）那一张");
                }
            }
            else CheckTrue(false, "（前提·C）阵营列里拿不到 UM 那一格 ⇒ 下面四条没查");

            // ========================================================== 场景 D：经典窗 + 该阵营只有 **1 套** ⇒ 只留 2 颗钮
            CheckTrue(CollectionData.Select(0), "（夹具）当前选中 = 第 0 套（经典 / Ultramarines）");
            rk = RankedEventWindow.Create(null);
            rk.Open();
            Check(rk.DeckIndex, 0, "★ D1 经典窗吸附到第 0 套（UM 经典）");
            Check(VisibleButtons(rk.transform), 2,
                  "★ D2 表长 **1** ⇒ 只剩 **2 颗**（换卡组 / 看卡组；原版 `previous/next = SetActive(1 < Count)`）"
                + "｜🧨 改回恒画 4 颗 ⇒ 4，红");
            CheckTrue(!IsActive(rk.transform, "Previous Deck Button")
                      && !IsActive(rk.transform, "Next Deck Button"),
                      "★ D2b …关的正好是**上一套 / 下一套**两颗（不是另外两颗）");
            CheckNear(BtnLeft(rk.transform, "Change Deck Button"), BarCx - BtnW, 0.6f,
                      "★ D2c 还剩 2 颗 ⇒ 重排后第 1 颗左沿 = 953.00 − 2×160.87/2 = **792.13**"
                    + "（不重排的话会停在 631.26 ⇒ 红）");
            CheckNear(BtnLeft(rk.transform, "View Deck Button"), BarCx, 0.6f,
                      "★ D2d …而第 2 颗的左沿 = **953.00**（两颗以栏心对称）");
            Check(TextOf(Find(rk.transform, "Numer Of Army Decks")), "1/1",
                  "★ D3 张数 = **`1/1`**（参考图 1 套时就是 `1/1`；旧写法是 `1/20`）");
            CheckTrue(!IsActive(rk.transform, "Faction Icon Image"),
                      "★ D4 经典窗有卡组 ⇒ 徽记同样关着（两扇窗同一份判据）");
            Check(rk.ArmyIndex, umIdx, "★ D4b 右栏选中格 = 当前卡组阵营（UM）");
        }
        catch (System.Exception ex)
        {
            // 🔴 **不许让异常静默跳过一整节**：抛了就记一条**红**（判据同「静默失败」那条红线）。
            CheckTrue(false, "★ E11 断言自身抛异常（这一条算**红**、⛔ 不是跳过）："
                             + ex.GetType().Name + " · " + ex.Message);
        }
        finally
        {
            // ---- 收尾：拆窗 + 复原存档指向（⛔ 不留窗、不碰玩家真存档）
            if (sk != null) RewardsWindow.DestroySafe(sk.gameObject);
            if (rk != null) RewardsWindow.DestroySafe(rk.gameObject);
            LiveOpsEventWindow.LastOpened = keepLast;
            DeckStore.OverridePath = keepPath;
            try { System.IO.File.Delete(tmp); } catch { }
            CollectionData.ResetForTest();
            Debug.Log(_sink.P + "合计：通过 " + _sink.Pass + " / 失败 " + _sink.Fail
                      + (keepPath == null ? "" : "（存档指向已还原）"));
        }
    }
}
