# W_G · 壳侧音效线（窗口开/关 · 按钮 · 开包窗）

> 日期：2026-10-17 · 写手代理：音效线 `W_G`
> 范围：**只做壳侧**（`GameWindow` / `WindowButton` / `BoosterPackOpenWindow`）。战斗音响 `T06` 一个字没动。
> ⛔ 本轮**没有起 player、没有跑 Unity**；只做了静态接线 + 秒级类型检查。

---

## 一、结论 + 改动清单

**三件都做完了，而且「默认音 clip 名没解出」这条老账已经解开。**

| # | 件 | 结果 |
|---|---|---|
| 1 | `GameWindow` 关窗音 | ✅ 补 `closeSound` + `useDefaultCloseSoundIfNull`（默认 true，照 ctor），`Close()` 守卫之后播 |
| 2 | 默认按钮音 | ✅ 接到 `WindowButton.Click()`（原版消费者 = `EverguildButton` / `EverguildToggle` 的 `PlayButtonSound`，两族 **1173/1173** 颗 `useDefaultAudioClipIfNull = 1`） |
| 3 | 开包窗音效 | ✅ 原先「本地没有」的判断**不成立** —— 12 条 cue 全部解开并接上（点卡/稀有度/第一张/拆包/关闭） |

**改动文件 / 行号**（改动后现读）

| 文件 | 行 | 改了什么 |
|---|---|---|
| `Unity/MyGame/Assets/CardPresentation/Shell/GameWindow.cs` | 79 | 新增 `public AudioClip closeSound`（原版字段 `0x30`） |
| 同上 | 81–95 | 新增 `useDefaultCloseSoundIfNull = true`（原版 `0x38`，ctor 默认 **true**）+ 默认 cue 的完整判据链注释 |
| 同上 | 616–625 | `Close()` 里 **守卫之后、`CurrentState = Closed` 之前** 调 `PlayCloseSound()` |
| 同上 | 674–693 | 新方法 `PlayCloseSound()`（显式 clip → 否则默认 cue → 否则静默，照原版） |
| `Shell/WindowButton.cs` | 866 | 新增 `public bool playClickSound = true` |
| 同上 | 868–876 | 新方法 `PlayClickSound()`：压暗层（`MenuDraw.WasShadeHit`）跳过；其余走默认按钮 cue |
| 同上 | 840 | `Click()` 在 `interactable` 判定之后、`onClick()` 之前播 |
| `Shell/WindowSounds.cs` | 新文件 | **壳侧音效唯一一张表** + 播放口（FX / Jingles）+ `timeToPlayAgain` 节流 + 缺 cue 出声 |
| `Shell/WindowSounds.cs.meta` | 新文件 | 最小 meta（照本仓现有 `.cs.meta` 的格式） |
| `Core/WarpforgeAudio.cs` | 148–189 | 新方法 `PlayJingle(WFSoundCue)` —— **Jingles 组**的播放口（`WFSoundPlayer` 写死挂 FX，不能复用它） |
| `Shell/BoosterPackOpenWindow.cs` | 331–336 | 新方法 `CloseFromTap()`（先播 `soundOnClose`，再 `Close()`） |
| 同上 | 370 | `Build()` 里播**拆包声**（`Booster Open <army>`，Jingles） |
| 同上 | 424 | `Tap to close` 整屏层的 `onClick`：`Close` → `CloseFromTap` |
| 同上 | 577、628–643 | 卡的命中区关掉通用按钮音；`Reveal()` 里播**点卡声 + 稀有度声 + 第一张声** |
| 同上 | 62–73、853–861 | 🔴 就地订正两处过期记录（「音效没做 / cue 在远端」→ 已接；「五处」→「四处」） |
| `Unity/工具/import_original_sfx.py` | 88–112 | `EXTRA_CUES` 增收 **14 条**壳侧 cue（带逐条出处） |
| `Resources/animfx_sounds.json` | — | 重跑导入器后**纯新增 154 行**（0 删除）：432 → 446 条 cue |
| `Resources/Art/audio/sfx/*.wav` | — | **新拷 14 条 clip**（`Resources/Art/` 按工程规矩不进 git） |

---

## 二、每条音效：原版字段/触发点 → clip 来源 → 接线点

| 音效 | 原版字段 / 触发点（判据） | cue → clip（本地已解出） | 混音组 | 我们的接线点 |
|---|---|---|---|---|
| **默认按钮音** | `SoundManager.defaultButtonSound`（MB pid `-5308416599568047990`，字段 pid `6176553109044381692`）；消费者 = `EverguildButton.PlayButtonSound` / `EverguildToggle.PlayButtonSound`，触发 = 各自 `OnPointerClick`；条件 = `m_onClickSound` 为空 ∧ `useDefaultAudioClipIfNull`(`0x178`) | `ButtonClick` → `UI Select Toggle` | FX(0) | `WindowButton.Click()` → `WindowSounds.PlayFx` |
| **默认关窗音** | `SoundManager.defaultCloseSound`（pid `8084683923307806969`）；`GameWindow__Close.c`：首句守卫之后、`WindowsManager.CloseWindow` **之前**，`closeSound == null && useDefaultCloseSoundIfNull` ⇒ 用它 | `ButtonClick Back` → `UIButton Back` | FX(0) | `GameWindow.Close()` → `PlayCloseSound()` |
| **开包窗关闭声** | `BoosterPackOpenWindow.soundOnClose`（窗口 MB pid `9012570135841684515`，字段 pid `3416723080278618344`）；`CloseButtonClick` 第一句 | `Booster Pack Window Close` → `Cardshuffle out` | FX(0) | `CloseFromTap()`（`Tap to close` / 整屏 `Collider`） |
| **点卡声** | `CardInBoosterPack.clickSound`（pid `3481876391266756950`）；`UiColliderOnClick` 第一句（`state == 2` 才响） | `CardClick 2` → `UI Click Card 2` | FX(0) | `BoosterPackOpenWindow.Reveal()` 开头 |
| **稀有度翻牌声** | `CardInBoosterPack.contentByRarities[].audioCue`；`ChangeState(3)` 里 `SoundManager.Play2D(cue, **MixerType 3**)`。分组 = `0/1→R1` · `2→R2` · `3→R3` · `4/5→R4` | `Booster Pack Open Card Rarity 1..4` → `Cardflip_Common / Rare / Epic / Legendary`（R1 的 pitch 区间 **0.9–1.1**，其余 1.0） | **Jingles(3)** | `Reveal()` → `PlayJingles` |
| **第一张新卡声** | `CardInBoosterPack.newCardSound`（pid `4962178071637942874`）；`_WaitForSpinAnimationEnd`：`GetOwnedCount(card) == 1` 才播 | `New Card` → `New Card` | FX(0) | `Reveal()` 里与 `IsNewCard` 同条件（**当前恒假**，见下） |
| **拆包声** | `BoosterPackVisualItem.openBoosterPack`（`boosterpacks_assets_all`）；`Initialize` 末段 `Play2D(cue, **3**)` | `Booster Open {Standard/Ultramarines/Sautekh/Space Wolves/Tyranid}` → `Open_Cardpack_*` | **Jingles(3)** | ⚠️ **落点是我们挑的**：`Build()`（原版那件 `BoosterPackVisualItem` 我们整段没建） |

**阵营 → 拆包 cue 的映射**（判据 = prefab 上同一组件里 `boosterName` ↔ `openBoosterPack` 的配对，逐颗实读）：

| 原版 `boosterName` | cue | 我们的 `Army` |
|---|---|---|
| `Armies/Ultramarines` | `Booster Open Ultramarines` | `Ultramarines` |
| `Armies/Sautekh_short` | `Booster Open Sautekh` | `Sautekh` |
| `Armies/SpaceWolves` | `Booster Open Space Wolves` | `SpaceWolves` |
| `Armies/Genestealers` **与** `Armies/Leviathan`（共用一条） | `Booster Open Tyranid` | `Leviathan` |
| `MenuShop/Alpha/AllArmies` | `Booster Open Standard` | `null`（全卡池） |

> 顺带记录：同一个阵营在 prefab 里有**多颗**实例，`Armies/TauEmpire` 有一颗用 `Booster Open Tau`、另一颗用
> `Booster Open Standard` —— 原版自己就不一致；我们只取与本窗 `Army` 对得上的那一条。

---

## 三、「默认音 clip 名没解出」—— 已解开（完整 PID 链）

`资料/普查产出_1017/UI/U1` §五·5 原记「在 `bundle_soundcollection_assets_all/AudioClip/` 按 pid 找不到，名字没解出」。
**错因 = 只在 AudioClip 目录里按 pid 找，没读 SoundManager 那个 PPtr 的 `m_FileID`（它指向的是 cue 包装，不是 clip）。**
正确链路（全部现读 `d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/` 的 bundle）：

```
SoundManager MB  pid -5308416599568047990   （bundle_staticgeneralassets_assets_all）
 ├─ defaultButtonSound = { m_FileID 5, m_PathID 6176553109044381692 }
 └─ defaultCloseSound  = { m_FileID 5, m_PathID 8084683923307806969 }
      m_FileID 是 **1-based** 索引进 externals ⇒ externals[4] = CAB-62d1945b7005c115fb946f0d264a205b
      = soundcollection bundle 的那个 CAB（已核：该 bundle 的内层 CAB 就是它）
   ↓ 在 soundcollection bundle 里按 PathID 找
 cue `ButtonClick`      (6176553109044381692) ─ clipList[0] 487309658559691530  → AudioClip `UI Select Toggle`
 cue `ButtonClick Back` (8084683923307806969) ─ clipList[0] -3585420588411809655 → AudioClip `UIButton Back`
 两条 cue 的参数：minPitch = maxPitch = 1.0 · minVolume = maxVolume = 1.0 · timeToPlayAgain = 0.1
```

`showNullAudioPlaysLog = 0`（同 MB 实读）⇒ 原版 `Play2D(null, …)` 是**静默**的 —— 所以 `closeSound`/默认都空时
我们也不出声、不打日志（照原版）。

---

## 四、没做的 / 没查清的（逐条如实）

1. **逐按钮的 `m_onClickSound` 没接** —— 原版 `EverguildButton` 有 **438 颗**、`EverguildToggle` **133 颗**带自己的 cue
   （非空 `m_onClickSound`）。我们的 `WindowButton` 是 `MenuDraw.Hit` 建出来的**透明命中区**，没有「这颗对应 prefab 哪一颗」
   的数据来源 ⇒ **不编表**，只接默认音。**要把这条做到位，得先给 `MenuDraw.Hit` 加一个 per-button cue 参数**（另一笔活）。
2. **`Collection Menu Variant` / `Shop Menu Variant` 原版是 `useDefaultCloseSoundIfNull = 0` + `closeSound = null`**
   （= 关窗**静音**，见下面普查）。我们这两扇（`Shell/CollectionWindow.cs` · `Shell/ShopWindow.cs`）**没设**这个字段
   ⇒ 会继承默认 `true`、多出一声关窗音。**判据已查清、改动各 1 行**，但不在本件的可改文件白名单里 ⇒ **留给调度台派活**。
3. **`Choose Army Deck FTUE Window` 有显式 `openSound`**（pid `-1332049501696130703`）—— 那扇 FTUE 窗我们**没有建**，
   所以 `openSound` 全工程仍是零赋值（`GameWindow.openSound` 只在 `TryOpen` 那一条读）。
4. **开包窗的视觉动画没建**（文件头原就标注）：拆包声的落点因此是**我们挑的**（`Build()`）；关闭动画没做 ⇒
   `soundOnClose` 与默认关窗音**落在同一拍**（原版隔一段动画时长）。⛔ 别为这个去「去重」——两声是原版就有的。
5. **`CardInBoosterPack` 的翻牌状态机没做**：原版点卡只在 `state == 2` 才播点卡声；我们 `Reveal` 的早退条件等价
   （`Opened[i]` / `CardsLeft <= 0`），但没有 state 这个显式档 —— 若将来补 spin 动画，需要按原版补 state。
6. **`timeToPlayAgain` 的时钟**：`WindowSounds` 用 `Time.realtimeSinceStartup`。批处理里它推进很慢，**同一 cue 在一趟自检里
   最多响一次**（这不影响真机；只是自检里数「响了几次」时要知道）。
7. **`WarpforgeAudio.PlayJingle` 的随机序列**是固定种子的 `System.Random(20261017)`（照 `WFSoundPlayer` 的既有口径）。

---

## 五、typecheck

- 命令：`TMPDIR=/tmp/wf_g_audio bash d:/4/Unity/工具/typecheck.sh`
- **结果：运行时错误数 0 · 编辑器错误数 0** ✅
- 中途有一次报 **11 条错，全部在 `Shell/SettingsWindow.cs`**（另一个代理 12:49 正在写的文件，不是我碰的文件）——
  按并行纪律没去动它，隔几分钟重跑，它写完后即 **0 错**。我负责的文件自始至终 0 条错。

---

## 六、顺手发现（⛔ 未改，交调度台分流）

1. **`资料/普查产出_1017/UI/U1` §五·5 的那条老账要订正**：默认关窗音 clip 名**已解出**（`UIButton Back`，链路见本文件 §三）。
   同理 `U3` §二 C「默认开/关窗音效」那一格也过期了。⛔ 我没改那两份普查（不在可改范围）。
2. **`Shell/PopUpGameWindow.cs:59` 的注释过期**：原写「② **开/关音效没做**」—— 本笔之后它**继承 `GameWindow` 的默认关窗音**，
   那半句不再成立（prefab 两个字段为空 + `useDefaultCloseSoundIfNull = 1` 那一半仍然是原版值）。同一条要顺手核：
   `AllianceMemberOptionsPopup.cs:15` / `GenericOptionsPanel.cs:15` / `ReferralPopupWindow.cs:22` / `RewardWindow.cs:10`
   都只写了「字段为空」，**没写「所以没声音」**，那些不用改。
3. **两扇关窗本应静音的窗**：`Collection Menu Variant` / `Shop Menu Variant`（`useDefaultCloseSoundIfNull = 0`）—— 见 §四·2，
   各 1 行 `useDefaultCloseSoundIfNull = false` 即可，但不在本件白名单。
4. **`GameWindow` 全族普查**（`menus` + `generalgamewindows` 共 **100** 颗带该字段的实例）：
   `useDefaultCloseSoundIfNull = 1` 且 `closeSound` 空 = **95**（我们的默认 true 覆盖这一档）· 显式 close cue = **2**
   （`Login Window` 两颗，**恰好也是 `ButtonClick Back`** ⇒ 等价）· **opt-out（= 0 且空）= 2**（上面那两扇）·
   显式 open cue = **1**（FTUE）。
5. **`import_original_sfx.py` 的反向查漏这次报了 2 个候选**：`UnitDamage`（10 处，`Battle/BattleDriver.cs:11781` 等）·
   `Mulligan`（1 处，`Battle/BattleDriver.cs:4890`）—— **战斗侧**，按边界我没碰；要么补进 `EXTRA_CUES`（连判据）、
   要么写进 `CODE_ONLY_KNOWN`（连理由）。
6. **`AudioCue` 这个原版类型在我们工程里没有对应物**：壳侧这几条是「单 clip + 固定参数」的 cue，
   我按 **cue 名 → `WFSoundBank` 表**落（与战斗 `T06` 同一套机制）；若将来遇到**多 clip 随机**的壳侧 cue，
   `WindowSounds` 已经能直接吃（`PickClip` 会随机挑），不需要新类型。
