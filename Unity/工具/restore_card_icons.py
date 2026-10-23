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

    python Unity/工具/restore_card_icons.py                     # 只报告（含自检）
    python Unity/工具/restore_card_icons.py --write             # 落盘（**自检红就不写**）
    python Unity/工具/restore_card_icons.py --check             # 只跑自检、给退出码（不落盘）
    python Unity/工具/restore_card_icons.py --legacy-smalltable # ⚠️ **演示开关**：退回 2026-10-12 改前的错小表

`--legacy-smalltable` 只为**现场演示「自检能红」**（照 `gen_cardface_fixes.py --legacy-bare-keys`
那套做法）—— 它把「太阳 → Energy」「数字圈 → 裸数字」这两条改前的错口径放回来，
⛔ **正常流程一条都不许走**。

产物：
  · `资料/卡表核对_卡图提取/_还原效果文字.md` —— 每张卡的**还原后英文效果文字**
  · 控制台报告：还原率 / 形状↔关键词 交叉表 / 没还原出来的清单 / **自检四条**

## 2026-10-12（`A1604`）就地订正的**三组 + 三处**（铁律 5）

「两份对账文件互相打架」那句话**性质写错了**：`_合并总表.md`（记**形状名**）与
`_还原效果文字.md`（记**还原出来的词**）不是两个并列的证人，是**同一条管线的前后两棒** ——
真错在**第二棒的小表**（本文件）。全池 331 处走小表的，本轮逐组复核，改了三组、补了三处：

| 形状 | 处 | 改前 | **改后（真值）** | 判据 |
|---|---|---|---|---|
| `太阳` | 43 | `Energy` ❌ | **`Faith`** | PnP 亲读 `Sorotitas/3部队/Warpforge_42_Exorcist.png` = `traits/faith.png` 同图 · `_图标清单.md:31` · 卡池 `[faith]`/`☀` |
| `圆内有数字N` | 19 | 裸 `N` ❌ | **`N Quest Point(s)`** | PnP 亲读 `Dark Angels/1督军/Warpforge_01_Asmodai.png`（齿环徽记「1」）· 卡池 19 张全写 `N Quest Point(s)` |
| `绿圈N` | 17 | 裸 `N` ❌ | **`N [Spirit Stone]`** | PnP 亲读 `Aeldari/3部队/Warpforge_03_Shining-Spear.png`（绿圈 + 白「1」）· 卡池 17 张全写 `N [Spirit Stone]` |
| `暗色圆徽` | 2 | `❓` | **`Quest Point`** | PnP 亲读 `Dark Angels/3部队/Warpforge_08_Apothecary.png`（暗色齿环徽记）· 卡池 `DA8`/`DA15` |
| `螺旋` + **紧跟数字** | 1 | `❓` | **`Quest Point`** | PnP 亲读 `Dark Angels/3部队/Warpforge_43_Repulsor.png`（紫色齿环 + 白「3」= `questPoints3`）· 卡池 `DA43` |

⚠️ `螺旋` **只按「后面跟不跟数字」分流** —— 它在别处 **12 次确实是 `Stun`**（金螺旋徽记）、
1 次是 `Fast`，那条路走的是「图标后紧跟英文词」那一支，**与小表无关**；
⛔ **不许**把 `螺旋` 整条写进 `STANDALONE`（那样一旦哪处后面没跟词，就会**静默**画成任务点）。

**改前那两条错口径的危害面**：`Energy` 这个错标签**没有污染过计划表**（现读计划表里没有任何
`"sprite": "energy"`），因为下游 `gen_icon_plan.py` 的 `ALIAS` 里早有一条 `"Energy": "faith"` 挡着
（那条**本轮已随这个修复删掉**，见那边的注释）。
"""

import io, os, re, sys, json, collections

# ⚠️ Windows 控制台默认 GBK，报告里有 ↗ ↔ 这类符号会直接抛 UnicodeEncodeError。
#    统一把 stdout 改成 UTF-8（本机 python 3.14）。
## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法：python 工具/restore_card_icons.py [--legacy-smalltable] [--write] [--check]\n  说明：restore_card_icons.py —— 把卡表提取里的 `【图标:X】` 占位符**还原成真关键词**。\n  选项：--legacy-smalltable / --write / --check（脚本内按字符串匹配；一个都不带 = 默认模式）')
    _wf_help_sys.exit(0)

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
    '太阳':   ('Faith', '修女会的**信仰**记号（金太阳）。判据：PnP 亲读 '
                        '`Sorotitas/3部队/Warpforge_42_Exorcist.png` —— 那枚与 `traits/faith.png` 同图；'
                        '`资料/关键词图标/_图标清单.md:31`（`faith.png` = 深棕古铜方底 + 金色太阳 + 十字尖芒 = 信仰）；'
                        '`资料/关键词图标/关键词与图标_对照表.md:105`（`icons/faith.png | 信仰 | Faith`）；'
                        '卡池同位置写 `[faith]` / `[Faith]` / `☀`'),
    # 🔴 2026-10-12（`C5` · `A1604`）**补两条一直没还原出来的**：形状 `暗色圆徽` ×2 全在**暗黑天使**
    #    （`Apothecary` / `Company Veteran`），卡面那枚 = **任务点徽记**。
    #    判据（PnP 亲读，本轮做的）：
    #      · `Dark Angels/3部队/Warpforge_08_Apothecary.png`：
    #        `When a friendly unit obtains 〔**深色圆盘 + 一圈齿纹 + 中央展翅纹章**〕, it heals 2.`
    #        —— 与 `gen_icon_plan.py` 给 `("DA8","[Quest Point]")` 写的证据
    #        「环底铭牌带 + 中央展翅矛纹章 = `icons/questPoints.png`」**是同一枚**；
    #      · `Dark Angels/3部队/Warpforge_15_Company-Veteran.png` 同一枚；
    #      · 卡池这两张写的就是它：`DA8` = `When a friendly unit obtains [Quest Point], it heals 2`、
    #        `DA15` = `When you gain [Quest Point], heal 2`。
    '暗色圆徽': ('Quest Point', '暗黑天使的**任务点**徽记（深色圆盘 + 齿纹 + 中央展翅纹章）。'
                                '判据：PnP 亲读 `Dark Angels/3部队/Warpforge_08_Apothecary.png`；'
                                '卡池 `DA8` `When a friendly unit obtains [Quest Point], it heals 2` · '
                                '`DA15` `When you gain [Quest Point], heal 2`'),
}

# 形状名里带这些**前缀**的（子代理有时写成 `拳头(红圆)` / `枪(紫圆)` / `紫圆(枪)`）
# 🔴 2026-10-12（`C5`）**就地合并了一份重复定义**：本表原来在文件里写了**两遍**
#    （第一份带 `粉圆`/`紫圆`，第二份只有 `拳头`/`枪`），**Python 是后者静默赢** ⇒ 第一份从来没生效过。
#    现在合成一份 = 两份的并集；本轮实测「形状 ↔ 关键词」整张交叉表**逐行不变**
#    （今天没有「前缀没被 `STANDALONE` 接走」的形状：`粉圆纹`/`紫圆纹`/`紫圆(枪)`/`紫色圆·枪` 都在上面那张表里）。
PREFIX_FALLBACK = [
    ('拳头', 'Melee'), ('粉圆', 'Melee'),
    ('枪', 'Ranged'), ('紫圆', 'Ranged'),
]

# ---------------------------------------------------------------------------
# 形状名里**带着**那个数字的（`【图标:圆内有数字1】` / `【图标:绿圈3】`）——
#   那个数字是**烘在图里**的（`questPointsN.png` / `SpiritStone_N.png`），**卡面文字里没有它**
#   ⇒ 还原时**必须连数字一起写回去**，否则这一档就丢了（改前正是这么丢的）。
#
# 🔴 2026-10-12（`C5` · `A1604`）**就地订正**：这里原来是一条 `NUM_RE`，注释写着
#    「= **这一行的能量费用**」—— **标签本身错**（两族人花的是**两种不同的资源**）：
#      · `圆内有数字N` = 暗黑天使的**任务点**（`questPointsN.png`）；
#      · `绿圈N`      = 灵族的**灵魂石**（`Atlas_SpiritStone_1..5`）—— 灵族花的是灵魂石，不是能量。
#    逐张判据（PnP 亲读 + 卡池逐张，本轮做的）见每条自己的 `why`。
# ---------------------------------------------------------------------------
SHAPE_DIGIT = [
    # `1 Quest Point` / `2 Quest Points` —— 与卡池逐字同形（单复数也照卡池）
    (re.compile(r'^圆内有数字(\d)$'),
     lambda n: f'{n} Quest Point' if n == '1' else f'{n} Quest Points',
     '暗黑天使的**任务点**徽记（数字烘在图里）。判据：PnP 亲读 '
     '`Dark Angels/1督军/Warpforge_01_Asmodai.png` = `Agenda: Gain 〔齿环徽记「1」〕.`；'
     '卡池 19 张全写 `N Quest Point(s)`（`DA_Asmodai` `Gain 1 Quest Point` · '
     '`DA21 Inner Circle Companion` `Gain Vanguard and 2 Quest Points`）'),
    # `N [Spirit Stone]` —— 卡面只画「绿圈 + 白数字」，**没有冒号**（卡池那 `:` 是卡池自己的写法）
    (re.compile(r'^绿圈(\d)$'),
     lambda n: f'{n} [Spirit Stone]',
     '灵族的**灵魂石**费用（数字烘在图里）。判据：PnP 亲读 '
     '`Aeldari/3部队/Warpforge_03_Shining-Spear.png` = `〔绿圈 + 白「1」〕Gain 〔旋刃〕Shuriken 2 this turn`；'
     '卡池 17 张全写 `N [Spirit Stone]`'),
]

# 形状名里**没有**数字、但卡面文字里**紧跟着**一个数字的资源徽记（数字**不**烘在图里，
# 所以**不能**写进替换串 —— 写进去会变成 `Quest Point3` 这种重复）。
#   判据：与 `齿轮` 那条同一条（`gain〔徽记〕3` = `gain 3 Quest Points`）。
DIGIT_AFTER = {
    # ⚠️ `螺旋` 是个**歧义形状名**，别整条收口径：
    #    · 13 处在**别处**确实是金螺旋 `Stun`（后接词，走「后接英文词」那一支，与小表无关）；
    #    · 只有 `Repulsor` 这一处是**任务点徽记**（`Dark Angels/3部队/Warpforge_43_Repulsor.png` 亲读：
    #      紫色齿环 + 白「3」= `questPoints3`；卡池 `DA43` = `gain 3 Quest Points`）。
    '螺旋': ('Quest Point', '暗黑天使任务点徽记**当且仅当**后面紧跟数字时（本处之后是 `3`，数字在卡面文字里）。'
                            '判据：PnP 亲读 `Dark Angels/3部队/Warpforge_43_Repulsor.png`（紫色齿环 + 白「3」）；'
                            '卡池 `DA43 Repulsor` `gain 3 Quest Points`。'
                            '⚠️ 同名的金螺旋 `Stun`（13 处）不在此列 —— 它们后面跟的是**词**'),
}

# ---------------------------------------------------------------------------
# ⚠️ **2026-10-12 改前的老口径**：只留给 `--legacy-smalltable` 现场演示「自检能红」用。
#    ⛔ 正常流程一条都不走（`resolve()` 里只在那个开关打开时才看它）。
# ---------------------------------------------------------------------------
LEGACY_SMALLTABLE = '--legacy-smalltable' in sys.argv
# 本轮**新加进 `STANDALONE`** 的键（演示旧口径时它们要**不认**，否则那 3 处不会红）
NEW_IN_STANDALONE = {'暗色圆徽'}

LEGACY_STANDALONE = {
    # 改前：太阳被记成「能量」（真值 = 信仰）
    '太阳': ('Energy', '⚠️ 演示用：`A1604` 改前的错值（太阳被记成能量）'),
}
# 改前：`圆内有数字N` / `绿圈N` 一律还原成**裸数字**，依据写「这一行的能量费用」
LEGACY_NUM_RE = re.compile(r'(?:圆内有数字|绿圈)(\d)')
LEGACY_NUM_WHY = '⚠️ 演示用：`A1604` 改前把数字圈还原成裸数字（语义丢了）'


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


def _resolve_legacy(shape):
    """`A1604` **改前**的小表口径 —— ⛔ 只在 `--legacy-smalltable` 时走（见文件头）。

    与现口径的三处差别：太阳 = `Energy` · 数字圈 = 裸数字 · 本轮新加的那几条**不认**。
    """
    if shape in LEGACY_STANDALONE:
        return LEGACY_STANDALONE[shape]
    for pre, kw in PREFIX_FALLBACK:
        if shape.startswith(pre):
            return kw, f'形状名以「{pre}」开头（数值图标）'
    m = LEGACY_NUM_RE.search(shape)
    if m:
        return m.group(1), LEGACY_NUM_WHY
    if shape in STANDALONE and shape not in NEW_IN_STANDALONE:
        return STANDALONE[shape][0], STANDALONE[shape][1]
    return None


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
    # ② 单独出现的数值 / 资源图标 —— 按形状查小表
    if LEGACY_SMALLTABLE:
        r = _resolve_legacy(shape)                      # ⚠️ 演示开关，见文件头
        if r:
            return r
    else:
        if shape in STANDALONE:
            return STANDALONE[shape][0], STANDALONE[shape][1]
        for pre, kw in PREFIX_FALLBACK:
            if shape.startswith(pre):
                return kw, f'形状名以「{pre}」开头（数值图标）'
        # 形状名里**带着**数字的那两族（数字烘在图里 ⇒ 必须连数字一起写回去）
        for rx, fn, why in SHAPE_DIGIT:
            mm = rx.match(shape)
            if mm:
                return fn(mm.group(1)), why
        # 形状名里没有数字、但**卡面文字里紧跟着**数字的资源徽记（数字在文字里 ⇒ 只写词）
        da = DIGIT_AFTER.get(shape)
        if da and after[:1].isdigit():
            return da[0], da[1]
    # ③ 再看**前面**有没有词（极少数写法把图标放在词后）
    for w in VOCAB:
        if before[-len(w):].lower() == w.lower():
            return w, '图标前紧跟英文词'
    return None, None


# ---------------------------------------------------------------------------
# **自检四条** —— 每条都要能红（`--legacy-smalltable` 现场演示，见文件头）。
# ⚠️ 判据**不是**「跟本文件的常量比」：每条的外部出处写在各形状自己的 `why` 里
#    （PnP 成品卡图亲读 / 卡池逐张 / `资料/关键词图标/` 两份清单）。下面这张表是把
#    **那份出处锁住**，防它再被改回去（同族做法见 `gen_cardface_fixes.py` 的裸键判据）。
# ---------------------------------------------------------------------------
SMALLTABLE_EXPECT = [
    # (形状的**正则**（精确到那一族，别用 `startswith` —— `绿圈人形` 那种是另一枚图标）, 人读名字,
    #  还原出来的词必须**含**这个子串, 判据出处)
    (r'^太阳$', '太阳', 'Faith',
     'PnP 亲读 `Sorotitas/3部队/Warpforge_42_Exorcist.png` = `traits/faith.png` 同图；'
     '`资料/关键词图标/_图标清单.md:31`；卡池 `[faith]`/`[Faith]`/`☀`'),
    (r'^圆内有数字\d$', '圆内有数字N', 'Quest Point',
     'PnP 亲读 `Dark Angels/1督军/Warpforge_01_Asmodai.png`；卡池 19 张全写 `N Quest Point(s)`'),
    (r'^绿圈\d$', '绿圈N', 'Spirit Stone',
     'PnP 亲读 `Aeldari/3部队/Warpforge_03_Shining-Spear.png`；卡池 17 张全写 `N [Spirit Stone]`'),
    (r'^暗色圆徽$', '暗色圆徽', 'Quest Point',
     'PnP 亲读 `Dark Angels/3部队/Warpforge_08_Apothecary.png`；卡池 `DA8`/`DA15` 写 `[Quest Point]`'),
    (r'^螺旋$', '螺旋', 'Quest Point',
     'PnP 亲读 `Dark Angels/3部队/Warpforge_43_Repulsor.png`（紫色齿环 + 白「3」）；卡池 `DA43`'),
]


def selfcheck(small_hits, unresolved, n_unresolved):
    """跑那四条，返回 (是否全过, 报告行)。`small_hits` = 形状 → Counter(走小表还原出的词)。"""
    lines, ok_all = [], True

    def add(title, good, detail):
        nonlocal ok_all
        ok_all = ok_all and good
        lines.append(f'  {"✅" if good else "🔴"} {title}'
                     + ('' if good else f'\n       → {detail}'))

    # ① 不许把图标还原成**裸数字**：数字单独出现 = 那一档的语义**整条丢了**
    bare = sorted(f'{sh}={kw}(×{n})'
                  for sh, c in small_hits.items() for kw, n in c.items()
                  if re.fullmatch(r'\d+', kw))
    add('① 小表里没有**裸数字**（数字单独出现 = 语义丢了）', not bare, ' / '.join(bare))

    # ② 不许有「没还原出来」
    add('② 没还原的图标 = 0 处（不许静默漏）', n_unresolved == 0,
        f'{n_unresolved} 处：' + ' / '.join(f'【{s}】×{n}' for s, n in unresolved.most_common()))

    # ③④ 每一族还原出来的词必须**含**那个词（判据见 SMALLTABLE_EXPECT 的最后一列）
    for rx, label, need, why in SMALLTABLE_EXPECT:
        hit = {kw: n for sh, c in small_hits.items() if re.match(rx, sh) for kw, n in c.items()}
        bad = {kw: n for kw, n in hit.items() if need not in kw}
        add(f'③/④ `{label}` 族还原出来的词必须含 `{need}`（{sum(hit.values())} 处）',
            hit and not bad,
            f'实际 {bad or "这一族今天一处都没走到小表"}；判据：{why}')

    return ok_all, lines


def main():
    global VOCAB
    VOCAB = load_vocab()
    txt = io.open(SRC, encoding='utf-8').read()
    lines = txt.split('\n')

    stats = collections.Counter()
    shape2kw = collections.defaultdict(collections.Counter)
    # 🆕 2026-10-12（`C5` · `A1604`）：**只记走小表那一支**的结果 —— 自检要盯的就是它
    #    （「后接英文词」那一支的答案印在卡面上，不可疑，几千处也不该进这张表）。
    small_hits = collections.defaultdict(collections.Counter)
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
                    small_hits[shape][kw] += 1
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

    # ---- 自检（四条）----
    p()
    p(f'── 自检（{"⚠️ `--legacy-smalltable` 演示模式：这是 2026-10-12 改前的错小表" if LEGACY_SMALLTABLE else "小表口径"}）──')
    ok, report = selfcheck(small_hits, unresolved, stats['没还原'])
    for ln in report:
        p(ln)
    p(f'  ⇒ {"✅ 四条全过" if ok else "🔴 有红"}')

    # 🔴 **退出码先算、再决定落不落盘**（同 `gen_icon_plan.py` 的 `A1626` 口径）——
    #    「非 0」必须**等于**「没写文件」，⛔ 别让「自检红了」和「产物已经写出去」同时成立。
    if not ok:
        p('')
        p('🔴 自检有红 ⇒ **本脚本不落盘**（`--write` 被忽略，`_还原效果文字.md` 一个字节都没动）。')
        p('   ⇒ 判据在每个形状自己的 `why` 里（PnP 成品卡图 / 卡池逐张 / `资料/关键词图标/`）。')

    if '--write' in sys.argv and '--check' not in sys.argv and ok:
        out = os.path.join(OUTDIR, '_还原效果文字.md')
        with io.open(out, 'w', encoding='utf-8') as f:
            f.write('# 卡面效果文字（占位符已还原）\n\n')
            f.write('> 由 `Unity/工具/restore_card_icons.py` 从 `_合并总表.md` 生成，**别手改**。\n')
            f.write('> 还原依据：卡面上**关键词图标旁边就印着英文词**（2026-09-13 看真卡确认），\n')
            f.write('> 所以答案在文本里、不用认图；只有**数值 / 资源图标**（拳头 / 枪 / 信仰 / 任务点 / 灵魂石）走小表'
                    '（判据 = PnP 成品卡图亲读 + 卡池逐张，逐条写在脚本那张小表的 `why` 里）。\n')
            f.write('> 为什么不用模板匹配，见脚本文件头；'
                    '⚠️ **表头就这 4 行、别加** —— `gen_icon_plan.py` 里有 115 处 `_还原效果文字.md:NN` 引用，'
                    '**加一行就把它们全部指歪一行**。\n\n')
            f.write('| 文件名 | 卡名 | 效果文字（还原后） | 图标数 |\n|---|---|---|---|\n')
            for fn, name, eff, n in out_rows:
                f.write(f'| {fn} | {name} | {eff.replace("|", "/")} | {n} |\n')
        p(f'\n已写入 {out}')

    return 0 if ok else 1


if __name__ == '__main__':
    sys.exit(main())
