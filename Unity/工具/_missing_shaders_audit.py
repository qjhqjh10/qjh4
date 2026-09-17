#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""_missing_shaders_audit.py —— 只读体检：给「运行时缺的原版 shader」出一张引用清单表

⚠️ **本脚本只读、不写任何文件。**
   它**不会**碰 `StreamingAssets/WarpforgeVFX/`，也不会调 `extract_missing_shaders.py`
   的非 --check 分支（那条路会重打 `wf_shaders_extra.bundle`）。

读的 4 个源：
  1. `资料/普查产出_0913/效果_shader_对账.tsv` —— 958 条效果 × 「用到的原版 shader 列表」
     （2026-09-13 从当时那份**三格格式**的 `导出报告.tsv` 解析出来的；今天那份报告已被
      正在跑的全量重导**改成了两格**，靠它拿不到这个信息了 ⇒ 必须用这份存档）
  2. `Assets/StreamingAssets/WarpforgeVFX/wf_shaders.bundle`      —— 主包（实读，不靠记录）
  3. `Assets/StreamingAssets/WarpforgeVFX/wf_shaders_extra.bundle`—— 补充包（实读）
  4. `WarpforgeShaderMap.cs` + `EffectExporter.cs` 的两张映射表（正则抓，反映**当前**代码）

用法：
  "D:/2/Warpforge_tools/py312/python.exe" "d:/4/Unity/工具/_missing_shaders_audit.py"
"""
import os
import re
import sys

import UnityPy

ROOT = r"d:/4/Unity"
VFX_TSV = os.path.join(ROOT, "资料/普查产出_0913/效果_shader_对账.tsv")
VFX_DIR = os.path.join(ROOT, "MyGame/Assets/StreamingAssets/WarpforgeVFX")
MAIN_BUNDLE = os.path.join(VFX_DIR, "wf_shaders.bundle")
EXTRA_BUNDLE = os.path.join(VFX_DIR, "wf_shaders_extra.bundle")
RUNTIME_MAP = os.path.join(ROOT, "MyGame/Assets/WarpforgeVFX/Runtime/WarpforgeShaderMap.cs")
EXPORT_MAP = os.path.join(ROOT, "MyGame/Assets/WarpforgeArena1/Editor/EffectExporter.cs")

# 脚本自带 shader（Shader.Find 拿得到，不算缺）—— 与 extract_missing_shaders.py:BUILTIN_PREFIX 同一份
BUILTIN_PREFIX = (
    "Universal Render Pipeline/", "Sprites/", "UI/", "TextMeshPro/",
    "WarpforgeVFX/", "Mobile/Particles/", "Particles/", "Legacy Shaders/", "Hidden/",
)

SELF_PREFIX = "WarpforgeVFX/"          # 自建 shader，不是原版名
UNAPPROX = re.compile(r"原版(补充包|主包)兜底\s*([^；,]+)")     # 未映射但原版 bundle 里有
SYSNONAME = re.compile(r"工程/包内同名\s*([^；,]+)")            # 未映射但工程/包内有同名
UNRESOLVED = re.compile(r"未映射且未解析\s*([^；,]+)")


def bundle_shader_names(path):
    """bundle 里 Shader 的真名（m_Name 是空的，真名在 m_ParsedForm.m_Name）。"""
    env = UnityPy.load(path)
    out = set()
    for o in env.objects:
        if o.type.name != "Shader":
            continue
        try:
            d = o.read()
        except Exception:
            continue
        pf = getattr(d, "m_ParsedForm", None)
        n = (getattr(pf, "m_Name", "") if pf is not None else "") or getattr(d, "m_Name", "") or ""
        if n:
            out.add(n)
    return out


def load_tsv(path):
    rows = []
    with open(path, encoding="utf-8-sig") as f:
        for line in f:
            p = line.rstrip("\r\n").split("\t")
            if len(p) < 3 or p[0].startswith("#") or p[0] == "效果名":
                continue
            shaders = [x.strip() for x in p[2].split(",") if x.strip()]
            detail = p[5] if len(p) > 5 else ""
            rows.append({"effect": p[0], "shaders": shaders, "detail": detail})
    return rows


def cs_map(path):
    """从 .cs 里抓 { "原版名", "目标名" } —— 目标名带 * 表示近似。"""
    src = open(path, encoding="utf-8-sig").read()
    m = re.search(r"ShaderMap\s*=\s*new\s+Dictionary", src) or \
        re.search(r"Replacements\s*=\s*new\s+Dictionary", src)
    if not m:
        return {}
    # 只取那一个初始化块（到 `};` 为止），别把文件后面别的字典一起抓进来
    start = src.index("{", m.end())
    end = re.search(r"\n\s*\};", src[start:])
    body = src[start:start + end.end()] if end else src[start:]
    out = {}
    for k, v in re.findall(r'\{\s*"([^"]+)"\s*,\s*"([^"]+)"\s*\}', body):
        if k.startswith("Universal Render") and k == v:
            continue                      # 映射到自身 = 标准 shader，不算替代
        out[k] = v
    return out


def main():
    for p in (VFX_TSV, MAIN_BUNDLE, EXTRA_BUNDLE, RUNTIME_MAP, EXPORT_MAP):
        if not os.path.exists(p):
            print(f"!! 缺文件：{p}")
            return 1

    rows = load_tsv(VFX_TSV)
    main = bundle_shader_names(MAIN_BUNDLE)
    extra = bundle_shader_names(EXTRA_BUNDLE)
    rt = cs_map(RUNTIME_MAP)
    ex = cs_map(EXPORT_MAP)

    print(f"[A] 效果行 {len(rows)} 条")
    print(f"[B] wf_shaders.bundle 实读 {len(main)} 个 shader")
    print(f"[C] wf_shaders_extra.bundle 实读 {len(extra)} 个 shader（与主包重叠 {len(main & extra)}）")
    print(f"[D] 映射表：运行时 Replacements {len(rt)} 条 / 导出期 ShaderMap {len(ex)} 条")

    # ---- 复原「33 个」这一档：未映射（两张表都没有）、非自建、非工程自带 ----
    cat = {}       # shader -> 表里的类别（补充包兜底/主包兜底/未解析）
    for r in rows:
        for label, name in UNAPPROX.findall(r["detail"]):
            cat.setdefault(name.strip(), "补充包兜底" if label == "补充包" else "主包兜底")
        for name in SYSNONAME.findall(r["detail"]):
            cat.setdefault(name.strip(), "工程同名")
        for name in UNRESOLVED.findall(r["detail"]):
            cat.setdefault(name.strip(), "未解析")

    # 每行第 3 格里的 shader 名（**只剔自建名**：`TextMeshPro/*` 虽然是「工程自带」前缀，
    # 但 `TextMeshPro/Distance Field Offset` 正是这一档里的一个，不能按前缀把它滤掉）
    n_eff = {}
    eff_of = {}
    for r in rows:
        for s in r["shaders"]:
            if s.startswith(SELF_PREFIX):
                continue
            n_eff[s] = n_eff.get(s, 0) + 1
            eff_of.setdefault(s, []).append(r["effect"])

    # 这一档 = 存档里标成「原版某包兜底 / 未映射且未解析」的那批（= 09-13 当时两张表都没有的）
    # ⚠️「工程/包内同名」(C 类) 不算：那几个靠 `Shader.Find` 就能解析，不是缺件
    group = sorted((s for s in cat if cat[s] != "工程同名"),
                   key=lambda s: (-n_eff.get(s, 0), s))
    still = [s for s in group if s not in rt and s not in ex]
    took = [s for s in group if s in rt or s in ex]

    print(f"\n[E] 该档共 {len(group)} 个（09-13 口径：非标准/非工程自带 + 当时两张映射表都没有）")
    print(f"    今天仍未被映射 {len(still)} 个 · 已被自建替代接管 {len(took)} 个")
    print(f"    实读：在主包 {sum(1 for s in group if s in main)} 个 · "
          f"在补充包 {sum(1 for s in group if s in extra)} 个 · "
          f"两边都没有 {sum(1 for s in group if s not in main and s not in extra)} 个")
    print(f"    影响效果行合计 {sum(n_eff.get(s, 0) for s in group)} 处"
          f"（仍未被映射的 {sum(n_eff.get(s, 0) for s in still)} 处）")

    print("\n--- 表格行：shader | 影响效果数 | 样例 | in_extra | in_main | 处置 ---")
    for s in group:
        effs = eff_of.get(s, [])
        samp = " / ".join(effs[:3])
        if s in rt:
            disp = f"有替代 shader：{rt[s]}"
        elif s in ex:
            disp = f"有替代 shader：{ex[s].rstrip('*')}（近似）"
        elif s in extra or s in main:
            disp = "掉兜底：原版 bundle（编辑器渲染有故障·不可发布）"
        else:
            disp = "未映射：占位材质保留"
        print(f"{s}\t{n_eff.get(s,0)}\t{samp}\t"
              f"{'Y' if s in extra else 'N'}\t{'Y' if s in main else 'N'}\t{disp}\t{cat[s]}")

    # ---- 反查：今天这两张映射表已经把上面哪几个接管了 ----
    print(f"\n[F] 该档里今天已进映射表的 {len(took)} 个：")
    for s in sorted(took, key=lambda x: -n_eff.get(x, 0)):
        print(f"      {n_eff.get(s,0):4d}  {s}  ->  rt={rt.get(s, '-')} / ex={ex.get(s, '-')}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
