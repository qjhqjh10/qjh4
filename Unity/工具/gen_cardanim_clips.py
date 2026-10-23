# -*- coding: utf-8 -*-
"""gen_cardanim_clips.py — 把**逐 CardAnim 的 `AnimInfo` 位置参数**烘成一张运行期表。

背景
----
`Battle/CardAnimClips.cs` 原来把这 16 行（`UnitAttack(50)` 12 个有件的 + `UnitDamaged(60)` 4 个）
**硬写在一个 C# 字典里**（2026-10-23 第一版，`W63` 收口）。那是「数据抄进代码」，而真正的原因只是
`数据/游戏数据/animinfo_lookup.json` **不在 `Assets` 里**（运行期读不到）。本生成器把它搬出来。

数据从哪来（**第一权威**）
--------------------------
`d:/2/新解包资源/assets_full/bundle_*cardanims*_assets_all/MonoBehaviour/<CardAnim>.json` 的
`animInfo` —— 逐字段：
    `animAdressable.m_AssetGUID`（→ prefab 名）
    `startPosOption`(0x18) · `endPosOption`(0x1C) · `pointTowardsOption`(0x20)
    `shouldMoveVFX`(0x38) · `vfxDelayTime`(0x24) · `timeAtStartPos`(0x2C) · `timeMoving`(0x30)
字段名与偏移的判据：`d:/2/tools/il2cpp_out/dump.cs:25205-25243`（`AnimInfo` 类）。

CardAnim 名 → prefab 名（GUID → 资产名）这一跳**不在本脚本里重算** —— 直接用
`d:/4/Unity/数据/索引/anim_address_map.json` 的 `cardanim_to_asset`（由 `工具/gen_anim_address_map.py`
从 bundle 的 `m_Container` 反解出来的，**10,564** 条 `guid_to_asset` / 1,099 条 `cardanim_to_asset`；
⚠️ 2026-10-23 现读（原写 10,539 —— `T14` 的二进制 catalog 补集 +25 条；本脚本只读 `cardanim_to_asset`，那一节未变）。

输出（**两份逐字节相同**）
--------------------------
1. `d:/4/Unity/数据/游戏数据/cardanim_clips.json`   —— 全量、给工具/人看
2. `d:/4/Unity/MyGame/Assets/CardPresentation/Resources/cardanim_clips.json` —— 运行期那份

形状（`JsonUtility` 只吃**数组**，不吃字典）：
    { "schema": 1, "count": N, "rows": [ {"n": prefab 名, "a": CardAnim 名,
                                          "sp":0,"ep":0,"pt":0,"mv":0,"vd":0,"ts":0,"tm":0,"te":0}, … ] }
· 🔴 **键 = `CardAnim` 资产名（`a`），⛔ 不是 prefab 名**：同一个 prefab 名会被多张 `CardAnim`
  共用、参数却不同（2026-10-23 现读 **215 组**，例：`Trait_GainQuest` 被
  `Trait_GainQuest_self`(sp=0) 与 `Trait_GainQuest_enemywarlord`(sp=4) 共用）⇒ 按 prefab 名查必错。
  运行期那一侧由 `VfxMap.ResolvedVfx.Anim`（= `CardPick.A`）把 `CardAnim` 名交出来。
· `n`（prefab 名）仍然带上，只作**诊断/人读**用。
· **按 `a` 升序**排（确定性）。
· 🔴 **不写时间戳** ⇒ 连跑两遍 **md5 逐字节相同**（幂等）。

同名 prefab 怎么办
------------------
**不合并**：每个 `CardAnim` 各一行（键是 `a`）⇒ 215 组「同 prefab 名不同参数」在这张表里**各行其是**，
运行期按 `a` 查，一个字都不会串。脚本只会把「同一个 `n` 出现几次」打进统计（诊断用，不是冲突）。

用法
----
    PY=D:/2/Warpforge_tools/py312/python.exe
    $PY d:/4/Unity/工具/gen_cardanim_clips.py            # 生成 + 打印统计
    $PY d:/4/Unity/工具/gen_cardanim_clips.py --check    # 只比对，不写（CI 用：退出码 1 = 与盘上不一致）
"""

import argparse
import io
import json
import os
import sys

ASSETS_FULL = r"d:/2/新解包资源/assets_full"
ADDR_MAP = r"d:/4/Unity/数据/索引/anim_address_map.json"
OUT_FULL = r"d:/4/Unity/数据/游戏数据/cardanim_clips.json"
OUT_RUNTIME = r"d:/4/Unity/MyGame/Assets/CardPresentation/Resources/cardanim_clips.json"


def build():
    am = json.load(io.open(ADDR_MAP, encoding="utf-8"))
    c2a = am["cardanim_to_asset"]

    rows = {}
    conflicts = []
    missing = []
    for ca, info in sorted(c2a.items()):
        # 🔴 **读 `sourceMono`，不是 `dump`**：`dump` 指的是 `cardanim_to_asset` 那个 **prefab（GameObject）**，
        #    `animInfo` 在**卡动画那个 MonoBehaviour** 上（= `sourceMono`）。拿错 → 全部字段读成默认 0，
        #    而 JSON 照样生成、形状照样对 —— 静默错（本脚本第一版就是这么错的，靠「16 行逐值比对」抓出来的）。
        src = info.get("sourceMono") or ""
        n = info.get("targetName") or ""
        if not src or not n:
            missing.append((ca, "no sourceMono/targetName"))
            continue
        p = os.path.join(ASSETS_FULL, src)
        if not os.path.exists(p):
            missing.append((ca, "sourceMono missing: " + src))
            continue
        try:
            j = json.load(io.open(p, encoding="utf-8", errors="replace"))
        except Exception as e:                                  # noqa: BLE001
            missing.append((ca, "json error: %s" % e))
            continue
        ai = j.get("animInfo") or {}
        row = {
            "n": n,
            "a": ca,
            "sp": int(ai.get("startPosOption") or 0),
            "ep": int(ai.get("endPosOption") or 0),
            "pt": int(ai.get("pointTowardsOption") or 0),
            "mv": 1 if ai.get("shouldMoveVFX") else 0,
            "vd": round(float(ai.get("vfxDelayTime") or 0.0), 6),
            "ts": round(float(ai.get("timeAtStartPos") or 0.0), 6),
            "tm": round(float(ai.get("timeMoving") or 0.0), 6),
            # `timeAtEndPos` —— 播放不用它，但 `GetAnimDuration() = vd+ts+tm+te` 要用它
            # （自检拿它复核 `VfxMap.SharedRangedVfxDuration` 那些「同 prefab 名多 CardAnim」该取哪一条）
            "te": round(float(ai.get("timeAtEndPos") or 0.0), 6),
        }
        # 🔴 键 = **CardAnim 名**（`a`）。同一 prefab 名可以有多张 CardAnim、参数不同 ⇒ 各存各的。
        if ca in rows:                       # 理论上 c2a 的键唯一（现读 1,099/1,099）
            conflicts.append((ca, rows[ca]["n"], n))
            continue
        rows[ca] = row
    out = {
        "schema": 1,
        "generatedBy": "d:/4/Unity/工具/gen_cardanim_clips.py",
        "what": "逐 CardAnim 的 AnimInfo 位置/移动参数（**键 = `a`（CardAnim 名）**；`n` = prefab 名，诊断用）"
                "；判据 → 本脚本文件头",
        "source": "assets_full/bundle_*cardanims*_assets_all/MonoBehaviour/<CardAnim>.json 的 animInfo"
                  " + 数据/索引/anim_address_map.json 的 cardanim_to_asset",
        "count": len(rows),
        "rows": [rows[k] for k in sorted(rows)],
    }
    return out, conflicts, missing


def dump_bytes(obj):
    # 统一 utf-8 + LF + 末尾换行；⛔ 不写 generatedAt（幂等要求）
    return (json.dumps(obj, ensure_ascii=False, indent=1, sort_keys=False) + "\n").encode("utf-8")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="只比对盘上那份，不写")
    args = ap.parse_args()

    out, conflicts, missing = build()
    b = dump_bytes(out)

    print("rows                     : %d" % out["count"])
    print("冲突（同 prefab 名不同参数）: %d" % len(conflicts))
    for c in conflicts[:20]:
        print("   CONFLICT %s  keep=%s  drop=%s" % c)
    print("跳过的 CardAnim            : %d" % len(missing))
    for m in missing[:10]:
        print("   SKIP %s  (%s)" % m)

    if args.check:
        bad = 0
        for p in (OUT_FULL, OUT_RUNTIME):
            if not os.path.exists(p):
                print("MISSING " + p)
                bad += 1
                continue
            cur = io.open(p, "rb").read()
            if cur != b:
                print("DIFF " + p)
                bad += 1
        print("check: %s" % ("OK" if bad == 0 else "%d 处不一致" % bad))
        return 1 if bad else 0

    for p in (OUT_FULL, OUT_RUNTIME):
        os.makedirs(os.path.dirname(p), exist_ok=True)
        io.open(p, "wb").write(b)
        print("wrote %s  (%d B)" % (p, len(b)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
