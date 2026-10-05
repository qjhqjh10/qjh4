#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""A234 · 修复 `d:/2/新解包资源/assets_full/` 全树里 9 个 **0 字节** 文件

成因（已确证，见报告 §A234·1）
------------------------------
`d:/2/Warpforge_tools/scripts/fix_exports.py` 的两支分支**先 `open(..., "wb")` 把文件截成 0、
再去算要写的字节**：

* `:137-145` `Font`      —— `data = d.m_FontData`（UnityPy 给的是**整数 list**）
  ⇒ `f.write(data)` 抛 `TypeError: a bytes-like object is required, not 'list'`
* `:155-160` `TextAsset` —— `f.write(bytes(d.m_Script))`（`m_Script` 是 **str**）
  ⇒ 抛 `TypeError: string argument without an encoding`

异常被 `:186-188` 的外层 `except` 吃掉（只 `fail += 1` + 一行日志），
`with` 退出时把**已经被截断的空文件**关掉 ⇒ **留下 0 字节文件**，
而 `os.path.exists()` 从此恒真 —— 「文件在、内容是空的」会骗过所有 `exists` 消费者。

本脚本做什么
------------
**不重跑任何导出器**（⛔ 红线：重跑会再加一遍重复）。直接从**原始容器**把正确字节取回来：

    容器 = Warpforge_Data/resources.assets          （独立 SerializedFile）
         + …/StreamingAssets/aa/StandaloneWindows64/fonts_assets_all.bundle（包，1 份 CAB）
    取法 = Font:      bytes(obj.read().m_FontData)      # 整数 list → bytes
           TextAsset: obj.read().m_Script.encode("utf-8", "surrogateescape")
                      # 与 UnityPy `read_aligned_string` 的解码**逐字对偶**
                      # （`streams/EndianBinaryReader.py:146-153`）

纪律（都是这个仓库踩过的坑）
----------------------------
1. 🔴 **先把内容算进变量、验完再 write** —— `io.open(p,'wb').write(<表达式>)` 会**先截断再求值**，
   表达式一抛异常文件就永久空了（正是本案的成因）。
2. 🔴 **写完当场回读**再判一次（`SetDirty` 那类「写完当场回读抓不到没落盘」的同族纪律）。
3. 🔴 **写之前先备份**到 `d:/2/_backup_1011/`（保留相对路径）；⛔ 不删任何东西。
4. 🔴 **只认「当前是 0 字节」的目标** —— 目标非空就**跳过 + 告警**，绝不覆盖非空内容。

用法
----
    PY = "d:/2/Warpforge_tools/py312/python.exe"
    $PY d:/4/Unity/工具/a234_fix_zero_byte_exports.py                 # dry-run（只看，不写）
    $PY d:/4/Unity/工具/a234_fix_zero_byte_exports.py --apply         # 真写（自动备份）
    $PY d:/4/Unity/工具/a234_fix_zero_byte_exports.py --verify-only   # 只验盘上现有文件
"""
import argparse
import hashlib
import json
import os
import re
import shutil
import struct
import sys
import time

GAME = r"d:/2/Warhammer 40k Warpforge/Warpforge_Data"
RES_ASSETS = os.path.join(GAME, "resources.assets")
FONTS_BUNDLE = os.path.join(GAME, "StreamingAssets", "aa", "StandaloneWindows64",
                            "fonts_assets_all.bundle")
EXPORT_ROOT = r"d:/2/新解包资源/assets_full"
BACKUP_ROOT = r"d:/2/_backup_1011"
# 独立佐证源（另一次解包，树不同、读法不同）
DECOMP_ROOT = r"d:/2/解包整理/10_字体"

# (导出相对路径, 容器, 类型, 名字, 记录里的 pathID, 佐证)
TARGETS = [
    ("bundle_fonts_assets_all/Font/NotoSerifCJK-Regular.ttf",
     FONTS_BUNDLE, "Font", "NotoSerifCJK-Regular", 363839005159822707, "fonts"),
    ("resources/Font/LiberationSans.ttf",
     RES_ASSETS, "Font", "LiberationSans", 54, "resources"),
    ("resources/Font/NotoSerifCJK-Regular.ttf",
     RES_ASSETS, "Font", "NotoSerifCJK-Regular", 52, "resources"),
    ("resources/Font/PerfectDOSVGA437.ttf",
     RES_ASSETS, "Font", "PerfectDOSVGA437", 53, "resources"),
    ("resources/TextAsset/BillingMode.txt",
     RES_ASSETS, "TextAsset", "BillingMode", 47, None),
    ("resources/TextAsset/LineBreaking Following Characters.txt",
     RES_ASSETS, "TextAsset", "LineBreaking Following Characters", 51, None),
    ("resources/TextAsset/LineBreaking Leading Characters.txt",
     RES_ASSETS, "TextAsset", "LineBreaking Leading Characters", 49, None),
    ("resources/TextAsset/PerformanceTestRunInfo.txt",
     RES_ASSETS, "TextAsset", "PerformanceTestRunInfo", 48, None),
    ("resources/TextAsset/PerformanceTestRunSettings.txt",
     RES_ASSETS, "TextAsset", "PerformanceTestRunSettings", 50, None),
]

SFNT_OK = (b"\x00\x01\x00\x00", b"OTTO", b"true", b"typ1", b"ttcf")


def md5(b):
    return hashlib.md5(b).hexdigest()


# --------------------------------------------------------------------------- #
# 取源
# --------------------------------------------------------------------------- #
def iter_sfs(env):
    """Yield (cab_label, SerializedFile)。⚠️ `SerializedFile.files` 是它自己 `.objects`
    的 property（`UnityPy/files/SerializedFile.py:225-232`）⇒ 必须按类型判、不能按属性判。"""
    from UnityPy.files import BundleFile, SerializedFile

    for top in env.files.values():
        if isinstance(top, SerializedFile):
            yield "<standalone>", top
        elif isinstance(top, BundleFile):
            for cab, inner in top.files.items():
                if isinstance(inner, SerializedFile):
                    yield cab, inner
        else:
            for cab, inner in (getattr(top, "files", {}) or {}).items():
                if isinstance(inner, SerializedFile):
                    yield cab, inner


def pull(env_by_path, container, tn, name):
    """在容器里按 (类型, m_Name) 找对象 → 返回 (cab, pid, bytes, extra)"""
    env = env_by_path[container]
    hits = []
    for cab, sf in iter_sfs(env):
        for pid, obj in (sf.objects or {}).items():
            if obj.type.name != tn:
                continue
            d = obj.read()
            if getattr(d, "m_Name", None) != name:
                continue
            hits.append((cab, pid, d))
    if len(hits) != 1:
        raise RuntimeError(f"{container}: {tn} '{name}' 命中 {len(hits)} 个（应为 1）")
    cab, pid, d = hits[0]
    if tn == "Font":
        fd = d.m_FontData
        if isinstance(fd, (bytes, bytearray)):
            raw = bytes(fd)
        elif isinstance(fd, list):
            raw = bytes(fd)                       # 整数 0..255 ⇒ 逐字节还原
        else:
            raise RuntimeError(f"m_FontData 类型意外: {type(fd)}")
    else:
        sc = d.m_Script
        if isinstance(sc, str):
            raw = sc.encode("utf-8", "surrogateescape")   # 与 read_aligned_string 对偶
        elif isinstance(sc, (bytes, bytearray)):
            raw = bytes(sc)
        else:
            raise RuntimeError(f"m_Script 类型意外: {type(sc)}")
    return cab, pid, raw


# --------------------------------------------------------------------------- #
# 自证
# --------------------------------------------------------------------------- #
def check_sfnt(raw):
    """解析 SFNT/TTC 表目录。返回 (kind, detail)。不通过就抛。"""
    if len(raw) < 12:
        raise ValueError("太短，连 sfnt 头都不够")
    tag = raw[:4]
    if tag == b"ttcf":
        ver, n = struct.unpack(">II", raw[4:12])
        offs = struct.unpack(">%dI" % n, raw[12:12 + 4 * n])
        subs = []
        for i, off in enumerate(offs):
            subs.append(_sfnt_tables(raw, off, f"subfont[{i}]"))
        return "ttc", {"version": f"0x{ver:08x}", "numFonts": n, "offsets": offs,
                       "sub_magic": [s[0] for s in subs],
                       "sub_numTables": [s[1] for s in subs]}
    kind, ntables = _sfnt_tables(raw, 0, "sfnt")
    return kind, {"numTables": ntables}


def _sfnt_tables(raw, base, label):
    if base + 12 > len(raw):
        raise ValueError(f"{label}: 偏移 {base} 越界")
    magic = raw[base:base + 4]
    if magic not in (b"\x00\x01\x00\x00", b"OTTO", b"true", b"typ1"):
        raise ValueError(f"{label}: magic {magic!r} 不是 sfnt/OTTO")
    n = struct.unpack(">H", raw[base + 4:base + 6])[0]
    end = base + 12 + 16 * n
    if end > len(raw):
        raise ValueError(f"{label}: 表目录 {end} 越界（文件 {len(raw)}）")
    for i in range(n):
        rec = raw[base + 12 + 16 * i: base + 28 + 16 * i]
        off, ln = struct.unpack(">II", rec[8:16])
        if off + ln > len(raw):
            raise ValueError(f"{label}: 表 {rec[:4]!r} 越界 off={off} len={ln} 文件={len(raw)}")
    return ("CFF/OTTO" if magic == b"OTTO" else "TrueType"), n


def check_text(raw, name):
    """TextAsset 自证：合法 UTF-8；名字带不带的 JSON 类要能 json.loads。"""
    txt = raw.decode("utf-8")           # 抛 UnicodeDecodeError 就说明不是 utf-8
    info = {"len": len(raw), "bom": raw[:3] == b"\xef\xbb\xbf"}
    if len(raw) and raw.lstrip()[:1] in (b"{", b"["):
        json.loads(txt)                 # 抛就是坏
        info["json"] = "OK"
    return info


def fonttools_probe(path, raw):
    """用真消费方（fontTools）读一遍。TTF 用 TTFont，TTC 用 TTCollection。"""
    out = {}
    if raw[:4] == b"ttcf":
        from fontTools.ttLib.ttCollection import TTCollection
        coll = TTCollection(path, lazy=True)
        names = []
        for f in coll.fonts:
            ps = ""
            for rec in f["name"].names:
                if rec.nameID == 6:
                    try:
                        ps = rec.toUnicode()
                        break
                    except Exception:
                        pass
            names.append(ps or f"<font {len(names)}>")
        out["ttcollection"] = names
        out["numFonts"] = len(coll.fonts)
        coll.close()
    else:
        from fontTools.ttLib import TTFont
        f = TTFont(path, lazy=True)
        out["numGlyphs"] = f["maxp"].numGlyphs
        out["bestCmap"] = len(f.getBestCmap())
        out["tables"] = sorted(f.keys())[:12]
        f.close()
    return out


def corroborate(rel, raw, tag):
    """第二来源对账：`d:/2/解包整理/10_字体/` 里那份（另一棵树、另一次解包）。"""
    if not tag:
        return None
    fn = os.path.basename(rel)
    p = os.path.join(DECOMP_ROOT, tag, fn)
    if not os.path.exists(p):
        return {"第二来源": f"{p} 不存在"}
    other = open(p, "rb").read()
    if len(other) == len(raw) and md5(other) == md5(raw):
        return {"第二来源": f"{p} **逐字节相同**（{len(other)} B）"}
    return {
        "第二来源": f"{p} 存在但**不同**（{len(other)} B / md5 {md5(other)[:12]}"
                    f" vs 本件 {len(raw)} B / md5 {md5(raw)[:12]}）",
        "差异原因": ("`10_字体/` 那份是 `restore_fonts.py` 把 TTC 拆出的 **SC 子字体**"
                     "（见该脚本文件头），本件还原的是 **原始 m_FontData（TTC 集合）** —— "
                     "两者本来就不该相同。"),
    }


# --------------------------------------------------------------------------- #
def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--apply", action="store_true")
    ap.add_argument("--verify-only", action="store_true")
    ap.add_argument("--json", default=None)
    a = ap.parse_args()

    import UnityPy

    print("UnityPy", UnityPy.__version__, flush=True)
    env_by_path = {}
    for c in (RES_ASSETS, FONTS_BUNDLE):
        t0 = time.time()
        env_by_path[c] = UnityPy.load(c)
        print(f"# 载入 {os.path.basename(c)} ({time.time()-t0:.1f}s)", flush=True)

    report = []
    for rel, container, tn, name, exp_pid, tag in TARGETS:
        dst = os.path.join(EXPORT_ROOT, rel.replace("/", os.sep))
        rec = {"rel": rel, "container": os.path.basename(container), "type": tn,
               "name": name, "expect_pid": exp_pid, "path": dst}
        before = os.path.getsize(dst) if os.path.exists(dst) else None
        rec["before_bytes"] = before
        try:
            cab, pid, raw = pull(env_by_path, container, tn, name)
            rec["cab"] = cab
            rec["path_id"] = pid
            rec["pid_matches_record"] = (pid == exp_pid)
            rec["src_bytes"] = len(raw)
            rec["head8"] = raw[:8].hex(" ")
            rec["md5"] = md5(raw)
            if tn == "Font":
                kind, det = check_sfnt(raw)
                rec["sfnt"] = kind
                rec["sfnt_detail"] = det
            else:
                rec["text"] = check_text(raw, name)
            rec["corroborate"] = corroborate(rel, raw, tag)
        except Exception as e:
            rec["error"] = f"{type(e).__name__}: {e}"
            report.append(rec)
            print(f"🔴 {rel}: {rec['error']}", flush=True)
            continue

        if a.verify_only:
            report.append(rec)
            print(f"· {rel}: 源 {rec['src_bytes']} B / 盘上 {before} B", flush=True)
            continue

        if before not in (None, 0) and before == len(raw):
            rec["action"] = "跳过（盘上已非空且长度相符）"
            report.append(rec)
            print(f"· {rel}: {rec['action']}", flush=True)
            continue
        if before not in (None, 0):
            rec["action"] = f"🔴 跳过（盘上 {before} B 非空，纪律：绝不覆盖非空内容）"
            report.append(rec)
            print(f"🔴 {rel}: {rec['action']}", flush=True)
            continue

        if not a.apply:
            rec["action"] = "dry-run（未写）"
            report.append(rec)
            print(f"· {rel}: 源 {rec['src_bytes']} B / 盘上 {before} B → 待写", flush=True)
            continue

        # ---- 备份 ----
        bkp = os.path.join(BACKUP_ROOT, "新解包资源", "assets_full", rel.replace("/", os.sep))
        os.makedirs(os.path.dirname(bkp), exist_ok=True)
        if os.path.exists(dst) and not os.path.exists(bkp):
            shutil.copy2(dst, bkp)
        rec["backup"] = bkp if os.path.exists(bkp) else None

        # ---- 写（内容早已在变量里）----
        with open(dst, "wb") as f:
            f.write(raw)
        # ---- 当场回读 ----
        back = open(dst, "rb").read()
        rec["after_bytes"] = len(back)
        rec["roundtrip_ok"] = (back == raw)
        rec["after_head8"] = back[:8].hex(" ")
        rec["after_md5"] = md5(back)
        # ---- 交给真消费方读一遍 ----
        try:
            rec["fonttools"] = fonttools_probe(dst, back) if tn == "Font" else None
        except Exception as e:
            rec["fonttools"] = f"🔴 {type(e).__name__}: {e}"
        rec["action"] = "已写 + 已回读"
        report.append(rec)
        print(f"✅ {rel}: {before} B → {rec['after_bytes']} B  head8={rec['after_head8']}  "
              f"roundtrip={rec['roundtrip_ok']}", flush=True)

    if a.json:
        with open(a.json, "w", encoding="utf-8") as f:
            json.dump(report, f, ensure_ascii=False, indent=1)
        print(f"# 报告 -> {a.json}", flush=True)
    return 0 if all("error" not in r for r in report) else 1


if __name__ == "__main__":
    sys.exit(main())
