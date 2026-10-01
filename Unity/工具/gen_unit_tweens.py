# -*- coding: utf-8 -*-
"""把原版 74 个 `UnitTweenSO` 的 JSON 压成**运行期那张表**。

为什么要这一步：
  `WarpforgeVFX.WFModuleTween` 的 `tweenAnims` 里存的是 UnitTweenSO 的**资产名**，
  而「这个名字对应哪几条补间」原版是**运行时读 ScriptableObject**。
  我们的 `Assets/` 里**没有**那份 SO（它只在 `d:/2` 的导出里），所以要把值抽成一张
  `Resources/` 下的 JSON，运行时按名字查 —— 与 `OffensiveCards.json` /
  `EnvironmentConditions.json` / `VfxMap` 那几件**同一个模式**。

数据源：`d:/4/Unity/数据/游戏数据/tween/*.json`（74 个，由解包管线导出）
判据（字段语义）：`d:/2/tools/decomp_full/` 里那 7 个 `*Tween__GetTween.c` +
  `TweenInfoBase__GetTween.c` + `BattleManagerSupport__GetTargetUnit.c`。

输出：`MyGame/Assets/CardPresentation/Resources/UnitTweens.json`

⚠️ 只搬**值**，不做任何解释 —— 解释在 C# 那边（`Core/UnitTweenTable.cs`）。
用法：`python d:/4/Unity/工具/gen_unit_tweens.py`
"""
import io
import json
import os
import glob

SRC = "d:/4/Unity/数据/游戏数据/tween"
DST = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/UnitTweens.json"

# 类名 → 我们这边的 kind（小写；C# 侧 switch 用）
KIND = {
    "ScaleTween": "scale",
    "MoveTween": "move",
    "RotateTween": "rotate",
    "PunchTween": "punch",
    "ShakeTween": "shake",
    "ResetBodyTween": "resetbody",
    "DelayTween": "delay",
}

# 每个 tween 类**共有**的那几个字段（`TweenInfoBase`）：直接搬；其余按类搬
COMMON = ("muted", "appendType", "duration", "delay", "loops", "loopType",
          "ease", "relative", "from", "targetUnit", "awayFromUnit", "isLocal")


def vec(d, k):
    v = d.get(k)
    if not isinstance(v, dict):
        return None
    return [round(float(v.get("x", 0.0)), 6),
            round(float(v.get("y", 0.0)), 6),
            round(float(v.get("z", 0.0)), 6)]


def one(ref):
    """一条 `TweenInfoBase` → 我们那份紧凑记录。**只搬值**。"""
    cls = ((ref.get("type") or {}).get("class") or "").strip()
    d = ref.get("data") or {}
    kind = KIND.get(cls)
    if kind is None:
        return {"kind": "unknown", "class": cls}          # 认不出就如实带出来（C# 侧会出声）

    r = {"kind": kind}
    for k in COMMON:
        if k in d:
            r[k] = d[k]
    # 各子类自己的负载
    v = vec(d, "scale") or vec(d, "movement") or vec(d, "rotation") or vec(d, "punch") \
        or vec(d, "strength")
    if v is not None:
        r["vec"] = v
    for k in ("punchType", "shakeType", "rotationMode", "vibrato", "vibratto",
              "elasticity", "randomness", "fadeOut",
              "resetPosition", "resetRotation", "resetScale"):
        if k in d:
            r[k] = d[k]
    # `curve` 的 `m_Curve` 在全部 74 份里都是**空列表**（实测）⇒ 不搬；
    # 真要用了再回来加（空搬一份空数组只是白占体积）。
    return r


def main():
    files = sorted(glob.glob(os.path.join(SRC, "*.json")))
    items, unknown, empty_curve = [], [], 0
    for f in files:
        d = json.load(io.open(f, encoding="utf-8"))
        name = d.get("m_Name") or os.path.splitext(os.path.basename(f))[0]
        refs = (d.get("references") or {}).get("RefIds") or []
        order = [(x or {}).get("rid") for x in ((d.get("tweens") or {}).get("list") or [])]
        by_rid = {r.get("rid"): r for r in refs}
        tweens = []
        for rid in order:
            r = by_rid.get(rid)
            if r is None:
                continue
            for cr in ((r.get("data") or {}).get("curve") or {}).get("m_Curve") or []:
                empty_curve += 1
            t = one(r)
            if t.get("kind") == "unknown":
                unknown.append((name, t.get("class")))
            tweens.append(t)
        items.append({
            "name": name,
            "wait": int(d.get("waitAnimation") or 0),        # `UnitTweenSO.waitAnimation`（+0x18）
            "useCallback": int(d.get("useCallBack") or 0),
            "callbackTime": float(d.get("callBackTime") or 0.0),
            "tweens": tweens,
        })

    src = {"items": items}
    txt = json.dumps(src, ensure_ascii=False, indent=1)
    with io.open(DST, "wb") as fp:                      # ⚠️ 二进制写：别让 python 把行尾翻掉
        fp.write(txt.replace("\n", "\r\n").encode("utf-8"))

    kinds = {}
    for it in items:
        for t in it["tweens"]:
            kinds[t["kind"]] = kinds.get(t["kind"], 0) + 1
    total = sum(kinds.values())
    waits = sum(1 for it in items if it["wait"])
    print("源文件 %d 份 → 表内 %d 条" % (len(files), len(items)))
    print("补间条目 %d 条，分布：" % total, dict(sorted(kinds.items(), key=lambda x: -x[1])))
    print("waitAnimation=1 的 SO：%d/%d（原版那 6 个）" % (waits, len(items)))
    print("认不出的类：%d" % len(unknown), unknown[:5])
    print("非空 curve 关键帧：%d（应为 0）" % empty_curve)
    print("写出 →", DST, "%.1f KB" % (len(txt.encode('utf-8')) / 1024.0))


main()
