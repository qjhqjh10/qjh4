# -*- coding: utf-8 -*-
"""ledger_stale_audit.py —— 「账说没做、实际可能早做完」抽检（**只读**账表与源码）。

为什么要它
  账行 230（`CustomRaycaster.Raycast`）写着「我们的教程层没有这条点击过滤」，实际 2026-10-25 就逐句做完了
  ⇒ 这类过期会**系统性低估真复刻率**（复刻率的分母是账）。本项目已实测：一轮抽检捞出 24 条强候选、
  其中 13 条确认该升 A（见 `资料/完全复刻/报告/B89_E15d_过期账强档核销.md`）。

判据（本工具只提供**候选**，⛔ 不是结论）
  1. 只筛 `状态 ∈ {B, C}`（A/D 不在范围）。
  2. 判词/差异/备注任一含否证词（没有 / 未建 / 缺 / 不存在 / 尚未）。
  3. 落点列里有 `路径.cs:行号`。
  4. 该文件里能搜到**同名方法**（`名字(` 或 `名字<`），方法名 ≥6 字且**排除生命周期名**
     （`Update`/`Awake`/`Refresh`/`Initialize`… —— 它们在任何文件里都出现，不能当证据）。
  5. 按「命中行与账上落点行的距离」分档：S=≤3 行且判词含「已落地/已复刻/等价实现/已作废/已对齐/
     逐句同义/已实现」· 0=≤3 行 · M=≤80 行 · W=更远（多半同名不同物）。

🔴 三条纪律（血换来的）
  · **命中同名方法 ≠ 账过期**：大量 B 行的形状是「方法体已逐句对齐，缺的是宿主/接线/另一件」——
    那种判词里也有「缺」字，落点上也有同名方法，但它**该保持 B**。逐条必须现读判词那一句「缺」指的是哪一半。
  · **判「升 A」的唯一标准 = 那段行为在我们仓里真的等价可跑**，不是「文件里有同名方法」。
  · **不许为了让数字好看而升**：D 是「原版本身没有」，与本工具无关，⛔ 不要为了「看起来缺」把 D 拉下来。

用法
  python -I -X utf8 d:/4/Unity/工具/ledger_stale_audit.py --report <输出.tsv>
  python -I -X utf8 d:/4/Unity/工具/ledger_stale_audit.py --apply        # 写 _parts/ 补丁**骨架**（⛔ 不碰主表）
  不带参数 ⇒ 只打印本段用法并退出（本仓事故：一个不解析 --help 的脚本把 232 行手工修订冲掉了）。
"""
import io
import os
import re
import glob
import argparse

LEDGER_DIR = r'd:/4/Unity/资料/完全复刻/账'
ASSETS = r'd:/4/Unity/MyGame/Assets'
DENY = ['没有', '未建', '缺', '不存在', '尚未']
POS = ['已落地', '已复刻', '等价实现', '已作废', '已对齐', '逐句同义', '已实现']
GENERIC = set('''Update Awake Start OnEnable OnDisable OnDestroy Refresh Initialize Init Close Open
Show Hide Build SetData GetData Add Remove Clear Reset Apply Handle OnClick OnOpen OnClose Tick
Render Draw Create Destroy Load Save Setup Dispose Enable Disable Copy Clone Equals ToString
OnReset OnInitialize GetItem GetID SetQuantity GetValuesForMode Validate'''.split())
PATH_RE = re.compile(r'([A-Za-z0-9_\-./]+[.]cs):(\d+)')
HDR = '域\t账行\t原版类.方法\t账上判词摘要\t落点文件:行号\t命中符号\t结论\t建议状态\t依据'


def usage_and_exit():
    print(__doc__)
    raise SystemExit(0)


def resolve(p):
    for c in (os.path.join(ASSETS, p), os.path.join(ASSETS, 'CardPresentation', p)):
        if os.path.isfile(c):
            return c
    base = os.path.basename(p)
    for root, _d, files in os.walk(ASSETS):
        if base in files:
            return os.path.join(root, base)
    return None


def rd(p):
    try:
        return io.open(p, 'rb').read().decode('utf-8', 'replace')
    except Exception:
        return ''


def scan():
    out = []
    for fp in sorted(glob.glob(os.path.join(LEDGER_DIR, '*.tsv'))):
        dom = os.path.basename(fp)
        lines = [l.rstrip('\r') for l in rd(fp).split('\n')]
        for i, l in enumerate(lines):
            if i == 0 or not l.strip():
                continue
            f = l.split('\t')
            if len(f) < 8 or f[5].strip() not in ('B', 'C'):
                continue
            txt = f[7] + ' ' + (f[10] if len(f) > 10 else '') + ' ' + f[4]
            if not any(w in txt for w in DENY):
                continue
            m = PATH_RE.search(f[6])
            if not m:
                continue
            real = resolve(m.group(1))
            if real is None:
                continue
            mn = f[2].strip().split('(')[0].strip()
            if mn.endswith('>'):
                mn = mn.split('<')[0].strip()
            if len(mn) < 6 or mn in GENERIC:
                continue
            body = rd(real)
            pat = re.compile(r'(?<![A-Za-z0-9_])' + re.escape(mn) + r'\s*[<(]')
            if not pat.search(body):
                continue
            try:
                L = int(m.group(2))
            except Exception:
                L = 0
            best, bestln, blines = 10 ** 9, 0, body.split('\n')
            for j, bl in enumerate(blines):
                if pat.search(bl):
                    d = abs((j + 1) - L)
                    if d < best:
                        best, bestln = d, j + 1
            if best <= 3 and any(w in txt for w in POS):
                tier, concl = 'S', '疑似过期（强）：落点行就是同名方法的声明，且判词写着已落地/已复刻/等价实现'
            elif best <= 3:
                tier, concl = '0', '存疑：落点行就是同名方法声明（判词无「已落地」类措辞）'
            elif best <= 80:
                tier, concl = 'M', '存疑（中）：同名方法在同一文件、落点 ±80 行内'
            else:
                tier, concl = 'W', '存疑（弱）：同名符号在该文件别处（可能是同名不同物）'
            out.append(dict(tier=tier, dom=dom, line=i + 1, name=f[1].strip() + '.' + f[2].strip(),
                            summary=txt[:180].replace('\t', ' '), landing=m.group(0), sym=mn,
                            concl=concl, dist=best, hitline=bestln,
                            hittext=(blines[bestln - 1].strip()[:90] if bestln else '')))
    out.sort(key=lambda r: (r['tier'] != 'S', r['tier'] != '0', r['dist']))
    return out


def main():
    ap = argparse.ArgumentParser(add_help=False)
    ap.add_argument('--report')
    ap.add_argument('--apply', action='store_true')
    args, rest = ap.parse_known_args()
    if rest or not (args.report or args.apply):
        usage_and_exit()
    rows = scan()
    body = '\n'.join('\t'.join([r['dom'], str(r['line']), r['name'], r['summary'], r['landing'],
                                r['sym'], r['concl'], '现读后定',
                                'dist=%d · 命中行=%d · `%s`' % (r['dist'], r['hitline'], r['hittext'])])
                         for r in rows)
    if args.report:
        io.open(args.report, 'wb').write((HDR + '\n' + body + '\n').encode('utf-8'))
        print('已写出 %d 行 → %s' % (len(rows), args.report))
    if args.apply:
        p = os.path.join(LEDGER_DIR, '_parts', 'STALE_AUDIT_骨架.tsv')
        io.open(p, 'wb').write((HDR + '\n' + body + '\n').encode('utf-8'))
        print('⛔ 本工具**不写主表**；已把候选写到 %s（合并前必须人工逐条现读）' % p)
    from collections import Counter
    c = Counter(r['tier'] for r in rows)
    print('S=%d 0=%d M=%d W=%d 合计 %d' % (c['S'], c['0'], c['M'], c['W'], len(rows)))
    print('⚠️ 这些是**候选**，不是结论 —— 三条纪律见文件头。')


if __name__ == '__main__':
    main()
