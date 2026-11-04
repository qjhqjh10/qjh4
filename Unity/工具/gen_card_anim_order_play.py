#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
gen_card_anim_order_play.py — **出牌（`ThisCardPlayed` / trigger 0）**那一族的逐卡逐拍演出表（运行期瘦身版）。

为什么要有它（判据链）
----------------------
原版「打出一张牌」时播的那几拍 = 该卡 **`cardAbilities[0]`（trigger = `ThisCardPlayed = 0`）** 那些行
各自的 `abilityLogic[].cardAnimations[]`，由 `AbilityLogic.PlayAnim` 逐拍播
（`d:/2/tools/decomp_full/AbilityLogic__PlayAnim.c` 外层 enumerator = `param_1 + 0x18`；
 `PlayAbility.c:273/1531/2465` 三处调用；`RawCardScript__TriggerAbilities.c` 会遍历**所有**命中 trigger 的行）。
⇒ 与我们既有的两个运行期表的关系：
  · `Resources/card_anim_order.json` = **`activeAbility`**（93 卡 / 158 拍）—— 主动技能那一支，⛔ 不是这条；
  · `Resources/card_anim_order_trigger.json` = **`cardAbilities` 全部 trigger**（726 卡 / 911 行 / 1,169 拍）；
  · **本表** = 上面那张表里 **`trigger == 0`** 的那一半（出牌那一拍）—— 单独成表是因为
    消费端时机不同（`EvtKind.Play` vs `EvtKind.Trigger`）、而且 `Play` 事件**不带 ability 下标**。

⚠️ 与旧 `play` 桶（`数据/游戏数据/card_vfx_by_card.json` 的 `byCard[id]["play"]`，= `card_vfx_by_card.py`
折叠成**单件**的那个）**不是同集合**：旧桶 534 卡、本表 474 卡，差集与原因由 `--diff-old` 现算（不写盘）。

数据从哪来
----------
全量件 `Unity/数据/游戏数据/card_anim_order.json`（`gen_card_anim_order.py` 的产物）里
`abilities[].path == 'cardAbilities'` 且 `trigger == 0` 的行；卡池/卡名 = `RuleEngine/Resources/cards_engine.json`。
每拍由 **`gen_card_anim_order.slim_beat` 同一份实现**产出（⛔ 不抄第二份）。

产物
----
`MyGame/Assets/CardPresentation/Resources/card_anim_order_play.json`
```json
{ "schema":1, "source":…, "filter":…, "note":…, "stats":{…},
  "cards":[ { "id":"EC29", "name":"Heldrake",
              "rows":[ { "ai":0, "li":0, "chosenAbility":10, "chosenAbilityName":"doDamage",
                         "steps":[ <CardAnimBeat 字段> ] } ] } ] }
```
- `rows` 按 `(ai, li)` 升序；`steps` 按全量件里的 `i` 升序。
- 空 `ca` 的拍**不产出**（原版 `PlayAnim:62-64` 跳过）；整行都空的**保留 `steps: []`**（行数统计要与全量件对得上）。

幂等 / 字节稳定
--------------
两次 `--write` 逐字节相同（卡按 id 排序、行按 `(ai,li)` 排序、拍按 `i` 升序；无时间戳/无随机）。

用法（🔴 不带参 = 只打印本用法并以**退出码 2** 退出，**绝不写盘**）
--------------------------------------------------------------
    python -I -X utf8 工具/gen_card_anim_order_play.py --check     # 只对账（不写盘）
    python -I -X utf8 工具/gen_card_anim_order_play.py --write     # 重新生成（写盘）
    python -I -X utf8 工具/gen_card_anim_order_play.py --dry       # 只打印摘要（不写盘）
    python -I -X utf8 工具/gen_card_anim_order_play.py --diff-old  # 与旧 `play` 桶比集合（只打印）
    -h, --help  同「不带参」，但退出码 0。
"""
import io
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)          # `python -I` 不会把脚本目录加进 sys.path —— 显式补
import gen_card_anim_order as gao     # 复用 `rj/dumps/slim_beat/_read_text/_write_text`（判据只此一处）

ROOT = "d:/4/Unity"
FULL = ROOT + "/数据/游戏数据/card_anim_order.json"            # 全量件（只读）
POOL = ROOT + "/MyGame/Assets/RuleEngine/Resources/cards_engine.json"
VFX = ROOT + "/数据/游戏数据/card_vfx_by_card.json"             # 旧 `play` 桶（只在 --diff-old 读）
DST = ROOT + "/MyGame/Assets/CardPresentation/Resources/card_anim_order_play.json"

TRIG_THIS_CARD_PLAYED = 0            # `AbilityTrigger.ThisCardPlayed`

USAGE = __doc__.split("用法（", 1)[0] + """用法（🔴 不带参 = 只打印本用法并以退出码 2 退出，绝不写盘）
    python -I -X utf8 工具/gen_card_anim_order_play.py --check
    python -I -X utf8 工具/gen_card_anim_order_play.py --write
    python -I -X utf8 工具/gen_card_anim_order_play.py --dry
    python -I -X utf8 工具/gen_card_anim_order_play.py --diff-old
"""


def build():
    """返回 (obj, stats)。obj 的形状见文件头。"""
    d = gao.rj(FULL)
    pool = gao.rj(POOL)
    anims = {a["guid"]: a for a in d.get("cardAnims") or []}
    pname = {c["id"]: (c.get("name") or "") for c in pool.get("cards") or [] if c.get("id")}

    beat_stats = {"blank_guid": 0, "unresolved_guid": 0, "pure_wait": 0,
                  "movers": 0, "offset_beats": 0}
    cards = []
    rows = steps_raw = steps = 0
    not_in_pool = 0
    cards_with_trigger = 0
    cards_no_rows = 0

    for c in d.get("cards") or []:
        cid = c.get("id") or ""
        if cid not in pname:
            not_in_pool += 1
            continue
        mine = []
        for ab in c.get("abilities") or []:
            if (ab.get("path") or "") != "cardAbilities":
                continue
            if ab.get("trigger") != TRIG_THIS_CARD_PLAYED:
                continue
            sst = ab.get("steps") or []
            if not sst:
                continue
            ai = ab.get("abilityIndex")
            li = ab.get("logicIndex")
            ai = -1 if ai is None else ai
            li = -1 if li is None else li
            beats = []
            for s in sorted(sst, key=lambda x: x.get("i", 0)):
                b = gao.slim_beat(s, anims, beat_stats)
                if b is None:
                    continue                  # 空 ca / 解不出 ⇒ 原版 `PlayAnim:62-64` 也是跳过
                beats.append(b)
            mine.append((ai, li, ab, beats))
        if not mine:
            cards_no_rows += 1
            continue
        cards_with_trigger += 1
        mine.sort(key=lambda x: (x[0], x[1]))     # (ai, li) 升序 = 原版遍历 cardAbilities / abilityLogic 的次序
        out_rows = []
        for ai, li, ab, beats in mine:
            rows += 1
            steps_raw += len(ab.get("steps") or [])
            steps += len(beats)
            out_rows.append({
                "ai": ai,
                "li": li,
                "chosenAbility": ab.get("chosenAbility"),
                "chosenAbilityName": ab.get("chosenAbilityName"),
                "steps": beats,
            })
        cards.append({"id": cid, "name": pname.get(cid) or c.get("name") or "", "rows": out_rows})

    cards.sort(key=lambda x: x["id"])

    stats = {
        "cards": len(cards),
        "rows": rows,
        "steps_raw": steps_raw,
        "steps": steps,
        "blank_guid": beat_stats["blank_guid"],
        "unresolved_guid": beat_stats["unresolved_guid"],
        "pure_wait": beat_stats["pure_wait"],
        "movers": beat_stats["movers"],
        "offset_beats": beat_stats["offset_beats"],
        "cards_with_trigger0": cards_with_trigger,
        "cards_no_rows": cards_no_rows,
        "not_in_pool": not_in_pool,
    }
    obj = {
        "schema": 1,
        "source": "Unity/数据/游戏数据/card_anim_order.json（全量件）里 abilities[].path == 'cardAbilities' "
                  "且 trigger == 0 (ThisCardPlayed) 的行；卡池与卡名 = RuleEngine/Resources/cards_engine.json"
                  "（2026-11-05 B143-W1）",
        "filter": "卡池内 + path == 'cardAbilities' + trigger == 0 + **有 cardAnimations 行**"
                  "（整行空 ca 的卡也保留、steps 为空表）；"
                  "空 ca 的拍按原版 PlayAnim:62-64 跳过",
        "note": "出牌（ThisCardPlayed）那一拍的逐卡有序步骤。字段短名 = CardAnimBeat（同 Resources/card_anim_order.json）："
                "ca=CardAnim guid · vfx=prefab 名（空串=纯延时拍）· sp/ep/pt=startPosOption/endPosOption/pointTowardsOption · "
                "vd/ts/tm/te=vfxDelayTime/timeAtStartPos/timeMoving/timeAtEndPos · w=waitAnimToApplyEffect · "
                "mv=shouldMoveVFX · otv=orientToVelocity · ease=easeCurve 扁平关键帧 · ao=applyMovementOffsets · "
                "ont=offsetMovementAxis.useNonModifiedAnimTime · ofx/ofy/ofz=三轴偏移曲线 · mnx/mxx/…=各轴随机幅度区间 · "
                "ai=原版 cardAbilities[ai] 的下标 · li=abilityLogic[li] 的下标。"
                "⚠️ 运行时读取口 = CardAnimOrder.TryGetPlayBeats（只读本表；⛔ 不要拿 card_anim_order_trigger / "
                "card_vfx_by_card 的 play 桶顶替 —— 三者行集不同）",
        "stats": stats,
        "cards": cards,
    }
    return obj, stats


def diff_old():
    """与旧 `play` 桶（`card_vfx_by_card.json` 的 `byCard[id]["play"]`）比卡集合。只打印，不写盘。"""
    obj, _ = build()
    new_ids = {c["id"] for c in obj["cards"]}
    vfx = gao.rj(VFX)
    bc = (vfx or {}).get("byCard") or {}
    old_ids = {k for k, v in bc.items() if isinstance(v, dict) and (v.get("play") or [])}

    full = gao.rj(FULL)
    in_full = {(c.get("id") or "") for c in full.get("cards") or []}

    only_new = sorted(new_ids - old_ids)
    only_old = sorted(old_ids - new_ids)
    print("[diff-old] 新表 %d 卡 · 旧 play 桶 %d 卡 · 交集 %d"
          % (len(new_ids), len(old_ids), len(new_ids & old_ids)))
    print("[diff-old] 新表有、旧桶没有 = %d %s" % (len(only_new), only_new[:12]))
    print("[diff-old] 旧桶有、新表没有 = %d %s" % (len(only_old), only_old[:12]))
    # 原因分类：① 这张卡不在全量件里（覆盖缺口）② 在全量件里但没有 trigger==0 的行 ③ 其它
    not_in_full = [k for k in only_old if k not in in_full]
    print("[diff-old]   其中「不在全量件 card_anim_order.json 里」 = %d（= 全量件覆盖缺口，不是口径差）"
          % len(not_in_full))
    rest = [k for k in only_old if k in in_full]
    print("[diff-old]   其余（在全量件里却没有 trigger==0 的行）= %d %s" % (len(rest), rest[:12]))
    return 0


def main(argv):
    args = [a for a in argv[1:]]
    if "-h" in args or "--help" in args:
        print(USAGE)
        return 0
    if not args:
        print(USAGE)
        return 2
    known = {"--check", "--write", "--dry", "--diff-old"}
    unknown = [a for a in args if a not in known]
    if unknown:
        print("[gen] 不认识的参数：%s\n" % unknown)
        print(USAGE)
        return 2

    if "--diff-old" in args:
        return diff_old()

    obj, stats = build()
    txt = gao.dumps(obj)
    print("[gen] 摘要 = " + json.dumps(stats, ensure_ascii=False, sort_keys=True))

    if "--dry" in args:
        print("[dry] 不写盘；目标 = " + DST)
        print("[dry] bytes = %d" % len(txt.encode("utf-8")))
        return 0

    if "--check" in args:
        old = gao._read_text(DST)
        if old is None:
            print("[check] 目标不存在：%s" % DST)
            return 1
        same = old == txt
        print("[check] 幂等 = %s" % same)
        if not same:
            try:
                oldj = json.loads(old)
                print("[check] 盘上 stats = " + json.dumps(oldj.get("stats") or {}, ensure_ascii=False, sort_keys=True))
            except Exception as e:
                print("[check] 盘上文件读不出 JSON：%s" % e)
        return 0 if same else 1

    # --write
    os.makedirs(os.path.dirname(DST), exist_ok=True)
    gao._write_text(DST, txt)
    print("[write] -> " + DST)
    print("[write] bytes = %d" % len(txt.encode("utf-8")))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
