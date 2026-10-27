# -*- coding: utf-8 -*-
"""import_mainmenu_effect_mats.py —— 把「卡片展示窗」那 4 张粒子材质搬进 `Resources/`（运行时取得到）。

为什么需要它（界面线 R2 续 · 2026-10-27 B1UI）：
  原版的 10 颗粒子（`Swirl` / `Glow Explosion Up/R/Down` / `Ray Right` / `Sparks R/RL`）用的 4 张材质
  **只在 bundle 里**（`AirSharpSwirl` · `Glow Rays UI` · `Lightning Random UI` · `Spark Additive Extra Color`），
  而 `Resources.Load` 只能读 `Resources/` 下的资产 ⇒ 不搬这一趟，粒子只能画成白方块。
  **特效线已经把同名材质重新建成了工程资产**（`Assets/WarpforgeVFX/Materials/`，shader = 我们自己复原的 `.shader`），
  所以这里**只做拷贝**（⛔ 不新建材质、⛔ 不用第三方近似图顶，铁律 3）。

判据（逐条可复查）：
  · 原版材质名 = `bundle_duplicateassetisolation_assets_all/Material/Material_{…}.json` 的 `m_Name`
    （`-3062488692366390093` AirSharpSwirl · `4206261991850403090` Glow Rays UI ·
     `-6937851114935498264` Lightning Random UI · `-8031580538284717323` Spark Additive Extra Color）
  · 混合状态（原版 vs 工程那份，逐值相同 ⇒ 是同一档）：
      `Lightning Random UI`：原版 `_Blend 2 / _SrcBlend 5 / _DstBlend 1` == 工程 `Lightning Random add.mat` 同值
      （⚠️ **工程里没有叫 `Lightning Random UI.mat` 的资产** —— 特效线按混合档命名成 `add` / `blend`）
  · 贴图不搬：那几张 `.mat` 里的 `_MainTex` 指向 `Assets/WarpforgeVFX/Textures/*.png`（guid 仍有效）

用法（**默认只打印用法，不写盘**）：
  PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/import_mainmenu_effect_mats.py --apply
  -h / --help   只打印用法并退出（退出码 0）

产物：`d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/vfx/<原版材质名>.mat`（4 个，字节级拷贝，重跑幂等）
  ⚠️ `.meta` 由 Unity 生成（第一次导入前不存在是正常的）。
"""
import io
import os
import sys

SRC = r"d:/4/Unity/MyGame/Assets/WarpforgeVFX/Materials"
OUT = r"d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/vfx"

# 原版材质名 -> 工程里那份源文件（见文件头「判据」）
MAP = {
    "AirSharpSwirl": "AirSharpSwirl.mat",
    "Glow Rays UI": "Glow Rays UI.mat",
    "Spark Additive Extra Color": "Spark Additive Extra Color.mat",
    "Lightning Random UI": "Lightning Random add.mat",
}

USAGE = """用法（默认不写盘）：
  PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe \\
      d:/4/Unity/工具/import_mainmenu_effect_mats.py --apply
  -h / --help   只打印本用法并退出（退出码 0）

做的事：把 4 张原版粒子材质从 `Assets/WarpforgeVFX/Materials/` 拷进
        `Assets/CardPresentation/Resources/Art/vfx/`（按**原版材质名**命名，运行时 `Resources.Load<Material>` 取得到）。
"""


def main(apply):
    rows = []
    for name, src_name in MAP.items():
        src = os.path.join(SRC, src_name)
        dst = os.path.join(OUT, name + ".mat")
        if not os.path.isfile(src):
            rows.append((name, src_name, "! 源不存在"))
            continue
        data = io.open(src, "rb").read()
        existed = os.path.isfile(dst)
        if existed and io.open(dst, "rb").read() == data:
            rows.append((name, src_name, "same"))
            continue
        if apply:
            os.makedirs(OUT, exist_ok=True)
            io.open(dst, "wb").write(data)
            rows.append((name, src_name, "diff" if existed else "new"))
        else:
            rows.append((name, src_name, "would-copy"))
    for name, src_name, st in rows:
        print("  %-30s <- %-32s %s" % (name, src_name, st))
    if not apply:
        print("\n要真写盘请加 --apply")
    return 0


if __name__ == "__main__":
    argv = sys.argv[1:]
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    if any(a in ("-h", "--help") for a in argv):
        print(USAGE)
        sys.exit(0)
    if "--apply" not in argv:
        print(USAGE)
        main(False)
        print("（本次未带 --apply ⇒ 什么都没做）")
        sys.exit(0)
    sys.exit(main(True))
