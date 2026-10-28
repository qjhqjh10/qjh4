#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""B93_E28c —— 只改账：把 `check_ledger_points --all-A` 失败明细里**全部 14 条**
「落点落在注释行 / 语句行 / 括号名字不存在」的 A 档行逐条重定位。

做法 = B93_E28b 同一套：**现读**找真声明 → 落点写成 `<文件>.cs:<行>（名字）`（一列只放**一处**引用）
→ 原散文 / 第二处引用移进「差异」列。
⛔ 只改「我们的落点 / 差异」两列；⛔ 不动状态、不动备注、不动主表、不动 .cs。

行号时效：全部在 2026-10-28 本轮现读（见报告「现读时刻」）。
"""
import io
import os
import sys

LEDGER = r"d:/4/Unity/资料/完全复刻/账"
OUT = os.path.join(LEDGER, "_parts", "B93_E28c.tsv")
COLS = ["域", "原版类", "方法", "判据出处", "判据要点", "状态",
        "我们的落点", "差异", "等级", "轮次", "备注"]

# (主表, rn, 期望类, 期望方法, 新落点, 追加到差异的说明)
E = [
    # ---- 个人资料_成就_排行.tsv ----
    ("个人资料_成就_排行.tsv", 92, "ChangeRankedModeToggle", "Clicked()",
     "Shell/RankedEventWindow.cs:283（ChangeRankedButtonClick）",
     "B93-E28c 2026-10-28 现读：`:283 = void ChangeRankedButtonClick()` 才是本跳的实现口"
     "（原 `:278` 是上一个方法的收尾 `}`，`:278（ToggleRankedMode）` 那个名字在文件里没有声明）。"
     "`RankedData.ToggleRankedMode` 是**被调用的数据层方法**，不是落点。"),
    ("个人资料_成就_排行.tsv", 249, "RankedEventWindowV2", "LeaderboardButtonClick()",
     "Shell/RankedEventWindow.cs:266（LeaderboardButtonClick）",
     "B93-E28c 2026-10-28 现读：`:266 = public void LeaderboardButtonClick()`；"
     "原 `:264（OpenLeaderboard）` 的 `:264` 是 `/// <summary>` 注释行，且**全文件没有 `OpenLeaderboard` 这个声明**"
     "（真身是 :274 的 `WindowsManager.OpenByRef(LeaderboardWindow.NameOf(kind), …)`）。"),

    # ---- 商店.tsv ----
    ("商店.tsv", 107, "GachaEventTab", "GetCardsClick()",
     "Shell/GachaEventWindow.cs:1325（OnGetCardsClicked）",
     "B93-E28c 2026-10-28 现读：`:1325 = void OnGetCardsClicked()`（`= 原版 GachaEventTab.GetCardsClick()`，见其 `///` 注释）；"
     "原 `:635（MenuDraw.Hit → WhereToGetCardsPopup）` 是**注释行**（:634-:637 那段说明）。"),
    ("商店.tsv", 113, "GachaEventTab", "ToFocus()",
     "Shell/GachaEventWindow.cs:358（Open）",
     "B93-E28c 2026-10-28 现读：本窗是**单页窗口**、没有页签 `ToFocus` 那一跳（原 `:372` 是注释行）⇒ 等价路径 = "
     "「关掉弹窗 ⇒ `ShowPreviousWindow` ⇒ 本窗 `Open`（整棵 `Build()` 重建）」⇒ 落点 = `:358 public override void Open()`。"),

    # ---- 日常_奖励.tsv ----
    ("日常_奖励.tsv", 412, "LiveOpsManager", "GetEvent(Type t, string id)",
     "Shell/LiveOpsManager.cs:461（GetEvent）",
     "B93-E28c 2026-10-28 现读：`:461 = public static ILiveOps GetEvent(Type t, string id)`；"
     "原 `:445` 是 `// =====` 分隔注释行。"),
    ("日常_奖励.tsv", 414, "LiveOpsManager", "GetEvents(Type t, string[] ids)",
     "Shell/LiveOpsManager.cs:479（GetEvents）",
     "B93-E28c 2026-10-28 现读：`:479 = public static IEnumerable<ILiveOps> GetEvents(Type t, string[] ids)`；"
     "原 `:463` 是方法体里的 `var h = GetHandler(t);`（语句行）。"),
    ("日常_奖励.tsv", 452, "MissionChallengeProgress", "AvailableRewards()",
     "CardPresentation/Shell/MissionEvent.cs:996（AvailableRewards）",
     "B93-E28c 2026-10-28 现读（**就地更正 B92_N7 的旧结论**）：`MissionEvent.cs:996 = public IEnumerable<ChallengeMilestone> AvailableRewards()`"
     "**已建、同名同流程**（`c == null` ⇒ 空表；`challengeType != 1` ⇒ 返回 `c.rewards` 全集；`== 1` ⇒ 出声后退回全集）。"
     "⇒ 原 `Shell/DailyData.cs:349/715（SkullsStepTarget / WeeklyStepTarget）+ 奖励表` 是**另一族**（里程碑步长），"
     "不是本条的实现口；B92_N7 写的「全仓 `AvailableRewards` 只在注释里、要先补」**已过期**。"
     "残差 = `challengeType == 1` 那一支的筛选谓词（本仓无该类挑战，恒不命中）。"),
    ("日常_奖励.tsv", 459, "MissionChallengeProgress", "_003CAvailableRewards_003Eb__17_0(ChallengeMilestone reward)",
     "CardPresentation/Shell/MissionEvent.cs:996（AvailableRewards）",
     "B93-E28c 2026-10-28 现读（**就地更正 B92_N7**）：宿主 `AvailableRewards()` **已建**于 `MissionEvent.cs:996`，"
     "但那只 lambda（`challengeType == 1` 的筛选谓词）**尚未单独落地** —— 原版是**实例方法委托**"
     "（`Predicate<ChallengeMilestone>(this, DAT_1842b0c48)`，`decomp_full` 里无 `__c___AvailableRewards_b__NN.c`）⇒ "
     "我们同支 `SayUnfilteredOnce()` + **退回全集**（⛔ 不猜「按 difficulty 过滤」）。"
     "⇒ 落点 = 宿主声明行；**残差 = 该筛选谓词本身**（要它得先把 `DAT_1842b0c48` 反汇编出来）。"
     "原 `Shell/DailyData.cs:349/715` 是注释行且与本 lambda 无关，已换掉。"),
    ("日常_奖励.tsv", 560, "PlayEvent", "Initialize()",
     "Shell/PlayEvent.cs:456（Initialize）",
     "B93-E28c 2026-10-28 现读：`:456 = public override void Initialize()`；"
     "原 `:462` 是方法体里的 `if (!Handler.IsFirstInitialization) return;`（语句行）。"),
    ("日常_奖励.tsv", 608, "RaidEventTab", "Initialize()",
     "CardPresentation/Shell/RaidEvent.cs:1082（Initialize）",
     "B93-E28c 2026-10-28 现读：真身 = `RaidEvent.cs:1082 public static InitializePlan Initialize(bool hasSelector, bool hasEvent, bool backgroundSpriteAvailable)`"
     "（`RaidEventTabLogic` 起于同文件 :1059）；原第二处 `:1059` 是**类的声明行**、不是方法。"
     "生产口调用点 = `Shell/RaidEventWindow.cs:702`（`if (RaidEventTabLogic.Initialize(...).RebuildRewards) …`）。"),
    ("日常_奖励.tsv", 616, "RaidHeaderController", "Initialize(ILiveOps scoringEvent)",
     "CardPresentation/Shell/RaidEvent.cs:980（Initialize）",
     "B93-E28c 2026-10-28 现读：真身 = `RaidEvent.cs:980 public static InitializePlan Initialize(bool hasEvent)`"
     "（`RaidHeaderControllerLogic` 起于同文件 :946）；原 `:980` 本身没错、错的是**第一处** `Shell/RaidEventWindow.cs:777`"
     "（生产口语句行 `if (RaidHeaderControllerLogic.Initialize(_raid != null).WritesText) …`）。"
     "⇒ 落点收成一处声明，调用点移到此。"),
    ("日常_奖励.tsv", 727, "SkirmishEventWindow", "Open()",
     "Shell/SkirmishEventWindow.cs:802（Open）",
     "B93-E28c 2026-10-28 现读：`:802 = public override void Open()`；原 `:801` 是 `/// <summary>` 注释行。"
     "表头接线 = `Shell/SkirmishEventWindow.cs:627`（`BuildExtras`）→ 类尾 `WireHeaderComponent`（`:913`）。"),

    ("日常_奖励.tsv", 311, "LiveOpsAssetUtility", "ReleaseEventAssets(ILiveOps liveOps)",
     "CardPresentation/Shell/LiveOpsEvent.cs:1391（ReleaseEventAssets）",
     "B93-E28c 2026-10-28 现读（本轮扫描期间别人新加的一条 A，同样错写法）："
     "`:1391 = public static void ReleaseEventAssets(ILiveOps liveOps)`（`LiveOpsContainers` 的 partial，"
     "该 `partial` 段起于同文件 :1366）；原 `:1369` 是 `/// 🔶 自己设计…` 注释行、且 `:1369` 那个位置声明的是"
     "**注入点字段** `public static Action<string> ReleaseAsset;`（:1371），不是本方法。"),

    # ---- 未分类_其他.tsv ----
    ("未分类_其他.tsv", 548, "ResultCounterSharedData", "GetValuesForMode(string playMode)",
     "CardPresentation/Shell/LiveOpsSharedData.cs:537（GetValuesForMode）",
     "B93-E28c 2026-10-28 现读：`:537 = public ResultValues GetValuesForMode(string playMode)`"
     "（类 `ResultCounterSharedData` 起于同文件 :523）；原 `:534` 是 `///` 注释行。"),

    # ---- 菜单_外壳.tsv ----
    ("菜单_外壳.tsv", 722, "TwoSidesTeamDrawer", "Initialize(TwoSidesEventData.TeamInfo teamInfo)",
     "CardPresentation/Shell/ParentEventWindow.cs:1645（ApplyTeamInfo）",
     "B93-E28c 2026-10-28 现读：`:1645 = public void ApplyTeamInfo(int side, string teamName, string captain, Texture2D teamImage)`"
     "（`= 原版 TwoSidesTeamDrawer.Initialize`，见其 `///` 注释）；原 `:1595-1601` 是**注释行**、`:1644` 是 `/// <param>` 行（差一行）。"
     "另：`InitializeTeamContainer`（`:1783`）是**外层容器**那一跳，不是本品。"),
]


def main():
    ledgers = {}
    patch, problems = [], []
    for f, rn, cls, mtd, land, why in E:
        if f not in ledgers:
            b = io.open(os.path.join(LEDGER, f), "rb").read()
            assert b.count(b"\r") == b.count(b"\r\n"), "裸 CR: " + f
            ledgers[f] = b.decode("utf-8").split("\n")
        row = ledgers[f][rn - 1].split("\t")
        if len(row) != 11:
            problems.append("%s:%d 列数 %d" % (f, rn, len(row)))
            continue
        if row[1].strip() != cls or row[2].strip() != mtd:
            problems.append("%s:%d 键不符: %r . %r" % (f, rn, row[1], row[2]))
            continue
        if row[5].strip() != "A":
            problems.append("%s:%d 状态不是 A（%r）—— 只收 A 档闸失败行" % (f, rn, row[5]))
            continue
        old = row[6]
        new = list(row)
        new[6] = land
        new[7] = (row[7] + "｜" if row[7].strip() else "") + \
                 "[B93-E28c 2026-10-28] **落点规范化**：原「我们的落点」= `" + old + "` ⇒ " + why
        for i in range(11):
            if i in (6, 7):
                continue
            if new[i] != row[i]:
                problems.append("%s:%d 第 %d 列被意外改动" % (f, rn, i))
        patch.append((f, rn, new))

    if problems:
        print("!! 先修这些问题（未写盘）：")
        for p in problems:
            print("   " + p)
        return 1

    body = ["\t".join(COLS)] + ["\t".join(r) for _f, _rn, r in patch]
    for i, l in enumerate(body):
        assert len(l.split("\t")) == 11, "行 %d 列数不是 11" % (i + 1)
    data = ("\n".join(body) + "\n").encode("utf-8")
    assert b"\r" not in data
    io.open(OUT, "wb").write(data)

    back = [l for l in io.open(OUT, "rb").read().decode("utf-8").split("\n") if l.strip()]
    assert len(back) == len(E) + 1, "回读行数 %d" % len(back)
    for i, l in enumerate(back):
        assert len(l.split("\t")) == 11, "回读行 %d 列数不是 11" % (i + 1)
    # 回环：逐行与主表比，只许 6/7 两列不同
    for (f, rn, r), l in zip(patch, back[1:]):
        c = ledgers[f][rn - 1].split("\t")
        rr = l.split("\t")
        for i in range(11):
            if i in (6, 7):
                continue
            assert rr[i] == c[i], "%s:%d 回读第 %d 列与主表不一致" % (f, rn, i)
    print("OK 写出 %s：%d 行（+表头）· 11 列 · LF-only · 回环比对通过" % (OUT, len(back) - 1))
    return 0


if __name__ == "__main__":
    sys.exit(main())
