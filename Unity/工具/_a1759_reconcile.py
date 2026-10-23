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
#
# 🔴 **2026-10-19（引擎线波 5 · 账 `A1986` / `A1978` / `A1991`）—— 本文件又动了五处，逐条判据见正文：**
#   ① **真值源换到【当前 build】**：`TRUTH` = `普查产出_1018/allcards_1350/cards_raw.json`（1,351 张当前
#      build 的 `RawCardScript` SO），旧 2022 快照只当旁证（`--truth-old` 可切回）。判据：当前 build 的
#      1,351 个 `uniqueId` **完整覆盖池内 1,127 张（1127/1127）**、且 `hasActiveAbility` 无 `None`
#      ⇒ 旧口径那 316 行「卡不在池」与 3 张「None 静默按 0」都从源头消失。
#   ② **`A1986` 补桥**：原版一步带 `targetCriteria.selectFromChooseResolution == 1` 且 `chosenAbility`
#      属于「造 / 召回 / 置顶 / 召唤 / 抽」那一族时，它的对照物是**我们的 `choosecard`/`chooseone`/
#      `chooseeffect` op**（`EffectResolver.DoChooseCard`），按 `ChooseAct` **逐动作**比（不是"有 choose 就算"）。
#      ⚠️ 只对**那一族**补桥 —— `buffOthers`/`playChosenCardAbility` 上的 `selectFromChooseResolution=1`
#      语义是「目标取自前一次选区」，不是「我们该有 choosecard」，⛔ 别一刀切（会造出假绿）。
#   ③ **`A1978②` 天赋桥**：`trigger=Talent(46)` + `chosenAbility=createCardInHand(100)` +
#      `summonCriteria.unitsToSummon` 的 PPtr **按 `path_id` 解析成卡**，再与我们的 `create` op 载荷
#      逐名比对（显示名取自 `loc_card_names.tsv` —— SO 的 `cardName` 是**内部名**，会与现行印刷名不同：
#      `Warmaster`↔`Chosen of the Four` · `Exemplar of Hate`↔`Master of Repentance` ·
#      `Path of the Warrior`↔`Skyborne Deployment`）。⚠️ 这同时是 `A1978②` 的前提验证。
#   ④ **`A1991`**：`卡id` 列仍印真值 `uniqueId`（下游按它 join 真值），**新追加第 17 列 `我们卡id`**。
#   ⑤ **新列 12–17 = 「打谁 / 打多少 / 什么触发」的机器化**（追加在末尾，前 11 列一字不动）：
#      触发 · 原版·打谁 · 原版·多少 · 我们·打谁 · 我们·多少 · 我们卡id。
# -*- coding: utf-8 -*-
"""A1759/A1772/A1823 —— 原版 cardAbilities[].abilityLogic[] 逐条对账我们的规则引擎。

对账单位 = (卡 id, ability 序号, abilityLogic 序号)
  原版面：d:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json（**当前 build**，1,351 张；默认）
         或 d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json（2022 快照，`--truth` 指过去）
  我们面：工具/ruleprobe.sh scan            → `SCAN`（`EffectText.Parse(desc)` 的产物，与 `scan`/`check` 基线同口）
        ＋工具/ruleprobe.sh layers <out.jsonl> → `LAYERS`（`CardDef.AuraSpecs` / `keywords` 层，卡级 —— 见 `E2`）
  用法：python -I -X utf8 d:/4/Unity/工具/_a1759_reconcile.py
        [--truth P] [--loc P] [--scan P] [--out P] [--layers P] [--no-layers]
        [--bridge-out P] [--talent-out P] [--all-truth]
"""
import json, io, re, os, sys, time, hashlib, collections

## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法：python -I -X utf8 d:/4/Unity/工具/_a1759_reconcile.py\n      [--truth P] [--loc P] [--scan P] [--out P] [--layers P] [--no-layers]\n      [--bridge-out P] [--talent-out P] [--all-truth]\n  -h, --help  只打印本用法并退出（退出码 0），不执行任何真活。')
    _wf_help_sys.exit(0)

TRUTH  = r'd:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json'
TRUTH_OLD = r'd:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json'
LOC    = r'd:/4/Unity/资料/普查产出_1018/allcards_1350/loc_card_names.tsv'
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
# 🔴 2026-10-19 波 5：光环层换成**当前池**的 dump（旧 `our_layers_1014.jsonl` 是 K06 id 迁移**之前**的
#   ⇒ 114 张改了 id 的卡按 id join 会整片落空、静默当成「无光环」）。生成方式（不用重编、不碰基线）：
#     dotnet d:/4/Unity/工具/ruleprobe/bin/Debug/net8.0/wfprobe.dll layers \
#         d:/4/Unity/资料/普查产出_1018/our_layers_1019.jsonl
#   🔴 2026-10-20（W57 EN-3）：**已按上面这条重新生成**（旧文件 2026-10-19 01:14 的 dump 早于
#      E10/E11 的 `CardDef.OfficialAuraSupplements` ⇒ `SW38` 显示「无光环」= **记录过期**，不是缺陷）。
#      现文件 md5 `9368fb77d1401fb1ec5f6cbda2c409a0` · 416,116 B · 1,127 行 · 41 条光环；连跑两次逐字节一致。
#      与旧文件的差 = 11 张卡（SW38 多 `Armour 2` 光环；其余 10 张是 willrun/ch 的解析改进）。
LAYERS = r'd:/4/Unity/资料/普查产出_1018/our_layers_1019.jsonl'
DICT   = r'd:/4/Unity/资料/普查产出_第二十七会话/abilityLogic_枚举词典.tsv'
# 🔴 账 `A1977` 同笔：`OUT` **改前** = `…/普查产出_第二十八会话/A1759_逐条对账.tsv`（历史件）——
#   照默认跑一遍就会把它**静默盖掉**，而 `A1876`/`A1877` 是按它取数的。⇒ 换成本轮新件。
OUT    = r'd:/4/Unity/资料/普查产出_1018/W5_对账_1019.tsv'
BRIDGE_OUT = r'd:/4/Unity/资料/普查产出_1018/W5_选区桥_1019.tsv'
TALENT_OUT = r'd:/4/Unity/资料/普查产出_1018/W5_天赋_1019.tsv'


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
ALL_TRUTH = '--all-truth' in sys.argv
if _scan: SCAN = _scan
if _out: OUT = _out
if _lay: LAYERS = _lay
_tr = _opt('--truth'); _lo = _opt('--loc')
_bo = _opt('--bridge-out'); _to = _opt('--talent-out')
if _tr:
    TRUTH = _tr
    # ⚠️ 旧的 2022 快照是**裸数组**、没有 `loc_card_names` 那套当前显示名 —— 换真值源时自动退回 SO 名，
    #    并在开头**出声**（⛔ 不静默按两套口径跑）。
if _lo: LOC = _lo
if _bo: BRIDGE_OUT = _bo
if _to: TALENT_OUT = _to


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

# 🔴 2026-10-19 波 5：**trait 名补齐**（否则当前 build 的 `huntMark 1260` / `sentry 1280` / `pack 610` 全是
#   `trait1260` 这种无名值 ⇒ 十几种「给 Hunt Mark / 给 Sentry」被仪器判成**取值不同**）。
#   权威 = `DefinedTrait` 枚举本体（反编译签名桩，只读）>`d:/2/Warpforge_code/Scripts/Assembly-CSharp/DefinedTrait.cs`。
#   ⚠️ 词典 `abilityLogic_枚举词典.tsv` 是**旧快照**生成的、只收了当时出现过的 25 个值 —— 这不是它的错，
#      但**别拿它当全集**（本行就是踩过的证据）。
TRAIT_SRC = r'd:/2/Warpforge_code/Scripts/Assembly-CSharp/DefinedTrait.cs'
TRAIT_SRC_OK = False
if os.path.exists(TRAIT_SRC):
    _tt = io.open(TRAIT_SRC, 'r', encoding='utf-8', errors='ignore').read()
    for _m in re.finditer(r'^\s*(\w+)\s*=\s*(\d+)', _tt, re.M):
        TRNAME[int(_m.group(2))] = _m.group(1)
    TRAIT_SRC_OK = True

# 🔴 2026-10-20（W57 EN-3 · T07 残红 `SAU44`）：**印刷名映射** —— `DefinedTrait` 枚举名 ≠ 卡面印刷名。
#   实测 `DefinedTrait.frenzied(680)` 的官方本地化 `Card_Trait/frenzied` = `["Destroyer","毁灭者"]`
#   （源 = `数据/游戏数据/官方本地化_en+zh-CN.json`，2026-10-18 从原版 I2 表并入）⇒ 卡面写
#   "Destroyer"，拿枚举名 `frenzied` 去比**永远对不上** —— `SAU44 Disruption Blades` 那条
#   「条件变体」红就是这么来的（原版 `melee+1` 全体 + `melee+2` 给 `traitsFilter=680` 的高档；
#   我们一条 `give +1 attack`（`altAmount=3` / `altCond=they are destroyer`），语义同）。
#   83 个 `Card_Trait/*` 键里 **12 个**印刷名与枚举名不同（另例：`rage→Penitence`、`sneak→Sneak Attack`、
#   `markOfChaos→Dark Pact`）⇒ 规则③ 的 trait ↔ 卡面文本比对（`_e8b_trait_in`）**两个名字都认**；
#   枚举名排第一（原有的词边界护栏不变，⛔ 不退化成裸 `in`）。
#   ⚠️ 只接 `_e8b_trait_in`（规则③ 的 trait↔卡面文本），**不动** `aura_hit()` 的 R1
#      （trait↔光环载荷）—— 那里换名字会改变另一族判定，本轮无判据、不碰。
TRAIT_LOC = r'd:/4/Unity/数据/游戏数据/官方本地化_en+zh-CN.json'
TRAIT_PRINTED = {}
TRAIT_LOC_OK = False
if os.path.exists(TRAIT_LOC):
    _tl = json.load(io.open(TRAIT_LOC, encoding='utf-8'))
    for _k, _v in (_tl.get('terms') or {}).items():
        if _k.startswith('Card_Trait/') and isinstance(_v, list) and _v:
            _names = [s for s in _v if isinstance(s, str) and s]
            if _names:
                # ⚠️ 这里还没到 `_letters()` 的定义（`:577`）⇒ 就地压字母（口径与它相同）。
                TRAIT_PRINTED[re.sub(r'[^a-z]', '', _k.split('/', 1)[1].lower())] = _names
    TRAIT_LOC_OK = True

# ------------------------------------------------ 真值（当前 build / 旧快照 两形态）
# 🔴 2026-10-19 波 5：当前 build 的导出是 `{"bundle":…, "count":1351, "cards":[…]}`；
#   2022 快照是**裸数组**，且末尾有几条 `uniqueId=None` 的占位（`#HB_*`）。
#   ⇒ 一个装载口收两种形态；**形态打出来**（同一件上「三套数字并存」的教训见 `A1977`）。
_raw_truth = json.load(io.open(TRUTH, 'r', encoding='utf-8'))
TRUTH_SHAPE = 'dict.cards' if isinstance(_raw_truth, dict) and 'cards' in _raw_truth else 'list'
truth = _raw_truth['cards'] if TRUTH_SHAPE == 'dict.cards' else _raw_truth
pool = json.load(io.open(POOL, 'r', encoding='utf-8'))['cards']
POOLIDS = set(c['id'] for c in pool)
namecnt = collections.Counter(c['name'] for c in pool)
truthnamecnt = collections.Counter(c.get('cardName') or '' for c in truth)

# 🔴 `A1978②`：`summonCriteria.unitsToSummon` 是 **PPtr**（`{m_FileID, m_PathID}`）。当前 build 的
#   `cards_raw.json` 每张卡都带 `path_id`（= 它自己的 `m_PathID`）⇒ **PPtr → 卡** 在本机可解。
#   显示名优先取 `loc_card_names.tsv`（`Card_Name/<id>` 的 en）—— SO 的 `cardName` 是**内部名**，
#   会用旧名（实测三例：`Warmaster`↔`Chosen of the Four`…），只拿 SO 名比会造出三条**假差异**。
PATH2CARD = {c.get('path_id'): c for c in truth if c.get('path_id') is not None}
UID2LOC = {}
LOC_SHAPE = '(未读)'
if LOC and os.path.exists(LOC):
    _lines = [l.rstrip('\n').split('\t') for l in io.open(LOC, 'r', encoding='utf-8')]
    if _lines and _lines[0][:3] == ['id', 'en', 'zh_CN']:
        for _r in _lines[1:]:
            if len(_r) >= 2 and _r[0]: UID2LOC[_r[0]] = _r[1]
        LOC_SHAPE = '%s  %d 条' % (LOC, len(UID2LOC))
    else:
        LOC_SHAPE = '!! 表头不是 id/en/zh_CN（没采用）'
else:
    LOC_SHAPE = '!! 文件不存在（显示名退回 SO cardName）'

def card_display(card):
    """真值卡 → 现行显示名。**先**本地化、**后**SO 内部名（理由见上）。"""
    if not card: return ''
    return UID2LOC.get(card.get('uniqueId') or '') or card.get('cardName') or ''

def resolve_summon_names(lg):
    """`summonCriteria.unitsToSummon` → [(uid, 显示名)]（解不出的 PPtr 如实留 `?`）。"""
    out2 = []
    for u in ((lg.get('summonCriteria') or {}).get('unitsToSummon') or []):
        c = PATH2CARD.get(u.get('m_PathID'))
        out2.append((c.get('uniqueId'), card_display(c)) if c else (None, '?UNRESOLVED'))
    return out2

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
    tc_raw = lg.get('targetCriteria') or {}
    parts, meta, dyn = [], {}, False
    # 🔴 2026-10-19（波 5 · `A1986` / `A1978②`）：三样**只进 meta、不改变 `原版取值` 的渲染**
    #   （前 11 列口径一字不动）：
    #     · `choose_res` = `targetCriteria.selectFromChooseResolution == 1`（原版：目标取自「选区结果」）
    #     · `orig_act`   = `chosenAbility` 原值（补桥按它判「该不该有 choosecard」）
    #     · `summon_ids` = `unitsToSummon` 解析出的卡（PPtr → `path_id` → 显示名）
    meta['choose_res'] = tc_raw.get('selectFromChooseResolution') == 1
    meta['orig_act'] = lg.get('chosenAbility')
    meta['summon_ids'] = resolve_summon_names(lg)
    meta['tc_raw'] = tc_raw
    if d:
        meta.setdefault('sec', []).append('damage')
        parts.append('dmg%s-%s%s' % (d.get('minDamage', 0), d.get('maxDamage', 0),
                                     ('/opt%d' % d['valueOptions']) if d.get('valueOptions') else ''))
        if d.get('valueOptions'): dyn = True
        elif d.get('minDamage', 0) == d.get('maxDamage', 0): meta['dmg'] = d.get('minDamage', 0)
        # 🔴 `E8b`：`valueOptions`（动态/区间档，例如 `Unstable` 的 `1-3`）原来**只进 `dyn`**，
        #   而 `dyn` 不进 `meta` ⇒ 「原版这一支是数值类」这件事在 `meta` 里看不出来。
        #   本键**只增加可判据**（`fmt_orig_amount` 的渲染一字不动），供对齐规则判
        #   「`Unstable` = `doDamage + valueOptions`」。
        if d.get('valueOptions'): meta['dmg_opt'] = d.get('valueOptions')
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
        # 🔴 2026-10-19 波 5：`chosenAbility == 50`（buffHand，手牌 buff）时，**承载块是 `handBuff`**，
        #   而 `buffEffect` 里那组数**是序列化残留** —— 实测两张（`TL41` health+3 / `GOF37` ranged+2）
        #   的卡面都只写「Your … cost 1 less this turn」，`handBuff.cardEffect.costChange = -1`
        #   与卡面逐字吻合 ⇒ 拿 `buffEffect` 去比会把两张卡判成「取值不同」。
        #   ⚠️ `原版取值` 的渲染**保留**那块残留（看得见才好核），只把它**排除出 `meta`**。
        _residue = lg.get('chosenAbility') == 50
        if not _residue:
            meta['bt'] = bt
        else:
            meta['bt_residue'] = bt
        ch = []; nums = []
        for k, a in (('melee', 'meleeAttackChange'), ('ranged', 'rangeAttackChange'),
                     ('armour', 'armourChange'), ('health', 'healthChange'), ('cost', 'costChange')):
            if b.get(a):
                ch.append('%s%+d' % (k, b[a]))
                if not b.get(a.replace('Change', 'ValueOptions')): nums.append((k, b[a]))
                else: dyn = True
        if not _residue:
            meta['nums'] = [t for t in nums if t[0] in ('melee', 'ranged', 'armour', 'health')]
        else:
            meta['nums_residue'] = nums
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
        meta['summon_n'] = len(s.get('unitsToSummon') or [])
        meta['summon_enemy'] = bool(s.get('isForEnemy'))
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

def env_fams(payload):
    """信封 op 的**全部**候选族（🔴 2026-10-19 波 5）。
    旧口径 `env_fam()` 只返回**第一个**命中的族 —— 而信封载荷是**整句卡面原文**
    （`At the start of your turn, destroy this unit and deal 3 damage …`），一句话里可以同时有
    「destroy + damage」两族 ⇒ 只看第一个 = 另一族永远找不到对照物（实测 `TL37 Spore Mine`
    的 `destroy` 被报成「我们缺」，其实我们那条 `atturn` op 两个动作都扛了）。"""
    t = (payload or '').lower()
    return {f for rx, f in ENV_FAM if re.search(rx, t)}

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
        _isfam = fam == 'envelope'
        if _isfam: fam = env_fam(f[5])
        # 🔴 2026-10-19 波 5：信封 op 记**全部**候选族（见 `env_fams()` 的说明）。
        _fams = env_fams(f[5]) if _isfam else {fam}
        if not _fams: _fams = {'envelope'}
        ops.append({'verb': verb, 'fam': fam, 'fams': _fams, 'amount': f[4], 'payload': f[5], 'cond': f[8],
                    # 🔴 2026-10-19（波 5 · `A1986`）：`choosecard` 的**动作**在 `ChooseAct`（第 4 列），
                    #   补桥要「逐动作」比 ⇒ 必须把它带出来（原来只留了 payload）。
                    'act': f[3], 'what': f[2], 'src_kind': f[1],
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
    # 🔴 2026-10-19 波 5：卡面**图标**归一（实测 `EC14 Varius` 的 desc 是 `-1 🗡 and -1 🔫` ——
    #   图标不换成词，`PAY` 一个都认不出 ⇒ 真实现被报成「取值不同」）。
    #   映射判据 = `CLAUDE.md` 铁律 7：左圆红剑 = 近战、右圆紫枪 = 远程、银盾 = 护甲。
    #   ⚠️ 全池只有这 1 张卡用这三个图标（现读扫描：🗡/🔫/🛡 各 1 次）⇒ 影响面可控。
    t0 = (payload or '').replace('🗡', ' melee ').replace('🔫', ' ranged ').replace('🛡', ' armour ')
    out = []
    for m in PAY.finditer(t0 or ''):
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
        ours = sorted(our_nums(o['payload']))
        want = sorted(nums)
        if ours == want: return True, ''
        if all(n in ours for n in want):
            # 🔴 2026-10-19 波 5：原版把「+1 Attack 和 +1 Health」拆成**两条 abilityLogic**，
            #   我们解析成**一条** op（payload 同时带两个数）⇒ 逐条比时「我们多一个」是**合并承载**、
            #   不是多给（实测 `Grisly Feast` / `Hardened Biology` / `Master of the Deathwing`）。
            #   ⚠️ 判据取「**包含**」而不是「相等」；备注里把两边都打出来（万一真是多给，人读得见）。
            return True, '原版 %s 我们 %s（我们这条 op 合并承载了原版拆在别步的数值）' % (want, ours)
        return False, '原版 %s，我们 payload=[%s]' % (nums, o['payload'])
    return True, ''

def match_card(c):
    cid = c.get('uniqueId') or ''
    if cid in our: return cid, 'id'
    # 🔴 2026-10-19 波 5：池里有、但 `scan` 没有这一行 = **`desc` 为空被 `Scan.cs` 跳过**（`A1941`
    #   收口的那 6 张）。原来与「不在池」共用「卡不在池」⇒ 把「没正文」说成了「没这张卡」。
    if cid in POOLIDS: return None, 'notext'
    nm = c.get('cardName') or ''
    if nm and namecnt.get(nm, 0) == 1 and truthnamecnt.get(nm, 0) == 1:
        for k, v in our.items():
            if v['name'] == nm: return k, 'name'
    return None, 'none'

# ================================================ 2026-10-19 第五十五会话 `E8b`：三条【有判据】的对齐规则
# 🔴 **它们治的是什么**：`E9_报告.md` 把 141 条 🔴 逐条裁定成 α（仪器/表示差异）124 行 —— 效果**在引擎
#   的另一层**（事件层 / 光环层 / 静态规则 / choose 分支 / persist 等信封 / 关键词通用实现），
#   而本仪器的「我们面」只吃 `EffectText.Parse(desc)` 那一条口（`scan` 的 ops 列）⇒ 按 op 逐条对不齐。
#   ⛔ **不重写既有判定**：新增规则**只在 `best is None` 的兜底段**里生效（既有 ✅/⚠️ 匹配一个字不动），
#   且每条都**带值核对**（`compare()` / `_e8b_branch_value_ok()`）⇒ 真差异（β/γ）**不会被吞**。
#
# **三条规则**（`E9_报告.md` §五 的建议，逐条给判据；每条都配反例，见报告 §三）：
#   ① **choose 分支逐支配**：`chooseone/chooseeffect` 的**每一个分支**、`choosecard` 的**动作**
#      ↔ 原版同一条 ability 的 N 条 abilityLogic **逐条**比（族 + 值）。
#      判据 = 我们的 `EffectText` 把 N 个分支解析成**一条** op（`EffectOp.Payload` 用 `|` 分，
#      `EffectResolver.DoChooseCard` 的 `switch (op.ChooseAct)` 是动作分支）—— 结构事实，不必引反编译。
#      反例：`GOF12 Ork Spanner` 我们只有 3 个分支（+2 melee/+2 ranged/+1 armour），原版 5 条
#      （多 +2 health / +2 melee+1 health / +2 ranged+1 health）⇒ **剩下 3 条仍然红**。
#   ② **`give`/`gain` ↔ `buffSelf`/`buffOthers`**：原版把「自己/全体获得」编成 `buffSelf` 或
#      `buffOthers`（`chosenAbility=40/30`），我们解析成 `give`（buff_other）或 `gain`（buff_self）。
#      `eq_set()` 原来只在 `targetsAffected ∈ {20,120}` 时放行 `buff_other→buff_self` 一个方向；
#      本规则补**另一个方向**（`give` ↔ `buffSelf`）与 `gain` 的「全体」档（⛔ 自指档不放行 ——
#      那会把「给一个友方单位」的 buffOthers 蒙混过去）。
#      反例：`GOF38 Proper Killy` 原版 `melee+2 + bloodThirst`，我们载荷只有 `blood thirst`
#      （卡面也**只印了** Blood Thirst）⇒ 值核对不通过，**仍留红**（⛔ 不拿「SO 多余数值」当已覆盖）。
#   ③ **别层承载 / 编码不同**：原版这一支在我们的**另一层**里（事件层 `WhenTriggers`、光环层
#      `AuraSpecs`、静态规则 `CardDef.MatchStaticBattleRule`、关键词通用实现 `RuleCore.UnstableBlast`、
#      `persist`/`whenclause` 信封…）。判据 = `EffectText.WillRunOps`（`EffectText.cs:2018`）把
#      七个来源汇成「执行层」而 `scan` 只看主解析器；子规则逐条见 `e8b_alignment()` 里每一支的注释。
#      🆕 2026-10-20（W58 E07）：`triggerEffectInCard(260)` / `followAfterResolution(320)` 的六张
#      （`GOF56`/`TL49`/`TL50`/`ASH46`/`GOF41`/`SAU11`）已**逐条建桥**到下面的
#      `E8B_WRAPPER_BETA`（原版载荷形状 + 我方承载 op **双检**）—— 它们**不再是反例集**。
#      判据链 = `资料/普查产出_1019/W57_引擎线_T06_T07.md` §2·2（EN-2 逐条裁）＋
#      `资料/普查产出_1020/W58_引擎线_260_320桥.md`（本笔：把裁定落成机器可复核的两道闸）。
#
# 🔴 **改判必须出声**：每一条被本规则改判的行都进 `e8b_flips`，文件尾按子规则打「改判行数 + 卡名单」。
# 🔴 **A/B 开关**：`A1759_NO_E8B=1` ⇒ 三条对齐规则**整段不生效**（用于复算「加规则前」的读数、
#   以及证明「规则没动既有 ✅/⚠️」—— 报告 §三 的逐行 diff 就是拿它跑出来的）。
E8B_ON = os.environ.get('A1759_NO_E8B') != '1'
E8B_CHOOSE_ACT_FAM = {    'hand': 'create', 'copies': 'create', 'enemyhand': 'create',
    'draw': 'draw', 'decktop': 'recall', 'todeck': 'recall', 'return': 'recall',
    'shuffle': 'recall', 'deploy': 'summon', 'inccost': 'cost',
}
# 🔴 规则③·事件层：**玩家操作驱动的「事件触发」**（不是卡自己打出/常驻）。只列本轮实测到的四个
#   + 同族常见四个；没列到的宁可不判（⛔ 不按「看起来像事件」放宽）。
E8B_EVENT_TRIGS = {'UnitReceivesMarkOfChaos', 'OtherUnitDeath', 'TriggeredSynapse',
                   'OtherCardMob', 'OtherCardPlayed', 'UnitSummoned', 'UnitDeath'}
# 🔴 规则③·静态规则：**判据 = `CardDef.MatchStaticBattleRule`**（`Core/CardDef.cs:1415-1470`）。
#   这些卡在 SO 里被编成一条逐卡 ability，而引擎在**静态规则层**按卡 id 判 —— 不产生 op。
#   ⚠️ 只收 E9 逐条点过名的 5 张（表外一张都不放行）。
E8B_STATIC_RULES = {
    # `卡 → (规则名, 出处, 原版 chosenAbility)` —— `chosenAbility` 一并钉住：这张卡将来若多出一条**别的**
    #   ability 的红色，本规则不会把它也吞掉（表外/别支一律留红）。
    'SAU27': ('MeleeEqualsHealth', 'CardDef.cs:1421', 40),
    'SAU33': ('Bodyguard', 'CardDef.cs:1422', 236),
    'GSC37': ('CostPerEnemyHandCard', 'CardDef.cs:1423-1426', 40),
    'UM106': ('OathTripleActivation', 'CardDef.cs:1461', 30),
    'UM93':  ('OathInAllTurns', 'CardDef.cs:1462', 30),
}
# 🔴 规则③·包装码白名单（**E9 逐条裁定 α 的那批**）：原版把一条真实效果包进 `triggerEffectInCard(260)` /
#   `followAfterResolution(320)`，我们这边是**展开的** op。⛔ 这张表是**卡级白名单**（E9 裁过、
#   **不查我方载荷**）；🆕 2026-10-20（W58 E07）：原来「β 那 6 张故意留作反例」的口径**已撤销** ——
#   它们改走下面的 `E8B_WRAPPER_BETA`（每条都带原版载荷 + 我方承载 op 双检，失配就留红并出声）。
E8B_WRAPPER_ALPHA = {
    'GOF31': 'blind + repeat(per friendly Vehicle) 覆盖「Repeat this effect」；follow/260 是包装码',
    'UM82':  'eachunitdeal 覆盖 Codex「Each of your units deals …」；260 是包装码',
    'ASH43': 'eachunitdeal(Shuriken) 覆盖；260 是包装码',
    'SAU52': 'destroy + deal(perCount=died) 覆盖；260 是包装码',
    'SW56':  'gain +1 fist + forceattack 覆盖；260 是包装码',
    # `DA39`：原版 260 的载荷是 `buff{choiceEnchantment}`（三种 +3 可选），我们是一条
    #   `chooseone/3`（+3 attack / +3 ranged / +3 health）⇒ 分支逐条对上（`E9` 判 α）。
    'DA39':  'chooseone 三个分支（+3 attack / +3 ranged / +3 health）覆盖 choiceEnchantment',
}
# 🔴🔴 2026-10-20（第五十八会话 W58 · 引擎线 E07）：**`260` / `320` 的"有证据桥"** —— 六张逐条建。
#
# **它治的是什么**：原版把一条真实效果包进 `triggerEffectInCard(260)` / `followAfterResolution(320)`
#   （包装码本身不是效果），我们这边是**展开的** op ⇒ 仪器按 op 逐条对不齐，六条留在 🔴「我们缺」。
#   `W57_引擎线_T06_T07.md` §2·2 已逐条给出卡面/原版字段/我方 op 的三方判据；
#   本表把那六条裁定落成**两道机器闸**（⛔ 不是"卡名白名单式地吞掉"）：
#     ① `orig`：原版这一支的**载荷形状**必须仍是判据里那一种（buffType / 伤害档 / 目标筛选）；
#     ② `need`：我们这张卡上**真的存在**承载它的 op（动词 + 数值/载荷/目标），缺一条就不放行。
#   两道闸任一不过 ⇒ **不吞**（继续走既有口径留红），并打一行「证据对不上」的出声（see `_e8b_beta_miss`）。
#   ⚠️ 与 `E8B_WRAPPER_ALPHA` 的分工：alpha = E9 裁过的**卡级**白名单（不查我方载荷）；
#      beta = 本笔这六张（**卡 + 包装码**级，且双检）。两张表互不覆盖。
#
# 逐条证据（卡名 / 卡面原文 / 我方 op / 原版包装码 / 为什么同义）：
#   · `GOF56` **Deffkopta** —— 卡面 `Tide 2. Rally: Other friendly Deffkopta deal 2 damage to a
#     random enemy.`；原版 `a1`（trigger=0 `ThisCardPlayed`）`triggerEffectInCard(260)`，载荷 =
#     `buff{buffType=15 singleUseEnchantment}` + `targetCriteria{friendlyUnits=1,includeMinions=1,
#     filterSpecificCards=[1 张卡]}`；那 1 张 = `c40a…`（`28 Goff-Deffkopta_enchantment`，**已解**），
#     它自己的 abilityLogic 是 `doDamage{2-2}` ⇒ **260 = 给每个其他友方 Deffkopta 挂「打 2」**。
#     （同一 abilityLogic 里那个 `damageCriteria{1-3,valueOptions=90}` 是 `a0` 的 `Unstable` 载荷残留，
#      不是 Rally 的伤害；本卡的 `Unstable` 在我们这边走关键词层 —— `layers.kwKeys.unstable=1`。）
#     我们 `eachunitdeal/2` = 「其他友方 Deffkopta 各打 2」⇒ 同义。
#   · `TL49` **Hyper-adaptation** —— 卡面 `Choose an effect and give it to a friendly troop`；
#     原版 `a0.0` = `triggerEffectInCard(260)` + `buff{buffType=16 choiceEnchantment}` +
#     `tgt{targetsAffected=30}`（一个友方部队）。我们 `chooseeffect/0|give`（目标 `own|troop`）
#     = 选一种附魔并给它 ⇒ 同义（`choiceEnchantment` 就是"选一个可给的效果"的载体）。
#   · `TL50` **Infinite Biomorphologies** —— 卡面 `Choose an effect and give it to all troops in
#     your hand`；原版 `a0.0` = `260` + `buff{buffType=16 choiceEnchantment}` +
#     `tgt{targetsAffected=50,friendlyUnits=1,includeMinions=1}`（手牌）。我们 `chooseeffect/0|hand`
#     ⇒ 同义。
#   · `ASH46` **Aerial Dominion** —— 卡面 `Deal 3 damage to a random enemy. Repeat this effect for
#     every friendly Vehicle`；原版 `a0.1` = `260` + `tgt{friendlyUnits=1,includeMinions=1,
#     keywordFilter=10(Vehicle)}`；它挂的 `enchantment` PPtr `5836…` 已解 = `2ece…`
#     （`T11 SaimHann-Aerial Dominion_enchantment`），其 abilityLogic 是 `doDamage{3-3}` ⇒
#     「对每个友方 Vehicle 各打 3」。我们 `deal/3` + `repeat ... own|vehicle|all board` 同义。
#   · `GOF41` **Unbridled Carnage** —— 卡面 `Deal 1-2 damage to a random enemy. Repeat this effect
#     for every friendly troop`；原版 `a0.1` = `260` + `tgt{friendlyUnits=1,includeMinions=1}`
#     （无 keywordFilter ⇒ 每个友方 troop）；`enchantment` PPtr `2643…` 已解 = `fae4…`
#     （`T12 Goff-Unbridled Carnage_enchantment`），其 abilityLogic 是 `doDamage{1-2,opt90}` ⇒
#     「对每个友方 troop 各打 1-2」。我们 `deal/1~2` + `repeat own|troop|all board` 同义。
#   · `SAU11` **Technomancer** —— 卡面 `Artifice: Reanimate a random friendly Remnant`；
#     原版 `a0.0` = `followAfterResolution(320)` + `tgt{friendlyUnits=1,includeMinions=1,
#     includeRemnants=1,maxTargets=1,conditionsFilter=340(onlyRemnants)}` = "先选一个随机友方残骸，
#     再执行后续"（后续 = `a1.0` 的 `reanimate(430)`，本仪器那里已 ✅）。
#     我们 `reanimate/0`（`DoReanimate` 残骸支：内部随机挑一个友方残骸，`ctx.Rng` 可复现）
#     ⇒ 一条 op 覆盖 320 + 430 两步，同义。
#
# ⚠️ `need` 里的 `raw_has` 用 `op['raw']`（scan 的原始 op 串）—— `repeat` 的计数引用
#   （`own|vehicle|all`）在尾部列的 `countRef`，`parse_ops_bundle` 没有单独取它（见该函数的字段表），
#   而 `raw` 是逐字节原文 ⇒ 用它做子串核对，**不改解析器、不动既有列**。
E8B_WRAPPER_BETA = {
    ('GOF56', 260): {
        'name': 'Deffkopta',
        'face': 'Tide 2. Rally: Other friendly Deffkopta deal 2 damage to a random enemy.',
        'ours': 'eachunitdeal/2',
        'why': '原版 `a1` 的 260 给每个其他友方 Deffkopta 挂 '
               '`28 Goff-Deffkopta_enchantment`（其能力 = 打 2），我们一条 `eachunitdeal 2` '
               '就把「其他友方 Deffkopta 各打 2」落成同义机制',
        'orig': {'bt': 15, 'dmg_opt': 90,
                 'tc': {'friendlyUnits': 1, 'includeMinions': 1}, 'tc_filter_cards': 1},
        'need': [{'verb': 'eachunitdeal', 'amount': '2'}],
    },
    ('TL49', 260): {
        'name': 'Hyper-adaptation',
        'face': 'Choose an effect and give it to a friendly troop',
        'ours': 'chooseeffect/0|give',
        'why': '原版 `buffType=16 choiceEnchantment` 就是「选一个可给的效果」的载体，'
               '我们 `chooseeffect(give)` 对 `own|troop` 与卡面逐字同义',
        'orig': {'bt': 16, 'tc': {'targetsAffected': 30}},
        'need': [{'verb': 'chooseeffect', 'payload_has': 'give',
                  'tgt': {'Side': 'own', 'Kind': 'troop'}}],
    },
    ('TL50', 260): {
        'name': 'Infinite Biomorphologies',
        'face': 'Choose an effect and give it to all troops in your hand',
        'ours': 'chooseeffect/0|hand',
        'why': '原版 `260` + `choiceEnchantment` 的目标是手牌（`targetsAffected=50`），'
               '我们 `chooseeffect(hand)` 与卡面同义',
        'orig': {'bt': 16, 'tc': {'targetsAffected': 50, 'friendlyUnits': 1, 'includeMinions': 1}},
        'need': [{'verb': 'chooseeffect', 'payload_has': 'hand'}],
    },
    ('ASH46', 260): {
        'name': 'Aerial Dominion',
        'face': 'Deal 3 damage to a random enemy. Repeat this effect for every friendly Vehicle',
        'ours': 'deal/3 + repeat ... own|vehicle|all board',
        'why': '原版 `a0.1` 的 260 对每个友方 Vehicle（`keywordFilter=10`）挂 `Aerial Dominion_enchantment`'
               '（其能力 = 打 3），我们 `deal 3` + `repeat(own|vehicle|all board)` 正好表达同一件事',
        'orig': {'bt': 0, 'tc': {'friendlyUnits': 1, 'includeMinions': 1, 'keywordFilter': 10}},
        'need': [{'verb': 'deal', 'amount': '3'},
                 {'verb': 'repeat', 'raw_has': 'own|vehicle|all'}],
    },
    ('GOF41', 260): {
        'name': 'Unbridled Carnage',
        'face': 'Deal 1-2 damage to a random enemy. Repeat this effect for every friendly troop',
        'ours': 'deal/1~2 + repeat ... own|troop|all board',
        'why': '原版 `a0.1` 的 260 对每个友方 troop（目标只筛 friendlyUnits）挂 '
               '`Unbridled Carnage_enchantment`（其能力 = 打 1-2），我们 `deal 1-2` + '
               '`repeat(own|troop|all board)` 正好表达同一件事',
        'orig': {'bt': 0, 'tc': {'friendlyUnits': 1, 'includeMinions': 1, 'keywordFilter': 0}},
        'need': [{'verb': 'deal', 'amount': '1', 'am': 2},
                 {'verb': 'repeat', 'raw_has': 'own|troop|all'}],
    },
    ('SAU11', 320): {
        'name': 'Technomancer',
        'face': 'Artifice: Reanimate a random friendly Remnant',
        'ours': 'reanimate/0',
        'why': '原版 `a0.0` 的 320（`followAfterResolution`）先选一个随机友方残骸、'
               '再执行 `a1.0` 的 `reanimate(430)`；我们 `reanimate`（残骸支内部随机挑一个友方残骸）'
               '一条 op 覆盖这两步，同义',
        'orig': {'bt': 0, 'tc': {'targetsAffected': 20, 'friendlyUnits': 1, 'includeMinions': 1,
                                 'includeRemnants': 1, 'maxTargets': 1, 'conditionsFilter': 340}},
        'need': [{'verb': 'reanimate'}],
    },
}
# 规则③·编码等价：`chosenAbility` 原值 → (需要的我方动词集合, 说明)。
E8B_ENCODING = {
    270: (('return',), 'createCardInDeck(270) ↔ 我们 `return`（回牌库）'),
    420: (('create',), 'setCardDeckToTop(420) ↔ 我们 `create`（造一份放牌库顶）'),
    90:  (('become',), 'v90(unitsToSummon) ↔ 我们 `become`'),
    360: (('gain', 'give', 'deal', 'heal', 'create', 'deploy', 'destroy', 'stun',
           'reanimate', 'forceattack'), 'playChosenCardAbility(360) = Oath 的包装码'),
}
# `E8B_WRAPPER_BETA` 的证据闸没通过时**出声一次**（⛔ 不静默：否则"桥没生效"只能靠人比数字）。
_e8b_beta_miss = set()


def _e8b_beta_orig_ok(meta, spec):
    """`E8B_WRAPPER_BETA[...]['orig']` ↔ 原版这一支的**载荷形状**（⛔ 形状变了就不许沿用桥）。
    支持四种键：`bt`（`meta['bt']`）· `dmg_opt`（`valueOptions` 档）· `tc`（`targetCriteria` 逐字段相等）
    · `tc_filter_cards`（`filterSpecificCards` 的条数）。未知键 ⇒ **停**（⛔ 不静默跳过）。"""
    tc = meta.get('tc_raw') or {}
    for k, v in (spec.get('orig') or {}).items():
        if k == 'bt':
            if meta.get('bt') != v: return False
        elif k == 'dmg_opt':
            if meta.get('dmg_opt') != v: return False
        elif k == 'tc':
            for kk, vv in v.items():
                if tc.get(kk) != vv: return False
        elif k == 'tc_filter_cards':
            if len(tc.get('filterSpecificCards') or []) != v: return False
        else:
            raise SystemExit('!! `E8B_WRAPPER_BETA.orig` 出现未知键 `%s`（⛔ 不静默跳过）' % k)
    return True


def _e8b_beta_carriers(ops, spec):
    """`E8B_WRAPPER_BETA[...]['need']` ↔ **我们卡上真的存在的承载 op**。
    每个 need 是 `{verb, amount?, am?, payload_has?, raw_has?, tgt?}` 的**合取**；
    `tgt` 逐键比 `op['tgt']`（scan 的 `{T:…}` 块）。任何一条 need 找不到 ⇒ 返回 `None`（不放行）。
    返回命中的 op 下标（按 need 顺序、去重）—— 调用方只用它作**证据**（并在行里显示第一条承载 op）；
    ⛔ **不要把它们标 `used`**：同一个 op 可能正当地覆盖另一条原版步骤（见 ③·f-a 的注释）。"""
    hit = []
    for need in spec.get('need') or []:
        found = None
        for i, o in enumerate(ops):
            if 'verb' in need and o['verb'] != need['verb']: continue
            if 'amount' in need and o['amount'] != need['amount']: continue
            if 'am' in need and o.get('am') != need['am']: continue
            if 'payload_has' in need and \
                    need['payload_has'].lower() not in (o['payload'] or '').lower(): continue
            if 'raw_has' in need and need['raw_has'] not in (o['raw'] or ''): continue
            if 'tgt' in need and any(o['tgt'].get(k) != v for k, v in need['tgt'].items()):
                continue
            found = i; break
        if found is None: return None
        if found not in hit: hit.append(found)
    return hit


def _e8b_branch_fams(text, forced):
    s = set(env_fams(text))
    if forced: s.add(forced)
    return s


def _e8b_trait_in(trait, text):
    """trait 名 ↔ 卡面文本（**词边界**，⛔ 别退化成 `_letters(text)` 里的裸 `in`）。
    为什么不直接 `_letters(trait) in _letters(text)`：`vulnerable` 会命中 `invulnerable`
    （与 `aura_hit()` 那条 `R1` 护栏同一个坑）。这里的写法在**原文**上加边界，
    字母之间允许任意非字母分隔 ⇒ `bloodThirst` ↔ `Blood Thirst`、`flank` ↔ `give Flank to …`
    都认得出，而 `vulnerable` 在 `invulnerable` 里匹配不到（前一个字符 `n` 是字母）。

    🆕 2026-10-20（W57 EN-3）：**枚举名与印刷名都认**（判据见上面 `TRAIT_PRINTED` 那段）——
    `frenzied` 卡面印的是 "Destroyer"；只比枚举名会把 `SAU44` 判成真差异。"""
    tw = _letters(trait)
    if not tw: return False
    low = (text or '').lower()
    for cand in [tw] + [_letters(p) for p in TRAIT_PRINTED.get(tw, [])]:
        if not cand: continue
        pat = r'(?<![a-z])' + r'[^a-z]*'.join(re.escape(c) for c in cand) + r'(?![a-z])'
        if re.search(pat, low): return True
    return False


def _e8b_branch_value_ok(fam, meta, text):
    """分支文本 ↔ 原版这一步的**值核对**（族已经对上之后才调用）。
    ⛔ 值核不过 = 不匹配（这是「不吞真缺口」的那一半）。"""
    tl = (text or '').lower()
    t = text or ''
    if fam in ('damage', 'heal'):
        want = meta.get('dmg') if fam == 'damage' else meta.get('heal')
        sp = meta.get('dmg_span') if fam == 'damage' else meta.get('heal_span')
        if want is None and sp is None: return True
        if fam == 'damage':
            nums = [(int(a), int(b) if b else None)
                    for a, b in re.findall(r'(\d+)(?:\s*-\s*(\d+))?\s*damage', tl)]
        else:
            nums = [(int(a), int(b) if b else None)
                    for a, b in re.findall(r'(\d+)(?:\s*-\s*(\d+))?\s*heal', tl)]
            nums += [(int(a), None) for a in re.findall(r'heal(?:s)?\s+(\d+)', tl)]
        if not nums: return False
        if sp is not None:
            return any((a, b if b else a) == tuple(sp) for a, b in nums)
        return any(a == want or (b is not None and a <= want <= b) for a, b in nums)
    if fam in ('buff_other', 'buff_self', 'buff_hand'):
        bt = meta.get('bt', 0)
        if bt in (3, 4, 90):
            if bt == 4: return True
            return _e8b_trait_in(meta.get('trait'), t)
        if bt == 2:
            return 'cost' in tl
        if bt in (8, 12, 15, 16, 17, 25, 70, 95, 100): return True
        nums = meta.get('nums') or []
        if not nums: return True
        ours = our_nums(t)
        return all(n in ours for n in nums)
    return True


def _e8b_choose_branches(o):
    """choose 类 op → `[(分支序号, 分支文本, 强制的族 or None)]`。
    · `chooseone`：`EffectOp.Payload` 用 `|` 分（解析层事实）；
    · `chooseeffect`：载荷就是那一句效果文本；
    · `choosecard`：**动作在 `ChooseAct`**（第 4 列）—— 用动作映射族，文本取 `what` 供值核对。"""
    if o['verb'] == 'chooseone':
        return [(i, b, None) for i, b in enumerate((o['payload'] or '').split('|')) if b.strip()]
    if o['verb'] == 'chooseeffect':
        return [(0, o['payload'] or '', None)]
    if o['verb'] == 'choosecard':
        return [(0, ((o.get('what') or '') + ' ' + (o.get('act') or '') + ' '
                     + (o.get('payload') or '')).strip(),
                 E8B_CHOOSE_ACT_FAM.get(o.get('act') or ''))]
    return []


def _e8b_willrun_fams(ocid):
    d = layers.get(ocid) or {}
    fs = set()
    for w in d.get('willrun') or []:
        fs.add(VFAM.get(w.get('v'), '?'))
    return fs


def _e8b_card_text(ocard):
    return ' '.join((o.get('src') or '') for o in (ocard or {}).get('ops') or [])


def _e8b_trait_of_tc(meta):
    """当前这一步的 targetCriteria 里带的**筛选 trait 名**（用于「条件变体」规则）。"""
    tc = meta.get('tc_raw') or {}
    for k in ('traitsFilter', 'keywordFilter', 'excludeTrait'):
        v = tc.get(k)
        if v:
            return TRNAME.get(v) or ''
    return ''


def _e8b_value_rules(fam, meta, ops, used, text_all):
    """规则③ 的**取值编码**两支（`拆分求和` / `取值以卡面为准`）＋ `条件变体`。
    这几支同时服务**两条路**：`best is None` 的兜底（`e8b_alignment`）与
    **已经配到 op、但值不等** 的那条（`取值不同`/`条数不同` 两格）—— 判据只写这一处。"""
    if fam in ('damage', 'heal'):
        want = meta.get('dmg') if fam == 'damage' else meta.get('heal')
        if want is not None:
            if fam == 'damage':
                famops = [(i, o) for i, o in enumerate(ops) if o['verb'] in ('deal', 'eachunitdeal')]
            else:
                famops = [(i, o) for i, o in enumerate(ops) if o['verb'] == 'heal']
            ssum = sum(int(o['amount'] or 0) for _, o in famops)
            if len(famops) > 1 and ssum == want:
                for i, _o in famops: used.setdefault(i, (fam, '拆分求和'))
                return ('⚠️表示不同·语义同', '编码不同·拆分求和',
                        '原版存**绝对 %d**；我们把它拆成 %d 条同族 op（合计 %d）' % (want, len(famops), ssum),
                        famops[0][1])
            for i, o in famops:
                n = int(o['amount'] or 0)
                if n and n != want and re.search(r'\b%d\b\s*(?:-\s*\d+\s*)?(?:damage|heal)' % n,
                                                 (o['src'] or '').lower()):
                    return ('⚠️表示不同·语义同', '取值以卡面为准',
                            '原版 SO 写 %d，我们的取值 %d **与卡面原文一致**（`%s`）⇒ 以卡面为准'
                            '（铁律 2/7）' % (want, n, o['src'][:60]), o)
    # 条件变体：原版把同一效果写成「基础值 + 带筛选 trait 的加强值」两条；我们的卡面用条件句表达。
    tw = _e8b_trait_of_tc(meta)
    if tw and any(f == fam for f, _ in used.values()) and _e8b_trait_in(tw, text_all):
        return ('⚠️表示不同·语义同', '条件变体',
                '原版把同一效果写成**两条**（第二条带筛选 `%s`），我们的卡面/op 用条件句表达' % tw, None)
    return None


def e8b_alignment(cid, ocid, tname, trig, fam, meta, oval, ocard, used, branch_used, orig_fams=None):
    """三条对齐规则的**唯一入口**（`best is None` 的兜底段调用）。
    返回 `(判定, 子类, 备注, 对照 op or None)`；`None` = 本规则不管这一行（调用方照旧判红）。"""
    if ocard is None: return None
    if not E8B_ON: return None
    ops = ocard['ops']
    _dbg = os.environ.get('A1759_DEBUG') == cid
    if _dbg:
        print('   [DBG]', cid, 'fam=', fam, 'trig=', tname, 'oa=', meta.get('orig_act'),
              'bt=', meta.get('bt'), 'kw=', (layers.get(ocid) or {}).get('kwKeys'),
              'ops=', [(o['verb'], o['act'], o['payload'][:20], o.get('tgt')) for o in ops])
    kw = (layers.get(ocid) or {}).get('kwKeys') or {}
    oa = meta.get('orig_act')
    text_all = _e8b_card_text(ocard)

    # ---------------- ① choose 分支逐支配 ----------------
    # ⚠️ **同一个分支可以被原版的第二条 step 再用一次**（`拆条`）：例如 `SAU40` 我们只有一个
    #   组合分支「+3 health and vanguard」，原版写成「+3 health」「+3 health + vanguard」两条 ——
    #   第二条要能复用第一条用掉的那个分支（值核依旧是 `_e8b_branch_value_ok`，⛔ 不是无条件重用）。
    for allow_reuse in (False, True):
        for i, o in enumerate(ops):
            if o['verb'] not in CHOOSE_VERBS: continue
            for bi, btext, forced in _e8b_choose_branches(o):
                if (i, bi) in branch_used and not allow_reuse: continue
                bfams = _e8b_branch_fams(btext, forced)
                if not (bfams & eq_set(fam, meta)): continue
                if not _e8b_branch_value_ok(fam, meta, btext): continue
                _reuse = (i, bi) in branch_used
                branch_used.add((i, bi))
                return ('⚠️表示不同·语义同', '选牌分支对齐',
                        '原版这一支 = 同一条 ability 的 N 个分支之一；我们把它解析成**一条** `%s` '
                        '（第 %d 个分支「%s」），族与值逐条对上%s · trig=%s'
                        % (o['verb'], bi + 1, btext[:70],
                           '（原版把同一分支拆成多条 step ⇒ 复用）' if _reuse else '', tname), o)

    # ---------------- ② give/gain ↔ buffSelf/buffOthers ----------------
    if fam in ('buff_self', 'buff_other'):
        for i, o in enumerate(ops):
            if o['verb'] not in ('give', 'gain'): continue
            if fam == 'buff_other' and o['verb'] == 'gain':
                # ⛔ 自指档（`未写主语：有施放者就是施放者自己`）**不放行** —— 见块头反例说明。
                if (o.get('tgt') or {}).get('Subjectless') == '1': continue
            # 🔴 **值核对不能被 `bt=3` 那句短路口放过**：`compare()` 对 `addTrait` 只看 trait 名、
            #   **不看同一块里的 nums**（原版常把「+2 melee + Blood Thirst」写在一起）⇒
            #   这里补一句：原版带 nums 时，我们的载荷必须**逐项含有**它们。
            #   反例 = `GOF38 Proper Killy`：原版 `melee+2 + bloodThirst`，我们载荷只有
            #   `blood thirst`（卡面也只印了它）⇒ 本规则**不替它翻绿**（留红待人工裁）。
            if meta.get('nums') and not all(n in our_nums(o['payload'] or '') for n in meta['nums']):
                continue
            same, _w = compare(fam, meta, o)
            if not same: continue
            reused = i in used
            used[i] = (fam, 'buff 族对齐')
            return ('⚠️表示不同·语义同', '拆条差异' if reused else 'buff 族对齐',
                    '原版 `%s`（`chosenAbility=%s`）↔ 我们 `%s`：族与值对上（%s）· trig=%s'
                    % (fam, oa, o['verb'], o['payload'][:60] or '（无载荷）', tname), o)

    # ---------------- ③·a `Unstable` 关键词通用实现 ----------------
    # 判据：原版把 Unstable 编成 `doDamage + buff{addTrait fast} + 1-3`（AUD §三「11 条」）；
    #   我们的实现 = 关键词表 `unstable` → `RuleCore.UnstableBlast`（`Core/RuleCore.cs:8936`）。
    if (kw.get('unstable') and meta.get('bt') == 3 and _letters(meta.get('trait')) == 'fast'
            and ('dmg' in meta or 'dmg_span' in meta or meta.get('dmg_opt'))):
        return ('⚠️表示不同·语义同', '别层承载·关键词',
                '原版这一支是 `Unstable` 的逐卡编码（`doDamage buff{addTrait fast}`）；'
                '我们走关键词通用实现（`RuleCore.UnstableBlast`，判据 `RuleCore.cs:8936`）· trig=' + tname, None)

    # ---------------- ③·b `Pray` 容器 ----------------
    # 判据：原版把 Pray 能力编成「自身 singleUseEnchantment + 召唤附魔卡」（或 `triggerPray`(462)）；
    #   我们的实现 = 关键词 `pray` → `TriggerTexts["Pray"]` → `ResolveAtTurn/TriggerOps`。
    if kw.get('pray') and (tname in ('Pray', 'TriggeredPray') or oa == 462):
        return ('⚠️表示不同·语义同', '别层承载·关键词',
                '原版这一支是 `Pray` 的容器（`singleUseEnchantment` / `triggerPray`）；'
                '我们的实现 = 关键词 `pray` 的触发正文（`CardDef.TriggerTexts`）· trig=' + tname, None)

    # ---------------- ③·c 事件层（`WhenTriggers`） ----------------
    # 判据：`EffectText.WillRunOps`（`EffectText.cs:2030-2031`）把 `WhenTriggers` 收进执行层；
    #   我们的主解析口（`scan`）看不到它 ⇒ 只要「事件触发 + willrun 里有同族 op」就是别层承载。
    if tname in E8B_EVENT_TRIGS and ((layers.get(ocid) or {}).get('ch') or {}).get('when', 0) > 0:
        if fam in _e8b_willrun_fams(ocid):
            return ('⚠️表示不同·语义同', '别层承载·事件层',
                    '原版这一支挂在事件触发（`%s`）上；我们的实现 = `WhenTriggers`'
                    '（`EffectText.WillRunOps` 收进执行层，`EffectText.cs:2030`）· trig=%s' % (tname, tname), None)

    # ---------------- ③·d 附魔载体（enchantment / singleUseEnchantment） ----------------
    # 判据：原版 `buffType ∈ {12,15,16}` 的那一支 = **载体**（附魔卡本身才是效果），
    #   `E9` 逐条裁定「编码，不是独立效果」；我们的表现 = `persist`/`whenclause`/`atturn` 信封、
    #   光环层，或 `choosecard` 的选区入口。⛔ 要求信封载荷**真的有族**（`env_fams` 非空）。
    if meta.get('bt') in (12, 15, 16) and oa not in (260, 320):
        envops = [o for i, o in enumerate(ops) if o['verb'] in ('persist', 'whenclause', 'atturn')]
        chooses = [o for i, o in enumerate(ops) if o['verb'] in CHOOSE_VERBS]
        if envops or (layers.get(ocid) or {}).get('auras') or chooses:
            for i, o in enumerate(ops):
                if o['verb'] in ('persist', 'whenclause', 'atturn') and env_fams(o['payload']):
                    used.setdefault(i, (fam, '附魔载体')); break
            if not envops:
                for i, o in enumerate(ops):
                    used.setdefault(i, (fam, '附魔载体'))
            return ('⚠️表示不同·语义同', '别层承载·附魔载体',
                    '原版这一支是 `%s` 附魔**载体**（`buffType=%s`）；我们把这句直接落成 '
                    '`persist`/`whenclause`/光环/选区（编码不同，不是独立 op）· trig=%s'
                    % (BTNAME.get(meta.get('bt'), meta.get('bt')), meta.get('bt'), tname), None)

    # ---------------- ③·e 静态战斗规则（`CardDef.MatchStaticBattleRule`） ----------------
    # ⚠️ 表内 5 张**逐条**由 `E9` 裁过（每张的 SO 里只有那一条「静态规则」被编成逐卡 ability）；
    #   表外一张都不放行 ⇒ 判据是表本身，不是家族猜测。
    if cid in E8B_STATIC_RULES and oa == E8B_STATIC_RULES[cid][2]:
        _nm, _src, _oa = E8B_STATIC_RULES[cid]
        return ('⚠️表示不同·语义同', '别层承载·静态规则',
                '原版这一支是逐卡 ability，我们的实现 = 静态规则 `%s`（`%s`）· trig=%s'
                % (_nm, _src, tname), None)

    # ---------------- ③·f-a `260` / `320` 的**有证据桥**（六张；W58 E07） ----------------
    # 🔴 2026-10-20：`E8B_WRAPPER_BETA` 的每一张都**双检**（见那张表的头注）：
    #   ① `orig`：原版这一支的载荷形状仍是判据里那一种；② `need`：我们卡上真的有承载 op。
    #   ⛔ 任一不过 ⇒ 不吞（继续走下面的既有口径留红），并出声一次（`_e8b_beta_miss`）。
    if oa in (260, 320):
        _beta = E8B_WRAPPER_BETA.get((cid, oa))
        if _beta is not None:
            _hit = _e8b_beta_carriers(ops, _beta)
            if _hit is not None and _e8b_beta_orig_ok(meta, _beta):
                # ⚠️ **刻意不标 `used`**（与 alpha 不同）：本桥的承载 op 常常**同时**被另一条原版步骤
                #   正当地用掉（实测 `SAU11`：`reanimate` 既覆盖 `a0.0` 的 320、又是 `a1.0` 的 430；
                #   在 320 这一行把它标 used ⇒ `a1.0` 的 ✅ 会掉成 `⚠️拆条差异`，凭空多一条改判）。
                #   这六张的 op 本来就各有归属（伤害/choose/reanimate 均有既有行），⛔ 不需要清「我们多」。
                return ('⚠️表示不同·语义同', '别层承载·包装码',
                        '原版这一支 = `%s` 包装码（%s）。卡面「%s」；我们 `%s`；%s · trig=%s'
                        % (ANAME.get(oa, oa), _beta['name'], _beta['face'],
                           _beta['ours'], _beta['why'], tname),
                        ops[_hit[0]] if _hit else None)
            if (cid, oa) not in _e8b_beta_miss:
                _e8b_beta_miss.add((cid, oa))
                print('   ⚠️ `E8B_WRAPPER_BETA` 的桥**证据对不上**，按原口径留红：%s/%s'
                      '（我方应有=%s；原版载荷=%s；need 命中=%s）'
                      % (cid, oa, _beta['ours'], oval[:100], _hit))

    # ---------------- ③·f 包装码白名单（`260` / `320`） ----------------
    if oa in (260, 320) and cid in E8B_WRAPPER_ALPHA:
        # ⚠️ 只把**原版整卡任何一步都用不到的族**记成「已承载」（清掉假「我们多」）；
        #   别把**别的 step 还要用的 op** 也吃掉 —— 实测 `UM118 a2` 的 doDamage 会因此从 ✅ 掉成 ⚠️。
        _of = orig_fams or set()
        for i, o in enumerate(ops):
            if o['verb'] in CHOOSE_VERBS: continue
            if not (set(o.get('fams') or {o['fam']}) & _of):
                used.setdefault(i, (fam, '包装码'))
        return ('⚠️表示不同·语义同', '别层承载·包装码',
                '原版这一支 = `%s` 包装码；%s · trig=%s'
                % (ANAME.get(oa, oa), E8B_WRAPPER_ALPHA[cid], tname), None)

    # ---------------- ③·g `drawCard + autoPlay`（从牌库打出） ----------------
    # 判据：原版 drawCard 那一步**带 `summonCriteria`**（`orig_value` 记成 `summ:n=`）= 抽到就打出；
    #   我们的实现 = 一条 `deploy … from deck`（`EffectOp.AutoPlay`，`A1571`）。
    if fam == 'draw' and 'summon_n' in meta:
        for i, o in enumerate(ops):
            if o['verb'] != 'deploy': continue
            used[i] = (fam, 'draw+autoPlay')
            return ('⚠️表示不同·语义同', '编码不同·draw+autoPlay',
                    '原版 `drawCard` 那一步带 `summonCriteria`（抽到即打出），我们落成一条 '
                    '`deploy %s`（`A1571` 的 autoPlay 语义）· trig=%s' % (o['payload'][:40], tname), o)

    # ---------------- ③·h 资源族（mana）由 `gain` 的载荷承载 ----------------
    if fam == 'mana':
        for i, o in enumerate(ops):
            if o['verb'] in ('gain', 'give', 'refill', 'gainenergy', 'gainquest', 'gainfaith', 'gainspirit') \
                    and re.search(r'energy|quest point|faith|spirit stone|\bmana\b|☀', o['payload'] or '', re.I):
                used.setdefault(i, (fam, '资源载荷'))
                return ('⚠️表示不同·语义同', '编码不同·资源',
                        '原版这一支 = `buffMana`；我们的 `%s` 载荷「%s」同时承载了它 · trig=%s'
                        % (o['verb'], o['payload'][:50], tname), o)

    # ---------------- ③·i `ferocitystay`（原版编成 addTrait） ----------------
    if meta.get('bt') == 3 and _letters(meta.get('trait')).startswith('dontreturnferocity'):
        for i, o in enumerate(ops):
            # 我们的解析口有两种写法：`ferocitystay` 动词（SW40）或 `whenclause` 承载整句
            # （SW36「it stays in play」）⇒ 两种都算同一个机制的产物。
            if o['verb'] == 'ferocitystay' or (
                    o['verb'] in ('whenclause', 'persist')
                    and 'stays in play' in (o['payload'] or '').lower()):
                used.setdefault(i, (fam, 'ferocitystay'))
                return ('⚠️表示不同·语义同', '编码不同·ferocitystay',
                        '原版把「用狂暴后留在场上」编成 `addTrait %s`；我们的动词/信封 = `%s`'
                        '（`EffectResolver.cs:8226 DoFerocityStay`）· trig=%s'
                        % (meta.get('trait'), o['verb'], tname), o)

    # ---------------- ③·j 副本修饰块（ephemeralCopy / setCost） ----------------
    if meta.get('bt') in (25, 95):
        for i, o in enumerate(ops):
            if o['verb'] not in ('whenclause', 'create'): continue
            used.setdefault(i, (fam, '副本修饰'))
            return ('⚠️表示不同·语义同', '编码不同·副本修饰',
                    '原版把「造一份副本」的修饰块（`bt=%s`）写成同一条 abilityLogic；'
                    '我们的 `%s` 承载 · trig=%s' % (BTNAME.get(meta.get('bt'), meta.get('bt')), o['verb'], tname), o)

    # ---------------- ③·k 编码等价（createCardInDeck / setCardDeckToTop / v90 / playChosenCardAbility） ----------------
    # 判据（`E9_报告.md` §二·1 逐条）：原版 `chosenAbility` 与我们动词的**同义编码**；
    #   例 `createCardInDeck(270)` ↔ 我们 `return`（把墓地里的牌放回牌库）；
    #   `setCardDeckToTop(420)` ↔ 我们 `create … top of your deck`；`playChosenCardAbility(360)` = Oath 包装码。
    if oa in E8B_ENCODING:
        wants, why = E8B_ENCODING[oa]
        for i, o in enumerate(ops):
            if o['verb'] not in wants: continue
            if oa == 420 and o['verb'] == 'create' and 'deck' not in (
                    (o['payload'] or '') + ' ' + (o.get('src') or '')).lower():
                continue        # ⛔ 420 只认「造一份放牌库顶」的那条 create（判据可来自 SRC 原文）
            if oa == 360:
                # `playChosenCardAbility(360)` = Oath 的**包装码**：整张卡的正文都是它的展开
                #   ⇒ 把**别的原版 step 用不到的** op 记为已承载（清掉假「我们多」）。
                #   ⚠️ 别无条件全标记 —— 实测 `UM118 a2` 的 doDamage 会因此从 ✅ 掉成 ⚠️（A/B 抓到）。
                for j, oj in enumerate(ops):
                    if not (set(oj.get('fams') or {oj['fam']}) & (orig_fams or set())):
                        used.setdefault(j, (fam, '编码等价'))
            else:
                used.setdefault(i, (fam, '编码等价'))
            return ('⚠️表示不同·语义同', '编码不同',
                    '原版 `%s`；%s（我们 `%s`）· trig=%s' % (ANAME.get(oa, oa), why, o['verb'], tname), o)

    # ---------------- ③·l `v480 highlightBloodThirst` = 纯表现 ----------------
    if oa == 480:
        return ('⚠️表示不同·语义同', '别层·纯表现',
                '原版这一支的 `highlightBloodThirst(480)` 是**纯表现**（高亮），棋局影响那半边'
                '（heal/buff）已由我们的同族 op 承载 ⇒ 属特效线 · trig=' + tname, None)

    # ---------------- ③·m `double` 动词 ↔ buff 族 ----------------
    if fam in ('buff_other', 'buff_self'):
        for i, o in enumerate(ops):
            if o['verb'] != 'double': continue
            used.setdefault(i, (fam, 'double'))
            return ('⚠️表示不同·语义同', '编码不同·double',
                    '原版这一支是属性 buff；我们的动词 = `double`（`EffectResolver.cs:874`）· trig=' + tname, o)

    # ---------------- ③·n 取值编码（卡面值 / 拆分求和）＋ ③·t 条件变体 ----------------
    return _e8b_value_rules(fam, meta, ops, used, text_all)


# ================================================ `A1986` 选区桥（2026-10-19 波 5）
# 🔴 **病灶**（判据 → `资料/待办判据_第三十三会话.md` §A1986 · `普查产出_第三十会话/D2_造卡族判定.md` §一）：
#   原版有些步骤的 `chosenAbility` 叫 `createCardInHand(100)` / `recallToDeck(300)` / `setCardDeckToTop(420)`
#   / `createCardInDeck(270)` / `summonUnit(70)`，但 `targetCriteria.selectFromChooseResolution == 1`
#   ⇒ 语义 = **先从候选里挑一张、再对它做那个动作**。我们这一侧是**一个** `choosecard` op
#   （动作在 `ChooseAct`，结算 = `EffectResolver.DoChooseCard`）⇒ 两条路之间**没桥**时，
#   这一族在仪器里**结构性恒报「我们缺」**（实测旧口径 32/152 条「我们缺」来自这里）。
#
# ⛔ **桥只给【造/召回/置顶/召唤/抽】这一族**（`BRIDGE_ACT` 的键）。理由：
#   `buffOthers` / `playChosenCardAbility` / `doDamage` 上的 `selectFromChooseResolution=1` 语义是
#   「**目标取自前一次选区**」（Oath 后续句、Dark Pact 之类），不是「我们该有一个 `choosecard` 动词」——
#   给它们也架桥 = 把「目标作用域」当成「动作」⇒ **假绿**。这一族的对照物仍是卡上那条同族 op
#   （`lowercost` / `give` / `deal` …，走既有的「拆条差异 / 承载不同」两支）。
#
# ⚠️ **动作要逐条比**（不是「有 choose 就算」）：原版说「造进手里」，我们的 `choosecard` 若做的是
#   `deploy`，那是**真差异**，不是口径差。
BRIDGE_ACT = {
    0:   {'draw'},                               # drawCard
    70:  {'deploy'},                             # summonUnit
    75:  {'deploy'},                             # summonBuffedUnit
    100: {'hand', 'copies', 'enemyhand'},        # createCardInHand
    101: {'hand', 'copies', 'enemyhand'},        # createCardInHandAndKeepEffects
    140: {'hand'},                               # recallToOwnerHand
    150: {'hand'},                               # （同族）
    250: {'deploy'},                             # autoPlayFromHand
    270: {'todeck', 'shuffle'},                  # createCardInDeck
    300: {'return', 'shuffle', 'todeck'},        # recallToDeck
    420: {'decktop'},                            # setCardDeckToTop
}
# `ChooseAct == ''` = **动作在下一句**（`EffectResolver.DoChooseCard` 的 `case ""`：日志明写
# 「后续句接着结算」）⇒ 这时看**同一张卡上**有没有对应的兄弟 op（`drawref` / `create` / …）。
BRIDGE_SIBLING = {
    0:   {'draw', 'drawref'},
    70:  {'deploy', 'summon', 'create'},
    75:  {'deploy', 'summon', 'create'},
    100: {'create'},
    101: {'create'},
    140: {'create'},
    150: {'create'},
    250: {'deploy', 'summon', 'create'},
    270: {'create', 'todeck'},
    300: {'return', 'shuffle'},
    420: {'decktop'},
}
CHOOSE_VERBS = ('choosecard', 'chooseone', 'chooseeffect')


def is_bridge_step(meta):
    """原版这一步是否属于「选区 + 造/召回/…」那一族（= 需要桥）。"""
    return bool(meta.get('choose_res')) and meta.get('orig_act') in BRIDGE_ACT


# 🔴 2026-10-19 波 5：**`buffOthers` ↔ 我们的 `gain`（buff_self）** —— 原版把「Your Warlord gains X」
#   编码成 `chosenAbility=30 buffOthers` + `targetCriteria.targetsAffected = friendlyHero(120)`（或
#   `self(20)`），而我们的解析器写的是 `gain/…`（`buff_self`）。旧家族表里两族**互不包含** ⇒
#   35 条真实现被报成「我们缺」（实测 `EC47`/`SW56`/`GOF72`/`GOF12` 那一族）。
#   ⚠️ **只在目标确实是「自己 / 己方督军」时才放行**（`targetsAffected ∈ {20, 120}`）——
#   否则「给一个友方单位」的 buffOthers 也会被 `gain` 蒙混过关（静默假绿）。
def eq_set(fam, meta):
    s = set(EQ.get(fam, set()))
    if fam == 'buff_other':
        _ta = (meta.get('tc_raw') or {}).get('targetsAffected')
        if _ta in (20, 120):
            s.add('buff_self')
    return s


def bridge_match(meta, o, ocard):
    """`True` = 这条 op 就是原版那一步的对照物；`False` = 是 choose 类但**动作不符**；
    `None` = 不是桥步骤 / 不是 choose 类（调用方照旧）。"""
    if not is_bridge_step(meta): return None
    if o['verb'] not in CHOOSE_VERBS: return None
    acts = BRIDGE_ACT[meta['orig_act']]
    if o['act'] in acts: return True
    if not o['act'] and ocard is not None:
        want = BRIDGE_SIBLING.get(meta['orig_act'], set())
        return any(x is not o and x['verb'] in want for x in ocard['ops'])
    return False


# ================================================ 「打谁 / 打多少 / 什么触发」的机器化（新列 12–17）
# 枚举名表（`TargetCriteria@…targetCriteria` 的字段值）—— 与 `ANAME`/`TNAME` 同一个词典文件，
# 因此不会出现「仪器一套名字、探针一套名字」。
TCNAME = collections.defaultdict(dict)
for _r in rows[1:]:
    if _r[0].startswith('TargetCriteria@') and _r[0].endswith('targetCriteria'):
        try: TCNAME[_r[1]][int(_r[2])] = _r[3]
        except ValueError: pass


def _enum_name(table, v):
    try: return table.get(int(v), str(v))
    except (TypeError, ValueError): return str(v)


def fmt_orig_target(tc):
    """原版 `targetCriteria` → 一行「打谁」。空 = 原版没写目标块（`targetsAffected=0` 且无过滤器）。"""
    if not tc: return ''
    parts = []
    ta = tc.get('targetsAffected', 0)
    parts.append('scope=%s(%s)' % (_enum_name(TCNAME.get('targetsAffected', {}), ta), ta))
    side = []
    if tc.get('friendlyUnits'): side.append('own')
    if tc.get('enemyUnits'): side.append('enemy')
    if side: parts.append('side=' + '+'.join(side))
    kinds = [lbl for key, lbl in (('includeMinions', 'unit'), ('includeHero', 'hero'),
                                  ('includeRemnants', 'remnant'), ('includeSelf', 'self'),
                                  ('includeSpells', 'spell')) if tc.get(key)]
    if kinds: parts.append('kinds=' + '+'.join(kinds))
    if tc.get('maxTargets'): parts.append('max=%s' % tc['maxTargets'])
    for k in ('keywordFilter', 'traitsFilter', 'chosenArmy', 'spellType', 'conditionsFilter'):
        v = tc.get(k)
        if v: parts.append('%s=%s(%s)' % (k, _enum_name(TCNAME.get(k, {}), v), v))
    for k in ('excludeTrait', 'excludeCondition', 'excludeRace'):
        if tc.get(k): parts.append(k + '=1')
    fsc = tc.get('filterSpecificCards') or []
    if fsc: parts.append('specific=%d' % len(fsc))
    if tc.get('selectFromChooseResolution'): parts.append('fromChoose')
    return ' '.join(parts)


def fmt_orig_amount(meta):
    """原版这一支的「打多少」（伤害/治疗/属性增减/费用/资源/召唤数）。空 = 这一支不是数值类。"""
    if not meta: return ''
    parts = []
    if 'dmg' in meta: parts.append('dmg=%s' % meta['dmg'])
    if 'dmg_span' in meta: parts.append('dmg=%s-%s' % meta['dmg_span'])
    if 'heal' in meta: parts.append('heal=%s' % meta['heal'])
    if 'heal_span' in meta: parts.append('heal=%s-%s' % meta['heal_span'])
    nums = meta.get('nums') or []
    if nums: parts.append('buff=' + ','.join('%s%+d' % (k, v) for k, v in nums))
    if 'mana' in meta: parts.append('mana=%s' % meta['mana'])
    if meta.get('summon_n'):
        parts.append('summon=%s%s' % (meta['summon_n'], '(enemy)' if meta.get('summon_enemy') else ''))
    if meta.get('bt') in (3, 4, 90) and meta.get('trait'):
        parts.append('trait=%s' % meta['trait'])
    return ' '.join(parts)


def fmt_our_target(o):
    """我们这条 op 的 `{T:…}` 目标块 → 一行「打谁」（只挑可读的几格）。"""
    if not o: return ''
    t = o.get('tgt') or {}
    if not t: return ''
    keys = ['Side', 'Kind', 'Count', 'SubtypeFilter', 'KeywordFilter', 'NameFilter',
            'Adjacent', 'Anchor', 'Random', 'Auto', 'Each', 'Deployed', 'AttackedBySelf']
    seg = []
    for k in keys:
        v = t.get(k)
        if v in (None, '', '-'): continue
        if k in ('Random', 'Auto', 'Each', 'Deployed', 'AttackedBySelf') and v == '0': continue
        seg.append('%s=%s' % (k, v))
    return ' '.join(seg)


def fmt_our_amount(o):
    """我们这条 op 的「打多少」：`amount`（含 `{AM:}` 上界）+ 载荷里的属性增减。"""
    if not o: return ''
    lo, hi = our_span(o)
    s = 'amount=%s' % span_txt((lo, hi))
    nums = our_nums(o.get('payload') or '')
    if nums: s += ' buff=' + ','.join('%s%+d' % (k, v) for k, v in nums)
    return s


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
# 🔴 2026-10-19 波 5（`A1986`）：被**选区桥**改判的行，逐条出声（⛔ 改判不许无声）
bridge_flip = []
bridge_rows = []
# 🔴 2026-10-19 第五十五会话 `E8b`：被**三条对齐规则**改判的行（逐条出声 + 文件尾按子规则汇总）
e8b_flips = []
# 🔴 `E2`：与 `out` **等长**的「我们面·光环」列（按**卡**成批补，见下面 `row_aura.extend`）
row_aura = []
# 🔴 2026-10-19 波 5：与 `out` **等长**的「打谁 / 打多少 / 什么触发」六列（追加在末尾）。
#   与 `row_aura` 用**同一个收尾守卫**（见落盘前那段）：长度不等 = 有 `emit` 点漏了 ⇒ 出声。
row_extra = []


def emit(vals, trig, meta, o, ocid):
    """`out.append` 的**唯一口**（波 5 起）：同时产出第 12–17 列。
    ⚠️ 新加行时**必须走这里**（`row_extra` 与 `out` 等长的守卫会抓漏，但别故意去踩）。"""
    out.append(vals)
    _m = meta or {}
    row_extra.append([
        trig or '',
        fmt_orig_target(_m.get('tc_raw')),
        fmt_orig_amount(_m),
        fmt_our_target(o),
        fmt_our_amount(o),
        ocid or '',
    ])
CARRY_VERB = ('choosecard', 'chooseone', 'chooseeffect', 'lowercost', 'costmore', 'costwhen',
              'costifcontrol', 'paidmod', 'repeat', 'noeffect', 'extratrigger', 'double',
              'sethealth', 'become', 'ferocitystay') + (
              # 🆕 `E8b`：`spendspirit` = **付费机制**的解析产物（原版把它编进
              # `targetCriteria`/`furtherMultiplier`），不是一条独立效果 ——
              # 实测 `ASH52 Hosts of the Dead`（deploy + spendspirit + deploy(perSpent) 覆盖原版 2 步）。
              ('spendspirit',) if E8B_ON else ())

# 🔴 2026-10-19 波 5：`A1978` 的另一半 —— `hasActiveAbility` 为 `None`（旧快照 3 张）时，
#   下面那句 `c.get(...)` 的真假值判会让它**静默按 0 走**。这里**出声计数**（⛔ 不改判定：
#   「按 0 走」是旧口径，当前 build 真值 1,351 张全是 0/1，本计数恒 0；留着是为了让旧快照
#   再被指过来时**看得见**）。
haa_none = 0
skipped_not_pool = 0
TRUTH_TOTAL = len(truth)

for c in truth:
    cid = c.get('uniqueId') or ''
    # 🔴 2026-10-19 波 5：**对账单位 = 池内卡**。当前 build 真值 1,351 张里有 224 张不在我们的池
    #   （未来卡 / 不在本 build 卡池的 SO）—— 它们不是「我们的缺陷」，混进表就是 224 行噪声。
    #   `--all-truth` 可放开（放开后这批走 `卡不在池` 那一支）。
    if not ALL_TRUTH and cid not in POOLIDS:
        skipped_not_pool += 1
        continue
    if c.get('hasActiveAbility') is None:
        haa_none += 1
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
    # 🆕 `E8b`：`used` 之外的**分支级**去重（同一个 choose op 的不同分支各配一条原版 step）
    branch_used = set()
    # 🆕 `E8b`：**原版整卡**用到的族集合（供「别把别的 step 还要用的 op 吃掉」这条判据；
    #   判据只算这一次，见 `e8b_alignment` 的 ③·f / ③·k）。
    _orig_fams = set()
    for _ab0 in abilities:
        for _lg0 in _ab0[2]:
            _ov0, _m0, _d0 = orig_value(_lg0)
            _orig_fams |= eq_set(OFAM.get(_lg0.get('chosenAbility'), 'other'), _m0)
    _r0 = len(out)          # `E2` 列：本卡产出的行从这儿开始（收尾时整段补同一个光环格）

    for abkey, trig, logics in abilities:
        tname = TNAME.get(trig, str(trig))
        # `head` = 这个 trigger 在卡面正文里的前缀（`Strike:` / `Pray:` …）。⚠️ 账 `A1937`：**只对真枚举
        # trigger 取**（`trig in TNAME`）—— `ACT` 行用的是非枚举标签，`head` 一律 None，
        # 于是下面那条「来源加分」启发式**结构上不可能**给它加分（旧写法会给 `ACT` 行一个
        # 假的 `head='talent'`，即给「`src` 以 `talent` 开头」的 op 白送 3 分）。
        head = T2H.get(tname) if trig in TNAME else None
        # 🔴 2026-10-19 波 5：**多分支卡**（同一 ability 里 N 条 abilityLogic 各给一种 buff，
        #   例 `Flickerjump` = 手里剑/flying/flank 三支）—— 原版顺序先到先得会把**精确匹配**的那一支
        #   让给前一支（实测 `67ad…`：l2 的 `flank` 与我们唯一那条 `give flank` 完全对上，
        #   却被 l0 的 `flying` 先吃掉 ⇒ 假「取值不同」）。⇒ 排序键加一档：**有精确匹配的步骤先走**。
        #   ⚠️ 只改**遍历顺序**，不改任何判定口径；`follow` 仍排最后。
        def _step_exact(lg):
            if ocard is None: return False
            _v = lg.get('chosenAbility'); _fam = OFAM.get(_v, 'other')
            _oval, _meta, _dyn = orig_value(lg)
            for _o in ocard['ops']:
                if (set(_o.get('fams') or {_o['fam']}) & eq_set(_fam, _meta)) and compare(_fam, _meta, _o)[0]:
                    return True
            return False
        order = sorted(range(len(logics)),
                       key=lambda k: (1 if OFAM.get(logics[k].get('chosenAbility')) == 'follow' else 0,
                                      0 if _step_exact(logics[k]) else 1, k))
        for li in order:
            lg = logics[li]
            v = lg.get('chosenAbility')
            oname = ANAME.get(v, 'v%s' % v)
            fam = OFAM.get(v, 'other')
            oval, meta, dyn = orig_value(lg)
            row = [cid, abkey, str(li), oname, oval]
            key = (oname, oval)

            if ocard is None:
                if route == 'notext':
                    # 🔴 2026-10-19 波 5：**在池、但 `desc` 为空**（`Scan.cs:138` 跳过）——
                    #   与「卡不在池」分开说。这 6 张已由 `A1941` 查实「原版本身没有效果文本」
                    #   （判据 → `Scan.cs:140-150` 的逐张注释）⇒ 不是我们的缺口，但**别混成没有这张卡**。
                    emit(row + ['', '', '❓查不清', '卡无正文',
                                '这张卡在池内、但 `desc` 为空 ⇒ `scan` 没有它的 ops 行（`A1941` 已查实'
                                '原版本身没有效果文本）；trig=' + tname], tname, meta, None, cid)
                    stat['❓查不清'] += 1; sub['卡无正文'] += 1
                    matrix[(oname, '❓查不清')] += 1
                else:
                    emit(row + ['', '', '❓查不清', '卡不在池',
                                '卡按 id 与唯一卡名都不在 d:/4 卡池；trig=' + tname], tname, meta, None, cid)
                    stat['❓查不清'] += 1; sub['卡不在池'] += 1
                    matrix[(oname, '❓查不清')] += 1; percard[(cid, oname)] += 1
                continue
            if key in seen_step:
                emit(row + [seen_step[key], '', '⚠️表示不同·语义同', '原版重复N次',
                            '同一张卡上同一条 (类型,取值) 步骤重复出现 —— 原版写成 N 条独立 ability，'
                            '我们只有一条 op（次数由数量字段承载）· trig=' + tname], tname, meta, None, ocid)
                stat['⚠️表示不同·语义同'] += 1; sub['原版重复N次'] += 1
                matrix[(oname, '⚠️表示不同·语义同')] += 1
                continue

            sec = [x for x in (meta.get('sec') or []) if x != fam]
            secset = set()
            for x in sec: secset |= eq_set(x, meta)
            best = None
            for i, o in enumerate(ocard['ops']):
                if i in used: continue
                prim = bool(set(o.get('fams') or {o['fam']}) & eq_set(fam, meta))
                # 🔴 `A1986` 选区桥：原版「选区 + 造/召回/…」那一步的对照物是我们的 choose 类 op。
                #   `False` = 是 choose 类但**动作不符** ⇒ 它不是这一支的对照物（别拿它凑数）；
                #   `None` = 不是桥步骤 / 不是 choose 类 ⇒ 照旧按家族表判。
                br = bridge_match(meta, o, ocard)
                if br is False: continue
                if br is None and not prim and not (set(o.get('fams') or {o['fam']}) & secset): continue
                sc = 30 if prim else 0
                if br: sc = max(sc, 28)
                # ⚠️ 这是**我们的启发式、不是原版规则**：op 的 SRC 前缀与 trigger 名对得上 ⇒ +3，
                #    作用是同一张卡有多条触发时，优先挑中「从这个 trigger 那句正文解析出来的」op。
                #    账 `A1937` 只收窄了它的**适用范围**（非枚举的 `ACT` 行拿不到 `head`），
                #    没有动它的打分本身 —— 真枚举行照旧（`Strike`/`Pray`/`Rally`… 那批）。
                if head and o['src'].lower().startswith(head): sc += 3
                if prim and compare(fam, meta, o)[0]: sc += 1
                if best is None or sc > best[0]: best = (sc, i, o, prim, br)

            if best is not None:
                sc, i, o, prim, br = best
                if not o['env']: used[i] = (fam, oname)
                oside = op_side(o)
                same, why = compare(fam, meta, o)
                seen_step[key] = oside
                if not same and not br:
                    # 🆕 `E8b`：**已经配到了 op、但值不等** 的那两格（`取值不同`/`条数不同`）也走
                    #   规则③的取值编码两支（拆分求和 / 卡面为准 / 条件变体）。判据与兜底路共用
                    #   `_e8b_value_rules()`（⛔ 不另写一份）。`used` 要**去掉当前这条**，
                    #   否则「条件变体」会拿自己证明自己是「第二条」。
                    # ⚠️ `hasAlternativeAbility` 的卡**不走这里**：原版按运行期棋盘二选一，
                    #   静态仪器判不了（既有 `❓查不清/替代分支` 那一格必须保留，⛔ 别抢它的判定）。
                    _used_prev = {k: v for k, v in used.items() if k != i}
                    _vr = (None if (c.get('hasAlternativeAbility') or not E8B_ON) else
                           _e8b_value_rules(fam, meta, ocard['ops'], _used_prev,
                                            _e8b_card_text(ocard)))
                    if _vr is not None:
                        _vj, _vs, _vn, _vo = _vr
                        _vo = _vo or o
                        emit(row + [op_side(_vo), _vo['src'][:100], _vj, _vs,
                                    _vn + ' · trig=' + tname], tname, meta, _vo, ocid)
                        stat[_vj] += 1; sub[_vs] += 1
                        matrix[(oname, _vj)] += 1
                        e8b_flips.append((cid, c.get('cardName') or '', oname, tname, _vs))
                        continue
                    # 🔴 2026-10-19 波 5：**先别急着判红** —— 原版可能把同一条效果写成**两步**
                    #   （实测 `UM64`：`followAfterResolution` 与 `FollowUpResolution` 两步都给
                    #   `+1 ranged`，我们只有一条 `give +1 ranged`，第一步用掉后第二步只能抓到
                    #   另一条同族但数值不符的 op ⇒ 假「取值不同」）。若**已用掉**的某条同族 op
                    #   数值与这一步一致 ⇒ 这是「原版拆条」，不是差异。
                    _m2 = next((ocard['ops'][i] for i in used
                                if (set(ocard['ops'][i].get('fams') or {ocard['ops'][i]['fam']}) & eq_set(fam, meta))
                                and compare(fam, meta, ocard['ops'][i])[0]), None)
                    if _m2 is not None:
                        _os2 = op_side(_m2)
                        emit(row + [_os2, _m2['src'][:100], '⚠️表示不同·语义同', '拆条差异',
                                    '原版把这一效果写成多条 abilityLogic（本步与已用掉的那条同值：%s）；'
                                    '我们只有一条 op · trig=%s' % (_os2, tname)], tname, meta, _m2, ocid)
                        stat['⚠️表示不同·语义同'] += 1; sub['拆条差异'] += 1
                        matrix[(oname, '⚠️表示不同·语义同')] += 1
                        continue
                if br:
                    # 🔴 `A1986` 选区桥命中：原版 = `chosenAbility + selectFromChooseResolution=1`
                    #   （先挑一张、再对它做动作）；我们 = 一条 `choose*` op，**动作已逐条比过**
                    #   （`BRIDGE_ACT`）。⇒ 语义同、编码不同。
                    _acts = sorted(BRIDGE_ACT.get(meta.get('orig_act'), set()))
                    emit(row + [oside, o['src'][:100], '⚠️表示不同·语义同', '选区桥',
                                '原版：`%s(%s)` + `targetCriteria.selectFromChooseResolution=1`'
                                '（从候选里挑一张再「%s」）↔ 我们：`%s(%s)` —— 动作相符（允许集 %s）· trig=%s'
                                % (oname, meta.get('orig_act'), '/'.join(_acts), o['verb'], o['act'],
                                   '/'.join(_acts), tname)], tname, meta, o, ocid)
                    stat['⚠️表示不同·语义同'] += 1; sub['选区桥'] += 1
                    matrix[(oname, '⚠️表示不同·语义同')] += 1
                    bridge_flip.append('%s %-22s %-18s %s ← 我们 %s(%s)'
                                       % (cid, oname, tname, oval[:40], o['verb'], o['act']))
                    bridge_rows.append([cid, c.get('cardName') or '', tname,
                                        '%s(%s)' % (oname, meta.get('orig_act')),
                                        '; '.join(n for _, n in (meta.get('summon_ids') or [])) or '（无 unitsToSummon）',
                                        '%s(%s) from=%s what=%s' % (o['verb'], o['act'] or '空',
                                                                    o['src_kind'], o['what']),
                                        '⚠️表示不同·语义同',
                                        '选区桥（动作相符）· 原版取值=' + oval])
                    continue
                if not prim:
                    emit(row + [oside, o['src'][:100], '⚠️表示不同·语义同', '原版一条含多效果',
                                '原版这一条 abilityLogic 同时带多个 criteria 块（%s），我们拆成多条 op'
                                '（本条对上的是 %s）· trig=%s' % ('+'.join([fam] + sec), o['verb'], tname)],
                         tname, meta, o, ocid)
                    stat['⚠️表示不同·语义同'] += 1; sub['原版一条含多效果'] += 1
                    matrix[(oname, '⚠️表示不同·语义同')] += 1
                    continue
                if o['env']:
                    emit(row + [oside, o['src'][:100], '⚠️表示不同·语义同', '粒度不同',
                                '我们把它收在一条「信封」op（%s，payload 即原句文本）里，'
                                '与引擎内真实登记粒度不同；原版是独立一条 abilityLogic · trig=%s'
                                % (o['verb'], tname)], tname, meta, o, ocid)
                    stat['⚠️表示不同·语义同'] += 1; sub['粒度不同'] += 1
                    matrix[(oname, '⚠️表示不同·语义同')] += 1
                    continue
                if same:
                    judge, s2 = '✅一致', ''
                else:
                    # 🔴 2026-10-19 波 5（`A1947` 的读数落地）：`hasAlternativeAbility == 1` 的卡，
                    #   原版按**运行期棋盘**在 `activeAbility` / `alternativeAbility` 两支里挑
                    #   （`RawCardScript__GetActiveAbility.c:12-20`）。本仪器只读前一支 ⇒ 数值不同时
                    #   **不能判成我们的缺陷** —— 实测全池 1 张（`SW21 Wulfen Pack Leader`：
                    #   前一支 = `dmg5`「你没别的部队时」，我们的卡面 = `3, or 5 …` 条件式）。
                    if abkey == 'ACT' and c.get('hasAlternativeAbility'):
                        judge, s2 = '❓查不清', '替代分支'
                        why = ('（原版这张卡 `hasAlternativeAbility=1`：`activeAbility` 与 '
                               '`alternativeAbility` 是**二选一**，按运行期棋盘挑；静态对账只读了前一支 '
                               '⇒ 这一行的数值差**不能**判成我们的缺陷）· ' + why)
                        sub['替代分支'] += 1
                        percard[(cid, oname)] += 1
                        stat[judge] += 1; matrix[(oname, judge)] += 1
                        emit(row + [oside, o['src'][:100], judge, s2,
                                    why + ' · trig=' + tname], tname, meta, o, ocid)
                        continue
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
                emit(row + [oside, o['src'][:100], judge, s2,
                            (why + ' · ' if why else '') + 'trig=' + tname], tname, meta, o, ocid)
                continue

            merged = None
            for i in used:
                o = ocard['ops'][i]
                if (set(o.get('fams') or {o['fam']}) & eq_set(fam, meta)) and compare(fam, meta, o)[0]:
                    merged = o; break
            if merged is not None:
                oside = op_side(merged)
                seen_step[key] = oside
                emit(row + [oside, merged['src'][:100], '⚠️表示不同·语义同', '拆条差异',
                            '原版把这一效果拆成多条 abilityLogic（或拆在兄弟 ability 里），'
                            '我们解析成一条 op（%s）；数值一致 · trig=%s' % (oside, tname)],
                     tname, meta, merged, ocid)
                stat['⚠️表示不同·语义同'] += 1; sub['拆条差异'] += 1
                matrix[(oname, '⚠️表示不同·语义同')] += 1
                continue
            if is_bridge_step(meta):
                # 🔴 `A1986` 的第二半（实测 `UM114`）：原版把「制造 2 份复制」写成**两条**
                #   `createCardInHand` 步骤，我们**一条** `choosecard(copies)` 就表达了（数量在动作里）
                #   ⇒ 第二条原版步骤走到这里时那个 choose op 已被 `used` 占掉。这不是「动作不符」，
                #   是**原版重复 N 次**（与上面 `seen_step` 同一支口径，只是键不同）。
                _br_used = [ocard['ops'][i] for i in used
                            if i < len(ocard['ops']) and bridge_match(meta, ocard['ops'][i], ocard)]
                if _br_used:
                    _u = _br_used[0]
                    emit(row + [op_side(_u), _u['src'][:100], '⚠️表示不同·语义同', '原版重复N次',
                                '原版把同一件事拆成多条 abilityLogic（本步是第 N 条）；我们的 `%s(%s)` '
                                '一条就表达了（数量/次数在动作里）· trig=%s'
                                % (_u['verb'], _u['act'], tname)], tname, meta, _u, ocid)
                    stat['⚠️表示不同·语义同'] += 1; sub['原版重复N次'] += 1
                    matrix[(oname, '⚠️表示不同·语义同')] += 1
                    continue
            # 🆕 `E8b`：**三条对齐规则**（choose 分支 / give-gain 族 / 别层承载）。
            #   ⚠️ 只在**既有匹配全部落空之后**才问它 ⇒ 已经 ✅/⚠️ 配上的行**一个字节都不动**。
            _al = e8b_alignment(cid, ocid, tname, trig, fam, meta, oval, ocard, used, branch_used, _orig_fams)
            if _al is not None:
                _aj, _as, _an, _ao = _al
                emit(row + [op_side(_ao) if _ao else '', (_ao['src'][:100] if _ao else ''),
                            _aj, _as, _an], tname, meta, _ao, ocid)
                stat[_aj] += 1; sub[_as] += 1
                matrix[(oname, _aj)] += 1
                e8b_flips.append((cid, c.get('cardName') or '', oname, tname, _as))
                continue
            if fam == 'vfxonly':
                emit(row + ['', '', '⚠️表示不同·语义同', '纯表现',
                            '原版这一支只播特效（无棋局效果），我们的 op 表不为它出 op · trig=' + tname],
                     tname, meta, None, ocid)
                stat['⚠️表示不同·语义同'] += 1; sub['纯表现'] += 1
                matrix[(oname, '⚠️表示不同·语义同')] += 1
                continue
            if fam == 'uionly':
                emit(row + ['', '', '⚠️表示不同·语义同', '纯 UI',
                            '原版这一支只开一个看牌窗（无棋局效果）· trig=' + tname], tname, meta, None, ocid)
                stat['⚠️表示不同·语义同'] += 1; sub['纯 UI'] += 1
                matrix[(oname, '⚠️表示不同·语义同')] += 1
                continue
            if fam in ('remove_ench', 'setref'):
                emit(row + ['', '', '❓查不清', '判据不足',
                            '原版这一支改的是 AbilityLogic 自己的附魔/引用位（记账），'
                            '我们这边没有可观测的对应物 · trig=' + tname], tname, meta, None, ocid)
                stat['❓查不清'] += 1; sub['判据不足'] += 1
                matrix[(oname, '❓查不清')] += 1; percard[(cid, oname)] += 1
                continue
            anyfam = [o for o in ocard['ops'] if set(o.get('fams') or {o['fam']}) & eq_set(fam, meta)]
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
            elif is_bridge_step(meta) and any(o['verb'] in CHOOSE_VERBS for o in ocard['ops']):
                # 🔴 2026-10-19 波 5：**有 choose op、但动作不符**（`bridge_match` 判过 False）——
                #   这是**真差异**，而且肉眼很难从「我们缺」里看出来，单列一个子类点名。
                _acts_here = sorted(set('%s(%s)' % (o['verb'], o['act'] or '空')
                                        for o in ocard['ops'] if o['verb'] in CHOOSE_VERBS))
                s2 = '选区动作不符'
                why = '原版这一步 = `%s` + `selectFromChooseResolution=1`（允许动作 %s），' \
                      '我们这边的 choose 类 op 动作是 %s' % (
                          oname, '/'.join(sorted(BRIDGE_ACT.get(meta.get('orig_act'), set()))),
                          ' / '.join(_acts_here))
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
            row_extra.append([tname, fmt_orig_target(meta.get('tc_raw')), fmt_orig_amount(meta),
                              '', '', ocid])
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
                _ov2, _m2, _d2 = orig_value(_lg)
                ofams |= eq_set(OFAM.get(_lg.get('chosenAbility'), 'other'), _m2)
                # 🔴 2026-10-19 波 5：**一条 abilityLogic 可以同时带多个 criteria 块**
                #   （`summonUnit` + `buffEffect{addTrait flank}` = 「部署它并给侧翼」）。
                #   旧口径的 `ofams` 只收**主块**那一族 ⇒ 我们的 `give flank` 这条 op 找不到归属、
                #   被记成「我们多」（实测 `AM73` / `AM76` / `GOF43` / `TAU75` 等一整族）。
                #   ⇒ 把每一步的**次要块**（`meta['sec']`）也并进来 —— 这与上面「原版一条含多效果」
                #   是同一件事的两面。
                for _x in (_m2.get('sec') or []):
                    ofams |= eq_set(_x, _m2)
        for i, o in enumerate(ocard['ops']):
            if i in used or o['env']: continue
            oside = '%s/%s%s' % (o['verb'], o['amount'], ('|' + o['payload']) if o['payload'] else '')
            if (set(o.get('fams') or {o['fam']}) & ofams) or o['verb'] in CARRY_VERB:
                emit([cid, '-', '-', '', '', oside, o['src'][:100],
                      '⚠️表示不同·语义同', '承载不同',
                      '我们这一条 op 是从同一句卡面正文解析出来的机制产物（选牌/改费/重复…）—— '
                      '原版把这件事编码在同一 abilityLogic 的 targetCriteria / cardEffect 字段里，'
                      '没有独立的 chosenAbility 步骤与之对齐'], '', None, o, ocid)
                stat['⚠️表示不同·语义同'] += 1; sub['承载不同'] += 1
                matrix[('(无独立对应步骤)', '⚠️表示不同·语义同')] += 1
            else:
                emit([cid, '-', '-', '', '', oside, o['src'][:100],
                      '🔴真差异', '我们多',
                      '我们这张卡有这条 op，原版整卡 %d 条步骤里没有同族 counterpart'
                      % sum(len(at[2]) for at in abilities)], '', None, o, ocid)
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
# 🔴 2026-10-19 波 5：同一道守卫，管第 12–17 列（`emit` 是唯一口，漏了就在这里出声）。
if len(row_extra) != len(out):
    print('!! 「打谁/打多少/触发」六列与行数不齐（%d vs %d）⇒ 有 `out.append` 没走 `emit`。'
          '缺的按空串补齐（⛔ 错位告警，不是「这几列没有值」）。' % (len(row_extra), len(out)))
    row_extra.extend([['', '', '', '', '', ''] for _ in range(len(out) - len(row_extra))])
lines = ['\t'.join(['卡id', 'ability序', 'logic序', '原版(类型枚举名)', '原版取值',
                    '我们(类型)', '我们取值', '判定', '子类', '备注', '我们面 · 光环',
                    # 🔴 2026-10-19 波 5 追加（⛔ 前 11 列下标一字不动）
                    '触发', '原版·打谁', '原版·多少', '我们·打谁', '我们·多少', '我们卡id'])]
for i, r in enumerate(out):
    r = [str(x if x is not None else '').replace('\t', ' ').replace('\n', ' ') for x in r]
    ex = [str(x if x is not None else '').replace('\t', ' ').replace('\n', ' ') for x in row_extra[i]]
    lines.append('\t'.join(r + [row_aura[i].replace('\t', ' ').replace('\n', ' ')] + ex))
io.open(OUT, 'wb').write(('\r\n'.join(lines) + '\r\n').encode('utf-8'))
print('wrote', OUT, 'rows=', len(out))
print('--- 输入现读（2026-10-19 波 5 追加：真值源换了，必须让「吃的是哪一份」看得见）---')
print('   真值        :', _prov(TRUTH), '· 形态=%s · 卡数=%d' % (TRUTH_SHAPE, TRUTH_TOTAL))
print('   本地化显示名:', LOC_SHAPE)
print('   trait 名表  :', ('DefinedTrait.cs 全量 %d 个值' % len(TRNAME)) if TRAIT_SRC_OK
      else ('⚠️ 没读到 DefinedTrait.cs ⇒ 退回词典 %d 个值（`huntMark/sentry/pack` 那一批会变无名值）' % len(TRNAME)),
      '· 印刷名映射:', ('`Card_Trait/*` %d 个' % len(TRAIT_PRINTED)) if TRAIT_LOC_OK
      else '⚠️ 没读到官方本地化 ⇒ `frenzied→Destroyer` 那一层缺失（SAU44 会留红）')
if skipped_not_pool:
    print('   ⚠️ 真值里有 %d 张**不在我们的池**（真值 %d 张；纳入对账的池内卡 = %d）⇒ 默认**跳过**'
          '（`--all-truth` 可放开；它们不是我们的缺陷）。'
          % (skipped_not_pool, TRUTH_TOTAL, TRUTH_TOTAL - skipped_not_pool))
if haa_none:
    print('   🔴 `hasActiveAbility` 为 `None` 的卡 = %d 张（旧口径会**静默按 0 走**；`A1978①`）' % haa_none)
print('stat =', dict(stat))
print('sub  =', dict(sub))
print('matchroute =', dict(matchroute))
print('matrix:')
for (k, j), n in sorted(matrix.items(), key=lambda x: -x[1])[:12]:
    print('   %-30s %-18s %d' % (k, j, n))
print('--- `A1986` 选区桥（`create/recall/…` + `selectFromChooseResolution=1` ⇒ `choose*` op）---')
print('   改判合计 %d 条（逐条见下；动作集见 `BRIDGE_ACT`）：' % len(bridge_flip))
for x in bridge_flip: print('      ·', x)
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

# 🔴 2026-10-19 第五十五会话 `E8b`：三条对齐规则的改判读数 —— **按子规则给行数与卡名单**（⛔ 改判不许无声）。
print('--- `E8b` 三条对齐规则（choose 分支 / give-gain 族 / 别层承载）---')
print('   改判合计 %d 条 · 涉及 %d 张卡 · 按子规则：' % (
    len(e8b_flips), len(set(x[0] for x in e8b_flips))))
_bysub = collections.OrderedDict()
for _cid, _nm, _on, _tg, _sb in e8b_flips:
    _bysub.setdefault(_sb, []).append((_cid, _nm, _on, _tg))
for _sb, _lst in _bysub.items():
    _cards = []
    for _cid, _nm, _on, _tg in _lst:
        _tag = '%s（%s · %s）' % (_cid, _nm or '?', _tg)
        if _tag not in _cards: _cards.append(_tag)
    print('     · %-16s %3d 条 · 卡：%s' % (_sb, len(_lst), '、'.join(_cards)))
print('   ⛔ 反例集（本规则**不吞**的真缺口，仍留红）：`GOF12` 的 health 三条（γ）'
      ' · `36892cd…` Bitter Blows / `9dcbe3d0…` Mekaniak（判据未定）· `GOF38` 两条（SO 与卡面冲突，按卡面）'
# 🔴 **2026-10-23（代理 B2 · `T07` 四红定死）**：上面那四条的判据这轮**逐条过了机器码 / 两代快照 / 官方本地化**，
#   **结论 = 判定一律不变（仍留红），但「定性」和「缺什么」写死在这里** —— 下一个会话别再从零查一遍。
#     ① `GOF38 Proper Killy` a0.0（**两条红是同一件事的两面**：原版「我们缺」+ 我们「多」一条 `gain`）
#        · 原版：`chosenAbility=30 buffOthers` · `buffEffect{buffType=3 addTrait, trait=220 bloodThirst,
#          meleeAttackChange=2, untilEndOfTurn=1}` · `tgt{targetsAffected=10 allFiltered, friendlyUnits=1,
#          includeHero=1, includeMinions=0}` ⇒ 「**所有友方英雄** 本回合 +2 近战 且 得嗜血」。
#          `DefinedTrait.bloodThirst = 220`（`Assembly-CSharp/DefinedTrait.cs:21`）· `BuffType.addTrait = 3`
#          （`BuffType.cs`）· `TargetsAffected.allFiltered = 10` / `friendlyHero = 120`（`TargetsAffected.cs`）。
#        · 我方：`gain/0|blood thirst` 目标 `own|warlord`（scan 基线 `GOF38` 行）。
#        · 🔴 **冲突是真的，⛔ 不能当残留糊过去**：`meleeAttackChange` 与 `addTrait` 同块时**引擎确实会用**
#          —— 反证 = `Pariah Vanguard`「Give **+2 [Melee]** and Vanguard」= `bt3 trait=980 melee=2`、
#          `Thunderous Charge`「+2 attack, +2 ranged and Concussive」、`Slaanesh's Spawn`「+1 Attack and Flank」
#          （全池 26 条 `bt=3` 同时带 stats，其中 3 条卡面**明写**那些 stats）。
#        · 🔴 **但卡面/官方本地化都没有那个 +2**：`Card_Description/GOF38` = `((Your_warlord_gains_trait_x_this_turn,
#          [[bloodThirst]]))`（`数据/游戏数据/官方本地化_en+zh-CN.json`）· PnP 卡图同句。
#          **同文孪生卡 `Proper Hunt`（同一句卡面）的 SO 里 `stats = {}`、`ta=120(friendlyHero)`** ⇒
#          Proper Killy 的 `melee+2` 与 `ta=10` 是**只在这一张上多出来的**。
#        · ⇒ **原版内部冲突**（SO 多 +2 melee、目标写成 allFiltered）。**我们按卡面**（只给督军 bloodThirst）；
#          目标两侧等价（本游戏每方恰好 1 位英雄，allFiltered+includeHero+includeMinions=0 ≡ your warlord）。
#          ⛔ 不改实现；要改也应由主对话拍板「按 SO 补 +2」，⛔ 别为了变绿把这条桥掉。
#     ② `Bitter Blows` a0.1（`36892cd…`，原版「我们缺」）
#        · 原版 step0 = **`destroy(110)`** + `targetSelectMethod=lowestHealth(6)` + `tgt{allFiltered, enemyUnits=1,
#          includeMinions=1, maxTargets=1}` ⇒ **「摧毁生命值最低的敌方部队」= 卡面**（`AbilityEffect.destroy = 110`，
#          `Assembly-CSharp/AbilityEffect.cs:25`）⇒ **我们那条 `destroy … PickMost=-health` 是对的，step0 已 ✅**。
#        · 红的只是 **step1**（`buffOthers` 打/治/加血一个友方英雄）。它**逐字节等于 `Warmaster`
#          （`97e95944…`）的 step1**，**两代快照（2022 时代 + 当前 build）完全相同** ⇒ 模板残余。
#        · ⚠️ **没闭合的那半截**：`targetsAffected=90 targetIfMeetsCriteria` 在 `AbilityLogic__GetTargets.c:747-760`
#          的 `0x5a` 支只是把**上一步的目标列表** `AddRange` 进去；若后续**不**再按 criteria 过滤，原版会把它
#          作用在**上一步那个敌方部队**上（-0..2 血 +2 生命）。**那一跳（谁过滤、过滤几次）没读到** ⇒
#          维持「判据未定」；**缺什么** = `AbilityLogic.GetTargets` 的调用方（`AbilityLogic__PlayAbility.c`
#          对 `local_170` 的传递）与 `BoardAnalysis__MeetsCriteria` 在 90 那支上的调用点。
#     ③ `Mekaniak` a0.0（`9dcbe3d0…`，选区动作不符 = 原版「我们缺」/「我们多」两面）
#        · 原版：`chosenAbility=100 createCardInHand` + `selectFromChooseResolution=1` ·
#          `tgt{choiceResolution(180), friendlyUnits=1, includeSpells=1, keywordFilter=10(Vehicle), maxTargets=1}`
#          （`Keyword.Vehicle = 10`，`Assembly-CSharp/Keyword.cs`）· 两代快照逐字节相同。
#        · 我方：`chooseeffect(give)` 目标 `own|vehicle`，池 = `More Dakka`/`Ramshackle`/`Wreckin' Ball`
#          （`EffectResolver.ChooseEffectPools`；这三张的 PPtr 判据 → `资料/普查产出_1019/E11_报告.md` §二·2）。
#        · 🔴 **冲突点**：同形 `createCardInHand + choiceResolution(180)` 的卡全池 **25 张**，卡面**清一色**写
#          「… and **put it in your hand**」；**只有 Mekaniak 写「Give a Kustom Job of your choice to a
#          friendly Vehicle」**（卡面 + 官方 `Card_Description/9dcbe3d0…` 同句）。⇒ SO 说「造进手里」，
#          卡面说「直接给载具」，**两者可观测不同**（是否占手牌格 / 能否被弃 / 三张 Kustom Job 都是
#          `Ephemeral` 0 费、回合末会没）。
#        · ⇒ **原版内部冲突，按卡面**（现实现）；要改成「造牌进手」属于**行为改动 + `play` 结算指纹会变**，
#          由主对话拍板，⛔ 本仪器不替它翻绿。
      # 🔴 2026-10-20（W57 EN-3 · 铁律 5 就地订正）：原来这一行还列着 `SW38`（判据冲突 β）——
      #   不成立了：`our_layers_1019.jsonl` 重生后 SW38 有 `Armour 2 ← Your Warlord has Armour 2`
      #   光环行（`W57_引擎线_T07_对账_EN3.tsv` 里是 `⚠️表示不同·语义同 / 光环层承载`）。
      ' · ⚠️（`SW38` 已于 2026-10-20 随 layers 重生转绿，从本反例集移除）'
      # 🔴 2026-10-20（W58 E07）：原来这里把 `GOF56`/`TL49`/`TL50`/`ASH46`/`GOF41`（260）与
      #   `SAU11`（320）当反例集 —— **不再成立**：`E8B_WRAPPER_BETA` 已逐条建桥（双检：原版载荷形状
      #   + 我方承载 op）。桥的判据与重跑数字 → `资料/普查产出_1020/W58_引擎线_260_320桥.md`。
      ' · ⚠️（`GOF56`/`TL49`/`TL50`/`ASH46`/`GOF41`/`SAU11` 已于 2026-10-20 建 260/320 桥，从本反例集移除）')

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

# ================================================ 选区桥 TSV（`A1986` · 2026-10-19 波 5）
# 只列**被桥改判**的行（`⚠️`）—— ⛔ 不把「桥没救回来」的也塞进来：那些仍在主表里留红，
# 主表按下标就能筛（子类 = `我们缺` / `条数不同` / `选区动作不符`）。
if BRIDGE_OUT:
    os.makedirs(os.path.dirname(BRIDGE_OUT), exist_ok=True)
    _bl = ['\t'.join(['卡id', '卡名', '触发', '原版(类型枚举名)', '原版 unitsToSummon(显示名)',
                      '我们 choose op', '判定', '说明'])]
    for r in bridge_rows:
        _bl.append('\t'.join(str(x if x is not None else '').replace('\t', ' ').replace('\n', ' ')
                             for x in r))
    io.open(BRIDGE_OUT, 'wb').write(('\r\n'.join(_bl) + '\r\n').encode('utf-8'))
    print('wrote', BRIDGE_OUT, 'rows=', len(bridge_rows))

# ================================================ `A1978②` 天赋桥（2026-10-19 波 5）
# 🔴 **为什么单开一段**：原版把「天赋」编码成 `trigger=Talent(46)` + `chosenAbility=createCardInHand(100)`
#   + `summonCriteria.unitsToSummon=[PPtr]`；我们把它解成**一条 `create` op**（`EffectText.TryTalent`），
#   payload = 天赋卡名。两条路**不是同一个东西** —— 只按「都有 create」比会看起来一致。
#   这一段就是把**原版指向哪张卡**解出来，再与我们的 payload **逐名比**。
#   PPtr → `path_id` → 卡 → **显示名**（`loc_card_names.tsv`；SO 的 `cardName` 是内部名，
#   实测三处会假红：`Warmaster`↔`Chosen of the Four` 等 —— 见文件头 ③）。
# ⚠️ `A random …` 那种（`BL23`/`BL36` = 三张 Black Legion Psychic Power 随机一张）机器判不出类别，
#   如实标 `⚠️随机类别` 并列出候选名（⛔ 不猜它等于哪一张）。
def _norm_name(s):
    return re.sub(r'[^a-z0-9]', '', (s or '').lower())


talent_rows = []
for c in truth:
    cid = c.get('uniqueId') or ''
    if not ALL_TRUTH and cid not in POOLIDS:
        continue
    oc = our.get(cid)
    for ai, ab in enumerate(c.get('cardAbilities') or []):
        if ab.get('trigger') != 46:            # AbilityTrigger.Talent
            continue
        for li, lg in enumerate(ab.get('abilityLogic') or []):
            if lg.get('chosenAbility') != 100:  # AbilityEffect.createCardInHand
                continue
            names = resolve_summon_names(lg)
            if not names:
                continue
            creates = [o for o in (oc['ops'] if oc else []) if o['verb'] == 'create']
            payloads = [o['payload'] for o in creates]
            want = [_norm_name(n) for _, n in names if n and n != '?UNRESOLVED']
            hit = [p for p in payloads if any(_norm_name(p) == w for w in want)]
            if hit:
                verdict, why = '✅一致', 'payload 与 unitsToSummon 的**显示名**逐字相等'
            elif payloads and any('random' in (p or '').lower() for p in payloads):
                verdict, why = ('⚠️随机类别',
                                '我们的 payload 是「随机/类别」写法；原版列出 %d 张具体卡 ⇒ '
                                '机器判不了类别归属（候选见左列）' % len(names))
            elif payloads:
                verdict, why = '🔴真差异', 'payload 与 unitsToSummon 的名字**都对不上**'
            elif oc is None:
                verdict, why = '❓查不清', '我们侧没有这张卡的 scan 行（卡无正文）'
            else:
                verdict, why = '🔴我们缺', '我们没解析出 create op（desc 缺 `Talent:` 行或解析器没吃下）'
            talent_rows.append([cid, c.get('cardName') or '', 'a%d' % ai, str(li),
                                '; '.join('%s %s' % (u or '?', n) for u, n in names),
                                '; '.join(p for p in payloads) or '（无 create op）',
                                verdict, why])

if TALENT_OUT:
    os.makedirs(os.path.dirname(TALENT_OUT), exist_ok=True)
    _tl = ['\t'.join(['卡id', '卡名', 'ability序', 'logic序', '原版 unitsToSummon（卡id 显示名）',
                      '我们 create payload', '判定', '说明'])]
    for r in talent_rows:
        _tl.append('\t'.join(str(x if x is not None else '').replace('\t', ' ').replace('\n', ' ')
                             for x in r))
    io.open(TALENT_OUT, 'wb').write(('\r\n'.join(_tl) + '\r\n').encode('utf-8'))
    print('wrote', TALENT_OUT, 'rows=', len(talent_rows))

_tstat = collections.Counter(r[6] for r in talent_rows)
print('--- `A1978②` 天赋桥（`Talent(46)` + `createCardInHand(100)` + `unitsToSummon` PPtr → 显示名）---')
print('   步骤数 = %d · 判定 = %s' % (len(talent_rows), dict(_tstat)))
for r in talent_rows:
    if r[6] != '✅一致':
        print('      · %-8s %-26s 原版=%s 我们=%s [%s] %s'
              % (r[0], r[1][:26], r[4][:60], r[5][:40], r[6], r[7][:70]))
