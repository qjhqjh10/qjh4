#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""列出一个原版 bundle 里**所有对象的 PathID + 类型 + 名字**（或按 PathID 反查名字）。

为什么要它：解包目录是按**名字**存文件的，而场景/资产之间是**按 PathID 引用**的
（`{"m_FileID": 0, "m_PathID": 123...}`）—— **PathID → 名字**这一步只有读原始包才有。
先例：`资料/反编译工具链_重建记录.md` 里 cue 的 `clipList` 存 PathID 那次。

用法：
    python 工具/bundle_dump_objects.py <包名或路径> [--pat <子串>] [--type <类型名>] [--limit N]
    python 工具/bundle_dump_objects.py battlesharedresources --pat 8327771005907127376

🔴 真包在 `d:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/`（168 个），
   包名可以不写 `.bundle` 后缀；也可用环境变量 `WF_BUNDLE_DIR` 换目录。
"""
import argparse
import os
import UnityPy

BUNDLE_DIR = os.environ.get(
    "WF_BUNDLE_DIR", r"D:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64")


def resolve(name):
    if os.path.exists(name):
        return name
    for cand in (name, name + ".bundle"):
        p = os.path.join(BUNDLE_DIR, cand)
        if os.path.exists(p):
            return p
    raise SystemExit(f"找不到包：{name}（在 {BUNDLE_DIR}）")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("bundle")
    ap.add_argument("--pat", default=None, help="只列 PathID 或名字里含这个子串的")
    ap.add_argument("--type", default=None, help="只列这个类型（子串匹配，大小写不敏感）")
    ap.add_argument("--limit", type=int, default=0)
    a = ap.parse_args()

    env = UnityPy.load(resolve(a.bundle))
    n = 0
    for obj in env.objects:
        try:
            tname = obj.type.name
        except Exception:
            tname = "?"
        if a.type and a.type.lower() not in tname.lower():
            continue
        name = ""
        try:
            d = obj.read()
            name = getattr(d, "m_Name", "") or ""
        except Exception:
            pass
        line = f"{obj.path_id:>22}  {tname:<24} {name}"
        if a.pat and a.pat.lower() not in line.lower():
            continue
        print(line)
        n += 1
        if a.limit and n >= a.limit:
            break
    print(f"# 共列出 {n} 条")


if __name__ == "__main__":
    main()
