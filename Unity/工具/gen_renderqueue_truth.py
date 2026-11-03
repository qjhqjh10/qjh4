# -*- coding: utf-8 -*-
"""建「队列真值」表（含 **bundle 维度**），供 `EffectExporter` 算**原版材质的生效渲染队列**。

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

产出（**四个文件**）：
  · `数据/游戏数据/renderqueue_truth.json` =
    `{ "mat": {材质名: m_CustomRenderQueue}, "mat_by_bundle": {bundle: {材质名: 队列}},
       "shader": {shader名: QUEUE数值}, "_conflicts": {...}, ... }`
  · `mat_renderqueue.tsv`（2 列 `材质名\tm_CustomRenderQueue`）
  · `shader_renderqueue.tsv`（2 列 `shader名\tQUEUE`）
  · `mat_renderqueue_by_bundle.tsv`（3 列 `bundle\t材质名\tm_CustomRenderQueue`）——
    🆕 2026-11-03（批 `B123` · 账 `B121-BF-02`）：按名字键的 `mat` 表**结构上表达不了
    「两个同名不同资产」**，所以要给消费方（`EffectExporter.TruthQueue`）一条带 bundle 的精确路。
    `bundle` 列 = **去掉 `bundle_` 前缀的 `assets_full` 包目录名**（`bundle_battleprefabs_vfxandmisc_assets_all`
    → `battleprefabs_vfxandmisc_assets_all`），与运行时 `xxx.bundle` 的文件名词干一致。

⚠️ **同名多值的现行算法（照代码，⛔ 不是下面那段旧描述）** —— 2026-11-03 就地对齐（旧 docstring 与代码相反）：
  1. 若同名各份的**生效队列**（`queue >= 0` 用它，否则 shader QUEUE）相同 ⇒ 用那个生效值；
  2. **真歧义** ⇒ `mat` 表**仍取非负最小值**（`min(nonneg)`，两条都没有非负值时取 `raws[0]`）——
     这是**旧行为、保留**，因为运行时 `WarpforgeEffectBinder.cs:573` 用 `renderQueue = -1`
     **不覆盖**（落回 shader 默认），所以「不知道就别猜」的落点是 −1，而旧消费方还要一个数；
     **同时**逐条打印 + 写进 `_conflicts` / `_conflict_sources`，**不再静默**。
  3. **要区分两个同名不同资产** ⇒ 读 **`mat_by_bundle` / `mat_renderqueue_by_bundle.tsv`**。
现读 2 条冲突（`SandParticle` 3000/3001 · `Campaign Points` −1/3000）。
⚠️ **对旧描述的订正（保留痕迹，铁律 5）**：本文件原来写「同名多值**不再取最小值**、该名字**不进 `mat` 表**」
   —— 那是一版**没有落地/被撤回**的改法；`resolve_name_conflict()` 里的 `min(nonneg)` 仍在跑。
   原来还写「`SandParticle` shader = Transparent」—— **也不对**：工程里 URP
   `Universal Render Pipeline/Particles/Unlit` 的 SubShader **没有 `Queue` tag**（已核 URP 包内
   `ParticlesUnlit.shader`）⇒ Unity 默认 **2000（Geometry）**，不是 3000。
   ⛔ 别照旧描述去「实现不取最小值」—— 那会让 `SandParticle` 这类退回 2000，实打实回归。

🔴 **写盘规则（CLAUDE.md §三 2026-10-26 那条）**：**不带显式动作 = 只打用法、退出 2、一个字节都不写**。
    `--dry` = 只算 + 打印读数（零写盘）· `--check` = 与盘上四个文件逐字节比（一致 rc=0 / 过期 rc=1）
    · `--write` = **唯一会写盘的档**。

用法：
    python -I -X utf8 d:/4/Unity/工具/gen_renderqueue_truth.py --dry
    python -I -X utf8 d:/4/Unity/工具/gen_renderqueue_truth.py --check
    python -I -X utf8 d:/4/Unity/工具/gen_renderqueue_truth.py --write
"""
import glob
import io
import json
import os
import sys
from collections import defaultdict

USAGE = """用法：写入 / 比对「队列真值」四件（默认不写盘）
  python -I -X utf8 d:/4/Unity/工具/gen_renderqueue_truth.py --dry    只跑计算、打印读数，**零写盘**
  python -I -X utf8 d:/4/Unity/工具/gen_renderqueue_truth.py --check  与盘上四个文件逐字节比（一致 rc=0 / 过期 rc=1）
  python -I -X utf8 d:/4/Unity/工具/gen_renderqueue_truth.py --write  **唯一会写盘的档**
  -h, --help  只打印本用法并退出（退出码 0）
⛔ 一个动作档都不给（或不认识）= 只打印本用法并退出 2，不计算、不写盘。"""

# A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print(USAGE)
    _wf_help_sys.exit(0)

ACTIONS = ("--dry", "--check", "--write")
_picked = [a for a in sys.argv[1:] if a in ACTIONS]
if len(_picked) != 1:
    print(USAGE)
    sys.exit(2)
MODE = _picked[0]

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


def bundle_key(dirname):
    """`assets_full` 的包目录名 → 与运行时包文件名词干一致的 key。

    `bundle_battleprefabs_vfxandmisc_assets_all` → `battleprefabs_vfxandmisc_assets_all`
    （运行时包文件名 = `battleprefabs_vfxandmisc_assets_all.bundle`）。
    消费方 `EffectExporter.NormalizeBundleKey` 做同一个归一。
    """
    if not dirname:
        return ""
    k = dirname
    if k.startswith("bundle_"):
        k = k[len("bundle_"):]
    return k


def resolve_name_conflict(lst):
    """同名多值的现行算法（见文件头 ⚠️ 那段）：先看生效队列是否一致，否则取非负最小值。

    返回 `(value, raws, true_ambiguity)`；`raws` 长度 > 1 就是「同名多值」（照旧记进 `_conflicts`，
    即使各份**生效队列**其实一致 —— 那是 `_conflicts` 的既有形状，⛔ 别改）。
    """
    raws = sorted(set(r["queue"] for r in lst))
    if len(raws) == 1:
        return raws[0], raws, False
    effs = sorted(set(r["effective"] for r in lst))
    eff_known = all(e is not None for e in effs)
    if eff_known and len(effs) == 1 and effs[0] is not None and effs[0] >= 0:
        return effs[0], raws, False
    nonneg = [q for q in raws if q >= 0]
    return (min(nonneg) if nonneg else raws[0]), raws, True


def read_text(path):
    try:
        return io.open(path, encoding="utf-8", newline="").read()
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
    ambiguous = {}
    for n, lst in recs.items():
        v, raws, is_ambiguous = resolve_name_conflict(lst)
        mat_table[n] = v
        if len(raws) == 1:
            continue
        conflicts[n] = raws
        ambiguous[n] = is_ambiguous
        conflict_sources[n] = [{
            "queue": r["queue"],
            "effective": r["effective"],
            "shader": r["shader"],
            "bundle": r["bundle"],
            "file": r["file"],
        } for r in lst]

    # ---- 🆕 2026-11-03（`B121-BF-02`）：**按 bundle 的材质表**（同一材质名在不同包里可以有不同队列）----
    by_bundle_raw = defaultdict(lambda: defaultdict(list))
    for n, lst in recs.items():
        for r in lst:
            by_bundle_raw[r["bundle"]][n].append(r)
    mat_by_bundle = {}
    for b, mats in sorted(by_bundle_raw.items()):
        m = {}
        for n, lst in mats.items():
            m[n] = resolve_name_conflict(lst)[0]
        mat_by_bundle[bundle_key(b)] = m

    payload = {
        "_note": ("原版队列真值。生效队列 = mat[材质名] 若 >=0，否则 shader[shader名]。"
                  "⚠️ 同名多值要精确区分时读 `mat_by_bundle[bundle][材质名]`"
                  "（bundle = 去掉 `bundle_` 前缀的 assets_full 包目录名）。"
                  "由 工具/gen_renderqueue_truth.py 从 assets_full 的原始 JSON 生成，别手改。"),
        "_conflict_policy": ("同名多值不静默：① 若各份的**生效队列**（queue>=0 用它，否则 shader QUEUE）相同 ⇒ 直接用那个值；"
                             "② 真歧义 ⇒ `mat` 表取**非负最小值**（保留旧行为；消费方 `EffectExporter` 回落到它，"
                             "而 `renderQueue=-1` 在运行时 = 不覆盖 = shader 默认）"
                             "**并逐条打印 + 记录在 `_conflicts` / `_conflict_sources` 里**。"
                             "⚠️ 按名字键的 `mat` 表**结构上表达不了两个同名不同资产** ⇒ "
                             "**精确区分读 `mat_by_bundle` / `mat_renderqueue_by_bundle.tsv`（3 列带 bundle）**。"),
        "_conflicts": conflicts,
        "_conflict_sources": conflict_sources,
        "mat": mat_table,
        "mat_by_bundle": mat_by_bundle,
        "shader": shaders,
    }
    mtsv = os.path.join(os.path.dirname(OUT), "mat_renderqueue.tsv")
    stsv = os.path.join(os.path.dirname(OUT), "shader_renderqueue.tsv")
    btsv = os.path.join(os.path.dirname(OUT), "mat_renderqueue_by_bundle.tsv")

    js_text = json.dumps(payload, ensure_ascii=False, indent=1)
    mt_text = ("材质名\tm_CustomRenderQueue\n"
               + "\n".join("%s\t%d" % (k, v) for k, v in mat_table.items()))
    st_text = ("shader名\tQUEUE\n"
               + "\n".join("%s\t%d" % (k, v) for k, v in shaders.items() if v is not None))
    bt_text = ("bundle\t材质名\tm_CustomRenderQueue\n"
               + "\n".join("%s\t%s\t%d" % (b, n, q)
                           for b in sorted(mat_by_bundle)
                           for n, q in mat_by_bundle[b].items()))

    if MODE == "--write":
        os.makedirs(os.path.dirname(OUT), exist_ok=True)
        io.open(OUT, "w", encoding="utf-8", newline="\n").write(js_text)
        # 另外落三张 **TSV** —— `EffectExporter` 在 Unity 里读它们（JSON 两层字典不好用 JsonUtility 解）
        io.open(mtsv, "w", encoding="utf-8", newline="\n").write(mt_text)
        io.open(stsv, "w", encoding="utf-8", newline="\n").write(st_text)
        io.open(btsv, "w", encoding="utf-8", newline="\n").write(bt_text)
        print("已落表：%s\n         %s\n         %s\n         %s" % (OUT, mtsv, stsv, btsv))
    elif MODE == "--check":
        bad = 0
        for path, want in ((OUT, js_text), (mtsv, mt_text), (stsv, st_text), (btsv, bt_text)):
            got = read_text(path)
            if got is None:
                print("❌ 不存在：%s" % path)
                bad += 1
            elif got != want:
                print("❌ 已过期（与现生成不一致）：%s" % path)
                bad += 1
            else:
                print("✅ 一致：%s" % path)
        if bad:
            print("❌ --check：%d 个文件缺失/过期 —— 重跑加 `--write` 落盘" % bad)
            return 1
        print("✅ --check：四个文件齐全且一致")
    else:   # --dry
        print("干跑（未写盘）：%s · %s · %s · %s" % (OUT, mtsv, stsv, btsv))

    print("材质 %d 个（同名多值 %d）· shader %d 个（有 QUEUE 的 %d）· 带 bundle 的材质 %d 个 / %d 个包"
          % (len(mat_table), len(conflicts), len(shaders),
             sum(1 for v in shaders.values() if v is not None),
             sum(len(v) for v in mat_by_bundle.values()), len(mat_by_bundle)))
    for k in sorted(conflicts):
        print("   ⚠️ 同名多值 `%s`：raw=%s ⇒ mat 表取 %s%s"
              % (k, conflicts[k], mat_table.get(k),
                 "" if not ambiguous.get(k) else "（真歧义：各份生效队列也不同）"))
        for s in conflict_sources.get(k, []):
            print("        queue=%-5s effective=%-5s shader=%r  ← %s"
                  % (s["queue"], s["effective"], s["shader"], s["file"]))
        print("        （同名两份是**两个不同资产**；按名字键的 mat 表表达不了，"
              "精确区分读 `mat_by_bundle` / `mat_renderqueue_by_bundle.tsv`）")
    return 0


if __name__ == "__main__":
    sys.exit(main())
