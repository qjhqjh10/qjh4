// B140_WY_Assert.cs — 执行代理 `B140_WY` 的断言宿主（ACT-02：活动窗左栏「事件钮」按原版轴重建）
//
// 活：把 `ParentEventWindow.BuildEventTabs` 的轴从「窗种类」改成「活动实例」（原版 `BaseParentEventWindow.SetupTabs`）。
// 判据（第一权威，方法体现读 2026-11-04）：
//   · `d:/2/tools/decomp_full/BaseParentEventWindow__SetupTabs.c`
//       `LiveOpsManager.GetAllEvents(2 /*= EventLocationType.Parent*/, this.Parent)` → `.Where(…)` → 逐事件：
//       `HasComponent(ev, 13 /*TabButton*/)` ⇒ `TabButtons.AddTabButton(GetLabel(ev,0), LoadAsset(ev,0))` + `SetAsLastSibling`
//       `HasComponent(ev, 11 /*Tab*/)`      ⇒ `CreateEventTab(ev)`（**本窗内的一页**，不是钮）
//       两样都没有 ⇒ 什么都不建；一个事件都没有 ⇒ 基类 `GameWindowWithTabs.SetupTabs(0)`（左栏空）
//       ⚠️ 那个 `2` **不是** `LiveOpState`（`LiveOpsManager.cs:276` 的签名 = `GetAllEvents(EventLocationType, string parent)`）；
//          状态闸（`GetState == Active`）是**我们按用户边界加的**，`.c` 里看不出状态过滤。
//   · `Everguild.LiveOps.LiveOpsAssetUtility__HasComponent.c` = `drawData.EventComponents.Any(c => c.key == type)`
//   · `BaseParentEventWindow.__c__DisplayClass4_0___SetupTabs_b__2.c` = 点钮 ⇒ `OpenWindow(GetComponentReference(ev, 12))`
//
// 跑法（⚠️ 要**主对话**跑 —— 子代理不许 `-executeMethod`）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B140_WY_Assert.Run -logFile -
//
// 判绿红：按**行首** `B140WY ✓` / `B140WY ✗` 数（⛔ 别 `grep -c ✗` —— `✗` 会出现在文案里）；
//   「未验」单独计数、**不算通过**（⛔ 不静默）。
//
// 覆盖（A–F + 2 条灭自证）：
//   A 两个**不同窗种类**的在期活动 ⇒ 恰好 2 颗（按窗种类遍历的老实现会给 3 颗）
//   B 两个**同窗种类**的在期活动 ⇒ 2 颗（证明不是按窗种类去重）
//   C 0 个在期活动 ⇒ 0 颗（⛔ 不退回「其它三扇窗」那 3 颗）
//   D 过期活动不参与 / 混合名单只出在期那颗
//   E **不带 `TabButton(13)`** 的在期活动 ⇒ 不出钮（原版真闸；本地 13 份 SO 全都没带 ⇒ 现读 0 颗）
//     E2 `location.type != Parent(2)` 的活动 ⇒ 不进候选（`GetAllEvents(2 /*Parent*/, …)` 那道闸）
//   F 真目录（`LiveOpsLocalCatalog.Events`）：在期活动有、但没一份带 13 ⇒ 真实现也建 0 颗
//   ★ 灭自证：`OldAxisCount(self)`（旧算法的颗数）必须 ≠ 新实现的颗数 —— 把实现改回遍历窗种类 ⇒ A / C 必红
using System;
using System.Collections.Generic;
using UnityEngine;
using CardPresentation;

public static class B140_WY_Assert
{
    static int _pass, _fail, _unverified;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
    public static int Unverified { get { return _unverified; } }

    const string Tag = "B140WY";

    // 现读自 `Shell/LiveOpsLocalCatalog.cs` 的组件 GUID（`判据/B115_W16_活动GUID施工单.md` §一 第 2 行）
    const string ExpPassTabGuid = "e6914cbdd45814f5f9bd40a854c12218";
    // 两条**真实存在**的词条键（`EventDescriptionKeys` 里就有；`TermText` 能查到非空值）
    const string TermTitle0 = "Event_Description/ExpansionPass/TabTitle";   // label 0 = Title
    const string TermNav4 = "Event_Description/Gacha/TabTitle";             // label 4 = NavigationBarTitle

    static readonly DateTime Base = new DateTime(2025, 4, 15, 0, 0, 0, DateTimeKind.Utc);   // ExpansionOrks1 在期那一刻

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void CheckEq(int got, int want, string msg)
    {
        Check(got == want, msg + "（期望 " + want + "；实得 " + got + "）");
    }

    static void CheckStr(string got, string want, string msg)
    {
        Check(got == want, msg + "（期望 `" + (want ?? "<null>") + "`；实得 `" + (got ?? "<null>") + "`）");
    }

    static void MarkUnverified(string msg) { _unverified++; Debug.Log(Tag + " … 未验：" + msg); }

    public static void Run()
    {
        _pass = _fail = _unverified = 0;
        Debug.Log(Tag + " ===== B140_WY · ACT-02 活动事件钮（每个活动实例一颗） =====");
        GroupPreconditions();
        GroupA();
        GroupB();
        GroupC();
        GroupD();
        GroupE();
        GroupF();
        Debug.Log(Tag + " 合计：通过 " + _pass + " · 失败 " + _fail + " · 未验 " + _unverified);
    }

    // ------------------------------------------------------------ 夹具
    static long Ms(DateTime utc)
    {
        return (long)(utc - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
    }

    /// <summary>造一场活动（`LiveOpsLocalEvent` 的离线等价物）。`tabButton = true` ⇒ 带 `TabButton(13)` 组件；
    /// `locationType` 缺省 = `Parent(2)`（`GetAllEvents(2, …)` 收的那一档）。</summary>
    static LiveOpsLocalEvent MakeEvent(string id, LiveOpsLocalKind kind, DateTime start, DateTime end,
                                       string mainWindowGuid, string tabGuid, bool tabButton,
                                       string titleTerm, string navTerm, int locationType = 2)
    {
        var comps = new List<KeyValuePair<int, string>>();
        if (!string.IsNullOrEmpty(mainWindowGuid))
            comps.Add(new KeyValuePair<int, string>((int)LiveOpsComponentRef.MainWindow, mainWindowGuid));
        if (!string.IsNullOrEmpty(tabGuid))
            comps.Add(new KeyValuePair<int, string>((int)LiveOpsComponentRef.Tab, tabGuid));
        if (tabButton)
            comps.Add(new KeyValuePair<int, string>((int)LiveOpsComponentRef.TabButton, "0000000000000000000000000000b140"));
        var labels = new List<LiveOpsLocalLabel>();
        if (!string.IsNullOrEmpty(titleTerm)) labels.Add(new LiveOpsLocalLabel { Key = 0, Term = titleTerm });
        if (!string.IsNullOrEmpty(navTerm)) labels.Add(new LiveOpsLocalLabel { Key = 4, Term = navTerm });
        return new LiveOpsLocalEvent
        {
            EventId = id, SoName = id, Kind = kind,
            Enabled = true, UploadToLive = true,
            // 候选集闸：`GetAllEvents(2 /*EventLocationType.Parent*/, …)` ⇒ 只收挂在父活动下的子活动
            LocationType = locationType,
            StartMs = Ms(start), EndMs = Ms(end), Expirable = false, ExpireMs = Ms(end),
            Labels = labels.ToArray(), Components = comps.ToArray(),
        };
    }

    static LiveOpsLocalEvent ParentActive(string id, bool tabButton)
    {
        return MakeEvent(id, LiveOpsLocalKind.Parent, Base.AddDays(-1), Base.AddDays(1),
                         LiveOpsEventEntry.ParentEventMainWindowGuid, null, tabButton, TermTitle0, TermNav4);
    }

    static LiveOpsLocalEvent ScoringActive(string id, bool tabButton)
    {
        return MakeEvent(id, LiveOpsLocalKind.Scoring, Base.AddDays(-1), Base.AddDays(1),
                         null, ExpPassTabGuid, tabButton, TermTitle0, TermNav4);
    }

    static List<ParentEventWindow.EventTab> Build(ParentEventWindow.EventWindowKind self,
                                                  params LiveOpsLocalEvent[] events)
    {
        var list = new List<LiveOpsLocalEvent>(events == null ? 0 : events.Length);
        if (events != null) list.AddRange(events);
        return ParentEventWindow.BuildEventTabs(self, Base, list);
    }

    /// <summary>★ 灭自证用：**旧实现**（2026-11-03 之前那一版，按窗种类遍历）在同一 `self` 下会建几颗。
    /// 复刻的就是被删掉的那段：`for (i &lt; EventWindowRootNames.Length) if ((EventWindowKind)i != self) add`。
    /// ⛔ 生产代码不用它 —— 它只负责证明「A / C 这两条真的能抓到回退」。</summary>
    static int OldAxisCount(ParentEventWindow.EventWindowKind self)
    {
        int n = 0;
        for (int i = 0; i < ParentEventWindow.EventWindowRootNames.Length; i++)
            if ((ParentEventWindow.EventWindowKind)i != self) n++;
        return n;
    }

    // ------------------------------------------------------------ 前提（先断前提，再断结论）
    static void GroupPreconditions()
    {
        var p = ParentActive("P0", true);
        var noBtn = ParentActive("P0N", false);
        var expired = MakeEvent("P0X", LiveOpsLocalKind.Parent, Base.AddDays(-10), Base.AddDays(-5),
                                LiveOpsEventEntry.ParentEventMainWindowGuid, null, true, TermTitle0, TermNav4);
        Check(p.IsCurrentlyActive(Base), "（前提）夹具 `P0` 在 `Base` 时刻是 `Active`（`GetState` 六档）");
        Check(!expired.IsCurrentlyActive(Base), "（前提）夹具 `P0X` 在 `Base` 时刻是 `Expired`（⛔ 不是 `Active`）");
        Check(ParentEventWindow.HasComponent(p, LiveOpsComponentRef.TabButton), "（前提）`P0` 带 `TabButton(13)`");
        Check(!ParentEventWindow.HasComponent(noBtn, LiveOpsComponentRef.TabButton), "（前提）`P0N` **不带** `TabButton(13)`");
        Check(p.LocationType == (int)LiveOpsLocationType.Parent,
              "（前提）夹具 `location.type = Parent(2)`（`GetAllEvents(2, …)` 的候选集闸）");
        Check(ParentEventWindow.HasComponent(p, LiveOpsComponentRef.MainWindow), "（前提）`P0` 带 `MainWindow(10)`");
        CheckStr(ParentEventWindow.ComponentGuidOf(p, LiveOpsComponentRef.MainWindow),
                 LiveOpsEventEntry.ParentEventMainWindowGuid, "（前提）`ComponentGuidOf(MainWindow)` 读得回那条 GUID");
        Check(ParentEventWindow.ResolveWindowKind(p) == ParentEventWindow.EventWindowKind.Parent,
              "（前提）`P0` 的窗种类解析 = `Parent`");
        Check(ParentEventWindow.ResolveWindowKind(ScoringActive("P0S", true)) == ParentEventWindow.EventWindowKind.ExpansionPass,
              "（前提）Scoring 活动（带 `Tab(11)`）的窗种类解析 = `ExpansionPass`");
    }

    // ------------------------------------------------------------ A：不同窗种类的两个在期活动 ⇒ 恰好 2 颗
    static void GroupA()
    {
        var p = ParentActive("ActParent", true);
        var s = ScoringActive("ActScoring", true);
        var tabs = Build(ParentEventWindow.EventWindowKind.Gacha, p, s);

        CheckEq(tabs.Count, 2, "★ A：2 个**不同窗种类**的在期活动 ⇒ **恰好 2 颗**钮（按窗种类遍历的老实现给 3 颗）");
        CheckEq(OldAxisCount(ParentEventWindow.EventWindowKind.Gacha), 3, "★ A 灭自证：旧算法在同一 `self` 下会给 3 颗");
        Check(tabs.Count != OldAxisCount(ParentEventWindow.EventWindowKind.Gacha),
              "★★ 灭自证：新实现的颗数 ≠ 旧算法的颗数 ⇒ 把实现改回「遍历窗种类」这条必红");
        if (tabs.Count != 2) return;    // 数目不对 ⇒ 下面按下标读会越界，先收（⛔ 用 if 包住结论）

        CheckStr(tabs[0].EventId, "ActParent", "A：第 1 颗来自 `ActParent`（顺序 = 传入表的序）");
        CheckStr(tabs[1].EventId, "ActScoring", "A：第 2 颗来自 `ActScoring`");
        Check(tabs[0].Kind == ParentEventWindow.EventWindowKind.Parent
           && tabs[1].Kind == ParentEventWindow.EventWindowKind.ExpansionPass,
              "A：两颗各自解析到**自己的**窗种类（`Parent` / `ExpansionPass`），⛔ 不是同一扇");
        Check(tabs[0].Open != null && tabs[1].Open != null,
              "A：两颗都有**去向**（原版 `b__2` = `OpenWindow(...)`；null ⇒ 点了没反应）");
        CheckStr(tabs[0].Label, LiveOpsLocalCatalog.TermText(TermTitle0),
                 "A：页签名取 **label 0（Title）**（原版 `SetupTabs` 只读 0；⛔ 不是 label 4 `NavigationBarTitle`）");
        Check(!string.IsNullOrEmpty(tabs[1].Label), "A：第 2 颗的文字非空（⛔ 不许建一颗空字页签）");
    }

    // ------------------------------------------------------------ B：同窗种类的两个在期活动 ⇒ 2 颗（不是按窗去重）
    static void GroupB()
    {
        var a = ParentActive("ActP1", true);
        var b = ParentActive("ActP2", true);
        var tabs = Build(ParentEventWindow.EventWindowKind.Parent, a, b);

        CheckEq(tabs.Count, 2, "★ B：2 个**同窗种类**的在期活动 ⇒ **2 颗**（证明不是按窗种类去重）");
        if (tabs.Count != 2) return;
        CheckStr(tabs[0].EventId, "ActP1", "B：第 1 颗 `ActP1`");
        CheckStr(tabs[1].EventId, "ActP2", "B：第 2 颗 `ActP2`（⛔ 不是两颗都落到「StartMs 最大者」那一场）");
        Check(tabs[0].Kind == ParentEventWindow.EventWindowKind.Parent
           && tabs[1].Kind == ParentEventWindow.EventWindowKind.Parent,
              "B：两颗都是 `Parent` 窗 —— 同窗多颗钮是原版形状，⛔ 别去重");
    }

    // ------------------------------------------------------------ C：0 个在期活动 ⇒ 0 颗
    static void GroupC()
    {
        var tabs = Build(ParentEventWindow.EventWindowKind.Parent, new LiveOpsLocalEvent[0]);
        CheckEq(tabs.Count, 0, "★ C：0 个在期活动 ⇒ **0 颗**钮（原版 `GameWindowWithTabs.SetupTabs(0)` 那一支）");
        CheckEq(OldAxisCount(ParentEventWindow.EventWindowKind.Parent), 3,
                "★ C 灭自证：旧算法在 0 个活动时仍会给 3 颗 ⇒ 回退必红");
        Check(tabs.Count != OldAxisCount(ParentEventWindow.EventWindowKind.Parent),
              "★★ 灭自证：C 的新实现 0 颗 ≠ 旧算法 3 颗");
    }

    // ------------------------------------------------------------ D：过期活动不参与
    static void GroupD()
    {
        var expired = MakeEvent("ActExpired", LiveOpsLocalKind.Parent, Base.AddDays(-10), Base.AddDays(-5),
                                LiveOpsEventEntry.ParentEventMainWindowGuid, null, true, TermTitle0, TermNav4);
        var live = ParentActive("ActLive", true);

        var onlyExpired = Build(ParentEventWindow.EventWindowKind.Parent, expired);
        CheckEq(onlyExpired.Count, 0, "D：**只有过期活动** ⇒ 0 颗（用户边界「过期活动不做」）");

        var mixed = Build(ParentEventWindow.EventWindowKind.Parent, expired, live);
        CheckEq(mixed.Count, 1, "D：混合名单（过期 + 在期）⇒ 只出**在期那颗**");
        if (mixed.Count == 1)
            CheckStr(mixed[0].EventId, "ActLive", "D：留下的那颗 = `ActLive`（过期那颗不在名单里）");
    }

    // ------------------------------------------------------------ E：`TabButton(13)` 是原版真闸
    static void GroupE()
    {
        var noBtn = ParentActive("ActNoTabButton", false);
        var tabs = Build(ParentEventWindow.EventWindowKind.Parent, noBtn);
        CheckEq(tabs.Count, 0,
                "★ E：**在期但不带 `TabButton(13)`** 的活动 ⇒ 0 颗（原版 `HasComponent(ev,13)` 是闸，"
              + "不是「在期就给一颗」—— 这一条是「别把闸拆了」的守门断言）");

        var withBtn = ParentActive("ActWithTabButton", true);
        CheckEq(Build(ParentEventWindow.EventWindowKind.Parent, withBtn).Count, 1,
                "E（正对照）：同一场活动只多一个 `TabButton(13)` ⇒ 1 颗（证明上面那条的 0 是闸拦的、不是别的 bug）");

        // 第二道闸（候选集）：`GetAllEvents(2 /*Parent*/, …)` —— `location.type` 不是 2 的活动根本不进候选
        var loc0 = MakeEvent("ActLocMain", LiveOpsLocalKind.Parent, Base.AddDays(-1), Base.AddDays(1),
                             LiveOpsEventEntry.ParentEventMainWindowGuid, null, true, TermTitle0, TermNav4,
                             (int)LiveOpsLocationType.Main);
        CheckEq(Build(ParentEventWindow.EventWindowKind.Parent, loc0).Count, 0,
                "E2：带 `TabButton(13)` 但 `location.type = Main(0)` ⇒ 0 颗（`GetAllEvents(2 /*Parent*/, …)` 只收子活动）");
    }

    // ------------------------------------------------------------ F：真目录现读（13 份 SO 没有一份带 13）
    static void GroupF()
    {
        int active = 0, withBtn = 0, childActive = 0;
        var evs = LiveOpsLocalCatalog.Events;
        for (int i = 0; i < evs.Count; i++)
        {
            var e = evs[i];
            if (e == null) continue;
            bool act = e.IsCurrentlyActive(Base);
            if (act) active++;
            if (act && e.LocationType == (int)LiveOpsLocationType.Parent) childActive++;
            if (ParentEventWindow.HasComponent(e, LiveOpsComponentRef.TabButton)) withBtn++;
        }
        CheckEq(withBtn, 0,
                "F 现读：本地 13 份活动 SO **没有一份带 `TabButton(13)`**（若将来补了 13，这条会红 —— "
              + "那时请改**期望**，⛔ 不要去改实现）");
        Check(active > 0, "（前提）`" + Base.ToString("yyyy-MM-dd") + "` UTC 本地目录里**有在期活动**"
                        + "（有 " + active + " 场；否则下面那条 0 颗是空验）");
        Check(childActive > 0, "（前提）其中有 `location.type = Parent(2)` 的在期子活动（有 " + childActive + " 场）"
                             + " ⇒ 候选集非空，0 颗真的是 `TabButton(13)` 闸拦的");
        var tabs = ParentEventWindow.BuildEventTabs(ParentEventWindow.EventWindowKind.Parent, Base);
        CheckEq(tabs.Count, 0,
                "★ F：真目录 + 真实现 ⇒ 0 颗（候选集非空、可它们都没带 13；原版拿这份数据同样是 0 颗 —— "
              + "旧实现那种「没有也硬建 3 颗」= 假钮，已删）");
        MarkUnverified("Unity 宿主（`ShellScene.Run` 左栏实景 + 四扇窗真建）本轮未跑 —— 子代理不许 -executeMethod");
    }
}
