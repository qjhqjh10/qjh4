#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""_try_typetree.py — 试验：用 UnityPy 的 TypeTreeGenerator + 本机 IL2CPP 组装，
能不能把 AnimFX 组件里**嵌套的序列化类**也解析出来。

背景：`dump_animfx.py` 现在能读出 MonoBehaviour 自己的字段，但**嵌套类的字段全是
`<UnknownObject<...>>`**（实测 1919 处，正好是最值钱的 `cameraShakes` 508 · `collisionAndParticles` 450 ·
`sounds` 937）。这份脚本验一条正路：拿 `MelonLoader/Il2CppAssemblies/Assembly-CSharp.dll`
（IL2CPP 生成的**真字段布局**程序集）喂给 TypeTreeGenerator，看能不能补出完整的类型树。
"""
import glob
import io
import os
import sys

import UnityPy
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator

BUNDLE_DIR = r"d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64"
DLLS = [
    r"d:/2/unity_run_ref/MelonLoader/Il2CppAssemblies/Assembly-CSharp.dll",
    r"d:/2/unity_run_ref/MelonLoader/Il2CppAssemblies/Assembly-CSharp-firstpass.dll",
]

TARGETS = ["AnimFXModuleScreenShake", "AnimFXController", "AnimFXModuleCollisions"]


def main():
    files = sorted(glob.glob(os.path.join(BUNDLE_DIR, "*.bundle")))
    print(f"[1] 加载 {len(files)} 个 bundle …")
    env = UnityPy.load(*files)

    ver = None
    for f in env.files.values():
        for attr in ("unity_version", "version", "m_UnityVersion"):
            v = getattr(f, attr, None)
            if isinstance(v, str) and v and v[0].isdigit():
                ver = v
                break
        if ver:
            break
    if not ver:
        ver = "6000.0.0f1"          # 取不到就用 Unity 6 —— 类型树格式按大版本走，够用
    print(f"[2] unity_version = {ver}")

    gen = TypeTreeGenerator(ver)
    # ⚠️ 只 load 那一对 `Assembly-CSharp*.dll` 会失败（`failed to dump nodes raw` /
    #    `Object reference not set`）；整目录 load 157 个 dll 也还是失败。
    #    ⇒ 走**最可信的那条**：`load_local_game(游戏根目录)` —— 它直接读
    #    `GameAssembly.dll` + `global-metadata.dat`（IL2CPP 的真类型布局），
    #    不依赖那批壳 dll 的互相引用能不能拼齐。
    game_root = r"d:/2/unity_run_ref"
    ok = False
    try:
        gen.load_local_game(game_root)
        print(f"[3] load_local_game {game_root}")
        ok = True
    except Exception as e:
        print(f"[3] load_local_game 失败 {type(e).__name__}: {e}")
    if not ok:
        dll_dir = os.path.dirname(DLLS[0])
        gen.load_local_dll_folder(dll_dir)
        print(f"[3] 退回 load_local_dll_folder {dll_dir}")

    trees = {}
    for t in TARGETS:
        try:
            n = gen.get_nodes_up("Assembly-CSharp", t)
            trees[t] = n
            print(f"[4] 类型树 {t}: {len(n)} 个节点")
        except Exception as e:
            print(f"[4] 类型树 {t} 失败: {type(e).__name__}: {e}")

    # 找几个目标组件，用类型树重读
    hit = 0
    for o in env.objects:
        if o.type.name != "MonoBehaviour":
            continue
        raw = o.read()
        f = vars(raw)
        for cls, nodes in trees.items():
            sig = {"AnimFXModuleScreenShake": {"cameraShakes"},
                   "AnimFXController": {"sounds"},
                   "AnimFXModuleCollisions": {"collisionAndParticles"}}[cls]
            if not sig & set(f):
                continue
            print(f"\n[5] 命中 {cls}（path_id={o.path_id}）")
            for k in sig:
                print(f"    旧读法 {k} = {repr(f[k])[:130]}")
            try:
                d = o.read_typetree(nodes)
                print(f"    ★新读法 keys = {list(d.keys())[:8]}")
                for k in sig:
                    if k in d:
                        print(f"    ★新读法 {k} = {repr(d[k])[:400]}")
            except Exception as e:
                print(f"    ★新读法失败: {type(e).__name__}: {e}")
            hit += 1
            break
        if hit >= 3:
            break
    print(f"\n[6] 试了 {hit} 个")


if __name__ == "__main__":
    main()
