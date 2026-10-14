# 🔴 **2026-10-14（第二十九会话）入仓副本** —— 原件一直住在 `d:/4/_tmp_view/`（**被 `.gitignore` 的
#   `/_tmp_*/` 挡住**）⇒ **下个会话 / 别人 clone 之后复算不了**，而 `资料/` 里有 **9 处**点名它。
#   本副本 = 原件【逐字节】拷贝（含 2026-10-14 `A1915` 加的那道 `hasActiveAbility` 闸），只多这一段头注。
#   🆕 2026-10-14 第二笔（账 `A1947`）：**替代主动能力（`alternativeAbility`）现在会读出并出声单列**
#      —— 判据与「为什么不替原版挑」见正文里那段长注释；本快照计数 = 0（零读数影响）。
#   判据与读数 → `资料/普查产出_第二十九会话/A1915_对账仪器加闸.md`（**加闸那一笔**）。
#   🔴 **2026-10-14（第三十会话 · 仪器块 `E1`/`E2` ＋ 账 `A1977`）：本副本已不再与 `_tmp_view` 那份逐字节相同**
#      —— 这一轮动了四处，逐条都在就地注释里写了判据：
#        ① 账 **`A1977`**：`SCAN` 不再指 `_tmp_view` 下那份（**那是 `Talent:` 修复【之前】13:20 的**，
#           谁照默认跑一遍就拿回**过期口径**）—— 改指**仓内的 `check` 基线**（`工具/ruleprobe/基线/
#           out_baseline.txt`，与 `scan` 同口、**永不过期**），并且**每次运行都把输入的
#           mtime / 字节数 / md5 / 行数打出来**（让「拿旧读数当现值」这件事**看得见**，而不是靠人记）。
#           ⚠️ 2026-10-14 同日**主对话裁定**（铁律 5 留痕）：这一笔原来先写成「指仓内 + 带日期那份」
#              （`资料/普查产出_第三十会话/our_scan_1014.txt`），裁定后**默认改成吃基线**；
#              那份带日期的凭据**保留**（它才是「那一轮读数」的可复算坐标，⛔ 别删）。
#              复算命令写在 `SCAN` 那段注释里。
#           同笔把 `OUT` 也改成**仓内新件**：⛔ `A1759_逐条对账.tsv` / `A1937_对账_改后.tsv` 是历史件
#           （`A1876`/`A1877` 按它们取数），**再跑一遍不许盖上去**。
#        ② 仪器块 **`E1`**：补 `{AM:}` / `{TAIL:}` 两处块解析（原来**只解 `{SRC:}` 一处**）⇒ 区间值可比
#           上界、尾句那半句读得到 —— 见 `parse_ops_bundle` 那一段的长注释。
#        ③ 仪器块 **`E2`**：把 `工具/ruleprobe.sh layers <out.jsonl>`（`AuraSpecs` / `keywords` 层）join 进来，
#           新增「我们面·光环」一列，并**重判** `WhileInPlay` / `WhileInPlayVariable` / `bt=3 addTrait` 三族
#           —— 见 `aura_hit()` / `e2_verdict()` 那一段的长注释（含「只翻机器判据能立住的，其余如实留红」）。
#        ④ 四个命令行开关 `--scan` / `--out` / `--layers` / `--no-layers`（**一节一验**要不覆盖既有 TSV 地跑）。
#           ⚠️ `E3`（重跑 `scan` 重生成基线）**不归本件** —— 那会重写 `check` 基线，属主对话。
# -*- coding: utf-8 -*-
"""A1759/A1772/A1823 —— 原版 cardAbilities[].abilityLogic[] 逐条对账我们的规则引擎。

对账单位 = (卡 id, ability 序号, abilityLogic 序号)
  原版面：d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json
  我们面：工具/ruleprobe.sh scan            → `SCAN`（`EffectText.Parse(desc)` 的产物，与 `scan`/`check` 基线同口）
        ＋工具/ruleprobe.sh layers <out.jsonl> → `LAYERS`（`CardDef.AuraSpecs` / `keywords` 层，卡级 —— 见 `E2`）
  用法：python -I -X utf8 d:/4/Unity/工具/_a1759_reconcile.py [--scan P] [--out P] [--layers P] [--no-layers]
"""
import json, io, re, os, sys, time, hashlib, collections

TRUTH  = r'd:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json'
POOL   = r'd:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json'
# 🔴 账 `A1977`（2026-10-14 第三十会话 · 仪器块 `E0`）：`SCAN` 原来写死 `d:/4/_tmp_view/our_scan.txt`
#   （**在 `.gitignore` 的暂存目录里**，而且那一份是 `Talent:` 修复【之前】13:20 的）
#   ⇒ 谁照默认跑一遍就拿回**过期口径**（已实际发生：同一件上 1782 / 1878 / 1821 三套数字并存）。
#   🔴 **同日由主对话裁定**：默认改为**直接吃 `check` 的基线** `工具/ruleprobe/基线/out_baseline.txt`
#      —— 它是 `scan` 的同口产物、**永不过期**（解析器一改、重生成基线就跟着走），
#      而「跟没跟上」由 `_prov()` **每次运行打出来的 mtime/md5/行数**保管（⛔ 那个打印别删）。
#   ✅ **本轮读数的凭据**留在 `资料/普查产出_第三十会话/our_scan_1014.txt`（**带日期**，
#      与当时的基线 md5 逐字节相同 = `6c0e41d71599bd7a42f078a50e90f614`）—— 复算那一轮就指它。
#   ⚠️ `--scan P` 可覆盖（复算别的轮次 / 跑实验用）。
#   📋 **复算（全只读，两个 `ruleprobe` 口都不占 Unity）**：
#       ① 我们面·解析层：`bash d:/4/Unity/工具/ruleprobe.sh scan d:/4/Unity/工具/ruleprobe/基线/out_baseline.txt`
#          ⚠️ **这一步会重写 `check` 的基线** ⇒ 属**主对话**（= 切块表里的 `E3`）⇒ 平时**别跑**；
#             只想拿一份带日期的凭据时，`scan` 到 `资料/普查产出_第N会话/our_scan_<日期>.txt` 即可。
#       ② 我们面·光环/关键词层：`bash d:/4/Unity/工具/ruleprobe.sh layers <out.jsonl>`
#       ③ `python -I -X utf8 d:/4/Unity/工具/_a1759_reconcile.py`
#   实测基线（2026-10-14 18:06）：`6c0e41d71599bd7a42f078a50e90f614` · 493,866 B · 1120 行 ·
#   含 `{AM:}` 32 op / `{TAIL:}` 88 op / `SRC:Talent` 86 处（= `Talent:` 修复后的口径）。
SCAN   = r'd:/4/Unity/工具/ruleprobe/基线/out_baseline.txt'
LAYERS = r'd:/4/Unity/资料/普查产出_第三十会话/our_layers_1014.jsonl'
DICT   = r'd:/4/Unity/资料/普查产出_第二十七会话/abilityLogic_枚举词典.tsv'
# 🔴 账 `A1977` 同笔：`OUT` **改前** = `…/普查产出_第二十八会话/A1759_逐条对账.tsv`（历史件）——
#   照默认跑一遍就会把它**静默盖掉**，而 `A1876`/`A1877` 是按它取数的。⇒ 换成本轮新件。
OUT    = r'd:/4/Unity/资料/普查产出_第三十会话/A1977_对账_E1E2_1014.tsv'


def _opt(name):
    """`--scan P` / `--out P` / `--layers P` / `--no-layers`（🆕 2026-10-14 `E1`/`E2`）。
    ⚠️ 解析失败**停下并出声**（本仓红线：不许静默失败）—— ⛔ 别退回默认值当没事。"""
    if name in sys.argv:
        i = sys.argv.index(name)
        if i + 1 >= len(sys.argv): raise SystemExit('!! %s 后面缺路径' % name)
        return sys.argv[i + 1]
    return None


_scan, _out, _lay = _opt('--scan'), _opt('--out'), _opt('--layers')
NO_LAYERS = '--no-layers' in sys.argv
if _scan: SCAN = _scan
if _out: OUT = _out
if _lay: LAYERS = _lay


def _prov(path):
    """输入文件的**现读**凭据（mtime / 字节 / md5 / 行数）—— 见 `A1977`。
    ⚠️ 为什么要打：这条链上**三套数字并存过**（1782 / 1878 / 1821），根因就是「谁也没记自己吃的是哪一份」。"""
    b = io.open(path, 'rb').read()
    return '%s  %s  %d B  md5=%s  %d 行' % (
        path, time.strftime('%Y-%m-%d %H:%M', time.localtime(os.path.getmtime(path))),
        len(b), hashlib.md5(b).hexdigest(), b.count(b'\n'))


# 🔴 账 `A1937`（2026-10-14）：`activeAbility`（→ `ACT` 行）**不是 `AbilityTrigger` 的取值** ——
#   `ActiveAbility`（`d:/2/tools/il2cpp_out/dump.cs:23750`）15 个字段里**没有 `trigger`**
#   （真值 JSON 现读：`activeAbility` 的键 = minHeroLevel/manaCost/abilitySprite/targeted/playAnim/
#    needsLoading/resolutionChoosingCount/psychicCost/plasmaCost/loadingAbilityLogic/targetCriteria/
#    targetsAvailableCounter/activeAbilityLogic/cardTargetScoringCriteria/nonTargetScoringCriteria），
#   而 `RawCardScript__ResolveActiveAbility.c` 是**把那份列表整条下发、全程不查任何 trigger**
#   ⇒ 给 `ACT` 行贴一个真枚举值（旧代码写 `46`）是**伪造了一条不存在的触发关系**。
#   本常量**不是枚举值**，只用于在备注里标明「这一行来自 `activeAbility`」。
#   与同源的汇总脚本 `_tmp_view/a1759_report2.py:22` 用的标签一致（`A1759_逐条对账.md:86` 的
#   `Activated(31)` 就是它算出来的）—— 两台脚本从此不再一个说 `Talent`、一个说 `Activated`。
ACT_TRIG = 'Activated'

rows = [l.rstrip('\n').split('\t') for l in io.open(DICT, 'r', encoding='utf-8')]
ANAME, TNAME, BTNAME, TRNAME = {}, {}, {}, {}
for r in rows[1:]:
    if r[1] == 'chosenAbility' and r[0].startswith('AbilityLogic@'):
        ANAME[int(r[2])] = r[3]
    if r[1] == 'trigger' and r[0] == 'CardAbility@cardAbilities':
        TNAME[int(r[2])] = r[3]
    if r[1] == 'buffType':
        BTNAME[int(r[2])] = r[3]
    if r[1] == 'trait' and r[0] == 'CardEffect@cardAbilities.abilityLogic.buffEffect':
        TRNAME[int(r[2])] = r[3]

truth = json.load(io.open(TRUTH, 'r', encoding='utf-8'))
pool = json.load(io.open(POOL, 'r', encoding='utf-8'))['cards']
namecnt = collections.Counter(c['name'] for c in pool)
truthnamecnt = collections.Counter(c.get('cardName') or '' for c in truth)

def nz(d):
    return {k: v for k, v in (d or {}).items() if v not in (0, '', None, [], {})}

def deep_nonzero(v):
    """递归判「这一份字段里到底有没有真值」—— `nz()` 是**浅**的（只看顶层那一格，
    `{'m_FileID': 0, 'm_PathID': 0}` 这种空引用它当「有值」）⇒ 账 `A1947` 要判
    `alternativeAbility` 是不是**整份空模板**，浅判会给出假阳性（实测：1195/1195 全会被判成「有内容」）。"""
    if isinstance(v, bool): return v
    if isinstance(v, (int, float)): return v != 0
    if isinstance(v, str): return v != ''
    if isinstance(v, list): return any(deep_nonzero(x) for x in v)
    if isinstance(v, dict): return any(deep_nonzero(x) for x in v.values())
    return v is not None

OFAM = {
    0: 'draw', 3: 'vfxonly', 10: 'damage', 12: 'damage', 13: 'damage', 15: 'damage',
    20: 'heal', 30: 'buff_other', 31: 'buff_other', 40: 'buff_self', 50: 'buff_hand',
    60: 'mana', 70: 'summon', 75: 'summon', 77: 'summon',
    100: 'create', 101: 'create', 110: 'destroy', 130: 'stun',
    140: 'recall', 150: 'recall', 180: 'steal', 185: 'steal',
    231: 'forceattack', 235: 'scripted_attack', 236: 'scripted_attack',
    237: 'scripted_attack', 238: 'scripted_attack',
    250: 'autoplay', 260: 'trigger_x', 270: 'create', 300: 'recall', 301: 'recall',
    320: 'follow', 325: 'follow', 340: 'remove_ench', 360: 'choose', 380: 'setref',
    410: 'trigger_x', 411: 'trigger_x', 440: 'trigger_x', 450: 'trigger_x',
    460: 'trigger_x', 461: 'trigger_x', 462: 'trigger_x', 465: 'trigger_x',
    466: 'trigger_x', 467: 'trigger_x', 468: 'trigger_x',
    420: 'recall', 430: 'reanimate', 470: 'uionly', 475: 'resetduty',
}
EQ = {
    'draw':        {'draw'},
    'vfxonly':     {'vfxonly'},
    'damage':      {'damage'},
    'heal':        {'heal'},
    'buff_other':  {'buff_other', 'debuff_other', 'cost', 'jam'},
    'buff_self':   {'buff_self', 'debuff_other', 'cost', 'jam'},
    'buff_hand':   {'buff_other', 'buff_self', 'cost'},
    'mana':        {'mana'},
    'summon':      {'summon'},
    'create':      {'create'},
    'destroy':     {'destroy'},
    'stun':        {'stun'},
    'recall':      {'recall'},
    'steal':       {'steal'},
    'forceattack': {'forceattack'},
    'scripted_attack': {'forceattack', 'damage', 'attack'},
    'autoplay':    {'summon', 'create', 'choose', 'recall'},
    'trigger_x':   {'trigger_x'},
    'follow':      {'damage', 'heal', 'buff_other', 'buff_self', 'debuff_other', 'destroy',
                    'stun', 'draw', 'summon', 'create', 'mana', 'recall', 'buff_hand', 'cost', 'choose'},
    'remove_ench': {'envelope', 'noop'},
    'choose':      {'choose', 'destroy'},
    'setref':      {'envelope', 'choose'},
    'reanimate':   {'reanimate'},
    'uionly':      {'vfxonly', 'envelope', 'noop'},
    'resetduty':   {'resetduty'},
    'other':       {'other'},
}
PRIM = {'damage': 'damage', 'heal': 'heal', 'draw': 'draw', 'buff_other': 'buff_other',
        'buff_self': 'buff_self', 'buff_hand': 'buff_other', 'mana': 'mana', 'summon': 'summon',
        'create': 'create', 'destroy': 'destroy', 'stun': 'stun', 'recall': 'recall',
        'steal': 'steal', 'forceattack': 'forceattack', 'trigger_x': 'trigger_x',
        'choose': 'choose', 'reanimate': 'reanimate', 'resetduty': 'resetduty',
        'scripted_attack': 'forceattack', 'autoplay': 'summon'}
VFAM = {
    'deal': 'damage', 'eachunitdeal': 'damage', 'heal': 'heal',
    'draw': 'draw', 'drawtype': 'draw', 'drawref': 'draw',
    'give': 'buff_other', 'lose': 'debuff_other', 'gain': 'buff_self',
    'stun': 'stun', 'destroy': 'destroy', 'deploy': 'summon', 'create': 'create',
    'return': 'recall', 'reanimate': 'reanimate', 'takecontrol': 'steal',
    'forceattack': 'forceattack', 'blind': 'jam',
    'refill': 'mana', 'gainenergy': 'mana', 'gainquest': 'mana', 'gainfaith': 'mana',
    'gainspirit': 'mana', 'spendspirit': 'mana',
    'triggerability': 'trigger_x', 'extratrigger': 'trigger_x',
    'reloadduty': 'resetduty',
    'chooseone': 'choose', 'choosecard': 'choose', 'chooseeffect': 'choose',
    'lowercost': 'cost', 'costmore': 'cost', 'costwhen': 'cost', 'costifcontrol': 'cost',
    'paidmod': 'cost', 'double': 'buff_self', 'become': 'transform', 'sethealth': 'sethealth',
    'whenclause': 'envelope', 'atturn': 'envelope', 'persist': 'envelope',
    'repeat': 'envelope', 'noeffect': 'noop', 'ferocitystay': 'envelope',
}
T2H = {
    'Talent': 'talent', 'Strike': 'strike', 'Slay': 'slay', 'Pray': 'pray',
    'Artifice': 'artifice', 'Mob': 'mob', 'Regiment': 'regiment', 'Cruelty': 'cruelty',
    'Ambush': 'ambush', 'Uprising': 'uprising', 'Teleport': 'teleport',
    'Stimulation': 'stimulation', 'Codex': 'codex', 'Ecstasy': 'ecstasy',
    'Backlash': 'backlash', 'Penitence': 'penitence', 'Agenda': 'agenda',
    'Ferocity': 'ferocity', 'Duty': 'duty', 'ThisCardPlayed': 'rally',
    'TriggeredCodex': 'codex', 'TriggeredPray': 'pray',
}

# ------------------------------------------------ 原版取值
def orig_value(lg):
    d = nz(lg.get('damageCriteria')); h = nz(lg.get('healCriteria'))
    b = nz(lg.get('buffEffect')); m = nz(lg.get('manaBuff'))
    s = nz(lg.get('summonCriteria')); tc = nz(lg.get('targetCriteria'))
    mult = nz(lg.get('multiplier')); hb = nz(lg.get('handBuff'))
    parts, meta, dyn = [], {}, False
    if d:
        meta.setdefault('sec', []).append('damage')
        parts.append('dmg%s-%s%s' % (d.get('minDamage', 0), d.get('maxDamage', 0),
                                     ('/opt%d' % d['valueOptions']) if d.get('valueOptions') else ''))
        if d.get('valueOptions'): dyn = True
        elif d.get('minDamage', 0) == d.get('maxDamage', 0): meta['dmg'] = d.get('minDamage', 0)
        # 🔴 `E1`（2026-10-14）：**原版是区间**（`min != max`）时，原来这里**什么都不记** ⇒ `compare()`
        #   拿不到期望值、只能返回「一致」⇒ 区间那批**从未被比过**（`资料/普查产出_第二十八会话/
        #   A1759_逐条对账.md` §查不清最后一行就是点这一条）。现在把区间记进 `dmg_span`，
        #   与我们的 `{AM:}`（上界）配成对。
        else: meta['dmg_span'] = (d.get('minDamage', 0), d.get('maxDamage', 0))
    if h:
        meta.setdefault('sec', []).append('heal')
        parts.append('heal%s-%s%s' % (h.get('minValue', 0), h.get('maxValue', 0),
                                      ('/opt%d' % h['valueOptions']) if h.get('valueOptions') else ''))
        if h.get('valueOptions'): dyn = True
        elif h.get('minValue', 0) == h.get('maxValue', 0): meta['heal'] = h.get('minValue', 0)
        else: meta['heal_span'] = (h.get('minValue', 0), h.get('maxValue', 0))   # 同上（`E1`）
    if b:
        bt = b.get('buffType', 0)
        meta['bt'] = bt
        ch = []; nums = []
        for k, a in (('melee', 'meleeAttackChange'), ('ranged', 'rangeAttackChange'),
                     ('armour', 'armourChange'), ('health', 'healthChange'), ('cost', 'costChange')):
            if b.get(a):
                ch.append('%s%+d' % (k, b[a]))
                if not b.get(a.replace('Change', 'ValueOptions')): nums.append((k, b[a]))
                else: dyn = True
        meta['nums'] = [t for t in nums if t[0] in ('melee', 'ranged', 'armour', 'health')]
        meta.setdefault('sec', []).append('buff_other')
        if bt in (3, 4, 90):
            meta['trait'] = TRNAME.get(b.get('trait', 0), 'trait%s' % b.get('trait', 0))
            ch.append('%s %s' % (BTNAME.get(bt, bt), meta['trait']))
        elif bt in (12, 15, 16):
            ch.append(BTNAME.get(bt, 'bt%d' % bt))
        elif bt not in (0, 1):
            ch.append('bt=%s' % BTNAME.get(bt, bt))
        parts.append('buff{%s}' % (','.join(ch) or ('bt=%s' % BTNAME.get(bt, bt))))
        if bt in (3, 4, 90, 2):
            dyn = True
    if m:
        meta.setdefault('sec', []).append('mana')
        parts.append('mana=%s{%s}' % (m.get('manaAmount', 0),
                                      ','.join(sorted(k for k in m if k != 'manaAmount'))))
        meta['mana'] = m.get('manaAmount', 0)
    if s:
        meta.setdefault('sec', []).append('summon')
        parts.append('summ:n=%d%s' % (len(s.get('unitsToSummon') or []),
                                      ',enemy' if s.get('isForEnemy') else ''))
    if hb:
        ce = {k: v for k, v in nz(hb.get('cardEffect')).items()
              if k not in ('enchantingCard', 'enchantment')}
        if ce or hb.get('numberOfUses'):
            parts.append('handBuff:n=%s{%s}' % (hb.get('numberOfUses'), ','.join(sorted(ce.keys())[:4])))
    if tc:
        keys = ['targetsAffected', 'friendlyUnits', 'enemyUnits', 'includeHero', 'includeMinions',
                'includeRemnants', 'includeSelf', 'maxTargets', 'keywordFilter', 'traitsFilter',
                'conditionsFilter', 'chosenArmy', 'spellType', 'filterSpecificCards']
        parts.append('tgt{%s}' % ','.join('%s=%s' % (k, tc[k]) for k in keys if k in tc))
    if mult:
        parts.append('x{ta=%s,mt=%s}' % (mult.get('targetsAffected'), mult.get('maxTargets')))
    if lg.get('furtherMultiplier'): parts.append('fm=%s' % lg['furtherMultiplier'])
    if lg.get('chanceOfHappening'): parts.append('ch=%s' % lg['chanceOfHappening'])
    if lg.get('playersAffected'): parts.append('pa=%s' % lg['playersAffected'])
    return ' '.join(parts) or '-', meta, dyn

# ------------------------------------------------ 我们面
def parse_ops_bundle(s):
    ops, depth, cur = [], 0, ''
    for ch in s:
        if ch == '{': depth += 1
        elif ch == '}': depth -= 1
        if ch == ';' and depth == 0:
            ops.append(cur); cur = ''
        else: cur += ch
    if cur.strip(): ops.append(cur)
    return [o for o in ops if o]

ENV_VERB_SET = {'whenclause', 'persist', 'atturn', 'repeat', 'noeffect', 'ferocitystay',
                'become', 'extratrigger'}
ENV_FAM = [
    (r'quest point|spirit stone|\bfaith\b|\bplasma\b|\bpsychic\b|\bmana\b|☀|\brefill\b|\benergy\b', 'mana'),
    (r'\b(deal|damage)\b', 'damage'), (r'\bheal\b', 'heal'), (r'\bdeploy\b', 'summon'),
    (r'\bcreate\b', 'create'), (r'\bdraw\b', 'draw'), (r'\breanimate\b', 'reanimate'),
    (r'\bgive\b', 'buff_other'), (r'\b(gain|gains)\b', 'buff_self'), (r'\bdestroy\b', 'destroy'),
    (r'\bstun\b', 'stun'), (r'\b(return|shuffle)\b', 'recall'), (r'\bchoose\b', 'choose'),
    (r'\bblind\b', 'jam'), (r'\battack', 'damage'), (r'\bcost\b', 'cost'),
]
def env_fam(payload):
    t = (payload or '').lower()
    for rx, f in ENV_FAM:
        if re.search(rx, t): return f
    return 'envelope'

our = {}
print('--- 输入现读（账 `A1977`：三套数字并存过，根因就是谁也不记自己吃的是哪一份）---')
print('   我们面 scan  :', _prov(SCAN))
for line in io.open(SCAN, 'r', encoding='utf-8'):
    line = line.rstrip('\n')
    if not line: continue
    p = line.split('|', 4)
    if len(p) < 5: continue
    cid, ops_s = p[0], p[4]
    ops = []
    for raw in parse_ops_bundle(ops_s):
        f = raw.split('/')
        while len(f) < 12: f.append('')
        tb = re.search(r'\{T:([^}]*)\}', raw)
        sb = re.search(r'\{SRC:([^}]*)\}', raw)
        # 🔴 2026-10-14（第三十会话 · 仪器块 `E1` · 账 `A1977`）：**原来这里只解 `{SRC:}` 一处块**。
        #   `Program.TargetBlock`（`工具/ruleprobe/Program.cs:632-634`）在 `/{T:…}` 之后、`/{SRC:…}` 之前
        #   还会追加**两个可选块**（只在非默认值时打），两侧一个字都没读到：
        #     · `/{AM:n}`   ← `EffectOp.AmountMax`（`>0` 才有）。原版那批**区间值**（`deals 2-4 damage`）
        #                      在我们这边就存在这里 —— 结算点 = `EffectResolver.cs:1817/2238/2267` 的
        #                      `Rng.Next(Amount, AmountMax+1)` ⇒ **不看它就只能比下界**，上界没有可比性。
        #     · `/{TAIL:…}` ← `EffectOp.Tail`（非空才有）= 「…and stun it」那半句的**原文**
        #                      （字段 `EffectText.cs:119`；`Finish` 会把它**递归解成第二条 op**，见 `:2958`）
        #                      ⇒ 不看它就不知道这一条 op 身后还挂着半句。
        #   ⚠️ 两块都是**数字 / 卡面原文**，可能自带 `}` 之外的一切 ⇒ 沿用同一条取舍：`[^}]*`，不转义。
        amb = re.search(r'\{AM:(-?\d+)\}', raw)
        tlb = re.search(r'\{TAIL:([^}]*)\}', raw)
        tgt = {}
        if tb:
            for kv in tb.group(1).split('|'):
                if '=' in kv:
                    k, v = kv.split('=', 1); tgt[k] = v
        verb = f[0]
        fam = VFAM.get(verb, 'other')
        if fam == 'envelope': fam = env_fam(f[5])
        ops.append({'verb': verb, 'fam': fam, 'amount': f[4], 'payload': f[5], 'cond': f[8],
                    'tgt': tgt, 'src': (sb.group(1) if sb else ''), 'raw': raw,
                    'am': (int(amb.group(1)) if amb else 0), 'tail': (tlb.group(1) if tlb else ''),
                    'env': verb in ENV_VERB_SET})
    our[cid] = {'name': p[1], 'ops': ops, 'nops': len(ops)}
print('   我们面卡数 = %d · op 总数 = %d · 带 `{AM:}` 的 op = %d · 带 `{TAIL:}` 的 op = %d'
      % (len(our), sum(v['nops'] for v in our.values()),
         sum(1 for v in our.values() for o in v['ops'] if o['am']),
         sum(1 for v in our.values() for o in v['ops'] if o['tail'])))

famseen = collections.Counter()
for _cid, _v in our.items():
    for _o in _v['ops']: famseen[_o['fam']] += 1

PAY = re.compile(r'([+-]?\d+)\s*(attack|ranged attack|ranged|melee|might|fist|strength|weapon'
                 r'|health|armour|armor|cost)\b'
                 r'|(attack|ranged attack|ranged|melee|might|fist|strength|weapon'
                 r'|health|armour|armor|cost)\s*([+-]?\d+)', re.I)
def our_nums(payload):
    out = []
    for m in PAY.finditer(payload or ''):
        if m.group(1) is not None: a, n = m.group(2).lower(), int(m.group(1))
        else: a, n = m.group(3).lower(), int(m.group(4))
        a = {'armor': 'armour', 'attack': 'melee', 'might': 'melee', 'fist': 'melee',
             'strength': 'melee', 'weapon': 'ranged', 'ranged attack': 'ranged'}.get(a, a)
        out.append((a, n))
    return out


# ================================================ `E2`：我们面的【光环 / keywords】层
# 🔴 2026-10-14（第三十会话 · 仪器块 `E2`）—— 把 `工具/ruleprobe.sh layers <out.jsonl>` 的产物 join 进来。
#
# **病灶**（`资料/普查产出_第二十八会话/A1759_逐条对账.md` §口径 与 §查不清；`W16_对账口仪器与旧id残留.md`）：
#   本对账的「我们面」走 `EffectText.Parse(desc)`（= `scan` 的 ops 列），而**光环（`CardDef.AuraSpecs`
#   → `Core/Aura.cs`）与 keywords 驱动的那一层是【另一层】**，结构上不进那个口 ⇒ `WhileInPlay` /
#   `WhileInPlayVariable` 那一族（40 条步骤，🔴30）被整片报成「我们缺」。`W16` 当年做过一次**只读复算**：
#   那 30 条里 **28 条的卡在光环层里就有对照物**（`TAU19` 原版 `addTrait vanguard` ↔ 我们光环 `Vanguard`…）
#   ⇒ 那是**口径缺口（判错了层），不是实现缺口**。本块就是那句「**换一条口重判**」的机器实现。
#   ⚠️ 引擎自己早就知道这两层是分开的：`EffectText.WillRunOps`（`EffectText.cs:1872`）把七个来源
#      （`WhenTriggers`/`TriggerTexts`/`SpiritOps`/`OathOps`/`FaithOps`/`AttackedOps`/`AuraSpecs`）
#      汇成「执行层」，而 `scan` 打的是**主解析器**那一层。
#
# ⛔ **只认四条机器立得住的对账判据**（每条都追得到字段/词典出处；⛔ 不按「看起来像」翻）：
#   `R1` **trait 名**：原版 `bt=3 addTrait <名>` 里那个 `<名>`（词典 = `abilityLogic_枚举词典.tsv` 的
#        `trait` 列）出现在我们光环的 **`Payload`**（= 这条光环「给什么」）里（只比字母、小写，
#        前后加字母护栏）。实测罩住的：`flank` · `vanguard` · `vulnerable` · `regeneration` · `camouflage` ·
#        `flying` …  ⚠️ **只比 `Payload`、⛔ 不比 `Filter`**（`Filter` 是「给谁」）—— 理由见 `aura_hit()` 里那段。
#   `R2` **数值集**：原版 `bt=1 addStats` 的增减集（`meta['nums']`）与**我们光环载荷**里解析出的数值集
#        **相等** —— 解析器**复用 `our_nums()`**（同一个口，⛔ 不另写一份）；先把卡面图标的方括号去掉
#        （`+2 [Melee]` → `+2 Melee`，`attack/might/fist/strength→melee`、`weapon→ranged` 那套归一化
#        也都在 `our_nums` 里）⇒ 与 op 那一侧**逐条同规**。
#   `R3` **改费用**：原版 `bt=2 changeCost` ⇒ 我们光环的 `CostLess` 非 `0`（字段语义 = 花费少 N，
#        出处 `Core/Aura.cs` 的 `AuraSpec.CostLess`）。
#   `R4` **`bt=90 addTraitRemnant`** ⇒ 我们光环的 `RemnantStay == 1`（词典 `trait 1190 = notDestroyRemnant`
#        ↔ `AuraSpec.RemnantStay`「相邻残骸不消失」—— 这是**名字与字段的显式对应**，⛔ 不是按汉字猜的）。
# ⛔ **判不动的一律不翻**：光环载荷是整句引用（`"Slay: …"`）、或规则对不上 ⇒ **如实留红**，
#    并在备注里写清是「有光环但对不上」还是「光环层为空」，把待人工判的部分交出去（本仓红线：不许静默）。
# ⚠️ `--no-layers` 时本块整体跳过（并把「没重判」这件事**打出来**，⛔ 不静默退化）。
layers = {}
if not NO_LAYERS:
    if not os.path.exists(LAYERS):
        raise SystemExit(
            '!! 光环层 dump 不存在：%s\n'
            '   `E2` 的「我们面·光环」列与 `WhileInPlay`/`WhileInPlayVariable`/`bt=3 addTrait` 三族重判\n'
            '   **全都依赖它** ⇒ 停下（⛔ 不静默按「这张卡没有光环」跑 —— 那会把整族的假账留在表里）。\n'
            '   生成：bash d:/4/Unity/工具/ruleprobe.sh layers <out.jsonl>（纯 dotnet、秒级、不占 Unity）\n'
            '   只跑 `E1`：加 `--no-layers`。' % LAYERS)
    for _l in io.open(LAYERS, 'r', encoding='utf-8'):
        _l = _l.strip()
        if not _l: continue
        _d = json.loads(_l)
        layers[_d['id']] = _d
    print('   我们面光环层:', _prov(LAYERS))
    print('   光环层卡数 = %d · 有光环的卡 = %d 张 · 光环条数 = %d'
          % (len(layers), sum(1 for d in layers.values() if d['auras']),
             sum(len(d['auras']) for d in layers.values())))
else:
    print('   ⚠️ `--no-layers`：**没有** join 光环层 ⇒「我们面·光环」列全空、`E2` 那三族**没有重判**。')

AURA_FLIP_SUB = '光环层承载'
AURA_TRIGS = ('WhileInPlay', 'WhileInPlayVariable')


def in_aura_fams(tname, oval):
    """`E2` 要重判的**三族**：① `WhileInPlay` 触发 ② `WhileInPlayVariable` 触发
    ③ 原版 `bt=3 addTrait`（含 `bt=90 addTraitRemnant` —— 两者在 `orig_value` 里走**同一个分支**，
    而 `bt=90` 的取值文本就是 `addTraitRemnant …` ⇒ 用**取值文本** `'addTrait' in oval` 一并罩住）。"""
    return tname in AURA_TRIGS or 'addTrait' in (oval or '')


def _letters(s):
    return re.sub(r'[^a-z]', '', (s or '').lower())


def _unnest(s):
    """去掉卡面图标的方括号 —— 只为让 `our_nums()` 看得懂（`-2 [Ranged]` → `-2  Ranged`）。
    ⚠️ 只用在**光环载荷**这一侧（op 那一侧的载荷在解析时就已经是裸文本了）。"""
    return (s or '').replace('[', ' ').replace(']', ' ')


def _aura_rows(ocid):
    """这张卡的光环，每条摊成 `(原始字典, 可搜文本)`。"""
    d = layers.get(ocid) if ocid else None
    if not d: return None
    out2 = []
    for a in d.get('auras') or []:
        txt = ' '.join(x for x in (a.get('Payload'), a.get('Filter'), a.get('Source'), a.get('Head')) if x)
        out2.append((a, txt))
    return out2


def aura_hit(ocid, meta):
    """在**我们这张卡的光环层**里找这一支的对照物（四条判据见上面那段）。
    返回 `(状态, 证据)`，状态 ∈ `hit`（有对照物）/ `empty`（这张卡光环层为空）/
    `miss`（有光环但对不上，证据 = 载荷摘要）/ `none`（我们侧根本没配到这张卡）。"""
    rows = _aura_rows(ocid)
    if rows is None: return 'none', ''
    if not rows: return 'empty', ''
    trait = meta.get('trait')
    nums = meta.get('nums') or []
    bt = meta.get('bt', 0)
    tw = _letters(trait)
    for a, txt in rows:
        ev = a.get('Source') or a.get('Payload') or ''
        # 🔴 `R1`/`R2` **只比 `Payload`（= 这条光环「给什么」），⛔ 不拿 `Filter`/`Source` 来比** ——
        #   `Filter` 说的是「给谁」（`带 flying` / `带 pack`），把那串并进来 = **把「筛选条件」当成
        #   「授予内容」**：`SW18`(`Filter=带 pack`, `Payload=Invulnerable`) 会被原版 `addTrait pack`
        #   判成命中 —— **静默的假命中**。另外 `Source` 会把载荷**重复一遍**，`R2` 的
        #   「数值集相等」当场变不等（实测 `EC27` 的 `Payload=-2 [Ranged]` 就被
        #   `… Enemy troops have -2 [Ranged]` 数成两份 ⇒ 假不命中）。两条都踩过：见报告。
        pl = a.get('Payload') or ''
        if bt == 90 and a.get('RemnantStay') == '1':                       # R4
            return 'hit', 'bt=90 `addTraitRemnant` ↔ 光环 `RemnantStay=1`（%s）' % ev
        if bt == 2 and (a.get('CostLess') or '0') != '0':                  # R3
            return 'hit', 'bt=2 `changeCost` ↔ 光环 `CostLess=%s`（%s）' % (a.get('CostLess'), ev)
        if tw and len(tw) >= 4 and re.search(r'(?<![a-z])%s(?![a-z])' % re.escape(tw), _letters(pl)):
            # 🔴 `R1` 的**前后字母护栏**（⛔ 别退化成裸 `in`）：裸包含会把
            #   trait `vulnerable` 判中载荷 `Invulnerable`（i-n-**vulnerable**）—— 静默假命中。
            #   `(?<![a-z])…(?![a-z])` 在**去非字母之后**的串上加边界，既能挡住上面那种，
            #   又仍然认得出 camelCase / 带空格的写法（`bloodThirst` ↔ `Blood Thirst`、
            #   `notDestroyRemnant` ↔ …，两边都被压成纯小写字母）。
            return 'hit', '原版 `addTrait %s` ↔ 光环载荷 `%s`（%s）' % (trait, pl, ev)
        if nums and sorted(our_nums(_unnest(pl))) == sorted(nums):         # R2
            return 'hit', '原版数值 %s ↔ 光环载荷 %s（%s）' % (nums, our_nums(_unnest(pl)), ev)
    return 'miss', ' ｜ '.join(((a.get('Source') or a.get('Payload') or '')[:70]) for a, _ in rows)


def e2_verdict(tname, meta, oval, ocid):
    """把「原版这一支 vs 我们」的最终判定算出来。返回 `(新判定 or None, 新子类, 追加备注)`。
    `None` = 本函数不管这一行（不在 `E2` 三族里）⇒ 调用方**照旧**用原来的判定。
    ⚠️ 只有 `hit` 才改判定（`empty`/`miss` **留红**，只把现读到的证据写进备注）。"""
    if not in_aura_fams(tname, oval): return None, '', ''
    st, ev = aura_hit(ocid, meta)
    if st == 'hit':
        return ('⚠️表示不同·语义同', AURA_FLIP_SUB,
                '（`E2` 重判：原版这一支在我们这边走**光环层**（`CardDef.AuraSpecs`），不在 `EffectParse(desc)`'
                '那条口里 ⇒ 本行左边的「我们」栏**不是它的对照物**（对不上或本来就是空的）· 判据 = %s）' % ev)
    if st == 'empty':
        return None, '', '（`E2` 现读：这张卡**光环层为空** ⇒ 这一支在我们两边都没有承载者）'
    if st == 'miss':
        return None, '', '（`E2` 现读：这张卡**有光环**，但四条机器判据都对不上 —— 载荷 = %s ⇒ 留红待人工逐条判）' % ev
    return None, '', ''


def aura_cell(ocid):
    """「我们面 · 光环」那一格（🔴 `E2` 新增的**第 11 列，追加在最末尾**）。
    ⚠️ **为什么追加在末尾**：前面 10 列的下标（`r[3]` 原版类型 / `r[5]` 我们取值 / `r[8]` 子类 /
    `r[9]` 备注）**已经有别的脚本在按位置读**（例如 `工具/_a1759_report.py`）⇒ 插在中间 = 静默改口径。
    三种取值**刻意不同**（⛔ 别把它们混成同一个 `-`，那正是「判不了」与「没有」混为一谈的病）：
      · 我们侧没配到卡 ⇒ `-`
      · 配到了、但那张卡光环层为空 ⇒ `<我们卡id>:（无光环）`
      · 有光环 ⇒ `<我们卡id>: <载荷> ← <来源句> ｜ …`
    ⚠️ 载荷/来源是**卡面原文**（可含 `|`/`;`/`/`），而 TSV 的分隔符是 **TAB** ⇒ 不用转义（本仓既有取舍）。"""
    rows = _aura_rows(ocid)
    if rows is None: return '-'
    if not rows: return '%s:（无光环）' % ocid
    return '%s: %s' % (ocid, ' ｜ '.join(
        '%s ← %s' % ((a.get('Payload') or '（无载荷）'), (a.get('Source') or '（无来源句）'))
        for a, _ in rows))


def our_span(o):
    """我们的 op 的**取值区间** —— 🔴 `E1`（2026-10-14）：**上界在 `{AM:n}` 里**（= `EffectOp.AmountMax`）。
    **改前这里根本没有这个函数、也读不到上界** ⇒ 区间伤害**只比得下界**。
    ⚠️ `AM` 缺省（`0`）＝ 单值（下界 = 上界）—— 与 `Program.TargetBlock` 那条「只在非默认值时打」一致。"""
    try: lo = int(o['amount'] or 0)
    except Exception: lo = None
    hi = o.get('am') or 0
    if hi <= 0: hi = lo
    return (lo, hi)


def span_txt(sp):
    return '%s' % sp[0] if sp[0] == sp[1] else '%s-%s' % (sp[0], sp[1])


def op_side(o):
    """`我们取值` 那一列的渲染口（**唯一一处** —— ⛔ 别在别处再拼一份）。
    🔴 `E1` 补两样：`~上界`（`{AM:}`）与 `+尾句[…]`（`{TAIL:}`）—— 这两样**只影响可读性，不影响判定**
    （判定走 `compare()` 读字段，⛔ 不读这串文本）。为什么补：改前那一列**只打得出下界**、
    也看不出这条 op 身后还挂着一句（`资料/普查产出_第二十八会话/A1759_逐条对账.md` §查不清最后一行）。"""
    s = '%s/%s' % (o['verb'], o['amount'])
    if o.get('am'): s += '~%s' % o['am']
    if o['payload']: s += '|' + o['payload']
    if o.get('tail'): s += ' +尾句[%s]' % o['tail']
    return s


def compare(fam, meta, o):
    """返回 (数值是否完全一致, 不一致说明)"""
    if o['verb'] in ENV_VERB_SET: return True, ''
    pl = (o['payload'] or '').lower()
    if fam == 'damage':
        # 🔴 `E1`：**原版单值 ⇒ `dmg`；原版区间 ⇒ `dmg_span`**（改前区间那一支**一个字段都没有**，
        #   于是 `n is None` 直接返回「一致」—— 那 104 条区间步骤从来没被比过）。
        #   我们的取值同样按**区间**取（`our_span`，上界来自新读的 `{AM:}`）。
        n, sp = meta.get('dmg'), meta.get('dmg_span')
        if n is None and sp is None: return True, ''
        ours = our_span(o)
        want = sp if sp is not None else (n, n)
        ok = (ours == want)
        return ok, ('' if ok else '原版 %s 点伤害，我们 %s' % (span_txt(want), span_txt(ours)))
    if fam == 'heal':
        n, sp = meta.get('heal'), meta.get('heal_span')   # 同上一支（`E1`）
        if n is None and sp is None: return True, ''
        ours = our_span(o)
        want = sp if sp is not None else (n, n)
        ok = (ours == want)
        return ok, ('' if ok else '原版治 %s，我们 %s' % (span_txt(want), span_txt(ours)))
    if fam in ('buff_other', 'buff_self', 'buff_hand'):
        bt = meta.get('bt', 0)
        if bt in (3, 4, 90):
            if bt == 4: return True, ''
            tw = (meta.get('trait') or '').lower()
            base = re.sub(r'[^a-z]', '', tw)
            if base and (base in re.sub(r'[^a-z]', '', pl) or base in o['verb']): return True, ''
            return False, '原版给 %s（addTrait），我们 payload=[%s]' % (meta.get('trait'), o['payload'])
        if bt == 2:
            if 'cost' in pl or o['verb'] in ('lowercost', 'costmore', 'costwhen', 'costifcontrol'):
                return True, ''
            return False, '原版改费用（costChange），我们 payload=[%s]' % o['payload']
        if bt in (8, 12, 15, 16, 17, 25, 70, 95, 100): return True, ''
        nums = meta.get('nums') or []
        if not nums: return True, ''
        if sorted(our_nums(o['payload'])) == sorted(nums): return True, ''
        return False, '原版 %s，我们 payload=[%s]' % (nums, o['payload'])
    return True, ''

def match_card(c):
    cid = c.get('uniqueId') or ''
    if cid in our: return cid, 'id'
    nm = c.get('cardName') or ''
    if nm and namecnt.get(nm, 0) == 1 and truthnamecnt.get(nm, 0) == 1:
        for k, v in our.items():
            if v['name'] == nm: return k, 'name'
    return None, 'none'

# ------------------------------------------------ 逐卡对账
out = []
stat = collections.Counter(); sub = collections.Counter()
matrix = collections.Counter(); percard = collections.Counter()
miss_list, extra_list, diff_list = [], [], []
matchroute = collections.Counter()
# 账 `A1947`：两族卡名（**只出声单列、不进 `out`**，理由见下面那段长注释）
alt_live, alt_content = [], []
# 🔴 `E2`：被**改判**（🔴 ⇒ ⚠️`光环层承载`）的行，逐条出声（⛔ 改判不许无声）
aura_flip = []
# 🔴 `E2`：与 `out` **等长**的「我们面·光环」列（按**卡**成批补，见下面 `row_aura.extend`）
row_aura = []
CARRY_VERB = ('choosecard', 'chooseone', 'chooseeffect', 'lowercost', 'costmore', 'costwhen',
              'costifcontrol', 'paidmod', 'repeat', 'noeffect', 'extratrigger', 'double',
              'sethealth', 'become', 'ferocitystay')

for c in truth:
    cid = c.get('uniqueId') or ''
    abilities = [('a%d' % ai, ab.get('trigger'), ab.get('abilityLogic') or [])
                 for ai, ab in enumerate(c.get('cardAbilities') or [])]
    # 🔴 账 `A1915` 的闸（2026-10-14 加）：**`hasActiveAbility == 0` 的卡不加 `ACT` 行**。
    #   为什么落在这里：`ACT` 行只由下面这一处 `abilities.append` 产生（本文件唯一一处），
    #   闸加在 append 的**条件**上 ⇒ ① `ACT` 行不再产生；② 该 ability 也不再进 `ofams`
    #   （`:446` 那圈算「我们多」时用的是 `abilities`）—— 两处口径同时收窄，这是**有意**的。
    #   判据（原版侧）：`CardScript__ResolveActiveAbilityPlayed.c:23` 在该标志为 0 时
    #   **直接打警告返回**（`if (*(char *)(param_1 + 0x220) == '\0')` → `CustomDebug__LogWarning` +
    #   `return`；`+0x220` = `CardScript.<hasActiveAbility>k__BackingField`，`dump.cs:35444`）
    #   ⇒ 那份 `activeAbility` 在棋局里**不可被发动**，不该拿来当对账的另一边。
    #   ⚠️ 本闸读的是**资产侧**那个同名字段：真值 JSON 是 `RawCardScript` 的 SO dump
    #   （实测 195 个字段名里 105 个命中 JSON；运行期的 `CardScript` 命中 0），
    #   对应 `RawCardScript.hasActiveAbility // 0x2A9`（`dump.cs:23343`）。
    #   **「资产侧 0x2A9 → 运行期 0x220 是否忠实同步」这一跳本闸没验** —— 那是账 `A1875` 的问题。
    #   实据（真值 1195 张现读）：`hasActiveAbility` ∈ {0:1134, 1:58, None:3}；
    #   `activeAbilityLogic` 非空的 **109 张 = 58 张 hAA==1（可发动）＋ 51 张 hAA==0（模板残留）**
    #   ⇒ 本闸只留前一半，被挡掉的 51 张原本产出 **58 条 `ACT` 行**。
    #   ⚠️ 已知误伤面（账 `A1875` 那条没查清）：`hAA==0` 里凡**真是活数据**的会被这道闸误伤 ⇒
    #   逐条点名见 `资料/普查产出_第二十九会话/A1915_对账仪器加闸.md` §四。
    aa = c.get('activeAbility') or {}
    if aa.get('activeAbilityLogic') and c.get('hasActiveAbility'):
        # 🔴 账 `A1937`：第二个字段原为 `46`(=AbilityTrigger.Talent) —— **错的**，改用非枚举标签
        #   `ACT_TRIG`（判据见文件头那段）。它同时影响下面那句 `head = T2H.get(...)` 与再往下的
        #   `if head and o['src'].lower().startswith(head): sc += 3`（「来源加分」那一条）。
        #   ⚠️ 2026-10-14（`A1947`）就地把**行号**换成了**锚点**：原文写的 `:345` / `:380`
        #      **在本笔插入之前就已经对不上**（那一版实测 = `:363` / `:402`）⇒ 别再用行号指这里。
        abilities.append(('ACT', ACT_TRIG, aa['activeAbilityLogic']))

    # 🔴 账 `A1947`（2026-10-14）：**原版的主动能力是【二选一】，上面那段只读了一边。**
    #   判据（第一权威 = 全量反编译，`d:/2/tools/decomp_full/`）：
    #     · `RawCardScript__GetActiveAbility.c:12-20`（属性 `GetActiveAbility`，**权威选择器**）：
    #         if (*(char *)(this + 0x2aa) != '\0')                       // hasAlternativeAbility
    #             if (!BoardAnalysis__MeetsCriteria(…, *(this + 0x2b0), …)) // normalAbilityBoardCriteria
    #                 return *(this + 0x2b8);                            // → alternativeAbility
    #         return *(this + 0x260);                                    // → activeAbility
    #     · `RawCardScript__ResolveActiveAbility.c:18-32` —— **同一选择逻辑的第二份实现**（逐行同形，
    #       连 `goto LAB_18094b162` 落回 `+0x260` 那一手都一样）⇒ 两处互证，不是孤例。
    #   字段（`d:/2/tools/il2cpp_out/dump.cs:23332-23346` · `public class RawCardScript`）：
    #     `activeAbility // 0x260` · `hasActiveAbility // 0x2A9` · `hasAlternativeAbility // 0x2AA`
    #     · `normalAbilityBoardCriteria // 0x2B0`（`BoardCriteria`）· `alternativeAbility // 0x2B8`；
    #     而 `ActiveAbility.activeAbilityLogic // 0x48`（`dump.cs:23765`）—— `.c` 里那句
    #     `lVar6 = *(longlong *)(lVar6 + 0x48)` 就是它。
    #   ⇒ 结论分两半，**别混**：
    #     ① 对 `hasAlternativeAbility == 0` 的卡，上面那段 `if` **完全正确**
    #        （`else` 支结构上不可达 ⇒ 读 `activeAbility` 就是原版的答案）；
    #     ② 对 `== 1` 的卡**够不着替代那一支**，而**离线判不了它该走哪一支** ——
    #        `BoardCriteria` 要**一盘棋的局面**，本仪器是**纯静态对账**（不跑引擎、没有局面）。
    #   ⇒ 处理办法（`⛔ 不替原版挑`）：**读出 + 出声单列**，⛔ **不**把那半边的 `abilityLogic`
    #     塞进 `abilities`。理由：塞进去就等于**替原版断言「它跑了那一条」** —— 它会拿我们一侧的
    #     op 去比一条原版**可能根本没跑**的列表，伪造出「原版有、我们缺」的假账。
    #     那与 `A1915` 那道闸要治的是**同一类病**（拿一个原版不可达的分支去产出对账行）。
    #   ✅ **本快照实测（`cards_1195_2022时代.json` 现读）**：`hasAlternativeAbility` 分布 =
    #     `{0: 1192, 缺: 3}`（缺的那 3 行是 `uniqueId is None` 的占位：`#HB_DA1` / `#HB_BL34`）；
    #     `alternativeAbility` **整份空模板 1195/1195**（`deep_nonzero()` 递归判：非零字段 **0 个**；
    #     只看 `activeAbilityLogic` 非空同样 = **0 张**）⇒ 原版**不会**走替代那一支，
    #     本账对今天的读数**零影响**。⚠️ 这是「**快照里没有**」，⛔ 不是「原版没有」——
    #     活服数据可能有；计数一旦非零，文件尾那一节会**出声点名**。
    if c.get('hasAlternativeAbility'):
        alt_live.append((cid, c.get('cardName'),
                         len((c.get('alternativeAbility') or {}).get('activeAbilityLogic') or [])))
    elif deep_nonzero(c.get('alternativeAbility')):
        alt_content.append((cid, c.get('cardName')))

    ocid, route = match_card(c)
    matchroute[route] += 1
    ocard = our.get(ocid) if ocid else None
    used = {}; seen_step = {}
    _r0 = len(out)          # `E2` 列：本卡产出的行从这儿开始（收尾时整段补同一个光环格）

    for abkey, trig, logics in abilities:
        tname = TNAME.get(trig, str(trig))
        # `head` = 这个 trigger 在卡面正文里的前缀（`Strike:` / `Pray:` …）。⚠️ 账 `A1937`：**只对真枚举
        # trigger 取**（`trig in TNAME`）—— `ACT` 行用的是非枚举标签，`head` 一律 None，
        # 于是下面那条「来源加分」启发式**结构上不可能**给它加分（旧写法会给 `ACT` 行一个
        # 假的 `head='talent'`，即给「`src` 以 `talent` 开头」的 op 白送 3 分）。
        head = T2H.get(tname) if trig in TNAME else None
        order = sorted(range(len(logics)),
                       key=lambda k: 1 if OFAM.get(logics[k].get('chosenAbility')) == 'follow' else 0)
        for li in order:
            lg = logics[li]
            v = lg.get('chosenAbility')
            oname = ANAME.get(v, 'v%s' % v)
            fam = OFAM.get(v, 'other')
            oval, meta, dyn = orig_value(lg)
            row = [cid, abkey, str(li), oname, oval]
            key = (oname, oval)

            if ocard is None:
                out.append(row + ['', '', '❓查不清', '卡不在池',
                                  '卡按 id 与唯一卡名都不在 d:/4 卡池；trig=' + tname])
                stat['❓查不清'] += 1; sub['卡不在池'] += 1
                matrix[(oname, '❓查不清')] += 1; percard[(cid, oname)] += 1
                continue
            if key in seen_step:
                out.append(row + [seen_step[key], '', '⚠️表示不同·语义同', '原版重复N次',
                                  '同一张卡上同一条 (类型,取值) 步骤重复出现 —— 原版写成 N 条独立 ability，'
                                  '我们只有一条 op（次数由数量字段承载）· trig=' + tname])
                stat['⚠️表示不同·语义同'] += 1; sub['原版重复N次'] += 1
                matrix[(oname, '⚠️表示不同·语义同')] += 1
                continue

            sec = [x for x in (meta.get('sec') or []) if x != fam]
            secset = set()
            for x in sec: secset |= EQ.get(x, set())
            best = None
            for i, o in enumerate(ocard['ops']):
                if i in used: continue
                prim = o['fam'] in EQ.get(fam, set())
                if not prim and o['fam'] not in secset: continue
                sc = 30 if prim else 0
                # ⚠️ 这是**我们的启发式、不是原版规则**：op 的 SRC 前缀与 trigger 名对得上 ⇒ +3，
                #    作用是同一张卡有多条触发时，优先挑中「从这个 trigger 那句正文解析出来的」op。
                #    账 `A1937` 只收窄了它的**适用范围**（非枚举的 `ACT` 行拿不到 `head`），
                #    没有动它的打分本身 —— 真枚举行照旧（`Strike`/`Pray`/`Rally`… 那批）。
                if head and o['src'].lower().startswith(head): sc += 3
                if prim and compare(fam, meta, o)[0]: sc += 1
                if best is None or sc > best[0]: best = (sc, i, o, prim)

            if best is not None:
                sc, i, o, prim = best
                if not o['env']: used[i] = (fam, oname)
                oside = op_side(o)
                same, why = compare(fam, meta, o)
                seen_step[key] = oside
                if not prim:
                    out.append(row + [oside, o['src'][:100], '⚠️表示不同·语义同', '原版一条含多效果',
                                      '原版这一条 abilityLogic 同时带多个 criteria 块（%s），我们拆成多条 op'
                                      '（本条对上的是 %s）· trig=%s' % ('+'.join([fam] + sec), o['verb'], tname)])
                    stat['⚠️表示不同·语义同'] += 1; sub['原版一条含多效果'] += 1
                    matrix[(oname, '⚠️表示不同·语义同')] += 1
                    continue
                if o['env']:
                    out.append(row + [oside, o['src'][:100], '⚠️表示不同·语义同', '粒度不同',
                                      '我们把它收在一条「信封」op（%s，payload 即原句文本）里，'
                                      '与引擎内真实登记粒度不同；原版是独立一条 abilityLogic · trig=%s'
                                      % (o['verb'], tname)])
                    stat['⚠️表示不同·语义同'] += 1; sub['粒度不同'] += 1
                    matrix[(oname, '⚠️表示不同·语义同')] += 1
                    continue
                if same:
                    judge, s2 = '✅一致', ''
                else:
                    # 🔴 `E2`：改判之前先问一句「原版这一支是不是落在我们的**光环层**」。
                    _ej, _es, _en = e2_verdict(tname, meta, oval, ocid)
                    if _ej:
                        judge, s2 = _ej, _es
                        # ⚠️ **追加**（⛔ 别覆盖）：`why` 是 `compare()` 给的「原版 X，我们 payload=[Y]」，
                        #   它是这一行**为什么红**的原证据 —— 改判时留着，才看得出「配到的是另一条 op」。
                        why = (why + ' · ' if why else '') + _en
                        sub[AURA_FLIP_SUB] += 1
                        aura_flip.append('%s %-22s %-18s %s ← 我们 %s'
                                         % (cid, oname, tname, oval[:44], oside[:36]))
                    else:
                        judge, s2 = '🔴真差异', '取值不同'
                        sub['取值不同'] += 1
                        diff_list.append((ocid, c.get('cardName'), oname, oval, oside, why))
                        why += _en
                stat[judge] += 1; matrix[(oname, judge)] += 1
                if judge == '🔴真差异': percard[(cid, oname)] += 1
                out.append(row + [oside, o['src'][:100], judge, s2,
                                  (why + ' · ' if why else '') + 'trig=' + tname])
                continue

            merged = None
            for i in used:
                o = ocard['ops'][i]
                if o['fam'] in EQ.get(fam, set()) and compare(fam, meta, o)[0]:
                    merged = o; break
            if merged is not None:
                oside = op_side(merged)
                seen_step[key] = oside
                out.append(row + [oside, merged['src'][:100], '⚠️表示不同·语义同', '拆条差异',
                                  '原版把这一效果拆成多条 abilityLogic（或拆在兄弟 ability 里），'
                                  '我们解析成一条 op（%s）；数值一致 · trig=%s' % (oside, tname)])
                stat['⚠️表示不同·语义同'] += 1; sub['拆条差异'] += 1
                matrix[(oname, '⚠️表示不同·语义同')] += 1
                continue
            if fam == 'vfxonly':
                out.append(row + ['', '', '⚠️表示不同·语义同', '纯表现',
                                  '原版这一支只播特效（无棋局效果），我们的 op 表不为它出 op · trig=' + tname])
                stat['⚠️表示不同·语义同'] += 1; sub['纯表现'] += 1
                matrix[(oname, '⚠️表示不同·语义同')] += 1
                continue
            if fam == 'uionly':
                out.append(row + ['', '', '⚠️表示不同·语义同', '纯 UI',
                                  '原版这一支只开一个看牌窗（无棋局效果）· trig=' + tname])
                stat['⚠️表示不同·语义同'] += 1; sub['纯 UI'] += 1
                matrix[(oname, '⚠️表示不同·语义同')] += 1
                continue
            if fam in ('remove_ench', 'setref'):
                out.append(row + ['', '', '❓查不清', '判据不足',
                                  '原版这一支改的是 AbilityLogic 自己的附魔/引用位（记账），'
                                  '我们这边没有可观测的对应物 · trig=' + tname])
                stat['❓查不清'] += 1; sub['判据不足'] += 1
                matrix[(oname, '❓查不清')] += 1; percard[(cid, oname)] += 1
                continue
            anyfam = [o for o in ocard['ops'] if o['fam'] in EQ.get(fam, set())]
            mech = famseen.get(PRIM.get(fam, fam), 0)
            # 🔴 `E1`（2026-10-14）：判「我们缺」之前，把**这张卡所有 op 的尾句**（`{TAIL:}`）也看一眼。
            #   为什么只改 `备注`、⛔ 不改判定：现读 1120 张卡里 88 条 op 带尾句，**88 条的那半句
            #   都已经各自成形为一条 op**（判据 = 尾句原文 == 某条 op 的 `{SRC:…}`，80/88 直接命中；
            #   余 8 条是「尾句被补全成整句」当 SRC 的那一族，逐条看过：op 也在）⇒ **尾句不构成新证据**，
            #   它只是让「完全没认出来」与「认出来了但没成形」这两种缺陷**在表里分得开**。
            #   ⚠️ 这与 `E2` 那条「光环层承载」是两回事：那条改判定（换了层），这条只补说明。
            _tfam = [(o['tail'], env_fam(o['tail'])) for o in ocard['ops'] if o['tail']]
            _thit = [t for t, f2 in _tfam if f2 == fam]
            tailnote = ('' if not _thit else
                        '；⚠️ 这张卡另有 %d 条 op 的**尾句**（`{TAIL:…}`）提到同族内容 ⇒ 那半句我们'
                        '**认出来了但没成形为 op**：%s' % (len(_thit), ' / '.join(t[:40] for t in _thit[:2])))
            if anyfam:
                s2 = '条数不同'
                why = '原版这张卡有 %d 条同族步骤，我们只有 %d 条同族 op（其余都被别处吃掉了）' % (
                    sum(1 for at in abilities for x in at[2]
                        if OFAM.get(x.get('chosenAbility')) == fam), len(anyfam))
            elif mech:
                s2 = '我们缺'
                why = '我们这边有同族机制（%s 全池 %d 条 op），但这张卡没解析出' % (PRIM.get(fam, fam), mech)
            else:
                s2 = '我们缺'
                why = '我们这边没有任何同族机制（%s 全池 0 条 op）' % '/'.join(sorted(EQ.get(fam, set())))
            # 🔴 `E2`：`WhileInPlay`/`WhileInPlayVariable`/`bt=3 addTrait` 三族 —— 先问光环层。
            _ej, _es, _en = e2_verdict(tname, meta, oval, ocid)
            _j, _s = (_ej or '🔴真差异'), (_es or s2)
            out.append(row + ['', '', _j, _s,
                              why + tailnote + _en + '；原版取值=' + oval + ' · trig=' + tname])
            stat[_j] += 1; sub[_s] += 1
            matrix[(oname, _j)] += 1
            if _ej:
                aura_flip.append('%s %-22s %-18s %s ← 我们（无同族 op）'
                                 % (cid, oname, tname, oval[:44]))
            else:
                percard[(cid, oname)] += 1
                miss_list.append((ocid, c.get('cardName'), oname, oval, s2, why))

    if ocard is not None:
        ofams = set()
        for _ab in abilities:
            for _lg in _ab[2]:
                ofams |= EQ.get(OFAM.get(_lg.get('chosenAbility'), 'other'), set())
        for i, o in enumerate(ocard['ops']):
            if i in used or o['env']: continue
            oside = '%s/%s%s' % (o['verb'], o['amount'], ('|' + o['payload']) if o['payload'] else '')
            if o['fam'] in ofams or o['verb'] in CARRY_VERB:
                out.append([cid, '-', '-', '', '', oside, o['src'][:100],
                            '⚠️表示不同·语义同', '承载不同',
                            '我们这一条 op 是从同一句卡面正文解析出来的机制产物（选牌/改费/重复…）—— '
                            '原版把这件事编码在同一 abilityLogic 的 targetCriteria / cardEffect 字段里，'
                            '没有独立的 chosenAbility 步骤与之对齐'])
                stat['⚠️表示不同·语义同'] += 1; sub['承载不同'] += 1
                matrix[('(无独立对应步骤)', '⚠️表示不同·语义同')] += 1
            else:
                out.append([cid, '-', '-', '', '', oside, o['src'][:100],
                            '🔴真差异', '我们多',
                            '我们这张卡有这条 op，原版整卡 %d 条步骤里没有同族 counterpart'
                            % sum(len(at[2]) for at in abilities)])
                stat['🔴真差异'] += 1; sub['我们多'] += 1
                matrix[('(无独立对应步骤)', '🔴真差异')] += 1
                extra_list.append((ocid, c.get('cardName'), o['verb'], o['payload'], o['src'][:80]))

    # 🔴 `E2`：「我们面·光环」列 —— 本卡产出的**每一行**都补上同一个光环格（见 `aura_cell` 的说明）。
    row_aura.extend([aura_cell(ocid)] * (len(out) - _r0))

# ------------------------------------------------ 落盘
os.makedirs(os.path.dirname(OUT), exist_ok=True)
# 🔴 `E2` 列数守卫：`row_aura` 必须与 `out` 等长。不等 = 有人在卡循环里新加了 `out.append`
#   却漏了收尾那句 `row_aura.extend` ⇒ **出声**并按 `?` 补齐（⛔ 别静默：那样第 11 列会从某一处起
#   **整体错位一格**，而每一行看起来都「有值」—— 本仓踩过同族的坑（行首当锚点、丢列而无人报错））。
if len(row_aura) != len(out):
    print('!! 「我们面·光环」列与行数不齐（%d vs %d）⇒ 有 `out.append` 点漏了收尾的 `row_aura.extend`。'
          '缺的按 `?` 补齐（⛔ 这是**错位告警**，不是「这些卡没有光环」）。' % (len(row_aura), len(out)))
    row_aura.extend(['?'] * (len(out) - len(row_aura)))
lines = ['\t'.join(['卡id', 'ability序', 'logic序', '原版(类型枚举名)', '原版取值',
                    '我们(类型)', '我们取值', '判定', '子类', '备注', '我们面 · 光环'])]
for i, r in enumerate(out):
    r = [str(x if x is not None else '').replace('\t', ' ').replace('\n', ' ') for x in r]
    lines.append('\t'.join(r + [row_aura[i].replace('\t', ' ').replace('\n', ' ')]))
io.open(OUT, 'wb').write(('\r\n'.join(lines) + '\r\n').encode('utf-8'))
print('wrote', OUT, 'rows=', len(out))
print('stat =', dict(stat))
print('sub  =', dict(sub))
print('matchroute =', dict(matchroute))
print('matrix:')
for (k, j), n in sorted(matrix.items(), key=lambda x: -x[1])[:12]:
    print('   %-30s %-18s %d' % (k, j, n))
print('--- 我们缺 前 12 ---')
for m in miss_list[:12]: print('   ', m[0], m[2], '|', m[3][:60], '|', m[5][:80])
print('--- 取值不同 前 12 ---')
for m in diff_list[:12]: print('   ', m[0], m[2], '|', m[5][:100])
print('--- 我们多 前 12 ---')
for m in extra_list[:12]: print('   ', m)
# 🔴 `E2`（2026-10-14 第三十会话）：光环层重判的读数 —— **改判了哪几条、按哪三条判据**。
print('--- `E2` 光环层重判（`🔴真差异` ⇒ `⚠️表示不同·语义同`/`%s`）---' % AURA_FLIP_SUB)
print('   改判合计 %d 条（全部逐条列在下面；⭐ 三条机器判据见 `aura_hit()` 的长注释）：' % len(aura_flip))
for x in aura_flip: print('      ·', x)
_red3 = collections.Counter(); _tot3 = collections.Counter()
for _r in out:
    _t = _r[9].rsplit('trig=', 1)[-1].strip() if 'trig=' in _r[9] else ''
    if _t in AURA_TRIGS: _tot3['trig=' + _t] += 1; _red3['trig=' + _t] += (_r[7] == '🔴真差异')
    if 'addTrait' in _r[4]: _tot3['addTrait(bt=3/90)'] += 1; _red3['addTrait(bt=3/90)'] += (_r[7] == '🔴真差异')
print('   三族现读（改判**后**）：' + ' · '.join(
    '%s 共 %d / 🔴 %d' % (k, v, _red3[k]) for k, v in sorted(_tot3.items())))

# 🔴 账 `A1947`：**替代主动能力**（`alternativeAbility`）—— 这一段**只出声、不产出行**，
#   理由（`GetActiveAbility` 的二选一 + 为什么⛔不替原版挑）见 `for c in truth:` 里那段长注释。
print('--- 替代主动能力（账 `A1947` · `GetActiveAbility` / `ResolveActiveAbility` 的二选一）---')
print('   `hasAlternativeAbility` 为真的卡：%d 张 %s' % (len(alt_live), alt_live[:12]))
print('   `alternativeAbility` 有非零内容的卡：%d 张 %s' % (len(alt_content), alt_content[:12]))
if not alt_live and not alt_content:
    print('   ⇒ 0 张（本快照）：原版**不会**走替代那一支 ——')
    print('      `RawCardScript__GetActiveAbility.c:12-20` 的 `if (hasAlternativeAbility)` 整支不可达')
    print('      ⇒ 上面那些 `ACT` 行就是原版会跑的那一份，**口径完整**。')
else:
    print('   ⚠️ 上面点名的卡：**没有**产出 `ACT` 行，或产出的 `ACT` 行只是二选一里的**一侧** ——')
    print('      原版按**运行期棋盘**（`normalAbilityBoardCriteria`）在 `activeAbility` 与')
    print('      `alternativeAbility` 之间挑（`RawCardScript__GetActiveAbility.c:12-20`）；')
    print('      本仪器是**纯静态对账**（不跑引擎、没有局面）⇒ **离线判不了**，⛔ 没有替它挑。')
    for x in alt_live[:12]: print('      · `hasAlternativeAbility` = 1：', x)
    for x in alt_content[:12]: print('      · `alternativeAbility` 有内容：', x)
