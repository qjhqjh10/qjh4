#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""_extract_dropped_props.py —— 把 EffectExporter 日志里「替代 shader 认不出源属性」那批警告，
提炼成一份可长期保存的清单。

为什么要这个工具：那批警告的原始文本只在 `_tmp_view/`（**gitignore 的临时目录，会被清空**），
而它记的是「**我们自建的替代 shader 比原版少了哪些属性**」—— 一直存在、只是 2026-09-17 之前
那条路是静默的（`if (!mat.HasProperty(tp)) continue;`）。

用法：
  "D:/2/Warpforge_tools/py312/python.exe" _extract_dropped_props.py <日志> <输出.md>
例：
  ... _extract_dropped_props.py d:/4/_tmp_view/export_0917.log \
      d:/4/Unity/资料/普查产出_0917/替代shader丢失的源属性_0917.md
"""
import collections
import io
import re
import sys

PAT = re.compile(r"目标 shader (.+?) 认不出 (\d+) 个属性，\*\*这些值没搬过去\*\*（材质 (.+?)）：(.*)$")

# 属性名 → 它大概是干什么的（人读用；不确定的一律留空，别猜）
NOTE = {
    "_Noise": "噪声图", "_NoiseTex1": "噪声图", "_NoiseTex2": "噪声图", "_FireNoise": "火焰噪声图",
    "_SecondaryTex": "第二张图（叠加层）", "_Disolve": "溶解遮罩图", "_EmissionTex": "发光图",
    "_EmissionColor": "发光颜色（**WFMatcap 没有这个属性 ⇒ 24 个材质的发光被丢**）",
    "_MatCap": "MatCap 贴图", "_Matcap": "同上（大小写不同，是另一个属性名）",
    "_RimColor": "边缘光颜色", "_BorderColor1": "边框色 1", "_BorderColor2": "边框色 2",
    "_Color1": "渐变色 1", "_Color2": "渐变色 2", "_Color3": "渐变色 3", "_Add_Color": "附加色",
    "_ExtraSine": "附加正弦纹理", "_DistortMap": "扭曲图", "_MinTex": "遮罩图", "_Normals": "法线图",
}


def main(log_path, out_path):
    rows = []
    by_shader = collections.defaultdict(lambda: [collections.Counter(), collections.Counter()])
    for line in io.open(log_path, encoding="utf-8", errors="ignore"):
        m = PAT.search(line.rstrip())
        if not m:
            continue
        sh, n, mat, items = m.group(1), int(m.group(2)), m.group(3), m.group(4)
        rows.append((sh, mat, n, items))
        by_shader[sh][0][mat] += n
        for it in items.split(" / "):
            key = it.split("=")[0].strip()
            if key:
                by_shader[sh][1][key] += 1

    all_props = collections.Counter()
    for sh, (mats, props) in by_shader.items():
        all_props.update(props)

    o = io.open(out_path, "w", encoding="utf-8", newline="\n")
    w = o.write
    w("# 替代 shader 丢掉的源属性（2026-09-17 全量重导实测）\n\n")
    w("> 数据来源：`EffectExporter.Run` 全量重导的日志 —— **`_tmp_view/` 是 gitignore 的，会被清空**，\n")
    w("> 所以结论落在这里。生成本文件的工具：`Unity/工具/_extract_dropped_props.py`（可复跑）。\n")
    w("> 产出它的代码：`EffectExporter.ImportMaterial` 里那段「源上有真值、目标 shader 却不认」的警告\n")
    w("> —— **2026-09-17 新加的**；在此之前这条路是**静默**的（裸 `continue`），所以这份账以前根本不存在。\n\n")
    w("## 一句话\n\n")
    w("这份清单是「**我们自建的替代 shader 比原版少了哪些属性**」的第一份有据可查的账。\n")
    w("**不是新 bug** —— 是一直流失、只是以前一声不吭的**还原度缺口**。修不修、怎么修要单独拍板：\n")
    w("每个属性都要先回原版 shader 的属性表（`资料/普查产出_0917/shader属性表_汇总.md`）确认语义，\n")
    w("**别按属性名猜**（本项目在「token 名 ≠ 语义」上踩过）。\n\n")
    w("## 汇总（按目标 shader）\n\n")
    w("| 目标 shader | 受影响材质 | 丢掉的属性条数 | 丢得最多的属性 |\n|---|---:|---:|---|\n")
    for sh, (mats, props) in sorted(by_shader.items(), key=lambda kv: -sum(kv[1][0].values())):
        top = ", ".join("%s×%d" % (k, v) for k, v in props.most_common(6))
        w("| `%s` | %d | %d | %s |\n" % (sh, len(mats), sum(mats.values()), top))

    w("\n## 属性频次（全局，前 40）\n\n")
    w("| 属性名 | 次数 | 像什么 |\n|---|---:|---|\n")
    for k, v in all_props.most_common(40):
        w("| `%s` | %d | %s |\n" % (k, v, NOTE.get(k, "")))

    w("\n## 逐材质明细（%d 条）\n\n" % len(rows))
    w("| 目标 shader | 材质 | 丢掉条数 | 具体属性 |\n|---|---|---:|---|\n")
    for sh, mat, n, items in sorted(rows):
        w("| `%s` | %s | %d | %s |\n" % (sh, mat, n, items.replace("|", "\\|")))
    o.close()
    return len(rows), len(by_shader)


if __name__ == "__main__":
    if len(sys.argv) < 3:
        print(__doc__)
        sys.exit(2)
    n, s = main(sys.argv[1], sys.argv[2])
    print("OK 逐材质 %d 条 / 目标 shader %d 个 → %s" % (n, s, sys.argv[2]))
