# -*- coding: utf-8 -*-
"""P1 收尾 · ① 把 .cs 里的旧 id 字面量换成新 id。

判据：`d:/4/_tmp_view/p1_1014/idmap.json`（旧 id -> 新 id，P1 生成）+ `M_卡牌P1_逐张对拍.tsv`。
本脚本**不含** `CampaignData.cs`（那一份的 NodeId / reward targetId 是【照 SO 抄的原版 id】，
不是卡 id 字面量 —— 见 `V_P1收尾.md` ①·二）。

替换纪律（铁律）：**逐条断言计数**，替换前后都打印；计数不符就**整脚本不写盘**。
"""
import io, json, sys, os

ROOT = 'd:/4/'
IDMAP = json.load(io.open(ROOT + '_tmp_view/p1_1014/idmap.json', encoding='utf-8'))

# (相对路径, 期望出现次数)  —— 期望次数来自 2026-10-15 现核（见报告 §①）
WORK = [
    ('Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs', [
        ('EC_Terminator_Champion', 1),
        ('EC_Maulerfiend', 1),
        ('BL_Dark_Pact_of_Fate', 2),      # L19113 + L19137
        ('BL_Dark_Pact_of_Blood', 1),
        ('BL_Dark_Pact_of_Excess', 1),
        ('BL_Dark_Pact_of_Resilience', 1),
        ('ASH_Path_of_the_Seer', 1),
        ('EC_Duelist_s_Hubris_Lucius_Talent', 1),
        ('SAU_Master_Chronomancer', 1),
        ('AM_Cadian_Honour', 1),
        ('BL_Diabolic_Strength', 1),
        ('BL_Infernal_Gaze', 1),
        ('BL_Unhallowed_Gifts', 1),
    ]),
    ('Unity/MyGame/Assets/CardPresentation/Editor/DeckScene.cs', [
        ('UM_Smite', 2),                  # L1227 一行里两次（`!= null` + `CannotAddToDeck`）
        ('EC72', 2),                      # L1231 同上
        ('DA_Convoke_the_Circle', 2),     # L1237 / L1238
        ('DA_Plasma_Generator', 2),       # L4289 / L4297
    ]),
    ('Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs', [
        ('GSC_Cult_Propaganda', 1),
        ('GSC_Improvised_Barricade', 1),
        ('GSC_Jammed_Communications', 1),
        ('GSC_Poisoned_Supplies', 1),
    ]),
    ('Unity/MyGame/Assets/CardPresentation/Editor/NetSelfTest.cs', [
        ('UM_Master_Tactician', 2),
    ]),
    ('Unity/MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs', [
        ('DA_Azrael', 1),
    ]),
    ('Unity/MyGame/Assets/CardPresentation/Shell/TutorialModePopup.cs', [
        ('BL_Sylar_Hexcorn', 1),
    ]),
    ('Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest_S14.cs', [
        ('SOR_Righteous_Repugnance', 1),
    ]),
    ('Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest_S23_Parse.cs', [
        ('GSC_Jammed_Communications', 1),
    ]),
]


def main(write):
    plan = []            # (path, text_after, [(old, new, n)])
    for rel, pairs in WORK:
        p = ROOT + rel
        raw = io.open(p, 'rb').read()
        text = raw.decode('utf-8')
        crlf0 = text.count('\r\n'); lf0 = text.count('\n')
        applied = []
        for old, want in pairs:
            assert old in IDMAP, 'no mapping for ' + old
            new = IDMAP[old]
            n_before = text.count('"' + old + '"')
            print('  %-46s %-36s x%d (want %d) -> %s' % (os.path.basename(rel), old, n_before, want, new))
            assert n_before == want, 'COUNT MISMATCH %s in %s: got %d want %d' % (old, rel, n_before, want)
            text = text.replace('"' + old + '"', '"' + new + '"')
            n_after = text.count('"' + new + '"')
            assert n_after >= want, 'lost tokens: ' + old
            # 旧 id 必须**一个不剩**（带引号形式）
            assert text.count('"' + old + '"') == 0, 'leftover old id ' + old
            applied.append((old, new, n_before))
        crlf1 = text.count('\r\n'); lf1 = text.count('\n')
        assert (crlf0, lf0) == (crlf1, lf1), 'line endings changed in ' + rel
        plan.append((p, raw, text.encode('utf-8'), applied))
        print('  -> %s  行尾 CRLF %d / LF %d（未变）' % (rel, crlf1, lf1))
    if write:
        for p, before, after, applied in plan:
            io.open(p, 'wb').write(after)
            print('WROTE %s  (%d 条)' % (p, len(applied)))
    else:
        print('DRY-RUN OK，未写盘（加 --write 才写）')


if __name__ == '__main__':
    main('--write' in sys.argv)
