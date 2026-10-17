# W_T26 · `Choose Army Deck FTUE Window`（选阵营送卡组窗）执行报告

> 执行写手 · 2026-10-17 · 界面线 **T26** + 音效线 **S02**
> 判据全部本地现读（prefab / 根 MB / 反编译 / bundle 里的 PrebuiltDeck），命令写在两个新文件的文件头。

## 一、结论

- **窗建出来了**：`WindowsManager.OpenChooseArmyDeck()` → 一扇 `FTUESelectDeckWindow`（`CurrentState = Open`）。
- **四张阵营卡按 prefab 的 `HorizontalLayoutGroup` 摆开**（spacing 106.58 · pad L70/R44 · MiddleCenter），
  每张 = Glow + 督军立绘 + 阵营徽记（含阴影）+ 阵营名（含阴影）+ 选中角标（出厂 INACT）+ 描述 + 整卡命中区。
- **`openSound` 接上了**：cue = `Draft Mode Progress Jingle`（音效线 S02 已导入的那条 clip `Draft Mode GainSkull`）。
- **数据缺口 2 处**（都**留空 + 出声**，⛔ 没编）：① 四副的阵营描述词条本机没有；② `SelectedTextBg` 的底图
  `40k_replace_display` 不在 `Resources/` 里。
- `bash d:/4/Unity/工具/typecheck.sh` = **运行时 0 / 编辑器 0**。

## 二、改了什么（逐文件）

| 文件 | 改动 |
|---|---|
| `Unity/MyGame/Assets/CardPresentation/Shell/FTUESelectDeckWindow.cs` | **新建**（整窗 · 类名 == 文件名） |
| `Unity/MyGame/Assets/CardPresentation/Shell/FTUESelectDeckContainer.cs` | **新建**（`FTUESelectDeckContainer : MonoBehaviour` · 类名 == 文件名） |
| `Unity/MyGame/Assets/CardPresentation/Shell/WindowsManager.cs` | +`PrefabRefChooseArmyDeck`（`"Choose Army Deck FTUE Window"`）· +`OpenChooseArmyDeck(...)`（照 `OpenItemDisplay` 的形状） |
| `Unity/MyGame/Assets/CardPresentation/Shell/WindowSounds.cs` | +`FtueOpenCue = "Draft Mode Progress Jingle"`（带字段/组/播放口的判据注释） |
| `Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs` | +`Section("T26 …")` 一节断言（放在结尾 `合计` 之前，不打断任何既有断言） |

## 三、判据对照（逐条）

### 窗参（根 MB `MonoBehaviour_-3133570412519170200.json` 实读）

| 字段 | 原版 | 我们 |
|---|---|---|
| `type` | **0** (Fullscreen) | `WindowType.Fullscreen` |
| `windowsPlacement` | 15 (Popup) | `WindowsPlacement.Popup` |
| `closeOnESC` | **0** | `closeOnEsc = false` |
| `updateNavPanel` | 0 | `false` |
| `extraScaleSmallScreen` | 1.0 | 1f |
| `useDefaultCloseSoundIfNull` / `closeSound` | 1 / null | `true` / null（关窗音走家族默认） |
| `openSound` | PPtr(fileID 6, pid `-1332049501696130703`) | 见 §四 |

### 节点（dump 逐值）

`Menu Dark Background` 4574.60×2572.36 · (0,0,0,0.773) ｜ `Title` 0,55→1920,165.349 · fs80 ·
`SubTitle` fs50/基准45/auto[18~50] ｜ `Continue Group` 748.643×84.504 ·
`Continue Button`（`40k_bt_underbutton` 485×83 · 九宫 240,0,240,0 · Sliced）·
`Text (TMP)` 217.19×85.51 · fs72/基准24/auto[18~72] ｜ `Selected Faction Icon` 105.3257² preserveAspect ｜
`Armies` 762.037（`Image`+`ScrollRect` 两个组件都 `m_Enabled = 0` ⇒ **不滚动**，如实复刻）·
`Content` 522.019（HLG spacing 106.58 · pad L70/R44/T0/B0 · align 4 MiddleCenter · expandH=1）·
`Deck Container FTUE` 362.548×522.019 · 子件逐值（Glow/Warlord 523.7936² · Army Icon 233.034×233.038 ·
内层徽记 206.76×208.57 · Army Text 441×88.92 / TMP 526.316×75.167 fs52/Capline ·
Selected Text 371.069×128.263 rot 17.95° / bg 371.069×120.518 / 字 333.57×66.22 fs69.9 ·
Army Description 398.639×138.208 fs45 Left/Top · Select Button 523.79×774.832 `trans=0`）—— **全部照抄**。

### 行为（反编译第一权威）

- `Open`：隐藏模板容器 → 对 4 副 `Instantiate` + `Initialize` → 关 `continueButton`、关
  `selectedFactionIcon`、`continueGroup.alpha = 0`。
- `OnDeckSelected`：其它卡全 `ToggleSelected(false)` → 记住这一副 → `RefreshContinueButton`。
- `ToggleSelected`：glow scale 1/1.1（`read_literal 0x1834b31f8 = 1.1`）· glow alpha 0/1 ·
  `highlightObjects` SetActive · `button.interactable = !option`。
- `RefreshContinueButton`：亮 Continue 钮 + 已选阵营徽记。
- `OnContinueButtonClicked`：PlayFab `GrantStarterDeck` 成功 ⇒ 关窗。**我们无后端 ⇒ 出声 + 关窗**（见 §五）。

## 四、`openSound`：**FX，不是 Jingles**（与 S02 简报/导入脚本注释冲突，以指令流为准）

- 根 MB `openSound` = `AudioCue`（`GameWindow` 字段偏移 **0x28**，`dump.cs`）—— `AudioCue` 本身**没有**
  混音组字段（`clipList/minPitch/maxPitch/minVolume/maxVolume/timeToPlayAgain`，现读），组只由**调用点**给。
- 原版调用点 = `GameWindow.TryOpen`（VA `0x180836120`）的 `Closed` 支：
  `mov r8,[rbx+0x28]`（openSound）→ `xor r9d,r9d` → `call 0x1806FA6F0`；
  `dump.cs:17901` 明确 `0x6FA6F0 = SoundManager.Play2D(AudioCue, MixerType)` ⇒ **r9d = 0 = FX**。
- 因此 `FTUESelectDeckWindow.Open()` 走 **`WindowSounds.PlayFx(...)`**（`WFSoundPlayer` 挂 FX 组），
  与 `Shell/GameWindow.cs:488` 既有口径一致。
- ⚠️ **需要主对话订正的两处**（不在我的白名单）：
  `工具/import_original_sfx.py:112` 的注释「（Jingles 组）」；以及照它写的 S02 简报口径。
  若调度台仍要 Jingles，改一行 `PlayFx → PlayJingles` 即可（其余不动）。

## 五、数据（starterDecks → 本地真值）

`starterDecks` 的 `deck` PPtr 指向 `prebuiltdecks_assets_all` 里四份 PrebuiltDeck，**本地都在**
（`UnityPy` 现读，不是猜的）：

| # | `deckInfo.mTerm` | PrebuiltDeck | 督军卡 | glowColor |
|---|---|---|---|---|
| 1 | `TutorialData/TutorialFTUEDeck/UltraMarines` | `UM_SK_Starter` | `UM47` (Uriel Ventris) | (0.3224012, 0.6783406, 0.9905660) |
| 2 | `TutorialData/TutorialFTUEDeck/Orks` | `ORK_SK_Starter` | `GOF29` (Grukk Face-Rippa) | (0.7169812, 0.6966832, 0.1454254) |
| 3 | `TutorialData/TutorialFTUEDeck/BlackLegion` | `BL_SK_Starter` | `BL35` (Haarken Worldclaimer) | (0.7075472, 0.0700872, 0.0700872) |
| 4 | `TutorialData/TutorialFTUEDeck/Sutekh` | `SAU_SK_Starter` | `SAU36` (Nemesor Zahndrekh) | (0.0000000, 0.7830189, 0.2969818) |

- 阵营 = `cards_engine.json` 里督军卡的 `faction`（等价原版 `deckHero.Reference.cardArmy`）。
- 阵营徽记 = `DeckRuntime.FactionIcon(...)`（唯一一份映射；四张图都在 `Resources/Art/ui_deck/`）。
- 督军立绘 = 优先 `UI_Deck_Warlord_<卡名>`（方形 2048²，= 原版 `fullHeroSprite` 那一档）；
  **只有 Uriel Ventris 本地有**，其余三张退回 `CardArt.Portrait(id)`（671×1024 竖构图，`keepAspect`）
  —— **这一档是我们挑的兜底**，已在文件头如实标注。

### 缺口（都出声，⛔ 不编）

1. **阵营描述**：`TutorialData/TutorialFTUEDeck/*` 四条在 `Resources/official_loc_13lang.json`（9441 条）与
   `数据/游戏数据/官方本地化_en+zh-CN.json` 里**逐字节搜零命中** ⇒ `Army Description` 留空（4 条各出声一次）。
2. **`SelectedTextBg` 的底图 `40k_replace_display`**：只在 `Art/原版/battleatlasui/`（非 Resources）与
   `素材/Warpforge原版/UI图集/` 里；`Resources/Art/{ui,ui_menu,ui_deck}` 都没有，`import_original_art.py` /
   `sync_battle_ui_art.py` 也没登记 ⇒ **底板不画（文字照画）+ 出声**。要真补 = 导一张图进
   `Resources/Art/ui/`（不在本轮白名单）。
3. **领取起始卡组**：原版 `PlayfabWrapper.GrantStarterDeck` 那一跳（+ `RewardService.Collect` +
   `CampaignHandler.SetSelectedArmy/SaveSelectedArmy` + `AddTag(CollectedFirstDeck)`）**本机没有后端**
   ⇒ `Continue` 只 `Debug.LogWarning`（把 deckId / 督军 / 阵营报出来）**并关窗**（= 原版成功支的末句；
   不关的话这扇窗 `closeOnESC=0` + 暗底不关窗 ⇒ 死窗）。⛔ 不伪造「已发放」、不写存档。

## 六、我们侧的如实偏离（都写在代码注释里）

1. **没有 tween 引擎** ⇒ 原版 1 s 的 glow `DOScale/DOFade`、0.5 s 的 Continue 组淡入、
   `selectedFactionIconShine` 闪烁，**一律直接落终值**。
2. **督军立绘的竖构图兜底**（见 §五）。
3. **督军语音**（原版 `RawCardScript.GetChatSound` → Jingles）**不播** —— 那条「卡 → 语音」链在远端音频表，
   本机没有；⛔ 不编一声顶上。
4. **`War ParticleSystemUI Left/Right`**：prefab `m_LocalScale = 0` ⇒ 原版不画；我们节点照建、无粒子。
5. **入口**：原版在 FTUE onboarding 链里，我们**没有菜单版教程状态机** ⇒ 只建窗 + 注册，
   ⛔ 不伪造调用点（同 `TutorialMessagePopup` / `WelcomePopup` 口径）。

## 七、断言（`Editor/MainMenuScene.cs` 的 T26 一节）

窗参 5 条 · 压暗层 3 条 · Title/SubTitle 文案+字号+矩形 5 条 · 四张卡（数量/尺寸/位置）3 条 ·
卡内容（阵营名/徽记/Glow 尺寸/glowColor/alpha/立绘）6 条 · 出厂态 2 条 · openSound 2 条 ·
选中态（Continue 亮/角标亮/alpha/scale/不可再点/徽记/其它卡取消）7 条 · 描述留空 2 条 ≈ **35 条**。
期望值全部 = prefab / MB / 官方词条表的**字面量**（⛔ 不调被测窗的常量；openSound 那条特意写成字面量）。

## 八、typecheck 原文

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

（本会话按口径**不跑 `*.Run` 自检、不起 player**；只跑了这一条。）

## 九、没查清 / 没做

1. **`FeatureConfig.isDeckHidden(deck)`**（`Open()` 的过滤条件）—— 它读的是账号/活动配置（远端），
   本机判不了 ⇒ 我们**四副全建**；如果将来有「某些阵营被隐藏」的本地数据，要在 `BuildCards` 里补这一道。
2. **`selectedFactionIconShine`**（`UIEffectTweener`）的动画参数没复刻（见 §六·1）。
3. **`Select()`/`Deselect()`**（原版 `ToggleSelected` 末句，虚表 0x368）—— 键盘导航本仓未做，未实现。
4. **`OnDisable` 的 `AssetReferenceTyped.Release`** —— 本工程无 Addressables，无可释放项，未实现。
5. `40k_replace_display` 的导入（见 §五·2，需动 `Resources/Art/`，不在白名单）。
