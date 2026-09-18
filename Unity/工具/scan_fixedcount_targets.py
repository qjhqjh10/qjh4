# -*- coding: utf-8 -*-
"""scan_fixedcount_targets.py —— 普查「**会走到 `ResolveTargets` 那条固定个数（非随机）支**」的目标。

**要回答的问题（待办第 11 行 ⑤「一处没量」）**：
  `RuleCore.ResolveTargets` 里那条 `!Random && Count > 0` 的**退化支**服务**哪些卡**？

**那条支长什么样**（`Assets/RuleEngine/Core/EffectResolver.cs:741-749`，判据抄在下面）：

```csharp
if (spec.Count == 0)                         list.AddRange(pool);            // `all`
else if (!string.IsNullOrEmpty(spec.PickMost)) … 挑最高/最低，不掷骰          // `the enemy with highest attack`
else if (spec.Random && spec.Count < pool.Count) … 用 ctx.Rng 抽 Count 个      // `a random enemy`
else {                                                                        // ★ 退化支
    int n = 0;
    foreach (var u in pool) { list.Add(u); if (++n >= spec.Count) break; }     // 取**池序前 Count 个**
}
```

⇒ 走到退化支 = **固定个数、不掷骰**：按**池序取前 N 个**。池序 = `AddSide` 的棋盘顺序
（`EffectResolver.cs:635-636`），也就是「**槽号最小的那几个**」—— 这条「定死、不随机」的规矩是
本工程铁律点名的（「定死的规则不要改成随机」）。

**怎么从卡表里认出来（判据）**：**不去猜英文句子**，而是读**解析器自己的输出** ——
逐句解析探针 `_tmp_view/probe_out.txt` 把每一条目标打成
`目标[<方>/<类> ×<Count> <标记> 「<原文>」]`（`EffectParseProbe.Target`，`EffectParseProbe.cs:177`）。
于是：

- **有 `×N`（N≥1）** ⇔ `spec.Count != 0`（探针只在非 0 时打 `×`）
- **没有 `随机`** ⇔ `spec.Random == false`
- **没有 `挑=`** ⇔ `spec.PickMost` 为空
⇒ 三者同时成立 = **正好落进退化支**。

**先被前面那些分支 `return` 掉的，一律不算**（它们在退化支之前就返回了）：
`只在手牌`(`HandOnly`，:528) · `prev`(:536) · `eventtarget`(:555) · `没写主语`(`Subjectless`，:566) ·
`已部署`(`Deployed`，:592) · `被本单位打过`(`AttackedBySelf`，:612) · `相邻`(`Adjacent`，:660，含 `AnchorInSet`)。
⚠️ **`自动`（`Auto`）不在此列** —— 它只在 `EffectResolver.cs:4286` 那一处被读，
**不在 `ResolveTargets` 里**，所以带它的 spec 照样走这条支。
⚠️ **`第二目标[…]`（`op.Target2`）也在内** —— 它经 `DefenderPool` 调 `ResolveTargets`（:1334-1346）。
⚠️ **`死后转给[…]`（`op.DeathWatchTarget`）也在内** —— `FlushDeathWatches` 那侧会把它当成新 op 的
`Target` 再走一遍（:4301-4315）。

**判不出来的，如实标「待核」**，不许猜（项目红线）：
  · `随机` 且 `Count >= 池子大小` ⇒ **也会落进这支**，但**要跑起来才知道当时池子有几个单位**。
    ⚠️ 实测把这条推到底：两条支在池子不够大时**取的是同一批**（退化支取整池、随机支的循环也是
    「抽到抽不动为止」= 整池）⇒ **集合等价**，只差顺序 ⇒ 单列一节附注，**不算真差异**。

**输入**：`d:/4/_tmp_view/probe_out.txt`（探针快照）· `cards_engine.json`
（探针读法与「desc→卡」的映射**不另写一份**：转调 `工具/zh_crosscheck.py` 的
`parse_probe` / `make_cards_for` —— 本工程规矩：同一条判据别写两份）
**输出**：`d:/4/_tmp_view/fixedcount_targets_0918.md`（清单）+ stdout 汇总

用法：`PYTHONIOENCODING=utf-8 python d:/4/Unity/工具/scan_fixedcount_targets.py`
"""
import io
import json
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)
import zh_crosscheck as Z            # noqa: E402  （转调它的探针读法与 desc→卡 映射）

OUT_MD = "d:/4/_tmp_view/fixedcount_targets_0918.md"
OUT_TSV = "d:/4/_tmp_view/fixedcount_targets_0918.tsv"

TARGET_RE = re.compile(r"(目标|第二目标|死后转给)\[([^\]]*)\]")
COUNT_RE = re.compile(r"×(\d+)")
# 🔴 **这道闸不能少**：只有这些**动词**的 handler 会调 `ResolveTargets`。
# 出处 = `EffectResolver.EffectDispatch`（`EffectResolver.cs:328-380`）+ 逐个 handler 里的调用点：
#   deal:1138 · eachunitdeal:1519 · forceattack:1268（`Target2` 走 `DefenderPool`:1343）· heal:1571 ·
#   return:2926 · destroy:1875 · stun:1906 · blind:1937 · sethealth:5245 · double:5317 ·
#   ferocitystay:5356 · extratrigger:3462 · takecontrol:3507 · triggerability:5406 · give/gain/lose:4253
# **反例（同一个 `目标[…]` 长在 op 上、但那条支根本不会被调到）**：
#   `gainquest` / `gainenergy` / `gainfaith` / `gainspirit` —— 这四个是**专用 handler**
#   （`DoFactionResource` / `DoEnergy`，给**玩家**加计数，不碰场上单位）。
#   实测它们的 `目标[own/player ×1 自动 「(玩家任务点)」]` 看着完全符合判据，
#   但**一眼不看动词就会多算 38 处** —— 这正是本工程那条「认得出 ≠ 判得了」。
# ⚠️ `atturn` / `persist` / `repeat` / `chooseone` 的正文是**结算期再解析一遍**的：
#   探针把内层 op 也打出来了（`↳回合内层` / `↳重复内层` / `↳修饰指向`），
#   所以**内层按它自己的动词判**，别按外层那几个动词判。
RESOLVE_VERBS = {
    "deal", "eachunitdeal", "forceattack", "heal", "return", "destroy", "stun", "blind",
    "sethealth", "double", "ferocitystay", "extratrigger", "takecontrol", "triggerability",
    "give", "gain", "lose",
}
# 退化支**之前**就 `return` 的分支（命中任一个 ⇒ 不走这支）
EARLY = [
    ("只在手牌", "HandOnly（`EffectResolver.cs:528`）"),
    ("没写主语", "Subjectless（:566）"),
    ("已部署", "Deployed（:592）"),
    ("被本单位打过", "AttackedBySelf（:612）"),
    ("相邻(", "Adjacent（:660，含 `AnchorInSet`）"),
]


def verb_of(piece):
    m = re.match(r"^([a-z]+)", piece)
    return m.group(1) if m else ""


def classify_seg(where, seg):
    """→ (判定, 说明)。判定 ∈ {固定个数(退化支), 全要, 随机抽, 挑最高最低, 别的分支, 待核}"""
    if seg.startswith("prev/"):
        return "别的分支", "指代上一条效果的目标（`prev`，:536）—— 在退化支之前就 return"
    if seg.startswith("eventtarget/"):
        return "别的分支", "事件的宾语（`eventtarget`，:555）—— 在退化支之前就 return"
    for mark, why in EARLY:
        if mark in seg:
            return "别的分支", why
    n = COUNT_RE.search(seg)
    if not n:
        return "全要", "`Count == 0`（`all` / 复数）—— 走 `list.AddRange(pool)` 那一支（:704）"
    cnt = int(n.group(1))
    if "挑=" in seg:
        return "挑最高最低", "`PickMost`（:708）—— 按最高/最低挑，**不掷骰**"
    if " 随机" in seg or seg.endswith("随机"):
        return "随机抽", "`Random && Count < pool.Count`（:730）—— 用 `ctx.Rng` 抽；" \
                         "⚠️ 但 `Count >= 池子大小` 时会落回退化支（**结果等价**，只差顺序）⇒ 见文末附注"
    return "固定个数(退化支)", "固定 %d 个、不随机 ⇒ 按**池序取前 %d 个**（槽号小的优先）" % (cnt, cnt)


def main():
    if not os.path.exists(Z.PROBE):
        sys.stderr.write("没有 %s —— 先跑 EffectParseProbe.Run\n" % Z.PROBE)
        return 1
    probe = Z.parse_probe(Z.PROBE)
    cards = json.load(io.open(Z.CARDS, encoding="utf-8"))["cards"]
    cards_for = Z.make_cards_for(cards)

    rows, todo, total_specs, skipped_verb = [], [], 0, 0
    for desc, blk in probe.items():
        cs, clause = cards_for(desc)
        names = sorted({c.get("name") or "?" for c in cs}) if cs else ["（卡池里找不到）"]
        ids = sorted({c.get("id") or "?" for c in cs}) if cs else []
        factions = sorted({c.get("faction") or "?" for c in cs}) if cs else []
        types = sorted({c.get("type") or "?" for c in cs}) if cs else []
        # op 逐条看（外层 + 内层；内层按**它自己的动词**判 —— 见 `RESOLVE_VERBS` 的注释）
        for o in blk["ops"]:
            for piece, is_inner in [(o["text"], False)] + [(x, True) for x in o["inner"]]:
                if verb_of(piece) not in RESOLVE_VERBS:
                    skipped_verb += len(TARGET_RE.findall(piece))
                    continue
                for m in TARGET_RE.finditer(piece):
                    total_specs += 1
                    where, seg = m.group(1), m.group(2)
                    verdict, why = classify_seg(where, seg)
                    if verdict == "固定个数(退化支)":
                        rows.append({"names": names, "ids": ids, "factions": factions,
                                     "types": types, "where": where, "seg": seg,
                                     "op": piece, "why": why, "clause": clause,
                                     "pline": blk.get("line", 0), "desc": desc,
                                     "verb": verb_of(piece), "inner": is_inner,
                                     "auto": "自动" in seg, "groups": list(o["groups"]),
                                     "kind": blk["segs"][0]["kind"] if blk["segs"] else ""})
                    elif verdict == "随机抽":
                        n = COUNT_RE.search(seg)
                        todo.append({"names": names, "ids": ids, "where": where, "seg": seg,
                                     "op": piece, "cline": clause, "pline": blk.get("line", 0),
                                     "cnt": int(n.group(1)) if n else 0})

    # ---- 汇总：按**目标短语**归并（同一个短语往往服务好几张卡）
    byphrase = {}
    for r in rows:
        raw = re.search(r"「([^」]*)」", r["seg"])
        key = (r["seg"].split("「")[0].strip(), raw.group(1) if raw else "")
        byphrase.setdefault(key, []).append(r)
    ncards = len({c for r in rows for c in r["names"]})

    with io.open(OUT_TSV, "w", encoding="utf-8", newline="\n") as f:
        f.write("卡名\tID\t阵营\t类型\t标记位\t目标段\top\t目标原文\t出处\n")
        for r in rows:
            f.write("\t".join([
                "/".join(r["names"]), "/".join(r["ids"]), "/".join(r["factions"]),
                "/".join(r["types"]), r["where"], r["seg"], r["op"][:160],
                r["desc"][:120], "probe_out.txt:%d" % r["pline"],
            ]) + "\n")

    with io.open(OUT_MD, "w", encoding="utf-8", newline="\n") as f:
        f.write("# `ResolveTargets` 那条**固定个数（非随机）**支服务哪些卡 —— 普查 2026-09-18\n\n")
        f.write("判据与出处的完整说明在脚本文件头（`工具/scan_fixedcount_targets.py`）。\n")
        f.write("一句话判据：**探针输出里 `目标[…]` 带 `×N`（N≥1）、不带 `随机`、不带 `挑=`、"
                "且不是前面那些会提前 return 的标记** ⇒ 正好落进 `EffectResolver.cs:741-749` 那条支。\n\n")
        f.write("- 探针 desc 块 **%d** · 会调 `ResolveTargets` 的目标段 **%d** · "
                "**固定个数（退化支）%d 处 · 卡 %d 张**\n"
                % (len(probe), total_specs, len(rows), ncards))
        f.write("- （另有 **%d 处** `目标[…]` 长在**不调 `ResolveTargets` 的动词**上 —— 已按动词闸排除）\n"
                % skipped_verb)
        f.write("- `随机` 那一批（池子不够大时也会漏到这支，**结果等价**）：**%d 处**（见文末附注）\n\n"
                % len(todo))
        # ---- 子族：这三种切法回答「这条支实际在替谁做决定」
        def _tally(keyfn):
            d = {}
            for r in rows:
                d.setdefault(keyfn(r), []).append(r)
            return sorted(d.items(), key=lambda kv: -len(kv[1]))
        f.write("## 子族（同一条支，后果不一样）\n\n")
        f.write("### 按标记位\n\n")
        for k, v in _tally(lambda r: r["where"]):
            f.write("- `%s[…]`：%d 处 / %d 张\n" % (k, len(v), len({c for r in v for c in r["names"]})))
        f.write("\n### 按动词\n\n")
        for k, v in _tally(lambda r: r["verb"]):
            f.write("- `%s`：%d 处 / %d 张\n" % (k, len(v), len({c for r in v for c in r["names"]})))
        f.write("\n### 按「谁在挑」（`Auto` / 外层 op / 嵌套内层）\n\n")
        f.write("- `自动`（`EffectOp` 上的 `Auto`，**引擎替玩家挑** ⇒ 结果不会被 `chosen` 覆盖）：**%d 处 / %d 张**\n"
                % (len([r for r in rows if r["auto"]]),
                   len({c for r in rows if r["auto"] for c in r["names"]})))
        f.write("- 嵌套内层（`↳回合内层` / `↳重复内层` / `↳修饰指向` —— 结算期再解析，`chosen` 一律是 `null`）："
                "**%d 处 / %d 张**\n" % (len([r for r in rows if r["inner"]]),
                                       len({c for r in rows if r["inner"] for c in r["names"]})))
        f.write("- 其余（战术卡外层 op；玩家点了目标时 `chosen` 会在 `:752` **覆盖**这条支挑出来的那个）："
                "**%d 处**\n\n" % len([r for r in rows if not r["auto"] and not r["inner"]]))
        f.write("## 按目标短语归并\n\n")
        for key in sorted(byphrase, key=lambda k: -len(byphrase[k])):
            mark, raw = key
            f.write("### `%s「%s」` —— %d 处\n\n" % (mark, raw, len(byphrase[key])))
            for r in byphrase[key]:
                f.write("- **%s**（%s · %s · %s）\n"
                        "  - 卡面：`%s`\n"
                        "  - op：`%s`\n"
                        "  - 出处：probe_out.txt:%d\n"
                        % ("/".join(r["names"]), "/".join(r["ids"]), "/".join(r["factions"]),
                           "/".join(r["types"]), r["desc"][:180], r["op"][:160], r["pline"]))
            f.write("\n")
        f.write("## 附注：`随机` 的那批（**同一支的第二张脸**，结果等价）\n\n")
        f.write("`随机抽` 那一支的判据是 `spec.Random && spec.Count < pool.Count`（:730）——\n"
                "**池子不够大时它会漏下去、由退化支接**。但两条支在那种情形下**取的是同一批**：\n"
                "退化支取 `min(Count, pool.Count)` = 整池；随机支的循环也是「抽到抽不动为止」= 整池。\n"
                "⇒ **集合相同**，唯一的差别是**顺序**（退化支按槽号、随机支按 `ctx.Rng`）——\n"
                "而顺序只影响 `ctx.LastTarget`（下一句 `it` 指谁，`ResolveTargets:760`）。\n"
                "**静态判不了**的是「那一局池子到底有几个」⇒ 这里只列不判。共 **%d 处**：\n\n"
                % len(todo))
        for t in todo:
            f.write("- **%s**：`%s` —— `随机`；个数 %d，池子里不足 %d 个时会漏到退化支"
                    "（probe_out.txt:%d）\n" % ("/".join(t["names"]), t["seg"], t["cnt"], t["cnt"],
                                               t["pline"]))

    print("探针 desc 块 %d · 会调 ResolveTargets 的目标段 %d（另有 %d 处动词闸挡掉）"
          % (len(probe), total_specs, skipped_verb))
    print("**固定个数（退化支）：%d 处 · 卡 %d 张**" % (len(rows), ncards))
    print("按目标短语：")
    for key in sorted(byphrase, key=lambda k: -len(byphrase[k])):
        print("   %-34s 「%s」 ×%d" % (key[0], key[1], len(byphrase[key])))
    print("随机那一批（池子不够时漏到这支、结果等价）：%d 处" % len(todo))
    print("→ %s" % OUT_MD)
    return 0


if __name__ == "__main__":
    sys.exit(main())
