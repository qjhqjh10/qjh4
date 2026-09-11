# 子代理读报 — FrontCanvas 弹层/展示层权威规格 (battlearena1)

> 读取日期: 2026-08-27 | 读取范围: FrontCanvas 弹层/展示层 10 组根 (Card Display Window / Generic Multi Card Display Combat / BattleSettingsPanel / Tutorial / Alliance Panel / BattleDoors / LoadingMenu / DebugButtons / Tutorial highlight / Anim Anchors)
> 数据源: `D:/2/解包整理/07_场景/battlearena1/{GameObject,RectTransform,MonoBehaviour,Canvas}/` 原始 Unity 序列化 JSON(权威) + `Warpforge_tools/data/ui_extract/battlearena1_sprite_map.json`(pid→精灵名,96 条) + `解包整理/{03_界面UI/图集/battleatlasui/Sprite,12_主程序资源/Sprite,12_主程序资源/内置资源/Sprite,03_界面UI/{光标,去重资源,军队图标}/Sprite,02_装饰品/头像/Sprite}`(m_Rect/m_Border)
> 工具: dump_go_tree.py(dump 口径, 数值四舍五入到 0.1、精灵名跨 bundle 解析不可靠) / battle_front_extract.py(本报告新建, raw 口径: 原始值不四舍五入、精灵名优先 battlearena1 专用 map) / chain_rect.py(chain_rect 口径: Godot 1920×1080 y 向下绝对坐标)
> **三口径纪律**: ① dump 有 2 处已知误标(见矛盾清单 #9/#17), 本报告一律以 raw JSON 为准; ② chain_rect 绝对坐标只对"父链完整"的节点有效, 单位化(CardUI/2DCard)节点换算无意义 → 标"单位尺寸, 运行时缩放"; ③ 双份 pid 文件(`Name_pid_pid.json`)与单份冲突时, 单份(场景实例)优先。

分辨率权威(与权威表一致): 主 Canvas CanvasScaler MB 4731 m_ReferenceResolution=(1920,1080), m_UiScaleMode=1(Scale With Screen Size), m_MatchWidthOrHeight=0(匹配宽度)。全部绝对坐标 = Godot 1920×1080 y 向下。

---

## 〇、挂接点与层级(全部原始)

```
BattleHud (GO 239)
└─ Canvas (GO 983, RT 2759)  ← 主 Canvas, Canvas_2572: renderMode=1(ScreenSpaceCamera) sortingOrder=0 overrideSorting=False
   ├─ FrontCanvas (GO 606, RT 2900)  ← Canvas_2569: renderMode=1(SS Camera) sortingOrder=0 **overrideSorting=True** (权威表 ✓)
   │  └─ Safe area FrontCanvas (GO 713, RT 2862, stretch 全屏)  ← FrontCanvas 弹层全部挂这里(m_Father=2862)
   │     ├─ Card Display Window GO 419 / RT 3030 (inactive)
   │     ├─ Generic Multi Card Display Combat GO 768 / RT 3175 (inactive)
   │     ├─ BattleSettingsPanel GO 803 / RT 3340 (inactive)
   │     ├─ Tutorial GO 1159 / RT 3013 (active)
   │     ├─ LoadingMenu GO 907 / RT 3313 (active; 子 Panel inactive)
   │     ├─ Alliance Panel GO 1130 / RT 2859 (inactive)
   │     └─ DebugButtons GO 251 / RT 3162 (**m_IsActive=False**, 原始 JSON 确认)
   ├─ BattleDoors GO 60 / RT 2717 (inactive)  ← **直属主 Canvas**(不是 FrontCanvas 子级!)
   │  └─ Canvas GO 1055 / RT 3118 (Canvas_2574: renderMode=2 Overlay, overrideSorting=True; GraphicRaycaster MB 4200)
   ├─ Tutorial highlight GO 656 / RT 3306 (inactive) ← 直属主 Canvas
   ├─ Anim Anchors GO 868 / RT 2614 (active) ← 直属主 Canvas
   └─ UI Error Message Controller (…MUST BE ENABLED) GO 481 / RT 3373 ← 直属主 Canvas (先前已核)
```

已核 Canvas 组件(原始 JSON raw): `Canvas_2569 {m_RenderMode:1, m_OverrideSorting:True, m_SortingOrder:0}` / `Canvas_2574 {2, True, 0}` / `Canvas_2572 {1, False, 0}` / `Canvas_2575 {2, False, 0}`。

---

## 一、Card Display Window(卡牌展示窗, GO 419, inactive)

根 GO 419 / RT 3030: anchor(0,0)-(1,1) pos(0,0) size(0,0) → 绝对=全屏 1920×1080; 组件 MB 4750 script=-9194957658433400948(卡牌展示窗控制类)。RT children=[3078, 2627]。

| 元素 | GO/RT/MB PathID | raw 值 (Unity) | chain_rect 绝对 (Godot) | 贴图名+原尺寸+缩放/9-slice border | 文字+fs+颜色 | active | 备注 |
|---|---|---|---|---|---|---|---|
| Menu Dark Background | 631 / 3078 / MB 4682 (Image) + 4809/4942 脚本 | anchor(0.5,0.5) pos(0,0) size(4574.60,2572.36) | x[-1327.3,3247.3] y[-746.2,1826.2] | sprite=0(无图纯色) color(0,0,0,**0.7725**) type=0(Simple) raycast=1 | — | True | 场景遮罩: 全层黑色半透明罩(4574×2572 超屏), 与 Generic MultiCard 的同值(见 §二) |
| Card Display | 243 / 2627 | pos(0,0) size(752,868) anchor(0.5,0.5) | x[584.0,1336.0] y[106.0,974.0] | — | — | True | 主展示区(卡+flavour 下条); RT children=[3533,3019,2599,**1433(导出缺失**)] |
| LowerSection | 827 / 3533 / MB 4461 | pos(0,-518.34) size(1320,137) pivot(0.5,**0**) | x[300.0,1620.0] y[921.3,1058.3] | — | — | True | 卡展示窗底部条; 相对卡展窗底并出头 13px |
| FlavourTextBG | 1095 / 2753 / MB 4277 | pos(0,0) size(1320,178) | x[300.0,1620.0] y[900.8,1078.8] | sprite=0 color(1,1,1,1) raycast=0 | — | True | 口味文字底(纯白, 运行时可能设材质) |
| LoreText | 885 / 3418 / MB 3837 (+3642) | pos(0,0) size(1250,93.47) | x[335.0,1585.0] y[943.1,1036.6] | — | 'Ghazghkull Mag Uruk Thraka is a mighty prophet of the Waaagh! He is the most influential greenskin in the galaxy, and commands billions to war.' fs=32 (base 36, **autoSize[18..32]**) 白(1,1,1,1) HAlign=2(中) VAlign=512(中) fontStyle=0 fontAsset=**-8244042478085975641=Asar-Regular SDF** | True | 场景占位文本=Ghazghkull 台词(运行时替换为所展示卡) |
| Voices Over Button(语音按钮) | 874 / 3221 / MB 4775 (Image)+4852 (Button)+4541壳 | pos(734.33,0.0002) size(**88.655**,88.655) | x[1650.0,1738.7] y[945.5,1034.2] | -2709381852941166021 = **40k_UI_bt_voicelines** 128×128 →88.66=0.693x border=0 | — | True | Button MB 4852: transition=2(SpriteSwap) interactable=1 colors[normal(1,1,1) hi(0.961,0.961,0.961) pres(0.784,0.784,0.784) sel(0.961…) dis(0.784,0.784,0.784,1)] onClick=0 |
| └ Button Text 'X' | 854 / 3179 / MB 3803 | anchor stretch pos(0,0) size(-16,0) | (按钮内) | — | 'X' fs=30 base 12 auto[12..30] 白 H=2 V=4096(Midline) font=**3485036404935369831=Pragati-Regular SDF** | **False** | 悬停 X 标(场景 inactive) |
| └ Image (X 变体) | 1090 / 3270 / MB 4494 | anchor stretch size(-2,-2) | — | 40k_UI_bt_voicelines | — | False | inactive |
| Cards | 140 / 3019 | pos(0,0) size(100,100) anchor(0.5,0.5) | x[910,1010] y[490,590] | — | — | True | 5 张 CardUI 模板的容器(100×100 中心锚) |
| CardUI (4) | 971 / 2663 | pos(0,60) size(2.5437,3.3686) | (单位尺寸, 运行时乘卡宽) | — | — | **True** | children=[3362,3081,3457,3079,3029]; 组件: script -9042063067722591315(CardUI 类)+5×-1746985122272567298 |
| CardUI (3) | 315 / 3170 | 同 (4): pos(0,60) size(2.5437,3.3686) | 同上重叠 | — | — | **True** | children=[2848,2640,2990,2649,3522] |
| CardUI (2) | 635 / 2908 | 同上 | 重叠 | — | — | **True** | children=[3067,3357,3210,3350,2782] |
| CardUI (1) | 1128 / 2772 | 同上 | 重叠 | — | — | **True** | children=[3489,3181,3328,3191,2929] (**注意: md 树标 (inactive) 的是 pid 1013=墓地日志用, 不是本节点**; 原始 JSON 本节点 active) |
| CardUI (base 无名) | 705 / 2874 | 同上 | 重叠 | — | — | **True** | children=[3137,3438,3303,3422,2701] (**"base inactive" 不成立, 原始 active**) |

**CardUI 模板内部(每变体同构, 全部单位尺寸=卡宽比例值)**: CreatedByText(unit pos(0,1.6) size(2.5,0.4), 'Created by someone fancy' fs2.45 auto[0.3-3.0] Asar, **inactive**); 2DCard(size 2.0913×3.3334) → Front(pos(0,0.08) 2.0927×3.4913) → { Card Highlight And Shadow(4.4281×4.4281, sprite=0 运行时 SDF 生成), CardImage(2.7484×2.7484, sprite=0 运行时 set 立绘), Textbackgrounds(inactive, 内部 Big/Small UI 贴图 -6907984259834462938=Card Text smooth background 32×32 border(7,0,7,9)), CardFrame(2.2452×3.2572, sprite=0 运行时 set 阵营卡框), Card Info(脚本组件, **室内 RT 未导出**(Card Info RT pid 1356/1261/1415/1388/1422 均无文件) — 运行时实例化/预制体, 详情见权威表第三节), EffectAnchor }; UI Collider(size -0.2×-0.44 stretch); Cardback Container(**inactive**, 1×1; Cardback Shadow SDF 3912869094951939818 / Cardback 5194395880697229034); Card Ready for level up(**inactive**, 1.8×2.7, 7506635356624546798=Card Ready For Level Up 256×256); New Card Badge(pos(0.3,1.1) size(1.2,0.4) anchor(0,0.5) pivot(0,0.5), Image -6381131329509798649=WF_Special offer_Value 324×87 border(162,0,162,0) **color(0.55,0.09,0.05,1.0)** + Text 'Новинка!' fs27.7 **fontColor(1,0.78,0,1.0)** 黄 H1 V4096 Pragati); Ban Icon(size(0,2.2), 40k_Cross_icon_cross_big Banned card 512×512) + Banned Text 'Запрещено' fs0.25 auto[0.25..72] fontStyle=17(粗斜) H2 V4096 white。
**注意**: (4)/(3)/(2)/(1)/base 五张模板静态完全重叠(pos 全同) — 运行时由脚本选择展示哪张/展几张; 这就是原版"多卡展示/12 阵营换框"的实现位(场景中无 12 阵营字样, 阵营换框=运行时给 CardFrame sprite)。

| TutorialObjs(卡展窗内教程标注) | 904 / 2599 | pos(-46.6,4.4) size(3501.4,2291.0) anchor stretch | x[-1213.2,3040.1] y[-1043.9,2115.1] | — | — | True | 仅教程第 1 关用(卡上标注); 内含 UnitObjs→{MeleeText 'Melee Attack' fs30+Arrow(UI_Description_Arrow 333×123,color(0.67,0.67,0.67),206.2×76.2), RangedText 'Ranged Attack' fs30+Arrow, HealthText 'Health Points' fs30+Arrow, EnergyText 'Energy Cost' fs30+Arrow(size 340.3×59.3,pos(497,252))} + EffectList(无 RT, 内含 AffectedBy/Elements/EffectElement 条目, RT 未导出) |

---

## 二、Generic Multi Card Display Combat(多卡展示, GO 768, inactive)

根 GO 768 / RT 3175: **anchor(0,0.5)-(1,0.5)** pos(0,0) size(0,**818.04**) → 绝对 x[0,1920] y[131,949]; **自身挂 ScrollRect MB 4823**: content=3174 viewport=3176 **m_Horizontal=1 m_Vertical=0 m_MovementType=1(Elastic)**(横向滚动多卡展示!); MB 4182 script=5501186504207076316。

| 元素 | GO/RT/MB PathID | raw 值 | chain_rect 绝对 | 贴图 | 文字+fs+颜色+对齐 | active | 备注 |
|---|---|---|---|---|---|---|---|
| Menu Dark Background | 511 / 2983 / MB 4557 | 同 §一 631 (pos0 size 4574.6×2572.4) | x[-1327.3,3247.3] y[-746.2,1826.2] | sprite=0 color(0,0,0,0.7725) raycast=1 | — | True | 属于本组(父 RT=3175) — **权威表把它挂在 Card Display Window 下易误读** |
| Header Text | 795 / 3391 / MB 3753 | anchor(0.5,1) pivot(0.5,1) pos(0,0) size(1192.37,63.204) | x[363.8,1556.2] y[131.0,194.2] | — | 'Header Text' fs=38 base31.9 auto[18..38] 白 H=2(中) V=512(中) Pragati | True | 多卡展示标题 |
| Viewport | 769 / 3176 | anchor stretch size(0,0) | x[0,1920] y[131,949] | — | — | True | ScrollRect viewport |
| Content | 767 / 3174 | size(**-1917.688**,0) pivot(0.5,0.5) | x[958.8,961.2](宽度塌缩=容器-1917.7 所致, 运行时由 OSA 展开) | — | — | True | ScrollRect content; **权威表 "Content=GO 541/RT 2761" 错误**(541/2761 是语言下拉的 Content, 见 §三) |
| CardUI Reference | 45 / 3421 | anchor(0,1) pos(1.2717,-409.02) size(2.5437,3.3686) | (单位尺寸) | — | — | True | 卡模板(同 CardUI): 脚本 -9042063067722591315+5×-1746985…; 全部 sprite=0 运行时填; 权威表第三节已核 |
| BattleContinueButton | 186 / 3058 | anchor(1,0) pivot(1,0) pos(-24.226,-53) size(832.89,49.84) | x[1062.9,1895.8] y[952.2,1002.0] | — | — | True | 无 MB(纯容器) |
| └ Button(横条) | 964 / 3348 / MB 4202+4460+5115+5110 | pos(132.11,0) size(577.50,63.84) | x[1322.7,1900.2] y[945.2,1009.0] | 5480819196723731418=**40k_bt_underbutton** 485×83 border(**240,0,240,0**) color(0.369,0.894,0.587) type=0 | — | True | **两个 Button**: 4460 = transition=1(ColorTint) targetGraphic=4202 colors[normal(0.369,0.894,0.587) hi(0.467,0.914,0.4) pres(0.478,0.494,0.482) sel(0.369,0.894,0.588) dis(0.784,0.784,0.784,0.502)] **onClickCalls=1 → MulliganManager.ClickMulliganDone(m_CallState=2)**; 5115=空 Button; 5110=空壳脚本 |
| └ Text | 242 / 2827 / MB 3886 | anchor(0,0)-(0.7702,0.9) pos(145.10,0) size(-354.2,0) | 条内右侧区 | — | 'Continue' fs=38 base36 auto[18..38] 白 **H=4(Right)** V=512 Pragati | True | '继续' 右对齐 |
| └ CircleButton | 804 / 3384 / MB 4285+4756 | pos(284.13,0) size(80.469,79.636) | x[1723.2,1803.7] y[937.3,1016.9] | 9056225148749506697=**40k_UI_bt_play** 128×128→80.47=0.63x | — | True | Button 4756 transition=2(SpriteSwap) onClick=0; 圆钮纵向凸出横条(条 y945-1009, 钮 y937-1017) |

---

## 三、BattleSettingsPanel(战斗设置面板, GO 803, inactive)

根 GO 803 / RT 3340: anchor(0.5,0.5) pos(0,-29.317) size(743.202,758.635) → 绝对 x[588.4,1331.6] y[190.0,948.6]; 组件: MB 4034 script=-8570598281047176983 + **GraphicRaycaster MB 4689**(面板自身拦截点击)。RT children=[3451,3102,3006,3099,2697,3321,2751,2836,2646]。
**注意面板内所有坐标均为面板局部**(anchor 相对 743×759 矩形)。

| 元素 | GO/RT/MB PathID | raw 值 (局部) | chain_rect 绝对 | 贴图+border | 文字+fs+颜色+对齐 | active | 备注 |
|---|---|---|---|---|---|---|---|
| Dark Shade(遮罩) | 37 / 3451 / MB 4739 | pos(0,0) size(3963.53,2692.97) | 超屏 (中心 (960,511)) | sprite=0 color(0,0,0,**0.6118**) raycast=1 | — | True | 阻断背后输入 |
| Generic Popup Background | 1142 / 3102 / MB 4155 | anchor stretch pos(0,1.094) size(-3.818,2.187) | x[590.3,1329.7] y[187.8,948.6] (739.4×760.8) | -7511397500040153103=**40k_popup** 359×336 border(**169,160,169,160**) **type=1(Sliced 9-slice)** | — | True | 面板底(9-slice 拉伸 359×336→739×761) |
| └ Mask | 544 / 3207 / MB 5256 | stretch size(-20.268,-19.245) | 同上内缩 | 40k_popup type=1 raycast=0 + MB 4477(Mask/RectMask2D 类) | — | True | 内容裁剪 |
| └ Background fill | 87 / 2657 / MB 4429 | stretch size(0,0) | 同上 | 8740064623924276539=**40k_popup_texture** 128×128 border 0 **type=2(Tiled)** | — | True | 面板纹理平铺层 |
| Generic Close Button | 805 / 3006 / MB 4346+4038+4794 | pos(364.514,345.3) size(75,75) | x[1287.0,1362.0] y[186.5,261.5] | 2381704724431365035=**UI_Button_Round_background** 237×237→75=0.316x raycast=0 | — | True | Button 4038 SpriteSwap onClick=0; (**权威表 "GO 679"=能量空槽, 错误**) |
| └ Close Button(叉) | 277 / 3460 / MB 5056 | anchor(0.1266,0.1367)-(0.8734,0.8633) | 钮内 56×54 | 6553861554683527146=**40k_bt_close** 175×174→≈56=0.32x | — | True | |
| Auto Zoom Toggle(行) | 1088 / 3099 / MB 4356 | pos(-282.42,249.48) size(472.46,75.64) pivot(0,0.5) | x[677.6,1150.0] y[282.0,357.7] | — | — | True | Button 4356 transition=1 targetGraphic=4075(子 Toggle 图) colors[normal(0.286,0.965,0.686) 绿] |
| └ Toggle | 1061 / 2654 / MB 4075 | pos(37.03,-37.82) size(74.06,57.67) anchor(0,1) | x≈[714.6,788.7] y≈[306.8,364.5] | **-5728790147372056906 = 40K_dropdown_bg** 119×102 border(**23,20,23,20**)→74.06×57.67 (**不是 40K_toggle_off!**) | — | True | **矛盾#1**(任务书/权威表写成 40K_toggle_off — 原始 pid→battlearena1 map=40K_dropdown_bg; 40K_toggle_off/on 只在 ChatToggle 用) |
| └ CheckMark | 677 / 3250 / MB 4816 | stretch size(0,0) | 同 Toggle | 4411787853012002210=**40K_settings_icon_checkmark** 66×51 | — | True | 勾图标 |
| └ Label | 624 / 3268 / MB 3977 | pos(79,-37.82) size(229.29,75.64) anchor(0,1) pivot(0,0.5) | x[756.6,985.9] y[282.0,357.7] | — | 'Auto zoom' fs=42 base36 auto[29..42] 白 H=1(左) V=512 Pragati | True | |
| Debug Buttons(调试区) | 137 / 2697 | anchor(0.5,1) pivot(0.5,0) pos(3.87,50) size(747.13,137.57) | x[590.3,1337.4] y[**2.4,140.0**] ⚠ 面板顶外(贴屏顶) | — | — | True | 原版就把调试按钮组放在弹窗上方屏幕顶 |
| └ Buttons container | 706 / 3147 | 747.13×90 | (区顶下 50) | — | — | True | MB 4846(FlexibleGrid/布局类) |
| └ Win | 490 / 3008 / MB 4392+4622 | pos(124.52,-45) size(249.04,100) anchor(0,1) | x[590.3,839.4] y[21.2,121.2] | 5651555388418207694=**40K_button** 489×107 border(**234,46,234,46**) color(**0.943,0.165,0.822** 品红) | 'Win' fs38 | True | **矛盾#2: 不是绿色, 品红**; Button 4622 SpriteSwap |
| └ Draw | 507 / 3060 | pos(373.57,-45) 249.04×100 | x[839.4,1088.4]… | 40K_button 品红 | 'Draw' fs38 | True | |
| └ Console | 819 / 3282 | pos(622.61,-45) 249.04×100 | x[1088.4,1337.4]… | 40K_button 品红 | 'Console' fs38 | True | |
| └ Debug button text | 1178 / 3337 / MB 3914 | size(200,50) anchor(0.5,1) | — | — | 'Debug Buttons' fs36 H2 V512 Pragati | True | |
| └ Border×3 | 748/2817, 944/3464, 113/2993 (MB 4712/4629/5270) | 767.92×3.04; 2.48×33.74×2 | — | -3734815765473162916=**White Square** 8×8 color(0.698,0.698,0.698,0.702) | — | True | 表格线 |
| Volume Sliders | 860 / 3321 / MB 4896 | pos(-2,-19.89) size(701.35,441.40) | x[607.3,1308.7] y[368.5,809.9] | — | — | True | 包含 Music/FX/Voiceover 三行 + ChatToggle |
| └ Music Container | 1193 / 3417 / MB 4332 | size(0,100) anchor(0,0) | 行高 100 | sprite=0 color(0.236,0.236,0.236,1) | — | True | 行底(深灰) |
| └ Music Slider | 263 / 2943 / MB 4304 | anchor(**0.1,0.33)-(0.9,0.45**) pos(0,0.4) | 行内中带 | — | — | True | SLIDER: fill=2987 handle=2979 **direction=0(L2R) min=0 max=1 value=1** |
| └ Background(条) | 540 / 3291 / MB 4355 | stretch | — | 5759692323890646976=**Volume_bar_inactive** 400×31 border(**184,0,184,0**) type=1(Sliced) | — | True | 9-slice 拉满 0.8 宽 |
| └ Fill | 1091 / 2987 / MB 4553 | stride | — | -6332789735026905802=**Volume_bar_active** 64×31 border(**30,0,30,0**) type=1 | — | True | 音量条填充 |
| └ Handle | 124 / 2979 / MB 4502 | pos(12,0) size(46.81,22.41) | — | -1471652398003037618=**Volume_button** 110×110→46.8×22.4 (0.43x/0.2y) | — | True | 滑钮 |
| └ Text 'Music' | 443 / 2754 / MB 3845 | anchor(0.1,0.45)-(1,1) | 行上 0-45 | — | 'Music' fs42 base36 auto[10..42] 白 H=1 V=**1024(Bottom)** Pragati | True | 行标题在滑条上方 |
| └ FX Container | 689 / 2865 / MB 4297 | 同构 0×100 | 行 100 | color(0.236…) | — | True | 第二行 |
| └ Sound effects(第一行内其实是 FX) Slider | 448 / 2631 / MB 4442 | anchor(0.1,0.33)-(0.9,0.45) | — | — | — | True | fill=2619 handle=2597 min0 max1 value1 |
| └ Text 'Sound Effects' | 939 / 3265 / MB 3815 | — | — | — | 'Sound Effects' fs42…V1024 | True | |
| └ Voiceover Container | 954 / 3297 / MB 4540 | 0×100 | 行 100 | color(0.236…) | — | True | 第三行 |
| └ Slider(voice) | 714 / 3329 / MB 4799 | 同构 | — | — | — | True | fill=3370 handle=3354 |
| └ Text 'Voice-overs' | 358 / 3056 / MB 3809 | — | — | — | 'Voice-overs' fs42…V1024 | True | **注意 GO 358 的 RT 3056 与 Mulligan TurnText RT 3056 撞号** — RT pathid 3056 属本节点(战斗场景内 RT 全局唯一, Mulligan TurnText 是 RT 3403) |
| ChatToggle | 225 / 2949 / MB 4784 | size(0,54.9) anchor(0,0) | x=607.3 起 y[782.5,837.4] | — | — | True | Button 4784 transition=1 targetGraphic=5147(Bottom off 图) |
| └ Background(off) | 676 / 3064 / MB 5147 | pos(190,0) size(180.76,56.80) | x≈[797.6,978.4] y[782.5,839.3] | 9192459206374834003=**40K_toggle_off** 301×120→180.8×56.8 (0.60x/0.47y) | — | True | |
| └ Checkmark(off) | 1064 / 2710 / MB 4527 | pos(-32.86,-0.82) size(57.93,46.22) | — | **-1471652398003037618=Volume_button**(同一 110×110 按钮图) | — | True | 右位, 内 OffText |
| └ OffText | 304 / 3380 / MB 3742 | pos(54.18,-0.5) size(-8.46,2.99) stretch | — | — | 'OFF' fs30 auto[1..30] H2 V512 Pragati | True | |
| └ Background(on) | 759 / 3063 / MB 4920 | pos(190,0) 同 off 尺寸 | 同位 | 8392723491736595734=**40K_toggle_on** 301×120 | — | True | |
| └ Checkmark(on) | 430 / 3440 / MB 4527式 | pos(34.1,-0.82) 57.93×46.22 | — | Volume_button | — | True | 左位 |
| └ OnText | 34 / 3412 / MB 3959 | pos(-53.1,-0.5) | — | — | 'ON' fs30 auto[1..30] H2 V512 | True | |
| └ Label 'Mute opponent' | 510 / 2812 / MB 4003 | pos(-73.5,0) size(346.2,53.0) | x≈[533.8,880.0] | — | 'Mute opponent' fs42 base36 auto[18..42] 白 H=1 V=512 Pragati | True | 标签(静音对手开关) |
| Bottom buttons | 366 / 2751 | anchor(0.5,0) pos(0,68.8) size(686.8,100) | x[616.6,1303.4] y[829.8,929.8] | — | — | True | 底部双按钮行 |
| └ Resign Button | 894 / 3211 / MB 4755+4744? | pos(171.71,-50) size(300,90) | x[638.3,938.3] y[834.8,924.8] | 40K_button border(234,46,234,46) color(0.369,0.894,0.587) 绿 | 'Resign' fs38 base12 auto[12..38] H2 V4096 | True | Button(原始组: IMAGE MB 4755; dump 显示 Button) |
| └ Button Text(Resign) | 870 / 3194 / MB 3799 | stretch size(-26,0) | — | — | 'Resign' fs38 | True | |
| └ SkipTutorial Button | 788 / 3271 / MB 4721 | pos(515.10,-50) size(300,90) | x[981.7,1281.7] y[834.8,924.8] | 40K_button 绿(0.369,0.894,0.587) | 'Skip tutorial' fs38 | True | |
| └ Button Text(Skip) | 807 / 3298 / MB 3762 | — | — | — | 'Skip tutorial' fs38 H2 V4096 | True | |
| Match Skulls(骷髅说明, 面板外!) | 169 / 2836 | anchor(0.5,0) pivot(0.5,1) pos(-0.40,-20) size(743.2,148.76) | x[588.0,1331.2] y[**968.6,1117.4**] ⚠ 面板底(948.6)之下 | — | — | **False** | 原版存这但面板不可见处; md 树漏掉此节点 |
| └ Match Skulls Text | 107 / 3531 / MB 3999 | pos(0,114) size(686.8,50) | — | — | "Reduce your **oponents'** health to win more skulls:" fs31.59 white **α0.6314** H2 V512 Pragati | True | 原文字即拼写 'oponents''(**原版笔误, 勿"修正"**) |
| └ Match Skulls(图标位) | 929 / 2967 / MB 4906 | size(686.8,75.21) | — | — | — | True | 运行时填骷髅 |
| Language Selector | 215 / 2646 / MB 5273 | anchor(**0.12,0.5**)-(0.9,0.5) pivot(0,0.5) pos(0,317) size(0,59.395) | x[677.6,1257.3] y[222.6,282.0] | — | — | True | 语言行(行内右挂下拉, 左侧标题) |
| └ LanguagesDropdown | 657 / 3546 / MB 4847+5260 | anchor(1,0.5) pos(-250,0) size(250,59.40) | x[1007.3,1257.3] y[222.6,282.0] | 4198243566598287219=**40K_dropdown_field_closed** 727×102 border(**60,35,60,35**)→250×59 color(0.122,0.973,0.537) 绿 | — | True | DROPDOWN MB 5260: template=3553 caption=3821 item=3842 **options=0(运行时填)** |
| └ Label(caption) | 1087 / 3462 / MB 3821 | stretch | — | — | '(空)' fs18 base14 auto[18..40] color(0.67,0.67,0.67) H1 V512 | True | 下拉框内当前语言文字 |
| └ Arrow | 789 / 3144 / MB 4695 | pos(-15,0) size(20,20) anchor(1,0.5) | — | -1891211968353393973=**40K_dropdown_arrow_closed** 46×19→20×20 color(0.02,0.353,0.192) | — | True | |
| └ Template(展开列表) | 182 / 3553 / MB 4782+4335+4166 | pos(-2.5,-22) size(-5.0,573.96) pivot(0.5,1) | 下拉按钮下方展开 574 高 | **-5728790147372056906=40K_dropdown_bg** 119×102 border(23,20,23,20) type=1 color(0.286,0.965,0.686) | — | **False** | 展开容器(ScrollRect: content=2761 viewport=2642 h=0 v=1 movement=2(Clamped)) |
| └ Viewport | 917 / 2642 / MB 4401+4599 | size(-17,0) pivot(0,1) | — | -426171492875694260=**UIMask** 32×32 border(10,10,10,10) type=1 color(0.369,0.894,0.588) | — | True | Mask 类 |
| └ Content | 541 / 2761 | size(0,41.72) anchor(0,1)-(1,1) | — | — | — | True | **语言下拉的列表 content**(权威表把 541/2761 误归多卡展示) |
| └ Item | 914 / 2589 / MB 4939 | size(0,40.87) | 行高 40.87 | — | — | True | Button transition=2 targetGraphic=4776 colors[normal(0.369,0.894,0.588)] |
| └ Item Background | 708 / 3092 / MB 4776 | stretch | — | 5175970378912652380=**40K_dropdown_item** 717×92 color(0,0.831,0.525) | — | True | 列表项底 |
| └ Item Checkmark | 1066 / 2591 / MB 5272 | pos(10,0) size(20,20) | — | 6419449077939965772=**Checkmark** 40×40(rect 12,12) | — | True | |
| └ Item Label | 1191 / 2624 / MB 3842 | pos(5,-0.5) | — | — | 'Option A' fs30 base14 auto[18..40] color(0.783,0.783,0.783) H1 V512 | True | 运行时替换语言名 |
| └ Scrollbar | 735 / 2794 / MB 4203+4672 | size(20,0) pivot(1,1) | — | 1660267235368898380=**Background** 32×32 border(10,10,10,10) type=1 | — | True | 同款 'Background' 内置资源 |
| └ Handle | 581 / 3127 / MB 4416 | anchor(0,0)-(1,0.9273) size(20,20) | — | 5837791642106728268=**UISprite** 32×32 border(10,10,10,10) type=1 | — | True | |
| SelectLanguageText | 1218 / 3119 / MB 4004 | anchor(0,0.5) pos(0,0) size(335,57.53) | x[677.6,1012.6] y[223.6,281.1] | — | 'Select Language' fs42 base36 auto[10..42] 白 H=1 V512 Pragati | True | 行标题 |

---

## 四、Tutorial(教程层, GO 1159 / RT 3013, **active**, 全屏 1920×1080)

RT 3013 anchor(0.5,0.5) pos(0,0) size(1920,1080) → 全屏。children=[3262,1447,2810,3377]。(1447=TutorialArrows 的 RT — **未导出文件中**, 与 Card Info 同类缺失。)

| 元素 | GO/RT/MB PathID | raw 值 | chain_rect 绝对 | 贴图 | 文字 | active | 备注 |
|---|---|---|---|---|---|---|---|
| InitTutorialTip | 1161 / 3262 | anchor(0.5,0.5) size(100,100) | x[910,1010] y[490,590] | — | — | **False** | 教程开场提示(运行时脚本定位) |
| └ TipText | 965 / 3352 / MB 3719 | pos(-3,0) size(**2.9,1.0**)(单位) | (单位) | — | '(空)' fs5.2 base4.3 auto[1..5.2] 白 H2 V512 **Asar** | True | 运行时填文案; 子级 TMP SubMesh/Bg/arrow1(1)=脚本生成 |
| └ TipText (1) | 1166 / 3528 / MB 3723 | pos(3,0) size(2.9,1.0) | (单位) | — | 同上 fs5.2 Asar | True | |
| TutorialArrows | 1160 / (RT 1447 缺失) | — | — | — | — | True | 下挂 arrow1/arrow2/arrow1(1)/arrow1(2)(全部无 RT 导出) |
| TutorialTip | 1158 / 2810 / MB 5246+5304+4960 | pos(4.04,-0.99) size(430,150) | x[749.0,1179.0] y[466.0,616.0] | — | — | True | 教程气泡主体 430×150(中心偏(4,-1)) |
| └ Generic Popup Background | 946 / 3301 / MB 4247+4990+4752 | size(**436.25,0**)(高 0!) | x[745.9,1182.2] y[541,541](高度动态) | 40k_popup 359×336 border(169,160,169,160) type=1 | — | True | **高度字段=0 → 由内容/LayoutGroup 撑开**(复刻时 NinePatch 高度须动态) |
| └ Mask | 354 / 3115 / MB 4982 | stretch | — | 40k_popup type=1 raycast=0 + 双 Mask 脚本 | — | True | |
| └ Background fill | 272 / 2714 / MB 4524 | stretch | — | 40k_popup_texture 128×128 type=2 | — | True | |
| └ TipText | 967 / 3353 / MB 3769 | pos(218.12,-55) size(408.85,0) anchor(0,1) | x[759.6,1168.5] y[596…](顶线 541-55) | — | 'Test tutorial tip' fs36 base36 auto[1..36] 白 H=2 V512 Pragati | True | 场景占位文案(运行时替换) |
| └ Bg | 966 / 3209 / MB 5263 | stretch size(0,0) | — | -3055422674097938669=**40K_display** 442×112 | — | **False** | 备用底 |
| └ ContinueText | 1164 / 3527 / MB 3722 | pos(0,-1.55) size(2.37,0.56)(单位) | (单位) | — | 'Continue' fs4.5 base4.3 auto[2..6] 白 H2 V512 **Asar** | **False** | 点击继续提示 |
| TutorialPointerCombat | 479 / 3377 / MB 4791 | size(1920,1080) | 全屏 | — | — | **False** | 教程指针层 |
| └ Pointer | 456 / 3261 / MB 4107 | pos(-34.44,-59.83) size(100,100) | x[875.6,975.6] y[549.8,649.8] | -2590500462584155290=**Cursor_Space Marine_01** 128×128→100=0.78x raycast=0 | — | True | 教程光标(运行时移动) |
| └ light | 1222 / 2821 / MB 4907 | stretch size(100,100) | 同 Pointer | -592821608806009055=**Cursor_Space Marine_01Glow** 128×128 color(**1,0.946,0.505**) raycast=0 | — | True | 光标黄光晕(盖在 pointer 上) |

---

## 五、Alliance Panel(联盟面板, GO 1130, inactive)

根 GO 1130 / RT 2859: anchor(0,1) pos(-1.044,-25.272) size(815.04,475.47) pivot(0.13,**0.825**) → 绝对 x[-107.0,708.0] y[**-57.9**,417.5](顶出屏 58px)。组件 MB 5251(script=3361136396530371980,**enabled=0** 空壳)+MB 4117(script=3032143368954510144 控制器)。children=[2698,3166,3126,2660]。

| 元素 | GO/RT/MB PathID | raw 值 (面板局部/父) | chain_rect 绝对 | 贴图 | 文字+fs+颜色+对齐 | active | 备注 |
|---|---|---|---|---|---|---|---|
| Close Background | 377 / 2698 / MB 5220 | pos(351.76,41.66) size(11668.3,8012.0) | 全屏盖(中心≈面板右上) | sprite=0 color(0,0,0,**0.4627**) raycast=1 | — | True | 点击关闭遮罩(超屏) |
| Background | 523 / 3166 | pos(371,206) size(722.66,403.13) | x[-97.3,625.3] y[10.0,413.1] | — | — | True | 面板主底 |
| └ BG(内底) | 362 / 3225 / MB 4162 | pos(28.82,5.62) size(557.31,301.35) | — | -3310669572026492178=**40K_shop_offer_bg_Sororitas_0** color(0.349,0.349,0.349,1) type=0 | — | True | 中心内容底(灰) |
| └ BGFrame | 1152 / 3339 | pos(2.22,-1.57) size(716.88,399.10) | — | — | — | True | 四边拼边框 |
| └ BGFrame Top | 167 / 3414 / MB 4691 | pos(16.37,171.31) size(517.69,33.92) | — | -7266636332286626103=**40k_UnitChat_Background_Top** | — | True | 与原版聊天框同素材族 |
| └ BGFrame Bottom | 1024 / 3122 / MB 5261 | pos(17.11,-153.83) 517.69×36.53 | — | 6130416589180386937=**40k_UnitChat_Background_Bottom** | — | True | |
| └ BGFrame Right | 555 / 3407 / MB 4301 | pos(317.09,13.59) 83.65×371.06 | — | 556259315706983459=**40k_UnitChat_Background_Right** | — | True | |
| └ BGFrame Left | 1037 / 2734 / MB 4338 | pos(-300.24,-5.41) 117.32×386.98 | — | 450114720226241101=**40k_UnitChat_Background_Left** | — | True | |
| EnemyInfo(敌信息组) | 559 / 3126 | anchor(0,1) pos(157,-107.74) size(260,75) | x[50.0,310.0] y[49.8,124.8] | — | — | True | 面板第一组 |
| └ Avatar Item Small | **923** / 2837 / MB 4280+4376+5045 | pos(-85.02,-50.81) size(212.54,186.39) | x[-11.3,201.3] y[44.9,231.3] | — | — | True | Button 4280 transition=1 targetGraphic=4176 **interactable=0** onClick=0; (**权威表 "GO 81/RT 3097" 是 BackCanvas 敌名条上的 155.6×136.5 变体 — 见矛盾#4**) |
| └ └ Raycast Target | 1185 / 3095 / MB 4596 | pos(0.45,7.02) size(109.05,122.11) | — | sprite=0 color(1,1,1,**0**) | — | False | |
| └ └ Image Container | 179 / 3054 | stretch pos(0,18.7) size(0,-37.4) | — | — | — | True | 头像区高 149 |
| └ └ └ Highlight | 1112 / 3037 / MB 4141 | stretch pos(1.67,2.6) size(3.34,1.85) | 容器+3.3×+1.85 | -6248698112513205973=**Player_Avatar_selected** 256×256 | — | **False** | 选中头像高亮 |
| └ └ └ Border | 571 / 3040 / MB 4114 | anchor(0,-0.1)-(1,0.9) | 容器 213×149(+上 15px) | -295601456118903261=**Player Profile Border** 256×286→213×149 | — | True | 头像框 |
| └ └ └ Image(头像) | 1169 / 2716 / MB 4176 | stretch pos(0,2.7) | — | sprite=0(运行时 set 头像) | — | True | |
| └ └ Avatar Name | 288 / 3469 / MB 3986 | anchor(0,0)-(1,0) pivot(0.5,1) size(0,41.31) | 头像下 | — | 'Avatar name' fs36 base36 auto[12..36] 白 H2 V512 Pragati | **False** | 战场中显示玩家名; 联盟面板inactive |
| └ PlayerInfo | 1109 / 3363 / MB 4213 | pos(188.49,-50.81) size(376.97,151.23) | x[180.0,557.0] y[62.5,213.7] | — | — | True | (权威表坐标 ✓一致) |
| └ └ NameHolder | 262 / 2650 | size(0,42.6) | — | — | — | True | |
| └ └ └ PlayerLabel | 196 / 3205 / MB 4073 | pos(0,-0.05) size(117.43,51) | — | — | 'Name:' fs35 auto[18-35] 白 H1 V=8192(Capline) **Asar** | True | |
| └ └ └ Name Text | 494 / 2779 / MB 4118 | pos(235.72,-0.05) size(348.76,51) | — | — | 'Alliance name' fs40 auto[18-40] H1 V8192 Asar | True | 运行时填对手名 |
| └ └ TitleHolder | 878 / 3259 | size(0,38.4) | — | — | — | True | |
| └ └ └ TitleLabel | 919 / 2887 / MB 4646 | pos(0,-0.69) 118.04×51 | — | — | 'Title:' fs35 auto[18-35] H1 V8192 Asar | True | |
| └ └ └ Title Text | 1017 / 3267 / MB 4901 | pos(235.72,-0.34) 348.76×51.32 | — | — | 'Alliance name' fs35 H1 V=4096(Midline) Asar | True | |
| └ └ Player Rating Display | 662 / 3111 | size(0,60.54) | — | — | — | True | |
| └ └ └ Secondary Icon | 1139 / 3536 / MB 4545 | anchor(0,1) size(61.82,60.54) | — | -6552398156213418268=**40k_UI_icon_ranked_Skirmish** 128×128→61.8×60.5 | — | **False** | 排名图标(备选位) |
| └ └ └ Main Icon | 637 / 3455 / MB 4325 | size(0,0) | — | 40k_UI_icon_ranked_Skirmish | — | True | 尺寸 0=运行时由 RatingDisplay 脚本设 |
| └ └ └ Individual rating value | 958 / 3323 / MB 5118 | size(0,0) | — | — | '-------' fs45 base31.38 auto[18..45] H1 V4096 Pragati | True | 运行时分 |
| └ Alliance | 847 / 3495 | pos(118.90,-201.26) size(551.61,135.95) | x[23.1,574.7] y[220.6,356.5] | — | — | True | 面板第二组(联盟行) |
| └ └ Alliance Label | 401 / 2635 / MB 4098 | pos(160,23.38) size(118.64,51) | — | — | 'Alliance:' fs33.4 auto[18-35] 白 H1 V8192 **Asar** | True | |
| └ └ Alliance Name | 1190 / 3272 / MB 5236 | pos(384.3,23.38) size(321.12,51) | — | — | 'alliance name' fs35 auto[18-35] H1 V8192 Asar | True | 运行时填联盟名 |
| └ └ Alliance Badge Drawer | 753 / 3503 | pos(-197.5,9.0) size(139.72,131.10) | x≈[403.7,543.4]? | — | — | True | 徽章抽屉(运行时) |
| └ └ └ Frame | 264 / 2595 / MB 4088 | stretch | — | sprite=0 | — | True | 徽章框(运行时) |
| └ └ └ Badge | 247 / 3281 / MB 4554 | stretch pivot(0,0.5) | — | sprite=0 | — | True | 徽章图(运行时) |
| └ └ Alliance Rating Display | 661 / 3110 / MB 4785+4974 | pos(230.54,40.43) size(184.18,60.54) | — | — | — | True | **不含 '34' 文字**(矛盾#5) |
| └ └ └ Secondary Icon | 1140 / 3534 / MB 4549 | anchor(0,1) 61.82×60.54 | — | 40k_UI_icon_ranked_Skirmish | — | **False** | |
| └ └ └ Main Icon | 639 / 3456 / MB 4327 | size(0,0) | — | 40k_UI_icon_ranked_Skirmish | — | True | |
| └ └ └ Individual rating value | 955 / 3322 / MB 5121 | size(0,0) | — | — | '------' fs45 base31.38 auto[18..45] H1 V4096 Pragati | True | 联盟总分(占位 '------') |
| NotInaAllianceText | 762 / 2660 / MB 4475 | anchor(0,0.5) pos(399.83,-95.7) size(592.66,46.2) | x[-3.5,589.2] y[252.4,298.6] | — | 'This player is is still not part of an Alliance' fs32.9 base36 auto[18..35] 白 **α0.7725** H=1(左) V=4096 **Asar** | True | **原文含 'is is' 双 is**(原版笔误, 勿改) |

---

## 六、BattleDoors(结算门, GO 60, inactive)

根 GO 60 / RT 2717: anchor(0.5,0.5) pos(-0.064,-5.276) size(1920.12,1118.98) → 绝对 x[-0.1,1920.0] y[-14.2,1104.8](比全屏高 15px 上下); 组件 MB 5238(script=82936325632428873=BattleDoors)。**直属主 Canvas**(非 FrontCanvas)。

| 元素 | GO/RT/MB PathID | raw 值 (局部) | chain_rect 绝对 | 贴图+9-slice | 文字+fs+颜色+对齐 | active | 备注 |
|---|---|---|---|---|---|---|---|
| Canvas(内嵌) | 1055 / 3118 | stretch | 全屏(=BattleDoors 区) | — | — | True | **Canvas_2574: renderMode=2 (Overlay) overrideSorting=True**; GraphicRaycaster MB 4200 |
| EndBattlePanel | 1194 / 2639 | stretch | 全屏 | — | — | True | |
| Background | 285 / 2637 / MB 4830 | pos(0,0) size(3840,2160) | x[-960.1,2879.9] y[-534.7,1625.3] | 1660267235368898380=**Background**(内置 32×32 border(**10,10,10,10**)) type=1(Sliced) color(**0.0283,0.0283,0.0283,0.7529**) | — | True | 结算黑幕(9-slice 拉到 3840×2160, 内 3×2 块纹理平铺区) |
| Video Image | 796 / 3401 / MB 4961 | pos(-6,64) size(1920,1080) | x[-6.1,1913.9] y[-58.7,1021.3] | — | — | True | **RawImage**(MB 4961: m_Texture 运行时 set; 无 VideoPlayer 组件在场景 — 播放器由代码创建) |
| AllRewardsHolder | 583 / 2854 / MB 4902 | pos(0,-260) size(100,100) | x[909.9,1009.9] y[755.3,855.3] | — | — | True | 奖励组(中心下 260px) |
| └ SkullsHolder | 391 / 2616 / MB 4938 | size(648.1,52.38) | x[585.9,1234.0] y[829.1,881.5] | 1838051009033412331=**40k_main_bt_nametag** 109×41(Simple 拉伸→648×52) | — | True | 骷髅条底(3 骷髅共用一张条) |
| └ └ skull1/2/3 | 219/3279, 989/2875, 517/3435 (MB 4980/5296/5250) | 三个全部 pos(0,0) size(64.33,71.21) **完全重叠** | 条中央 x≈[909.9,974.2]… | -8668969440801097811=**40k_battle_Win Skull** 66×73→64.3×71.2=0.975x | — | True | **静态重叠, 运行时由脚本横排**(同 ChatButton 模式) |
| └ RewardsHolder | 287 / 3458 / MB 4801+4113 | size(495.2,49.0) | x[662.3,1157.5] y[830.8,879.8] | 40k_main_bt_nametag(拉伸 495×49) | — | True | 奖励条 |
| └ └ RewardHolder | 949 / 3481 / MB 4091 | size(140,50) | 条内 | — | — | True | 货币格(运行时按奖励数复制) |
| └ └ └ DrawerHolder | 1077 / 2954 | pos(5,0) size(65,50) pivot(1,0.5) | — | — | — | True | 图标槽 |
| └ └ └ Quantity | 587 / 3139 / MB 3928 | anchor(0.5,0.5)-(1,0.5) pivot(1,0.5) size(0,45) | — | — | '2000' fs40 base36 auto[18..40] 白 H2 V=8192(Capline) Pragati | True | 数量 |
| └ └ HolderRating | 1051 / 3228 / MB 4922 | size(186.97,45) | 条内右区 | — | — | True | 排名格 |
| └ └ └ Trophy | 1098 / 3441 / MB 4063 | size(60.06,60) | — | 40k_UI_icon_ranked_Skirmish 128×128→60.1 | — | True | 奖杯图标 |
| └ └ └ ArmyIcon | 741 / 2851 / MB 4880 | pos(92.48,-22.5) size(64.84,60) anchor(0,1) | — | sprite=0(运行时 set) | — | **False** | 阵营图标位 |
| └ └ └ RatingText | 1071 / 2897 / MB 3964 | size(65.30,46.87) | — | — | '34' fs40 base36 auto[18..40] H2 V8192 Pragati | True | **'34' 在这里**(联盟面板没有) |
| LoadingMenu(附带) | 907 / 3313 | anchor stretch size(0,0) | 全屏 | — | — | True | GO active(加载菜单容器) |
| └ Panel | 908 / 3314 / MB 4972 | size(12006.4,10394.9) | 超屏黑幕 | sprite=0 color(0,0,0,**1.0**) | — | **False** | 全黑覆盖(加载时由脚本启用) |

---

## 七、DebugButtons(调试层, GO 251, inactive)+ 教学高亮

| 元素 | GO/RT/MB PathID | raw 值 | chain_rect 绝对 | 贴图 | 文字 | active | 备注 |
|---|---|---|---|---|---|---|---|
| DebugButtons | 251 / 3162 | anchor(1,0.5) size(1920,1080) | 全屏 | (MB 4259+2081 缺) | — | **False** | **(原始 m_IsActive=False; dump_go_tree 输出误标 active — 双份拷贝 bug, 矛盾#17)** |
| └ PlayerDebugButtons | 947 / 3358 | pos(-157.3,-251) size(314.7,248.6) | x[1605.3,1920.0] y[666.7,915.3] | — | 'Player' fs36 | True | 钮: DiscardHand(100×100 Debug Clear Hand)/DestroyMinions(Debug Destroy troops)/Heal(Debug Heal)/AddPlayerCardButton/AddSelectedCardButton(橙 1,0.58,0)/SummonCardButton(0.45,1,0.98)/AddMana/RemoveMana/DamageWarlord(红 1,0.02,0.02)/RemoveSummonSickness/Toggle Infinity Mana(Card Frame Cost Icon+**'inf' fs52.75**)/ApplyEnvEffect('E' fs89.7) — 部分尺寸 0×0(运行时 layout) |
| └ EnemyDebugButtons | 747 / 3312 | pos(-157.3,337) size(314.7,212.7) | x[1605.3,1920.0] y[96.7,309.3] | — | 'Enemy' fs36 | True | 同族按钮(AddCardButton/AddSelectedCardButton/SummonCardButton/AddMana/RemoveMana…) |
| └ Card Picker | 192 / 3232 | size(1400,800) | x[260,1660] y[140,940] | color(1,1,1,0.753) | — | **False** | 卡选择器(调试): Shade background(0,0,0,0.7804)/DropDownList(下拉 200×30, ItemTemplate 150×30 UISprite)/Card Preview(0,0,0,0.75, 内 CardUI 模板 2.5×3.4)/PickButton('PICK' fs24 黄 0.96,1,0.45)/SummonButton('SUMMON' fs24)/OSA(卡列表, Views=40k_battlelog_display_enemy color(0.63,0.14,0.14), Card Name fs27, Cost fs27.7)/Close(40k_bt_close 60×60)/InputField(TMP)(InputFieldBackground 32×32 border10; Placeholder 'Enter text...' fs14) |
| Tutorial highlight | 656 / 3306 / MB 5145 | stretch | 全屏(直属主 Canvas) | -2216962027063784652=**Tutorial Highlight** 128×128 border(**3,2,2,2**) color(1,1,1,**0.8235**) raycast=0 | — | **False** | 教程圆环高亮(整屏一张图拉伸+9slice) + MB 4252 控制脚本 |

---

## 八、Anim Anchors(动画锚点, GO 868 / RT 2614, active, 直属主 Canvas)

| 元素 | GO/RT PathID | raw 值 | chain_rect 绝对 | 备注 |
|---|---|---|---|---|
| Anim Anchors | 868 / 2614 | anchor stretch | 全屏 | 动画定位容器(纯锚点) |
| Anchor: UI Center | 872 / 2905 | pos(0,0) size(100,100) | x[910,1010] y[490,590] | 屏幕中心 |
| Anchor: CreateCard | 835 / 2723 | pos(885.64,84.33) | x[**1795.6,1895.6**] y[**405.7,505.7**] | 右上(右下近能量区) **权威表/md 树 (836,946) 是 offset 局部值非绝对** |
| Anchor: Cemetery | 175 / 2960 | pos(-1763.09,-10.81) | x[-853.1,-753.1] y[500.8,600.8] | 屏幕左外(墓地动画滑入点) |
| Anchor: Stratagem ShowCase | 1144 / 2807 | pos(529,83) | x[1439,1539] y[407,507] | 右侧中上 **/(479,947) 同误** |
| Anchor: Next To Clock | 378 / 3015 | pos(766,83) | x[1676,1776] y[407,507] | 右上近时钟 **/(716,947) 同误** |

---

## 九、精灵资源表(pid → 名 → m_Rect → m_Border, 全部原始 Sprite JSON)

| pid | 名 | rect (w×h) | border(l,b,r,t→x,y,z,w) | 出处目录 (解包整理/) |
|---|---|---|---|---|
| -7511397500040153103 | 40k_popup | 359×336 | **169,160,169,160** | 12_主程序资源/Sprite |
| 8740064623924276539 | 40k_popup_texture | 128×128 | 0,0,0,0 (tiled) | 12_主程序资源/Sprite |
| 5651555388418207694 | 40K_button | 489×107 | **234,46,234,46** | 12_主程序资源/Sprite |
| 5480819196723731418 | 40k_bt_underbutton | 485×83 | **240,0,240,0** | 12_主程序资源/Sprite |
| 4198243566598287219 | 40K_dropdown_field_closed | 727×102 | **60,35,60,35** | 12_主程序资源/Sprite |
| -5728790147372056906 | **40K_dropdown_bg** | 119×102 | **23,20,23,20** | 12_主程序资源/Sprite |
| 9192459206374834003 | 40K_toggle_off | 301×120 | 0,0,0,0 | 12_主程序资源/Sprite |
| 8392723491736595734 | 40K_toggle_on | 301×120 | 0,0,0,0 | 12_主程序资源/Sprite |
| 4411787853012002210 | 40K_settings_icon_checkmark | 66×51 | 0,0,0,0 | 12_主程序资源/Sprite |
| 5759692323890646976 | Volume_bar_inactive | 400×31 | **184,0,184,0** | 12_主程序资源/Sprite |
| -6332789735026905802 | Volume_bar_active | 64×31 | **30,0,30,0** | 12_主程序资源/Sprite |
| -1471652398003037618 | Volume_button | 110×110 | 0,0,0,0 | 12_主程序资源/Sprite |
| -2709381852941166021 | 40k_UI_bt_voicelines | 128×128 | 0,0,0,0 | 12_主程序资源/Sprite |
| 9056225148749506697 | 40k_UI_bt_play | 128×128 | 0,0,0,0 | 12_主程序资源/Sprite |
| 6553861554683527146 | 40k_bt_close | 175×174 | 0,0,0,0 | 12_主程序资源/Sprite |
| 2381704724431365035 | UI_Button_Round_background | 237×237 | 0,0,0,0 | 12_主程序资源/Sprite |
| 1838051009033412331 | 40k_main_bt_nametag | 109×41 | 0,0,0,0 (Simple 拉伸用) | 12_主程序资源/Sprite |
| -8668969440801097811 | 40k_battle_Win Skull | 66×73 | 0,0,0,0 | 12_主程序资源/Sprite |
| -295601456118903261 | Player Profile Border | 256×286 | 0,0,0,0 | 12_主程序资源/Sprite |
| -6907984259834462938 | Card Text smooth background | 32×32 | **7,0,7,9** | 12_主程序资源/Sprite |
| -3055422674097938669 | 40K_display | 442×112 | 0,0,0,0 | 12_主程序资源/Sprite |
| -3734815765473162916 | White Square | 8×8 | 0,0,0,0 | 12_主程序资源/Sprite |
| -1891211968353393973 | 40K_dropdown_arrow_closed | 46×19 | 0,0,0,0 | 12_主程序资源/Sprite |
| 5175970378912652380 | 40K_dropdown_item | 717×92 | 0,0,0,0 | 12_主程序资源/Sprite |
| 1660267235368898380 | Background | 32×32 | **10,10,10,10** | 12_主程序资源/内置资源/Sprite |
| 5837791642106728268 | UISprite | 32×32 | **10,10,10,10** | 12_主程序资源/内置资源/Sprite |
| 8385535716932718412 | InputFieldBackground | 32×32 | **10,10,10,10** | 12_主程序资源/内置资源/Sprite |
| 6419449077939965772 | Checkmark | rect(12,12,40,40) | 0,0,0,0 | 12_主程序资源/内置资源/Sprite |
| -426171492875694260 | UIMask | 32×32 | **10,10,10,10** | 12_主程序资源/Sprite |
| -2216962027063784652 | Tutorial Highlight | 128×128 | **3,2,2,2** | 03_界面UI/去重资源/Sprite |
| 7506635356624546798 | Card Ready For Level Up | 256×256 | 0,0,0,0 | 03_界面UI/去重资源/Sprite |
| -2590500462584155290 | Cursor_Space Marine_01 | 128×128 | 0,0,0,0 | 03_界面UI/光标/Sprite |
| -592821608806009055 | Cursor_Space Marine_01Glow | 128×128 | 0,0,0,0 | 03_界面UI/光标/Sprite |
| -6552398156213418268 | 40k_UI_icon_ranked_Skirmish | 128×128 | 0,0,0,0 | 03_界面UI/军队图标/Sprite |
| -6248698112513205973 | Player_Avatar_selected | 256×256 | 0,0,0,0 | 02_装饰品/头像/Sprite |
| 7052792951493041307 | UI_Description_Arrow | 333×123 | 0,0,0,0 | 12_主程序资源/Sprite |
| -6381131329509798649 | WF_Special offer_Value | 324×87 | **162,0,162,0** | (沿用权威表) |
| -4138590900053668541 | 40k_Cross_icon_cross_big Banned card | 512×512 | 0,0,0,0 | (沿用权威表) |
| -3526939114648998153 | Card Frame Cost Icon | 248×244 | 0,0,0,0 | (沿用权威表) |

---

## 十、文字资源表(全部原始 MB; TMP 对齐枚举: HAlign 1=Left 2=Center 4=Right; VAlign 256=Top 512=Middle 1024=Bottom 2048=Baseline 4096=Midline 8192=Capline)

| 元素 | m_text | fontSize (base, autoSize) | fontColor | H/V | fontAsset |
|---|---|---|---|---|---|
| LoreText | Ghazghkull Mag Uruk Thraka is a mighty prophet of the Waaagh! He is the most influential greenskin in the galaxy, and commands billions to war. | 32 (36, [18..32]) | (1,1,1,1) | 2/512 | **-8244042478085975641 = Asar-Regular SDF** |
| Header Text | Header Text | 38 (31.9, [18..38]) | 白 | 2/512 | **3485036404935369831 = Pragati-Regular SDF** |
| Continue (多卡) | Continue | 38 (36, [18..38]) | 白 | **4(Right)**/512 | Pragati |
| Auto zoom | Auto zoom | 42 (36, [29..42]) | 白 | 1/512 | Pragati |
| Music/Sound Effects/Voice-overs | … | 42 (36, [10..42]) | 白 | 1/**1024(Bottom)** | Pragati |
| Mute opponent | Mute opponent | 42 (36, [18..42]) | 白 | 1/512 | Pragati |
| OFF / ON | OFF/ON | 30 (36, [1..30]) | 白 | 2/512 | Pragati |
| Select Language | Select Language | 42 (36, [10..42]) | 白 | 1/512 | Pragati |
| Dropdown caption | (空) | 18 (14, [18..40]) | (0.67,0.67,0.67) | 1/512 | Pragati |
| Item Label | Option A | 30 (14, [18..40]) | (0.783,0.783,0.783) | 1/512 | Pragati |
| Debug Buttons | Debug Buttons | 36 | 白 | 2/512 | Pragati |
| Win/Draw/Console/Resign/Skip tutorial | … | 38 (12, [12..38]) | 白 | 2/**4096(Midline)** | Pragati |
| Match Skulls Text | "Reduce your oponents' health to win more skulls:" | 31.59 (31.59) | (1,1,1,**0.6314**) | 2/512 | Pragati |
| TipText(教程气泡) | Test tutorial tip | 36 (36, [1..36]) | 白 | 2/512 | Pragati |
| InitTipText / ContinueText(教程) | (空)/Continue | 5.2/4.5 (单位) | 白 | 2/512 | **Asar** |
| Avatar name | Avatar name | 36 (36, [12..36]) | 白 | 2/512 | Pragati |
| Name: | Name: | 35 (36, [18..35]) | 白 | 1/**8192** | **Asar** |
| Name Text | Alliance name | 40 (36, [18..40]) | 白 | 1/8192 | **Asar** |
| Title: | Title: | 35 (36, [18..35]) | 白 | 1/8192 | **Asar** |
| Title Text | Alliance name | 35 | 白 | 1/4096 | **Asar** |
| Individual rating value | ------- / ------ | 45 (31.38, [18..45]) | 白 | 1/4096 | Pragati |
| Alliance: | Alliance: | 33.4 (36, [18..35]) | 白 | 1/8192 | **Asar** |
| alliance name | alliance name | 35 (36, [18..35]) | 白 | 1/8192 | **Asar** |
| NotInaAllianceText | This player is is still not part of an Alliance | 32.9 (36, [18..35]) | (1,1,1,**0.7725**) | 1/4096 | **Asar** |
| Quantity | 2000 | 40 (36, [18..40]) | 白 | 2/8192 | Pragati |
| RatingText | 34 | 40 (36, [18..40]) | 白 | 2/8192 | Pragati |
| Новинка! | Новинка! | 27.7 | **(1,0.78,0)** 黄 | 1/4096 | Pragati |
| Запрещено | Запрещено | 0.25 (36, [0.25..72]) | 白 (fontStyle=17) | 2/4096 | Pragati |
| Created by someone fancy | … | 2.45 (2.4, [0.3..3.0]) 单位 | 白 | 2/512 | **Asar** |
| X (VO 悬停) | X | 30 (12, [12..30]) | 白 | 2/4096 | Pragati |

> 字体文件: `D:/2/解包整理/10_字体/字体资源/MonoBehaviour/Asar-Regular SDF_-8244042478085975641.json` 与 `Pragati-Regular SDF_3485036404935369831.json`。(权威表 #15 "Asar Regular Almost white" 是材质名, 字体资源本体名=**Asar-Regular SDF**; 权威表只提到一个字体 — 实际**两族**: 大字/风味/联盟=Asar, 界面按钮标题=Pragati。)

---

## 十一、与现有权威表(战斗界面JSON权威表_0827.md)逐项对照

权威表 §一 层级(FrontCanvas 子级清单): 增加确认 — ① Generic MultiCard 祖先链=Safe area FrontCanvas ✓; ② BattleDoors=主 Canvas 直属 ✓; ③ Tutorial highlight=主 Canvas 直属 ✓ (权威表列在"Tutorial highlight (inactive)" 位置含糊, 现已明确)。

权威表 §K (弹层表):
- Card Display Window 行: ✓ 结构一致; **修正**: Menu Dark Background 属本窗的=GO 631/RT 3078 (权威表写的 511/2983 属 Generic MultiCard); FlavourTextBG 1320×178 ✓; LoreText fs32 ✓ (补: base36 auto[18..32], Asar); VO 按钮 89×89 ✓ (88.655); Voice Over Button 子级 X/Image 均 inactive ✓。
- Generic MultiCard 行: Header Text fs38 ✓ 1192.4×63.2 ✓; **修正**: Content=GO 767/RT 3174 (权威表写的 GO 541/RT 2761 是语言下拉 content); BattleContinueButton 添加: 横条 Button 双组件+onClick=MulliganManager.ClickMulliganDone。
- BattleSettingsPanel 行: 面板 743×759 ✓ popup 739×761 ✓; 40k_popup border(169,160,169,160) ✓; **修正**: Auto Zoom Toggle 的 Toggle 精灵=40K_dropdown_bg(非 40K_toggle_off); Generic Close Button=GO 805(非 679)/75×75 底=UI_Button_Round_background(237×237, 权威表"名未留"已补名); Close Button 175×174→56×54 ✓; Resign/Skip 300×90 绿(0.369,0.894,0.587) fs38 ✓ 40K_button border(234,46,234,46) ✓; **修正**: Win/Draw/Console = 品红颜色(0.943,0.165,0.822) 非绿; Volume: Music/FX/Voiceover ✓ text fs42 ✓ (补 auto 10-42); ChatToggle 181×57 ✓ OFF/ON fs30 ✓; LanguageSelector ✓ 250×59 ✓ 绿(0.122,0.973,0.537) ✓ SelectLanguageText fs42 ✓。
- Tutorial 行: 1920×1080 ✓; TutorialTip 430×150 ✓(popup 高 0=内容撑开); TipText 'Test tutorial tip' ✓; ContinueText ✓(单位缩放); Pointer/light ✓ (补: Cursor 128×128→100, light color(1,0.946,0.505))。
- Alliance Panel 行: anchor(0,1) pivot(0.13,**0.825**) pos(-1.04,-25.27) ✓ (权威表 0.82 笔误); EnemyInfo 260×75 ✓; PlayerInfo pos(188.49,-50.81) 377×151 ✓ (chain ✓ x[180,557] y[62.5,213.7]); **修正**: Avatar Item Small=GO 923/RT 2837(权威表 GO 81/RT 3097 是 BackCanvas 敌名条变体); Avatar Name fs36 inactive ✓; Alliance Label fs33.4 ✓; Alliance Name fs35 ✓; NotInaAllianceText fs32.9 α0.7725 ✓ (补 auto[18..35] Asar); **修正**: Alliance Rating Display 内无 'RatingText 34' — 是 Main Icon(0×0)+Secondary(inactive)+Individual rating value('------' fs45); '34' 属 BattleDoors。
- BattleDoors 行: 结构 ✓; Background 3840×2160 α0.7529 ✓ (补: 内置 Background 32×32 border10 type1); Video Image 1920×1080 ✓ (RawImage); SkullsHolder 648×52 ✓ skull 64×71×3 ✓ (补: 三 skull 静态重叠, 40k_main_bt_nametag 109×41 拉伸); Quantity '2000' ✓ Trophy 60×60 ✓ RatingText '34' ✓。
- LoadingMenu/调试/教学高亮/Anim Anchors 行: 已补全(Anim Anchors 五锚点绝对坐标见 §八)。

---

## 十二、矛盾清单(一律以原始 JSON 为准)

1. **Auto Zoom Toggle 贴图**: 任务书+权威表 = 40K_toggle_off(301×120); 原始 GO 1061 m_Sprite=-5728790147372056906 → battlearena1_sprite_map.json = **40K_dropdown_bg**(119×102, border 23,20,23,20)。已按原始改。
2. **Win/Draw/Console 颜色**: 任务书"同绿"; 原始 = 品红 color(0.943,0.165,0.822)。绿只用于 Resign/SkipTutorial/多卡 Continue 条。
3. **Content(GO 541/RT 2761)归属**: 权威表 K 行挂在 Generic MultiCard; 原始父链 = BattleSettingsPanel→LanguageSelector→LanguagesDropdown→Template→Viewport 的子级; GenericMultiCard 的 Content = GO 767/RT 3174 (-1917.7×0)。
4. **Avatar Item Small**: 权威表"GO 81/RT 3097 213×186" — GO 81/RT 3097 (155.64×136.49, interactable=0) 是 BackCanvas→LeftArea→EnemyInfo(280)→EnemyName(409) 下的敌名条头像; 联盟面板头像 = GO 923/RT 2837 (212.54×186.39)。
5. **Alliance Rating Display 内容**: 权威表"RatingText '34'" — '34' 在 BattleDoors/RewardsHolder/HolderRating/RatingText (GO 1071); 联盟面板 Rating = Individual rating value '------'/'-------' fs45 占位。
6. **Menu Dark Background 归属**: 同名同值两实例 — Card Display Window 用 GO 631/RT 3078(father 3030); GenericMultiCard 用 GO 511/RT 2983(father 3175)。权威表只列 511/2983 且放在 Card Display Window 条目下 — 归属混淆。
7. **Generic Close Button**: 权威表 "GO 679" — GO 679 实为 BackCanvas Energy Player Accumulation OFF; 本按钮 = GO 805/RT 3006, anchor(0.5,0.5) pos(364.51,345.3)(非某处锚定), 底图名 = UI_Button_Round_background。
8. **CardUI (1)/base active 状态**: md 树标 "CardUI (1) (inactive)"、任务书说 "base inactive" — 原始: Cards 下 5 个 CardUI(含 base GO 705)全部 m_IsActive=True; md 树那个 inactive 的 CardUI (1) 是 pid 1013(父链=CemeteryGroup, 墓地日志面板的模板)。
9. **DebugButtons 的 m_IsActive**: dump_go_tree 输出无 inactive 标记(双份 pid 文件扫描顺序导致), 原始单份文件 m_IsActive=**False**。同样 dump 的 "CardUI (1)" 等也受此影响 — 本报告全部经单份文件复核。
10. **Tutorial Tip 高度**: 任务书/权威表 "430×150"=同值; 但 popup 本体 Generic Popup Background sizeDelta=(436.25,**0**) — 高度由内容撑开(复刻时不能写死 150)。
11. **BattleContinue Button 的 onClick**: 权威表结论"场景内全部 Button m_OnClick 全空"不准确 — 多卡 Continue 的主 Button(MB 4460)onClickCalls=1 → **MulliganManager.ClickMulliganDone**; 其余按钮确实全空。
12. **联盟面板根 pivot**: 权威表 (0.13,0.82) vs 原始 y=**0.825**; 且面板绝对 y[-57.9,417.5](顶部出屏 58px)。
13. **字号/对齐补全**: 权威表未记 VAlign 枚举(512=Middle/1024=Bottom/4096=Midline/8192=Capline)、autoSize 区间、HAlign(4=Right 仅 'Continue' 用) — 本报告 §十 已全量记录。
14. **字体族**: 权威表 #15 只记一个字体 pid; 实际两族 — Asar-Regular SDF(-8244042478085975641) 与 Pragati-Regular SDF(3485036404935369831), 且权威表所列材质名 "Asar Regular Almost white" ≠ 字体资源文件名 Asar-Regular SDF。
15. **Anim Anchors**: 权威表/任务书坐标 (836,946)/(-1813,1041)/(479,947)/(716,947) = offset 局部值(正 x=向右、946=自顶), 与 chain_rect 绝对(y 向下的 Godot 屏幕坐标)语义不同 — 以 §八 绝对值为准。
16. **Match Skulls(设置面板子级)**: md 树与权威表均缺失 — GO 169/RT 2836(inactive), 绝对位置在面板下缘之外(y 968.6-1117.4), 文字 "Reduce your oponents' health to win more skulls:" fs31.59 α0.6314。
17. **TutorialArrows / Card Info / RT 1433**: RT 1447/1356/1261/1415/1388/1422/1433 等 pid 在 RectTransform/ 目录无导出文件(md 树显示其子树但原始 JSON 无) — 属运行时实例化或导出缺失, 复刻时这些子树按运行时生成处理(参照权威表第三节 2DCard 模板)。

---

## 十三、重建最难踩的坑(按重要性)

1. **单位尺寸卡模板**: CardUI/2DCard/CreateCard ContinueText 等全部用"卡宽比例单位"(2.5437×3.3686), 场景中没有实际像素尺寸 — Godot 必须用 Control 尺寸乘以运行时卡宽, 不能照搬 offset。
2. **Card Display Window 底部条结构**: LowerSection pivot(0.5,0) pos(0,-518.34) — Flavour 区在卡展窗下缘以下(y 921-1078), VO 按钮(88.7)在 Flavour 区右侧 x 1650-1739 — 与卡展窗(x 584-1336)分离; 卡展窗整体高 868 居中。
3. **Menu Dark Background 是 4574×2572 的巨幅黑色 9 常规 Image**(sprite=0 纯色 α0.7725) — 不是 shader 覆盖; 两个弹层各有一份。
4. **BattleSettingsPanel 的 9-slice**: 40k_popup(359×336)→739×761, border 169/160 — 窗口四角各 169px 边框拉伸; Background fill 是 Tiled 纹理层(40k_popup_texture 128×128); 复刻= NinePatchRect + TileTextureRect。
5. **音量滑条尺寸走 anchor 比例**: Slider anchor(0.1,0.33)-(0.9,0.45) — 滑条实际宽 = 容器 0.8 宽、位于行内 33%-45% 高度带(手柄 46.8×22.4 不全高); 三行高 100, 行标题在 anchor(0.1,0.45)-(1,1) 区域(VAlign=Bottom)。
6. **下拉列表 Template 高 574、item 高 40.87** — 语言下拉展开=574px 高(超出面板), ScrollRect Clamped; 列表内容(content 41.7 高)运行时由脚本填。
7. **联盟面板挂屏外**: 根 y[-57.9,417.5] 顶出屏, pivot(0.13,0.825) — 面板内容(EnemyInfo 组)实际显示在 y 44-357 区间; NotInaAllianceText(α0.7725)与玩家信息组在无联盟时显示。
8. **BattleDoors 的 4 层**: 黑幕(9slice border10 的 32×32 内置 Background 拉到 3840×2160!) → VideoImage(RawImage 运行时 set 纹理) → AllRewardsHolder(骷髅条+奖励条, 全部静态重叠, 运行时横排) — 三骷髅、RewardHolder/HolderRating 各自重叠, 必须运行时布局。
9. **教程层 3 个子层 active 状态**: Tutorial 根 active, 但 InitTutorialTip/TutorialPointerCombat/Bg(966)/ContinueText(1164) 皆 inactive; TutorialArrows 无 RT 导出; pointer 动效用 Cursor 128×128.
10. **双份 pid 文件陷阱**: 场景每个类型都有 `Name_pid.json` 与 `Name_pid_pid.json` 双份, dump_go_tree 按目录扫描序取先 — m_IsActive/m_Name 可能取自拷贝; 权威读数一律先"单份优先"再复核(battle_front_extract.py 已实现)。
11. **TMP 对齐枚举**: HAlign 1/2/4 = Left/Center/Right; VAlign 256/512/1024/2048/4096/8192 = Top/Middle/Bottom/Baseline/Midline/Capline — Godot 无 mid/capline 概念, 垂直居中近似取 Middle 或按行高微调(Skip/Resign 文本用 Midline=4096)。
12. **两族字体**: Asar-Regular SDF(风味/联盟/END TURN/教程大提示) vs Pragati-Regular SDF(面板按钮标题) — Godot 需两个 TMP 字体资源; cardinfo 内 CreatedByText 用 Asar 单位缩小。
13. **原版笔误勿"修正"**: Match Skulls Text "oponents'"、NotInaAllianceText "is is" — 按原文复刻。
14. **DebugButtons 层其实 3 组**: Player 组(右下 y 666-915)/Enemy 组(右上 y 96-309)/Card Picker(中部 1400×800) — 各自按钮部分 0×0(运行时布), 不必逐一复刻像素。
15. **GraphicRaycaster 挂在面板根**: BattleSettingsPanel(GO 803 MB 4689)和内嵌 BattleDoors Canvas 各挂 GraphicRaycaster — Godot 对应 Control 的 mouse_filter 拦截。

---

## 十四、关键数值 TOP 20

1. 全部弹层挂 Safe area FrontCanvas(RT 2862) 或主 Canvas(RT 2759) 下 — 两个父级, 别挂错。
2. FrontCanvas Canvas_2569 = ScreenSpaceCamera + **OverrideSorting=True**; BackCanvas 2567/2575=Overlay; BattleDoors 内 Canvas_2574=Overlay+**OverrideSorting=True**。
3. Card Display Window: 752×868 居中 x[584,1336] y[106,974]; 遮罩 4574.6×2572.4 α0.7725。
4. LowerSection 1320×137 y[921.3,1058.3] — 卡窗下条; FlavourTextBG 1320×178 y[900.8,1078.8]; LoreText 1250×93.5 fs32 Asar。
5. VO 按钮 88.655×88.655 x[1650,1738.7] y[945.5,1034.2] (40k_UI_bt_voicelines 128×128)。
6. Generic MultiCard: 全宽 x[0,1920] y[131,949] 高 818.04 — 锚 (0,0.5)-(1,0.5); 自身 ScrollRect 横向 Elastic。
7. Header Text 1192.4×63.2 x[363.8,1556.2] y[131,194.2] fs38。
8. CardUI Reference: unit 2.5437×3.3686 @anchor(0,1) pos(1.2717,-409.02)。
9. BattleContinueButton x[1062.9,1895.8] y[952.2,1002]; 横条 577.5×63.8 (40k_bt_underbutton border 240,0,240,0 绿 0.369,0.894,0.587); 圆钮 40k_UI_bt_play 80.5×79.6 x[1723.2,1803.7] y[937.3,1016.9]; onClick=ClickMulliganDone。
10. BattleSettingsPanel x[588.4,1331.6] y[190,948.6] 743.2×758.6; popup 739.4×760.8 9-slice(169/160)。
11. 关闭钮 75×75 @x[1287,1362] y[186.5,261.5](UI_Button_Round_background 237×237; 40k_bt_close 175×174→56×54)。
12. Auto Zoom 行 x[677.6,1150] y[282,357.7]; Toggle 74.06×57.67(40K_dropdown_bg border 23/20) @x≈[714.6,788.7]; Label fs42。
13. Debug 按钮组 x[590.3,1337.4] y[2.4,140](面板上方贴屏顶!) Win/Draw/Console 各 249×100 品红(0.943,0.165,0.822) 40K_button border 234/46。
14. Volume 组 x[607.3,1308.7] y[368.5,809.9]; 三行 100 高; 滑条 anchor(0.1,0.33)-(0.9,0.45) max=1 value=1。
15. ChatToggle 背景 180.76×56.8(40K_toggle_off/on 301×120) x≈[797.6,978.4]; OFF/ON fs30; Checkmark 用 Volume_button 110×110→57.9×46.2。
16. 底钮 Resign x[638.3,938.3]、Skip x[981.7,1281.7] y[834.8,924.8] 300×90 绿 fs38。
17. 语言行 x[677.6,1257.3] y[222.6,282]; dropdown 250×59.4 绿(0.122,0.973,0.537)(40K_dropdown_field_closed border 60/35); Template 高 574。
18. TutorialTip 430×150 x[749,1179] y[466,616]; popup 436.25×0(高动态); TipText fs36 自动缩放; TutorialPointerCombat 全屏 + Pointer 100×100 @x[875.6,975.6] y[549.8,649.8]。
19. 联盟面板 x[-107,708] y[-57.9,417.5] pivot(0.13,0.825); 敌头像组 212.5×186.4; 玩家信息 377×151 @x[180,557] y[62.5,213.7]; 联盟行 551.6×136 @x[23.1,574.7] y[220.6,356.5]; NotIna 592.7×46.2 @y[252.4,298.6] α0.7725。
20. BattleDoors: 黑幕 3840×2160 α0.7529(内置 Background 32×32 border10 9-slice); Video 1920×1080 @x[-6.1,1913.9] y[-58.7,1021.3]; 奖励组 @x[909.9,1009.9] y[755.3,855.3]; 骷髅条 648.1×52.4 @x[585.9,1234] y[829.1,881.5]; 奖励条 495.2×49 @x[662.3,1157.5]; '2000' fs40 / '34' fs40 / Trophy 60.1×60。

---

**覆盖统计**: 10 组根全部完整读取(dump_go_tree 全树 10 份 + battle_front_extract.py 原始精确提取 190+ 节点 GO/RT/MB + chain_rect 61 节点绝对坐标 + 精灵 m_Rect/m_Border 38 条 + TMP 文字 32 条 + Canvas 组件 5 份)。与原权威表结论一致的条目已在 §十一标注 ✓; 15 条矛盾/修正见 §十二; 重建警示见 §十三。
