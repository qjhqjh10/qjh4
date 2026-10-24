// BattleVfxAssertE3.cs — 执行代理 E3（2026-10-24）· 战斗线四项的**灭自证**断言
//
// 这一份**只断、不改**：它把「改回去会红」写成可执行的断言，宿主在 `BattleScene.Run` 里调一次
// `BattleVfxAssertE3.Run()` 即可（⛔ 本文件不建场景、不开关窗、不动驱动状态 ⇒ 放哪一步都安全，
// 建议紧跟 `---- W63/D3 ----` 那一节之后，见报告 `E3_VfxMap四项.md`）。
//
// 覆盖的四项（编号 = 简报里的编号）：
//   · **D3** `attack_melee` / `hit` 两桶的行**搬**进 `unit_attack_anim` / `unit_damaged_anim`
//     —— 判据 → `资料/普查产出_1023e/A21_VfxMap白名单.md` §二（全池自检 + 五张卡的逐件读数）。
//   · **D4** `trigger` 桶按拍分件（`Start(0)` 不许在触发那一拍播）
//     —— 判据 → `C4_动画特效审查_索引.md` §四 `D4` / P7（全池自检 + 四张夹具卡）。
//   · **UM37 / UM84** 一行 `CardAnim` 挂两个 trigger（分类键要带上那一条 occurrence）
//     —— 判据 → 同报告 §四·8（含**时刻**：`Order` 钉错会以 `T` 差报出来）。
//   · **D10(C4)** `EC57 Xarahan` 的登场语音照原版播（原版自己抄错成了 UM 的台词）。
//
// ⚠️ 期望值**一律不硬编码行数**：能"按数据现算"的一律现算（`SelfCheckUnitSkillAnims` /
//    `SelfCheckTriggerBeatSplit`），其余写的是**原版数据里的具体名字/时刻**（`cards_engine.json` 的
//    `animTimes[]`，可逐条复查）。⛔ 别把它们改成"我们自己的常量"。
// ⚠️ 行为级（谁攻击时播、几拍、锚点）**不在本文件**：那几条在 `BattleScene.cs` 的
//    `W63/D3` 一节（B1 落地）—— 本文件补的是 VfxMap 侧**全池逐卡**那一层，两者不重复。
using System.Collections.Generic;
using CardPresentation;      // VfxMap / VoiceLines
using RuleEngine;            // CardDatabase / CardDef
using UnityEngine;

/// <summary>执行代理 `E3` 的断言集合。`Run()` 幂等、无副作用（除了 `UnpinnedPicks` 计数，
/// 见 <see cref="Run"/> 里那句 `ResetUnpinnedForTest`）。结果同时写进 <see cref="Passed"/> / <see cref="Failed"/>，
/// 宿主如果要并进自己的总数，断 `BattleVfxAssertE3.Failed == 0` 即可。</summary>
public static class BattleVfxAssertE3
{
    public static int Passed { get; private set; }
    public static int Failed { get; private set; }

    public static void Run()
    {
        Passed = 0; Failed = 0;
        Debug.Log("BT E3 --- 执行代理 E3（2026-10-24）：D3 / D4 / UM37·UM84 / D10(C4) 的灭自证断言 ---");

        var pool = CardDatabase.Load();
        Check(pool != null && pool.Count > 0,
              "(前提) 卡池载得到（`CardDatabase.Load()`）—— 载不到的话下面每一条都**没验到**，别读成通过");
        if (pool == null || pool.Count == 0) { Summary(); return; }

        // 下面的全池自检都走**生产解释路径**（`ResolveAllTimed`）⇒ 会给这个计数加数。
        //   ⛔ 这不是清零生产数据，只是自检计数器（见 `ResetUnpinnedForTest` 的 doc）。
        VfxMap.ResetUnpinnedForTest();

        D3(pool);
        D4(pool);
        MultiTriggerOneAnim(pool);
        D10C4();

        // 简报点名的「别被我这次改动破坏」那一批：白名单（D1/D6/D7/D11/D12…）的名字级 + 件级两条常驻闸。
        var namesOk = VfxMap.SelfCheckRoutedTriggerNames();
        Check(namesOk.Length == 0,
              "★ 回归闸（白名单·名字级）：`Mob`/`Teleport`/`Codex`/`Uprising`/`Artifice`/`Cruelty`/`Stimulation`/"
            + "`Ecstasy`/`UnitReceivesHuntMark`/`OnCollectFaith`/`OnCollectSpiritStone`/`ThisUnitSummoned`/"
            + "`WhileInPlay`/`Regiment`/`TriggeredSynapse` 都在白名单（且小写 / 全大写两态都命中），"
            + "`NoEmitTriggerNames` 那 8 个都不在"
            + (namesOk.Length == 0 ? "" : "\n  ✗ " + string.Join("\n  ✗ ", namesOk))
            + " —— 🧨 把 `switch (triggerName.ToLowerInvariant())` 改回大小写敏感、删掉任一正向 case、"
            + "或把反向闸里那 8 个名字塞进白名单 ⇒ 本条红");
        var rowsOk = VfxMap.SelfCheckRoutedTriggers();
        Check(rowsOk.Length == 0,
              "★ 回归闸（白名单·件级）：15 个 trigger 的**逐卡归口不多不少**（含 `UM37`/`UM84` 那两行歧义行）"
            + (rowsOk.Length == 0 ? "" : "\n  ✗ " + string.Join("\n  ✗ ", rowsOk))
            + " —— 🧨 把 `TryResolveTriggerPicks` 的 ③ 改回「只查 `trigger` 桶」、或把 `AddTriggerNamesOf` "
            + "改回「只取第一条同名行」⇒ 逐条报「漏播」⇒ 红");

        // 🆕 2026-10-24（`W10` · 另一代理）：徽标筛选 + 主动能力图标来源（它自己的断言、自己的计数）。
        RunW10BadgeFilter();

        Summary();
    }

    /// <summary>🆕 **2026-10-24（`W10` · 另一代理）**：把 `BadgeFilterAssert`（场上徽标筛选 + 主动能力图标来源）
    /// 挂在本宿主上跑一次，并把它那两条计数并进本文件的总数。
    ///
    /// **为什么挂在这里**：`BattleScene.Run` 只认 `BattleVfxAssertE3.Run()` 这一个调用点
    /// （`BattleScene.cs:12171`），而 `BattleScene.cs` 本批**不归 `W10`** ⇒ 借这个已有宿主，
    /// 省掉「再改一次宿主」。⚠️ 它跑的东西**与本文件那四项无关**（各断各的、各自的 `✗` 前缀是 `BT W10`）。
    /// 想拆开：在 `BattleScene` 里单独加一句 `BadgeFilterAssert.Run(); pass += …Failed;` 即可，
    /// 然后把这里两行删掉（⛔ 别两边都调 —— 会跑两遍、计数翻倍）。</summary>
    static void RunW10BadgeFilter()
    {
        BadgeFilterAssert.Run();
        Passed += BadgeFilterAssert.Passed;
        Failed += BadgeFilterAssert.Failed;
    }

    // ==================================================================
    //  D3 · `attack_melee` / `hit` 两桶的行【搬】进两个技能动画桶
    // ==================================================================
    //  判据（第一权威）：
    //   · `BattleManager._ResolveAttack_d__438__MoveNext.c:987-990` → `BroadcastUnitAttacked`，
    //     **近战与远程两条路都会走到**（改前我们只在近战查 `attack_melee` ⇒ 远程永不播）；
    //   · `CardScript__ResolveUnitAttacked.c:28-30` → `OnTrigger(0x32 = 50)`；
    //   · `CardAbility__CanTriggerAbility.c:126-140` → 动画是 ability 的一步（不是「攻方无条件播」）。
    //  ⇒ 那 13 行（`UnitAttack` 12 + `UnitDamaged` 1）是**技能动画**，不是「攻击/受击特效」。
    //  ⚠️ 「谁攻击时播 / 播几拍 / 用什么载体」是行为级 = `BattleScene.cs` 那一节；
    //     这里断的是 **VfxMap 侧**：桶有没有搬对、旧桶还在不在、按生产路径取回来的对不对。
    static void D3(List<CardDef> pool)
    {
        Debug.Log("BT E3 --- D3：`attack_melee`/`hit` → `unit_attack_anim`/`unit_damaged_anim` ---");

        var all = VfxMap.SelfCheckUnitSkillAnims();
        Check(all.Length == 0,
              "★ D3（全池·桶归口）：`UnitAttack`（现读 " + VfxMap.UnitAnimCardIds(VfxMap.UnitAttack).Length
            + " 张 / " + VfxMap.CardTableUnitAttackEntries + " 行）+ `UnitDamaged`（现读 "
            + VfxMap.UnitAnimCardIds(VfxMap.UnitDamaged).Length + " 张 / " + VfxMap.CardTableUnitDamagedEntries
            + " 行）**逐卡不多不少**，且旧桶 `attack_melee`/`hit` 一件都不留"
            + (all.Length == 0 ? "" : "\n  ✗ " + string.Join("\n  ✗ ", all))
            + " —— 🧨 删掉 `LoadTableFromJson` 里 `m[UnitAttack] = picks;` 那一跳 ⇒「漏搬」；"
            + "把 `movedToUnitAnim` 改成「也写回旧桶」⇒「旧桶还留着」⇒ 每种都红");

        // 逐件读数（原版 `cards_engine.json` 的 `animTimes[]`，可逐条复查）——
        //   这五张卡各代表一种形状，断了能一眼分出是哪一档坏了。
        var sw26 = Card(pool, "SW26");          // attacked 族：单行
        var sw26Atk = Pieces(VfxMap.UnitAttack, sw26);
        var sw26Old = Pieces(VfxMap.AttackMelee, sw26);
        Check(sw26 != null && sw26Atk.Length == 1 && sw26Atk[0].Name == "Hammer_Slam_SW" && sw26Old.Length == 0,
              "★ D3·`SW26 Arjac Rockfist`：`unit_attack_anim` = **1 件** `Hammer_Slam_SW`，"
            + "`attack_melee` = **0 件**（实得 " + sw26Atk.Length + " / " + sw26Old.Length + "）"
            + "｜🧨 把这一行「抄」回 `attack_melee`（两桶都有）⇒ 旧桶变 1 ⇒ 红；删掉搬的那一跳 ⇒ 新桶变 0 ⇒ 红");

        var da22 = Card(pool, "DA22");          // 监听型：两行（Trait_GainQuest 0.0 / Healing_Projectile 0.7）
        var da22Atk = Pieces(VfxMap.UnitAttack, da22);
        Check(da22 != null && da22Atk.Length == 2
              && da22Atk[0].Name == "Trait_GainQuest" && da22Atk[1].Name == "Healing_Projectile"
              && Mathf.Abs(da22Atk[0].T) < 1e-3f && Mathf.Abs(da22Atk[1].T - 0.7f) < 1e-3f,
              "★ D3·`DA22 Techmarine`：两件的**顺序与起点**照原版 `animTimes`（`Trait_GainQuest` T=0.0 → "
            + "`Healing_Projectile` T=0.7；实得 " + Desc(da22Atk) + "）"
            + "｜🧨 把桶里的候选顺序颠倒、或把 `T` 压成恒 0（单候选口径被套到两件上）⇒ 本条红");

        var um70 = Card(pool, "UM70");          // 判据取远程（ranged 3→5）：改前「远程永不播」
        var um70Atk = Pieces(VfxMap.UnitAttack, um70);
        Check(um70 != null && um70Atk.Length == 1 && um70Atk[0].Name == "BulletImpact_2shot_autocannon",
              "★ D3·`UM70 Invictor Warsuit`（**判据取远程**的卡）：`unit_attack_anim` 里**有**它那件"
            + "（实得 " + Desc(um70Atk) + "）—— 改前只在 `EvtKind.Attack && !e.Ranged` 查桶 ⇒ 这张 100% 永不播"
            + "｜🧨 把桶名改回 `attack_melee`（那一路只查近战）⇒ 本条红");

        var sau31 = Card(pool, "SAU31");        // 两行 animTimes，其中 `Wait_1s` 无 prefab（纯计时拍）
        var sau31Atk = Pieces(VfxMap.UnitAttack, sau31);
        Check(sau31 != null && sau31Atk.Length == 1 && sau31Atk[0].Name == "NecronGauss_Large",
              "★ D3·`SAU31 Doomstalker`：桶里 = **1 件** `NecronGauss_Large`（第二行 `Wait_1s` 是纯计时拍、"
            + "原版 `animAdressable` 是空 GUID ⇒ 不该有件；实得 " + Desc(sau31Atk) + "）"
            + "｜🧨 给 `Wait_1s` 补一件占位 ⇒ 本条红（D5 那笔账的复发闸）");

        var bl20 = Card(pool, "BL20");          // `UnitDamaged` 全池唯一一条
        var bl20Dmg = Pieces(VfxMap.UnitDamaged, bl20);
        var bl20Old = Pieces(VfxMap.Hit, bl20);
        Check(bl20 != null && bl20Dmg.Length == 1 && bl20Dmg[0].Name == "Buff_DiabolicStrength"
              && bl20Old.Length == 0,
              "★ D3·`BL20 Child of Torment`：`unit_damaged_anim` = **1 件** `Buff_DiabolicStrength`，"
            + "`hit` = 0（实得 " + bl20Dmg.Length + " / " + bl20Old.Length + "）"
            + "｜🧨 把 `ev.ev == Hit` 那条搬的支删掉 ⇒ 新桶空、旧桶 1 ⇒ 两条都红");
    }

    // ==================================================================
    //  D4 · `trigger` 桶按拍分件（`Start(0)` 是召唤/附魔那一刻，不在触发那一拍）
    // ==================================================================
    //  判据（第一权威）：
    //   · 触发那一拍 = `CardScript__ActivateTriggerTraitAnim.c` → `BattleCardUI__DisplayTriggerAnim.c:71`
    //     **只处理 `TraitEffect.TriggerType ∈ {3, 6}`**；
    //   · 召唤 / 附魔那一刻 = `CardScript__ActivateTraitsOnSummonOrEnchantment.c` →
    //     `DisplayStatusAnim.c:86-119`（`Start(0)` / `OnPlay(2)`）。
    //  ⚠️ 「混着 Start(0) 的卡有 46 张」是**现读**（会随数据变），所以全池那一条**不写死张数**。
    static void D4(List<CardDef> pool)
    {
        Debug.Log("BT E3 --- D4：`trigger` 桶按拍分件（Start(0) 不在触发那一拍）---");

        // ⚠️ 2026-10-24 更正（铁律 5 · `T04`）：这段原来的说明写「【④ 关键词不在白名单 ⇒ 落回整桶】
        //   ……（`Trigger(3)` / `TriggerOnPlay(6)` + 卡级行）」，读起来像「④ 与 ② 期望相同、卡级行全保留」
        //   —— **实际不成立**（2026-10-24 `W6` 起）：④ 走的是 `VfxMap.TriggerBeatRowsMatching`，
        //   在按拍分件之上**再挡一道** —— 卡级行只保留【没标名】与【标名且正是本拍】的，标着别的
        //   trigger 名的会被挡掉（`Acolyte Heavy` 的 `Buff_GSC` / `Buff_Red` 不许在 `Strike` 那一拍混进来）；
        //   ② 结构上做不到这一层（`times` 为空 ⇒ 标不上名）⇒ **两档的期望本来就不同**。
        //   期望值的独立重算（只用原始判据、⛔ 不调被测实现）= `Core/VfxMap.cs` 的
        //   `SelfCheckTriggerBeatSplit` 里 `expectFour` 那段（2026-10-24 现读 `:3560-3575`）；
        //   行为级落点 = `Editor/BattleScene.cs` 的 `J1·D4` 四条（现读 `:37334-37374`）
        //   ＋ `W6` 那道闸的灭自证（现读 `:37110-37124`）。⛔ 行号会漂，判据是那几段代码本身。
        var all = VfxMap.SelfCheckTriggerBeatSplit();
        Check(all.Length == 0,
              "★ D4（全池·触发分拍）：每张「`trigger` 桶里混着 `Start(0)`/`OnPlay(2)` 行」的卡，在"
            + "【② 没有 `animTimes`】与【④ 关键词不在白名单】**两档**里都只取触发那一拍该播的"
            + "（`Trigger(3)` / `TriggerOnPlay(6)` + 卡级行；⚠️ ④ 还会挡掉标着别的 trigger 名的卡级行，"
            + "② 结构上做不到这一层 —— 见本 Check 上方的「2026-10-24 更正」注）"
            + (all.Length == 0 ? "" : "\n  ✗ " + string.Join("\n  ✗ ", all))
            + " —— 🧨 删掉 `TriggerBeatRows` 的任一处调用、把 `IsNonTriggerBeatTraitRow` 的 "
            + "`tt != 3 && tt != 6` 改恒 false（整桶播）或恒 true（把卡级行也切掉）⇒ 红");

        var am27 = Card(pool, "AM27");          // 没有 animTimes ⇒ 第 ② 支
        var am27Got = Pieces(VfxMap.Trigger, am27);
        Check(am27 != null && am27Got.Length == 1 && am27Got[0].Name == "TalentEffect",
              "★ D4·`AM27 Nork Deddog`（**没有 `animTimes`** ⇒ 第 ② 支）：只播 `TalentEffect`（`Trigger(3)`），"
            + "`ArmourEffect`（`Start(0)`）被挡（实得 " + Names(am27Got) + "）"
            + "｜🧨 删掉第 ② 支那个 `TriggerBeatRows(trig)` ⇒ `ArmourEffect` 跟着播 ⇒ 2 件 ⇒ 红");

        var gof26 = Card(pool, "GOF26");        // 关键词 `unstable` 不在白名单 ⇒ 第 ④ 支
        var gof26Got = Pieces(VfxMap.Trigger, gof26, KeywordTable.Unstable);
        Check(gof26 != null && gof26Got.Length == 1 && gof26Got[0].Name == "ConcussiveEffect",
              "★ D4·`GOF26 Stompa`（关键词 `unstable` 不在白名单 ⇒ 第 ④ 支）：只播 `ConcussiveEffect`，"
            + "`ArmourEffect` / `RallyEffect`（两条 `Start(0)`）被挡（实得 " + Names(gof26Got) + "）"
            + "｜🧨 删掉第 ④ 支那个过滤 ⇒ 3 件 ⇒ 红");

        var da10 = Card(pool, "DA10");          // 卡级行（TriggerTypeOf 回 -1）不许被误伤
        var da10Got = Pieces(VfxMap.Trigger, da10);
        Check(da10 != null && da10Got.Length == 1 && da10Got[0].Name == "Trait_GainQuest",
              "★ D4·`DA10 Black Knight`（**别切过头**）：**卡级**行 `Trait_GainQuest`（不在 trait 绑定表里、"
            + "`TriggerTypeOf` 回 -1）照旧保留，只有 `Start(0)` 的 `FlankEffect` 被挡（实得 "
            + Names(da10Got) + "）"
            + "｜🧨 把过滤写成「白名单外一律丢」⇒ 卡级行也没了 ⇒ 0 件 ⇒ 红");

        var am15 = Card(pool, "AM15");          // 桶里全是 Trigger(3) ⇒ 正例，别切过头
        var am15Got = Pieces(VfxMap.Trigger, am15);
        Check(am15 != null && am15Got.Length == 1 && am15Got[0].Name == "TalentEffect",
              "★ D4·`AM15 Minka Lesk`（正例·**别切过头**）：桶里全是 `Trigger(3)` ⇒ `TalentEffect` 照旧播"
            + "（实得 " + Names(am15Got) + "）｜🧨 把 `TriggerBeatRows` 写成「整桶丢弃」⇒ 0 件 ⇒ 红");
    }

    // ==================================================================
    //  UM37 / UM84（+ SW18 正例）· 一行 `CardAnim` 挂两个 trigger
    // ==================================================================
    //  形状：同一个 `CardAnim` 在**两条不同的 `CardAbility`** 里各出现一次、`TriggerName` 不同
    //  （`cards_engine.json` 的 `animTimes[]` 按 `Order` 区分：`UM37` 0.0.2 `ThisCardPlayed` ↔ 1.0.0 `Codex`；
    //   `UM84` 0.0.1 ↔ 1.0.1；`SW18` 0.0.x `WhileInPlay` ↔ 1.0.0 `ThisCardPlayed`）。
    //  ⇒ 分类键必须**带上那一条 occurrence**（`AddTriggerNamesOf` 返回全部名字），否则第二条
    //    occurrence 的那个 trigger **结构上取不到**。
    //  ⚠️ 这里额外断 **`T`**（= `CardAnimTime.Offset`）：名字对不代表钉到的是**对的那一行** ——
    //    `UM84` 的 `Buff_Purple` 若被钉回 `ThisCardPlayed` 那条（0.0.1，T=1.0）而不是 Codex 那条
    //    （1.0.1，T=0.5），名字照样是 `Buff_Purple`，但**晚 0.5 s 才起播**。这条断言就是为它写的。
    static void MultiTriggerOneAnim(List<CardDef> pool)
    {
        Debug.Log("BT E3 --- UM37/UM84：一行 CardAnim 挂两个 trigger ---");

        var um37 = Card(pool, "UM37");
        var um37Got = Pieces(VfxMap.Trigger, um37, KeywordTable.Codex);
        Check(um37 != null && um37Got.Length == 1 && um37Got[0].Name == "BulletTrail_Stormraven_Single",
              "★ UM37·`Death from Above` 的 `Codex` 那一拍取得到它那条 "
            + "`BulletTrail_Stormraven_SingleTarget`（`Order=1.0.0` / `tn=Codex`；实得 " + Desc(um37Got) + "）"
            + "｜🧨 把 `AddTriggerNamesOf` 改回「只取第一条同名行」（= `ThisCardPlayed`）⇒ 0 件 ⇒ 红");

        var um84 = Card(pool, "UM84");
        var um84Got = Pieces(VfxMap.Trigger, um84, KeywordTable.Codex);
        float tCodex = TOf(um84Got, "CodexEffect"), tBuff = TOf(um84Got, "Buff_Purple");
        Check(um84 != null && um84Got.Length == 2
              && Mathf.Abs(tCodex) < 1e-3f && Mathf.Abs(tBuff - 0.5f) < 1e-3f,
              "★ UM84·`Devastator Doctrine` 的 `Codex` 那一拍 = **两件**，且**时刻钉在 Codex 那两条 occurrence 上**"
            + "（`CodexEffect` Order 1.0.0 T=0.0 · `Buff_Purple` Order 1.0.1 T=0.5；实得 " + Desc(um84Got) + "）"
            + "｜🧨 改回「只取第一条」⇒ `Buff_Purple` 丢掉 ⇒ 1 件 ⇒ 红；"
            + "🧨 把钉行改成「按 `Anim` 名全局首匹配」⇒ `Buff_Purple` 钉到 `ThisCardPlayed` 那条（T=1.0）⇒ 红色（时刻不符）");

        var sw18 = Card(pool, "SW18");
        var sw18Got = Pieces(VfxMap.Trigger, sw18, "WhileInPlay");
        Check(sw18 != null && sw18Got.Length == 2
              && Has(sw18Got, "Buff_Blue_SW") && Has(sw18Got, "Card_Buff_Hand_SW"),
              "★ SW18·`Hrolf the Ironhowl` 的 `WhileInPlay` 那一拍 = **两条都播**"
            + "（`Buff_Blue_SW` + `Card_Buff_Hand_SW`；后者同时挂在 `ThisCardPlayed` 下 = 同一种歧义行，正例·别切过头"
            + "；实得 " + Desc(sw18Got) + "）｜🧨 改回「只取第一条」⇒ 1 件 ⇒ 红");
    }

    // ==================================================================
    //  D10(C4) · `EC57 Xarahan` 的登场语音（原版数据 bug，照原版播）
    // ==================================================================
    //  现读判据：原版卡 SO 的 `summonAudio` GUID `6a6fb8c37ab200c469bcc799c5dbd57b` 解出来是
    //  **`VO_UM_Primaris Intercessor - For the Imperium`**（= `UM6 Primaris Intercessor` 的登场台词）——
    //  **原版自己抄错的引用**。主对话裁定：**照原版播**（复刻口径优先，铁律 11）。
    //  数据落点 = `Resources/voice_lines.json` 里 `EC57` 的 `deploy` 行（`src = cardSO` / `so = summonAudio`），
    //  消费 = `VoiceLines.TryPick(id, ForDeploy, …)` ← `VoiceLinesController__DisplaySummonMessage.c:31`
    //  → `RawCardScript__GetSummonSound`（取不到**整条消息都不发**，原版没有回落）。
    static void D10C4()
    {
        Debug.Log("BT E3 --- D10(C4)：`EC57` 的登场语音 ---");

        Check(VoiceLines.Ready,
              "★ D10（前提）：`voice_lines.json` 读得到（" + (VoiceLines.LoadError ?? "OK") + "）");

        string clip, text, ev;
        bool hit = VoiceLines.TryPick("EC57", VoiceLines.ForDeploy, null, out clip, out text, out ev);
        Check(hit && ev == VoiceLines.ExactDeploy,
              "★ D10：`EC57 Xarahan, Daemon Prince` 有 `deploy` 行（实得 ev=" + (ev ?? "无") + "）"
            + "｜🧨 删掉 `voice_lines.json` 里 EC57 那条 `deploy` ⇒ 登场**静默不播** ⇒ 本条红");
        Check(hit && clip == "VO_UM_Primaris Intercessor - For the Imperium.ogg",
              "★ D10：那条指的是 **UM 的台词** —— `VO_UM_Primaris Intercessor - For the Imperium.ogg`"
            + "（原版卡 SO 就是这么指的；现读 " + (clip ?? "无") + "）"
            + "｜🧨 为了「看起来合理」改成 EC 自己的某个 clip ⇒ 与 `summonAudio` 不符 ⇒ 红");
        Check(VoiceLines.SoRowCount("EC57", VoiceLines.ExactDeploy) >= 1,
              "★ D10：那行是**卡 SO 指名**的（`src=cardSO` / `so=summonAudio`；现读 SO 行数 "
            + VoiceLines.SoRowCount("EC57", VoiceLines.ExactDeploy) + "）"
            + "｜🧨 换成一条「按文件名猜」的 `deploy` 行 ⇒ SO 行数归 0 ⇒ 红");
        Check(hit && VoiceLines.Clip(clip) != null,
              "★ D10：音频资源真的在 `Resources/" + VoiceLines.ClipRoot + "…`（现读 "
            + (hit ? (VoiceLines.Clip(clip) != null ? "在" : "**不在**") : "没命中") + "）"
            + "｜🧨 把 .ogg 挪走/改名 ⇒ 红（这条挡「表里有行、文件没了」那种假绿）");
    }

    // ------------------------------------------------------------------ 小工具
    static void Check(bool ok, string msg)
    {
        if (ok) { Passed++; Debug.Log("BT E3 ✓ " + msg); }
        else { Failed++; Debug.LogError("BT E3 ✗ " + msg); }
    }

    static void Summary()
    {
        string line = $"BT E3 === 断言合计：{Passed} 通过 / {Failed} 失败 ===";
        if (Failed == 0) Debug.Log(line);
        else Debug.LogError(line + "（失败条目见上面每一条 `BT E3 ✗`）");
    }

    static CardDef Card(List<CardDef> pool, string id) { return CardDatabase.FindById(pool, id); }

    /// <summary>**生产路径逐参**（`perCardOnly: true`；`rng = null` = 不抽组 = 按序全播）。
    /// ⚠️ 与 `BattleDriver.PlaySignal` / `ScheduleAttackPresentation` 的取法一致 —— ⛔ 别另写一份。</summary>
    static VfxMap.ResolvedVfx[] Pieces(string ev, CardDef cd, string kw = null)
    {
        if (cd == null) return new VfxMap.ResolvedVfx[0];
        return VfxMap.ResolveAllTimed(ev, cd.Faction, cd.Id, cd.AnimTimes, null, true, kw);
    }

    static string Names(VfxMap.ResolvedVfx[] a)
    {
        var s = new string[a.Length];
        for (int i = 0; i < a.Length; i++) s[i] = a[i].Name;
        return string.Join(" / ", s);
    }

    static string Desc(VfxMap.ResolvedVfx[] a)
    {
        var s = new string[a.Length];
        for (int i = 0; i < a.Length; i++) s[i] = a[i].Name + "(T=" + a[i].T.ToString("F2") + ")";
        return string.Join(" / ", s);
    }

    static bool Has(VfxMap.ResolvedVfx[] a, string name)
    {
        for (int i = 0; i < a.Length; i++) if (a[i].Name == name) return true;
        return false;
    }

    /// <summary>取某一件的 `T`；取不到回 <c>-1</c>（⇒ 断言里的 `Mathf.Abs(t - 期望) < 1e-3` 必然红，
    /// ⛔ 不会因为「没这一件」而静默变绿）。</summary>
    static float TOf(VfxMap.ResolvedVfx[] a, string name)
    {
        for (int i = 0; i < a.Length; i++) if (a[i].Name == name) return a[i].T;
        return -1f;
    }
}
