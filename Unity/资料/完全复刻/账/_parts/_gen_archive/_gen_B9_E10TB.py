# -*- coding: utf-8 -*-
# 生成 B9_E10TB.tsv（只写本次改过的 3 行；键 = (原版类, 方法) 精确串）
# 执行代理 E10TB · 2026-10-26 · P-05（UnitState 三件）+ BL47（EffectText）
import io

LEDGER = r"d:/4/Unity/资料/完全复刻/账/战斗逻辑驱动.tsv"
OUT = r"d:/4/Unity/资料/完全复刻/账/_parts/B9_E10TB.tsv"
COLS = ["域", "原版类", "方法", "判据出处", "判据要点", "状态",
        "我们的落点", "差异", "等级", "轮次", "备注"]

b = io.open(LEDGER, "rb").read()
nl = "\r\n" if b.count(b"\r\n") == b.count(b"\n") and b.count(b"\r\n") else "\n"
rows = [l.split("\t") for l in b.decode("utf-8-sig").split(nl) if l.strip()]
by_key = {(r[1], r[2]): r for r in rows[1:]}

PATCH = {}

# ---- 1040 HasCurrentTrait：残骸闸 + 「90 档」那一本书 ----
k = ("EntityScript", "HasCurrentTrait(DefinedTrait trait)")
r = list(by_key[k])
r[5] = "A"
r[6] = ("RuleEngine/Core/UnitState.cs:968（Has）（等价实现 = `UnitState.Has`："
        "非残骸档读 `_keywords`（= 原版 `CurrentTraits(+0x128)` ＋ `activeEffects` 里 `buffType 3`），"
        "残骸档（`IsRemnant && !IsLeavingRemnant`）只读 `_remnantTraits`（= 原版 `activeEffects` 里 `buffType 90`）"
        "· 那一本的写点在 `AddKeyword`（UnitState.cs:1157）、摘点在 `RemoveAll`（:1286）与 `ClearAllKeywords`（:2584））")
r[7] = ("✅ **2026-10-26 批次 9（`E10TB`）补齐**：原版三支（`EntityScript__HasCurrentTrait.c:1-75` 全文逐句）"
        "① `(非残骸 || 正在离场)` ⇒ `CurrentTraits` ＋ `activeEffects{buffType 3}`；"
        "② 否则（**残骸**）⇒ **只**认 `activeEffects{buffType 0x5a(90 addTraitRemnant)}`。"
        "改前我们只有一本合并字典、**没有残骸闸** ⇒ 残骸身上「翻面之后挂上来的」任何关键词都看得见"
        "（实测两例：光环给的 `pack`、残骸期间授予的 trait；改前口径 `_keywords.ContainsKey` = **true**）。"
        "现在残骸档只读 `_remnantTraits`（= 残骸期间由 `AddKeyword` 记下的那一批 —— 闸③ "
        "`CardScript__AddEffect.c:159-170` 对残骸只放行 90 档与生命增减，而后者不授 trait ⇒ 二者等价）。\n"
        "**仍存的差异（零可达）**：非残骸档我们的 `_keywords` 里**也含 90 档授予的那一批**，"
        "而原版非残骸支只扫 `buffType 3` ⇒ 「挂着 90 授予的 trait 又翻回成活单位」时我们看得见、原版看不见。"
        "今天零可达：90 档只在残骸上授予，而 `TransformBackFromRemnant`（:2729）会把 `_remnantTraits` 那一批"
        "整本摘光。`KwValue` **不带**残骸闸（原版 `GetCurrentTraitValueWithModifiers.c:1-80` 里没有 `isRemnant`）。")
r[10] = (r[10].rstrip()
         + " ｜ 2026-10-26 批次 9 `E10TB`：残骸闸 + 90 档那一本书已落地（`UnitState.cs:968/:1157/:1286/:2584/:2620`）。"
           "读数（离线真跑 · `_tmp` 临时探针编 `Core/*.cs`）："
           "活单位 `Has(卡面 trait)`=**true** → `IsRemnant=true` 后=**false**（改前口径 true）→ "
           "残骸期间 `AddKeyword` 的=**true** → `IsLeavingRemnant=true` 又=**true**；`KwValue` 恒不变。"
           "全池 `play all dump` **逐字节不变**；事件电池只有 `SW25` 一行变了（见报告 §三，"
           "= 该夹具里带 `pack` 的残骸不再被当成「有 WhileInPlay 能力/有 pack」，与原版残骸支一致）。")
PATCH[k] = r

# ---- 938 TransformFromRemnant：原版复位链读清了 + 原地翻回原语备好 ----
k = ("CardScript", "TransformFromRemnant(Action onComplete)")
r = list(by_key[k])
r[5] = "A"
r[6] = ("RuleEngine/Core/UnitState.cs:2729（TransformBackFromRemnant）（等价实现 = `TransformBackFromRemnant`："
        "= 原版 `TransformFromRemnant`（清 `isRemnant(+0x65)` / `isLeavingRemnant(+0x66)` 两个字节）"
        "＋ 完成回调第一句 `ResetToValuesInHand(true)`（`UnitState.cs:2657` 逐句搬："
        "清 activeEffects/附魔那几本、数值回卡面、`currentHealth=最大生命`、"
        "摘 jam/stun/blind/stealth、清 questCounter、清两个「回合开始」位）。"
        "**今天真正的落点仍是** `RuleEngine/Core/EffectResolver.cs:15315` 的 `new UnitState(rem.Instance, false)`"
        "（换对象那条等价的近路 —— 观测量等价，见「差异」）；切到本原语 = 改动请求（报告 §四·2））")
r[7] = ("✅ **2026-10-26 批次 9（`E10TB`）：原账那句「属性由谁恢复在本方法体里读不到」已闭合。**"
        "原版真实链路 = `BattleManager__AddTransformFromRemnant.c`（`SetLeavingRemnant` + 入队 0x48）→ "
        "`BattleManager__ResolveTransformFromRemnant.c`（`TransformFromRemnant`）→ 完成回调 "
        "`…DisplayClass555_0___ResolveTransformFromRemnant_b__0.c:7-31`："
        "**`ResetToValuesInHand(card, true)`（第一句）** → 广播 Reanimated → `SetAsUnitInPlay` → "
        "`ResetSummonSickness` → `ActivateTraitsOnSummonOrEnchantment` → `UpdateWhileInPlay` → `CheckStun`。"
        "🔴 **`ResetToValuesInHand.c:45-70` 是两次 `List.Clear()`（`activeEffects(+0x108)` / "
        "`activeEnchantments(+0x110)`）** ⇒ **原版【不保留】buff / 附魔**；它保留的是**没被它碰的字段**："
        "`companionCounter(+0x60)` / `tideCounter(+0x5C)` / `orderInPlay` / `isPraying`。"
        "（同一个 `ResetToValuesInHand` 也被「翻成残骸」那一跳调用 —— "
        "`BattleManager._ResolveTransformIntoRemnant_d__554__MoveNext.c:43`。）\n"
        "**仍存的差异**：① `currentDropPodHealth/Attack(+0xcc/+0xe0)` 我们**没有字段**（drop pod 在我们这边是"
        "`KwValue(droppod)`）⇒ 原版那两句没搬；② `questCounter(+0x270)` 原版复位 0，我们换对象那条路**不复位** "
        "`Instance.QuestCounter`（池内 `spellType 120` **0 张** ⇒ 零可达）；③ 附魔（`+0x110`）的清除归调用方，"
        "换对象那条路**没清** `CardInstance.ActiveEnchantments`；④ `IsLeavingRemnant` 的瞬态"
        "（原版翻回中间态是「不是残骸、正在离场」）在我们这条路上不出现 —— 两侧终态同为 0。")
r[10] = (r[10].rstrip()
         + " ｜ 2026-10-26 批次 9 `E10TB`：判据闭合（`ResetToValuesInHand` 那一跳）＋ 原语落地"
           "（`UnitState.cs:2657/:2729`）。离线真跑读数：翻回后 `IsRemnant/IsLeavingRemnant`=false、"
           "`AttackBase/MaxHealthBase` 回卡面、`Health=MaxHealth`、卡面 trait 又可见、`statMods=0`、"
           "`AttacksThisTurn=0`、`UsedActiveAbilityThisTurn=false`、`OathUsesThisTurn=0`（全部 = 原版逐句）。"
           "⚠️ 派活单原写的验收「buff/标记与翻回前一致」**与第一权威相反** —— 见报告 §三·2（判据原文已引）。")
PATCH[k] = r

# ---- 853 RemoveEffect：按记录摘的原语（1/3/90/5/40/45 六档）----
k = ("CardScript", "RemoveEffect(CardEffect removedEffect, bool fromJam)")
r = list(by_key[k])
r[5] = "B"
r[6] = ("RuleEngine/Core/UnitState.cs:2792（RemoveEffectRecord）（等价实现 = `UnitState.RemoveEffectRecord`："
        "入参 = `UnitState.EffectRecord`（:2744，原版 `CardEffect` 的六列投影："
        "`buffType(+0x38)` / `trait(+0x3c)` / `meleeAttackChange(+0x48)` / `armourChange(+0x58)` / "
        "`healthChange(+0x60)` / `permanentBuff(+0x74)`）；逐档照 `.c`："
        "1 = 夹血 + `RemoveKeyword(armour)`（= `AddArmour(-x)`）· 3/90 = 卡面没印才 `RemoveAll(trait, silent)`"
        "（= `RemoveTraitSilently`）· 5 = `AttackBase/MaxHealthBase` 回卡面 + 夹血 · "
        "45 = drop pod 血量（`KwValue(droppod)`）减 `healthChange` · 40 = 无动作（我们的 `Ability` 恒读卡面）· "
        "`permanentBuff` 非 0 直接 return）")
r[7] = ("🟡 **2026-10-26 批次 9（`E10TB`）按记录摘的原语已备好**（`decomp_full/CardScript__RemoveEffect.c:1-279` "
        "全文逐 case 对过）。**仍缺三处**：① **调用点** —— 今天没有任何一处按「一条记录」摘"
        "（`RevertBuffs` / `RevertGrantsFrom` / `Enchantment.Remove` 走的是「按来源/关键词整批摘」），"
        "切换 = 改动请求（报告 §四·3）；② case 3 的两条特判（`trait==230 dropPod ⇒ RemoveDropPod`、"
        "`trait==610 pack ⇒ +0x358=0`）**没有落点**（前者 = `RuleCore.OpenDropPod` 那条独立机制，"
        "后者那格缓存在 `Aura.cs`）；③ case 12 的 `BroadcastQuestResolved` + `questCounter=0` 在 "
        "`Enchantment.Remove` 侧（本方法只留 `fromJam` 形参）。"
        "池内 `buffType 5/40/45` **各 0 条** ⇒ 今天零可观察；`buffType 3`（368 条）今天也不走这条按记录摘的路。")
r[10] = (r[10].rstrip()
         + " ｜ 2026-10-26 批次 9 `E10TB`：原语落地（`UnitState.cs:2792` / `EffectRecord` :2744）。"
           "离线真跑读数：case5 攻/血回卡面且夹血 ✓ · case45 drop pod 血量 3→1 ✓ · "
           "case1 护甲 2→1 ✓ · case40 主动能力仍是卡面那份 ✓ · `permanentBuff=true` ⇒ 什么都不摘 ✓ · "
           "空档 buffType 8 ⇒ 不动 ✓（共 8 条断言，0 失败）。")
PATCH[k] = r

# ======================================================================
#  顺带修「被我这次插行顶掉的 4 条落点」（`check_ledger_points` 报的是【失败】不是【漂移】，
#  而 `--fix` 只修行号漂移、修不了「落点落到注释行/语句中间」⇒ 只能由补丁重指）。
#  ⚠️ 只改「我们的落点」列的行号，其余列原样保留。
# ======================================================================
REPOINT = {
    ("CardScript", "ClearEndOfTurnEffects()"): [
        ("UnitState.cs:1646-1685", "UnitState.cs:1682-1721"),
    ],
    ("CardScript", "HasWhileInPlayAbility()"): [
        ("UnitState.cs:2374", "UnitState.cs:2410"),
        ("UnitState.cs:1849", "UnitState.cs:1869"),
    ],
    ("CardScript", "HasWhileInPlayAdjacentEffect()"): [
        ("UnitState.cs:2429", "UnitState.cs:2465"),
    ],
    ("EntityScript", "GetCurrentTraitValueWithModifiers(DefinedTrait trait)"): [
        ("UnitState.cs:1074-1078", "UnitState.cs:1104-1108"),
        ("UnitState.cs:2468-2472", "UnitState.cs:2504-2508"),
        ("UnitState.cs:1090-1135", "UnitState.cs:1120-1165"),
    ],
}
for k2, subs in REPOINT.items():
    r = list(by_key[k2])
    for old, new in subs:
        assert old in r[6], (k2, old, r[6][:200])
        r[6] = r[6].replace(old, new)
    r[10] = (r[10].rstrip()
             + " ｜ 2026-10-26 批次 9 `E10TB`：落点行号重指（本批在 `UnitState.cs` 里插了"
               "`Has` 的残骸闸 / `AddKeyword` 的记账 / 类尾原语 ⇒ 下游整体下移；"
               "`check_ledger_points` 把这条判成「失败」而不是「漂移」⇒ 只能由补丁重指，`--fix` 修不了）")
    PATCH[k2] = r


# 🔴 单元格里**不许有真换行** —— 主表 1375 行全部恰好 11 列（现读），
#    带 `\n` 的行会被当成两条、`check_ledger_points` / 合并工具都会崩。用 ` ｜ ` 当段分隔。
def cell(s):
    return s.replace("\r\n", " ｜ ").replace("\n", " ｜ ")


with io.open(OUT, "wb") as f:
    lines = ["\t".join(COLS)] + ["\t".join(cell(c) for c in PATCH[k2]) for k2 in PATCH]
    f.write(("\n".join(lines) + "\n").encode("utf-8"))
print("wrote", OUT, "rows", len(PATCH))
for k2 in PATCH:
    assert len(PATCH[k2]) == 11, (k2, len(PATCH[k2]))
    assert "(" in PATCH[k2][2], k2
