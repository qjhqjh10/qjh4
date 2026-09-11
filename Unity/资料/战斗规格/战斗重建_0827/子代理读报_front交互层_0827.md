# 子代理读报: 原版战斗 FrontCanvas 交互层权威规格（battlearena1 + 13 场景差异）

> 任务: 完整读取 FrontCanvas 交互层（攻击选择/技能描述/聊天/换牌/选卡/错误提示/导语/Overtime/工具锚）原始 Unity JSON 与说明书，输出权威规格。
> 数据源: `D:/2/解包整理/07_场景/battlearena1/{GameObject,RectTransform,MonoBehaviour,Canvas,Transform,AudioSource}/`（全部原始序列化 JSON，逐 PathID 读取）
> 场景对照: battlearena1/2/3 + 10 阵营场景（aeldari/astramilitarum/blacklegion/darkangels/emperorschildren/genestealers/leviathan/sororitas/spacewolves/tauviorla）共 13 个
> 贴图: `Warpforge_tools/data/ui_extract/battlearena1_sprite_map.json`（PathID→名）+ `解包整理/03_界面UI/图集/battleatlasui/Sprite/` m_Rect + `解包整理/12_主程序资源/Sprite/` m_Rect + `12_主程序资源/内置资源/Sprite/`
> 坐标: `chain_rect.py`（Godot 1920×1080，y 向下，含 pivot/锚点/父链/scale）
> 生成日期: 2026-08-27

**三口径说明**（本报告严格区分）:
- **raw** = RectTransform JSON 的 m_AnchoredPosition/m_SizeDelta/m_AnchorMin/Max/m_Pivot（相对父锚点, Unity y 向上）
- **chain_rect 绝对** = chain_rect.py 沿 m_Father 链换算的 Godot 屏幕坐标 x[y1,y2]（y 向下, 可直接用于 1920×1080）
- **dump 全树** = `说明书/01_战斗_对战/battlearena1_2D层全树.md` 的视觉布局值（与 `2D层_battlearena1全树.md` 完全相同, diff=0; 其坐标为右上偏移式的局部表达, 仅参考）
- 所有数值均以 raw 为准；本报告与权威表 `d:/2/战斗重建_0827/战斗界面JSON权威表_0827.md` 的矛盾见 §15。

**行内约定**: GO/RT/MB 均为 scene 内 PathID（如 GO 172 / RT 2973 / MB 4492）；`Image` 指 m_Sprite 的 PathID；`active` 列 = m_IsActive。

---

## 1. FrontCanvas 层级结构与 Canvas 参数

```
Canvas (GO 983, RT 2759)  ← 主 Canvas（Screen Space Camera）
├─ UI Error Message Controller (MUST BE ENABLED) (GO 481, RT 3373)  ← 直接挂主 Canvas
├─ BackCanvas (GO 1198, RT 2684)  ← Canvas_2575 renderMode=2 Overlay
└─ FrontCanvas (GO 606, RT 2900)  ← Canvas_2569 renderMode=1, overrideSorting=True
   ├─ ShadePanel (GO 763, RT 2952, inactive)  ← 直接子级（不是 Safe area 子级!）
   ├─ Safe area FrontCanvas (GO 713, RT 2862, stretch 全屏)
   │  ├─ Drag Attack Selector (GO 172, RT 2973)
   │  ├─ ActiveSkillDesc (GO 673, RT 2911, 场景 inactive)
   │  ├─ Unit Chat (GO 246, RT 3087)
   │  ├─ Mulligan (GO 321, RT 3344)
   │  ├─ Generic Multi Card Display Combat (GO 768, RT 3175, inactive)
   │  ├─ ChooseCardMenu (GO 322, RT 3273, inactive; blacklegion=active)
   │  ├─ Card Display Window (GO 419, RT 3030, inactive)
   │  ├─ DebugButtons (inactive) / AboveShader (GO 682, RT 3055)
   │  ├─ PopupAnchor (GO 920, RT 2686) / BattleSettingsPanel (inactive) / Tutorial / LoadingMenu / Alliance Panel
   └─ TooltipManager (GO 459, RT 2890)  ← 直接子级（不是 Safe area 子级!）
```

Canvas 组件（raw 实证）:

| 组件 | raw 值 |
|---|---|
| 主 Canvas (Canvas_2572) | m_RenderMode=1 Screen Space Camera, m_Camera=1462(UI Camera), m_PlaneDistance=100, m_OverrideSorting=**False**, m_SortingLayerID=0(Default), m_SortingOrder=0, m_AdditionalShaderChannelsFlag=25 |
| FrontCanvas (Canvas_2569) | m_RenderMode=1, m_Camera=1462, m_PlaneDistance=100, m_OverrideSorting=**True**, m_SortingLayerID=**2007638115**, m_SortingOrder=0, AdditionalChannels=25 |
| FrontCanvas GraphicRaycaster (MB 4849) | m_IgnoreReversedGraphics=1, m_BlockingObjects=0(None), m_BlockingMask Bits=32 |
| FrontCanvas 另含 CanvasRenderer (MB 4849 之外), 无 CanvasGroup | — |
| CanvasScaler (MB 4731, 主 Canvas) | m_ReferenceResolution=(1920,1080), m_UiScaleMode=1(Scale With Screen Size), m_MatchWidthOrHeight=0(匹配宽度) |

Godot 复刻要点: 主 Canvas 与 FrontCanvas 同为 Screen Space Camera（挂 UI Camera, planeDistance 100）; FrontCanvas 用 overrideSorting=True + sortingLayerID=2007638115 把自己抬到 BackCanvas(Overlay) 之上——**双层 Canvas 且 Front 层必须独立排序层**。BackCanvas 是 renderMode=2(Overlay)（权威表已记）。

---

## 2. Drag Attack Selector（攻击选择器）全树

父链: Safe area FrontCanvas(2862) → Drag Attack Selector(2973)。根 GO 172 组件: RT 2973 + MB 4240(脚本 -5378975638431045206) + MB 4602(-2470792663603520348) + MB 4455(-3229211799126679632) + Transform 2566 + **MB 4391 = GraphicRaycaster(3762903421402112529)**（拖拽选择器自带 raycaster, 拖拽时保证可在 UI 上方接收事件）。

| 元素 | GO/RT/MB | raw RT (pos/size/pivot/anchor) | chain_rect 绝对 | 贴图 (PathID=名, 原尺寸→显示=缩放) | 文字 (text/fs/color/对齐) | active | 备注 |
|---|---|---|---|---|---|---|---|
| Drag Attack Selector | GO 172 / RT 2973 | pos(0,0) size(621.72,122.25) pivot(0.5,0.5) anchor(0.5,0.5) | x[649.1,1270.9] y[478.9,601.1] (621.7x122.2) | — | — | True | 3 脚本+GraphicRaycaster; 由 AttackSelector 脚本运行时定位到被选单位上方 |
| Select Attack Background | GO 951 / RT 3061 / Image MB 4492 | pos(0,-18.38) size(780,285) pivot(0.5,1) anchor(0.5,1) | x[570.0,1350.0] y[497.3,782.3] (780x285) | sprite=0 (m_Type=0) color(1,1,1,1) 运行时由脚本画 | — | **False** | 附脚本 MB 4339(-8545018526213454877)+MB 5200(3079216281970865761)（运行时填充攻击范围图） |
| Select Melee Button | GO 867 / RT 3368 / Image MB 4206 | pos(0,0) size(118.37,118.81) pivot(0.5,0.5) anchor(0,0) | x[590.0,708.3] y[541.7,660.5] (118.4x118.8) | 8511722742595327015 = Attack type button Melee (154x154) → 0.769x | — | True | Button: MB 4957 (EverguildButton 1015376240363272691, m_Transition=1 ColorTint, targetGraphic=4206, OnClick=空) + MB 4956 (6466633716071143260) + MB 4716(空壳 -9096086197743689236) |
| Select Active Skill Button | GO 760 / RT 2626 / Image MB 4054 | 同上 (三钮静态同一坐标) | 同一坐标 | sprite=0 (运行时按技能图标 set) | — | True | Button MB 4677(EverguildButton)+4449(6466633716071143260)+4749(空壳); **ValueText 是它的子节点** |
| Select Range Button | GO 1005 / RT 2804 / Image MB 4121 | 同上 | 同一坐标 | 5998267373089430251 = Attack type button Ranged (154x154) → 0.769x | — | True | Button MB 5240(EverguildButton)+4361+4779 |
| Highlight ×3 | GO 112(RT 3028,MB 5267) / GO 396(RT 2776) / GO 180(RT 3512) | pos(0,0) size(176.23,176.88) pivot(0.5,0.5) anchor(0.5,0.5) | x[561.0,737.3] y[512.7,689.6] (176.2x176.9) | -7315808829623560242 = Attack type button highlight (227x227) → 0.776x | — | True | 高亮环比按钮大 58px; 各含脚本 MB 4481/4108/4478(-6857224376968460714 高亮动画) |
| ValueText | GO 1042 / RT 2617 / MB 3755 | anchor(0,1) pos(59.64,-60.61) size(91.88,**0**) pivot(0.5,0.5) | x[603.6,695.5] y[602.3,602.3] 高度=0(自动) | — | `0` fs=89.01 白(1,1,1,1) H=2(中) V=512(中) fontAsset=3485036404935369831 | True | 技能数值(攻击力/费用), 运行时更新; 高度 0=按行高自动撑开 |

关键结论: 三钮 anchor(0,0) pos(0,0) 118.4×118.8 静态完全重叠（运行时切换显示）, Highlight 三份也重叠于同一位（176×177）；Select Attack Background 是 780×285 的运行时绘制区（场景 inactive）。

---

## 3. ActiveSkillDesc（技能描述面板）

父链: Safe area FrontCanvas(2862) → ActiveSkillDesc(RT 2911)。根 GO 673 组件: RT 2911 + Transform 2218 + MB 4368(脚本 5546416171644318052)。

| 元素 | GO/RT/MB | raw RT | chain_rect 绝对 | 贴图 | 文字 | active | 备注 |
|---|---|---|---|---|---|---|---|
| ActiveSkillDesc | GO 673 / RT 2911 | anchorMin(0.6656,0) anchorMax(0.9658,0.3040) pos(-606.11,55.00) size(-0.0004,-3.51) pivot(0.5,0.5) | x[671.8,1248.2] y[698.4,1023.2] (576.4x324.8) | — | — | **场景 b1=False; 12 阵营场景=True** | 底右区 576×325 面板（内部元素按锚点百分比布局） |
| HideAbilityButton | GO 1048 / RT 2860 / Image MB 4207 | anchor(1,0) pivot(1,0) pos(220.39,-146.01) size(3707.83,1985.32) | x[-2239.2,1468.6] y[-816.1,1169.3] (3708x1985) ⚠超屏遮挡层 | 1660267235368898380 = `Background` (32x32, **border 10,10,10,10**) m_Type=1(Sliced) | — | True | color(1,1,1,**0**) 全透明点击拦截层（点面板外关闭） |
| AbilityContainer | GO 724 / RT 3128 / Image MB 4864 | anchor(0.0687,0.0831)-(0.9126,0.7644) pos(0,0) size(0,0) | x[711.4,1197.8] y[775.0,996.3] (486.4x221.3) | sprite=0 | — | b1=True; **12 阵营=False** | 背板/边框运行时画 |
| NameText | GO 59 / RT 3039 / MB 3824 | anchor(0.0375,0.6318)-(0.4280,0.9340) pos(0,0) size(0,0) | x[729.6,919.6] y[789.6,856.4] (189.9x66.9) | — | `Fire Arrow` fs=**42.35**(b1)/**55.4**(12阵营) 白 H=2 V=512 font=-8244042478085975641 | True | 技能名, 运行时填 | 
| CostPanel | GO 149 / RT 3141 | anchor(0.7409,0.6114)-(0.9609,0.9134) pos(-0.0003,0.6) size(-0.0002,1.2) | x[1071.8,1178.8] y[792.9,861.0] (107.0x68.0) | — | — | True | 费用区(右上) |
| CostText | **GO 61 / RT 3415 / MB 3948** | anchor(0.4436,0)-(0.9687,1.0054) pos(0,0) size(0,0) | x[1119.3,1175.5] y[792.6,861.0] (56.2x68.4) | — | `10` fs=**52.4**(b1)/**57**(12阵营) 白 | True | ⚠权威表引『GO 1040/RT 3338/MB 3859 fs0.34』= **2DCard 模板的费用字**, 不是本面板的（见 §15-2） |
| CostIcon | GO 545 / RT 3236 / MB 4131 | anchor(0,0)-(0.381,1) pos(0,0) size(0,0) | x[1071.8,1112.5] y[792.9,861.0] (40.7x68.0) | sprite=0 | — | **False** | 费用小图标, 运行时 set |
| DescText | GO 1033 / RT 2899 / MB 3737 | anchor(0.0375,0.1264)-(0.8842,0.6114) pos(0.167,-0.5) size(-0.25,1.0) | x[730.0,1141.5] y[861.0,969.3] (411.5x108.3) | — | `Deal 1 damage to a target character target character\n`(结尾含\n) fs=**37.35**(b1)/**45**(12阵营) 白 H=1(左) V=512 font=-8244042478085975641 | True | 技能描述, 运行时填 |
| Lights | GO 1093 / RT 3121 | anchor(0.031,0)-(0.963,0.847) pos(0,0) size(0,0) | x[689.6,1226.9] y[748.2,1023.2] (537.3x275.1) | — | — | True | ⚠**与能量区 Lights 无关**（能量区那组=GO 857/RT 3404, 见 §15-5） |
| LightActing | GO 887 / RT 3000 / MB 4313 | anchor(0,0)-(1,1) pos(0,0) size(0,0) | 同 Lights 区 | sprite=0 | — | b1=True; 12阵营=False | color(1,1,1,**0.392**) 白辉光层 |
| LightAvailable | GO 537 / RT 2771 / MB 4521 | 同上 | 同 | sprite=0 | — | b1=True; 12阵营=False | color(0.9254,1.0,0,**0.392**) 黄绿可用光 |
| LightPressed | GO 1085 / RT 2606 / MB 4225 | 同上 | 同 | sprite=0 | — | b1=True; 12阵营=False | color(0.006,0,1.0,**0.392**) 蓝按定光 |
| TargetsAvailableText | GO 734 / RT 2625 / MB 3763 | anchor(0.158,0.7644)-(0.8445,0.9290) pos(0,0) size(0,0) | x[762.9,1158.6] y[721.5,775.0] (395.7x53.5) | — | `0 available` fs=**56.45**(b1)/**44.65**(12阵营) 白 H=2 V=512 font=3485036404935369831 | b1=True; **12阵营=False** | 可用目标计数, 运行时填 |

要点: 面板为锚点百分比排版（全部子元素 anchor 相对容器, size 归零 = ContentSizeFitter 式自适应）；三层 Light 叠色 alpha 0.392 是同尺寸覆盖辉光（白=acting / 黄绿=available / 蓝=pressed）。

---

## 4. Unit Chat（聊天）全树

父链: Safe area FrontCanvas(2862) → Unit Chat(RT 3087)。根 GO 246 组件: RT 3087 + Transform 1800 + MB 5205(4532068630576665233) + MB 4253(-6509610706117204233, VoiceLinesPopupSelector)。

### 4.1 PlayerChatDisplay（玩家台词气泡, 左上）

| 元素 | GO/RT/MB | raw RT | chain_rect 绝对 | 贴图 | 文字 | active | 备注 |
|---|---|---|---|---|---|---|---|
| PlayerChatDisplay | GO 254 / RT 3038 | anchor(0,0) pivot(0,0) pos(12.61,200.00) size(648.77,236.50) | x[12.6,661.4] y[643.5,880.0] (648.8x236.5) | — | — | True | 气泡位于左侧中上 |
| Mask | GO 610 / RT 2600 | anchor(0.5,0.5) pos(-219.89,5.09) size(144.46,157.46) | x[44.9,189.3] y[677.9,835.4] (144.5x157.5) | — | — | True | 卡图裁剪窗(RectMask) — 卡图 176 被裁进 144.5×157.5 |
| CardImage | GO 295 / RT 2844 / MB 4263 | pos(-2.48,-5.00) size(176,176) | x[26.6,202.6] y[673.6,849.6] (176x176) | sprite=0 运行时 set 卡图 (m_Type=0) | — | True | 聊天气泡里的卡图 |
| Background(气泡底) | **GO 1018 / RT 3516** / Image MB | anchor(0,0) pos(324.39,118.25) size(648.77,236.50) | x[12.6,661.4] y[643.5,880.0] | -6417496503820834197 = 40k_voicelines_radio (**766x280**) → 0.847x | — | True | ⚠权威表『GO 88/RT 2745』= **敌方**的气泡底（见 §15-6） |
| wave | **GO 308 / RT 3240** / MB 4970 | anchor(0.5,0.5) pos(111.11,49.60) size(387.95,62.60) | x[254.1,642.1] y[680.8,743.4] (387.9x62.6) | -8253178407798656119 = 40k_voicelines_radio_wave equalizer（m_Rect 未导出, 仅名）m_Type=0 | — | True | 语音波形条 color(0.6988,0.9811,0.9265,1)≈(0.7,0.98,0.93) 青绿; ⚠权威表把它标为『敌』（见 §15-6）; MB 4970 m_RaycastTarget=0 |
| ChatText | GO 306 / RT 3566 / MB 3894 | anchor(0.2906,0.2582)-(0.9736,0.6036) pos(-1.12,-0.93) size(-2.19,-0.001) | x[201.2,642.1] y[738.2,819.9] (440.9x81.7) | — | `Here goes a chat line` fs=33.0 白 H=1(左) V=512 font=3485036404935369831 | True | 台词, 运行时朗读时打字机显示 |

### 4.2 EnemyChatDisplay（敌方台词气泡, 左侧中下）— 同构镜像

| 元素 | GO/RT/MB | raw RT | chain_rect 绝对 | 备注 |
|---|---|---|---|---|
| EnemyChatDisplay | GO 1102 / RT 3180 | anchor(**0,1**) pivot(0,0) pos(**12.61,-410.00**) size(648.77,236.50) | x[12.6,661.4] y[**173.5,410.0**] | ⚠任务提示『pos 差异 y 0.75 vs -0.94』与原始 JSON 不符: 实为 anchor(0,1) + pos(12.61,-410.00)（见 §15-11） |
| Mask | GO 445 / RT 3408 | pos(-219.89,5.09) size(144.46,157.46) | x[44.9,189.3] y[207.9,365.4] | |
| CardImage | GO 1097 / RT 3169 | pos(-2.48,-5.00) size(176,176) | x[26.6,202.6] y[203.6,379.6] | |
| Background | **GO 88 / RT 2745** | pos(324.39,118.25) size(648.77,236.50) | x[12.6,661.4] y[173.5,410.0] | 40k_voicelines_radio |
| wave | **GO 745 / RT 2980** | pos(111.11,49.60) size(387.95,62.60) | x[254.1,642.1] y[210.8,273.4] | |
| ChatText | GO 1086 / RT 2728 | anchor(0.2906,0.2582)-(0.9736,0.6036) | x[201.2,642.1] y[268.2,349.9] | fs=33.0 H=1 |

### 4.3 ChatPopup（预设台词弹窗, inactive）

| 元素 | GO/RT/MB | raw RT | chain_rect 绝对 | 贴图/文字 | active | 备注 |
|---|---|---|---|---|---|---|
| ChatPopup | GO 991 / RT 2688 | anchor(0,0) pivot(0,0.5) pos(0,479.81) size(815.04,475.47) | x[0.0,815.0] y[362.5,837.9] | — | **False** | 左侧弹窗 815×475 |
| CloseChatPopup | GO 363 / RT 3305 | anchor(0.5,0.5) pos(2.5,41.7) size(4055.3,2114.4) | x[-1617.7,2437.6] y[-498.7,1615.7] 超屏遮挡层 | color(1,1,1,1); MB 4913 脚本 -78715595128852713 挂 **m_Delegates eventID=2 → `VoiceLinesPopupSelector.Hide`(target 4231)**; MB 4211 脚本 -2844744054636863780 (无 sprite) | True | 点击任意处=关闭弹窗（序列化事件!） |
| ChatButtons | GO 658 / RT 3168 | pos(371,206) size(722.66,403.13) | x[9.7,732.3] y[430.4,833.5] (722.7x403.1) | — | True | 纯容器 |
| BG | GO 829 / RT 2667 / MB 5163 | pos(28.8,5.6) size(557.3,301.3) | x[121.2,678.5] y[475.6,777.0] | -3734815765473162916 = White Square (8x8) color(**0,0.07,0,1**) 深绿 | True | 弹窗底色 |
| BGFrame | GO 86 / RT 2976 | pos(2.2,-1.6) size(716.9,399.1) | x[14.8,731.7] y[433.9,833.0] | — | True | 边框容器(四角拼图) |
| BGFrame Top | GO 716 / RT 2966 | pos(16.4,171.3) size(517.7,33.9) | x[130.8,648.4] y[445.2,479.2] | -7266636332286626103 = 40k_UnitChat_Background_Top (590x39) → 0.877x | True | 四边为 40k_UnitChat_Background_{Top,Bottom,Left,Right} 拼图 |
| BGFrame Bottom | GO 119 / RT 2729 | pos(17.1,-153.8) size(517.7,36.5) | x[131.5,649.2] y[769.1,805.6] | 6130416589180386937 = ..._Bottom (590x42) → 0.877x | True | |
| BGFrame Right | GO 666 / RT 3186 | pos(317.1,13.6) size(83.7,371.1) | x[648.5,732.1] y[434.4,805.4] | 556259315706983459 = ..._Right (95x427) → 0.881x | True | |
| BGFrame Left | GO 976 / RT 2602 | pos(-300.2,-5.4) size(117.3,387.0) | x[14.3,131.6] y[445.4,832.4] | 450114720226241101 = ..._Left (134x445) → 0.875x | True | |
| Buttons | GO 406 / RT 3449 / **MB 5105 = VerticalLayoutGroup(-4621643977240678714)** | pos(8,2) size(557.3,296.1) | x[100.3,657.7] y[481.9,778.0] | — | True | padding=0, childAlignment=1(UpperCenter), **spacing=1.48**, ForceExpand W/H=0, Control W/H=0 → 只做纵向堆排，子尺寸保留 |
| ChatButton(0..5) | GO 963(RT 2847) / 940(2861) / 474(3491) / 565(3543) / 279(3274) / 573(3535) | anchor(0,0) pivot(0.5,0.5) pos(0,0) size(603.60,48.00) | 静态全部 x[-201.5,402.1] y[754.0,802.0] **六钮重叠** | 自身 Image sprite=0 color(1,1,1,**0**) | True | ⚠运行时 VerticalLayoutGroup 纵向重排间距 1.48（6条×48+5×1.48≈295.4 恰满 Buttons 296 高） |
| └ bg | GO 43(RT 3294,Image MB 4987) 等 ×6 | pos(23.74,0) size(550.4,44.5) | (按钮内) | 6162082053088366362 = 40k_voicelines_bt_R (264x24) → **2.085x/1.85x 拉伸** m_Type=0 Simple | — | ChatButton 的 Button#1 (MB 4165) m_Transition=2 SpriteSwap targetGraphic=**4987(bg)**（悬停换 bg sprites 用 L_hover 等变体） |
| └ Text (TMP) | GO 108(RT 3558,MB 3888) 等 ×6 | anchor(0,0)-(1,1) pos(26.74,0) size(-74.9,0) | — | `Greetings` fs=30.0 白 H=1(左) V=512 font=3485036404935369831 | — | 六条均为占位 'Greetings', 运行时填 6 条预设台词 |
| └ button | GO 176(RT 2922) 等 ×6 | anchor(0,0.5) pos(23.5,0) size(40,40) | — | -1396804370094666590 = 40k_voicelines_bt_L (85x85) → **0.47x** | — | 左端小圆钮（播放音效图标） |

ChatButton 双 Button 配置（GO 963 为例）: MB 4165(-4448497653027179337, Transition=2 SpriteSwap, target=bg 4987, OnClick 空) + MB 4585(1266098396536617832, Transition=1 ColorTint, target=自身 Image 4738, OnClick 空) + MB 4979 空壳。事件运行时绑定。

### 4.4 RadioChat（无线电聊天, inactive）+ 音频

| 元素 | GO/RT | raw RT | chain_rect 绝对 | 备注 |
|---|---|---|---|---|
| RadioChat | GO 1189 / RT 3244 | anchor(1,1) pivot(**1,1**) pos(-25,-126) size(648.8,236.5) | x[1246.2,1895.0] y[126.0,362.5] | 右上角镜像气泡; inactive; 子节点 Mask(380)/CardImage(1187)/Background(165, 40k_voicelines_radio)/wave(660)/ChatText(1172, fs33) 与 Player 同构 |
| PlayerChatAudioSource | GO 485 / RT 2904 | anchor(0.5,0.5) 100x100 | x[910,1010] y[490,590] | 脚本 MB 4035(7753607692688777346): audioSource=1596, mixerType=2(Voiceover) |
| EnemyChatAudioSource | GO 665 / RT 2671 | 同上 | 同上 | AudioSource 1596→ 1600? (脚本 MB 5232) mixerType=2 |
| RadioChatAudioSource | GO 588 / RT 2775 | 同上 | 同上 | (脚本 MB 4393) |
| AudioSource 1596 | AudioSource_1596.json | — | — | m_PlayOnAwake=False, m_Volume=1.0（全部运行时触发） |

---

## 5. Mulligan（换牌）全树

父链: Safe area FrontCanvas(2862) → Mulligan(RT 3344, stretch 全屏)。根 GO 321 组件: RT 3344 + **MB 4352 = MulliganManager(2586525550486630815)**（按钮 onClick 的 target）。MulliganAnchor GO 131 无 RT（纯 Transform, 手牌区锚）。**ButtonsGroup GO 299 / RT 3069**（stretch 全屏, 仅 RT 无组件）是权威表漏掉的容器。

| 元素 | GO/RT/MB | raw RT | chain_rect 绝对 | 贴图 | 文字 | active | 备注 |
|---|---|---|---|---|---|---|---|
| Mulligan | GO 321 / RT 3344 | stretch 全屏 | 全屏 | — | — | True | |
| MulliganText | GO 319 / RT 2828 | anchor(0.5,1) pos(7,-106.50) size(1344,79.44) | x[295.0,1639.0] y[66.8,146.2] | — | — | True | 顶部提示条 |
| Text(提示) | **GO 274 / RT 3442** / MB 3731 | anchor(0,0)-(1,1) pos(-0.97,**19**) size(-36.94,-10) | x[312.5,1619.6] y[**52.8,122.2**] | — | `Choose cards to replace in first hand` fs=**65.0** 白 H=2 V=512 | True | ⚠权威表引『GO 358/RT 3056』= 设置面板 Voiceover 的 Text（见 §15-9） |
| TurnText(后手) | GO 293 / RT 3403 / MB 3856 | anchor(0,0)-(1,1) pos(-0.97,**-50**) size(-36.94,-25.27) | x[312.5,1619.6] y[**129.4,183.6**] | — | `You go second` fs=**55.0** 白 H=2 V=512 | True | 后手时显示, 先手时运行时隐藏 |
| ButtonsGroup | GO 299 / RT 3069 | stretch 全屏 | 全屏 | — | — | True | 仅 RT（无布局组件, MulliganManager 代码管理） |
| MulliganContinueButton | GO 1195 / RT 3203 | anchor(1,0) pivot(1,0) pos(-24.2,41.0) size(832.9,117.5) | x[1062.9,1895.8] y[921.5,1039.0] (832.9x117.5) | — | — | True | 右下大按钮组 |
| └ Button(绿条) | GO 121 / RT 2659 / Image MB 5292 | pos(132.1,0) size(577.5,63.8) | x[1322.7,1900.2] y[948.3,1012.2] | 5480819196723731418 = 40k_bt_underbutton (485x83, border 240,0,240,0) m_Type=**0 Simple** color(**0.3686,0.8941,0.5874,1**) | — | True | OnClick → **MulliganManager.ClickMulliganDone**（MB 4662, 序列化!）; 另有 EverguildButton MB 4025 |
| └ Text | GO 1116 / RT 3160 / MB 3838 | pos(45.7,-0.8) size(368.9,62.2) | x[1340.6,1709.5] y[949.9,1012.2] | — | `Continue` fs=**45.0** 白 H=**4(右)** V=512 | True | H=4=右对齐 |
| └ CircleButton | GO 533 / RT 2630 / MB 5212 | pos(284.1,0) size(80.5,79.6) | x[1723.2,1803.7] y[940.4,1020.1] | 9056225148749506697 = 40k_UI_bt_play (128x128) → 0.629x | — | True | 圆形播放钮; Button MB 4419(1515810523724505295, Transition=2) |
| HideMulliganButton | GO 1106 / RT 3426 / Image MB 5127 | anchor(0,0) pos(143.0,131.7) size(82.9,79.6) | x[101.5,184.5] y[908.5,988.1] | -7255197835733746773 = 40k_UI_bt_eye (256x256) → 0.324x | — | True | 左下眼睛钮; OnClick → **MulliganManager.ToggleMulliganVisibility**（MB 4144）; Button MB 4464(EverguildButton Transition=1) |

---

## 6. ChooseCardMenu（选卡菜单, 场景 inactive; blacklegion=active）

父链: Safe area FrontCanvas(2862) → ChooseCardMenu(RT 3273, stretch)。根 GO 322 组件: RT 3273 + **MB 4353 = ChooseCardMenu(1054633627138450046)**。ChooseCardMenuAnchor GO 57 无 RT。ButtonsGroup **GO 560 / RT 3448**（注意与 Mulligan 的 299/3069 不同 GO）; 权威表漏列。

| 元素 | GO/RT/MB | raw RT | chain_rect 绝对 | 贴图 | 文字 | active | 备注 |
|---|---|---|---|---|---|---|---|
| ChooseCardMenu | GO 322 / RT 3273 | stretch 全屏 | 全屏 | — | — | b1/b2/b3/10阵营 = False; **blacklegion = True** | |
| ButtonsGroup | GO 560 / RT 3448 | stretch 全屏 | 全屏 | — | — | True | |
| HideChooseButton | GO 70 / RT 3051 / Image MB 4921 | anchor(0,0) pos(143.4,133.17) size(78.44,78.44) | x[104.2,182.6] y[907.6,986.1] | 40k_UI_bt_eye (256x256) → 0.306x, m_PreserveAspect=1 | — | True | OnClick → **ChooseCardMenu.ToogleChooseMenuVisibility**（MB 4813, 方法名拼写即原版如此 ‘Toogle’）; Button MB 4387(EverguildButton) |
| Continue Button | GO 779 / RT 3196 | anchor(1,0) pos(-396.4,105.0) size(548.02,75.90) | x[1249.6,1797.6] y[937.0,1013.0] | 自身 Image sprite=0 color(1,1,1,0); Button MB 4897(EverguildButton) + MB 4262(-4448497653027179337, Transition=1, target=BG 4210) | — | True | |
| └ BG | GO 1153 / RT 3288 / Image MB 4210 | pos(65.2,-5.7) size(577.5,63.8) | x[1300.0,1877.5] y[948.8,1012.7] | 40k_bt_underbutton m_Type=0 color(0.3686,0.8941,0.5874,1) | — | True | 绿色横条 |
| └ ContinueText | **GO 444 / RT 2741 / MB 3994** | pos(-4.6,-5.3) size(372.9,43.6) | x[1332.6,1705.4] y[958.5,1002.1] | — | `Continue` fs=**45.0** 白 H=4(右) | True | ⚠权威表『GO 1164/RT 3527 fs4.5』= TutorialTip 里的 (见 §15-8) |
| └ ContinueButton | GO 926 / RT 3324 / MB 5075 | pos(240.3,-3.4) size(80.5,79.6) | x[1723.7,1804.1] y[938.6,1018.2] | 40k_UI_bt_play (128x128) → 0.629x | — | True | OnClick → **ChooseCardMenu.ClickChooseDone**（MB 4565）; Button MB 4996 空壳 |
| ChooseText | GO 709 / RT 3465 | anchor(0.5,1) pos(7,-106.5) size(1344,79.44) | x[295.0,1639.0] y[66.8,146.2] | 自身 Image sprite=0 color(**1,0.4941,0.9457,1**) 粉紫 | — | True | 顶部提示条(粉紫底色, blacklegion 可见) |
| └ Text | **GO 1019 / RT 2795** / MB 3728 | anchor(0,0)-(1,1) pos(-0.97,0) size(-36.94,0) | x[312.5,1619.6] y[66.8,146.2] | — | `Choose one card` fs=**55.0** 白 H=2 V=512 font=3485036404935369831 | True | ⚠权威表『GO 1045/RT 2832』= Card Picker 下拉框的 (见 §15-7) |

---

## 7. AboveShader（顶层导语/Overtime）

父链: Safe area FrontCanvas(2862) → AboveShader(RT 3055, stretch)。根 GO 682 仅 RT（无脚本）。

| 元素 | GO/RT/MB | raw RT | chain_rect 绝对 | 贴图 | 文字 | active | 备注 |
|---|---|---|---|---|---|---|---|
| AboveShader | GO 682 / RT 3055 | stretch 全屏 | 全屏 | — | — | True | 空容器 |
| TipText(阻挡提示) | GO 692 / RT 2672 | anchor(0.5,0.5) pos(1.1,91.8) size(2450.1,99.7) | x[-264.0,2186.1] y[398.4,498.1] 超屏宽条 | 40k_bt_underbutton m_Type=0 color(**0.3686,0.8941,0.5874,0.6**) 60% 绿 | — | **False** | 中央偏上横幅; 运行时由 BattleTipController 显示 '必须攻击带阻挡的随从' 类提示 |
| └ Text | GO 417 / RT 2937 / MB 3813 | anchor(0.282,0)-(0.719,1) pos(0,0) size(0,0) | x[427.6,1497.0] y[398.4,498.1] (1069.4x99.7) | — | `You must attack the minion with block` fs=**60.0** 白 H=2 V=512 font=3485036404935369831 | True | 占位文本（原版通用导语样式） |
| OvertimeSplashText | GO 390 / RT 2829 | stretch 全屏 | 全屏 | — | — | **False** | OVERTIME! 字幕组 |
| └ Background | GO 601 / RT 2748 / MB 4518 | anchor(0,0)-(1,1) pos(0,0) sizeDelta(716.6,699.9) | x[-358.3,2278.3] y[-349.9,1429.9] (**实际 2636.6x1779.9**, 尺寸向外扩出屏幕) | sprite=0 color(**0,0,0,0.533**) | — | True | 中央暗化块（拉伸锚点+大 sizeDelta 的写法, 不是 716x700!） |
| └ Text Container | GO 558 / RT 3201 / MB 4786 | anchor(0,0)-(1,1) pos(0,0) size(2,-981) | x[-1.0,1921.0] y[490.5,589.5] (**1922x99**) | -9022563678303461981 = 40k_dsplay_overtime (5x198 竖条) | — | True | 全宽 99 高垂直中心横条（OVERTIME 文字跑马灯容器） |
| └ Text | GO 761 / RT 2724 / MB 3973 | anchor(0.5,0)-(0.5,1) pos(0,0) size(0,0.7) | x[960.0,960.0] y[490.2,589.8] 宽=0 | — | `OVERTIME!` fs=**80.0** 白 H=**32(Geometry)** V=**8192(Capline)** font=-8244042478085975641 | True | ⚠权威表称『场景未保留 fontSize』→ 实际 80（见 §15-10）; Geometry/Capline = 竖条内自适应排布 |
| └ Image(计时图标) | GO 743 / RT 3093 | anchor(0,0.5) pivot(1,0.5) pos(-15,0) size(181.3,187.3) | x[763.7,945.0] y[446.4,633.6] (181.3x187.3) | 7461627760614701739 = 40k_icon_overtime (174x180) → 1.04x | — | True | 文字左侧计时图标 |

---

## 8. EffectList（卡上效果列表, inactive; 教程节点）

父链: ... → TutorialObjs(属于 Card Display Window 的 Cards 区) → EffectList (GO 253, 无 RT, 纯 Transform 1433; 组件 3573=Transform)。子: AffectedBy(237), Elements(570), CreatedBy(561, inactive)。**尺寸/字号均为卡相对单位**（卡宽≈2.09 unit, 运行时由 2DCard 脚本换算实 px), 场景值不可直接当像素。

| 元素 | GO/RT/MB | raw RT (卡单位) | 贴图 | 文字 | active | 备注 |
|---|---|---|---|---|---|---|
| AffectedBy | GO 237 / RT 3332 / MB 3908 | pos(0.2,0.1) size(4.4,0.5) | — | `Affected by:` fs=0.4 白 H=1 | True | 父 inactive |
| Elements | GO 570 / RT 3043 | pos(0.4,-3.4) size(5.0,6.4) | — | — | True | 效果条目容器 |
| EffectElement ×5 | GO 1032(RT 2912) / 89(RT 3114) / 668(RT 2643) / 352(RT 3335) / 92(RT 3107) | 每条 pos(2.5,-{0.7,2.0,3.3,4.6,5.8}) size(5.0,1.4) anchor(0,1) | — | — | True | 第一/三条含 EffectText, 其余条仅为 EnchanterText 占位（模板差异: 1/2/3/4 条 EnchanterText+EffectText, 1/4 条无 EffectText…按 dump: (0)全 (1)无EffectText (2)全 (3)全 (4)全）详见 dump |
| └ EffectBg | GO 512(RT 2622) 等 | pos(0,0) size(9.8,3.0) | -3055422674097938669 = 40K_display (442x112) | — | True | 效果底色条 |
| └ EnchanterText | GO 582(RT 2721,MB 3750) 等 | pos(0,0.3) size(4.3,0.3) | — | `Blind Librarian` fs=0.3 白 H=1 | True | 施效者名 |
| └ EffectText | GO 220(RT 3204,MB 4006) 等 | pos(0,-0.2) size(4.3,0.6) | — | `Get {0} Melee Attack, {1} Ranged Attack and {2} Health` fs=0.3 白 H=1 | True | {0}{1}{2} 占位, 运行时填数值 |
| └ line | GO 557(RT 2675) 等 | pos(-0.7,0) size(4.1,0.1) | -8906274027440120365 = traitline (344x5) | — | **False** | 分隔线 inactive |
| CreatedBy | GO 561 / RT 3308 / MB 3714 | pos(0,3.0) size(3.4,0.3) | — | `Created by someone` fs=2.9 白 H=2 | **False** | |

（EffectList 全部 13 场景均 inactive, 内容一致。）

---

## 9. TooltipManager / PopupAnchor / ShadePanel / 错误横幅

| 元素 | GO/RT/MB | raw / 说明 |
|---|---|---|
| TooltipManager | GO 459 / RT 2890 / MB 4863(脚本 -4892667331910005099) | anchor(0.5,0.5) 100x100, father=RT 2900(FrontCanvas 直子, **不在 Safe area 下**); 提示框运行时实例化到该锚 |
| PopupAnchor | GO 920 / RT 2686 / MB 4220(脚本 -6780635296184661163) + **MB 4425 = GraphicRaycaster** | anchor(0.5,0.5) 100x100; chain x[910,1010] y[490,590]; 通用弹窗生成锚; 自带 raycaster |
| ShadePanel | GO 763(RT 2952, **inactive**) / Image MB 4215 + MB 4399(脚本 -6694942994047174594) | father=RT 2900(FrontCanvas 直子); RT 2952 stretch 全屏 pos(0,0) size(0,0); Image sprite=0 color(0,0,0,**0**) m_RaycastTarget=0; MB 4399 字段: shade=0, shadeUI=4215, **onAlphaLevel=0.8, defaultTimeToSwitch=0.5** → ShadePanel 控制器（0.5s 渐暗到 α0.8） |
| UI Error Message Controller (MUST BE ENABLED) | GO 481 / RT 3373 | anchor(0.5,1) pivot(0.5,1) pos(0,-213.6) size(1920.70,336.64); **father=RT 2759(主 Canvas 直子, 不在 Front/Back 层)** |
| └ Container | GO 578 / RT 3154 | anchor(0,0)-(1,0) pos(0,133.15) size(0,266.29) |
| └ Error Mensage_Ref | GO 632 / RT 2823 | anchor(0,1) pos(960.35,-241.29) size(1439.16,80) — **模板, inactive, 报错时实例化** |
| └ Background | GO 477 / RT 2989 / MB 5101 | pos(0,0) size(870.98,60.85); 40k_bt_underbutton (485x83 border 240,0,240,0) **m_Type=1(Sliced)** color(0.3585,0.3585,0.3585,1) 灰 |
| └ ErrorMesage Text | GO 1099 / RT 2942 / MB 3740 | anchor(0,1) pos(435.49,-32.42) size(630.98,56.85); `ERROR MESSAGE CONTENT` fs=**60.0** color(**0.8019,0,0,1**) 暗红 H=2 V=512 |
| | | 注意: 同用 40k_bt_underbutton 但**错误条=m_Type 1 (9-slice), Mulligan/选卡/TipText=m_Type 0 (Simple 拉伸)** |

---

## 10. 13 场景 active 状态差异表（原版设计差异）

| 节点 | battlearena1 | battlearena2 | battlearena3 | aeldari | astra | **blacklegion** | darkangels | EC | genestealers | leviathan | sororitas | spacewolves | tauviorla |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| **ActiveSkillDesc 根** | **False** | True | True | True | True | True | True | True | True | True | True | True | True |
| ├ AbilityContainer | True | False | False | False | False | False | False | False | False | False | False | False | False |
| ├ TargetsAvailableText | True | False | False | False | False | False | False | False | False | False | False | False | False |
| ├ LightActing/LightAvailable/LightPressed | True | False | False | False | False | False | False | False | False | False | False | False | False |
| ├ HideAbilityButton / Lights 根 / NameText / CostPanel / CostText / DescText | True | True | True | True | True | True | True | True | True | True | True | True | True |
| ├ CostIcon | False | False | False | False | False | False | False | False | False | False | False | False | False |
| **ChooseCardMenu 根** | False | False | False | False | False | **True** | False | False | False | False | False | False | False |
| ├ ButtonsGroup / ChooseText / HideChooseButton / Continue Button 子树 | True（全部） | 同 | 同 | 同 | 同 | 同 | 同 | 同 | 同 | 同 | 同 | 同 | 同 |
| Mulligan 根及其全文 | True（全部 13 场景一致） | | | | | | | | | | | | |
| Drag Attack Selector: 三钮+Highlight | True; Select Attack Background=False（全部一致） | | | | | | | | | | | | |
| Unit Chat: 两气泡+AudioSources=True; ChatPopup / RadioChat=False（全部一致） | | | | | | | | | | | | | |
| AboveShader 根=True; TipText / OvertimeSplashText=False（全部一致） | | | | | | | | | | | | | |
| EffectList=False（全部; 无 RT 纯 Transform） | | | | | | | | | | | | | |

**解读**: 
(a) ActiveSkillDesc 是唯一"base 场景 inactive、12 阵营场景 active"的根面板; 但其内容子层(AbilityContainer/三 Light/TargetsAvailableText)在 12 阵营场景里反而 inactive、在 b1 里 active——两组场景互为镜像的"编辑残留态"。运行时态由代码接管（战斗开始时按选中单位显示）。**复刻建议: 以 b1 的结构+12 阵营的字号做运行态 = 完整可见状态**（内容子层全开）。
(b) ChooseCardMenu 仅 blacklegion 场景 active（红卡/黑军团专属选卡机制场景默认可见, 运行时关闭）。
(c) 其余全部节点 13 场景完全一致。

## 11. 12 阵营场景 vs battlearena1 字号差异（ActiveSkillDesc 专属）

| 文本 | battlearena1 | 12 阵营场景（blacklegion 实测+9 场景核对, 全部一致） | 备注 |
|---|---|---|---|
| NameText 'Fire Arrow' | fs 42.35 | fs **55.4** | 其余字段完全一致（白/H2/V512/同 fontAsset）; **12 阵营场景全部逐场核对过（11 场 regex+blacklegion 全文 dump）** |
| CostText '10' | fs 52.4 | fs **57.0** | |
| DescText | fs 37.35 | fs **45.0** | 文本含末尾 `\n` |
| TargetsAvailableText '0 available' | fs 56.45 | fs **44.65** | |
| Mulligan Text/TurnText/Continue/Choose 文本/聊天/Errors | 全部一致（65/55/45/55/33/30/60） | 同 | 逐场景核对无差异 |

即: 只有 ActiveSkillDesc 四个文本的字号在两族场景间不同——**这 4 个字号都是场景占位值, 运行时由 ActiveSkillDesc 脚本按实际技能数据重设**；Godot 复刻应实现运行时字号（模板值用 12 阵营组 55.4/57/45/44.65 更接近成品观感, 但最终以运行时为准）。

## 12. 序列化事件接线表（m_OnClick / m_Delegates 实证, 非"全空"）

| 组件 | 脚本 type | 事件 | 目标 |
|---|---|---|---|
| Mulligan Button (GO 121, MB 4662, script -4448497653027179337) | UnityEngine.UI.Button 类 | ClickMulliganDone | MulliganManager (4352) |
| HideMulliganButton (MB 4144) | 同上 | ToggleMulliganVisibility | MulliganManager (4352) |
| HideChooseButton (MB 4813) | 同上 | ToogleChooseMenuVisibility（原版拼写即 Toogle） | ChooseCardMenu (4353) |
| ChooseCardMenu ContinueButton (MB 4565) | 同上 | ClickChooseDone | ChooseCardMenu (4353) |
| CloseChatPopup (GO 363, MB 4913, script -78715595128852713) | EventSystem m_Delegates **eventID=2(pointerClick)** | Hide | VoiceLinesPopupSelector (4231) |
| 攻击三钮 / ChatButton×6 / CircleButton / SettingsBtn / TurnBtn | EverguildButton(1015376240363272691) 等 | OnClick=空 → 运行时绑定 | （权威表"全部为空"仅对这批成立） |

Button 类脚本 ID: **-4448497653027179337**=标准 Button 类(带 m_OnClick 序列化), **1015376240363272691**=EverguildButton, 1266098396536617832=聊天按钮第二 Button 类, 1515810523724505295=CircleButton 类, -9096086197743689236=空壳(仅 m_Enabled/m_Name), 6466633716071143260=攻击钮附加脚本类, 350208831926335389=UnityEngine.UI.Image。

## 13. 重要脚本 ID 速查

| scriptID | 挂载点 | 推断类名 |
|---|---|---|
| 2586525550486630815 | Mulligan(321) | MulliganManager |
| 1054633627138450046 | ChooseCardMenu(322) | ChooseCardMenu |
| 5546416171644318052 | ActiveSkillDesc(673) | ActiveSkillDesc |
| 4532068630576665233 / -6509610706117204233 | Unit Chat(246) | ChatController / VoiceLinesPopupSelector |
| -5378975638431045206 / -2470792663603520348 / -3229211799126679632 | Drag Attack Selector(172) | 拖拽攻击选择 3 脚本 |
| -78715595128852713 | CloseChatPopup(363) / CemeterySliderUI 共用 | VoiceLinesPopupSelector 事件适配/UI 点击类 |
| -4892667331910005099 | TooltipManager(459) | Tooltip 系统 |
| -6780635296184661163 | PopupAnchor(920) | 弹窗生成器 |
| -6694942994047174594 | ShadePanel(763) | ShadePanel 控制器（onAlphaLevel=0.8, 0.5s） |
| 3762903421402112529 | FrontCanvas raycaster / PopupAnchor(4425) / DragAttack(4391) | GraphicRaycaster |
| -4621643977240678714 | ChatButtons→Buttons(406) | VerticalLayoutGroup |
| -6857224376968460714 | Highlight×3 | 高亮动画 |

## 14. Sprite m_Rect 原尺寸表（本层用到的）

| 名 (PathID) | m_Rect | border | 显示尺寸(该层) | 缩放/用法 |
|---|---|---|---|---|
| Attack type button Melee (8511722742595327015) | 154x154 | 0 | 118.4x118.8 | 0.769x, m_Type=0 |
| Attack type button Ranged (5998267373089430251) | 154x154 | 0 | 同 | 同 |
| Attack type button highlight (-7315808829623560242) | 227x227 | 0 | 176.2x176.9 | 0.776x |
| 40k_voicelines_radio (-6417496503820834197) | 766x280 | 0 | 648.8x236.5 | 0.847x |
| 40k_voicelines_bt_R (6162082053088366362) | **264x24** | 0 | 550.4x44.5 | **2.08x/1.85x 拉伸** m_Type=0 |
| 40k_voicelines_bt_L (-1396804370094666590) | 85x85 | 0 | 40x40 | 0.47x |
| 40k_voicelines_radio_wave equalizer (-8253178407798656119) | **未找到 m_Rect**（仅 ui_extract 有 名+pathid, battleatlasui 无此 Sprite JSON） | — | 387.9x62.6 | 运行色 (0.699,0.981,0.927) |
| 40k_UnitChat_Background_Top (-7266636332286626103) | 590x39 | 0 | 517.7x33.9 | 0.877x |
| 40k_UnitChat_Background_Bottom (6130416589180386937) | 590x42 | 0 | 517.7x36.5 | 0.877x |
| 40k_UnitChat_Background_Left (450114720226241101) | 134x445 | 0 | 117.3x387.0 | 0.875x |
| 40k_UnitChat_Background_Right (556259315706983459) | 95x427 | 0 | 83.7x371.1 | 0.881x |
| 40k_bt_underbutton (5480819196723731418) | 485x83 | (240,0,240,0) | 错误条 870.98x60.85 / Mulligan 577.5x63.8 / TipText 2450.1x99.7 | **错误条 m_Type=1(Sliced); 其余 m_Type=0(Simple)** — 同一贴图两种用法 |
| Background (1660267235368898380, 内置资源) | 32x32 | (10,10,10,10) | HideAbilityButton 3708x1985 | m_Type=1(Sliced), color α=0 透明点击层 |
| 40k_UI_bt_eye (-7255197835733746773) | 256x256 | 0 | 78.4x78.4 / 82.9x79.6 | 0.306x/0.324x, m_PreserveAspect=1 |
| 40k_UI_bt_play (9056225148749506697) | 128x128 | 0 | 80.5x79.6 | 0.629x |
| 40k_dsplay_overtime (-9022563678303461981) | 5x198 | 0 | RT size(2,-981) → 实际 1922x99 | 竖条纹理, 全宽横条渲染 |
| 40k_icon_overtime (7461627760614701739) | 174x180 | 0 | 181.3x187.3 | 1.04x |
| 40K_display (-3055422674097938669) | 442x112 | 0 | EffectBg 9.8x3.0(卡单位) | 卡相对 |
| White Square (-3734815765473162916) | 8x8 | 0 | ChatPopup BG 557.3x301.3 | 拉伸纯色, color(0,0.07,0,1) |
| traitline (-8906274027440120365) | 344x5 | 0 | 4.1x0.1(卡单位) | 卡相对, inactive |

## 15. 与《战斗界面JSON权威表_0827.md》矛盾清单（以原始 JSON 为准）

1. **B 节能量区 "Lights (GO 1093, RT 3121)" → 张冠李戴**: GO 1093/RT 3121 = **ActiveSkillDesc 的 Lights**（子=LightActing 887/LightAvailable 537/LightPressed 1085）。能量区气泡灯组实为 **GO 857 / RT 3404**（子=Glow Green Eye 3018 / Glow Green 2 3026 / Glow Orange×4 2718/2638/3381/3085, sprite=-8118772561931967345 Glow UI W40K）。
2. **G 节 CostText**: 权威表 'GO 1040 / RT 3338 / MB 3859, size(0.456,0.42), '1'+上标 fs0.34' = **2DCard 卡模板的费率字**（卡费容器下），不是 ActiveSkillDesc.CostPanel 下的。本面板的 = **GO 61 / RT 3415 / MB 3948, '10' fs 52.4(12阵营 57)**。
3. **Mulligan 子树缺节**: 权威表 I 节无 ButtonsGroup (GO 299/RT 3069)、MulliganContinueButton (GO 1195/RT 3203) 及其 Button/Text/CircleButton 子树、HideMulliganButton (GO 1106/RT 3426)。Mulligan 的 'Text GO 358/RT 3056' = 设置面板 Voiceover 标签（father RT 3297）;**正确 = GO 274 / RT 3442 / MB 3731**。
4. **I 节选卡 'Text(选卡提示) GO 1045 / RT 2832 / MB 3728'** → 正确 = **GO 1019 / RT 2795 / MB 3728**（fs55 H2）; GO 1045/RT 2832 = 调试 Card Picker 下拉 MainButton 的 Text（pos(10,0) size(-14,0)）。
5. **I 节 'ContinueText GO 1164 / RT 3527 / MB 3722 fs4.5 占位'** 属 **TutorialTip**（father RT 2810=GO 1158 TutorialTip）; 选卡 Continue 的文字 = **GO 444 / RT 2741 / MB 3994, 'Continue' fs 45**。
6. **H 节聊天气泡 GO 互换**: 玩家气泡底 = GO **1018**/RT **3516**（权威表写 88/2745=敌方）; 敌方 = GO **88**/RT **2745**。玩家 wave = GO **308**/RT **3240**（权威表把它写作"敌波形条"）; 敌方 wave = GO **745**/RT **2980**。（位置/贴图值本身两处相同, 错的是归属。）
7. **K 节 'OVERTIME! fontSize 场景未保留'** → 实际 **fs=80.0**（MB 3973, H=32 Geometry, V=8192 Capline）。
8. **层级节**: 权威表把 ShadePanel、TooltipManager 视为 Safe area FrontCanvas 子级 → 实际两者 father=**RT 2900（FrontCanvas 直子）**, Safe area 下是 14 个功能模块。权威表对 Error 控制器"主 Canvas 直子"的标注是正确的。
9. **§二 '各按钮 m_OnClick 均空'** → 不成立: Mulligan Button→`MulliganManager.ClickMulliganDone`、HideMulliganButton→`ToggleMulliganVisibility`、HideChooseButton→`ChooseCardMenu.ToogleChooseMenuVisibility`、ContinueButton(选卡)→`ChooseCardMenu.ClickChooseDone`、CloseChatPopup→(EventSystem delegate eventID=2) `VoiceLinesPopupSelector.Hide`。TurnBtn/SettingsBtn/攻击三钮/ChatButton×6 的 OnClick 确为空（运行时绑定）。
10. **FrontCanvas overrideSorting**: 权威表已正确记 True + sortingOrder 0; 补充 m_SortingLayerID=2007638115、m_Camera=1462、m_PlaneDistance=100（权威表未列）。
11. **任务提示里 'EnemyChatDisplay anchor(0,1) pos 差异 y 0.75 vs -0.94'** — 原始 JSON 无 0.75/-0.94: 实为 anchor(0,1) pos(12.61,**-410.00**) size(648.77,236.50)（chain y[173.5,410.0]）。该提示作废, 以本表为准。
12. **ValueText 父级**: 权威表 F 节未注明 → 它挂在 **Select Active Skill Button (GO 760)** 下（anchor(0,1) pos(59.64,-60.61) 相对按钮左上）。
13. **Mulligan Color/绿条的 m_Type**: 权威表把 40k_bt_underbutton 一律记为 9-slice border 240 → 实际 Mulligan 绿条/选卡 BG/TipText 均为 **m_Type=0 Simple**（拉伸）, 仅错误横幅=**m_Type=1 Sliced**。
14. **ChatButton(4) 的全树 dump 漏 bg 行**（dump 显示仅 Text+button）→ 原始 JSON 六钮全有 bg（RT 3274 子=bg/Text (TMP)/button）; 全树 dump 有 1 处缺行, 不得据此复刻。
15. 50w: 权威表 '16 条 Sprite 未定位' 中 `40k_voicelines_radio_wave equalizer` 现在找到 ui_extract/scenes_scenes_battlearena1_sprites/Sprite/40k_voicelines_radio_wave equalizer.json（但仅名+pathid, **m_Rect 仍缺**）; 其余未定位项不变。

## 16. 重建最难踩的坑（按重要度）

1. **双层 Canvas**: 主 Canvas 与 FrontCanvas 都 Screen Space Camera(UI Camera, plane 100); FrontCanvas 必须 overrideSorting + 独立 sortingLayer(2007638115) 才浮在 BackCanvas(Overlay) 之上。Godot: Front 层用独立 CanvasLayer + 相机空间。ShadePanel/TooltipManager 挂在 FrontCanvas 根（不是 Safe area 子级）——层级树错了遮挡关系就错。
2. **三钮+三高亮 + 六 ChatButton + 四条文本全部静态重叠**: 攻击三钮 anchor(0,0) pos(0,0) 同一坐标运行时切换; 六 ChatButton 全叠在 x[-201.5,402.1] y[754,802], 靠 VerticalLayoutGroup(spacing=1.48, alignment=UpperCenter, control/expand=0) 纵向重排——**不能照抄单钮坐标**; Mulligan 的"眼/绿条/圆钮"则是有实坐标的。
3. **同一贴图两种 m_Type**: 40k_bt_underbutton 在错误横幅=Sliced(9-slice), 在 Mulligan/选卡/TipText=Simple(拉伸)。Godot 一个 texture 两种 stretch_mode。
4. **ActiveSkillDesc 是 100% 锚点百分比排版**（子元素 anchor 相对容器 + sizeDelta≈0 自适应）——用绝对像素照搬 b1 会错; 面板 576.4×324.8 在右下, NameText 上 37%-93%、DescText 13%-61%、CostPanel 61%-91%、三 Light 全盖容器、TargetsAvailableText 底部 76%-93%。运行时文字/图标全部动态重填, 场景字仅为占位。
5. **m_Type/尺寸单位混用**: EffectList 全部尺寸/字号是卡相对单位(0.3~9.8), 与上面板绝对像素完全不是一套换算; CostText/ValueText 的 sizeDelta=0 或高度=0 = 运行时自适应(TextMeshPro auto-size / 内容撑高), 复刻用自动尺寸。
6. **字号别统一**: 65(换牌提示)/55(后手+选一)/45(两个 Continue)/60(阻挡提示+错误条)/33(聊天)/30(预设句)/31(END TURN)/80(OVERTIME)/89(攻击数值——最大号)/37-57(技能面板 4 档)。且 ActiveSkillDesc 4 档字在 b1 与 12 阵营场景间不同(42.35↔55.4 等)。
7. **按钮组件是双份**: 每个交互钮 = EverguildButton(1015376240363272691, ColorTint) + 标准 Button(-4448497653027179337 或专用脚本类) 并列; ChatButton 1 号钮=SpriteSwap(target=bg Image 4987)。悬停态的 sprite 变体(bg → 40k_voicelines_bt_R_hover 等, 见 04 图集目录)存在, 复刻悬停反馈照此。
8. **透明遮挡层**: HideAbilityButton(Background 32x32 sliced, α0)、Continue Button 自身 Image α0、ChatButton 自身 Image α0——点击区与视觉分离; 还有 CloseChatPopup 4055×2114 隐形层+事件委托关闭。漏任一=点击穿透。
9. **错误横幅不在 FrontCanvas**(主 Canvas 直子, anchor(0.5,1) pos(0,-213.6)), 9-slice 灰底 870.98×60.85 暗红字 60fs——配色(0.3585 灰/0.8019,0,0 暗红)照原版, 别改白。
10. **字形差异**: 全部文本 TextMeshPro: fontAsset 3485036404935369831(多数) vs -8244042478085975641(Fire Arrow/Desc/END TURN/OVERTIME=第二个字形); H 对齐多数=Center(2), 两处 Left(Greetings=1/ChatText=1/DescText=1), 两处 Right(Continue=4), OVERTIME=Geometry(32)+Capline(8192)。
11. **wave 波形条原贴图 m_Rect 缺失**(图集内未导出), 只有名+pathid——重建时若做波形动画, 需从其他渠道(同 bundle 的 Sprite/Texture 或另查 atlas)取像素, 或按显示尺寸 387.9×62.6 + 青绿色 (0.699,0.981,0.927) 用图集内同名贴图, 不得自绘近似。
12. **音频**: 三个 ChatAudioSource 节点皆空壳(PlayOnAwake=False, Volume=1, mixerType=2 Voiceover), 无序列化 clip——所有语音在运行时由 VoiceLinesPopupSelector/ChatController 加载, 复刻需实现该管线而非场景静态音频。

## 17. 依据文件清单（全部原始 JSON, 均可溯源）

- 场景: `D:/2/解包整理/07_场景/battlearena1/{GameObject,RectTransform,MonoBehaviour,Canvas,Transform,AudioSource}/*.json`（本报告所有 PathID 均指向这些文件）
- 场景对照(active 差异/字号): 同上目录于 battlearena2/3/aeldari/astramilitarum/blacklegion/darkangels/emperorschildren/genestealers/leviathan/sororitas/spacewolves/tauviorla
- Sprite m_Rect: `D:/2/解包整理/03_界面UI/图集/battleatlasui/Sprite/<名>_<pid>.json`; `D:/2/解包整理/12_主程序资源/Sprite/<名>.json`; `D:/2/解包整理/12_主程序资源/内置资源/Sprite/Background_1660267235368898380.json`
- Sprite 名映射: `D:/2/Warpforge_tools/data/ui_extract/battlearena1_sprite_map.json`（96 条）；wave 仅有名: `Warpforge_tools/data/ui_extract/scenes_scenes_battlearena1_sprites/Sprite/40k_voicelines_radio_wave equalizer.json`
- 说明书: `D:/2/解包整理/说明书/01_战斗_对战/{README.md,战斗HUD说明.md,教程流程.md,battlearena1_2D层全树.md}`（battlearena1_2D层全树.md 与 2D层_battlearena1全树.md 逐字节相同; 其坐标=局部表达仅参考）
- 工具: `dump_go_tree.py`（按 PathID 走子树; **按名找根有 bug: 无后缀主文件 pid=None 被索引, 若其为 pids[0] 会返回 None → 应用 PathID**）、`chain_rect.py`
- 对照对象: `D:/2/战斗重建_0827/战斗界面JSON权威表_0827.md`
