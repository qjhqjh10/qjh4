# -*- coding: utf-8 -*-
# 生成第四批账补丁 _parts/B9_B2AU3.tsv（只读主表；改 我们的落点/差异/备注/轮次；状态不动）
import csv, io

LEDGER = 'D:/4/Unity/资料/完全复刻/账/音频_语音.tsv'
OUT = 'D:/4/Unity/资料/完全复刻/账/_parts/B9_B2AU3.tsv'

with io.open(LEDGER, encoding='utf-8', newline='') as f:
    rd = csv.reader(f, delimiter='\t')
    hdr = next(rd)
    rows = [r for r in rd]
assert len(hdr) == 11, hdr
I = {h: i for i, h in enumerate(hdr)}

CH = {}

CH['GetSnapshot(SnapshotsType snapshotType)'] = dict(
  land='CardPresentation/Core/WarpforgeAudio.cs（类尾新增 `GetSnapshot(SnapshotsType)` + `SnapshotsType` 枚举 + '
       '`SnapshotNames` 表 + `SnapshotMisses` 读口；`ToggleSFX` 在同一段）',
  diff='原版 `AudioMixerAddresableReferences.GetSnapshot`（ILSpy `:90-` 起）遍历的是 `AudioMixerReference<SnapshotsType, '
       'AudioMixerSnapshot>[] snapshots` 那张 SO 表，取第一个类型匹配的并返回；一个都不匹配 ⇒ 返回 null（不抛）。'
       '我们按**名字** `mixer.FindSnapshot(name)` 取（🔶 六个名字是我们定的，见下）。'
       '🔴 **资产侧还没做**：`Resources/Audio/Main Mixer.mixer` 现读 `m_Snapshots` 只有 1 条 = 默认块、'
       '也没有 `BattleGroup Low/Medium/High` ⇒ 本函数**今天恒返回 null**，但**每次缺件都出声一次**（按名字去重，'
       '`SnapshotMisses` 可断言）—— 不静默。',
  note='[B2AU 2026-10-27] `.cs` 侧已落（typecheck 0/0）。**阻塞**：建快照必须跑 Unity 编辑器脚本'
       '（`Editor/AudioSetup.cs` / `工具/setup_audio_mixer.py`）⇒ 只有主对话能跑。'
       '建的时候名字**必须**与 `WarpforgeAudio.SnapshotNames` 一致：`SFX On` / `SFX Off` / `Music Low` / '
       '`Music Medium` / `Music High` / `Music None`（🔶 我们自己定的六个名字 —— 原版真名在那个未导出的 SO 里）。'
       '`SnapshotsType` 六个枚举值是从 `dump.cs:17953-17962` 逐字抄的，不是猜的。')

CH['ToggleSFX(bool option, bool instant)'] = dict(
  land='CardPresentation/Core/WarpforgeAudio.cs（类尾新增 `ToggleSFX(bool option, bool instant = false)`）',
  diff='逐字照原版 `SoundManager.ToggleSFX`（ILSpy `:596-602`）：`snap = option ? FxNormal : FxNoVolume`，'
       '`timeToReach = ((instant ? 1 : 0) ^ 1) * 2f`（⇒ **instant 0 s / 否则 2 s**），`snap.TransitionTo(timeToReach)`。'
       '⚠️ 依赖上一条：快照取不到 ⇒ **什么都不做**（`GetSnapshot` 已出声）⇒ 今天切开关**没有任何听觉变化**。',
  note='[B2AU 2026-10-27] `.cs` 侧已落（typecheck 0/0）；验收要等主对话把那两个快照建进 mixer '
       '（`AudioMixer.FindSnapshot("SFX Off") != null` 是派活包给的验收口）。'
       '⚠️ 调用点（`Shell/SettingsWindow.cs` 的 SFX 开关）不在本波白名单 ⇒ 接线由 `B1UI` 负责。')

CH['AnimationFinished()'] = dict(
  land='CardPresentation/Effects/UIEffectTogglerPlaySound.cs（新建，`AnimationFinished` 覆写）',
  diff='逐句照原版（ILSpy `UIEffectTogglerPlaySound.cs:14-24`）：两槽**都** `Reset()`（= `numberOfTimesPlayed = 0`），'
       '空槽跳过。⚠️ **断言缺口**：`PlaySoundOnTime.numberOfTimesPlayed` 是 **private**（`AnimFXController.cs:182`）'
       '⇒ 「两槽归零/递增」这两条断言**读不到**。改法（不在本波白名单，2 行、追加在 `PlaySoundOnTime` 类尾，'
       '⛔ 别重构它）：`public int PlayedCountForTest { get { return numberOfTimesPlayed; } }`。'
       '本类已自备等价读口 `ResetCount` / `DrivenOnCount` / `DrivenOffCount`（🔶 我们加的，原版没有）。',
  note='[B2AU 2026-10-27] 新文件已建、typecheck 0/0；`deactivateOnDisable` 走基类默认 true（= 原版构造函数那一句）。')

CH['DoUpdate(float currentTime, float normalizedTime)'] = dict(
  land='CardPresentation/Effects/UIEffectTogglerPlaySound.cs（新建，`DoUpdate` 覆写）',
  diff='逐句照原版（ILSpy `:9-12`）：**只推目标态那一个槽** —— `(!targetStateEnabled ? offSound : onSound)?.Update(currentTime, null)`；'
       '空槽什么都不做（不是「推空槽」）。⚠️ 第二实参原版就是 **null** ⇒ `PlaySoundOnTime.Update` 里那条 '
       '`!is2d && transform != null` 会落 **2D** 分支，我们照抄。两槽做成 `public` 字段（原版 private+预制体序列化；'
       '我们的 UI 件是代码里建的，同 `UIEffectTogglerImage.fadeTargets` 口径）⇒ 空槽 = 这一档不出声，`Ready` 可判。',
  note='[B2AU 2026-10-27] 与 82 同一文件；接线方 = 建 `UIEffectTogglerController` + 本件、赋两槽。'
       '⛔ 没在本文件抄第二份 `PlaySoundOnTime`（复用 `AnimFXController.cs` 里那个顶层类）。')

out = []
for r in rows:
    m = r[I['方法']]
    if m not in CH:
        continue
    r = list(r)
    c = CH[m]
    r[I['我们的落点']] = c['land']
    r[I['差异']] = c['diff']
    r[I['备注']] = c['note']
    r[I['轮次']] = 'B9_B2AU3'
    out.append(r)

assert len(out) == 4, len(out)
with io.open(OUT, 'w', encoding='utf-8', newline='') as f:
    w = csv.writer(f, delimiter='\t', lineterminator='\n', quoting=csv.QUOTE_MINIMAL)
    w.writerow(hdr)
    for r in out:
        w.writerow(r)

with io.open(OUT, encoding='utf-8', newline='') as f:
    chk = list(csv.reader(f, delimiter='\t'))
print('rows', len(chk), 'cols', [len(x) for x in chk])
for x in chk[1:]:
    print(' ', x[I['方法']][:46], '| 状态=', x[I['状态']], '| 轮次=', x[I['轮次']],
          '| diff', len(x[I['差异']]), '| note', len(x[I['备注']]))
