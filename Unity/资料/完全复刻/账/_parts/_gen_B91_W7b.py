#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""生成 B91_W7b 账补丁：G1 奖励事件层真的建了（17 条）＋ LiveOpsManager.BuildHandler（406）。

方言与 merge_ledger_parts.py 一致：**裸 tab 切列**（`l.split("\\t")`），LF 写盘。
"""
import io

LEDGER = r"d:/4/Unity/资料/完全复刻/账/日常_奖励.tsv"
OUT = r"d:/4/Unity/资料/完全复刻/账/_parts/B91_W7b.tsv"
TAG = "B91_W7b 2026-10-28"

raw = io.open(LEDGER, "rb").read()
assert raw.count(b"\r\n") == 0
rows = [l.split("\t") for l in raw.decode("utf-8-sig").split("\n") if l.strip()]
HDR, BODY = rows[0], rows[1:]
assert len(HDR) == 11
IDX = {}
for i, r in enumerate(BODY):
    assert len(r) == 11
    IDX.setdefault((r[1], r[2]), []).append(i)


def get(cls, meth):
    hit = IDX.get((cls, meth), [])
    if len(hit) != 1:
        raise SystemExit("HIT %d for (%s, %s)" % (len(hit), cls, meth))
    return list(BODY[hit[0]])


def app(col, text):
    return (col + text) if col.strip() else text


def put(cls, meth, **kw):
    r = get(cls, meth)
    if "status" in kw: r[5] = kw["status"]
    if "need" in kw:   r[4] = kw["need"]
    if "lead" in kw:   r[6] = kw["lead"]
    if "diff" in kw:   r[7] = kw["diff"]
    if "note" in kw:   r[10] = app(r[10], kw["note"])
    for c in r:
        assert "\t" not in c and "\n" not in c and "\r" not in c
    return r


OUTROWS = []

# ── LiveOpsManager.BuildHandler（406）─────────────────────────────────────────
OUTROWS.append(put(
    "LiveOpsManager", "BuildHandler(ILiveOpsHandler handler, LiveopsHandlerData handlerData, FeatureConfig config)",
    status="B",
    diff=("🆕 %s：**逐句重走 `_BuildHandler_d__17__MoveNext.c`（472 行）后补了两跳**："
          "① 遍历源是 **`Enumerable.OrderByDescending(raw =&gt; raw.Data.priority)`**（选择器 = 闭包 `b__17_0`，"
          "`cpp2il/out_cpp2il_dev_ilspy/LiveOpsManager.cs:272` 直读）⇒ 已按优先降序排（`LiveOpsManager.cs:303`）；"
          "② 每条事件前有**逐事件特性闸** `FeatureConfig.CheckFeature(config, raw.Data.eventId)`"
          "（`.c:158-162`；`raw.Data`=+0x10、`eventId`=`LiveOpsEventData` 的 +0x18）⇒ 已补（`LiveOpsManager.cs:309`，"
          "本机无远端配置 ⇒ 走 `EventFeatureGate` 空实现放行）。**残差**：live/blocked 的分派仍未定案 —— "
          "`.c`（槽 4 假 ⇒ 调槽 8 `RegisterEventTriggers` + 进 live）与 ILSpy（槽 4 假 ⇒ 进 blocked）**互相矛盾**，"
          "槽 4 究竟是不是 `IsActive()` 也未坐实 ⇒ 暂留 `data.enabled` 这个最小自洽解释" % TAG),
    note=" ｜[%s] 补「按 priority 降序」+「逐事件特性闸」两跳；live/blocked 判法仍属未定案（两源冲突，⛔ 别猜着改）" % TAG))

# ── G1 · 17 条 ────────────────────────────────────────────────────────────────
A = "A"
B = "B"
C = "C"

G1 = [
    # (原版类, 方法, 状态, 落点, 差异, 备注)
    ("MultiplyRewardsFunction", "Preview(ILiveOps _, IEnumerable<RewardInfo> rewardInfos)", A,
     "Shell/MultiplyRewardsFunction.cs（Preview）",
     "🆕 %s 已建（裁定 1 的宿主文件）：判据 `MultiplyRewardsFunction__Preview.c` = **全部内容一句** `Enumerable.Select(rewards, b__3_0)` ⇒ "
     "逐条过 `Scale`；`b__3_0` 的三个字面量照抄（数量 = `Math.Round(qty*multiplier, AwayFromZero)`、档位归 `Basic(0)`、instanceId=null）。"
     "⚠️ 原版基类 `EventFunctionBase` 本工程不存在 ⇒ 用 `IRewardFunction` 接口顶上（🔶 自设计，理由在文件头）。**零调用者**（本账那 4 条链都没接它）" % TAG, ""),

    ("RewardCollected", "OnInitialize()", B,
     "Shell/RewardCollected.cs（OnInitialize）",
     "🆕 %s 已建：`Signal.Register&lt;CollectRewardsSignal&gt;(OnTrigger)`（本地总线 = `RaidSignals`）+ **唯一早退** `if (anyItemType || !checkAlreadyCollected) return;` + "
     "逐类型求和（选择器 `ignoreDuplicates ? 1 : 数量`）→ `total&gt;0 ⇒ UpdateProgress(total, shouldOverride:true)`，全部照 `.c`。"
     "**残差**：谓词 `b__6_0` 要读物品的 `army`，本工程物品侧没有统一 `army` 成员 ⇒ 反射读不到时**出声并放行**（⛔ 不是判它有效）" % TAG, ""),

    ("RewardCollected", "OnReset()", A,
     "Shell/RewardCollected.cs（OnReset）",
     "🆕 %s 已建，并**就地改正原判词**：`RewardCollected__OnReset.c` **全部内容只有一句** `Signal.Unregister&lt;CollectRewardsSignal&gt;(OnTrigger)` "
     "—— 原记录的「缺 Reset(清已收集标记)」**不成立**（`.c` 里没有任何清标记动作）" % TAG,
     " ｜[%s] 原判词「清已收集标记」已作废（判据 = OnReset.c 全文）" % TAG),

    ("RewardCollected", "OnTrigger(CollectRewardsSignal obj)", B,
     "Shell/RewardCollected.cs（OnTrigger）",
     "🆕 %s 已建：`list = obj.Items` → `Where(b__8_0 未拥有)` → `if (ignoreDuplicates) 同件只算一次` → `Sum(b__8_2 数量)` → `sum!=0 ⇒ UpdateProgress(sum, false)`，逐句照 `.c`。"
     "**残差**：`.c` 里那段 **PrebuiltDeck 展开**（`b__8_3/b__8_4`：卡组拆成「每张卡 + 卡背 + 督军」）未搬 —— 本机 `RewardSpec.Id` 全指向卡/货币，没有卡组型奖励条目，遇到 `deck:` 前缀会**出声一次**" % TAG, ""),

    ("RewardCollected", "_003COnInitialize_003Eb__6_0(IIdentifiable x)", B,
     "Shell/RewardCollected.cs（ArmyPasses）",
     "🆕 %s 已建 = `anyArmy ? true : ((PlayerItem)x).army == this.army`，落成 `ArmyPasses(object)`。"
     "**残差**：本工程物品类型上没有统一的 `army` 字段/属性 ⇒ 反射取不到时**出声一次并放行**（如实记，⛔ 不静默排除整类）" % TAG, ""),

    ("RewardCollected", "_003COnInitialize_003Eb__6_1(IItemInfo<IIdentifiable> x)", A,
     "Shell/RewardCollected.cs（SumOwnedOfType 的选择器）",
     "🆕 %s 已建 = `ignoreDuplicates ? 1 : 该条目数量`（`Enumerable.Sum` 的选择器），逐句照 `.c`" % TAG, ""),

    ("RewardCollected", "_003COnTrigger_003Eb__8_0(RewardInfo i)", A,
     "Shell/RewardCollected.cs（IsNotOwnedYet）",
     "🆕 %s 已建 = `!InventoryManager.HasItem(i.Item)`（「背包里还没有」）⇒ 落成 `IsNotOwnedYet(id)`（本工程按 `ICurrency` 那一路判，其余类型视为未拥有）" % TAG, ""),

    ("RewardCollected", "_003COnTrigger_003Eb__8_2(RewardInfo x)", A,
     "Shell/RewardCollected.cs（OnTrigger 的求和）",
     "🆕 %s 已建 = `Sum(数量)`（`.c` 里那一跳 `Enumerable.Sum` 的取值器）" % TAG, ""),

    ("RewardEvent", "AppliesTo(ILiveOps evt)", A,
     "Shell/RewardEvent.cs（AppliesTo）",
     "🆕 %s 已建：按 `Data.Filter` 三档逐句照 `cpp2il/out_il2csharp/.../RewardEvent.cs:122` —— ById = `EventIds.Contains(evt.GetID())`；"
     "ByHandler = `evt.Handler.HandlerKey == LiveOpsHandlerKey`；ByType = `EventIds.Contains(类型名)`；其余 false。"
     "⚠️ 类型名那一跳原版走 `RewardEventData.GetTypeName`（同程序集 ⇒ `FullName`），我们直接比 `FullName`（账 664 的「写序列化类型名」用途与这里无关）" % TAG, ""),

    ("RewardEvent", "HandleRewardDisplay()", B,
     "Shell/RewardEvent.cs（HandleRewardDisplay）",
     "🆕 %s 已建：逐条奖励 `text = string.Format(\"+{0}\", r.Quantity)` → `new Context(text, type:0)` + `ControllerId = GetID()` → `ScheduleNotification`（**每条一个**，推迟一拍由总线做）。"
     "**残差**：原版实参里还有一个 `data`（奖励对象本身），本工程 `ToastNotifications.Context` **没有这一槽**（只有 `MissionProgress` 四件套）⇒ 传不进去，已写在注释里" % TAG, ""),

    ("RewardEvent", "IsArmyValid(CardArmy army)", A,
     "Shell/RewardEvent.cs（IsArmyValid）",
     "🆕 %s 已建：`allowList` 非空 ⇒ 只看它；否则 `blacklist` 非空 ⇒ 取反；都没有 ⇒ true（判据 `RewardEvent.cs:167`，⛔ 别加第四档）" % TAG, ""),

    ("RewardEvent", "IsTargetTo(ILiveOps evt)", A,
     "Shell/RewardEvent.cs（IsTargetTo）",
     "🆕 %s 已建：取 `Data.Function` 报出的目标事件 id（原版两支 = `EventScoringFunction.TargetEvent` / `GenericEventFunction.Args.LastOrDefault()`），"
     "**两者都不是 ⇒ false**；最后与 `evt.GetID()` 比。本工程用 `IRewardFunctionTarget` 顶那两支 ⇒ `MultiplyRewardsFunction` 落「既非 scoring 也非 generic ⇒ false」，与原版一致" % TAG, ""),

    ("RewardEventHandler", "ApplyExtraRewards(ILiveOps evt, IEnumerable<RewardInfo> rewards)", A,
     "Shell/RewardEventHandler.cs（ApplyExtraRewards）",
     "🆕 %s 已建 = **全部内容一句** `GetExtraRewardsFunction(evt)(rewards)`（判据 `RewardEventHandler.cs:23`）" % TAG, ""),

    ("RewardEventHandler", "GetExtraRewardsFunction(ILiveOps evt)", A,
     "Shell/RewardEventHandler.cs（GetExtraRewardsFunction）",
     "🆕 %s 已建，闭包三段逐句照 `:38/67/96/105`：`rewards.Concat(其它奖励事件.Where(EventIds 含目标 id).SelectMany(Type!=0 ? Function.Preview(evt,rewards) : Rewards))`。"
     "⚠️ 与原版的一处有意差异：原版取的是 handler 里那批 `RewardEvent`（它们实现 `ILiveOps`），本工程 `RewardEvent` 不派生 `LiveOpsEvent` ⇒ 改为**宿主登记的事件表**（语义等价，表空时出声一次）" % TAG, ""),

    ("RewardInfo", "CanCollect()", B,
     "Shell/RewardInfo.cs（CanCollect）",
     "🆕 %s 已建（物品层，本工程新类型）：原版 `RewardInfo__CanCollect.c` **全部内容一句** `return Item.CanBeCollected();`。"
     "我们按 `ICurrency.MaxAmount` 判（持有量已到上限 ⇒ false），其余物品类型本机没有「上限」这一档 ⇒ 可领。"
     "**残差**：原版 `CampaignPoints` 那一支（按数值上限）本工程没有对应物品类型 ⇒ 未覆盖。⚠️ 今天**零生产调用点**（领取链没接）" % TAG, ""),

    ("RewardService", "CollectItemReward(RewardInfo reward)", C,
     "CardPresentation/Shell/Inventory.cs:1151（InventoryManager.Add，原语已存在）",
     "🆕 %s 物品层 `RewardInfo` **已建**（`Shell/RewardInfo.cs`）⇒ 原记录「无输入类型」这一条**不再成立**；剩下的是这条分派器本身 —— "
     "它该落在 `Shell/Inventory.cs:1950` 的 `RewardService` 类里，**那个文件不在本批白名单**（另有写手）⇒ 未动" % TAG,
     " ｜[%s] 物品层已就位，分派器等 Inventory.cs 的写手" % TAG),

    ("RewardService", "CollectSingleReward(RewardInfo reward)", C,
     "（货币半已在 CardPresentation/Shell/Inventory.cs:1960 RewardService.CollectCurrencyReward；物品半等 G1）",
     "🆕 %s 同上一行：`RewardInfo` 物品层**已建**（输入类型已存在），分派器（按 `Item` 类型 → `CollectCurrencyReward` / `CollectItemReward`）"
     "要落在 `Shell/Inventory.cs:1950` 的 `RewardService`，**不在本批白名单** ⇒ 未动" % TAG,
     " ｜[%s] 输入类型已就位，分派器等 Inventory.cs 的写手" % TAG),
]

for cls, meth, st, lead, diff, note in G1:
    OUTROWS.append(put(cls, meth, status=st, lead=lead, diff=diff,
                       note=(note or " ｜[%s] G1 已建（裁定 1 的宿主文件）" % TAG)))

keys = [(r[1], r[2]) for r in OUTROWS]
assert len(keys) == len(set(keys)), "补丁内重复键"
lines = ["\t".join(HDR)] + ["\t".join(r) for r in OUTROWS]
io.open(OUT, "wb").write(("\n".join(lines) + "\n").encode("utf-8"))

from collections import Counter
print("wrote", OUT, "rows", len(OUTROWS), dict(Counter(r[5] for r in OUTROWS)))
