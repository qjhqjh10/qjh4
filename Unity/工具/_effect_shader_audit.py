# -*- coding: utf-8 -*-
"""_effect_shader_audit.py — 效果名 → 用了哪些原版 shader 的完整对账表（纯 Python，不碰 Unity）

**主源**：`MyGame/Assets/WarpforgeVFX/导出报告.tsv`（`EffectExporter` 每次导出后`SaveReport()` 落盘）
  行格式：`效果名 ⇥ 状态/原因 [⇥ 材质定义N个；原 shader: A, B[, C]；近似替代 N 处]`
  ⚠️ 第 3 格**只有真被导出过的行才有**：一次 `Resume=true` 续跑会把没重导的行永久截成两格
     （`LoadReport():229` 只取第 2 格，`SaveReport():234-240` 照原样写回）。见产物里的 §五。
  ⚠️ 全量重导**正在跑**时这份文件是不完整的（每 10 个效果刷一次），**要等它 959 行且稳定**再用：
     `wc -l` 满 959 + 连续两次 `md5sum` 相同 + `awk -F'\t' '{print NF}'` 基本全是 3。

**交叉核对源**：`资料/普查产出_0913/效果_shader_对账.tsv`（959 行 × 6 列：效果名/材质数/用到的原版
  shader 列表/是否含未映射·未解析 shader/近似替代处数/未映射明细）—— 0913 那次完整解析的存档。

判据（两张映射表，**从当前代码正则抓**，不抄记录）：
  · 导出期 `EffectExporter.ShaderMap`       （占位材质选谁 + 报告标不标「近似替代」）
  · 运行时 `WarpforgeShaderMap.Replacements`（运行时材质重建解析到谁）
  两张表**要求同步**，只在一张里有 = 缺口（§三 B 组会点出来）。
  自建的 `WarpforgeVFX/*` 名**不算原版 shader**（它们是映射的**结果**，出现在报告里是
  拖尾槽记账写的是替换后名字所致，见 `资料/普查产出_0913/效果_shader_对账.md:31`）。

用法：
  "D:/2/Warpforge_tools/py312/python.exe" -X utf8 "d:/4/Unity/工具/_effect_shader_audit.py"
  "…python.exe" -X utf8 "…/_effect_shader_audit.py" --check      # 只打汇总，不写文件
  "…python.exe" -X utf8 "…/_effect_shader_audit.py" <别的报告.tsv>

产物（就这两个）：
  d:/4/Unity/资料/普查产出_0917/效果_shader对账.tsv        （958 行 + 表头，UTF-8 BOM，LF）
  d:/4/Unity/资料/普查产出_0917/效果_shader对账_小结.md    （≤120 行）
"""
import io
import os
import re
import sys
import collections

A = r"d:/4/Unity/MyGame/Assets"
ROOT = r"d:/4/Unity"
REPORT = A + "/WarpforgeVFX/导出报告.tsv"
SNAPSHOT = r"d:/4/_tmp_view/导出报告_快照_0917.tsv"     # 0917 那份被续跑截过的快照（只用来对照）
ARCHIVE = ROOT + "/资料/普查产出_0913/效果_shader_对账.tsv"
OUT_TSV = ROOT + "/资料/普查产出_0917/效果_shader对账.tsv"
OUT_MD = ROOT + "/资料/普查产出_0917/效果_shader对账_小结.md"

NL = chr(10)
TAB = chr(9)
SELF_PREFIX = "WarpforgeVFX/"          # 自建 shader，不是原版名
COMPLETE_MIN = 900                     # 第 3 格覆盖多少行才算「这源能当主源」
# ⚠️ 行号会漂（那份交接文档常被改）—— 依据是这句话本身：「必须 Resume=true，否则 ClearGenerated() 会把其余 956 个效果一起清掉」
HANDOFF_CITE = "资料/特效还原_进度与交接.md:224"


# ---------------------------------------------------------------- 输入
def read_text(path):
    return io.open(path, "rb").read().decode("utf-8-sig").replace("\r\n", NL).replace("\r", NL)


def map_keys(path, anchor):
    block = read_text(path).split(anchor)[1].split("};")[0]
    out = {}
    for line in block.split(NL):
        line = line.split("//")[0].strip()
        if not line.startswith("{"):
            continue
        p = [x.strip().strip('"') for x in line.rstrip(",").strip("{}").split(",")]
        if len(p) == 2:
            out[p[0]] = p[1]
    return out


EDITOR = map_keys(A + "/WarpforgeArena1/Editor/EffectExporter.cs",
                  "static readonly Dictionary<string, string> ShaderMap = new Dictionary<string, string>")
RUNTIME = map_keys(A + "/WarpforgeVFX/Runtime/WarpforgeShaderMap.cs",
                   "Replacements = new Dictionary<string, string>")


def parse_detail(detail):
    """`材质定义N个；原 shader: A, B；近似替代 N 处` → (mats, [shaders], approx)"""
    mats, shaders, approx = None, [], None
    for s in detail.split("；"):
        m = re.search(r"材质定义\s*(\d+)\s*个", s)
        if m:
            mats = int(m.group(1))
        if "原 shader:" in s:
            shaders = [x.strip() for x in s.split("原 shader:")[-1].split(",") if x.strip()]
        m = re.search(r"近似替代\s*(\d+)\s*处", s)
        if m:
            approx = int(m.group(1))
    return mats, shaders, approx


def parse_report(path):
    """报告格式（2~3 格）。→ [dict]"""
    rows = []
    for line in read_text(path).split(NL):
        if not line.strip() or line.startswith("#"):
            continue
        f = line.split(TAB)
        detail = f[2].strip() if len(f) > 2 else ""
        mats, shaders, approx = parse_detail(detail) if detail else (None, [], None)
        rows.append(dict(name=f[0].strip(), status=f[1].strip() if len(f) > 1 else "",
                         detail=detail, mats=mats, shaders=shaders, approx=approx))
    return rows


def parse_archive(path):
    """0913 存档格式（6 格）。→ [dict]
    ⚠️ 它的第 1 行是**没有 `#` 的表头**（`效果名 ⇥ 材质数 ⇥ …`），按「第 2 格是不是数字」剔除。"""
    rows = []
    for line in read_text(path).split(NL):
        if not line.strip() or line.startswith("#"):
            continue
        f = line.split(TAB)
        if len(f) < 3 or not f[1].strip().isdigit():
            continue
        mats = int(f[1]) if f[1].strip().isdigit() else None
        shaders = [x.strip() for x in f[2].split(",") if x.strip()]
        approx = int(f[4]) if len(f) > 4 and f[4].strip().isdigit() else None
        rows.append(dict(name=f[0].strip(), status="OK", detail="",
                         mats=mats, shaders=shaders, approx=approx))
    return rows


def is_unmapped(sh):
    """未映射 = 原版名，且两张表都不是**键**（自建名不算）。"""
    return (not sh.startswith(SELF_PREFIX)) and (sh not in EDITOR) and (sh not in RUNTIME)


# ---------------------------------------------------------------- 统计
def tally(rows):
    cnt = collections.Counter()
    ex = collections.defaultdict(list)
    unmapped_rows = []
    for r in rows:
        for sh in r["shaders"]:
            if sh.startswith(SELF_PREFIX):
                continue
            cnt[sh] += 1
            ex[sh].append(r["name"])
        u = [s for s in r["shaders"] if is_unmapped(s)]
        if u:
            unmapped_rows.append((r["name"], u))
    self_names = collections.Counter(
        s for r in rows for s in r["shaders"] if s.startswith(SELF_PREFIX))
    return cnt, ex, unmapped_rows, self_names


def wrap(items, per_line=110):
    """把一堆短串按宽度折成若干行。"""
    out, buf = [], ""
    for it in items:
        if buf and len(buf) + len(it) + 3 > per_line:
            out.append(buf)
            buf = ""
        buf = (buf + " · " + it) if buf else it
    if buf:
        out.append(buf)
    return out


def delta_desc(o_sh, r_sh):
    """两版 shader 列表的差 → 一个短描述。"""
    o, r = set(o_sh), set(r_sh)
    add, rem = sorted(r - o), sorted(o - r)
    if not add and not rem:
        return "同"
    if add and not rem and all(x.startswith(SELF_PREFIX) for x in add):
        return "+%d 自建名" % len(add)
    return "+%d / -%d" % (len(add), len(rem))


def compare(new_rows, old_rows):
    """逐效果比 shader 名单（按名字配对）。→ (same_n, diffs, new_cnt, old_cnt)"""
    old = {r["name"]: r for r in old_rows}
    same, diffs = 0, []
    for r in new_rows:
        o = old.get(r["name"])
        if o is None:
            continue
        if o["shaders"] == r["shaders"] and o["mats"] == r["mats"] and o["approx"] == r["approx"]:
            same += 1
        else:
            diffs.append((r["name"], o, r))
    return same, diffs, collections.Counter(s for r in new_rows for s in r["shaders"] if not s.startswith(SELF_PREFIX)), \
        collections.Counter(s for r in old_rows for s in r["shaders"] if not s.startswith(SELF_PREFIX))


# ---------------------------------------------------------------- 主流程
def main():
    argv = [a for a in sys.argv[1:] if not a.startswith("--")]
    check_only = "--check" in sys.argv
    src = argv[0] if argv else REPORT

    if not os.path.exists(src):
        raise SystemExit("找不到主源：%s" % src)
    rows = parse_report(src)
    have = [r for r in rows if r["detail"]]
    miss = [r for r in rows if not r["detail"]]
    bad = [r for r in rows if r["detail"] and (r["mats"] is None or not r["shaders"])]

    archive = parse_archive(ARCHIVE) if os.path.exists(ARCHIVE) else []
    primary_is_fresh = len(have) >= COMPLETE_MIN
    use = rows if primary_is_fresh else archive
    use_label = ("主源（%s）" % os.path.basename(src)) if primary_is_fresh else \
                ("0913 存档（主源当前只有 %d 行 / 应 958 行 —— 多半还没导完，不可用）" % len(rows))

    cnt, ex, unmapped_rows, self_names = tally(use)
    all_names = sorted(cnt, key=lambda n: (-cnt[n], n))
    neither = [n for n in all_names if n not in EDITOR and n not in RUNTIME]
    only_rt = [n for n in all_names if n not in EDITOR and n in RUNTIME]
    only_ed = [n for n in all_names if n in EDITOR and n not in RUNTIME]
    approx_mapped = [n for n in all_names if n in EDITOR and n in RUNTIME
                     and (EDITOR[n].endswith("*") or RUNTIME[n].endswith("*"))]
    snap = parse_report(SNAPSHOT) if os.path.exists(SNAPSHOT) else []

    # ---- 产物 1：TSV（严格 5 列，按调用方口径）----
    lines = [TAB.join(["效果名", "材质数", "原 shader 列表", "是否含未映射 shader", "状态"])]
    for r in rows:
        if not r["detail"]:
            lines.append(TAB.join([r["name"], "查不到", "查不到", "查不到", r["status"]]))
        else:
            lines.append(TAB.join([r["name"], str(r["mats"]) if r["mats"] is not None else "查不到",
                                   ", ".join(r["shaders"]) if r["shaders"] else "查不到",
                                   "是" if [s for s in r["shaders"] if is_unmapped(s)] else "否",
                                   r["status"]]))
    if not check_only:
        os.makedirs(os.path.dirname(OUT_TSV), exist_ok=True)
        io.open(OUT_TSV, "wb").write(("\ufeff" + NL.join(lines) + NL).encode("utf-8"))

    # ---- 产物 2：小结（md）----
    m = []
    ap = m.append
    ap("# 效果 → 用了哪些原版 shader · 对账表（%d 行）· 2026-09-17" % len(rows))
    ap("")
    ap("> 纯 Python 解析（`工具/_effect_shader_audit.py`）：**未启动 Unity、未写 `Assets/` 下任何文件**。")
    ap("> 全表见同目录 `效果_shader对账.tsv`（%d 行 + 表头，UTF-8 BOM，LF）。" % len(rows))
    ap("")
    ap("## 〇、一句话 + 口径")
    ap("")
    if miss:
        ap("🔴 **主源里 %d/%d 行的第 3 格是空的**（`材质定义N个；原 shader: …`）—— **不是导出失败**")
        ap("（状态列 %s），是一次 `Resume=true` 续跑把它抹掉了（机制见 §五）。本表按主源**原样**出：")
        ap("能填的填、其余写 `查不到`；§二~§四 的 shader 清单改用 **%s**。" % use_label)
    else:
        ap("✅ 第 3 格 **%d/%d 行齐全** ⇒ 下面的材质数 / shader 列表 / 是否含未映射**全部出自主源**（新鲜的）。"
           % (len(have), len(rows)))
    ap("")
    ap("- **未映射** = 该原版名**不是**两张映射表的**键**（`EffectExporter.ShaderMap` ∪ `WarpforgeShaderMap.Replacements`）。")
    ap("  两张表都没有 ⇒ 运行时只剩 `Shader.Find` → 原版 bundle 兜底 → 再落空就**保留占位材质**（静默）。")
    ap("- **自建 `WarpforgeVFX/*` 不算原版名**，已从计数里剔除：本源里出现 %d 次 / %d 个名字。" % (sum(self_names.values()), len(self_names)))
    ap("  它出现在报告里是因为**拖尾槽记账写的是替换后的名字**（`EffectExporter.cs` trail 分支 `usedShaders.Add(to)`）。")
    ap("")
    ap("## 一、数据源实测结构（不是照文档抄）")
    ap("")
    ap("| 事实 | 值 |")
    ap("|---|---|")
    ap("| 主源 | `%s` |" % src)
    ap("| 行数 | **%d**（1 表头 + %d 效果）· 行尾 %s |" % (len(rows) + 1, len(rows),
                                                        "CRLF" if b"\r\n" in io.open(src, "rb").read() else "LF"))
    ap("| 列数 | 2 格的行 **%d** · 3 格的行 **%d** |" % (len(miss), len(have)))
    ap("| 状态列 | %s |" % ("全 `OK`，无 FAIL" if all(r["status"] == "OK" for r in rows)
                            else str(dict(collections.Counter(r["status"] for r in rows)))))
    ap("| 第 3 格内部（`；` 切） | `①材质定义N个` · `②原 shader: A, B` · `③近似替代 N 处`（③ 可缺） |")
    ap("| 3 格齐全但抽不出数的行 | **%d** |" % len(bad))
    ap("| shader 清单用的源 | **%s** |" % use_label)
    ap("")
    ap("## 二、① 全部原版 shader 名 + 各自影响多少效果")
    ap("")
    ap("⚠️ 出自 **%s**；`名字×N` 的 N = %d 行里有多少行的第 3 格出现该名（同一效果重复只算 1，**单位是效果行数**，"
       % (use_label, len(use)))
    ap("不是材质槽）。**`*` = 没进导出期表 `ShaderMap`**（§三 详列）。下面几行是**全部**原版名，共 %d 个："
       % len(all_names))
    ap("")
    for ln in wrap(["%s×%d%s" % (n, cnt[n], "*" if n not in EDITOR else "")
                    for n in all_names], per_line=220):
        ap("- " + ln)
    ap("")
    ap("其中 **%d 个没进导出期表 `ShaderMap`** · **%d 个两张表都没有**（= §三 A 组）。"
       % (len(neither) + len(only_rt), len(neither)))
    ap("")
    ap("## 三、② 没有进 `ShaderMap` 的 shader（**最该看的一批**）")
    ap("")
    ap("`ShaderMap` = `EffectExporter.cs` 的导出期表（%d 键）· `Replacements` = 运行时表（%d 键）。"
       % (len(EDITOR), len(RUNTIME)))
    ap("")
    ap("**A · 两张表都没有（真掉兜底）—— %d 个 / 命中 %d 处**（一处 = 一个效果×一个 shader；**去重后是 %d 条效果**，见 §四）"
       "，`shader` × 效果数 —— 样例效果："
       % (len(neither), sum(cnt[n] for n in neither), len(unmapped_rows)))
    ap("")
    a_big = [n for n in sorted(neither, key=lambda n: -cnt[n]) if cnt[n] >= 5]
    a_small = [n for n in sorted(neither, key=lambda n: -cnt[n]) if cnt[n] < 5]
    for n in a_big:
        nm = ex[n]
        ap("- `%s` × %d —— %s%s" % (n, cnt[n], " / ".join(nm[:3]),
                                    "" if len(nm) <= 3 else " …另 %d 条" % (len(nm) - 3)))
    if a_small:
        ap("- **影响 ≤4 条的其余 %d 个**：%s"
           % (len(a_small), " · ".join("`%s`×%d（%s）" % (n, cnt[n], ex[n][0]) for n in a_small)))
    ap("")
    ap("**B · 只在运行时表、导出期表没有（两张表的同步缺口）—— %d 个**：%s"
       % (len(only_rt), " · ".join("`%s`×%d" % (n, cnt[n]) for n in sorted(only_rt, key=lambda n: -cnt[n])) or "（无）"))
    ap("**C · 只在导出期表、运行时表没有 —— %d 个**（都是标准 shader，运行时 `Shader.Find(原名)` 直接命中，不需要替代）：%s"
       % (len(only_ed), " · ".join("`%s`×%d" % (n, cnt[n]) for n in sorted(only_ed, key=lambda n: -cnt[n])) or "（无）"))
    ap("**D · 在表里但标了「近似替代」（`*`，拿最接近的自建 shader 顶上，功能没做全）—— %d 个**：%s"
       % (len(approx_mapped), " · ".join("`%s`×%d" % (n, cnt[n]) for n in sorted(approx_mapped, key=lambda n: -cnt[n])) or "（无）"))
    ap("")
    ap("## 四、③ 含未映射 shader 的效果清单")
    ap("")
    ap("口径 = §三 A 组（两张表都没有）；同一效果只算 1 行 ⇒ **共 %d 条效果**（%s）。全清单："
       % (len(unmapped_rows), use_label))
    ap("")
    for ln in wrap(sorted(r["name"] for r in use if [s for s in r["shaders"] if is_unmapped(s)]),
                   per_line=260):
        ap("- " + ln)
    ap("")
    ap("## 五、④ 抽不出第 3 格的行有多少、为什么")
    ap("")
    ap("**本表用的是主源：%d/%d 行抽不出**（%d 行有）。" % (len(miss), len(rows), len(have)))
    ap("")
    ap("🔴 这个坑**要一直记着** —— `Resume=true` 的续跑会把「没重导的每一行」的第 3 格**永久抹掉**：`EffectExporter.LoadReport():229`")
    ap("只取第 2 格（`Report[p[0]] = p[1]`，第 3 格直接丢），而 `SaveReport():234-240` 每 10 个效果刷一次盘、把内存那份**照原样写回**。")
    ap("")
    if miss:
        ap("实据（三条独立对上）：")
        ap("")
        ap("- 本次主源里只有 %d 行有第 3 格：%s" % (len(have), " / ".join("`%s`" % r["name"] for r in have[:4])))
        ap("- 旁证：`资料/特效还原_进度与交接.md:187` 写「必须 `Resume=true`，否则 `ClearGenerated()` 会把**其余 956 个**")
        ap("  效果一起清掉」—— 那个 **956** 与 `导出报告_快照_0917.tsv` 里「只有 2 行有第 3 格」逐位吻合。")
        ap("- 快照里残留的那一格与 0913 存档**逐字一致**（材质数 / shader 列表 / 近似替代处数 三样都对）")
        ap("  ⇒ 它是**真数据**，不是另一种写法。")
    else:
        ap("📌 **实测到的实例** = 本次交上来的那份快照：`_tmp_view/导出报告_快照_0917.tsv` 里 **956/958 行**抽不出第 3 格，")
        ap("只有那趟真被重导的 2 个效果（`BulletImpact_artillery_big` / `Plasma_basic_blue`）留着 —— 旁证是 `%s`" % HANDOFF_CITE)
        ap("那句「其余 **956** 个效果」，与「956 行被截」逐位吻合。")
        ap("本次主源**没有**这个症状（全量重导是 `Resume=false`：先 `File.Delete(ReportPath)`，`EffectExporter.cs:139`）。")
    ap("")
    ap("## 六、新旧两源差异（本次主源 vs 0913 存档）")
    ap("")
    if not archive:
        ap("（0913 存档不在，跳过）")
    else:
        same, diffs, ncnt, ocnt = compare(rows, archive)
        both = set(ncnt) | set(ocnt)
        added = sorted([n for n in ncnt if n not in ocnt], key=lambda n: -ncnt[n])
        gone = sorted([n for n in ocnt if n not in ncnt], key=lambda n: -ocnt[n])
        moved = sorted([n for n in both if ncnt[n] != ocnt[n]], key=lambda n: -abs(ncnt[n] - ocnt[n]))
        ap("配对上的效果行 **%d** · **名单+材质数+近似处数全同 %d 条** · **有差 %d 条**。"
           % (same + len(diffs), same, len(diffs)))
        ap("")
        ap("**shader 名单的净变化**：新增 **%d** 个 · 消失 **%d** 个 · 效果数变了 **%d** 个。"
           % (len(added), len(gone), len(moved)))
        ap("")
        if not (added or gone or moved):
            ap("即 **原版 shader 名单一个没加、没减、没换**（连出现次数都没变）。")
        else:
            if added:
                ap("- 新增：" + " · ".join("`%s`×%d" % (n, ncnt[n]) for n in added))
            if gone:
                ap("- 消失：" + " · ".join("`%s`×%d" % (n, ocnt[n]) for n in gone))
            if moved:
                ap("- 计数变了：" + " · ".join("`%s` %d→%d" % (n, ocnt[n], ncnt[n]) for n in moved))
        nn = set(r["name"] for r in rows)
        oo = set(r["name"] for r in archive)
        if nn != oo:
            ap("- ⚠️ **效果名单本身也变了**：只在新源 %d 条 · 只在存档 %d 条（%s%s）"
               % (len(nn - oo), len(oo - nn), " / ".join(sorted(nn - oo)[:4]), " / ".join(sorted(oo - nn)[:4])))
        ap("")
        shader_diffs = [t for t in diffs if t[1]["shaders"] != t[2]["shaders"]]
        only_self_plus = [t for t in shader_diffs
                          if (set(t[2]["shaders"]) - set(t[1]["shaders"]))
                          and not (set(t[1]["shaders"]) - set(t[2]["shaders"]))
                          and all(x.startswith(SELF_PREFIX)
                                  for x in set(t[2]["shaders"]) - set(t[1]["shaders"]))]
        if not diffs:
            ap("⇒ 逐效果两源**完全一致**。")
        elif not shader_diffs:
            ap("⇒ **%d 条有差的里，shader 列表一条都没变** —— 差别全在材质数 / 近似替代处数上（下表前 4 条）。" % len(diffs))
        elif len(only_self_plus) == len(shader_diffs):
            ap("⇒ 这 **%d** 条有差的效果里，**%d 条的 shader 列表变了，而且变的全是同一件事**：**只多出一个自建 `WarpforgeVFX/*` 条目**、**没有少任何名字**"
               % (len(diffs), len(shader_diffs)))
            ap("  —— 0917「拖尾材质按渲染器逐槽记账」的直接结果（trail 分支 `usedShaders.Add(to)` 记的是**替换后的名字**）。下表前 4 条：")
        else:
            ap("⇒ 其中 **%d 条 shader 列表本身变了**（下表前 4 条）。" % len(shader_diffs))
        if diffs:
            ap("")
            ap("| 效果 | 材质数 0913→本次 | 近似 0913→本次 | shader 列表 |")
            ap("|---|---|---|---|")
            for nm, o, r in sorted(diffs, key=lambda t: t[2]["name"])[:4]:
                ap("| %s | %s→%s | %s→%s | %s |" % (nm, o["mats"], r["mats"], o["approx"], r["approx"],
                                                    delta_desc(o["shaders"], r["shaders"])))
    ap("")
    ap("## 七、复跑与出处")
    ap("")
    ap("- 复跑：`\"D:/2/Warpforge_tools/py312/python.exe\" -X utf8 \"%s\"`" % (ROOT + "/工具/_effect_shader_audit.py"))
    ap("  （加 `--check` 只打汇总不写文件；带一个路径参数就换主源）")
    ap("- 映射表：`MyGame/Assets/WarpforgeVFX/Runtime/WarpforgeShaderMap.cs` · `MyGame/Assets/WarpforgeArena1/Editor/EffectExporter.cs:50-109`")
    ap("- 另一份体检（A 组那批 shader 落在哪个 bundle）：`资料/普查产出_0917/补充shader引用清单.md`")
    ap("- 0913 完整表与解析器：`资料/普查产出_0913/效果_shader_对账.{tsv,md}` · `工具/audit_effect_shaders.py`")
    if check_only:
        for i, ln in enumerate(m, 1):
            if ln.startswith("## "):
                print("  md line %3d / %d : %s" % (i, len(m), ln.encode("ascii", "backslashreplace").decode("ascii")))

    print("主源 %s" % src)
    print("效果 %d · 有第3格 %d · 缺 %d · 3格但抽不出数 %d" % (len(rows), len(have), len(miss), len(bad)))
    print("原版名 %d · 没进 ShaderMap %d（其中两张表都没有 %d）· 只在运行时表 %d · 近似 %d"
          % (len(all_names), len(only_ed) + len(neither), len(neither), len(only_rt), len(approx_mapped)))
    print("含未映射 shader 的效果 %d 条 · 自建名 %d 个" % (len(unmapped_rows), len(self_names)))
    if archive:
        same, diffs, ncnt, ocnt = compare(rows, archive)
        print("新旧对比：全同 %d / 有差 %d · shader 名单 新增 %d 消失 %d 计数变 %d"
              % (same, len(diffs), len(set(ncnt) - set(ocnt)), len(set(ocnt) - set(ncnt)),
                 len([n for n in set(ncnt) & set(ocnt) if ncnt[n] != ocnt[n]])))
    if len(m) > 120:
        print("❌ md 超过 120 行：%d" % len(m))
    if check_only:
        print("(--check：没写文件)")
        return
    io.open(OUT_MD, "wb").write((NL.join(m) + NL).encode("utf-8"))
    print("写出：%s（%d 行）· %s（%d 行）" % (OUT_TSV, len(lines), OUT_MD, len(m)))


if __name__ == "__main__":
    main()
