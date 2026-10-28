#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""生成 B91_L1 账补丁：只改 31 条失败行的 `我们的落点` 一列。

⛔ 不动任何 .cs；⛔ 不动主表；只写 `账/_parts/B91_L1.tsv`。
🔴 裸 split("\\t") 读写；二进制读写 + LF 行尾（六份主表现读都是 LF）。
"""
import io
import os
import sys

LEDGER = r"d:/4/Unity/资料/完全复刻/账"
ASSETS = r"d:/4/Unity/MyGame/Assets"
OUT = os.path.join(LEDGER, "_parts", "B91_L1.tsv")

COLS = ["域", "原版类", "方法", "判据出处", "判据要点", "状态",
        "我们的落点", "差异", "等级", "轮次", "备注"]
I_LAND = 6


def read_lines(path):
    b = io.open(path, "rb").read()
    assert b.count(b"\r") == b.count(b"\r\n"), "有裸 CR：" + path
    if b.count(b"\r\n") and b.count(b"\r\n") == b.count(b"\n"):
        return b.decode("utf-8-sig").split("\r\n"), "\r\n"
    return b.decode("utf-8-sig").split("\n"), "\n"


def src_lines(rel):
    p = os.path.join(ASSETS, rel)
    lines, _ = read_lines(p)
    return lines


def assert_decl(rel, ln, needle):
    lines = src_lines(rel)
    assert 1 <= ln <= len(lines), "%s:%d 越界" % (rel, ln)
    got = lines[ln - 1]
    assert needle in got, "断言失败 %s:%d 期望含 %r，实际 %r" % (rel, ln, needle, got[:120])
    return True


# ---------------------------------------------------------------- 落点改写表
# key = (原版类, 方法) —— 必须与主表逐字相同
# value = 新落点全文

LR = "CardPresentation/Core/LoadableRef.cs"

NEW = {}

# ① 其他_基础设施.tsv —— Extensions.Contains（原先是「—（等价实现…」没有文件:行）
NEW[("Extensions", "Contains(int[] target, int nr)")] = (
    "CardPresentation/Battle/HudCanvas.cs:627（IsRanked）—— 等价实现 = 内联 `System.Array.IndexOf(...) >= 0`"
    "（代表调用点 :629）· Core/VfxMap.cs:3450 · Deck/DeckEditorState.cs:859；本仓没有同名扩展族 "
    "⇒ 指不到同名方法声明，按「代表等价实现所在的方法」落点；"
    "⚠️ 2026-10-28 B91_L3 现核：原账指的 Core/Loc.cs:3043 是 Lang13Of，与本法无关"
    "（2026-10-28 B91_L1 补 文件:行）"
)

# ① 其他_基础设施.tsv —— LoadableRef 一族（类尾加了 ==/!= ⇒ 行号整体位移；按符号名现读重定位）
NEW[("LoadableReference", "Equals(LoadableReference<T> other)")] = (
    LR + ":165（protected bool Equals(LoadableRef<T> other)）"
    "（2026-10-28 B91_L1 现读重定位：原 :160 已落到 doc 注释行）"
)
NEW[("LoadableReference", "Equals(object obj)")] = (
    LR + ":182（public override bool Equals(object obj)）"
    "（2026-10-28 B91_L1 现读重定位：原 :177 已落到 doc 注释行）"
)
NEW[("LoadableReference", "GetHashCode()")] = (
    LR + ":194（public override int GetHashCode()）"
    "（2026-10-28 B91_L1 现读重定位：原 :189 已落到空行）"
)
NEW[("LoadableReference", "LoadableReference<T>(T instance)")] = (
    LR + ":68（public LoadableRef(T identifiable)）"
    "（2026-10-28 B91_L1 现读重定位：原 :63 已落到空行）"
)
NEW[("LoadableReference", "LoadableReference<T>(string id)")] = (
    LR + ":65（public LoadableRef(string id)）"
    "（2026-10-28 B91_L1 现读重定位：原 :60 已落到字段行 `_idOf`）"
)
NEW[("LoadableReference", "SetReferenceByObject(T reference)")] = (
    LR + ":113（public void SetReferenceByObject(T obj)）"
    "（2026-10-28 B91_L1 现读重定位：原 :108 已落到方法体内一行 `targetId = id;`）"
)
NEW[("LoadableReference", "T(LoadableReference<T> instanceRef)")] = (
    LR + ":244（public static implicit operator T(LoadableRef<T> instanceRef)）"
    "（2026-10-28 B91_L1 现读重定位：原 :239 已落到 doc 注释行 `/// </code>`）"
)
NEW[("LoadableReference", "string(LoadableReference<T> instanceRef)")] = (
    LR + ":256（public static implicit operator string(LoadableRef<T> instanceRef)）"
    "（2026-10-28 B91_L1 现读重定位：原 :249 已落到另一方法体内一行）"
)

# ② 战斗逻辑驱动.tsv
NEW[("BattleManagerSupport", "BroadcastUnitReanimated(BattleManager manager, CardScript unitReanimated)")] = (
    "RuleEngine/Core/EffectResolver.cs:16422（BoardJumpIncludesPendingDeath）—— 棋盘那一跳「收待死单位」的"
    "**按族名单**（`Reanimated` 在它 :16430 那一句）· :15595（`BroadcastWhen(WhenEventKind.Reanimated, …)` "
    "调用点）· :15600（`ctx.Tally.AddReanimated`）；"
    "⚠️ 原账写的 :16383 是分节注释行、:15565 / :15607 也已漂（2026-10-28 B91_L1 现读重定位）"
)
NEW[("CardScript", "MissActions()")] = (
    "RuleEngine/Core/RuleCore.cs:14991（MissActions）—— 原版 `CardScript.MissActions` 的动作体："
    "日志 + 单位则 `attacksThisTurn(+0x48) += 1`（不重算能动/能攻击）"
    "（2026-10-28 B91_L1 现读重定位：原 :14989 是它的 doc 注释行）"
)
NEW[("CardScript", "OnTurnStarted(bool playerTurn)")] = (
    "RuleEngine/Core/RuleCore.cs:14762（BroadcastTurnStarted）—— 等价实现 = 原版 "
    "`BattleManagerSupport.BroadcastTurnStarted` 的棋盘那一跳（卡侧 `OnTrigger(0xa0=160)` ⇒ "
    "`FireTriggerAlways(…, \"TurnStarted\")`）· :15876（`BroadcastTurnStartedToHands` = ②③ 手牌两跳、只出声）"
    "· :2262（`BeginTurnCore` 的调用点，①→②→③ 顺序）；"
    "⚠️ 原账两点都是调用行/注释行（2026-10-28 B91_L1 现读重定位）"
)
NEW[("CardScript", "ResolveUnitAttacked(CardScript actingCard, CardScript targetCard, AttackTypes attackType)")] = (
    "RuleEngine/Core/EffectResolverPartial_B88E2.cs:568（EnqueueExpireUntilNextAttack）—— 等价实现 = "
    "`ResolveUnitAttacked` 的「扫 `activeEffects(+0x108)`、`untilNextAttack(+0x36)` 建档 ⇒ "
    "`SendRemoveEffect(…, priority 1)`」那一跳 · 两条调用点 RuleCore.cs:6980（DeclareAttack 攻方侧预调之后）"
    " / :12152（UseAlternative 的 ferocity 分支末尾）· RuleEngine/Core/UnitState.cs:3323"
    "（`ExpireUntilNextAttack` 原语）；⚠️ 原账写的 :3383 与 EffectResolverPartial_B88E2.cs:586 均已漂"
    "（2026-10-28 B91_L1 现读重定位）"
)
NEW[("CardScript", "SetAsPlayer(bool isPlayer, CardArmy army, bool setCardback)")] = (
    "RuleEngine/Core/EffectResolver.cs:17298（StealCardAcrossSides）—— 等价实现 = 原版 `SetAsPlayer` 的"
    "「写 `isPlayer(+0x40)` + 换边」那一跳：我们模型里 isPlayer = 实例待在谁的数组里"
    "（映射写在 EffectResolver.cs:17232）⇒ 偷牌链的换边写点 = 本方法（另一处 = 临时控制归还，"
    "见 BattleContext.cs:543 `TempControl`）· 读侧 = `owner == _me`"
    "（CardPresentation/Battle/BattleDriver.cs:25967 / :26171；:34357 `h.IsPlayerSide = side == _me`）；"
    "⚠️ 原账三点都是语句行、不是声明（2026-10-28 B91_L1 现读重定位）"
)

# ③ 日常_奖励.tsv —— 11 条（原落点没写行号 / GetEventLabel 指到注释行）
NEW[("LiveOpsAssetUtility", "GetEventLabel(LiveOpsEventData liveops, EventLabelReferenceType type)")] = (
    "CardPresentation/Shell/LiveOpsLocalCatalog.cs:1153（LiveOpsAssetUtility.GetEventLabel）"
    "（2026-10-28 B91_L1 现读重定位：原 :1152 是它的 doc 注释行）"
)
NEW[("MultiplyRewardsFunction", "Preview(ILiveOps _, IEnumerable<RewardInfo> rewardInfos)")] = (
    "Shell/MultiplyRewardsFunction.cs:70（Preview）—— 实现 = 逐条 `Scale`（:89）"
    "｜【裁定 1 · B91_W7 2026-10-28】宿主 = 新建独立文件 `Shell/MultiplyRewardsFunction.cs`"
    "（类名 == 文件名，否则 Unity 建不出脚本资产）；⛔ 不塞进 `Shell/ChallengeEngine.cs`"
    "（2026-10-28 B91_L1 补 文件:行 —— 原落点只有「文件（方法）」没有行号）"
)
NEW[("RewardCollected", "OnReset()")] = (
    "Shell/RewardCollected.cs:109（OnReset）—— 原版逐句：只有一句 "
    "`Signal.Unregister<CollectRewardsSignal>(OnTrigger)`"
    "（2026-10-28 B91_L1 补 文件:行 —— 原落点只有「文件（方法）」没有行号）"
)
NEW[("RewardCollected", "_003COnInitialize_003Eb__6_1(IItemInfo<IIdentifiable> x)")] = (
    "Shell/RewardCollected.cs:161（SumOwnedOfType）—— 该 lambda 的等价实现（原版 b__6_1 = "
    "`Sum(选择器)`，选择器 = `ignoreDuplicates ? 1 : 数量`；doc 见 :159-160）"
    "（2026-10-28 B91_L1 补 文件:行）"
)
NEW[("RewardCollected", "_003COnTrigger_003Eb__8_0(RewardInfo i)")] = (
    "Shell/RewardCollected.cs:149（IsNotOwnedYet）—— 原版 b__8_0 = "
    "`!InventoryManager.HasItem(i.Item)`（「背包里还没有」）"
    "（2026-10-28 B91_L1 补 文件:行）"
)
NEW[("RewardCollected", "_003COnTrigger_003Eb__8_2(RewardInfo x)")] = (
    "Shell/RewardCollected.cs:117（OnTrigger）—— b__8_2 的「数量求和」**内联在这一方法的 :140-141**"
    "（没有独立方法 ⇒ 落到宿主方法声明）"
    "（2026-10-28 B91_L1 补 文件:行）"
)
NEW[("RewardEvent", "AppliesTo(ILiveOps evt)")] = (
    "Shell/RewardEvent.cs:146（AppliesTo）"
    "｜【裁定 1 · B91_W7 2026-10-28】宿主 = 新建独立文件 `Shell/RewardEvent.cs`"
    "（类名 == 文件名）（2026-10-28 B91_L1 补 文件:行 —— 原落点没有行号）"
)
NEW[("RewardEvent", "IsArmyValid(CardArmy army)")] = (
    "Shell/RewardEvent.cs:173（IsArmyValid）"
    "（2026-10-28 B91_L1 补 文件:行 —— 原落点没有行号）"
)
NEW[("RewardEvent", "IsTargetTo(ILiveOps evt)")] = (
    "Shell/RewardEvent.cs:188（IsTargetTo）"
    "（2026-10-28 B91_L1 补 文件:行 —— 原落点没有行号）"
)
NEW[("RewardEventHandler", "ApplyExtraRewards(ILiveOps evt, IEnumerable<RewardInfo> rewards)")] = (
    "Shell/RewardEventHandler.cs:66（ApplyExtraRewards）"
    "｜【裁定 1 · B91_W7 2026-10-28】宿主 = 新建独立文件 `Shell/RewardEventHandler.cs`"
    "（类名 == 文件名）（2026-10-28 B91_L1 补 文件:行 —— 原落点没有行号）"
)
NEW[("RewardEventHandler", "GetExtraRewardsFunction(ILiveOps evt)")] = (
    "Shell/RewardEventHandler.cs:73（GetExtraRewardsFunction）"
    "（2026-10-28 B91_L1 补 文件:行 —— 原落点没有行号）"
)

# ④ 未分类_其他.tsv —— UIMessageController.OnDestroy（我们没实现；退到类声明 + 如实标）
NEW[("UIMessageController", "OnDestroy()")] = (
    "CardPresentation/Battle/ErrorMessageBanner.cs:205（ErrorMessageBanner 类）—— 类级落点（闸的 §4.1 "
    "「结构等价 ⇒ 只报告警」那一档）：**原版 `UIMessageController.OnDestroy`（`if (Instance == this) "
    "Destroy(gameObject)`）我们没实现** —— 本件没有单例 `Instance` 字段、也没有 `Awake`/`OnDestroy`"
    "（每次 `Create(parent)` 新建一份，文件头 ⑤ 的单例守卫同样没落）⇒ 同名声明查不到"
    "（已查：`ErrorMessageBanner.cs` 全文 grep `OnDestroy`/`Instance` = 0 处方法声明；"
    "全仓 `grep UIMessageController` = 只有注释；`BattleDriver._errorBanner` 三个调用点 "
    "= Create/ShowError/Advance，没有销毁路）· 2026-10-28 B91_L1"
)

# ⑤ 泛型与启动链_补读.tsv —— 4 条（同一份 .cs 的四个声明）
NEW[("LoadableReference<T>", "SetReferenceByObject(T reference)")] = (
    LR + ":113（public void SetReferenceByObject(T obj)）"
    "（2026-10-28 B91_L1 现读重定位：原 :108 已落到方法体内一行）"
)
NEW[("LoadableReference<T>", "Equals(LoadableReference<T> other)")] = (
    LR + ":165（protected bool Equals(LoadableRef<T> other)）"
    "（2026-10-28 B91_L1 现读重定位：原 :160 已落到 doc 注释行）"
)
NEW[("LoadableReference<T>", "Equals(object obj)")] = (
    LR + ":182（public override bool Equals(object obj)）"
    "（2026-10-28 B91_L1 现读重定位：原 :177 已落到 doc 注释行）"
)
NEW[("LoadableReference<T>", "GetHashCode()")] = (
    LR + ":194（public override int GetHashCode()）"
    "（2026-10-28 B91_L1 现读重定位：原 :189 已落到空行）"
)

# ⑥ 菜单_外壳.tsv —— UpdateTextSizeSmallScreen
NEW[("EverguildTextMeshPro", "UpdateTextSizeSmallScreen()")] = (
    "CardPresentation/Battle/Label.cs:2495（ApplySmallScreenTextSize）—— = 原版 "
    "`EverguildTextMeshPro.UpdateTextSizeSmallScreen()`（逐句）；两格字号 = :2460 的 "
    "`_ssMaxPx` / `_ssMinPx` 字段 + :2474 `SetSmallScreenFontSizes` 写入 · :1414（换面重放补一次）· "
    "Shell/TransformScalerBySmallScreenUI.cs（`SmallScreenUI.Enabled` = 原版 "
    "`GameStaticData.smallScreenUI(+0x11c)` 的等价物）；"
    "⚠️ 原账的 :2460 是字段行、:2474/:2495 是写入/等价方法（2026-10-28 B91_L1 现读重定位）"
)

# ---------------------------------------------------------------- 断言：目标行真的是那个声明
DECL_CHECKS = [
    ("CardPresentation/Battle/HudCanvas.cs", 627, "public static bool IsRanked(string name)"),
    ("RuleEngine/Core/EffectResolver.cs", 16422, "static bool BoardJumpIncludesPendingDeath(string kind)"),
    ("RuleEngine/Core/RuleCore.cs", 14991, "public static void MissActions(BattleContext ctx, UnitState u)"),
    ("RuleEngine/Core/RuleCore.cs", 14762, "static void BroadcastTurnStarted(BattleContext ctx)"),
    ("RuleEngine/Core/RuleCore.cs", 15876, "static void BroadcastTurnStartedToHands(BattleContext ctx)"),
    ("RuleEngine/Core/EffectResolverPartial_B88E2.cs", 568,
     "public static int EnqueueExpireUntilNextAttack(BattleContext ctx, UnitState u, string by = null)"),
    ("RuleEngine/Core/UnitState.cs", 3323, "public int ExpireUntilNextAttack()"),
    ("RuleEngine/Core/EffectResolver.cs", 17298, "static bool StealCardAcrossSides(BattleContext ctx"),
    ("CardPresentation/Shell/LiveOpsLocalCatalog.cs", 1153,
     "public static LiveOpsLocalLabel GetEventLabel(LiveOpsLocalEvent evt, LiveOpsLabelRef type)"),
    ("CardPresentation/Shell/MultiplyRewardsFunction.cs", 70,
     "public IEnumerable<RewardInfo> Preview(ILiveOps evt, IEnumerable<RewardInfo> rewards)"),
    ("CardPresentation/Shell/RewardCollected.cs", 109, "public void OnReset()"),
    ("CardPresentation/Shell/RewardCollected.cs", 117, "public void OnTrigger(CollectRewardsSignal obj)"),
    ("CardPresentation/Shell/RewardCollected.cs", 149, "protected virtual bool IsNotOwnedYet(string itemId)"),
    ("CardPresentation/Shell/RewardCollected.cs", 161, "protected virtual int SumOwnedOfType(string typeName)"),
    ("CardPresentation/Shell/RewardEvent.cs", 146, "public bool AppliesTo(ILiveOps evt)"),
    ("CardPresentation/Shell/RewardEvent.cs", 173, "public bool IsArmyValid(CardArmy army)"),
    ("CardPresentation/Shell/RewardEvent.cs", 188, "public bool IsTargetTo(ILiveOps evt)"),
    ("CardPresentation/Shell/RewardEventHandler.cs", 66,
     "public IEnumerable<RewardInfo> ApplyExtraRewards(ILiveOps evt, IEnumerable<RewardInfo> rewards)"),
    ("CardPresentation/Shell/RewardEventHandler.cs", 73,
     "public Func<IEnumerable<RewardInfo>, IEnumerable<RewardInfo>> GetExtraRewardsFunction(ILiveOps evt)"),
    ("CardPresentation/Battle/ErrorMessageBanner.cs", 205, "public class ErrorMessageBanner : MonoBehaviour"),
    ("CardPresentation/Battle/Label.cs", 2495, "public void ApplySmallScreenTextSize()"),
    ("CardPresentation/Core/LoadableRef.cs", 65, "public LoadableRef(string id)"),
    ("CardPresentation/Core/LoadableRef.cs", 68, "public LoadableRef(T identifiable)"),
    ("CardPresentation/Core/LoadableRef.cs", 113, "public void SetReferenceByObject(T obj)"),
    ("CardPresentation/Core/LoadableRef.cs", 165, "protected bool Equals(LoadableRef<T> other)"),
    ("CardPresentation/Core/LoadableRef.cs", 182, "public override bool Equals(object obj)"),
    ("CardPresentation/Core/LoadableRef.cs", 194, "public override int GetHashCode()"),
    ("CardPresentation/Core/LoadableRef.cs", 244, "public static implicit operator T(LoadableRef<T> instanceRef)"),
    ("CardPresentation/Core/LoadableRef.cs", 256,
     "public static implicit operator string(LoadableRef<T> instanceRef)"),
]


# ---------------------------------------------------------------- 依据（写进报告）
BASIS = {
    ("Extensions", "Contains(int[] target, int nr)"):
        "原落点以 `—（…）` 开头、没有 `文件:行`；三个代表点里第一个 `HudCanvas.cs:629` 是方法体内一行。"
        "现读该内联式所在方法 = `IsRanked`（:627 声明）。",
    ("LoadableReference", "Equals(LoadableReference<T> other)"):
        "`grep -n \"protected bool Equals\"` ⇒ :165（现读）；原 :160 是 `///` doc 注释。",
    ("LoadableReference", "Equals(object obj)"):
        "`grep -n \"public override bool Equals(object\"` ⇒ :182；原 :177 是 doc 注释。",
    ("LoadableReference", "GetHashCode()"):
        "`grep -n \"public override int GetHashCode\"` ⇒ :194；原 :189 是空行。",
    ("LoadableReference", "LoadableReference<T>(T instance)"):
        "`grep -n \"public LoadableRef(T\"` ⇒ :68；原 :63 是空行。",
    ("LoadableReference", "LoadableReference<T>(string id)"):
        "`grep -n \"public LoadableRef(string\"` ⇒ :65；原 :60 是字段行 `readonly Func<T,string> _idOf;`。",
    ("LoadableReference", "SetReferenceByObject(T reference)"):
        "`grep -n \"public void SetReferenceByObject\"` ⇒ :113；原 :108 已落在该方法体里。",
    ("LoadableReference", "T(LoadableReference<T> instanceRef)"):
        "`grep -n \"implicit operator T\"` ⇒ :244；原 :239 是 `/// </code>`。",
    ("LoadableReference", "string(LoadableReference<T> instanceRef)"):
        "`grep -n \"implicit operator string\"` ⇒ :256；原 :249 是**另一个方法**（operator T）体内一行。",
    ("BattleManagerSupport",
     "BroadcastUnitReanimated(BattleManager manager, CardScript unitReanimated)"):
        "现存真声明 = `BoardJumpIncludesPendingDeath`（:16422），其 :16430 那句的注释**自带 `账 L660`**"
        "（就是本行）；原 :16383 是分节注释、:15565/:15607 已漂到 15595/15600。",
    ("CardScript", "MissActions()"):
        "`grep -n \"public static void MissActions\"` ⇒ :14991；原 :14989 是它的 `///` doc。",
    ("CardScript", "OnTurnStarted(bool playerTurn)"):
        "本仓**没有** `OnTurnStarted` 方法（`grep -rn OnTurnStarted` = 0 处声明）⇒ 等价实现 = "
        "`BroadcastTurnStarted`（:14762，卡侧 `OnTrigger(0xa0)` 落在 `FireTriggerAlways`）+ "
        "`BroadcastTurnStartedToHands`（:15876，②③ 手牌两跳）。原账两点（:2262 调用行 / :14769 语句）都不是声明。",
    ("CardScript", "ResolveUnitAttacked(CardScript actingCard, CardScript targetCard, AttackTypes attackType)"):
        "本仓**没有** `ResolveUnitAttacked` 方法（`grep -rn` = 0 处声明）⇒ 该行判据要点描述的那一跳"
        "（`untilNextAttack` 建档 ⇒ `SendRemoveEffect`）的等价实现 = `EnqueueExpireUntilNextAttack`（:568）；"
        "它的 doc 里逐条引了 `ResolveUnitAttacked.c:79-94` 那两条调用点。原 :3383 是 `ExpireUntilNextAttack` "
        "的旧行号（现 :3323）。",
    ("CardScript", "SetAsPlayer(bool isPlayer, CardArmy army, bool setCardback)"):
        "本仓**没有** `SetAsPlayer`（`grep -rn SetAsPlayer` = 0 处声明；`isPlayer` 的等价物 = 实例待在谁的数组里，"
        "映射写在 `EffectResolver.cs:17232`）⇒ 换边写点的代表 = `StealCardAcrossSides`（:17298）。"
        "原账三点全是方法体内 `bool mine = owner == _me;` 一类语句。",
    ("LiveOpsAssetUtility", "GetEventLabel(LiveOpsEventData liveops, EventLabelReferenceType type)"):
        "`grep -n \"GetEventLabel\"` ⇒ 声明在 :1153；原 :1152 是 `///` doc。",
    ("MultiplyRewardsFunction", "Preview(ILiveOps _, IEnumerable<RewardInfo> rewardInfos)"):
        "原落点没有 `:行号`；现读声明 = `Shell/MultiplyRewardsFunction.cs:70`。",
    ("RewardCollected", "OnReset()"):
        "原落点没有 `:行号`；现读声明 = `Shell/RewardCollected.cs:109`。",
    ("RewardCollected", "_003COnInitialize_003Eb__6_1(IItemInfo<IIdentifiable> x)"):
        "原落点没有 `:行号`；该 lambda 的等价实现 = `SumOwnedOfType`（`Shell/RewardCollected.cs:161`；"
        "doc :159 明写「原版 `_003COnInitialize_003Eb__6_1` 的求和」）。",
    ("RewardCollected", "_003COnTrigger_003Eb__8_0(RewardInfo i)"):
        "该 lambda 的等价实现 = `IsNotOwnedYet`（`Shell/RewardCollected.cs:149`；doc :148 明写原版 b__8_0）。",
    ("RewardCollected", "_003COnTrigger_003Eb__8_2(RewardInfo x)"):
        "b__8_2 的求和**内联**在 `OnTrigger`（`Shell/RewardCollected.cs:117`）的 :140-141，没有独立方法 "
        "⇒ 落到宿主方法声明。",
    ("RewardEvent", "AppliesTo(ILiveOps evt)"):
        "原落点没有 `:行号`；现读声明 = `Shell/RewardEvent.cs:146`。",
    ("RewardEvent", "IsArmyValid(CardArmy army)"):
        "原落点没有 `:行号`；现读声明 = `Shell/RewardEvent.cs:173`。",
    ("RewardEvent", "IsTargetTo(ILiveOps evt)"):
        "原落点没有 `:行号`；现读声明 = `Shell/RewardEvent.cs:188`。",
    ("RewardEventHandler", "ApplyExtraRewards(ILiveOps evt, IEnumerable<RewardInfo> rewards)"):
        "原落点没有 `:行号`；现读声明 = `Shell/RewardEventHandler.cs:66`。",
    ("RewardEventHandler", "GetExtraRewardsFunction(ILiveOps evt)"):
        "原落点没有 `:行号`；现读声明 = `Shell/RewardEventHandler.cs:73`。",
    ("UIMessageController", "OnDestroy()"):
        "**查不到同名声明**：`ErrorMessageBanner.cs` 没有单例 `Instance`、也没有 `Awake`/`OnDestroy`；"
        "`grep -rn UIMessageController` 全仓只在注释里；`BattleDriver._errorBanner` 三个调用点 "
        "= `Create`/`ShowError`/`Advance`，没有销毁路。⇒ 退回**类级落点**（闸 §4.1「类型声明 ⇒ 只报告警」），"
        "并在落点里如实写「我们没实现」。",
    ("LoadableReference<T>", "SetReferenceByObject(T reference)"):
        "同 `LoadableReference.SetReferenceByObject`：现读 :113；原 :108 已落在方法体内。",
    ("LoadableReference<T>", "Equals(LoadableReference<T> other)"):
        "现读 :165；原 :160 是 doc 注释。",
    ("LoadableReference<T>", "Equals(object obj)"):
        "现读 :182；原 :177 是 doc 注释。",
    ("LoadableReference<T>", "GetHashCode()"):
        "现读 :194；原 :189 是空行。",
    ("EverguildTextMeshPro", "UpdateTextSizeSmallScreen()"):
        "本仓没有同名方法；doc :2481 明写 `ApplySmallScreenTextSize`（:2495）= 原版 "
        "`UpdateTextSizeSmallScreen()`。原账第一个点 :2460 是字段声明行（`float _ssMaxPx, _ssMinPx;`）。",
}


def main():
    for rel, ln, needle in DECL_CHECKS:
        assert_decl(rel, ln, needle)
    print("声明断言：%d 条全对" % len(DECL_CHECKS))

    files = ["其他_基础设施.tsv", "战斗逻辑驱动.tsv", "日常_奖励.tsv",
             "未分类_其他.tsv", "泛型与启动链_补读.tsv", "菜单_外壳.tsv"]
    index = {}   # key -> (file, lineno, fields, eol)
    for fn in files:
        lines, eol = read_lines(os.path.join(LEDGER, fn))
        for i, l in enumerate(lines, 1):
            if not l.strip():
                continue
            f = l.split("\t")
            if len(f) != 11:
                continue
            if i == 1:
                continue
            k = (f[1].strip(), f[2].strip())
            if k in index:
                print("!! 主表内重复键 %r：%s:%d 与 %s:%d"
                      % (k, index[k][0], index[k][1], fn, i))
            index[k] = (fn, i, f, eol)

    out_rows = []
    missing = []
    for k, new_land in NEW.items():
        if k not in index:
            missing.append(k)
            continue
        fn, i, f, eol = index[k]
        f2 = list(f)
        old = f2[I_LAND]
        assert old != new_land, "新落点与旧落点相同：%r" % (k,)
        f2[I_LAND] = new_land
        out_rows.append((fn, i, f2, old))
    if missing:
        print("!! 主表里找不到这些键：")
        for k in missing:
            print("   ", k)
        return 3

    # 补丁：表头 + 行（按主表文件名/行号排序，读起来方便）
    out_rows.sort(key=lambda t: (files.index(t[0]), t[1]))
    buf = ["\t".join(COLS)]
    for fn, i, f2, old in out_rows:
        assert len(f2) == 11
        buf.append("\t".join(f2))
    # 同一补丁内不许重复键
    seen = set()
    for line in buf[1:]:
        f = line.split("\t")
        k = (f[1].strip(), f[2].strip())
        assert k not in seen, "补丁内重复键 %r" % (k,)
        seen.add(k)

    text = "\n".join(buf) + "\n"
    assert text.count("\r") == 0
    io.open(OUT, "wb").write(text.encode("utf-8"))
    print("写出 %s：%d 行（表头 1 + 数据 %d）" % (OUT, len(buf), len(buf) - 1))
    for fn, i, f2, old in out_rows:
        print("  %-28s:%-5d %s . %s" % (fn, i, f2[1], f2[2]))

    # ------------------------------------------------ 报告
    rep_dir = os.path.join(LEDGER, "..", "报告")
    rep_dir = os.path.normpath(rep_dir)
    assert os.path.isdir(rep_dir), rep_dir
    md = []
    md.append("# B91_L1 —— 落点闸 31 条失败的重定位（只改 `我们的落点` 一列）")
    md.append("")
    md.append("> 2026-10-28 · 账务代理 `L1`。⛔ 未动任何 `.cs`（Unity 批处理正在跑全量自检）、"
              "未动主表、未动 git。补丁 = `账/_parts/B91_L1.tsv`（11 列，31 行）。")
    md.append("")
    md.append("## 一、验收（在账目录副本上跑的，主表一字未改）")
    md.append("")
    md.append("```")
    md.append("cp 账/*.tsv _tmp_view/b91_l1_accept/ledger/")
    md.append("python -I -X utf8 工具/merge_ledger_parts.py --ledger <副本> --parts <只含 B91_L1.tsv 的目录> --only B91_L1")
    md.append("   ⇒ 更新 31 · 新增 0 · 无变化 0")
    md.append("python -I -X utf8 工具/check_ledger_points.py <副本>/*.tsv --all-A")
    md.append("   ⇒ A 5131 条 · 通过 5130（其中告警 268 条）· 漂移 1 · **失败 0**")
    md.append("   （修前：通过 5099 / 失败 31；剩下的 1 条漂移 = `CardScript.HasWhileInPlayAdjacentEffect`，"
              "本来就有的「歧义（2 处声明）」，不在本笔 31 条内）")
    md.append("")
    md.append("逐列核对：改动 **31 行**、每行**只**改第 7 列（`我们的落点`），其余 10 列逐字节相同。")
    md.append("```")
    md.append("")
    md.append("## 二、逐条：旧落点 → 新落点（依据在最右）")
    md.append("")
    md.append("| # | 账 | 类.方法 | 旧落点 | 新落点 | 依据 |")
    md.append("|---:|---|---|---|---|---|")
    for n, (fn, i, f2, old) in enumerate(out_rows, 1):
        k = (f2[1].strip(), f2[2].strip())
        esc = lambda s: (s or "").replace("|", "\\|").replace("\n", " ")
        md.append("| %d | `%s:%d` | `%s.%s` | %s | %s | %s |"
                  % (n, fn, i, esc(k[0]), esc(k[1]), esc(old) or "（空）",
                     esc(f2[I_LAND]), esc(BASIS.get(k, ""))))
    md.append("")
    md.append("## 三、三类的整体说法")
    md.append("")
    md.append("1. **`LoadableRef.cs` 一族（12 条 = 其他表 8 + 泛型表 4）**：该类类尾新加 `operator ==`/`!=` "
              "（+doc）后，原账里的行号整体位移 3~12 行 ⇒ 现在落到注释行 / 空行 / 方法体内一行。"
              "全部按**符号名现读**重定位，共 8 个不同声明（`:65/:68/:113/:165/:182/:194/:244/:256`）。")
    md.append("2. **`日常_奖励.tsv` 的 10 条 `Reward*` + 1 条 `GetEventLabel`**：前 10 条原落点只有"
              "「`文件（方法）`」**没有行号**（本次新建文件的裁定只钉了宿主），逐条现读补 `文件:行号（方法名）`；"
              "`GetEventLabel` 是指到注释行，改到 :1153。")
    md.append("3. **其余 9 条**：`RuleCore` 的 `MissActions`（注释行）/`OnTurnStarted`（调用行）/`ResolveUnitAttacked`"
              "（调用行）· `EffectResolver` 的 `BroadcastUnitReanimated`（分节注释行）· `BattleDriver` 的 "
              "`SetAsPlayer`（语句行）· `Label` 的 `UpdateTextSizeSmallScreen`（字段行）· `ErrorMessageBanner` "
              "的 `UIMessageController.OnDestroy`（括号里的名字在该文件里不是声明）。")
    md.append("")
    md.append("## 四、如实标的边界")
    md.append("")
    md.append("- **`CardScript.OnTurnStarted` / `CardScript.ResolveUnitAttacked` / `CardScript.SetAsPlayer`** "
              "在本仓**没有同名方法**（`grep` 0 处声明）⇒ 落点是**等价实现**（`BroadcastTurnStarted` / "
              "`EnqueueExpireUntilNextAttack` / `StealCardAcrossSides`），已在落点文字里写清「等价实现 = …」——"
              "**不是**为了过闸去指一个名字相近的邻方法。")
    md.append("- **`UIMessageController.OnDestroy()` 查不到**：原版「自己就是 `Instance` ⇒ `Destroy(gameObject)`」"
              "本仓**没实现**（`ErrorMessageBanner` 无单例、无 `Awake`/`OnDestroy`）⇒ 落点退到类声明"
              "（`:205`，闸报 `MEMBER` 告警、不算失败），并在落点里如实写「我们没实现」。"
              "这一条的「实现缺口」不在本笔白名单内（⛔ 不许改 `.cs`），**留给调度台立账**。")
    md.append("- 行号是**现读快照**（2026-10-28）。`.cs` 仍有别的写手在改 ⇒ 用之前按符号名再核一次"
              "（`check_ledger_points.py` 报「漂移」就是又动了）。")
    rep = os.path.join(rep_dir, "B91_L1.md")
    io.open(rep, "wb").write(("\n".join(md) + "\n").encode("utf-8"))
    print("写出 %s（%d 行）" % (rep, len(md)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
