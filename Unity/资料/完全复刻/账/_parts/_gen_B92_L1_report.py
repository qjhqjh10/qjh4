#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""B92_L1 报告生成器：把补丁与主表现读对齐，产出 报告/B92_L1.md（只读主表）。"""
import glob
import importlib.util
import io
import os

LEDGER = r"D:/4/Unity/资料/完全复刻/账"
PATCH = os.path.join(LEDGER, "_parts", "B92_L1.tsv")
OUTMD = r"D:/4/Unity/资料/完全复刻/报告/B92_L1.md"

spec = importlib.util.spec_from_file_location("C", r"d:/4/Unity/工具/check_ledger_points.py")
C = importlib.util.module_from_spec(spec)
spec.loader.exec_module(C)

DOWNGRADED = {
    ("教程_引导.tsv", "BattleTutorialManager.Initialize()"),
    ("未分类_其他.tsv", "ListExtensions.SpliceWhere<T>(IList<T> list, Func<T, bool> predicate, int count)"),
    ("未分类_其他.tsv", "ListExtensions.GetMax<T, TU>(IEnumerable<T> data, Func<T, TU> f)"),
    ("未分类_其他.tsv", "ListExtensions.GetMin<T, TU>(IEnumerable<T> data, Func<T, TU> f)"),
    ("泛型与启动链_补读.tsv", "ShopStore<TSave,.GetBaseSave()"),
    ("社交_聊天.tsv", "AllianceInvitationEntry.OnDestroy()"),
    ("菜单_外壳.tsv", "AdaptFlexibleGridLayoutByAspectRatio.OnDisable()"),
    ("菜单_外壳.tsv", "AdaptFlexibleGridLayoutByAspectRatio.OnEnable()"),
    ("轮抽.tsv", "DraftEventBase.GetBaseData()"),
}


def read_rows(path):
    b = io.open(path, "rb").read()
    return b.decode("utf-8-sig").replace("\r\n", "\n").split("\n")


def cut(s, n):
    s = s.replace("\n", " ").strip()
    return s if len(s) <= n else s[:n] + "…"


def main():
    # 1) 主表现为 (类,方法) -> 该行（只收 A）
    main_rows = {}
    for p in sorted(glob.glob(os.path.join(LEDGER, "*.tsv"))):
        fn = os.path.basename(p)
        if fn.startswith("_"):
            continue
        for l in read_rows(p):
            if not l.strip():
                continue
            c = l.split("\t")
            if len(c) != 11:
                continue
            main_rows.setdefault((c[1].strip(), c[2].strip()), []).append((fn, c))

    # 2) 补丁
    plines = [l for l in read_rows(PATCH) if l.strip()]
    patch = {}
    for l in plines[1:]:
        c = l.split("\t")
        dom = c[0].strip().replace("/", "_")
        hits = main_rows.get((c[1].strip(), c[2].strip()), [])
        cand = [h for h in hits if h[0][:-4] == dom] or hits
        fn = cand[0][0] if cand else "?"
        patch[(fn, "%s.%s" % (c[1].strip(), c[2].strip()))] = c

    # 3) 现读闸：哪 113 条原本失败/漂移
    groups = {"noref": [], "shape": [], "drift": []}
    for p in sorted(glob.glob(os.path.join(LEDGER, "*.tsv"))):
        fn = os.path.basename(p)
        if fn.startswith("_"):
            continue
        _t, fails, drifts, _rw, _g, _w = C.check_file(p, list(C.DEFAULT_ROOTS))
        for who, land, why in [(w, ld, wy) for (_f, w, ld, wy) in fails]:
            key = (fn, who)
            if key not in patch:
                continue
            g = "noref" if "落点里没有" in why else "shape"
            groups[g].append((key, land, why))
        for who, land, why in [(w, ld, wy) for (_f, w, ld, wy) in drifts]:
            key = (fn, who)
            if key not in patch:
                continue
            groups["drift"].append((key, land, why))

    L = []
    A = L.append
    A("# B92_L1 —— 落点闸：15 漂移 + 98 失败 → 0 / 0")
    A("")
    A("## §1 一句话")
    A("")
    A("修了 **113 条**（15 漂移 + 98 失败）：**104 条**只改 `我们的落点`（状态仍 A），"
      "**9 条**判「无对应物」并把状态 **A→B** ⇒ 补丁合并后 A 由 5163 降到 **5154**；"
      "**没有判 D 的**。⛔ 未改任何 `.cs`、未改账主表、未跑 Unity、未动 git。")
    A("")
    A("修法一句话：**落点必须落在一个「真声明行」上**，且括号里/行号后的名字要与该行声明的名字一致；"
      "同一格里只写一个 `文件:行`，第二处改用不带行号的散文。")
    A("")
    A("## §2 逐条对照")
    A("")
    A("### §2·A「落点里写了文件、但没有 `文件:行`」—— %d 条（只补行号，状态不变）" % len(groups["noref"]))
    A("")
    A("| # | 账 / 类.方法 | 原落点 | 新落点 | 依据 |")
    A("|---|---|---|---|---|")
    n = 0
    for (fn, who), land, why in sorted(groups["noref"]):
        c = patch[(fn, who)]
        n += 1
        why_cn = "判无对应物 ⇒ 降 B（见 §3/§4）" if (fn, who) in DOWNGRADED             else "现读该文件，取**承载该行为的方法声明行**"
        A("| %d | `%s`<br>`%s` | %s | %s | %s |"
          % (n, fn, cut(who, 70), cut(land, 70), cut(c[6], 110), why_cn))
    A("")
    A("### §2·B「行号指到了注释 / 语句 / 名字对不上 / 文件解析不到」—— %d 条（重钉到真声明行）"
      % len(groups["shape"]))
    A("")
    A("| # | 账 / 类.方法 | 原落点 | 闸的判词 | 新落点 |")
    A("|---|---|---|---|---|")
    n = 0
    for (fn, who), land, why in sorted(groups["shape"]):
        c = patch[(fn, who)]
        n += 1
        A("| %d | `%s`<br>`%s` | %s | %s | %s |"
          % (n, fn, cut(who, 70), cut(land, 70), cut(why, 70), cut(c[6], 110)))
    A("")
    A("### §2·C「漂移 / 歧义」—— %d 条（换到正确的那一处声明）" % len(groups["drift"]))
    A("")
    A("| # | 账 / 类.方法 | 原落点 | 闸的判词 | 新落点 |")
    A("|---|---|---|---|---|")
    n = 0
    for (fn, who), land, why in sorted(groups["drift"]):
        c = patch[(fn, who)]
        n += 1
        A("| %d | `%s`<br>`%s` | %s | %s | %s |"
          % (n, fn, cut(who, 70), cut(land, 70), cut(why, 70), cut(c[6], 110)))
    A("")

    # ---------------- §3 / §4 ----------------
    A("## §3 判「无对应物」的逐条（9 条，全部同时降 B）")
    A("")
    A("判据口径 = 任务书「确实没有对应物 ⇒ 落点写 `无对应物 —— <理由>` + 降 B」，"
      "以及 CLAUDE.md §4.1 的 MEMBER 例外边界（落点可以是字段/属性，但**本仓连那个字段/对象都没有**时不算）。")
    A("")
    A("| # | 账 / 类.方法 | 为什么没有对应物 |")
    A("|---|---|---|")
    S3 = [
        ("教程_引导.tsv", "BattleTutorialManager.Initialize()",
         "原版 `Initialize` 全文只有两个**单例字段缓存**（`+0x38` BattleManager / `+0x40` PlayerDataManager），"
         "零行为；本仓 `BattleDriver` 本身就是那个 BattleManager、`Ctx` 直接可用 ⇒ **不需要缓存**，也没有可指的行。"),
        ("未分类_其他.tsv", "ListExtensions.SpliceWhere<T>(IList<T> list, Func<T, bool> predicate, int count)",
         "原版通用扩展；本仓等价 = 各调用点**内联** `List.RemoveAll` / LINQ"
         "（`Shell/BaseOfferPopup` 的奖励绘制链、`Deck/DeckRuntime` 的摘空等）。现读**找不到**任何"
         "「按谓词删至多 N 条」的单点实现 ⇒ 无方法级单点。"),
        ("未分类_其他.tsv", "ListExtensions.GetMax<T, TU>(IEnumerable<T> data, Func<T, TU> f)",
         "同上：本仓等价 = 各调用点 `Select(...).Max()` / 手写比较。现读 `Shell/DailyData` 里**没有**"
         "`Target` 取最大那一段（旧判据写的「DailyData 的 Target 用法」在现读代码里对不上）⇒ 无单点。"),
        ("未分类_其他.tsv", "ListExtensions.GetMin<T, TU>(IEnumerable<T> data, Func<T, TU> f)",
         "同上：`Shell/LiveOpsEventWindow` / `RankedRewardEventWindow` 的 boost 段现读**没有** `.Min()` 取最小；"
         "全仓 `.Min(` 只命中 `Mathf.Min`（钳位，不是数据取最小）⇒ 无单点。"),
        ("泛型与启动链_补读.tsv", "ShopStore<TSave,.GetBaseSave()",
         "原版 = `return (TSave)this.<字段@0x38>`，**纯取值器**；我们的「本店存档」= 静态表 `Available` 列 + "
         "`InventoryStore` extras，**没有那个 `TSave` 对象** ⇒ 连结构等价的字段落点都不存在。"),
        ("社交_聊天.tsv", "AllianceInvitationEntry.OnDestroy()",
         "原版 = 对两颗钮的 `UnityEvent` 各 `RemoveAllListeners`；本仓按钮走闭包、随节点销毁 ⇒ 没有要摘的东西，"
         "**原版这条本身也没有可观察效果**（铁律 11 例外①那一档的旁支）。"),
        ("菜单_外壳.tsv", "AdaptFlexibleGridLayoutByAspectRatio.OnDisable()",
         "原版只做 `Signal.Unregister`；我们设计空间**钉高 1080**（`Core/LayoutSpace.DesignHeight`）⇒ 该适配器的 "
         "`itemSize = 原值 × s` **恒等** ⇒ 既不需要注册也不需要退订。本仓**没有这个组件**。"),
        ("菜单_外壳.tsv", "AdaptFlexibleGridLayoutByAspectRatio.OnEnable()",
         "原版 = `Signal.Register` + `itemSize = 原值 × s`；钉高 1080 ⇒ `s` 恒 1、`CardH = 414.4` 已是那一档 "
         "⇒ 既不需要订阅也不需要每次重算。本仓**没有这个组件**。"),
        ("轮抽.tsv", "DraftEventBase.GetBaseData()",
         "原版 = `return (TData)this.<字段@0x220>`，**纯取值器**；我们的局数据 = `DraftModeWindow` 的静态 `_run`，"
         "**没有同形的 `Data` 访问器** ⇒ 无结构等价落点。"),
    ]
    for i, (fn, who, why) in enumerate(S3, 1):
        A("| %d | `%s`<br>`%s` | %s |" % (i, fn, cut(who, 70), why))
    A("")
    A("## §4 降档的逐条（A→B，共 9 条）")
    A("")
    A("上表 9 条**全部**由 A 降到 B —— 因为它们都拿不出「方法级/结构级落点」。降 B 的含义按方案 §3·2："
      "**行为面不是没做，而是本仓以「结构性/内联」方式承载它，拿不出单点方法级对应**；"
      "B 不是「不做」，是「没有可机检的落点」。")
    A("")
    A("| # | 账 / 类.方法 | 现在留在哪儿（缺什么） |")
    A("|---|---|---|")
    S4 = [
        ("教程_引导.tsv", "BattleTutorialManager.Initialize()", "缺一个「单例缓存」的显式落点；行为已由 `BattleDriver` + `Ctx` 结构性替代。"),
        ("未分类_其他.tsv", "ListExtensions.SpliceWhere<T>(…)", "缺单点实现；只在各调用点内联。"),
        ("未分类_其他.tsv", "ListExtensions.GetMax<T, TU>(…)", "缺单点实现；只在各调用点内联。"),
        ("未分类_其他.tsv", "ListExtensions.GetMin<T, TU>(…)", "缺单点实现；只在各调用点内联。"),
        ("泛型与启动链_补读.tsv", "ShopStore<TSave,.GetBaseSave()", "缺 `TSave` 那一层存档对象（我们是静态表 + extras）。"),
        ("社交_聊天.tsv", "AllianceInvitationEntry.OnDestroy()", "缺「显式退订」；我们的按钮闭包随节点销毁。"),
        ("菜单_外壳.tsv", "AdaptFlexibleGridLayoutByAspectRatio.OnDisable()", "缺该适配器组件；钉高 1080 ⇒ 恒等。"),
        ("菜单_外壳.tsv", "AdaptFlexibleGridLayoutByAspectRatio.OnEnable()", "缺该适配器组件；钉高 1080 ⇒ 恒等。"),
        ("轮抽.tsv", "DraftEventBase.GetBaseData()", "缺 `Data` 访问器（局数据在 `DraftModeWindow._run`）。"),
    ]
    for i, (fn, who, why) in enumerate(S4, 1):
        A("| %d | `%s`<br>`%s` | %s |" % (i, fn, cut(who, 70), why))
    A("")
    A("> ⚠️ 其中 **3 条 ListExtensions** 与 **3 条「纯取值器/恒等适配」**属同一性质："
      "**不是缺复刻，是缺「落点形状」**。若调度台要求保留 A，唯一合规做法是给它们各写一个带行为的薄壳方法"
      "（= 为工具写代码，CLAUDE.md 明确反对）⇒ 本代理选择降 B。")
    A("")

    # ---------------- §5 ----------------
    A("## §5 验了什么")
    A("")
    A("1. **补丁自检**：`python -I -X utf8 d:/4/Unity/工具/merge_ledger_parts.py --dry --only B92_L1.tsv`")
    A("   ⇒ `合计：更新 113 · 新增 0 · 无变化 0`（**无报错、无「域归一」提示、无歧义**）。")
    A("   补丁本体：114 行（1 表头 + 113 数据）、**每行 11 列**、**裸 CR = 0**、`方法` 列全部是单方法签名。")
    A("2. **落点闸现读验证**（⚠️ 主表不能由本代理改 ⇒ 用**临时副本**跑）：")
    A("   把 `B92_L1.tsv` 应用进 `d:/4/_tmp_view/b92_l1_sim/`（主表的完整副本，按 merge 的"
      "「(原版类,方法) + 域消歧」口径落行），再对**副本**跑")
    A("   `check_ledger_points.py --all-A`：")
    A("   ```")
    A("   合计：A 5154 条 · 通过 5154（其中告警 289 条）· 漂移 0 · 失败 0")
    A("   ```")
    A("   A 5154 = 5163 − 9（降 B 那 9 条）。⚠️ `--fix` 的第一遍输出是**修前快照**，"
      "上面的数字来自**完整重跑**，不是 `--fix` 那一行。")
    A("3. **没做的事（红线）**：⛔ 未改任何 `.cs`（Unity 批处理在跑）· ⛔ 未改账主表（只写 `_parts/`）· "
      "⛔ 未动 git · ⛔ 未跑 Unity。")
    A("4. **本代理写的文件**：`账/_parts/B92_L1.tsv`（补丁）· `报告/B92_L1.md`（本报告）· "
      "`账/_parts/_gen_B92_L1{,_verify,_report}.py`（生成/验证脚本，可复跑）。")
    A("")

    # ---------------- §6 ----------------
    A("## §6 顺手发现")
    A("")
    A("1. **`RuleCore.FieldAttack` 的第二处「声明」是假的**：闸报「有 2 处声明：5009, 6569」，"
      "而 `:6569` 现读是**调用行** `&& FieldAttack(ctx, contP, contUnit, true) <= 0)`。"
      "`decl_name_at` 对「以 `&&` 开头、名字前面非空」的行会返回 `FieldAttack` ⇒ 假候选。"
      "**后果**：`--fix` 因「歧义」永远不敢改这些行（本批 4 条 FieldAttack 行都是这么卡住的）。"
      "**建议**（不在本批权限内）：`decl_name_at` 应以运算符/`&&`/`||`/`,` 开头的行返回 `None`。"
      "本批是**手工选 5009** 绕过的。")
    A("2. **三处旧行号已过期很久，且都漂进了注释/别的方法体**：`RuleCore.cs:16269`（现为 `GameStateHand` "
      "的注释区，真声明在 `16311`）· `:8432`（现为注释，真声明 `8453`）· `:11351`（现为注释，"
      "真声明 `11386`）· `:8183`（现读是**上一个方法的** `return any;`，真声明 `8204`）。"
      "⇒ 说明这几行的行号快照很久没重钉过（`--fix` 只会按名字重定位，名字在括号里写成散文时它就不动）。")
    A("3. **`RuleCore.FireTriggerOnSide` 的 3 参重载在 `:13401`（不是旧注里的 `:13042`）**，"
      "它只转调 4 参重载 `:13420`；本批 4 条（AddTriggerUprising / BroadcastTacticPlayed / "
      "ResolveTriggerUprising / TriggerUprising）**统一钉到 4 参真实现 `:13420`**。")
    A("4. **一个方法覆盖原版多个方法（合法，属方案 §4.1）**：`CardView.RaceTerm`（`Core/CardView.cs:5401`）"
      "同时承载 `CardRaceToString` / `MinionRaceToString` / `StratagemTypeToString` 三条账行 —— "
      "本批 2 条都钉到同一行，**不是**「落点写重了」。")
    A("5. **重复键（合并工具会按「域」消歧，本次只改到主题表那一半）**："
      "`LoginManager.IsActivatedLanguage(string currentLanguage)` 在 `其他_基础设施.tsv:316`（A）"
      "与 `泛型与启动链_补读.tsv:809`（**B**）两份都有。本批按域只更新了前者；后者是 B、过闸本来就不查落点，"
      "**但它的落点列仍是旧散文**，与 `:316` 的备注里那条待办（「另一份重复行仍在 …」）是同一件事 ⇒ 仍待人工一次性对齐。")
    A("6. **`Shell/EventBadge.cs:99` 的 `Initialize(ILiveOps)` 会触发 SELFREF 告警**（它是转发口，"
      "真体在同文件 `:102` 的两参重载）。本批按**原版签名**保留 `:99`（告警不算失败，但复核时会被列出来）；"
      "若要零告警，只能改指 `:102` —— 代价是落点签名与原版不一致，故未改。")
    A("7. **告警数 295 → 289**（净 **−6**，逐条 diff 过 warn 集合）：本批**新增 3 条**——"
      "`TitleDrawer.GetItemName`→`ProfileData.TitleName:372` 与 `VarsDevice…`→`HandLayout.HoverLiftPxForDevice:1037` "
      "的 `MEMBER`（属性当落点，属 §4.1 例外）+ `EventBadge.Initialize:99` 的 `SELFREF`（转发口）；"
      "**消失 9 条**旧 `PROSE` 告警（旧落点括号里塞了多个 ASCII 标识符，改成一格一个名字后不再触发）。"
      "⇒ 告警整体是**减少**的，不是新增缺陷。")
    A("")

    body = "\n".join(L)
    io.open(OUTMD, "wb").write(body.encode("utf-8"))
    print("写出 %s（§2 合计 %d 条）" % (OUTMD, sum(len(v) for v in groups.values())))
    for k, v in groups.items():
        print("  ", k, len(v))


main()
