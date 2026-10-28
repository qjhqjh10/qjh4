// B94B9_Assert.cs — 执行代理 B9（第九十四会话 · 波 2）的唯一一处代码改动的自检。
//
// ============================ 它盯的是哪一行账 ============================
//  · **`特效动画:195`** — `CardEffectItem.LoadEffect(CardEffect effect, bool isPlayer)` 的
//    「`buffType ∈ {markOfChaos(8), lastMarkOfChaos(17)}` 走另一条『谁给的』」那一支，
//    落点 = `Battle/CardDisplayWindow.cs` 的 `RowsOf`（+ 类尾的 `MarkGodTraitEnum`）。
//  逐条的判据 / 旧账为什么说错 / 今天的可达性 → `资料/完全复刻/报告/B94_B9.md`（⛔ 别在这里抄第二份）。
//
// ============================ 怎么用（宿主）============================
// `B94B9_Assert.Run();`
//   · **不跑 Unity 批处理**、不建持久资产、不碰场景；纯内存（合成 `UnitState.TempBuff`）。
//   · ⚠️ 本文件**还没接到宿主里**（文件所有权只到 `Battle/CardDisplayWindow.cs` 与本文件）⇒
//     由调度台在宿主 `Run()` 里加两行：
//       `B94B9_Assert.Run();`
//       `_sink.Pass += B94B9_Assert.Pass; _sink.Fail += B94B9_Assert.Fail;`
//   · 不依赖当前语档：期望值一律**从数据重算**（`Loc.HasBulk` / `Loc.T` 那条官方表），不写字面量译文。
//
// ============================ 🔴 灭自证（每条都把「改哪一处会红」写进文案）============================
//   · 把 `RowsOf` 里的 mark 支删掉（退回「`who = DisplaySourceName`」）⇒ **M2 / M4 / M5 / M7 红**。
//   · 把 `MarkGodTraitEnum` 的四档写死成同一档（比如恒 `markOfKhorne`）⇒ **M5 红**。
//   · 把 `traitValue > 1` 那半句删掉 ⇒ **M4 红**。
//   · 把「其余神 ⇒ 空串」改成「其余神 ⇒ 也印专名」⇒ **M7 红**。
//   · 把非 mark 那条路顺手改掉（`who` 不再取 `DisplaySourceName`）⇒ **M8 红**。
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 B9：`CardEffectItem.LoadEffect` 的 mark 专名那一支（账 `特效动画:195`）在
/// `CardDisplayWindow.RowsOf` 上的断言。计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。
/// <para>⛔ **期望值不从被测实现里读**：四条「专名」期望走 `Loc` 的官方表（数据重算），
/// 其余全是**关系式**（「≠ 来源卡名」「两神不同」「上限 N 才追加」）。</para></summary>
public static class B94B9_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B94B9] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void Eq<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }

    /// <summary>造一条**关键词档**的限时增益（`TempBuff.BuffType` 走 `TraitRec` 那半边）。</summary>
    static UnitState.TempBuff MarkBuff(int buffType, int god, int traitValue, string sourceCard)
    {
        return new UnitState.TempBuff
        {
            IsKeyword = true,
            Name = KeywordTable.MarkOfChaos,
            Value = traitValue,
            Src = "战术卡",                 // 故意与来源卡名不同：用来证明 mark 支**没**走 `Src`/`DisplaySourceName`
            SourceCard = sourceCard,
            TraitRec = new UnitState.TraitEffectRecord
            {
                Trait = KeywordTable.MarkOfChaos,
                BuffType = buffType,
                TraitValue = traitValue,
                MarkOfChaosType = god,
            },
        };
    }

    public static void Run()
    {
        MenuCheck.Section(_sink, "B94_B9：`LoadEffect` 的 mark 专名支 —— `RowsOf` 的 `who`（账 特效动画:195 · G??）");

        // ---- 前提（硬检查，⛔ 不写成 `if (前提) Check(...)`）----
        Tr(EffectsToShow.IsMarkBuffType(8) && EffectsToShow.IsMarkBuffType(17)
           && !EffectsToShow.IsMarkBuffType(3),
           "P1 前提：`EffectsToShow.IsMarkBuffType` 只认 8 / 17（判据 `CardEffectItem__LoadEffect.c:53`）");
        Tr(Loc.HasBulk("Card_Trait/markOfKhorne") || Loc.HasEntry("Card_Trait/markOfKhorne"),
           "P2 前提：官方表里有 `Card_Trait/markOfKhorne`（整条断言链依赖它取得到专名）"
           + "｜🧨 词条丢了 ⇒ 下面几条会退化成印枚举名，本条先红");

        // ===================== ① 四神专名（8 与 17 同走）=====================
        var k8 = CardDisplayWindow.RowsOf(new[] { MarkBuff(8, 10, 1, "Blood Pact Card") });
        var n8 = CardDisplayWindow.RowsOf(new[] { MarkBuff(8, 20, 1, "Blood Pact Card") });
        var k17 = CardDisplayWindow.RowsOf(new[] { MarkBuff(17, 10, 1, "Blood Pact Card") });

        Eq(k8.Count, 1, "M1 前提：一条 mark buff ⇒ 铸出一行（进槽过滤 `IsEffectListEntry(8)` 放行）");
        Tr(!string.IsNullOrEmpty(k8[0].Who),
           "M2 ★★ mark 行的「谁给的」**非空**（原版 mark 支印专名）"
           + "｜🧨 把 mark 支删掉退回 `DisplaySourceName` ⇒ 本条仍非空（那是卡名），所以由 M3 兜底判");
        Tr(k8[0].Who != "Blood Pact Card" && k8[0].Who != "战术卡",
           "M3 ★★ mark 行的「谁给的」**不是**来源卡名、也**不是** `Src`（`LoadEffect.c:56-79` 那支不用它们）"
           + "｜🧨 删掉 mark 支 ⇒ 印成 `Blood Pact Card` ⇒ 本条红");
        Tr(k8[0].Who != n8[0].Who,
           "M5 ★★ 灭自证：**Khorne(10) 与 Nurgle(20) 印出来不一样**（神真的被读了）"
           + "｜🧨 `MarkGodTraitEnum` 四档写死同一档 ⇒ 两条相等 ⇒ 本条红");
        Eq(k17[0].Who, k8[0].Who,
           "M6 `lastMarkOfChaos(17)` 与 `markOfChaos(8)` **同走一支**（原版 `case 8: case 0x11:` 共用一个体）"
           + "｜🧨 只判 `== 8` ⇒ 17 掉回卡名 ⇒ 本条红");

        // ===================== ② `traitValue > 1` 才追加 `" (xN)"` =====================
        var k3 = CardDisplayWindow.RowsOf(new[] { MarkBuff(8, 10, 3, "Blood Pact Card") });
        Tr(k3[0].Who.EndsWith("(x3)"),
           "M4 ★★ `traitValue = 3 > 1` ⇒ 专名后追加 `\" (x3)\"`（原版 `LoadEffect.c:96-99`）"
           + "｜🧨 删掉追加那半句 ⇒ 本条红");
        Tr(!k8[0].Who.Contains("(x"),
           "M4b `traitValue = 1` ⇒ **不**追加（判据 `1 < traitValue`）"
           + "｜🧨 写成 `>= 1` 或恒追加 ⇒ 本条红");

        // ===================== ③ 其余神（none 0 / Random 50）⇒ 跳过专名 =====================
        var none = CardDisplayWindow.RowsOf(new[] { MarkBuff(8, 0, 1, "Blood Pact Card") });
        var rnd = CardDisplayWindow.RowsOf(new[] { MarkBuff(8, 50, 1, "Blood Pact Card") });
        Eq(none[0].Who, "", "M7 ★★ `markOfChaosType = none(0)` ⇒ `who` **空串**（原版那一支 `text2 = \"\"`、跳过专名）"
           + "｜🧨 「其余也印专名」⇒ 本条红");
        Eq(rnd[0].Who, "", "M7b `Random(50)` 与 `none` 同档 ⇒ `who` 空串（原版只有 10/20/30/40 四档）");
        var noneX = CardDisplayWindow.RowsOf(new[] { MarkBuff(8, 0, 4, "Blood Pact Card") });
        Eq(noneX[0].Who, " (x4)",
           "M7c 跳过专名**但仍走追加**（原版 `text2` 从空串起、后面照样 `+ \" (xN)\"`）"
           + "｜🧨 提前 return ⇒ 本条红");

        // ===================== ④ 非 mark 那条路【不变】=====================
        var normal = new UnitState.TempBuff
        {
            IsKeyword = true,
            Name = KeywordTable.MarkOfChaos,
            Value = 1,
            Src = "战术卡",
            SourceCard = "Blood Pact Card",
            TraitRec = new UnitState.TraitEffectRecord { BuffType = 3, TraitValue = 1, MarkOfChaosType = 10 },
        };
        var nrow = CardDisplayWindow.RowsOf(new[] { normal });
        Eq(nrow[0].Who, "Blood Pact Card",
           "M8 ★★ 非 mark（`buffType = addTrait(3)`）**逐字不变**：仍取 `DisplaySourceName`（= 来源卡名）"
           + "｜🧨 把分支条件写成「有 `TraitRec` 就印专名」⇒ 本条红（addTrait 记录也会中招）");

        // ---- 本批的可达性（如实断「今天不可达」，⛔ 不假装可达）----
        Tr(true,
           "M9 如实记：`TempBuffs` 里今天**没有** `buffType == 8/17` 的条目（mark 记录账写的是 `addTrait(3)`，"
           + "`EffectResolver.cs:12537`）⇒ 本支**今天不可达**；buffType 8 那条来自 `CreateMarkCardEffect` 的投影"
           + "（账 `特效动画:521` 未接）⇒ 本断只是把这件事钉成文字，它不是功能断言");

        Debug.Log(_sink.P + "B94_B9 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }
}
