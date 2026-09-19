# -*- coding: utf-8 -*-
"""建两张「队列真值」表，供 `EffectExporter` 算**原版材质的生效渲染队列**。

**为什么要它**（2026-09-19 晚）：导出器原来记 `Material.renderQueue` —— 而那个值在
**材质没有 override** 时返回的是 **shader 的默认队列**，**只在 bundle 里的 shader** 在编辑器里
解析不出队列 ⇒ 返回兜底的 **2000**（不透明队列）⇒ 粒子被当不透明排 ⇒ 看起来暗/亮。
试过的两版判据都不完备（`AssetDatabase.Contains` 恒 true；`om.renderQueue != om.shader.renderQueue`
判不出「材质恰好 override 成与 shader 同值」）⇒ **直接读原始 JSON 的真值**。

**生效队列的算法**（`m_CustomRenderQueue >= 0` 就是它，否则取 shader 的 SubShader QUEUE）：

| shader | SubShader QUEUE | = 数值 |
|---|---|---|
| `Shader Graphs/Fx_RockDissolve` | `AlphaTest` | **2450** ← 与实测原版一致 |
| `Everguild/FX/Extra Color` / `Spiral Trail FX` | `Transparent` | 3000 |
| （`Geometry` / `Background` / `AlphaTest` / `Transparent` / `Overlay`） | | 2000 / 1000 / 2450 / 3000 / 4000 |

产出：`数据/游戏数据/renderqueue_truth.json` =
`{ "mat": {材质名: m_CustomRenderQueue}, "shader": {shader名: QUEUE数值}, "_conflicts": {...} }`

用法：`python 工具/gen_renderqueue_truth.py`
"""
import glob
import io
import json
import os
import sys
from collections import defaultdict

SRC = "D:/2/新解包资源/assets_full"
OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
                   "数据/游戏数据/renderqueue_truth.json")

NAMED = {"Background": 1000, "Geometry": 2000, "AlphaTest": 2450,
         "Transparent": 3000, "Overlay": 4000}


def queue_num(name):
    if not name:
        return None
    if isinstance(name, str) and name.startswith("+"):
        name = name[1:]
    if isinstance(name, str) and name in NAMED:
        return NAMED[name]
    try:
        return int(name)
    except Exception:
        return None


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass

    mats = defaultdict(set)
    for f in glob.glob(os.path.join(SRC, "*/Material/*.json")):
        try:
            d = json.load(io.open(f, encoding="utf-8"))
        except Exception:
            continue
        n = d.get("m_Name")
        q = d.get("m_CustomRenderQueue")
        if n and q is not None:
            mats[n].add(q)

    shaders = {}
    for f in glob.glob(os.path.join(SRC, "*/Shader/*.json")):
        try:
            d = json.load(io.open(f, encoding="utf-8"))
        except Exception:
            continue
        pf = d.get("m_ParsedForm") or {}
        n = pf.get("m_Name")
        if not n:
            continue
        subs = pf.get("m_SubShaders") or []
        if not subs:
            continue
        raw = subs[0].get("m_Tags") or {}
        # ⚠️ 这个导出器把 tags 存成 {"tags": [[k, v], ...]}，不是字典
        qv = None
        tl = raw.get("tags") if isinstance(raw, dict) else None
        if isinstance(tl, list):
            for kv in tl:
                if len(kv) == 2 and kv[0] == "QUEUE":
                    qv = queue_num(kv[1])
        shaders[n] = qv

    conflicts = {k: sorted(v) for k, v in mats.items() if len(v) > 1}
    mat_table = {k: sorted(v)[0] for k, v in mats.items() if len(v) == 1}
    for k, v in conflicts.items():          # 冲突的取最小的（并记进 _conflicts）
        mat_table[k] = v[0]

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    payload = {
        "_note": ("原版队列真值。生效队列 = mat[材质名] 若 >=0，否则 shader[shader名]。"
                  "由 工具/gen_renderqueue_truth.py 从 assets_full 的原始 JSON 生成，别手改。"),
        "_conflicts": conflicts,
        "mat": mat_table,
        "shader": shaders,
    }
    io.open(OUT, "w", encoding="utf-8", newline="\n").write(
        json.dumps(payload, ensure_ascii=False, indent=1))

    # 另外落两张 **TSV** —— `EffectExporter` 在 Unity 里读它们（JSON 两层字典不好用 JsonUtility 解）
    mtsv = os.path.join(os.path.dirname(OUT), "mat_renderqueue.tsv")
    stsv = os.path.join(os.path.dirname(OUT), "shader_renderqueue.tsv")
    io.open(mtsv, "w", encoding="utf-8", newline="\n").write(
        "材质名\tm_CustomRenderQueue\n" + "\n".join("%s\t%d" % (k, v) for k, v in mat_table.items()))
    io.open(stsv, "w", encoding="utf-8", newline="\n").write(
        "shader名\tQUEUE\n" + "\n".join("%s\t%d" % (k, v) for k, v in shaders.items() if v is not None))
    print("材质 %d 个（同名多值 %d）· shader %d 个（有 QUEUE 的 %d）"
          % (len(mat_table), len(conflicts), len(shaders),
             sum(1 for v in shaders.values() if v is not None)))
    for k, v in list(conflicts.items())[:8]:
        print("   ⚠️ 同名多值 %s -> %s" % (k, v))
    print("已落表：%s" % OUT)
    return 0


if __name__ == "__main__":
    sys.exit(main())
