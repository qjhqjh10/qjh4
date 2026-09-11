# -*- coding: utf-8 -*-
"""restore_fonts.py — 从 `10_字体/**/Font_*.json` 的 `m_FontData` 还原字体文件。

背景（审计报告 A4）:
    `10_字体/` 下 4 个 .ttf 全部是 0 字节（fix_exports.py 导出时 m_FontData 没写进去），
    但同名 JSON 里 `m_FontData` 是完整的。本脚本无损还原。

    NotoSerifCJK-Regular 的 m_FontData 是 **TTC 集合**（magic `ttcf`，内含 JP/KR/SC/TC 4 个字体），
    Unity 不认 TTC，因此默认用 fontTools 拆出**简体中文 (SC)** 子字体写成 .ttf
    （游戏是中文界面；如需其它语言用 --all-subfonts 或 --lang 指定）。

用法:
    py312/python.exe restore_fonts.py                  # dry-run: 只报告会写什么
    py312/python.exe restore_fonts.py --apply          # 真正落盘
    py312/python.exe restore_fonts.py --apply --all-subfonts   # TTC 全部子字体都拆出来
    py312/python.exe restore_fonts.py --apply --raw-ttc        # 额外保留原始 .ttc 集合
    py312/python.exe restore_fonts.py --apply --lang KR        # 指定拆哪个语言 (JP/KR/SC/TC)
    py312/python.exe restore_fonts.py --verify         # 校验已落盘文件 vs JSON

纪律: 只写 `10_字体/` 下的字体文件；不删除、不改动任何 JSON。
      目标文件已存在且内容不同（非 0 字节）时，改写 `<name>__restored.<ext>` 并告警，绝不覆盖。
"""
import os
import sys
import json
import hashlib

sys.stdout.reconfigure(encoding="utf-8")

ROOT = r"d:/2/解包整理/10_字体"

MAGIC = {
    b"\x00\x01\x00\x00": ("ttf", "TrueType"),
    b"true": ("ttf", "TrueType(Apple)"),
    b"OTTO": ("otf", "CFF/OpenType"),
    b"ttcf": ("ttc", "TrueType Collection"),
}


def magic_of(raw):
    return MAGIC.get(raw[:4], ("bin", "未知(%r)" % raw[:4]))


def md5(b):
    return hashlib.md5(b).hexdigest()


def font_fingerprint(path):
    """字体语义指纹: glyph 数 + cmap + glyph 顺序。
    fontTools 重新 save() 会重算 head.checkSumAdjustment，逐字节比对会有几字节噪声，
    所以 TTC 拆出的文件用语义指纹比对。"""
    from fontTools.ttLib import TTFont
    f = TTFont(path, lazy=True)
    h = hashlib.sha1()
    h.update(str(f["maxp"].numGlyphs).encode())
    h.update(repr(sorted(f.getBestCmap().items())).encode())
    h.update("\x00".join(f.getGlyphOrder()).encode())
    f.close()
    return h.hexdigest()


def load_fontdata(path):
    """返回 (raw_bytes, m_Name) 或 (None, 原因)。"""
    try:
        with open(path, encoding="utf-8") as f:
            d = json.load(f)
    except Exception as e:
        return None, "JSON 解析失败: %s" % e
    if not isinstance(d, dict):
        return None, "不是对象转储"
    fd = d.get("m_FontData")
    if not fd:
        return None, "无 m_FontData"
    if isinstance(fd, str):
        try:
            raw = bytes.fromhex(fd)
        except ValueError:
            raw = fd.encode("latin-1")
    else:
        raw = bytes(fd)
    if not raw:
        return None, "m_FontData 为空"
    return raw, d.get("m_Name") or os.path.basename(path)[:-5]


def ttc_fonts(raw, tmpdir):
    """用 fontTools 打开 TTC 字节流，返回 [(ps_name, family, subfamily)]（顺序=集合内顺序）。"""
    os.makedirs(tmpdir, exist_ok=True)
    tmp = os.path.join(tmpdir, "_restore_fonts_probe.ttc")
    with open(tmp, "wb") as f:
        f.write(raw)
    from fontTools.ttLib.ttCollection import TTCollection
    coll = TTCollection(tmp, lazy=True)
    out = []
    for f in coll.fonts:
        nm = {}
        for rec in f["name"].names:
            try:
                nm[(rec.nameID, rec.platformID)] = rec.toUnicode()
            except Exception:
                pass

        def pick(nid):
            for (n, pl), v in nm.items():
                if n == nid and pl == 3:
                    return v
            for (n, pl), v in nm.items():
                if n == nid:
                    return v
            return ""

        out.append((pick(6) or pick(4), pick(1), pick(2)))
    return out, coll


def pick_lang_index(fonts, lang):
    """在 TTC 子字体列表里挑目标语言 (fonts = [(ps_name, family, subfamily)])。
    ps_name 形如 NotoSerifCJKsc-Regular / …jp- / …kr- / …tc-。"""
    idx = next((i for i, f in enumerate(fonts)
                if f[0].upper().endswith(lang) or lang in f[0].upper()), None)
    return idx


def extract_subfont(coll, idx, dst):
    """把 TTC 里第 idx 个子字体存成独立 ttf。"""
    font = coll.fonts[idx]
    font.flavor = None
    font.save(dst)


def safe_write(dst, data, apply_):
    """写文件；已存在且内容不同(非0字节)时改写 __restored 变体。返回 (实际路径, 状态)。"""
    if os.path.exists(dst):
        old = open(dst, "rb").read()
        if old == data:
            return dst, "已一致(跳过)"
        if len(old) > 0:
            base, ext = os.path.splitext(dst)
            dst2 = base + "__restored" + ext
            if apply_:
                with open(dst2, "wb") as f:
                    f.write(data)
            return dst2, "原文件非空且不同 -> 写 %s" % os.path.basename(dst2)
        note = "覆盖 0 字节占位"
    else:
        note = "新建"
    if apply_:
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        with open(dst, "wb") as f:
            f.write(data)
    return dst, note


def main():
    apply_ = "--apply" in sys.argv
    verify_ = "--verify" in sys.argv
    all_sub = "--all-subfonts" in sys.argv
    raw_ttc = "--raw-ttc" in sys.argv
    lang = "SC"
    if "--lang" in sys.argv:
        lang = sys.argv[sys.argv.index("--lang") + 1].upper()
    tmpdir = r"d:/2/Warpforge_tools/tmp/restore_fonts"

    print("=" * 78)
    print("restore_fonts.py  %s   根目录: %s" % ("[APPLY]" if apply_ else "[dry-run]", ROOT))
    print("=" * 78)

    targets = []
    for dp, dn, fn in os.walk(ROOT):
        for f in sorted(fn):
            if f.lower().endswith(".json"):
                targets.append(os.path.join(dp, f))
    if not targets:
        print("!! 没有找到 Font JSON")
        return 1

    n_written = n_skip = 0
    problems = []
    for p in sorted(targets):
        raw, name = load_fontdata(p)
        rel = os.path.relpath(p, ROOT).replace("\\", "/")
        if raw is None:
            print("[跳过] %-58s %s" % (rel, name))
            continue
        kind, kindname = magic_of(raw)
        print("-" * 78)
        print("源  : %s" % rel)
        print("      m_Name=%-24s %9d 字节  md5=%s  魔数=%s(%s)"
              % (name, len(raw), md5(raw), raw[:4], kindname))

        if verify_:
            dst = os.path.join(os.path.dirname(p), name + ".ttf")
            if not os.path.exists(dst):
                print("校验: 目标不存在 %s" % os.path.relpath(dst, ROOT))
                problems.append((rel, "缺失"))
                continue
            cur = open(dst, "rb").read()
            if kind == "ttc":
                # 落盘的是拆出的子字体，用「重新拆一次 + 语义指纹」比对
                try:
                    fonts, coll = ttc_fonts(raw, tmpdir)
                    tmp_out = os.path.join(tmpdir, "_verify_sub.ttf")
                    i = pick_lang_index(fonts, lang)
                    if i is None:
                        raise RuntimeError("集合内找不到语言 %s" % lang)
                    extract_subfont(coll, i, tmp_out)
                    fa, fb = font_fingerprint(tmp_out), font_fingerprint(dst)
                    ok = fa == fb
                    print("校验: %s  拆出子字体 %s  (%d 字节, 源 TTC %d 字节; 指纹 %s vs %s)"
                          % ("一致" if ok else "不一致", os.path.relpath(dst, ROOT), len(cur), len(raw),
                             fa[:10], fb[:10]))
                except Exception as e:
                    ok = len(cur) > 0
                    print("校验: 无法重拆 TTC (%s)，只查非空: %s %d 字节"
                          % (e, os.path.relpath(dst, ROOT), len(cur)))
            else:
                ok = cur == raw
                print("校验: %s  %s  (%d 字节)" % ("一致" if ok else "不一致", os.path.relpath(dst, ROOT), len(cur)))
            if not ok:
                problems.append((rel, os.path.relpath(dst, ROOT)))
            continue

        if kind == "ttc":
            try:
                fonts, coll = ttc_fonts(raw, tmpdir)
            except Exception as e:
                print("      !! fontTools 打开 TTC 失败: %s" % e)
                print("      -> 退化为整包写出 %s.ttc" % name)
                dst, st = safe_write(os.path.join(os.path.dirname(p), name + ".ttc"), raw, apply_)
                print("      %s %s" % (st, os.path.relpath(dst, ROOT)))
                problems.append((rel, "TTC 未拆分: %s" % e))
                continue
            print("      TTC 内含 %d 个字体: %s" % (len(fonts), ", ".join(f[0] for f in fonts)))
            picks = list(range(len(fonts))) if all_sub else []
            if not all_sub:
                idx = pick_lang_index(fonts, lang)
                if idx is None:
                    idx = 0
                    print("      !! 集合内找不到语言 %s，退用第 0 个 (%s)" % (lang, fonts[0][0]))
                picks = [idx]
            for i in picks:
                ps, fam, sub = fonts[i]
                if all_sub:
                    out_name = "%s.%s.ttf" % (name, ps)
                else:
                    out_name = "%s.ttf" % name
                dst = os.path.join(os.path.dirname(p), out_name)
                # 拆出的子字体先落到临时文件，再走 safe_write（保留 0 字节占位检测/不覆盖逻辑）
                tmp_out = os.path.join(tmpdir, "_sub.ttf")
                extract_subfont(coll, i, tmp_out)
                # 幂等: fontTools 重存会重算 head 时间戳/校验和(逐字节差 6 B)，故先做语义比对
                if os.path.exists(dst) and os.path.getsize(dst) > 0:
                    try:
                        if font_fingerprint(dst) == font_fingerprint(tmp_out):
                            print("      [%d] %-24s %-28s -> %s (已一致(语义指纹), 跳过)"
                                  % (i, ps, fam, os.path.relpath(dst, ROOT)))
                            continue
                    except Exception:
                        pass
                sub_raw = open(tmp_out, "rb").read()
                dst, st = safe_write(dst, sub_raw, apply_)
                print("      [%d] %-24s %-28s %9d 字节 -> %s (%s)"
                      % (i, ps, fam, len(sub_raw), os.path.relpath(dst, ROOT), st))
                n_written += 1 if st != "已一致(跳过)" else 0
            if raw_ttc:
                dst, st = safe_write(os.path.join(os.path.dirname(p), name + ".ttc"), raw, apply_)
                print("      [raw] 原始 TTC -> %s (%s)" % (os.path.relpath(dst, ROOT), st))
        else:
            ext = ".ttf" if kind in ("ttf", "otf") else "." + kind
            dst, st = safe_write(os.path.join(os.path.dirname(p), name + ext), raw, apply_)
            print("      -> %s (%s)" % (os.path.relpath(dst, ROOT), st))
            n_written += 1 if st != "已一致(跳过)" else 0

    print("=" * 78)
    if verify_:
        print("校验结束: %d 个源, %d 个问题" % (len(targets), len(problems)))
        for a, b in problems:
            print("   !! %s -> %s" % (a, b))
    else:
        print("结束: 写出 %d 个字体文件 (dry-run=%s)" % (n_written, not apply_))
        if not apply_:
            print("加 --apply 真正落盘。")
    return 0


if __name__ == "__main__":
    sys.exit(main())
