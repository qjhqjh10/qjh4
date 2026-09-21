#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""在**所有原版 bundle** 里按**资产名**找一个东西（不看类型树，只读 container 路径）。

为什么要它：`UnityPy` 读 MonoBehaviour 时经常没有类型树（导出成看不懂的裸 dict），
但 **bundle 的 container 路径（资产名）是明文的** —— 「这个资产在哪个包、叫什么」先查它，
再决定要不要啃字节。先例：`VarsGlobal` 整表当年就是按名字找回来的。

用法：
    python 工具/find_asset_by_name.py <子串> [<子串2> ...]      # 大小写不敏感
    python 工具/find_asset_by_name.py --list-types              # 顺便统计每个包里有哪些类型

输出：`包名 ‖ 资产名`，一行一条；末尾给命中总数。
⚠️ 只读 container 名，**不加载对象** —— 所以很快（84 个包全扫几十秒）。
🔴 **原始 bundle 不在 `新解包资源/assets_full`**（那是**已经解包好的分目录**）——
   真包在 **`d:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/`**（168 个）。
   可用环境变量 `WF_BUNDLE_DIR` 换目录。
"""
import sys
import os
import UnityPy

BUNDLE_DIR = os.environ.get(
    "WF_BUNDLE_DIR", r"D:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64")


def main(argv):
    if not argv:
        print(__doc__)
        return 1
    pats = [p.lower() for p in argv]

    bundles = []
    for root, _dirs, files in os.walk(BUNDLE_DIR):
        for f in files:
            if f.endswith(".bundle") or f.endswith(".unity3d") or f.endswith(".assets"):
                bundles.append(os.path.join(root, f))
    print(f"# 扫 {len(bundles)} 个包")

    hits = 0
    for b in bundles:
        try:
            env = UnityPy.load(b)
        except Exception as e:
            print(f"[跳过] {os.path.basename(b)}: {e}")
            continue
        for name in list(env.container.keys()):
            low = name.lower()
            if any(p in low for p in pats):
                hits += 1
                print(f"{os.path.basename(b)}  ‖  {name}")
    print(f"# 命中 {hits} 条")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
