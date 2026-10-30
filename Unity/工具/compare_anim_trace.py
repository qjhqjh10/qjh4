#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""compare_anim_trace.py — 播放 trace vs 原版逐卡演出数据表（执行代理 `B104_V2` · 2026-10-31）

======================= 它干什么 =======================
把战斗表现层落下来的**播放 trace**（`Application.persistentDataPath/WarpforgeAnimTrace/*.tsv`，
由 `CardPresentation.AnimTrace` 写、开关 = 环境变量 `WF_ANIM_TRACE=1`）和**原版全表**逐卡对账：

  · 原版侧 = `数据/游戏数据/card_anim_order.json`（928 卡 / 1,527 refs / 551 CardAnim）
             ＋ `数据/游戏数据/animinfo_lookup.json`（418 条 AnimInfo，按件名查，用来补 `card_anim_order`
             里没带参数的那几件）
  · 我们侧 = trace 里 `cueKind == beat` 的行（= 按表展开的每一拍：件名 / 起始偏移 / 阻塞时长）

每张卡比三样：**件名序列** · **起始偏移** · **阻塞时长**，输出 `卡id | 原版值 | 我们值 | 差`。

========================= 用法 =========================
  python -I -X utf8 d:/4/Unity/工具/compare_anim_trace.py                 # 打印用法，**退出 0**（不跑）
  python -I -X utf8 d:/4/Unity/工具/compare_anim_trace.py --check         # 真的跑（默认取最新的 trace）
  python -I -X utf8 d:/4/Unity/工具/compare_anim_trace.py --check --trace <文件.tsv>
  python -I -X utf8 d:/4/Unity/工具/compare_anim_trace.py --check --card SOR36 --tol 0.02 --max-diffs 20

退出码：**0** = 有比到的卡全部一致（或什么都没比到）· **1** = 有差异 · **2** = 用法/IO 错。

======================== 口径（⛔ 别自己另立） =========================
1. **逐卡摊平比**：原版表把一张卡的每一步按「全量件行序 + `steps[].i` 升序」摆好，我们的
   `CardAnimOrder` / `ExpandAbilityBeats` 也是**按这张卡的全部拍**展开 ⇒ 对账按「这张卡的拍序列」摊平比。
2. **起始偏移**：我们侧 = `startOffset − 本组首拍 startOffset`（trace 的 `startOffset` 是 `_clock` 口径的
   **绝对**时刻；`firstOfEvent == 1` 切组）。原版侧 = `steps` 的累加量 `acc`
   （只有 `waitAnimToApplyEffect != 0` 的拍才把 `Duration` 加进 `acc` —— 判据 = `AbilityLogic.PlayAnim`）。
3. **阻塞时长**：我们侧 = trace 的 `holdSeconds`（按表展开的拍 = `CardAnimBeat.Duration`）；原版侧 =
   `vfxDelayTime + timeAtStartPos + timeMoving + timeAtEndPos`（= `AnimInfo.GetAnimDuration()`，**不含** `startDelay`）。
4. **原版的 N 拍是上限**：`AbilityLogic__PlayAnim.c` 运行时还会逐拍跳过（AssetReference 无效 / 起终点是
   `14,15,16,17` 且空位不可用）⇒ **「我们比原版少几拍」可能是合法的运行时跳过**，不是缺陷。
   本工具把它报成 `拍数差`，**要人看具体卡**（⛔ 别自动判红）。
5. 原版侧参数缺失时先查 `animinfo_lookup.json`（键 = 件名）；两处都没有 ⇒ 该拍时长记 `None`（**不猜 0**），
   汇总里单独计数。

⚠️ 本工具**只读**：不写任何数据文件、不改任何表（报告由人写）。
"""

from __future__ import print_function

import glob
import io
import json
import os
import sys

DEFAULT_ORDER = r"d:/4/Unity/数据/游戏数据/card_anim_order.json"
DEFAULT_INFO = r"d:/4/Unity/数据/游戏数据/animinfo_lookup.json"
TRACE_DIR_NAME = "WarpforgeAnimTrace"
COLUMNS = ["seq", "cardId", "evtKind", "piece", "startOffset", "holdSeconds", "seat", "cueKind", "firstOfEvent"]
WAIT_PIECE = "(wait)"          # 原版 `waitAnimToApplyEffect` 的纯延时拍（`vfx` 空串）—— 与 trace 里 `piece` 空对齐

USAGE = __doc__


# ----------------------------------------------------------------------
#  基础
# ----------------------------------------------------------------------
def usage_exit(code=0):
    print(USAGE)
    sys.exit(code)


def die(msg, code=2):
    print("[compare_anim_trace] " + msg)
    sys.exit(code)


def load_json(path):
    with io.open(path, "r", encoding="utf-8-sig") as f:
        return json.load(f)


def find_newest_trace():
    """在 `%USERPROFILE%/AppData/LocalLow/*/*/WarpforgeAnimTrace/*.tsv` 里找 mtime 最新的一份。"""
    cands = []
    root = os.path.join(os.environ.get("USERPROFILE", ""), "AppData", "LocalLow")
    if os.path.isdir(root):
        for comp in sorted(os.listdir(root)):
            cdir = os.path.join(root, comp)
            if not os.path.isdir(cdir):
                continue
            for prod in sorted(os.listdir(cdir)):
                d = os.path.join(cdir, prod, TRACE_DIR_NAME)
                if os.path.isdir(d):
                    cands.extend(glob.glob(os.path.join(d, "*.tsv")))
    # 兜底：本仓 `_tmp_view` 下手工放的那一份（自检/开发常用）
    cands.extend(glob.glob(os.path.join("d:/4/_tmp_view", "**", "*.tsv"), recursive=True))
    cands = [p for p in cands if os.path.isfile(p)]
    if not cands:
        return None
    return max(cands, key=lambda p: os.path.getmtime(p))


def read_trace(path):
    """读 trace → (rows, header, notes)。列名与 `AnimTrace.Columns` 不一致 ⇒ 直接报错（不许猜列）。"""
    rows = []
    header = None
    with io.open(path, "r", encoding="utf-8-sig") as f:
        for ln, line in enumerate(f):
            line = line.rstrip("\r\n")
            if not line:
                continue
            parts = line.split("\t")
            if header is None:
                header = parts
                continue
            if len(parts) != len(COLUMNS):
                die("第 %d 行列数 %d，期望 %d（trace 格式不对 ⇒ 见 AnimTrace.Columns）"
                    % (ln + 1, len(parts), len(COLUMNS)))
            d = dict(zip(COLUMNS, parts))
            d["seq"] = int(d["seq"])
            d["startOffset"] = float(d["startOffset"])
            d["holdSeconds"] = float(d["holdSeconds"])
            d["seat"] = int(d["seat"])
            d["firstOfEvent"] = d["firstOfEvent"] == "1"
            rows.append(d)
    if header is None:
        die("trace 空文件：%s" % path)
    if header != COLUMNS:
        die("trace 表头是 %r，期望 %r（列名/列序变了 ⇒ 先对 `AnimTrace.Columns`）" % (header, COLUMNS))
    return rows, header


# ----------------------------------------------------------------------
#  原版侧
# ----------------------------------------------------------------------
def beat_duration(params, info):
    """一拍的 `AnimInfo.GetAnimDuration()`；查不到参数 ⇒ None（**不猜 0**）。"""
    if params:
        vd = params.get("vfxDelayTime")
        ts = params.get("timeAtStartPos")
        tm = params.get("timeMoving")
        te = params.get("timeAtEndPos")
        if None not in (vd, ts, tm, te):
            return float(vd) + float(ts) + float(tm) + float(te), "card_anim_order"
        if None not in (ts, tm, te):
            # `vfxDelayTime` 缺省按 0（AnimInfo 的默认值就是 0 —— 判据见 `CardAnimBeat.vd`）,
            # 但**必须出声**：这条走的是「参数不全」那一档。
            return float(ts) + float(tm) + float(te), "card_anim_order(缺 vfxDelayTime)"
    if info:
        ts = info.get("ts")
        tm = info.get("tm")
        te = info.get("te")
        if None not in (ts, tm, te):
            return float(ts) + float(tm) + float(te), "animinfo_lookup"
    return None, "缺"


def build_original(order_json, info_json):
    """→ {key: {'id','name','beats':[(piece, start, hold, src)],'groups':n}}
    key 同时登记卡 id 与卡名（trace 里的 `cardId` 是**卡名**，判据见 `BattleEvent.CardId`）。"""
    ca_by_guid = {}
    for a in order_json.get("cardAnims") or []:
        g = a.get("guid") or ""
        if g:
            ca_by_guid[g] = a
    out = {}
    for c in order_json.get("cards") or []:
        beats = []
        acc = 0.0
        groups = 0
        for ab in c.get("abilities") or []:
            steps = sorted(ab.get("steps") or [], key=lambda s: s.get("i", 0))
            if not steps:
                continue
            groups += 1
            for st in steps:
                guid = st.get("ca") or ""
                anim = ca_by_guid.get(guid) if guid else None
                piece = (anim or {}).get("vfx") or ""
                if not piece:
                    piece = WAIT_PIECE
                params = (anim or {}).get("params") or {}
                info = info_json.get(piece) if piece != WAIT_PIECE else None
                hold, src = beat_duration(params, info)
                beats.append((piece, acc, hold, src))
                if params.get("waitAnimToApplyEffect", 0):
                    if hold is None:
                        # 原版这一拍要等、而参数不全 ⇒ 只报出来，**不替它编一个数**
                        pass
                    else:
                        acc += hold
        ent = {"id": c.get("id") or "", "name": c.get("name") or "", "beats": beats, "groups": groups}
        for k in (ent["id"], ent["name"]):
            if k and k not in out:
                out[k] = ent
    return out


# ----------------------------------------------------------------------
#  我们侧（trace）
# ----------------------------------------------------------------------
def build_ours(rows):
    """→ {cardId: [ (piece, start_rel, hold) ... ]}，按 `firstOfEvent` 切组、组内取相对偏移。
    ⚠️ 只收 `cueKind == beat` 的行（事件拍 / cueVfx / cueAction 不是「按表展开的拍」）。"""
    ours = {}
    cur = None
    group_start = 0.0
    for r in rows:
        if r["cueKind"] != "beat":
            continue
        cid = r["cardId"]
        if cur != cid or r["firstOfEvent"]:
            cur = cid
            group_start = r["startOffset"]
        piece = r["piece"] or WAIT_PIECE
        ours.setdefault(cid, []).append((piece, r["startOffset"] - group_start, r["holdSeconds"]))
    return ours


# ----------------------------------------------------------------------
#  比对 + 输出
# ----------------------------------------------------------------------
def fmt_seq(beats, limit=6):
    if not beats:
        return "(0 拍)"
    def _one(b):
        p, st, h = b[0], b[1], b[2]
        return "%s@%.2f/%s" % (p, st, ("%.2f" % h) if h is not None else "?")
    shown = beats[:limit]
    s = " → ".join(_one(b) for b in shown)
    if len(beats) > limit:
        s += " …(共 %d 拍)" % len(beats)
    return s


def compare_card(orig, ours, tol):
    """→ (diff_lines, unknown_lines, ok)。
    逐拍比件名 / 相对偏移 / 阻塞时长；长度差单列。
    ⚠️ **原版侧参数不全**（`card_anim_order` 与 `animinfo_lookup` 都没有）⇒ 进 `unknown_lines`，
    **不算差异**（那是原版数据缺口，不是我们演错了 —— ⛔ 别逼我们「猜一个数」去凑）。"""
    diffs = []
    unknowns = []
    n = max(len(orig), len(ours))
    if len(orig) != len(ours):
        diffs.append("拍数：原版 %d / 我们 %d" % (len(orig), len(ours)))
    for i in range(n):
        o = orig[i] if i < len(orig) else None
        u = ours[i] if i < len(ours) else None
        if o is None:
            diffs.append("第 %d 拍：原版 (无) / 我们 %s@%.2f/%.2f" % (i + 1, u[0], u[1], u[2]))
            continue
        if u is None:
            diffs.append("第 %d 拍：原版 %s@%.2f/%s / 我们 (无)" %
                         (i + 1, o[0], o[1], ("%.2f" % o[2]) if o[2] is not None else "?"))
            continue
        if o[0] != u[0]:
            diffs.append("第 %d 拍：件名 原版 %r / 我们 %r" % (i + 1, o[0], u[0]))
        if abs(o[1] - u[1]) > tol:
            diffs.append("第 %d 拍：起始偏移 原版 %.3f / 我们 %.3f（差 %+.3f）" % (i + 1, o[1], u[1], u[1] - o[1]))
        if o[2] is None:
            unknowns.append("第 %d 拍：阻塞时长 原版**参数不全** / 我们 %.3f（不计差异）" % (i + 1, u[2]))
        elif abs(o[2] - u[2]) > tol:
            diffs.append("第 %d 拍：阻塞时长 原版 %.3f / 我们 %.3f（差 %+.3f）" % (i + 1, o[2], u[2], u[2] - o[2]))
    return diffs, unknowns, (not diffs)


def main(argv):
    if "--check" not in argv:
        usage_exit(0)
    trace_path = None
    card_filter = None
    tol = 0.01
    max_diffs = 40
    order_path = DEFAULT_ORDER
    info_path = DEFAULT_INFO
    i = 1
    while i < len(argv):
        a = argv[i]
        if a == "--check":
            i += 1
        elif a == "--trace":
            trace_path = argv[i + 1]; i += 2
        elif a == "--card":
            card_filter = argv[i + 1]; i += 2
        elif a == "--tol":
            tol = float(argv[i + 1]); i += 2
        elif a == "--max-diffs":
            max_diffs = int(argv[i + 1]); i += 2
        elif a == "--order":
            order_path = argv[i + 1]; i += 2
        elif a == "--info":
            info_path = argv[i + 1]; i += 2
        elif a in ("-h", "--help"):
            usage_exit(0)
        else:
            die("不认识的参数 %r（--help 看用法）" % a)
            i += 1

    if not trace_path:
        trace_path = find_newest_trace()
        if not trace_path:
            die("找不到任何 trace（`persistentDataPath/%s/*.tsv`）。先开 trace 打一局：\n"
                "  Windows:  set WF_ANIM_TRACE=1  然后进战斗；或自检里调 AnimTrace.SetEnabledForTest(true)"
                "\n或用 --trace 指定文件。" % TRACE_DIR_NAME)
    for p in (trace_path, order_path):
        if not os.path.isfile(p):
            die("文件不存在：%s" % p)
    if not os.path.isfile(info_path):
        print("[warn] `animinfo_lookup.json` 不存在（%s）⇒ 只用 `card_anim_order` 的参数档" % info_path)
        info = {}
    else:
        info = load_json(info_path)
    order = load_json(order_path)
    rows, header = read_trace(trace_path)

    original = build_original(order, info)
    ours = build_ours(rows)

    keys = sorted(ours.keys())
    if card_filter:
        f = card_filter.strip()
        keys = [k for k in keys if k == f or (k in original and (original[k]["id"] == f or original[k]["name"] == f))]

    print("=" * 100)
    print("播放 trace 对账（按表展开的拍）· trace = %s" % trace_path)
    print("  行数 %d（其中 beat 行 %d，覆盖 %d 张卡）· 原版表 = %s"
          % (len(rows), sum(1 for r in rows if r["cueKind"] == "beat"), len(ours), order_path))
    print("  容差 %.3f s · 口径见文件头 §口径（原版 N 拍是**上限**：运行时可能合法少几拍）" % tol)
    print("=" * 100)
    print("%-14s | %-58s | %-58s | %s" % ("卡id", "原版值", "我们值", "差"))
    print("-" * 100)

    passed = 0
    failed = 0
    no_orig = 0
    unknown_beats = 0
    shown = 0
    failed_cards = []
    for k in keys:
        ent = original.get(k)
        list_ours = ours.get(k) or []
        if ent is None:
            no_orig += 1
            continue
        diffs, unknowns, ok = compare_card(ent["beats"], list_ours, tol)
        unknown_beats += len(unknowns)
        if ok:
            passed += 1
            continue
        failed += 1
        failed_cards.append(ent["id"] or k)
        if shown < max_diffs:
            shown += 1
            print("%-14s | %-58s | %-58s | %s"
                  % (ent["id"] or ent["name"], fmt_seq(ent["beats"]), fmt_seq(list_ours),
                     "；".join(diffs[:3]) + (" …" if len(diffs) > 3 else "")))
            for d in diffs[3:8]:
                print("%14s | %-58s | %-58s |   ↳ %s" % ("", "", "", d))
        elif shown == max_diffs:
            print("… 还有差异卡未逐张打印（--max-diffs 调大；或 --card <id> 单看）")
            shown += 1

    # 「表里有、本局没跑到」按**卡**去重（`original` 同一张卡在 id / 名 两个键各登记了一次）
    seen = set()
    orig_only = []
    for x in original.values():
        key = x["id"] or x["name"]
        if key in seen:
            continue
        seen.add(key)
        if x["beats"] and x["name"] not in ours and x["id"] not in ours:
            orig_only.append(key)
    print("-" * 100)
    print("汇总：比到 %d 张 · ✅ 一致 %d · ❌ 差异 %d · ⚠️ 原版无数据 %d（trace 里有、表里没有）"
          " · ℹ️ 本局没跑到 %d 张（表里有、trace 没有 —— 单局很正常，⛔ 别当缺陷）"
          % (passed + failed, passed, failed, no_orig, len(orig_only)))
    if unknown_beats:
        print("ℹ️ 原版**参数不全**的拍 %d 个（已跳过时长比对，不计差异 —— 数据缺口，⛔ 别拿 0 去凑）" % unknown_beats)
    if failed_cards:
        print("差异卡（前 40）：" + ", ".join(failed_cards[:40]))
    return 0 if failed == 0 else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv))
