#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""B93_E26：只改「我们的落点」列里**第二处及以后**的陈旧 `文件:行`（Q10 分类里的可施工那批）。

输入（只读）：
  · 判据/B93_Q10_SECOND204.tsv      （205 条 SECOND 告警的逐条分类）
  · 账/*.tsv                        （主表，**只读**；本脚本一个字不写主表）

输出：
  · 账/_parts/B93_E26.tsv           （11 列补丁，只含改过的行，其它列逐字照抄主表）

纪律：
  · 裸 `split("\t")` 读写（⛔ csv 模块）
  · 每条都**现读** `.cs`（二进制读 + 自己按 \\r\\n / \\n 切行）复核，⛔ 不拿 Q10 的行号直接写
  · 只改「我们的落点」列里的那一处 `文件:行`；其它 10 列逐字照抄

用法：
  python -I -X utf8 d:/4/Unity/资料/完全复刻/账/_parts/_gen_B93_E26.py            # 只打印核验结果（不写盘）
  python -I -X utf8 d:/4/Unity/资料/完全复刻/账/_parts/_gen_B93_E26.py --apply    # 真写补丁
"""
import io
import os
import re
import sys
import importlib.util

Q10 = r"d:/4/Unity/资料/完全复刻/判据/B93_Q10_SECOND204.tsv"
LED = r"d:/4/Unity/资料/完全复刻/账"
OUT = r"d:/4/Unity/资料/完全复刻/账/_parts/B93_E26.tsv"
TOOL = r"d:/4/Unity/工具/check_ledger_points.py"

COLS = ["域", "原版类", "方法", "判据出处", "判据要点", "状态",
        "我们的落点", "差异", "等级", "轮次", "备注"]

_MODS = ("public ", "private ", "protected ", "internal ", "static ", "readonly ", "const ",
         "sealed ", "override ", "virtual ", "abstract ", "extern ", "unsafe ", "partial ",
         "new ", "async ", "event ", "volatile ")

# 任务书 §①③ 点名的 6 例「名字对、文件/类错」（Q10 已给真身；BuildCell 明确「别改」）
SPECIAL = {
    "17":  dict(new="CardPresentation/Core/Badges.cs:367", rename=None),
    "28":  dict(new="CardPresentation/Deck/DeckRuntime.cs:4237", rename=("IsAltArt", "IsAltArtCard")),
    "19":  dict(new="CardPresentation/Battle/CardChoicePanel.cs:732", rename=None),
    "20":  dict(new="CardPresentation/Battle/CardChoicePanel.cs:732", rename=None),
    "21":  dict(new="CardPresentation/Battle/CardChoicePanel.cs:732", rename=None),
    "130": dict(new="RuleEngine/Core/RuleCore.cs:3531", rename=None),
    "143": dict(new="RuleEngine/Core/RuleCore.cs:9013", rename=None),
}
# 语义上不是「声明指针」而是「调用点 / 驱动点」的那几条：Q10 的建议列是**声明行**，
#   直接照它改会把散文读成假的（例：`（DeclareAttack 攻方侧预调之后）` 指到方法第一行）。
#   这些行我**现读找到真正的调用行**，并把括号补成「被调方法 · 外层方法」两个标识符
#   （否则闸会按外层方法名继续报 SECOND）。
CALLSITE = {
    "94":  dict(new="RuleEngine/Core/EffectResolver.cs:16594", call="BroadcastEarnedSkull",
                fix=[("（NoteWarlordHealthForSkulls）", "（NoteWarlordHealthForSkulls 里的 BroadcastEarnedSkull 调用）")]),
    "131": dict(new="RuleEngine/Core/RuleCore.cs:7001", call="EnqueueExpireUntilNextAttack",
                fix=[("（DeclareAttack 攻方侧预调之后）",
                      "（EnqueueExpireUntilNextAttack 调用 · DeclareAttack 攻方侧预调之后）")]),
    "163": dict(new="CardPresentation/Battle/BattleDriver.cs:27431", call="RefreshHandPlayable",
                fix=[("（SyncHand）（调用 :26451）", "（SyncHand 里的 RefreshHandPlayable 调用 :27431）")]),
    "165": dict(new="CardPresentation/Battle/BattleDriver.cs:27431", call="RefreshHandPlayable",
                fix=[("（SyncHand）（调用 :26451）", "（SyncHand 里的 RefreshHandPlayable 调用 :27431）")]),
}

# ⛔ 不改（每条都带理由，报告里逐条列）
SKIP = {
    "186": "任务书点名「别改」：全仓三处同名 `BuildCell`（ShopTabPage.cs:484 / ForgeTab.cs:995 / AchievementsMenu.cs:356），需人判",
    "17":  "闸假阳性：这一处 `TraitFrames.cs:61` 是「原版 `stealthPrefab` 是空引用」的**证据指针**（现读该行原文就是那句），"
           "不是 `SpriteOf` 的声明；本行第一处已经正确指向 `Badges.cs:367`。改成 Badges.cs:367 会既重复又毁掉证据",
}


def load_mod():
    spec = importlib.util.spec_from_file_location("clp", TOOL)
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


CLP = load_mod()
ROOTS = list(CLP.DEFAULT_ROOTS)


def read_rows(path):
    """二进制读 + 自己切行（⛔ 文本模式：会把裸 CR 当换行 ⇒ 行号整体偏移）。"""
    b = io.open(path, "rb").read()
    bare_cr = b.count(b"\r") - b.count(b"\r\n")
    t = b.decode("utf-8-sig")
    lines = t.split("\r\n") if (b.count(b"\r\n") and b.count(b"\r\n") == b.count(b"\n")) else t.split("\n")
    return lines, bare_cr


def load_ledgers():
    out = {}
    for fn in sorted(os.listdir(LED)):
        if not fn.endswith(".tsv") or fn.startswith("_"):
            continue
        lines, bare = read_rows(os.path.join(LED, fn))
        out[fn] = dict(lines=lines, bare=bare)
    return out


def norm_dom(s):
    return s.strip().replace("/", "_")


def locate(LEDGERS, dom, lineno, land):
    """按 (域归一, 主表物理行号, 落点原文) 三重指纹找主表那一行。"""
    hits = []
    for fn, L in LEDGERS.items():
        idx = lineno - 1
        if not (0 <= idx < len(L["lines"])):
            continue
        if not L["lines"][idx].strip():
            continue
        f = L["lines"][idx].split("\t")
        if len(f) != 11:
            continue
        if norm_dom(f[0]) == norm_dom(dom) and f[6] == land:
            hits.append((fn, idx, f))
    return hits


def src(rel):
    full, note, exact = CLP.resolve_ref_file(rel, ROOTS)
    if full is None:
        return None, note, exact
    try:
        return CLP.read_src_lines(full), note, exact
    except Exception as e:  # noqa
        return None, "读不了文件：%s" % e, exact


def show(lines, ln):
    if lines is None or not (1 <= ln <= len(lines)):
        return "<越界/无文件>"
    return lines[ln - 1].strip()


def main():
    apply = "--apply" in sys.argv
    q, _ = read_rows(Q10)
    qrows = [l.split("\t") for l in q[1:] if l.strip()]
    LEDGERS = load_ledgers()

    targets = []
    for r in qrows:
        tier = r[8]
        if tier.startswith("C · C-1"):
            targets.append(r)
        elif r[0] in SPECIAL or r[0] in SKIP:
            targets.append(r)
    # 排序：按域、主表行号，便于人读
    targets.sort(key=lambda r: (r[1], int(r[2]), int(r[0])))

    # (主表文件, 行下标) -> 新的 land 文本（同一行可能被改多处）
    edits = {}
    log, bad, unchanged = [], [], []

    for r in targets:
        no, dom, lineno, who = r[0], r[1], int(r[2]), r[3]
        land_q, n, name = r[4], int(r[5]), r[6]
        tier = r[8]
        if no in SKIP:
            unchanged.append((no, dom, lineno, who, name, SKIP[no]))
            continue

        hits = locate(LEDGERS, dom, lineno, land_q)
        if len(hits) != 1:
            bad.append(dict(no=no, dom=dom, lineno=lineno, which="定位主表行", why="命中 %d 处" % len(hits)))
            continue
        fn, ridx, fields = hits[0]
        key = (fn, ridx)
        land = edits[key][1] if key in edits else fields[6]

        ms = list(CLP.REF.finditer(land))
        if len(ms) < n:
            bad.append(dict(no=no, dom=dom, lineno=lineno, which="取第 %d 处" % n,
                        why="这一行现读只有 %d 处 `文件:行`（落点列可能已被别人改过）" % len(ms)))
            continue
        m = ms[n - 1]
        cur_rel, cur_ln = m.group(1), int(m.group(2))

        # ---- ① 现读：当前这一处到底指到哪、那一行是什么 ----
        cur_lines, cur_note, _ = src(cur_rel)
        cur_txt = show(cur_lines, cur_ln)
        stale = cur_lines is not None and CLP.name_lines(cur_lines, name) and cur_ln not in CLP.name_lines(cur_lines, name)

        # ---- ② 目标行（CALLSITE / SPECIAL 给定 / Q10 建议），逐条现读复核 ----
        want_call = None
        if no in CALLSITE:
            new_rel, new_ln = CALLSITE[no]["new"].rsplit(":", 1)
            new_ln = int(new_ln)
            want_name = name
            want_call = CALLSITE[no]["call"]
        elif no in SPECIAL:
            tgt = SPECIAL[no]
            new_rel, new_ln = tgt["new"].rsplit(":", 1)
            new_ln = int(new_ln)
            want_name = tgt["rename"][1] if tgt["rename"] else name
        else:
            s = r[9].strip()
            if ":" not in s or ".cs:" not in s:
                bad.append(dict(no=no, dom=dom, lineno=lineno, which="建议行号", why="Q10 没给唯一建议：%r" % s[:80]))
                continue
            # 取最后一处 `xxx.cs:NNN`（Q10 写法：`候选 A:1（声明）/ 近引用 L2` 少见，这里要求单一）
            import re as _re
            cand = _re.search(r"([\w\-./\\]+\.cs):(\d+)", s)
            if not cand:
                bad.append(dict(no=no, dom=dom, lineno=lineno, which="建议行号", why="Q10 建议里没有 `文件.cs:行`：%r" % s[:80]))
                continue
            new_rel, new_ln = cand.group(1), int(cand.group(2))
            want_name = name

        new_lines, new_note, new_exact = src(new_rel)

        def judge(lines_, ln_):
            """强：能解析成声明（与 `--fix` 同口径）；弱：那一行确实是这个名字的声明式
            （`const string X = …` 这类常量行 / 字段行 —— 闸的 SECOND 只要求名字在行上，
            但 `find_decl_candidates` 不认常量行 ⇒ 降一档、报告里标出来）。
            调用点行（`want_call`）另判：那一行必须**真的在调那个方法**。"""
            if lines_ is None or not (1 <= ln_ <= len(lines_)):
                return None
            if want_call:
                return "callsite" if re.search(r"\b" + re.escape(want_call) + r"\s*\(", lines_[ln_ - 1]) else None
            if ln_ in CLP.find_decl_candidates(lines_, want_name):
                return "strong"
            if ln_ in CLP.name_lines(lines_, want_name):
                l = lines_[ln_ - 1]
                if CLP.looks_like_decl(l)[0] or CLP.member_decl_kind(l, lines_, ln_ - 1) is not None:
                    return "weak"
                # 常量 / 字段行（`const string X = "…";`）—— `looks_like_decl` 不认（无 `(`/`=>`/`{`），
                #   但 `decl_name_at` 取得出这个名字，且行首是修饰符 ⇒ 就是它的声明。
                #   ⚠️ CLAUDE.md 记过：`const string` 常量行不能当 **A 档**落点；这里是 **次要落点**，
                #   它本来就只要求「名字在行上」，弱一档、报告里标出来。
                if l.strip().startswith(_MODS) and CLP.decl_name_at(l) == want_name:
                    return "weak"
            return None

        ok_line = False
        tier_ok = None
        if new_lines is not None and 1 <= new_ln <= len(new_lines):
            cands = CLP.find_decl_candidates(new_lines, want_name)
            tier_ok = judge(new_lines, new_ln)
            ok_line = tier_ok is not None
            note_extra = "候选行=%s" % (cands[:5],)
        else:
            note_extra = "文件解析不了：%s" % new_note

        relocated = False
        if not ok_line and not (no in SKIP):
            # 按符号名重定位（同一份文件内唯一声明才算数）
            if new_lines is None:
                # 目标文件解析不了 ⇒ 退回「当前文件」里找
                new_lines = cur_lines
                new_rel = cur_rel
                new_note = cur_note
            if new_lines is not None:
                cands = CLP.find_decl_candidates(new_lines, want_name)
                if len(cands) == 1:
                    new_ln = cands[0]
                    ok_line = True
                    tier_ok = "strong"
                    relocated = True
                    note_extra += " ⇒ 按符号重定位到 L%d" % new_ln
                else:
                    note_extra += " ⇒ 重定位失败（候选 %s）" % (cands[:5],)

        if not ok_line:
            bad.append(dict(no=no, dom=dom, lineno=lineno, which="目标行复核", why="%s:%d 不是 `%s` 的声明 —— %s"
                        % (new_rel, new_ln, want_name, note_extra)))
            continue

        # ---- ③ 拼新落点：只替换这一处的 `文件:行`（必要时连名字一起改名） ----
        # 最小改动：**目标文件与当前文件是同一个**时，保留账里原来的写法（`Battle/BattleDriver.cs` 这类短名），
        #   只改行号 —— 否则会把一处行号修正变成「顺手改写法」，白增 diff。
        cur_full, _cn, _ce = CLP.resolve_ref_file(cur_rel, ROOTS)
        new_full, _nn, _ne = CLP.resolve_ref_file(new_rel, ROOTS)
        if cur_full is not None and new_full is not None and \
                os.path.normcase(cur_full) == os.path.normcase(new_full):
            new_rel = cur_rel
        new_txt = show(new_lines, new_ln)
        repl = "%s:%d" % (new_rel, new_ln)
        # 只动**这一处**：`[m.start(), 下一处.start())`；其它两处一个字不碰。
        tail_start = m.end()
        tail_end = ms[n].start() if len(ms) > n else len(land)
        tail = land[tail_start:tail_end]
        fixes = []
        if no in SPECIAL and SPECIAL[no]["rename"]:
            fixes.append(SPECIAL[no]["rename"])
        if no in CALLSITE:
            fixes.extend(CALLSITE[no]["fix"])
        for a, b in fixes:
            if a not in tail:
                bad.append(dict(no=no, dom=dom, lineno=lineno, which="括号改写",
                                why="这一处的括号段里没有 %r（现读：%r）" % (a, tail[:120])))
                tail = None
                break
            tail = tail.replace(a, b, 1)
        if tail is None:
            continue
        new_land = land[:m.start()] + repl + tail + land[tail_end:]
        edits[key] = (fields, new_land)
        log.append(dict(no=no, dom=dom, lineno=lineno, who=who, tier=tier, name=name, want=want_name,
                        cur="%s:%d" % (cur_rel, cur_ln), cur_txt=cur_txt, stale=stale,
                        new_out=repl, new_txt=new_txt, relocated=relocated, land_old=land,
                        land_new=new_land, ok=tier_ok, call=no in CALLSITE,
                        fixed_key=(new_rel.replace("\\", "/"), new_ln),
                        old_key=(cur_rel.replace("\\", "/"), cur_ln)))

    # ---- ④ 写补丁（把改过的行按 11 列重拼；其它列逐字照抄）----
    rows_out = []
    for (fn, ridx), (fields, new_land) in sorted(edits.items(), key=lambda kv: (kv[0][0], kv[0][1])):
        f = list(fields)
        f[6] = new_land
        if len(f) != 11:
            print("!! %s:%d 列数 %d ≠ 11" % (fn, ridx + 1, len(f)))
            return 2
        rows_out.append(f)

    # ---- ③b 自检：用闸自己的 `check_extra_refs` 跑「改前 / 改后」两份落点，比对那一条告警是否真消失 ----
    #   ⛔ 只读（不写主表、不改工具）；判据与闸**同一份代码**，避免我另立口径。
    self_bad = []
    for e in log:
        def warned(land_):
            first = CLP.REF.search(land_)
            skip = (first.group(1).replace("\\", "/"), int(first.group(2))) if first else None
            _f, w = CLP.check_extra_refs(land_, ROOTS, skip_key=skip)
            return set((a.replace("\\", "/"), b) for a, b, _ in w)
        before = warned(e["land_old"])
        after = warned(e["land_new"])
        k, ko = e["fixed_key"], e["old_key"]
        # 改前：**旧的那一处**应该在被警告的集合里（说明我改的确实是那条告警）
        if ko not in before:
            self_bad.append((e["no"], "改前这一处并不在 SECOND 告警里（%s:%d）—— 第几处对不上？" % ko))
        if ko in after:
            self_bad.append((e["no"], "旧的 %s:%d 改后仍在告警" % ko))
        if k in after:
            self_bad.append((e["no"], "改后**仍然**告警（%s:%d）" % k))
        if len(after) > len(before):
            self_bad.append((e["no"], "改后告警数变多了（%d → %d）" % (len(before), len(after))))
        e["warn_before"], e["warn_after"] = len(before), len(after)
    print("== 自检（用闸自己的 check_extra_refs 对比改前/改后）==")
    print("改前合计告警 %d 处 → 改后 %d 处（同一批行；差额应 = 改动处数 %d）"
          % (sum(e["warn_before"] for e in log), sum(e["warn_after"] for e in log), len(log)))
    for x in self_bad:
        print("   !! #%s %s" % x)

    print("== 计划 ==")
    print("目标 %d 行（C-1 %d + 特例 %d）· 拟改主表行 %d 行 · 未改 %d · 卡住 %d"
          % (len(targets), sum(1 for r in targets if r[8].startswith("C · C-1")),
             sum(1 for r in targets if r[0] in SPECIAL), len(edits), len(unchanged), len(bad)))
    for e in log:
        print("#%-4s %-12s L%-5d %-52s | %s -> %s%s [%s%s]\n        旧: %s\n        新: %s"
              % (e["no"], e["dom"], e["lineno"], e["who"][:52], e["cur"], e["new_out"],
                 "", e["ok"], " · 重定位" if e["relocated"] else "",
                 e["cur_txt"][:110], e["new_txt"][:110]))
    for u in unchanged:
        print("-- 未改 #%s %s L%d %s | %s：%s" % (u[0], u[1], u[2], u[3][:40], u[4], u[5]))
    for b in bad:
        print("!! 卡住 #%s %s L%s [%s] %s" % (b["no"], b["dom"], b["lineno"], b["which"], b["why"]))

    if not apply:
        print("\n（只打印，未写盘；--apply 才写 %s）" % OUT)
        return 0

    # ---- ⑤ 写前校验：用**合并脚本自己的**判据（表头 / `方法` 签名 / 键唯一）----
    MERGE = importlib.util.spec_from_file_location("merge", r"d:/4/Unity/工具/merge_ledger_parts.py")
    mg = importlib.util.module_from_spec(MERGE)
    MERGE.loader.exec_module(mg)
    led = mg.load_ledgers(LED)
    keyidx = {}
    for fn, L in led.items():
        if L["bad"] or L["bare_cr"]:
            print("!! 主表 %s 有坏行 / 裸 CR（%s / %s）⇒ 合并脚本会拒绝" % (fn, L["bad"][:3], L["bare_cr"]))
            return 2
        for i, f in L["rows"][1:]:
            keyidx.setdefault((f[1].strip(), f[2].strip()), []).append(fn)
    pre = []
    for f in rows_out:
        ok, why = mg.looks_like_one_method(f[2].strip())
        if not ok:
            pre.append("%s.%s：`方法` 不合规（%s）" % (f[1][:30], f[2][:40], why))
        hits = keyidx.get((f[1].strip(), f[2].strip()), [])
        if len(hits) > 1:
            dom_norm = f[0].strip().replace("/", "_")
            cand = [h for h in hits if h[:-4] == dom_norm]
            if len(cand) != 1:
                pre.append("%s.%s：(原版类,方法) 在 %d 份主表里都有（%s）、域 `%s` 命中 %d ⇒ 合并会「请人指定」"
                           % (f[1][:30], f[2][:40], len(hits), ", ".join(hits), f[0], len(cand)))
    if pre:
        print("\n!! 补丁预校验不过（%d 条），**未写盘**：" % len(pre))
        for x in pre:
            print("   " + x)
        return 2
    print("\n✅ 预校验通过：%d 行 × 11 列 · `方法` 全是单方法签名 · (原版类,方法) 键在合并脚本眼里唯一" % len(rows_out))

    lines = ["\t".join(COLS)] + ["\t".join(f) for f in rows_out]
    out = "\n".join(lines) + "\n"
    # 写完立刻读回：11 列 + 逐列往返比对
    io.open(OUT, "wb").write(out.encode("utf-8"))
    back = io.open(OUT, "rb").read().decode("utf-8").split("\n")
    hdr = back[0].split("\t")
    assert hdr == COLS, hdr
    for i, l in enumerate(back[1:], start=1):
        if not l.strip():
            continue
        c = l.split("\t")
        assert len(c) == 11, (i, len(c))
        assert c == rows_out[i - 1], ("往返不一致", i)
    print("\n✅ 写盘 %s（%d 行 + 表头），读回 11 列 × %d 行逐列一致" % (OUT, len(rows_out), len(rows_out)))

    # ---- ⑥ 报告里的逐条表（机械生成，⛔ 不手抄，避免行号/原文抄错）----
    if "--report" in sys.argv:
        rep = []
        rep.append("<!-- 本表由 `账/_parts/_gen_B93_E26.py --report` 机械生成（每格都是**脚本现读**的那一行原文） -->\n")
        rep.append("| # | 域 | 主表行 | 原版类.方法 | 旧次要落点 | 新落点 | 改前那一行现读（证据：它是别的行） | 改后那一行现读（证据：这是声明的原文） | 档 |")
        rep.append("|---|---|---|---|---|---|---|---|---|")
        for e in log:
            rep.append("| %s | %s | %d | `%s` | `%s` | `%s` | %s | **%s** | %s%s |"
                       % (e["no"], e["dom"], e["lineno"], e["who"],
                          e["cur"], e["new_out"],
                          "`" + e["cur_txt"].replace("|", "\\|")[:150] + "`",
                          e["new_txt"].replace("|", "\\|")[:190],
                          e["ok"], "·调用点" if e["call"] else ""))
        table = "\n".join(rep)
        io.open(r"d:/4/_tmp_view/e26/table.md", "wb").write(table.encode("utf-8"))
        print("✅ 逐条表写到 d:/4/_tmp_view/e26/table.md（%d 行）" % len(log))
    return 0


if __name__ == "__main__":
    sys.exit(main())
