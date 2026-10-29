#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""生成 `账/_parts/B98_G4.tsv`（执行代理 B98_G4 · 第九十八会话 · 波 5b）。

做法：**读主表 → 只改本代理动过的那几列 → 写出补丁**（其余列逐字照抄 ⇒ 合并不会丢内容）。
⛔ 裸 `split("\t")`（与 `merge_ledger_parts.py` 同一口径）；
⛔ 先算进变量、确认无误再写盘（`io.open(p,'wb').write(<表达式>)` 会先截断再求值）。
"""
import io

LEDGER = r"d:/4/Unity/资料/完全复刻/账"
PARTS = LEDGER + r"/_parts"

COLS = ["域", "原版类", "方法", "判据出处", "判据要点", "状态",
        "我们的落点", "差异", "等级", "轮次", "备注"]
IDX = {k: i for i, k in enumerate(COLS)}


def load(name):
    b = io.open(LEDGER + "/" + name, "rb").read()
    text = b.decode("utf-8-sig")
    eol = "\r\n" if b.count(b"\r\n") == b.count(b"\n") and b.count(b"\r\n") else "\n"
    rows = text.split(eol)
    return rows, eol


def find(rows, cls, method):
    hits = []
    for i, L in enumerate(rows):
        c = L.split("\t")
        if len(c) != len(COLS):
            continue
        if c[IDX["原版类"]] == cls and c[IDX["方法"]] == method:
            hits.append((i, c))
    if len(hits) != 1:
        raise SystemExit("⛔ 定位不唯一/找不到：%s / %s ⇒ %d 处" % (cls, method, len(hits)))
    return hits[0]


def patch_row(rows, cls, method, cls_name, **cols):
    i, c = find(rows, cls, method)
    for k, v in cols.items():
        if k not in IDX:
            raise SystemExit("⛔ 未知列 " + k)
        c[IDX[k]] = v
    return c


def append_col(c, key, text):
    c[IDX[key]] = (c[IDX[key]] or "") + text


# ======================================================================
#  战斗逻辑驱动.tsv
# ======================================================================
battle, _ = load("战斗逻辑驱动.tsv")
out = []

# ---- 240 CancelBattleActionFromQueue：B → A（表现层那一跳已接）----
c = patch_row(battle, "BattleManager",
              "CancelBattleActionFromQueue(List<BattleAction> queue, int actionIndex)",
              "BattleManager", 状态="A")
append_col(c, "我们的落点",
           " · 🆕 2026-10-29（`B98_G4`）：表现层订阅 = `CardPresentation/Battle/BattleDriver.cs:5556（Begin）`"
           "（同一物理行 `ActionQueue.OnCancelCreateHandCard = OnCancelCreateHandCard;`）"
           "· `BattleDriver.cs:4245（ForEachStaticHook）`（同一行 `visit(..., = null)`）"
           "· 处理器 `CardPresentation/Battle/BattleDriver.cs:38654（OnCancelCreateHandCard）`"
           "（等价实现 = 按 `UnitState` **身份**找 `CardView` 再 `SetActive(false)`；`FindCardViewOfUnit:38675` 走 `_myUnitViewByUnit`→`_foeUnitViewByUnit`→我方手牌按 `u.Instance` 引用比）")
append_col(c, "差异",
           "｜🆕 **B98_G4 2026-10-29：表现层那一跳已接** —— `BattleDriver.Begin()` 里订阅、"
           "`ForEachStaticHook` 里摘（清单只此一处），处理器按 **`UnitState` 身份**（不是卡名 —— 同名两份会关错）"
           "找视图后 `SetActive(false)`；目标空 / 找不到视图 ⇒ **出声**（⛔ 不静默）。"
           "⚠️ 全链今天**休眠**（全仓 `actionType: 0x17` 入队点 = 0）。")
append_col(c, "轮次", "｜B98_G4")
append_col(c, "备注",
           "｜[B98_G4 2026-10-29] `BattleDriver.cs` **零行数漂移**（两处同一物理行追加 + 新成员全在类尾）；"
           "订阅点全仓 **1 处**（`grep -c \"ActionQueue.OnCancelCreateHandCard = OnCancelCreateHandCard;\"` = 1）；"
           "`typecheck` 0/0。断言 = `CardPresentation/Editor/B98G4_Assert.cs` 240a–240e（240d 是本体的灭自证："
           "调用后 `activeSelf` 必须变 false；240e 还原现场）。")
out.append(c)

# ---- 254 ChoiceOfCardEnemy：面板侧落地，但记 B（时序 + choiceEnchantment）----
c = patch_row(battle, "BattleManager",
              "ChoiceOfCardEnemy(CardScript actingCard, CardScript targetCard, int selectedCard)",
              "BattleManager", 状态="B")
append_col(c, "我们的落点",
           " · 🆕 2026-10-29（`B98_G4`，面板侧落地）：`CardPresentation/Battle/BattleDriver.cs:38722（SharedChoiceOptionsFor）`"
           "（等价实现 = `ReferenceEquals(ctx.ChoiceOptionsSource, PendingCard)` ∧ 那份全在候选域里（按 `Id`）⇒ 照用引擎那一份）"
           "· 调用点 `BattleDriver.cs:11233（ShowAsk）`（同一物理行；否则落回 `DrawForDisplay` 旧兜底）")
append_col(c, "差异",
           "｜🆕 **B98_G4 2026-10-29：面板侧已按施工单落地** —— `ShowAsk` 现在先问 `SharedChoiceOptionsFor(PendingCard, cands)`："
           "来源卡命中且那份都在候选域里 ⇒ **照用引擎那一份**（⛔ 不再自己抽一边），否则落回 `DrawForDisplay`。"
           "🔴 **仍记 B 的两条理由**：① **时序** —— 我们的面板在**引擎结算之前**摆（`BeginPlay` → 面板 → `DoPlay`），"
           "而 `BuildChoiceOptions` 在**结算里**才跑 ⇒ 大多数 ask 时 `ChoiceOptionsSource` 还是上一次的卡 ⇒ 走旧兜底；"
           "要原版那样「开面板前就算好」得在 `EffectResolver` 侧提前（不在本代理白名单）；"
           "② `choiceEnchantment(16)` 的通用化仍在 `Enchantment.cs`。")
append_col(c, "轮次", "｜B98_G4")
append_col(c, "备注",
           "｜[B98_G4 2026-10-29] 断言 = `B98G4_Assert` 254a–254l："
           "254d（引擎随机流只前进 `options.Count`）/ 254g（面板候选 **== `ctx.ChoiceOptions` 逐项且同序**）/ "
           "254h（面板取候选**一格随机都不消耗**）/ 254j（排序只改摆法、不改集合）/ "
           "254l（来源卡不是这一张 ⇒ 回 `null`，🧨 去掉 `ChoiceOptionsSource` 那道闸 ⇒ 红）。")
out.append(c)

# ---- 255 ChoiceOfCardPlayer：同上（① 已由 B97_BD 落）----
c = patch_row(battle, "BattleManager",
              "ChoiceOfCardPlayer(CardScript actingCard, CardScript targetCard, int actionValue)",
              "BattleManager", 状态="B")
append_col(c, "我们的落点",
           " · 🆕 2026-10-29（`B98_G4`）：与账 254 **同一份**面板施工单 —— `BattleDriver.cs:38722（SharedChoiceOptionsFor）`"
           "· `BattleDriver.cs:11233（ShowAsk）`")
append_col(c, "差异",
           "｜🆕 **B98_G4 2026-10-29：残余 ② 的面板侧已落地**（与 254 同一行代码）—— ① 上限读 `CardDef.ChoicesMaxTargets` 已由 `B97_BD` 落。"
           "🔴 **仍记 B 的理由同 254**：我们的面板在引擎结算之前摆 ⇒ `ChoiceOptionsSource` 多数时候还没指向这一问 ⇒ 走旧兜底；"
           "要真「同一处算」得让 options 在**开面板前**就算好（`EffectResolver` 侧，本代理白名单外）。")
append_col(c, "轮次", "｜B98_G4")
append_col(c, "备注",
           "｜[B98_G4 2026-10-29] 断言同 254（同一段 `B98G4_Assert` 254a–254l；⛔ 没有另写第二份）。")
out.append(c)

# ---- 233 CanPlayCard：showTips 收口已核，仍 B（actionAlreadyStarted 未建模）----
c = patch_row(battle, "BattleManager",
              "CanPlayCard(CardScript card, bool showTips, bool initiateAction, bool actionAlreadyStarted)",
              "BattleManager", 状态="B")
append_col(c, "差异",
           "｜🆕 **B98_G4 2026-10-29 现核（本件零代码改动）**：`showTips` 那一半**已经收口** —— 提示出口 = "
           "`BattleDriver.NotifyCantDoAction`（语音 + 抬头文字一次调用，`W63` 2026-10-22），四档拒因经 `RuleCodes.TermKey` → `HintForCode` 1:1；"
           "② 教程闸我们**有**（`BattleDriver.TutorialPermits`，接在 `CanDropAtSlot` 与 `DoPlay` 两处，`A938`）。"
           "🔴 **仍 B 的唯一残余** = `actionAlreadyStarted`：引擎 `RuleCore.CanPlayCard` 收了这个形参但**体内没读**"
           "（原版 `param_5` 参与 `manager+0x245/0x246` 那两格闸，`.c:66-72`）。")
append_col(c, "轮次", "｜B98_G4")
append_col(c, "备注",
           "｜[B98_G4 2026-10-29] 断言 = `B98G4_Assert` 233a/233b/233c：四档拒因码的**文案互不相同**且非空"
           "（🧨 四档塌成一档 ⇒ 红）；无引擎码时退回旧文案、**不静默**。")
out.append(c)

# ---- 1037 GetThisCardPlayedDamage：B → A（两条残余都消）----
c = patch_row(battle, "EntityScript",
              "GetThisCardPlayedDamage(CardScript targetCard)",
              "EntityScript", 状态="A")
append_col(c, "我们的落点",
           " · 🆕 2026-10-29（`B98_G4`，表现层残余）：`CardPresentation/Battle/BattleDriver.cs:34447（BattleBoardPreview）`"
           "（`if (info.OtherIsTactic || !TryLocateBoardView(other, out _, out _))` ⇒ **手牌里的单位卡**也走这一支）"
           "· `BattleDriver.cs:37979（HandTacticUnderPointer）`（不再跳过 `isUnit`）")
append_col(c, "差异",
           "｜🆕 **B98_G4 2026-10-29：两条残余都消** —— ① `BattleBoardPreview` 的 `other` 不再只认战术卡："
           "`OtherIsTactic || !TryLocateBoardView(other, …)` ⇒ **手牌里的单位卡**也走 `GetThisCardPlayedDamage` 那一支"
           "（`RuleCore.ThisCardPlayedDamageEntries`），`HandTacticUnderPointer` 也不再跳过 `isUnit`（原版 `cardTracked` 本来就不分单位/战术）。"
           "⚠️ `OtherIsTactic`（底光那一格）**没放宽** —— 原版只对计策卡不点亮攻击类型底光（`.c:44/:132`）。"
           "⚠️ 数值预览仍需**真机核**（预览画在哪一格是表现层的事）。")
append_col(c, "轮次", "｜B98_G4")
append_col(c, "备注",
           "｜[B98_G4 2026-10-29] 断言 = `B98G4_Assert` 1037a–1037f：1037c ★ 手牌里的**单位卡**也出预览（`Valid == true`，"
           "🧨 退回「只喂战术卡」⇒ `Valid == false` ⇒ 红）· 1037d 单位卡不点亮底光 · 1037f 引擎取数口自洽"
           "（`ThisCardPlayedDamage == Σ entries` 抽 3 张逐张对）。")
out.append(c)

# ---- 94 ExecuteAction(Campaign)：C → B（真实宿主 + 接线已落，剩宿主三缺口 + 零可观测）----
c = patch_row(battle, "AiScripted",
              "ExecuteAction(BattleManager manager, ScriptedActionCampaignData action)",
              "AiScripted", 状态="B")
append_col(c, "我们的落点",
           " · 🆕 2026-10-29（`B98_G4`，接线）：`CardPresentation/Battle/BattleDriver.cs:9126（BeginBattleAfterSetup）`"
           "（同一物理行 `_campaignHost = new BattleCampaignScriptHost(this);`）· 字段 `BattleDriver.cs:38804（_campaignHost）`"
           "· 读口 `BattleDriver.cs:38808（CampaignHost）`")
append_col(c, "差异",
           "｜🆕 **B98_G4 2026-10-29：接线已落** —— `BeginBattleAfterSetup` 建**真实宿主** `BattleCampaignScriptHost`"
           "（15 个成员已由 `B97_W3` 接到真实资源），不再只有 `NullCampaignScriptHost` 出声兜底。"
           "🔴 **仍 B 的两条**：① `BattleCampaignScriptHost` 自己那三条如实缺口（`ShowRadioMessage` **无立绘** / "
           "`LandWarlordsInCampaign` **只出声** / `DisplayWarlordChatMessage` **只落 `Intro(0)`**）；"
           "② 战役脚本数据本地不存在 ⇒ 接上**仍零可观测**。")
append_col(c, "轮次", "｜B98_G4")
append_col(c, "备注",
           "｜[B98_G4 2026-10-29] 断言 = `B98G4_Assert` 94a/94b：宿主非 null ∧ **不是** `NullCampaignScriptHost`。"
           "⚠️ 「`Begin/Pump/End` 被调」这条**写不了**：`CampaignScriptTurnDriver` 要 `List<CampaignScriptAction>`，"
           "而战役脚本数据本地 0 条 ⇒ 如实记在这里，⛔ 不假装断言盖住了。")
out.append(c)

# ======================================================================
#  音频_语音.tsv（召唤语音闸）
# ======================================================================
audio, _ = load("音频_语音.tsv")
c = patch_row(audio, "VoiceLinesController", "DisplaySummonMessage(CardScript cardScript)",
              "VoiceLinesController", 状态="A")
c[IDX["差异"]] = ("一致（🆕 2026-10-29 `B98_G4`：`ephemeral` / `ambush` / 非 `HandPlayed` 这三档现在**真的不播**了 —— 见备注）")
append_col(c, "轮次", "｜B98_G4")
append_col(c, "备注",
           "｜[B98_G4 2026-10-29] **补上缺的那道闸**：`BattleDriver.SpeakFor` 的 `case EvtKind.Deploy` 现在 "
           "`if (!ShouldTriggerSummonAnim(e, out _)) return;`，闸体 = `BattleDriver.SummonAnimGate`（与 `PlaySummonDelayedVfx` **共用**，"
           "⛔ 不是第二份）。判据 = `CardScript._MinionPlayedIntoField_d__314.MoveNext.c:186-207`（过了闸才 invoke `card+0x138` = "
           "`DisplaySummonMessage`）+ `BattleCardManager__CreateCard.c:86-89`（唯一订户）。🔴 **这是真的可观测变化**："
           "效果召唤 / `ephemeral` / `ambush` / `AutoPlayFromHand` **不再播登场语音**（原来这四档都会从 `deploy` 精确档捞一条念）。"
           "⚠️ 声音是**删掉一类**，最容易「删多了没人发现」⇒ **建议进一次真机听感验收**。"
           "断言 = `B98G4_Assert` SV2–SV6b（含灭自证：挂了 `ephemeral`/`ambush` 不播、摘掉又播）。")
audio_out = [c]

# ======================================================================
#  写盘（先拼好再写；`\n` 行尾）
# ======================================================================
lines = []
for r in out + audio_out:
    if len(r) != len(COLS):
        raise SystemExit("⛔ 列数不是 11：%d" % len(r))
    if "\n" in "".join(r) or "\r" in "".join(r):
        raise SystemExit("⛔ 单元格里有换行")
    lines.append("\t".join(r))

payload = "\t".join(COLS) + "\n" + "\n".join(lines) + "\n"
p = PARTS + "/B98_G4.tsv"
io.open(p, "wb").write(payload.encode("utf-8"))
print("WROTE", p, len(payload.encode("utf-8")), "bytes,", len(lines), "rows")

# 回读自检：逐列往返（读→写→再读，11 列全等）
b = io.open(p, "rb").read().decode("utf-8")
back = b.split("\n")
print("reread cols:", len(back[0].split("\t")), "rows:", len(back) - 2)
assert len(back[0].split("\t")) == 11
for L in back[1:]:
    if not L:
        continue
    assert len(L.split("\t")) == 11, L[:80]
print("OK")
