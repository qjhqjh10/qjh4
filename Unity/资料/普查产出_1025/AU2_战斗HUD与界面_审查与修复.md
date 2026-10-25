# AU2 · 战斗 HUD 与战斗界面构件 —— 审查与修复（2026-10-25）

> 执行代理 `AU2`，范围 = **战斗 HUD 的布局 / 尺寸 / 颜色 / 显隐条件 / 渲染队列层级 / 文本格式**。
> 白名单 15 个文件（`Battle/{HudCanvas, HudPiece, WaitBanner, ReplayBar, Label, ImageQuad, WfSlider, EndPanel,
> SkillPanel, ChoosePanel, CardChoicePanel, MulliganPanel, BattleHudButton, ErrorMessageBanner, UnitChatPanel}.cs`）。
> ⛔ 未跑 Unity（红线①）· ⛔ 未动 git（红线②）· ⛔ 未改任何 `任务总表.md` / `CLAUDE.md` / `项目任务.md`。
> ✅ 改完跑过 `TMPDIR=/tmp/wf_au2 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（两次，最后一次在全部改动之后）。
> ⚠️ 按用户 2026-10-25 口径：**真机验收暂停** ⇒ 本文把「只能真机看」的单独列一节，⛔ 不拿它当收尾理由。

---

## 0. 结论摘要

| 项 | 数 |
|---|---|
| 审的「件」（HUD 构件 / 分层 / 文本格式） | **31** |
| 判据充分并**已修** | **3 件 / 5 处代码**（+ 3 处文档就地订正） |
| 判据**不足**（查不下去、⛔ 没猜） | **3 条**（技能面板底板 / 头像小名与电台聊天 / 回合高亮） |
| 只能真机（已单列） | **5 条** |
| 给 `BattleDriver.cs`（AU1 的件）的**移交账** | **4 条** |
| 需补断言（宿主 `Editor/BattleScene.cs` 不在白名单） | **3 条** |

---

## 1. 差异总表（逐件）

格式：`件 | 原版（值 + 出处） | 我们（文件:行号） | 判定`

### 1.1 已修正的三件（详见 §2）

| 件 | 原版 | 我们（改前） | 判定 |
|---|---|---|---|
| 结算面板整屏压暗 `EndBattlePanel/Background` | `m_Color = (0.0283, 0.0283, 0.0283, **0.7529**)` · `m_Sprite = Background`（Sliced）· 3840×2160 · `rt=1`（`bundle_scenes_scenes_battlearena2/MonoBehaviour/MonoBehaviour_5043.json` 现读） | **纯黑 α 0.97**（`EndPanel.cs` 建件处） | 🔴 **真错**：比原版**暗得多**（原版背景透 25%）⇒ **已修** |
| 选牌面板整屏压暗 `ShadePanel` | `Shade.onAlphaLevel = **0.8**`（`MonoBehaviour_4598.json`）· 底图 `m_Color (0,0,0,0)`/无 sprite/`rt=0` · 3 形参重载 ⇒ **硬切** | **α 0.55**（`CardChoicePanel.cs` 行内字面量，注释自认「我们挑的」） | 🔴 **真错**：与换牌面板那层（0.8）**同源不同值** ⇒ **已修** |
| 选牌面板：压暗层 / 候选卡的**层级** | 原版 `FrontCanvas` 的 `m_Children` = `[ShadePanel, Safe area FrontCanvas, TooltipManager]` ⇒ **压暗先画、面板与卡后画** ⇒ **卡永不被压暗**（RT_3093 现读） | 两颗都在**默认队列 3000**、z 也同为 `−0.9` ⇒ 先后只剩「到相机距离」这个含糊次序（压暗层中心 = 屏心 ⇒ 比任何一张卡都更近），而战场里队列 **3001/3003/3004/3005** 的 18 件又会盖住 3000 的卡 | 🔴 **真错（同一族 = `M1`/`D6`）** ⇒ **已修**（压暗 3006 / 卡 3007） |

### 1.2 逐件核对后**一致**（抽关键值，全部带出处）

| 件 | 原版 | 我们 | 判定 |
|---|---|---|---|
| `WaitText` 根 / 框 / 文字框 | 1344×79.44 · 1323×90 · 框 1270×70 · fs `[18, 55.3]` base 36 · 词条 `Battle/Mulligan/WaitEnemy` | `WaitBanner.cs` 同名常量逐值一致（`RootW/H` · `PopupW/H` · `WaitTextBoxW/H` · `WaitTextFs*`） | ✅ 一致 |
| `WaitText/Dark Shade` | 3963.5×3366 · α 0.6118 · `rt=1` + `padding(0,1000,0,0)` | `WaitBanner.cs` 同值；`raycast:true` + `ShadeRaycastPadBottomPx=1000` + `ShadeContains`（`W1` 已接线进 `BattleDriver`） | ✅ 一致 |
| 「等待敌人」横幅的**时机** | 只有「联网局、本地方已交换牌、对手还没交」；**单机恒不显示**（`N1`） | `BattleDriver.ShowWaitBanner` 静态纯函数（已落地） | ✅ 一致 |
| 回放条四颗钮 | 容器 293.6×57.41 · Holder 258.54×59.43 · 钮 79.8×48.57 · `ap` 逐值 · 显隐做在 Holder、`matchType==0xA0` | `ReplayBar.cs` 逐值一致；`Setup(matchType==0xA0)`（`BattleDriver:1039/4716`） | ✅ 一致 |
| 技能条三态色 | `LightActing (1,1,1,.392)` · `LightAvailable (0.92543,1,0,.392)` · `LightPressed (0.00602674,0,1,.392)`（B2:74-76） | `SkillPanel.LightActing/Available/Pressed` 逐值一致 | ✅ 一致 |
| `HideAbilityButton` | `aMin=aMax=(1,0)` · `piv(1,0)` · `ap(220.39,−146.01)` · `sd 3707.83×1985.32` · sprite `Background` Sliced · `col (1,1,1,**0**)` · `rt=1` | `SkillPanel.HideBtnW/H/AnchoredX/Y` 逐值；`col (1,1,1,0)` | ✅ 一致 |
| 换牌提示行字号 | `MulliganText/Text` 框 1307.064×69.44 · fs 65 / `[18,65]` / base **36** | `MulliganPanel.PromptBoxW/H` + `PromptFsMin/Max/Base` 逐值（原 0.55 系数已废） | ✅ 一致（`R5` 已修） |
| 换牌倒计时格式 | `"0:0" + 秒数`（字面量 `StringLiteral_20746`；`<10 s` 才刷） | `BattleDriver:7663` `SetDoneText("0:0" + s)` | ✅ 一致 |
| 换牌压暗层 | `Shade.onAlphaLevel = 0.8` | `MulliganPanel.ShadeAlpha = 0.8` + 世界 quad 3006 / 卡 3007 | ✅ 一致（`M1` 已修） |
| 选牌标题色 | `ChooseText` 挂 `Pragati-Regular Orange` `_FaceColor (0.996,0.596,0.118)`；Image **DISABLED** | `CardChoicePanel` 传同色、不画底板 | ✅ 一致 |
| 错误横幅（战斗档） | 底条 1439.16×80 · `40k_bt_underbutton` `col (0.3585³,1)` `rt=0` · 字 fs 60 `col (0.8019,0,0)` · 模板 `ap (960.35,−241.29)` **= 存档值** | `ErrorMessageBanner` 逐值一致，且**明确区分**「存档值 vs 布局算出来的 −510.24」 | ✅ 一致 |
| 语音条（`Unit Chat`） | 气泡 648.774×236.505 · `wave` 387.95×62.60 `col (0.6988,0.9811,0.9265)` · `ChatText` 框 440.9×81.7 · fs 33 `[10,33]` · `wave.rt=0` / `ChatText.rt=1` | `UnitChatPanel` 逐值一致（`TextW/H` · `OrigFontPx` · `WaveTint` · 三件 raycast 三条都写了） | ✅ 一致 |
| `WfSlider` 手柄**纵向位置** | 序列化 `m_AnchoredPosition.y = 6.1e-05`≈0；`Slider.UpdateVisuals()` **只写 anchors（y 恒 0→1）**、不写 `anchoredPosition` ⇒ 运行时**竖直居中** | `WfSlider` 竖直居中 | ✅ 一致（**原文自认「我们挑的」= 错，已订正**，见 §2·C） |
| `HudCanvas` 根层 | `ScreenSpaceCamera` · `CanvasScaler(1920×1080, Expand)` · 全场景**只一个** Scaler | `HudCanvas.Ensure` 同；子画布不加 Scaler | ✅ 一致 |
| 分层机制 | 原版靠 uGUI 排序层 / 兄弟序 | 我们 = 渲染队列 + 兄弟序（`HudCanvas.Add` 的 z 当兄弟序键） | ✅ 机制等价（⛔ 别用 z 判透明件先后 —— 本件头部与 `ImageQuad.SetRenderQueue` 都写着） |

### 1.3 记录在案、**不改**（原版本身如此 / 用户已拍板 / 例外①）

| 件 | 事实 | 依据 |
|---|---|---|
| 结算面板**标题/副标题** | 原版 `EndBattlePanel` 子树**零文字零按钮**；我们只有「视频不在播」时才显示自己那两行 | `EndPanel.cs` 文件头 `2026-10-20` 那段的现读；属「视频缺位时的替代」 |
| 结算面板**奖励行 / 评分** | 原版有、我们**不建**（服务器数据，用户 2026-09-17 拍板不做） | `EndPanel.cs` 文件头 |
| 错误横幅队列 `Q=4001` / `MenuQueue=4035` | 原版是独立 overlay Canvas（`PopUps` / order 1），号段只能等价逼近 | 文件头 §B，已如实标「[我们挑的]」 |
| `ChoosePanel` 卡行纵向位置 / 卡下按钮偏移 | 原版 anchor 节点**只有 Transform**、按钮 prefab 运行时实例化 ⇒ 静态无 anchor；现取值为可复算的判据（`(0,29,0)` × 父链；`322.618 px`） | `ChoosePanel.cs` / `CardChoicePanel.cs` 文件头 |
| `BattleHudButton` | `IPointerClickHandler` = 原版 `EverguildButton`（`Button` 基类）的松手沿 | 文件头 |

---

## 2. 已修清单（判据 + 落点 + 灭自证思路）

### A. 结算面板压暗层改成原版色（`Battle/EndPanel.cs`）

- **判据**：`bundle_scenes_scenes_battlearena2/MonoBehaviour/MonoBehaviour_5043.json` ——
  `EndBattlePanel/Background` 那颗 `Image`：`m_Color = (0.0283019, 0.0283019, 0.0283019, 0.7529412)` ·
  `m_Type = 1`（Sliced）· `m_Sprite = {FileID 9, PathID 1660267235368898380}` · `m_RaycastTarget = 1` ·
  `m_RaycastPadding = (0,0,0,0)` · `m_PixelsPerUnitMultiplier = 1`。
- **改法**：新增 `EndPanel.DimColor`（原值逐位），建件处改为传它（改前 `new Color(0,0,0,0.97f)`）。
- 🔴 **同时就地订正一条错记录（铁律 5）**：原文写「**我们没有那张图**」——
  **那张图本地有**，它是 **Unity 内置 UI sprite `Background`**：
  全库 `*/Sprite/*.json` 里 `m_Name == "Background"` 只有一份 =
  `bundle_Warpforge_unitybuiltinassets/Sprite/Background.json`（`m_Rect 32×32` · `m_Border (10,10,10,10)` ·
  `m_PixelsToUnits 200`），运行时 dump 那一列也印着 `IMG  Background`（`runtime_ui_dump_Battle_Arena_1.tsv:1150`）。
  它**只能靠编辑器 `AssetDatabase.GetBuiltinExtraResource<Sprite>` 取**（运行时无公开口），
  而 9 宫格 10 px 的角块落在 3840×2160 的四个角上（屏外）⇒ **可见面就是均匀色**
  ⇒ 1×1 白图 + `DimColor` **逐像素等价**。**贴图这一半如实记为「未接内置 sprite、可见面无差」**。
- **灭自证思路**（断言待补，见 §5）：断「`DimGraphicForTest.color` ≈ 原版四元组」+
  「δ = |α_我们 − α_原版| ≥ 0.2 时必红」，⛔ 不要只断「不是纯黑」。

### B. 选牌面板压暗 α 0.55 → 0.8（`Battle/CardChoicePanel.cs`）

- **判据**：`battlearena2/FrontCanvas/ShadePanel` 的 `Shade`（`MonoBehaviour_4598.json`）：
  `onAlphaLevel = 0.800000011920929` · `defaultTimeToSwitch = 0.5`；底图 `Image`（`MonoBehaviour_4413.json`）
  `m_Color = (0,0,0,0)` · `m_Sprite = null` · `m_RaycastTarget = 0`。
  调用点 = `ChooseCardMenu__ShowChooseCardElements.c`（末尾 `Shade__SwitchShade(bm.shade, on, 0, 0)`）与
  `MulliganManager__ShowMulliganElements.c:16` —— **选牌与换牌共用同一个 `ShadePanel`**（`BattleManager.shade`，`+0x230`）；
  3 形参重载 ⇒ `useAnim=false` ⇒ **硬切**。
- **改法**：`public const float ShadeAlpha = 0.8f`（取消行内 `0.55f`），并**就地订正**文件头「颜色/透明度没查到」那句。

### C. 选牌面板压暗层 / 候选卡的渲染队列（`Battle/CardChoicePanel.cs`）

- **判据**：`battlearena2/RectTransform/RectTransform_3093.json`（`FrontCanvas`）的 `m_Children` =
  **`[0] ShadePanel` · `[1] Safe area FrontCanvas` · `[2] TooltipManager`** ⇒ 原版**压暗先画、面板与卡后画**
  ⇒ **卡永不被压暗**；我们两颗都在世界空间且**同队列同 z** ⇒ 先后不可控（压暗层中心 = 屏心，比任何一张卡都更靠近相机；
  战场里队列 3001/3003/3004/3005 的 18 件又反过来盖住 3000 的卡）。
- **改法**：`QChoiceShade = MulliganPanel.QMulliganShade`(3006) / `QChoiceCard = MulliganPanel.QMulliganCard`(3007)
  —— **常量复用、不写第二份**（「战场最大 3005」的逐场计数与「< `CardFan.QCardBase` 3009」的上下界判据都在
  `MulliganPanel` 那两条 doc 里）；`_shade.SetRenderQueue(QChoiceShade)`、`LayOutCards` 里 `c.SetQueue(QChoiceCard)`。
  ⚠️ 选牌面板的候选卡由调用方 `Kill`（`ClearChooseViews` / `ClearEnvPickViews`）⇒ **不需要还原队列**
  （换牌那边复用手牌卡，所以它才要还 —— 两者生命周期不同，已在注释里写死）。
- **灭自证思路**：断 `_shade` 与候选卡的**材质 `renderQueue`** 大小关系（⛔ 别断 z），
  再加一条结构反例：把卡改回 3000 ⇒ 必红。

### D. 文档就地订正（3 处，铁律 5）

1. `Battle/WfSlider.cs` 文件头「我们挑的 #2：手柄纵向位置」——
   **原版序列化 `ap.y = 6.1e-05`≈0**（`普查产出_1007/波8_A169_WfSlider.md:177`），且
   `Slider.UpdateVisuals()`（本机 uGUI `Slider.cs:616-624`）**每次只写 anchors（横滑块 y 恒 0→1）**、
   **不写 `anchoredPosition`** ⇒ 运行时**就是竖直居中** ⇒ 本件与判据逐值一致，**不是「我们挑的」**。
   （顺带：A169 那句「音频九根锚 y 是 0→0」只是**序列化值**，运行时同样被改写成 0→1。）
2. `Battle/CardChoicePanel.cs` 文件头「压暗层颜色/透明度没查到」⇒ 改成「两样都有判据」。
3. `资料/普查产出_1015战斗/B2_FrontCanvas与弹窗_层参数表.md:67`（`AbilityContainer` 那一格）——
   原文把三条赋值点记成开关 `AbilityContainer`，**三条全部指错节点**：
   `InitialSetup.c:11` 关的是 **`pressingLight`(+0x40)`**、`FadeIn.c:18` 与 `FadeOut.c:25` 开关的是
   **`hideSkillBtn`(+0x20)`**（`FadeOut.c:41` 那颗也是 `+0x40`）⇒ **`AbilityContainer` 在本 build 没有任何
   SetActive 赋值点**（arena2 全库深搜 `m_PathID 738` 只命中它自己那两个组件）。
   它的显隐**就是场景值**：**arena1 = True、其余 12 场 = False**（13 份 `GameObject/AbilityContainer.json` 逐份现读）；
   而运行时实况 dump 用的是 **arena1**（`原版参照图/Unity参照管线_0825/README.md:30`）⇒ 那一行 `activeSelf=True`
   **不能推广到另外 12 场**。

---

## 3. 判据不足清单（⛔ 没猜、没改）

### 3.1 技能面板（`ActiveSkillDesc`）的底板 —— `B-23` 的「运行时压暗机制」仍不闭合

本轮**新挖到三条硬事实**（都是本轮现读，前几轮没有）：

1. **`ActiveSkillDisplay.cardFrame`（偏移 `+0x28`）= `AbilityContainer` 的那颗 `Image`（PathID 5081）**
   —— 实据：`MonoBehaviour_4565.json`（= `ActiveSkillDesc` 上的 `ActiveSkillDisplay`）的 `cardFrame` → 5081，
   而 5081 的 `m_GameObject` = 738 = `AbilityContainer`。
2. 它**被淡入**：`FadeIn` 先 `SupportMethods.SetSpriteAlpha(cardFrame, 0)`，再
   `FadePower(1.0, fadeTime=0.2)`；那个「1.0」是从 `GameAssembly.dll` 的 `DAT_1834b2bb8` 读出来的
   （RVA→文件偏移读 4 字节 = `00 00 80 3F`）⇒ **白板最终是不透明的**。
3. 它是 **`m_Sprite = null` + `m_Color (1,1,1,1)`** ⇒ uGUI 的 `Image.OnPopulateMesh` 在 `activeSprite == null` 时
   走 `Graphic` 的纯色面片（本机 `Image.cs:883-889` 现读）⇒ **运行时是一块不透明白矩形**；
   而它里面那三颗字 `NameText/CostText/DescText` 的材质是 **`Asar-Regular Atlas Material`**
   （`_FaceColor (1,1,1,1)` · `_OutlineWidth = 0` · 顶点色也是白）⇒ **白底白字**。

⇒ **静态判据自相矛盾**（「白板 + 白字」在游戏里不可能是这样），而两条能解释它的路都判不出：
① 那 12 个 arena 的 `AbilityContainer` 是 `m_IsActive=false` 且**无人赋值**（若真如此 ⇒ 原版这一屏**连字都没有**）；
② 另有一条运行时路径在改它的颜色/透明度，**本地语料里找不到**。

**因此本轮不动 `SkillPanel` 的底板**（现值：深色板 `rgba(.05,.06,.09,.86)`，文件里如实标着「我们挑的」）——
照搬「白底白字」会得到一个**玩家读不出字**的面板，那不是「更忠实」。
**要闭合还缺**：一次能跑到 `ActiveSkillDesc` 打开状态的原版实拍（或 RGBA 采样）。
⛔ 不要用「原版字段是白的所以我们也画白的」结案 —— 那会把可读性换掉，且与上面第 1、2 条并不自洽。

### 3.2 `Avatar Name` / `RadioChat` 的显示条件

`T13` 记为「静态 False + 反编译零赋值点」。本轮**没有新增判据**（与 `ChatButton` 那条同族，`ChatButton` 有实况 True
而找不到打开点）⇒ **判不出**，维持「原版恒不显示」的写法，**不改**。

### 3.3 `DeckManager.SetYourTurn` 的实况（回合高亮两枚同时 True）

实况 dump（arena1，`runtime_ui_dump_drive_0912.tsv`）里 `YourTurnImage` 与 `NotYourTurnImage` **两枚都 True**，
与 `SetYourTurn`（互斥）对不上。⛔ 仍不改我们的互斥实现（原版方法体明确是互斥；实况那一刻可能不在该状态上）。

---

## 4. 只能真机清单（⛔ 不是「以后再说」，是「静态判据到此为止」）

| # | 件 | 要看什么 | 为什么静态判不了 |
|---|---|---|---|
| 1 | 选牌面板压暗 + 卡（§2·C） | 卡是否**全亮**（max≈255）、背景是否被压到 ≈1−0.8 | 队列先后是硬判据，但**像素**要实拍；`M1` 那次就是「队列看似对、像素不对」 |
| 2 | 结算面板压暗（§2·A） | α 0.753 下棋盘是否像原版那样透出 | 原版那张 `Background` 是内置 9 宫格，我们用纯色等价 —— **等价性靠肉眼确认** |
| 3 | 技能面板（§3·1） | 原版这一屏到底看不看得见字、什么底 | 静态自相矛盾 |
| 4 | `WfSlider` 手柄绘制尺寸（短边 `preserveAspect`） | 与设置窗 FPS 那根并排 | 原版 `preserveAspect` + 110×110 塞进非方框，静态只能算「短边」，**像不像要并排** |
| 5 | 「等待敌人」横幅（联网局） | 停多久 / 有没有一闪而过 | 关闭边沿依赖对手网络往返；`ShouldFadeMulliganCards()` 恒 false 那段是死支 |

---

## 5. 需补的断言（宿主 `Editor/BattleScene.cs` 不在白名单 ⇒ 只写不落）

| # | 断什么 | 灭自证（改回旧写法必红） | 建议落点 |
|---|---|---|---|
| 1 | 结算压暗 `color ≈ (0.0283,0.0283,0.0283,0.7529)` | 改回 `(0,0,0,0.97)` ⇒ 红；⛔ 别只断「不是纯黑」 | `EndPanel` 那段（`DimGraphicForTest` 已有读口，**全仓零使用**） |
| 2 | 选牌：`_shade` 队列 < 候选卡队列，且 `_shade` 队列 > 3005 | 卡退回 3000 ⇒ 红；压暗退回 3000 ⇒ 一条红（被战场几何盖） | 选牌面板那段（需给 `CardChoicePanel` 加一个只读口读 `_shade.RenderQueue` 与某张卡的 `Queue`） |
| 3 | 选牌压暗 `ShadeTintAlpha ≈ 0.8` | 改回 0.55 ⇒ 红 | 同上（与换牌 `D6⑤` 那条同形） |

---

## 6. 移交给 `BattleDriver.cs`（AU1 的件）—— 4 条，⛔ 我没改

1. 🔴 **`Energy Accumulation` 整组显隐**（`T13:120-121` 的「顺手发现 ②」仍在）：
   原版 `manaAccumulation <= 0` 时**整组 `SetActive(false)`**（`ManaTypeHolder__SetAccumulationMana.c:16,20`）；
   我们只按它**换 ON/OFF 那张图**、没做整组开关 ⇒ 默认 `0` 时原版看不见、我们看得见。
   ⚠️ 归 `BattleDriver` 的 `:14323-14325` 那一带（行号是 2026-10-15 的，按符号找）。
2. 🔴 **牌库 / 手牌张数「按住牌才显示」没做**（`T13` §我们现状那一行）：原版 `CanDisplayDeckSize`
   + `CardScript__OnTouchDown` → `ShowDeckSize`，松手收起（`BattleManager__Update.c:140-150`）。我们常显。
   ⚠️ 那半个「松手」的判据（`FUN_180617800`）**没有反编译产物** ⇒ 只能按「按下显示 / 松开收起」实现，
   并在注释里如实标「一半是推断」（`T13` §没查清 2）。
3. ⚠️ **HUD 级压暗层 `FrontCanvas/ShadePanel`（`BattleManager.shade`）我们没有对应件**：
   本轮只把它用在「选牌 / 换牌」两处（见 §2·B/C），而原版还在 **`CemeteryManager` 的日志滑块**、
   `BattleManager` 的选牌/结算那几段调 `Shade.SwitchShade`。要不要补一个**共享**的 shade 件（而不是每扇面板各建一层）
   —— 属结构性改动，⛔ 不在本片白名单内，交由调度台裁。
4. `TickMultiCards`「先 `Hide` 再 `ShowMyDeck(true)`」（与注释相反，疑似独立缺陷）—— 仍在，未核。

## 7. 顺手发现（⛔ 我没改）

1. **`B2_FrontCanvas与弹窗_层参数表.md:67` 三条赋值点全指错节点**（§2·D·3）——
   这类「按方法名/行号推节点」的错最容易静默传播（本表正是别人做技能面板时的第一把尺子）。
2. **`RadioChat` 的显隐在 `B2:101` 写着有驱动**（`VoiceLinesController.ShowRadioMessage / CloseRadio`），
   而 `T13` 记「反编译零赋值点」—— **两份判据件打架**，建议以反编译现搜为准再统一（本轮没搜到新证据，未改）。
3. `EndPanel` 的两个读口 `DimPieceForTest` / `DimGraphicForTest` **全仓零使用** ⇒ 压暗层今天**没有任何断言**（§5·1）。

---

## 8. 改动清单（文件 / 行数）

| # | 文件 | 改了什么 | `git diff --numstat` |
|---|---|---|---|
| 1 | `Unity/MyGame/Assets/CardPresentation/Battle/EndPanel.cs` | 新增 `DimColor`（原版四元组）+ 建件处改用它 + 两段注释订正（「原图本地没有」不成立） | `32 / 3` |
| 2 | `Unity/MyGame/Assets/CardPresentation/Battle/CardChoicePanel.cs` | `ShadeAlpha = 0.8`（原 0.55）+ `QChoiceShade/QChoiceCard` + `_shade.SetRenderQueue` + `c.SetQueue` + 文件头/建件处注释订正 | `54 / 3` |
| 3 | `Unity/MyGame/Assets/CardPresentation/Battle/WfSlider.cs` | 文件头「我们挑的 #2」就地订正（竖直居中 = 原版运行时行为） | `12 / 2` |
| 4 | `Unity/资料/普查产出_1015战斗/B2_FrontCanvas与弹窗_层参数表.md` | `AbilityContainer` 那一格的三条赋值点订正（+ arena1/arena2 差异 + 白底白字这条矛盾） | `1 / 1`（表格行内替换） |

✅ `TMPDIR=/tmp/wf_au2 bash 工具/typecheck.sh` → **运行时 0 / 编辑器 0**。
✅ 行尾未翻（`git diff --numstat` 数字小；四个文件原本都是 LF/CRLF 原样保留）。
⛔ 未跑 Unity、未动 git、未改任何正本 / 任务总表。
