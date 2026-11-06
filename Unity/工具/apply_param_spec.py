#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""apply_param_spec.py — 按「参数施工单」批量改值（B150 流水线核心件，2026-11-05）

## 它解决什么
系统性审查（B149）的产出里，有一大类是「**参数不是原版的**」：
阈值 5 vs 10、队列号 / 时长 / 字号 / 排序键 …… 这些**不需要写手理解语义**，
只需要「把这一行里的这个值换成那个值」。人工改 200 条既慢又容易漏、还会把行尾搞翻。
本工具把这类改动变成**一份 TSV 施工单 + 一次可回滚的批量执行**。

## 施工单 schema（7 列，制表符分隔，首行表头）
    ID  文件  锚点  期望旧值  新值  判据出处  备注

| 列 | 含义 | 约束 |
|---|---|---|
| ID | 施工单内唯一编号（`E1-007` 这种） | 非空、全单唯一 |
| 文件 | 目标源文件 | 绝对路径，或相对 `--root`（默认 `d:/4/Unity/MyGame`） |
| 锚点 | **能唯一定位到那一行**的连续字符串（例：`HudImageZ = 0.3f`、方法签名） | 单行内可见；**全文件必须恰好出现 1 次** |
| 期望旧值 | 匹配到锚点后，**该行必须还包含**这个旧值 | 非空；新值里不许含换行 |
| 新值 | 要替换成什么 | 不许含 `\\n` / `\\r`；等于旧值时判 SKIP |
| 判据出处 | 原版判据 `文件:行` 或资产路径+字段 | **非空**（灭自证：期望值必须来自原版） |
| 备注 | 自由文本 | 可空；不许含制表符 |

## 三态（每条都逐行打印，⛔ 不汇总成一句「完成」）
- `OK`       五项校验全过，本次会改/已改。
- `SKIP`     良性跳过（新值==旧值 / 同单内完全重复的重复行）。
- `CONFLICT` 危险或无法定位：锚点 0 次 / 锚点多次 / 该行不含期望旧值 / 新值含换行 / 判据出处为空。
              **只跳过这一条**，其余继续（`--strict` 下则整轮拒写）。

## 六项硬校验（逐条对应上面的三态）
1. **锚点唯一**：全文件恰好 1 次；0 次或 ≥2 次 ⇒ CONFLICT（绝不猜是哪一处）。
2. **旧值仍在**：锚点所在行必须含期望旧值 ⇒ 文件已被人改过时**自动拦下**，不会覆盖别人的改动。
3. **行数守恒**：只做**行内替换**，改前改后 `\\n` 计数必须相同（新值含换行则直接拒绝）。
4. **行尾保持**：二进制读写；改前改后 `\\r\\n` 计数、裸 `\\r` 计数必须与改前**逐一相同**
   （CRLF 文件仍是 CRLF，LF 文件仍是 LF）。文件本来就有裸 `\\r` ⇒ 整份文件拒改（会让行号整体偏移）。
   UTF-8 BOM 原样保留（读写都剥、回写再加）。
5. **可回滚性**：写盘**之前**在内存里跑一遍反向施工单，必须逐字节回到改前；
   回不去（例：新值让某个锚点变成 2 处）⇒ 整份文件拒改。反向单会**连锚点一起换**
   （锚点常含旧值，只换值不换锚点 ⇒ 回滚 0 命中 —— 自测第一天就撞上过）。
6. **写盘必须显式**：默认行为 = 打印用法 + 退出码 2；`--help` 只打印用法、不做任何动作；
   `--dry` 只算不写；`--write` 才写。

## 用法
    python -I -X utf8 d:/4/Unity/工具/apply_param_spec.py <spec.tsv> --dry
    python -I -X utf8 d:/4/Unity/工具/apply_param_spec.py <spec.tsv> --write [--rollback-out <path>]
    python -I -X utf8 d:/4/Unity/工具/apply_param_spec.py --check-owners <spec1.tsv> <spec2.tsv> ...

    --root DIR          相对路径的根（默认 d:/4/Unity/MyGame）
    --strict            有任何 CONFLICT 就整轮拒写（rc=2），默认是「跳过坏行、写好行」
    --rollback-out P    把「反向施工单」写到 P（回滚 = 拿它再跑一次 --write）
    --quiet            只打印 CONFLICT / 汇总（OK 行不逐条刷屏）

## 退出码
    0  全部 OK / SKIP，无 CONFLICT
    1  跑完了，但有 CONFLICT（已写 OK 的那些行）
    2  拒绝执行：用法错 / 施工单格式错 / `--strict` 下有 CONFLICT / 文件级拒改

## 回滚
`--write` 会打印每个文件的「改前 sha / 改后 sha」，并把**反向施工单**（旧值/新值互换）写到
`--rollback-out` 或直接打印。回滚 = 对新值再跑一次 `--write`：
行号守恒 ⇒ **不会影响同文件里别人的未提交改动**（⛔ 别用 `git checkout --`，那会一并冲掉）。

## 不做什么
- ⛔ 不新增 / 删除行（一次都不行 —— 账按 `文件:行号` 引用）。
- ⛔ 不做跨行替换、不做插入、不做正则（锚点是**字面量**，免得 `.` `(` 这类字符被当元字符）。
- ⛔ 不改账 `.tsv`、不改 `活状态表.md`（那些有各自的写入纪律）。
- ⛔ 不判断「这个值对不对」—— 那由施工单的 `判据出处` 负责；本工具只保证**改得准、改得住、能回滚**。
"""
import hashlib
import io
import os
import sys

COLS = ["ID", "文件", "锚点", "期望旧值", "新值", "判据出处", "备注"]
NCOL = len(COLS)
BOM = b"\xef\xbb\xbf"
DEFAULT_ROOT = r"d:/4/Unity/MyGame"

USAGE = """用法：
  python -I -X utf8 d:/4/Unity/工具/apply_param_spec.py <spec.tsv> --dry
  python -I -X utf8 d:/4/Unity/工具/apply_param_spec.py <spec.tsv> --write [--rollback-out <path>] [--strict] [--quiet]
  python -I -X utf8 d:/4/Unity/工具/apply_param_spec.py --check-owners <spec1.tsv> <spec2.tsv> ...

  --root DIR        相对路径的根（默认 d:/4/Unity/MyGame）
  --dry             只算不写（先跑这个）
  --write           真写盘（必须显式给，否则本脚本只打印这段用法并以 rc=2 退出）
  --strict          有任何 CONFLICT 就整轮拒写
  --rollback-out P  把反向施工单写到 P
  --quiet           只打印 CONFLICT / 汇总

施工单 = 7 列 TSV（表头：ID 文件 锚点 期望旧值 新值 判据出处 备注）
"""

OK, SKIP, CONFLICT = "OK", "SKIP", "CONFLICT"


# ---------------------------------------------------------------- 基础设施
def _stdout_utf8():
    """Windows 控制台默认 cp936，中文 + 箭头会炸；统一成 utf-8（失败就退化成 replace）。"""
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass


def die(msg, code=2):
    print(msg)
    sys.exit(code)


def sha16(data):
    return hashlib.sha256(data).hexdigest()[:16]


def read_lines_raw(path):
    """二进制读一个文本文件，按 b'\\n' 切。

    返回 dict：
      bom    : 是否带 UTF-8 BOM
      chunks : [{'text': str, 'cr': bool}] —— `cr` 表示该行以 CRLF 结尾
      data   : 原始字节（算 sha 用）
      n_nl / n_crlf / n_bare_cr : 三个行尾计数（校验用）
    裸 CR 一律由调用方拒改（见 docstring 校验 4）。
    """
    data = io.open(path, "rb").read()
    bom = data.startswith(BOM)
    body = data[len(BOM):] if bom else data
    n_crlf = body.count(b"\r\n")
    n_nl = body.count(b"\n")
    n_bare_cr = body.count(b"\r") - n_crlf
    chunks = []
    for ch in body.split(b"\n"):
        cr = ch.endswith(b"\r")
        chunks.append({"text": (ch[:-1] if cr else ch).decode("utf-8"), "cr": cr})
    return {"bom": bom, "chunks": chunks, "data": data,
            "n_nl": n_nl, "n_crlf": n_crlf, "n_bare_cr": n_bare_cr}


def render_lines(doc):
    """把 chunks 还原成字节。⚠️ 只重排、不增删 chunk ⇒ 行数天然守恒。"""
    body = b"\n".join(
        (c["text"] + ("\r" if c["cr"] else "")).encode("utf-8") for c in doc["chunks"])
    return (BOM if doc["bom"] else b"") + body


# ---------------------------------------------------------------- 施工单
def parse_spec(path):
    """读施工单，返回 (rows, errors)。errors 非空 ⇒ 整轮拒跑（格式错不该被猜）。"""
    if not os.path.isfile(path):
        die("⛔ 施工单不存在：%s" % path)
    raw = io.open(path, "rb").read()
    if raw.startswith(BOM):
        raw = raw[len(BOM):]
    text = raw.decode("utf-8")
    lines = text.split("\n")
    rows, errors, seen_ids = [], [], {}
    header_seen = False
    for i, ln in enumerate(lines, 1):
        if ln.endswith("\r"):
            ln = ln[:-1]
        if not ln.strip() or ln.lstrip().startswith("#"):
            continue
        cells = ln.split("\t")
        if not header_seen and [c.strip() for c in cells] == COLS:
            header_seen = True
            continue
        # 容忍「行尾多余的制表符」—— 只丢**全空**的尾列，别的一律报错
        while len(cells) > NCOL and cells[-1].strip() == "":
            cells.pop()
        if len(cells) != NCOL:
            errors.append("施工单第 %d 行：列数 = %d，应为 %d ⇒ 制表符分隔、7 列；"
                          "含制表符的字段要改写" % (i, len(cells), NCOL))
            continue
        r = dict(zip(COLS, cells))
        if not r["ID"].strip():
            errors.append("施工单第 %d 行：ID 为空" % i)
            continue
        if r["ID"] in seen_ids:
            errors.append("施工单第 %d 行：ID 重复（%s，与第 %d 行同号）"
                          % (i, r["ID"], seen_ids[r["ID"]]))
            continue
        seen_ids[r["ID"]] = i
        r["_line"] = i
        rows.append(r)
    if not rows and not errors:
        errors.append("施工单里没有任何数据行（只有表头/注释）")
    return rows, errors


# ---------------------------------------------------------------- 单条改值
def apply_row(doc, r):
    """对一条施工单行做五项校验 + 行内替换。返回 (status, reason, line_no)。

    ⚠️ 有副作用：status == OK 时**已经改了 doc['chunks']** —— 调用方要么回滚要么写盘。
    """
    anchor, old, new = r["锚点"], r["期望旧值"], r["新值"]

    if not r["判据出处"].strip():
        return CONFLICT, "判据出处为空（期望值必须来自原版判据，否则就是自证）", 0
    if anchor == "":
        return CONFLICT, "锚点为空", 0
    if "\n" in anchor or "\r" in anchor:
        return CONFLICT, "锚点含换行（本工具不做跨行锚点）", 0
    if old == "":
        return CONFLICT, "期望旧值为空（本工具只做行内替换，不做插入）", 0
    if "\n" in new or "\r" in new:
        return CONFLICT, "新值含换行/回车（会让行数或行尾改变）", 0
    if new == old:
        return SKIP, "新值 == 旧值（无操作）", 0

    # 校验 1：锚点全文件恰好 1 次（按行统计，避免跨行误配）
    total, hit_idx = 0, -1
    for i, c in enumerate(doc["chunks"]):
        n = c["text"].count(anchor)
        if n:
            total += n
            if hit_idx < 0:
                hit_idx = i
    if total == 0:
        return CONFLICT, "锚点在文件里出现 0 次（文件已改过？锚点写错？）", 0
    if total > 1:
        return CONFLICT, "锚点在文件里出现 %d 次（必须恰好 1 次，拒绝猜）" % total, hit_idx + 1

    content = doc["chunks"][hit_idx]["text"]
    a_start = content.find(anchor)
    a_end = a_start + len(anchor)

    # 校验 2：期望旧值仍在（优先取锚点区间内那一处）
    if old in content[a_start:a_end]:
        pos = content.find(old, a_start)
    else:
        allpos, p = [], content.find(old)
        while p >= 0:
            allpos.append(p)
            p = content.find(old, p + 1)
        if not allpos:
            return CONFLICT, "该行不含期望旧值（文件已被人改过，拒绝覆盖）", hit_idx + 1
        if len(allpos) > 1:
            return CONFLICT, "旧值不在锚点内，且该行有 %d 处 ⇒ 无法唯一定位" % len(allpos), hit_idx + 1
        pos = allpos[0]

    new_content = content[:pos] + new + content[pos + len(old):]
    if "\n" in new_content or "\r" in new_content:
        return CONFLICT, "替换后会引入换行（内部不一致，拒改）", hit_idx + 1

    doc["chunks"][hit_idx]["text"] = new_content
    return OK, "行内字符位 %d→%d 已替换" % (pos, pos + len(old)), hit_idx + 1


def reverse_row(r):
    """生成一条反向施工单行（新值 ↔ 旧值）。

    ⚠️ **锚点也要跟着换**：`锚点` 常常**包含**旧值（例：`HudImageZ = 0.3f`）。
    只换值不换锚点 ⇒ 回滚时锚点在文件里已经是 0 次 ⇒ 回滚整批 CONFLICT
    （2026-11-05 自测第一版就撞上了，这是真缺陷不是理论风险）。
    """
    back = dict(r)
    back["期望旧值"], back["新值"] = r["新值"], r["期望旧值"]
    if r["期望旧值"] and r["期望旧值"] in r["锚点"]:
        back["锚点"] = r["锚点"].replace(r["期望旧值"], r["新值"])
    back["备注"] = ("回滚 " + r["ID"] + ("；" + r["备注"] if r["备注"] else ""))
    return back


def clone_doc(doc, texts=None):
    """克隆行内容（用于「先模拟、后写盘」）。texts 给了就覆盖文本。"""
    return {"bom": doc["bom"],
            "chunks": [{"text": (texts[i] if texts is not None else c["text"]), "cr": c["cr"]}
                       for i, c in enumerate(doc["chunks"])]}


def check_reversible(doc, ok_rows, before_bytes):
    """写盘前**在内存里**把改动回滚一遍，要求逐字节回到改前。

    为什么值得多花这几毫秒：批量改值最怕的是「改了、但回不去」——
    比如新值让某个锚点在文件里变成 2 处（这次自测的 anchor 唯一性就依赖它）。
    回不去 = 现场无法一键撤销 ⇒ 必须在写盘之前就拦下。
    """
    sim = clone_doc(doc)
    for r in reversed(ok_rows):
        status, reason, _ = apply_row(sim, reverse_row(r))
        if status != OK:
            return False, "%s 回滚不了（%s）" % (r["ID"], reason)
    if render_lines(sim) != before_bytes:
        return False, "回滚后与改前不逐字节相同"
    return True, ""


# ---------------------------------------------------------------- 主流程
def resolve(path, root):
    return path if os.path.isabs(path) else os.path.join(root, path)


def run_spec(spec_path, mode, root, strict, quiet, rollback_out):
    rows, errors = parse_spec(spec_path)
    if errors:
        print("⛔ 施工单有问题，**一个字节都没写**：")
        for e in errors:
            print("   " + e)
        return 2

    by_file, order = {}, []
    for r in rows:
        f = resolve(r["文件"].strip(), root)
        key = os.path.normcase(os.path.abspath(f))
        if key not in by_file:
            by_file[key] = {"path": f, "rows": []}
            order.append(key)
        by_file[key]["rows"].append(r)

    results = []          # (status, row, reason, line_no, path)
    file_reports = []     # (path, before_sha, after_sha, n_ok, n_skip, n_conf, refused, reason)
    seen_sig = {}

    for key in order:
        ent = by_file[key]
        path = ent["path"]
        if not os.path.isfile(path):
            for r in ent["rows"]:
                results.append((CONFLICT, r, "目标文件不存在：%s" % path, 0, path))
            file_reports.append((path, "-", "-", 0, 0, len(ent["rows"]), True, "文件不存在"))
            continue
        try:
            doc = read_lines_raw(path)
        except UnicodeDecodeError as e:
            for r in ent["rows"]:
                results.append((CONFLICT, r, "不是 UTF-8 文本：%s" % e, 0, path))
            file_reports.append((path, "-", "-", 0, 0, len(ent["rows"]), True, "解码失败"))
            continue
        if doc["n_bare_cr"] > 0:
            # 裸 CR 会让所有「按行号」的账整体偏移，且是静默的 ⇒ 整份文件拒改
            msg = "文件含 %d 个裸 CR（会让账的行号整体偏移）⇒ 整份拒改；先按二进制修行尾" % doc["n_bare_cr"]
            for r in ent["rows"]:
                results.append((CONFLICT, r, msg, 0, path))
            file_reports.append((path, "-", "-", 0, 0, len(ent["rows"]), True, "裸 CR"))
            continue

        before = doc["data"]
        res_start = len(results)
        n_ok = n_skip = n_conf = 0
        changed_any = False
        ok_rows = []
        touched_lines = set()
        for r in ent["rows"]:
            sig = (key, r["锚点"], r["期望旧值"], r["新值"])
            if sig in seen_sig:
                n_skip += 1
                results.append((SKIP, r, "与 %s 完全重复（同文件同锚点同值）" % seen_sig[sig], 0, path))
                continue
            snapshot = [c["text"] for c in doc["chunks"]]
            status, reason, line_no = apply_row(doc, r)
            if status == OK:
                # 同一行被两条施工单改过 ⇒ 后一条的锚点通常已失效，但这里再兜一层
                if line_no in touched_lines:
                    for i, t in enumerate(snapshot):
                        doc["chunks"][i]["text"] = t
                    status, reason, line_no = CONFLICT, "同一行被本单改过两次（顺序依赖），拒绝二次覆盖", line_no
                else:
                    touched_lines.add(line_no)
                    seen_sig[sig] = r["ID"]
                    changed_any = True
                    ok_rows.append(r)
            if status == OK:
                n_ok += 1
            elif status == SKIP:
                n_skip += 1
            else:
                n_conf += 1
            results.append((status, r, reason, line_no, path))

        # 校验 3 / 4：行数守恒 + 行尾逐一相同
        out = render_lines(doc)
        after_hashes = {"n_nl": out.count(b"\n"),
                        "n_crlf": out.count(b"\r\n")}
        after_hashes["n_bare_cr"] = out.count(b"\r") - after_hashes["n_crlf"]
        if after_hashes["n_nl"] != doc["n_nl"]:
            for r in ent["rows"]:
                results.append((CONFLICT, r, "行数不守恒（内部错误，未写盘）", 0, path))
            file_reports.append((path, sha16(before), sha16(before), 0, 0, len(ent["rows"]), True, "行数不守恒"))
            continue
        if (after_hashes["n_crlf"] != doc["n_crlf"] or after_hashes["n_bare_cr"] != doc["n_bare_cr"]):
            for r in ent["rows"]:
                results.append((CONFLICT, r, "行尾计数变了（内部错误，未写盘）", 0, path))
            file_reports.append((path, sha16(before), sha16(before), 0, 0, len(ent["rows"]), True, "行尾漂移"))
            continue

        # 可回滚性自检（写盘前，在内存里跑一遍反向施工单）
        # ⚠️ 模拟的起点必须是**改后**内容（doc 此刻已是改后）——回滚是「在改后的文件上把新值换回旧值」。
        irreversible = False
        if changed_any:
            rev_ok, why = check_reversible(clone_doc(doc), ok_rows, before)
            if not rev_ok:
                irreversible = True
                for idx in range(res_start, len(results)):
                    st, r, reason, ln, p = results[idx]
                    if st == OK:
                        results[idx] = (CONFLICT, r, "回滚自检失败：" + why + "（本文件未写盘）", ln, p)
                n_conf += n_ok
                n_ok = 0
                changed_any = False

        wrote = False
        blocked = False
        if mode == "--write" and changed_any:
            if strict and n_conf:
                blocked = True
            else:
                io.open(path, "wb").write(out)
                wrote = True
        if wrote:
            note = "已写盘"
        elif blocked:
            note = "strict 拒写"
        elif irreversible:
            note = "不可回滚·拒写"
        elif mode == "--dry":
            note = "仅 dry"
        else:
            note = "无改动"
        file_reports.append((path, sha16(before), sha16(out), n_ok, n_skip, n_conf, False, note))

    # ------------------------------------------------------------ 输出
    print("=" * 78)
    print("施工单：%s    模式：%s    文件数：%d    条目数：%d" % (spec_path, mode, len(order), len(rows)))
    print("=" * 78)
    cur = None
    for status, r, reason, line_no, path in results:
        if path != cur:
            cur = path
            print("\n## %s" % path)
        loc = ("L%d" % line_no) if line_no else "  - "
        if status == OK:
            if quiet:
                continue
            print("  [OK      ] %-10s %s  %s" % (r["ID"], loc, reason))
        elif status == SKIP:
            if quiet:
                continue
            print("  [SKIP    ] %-10s %s  %s" % (r["ID"], loc, reason))
        else:
            print("  [CONFLICT] %-10s %s  %s" % (r["ID"], loc, reason))
            print("             锚点=%r 旧值=%r 新值=%r 判据=%s"
                  % (r["锚点"], r["期望旧值"], r["新值"], r["判据出处"]))

    tot_ok = sum(1 for s, *_ in results if s == OK)
    tot_skip = sum(1 for s, *_ in results if s == SKIP)
    tot_conf = sum(1 for s, *_ in results if s == CONFLICT)

    print("\n" + "-" * 78)
    print("%-58s %4s %5s %5s %8s" % ("文件", "OK", "SKIP", "CONFL", "状态"))
    for path, b, a, n_ok, n_skip, n_conf, refused, note in file_reports:
        print("%-58s %4d %5d %5d %8s" % (path[-58:], n_ok, n_skip, n_conf, note))
    print("%-58s %4d %5d %5d" % ("合计", tot_ok, tot_skip, tot_conf))

    if mode == "--write":
        print("\nsha256(前16位)：")
        for path, b, a, n_ok, n_skip, n_conf, refused, note in file_reports:
            if note == "已写盘":
                print("  %s\n    改前 %s → 改后 %s" % (path, b, a))
            elif b != "-":
                print("  %s\n    改前 %s → (未写，若写则为 %s) [%s]" % (path, b, a, note))
            else:
                print("  %s\n    (未读/未写) [%s]" % (path, note))

    # 反向施工单（回滚用）
    rev = []
    for status, r, reason, line_no, path in results:
        if status != OK:
            continue
        rev.append(reverse_row(r))
    if rev:
        body = "\t".join(COLS) + "\n" + "".join(
            "\t".join(m.get(c, "") for c in COLS) + "\n" for m in rev)
        if rollback_out:
            io.open(rollback_out, "wb").write(body.encode("utf-8"))
            print("\n↩ 反向施工单已写：%s（回滚 = 拿它再跑一次 --write）" % rollback_out)
        else:
            print("\n↩ 回滚用反向施工单（加 --rollback-out <path> 可落盘）：")
            for ln in body.rstrip("\n").split("\n"):
                print("   " + ln)

    print("")
    if mode == "--dry":
        print("⇒ DRY：会改 %d 条 · SKIP %d · CONFLICT %d。⛔ 未写任何文件。" % (tot_ok, tot_skip, tot_conf))
        return 0 if tot_conf == 0 else 1
    if strict and tot_conf:
        print("⇒ WRITE：OK %d · SKIP %d · CONFLICT %d。" % (tot_ok, tot_skip, tot_conf))
        print("⛔ --strict：存在 CONFLICT ⇒ 本轮**一个字节都没写**。")
        return 2
    print("⇒ WRITE：已改 %d 条 · SKIP %d · CONFLICT %d。%s"
          % (tot_ok, tot_skip, tot_conf,
             "有 CONFLICT，只写了 OK 的那些行" if tot_conf else "全部通过"))
    return 0 if tot_conf == 0 else 1


def check_owners(specs, root):
    """施工单之间的**文件所有权冲突**检测（同一文件出现在两份施工单 ⇒ 报错）。

    为什么单列：铁律 13·3「一个文件同一时刻只有一个写手」。两份施工单各自都对、
    合并起来却会让后写的盖掉先写的（而且零报错）。派活前跑一次，5 秒。
    """
    owner = {}
    bad = 0
    for sp in specs:
        rows, errors = parse_spec(sp)
        if errors:
            print("⛔ %s 格式有问题：" % sp)
            for e in errors:
                print("   " + e)
            bad += 1
            continue
        files = {}
        for r in rows:
            f = os.path.abspath(resolve(r["文件"].strip(), root))
            files.setdefault(f, 0)
            files[f] += 1
        for f, n in sorted(files.items()):
            owner.setdefault(os.path.normcase(f), {"path": f, "specs": []})
            owner[os.path.normcase(f)]["specs"].append((sp, n))

    print("参与检查的施工单：%d" % len(specs))
    print("%-6s %-56s %s" % ("条数", "文件", "施工单"))
    conflicts = []
    for k in sorted(owner):
        ent = owner[k]
        tags = " + ".join("%s(%d)" % (os.path.basename(s), n) for s, n in ent["specs"])
        print("%-6d %-56s %s" % (sum(n for _, n in ent["specs"]), ent["path"][-56:], tags))
        if len(ent["specs"]) > 1:
            conflicts.append(ent)
    print("")
    if conflicts:
        print("⛔ 文件所有权冲突 %d 个（同一文件落在多于 1 份施工单）：" % len(conflicts))
        for ent in conflicts:
            print("   " + ent["path"])
            for s, n in ent["specs"]:
                print("       %s（%d 条）" % (s, n))
        print("⇒ 派活前必须合并成一份，或改成串行（铁律 13·3）。")
        return 2
    if bad:
        return 2
    print("⇒ 无所有权冲突：%d 份施工单 → %d 个文件，每个文件恰好一个所有者。" % (len(specs), len(owner)))
    return 0


def main(argv):
    _stdout_utf8()
    if len(argv) < 2:
        print(USAGE)
        return 2

    args = argv[1:]
    if "-h" in args or "--help" in args:
        # 只打印用法；⛔ 绝不因为「看到了 --help」就去做任何动作（本仓 2026-10-26 踩过一次：
        # 有脚本不解析 --help，不带参就是真合并，结果冲掉 232 行手工修订）。
        print(USAGE)
        return 0
    mode = None
    root = DEFAULT_ROOT
    strict = False
    quiet = False
    rollback_out = None
    owners_mode = False
    positionals = []
    i = 0
    while i < len(args):
        a = args[i]
        if a == "--dry":
            if mode == "--write":
                die("⛔ --dry 与 --write 只能给一个。\n" + USAGE)
            mode = "--dry"
        elif a == "--write":
            if mode == "--dry":
                die("⛔ --dry 与 --write 只能给一个。\n" + USAGE)
            mode = "--write"
        elif a == "--strict":
            strict = True
        elif a == "--quiet":
            quiet = True
        elif a == "--check-owners":
            owners_mode = True
        elif a == "--root":
            i += 1
            if i >= len(args):
                die("⛔ --root 后面缺目录。\n" + USAGE)
            root = args[i]
        elif a == "--rollback-out":
            i += 1
            if i >= len(args):
                die("⛔ --rollback-out 后面缺路径。\n" + USAGE)
            rollback_out = args[i]
        elif a.startswith("-"):
            die("⛔ 未知参数：%s\n%s" % (a, USAGE))
        else:
            positionals.append(a)
        i += 1

    if owners_mode:
        if len(positionals) < 2:
            die("⛔ --check-owners 至少给两份施工单。\n" + USAGE)
        if mode is not None:
            die("⛔ --check-owners 与 --dry/--write 不能同时给（它只读、不写盘）。\n" + USAGE)
        return check_owners(positionals, root)
    if not positionals:
        print(USAGE)
        return 2
    if len(positionals) > 1:
        die("⛔ 多余的参数：%s（本脚本一次只吃一份施工单）\n%s" % (positionals[1], USAGE))
    spec = positionals[0]
    if mode is None:
        print("⛔ 必须显式给 --dry 或 --write（防止误写盘）。\n" + USAGE)
        return 2
    return run_spec(spec, mode, root, strict, quiet, rollback_out)


if __name__ == "__main__":
    sys.exit(main(sys.argv))
