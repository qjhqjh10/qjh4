# B9_B1UI — 开场链（R1）与三张 offer 底图（R3）

> 执行代理 **B1UI** · 2026-10-27 · 工作目录 `d:/4` · 工程 `d:/4/Unity/MyGame/Assets`
> 白名单内改动：`Shell/**`（不含 `ChatPopupPanel.cs` / `UnitChatPanel.cs`）· `Resources/**` · 新建 `工具/import_*.py`。

---

## ① 结论

| 件 | 结论 | 一句话 |
|---|---|---|
| **R1 开场链** | **原账「✗ 未做」是【过期账】—— 链早就有了，而且有断言** | `Boot → 播 `intro` → 淡出 → 主菜单` 全在 `Shell/ShellRuntime.cs`（2026-09 阶段二就落地）；`Loading text` / `Progress text` / `FadeBackground` 四向 / `Intro Videoplayer` 节点齐全，`Editor/ShellScene.cs` 有 5 条断言。本轮只补了一处**无判据的字面量**（淡出时长 0.5→**1.0 s**，原版 `DOFade(…,1f,1f)`）。 |
| **R3 三张 offer 底图** | **资源已进工程**（本轮切好）+ **画法已接**（容器 `background`/`foreground` 真画图），**入口仍在服务端报价那一侧（P3）** | 图从 `Offers Atlas` 按 `SpriteDataMap` 现切（不手抄矩形）；`OfferContainer.Build` 现在按原版语义 `有图才 enabled`、`foreground` 保比例。顺带修掉一条**真缺陷**（见 ⑥-1）。 |

**自查**：`TMPDIR=/tmp/wf_B1UI bash 工具/typecheck.sh` = **运行时 0 / 编辑器 0**；
三个被账引用的 `.cs` **行数一字未变**（`OfferContainer.cs` 1487 · `BaseOfferPopup.cs` 1184 · `ShellRuntime.cs` 1380，`git show HEAD:` 对比过）。

---

## ② 证据

### R1 —— 开场链**已经实现**（逐条坐标）

| 环节 | 落点 | 判据（原版） |
|---|---|---|
| 建开场件 | `Shell/ShellRuntime.cs:1029 BuildIntro` | `level0/GameObject/Intro Videoplayer.json` 组件表 + `VideoPlayer_289.json` |
| 视频参数 | 同处 `:1050-1069` | `VideoPlayer_289.json`：`m_AspectRatio:3`(FitVertically) · `m_PlayOnAwake:1` · `m_WaitForFirstFrame:1` · `m_SkipOnDrop:1` · `m_Looping:0` · 音量 `IntroVolume=0.65`（`AudioSource_261`） |
| 任意键跳过 | `:1082 TickIntro` / `:1105 SkipIntro` | `GameBootController__Update.c`（`Input.anyKeyDown \|\| GetMouseButtonDown(0)`）· `__SkipToVideoToEnd.c` |
| 播完 | `:1113 FinishIntro` | `<Start>d__7__MoveNext.c`（等 `clip.length + 0.25` 后 `VideoFinished()`；我们走 `loopPointReached` + `time ≥ length − 0.02`，**画面等价**） |
| 淡出 | `:1132 FadeThenMenu`（**本轮改**） | `GameBootController__VideoFinished`：`DOTweenModuleUI.DOFade(loginScreenManager.canvasGroup, 1f, **1f**)` ⇒ **1.0 秒** |
| 进主菜单 | `:1146 LoadMainMenu` | `GameBootController__StartGame.c`：`EverguildSceneManager.ClearLoadedScenes()` + `SceneChanger.DoChange()`（我们换成关壳相机 + `LoadScene("MainMenu")`，理由写在原注释） |
| 首屏两块字 | `:792-793 TextBand(...)` | `level0/GameObject/Loading text.json` / `Progress text.json` + `RectTransform_280/279.json`（1920×48 · y=70 / y=21.8 · 后者 `m_IsActive=false`） |
| 四向压暗 | `:753-756 FadeEdge(...)` | `RectTransform_{282,283,286,277}.json` + `Gradient2` 实读（外端 1 / 中点 0.709804 / 内端 0） |
| 已有断言 | `Editor/ShellScene.cs:4728`（视频加载得到）· `:4731`（音量 0.65）· `:4736-4739`（音乐 key 复用的乘积）· `:4818-4820`（`Phase.Intro` → `SkipIntro()` → `Phase.Menu`）· `:1227-1244`（`Loading text` / `Progress text` 版式） | —— |

🔴 **为什么原账会判「未做」**（可复查）：账上的证据是 `grep "intro.webm" = 0`，
而代码用的是 **`Resources.Load<VideoClip>("Art/videos/intro")`（不带扩展名）**（`ShellRuntime.cs:1031`）
⇒ **那条 grep 命中 0 是必然的，不是「没实现」**。这是「按字符串形状找、而不是按加载 API 找」的假否定，
与 CLAUDE.md 里「翻过一个镜像目录就说本地没有」同族。

### R3 —— 三张底图（资源 + 画法）

| 事实 | 出处 |
|---|---|
| 三张图是 **`Offers Atlas` 图集**里的切片（316×739），`Sprite/*.json` 拿不到图集坐标 | `bundle_liveopsmenuimages_assets_all/SpriteAtlas/SpriteAtlas_6374560847574922512.json` 的 `m_PackedSpriteNamesToIndex` + `m_RenderDataMap[].textureRect`：tier1 `(569,0)` · tier3 `(889,0)` · tier2 `(1209,0)` · `40k_shop_popup_info_bg` `(0,0,565,588)` |
| 三张图**属于 `BackgroundImage`(=1)**，不是 `ForegroundImage`(=2) | 4 份 `Premium Item Offer` SO 的 `drawData.EventAssets[{key:1, GUID:"117e7654c91ec124db66c365be480065"}]`；`LiveOpsAssetRef` 枚举 `Icon=0 / BackgroundImage=1 / ForegroundImage=2`（`Shell/LiveOpsLocalCatalog.cs:119-133`） |
| GUID → 图名（离线唯一可解的路） | `bundle_liveopsmenuimages_assets_all/AssetBundle/AssetBundle_1.json` 容器表 ↔ `m_PackedSprites` 顺序：`117e7654…`→tier1 · `d8cb224a…`→tier2 · `64795275…`→tier3 |
| **消费点 = 商品条目容器**（不是 `BaseOfferPopup`） | `PurchaseableOfferContainer__OnInitialize.c`：`foreground(+0x58).sprite = LoadAsset(offer,2)`、`background(+0x60).sprite = LoadAsset(offer,1)`，然后 **`set_enabled(op_Implicit(sprite))`** ⇒ **没图连白块都不画** |
| `BaseOfferPopup` 只吃 `ForegroundImage`(=2) | ILSpy `BaseOfferPopup.cs:260-270`（`foregroundImage.sprite = LoadAsset<Sprite>(liveOps, EventAssetReferenceType.ForegroundImage)`） |
| 容器两格的 prefab 出厂 | `menu_dump.py bundle_menus_assets_all "General Basic Offer Container Variant Premium_Resource"`：`background` 339×778 `<无图>` **`m_Enabled=0`** · 子件 `foreground` 339×778 `<无图>` `preserveAspect` **`m_Enabled=0`** ⇒ 与上一条「有图才 enabled」自洽 |
| 三张图**视觉确实不同档** | 本轮导入后实测均值 RGB：tier1 `(34.7,53.6,52.4)` 青 · tier2 `(44.6,59.0,0.7)` 绿 · tier3 `(70.3,48.2,0.5)` 暖橙 |

---

## ③ 没查清的部分（如实记，铁律 2）

1. **tier2 / tier3 在离线数据里 0 个引用者** —— 全 `assets_full` 只有图集与容器表提到它们的 GUID。
   ⇒ 只能判「**服务端报价那一侧用**」（4 份离线 SO 全指 tier1）。**没查到**任何离线 prefab/SO 直接引 tier2/tier3。
2. **「报价列表」这一层不在离线数据里** —— 所以 `OfferContainer.WithEventArt(...)` 目前**没有产品调用点**
   （唯一调用点是 `Editor/ShopScene.cs` 的脚手架，且那条路今天用 `Content.Def()`）。
   ⇒ 我**没有编入口**（与 `OfferContainer.Build` 原有口径一致：原版由 `ContainerService.OpenOfferContainer` 在服务端报价到达时实例化）。
3. **`Loading text` / `Progress text` 的文案** —— 原版是本地化词条 `EverguildLocalization`（远程语言表），
   本机查不到就**留空**（原注释已写死这条）；`Progress text` 在场景里本来就是 `m_IsActive=false`。
   **谁在运行时给它们灌值**：没查到（推测在资源下载进度那一侧 = P3）。
4. **`Intro UI` 这层容器**我们没有单独建 —— 两块字和压暗四条直接挂在壳根；
   原版是 `Intro UI`(Canvas) → `Safe area`(CanvasGroup) → 两块字。**PC 全屏无差异**（安全区 = 整屏），
   ⚠️ 有刘海/挖孔时两块字的位置会与原版差一个安全区偏移。**没跑 Unity，没实测**。
5. 原版 `BlackBoard.skipIntro / skipTutorial`（`GameBootController.<Start>d__7` 的两颗静态开关）我们没有对应物
   —— 那是**调试用**的跳过开关，玩家路径不受影响。

---

## ④ 改动清单

| 文件 | 改动 | 行数 |
|---|---|---|
| 🆕 `Unity/工具/import_offer_backgrounds.py` | 从 `Offers Atlas` 按 `RenderDataMap` 切 4 张 + 拷 `40k_OfferBadge`；**默认只打印用法**，必须 `--apply` 才写盘；重跑按字节幂等 | 新文件 |
| 🆕 `Assets/CardPresentation/Shell/OfferArt.cs` | `OfferContainer` 的 `partial`：`Content.BgArt/FgArt` 两格 + `ArtOf()` / `ArtForAsset()` / `WithEventArt()` / `BuildForeground()` | 新文件（115 行） |
| `Assets/CardPresentation/Shell/OfferContainer.cs` | ① `static class`→`static partial class` ② `struct Content`→`partial struct Content` ③ `background`/`foreground` 改成**真画图 + 有图才 enabled + foreground 保比例** | **1487 → 1487**（只改既有行） |
| `Assets/CardPresentation/Shell/BaseOfferPopup.cs` | ① `Artwork/foreground` 改走 `OfferContainer.BuildForeground(...)`（原版 `SetEventData` 那一跳）② **修 `Offer Badge` 取错图**（见 ⑥-1）③ 三处「两张图工程里没有」的**过期注释就地订正** | **1184 → 1184** |
| `Assets/CardPresentation/Shell/ShellRuntime.cs` | ① `FadeThenMenu` 的淡出 **0.5 → 1.0 s**（原版 `DOFade(…,1f,1f)`）② 「找不到开场动画」那条警告里写死的 **`intro.mp4` → `intro.webm`**（工程里存的就是 webm） | **1380 → 1380** |
| 🆕 `Resources/Art/ui_menu/WF_Special_offer_Bck_tier1.png` | 316×739（青） | 新（`Resources/Art/` 在 `.gitignore` ⇒ 换机要重跑导入器） |
| 🆕 `…/WF_Special_offer_Bck_tier2.png` | 316×739（绿） | 新 |
| 🆕 `…/WF_Special_offer_Bck_tier3.png` | 316×739（暖橙） | 新 |
| ⚠️ `…/40k_shop_popup_info_bg.png` | **原已存在**（10-24 导入），本轮按 `RenderDataMap` 重切 ⇒ **字节与旧的不同**（尺寸/矩形与图集一致 565×588）；`Resources/Art/` 不在 git，回滚只能重导 | 覆盖 |
| ✅ `…/40k_OfferBadge.png` | 原已存在（内容相同，本轮 `same` 跳过） | 无变化 |

`OfferArt.cs` 为什么单开一个文件：`OfferContainer.cs` / `BaseOfferPopup.cs` 被 `账/*.tsv` 按 `文件:行号` 引用
（39 / 34 条 A 行）⇒ 插一行就整体漂移（CLAUDE.md 铁律 18·b）。用 `partial` 把新成员放进新文件，原文件只改既有行。

---

## ⑤ 需要调度台执行的 Unity 命令（我不能跑 Unity）

**前提**：3 张新 PNG + 1 个新 `.cs` **都还没有 `.meta`** ⇒ 必须让 Unity 先导入一次（下面第一条跑起来就顺带导入了）。

```bash
# ① 导入 + 壳自检（动过 ShellRuntime）
unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod ShellScene.Run -logFile -
# ② 动过 OfferContainer（19 条变体 + 抽屉）
unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod ShopScene.Run -logFile -
# ③ 动过 BaseOfferPopup
unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod MainMenuScene.Run -logFile -
```

⚠️ **这三条都会 `SaveScene` 覆盖各自场景**（CLAUDE.md §二 那条）⇒ 跑前留副本、跑完 `git diff --numstat` 盯一眼。
**该跑的就是这三条**（铁律 12：只动一个宿主跑一条；本批动了 3 个宿主）。

**建议调度台加的三条断言**（我不能改 `Editor/**`）：
1. **灭自证型**：`BaseOfferPopup.Build()` 之后 `Artwork/foreground` 在 `C.FgArt` 为空时**没有**子 quad
   —— 同时再造一条 `C.FgArt = "WF_Special_offer_Bck_tier1"` 的，**必须**有子 quad 且 `keepAspect` 生效
   （改坏成「总是画」或「永远不画」都会红一条）。
2. `OfferContainer.Build(..., c)` 传 `BgArt = "WF_Special_offer_Bck_tier1"` 时，
   `background` 下多出 `background Gfx`；不传时**一条都不多**（今天 19 条变体的 `Content.Def()` 全走这条）。
3. `ArtOf("__不存在__")` 返回 `null` **且**日志里出现一次 `[OfferArt]` 警告（验「不许静默失败」）。

---

## ⑥ 顺手发现

1. 🔴 **`BaseOfferPopup.BuildBadge()` 一直取的是错图**（本轮已修，判据硬）：
   原代码 `var t = Art(ArtOfferBadge);` 用的是 **`40k_OfferBadge`（256×256，那是打折角标）**，
   而 `Offer Badge` 在原版是 **`WF_Special offer_Value`（324×87 · 九宫 162,0,162,0 · tint (0.651,0,0,1)）**
   —— `menu_dump.py bundle_menus_assets_all "Base Offer Popup" --depth 5` 那一行逐字如此，
   而同文件里的常量 `BadgeBorder=(162,0,162,0)` / `BadgeTexW/H=324/87` / `BadgeTint=(0.651,0,0,1)` **本来就是 Value 的**。
   ⚠️ **为什么以前没现形**：`40k_OfferBadge.png` 是 10-24 才进 `Resources/` 的（在那之前 `Art()` 返回 null ⇒ 只建节点不画）；
   一旦图在了，这条就把**打折角标**（256²）按 162 的九宫边界画到 406×78.85 的框里 ⇒ 画面错、且九宫边界越界。
   改成 `Art(OfferContainer.BadgeArt)`（= `WF_Special_offer_Value`），**未新增常量、行数不变**。
2. **账上「两张图工程里没有」已经过期**（`BaseOfferPopup` 文件头 ② + 两处 `const` 注释 + 三处日志文案，本轮都就地订正）：
   `40k_shop_popup_info_bg.png` 与 `40k_OfferBadge.png` **都在 `Resources/Art/ui_menu/`**（10-24 导入）。
   ⚠️ **但 `Resources/Art/` 在 `.gitignore` 里** ⇒ 「换台机器就没有」这件事是真的，所以**导入器必须留着**
   （这正是我新写 `import_offer_backgrounds.py` 的第二个理由）。
3. **R1 那条账的判据错在 grep 的写法**（`"intro.webm"` vs `Resources.Load<VideoClip>("Art/videos/intro")`），
   建议调度台改账时把**判据换成「加载 API 有没有被调用」**而不是「文件名字符串有没有出现」——同族坑见 ⑥-4。
4. **「还缺 4 个视频」其实一个都不缺**（都在本地，只是没用上）：
   `bundle_videos_assets_all/VideoClip/`：`Main Menu Multiplayer Video.mp4` · `Main Menu Practice Video.mp4` ·
   `Menu_Chronomancer_Animation.mp4`（另有 `Victory/Defeat/Draw Video.mp4`）；
   `bundle_menus_assets_all/VideoClip/Gacha Crate Opening.mp4`。
   ⇒ 这三处（多人/练习模式窗背景视频、抽卡开箱、Chronomancer 动画）是**新的可执行任务**，本轮没做（不在 R1/R3 范围）。
5. **`sactx-…-Offers Atlas-….png` 是 2048×1024 的 DXT5 图集，解包目录里的 PNG 已是解码后的 RGBA**
   ⇒ 切图**不需要** DXT 解码器，PIL 直接 crop 即可（本脚本就是这么做的，坐标换算 `top = H − y − h`）。
6. ⚠️ **并发提醒**：本轮工作期间 `ChatPopupPanel.cs` / `UnitChatPanel.cs` / `CardFeel.cs` / `CardView.cs` /
   `RuleEngine/**` / `账/*.tsv` / `工具/import_original_art.py` 都**有别人在改**（`git status` 现读），我一个都没碰。

---

# R2 — 主菜单 21 个「环境光效」：**账的前提不成立**（2026-10-27 · B1UI）

> 结论先说：**主菜单静止时一个环境光效都没在跑**，原版本来就那样。
> 21 个里 **19 个在默认关闭的子树里**（`Card Displayer Menu For Menu` = 原版类 `CardDisplayWindow`；
> `Borders`；以及 4 个 `Swirl` 其实挂在**收藏/图鉴页的通配符按钮** `Wildcard_Use_Button 1~4` 下面），
> **2 个（`Animated Loading Image` + `Cog`）我们早就实现了**（`Shell/LoadingMenu.cs`，同一份判据）。
> ⇒ 没有「主菜单是静帧」这回事；按账上那句话去给主菜单加一层粒子 = **凭空造**（铁律 3 / 18 红线）。

## ① 判据（机器生成，可复跑）

```bash
PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/gen_mainmenu_effects.py --apply
```
产物 = **`Assets/CardPresentation/Resources/Art/mainmenu_effects.json`**（270,779 B）：
逐节点给出 父链（到 `Game UI`，每层带 `m_IsActive`）· 组件类名（经 `m_Script` → MonoScript 的 `m_ClassName`）
· 矩形原始字段 · **粒子只有 enabled 的模块**（整块原样）· renderer（mode/order/pivot）· 材质名 + `_MainTex` pid + shader pid。
⚠️ 脚本**默认只打印用法**，必须 `--apply` 才写盘；只读 `d:/2/新解包资源/`。

## ② 21 条逐条（表就是上面那份 JSON 的摘要，可复算）

| 节点（`GameObject` 名） | 粒子 | 挂在哪（父链） | 父链里 **inactive** 的层 | 组件类 |
|---|---|---|---|---|
| `Swirl` · `Swirl_297` · `Swirl_304` · `Swirl_314` | ✅ | **`Wildcard_Use_Button 1 Common / 2 Rare / 3 Epic / 4 Legendary`** → `Circling Particle` | （链条在按钮处截断：按钮的父未导出） | ParticleSystem |
| `Glow Explosion Up / R / Down` | ✅ | `Card Displayer Menu For Menu` → `Glow Explosion` | **`Glow Explosion` + 窗本身** | ParticleSystem |
| `Ray Right` | ✅ | 同上 → `Lighting Rays` | 同上 | ParticleSystem |
| `Sparks R` · `Sparks RL` | ✅ | 同上 → `Glow Explosion` | 同上 | ParticleSystem |
| `Side Border Left / Right` | — | `Borders`（1920×1080） | **`Borders`** | `UnityEngine.UI.Image`（**sprite=null · 纯黑 · a=1**，933.9×2280.3 @ x=∓1426 ⇒ **整块在屏外**） |
| `EffectElement` ×5 | — | `Card Displayer Menu For Menu` | **窗本身** | **`CardEffectItem`**（字段 `enchanterText` / `effectText`）⇒ **卡面效果文字件，不是光效** |
| `Craft` | — | 同上 | 窗本身 | `Image` + `EverguildButton` + `EverguildButtonMaterialModifier` + `HorizontalLayoutGroup` ⇒ **按钮件** |
| `Level Up Effect Down` | — | 同上 → `Level Up Effect` | **`Level Up Effect` + 窗本身** | `Image` + **`UIBorderGlow`**(animTime 1 / animDelay 0 / playOnEnable 0) + **`AnimateUIMaterialProperty`**(`_Reveal` −0.5) |
| `Cog` | — | **`Menu Dark Background`** → `Animated Loading Image` | `Menu Dark Background` | `Image`(`40K_icon_searching_cog`) + **`SimpleRotation` speed (0,0,−20)** |
| `Animated Loading Image` | — | `Menu Dark Background` | `Menu Dark Background` | `Image`(`40K_icon_searching_skull`) + `CanvasGroup` **0.699999988** |

**判定**：
- **2 条已完成**：`Cog` + `Animated Loading Image` —— `Shell/LoadingMenu.cs` 就是这对节点（`ArtCog = "40K_icon_searching_cog"` ·
  `SpinDegPerSecond = -20f` · `SpinnerAlpha = 0.699999988f`，逐值与上面一致）。
- **4 条 `Swirl` = 收藏页通配符按钮的绕环粒子**（四个稀有度各一份，四份 `startColor` 不同：
  Epic 紫 `(0.889,0.718,0.934)` · Rare 绿 `(0.446,0.868,0.556)` · Common 蓝 `(0.425,0.606,0.698)` · Legendary 橙 `(0.962,0.466,0.286)`；
  材质 `AirSharpSwirl` · `startDelay 0.4` · `lengthInSec 1` · 一次性不循环）⇒ **归卡组编辑/收藏线**，不归主菜单。
- **其余 15 条全部在一个 `m_IsActive = false` 的 `CardDisplayWindow`（`Card Displayer Menu For Menu`）或 `Borders` 里**
  ⇒ 它们只在**那扇窗打开时**才播（`playOnAwake=1` 但父件关着 = 不跑）。**Borders 的激活条件没查到**（全离线数据里没有对它的写点）。

## ③ 为什么我没写「主菜单光效层」

- 主菜单静止时原版就是静的（判据 = 上表每一行的 inactive 祖先）。加一层就是**自造**。
- 那 15 条真正的归属是 **`CardDisplayWindow`**（主菜单里的那扇"看卡"窗）。我们那份实现是
  `CardPresentation/Battle/CardDisplayWindow.cs`（1910 行，**全文件 0 处 `ParticleSystem`**）——
  **它在我的白名单外**（本波只许 `Shell/**`），而且它是**战斗侧在用的同一份类**，改它要连带战斗。
  ⇒ 建议：**另派一件活**，作者需有 `Battle/CardDisplayWindow.cs` 的写权；判据直接读
  `Resources/Art/mainmenu_effects.json`（每颗粒子的 enabled 模块 + 材质名都在里面，不用再翻解包）。
- `Swirl ×4` 同理：**归收藏/卡组编辑线的通配符按钮**（我们的 `Wildcard` 抽屉在位，但按钮**没有**绕环粒子）。

## ④ 这份 JSON 怎么用（给下一件活留的口径）

- 粒子：`items[i].particle.modules` 里**只有 enabled 的模块**，键名与 Unity `ParticleSystemModule` 的字段名一致
  （`InitialModule.startLifetime` / `ShapeModule.angle` / `EmissionModule.rateOverTime` …）；
  `renderer.m_RenderMode` / `m_SortingOrder` / `m_Pivot` / `materials[].name` 一并给了。
- ⚠️ **材质/贴图不在我们工程里**：`AirSharpSwirl` / `Glow Rays UI` / `Lightning Random UI` / `Spark Additive Extra Color`
  这四张 `.mat` 只在**战场那条线**被烘进 `Assets/WarpforgeArena1/arenas/*/Materials/PS_*.mat`（不在 `Resources/` ⇒ 运行时取不到）。
  要接这 10 颗粒子，**先解决材质/贴图的落点**（那条属于特效线，不属于界面线）——这是**前置**，不是可选项。
- 建议断言（给将来那件活）：① 那 15 条的效果层**出厂必须关**（`CardDisplayer` 父件 inactive，照原版）——
  这就是「该藏的时候藏住了吗」那一条；② `Cog` 的 `-20°/s` 与 `SpinnerAlpha 0.699999988` 已有断言在 `LoadingMenu` 那边，别写第二份。

## ⑤ R2 的账该怎么改（建议，⛔ 我没动 `任务总表.md`）

`R2` 那一行（`任务总表.md:203`）建议改成：
> ~~「主菜单 21 个环境光效 · 主菜单不再是静帧」~~ ⇒ **2026-10-27 订正（B1UI）**：判据见 `报告/B9_B1UI_开场链与offer底图.md` §R2 + `Resources/Art/mainmenu_effects.json`。
> 21 条的真实归属 = **`CardDisplayWindow` 15** · **通配符按钮 4** · **加载转圈 2（已实现）**；**主菜单静止时本来就一个不跑**。
> 拆成两件新活：① 卡片展示窗的光效栈（要 `Battle/CardDisplayWindow.cs` 写权）② 通配符按钮的绕环粒子（收藏/卡组编辑线）。
> 另：`R2` 行原文指的「同上 §六 #2」在 `B9_W16R0_R0资源账重算.md` 里是 **`Intro Videoplayer`**，与光效无关 —— 指针也错了。

## ⑥ 本轮结束时 `typecheck` 的状态（**不是我的错，如实记**）

- 我自己那批改完时跑过 3 次：**运行时 0 / 编辑器 0**。
- 收尾那次（并发期间）变成 **运行时 3 / 编辑器 6**，**全部落在别人的文件里**：
  · `RuleEngine/Core/EffectResolver.cs:5503/9883/9891/10685` —— `CS0103 未定义 HandExpireOf / DrawnNow / DrawHandHopTakes`（引擎代理正在写）
  · `Editor/BattleScene.cs:22202-22220` —— `CS1061 BattleDriver 无 CanUseVOsForTest / ChatPopUpBlockedForTest / SetTransformedWarlordNewChats`（断言先落、被测成员还没加）
  ⇒ 按 CLAUDE.md §13·3，这类「错误全集中在你不负责的文件上」**不算我这条的问题**，我一个字都没碰那两个文件。
  ⚠️ **但它们会挡住同步点那次编译** —— 排 `ShellScene/ShopScene/MainMenuScene.Run` 之前请先确认这两处已收口。

---

# R2 续 · `CardDisplayWindow` 光效（2026-10-27 · B1UI）

## ① 做了什么

| 件 | 落点 | 说明 |
|---|---|---|
| 光效层建件器 | 🆕 `Shell/CardDisplayEffects.cs` | 读判据 JSON → 建 **10 颗粒子 + 3 条图件**；模块逐条灌（initial / shape / emission+bursts / sizeOverLifetime / rotationOverLifetime / colorOverLifetime / velocity / UV / renderer） |
| 运行时 DTO | 🆕 `Resources/Art/card_display_effects.json`（34,568 B） | 同一份判据的**扁平机器可读版**（`JsonUtility` 能吃）；由 `mainmenu_effects.json` 逐字段折算而来 |
| 材质搬运 | 🆕 `工具/import_mainmenu_effect_mats.py`（**默认只打印用法，`--apply` 才写盘**）→ `Resources/Art/vfx/*.mat`（4 个） | 见 ② |
| 接线 | `Battle/CardDisplayWindow.cs`（**+9/−1**：1 行原地改 + 8 行**追加在类尾**） | `Build()` 里 `_cards` 建完立刻 `_fx = CardDisplayEffects.Build(_cards, CardWinBox.CardsS);`；类尾加 `Transform _fx` + `EffectsRoot` 读口 |

**为什么这批能落地**：原版这 10 颗粒子挂在 `2DCard`（展示窗里那张大卡）下、整棵 `Card Displayer Menu For Menu`
默认 `m_IsActive = false` ⇒ 它们只在**展示窗打开时**播。我们把光效层挂在**卡体容器 `Cards`** 下，与原版同层级。

## ② 材质这条路（原来是「过不去的关卡」，现在解了）

- 原版材质在 bundle 里（`AirSharpSwirl` / `Glow Rays UI` / `Lightning Random UI` / `Spark Additive Extra Color`），
  它们的 shader 是**bundle 里的自定义 shader** → 落不了工程资产。
- **但特效线早就把这 4 张重建成了工程资产**：`Assets/WarpforgeVFX/Materials/*.mat` + `Assets/WarpforgeVFX/Shaders/*.shader`
  （`AirSharpSwirl.mat` 的 shader guid → `WFParticlesExtraColorEC.shader`，**同族复原件**）。
  ⇒ 本批**只做字节级拷贝**进 `Resources/Art/vfx/`（⛔ 没新建材质、⛔ 没拿第三方近似图顶）。
- ⚠️ **一张改名（有判据）**：工程里**没有** `Lightning Random UI.mat`，只有 `Lightning Random add.mat` / `blend.mat`。
  实测混合状态**逐值相同**（原版 `_Blend 2 / _SrcBlend 5 / _DstBlend 1` == `add` 那份），⇒ 用 `add` 顶上，拷进后按**原版名**命名
  （`Resources/Art/vfx/Lightning Random UI.mat`），运行时按原版名取，代码里**没有映射表**。

## ③ 坐标换算（唯一一处，🔶 已标注）

原版父件 `2DCard` 的 `m_SizeDelta = 2.0927 × 3.3313`（原版画布单位），本窗卡体容器是 `CardWinBox.CardsS` **px**：
`unitToPx = CardsS / 2.0927` · `pxToWorld = 1/108` · 粒子节点 `localScale = unitToPx/108`
⇒ 原版那些「单位」量（startSize / speed / shape.radius / 位置）由 Unity 的 `scalingMode = Hierarchy` 自动换算，
**不必逐字段乘**。代码里已按 CLAUDE.md 铁律 18 打标：
`// 🔶 自己设计（非原版）：unitToPx 这个换算系数 … 调度台 2026-10-27 裁定`。

## ④ 没做的两条（**不是忘了，是归属不同**）

- `EffectElement ×5` = 原版类 **`CardEffectItem`**（字段 `enchanterText` / `effectText`）= **卡面效果文字件**；
  硬塞进本窗会变成第三套文字排版（本窗已有 `EffectList`）。
- `Craft` = `EverguildButton` **按钮件**，不是光效。
两条都仍在**默认关闭**的同一棵子树里，不影响「打开展示窗看到什么」的主线；已在 `CardDisplayEffects.cs` 文件头写明。

## ⑤ 已知未验 / 残余风险（**别当成已经对了**）

1. **没跑过 Unity**（本代理不能跑）⇒ 下面这些只有静态依据，**必须真机看一眼**：
   位置/大小量级（`unitToPx` 这个桥是唯一自设计处）· 4 张材质的 HDR 强度（原版 `_Color 4.237`、`_EmissionColor 1.97/1.19/2.05` 已经烘在材质里）·
   `Sparks` 的 `renderMode = 1`（Stretch）在 2D 下是否需要 `velocityScale`（原版 `m_VelocityScale = 0`，我们照抄 0）。
2. **`UVModule.frameOverTime` 没接**（`Ray Right` 用 UV 动画）：DTO 里带了 `uvEnabled/tilesX/tilesY/animType/fps`，
   但 `frameOverTime` 这条曲线没灌 ⇒ 那一颗的 UV 播放会走 Unity 默认（匀速 0→1）。**记在这里，下一轮补。**
3. 3 条图件（`Side Border L/R` = 纯黑 + 在屏外 · `Level Up Effect Down` = `UIBorderGlow`）**建成即关**
   —— 照原版（父件 `Borders` / `Level Up Effect` 都是 `m_IsActive = false`）；`UIBorderGlow` 的脉动**没接**（那一件是"装备后才有"的图件）。
4. `EffectElement` / `Craft` 见 ④。

## ⑥ 该跑哪条自检 / Unity 命令

```bash
# 1) 先让 Unity 导入（新 .cs + 4 个 .mat + 2 个 .json 都还没有 .meta）
unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod ShellScene.Run -logFile -          # 9s
# 2) 主宿主（CardDisplayWindow 的 17 条断言在这里面）
unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod BattleScene.Run -logFile -        # 7m56s
# 3) 主菜单也实例化这扇窗（2 处引用）
unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod MainMenuScene.Run -logFile -      # 24s
```
**建议补的断言**（我不能改 `Editor/**`）：
① `EffectsRoot != null`（判据/材质在不在）——**先断前提**，别写成 `if (fx != null) Check(...)`；
② **灭自证**：3 条图件出厂 `activeSelf == false`（改坏成"默认开"就红）；同时 `particles` 那一层**有 10 个 `ParticleSystem` 子件**（改坏成"一颗不建"也红）；
③ 10 颗粒子每颗的 `renderer.sharedMaterial != null`（材质搬运是否到位）。

---

# 界面线余项 · 2026-10-27 第三批（B1UI）

## ① 上一批「已知未验」里能静态做的两条 —— 已补完

| 条 | 做法 | 判据 |
|---|---|---|
| **`UVModule.frameOverTime` 没接** | 生成器新增 `uvFrameOverTime`（`minMaxState` 曲线原样带出）＋ `CardDisplayEffects` 灌进 `textureSheetAnimation.frameOverTime`。现读：**只有 `Ray Right` 用 UV**（`mode 1`(Curve)、2 个键、`frames 0→0.9999`），其余 9 颗 `uvEnabled=false` | 原版 `ParticleSystem_633.json` 的 `UVModule.frameOverTime`；DTO 字段 `uvFrameOverTime` |
| **`UIBorderGlow` 的脉动没接** | `Level Up Effect Down` 那颗现在**真的挂上** `UIBorderGlow`，参数照原版灌（`animTime 1` · `animDelay 0` · `playOnEnable 0` · `revealFrom −0.5`（= 原版 `AnimateUIMaterialProperty.floatProperty1.value`）· `revealTo 1`） | 原版 `MonoBehaviour_2236.json`（`UIBorderGlow`）+ `_2204.json`（`AnimateUIMaterialProperty`） |

🔴 **但脉动这一条只到「参数接好」这一层 —— 画面还出不来，原因是缺 shader 不是缺代码**：
`UIBorderGlow` 靠材质的 **`_Reveal`** 属性做「从中间往两边揭开」，而**全仓 `.shader`/`.mat` 里 `_Reveal` 命中 = 0**
（`grep -rl "_Reveal" --include=*.shader --include=*.mat`）。⇒ 组件自己会打 `[UIBorderGlow] 材质上没有 _Reveal`（**不是静默失败**）；
等那个 shader 到位后这一件**不用再改代码**。
📌 DTO 为此加了一格 **`hasGlow`**（★ 不能靠 `glowTime` 猜 —— 缺组件时那个默认值也是 `1.0`，这是本项目「判不出就别猜」的同族坑）。

⛔ **按你的要求，量级 / HDR / Stretch 这三条我一个字都没调**，仍留在上一节「已知未验」里等真机。

## ② 界面线 §一 余项：本轮做了 1 条（判据最清、风险最小的一条）

**`Generic Popup Background` 补 `m_PixelsPerUnitMultiplier = 0.75`**（`Shell/SettingsWindow.cs`，**行数 6649 → 6649 中性**）
- 判据：`menu_dump` 现读原版 prefab；uGUI 算式 `GetAdjustedBorders(border / multipliedPixelsPerUnit, rect)`
  ⇒ 画出来的端帽 = `PopupBorder ÷ 0.75` = **225.33 / 213.33**（改前按 1.0 画 169 / 160 ⇒ 角块小一圈）。
- 落点：把 `Nine(...)` 换成**本窗专为 ppuMul 建的** `NineOut(..., border, borderOut, ...)`（那个口本来就是干这个的）。
- ⚠️ **这一档断言量不出「像不像」**，验收要并排比。

### 还开着、我**没动**的（附理由，供你派活）
| 条 | 为什么本轮没动 |
|---|---|
| **同窗 `Mask Tabs buttons` 的 `ppuMul 0.76`** | 我们那节点是 `ViewportClip.Hang` 建的**纯裁切节点（没有 Image）** ⇒ 要补它得先定「那层底图现在归谁画」，是**设计决定**不是补参数（会在既有裁切链上叠一层图，得并排比） |
| **`ShopData._shopItems` 是字典 ⇒ 丢顺序** | 落点 `Shell/ShopData.cs`，判据清楚（`FixedStoreData.AddItem` = `orders.Add` + `OfferData.ToRewards` 同 `TargetId` 分组），但要动**货架排序的数据结构**（跨 `ShopWindow`/`ShopTabPage` 消费者）⇒ 值得单独一件、单独跑 `ShopScene.Run` |
| **设置窗 3~4 颗底部钮点不到** | 判据只到「点位落了 `AbsorbHit`」，「是 `Hit` 没建还是队列被压」**没查清**（账上原话），要先只读诊断 |
| **U2·V1 头衔默认种子搬进 `ProfileData`** | 判据清楚、改动小，但会动存档初始化顺序（`ProfileData` 在双写期），建议单独一件并跑 `MainMenuScene.Run` |
| **费用曲线口径 / 卡池搜索少 3 类 / 复制卡组命名** | 落点全在 `Core/CostCurveDrawer.cs` 与 `Deck/DeckEditorState.cs` ⇒ **不在本波白名单** |

## ③ 验收
- `TMPDIR=/tmp/wf_B1UI bash 工具/typecheck.sh` = **运行时 0 / 编辑器 0**（本批共跑 5 次；中途两次撞上别人在写 `EffectResolver.cs` / `BattleDriver.cs` / `SkillPanel.cs`，等它们收口后即归零）。
- 行数：`SettingsWindow.cs` **6649 → 6649**；`CardDisplayWindow.cs` 仍是 **+9/−1**（1 行原地改 + 8 行类尾）。
- Unity 命令同上一节（`ShellScene.Run` → `BattleScene.Run` → `MainMenuScene.Run`）；**本批新增的判据/DTO 由那三条顺带导入**，不新增命令。
- 建议追加断言：`Level Up Effect Down` 那颗上**有** `UIBorderGlow` 且 `animTime == 1` / `revealFrom == -0.5`（改坏成"没挂"或"参数没灌"都会红）；
  `Ray Right` 那颗的 `textureSheetAnimation.frameOverTime.mode == Curve`。

---

# 设置窗点击（诊断）+ 头衔种子（已改）· 2026-10-27 第四批（B1UI）

## ① 设置窗 3~4 颗底部钮点不到 —— **诊断结论：不是「队列被压」，是「命中区没建/没登记」**

### 判据（**决定性证据 = 真机点击记录自己列出的候选表**）
`_tmp_view/click_log.txt:13414-13426`（点击 #119 / #120，就是账上那两次）：
```
命中 `AbsorbHit`/Window Anchors/2 - Canvas Holder Above upper bar/Main Menu Settings Window · 队列 3134
同时压在这点上的（按渲染队列↓ / z↑ 排，第一个才是真吃到的）：
    q=3134  `AbsorbHit`/…/Main Menu Settings Window
    q=3130  `BackgroundHit`/…/Main Menu Settings Window
```
**候选表里只有两条**（吸收层 + 压暗层），**那颗钮的 `Hit` 根本不在里面**。
⇒ 账上那句「`Hit` 没建 vs 队列被压」**到此定案 = 前者**：
- 队列假说**直接排除**：钮的 `Hit` 建在 `QOverlay`(3135)、吸收层是 `Absorb` 算出来的 `QOverlay − 1 = 3134`
  （`MenuDraw.Absorb:2507` `int q = qContentMin - 1`）⇒ 就算建了，**钮也赢**。
- 坐标假说**也排除**：同一条日志给了画布坐标 —— #119 → 画布 **(924.8, 709.5)**、#120 → **(924.8, 792.8)**；
  而 `GenBtnT..+90 = 779.56…869.56`（`SettingsWindow.cs:628`）⇒ #120 那一下**确实落在钮的矩形里**。
- 旁证：同一页上**更高**的那颗【联机】点得到 ⇒ 不是整页没建。

### 已排除 / 还差一步
- 已排除：① 队列顺序 ② 点击坐标 ③ 整页未建（联机钮可点）④ `PointerReachable`（那是整窗级，同窗别的钮可点）。
- **还差的最后一步（必须真机/Unity 侧，不能靠读）**：确认 `GenButton` 里那句
  `Hit(n, "Hit", x1, GenBtnT, x2, y2, QOverlay, …)` 的返回值是不是 `null` ——
  `MenuDraw.Hit:2248` 有 `if (!ClipRect(r, PaddedClip(clip, maskPad), out hr)) return null;`（**整块在视口外 ⇒ 连节点一起不建**），
  所以最可能是**这一层 `ViewportClip` 把钮判到框外**（`ViewportClip.Resolve` 会沿父链找最近的 clip）。
  建议的做法（一条探针、5 分钟）：在 `GenButton` 里对 `Hit(...)` 的返回值判空并 `Debug.LogError`（**出声**），
  或在 `Editor/SettingsScene.cs` 里 dump `FindObjectsByType<WindowButton>()` 里含 `Redeem Code` 的那些。
  ⛔ **我没有直接改**：根因还差这一格，「把队列调大」正是你点名不许的蒙混法；而按本项目规矩
  （`CLAUDE.md` §三「红了先只读诊断、别猜着修」）应在证据闭合后再动。

## ② `U2`V1 头衔默认种子 —— **已落地（行数中性）**

- 落点：`Shell/ProfileData.cs` 的 `TitleIndex` **读口**（**1258 → 1258 行**，只改 1 行）：
  `get { EnsureSave(); if (_titleIndex < 0 && Titles.Count > 0) _titleIndex = 0; return _titleIndex; }`
- 语义：**只在内存里兜底**（⛔ 不走 setter ⇒ **不落盘、不产生多余的存档写**），每次读都是幂等的。
- 为什么下沉到读口：原兜底在 `ProfileTab.EnsureLocalDefaultTitle()`（**首次打开档案窗**才跑）⇒
  「没开过档案窗就直接进战斗」时 HUD 名牌仍空（= 账上那条代码项）。下沉后**任何读者**（HUD 名牌 / 头衔页）拿到的都是兜底值。
- 与原兜底**并存不冲突**：开过窗之后 `TitleIndex` 已是 0 ⇒ 那一句直接早退（`ProfileTab.cs` 一字未改）。
- ⚠️ **存档顺序风险已规避**：故意**不走 setter** ⇒ 读口不会触发 `SaveIfAuto()`，双写期不会被读操作意外写盘。
- 判据：原版那条「已装备称号」来自服务器（`PlayerDataManager.get_PlayerTitle`，本地拿不到）⇒ 本地兜底「清单第 1 条」，**用已有数据、不编占位文案**（与 `ProfileTab` 那条同一口径）。

### 该跑哪条
`MainMenuScene.Run`（档案窗那 58 条 `ProfileAssertU2b` 在这里）+ **`BattleScene.Run`**（HUD 名牌读 `TitleIndex`，这条改动真正的受益面）。
建议追加断言（我不能改 `Editor/**`）：① `ProfileData.TitleIndex = -1` 之后**直接**读（不碰档案窗）应得 `0`；
② 再 `= -1` 之后读**不得**产生存档写（`SaveCenter` 的写计数不变）；③ 清单为空时仍返回 `-1`（⛔ 不许兜底成 0）。

## ③ 验收
- `TMPDIR=/tmp/wf_B1UI bash 工具/typecheck.sh` = **运行时 0 / 编辑器 0**。
- 行数：`ProfileData.cs` **1258 → 1258**；`SettingsWindow.cs` 6649 → 6649（上一批）；`CardDisplayWindow.cs` +9/−1。
- 本批**没改** `SettingsWindow.cs` 的按钮/队列（见 ①：证据还差一格）。

---

# 设置窗钮 · 出声探针 + 断言（2026-10-27 第五批 · B1UI）

## ① 最小探针（`Shell/SettingsWindow.cs` · **6649 → 6649 行中性**）
`GenButton` 里那句 `Hit(...)` 现在**接返回值**并判空：
```
var hitNode = Hit(n, "Hit", x1, GenBtnT, x2, y2, QOverlay, …);
    aq, ArtButton); if (hitNode == null) { … Debug.LogError("[Settings] 钮 `" + name + "` 的命中区**没建出来**（rect … · 最近视口 …）⇒ 点它只会落到吸收层 `AbsorbHit`（2026-10-27 真机实测；⛔ 不许静默）"); }
```
- 报错里带三样：**钮名** · **算出来的命中矩形** · **最近 `ViewportClip` 的 `ClipPx`**（`ViewportClip.FindAbove(n)`）。
  最后这一格就是为了让同步点那一次 `SettingsScene.Run` 的日志**直接回答**「是不是被 `ClipRect` 判到框外」——
  如果打出来是 `rect (596.52,779.56)-(896.52,869.56) · 最近视口 (…)`，把矩形的 y 和视口的 y 一比就定案（不用再猜）。
- ⛔ 没碰 `ClipRect` / `MenuDraw.Hit` / 任何队列（按你的要求：根因未闭合前不改行为）。

## ② 断言（`Editor/SettingsScene.cs` · 只追加在**文件末尾**，既有行号零偏移）
新增 `AssertBottomHits(Transform page)`，在「④ 底下两颗钮」那段末尾调一句（**同一个 scope，`gtab` 现成**）：
1. **前提** `FindObjectsByType<WindowButton>()` 非空（否则下面两条是同义反复）；
2. **前提** 两颗钮的**节点**在；
3. **结论** 两颗钮各自**挂着一个 `WindowButton`**（= `Hit` 没被丢掉）。
⛔ 三条都是**硬 `CheckTrue`**，没有 `if (x != null) Check(...)` 那种静默跳过（本项目踩过三次）。
⚠️ 判据只到「命中区挂上了」这一层 —— **画出来的位置**对不对仍要并排比 / 真机点。

## ③ 验收
- `TMPDIR=/tmp/wf_B1UI bash 工具/typecheck.sh` = **运行时 0 / 编辑器 0**。
- 行数：`SettingsWindow.cs` **6649 → 6649**（3 处都是同行替换）；`SettingsScene.cs` **5703 → 5729**（+26 全在文件末尾，账里那 5 条 >1627 的落点**零偏移**，diff hunk 已核）。
- 该跑：**`SettingsScene.Run`**（10s）。看两样：① 新断言 3 条是否绿；② 日志里有没有 `[Settings] 钮 … 的命中区**没建出来**`
  —— 有 ⇒ 把那一行连同 `rect` / `最近视口` 两个矩形一起发我（或直接派下一件），根因就闭合了；
  没有 ⇒ 说明 `Hit` **建出来了**，那问题在「`WindowButton` 没登记进 `AllButtons()` / `HitBoxPx` 算不出」这一支（下一步查那里）。

---

# 商店货架顺序（`ShopData._shopItems`）· 2026-10-27 第六批（B1UI）

## 结论：**画面顺序没丢 —— 账上那条是假账**（只补注释，**没动数据结构**）

### 判据一：`_shopItems` 是**身份缓存**，不参与排序（现读全文 + 全仓 grep）
- 全仓对它的用法**只有三处**：`ItemFor` 的 `TryGetValue` / `_shopItems[k] = it`、`Load` 里的 `Clear()`；
  **没有一处 `foreach` / `Keys` / `Values` / `ToList`**（`grep -n "_shopItems" ShopData.cs` = 4 行，逐行看过）。
- 它**在 `ShopData.cs` 之外 0 个消费者**（`grep -rn "_shopItems" --include=*.cs CardPresentation` 除本文件外 = 0 命中）。
- 真正决定画面顺序的是 `ShopData.Offers(pageIndex)`（`ShopData.cs:238-246`）—— 返回 `_cards` / `_daily` / `_items`
  **三个定长数组**，顺序 = **数组字面量次序**；`ShopTabPage` 与自检都只读它（`ShopScene.cs:1614` 等）。

### 判据二：原版那一层是什么（读了反编译）
- `Everguild.LiveOps.FixedStoreData__AddItem.c` → `System_Collections_Generic_List<object>__AddWithResize`
  ⇒ 原版存的是 **`List`（插入序）**，`AddItem` 就是一句 `Add`；
- `Everguild.LiveOps.FixedStoreData__get_Items.c` 读 `+0x78` / `+0x70` 两个集合
  ⇒ 与你给的判据一致：**`items` 在前 + `offerLists` 按列表序展开**（`Where(IsActive)` 那一步在同一方法里）。
  我们**没有 offerLists 这一层**（三页的 store SO 没导出，见 `ShopData.cs` §「三个页对应的 store SO 一个都没导出」）。

### 所以
- **「字典 ⇒ 丢顺序」前提不成立**：字典不是顺序的载体，换不换 `Dictionary` 都不影响画面。
- ⚠️ **但也⛔ 别反过来说「顺序已验证」**：我们的 `_cards/_daily/_items` 是**手写数组**，
  它**逐条对应原版顺序**这件事**本地无判据**（那三份 SO 没导出）⇒ 报告里如实记成「数组次序的对错仍无判据」。

## 改动（行数中性）
`Shell/ShopData.cs` 的 `_shopItems` 那行 doc **就地改写**（**511 → 511 行**）：写明「不承担顺序职责 + 全仓无 `foreach`」
+ 「顺序在 `Offers()` 的数组字面量里」+「数组次序本身仍无判据」。⛔ 一行代码没动、没改数据结构。

## 建议（给账）
`任务总表` 那条「`ShopData._shopItems` 是字典 ⇒ 丢了顺序」建议改成：
> **假账**（2026-10-27 B1UI 核）：字典是**身份缓存**、不承担顺序；画面顺序 = `Offers()` 三个数组的字面量次序。
> **真缺口另立**：「我们那三页的手写商品表与原版 `FixedStoreData`（`items` + `offerLists` 展开）**是否同序**」——
> **判据在缺失的 3 份 store SO 上**，补到 SO 才验得了。

## 验收
- `TMPDIR=/tmp/wf_B1UI bash 工具/typecheck.sh` = **运行时 0 / 编辑器 0**。
- 本批**不新增** Unity 命令；`ShopScene.Run` 是这条的宿主（只在将来真动顺序时才必须跑）。

---

# 商店顺序 · 三跳取证（2026-10-27 第七批 · B1UI · **只读**）

> 判据正本 → **`判据/商店三页顺序_判据.md`**（三问逐条 + 三跳表 + 搜过哪里）。本节只留结论。

## ① 类名：`Everguild.LiveOps.FixedStoreData`（唯一一种 store 类）
`Premium Campaign Store.json` 的 `m_Script` pid → `MonoScript_4669455528150324631.json` 的 `m_ClassName/m_Namespace`。
扫全部 5,175 个 MonoScript，带 Store/ShopOffer 的只有 4 个：`FixedStoreData`（唯一数据类）· `ShopOfferContainer` · `ShopOfferDataV2` · `ShopTab`（后三个是界面件）。

## ② 本地有没有：**没有**（三页那三份不在离线包体里）
- 全 `assets_full`（84 个 bundle / 24.7 万文件）按字段名扫 `"offerLists"` / `"transactionLocation"` ⇒ **各 1 命中**，
  都是 `bundle_menus_assets_all/MonoBehaviour/Premium Campaign Store.json`，而它 `offerLists = []`、`items` 只有一条
  `premium_campaign_um` ⇒ **Premium 战役那条，不是商店三页**。
- **覆盖性核查**：AA 目录 `.bundle` **84 个** vs `assets_full` 的 `bundle_*` **84 个**、**差集为空** ⇒ 导出覆盖整个本地包，**没跳包**。
- ⚠️ **唯一没完全覆盖的角落**：`level0` 的 **147 个 MonoBehaviour 无法按类名判**（该文件**无 type tree**）；
  原始字节按 `*Store*` 扫只有一处 `Stores`、**没有任何 store 的 `eventId`** ⇒ 只能说「字节里没有」，不能说「按类名穷举过 level0」。
  闭合办法：借样板 typetree 逐实例读 `m_Script`（`工具/read_tmp_level0.py` 已有现成做法）。

## ③ 顺序：**无判据**（不是「错了」、也不是「对过了」）
原版顺序 = `FixedStoreData.get_Items`（`items` 在前 + `offerLists` 按列表序展开 + `Where(IsActive)`；
`__AddItem.c` 里是 `List.AddWithResize` ⇒ 插入序）。我们的 `_cards/_daily/_items` 是**手写数组**，
而那三份 SO **不在本地** ⇒ **差异表产不出来**。⛔ 别把「没判据」读成「有差异」，也别读成「已验证」。

## 给账
「`_shopItems` 是字典 ⇒ 丢顺序」= **假账**（见报告上一节）；真缺口另立为
「手写商品表 vs 原版 `FixedStoreData` 是否同序 —— **卡在原版数据在服务端（P3）**」。

## 本轮边界
只读 `d:/2/新解包资源/` + 写了 `判据/商店三页顺序_判据.md` 与本报告；**没改任何 `.cs`**、没动 `d:/2`、没动 git、没跑 Unity。

## ⑦ level0 闭合（第八批 · 只读）

判据全文 → `判据/商店三页顺序_判据.md` §④。要点：

- 复用 `工具/read_tmp_level0.py` 的同一套口径（MB 头 `script_of(raw)` 取 `m_Script` 的 `(fileID,pathID)` → `globalgamemanagers.assets` 的 MonoScript 类名表），**没改那个工具**。
- **147/147 个 MonoBehaviour 全部解出类名**（0 条读不出头 / 0 条脚本查不到）。
- **自检**：`pid 297/298`（`Loading text` / `Progress text` 的两条 TMP）解出 `TextMeshProUGUI`、`295/296` → `EverguildTextController`
  ⇒ 与当初那批结论逐条吻合，**字段没串位**。
- **结论升级**：**`level0` 里没有 `FixedStoreData`**（按类名穷举）；而且与 store 有关的 3 条都是**空组件**：
  `ShopOfferListLocator`(331) · `DailyStoresConfigProvider`(363) · `ShopOfferAssetProvider`(366) —— **原始体各 32 B = 只有 MB 头、零序列化字段**，
  扫不出任何 GUID / 路径串 ⇒ 它们是**纯代码服务**，本地**根本没有承载 store 定义的资产**。
- ⇒ 三页顺序的判据「卡在原版数据在服务端（P3）」这条，从「没找到」升级为「本地没有承载物」。

---

# 设置窗钮 · 第二支静态预研（2026-10-27 第九批 · **只读**）

## ① `WindowButton` 怎么被 `AllButtons()` 收进来 —— **没有登记表**
- `AllButtons()` = `Object.FindObjectsByType<WindowButton>(FindObjectsSortMode.None)`（`Shell/PointerLayer.cs:1257-1258`）。
- `Shell/WindowButton.cs:760` 明写「**没有「登记表」**（2026-09-23 撤掉）——原来想用 `OnEnable/OnDisable` 维护静态表」。
  ⇒ **「建出来但没登记」这条不可能发生**：只要组件活着就一定会被枚举到。
  ⇒ 失败只可能是 **(a) 组件没建 / 被销毁**，或 **(b) 建了但 `HitQuad` 判不出**，或 **(c) 点在 `center ± half` 之外**。

## ② `HitBoxPx` 依赖什么（三道，任一不过 = 点不到，**且全都静默**）
| 关 | 行号 | 条件 |
|---|---|---|
| 1 | `PointerLayer.cs:1246-1252` `HitQuad` | `b != null && b.isActiveAndEnabled` **且** `GetComponentInChildren<ImageQuad>()` 非空 **且** `q.gameObject.activeInHierarchy` ⇒ **hit 节点下必须有一个「活动」的 `ImageQuad`**（`MenuDraw.cs` 那段注释原话：「`PointerLayer` 只认 `ImageQuad` ⇒ 这一颗真鼠标点不动」） |
| 2 | `PointerLayer.cs:1206-1210` | `center = LayoutSpace.ToDesignPixel(q.transform.position)`（**裸世界坐标**，与指针同一帧） |
| 3 | `PointerLayer.cs:1216+` | `half = WorldW/H × 父链 lossyScale`（A167：窗根被 `TransformScalerBySmallScreenUI` 乘 M 时命中区跟着放大） |

## ③ 那两颗（`Redeem Code` / `Close Game Button`）与能点到的（联机入口）**建法同构**
- `GenButton`（`SettingsWindow.cs:3238-3266`）与 `GenOnlineEntry`（`:3283-3310`）逐句对齐：
  都是 `Node(page, …)` → `Rect(n, "bg", …, ArtButton, QContent, GenBtnTint, true)` → `Hit(n, "Hit", …, QOverlay, …, aq, ArtButton)`。
- **唯一结构性差异 = 位置**：`GenBtnT = 779.56`（底部那两颗，`:628`）vs 联机入口在页面中部（约 y≈700 那一带）。
  ⇒ **静态上不存在「不同建法」这条解释**；能解释「同页别的钮能点、这两颗不能」的只剩
  **「位置 → 裁切/可见性」**（`ClipRect` 判到框外 / 那一片的 quad 不在 `activeInHierarchy`）。

## ④ 两条分支（拿到同步点那句日志就能二选一）
### A. 日志出现 `[Settings] 钮 … 的命中区**没建出来**`（= `Hit(...)` 返回 `null`）
唯一出口 = `MenuDraw.cs:2248` `if (!ClipRect(r, PaddedClip(clip, maskPad), out hr)) return null;`
⇒ 查三样（按顺序）：
1. `ClipRect` 的输入 `clip` 是谁 —— `ViewportClip.Resolve(parent, …)`（`Shell/ViewportClip.cs:365`）沿 `n` 的父链找到的**最近 `ViewportClip`**；
   ⚠️ 注意 `BuildGeneralPage` 的 `page = Node(area, "General Tab", TabsL, TabsT, TabsR, TabsB)`（`SettingsWindow.cs:2227`）
   —— **页面不在 `Mask Tabs buttons` 那个 clip 里**（它是 `ViewportClip.Hang(area, …)`，`:1853`），所以 clip 只可能来自**更上层**；
2. 那个 clip 的 `ClipPx`（`ViewportClip.cs:257`）与按钮矩形 `(596.52,779.56)-(896.52,869.56)` 的 y 关系；
3. 对照 `GenOnlineEntry` 为什么没被判出去（同页、同 clip、只差 y）。

### B. 日志**没有**那句（= `Hit` 建出来了，`WindowButton` 与 quad 都在）
⇒ 按 ② 的三道逐一排除，全部有现成读口：
1. `PointerLayer.HitQuadForTest(b)`（`PointerLayer.cs:1307`）对这三颗是否为 `null`；
2. `PointerLayer` 的 `AllButtons` 读口（`:1309+`，注释写「自检用：场上所有 `WindowButton`」）里**有没有这三颗**；
3. 若 1/2 都正常 ⇒ 是 **center/half 错**（第 2/3 关）：量 `q.transform.position` 与 `WorldW/H`，与 `Screen()` 换算出来的矩形比。
   ⚠️ 这一支最容易骗人：**`Small Screen UI` 开关打开时（窗根乘 M）** 命中区与画出来那块的比例关系是 A167 那批修过的，
   别把「M ≠ 1 时缩了」当成缺陷。

## 结论
静态预研**无法二选一**（缺的正是那句运行时日志），但已经把可能性收成**两条、且每条都指到具体行号与现成读口**。
同步点跑完 `SettingsScene.Run` 后：**有那句 LogError ⇒ 走 A**；**没有 ⇒ 走 B**，两步之内必能定位。

---

# `TitleIndex` 兜底改位（2026-10-27 第十批 · 修 γ）

## 我按方案 1 改的（读口回到纯读，兜底挪到装载期）
| 落点 | 改法 | 行 |
|---|---|---|
| `Shell/ProfileData.cs:365` `TitleIndex.get` | **改回纯读**：`get { EnsureSave(); return _titleIndex; }`（⛔ 无副作用） | 同行替换 |
| `Shell/ProfileData.cs:1038` `EnsureSave()` 里 | `LoadFromDisk(); EnsureLocalDefaultTitleOnce();` —— **只在这一支**（`if (Application.isBatchMode) return;` **之后**） | 同行替换 |
| 类尾（`+14` 行，**不影响任何既有行号**） | 新增 `static void EnsureLocalDefaultTitleOnce()`：`if (_titleIndex >= 0) return; EnsureLoad(); if (_titles != null && _titles.Count > 0) _titleIndex = 0;` | 追加 |

- **「没选」重新可达**：批处理（`-batchmode`，自检全走这条）在 `:1037` 就 `return` 了 ⇒ `_titleIndex` 保持 `-1`
  ⇒ `BattleScene` 那两条「没选 ⇒ 整块关」的断言回到绿。
- **真机上仍然兜底**：非批处理装载期跑一次 ⇒ 玩家没开过档案窗就进战斗，HUD 名牌有值（原始目的不变）。
- **仍然不写盘**（内存内赋值，⛔ 不走 setter）；与 `ProfileTab.EnsureLocalDefaultTitle()` 并存（那边会顺手落盘，属于「开过窗后的持久化」）。

## 另外两条（你点名的）都补了
1. 🔴 **读口副作用** —— 已在 getter 行**写明**「⛔ 纯读、无副作用；兜底已挪到装载期」，并在新方法的 doc 里记了**为什么不能写在 getter**（2026-10-27 第一版就是这么把两条断言吃红的），防止下次有人再挪回去。
2. 🔴 **缺 `🔶 自己设计` 标记** —— 补了**两处**（调用点行尾 + 方法 doc 块），格式照铁律 18：
   `🔶 自己设计（非原版）：未装备时默认装「清单第 1 条」· 原版称号来自服务器 PlayerDataManager.get_PlayerTitle、本地拿不到 · 调度台 2026-10-27 裁定`

## 验收
- `TMPDIR=/tmp/wf_B1UI bash 工具/typecheck.sh` = **运行时 0 / 编辑器 0**。
- 行数：`ProfileData.cs` **1258 → 1272**；hunk = `@365` 同行替换 + `@1038` 同行替换 + `+14` 全在**文件末尾** ⇒ 账里 61 条落点**零偏移**（已核 hunk）。
- ⚠️ **一条边界**：兜底只在**非批处理**跑。若将来有人在**编辑器里直接跑** `BattleScene.Run`（非 `-batchmode`），
  那两条断言会再次变红 —— 那不是回归，是「自检跑到玩家那条路上去了」。到时要么用 `-batchmode` 跑，要么给自检加个 `ProfileData` 复位口。
