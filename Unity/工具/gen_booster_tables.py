#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""gen_booster_tables.py —— 从本地原版 SO 生成「卡包 / 掉落表」两张表（B143 · B142-REWARD-INVENTORY Step 0）

判据（唯一来源，⛔ 别在别处再抄一份）：
  · 卡包 SO：`m_Script.m_PathID == -5965435178007098505` ⇒ `m_ClassName "ShopContainer"`（命名空间 `Everguild.LiveOps`）。
    字段：`uniqueId` · `containerType` · `cardArmy` · `dropTableItems[].targetId`（= 5 个卡位）·
          `legendaryContainer.targetId` · `containerPreviewImage.m_SubObjectName` · `containerNameKey.mTerm`。
  · 掉落表 SO：`m_Script.m_PathID == -2313241570638646064` ⇒ `m_ClassName "DropTableItem"`。
    字段：`uniqueId` · `cardArmy` · `cardRarity` · `obtainableItems[].targetId` + `obtainableItemsWeight` ·
          `dropTableItems[].targetId` + `dropTableItemsWeight`。
  · 源目录 = `D:/2/新解包资源/assets_full`（**只读**，一个字节都不改）。

产物（两份、内容逐字节相同 —— 与 `cards_truth.json` 的「人读版 / 运行时副本」同一条口径）：
  · `Unity/数据/游戏数据/booster_tables.json`            —— 人读正本
  · `Unity/MyGame/Assets/RuleEngine/Resources/booster_tables.json` —— 运行时副本（`Resources.Load<TextAsset>`）

用法（🔴 不带显式动作 = 只打用法 + rc=2，⛔ 绝不写盘）：
  python -I -X utf8 gen_booster_tables.py --dry     只打印统计
  python -I -X utf8 gen_booster_tables.py --check   与盘上两份比对（全等 rc=0 / 有差异 rc=1 / 文件缺失 rc=1）
  python -I -X utf8 gen_booster_tables.py --write   写盘
"""
import io
import json
import os
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
ASSETS = "D:/2/新解包资源/assets_full"
HUMAN = os.path.join(ROOT, "Unity", "数据", "游戏数据", "booster_tables.json")
RUNTIME = os.path.join(ROOT, "Unity", "MyGame", "Assets", "RuleEngine", "Resources", "booster_tables.json")

SCRIPT_CONTAINER = -5965435178007098505
SCRIPT_DROPTABLE = -2313241570638646064

# 自检基线（本轮实读的值；对不上 ⇒ 直接 rc=2，⛔ 不写盘）
EXPECT_CONTAINER_MIN = 11
EXPECT_DROPTABLE_MIN = 34
EXPECT_SLOTS = 5
EXPECT_UM_FIRST_SLOT = "DT Ultramarines All R2+"
EXPECT_UM_ALL_WEIGHTS = [2600, 200, 97, 4]


def usage():
    sys.stderr.write(
        "gen_booster_tables.py —— 从原版 SO 生成 booster_tables.json\n"
        "  --dry    只打印统计（不写盘）\n"
        "  --check  比对盘上两份（0=全等，1=有差异/缺失，2=生成失败）\n"
        "  --write  写盘\n"
        "⛔ 不带参数 = 本行 + rc=2（绝不写盘）\n")


def load(name, path):
    try:
        with io.open(path, "r", encoding="utf-8") as f:
            return json.load(f)
    except Exception as e:  # noqa: BLE001
        sys.stderr.write("  [skip] %s: %s\n" % (name, e))
        return None


def scan():
    """只扫文件名以 `Booster Pack` / `DT ` 开头的 MonoBehaviour，避免读 24 万个 json。"""
    containers = {}
    droptables = {}
    dup = []
    for dirpath, _dirnames, filenames in os.walk(ASSETS):
        if not dirpath.replace("\\", "/").endswith("/MonoBehaviour"):
            continue
        for fn in filenames:
            if not fn.endswith(".json"):
                continue
            if not (fn.startswith("Booster Pack") or fn.startswith("DT ")):
                continue
            p = os.path.join(dirpath, fn)
            d = load(fn, p)
            if not isinstance(d, dict):
                continue
            sid = (d.get("m_Script") or {}).get("m_PathID")
            uid = d.get("uniqueId")
            if not uid:
                continue  # `Booster Pack Open Card Rarity N` / `Booster Pack Window Close` 这类特效件没有 uniqueId
            if sid == SCRIPT_CONTAINER:
                if uid in containers:
                    dup.append(("container", uid, containers[uid][0], p))
                    prev = containers[uid][1]
                    now = [x.get("targetId") for x in (d.get("dropTableItems") or [])]
                    if [x.get("targetId") for x in (prev.get("dropTableItems") or [])] != now:
                        sys.stderr.write("  [FATAL] 同名容器两只副本的卡位不同：%s\n" % uid)
                        raise SystemExit(2)
                else:
                    containers[uid] = (p, d)
            elif sid == SCRIPT_DROPTABLE:
                if uid in droptables:
                    dup.append(("droptable", uid, droptables[uid][0], p))
                else:
                    droptables[uid] = (p, d)
    return containers, droptables, dup


def build():
    containers, droptables, dup = scan()

    def rel(p):
        return os.path.relpath(p, ASSETS).replace("\\", "/")

    crows = []
    for uid in sorted(containers):
        p, d = containers[uid]
        slots = [x.get("targetId") or "" for x in (d.get("dropTableItems") or [])]
        crows.append({
            "id": uid,
            "army": int(d.get("cardArmy") or 0),
            "containerType": int(d.get("containerType") or 0),
            "nameKey": ((d.get("containerNameKey") or {}).get("mTerm")) or "",
            "previewSprite": ((d.get("containerPreviewImage") or {}).get("m_SubObjectName")) or "",
            "legendary": ((d.get("legendaryContainer") or {}).get("targetId")) or "",
            "slots": slots,
            "src": rel(p),
        })

    drows = []
    for uid in sorted(droptables):
        p, d = droptables[uid]
        cards = [x.get("targetId") or "" for x in (d.get("obtainableItems") or [])]
        cw = [int(x) for x in (d.get("obtainableItemsWeight") or [])]
        tables = [x.get("targetId") or "" for x in (d.get("dropTableItems") or [])]
        tw = [int(x) for x in (d.get("dropTableItemsWeight") or [])]
        # 🔴 原版 SO 里「另一侧」的权重数组是**上一版遗留**：该侧候选为 0 条、权重却仍有一整串
        #    （例 `DT Goff R1`：obtainableItems 34 条 / dropTableItems 0 条，而 dropTableItemsWeight 仍有 34 个）。
        #    ⇒ 只校验**候选非空**的那一侧；空的那一侧权重一律丢弃（⛔ 别把遗留值写成「有效权重」）。
        if cards and len(cards) != len(cw):
            sys.stderr.write("  [FATAL] %s：obtainableItems 候选 %d 条 vs 权重 %d 个\n" % (uid, len(cards), len(cw)))
            raise SystemExit(2)
        if tables and len(tables) != len(tw):
            sys.stderr.write("  [FATAL] %s：dropTableItems 候选 %d 条 vs 权重 %d 个\n" % (uid, len(tables), len(tw)))
            raise SystemExit(2)
        drows.append({
            "id": uid,
            "army": int(d.get("cardArmy") or 0),
            "rarity": int(d.get("cardRarity") or 0),
            "cards": [{"id": i, "weight": w} for i, w in zip(cards, cw)],
            "tables": [{"id": i, "weight": w} for i, w in zip(tables, tw)],
            "src": rel(p),
        })

    doc = {
        "version": 1,
        "source": "d:/2/新解包资源/assets_full（ShopContainer / DropTableItem 两类 SO 逐字段转写）",
        "note": ("由 工具/gen_booster_tables.py 生成，⛔ 别手改。判据：卡包 = m_Script -5965435178007098505；"
                 "掉落表 = -2313241570638646064。`slots` = 卡包的 5 个卡位（每个一条掉落表 id）；"
                 "`cards` = 叶子候选卡；`tables` = 子掉落表。权重原样照抄 SO。"),
        "counts": {"containers": len(crows), "dropTables": len(drows)},
        "duplicates": [[d[0], d[1], rel(d[2]), rel(d[3])] for d in dup],
        "containers": crows,
        "dropTables": drows,
    }
    return doc


def validate(doc):
    ok = True
    cs = {c["id"]: c for c in doc["containers"]}
    ds = {d["id"]: d for d in doc["dropTables"]}

    if len(cs) < EXPECT_CONTAINER_MIN:
        sys.stderr.write("  [FAIL] 卡包只有 %d 份（基线 >= %d）\n" % (len(cs), EXPECT_CONTAINER_MIN)); ok = False
    if len(ds) < EXPECT_DROPTABLE_MIN:
        sys.stderr.write("  [FAIL] 掉落表只有 %d 份（基线 >= %d）\n" % (len(ds), EXPECT_DROPTABLE_MIN)); ok = False

    for cid, c in sorted(cs.items()):
        if len(c["slots"]) != EXPECT_SLOTS:
            sys.stderr.write("  [FAIL] %s 的卡位数 = %d（原版 5）\n" % (cid, len(c["slots"]))); ok = False
        for s in c["slots"]:
            if s and s not in ds:
                sys.stderr.write("  [FAIL] %s 的卡位引用了不存在的掉落表 `%s`\n" % (cid, s)); ok = False

    um = cs.get("Booster Pack Ultramarines")
    if um is None:
        sys.stderr.write("  [FAIL] 找不到 `Booster Pack Ultramarines`\n"); ok = False
    elif um["slots"][:1] != [EXPECT_UM_FIRST_SLOT]:
        sys.stderr.write("  [FAIL] `Booster Pack Ultramarines` 第 0 卡位 = %r（原版 %r）\n"
                         % (um["slots"][:1], EXPECT_UM_FIRST_SLOT)); ok = False

    dt = ds.get("DT Ultramarines All")
    if dt is None:
        sys.stderr.write("  [FAIL] 找不到 `DT Ultramarines All`\n"); ok = False
    else:
        got = [t["weight"] for t in dt["tables"]]
        if got != EXPECT_UM_ALL_WEIGHTS:
            sys.stderr.write("  [FAIL] `DT Ultramarines All` 权重 = %r（原版 %r）\n" % (got, EXPECT_UM_ALL_WEIGHTS)); ok = False
        if [t["id"] for t in dt["tables"]] != ["DT Ultramarines R1", "DT Ultramarines R2",
                                               "DT Ultramarines R3", "DT Ultramarines R4"]:
            sys.stderr.write("  [FAIL] `DT Ultramarines All` 的子表顺序与原版不符\n"); ok = False

    return ok


def dumps(doc):
    return json.dumps(doc, ensure_ascii=False, indent=2, sort_keys=False) + "\n"


def main(argv):
    if len(argv) != 2 or argv[1] not in ("--dry", "--check", "--write"):
        usage()
        return 2

    action = argv[1]
    doc = build()
    if not validate(doc):
        return 2

    text = dumps(doc)
    print("[gen_booster_tables] 卡包 %d 份 · 掉落表 %d 份 · 重复导出 %d 组"
          % (doc["counts"]["containers"], doc["counts"]["dropTables"], len(doc["duplicates"])))
    for c in doc["containers"]:
        print("  container %-42s army=%-4d type=%d slots=%s"
              % (c["id"], c["army"], c["containerType"], " | ".join(c["slots"])))
    for d in doc["dropTables"]:
        print("  droptable %-42s army=%-4d rarity=%d cards=%d tables=%d"
              % (d["id"], d["army"], d["rarity"], len(d["cards"]), len(d["tables"])))

    if action == "--dry":
        print("[gen_booster_tables] --dry：不写盘（%d 字节）" % len(text.encode("utf-8")))
        return 0

    if action == "--check":
        bad = 0
        for p in (HUMAN, RUNTIME):
            if not os.path.exists(p):
                sys.stderr.write("  [DIFF] 缺文件 %s\n" % p); bad += 1; continue
            cur = io.open(p, "r", encoding="utf-8").read()
            if cur != text:
                sys.stderr.write("  [DIFF] %s 与生成结果不同（现 %d 字节 / 应 %d 字节）\n"
                                 % (p, len(cur.encode("utf-8")), len(text.encode("utf-8"))))
                bad += 1
        print("[gen_booster_tables] --check：%s" % ("一致" if bad == 0 else ("%d 处差异" % bad)))
        return 0 if bad == 0 else 1

    for p in (HUMAN, RUNTIME):
        d = os.path.dirname(p)
        if d and not os.path.isdir(d):
            os.makedirs(d)
        with io.open(p, "w", encoding="utf-8", newline="\n") as f:
            f.write(text)
        print("[gen_booster_tables] 写入 %s（%d 字节）" % (p, len(text.encode("utf-8"))))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
