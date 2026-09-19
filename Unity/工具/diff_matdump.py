# -*- coding: utf-8 -*-
"""解析 `EffectCompare` 的材质 dump（`WFCMP_MATDUMP=1`），逐属性 diff 原版 / 导出。

用法：
    unset ELECTRON_RUN_AS_NODE && WFCMP_MATDUMP=1 WFCMP_SIMT=0.4 \
      "$UNITY" -batchmode -quit -projectPath "D:\\4\\Unity\\MyGame" \
      -executeMethod EffectCompare.Run -logFile "d:/4/_tmp_view/matdump.log"
    python 工具/diff_matdump.py d:/4/_tmp_view/matdump.log

为什么要它：`ParticleModuleProbe` 读的是**磁盘上的 `.mat`（＝导出时的占位）**，
看不到 `binder.Apply()` **重建之后**的状态；而「偏暗/偏亮」常出在
「某个 float/color/贴图没灌进来、吃了默认值」上。这份 diff 就是那件事的判据。

判据（**别只看"几处不同"这个数**，要看**哪些字段**不同 —— 与 `ParticleModuleProbe` 同一条规矩）：
  · 一侧 `<null>` / 另一侧有值  ⇒ **真丢了**（最重）
  · 贴图**名字**不同            ⇒ 配错了贴图
  · 只有**尺寸**不同            ⇒ 多半是导入格式/尺寸差异，先降级
  · float / color 数值不同      ⇒ 灌值没灌对
"""
import io
import re
import sys
from collections import OrderedDict

MD = re.compile(r"^MD (.*?) \| mat=(.*?) \| shader=(.*?) \| queue=(-?\d+) \| keys=\[(.*?)\](.*)$")
KV = re.compile(r"([A-Za-z_][A-Za-z0-9_]*)=(\S+)")


def parse(path):
    """→ [(side, key, {attr: val})]，side ∈ {'orig','exp'}，key = (层级路径, 材质名)"""
    out = []
    side = None
    cur = None            # 当前效果名（`比对 X（原版）` 那行给的）
    for ln in io.open(path, encoding="utf-8", errors="replace"):
        ln = ln.rstrip("\n")
        m = re.search(r"比对 (.+?)（(原版|导出)）", ln)
        if m:
            cur, side = m.group(1), ("orig" if m.group(2) == "原版" else "exp")
            continue
        if not ln.startswith("MD "):
            continue
        m = MD.match(ln)
        if not m:
            continue
        p, mat, shader, queue, keys, rest = m.groups()
        kv = OrderedDict()
        kv["__shader"] = shader
        kv["__queue"] = queue
        kv["__keys"] = keys
        for k, v in KV.findall(rest):
            kv[k] = v
        out.append((side, cur, (p, mat), kv))
    return out


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        return 1
    recs = parse(sys.argv[1])
    groups = OrderedDict()
    for side, eff, key, kv in recs:
        groups.setdefault(eff, {"orig": OrderedDict(), "exp": OrderedDict()})
        groups[eff][side][key] = kv

    total = 0
    for eff, d in groups.items():
        o, e = d["orig"], d["exp"]
        diffs = []
        for key in o:
            if key not in e:
                diffs.append((key, "**只有原版有**", "", ""))
                continue
            a, b = o[key], e[key]
            for k in a:
                va, vb = a.get(k), b.get(k)
                if va != vb:
                    diffs.append((key, k, va, vb))
            for k in b:
                if k not in a:
                    diffs.append((key, k, "", b[k]))
        for key in e:
            if key not in o:
                diffs.append((key, "**只有导出有**", "", ""))
        total += len(diffs)
        print(f"\n=== {eff} —— {len(diffs)} 处不同 ===")
        for (p, mat), k, va, vb in diffs:
            print(f"  [{p}] {k}\n      原版: {va}\n      导出: {vb}")

    print(f"\n=== 合计 {total} 处 ===")
    print("⚠️ 判据看「哪些字段」不同，别看这个数 —— 贴图尺寸差异是导入格式噪声，先降级。")
    return 0


if __name__ == "__main__":
    sys.exit(main())
