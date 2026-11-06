# -*- coding: utf-8 -*-
"""gen_aud02_backfill.py —— 把 AUD02 的三份裁定按 key 回填进主表 `AUD02_serialized_params.tsv` 第 7 列（判档）。

来源（`B146_A31_A32_审计.md` §三 的 892 + 747 = 1,639 行）：
  ① `判据/AUD02_裁定_卡UI.tsv`        419 行（裁定在第 10 列）
  ② `判据/AUD02_裁定_卡组编辑.tsv`     473 行（裁定在第 10 列）
  ③ `判据/B106_AUD02_余问分级.md`      747 行（**逐行表**，判档在第 6 列；见下 ⚠️）

🔴 **⛔ 不要用 `判据/AUD02_裁定_剩余747.tsv`**：那一份 747 行**判档列全是 `✅`、改法列全是 `无`**
   —— 而 `B106_AUD02_余问分级.md` 的逐行表给的是 **✅693 / ❌12 / ⛔7 / ？35**，两者打架。
   现读取证（2026-11-05）：
     · 该文件 `collections.Counter(col7)` = `{✅: 747}`（唯一值，无其它档）；
     · `B106` 逐行表 747 行 = `✅693 / ？35 / ❌12 / ⛔7`，且与 `B146_A31_A32_审计.md` §三 的
       总账（✅1,468 / ❌106 / ⛔7 / ？58）**逐档相加对得上**：
       (卡UI 394✅/9❌/16？) + (卡组编辑 381✅/85❌/7？) + (B106 693✅/12❌/7⛔/35？)
       = ✅1,468 · ❌106 · ⛔7 · ？58 ✔
   ⇒ **B106 的逐行表才是那 747 行的真源**；`剩余747.tsv` 是一条**被压平成全 ✅ 的副本**，
   拿它回填会把 12 ❌ + 7 ⛔ + 35 ？ **静默标成已对上**。本脚本因此只认 B106。

配对键（**5 元组**，见下「为什么不是 4 元组」）：
    (prefab, 节点路径[去掉 `<prefab>/` 前缀], 组件, 字段, 原版值)
   · 4 元组在主表里**有 10 组重码（22 行）**，其中 **13 行**正是审计说的「同名节点跨 prefab 实例」；
     加上「原版值」后**全部唯一**（现读：主表 1,772 行 → 1,772 个不同 5 元组，0 重码）。

用法（⛔ 不带显式动作 ⇒ 只打印用法并退出 rc=2，一个字节都不写）：
    python -I -X utf8 d:/4/Unity/工具/gen_aud02_backfill.py --dry                 # 只报，不写
    python -I -X utf8 d:/4/Unity/工具/gen_aud02_backfill.py --dry --ab            # 只报（A/B 词表口径）
    python -I -X utf8 d:/4/Unity/工具/gen_aud02_backfill.py --write               # 真写（逐字口径）
    python -I -X utf8 d:/4/Unity/工具/gen_aud02_backfill.py --write --ab          # 真写（A/B 口径）

两套词表：
    · **逐字（默认）**：把来源里的 `✅/❌/⛔/？` **原样**写进第 7 列。⛔ 不发明任何映射。
    · **`--ab`**：按主表现存 `A(110)/B(23)` 两档的语义归并 —— `A = 无需动作`（✅ + ⛔）、`B = 确认有差`（❌）。
      依据：主表现存 A 行的 `改法` 全是 `—`（110/110）、B 行全有改法（23/23）。
      ⚠️ 这是**推断**（主表没有写死 A/B 的定义），要它就得接受这条推断。

自检（不满足 ⇒ 拒写整份文件）：
    · 每份来源的每一行**恰好命中主表 1 行**（0 命中和 >1 命中都报出来、不猜）；
    · 命中行的现读判档必须是 `？`（193 行非 ？ 的行不允许被覆盖）；
    · 写回前后：**行数不变**、**每行 11→9 列不变**、**行尾不变**（LF 保持 LF）。
"""
import io
import os
import re
import sys

JUDG = r"D:/4/Unity/资料/完全复刻/判据"
MAIN = os.path.join(JUDG, "AUD02_serialized_params.tsv")
SRC_CARDUI = os.path.join(JUDG, "AUD02_裁定_卡UI.tsv")
SRC_DECK = os.path.join(JUDG, "AUD02_裁定_卡组编辑.tsv")
SRC_B106 = os.path.join(JUDG, "B106_AUD02_余问分级.md")
TRAP = os.path.join(JUDG, "AUD02_裁定_剩余747.tsv")
NCOL = 9
COL_JUDGE = 6          # 0-based：第 7 列「判档」

USAGE = __doc__

# 词表映射（`--ab` / `--ab-all`）
#   ✅ = 已对上 ⇒ A（主表 A 的语义「机制在且值对上」）
#   ❌ = 确认有差 ⇒ B
#   ⛔ = 原版字段运行期无效 / 判据本身错 —— **`--ab` 保留 ⛔**（⛔→A 会把「前提不成立」写成「已复刻」，是错档）
AB_MAP = {'✅': 'A', '❌': 'B', '⛔': '⛔'}
#   `--ab-all` = 把 ⛔ 也压进 A（**不推荐**，只用于对照）：⛔ 与「已复刻」语义不同，压进去就再也分不出来
AB_ALL = {'✅': 'A', '❌': 'B', '⛔': 'A'}


def die(msg=None):
    if msg:
        print("⛔ " + msg)
    print(USAGE)
    sys.exit(2)


def read_raw(path):
    b = io.open(path, 'rb').read()
    bare_cr = b.count(b'\r') - b.count(b'\r\n')
    crlf = b.count(b'\r\n') == b.count(b'\n') and b.count(b'\r\n') > 0
    text = b.decode('utf-8-sig')
    lines = text.split('\r\n') if crlf else text.split('\n')
    return lines, ('\r\n' if crlf else '\n'), bare_cr, b


def norm_path(p, pref):
    return p[len(pref) + 1:] if p.startswith(pref + '/') else p


def key5(pref, path, comp, field, val):
    return (pref, norm_path(path, pref), comp, field, val)


def load_main():
    lines, eol, bare_cr, raw = read_raw(MAIN)
    while lines and lines[-1] == '':
        lines.pop()
    hdr = lines[0].split('\t')
    rows = [ln.split('\t') for ln in lines[1:]]
    return lines, eol, bare_cr, raw, hdr, rows


def load_verdict_tsv(path, judge_col):
    lines, eol, bare_cr, raw = read_raw(path)
    while lines and lines[-1] == '':
        lines.pop()
    hdr = lines[0].lstrip('\ufeff').split('\t')
    rows = [ln.split('\t') for ln in lines[1:]]
    return hdr, rows, judge_col


def load_b106_rows():
    """B106 的逐行表：`| # | prefab | 节点路径 | 字段 | 原版值 | 判档 | 判据 | 证据 |`（8 列）。"""
    text = io.open(SRC_B106, 'rb').read().decode('utf-8')
    out = []
    for lineno, ln in enumerate(text.replace('\r\n', '\n').split('\n'), 1):
        if not re.match(r'^\| \d+ \|', ln):
            continue
        c = [x.strip() for x in ln.strip().strip('|').split('|')]
        if len(c) != 8:
            continue
        pref, path, fld, val, verdict = c[1], c[2].strip('`'), c[3], c[4], c[5]
        comp, field = (fld.split('.', 1) if '.' in fld else ('', fld))
        out.append({'src': 'B106:%d' % lineno, 'k': key5(pref, path, comp, field, val),
                    'verdict': verdict})
    return out


def main(argv):
    if not argv:
        die()
    action = None
    use_ab = False
    sample = 0
    for a in argv:
        if a in ('--dry', '--write'):
            if action and action != a:
                die("--dry 与 --write 只能给一个")
            action = a
        elif a == '--ab':
            use_ab = 'ab'
        elif a == '--ab-all':
            use_ab = 'ab-all'
        elif a.startswith('--sample='):
            try:
                sample = int(a.split('=', 1)[1])
            except ValueError:
                die("--sample= 后面要跟整数")
        elif a in ('-h', '--help'):
            print(USAGE)
            return 0
        else:
            die("不认识的参数：" + a)
    if action is None:
        die("必须显式给 --dry 或 --write")

    # 0) ⛔ 防呆：确认「剩余747」那份确实是全 ✅（如果是，就印出来、不静默）
    if os.path.isfile(TRAP):
        _h, trows, tcol = load_verdict_tsv(TRAP, COL_JUDGE)
        vals = sorted({r[tcol] for r in trows if len(r) > tcol})
        if vals == ['✅']:
            print("⚠ 已知：%s 的判档列 %d 行全是 `✅` ⇒ **本脚本不采用它**（真源 = B106 逐行表）。"
                  % (os.path.basename(TRAP), len(trows)))

    lines, eol, bare_cr, raw, hdr, rows = load_main()
    n_before = len(lines)
    if bare_cr:
        die("主表有 %d 个裸 CR ⇒ 行号不可信，拒做" % bare_cr)
    if len(hdr) != NCOL or any(len(r) != NCOL for r in rows):
        die("主表列数不是 %d，拒做" % NCOL)

    idx = {}
    for i, r in enumerate(rows):
        idx.setdefault(key5(r[0], r[1], r[2], r[3], r[4]), []).append(i)

    sources = []
    src_counts = {}
    for path in (SRC_CARDUI, SRC_DECK):
        h, rr, jc = load_verdict_tsv(path, 9)   # 裁定 = 第 10 列
        if h[9].strip() != '裁定':
            die("%s 第 10 列表头不是 `裁定`（现读 %r）" % (os.path.basename(path), h[9]))
        src_counts[os.path.basename(path)] = len(rr)
        for n, r in enumerate(rr, 2):
            sources.append({'src': '%s:%d' % (os.path.basename(path), n),
                            'k': key5(r[0], r[1], r[2], r[3], r[4]), 'verdict': r[9]})
    b106 = load_b106_rows()
    src_counts['B106'] = len(b106)
    sources.extend(b106)

    stat = {'hit1': 0, 'miss': 0, 'multi': 0, 'not_q': 0, 'same': 0}
    plan = {}          # main row idx -> (verdict, src)
    misses, multis, notq = [], [], []
    for s in sources:
        hit = idx.get(s['k'], [])
        if len(hit) == 0:
            stat['miss'] += 1
            misses.append(s)
            continue
        if len(hit) > 1:
            stat['multi'] += 1
            multis.append((s, hit))
            continue
        stat['hit1'] += 1
        i = hit[0]
        cur = rows[i][COL_JUDGE]
        if cur != '？':
            stat['not_q'] += 1
            notq.append((i + 2, cur, s))
            continue
        v = s['verdict']
        if use_ab == 'ab':
            v = AB_MAP.get(v, v)
        elif use_ab == 'ab-all':
            v = AB_ALL.get(v, v)
        if rows[i][COL_JUDGE] == v:
            stat['same'] += 1
            continue
        plan[i] = (v, s['src'], s['verdict'])

    from collections import Counter
    print("来源行数 %d（卡UI %d + 卡组编辑 %d + B106 %d）"
          % (len(sources), src_counts[os.path.basename(SRC_CARDUI)],
             src_counts[os.path.basename(SRC_DECK)], src_counts['B106']))
    print("配对：恰好命中 1 行 %d · 0 命中 %d · 多命中 %d · 现读非「？」 %d · 无需改 %d"
          % (stat['hit1'], stat['miss'], stat['multi'], stat['not_q'], stat['same']))
    print("拟回填 %d 行（词表口径 = %s）"
          % (len(plan), {'ab': 'A/B 归并（⛔ 保留）', 'ab-all': 'A/B 全并（⛔→A，不推荐）'}
             .get(use_ab, '逐字 ✅/❌/⛔/？')))
    print("  拟写入值分布：%s"
          % dict(Counter(v for v, _s, _o in plan.values()).most_common()))
    print("  来源裁定分布：%s"
          % dict(Counter(o for _v, _s, o in plan.values()).most_common()))
    for s in misses[:10]:
        print("   ⛔ 0 命中 %s · %s" % (s['src'], s['k'][:4]))
    for s, hit in multis[:10]:
        print("   ⛔ 多命中 %s → 主表行 %s" % (s['src'], [x + 2 for x in hit]))
    for rowno, cur, s in notq[:10]:
        print("   ⚠ 现读非「？」主表行 %d = %r（来源 %s）⇒ 不覆盖" % (rowno, cur, s['src']))

    if sample:
        print()
        print("## 样例（每档最多 %d 行；只打印，不写盘）" % sample)
        print("   主表行 | prefab | 节点路径 | 组件.字段 | 原版值 | 现读第7列 | 来源裁定 | 写入字面")
        per = {}
        for i in sorted(plan):
            per.setdefault(plan[i][2], []).append(i)
        for orig in ('✅', '❌', '⛔', '？'):
            lst = per.get(orig, [])[:sample]
            if not lst:
                continue
            print("   —— 来源 %s ——" % orig)
            for i in lst:
                r = rows[i]
                v = plan[i][0]
                print("   %d | %s | %s | %s.%s | %s | %r | %s | %r"
                      % (i + 2, r[0], norm_path(r[1], r[0])[-46:], r[2], r[3],
                         r[4][:16], r[COL_JUDGE], orig, v))

    if action == '--dry':
        print("⇒ DRY：会改 %d 行 · 行数不变（%d）· 行尾 %s · ⛔ 未写任何文件"
              % (len(plan), n_before, eol == '\r\n' and 'CRLF' or 'LF'))
        return 0

    # --write
    if not plan:
        print("⇒ 无变化，不写盘。")
        return 0
    out_lines = list(lines)
    for i, (v, src, orig) in plan.items():
        c = list(rows[i])
        c[COL_JUDGE] = v
        out_lines[i + 1] = '\t'.join(c)
    body_lines = out_lines
    body = (eol.join(body_lines)).encode('utf-8')
    # 🔴 2026-11-06 修：load_main() 会 pop 掉行尾的空元素；原实现只处理「原文不以换行结尾」那一支，
    # 于是「原文以换行结尾」时 join 出来少一个 eol ⇒ 行数断言必炸。补上另一支。
    if raw.endswith(eol.encode()):
        if not body.endswith(eol.encode()):
            body += eol.encode()
    elif body.endswith(eol.encode()):
        body = body[:-len(eol.encode())]
    assert body.count(b'\n') == raw.count(b'\n'), "行数变了"
    assert body.count(b'\r') - body.count(b'\r\n') == 0, "写出裸 CR"
    io.open(MAIN, 'wb').write(body)
    print("✅ 已写 %s：%d 行 · 行数 %d→%d · 行尾 %s"
          % (MAIN, len(plan), n_before, body.count(b'\n'), eol == '\r\n' and 'CRLF' or 'LF'))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
