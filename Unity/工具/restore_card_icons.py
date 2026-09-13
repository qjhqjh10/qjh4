#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
restore_card_icons.py —— 把卡表提取里的 `【图标:X】` 占位符**还原成真关键词**。

## 为什么要这么干（而不是模板匹配）

卡表提取（`资料/卡表核对_卡图提取/_合并总表.md`）里的效果文字是子代理**逐张看图**抄的，
图标统一写成 `【图标:形状】`，规格明说「**认不出语义就写 `?`，不要猜**」。
原来的计划是**从卡图上抠图标、和 `icons/*.png` 做模板匹配**。

2026-09-13 实查之后**否掉了那条路**，两条硬证据：

1. 🔴 **形状名认不出图标**：29 个子代理给 78 个图标起了 **180 个不同的形状名**
   （1968 次出现）。`【图标:骷髅】` 出现 130 次，后接的词是
   `Remnant ×34 / Strike ×18 / Synapse ×16 / Regiment ×13 / Backlash ×10` ——
   一个「骷髅」形状其实横跨十几个完全不同的图标。**按形状匹配 = 拿噪声当键。**
2. ✅ **卡面上关键词图标旁边就印着英文词**（2026-09-13 看真卡确认，
   例 `Aeldari/3部队/Warpforge_03_Shining-Spear.png`：
   `【翅膀】Flying. 【箭头】Flank. … 【星】Shuriken 2`）。
   ⇒ **答案本来就在文本里**，不用认图。

**例外（真的需要按形状还原的）**：**数值图标是单独出现的、后面没有词** ——
`Give +1 【图标:拳头】 and +1 【图标:枪】 to your troops`（真卡确认：
`Ultramarines/2天赋/Warpforge_00d_Paragon-of-Ultramar-2.png`）。
那一类走下面 `STANDALONE` 小表，**逐条标出处**。

## 用法

    python Unity/工具/restore_card_icons.py            # 只报告，不写文件
    python Unity/工具/restore_card_icons.py --write    # 落盘

产物：
  · `资料/卡表核对_卡图提取/_还原效果文字.md` —— 每张卡的**还原后英文效果文字**
  · 控制台报告：还原率 / 形状↔关键词 交叉表 / 没还原出来的清单
"""

import io, os, re, sys, json, collections

# ⚠️ Windows 控制台默认 GBK，报告里有 ↗ ↔ 这类符号会直接抛 UnicodeEncodeError。
#    统一把 stdout 改成 UTF-8（本机 python 3.14）。
try:
    sys.stdout.reconfigure(encoding='utf-8')
except Exception:
    pass

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # d:/4/Unity
OUTDIR = os.path.join(ROOT, '资料', '卡表核对_卡图提取')
SRC    = os.path.join(OUTDIR, '_合并总表.md')
RULE   = os.path.join(ROOT, '资料', '关键词图标', '_规则书关键词表.md')
ICONS  = os.path.join(ROOT, '资料', '关键词图标', '关键词与图标_对照表.md')

ICON_RE = re.compile(r'【图标:([^】]+)】')

# ---------------------------------------------------------------------------
# **单独出现**的图标（后面没有词）—— 只能按形状还原。
#   每条都要有真卡佐证，不许按「看起来像」填。
#   2026-09-13：42 处没还原的全部逐张看了卡图才定下来。
# ---------------------------------------------------------------------------
DARK_ANGELS = '`Dark Angels/4计策/Warpforge_48_Grim-Resolve.png`'

STANDALONE = {
    # ---- 属性图标（近战 / 远程）----
    '拳头':   ('Melee',  '真卡 `Ultramarines/2天赋/Warpforge_00d_Paragon-of-Ultramar-2.png`：'
                         '`Give +1 (拳) and +1 (枪) to your troops` —— 图标前是 +1、后无词'),
    '枪':     ('Ranged', '同上那张卡'),
    '粉圆纹': ('Melee',  '真卡 `Space Wolves/2天赋/Warpforge_07_Murderous-Hurricane.png`：'
                         '`Give +2 (粉圈拳头) to a friendly unit this turn`'),
    '紫圆纹': ('Ranged', '与 `粉圆纹` 成对（`Give +N 粉圆纹, +N 紫圆纹 and +N Health`，'
                         '7 张卡都这么写）；粉圈拳头 = Melee ⇒ 紫圈 = Ranged'),
    '紫圆(枪)': ('Ranged', '形状名里就写着「枪」；`Give +2 (紫圆枪) to a friendly troop`'),
    '紫色圆·枪': ('Ranged', '同上'),
    '准星':   ('Ranged', '仅当**单独出现**时走这条（前面若跟 `Sniper` 走词那条路）。'
                         '真卡 `Necron/2天赋/Warpforge_02_Hardwired-Destruction.png`：'
                         '`Give +1 (拳) and +1 (准星)` —— 与拳头成对'),
    '爪':     ('Ranged', '仅当**单独出现**时走这条。真卡 '
                         '`Sorotitas/1督军/Warpforge_01_Celestian-Sacresant-Aveline.png`：'
                         '`Pray: Give +1 (粉圈拳) and +1 (紫圈枪)` —— 10 处里 8 处都是这个成对写法'),

    # ---- 阵营资源 ----
    # ⚠️ 同一个形状名 `齿轮` 其实是**两个不同的图标**，靠「后面跟不跟词」分开：
    #    · 跟 `Artifice` → `icons/artifice.png`（白色齿轮，混沌/黑军团/基因窃取者都有）
    #    · 跟**数字**     → `icons/questPointsN.png`（带放射齿纹的大数字）
    #    真卡对照：`Chaos/3部队/Warpforge_40_Dark-Apostle.png` 是白齿轮；
    #             6 张 Dark Angels 卡（Grim Resolve 等）是数字徽章。
    '齿轮':   ('Quest Point', '暗黑天使的阵营资源，**数字紧跟其后**（`gain (徽章)1/2/3`）。'
                              f'真卡 {DARK_ANGELS}：`Deal 2 damage. If the target dies, gain (①)` —— '
                              '图标与 `icons/questPoints1.png` 逐字对上（带放射齿纹的「1」）。'
                              '⚠️ 与 `artifice.png` 的白齿轮**不是同一个图标**，'
                              '靠「后接 Artifice 一词」区分'),
    '太阳':   ('Energy', '修女会卡面 `8 ☀: Destroy all enemy troops` —— ☀ 是能量记号'),
}

# 形状名里带这些**前缀**的（子代理有时写成 `拳头(红圆)` / `枪(紫圆)` / `紫圆(枪)`）
PREFIX_FALLBACK = [
    ('拳头', 'Melee'), ('粉圆', 'Melee'),
    ('枪', 'Ranged'), ('紫圆', 'Ranged'),
]
# 数字圈：`【图标:圆内有数字1】` / `绿圈1` … = **这一行的能量费用**。
#   真卡佐证 `Aeldari/3部队/Warpforge_03_Shining-Spear.png`：`(绿圈 1) Gain (星) Shuriken 2 this turn`
NUM_RE = re.compile(r'(?:圆内有数字|绿圈)(\d)')

# 形状名里带「拳/枪/盾」等**前缀描述**的（子代理有时写成 `拳头(红圆)` / `枪(紫圆)`）
PREFIX_FALLBACK = [
    ('拳头', 'Melee'), ('枪', 'Ranged'),
]


def load_vocab():
    """关键词英文名。两个来源合起来：规则书 61 条 + 图标对照表 78 个。"""
    words = set()
    for path in (RULE, ICONS):
        if not os.path.exists(path):
            continue
        for line in io.open(path, encoding='utf-8'):
            if not line.startswith('|'):
                continue
            cells = [c.strip() for c in line.strip().strip('|').split('|')]
            if len(cells) < 2:
                continue
            w = cells[1]
            # 英文名列：只收纯英文（含 ' 与空格、允许 Can't 这种）
            if re.fullmatch(r"[A-Za-z][A-Za-z'’\- ]{1,24}", w or ''):
                words.add(w)
    # 规则书正文/卡面里出现、但上面两张表没单列的
    words |= {'Melee', 'Ranged', 'Energy', 'Health', 'Attack', 'Cost', 'Type', 'Skulls',
              'Concussive', 'Dark Pact', 'Dark Pacts', 'Destroyer', 'Penitence',
              'Stratagem', 'Reload', 'Artifice'}
    # 长词优先（`Blood Thirst` 要压过 `Blind`；`Dark Pact` 压过 `Dark`）
    return sorted(words, key=len, reverse=True)


def resolve(shape, after, before):
    """→ (关键词 or None, 依据)"""
    # ⚠️ **必须 lstrip**：`after` 是从图标结束处**原样切下来**的，开头几乎总有一个空格。
    #    忘了这一步，`after[:6]` 就变成 `" Flyin"`，拿它跟 `flying` 比**永远不等** ——
    #    2026-09-13 撞到过：460 处「没还原」里绝大多数是这个 bug，不是数据的问题。
    after = after.lstrip()
    before = before.rstrip()
    # ① 图标**后面**紧跟关键词英文名（绝大多数走这条）
    for w in VOCAB:
        if after[:len(w)].lower() == w.lower():
            # 词尾必须是边界（`Armour` 不能吃掉 `Armoury`）
            nxt = after[len(w):len(w)+1]
            if nxt == '' or not (nxt.isalnum() or nxt == "'"):
                return w, '图标后紧跟英文词'
    # ② 单独出现的数值图标 —— 按形状查小表
    if shape in STANDALONE:
        return STANDALONE[shape][0], STANDALONE[shape][1]
    for pre, kw in PREFIX_FALLBACK:
        if shape.startswith(pre):
            return kw, f'形状名以「{pre}」开头（数值图标）'
    m = NUM_RE.search(shape)
    if m:
        return m.group(1), '数字圈 = 这一行的能量费用（真卡佐证见文件头）'
    # ③ 再看**前面**有没有词（极少数写法把图标放在词后）
    for w in VOCAB:
        if before[-len(w):].lower() == w.lower():
            return w, '图标前紧跟英文词'
    return None, None


def main():
    global VOCAB
    VOCAB = load_vocab()
    txt = io.open(SRC, encoding='utf-8').read()
    lines = txt.split('\n')

    stats = collections.Counter()
    shape2kw = collections.defaultdict(collections.Counter)
    unresolved = collections.Counter()
    unresolved_ex = {}
    out_rows, header = [], None

    for line in lines:
        if not line.startswith('|'):
            continue
        cells = line.split('|')
        if len(cells) < 12:
            continue
        if cells[1].strip() == '文件名':
            header = cells
            continue
        eff_idx = len(cells) - 3            # 效果原文列
        eff = cells[eff_idx]
        if '【图标' not in eff:
            out_rows.append((cells[1].strip(), cells[2].strip(), eff.strip(), 0))
            continue

        restored = eff
        # 从后往前替换，避免改下标
        for m in reversed(list(ICON_RE.finditer(eff))):
            shape = m.group(1)
            after = eff[m.end():m.end()+40]
            before = eff[max(0, m.start()-30):m.start()]
            kw, why = resolve(shape, after, before)
            if kw is None:
                stats['没还原'] += 1
                unresolved[shape] += 1
                # ⚠️ 存**出错那一处**的上下文，不是整段文字 ——
                #    存整段的话，例子里显示的那一处其实可能是**已经还原成功**的，根本看不出问题。
                unresolved_ex.setdefault(
                    shape, (cells[2].strip(), '…' + eff[max(0, m.start()-30):m.end()+30].strip() + '…'))
                restored = restored[:m.start()] + '❓' + restored[m.end():]
            else:
                stats['还原了'] += 1
                if why == '图标后紧跟英文词':
                    stats['· 靠后接词'] += 1
                else:
                    stats['· 靠形状/小表'] += 1
                shape2kw[shape][kw] += 1
                restored = restored[:m.start()] + kw + restored[m.end():]

        out_rows.append((cells[1].strip(), cells[2].strip(),
                         re.sub(r'\s+', ' ', restored).strip(),
                         len(ICON_RE.findall(eff))))

    # ---- 报告 ----
    p = print
    p(f'占位符合计 {stats["还原了"] + stats["没还原"]}：还原 {stats["还原了"]}'
      f'（后接词 {stats["· 靠后接词"]} · 形状/小表 {stats["· 靠形状/小表"]}）'
      f' · 没还原 {stats["没还原"]}')
    p()
    p('── 形状 ↔ 还原出的关键词（只看出现 ≥5 次的形状，看它「稳不稳」）──')
    for shape, c in sorted(shape2kw.items(), key=lambda kv: -sum(kv[1].values())):
        n = sum(c.values())
        if n < 5:
            continue
        top = ' · '.join(f'{k}×{v}' for k, v in c.most_common(6))
        flag = '⚠️ 一个形状对应多个关键词（形状名不可靠）' if len(c) > 1 else '✅ 一致'
        p(f'  【{shape}】×{n}  → {top}   {flag}')
    p()
    if unresolved:
        p(f'── 没还原出来（{len(unresolved)} 种 / {sum(unresolved.values())} 次）──')
        for shape, n in unresolved.most_common():
            cname, ctx = unresolved_ex[shape]
            p(f'  【{shape}】×{n}  卡={cname}')
            p(f'       {ctx}')
    else:
        p('── 没还原出来：无 ──')

    if '--write' in sys.argv:
        out = os.path.join(OUTDIR, '_还原效果文字.md')
        with io.open(out, 'w', encoding='utf-8') as f:
            f.write('# 卡面效果文字（占位符已还原）\n\n')
            f.write('> 由 `Unity/工具/restore_card_icons.py` 从 `_合并总表.md` 生成，**别手改**。\n')
            f.write('> 还原依据：卡面上**关键词图标旁边就印着英文词**（2026-09-13 看真卡确认），\n')
            f.write('> 所以答案在文本里、不用认图；只有**数值图标**（拳头/枪/太阳/数字圈）走小表。\n')
            f.write('> 为什么不用模板匹配，见脚本文件头。\n\n')
            f.write('| 文件名 | 卡名 | 效果文字（还原后） | 图标数 |\n|---|---|---|---|\n')
            for fn, name, eff, n in out_rows:
                f.write(f'| {fn} | {name} | {eff.replace("|", "/")} | {n} |\n')
        p(f'\n已写入 {out}')


if __name__ == '__main__':
    main()
