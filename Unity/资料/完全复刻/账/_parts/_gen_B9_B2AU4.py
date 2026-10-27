# -*- coding: utf-8 -*-
# 生成第五批账补丁 _parts/B9_B2AU4.tsv（只读主表；改 备注/轮次；状态不动）
import csv, io

LEDGER = 'D:/4/Unity/资料/完全复刻/账/音频_语音.tsv'
OUT = 'D:/4/Unity/资料/完全复刻/账/_parts/B9_B2AU4.tsv'

with io.open(LEDGER, encoding='utf-8', newline='') as f:
    rd = csv.reader(f, delimiter='\t')
    hdr = next(rd)
    rows = [r for r in rd]
I = {h: i for i, h in enumerate(hdr)}

EV = {
 'Blend(AudioClip audioClip)':
   '`Core/AudioSourcesController.cs:160` —— `FadeOut()` → `SwitchMainAudioSource()` → `FadeIn(clip)`（两源交叉，逐句照 `__Blend.c`）',
 'DecrementVolume(AudioSource audioSource)':
   '`Core/AudioSourcesController.cs:236` —— `VolumeLerp(src, 1→0)`，到点写 `volume = 0` 且 `enabled = false`（`__DecrementVolume.c` + MoveNext 末句）',
 'IncrementVolume(AudioSource audioSource)':
   '`Core/AudioSourcesController.cs:222` —— `VolumeLerp(src, 0→1)`，到点写 `volume = 1`',
 'FadeIn(AudioClip audioClip)':
   '`Core/AudioSourcesController.cs:138` —— `PlayClip` 后起 `IncrementVolume`（3 s 0→1）',
 'FadeOut()':
   '`Core/AudioSourcesController.cs:151` —— 取当前主源起 `DecrementVolume`（3 s 1→0，到点 `enabled=false`）',
 'SetMaxVolume(AudioSource audioSource)':
   '`Core/AudioSourcesController.cs:229` —— `volume = 1.0`。⚠️ 原版**全库无调用点**（`grep -rl SetMaxVolume` = 1，只有自己）⇒ 保持 private、照抄形状',
 'SetMinVolume(AudioSource audioSource)':
   '`Core/AudioSourcesController.cs:243` —— `volume = 0.0`。⚠️ 同上，原版无调用点',
 'Stop()':
   '`Core/AudioSourcesController.cs:170` —— `enabled = false` + `Stop()`（`__Stop.c:6,8`）',
 'SwitchMainAudioSource()':
   '`Core/AudioSourcesController.cs:184` —— `current == first ? second : first`（`__SwitchMainAudioSource.c`）；原版 private ⇒ 我们 private',
 'VolumeLerp(AudioSource audioSource, float startValue, float endValue)':
   '`Core/AudioSourcesController.cs:250` —— 3 s 线性、`t = (now − start) / LERP_DURATION` 夹 [0,1]，由 `Tick(now)` 每帧推（原版是 `Task`，见文件头「执行模型」）',
 'CheckQueue(string sceneTarget)':
   '`Shell/ShellRuntime.cs:647` —— 场景切换取消排队下一曲；两个调用点（`LoadSceneWithFade` 切之前 + `OnSceneLoadedApplyChrome` 覆盖直调 LoadScene 的路径）',
 'DelayedQueuedPlay(AudioClip musicClip, float scheduledTime)':
   '`Shell/ShellRuntime.cs:379-392` —— `TickMusic` 每帧判「到点」⇒ `PlayMusic(clip, Blend)` 交叉淡变（原版 `WaitForSeconds` 的等价物，🔶 机制等价已在代码里标）',
 'PlayMusic(AudioClip musicClip, MusicTransitionType type)':
   '`Shell/ShellRuntime.cs:1226-1250` —— 三档 `FadeIn/Blend/Instant` 各自转 `_musicSources`，**其他值照原版 throw**（`ArgumentOutOfRangeException`）。'
   '66 的响度裁定已落地：`0.2` 移到 `WarpforgeAudio.MusicVolumeMultiplier`（乘进 mixer），源侧不再乘',
 'PlayMusicQueued(AudioClip musicClip)':
   '`Shell/ShellRuntime.cs:1258-` —— `delay = clip.length − source.time − 3.0`，下限 0、上限不夹；句柄 `_queuedMusic`',
 'StopMusic(bool instant)':
   '`Shell/ShellRuntime.cs:1303` —— `instant ? Stop() : FadeOut()`；原版**不清排队句柄**（清排队是 `CheckQueue` 的事），照反编译做。'
   '🔴 **接线缺**：壳侧入口 `StopMenuMusic()`（`:1316`）**零调用点** ⇒ 「进战斗时菜单 idle 与战斗 BGM 叠放」这个原症状**今天仍会发生**，'
   '需要 `BattleDriver.Begin` 那一跳调一次（`E4BDR` 的文件，已写「待接线」）',
 'Start()':
   '`Shell/ShellRuntime.cs:1064-1068` —— `PlayerPrefs.GetFloat(WarpforgeAudio.MusicPrefKey, 源自身音量)` ⇒ `volume = 源音量 × v`'
   '（`MusicVolumePlayerPrefsSetting__Start.c:16-27`）；键名 `"MusicVolume"` 在 `Core/WarpforgeAudio.cs:26`，另一处读点在 `:52`',
}

NEW_NOTE_HEAD = '[B2AU 2026-10-27 第五批] 逐条复核（不是抽查）：'

out = []
for r in rows:
    m = r[I['方法']]
    if m.startswith('DisplayWarlordChatMessage('):
        r = list(r)
        r[I['备注']] = ('[B2AU 2026-10-27 第五批] **A-06 那一半（Net 转发）已做**：'
                        '① `Net/NetChat.cs` 的 `ShouldRecord` 注释「4 = HurryUp」→ **4 = Concede**'
                        '（枚举 0 Intro/1 MirrorMatch/2 Bored/3 ICantDoThat/4 Concede/5…10 Greet…Oops）；'
                        '② 发送闸从 `IsButtonLine(5…10)` **收窄成只用 `IsRelayable`**（`Net/NetBattle.cs:1248`，'
                        '判据 = 本文件 `:65` 只有 0/1 两道闸）⇒ `chatId=4`（认输）**现在发得出去**，'
                        '与 `E4BDR` 落的那半（`BattleDriver.OnLocalChat(4)`）**两侧同时生效**；'
                        '③ 收侧仍保留 `IsButtonLine`（`NetBattle.cs:1650`，防伪造越界）—— 两侧的闸本来就不是同一条，⛔ 别统一。'
                        '⚠️ **本行状态不动**：显示链本体（气泡/官方词条/语音那几支）不归 A-06。')
        r[I['轮次']] = 'B9_B2AU4'
        out.append(r)
        continue
    if m in EV and r[I['原版类']] in ('AudioSourcesController', 'MusicManager', 'MusicVolumePlayerPrefsSetting'):
        r = list(r)
        r[I['备注']] = NEW_NOTE_HEAD + '判据与实现逐条对上，**维持 A**。证据 = ' + EV[m] + \
                       '。' + (r[I['备注']] if r[I['备注']] else '')
        r[I['轮次']] = 'B9_B2AU4'
        out.append(r)

assert len(out) == 17, len(out)   # 16 条 A-01 + 1 条 A-06
with io.open(OUT, 'w', encoding='utf-8', newline='') as f:
    w = csv.writer(f, delimiter='\t', lineterminator='\n', quoting=csv.QUOTE_MINIMAL)
    w.writerow(hdr)
    for r in out:
        w.writerow(r)

with io.open(OUT, encoding='utf-8', newline='') as f:
    chk = list(csv.reader(f, delimiter='\t'))
print('rows', len(chk), 'cols', sorted({len(x) for x in chk}))
for x in chk[1:]:
    print(' ', x[I['状态']], '|', x[I['方法']][:44], '| note', len(x[I['备注']]))
