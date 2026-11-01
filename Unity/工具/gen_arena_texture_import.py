#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""gen_arena_texture_import.py —— 把**战场贴图**的原版导入真值落成一张旁挂表。

**为什么要它**（而不是在 C# 里猜 / 一刀切）：战场贴图的 `.meta` 不是我们从原版抄来的，
是 Unity 按**默认值**导入出来的。`ArenaBuilder.ApplyTextureImportSettings` 原来只设
`textureType / mipmapEnabled / npotScale / alphaIsTransparency / maxTextureSize`
并**把 `mipmapEnabled` 写死成 `false`**（判据只取了 `Battle Arena 1 Floor` 一张 —— 它 `m_MipCount = 1`
确实没有 mip），`sRGBTexture` **压根没设**（吃 Unity 新贴图默认 `1`）。

实测（`资料/完全复刻/判据/B112W_B192_战场贴图树普查.md` §五·2 / §六·2）：
  · **9 个名字 / 25 份** 原版 `m_ColorSpace: 0`（线性），我们 `.meta` 是 `sRGBTexture: 1`；
  · **77 个名字 / 143 份** 原版 `m_MipCount > 1`，我们 `enableMipMap: 0`。

🔴 **2026-11-01（B-201 / B-202）扩表**：
  · 加 `wrapU / wrapV / filterMode / aniso` 四列（同 `vfx_texture_mips.tsv` 的口径；
    `ArenaBuilder.ApplyOriginalImport` 之前只接了 mip / sRGB）。实测战场侧
    `wrapU/wrapV` 不一致 **36 份**、`aniso` **5 份**、`filterMode` **0 份**（`报告/B112W_B198_B199.md` §七·2）。
  · **范围扩到 `arenas/*/Sprites/**` 与顶层 `Textures/LUT/**`**（B-202：这两处原来没有任何导入设置管理）。
    `Sprites/` 里唯一一个**不是独立 `Texture2D`** 的名字是 `Atack_Icon BW` —— 它是
    `atlasindividual_assets_battleatlasui.bundle` 里 `BattleAtlasUI` 图集的一张 **Sprite**
    （`m_RD.texture.m_PathID = 0`、`m_SpriteAtlas` 非 0）⇒ 走**图集兜底**：取**同一个 bundle 里唯一的
    `Texture2D`**（`sactx-0-2048x2048-BC7-BattleAtlasUI-06d3a381`）的采样设置。⛔ 这条兜底**只在
    「找不到独立 Texture2D + 该名字确实是 Sprite + 那个 bundle 只有一张 Texture2D」时**才走，否则 rc=2。

**判据来源**：`UnityPy` 打开 `D:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/*.bundle`，
逐张读 `Texture2D.m_Width / m_Height / m_MipCount / m_ColorSpace / m_TextureSettings.*`。
⚠️ 走「旁挂表 + 生成脚本」是本工程既定做法（同 `gen_vfx_texture_mips.py`）：bundle 里**非 addressable**
的资产 `AssetBundle.LoadAllAssets` 取不全，不能指望运行时枚举。

产出：`数据/游戏数据/arena_texture_import.tsv`
      列 = `name  width  height  mipCount  colorSpace  bundle  wrapU  wrapV  filterMode  aniso`
           （按 name 排序 · LF · 无 BOM；前 6 列与旧表逐字节一致，后 4 列是新增）
      消费侧 = `ArenaBuilder.ApplyOriginalImport`（`Assets/WarpforgeArena1/Editor/ArenaBuilder.cs` 类尾）。
      ⚠️ 老表只有 6 列时消费侧把新增四格当 **-1（未知）**⇒ **不动**（不许拿「未知」覆盖现值）。

**范围 = 工程里实际存在的战场贴图 / 精灵 / LUT**：
  · `Assets/WarpforgeArena1/arenas/*/Textures/**/*.png`（现读 116 个名字 / 226 份）
  · `Assets/WarpforgeArena1/arenas/*/Sprites/**/*.png`（现读 5 个名字 / 18 份）
  · `Assets/WarpforgeArena1/Textures/LUT/**/*.png`（现读 7 个名字 / 7 份）
  ⛔ 不要改成「把 2984 张全写进来」—— 那张是 `vfx_texture_mips.tsv` 的活。

🔴 **对不上就大声失败**（rc=2），⛔ 不许静默少一行：
  · 某个名字在所有 bundle 里都找不到 ⇒ 说明源包里没有它（或被改名）⇒ 消费侧会退回旧口径，
    必须让人看见；
  · 同一个名字在多个包里 `(mipCount, colorSpace, w, h)` 不一致 ⇒ 「该听谁的」本身是问题，不许挑一个；
  · 同一个名字在多个包里 `(wrapU, wrapV, filterMode, aniso)` 不一致 ⇒ 同上（与 `vfx_texture_mips.tsv` 一致的口径）。

用法：
  "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_arena_texture_import.py
  "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_arena_texture_import.py --check
  -h / --help  只打印本用法并退出（rc=0），不执行任何真活。
"""
import glob
import os
import sys

try:  # Windows 控制台默认 GBK，中文/emoji 会乱码或抛 UnicodeEncodeError
    sys.stdout.reconfigure(encoding="utf-8")
    sys.stderr.reconfigure(encoding="utf-8")
except Exception:
    pass

if any(a in ("-h", "--help") for a in sys.argv[1:]):
    print(__doc__)
    sys.exit(0)

import UnityPy  # noqa: E402  （--help 要在 import 之前退出：没装 UnityPy 也能看用法）

BUNDLE_DIR = (r"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa"
              r"\StandaloneWindows64")
TEX_GLOB = r"d:/4/Unity/MyGame/Assets/WarpforgeArena1/arenas/*/Textures/**/*.png"
SPR_GLOB = r"d:/4/Unity/MyGame/Assets/WarpforgeArena1/arenas/*/Sprites/**/*.png"
LUT_GLOB = r"d:/4/Unity/MyGame/Assets/WarpforgeArena1/Textures/LUT/**/*.png"
OUT = r"d:/4/Unity/数据/游戏数据/arena_texture_import.tsv"
HEADER = "name\twidth\theight\tmipCount\tcolorSpace\tbundle\twrapU\twrapV\tfilterMode\taniso"


def wanted_names():
    """工程里实际存在的战场贴图 / 精灵 / LUT 名（文件名主干），去重排序。"""
    names = set()
    for pat in (TEX_GLOB, SPR_GLOB, LUT_GLOB):
        for p in glob.glob(pat, recursive=True):
            names.add(os.path.splitext(os.path.basename(p))[0])
    return sorted(names)


def _wrap(d, field):
    """读 `Texture2D.m_TextureSettings.<field>`；读不到返回 -1（**未知**，调用方不许拿它覆盖）。"""
    try:
        st = getattr(d, "m_TextureSettings", None)
        if st is None:
            return -1
        v = getattr(st, field, None)
        return int(v) if v is not None else -1
    except Exception:
        return -1


def scan_bundles(want):
    """→ ({name: rec}, conflicts, missing_sprite_candidates)

    rec = dict(w,h,mip,cs,bundle,wu,wv,fm,an)；同名字段冲突 → 报错。
    `missing_sprite_candidates` = {name: [(bundle_file, atlas_pathid), ...]}：
    名字**不是 Texture2D**、但在某个 bundle 里是 **Sprite**（为 `Atack_Icon BW` 这类图集切片准备的兜底线索）。
    """
    recs, conflicts, setting_conflicts = {}, [], []
    sprite_hits = {}
    files = sorted(glob.glob(os.path.join(BUNDLE_DIR, "*.bundle")))
    if not files:
        sys.stderr.write("🔴 找不到源包：%s\n" % BUNDLE_DIR)
        sys.exit(2)
    for f in files:
        bname = os.path.basename(f)
        try:
            env = UnityPy.load(f)
        except Exception as e:
            sys.stderr.write("⚠️ 打不开 %s：%s\n" % (bname, e))
            continue
        for obj in env.objects:
            if obj.type.name == "Texture2D":
                try:
                    d = obj.read()
                except Exception:
                    continue
                name = getattr(d, "m_Name", None)
                if not name:
                    continue
                rec = {
                    "w": int(getattr(d, "m_Width", 0) or 0),
                    "h": int(getattr(d, "m_Height", 0) or 0),
                    "mip": int(getattr(d, "m_MipCount", 0) or 0),
                    "cs": int(getattr(d, "m_ColorSpace", -1)),
                    "bundle": bname,
                    "wu": _wrap(d, "m_WrapU"),
                    "wv": _wrap(d, "m_WrapV"),
                    "fm": _wrap(d, "m_FilterMode"),
                    "an": _wrap(d, "m_Aniso"),
                }
                old = recs.get(name)
                if old is None:
                    recs[name] = rec
                elif (old["w"], old["h"], old["mip"], old["cs"]) != \
                     (rec["w"], rec["h"], rec["mip"], rec["cs"]):
                    conflicts.append((name, old, rec))
                elif (old["wu"], old["wv"], old["fm"], old["an"]) != \
                        (rec["wu"], rec["wv"], rec["fm"], rec["an"]):
                    setting_conflicts.append((name, old, rec))
            elif obj.type.name == "Sprite":
                # 只对「要的名字里、且不是独立 Texture2D」的那些读内容（其余用 peek_name 跳过）
                try:
                    nm = obj.peek_name()
                except Exception:
                    continue
                if nm not in want:
                    continue
                try:
                    d = obj.read()
                except Exception:
                    continue
                atlas = getattr(d, "m_SpriteAtlas", None)
                pid = getattr(atlas, "m_PathID", 0) if atlas is not None else 0
                sprite_hits.setdefault(nm, []).append((bname, int(pid or 0)))
    return recs, conflicts, setting_conflicts, sprite_hits


def atlas_fallback(name, hits):
    """`name` 是 Sprite（不是独立 Texture2D）⇒ 用它所在**图集 bundle 里唯一的 Texture2D** 的采样设置。

    🔴 只在三条同时成立时才走（⛔ 不许猜）：
      ① 该名字确实是一个 **Sprite**，且 `m_SpriteAtlas.m_PathID != 0`（= 它进图集了，没有独立贴图）；
      ② 它只在**一个** bundle 里出现（跨包出现 ⇒ 「听谁的」是个问题）；
      ③ 那个 bundle 里**恰好只有一张** Texture2D（多于一张就无法确定是哪张图集）。
    否则返回 (None, 原因)。
    """
    if len(hits) != 1:
        return None, "在 %d 个 bundle 里都是 Sprite，无法确定图集" % len(hits)
    bname, atlas_pid = hits[0]
    if atlas_pid == 0:
        return None, "Sprite 的 m_SpriteAtlas 为 0（不是图集切片，但也不是 Texture2D）"
    f = os.path.join(BUNDLE_DIR, bname)
    try:
        env = UnityPy.load(f)
    except Exception as e:
        return None, "图集 bundle 打不开：%s" % e
    texs = []
    for obj in env.objects:
        if obj.type.name != "Texture2D":
            continue
        try:
            d = obj.read()
        except Exception:
            continue
        texs.append(d)
    if len(texs) != 1:
        return None, "图集 bundle 里有 %d 张 Texture2D（需要恰好 1 张才能确定）" % len(texs)
    d = texs[0]
    rec = {
        "w": int(getattr(d, "m_Width", 0) or 0),
        "h": int(getattr(d, "m_Height", 0) or 0),
        "mip": int(getattr(d, "m_MipCount", 0) or 0),
        "cs": int(getattr(d, "m_ColorSpace", -1)),
        "bundle": bname,
        "wu": _wrap(d, "m_WrapU"),
        "wv": _wrap(d, "m_WrapV"),
        "fm": _wrap(d, "m_FilterMode"),
        "an": _wrap(d, "m_Aniso"),
    }
    return rec, "图集 %s 的唯一贴图 `%s`" % (bname, getattr(d, "m_Name", "?"))


def build_lines():
    names = wanted_names()
    if not names:
        sys.stderr.write("🔴 一个战场贴图都没找到（glob = %s / %s / %s）\n"
                         % (TEX_GLOB, SPR_GLOB, LUT_GLOB))
        sys.exit(2)
    want = set(names)
    recs, all_conflicts, all_setting_conflicts, sprite_hits = scan_bundles(want)
    # ⚠️ 只对**本表范围内**的名字报冲突：`ring2` 这类同名不同尺寸的贴图在全库里不止一处，
    #   但它不在任何战场的 `Textures/` 下 ⇒ 与本表无关，报出来只会制造噪声。
    conflicts = [c for c in all_conflicts if c[0] in want]
    setting_conflicts = [c for c in all_setting_conflicts if c[0] in want]
    if conflicts:
        sys.stderr.write("🔴 同名贴图在多个包里真值不一致（%d 条）—— 先裁定再落表：\n"
                         % len(conflicts))
        for n, a, b in conflicts:
            sys.stderr.write("   %s: %s vs %s\n" % (n, a, b))
        sys.exit(2)
    if setting_conflicts:
        sys.stderr.write("🔴 同名贴图的 wrap/filter/aniso 在多个包里不一致（%d 条）—— 先裁定再落表：\n"
                         % len(setting_conflicts))
        for n, a, b in setting_conflicts:
            sys.stderr.write("   %s: wrap(%d,%d) f%d a%d  vs  wrap(%d,%d) f%d a%d\n"
                             % (n, a["wu"], a["wv"], a["fm"], a["an"],
                                b["wu"], b["wv"], b["fm"], b["an"]))
        sys.exit(2)

    missing = [n for n in names if n not in recs]
    derived = []
    hard_missing = []
    for n in missing:
        rec, why = atlas_fallback(n, sprite_hits.get(n, []))
        if rec is None:
            hard_missing.append((n, why))
        else:
            recs[n] = rec
            derived.append((n, why))
    if hard_missing:
        sys.stderr.write("🔴 这些名字在所有源包里都找不到（%d 个）：\n" % len(hard_missing))
        for n, why in hard_missing:
            sys.stderr.write("   %s —— %s\n" % (n, why))
        sys.exit(2)

    lines = [HEADER]
    for n in names:
        r = recs[n]
        lines.append("%s\t%d\t%d\t%d\t%d\t%s\t%d\t%d\t%d\t%d"
                     % (n, r["w"], r["h"], r["mip"], r["cs"], r["bundle"],
                        r["wu"], r["wv"], r["fm"], r["an"]))

    # 诊断（不进文件，只印出来）：mip 数不是 1 也不是满链的，说明 Unity 用 mipmapEnabled 表达不了它的精确层数
    odd = []
    for n in names:
        r = recs[n]
        m = max(r["w"], r["h"])
        full = 1 if m <= 1 else m.bit_length()  # = floor(log2(m)) + 1 对 2 的幂成立
        if r["mip"] not in (1, full):
            odd.append((n, r["w"], r["h"], r["mip"], full))
    if odd:
        sys.stderr.write("⚠️ %d 个名字的 mipCount 既不是 1 也不是满链（Unity 只能做「开/关」）：\n"
                         % len(odd))
        for o in odd:
            sys.stderr.write("   %s %dx%d mip=%d full=%d\n" % o)
    if derived:
        sys.stderr.write("ℹ️ %d 个名字不是独立 Texture2D，走**图集兜底**（取图集那张贴图的采样设置）：\n"
                         % len(derived))
        for n, why in derived:
            sys.stderr.write("   %s ← %s\n" % (n, why))
    return lines, names, recs


def main():
    check = "--check" in sys.argv[1:]
    lines, names, recs = build_lines()
    text = "\n".join(lines) + "\n"

    if check:
        try:
            old = open(OUT, "rb").read().decode("utf-8")
        except OSError:
            sys.stderr.write("🔴 缺产物 %s —— 先跑一次不带 --check 的\n" % OUT)
            sys.exit(1)
        if old != text:
            sys.stderr.write("🔴 %s 与源包真值不一致（内容过期）\n" % OUT)
            sys.exit(1)
        print("✅ %s 与源包真值逐字节一致（%d 行 + 表头）" % (OUT, len(lines) - 1))
        return

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "wb") as fh:
        fh.write(text.encode("utf-8"))
    n_lin = sum(1 for n in names if recs[n]["cs"] == 0)
    n_mip = sum(1 for n in names if recs[n]["mip"] > 1)
    n_clamp = sum(1 for n in names if recs[n]["wu"] == 1)
    n_mirror = sum(1 for n in names if recs[n]["wu"] == 2)
    print("✅ %s：%d 个名字（colorSpace=0 的 %d 个 · mipCount>1 的 %d 个 · wrapU=Clamp 的 %d 个 · "
          "wrapU=Mirror 的 %d 个）"
          % (OUT, len(names), n_lin, n_mip, n_clamp, n_mirror))


if __name__ == "__main__":
    main()
