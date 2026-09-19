# -*- coding: utf-8 -*-
"""从解包目录的材料 JSON 生成「材质名 → 原版 m_CustomRenderQueue」表。

为什么要它（2026-09-19 晚）：`EffectExporter` 原来记 `Material.renderQueue` —— 而那个值在
**材质没有 override** 时返回的是 **shader 的默认队列**，**只在 bundle 里的 shader** 在编辑器里
解析不出队列 ⇒ 返回兜底的 **2000（不透明队列）** ⇒ 粒子被当不透明排 ⇒ 看起来暗。

判据先后试过两版都不完备（`AssetDatabase.Contains` 恒为 true；`om.renderQueue != om.shader.renderQueue`
判不出「材质恰好 override 成与 shader 同值」）—— 而**解包里的 `m_CustomRenderQueue` 是原始真值**，
直接读它最干净。

产出：`数据/游戏数据/mat_renderqueue.json` = `{ 材质名: 队列值 }`，**只收 `!= -1` 的**
（−1 = 没 override ⇒ 导出器看到"表里没有"就回退到 shader 默认，行为本来就对）。

用法：`python 工具/gen_mat_renderqueue.py`
"""
import glob
import io
import json
import os
import sys
from collections import defaultdict

SRC = "D:/2/新解包资源/assets_full/*/Material/*.json"
OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
                   "数据/游戏数据/mat_renderqueue.json")


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")   # Windows 控制台默认 GBK，emoji 会炸
    except Exception:
        pass
    by_name = defaultdict(set)
    files = glob.glob(SRC)
    bad = 0
    for f in files:
        try:
            d = json.load(io.open(f, encoding="utf-8"))
        except Exception:
            bad += 1
            continue
        name = d.get("m_Name")
        q = d.get("m_CustomRenderQueue")
        if not name or q is None or q == -1:
            continue
        by_name[name].add(q)

    conflicts = {k: sorted(v) for k, v in by_name.items() if len(v) > 1}
    table = {k: sorted(v)[0] for k, v in by_name.items() if len(v) == 1}

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    payload = {
        "_note": ("材质名 → 原版 m_CustomRenderQueue（只收 != -1 的）。"
                  "由 工具/gen_mat_renderqueue.py 从 assets_full/*/Material/*.json 生成，别手改。"
                  "导出器：表里有就用它；没有就回退到 shader 默认（= 不记，行为本来就对）。"),
        "_conflicts": conflicts,
        "queue": table,
    }
    io.open(OUT, "w", encoding="utf-8", newline="\n").write(
        json.dumps(payload, ensure_ascii=False, indent=1))

    print("材质 JSON %d 个（读失败 %d）" % (len(files), bad))
    print("有 override 的唯一名 %d 个；同名多值 %d 个" % (len(table), len(conflicts)))
    for k, v in list(conflicts.items())[:8]:
        print("   ⚠️ %s -> %s" % (k, v))
    print("已落表：%s" % OUT)
    return 0


if __name__ == "__main__":
    sys.exit(main())
