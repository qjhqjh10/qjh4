# -*- coding: utf-8 -*-
"""_gen_B110_R5.py —— 生成账补丁 `_parts/B110_R5.tsv`（11 列）。

做什么：把主表 `特效动画.tsv` 里 `ScenarioGenericMaterialBlend` 的三行
（`DoBlend` / `DoScenarioBlend` / `Init`）**差异 + 备注 + 轮次**三列改写，反映
2026-10-31 `B110_R5` 的唯一行为变化：

  原来这三行都写着「`customMaterial` 恒 null（原版那件 Material 我们工程里没有）」
  ⇒ 现在 `ScenarioBlendableFactory.TryLoadOriginalMaterial` **按原版材质名从随包
  `StreamingAssets/WarpforgeVFX/*.bundle` 取原件并克隆**（取不到才 null + 出声一次）。
  ⇒ 有原件时走原版那条 `renderer.material = customMaterial`；没打进包时退回旧行为。

其余列**逐字照抄主表**（状态 A / 判据出处 / 落点 等都不动）。合并时按 (原版类,方法) 键覆盖。

⛔ 不允许把 `--help` 当动作：**不带参就是打印用法并退出 0**；写盘必须 `--write`。
"""
import io
import os
import sys

LEDGER = r"d:/4/Unity/资料/完全复刻/账/特效动画.tsv"
OUT = r"d:/4/Unity/资料/完全复刻/账/_parts/B110_R5.tsv"
COLS = ["域", "原版类", "方法", "判据出处", "判据要点", "状态", "我们的落点",
        "差异", "等级", "轮次", "备注"]

# (方法, 新差异, 新轮次, 新备注追加)
EDITS = {
    "DoBlend(float target, float blendSpeed, bool instant)": (
        "一致，多了「属性不存在⇒出声一次」。🆕 B110_R5（2026-10-31）：`customMaterial` 不再恒 null —— "
        "`ScenarioBlendableFactory.TryLoadOriginalMaterial` 按原版材质名从随包 "
        "`StreamingAssets/WarpforgeVFX/*.bundle` 取原件并克隆（取不到才 null + 出声一次）"
        "⇒ 有原件时走原版那条 `renderer.material = customMaterial`；没打进包时退回旧行为（改现有材质同名属性）",
        "R5｜B110_R5",
        "｜🆕 B110_R5：取件口 = `CardPresentation/Battle/ScenarioBlendables.cs` 的 `TryLoadOriginalMaterial`"
        "（依赖调度台把该 Material + shader + 贴图打进 `wf_arena_*.bundle`；判据 → "
        "`判据/B110_R5_资源侧放回11条.md` §B-42）",
    ),
    "DoScenarioBlend(ScenarioBlendOptions blendOptions)": (
        "逐句照（含 doFade=!doFade 与两处 NRE 出声）。🆕 B110_R5（2026-10-31）：原版 "
        "`renderer.material = customMaterial` 那条**现在可达** —— 该 Material 由 "
        "`TryLoadOriginalMaterial` 从随包 bundle 取并克隆；取不到时退回旧行为（改现有材质同名属性 + 出声）",
        "R5｜B110_R5",
        "｜🆕 B110_R5：同上（`ScenarioBlendables.cs:TryLoadOriginalMaterial`）",
    ),
    "Init(float startValue)": (
        "同 DoScenarioBlend：🆕 B110_R5（2026-10-31）起 `customMaterial` 由 "
        "`TryLoadOriginalMaterial` 从随包 bundle 取原件并克隆（取不到才 null + 出声）；"
        "dump 记的默认 1 一次没用到（三处调用点全传 0）",
        "R5｜B110_R5",
        "｜🆕 B110_R5：同上",
    ),
}


def main():
    if "--write" not in sys.argv:
        print(__doc__)
        print("用法：python -I -X utf8 %s --write" % os.path.basename(__file__))
        return 0
    raw = io.open(LEDGER, "rb").read()
    if b"\r" in raw:
        print("!! 主表里有 CR —— 先确认行尾，别把补丁写成混行尾")
        return 2
    lines = raw.decode("utf-8").split("\n")
    hdr = [h.strip() for h in lines[0].split("\t")]
    if hdr != COLS:
        print("!! 主表表头不是那 11 列：%r" % hdr)
        return 2
    out = ["\t".join(COLS)]
    hit = set()
    for l in lines[1:]:
        if not l.strip():
            continue
        f = l.split("\t")
        if len(f) != 11:
            print("!! 主表有一行列数 %d（跳过）：%r" % (len(f), l[:80]))
            continue
        if f[1].strip() == "ScenarioGenericMaterialBlend" and f[2].strip() in EDITS:
            new_diff, new_round, extra_note = EDITS[f[2].strip()]
            f[7] = new_diff
            f[9] = new_round
            f[10] = f[10] + extra_note
            hit.add(f[2].strip())
        if f[1].strip() == "ScenarioGenericMaterialBlend" and f[2].strip() in EDITS:
            out.append("\t".join(f))
    missing = set(EDITS) - hit
    if missing:
        print("!! 主表里没找到这些方法：%r" % sorted(missing))
        return 2
    buf = "\n".join(out) + "\n"
    # ⚠️ 主表本来就有**空列**（`等级` 那列这三行是空的）⇒ 连续 tab 是合法的，⛔ 别拿它当错误。
    #    只校验「每行列数 == 11」即可（上面已经逐行查过）。
    with io.open(OUT, "wb") as f:
        f.write(buf.encode("utf-8"))
    print("已写 %s（%d 行数据）" % (OUT, len(out) - 1))
    return 0


if __name__ == "__main__":
    sys.exit(main())
