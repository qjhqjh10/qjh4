# 戊2 · 批次 1（**A237 建原版 `Reward Window`** + **A302 `ItemDrawer` 三层文字吃裁切**）—— 2026-10-11

> 写手：戊2（批次 1）· 白名单 = `Shell/RewardWindow.cs`（**新建**）· `Shell/ItemDrawer.cs` · `Editor/RewardsScene.cs`。
> ⛔ 本件**没跑 Unity**（批处理全局串行、只有调度台能跑）；跑的是**秒级类型检查**（读数见 §七）。
> 🔴 本文件按调度台要的格式：① 结论 ② **A237 的「层 × 参数」表** ③ 证据 ④ 改动清单 ⑤ 没查清 ⑥ 顺手发现（**只报不改**）⑦ 跑过的检查。

---

## 一、结论（两件各一句）

| # | 件 | 一句话结论 |
|---|---|---|
| 1 | **A237** | 原版 **`Reward Window`**（pid 13701 · 类 `RewardWindow : GameWindow`）**整扇窗建起来了**（`Shell/RewardWindow.cs`，新建 526 行）：树 / 矩形 / 图 / 字号 / 三组可见性 / 点窗外关窗 / 点面板吸收 / 点 `Collect` 只发一次 / **开场揭示动画（`mask2D.padding` 950→0 · 线性 · 0.8s）** / **水平滚动**（内容比视口宽时才滚） 全部照原版；**两处 3D 粒子 + 逐件 punch 没做**（判据齐、已出声，见 §五）。**入口查到了**：`RewardService.Preview` / `RewardService.Collect` → `WindowsManager.OpenWindow<RewardWindow>(RewardWindowContext)`；⚠️ **本工程还没有任何调用点**（我们没有那套服务端流程）⇒ 本类只提供 `Create` + `TryOpen(ctx)`，**接线是另一件活**。 |
| 2 | **A302** | `ItemDrawer` 的三层文字（阵营名 / 短名 / 数量）**现在跟着裁**了 —— 新增**唯一一份** `ClippedText(...)`（= `MenuDraw.Visible` → `MenuDraw.Text` → **对齐** → `MenuDraw.ClipText`，四步顺序与同族 `GameWindow.Text` **逐字相同**），三个调用点全改走它；`Quantity` 那处**顺带把「先裁再挪」的次序倒过来**（原来 `Text` 建完再 `AlignRight` ⇒ 正确的裁法会把块挪出框）。 |

---

## 二、A237 的「层 × 参数」表（判据 = 铁律 10 第 2 条）

> **口径**：矩形 = **画布像素**（1920×1080 · 左上原点 · y 向下）；**出处一律给原版来源**。
> 逐行按 **PathID** 走树得到的（不是按名字 —— 解包目录里 `Content` 有 **436** 份、`Button Text` **447** 份、`Tap To Continue` **4** 份 ⇒ **按名字取一定会取错一份**）。

| 层 | 名字 | 矩形（画布 px） | 组件 / 字段 | 出现条件 | 出处 |
|---|---|---|---|---|---|
| 0 | `Reward Window` | 0,0 → 1920,1080（锚 0,0–1,1 · sizeDelta 0,0） | `RewardWindow` + `UIAnchorAvoidSafeArea`；**`type=1` · `windowsPlacement=15` · `closeOnESC=1` · `updateNavPanel=0` · `extraScaleSmallScreen=1.0` · `animTime=0.8`** · `openSound`/`closeSound` = null + `useDefaultCloseSoundIfNull=1` | 运行期开 | `MonoBehaviour/MonoBehaviour_-1684643839753787023.json` |
| 1 | `Menu Dark Background` | −1327.30,−746.18 → 3247.30,1826.18 | `Image`（**无 sprite** · Type 0 Simple · `(0,0,0,0.772549)` · `m_RaycastTarget=1`）+ `BackgroundCloseButton`（= `closeButton` 0x78）+ `BackgroundOverDrawController` | 恒在 | 同上 + 树走 |
| 1 | `Content` | 0,165 → 1920,965（锚 0,0.5–1,0.5 · sz 0,800 · pos 0,−25） | 容器 | 恒在 | 同上 |
| 2 | `Reward Background Get Reward` | = `Content` | `Image` `40k_general_popup_simple red`（九宫 0,26,0,26 · Sliced · raycast 1） | `!IsPremiumLocked` | 同上 |
| 2 | `Reward Background Preview Reward` | = `Content` | 同上但 `… greyscale` | `IsPremiumLocked`（**出厂 INACT**） | 同上 |
| 2 | `Scroll View` | 0,300 → 1920,932.5（锚 0,0–1,1 · sz 0,−167.469 · pos 0,−51.265 · pivot 0,0.5） | `Image`（`Background` · **a=0 ⇒ 不画**）+ `ScrollRect` **h=1 v=0 mode=1(Elastic) inertia=1 elasticity 0.1 decel 0.135** | 恒在 | 同上 |
| 3 | `Viewport` | = `Scroll View` | `Image`（`UIMask` · **a=0**）+ **`RectMask2D` `soft=(200,0)` `pad=(0,0,0,0)` `en=1`** | 恒在 | 同上（波 C1 报告 §1·D 同值） |
| 4 | `Content`（= **`listHolder` 0x70**） | 拉伸锚 + CSF ⇒ 宽 = **max(120, 首选宽)** · **水平居中** · y 300→932.5 | `HorizontalLayoutGroup` **spacing 40 · align 4(MiddleCenter) · pad L60 R60 T30 B50 · ctrlW 0 / ctrlH 1** + `ContentSizeFitter` **h=1(MinSize=120)** | 恒在 | 同上 |
| 5 | 物品抽屉 ×n | HLG 摆：x = 内容左 + 60 + i×(宽+40)；y 居中于（330 … 882.5） | **`ItemDrawer.Draw(listHolder, item, quantity, DrawerOverride.Default)`**；另有两跳：`tier==10` 传 premium、`IsEphemeral`/`convertedInto` 各一跳（虚表 0x1b8/0x1d8/0x1e8） | 每条奖励一个（**按 `RewardTier` 排序**，见 §五·4） | `RewardWindow__Open.c` |
| 2 | `Title` | 660,193.30 → 1260,268.30（锚 .5,1 · pivot .5,1 · pos y −28.3） | 容器 | 恒在 | 树走 |
| 3 | `Glow Get reward`（= `getRewardText` 0xA0） | 660,187.38 → 1260,262.38（**比 Title 高 5.92**） | `Image` `40k_bt_underbutton`（Sliced 九宫 240,0,240,0 · **PA=1**）· tint **(0.8018868, 0.0794322, 0.0794322)** | `!IsPremiumLocked` | 同上 |
| 4 | `Text Get Reward` | = Glow | TMP **`Rewards claimed`** · 50 · auto[25~50] · **Center/Middle** · wrap=1 · 白 | 随上一层 | 同上 |
| 3 | `Glow Preview reward`（0xB0） | 同上 | 同图 · tint **(0.5283019, 0.3563546, 0.3563546)** | `IsPremiumLocked`（**出厂 INACT**） | 同上 |
| 4 | `Text Preview Reward` | = Glow | TMP **`You will get`** · 50 · auto[25~50] | 随上一层 | 同上 |
| 2 | `Collect Button`（0x80） | 837.5,902.5 → 1082.5,977.5（锚 .5,0 · sz 245×75 · pos y **+25**） | `Image` `UI_Button_Mulligan`（**Type 0 Simple**）+ `EverguildButton`（**trans=2 SpriteSwap · HL `…_hover` · P `…_Pressed` · interactable=1**）+ `EverguildButtonMaterialModifier` | **`OnCollect != null`** | 同上 |
| 3 | `Button Text` | 849.19,911.18 → 1070.00,968.70（ARF **宽控高 3.83864** ⇒ 220.81×57.52） | TMP **`Collect`** · 40 · auto[10~40] · **Center/Capline** · wrap=0 | 随上一层 | 同上 |
| 2 | `Premium Disclaimer`（= **`premiumWarning` 0x88**） | 右沿 **1708.8** · y 885→965（锚 .89,.05 · pivot 1,.5 · sz 0,80 + **CSF h:PreferredSize**） | TMP **`Upgrade to premium to unlock`** · 36 · **Right/Middle** · wrap=0 · **autosize=0** | **`IsPreview && Any(tier==10)`** | 同上 |
| 3 | `Premium Icon` | 1630.33,885 → 1708.88,963.55 | `Image` `40k_campaign_Premium-icon`（Type 0 Simple） | 随上一层 | 同上 |
| 2 | `Tap To Continue`（0x90） | 576,965 → 1344,1045（锚 .5,0 · sz 768×80 · pivot .5,1） | TMP **`Click to continue`** · 55 · **Center/Bottom** · wrap=0 · **`m_RaycastTarget=0`** + `BlinkGraphic`（blinkSpeed / colorVariation） | **`!IsPremiumLocked`** | 同上 |
| 2 | `Reward Claim`（0xB8） | **纯 `Transform`**（无 RectTransform）→ `Wave left` / `Wave right` → `Wave Shine` / `Trails` | 3D 粒子（领取特效） | — | 树走 |
| 1 | `Menu Vignette` | 0,0 → 1920,1080 | `Image`（无 sprite · Sliced · **(0,0,0,0.5803922)** · raycast **0**） | 恒在 | 同上 |
| — | `RewardAppearParticle`（0xD8） | **不在窗口树上** | 运行期实例化的粒子模板 | — | 字段表 |
| 动画 | `mask2D.padding` | **(950,0,950,0) → (0,0,0,0)** · **线性** · **0.8s**（`animTime`）；宽屏 **aspect > 1.77778** 时初值 = `950 + clamp01((aspect−1.77778)/0.555555) × 300` | `DoRewardAnimation` + `DOTween.To(…, SetEase(…,1))` | **只在 `!IsPremiumLocked` 时播** | `RewardWindow__DoRewardAnimation.c` + `工具/read_literal.py`（4 个字面量） |
| 交互 | 关窗 / 领奖 | — | `Menu Dark Background` 上那颗 `BackgroundCloseButton` ⇒ `GameWindow.Close`（虚表 Slot 8）；`Collect Button` ⇒ `OnCollectClicked` = 发 `OnCollect(Rewards)` + **`interactable=false`**；`Close()` = 发 **`OnClose(Rewards)`** 再 `base.Close()` | — | 三个 `.c` 逐句 |

**三组状态**（铁律 5·c：「一个值 ≠ 全部情况」）：
① `IsPremiumLocked`（**ctor 默认 `true`**）切四个件（两态底图 + 两团底光）· ② `IsPreview` **只多管一件**（`Premium Disclaimer` 的显隐）· ③ `OnCollect != null` 管 `Collect Button` 整颗建不建。
⚠️ **出厂 `m_IsActive` 是第四个状态**：`Reward Background Get Reward` / `Glow Get reward` 出厂 ACT、另两个 INACT —— 但 `Open()` **一定会重设这四个** ⇒ 出厂值只在「没人调 `Open`」时有意义。

---

## 三、证据（文件:行号 / 资产路径 + 字段名）

**原版侧（全部亲读）**
- 树 / 矩形 / 图 / 字号 / 对齐：`python 工具/menu_dump.py bundle_menus_assets_all "Reward Window" --depth 8`
- 根组件 19 个字段：`d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-1684643839753787023.json`
- **字段 → 节点**（按 PathID 解，逐个核过）：`listHolder`0x70=`Content`（Viewport 下）· `closeButton`0x78=`Menu Dark Background` · `collectButton`0x80=`Collect Button` · `premiumWarning`0x88=`Premium Disclaimer` · `tapToContinueText`0x90=`Tap To Continue` · `getRewardBackground`0x98=`Reward Background Get Reward` · `getRewardText`0xA0=`Glow Get reward` · `previewRewardBackground`0xA8=`Reward Background Preview Reward` · `previewRewardText`0xB0=`Glow Preview reward` · `claimRewardParticles`0xB8=`Reward Claim` · `mask2D`0xC0=`Viewport` · `scrollRect`0xD0=`Scroll View` · `particleOnAppear`0xD8=`RewardAppearParticle`
- 行为：`d:/2/tools/decomp_full/RewardWindow__{Open,Close,OnCollectClicked,ConfigureIsPreviewState,FixLayoutSize,DoRewardAnimation,DelayedParticlePlay}.c` · `RewardWindowContext__*.c` · `RewardService__{Collect,Preview}.c` · 类型/字段 `d:/2/tools/il2cpp_out/dump.cs:94553-94686`（`RewardInfo` 在 `:94087`，`rewardTier` 在 **0x30**）
- 动画字面量（`工具/read_literal.py d:/2/unity_run_ref/GameAssembly.dll <地址>`，单位 4 字节 float）：`0x1834b32e0 = 950` · `0x1834b32e8 = 950` · `0x1834b32ec = 0` · `0x1834b32d0 = 1.77778` · `0x1834b32cc = 0.555555` · `0x1834b326c = 300` · `0x1834b2bb8 = 1`
- 入口：`RewardService__Preview.c`（`new RewardWindowContext(...isPreview:1...)` → `WindowsManager.OpenWindow<RewardWindow>`）· `RewardService__Collect.c`（同一条出口，领奖流程的**公共出口**）

**我们侧（改完的落点）**
- `Shell/RewardWindow.cs`（新建，**526 行**）：常量区 `:76-190`（矩形 / 队列 / 图 / 文案 / 动画字面量）· `Tick/Update` `:263-282` · `Build()` `:284-403` · `BuildItems()` `:405-430` · `ConfigureIsPreviewState` `:461`
- `Shell/ItemDrawer.cs`：`ClippedText` `:903-912` · 三个调用点 `:802`（阵营名）· `:849`（短名）· `:886`（数量）· `ItemDrawerStyle.ClipSoftness` 的注释就地订正 `:182-185`
- `Editor/RewardsScene.cs`：A237 段 `:3871-4172`（含 A302 的探针/真窗两半）；新增两个小工具 `TmpVertsAndAlpha` `:367` · `QuantityLabelOf` `:394`

---

## 四、改动清单（白名单内，三个 `.cs`，零越界）

| 文件 | 位置 | 改了什么 |
|---|---|---|
| **`Shell/RewardWindow.cs`** | 全文件（**新建**） | 原版 `Reward Window` 整扇窗：`RewardWindowContext`（9 字段）+ `RewardWindow : GameWindow`（`Create` / `Build` / `BuildItems` / `Tick` / `Update` / `ConfigureIsPreviewState` / `Close` 覆写 / `Dump`） |
| `Shell/ItemDrawer.cs` | `:886-910` | **新增** `ClippedText(...)`（`Visible` → `Text` → `align` → `ClipText`，唯一一份） |
| | `:802` / `:849` / `:886` | 三层文字改走 `ClippedText`；`Quantity` 的「先裁再挪」倒成「先对齐再裁」（`align: 2`） |
| | `:182-185` | 就地订正 `ClipSoftness` 那段「文字仍不吃裁切」的旧话（铁律 5，留更正痕迹） |
| `Editor/RewardsScene.cs` | `:356-405` | 新增 `TmpVertsAndAlpha`（顶点 + 顶点色 alpha，**只认 `isVisible` 的字** = 与裁切同一个集合）与 `QuantityLabelOf` |
| | `:3871-4172` | **A237 + A302 的断言段**：**约 78 个调用点**（`Check` 15 · `CheckTrue` 30 · `CheckNear` 21 · `CheckAt` 8 · `CheckArt` 4）+ `ClickButtonByQuad` ×2（各带 2 条前提）+ `CheckAbsorbRule`（内含 8 条）+ 两张截图 `07_奖励窗_Get.png` / `07b_奖励窗_Preview.png`。**旧断言一条没删**。 |

**行尾**：三个文件现读**全是纯 LF**（`RewardWindow.cs` 526 行 / 0 个 CRLF；`ItemDrawer.cs` 与 `RewardsScene.cs` 改完 `git diff --numstat` 只动我碰的那几段）。

**每条断言「改坏哪里它会红」的一句话**（抽样，全表在源码注释里）：
- `ClickButtonByQuad`/`Check(collectCalls,1,…)` —— 删 `OnCollectClicked` 里那两句 ⇒ 红；去掉 `_collectArmed` 那道门 ⇒ 第 2 下也发 ⇒ 红
- `CheckNear(rw.MaskPad.x, rwPad0.x*0.5f, …)` —— 把 `Tick` 里 `dt/AnimTime` 写成常量步进 ⇒ 非线性 ⇒ 红
- `CheckTrue(!RectOfUnion(rw.ListHolder, …))`（t=0） —— 把 `BuildItems` 里 `ClipPad = MaskPad` 删掉 ⇒ 物品在「遮罩收拢」时照样画 ⇒ 红
- `Check(rw.Scroll.ClampLo, rw.Scroll.ClampHi * -1f, …)` —— 把 `CenteredContent` 改成左对齐 ⇒ 得 `[0, +1040]` ⇒ 红
- `Check(qLabels, 10, …)`（12 格里 2 格整块在外） —— 删 `ClippedText` 里那句 `MenuDraw.Visible` ⇒ 建满 12 个 ⇒ 红
- `Check(nBad, 0, …)`（文字顶点 alpha 逐点对原版剖面） —— 不给 `ClipTmpMesh` 传 `softPx`（或 `ItemDrawerStyle.ClipSoftness` 不透传）⇒ 全 1 ⇒ 红
- 探针 `pmx ≥ 1919.5` —— 删掉 `ClipText` 那一句 ⇒ 顶点跑到 **1970**（`box.x2 − 10`）⇒ 两条一起红

---

## 五、没查清的部分（⛔ 不猜）

1. **`scrollRect.normalizedPosition` 的初值读不出来**：反编译里那个 Vector2 是**静态字段**（`DAT_1842d9aa8 + 0xb8`），不是字面量 ⇒ 「休息态到底滚到哪」**没有确证**。我们的模型 = **偏移 0 = 内容居中**（因为 `Viewport/Content` 是**拉伸锚 + pivot .5** ⇒ 内容天然居中；`MenuScroll` 的 `ContentX1/X2` 就按居中给）—— 这一条是**几何推断**，不是读出来的。
2. **动画的 `endValue` 同样是个静态字段**（同一个 `DAT_1842daba0`），我们按 **`(0,0,0,0)`** 做：理由是 `Open()` 里另有一句 `set_padding(零点)`，且「全开」是唯一合理的终态。**没有字面量旁证。**
3. **逐件 punch 的三个字面量没读**（`DAT_1834b2dc4` 那一组，punch 幅度 / elasticity）⇒ **`DOPunchScale` 那一段没实现**（`DoRewardAnimation` 里对每个抽屉 0.4s 弹一下、延迟 ∝ **到 `m_Viewport`** 的归一化距离）。判据是有的（`ShortcutExtensions.DOPunchScale(…, 0.4f, vibrato 5, …)` 里的 5 是立即数），**要补是一次独立的小活**。
   🔴 **2026-10-11（批次1 · F1）就地订正（铁律 5）**：这半句原写「延迟 ∝ 到**内容左沿**的归一化距离」—— **参照物记错了**。判据两条：① `RewardWindow__DoRewardAnimation.c:123` 实读的是 `scrollRect`（`param_1 + 0xd0`）**`+ 0x40`**；② `d:/2/tools/il2cpp_out/dump.cs` 的 `ScrollRect` 字段表：`m_Content` = **0x20**（`:926194`）· `m_Viewport` = **0x40**（`:926210`）⇒ 那个参照 = **`m_Viewport`（视口）**，**不是内容**。（⚠️ 换算到我们这边 = 视口半宽 960，见 `Shell/RewardWindow.cs` 的 `PunchRefPx`。）
4. **「每条奖励先 `OrderBy(r => r.RewardTier)`」是推断**：`<>c__<Open>b__20_0` 的签名是 `RewardInfo → RewardTier`（键选择器）、`b__20_1` 是 `RewardInfo → bool` 且用在 `Any` 上（`dump.cs` 的签名行）⇒ 中间那份列表**仍是 `List<RewardInfo>`**；但 `FUN_180c99af0` 是**泛型 LINQ 助手、符号丢了** ⇒ 分不出是 `OrderBy` 还是别的（`Select` 被上面两条签名排除）。本类**按 `OrderBy` 实现**（排错了只会影响多条奖励时的**顺序**，不影响画法）。
5. **抽屉内部那三跳没做**：`premium` 高亮（`ItemDrawerOptions.showName` 那一跳 + 虚表 0x1b8）· `IsEphemeral`（0x1e8）· `convertedInto`（0x1d8）· 「非堆叠 ⇒ 画 `quantity−1` 份、每份 1 个」—— 这些是 **`ItemDrawer` 层**的特性，我们那套 `ItemDrawerStyle` **没有对应字段** ⇒ 属**另一件**（不是本窗漏了）。
6. **两处 3D 粒子不建**（`Reward Claim`（挂在 `Content` 下）/ `RewardAppearParticle`（运行期模板））—— 同 `ForgeTab` 那三套粒子的口径（素材 / 插件不在本工程），已在 `Build()` 末尾**出声**记着。
7. **`Tap To Continue` 的 `BlinkGraphic`（blinkSpeed / colorVariation）没实现**：我们 `Label` 没有闪烁件，那两个字段的值也没读 ⇒ 现在是一段**静止**的提示文字。
8. **抽屉格的尺寸 `ItemW×ItemH = 200×300` 是我们挑的**（原版在 `ItemDrawer` 的抽屉 prefab 里；那批 prefab dump 得出来但**照它改版式是待做的活**，见 `Shell/ItemDrawer.cs` 文件头 ②）⇒ 自检里「内容宽 / 滚动范围」那两条期望值**建立在它上面**（已在注释里标明是 fixture 算式）。

---

## 六、顺手发现（⛔ 一个都没改）

1. 🔴 **`Shell/CampaignRewardWindow.cs:621` 那句「仍不吃裁切的：抽屉里那三层文字」现在过期了**（A302 已做完）—— 该文件**不在我的白名单** ⇒ **没动**，请调度台订正那一句（一行级）。
2. 🔴 **`Shell/ItemDrawer.cs` 的另外几个调用方（商店格 / 战役节点首奖励 / 礼包弹窗）本来就没有 `Clip`** ⇒ 它们的文字「压在容器外」这一档**仍然存在**（各自宿主自己的问题，不是 A302 的回归）。其中 `Shell/CampaignTab.cs:389` 给抽屉的 `NamePx = 0 / QuantityPx = 0` ⇒ 那 47 个节点**只有一张图**，A302 对它们**零影响**。
3. ℹ️ **原版 `Tap To Continue` 那一段的出厂数据自相矛盾**：`m_fontSize = 55` · `m_fontSizeMin = 55` · **`m_fontSizeMax = 11`**（倒的，max < min）+ `m_enableAutoSizing = 1`。我们**按 55 画、不开自适应**（照数据不照我们的猜测）。若将来真 Play 发现它被 TMP 缩成一团，这条就是线索。
4. ℹ️ **`Reward Window` 的 `Content` 里那个 `Reward Claim` 是纯 `Transform`（3D）** —— `menu_dump` 表尾那句「另有 1 个纯 `Transform` 子件」就是它；本工程别的地方（`ForgeTab` 的 `Particle System nebula`）有同样的形状，口径一致。
5. ℹ️ **`A182` 那份报告 §一·D 的四行小表已全部被本件落实**（`Content` / `Scroll View` / `Viewport` 的 soft (200,0) / `Content` 的 HLG 参数）——`Viewport` 那一格当时写的 `soft=(200,0)` 与我们实读**逐字一致** ✓。

---

## 七、跑过的检查

- ✅ **秒级类型检查**：`TMPDIR=/tmp/wf_e2 bash d:/4/Unity/工具/typecheck.sh` —— 共跑 **5 次**（新窗落地后 / 断言段落地后 / 修 `.gameObject.activeInHierarchy` 后 / 修声明次序后 / 换 `CheckArt`·`CountByPrefix` 后）：**运行时 0 错 · 编辑器 0 错**（每次都 0，**没有别人半成品文件混进来的报错**）。
- ⛔ **没跑任何 Unity 自检**（红线）—— `RewardsScene.Run` 由调度台在同步点跑（本件覆盖的断言**全在那一条自检里**）。
- ⛔ **没动 git**；⛔ **没改两张正本**、没改任何 `资料/待办判据_*.md` / `资料/已知的坑.md`；⛔ 白名单外**一个文件都没碰**（`git status` 里 `Shell/{MainMenuRuntime,DeckInfoPopup,MenuWindowBase}.cs` 与 `Editor/{MainMenuScene,CollectionScene,ShellScene}.cs` 的改动**都是别人的**）。
- 📸 本件新增两张截图（在自检里）：`07_奖励窗_Get.png`（`IsPremiumLocked=false`）/ `07b_奖励窗_Preview.png`（`=true` + `IsPreview=true`）—— 并排比用。
