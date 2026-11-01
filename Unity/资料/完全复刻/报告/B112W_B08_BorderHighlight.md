# B-08 Border Highlight 族施工报告（`B112W_B08` · 2026-11-01）

> 执行代理报告。**没跑 Unity**、没动 git、没改账/活状态表/断言宿主。
> 🔴 **白名单偏差（请调度台过一眼）**：简报的 ✅ 是「`Shell/*Window.cs`（接线图点名的那几个窗）」，
> 而 #2 的真实落点是 `BaseOfferPopup.cs` / `DailyRewardPopup.cs` / `DailyStreakPopup.cs` / `OfferContainer.cs` 四个
> **`*Popup.cs` / 普通 `.cs`**（接线图 §表 2 #2 逐条点名）。我按「点名即授权」做了这四处，**每处只有一条同行追加、行数零变化**。
> 若调度台认定它们超范围，把这四处的 `MenuFxMaterials.ApplyBorderHighlightAppearAll(…, Green)` 删掉即可，其余不受影响。
> 判据来源 = `判据/B112W_B08_接线图.md` §表 2 / §2·2 / §顺手发现（**逐条现读复核过**，下面标出与它不一致的地方）。
> 原版材质 JSON 全部由本代理**现读**（`d:/2/新解包资源/assets_full/bundle_menus_assets_all/Material/*.json`），
> 本文里所有数值都取自 JSON 的 `m_SavedProperties`，不是从判据表抄的。

---

## 一、逐条（11 + #13）

> 「落点」行号是**改完之后的现读**（本批所有改动**行数零变化**，见 §四）。
> 所有「走原件」的材质都走 `WarpforgeShaderMap.TryResolve(原版名)` → `WarpforgeShaderLoader` → 随包 `wf_shaders.bundle`
> （与 `Shell/ForgeTab.cs:1143`、`MenuFxMaterials.BorderRotatingCampaignProto` 同一条路）。

| # | 原版材质 → 消费者 | 原版 `m_SavedProperties`（现读） | 我们的落点 | 怎么接的 | 改坏哪句会怎样 |
|---|---|---|---|---|---|
| **1** | `Border Highlight Appear Expansion pass`（`Material_-265774635809289417`）→ `Expansion Pass Reward Container/Collect Highlight Effect` | `_ColorMain (.8616262, 2.9029472, 4.2480173, 1)` · `_ColorSecondary (0,.15004349,1,1)` · `_Color_offsets (.8,1,0,0)` · `_Noise_Strength_X_Speed_Y_Scale_Z (.02,−.1,8.17,0)` · `_Scale_X_Speed_Y (.9,3,0,0)` · `_Reveal 1.0` · `_Smoothness .169` · `_Rotation 7.42` · `_AUTOROTATION 1` · `_AutoRotationSpeed −186.64` · `_DEBUG 1` · kw `_AUTOROTATION/_DEBUG/_SMOOTHGRADIENT` | 建：`Shell/ExpansionPassEventWindow.cs:1117`；材质 + 出厂关：`:1120`；播放：`:2275-2277` | `MenuFxMaterials.ApplyBorderHighlightAppearAll(hi.gameObject, ExpansionPass)`（九宫逐块克隆）+ `hi.gameObject.SetActive(false)`（= 原版 `Initialize` 无条件关）+ 领取时 `StartCoroutine(MenuFxMaterials.RevealSweep(hi, 1f, 0f))`（`_Reveal` −0.5→1 线性 · animTime 1 · animDelay 0） | 去掉 `SetActive(false)` ⇒ 每一格奖励出厂就挂着橙蓝高亮（原版只有领完那一格亮）；把 `RevealSweep` 删掉 ⇒ 领取后**瞬间**出现而不是扫入 1 s；把 `ExpansionPass` 换成别的档 ⇒ 颜色变（这张是橙→蓝） |
| **2** | `Border Highlight Appear Green`（`Material_-4286932454265387033`）→ 69 处 `…/Highlight`（`WebShop Button` · `WebShop Button Square Variant` · `Daily Reward Popup Entry` · `Daily Streak Reward Popup Entry`） | `_ColorMain (.2002818, 1.319508, 0, 1)` · `_ColorSecondary (0,1,.170378,1)` · `_Color_offsets (.8,1,0,0)` · `_Noise_Strength… (.02,−.1,8.17,0)` · `_Scale_X_Speed_Y (.9,3,0,0)` · `_Reveal 1.0` · `_Smoothness .032` · `_AUTOROTATION 0` · **kw 空** | ① `Shell/BaseOfferPopup.cs:1030`（`WebShop Button/Highlight`）② `Shell/OfferContainer.cs:1648-1650`（`WebShop Button Square Variant/Highlight`）③ `Shell/DailyRewardPopup.cs:505-506` ④ `Shell/DailyStreakPopup.cs:759` | 四处都是「建完之后 `ApplyBorderHighlightAppearAll(…, Green)`」（九宫逐块克隆；日常/连签那颗是单 quad，走 `ApplyBorderHighlightAppear`）。**不挂 `UIBorderGlow`、不编 hover 触发**（原版 82/85 不播，见 §二） | 把 `Green` 换成 `ExpansionPass`/`Raid` ⇒ 商店与日常的强调描边变成橙/红；漏掉 `BaseOfferPopup` 那处 ⇒ 21 份报价格 popup 全没有描边（最大一处覆盖） |
| **4** | `Border Highlight Appear Purple No Smooth`（`Material_-1689040976176618117`）→ `Two Sides Event Window/…/Team Bonus/Skull SDF` | `_ColorMain (1,0,.8814936,1)` · `_ColorSecondary (.1539540,0,1,1)` · `_Color_offsets (.8,1,0,0)` · `_Noise… (.02,−.1,8.17,0)` · `_Scale_X_Speed_Y (.9,3,0,0)` · `_Reveal .723` · `_Smoothness .032` · kw 空（**没有** `_SMOOTHGRADIENT`） | `Shell/ParentEventWindow.cs:1437-1438`（`Skull SDF`） | `MenuFxMaterials.ApplyBorderHighlightAppearAll(sdf.gameObject, PurpleNoSmooth)` | 换成 `Purple`（带 `_SMOOTHGRADIENT` 那份）⇒ 边缘柔化档不同 ⇒ 描边更「糊」；`_Reveal` 用 1.0 而不是 .723 ⇒ 揭开的角区间多一截 |
| **5** | `Border Highlight Appear Raid`（`Material_-5332349378384099707`）→ `Raid Reward Container/Collect Highlight Effect` | `_ColorMain (3.4415908,.9281037,0,1)` · `_ColorSecondary (1,.04843266,0,1)` · `_Color_offsets (.8,1,0,0)` · `_Noise… (.02,−.1,8.17,0)` · `_Scale_X_Speed_Y (.9,3,0,0)` · `_Reveal 1.0` · `_Smoothness .169` · `_Rotation 7.42` · `_AUTOROTATION 1` · `_AutoRotationSpeed −186.64` · `_DEBUG 1` | 建：`Shell/RaidEventWindow.cs:855-858`；播放：`:1493`（`SetHighlight`） | 同 #1：`ApplyBorderHighlightAppearAll(card.Highlight.gameObject, Raid)`；领取时 `StartCoroutine(MenuFxMaterials.RevealSweep(card.Highlight, 1f, 0f))` | `plan.PlayGlow` 那一跳删掉 ⇒ 领完只亮框不扫入；把 `Raid` 换成 `ExpansionPass` ⇒ 颜色从橙黄变青蓝 |
| **6** | `Border Highlight Appear White`（`Material_-3131437003372528232`）→ `Main Menu Offer Container*` 的 `Border Glow`（5 份 prefab） | `_ColorMain/_ColorSecondary` 全白 · `_Color_offsets (.8,1,0,0)` · `_Noise… (.02,−.1,8.17,0)` · `_Scale_X_Speed_Y (.9,3,0,0)` · `_Reveal 1.0` · `_Smoothness .156` · **`_StartOffset 168`** · kw `_SMOOTHGRADIENT` | **未接** —— 节点在我们这边不存在，且落点文件 `Shell/MainMenuRuntime.cs` **不在本代理白名单**内。补丁见 §六·① | —— | —— |
| **7** | `Border Highlight Rotation Yellow`（`Material_5632552898289262306`）→ `Expansion Pass Tab/Purchase premium Panel/IsNotPremium/Generic Simplified UI Button/Highlight` | `_ColorMain (337.79395, 155.70850, 0, 1)`（**HDR 过曝**） · `_ColorSecondary (1,.08363296,0,1)` · `_Color_offsets (.91,1,0,0)` · `_Noise… (.005,.005,15,0)` · `_Scale_X_Speed_Y (.9,3,0,0)` · `_Reveal .326` · `_Smoothness .345` · `_Rotation −2.06` · `_AUTOROTATION 1` · `_AutoRotationSpeed −180` · kw `_AUTOROTATION/_SMOOTHGRADIENT` | `Shell/ExpansionPassEventWindow.cs:881-882`（`Highlight` 九宫已存在，见 §七·①） | `MenuFxMaterials.ApplyBorderHighlightAppearAll(hiNp, RotationYellow)` | 把 HDR 的 `_ColorMain` 夹到 1 ⇒ 变成普通土黄、失去过曝辉光；`_Reveal` 用 1.0 ⇒ 一整圈都亮（原版只亮 .326 那一段） |
| **8** | `UI Border Highlight Appear`（**与 shader 同名的「总材质」**，`bundle_shaders_assets_all/Material/Material_-5500902993734456246`） | —— | **无消费者**（全 `assets_full` 只被它自己的包清单引用；R0 账 `深度 0`） | **不做**（铁律 11 例外①：原版也没用它） | 为它编一个节点 = 造出原版没有的行为（违 18·0） |
| **9** | `Border Gacha Button Glow`（`Material_-7161444275765033636`，shader = `… Pulsating Scale`）→ `Gacha Tab/Chest panel/Price Display Button/Button Higlight border` | `_ColorMain (4.2480173,.5810968,1.6562152,1)` · `_ColorSecondary (1,.0235849,.1237298,1)` · `_Color_offsets (.8,1,0,0)` · `_Noise… (.02,.01,8.17,0)` · `_Scale_X_Speed_Y (.8,3,0,0)` · `_Reveal 1.0` · `_Smoothness .169` · `_Rotation 7.42` · `_AUTOROTATION 1` · `_AutoRotationSpeed −186.64` · `_DEBUG 1` · kw `_AUTOROTATION/_DEBUG` | `Shell/GachaEventWindow.cs:670-673`（`_openCrateHighlightGo`） | `MenuFxMaterials.ApplyBorderGachaButtonGlowAll(_openCrateHighlightGo)`（shader 自走 `_Time`，**不补间 `_Reveal`**） | 给它补一个 `RevealSweep` ⇒ 每帧被协程覆盖掉自走动画（原版没有这条）；把 `_ColorMain` 写成 (1,0,1) 之类 ⇒ 粉色脉冲变紫 |
| **10** | `Border Highlight Appear Yellow`（`bundle_menusharedresources…/Material_-678375647445659077`，shader = `… Pulsating Scale`）→ `Campaign Tab/Tutorial Message/Highlight Up` + `Highlight Down` | `_ColorMain (337.79395,155.70850,0,1)` · `_ColorSecondary (1,.08363296,0,1)` · `_Color_offsets (.91,1,0,0)` · `_Noise… (.005,.005,15,0)` · `_Reveal .723` · `_Smoothness .032` · `_AUTOROTATION 0` · `_ColorMask 15/_StencilComp 8`（属性表里**没有**，是死值） | **未接** —— `Tutorial Message` 整棵我们没建（`Editor/RewardsScene.cs:5854-5855` 明写「不建，改由教程线做」）；补丁见 §六·② | —— | —— |
| **11** | `Border Highlight Rotating Campaign`（`Material_1780489488266546679`）→ `Campaign Node/Collectable Highlight` | `_ColorMain (0,.2480117,.4607843,1)` · `_ColorSecondary (0,.1366984,2.2706029,1)` · `_Color_offsets (.92,1,0,0)` · `_Noise… (.02,.015915494,7.09,0)` · `_Scale_X_Speed_Y (.9,3,0,0)` · `_Reveal 1.0` · `_Smoothness .706` · `_AutoRotationSpeed **−0.5**` · `_FLIPDIRECTION 1` · kw `_AUTOROTATION/_FLIPDIRECTION` | `Shell/CampaignTab.cs:660-673` | **本批之前已经做完**（同波 `B-187`：`MenuFxMaterials.ApplyBorderHighlightRotatingCampaign(hlQ)`，未提交的工作树里）。本代理**现读复核**：参数逐值一致、`GlowSdf` 那族已撤掉 ⇒ 不需要再动 | 退回 `GlowSdfHighlightSmoothRed` ⇒ 装错族（原版是 Pulsating Scale 那支），颜色/运动全错 |
| **12** | `UI Border Highlight Appear Pulsating Scale`（与 shader 同名的「总材质」，`bundle_shaders_assets_all/Material/Material_-8491520406743025510`） | —— | **无消费者**（同上，R0 `深度 0`） | **不做**（铁律 11 例外①） | 同上 |
| **13** | `Border Highlight Pulse Blue`（`Material_1603963957059948543`，shader **`UI Border Highlight Pulse Alpha`**）→ `Expansion Pass Reward Container/Personal Progression/Claim Button Container/Claim Button/Highlight button claim` | `_ColorMain (0, 112.16699, 337.79391, 1)`（HDR） · `_ColorSecondary (0,.30313706,1,1)` · `_Color_offsets (.91,1,0,0)` · `_Noise_Strength_X_Speed_Y_Scale_Z (.007,.05,15,0)` · `_ScaleFromCenter (1,1,0,0)` · `_TimesParameters **(0.5,1,0.5,0)**` · `_AnimationSpeed 1` · `_Reveal .254` · `_Smoothness .345` · `_Rotation −2.06` · `_NOISE_CHANNEL 0` · kw `_NOISE_CHANNEL_R` | `Shell/ExpansionPassEventWindow.cs:1183-1185`（九宫已存在，sprite/九宫/矩形与原文逐值一致）+ **新建 shader** `WarpforgeVFX/Shaders/WFUIBorderPulseAlpha.shader` + `MenuFxMaterials.ApplyBorderHighlightPulseBlueAll` | 见 §三 | 改坏 `Pulse01`（比如把 `toOff` 段删掉）⇒ 变成常亮呼吸而不是 0→1→0；把 `_ColorMain` 夹到 1 ⇒ 失去青色过曝；`_Noise` 不喂 ⇒ 噪声偏移恒 0、描边变成死板一圈 |

**`Border Highlight Appear` 这一族的公共事实**（§2·2 之外、本代理现读补的）：

- 属性表（`Shader_6773909295900798026.json`）**只有**：`_ColorMain _ColorSecondary _Color_offsets _DEBUG _Noise_R _Noise_Strength_X_Speed_Y_Scale_Z _ScaleFromCenter _FLIPDIRECTION _SMOOTHGRADIENT _StartOffset _Reveal _Smoothness _Rotation _AUTOROTATION _AutoRotationSpeed _QueueOffset _QueueControl`。
  材质 JSON 里的 `_Length(0.43,.9,0,0)` / `_MaskScale(1,1,0,0)` / `_Add 0.09` / `_Range 89` / `_RangeStartOffset 196` / `_NoiseStrength 0.04` / `_ColorMask 15` / `_StencilComp 8` **都不在表里** = 死值 ⇒ 一律不抄（`HasProperty` 兜底）。
- `_ObjectScreenSpacePosition` / `_OffsetTest` / `_TempOffset` 是逐实例由 `UIBorderGlow.ModifyMesh` 写 `uv0.zw` 的那几格 ⇒ **不是常量**，没抄。
- `Pulsating Scale` 那支（`Shader_-4463273156108355430`）属性表**没有** `_ScaleFromCenter` / `_SMOOTHGRADIENT`；`_TimesParameters` / `_Inner_Edge` / `_Outer_Edge` / `_NoiseIntensity` / `_Outline` 也是死值。

---

## 二、`playOnEnable` 那 82 处的处理口径

本代理**现读复核**了 `bundle_menus_assets_all` 里全部 `m_Script = -3019295912040197694` 的实例（脚本扫描，不是抽样）：

- **85 个 `UIBorderGlow` 实例**；`playOnEnable` 真值 **3 个**（另 82 个 = 0）。
- `animTime` **85/85 = 1.0**；`animDelay` **85/85 = 0.0**。三个 ON 的实例也全是 `(1.0, 0.0, 1)`。
- 有 `UIBorderGlow` 组件的消费者在本批里只有 **#1 / #5**（`Collect Highlight Effect` 两颗，dump 的组件列 = `Image,UIBorderGlow`）；#11 那颗也挂（已由 `B-187` 处理，且原版 `playOnEnable = 0`）。

⇒ **口径**：
1. **82 处只挂材质、静态显形**，⛔ **不给它们编 hover / 出现就播**的触发（那是原版没有的行为，违 18·0）。本批 #2/#4/#7/#9 全部按这条处理。
2. **只有 #1 / #5 会播**，触发点 = 领取之后 `ToggleHighlightEffect(true)`；本批用 `MenuFxMaterials.RevealSweep`（`_Reveal` −0.5→1 线性、1 s、无延迟）实现。
3. **⛔ 没有挂 `Shell/UIBorderGlow.cs`**，两条硬原因（都可在本机复核）：
   - ① 这两支原版 shader 的**属性表里没有 `_Color`**，而 `UIBorderGlow.ChangeGraphicAlpha` 走 `ImageQuad.SetTint` → `sharedMaterial.color`
     ⇒ 每次播都打 `Material doesn't have a color property '_Color'`（本项目已有先例记录：`MenuFxMaterials.ApplyOrangeButtonToPurple` 的 doc）。
   - ② 我们的 `Collect Highlight Effect` 是**九宫多块**（原版是一颗 `Image`），`UIBorderGlow.CacheRefs()` 用的是
     `GetComponent<ImageQuad>() / GetComponent<Renderer>()` ⇒ 挂在节点上**找不到任何渲染件**（`Glow` 那几颗是 `MenuDraw.Nine` 建出来的**子件**）。
     实测先例：`Shell/ForgeTab.cs:1063` 把组件加在 `glowNode.gameObject` 上，而 quad 是 `ImageQuad.Create(glowNode, …)` 建的**子对象**
     ⇒ 那个组件在 Forge 那边同样拿不到渲染件（**属既有缺陷，不在本批白名单内**，见 §七·②）。
   ⇒ `RevealSweep` 因此打了一条 `🔶 自己设计（非原版）` 标记（协程替 `DOTween.DOFloat`、九宫各写一份）。

---

## 三、#13 走了哪条路、为什么

**结论：走「照 Ruri 源码重建」（路 ②），不走「补 menu 专用 shader 包走原件」（路 ①）。**

| 路 | 可行性（本代理现读核过） | 取舍 |
|---|---|---|
| ① 补一个 menu 专用 shader 包，让 `Everguild/UI/UI Border Highlight Pulse Alpha` 进 `wf_shaders.bundle` | 需要**重打 bundle**（Unity / 打包脚本 + 改 `StreamingAssets`），本代理**不许跑 Unity** | ❌ 不可行（本批） |
| ② 照 Ruri 反编译的 HLSL 重建 | **可行**：`d:/2/tools/ruri_out_1020_allshaders/ExportedProject/Assets/Shader/Everguild_UI_UI Border Highlight Pulse Alpha/` 下 `Sub0_Pass0_Vertex_b1.hlsl`（脉冲相位）+ `Sub0_Pass0_Fragment_b7.hlsl`（屏幕 UV 噪声 + `_MainTex` + HDR 线性→sRGB）**逐指令可读** | ✅ 采用 |

落点：**新建** `Unity/MyGame/Assets/WarpforgeVFX/Shaders/WFUIBorderPulseAlpha.shader`（名 `WarpforgeVFX/UI/UI Border Highlight Pulse Alpha`），
骨架照 `WFUIBorderHighlight.shader`（`CGPROGRAM` + `UnityCG.cginc` + **不写 `LightMode`** ⇒ URP 走 `SRPDefaultUnlit`）。

逐句对应：

- **顶点脉冲**（`Vertex_b1.hlsl:283-323`）：`t = _Time.y·_AnimationSpeed`；`total = toOn+on+toOff+off`；`tt = total·frac(t/total)`；
  `pulse = saturate(tt/toOn) · saturate(1 − (tt−(toOn+on))/toOff)`。取 `_TimesParameters=(0.5,1,0.5,0)` ⇒ **2 s 一圈：0.5 s 升、1 s 满、0.5 s 落**。
- **片元**（`Fragment_b7.hlsl:76-273`）：屏幕 UV × `_Noise_Strength.z` 绕 (−0.5) → 按 `_Time.y·_Noise_Strength.y` 旋转 → 采样 `_Noise.r` ×2−1
  → `uv'` = 原 uv 绕 0.5 乘 `_ScaleFromCenter` 再 + `n·_Noise_Strength.x` → `k = smoothstep((c.r−off.x)/(off.y−off.x))`
  → `lerp(LinearToSRGB(_ColorSecondary), LinearToSRGB(_ColorMain), k)` → `rgb = c.a·col·COLOR.rgb`、`a = c.a·COLOR.a·pulse`。
- **混合** = 原版 `Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha`。

**与原版的两处差异（都是「载体不同」，不是算式不同，已在 shader 头写明）**：
① 顶点色里合成 `v.color * _Color`（原版只有 uGUI 的 `COLOR`；我们 `ImageQuad` 用材质 tint）——`_Color` 出厂白 ⇒ 默认逐像素相同；
② 取屏幕 UV 用 `ComputeScreenPos`（平台无关）替 `gl_FragCoord/_ScaledScreenParams`。

---

## 四、行号影响

**所有改过的既有文件都是【行数零变化】**（`git diff --numstat` 逐份核过），所以账里按 `文件:行号` 的落点**一个都不会漂**：

| 文件 | numstat |
|---|---|
| `Shell/ExpansionPassEventWindow.cs` | 12 / 12 |
| `Shell/RaidEventWindow.cs` | 3 / 3 |
| `Shell/ParentEventWindow.cs` | 1 / 1 |
| `Shell/GachaEventWindow.cs` | 1 / 1 |
| `Shell/BaseOfferPopup.cs` | 1 / 1 |
| `Shell/DailyRewardPopup.cs` | 1 / 1 |
| `Shell/DailyStreakPopup.cs` | 1 / 1 |
| `Shell/OfferContainer.cs` | 2 / 2 |

- `Shell/MenuFxMaterials.cs`：**只在类尾追加**（唯一被账引用的行是 `:60`，在追加点之前，未动）；同文件里 `ApplyProto` 内部改了几行（也在 `:60` 之后）。
  ⚠️ 该文件工作树里另有同波 `B-187/B-188` 的未提交改动（`+136`），本批再叠 `+328` ⇒ 合计 `464/0`。
- **新建**：`Assets/WarpforgeVFX/Shaders/WFUIBorderPulseAlpha.shader`（无 `.meta`，交给 Unity 导入时生成）。
- ⚠️ 所有「追加语句」都写在**同一物理行的行尾**（没有 `//` 在前），沿用本仓「行数零变化」的纪律；
  但**这几行以后不要在其末尾再追加**（会落到语句后面），要改就整行替换。

---

## 五、typecheck

```text
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
（`TMPDIR=/tmp/wf_b112w08 bash 工具/typecheck.sh`，用独立 TMPDIR 避免与并发写手互相覆盖。）

---

## 六、还差什么

1. **#6 `Border Glow`（Main Menu Offer Container ×5）—— 补丁就绪，落点文件不在白名单**
   （`Shell/MainMenuRuntime.cs` 不属于 `Shell/*Window.cs`）。几何已从 5 份 prefab 现读，**全部是「卡框外扩 ±40.77 / ±37.2」**：
   - `1x1` 卡 `535×414` → glow `616.54×488.34`；`1x2` 卡 `535×848.8` → glow `616.54×923.14`（另 3 份 1x1 逐值相同）。
   - sprite = `OctagonUI_Filled_Fade_SDF`（128²）· 九宫 `52,52,52,52` · `Sliced` · **`fillCenter = 0`** · `ppuMul = 1`；
   - Image `m_Color = (1, 0.165, 0.966, 0.537)`；组件 = `Image, EverguildButtonMaterialModifier` ⇒ **没有 `UIBorderGlow`**（静态）。
   - 补丁（插在 `Shell/MainMenuRuntime.cs:3019` 那条 `Border` 的**下一行**；那条 `Border` 已占用 3019 一行的写法，直接接在后面即可）：

     ```csharp
     var borderGlowGo = MenuDraw.Nine(card, CardArt.MenuUi("OctagonUI_Filled_Fade_SDF"),
                          new PxRect(x - 40.77f, y - 37.2f, x + w + 40.77f, y + h + 37.2f),
                          new Vector4(52f, 52f, 52f, 52f), 128f, 128f, QOverlay,
                          new Color(1f, 0.165f, 0.966f, 0.537f), false, "Border Glow", null, clip);
     MenuFxMaterials.ApplyBorderHighlightAppearAll(borderGlowGo, MenuFxMaterials.BorderHighlightAppear.White);
     ```
     ⚠️ 需与 `Border` 那条同一个 `clip`、且**画在 `Border` 之后**（原版兄弟序 Border → Border Glow）。
     ⚠️ `x/y/w/h` 用 `BuildModeCardForCard` 里那一组局部变量（`Border` 那条就在用它）。
2. **#10 `Border Highlight Appear Yellow`（教程线的 `Tutorial Message/Highlight Up|Down`）**
   - 节点**整棵没建**（`Editor/RewardsScene.cs:5854-5855` 明写「不建 ⇒ 改由教程线做」）⇒ 本批不接。
   - 参数已备（§一 #10 那一行，`Pulsating Scale` 支）。**要用时照 `MenuFxMaterials.GachaGlowProto()` 复制一份改 3 个颜色/1 个 `_Reveal`/`_Smoothness`/`_AUTOROTATION`**（或把那个工厂参数化）。
3. **新 shader 必须验一次编译 / 不被 build 剥掉**（本代理不跑 Unity）：
   - `WFUIBorderPulseAlpha.shader` 只在**运行期 `Shader.Find`** 取用，没有任何 `Resources/` 下的 `.mat` 引用它
     ⇒ **出 player 有被剥掉的风险**。建议（**在白名单外，需调度台做**）：
     ① 往 `WarpforgeVFX/Runtime/WarpforgeShaderMap.cs` 的 `Replacements` 加一条
        `{ "Everguild/UI/UI Border Highlight Pulse Alpha", "WarpforgeVFX/UI/UI Border Highlight Pulse Alpha" }`；
     ② 重跑 `WarpforgeSetup.RegisterShaders`（它会遍历这张表的值进 Always Included）。
     在那之前，`MenuFxMaterials.BorderPulseAlphaProto()` 取不到会**出声告警**并保留静态图（不静默）。
4. **要跑的宿主 / 真机看什么**（本例没有新增断言，靠运行时 `Note` 与肉眼）：
   - `ExpansionPassEventWindow`：进战令页 → 领一次奖励 ⇒ 该格 `Collect Highlight Effect` **扫入**（1 s）而不是瞬现；`Claim Button` 上蓝青脉冲以 2 s 为周期 0→1→0。
   - `RaidEventWindow`：领奖后 `Collect Highlight Effect` 同样扫入。
   - `GachaEventWindow`：`Price Display Button` 的 `Button Higlight border` 应**缓慢自转的品红脉冲**（shader 自走 `_Time`，不靠补间）。
   - `BaseOfferPopup` / `OfferContainer` / `DailyRewardPopup` / `DailyStreakPopup`：绿色强调描边应**静态出现**（原版就不播）。
   - ⚠️ 一处**必须真机核**：`Collect Highlight Effect` 的 Image `m_Color.a` 现读 = **0**（`CollectHiTint`），
     而我们按原版值照抄 ⇒ 若该 shader 真的乘了顶点 α，原版这颗高亮本来就看不见。**我们与原版逐值一致**，但仍值得看一眼「领完到底亮不亮」。

---

## 七、顺手发现

1. 🟢 **判据 §表 2 #7 说「`Highlight` 子件没建」—— 不成立**：`Shell/ExpansionPassEventWindow.cs:881` 早就建好了九宫
   （矩形 `NpHighlightR = R(-0.7, 249.3, 306.9, 359.9)`，与本次现读的 prefab `(163.1,249.5)-(470.7,360.1)` 屏幕绝对坐标**逐值吻合**），
   缺的只是**材质**。本批只补了一行 `ApplyBorderHighlightAppearAll`。
2. 🔴 **`MenuFxMaterials.ApplyProto` 原来对「没有 `_Color` 的原版 shader」会静默丢掉染色/α**
   （`HasProperty("_Color")` 为假 ⇒ 什么都不写 ⇒ 新材质的 tint 恒白）。受影响的不只本批：`B-187`（#11 `Border Highlight Rotating Campaign`）
   与 `B-188`（`UI Orange Button To Purple`）走的是同一个 `ApplyProto`。
   ⇒ 已改成 **`else q.SetVertexTint(tint)`**（`B-190` 那条顶点色通道），原版 shader 读的正是顶点 `COLOR`，语义更贴原版。
   ⚠️ 副作用（好的）：这三族以后 `ImageQuad.SetTint` 也不会再打 `Material doesn't have a color property '_Color'`。
3. 🟠 **`B109_R15_菜单13材质.md` §1·10 的断言那句与 Ruri 源码不符**：它写「`Pulse01(t)` 在 t=0.5 时=1、t=1.5 时≈0.5」。
   按 `Vertex_b1.hlsl:283-323` + `_TimesParameters=(0.5,1,0.5,0)` 实算：**t=0.5 → 1**、**t=1.5 → 1**（下降段从 1.5 才开始）、**t=1.75 → 0.5**、**t=2.0 → 0**。
   ⇒ 将来写断言请按源码，别照那句话（否则会红）。
4. 🟠 **`Shell/ForgeTab.cs:1063` 的 `UIBorderGlow` 拿不到渲染件**（同 §二 ② 那条）：`glowNode` 是 `MenuDraw` 建的**空节点**，
   而 quad 是 `ImageQuad.Create(glowNode, …)` 建出来的**子对象** ⇒ `CacheRefs()` 三个 `GetComponent` 全 null，
   `Play()` 只是把 `PlayCount` 加 1、`DelayedAnim` 空转。**不在本批白名单**（`ForgeTab.cs` 未授权），记在这里。
   顺带：`Editor/RewardsScene.cs:4644-4645` 的断言正因为它是挂在**节点**上才绿 —— 那条断言只证「组件在」，不证「它会动」。
5. 🟠 **`Shell/CardDisplayEffects.cs:286` 的 `go.AddComponent<UIBorderGlow>()`** 也需要核一下 `go` 是不是 quad 本身
   （同族风险，未展开查）。
6. 🟢 **三个 `playOnEnable = 1` 的实例值完全一致**：`animTime 1.0 / animDelay 0.0 / playOnEnable 1` ⇒ 本批两个窗口都按 `(1, 0)` 硬编码，没有第三种情况。
7. ⚪ `MenuFxMaterials` 里 `ForgeGlowMaterial()` 写了一批**属性表里没有**的名字（`_Length` / `_MaskScale` / `_ScaleFromCenter` / `_Add` / `_NoiseStrength` / `_Range` / `_RangeStartOffset` / `_SMOOTHGRADIENT`），
   因为有 `HasProperty` 兜底所以是 no-op —— 不是 bug，但「看着像在生效」，容易让后人以为这些值起了作用。本批新代码**只写表里有的**。
