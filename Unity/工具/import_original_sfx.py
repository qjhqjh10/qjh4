#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""import_original_sfx.py — 把 **AnimFX `sounds` / `exitSounds` 指的那批原版音效**导进 Unity 工程

背景：每个特效的 `AnimFXController` 上有一组「特效开始后第 T 秒播一条声音」的定时条目
（`sounds[i].{time,sound,is2d,repeat,loops,timeInterval}`，另有 `exitSounds[]`）。
`sound` 指向的**不是 clip，是一个随机化 cue 包装**（`bundle_soundcollection_assets_all`
里同名的 MonoBehaviour），里面的 `clipList` 才是真 clip。

产出两块：

  ① **音频**：`MyGame/Assets/CardPresentation/Resources/Art/audio/sfx/<clip 名>.wav|ogg`
     —— `Resources/Art/` 整个被 `.gitignore` 排除（原版资产不进仓库，和美术/语音同一条规矩）。
     🆕 **2026-10-24（G4）**：拷完**顺手补 `<clip 名>.wav.meta`** —— 共用实现 =
     `工具/_sfx_meta.py`（模板 = 同目录同扩展名的众数，只换 guid；**已存在的 meta 一个字节都不碰**）。
     没有这步的话，新补进来的音频会被 Unity 用**内置默认**导入（`loadType: 0`），
     与同目录既有 510 条的 `loadType: 1 · compressionFormat: 2 · preloadAudioData: 0` 不一致。
  ② **表**：`MyGame/Assets/CardPresentation/Resources/animfx_sounds.json`
     —— **我们的分析产物，进仓库**（和 `voice_lines.json` 同一位置、同一形状）。
     键 = cue 名；值是 `clipList` + 音高/音量随机区间 + `timeToPlayAgain`。

数据源（唯一来源）：

  · bundle 本体 `d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/soundcollection_assets_all.bundle`
    （**必须读 bundle**：cue 里的 `clipList` 存的是 **PathID**，解包目录的文件名里没有 PathID ——
      实测过，`assets_full/.../MonoBehaviour/*.json` 拿不到 `pathid → 资产名` 的映射）
  · 谁在用   `d:/4/Unity/数据/游戏数据/animfx_modules.json` —— **三条路径**（`used_cues()`）：
      ① `sounds[*].sound`   ② `exitSounds[*].sound`   ③ **`collisionEvent` 的 `PlaySound` 订阅**
      （③ 是 🆕 2026-10-16 加的，见下面 `COLLISION_CALL_RE` 那段注释）
    这**三条都推不出来**的（工程按名字播、原版那格是组件上的序列化 `AudioCue`）⇒ 人手声明在
      `EXTRA_CUES` 里（🆕 2026-10-17 收 `CardStartDrag`，A910）；每次跑还会**反向查漏**一遍
      （`code_only_cue_report`：原版 cue 名 ∩ 工程 `.cs` 的音效代码行 − 已收），**只报告**。

用法：
    PYTHONIOENCODING=utf-8 python 工具/import_original_sfx.py --check   # 只报告，不写盘
    PYTHONIOENCODING=utf-8 python 工具/import_original_sfx.py           # 拷音频 + 写表
    （两种都会打印「查漏（反向）」那一节；常态应为 0 个候选）
"""
import argparse
import glob
import io
import json
import os
import re
import shutil
import sys

sys.stdout.reconfigure(encoding="utf-8")

# 🔴 **2026-10-24（G4）：补 `.meta` 的共用实现** —— 三个导入脚本（本脚本 ·
#   `import_battle_sfx.py` · `import_remnant_sfx.py`）**共用这一份**，⛔ 别在各自文件里再写一遍
#   模板/guid 逻辑。`python -I` 不会把脚本目录放进 `sys.path` ⇒ 这里显式插一下。
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import _sfx_meta                                  # noqa: E402

import UnityPy

BUNDLE_DIR = "d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64"
UNPACK = "d:/2/新解包资源/assets_full"
# 兜底：cue 名字在这些包里找（先按名字找包，找不到才退回这几个）
BUNDLE_FALLBACK = ["soundcollection_assets_all", "battleprefabs_vfxandmisc_assets_all"]
MODULES = "d:/4/Unity/数据/游戏数据/animfx_modules.json"
DST_AUDIO = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/audio/sfx"
DST_JSON = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/animfx_sounds.json"

REF_PREFIX = "@asset:MonoBehaviour:"

# 🆕 **2026-10-15（A425④）**：**窗口级 cue** —— 它**不在** `animfx_modules.json` 的 `sounds[]` 里
#   （引它的是 `RewardWindow` 的 `soundOnAppear` 序列化字段，不是 AnimFX 模块）⇒ `used_cues()` 收不到。
#   出处 = `Shell/RewardWindow.cs:409-417`（cue `Reward open item by item` → `clipList` 唯一一条 clip
#   `Add card to deck`）；判据 → `资料/普查产出_1013/A表现核_块6.md` §A425 第 ④ 步。
#   ⚠️ **只把这一份并进 `used_cues()` 的结果**，⛔ 别去动 `used_cues()` 的解析口径。
#   ⚠️ 它在 `bundle_soundcollection_assets_all` 里（同 `soundcollection_assets_all` 包）。
#
# 🔴 **2026-10-17（B21·A910）—— 这一格的口径说清楚：它不是「窗口级」，是【工程按名字播的 cue】。**
#   前三口（`sounds[]` / `exitSounds[]` / `collisionEvent`）都从 `animfx_modules.json` **推**得出来；
#   而**我们自己代码**里按名字播的 cue **推不出来**（原版是组件上的序列化 `AudioCue` 字段，
#   我们这边落成 C# 常量）⇒ **只能一条条声明在这里**。
#   判据（本轮亲核）：把 `bundle_*` 里那 **612** 个 cue 包装名 ∩ 工程 `MyGame/Assets/**/*.cs`
#   （只在**音效上下文的代码行**上算命中）− `used_cues()` ⇒ 剩 8 个名字，逐条都能指到出处。
#   `CardStartDrag`（**A910**）：原版 `DraggableController.dragSound`（两个实例共用，
#     PathID `−4308815958917459268`，`bundle_soundcollection_assets_all/MonoBehaviour/CardStartDrag.json`）
#     → 我们 `Deck/DeckRuntime.cs` 的 `const string DragCue = "CardStartDrag"`。
#     判据 → `资料/普查产出_1017/W_G3_拖拽件.md` §①/§② + `Core/DraggableController.cs:130`。
#   ⚠️ 另 7 个名字**另有产出方，别并到这里来**（理由逐条在 `CODE_ONLY_KNOWN` 里）：
#     残骸六条 = `工具/import_remnant_sfx.py` · `OvertimeStart` = `工具/rebuild_overtime_start_ogg.py`
#     （⛔ 并进来会把那条**重制过的 ogg 覆盖**掉）· `Add card to deck` 是 clip 名不是 cue 名。
# 🆕 **2026-10-17（音效线 `W_G`）—— 壳侧（主菜单/窗口/开包）的 cue，同一类「工程按名字播」。**
#   出处逐条（判据 = 反编译 + 序列化字段，全文 → `资料/普查产出_1017/UI/W_G_AudioLine.md`）：
#   · `ButtonClick`       —— `SoundManager.defaultButtonSound`（MB pid `-5308416599568047990` 的
#      `defaultCloseSound` 旁边那一格 = pid `6176553109044381692`）→ 消费者 = `EverguildButton.PlayButtonSound`
#      / `EverguildToggle.PlayButtonSound`（`useDefaultAudioClipIfNull = 1` 时）；clip `UI Select Toggle`。
#   · `ButtonClick Back`  —— `SoundManager.defaultCloseSound`（pid `8084683923307806969`）→
#      `GameWindow.Close`（`GameWindow__Close.c` 里那一跳 `SoundManager.Play2D`）；clip `UIButton Back`。
#   · `CardClick 2` / `New Card` —— `CardInBoosterPack.clickSound` / `.newCardSound`（prefab pid
#      `3481876391266756950` / `4962178071637942874`）；`UiColliderOnClick` 与 `_WaitForSpinAnimationEnd`。
#   · `Booster Pack Window Close` —— `BoosterPackOpenWindow.soundOnClose`（pid `3416723080278618344`）；
#      `CloseButtonClick`；clip `Cardshuffle out`。
#   · `Booster Pack Open Card Rarity 1..4` —— `CardInBoosterPack.contentByRarities[].audioCue`
#      （`ChangeState(3)`，**MixerType 3 = Jingles**）。
#   · `Booster Open *` —— `BoosterPackVisualItem.openBoosterPack`（`Initialize`，**MixerType 3**）。
#   ⚠️ 这一批本来报「全在远端音频表里、本地没有」（`Shell/BoosterPackOpenWindow.cs:62`）—— **不成立**：
#      12 个 cue 全在 `bundle_soundcollection_assets_all` 里，clip 名也全部解得出来（逐个写进了报告）。
EXTRA_CUES = ["Reward open item by item", "CardStartDrag",
              # —— 壳侧（2026-10-17 · W_G）——
              "ButtonClick", "ButtonClick Back",
              "CardClick 2", "New Card", "Booster Pack Window Close",
              "Booster Pack Open Card Rarity 1", "Booster Pack Open Card Rarity 2",
              "Booster Pack Open Card Rarity 3", "Booster Pack Open Card Rarity 4",
              "Booster Open Standard", "Booster Open Ultramarines", "Booster Open Sautekh",
              "Booster Open Space Wolves", "Booster Open Tyranid",
              # —— 逐按钮 cue（2026-10-17 · S01 写手 G）——
              # 原版 `EverguildButton|EverguildToggle.m_onClickSound` 的另外 8 个 cue 名；
              # 判据 = `资料/普查产出_1017/UI/R3_S01按钮cue映射.md` §二·B（10 个 cue，`ButtonClick`
              # 与 `ButtonClick Back` 上面已有）。⚠️ `Select Toggle` 的 clip 是 `UI Click 2_no delay`，
              # 与 `ButtonClick` 的 `UI Select Toggle` **不是同一个资产**。
              "ButtonClick Positive", "ButtonClick Negative", "Select Toggle",
              "SelectAvatar", "Purchase Click", "MenuStartGame",
              "UI Send Message", "UI Purchase 2",
              # —— 壳侧（2026-10-17 · `T26` / 音效线 `S02`）——
              # `Choose Army Deck FTUE Window` 的 `openSound` = PPtr pid `-1332049501696130703`
              # ⇒ cue 名 = `Draft Mode Progress Jingle`。
              # 🔴 **2026-10-17 订正（`T26` 写手现核反编译）**：它走 **FX 组（MixerType 0）**，不是 Jingles ——
              #   调用点 `GameWindow.TryOpen`（VA `0x180836120`）：`SoundManager.Play2D(AudioCue, MixerType)` 的
              #   第 2 实参 `r9d = 0`；`AudioCue` 资产本身**没有混音组字段**（组只由调用点给）。
              # 判据 = `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-3133570412519170200.json`
              # 的 `openSound` 字段 + 素材库文件名 `Draft Mode Progress Jingle_-1332049501696130703.json`。
              "Draft Mode Progress Jingle",
              # —— 音效线缺口修复（2026-10-18 · `W_音效线_缺口修复`）——
              # 判据 → `资料/普查产出_1018/AUD_音效线_完整性复核.md` §三 A1/A2/B1/D：
              #   · `CardExpand`         —— 场景 `bundle_scenes_scenes_mainmenuwarpforge` 的
              #     `Card Displayer Menu For Menu` 根 MB（`CardDisplayWindow : GameWindow`，pid **2033**）
              #     `openSound = PPtr(fileID 6, pid 4006604330728631164)`；消费点 = 继承来的
              #     `GameWindow.TryOpen`（`SoundManager.Play2D(..., MixerType 0 = FX)`）。
              #     cue 包装 `bundle_soundcollection_assets_all/MonoBehaviour/CardExpand.json`
              #     （clip `CardExpand`，pitch 0.8–1.1，vol 1–1，ttp 0.1）。
              #   · `Close Card Display` —— 同一根 MB 的 `closeSound = PPtr(fileID 6, pid 3524674544779823761)`；
              #     消费点 = `GameWindow.Close`（`GameWindow__Close.c`）。clip 同名，ttp 0.1。
              #   · `Division Change`    —— `bundle_menus_assets_all` MB `-4938671765695524530`
              #     （`RankedEventDivisionChangeWindowNewSystem : GameWindow`，prefab `Ranked Division Change Window`）
              #     的 `soundOnOpen = PPtr(fileID 6, pid -2236843390636958589)`；消费点 =
              #     `RankedEventDivisionChangeWindowNewSystem._DoPromoteAnimations_d__22.MoveNext.c:32-33`
              #     `AudioCue__Play2D(cue, 3)` ⇒ **MixerType 3 = Jingles**（⛔ 不是 FX）。
              #     clip `Division Increase`，ttp 0.01。
              "CardExpand", "Close Card Display", "Division Change",
              # —— 战斗线（2026-10-22 · L-D）：`SwarmIconController.toggleOnSound` ——
              # 判据 = `bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/
              #   MonoBehaviour_-220689081911957516.json`（= `SwarmIconController`）的
              #   `toggleOnSound` = PPtr(fileID 9, pid −4158225771113159080)
              #   ⇒ `bundle_soundcollection_assets_all` 里 `m_Name = "Swarm Trait UI Enabled"` 的 `AudioCue`
              #   （`clipList` 唯一一条 = `Tyranid_BulletHit` · pitch 0.9–1.1 · vol 1–1 · ttp 0.01）。
              # 消费点 = `CardView.PlaySwarmOnSound`（原版 `SoundManager.Play2D` ⇒ 2D）。
              # ⚠️ 原版**只有开音**：`SwarmIconController` 没有 toggleOffSound 字段，
              #   `Toggle(false)` 的方法体里也没有任何 `SoundManager` 调用。
              "Swarm Trait UI Enabled",
              # —— 特效线（2026-10-24 · `G3`）：虫群合并那一拍的 **3D 音** ——
              # 判据 = `资料/普查产出_1023e/A31_特效接线.md` §四·3 / §六 ②（原版引用者 =
              #   `SwarmVFXController.sound`；反编译 `CardScript__ExecuteSwarm.c:79` 之后
              #   `SwarmVFXController.Initialize` 里那一跳 `SoundManager.Play3D(cue)`）。
              #   cue 资产 = `bundle_soundcollection_assets_all/MonoBehaviour/Trait Swarm Tyranid.json`：
              #   clipList 唯一一条 PathID `-3423403551600047926` → `AudioClip/Trait Swarm Tyranid.wav`
              #   （44.1 kHz / 16 bit / 3.642 s）· pitch 0.8–1.24 · vol 1–1 · ttp 0.1。
              # ⚠️ 它**推不出来**：`Swarm Card Merge VFX` 不是 AnimFX 模块（没有 `AnimFXController`），
              #   音效挂在那份 prefab 的 `SwarmVFXController` 序列化字段上 ⇒ 只能声明在这里。
              #   反向查漏也独立指到了它（`WarpforgeUiFxProbe.cs:245`）。
              "Trait Swarm Tyranid",
              # —— 战斗线 / 音效线（2026-10-25 · 执行代理 `T`）：
              #   **卡牌放大窗「换位」那一声** —— `CardDisplayWindow.soundOnCardSwap`。
              #   判据（逐条亲读，2026-10-25）：
              #   · 引用者 = 放大窗根 MB。**亲扫全解包：带 `soundOnCardSwap` 的 MB 共 14 个**
              #     （13 个战场 `bundle_scenes_scenes_battlearena*` + 主菜单 `…mainmenuwarpforge`），
              #     **14 个全是同一个 pid 4812904303840730115**（例：战场版
              #     `bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4750.json`；
              #     菜单版 `bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_2033.json`）。
              #   · 消费点 = `CardDisplayWindow__ChangeCardPosition.c`（反编译，亲读 `:68-73`）：
              #     `SoundManager__Play2D(&local_38, <SoundManager 单例>, *(param_1 + 0xd8), 0, 0)`
              #     —— 第 4 实参 `0` = **MixerType.FX**；**位置在两条换位补间之前**
              #     （紧跟 `SetupTutorialObj` / `+0x150 = 1`，早于 `SetImageOverDraw` 与 DOTween Sequence）。
              #     我们的落点 = `Battle/CardDisplayWindow.cs` 的 `SwapToFront()`（`_swapping = true;` 之后、
              #     `ArtOverdraw` 那两行之前），cue 常量 = `CardDisplayWindow.SwapSoundCue`。
              #   · cue 包装 = `bundle_soundcollection_assets_all/MonoBehaviour/
              #     CardMove_4812904303840730115.json`（`m_Name = "CardMove"`）：
              #     **`clipList` 4 条** `[-6740773956303463874, 5303275677218709805,
              #     -1522524771764259241, 8024429088123589697]` ·
              #     `minPitch 0.8 / maxPitch 1.1` · `minVolume = maxVolume = 1` · `timeToPlayAgain 0.1`。
              #     🔴 **就地订正 `AU4` 报告**：它写「`minPitch=maxPitch=1`」—— 资产实读是 **0.8–1.1**
              #     （与 `CardStartDrag` 同值）；以资产为准。
              #   ⚠️ **与两张同名/近名资产划清界限**（⛔ 别混）：
              #     ① `CardStartDrag`（**另一个 cue**）的 clipList 只有 1 条，恰好就是本 cue 4 条里的第 1 条
              #        `CardMove`（**那是 clip 名**，不是 cue 名）—— 所以「表里已经有 CardMove 的 clip」≠「有 cue」；
              #     ② `bundle_soundcollection_assets_all/MonoBehaviour/Cardmove.json`（小写 m）是**另一个**
              #        1-clip cue（`Cardmove`，pitch 1.0），本笔**不要**它；
              #     ③ `battle_sounds.json` 里的 `moveCardIntoBattle` / `createCardInHand` 是**战斗字段名**表，
              #        它们的 `clips` 里出现 `CardMove` / `CardMove_1..3` 也是 **clip 名** ⇒ 与 cue 名不冲突
              #        （两张表装同一个字典时 `WFSoundBank` 按 `name` 撞名，这里撞不上）。
              "CardMove",
              # —— 战斗线 · Tap 族（2026-10-25 · 执行代理 `N16`）——
              #   点战场地面的反馈那 **10 条 cue**（原版组件 `TapHitMaterialDefinition.particleDefinition.audio`
              #   是 `AssetReferenceTyped<AudioCue>` ⇒ `animfx_modules.json` 里**一条都推不出来**）。
              #   判据 → `资料/完全复刻/判据/Tap族_地面点击反馈.md` §2.2（25 种组合）/ §2.4（cue 参数表）。
              #   **本轮亲核**（不是抄文档）：
              #     · 全库 30 个 `TapHitMaterialDefinition` 的 `m_AssetGUID` 去重 = **19 粒子 + 11 cue**；
              #       逐个 `python -I -X utf8 工具/_guid_resolve_probe.py g <guid>` ⇒ 11 条 cue 名如下
              #       （**11 条里 1 条 `Buff Mana 1` 早就在表里**，其余 10 条就是这一批）。
              #     · clip：`bundle_soundcollection_assets_all/AudioClip/` 里那 21 个 `.wav` **全在**
              #       （`Hit Rock 1..3` / `Hit Fire 1..3` / `Hit Floor Generic 1..2` / `Hit Metal Light 1..2` /
              #        `Hit Heavy Metal 1..2` / `Hit Spark 1..3` / `Hit Squishy 1..2` / `Hit Water 1..2` /
              #        `Hit Monolith` / `Squig Growl 1`）。
              #     ⚠️ **cue 名 ≠ clip 名**：`Tap Monolith` → `Hit Monolith`、`Tap Squig` → `Squig Growl 1` ⇒
              #       ⛔ 别拿 cue 名去 `Resources/` 找文件。
              "Tap Hit Rock", "Tap Hit Metal Heavy", "Tap Hit Metal Light", "Tap Hit General",
              "Tap Hit Fire", "Tap Hit Spark", "Tap Hit Squishy", "Tap Hit Water",
              "Tap Monolith", "Tap Squig",
              # —— 收藏线 · 格子点击音（2026-10-25 · 执行代理 `R17`）——
              #   判据（`R13` 已查好，本轮只**落地**、不重查；另**亲核**了 cue→clip 那一跳）：
              #   · 引用者 = `CollectionItem` 基类的 `onClickSound`（`+0x38`，消费点 =
              #     `CollectionItem.OnPointerClick`：`if (listenToPointerEvents) { display.Select(...);
              #     if (onClickSound) SoundManager.Play2D(onClickSound); }`，RVA `0x15F2B80`）。
              #   · `Collection Card`（GO `-3932997974561655464`）`onClickSound` = pid `604076303329428464`
              #     ⇒ **`CardClick 1`**；`Collection Deck`（GO `-2024334647938937812`）与
              #     `Collection Deck With Highlight`（GO `-7515975617742763446`）= pid `605815307224280273`
              #     ⇒ **`DeckClick 1`**；`Collection Cosmetic`（GO `-586895015824482954`）= **null**（原版卡背格不响）。
              #   · 🔴 **为什么必须单列在这里**：`CollectionItem` **不是** `EverguildButton|EverguildToggle`
              #     ⇒ 原版**没有**默认按钮音（`SoundManager.defaultButtonSound`）；我们这一层建的是
              #     `WindowButton`（缺省 `playClickSound = true`）⇒ 不登记这两条就只能听「错的那一声」。
              #   · **cue→clip 亲核（2026-10-25 用 UnityPy 读 `soundcollection_assets_all.bundle`）**：
              #     `CardClick 1` 的 `clipList` 唯一一条 pid `487309658559691530` = **`UI Select Toggle`**
              #     （⚠️ 与 `ButtonClick` 的 clip **是同一个资产** —— 听感差别只在 pitch：本 cue 0.8–1.1 随机，
              #      `ButtonClick` 恒 1.0 ⇒ ⛔ 别因为「clip 一样」就把两条 cue 合并）；
              #     `DeckClick 1` 的唯一一条 pid **`-1688727879682680216`**（⚠️ 负数！`R13` 报告里写成正数，
              #      铁律 5 就地订正）= **`UI Click Deck 1`** —— 这条 clip **工程里原本没有**，
              #      所以本 cue 是「登记 + 重跑导入」才真的会响。
              #   · 参数（资产实读）：`CardClick 1` pitch 0.8–1.1 / vol 1–1 / ttp 0.1；
              #     `DeckClick 1` pitch 1.0 / vol 1–1 / ttp 0.1。
              "CardClick 1", "DeckClick 1",
              # —— 🆕 **2026-10-27（音频线 `B9` · 执行代理 `B2AU6`）：cue→WindowSounds 映射表里那 37 条缺口的 35 条**
              #   判据 → `资料/完全复刻/报告/B9_B2AU6_cue映射补缺.md` ①/②：
              #   · 逐条核过 `d:/2/新解包资源/assets_full/bundle_soundcollection_assets_all/AudioClip/` **37/37 全在**
              #     （35 `.wav` + 2 `.ogg`）；我们 `Resources/Art/audio/**` 里 35 条**一个都没有**（模糊匹配也 0）。
              #   · **本脚本推不出它们**：这 35 条既不是 AnimFX `sounds/exitSounds` 的、也不是 `collisionEvent`
              #     的 PlaySound（它们分别来自战场环境事件、抽卡开包、能量累积、锻造、排位印章、Raid 领奖 …）
              #     ⇒ 只能在这里声明。⚠️ 反向查漏本来就点到了其中 3 条（`Asteroid Zone` / `ButtonClick 1` / `Dawn Attack`，
              #     出处 = `WarpforgeArena1/Editor/AnimFXCheck.cs` 与 `Shell/ItemDrawer.cs`）⇒ 我们**代码侧已经在按名字播**。
              #   · ⚠️ **`Draft Pack Finish Animation`（148）与 `Draft Prebuilt Pack preview`（150）是两条 cue
              #     但共用同一个 clip `UI Click Card 3`** ⇒ 导入 1 个文件、建 2 条条目（别导两遍）。
              #   · ⚠️ cue 名 ≠ clip 名：`Booster Open Orks` → `Open_Cardpack_Goff.wav`、
              #     `Star Orbit` → `Dark Angels Sun Orbit.wav`、`Webway Rift` → `Aeldari Blackout.wav`、
              #     `Orbital Initial` → `Orbital Initial ringing.wav`、`Daemonic Feast` → `Black Legion Daemonic Feast.wav`
              #     ⇒ ⛔ 别拿 cue 名去 `Resources/` 找文件。
              #   —— UI 侧（18 条）——
              "Booster Open AstraMilitarum", "Booster Open BlackLegion",
              "Booster Open Emperors Children", "Booster Open Orks",
              "Booster Open SaimHann", "Booster Open Sororitas", "Booster Open Tau",
              "ButtonClick 1", "Deck Editing Valid Deck",
              "Draft Pack Appear", "Draft Pack Finish Animation",
              "Draft Pack Selected Moving", "Draft Prebuilt Pack preview",
              "Energy Accumulated", "Energy Accumulated Used",
              "Forge Card", "Gacha vault open", "Ranked Seal SFX",
              "UI Raid Claim Reward Ching", "UI Raid Claim Reward Wait",
              #   —— 战场环境侧（17 条；⚠️ 11 条是**环境事件音**，不是一次性 UI 音）——
              "Asteroid Zone", "Daemonic Feast", "Dawn Attack", "Everstorm", "Factory Overdrive",
              "Firestorm Loop", "First Light", "Full Moon", "Immortal Beams", "Orbital Initial",
              "Orks Dust Storm Start", "Star Orbit", "Sump Overspill",
              "Tau Electro-Static Interference", "Toxic Fumes Start", "Warp Storm", "Webway Rift",
              # —— 🆕 **2026-10-27（`B2AU` 第九批 → 第十四批订正）：反向查漏剩下的 3 个**
              #   🔴 **`Cardmove` 已被本批【撤销】（2026-10-27，铁律 5 就地订正）**：我第九批把它当「真 cue」收进来了，**错**。
              #     · 判据（`D3OT` 现核 + 我复核）：真正的换位 cue 是 **`CardMove`（大写 M）**，它**本来就在**
              #       `EXTRA_CUES` 里（见上面那一段 191-192 行早就写明的「小写 `Cardmove` 是另一个 1-clip cue、**本笔不要它**」）。
              #     ② 我给的依据 `Editor/SoundCueAssertT.cs:77` **读反了** —— 那一行恰恰是**禁止** `Cardmove` 的断言文案。
              #     ③ **全仓零生产消费点**（`grep` 现核：只有断言/注释提到，没有任何播放口按这个名字播）。
              #     · `Cardmove.json` 确实是个**独立资产**（`m_Name = Cardmove`，1 个 clip = pid `-6740773956303463874`，
              #       pitch 1.0 / ttp 0.1；与 `Draft Mode Warlord Appear` **共用同一个 clip**），
              #       但**没人引用** ⇒ 它属于「反向查漏的报表」那一档，⛔ **不该塞进 cue 表**（塞进来会让
              #       `CardFaceAssertE5` 的一条断言红：表里多了个现代码里不存在的名字）。
              #     ⚠️ 坑：本机**文件名大小写不敏感** —— 我第九批打开 `CardMove.json` 实际读到的是 `Cardmove.json`
              #       （Windows 上 `CardMove.json` 不存在、被解析到 `Cardmove.json`），这就是当时误判的直接原因。
              #   · `Draft Mode Loss Jingle` / `Draft Mode Warlord Appear` —— 包里有同名 cue 包装；
              #     消费点 = `Shell/DraftModeWindow.cs:2648` / `:3536`（我们代码**已经在按名字播**，只是表里没有 ⇒ 静音）。
              #     参数：`Loss Jingle` pitch 1.0 / vol 1.0 / ttp 0.1；`Warlord Appear` pitch 0.8–1.199 · vol 0.8 · ttp **0**。
              #   ⇒ 这两条是「我们有名字、原版资产在本地、表里没有」= 真缺（属轮抽线）。
              "Draft Mode Loss Jingle", "Draft Mode Warlord Appear",
              ]

# 🔴 **2026-10-16（A828）第三来源 —— `collisionEvent` 上的 `PlaySound`。**
#   `AnimFXModuleCollisions` 的 `collisionAndParticles[i].particleSystemsDefinition[j].collisionEvent`
#   是一棵 **UnityEvent**（原版在 prefab 的 Inspector 里连的订阅），`PlaySound` 那几条要播的 cue
#   存在 **`m_Arguments.m_ObjectArgument`**（`@asset:MonoBehaviour:<cue 名>`，类型名 `AudioCue, Assembly-CSharp`）。
#   ⚠️ 它**不在 `sounds[]` / `exitSounds[]` 里**（那两条是 `AnimFXController` 的字段，这是另一个模块）
#      ⇒ `used_cues()` 原来**一条都收不到**。
#   实测（本脚本亲扫 `animfx_modules.json`）：`collisionEvent` 共 **563** 条订阅，其中
#      `PlaySound 283` / `TriggerCameraShake 179` / `PlayAnim 92` / `AnimEventDoShake 5` / 空方法名 4；
#      **非空 `m_ObjectArgument` 恰好 283 条、全在 `PlaySound` 上、类型名全是 `AudioCue`**（无一例外）。
#      283 条里 **211 条（22 个 cue 名）不在旧表** ⇒ 它们在运行期走
#      `WFModuleCollisions.MissingSoundCues` **静默不响**（2026-10-16 之前）。
#   判据 = `资料/普查产出_1016/W2_A828碰撞屏震.md` §④ 第 3 条；
#   生产侧**同一套读法** = `MyGame/Assets/WarpforgeVFX/Runtime/WFModuleCollisions.cs` 的 `ReadCalls`
#      （它同样按 `m_ObjectArgument` 取名字 ⇒ 两边的「认不认得这个 cue」必须一致）。
COLLISION_CALL_RE = re.compile(
    r"^(?P<prefix>collisionAndParticles\[\d+\]\.particleSystemsDefinition\[\d+\]"
    r"\.collisionEvent\.m_PersistentCalls\.m_Calls\[\d+\]\.)(?P<field>[A-Za-z_][A-Za-z0-9_.]*)$")
PLAYSOUND_METHOD = "PlaySound"


def collision_play_cues(kw, effect_name):
    """→ [cue 名]：本模块 `collisionEvent` 上**所有 `PlaySound` 订阅**要播的 cue（一条一元素，不去重）。

    一条订阅 = 键名上的一组 `…m_Calls[i].<字段>`。**只认 `PlaySound`**：另三种方法
    （`TriggerCameraShake` / `AnimEventDoShake` / `PlayAnim`）里那条名字不是 cue
    —— 实测它们的 `m_ObjectArgument` **全是空**，所以这里不是「筛掉了一批」（见文件头那段注释）。"""
    seen, out = set(), []
    for k in kw:
        m = COLLISION_CALL_RE.match(k)
        if not m:
            continue
        pre = m.group("prefix")
        if pre in seen:
            continue
        seen.add(pre)
        if kw.get(pre + "m_MethodName") != PLAYSOUND_METHOD:
            continue
        v = kw.get(pre + "m_Arguments.m_ObjectArgument", "")
        if not v:
            continue
        if not v.startswith(REF_PREFIX):
            print(f"⚠️ 认不出的引用形状：{v}（{effect_name}）")
            continue
        out.append(v[len(REF_PREFIX):])
    return out


def bundles_for(cue_names):
    """按名字定位 cue 在哪个包里（**返回裸名**，不带 `bundle_` 前缀；顺序 = 先两个音效主包，再其余）。

    🔴 **别再写死一个包**：第一次只读了 `soundcollection_assets_all`，结果 4 个 cue 报「bundle 里没有」——
       它们其实在 `battleprefabs_vfxandmisc_assets_all` 里（**又一个「搜错目录」**）。
    ⚠️ 顺序有意义：同名 MonoBehaviour 可能出现在别的包（卡动画包也有一堆同名资产），
       而**只有带 `clipList` 的那个才是 cue**。所以两个音效主包**先读**，其余按名字兜底。"""
    hits = set()
    for n in cue_names:
        for p in glob.glob(os.path.join(UNPACK, "bundle_*", "MonoBehaviour", n + ".json")):
            b = os.path.basename(os.path.dirname(os.path.dirname(p)))
            hits.add(b[len("bundle_"):] if b.startswith("bundle_") else b)
    rest = sorted(hits - set(BUNDLE_FALLBACK))
    return list(BUNDLE_FALLBACK) + rest


def load_bundles(names):
    """→ (cues, clip 名 → 源文件路径)。多个包的结果**合并**（cue 表按名字并，clip 按名去重）。"""
    cues, files = {}, {}
    for b in names:
        path = os.path.join(BUNDLE_DIR, b + ".bundle")
        if not os.path.exists(path):
            print(f"⚠️ bundle 不在（跳过）：{b}")
            continue
        env = UnityPy.load(path)
        name_of = {}          # pathid -> (type, name)
        n_clip = 0
        for obj in env.objects:
            nm = None
            try:
                nm = obj.read_typetree().get("m_Name")
            except Exception:                                    # noqa: BLE001
                pass
            name_of[obj.path_id] = (obj.type.name, nm)
            if obj.type.name == "AudioClip":
                n_clip += 1

        added = 0
        for obj in env.objects:
            if obj.type.name != "MonoBehaviour":
                continue
            d = obj.read_typetree()
            if "clipList" not in d:
                continue
            clips = []
            for c in d["clipList"]:
                t, n = name_of.get(c.get("m_PathID"), (None, None))
                clips.append(n if t == "AudioClip" else None)
            key = d.get("m_Name")
            if key in cues:
                continue
            cues[key] = {
                "clips": clips,
                "minPitch": d.get("minPitch", 1.0),
                "maxPitch": d.get("maxPitch", 1.0),
                "minVolume": d.get("minVolume", 1.0),
                "maxVolume": d.get("maxVolume", 1.0),
                "timeToPlayAgain": d.get("timeToPlayAgain", 0.0),
            }
            added += 1

        # 同包的音频文件（扩展名可能不同：wav / ogg / vorbis）
        cdir = os.path.join(UNPACK, "bundle_" + b, "AudioClip")
        got = 0
        if os.path.isdir(cdir):
            for f in sorted(os.listdir(cdir)):
                stem, ext = os.path.splitext(f)
                files.setdefault(stem.strip(), os.path.join(cdir, f))
                got += 1
        print(f"  · {b}：AudioClip {n_clip} 个 · 文件 {got} 个 · 新收 cue {added} 个")
    return cues, files


def used_cues():
    """→ (cue 名集合, sounds 条目数, exitSounds 条目数, collisionEvent 条目数)。从未含 `sound` 的槽位不算。"""
    d = json.load(io.open(MODULES, encoding="utf-8"))
    names, n, nex, ncol = set(), 0, 0, 0
    for e in d["effects"]:
        for m in e["modules"]:
            kw = dict(zip(m["keys"], m["values"]))
            if m["kind"] == "AnimFXController":
                for k, v in kw.items():
                    if not (k.startswith("sounds[") or k.startswith("exitSounds[")) \
                            or not k.endswith(".sound"):
                        continue
                    if not v.startswith(REF_PREFIX):
                        print(f"⚠️ 认不出的引用形状：{v}（{e['name']}）")
                        continue
                    names.add(v[len(REF_PREFIX):])
                    if k.startswith("exitSounds"):
                        nex += 1
                    else:
                        n += 1
            # 🆕 第三来源：`collisionEvent`（在 `AnimFXModuleCollisions` 上，**与上面不是同一批模块**）
            for nm in collision_play_cues(kw, e["name"]):
                names.add(nm)
                ncol += 1
    return names | set(EXTRA_CUES), n, nex, ncol


# ============================================================================================
#  🔴 **2026-10-17（B21·A828 尾巴③）查漏（反向）—— 「取材面还漏了哪条 cue」**
# ============================================================================================
#  为什么要有它：前三口 + `EXTRA_CUES` 都是「**我声明什么就收什么**」，声明漏了**没人知道**
#  —— `A910`（起拖音 `CardStartDrag` 首次起拖打一条解不出的告警）就是这么来的：
#  它在 `animfx_modules.json` 里**一次都不出现**（原版那格是组件上的序列化 `AudioCue`），
#  而我们代码里是个 C# 常量。**没有第二个人会去比这张表**。
#
#  口径（三句话）：
#    ① **起点是权威集合**：拿**原版 cue 包装名**（`load_bundles()` 读到的那些 `m_Name`）去比，
#       ⛔ 不是拿 `.cs` 里随便一个字面量 —— 所以**不会凭空发明**一条原版没有的 cue。
#    ② **降噪两层**：命中的行必须在**音效上下文**里（`SCAN_CTX_RE`），且**注释行不算**（`_is_comment`）。
#       实测：不加这两层剩 28 个名字（`Victory`/`Mulligan`/`Asteroid Zone`… 全是重名噪声），
#       加了剩 8 个，**每个都指得到出处**。
#       🔴 **2026-10-18（A917）：这两层是【全项目唯一一处】，已收进 `sfx_context_line()`。**
#          ⛔ **别在别的脚本里再写一遍** —— 全项目有三个「取材口」，各管自己那一摊：
#            本脚本（`sounds[]` + `exitSounds[]` + `collisionEvent` + `EXTRA_CUES`）·
#            `import_remnant_sfx.py`（残骸六条）· `rebuild_overtime_start_ogg.py`（`OvertimeStart`，重制 ogg）。
#          口径各写一遍 ⇒ 既会漂、又要**跑三遍才拼得出「还漏了哪条 cue」**。
#          ⇒ 另两个脚本**一律 `import` 调这里**（`sfx_context_line()` / `code_only_cue_report()`
#            的登记表 `CODE_ONLY_KNOWN` / 自检 `check_coverage_registered()`），不自己实现。
#          ⚠️ **它是启发式、会漏报**（28 → 8 这个数就是这个意思：**宁少报、不误报**）——
#            只能用来**提醒人去核**，⛔ 不能当「取材面已经完整」的证明。
#    ③ **只报告、不自动收**：剩下那 8 个里仍有「同名 clip」这类误报（`Add card to deck`），
#       自动并进表会把表弄脏。已解释过的列在 `CODE_ONLY_KNOWN` 里（**带理由**）⇒ 常态输出安静，
#       一旦出现**新**名字就 ⚠️ 打出来。
SCAN_ROOT = "d:/4/Unity/MyGame/Assets"
SCAN_CTX_RE = re.compile(r"(?i)sound|audio|cue|clip|\.Play\(")
CODE_ONLY_KNOWN = {
    "OvertimeStart": "**clip** 名（`BattleDriver` 直取 `WFSoundBank.Clip`）· 产出方 = "
                     "`工具/rebuild_overtime_start_ogg.py` —— ⛔ **别并进本表**：本表会把原版那条 "
                     "`.ogg/.wav` 原样拷过去，**覆盖掉那条重制过的**",
    "Add card to deck": "**clip** 名（cue `Reward open item by item` 的 clip，已在表里）· "
                        "与某个 cue 包装**同名** ⇒ 是误报",
    # 🆕 **2026-10-17（音效线 `S03` 复核 · 跨线顺手）**：战斗侧两条 —— **另有产出方，别并到这里**
    "UnitDamage": "**战斗 cue**（`BattleSfx.UnitDamage` = 原版 `SoundAssetCollection.unitDamage`）· "
                  "产出方 = `工具/import_battle_sfx.py` → `Resources/battle_sounds.json`"
                  "（cue 资产 `bundle_soundcollection_assets_all/MonoBehaviour/UnitDamage.json`，clipList 1 条）",
    "Mulligan": "**战斗 cue**（`BattleSfx.Mulligan` = 原版 `SoundAssetCollection.mulligan`）· "
                "产出方 = `工具/import_battle_sfx.py` → `Resources/battle_sounds.json`"
                "（cue 资产 `bundle_soundcollection_assets_all/MonoBehaviour/Mulligan.json`）",
    "Aeldari To Waystone": "残骸 cue（预制体上的序列化 `AudioCue` 字段，不在 `animfx_modules.json` 里）"
                           "· 产出方 = `工具/import_remnant_sfx.py`",
    "Aeldari Waystone Destruction": "同上（残骸线）",
    "CardShatter": "同上（残骸线）",
    "NecronsCardReanimate": "同上（残骸线）",
    "RemnantsDestroyed": "同上（残骸线）",
    # 🆕 **2026-10-27（`B2AU` 第十四批）：`Cardmove`（小写 m）—— ⛔ 不入 cue 表，登记在这里**
    "Cardmove": "**独立资产但零生产消费点**（`bundle_soundcollection_assets_all/MonoBehaviour/Cardmove.json`，"
                "`m_Name = Cardmove`，1 个 clip = pid `-6740773956303463874`，pitch 1.0 / ttp 0.1；"
                "与 `Draft Mode Warlord Appear` **共用同一个 clip**）。真正的换位 cue 是 **`CardMove`（大写 M）**，"
                "它**已在 `EXTRA_CUES`**。反向查漏点到的那 5 处里，`Editor/SoundCueAssertT.cs:77` 恰恰是"
                "**禁止**这条 cue 的断言文案 ⇒ **误报**。⚠️ 本机**文件名大小写不敏感**（`CardMove.json` 会被解析到"
                "`Cardmove.json`）—— 这是 2026-10-27 误把它收进 `EXTRA_CUES` 的直接原因（已撤销）。",
}


def _is_comment(line):
    s = line.lstrip()
    return s.startswith("//") or s.startswith("*") or s.startswith("/*")


def sfx_context_line(line):
    """🔴 **全项目唯一一处**的「音效线索」降噪判据（A917）—— **两层合起来才认**：

      ① 这一行在**音效上下文**里（`SCAN_CTX_RE`：`sound` / `audio` / `cue` / `clip` / `.Play(`，忽略大小写）；
      ② 它是**代码行**、不是注释（`_is_comment`）。

    ⚠️ **这是启发式，会漏报**：实测不加这两层剩 **28** 个名字（`Victory` / `Mulligan` /
    `Asteroid Zone` … 全是重名噪声），加了剩 **8** 个（每个都指得到出处）
    —— 也就是**宁可漏报、不误报**。⇒ 它只用来**提醒人去核**，
    ⛔ **不能当「取材面已经完整」的证明**。

    ⛔ **别在别的脚本里重写这两层** —— `import_remnant_sfx.py` /
    `rebuild_overtime_start_ogg.py` 一律 `import` 过来调（A917）。"""
    return bool(SCAN_CTX_RE.search(line)) and not _is_comment(line)


def scan_sfx_hits(names, root=SCAN_ROOT):
    """把 `names` 在 `root` 下的 `.cs` 里找一遍 —— **只算「音效上下文里的代码行」**
    （判据 = `sfx_context_line()`，**全项目唯一一处**）。

    → `(hits, n_lines)`：`hits[name] = ["相对路径:行号", …]`（**按文件顺序**）；
      `n_lines` = 扫过的**总行数**（含被降噪跳过的那些，与原实现同口径）。

    🔴 **2026-10-18（A917）**：这个循环原来内联在 `code_only_cue_report()` 里 —— 抽出来是
    为了让「反向查漏」的**扫描口径**也只有一处（`check_coverage_registered()` 复用同一个）。"""
    hits, n_lines = {}, 0
    if not names or not os.path.isdir(root):
        return hits, n_lines
    # 一条正则匹所有候选名（长的排前面，免得 `Aeldari To Waystone` 被 `Aeldari To Waystone Death` 截断）
    big = re.compile("|".join(re.escape(c) for c in sorted(names, key=len, reverse=True)))
    for dirpath, _dirs, files in os.walk(root):
        for f in files:
            if not f.endswith(".cs"):
                continue
            fp = os.path.join(dirpath, f)
            rel = os.path.relpath(fp, "d:/4/Unity").replace("\\", "/")
            for i, line in enumerate(io.open(fp, encoding="utf-8", errors="replace"), 1):
                n_lines += 1
                if not sfx_context_line(line):      # 🔴 A917：两层降噪只有这一处实现
                    continue
                for m in big.finditer(line):
                    hits.setdefault(m.group(0), []).append(f"{rel}:{i}")
    return hits, n_lines


def check_coverage_registered(cues, owner, root=SCAN_ROOT):
    """🆕 **2026-10-18（A917）：给【另两个取材口脚本】调的自检。**

    `cues` = `owner` 那个脚本自己覆盖的原版 cue 名。**只在「真的会误报」时才出声** ——
    也就是某条 cue ①**没登记**在上面的 `CODE_ONLY_KNOWN` 里、**且** ②**真的出现在「音效上下文」
    的代码行里**（判据同 `scan_sfx_hits()`）。两个条件缺一个就不出声：
      · 登记过 ⇒ 那边的查漏本来就会跳过它；
      · 只在注释里出现 ⇒ 那边的查漏**根本扫不到它**（降噪第 ② 层），登记纯属多余。
    **常态 = 一声不响。**

    ⚠️ 调用方自己负责 `import` 本模块（`sys.path` 引导见 `import_remnant_sfx.py` 的
    `_shared_sfx_scan()`）；这里**不**替调用方做引导，也**不**在取不到时假装通过。"""
    miss = [c for c in cues if c not in CODE_ONLY_KNOWN]
    if not miss:
        return []
    hits, _n = scan_sfx_hits(miss, root)
    bad = [c for c in miss if c in hits]
    if bad:
        print(f"⚠️ {owner} 的这 {len(bad)} 条 cue **没在** `工具/import_original_sfx.py` 的 "
              f"`CODE_ONLY_KNOWN` 里登记、而**代码里真的在提** ⇒ 那边跑「查漏（反向）」会把它们"
              f"当成新漏网重复报出来：" + "；".join(f"`{c}`（{hits[c][0]}）" for c in bad))
    return bad


def code_only_cue_report(all_cue_names, want):
    """→ 新候选 `[(name, [file:line, …]), …]`；**顺手把结果打印出来**（常态 = 0 个）。

    ⚠️ 调用点在 `load_bundles()` **之后**（`all_cue_names` 就是它读到的 cue 名，见 `main()`）。"""
    if not os.path.isdir(SCAN_ROOT):
        # 不许静默：扫不动就等于**这一次根本没查漏**，而输出看起来和「查过、没漏」一模一样。
        print(f"⚠️ 查漏（反向）：`{SCAN_ROOT}` 不在 ⇒ 这一趟**没有查漏**（不是「查了没漏」）")
        return []
    todo = sorted(set(all_cue_names) - set(want) - set(CODE_ONLY_KNOWN))
    if not todo:
        print("查漏（反向）：原版 cue 名里没有「取材面没收、代码又在提」的新名字 ✅")
        return []
    hits, n_lines = scan_sfx_hits(todo)     # 🔴 A917：扫描口径也只有这一处
    out = sorted(hits.items(), key=lambda kv: -len(kv[1]))
    print(f"查漏（反向）：扫 {SCAN_ROOT} 的 .cs 共 {n_lines} 行（只算音效上下文的**代码行**）⇒ "
          f"**{len(out)} 个候选**")
    for name, where in out:
        print(f"  ⚠️ `{name}`（{len(where)} 处，例如 {where[0]}）—— 表里没有它 ⇒ "
              f"要么补进 `EXTRA_CUES`（连判据一起），要么写进 `CODE_ONLY_KNOWN`（连理由一起）")
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="只报告，不写盘")
    args = ap.parse_args()

    want, n_snd, n_exit, n_col = used_cues()
    print(f"特效在用：`sounds` 有值的 {n_snd} 条 + `exitSounds` {n_exit} 条 + "
          f"`collisionEvent` 的 `PlaySound` {n_col} 条 ⇒ **{n_snd + n_exit + n_col} 条**，"
          f"涉及 {len(want)} 个不同 cue (其中 `EXTRA_CUES` 硬编 {len(EXTRA_CUES)} 个)")

    need_bundles = bundles_for(want)
    print(f"cue 分散在 {len(need_bundles)} 个包里，逐个读：")
    cues, files = load_bundles(need_bundles)
    print(f"合起来：cue 包装 {len(cues)} 个 · 能对上的音频文件 {len(files)} 个")

    # ---- 🆕 查漏（反向）：取材面漏没收的 cue（只报告，见 `code_only_cue_report`）----
    code_only_cue_report(cues.keys(), want)

    # ---- ① 解析 cue → clip ----
    table, missing_cue, missing_clip, no_clip = [], [], set(), []
    for name in sorted(want):
        c = cues.get(name)
        if c is None:
            missing_cue.append(name)
            continue
        clips = [x for x in c["clips"] if x]
        # ⚠️ 资产名里有**尾随空格**的（`Hero of the Empire ` / `Tau Summon `）—— Windows 文件名存不下
        #    尾随空格，解包目录里是剥过的 ⇒ **两侧都要 strip 再比**，否则那两条静默落空。
        clips = [x.strip() for x in clips]
        if not clips:
            no_clip.append(name)
            continue
        got = []
        for cl in clips:
            if cl not in files:
                missing_clip.add(cl)
                continue
            got.append(cl)
        if not got:
            no_clip.append(name)
            continue
        table.append({
            "name": name, "clips": sorted(set(got)),
            "minPitch": round(c["minPitch"], 4), "maxPitch": round(c["maxPitch"], 4),
            "minVolume": round(c["minVolume"], 4), "maxVolume": round(c["maxVolume"], 4),
            "timeToPlayAgain": round(c["timeToPlayAgain"], 4),
        })

    need_files = sorted({cl for t in table for cl in t["clips"]})
    print(f"解析成功 {len(table)}/{len(want)} 个 cue；要用到 {len(need_files)} 个音频文件")
    if missing_cue:
        print(f"⚠️ bundle 里没有这个 cue（{len(missing_cue)}）：{missing_cue}")
    if no_clip:
        print(f"⚠️ cue 在、但 clipList 解不出名字（{len(no_clip)}）：{no_clip}")
    if missing_clip:
        print(f"⚠️ clip 名对不上解包文件（{len(missing_clip)}）：{sorted(missing_clip)}")

    # ---- ② 拷音频 + 补 .meta ----
    # 🔴 2026-10-24（G4）：**每条音频（这次新拷的 + 上次已拷过的）都过一遍 `ensure_meta`**。
    #    `ensure_meta` 对已存在的 meta **一个字节都不碰** ⇒ 重跑幂等；而「上次拷了 wav 但没 meta」
    #    的这一次会补上 —— 也就是「只补 meta、不重拷 wav」那条路（不跑本脚本的话走
    #    `python -I -X utf8 工具/_sfx_meta.py --backfill`，同一份实现、同一套模板）。
    copied = skipped = n_meta = 0
    exts = {}
    if not args.check:
        os.makedirs(DST_AUDIO, exist_ok=True)
        guids = _sfx_meta.known_guids()      # 全工程 meta 扫一次（实测 ~1.8 s），别每条都扫
        for cl in need_files:
            src, dst = files[cl], os.path.join(DST_AUDIO, cl)
            ext = os.path.splitext(src)[1].lower()
            exts[ext] = exts.get(ext, 0) + 1
            path = dst + ext
            if os.path.exists(path) and os.path.getsize(path) == os.path.getsize(src):
                skipped += 1
            else:
                shutil.copy2(src, path)
                copied += 1
            if _sfx_meta.ensure_meta(path, guids=guids) == "created":
                n_meta += 1

    # ---- ③ 写表 ----
    out = {
        "version": 1,
        "note": "AnimFX 的随机化 cue 表（来源三条路径：`sounds[*].sound` · `exitSounds[*].sound` · "
                "`collisionEvent` 的 `PlaySound` 订阅；另加 `EXTRA_CUES` —— 工程按名字播、"
                "AnimFX 数据里推不出来的 cue）。clips 里随机挑一条；"
                "播放音量 = minVolume..maxVolume、音高 = minPitch..maxPitch；"
                "文件名在 Resources/Art/audio/sfx/<clip 名>。",
        "source": {
            "bundle": "soundcollection_assets_all.bundle",
            "who": "数据/游戏数据/animfx_modules.json 的 sounds[*].sound / exitSounds[*].sound /"
                   " collisionEvent 的 PlaySound（m_Arguments.m_ObjectArgument）"
                   " + 脚本里的 EXTRA_CUES（按名字播的那几条）",
            "script": "工具/import_original_sfx.py",
        },
        "cues": table,
    }
    if not args.check:
        io.open(DST_JSON, "w", encoding="utf-8", newline="\n").write(
            json.dumps(out, ensure_ascii=False, indent=1))

    if args.check:
        # 🔴 2026-10-27（执行代理 B9_W35CHECK）：`--check` 必须**连产物一起查** ——
        #   原来只报「cue 解析 + 源 clip 在不在」（上面 `missing_cue`/`missing_clip`），
        #   而拷贝循环整段被 `if not args.check` 跳过 ⇒ **产物（音频 + 这张表）缺失/过期一律静默绿**。
        #   判据同 工具/import_original_art.py `--only-altart`（`:1714-1721`）。
        lack_json = None
        if not os.path.exists(DST_JSON):
            lack_json = f"{DST_JSON} 不存在"
        else:
            old = io.open(DST_JSON, encoding="utf-8", newline="").read()
            if old != json.dumps(out, ensure_ascii=False, indent=1):
                lack_json = f"{DST_JSON} 已过期（与现生成不一致）"
        lack_files = []
        for cl in need_files:
            src = files[cl]
            ext = os.path.splitext(src)[1].lower()
            dst = os.path.join(DST_AUDIO, cl + ext)
            if not os.path.exists(dst):
                lack_files.append(cl + ext)
            elif os.path.getsize(dst) != os.path.getsize(src):
                lack_files.append(cl + ext + "（尺寸不符）")
        if lack_json:
            print("🔴 产物缺失/过期：" + lack_json)
        if lack_files:
            print(f"🔴 音频产物缺失 {len(lack_files)}/{len(need_files)} 个（{DST_AUDIO}）—— "
                  f"重跑不带 --check 落盘：")
            for b in lack_files[:50]:
                print("   缺:", b)
            if len(lack_files) > 50:
                print(f"   …共 {len(lack_files)} 个")
        if not lack_json and not lack_files:
            print(f"✅ --check：产物齐全（表 {DST_JSON} · 音频 {len(need_files)} 个）")
        print("（--check：没有写盘）")
        return 0 if (not lack_json and not lack_files) else 1
    print(f"已拷 {copied} 条 · 已存在跳过 {skipped} 条 · 补 meta {n_meta} 条 → {DST_AUDIO}"
          f"（扩展名分布 {exts}）")
    print(f"表已写：{DST_JSON}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
