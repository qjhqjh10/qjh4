#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""生成 B91_W7 账补丁（日常_奖励 域）。

约定（与 merge_ledger_parts.py 的消费者口径逐字一致）：
- 主表与补丁都用**裸 `\\t` 切列**（merge 工具就是 `l.split("\\t")`）；
  ⛔ 不用 csv+escapechar —— 主表字段里有 Windows 反斜杠路径（`Assets\\CardPresentation\\…`），
  escapechar 会把 `\\C` 吃掉，读出来的列就变了。
- 写盘 `io.open(p,'wb')` + LF；⛔ 不改主表。
- 只放「改过的行」，11 列；`(原版类, 方法)` 必须唯一命中，>1 处直接报错退出。
"""
import io
import sys

LEDGER = r"d:/4/Unity/资料/完全复刻/账/日常_奖励.tsv"
OUT = r"d:/4/Unity/资料/完全复刻/账/_parts/B91_W7.tsv"

TAG = "B91_W7 2026-10-28"

# ── 主表读取（裸 tab 切；与 merge 工具同方言） ─────────────────────────────────
raw = io.open(LEDGER, "rb").read()
assert raw.count(b"\r\n") == 0, "主表不是纯 LF，先核实"
rows = [l.split("\t") for l in raw.decode("utf-8-sig").split("\n") if l.strip()]
HDR = rows[0]
assert len(HDR) == 11, len(HDR)
BODY = rows[1:]

IDX = {}
for i, r in enumerate(BODY):
    assert len(r) == 11, (i, len(r))
    IDX.setdefault((r[1], r[2]), []).append(i)


def get(cls, meth):
    hit = IDX.get((cls, meth), [])
    if len(hit) != 1:
        raise SystemExit("HIT %d for (%s, %s)" % (len(hit), cls, meth))
    return list(BODY[hit[0]])


# ── 编辑工具 ────────────────────────────────────────────────────────────────
def app(col, text):
    """追加（保留原记录）。"""
    return (col + text) if col.strip() else text


def put(cls, meth, **kw):
    """kw 可含 status/need/lead/diff/note（= 第 5/4/6/7/10 列）。"""
    r = get(cls, meth)
    if "status" in kw: r[5] = kw["status"]
    if "need" in kw:   r[4] = kw["need"]
    if "lead" in kw:   r[6] = kw["lead"]
    if "diff" in kw:   r[7] = kw["diff"]
    if "note" in kw:   r[10] = app(r[10], kw["note"])
    assert len(r) == 11
    for c in r:
        assert "\t" not in c and "\n" not in c and "\r" not in c, c[:80]
    return r


OUTROWS = []
OUTROWS.append(put(
    "FastModeBaseEvent", "GetMatchmakingFilter(int currentElo, int tryCount, int gamesLost)",
    status="B",
    need=("快模式活动基类（阵营战绩 / 里程碑）｜🔴 B91_W7 2026-10-28 现读订正（旧判据写的是 `gamesLost % arr.Length`，**错**）："
          "`delta = arr[tryCount % 3]`（实参 = **tryCount**、模**常量 3**）· `min = max(0, currentElo - delta)` · `max = currentElo + delta` · "
          "`if (tryCount == 2 && Data is IRankedData && LegendaryElo <= currentElo) { max = MaxEloCap; min = Min(min, LegendaryElo) }` · "
          "末段 `GetSqlFilter(max, min, group, isRanked, blacklist, gamesLost)`"),
    diff=("🔴 2026-10-28 B91_W7 **就地改正**（原记录「`delta=arr[gamesLost%len]`」「缺 gamesLost==2 收紧上界那一跳」**两处都错**；"
          "判据 = `decomp_generic_trial/out/FastModeBaseEvent_object__object___GetMatchmakingFilter__1a10120.c` "
          "＋同形状非泛型 `decomp_full/Everguild.LiveOps.RankedV3Event__GetMatchmakingFilter.c` ＋ cpp2il `FastModeBaseEvent.cs:856-944`）："
          "① 档位下标 = **`tryCount % 3`**（除以 3 的魔数 + `imul` 乘回 3；IL2CPP 实参序 currentElo/tryCount/gamesLost，"
          "`GetSqlFilter` 的第 6 参才叫 gamesLost）；② `==2` 那道闸同样在 **tryCount** 上；"
          "③ 「收紧上界」的实参**读得出** = `LegendaryElo`(+0x98) / `maxEloCap`(+0x9c)（与 `RankedBaseData` 字段序逐一对上），"
          "已按 `Data is IRankedData` 落地（`FastModeBaseEvent.cs:513-514`）。⇒ 算式现读全对。"
          "**残差**：`GetSqlFilter` 的联盟 / 黑名单两段未搬（本机没有好友/联盟/作弊名单那几条线）＋ P2P 无匹配过滤层 ⇒ **零消费方**"
          "（`FastModeBaseEvent.cs:499` 算对了但用不上）"),
    note=" ｜[%s] 判据要点+差异两列已按 `.c` 就地改正（铁律 5，⛔ 别照旧判据把代码改回去）" % TAG))

OUTROWS.append(put(
    "FastModeBaseEvent", "Expire(Action onComplete)",
    status="B",
    need=("快模式活动基类（阵营战绩 / 里程碑）｜🔴 %s 坐实：`<虚方法 0x758>` = **`CurrentScore`**"
          "（`decomp_full/RankedFastMode__GetCurrentRankedDivision.c:7` 把它的返回值直接当 points 传给 `GetDivisionByPoints`；"
          "`FastModeBaseEvent.__c__…__TryCollect_b__0.c` = `virt() < r.milestone`）⇒ 第二闸 = `CurrentScore == 0`，⛔ 不是「能不能开」；"
          "`SaveGame+0x30` = `joinState`（`LiveOpsEventPersistence` 6 字段打头 + `PlayEventSaveGame` 前 5 个，逐字节对上）" % TAG),
    diff=app(get("FastModeBaseEvent", "Expire(Action onComplete)")[7],
             " ｜B91_W7 2026-10-28：闸已由旧推断 `EventState != Active` 改成 **`CurrentScore == 0`**（`FastModeBaseEvent.cs:361`）。"
             "**残差** = 「组件 21 那扇窗」未搬（我们按类不按组件引用 ⇒ 本地恒 `HasComponent(0x15)` 为空）"),
    note=" ｜[%s] 第二道闸按 `.c` 坐实（原标「推断」已作废）" % TAG))

OUTROWS.append(put(
    "FastModeBaseEvent", "OnMatchProcessSuccess(OnlineMatchResult result)",
    status="B",
    diff=app(get("FastModeBaseEvent", "OnMatchProcessSuccess(OnlineMatchResult result)")[7],
             " ｜B91_W7 2026-10-28：**已补本地榜计分那一跳** `LeaderboardData.LocalScore(Data.LeaderboardKey, result.newRating, SetValue)`"
             "（原版同刻 `LeaderboardManager.LocalScore(key, NewRating, mode=2)`；落点 `FastModeBaseEvent.cs:336`；"
             "接收端 `Shell/LeaderboardData.cs:292` 早就实现了、此前零生产调用点）。"
             "**残差**：`SaveGame.armyStats` 拷贝那一跳（我们的 `PlayEventSaveGame` 没有该字段）＋ 云脚本 0x180 上报（P3）"),
    note=" ｜[%s] 补 `LocalScore` 一跳" % TAG))

OUTROWS.append(put(
    "BaseParentEventWindow", "Open()",
    status="A",
    diff=app(get("BaseParentEventWindow", "Open()")[7],
             " ｜B91_W7 2026-10-28：**末段已补** `MainMenuRuntime.SetNavToggle(NavigationPanelToggleType.Event)`"
             "（落点 `ParentEventWindow.cs:579`；本仓 `SetNavToggle` = `GetToggle(type)` → `isOn==false ⇒ SetIsOnWithoutNotify(true)`，"
             "与原版 `SetToggle(5)` **同语义且幂等**，见 `MainMenuRuntime.cs:805-807`）。"
             "**唯一差异**：原版 nav 实例取不到会抛，我们无实例时静默 no-op（全工程同口径，不是失败）"),
    note=" ｜[%s] 末段导航切 Event 已补 ⇒ 本行升 A" % TAG))

OUTROWS.append(put(
    "LiveOpsHandler", "RefreshHandlerLocally()",
    status="A",
    need=("HandleBeforeRefresh(); await <虚调用，单参 = 字段 +0x60> ｜🔴 %s 坐实（`LiveOpsHandler._RefreshHandlerLocally_d__27_object___MoveNext.c:44`）："
          "`(**(code**)(*this + 0x2a8))(this, plVar1[0xc], …)` —— 虚槽 `0x2a8` = `RefreshHandler(LiveopsHandlerData)`，"
          "第 1 实参读的就是 **this+0x60**" % TAG),
    diff=("✅ 等价（原判词「两读法只差一次取值，**未坐实**」**已作废**）：`.c` 现读证明那次虚调用只传一个实参、读的就是 `this+0x60`；"
          "我们传的 `handlerCache` 就是同一个槽（`LiveOpsHandler.cs:543`）。残余只有「+0x60 的字段名是不是 `handlerCache`」这一层命名对应"
          "（该类的字段偏移在 dump 里对不上，⛔ 不再据此标 B）"),
    note=" ｜[%s] 判据坐实 ⇒ 升 A" % TAG))

OUTROWS.append(put(
    "LiveOpsEventWindow", "OnRefresh()",
    status="A",
    need=("活动窗基类（标题/帮助/进度/领奖）｜🔴 %s 判据订正：真判据件 = "
          "`decomp_generic_trial/out/LiveOpsEventWindow_object___OnRefresh__1016a30.c` ＋ `cpp2il/out_cpp2il_dev_ilspy/LiveOpsEventWindow.cs:71-200`"
          "（原判词「泛型类 decomp_full 无产物」已作废）" % TAG),
    diff=app(get("LiveOpsEventWindow", "OnRefresh()")[7],
             " ｜B91_W7 2026-10-28 **关键订正**：闭窗/聚焦那一支**只在 `GetEvent` 成功之后**才走；"
             "**取不到在原版是 throw**（NRE / 类型转换失败）⇒「取不到 ⇒ `Close()` + 出声」是**有意的差异**（⛔ 别当成原版行为）"),
    note=" ｜[%s] 判据订正 ⇒ 升 A（⚠️ 休眠：13 份活动 SO 全过期、`EventId` 恒空）" % TAG))

# ── 裁定 1 · G1 奖励事件层 17 条：钉宿主、不实现 ──────────────────────────────
G1 = [
    ("MultiplyRewardsFunction", "Preview(ILiveOps _, IEnumerable<RewardInfo> rewardInfos)", "Shell/MultiplyRewardsFunction.cs"),
    ("RewardCollected", "OnInitialize()", "Shell/RewardCollected.cs"),
    ("RewardCollected", "OnReset()", "Shell/RewardCollected.cs"),
    ("RewardCollected", "OnTrigger(CollectRewardsSignal obj)", "Shell/RewardCollected.cs"),
    ("RewardCollected", "_003COnInitialize_003Eb__6_0(IIdentifiable x)", "Shell/RewardCollected.cs"),
    ("RewardCollected", "_003COnInitialize_003Eb__6_1(IItemInfo<IIdentifiable> x)", "Shell/RewardCollected.cs"),
    ("RewardCollected", "_003COnTrigger_003Eb__8_0(RewardInfo i)", "Shell/RewardCollected.cs"),
    ("RewardCollected", "_003COnTrigger_003Eb__8_2(RewardInfo x)", "Shell/RewardCollected.cs"),
    ("RewardEvent", "AppliesTo(ILiveOps evt)", "Shell/RewardEvent.cs"),
    ("RewardEvent", "HandleRewardDisplay()", "Shell/RewardEvent.cs"),
    ("RewardEvent", "IsArmyValid(CardArmy army)", "Shell/RewardEvent.cs"),
    ("RewardEvent", "IsTargetTo(ILiveOps evt)", "Shell/RewardEvent.cs"),
    ("RewardEventHandler", "ApplyExtraRewards(ILiveOps evt, IEnumerable<RewardInfo> rewards)", "Shell/RewardEventHandler.cs"),
    ("RewardEventHandler", "GetExtraRewardsFunction(ILiveOps evt)", "Shell/RewardEventHandler.cs"),
    ("RewardInfo", "CanCollect()", "Shell/RewardInfo.cs"),
    ("RewardService", "CollectItemReward(RewardInfo reward)", "（类已存在）Shell/Inventory.cs:1950 —— 缺口在物品层 Shell/RewardInfo.cs"),
    ("RewardService", "CollectSingleReward(RewardInfo reward)", "（类已存在）Shell/Inventory.cs:1950 —— 缺口在物品层 Shell/RewardInfo.cs"),
]
for cls, meth, host in G1:
    cur = get(cls, meth)
    lead = cur[6]
    if lead.strip() in ("", "无", "—"):
        lead = "（未建）" + host
    else:
        lead = app(lead, " ｜宿主待建：" + host)
    OUTROWS.append(put(
        cls, meth, status="C", lead=lead,
        need=app(cur[4], " ｜【裁定 1 · %s】宿主 = **新建独立文件 `%s`**（类名 == 文件名，否则 Unity 建不出脚本资产）；"
                          "⛔ 不塞进已存在的 `Shell/ChallengeEngine.cs`（那是挑战计数口）。实现留待下一棒，本行只钉宿主与口径" % (TAG, host)),
        note=" ｜[%s] 裁定 1：宿主按原版类型名独立成文件 `%s`；实现留下一棒" % (TAG, host)))

# ── 裁定 2 · LiveOpsAssetUtility 12 条 ──────────────────────────────────────
# 例外两条（B89 实测已实现本体）⇒ A
OUTROWS.append(put(
    "LiveOpsAssetUtility", "GetAssetReference(LiveOpsEventData liveops, EventAssetReferenceType type)",
    status="A",
    diff=("✅ 等价（裁定 2 的例外：本体已实现，不是空口）：`Shell/LiveOpsLocalCatalog.cs:1164` 已按 `.c` 逐句落地 —— 按 key 查 `EventAssets`，"
          "查不到 LogWarning 后返 null；原版的 `RuntimeKeyIsValid` 校验用「`SubObjectName` 非空」代理"
          "（🔶 自己设计（非原版）：本机没有 Addressables runtime key · 为什么必须自己定：装载层不存在 · 调度台 %s 裁定）" % TAG),
    note=" ｜[%s] 裁定 2 例外：本体已实现 ⇒ A" % TAG))

OUTROWS.append(put(
    "LiveOpsAssetUtility", "GetEventLabel(LiveOpsEventData liveops, EventLabelReferenceType type)",
    status="A",
    diff=("✅ 等价（裁定 2 的例外：本体已实现）：`Shell/LiveOpsLocalCatalog.cs:1152` 按 key 查 `EventLabel`，取不到 ⇒ LogWarning 后返 null，"
          "与原版 `Everguild.LiveOps.LiveOpsAssetUtility__GetEventLabel.c:38` 一致"),
    note=" ｜[%s] 裁定 2 例外：本体已实现 ⇒ A" % TAG))

D2 = [
    ("LoadAsset<T>(ILiveOps liveops, EventAssetReferenceType type)",
     "接入条件：需要 ① Addressables 装载层（AssetReference → RuntimeKey → LoadAssetAsync&lt;T&gt;）② 至少一个按组件/资产引用装 UI 的活动窗。两条都在之前行为恒空"),
    ("LoadAsset<T>(LiveOpsEventData liveops, EventAssetReferenceType type)",
     "接入条件：同上（data 侧重载）。我们的事件窗全部由代码建树，不按资产引用 loading ⇒ 无输入类型、无消费者"),
    ("LoadComponent<T>(ILiveOps liveops, EventComponentReferenceType type)",
     "接入条件：需要 Addressables 组件装载 + 消费者。本仓最接近的等价物是注入口 `LiveOpsContainers.Load`（默认无 provider ⇒ 恒 null），已覆盖「取不到就出声」那一层"),
    ("LoadComponent<T>(LiveOpsEventData liveops, EventComponentReferenceType type)",
     "接入条件：同上（data 侧）。等价物 = `LiveOpsContainers.LoadForData`（`Shell/LiveOpsEvent.cs:1257` 已接、默认无 provider）"),
    ("ReleaseEventAssets(ILiveOps liveOps)",
     "接入条件：只有先有「装载」才有「释放」。本仓无装载层 ⇒ 无引用可释放、行为恒空（与账 524 `MissionsConfig.CleanUp` 同一口径）"),
    ("TryLoadComponent<T>(ILiveOps liveops, EventComponentReferenceType type, out T component)",
     "接入条件：同 `LoadComponent<T>`，另需消费者。行为 = 装载失败返 false，无装载层时恒 false ⇒ 恒空"),
    ("TryLoadComponent<T>(LiveOpsEventData liveops, EventComponentReferenceType type, out T component)",
     "接入条件：同上（data 侧）"),
    ("Unload(AssetReference assetReference)",
     "接入条件：需要 Addressables 的 `AssetReference.ReleaseAsset()` 与一个真装载出来的句柄。本仓没有该类型实例 ⇒ 无输入"),
    ("Unload<T>(ILiveOps liveops, T key)",
     "接入条件：同上（按 key 查已装载表再释放）；本仓无「已装载表」"),
    ("Unload<T>(LiveOpsEventData liveops, T key)",
     "接入条件：同上（data 侧）"),
]
for meth, cond in D2:
    cur = get("LiveOpsAssetUtility", meth)
    OUTROWS.append(put(
        "LiveOpsAssetUtility", meth, status="D",
        diff=("🔶 机制不同（架构性不适用）：本机没有 Addressables 装载层、零消费者 ⇒ 行为恒空。"
              + cond.replace("接入条件：", "接入条件：") +
              "。**可逆**：将来若引入 Addressables 装载层并出现第一个消费者，本行直接改 B 按原版实现"),
        note=app(cur[10], " ｜[%s] 裁定 2（d）：架构性不适用判 D；可逆（接入条件已写进差异列）" % TAG)))

# ── 写盘 ───────────────────────────────────────────────────────────────────
keys = [(r[1], r[2]) for r in OUTROWS]
assert len(keys) == len(set(keys)), "补丁内重复键"
lines = ["\t".join(HDR)] + ["\t".join(r) for r in OUTROWS]
text = "\n".join(lines) + "\n"
data = text.encode("utf-8")
io.open(OUT, "wb").write(data)

print("wrote", OUT)
print("rows", len(OUTROWS))
from collections import Counter
print("status:", dict(Counter(r[5] for r in OUTROWS)))
print("cols ok:", all(len(r) == 11 for r in OUTROWS))
print("dup keys:", len(keys) - len(set(keys)))
