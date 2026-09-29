# -*- coding: utf-8 -*-
"""import_offensive_faces.py —— 把 13 阵营的**进攻卡卡面插画**导进工程（§25 要用）

数据源：`d:/4/Unity/数据/游戏数据/offensive_cards.json`（由 `工具/gen_offensive_cards.py` 从解包资源生成）
产物：`d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/offensive/<键>.png`
  · 键 = `<army>_c<槽号>`（0/1/2 = 三张进攻卡）· `<army>_empty`（「不使用进攻卡」那张）
  · 命名**只用 ASCII**（工程里 `Resources` 路径历史上踩过非 ASCII 的坑）

⚠️ **这是插画、不是拼好的卡**：解包侧那批 PNG 是**纯插画**（原版 1024²），
   拼卡要另行组装（卡框/卡名/效果文字）—— 效果文字我们**没有**（进攻卡不在 `cards_engine.json` 里）。

用法：
  PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/import_offensive_faces.py
只读 `d:/2/`，只写上面那个目录；重跑幂等（同名文件按内容比对，一样就跳过）。
"""
import io, json, os, re, shutil, sys

ASSETS = "d:/2/新解包资源/assets_full"
DATA = "d:/4/Unity/数据/游戏数据/offensive_cards.json"
OUT = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/offensive"


def sanitize(s):
    """只留 ASCII 字母数字，其余压下划线（原版贴图名里有空格/撇号/点）。"""
    s = re.sub(r"[^A-Za-z0-9]+", "_", s or "")
    return s.strip("_")


def find_png(bundle, name):
    """在解包资源的 <bundle>/Texture2D/ 下按名字找那张 PNG。

    ⚠️ `offensive_cards.json` 里的 `face_texture_bundle` 是**短名**（如 `spacemarinesultramarinescardassets`），
       而解包目录是 **`bundle_<短名>_assets_all`** ⇒ 三种形态都要试（2026-09-29 实测：
       只拼 `bundle_<短名>` 会 52 条全部落空）。
    """
    if not bundle or not name:
        return None
    for pre in ("bundle_" + bundle + "_assets_all", "bundle_" + bundle, bundle):
        d = os.path.join(ASSETS, pre, "Texture2D")
        if not os.path.isdir(d):
            continue
        for fn in (name + ".png", sanitize(name) + ".png"):
            p = os.path.join(d, fn)
            if os.path.isfile(p):
                return p
    return None


def copy(src, dst):
    """按内容比对再拷（重跑幂等）。返回 'new' / 'same' / 'diff'。"""
    if os.path.isfile(dst):
        a = io.open(src, "rb").read()
        b = io.open(dst, "rb").read()
        if a == b:
            return "same"
        io.open(dst, "wb").write(a)
        return "diff"
    shutil.copyfile(src, dst)
    return "new"


def main():
    d = json.load(io.open(DATA, encoding="utf-8"))
    armies = d["armies"]
    os.makedirs(OUT, exist_ok=True)

    n_new = n_same = n_diff = 0
    miss = []
    for army, a in sorted(armies.items()):
        # 三张进攻卡
        for i, c in enumerate(a.get("cards") or []):
            tex, bun = c.get("face_texture"), c.get("face_texture_bundle")
            dst = os.path.join(OUT, "%s_c%d.png" % (sanitize(army), i))
            src = find_png(bun, tex) if tex else None
            if src is None:
                miss.append("%s 槽%d（%s / %s）" % (army, i, tex, bun))
                continue
            r = copy(src, dst)
            n_new += r == "new"; n_same += r == "same"; n_diff += r == "diff"
        # 「不使用进攻卡」那张
        e = a.get("emptyOffensiveCard") or {}
        tex, bun = e.get("face_texture"), e.get("face_texture_bundle")
        dst = os.path.join(OUT, "%s_empty.png" % sanitize(army))
        src = find_png(bun, tex) if tex else None
        if src is None:
            miss.append("%s 空卡（%s / %s）" % (army, tex, bun))
        else:
            r = copy(src, dst)
            n_new += r == "new"; n_same += r == "same"; n_diff += r == "diff"

    print("OFF 导入目录 %s" % OUT)
    print("OFF 新增 %d · 覆盖 %d · 内容变过 %d" % (n_new, n_diff, n_same))
    if miss:
        print("OFF **没导出来的 %d 条**（如实报，不静默）：" % len(miss))
        for m in miss:
            print("OFF   " + m)
    else:
        print("OFF 13 阵营 × (3 + 1) 全部导到")


if __name__ == "__main__":
    main()
