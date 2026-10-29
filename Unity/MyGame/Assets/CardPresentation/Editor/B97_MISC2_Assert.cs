// B97_MISC2_Assert.cs — 第九十七会话 · 执行代理 `B97_MISC2` 的断言（账 `未分类_其他.tsv` 的 12 条残余 B）
//
// ============================ 它盯的是什么（本批真正**改了代码**那几格的钉子）============================
//   A/B  `CardPresentation.ListExtensions.GetMax` / `GetMin`（账 `未分类_其他:398` / `:399`，本批**新建** `Shell/ListExtensions.cs`）
//        判据 = `decomp_full/ListExtensions__GetMax_object__int_.c`（体：`Append` + `Aggregate` 无种子）
//             ＋ 两个 lambda 的 `.c`（方向**只有 `.c` 读得出来**，ILSpy 两句都降级成 `return this`）：
//               `…GetMax_b__0.c` = `f(i1).CompareTo(f(i2)) > 0 ? i1 : i2`（**同分取后者**）
//               `…GetMin_b__0.c` = `f(i1).CompareTo(f(i2)) < 0 ? i1 : i2`（**同分取后者**）
//   C    `CursorByArmySO.GetCursor` 的**图那一半**（账 `:220`）：11 个阵营的 21 张 PNG 已落盘
//        （`Resources/Art/ui_menu/Cursor_*`，21 个文件 128×128 现读），本段**真的 `Resources.Load` 一遍**
//        —— 「表里有名字」不等于「图加载得出来」（B93S3 的 F1..F6 只钉表，没钉加载）。
//   D    `DeepCopy` 家族（账 `:99` / `:348`）：本仓**不实现深拷**，本段用反射把这条结论钉住
//        （判据 = 11 个 `*__DeepCopy.c` 之间互相调用、**家族外 0 调用点**，三源同结论）。
//
// 逐条结论 / 没查清的部分 → `资料/完全复刻/报告/B97_MISC2.md`（⛔ 别在这里抄第二份）。
//
// ============================ 怎么接（⚠️ 本代理不改宿主）============================
// 宿主建议 = **`Editor/ShellScene.cs`**（照 `B93S3Assert` 的先例：`Shell/*.cs` 全是它建的）：
//     1) 在 `Run()` 里加一句 `B97MISC2Assert.Run();`
//     2) 紧挨着加计数转发：`_sink.Pass += B97MISC2Assert.Pass; _sink.Fail += B97MISC2Assert.Fail;`
//   · 不跑 Unity 批处理、不建持久资产；建的临时对象在 `finally` 里 `DestroyImmediate`。
//   · 静态注入点（`GenericArmyItem.CurrentArmyProvider` 那类）本件**一个都不碰** ⇒ 没有跨宿主污染。
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」）============================
//   A1/A2/A3：把 `GetMax` 换成 `data.Select(f).Max()`（返回投影值）⇒ A1 编译不过 / A2 红（拿不到元素）；
//             把 lambda 改成 `>= 0 ? best : candidate`（同分取前者）⇒ A3 红（期望的是**后半段那个元素**）。
//   B1/B2：  把 `GetMin` 的方向 `>` 写成 `<` ⇒ B1 红（变成取最大）；同分取前者 ⇒ B2 红。
//   A4/B3：  把空序列改成 `return default` ⇒ A4/B3 红（期望抛 `InvalidOperationException`）。
//   C1..C3： 删掉任何一个 `Cursor_*` PNG（或让 Unity 不 import 它）⇒ 对应那一条红；
//             把 `CursorByArmy.Table` 里某个名字改成不存在的图名 ⇒ C2 红。
//   D1..D2： 给本仓的引擎类型加一个 `DeepCopy` 成员 ⇒ D1 红（那意味着我们开始做原版里**没人调用**的事）。
using System;
using System.Collections.Generic;
using System.Reflection;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B97_MISC2` 的断言（`GetMax`/`GetMin` · 指针图资源 · DeepCopy 家族）。
/// 计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。
/// <para>⛔ **期望值全是字面量/结构事实**，不从被测实现里读（例：A3 期望的是 `"b"` 那个**字面量**，
/// 不是「再跑一次 GetMax 得到的东西」）。</para></summary>
public static class B97MISC2Assert
{
    static readonly CheckSink _sink = new CheckSink("[MISC2] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败的条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    /// <summary>唯一入口（宿主 `Run()` 里调一句）。</summary>
    public static void Run()
    {
        SectionA_GetMax();
        SectionB_GetMin();
        SectionC_CursorArt();
        SectionD_DeepCopyFamily();
    }

    // ============================================================ 小工具
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void Eq<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }

    // ============================================================ A · GetMax（账 398）
    // 一条用于测「返回的是元素、不是投影值」的最小模型：`Tag` 是身份，`Score` 是投影。
    sealed class Row
    {
        public string Tag;
        public int Score;
        public Row(string tag, int score) { Tag = tag; Score = score; }
        public override string ToString() { return Tag + "(" + Score + ")"; }
    }

    static void SectionA_GetMax()
    {
        MenuCheck.Section(_sink, "A 原版 `ListExtensions.GetMax<T,TU>`：返回**元素** · 同分取**后者** · 空序列抛");

        // ---- A1：返回的是**元素**（不是 `f` 的返回值）
        var rows = new List<Row>
        {
            new Row("a", 5),
            new Row("b", 9),      // ★ 最大
            new Row("c", 2),
        };
        Row maxRow = ListExtensions.GetMax(rows, r => r.Score);
        Eq(maxRow != null ? maxRow.Tag : "<null>", "b",
           "A1 ★ 返回**元素**（`b`），不是投影值 9 —— `Select(f).Max()` 那种写法这一条必红");

        // ---- A2：单元素序列 ⇒ 原样返回那一个
        var single = new List<Row> { new Row("only", 7) };
        Eq(ListExtensions.GetMax(single, r => r.Score).Tag, "only", "A2 单元素 ⇒ 原样返回（= `Aggregate` 的种子那一档）");

        // ---- A3：同分取**后者**（判据 = `GetMax_b__0.c` 的 `if (0 < c) 取 i1; else 取 i2;`）
        var tie = new List<Row> { new Row("first", 9), new Row("second", 9), new Row("low", 1) };
        Eq(ListExtensions.GetMax(tie, r => r.Score).Tag, "second",
           "A3 ★ 同分取**后者**（`second`）—— 改成 `>= 0` 会得 `first`；这是 B94_B6 记反过、B96_MENU 按 `.c` 订正的那一格");

        // ---- A4：空序列 ⇒ InvalidOperationException（原版 `Aggregate` 无种子那一档）
        bool threw = false;
        try { ListExtensions.GetMax(new List<Row>(), r => r.Score); }
        catch (InvalidOperationException) { threw = true; }
        Tr(threw, "A4 空序列 ⇒ `InvalidOperationException`（⛔ 不是悄悄返回 `default`）");
    }

    // ============================================================ B · GetMin（账 399）
    static void SectionB_GetMin()
    {
        MenuCheck.Section(_sink, "B 原版 `ListExtensions.GetMin<T,TU>`：返回**元素** · 同分取**后者** · 空序列抛");

        var rows = new List<Row>
        {
            new Row("a", 5),
            new Row("b", 9),
            new Row("c", 2),      // ★ 最小
        };
        Eq(ListExtensions.GetMin(rows, r => r.Score).Tag, "c", "B1 ★ 取最小元素（`c`），不是最大值（方向写反这一条就红）");

        var tie = new List<Row> { new Row("first", 2), new Row("second", 2), new Row("high", 8) };
        Eq(ListExtensions.GetMin(tie, r => r.Score).Tag, "second",
           "B2 ★ 同分取**后者**（判据 = `GetMin_b__0.c` 的 `if (c < 0) 取 i1; else 取 i2;`）");

        bool threw = false;
        try { ListExtensions.GetMin(new List<Row>(), r => r.Score); }
        catch (InvalidOperationException) { threw = true; }
        Tr(threw, "B3 空序列 ⇒ `InvalidOperationException`（同 `GetMax`）");

        // ---- B4：`TU` 是 `int` 以外也成立（原版 `where TU : IComparable`；用 `long` 验约束没写窄）
        var longs = new List<Row> { new Row("p", 1), new Row("q", 3) };
        Eq(ListExtensions.GetMax(longs, r => (long)r.Score).Tag, "q", "B4 `TU = long` 也成立（原版约束是 `IComparable`，⛔ 别收紧成 `int`）");
    }

    // ============================================================ C · 指针图（账 220）
    static void SectionC_CursorArt()
    {
        MenuCheck.Section(_sink, "C 账 220 `CursorByArmySO.GetCursor` 的**图**那一半：11 阵营 / 21 张 PNG 能真的加载出来");

        int rows = 0, cursorOk = 0, glowOk = 0, glowMissing = 0;
        foreach (var e in CursorByArmy.Table)
        {
            rows++;
            var cur = CardArt.MenuUi(e.Cursor);
            if (cur != null) cursorOk++;
            if (e.Glow == null) { glowMissing++; continue; }     // Dark Angels：原版就没有 Glow
            if (CardArt.MenuUi(e.Glow) != null) glowOk++;
        }

        // 表 = 11 行（原版 `bundle_armycursors_assets_all` 的 21 个 Sprite json = 11 阵营，DA 缺 Glow）
        Eq(rows, 11, "C1 ★ `CursorByArmy.Table` = **11 行**（原版 21 张图 = 10 对 + Dark Angels 单张）");
        Eq(cursorOk, 11, "C2 ★ **11 张指针图全部加载出来**（`Resources/Art/ui_menu/Cursor_*`，128×128）—— 缺任何一张这条就红");
        Eq(glowOk, 10, "C3 ★ **10 张高光图全部加载出来**（Dark Angels 原版就没有 Glow ⇒ 表里是 `null`）");
        Eq(glowMissing, 1, "C3b 只有 Dark Angels 那 1 行是 `Glow == null`（⛔ 别顺手给它补一张别的阵营的高光）");

        // ---- C4：`GetCursor` 对「表里没有的阵营」⇒ false + 两个 out 都 null（B93S3 F4 已钉，这里只交叉一格）
        string c0, g0;
        bool found = CursorByArmy.GetCursor(0, out c0, out g0);
        Tr(!found && c0 == null && g0 == null,
           "C4 阵营 0（Neutral）/ 120 / 130 原版一张图都没有 ⇒ `GetCursor` 返回 false 且两个 out 都 null");
    }

    // ============================================================ D · DeepCopy 家族（账 99 / 348）
    static void SectionD_DeepCopyFamily()
    {
        MenuCheck.Section(_sink, "D 账 99/348 `DeepCopy` 家族：原版**家族外 0 调用点** ⇒ 本仓不实现深拷（结构钉住）");

        // 本仓「判据对象」的等价类型（原版 :99 = `BoardCriteria`、:348 = `HealCriteria`）。
        // ⚠️ 我们**没有** `BoardCriteria`/`HealCriteria` 这两个类（判据表落在 `CardDef.CountSetSpec` /
        //    `AiScoringData.AiHealCriteriaDto` 两处数据上，都是**只读共享**），所以这里钉的是「本仓的引擎类型
        //    里**一个** `DeepCopy` 成员都没有」—— 那正是「原版那 22 处深拷在本仓没有对应行为」的可执行表述。
        var probes = new[]
        {
            typeof(RuleEngine.CardDef),
            typeof(RuleEngine.CardDatabase),
            typeof(RuleEngine.BattleContext),
        };
        int clones = 0;
        string who = "";
        foreach (var t in probes)
        {
            var ms = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (var m in ms)
            {
                if (m.Name != "DeepCopy") continue;
                clones++;
                who += t.Name + "." + m.Name + " ";
            }
        }
        Eq(clones, 0, "D1 ★ 本仓引擎类型**没有** `DeepCopy` 成员（原版 11 个 `*__DeepCopy` 家族外 0 调用点 ⇒ 无可观测行为）" + (clones > 0 ? " —— 命中的：" + who : ""));

        // ---- D2：`RuleEngine` 程序集里**也没有别处**偷偷加了深拷（扫一遍全程序集的公开静态/实例方法名）
        int asmClones = 0;
        string asmWho = "";
        var asm = typeof(RuleEngine.CardDef).Assembly;
        Type[] all;
        try { all = asm.GetTypes(); }
        catch (ReflectionTypeLoadException e) { all = e.Types; all = Array.FindAll(all, x => x != null); }
        foreach (var t in all)
        {
            if (t == null || t == typeof(B97MISC2Assert)) continue;
            foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                if (m.Name == "DeepCopy") { asmClones++; asmWho += t.Name + "." + m.Name + " "; }
        }
        Eq(asmClones, 0, "D2 ★ `RuleEngine` 程序集全量扫：`DeepCopy` 成员 = 0（先把这条钉住，将来谁加就会红）" + (asmClones > 0 ? " —— 命中的：" + asmWho : ""));
    }
}
