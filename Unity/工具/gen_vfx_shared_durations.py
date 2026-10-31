# -*- coding: utf-8 -*-
"""gen_vfx_shared_durations.py — 把**共享攻击件的 `AnimInfo` 时长**烘成一张运行期表。

背景（B107_W7 · 特效线施工单 v2 §2 的 `VFX-2`）
------------------------------------------------
`CardPresentation/Core/VfxMap.cs` 的 `SharedRangedVfxDuration` 原来是一张**手写 7 行**的
`Dictionary<string, float>` —— 「数据抄进代码」。真正的原因只是
`数据/游戏数据/animinfo_lookup.json` **不在 `Assets` 里**（运行期读不到）。本生成器把它搬出来。

数据从哪来（**唯一输入**）
--------------------------
`d:/4/Unity/数据/游戏数据/animinfo_lookup.json`（现读 418 键）—— **键 = prefab 名**，
值里的 `m` = 它对应的 CardAnim 名，`ts` / `tm` / `te` = `timeAtStartPos` / `timeMoving` / `timeAtEndPos`。
时长口径与原版 `AnimInfo.GetAnimDuration()` 一致 = **`ts + tm + te`**（该表 418 条里没有一条
`vfxDelay != 0` ⇒ 这里不加 Delay）。

收哪些件（`SHARED` 清单）
-------------------------
只收 **VfxMap 里「共享件」那 7 个 prefab 名**（`ByFaction[AttackRanged]` 4 条 +
`ByEvent[AttackRanged]` 1 条 + 1-shot 那一档 + 近战那件）。逐卡件**不进来** ——
它们的时长在 `CardDef.AnimTimes` 里（逐卡真值优先）。

⚠️ `expect` 列**不是时长**，是「这个 prefab 名对应哪一张 CardAnim」的语义钉子：
现读 **215 个 prefab 名被 ≥2 张 CardAnim 共用**（参数/时长不同，`_quick` / `_nurgle` 是**另一张**）。
数据里 `m` 一旦指到别的 CardAnim，本脚本**报错退出、不写盘** —— 否则会静默把 0.75 写成 0.21。
⛔ 不在这里抄一份「期望时长」：时长唯一真值 = `animinfo_lookup.json`，
手写表那一份只作**运行期交叉校验副本**（见 `VfxMap.LookupSharedDuration`）。

输出
----
`d:/4/Unity/MyGame/Assets/CardPresentation/Resources/vfx_shared_durations.json` —— 运行期那份。
形状（`JsonUtility` 只吃**数组**，不吃字典）：
    { "schema": 1, "count": N, "source": "数据/游戏数据/animinfo_lookup.json",
      "note": "...", "rows": [ {"n": prefab 名, "a": CardAnim 名,
                                 "ts": 0.0, "tm": 0.0, "te": 0.0, "d": ts+tm+te}, … ] }
**确定性**：`rows` 按 `n` 排序、浮点统一保留 6 位（去掉 float32 尾巴）、
固定 `indent=1` + 末尾一个换行 ⇒ 同样的输入**逐字节相同**。

怎么跑
------
    "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_vfx_shared_durations.py --write
    "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_vfx_shared_durations.py --check

⚠️ **不带显式动作参数（`--write` / `--check`）时只打印用法、一个字节都不写。**
"""

import argparse
import io
import json
import os
import sys

DATA = r"d:/4/Unity/数据/游戏数据/animinfo_lookup.json"
OUT = r"d:/4/Unity/MyGame/Assets/CardPresentation/Resources/vfx_shared_durations.json"
SOURCE_LABEL = "数据/游戏数据/animinfo_lookup.json"

# (prefab 名, 期望的 CardAnim 名, 是否必须存在)
# ⚠️ `BulletImpactBurst_crowd` 已无生产路径（只给试片台/历史引用）⇒ **不要求必须存在**；
#    数据里有它就照写（保留值），没有就跳过并打印提示。
SHARED = [
    # ===== `ByFaction[AttackRanged]` 那 4 条（`card_vfx_by_card.json` generic.attackRangedByArmy）=====
    ("BulletImpact_2shot_trail",           "Atk_BulletImpact_2shot",       True),   # Ultramarines
    ("BulletImpact_2shot_trail_ork_NEW",   "Atk_BulletImpact_2shot_ork",   True),   # Goff
    ("NecronGauss",                        "Atk_Gauss_Basic",              True),   # Sautekh
    ("BulletImpact_2shot_trail_chaos",     "Atk_BulletImpact_2shot_chaos", True),   # BlackLegion
    # ===== `ByEvent[AttackRanged]`（没配专属粒子的阵营走它；兜底名 = 上面第一行那条）=====
    ("BulletImpact_2shot",                 "BulletImpact_2shot",           True),   # 仅逐卡 `UM34` 引用
    # ===== 1-shot 那一档（同名 prefab 的原版时长，自检拿它当第三档尺子）=====
    ("BulletImpact_1shot_trail",           "Atk_BulletImpact_1shot",       True),
    # ===== 近战那件（原版无通用近战件；只给试片台/历史引用）=====
    ("BulletImpactBurst_crowd",            "BulletImpactBurst_ability_unit", False),
]

USAGE = """用法：
  python gen_vfx_shared_durations.py --write   # 从 animinfo_lookup.json 生成运行期表并写盘
  python gen_vfx_shared_durations.py --check   # 只比对盘上那份与重新生成的结果，不写盘

输入：%s
输出：%s

⛔ 不带 --write / --check 时只打印本用法，不写任何文件。
""" % (DATA, OUT)


def _num(v, where):
    if isinstance(v, bool) or not isinstance(v, (int, float)):
        raise SystemExit("ERROR: %s 的 ts/tm/te 不是数字：%r" % (where, v))
    if v < 0:
        raise SystemExit("ERROR: %s 的 ts/tm/te 是负数：%r" % (where, v))
    return float(v)


def build():
    if not os.path.isfile(DATA):
        raise SystemExit("ERROR: 输入不存在：%s" % DATA)
    with io.open(DATA, encoding="utf-8") as fh:
        src = json.load(fh)
    if not isinstance(src, dict):
        raise SystemExit("ERROR: 输入不是字典：%s" % DATA)

    rows = []
    problems = []
    skipped = []
    for name, cardanim, required in SHARED:
        e = src.get(name)
        if e is None:
            if required:
                problems.append("MISSING  %s（数据表里没有这个 prefab 名）" % name)
            else:
                skipped.append("%s（可选件，数据表里没有）" % name)
            continue
        if not isinstance(e, dict):
            problems.append("BAD      %s（值不是对象）" % name)
            continue
        got_anim = str(e.get("m") or "")
        if got_anim != cardanim:
            problems.append("CARDANIM %s：期望 `%s`，数据里是 `%s`（同名 prefab 被多张 CardAnim 共用，"
                            "⛔ 指错会把时长写错）" % (name, cardanim, got_anim or "(空)"))
            continue
        ts = round(_num(e.get("ts", 0), name + ".ts"), 6)
        tm = round(_num(e.get("tm", 0), name + ".tm"), 6)
        te = round(_num(e.get("te", 0), name + ".te"), 6)
        d = round(ts + tm + te, 6)
        rows.append({"n": name, "a": cardanim, "ts": ts, "tm": tm, "te": te, "d": d})

    rows.sort(key=lambda r: r["n"])
    out = {
        "schema": 1,
        "count": len(rows),
        "source": SOURCE_LABEL,
        "note": "由 工具/gen_vfx_shared_durations.py 生成（⛔ 别手改）。"
                "键 n = prefab 名 · d = AnimInfo.GetAnimDuration() = ts+tm+te。",
        "rows": rows,
    }
    return out, problems, skipped


def dump_bytes(out):
    return (json.dumps(out, ensure_ascii=False, indent=1, sort_keys=False) + "\n").encode("utf-8")


def main(argv=None):
    ap = argparse.ArgumentParser(add_help=True)
    g = ap.add_mutually_exclusive_group()
    g.add_argument("--write", action="store_true", help="生成并写盘")
    g.add_argument("--check", action="store_true", help="只比对盘上那份，不写")
    args = ap.parse_args(argv)

    if not args.write and not args.check:
        sys.stdout.write(USAGE)
        return 2

    out, problems, skipped = build()
    for r in out["rows"]:
        print("  %-34s -> %-32s ts=%.3f tm=%.3f te=%.3f  d=%.3f"
              % (r["n"], r["a"], r["ts"], r["tm"], r["te"], r["d"]))
    print("rows                     : %d（清单 %d 条）" % (out["count"], len(SHARED)))
    for s in skipped:
        print("  SKIP %s" % s)
    if problems:
        print("⚠️ 问题 %d 条（**不写盘**）：" % len(problems))
        for p in problems:
            print("   " + p)
        return 1

    b = dump_bytes(out)
    if args.check:
        if not os.path.isfile(OUT):
            print("MISSING " + OUT)
            return 1
        cur = io.open(OUT, "rb").read()
        if cur != b:
            print("DIFF " + OUT)
            return 1
        print("check: OK（盘上与重新生成逐字节相同，%d B）" % len(b))
        return 0

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    io.open(OUT, "wb").write(b)
    print("wrote %s  (%d B)" % (OUT, len(b)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
