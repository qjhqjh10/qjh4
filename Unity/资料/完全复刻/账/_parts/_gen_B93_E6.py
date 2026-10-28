# -*- coding: utf-8 -*-
"""B93_E6 账补丁生成器：从主表按 (原版类, 方法) 键取出 3 行 → 追加本次订正 → 写 _parts/B93_E6.tsv。
读写法 = 裸 split("\t")（⛔ 不用 csv）；写前读→改→再读回环比对 11 列。"""
import io, os, sys

MAIN = r"d:/4/Unity/资料/完全复刻/账/战斗逻辑驱动.tsv"
OUT = r"d:/4/Unity/资料/完全复刻/账/_parts/B93_E6.tsv"
HDR = "域\t原版类\t方法\t判据出处\t判据要点\t状态\t我们的落点\t差异\t等级\t轮次\t备注"

b = io.open(MAIN, "rb").read().decode("utf-8")
lines = b.split("\r\n") if b.count("\r\n") > 10 else b.split("\n")

WANT = [
    ("BattleManager", "PayCardCost(CardScript card, CardScript targetCard)"),
    ("DamageDeltToEnemyUnit", "OnDamageDelt(DamageSignal obj)"),
    ("EnergySpentSignal", "Raise()"),
]

# ---- 每行的追加内容（列 6 = 落点, 7 = 差异, 10 = 备注；状态/等级/轮次不动）----
APPEND = {}

APPEND[("BattleManager", "PayCardCost(CardScript card, CardScript targetCard)")] = {
    6: " ｜ 🔴 **B93_E6 2026-10-28：`costType` 形参已接** —— `RuleCore.cs:3943`（出单位 · `costType: EnergyCostCard`）"
       "与 `EffectResolver.cs:2582`（出战术卡 · 同 CardCost）两处显式传；`SpendNormalMana` → `RecordEnergySpent` → "
       "`AchievementTally.AddEnergySpent`（按类型分档那一格）。",
    7: " ｜ 🔴 **B93_E6 2026-10-28 就地订正（铁律 5）**：上面 ② 那句「本仓**无任何等价事件**（`BattleEvent.cs` 无 EnergySpent 类）"
       "⇒ **相关成就/计量不会记**」**已过期** —— 不新增 `EvtKind`（裁定不变），但**等价记账口**现在有了："
       "`AchievementTally.EnergySpentByCostType`（`EnergyCostCard = 1`）+ 原有的 `RuleCore.EnergySpentTotal`（总数，P-01/E11RC）。"
       "两条发起源各按原版 `EnergySpentSignal.Type` 传值：`PayCardCost`（出单位 `RuleCore.cs:3943` / 出战术卡 `EffectResolver.cs:2582`）"
       "→ CardCost(1)；`PayActiveAbilityCostOath`（单位主动能力 `RuleCore.cs:15438`）→ AbilityCost(2)。"
       "**仍未消的是消费方**（任务挑战 `EnergySpent__OnEnergySpent` 的本地落地）⇒ 归 日常/奖励线，见 `EnergySpentSignal.Raise()` 行。",
    10: " ｜ B93_E6 2026-10-28：数值侧一个字没动（`costType` 只多记一格，不进 `Fingerprint`）；`play check` rc=0、"
        "结算指纹 `ef8a0249d78cd91808db49e30da6cd2a` 与基线一致（1129 主档 + 1073 电池逐字节相同）。",
}

APPEND[("DamageDeltToEnemyUnit", "OnDamageDelt(DamageSignal obj)")] = {
    6: " ｜ 🔴 **B93_E6 2026-10-28：写点已接（原版口径那一格不再空着）** —— "
       "`RuleEngine/Core/RuleCore.cs:8475`（`ApplyDamageCore` 函数头、`DamageAfterReduction` 之后**所有早退之前**："
       "`AddDamageRaw(1 - 攻方, kind, dmg)`，`dmg` = **扣甲前**原值）＋ `RuleEngine/Core/RuleCore.cs:8967`"
       "（函数尾：易伤第二发 `AddDamageRaw(…, VulnerableRaw(u))` —— 原版那第二发是**另一条 `DamageInfo`**、自己也广播一次）。"
       "两条都**与 `AddDamage` 并存、没有替换它**（⛔ 双计风险见 `DamageByKindRaw` 的 doc）。",
    7: " ｜ 🔴 **B93_E6 2026-10-28：① 那一半（量）在【记账层】已消** —— `DamageByKindRaw` 现在拿到的是**扣甲前**、"
       "且记在早退**之前**（盾 / 闪避 / 无敌 / 空投舱 / 堡垒整份吃下都「先记再退」= 原版 absorbed 也计入）、"
       "易伤第二发另记一笔 ⇒ 与原版 `DamageInfo.damageValue` 同口径。"
       "**但【消费侧仍是老的】**：`BattleDriver.AwardAchievements` 读的还是 `AddDamage`（扣甲后）—— 它在 "
       "`CardPresentation/` 里（B3/E5 的文件），本路无权改 ⇒ **玩家可见的成就数值仍然少计，本行不升档（留 B）**。"
       "② 归属（按受害者对侧 vs `actingCard.isPlayer`）与 ③ 分类（靠调用点打 `DamageKind`）**原样未动**。"
       "⚠️ 已知唯一缺口（今天不可达）：空投舱血池把**易伤第二发**也吃掉那一支会绕过函数尾 ⇒ 那一发原版会广播、我们没记；"
       "全池 0 张卡带 `dropPod`（现扫 `cards_engine.json`）⇒ 行为差异 0。",
    10: " ｜ B93_E6 2026-10-28：改动**零行数变化**（`RuleCore.cs` 8 行就地改写 ⇒ 行号零漂移，"
        "`check_ledger_points.py 战斗逻辑驱动.tsv` 漂移 0）；typecheck 运行时 0 / 编辑器 0；"
        "`ruleprobe play check` rc=0、指纹与基线一致（两格今天 0 个读者 ⇒ 输出逐字节不变）。",
}

APPEND[("EnergySpentSignal", "Raise()")] = {
    6: " ｜ 🔴 **B93_E6 2026-10-28：写点已接** —— `RuleCore.SpendNormalMana(..., int costType = 0)` → "
       "`RecordEnergySpent(ctx, p, paid, source, costType)` → `ctx.Tally.AddEnergySpent(seat, paid, costType)`"
       "（`RuleEngine/Core/RuleCore.cs:13087 / :13115 / :14983 / :14997`）。三个调用点："
       "`RuleCore.cs:3943`（出单位）与 `EffectResolver.cs:2582`（出战术卡）→ CardCost(1)；"
       "`RuleCore.cs:15438`（单位主动能力）→ AbilityCost(2)；维护费 `RuleCore.cs:1826` 传 `energySpentSignal: false`（不动）。",
    7: " ｜ 🔴 **B93_E6 2026-10-28**：施工单 ① 那半**已完成**（`costType` 形参 + 三处显式传值）。三条口径写死："
       "· **刻意不记的两处**（原版**根本不发**这条信号，不是漏接）：卡面付费前缀（`EffectResolver` 的 `CostPrefixPayable` 那一支）—— "
       "原版对应 `PayCardPlayedCostOath.c`（无 `EnergySpentSignal___ctor`）与 `RawCardScript__TriggerAbility.c:52`"
       "（`UseMana(…, ManaType 5)`，也不发）；全树 `grep EnergySpentSignal___ctor` **只有两个命中**。"
       "⇒ 这两个地方传 `costType = 0`：只记 `EnergySpentTotal` 那个总数、**不记分档**（⛔ 不编类型）。"
       "· ⚠️ **仍存的差异**：原版记 `get_CurrentCost`（**付费后重取**），我们记实付 `paid` ⇒ 在「付完会改费用」的卡上会分叉"
       "（两格都取 `paid`，所以彼此一致）。· **② 消费方仍缺**（任务挑战）⇒ 状态留 B。",
    10: " ｜ B93_E6 2026-10-28：⛔ 仍**不加 `EvtKind`**（上一条裁定继续成立：没有消费口 = 死代码 + 表现层会按关键词误播）。"
        "改动**零行数变化**（`RuleCore.cs` 4 处就地改写 + `EffectResolver.cs` 1 处同长替换）⇒ 行号零漂移；"
        "typecheck 0/0；`ruleprobe play check` rc=0、指纹 `ef8a0249…` 与基线一致。"
        " ｜ ⚠️ 归属线索（调度台请复核）：本行把 **`RuleCore.cs:15438`（单位主动能力）** 认作 `PayActiveAbilityCostOath`"
        "（判据 = 全树唯一调用点 `CardScript__UsedActiveAbility.c:44`）；而账行 `PayActiveAbilityCostOath(CardScript card)`"
        "的落点写的是 `EffectResolver.cs:9152 ResolveOathAbility` —— 后者按调用链更像 `PayCardPlayedCostOath`"
        "（`AbilityLogic__PlayAbility.c:2033`，**不发**信号）。本路**未改那一行**（A 级、不属本路），只把反证记在这里。",
}

out_rows = []
for cls, meth in WANT:
    hit = None
    for i, ln in enumerate(lines):
        if not ln:
            continue
        c = ln.split("\t")
        if len(c) != 11:
            continue
        if c[1] == cls and c[2] == meth:
            hit = (i, c)
            break
    if hit is None:
        sys.exit("MISS: %s | %s" % (cls, meth))
    i, c = hit
    c = list(c)
    for col, txt in APPEND[(cls, meth)].items():
        c[col] = c[col] + txt
    if len(c) != 11:
        sys.exit("COLS != 11 after edit: %s" % (cls,))
    out_rows.append("\t".join(c))

data = HDR + "\n" + "\n".join(out_rows) + "\n"
io.open(OUT, "wb").write(data.encode("utf-8"))

# ---- 回环比对：读回来，逐列与内存里的 11 列全等 ----
back = io.open(OUT, "rb").read().decode("utf-8").split("\n")
assert back[0] == HDR, "header mismatch"
for k, row in enumerate(out_rows):
    rb = back[k + 1].split("\t")
    rm = row.split("\t")
    assert len(rb) == 11 and len(rm) == 11, "col count %d/%d" % (len(rb), len(rm))
    for j in range(11):
        assert rb[j] == rm[j], "col %d mismatch in row %d" % (j, k)
print("OK  wrote %s  rows=%d  roundtrip 11 cols each" % (OUT, len(out_rows)))
for row in out_rows:
    r = row.split("\t")
    print("  -", r[1], "|", r[2], "| 状态", r[5], "| 等级", r[8])
