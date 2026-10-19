#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
gen_card_anim_order.py — T05 第 3 步（驱动侧按表展开 N 拍）的**运行期瘦身表**生成器。

输入（全量件，只读）：`Unity/数据/游戏数据/card_anim_order.json`（3.5 MB · 536 cardAnims / 897 cards）
      ＋ `Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json`（**只取 `id → 卡名`**；见 `POOL` 那段）
输出（运行期）：`Unity/MyGame/Assets/CardPresentation/Resources/card_anim_order.json`

## 为什么要有这一步
全量件里每一步只是一把键（`steps[].ca` = CardAnim 资产 GUID），参数在**另一张** `cardAnims[]` 上。
运行期做那次 join 没有意义（表是死的）⇒ **生成期把 join 做完**，运行期的每一步都自带它那 10 个字段。
同时把用不到的 `animInfo`（~20 字段）与 `_*` 元数据整段剔掉
（🆕 2026-10-19 起只额外留「VFX movement」那几位：`shouldMoveVFX` / `orientToVelocity` / `easeCurve` 的关键帧，
外加「逐轴偏移」的 `applyMovementOffsets` / `offsetMovementAxis.useNonModifiedAnimTime` / 三轴曲线与随机幅度区间 —— 只给 `ao==1` 的拍写）。

## 瘦身方案（逐条）
- 🔴 **只取 `path == "activeAbility"` 的那些行**（判据见下面那一节「为什么只取 activeAbility」）——
  全量件 897 张卡共 1112 条 ability 行，其中 **109 条是 `activeAbility`**（其余 1003 条是 `cardAbilities`，
  = **别的时机**：`ThisCardPlayed`/`UseFaith`/`Strike`/`Slay`/`TurnEnd`/…）。别的行**整条丢掉**。
  🆕 2026-10-19（W）：**先按卡池过滤**（`POOL` 那段）⇒ `stats` 里除 `not_in_pool` / `renamed` 之外的
  计数都是**池内卡**的口径（现读：62 张 / 99 拍 / 7 个卡名按池子对齐 / 1 张池外卡丢弃）。
- **只留运行期真要用的字段**：`vfx`（prefab 名，= 播哪件）+ 四个时长（`vfxDelayTime` 0x24 /
  `timeAtStartPos` 0x2C / `timeMoving` 0x30 / `timeAtEndPos` 0x34 —— 这四条正是
  `AnimInfo.GetAnimDuration()` 的求和项，见 `Core/AbilityAnimDurations.cs` 文件头）
  + 三个位置选项（`startPosOption` 0x18 / `endPosOption` 0x1C / `pointTowardsOption` 0x20）
  + `waitAnimToApplyEffect`（0x58，是否等这拍播完）。
  ⛔ `startDelay`(0x28) **不收** —— `GetAnimDuration` 不算它，本步也不消费（属第 4 步「接参数」）。
- **字段名改短**：`params` 是 C# 关键字（`JsonUtility` 解不了叫 `params` 的字段）⇒ 一律扁平化 + 短名。
- **键拆开**：每张卡 = `{id, name, steps[]}`；`steps[]` 已经是 join 好的自足列表（去掉了 `abilities[]` 那层）。
- **只留能播的拍**：
  · `ca` 为空串（原版没绑 CardAnim）⇒ **丢掉**并计数；
  · `ca` 在 `cardAnims` 里找不到 ⇒ 丢掉并计数（如实记，⛔ 不填替代值）。
  · `vfx` 为空但 `ca` 解出来了（= **纯延时拍**，那 4 个 `Wait_*`）⇒ **保留**（`vfx` 写空串）——
    它的时长要照算（原版用它插停顿），只是没有特效可播。

## 为什么只取 `activeAbility`（2026-10-17 · 主对话裁决后补）
**`EvtKind.Ability` ↔ 该卡的 `activeAbility`**，⛔ **不是**全部 `cardAbilities`：
- 本仓自己的记录：`Core/EventTiming.cs` 的 `SourceOf(EvtKind.Ability)` 写着「该卡 **`activeAbility`** 各 CardAnim 的
  `GetAnimDuration()` 之和」；`Core/AbilityAnimDurations.cs` 文件头注明「`cardAbilities[*]`（`ThisCardPlayed` / `Slay` / …）
  是**别的时机**播的 ⇒ 归 `EvtKind.Trigger` / `Play`」。
- 逐卡复核（**897/897 精确相等**，两个方向各跑一遍）：把全量件 `cards[].abilities[]` 里
  `path == "activeAbility"` 的 `steps[].ca` 取出，与**真值源** `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`
  的 `activeAbility.activeAbilityLogic[].cardAnimations[].m_AssetGUID`（按序）比 —— 897 张卡**逐张逐 GUID 相同**；
  同一比法对 `path == "cardAbilities"` ↔ SO 的 `cardAbilities[].abilityLogic[].cardAnimations[]` 也是 897/897。
  ⇒ **判别据 = 全量件里那一行的 `path` 字段**（它本身就来自 SO 的两个字段名），可逐卡复核。
- ⚠️ 若拿全表（含 `cardAbilities`）展开，会把**出场/触发时机的动画播在「发动技能」那一拍上** ——
  **错误演示、而且看着有动画（完全静默）**，比少播更坏。


## 幂等
跑两遍结果逐字节相同（无时间戳、无随机、字典序稳定）。`--check` 只比对不写盘。

用法：
    "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_card_anim_order.py
    "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_card_anim_order.py --check
"""
import io
import json
import sys
import os

SRC = r"d:/4/Unity/数据/游戏数据/card_anim_order.json"
DST = r"d:/4/Unity/MyGame/Assets/CardPresentation/Resources/card_anim_order.json"
# 🆕 2026-10-19（W）：**运行期用的卡名以【我们卡池】为准**。
#   为什么：消费端 `CardAnimOrder.TryGetBeats(e.CardId)` 的键是
#   **`CardDef.Name`（英文卡名）**（`BattleEvent.CardId`），而全量件里的 `cards[].name`
#   是**原版快照的卡名** —— 两者不是一张表（现读 63 张里 **8 张不同**，例：
#   `GSC66` 快照叫 `Shanus Daskovian`、我们池子叫 `Lhaska Szenari`）。
#   ⇒ 名字照抄会让那 8 张卡的整段 N 拍**查不到**（`TryGetBeats` 回 0 ⇒ 退回原样一拍），
#     **静默少演**。判据 = `Battle/CardAnimOrder.cs` 的 `TryGetBeats` doc（先查 id、再查卡名）。
#   ⚠️ `id` 两边是同一套（我们池子的 id 就是原版 id）⇒ 只用它对齐，**不猜名字**。
POOL = r"d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json"

# 步骤字段的短名（JsonUtility 侧一一对应 `CardAnimBeat`）
def _curve_flat(curve):
    """`AnimationCurve`（全量件里的序列化形状）→ 扁平 `[t0,v0,t1,v1,…]`（按 time 升序）。

    只留 `time/value` 两列（运行期按**分段线性**求值；原版 Evaluator 带切线 —— 实测本表里
    `mv=1` 的 7 条拍曲线与 `ao=1` 的 3 拍三轴曲线全是折线，逐值等价，见 `CardAnimOrder.EaseAt`）。
    没有键 / 曲线是 null ⇒ 返回空列表（运行期按各自默认处理）。
    """
    cur = ((curve or {}).get("m_Curve") or [])
    out = []
    for k in sorted(cur, key=lambda x: x.get("time", 0.0)):
        out.append(round(float(k.get("time") or 0.0), 4))
        out.append(round(float(k.get("value") or 0.0), 4))
    return out


def _ease_flat(ai):
    """`easeCurve.m_Curve` → 扁平 `[t0,v0,t1,v1,…]`（升序）。

    🆕 2026-10-19（偏差 6）：`CardAnimController.Update` 用 `Evaluate((t−timeAtStartPos)/timeMoving)`
    当位移进度 ⇒ 运行期要这条曲线。
    没有键 / 曲线是 null ⇒ 返回空列表（运行期按线性处理 = 原版 `AnimInfo..ctor` 的默认曲线）。
    """
    return _curve_flat(ai.get("easeCurve"))


def slim_beat(step, anims, stats):
    ca = (step.get("ca") or "").strip()
    if not ca:
        stats["blank_guid"] += 1
        return None
    a = anims.get(ca)
    if a is None:
        stats["unresolved_guid"] += 1
        return None
    p = a.get("params") or {}
    ai = a.get("animInfo") or {}
    vfx = a.get("vfx")
    if vfx is None:
        stats["pure_wait"] += 1
    # 🆕 2026-10-19（偏差 6）：`shouldMoveVFX` / `orientToVelocity` / `easeCurve` 三位
    #   —— `params` 里**没有**后两位（它们在 `animInfo` 上），所以这一段读 `ai`。
    mv = 1 if ai.get("shouldMoveVFX") else 0
    if mv:
        stats["movers"] += 1
    b = {
        "ca": ca,
        "vfx": vfx if vfx is not None else "",
        "sp": int(p.get("startPosOption") or 0),
        "ep": int(p.get("endPosOption") or 0),
        "pt": int(p.get("pointTowardsOption") or 0),
        "vd": float(p.get("vfxDelayTime") or 0.0),
        "ts": float(p.get("timeAtStartPos") or 0.0),
        "tm": float(p.get("timeMoving") or 0.0),
        "te": float(p.get("timeAtEndPos") or 0.0),
        "w": int(p.get("waitAnimToApplyEffect") or 0),
        "mv": mv,
        "otv": 1 if ai.get("orientToVelocity") else 0,
        "ease": _ease_flat(ai) if mv else [],
    }
    # 🆕 2026-10-19（W · 偏差 6 第 1 条）：`applyMovementOffsets` / `offsetMovementAxis`
    #   判据（`CardAnimController__Update.c:60-97` + `OffsetMovementCurvesByAxis__GetRandomOffsetValues.c`）：
    #   `ao` 开着时，`Update` 在 lerp 之后逐轴叠 `curve_axis.Evaluate(t) × randomAmp_axis`；
    #   `randomAmp_axis` = 每拍初始化时 `Random.Range(minMax*Offset.x, minMax*Offset.y)` 抽一次。
    #   `ont` = `useNonModifiedAnimTime`：真用原始 `t01`、假用缓动后的 `e`（见 `CardAnimBeat.OffsetAt`）。
    #   ⚠️ **只有 `ao==1` 的拍才写这 11 个字段** —— 其余拍缺省即 0（JsonUtility 缺字段给默认值）；
    #      全量件 536 条里 `applyMovementOffsets` 只有 0/1 两种值（现读 17 条为 1）。
    ao = 1 if ai.get("applyMovementOffsets") else 0
    if ao:
        stats["offset_beats"] += 1
        om = ai.get("offsetMovementAxis") or {}
        b["ao"] = 1
        b["ont"] = 1 if om.get("useNonModifiedAnimTime") else 0
        b["ofx"] = _curve_flat(om.get("xAxis"))
        b["ofy"] = _curve_flat(om.get("yAxis"))
        b["ofz"] = _curve_flat(om.get("zAxis"))
        for mn_key, mx_key, range_key in (("mnx", "mxx", "minMaxXOffset"),
                                          ("mny", "mxy", "minMaxYOffset"),
                                          ("mnz", "mxz", "minMaxZOffset")):
            r = om.get(range_key) or {}
            b[mn_key] = round(float(r.get("x") or 0.0), 6)
            b[mx_key] = round(float(r.get("y") or 0.0), 6)
    return b


def build():
    d = json.load(io.open(SRC, encoding="utf-8"))
    anims = {a["guid"]: a for a in d.get("cardAnims") or []}
    # 🆕 2026-10-19（W）：id → 我们池子的卡名（见 `POOL` 那段）。
    #   读不到就退回原版快照的卡名（⛔ 不编；并在 `stats.poolMissing` 里出声）。
    pool_name = {}
    try:
        pool = json.load(io.open(POOL, encoding="utf-8"))
        for pc in pool.get("cards") or []:
            if pc.get("id"):
                pool_name[pc["id"]] = pc.get("name") or ""
    except Exception as e:
        print("[gen] ⚠️ 读不到卡池 %s（%s）⇒ 卡名退回原版快照那一份" % (POOL, e))
    stats = {"blank_guid": 0, "unresolved_guid": 0, "pure_wait": 0,
             "cards": 0, "steps": 0, "cards_no_steps": 0,
             "rows_not_active": 0, "steps_not_active": 0,
             "movers": 0, "offset_beats": 0,
             "not_in_pool": 0, "renamed": 0, "poolMissing": 0 if pool_name else 1}
    cards = []
    for c in d.get("cards") or []:
        cid = c.get("id") or ""
        src_name = c.get("name") or ""
        # 🆕 2026-10-19（W）：卡池里没有的卡 ⇒ **运行期永远查不到**（`e.CardId` 只可能是池内卡名），
        #   而且它的名字会占住 `_byName` 的槽位（现读 `Tempestus Scion` 那条就把池内的 `AM68` 挡住了）
        #   ⇒ 直接丢掉并计数（判据见 `POOL` 那段）。
        if pool_name and cid not in pool_name:
            stats["not_in_pool"] += 1
            continue
        name = pool_name.get(cid) or src_name
        if name != src_name:
            stats["renamed"] += 1
        steps = []
        for ab in c.get("abilities") or []:
            # 🔴 **只取 `activeAbility`**（`EvtKind.Ability` ↔ 该卡的 `activeAbility`；判据见模块 doc 那一节）
            if (ab.get("path") or "") != "activeAbility":
                stats["rows_not_active"] += 1
                stats["steps_not_active"] += len(ab.get("steps") or [])
                continue
            # `steps[].i` 升序 = 原版 cardAnimations 的 List 下标序（全量件 `_conventions.stepOrder`）
            for st in sorted(ab.get("steps") or [], key=lambda s: s.get("i", 0)):
                b = slim_beat(st, anims, stats)
                if b is not None:
                    steps.append(b)
        if not steps:
            stats["cards_no_steps"] += 1
            continue
        cards.append({"id": cid, "name": name, "steps": steps})
        stats["cards"] += 1
        stats["steps"] += len(steps)
    out = {
        "schema": 1,
        "source": "Unity/数据/游戏数据/card_anim_order.json（A6 · 2026-10-16）；"
                  "卡名以 cards_engine.json 的 id→name 为准（W · 2026-10-19）",
        "filter": "abilities[].path == 'activeAbility'（= 该卡的主动能力；判据见生成器文件头）；"
                  "卡池里没有的卡整条丢弃（运行期永远查不到，且名字会挡住院内同名卡）",
        "note": "运行期瘦身版。字段短名 = CardAnimBeat："
                "id/name = 卡 id + **我们池子的卡名**（`TryGetBeats` 的卡名键就是它）· "
                "ca=CardAnim guid · vfx=prefab 名（空串 = 纯延时拍）· "
                "sp/ep/pt=startPosOption/endPosOption/pointTowardsOption · "
                "vd/ts/tm/te=vfxDelayTime/timeAtStartPos/timeMoving/timeAtEndPos · w=waitAnimToApplyEffect · "
                "mv=shouldMoveVFX · otv=orientToVelocity · ease=easeCurve 的扁平关键帧 [t0,v0,t1,v1,…]"
                "（2026-10-19 · 偏差 6「起→终位移 + 朝向」）· "
                "ao=applyMovementOffsets · ont=offsetMovementAxis.useNonModifiedAnimTime · "
                "ofx/ofy/ofz=三轴偏移曲线（同 ease 的扁平约定）· mnx/mxx/mny/mxy/mnz/mxz=各轴随机幅度区间"
                "（2026-10-19 · 偏差 6「逐轴偏移」；只有 ao==1 的拍写这 11 个字段，其余缺省 0）",
        "stats": stats,
        "cards": cards,
    }
    return out


def dumps(o):
    return json.dumps(o, ensure_ascii=False, indent=1, sort_keys=False)


def main():
    o = build()
    txt = dumps(o)
    if "--check" in sys.argv:
        old = None
        if os.path.exists(DST):
            old = io.open(DST, encoding="utf-8").read()
        same = (old == txt)
        print("[check] 幂等 =", same, "| cards =", o["stats"]["cards"],
              "| steps =", o["stats"]["steps"])
        return 0 if same else 1
    # ⚠️ 先算好再写（本仓踩过 `open(...,'wb').write(<表达式>)` 先截断后求值的坑）
    with io.open(DST, "w", encoding="utf-8", newline="\n") as f:
        f.write(txt)
    print("[gen] ->", DST)
    print("[gen] bytes =", len(txt.encode("utf-8")))
    print("[gen] stats =", o["stats"])
    return 0


if __name__ == "__main__":
    sys.exit(main())
