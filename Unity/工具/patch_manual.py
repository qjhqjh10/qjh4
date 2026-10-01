# ⚠️ 一次性修复脚本（2026-10-01 已跑过，结果已进仓库）。
# 重新跑是**幂等**的（已经是合法局面的地方不会再改），但**只在** RuleEngineTest.cs
# 回到「有洞棋盘」状态时才有意义；`patch_manual.py` 依赖前两个脚本先跑过。
# -*- coding: utf-8 -*-
"""主对话手工拍板的几处（脚本判不出朝向/带连带语义的）：可重复执行。"""
import io

P = 'd:/4/Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs'
raw = io.open(P, 'rb').read()
txt = raw.decode('utf-8').replace('\r\n', '\n')     # ⚠️ 文件是 CRLF：统一成 LF 再匹配，写回时还原

REPL = [
    # ① TestWhenEventsWidened：监听器现在在 3 号格（原来读 1 号格 ⇒ NRE）
    ('RuleCore.ApplyDamage(ctx, Board(ctx, 0, 1), 3, "自检");',
     'RuleCore.ApplyDamage(ctx, Board(ctx, 0, 3), 3, "自检");   // ⚠️ 监听器在 3 号格（连续模型下它不再在 1 号格）'),
    # ② TestTacticPlay：pick 是「只打一个敌方单位」的真卡 ⇒ 目标在对面 3 号格
    ('int code = RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, pick.Name), 0);',
     'int code = RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, pick.Name), 3);'),
    # ③ FixtureSpend2 = `Deal 1 damage to a random enemy`：对手单位在 3 号格
    ('RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "FixtureSpend2"), 2)',
     'RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "FixtureSpend2"), 3)'),
    ('Check(Board(ctx, 0, 2).Attack, 3, "★ 0 号格那个**触发了**");',
     'Check(Board(ctx, 0, 2).Attack, 3, "★ 2 号格那个（先放上去的）**触发了**");'),
    ('"★ 1 号格那个**不触发**（原版 `break`：一次只触发一个）"',
     '"★ 3 号格那个**不触发**（原版 `break`：一次只触发一个）"'),
    # ⑦ 走**辅助函数**摆盘的用例（`Duel(..., aSlot:3)` / `ToP2TurnWith`）—— 那些槽位脚本看不见：
    #    TestEffectChainGuard：Poker 由 `Duel(aSlot: 3)` 摆在 3 号格 ⇒ p1 要摆 2 号格（不是 3，会覆盖）
    ('        Place(ctx, 0, 3, p1);\n        ctx.ClearSignals();',
     '        Place(ctx, 0, 2, p1);        // ⚠️ Poker 在 3 号格（`Duel(aSlot:3)`）⇒ 这里摆 2 号格\n        ctx.ClearSignals();'),
    # ⑧ TestAiKillLine：`ToP2TurnWith` 里已经把卡摆在 3 号格 ⇒ Mine1 要摆 2；疲劳那条读 3
    ('        Place(ctx2, 1, 3, Unit("Mine1", 1, 3, 3));',
     '        Place(ctx2, 1, 2, Unit("Mine1", 1, 3, 3));   // ⚠️ 3 号格已经被 `ToP2TurnWith` 摆上了'),
    ('        ctx4.Players[1].Board[1].Exhausted = true;',
     '        ctx4.Players[1].Board[3].Exhausted = true;'),
    # ⑨ TestAiUsesAbility：`ToP2TurnWith` 把卡摆在 3 号格 ⇒ 断言里的 1 改成 3
    ('        Check(s, 1, "选中了场上的 Reef Guard");',
     '        Check(s, 3, "选中了场上的 Reef Guard");'),
    ('        Check(t, 1, "目标是对面那个单位");',
     '        Check(t, 3, "目标是对面那个单位");'),
    # ⑩ 其它「按槽位号判存活」的断言（`SlotOf` 是动态找 → 期望值要按**现在的**布局写）
    ('Check(SlotOf(ctx, 0, "FixtureOwn"), 1, "我方那张还活着 —— 这次死的是**敌方**的");',
     'Check(SlotOf(ctx, 0, "FixtureOwn"), 2, "我方那张还活着 —— 这次死的是**敌方**的");'),
    ('Check(SlotOf(ctx, 0, "FixtureOurKiller"), 2, "我方攻击方也活着（没吃反击 —— 目标 0 攻）");',
     'Check(SlotOf(ctx, 0, "FixtureOurKiller"), 3, "我方攻击方也活着（没吃反击 —— 目标 0 攻）");'),
    ('Check(SlotOf(ctx, 0, "FixtureHurtTarget"), 1, "挨打的那个还活着（没被这一下打死）");',
     'Check(SlotOf(ctx, 0, "FixtureHurtTarget"), 3, "挨打的那个还活着（没被这一下打死）");'),
    ('Check(SlotOf(ctx, 0, "FixtureOurHurtKiller"), 1, "我方攻击方还活着（没吃反击 —— 目标 0 攻）");',
     'Check(SlotOf(ctx, 0, "FixtureOurHurtKiller"), 3, "我方攻击方还活着（没吃反击 —— 目标 0 攻）");'),
    ('Check(SlotOf(ctx, 1, "FixtureCruelPrey"), 1, "对面那个还活着（没被打死）");',
     'Check(SlotOf(ctx, 1, "FixtureCruelPrey"), 3, "对面那个还活着（没被打死）");'),
    # ⑪ TestAiOriginal ④：`ToP2TurnWith` 已经把 A 摆在 3 号格 ⇒ B 摆 2（摆 3 会覆盖 A）
    ('Place(ctx, 1, 3, Unit("B", 2, 2, 2));',
     'Place(ctx, 1, 2, Unit("B", 2, 2, 2));       // ⚠️ 3 号格已经被 `ToP2TurnWith` 摆上了'),
    # ⑫ TestAiKillLine ⑤：两个候选单位现在在 3 / 2 号格
    ('Check(ts, 1, "★ **两个都能一刀打死时，打价值高那个**',
     'Check(ts, 3, "★ **两个都能一刀打死时，打价值高那个**'),
    ('Check(ts, 0, "★ **打不死的那口先不碰**',
     'Check(ts, 2, "★ **打不死的那口先不碰**'),
    # ⑬ `FixtureDroneEarly` —— 原来摆在 **4 号格（督军格）**，会把督军顶掉（引擎报「督军不在 4 号格」）
    ('            var early = Place(ctx, 0, 4, new CardDef("FixtureDroneEarly", "FixtureDroneEarly",',
     '            // ⚠️ 2026-10-01：原来摆在 4 号格（督军格）上 —— 那会把督军顶掉、棋盘非法；改摆 3 号格。\n'
     '            var early = Place(ctx, 0, 3, new CardDef("FixtureDroneEarly", "FixtureDroneEarly",'),
    # ⑭ TestAiKillLine ①：`ToP2TurnWith` 已经把 Mine0 摆在 3 号格 ⇒ Mine1 摆 2
    ('        Place(ctx, 1, 3, Unit("Mine1", 1, 3, 3));',
     '        Place(ctx, 1, 2, Unit("Mine1", 1, 3, 3));   // ⚠️ 3 号格已经被 `ToP2TurnWith` 摆上了'),
    # ⑮ AI 收集灵魂石：石头现在在 3 号格
    ('new AiAction { Kind = AiActionKind.CollectWaystone, Slot = 0 }',
     'new AiAction { Kind = AiActionKind.CollectWaystone, Slot = 3 }'),
    # ⑯ TestBattleKeywords ⑤：P2 那侧原来只有 2 号格有人（有洞）⇒ 改成 3（贴督军）打头、Next 摆 2
    ('            Place(ctx, 1, 2, Unit("Prey", 1, 0, 1));      // 1 血，3 攻一击必杀',
     '            Place(ctx, 1, 3, Unit("Prey", 1, 0, 1));      // 1 血，3 攻一击必杀（⚠️ 连续模型：贴督军那格 = 3）'),
    ('            int kcode = RuleCore.DeclareAttack(ctx, 0, 3, 1, 2);',
     '            int kcode = RuleCore.DeclareAttack(ctx, 0, 3, 1, 3);'),
    ('            Check(Board(ctx, 1, 2), null, "猎物被摧毁、格位空了");',
     '            Check(Board(ctx, 1, 3), null, "猎物被摧毁、格位空了");'),
    ('            Place(ctx, 1, 3, Unit("Next", 1, 0, 9));',
     '            Place(ctx, 1, 2, Unit("Next", 1, 0, 9));'),
    ('            RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Died"), 3);',
     '            RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Died"), 2);'),
    ('            Check(Board(ctx, 1, 3).Health, 6, "阵亡 1 个 → 打 1 遍 3 点（9 → 6）");',
     '            Check(Board(ctx, 1, 2).Health, 6, "阵亡 1 个 → 打 1 遍 3 点（9 → 6）");'),
    # ⑰ TestA4Batch3：第一只用狂暴的那个原来独自挂在 2 号格（有洞）⇒ 改摆 3 号格
    ('                Place(ctx, 0, 2, fero);\n'
     '                CheckCode(RuleCore.UseAlternative(ctx, 0, 2, "ferocity"), RuleCodes.OK,',
     '                Place(ctx, 0, 3, fero);\n'
     '                CheckCode(RuleCore.UseAlternative(ctx, 0, 3, "ferocity"), RuleCodes.OK,'),
    # ⑤ 事件里带的格位（残骸 / 攻击 / 死亡 / 反冲）—— 单位现在都在 3 号格
    ('CheckTrue(sigCollect != null && sigCollect.Slot == 0 && sigCollect.Player == 1,',
     'CheckTrue(sigCollect != null && sigCollect.Slot == 3 && sigCollect.Player == 1,'),
    ('CheckTrue(strike != null && strike.Slot == 1, "Strike 事件带着攻击者的格位");',
     'CheckTrue(strike != null && strike.Slot == 3, "Strike 事件带着攻击者的格位");'),
    ('CheckTrue(death != null && death.Player == 1 && death.Slot == 1,',
     'CheckTrue(death != null && death.Player == 1 && death.Slot == 3,'),
    ('CheckTrue(back != null && back.Player == 1 && back.Slot == 1,',
     'CheckTrue(back != null && back.Player == 1 && back.Slot == 3,'),
]

# ④ TestStealthUntargetable：Plain（对面 3 号格）被打死后 Sneak **补位到 3 号格**
#    （连续模型的必然结果）⇒ 后面两条指向 2 号格的引用要改成 3。空白不敏感。
import re
pat4 = re.compile(r'CheckCode\(RuleCore\.DeclareAttack\(ctx, 1, 2, 0, 3\), RuleCodes\.OK, "隐身单位自己可以攻击"\);\s*\n(\s*)Check\(Board\(ctx, 1, 2\)\.Has\("stealth"\), false,')
if pat4.search(txt):
    txt = pat4.sub(lambda m: 'CheckCode(RuleCore.DeclareAttack(ctx, 1, 3, 0, 3), RuleCodes.OK, "隐身单位自己可以攻击");\n'
                           + m.group(1) + '// ⚠️ Plain（对面 3 号格）被打死 ⇒ Sneak **补位到 3 号格**（连续模型的必然结果）\n'
                           + m.group(1) + 'Check(Board(ctx, 1, 3).Has("stealth"), false,', txt, count=1)
    n4 = 1
else:
    n4 = 0

# ⑥ Bjorn's Shrine 那块：连续模型下「没标记的那个洗回牌库」会让**另一个补位到 3 号格**
#    ⇒ 改为量**牌库张数 +1**（这才是「洗回牌库」的直接判据），并断言场上剩的是被标记那个。
pat6a = re.compile(r'Place\(ctx, 0, 3, fero\);\s*// 同一张卡的另一个单位（不标记）')
pat6b = re.compile(
    r'( *)CheckCode\(RuleCore\.UseAlternative\(ctx, 0, 3, "ferocity"\), RuleCodes\.OK,\s*\n'
    r' *"另一个（\*\*没标记\*\*的）用狂暴"\);\s*\n'
    r'( *)CheckTrue\(Board\(ctx, 0, 3\) == null,\s*\n'
    r' *"★ 反例：没标记的那个\*\*照常洗回牌库\*\*（规则书 :186）" \+ LogTail\(ctx\)\);')
n6 = 0
if pat6a.search(txt):
    txt = pat6a.sub('var unmarked = Place(ctx, 0, 3, fero);        // 同一张卡的另一个单位（不标记）', txt, count=1)
    n6 += 1
if pat6b.search(txt):
    def _r6(m):
        i = m.group(2)
        return (f'{m.group(1)}int deck0 = ctx.Players[0].Deck.Count;\n'
                f'{m.group(1)}CheckCode(RuleCore.UseAlternative(ctx, 0, 3, "ferocity"), RuleCodes.OK,\n'
                f'{m.group(1)}          "另一个（**没标记**的）用狂暴");\n'
                f'{i}// ⚠️ 2026-10-01：连续棋盘下它会**补位** —— 洗回牌库那个一离场，留下的那个挪到贴督军那格\n'
                f'{i}//    ⇒ 「洗回牌库」的直接判据是**牌库张数 +1**，不是「3 号格空了」。\n'
                f'{i}Check(ctx.Players[0].Deck.Count, deck0 + 1,\n'
                f'{i}      "★ 反例：没标记的那个**照常洗回牌库**（规则书 :186）" + LogTail(ctx));\n'
                f'{i}CheckTrue(ReferenceEquals(Board(ctx, 0, 3), marked)\n'
                f'{i}          && !ReferenceEquals(Board(ctx, 0, 3), unmarked),\n'
                f'{i}          "★ 场上只剩**被标记**的那个（它补位到 3 号格）");')
    txt = pat6b.sub(_r6, txt, count=1)
    n6 += 1

n = 0
for old, new in REPL:
    if old in txt:
        txt = txt.replace(old, new, 1); n += 1
    elif new in txt:
        pass                     # 已经改过
    else:
        print('⚠️ 没找到（可能已被别处改动）:', old.splitlines()[0][:70])
io.open(P, 'wb').write(txt.replace('\n', '\r\n').encode('utf-8'))
print('套用', n, '处 · ④ stealth 补位', n4, '处 · ⑥ Bjorn 牌库', n6, '处')
