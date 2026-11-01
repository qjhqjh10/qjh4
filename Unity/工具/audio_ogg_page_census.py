#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""audio_ogg_page_census.py — **只读**普查「我们自己的 `.ogg` 页头 granule 是否等于实际解码帧数」。

为什么单开一个脚本（`B-241`，2026-11-01）
------------------------------------------
`判据/B112W_B197_granule假说.md` 只查过 **B450**（备份里的 450 份原样）＋ **C43**（当时工程里的 43 份）。
`Resources/Art/audio/` 现在有 **1,822 份 `.ogg`**（`vo/` 1,785 + `sfx/` 10 + `bgm/` 25 + `music/` 2），
剩下约 1,292 份**从没做过页结构扫描**。「若 `B-204` 判到 FMOD 会看中间页 granule」
⇒ 需要知道**要修的是多少份**，这个数字只能扫出来。

判据（与 B197 §2·2 同一套，但换了个更便宜的量法）
--------------------------------------------------
B197 §2·2 的量法是「**截断到该页末尾 + granule 设 1e7 + 打 EOS + 重算 CRC，再用 ffmpeg native 解**」
⇒ 每份文件要跑 `页数` 次解码、O(n²)。本脚本改用**等价但不用解码**的量法（B197 §2·2·2 自己写的那句
「这个数与解码器无关：每包产出多少样点由包内 `blockflag` 决定，是码流自身属性」）：

* `块长` 是标识头的 `blocksize_0 = 2^(b&0xF)` / `blocksize_1 = 2^(b>>4)`；
* `blockflag` 是**模式号的最低位**，模式号 = 每个音频包的**头几个 bit**（位数 = `ilog(mode_count-1)`）；
  模式表在 **setup 头**里（本脚本完整解析 setup 头才能跳过 codebook/floor/residue/mapping 读到模式表）；
* 累计样点（Vorbis I §4.3）：第 0 个包不吐样点，第 i 个包吐 `(block[i-1]/4 + block[i]/4)`。

⇒ 页 granule 的**正确定义** = 「到这一页为止（含本页内**结束**的那些包）已经吐出的累计样点」。
  末页 granule **可以**小于累计（EOS 裁尾），所以末页单独判（`<= 累计` 且 `== 原版真值`）。

⚠️ **三个不许忘的口径**
1. `assets_full` 里的 `.ogg` **全是坏的**（1,797/1,797 解码时长 ≠ 原版）⇒ ⛔ 别拿它当源。
2. 本脚本**只读**：不改任何 `.ogg`、不回滚、不重编码。唯一的写盘 = `--out` 的 JSON。
3. `--validate-ffmpeg` 那次会写临时文件（`tempfile.mkdtemp()`，跑完删）—— 那是**交叉验证**，
   拿 B197 §2·2 的原量法（真解码）对同一批文件的一个抽样，证明本脚本的块长算法没算错。

用法（**不带 `--out` 就只打用法、什么都不做** —— 本项目有过 `--help` 误触真写入的先例）
------------------------------------------------------------------------------------
    python -I -X utf8 工具/audio_ogg_page_census.py --out _tmp_view/x/census.json
    # 抽 40 份用 B197 的原量法（ffmpeg 真解码）交叉验证块长算法：
    python -I -X utf8 工具/audio_ogg_page_census.py --out _tmp_view/x/census.json --validate-ffmpeg 40
    # 只扫一个子目录 / 前 N 份：
    ... --only vo/ --limit 200

输出 JSON 结构
--------------
    {"root": ..., "generated": ..., "totals": {...},
     "files": [{"file": "vo/xx.ogg", "npages": 4, "naudio_pages": 2,
                "bs0": 256, "bs1": 2048, "modes": [0,1], "packets": 251,
                "pages": [{"i":2, "seq":2, "nseg":250, "gran":31872, "cum":164928, "ok":false}, ...],
                "last_gran": ..., "cum_total": ..., "truth": ..., "last_ok": ..., "mid_ok": ...}]}
"""
import argparse
import io
import json
import os
import struct
import sys

PROJECT_ROOT = "d:/4/Unity"
AUDIO_DIR = PROJECT_ROOT + "/MyGame/Assets/CardPresentation/Resources/Art/audio"
CLIP_INDEX = "d:/4/_tmp_view/b112_r1/clip_index.json"


# ---------------------------------------------------------------- Ogg 页解析
def parse_pages(b):
    """→ `(页 list, 认得的字节数)`。认不出（不是 `OggS`）就停在那儿。"""
    i = 0
    out = []
    n = len(b)
    while i + 27 <= n and b[i:i + 4] == b"OggS":
        flags = b[i + 5]
        gran = struct.unpack_from("<q", b, i + 6)[0]
        serial = struct.unpack_from("<I", b, i + 14)[0]
        seq = struct.unpack_from("<I", b, i + 18)[0]
        crc = struct.unpack_from("<I", b, i + 22)[0]
        nseg = b[i + 26]
        seg = list(b[i + 27:i + 27 + nseg])
        body = sum(seg)
        end = i + 27 + nseg + body
        if end > n:
            break
        out.append(dict(off=i, end=end, flags=flags, cont=bool(flags & 0x01),
                        eos=bool(flags & 0x04), gran=gran, serial=serial, seq=seq,
                        crc=crc, nseg=nseg, seg=seg, body=body, data=b[i + 27 + nseg:end]))
        i = end
    return out, i


def _packets(pages):
    """→ 逐包 `(包的字节, 该包**结束**在哪一页的 index)`。跨页包（255-lace）会拼起来。"""
    out = []
    cur = bytearray()
    for pi, pg in enumerate(pages):
        body_start = pg["off"] + 27 + pg["nseg"]
        p = body_start
        for lace in pg["seg"]:
            cur += pg["data"][p - body_start:p - body_start + lace]
            p += lace
            if lace < 255:
                out.append((bytes(cur), pi))
                cur = bytearray()
    if cur:
        out.append((bytes(cur), len(pages) - 1))     # 文件末尾未收尾的包（异常，但要报）
    return out


# ---------------------------------------------------------------- bit 读
class Bits(object):
    """**LSB-first** 读位 —— Vorbis 的位打包就是 LSB-first（`oggpack_read` 从每字节的 bit0 往上读）。

    ⚠️ 2026-11-01 踩过一次：写成 MSB-first 时，标识头之外什么都读不出 —— 症状是
    **codebook sync 读出 `0x424356`**（应 `0x564342`）。那是同一个值按字节顺序反过来，
    因为 `0x564342` 的 24 位按 LSB-first 打出来正好是 `42 43 56`（拿一份 ffmpeg/libvorbis
    现编的参考文件一比就确认了，别怀疑文件本身）。"""
    __slots__ = ("d", "pos")

    def __init__(self, data, pos=0):
        self.d = data
        self.pos = pos

    def read(self, n):
        v = 0
        for k in range(n):
            byte = self.pos >> 3
            bit = self.pos & 7
            v |= ((self.d[byte] >> bit) & 1) << k
            self.pos += 1
        return v

    def skip(self, n):
        self.pos += n


def ilog(x):
    """Vorbis 的 `ilog(x)`：x 的二进制位数（ilog(0)=0）。"""
    return x.bit_length()


def find_modes(setup):
    """从 setup 头**尾部**反推模式表 → `(nmode, mode_blockflags)`。找不到唯一解就抛 `ValueError`。

    为什么反推而不是从头解：模式表是 setup 头的**最后一项**（`mode_count(6)+1` →
    `nmode × [blockflag(1) + windowtype(16) + transformtype(16) + mapping(8)]` → `framing(1)`），
    而它前面塞着 codebook / floor / residue / mapping 几大段 —— 那几段要从头逐字段跳，
    任何一处读歪都会静默漂到后面（2026-11-01 实测：本脚本第一版照 spec 逐段解，**在 residue 段读歪**，
    读出的 `residue_type = 7452`）。反推只需要「末尾对齐 + windowtype/transformtype 恒 0 +
    framing == 1」三条硬约束，判据全部来自 spec，且**不依赖任何前半段**。

    约束（缺一不可，所以假阳性极低）：
      · `mode_count_read + 1 == nmode`，且整张表读完后**正好落在**包尾（允许 `0..7` 位零填充）；
      · 每个模式的 `window_type` / `transform_type` 都是 0（Vorbis I §4.2.4 只定义 0）；
      · framing 位 == 1。"""
    if not (len(setup) > 7 and setup[0] == 5 and setup[1:7] == b"vorbis"):
        raise ValueError("不是 setup 头（包类型应 = 5）")
    nbits = len(setup) * 8
    cands = []
    for pad in range(0, 8):
        end = nbits - pad
        for nmode in range(1, 5):
            start = end - 1 - nmode * 41 - 6
            if start < 8 * 7:
                continue
            br = Bits(setup, start)
            if br.read(6) + 1 != nmode:
                continue
            bfs, ok = [], True
            for _m in range(nmode):
                bf = br.read(1)
                if br.read(16) != 0 or br.read(16) != 0:
                    ok = False
                    break
                if br.read(8) > 63:
                    ok = False
                    break
                bfs.append(bf)
            if not ok or br.read(1) != 1 or br.pos != end:
                continue
            cands.append((pad, nmode, tuple(bfs)))
    if not cands:
        raise ValueError("setup 头尾部找不到合法的模式表")
    return [(nmode, bfs) for _pad, nmode, bfs in cands]


# ---------------------------------------------------------------- 单文件
def census_file(path, truth=None):
    """→ dict（页结构 + 每页 `gran` vs 实算累计）。解析不动就 `raise ValueError`。

    `truth` = 原版 FSB5 真值总样点数（`len × freq`），有它才判「末页 granule 对不对」。"""
    b = io.open(path, "rb").read()
    pages, end = parse_pages(b)
    if len(pages) < 3:
        raise ValueError("页数 %d < 3（连标识/注释/setup 都不全）" % len(pages))
    pkts = _packets(pages)

    # 包 0 = 标识头（30 字节）；包 1 = 注释；包 2 = setup
    ident = pkts[0][0]
    if len(ident) < 30 or ident[0] != 1 or ident[1:7] != b"vorbis":
        raise ValueError("包 0 不是标识头")
    bs0 = 1 << (ident[28] & 0x0F)
    bs1 = 1 << (ident[28] >> 4)
    comment = pkts[1][0]
    if not (comment and comment[0] == 3 and comment[1:7] == b"vorbis"):
        raise ValueError("包 1 不是注释头")
    cands = find_modes(pkts[2][0])

    audio = pkts[3:]
    if not audio:
        raise ValueError("没有音频包")

    # 逐包：模式号 → blockflag → 块长；累计（第 0 个包不吐样点）
    #  第 i 个包吐 (block[i-1]/4 + block[i]/4)，i >= 1
    def _walk(nmode, blockflags, modebits):
        """→ `(per_page_cum, cum_total)`；模式号越界 ⇒ `None`。"""
        per, cum, prev = {}, 0, None
        for k, (data, pidx) in enumerate(audio):
            if not data:
                raise ValueError("第 %d 个音频包是空的" % k)
            # 🔴 音频包**第一个 bit 是「包类型」标志，必须为 0**（`vorbis_synthesis`：`if(oggpack_read(opb,1)!=0) return OV_ENOTAUDIO;`），
            #    **模式号在它后面**。2026-11-01 踩过：从 bit0 直接读模式 ⇒ 每个包都读出「模式 0」，
            #    整棵树都算成同一种块长（实测 3 个文件的全部音频包 bit0 **恒为 0**，一眼就该看出来）。
            br = Bits(data, 0)
            if br.read(1) != 0:
                raise ValueError("第 %d 个音频包的包类型位不是 0（不是音频包）" % k)
            m = br.read(modebits) if modebits else 0
            if m >= nmode:
                return None, None
            bs = bs1 if blockflags[m] else bs0
            if prev is not None:
                cum += (prev + bs) // 4
            prev = bs
            per[pidx] = cum
        for pi in range(len(pages)):
            per.setdefault(pi, per.get(pi - 1, 0))
        return per, cum

    # 模式表从尾部反推时，`nmode=1` 与 `nmode=2` 常同时「合法」（前者少读 47 位也能凑出一张表）。
    # **判据 = 哪种读法让更多页的 granule 对得上**（本普查问的就是这个），其次取更大的 nmode。
    best = None
    for nmode, blockflags in cands:
        modebits = ilog(nmode - 1)
        per, cum = _walk(nmode, blockflags, modebits)
        if per is None:
            continue
        score = sum(1 for pi in range(2, len(pages)) if pages[pi]["gran"] == per.get(pi, 0))
        key = (score, nmode)
        if best is None or key > best[0]:
            best = (key, nmode, blockflags, modebits, per, cum)
    if best is None:
        raise ValueError("所有模式表候选都读不出一致的样点累计")
    _key, nmode, blockflags, modebits, per_page_cum, cum = best

    # 逐页对账：页 0/1 是头（granule 应 0）；页 2.. 是音频页
    rows = []
    for pi, pg in enumerate(pages):
        cum_pi = per_page_cum.get(pi, 0)
        ok = (pg["gran"] == cum_pi) if pi >= 2 else (pg["gran"] == 0)
        rows.append(dict(i=pi, seq=pg["seq"], nseg=pg["nseg"], cont=pg["cont"], eos=pg["eos"],
                         gran=pg["gran"], cum=cum_pi, ok=ok))

    last = rows[-1]
    # 🔴 **末页 granule 不等于累计**是**正常**的：EOS 会把最后一个长块的尾巴裁掉（实测 ref 文件
    #    裁 508、`Warpforge Aeldari Combat High` 裁 251），所以末页单独判 —— 优先对**原版真值**
    #    （`clip_index.json` 的 `len × freq`），没有真值才用「`0 <= 累计 − granule < blocksize_1`」。
    eos_trim = cum - last["gran"]
    if truth is not None:
        last_ok = (last["gran"] == truth)
    else:
        last_ok = (0 <= eos_trim < bs1)
    last["ok"] = last_ok
    last["eos_trim"] = eos_trim

    return dict(path=path, npages=len(pages), naudio_pages=max(0, len(pages) - 2),
                bs0=bs0, bs1=bs1, nmodes=nmode, modebits=modebits,
                modes=blockflags, mode_cands=[list(c[1]) for c in cands],
                packets=len(pkts), audio_packets=len(audio),
                pages=rows, last_gran=last["gran"], cum_total=cum,
                truth=truth, eos_trim=eos_trim,
                in_pages_ok=all(r["ok"] for r in rows[:-1]),
                last_page_ok=last_ok,
                mid_ok=all(r["ok"] for r in rows[2:-1]),
                unparsed_tail=len(b) - end)


# ---------------------------------------------------------------- ffmpeg 交叉验证
FF_CANDIDATES = [r"D:/4/_tmp_view/b112_r1w/tools/ffmpeg/ffmpeg.exe"]


def _ffmpeg():
    for p in FF_CANDIDATES:
        if os.path.exists(p):
            return p
    return None


_CRC_TPL = None


def _crc_tpl():
    global _CRC_TPL
    if _CRC_TPL is None:
        t = [0] * 256
        for i in range(256):
            r = i << 24
            for _ in range(8):
                r = ((r << 1) ^ 0x04C11DB7) & 0xFFFFFFFF if (r & 0x80000000) else ((r << 1) & 0xFFFFFFFF)
            t[i] = r
        _CRC_TPL = t
    return _CRC_TPL


def _crc(pg):
    t = _crc_tpl()
    c = 0
    for x in pg:
        c = ((c << 8) & 0xFFFFFFFF) ^ t[((c >> 24) ^ x) & 0xFF]
    return c


def _prefix_to_page(b, pages, idx, big=10000000):
    """B197 §2·2 的原量法：截断到第 idx 页末尾、granule 设 `big`、打 EOS、重算 CRC。"""
    p = pages[idx]
    pre = bytearray(b[:p["end"]])
    struct.pack_into("<q", pre, p["off"] + 6, big)
    pre[p["off"] + 5] |= 0x04
    struct.pack_into("<I", pre, p["off"] + 22, 0)
    struct.pack_into("<I", pre, p["off"] + 22, _crc(bytes(pre[p["off"]:p["end"]])))
    return bytes(pre)


def validate_ffmpeg(files, sample_n, seed=20261101):
    """抽样 N 份，用 ffmpeg native 真解码量「每页累计」，与本脚本的块长算法逐页比。→ (ok, bad list)"""
    import random
    import shutil
    import subprocess
    import tempfile
    ff = _ffmpeg()
    if not ff:
        print(u"⚠️ 找不到 ffmpeg（%s）⇒ 跳过交叉验证" % FF_CANDIDATES[0])
        return 0, [], 0
    cand = [f for f in files if f["naudio_pages"] >= 1 and not f.get("err")]
    random.Random(seed).shuffle(cand)
    sample = cand[:sample_n]
    tmp = tempfile.mkdtemp(prefix="wf_oggcensus_")
    ok = 0
    bad = []
    try:
        for rec in sample:
            b = io.open(rec["path"], "rb").read()
            pages, _e = parse_pages(b)
            rows = rec["pages"]
            # 每个中间音频页 + 末页前一页各量一次
            idxs = [r["i"] for r in rows[2:]] or []
            for idx in idxs:
                ogg = os.path.join(tmp, "_p.ogg")
                wav = os.path.join(tmp, "_p.wav")
                with io.open(ogg, "wb") as f:
                    f.write(_prefix_to_page(b, pages, idx))
                if os.path.exists(wav):
                    os.remove(wav)
                subprocess.run([ff, "-nostdin", "-y", "-v", "quiet", "-c:a", "vorbis",
                                "-i", ogg, "-f", "wav", "-acodec", "pcm_s16le", wav],
                               capture_output=True)
                if not os.path.exists(wav):
                    bad.append(dict(file=rec["file"], page=idx, why="ffmpeg 没产出"))
                    continue
                import soundfile as sf
                a, _sr = sf.read(wav, dtype="int16", always_2d=True)
                got = int(len(a))
                want = rows[idx]["cum"]
                # ffmpeg 对「截断前缀 + 巨大 granule + EOS」的输出 = 该页为止的累计（B197 §2·2·2）
                if abs(got - want) <= 1:
                    ok += 1
                else:
                    bad.append(dict(file=rec["file"], page=idx, want=want, got=got, diff=got - want))
    finally:
        shutil.rmtree(tmp, ignore_errors=True)
    return ok, bad, len(sample)


# ---------------------------------------------------------------- main
def main():
    ap = argparse.ArgumentParser(
        description="只读普查：我们自己的 .ogg 的页头 granule 是否 == 实际解码帧数（B-241）")
    ap.add_argument("--out", help="**必填**：结果 JSON 落点（不给就只打用法，什么都不做）")
    ap.add_argument("--root", default=AUDIO_DIR, help="音频根目录（默认 %s）" % AUDIO_DIR)
    ap.add_argument("--clip-index", default=CLIP_INDEX,
                    help="原版 FSB5 真值索引 clip_index.json（含 len×freq；没有就跳过末页对账）")
    ap.add_argument("--only", help="只扫相对路径含这个子串的文件")
    ap.add_argument("--limit", type=int, help="只扫前 N 份（冒烟用）")
    ap.add_argument("--validate-ffmpeg", type=int, metavar="N", default=0,
                    help="抽 N 份，用 ffmpeg 真解码（B197 §2·2 原量法）交叉验证块长算法")
    a = ap.parse_args()
    if not a.out:
        ap.print_help()
        print(u"\n⛔ 没给 --out ⇒ 什么都没做（本脚本要求显式落点）")
        return 2

    truth = {}
    if a.clip_index and os.path.exists(a.clip_index):
        try:
            raw = json.load(io.open(a.clip_index, encoding="utf-8"))
            for k, v in raw.items():
                if isinstance(v, dict) and "len" in v and "freq" in v:
                    truth[k] = int(round(float(v["len"]) * float(v["freq"])))
        except Exception as e:                                   # noqa: F841
            print(u"⚠️ 读不了 clip_index（%s）⇒ 末页对账只比「granule == 累计」" % e)

    targets = []
    for dp, _dn, fn in os.walk(a.root):
        for f in sorted(fn):
            if not f.lower().endswith(".ogg"):
                continue
            p = os.path.join(dp, f)
            rel = os.path.relpath(p, a.root).replace("\\", "/")
            if a.only and a.only not in rel:
                continue
            targets.append((rel, p))
    targets.sort()
    if a.limit:
        targets = targets[:a.limit]
    print(u"扫 %d 份 .ogg（根 = %s）" % (len(targets), a.root))

    files, errs = [], []
    for i, (rel, p) in enumerate(targets, 1):
        try:
            r = census_file(p, truth.get(os.path.splitext(os.path.basename(p))[0]))
        except Exception as e:                                   # noqa: BLE001
            errs.append((rel, "%s: %s" % (type(e).__name__, e)))
            files.append(dict(file=rel, path=p, err=str(e)))
            continue
        r["file"] = rel
        files.append(r)
        if i % 200 == 0:
            print(u"  … %d/%d" % (i, len(targets)))

    # ---- 汇总
    good = [r for r in files if not r.get("err")]
    tot = dict(files=len(files), parsed=len(good), parse_err=len(errs),
               audio_pages=sum(r["naudio_pages"] for r in good),
               mid_pages=sum(max(0, r["npages"] - 3) for r in good),
               files_mid_bad=[r["file"] for r in good if not r["mid_ok"]],
               files_last_bad=[r["file"] for r in good if not r["last_page_ok"]],
               files_all_ok=[r["file"] for r in good if r["mid_ok"] and r["last_page_ok"]],
               unparsed_tail=sum(1 for r in good if r.get("unparsed_tail")))
    tot["n_mid_bad"] = len(tot["files_mid_bad"])
    tot["n_last_bad"] = len(tot["files_last_bad"])
    tot["n_all_ok"] = len(tot["files_all_ok"])
    tot["n_need_fix"] = len({r["file"] for r in good if not (r["mid_ok"] and r["last_page_ok"])})

    val = None
    if a.validate_ffmpeg:
        oks, bads, ns = validate_ffmpeg(files, a.validate_ffmpeg)
        val = dict(sample=ns, page_checks=oks + len(bads), ok=oks, bad=bads[:30], n_bad=len(bads))
        print(u"【ffmpeg 交叉验证】抽样 %d 份 · 逐页核 %d 处 · 一致 %d · 不一致 %d"
              % (ns, oks + len(bads), oks, len(bads)))

    doc = dict(root=a.root, generated=__import__("time").strftime("%Y-%m-%d %H:%M:%S"),
               totals=tot, parse_errors=errs, validate_ffmpeg=val,
               files=[{k: v for k, v in r.items() if k != "path"} for r in files])
    os.makedirs(os.path.dirname(os.path.abspath(a.out)), exist_ok=True)
    with io.open(a.out, "w", encoding="utf-8", newline="\n") as f:
        f.write(json.dumps(doc, ensure_ascii=False, indent=1) + "\n")
    print(u"\n总数 %d · 解析成功 %d · 解析失败 %d" % (tot["files"], tot["parsed"], tot["parse_err"]))
    print(u"音频页 %d（其中中间页 %d）" % (tot["audio_pages"], tot["mid_pages"]))
    print(u"中间页 granule 不对的文件 %d · 末页不对的文件 %d · **合计要修 %d 份**"
          % (tot["n_mid_bad"], tot["n_last_bad"], tot["n_need_fix"]))
    print(u"→ %s" % a.out)
    for rel, e in errs[:10]:
        print(u"  ✗ %s：%s" % (rel, e))
    return 0 if not errs else 1


if __name__ == "__main__":
    sys.exit(main())
