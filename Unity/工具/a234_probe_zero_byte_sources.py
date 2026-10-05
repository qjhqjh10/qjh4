#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""A234 probe (read-only): locate the source objects for the 9 zero-byte exports.

No writes anywhere.  Prints, for each of the 9 targets, which source container
(`resources.assets` / `fonts_assets_all.bundle`) holds the object, which inner
CAB it lives in, its path_id, and the *raw* shape of the payload field
(`m_FontData` / `m_Script`).

Key API note (learned the hard way): for a **bundle**, `env.files` maps the
container path -> `BundleFile`; the per-CAB `SerializedFile`s live in
`env.file.files`.  For a standalone `.assets`, `env.file` IS the SerializedFile.
`iter_sfs()` below normalises both shapes.
"""
import os, sys, io, json
import UnityPy

GAME = r"d:/2/Warhammer 40k Warpforge/Warpforge_Data"
RES = os.path.join(GAME, "resources.assets")
FONTS_BUNDLE = os.path.join(GAME, "StreamingAssets", "aa", "StandaloneWindows64",
                            "fonts_assets_all.bundle")

TARGETS_RES = [
    ("Font", "LiberationSans"),
    ("Font", "NotoSerifCJK-Regular"),
    ("Font", "PerfectDOSVGA437"),
    ("TextAsset", "BillingMode"),
    ("TextAsset", "LineBreaking Following Characters"),
    ("TextAsset", "LineBreaking Leading Characters"),
    ("TextAsset", "PerformanceTestRunInfo"),
    ("TextAsset", "PerformanceTestRunSettings"),
]
TARGETS_FONTS = [("Font", "NotoSerifCJK-Regular")]


def iter_sfs(env):
    """Yield (cab_label, SerializedFile) for every inner CAB of a container."""
    for name, v in (getattr(env.file, "files", {}) or {}).items():
        if hasattr(v, "objects"):
            yield name, v
    if not getattr(env.file, "files", None) and hasattr(env.file, "objects"):
        yield "<standalone>", env.file


def dump(env, want, label):
    print(f"\n########## {label} ##########")
    print(f"  outer file type = {type(env.file).__name__}")
    n = 0
    for cab, sf in iter_sfs(env):
        nobj = len(getattr(sf, "objects", {}) or {})
        print(f"  -- CAB {cab!r}: {nobj} objects")
        n += 1
        for pid, obj in (getattr(sf, "objects", {}) or {}).items():
            tn = obj.type.name
            if tn not in ("Font", "TextAsset"):
                continue
            try:
                d = obj.read()
                nm = getattr(d, "m_Name", None)
            except Exception as e:
                nm = f"<read err {e}>"
            if (tn, nm) not in want:
                continue
            print(f"     HIT {tn} pid={pid} (obj.path_id={obj.path_id}) name={nm!r}")
            if tn == "Font":
                fd = getattr(d, "m_FontData", None)
                print(f"        m_FontData type={type(fd).__name__} "
                      f"len={len(fd) if fd is not None else None}")
                if isinstance(fd, (bytes, bytearray)):
                    print(f"        head8 = {bytes(fd[:8]).hex(' ')}")
                elif isinstance(fd, list):
                    head = bytes(x & 0xFF for x in fd[:8])
                    print(f"        head8(as ints) = {head.hex(' ')}")
                    print(f"        int range = [{min(fd)}, {max(fd)}] "
                          f"all_int={all(isinstance(x, int) for x in fd)}")
                print(f"        m_FontSize={getattr(d,'m_FontSize',None)} "
                      f"m_Ascent={getattr(d,'m_Ascent',None)} "
                      f"m_FontNames={getattr(d,'m_FontNames',None)!r}")
            else:
                sc = getattr(d, "m_Script", None)
                print(f"        m_Script type={type(sc).__name__} "
                      f"len={len(sc) if sc is not None else None}")
                if isinstance(sc, str):
                    print(f"        repr[:120] = {sc[:120]!r}")
                    try:
                        enc = sc.encode("utf-8")
                        print(f"        utf-8 bytes head8 = {enc[:8].hex(' ')} "
                              f"len={len(enc)}")
                    except Exception as e:
                        print(f"        utf-8 encode failed: {e}")
                elif isinstance(sc, (bytes, bytearray)):
                    print(f"        head8 = {bytes(sc[:8]).hex(' ')}")
                print(f"        m_PathName={getattr(d,'m_PathName',None)!r}")
    if n == 0:
        print("  (no CAB with objects found)")

    # inventory every Font/TextAsset in the container, so we can tell whether
    # the target set is complete
    print("  -- all Font/TextAsset in this container:")
    for cab, sf in iter_sfs(env):
        for pid, obj in (getattr(sf, "objects", {}) or {}).items():
            if obj.type.name in ("Font", "TextAsset"):
                try:
                    nm = getattr(obj.read(), "m_Name", None)
                except Exception:
                    nm = "?"
                print(f"       {obj.type.name:10s} pid={pid:>22d} name={nm!r}")


def main():
    print("UnityPy", UnityPy.__version__)
    env = UnityPy.load(RES)
    dump(env, set(TARGETS_RES), f"resources.assets  ({RES})")
    env2 = UnityPy.load(FONTS_BUNDLE)
    dump(env2, set(TARGETS_FONTS), f"fonts_assets_all.bundle  ({FONTS_BUNDLE})")


if __name__ == "__main__":
    main()
