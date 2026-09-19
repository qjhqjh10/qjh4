# -*- coding: utf-8 -*-
"""scan_inline_keywords.py —— 扫「带正文关键词」的可疑项：只挑三种精确形状，不碰正常写法。

用法：PYTHONIOENCODING=utf-8 python d:/4/Unity/工具/scan_inline_keywords.py
落地记录与逐条证据：`资料/普查产出_0919/关键词误抽_逐张核对_13张.md`。

为什么不用「关键词有没有独立成段」那类宽判据（试过，111 条里绝大多数是**正常写法**）：
本工程有一条**裸写正文**约定 —— 卡面写 `<图标>Keyword: <正文>` 时，数据管线会把前缀收进
`keywords` 列、`desc` 只留正文（例 `Blood Claw` kw=['Ferocity'] desc='Deal 3 damage to an enemy'）。
所以「desc 里没有关键字面」**恰恰是正常的**。真正可疑的只有三种：
  ① desc 里**引号内**出现 `Kw:`  ⇒ 那是**授予别人**的能力（`Beastboss` 那条踩过）
  ② desc 里该词带**引用动词**（uses/triggers/with/has/gains）⇒ 句中引用，不是本卡能力
  ③ desc 是**事件从句/常驻句**开头（When / At the / For the rest of / After receiving）⇒ 不像这个关键词的正文
"""
import io
import json
import re

BODY = ['rally', 'strike', 'slay', 'backlash', 'penitence', 'mob', 'regiment', 'cruelty',
        'artifice', 'agenda', 'ferocity', 'pray', 'duty', 'uprising', 'teleport',
        'stimulation', 'ambush', 'codex']

path = 'd:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json'
d = json.load(io.open(path, encoding='utf-8'))
cs = d['cards'] if isinstance(d, dict) and 'cards' in d else d


def strip_icons(s):
    return re.sub(r'【图标:[^】]*】', '', s or '').strip()


QUOTED = re.compile(r'["\']([^"\']+)["\']')

hits = []
for c in cs:
    desc = c.get('desc') or ''
    kws = [re.sub(r'\s+\d+$', '', x).strip().lower() for x in (c.get('keywords') or [])]
    for kw in sorted(set(kws)):
        if kw not in BODY:
            continue
        why = None
        # ⓿ **先排除真写了的**：desc 里只要有 `<Kw>:` / `<Kw> N:` / 图标写法 `[Kw] …`，
        #    那就是这张卡**自己的**能力（`Bjorn the Fell-Handed` 的 `Ferocity: Deal …` 就是这种，
        #    它同时也**引用**了 Ferocity，被 ② 误伤过一次）。
        #    ⚠️ **判断前先把引号里那截抠掉** —— 引号里的 `Kw:` 是**授予别人**的，不是本卡的
        #       （`Beastboss` 的 `have "Slay: Gain Blood Thirst"`；不抠掉就会把这一族全漏掉）。
        unquoted = QUOTED.sub(' ', desc)
        if re.search(r'(^|[^a-z])' + kw + r'(\s+\d+)?\s*:', unquoted, re.I):
            continue
        if re.search(r'\[(' + kw + r')(\s+\d+)?\]', unquoted, re.I):
            continue
        for q in QUOTED.findall(desc):
            if re.match(r'\s*' + kw + r'\s*:', q, re.I):
                why = '引号里授予别人'
        if not why and re.search(r'\b(uses?|triggers?|with|has|have|gains?)\s+' + kw + r'\b', desc, re.I):
            why = '句中被引用(uses/with/has/gains)'
        if not why and re.match(r'\s*(when|whenever|at the|for the rest of|after receiving)', desc, re.I):
            why = '正文是事件从句/常驻句'
        if why:
            hits.append((c['id'], c['type'], c['name'], kw, why, strip_icons(desc)[:78]))

print('可疑 %d 条：' % len(hits))
by = {}
for h in hits:
    by.setdefault(h[1], []).append(h)
for t in ('unit', 'hero', 'tactic', 'defence'):
    for h in by.get(t, []):
        print('  %-32s %-7s | %-22s | %-10s | %s | %s' %
              (h[0], h[1], h[2][:22], h[3], h[4], h[5]))
