#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_environment_conditions.py —— 抽「环境条件 SO」的值 → 数据层
================================================================================
为什么：
  `项目任务.md` §三 第 30 条【战场场景线】· 散件 A：「42 个『效果』环境 SO 的**数据一个都没进工程**
  …… ⇒ 4 环境的 `blendTime` / `ambientColor` / `fogDensity` **要另建数据层**」。
  「卡 → SO → prefab」那条**链**已经建好（`工具/gen_offensive_cards.py` 于 2026-09-29 解出
  `envSO_name` / `prefab_name` / `face_texture`）—— **本脚本补的是「SO 自己的值」**。

抽什么（逐 SO，字段照 `ScenarioEnvironmentConditionSO` 的签名桩）：
  blendTime · ambientColor · ambientBlend · fogColor · fogDensity ·
  scenarioObjects(GUID) · defaultScenarioObjectsState · filterOptions ·
  animationsToChange（条数/内容）

判据来源：
  · SO 资产：`d:/2/新解包资源/assets_full/bundle_*cardanims*_assets_all/MonoBehaviour/*.json`
    —— **按内容认**（有 `blendTime` + `scenarioObjects` 两键即是），**不按文件名**
    （帝皇之子那份默认 SO 叫 `Emperor's Children Default Condition`，名字里没有 `Enviro` ⇒ 名字匹配会漏）。
  · 字段名单：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/ScenarioEnvironmentConditionSO.cs`
  · 卡 → SO 的对应：`数据/游戏数据/offensive_cards.json`（由 gen_offensive_cards.py 生成）

产物（两份）：
  · `数据/游戏数据/environment_conditions.json` —— 正表（带 `_sources` 与 `_unresolved`）
  · `MyGame/Assets/Resources/EnvironmentConditions.json` —— **摊平版**（`JsonUtility` 读不了字典，
    与 `Resources/OffensiveCards.json` 同一套做法），运行时用
可重跑、幂等。

用法：PYTHONIOENCODING=utf-8 python 工具/gen_environment_conditions.py
"""
import io, os, re, json, glob, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
FULL = 'd:/2/新解包资源/assets_full'
CANON = os.path.join(ROOT, '数据', '游戏数据', 'environment_conditions.json')
FLAT = os.path.join(ROOT, 'MyGame', 'Assets', 'Resources', 'EnvironmentConditions.json')


def r6(x):
    try:
        return round(float(x), 6)
    except (TypeError, ValueError):
        return None


def col(c):
    if not isinstance(c, dict):
        return None
    return [r6(c.get('r')), r6(c.get('g')), r6(c.get('b')), r6(c.get('a'))]


def find_sos():
    """扫所有 cardanims 包，按内容认环境条件 SO。"""
    out = []
    for d in sorted(glob.glob(os.path.join(FULL, 'bundle_*cardanims*_assets_all', 'MonoBehaviour'))):
        bundle = os.path.basename(os.path.dirname(d))
        for f in sorted(glob.glob(os.path.join(d, '*.json'))):
            try:
                j = json.load(io.open(f, encoding='utf-8'))
            except Exception:
                continue
            if 'blendTime' in j and 'scenarioObjects' in j and 'ambientColor' in j:
                out.append((bundle, os.path.basename(f)[:-5], j))
    return out


def main():
    cards = json.load(io.open(os.path.join(ROOT, '数据', '游戏数据', 'offensive_cards.json'), encoding='utf-8'))
    armies = cards['armies']
    bundle2army = {a['cardanimsBundle']: name for name, a in armies.items()}

    # 卡 → SO 名（按 envSO_name）/ SO 名 → 卡槽
    so2cards = {}
    for name, a in armies.items():
        for c in a.get('cards') or []:
            if c.get('envSO_name'):
                so2cards.setdefault(c['envSO_name'], []).append(
                    {'army': name, 'slot': c.get('slot'), 'forgeLevel': c.get('forgeLevel'),
                     'cardName_candidate': c.get('cardName_candidate')})
    defaults = {a['defaultEnviromentalEffectVFX'].get('envSO_name'): name for name, a in armies.items()}

    rows = []
    for bundle, so_name, j in find_sos():
        army = bundle2army.get(bundle)
        kind = 'default' if so_name in defaults else 'effect'
        so = j.get('scenarioObjects') or {}
        rows.append({
            'so': so_name,
            'army': army,
            'armyBundle': bundle,
            'kind': kind,
            'blendTime': r6(j.get('blendTime')),
            'ambientColor': col(j.get('ambientColor')),
            'ambientBlend': r6(j.get('ambientBlend')),
            'fogColor': col(j.get('fogColor')),
            'fogDensity': r6(j.get('fogDensity')),
            'scenarioObjects_GUID': so.get('m_AssetGUID') or '',
            'defaultScenarioObjectsState': j.get('defaultScenarioObjectsState'),
            'filterCode': ((j.get('filterOptions') or {}).get('<FilterCode>k__BackingField') or ''),
            'filterEnabled': ((j.get('filterOptions') or {}).get('<isEnabled>k__BackingField') or 0),
            'animationsToChange': len(j.get('animationsToChange') or []),
            'prefab_name': None,       # 由 offensive_cards.json 回填
            'prefab_guid': None,
            'face_texture': None,
            'used_by_cards': so2cards.get(so_name, []),
        })

    # 回填 ①：按「卡引用的 SO 名」（数据都在 offensive_cards.json 里，本脚本不重解 GUID）
    by_name = {}
    for name, a in armies.items():
        for c in a.get('cards') or []:
            if c.get('envSO_name'):
                by_name[c['envSO_name']] = c
    for r in rows:
        c = by_name.get(r['so'])
        if c:
            r['prefab_name'] = c.get('prefab_name')
            r['prefab_guid'] = c.get('prefab_guid')
            r['face_texture'] = c.get('face_texture')

    # 回填 ②：按 scenarioObjects GUID 共享 —— 两个 SO 指向同一 GUID 时共用同一个 prefab。
    #   实例：`BL Void battle` 与 `BL Warp Storm` 同 GUID（原版自己这样），
    #   而 Void Battle **不被任何卡引用** ⇒ ① 填不到它 ⇒ 靠这一遍补上。
    guid2prefab = {}
    for r in rows:
        if r['scenarioObjects_GUID'] and r['prefab_name']:
            guid2prefab.setdefault(r['scenarioObjects_GUID'], r['prefab_name'])
    for r in rows:
        if not r['prefab_name'] and r['scenarioObjects_GUID'] in guid2prefab:
            r['prefab_name'] = guid2prefab[r['scenarioObjects_GUID']]
            r['prefab_guid'] = None
            r['prefab_note'] = '与同 GUID 的另一个 SO 共用（原版两个 SO 指同一个 prefab）'

    # 回填 ③：**prefab 到底在不在我们工程里** —— 归一化名字比对
    #   （原版名字与我们的文件名有空格/词序差异，例：`Orks Environmental Condition Night`
    #     vs 我们别的场是 `Environmental Condition <阵营> <效果>` ⇒ 必须归一化）
    #   🔴 这一条就是为了「`Orks Night` 那个 prefab 没导进工程」这类缺口**下次重跑时会自己报出来**。
    def norm(s):
        return re.sub(r'[^a-z0-9]', '', (s or '').lower())
    proj = {norm(os.path.basename(p)[:-len('.prefab')])
            for p in glob.glob(os.path.join(ROOT, 'MyGame', 'Assets', 'WarpforgeVFX', 'Prefabs', '*.prefab'))}
    for r in rows:
        if r['prefab_name']:
            r['prefab_in_project'] = norm(r['prefab_name']) in proj
        else:
            r['prefab_in_project'] = None

    # 统计与未解
    unresolved = {}
    no_prefab = [r['so'] for r in rows if r['kind'] == 'effect' and not r['prefab_name']]
    if no_prefab:
        unresolved['prefab_name'] = no_prefab
    not_in_proj = [{'so': r['so'], 'prefab': r['prefab_name']}
                   for r in rows if r['kind'] == 'effect' and r['prefab_name'] and not r['prefab_in_project']]
    if not_in_proj:
        unresolved['🔴 prefab 没进工程（要补导）'] = not_in_proj
    shared = [r['so'] for r in rows if r.get('prefab_note')]
    if shared:
        unresolved['共用同一 prefab（不是缺陷，原版如此）'] = shared

    doc = {
        '_schema': 'environment_conditions/v1 —— 环境条件 SO 的值（战场场景线 · 4 环境的数据层）。'
                   '由 d:/4/Unity/工具/gen_environment_conditions.py 生成，可重跑。',
        '_sources': {
            'SO 资产': 'assets_full/bundle_*cardanims*_assets_all/MonoBehaviour/*.json（按内容认：有 blendTime + scenarioObjects 两键）',
            '字段名单': 'd:/2/Warpforge_code/Scripts/Assembly-CSharp/ScenarioEnvironmentConditionSO.cs',
            '卡→SO→prefab': '数据/游戏数据/offensive_cards.json（工具/gen_offensive_cards.py 生成）',
            '⚠️ 口径': 'SO 里**没有** fogMode / 线性雾范围 —— 雾的形状取自**场景** RenderSettings，'
                     '见 资料/普查产出_0930/§28逐场核_第一轮.md §三',
        },
        '_fields': {
            'kind': 'default（每阵营一条，「不使用进攻卡」时生效）/ effect（进攻卡那条）',
            'ambientColor': '[r,g,b,a] —— 原版 ColorUsage(false,true) = HDR',
            'ambientBlend': '0=不施加 1=全施加（原版 Range(0,1)）',
            'scenarioObjects_GUID': '要实例化的环境 prefab 的 assetGUID（空 = 不换任何东西）',
            'defaultScenarioObjectsState': '0/1 —— prefab 建出来时的初始开关',
            'prefab_name': '解出的 GameObject 名（battleprefabs 包里）；解不出 = null',
            'used_by_cards': '哪几张进攻卡会切到它（来自 offensive_cards.json）',
        },
        '_unresolved': unresolved,
        'stats': {
            'total': len(rows),
            'default': sum(1 for r in rows if r['kind'] == 'default'),
            'effect': sum(1 for r in rows if r['kind'] == 'effect'),
            'with_prefab': sum(1 for r in rows if r['prefab_name']),
        },
        'conditions': rows,
    }
    io.open(CANON, 'w', encoding='utf-8', newline='\n').write(
        json.dumps(doc, ensure_ascii=False, indent=1))

    # ---- 摊平版（JsonUtility）----
    flat = {
        'items': [{
            'so': r['so'], 'army': r['army'] or '', 'kind': r['kind'],
            'blendTime': r['blendTime'] if r['blendTime'] is not None else 0.0,
            'ambientColor': r['ambientColor'] or [1, 1, 1, 0],
            'ambientBlend': r['ambientBlend'] if r['ambientBlend'] is not None else 0.0,
            'fogColor': r['fogColor'] or [1, 1, 1, 0],
            'fogDensity': r['fogDensity'] if r['fogDensity'] is not None else 0.0,
            'scenarioObjects_GUID': r['scenarioObjects_GUID'],
            'defaultScenarioObjectsState': r['defaultScenarioObjectsState'] or 0,
            'prefabName': r['prefab_name'] or '',
        } for r in rows],
    }
    os.makedirs(os.path.dirname(FLAT), exist_ok=True)
    io.open(FLAT, 'w', encoding='utf-8', newline='\n').write(json.dumps(flat, ensure_ascii=False, indent=1))

    print('SO 总数 %d（default %d / effect %d）· 有 prefab 名 %d'
          % (doc['stats']['total'], doc['stats']['default'], doc['stats']['effect'], doc['stats']['with_prefab']))
    for k, v in unresolved.items():
        print('  ⚠️ %s: %s' % (k, v))
    print('→ %s' % CANON)
    print('→ %s' % FLAT)


if __name__ == '__main__':
    main()
