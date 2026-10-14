# 🔴 **2026-10-14（第二十九会话）入仓副本** —— 原件一直住在 `d:/4/_tmp_view/`（**被 `.gitignore` 的
#   `/_tmp_*/` 挡住**）⇒ **下个会话 / 别人 clone 之后复算不了**，而 `资料/` 里有 **9 处**点名它。
#   本副本 = 原件【逐字节】拷贝（含 2026-10-14 `A1915` 加的那道 `hasActiveAbility` 闸），只多这一段头注。
#   🆕 2026-10-14 第二笔（账 `A1947`）：**替代主动能力（`alternativeAbility`）现在会读出并出声单列**
#      —— 判据与「为什么不替原版挑」见正文里那段长注释；本快照计数 = 0（零读数影响）。
#   ⚠️ 它读的 `SCAN`（`d:/4/_tmp_view/our_scan.txt`）**仍在暂存目录里** —— 那是 `工具/ruleprobe.sh scan`
#      的产物，复算前先跑一次 `scan` 再指过去。
#   判据与读数 → `资料/普查产出_第二十九会话/A1915_对账仪器加闸.md`。
# -*- coding: utf-8 -*-
"""A1759/A1772/A1823 —— 原版 cardAbilities[].abilityLogic[] 逐条对账我们的规则引擎。

对账单位 = (卡 id, ability 序号, abilityLogic 序号)
  原版面：d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json
  我们面：工具/ruleprobe.sh scan → d:/4/_tmp_view/our_scan.txt
          （= EffectText.Parse(desc) 的产物，与 `scan`/`check` 基线同口；⚠️ 不含 AuraSpecs / keywords 层）
"""
import json, io, re, os, collections

TRUTH = r'd:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json'
POOL  = r'd:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json'
SCAN  = r'd:/4/_tmp_view/our_scan.txt'
DICT  = r'd:/4/Unity/资料/普查产出_第二十七会话/abilityLogic_枚举词典.tsv'
OUT   = r'd:/4/Unity/资料/普查产出_第二十八会话/A1759_逐条对账.tsv'

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
    if h:
        meta.setdefault('sec', []).append('heal')
        parts.append('heal%s-%s%s' % (h.get('minValue', 0), h.get('maxValue', 0),
                                      ('/opt%d' % h['valueOptions']) if h.get('valueOptions') else ''))
        if h.get('valueOptions'): dyn = True
        elif h.get('minValue', 0) == h.get('maxValue', 0): meta['heal'] = h.get('minValue', 0)
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
                    'env': verb in ENV_VERB_SET})
    our[cid] = {'name': p[1], 'ops': ops, 'nops': len(ops)}

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

def compare(fam, meta, o):
    """返回 (数值是否完全一致, 不一致说明)"""
    if o['verb'] in ENV_VERB_SET: return True, ''
    pl = (o['payload'] or '').lower()
    if fam == 'damage':
        n = meta.get('dmg')
        if n is None: return True, ''
        try: a = int(o['amount'] or 0)
        except Exception: a = None
        return (a == n), ('' if a == n else '原版 %d 点伤害，我们 %s' % (n, o['amount']))
    if fam == 'heal':
        n = meta.get('heal')
        if n is None: return True, ''
        try: a = int(o['amount'] or 0)
        except Exception: a = None
        return (a == n), ('' if a == n else '原版治 %d，我们 %s' % (n, o['amount']))
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
                oside = '%s/%s%s' % (o['verb'], o['amount'], ('|' + o['payload']) if o['payload'] else '')
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
                    judge, s2 = '🔴真差异', '取值不同'
                    sub['取值不同'] += 1
                    diff_list.append((ocid, c.get('cardName'), oname, oval, oside, why))
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
                oside = '%s/%s%s' % (merged['verb'], merged['amount'],
                                     ('|' + merged['payload']) if merged['payload'] else '')
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
            out.append(row + ['', '', '🔴真差异', s2, why + '；原版取值=' + oval + ' · trig=' + tname])
            stat['🔴真差异'] += 1; sub[s2] += 1
            matrix[(oname, '🔴真差异')] += 1; percard[(cid, oname)] += 1
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

# ------------------------------------------------ 落盘
os.makedirs(os.path.dirname(OUT), exist_ok=True)
lines = ['\t'.join(['卡id', 'ability序', 'logic序', '原版(类型枚举名)', '原版取值',
                    '我们(类型)', '我们取值', '判定', '子类', '备注'])]
for r in out:
    r = [str(x if x is not None else '').replace('\t', ' ').replace('\n', ' ') for x in r]
    lines.append('\t'.join(r))
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
