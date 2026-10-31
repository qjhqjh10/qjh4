# -*- coding: utf-8 -*-
"""生成 B108_A1 的账补丁（只读主表 → 写 _parts/B108_A1.tsv）。

规则（见 `merge_ledger_parts.py` 文件头）：
  · 补丁 = 与主表**完全相同的 11 列**，只放改过的行；
  · 行按 `(原版类, 方法)` 定位，所以本脚本从主表**原样取整行**再改具体格 ⇒ 不会手抄错。
  · 二进制读 / 二进制写；先算进变量、再写盘。
"""
import io
import os

LEDGER = r"d:/4/Unity/资料/完全复刻/账/战斗逻辑驱动.tsv"
OUT = r"d:/4/Unity/资料/完全复刻/账/_parts/B108_A1.tsv"

COLS = ["域", "原版类", "方法", "判据出处", "判据要点", "状态",
        "我们的落点", "差异", "等级", "轮次", "备注"]


def read_lines(path):
    b = io.open(path, "rb").read()
    text = b.decode("utf-8-sig")
    if b.count(b"\r\n") and b.count(b"\r\n") == b.count(b"\n"):
        return text.split("\r\n")
    return text.split("\n")


lines = read_lines(LEDGER)
rows = {}
for ln in lines:
    c = ln.split("\t")
    if len(c) != len(COLS):
        continue
    rows[(c[1], c[2])] = c   # 同键多行 ⇒ 后面会断言唯一性

# ---------- 定位（键必须唯一命中，否则停手） ----------
KEYS = [
    ("AbilityLogic", "GetValueFromCriteria(BattleManager manager, int minValue, int maxValue, CardScript thisCard, CardScript targetCard, ChooseValueOptions valueOptions, int multiplierFactor, CardScript refCard)"),
    ("RawCardScript", "OnMinionDeath(BattleManager manager, CardScript thisCard, CardScript actingCard, CardScript deadCard, UnitDeathType deathType, bool skipBacklash)"),
    ("CardScript", "ReactToCollectQuestPoints(CardScript unitCollected)"),
    ("CardScript", "ReactToSabotageCreated(CardScript unitCollected)"),
    ("CardScript", "ReactToSecretCreated(CardScript unitCollected)"),
    ("CardScript", "ReactToUnitStunned(CardScript stunnedUnit, CardScript actingUnit)"),
    ("CardScript", "TriggerLoseStealth(CardScript stealthUnit)"),
    ("DamageInfo", "IsPlayer()"),
]
picked = []
for k in KEYS:
    assert k in rows, "主表里找不到键：%s / %s" % k
    picked.append(rows[k])

WHITELIST_NOTE = (
    "🆕 2026-10-31（执行代理 `B108_A1`）：该 trigger 已**从 `VfxMap.NoEmitTriggerNames` 放行**，"
    "并进了 `VfxMap.IsRoutedTriggerName` + `MustRouteTriggerNames` / `MustRouteTriggerKeywords`（PascalCase 原版枚举名，"
    "两张正向锁**同长同序**）⇒ 生成器 `gen_card_vfx_by_card.py` 重跑后，这批动画行会从 `trigger_unmapped` 挪进 "
    "`trigger` 桶、由 `TryResolveTriggerPicks` 按名字归口播放（原来是掉回整桶 / 一条不播）。"
)

# ---------- 1) GetValueFromCriteria（N08 第一步：形状 → 数据位） ----------
c = picked[0]
old = "RuleEngine/Core/EffectResolver.cs:17858（IsMultiplierFoldedIntoValue）（等价实现 ="
assert old in c[6], "GetValueFromCriteria 落点里找不到旧符号"
c[6] = c[6].replace(
    old,
    "RuleEngine/Core/EffectResolver.cs:18087（IsMinPlusMaxTimesMultiplier）（🔴 2026-10-31 `B108_A1` · N08 第一步："
    "80 档判据已从**形状**换成**数据位** `card.AbilityDamageAt(op.AbilityIndex, op.LogicIndex)?.ValueOptions == 80`（v36 的 "
    "`abilityDamage` 列）；op 未归位 ⇒ `RuleEngine/Core/CardDef.cs:6839（SoleAbilityDamageIs80）`（全卡恰好一行且为 80，"
    "与 `FindSoleMultiplierRow` 同纪律）；缺那格 ⇒ `RuleEngine/Core/EffectResolver.cs:21035（V80GapNote，Debug.LogWarning 出声、不进 ctx.Log）`，"
    "⛔ 不退回形状。旧 `IsMultiplierFoldedIntoValue` 已删）（等价实现 =",
    1)
old_d = "⚠️ 仍差：`min/max` 与 `multiplierFactor` 的**数据位**没进 `CardDef`（现依赖 `AmountRef=\"enemyhand\"`）。"
assert old_d in c[7], "GetValueFromCriteria 差异里找不到旧句"
c[7] = c[7].replace(
    old_d,
    "🔴 2026-10-31（`B108_A1` · N08 第一步）：原句「`min/max` 与 `multiplierFactor` 的**数据位**没进 `CardDef`」**已过期**"
    "（铁律 5）—— `abilityDamage` 列（`min/max/valueOptions`）v36 起就在、80 档判据已换成数据位（18·0 违规消掉）。"
    "🆕 第二步（按原版公式 `min + max × multiplierFactor` 求值）= `EffectResolverPartial_B88E2.cs:467（ApplyRefCardValueOption）` 的 80 支"
    "（`B108_R3`）+ 调用点接线 `EffectResolver.cs:840` 递 `owner, chosen`（本代理）；⚠️ 数值面**未跑**（需 `play GSC35/GSC59` 复核）。",
    1)
c[10] = (c[10] + "｜🆕 2026-10-31（`B108_A1` · N08 第一步 + 第二步接线）：判据从形状（`ta==50 ∧ AmountRef==\"enemyhand\"`）换成**数据位** "
         "`valueOptions == 80`（判据 → `判据/B108_N08_valueOptions80.md` §四·1 · `判据/B88_R2_80档_VA判定.md` §一）；"
         "`opt==80` 现读 **2 行 / 2 卡**（`GSC35 Benefictus` / `GSC59 Day of Ascension`）与旧形状命中面**同一批** ⇒ 判据面无差；"
         "`TestB88PlayerHandMultiplier` 的日志子串「80 档」「计数 = {n}」逐字保留；"
         "第二步接线 = `EffectResolver.cs:840` 的 `ApplyRefCardValueOption(ctx, op, card, owner, chosen)`（原来不递 owner/chosen）。")

# ---------- 2) RawCardScript.OnMinionDeath（Unstable 81 的发射口） ----------
c = picked[1]
c[6] = (c[6] +
        " ｜ 81 `Unstable` 的**表现信号**：RuleEngine/Core/RuleCore.cs:18226（`EmitUnstableDeathTrigger` = "
        "`EmitCardTrigger(..., \"Unstable\", explicitSlot: 死者格位)` → `EvtKind.Trigger`；调用点 = `CleanupDeaths` 的 81 队列体 "
        "`RuleEngine/Core/RuleCore.cs:10196`，排在 `UnstableBlastDeferred` **之前**）")
c[10] = (c[10] +
         "｜🔴 2026-10-31（`B108_A1` · 6 个 trigger 发射口）：`Unstable`(81) 原来**只入队自爆、不发 `EvtKind.Trigger`** ⇒ "
         "Goff 18 张卡的 36 行动画**永不播**。本批补发射口（判据 = `out_cpp2il_dev_ilspy/RawCardScript.cs:1962` 的 "
         "`OnTrigger(Unstable)`，紧跟 `OnTrigger(ThisUnitDeath)`；`decomp_full` 无独立 `.c`，取 cpp2il 那份）。"
         "`EmitCardTrigger` 新增可选 `explicitPlayer` / `explicitSlot`（死者已过 `BoardSlots.RemoveAt`、`FindSlot` 找不到它 ⇒ "
         "否则落点退化成 −1）。闸 = `HasTriggerSource`（ability presence 第一道闸）⇒ **有关键词、没 81 那条 ability** 的卡不发"
         "（与原版 `TriggerAbilities(81)` 找不到 ability 同形）。⚠️ `NetProtocol.Fingerprint` 会变（多一拍 `EvtKind.Trigger`）"
         "⇒ 调度台在无 `.cs` 写手窗口重钉基线。断言 = `Editor/B108_A1_TriggerEmit_Assert.cs`。")

# ---------- 3) 五个监听者族：白名单放行 ----------
for c in picked[2:6]:
    c[7] = (c[7] + ("" if not c[7] else " ｜ ") + WHITELIST_NOTE)
    c[10] = (c[10] + ("" if not c[10] else "｜") +
             "🆕 2026-10-31（`B108_A1`）：发射链逐跳现核 = 事件源（`BroadcastWhen` / `BroadcastCreatedCardWithWarlordPreCall` / "
             "`BroadcastKeywordEvent`）→ `FireBoardWhenListener` → `EmitListenerTrigger`（`RuleEngine/Core/EffectResolver.cs:8027`）"
             "→ `TriggerNameOfWhenKind` → `EmitCardTrigger` → `EvtKind.Trigger`；缺的只是白名单那一步。")

c = picked[6]   # TriggerLoseStealth
c[10] = (c[10] +
         "｜🆕 2026-10-31（`B108_A1`）：`UnitLoseStealth`(647) 已从 `VfxMap.NoEmitTriggerNames` 放行、进白名单三表。"
         "发射链现核 = `BroadcastKeywordEvent(LosesStealth)`（`RuleEngine/Core/EffectResolver.cs:12287` / `RuleCore.cs:2167` / `:6620`）"
         "→ `FireBoardWhenListener` → `EmitListenerTrigger` → `TriggerNameOfWhenKind` 兜底 `WhenEvents.ExtraTriggerNameForKind` 的 "
         "`case LosesStealth` → `EmitCardTrigger` → `EvtKind.Trigger`。")

# ---------- 4) DamageInfo.IsPlayer()（B108_A7 回源：actingCard == null 那一支） ----------
c = picked[7]
c[6] = (c[6] + " · 🆕 2026-10-31（`B108_A1`）`RuleEngine/Core/RuleCore.cs:18247（ActingCardNullDealerSeat）`"
        "（`actingCard == null` 那一支：按**本地玩家座位 0** 记 + 一次性 `Debug.LogError`，"
        "替换改前的 `1 - 受害者座位` 且静默）")
c[10] = (c[10] +
         "｜🔴 2026-10-31（`B108_A1` · `B108_A7` 回源）：原版 `DamageInfo__IsPlayer.c:20-36` 在 `actingCard == null` 时是 "
         "**`Debug.LogError` + `return 1`（视为玩家）**；我们改前静默回落 `1 - 受害者座位`（`ApplyDamageCore`）⇒ "
         "自己打自己人时记到**对面格**。已对齐：`ActingCardNullDealerSeat`（视为**本地玩家座位 0** + 一次性 LogError）。"
         "⚠️ 「视为玩家」= 座位 0（`isPlayer` 是 bool「是不是本地玩家」，`CardPlayedSignal.LocalSeat` 缺省 0；"
         "⛔ 不是字面的 1）；有 `attackerSeat` 的路径一个字没动（`B98G1` D2 仍钉着它）。触发面很窄"
         "（`kind != Unknown ∧ attackerSeat 未知 ∧ 不在棋盘`）⇒ 今天大概不可达。")

# ---------- 写盘 ----------
assert len(picked) == 8
out_lines = ["\t".join(COLS)]
for c in picked:
    assert len(c) == len(COLS), "列数不是 11：%r" % (len(c),)
    assert "\r" not in "\t".join(c) and "\n" not in "\t".join(c)
    out_lines.append("\t".join(c))
out_text = "\n".join(out_lines) + "\n"
io.open(OUT, "wb").write(out_text.encode("utf-8"))
print("wrote", OUT, len(out_lines), "lines")
for c in picked:
    print(" -", c[1], "|", c[2][:60])
