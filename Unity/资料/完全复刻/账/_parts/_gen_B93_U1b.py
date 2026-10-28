# -*- coding: utf-8 -*-
# B93_U1b — 调度台 2026-10-28 裁定 ①② 的落地补丁（11 列 · 只写这 6 行 · 裸 split("\t")）
import io

MAIN = 'd:/4/Unity/资料/完全复刻/账/未分类_其他.tsv'
OUT = 'd:/4/Unity/资料/完全复刻/账/_parts/B93_U1b.tsv'
TAG = '｜[B93_U1b 2026-10-28] '

b = io.open(MAIN, 'rb').read().decode('utf-8')
lines = b.split('\n')
hdr = lines[0].split('\t')
assert len(hdr) == 11, hdr
idx = {}
for l in lines[1:]:
    if not l.strip():
        continue
    f = l.split('\t')
    idx[(f[1], f[2])] = f

# ⚠️ 修复 B93_U1 的 off-by-one：那轮把「判据要点」的正文写进了【判据出处】列（r[3] 本应是 r[4]）。
# 本补丁覆盖的行同时把【判据出处】恢复成 HEAD 原值（git show HEAD:…/未分类_其他.tsv）。
HEADT = 'd:/4/_tmp_view/b93u1/main_head.tsv'
_head = {}
for l in io.open(HEADT, 'rb').read().decode('utf-8').split('\n')[1:]:
    if l.strip():
        f = l.split('\t')
        _head[(f[1], f[2])] = f

U = {}


def add(cls, meth, **kw):
    U[(cls, meth)] = kw


CORRECT_HEAD = ('｜🔴 B93_U1b 就地更正（铁律 5）：B93_U1 那轮写「`OnReset` 也是 `Signal.Register`、不是反注册」**是错的**。'
                '真相 = `OnReset` 走的是 **`Signal.Unregister`**（`out_cpp2il_dev_ilspy/<类>.cs:9-18` 与 '
                '`out_il2csharp/Assembly-CSharp/<类>.cs` **两源一致**；`dump.cs:1143072-1143130` 显示 '
                '`Signal.Register<T>(Action<T>)` 与 `Signal.Unregister<T>(Action<T>)` **同址 RVA `0xDA0A00`** ⇒ '
                '`decomp_full/*.c` 里两个调用被印成同一个符号名（`Signal__Register`），**那个名字分不出 Register/Unregister**）。'
                '⇒ 账里原判「反注册」**成立**；⛔ 别再把 `.c` 的符号名当 Register/Unregister 的判据。')

SIGNAL_RATIONALE = ('判 **A**（调度台 2026-10-28 裁 ①，理由按更正后的事实重写）：本仓**无信号总线** —— '
                    '计数由引擎在结算点直写 `ctx.Tally.*`，而 **`Tally` 每局一份**（`RuleEngine/Core/BattleContext.cs:348` '
                    '`public readonly AchievementTally Tally = new AchievementTally();`）随 `BattleContext` 一起丢弃 ⇒ '
                    '原版「`OnInitialize` 订阅 / `OnReset` 退订」这一对语义由「**每局新建**」承载'
                    '（= 已按同一口径判 A 的先例 `AddSabotageCard.OnReset`，B92_N6 2026-10-28；⛔ 别再把它当成两个待办）。')

# ---------------- ① Signal 族（4 行） ----------------
add('AddSabotageCard', 'OnInitialize()',
    status='A',
    judge='`Signal.Register(OnAddCard)`（AddSabotageCard.cs:15）｜🔴 B93_U1b：与 `OnReset`（`Signal.Unregister`，AddSabotageCard.cs:9）**成对**；'
          '两个调用在 `dump.cs` 里**同址**（RVA `0xDA0A00`）⇒ `decomp_full/*.c` 的符号名分不出来（都印成 `Signal__Register`），⛔ 别拿它判 Register/Unregister',
    land='RuleEngine/Core/AchievementTally.cs:231（AddSabotage）（等价实现 = 引擎在结算点直写 `ctx.Tally.AddSabotage(owner)`，'
         '调用点 `RuleEngine/Core/EffectResolver.cs:4221`；**生命周期 = 每局一份 `Tally`**（`RuleEngine/Core/BattleContext.cs:348`）承载原版「订阅」语义）',
    diff='原版 = 卡脚本把自己挂到事件总线 / 我们 = **不带总线的直写**（`AchievementTally.AddSabotage`，`Ach_65`）' + CORRECT_HEAD + SIGNAL_RATIONALE,
    note='A（裁 ①）。⚠️ 上一轮那句「OnReset 不是反注册」已就地更正 —— `OnReset` **是** `Signal.Unregister`；'
         'A 的理由改成与已 A 的兄弟行 `AddSabotageCard.OnReset`（B92_N6）同一口径：订阅/退订由「每局一份 `Tally`」承载')

add('CollectSpiritStone', 'OnReset()',
    status='A',
    judge='反注册（MissionChallenge 重置时摘掉回调）；`Signal.Unregister(OnCollectSpiritStone)`；CollectSpiritStone.cs:9',
    land='CardPresentation/Battle/BattleDriver.cs:19474（PlaySignals）（`:19576` = `EvtKind.CollectWaystone ⇒ _waystonesCollected[seat] += …`；'
         '`:32547` = 结算交出并清零；消费口 `Shell/ProfileData.cs:682`；**生命周期 = 每局一份 `Tally`**（`RuleEngine/Core/BattleContext.cs:348`）承载原版「退订」语义）',
    diff='退订那一跳 + 计数三跳' + CORRECT_HEAD + SIGNAL_RATIONALE,
    note='C→A（裁 ①，理由已按更正后的事实重写）。⚠️ 可观测后果为 0（现读 102 条成就里 0 条用它），但「有没有做」与「有没有后果」是两件事')

add('CreateSecret', 'OnInitialize()',
    status='A',
    judge='`Signal.Register(OnAddCard)`（`out_cpp2il_dev_ilspy/CreateSecret.cs:15-18` / `out_il2csharp/Assembly-CSharp/CreateSecret.cs:17-20`）',
    land='RuleEngine/Core/AchievementTally.cs:225（AddSecret）（等价实现 = 造出隐秘时直写 `ctx.Tally.AddSecret(owner)`，'
         '调用点 `RuleEngine/Core/EffectResolver.cs:5051` / `:13311`；**生命周期 = 每局一份 `Tally`**（`RuleEngine/Core/BattleContext.cs:348`））',
    diff='类级命中 → **方法级对齐**：原版 = `Signal.Register(OnAddCard)`；我们 = 无总线、结算点直写 ' + CORRECT_HEAD + SIGNAL_RATIONALE,
    note='B→A（裁 ①）')

add('CreateSecret', 'OnReset()',
    status='A',
    judge='`Signal.Unregister(OnAddCard)`（`out_cpp2il_dev_ilspy/CreateSecret.cs:9-13` / `out_il2csharp/Assembly-CSharp/CreateSecret.cs:11-15`；'
          '⚠️ `decomp_full/CreateSecret__OnReset.c` 里印的 `Signal__Register` 是**同址符号撞名**，不作判据）',
    land='RuleEngine/Core/BattleContext.cs:348（Tally）（等价实现 = 无持久订阅可退：`Tally` 每局新建、随局丢弃 ⇒ '
         '「重置后处理器不再触发」由结构保证；处理侧同 `OnInitialize`：`AchievementTally.cs:225`）',
    diff='类级命中 → **方法级对齐**：原版 = `Signal.Unregister(OnAddCard)`；我们 = 没有订阅残留' + CORRECT_HEAD + SIGNAL_RATIONALE,
    note='B→A（裁 ①）。⚠️ 本条判据要点原写 `cpp2il :9-13 = Signal.Unregister` —— **那条是对的**，B93_U1 那轮的更正才是错的，已回退')

# ---------------- ② 共享落点（2 行） ----------------
add('MilestoneScoreBar', 'Initialize(int currentScore, GenericScoringMilestone[] scoringMilestones, Sprite resourceIcon)',
    status='A',
    land='CardPresentation/Shell/AllianceEventScoreInfo.cs:181（SetMilestones）（**共享落点**：与 `AllianceScoreBar.Initialize` 同一段；'
         '例外依据 = §4.1「多个原版方法确实由同一段代码实现」—— ① 落点给到行 ② 「各自怎么被满足」见差异列）',
    diff='机制不同 → **A**（调度台 2026-10-28 裁 ②，按 §4.1 **共享落点例外**）：本方法 = `SetProgressBar(thresholds, currentScore)` + '
         '按下标逐行 `GenericScoreBarLine.Initialize(thresholds[i], resourceIcon)`（`decomp_full/MilestoneScoreBar__Initialize.c` 全文）。'
         '这两个动作在 `AllianceEventScoreInfo` 里分别由 ① `SetProgress:153`（滑块 + `InterpolateMilestones`）、'
         '② `Build` 里 `_scores` 五行（`:128-133` 逐行喂 `thresholds[i]` + `Skull`/`Chest` 图标）、'
         '③ `SetMilestones:181`（三态：已达⇒开箱图 / 越过⇒压暗 0.5 / 未达⇒不动）共同满足 ⇒ 同一落点覆盖两个原版方法**本来就允许 A**',
    note='B→A（裁 ②）。原「按 2026-10-25 口径留 B」作废 —— 那个口径与 §4.1 的共享落点例外冲突，以后者为准')

add('ProgressBar', 'SetProgress(ScoringDataInfo scoringDataInfo)',
    status='A',
    land='CardPresentation/Shell/AllianceEventScoreInfo.cs:153（SetProgress）（**共享落点**；外层是纯转调，比值/文本在内层）',
    diff='机制不同 → **A**（裁 ②，§4.1 共享落点例外）：外层 `.c` **没有自己的行为** —— 只把 `ScoringDataInfo` 的 `+0x10` / `+0x18` '
         '两个 float 传给内层 `SetProgress(current, max, format, useWholeNumbers)`（`decomp_full/ProgressBar__SetProgress.c` 全文 = 一句转调）。'
         '内层那个重载在本账里**是独立的 A 行**（`ProgressBar.SetProgress(int value, int maxValue, string …)`）；'
         '它的「按 current/max 算填充 + 写 `{0}/{1}`」由 `AllianceEventScoreInfo.SetProgress(float):153`（填充）+ `Score` 标签（文本）满足',
    note='B→A（裁 ②）。原「同一落点覆盖两个重载 ⇒ 留 B」作废')

# ---------------- 输出 ----------------
out = ['\t'.join(hdr)]
missing = []
for k, upd in U.items():
    if k not in idx:
        missing.append(k)
        continue
    r = list(idx[k])
    # 出处恢复（U1 把那列覆盖成了判据要点正文），判据要点写进 r[4]
    if k in _head:
        r[3] = _head[k][3]
    r[4] = upd.get('judge', r[4])
    r[5] = upd.get('status', r[5])
    r[6] = upd.get('land', r[6])
    r[7] = upd.get('diff', r[7])
    r[8] = upd.get('level', r[8])
    r[9] = (r[9] + ' · B93_U1b') if ('B93_U1b' not in r[9]) else r[9]
    r[10] = (r[10] + TAG + upd['note']) if r[10].strip() else (TAG + upd['note'])
    assert len(r) == 11, (k, len(r))
    out.append('\t'.join(r))
if missing:
    raise SystemExit('MISSING KEYS: %r' % (missing,))

io.open(OUT, 'wb').write(('\n'.join(out) + '\n').encode('utf-8'))

chk = io.open(OUT, 'rb').read().decode('utf-8')
cl = chk.split('\n')
assert cl[0].split('\t') == hdr, 'header mismatch'
n = 0
for l in cl[1:]:
    if not l.strip():
        continue
    f = l.split('\t')
    assert len(f) == 11, ('cols', len(f))
    assert (f[1], f[2]) in idx, ('key', f[1], f[2])
    assert f[5] == 'A', ('not A', f[1], f[2], f[5])
    n += 1
assert '\r' not in chk
print('rows written', n)
