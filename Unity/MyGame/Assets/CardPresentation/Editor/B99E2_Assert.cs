// B99E2_Assert.cs — 执行代理 `B99_E2`（第九十九会话 · 波 1 · 战斗线）的断言集
//
// ============================ 它盯的是什么（一条）============================
//   账 `战斗逻辑驱动` 的 `CardScript.RemoveEffect(CardEffect removedEffect, bool fromJam)`
//   （判据文档 `判据/B98_J0_战斗剩余终局.md` §852；主表现读 = `账/战斗逻辑驱动.tsv:853`）
//   —— **`case 5 swapStats` 里 `ChangeMaxHealth` 的内层夹血那一格**：
//     原版 `CardScript__ChangeMaxHealth.c` 体末六句在写完 `currentMaxHealth(+0xa4) = 卡面血 + Σ(extrinsic 血增量)`
//     之后**立刻** `currentHealth(+0x68) = min(currentHealth, 新 +0xa4)` ⇒ **夹到 base**；
//     `RemoveEffect` 的 `case 5`（`.c:222-238`）外层再夹一次 **processed**。
//     ⇒ 净效果 = `min(Health, base, processed)`。`B98_K6` 落了外层（`ClampHealthToMax`），
//       **内层那一句漏了**（生命落在 base 与 processed 之间时会多留血）—— 本文件钉它。
//
//   判据（⛔ 别在这里抄第二份）→ `资料/完全复刻/判据/B98_J0_战斗剩余终局.md` §852
//                            ＋ `资料/完全复刻/报告/B98_K6.md` §一（852 ②）
//                            ＋ 本代理 `资料/完全复刻/报告/B99_E2.md`
//
// ============================ 🔴 灭自证（怎么才能红）============================
//   · E1：把 `case 5` 里新加的那句 `if (Health > MaxHealthBase) Health = MaxHealthBase;` 删掉
//         ⇒ 只剩外层夹 processed（9）⇒ 生命停在 **8** ⇒ E1 红。
//   · E2：对照组**不调** `RemoveEffectRecord` 的同一套夹具 ⇒ 仍是 8。
//         E1（7）与 E2（8）**结构上不可能同时满足** ⇒ 「夹血恒 7 / 恒 8」都会被抓。
//   · P1–P4 是**前提断言**（先把「摆在中段」这件事本身钉住），⛔ 不是 `if (前提) Check(结论)`。
//
// ============================ 怎么用（宿主）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的 `Run()` 那一行里插一行：`Step(TestB99E2);`
//   （与 `Step(TestB98K6);` 同一行追加即可 —— 那一行本来就靠「同一行追加」保持行数不变。）
//   ⚠️ **本批白名单不含宿主** ⇒ 这一行要**调度台**加；加之前本文件一条都不跑（如实标）。
//   ⚠️ 本文件**不建 GameObject、不建资产、不写盘**；只用 `new CardDef` / `BattleContext` 的内存局面。
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>B99_E2 的全部断言（宿主：`RuleEngineTest.Run` 里 `Step(TestB99E2);`）。</summary>
    public static void TestB99E2()
    {
        Section("B99_E2 —— 账 :853（J0 §852 ②）case 5 swapStats：`ChangeMaxHealth` 的内层夹血（夹 base，不是 processed）");
        TestB99E2SwapBaseClamp();
    }

    // ==============================================================================================
    //  E · `case 5` 的两道夹血：内层夹 base（`ChangeMaxHealth` 自己）+ 外层夹 processed（case 5 尾部）
    // ==============================================================================================
    static void TestB99E2SwapBaseClamp()
    {
        // 卡面：近战 3 · 生命 5
        var card = new CardDef("E2SWAP", "E2SWAP", "unit", "", "common", "Test", 1, 3, 5, 0, null);

        // ---- E2 对照组：**不**摘 swapStats 记录 ⇒ 生命保持 8 ----
        var ctx0 = ProbeBattle(new CardDef[0], new CardDef[0]);
        var u0 = Place(ctx0, 0, 3, card);
        u0.AddStatMod("attack", 2);                                    // 非 extrinsic 近战 +2
        u0.AddStatMod("health", 1);                                    // 非 extrinsic 生命 +1
        u0.AddStatMod("health", 2, null, extrinsic: true);             // extrinsic 生命 +2
        Check(u0.MaxHealth, 8, "(前提 P1) 摘之前上限 = 卡面 5 + 1(非 ext) + 2(ext) = 8");
        u0.Health = 8;                                                 // 满血
        Check(u0.Health, 8, "(前提 P2) 生命摆到 8（= 旧上限，合法局面）");

        // ---- E1 真摘：新的 base = 7、新的 processed = 9 ⇒ 生命必须被夹到 **7** ----
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        var u = Place(ctx, 0, 3, card);
        u.AddStatMod("attack", 2);
        u.AddStatMod("health", 1);
        u.AddStatMod("health", 2, null, extrinsic: true);
        u.Health = 8;
        Check(u.MaxHealth, 8, "(前提 P3) 摘之前上限 8 ⇒ 摆位 = 「新 base 7 < 生命 8 < 新 processed 9」，正好测两道夹血之差");

        u.RemoveEffectRecord(new UnitState.EffectRecord { BuffType = 5 });   // swapStats

        Check(u.MaxHealth, 9,
              "(前提 P4) 摘后 processed = 卡面 5 + 折进 base 的 extrinsic 2 + 仍在投影里的 extrinsic 2 = 9"
            + "（原版 `ChangeMaxHealth` 与 `get_currentMaxHealthProcessed` 就是这样双计，§18·0 照抄）");
        Check(u.Health, 7,
              "★ E1 生命被夹到 **base 7**（原版 `CardScript__ChangeMaxHealth.c` 体末六句：写完 `+0xa4` 后"
            + " `currentHealth = min(currentHealth, 新 +0xa4)`）"
            + "｜🧨 只留外层夹 processed ⇒ 停在 8 ⇒ 红（这正是 `B98_K6` 漏的那一格）");
        Check(u0.Health, 8,
              "★★ E2 灭自证：**没摘**的那张仍是 8 ⇒ E1 的 7 不是「生命恒被写成 7」造成的");
    }
}
