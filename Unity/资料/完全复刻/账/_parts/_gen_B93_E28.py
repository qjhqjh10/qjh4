#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""B93_E28 —— 只改账：把 30 条「无落点/写法不规范」的落点列规范化。

判据源 = 资料/完全复刻/判据/B93_M1_无落点解析.tsv（该派给谁 = 只改账/只回填账）
本脚本**只写** 账/_parts/B93_E28.tsv；⛔ 不动主表、不动 .cs。

写法规范（任务书 §④）：
  · 落点列只放**一个** `文件.cs:行`；第二处及以后移进「差异」列
  · 只改「我们的落点 / 差异 / 备注」三列，⛔ 不动「状态」
"""
import io
import os
import sys

LEDGER = r"d:/4/Unity/资料/完全复刻/账"
OUT = os.path.join(LEDGER, "_parts", "B93_E28.tsv")
COLS = ["域", "原版类", "方法", "判据出处", "判据要点", "状态",
        "我们的落点", "差异", "等级", "轮次", "备注"]

# (主表文件, 行号rn, 期望类, 期望方法前缀, 新落点, 差异追加[], 差异替换[(old,new)], 备注追加, 备注替换[(old,new)])
E = []

def add(f, rn, cls, mtd, land, dapp=None, drep=None, rapp=None, rrep=None):
    E.append(dict(f=f, rn=rn, cls=cls, mtd=mtd, land=land,
                  dapp=dapp or [], drep=drep or [],
                  rapp=rapp, rrep=rrep or []))

# ---------------- 其他_基础设施.tsv ----------------
add("其他_基础设施.tsv", 160, "Extensions",
    "AlmostEquals(Quaternion target, Quaternion second, float maxAngle)",
    "无单点实现 —— 等价 = 调用点内联（CardPresentation/Core/CardView.cs:9975 的 Quaternion.Angle 阈值判；原版 "
    "`AlmostEquals(Quaternion,Quaternion,maxAngle)` 这个封装我们没有）· ⛔ 不新建 Extensions helper",
    drep=[("全仓 `Quaternion.Angle` / `Quaternion.Dot` **0 命中**（最接近 = `CombatCameraZoom.cs:923-925` 的 dot 退化守卫、`BattleDriver.cs:22799` 的近零守卫）。",
           "全仓 `Quaternion.Dot` **0 命中**（⚠️ 2026-10-28 `B93-E28` 就地更正：原文写 `Quaternion.Angle` 也 0 命中 —— **不成立**："
           "现读生产码 2 处 = CardPresentation/Core/CardView.cs:9975/9976（「转过没」的角阈值判）+ 编辑器/自检 21 处；"
           "**死的只是 `AlmostEquals(Quaternion,Quaternion,maxAngle)` 这个封装**；最接近的 dot 退化守卫 = `CombatCameraZoom.cs:923-925`、近零守卫 = `BattleDriver.cs:22799`）。")],
    rapp="[B93-E28 2026-10-28] 落点规范化：`—（暂无落点）` → 「无单点实现 + 等价调用点 CardView.cs:9975」；并就地更正差异列「Angle 0 命中」那句")

add("其他_基础设施.tsv", 291, "LoadableReference", "GetReference()",
    "等价实现 = CardPresentation/Core/CardArt.cs:1060（Get）",
    dapp=["等价实现之二 = RuleEngine/Data/CardDatabase.cs:1112（Load）"],
    rapp="[B93-E28 2026-10-28] 落点规范化：`—` → 等价实现 CardArt.cs:1060（Get）；第二处移入差异列")

add("其他_基础设施.tsv", 292, "LoadableReference", "GetReferenceObjEditor()",
    "无对应物 —— 纯编辑器支（UnityEditor-only，运行期不存在）；今天不该有落点",
    rapp="[B93-E28 2026-10-28] 落点规范化：`—` → 「无对应物·纯编辑器支」")

add("其他_基础设施.tsv", 293, "LoadableReference", "GetTargetId()",
    "等价实现 = CardPresentation/Core/CardArt.cs:1060（Get）",
    dapp=["等价实现之二 = RuleEngine/Data/CardDatabase.cs:1112（Load）（与 rn=291 同族）"],
    rapp="[B93-E28 2026-10-28] 落点规范化：`—` → 等价实现 CardArt.cs:1060（Get）")

# ---------------- 战斗逻辑驱动.tsv ----------------
add("战斗逻辑驱动.tsv", 534, "BattleManager", "ResolveRemoveFromCemetery(BattleAction battleAction)",
    "无对应物 —— 我们同步结算、没有该中间态（D 候选 d）；近邻 = RuleEngine/Core/RuleCore.cs:15770（GetCardsInCemetery）",
    rapp="[B93-E28 2026-10-28] 落点规范化：原「（无落点：…中间态）」→ 保留结论 + 补近邻 RuleCore.cs:15770（GetCardsInCemetery）")

add("战斗逻辑驱动.tsv", 644, "BattleManagerSupport", "BroadcastTurnStarted(BattleManager manager, bool isPlayerTurn)",
    "RuleEngine/Core/RuleCore.cs:14804（BroadcastTurnStarted）",
    dapp=["接线 = RuleEngine/Core/RuleCore.cs:15918（BroadcastTurnStartedToHands）· RuleEngine/Core/RuleCore.cs:2262（BeginTurnCore 三跳接线）"],
    rapp="[B93-E28 2026-10-28] 落点规范化：`:14762`→`:14804`、`:15876`→`:15918`（现读复核；`:2262` 仍准）；第二处以后移入差异列")

add("战斗逻辑驱动.tsv", 835, "CardScript", "ReactToUnitPoisoned(CardScript poisonedUnit, CardScript actingUnit)",
    "半落点 —— 广播 = RuleEngine/Core/RuleCore.cs:16816（BroadcastUnitPoisoned）",
    drep=[("既不广播 290、也无监听端",
           "监听端 = 表驱动 `abilityTriggers==290`（池内 0 张）；⚠️ 2026-10-28（`B93-E28`）就地更正：原「既不广播 290」半句已过期 —— "
           "`BroadcastUnitPoisoned` 已建（RuleEngine/Core/RuleCore.cs:16816；判据注释 :16785 已点名），调用点 = RuleEngine/Core/RuleCore.cs:16153"
           "（poisonous trait 落账后）；trigger 290 映射 = RuleEngine/Core/CardDef.cs:1929")],
    rrep=[("未核：毒是怎么被施加的（`BroadcastUnitPoisoned` 的调用点没查）",
           "已核（`B93-E28` 2026-10-28）：施加点 = RuleEngine/Core/RuleCore.cs:16153")],
    rapp="[B93-E28 2026-10-28] 落点规范化：`无` → 「半落点（广播已建，监听端由表驱动·池内 0 张）」")

add("战斗逻辑驱动.tsv", 992, "CardTraitCollection", "GetTraitEffects(DefinedTrait trait)",
    "CardPresentation/Core/TraitFrames.cs:325（EnsureBindings）",
    dapp=["取整列 = CardPresentation/Core/TraitFrames.cs:287（RowsOfTriggerType）· 分档 = CardPresentation/Core/TraitFrames.cs:467（BindingsFor）· CardPresentation/Core/TraitFrames.cs:603（StartBindingsFor）"],
    rapp="[B93-E28 2026-10-28] 落点规范化：只到文件/成员级 → 补行号（:325/:287/:467/:603 现读全部命中）；第二处以后移入差异列")

add("战斗逻辑驱动.tsv", 1093, "MinionManager", "ActivateMinions(bool isPlayerTurn)",
    "RuleEngine/Core/RuleCore.cs:3531（RecomputeActAndAttack）",
    dapp=["回合起 = RuleEngine/Core/RuleCore.cs:1696（BeginTurnCore）· RuleEngine/Core/RuleCore.cs:2262（三跳调用点）"],
    rapp="[B93-E28 2026-10-28] 落点规范化：`:3518`→`:3531`（现读 = 声明行；3518 现为空注释 `///`）；`:2262` 仍准；第二处以后移入差异列")

add("战斗逻辑驱动.tsv", 1279, "RawCardScript", "PreloadAssets()",
    "无对应物 —— 本仓运行时 0 处 Addressables；今天不该有落点（接入后按 .c 三类逐条照抄）",
    rapp="[B93-E28 2026-10-28] 落点规范化：`—（无；接入 Addressables 后才补）` → 「无对应物·0 处 Addressables」（⛔ 不再写「将来的落点」）")

add("战斗逻辑驱动.tsv", 1379, "CardScript", "set_hasActiveAbility(bool)",
    "RuleEngine/Core/CardDef.cs:235（HasActiveAbility 列）",
    dapp=["唯一写点 = RuleEngine/Core/CardDef.cs:1019（装载时按 JSON 赋）· 读点 = CardPresentation/Battle/BattleDriver.cs:15731 / CardPresentation/Battle/BattleDriver.cs:36331"],
    rapp="[B93-E28 2026-10-28] 落点规范化：只到成员级 → 补声明行 :235（列）；写点/读点移入差异列。⛔ 不「好心点亮」45 张（无运行期重算入口）")

# ---------------- 教程_引导.tsv ----------------
add("教程_引导.tsv", 3, "BattleTutorialManager", "Initialize()",
    "无对应物 —— 原版只缓存两个单例字段引用（+0x38 BattleManager / +0x40 PlayerDataManager）、零行为（本仓 `BattleDriver` 即 BattleManager、`Ctx` 直接用）；今天不该有落点",
    rapp="[B93-E28 2026-10-28] 落点规范化：长句 → 「无对应物·零行为」；保留 B92_V1 的结论（⛔ 不指 EnsureTutorialOverlay）")

# ---------------- 日常_奖励.tsv ----------------
add("日常_奖励.tsv", 177, "EventImageSelectorController", "Initialize(List<EventAsset> eventAssets)",
    "CardPresentation/Shell/EventImageSelector.cs:133（Initialize）",
    dapp=["同族 = CardPresentation/Shell/EventImageSelector.cs:109（ImageEventDrawData.TryDraw）· 所属类 = CardPresentation/Shell/EventImageSelector.cs:73（EventImageSelectorController）"],
    rapp="[B93-E28 2026-10-28] 落点规范化（账过期回填）：`无` → EventImageSelector.cs:133（现读 = public static int Initialize(List<ImageEventDrawData>, List<EventAsset>)）；第二处以后移入差异列")

add("日常_奖励.tsv", 180, "EventImageSelectorController", "TryDraw(List<EventAsset> eventAssets)",
    "CardPresentation/Shell/EventImageSelector.cs:109（TryDraw）",
    dapp=["资产取用 = CardPresentation/Shell/EventImageSelector.cs:198（EventArt.HasAsset）· CardPresentation/Shell/EventImageSelector.cs:204（LoadAsset）· CardPresentation/Shell/EventImageSelector.cs:214（LoadByName）"],
    rapp="[B93-E28 2026-10-28] 落点规范化：`无` → EventImageSelector.cs:109（现读 = public bool TryDraw(List<EventAsset>)）；原落点的 `:195` 实为 `class EventArt` 声明 ⇒ 换成真正的三个成员 :198/:204/:214（移入差异列）")

add("日常_奖励.tsv", 423, "LiveopUIDrawer_EventAssetSprites", "EventAssetImages.Initialize(ILiveOps liveopEvent)",
    "CardPresentation/Shell/EventImageSelector.cs:164（Initialize）",
    dapp=["外层 = CardPresentation/Shell/EventImageSelector.cs:181（LiveopUIDrawer_EventAssetSprites.Initialize）· 所属类 = CardPresentation/Shell/EventImageSelector.cs:149（EventAssetImages）"],
    rapp="[B93-E28 2026-10-28] 落点规范化：`无` → 嵌套类 EventAssetImages.Initialize :164（现读命中）；其余两处移入差异列")

add("日常_奖励.tsv", 424, "LiveopUIDrawer_EventAssetSprites", "LiveopUIDrawer_EventAssetSprites.Initialize(ILiveOps liveopEvent)",
    "CardPresentation/Shell/EventImageSelector.cs:181（Initialize）",
    dapp=["逐件 = CardPresentation/Shell/EventImageSelector.cs:164（EventAssetImages.Initialize）"],
    rapp="[B93-E28 2026-10-28] 落点规范化：`无` → 外层 Initialize :181（现读 = public static int Initialize(List<EventAssetImages>, LiveOpsLocalEvent)）")

add("日常_奖励.tsv", 487, "MissionEvent", "ReleaseContainer()",
    "CardPresentation/Shell/MissionEvent.cs:387（ReleaseContainer）",
    dapp=["两支 = `UseSmallContainer` 支出声不做事 / 另一支转调 `base.ReleaseContainer()`"],
    rapp="[B93-E28 2026-10-28] 落点规范化（账过期回填）：`—（无；同上注入口）` → MissionEvent.cs:387（现读 = public override void ReleaseContainer()）")

# 日常_奖励 732/733/734（SurvivalEventWindowEnemyMarker）：**2026-10-28 已由 `B93_D1` 回填并合入主表**
#   （现读 状态 C→B、落点 = Shell/SurvivalEventWindowEnemyMarker.cs:66/:74/:82，差异列已写清「今天没有调用方 + 缺整条生存事件窗线」）
#   ⇒ 本补丁**不再重复改**（重复改 = 两份说法 + 覆盖别人的活）。

# ---------------- 未分类_其他.tsv ----------------
add("未分类_其他.tsv", 396, "ListExtensions", "SpliceWhere<T>(IList<T> list, Func<T, bool> predicate, int count)",
    "无单点实现 —— 等价 = 逐调用点内联（List.RemoveAll / LINQ）；⛔ 不新建 helper",
    rapp="[B93-E28 2026-10-28] 落点规范化：长句 → 「无单点实现 + 逐调用点内联」；判定口径不变（B92_N6）")

add("未分类_其他.tsv", 398, "ListExtensions", "GetMax<T, TU>(IEnumerable<T> data, Func<T, TU> f)",
    "无单点实现 —— 等价 = 逐调用点内联（Select(...).Max()）；⛔ 不新建 helper",
    rapp="[B93-E28 2026-10-28] 落点规范化：长句 → 「无单点实现 + Select(...).Max() 内联」")

add("未分类_其他.tsv", 399, "ListExtensions", "GetMin<T, TU>(IEnumerable<T> data, Func<T, TU> f)",
    "无单点实现 —— 等价 = 逐调用点内联（Select(...).Min()）；⛔ 不新建 helper",
    rapp="[B93-E28 2026-10-28] 落点规范化：长句 → 「无单点实现 + Select(...).Min() 内联」")

add("未分类_其他.tsv", 709, "UIResourceCounter", "Toggle(bool option)",
    "CardPresentation/Battle/BattleDriver.cs:11726（SetFactionResourceVisible）",
    dapp=["调用点 = CardPresentation/Battle/BattleDriver.cs:32048"],
    drep=[("已删；按「我们的落点」列的实情写成「无逐方法实现」。行为不变（显隐在各处 `SetActive` 里）。",
           "已删。🔴 2026-10-28（`B93-E28`）再订正：等价实现 = `BattleDriver.cs:11726 SetFactionResourceVisible(...)`"
           "（四个 bool 一次显隐四颗货币件）、调用点 = `BattleDriver.cs:32048` ⇒ 「无逐方法实现」这句本身也过期了。")],
    rapp="[B93-E28 2026-10-28] 落点规范化：`无逐方法实现` → BattleDriver.cs:11726（SetFactionResourceVisible）；调用点 :32048 移入差异列")

# ---------------- 泛型与启动链_补读.tsv ----------------
add("泛型与启动链_补读.tsv", 8, "LoadableReference<T>", "LoadableReference<T>.op_Implicit(LoadableReference<T> instanceRef) → T",
    "替代 = CardPresentation/Core/CardArt.cs:1060（Get）",
    dapp=["替代之二 = RuleEngine/Data/CardDatabase.cs:1112（Load）"],
    rapp="[B93-E28 2026-10-28] 落点规范化：`—` → 替代 CardArt.cs:1060（Get）；第二处移入差异列")

add("泛型与启动链_补读.tsv", 9, "LoadableReference<T>", "LoadableReference<T>.op_Implicit(LoadableReference<T> instanceRef) → string",
    "替代 = CardPresentation/Core/CardArt.cs:1060（Get）",
    dapp=["替代之二 = RuleEngine/Data/CardDatabase.cs:1112（Load）"],
    rapp="[B93-E28 2026-10-28] 落点规范化：`—` → 替代 CardArt.cs:1060（Get）（备注那条「旧判据要点其实对应返回 T 那条」保留）")

# ---------------- 社交_聊天.tsv ----------------
add("社交_聊天.tsv", 15, "AllianceInvitationEntry", "OnDestroy()",
    "无对应物 —— 原版只 RemoveAllListeners、零可观察效果（本仓按钮走闭包、随节点销毁）；今天不该有落点（建行 = CardPresentation/Shell/AllianceSearchTab.cs:625）",
    rapp="[B93-E28 2026-10-28] 落点规范化：长句 → 「无对应物·零可观察效果」+ 建行指针 AllianceSearchTab.cs:625")

# ---------------- 菜单_外壳.tsv ----------------
add("菜单_外壳.tsv", 4, "AdaptFlexibleGridLayoutByAspectRatio", "OnEnable()",
    "无对应物 —— 我们钉高 1080 ⇒ s 恒 1；今天不该有落点（残留归 CardPresentation/Core/LayoutSpace.cs:23 的画布模型）",
    rapp="[B93-E28 2026-10-28] 落点规范化：长句 → 「无对应物·s 恒 1」+ 残留指针 LayoutSpace.cs:23")

# ---------------- 轮抽.tsv ----------------
add("轮抽.tsv", 114, "DraftModeMenuPayStateTimed", "OnOpen()",
    "CardPresentation/Shell/DraftModeWindow.cs:4281（BuildTimedFamily，gated TimedDraftLocal.Enabled）",
    dapp=["双阵营条 = CardPresentation/Shell/DraftModeWindow.cs:4311（BuildTimedArmyBar = SetProgress + SetArmies）· 支付三块 = 同文件 :4286-4296"],
    rapp="[B93-E28 2026-10-28] 落点规范化（账过期回填）：`无` → DraftModeWindow.cs:4281（现读 = void BuildTimedFamily(Transform root)）")

add("轮抽.tsv", 147, "DraftModeWarlordSelectionStateTimed", "OnOpen()",
    "CardPresentation/Shell/DraftModeWindow.cs:4281（BuildTimedFamily 内 :4299-4305：按 specificArmies 过滤 + RebuildWarlordCards）",
    dapp=["同 rn=114 的 gated 前提（TimedDraftLocal.Enabled 默认关）"],
    rapp="[B93-E28 2026-10-28] 落点规范化：`无（Timed 变体页未建…）` → DraftModeWindow.cs:4281 的 Timed 选将段（现读 :4299 过滤 / :4305 RebuildWarlordCards）")


def read_rows(path):
    b = io.open(path, "rb").read()
    assert b.count(b"\r") == b.count(b"\r\n"), "裸 CR: " + path
    return b.decode("utf-8").split("\n")


def main():
    main_rows = {}
    patch = []
    problems = []
    for e in E:
        if e["f"] not in main_rows:
            main_rows[e["f"]] = read_rows(os.path.join(LEDGER, e["f"]))
        lines = main_rows[e["f"]]
        row = lines[e["rn"] - 1].split("\t")
        if len(row) != 11:
            problems.append("%s:%d 列数 %d" % (e["f"], e["rn"], len(row)))
            continue
        if row[1].strip() != e["cls"] or row[2].strip() != e["mtd"]:
            problems.append("%s:%d 键不符: %r . %r" % (e["f"], e["rn"], row[1], row[2]))
            continue
        new = list(row)
        new[6] = e["land"]
        d = new[7]
        for old, rep in e["drep"]:
            if old not in d:
                problems.append("%s:%d 差异列找不到要替换的片段: %r" % (e["f"], e["rn"], old[:40]))
                continue
            d = d.replace(old, rep, 1)
        for extra in e["dapp"]:
            d = (d + "｜" if d.strip() else "") + extra
        new[7] = d
        r = new[10]
        for old, rep in e["rrep"]:
            if old not in r:
                problems.append("%s:%d 备注列找不到要替换的片段: %r" % (e["f"], e["rn"], old[:40]))
                continue
            r = r.replace(old, rep, 1)
        if e["rapp"]:
            r = (r + "｜" if r.strip() else "") + e["rapp"]
        new[10] = r
        # 只允许改 6/7/10
        for i in range(11):
            if i in (6, 7, 10):
                continue
            if new[i] != row[i]:
                problems.append("%s:%d 第 %d 列被意外改动" % (e["f"], e["rn"], i))
        patch.append(new)

    if problems:
        print("!! 先修这些问题（未写盘）：")
        for p in problems:
            print("   " + p)
        return 1

    body = ["\t".join(COLS)] + ["\t".join(r) for r in patch]
    text = "\n".join(body) + "\n"
    # 写前自检：每行 11 列、无裸 CR、无 NUL
    for i, l in enumerate(body):
        assert len(l.split("\t")) == 11, "行 %d 列数不是 11" % (i + 1)
    data = text.encode("utf-8")
    assert b"\r" not in data
    io.open(OUT, "wb").write(data)

    # 回读环比
    back = io.open(OUT, "rb").read().decode("utf-8").split("\n")
    back = [l for l in back if l.strip()]
    assert len(back) == len(E) + 1, "回读行数 %d（应为 %d）" % (len(back), len(E) + 1)
    for i, l in enumerate(back):
        assert len(l.split("\t")) == 11, "回读行 %d 列数不是 11" % (i + 1)
    print("OK 写出 %s：%d 行补丁（+表头）" % (OUT, len(back) - 1))
    print("列数全部 11 ✅ · 行数 %d ✅ · LF-only ✅" % len(back))
    return 0


if __name__ == "__main__":
    sys.exit(main())
