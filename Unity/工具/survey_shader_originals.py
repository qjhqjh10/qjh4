# -*- coding: utf-8 -*-
"""survey_shader_originals.py —— ④「改走原件」的第一张表（2026-09-18）

**回答一个问题**：现在被映射到**自建 shader** 的那些原版 shader 名，
**原件到底在不在我们随包的两个 bundle 里**？在的才能改走原件。

为什么要这张表：`资料/普查产出_0917/补充shader引用清单.md` 只覆盖了
「两张映射表都没有」的那 33 个；**已经映射到自建替代的那批从来没查过原件在不在**。
而 README 线索 #1 的建议（「原件在手 ⇒ 可以改走原版 shader」）正是针对它们的。

**只读**：不写 `Assets/`、不写 `StreamingAssets/`、不跑 `extract_missing_shaders.py`。

用法：
  PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/survey_shader_originals.py
"""
import io, os, re, sys
import UnityPy

ROOT = "d:/4/Unity/MyGame/Assets"
RUNTIME_MAP = ROOT + "/WarpforgeVFX/Runtime/WarpforgeShaderMap.cs"
EXPORT_MAP = ROOT + "/WarpforgeArena1/Editor/EffectExporter.cs"
MAIN_BUNDLE = ROOT + "/StreamingAssets/WarpforgeVFX/wf_shaders.bundle"
EXTRA_BUNDLE = ROOT + "/StreamingAssets/WarpforgeVFX/wf_shaders_extra.bundle"
EFFECT_TSV = "d:/4/Unity/资料/普查产出_0913/效果_shader_对账.tsv"   # 958 行三格存档
OUT = "d:/4/Unity/资料/普查产出_0918/shader原件可用性_表.md"


def bundle_shader_names(path):
    """bundle 里 Shader 的真名（m_Name 是空的，真名在 m_ParsedForm.m_Name）。"""
    env = UnityPy.load(path)
    out = {}
    for o in env.objects:
        if o.type.name != "Shader":
            continue
        d = o.read()
        pf = getattr(d, "m_ParsedForm", None)
        nm = getattr(pf, "m_Name", None) if pf else None
        if not nm:
            nm = getattr(d, "m_Name", "") or ""
        if nm:
            out[nm] = o.path_id
    return out


def bundle_container(path):
    """AssetBundle.m_Container 的键（容器路径）。"""
    env = UnityPy.load(path)
    keys = []
    for o in env.objects:
        if o.type.name == "AssetBundle":
            d = o.read()
            c = getattr(d, "m_Container", None)
            if c is None:
                continue
            try:
                keys = list(c.keys())
            except Exception:
                keys = [x[0] for x in c]
    return keys


def cs_map(path):
    """从 .cs 里抓 { "原版名", "目标名" }。"""
    src = io.open(path, encoding="utf-8-sig").read()
    m = re.search(r"ShaderMap\s*=\s*new\s+Dictionary", src) or \
        re.search(r"Replacements\s*=\s*new\s+Dictionary", src)
    if not m:
        return {}
    start = src.index("{", m.end())
    end = re.search(r"\n\s*\};", src[start:])
    body = src[start:start + end.end()] if end else src[start:]
    out = {}
    for k, v in re.findall(r'\{\s*"([^"]+)"\s*,\s*"([^"]+)"\s*\}', body):
        if k.startswith("Universal Render") and k == v:
            continue
        out[k] = v
    return out


def effect_counts():
    """原版 shader 名 -> 影响效果数（效果行数口径，与 0917 台账一致）。"""
    if not os.path.exists(EFFECT_TSV):
        return {}
    cnt = {}
    for line in io.open(EFFECT_TSV, encoding="utf-8-sig"):
        p = line.rstrip("\n").split("\t")
        if len(p) < 3 or p[0] == "effect":
            continue
        for s in [x.strip() for x in p[2].split(",") if x.strip()]:
            cnt[s] = cnt.get(s, 0) + 1
    return cnt


def main():
    for p in (RUNTIME_MAP, EXPORT_MAP, MAIN_BUNDLE, EXTRA_BUNDLE):
        if not os.path.exists(p):
            print("!! 缺文件：%s" % p)
            return 1

    main_b = bundle_shader_names(MAIN_BUNDLE)
    extra_b = bundle_shader_names(EXTRA_BUNDLE)
    main_c = set(bundle_container(MAIN_BUNDLE))
    extra_c = set(bundle_container(EXTRA_BUNDLE))
    rmap = cs_map(RUNTIME_MAP)
    emap = cs_map(EXPORT_MAP)
    eff = effect_counts()

    print("主包 wf_shaders.bundle      : %d 个 shader / %d 条容器" % (len(main_b), len(main_c)))
    print("补充包 wf_shaders_extra.bundle: %d 个 shader / %d 条容器" % (len(extra_b), len(extra_c)))
    print("运行时映射表 %d 条 · 导出期映射表 %d 条 · 效果对账 %d 行" % (len(rmap), len(emap), len(eff)))
    print()

    names = sorted(set(rmap) | set(emap) | set(main_b) | set(extra_b))
    rows = []
    for n in names:
        in_main = n in main_b or n in main_c
        in_extra = n in extra_b or n in extra_c
        rows.append({
            "name": n,
            "rt": rmap.get(n, ""),
            "ex": emap.get(n, ""),
            "main": in_main,
            "extra": in_extra,
            "n": eff.get(n, 0),
        })
    # 对照：这份检查本身可信吗 —— 拿已知在包里的名字验
    ctrl = [r for r in rows if r["name"] in ("Everguild/FX/Rays For Trail", "Spine/Special/HiddenPass",
                                             "Everguild/FX/Extra Color", "Everguild/Matcap/Matcap Full Options")]
    print("=== 阳性对照（这几个应当「在补充包/主包」）===")
    for r in ctrl:
        print("  %-46s 主包=%s 补充包=%s" % (r["name"], r["main"], r["extra"]))
    print()

    both = [r for r in rows if r["main"] and r["extra"]]
    only_main = [r for r in rows if r["main"] and not r["extra"]]
    only_extra = [r for r in rows if r["extra"] and not r["main"]]
    neither = [r for r in rows if not r["main"] and not r["extra"]]
    print("原件在**主包**   : %d" % len(only_main))
    print("原件在**补充包** : %d" % len(only_extra))
    print("两个包都有       : %d" % len(both))
    print("**两个包都没有** : %d" % len(neither))

    # ---- 落表 ----
    mapped = [r for r in rows if r["rt"] or r["ex"]]
    fallback = [r for r in rows if not (r["rt"] or r["ex"]) and (r["main"] or r["extra"])]
    mapped.sort(key=lambda r: (-r["n"], r["name"]))
    fallback.sort(key=lambda r: (-r["n"], r["name"]))

    L = []
    L.append("# shader 原件可用性表（④「改走原件」的输入）")
    L.append("")
    L.append("> 2026-09-18 · 由 `工具/survey_shader_originals.py` 生成（**纯 Python 只读**，未启动 Unity、"
             "未写 `Assets/` 与 `StreamingAssets/`）")
    L.append("> 数据源：实读两个 bundle（UnityPy 读 `Shader.m_ParsedForm.m_Name` + `AssetBundle.m_Container`）、"
             "两张映射表（正则抓 `.cs`）、效果行数来自 `资料/普查产出_0913/效果_shader_对账.tsv`。")
    L.append("")
    L.append("## 一句话")
    L.append("")
    L.append("当前**被映射到自建 shader** 的原版名共 **%d** 个；其中原件**就在随包 bundle 里**的有 **%d** 个"
             "（可改走原件），**原件不在包里**的有 **%d** 个（只能继续用自建）。"
             % (len(mapped), len([r for r in mapped if r["main"] or r["extra"]]),
                len([r for r in mapped if not (r["main"] or r["extra"])])))
    L.append("")
    L.append("## 一、当前已映射到自建 shader 的（这张表要判的就是这些）")
    L.append("")
    L.append("| 原版 shader | 影响效果数 | 运行时映射目标 | 导出期映射目标 | 原件在主包 | 原件在补充包 | **能否改走原件** |")
    L.append("|---|---|---|---|---|---|---|")
    for r in mapped:
        ok = "**✅ 能**" if (r["main"] or r["extra"]) else "❌ 不能（原件不在包里）"
        L.append("| `%s` | %d | %s | %s | %s | %s | %s |" % (
            r["name"], r["n"],
            ("`%s`" % r["rt"]) if r["rt"] else "—",
            ("`%s`" % r["ex"]) if r["ex"] else "—",
            "有" if r["main"] else "无", "有" if r["extra"] else "无", ok))
    L.append("")
    L.append("## 二、未映射 → 掉原版 bundle 兜底 的（**已经在用原件**，列在这里只为对照）")
    L.append("")
    L.append("| 原版 shader | 影响效果数 | 原件在主包 | 原件在补充包 |")
    L.append("|---|---|---|---|")
    for r in fallback:
        L.append("| `%s` | %d | %s | %s |" % (r["name"], r["n"],
                                            "有" if r["main"] else "无", "有" if r["extra"] else "无"))
    L.append("")
    L.append("## 三、判读前必须知道的")
    L.append("")
    L.append("- **`m_Container` 与 Shader 名都要读**：这个包的资产名是 32 位 GUID、容器路径另有一套；"
             "且数据块是 LZ4 压缩的，**裸字节 grep 会骗人**（0917 已经栽过一次）。")
    L.append("- **「能改走原件」只是必要条件，不是充分条件**：原版编译字节码**不能进发布版本**"
             "✅ 2026-09-18 用户取消版权红线（个人学习用途）⇒ 不再要求发布前处理。")
    L.append("- 效果数口径 = **效果行数**（同一效果重复出现只算 1），与 0917 台账一致；"
             "`特效还原_进度与交接.md` 的「370 处引用」是**另一个口径**，别混用。")
    L.append("")

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    io.open(OUT, "w", encoding="utf-8", newline="\n").write("\n".join(L))
    print()
    print("已落表：%s" % OUT)
    return rows


if __name__ == "__main__":
    main()
