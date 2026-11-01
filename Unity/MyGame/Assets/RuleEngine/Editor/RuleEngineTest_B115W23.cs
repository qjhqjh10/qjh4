// RuleEngineTest_B115W23.cs — 执行写手 `B115_W23`（2026-11-01 · 第一百一十五会话）
//
// ============================ 这件活是什么 ============================
// `B-254` / `SAN-04` 的 **W1 批**：把「卡级目标」的载体从 `UnitState` 换到 `CardInstance`，
// 让**手牌实例**也能进目标表（原版 `CardScript` 手牌与场上**同一个类**）。
// 判据全文 → `资料/完全复刻/判据/B115_S1_卡级目标模型施工面.md` §1/§2；
// 执行记录 → `资料/完全复刻/报告/B115_W23_卡级目标W1整批.md`。
//
// ============================ 判据（⛔ 不是我挑的）============================
//   · `d:/2/tools/il2cpp_out/dump.cs:19482 / :19484 / :30823 / :35480 / :35549`
//       ⇒ 原版那四张表（`abilityTargetsToSave` / `pooledPossibleTargets` /
//         `previousAbilityTargets` / `CardScript.referencedCards(+0x278)`）**全是 `List<CardScript>`**；
//   · `dump.cs:32595 / :32610 / :32613 / :32574` ⇒ `GetCardsInHandList` / `GetCardsInCemetery` /
//       `GetCardsInDeck` / `GetUnitsInPlayList` 返回的**也是 `List<CardScript>`**（手牌/牌库/墓地/场上同型）；
//   · `dump.cs:35370` ⇒ `CardScript : EntityScript`，手牌也带 `isPlayer(+0x40)`；
//   · `AbilityLogic__AddDrawCard.c:46-49` ⇒ 抽到的牌 `AddRange` 进 `abilityTargetsToSave(+0x80)`；
//   · `AbilityLogic__GetTargets.c:867-883` + `FilterMethods__CheckIfMeetsCriteria.c:172-183`
//       ⇒ `ta = 140/150` 的候选池 = 那张静态表（`150` 还要逐候选过一遍常规筛）；
//   · `AbilityLogic__PlayAbility.c:2128-2160` ⇒ 380 写的是 `abilityData.thisCard(+0x10).referencedCards(+0x278)`
//       —— **那个 thisCard 就是牌自己**，所以从手牌打出的战术卡也要能写这张表。
//
// ============================ 宿主接线 ============================
//   在 `RuleEngineTest.cs` 的 `Run()` 里（**同一物理行、且在行尾 `//` 之前**，见 `CLAUDE.md`
//   那条「行末 `//` 会吞掉代码」）：
//       Guard("CheckB115W23CardRefTargets", CheckB115W23CardRefTargets);
//   ⚠️ 方法名**不以 `Test` / `Report` 打头** —— `VerifyRunCaseCoverage` / `gen_diag_runner` 按那两个前缀认用例。
//
// ============================ 🔴 灭自证 ============================
//   ①（**正向**：`ta=150` 真的绕开文本 spec、把**手牌实例**收进 `refsOut`）
//   与 ②（**反向**：同一份 spec、把表清空 ⇒ 一个都不产）
//   这两条**结构上不可能同时**被下面两种错实现满足：
//     · 不建 `ta=150` 支（落回文本 spec 扫棋盘）⇒ ①-a 实得 ≥2（督军 + 摆上去那张）⇒ **①红**，②也红；
//     · 「表空就回落扫棋盘」的兜底 ⇒ ②实得 ≥2 ⇒ **②红**。
//   再加 ①-b/①-c（`refsOut` 里必须是**那一份实例**）⇒ 「恒返回空表」也过不了 ①。
//   ③ 断「`UnitState.ReferencedCards` 真的转发到 `CardInstance`」—— 两处各写一份字段的实现过不了。
//   ④ 断「380 写表**不需要棋盘单位**」（`actingInst` 单独给得出载体）—— 老签名（只认 `acting`）过不了。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>`B115_W23` 的唯一入口（`Run()` 里一行 `Guard(...)`）。</summary>
    public static void CheckB115W23CardRefTargets()
    {
        Section("B115·W23：卡级目标模型（`CardRef` 载体 · `ta=150` 手牌候选 · 引用表转发 · 380 写手牌卡）");

        var ctx = Battle(new CardDef[0], new CardDef[0]);
        int me = ctx.Active;

        // 夹具：棋盘上先摆一张（下面 ②「不回落扫棋盘」靠它才有鉴别力）+ 手里两张**不同实例号**的卡
        var board = Place(ctx, me, 3, Unit("B115W23_board", 1, 0, 5), exhausted: true);
        CheckTrue(board != null, "（前提）棋盘上摆好了一张部队（② 的分母靠它）");

        var c1 = ctx.NewInstance(Unit("B115W23_inhand", 1, 0, 5));
        var c2 = ctx.NewInstance(Unit("B115W23_drawn", 1, 0, 5));
        CheckTrue(c1 != null && c2 != null && c1.Id != c2.Id, "（前提）两张手牌实例拿到**不同的实例号**");
        ctx.Players[me].Hand.Clear();
        ctx.Players[me].Hand.Add(c1);
        ctx.Players[me].Hand.Add(c2);

        // ⚠️ 这条 spec 是**故意会命中棋盘**的（`own / unit / Count=0`）——
        //    `ta=150` 一旦没接管，它就会把己方全体扫出来 ⇒ ①-a 立刻红。
        var spec = new EffectTargetSpec
        {
                   Raw = "(B115W23 夹具：己方全体 —— 只用来验 `ta=150` 会绕开它)", Side = "own", Kind = "unit", Count = 0,
        };
        var op150 = new EffectOp { Verb = "give", Source = "B115W23 ta=150", SubjectTargetsAffected = 150 };

        // ---- ① 正向：手牌候选真的进得了目标表 ----
        ctx.PreviousAbilityTargets.Clear();
        ctx.PreviousAbilityTargets.Add(CardRef.Of(c2));
        var refs = new List<CardRef>();
        var got = RuleCore.ResolveTargets(ctx, me, spec, null, null, null, op150, null, refs);
        Check(got.Count, 0,
                     "★ ①-a `ta=150` 解出的**棋盘**目标 = 0（候选在手里）"
                   + "｜🧨 不建 `ta=150` 支 ⇒ 落回文本 spec（`own/unit`）⇒ 实得督军 + 摆上去那张 ⇒ 红");
        Check(refs.Count, 1,
                     "★★ ①-b `ta=150` 把**手牌候选**收进 `refsOut`（原版 `CardScript` 手牌/场上同型，`dump.cs:35370`；"
                   + "候选池 = 静态表，判据 `AbilityLogic__GetTargets.c:867-883`）"
                   + "｜🧨 只在棋盘上找 / 不建这一支 ⇒ 实得 0 ⇒ 红");
        CheckTrue(refs.Count == 1 && object.ReferenceEquals(refs[0].Inst, c2),
                  "★★ ①-c 收的正是**那一份实例**（按引用认，不按卡名）"
         + "｜🧨 按名字/卡 id 匹配 ⇒ 同模板的另一份 `c1` 也可能进来 ⇒ 红");

        // ---- ② 反向：表空 ⇒ 一个都不产（⛔ 不许回落扫棋盘）----
        ctx.PreviousAbilityTargets.Clear();
        var refs2 = new List<CardRef>();
        var got2 = RuleCore.ResolveTargets(ctx, me, spec, null, null, null, op150, null, refs2);
        Check(got2.Count, 0,
                     "★★ ② 表为空时 `ta=150` **什么都不产**（原版这一支的池子就是那张表本身，`GetTargets.c:867-883`）"
                   + "｜🧨 写「表空就回落文本 spec」的兜底 ⇒ 实得 ≥2 ⇒ 红。⚠️ 与 ①-a/①-b 是**结构上不可能同时满足**的一对");
        Check(refs2.Count, 0, "★ ②-b 同上，`refsOut` 也是空的");

        // ---- ③ 引用表：`UnitState` 只转发，真身长在 `CardInstance` 上 ----
        board.ReferencedCards.Clear();
        board.ReferencedCards.Add(CardRef.Of(c2));
        Check(board.Instance.ReferencedCards.Count, 1,
                     "★★ ③-a `UnitState.ReferencedCards` 是 `CardInstance.ReferencedCards` 的**转发属性**"
                   + "（原版 `CardScript.referencedCards // +0x278`，`dump.cs:35480`）"
                   + "｜🧨 仍各写一份字段 ⇒ 实例那张恒 0 ⇒ 红");
        CheckTrue(board.Instance.ReferencedCards.Count == 1
                  && object.ReferenceEquals(board.Instance.ReferencedCards[0].Inst, c2),
                  "★★ ③-b 表里装的是**手牌实例**（原版这张表能装手牌 / 牌库 / 墓地的卡，`dump.cs:32595-32613`）"
         + "｜🧨 元素类型退回 `UnitState` ⇒ 手牌实例根本写不进去 ⇒ 红");

        // ---- ④ 380 写表：**没有棋盘单位**也能写（载体 = 牌自己）----
        c1.ReferencedCards.Clear();
        int n = RuleCore.SetReferenceCards(ctx, me, null, spec, null, null, c1);
        CheckTrue(n > 0, "★★ ④-a 传 `acting = null` + `actingInst = c1`（手牌里的那张战术卡）⇒ 引用表**写得进去**"
                       + "（判据 `AbilityLogic__PlayAbility.c:2139`：写的是 `actingCard + 0x278`，那个 actingCard 就是牌自己）"
                       + "｜🧨 仍然要求 `acting`（棋盘单位）⇒ 返回 0、表空 ⇒ 红");
        Check(c1.ReferencedCards.Count, n, "★ ④-b 写进去的条数就是返回值（先清空再填 = **覆盖**语义，原版 `:2135-2143` 换新 List）");
    }
}
