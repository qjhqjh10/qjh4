# -*- coding: utf-8 -*-
"""W2 · R4 归口分类器（只读校验；⛔ 不改数据）。

把 `数据/游戏数据/card_vfx_by_card.json` 里 **`ability` 桶**的每条条目按
`abilityTriggerName`（原版 `AbilityTrigger` 枚举名）分三档，并给出
「改后应该落在哪个桶」的投影 —— 用来做 before/after 对照。

三档的判据（**都从代码现读，不在这里另立一份口径**）：
  · **MAPPED**  —— `工具/gen_card_vfx_by_card.py` 的 `TRIGGER_EVENT`（7 个名字）
  · **ABILITY** —— `CardPresentation/Core/VfxMap.cs` 的 `IsAbilityTrigger`（-1/400/600/605）
                   + `activeAbility` 那一支（`tg = -1`）
  · **ROUTED**  —— `VfxMap.IsRoutedTriggerName` 的 `case` 标签（引擎真的会带着这个名字发
                   `EvtKind.Trigger`；`MustRouteTriggerNames` 是它的正向锁）
  · 其余 = **UNMAPPED**（引擎没有对应事件 ⇒ 新桶 `trigger_unmapped`，留档不播）

用法：
    python -I -X utf8 w2_classify.py [--json PATH]
"""
import io
import json
import re
import sys

ROOT = "d:/4/Unity"
FULL = ROOT + "/数据/游戏数据/card_vfx_by_card.json"
VFXMAP = ROOT + "/MyGame/Assets/CardPresentation/Core/VfxMap.cs"


def routed_names(path=VFXMAP):
    """`VfxMap.IsRoutedTriggerName` 的白名单（`case` 标签 + 两条 OrdinalIgnoreCase）。

    这与 AN1 的 `an1_bucket_matrix.py` **同一个解析法**（那边只用来判「放行/未放行」）。
    ⚠️ 解析不到任何名字 ⇒ 抛（⛔ 不许静默返回空集：那会把所有 routed 行误判成 unmapped）。
    """
    src = io.open(path, encoding="utf-8").read()
    i = src.index("static bool IsRoutedTriggerName")
    j = src.index("static string CanonicalTriggerName", i)
    body = src[i:j]
    names = set(m.group(1) for m in re.finditer(r'case\s+"([^"]+)"\s*:', body))
    for m in re.finditer(r'string\.Equals\(\s*triggerName\s*,\s*KeywordTable\.(\w+)', body):
        names.add(m.group(1))
    if len(names) < 10:
        raise SystemExit("解析 IsRoutedTriggerName 只拿到 %d 个名字 ⇒ 解析法过期，停手" % len(names))
    return names


def must_route_names(path=VFXMAP):
    """`MustRouteTriggerNames` 数组字面量（正向锁；用来交叉校验上面的解析）。"""
    src = io.open(path, encoding="utf-8").read()
    i = src.index("static readonly string[] MustRouteTriggerNames")
    j = src.index("};", i)
    body = src[i:j]
    # 去掉行注释，只留引号里的名字
    body = "\n".join(l.split("//")[0] for l in body.splitlines())
    return set(re.findall(r'"([^"]+)"', body))


def no_emit_names(path=VFXMAP):
    src = io.open(path, encoding="utf-8").read()
    i = src.index("static readonly string[] NoEmitTriggerNames")
    j = src.index("};", i)
    body = "\n".join(l.split("//")[0] for l in src[i:j].splitlines())
    return set(re.findall(r'"([^"]+)"', body))


# 归口规则的正本 = `工具/gen_card_vfx_by_card.py` 的 `TRIGGER_EVENT` / `ABILITY_BEAT_TRIGGERS`
# / `trigger_event()`（⛔ 不在这里抄第二份：两处写同一条规则 = 迟早不一致）。
# 本脚本只把生成器**当模块导入**、用它的判定，自己不复写逻辑。
_TOOLS = ROOT + "/工具"
if _TOOLS not in sys.path:
    sys.path.insert(0, _TOOLS)
import importlib.util as _ilu  # noqa: E402

_spec = _ilu.spec_from_file_location("gen_card_vfx_by_card", _TOOLS + "/gen_card_vfx_by_card.py")
gen = _ilu.module_from_spec(_spec)
# ⚠️ 生成器模块级有一句「`-h`/`--help` 就打印用法并 `exit(0)`」的保护 ⇒ 导入期间把 argv 藏起来
# （否则我们自己的 `--help` 会把生成器的用法打出来然后**整个进程退出 0**）。
_argv_bak = sys.argv
sys.argv = [sys.argv[0]] if sys.argv else ["w2_classify"]
try:
    _spec.loader.exec_module(gen)
finally:
    sys.argv = _argv_bak

MAPPED = gen.TRIGGER_EVENT
ABILITY_BEAT = gen.ABILITY_BEAT_TRIGGERS
# 与 VfxMap.IsAbilityTrigger 同一口径（-1=activeAbility / 400 / 600 / 605）—— 只用于**诊断**，
# 判据正本在 `Core/VfxMap.cs`（本文件的分类以生成器写进条目里的 `abilityEventClass` 为准）。
ABILITY_CLASS = {-1, 400, 600, 605}


def classify(e):
    """一条 `ability` 桶条目 → (档, 名字, 目标桶)。

    🆕 2026-10-24（`W2`）：**优先读条目自带的那一列**（`abilityEventClass`，= 生成器
    `trigger_event()` 那一次判定的产物）；老数据没有这一列时才按 `trigger_event()` 现判。
    ⇒ 统计与生成用**同一次**判定（⛔ 不两处各判一遍）。
    """
    slot = e.get("abilitySlot") or ""
    cls = e.get("abilityEventClass")
    tn = e.get("abilityTriggerName")
    tg = e.get("abilityTrigger")
    if cls:
        dst = {"mapped": MAPPED.get(tn, "?"), "ability": "ability",
               "routed": "trigger", "unmapped": "trigger_unmapped",
               "no-name": "trigger_unmapped"}.get(cls, "?")
        return cls, tn, dst
    # ---- 老数据（没有 `abilityEventClass`）：按同一个 `trigger_event()` 现判 ----
    if slot.startswith("activeAbility"):
        return "ABILITY", "activeAbility", "ability"
    if tg in ABILITY_CLASS:
        return "ABILITY", tn, "ability"
    ev, _why, cls2 = gen.trigger_event(tn)
    if cls2 == "mapped":
        return "MAPPED", tn, ev
    if cls2 == "ability":
        return "ABILITY", tn, "ability"
    if cls2 == "routed":
        return "ROUTED", tn, "trigger"
    return "UNMAPPED", tn, "trigger_unmapped"


def main():
    path = FULL
    if "--json" in sys.argv:
        path = sys.argv[sys.argv.index("--json") + 1]
    d = json.load(io.open(path, encoding="utf-8"))

    global ROUTED_LC, ROUTED, MUST, NOEMIT
    ROUTED = routed_names()
    ROUTED_LC = set(n.lower() for n in ROUTED)
    MUST = must_route_names()
    NOEMIT = no_emit_names()
    miss = [n for n in MUST if n.lower() not in ROUTED_LC]
    bad = [n for n in NOEMIT if n.lower() in ROUTED_LC]
    print("== 白名单解析 ==")
    print("  IsRoutedTriggerName 名字数 : %d" % len(ROUTED))
    print("  MustRouteTriggerNames 中不在白名单 : %s" % (miss or "（无）"))
    print("  NoEmitTriggerNames 中却在白名单 : %s" % (bad or "（无）"))

    import collections
    cls = collections.Counter()
    cls_cards = collections.defaultdict(set)
    unmapped = collections.Counter()
    unmapped_cards = collections.defaultdict(set)
    ability_rows = 0
    ability_cards = set()
    for cid, c in sorted(d["byCard"].items()):
        for e in c.get("ability") or []:
            ability_rows += 1
            ability_cards.add(cid)
            k, tn, dst = classify(e)
            cls[(k, dst)] += 1
            cls_cards[(k, dst)].add(cid)
            if k == "UNMAPPED":
                unmapped[tn] += 1
                unmapped_cards[tn].add(cid)
    print()
    print("== ability 桶现状（%s）==" % path)
    print("  行 %d / 卡 %d" % (ability_rows, len(ability_cards)))
    for (k, dst), n in sorted(cls.items(), key=lambda kv: -kv[1]):
        print("  %-9s → %-16s 行=%-4d 卡=%-4d" % (k, dst, n, len(cls_cards[(k, dst)])))
    print()
    print("== UNMAPPED 逐个 trigger 名（改前这些会默认进 `ability`；改后在 `trigger_unmapped`）==")
    tot = 0
    for tn, n in unmapped.most_common():
        tot += n
        print("  %-32s 行=%-3d 卡=%d" % (tn, n, len(unmapped_cards[tn])))
    print("  合计 %d 行 / %d 卡" % (tot, len(set().union(*unmapped_cards.values()) if unmapped_cards else set())))


if __name__ == "__main__":
    main()
