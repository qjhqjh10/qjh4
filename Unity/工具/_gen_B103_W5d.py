# -*- coding: utf-8 -*-
"""生成 账/_parts/B103_W5d.tsv：主表 战斗逻辑驱动 的 1282/1380 两行，只改 4 列。
⛔ 只读主表、只写补丁；不碰主表。表头 11 列；`方法` 列原样带 (...)。"""
import io, os

LEDGER = r"D:/4/Unity/资料/完全复刻/账/战斗逻辑驱动.tsv"
OUT = r"D:/4/Unity/资料/完全复刻/账/_parts/B103_W5d.tsv"

COLS = ["域", "原版类", "方法", "判据出处", "判据要点", "状态",
        "我们的落点", "差异", "等级", "轮次", "备注"]

raw = io.open(LEDGER, "rb").read()
eol = "\r\n" if (raw.count(b"\r\n") and raw.count(b"\r\n") == raw.count(b"\n")) else "\n"
text = raw.decode("utf-8-sig")
lines = text.split(eol)

KEYS = {
    "ResolveActiveAbility(BattleManager manager, CardScript thisCard, CardScript cardPlayed, CardScript targetCard)": True,
    "ResolveActiveAbility(BattleManager manager, CardScript thisCard, AbilityData abilityData)": True,
}

found = {}
for l in lines:
    f = l.split("\t")
    if len(f) != 11:
        continue
    if f[1].strip() == "RawCardScript" and f[2].strip() in KEYS:
        found[f[2].strip()] = f

assert len(found) == 2, "没找到两行：%r" % list(found)

ADD_LANDING = ("｜🆕 2026-10-30（`B103_W5b`）：`RuleEngine/Core/RuleCorePartial_B103W5.cs` **类尾新增** "
               "`RunExchangeRandomFriendlyMinionRow`（ca 200）· `RunRemoveEnchantmentRow`（ca 345）· "
               "`RunTransformSelfRow`（ca 80）—— 三个执行体 + `…ForTest` 门；⚠️ **均未接 ca 分派 / 计划表闸**"
               "（`EffectResolver.cs:18533` / `RuleCore.cs:17364` 本轮白名单标「⛔ 碰」⇒ 补丁写在报告 §④ 待接）。"
               "另：`RunExchangeRandomFriendlyMinionRow` 内部复用 `DoTakeControl`（抢）与 `DoExchange`（换），⛔ 不另写搬运逻辑。")

ADD_DIFF = ("｜🆕 **B103_W5b 2026-10-30**：86 支的收口推进 —— ① `71 summonUnitAndSetRef` **判等价**（`.c` 两格不同字段："
            "`+0x278` = `ReferencedCards`/列表、`+0x280` = `AbilityLogic` 实例侧「本 ability 刚召唤那张」= 我们的 `ctx.LastTargets` 记账；"
            "已补闭环断言 `B103W5_Assert.Case71`，**零产品代码改动**）；② `200 exchangeRandomFriendlyMinion` 补上"
            "「临时 criteria → 目标表」这一跳（原版 `.c:469-483` 的匿名 `TargetCriteria` = 己方随机一个部队）；"
            "③ `345 removeEnchantment` 的判据缺**已解除** —— `CardEffect + 0x88` 是 `enchantment`（按 `CardEffect.cs` 声明序核偏移），"
            "数据面早就有（卡表 `enchantment` 列 / `Enchantments.GrantedEnchantmentRowsOf`）⇒ **不必给 `ActiveAbilityLogicSpec` 加字段**；"
            "④ `80 transformSelf` 执行体已建（复用 `RuleCore.TransformUnit`），但**两处判据缺仍在**（`abilitySummon` 列不含 `activeAbilityLogic` ⇒ 「变成哪张卡」与 `keepEffects` 都取不到，执行体一律不猜、返回 0）；"
            "⑤ `230/233` 族映射已加（`forceattack`；C# `ActiveAbilityKindFamily` 的 default 那一格 + `ActiveAbilityKindFamilyNames` + 生成器 `ABILITY_KIND_FAMILY` 同步，**行数零变化**）。"
            "⚠️ **`231 forceAttackDontCountAttack` 故意未映射** —— `DoForceAttack` 没有 `dontCount` 那位，映射上去会静默丢掉「这次强制攻击不消耗行动」。")

ADD_ROUND = "｜B103_W5b"

ADD_NOTE = ("｜🆕 **B103_W5b 2026-10-30**：`cards_truth.json`（全量 1,351 张）现扫 `chosenAbility` 直方图 —— "
            "**`170/175/190/200/210/240/162/310/80/345/230/233` 出现次数全为 0**（`231` 有 2 张、`340` 10 张、`160` 1 张、`185` 1 张）"
            "⇒ 上述各支是**枚举里存在、但没有任何卡使用**的休眠件。"
            "🔴 **由此否掉「补 `EffectText` 生产者」这一路**：全池 1,129 张（及 1,213 张 `card_stats`）的卡面正文里 "
            "`steal` / `exchange` / `cemetery` / `removed from the game` / `discard` / `show` 的**效果句命中数 = 0** ⇒ 没有句子可解析，"
            "写生产者等于**发明文法**（铁律 2）⇒ 未写。"
            "同理 `162 disappearFromHand` / `310 displayFromHand` 要新建动作类型（`ActionPump`/`ActionQueue`）—— "
            "机制在枚举里真实存在，但**零卡零文本零视图侧**，本轮未建（`162`/`310` 的原子语义在 "
            "`BattleManager._ResolveDisappearFromHand_d__545__MoveNext.c:134/153`（`DestroyCard` + `PlayerHand.RemoveCardFromHand`）"
            "与 `…_ResolveDisplayFromHand_d__548__MoveNext.c`（展示 + 可能 `AddDisplayedCardActionToCemetery`），下轮可直接照做）。"
            "⚠️ `230/233` 加了族映射 ⇒ `Editor/RuleEngineTest_EngineX6.cs` 的 `X6FamilyWhitelist` 若将来接数据要补 `forceattack`（今天池内 0 行 ⇒ 不红）。"
            "🆕 另按调度台指派：`CardDef.ActiveAbilityFamilyHasVerb` 的 `handmove` 那一行**放开四个动词**"
            "（`exchange` / `stealhand` / `stealdeck` / `copycemetery`，行数零变化追加）—— 四个执行器早就在，"
            "但因**池内这四码 0 张 + `EffectText` 0 生产者**，今天仍是**零行为变化**；"
            "`EffectText` 生产者**未写**（没有句子可解析 ⇒ 写就是发明文法）。")

for k, f in found.items():
    f[6] = f[6] + ADD_LANDING
    f[7] = f[7] + ADD_DIFF
    f[9] = f[9] + ADD_ROUND
    f[10] = f[10] + ADD_NOTE

order = ["ResolveActiveAbility(BattleManager manager, CardScript thisCard, CardScript cardPlayed, CardScript targetCard)",
         "ResolveActiveAbility(BattleManager manager, CardScript thisCard, AbilityData abilityData)"]

out_lines = ["\t".join(COLS)]
for k in order:
    out_lines.append("\t".join(found[k]))

data = eol.join(out_lines) + eol
io.open(OUT, "wb").write(data.encode("utf-8"))
print("written", OUT, len(out_lines), "lines", "eol=", repr(eol))
