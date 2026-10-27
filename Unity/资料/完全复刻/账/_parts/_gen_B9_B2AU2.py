# -*- coding: utf-8 -*-
# 生成第三批账补丁 _parts/B9_B2AU2.tsv（只读主表；只改 差异/备注/轮次 三列；状态不动）
import csv, io

LEDGER = 'D:/4/Unity/资料/完全复刻/账/战斗界面_HUD.tsv'
OUT = 'D:/4/Unity/资料/完全复刻/账/_parts/B9_B2AU2.tsv'

with io.open(LEDGER, encoding='utf-8', newline='') as f:
    rd = csv.reader(f, delimiter='\t')
    hdr = next(rd)
    rows = [r for r in rd]
assert len(hdr) == 11, hdr
I = {h: i for i, h in enumerate(hdr)}
I_STATUS, I_METHOD, I_DIFF, I_NOTE, I_ROUND = (
    I['状态'], I['方法'], I['差异'], I['备注'], I['轮次'])

TARGETS = [
    'SetupActiveSkill(CardScript parentCard)',
    'SetupTutorialObj(RawCardScript rawCardData, bool forceHide)',
    'AutoProcessDone()',
    'Setup(List<RawCardScript> chooseCards, string actingCardId, int timerCounter, int target)',
    'DisplayOffensiveCardInCenter(float timeToDraw, float stepTime)',
    'GetCurrentLocalizedAbilityDescription()',
    'GetEnemyCardPlayedDisplayPosition()',
    'GetOffensiveCardPlayedDisplayPosition()',
]

NEW_DIFF = {
 'SetupActiveSkill(CardScript parentCard)':
   '面板侧与闸都已就位（`SkillPanel.SetTargetsSuffix` / `CardFaceRules.ShouldShowTargetsAvailable`）。'
   '🔴 **2026-10-27（B2AU）就地订正账里那句「`scan_ability` 不 emit ability 级 `targetsAvailableCounter`」—— 已过期**：'
   '现读 `工具/gen_cards_truth.py:308-311` **就在 ability 级循环里 emit** `targetsAvailableCounter`（`compact_criteria`）。'
   '真正的断点在后两级：② `RuleEngine/Core/CardDef.cs` 只落了 **卡级** `avail` 行（v22，`+0x298` 口径），'
   '**没有 ability 级那一格**；`RuleEngine/Data/CardDatabase.cs:802` 同；'
   '③ `BattleDriver.ShowSkillPanel` 读的是卡级 `CardFaceRules.HasTargetsAvailableCounter`。'
   '⇒ 缺的是「ability 级落列 + 改读」，不是生成器。',
 'SetupTutorialObj(RawCardScript rawCardData, bool forceHide)':
   '缺「教程指向物」子树本身。🔴 **2026-10-27（B2AU）复验：`CardDisplayWindow.cs` 里 `TutorialObjs` 只出现在 3 处注释'
   '（`:21` / `:245` / `:1591`）、`SetupTutorialObj` **零命中**、没有任何 `TutorialObjs` 子树实现** ⇒ 与本行原判一致。'
   '另两条「为什么现在不建」仍然成立且我核过：① 闸的数据源 = LiveOps `TutorialEvent`（本地不可得，'
   '`Shell/TutorialModePopup.cs` 自己写着随远端下发）⇒ 建出来恒不可达；② 树根 rect 只在与父链缩放联立时成立，父链 scale 未逐节核。',
 'AutoProcessDone()':
   '与 `Setup` 的 ④ 同一卡点：录像里**只记了答案**（`Ctx.ChoosePicks` / `ChooseCardIds`），没记候选集，'
   '原版面板要摆的是 `GetChoiceOptions` 的**输出** ⇒ 重建不出来。🔴 **2026-10-27（B2AU）**：复验成立 —— '
   '`BeginAsk` 只有 `BeginPlay` / `Resolve` 两个调用点，回放局根本不走它 ⇒ 面板不弹、这一段无落点。'
   '要复刻得先给录像头加「本次选择候选集」（生产者 = `BattleDriver`；读侧 `Battle/ReplayStore.cs` 我可改，'
   '但**没有生产者时那一格永远是空**、属死件 ⇒ 本轮不单建）。',
 'Setup(List<RawCardScript> chooseCards, string actingCardId, int timerCounter, int target)':
   '①②③⑤ 齐（`ShowAsk`/`_choosePanel.Open`：标题键回落链、确认钮文案、初始不可点、0.5 s 延迟都在）；'
   '**④ 回放自动选**与 `AutoProcessDone` 是同一笔（候选集没记）。'
   '🔴 2026-10-27（B2AU）复验：结论不变；本行另外确认 —— 回放分支接上后要保的「标题回落 `Instructions-<id>` 优先」'
   '那一条**现在已在**（`ShowAsk` 里），⛔ 别在接线时把它挪走。',
 'DisplayOffensiveCardInCenter(float timeToDraw, float stepTime)':
   '🔴 **2026-10-27（B2AU）就地订正本行的落点指法**：原版这三个「展示位」方法**不是**放大窗（`CardDisplayWindow`）'
   '的行为，它移动的是**棋盘/手牌上那张 CardScript 本体**。我们这一侧的等价物**已经存在**，在 '
   '`BattleDriver` 的 reveal 链里：`RevealStartPx/RevealEndPx`（`BattleDriver.cs:9712-9713`）+ '
   '`RevealMoveDur = 0.15`（`DAT_1834b3178`，`:9667`）/ `RevealRotDur = 0.1`（`:9668`）+ '
   '终点缩放 `GetEnemyCardPlayedDisplayScale`（`:25390-25470`），方法头的推导在 `:9644-9660`。'
   '**残差只有一项**：原版位置 = `BattleManager.enemyHand.transform.position + right×Xoffset − up×Yoffset`，'
   '而 `enemyHand` 那个世界点在我们这边**没有一一对应的判据** ⇒ `RevealStartPx/EndPx` = 🔶 我们挑的（已在代码里标）。'
   '⇒ 本行**不建议**在 `CardDisplayWindow` 里另加偏移（那会造第二条常驻路径）。',
 'GetCurrentLocalizedAbilityDescription()':
   '我们的描述 = `CardText.EffectSentence(EffectSpec)`（由引擎解析的效果树现拼），'
   '原版那三档是**按卡 id 取的官方键**（`-load` / `-effect` / 裸键 + `Card_Abilities/GenericSiegeText`）'
   '⇒ 形态不同，缺的是三档后缀链。'
   '🔴 **2026-10-27（B2AU）现读原版（`decomp_full/CardScript__GetCurrentLocalizedAbilityDescription.c` + ILSpy `:17102-17157`）**：'
   '`if (DoesAbilityRequireLoading()) { if (!activeAbilityLoaded) return LocalizedAbilityDesc("-load") ?? ModifyLocalization(Loc("Card_Abilities/GenericSiegeText"));'
   ' else return LocalizedAbilityDesc("-effect") ?? LocalizedAbilityDesc(); }`（不带 loading 时才走 `needsLoading && HasRemovedTrait(reload) → RemovingSiege`）。'
   '**数据我们已经有**：`CardDef.ActiveAbilityNeedsLoading`（v21，`CardDef.cs:239`）、`ConditionEval.ReloadTrait = "reload"`；'
   '**卡住的是入口**：`SkillPanel.Show(cardName, EffectSpec, …)` **收不到 cardId / needsLoading / loaded** ⇒ 加形参就得改调用点（`BattleDriver`）。',
 'GetEnemyCardPlayedDisplayPosition()':
   '🔴 **2026-10-27（B2AU）就地订正落点指法**（同 `DisplayOffensiveCardInCenter`）：这是**卡本体**的展示位，'
   '不是放大窗。公式（原版实读）：`enemyHand.transform.position + right×enemyCardDisplayXoffset(全局变量 +0x54) − up×enemyCardDisplayYoffset(+0x50)`，'
   '移动端把 X 换成 `+0x58`；`right/up` 取自静态块 `+0x3c / +0x18`。'
   '我们这一侧的载体 = `BattleDriver` 的 `DisplayEnemyCardInCenter` / reveal 链（带 `GetEnemyCardPlayedDisplayScale`）；'
   '残差同样是「`enemyHand` 世界点 → 我们的点」= 🔶。',
 'GetOffensiveCardPlayedDisplayPosition()':
   '🔴 **2026-10-27（B2AU）就地订正落点指法**：同族。公式：`enemyHand.position + right×offensiveCardDisplayXoffset(+0x60 = −25) − up×offensiveCardDisplayYoffset(+0x5c = 30)`，'
   '移动端 X 换 `+0x64`（数值已由 `G7`/`A19` 读出并写进 `BattleDriver.cs:9650-9656` 的注释）。'
   '载体现已在 `BattleDriver` 的 reveal 链，`RevealStartPx/EndPx` = 🔶（那一层世界点映射拿不到判据）。',
}

NEW_NOTE = {
 'SetupActiveSkill(CardScript parentCard)':
   '[B2AU 2026-10-27] 更正：生成器那一半**已经做了**（`gen_cards_truth.py:308-311` 在 ability 级 emit）；'
   '剩 `CardDef`/`CardDatabase`（E1RNG）落 ability 级 + `BattleDriver.ShowSkillPanel`（E4BDR）改读。'
   '池内 17 张该拼（B8_BL 口径不变）。',
 'SetupTutorialObj(RawCardScript rawCardData, bool forceHide)':
   '[B2AU 2026-10-27] 复验仍不建（理由同前，且源码里确实没有这件）；转教程线。',
 'AutoProcessDone()':
   '[B2AU 2026-10-27] 复验：结论不变。改动请求 = 录像头加「本次选择候选集」（CardDef id 数组，'
   '写点在 `BattleDriver.BeginAsk` 那一跳）；⛔ 别只画面板不摆候选。',
 'Setup(List<RawCardScript> chooseCards, string actingCardId, int timerCounter, int target)':
   '[B2AU 2026-10-27] 并入 `AutoProcessDone`；回放分支接上后复核标题回落链仍在（现在已在）。',
 'DisplayOffensiveCardInCenter(float timeToDraw, float stepTime)':
   '[B2AU 2026-10-27] 落点改指 `BattleDriver` reveal 链（见差异列）；`CardDisplayWindow` 不是它的载体。'
   '⛔ 别再往放大窗里加第二套偏移（`CLAUDE.md`「两处写同一条规则」）。',
 'GetCurrentLocalizedAbilityDescription()':
   '[B2AU 2026-10-27] 精确改法（给 E4BDR）：`SkillPanel.Show` 加**可选**形参 '
   '`string cardId = null, int needsLoading = CardDef.GateUnknown, bool abilityLoaded = false, bool hasRemovedReload = false`，'
   '再在描述取值处按原版三档走 `Card_Description/<id>-load` / `-effect` / `Card_Abilities/GenericSiegeText`；'
   '`CardDef.ActiveAbilityNeedsLoading` 数据现成。⚠️ 键名后缀（`-load` / `-effect`）是从 ILSpy `:17109/:17120` 直读的；'
   '`RemovingSiege` 那一支的后缀没解出来（`GetLocalizedAbilityDescRemovingSiege` 内部还拼了 5 个常量）⇒ 要做那一档得先解它。',
 'GetEnemyCardPlayedDisplayPosition()':
   '[B2AU 2026-10-27] 落点改指 `BattleDriver` 的 `DisplayEnemyCardInCenter` / reveal 链；残差 = enemyHand 世界点映射（🔶）。',
 'GetOffensiveCardPlayedDisplayPosition()':
   '[B2AU 2026-10-27] 落点改指同一 reveal 链；`−25/30` 两个偏移值已读，缺的只是世界点映射（🔶）。',
}

out_rows = []
for r in rows:
    m = r[I_METHOD]
    if m not in NEW_DIFF:
        continue
    r = list(r)
    r[I_DIFF] = NEW_DIFF[m]
    r[I_NOTE] = NEW_NOTE[m]
    r[I_ROUND] = 'B9_B2AU2'
    out_rows.append(r)

assert len(out_rows) == 8, len(out_rows)

with io.open(OUT, 'w', encoding='utf-8', newline='') as f:
    w = csv.writer(f, delimiter='\t', lineterminator='\n', quoting=csv.QUOTE_MINIMAL)
    w.writerow(hdr)
    for r in out_rows:
        w.writerow(r)

with io.open(OUT, encoding='utf-8', newline='') as f:
    chk = list(csv.reader(f, delimiter='\t'))
print('rows', len(chk), 'cols', [len(x) for x in chk])
for x in chk[1:]:
    print(' 方法=', x[I_METHOD][:52], '| 状态=', x[I_STATUS], '| 轮次=', x[I_ROUND],
          '| diff', len(x[I_DIFF]), '| note', len(x[I_NOTE]))
