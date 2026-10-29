// B98A6_Assert.cs — 执行代理 `B98_A6`（第九十八会话 · 波 5c · 2026-10-29）
//
// ============================ 它盯的是哪一行账 / 哪一条可见偏差 ============================
//  · **`特效动画:195`** — `CardEffectItem.LoadEffect(CardEffect effect, bool isPlayer)`。
//    本批只改其中**一处**：`buffType ∈ {markOfChaos(8), lastMarkOfChaos(17)}` 那一支的**右列**
//    （原版 `effectText = GetEffectDesc(effect)` = `Battle/Effect/<神名>` 的译文；我们原来是空串）。
//  · 落点 = `Battle/CardDisplayWindow.cs`：`MarkRow`（生产口，`RowsOfEffects` 用它）＋ 旧口 `RowsOf`
//    ＋ 类尾新增的 `MarkGodEffectKey` / `MarkGodEffectText`。
//  · 判据（⛔ 别在这里抄第二份）→ 类尾 `MarkGodEffectKey` 上方那一段 ＋
//    `资料/完全复刻/判据/B98_R1_特效3条闭合.md` §一（账行 195）。
//
// ============================ 这条偏差在哪些界面可见 ============================
//  `CardDisplayWindow` 就是**战斗里点单位拉近**的那扇「受到以下影响：」展示窗（**唯一**消费方；
//  `RowsOfEffects` 由 `BattleDriver.cs:13212` 喂，`SetEffectRows` 画）。⇒ 敌方/我方**任何带混沌标记**
//  的单位，在拉近视图里 mark 那一行原来是**右列空白**（左列只有 `[[markOfKhorne]]` 那种专名）。
//  ⛔ 卡牌详情窗（收藏/卡组那两个）**不走这里**（它们画卡面正文，不画 `activeEffects` 清单）。
//
// ============================ 怎么用（宿主）============================
//  `B98A6_Assert.Run();`
//   · **不跑 Unity 批处理**、不建资产、不碰场景；**纯内存**（合成 `ShownEffect` / `UnitState.TempBuff`）。
//   · 任意宿主都能跑（只碰 `Loc` 与两个**静态**函数）⇒ 推荐接 `Editor/BattleScene.cs`（`BattleScene.Run`）。
//   · 本文件**还没接到宿主里**（文件所有权只到 `Battle/CardDisplayWindow.cs` 与本文件）⇒ 由调度台加两行：
//       `B98A6_Assert.Run();`
//       `_sink.Pass += B98A6_Assert.Pass; _sink.Fail += B98A6_Assert.Fail;`
//   · 不依赖当前语档：期望值一律**从数据 / 关系式**算，不写字面量译文。
//
// ============================ 🔴 灭自证（每条都把「改哪一处会红」写进文案）============================
//   · `MarkRow` 退回 `new EffectRow(who, "")`（右列硬写空）⇒ **A1 / A2 / A3 / A4 / A8 红**。
//   · `MarkRow` 退回 `new EffectRow(who, what)`（不用 `TextRow`）⇒ **A6 红**（none/Random 那档 `Who` 变成 " (xN)"）。
//   · `MarkGodEffectKey` 把四神写死成同一档 ⇒ **A4 红**（四种值的 `What` 相等）。
//   · `MarkGodEffectKey` 把 none/Random 也映射成 Khorne ⇒ **A6 红**。
//   · `MarkGodEffectText` 缺键时**回键名**（不落空档）⇒ **A2 红**（`What` 含 `Battle/Effect/`）。
//   · 旧口 `RowsOf` 不改（仍是空串）⇒ **A7 红**（两条路同值）。
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B98_A6`：`CardEffectItem.LoadEffect` 的 mark 支**右列**（账 `特效动画:195`）
/// 在 `CardDisplayWindow.MarkRow` / `RowsOf` 上的断言。
/// <para>⛔ 期望值不从被测实现里反算：四条键是**原版枚举成员名的字面量**（`dump.cs:45915-45925`），
/// 译文一律走 `Loc.T`（数据），其余是**关系式**（「≠ 键名」「四神互不相同」「层数不改右列」）。</para></summary>
public static class B98A6_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B98A6] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void Eq<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }

    // 原版 `MarkOfChaosType` 的**成员名**（`d:/2/tools/il2cpp_out/dump.cs:45915-45925`，现读）。
    // ⚠️ 这里是**字面量**、⛔ 不从 `CardDisplayWindow.MarkGodEffectKey` 反算（那等于自己证自己）。
    static readonly int[] GodValues = { 10, 20, 30, 40 };
    static readonly string[] GodKeys =
    {
        "Battle/Effect/Khorne", "Battle/Effect/Nurgle", "Battle/Effect/Slaanesh", "Battle/Effect/Tzeentch",
    };

    /// <summary>造一条 mark 族的**投影**（原版 `EntityScript.activeEffects` 里的一条 `CardEffect`）。</summary>
    static ShownEffect Mark(int god, int traitValue, string source)
    {
        return new ShownEffect
        {
            Family = ShownFamily.Mark,
            BuffType = EffectsToShow.BuffTypeMarkOfChaos,
            MarkOfChaosType = god,
            TraitValue = traitValue,
            Timed = false,
            Source = source,
        };
    }

    /// <summary>词条原文里 `[[` 之前那段**纯文字**（`RewriteMarks` 只改记号、不动纯文字）⇒
    /// 用它当期望值：既来自数据、又**不**与被测用的那个渲染函数同一条逻辑。</summary>
    static string PlainPrefix(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        int i = raw.IndexOf("[[", System.StringComparison.Ordinal);
        return i > 0 ? raw.Substring(0, i) : raw;
    }

    public static void Run()
    {
        MenuCheck.Section(_sink, "B98_A6：`LoadEffect` mark 支的**右列**（账 特效动画:195 · §一）");

        // ---- 前提（硬检查，⛔ 不写成 `if (前提) Check(...)`）----
        Tr(EffectsToShow.MarkKhorne == 10 && EffectsToShow.MarkNurgle == 20
           && EffectsToShow.MarkSlaanesh == 30 && EffectsToShow.MarkTzeentch == 40
           && EffectsToShow.MarkNone == 0 && EffectsToShow.MarkRandom == 50,
           "P1 前提：`MarkOfChaosType` = none 0 / Khorne 10 / Nurgle 20 / Slaanesh 30 / Tzeentch 40 / Random 50"
           + "（判据 `dump.cs:45915-45925`）｜🧨 枚举值被改 ⇒ 下面几条全部失去意义，本条先红");

        for (int i = 0; i < GodKeys.Length; i++)
        {
            string k = GodKeys[i];
            string raw = Loc.T(k);
            Tr(!string.IsNullOrEmpty(raw) && raw != k,
               $"P2.{i + 1} 前提：官方表里有 `{k}`（`Loc.T` 取得到、且**不是**键名回显）"
               + $"｜🧨 词条丢了 ⇒ 右列会落进「留空 + 出声」那一档（那是设计内的兜底，不是本批要测的东西）");
        }

        // ===================== ① 生产口 `RowsOfEffects`（`BattleDriver.cs:13212` 喂的就是它）=====================
        var kRows = CardDisplayWindow.RowsOfEffects(new[] { Mark(10, 1, "血之黑暗契约") }, true);
        Tr(kRows != null && kRows.Count == 1, "A0 前提：一条 mark 投影 ⇒ 铸出一行");
        if (kRows == null || kRows.Count != 1) return;      // 前提塌了就别往下读下标

        string kWhat = kRows[0].What ?? "";
        string kRaw = Loc.T("Battle/Effect/Khorne");
        string kPrefix = PlainPrefix(kRaw);

        Tr(!string.IsNullOrEmpty(kWhat),
           "A1 ★★ 原版 mark 行的**右列非空**（`.c:98-101` `goto LAB_18060aaa7` ⇒ `.c:148-153` 写 `effectText`）"
           + $"｜🧨 退回 `new EffectRow(who, \"\")` ⇒ 本条红（那正是改动前的样子：右列空白）");

        Tr(kWhat != "Battle/Effect/Khorne" && kWhat.IndexOf("Battle/Effect/", System.StringComparison.Ordinal) < 0,
           $"A2 ★★ 右列**不是键名**（⛔ 不许把键印到屏幕上）；实得「{kWhat}」"
           + "｜🧨 `MarkGodEffectText` 缺键时回键名 ⇒ 本条红");

        Tr(!string.IsNullOrEmpty(kPrefix) && kWhat.IndexOf(kPrefix, System.StringComparison.Ordinal) >= 0,
           $"A3 ★★ 右列**就是 `Battle/Effect/Khorne` 那条词条**：它必须以词条原文的纯文字段「{kPrefix}」开头"
           + $"（`RewriteMarks` 只改 `[[…]]` 记号、不动纯文字）；实得「{kWhat}」"
           + "｜🧨 换成别的键（比如 `CardTraitDescription/*` 那一族）⇒ 开头就对不上 ⇒ 本条红");

        Tr(!string.IsNullOrEmpty(kRows[0].Who),
           "A8 ★★ 左列（专名）与右列（契约正文）**同时有字**（原版两列都写）"
           + "｜🧨 把 `TextRow` 写成「有右列就把左列清空」⇒ 本条红");

        // ===================== ② 四神各不相同（神真的被读了）=====================
        {
            var seen = new Dictionary<string, int>();
            bool anyEmpty = false, anyDup = false;
            for (int i = 0; i < GodValues.Length; i++)
            {
                var r = CardDisplayWindow.RowsOfEffects(new[] { Mark(GodValues[i], 1, "血之黑暗契约") }, true);
                string w = (r != null && r.Count == 1) ? (r[0].What ?? "") : "";
                if (string.IsNullOrEmpty(w)) anyEmpty = true;
                if (seen.ContainsKey(w)) anyDup = true; else seen[w] = GodValues[i];
            }
            Tr(!anyEmpty, "A4a ★★ 四神都有右列（Khorne / Nurgle / Slaanesh / Tzeentch 逐条试）"
                + "｜🧨 `MarkGodEffectKey` 漏一档 ⇒ 那一档落进留空 ⇒ 本条红");
            Tr(!anyDup, "A4b ★★ 灭自证：四神印出来的**互不相同**（神真的被读了，不是恒取同一键）"
                + "｜🧨 `MarkGodEffectKey` 四档写死同一档 ⇒ 相等 ⇒ 本条红");
        }

        // ===================== ③ 层数（`traitValue`）只影响左列 =====================
        {
            var r3 = CardDisplayWindow.RowsOfEffects(new[] { Mark(10, 3, "血之黑暗契约") }, true);
            string w3 = (r3 != null && r3.Count == 1) ? (r3[0].Who ?? "") : "";
            string x3 = (r3 != null && r3.Count == 1) ? (r3[0].What ?? "") : "?";
            Tr(w3 != null && w3.EndsWith("(x3)"),
               $"A5a 左列带层数（`traitValue = 3 > 1` ⇒ 追加 `\" (x3)\"`，`LoadEffect.c:94-99`）；实得「{w3}」");
            Eq(x3, kWhat,
               "A5b ★★ 右列**不随层数变**（契约正文是同一个词条）"
               + "｜🧨 把层数也拼进右列 ⇒ 本条红");
        }

        // ===================== ④ none / Random ⇒ 整行折空（= 原版 `IsTextEmpty` 为真）=====================
        {
            var none = CardDisplayWindow.RowsOfEffects(new[] { Mark(EffectsToShow.MarkNone, 4, "血之黑暗契约") }, true);
            var rnd = CardDisplayWindow.RowsOfEffects(new[] { Mark(EffectsToShow.MarkRandom, 4, "血之黑暗契约") }, true);
            string nw = (none != null && none.Count == 1) ? (none[0].Who ?? "") : "?";
            string nx = (none != null && none.Count == 1) ? (none[0].What ?? "") : "?";
            string rw = (rnd != null && rnd.Count == 1) ? (rnd[0].Who ?? "") : "?";
            Eq(nx, "",
               "A6a ★★ `none(0)` ⇒ 右列空串（`Battle/Effect/none` 官方表里没有 ⇒ 原版 `GetTranslation` 回 null）"
               + "｜🧨 把 none 也映射成某个神的键 ⇒ 本条红");
            Eq(nw, "",
               "A6b ★★★ 灭自证（钉住 `TextRow`）：`none` + `traitValue 4` ⇒ **整行折空**，连左列的 `\" (x4)\"` 也不显示"
               + "（原版 `IsTextEmpty` **只读右列** ⇒ 整格 `SetActive(false)`）"
               + "｜🧨 `MarkRow` 退回 `new EffectRow(who, what)` ⇒ `Who` 变成 `\" (x4)\"` ⇒ 本条红");
            Eq(rw, "", "A6c `Random(50)` 与 `none` 同档 ⇒ 整行折空（原版只认 10/20/30/40 四档）");
        }

        // ===================== ⑤ 旧口 `RowsOf`（无生产调用点，但同一份语义⛔ 不许留两套）=====================
        {
            var oldRows = CardDisplayWindow.RowsOf(new[]
            {
                new UnitState.TempBuff
                {
                    IsKeyword = true,
                    Name = KeywordTable.MarkOfChaos,
                    Value = 1,
                    Src = "战术卡",
                    SourceCard = "血之黑暗契约",
                    TraitRec = new UnitState.TraitEffectRecord
                    {
                        Trait = KeywordTable.MarkOfChaos,
                        BuffType = 8,                       // markOfChaos
                        TraitValue = 1,
                        MarkOfChaosType = 10,               // Khorne
                    },
                },
            });
            Tr(oldRows != null && oldRows.Count == 1, "A7 前提：旧口 `RowsOf` 也铸出一行");
            if (oldRows != null && oldRows.Count == 1)
                Eq(oldRows[0].What ?? "", kWhat,
                   "A7 ★★ 旧口 `RowsOf` 的 mark 右列与生产口**同值**（判据只写一处：两边都走 `MarkGodEffectText`）"
                   + "｜🧨 只改 `MarkRow` 不改旧口 ⇒ 旧口还是空串 ⇒ 本条红");
        }

        Debug.Log(_sink.P + "B98_A6 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }
}
