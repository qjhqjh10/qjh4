#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""import_original_card_sdf.py — 把原版卡面那层 **SDF 软光/影** 的贴图导进工程

**为什么要它**：原版卡面最底层有一层 4.4281² 的软光/影（`Card Highlight And Shadow`）——
卡牌落地感的来源。2026-09-19 查实（`资料/普查产出_0919/卡面SDF软光影_查证.md`）：
它不是运行时生成的，而是 **Addressables 里预生成的 SDF 资产**：
`40k_Cardframe{s}_{troop|stratagem}_<阵营>_SDF_tier{1..4}`，**13 阵营 × 4 tier × 2 类型 = 104 张**，
本机解包资源里就有。另有一张**通用**的 `Card board frame SDF.png`。

**导出到哪**：`Assets/CardPresentation/Resources/Art/card_sdf/`（该目录 **gitignore**，是本地件，
和 `cards/frame_*.png` 同一个规矩：随时可以用本脚本重建）。
命名与 `cards/frame_*` **一一对应**：`frame_goff_tier2.png` ↔ `card_sdf/goff_tier2.png`、
`frame_goff_strat_tier2.png` ↔ `card_sdf/goff_strat_tier2.png`（`CardArt` 两边用同一套拼法）。

用法
----
  PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/import_original_card_sdf.py
  # 加 --check（= --dry-run）只体检、不写文件

⚠️ 导出来的仍是**原版美术**。本项目是个人学习用途（版权红线 2026-09-18 已由用户取消），照用。

🔴 **`.meta` 里的 `sRGBTexture: 0` 是本脚本的硬不变量**（2026-10-22 补）
----------------------------------------------------------------------
**判据（原版 Gamma + `Texture2D m_ColorSpace=1` + 我们的 Linear 工程）**：

* 原版是 **Gamma 色彩空间** —— `unity_run_ref/Warpforge_Data/globalgamemanagers` offset 272 = 0
  （`ColorSpace.Gamma`）；而这张 SDF 在原版资产里是 **`m_ColorSpace = 1`**（sRGB 标记）
  ⇒ 原版**按贴图的原始字节值采样**（Gamma 下 sRGB 标记等于不做事）。
* 我们的工程是 **Linear**（`ProjectSettings.asset` 的 `m_ActiveColorSpace: 1`）
  ⇒ 若 meta 里 `sRGBTexture: 1`，Unity 会多做一次 sRGB→linear 解码（R 0.565 → 0.278）。
* 后果（同一张 SDF 的 R 剖面）：绿光描边带 **Gamma ⇒ |x|≈0.84..0.99 卡半宽**（贴着卡框外沿）；
  **Linear ⇒ 0.69..0.83**（落进画窗内沿）。之前 `CardView.OutlineGlowScale = 1.35` 就是给这个
  色彩空间差做补偿（0.75×1.35≈1.01）。**`sRGBTexture: 0` 才是与原版等价的采法**。
* 出处：`资料/普查产出_1022/W63_换牌倾斜与绿光判据_调查.md` §B（「🔴 更深一层：原版是 Gamma、我们是 Linear」）。

⚠️ **只拷 PNG、不管 meta 是不行的**：`ArtBaker.ApplyImportSettings` **不碰** `sRGBTexture`
（它不会把 0 改回 1），但**删掉 meta 重新导入** = Unity 默认 **`sRGBTexture: 1`** ⇒ 绿光又缩回去。
所以**拷完 PNG 就在这里把 meta 钉成 0**（幂等 —— 已经是 0 就一个字节都不写）：

* **meta 已存在** ⇒ 只把 `sRGBTexture` 那一行改成 `0`；`guid` / 其它字段 / **原行尾**全都原样不动。
* **meta 不存在** ⇒ 按下面 `META_TEMPLATE` 生成一份**最小 TextureImporter meta**
  （`textureType: 0` = Default、`filterMode: 1` = Bilinear、`wrapU/V/W: 1` = Clamp ——
  这三个值不是挑的，是照 `ArtBaker.ApplyImportSettings` 的设定 + 本目录 107 份已有 meta 实测抄的），
  **`guid` = 相对路径的 md5**（带 `warpforge-card-sdf:` 命名空间前缀，照 `import_original_luts.py:88-90`
  的规矩，避免与别的生成器撞号）⇒ **重复运行 guid 不变**，引用不会被打散。

`--check` / `--dry-run`：**只比对、完全不写盘**（不建目录、不拷 PNG、不动 meta），
退出码 **0 = 全部已是 0**。
"""

import argparse
import glob
import hashlib
import io
import os
import re
import shutil
import sys

SRC_GLOB = "d:/2/新解包资源/assets_full/bundle_*cardassets_assets_all/Texture2D/*SDF_tier*.png"
GENERIC = ("d:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/Texture2D/"
           "Card board frame SDF.png")
# 🆕 2026-09-26：**卡背那层 SDF** 要的噪声图。
#   原版材质 `Card Backs SDF`（`bundle_duplicateassetisolation_assets_all/Material/Material_6996605638394099752.json`）
#   的 `_Noise` 槽 → pathID `-8214795764591836323` = 贴图 **`Noise Combined`**（128×128）。
#   材质上的 `_NoiseColor` = (1, 0.67256, 0, 1)、`_NoiseIntensity` = 0.2、关键字 `_NOISE_CHANNEL_B`。
CARDBACK_NOISE = ("d:/2/新解包资源/assets_full/bundle_duplicateassetisolation_assets_all/Texture2D/"
                  "Noise Combined.png")
DEST_DIR = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/card_sdf"

# ---------------------------------------------------------------------------
# `.meta` 的 `sRGBTexture: 0`（判据见模块 docstring 那一节）
# ---------------------------------------------------------------------------
# 只在「meta 不存在」时才用这份模板（存在的话只改那一行，别的字段一概不动）。
# 里面每个值都有出处，不是挑的：
#   sRGBTexture: 0        ← 原版 Gamma 按原始值采（见 docstring · W63 §B）
#   textureType: 0        ← Default（`ArtBaker.ApplyImportSettings`：`TextureImporterType.Default`，
#                            我们自己按 UV 摆，不要 Sprite）
#   filterMode: 1         ← Bilinear（同上 `ti.filterMode = FilterMode.Bilinear`）
#   wrapU/V/W: 1          ← Clamp（同上；本目录 107 份实测 meta 全是 1/1/1）
#   enableMipMap: 0       ← 同上 `ti.mipmapEnabled = false`
#   alphaIsTransparency: 0← 同上（卡面要**透明区的 RGB**，踩过，见 ArtBaker.cs 那段注释）
#   maxTextureSize: 1024  ← 同上（`Resources/Art` 下非 backdrop 非 ui_deck ⇒ 1024）
#   serializedVersion: 13 ← Unity 6000.3 的 TextureImporter 版本（照本目录已有 meta）
# 其余字段留省略 ⇒ Unity 按默认补齐（导入时它会自己把文件重写成完整形态）。
META_TEMPLATE = """fileFormatVersion: 2
guid: __GUID__
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 0
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 1024
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 0
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 100
  spriteBorder: {x: 0, y: 0, z: 0, w: 0}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 0
  spriteTessellationDetail: -1
  textureType: 0
  textureShape: 1
  singleChannelComponent: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings: []
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID:
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def guid_for(png_path):
    """meta 的 guid = **相对路径的 md5**（稳定 ⇒ 重复运行 / 删 meta 重导都不变）。

    相对谁：Unity 工程里的 **`Assets/` 之后那一段**（= Unity 认资产的唯一身份）。
    `warpforge-card-sdf:` 前缀照 `import_original_luts.py:88-90` 的规矩，避免与别的生成器撞号。
    """
    ap = os.path.abspath(png_path).replace("\\", "/")
    i = ap.lower().rfind("/assets/")
    rel = ap[i + len("/assets/"):] if i >= 0 else os.path.basename(ap)
    return hashlib.md5(("warpforge-card-sdf:" + rel).encode("utf-8")).hexdigest()


def set_srgb0(text):
    """把 meta 文本里 `mipmaps:` 块内的 `sRGBTexture` 改成 0。返回 (新文本, 是否改动)。

    * **幂等**：已经是 0 ⇒ 原样返回 `(text, False)`，调用方连写都不写（mtime 都不动）。
    * **只动那一行**：`guid` / 其它字段一概不碰（`guid` 变了 = 全工程引用散架）。
    * **行尾逐行保留**：用 `splitlines(keepends=True)` 按行处理，只把这一行的值换掉 ——
      这些 meta 是 LF，但工程里有 CRLF 的资产，别整篇翻掉（CLAUDE.md「行尾」那条）。
    * 没有 `mipmaps:` 块 ⇒ 不是 TextureImporter 的 meta，**不碰**（返回未改动）。
    * 有块但没有这个键（极老格式）⇒ 紧跟块头补一行，缩进照块头 +2。
    """
    lines = text.splitlines(keepends=True)
    i_mip = None
    for idx, ln in enumerate(lines):
        if ln.strip() == "mipmaps:":
            i_mip = idx
            break
    if i_mip is None:
        return text, False
    hdr = lines[i_mip]
    hdr_indent = len(hdr) - len(hdr.lstrip())
    end = len(lines)
    for idx in range(i_mip + 1, len(lines)):
        s = lines[idx].strip()
        if s and (len(lines[idx]) - len(lines[idx].lstrip())) <= hdr_indent:
            end = idx                      # 缩进回到块头 ⇒ 块结束
            break
    for idx in range(i_mip + 1, end):
        body = lines[idx].rstrip("\r\n")
        if body.strip().startswith("sRGBTexture:"):
            if body.split(":", 1)[1].strip() == "0":
                return text, False
            eol = lines[idx][len(body):]   # '' / '\n' / '\r\n' —— 原样保留
            lines[idx] = body.split(":", 1)[0] + ": 0" + eol
            return "".join(lines), True
    eol = "\r\n" if hdr.endswith("\r\n") else "\n"
    lines.insert(i_mip + 1, " " * (hdr_indent + 2) + "sRGBTexture: 0" + eol)
    return "".join(lines), True


def ensure_meta(png_path, dry):
    """保证 `png_path + '.meta'` 存在且 `sRGBTexture: 0`。dry=True 时**一个字节都不写**。

    返回状态串：ok / fixed / created / wrong / missing / unreadable
    （前三个是「写完之后的状态 / dry 下等价于 OK」，后三个是「dry 下发现的问题」）。
    """
    meta = png_path + ".meta"
    if not os.path.isfile(meta):
        if dry:
            return "missing"
        body = META_TEMPLATE.replace("__GUID__", guid_for(png_path))
        with io.open(meta, "wb") as fh:            # wb ⇒ 不自作主张加 CRLF
            fh.write(body.encode("utf-8"))
        return "created"
    raw = io.open(meta, "rb").read()
    try:
        text = raw.decode("utf-8")
    except UnicodeDecodeError:
        return "unreadable"
    new, changed = set_srgb0(text)
    if not changed:
        return "ok"
    if dry:
        return "wrong"
    with io.open(meta, "wb") as fh:
        fh.write(new.encode("utf-8"))
    return "fixed"


def meta_pass(dry):
    """对整个 `DEST_DIR` 扫一遍（不只本次拷的那几张 —— 幂等收口，也覆盖 generic / 噪声图）。

    返回 (坏掉的数量, 坏掉的文件名列表)。dry=True 时**绝不写盘**（不存在也不建目录）。
    """
    if not os.path.isdir(DEST_DIR):
        print(f"[M] {DEST_DIR} 不存在（还没导过）—— 没有 meta 要处理")
        return 0, []
    pngs = sorted(f for f in os.listdir(DEST_DIR) if f.lower().endswith(".png"))
    cnt = {"ok": 0, "fixed": 0, "created": 0, "wrong": 0, "missing": 0, "unreadable": 0}
    bad = []
    for f in pngs:
        st = ensure_meta(os.path.join(DEST_DIR, f), dry)
        cnt[st] = cnt.get(st, 0) + 1
        if st in ("wrong", "missing", "unreadable"):
            bad.append((f, st))
    if dry:
        print(f"[M] meta 体检（只读）：{len(pngs)} 个 PNG · 已 OK {cnt['ok']}"
              f" · sRGBTexture!=0 **{cnt['wrong']}** · 缺 meta **{cnt['missing']}**"
              f" · meta 读不出 {cnt['unreadable']}")
        why = {"missing": "缺 .meta（Unity 重导会给 sRGBTexture: 1）",
               "wrong": "sRGBTexture != 0",
               "unreadable": "meta 不是 utf-8，读不出"}
        for f, st in bad[:20]:
            print(f"      !! {f:<44s} {why[st]}")
        if len(bad) > 20:
            print(f"      … 另有 {len(bad) - 20} 个（只列前 20）")
    else:
        print(f"[M] meta：新建 {cnt['created']} · 改成 sRGBTexture 0 {cnt['fixed']}"
              f" · 本来就对 {cnt['ok']} · 读不出 {cnt['unreadable']}")
        for f, st in bad:
            print(f"      !! {f} —— meta 读不出（不是 utf-8），没敢动")
    return len(bad), [f for f, _ in bad]

# 原版 SDF 里的阵营名 → **我们工程的阵营 key**（= `cards/frame_<key>.png` 里那一段，全小写）
# ⚠️ 四个不是「小写去空格」就能对上的，**必须显式写出来**（写错不会报错，只会静静少一层软光）：
#      Emperor Children → emperorschildren（我们多一个 s）
#      GSC              → genestealers（原版这里是缩写）
#      Orks             → goff（我们的兽人阵营叫 Goff）
#      Tau              → tauempire
#   另外两个「去空格」就对的：Astra Militarum → astramilitarum · Space Wolves → spacewolves。
FACTION = {
    "Astra Militarum": "astramilitarum",
    "BlackLegion": "blacklegion",
    "DarkAngels": "darkangels",
    "Emperor Children": "emperorschildren",
    "GSC": "genestealers",
    "Leviathan": "leviathan",
    "Orks": "goff",
    "SaimHann": "saimhann",
    "Sautekh": "sautekh",
    "Sororitas": "sororitas",
    "Space Wolves": "spacewolves",
    "Tau": "tauempire",
    "Ultramarines": "ultramarines",
}

RE = re.compile(r"40k_Cardframes?_(troop|Troop|stratagem|Stratagem)_(.+)_SDF_tier(\d)\.png$")


def main():
    ap = argparse.ArgumentParser()
    # `--dry-run` 是 `--check` 的别名（本脚本原来就叫 --check，两个都收）
    ap.add_argument("--check", "--dry-run", action="store_true", dest="check",
                    help="只体检，不写文件（含 .meta；完全不写盘）")
    args = ap.parse_args()

    files = sorted(glob.glob(SRC_GLOB))
    print(f"[1] 源目录里找到 {len(files)} 张 SDF 贴图（期望 13 阵营 × 4 tier × 2 类型 = 104）")

    jobs, unknown, seen = [], [], {}
    for p in files:
        m = RE.search(os.path.basename(p))
        if not m:
            unknown.append(os.path.basename(p))
            continue
        kind, fac, tier = m.group(1), m.group(2), m.group(3)
        key = FACTION.get(fac)
        if key is None:
            unknown.append(f"{os.path.basename(p)}（阵营「{fac}」没在 FACTION 表里）")
            continue
        name = f"{key}{'_strat' if kind.lower() == 'stratagem' else ''}_tier{tier}.png"
        if name in seen:
            print(f"      !! 重名：{name} ← {os.path.basename(p)} 与 {seen[name]}")
        seen[name] = os.path.basename(p)
        jobs.append((p, name))

    print(f"[2] 解析出 {len(jobs)} 张、目标名 {len(seen)} 个唯一")
    if unknown:
        print(f"[2b] 解析不出来的 {len(unknown)} 个：")
        for u in unknown:
            print("      !!", u)

    # 对账：每个阵营都该有 4 tier × 2 类型
    miss = []
    for key in sorted(set(FACTION.values())):
        for t in "1234":
            for s in ("", "_strat"):
                if f"{key}{s}_tier{t}.png" not in seen:
                    miss.append(f"{key}{s}_tier{t}.png")
    print(f"[3] 按「13 阵营 × 4 tier × 2 类型」对账：缺 {len(miss)} 个")
    for m in miss:
        print("      !! 缺", m)

    if args.check:
        # 🔴 这里**绝不写盘**：不建目录、不拷 PNG、不动 meta（本脚本会覆盖 assets，check 必须纯读）
        n_meta_bad, _ = meta_pass(dry=True)
        # 🔴 2026-10-27（执行代理 B9_W35CHECK）：`--check` 必须**连产物一起查** ——
        #   上面 `miss` 是「源能不能解析出来」，`meta_pass` 只扫**已存在**的 PNG
        #   ⇒ 产物 PNG 被删/没导过时，两者都看不见（`meta_pass` 根本进不了那个文件）。
        #   判据同 工具/import_original_art.py `--only-altart`（`:1714-1721`）。
        expected = {name for _s, name in jobs}
        if os.path.exists(GENERIC):
            expected.add("generic.png")
        if os.path.exists(CARDBACK_NOISE):
            expected.add("cardback_noise.png")
        lack_prod = sorted(n for n in expected
                           if not os.path.isfile(os.path.join(DEST_DIR, n)))
        if lack_prod:
            print(f"🔴 产物缺失 {len(lack_prod)}/{len(expected)}（{DEST_DIR}）—— 重跑不带 --check 落盘：")
            for n in lack_prod[:40]:
                print("   缺:", n)
            if len(lack_prod) > 40:
                print(f"   …共 {len(lack_prod)} 个")
        print("\n--check：只体检，未写文件")
        ok = not (unknown or miss or n_meta_bad or lack_prod)
        if not ok:
            print(f"--check 不通过：解析不出 {len(unknown)} · 缺图 {len(miss)} · "
                  f"meta/字符串 {n_meta_bad} · 产物缺失 {len(lack_prod)}")
        return 0 if ok else 1

    os.makedirs(DEST_DIR, exist_ok=True)
    for src, name in jobs:
        shutil.copyfile(src, os.path.join(DEST_DIR, name))
    print(f"[4] 已写入 {len(jobs)} 张 → {DEST_DIR}")

    if os.path.exists(GENERIC):
        shutil.copyfile(GENERIC, os.path.join(DEST_DIR, "generic.png"))
        print("[5] 通用那张 `Card board frame SDF.png` → generic.png")
    else:
        print("[5] !! 通用那张没找到（不影响，只是少一个兜底）")

    # 🆕 2026-09-26：卡背 SDF 那层要的噪声图
    if os.path.exists(CARDBACK_NOISE):
        shutil.copyfile(CARDBACK_NOISE, os.path.join(DEST_DIR, "cardback_noise.png"))
        print("[5b] 卡背 SDF 的 `_Noise`（原版贴图 `Noise Combined`）→ cardback_noise.png")
    else:
        print("[5b] !! `Noise Combined` 没找到 ⇒ 卡背 SDF 那层会**没有噪声**（画得出，但少一点颗粒）")

    # 🔴 **拷完 PNG 就在这里把 meta 钉成 `sRGBTexture: 0`**（判据见模块 docstring）。
    #   为什么必须在「拷完之后」：`shutil.copyfile` 只覆盖 PNG，meta 是**另一份文件**；
    #   而只要 meta 在，Unity 就沿用它 —— 只有 meta **缺失**时 Unity 才会新建一份（默认 sRGBTexture: 1）。
    #   整目录扫一遍（不只本次拷的）：幂等、也顺手收口 generic / cardback_noise 那两张。
    n_meta_bad, _ = meta_pass(dry=False)
    if n_meta_bad:
        print("      !! 有 meta 读不出 —— 上面逐条列了，**没有静默跳过**")

    total = sum(os.path.getsize(os.path.join(DEST_DIR, f)) for f in os.listdir(DEST_DIR))
    print(f"[6] 目录现在 {len(os.listdir(DEST_DIR))} 个文件、{total/1024:.0f} KB")
    print("\n⚠️ 导完记得在 Unity 里跑一次 `ArtBaker.ApplyImportSettings`（mipmap/alpha 那套统一设置）")
    return 0


if __name__ == "__main__":
    sys.exit(main())
