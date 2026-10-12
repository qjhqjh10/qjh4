# -*- coding: utf-8 -*-
# aura_dryrun.py — 把「光环句」的锚定正则拿**全卡池**试一遍（不碰工程，纯读 JSON）。
#
# **什么时候跑**：动 `Core/Aura.cs` 里那几条正则（`ReAura` / `ReCostCombo` / `ReCostOnly` /
# `ReSelfTurn` / `ReRemnantStay`）**之前和之后**。这一族最大的风险是**从句子中间匹配**，
# 把前/后半句静静吃掉 —— 实测那批「长得像」的句子（`If it has Flying, …` /
# `If the target has Armour, …` / `Has Flying during your turn` / `Rally: If you have …`）
# **必须一条都不被吃**，而这件事**读代码看不出来**，只能拿全池跑一遍。
#
# 用法：`PYTHONIOENCODING=utf-8 python Unity/工具/aura_dryrun.py`（产物 `_tmp_view/aura_dryrun.txt`）
#
# ⚠️ **它和 `Aura.cs` 的正则是两份**（语言不同，没法共用）—— 改了 C# 那份**要同步改这里**，
#    否则这把尺子会量错。同步的办法：跑 `EffectParseProbe` 抽几条真句子对一下两边的结论。
import json, re, io, os, glob, collections

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
path = glob.glob(os.path.join(REPO, 'Unity', 'MyGame', 'Assets', 'RuleEngine', 'Resources',
                              'cards_engine.json'))[0]
d = json.load(io.open(path, encoding='utf-8'))
cards = d if isinstance(d, list) else d.get('cards', d)
out = ["cards_engine.json = %s  卡数 %d" % (path, len(cards))]

HEADS = r'(adjacent|your other|other friendly|friendly|your|enemy|enemies)'
# 先剥「费用那半」：`<head> <subject> cost(s) N less/more and have <payload>`
COST = re.compile(r'^%s\s+(.*?)\s+costs?\s+(\d+)\s+(less|more)\s+and\s+have\s+(.+)$' % HEADS, re.I)
# 标准形：`<head> [subject] have|has <payload>`（主语可以空：`Enemies have Vulnerable 1`）
MAIN = re.compile(r'^%s\s*(.*?)\s*(?:have|has)\s+(.+)$' % HEADS, re.I)
# 🆕 `A1458`：**只有费用那半、连 `have` 都没有** —— `<head> <subject> cost(s) N less`（整句到底）。
#    同步自 `Core/Aura.cs` 的 `ReCostOnly`。实测全池 **7 句 / 7 张**（全是 unit）。
COSTONLY = re.compile(r'^%s\s+(.+?)\s+costs?\s+(\d+)\s+less$' % HEADS, re.I)
# 🆕 `A1458`：`ReCostOnly` 的**排除词**（同步 `Core/Aura.cs` 的 `ReCostOnlyOneShot`）——
#    卡面写 `next` / `this turn` / `for the rest of this battle` 的是**一次性降费**，
#    已经由 `EffectText.TryLowerCost` 的 ④/⑤ 支收成 `lowercost` op、`PlayTactic` 消费
#    ⇒ **光环这一层【故意】不收它**（两处登记同一条规则 = 迟早不一致）。
COSTONLY_ONESHOT = re.compile(r'\bnext\b|\bthis turn\b|\bfor the rest of this battle\b', re.I)
# 两种特殊形状
SELF = re.compile(r'^(?:has\s+)?flying\s+during\s+your\s+turn$', re.I)
REMNANT = re.compile(r'^adjacent\s+remnants?\s+do\s+not\s+disappear\s+at\s+the\s+end\s+of\s+your\s+turn$', re.I)


def segs(t):
    return [s.strip() for s in re.split(r'[.\n\r]', t or '') if s.strip()]


hit, cost_hit, costonly_hit, costonly_oneshot, special, others = [], [], [], [], [], []
for c in cards:
    typ = c.get('type')
    for src in [c.get('desc')] + list(c.get('keywords') or []):
        for seg in segs(src):
            if REMNANT.match(seg) or SELF.match(seg):
                special.append((c.get('name'), typ, seg)); continue
            m = COST.match(seg)
            if m:
                cost_hit.append((c.get('name'), typ, seg)); continue
            m = COSTONLY.match(seg)
            if m:
                # ⚠️ 分组序号：`HEADS` 在 Python 这份里是**第 1 个捕获组** ⇒
                #    `group(1)=锚点` / `group(2)=主语` / `group(3)=N`（C# 那份是**具名组**，别照抄名字）。
                if COSTONLY_ONESHOT.search(m.group(2)):
                    costonly_oneshot.append((c.get('name'), typ, seg))
                else:
                    costonly_hit.append((c.get('name'), c.get('faction'), typ,
                                         m.group(2), m.group(3), seg))
                continue
            m = MAIN.match(seg)
            if m:
                hit.append((c.get('name'), c.get('faction'), typ, m.group(1), m.group(2), m.group(3), seg))
            elif re.search(r'\b(have|has)\b', seg, re.I) and typ in ('unit', 'hero'):
                others.append((c.get('name'), typ, seg))

out.append("")
out.append("=== A) 特殊形状（自指飞行 / 残骸留场）%d 处 ===" % len(special))
for n, t, s in special: out.append("  [%s] %s :: %s" % (t, n, s))
out.append("")
out.append("=== B) 「费用+属性合体」形 %d 处 ===" % len(cost_hit))
for n, t, s in cost_hit: out.append("  [%s] %s :: %s" % (t, n, s))
out.append("")
out.append("=== B2) 🆕 `A1458`「只有费用那半」形 %d 处 / 卡 %d 张（`ReCostOnly`；必须全是 unit）===" %
           (len(costonly_hit), len(set(h[0] for h in costonly_hit))))
for n, f, t, subj, amt, seg in costonly_hit:
    out.append("  [%s] %-28s | %-14s | 筛「%s」 | -%s | %s" % (t, n, f, subj, amt, seg))
badco = [h for h in costonly_hit if h[2] not in ('unit', 'hero')]
out.append("  （非 unit/hero 的 %d 处 —— 必须为 0）" % len(badco))
for n, f, t, subj, amt, seg in badco: out.append("    [%s] %s :: %s" % (t, n, seg))
out.append("")
out.append("=== B3) 🆕 `A1458` 同形但【故意不收】的（一次性降费，`ReCostOnlyOneShot`）%d 处 ===" %
           len(costonly_oneshot))
for n, t, s in costonly_oneshot: out.append("  [%s] %s :: %s" % (t, n, s))
out.append("")
out.append("=== C) 标准形 %d 处 / 去重 %d 种 / 卡 %d 张 ===" %
           (len(hit), len(set(h[6] for h in hit)), len(set(h[0] for h in hit))))
for s, n in collections.Counter(h[6] for h in hit).most_common(): out.append("  x%d  %s" % (n, s))
out.append("")
out.append("=== D) 逐条拆解：类型 | 卡名 | 锚点 | 主语 | 载荷 ===")
for n, f, t, head, subj, pay, seg in sorted(hit, key=lambda x: (x[3], x[4])):
    out.append("  [%s] %-28s | %-13s | %-32s | %s" % (t, n, head, subj or '(空)', pay))
out.append("")
out.append("=== E) 单位/督军卡里含 have/has 但**没被命中**的（%d 处 / %d 种）—— 必须不被吃掉 ===" %
           (len(others), len(set(o[2] for o in others))))
for s, n in collections.Counter(o[2] for o in others).most_common(): out.append("  x%d  %s" % (n, s))
out.append("")
out.append("=== F) 非 unit/hero 卡被命中的（应为 0）===")
bad = [h for h in hit if h[2] not in ('unit', 'hero')]
for n, f, t, head, subj, pay, seg in bad: out.append("  [%s] %s :: %s" % (t, n, seg))
out.append("  （共 %d 处）" % len(bad))

dst = os.path.join(REPO, '_tmp_view', 'aura_dryrun.txt')
io.open(dst, 'w', encoding='utf-8').write("\n".join(out))
print("OK -> %s" % dst)
