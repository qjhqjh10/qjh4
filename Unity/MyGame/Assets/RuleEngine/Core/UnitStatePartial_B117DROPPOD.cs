// UnitStatePartial_B117DROPPOD.cs — 执行代理 `B117_WDROPPOD`（2026-11-02 · 第一百一十七会话 · 引擎线）
//   活状态表 `B117-47`：原版 `CardScript__ActivateTraitsOnSummonOrEnchantment.c:70-78` 的 `0xe6`（dropPod）支。
//
// ============================ 它是什么（逐句，判据 = 现读全文 145 行）============================
//  `d:/2/tools/decomp_full/CardScript__ActivateTraitsOnSummonOrEnchantment.c`：
//    `:42-43` 外层闸 = `cardState(+0x228) ∈ {2 在场, 0xf, 3, 0x11}`（⛔ 不是 cardType —— 见 `Enchantment.cs` 同口径注）；
//    `:48-58` 遍历 `CurrentTraits(+0x128)`，逐条 `:67-69 BattleCardUI.DisplayStatusAnim`（表现层，我们无对应层）；
//    `:70-74` `iVar1 = *(int *)(lVar6 + 0x18);`（这一条的 trait id）→ `if (iVar1 == 0xe6) cVar7 = param_2;`
//             ⇒ **只有「这一条 trait 是 dropPod」且「onSummon 为真」时才走新支**；`onSummon == false`（附魔那条路）
//             留着 `cVar7 = 0`，落到 `:75` 的旧支 ⇒ 对 dropPod 什么都不做（旧支四个 id 不含 0xe6）。
//    `:75-101` 旧支（`fast 0x28`/`flank 0x1cc` ⇒ `canAct = true`；`ferocity 0x4f1`/`oath 0x4fb` ⇒ `canAct = true` +
//             `canAttack = false`）—— 与我们 `RuleCore.ActivateTraitsOnSummonOrEnchantment` 已有那两句同形。
//    `:102-135` **新支（本文件补的就是它）**：
//       `:106` 读 `GameStaticData.dropPodMaxHealth`（`+0x1fc`，值 **2**）
//       `:116-120` 写 `+0xcc/+0xd0/+0xd4/+0xd8/+0xdc`（= `EntityScript.currentDropPodHealth`，
//                 `EntityScript__get_currentDropPodHealth.c:8-12` 读的正是 `+0xcc`）
//       `:123` 读 `GameStaticData.dropPodAttack`（`+0x1f8`，值 **0**）
//       `:129-133` 写 `+0xe0..+0xf0`（= `currentDropPodAttack`）
//       `:134` `CardScript__UpdateFigures(param_1, 0)`（表现层刷数值，我们无对应层）
//     常量出处 = `cpp2il/out_cpp2il_dev_ilspy/GameStaticData.cs:2404-2405`（`dropPodAttack = 0; dropPodMaxHealth = 2;`，
//     字段偏移 `+0x1F8/+0x1FC` 见 `dump.cs:119543-119544`）。
//
//  🔴 **它【不】做的事**（别顺手补）：不摘 `dropPod`（舱体还在身上）、不动 `canAct/canAttack`、不发 `Landing`。
//     开舱仍然只走 `RuleCore.OpenDropPod`（`BeginTurn` / `DeclareAttack` 两个调用点）。
//
// ============================ 它和 `SetupDropPodStats` 的关系（⛔ 别合并）============================
//  `UnitState.SetupDropPodStats()` = 原版 **`CardScript__CardSetup.c:157-200`** 那一格，闸是
//  **`cardType == 10`（英雄/督军）+ `HasCurrentTrait(0xe6)`**（现读 `CardScript__CardSetup.c:158-160`：
//  `iVar9 = EntityScript__get_cardType(...); if (iVar9 == 10) { cVar8 = HasCurrentTrait(0xe6); …}`）
//  ⇒ **只有督军**在「建卡」那一刻拿到初值；**普通单位（cardType != 10）在构造时池子是 0/0**，
//  它们的池子**只能靠本文件这条召唤支**初始化。两条判据（一条带 cardType 闸、一条不带）**本来就不同**，
//  ⇒ 铁律：两处口径不许「统一」，⛔ 别把本方法改成转调 `SetupDropPodStats`（那会给非督军加一道原版没有的闸）。
//  另注：`ResetToValuesInHand.c:110-133` 还有一处**无条件**写这两个格（我们 `UnitState.cs:2675` 已对齐）——
//  那是第三条写点，与本支互不替代（原版三处都在）。
//
// ============================ 🔴 可达性 / 休眠 ============================
//  池内 **0 张**卡带 `dropPod`（`cards_engine.json` 1127 张现扫，见 `KeywordTable.DropPod` 的 doc 与
//  `CardDef.cs:7265-7267`）⇒ **本支今天零可观测**。按铁律 11（原版有、我们缺 ⇒ 全部要补、只分先后）
//  补齐，不是在修一个今天会犯的错。⛔ 别因为「0 张」把它当成可以删的死支。
//
// ============================ 🔴 接线状态：⛔ 本文件不自己接线 ============================
//  调用点必须加在 `RuleEngine/Core/RuleCore.cs`（两处），而该文件本批由 `WREPLAY2` 占用 ⇒
//  **补丁交回调度台**（全文见 `资料/完全复刻/判据/B117-WDROPPOD_dropPod支.md` §六）：
//    ① `ActivateTraitsOnSummonOrEnchantment(UnitState u)` 末尾追加 `if (onSummon) u.ApplyDropPodOnSummon();`
//       （同时把签名改成 `(UnitState u, bool onSummon = true)`，`Enchantment.cs:1016` 传 `onSummon: false`）；
//    ② `ApplyDeployTurnState(UnitState u, bool keepStatus = false)` 的 `RecomputeActAndAttack(u);` 之后追加
//       `u.ApplyDropPodOnSummon();`（= 原版 `_MinionPlayedIntoField_d__314__MoveNext` → 那次 `onSummon:1` 的调用）。
//  ⚠️ **没接线 = 本文件永远不被生产代码调用** ⇒ `RuleEngine/Editor/RuleEngineTest_B117DROPPOD.cs` 的
//     **W 段会一直红** —— 那一段就是专门盯「接线到底有没有落」的（红 = 补丁没应用，不是本方法写错）。

namespace RuleEngine
{
    public partial class UnitState
    {
        /// <summary>
        /// **原版 `ActivateTraitsOnSummonOrEnchantment` 的 `dropPod(0xe6)` 支**（`CardScript__ActivateTraitsOnSummonOrEnchantment.c:70-78`
        /// 的判定 + `:102-135` 的体）—— **单位被召唤那一刻**把空投舱的独立血池/攻击力刷成
        /// `GameStaticData` 的全局默认（`dropPodMaxHealth = 2` / `dropPodAttack = 0`）。
        ///
        /// <para>判据逐句、它与 `SetupDropPodStats` 的区别、以及为什么「0 张卡带它」也要补 → 本文件头。</para>
        ///
        /// <para>🔴 **只在 `onSummon == true` 的入口调**（部署 / 起始兵 / 督军开局 / 变身后返场）；
        /// **附魔那条路（原版 `onSummon: false`）不许调** —— 原版 `.c:72-74` 是 `if (id == 0xe6) cVar7 = param_2;`，
        /// `param_2 == 0` 时**什么都不做**。</para>
        ///
        /// <para>⚠️ 原版外层还有一道 `cardState(+0x228) ∈ {2, 0xf, 3, 0x11}` 的闸；我们的调用点全部落在
        /// 「已经写进棋盘」的那一刻（`ApplyDeployTurnState` / 起始兵摆位 / 督军 `Board[WarlordSlot] = …`）
        /// ⇒ 该闸恒真，**没有对象可复刻**，⛔ 别为此新造一个 cardState 形参。</para>
        /// </summary>
        /// <returns>`true` = 命中 dropPod 支（池子已刷成默认值）；`false` = 这个单位不带 `dropPod`（什么都没做）。</returns>
        public bool ApplyDropPodOnSummon()
        {
            // `EntityScript.HasCurrentTrait(0xe6)` 的投影（原版 `.c:70` 遍历的是 `CurrentTraits(+0x128)`）。
            // ⚠️ 用 `HasCurrentTrait` 而不是 `Has`：两词的差别（残骸档 / jam 摘除 / `cardTraits` 列）见
            //    `HasCurrentTrait` 的 doc，与 `B117-24` 那一族同口径。
            // 🔴 **顺手发现（本代理只报不改）**：既有的 `UnitState.SetupDropPodStats()`（`UnitState.cs:3907`）
            //    用的是 `Has(...)`，而原版 `CardSetup.c:159` 调的是 `EntityScript__HasCurrentTrait` ——
            //    那是**同一条判据的第二个写法**，只差 `cardTraits` 列/残骸档/jam 三处（今天 0 张卡带 `dropPod`
            //    ⇒ 零可观测）。已写进报告 §五，由调度台派 `UnitState.cs` 的写手改一行（⛔ 本代理不动那个文件）。
            if (!HasCurrentTrait(KeywordTable.DropPod)) return false;
            CurrentDropPodHealth = DropPodMaxHealthDefault;   // 原版 `.c:106 → :116-120`（`GameStaticData.dropPodMaxHealth = 2`）
            CurrentDropPodAttack = DropPodAttackDefault;      // 原版 `.c:123 → :129-133`（`GameStaticData.dropPodAttack = 0`）
            return true;
        }
    }
}
