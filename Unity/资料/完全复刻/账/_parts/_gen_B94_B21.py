#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""生成 `账/_parts/B94_B21.tsv`（第九十四会话 · 波 6 · 执行代理 B94_B21）。

只读主表、只写补丁；**不碰主表**。列口径 = 主表 11 列（`\t` 裸切，⛔ 不用 csv 模块）。
运行：`python -I -X utf8 d:/4/Unity/资料/完全复刻/账/_parts/_gen_B94_B21.py`
"""
import io
import os

LEDGER = r"d:/4/Unity/资料/完全复刻/账/战斗逻辑驱动.tsv"
OUT = r"d:/4/Unity/资料/完全复刻/账/_parts/B94_B21.tsv"

# 行号（主表 1-based，含表头）→ 逐列覆盖（只写要改的那几列）。键 = 行号。
# 说明：行号仅用于**定位**，脚本会先断言「该行的 原版类+方法 与预期一致」再改（见 KEY 表）。
UPD = {}


def upd(lineno, expect_class, expect_method_prefix, status=None, landing_add=None,
        diff_add=None, note_add=None, level=None):
    UPD[lineno] = dict(cls=expect_class, m=expect_method_prefix, status=status,
                       landing_add=landing_add, diff_add=diff_add, note_add=note_add,
                       level=level)


NEWF = "RuleEngine/Core/RuleCorePartial_B94B21.cs"
NEWT = "RuleEngine/Editor/RuleEngineTest_EngineB94B21.cs"

D0 = "[B94_B21 2026-10-29] "

# ── 178 / 183：`chosenAbility 400/370` 的 op 分派**已实现**，本轮就地结案（C→B）─────────────
CLOSE_GRANTED = (
    "｜✅ **B94_B21 2026-10-29 就地结案**：残余②「`chosenAbility %d` 的 op 分派」**已实现** —— "
    "`RunGrantedTriggerRow`（`Core/EffectResolver.cs:18486`，判据 `AbilityLogic__PlayAbility.c:2102/2006`）"
    "由 `RunActiveRowOpsOrGrantedTrigger`（`:18472`）分派、调用点 `RuleCore.cs:15493` ⇒ "
    "「逐目标登记一条 Courtesy/Relentless 触发」这条链**通了**。残余①（`CardDef` 侧）已由 `B93_F4` 闭合。"
    "⇒ 本行残余 **0**，状态 C→B 的唯一理由 = **休眠**（本轮现扫 `cards_engine.json` 1129 张："
    "`chosenAbility` 共 **1533 行 / 52 种取值**，其中 `== %d` **0 行**；`abilityTriggers` 共 1196 条 / 57 种，含 %d 的 **0 张**）"
    "—— 休眠 ≠ 不可达，⛔ 不判 D。"
    "⚠️ 本轮**没有**为这条链补断言（只做静态复核；`RunGrantedTriggerRow` 是 `internal`，"
    "Editor 程序集够不着，要验得走公开的 `UseActiveAbility` 入口）。")

upd(178, "BattleManager", "AddTriggerCourtesy(",
    status="B",
    diff_add=CLOSE_GRANTED % (400, 400, 43),
    note_add=D0 + "复核：两残余均闭（CardDef 侧 B93_F4 + EffectResolver 侧本轮现读 `:18473/:18487`）；"
                  "残留只剩「休眠」这一档。⛔ 别再把「缺 `chosenAbility 400` 分派」当待办派活。")

upd(183, "BattleManager", "AddTriggerRelentless(",
    status="B",
    diff_add=CLOSE_GRANTED % (370, 370, 230),
    note_add=D0 + "复核：与 178 同一份入口（`courtesy: ca == 400` 那一个三元）；两残余均闭。")

# ── 642：三跳广播本体本轮落地 ─────────────────────────────────────────────────────────────
upd(642, "BattleManagerSupport", "BroadcastTurnSetup(",
    landing_add=(" · 🆕 " + NEWF + ":71（`BroadcastTurnSetup` = 三跳本体；筛子 `HasTurnSetupBody` 在 `:104`）"),
    diff_add=("｜✅ **B94_B21 2026-10-29 补上三跳本体**（新文件 `" + NEWF + ":71`，200 行）："
              "① 棋盘（原版 `unitsInPlay` = **跨双方单一列表**，先过 `IsInPlay`）；②③ 双手两跳走既有的 "
              "`HandHops`（`EffectResolver.cs:18829`，与 `TurnStarted` / `UnitPoisoned` / `Regiment` 同一份）。"
              "筛子 `HasTurnSetupBody`（`:104`）= 原版「`OnTrigger(155)` 对没这条能力的卡内部空转」的等价物 —— "
              "⛔ 去掉它会给一堆无 155 的卡**凭空发 `EvtKind.Trigger`**、污染 `play all dump` 指纹。"
              "**接线仍差一行**（本轮未落，见报告 §接线）：`RuleCore.cs:1965` 的相位钩子旁追加 "
              "`BroadcastTurnSetup(ctx);` —— 🔴 **必须在回合开始那条动作作用域内调**（`HandHops` 自带 "
              "`BeginAction`，若在作用域外会成为最外层 ⇒ 多跑一次 `RefreshMeleeEqualsHealth` + `WhileInPlay` 发射 ⇒ 改指纹）。"
              "断言 = `" + NEWT + ":39` 的 `B94B21Case1TurnSetupBroadcast`（正例 2 条事件 / 无 155 的卡 0 条，各带 🧨）。"),
    note_add=D0 + "三跳本体已建（休眠实现）；池内 `TurnSetup(155)` **0/1129**（本轮 `cards_engine.json` 现扫）"
                  "＋ 卡面 `turn setup` 正则 **0 张**（`desc`+`descZh`）⇒ 今天零可观测。⛔ 不判 D。")

# ── 876：550 `Support` 那一跳本轮补上 ────────────────────────────────────────────────────
upd(876, "CardScript", "ResolveUnitSummoned(",
    landing_add=" · 🆕 " + NEWF + ":150（`BroadcastSupportOnSummon` = 550 那一跳）",
    diff_add=("｜✅ **B94_B21 2026-10-29 补上 550 `Support`**（残余①）：`BroadcastSupportOnSummon`"
              "（`" + NEWF + ":150`，判据 `CardScript__ResolveUnitSummoned.c:46-60` 逐句）= "
              "「排除刚落地那张自己」+「只同方」+「接收者确有 550 能力」+ 按 `PlayOrder`（进场序）触发，"
              "`subject`（≈ 原版 `actingCard` 槽）传刚落地那张。**接线两处未落**（⛔ 本轮不动 `RuleCore.cs`）："
              "`PlayCardCore`（`RuleCore.cs:4156` 那行 `FireTriggerOnSide(…Uprising…)` **之前**）与 "
              "`DeployFreeCore`（`:4968` 同形那一行之前）—— 顺序判据 = `.c` 里 550 在 680 **之前**。"
              "断言 = `" + NEWT + ":90` 的 `B94B21Case2SupportOnSummon`（4 条：命中 1 / 排自己 / 不跨方 / 无 550 不发）。"),
    note_add=D0 + "550 本体已建（休眠实现）；池内 `Support(550)` **0/1129**（本轮现扫）＋ 卡面 `support` "
                  "只有 1 处命中（`TAU22` 的伴生名 `DS8 Support Turret`，**不是**触发词）⇒ 今天零可观测。⛔ 不判 D。")

# ── 其余 22 条：只做复核 / 记录残余（不改状态）───────────────────────────────────────────────
upd(169, "BattleManager", "AddSummonUnit(",
    note_add=D0 + "复核：`enchantments` 形参与挂载点均已由 `B91-W1b` 落位；残留只剩「入队 vs 同步」= 引擎模型差。"
                  "池内附魔卡 0 张 ⇒ 零可观察。⛔ 别另开第三条挂附魔的路。")

upd(212, "BattleManager", "BroadcastQuestResolved(",
    note_add=D0 + "复核：方法本体在 `RuleEngine/Core/RuleCorePartial_B94B8.cs:65`；"
                  "**上游两件仍在白名单外**：(a) `CardScript__RemoveEffect.c:229-247` 的附魔摘除支（`EffectResolver` 的文件）"
                  "(b) 490 的广播出口。池内 `spellType==120` **0/1129**（本轮现扫）⇒ 休眠。⛔ 不判 D。")

upd(227, "BattleManager", "BroadcastUnitRegiment(",
    note_add=D0 + "复核：与 Support 那个同名方法**共享同一段**（原版 `BroadcastUnitRegiment.c:5` 是纯转调）⇒ 按 §4.1 只能记 B。"
                  "`OtherCardRegiment(646)` 池内 **0/1129**（本轮现扫）⇒ 休眠；⛔ 别与关键词 `regiment`（301 那一侧）混为一谈。")

upd(248, "BattleManager", "CheckForHeroDeckTransform(",
    diff_add=("｜[B94_B21 2026-10-29] 复核：谓词 `BoardAnalysis.DeckMeetsCriteria` 在位；**仍缺** = "
              "① 换牌库写口（`matchData +0xd8/+0xe0` 那一步的等价物）+ ② `BattleDriver` 开局链两个调用点"
              "（原版 `SetupBoardPhase.c:56/66`）+ ③ **关卡数据里那两格**（`rawBattleData+0x18` 的变身督军 / `+0x28` 的牌表）"
              "在本机**根本不存在**（`Ctx.CampaignStage` 恒 null、`tutorial_stages.json` 无该格）⇒ "
              "整条链**判据齐、数据缺**。⛔ 不许自己编一张「变身督军」表（那是服务器/关卡数据，属 §18·0 的服务器那一档才允许自设计，本件不算）。"),
    note_add=D0 + "复核：可达性仍是「要先有关卡数据」——本 build 内恒不可达；休眠如实标，⛔ 不判 D。")

upd(372, "BattleManager", "GetPremulliganScriptedData(",
    note_add=D0 + "复核（顺手发现，⛔ 本代理未改）：本仓现有**两份**同义实现 —— "
                  "`Core/TutorialPremulligan.cs:36`（`TutorialPremulligan.GetPremulliganScriptedData`）与 "
                  "`Core/TutorialScriptPredicates_B94B8.cs:51`（`TutorialScriptPredicatesB94B8.PremulliganScriptedActions`），"
                  "闸完全相同 ⇒ **两处写同一条规则**（铁律 6 的反面），建议调度台把其中一个删掉/转调另一个。"
                  "数据现状不变：6 关 `scriptedTurn` 全 false ⇒ 恒 null。")

upd(422, "BattleManager", "IsPremulliganScriptAvailable(",
    note_add=D0 + "复核：同 372 —— 本仓有**两份**同义谓词（`Core/TutorialPremulligan.cs:51` 与 "
                  "`Core/TutorialScriptPredicates_B94B8.cs:35`）；执行那一半（`BattleDriver.Begin` 的异步桥）仍在白名单外。")

upd(521, "BattleManager", "ResolveJamUnit(",
    note_add=D0 + "复核：`B94_B14` 已补 ② 的清两本账；① **顺序**（`CheckIfDead` 应在剔别人之前）仍未动 —— "
                  "整块对调会让 `:16075–:16160` 区内账落点整体漂移，只能在「无 `.cs` 写手」窗口做。⛔ 本代理未动。")

upd(558, "BattleManager", "ResolveTriggerRequiem(",
    note_add=D0 + "复核：入口（`IsInPlayOrDying` 闸 → 自己 430 → 别人 435 三跳）在位。"
                  "池内 `Requiem=430` / `OtherCardRequiem=435` **0/1129**（本轮现扫）＋ 卡面 `requiem` **0 张** ⇒ 休眠。⛔ 不判 D。")

upd(559, "BattleManager", "ResolveTriggerSacrifice(",
    note_add=D0 + "复核：分三层不变（221 行的裁决仍成立）。**225 `OtherCardSacrifice` 的独立广播仍缺** —— "
                  "它要 `WhenEventKind` 常量（`Core/WhenEvent.cs`，**不在本代理白名单**）+ `BroadcastWhen` 体，"
                  "⇒ 可粘贴交回给该文件的写手。池内 `sacrifice` 卡面 **0 张**（本轮现扫 `desc`+`descZh`）⇒ 休眠。")

upd(658, "BattleManagerSupport", "BroadcastUnitPoisoned(",
    note_add=D0 + "复核：三跳全部在位（棋盘 `BroadcastUnitPoisoned` + 双手 `BroadcastUnitPoisonedHandHops`，`B94_B13` 接线）；"
                  "残留 = `EffectResolver` 通用 `addTrait` 正支那个生产点（不在本代理白名单）。"
                  "池内 `UnitPoisoned=290` **0/1129**、卡面 `poisoned` 只 1 处（`GSC10` 的卡名 `Poisoned Supplies`，"
                  "**不是**触发词）⇒ 休眠。")

upd(661, "BattleManagerSupport", "BroadcastUnitRegiment(",
    note_add=D0 + "复核：手牌两跳已接（`B94_Z2`）；残留只剩「自排除按 `subject` vs 原版按 `caster`」这一格"
                  "＋ 休眠口径。`OtherCardRegiment=646` **0/1129**（本轮现扫）⇒ 休眠。")

upd(676, "BoardAnalysis", "MeetsCriteria(",
    note_add=D0 + "复核：残余仍是两条 —— ③ `RandomChance(8)` 掷法（等全局随机源，且原版是「开局预烧 100 个 + 按下标取」"
                  "的形状，⛔ 别在结算里直接 `ctx.Rng.Next`）；⑥ 卡级 `countCriteria.conditionsFilter`（四处一起动："
                  "`CardDef.BoardGateSpec` / `UsesAlternativeAbility` / `CardDatabase.MapBoardGate` / `gen_cards_engine.py`，"
                  "**全在本代理白名单外**）。池内该值 0 行 ⇒ 落上也零行为变化。")

upd(770, "CardScript", "HasCancelAttackEffect(",
    note_add=D0 + "复核：调用点已切（`W14EC`）；三条残差不变（逐条 ability 判 / `ignoreRandom` 无等价位 / 附魔卡不进卡表）。"
                  "池内 0 张卡挂「带 280 能力」的附魔 ⇒ 零可观察。⛔ 不判 D。")

upd(815, "CardScript", "OnTurnEnd(",
    note_add=D0 + "复核：`Courtesy(43)` / `Resolution(45)` 两跳已夹在 `TurnEnd(40)` 前后（本行已改）。"
                  "池内 `abilityTriggers` 含 43 的 **0/1129**、含 45 的也 0（本轮现扫）⇒ 休眠；"
                  "⛔ 别拿卡面里偶然出现的 `resolution` 词当可达。")

upd(816, "CardScript", "OnTurnSetup(",
    note_add=D0 + "复核：三档到期已闭（`UnitState.cs:1700`）；剩「逐条 `SendRemoveEffect` 入队」那一半（同 722 族口径差）。"
                  "155 池内 **0/1129**（本轮现扫）⇒ 休眠。⚠️ 与本批 `642` 的关系：642 补的是**155 那一跳**，"
                  "本行的「逐条入队」是**另一个轴**（撤 buff 的时机），⛔ 别合并。")

upd(835, "CardScript", "ReactToUnitPoisoned(",
    note_add=D0 + "复核：广播 + 手牌两跳都已在位（见 658）；本行「半落点」的实质 = 监听端完全由 `abilityTriggers==290` 表驱动，"
                  "池内 **0/1129**（本轮现扫）⇒ 休眠。保持 C 的理由 = 这个触发**没有独立实现**（它是 658 那一跳的接收端别名）。")

upd(1025, "EntityScript", "EnemyReturnsAttack(",
    note_add=D0 + "复核：三处调用点已接（含 `BattleDriver.cs:34424` 的预览那条）；残差 ⓑ `dropPod +0xe0 = currentDropPodAttack` "
                  "字段已定位、池内 `dropPod` 0 张 ⇒ 休眠；ⓐ 仍是「我们即时结算 ⇒ 无待结算伤害队列」这一结构差。")

upd(1042, "EntityScript", "HasEnchantment(",
    diff_add=("｜[B94_B21 2026-10-29] 复核：三个闸（`HasWhileInPlayGate` / `HasMobGate` / `HasRageGate`）"
              "全仓 **0 个生产调用点**（本轮 `grep` 现核：只有 `Editor/RuleEngineTest_EngineB94B3.cs:335` 一处断言）⇒ "
              "「调用点未接」这条**成立且仍然成立**。三处的落点都在 `RuleCore` / `EffectResolver`（本代理白名单外）⇒ 可粘贴交回。"),
    note_add=D0 + "复核：谓词侧逐句对得上；缺的只有调用点。池内附魔 0 张 ⇒ 休眠。")

upd(1254, "RawCardScript", "IsCumulativeMutation(",
    note_add=D0 + "复核：谓词已落（`B94_B16`）；**数据列仍缺** —— 本轮现扫 `cards_engine.json`：**没有 `mutationType` 键**、"
                  "`spellType==130` **0/1129**。要接通得改 `工具/gen_cards_engine.py` + `Data/CardDatabase.cs`（白名单外）。")

upd(1360, "TroopsKilledCount", "OnTroopKilled(",
    note_add=D0 + "复核：计数格 + 写点 + 读口都在位；覆盖差两条不变（(a) 非本方回合的击杀不计、(b) 今天无读者）。"
                  "⛔ 别当 D 关掉（生产 0 实例只是今天不可观测）。")

upd(1379, "CardScript", "set_hasActiveAbility(",
    note_add=D0 + "复核：六写点全部有表示（`B94_B2` 结案）；残余 = `buffType 40` 两条（池内 0）＋ 教程写点未接。"
                  "顺手记：`ActiveAbilityAvailable` 现在**读** `ActiveAbilitySuppressed`（jam）—— 那是个真缺陷修复，⛔ 别回退。")


def main():
    b = io.open(LEDGER, "rb").read()
    text = b.decode("utf-8-sig")
    nl = "\r\n" if (b.count(b"\r\n") and b.count(b"\r\n") == b.count(b"\n")) else "\n"
    lines = text.split(nl)
    header = lines[0].split("\t")
    assert len(header) == 11, ("主表列数不是 11", len(header), header)
    idx = {name: i for i, name in enumerate(header)}

    out_rows = []
    problems = []
    for lineno, u in sorted(UPD.items()):
        raw = lines[lineno - 1]
        cols = raw.split("\t")
        if len(cols) != 11:
            problems.append(f"行 {lineno} 列数 = {len(cols)}（期望 11）")
            continue
        # 断言这一行确实是那个方法（⛔ 别按行号盲改）
        if cols[idx["原版类"]].strip() != u["cls"]:
            problems.append(f"行 {lineno} 原版类 = {cols[idx['原版类']]!r} ≠ {u['cls']!r}")
            continue
        if not cols[idx["方法"]].strip().startswith(u["m"]):
            problems.append(f"行 {lineno} 方法 = {cols[idx['方法']][:40]!r} 不以 {u['m']!r} 打头")
            continue

        if u["status"]:
            cols[idx["状态"]] = u["status"]
        if u["level"]:
            cols[idx["等级"]] = u["level"]
        if u["landing_add"]:
            cols[idx["我们的落点"]] = cols[idx["我们的落点"]] + u["landing_add"]
        if u["diff_add"]:
            cols[idx["差异"]] = cols[idx["差异"]] + u["diff_add"]
        if u["note_add"]:
            cols[idx["备注"]] = cols[idx["备注"]] + "｜" + u["note_add"]
        out_rows.append(cols)

    if problems:
        raise SystemExit("⛔ 定位校验失败，未写补丁：\n  " + "\n  ".join(problems))

    body = nl.join(["\t".join(header)] + ["\t".join(r) for r in out_rows]) + nl
    data = body.encode("utf-8")          # ⚠️ 先算进变量，再 open+write（`wb` 会先截断文件）
    f = io.open(OUT, "wb")
    try:
        f.write(data)
    finally:
        f.close()
    print("写出", OUT, "行数", len(out_rows) + 1)


if __name__ == "__main__":
    main()
