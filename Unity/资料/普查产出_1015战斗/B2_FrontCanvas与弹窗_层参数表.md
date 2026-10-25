# B2 · 战斗【FrontCanvas 一族】+【弹窗/转场一族】层 × 参数表

- **生成**：2026-10-15（第三十五会话 · B2 子代理）
- **主判据（资产序列化字段）**：`d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena2/`
  （`GameObject/` `RectTransform/` `Transform/` `MonoBehaviour/`）+ 脚本类名 `bundle_Waprforge_monoscripts/MonoScript/`
  ⛔ 未用 `battlearena1`（它的全树 dump 是截断版）。全部数值由本轮**重解 JSON 现读**，不是抄现成文档。
- **判据 2（显隐/交互）**：`d:/2/tools/decomp_full/`（逐方法 `.c`）。文中形如 `类__方法.c:行号` 的都是直接读出来的。
- **判据 3（只当尺子）**：`d:/4/Unity/资料/战斗UI_原版对账表.md`（§二/§四/§三点五）。
- **同构前提**（CLAUDE.md 已记）：13 个战场在 2D 层完全同构 ⇒ 只做 battlearena2 一套。
- **组件里的 `Image` / `TextMeshPro` 也是 `MonoBehaviour/` 下的文件**（本 bundle **没有** `Image/` 目录）；`m_FileID` 一律 0，
  脚本走 `m_FileID=1` → `bundle_Waprforge_monoscripts`。
- **sprite 名**：本轮自建索引（扫全库 `SpriteAtlas/` 的 `m_PackedSprites`↔`m_PackedSpriteNamesToIndex` 配对 + 名字带 pathid 的 `Sprite/*.json` + `battle_sprite_pathid.json` / `battlearena1_sprite_map.json`），共 696 条。
  ⚠️ 有 **19 处 pathid 冲突被覆盖**（跨包同号），所以「sprite 名」是**强旁证、不是绝对判据**；`pathid=名` 两个都给。
- **坐标**：`aMin/aMax/piv/pos/size` 全部照抄 serialized（**不是**屏幕绝对矩形）；`size` 是 `m_SizeDelta`。

---

## 〇、这一族的骨架（实测节点数）

| 节点 | GO pid | RT pid | 子树节点数 | 备注 |
|---|---|---|---|---|
| `BattleHud`（根） | 230 | 1471 | 1083 | 挂 `Animation / OvertimeUi / BattleHud`；父 = `BattlePrefab`(RT 1348) |
| `BattleHud/Canvas` | 1006 | 2952 | 1074 | 唯一 `CanvasScaler`（1920×1080 / ScreenMatchMode=1 Expand / PPU 100） |
| `Canvas/FrontCanvas` | 618 | 3093 | **790** | `Canvas`{renderMode=1 ScreenSpaceCamera, overrideSorting=**True**, sortingLayerID=**2007638115**, sortingOrder=0, planeDistance=100, additionalShaderChannels=25} |
| `FrontCanvas/Safe area FrontCanvas` | 727 | 3055 | 787 | 全屏 stretch，无脚本 |
| `FrontCanvas/ShadePanel` | 778 | 3145 | 1 | **inactive** |
| `FrontCanvas/TooltipManager` | — | 3083 | 1 | 不在 Safe area 下 |

> `Canvas` 的三个直接子（`Aspect Ratio Filler Top/Bottom`、`BackCanvas`）不在本任务范围。
> `FrontCanvas` 的 Canvas 字段 **与 0827 报告记的 arena1 值逐条相同**（renderMode 1 / overrideSorting True / sortingLayerID 2007638115 / planeDistance 100）。

---

## 一、逐元素「层 × 参数」表（含显隐条件）

> 列义：**RT** = `aMin / aMax / pivot / pos / sizeDelta [/ localScale]`；**组件** = 除 `RectTransform`/`CanvasRenderer`/`Canvas` 外的组件与脚本类名；
> **active** = `m_IsActive` 场景初值；**显隐条件** = 反编译里写 `SetActive` 的方法（`文件:行`）；写「未查到」就是不猜。

### 1.1 `Drag Attack Selector`（整槽 9 节点）

| 元素 | RT | 组件（脚本类） | Image / TMP 关键值 | active | 显隐条件 |
|---|---|---|---|---|---|
| `Drag Attack Selector` | `(0.5,0.5)/(0.5,0.5)/piv(0.5,0.5)/pos(0,0)/size(621.725,122.247)` | `EverguildGraphicRaycaster`(ignore=1,blockingMask=32) · **`HorizontalLayoutGroup`{enabled=1, align=4 MiddleCenter, spacing=37, forceExpand 0/0, childControl 0/0, childScale 0/0, reverse=0}** · `AttackTypesButtonsController` · `UnitOnBoardAttackTypeSelector` | — | ✅ | `UnitOnBoardAttackTypeSelector.Toggle(bool,Entity)` → **`SetActive(self.gameObject)`**（`UnitOnBoardAttackTypeSelector__Toggle.c:39`）；条件里读实体字段与 `EntityScript.HasCurrentTrait(0x4C4)`（`:52`） |
| ├ `Select Attack Background` | `(0.5,1)/(0.5,1)/piv(0.5,1)/pos(0,-18.375)/size(780,285)/**sc(0.44,0.44)**` | `LayoutElement`{ignoreLayout=**1**} · `Image` · `SelectAttackBackgroundController`{**inputScaleModifier=1.5, elementsHorizontalScaleModifier=1.5**} | sprite=**None**(m_Type=0, 运行时画) col=1,1,1,1 PA=0 **RTgt=1** | ❌ **inactive** | 显示时 `sizeDelta` 被 `ChangeSizeByElements` 改（含 ×1.5）；⇒ 实绘 `780×285×0.44×1.5 = 514.8×125.4` |
| ├ `Select Melee Button` | `(0,0)/(0,0)/piv(0.5,0.5)/pos(0,0)/size(118.372,118.806)` | `EverguildButton`(tgtGraphic=4404, interact=1, **onClick 0 条**) · `EverguildButtonMaterialModifier` · `CardDisplayAttackTypeButton`{attackType=**1**, highlightImage=PPtr(95), scaleMultiplierWhenSelected=**1.3**, directionToPivotPoint=**(-0.5,-1)**, furtherPointDistance=**45**, disableTimeAfterPointerExit=0.1, tooltipVerticalOffset=100} | sprite `8511722742595327015 = Attack type button Melee` type=0 col=1,1,1,1 PA=0 **RTgt=1** | ✅ | `AttackTypesButtonsController.ToggleAttackTypesSelector` → 对 `attackTypeButtons` 每个元素 `SetActive(1)`（`AttackTypesButtonsController__ToggleAttackTypesSelector.c:47`） |
| │ └ `Highlight` | `(0.5,0.5)/(0.5,0.5)/piv(0.5,0.5)/pos(0,0)/size(176.23,176.875)` | `SimpleRotation`{speed=(0,0,**100**)} · `EverguildButtonMaterialModifier` · `Image` | sprite `-7315808829623560242 = Attack type button highlight` col=1,1,1,1 PA=0 **RTgt=1** | ✅ | 随选中态（`CardDisplayAttackTypeButton.HighlightIfNeeded`） |
| ├ `Select Active Skill Button` | 同 Melee（`pos(0,0)` / `size(118.372,118.806)`） | `EverguildButton`(tgtGraphic=**4249**, onClick 0 条) · `CardDisplayAttackTypeButton`{attackType=**4**, buttonIcon=PPtr(4249), buttonIconValue=PPtr(3952), highlightImage=PPtr(392), scaleMultiplier=1.3, **directionToPivotPoint=(0,-1.1)**, **furtherPointDistance=80**, tooltipVerticalOffset=**-100**} | sprite **None**（运行时 `SetButtonIcon` 赋技能图标） | ✅ | 同 Melee |
| │ ├ `Highlight` | `piv(0.5,0.5)/pos(0,0)/size(176.23,176.875)` | `SimpleRotation`{speed=(0,0,100)} · `Image` | 同 `Attack type button highlight` | ✅ | 同 Melee |
| │ └ `ValueText` | `(0,1)/(0,1)/piv(0.5,0.5)/pos(59.638,-60.608)/size(91.88,**0**)` | `EverguildTextController` · `ContentSizeFitterMinMax` | TMP `'0'` **fs=89.01** col=1,1,1,1 align=2(Center) RTgt=1 | ✅ | 数值运行时填；高度 0 = 按行高自动撑 |
| └ `Select Range Button` | 同 Melee | `EverguildButton`(tgtGraphic=4319, onClick 0 条) · `CardDisplayAttackTypeButton`{attackType=**2**, **directionToPivotPoint=(0.5,-1)**, **furtherPointDistance=45**} | sprite `5998267373089430251 = Attack type button Ranged` | ✅ | 同 Melee |
| &nbsp;&nbsp;└ `Highlight` | `piv(0.5,0.5)/pos(-0,0)/size(176.23,176.875)` | `SimpleRotation`{speed=(0,0,100)} · `Image` | 同 `Attack type button highlight` | ✅ | 同 Melee |

**兄弟序（`m_Children` 实测）**：`Select Attack Background(3254)` → `Select Melee Button(3563)` → `Select Active Skill Button(2819)` → `Select Range Button(2997)`。
`Background` 带 `m_IgnoreLayout=1` ⇒ **不占布局位** ⇒ 视觉横排左右序 = **近战 · 技能 · 远程**。

**控制器字段**（同节点上）：
`AttackTypesButtonsController{attackTypeButtons=[**3** 项], layoutGroup=PPtr(**4653**), accumulatedDragForMinDistance=**0.085**, backgroundController=PPtr(5417), tooltip=GUID 5f9ee715b3621469e86a3d592a16ae66}`
`UnitOnBoardAttackTypeSelector{buttonsController=PPtr(4437), targetReticleController=PPtr(5313)}`
🔑 **`layoutGroup=4653` 就是同一 GameObject 上那个 `HorizontalLayoutGroup` 实例**（本 bundle 该 GO 的组件 pid 实测 = `4437 / 4589 / 4653 / 4804`，与控制器的两个 PPtr 一一对上）⇒ 控制器**显式持有布局组引用**，`enabled=1`。

### 1.2 `ActiveSkillDesc`（13 节点）

| 元素 | RT | 组件（脚本类） | Image / TMP 关键值 | active | 显隐条件 |
|---|---|---|---|---|---|
| `ActiveSkillDesc` | `(0.666,0)/(0.966,0.304)/piv(0.5,0.5)/pos(-606.11,55.001)/size(-0,-3.51)` | `ActiveSkillDisplay`{hideSkillBtn=1085, cardFrame=5081, bgLight=4720, actingLight=4510, pressingLight=4423, nameText=4021, costText=4145, descText=3933, manaIcon=4328, costSprite=**0**, fadeTime=**0.2**, xOffsetOffScreen=**4.5**, targetsAvailableText=3960, startPos=(0,0,0)} | — | ✅ | `ActiveSkillDisplay.FadeIn/FadeOut`；`StopDisplayingSkillFromManager` |
| ├ `HideAbilityButton` | `(1,0)/(1,0)/piv(1,0)/pos(220.39,-146.01)/size(**3707.83,1985.32**)` | `EventTrigger` · `Image` | sprite `1660267235368898380 = Background` m_Type=**1 Sliced** col=(1,1,1,**0**) RTgt=1 | ✅ | 全透明**点击拦截层**（点面板外关掉），超屏覆盖 |
| ├ `AbilityContainer` | `(0.069,0.083)/(0.913,0.764)/piv(0.5,0.5)/pos(0,0)/size(0,0)` | `EventTrigger` · `Image` | sprite None col=1,1,1,1 RTgt=1 | ❌ **inactive（arena2）**／✅ **arena1** | 🔴 **2026-10-25 就地订正（审查代理 `AU2` · 铁律 5）：本格原来写的那三条赋值点【全部指错节点】** —— `InitialSetup.c:11` 关的是 **`pressingLight`(+0x40)**、`FadeIn.c:18` / `FadeOut.c:25,41` 开关的是 **`hideSkillBtn`(+0x20)**（`FadeOut:41` 那颗是 `+0x40`）⇒ **`AbilityContainer` 在本 build 里没有任何 SetActive 赋值点**（arena2 全库深搜 `m_PathID 738` 只命中它自己的两个组件）。⇒ 它的显隐**就是场景值**：**arena1 = True、其余 12 场 = False**（13 份 `GameObject/AbilityContainer.json` 逐份现读）。⚠️ **运行时实况 dump 是 arena1**（`README.md:30` 的 `arena1_0912`）⇒ 那一行 `activeSelf=True` 只代表 arena1，不能推广到另外 12 场。另：那颗 `Image` = **`ActiveSkillDisplay.cardFrame`（+0x28，`MB_4565` 的 PPID 5081）** ⇒ `FadeIn`/`FadePower` 把它的 **alpha 0→1**（`fadeTime 0.2`，目标 = `DAT_1834b2bb8` **实测 1.0**，RVA 读 4 字节）⇒ **运行时是一块不透明白板**；而 `NameText/CostText/DescText` 都是**白字**（材质 `Asar-Regular Atlas Material`，`_FaceColor=(1,1,1,1)`、`_OutlineWidth=0`）⇒ **白底白字，静态判据自相矛盾**，这一格**仍未闭合**（详见 `普查产出_1025/AU2_战斗HUD与界面_审查与修复.md` §判据不足） |
| │ ├ `NameText` | `(0.038,0.632)/(0.428,0.934)/piv(0.5,0.5)/pos(0,0)/size(-0,0)` | — | TMP `'Fire Arrow'` **fs=55.4** 白 align=2 | ✅ | 运行时填 |
| │ ├ `CostPanel` | `(0.741,0.611)/(0.961,0.913)/piv(0.5,0.5)/pos(-0,0.599)/size(-0,1.198)` | — | — | ✅ | — |
| │ │ ├ `CostText` | `(0.444,0)/(0.969,1.005)/piv(0.5,0.5)/pos(-0,0)/size(0,0)` | — | TMP `'10'` **fs=57** 白 align=2 | ✅ | — |
| │ │ └ `CostIcon` | `(0,0)/(0.381,1)/piv(0.5,0.5)/pos(-0,0)/size(0,0)` | `Image` | sprite None col=1,1,1,1 **PA=1** RTgt=0 | ❌ **inactive** | 运行时 set |
| │ └ `DescText` | `(0.038,0.126)/(0.884,0.611)/piv(0.5,0.5)/pos(0.167,-0.5)/size(-0.334,1)` | — | TMP `'Deal 1 damage to a target character target character\n'` fs=**45** align=**1**(Left) | ✅ | — |
| ├ `Lights` | `(0.031,0)/(0.963,0.847)/piv(0.5,0.5)/pos(0,0)/size(0,0)` | — | — | ✅ | ⚠️ **与能量区 Lights 无关**（对账表 §15-5 已记） |
| │ ├ `LightActing` | 全 stretch | `Image` | sprite None col=(1,1,1,**0.392**) RTgt=0 | ❌ | `SetupActiveSkill` 逐灯 `SetActive(lVar5,uVar3)`（`__SetupActiveSkill.c:144`） |
| │ ├ `LightAvailable` | 全 stretch | `Image` | col=(**0.92543,1,0**,0.392) | ❌ | 同上 |
| │ └ `LightPressed` | 全 stretch | `Image` | col=(**0.00602674,0,1**,0.392) | ❌ | 同上 |
| └ `TargetsAvailableText` | `(0.158,0.764)/(0.845,0.929)/piv(0.5,0.5)/pos(-0,0)/size(-0,0)` | — | TMP `'0 available'` fs=**44.65** align=2 | ❌ | `InitialSetup` → `SetActive(false)`（`__InitialSetup.c:15`） |

### 1.3 `Unit Chat`（**56** 节点；简报写 52 —— 差 4，未逐条定位，最可能是三个 `*AudioSource` 与 `ChatPopup` 的计法）

| 元素 | RT | 组件（脚本类） | Image / TMP 关键值 | active | 显隐条件 |
|---|---|---|---|---|---|
| `Unit Chat` | 全 stretch | `VoiceLinesController`{priority=1, status=0, chatButton=5507, chatMuted=0, unitsVoiceLinesPanel=5422, communicationsManager=4567, voiceLinesPopupSelector=4429} · `UnitsVoiceLinesPanel`{playerVoiceLineBox=4469, opponentVoiceLineBox=5432, radioMessage=5516} | — | ✅ | — |
| ├ `PlayerChatDisplay` | `(0,0)/(0,0)/piv(0,0)/pos(12.613,200)/size(648.774,236.505)` | `VoiceLineBox` · `TransformScalerBySmallScreenUI` | — | ✅ | `UnitsVoiceLinesPanel.ShowChatBox/HideChatBox` |
| │ ├ `Mask` | `piv(0.5,0.5)/pos(-219.895,5.1)/size(144.457,157.465)` | `RectMask2D` | — | ✅ | — |
| │ │ └ `CardImage` | `piv(0.5,0.5)/pos(-2.48,-5)/size(176,176)` | `Image` | sprite None RTgt=1（运行时 set 卡图） | ✅ | — |
| │ ├ `Background` | `(0,0)/(0,0)/piv(0.5,0.5)/pos(324.387,118.252)/size(648.774,236.505)` | `Image` | sprite `-6417496503820834197 = 40k_voicelines_radio` | ✅ | — |
| │ ├ `wave` | `piv(0.5,0.5)/pos(111.11,49.6)/size(387.946,62.601)` | `AudioWaveEqualizer` · `Gradient2` · `Image` | sprite `-8253178407798656119 = 40k_voicelines_radio_wave equalizer` col=(0.698825,0.981132,0.926518,1) **RTgt=0** | ✅ | — |
| │ └ `ChatText` | `(0.291,0.258)/(0.974,0.604)/piv(0.5,0.5)/pos(-1.093,0.749)/size(-2.187,-0.02)` | `EverguildTextController` | TMP `'Here goes a chat line'` fs=**33** align=1 RTgt=1 | ✅ | 打字机（`maxVisibleCharacters`） |
| ├ `PlayerChatAudioSource` | `piv(0.5,0.5)/pos(0,0)/size(100,100)` | `AudioSource` · `AudioSourceMixerGroupFixer` | — | ✅ | — |
| ├ `EnemyChatDisplay` | `(0,1)/(0,1)/piv(0,0)/pos(12.613,**-410**)/size(648.774,236.505)` | `VoiceLineBox` · `TransformScalerBySmallScreenUI` | 子结构同 Player（ChatText pos=(-1.093,**-0.94**)） | ✅ | 同上（对手侧） |
| ├ `EnemyChatAudioSource` | `pos(0,0)/size(100,100)` | `AudioSource` · `AudioSourceMixerGroupFixer` | — | ✅ | — |
| ├ `ChatPopup` | `(0,0)/(0,0)/piv(0,0.5)/pos(0,479.81)/size(815.044,475.47)` | `CanvasGroup` · `VoiceLinesPopupSelector` · `TransformScalerBySmallScreenUI` | — | ❌ **inactive** | `VoiceLinesController.DisplayChatBox/CloseChatBox/ForcePopUpClose`；`PopUpShowed/PopUpHid` 广播 |
| │ ├ `CloseChatPopup` | `piv(0.5,0.5)/pos(2.45,41.66)/size(**4055.34,2114.44**)` | `NonDrawingGraphic` · `EventTrigger` | — | ✅ | 点外部关掉 |
| │ ├ `ChatButtons` | `(0,0)/(0,0)/piv(0.5,0.5)/pos(371,206)/size(722.66,403.13)` | — | — | ✅ | — |
| │ │ ├ `BG` | `piv(0.5,0.5)/pos(28.816,5.615)/size(557.314,301.346)` | `Image` | sprite `-3734815765473162916 = White Square` col=(0.00155419,0.0660377,0,1) RTgt=0 | ✅ | — |
| │ │ ├ `BGFrame` | `piv(0.5,0.5)/pos(2.222,-1.572)/size(716.882,399.1)` | — | 四边图：`40k_UnitChat_Background_Top/Bottom/Right/Left` (Top pos(16.372,171.305) size(517.687,33.923) · Bottom pos(17.109,-153.833) size(517.687,36.532) · Right pos(317.094,13.59) size(83.65,371.064) · Left pos(-300.237,-5.407) size(117.321,386.982)) | ✅ | — |
| │ │ └ `Buttons` | `piv(0.5,0.5)/pos(8,2)/size(557.31,296.06)` | **`VerticalLayoutGroup`{spacing=1.48, align=1 UpperCenter}** | — | ✅ | — |
| │ │ &nbsp;&nbsp;└ `ChatButton` ×6（无后缀 / (1)…(5)） | `(0,1)/(0,1)/piv(0.5,0.5)/pos(278.655, **-24 / -73.48 / -122.96 / -172.44 / -221.92 / -271.4**)/size(603.6,48)` | `Button`{tgtGraphic=5202/5258/4733/4788/4454/4774, interact=1, **onClick 0 条**（连线在 `ChatPopupButton` 里，不走 UnityEvent）} · `ChatPopupButton` · `Image` | 本体 sprite **None** col=(1,1,1,**0**) RTgt=1 ⇒ **透明命中层** | ✅ | 由 `VoiceLinesController.SetUpChatOptions` 填 6 段台词 |
| │ │ &nbsp;&nbsp;&nbsp;&nbsp;├ `bg` | `piv(0.5,0.5)/pos(23.724,0)/size(550.405,44.5)` | `Image` | sprite `6162082053088366362 = 40k_voicelines_bt_R` | ✅ | — |
| │ │ &nbsp;&nbsp;&nbsp;&nbsp;├ `Text (TMP)` | `(0,0)/(1,1)/piv(0.5,0.5)/pos(26.71,0)/size(-74.902,0)` | `EverguildTextController` | TMP `'Greetings'` fs=**30** align=1 | ✅ | — |
| │ │ &nbsp;&nbsp;&nbsp;&nbsp;└ `button` | `(0,0.5)/(0,0.5)/piv(0.5,0.5)/pos(23.5,0)/size(40,40)` | `Image` | sprite `-1396804370094666590 = 40k_voicelines_bt_L` | ✅ | — |
| ├ `RadioChat` | `(1,1)/(1,1)/piv(1,1)/pos(-25,-126)/size(648.774,236.505)` | `VoiceLineBox` · `TransformScalerBySmallScreenUI` | 子结构与 Player 同（Mask/Background/wave/ChatText） | ❌ **inactive** | `VoiceLinesController.ShowRadioMessage` / `CloseRadio` |
| └ `RadioChatAudioSource` | `pos(0,0)/size(100,100)` | `AudioSource` · `AudioSourceMixerGroupFixer` | — | ✅ | — |

### 1.4 `Mulligan`（11 节点）

| 元素 | RT | 组件（脚本类） | Image / TMP 关键值 | active | 显隐条件 |
|---|---|---|---|---|---|
| `Mulligan` | 全 stretch | `MulliganManager`{mulliganButtonText=4035, mulliganTextLocalize=5414, mulliganTurnTextLocalize=5400, mulliganTextObj=**316**, buttonsGroup=**295**, mulliganButton=1239, mulliganAnchor=5098, enableToolTipsOnCard=1} | — | ✅ | `ShowMulliganElements(bool)` → `SetActive(mulliganTextObj)` + `SetActive(buttonsGroup)`（`MulliganManager__ShowMulliganElements.c:10,12`）＋ `Shade.SwitchShade`；`StopMulligan` → `false`（`__StopMulligan.c:11,15`）；`ActivateMulligan` → `true`（`__ActivateMulligan.c:30,42`）；`SetWaitingForEnemy` → `true`（`__SetWaitingForEnemy.c:16`） |
| ├ `MulliganAnchor` | `Transform`（**不是** RectTransform，无 anchor/size） | `CardsHorizontalLayout` | — | ✅ | **子节点 0** ⇒ 换牌卡位**运行期生成**（`CardsHorizontalLayout` 自己排） |
| ├ `ButtonsGroup` | `(0,0)/(1,1)/piv(0.5,0.5)/pos(-0,0)/size(-0,-0)` | — | — | ✅ | 同根的 `buttonsGroup=295` |
| │ ├ `MulliganContinueButton` | `(1,0)/(1,0)/piv(1,0)/pos(-24.226,41)/size(832.889,117.489)` | — | — | ✅ | — |
| │ │ ├ `Button` | `piv(0.5,0.5)/pos(132.11,0)/size(577.5,63.84)` | `EverguildButton` · `Button`{tgtGraphic=5508, interact=1, calls=**`?#ClickMulliganDone`**} · `Image` | sprite `5480819196723731418 = 40k_bt_underbutton` col=(0.368627,0.894118,0.587437,1) **pad=(0,-40,0,-40)** | ✅ | — |
| │ │ ├ `Text` | `piv(0.5,0.5)/pos(45.72,-0.799)/size(368.929,62.242)` | `EverguildTextController` · `Localize` | TMP `'Continue'` fs=**45** align=**4**(Right) RTgt=0 | ✅ | — |
| │ │ └ `CircleButton` | `piv(0.5,0.5)/pos(284.13,0)/size(80.469,79.636)` | `EverguildButtonStateFollower` · `Image` | sprite `9056225148749506697 = 40k_UI_bt_play` | ✅ | — |
| │ └ `HideMulliganButton` | `(0,0)/(0,0)/piv(0.5,0.5)/pos(143,131.74)/size(82.947,79.603)` | `EverguildButton` · `Button`{tgtGraphic=5343, interact=1, calls=**`?#ToggleMulliganVisibility`**} · `Image` | sprite `-7255197835733746773 = 40k_UI_bt_eye` **PA=1** RTgt=1 | ✅ | 「眼睛」= 显隐开关；`MulliganManager.ToggleMulliganVisibility` 读 `activeInHierarchy` 取反 |
| └ `MulliganText` | `(0.5,1)/(0.5,1)/piv(0.5,0.5)/pos(7,-106.5)/size(1344,79.44)` | — | — | ✅ | 同根 `mulliganTextObj=316` |
| &nbsp;&nbsp;├ `Text` | `(0,0)/(1,1)/piv(0.5,0.5)/pos(-0.972,19)/size(-36.936,-10)` | `EverguildTextController` · `Localize` | TMP `'Choose cards to replace in first hand'` fs=**65** align=2 | ✅ | — |
| &nbsp;&nbsp;└ `TurnText` | `(0,0)/(1,1)/piv(0.5,0.5)/pos(-0.972,**-50**)/size(-36.936,-25.272)` | `EverguildTextController` · `Localize` | TMP `'You go second'` fs=**55** align=2 | ✅ | 先手/后手文案 |

### 1.5 `Generic Multi Card Display Combat`（68 节点）

| 元素 | RT | 组件 | Image / TMP | active | 显隐条件 |
|---|---|---|---|---|---|
| 根 | `(0,0.5)/(1,0.5)/piv(0.5,0.5)/pos(0,0)/size(0,818.04)` | `UIMultiCardDisplay` · `ScrollRect`{H=**1**, V=0} | — | ❌ **inactive** | 窗口式开（多卡横滑展示） |
| ├ `Menu Dark Background` | `piv(0.5,0.5)/pos(0,0)/size(4574.6,2572.36)` | `BackgroundOverDrawController` · `BackgroundCloseButton` · `Image` | sprite None col=(0,0,0,0.772549) RTgt=1 | ✅ | 压暗层点击 = 关窗 |
| ├ `Header Text` | `(0.5,1)/(0.5,1)/piv(0.5,1)/pos(0,0)/size(1192.37,63.204)` | — | TMP `'Header Text'` fs=**38** align=2 | ✅ | — |
| ├ `Viewport` | 全 stretch / **sc(0.7,0.7)** | — | — | ✅ | — |
| │ └ `Content` | `(0,0)/(1,1)/pos(-0.001,0)/size(-1917.69,0)` | `HorizontalLayoutGroup`{**spacing=500**, align=4} · `ContentSizeFitter` | — | ✅ | 卡位由代码 instantiate |
| │ &nbsp;&nbsp;└ `CardUI Reference` | `(0,1)/(0,1)/piv(0.5,0.5)/pos(1.272,-409.02)/size(2.544,3.369)/**sc(223.14,223.14)**` | `BasicCardUI` + `CardTextCountersController`×5 | 内部同 §二 的 `2DCard→Front`（尺寸逐条相同） | ✅ | **模板卡**（同 `DebugButtons/Card Preview` 那棵） |
| └ `BattleContinueButton` | `(1,0)/(1,0)/piv(1,0)/pos(-24.226,-53)/size(832.89,49.84)` | `Button`{tgtGraphic=4400, calls=**`?#ClickMulliganDone`**} · `EverguildButton` · `EverguildButtonStateFollower` | `Button/BG` sprite `5480819196723731418=40k_bt_underbutton` col=(0.368627,0.894118,0.587437,1) pad=(0,-40,0,-40) · `Text` TMP `'Continue'` fs=38 align=4 · `CircleButton` sprite `40k_UI_bt_play` | ✅ | — |

### 1.6 `ChooseCardMenu`（10 节点）

| 元素 | RT | 组件 | Image / TMP | active | 显隐条件 |
|---|---|---|---|---|---|
| 根 | 全 stretch | `ChooseCardMenu`{chooseMenuAnchor=4247, chooseButton=5114, titleTextField=723, buttonsGroup=571, selectCardSound=GUID 4006604330728631164} | — | ❌ **inactive** | 选卡流程开（`ChooseCardMenu` 脚本；`ToogleChooseMenuVisibility`） |
| ├ `ChooseCardMenuAnchor` | `Transform`（非 RectTransform） | `CardsHorizontalLayout` | — | ✅ | **子节点 0** ⇒ 卡位运行期生成 |
| ├ `ButtonsGroup` | `(0,0)/(1,1)/pos(-0,0)/size(-0,-0)` | — | — | ✅ | — |
| │ ├ `HideChooseButton` | `(0,0)/(0,0)/piv(0.5,0.5)/pos(143.4,133.17)/size(78.44,78.44)` | `EverguildButton` · `Button`{tgtGraphic=5137, calls=**`?#ToogleChooseMenuVisibility`**} · `Image` | sprite `-7255197835733746773 = 40k_UI_bt_eye` PA=1 RTgt=1 | ✅ | 眼睛开关 |
| │ └ `Continue Button` | `(1,0)/(1,0)/piv(0.5,0.5)/pos(-396.4,105)/size(548.015,75.905)` | `Button`{tgtGraphic=4408, **calls 0 条**} · `EverguildButton` · `Image` | sprite **None** col=(1,1,1,**0**) **ppum=0.67** | ✅ | — |
| │ &nbsp;&nbsp;├ `BG` | `pos(65.196,-5.749)/size(577.5,63.84)` | `Image` | `40k_bt_underbutton` col=(0.368627,0.894118,0.587437,1) | ✅ | — |
| │ &nbsp;&nbsp;├ `ContinueText` | `pos(-4.6,-5.263)/size(372.857,43.622)` | `Localize` | TMP `'Continue'` fs=45 align=4 | ✅ | — |
| │ &nbsp;&nbsp;└ `ContinueButton` | `pos(240.308,-3.388)/size(80.469,79.636)` | `Button`{calls=**`?#ClickChooseDone`**} · `Image` | `40k_UI_bt_play` | ✅ | — |
| └ `ChooseText` | `(0.5,1)/(0.5,1)/piv(0.5,0.5)/pos(7,-106.5)/size(1344,79.44)` | **`MeshRenderer`** · `Image` | sprite None col=(1,0.494118,0.945692,1) RTgt=1 | ✅ | ⚠️ 同节点带 `MeshRenderer`（与 `MulliganText` 组同族） |
| &nbsp;&nbsp;└ `Text` | `(0,0)/(1,1)/pos(-0.972,0)/size(-36.936,0)` | `Localize` | TMP `'Choose one card'` fs=**55** align=2 | ✅ | — |

### 1.7 `AboveShader`（8 节点）· `DebugButtons`（124 节点）· `PopupAnchor`（1 节点）

| 元素 | RT | 组件 | Image / TMP | active | 显隐条件 |
|---|---|---|---|---|---|
| `AboveShader` | 全 stretch | **无脚本** | — | ✅ | — |
| ├ `TipText` | `piv(0.5,0.5)/pos(1.06,91.78)/size(2450.13,99.697)` | `Image` | sprite `5480819196723731418=40k_bt_underbutton` col=(0.368627,0.894118,0.587437,**0.596078**) RTgt=0 | ❌ **inactive** | 提示条（`Text` = `'You must attack the minion with block'` fs=60 align=2） |
| └ `OvertimeSplashText` | 全 stretch | `CanvasGroup` | — | ❌ **inactive** | `OvertimeUi.DisplayOvertime` → `SetActive(true)` ×2（`OvertimeUi__DisplayOvertime.c:19,23`）。⚠️ `OvertimeUi` 脚本挂在 **`BattleHud` 根**，不在 AboveShader 上 |
| &nbsp;&nbsp;├ `Background` | 全 stretch / `pos(0,0)/size(716.622,699.891)` | `Image` | sprite None col=(0,0,0,0.533333) RTgt=1 | ✅ | — |
| &nbsp;&nbsp;└ `Text Container` | 全 stretch / `pos(-0,0)/size(2,-981)` | `Gradient2` · `Image` | sprite `-9022563678303461981 = 40k_dsplay_overtime` | ✅ | — |
| &nbsp;&nbsp;&nbsp;&nbsp;└ `Text` | `(0.5,0)/(0.5,1)/piv(0.5,0.5)/size(0,0.697)` | `ContentSizeFitter` · `Localize` | TMP `'OVERTIME!'` fs=**80** align=**32** | ✅ | — |
| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;└ `Image` | `(0,0.5)/(0,0.5)/piv(1,0.5)/pos(-15,0)/size(181.301,187.295)` | `Image` | sprite `7461627760614701739 = 40k_icon_overtime` **PA=1** RTgt=1 | ✅ | — |
| `DebugButtons` | `(1,0.5)/(1,0.5)/piv(1,0.5)/pos(0,0)/size(1920,1080)` | `BattleDebugController`（**无序列化字段**） | — | ❌ **inactive** | 正式版裁掉（CLAUDE.md 已记 `BattleDebugController` 在正式版被裁） |
| ├ `PlayerDebugButtons` | `(1,0.5)/(1,0.5)/piv(0.5,0.5)/pos(-157.349,-251)/size(314.688,248.65)` | `GridLayoutGroup`{**cellSize=(100,100)**, spacing=(0,0), constraint=0(Flexible), constraintCount=**2**} | 12 个按钮 + 标题 TMP `'Player'` fs=36；`Toggle Infinity Mana` 是 `EverguildToggle`（`Text (TMP)`=`'inf'` fs=52.75 + `Mark` inactive）；`ApplyEnvEffect` 的 `Text (TMP)`=`'E'` fs=89.7 | ✅（父 inactive 之下） | `onClick` 目标（持久化名）：`DiscardPlayerHand / DestroyPlayerMinions / HealPlayer / ShowPlayerCardPicker / AddPlayerCard / SummonPlayerCard / AddPlayerMana / RemovePlayerMana / DamagePlayerWarlord / RemoveSummonSickness / ShowPlayerEnvCardPicker` |
| ├ `EnemyDebugButtons` | `(1,0.5)/(1,0.5)/pos(-157.344,**337**)/size(314.688,212.66)` | 同上 | 11 个按钮 + 标题 TMP `'Enemy'` fs=36 | 同上 | `DiscardEnemyHand / DestroyEnemyMinions / HealEnemy / ShowEnemyCardPicker / AddEnemyCard / SummonEnemyCard / AddEnemyMana / RemoveEnemyMana / DamageEnemyWarlord / ShowEnemyEnvCardPicker` |
| └ `Card Picker` | `piv(0.5,0.5)/pos(0,0)/size(1400,800)` | `BoxCollider2D` · `UICollider` · `Image` | sprite None col=(1,1,1,0.752941) RTgt=1 | ❌ **inactive** | 3 个子系统：`Shade background`(col 0,0,0,0.780392; `BackgroundCloseButton`) · `DropDownList`(`ItemTemplate`❌/`MainButton`→`ToggleDropdownPanel`/`Overlay`→`ScrollPanel`(Mask+ScrollRect)→`Items`+`Scrollbar`；`Arrow`) · `Card Preview`(`CardUI` **sc(150,150)** →2DCard→Front；`PickButton` `'PICK'`→`PickCard`；`SummonButton` `'SUMMON'`→`SummonCard`) · `OSA`(`DebugCardPickerGrid`→`Viewport`(Mask)→`Content`；`BasicGridAdapterItem`→`Views`→`Card Name`/`Cost Image`(`Card Frame Cost Icon`)+`Cost`) · `Close`(`Button` calls=**`?#SetActive`**) · `InputField (TMP)`(`TMP_InputField`；sprite `8385535716932718412=InputFieldBackground`) |
| `PopupAnchor` | `piv(0.5,0.5)/pos(0,0)/size(100,100)` | `WindowHolder`{**placement=15**} · `EverguildGraphicRaycaster` | — | ✅ | `WindowHolder.OnEnable/OnDisable` → 向窗口管理器注册/注销自己的 transform（`WindowHolder__OnEnable.c` / `__OnDisable.c`）。**无子节点**（弹窗是运行期挂上来的） |

### 1.8 `BattleSettingsPanel`（78 节点）

| 元素 | RT | 组件 | Image / TMP | active | 显隐条件 |
|---|---|---|---|---|---|
| 根 | `piv(0.5,0.5)/pos(0,-29.317)/size(743.202,758.635)` | `BattleSettingsWindow`{type=0, windowsPlacement=0, openSound=0, closeSound=0, useDefaultCloseSoundIfNull=1, **closeOnESC=1**, extraScaleSmallScreen=**1.1**, resignButton=5052, closeButton=4233, musicSlider=4501, soundFXSlider=4641, voiceOverSlider=5012, debugButtons=120, winButton=4825, drawButton=4789, consoleButton=5131, backgroundCloseButton=5143, skipTutorialButton=5161, muteChatToggle=4996, voiceLinesController=4450, languageSelector=5491, autoZoom=4553} · `EverguildGraphicRaycaster` | — | ❌ **inactive** | 窗口式开（`BattleSettingsWindow` 继承窗口基类；`closeOnESC=1`） |
| ├ `Dark Shade` | `piv(0.5,0.5)/pos(-0,0)/size(3963.53,2692.97)` | `BackgroundCloseButton` · `Image` | sprite None col=(0,0,0,**0.611765**) RTgt=1 | ✅ | 点击关窗 |
| ├ `Generic Popup Background` | `(0,0)/(1,1)/pos(0,1.094)/size(-3.818,2.187)` | `Image` | sprite `-7511397500040153103 = 40k_popup` type=**1 Sliced** RTgt=1 | ✅ | 子 `Mask`(同 sprite, `Mask`) → `Background fill`(sprite `8740064623924276539=40k_popup_texture` type=**2 Tiled** **ppum=2**) |
| ├ `Generic Close Button` | `piv(0.5,0.5)/pos(364.514,345.3)/size(75,75)` | `EverguildButton` · `Image` | sprite `2381704724431365035=UI_Button_Round_background` PA=1 RTgt=0 | ✅ | 子 `Close Button` sprite `40k_bt_close` **pad=(-20,-20,-20,-20)** |
| ├ `Auto Zoom Toggle` | `(0.5,0.5)/(0.5,0.5)/piv(0,0.5)/pos(-282.42,249.48)/size(472.46,75.641)` | `EverguildToggle` | `Toggle` sprite `40K_dropdown_bg`(PA=1) → `CheckMark` sprite `40K_settings_icon_checkmark`(PA=1)；`Label` TMP `'Auto zoom'` fs=42 align=1 | ✅ | — |
| ├ `Debug Buttons` | `(0.5,1)/(0.5,1)/piv(0.5,0)/pos(3.873,50)/size(747.13,137.57)` | — | `Buttons container`(`HorizontalLayoutGroup`{spacing=0,align=4}) → `Win`/`Draw`/`Console`/`Toggle Parrticles`：每个 `Image` sprite `5651555388418207694=40K_button` col=(0.943396,0.164649,0.822019,1) **PA=1** + `Button Text` fs=38；`Toggle Parrticles` 带 `DebugToggleObjects`。另有 `Debug button text`(`'Debug Buttons'` fs=36) 与 3 条 `Border`(sprite `White Square`) | ✅ | — |
| ├ `Volume Sliders` | `piv(0.5,0.5)/pos(-1.998,-19.891)/size(701.35,441.398)` | `VerticalLayoutGroup`{spacing=0, align=4} | 三块容器 `Music / FX / Voiceover Container`（各 `size(701.35,100)`，`Image` col=(0.235849,0.235849,0.235849,1)）；每块一个 `Slider`：`Background` sprite `5759692323890646976=Volume_bar_inactive`(type=1, **ppum=2**) · `Fill` sprite `-6332789735026905802=Volume_bar_active`(type=1, ppum=2) · `Handle` sprite `-1471652398003037618=Volume_button`(PA=1, **pad=(-25,-25,-25,-25)**)；文案 `'Music'` / `'Sound Effects'` / `'Voice-overs'` fs=42 align=1。⚠️ 第三块的 Slider 节点**名字也叫 `Sound effects Slider`**（复制未改名） | ✅ | — |
| ├ `ChatToggle` | `(0,1)/(0,1)/piv(0.5,0.5)/pos(350.675,-454.87)/size(701.35,54.9)` | `EverguildToggle` | `Background` sprite `40K_toggle_off`(PA=1) → `Checkmark` + `OffText 'OFF'` fs=30；`Background On` sprite `40K_toggle_on`(PA=1) → `Checkmark` + `OnText 'ON'` fs=30；`Label` TMP `'Mute opponent'` fs=42 align=1 | ✅ | — |
| ├ `Bottom buttons` | `(0.5,0)/(0.5,0)/piv(0.5,0.5)/pos(0,68.84)/size(686.8,100)` | `HorizontalLayoutGroup`{spacing=0, align=4} | `Resign Button`(300×90, sprite `40K_button` col=(0.368627,0.894118,0.587437,1) PA=1, TMP `'Resign'` fs=38) · `SkipTutorial Button`(300×90, 同款, TMP `'Skip tutorial'` fs=38) | ✅ | — |
| ├ `Match Skulls` | `(0.5,0)/(0.5,0)/piv(0.5,1)/pos(-0.4,-20)/size(743.2,148.764)` | `HorizontalLayoutGroup`{spacing=0, align=4} | 文案 TMP `"Reduce your oponents' health to win more skulls:"` fs=**31.59** col=(1,1,1,0.631373) | ❌ **inactive** | 排位/骷髅模式才开 |
| └ `Language Selector` | `(0.12,0.5)/(0.9,0.5)/piv(0,0.5)/pos(0,317)/size(0,59.395)` | `LanguageSelector` | `LanguagesDropdown`(`TMP_Dropdown`；sprite `40K_dropdown_field_closed` col=(0.121569,0.972549,0.537255,1)) → `Label` fs=18 / `Arrow` sprite `40K_dropdown_arrow_closed` PA=1 / `Template` ❌(**inactive**)；`SelectLanguageText` TMP `'Select Language'` fs=42 align=1 | ✅ | — |

### 1.9 `Tutorial`（23 节点）· `LoadingMenu`（2）· `Alliance Panel`（39）· `TooltipManager`（1）

| 元素 | RT | 组件 | Image / TMP | active | 显隐条件 |
|---|---|---|---|---|---|
| `Tutorial` | `piv(0.5,0.5)/pos(0,0)/size(1920,1080)` | **无脚本** | — | ✅ | — |
| ├ `InitTutorialTip` | `piv(0.5,0.5)/pos(0,0)/size(100,100)` | — | `TipText` / `TipText (1)`：各 `MeshRenderer` + TMP `''` fs=**5.2** align=2 + `Localize`；子节点为 `SpriteRenderer`（`Bg`、`arrow1`） | ❌ **inactive** | 教程流程（`TutorialTipScript` 那一路） |
| ├ `TutorialArrows` | `Transform`（非 RectTransform） | `Transform` | `arrow1` / `arrow2` 均 `SpriteRenderer` | ❌ **inactive** | — |
| ├ `TutorialTip` | `piv(0.5,0.5)/pos(4.04,-0.99)/size(430,150)` | `CanvasGroup` · `TutorialTipScript` · `LayoutGroupContentFixer` · `TransformScalerBySmallScreenUI` | `Generic Popup Background`(`VerticalLayoutGroup`{spacing=0,align=4}+`ContentSizeFitter`；`Image` sprite `40k_popup` type=1 **ppum=2**；`Mask`→`Background fill` sprite `40k_popup_texture` type=2 ppum=2；`TipText` TMP `'Test tutorial tip'` fs=36 align=2)；`Bg` ❌(sprite `40K_display`)、`ContinueText` ❌(fs=4.5) | ✅ | — |
| └ `TutorialPointerCombat` | `piv(0.5,0.5)/pos(0,0)/size(1920,1080)` | `TutorialPointer` | `Pointer` sprite `-2590500462584155290 = Cursor_Space Marine_01`(PA=1) → `light` sprite `-592821608806009055 = Cursor_Space Marine_01Glow` col=(1,0.94569,0.504717,1)(PA=1) | ❌ **inactive** | — |
| `LoadingMenu` | 全 stretch | `LoadingMenu`{LoadingImage=5187, **StartFaded=1**} | — | ✅ | `LoadingMenu.Awake` → `SetActive(true)`（`LoadingMenu__Awake.c:19`）；`LoadingFade` → `true`（`__LoadingFade.c:35`） |
| └ `Panel` | 全 stretch / `pos(-0.01,2.85)/size(**12006.4,10394.9**)` | `AspectRatioFitter` · `Image` | sprite None col=(0,0,0,**1**) RTgt=1 | ❌ **inactive** | 淡入淡出 |
| `Alliance Panel` | `(0,1)/(0,1)/piv(**0.13,0.825**)/pos(-1.044,-25.272)/size(815.044,475.47)` | `CanvasGroup` · `BattleAlliancePanel`{avatarDisplay=4573, playerName=4315, playerTitle=5117, playerScore=5190, allianceGO=863, playerAllianceName=5453, allianceRatingDisplay=5188, backgroundCloseButton=4779, canvasGroup=3776, allianceBadgeDrawer=4672, notInAnAllianceMessage=4673} · `TransformScalerBySmallScreenUI`{**menuScale=1.3**} | `Close Background`(col=(0,0,0,0.462745))；`Background`→`BG`(sprite `40K_shop_offer_bg_Sororitas_0` col=(0.349057²,1)) + `BGFrame` 四边（同 `Unit Chat` 那四张 `40k_UnitChat_Background_*`）；`EnemyInfo`(`Avatar Item Small`→`Image Container`(`Highlight`/`Border`=`Player Profile Border` PA=1/`Image` sc(2,2)) + `PlayerInfo`(`Name:`/`Title:`/`Player Rating Display`) + `Alliance`(`Alliance:`/`Alliance Badge Drawer`/`Alliance Rating Display`))；`NotInaAllianceText` TMP `'This player is is still not part of an Alliance'` fs=32.9 col=(1,1,1,0.772549) | ❌ **inactive** | 窗口式开；⚠️ **对账表 §三点五 已记**：督军名牌一度抄错成这里的 `PlayerInfo/EnemyInfo` |
| `TooltipManager` | `piv(0.5,0.5)/pos(0,0)/size(100,100)` | `EverguildTooltipManager` | — | ✅ | 无子节点（tooltip 运行期建） |

### 1.10 `Canvas` 下那几件（**不在 Safe area 之下**）

| 元素 | RT | 组件 | Image / TMP | active | 显隐条件 |
|---|---|---|---|---|---|
| `ShadePanel` | 全 stretch | `Shade`{shade=**PPtr(0)**, shadeUI=PPtr(4413), **onAlphaLevel=0.8**, **defaultTimeToSwitch=0.5**} · `Image` | sprite None col=(**0,0,0,0**) RTgt=0 | ❌ **inactive** | `Shade.SwitchShade(bool)` → `SetActive`（`Shade__SwitchShade.c:21,44`）；`MulliganManager.ShowMulliganElements` 会调它（换牌时压暗） |
| `BattleDoors` | `piv(0.5,0.5)/pos(-0.064,-5.276)/size(1920.12,1118.98)/**sc(1.066,1.066)**` | `EndBattleDoors`{ratingText=4161, ratingIcon=4258, armyRatingIcon=5097, sealIconSprite=-300467167438017286, skullsObjs=[3], skullHolderObj=388, rewardsHolderObj=283, rewardHolderPrefab=4288, ratingObj=1087, allRewardsGroup=3773, victoryVideo=GUID b798baea…, defeatVideo=GUID d809c7c2…, drawVideo=GUID 6feab688…, videoPlayer=3788, videoImage=5177} | — | ❌ **inactive** | `EndBattleDoors.Awake` → `SetActive(false)`（`__Awake.c:10`）；`SetupDoor` → `true`(`:54`) 后按胜负逐件 `false`(`:78,82,84,86,90,102`)；`ShowRewards` 逐件 `true/false`(`:53,55,65,77,107,153,167`) |
| └ `Canvas` | 全 stretch / `pos(0,-0.012)` | `EverguildGraphicRaycaster` | — | ✅ | — |
| &nbsp;&nbsp;└ `EndBattlePanel` | 全 stretch | — | — | ✅ | — |
| &nbsp;&nbsp;&nbsp;&nbsp;├ `Background` | `piv(0.5,0.5)/pos(0,0)/size(**3840,2160**)` | `Image` | sprite `1660267235368898380=Background` type=1 col=(0.0283019²,0.752941) RTgt=1 | ✅ | — |
| &nbsp;&nbsp;&nbsp;&nbsp;├ `Video Image` | `piv(0.5,0.5)/pos(**-6,64**)/size(1920,1080)` | `RawImage` · `VideoPlayer` | — | ✅ | 胜/负/平三段视频（`victoryVideo` / `defeatVideo` / `drawVideo`） |
| &nbsp;&nbsp;&nbsp;&nbsp;└ `AllRewardsHolder` | `piv(0.5,0.5)/pos(0,**-260**)/size(100,100)` | `CanvasGroup` · `VerticalLayoutGroup`{**spacing=30**, align=7 LowerCenter} | `SkullsHolder`(`HorizontalLayoutGroup`{spacing=6.2,align=4}；`Image` sprite `40k_main_bt_nametag`；`skull1/2/3` size(**64.33,71.205**) sprite `-8668969440801097811=40k_battle_Win Skull`)；`RewardsHolder`(`HorizontalLayoutGroup`{spacing=**33**,align=4}；`Image` 同 nametag) → `RewardHolder`(RT 3676, `EndBattleRewardHolder`；`DrawerHolder` sc(1.2,1.2)；`Quantity` TMP `'2000'` fs=40) + `HolderRating`(HLG) → `Trophy`(sprite `40k_UI_icon_ranked_Skirmish` PA=1) / `ArmyIcon`(sprite None PA=1) / `RatingText`(TMP `'34'` fs=40) | ✅ | `EndBattleDoors.ShowRewards` |
| `Tutorial highlight` | 全 stretch | `ScreenHighlightPosition` · `Image` | sprite `-2216962027063784652 = Tutorial Highlight` col=(1,1,1,0.823529) RTgt=0 **ppum=0.01** | ❌ **inactive** | `ScreenHighlightPosition.Toggle` → `SetActive(true)`（`__Toggle.c:27`）；`HighlightPosition/Fade` 调 alpha |
| `Anim Anchors` | 全 stretch | 无 | 6 节点 = 自身 + **5 个锚点**（全 `(0.5,0.5)/(0.5,0.5)/piv(0.5,0.5)/size(100,100)`）：`UI Center` pos(0,0) · `CreateCard` pos(**885.563,84.273**) · `Cemetery` pos(**-1763.1,-10.765**) · `Stratagem ShowCase` pos(**529,83**) · `Next To Clock` pos(**766,83**) | ✅ | — |
| `UI Error Message Controller (MUST BE ENABLED)` | `(0.5,1)/(0.5,1)/piv(0.5,1)/pos(0,-213.6)/size(1920.7,336.64)` | `UIMessageController` | — | ✅ | `UIMessageController.ShowError/ShowMessage` —— 两个方法里**都没有 `SetActive`** ⇒ 走「实例化 + CanvasGroup 动画」；**具体显隐写法未查到** |
| └ `Container` | `(0,0)/(1,0)/piv(0.5,0.5)/pos(0,133.145)/size(0,266.29)` | `VerticalLayoutGroup`{spacing=0, align=7 LowerCenter} | — | ✅ | — |
| &nbsp;&nbsp;└ `Error Mensage_Ref` | `(0,1)/(0,1)/piv(0.5,0.5)/pos(**960.35,-241.29**)/size(1439.16,80)` | `CanvasGroup` · `UIErrorMessageItem` | `Background`(`ContentSizeFitter` + HLG{spacing=0,align=4}；`Image` sprite `40k_bt_underbutton` type=1 col=(0.358491²,1) RTgt=0) → `ErrorMesage Text` TMP `'ERROR MESSAGE CONTENT'` fs=**60** col=(**0.801887,0,0**,1) align=2 | ❌ **inactive** | 模板项，运行期实例化 |

---

## 二、`Card Display Window` 专节（**344** 节点；简报/对账表口径 282 —— 见 §四 不一致表）

> 根：`(0,0)/(1,1)` stretch，组件 `Animation` + `CardDisplayWindow`，**场景态 inactive**。
> `CardDisplayWindow` 序列化字段：`type=1, windowsPlacement=0, closeOnESC=1, extraScaleSmallScreen=1.0,
> cardUIs=[**5** 项], cardEffectSlots=[**5** 项], cardEffectsGroup=PPtr(1518), loreObjectText=4034, loreObjectBG=4474,
> backgroundButton=5022, tutorialObj=919, tutorialObjUnit=630, tutorialObjCost=469, displayAnimation=1776,
> relatedCardSwapTime=**0.25**, cardInBackGroundColorTint=(**0.65,0.65,0.65**,1), enableTutorial=1,
> options=PPtr(0), informationPanel=PPtr(0), tryShowResources=0, voiceOverButton=5069, showCardTextButton=PPtr(0), voiceOverAudioSource=PPtr(0)`

| 层 | RT | 组件 | 关键值 | active |
|---|---|---|---|---|
| `Menu Dark Background` | `piv(0.5,0.5)/pos(0,0)/size(4574.6,2572.36)` | `BoxCollider2D` · `Image` · `BackgroundCloseButton` · `BackgroundOverDrawController` | sprite None col=(0,0,0,**0.772549**) RTgt=1 | ✅ |
| `Card Display` | `piv(0.5,0.5)/pos(0,0)/size(**752,868**)` | — | 整个放大窗的框 | ✅ |
| ├ `LowerSection` | `piv(0.5,**0**)/pos(0,**-518.34**)/size(**1320,137**)` | `TransformScalerBySmallScreenUI` | — | ✅ |
| │ ├ `FlavourTextBG` | `piv(0.5,0.5)/pos(0,0)/size(1320,178)` | `Image` | sprite None RTgt=0 | ✅ |
| │ │ └ `LoreText` | `piv(0.5,0.5)/pos(0,0)/size(1250,93.47)` | `EverguildTextController` | TMP 长文案（`Ghazghkull Mag Uruk Thraka is a mighty prophet…`）fs=**32** align=2 | ✅ |
| │ └ `Voice Over Button` | `piv(0.5,0.5)/pos(734.33,0)/size(88.655,88.655)` | `EverguildButton` · `Image` | sprite `-2709381852941166021=40k_UI_bt_voicelines` **PA=1** **ppum=0.01** | ✅ |
| │ &nbsp;&nbsp;├ `Button Text` | `(0,0)/(1,1)/pos(0,0)/size(-16,0)` | `EverguildTextController` | TMP `'X'` fs=30 align=2 | ❌ **inactive** |
| │ &nbsp;&nbsp;└ `Image` | `(0,0)/(1,1)/pos(-1,0)/size(-2,-2)` | `Image` | 同 sprite PA=1 | ❌ **inactive** |
| ├ `Cards` | `piv(0.5,0.5)/pos(0,0)/size(100,100)` | — | 5 张卡的容器 | ✅ |
| │ └ **`CardUI` ×5**（`CardUI (4)/(3)/(2)/(1)` + `CardUI`） | **五份逐值相同**：`piv(0.5,0.5)/pos(0,60)/size(2.544,3.369)/sc(250,250)` | `BasicCardUI` + `CardTextCountersController`×5 | 每份 5 个子节点：`CreatedByText`❌(MeshRenderer+TMP `'Created by someone fancy'` fs=2.45) · `2DCard` · `Card Ready for level up`❌(sprite `Card Ready For Level Up`) · `New Card Badge`(sprite `WF_Special offer_Value` col=(0.54717,0.0875991,0.0542008,1)；子 `Text` 是**俄语**本地化串、源文件编码乱码) · `Ban Icon`(sprite `40k_Cross_icon_cross_big Banned card` **PA=1**) | ✅ |
| │ &nbsp;&nbsp;└ **`2DCard`** | `piv(0.5,0.5)/pos(0,0)/size(2.093,3.331)` | `BoxCollider2D`×2 · `CanvasGroup` · `CardTextsController` · `CardCollider2D` · `Card2DController` · `AutoCardRotation` · `UIGroupTint` · `CardTierUIController` · `UIImageGreyscaleController` | — | ✅ |
| │ &nbsp;&nbsp;&nbsp;&nbsp;├ `UI Collider` | 全 stretch / `pos(0,-0.02)/size(-0.2,-0.44)` | `UIGenericEventCatcher` · `NonDrawingGraphic` | — | ✅ |
| │ &nbsp;&nbsp;&nbsp;&nbsp;└ **`Front`** | `piv(0.5,0.5)/pos(0,**0.08**)/size(2.093,**3.491**)` | 无（纯容器） | — | ✅ |
| │ &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;├ `Card Highlight And Shadow` | `pos(0,-0.013)/size(**4.428,4.428**)` | `Image` · `UIImageMaterialColorChanger` | sprite **None**（运行时/Addressables SDF，见对账表 §三点六）PA=0 RTgt=0 | ✅ |
| │ &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;├ `CardImage` | `pos(0,-0.044)/size(**2.748,2.748**)` | `Image` · `CardImageController` | sprite None RTgt=0 | ✅ |
| │ &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;├ `Textbackgrounds` | `Transform`（**非 RectTransform**，2 子） | `Transform` | 子：`TextBackground Big UI`(`pos(-0.001,-0.65)/size(1.777,1.55)`；sprite `-6907984259834462938=Card Text smooth background` type=**1 Sliced** col=(0,0,0,0.647059) **ppum=14.7**) · `TextBackground Small UI`(`pos(0,-1.02)/size(1.863,0.962)` 同 sprite，**inactive**) | 组 ❌ |
| │ &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;├ `CardFrame` | `pos(0,-0.03)/size(**2.245,3.257**)` | `Image` · `SoftMaskScript` | sprite None RTgt=0 | ✅ |
| │ &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;├ `Card Info` | `Transform`（非 RectTransform，**9 子**） | `Transform` | 9 个子位（见下表） | 组 ✅ |
| │ &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;└ `EffectAnchor` | `Transform`（0 子） | `SortingGroup` | `m_SortingOrder` **未查到**（该组件文件里没有这个键） | ✅ |

**`Card Info` 的 9 个子位**（卡单位；`pos` 为相对 `Card Info`）：

| 子位 | pos / size | 组件 | 内容 |
|---|---|---|---|
| `Health Container` | `(0.74,-1.36)` / `(0.4,0.4)` | `EverguildTooltipTrigger` | `HealthText` TMP `'30'` fs=**0.34** align=2 |
| `Armour Container` | `(0.899,-0.991)` / `(0.3,0.3)` | — | `Image` sprite `-4840078238721171920=pedestal_icon_armor` RTgt=1 + `Armour Text` `'2'` fs=0.34 |
| `Range Attack Container` | `(-0.595,-1.4)` / `(0.4,0.4)` | `HighlightAttackType` · `EverguildTooltipTrigger` | `Range Attack Text` `'12\x03'` fs=0.34 |
| `Melee Attack Container` | `(-0.836,-1.19)` / `(0.4,0.4)` | `HighlightAttackType` · `EverguildTooltipTrigger` | `Melee Attack Text` `'5\x03'` fs=0.34 |
| `Cost Container` | `(0.788,0.666)` / `(0.4,0.4)` | `EverguildTooltipTrigger` | `Cost Background` sprite `-3526939114648998153=Card Frame Cost Icon`(pos 0,0 size(0.489,0.481)) + `CostText` `'1\x03'` fs=0.34 |
| `Cost Container Big` | `(0.788,0.643)` / `(0.475,0.475)` | `EverguildTooltipTrigger` | 同款放大（`CostText` fs=**0.475**）—— **inactive** |
| `Rarity` | `(0,-1.458)` / `(0.4,0.4)` | `CardRarityHolderController` | `Card Rarity Sprite` sprite `-3980338515175932349=1_40k_cardframe_rarity_common`(size(0.258,0.233) pos(0,-0.005)) |
| `Name and description` | `(0,-0.774)` / `(1.3,0.68)` | — | `MinionOrWarlord Container` ❌ / `Tactic Container` / `Created By Text` ❌(fs=0.3) |
| `Created By Text` 归在上一行内 | ⬆ | — | 见上 |

**`MinionOrWarlord Container` 的 10 个文本位**（全部 `sc(0.01,0.01)` = 1/100）：

| 位 | pos / size | TMP | 状态 |
|---|---|---|---|
| `NameTextUnit` | `(0,0.501)` / `(160,29.731)` | fs=**16** 橙 `(0.992157,0.603922,0.345098,1)` | ✅ 在 |
| `NameTextUnit Big` | `(0,0.401)` / `(162.979,48.711)` | fs=**21.15** | ❌ inactive |
| `NameTextUnit No description` | `(0,0)` / `(160,23.767)` | fs=**19** | ❌ inactive |
| `NameTextUnit No Description Big` | `(0,-0.137)` / `(160,57.547)` | fs=**24** | ❌ inactive |
| `DescTextUnit` | `(0.016,0.001)` / `(152.74,76.159)` | fs=**23.55** 米白 `(0.980392,0.913725,0.866667,1)` | ✅ 在 |
| `DescTextUnit Big` | `(0,0.118)` / `(160,103.645)` | fs=**26** | ❌ inactive |
| `RaceText` | `(0,-0.452)` / `(90,21)` | fs=**18** 橙 | ✅ 在 |
| `RaceText Big` | `(0,-0.435)` / `(90,22)` | fs=**22** 橙 | ❌ inactive |
| `ArmyTextUnit` | `(0,0.31)` / `(160,17)` | fs=**15** 白 | ❌ inactive |
| `Army No Desciption` | `(0,-0.18)` / `(160,15.13)` | fs=**15** 白 | ❌ inactive |

**`Tactic Container` 的 7 个文本位**：`DescTextTactic` `(0,-0.184)/(160,78.976)` fs=19 ✅ · `DescTextTactic Big` `(0,-0.088)/(160,99.293)` fs=25.2 ❌ · `DescTacticSmall` `(0,-0.09)/(160,62.641)` fs=19 ❌ · `NameTextTactic` `(0,0.313)/(157.794,26.352)` fs=21.5 ✅ · `NameTextTacticBig` `(0,-0.07)/(160,80)` fs=24 ❌ · `ArmyTextTactc` `(0,0.13)/(156,15)` fs=14 ❌ · `RaceText` `(0,-0.49)/(155,19)` fs=18 ❌

**`Card Display` 的另两支**：
- `TutorialObjs` `(0,0)/(1,1)/pos(-46.563,4.377)/size(3501.37,2291.01)` → `UnitObjs`（`MeleeText 'Melee Attack'` fs=30 + `Arrow` sc(**-1,1**) / `RangedText 'Ranged Attack'` fs=30 + `Arrow` sc(**-1,-1**) / `HealthText 'Health Points'` fs=30 + `Arrow` sc(**1,-1**)；箭头 sprite 一律 `7052792951493041307=UI_Description_Arrow` col=(0.669811³,1)）+ `EnergyText 'Energy Cost'` fs=30 `pos(497,252)/size(340.269,59.342)` + `Arrow`
- `EffectList`（`CanvasGroup`，**inactive**）→ `AffectedBy 'Affected by:'` fs=0.4 `pos(0.25,0.12)/size(4.43,0.53)/sc(0.885,0.885)` + `Elements`（**`VerticalLayoutGroup`{spacing=-0.09, align=0}**，`pos(0.439,-3.406)/size(4.968,6.437)`）→ **5** 个 `EffectElement`（`CardEffectItem`，`(0,1)/(0,1)/piv(0.5,0.5)/pos(2.484, -0.69/-1.979/-3.268/-4.557/-5.846)/size(4.968,1.379)`），每个 4 子：`EffectBg` `pos(0,0.014)/size(9.83,2.974)/sc(0.5,0.4)`(sprite `40K_display`) · `EnchanterText 'Blind Librarian'` fs=0.3 · `EffectText 'Get {0} Melee Attack, {1} Ranged Attack and {2} Health'` fs=0.3（`TextTooltipController`）· `line` ❌(sprite `-8906274027440120365=traitline`)；末一个 `CreatedBy` ❌ `pos(0,2.957)/size(3.4,0.35)` fs=2.9

🔑 **5 张 `CardUI` 与 5 个 `EffectElement` 与 `cardUIs=[5]` / `cardEffectSlots=[5]` 三个数字互证** ⇒ 这个窗口是「最多 5 张卡 + 每张最多 5 条效果」。

---

## 三、攻击选择器悬案 —— 裁定

### 三说各是什么

| 说 | 原文 | 出处 |
|---|---|---|
| ① **静态值说** | 「近战钮在槽**左下角**、Active 与 Range **叠在槽中心**」 | `资料/战斗UI_原版对账表.md` §四「悬案」表第 1 行（它自己注「但整条 Drag Attack Selector 的 activeSelf=False，这是预制体静态值，运行时很可能由代码重排」） |
| ② **横排说** | 「原版三钮同位叠加是错的、**以横排为准**」 | `Unity/MyGame/Assets/CardPresentation/Battle/AttackSelector.cs` 文件头（我们已按它实现：槽 621.7×122.2、钮 118.4²、间距 37、居中排开） |
| ③ **四钮同位说** | 「攻击选择器 = **四钮同坐标 (590,541.7)** 互斥 + Highlight 176.2 + ValueText fs89」 | `资料/战斗规格/战斗重建_0827/README.md:23`（⚠️ **该行在 README 里，不在 `审查更正清单_0827.md:23`**；那一份是 **Godot 时代的历史件**） |

### 实测是什么（`bundle_scenes_scenes_battlearena2`，逐字段现读）

1. **三个按钮的 RectTransform 完全相同**：
   `Select Melee Button (rt 3563)` / `Select Active Skill Button (rt 2819)` / `Select Range Button (rt 2997)`
   一律 `aMin(0,0) aMax(0,0) piv(0.5,0.5) pos(0,0) size(118.372,118.806)`，`localScale=(1,1)`。
   ⇒ **① 的措辞不成立**：不是「一钮在左下、两钮在中心」，而是**三钮全在同一个锚点**（都在槽的左下锚点 (0,0)，`piv=0.5` ⇒ 三钮静态完全重叠）。
   （复核：槽 `Drag Attack Selector` 自身 `aMin=aMax=(0.5,0.5) pos(0,0)`，其左下锚点在屏幕中心处 —— 所以「(590,541.7)」这类绝对坐标是从**同一份静态值**折算出来的，不是三钮真的分居三处。）
2. **父节点上有一个 `m_Enabled=1` 的 `HorizontalLayoutGroup`**：`spacing=37`、`m_ChildAlignment=4`(MiddleCenter)、`forceExpandWidth/Height=0/0`、`childControlWidth/Height=0/0`、`childScaleWidth/Height=0/0`、`reverse=0`。
   它的组件 pid = **4653**，而 `AttackTypesButtonsController` 的 `layoutGroup` 字段 = **PPtr(4653)** ⇒ **控制器显式持有这个布局组**（不是「碰巧有」）。
   ⇒ **布局组会在运行时重写这三个按钮的位置**；静态 `pos(0,0)` 是 **LayoutGroup 跑之前的模板位**（CLAUDE.md §三 已把这个坑记成通用规律）。
   `Select Attack Background` 带 `m_IgnoreLayout=1` ⇒ 不参与布局。
3. **兄弟序** = `Background → Melee → Active Skill → Range` ⇒ 横排左右序 **近战 / 技能 / 远程**。
4. **每个按钮还有一份「扇开」向量**（`CardDisplayAttackTypeButton` 的序列化字段）：
   `directionToPivotPoint` = 近战 `(-0.5,-1)` · 技能 `(0,-1.1)` · 远程 `(0.5,-1)`；`furtherPointDistance` = **45 / 80 / 45**。
   `AttackTypesButtonsController.MoveButtons` 对每个按钮调 `CardDisplayAttackTypeButton.MoveButton(f)`（`...__MoveButtons.c:41`），
   而 `MoveButton` 的算式是 `position = originalPosition + normalize(direction) * furtherPointDistance * f`（`CardDisplayAttackTypeButton__MoveButton.c`）⇒ **打开/拖拽时三钮各自向左下 / 正下 / 右下扇开**。
5. **`CardDisplayAttackTypeButton` 一共只有 3 个实例**（全库计数实测 =3）。⇒ **③ 的「四钮」原版不存在** —— 它描述的是**我们自己的 Godot 重实现**：`子代理读报_csharp行为_0827.md:416` 明写 `AttackTypes={**Melee,Ranged,Spell,Active**}`（4 个），且 `README.md:20` 说那一轮改的是 `battle.gd`。

### 结论（以谁为准）

- ✅ **② 横排为准**，并且要**加一条**：横排是**静息位形**，打开/拖拽时每个按钮再沿自己的 `directionToPivotPoint × furtherPointDistance` **向外扇开**（近战左下 45 / 技能正下 80 / 远程右下 45）。我们 `AttackSelector.cs` 的常量（槽 621.7×122.2 / 钮 118.4×118.8 / 间距 37 / 黄圈 176.2 / `scaleMultiplierWhenSelected` 1.3 / `accumulatedDragForMinDistance` 0.085 / 三条 `directionToPivotPoint`）**逐条与 arena2 实测相同**，无需改。
- ❌ **① 不成立**（措辞错：三钮是**同一锚点**，不是分居两处）。它的「同位」事实**来自模板位**，被 `HorizontalLayoutGroup` 覆盖。
- ❌ **③ 是另一件事**（Godot 时代的四钮重实现），**不能当原版判据**。
- ⚠️ **仍未钉死的一处**（如实写「未查到」）：`CardDisplayAttackTypeButton.SetOriginalPosition()` 把 `transform.localPosition` 缓存进 `+0x84`，`MoveButton` 以它为基准 —— 但**全量反编译里找不到 `SetOriginalPosition` 的调用点**（只找到定义）。
  ⇒ 扇开动画的**基准点**是「布局组排完的横排位」还是「(0,0)」，**本次判不了**。要钉死只能**进原版跑一次实况**（`资料/命令速查.md` 的 SceneJumpShot 链）。

---

## 四、与 `战斗UI_原版对账表.md` 不一致的地方 ⭐

| # | 对账表/文档原文 | 本轮实测（battlearena2） | 谁对 |
|---|---|---|---|
| 1 | §四 悬案：「攻击选择器三钮的位置 …**没定论**」 | **可定论**（见 §三）：HLG enabled + 控制器持有其引用 + 3 个按钮实例 + 扇开向量 | **本条可结案**；理由不是新资源，是**把「父节点上的 LayoutGroup + 控制器的 layoutGroup 字段」这两条一起读**（对账表当时只读了子节点） |
| 2 | §四 悬案：「① dump：近战在槽**左下角**、Active 与 Range **叠在槽中心**」 | 三钮 RT **完全相同**（`aMin=aMax=(0,0) pos=(0,0)`） | **对账表的转述不准**（见 §三 实测 1） |
| 3 | 「攻击选择器…**四钮**同坐标」(`战斗重建_0827/README.md:23`) | `CardDisplayAttackTypeButton` 实例数 = **3** | ③ 出自 Godot 重实现（那里 4 个），**不能当原版** |
| 4 | §二「攻击方式选择器」行：尺寸 621.7×122.2 / 底图 780×285×0.44 / 钮 118.8² / 黄圈 176.2² | **逐条相同** ✅ | 一致。**补充**：×1.5 有**两个**来源字段（`inputScaleModifier=1.5` **与** `elementsHorizontalScaleModifier=1.5`），对账表只写了「显示时再横向 ×1.5」 |
| 5 | §二「换牌面板」行：…「提示行 1344×79.4」 | 除 `Text` 外**还有第二行** `TurnText` TMP `'You go second'` fs=55（对账表没提） | 补漏 |
| 6 | §二「卡牌放大展示窗」行：「放大卡 ≈**467×743** px」 | `CardUI` = `sc(250,250)` × `size(2.544,3.369)` ⇒ **636×842.25**；其内 `2DCard` `2.093×3.331`×250 = **523.25×832.75**；`Front` `2.093×3.491`×250 = **523.25×872.75** | **对不上** ⇒ 建议按 CLAUDE.md 已记的「**卡体 832.825 不是 743**（743 是另一棵树）」为口径复核 743 那一处的出处 |
| 7 | §二「结算面板」行 | **逐条相同** ✅（Background 3840×2160 / Video Image 1920×1080@(−6,64) / SkullsHolder 648.1×52.4 / skull 64.3×71.2 / Trophy 60.1×60） | **补充**：根有 **localScale 1.066**；`AllRewardsHolder` 是 `VerticalLayoutGroup{spacing=30, align=7}` |
| 8 | §三点五 ③「`OvertimeIndicator` 默认关着 = 原版行为」 | ✅ 一致：`AboveShader/OvertimeSplashText` 场景态 **inactive**，`OvertimeUi.Awake` 自己关它 | 一致 |
| 9 | 简报「`Card Display Window` **282 节点**」 | 实测 **344** 节点 | 简报口径偏小（battlearena1 截断版所致）——**以 344 为准** |
| 10 | 简报「`Unit Chat` …共 **52 节点**」 | 实测 **56** | 差 4，**未逐条定位**（最可能是三个 `*AudioSource` + `ChatPopup` 的计法） |
| 11 | §四 悬案同格附注：「整条 `Drag Attack Selector` 的 **`activeSelf=False`**」 | arena2 实测 **`m_IsActive=True`**（同场景里 `Select Attack Background` 才是 **False**）；0827 报告（arena1）也记 `True`/`False` 这个组合 | **对账表那句附注错**（大概是「槽内底板 inactive」被写成了「整条 inactive」） |
| 12 | 简报「`Card Display→Cards→CardUI×5` + `CardUI Reference`」 | `Card Display Window/Card Display/Cards` 下**只有 5 张 `CardUI`，没有 `CardUI Reference`**；`CardUI Reference` 在 `Generic Multi Card Display Combat` 与 `DebugButtons/Card Picker/Card Preview`（后者叫 `CardUI`，`sc(150,150)`） | 简报把两个窗口的模板节点混了；**三处模板卡尺寸不同**：放大窗 `sc(250)` · 多卡横滑 `sc(223.14)` · debug 卡池 `sc(150)` |

---

## 五、查不到的格子汇总（⛔ 不猜）

1. **「PPtr → 目标名」解不出**（本 bundle 的 `MonoBehaviour/*.json` 有的按 `m_Name` 命名、**没有 pathid**，所以按 pid 反查会落空）：
   `layoutGroup=4653`(由同 GO 组件清单可锁定 = 那个 HLG ✅) · `backgroundController=5417` · `buttonsController=4437` · `targetReticleController=5313` ·
   `m_mulliganAnchor=5098` · `cardUIs[5]` / `cardEffectSlots[5]` 的**逐项** · `rewardHolderPrefab=4288` · `LoadingImage=5187` · `EffectBg` 之外的多数 PPtr。
   ⇒ 文中凡是 `PPtr(n)` 未带名字的，就是**没解出来**。
2. **`Button.m_OnClick` 的「目标 GO 名」**解不出（同上），只给出 PathID（表里写作 `?#方法名`）。
3. **`m_OnClick` 本身就 0 条的**（= 交互走脚本、不走 UnityEvent）：`ChatButton ×6`、`ChooseCardMenu/Continue Button`、`DebugButtons/Card Picker/ItemTemplate`、`OSA/Views`。**这是实测 0，不是没读到。**
4. **枚举语义未查到**：`Shade.shade`(PPtr 0) · `WindowHolder.placement=15` · `CardDisplayWindow.type=1` / `windowsPlacement=0` / `placement=15` 的可选值表 ·
   `CardDisplayAttackTypeButton.attackType` 的完整位表（已知 1/2/4，**未见更高位**） · `EffectAnchor` 的 `m_SortingOrder`（组件文件里没有该键）。
5. **`MulliganAnchor` / `ChooseCardMenuAnchor` / `TutorialArrows` / `Textbackgrounds` / `Card Info` 是 `Transform` 而非 `RectTransform`** ⇒ 它们**没有** `m_AnchorMin/SizeDelta`（不是「没读到」，是**字段不存在**）。
   `MulliganAnchor` / `ChooseCardMenuAnchor` 的**子节点数 = 0** ⇒ 换牌区 / 选牌区的卡位**运行期生成**，静态树里拿不到（与对账表 §二 那条备注一致）。
6. **`CardsHorizontalLayout` 的序列化字段未查**（该脚本在 `MonoScript` 里有类名，但本轮没进它的字段）。
7. **`UIMessageController` 的显隐写法未查到**（`ShowError` / `ShowMessage` 两个方法体里都没有 `SetActive`；也没在本次范围内找到 `Instantiate` 点）。
8. **`TutorialController` / `TutorialObjs`(脚本) / `InitTutorialTip` 的显隐判据未查到**（本场景里那几个节点没有可读的脚本实例）。
9. **`ScreenHighlightPosition.HighlightPosition/Fade` 的具体参数**未查（只读到 `Toggle` 的 `SetActive`）。
10. **俄语本地化串在源 JSON 里是乱码**（`New Card Badge/Text`、`Ban Icon/Banned Text`）⇒ 这两处文字**未采**（给不出正确串）。

---

## 六、顺手发现

1. 🔴 **`SlideRect`(ScrollRect) 的 `m_Content` 没在主表里体现**：`Generic Multi Card Display Combat` 根挂 `ScrollRect{H=1,V=0}`，但**Viewport/Content 是它的子节点**；而 `ChooseCardMenu` 根**没有** ScrollRect（选牌卡位靠 `CardsHorizontalLayout`）。→ 做多卡横滑时别照抄选牌那一套。
2. 🔴 **`DebugButtons` 在正式版被裁脚本，但整棵树还在场景里**（124 节点、`m_IsActive=False`）——`Card Picker` 里那套 `TMP_InputField` + `DropDownList` + `DebugCardPickerGrid` 是**本地就有的完整实现**，做「卡池/检索」类 UI 时可以当原版样板用。
3. 🔴 **`Alliance Panel` 与 `Unit Chat/ChatPopup` 的 `BGFrame` 用的是同一组四张 sprite**（`40k_UnitChat_Background_Top/Bottom/Right/Left`）——两块面板的边框可共用一份切图。
4. 🔴 **`ChatButton` 本体 `Image` 的 `col=(1,1,1,0)`（全透明）**，可见内容全在子节点 `bg`/`Text (TMP)`/`button` 上 ⇒ **命中区 = 全透明父图的 rect**（`603.6×48`）。若按「可见图」摆点击区会**偏小**（子 `bg` 只有 550.405×44.5）。
5. 🔴 **`BattleSettingsPanel/Volume Sliders` 的第三块容器里，Slider 节点名也是 `Sound effects Slider`**（复制未改名，三块分别在 `Music Container` / `FX Container` / `Voiceover Container` 下）——**按名字找会拿错**，必须按父节点取。
6. 🔴 **`AboveShader` 这个父节点上没有任何脚本**：加时 splash 的驱动脚本 `OvertimeUi` 挂在 **`BattleHud` 根**上（`BattleHud` 的组件清单 = `Animation / OvertimeUi / BattleHud`）——找「加时是谁在管」别在 AboveShader 下找。
7. 🔴 **`ShadePanel` 与 `TooltipManager` 是 `FrontCanvas` 的直接子节点，不在 `Safe area` 之下**；而任务里写的 `FrontCanvas/Drag Attack Selector` 这类路径**中间都还差一层 `Safe area FrontCanvas`**（0782-0827 的报告里也是这个形状）。
8. 🔴 **`EndBattlePanel` 里 `AllRewardsHolder` 的 `pos(0,-260)` + `VerticalLayoutGroup{spacing=30, align=7 LowerCenter}`**：骷髅条 (`SkullsHolder`) 与奖励条 (`RewardsHolder`) 是**上下两行、由布局组排**，两者**各自的 X 不是从父 pos 直接读的**。
9. 🔴 **`Anim Anchors` 的 5 个锚点全在屏幕中心坐标系下**：`Cemetery` 的 x=**−1763.1**（远在屏外左侧）——它是给「进墓地」动画当飞行终点用的，**不能当可见元素摆**。
10. 🔴 **`ChooseText`（`Choose one card` 那行）同节点上挂着 `MeshRenderer`**（`MulliganText` 的兄弟结构没有），本轮**没查清它为什么需要**——记一条疑点，**未查到**。
