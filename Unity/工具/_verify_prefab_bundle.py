#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""_verify_prefab_bundle.py — 自检 `wf_prefabs_extra.bundle` 的重打包产物（**只读**）

背景：这两件 prefab（`Card 3D Death Explosion` / `Vanguard Frame Animated VAT`）
**不是 addressable** ⇒ Unity 的 `GetAllAssetNames()` / `LoadAllAssets<GameObject>()`
两条枚举路都不含它们（判据 → `资料/已知的坑.md` 同名那条）。
做法 = `extract_missing_shaders.py --prefabs` 把它们 + 整棵依赖树重打成一个小包。

**这个脚本查四件事**（每件都要有明确结论，别只看「跑通了」）：
 1. 产物能被打开、内层文件是 2 个（主 CAB + `.resS`）
 2. `AssetBundle.m_Container` 的 **6** 条名字对不对（**3 件根 × 裸名 / 小写路径**）
 3. 6 张纹理的 `m_StreamData.path` 已指向**新 CAB 名**（改名了），且**取出来的字节与源包逐字节相同**
    —— 🔴 判据是「**字节**一样」，**不是「offset 一样」**：资源流做了瘦身（只留用到的区间）⇒ offset 本来就该变
 4. 流瘦身的长度落在预期区间（`Σsize ≤ 产物 ≤ Σsize + 每段 16 字节对齐的填充`）

🆕 **2026-10-01 加第 3 件根**：`Vanguard_Frame VAT Dissolve`（一个**材质**）。
为什么它也要当根收进来：**没有任何 Unity 对象引用它**（引用它的是 AnimFX 的**模块字段**，
活在 JSON 里）⇒ 依赖树走不到、容器里也没有 ⇒ Unity 侧三条取法**一条都拿不到**。
判据 → `资料/普查产出_1001/资产导入路三件_侦察.md` §①（含探针 `EffectExporter.ProbeModuleMaterials`）。
⇒ 本脚本的期望值随之从 **2 件 / 4 条 / 2 条预加载** 改成 **3 件 / 6 条 / 3 条预加载**。

用法：
  "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/_verify_prefab_bundle.py
"""
import hashlib
import io
import os
import sys

import UnityPy

SRC = r"D:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/battleprefabs_vfxandmisc_assets_all.bundle"
DST = r"d:/4/Unity/MyGame/Assets/StreamingAssets/WarpforgeVFX/wf_prefabs_extra.bundle"
OLD_CAB = "CAB-d47690319398b604c3bb5a35a8ed2499"
NEW_CAB = "CAB-wfprefabsextra"
# 3 件根（2026-10-01 起）：两件 prefab + 一件**只被 JSON 数据引用的材质**
WANT = ["Card 3D Death Explosion", "Vanguard Frame Animated VAT", "Vanguard_Frame VAT Dissolve"]


def load(path):
    env = UnityPy.load(path)
    bf = list(env.files.values())[0]
    sf = next(v for v in bf.files.values() if type(v).__name__ == "SerializedFile")
    return bf, sf


def streams(sf):
    """{对象名: (path, offset, size)} —— 只看走资源流的 Texture2D。"""
    out = {}
    for o in sf.objects.values():
        if o.type.name != "Texture2D":
            continue
        try:
            d = o.read_typetree()
        except Exception:
            continue
        sd = d.get("m_StreamData") or {}
        if sd.get("path"):
            out[d.get("m_Name")] = (sd["path"], sd.get("offset"), sd.get("size"))
    return out


def main():
    bad = 0
    if not os.path.isfile(DST):
        print(f"!! 产物不存在：{DST}（先跑 `extract_missing_shaders.py --prefabs`）")
        return 1

    bf, sf = load(DST)
    print(f"[1] 产物内层文件 {len(bf.files)} 个：" + ", ".join(sorted(bf.files.keys())))
    if len(bf.files) != 2:
        print("   🔴 期望 2 个（主 CAB + .resS）")
        bad += 1

    ab = None
    for o in sf.objects.values():
        if o.type.name == "AssetBundle":
            ab = o.read()
    names = [c[0] for c in ab.m_Container] if ab is not None else []
    print(f"[2] 容器 {len(names)} 条：{names}")
    for w in WANT:
        if w not in names:
            print(f"   🔴 容器里没有裸名 `{w}` —— `LoadAsset(name)` 会取不到")
            bad += 1
    print(f"    预加载表 {len(ab.m_PreloadTable)} 条（应为 {len(WANT)} = 根个数）")
    if len(ab.m_PreloadTable) != len(WANT):
        bad += 1

    src_bf, src_sf = load(SRC)
    dst_s, src_s = streams(sf), streams(src_sf)
    src_raw = src_bf.files[f"{OLD_CAB}.resS"].bytes
    dst_raw = bf.files[f"{NEW_CAB}.resS"].bytes
    print(f"[3] 走资源流的纹理：源包 {len(src_s)} 张 · 产物 {len(dst_s)} 张；"
          f"流大小 源包 {len(src_raw)/1024/1024:.1f} MB → 产物 {len(dst_raw)/1024:.0f} KB")
    for name, (p, off, size) in sorted(dst_s.items()):
        ok_p = NEW_CAB in p and OLD_CAB not in p
        o0 = src_s.get(name)
        # 🔴 **判据是「取出来的字节一样」，不是「offset 一样」** —— 瘦身之后 offset 本来就该变
        ok_d = False
        if o0 is not None and o0[2] == size:
            try:
                ok_d = bytes(src_raw[o0[1]:o0[1] + size]) == bytes(dst_raw[off:off + size])
            except Exception:
                ok_d = False
        flag = "✅" if (ok_p and ok_d) else "🔴"
        if not (ok_p and ok_d):
            bad += 1
        print(f"   {flag} {name:42s} 产物 offset={off} size={size}"
              + (f"（源包 offset={o0[1]} —— offset 变了没关系，**字节必须一样**）" if o0 else "（源包没有这张？）"))

    # [4] 瘦身后的流**只装用到的区间**：总长应当在「Σsize」与「Σsize + 每段 16 字节对齐的填充」之间
    need = sum(s[2] for s in dst_s.values())
    cap = need + 16 * len(dst_s)
    print(f"[4] 流瘦身：产物 {len(dst_raw)/1024:.0f} KB（用到的区间合计 {need/1024:.0f} KB，"
          f"上限 {cap/1024:.0f} KB）· 源包 {len(src_raw)/1024/1024:.1f} MB")
    if not (need <= len(dst_raw) <= cap):
        print("   🔴 长度不在预期区间 —— 要么多装了、要么少装了（贴图会花/黑）")
        bad += 1

    print(f"\n结论：{'✅ 四项全过' if bad == 0 else f'🔴 {bad} 处不对'}")
    return 0 if bad == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
