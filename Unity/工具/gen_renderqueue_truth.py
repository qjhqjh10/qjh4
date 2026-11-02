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

🔴 **同名多值（T06·残余，2026-11-03 定）**：同一个材质名在**不同包里**带不同 `m_CustomRenderQueue`
时，**不再取最小值** —— 那是在两个不同资产之间**静默挑一个**（旧行为，`mat_table[k] = sorted(v)[0]`）。
现在：
  · 该名字**不进 `mat` 表**（消费方 `EffectExporter.TruthQueue` 会自动退回 `shader[shader名]`，
    再没有就 −1 = 运行时用 shader 默认 —— 那是「不知道就别猜」的正确落点）；
  · 每个冲突**逐条出声**（打印全部取值 + 各取值来自哪个包的哪份 JSON），并写进
    `_conflicts`（名字 → 取值列表）与 `_conflict_sources`（名字 → [{queue, file}]）。
现读 2 条（`SandParticle` 3000/3001 · `Campaign Points` −1/3000）；两条的 shader 默认队列
**恰好都等于**原来取 min 之后的结果（`Campaign Points` shader = 3000；
`SandParticle` shader = `Universal Render Pipeline/Particles/Unlit` = Transparent）⇒ **本次改动对现有数据零副作用**，
只是不再把两个不同资产混成一个数。

用法：`python 工具/gen_renderqueue_truth.py`
      加 `--dry` = 只跑计算、打印读数、**零写盘**（默认行为不变；`A1648` 同族口径）。
"""
import glob
import io
import json
import os
import sys
from collections import defaultdict

# 🔴 `--dry` = 只跑计算、打印读数、**零写盘**。默认（不带它）行为与以前完全一样。
## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法：`python 工具/gen_renderqueue_truth.py`\n      加 `--dry` = 只跑计算、打印读数、**零写盘**（默认行为不变；`A1648` 同族口径）。\n  -h, --help  只打印本用法并退出（退出码 0），不执行任何真活。')
    _wf_help_sys.exit(0)

DRY = "--dry" in sys.argv[1:]

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

    recs = defaultdict(list)        # 材质名 -> [{queue, shader_pid, bundle, file}]
    for f in glob.glob(os.path.join(SRC, "*/Material/*.json")):
        try:
            d = json.load(io.open(f, encoding="utf-8"))
        except Exception:
            continue
        n = d.get("m_Name")
        q = d.get("m_CustomRenderQueue")
        if n and q is not None:
            recs[n].append({
                "queue": q,
                "shader_pid": (d.get("m_Shader") or {}).get("m_PathID"),
                "bundle": os.path.basename(os.path.dirname(os.path.dirname(f))),
                "file": os.path.relpath(f, SRC).replace(os.sep, "/"),
            })

    shaders = {}
    shader_by_pid = {}
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
        stem = os.path.splitext(os.path.basename(f))[0]
        if "_" in stem:
            try:
                shader_by_pid[int(stem.rsplit("_", 1)[-1])] = n
            except Exception:
                pass

    for n, lst in recs.items():
        for r in lst:
            sh = shader_by_pid.get(r["shader_pid"])
            r["shader"] = sh
            r["effective"] = r["queue"] if r["queue"] >= 0 else shaders.get(sh)

    conflicts = {}
    conflict_sources = {}
    mat_table = {}
    for n, lst in recs.items():
        raws = sorted(set(r["queue"] for r in lst))
        if len(raws) == 1:
            mat_table[n] = raws[0]
            continue
        # ---- 同名多值：先看「生效队列」是不是其实一致（-1 = 没有 override ⇒ 取 shader 的 QUEUE）----
        effs = sorted(set(r["effective"] for r in lst))
        eff_known = all(e is not None for e in effs)
        conflicts[n] = raws
        conflict_sources[n] = [{
            "queue": r["queue"],
            "effective": r["effective"],
            "shader": r["shader"],
            "bundle": r["bundle"],
            "file": r["file"],
        } for r in lst]
        if eff_known and len(effs) == 1 and effs[0] is not None and effs[0] >= 0:
            # 生效值其实相同（例：一条 -1 落到 shader QUEUE、另一条正好 override 成同一个数）
            mat_table[n] = effs[0]
        else:
            # 真歧义：`mat` 表按**名字**键，表达不了「两个不同资产」⇒ 保住旧行为（取非负最小值），
            # 但**不再静默**：每个冲突都打印 + 写进 `_conflicts` / `_conflict_sources`。
            nonneg = [q for q in raws if q >= 0]
            mat_table[n] = min(nonneg) if nonneg else raws[0]

    payload = {
        "_note": ("原版队列真值。生效队列 = mat[材质名] 若 >=0，否则 shader[shader名]。"
                  "由 工具/gen_renderqueue_truth.py 从 assets_full 的原始 JSON 生成，别手改。"),
        "_conflict_policy": ("同名多值不静默：① 若各份的**生效队列**（queue>=0 用它，否则 shader QUEUE）相同 ⇒ 直接用那个值；"
                             "② 真歧义 ⇒ mat 表取非负最小值（旧行为，消费方 EffectExporter 实际用的是这一份），"
                             "**并逐条打印 + 记录在 _conflicts / _conflict_sources 里**。"
                             "⚠️ 按名字键的 mat 表**结构上表达不了两个同名不同资产**；"
                             "要彻底区分得让消费方带 bundle 维度（未做，见报告）。"),
        "_conflicts": conflicts,
        "_conflict_sources": conflict_sources,
        "mat": mat_table,
        "shader": shaders,
    }
    mtsv = os.path.join(os.path.dirname(OUT), "mat_renderqueue.tsv")
    stsv = os.path.join(os.path.dirname(OUT), "shader_renderqueue.tsv")
    if DRY:
        print("干跑（未写盘）：%s · %s · %s" % (OUT, mtsv, stsv))
    else:
        os.makedirs(os.path.dirname(OUT), exist_ok=True)
        io.open(OUT, "w", encoding="utf-8", newline="\n").write(
            json.dumps(payload, ensure_ascii=False, indent=1))

        # 另外落两张 **TSV** —— `EffectExporter` 在 Unity 里读它们（JSON 两层字典不好用 JsonUtility 解）
        io.open(mtsv, "w", encoding="utf-8", newline="\n").write(
            "材质名\tm_CustomRenderQueue\n" + "\n".join("%s\t%d" % (k, v) for k, v in mat_table.items()))
        io.open(stsv, "w", encoding="utf-8", newline="\n").write(
            "shader名\tQUEUE\n" + "\n".join("%s\t%d" % (k, v) for k, v in shaders.items() if v is not None))
    print("材质 %d 个（同名多值 %d）· shader %d 个（有 QUEUE 的 %d）"
          % (len(mat_table), len(conflicts), len(shaders),
             sum(1 for v in shaders.values() if v is not None)))
    for k in sorted(conflicts):
        print("   ⚠️ 同名多值 `%s`：raw=%s ⇒ mat 表取 %s" % (k, conflicts[k], mat_table.get(k)))
        for s in conflict_sources.get(k, []):
            print("        queue=%-5s effective=%-5s shader=%r  ← %s"
                  % (s["queue"], s["effective"], s["shader"], s["file"]))
        print("        （同名两份是**两个不同资产**；按名字键的 mat 表表达不了，⛔ 别当它已区分）")
    if not DRY:
        print("已落表：%s" % OUT)
    return 0


if __name__ == "__main__":
    sys.exit(main())
