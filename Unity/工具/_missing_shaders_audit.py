#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""_missing_shaders_audit.py —— 只读体检：给「运行时缺的原版 shader」出一张引用清单表

⚠️ **本脚本只读、不写任何文件。**
   它**不会**碰 `StreamingAssets/WarpforgeVFX/`，也不会调 `extract_missing_shaders.py`
   的非 --check 分支（那条路会重打 `wf_shaders_extra.bundle`）。

读的源：
  1. `资料/普查产出_0913/效果_shader_对账.tsv` —— 958 条效果 × 「用到的原版 shader 列表」
     （2026-09-13 从当时那份**三格格式**的 `导出报告.tsv` 解析出来的；今天那份报告已被
      正在跑的全量重导**改成了两格**，靠它拿不到这个信息了 ⇒ 必须用这份存档）
  2. `Assets/StreamingAssets/WarpforgeVFX/*.bundle` —— **目录下每一只包都实读**（不靠记录、
     也不按文件名挑）。其中 `wf_shaders.bundle` = 主包、`wf_shaders_extra.bundle` = 补充包，
     表格里单列；其余包（`wf_tmp_shaders` / `wf_prefabs_extra` / `wf_arena_*` / `wf_builtin` …）
     合并报在「在别的包」那一列里。
  3. `WarpforgeShaderMap.cs` + `EffectExporter.cs` 的两张映射表（正则抓，反映**当前**代码）

⚠️ **2026-10-22 更正（铁律 5）**：上面第 2 条原来写的是「只读 `wf_shaders.bundle` + `wf_shaders_extra.bundle`
   两只包」。**那个口径是错的、而且已经把结论带偏过**：`TextMeshPro/Distance Field Offset` 因此被判成
   「未解析 / 有替代 shader：`TextMeshPro/Distance Field`（近似）」—— 而它**实读就在
   `wf_tmp_shaders.bundle` 里**（`工具/extract_missing_shaders.py --tmp` 打出来的 4 件 TMP shader 之一，
   见 `资料/普查产出_1022/W64_素材导出缺口.md` §四①）。⇒ 改成**扫全目录**。
   同一次实读还发现 `wf_prefabs_extra.bundle` 里也有 shader（`Everguild/Cards/3D Card Explosion`、
   `Everguild/Matcap/Matcap Full Options VAT`，两件都与补充包重叠），旧口径对它们同样是瞎的。

用法：
  "D:/2/Warpforge_tools/py312/python.exe" "d:/4/Unity/工具/_missing_shaders_audit.py"
"""
import glob
import os
import re
import sys

import UnityPy

ROOT = r"d:/4/Unity"
VFX_TSV = os.path.join(ROOT, "资料/普查产出_0913/效果_shader_对账.tsv")
VFX_DIR = os.path.join(ROOT, "MyGame/Assets/StreamingAssets/WarpforgeVFX")
MAIN_BUNDLE = os.path.join(VFX_DIR, "wf_shaders.bundle")
EXTRA_BUNDLE = os.path.join(VFX_DIR, "wf_shaders_extra.bundle")
# ⚠️ 2026-10-22：原来这里只有上面两只包。`wf_tmp_shaders.bundle`（TMP 4 件 shader）**必须**一起读，
#    否则 `TextMeshPro/Distance Field Offset` 会被判成「未解析」（那个结论已过期）。见文件头更正块。
TMP_BUNDLE = os.path.join(VFX_DIR, "wf_tmp_shaders.bundle")
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


def read_all_shader_bundles(vfx_dir):
    """**目录下每一只 `.bundle` 都实读**，返回 {包文件名: {shader 名…}}。

    ⚠️ **2026-10-22 加（铁律 5：发现记错了就地改掉）**：本工具原来写死只读主包 + 补充包两只，
    于是 `TextMeshPro/Distance Field Offset` 被判成「未解析 / 有替代 shader（近似）」——
    **那行是过期结论**，它实读就在 `wf_tmp_shaders.bundle` 里。
    同一次全目录实读还查出 `wf_prefabs_extra.bundle` 也有 shader（2 件与补充包重叠）。
    ⇒ **别按文件名猜哪只包里有 shader**（`wf_arena_*` / `wf_builtin` / `wf_menus_extra` /
      `wf_boosters_extra` / `wf_arena_mirror` 里都有），一律实读。
    """
    out = {}
    for p in sorted(glob.glob(os.path.join(vfx_dir, "*.bundle"))):
        out[os.path.basename(p)] = bundle_shader_names(p)
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


def cs_set(path, name):
    """从 .cs 里抓 `Name = new HashSet<string> { "A", "B", … };` —— 纯字符串集合。

    ⚠️ **2026-10-02 加**：`UseOriginal` 是一张 **HashSet<string>**（不是 Dictionary），
    上面 `cs_map` 那个 `{"原名", "目标名"}` 的**一条都抓不到** ⇒ 本工具对这张表一直是**瞎的**，
    于是把「已经改走原件」的名字继续算成「未被映射」。
    实测（2026-10-02）：该档 33 件里 **有 5 件早在白名单里**（`Spiral Trail FX` / `ShieldVfx` /
    `Glow Shader` / `Gem Crystal Glitter` / `Gem Crystal Glitter Explosion`），工具却一直报「28 未映射」。
    """
    src = open(path, encoding="utf-8-sig").read()
    m = re.search(re.escape(name) + r"\s*=\s*new\s+HashSet<string>", src)
    if not m:
        return set()
    start = src.index("{", m.end())
    end = re.search(r"\n\s*\};", src[start:])
    body = src[start:start + end.end()] if end else src[start:]
    return set(re.findall(r'"([^"]+)"', body))


def main():
    for p in (VFX_TSV, MAIN_BUNDLE, EXTRA_BUNDLE, RUNTIME_MAP, EXPORT_MAP):
        if not os.path.exists(p):
            print(f"!! 缺文件：{p}")
            return 1

    rows = load_tsv(VFX_TSV)
    # ⚠️ 2026-10-22：**全目录实读**，不再只读主包 + 补充包（旧口径漏了 `wf_tmp_shaders.bundle`，
    #    把 `TextMeshPro/Distance Field Offset` 误报成「未解析」）。见文件头更正块。
    bundles = read_all_shader_bundles(VFX_DIR)
    main_name = os.path.basename(MAIN_BUNDLE)
    extra_name = os.path.basename(EXTRA_BUNDLE)
    main = bundles.get(main_name, set())
    extra = bundles.get(extra_name, set())
    others = {k: v for k, v in bundles.items() if k not in (main_name, extra_name)}
    alln = set()
    for v in bundles.values():
        alln |= v
    where = {}                     # shader -> 命中的包文件名（列表）
    for bn, names in bundles.items():
        for s in names:
            where.setdefault(s, []).append(bn)
    rt = cs_map(RUNTIME_MAP)
    ex = cs_map(EXPORT_MAP)
    uo = cs_set(RUNTIME_MAP, "UseOriginal")

    print(f"[A] 效果行 {len(rows)} 条")
    print(f"[B] {main_name} 实读 {len(main)} 个 shader")
    print(f"[C] {extra_name} 实读 {len(extra)} 个 shader（与主包重叠 {len(main & extra)}）")
    print(f"[C2] 另外实读 {len(others)} 只包（**含 shader 的一律算进来**，不按文件名猜）：")
    for bn in sorted(others):
        print(f"       {bn}\t{len(others[bn])} 个\t"
              f"与主包重叠 {len(others[bn] & main)} · 与补充包重叠 {len(others[bn] & extra)}")
    if os.path.basename(TMP_BUNDLE) not in bundles:
        # 不静默：这只包是 gitignore 产物（`extract_missing_shaders.py --tmp` 才生成）
        print(f"[C3] !! 缺 {os.path.basename(TMP_BUNDLE)} —— TMP 那 4 件 shader 会显示成缺件。"
              f"生成：工具/extract_missing_shaders.py --tmp")
    print(f"[D] 映射表：运行时 Replacements {len(rt)} 条 / 导出期 ShaderMap {len(ex)} 条 / "
          f"**UseOriginal 白名单 {len(uo)} 条**  · 全目录 shader 名并集 {len(alln)} 个")

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
    still = [s for s in group if s not in rt and s not in ex and s not in uo]
    took = [s for s in group if s in rt or s in ex]
    in_uo = [s for s in group if s in uo]

    print(f"\n[E] 该档共 {len(group)} 个（09-13 口径：非标准/非工程自带 + 当时两张映射表都没有）")
    print(f"    今天**仍未被声明** {len(still)} 个 · 已进 Replacements/ShaderMap {len(took)} 个 · "
          f"**已进 `UseOriginal` 白名单 {len(in_uo)} 个**（两类可能重叠：白名单会盖住 Replacements）")
    print(f"    实读：在主包 {sum(1 for s in group if s in main)} 个 · "
          f"在补充包 {sum(1 for s in group if s in extra)} 个 · "
          f"**只在别的包** {sum(1 for s in group if s not in main and s not in extra and s in alln)} 个 · "
          f"**所有包都没有** {sum(1 for s in group if s not in alln)} 个")
    print(f"    影响效果行合计 {sum(n_eff.get(s, 0) for s in group)} 处"
          f"（仍未被声明的 {sum(n_eff.get(s, 0) for s in still)} 处）")

    print("\n--- 表格行：shader | 影响效果数 | 样例 | in_extra | in_main | 在别的包 | 处置 | 09-13 类别 ---")
    for s in group:
        effs = eff_of.get(s, [])
        samp = " / ".join(effs[:3])
        other_hit = [bn for bn in where.get(s, []) if bn not in (main_name, extra_name)]
        if s in uo and s in alln:
            # 🔴 **2026-10-02 加这个分支，而且要排在 `rt` 前面** —— `UseOriginal` 白名单在
            #    `TryResolve` 里**先于** `Replacements` 生效（白名单 = 「跳过自建替代、去取原件」）
            #    ⇒ 一个名字**同时在两张表里**时，运行时真正走的是**原件**。
            #    例：`Everguild/FX/Particle Premultiply` 在 `Replacements` 里映射到自建
            #    `WarpforgeVFX/Particles/Extra Color`，但白名单把它拽去取原版 bundle 了。
            #    （本分支出现之前，这些行被显示成「有替代 shader」，与运行时行为**不符**。）
            disp = "✅ **已进 `UseOriginal` 白名单**（走原件；本行原本就解析得到）"
        elif s in rt:
            disp = f"有替代 shader：{rt[s]}"
        elif s in ex:
            disp = f"有替代 shader：{ex[s].rstrip('*')}（近似）"
        elif s in alln:
            # 🔴 2026-09-18 更正：原来这里写「编辑器渲染有故障」。实测对这批**不成立** ——
            #    `ShaderFallbackProbe`（2026-09-18）拿这 28 个名字逐条真渲：28/28 解析到原版 bundle、
            #    isSupported 全 true、0 个洋红。原那条结论讲的是 `URP Particles/Unlit`（工程自带的
            #    内建 shader 从 bundle 取到的那份），是从单例推广到全体的推断。
            #    ⚠️ **2026-10-02：那后半句「不能进发布版本」也已作废** —— 版权红线 2026-09-18 取消
            #       （个人学习用途），不再是约束 ⇒ 这里不再打印它。
            #    ⚠️ **2026-10-22**：`extra or main` 换成 `alln` —— 否则「只在 `wf_tmp_shaders` /
            #       `wf_prefabs_extra` 里」的件会掉进下面那句「未映射：占位材质保留」（假缺口）。
            disp = "掉兜底：原版 bundle（**能渲染**；只是没被声明）"
        else:
            disp = "未映射：占位材质保留"
        print(f"{s}\t{n_eff.get(s,0)}\t{samp}\t"
              f"{'Y' if s in extra else 'N'}\t{'Y' if s in main else 'N'}\t"
              f"{','.join(other_hit) if other_hit else '-'}\t{disp}\t{cat[s]}")

    # ---- 反查：今天这两张映射表已经把上面哪几个接管了 ----
    print(f"\n[F] 该档里今天已进映射表（Replacements / ShaderMap）的 {len(took)} 个：")
    for s in sorted(took, key=lambda x: -n_eff.get(x, 0)):
        print(f"      {n_eff.get(s,0):4d}  {s}  ->  rt={rt.get(s, '-')} / ex={ex.get(s, '-')}")

    # ---- 🆕 2026-10-02：第三张表（`UseOriginal` 白名单）也要反查 ----
    #      ⚠️ 它和 [F] **会重叠**：白名单的语义是「**跳过** Replacements 去取原件」，
    #         所以一个名字可以同时出现在两张表里（例：`Everguild/FX/Particle Premultiply`）。
    print(f"\n[G] 该档里已进 `UseOriginal` 白名单的 {len(in_uo)} 个（**走原件**，与 [F] 可能重叠）：")
    for s in sorted(in_uo, key=lambda x: -n_eff.get(x, 0)):
        # ⚠️ 2026-10-22 更正：这里原来写 `'原件在主包' if s in main else '原件在补充包'` ——
        #    一个**非此即彼**的二分，而「别的包」（`wf_tmp_shaders` 等）根本不在这个二分里
        #    ⇒ `TextMeshPro/Distance Field Offset` 明明在 tmp 包里，却被印成「原件在补充包」（假的）。
        #    现在按**实读命中的包名**逐条打印。
        print(f"      {n_eff.get(s,0):4d}  {s}  ->  原件在 {' / '.join(where.get(s, []) or ['**所有包都没有**'])}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
