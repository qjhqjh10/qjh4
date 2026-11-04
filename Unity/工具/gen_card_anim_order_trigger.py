#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
gen_card_anim_order_trigger.py — **触发 / 出牌能力**那一族的逐卡逐技能演出表（运行期瘦身版）。

为什么要有它（判据链）
----------------------
原版 `AbilityLogic.PlayAnim` 遍历的是**当前这条 `AbilityLogic` 自己的** `cardAnimations[]`
（`d:/2/tools/decomp_full/AbilityLogic__PlayAnim.c`，外层 enumerator = `param_1 + 0x18`）；
而 `BattleManager.TriggerAbilities` 会把**所有命中 trigger 的 `cardAbilities` 行**逐条交给
`AbilityLogic.PlayAbility` → `PlayAnim`（`B143-R2` 查清）。
⇒ 「打出这张卡 / 触发某个时机」播的 N 拍 = **`cardAbilities[]` 行自己的 `abilityLogic[].cardAnimations[]`**，
⛔ **不是** `activeAbility` 那一份（`Resources/card_anim_order.json` 只装了后者）。

数据从哪来（⚠️ 与派单字面不同，已实测）
--------------------------------------
派单写「从 `cards_truth.json` / `cards_engine.json` 抽」。**实测 `cards_truth.json` 里 `cardAnimations`
出现 0 次**（全树递归搜；它只有语义能力 `abilities[].logic[]`，不含动画引用）—— 所以动画引用只能来自：
  · **全量件** `Unity/数据/游戏数据/card_anim_order.json`（3.5 MB）：它已经把**每条 `cardAbilities` 行**
    连同 `steps[].ca` 和解析好的 `cardAnims[]` 参数烘好了（`gen_card_anim_order.py` 的产物，
    基底 = 2022 快照 + 当前 build `cards_raw.json` 的增量）；
  · **卡池/卡名** `MyGame/Assets/RuleEngine/Resources/cards_engine.json`（只用于过滤 + 取我们的卡名）。
⇒ 本生成器 = 「全量件里 `path == 'cardAbilities'` 的那一半」的瘦身投影（与 `--write-slim` 完全同形，
只是 path 相反）。⛔ **不改** `card_anim_order.json`（全量件与 activeAbility 瘦身版都不碰）。

口径（与 `B143-R2` 对齐）
------------------------
- 只取**卡池里有的卡**（`cards_engine.json` 的 `id` 集合）；
- 只取 `abilities[].path == 'cardAbilities'` 且**有 steps** 的行；
- 卡池内合计 = **726 卡 / 911 行 / 1,178 拍（raw）**，其中 `trigger == 0`（`ThisCardPlayed`）= **579 行**；
- 每拍的 `ca` 为空串（原版自己没绑，`PlayAnim:62-64` 会跳过那一拍）⇒ **不产出这一拍**
  （与 activeAbility 瘦身版同一条纪律）⇒ 实际写出 **1,169 拍**（9 拍被跳过）；
  `stats` 里两个数都留着：`steps_raw = 1178` / `steps = 1169`。

产物
----
`MyGame/Assets/CardPresentation/Resources/card_anim_order_trigger.json`
```json
{ "schema":1, "source":…, "filter":…, "note":…, "stats":{…},
  "cards":[ { "id":"EC29", "name":"Heldrake",
              "abilities":[ { "trigger":0, "triggerName":"ThisCardPlayed",
                              "ai":0, "li":0, "chosenAbility":10, "chosenAbilityName":"doDamage",
                              "steps":[ <CardAnimBeat 字段 + ai/li> ] } ] } ] }
```
- 每拍字段 = `CardAnimBeat` 的短名（`ca/vfx/sp/ep/pt/vd/ts/tm/te/w/mv/otv/ease/ao/ont/ofx/ofy/ofz/mnx/mxx/…`），
  由 `gen_card_anim_order.slim_beat` **同一份实现**产出（⛔ 不抄第二份，避免两份迟早不一致）。
- 新增 `ai` = 原版 `cardAbilities[ai]` 的下标、`li` = `abilityLogic[li]` 的下标
  （同一张卡可以有多条同 trigger 的行 ⇒ 消费端靠 `(ai,li)` 区分，⛔ 不能靠 `ca` 反查）。

幂等 / 字节稳定
--------------
两次 `--write` 逐字节相同（卡按 id 排序、行按全量件里的存储序、拍按 `i` 升序；无时间戳/无随机）。

用法（🔴 不带参 = 只打印本用法并以**退出码 2** 退出，**绝不写盘**）
--------------------------------------------------------------
    python -I -X utf8 工具/gen_card_anim_order_trigger.py --check   # 只对账（不写盘）
    python -I -X utf8 工具/gen_card_anim_order_trigger.py --write   # 重新生成（写盘）
    python -I -X utf8 工具/gen_card_anim_order_trigger.py --dry     # 只打印摘要（不写盘）
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
FULL = ROOT + "/数据/游戏数据/card_anim_order.json"          # 全量件（只读）
POOL = ROOT + "/MyGame/Assets/RuleEngine/Resources/cards_engine.json"
DST = ROOT + "/MyGame/Assets/CardPresentation/Resources/card_anim_order_trigger.json"

TRIG_THIS_CARD_PLAYED = 0            # `AbilityTrigger.ThisCardPlayed`（R2 的 579 行就是它）

USAGE = __doc__.split("用法（", 1)[0] + """用法（🔴 不带参 = 只打印本用法并以退出码 2 退出，绝不写盘）
    python -I -X utf8 工具/gen_card_anim_order_trigger.py --check
    python -I -X utf8 工具/gen_card_anim_order_trigger.py --write
    python -I -X utf8 工具/gen_card_anim_order_trigger.py --dry
"""


def build():
    """返回 (obj, stats)。obj 的形状见文件头。"""
    d = gao.rj(FULL)
    pool = gao.rj(POOL)
    anims = {a["guid"]: a for a in d.get("cardAnims") or []}
    pname = {c["id"]: (c.get("name") or "") for c in pool.get("cards") or [] if c.get("id")}

    # `slim_beat` 会往这个字典里累计（键名照 `gen_card_anim_order.py` 的 slim 那一支）
    beat_stats = {"blank_guid": 0, "unresolved_guid": 0, "pure_wait": 0,
                  "movers": 0, "offset_beats": 0}
    cards = []
    rows = steps_raw = steps = 0
    this_played = 0
    not_in_pool = 0
    cards_no_rows = 0

    for c in d.get("cards") or []:
        cid = c.get("id") or ""
        if cid not in pname:
            not_in_pool += 1
            continue
        abilities = []
        for ab in c.get("abilities") or []:
            if (ab.get("path") or "") != "cardAbilities":
                continue
            sst = ab.get("steps") or []
            if not sst:
                continue
            rows += 1
            steps_raw += len(sst)
            if ab.get("trigger") == TRIG_THIS_CARD_PLAYED:
                this_played += 1
            ai = ab.get("abilityIndex")
            li = ab.get("logicIndex")
            ai = -1 if ai is None else ai
            li = -1 if li is None else li
            beats = []
            for s in sorted(sst, key=lambda x: x.get("i", 0)):
                b = gao.slim_beat(s, anims, beat_stats)
                if b is None:
                    continue                  # 空 ca / 解不出 ⇒ 原版 `PlayAnim:62-64` 也是跳过
                b["ai"] = ai
                b["li"] = li
                beats.append(b)
            # 🔴 **整行都是空 ca 时仍保留这一行**（`steps: []`）：原版那几行确实存在、只是每拍都被
            #   `PlayAnim:62-64` 跳过 ⇒ 保留行才能让统计与 `B143-R2` 的 726 卡 / 911 行对上；
            #   运行期取到 0 拍 = 不播，与原版逐字等价。
            steps += len(beats)
            abilities.append({
                "trigger": ab.get("trigger"),
                "triggerName": ab.get("triggerName"),
                "ai": ai,
                "li": li,
                "chosenAbility": ab.get("chosenAbility"),
                "chosenAbilityName": ab.get("chosenAbilityName"),
                "steps": beats,
            })
        if abilities:
            cards.append({"id": cid, "name": pname.get(cid) or c.get("name") or "", "abilities": abilities})
        else:
            cards_no_rows += 1

    cards.sort(key=lambda x: x["id"])     # 稳定：卡按 id 排序（行序保持全量件的存储序）

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
        "this_card_played_rows": this_played,
        "cards_no_rows": cards_no_rows,
        "not_in_pool": not_in_pool,
    }
    obj = {
        "schema": 1,
        "source": "Unity/数据/游戏数据/card_anim_order.json（全量件）里 abilities[].path == 'cardAbilities' 的行；"
                  "卡池与卡名 = RuleEngine/Resources/cards_engine.json（2026-11-05 B143）",
        "filter": "卡池内 + path == 'cardAbilities' + **有 cardAnimations 行**（整行空 ca 的卡也保留、steps 为空表）；"
                  "空 ca 的拍按原版 PlayAnim:62-64 跳过",
        "note": "触发/出牌能力的逐卡有序演出步骤。字段短名 = CardAnimBeat（同 Resources/card_anim_order.json）："
                "ca=CardAnim guid · vfx=prefab 名（空串=纯延时拍）· sp/ep/pt=startPosOption/endPosOption/pointTowardsOption · "
                "vd/ts/tm/te=vfxDelayTime/timeAtStartPos/timeMoving/timeAtEndPos · w=waitAnimToApplyEffect · "
                "mv=shouldMoveVFX · otv=orientToVelocity · ease=easeCurve 扁平关键帧 · ao=applyMovementOffsets · "
                "ont=offsetMovementAxis.useNonModifiedAnimTime · ofx/ofy/ofz=三轴偏移曲线 · mnx/mxx/…=各轴随机幅度区间 · "
                "ai=原版 cardAbilities[ai] 的下标 · li=abilityLogic[li] 的下标（同一张卡可有多条同 trigger 的行，"
                "消费端靠 (ai,li) 区分，⛔ 不能靠 ca 反查） · "
                "⚠️ **行数口径**：`stats.rows` = (abilityIndex, logicIndex) **组数**（2026-11-05 换源后现读 **1,024**）；"
                "**ability 级行数是 928**（`cardAbilities[]` 条数）—— 两个数都对、只是粒度不同，⛔ 别当成谁写错了",
        "stats": stats,
        "cards": cards,
    }
    return obj, stats


def main(argv):
    args = [a for a in argv[1:]]
    if "-h" in args or "--help" in args:
        print(USAGE)
        return 0
    if not args:
        print(USAGE)
        return 2
    known = {"--check", "--write", "--dry"}
    unknown = [a for a in args if a not in known]
    if unknown:
        print("[gen] 不认识的参数：%s\n" % unknown)
        print(USAGE)
        return 2

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
