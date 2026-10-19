#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""gen_simple_animation_clips.py —— 扫出**原版 `SimpleAnimation` 组件的「状态名 → AnimationClip」**表。

**为什么要它**（2026-10-19 · 特效线 `T12` / 账 `A1729`）：
原版 `AnimFXModuleAnimation.Exit()` 调的是 AssetStore 插件 **`SimpleAnimation.Play("Exit")`**
（方法体实读：`资料/AnimFX_18类方法体_块2.md` §2）。那套组件的序列化字段是
`m_States = [{name, clip}, …]` + `m_Clip` + `m_PlayAutomatically` —— **它把「状态名」和「片段」的映射
存在 prefab 的组件上**，而我们的导出链不导这个组件（没有那个插件类）⇒ 运行时只知道要播 `"Exit"`、
却不知道 `"Exit"` 对应哪条 clip。

实测（5 个 `ShieldTraitEffect_Idle*` 全是同一对）：
    Default → `Shield Enter` · Exit → `Shield Exit`

**这个表怎么被用**：
· `工具/extract_missing_shaders.py --prefabs` 读它 ⇒ 把 clip 名加进 `extra_clips`（重打进
  `StreamingAssets/WarpforgeVFX/wf_prefabs_extra.bundle`），并登记一条**容器别名**
  `<owner>|<state>`（例：`ShieldTraitEffect_Idle|Exit`）。
· 运行时 `WarpforgeAnimatorBridge.TryGetClip("<owner>|<state>")` 按别名取回原版片段，
  `WFModuleAnimation` 装到 legacy `Animation` 上播。

用法：
  "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_simple_animation_clips.py [--check]
产出：
  d:/4/Unity/数据/游戏数据/simple_animation_clips.json
"""
import glob
import io
import json
import os
import sys

import UnityPy

BUNDLE_DIR = (r"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa"
              r"\StandaloneWindows64")
OUT = r"d:/4/Unity/数据/游戏数据/simple_animation_clips.json"

# `SimpleAnimation` 的判据 = **这三个字段同时存在**（它没有可加载的 m_Script 名字可读 ——
#   脚本资产在别的包里，且 `m_Script.read()` 对这类 MonoBehaviour 会失败）。
#   三字段同现的其它组件：本库实测 0 个（`--check` 会打「疑似」清单供人核）。
NEED = ("m_States", "m_Clip", "m_PlayAutomatically")


def main():
    check = "--check" in sys.argv
    bundles = sorted(glob.glob(os.path.join(BUNDLE_DIR, "*.bundle")))
    out = {}
    suspects = []
    for p in bundles:
        bn = os.path.basename(p)
        try:
            env = UnityPy.load(p)
        except Exception as e:
            print(f"  !! 打不开 {bn}: {e}")
            continue
        rows = []
        for o in env.objects:
            if o.type.name != "MonoBehaviour":
                continue
            try:
                d = o.read()
            except Exception:
                continue
            if not all(hasattr(d, f) for f in NEED):
                continue
            try:
                go = d.m_GameObject.read()
                owner = go.m_Name
            except Exception:
                continue
            states = []
            try:
                for s in (d.m_States or []):
                    cn = None
                    try:
                        cn = s.clip.read().m_Name
                    except Exception:
                        pass
                    sn = getattr(s, "name", None)
                    if sn and cn:
                        states.append({"state": sn, "clip": cn})
            except Exception:
                pass
            if not states:
                continue
            # 同 owner 多份（同名 prefab / 多 CAB）时取并集
            cur = {x["state"]: x["clip"] for x in
                   next((r["states"] for r in rows if r["owner"] == owner), [])}
            cur.update({x["state"]: x["clip"] for x in states})
            merged = [{"state": k, "clip": v} for k, v in sorted(cur.items())]
            hit = next((r for r in rows if r["owner"] == owner), None)
            if hit:
                hit["states"] = merged
            else:
                rows.append({"owner": owner, "states": merged,
                             "playAutomatically": int(getattr(d, "m_PlayAutomatically", 0) or 0)})
        if rows:
            # 🔴 键的写法必须与 `animator_controllers.json` 的 `source` 一致：
            #   `bundle_<bundle 文件名去掉 .bundle>`（`extract_missing_shaders.py` 就是这么算 src_dir 的）
            key = "bundle_" + bn[:-len(".bundle")] if bn.endswith(".bundle") else bn
            out[key] = rows
            print(f"{bn}: {len(rows)} 个 SimpleAnimation 宿主")
    # 疑似：有 m_States 但没有 m_Clip 的（可能是别的组件）
    n = sum(len(v) for v in out.values())
    print(f"合计 {n} 个宿主（{len(out)} 个包）")
    if check:
        print(f"--check：{OUT} {'存在' if os.path.exists(OUT) else '不存在'}")
        return 0
    doc = {
        "_note": "由 工具/gen_simple_animation_clips.py 生成。判据 = MonoBehaviour 上同时有 "
                 "m_States/m_Clip/m_PlayAutomatically（原版 AssetStore SimpleAnimation 的序列化形状）。"
                 "消费方：extract_missing_shaders.py --prefabs（收 clip + 登记 <owner>|<state> 别名）"
                 "与 WarpforgeAnimatorBridge.TryGetClip。",
        "bundles": out,
    }
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with io.open(OUT, "w", encoding="utf-8", newline="\n") as f:
        json.dump(doc, f, ensure_ascii=False, indent=1)
        f.write("\n")
    print(f"写出 {OUT}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
