# -*- coding: utf-8 -*-
# 第六批：音频_语音.tsv 里 34 条 状态=C 的逐条判定 → 写 _parts/B9_B2AU5.tsv
# 只读主表；改 状态 / 备注 / 轮次（差异列只在需要时补）。
import csv, io

LEDGER = 'D:/4/Unity/资料/完全复刻/账/音频_语音.tsv'
OUT = 'D:/4/Unity/资料/完全复刻/账/_parts/B9_B2AU5.tsv'

with io.open(LEDGER, encoding='utf-8', newline='') as f:
    rd = csv.reader(f, delimiter='\t')
    hdr = next(rd)
    rows = [r for r in rd]
I = {h: i for i, h in enumerate(hdr)}

V = {}   # (类, 方法) -> (状态, 备注)

def put(cls, meth, st, note):
    V[(cls, meth)] = (st, note)

put('AudioMixerAddresableReferences', 'GetSnapshot(SnapshotsType snapshotType)', 'B',
    '[B2AU 2026-10-27 第四批] `.cs` 侧已落：`Core/WarpforgeAudio.cs` 类尾新增 `SnapshotsType`（六个值逐字抄 `dump.cs:17953-17962`）'
    '+ `SnapshotNames` + `GetSnapshot`（`FindSnapshot` + 缺件**出声一次**，`SnapshotMisses` 可断言）。'
    '🔴 **判 B 的原因 = 资产侧缺**：`Resources/Audio/Main Mixer.mixer` 现读 `m_Snapshots` 只有默认块、无 BattleGroup ⇒ 今天恒 null。'
    '建快照必须跑 Unity（`Editor/AudioSetup.cs` / `工具/setup_audio_mixer.py`），名字须与 `SnapshotNames` 一致。')

put('SoundManager', 'ToggleSFX(bool option, bool instant)', 'B',
    '[B2AU 2026-10-27 第四批] `.cs` 侧已落：`WarpforgeAudio.ToggleSFX`（`instant ? 0 : 2` s `TransitionTo`，判据 ILSpy `SoundManager.cs:596-602`）。'
    '依赖快照（行 12）⇒ 今天什么都不做但已出声。调用点 = `Shell/SettingsWindow.cs`（B1UI，不在本波白名单）。')

put('UIEffectTogglerPlaySound', 'AnimationFinished()', 'A',
    '[B2AU 2026-10-27 第四批+第五批] 新文件 `Effects/UIEffectTogglerPlaySound.cs` 逐句照原版（两槽都 `Reset()`）；'
    '断言读口 `PlaySoundOnTime.PlayedCountForTest` 同批补在 `AnimFXController.cs` 类尾（2 行、行为零改动）。')

put('UIEffectTogglerPlaySound', 'DoUpdate(float currentTime, float normalizedTime)', 'A',
    '[B2AU 2026-10-27 第四批] 同文件 `DoUpdate`：**只推目标态那一个槽**，第二实参恒 `null`（原版如此 ⇒ 落 2D 分支）；'
    '两槽做成 public 字段（我们代码建件，同 `UIEffectTogglerImage.fadeTargets` 口径）。')

put('BattleCardSounds', 'GetInvokeCue(int cardRarity)', 'A',
    '[B2AU 2026-10-27 第六批] **E4BDR 同一天已修**（差异列写着：`rarity == "legendary" && !Has(Ambush)`，'
    '判据 `CardToMinionAnim.c:20-26` + `GetInvokeCue.c:5-8` 只在 `num == 4` 给 legendary ⇒ 伏击单位走普通 cue）。'
    '本轮只把状态从 C 补成 A（可达性仍为 0：池内 0 张 ambush ⇒ 必须夹具验）。')

for meth, note in [
  ('HideChatBox(VoiceLinesOnSceneType type)',
   '按颗关一颗（`HideBox(int)`）+ 五步照 `UnitsVoiceLinesPanel__HideChatBox.c:9-25`；`HideAll` 改走它'),
  ('IsAnyVoiceLinePlaying()',
   '`Bubble.isPlaying`（= 原版 `VoiceLineBox +0x40`）取代 `audio.isPlaying`；纯文字气泡也算在播'),
  ('FinishChat(float duration)',
   '寿命与打字机改**并发**（原判据读反了）；`Advance(10f)` 大步仍能收'),
  ('HideChat()',
   '`HideBubble` 补 `Stop()` + `maxVisibleCharacters=0`；`AudioSource` 挂到**每颗气泡自己的底图 GO**'),
  ('ShowChat()',
   '起播前 0.1 s 等一拍 —— **触发条件已查清**：`+0x44` = 打字机已显字数 > 0（= 上一句打字被打断），⛔ 不是「有语音就等」'),
]:
    put('VoiceLineBox' if meth != 'HideChatBox(VoiceLinesOnSceneType type)' and meth != 'IsAnyVoiceLinePlaying()'
        else 'UnitsVoiceLinesPanel', meth, 'A',
        '[B2AU 2026-10-27 第一批（A-04）] `Battle/UnitChatPanel.cs`：' + note + '。判据与逐条证据见 `报告/B9_B2AU_聊天面板音效.md` §②。')

put('VoiceLinesController', 'IsAnyVoiceLinePlaying()', 'A',
    '[B2AU 2026-10-27 第一批（A-04 · 账 124）] 与 87 同一处改动（`UnitChatPanel.IsSpeaking` 读 `isPlaying`）；'
    '消费点 `BattleDriver.SpeakCantDo` 的闸同批自动对齐原版。')

for cls, meth, note in [
  ('VoiceLinesController', 'CanChat(bool isPlayer)', ''),
  ('VoiceLinesController', 'CanUseVOs(CardTypeOptions cardType)', ''),
  ('VoiceLinesController', 'CloseChatBox(bool isPlayer)', ''),
  ('VoiceLinesController', 'DisplayWarlordChatMessage(ChatMessage chatMsgIndex, bool isPlayer, bool ignoreUiState)', ''),
  ('VoiceLinesController', 'DisplayWarlordRegularChatMessage(ChatMessage chatMsgIndex, bool isPlayer)', ''),
  ('VoiceLinesController', 'GenerateVoiceLineData(ChatMessage chatMessage, bool isPlayer)', ''),
  ('VoiceLinesController', 'SetPopUpDisabled()', ''),
  ('VoiceLinesController', 'SetPopUpEnabled()', ''),
  ('VoiceLinesController', 'SetTransformedWarlordNewChats(RawCardScript selectedWarlord, MatchData matchData, bool updateAvailable)', ''),
  ('VoiceLinesController', 'ShowHeroesIntroMessage()', ''),
]:
    put(cls, meth, 'A',
        '[B2AU 2026-10-27 第六批] **A-05（E4BDR）已落**；本轮现核：`BattleDriver.cs` 里该族标记均有命中'
        '（`CanChat` 30 · `CanUseVOs` 8 · `CloseChatBox` 19 · `DisplayWarlordRegularChatMessage` 8 · '
        '`GenerateVoiceLineData` 2 · `SetPopUpDisabled/Enabled` 9 · `SetUpChatOptions` 1 · `ShowHeroesIntroMessage` 12 · '
        '`SetTransformedWarlordNewChats` 6 · `IsAnyVoiceLinePlaying` 1 处）。只把状态补成 A。')

for meth, note in [
  ('Hide()', '淡变曲线改 **OutCubic**（`1-(1-t)³`，`t=0.5 ⇒ α=0.875`），判据 `__Hide.c:34-37` 的 `SetEase(9)`'),
  ('IsAvailable(MatchData matchData, string warlordId, int chatMessageIndex, MatchType[] possibleTypes)',
   '`ChatPopupPanel.IsAvailable` + 3 元素表 `{110,120,80}`；原版**当前不可达** ⇒ 夹具验（账里已写）'),
  ('IsIndexOutOfEnumBounds(int chatMessageIndex)',
   '`ChatPopupPanel.IsIndexOutOfEnumBounds` + `ChatMessageEnumCount = 11`（⛔ 不用 `ChatTerms.Length`）'),
  ('IsMatchOneOfCollection(MatchType matchType, MatchType[] possibleTypes)',
   '`ChatPopupPanel.IsMatchOneOfCollection`（LINQ `Any` 语义：null/空 ⇒ false）'),
]:
    put('VoiceLinesPopupSelector', meth, 'A',
        '[B2AU 2026-10-27 第一批（A-03）] `Battle/ChatPopupPanel.cs`：' + note + '。')

for meth, note in [
  ('DisplayWarlordChatMessage(ChatMessage chatMsgIndex, bool isPlayer, bool ignoreUiState)',
   '**A-06 的 Net 那一半已做**（发送闸 `IsButtonLine`→`IsRelayable`，`Net/NetBattle.cs:1248`；收侧保留）⇒ `chatId=4` 认输发得出去'),
]:
    pass

B_NOTE = {
 'ForcePopUpClose()':
   '`ChatPopupPanel.ForcePopUpClose(bool)` 已实现（`+0x78==0` 闸 = `Visible`）；**缺调用点**：原版在 `ChoiceOfCardPlayer`'
   '（开选牌面板那一跳）—— `BattleDriver`（E4BDR）加一行 `if (_chatPopup != null) _chatPopup.ForcePopUpClose(...);`',
 'SetUpChatOptions(MatchData matchData)':
   '`ChatPopupPanel.SetChatContext(warlordId, matchType)` + `SetupChatOptionTypes`（3 元素表）已实现；'
   '**缺接线**：`BattleDriver` 建面板后一行 `_chatPopup.SetChatContext(myDeck?.WarlordId, Ctx.MatchType);`（E4BDR）',
 'RefreshChatLines()':
   '`ChatPopupPanel.RefreshChatLines()` 已在 `Build()` + `Show()` 各调一次；**缺督军数据**（同上那行接线）'
   '⇒ 今天仍印分类词（与改前逐字相同，故不红）',
 'SetTransformedWarlordNewChats(RawScript selectedWarlord, MatchData matchData, bool updateAvailable)':
   '（VoiceLinesController 侧）A-05 已落',
 'SetTransformedWarlordNewChats(RawCardScript selectedWarlord, MatchData matchData, bool updateAvailable)':
   '`ChatPopupPanel.SetTransformedWarlordNewChats(warlordId, updateAvailable)` 已实现（2 元素表 `{110,120}`，与 Setup 的 3 元素表'
   '**故意不统一**＝原版如此）；**上游「督军换卡」整条还没复刻** ⇒ 今天零调用者',
 'SetupChatOptions(MatchData matchData)':
   '`ChatPopupPanel.ApplyChatLines` + 3 元素表 + `interactable` 越界支已实现；与 134 同一行接线',
 'Show()':
   '`ChatPopupPanel.Show()` 里先 `RefreshChatLines()` 再开淡入 + OutCubic 已实现；与 150 同一行接线（缺督军数据）',
}

out = []
for r in rows:
    if r[I['状态']].strip() != 'C':
        continue
    cls, m = r[I['原版类']], r[I['方法']]
    if (cls, m) in V:
        st, note = V[(cls, m)]
        r = list(r); r[I['状态']] = st; r[I['备注']] = note; r[I['轮次']] = 'B9_B2AU5'
        out.append(r); continue
    if m in B_NOTE:
        r = list(r); r[I['状态']] = 'B'; r[I['备注']] = '[B2AU 2026-10-27 第六批] ' + B_NOTE[m]; r[I['轮次']] = 'B9_B2AU5'
        out.append(r); continue
    if m in ('ClosePopupHorizontal()', 'OpenPopupHorizontal()', 'Toggle(bool option)'):
        r = list(r); r[I['状态']] = 'D'
        r[I['备注']] = ('[B2AU 2026-10-27 第六批] 判 **D（铁律 11 例外①：原版本身不可达）**，本轮已复现：'
                        + ('`grep -rl ClosePopupHorizontal d:/2/tools/decomp_full` = **1**（只有它自己）；' if m.startswith('Close') else
                           '`grep -rl OpenPopupHorizontal d:/2/tools/decomp_full` = **1**（只有它自己）；' if m.startswith('Open') else
                           '`head -3 VoiceLinesPopupSelector__Toggle.c` 的内容是 **`Card2DController__Toggle`**（文件名与内容不符）；') +
                        '时间限定 = 2026-10-27 现读（本 build）。⛔ 不是「不做」，是「原版这一跳没有调用者/判据为空」。')
        r[I['轮次']] = 'B9_B2AU5'
        out.append(r); continue
    raise SystemExit('未判定的行：%s | %s' % (cls, m))

assert len(out) == 34, len(out)
with io.open(OUT, 'w', encoding='utf-8', newline='') as f:
    w = csv.writer(f, delimiter='\t', lineterminator='\n', quoting=csv.QUOTE_MINIMAL)
    w.writerow(hdr)
    for r in out:
        w.writerow(r)

with io.open(OUT, encoding='utf-8', newline='') as f:
    chk = list(csv.reader(f, delimiter='\t'))
from collections import Counter
print('rows', len(chk), 'cols', sorted({len(x) for x in chk}), Counter(x[I['状态']] for x in chk[1:]))
