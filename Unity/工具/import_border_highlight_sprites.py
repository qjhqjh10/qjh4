#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""import_border_highlight_sprites.py — 把「边框高亮」那一族材质**真正用的那张 sprite** 导进工程

为什么要有它
------------
原版 shader `Everguild/UI/UI Border Highlight Appear Pulsating Scale` 的 `_MainTex`
**不在材质的 `m_TexEnvs` 里**（5 份材质逐份现读：只有 `_Noise_R`）——
它是 uGUI 的 **per-renderer 纹理**（`Graphic.mainTexture` = `Image.m_Sprite.texture`）。
⇒ 复刻「边框形状来自 `_MainTex.a`」这一条（判据 → `资料/完全复刻/判据/B114_R5_Reveal_DXBC.md` §2·2 ⑤）
就必须把**那个 Image 的 sprite** 找到并喂进去。

本工程用到这颗 shader 的只有 **1 个材质** `Border Effect For Level up`（主菜单 `Level Up Effect` /
`Level Up Effect Down` 两件，`Shell/CardDisplayEffects.cs`），它对应的两张图是：

    Level Up Effect       → Image.m_Sprite = `OctagonUI Filled SDF`   (128², border 52)
    Level Up Effect Down  → Image.m_Sprite = `OctagonUI Border SDF`   (128², border 52)

判据（全部现读，2026-11-01 · 写手 `W-8`）
  · 材质 → 用它的 Image：`d:/2/tools/assetripper_out/ExportedProject/Assets/Data/Scenes/MainMenu Warpforge.unity`
    的 `2b1eaf3b9ec4b9d469a8b7570d99d153`（= `Border Effect For Level up.mat.meta` 的 guid）四处命中：
    两处是 `m_Material`（Image 2057 → GameObject 404 `Level Up Effect Down`；
    Image 2545 → GameObject 338 `Level Up Effect`），两处是 `AnimateUIMaterialProperty.changeMaterialOnAnimation`。
  · sprite → 贴图：`Assets/OctagonUI {Border,Filled} SDF.asset` 的 `m_RD.texture` =
    `7622c9f2ff71fc343a384bf40bdb72c4` / `a7ea0f52deaa78746aa6c7ddb858c4f8`。
  · 落哪个 bundle：两张 sprite 的 `.meta` 里 `assetBundleName` =
    `a33094020739718ee8155f70617a4ac1`（= `menusharedresources_assets_all`）/
    `a57b3f2fd9f9b3aeac58cabe276618f6`（= `duplicateassetisolation_assets_all`）。

落到哪
------
`Unity/MyGame/Assets/CardPresentation/Resources/Art/ui_menu/`，**空格换下划线**（与 `CardArt.MenuUi`
的查法、以及同族的三张 `Border_*_FX.png` 一致 ⇒ 读取口 = `CardArt.MenuUi("OctagonUI_Border_SDF")`）。

⚠️ `Resources/Art/` 整个目录在 `.gitignore` 里（第 99-100 行）⇒ **盘上的图是本地件、随时可用本脚本重建**，
   所以「脚本 + 这张表」才是可复现的那一份。

用法
----
    python -I -X utf8 d:/4/Unity/工具/import_border_highlight_sprites.py --check
    python -I -X utf8 d:/4/Unity/工具/import_border_highlight_sprites.py --apply

🔴 `--check` / `--apply` **必须二选一**；不给（或两个都给）就只打印用法并退出 ——
   写盘类脚本「不带显式动作参数时什么都不做」（CLAUDE.md §三）。

`.meta` 的口径
--------------
模板逐字段照 `Art/ui_menu/Border_Circles_FX.png.meta`（同目录、同族、已在用），只改 `guid`：
· `textureType: 0`（Default）—— 我们要的是 `Texture2D`，不是 `Sprite` 子资产；
  `spriteBorder` 那 52 本脚本**不写**（原版是 Sliced 九宫格，但本工程这条链是**整张 quad**，
  九宫格切片是另一件事，见报告「没查清的部分」）。
· `sRGBTexture: 1` / `enableMipMap: 0` / `filterMode: 1`（Bilinear）/ `wrapU/V/W: 1`（Clamp）——
  取原版 `.meta` 的同类值（`wrap 1 = Clamp` 很重要：shader 的 `uv2 += nz * _Noise_Strength.x`
  会把 uv 推出 [0,1]，Clamp 才不会绕回来）。
· `alphaIsTransparency: 1` —— 原版 `.meta` 是 1（同目录三张邻居是 0，但那是别批导的；
  这张是**软边 SDF**，按原版走）。
· `guid` = `md5('warpforge-uiborder:' + 相对路径)` ⇒ **重复运行 / 删 meta 重导 guid 都不变**。
"""

import argparse
import hashlib
import io
import os
import shutil
import sys

# ── 路径 ─────────────────────────────────────────────────────────────────────
HERE = os.path.dirname(os.path.abspath(__file__))            # d:/4/Unity/工具
UNITY = os.path.dirname(HERE)                                # d:/4/Unity
DEST_DIR = os.path.join(UNITY, 'MyGame', 'Assets', 'CardPresentation', 'Resources', 'Art', 'ui_menu')
EXTRACT = 'd:/2/Warpforge_tools/data/ui_extract'
RIPPER = 'd:/2/tools/assetripper_out/ExportedProject/Assets'

# (ui_extract 里的切片名, 它落在哪个 bundle 目录下, 落盘名（空格换下划线）)
SPRITES = [
    ('OctagonUI Border SDF', 'menusharedresources_assets_all', 'OctagonUI_Border_SDF'),
    ('OctagonUI Filled SDF', 'duplicateassetisolation_assets_all', 'OctagonUI_Filled_SDF'),
]

META_TEMPLATE = '''fileFormatVersion: 2
guid: __GUID__
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
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
  maxTextureSize: 2048
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
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 0
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 2
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Standalone
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData:
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
'''


def guid_for(png_path):
    """meta 的 guid = md5(命名空间 + 相对路径)，**稳定**（与 `import_original_card_sdf.py` 同一条规矩）。"""
    rel = os.path.relpath(png_path, UNITY).replace('\\', '/')
    return hashlib.md5(('warpforge-uiborder:' + rel).encode('utf-8')).hexdigest()


def find_source(name, bundle):
    """按 `Sprite/` → `Texture2D/` 找源 PNG。

    ⚠️ `OctagonUI Border SDF` 在 `menusharedresources` 下**只有 Sprite 的 .json、没有 Sprite 的 .png**
    （那个 bundle 里这张图只导出了 Texture2D）⇒ 必须两档都试。
    """
    for sub in ('Sprite', 'Texture2D'):
        p = os.path.join(EXTRACT, bundle, sub, name + '.png')
        if os.path.exists(p):
            return p
    return None


def pixels(png_path):
    """解码后的 RGBA 像素（用来判「两份文件内容是否同一张图」—— 光比 sha256 不行，PNG 编码会不同）。"""
    from PIL import Image
    im = Image.open(png_path).convert('RGBA')
    return im.size, im.tobytes()


def check_one(name, bundle, dest_name):
    """返回 dict：source / dest / 状态。只读，不写盘。"""
    src = find_source(name, bundle)
    dst = os.path.join(DEST_DIR, dest_name + '.png')
    rec = {'name': name, 'bundle': bundle, 'src': src, 'dst': dst, 'issues': []}
    if src is None:
        rec['issues'].append('源 PNG 找不到（`ui_extract/<bundle>/{Sprite,Texture2D}/` 都没有）')
        return rec

    # 与原版导出工程里的那张图逐像素比（防止 ui_extract 那份是紧裁过的）
    rip = os.path.join(RIPPER, name + '.png')
    if os.path.exists(rip):
        try:
            if pixels(src) != pixels(rip):
                rec['issues'].append('源 PNG 与 `assetripper_out/…/%s.png` **像素不一致**' % name)
        except Exception as e:                       # pragma: no cover
            rec['issues'].append('像素比对失败：%s' % e)
    else:
        rec['issues'].append('⚠️ 找不到原版导出工程里的 `%s.png`（跳过像素比对）' % name)

    # 盘上那份
    if not os.path.exists(dst):
        rec['state'] = '缺文件'
    else:
        try:
            if pixels(dst) == pixels(src):
                rec['state'] = '已一致'
            else:
                rec['state'] = '内容不同'
        except Exception as e:                       # pragma: no cover
            rec['state'] = '读不了（%s）' % e

    # meta（已存在就算「不动」—— 只有缺了才会按模板生成，见 `apply_one`）
    meta = dst + '.meta'
    if not os.path.exists(meta):
        rec['meta'] = '缺 meta（--apply 会按模板补）'
    else:
        head = io.open(meta, encoding='utf-8', errors='replace').read(400)
        want = guid_for(dst)
        rec['meta'] = ('已存在（不动）· guid %s…' % want[:8]) if want in head else '已存在（不动）'
    return rec


def apply_one(rec):
    src, dst = rec['src'], rec['dst']
    if src is None:
        return '⛔ 跳过（无源）'
    did = []
    if rec['state'] != '已一致':
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        shutil.copyfile(src, dst)
        did.append('拷 PNG')
    meta = dst + '.meta'
    # 🔴 只在**没有 meta** 时生成（= 目录被清空后重建）。
    #    已存在的 meta **一个字节都不动** —— 换 guid 没有任何收益（本工程没有按 guid 引用它们的地方），
    #    却会让「盘上那份」和「上一次导入的那份」在两个不同 id 之间来回跳。
    if not os.path.exists(meta):
        body = META_TEMPLATE.replace('__GUID__', guid_for(dst))
        # 二进制写，避免 python 文本模式在 Windows 上写 CRLF（与邻居的 LF 不一致）
        with io.open(meta, 'wb') as f:
            f.write(body.encode('utf-8'))
        did.append('写 meta')
    return ('、'.join(did) if did else '无改动')


def main():
    ap = argparse.ArgumentParser(add_help=False)
    ap.add_argument('--check', action='store_true')
    ap.add_argument('--apply', action='store_true')
    args, _unknown = ap.parse_known_args()

    if args.check == args.apply:          # 都不给、或两个都给
        print(__doc__, file=sys.stderr)
        print('🔴 必须且只能给 `--check` 或 `--apply` 其中之一 ⇒ 什么都没做。', file=sys.stderr)
        return 2

    rc = 0
    for name, bundle, dest_name in SPRITES:
        rec = check_one(name, bundle, dest_name)
        tag = '[check]' if args.check else '[apply]'
        print('%s %s  (%s)' % (tag, name, bundle))
        print('        源  = %s' % rec['src'])
        print('        目标 = %s' % rec['dst'])
        print('        状态 = %s · meta = %s' % (rec.get('state', '?'), rec.get('meta', '?')))
        for i in rec['issues']:
            print('        ⚠️ %s' % i)
            rc = 1
        if not args.check:
            print('        动作 = %s' % apply_one(rec))
    return rc


if __name__ == '__main__':
    sys.exit(main())
