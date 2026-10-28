import io, os

SRC = 'd:/4/Unity/资料/完全复刻/判据/B92_战斗_待查判据_全量.tsv'
DST = 'd:/4/Unity/账/_parts/B92_B7.tsv'
HDR = ['域','原版类','方法','判据出处','判据要点','状态','我们的落点','差异','等级','轮次','备注']

raw = io.open(SRC, 'rb').read().decode('utf-8-sig')
rows = [l.split('\t') for l in raw.split('\r\n') if l.strip()]
assert rows[0] == HDR, rows[0]
data = rows[1:]
assert all(len(r) == 11 for r in data), [len(r) for r in data if len(r) != 11]

def find(cls, meth_start):
    hits = [r for r in data if r[1] == cls and r[2].startswith(meth_start)]
    assert len(hits) == 1, (cls, meth_start, len(hits))
    return list(hits[0])          # 复制一份，别改原表

out = []

# ---------------- 1. MatchData.ShoultSetAiOpponent => D ----------------
r = find('MatchData', 'ShoultSetAiOpponent')
old = list(r)
r[3] = ('decomp_full/MatchData__ShoultSetAiOpponent.c:1-22（🥇 全文现读）· `dump.cs:78245`（`private bool ShoultSetAiOpponent(PlayModes)`，'
        'RVA 0x736AF0 / VA 0x180736AF0）· `PlayModes` 枚举 `dump.cs:46188-46207` · 调用点四源：`out_il2csharp/Assembly-CSharp/MatchData.cs:854`（只有定义）'
        '· `out_cpp2il_dev_ilspy/MatchData.cs:764`（只有定义）· `decomp_full` 按 VA 全树 0 命中')
r[4] = ('`u = playMode − 3; u < 10 && u ∉ {4,7,8} ⇒ true`。**AI 七档** = {Dungeon(3) · Tutorial(4) · CutScene(5) · OfflinePractice(6) · Campaign(8) · '
        'TutorialReplay(9) · OwnDeckTraining(12)}；**假** = {Classic(0) · Duel(1) · PracticeLodge(2) · ClosedDeck(7) · Replay(10) · RankedFriendly(11)} ∪ ≥13'
        '（🔴 **Skirmish(13) / Battle4Warpforge(14) 也是假**）。')
r[5] = 'D'
r[6] = ('CardPresentation/Battle/BattleDriver.cs:35367（ShoultSetAiOpponent 纯函数）· :35375（ShoultSetAiOpponentForTest 自检口）'
        '—— **纯函数保留作判据留档，⛔ 不接线**')
r[7] = ('【B92_B7 2026-10-28 判 D · 四类 (c) 四源全负】原版 `private` 且四处**都只有定义**：`decomp_full` 按名只有自身、按 VA `0x180736AF0` 全树 **0 命中**、'
        '`out_il2csharp/MatchData.cs:854` 与 `out_cpp2il_dev_ilspy/MatchData.cs:764` 都只有定义 ⇒ **原版从不执行它、零可观测行为**。'
        '🔴 **E4 族14 的「接 `DriveAiTurn`」是我们发明的闸，原版不存在**（接上 ≠ 复刻原版）。'
        '🔴 同源冲突：**`Skirmish(13)` 在原版表里是「非 AI」—— 接上会让遭遇战单机局没人出牌**（我们的离线替代必须替这些模式驱动对手；原版那些模式是真人 PvP）。'
        '我们的纯函数与上述七/六档集合一致（旧记「u = mode−3，u∈[0,10) 且 u∉{4,7,8}」口径不变）。')
r[10] = (old[10]
         + ' ｜ 🔴 **B92_B7 2026-10-28 判 D（四类 (c) 四源全负）**，覆盖 E4 族14「保持 B + 接 `DriveAiTurn`」的裁定 —— 重裁依据见「差异」列。'
         '**时间限定** = 当前 build（2026-09 快照）。**可逆备注** = 若在 `MatchData` 任何方法体 / 异步状态机体 / `assets_full` 的 `m_MethodName`（场景·prefab 用 UnityEvent 直调）'
         '里发现生产点 ⇒ **立即回 C**；⛔ 本条不再作为施工项。')
out.append(r)

# ---------------- 2. ActiveAbility.DeepCopy => D ----------------
r = find('ActiveAbility', 'DeepCopy')
old = list(r)
r[3] = ('cpp2il/out_cpp2il_dev_ilspy/ActiveAbility.cs:72-162（🥇 全文现读；深拷三处 = `:116`/`:119`/`:133`）· 字段表 `dump.cs:23753-23767`'
        '（`targetCriteria +0x38` / `targetsAvailableCounter +0x40` / `activeAbilityLogic +0x48`）· 四源调用点：按名只有自身 · VA `0x18093D790` 全树 0 · '
        'dev_ilspy 0 · il2csharp 0 · `ActiveAbility___ctor` 全仓 0 调用（ctor RVA `0x4B3000` = 全局空方法桩）')
r[4] = ('`MemberwiseClone()` 之后深拷三样：`targetCriteria(+0x38)`、`targetsAvailableCounter(+0x40)`、`activeAbilityLogic` 列表**逐条** `AbilityLogic.DeepCopy()`；'
        '其余字段共用引用。⚠️ 三格在池内**全部只读**（`ActiveSkillDisplay.cs:157-172` · `BattleCardUI.cs:1282-1285` · `CardScript.cs:8317-8344` · '
        '`RawCardScript.cs:3441/3916` · `AbilityLogic.GetTargets`），**0 处写**。')
r[5] = 'D'
r[7] = ('【B92_B7 2026-10-28 判 D】原版 = 可变数据 + `DeepCopy` 隔离；我们 = `ActiveAbilityLogicSpec` 共享不可变 —— **净等价**。'
        '**证据**：①三格全只读（见「判据要点」）；②本方法**四处全负**（按名 · VA `0x18093D790` · dev_ilspy · il2csharp）；'
        '③**代码侧无构造点**：全仓 `ActiveAbility___ctor` 0 调用，且其 RVA = `0x4B3000`（全局空方法桩 `ret 0`）⇒ 实例只由 SO 反序列化产生。'
        '⇒ 该方法在本 build **永不可达**。')
r[10] = (old[10] + ' ｜ 🔴 **B92_B7 2026-10-28 判 D（四类 (c) + (b)）**，结掉原「还差：`targetCriteria` 各字段在运行期有没有写点（VA 反汇编扫该偏移）」。'
         '**时间限定** = 当前 build。**可逆备注** = 发现内联（先查 `decomp_full/AbilityLogic__PlayAbility.c` 的 `MemberwiseClone` 痕迹）或异步状态机体里的生产点 ⇒ **回 C**。'
         '⚠️ 判 D **不省任何行为**（我们共享不可变属净等价）。')
out.append(r)

# ---------------- 3. ActiveAbility.ShallowCopy => D ----------------
r = find('ActiveAbility', 'ShallowCopy')
old = list(r)
r[3] = ('cpp2il/out_cpp2il_dev_ilspy/ActiveAbility.cs:39-70（🥇 全文现读：纯 `MemberwiseClone()`）· 调用点：按名只有自身 · VA `0x18093D9D0` 全树 0 · '
        'dev_ilspy 0 · il2csharp 0（同类 `CardAbility.ShallowCopy` 同样 0 调用点）')
r[4] = ('`(ActiveAbility)MemberwiseClone()` —— 只换外壳，所有引用字段（含 `targetCriteria` / `targetsAvailableCounter` / `abilityLogic` 列表）**共享**。')
r[5] = 'D'
r[7] = ('【B92_B7 2026-10-28 判 D】`ShallowCopy` = `MemberwiseClone` + 类型校验；**四处全负**（按名 · VA `0x18093D9D0` · dev_ilspy · il2csharp），'
        '且同族 `ActiveAbility___ctor` 0 调用（RVA `0x4B3000` 空桩）⇒ 无构造点 ⇒ 本 build 永不可达。'
        '我们无「浅副本」概念（数据共享不可变 = 「浅拷」的常态），原版用它造「只换几格字段的临时副本」——**净等价**。'
        '⚠️ 同类死代码：`CardAbility.ShallowCopy` 亦 0 调用点。')
r[10] = (old[10] + ' ｜ 🔴 **B92_B7 2026-10-28 判 D（四类 (c) + (b)，与 `ActiveAbility.DeepCopy` 同一组证据）**。'
         '**时间限定** = 当前 build。**可逆备注** = 同 DeepCopy（发现内联或异步体生产点即回 C）。')
out.append(r)

# ---------------- 4. EntityScript.GetThisCardPlayedDamage => 判据补全（仍 C） ----------------
r = find('EntityScript', 'GetThisCardPlayedDamage')
old = list(r)
r[3] = ('decomp_full/EntityScript__GetThisCardPlayedDamage.c:1-113（🥇 全文现读）· 字段：`AbilityLogic.chosenAbility +0x10` / `damageCriteria +0x40`'
        '（`dump.cs:19456/19466`）· `DamageCriteria.minDamage +0x14`（`dump.cs:19589`）· `AbilityEffect` 10/12/15（`dump.cs:20651-20654`）· '
        '`CardAbility.targetCriteria +0x30` · 旧注记 `out_cpp2il_dev_ilspy/EntityScript.cs:1526`（同名方法，体噪）')
r[4] = ('取 `rawCardData.GetAbilitiesForTriggerRawcard(ThisCardPlayed(0))`；每个 ability 先过 '
        '`FilterMethods.CheckIfMeetsCriteria(this, target, target, ability.targetCriteria(+0x30))`，再对该 ability 的 `abilityLogic(+0x50)` **逐条**：'
        '`chosenAbility(+0x10) ∈ {doDamage(10), doDamageMultiRandom(12), doDamageAndDestroySelf(15)}` ⇒ 累加 **`damageCriteria.minDamage(+0x14)`**，返回合计。'
        '🔴 **原版只算三档、漏 `doDamageFast(13)`**（`.c:80-93` 只比 10/15/12）—— 照抄、⛔ 别补齐。')
r[7] = (old[7] + ' ｜ 🆕 **B92_B7 2026-10-28 判据补全**：全文取到（见「判据要点」），不再是「只知道是取数口」。'
        '**可达性（现扫，不是推断）**：`cards_engine.json` 里 `abilityTriggers` 含 0 且 `abilityDamage.min > 0` 的卡 = **183 张**'
        '（例 `ASH46` Aerial Dominion min 3）⇒ 这不是休眠件。验收口不变：一张 damage 3 的战术卡悬停敌人时预览显示 3。')
r[10] = (old[10] + ' ｜ 🆕 B92_B7 2026-10-28：判据已补全（三档 chosenAbility + `minDamage` 求和；可达 183 张）；**状态仍 C**（取数口与预览接线都未做）。'
         '建议落点 = `RuleEngine/Core/CardDef.cs`（或 RuleCore）新增取数口 `PlayedDamageAgainst(target)` ＋ `BattleDriver.BattleBoardPreview`（现读 `:34387`）的伤害条目改用它。')
out.append(r)

# ---------------- 5. DamageDeltToEnemyUnit.OnDamageDelt => 差异改写（仍 B） ----------------
r = find('DamageDeltToEnemyUnit', 'OnDamageDelt')
old = list(r)
r[3] = ('decomp_full/DamageDeltToEnemyUnit__OnDamageDelt.c:14-86（🥇 全文现读）· 字段表 `dump.cs:59180-59200`（14 格）· `DamageInfo` `dump.cs:119230-119237`'
        '（`actingCard +0x10` / `damageValue +0x18` / `damageType +0x1C` / `damageAbsorbed +0x24` / `dealtByShuriken +0x25` / `+0x26 sentry` / `+0x27 markerlight`）· '
        '`DamageSignal` `dump.cs:26900-26901`（`DamageInfo +0x10` / `DamagedUnit +0x18`）· 信号发射口 `decomp_full/BattleManager__BroadcastUnitDamaged.c` 全文 ＋ dev `BattleManager.cs:24516-24527` · '
        '护甲扣减 `CardScript.ResolveDamageDealt`（dev `CardScript.cs:14317`）')
r[4] = ('攻方 = `DamageInfo.actingCard`；闸 = `damageInfo.IsPlayer() && (anyAttackingType(+0x58) || actingCard.cardType == attackingType(+0x68))`，再依次过 '
        '`anyEnemyType(+0x5A)` / `anyDamageType(+0x5C)` / `anyAttackType(+0x5B)`+bVar7 / `onlyDamageByShuriken(+0x5D)` / `onlyDamageBySentry(+0x5F)` / `onlyDamageByMarkerlight(+0x5E)` / '
        '`anyAttackingTrait(+0x59) | HasCurrentTrait(attackingTrait +0x64)` + `(!attackingIsCompanion(+0x60) || actingCard.isCompanion)` ⇒ `MissionChallenge.UpdateProgress(damageValue(+0x18))`。'
        'bVar7：攻方 `cardType ∈ {Minion(0), Hero(10)}` ⇒ 要求 `attackType(+0x70) == actingCard+0x120 且 damageType == Attack(4)`；否则要求 `attackType == Spell(3)`。')
r[7] = ('① **量**：原版记 `DamageInfo.damageValue(+0x18)` = **扣甲前**（护甲在 `CardScript.ResolveDamageDealt` 才减：dev `CardScript.cs:14317` '
        '`val = damageValue - num2; num3 = Math.Max(1, val)`），且 **absorbed 也计入**（`BroadcastUnitDamaged` 只在 `actingCard != null` 时 `challengeLog.LogDamage`，**不看** `damageAbsorbed`）；'
        '我们记 `actual`（扣甲后、被盾/闪避/无敌/空投舱挡下时不计）⇒ **少计**。'
        '② **归属**：原版按 `actingCard.isPlayer` 记给**攻方**；我们按「受害者对侧」`1 - _own` ⇒ 自己打自己人时记到对方 ⇒ **归属口径不同**。'
        '③ **分类**：靠调用点打 `DamageKind`（带 `kind` 形参的入口现读 **6 处**：`RuleCore.cs:8437/8444/9010/9028/9156/16086`；`Tally.AddDamage` 的唯一写点 = `RuleCore.ApplyDamageCore:8946`），'
        '不是从 `DamageInfo` 推（原账「9 个调用点」已过期）。'
        '⚠️ 施工前置：`ApplyDamageCore` 现签名**拿不到攻方**（只有 `u`/`source`/`kind`）⇒ 须把攻方座位（或 `actingCard`）作为形参带进来。')
r[10] = (old[10] + ' ｜ 🆕 **B92_B7 2026-10-28 结掉两条「未核」**：①量 = 扣甲前、absorbed 也计入；②归属按 `actingCard.isPlayer`。'
         '⇒ 三处差异仍成立（**少计 + 归属口径不同 + 分类靠调用点**），状态保持 **B**（不是 A：行为不等价）。')
out.append(r)

# ---------------- 6. 行号重钉：SetBotOpponentDeck（差异列 1378→1494） ----------------
r = find('MatchData', 'SetBotOpponentDeck')
assert 'TutorialScript.cs:1378' in r[7], r[7][:200]
r[7] = r[7].replace('TutorialScript.cs:1378', 'TutorialScript.cs:1494')
r[10] = r[10] + ' ｜ B92_B7 2026-10-28 现读重钉：`TutorialRules.ShouldShuffleDeck` 1378 → **1494**。'
out.append(r)

# ---------------- 7. 行号重钉：SetPlayerDeck（落点 601→605） ----------------
r = find('MatchData', 'SetPlayerDeck')
assert 'RuleCore.cs:601' in r[6], r[6]
r[6] = r[6].replace('RuleCore.cs:601', 'RuleCore.cs:605')
r[10] = r[10] + ' ｜ B92_B7 2026-10-28 现读重钉：`RuleCore.BuildPlayer` 601 → **605**。'
out.append(r)

# ---------------- 8. 行号重钉：get_displaySummonSickness（落点 3295→3235） ----------------
r = find('EntityScript', 'get_displaySummonSickness')
assert ':3295' in r[6], r[6]
r[6] = r[6].replace(':3295', ':3235')
r[10] = r[10] + ' ｜ B92_B7 2026-10-28 现读重钉：`RecomputeActAfterAuraKeyword` 3295 → **3235**。'
out.append(r)

# ---------------- 9. 行号重钉：SetBotMatch（备注 2152-2166→2222） ----------------
r = find('MatchData', 'SetBotMatch')
assert 'BattleContext.cs:2152-2166' in r[10], r[10][:300]
r[10] = r[10].replace('BattleContext.cs:2152-2166', 'BattleContext.cs:2222')
r[10] = r[10] + ' ｜ B92_B7 2026-10-28 现读重钉：`MatchTypes.Effective` 2152 → **2222**。'
out.append(r)

# ---------------- 写出 ----------------
for r in out:
    assert len(r) == 11, (len(r), r[1], r[2])
    assert r[5] in ('A', 'B', 'C', 'D'), r[5]
    assert '(' in r[2], r[2]                       # 方法列必须带签名
    for c in r:
        assert '\t' not in c and '\r' not in c and '\n' not in c

payload = '\r\n'.join(['\t'.join(HDR)] + ['\t'.join(r) for r in out]) + '\r\n'
os.makedirs(os.path.dirname(DST), exist_ok=True)
io.open(DST, 'wb').write(b'\xef\xbb\xbf' + payload.encode('utf-8'))

b = io.open(DST, 'rb').read()
print('WROTE', DST, 'bytes', len(b), 'BOM', b[:3] == b'\xef\xbb\xbf', 'crlf', b.count(b'\r\n'), 'lf', b.count(b'\n'))
t = b.decode('utf-8-sig')
lines = [l for l in t.split('\r\n') if l.strip()]
print('rows', len(lines) - 1)
for l in lines[1:]:
    f = l.split('\t')
    print(' ', f[1] + '.' + f[2][:46], '| 状态', f[5], '| cols', len(f))
