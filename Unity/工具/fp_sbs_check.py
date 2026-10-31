#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""fp_sbs_check.py -- check the FP side-by-side ledger (method (3) "并排比").

Spec owner: 资料/完全复刻/判据/B106_功能点三法缺口.md  §3.3
Ledger   : 资料/完全复刻/判据/功能点_并排比记账.tsv   (10 columns)

    FP_ID | 件 | 渲染命令 | 我们的图 | 原版图 | 差异指标 | 判定 | 签字 | 日期 | 备注
    件   in {卡面, 特效, 窗口, 动画, 音频}
    判定 in {一致, 有差异, 没比到}
    签字 MUST be a human name.  This tool NEVER signs anything.

WHAT IT DOES
------------
For every ledger row it
  1. resolves and opens both images; checks existence and aspect ratio;
  2. (unless --dry) runs the same "tone-normalise -> gaussian -> connected
     components" pipeline as 工具/arena_diff.py, by IMPORTING its four
     functions (align / tone_match / rank_match / label_blobs).  arena_diff.py
     is NOT modified;
  3. writes side-by-side / mask / zoomed candidate blocks under --out;
  4. counts the row as SIGNED only when 判定 in {一致,有差异} AND 签字 is
     non-empty AND both images exist.  Everything else is red (Z).

It also has a --scan mode that proves the pipeline works on existing image
sets WITHOUT touching the ledger:
    --scan d:/4/_tmp_view/cmp                  # <name>__orig.png / <name>__exp.png pairs
    --scan d:/4/_tmp_view/cardface_vs_pnp      # uses that dir's manifest.tsv (card/ours/pnp)

SUMMARY LINE (fixed, machine readable)
--------------------------------------
    === FP 并排比：已签 X / 有差异 Y / 没比到 Z ===
    X = signed rows (判定 in {一致,有差异} and 签字 non-empty and images OK)
    Y = signed rows whose 判定 is 有差异   (Y is a subset of X -- kept visible)
    Z = everything else (not signed / 没比到 / image missing / bad verdict)

EXIT CODE
---------
    Z > 0 -> 1        (red; includes "not signed yet")
    nothing to check at all -> 2
    else -> 0

CLI
---
    python -I -X utf8 工具/fp_sbs_check.py --help
    python -I -X utf8 工具/fp_sbs_check.py                     # full check of the ledger
    python -I -X utf8 工具/fp_sbs_check.py --dry               # existence/size only
    python -I -X utf8 工具/fp_sbs_check.py --only FP-CARD-001
    python -I -X utf8 工具/fp_sbs_check.py --json _tmp_view/sbs/report.json
    python -I -X utf8 工具/fp_sbs_check.py --scan d:/4/_tmp_view/cmp --dry

Notes
-----
* TSV is read with a bare split('\t') (same convention as the ledger tools).
* READ-ONLY outside --out (default d:/4/_tmp_view/sbs/).
* A row is NEVER auto-signed, even when the difference metric is tiny.
"""
import argparse
import glob as globmod
import io
import json
import os
import re
import sys
import time

import numpy as np
from PIL import Image, ImageFilter

sys.stdout.reconfigure(encoding='utf-8')

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(HERE)                      # d:/4/Unity
LEDGER_DEFAULT = os.path.join(REPO, '资料', '完全复刻', '判据', '功能点_并排比记账.tsv')
OUT_DEFAULT = 'd:/4/_tmp_view/sbs'

HEADER = ['FP_ID', '件', '渲染命令', '我们的图', '原版图', '差异指标', '判定', '签字', '日期', '备注']
PARTS = {'卡面', '特效', '窗口', '动画', '音频'}
VERDICTS = {'一致', '有差异', '没比到'}

WORK = (640, 360)
SMOOTH = 6
PAD = 0.06

ROOTS = [
    'd:/4/Unity/',
    'd:/4/Unity/MyGame/Assets/',
    'd:/4/Unity/资料/完全复刻/',
    'd:/4/',
]


# ---------------------------------------------------------------- reuse arena_diff

def _load_arena_diff():
    """Import 工具/arena_diff.py (four functions only); never modify it."""
    if HERE not in sys.path:
        sys.path.insert(0, HERE)
    import arena_diff as ad
    for name in ('align', 'tone_match', 'rank_match', 'label_blobs'):
        if not hasattr(ad, name):
            raise RuntimeError('arena_diff.py 缺少函数 %s' % name)
    return ad


AD = _load_arena_diff()


# ---------------------------------------------------------------- helpers

def resolve_path(p):
    if not p:
        return None
    p = p.strip().replace('\\', '/')
    if os.path.isabs(p) and os.path.exists(p):
        return p
    for r in ROOTS:
        cand = r + p
        if os.path.exists(cand):
            return cand
    return None


def read_tsv(p):
    hdr = None
    rows = []
    with io.open(p, encoding='utf-8', newline='') as f:
        for raw in f:
            line = raw.rstrip('\r\n')
            if not line or line.startswith('#'):
                continue
            cells = line.split('\t')
            if hdr is None:
                hdr = cells
                continue
            rows.append(cells)
    return hdr, rows


def aspect_ok(w1, h1, w2, h2, tol=0.02):
    r1 = w1 / float(h1)
    r2 = w2 / float(h2)
    return abs(r1 - r2) <= tol * max(r1, r2)


def analyze(ours_p, orig_p, outdir, top=6, thr_hi=30.0, thr_lo=18.0):
    """Tone-normalise -> gaussian -> connected components, reusing arena_diff."""
    orig = Image.open(orig_p).convert('RGB')
    ours = Image.open(ours_p).convert('RGB')
    os.makedirs(outdir, exist_ok=True)

    a = np.asarray(orig.resize(WORK, Image.LANCZOS)).astype(float)
    b = np.asarray(ours.resize(WORK, Image.LANCZOS)).astype(float)
    b, dx, dy = AD.align(a, b)
    bn = AD.rank_match(a, b)
    diff = np.abs(bn - a).mean(axis=2)
    sm = np.asarray(Image.fromarray(np.clip(diff, 0, 255).astype(np.uint8))
                    .filter(ImageFilter.GaussianBlur(SMOOTH))).astype(float)
    m = abs(dy) + 2
    if m:
        sm[:m, :] = 0.0
        sm[-m:, :] = 0.0
    m = abs(dx) + 2
    if m:
        sm[:, :m] = 0.0
        sm[:, -m:] = 0.0

    blobs = []
    for thr in (thr_hi, thr_lo, 8.0):
        blobs = AD.label_blobs(sm > thr)
        if len(blobs) >= top:
            break

    scored = []
    for cx, cy, n, bb in blobs:
        x0, y0, x1, y1 = bb
        sub = sm[y0:y1 + 1, x0:x1 + 1]
        mean = float(sub.mean())
        d = (bn - a).mean(axis=2)[y0:y1 + 1, x0:x1 + 1]
        scored.append({'cx': cx / WORK[0], 'cy': cy / WORK[1], 'px': n,
                       'box': [x0 / WORK[0], y0 / WORK[1], x1 / WORK[0], y1 / WORK[1]],
                       'mean': mean, 'score': n * mean,
                       'dir': '+' if d.mean() > 0 else '-'})
    scored.sort(key=lambda z: -z['score'])
    scored = scored[:top]

    W, H = 720, 405
    o2, u2 = orig.resize((W, H), Image.LANCZOS), ours.resize((W, H), Image.LANCZOS)
    sbs = Image.new('RGB', (W * 2 + 8, H), (15, 15, 15))
    sbs.paste(o2, (0, 0))
    sbs.paste(u2, (W + 8, 0))
    sbs.save(os.path.join(outdir, 'sbs.png'))
    ov = np.asarray(u2).copy()
    nm = np.asarray(Image.fromarray(np.clip(sm, 0, 255).astype(np.uint8))
                    .resize((W, H), Image.BILINEAR)).astype(float) > (thr_lo * 0.9)
    ov[nm] = [255, 0, 255]
    Image.fromarray(ov).save(os.path.join(outdir, 'mask.png'))

    for i, z in enumerate(scored):
        x0, y0, x1, y1 = z['box']
        x0, y0 = max(0.0, x0 - PAD), max(0.0, y0 - PAD)
        x1, y1 = min(1.0, x1 + PAD), min(1.0, y1 + PAD)

        def crop(im):
            Wp, Hp = im.size
            c = im.crop((int(x0 * Wp), int(y0 * Hp), int(x1 * Wp), int(y1 * Hp)))
            return c.resize((300, max(1, int(300 * c.height / max(1, c.width)))), Image.LANCZOS)

        ca, cb = crop(orig), crop(ours)
        h = max(ca.height, cb.height)
        c = Image.new('RGB', (610, h), (15, 15, 15))
        c.paste(ca, (0, 0))
        c.paste(cb, (310, 0))
        c.save(os.path.join(outdir, 'z%02d.png' % (i + 1)))

    return {'mean_diff': float(sm.mean()), 'dx': int(dx), 'dy': int(dy),
            'n_blobs': len(scored), 'top': scored}


# ---------------------------------------------------------------- ledger mode

def check_row(row, outdir, dry, idx):
    fp = row[0].strip()
    part = row[1].strip()
    verdict = row[6].strip()
    signer = row[7].strip()
    ours_p = resolve_path(row[3])
    orig_p = resolve_path(row[4])
    reasons = []
    if part and part not in PARTS:
        reasons.append('件 不在闭集: %s' % part)
    if verdict and verdict not in VERDICTS:
        reasons.append('判定 不在闭集: %s' % verdict)
    if not verdict:
        reasons.append('判定 为空')
    imgs_ok = True
    info = ''
    if not ours_p:
        reasons.append('我们的图不存在: %s' % row[3])
        imgs_ok = False
    if not orig_p:
        reasons.append('原版图不存在: %s' % row[4])
        imgs_ok = False
    if imgs_ok:
        try:
            im_o = Image.open(ours_p)
            im_g = Image.open(orig_p)
            if not aspect_ok(im_o.size[0], im_o.size[1], im_g.size[0], im_g.size[1]):
                reasons.append('尺寸比例差: %s vs %s' % (im_o.size, im_g.size))
            info = '%s vs %s' % (im_o.size, im_g.size)
        except Exception as e:
            reasons.append('打不开图: %s' % e)
            imgs_ok = False
    if verdict == '没比到':
        reasons.append('判定 = 没比到')
    signed = (imgs_ok and not reasons and verdict in ('一致', '有差异') and signer)
    if not signer:
        reasons.append('未签字')
    analysis = None
    if imgs_ok and not dry:
        try:
            analysis = analyze(ours_p, orig_p, os.path.join(outdir, '%s__%02d' % (fp or 'row', idx)))
            info += ' mean_diff=%.2f blobs=%d' % (analysis['mean_diff'], analysis['n_blobs'])
        except Exception as e:
            info += ' analyze_error=%s' % e
    return {'fp': fp, 'part': part, 'verdict': verdict, 'signer': signer,
            'signed': bool(signed), 'reasons': reasons, 'info': info, 'analysis': analysis}


def run_ledger(args):
    if not os.path.exists(args.ledger):
        print('ledger not found: %s' % args.ledger)
        return None
    hdr, rows = read_tsv(args.ledger)
    if hdr != HEADER:
        print('WARN: ledger header differs from the 10-column contract')
        print('  got: %r' % (hdr,))
    only = [x.strip() for x in args.only.split(',') if x.strip()]
    results = []
    for i, cells in enumerate(rows):
        if len(cells) < len(HEADER):
            cells = cells + [''] * (len(HEADER) - len(cells))
        if only and not any(cells[0].startswith(o) for o in only):
            continue
        r = check_row(cells, args.out, args.dry, i + 1)
        results.append(r)
        tag = 'SIGNED' if r['signed'] else 'RED   '
        why = '' if r['signed'] else ' <- ' + '; '.join(r['reasons'])
        print('[%s] %-14s %-4s %-6s %s%s' % (tag, r['fp'], r['part'], r['verdict'], r['info'], why))
    return hdr, results


# ---------------------------------------------------------------- scan mode

def scan_pairs(d):
    """Return [(name, ours, orig)] from a dir with manifest.tsv or __exp/__orig pairs."""
    d = d.replace('\\', '/')
    man = os.path.join(d, 'manifest.tsv')
    pairs = []
    if os.path.exists(man):
        _, rows = read_tsv(man)
        for r in rows:
            if len(r) < 5:
                continue
            name = r[0]
            ours = resolve_path(r[1])
            orig = resolve_path(r[2])
            pairs.append((name, ours, orig))
        return pairs, 'manifest.tsv'
    for f in sorted(globmod.glob(os.path.join(d, '*__orig.png'))):
        base = f[:-len('__orig.png')]
        exp = base + '__exp.png'
        pairs.append((os.path.basename(base), exp if os.path.exists(exp) else None, f))
    return pairs, 'pair-glob'


def run_scan(args):
    pairs, how = scan_pairs(args.scan)
    print('scan %s (%s): %d pair(s)' % (args.scan, how, len(pairs)))
    ok = 0
    fail = 0
    cand = 0
    results = []
    for name, ours, orig in pairs:
        if not ours or not orig or not os.path.exists(ours) or not os.path.exists(orig):
            print('  [MISS] %s  ours=%s orig=%s' % (name, bool(ours), bool(orig)))
            fail += 1
            continue
        try:
            im_o = Image.open(ours)
            im_g = Image.open(orig)
        except Exception as e:
            print('  [MISS] %s  open error: %s' % (name, e))
            fail += 1
            continue
        info = '%s vs %s' % (im_o.size, im_g.size)
        if not aspect_ok(im_o.size[0], im_o.size[1], im_g.size[0], im_g.size[1]):
            info += ' (aspect differs)'
        if args.dry:
            print('  [OK]   %s  %s' % (name, info))
            ok += 1
            results.append({'name': name, 'ok': True, 'info': info})
            continue
        try:
            safe = re.sub(r'[^A-Za-z0-9_.\-]+', '_', name)[:80]
            a = analyze(ours, orig, os.path.join(args.out, 'scan', safe), top=args.top)
            print('  [OK]   %s  %s  mean_diff=%.2f blobs=%d' % (name, info, a['mean_diff'], a['n_blobs']))
            ok += 1
            cand += a['n_blobs']
            results.append({'name': name, 'ok': True, 'info': info, 'analysis': a})
        except Exception as e:
            print('  [MISS] %s  analyze error: %s' % (name, e))
            fail += 1
            results.append({'name': name, 'ok': False, 'info': str(e)})
    print('scan: ok %d / miss %d / candidate blocks %d' % (ok, fail, cand))
    return results, ok, fail


# ---------------------------------------------------------------- main

def main():
    ap = argparse.ArgumentParser(
        description='Check 判据/功能点_并排比记账.tsv (10 cols); opens both images and '
                    'reuses arena_diff.py align/rank_match/label_blobs to emit candidate blocks. '
                    'Never signs a row.',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__)
    ap.add_argument('--ledger', default=LEDGER_DEFAULT, help='side-by-side ledger TSV (10 cols)')
    ap.add_argument('--only', default='', help='comma separated FP ids (prefix match)')
    ap.add_argument('--scan', default='', help='scan an image dir and emit candidates only (no ledger)')
    ap.add_argument('--dry', action='store_true', help='existence + size only, no image analysis')
    ap.add_argument('--top', type=int, default=6, help='candidate blocks per image (default 6)')
    ap.add_argument('--json', default='', help='write a JSON report (default beside --out)')
    ap.add_argument('--out', default=OUT_DEFAULT, help='output dir for candidate images')
    args = ap.parse_args()

    os.makedirs(args.out, exist_ok=True)
    results = []
    X = Y = Z = 0
    measured = 0

    if args.scan:
        results, ok, fail = run_scan(args)
        measured = len(results)
        # a scan never signs anything: it is proof the pipeline runs
        print('=== FP 并排比：已签 0 / 有差异 0 / 没比到 0 ===')
        print('(scan mode: the ledger is not read and nothing is signed)')
        rc = 0 if measured else 2
        report = {'mode': 'scan', 'pairs': measured, 'ok': ok, 'miss': fail,
                  'signed': 0, 'diff': 0, 'not_compared': 0, 'rc': rc, 'rows': results}
    else:
        got = run_ledger(args)
        if got is None:
            return 2
        _, results = got
        measured = len(results)
        for r in results:
            if r['signed']:
                X += 1
                if r['verdict'] == '有差异':
                    Y += 1
            else:
                Z += 1
        print('=== FP 并排比：已签 %d / 有差异 %d / 没比到 %d ===' % (X, Y, Z))
        rc = 1 if Z > 0 else (0 if X > 0 else 2)
        report = {'mode': 'ledger', 'signed': X, 'diff': Y, 'not_compared': Z,
                  'rows': results, 'rc': rc}

    report['generated'] = time.strftime('%Y-%m-%d %H:%M:%S')
    if args.json:
        jp = args.json
        if not os.path.isabs(jp):
            if jp.replace('\\', '/').startswith('_tmp_view/'):
                jp = 'd:/4/' + jp.replace('\\', '/')
            else:
                jp = os.path.join(args.out, os.path.basename(jp))
        os.makedirs(os.path.dirname(jp), exist_ok=True)
        with io.open(jp, 'w', encoding='utf-8', newline='\n') as f:
            json.dump(report, f, ensure_ascii=False, indent=1)
        print('json -> %s' % jp)
    print('rc=%d' % rc)
    return rc


if __name__ == '__main__':
    sys.exit(main())
