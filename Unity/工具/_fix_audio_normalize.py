#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""_fix_audio_normalize.py — 把 `Resources/Art/audio/**` 里 `normalize: 1` 的 `.meta` 就地改成 `0`。

为什么是一次性脚本（不能用 `_sfx_meta.py --backfill`）
------------------------------------------------------
`ensure_meta()` 是**幂等**的（`if os.path.exists(meta): return "exists"`）⇒ 已存在的 meta
**一个字节都不会被覆盖**，`--backfill` 碰不到这 55 份（`B114_W10` §一·6 已写明）。

为什么必须改
------------
`normalize: 1` = Unity **导入时做响度归一化**、`0` = 不做 ⇒ 那几条音在游戏里的**音量**与
其余 2,494 条**不同**。原版走 FMOD，**没有这一层归一化**。
⚠️ `normalize` 属**无原版真值**的一项（`AudioImporter` 是编辑器侧设置、不进包体，
`B111_T2` §B-25 已查三处）⇒ 本脚本的口径是「**与其余 2,494 条的现读众数对齐**」，
⛔ **不是**「原版就是这样」。

三条纪律（本脚本强制）
----------------------
1. **只改 `normalize` 那一项**：正则锚死 `^  normalize: 1$`（行首两空格、行尾可带 `\r`），
   替换后**整篇其余字节逐个比对**，有任何别的差异 ⇒ **不写、报红**。
2. **行数守恒**：`1` → `0` 同宽 ⇒ 字节数与行数都必须不变；变了 ⇒ **不写、报红**。
3. **二进制读 / 二进制写**（`io.open(..., "rb"/"wb")`）—— ⛔ 别用文本模式（会翻行尾）。

用法（⛔ 不带显式动作参数 = 只打用法，什么都不做）
------------------------------------------------
    python -I -X utf8 工具/_fix_audio_normalize.py --scan
        # 只读普查：`normalize` 分布 + 逐份清单 + 每份的行尾/行数
    python -I -X utf8 工具/_fix_audio_normalize.py --apply --expect 55
        # 真写盘：只改 normalize 1 → 0；`--expect` 与实际命中数不符则**整批不写**
    python -I -X utf8 工具/_fix_audio_normalize.py --apply --expect 0
        # 幂等复跑：已经是 0 份可改 ⇒ 什么都不写、rc=0
"""
import argparse
import collections
import io
import os
import re
import sys

AUDIO_DIR = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/audio"

# 行首两个空格 + `normalize:` + 值 + 行尾（`\r` 吃掉、原样写回 ⇒ CRLF 文件不会被翻成 LF）
NORMALIZE_RE = re.compile(rb"(?m)^(?P<ind>  normalize:)(?P<sp>[ \t]*)(?P<val>[0-9]+)(?P<eol>\r?)$")


def iter_meta(root=AUDIO_DIR):
    """→ 递归列出所有**音频** `.meta`（含 `AudioImporter:` 的），排序。

    ⚠️ 目录自身的 `.meta`（`sfx.meta` 等）与 `*.json.meta` **都不含 `AudioImporter:`**，
    它们没有 `normalize` 一行是**正常的**，⛔ 不能算「形状异常」。"""
    out = []
    for dirpath, _dirnames, filenames in os.walk(root):
        for f in filenames:
            if not f.endswith(".meta"):
                continue
            p = os.path.join(dirpath, f)
            try:
                head = io.open(p, "rb").read(4096)
            except OSError:
                continue
            if b"AudioImporter:" not in head:
                continue
            out.append(p)
    return sorted(out)


def read_norm(b):
    """→ `(值 bytes, 命中次数)`。命中次数 != 1 说明这份 meta 不是我们能安全改的形状。"""
    ms = NORMALIZE_RE.findall(b)
    if not ms:
        return None, 0
    return ms[0][2], len(ms)


def scan(root=AUDIO_DIR):
    """只读普查。→ 退出码（0 = 形状都正常）"""
    metas = iter_meta(root)
    counts = collections.Counter()
    bad_shape, offenders = [], []
    eol_counter = collections.Counter()
    for p in metas:
        b = io.open(p, "rb").read()
        val, n = read_norm(b)
        if n != 1:
            bad_shape.append((p, n))
            counts["<无/多 normalize 行>"] += 1
            continue
        counts[val.decode("ascii", "replace")] += 1
        eol_counter["CRLF" if b"\r\n" in b else "LF"] += 1
        if val != b"0":
            offenders.append((p, val.decode("ascii", "replace"), len(b), b.count(b"\n")))
    print(u"音频目录：%s" % root)
    print(u"`.meta` 共 %d 份 · normalize 分布：%s"
          % (len(metas), u" · ".join(u"%s × %d" % (k, v) for k, v in sorted(counts.items()))))
    print(u"行尾：%s" % u" · ".join(u"%s × %d" % (k, v) for k, v in sorted(eol_counter.items())))
    print(u"normalize != 0 的 %d 份：" % len(offenders))
    for p, v, nb, nl in offenders:
        print(u"  %s  (normalize: %s · %d 字节 · %d 行)"
              % (os.path.relpath(p, root).replace("\\", "/"), v, nb, nl))
    if bad_shape:
        print(u"🔴 形状异常的 %d 份（normalize 行不是恰好 1 条）：" % len(bad_shape))
        for p, n in bad_shape:
            print(u"  %s → %d 条" % (os.path.relpath(p, root).replace("\\", "/"), n))
    return 0 if not bad_shape else 1


def apply_fix(root=AUDIO_DIR, expect=None, dry_run=False):
    """把 `normalize: 1` 就地改成 `0`。→ 退出码（0 = 全成功或无事可做）

    🚨 三条纪律（模块 docstring）全部在写盘**之前**逐份校验；任何一份不过 ⇒ **整批不写**。"""
    metas = iter_meta(root)
    targets = []
    for p in metas:
        b = io.open(p, "rb").read()
        val, n = read_norm(b)
        if n != 1:
            print(u"🔴 形状异常（normalize 行 %d 条，应 1 条）：%s ⇒ **整批不写**"
                  % (n, os.path.relpath(p, root).replace("\\", "/")))
            return 1
        if val != b"1":
            continue
        targets.append((p, b))

    print(u"待改：%d 份 · 已是 0 的：%d 份" % (len(targets), len(metas) - len(targets)))
    if expect is not None and len(targets) != expect:
        print(u"🔴 实际命中 %d 份，与 --expect %d 不符 ⇒ **整批不写**（先 --scan 看现状）"
              % (len(targets), expect))
        return 1
    if not targets:
        print(u"没有需要改的 ⇒ 什么都不做（幂等）")
        return 0

    # ---- 先在内存里把每一份的新字节算好并逐条自检（此时还没写盘）----
    planned = []
    for p, b in targets:
        def _sub(m):
            return m.group("ind") + m.group("sp") + b"0" + m.group("eol")

        new, n = NORMALIZE_RE.subn(_sub, b, count=1)
        if n != 1:
            print(u"🔴 %s：替换命中 %d 次（应 1）⇒ **整批不写**"
                  % (os.path.relpath(p, root).replace("\\", "/"), n))
            return 1
        # ① 字节数不变（`1` → `0` 同宽）
        if len(new) != len(b):
            print(u"🔴 %s：字节数 %d → %d（应不变）⇒ **整批不写**"
                  % (os.path.relpath(p, root).replace("\\", "/"), len(b), len(new)))
            return 1
        # ② 行数不变
        if new.count(b"\n") != b.count(b"\n"):
            print(u"🔴 %s：行数 %d → %d（应不变）⇒ **整批不写**"
                  % (os.path.relpath(p, root).replace("\\", "/"), b.count(b"\n"), new.count(b"\n")))
            return 1
        # ③ 差异**只**在那一处 `1` → `0`
        idx = [i for i in range(len(b)) if b[i] != new[i]]
        if len(idx) != 1 or b[idx[0]:idx[0] + 1] != b"1" or new[idx[0]:idx[0] + 1] != b"0":
            print(u"🔴 %s：差异 %d 处（应恰好 1 处 `1`→`0`，位置 %r）⇒ **整批不写**"
                  % (os.path.relpath(p, root).replace("\\", "/"), len(idx), idx[:5]))
            return 1
        # ④ 改后重读：normalize 必须是 0、且只有 1 条
        v2, n2 = read_norm(new)
        if n2 != 1 or v2 != b"0":
            print(u"🔴 %s：改后 normalize 读回 %r × %d ⇒ **整批不写**"
                  % (os.path.relpath(p, root).replace("\\", "/"), v2, n2))
            return 1
        planned.append((p, b, new))

    if dry_run:
        print(u"（dry-run：%d 份已通过全部校验，未写盘）" % len(planned))
        for p, _b, _n in planned:
            print(u"  ~ " + os.path.relpath(p, root).replace("\\", "/"))
        return 0

    for p, _b, new in planned:
        with io.open(p, "wb") as f:      # 二进制写：⛔ 别让文本模式把行尾翻掉
            f.write(new)
        print(u"  ✓ " + os.path.relpath(p, root).replace("\\", "/"))
    print(u"已改 %d 份（normalize 1 → 0，其余字节逐个比对未变）" % len(planned))
    return 0


def main():
    ap = argparse.ArgumentParser(
        description="把 audio/**/*.meta 的 normalize: 1 就地改成 0（只改这一项；行数/字节数不变）。"
                    "⛔ 不带 --scan / --apply 时只打用法，什么都不做。")
    ap.add_argument("--scan", action="store_true", help="只读普查：normalize 分布 + 清单")
    ap.add_argument("--apply", action="store_true", help="真写盘：只改 normalize 1 → 0")
    ap.add_argument("--dry-run", action="store_true", help="配合 --apply：全部校验跑一遍但不写盘")
    ap.add_argument("--expect", type=int, default=None,
                    help="配合 --apply：预期命中份数，与实际不符则整批不写（防误批量改）")
    ap.add_argument("--dir", default=AUDIO_DIR, help="音频目录（默认 %s）" % AUDIO_DIR)
    a = ap.parse_args()

    if not (a.scan or a.apply):
        ap.print_help()
        return 0
    rc = 0
    if a.scan:
        rc = scan(a.dir) or rc
    if a.apply:
        rc = apply_fix(a.dir, expect=a.expect, dry_run=a.dry_run) or rc
    return rc


if __name__ == "__main__":
    raise SystemExit(main())
