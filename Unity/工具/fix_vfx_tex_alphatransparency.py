#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""fix_vfx_tex_alphatransparency.py —— 把特效贴图 `.meta` 里的 `alphaIsTransparency` 关掉。

**为什么**（2026-10-25 · X15）：
  `alphaIsTransparency: 1` 会让 Unity 在**导入时**把透明区的 RGB 用邻近不透明像素的颜色填上
  （alpha dilation，为了规避 mip/双线性滤波时的黑边）。原版这批特效贴图**没有**这个填充
  ⇒ 我们的 mip 链与原件不同，**小尺寸（采样到 mip）时轮廓/描边的颜色就错了**。
  本仓早就踩过同一件事（卡面插图那批：`工具/import_original_art.py` 的 `fix_art_meta()` ——
  「不关的话透明区会被 Unity 填成马赛克」），只是 `WarpforgeVFX/Textures/` 这批没做。

**判据（2026-10-25 实测，可复核）**：`Environmental Condition Dark Angels Asteroid Zone` 槽 #0
  `Asteroids (3)`（材质 `Asteroids` / `_BaseMap = Asteroid_sheet_2`）：
  · 该 PNG 有 **74.6% 的像素 alpha==0，且透明区 RGB≈(222,228,231)（亮灰）**，而不透明区是 (48,88,110)（暗蓝）。
  · 原件（bundle）的 mip 链 = **按 alpha 无权的普通盒式平均**（模拟值 295.07e6 vs
    `EffectIso` 读到的原件指纹 297.26e6，差 0.7%）⇒ **原版没做 dilation**。
  · 我们这一侧 `EffectIso` 读到的指纹 = **147.13e6**（≈ 把透明区换成了暗蓝后的值）⇒ **做了 dilation**。
  · 渲出来对得上的证据：原版那颗小行星**描边是亮灰**（(79,72,68)/(68,68,66)…），
    我们的是**暗蓝**（(27,37,41)/(25,35,38)…）；内部像素逐点相同 ⇒ 差的正是透明区 RGB 的那一跳。

**幂等**：只把 `alphaIsTransparency: 1` 这一行换成 `0`，**行尾逐字节照旧**（LF/CRLF 各自保留）。

用法（在 d:/4/Unity 下）：
  python -X utf8 工具/fix_vfx_tex_alphatransparency.py                      # 只列（dry）
  python -X utf8 工具/fix_vfx_tex_alphatransparency.py --only Asteroid_sheet_2 --apply
  python -X utf8 工具/fix_vfx_tex_alphatransparency.py --dir MyGame/Assets/WarpforgeVFX/Textures --apply
  python -X utf8 工具/fix_vfx_tex_alphatransparency.py --dir MyGame/Assets/WarpforgeVFX/ArenaOriginalPrefabs --apply   # 递归，13 个场的 Texture2D/ 一次覆盖
  python -X utf8 工具/fix_vfx_tex_alphatransparency.py --only Asteroid_sheet_2 --check   # 0=已经干净 1=还开着
"""
import argparse
import glob
import io
import os
import sys

## 与其它 `工具/*.py` 同一条规矩：-h/--help 只打用法、退出码 0，不干真活。
if any(a in ("-h", "--help") for a in sys.argv[1:]):
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print(__doc__)
    sys.exit(0)

HERE = os.path.dirname(os.path.abspath(__file__))
DEFAULT_DIR = os.path.join(HERE, "..", "MyGame", "Assets", "WarpforgeVFX", "Textures")

ON = "alphaIsTransparency: 1"
OFF = "alphaIsTransparency: 0"


def targets(d, only):
    """🔴 2026-10-25（第三轮）：改成**递归**（`**`）—— `ArenaOriginalPrefabs/<场>/Texture2D/` 是 13 层子目录，
    要一次覆盖 53 张。`Textures/` 没有子目录 ⇒ 它的行为逐字不变。"""
    out = []
    for p in sorted(glob.glob(os.path.join(d, "**", "*.png.meta"), recursive=True)):
        if only and only.lower() not in os.path.basename(p).lower():
            continue
        out.append(p)
    return out


def main():
    ap = argparse.ArgumentParser(add_help=False)
    ap.add_argument("--dir", default=DEFAULT_DIR)
    ap.add_argument("--only", default=None, help="只处理文件名里含这个片段的贴图（大小写不敏感）")
    ap.add_argument("--apply", action="store_true", help="真写盘（默认只列）")
    ap.add_argument("--check", action="store_true", help="有还开着的一律退出码 1（CI/自检用）")
    a = ap.parse_args()

    files = targets(a.dir, a.only)
    if not files:
        print("!! 没找到 .png.meta：%s" % a.dir)
        return 2

    todo, already = [], []
    for p in files:
        b = open(p, "rb").read()          # ⚠️ 必须二进制：文本模式会把 CRLF 折成 LF（本仓踩过）
        (already if ON.encode() not in b else todo).append(p)

    print("目录 %s：%d 个 .png.meta —— 还开着 %d 个 · 已经是关的 %d 个"
          % (os.path.relpath(a.dir, os.path.join(HERE, "..")), len(files), len(todo), len(already)))
    for p in todo:
        print("   %s  %s → %s%s" % ("(将改)" if a.apply else "(待改)", os.path.basename(p), OFF,
                                    "" if a.apply else "   ← 加 --apply 才写"))

    if a.apply:
        for p in todo:
            b = open(p, "rb").read()
            b2 = b.replace(ON.encode(), OFF.encode(), 1)
            assert b2 != b and b2.count(ON.encode()) == 0
            # 先算好再写（⛔ 不要 `open(...,'wb').write(<表达式>)` —— 表达式抛异常会把文件截成 0 字节）
            open(p, "wb").write(b2)
        print("已写 %d 个" % len(todo))

    if a.check:
        return 0 if not todo else 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
