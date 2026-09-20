#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""gen_skybox_cubemap.py — 导出原版战斗场景的天空盒 6 面（13 场共用同一张）

为什么要它：**原版 13 个战场的 `RenderSettings.m_SkyboxMaterial` 全是同一个**
`Skybox Clouds Dusk`，而我们**一个都没设** ⇒ 战场上有洞的地方会透出 **Unity 默认天空盒**
（灰褐色），而原版那里是黄昏云。实测最明显的是 sororitas（地板修好之后，红毯护栏之外
那两角仍是灰褐）。见 `资料/普查产出_0920/场景光照与后处理_原版规格.md` §13.1。

原版规格（逐值实读，别改）：
- 材质 `Skybox Clouds Dusk`（`battlesharedresources_assets_all`，pid `-246617819069842608`）
- shader **`Skybox/Cubemap`**（Unity 内置，在 `Warpforge_unitybuiltinassets`）
- cubemap `Skybox Clouds`（pid `418035609478688544`）：256×256、**BC6H**（TextureFormat 24，HDR）、
  6 面、9 级 mip ⇒ 每面 87408 B（mip0 = 65536 B）
- 材质参数：`_Tint = (0.5490196, 0.5061521, 0.4039216, 0.5)` · `_Exposure = 0.67` · `_Rotation = 0`

⚠️ **两个踩过的坑**（重跑时别重踩）：
1. `ObjectReader.get_raw_data()` 对这个 cubemap 只返回 **292 字节**（元数据），**不是像素** ——
   UnityPy 的 `Cubemap` 类没实现像素读取（`image_data` 是空的）。真数据在 bundle 的
   **`.resS` 流**里：`m_StreamData = {offset: 49063696, size: 524448, path: 'archive:/CAB-…/CAB-….resS'}`。
   读法 = `env.file.files['CAB-….resS'].read()[offset : offset+size]`。
2. **TextureFormat 24 是 BC6H、不是 BC7**（25 才是 BC7）。用 `decode_bc7` 解出来是一张彩色噪点图。

用法：
  PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/gen_skybox_cubemap.py
产物：`Assets/WarpforgeArena1/skybox/{pZ,mZ,pX,mX,pY,mY}.png`（6 张 256²，**按 Unity 的 `Skybox/Cubemap` 面序**）
"""
import io
import os
import sys

sys.stdout.reconfigure(encoding='utf-8')

BUNDLE_DIR = r'd:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/'
BUNDLE = 'battlesharedresources_assets_all.bundle'
CUBEMAP_PID = '418035609478688544'
CUBEMAP_TEX_FORMAT = 24          # BC6H（HDR，1 B/px）
FACE_W = FACE_H = 256
MIP0_BYTES = FACE_W * FACE_H     # BC6H = 1 字节/像素
FACE_STRIDE = 87408              # 每面（含 9 级 mip）的字节数；实测 = 524448 / 6
RES_S_NAME = 'CAB-68cc81d35f88d5ad277f2717d6e1ac25.resS'

# Unity `Skybox/Cubemap` 的面序（= CubemapFace 枚举序）
FACE_ORDER = ['pX', 'mX', 'pY', 'mY', 'pZ', 'mZ']
OUT_DIR = r'd:/4/Unity/MyGame/Assets/WarpforgeArena1/skybox'


def main():
    import UnityPy
    import texture2ddecoder as t2d
    from PIL import Image

    env = UnityPy.load(BUNDLE_DIR + BUNDLE)

    # ---- 1. 定位 cubemap 对象，拿它的 m_StreamData 偏移 ----
    off = size = None
    for o in env.objects:
        if str(o.path_id) != CUBEMAP_PID:
            continue
        t = o.read_typetree()
        if t.get('m_TextureFormat') != CUBEMAP_TEX_FORMAT:
            raise SystemExit('格式变了（期望 %d=BC6H，实际 %s）—— 别按旧假设解码'
                             % (CUBEMAP_TEX_FORMAT, t.get('m_TextureFormat')))
        sd = t.get('m_StreamData') or {}
        off, size = sd.get('offset'), sd.get('size')
        print('cubemap %s：%dx%d · 格式 %d(BC6H) · %d 面 · mip %s' % (
            t.get('m_Name'), t.get('m_Width'), t.get('m_Height'), t['m_TextureFormat'],
            t.get('m_ImageCount'), t.get('m_MipCount')))
        print('  m_StreamData offset=%s size=%s' % (off, size))
        break
    if off is None:
        raise SystemExit('没找到 cubemap pid=%s' % CUBEMAP_PID)
    if size != FACE_STRIDE * 6:
        print('⚠️ size %s ≠ 6×%d=%d —— 面跨距可能变了，先核对再跑' % (size, FACE_STRIDE, FACE_STRIDE * 6))

    # ---- 2. 从 .resS 流里取像素（get_raw_data() 给的是元数据，不是像素）----
    rs = env.file.files[RES_S_NAME]
    blob = rs.read()[off:off + size]
    print('  从 %s 切出 %d 字节' % (RES_S_NAME, len(blob)))

    # ---- 3. 逐面解 BC6H ----
    os.makedirs(OUT_DIR, exist_ok=True)
    for i, name in enumerate(FACE_ORDER):
        mip0 = blob[i * FACE_STRIDE:i * FACE_STRIDE + MIP0_BYTES]
        px = t2d.decode_bc6(mip0, FACE_W, FACE_H)      # 出 RGBA 8bit
        if len(px) != FACE_W * FACE_H * 4:
            raise SystemExit('面 %s 解码长度不对：%d' % (name, len(px)))
        dst = os.path.join(OUT_DIR, name + '.png')
        Image.frombytes('RGBA', (FACE_W, FACE_H), px, 'raw', 'RGBA').save(dst)
        print('  → %s' % dst)

    # ---- 4. 把材质参数落一份，Unity 侧照抄 ----
    params = os.path.join(OUT_DIR, 'material_params.json')
    with io.open(params, 'w', encoding='utf-8') as fp:
        fp.write('{\n'
                 '  "material": "Skybox Clouds Dusk",\n'
                 '  "shader": "Skybox/Cubemap",\n'
                 '  "_Tint": [0.5490196, 0.5061521, 0.4039216, 0.5],\n'
                 '  "_Exposure": 0.67,\n'
                 '  "_Rotation": 0.0\n'
                 '}\n')
    print('  → %s' % params)
    print('✅ 6 面导出完毕（Unity 侧由 ArenaBuilder.SetupSkybox 建 Cubemap 资产 + 材质）')
    return 0


if __name__ == '__main__':
    sys.exit(main())
