# 批次 1 · 查证 V4a —— 壳与共用件口径（7 问）

> **只读查证**（没跑 Unity / 没动 git / 没改任何代码或正本）。日期 2026-10-10。
> 现读基线：`Shell/{WindowsManager,TransformScalerBySmallScreenUI,MenuDraw,MenuScroll,PromptPopup,PracticeModePopup,CardDetailPopup,PlayerProfileWindow,CollectionWindow,SocialWindow,MainMenuRuntime,RankedEventWindow}.cs` ·
> `Battle/Label.cs` · `Battle/ImageQuad.cs` · `Editor/{SettingsScene,ShellScene}.cs` ·
> 原版：`d:/2/tools/decomp_full/GameWindow__{Close,TryOpen,UnHide,Hide}.c` · `WindowsManager__{OpenWindowCO,CloseWindowCO,CloseAllWindows,HideAllWindows,ShowPreviousWindow}.c` + `WindowsManager.__c___HideAllWindows_b__49_0.c` ·
> 本地真 uGUI `MyGame/Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/ScrollRect.cs` · 原版 prefab 用 `工具/menu_dump.py` **亲跑**。
> ⚠️ **行号漂了**（本仓常态）：A228 判据的 `WindowsManager :330-342` ⇒ 实际 **:344**；W2 报告的 `PracticeModePopup :1497/:1548` ⇒ 实际 **:1587/:1640**；波 B4 的 `Label :606/:616` ⇒ 实际 **:619/:629**；A286 简报的 `PromptPopup :567` ⇒ 实际 **:602**。

## Q1 · A228（小屏缩放 × `Align*On`）

**现状（现读）** `WindowsManager.TryOpen`（`:316-324`）末尾调 `:344 ApplySmallScreenScale()` → `TransformScalerBySmallScreenUI.Tick()` 给**窗口根**乘 M。
`Battle/Label.cs`：`AlignLeftOn:619` / `AlignRightOn:629` 都写 `localPosition.x = worldX − transform.parent.position.x ± WorldW*0.5f`；
`WorldW = _tmpW = |textBounds.size.x|`（`:42`，TMP **本地**尺度、**不含父链缩放**；字段名叫 `WorldW` 是误导）。
**机制（只错一项，不是"两项不同量纲"）**：`localPosition` 是**父局部**坐标，而 `worldX − parentX` 是**世界**差 ⇒ 缺 `/ 父链 lossyScale`；
`WorldW*0.5f` 这一项**单位本来就对**（局部长度）—— 所以真正要除缩放的只有位移那一项。
精确式：设 M = 窗口根倍数、`nl` = 字的父节点相对窗口根的**设计**空位、D = `worldX − 窗口根世界 x`：
正确 `l = D − nl + W/2`（与 M 无关）；现写 `l_code = D − M·nl + W/2` ⇒ **偏差(local) = (1−M)·nl = (1−M)×(父到窗口根的世界距离)/M**。
⇒ 判据原文那句 `(M−1)×(父到 pivot 距离)` **方向与量纲都对**，只是没写明 **pivot = 窗口根的 `transform.position`**（不是 RectTransform pivot）。
**默认关吗**：✓ `SmallScreenUI` 静态构造 = `PlayerPrefs.GetInt(PrefKey, 0)`（默认 0），原版 `GameStaticData__.cctor` = 0 ⇒ 出厂关、今天不发作；但**设置窗那颗开关是活的**（玩家一点就开）。
**"没有任何断言会红"**：✓ **成立** —— 自检**确实开过**这个开关（`Editor/SettingsScene.cs:1248 Set(true)`，`:1283/:1319` 收回；`:372-373` 用 `PersistOverride`+`ResetForTest`，不动玩家真设置），但那一节量的是 `localScale` 与一颗 quad 的 `MeshRenderer.bounds` 宽度，**没量过任何对齐文字**。
**非 1 的窗（现读全量）**：`SettingsWindow 1.2`(`:412`) · `BoosterInfoPopup 1.2`(`:226`) · `DuelPopupWindow 1.15`(`:114`) · `PlayerProfileWindow 1.075`(`:202`) · `PracticeModePopup 1.07`(`:650`) · `TrophyInfoPopup` 烤 `1.35`。
🔴 **判据原文逐句核**：①窗口清单里 "**活动窗 1.25**" **查不到**（`LiveOpsEventWindow.cs:320` 现读 = `1f`）②**漏了 `BoosterInfoPopup` = 1.2**；其余四个 ✓。③`Battle/Label.cs` 那句「**只减位移、不除缩放** —— 本工程 Shell 这一线的父链**不带 `transform` 缩放**」**已过期**（A165 之后会带）。
**原版判据**：原版对齐靠 TMP 自身 `m_HorizontalAlignment`（在**文本本地空间**排），缩放由窗口根 `localScale` 统一乘 ⇒ 不存在"设计值/世界值"两套坐标。我们这套"渲染后量宽再挪节点"本来就是**等价物**（`Label` 注释自述）。
**可选做法**：
- **(a) `Align*On` 里除 `lossyScale`**：`l = (worldX − parentX)/k + W/2`。**只有窗口根在世界原点时精确**（精确式见上；不引 root 会把 `worldX` 也除错 ⇒ 当 root.x≠0 时多出 `(1/k−1)·worldX`）。动**共用件**（`Align*On` 全仓 ~105 个调用点、战斗侧也在用）⇒ 按铁律 12 要跑全套 11 条。
- **(b) 调用侧传"缩放后的值"**：**按字面不成立** —— 只缩 `worldX` 会引入新偏差 `(M−1)·W/2`；真要做等于把契约改成"父局部 x"、105 处全改。
- **(c) 让对齐**在窗口根那一层做**（把目标点先映射成视觉世界点、再 `parent.InverseTransformPoint` 回父局部）：精确、与 root 位置无关。等价于"契约改成父局部 x"，改动面与 (b) 同量级。
- **(d) 不改、只记账**：撞铁律 11 ⇒ 不建议。
**⚖️ 我判不了**：(i) `worldX` 的**契约**是"设计世界 x"还是"视觉世界 x"（M=1 时重合；函数头写"世界 x"、`LayoutSpace.FromPixel` 给的是设计值）—— **契约不定，任何修法都只在一种情形下对**；(ii) `lossyScale` 取**父**的还是**自己**的（中间层有非单位缩放时不等；本壳有没有那种窗**我没查**）。

## Q2 · A217 ①②③（`GameWindow` 三笔）

**原版逐句（我读的反编译，不是转述）**：`GameWindow__Close.c` = ①null ②`gameObject` ③**`if (!activeSelf) return;`** ④播关窗音（0x30/0x38，**我们没有**）⑤`WindowsManager.CloseWindow(this)`（**记账在 manager 那一侧**）。
`GameWindow__TryOpen.c` = `CurrentState != Closed ⇒ (state=Open + ToFocus) 后 return`（**不 SetupData、不 Open**）；`== Closed` 才 `SetActive(true) + state=Open + virtual[0x178](=Open)`。
`WindowsManager__OpenWindowCO.c` = 全屏支 **`if (openWindows.Count>0) HideAllWindows()`**；弹窗支 `currentWindow.ToBackground()`；**两支之后统一 `set_CurrentWindow(win)`**（弹窗也写）。
`__HideAllWindows` 的 predicate（`…b__49_0.c`）= `存活 && *(int*)(w+0x68) != 0` ⇒ **只藏 state≠Closed 的**，`Hide()` 也不动 manager 字段。

**① 缺首句守卫**：**谁在"本来就没开"时调 `Close()`？现读生产路径 = 0 个** —— 全壳唯一把窗根置 inactive 的地方就是 `Close()` 自己（`grep SetActive(false)` 命中的 `SearchingMatchPopup`/`BattleDoors`/`ChatPopupPanel`/`Tooltip` **都不是 `GameWindow`**），而 `Close()` 恒把记账清干净（`NotifyClosed` 摘表 + 清两格）⇒ **加上今天零可观测差异**（与 A217 原文一致）。
四处 `Close` 覆写（`SearchingOpponentWindow:146 ReleaseHold` · `LiveOpsEventWindow:342` 取消搜索 · `MissionRerollPopup:409` 换回页签 · `SettingsWindow:465 ApplyIfDirty`）都在 `base.Close()`**之前**跑，守卫加在基类里**不会**掐掉它们。
代价（会变的地方，今天都不发生）：`CloseAllWindows():739` 会顺手"清掉 inactive 但还在表里的窗"；加守卫后跳过 ⇒ 表尾仍 `Clear()`、**只少一次 `NotifyClosed`（= 少一次 `ShowPreviousWindow`）**。
**⚖️ 我判不了**：要不要**配套**加一条"inactive 却在 `openWindows` 里 ⇒ 出声"的断言 —— 不加的话，守卫等于把"兜底"换成"静默留脏"。

**② `TryOpen` 同窗再开会重建**：`SetupData(data)+Open()` 恒跑（`:317-321`）；原版不跑。
**依赖"重建"的站（逐处现读全部 `OpenWindow` 调用点）只有两类**：
- `Shell/CardDetailPopup.cs:243 ShowCard`（已知）；
- `Shell/MainMenuRuntime.cs:440 OpenByRef`（9 条入口的**复用支**）—— 其 doc 明写"同一个实例、`Open()` 重建内容"，其中 Social/Rewards/Collection/Shop 4 条带 `closeAll:true` ⇒ 复用那一支**先被 `CloseAllWindows()` 关一次**再重开（`Close()` 只 SetActive(false)、不销毁内容；但 `Open()` 里那些"按当前存档重铺"的逻辑是入口语义的来源）。
其余 20+ 个 `OpenWindow` 站点**全是新建实例**（`X.Create(...)`）⇒ 不受影响。
两案代价：**(甲) 保留重建 + 如实标注**＝现状、零风险，偏离留着（铁律 11）。**(乙) 照原版不重建 + 给 `CardDetailPopup` 另开换卡路**：要动 `CardDetailPopup`（新增显式 `Rebuild/Reload`，`ShowCard` 改调它）+ `OpenByRef` 复用支（显式重建或接受内容陈旧）+ 一批"复用后内容是新数据"的断言要改口径；最毒的是**静默**（换了卡但卡面没换 / 8 条入口显示旧数据）。
**⚖️ 我判不了**：**原版"不重建"时那 9 条入口靠什么刷新内容**（`updateNavPanel`？`OnEnable`？还是根本不刷）—— 没查；它决定 (乙) 是"照原版"还是"照原版但缺一条链"。查法：真 Play 连点两次 SHOP 看内容变不变，或读那几扇窗 prefab 的 `m_OnEnable` 绑定。

**③ 弹窗不写 `currentWindow` / 全屏只 `Close` 不 `HideAllWindows`**：现读 ≥2 处会变行为 ——
- **ESC 打到谁**：`Shell/PointerLayer.cs:597` 用 `wm.TopWindow`（`:640` = `popUpWindow ?? currentWindow`）。今天"**全屏盖在弹窗上**"时（现成站：`Shell/RankedEventWindow.cs:243` 开全屏 `SearchingOpponentWindow`，而本窗是 Popup）⇒ `popUpWindow` 仍指着底下那扇 ⇒ **`TopWindow` = 被盖住的那扇**（与"最上面那扇"矛盾）⇒ ESC 关错窗。照原版改（两支统一写 `currentWindow`）**立刻变行为** ⇒ `Editor/ShellScene.cs:1339-1361` 那三条三态断言要按新模型重写。
- **全屏窗开时谁被收掉**：原版 `HideAllWindows()` 连**弹窗一起藏**；我们只 `Close()` 当前主窗 ⇒ **弹窗照旧可见**（全屏窗盖不住弹窗）。站 = 任何"弹窗开着时开全屏窗"的路径。
- 附带差（已在 A123 记过）：原版关窗会 `Destroy` + 释放 addressable，我们只 `SetActive(false)`。

## Q3 · A198 ①③（+ ② 只核一句）

**① 现状 = 判据已过期（应销账）**：`MenuDraw.PaddedClip`（`:262`）首行 `if (!clip.HasValue || pad == Vector4.zero) return clip;`，而 `Hit`（`:1361`）与 `DeckCell`（`:1944`）**都**走 `ClipRect(r, PaddedClip(clip, maskPad), out hr)` ⇒ **`Clip==null` ⇒ pad 不生效 = 原版那一支（无 mask 即无 padding）**。
A198① 描述的是 **A188（2026-10-08）之前**的旧写法 `ClipRect(PaddedHitRect(r,hitPad), clip, …)`（A188 的订正注释就在 `MenuDraw.cs:1351`）⇒ **① 已被 A188 顺带修掉**。
**站点现读**：全工程非零 `ClipPad` **只剩一处**（`Shell/ForgeTab.cs:251` `TrackPad=(10,0,0,0)`），且与 `Clip` **成对设/还**（`:665-676`）；`maskPad:`/`hitPad:` 在生产代码里 **0 个调用点传非零值**（grep 只命中注释）⇒ **今天没有任何站点吃到这个差**，改成"无 clip 即无 padding"= 现状 ⇒ **无需改**，只需留 `Editor/RewardsScene.cs:1907` 那条改坏法。
**③ `DeckCell` 的 `maskPad` 无包装**：**谁在用** = `Shell/CollectionWindow.cs:2504 BuildDeckCell`（收藏窗卡组页）+ `Shell/DeckSelectionPopup.cs:490`（选卡组弹窗）—— **两处都在 `GameWindow` 族里**（A78② 已把三兄弟上移到 `GameWindow`，`WindowsManager.cs:105-118`）⇒ "够不着"这个理由**已不成立**；两处都**不传** `maskPad`，且两处视口原版 `m_Padding` 都是 `(0,0,0,0)` ⇒ **补包装 = 零行为变化**（纯 API 口径）。
**⚖️ 我判不了**：要不要给 `DeckCell` 加"用本窗 `Clip`/`ClipPad`"的包装 —— 得先定**怎么表达"我就是要不裁"**（`PxRect?` 已是 `null = 不裁`，包装只能靠"重载 + 命名"区分）。**这条请调度台裁。**
**② 只核一句**：它是 **A78② 的续篇、不冲突** —— A198 自述"2026-10-07（A78② 落地时报出）"，A78 原文在 `资料/待办判据_1006.md:42`；A78② 解决的是"**够不够得着**"（状态搬到 `GameWindow`），A198② 剩的是"**同一扇窗两个视口 pad/softness 不同时表达不了**（要可入栈）"⇒ 两件是上下游关系，不打架。今天生产上非零 pad 只有锻造轨道一处 ⇒ 不发作。

## Q4 · A258（三处 `wrap` 缺省）

**现状**：`PlayerProfileWindow.cs:576 ProfilePage.Text` `wrap=false` · `CollectionWindow.cs:1613 TextAligned` `wrap=true` · `SocialWindow.cs:316 SocialPage.Text` / `:428 SocialWindow.Text`(转调) `wrap=true`。
**逐处真值（我本机 `工具/menu_dump.py` 亲跑，2026-10-10；命令 = `python 工具/menu_dump.py bundle_menus_assets_all "<根>" --depth N --md`）**：

| 原版根 | 折行=1 | 折行=0 | 折行=3 |
|---|---|---|---|
| `Player Profile Window`（depth 16） | **112** | **44** | 1 |
| `Social Submenu Variant`（depth 16） | **56** | **44** | 3 |
| `Collection Menu Variant`（depth 14） | **26** | **28** | 3 |

⇒ **三族原版取值本来都是混的（≈50/50）** ⇒ **"统一到哪一套"是伪问题**：缺省值**不是原版概念**，它只是我们给"没显式声明的调用点"兜的底；原版只有**逐个节点**的真值。
**缺省今天真的在载荷的两处（现读 + 现读原版）**：
- `TitleTab` 的 `Select Avatar Button/Button Text`（`'Selecionar'`）不传 `wrap` ⇒ 靠 `false` 缺省；原版现读 = **`字号=36.0 auto[10.0~36.0] 对齐=Center/Capline 折行=0`** ✓ 缺省**对**。
- `TitleTab` 格子 `Name`（模板 `Title Drawer Horizontal Variant/Content/Label/Name`）不传 `wrap`；原版现读 = **`字号=19.0 auto[12.0~75.0] 对齐=Center/Midline 折行=0`** ✓ 缺省**对**。
⇒ **把 profile 那个缺省改成 `true` = 当场回归两处**。
- `CollectionWindow.TextAligned` 的站点：筛选格走数据 `c.LabelWrap`（`:1554`）；搜索框 `:1677` **用缺省 `true`**，而原版那一格是 **3** ⇒ 缺省在**这处本来就是错档**，已另立 **A249**。
- `SocialWindow` 一族：`AlliancesTab` 15 处里 4 处原版 `0`（A213 已显式传 `false`）· 11 处 `1`；**`FriendsTab`(4) + `AllianceMemberTab`(9+4) 共 17 处仍未逐条核原版**（现读**全用缺省**）⇒ 这才是真待办。
**可选做法**：(a) 三处缺省各留各的 + **逐处显式声明**（补完 social 那 17 处后缺省值就**不再载荷**）；(b) 往任一边统一 ⇒ **静默改掉另一边的站点**（改 `true` 打脸 profile 两处已核对的站点，改 `false` 打脸 social/collection 的多数）⇒ **不可取**；(c) 唯一有依据的"统一" = **去掉缺省值、让形参必填**（编译期逼每个站点声明），代价 = 50+ 处逐条现读（profile 那 50 处已核完、social 17 处未核）。
⇒ **建议 A258 改写成"去缺省 + 逐处现读"，或销账并把尾巴并入 A213（social 17 处）**；⛔ 不要"统一缺省值"。

## Q5 · A286（软边切块 × 悬停换图）

**今天 `ApplySoftEdges` 到底是哪一种（现读 `MenuDraw.cs:458-590`）**：**既切几何、又建独立子件** —— 主格落在**原 quad** 上（`PlaceCell(q, mainCell, vis, uv0)` ⇒ 改矩形 + 改 uv）；其余格 **新建 `ImageQuad`**（`ImageQuad.Create(q.transform, q.Texture, …, baseName+"_soft"+i+j)`），各自 `SetTint/SetRenderQueue` + `q.SoftEdgeRegister(sub)`。⇒ **不是共享材质、也不只改 uv**。
**根因（本报告新增）**：`WindowButton.SwapTo`（`PromptPopup.cs:602`）→ `SetOn`（`:609`）只对 `target`(+`_nine`) 做 `q.SetTexture(tex)` + `q.SetAspect(_targetAspect)`；而 `ImageQuad.SetTexture`（`ImageQuad.cs:156-160`）**不发 `NotifySoftEdgeChanged`**，`SetAspect`（`:231-235`）**首句就是 `Mathf.Approximately(_aspect, aspect) ⇒ return`** ⇒ **常态图与高亮图同比例时（按钮图集通例）那条重切链根本不触发**，子块停在旧图。⇒ 不是"忘了登记"，是**重建的触发条件恰好不成立**。
**可选形状**：
- **(a) `SetTexture` 把图转给登记过的子块**（照 `SetTint` 既有形状，`ImageQuad.cs:192-200`）。代价：**必须绕开 `_aspect`**（`SetTexture` 会把子块比例冲成"贴图自己的"，而子块是**细条**，比例是切出来那块的 —— 同 `WindowButton.SetOn` 那句注释记的坑）⇒ 需要"只换图、不动 `_aspect`"的写法。只对"有子块的宿主"生效 ⇒ 其余 quad 零影响。
- **(b) 换图后显式触发重切**：给 `ImageQuad` 加公共口（现只暴露 `SoftEdgeRebuild` 的 getter）或让 `SetAspect` 早退前也通知。代价：**每次悬停进/出都销毁+重建 N 块**（分配抖动、子块**对象身份每次都变** ⇒ 按引用抓的断言会失效）；好处是几何/uv/图一次重算。
- **(c) 让 `WindowButton` 认识子块**（`SwapTo` 遍历登记表）：等于把 (a) 挪到调用方，仍要处理 `_aspect`，收益相同、耦合更差。
- ⛔ **`BindNine` 不可用**（A286 已实测：它给每块 `SetAspect(主格比例)` ⇒ `WorldW = _worldH×_aspect` ⇒ 细条被拉成整条宽）。
**⚖️ 我判不了**：(i) 走 (a) 还是 (b) —— 取决于"HoverArt 与常态图**是否同尺寸**"（同尺寸 ⇒ (a) 零几何风险），**我没逐张核那 630 颗 SpriteSwap 的两张图尺寸**；(ii) 选 (a) 后要不要用 `SoftEdgeKidCount`（已有）补一条"换图后子块也换了"的断言。

## Q6 · A233（`clip.HasValue` 那半的断言）

**现状**：`PracticeModePopup` 两处闸现读在 **`:1587`(ImgTex) / `:1640`(Txt)**（简报的 `:1497/:1548` 已漂）；`MenuDraw.ClipText`（`:771`）**自己首句就是 `if (lb == null || !clip.HasValue) return false;`** ⇒ 对 `Txt` 那条闸**已是同义反复**（调与不调结果相同）⇒ **"`clip == null` 时到底叫没叫 `ClipText`"这件事在行为上不可观测**。`ImgTex` 那半**不能删闸**（`ApplySoftEdges` 收的是**非空** `PxRect clip`）。
**要验的其实只有这样一句**：`clip` 有值 **且** `softness == (0,0)` 时那一刀真的落 —— 而今天**两处调用点 softness 都非 0**（`ArmyClipSoft (0,50)` / `DeckClipSoft (0,23)`）⇒ **要看这一档就必须有一个走 `(0,0)` 的调用实例**。
**可行形状**：① 加 `PracticeModePopup.ClipSoftnessOverride`（W2 草案）。**与 A185 是不是一回事**：A185 的裁定原文（`资料/待办判据_1007.md:30`）=「**不为不可达支路给生产类开测试注入口**（按如实标注处理）」，针对的是一条**原理上不可达**的死代码；A233 这条**可达、只是今天没有实例** ⇒ **不是同一件事，但落在同一条纪律的射程**（同样是"往产品类加 static 测试口"换一条今天不可观测的行为）。
② **不开口的等价办法（我倾向）**：把 `Txt` 那道**冗余闸删掉**（直接 `MenuDraw.ClipText(lb, Clip, ClipSoftness)`，判据 = 前置条件由被调方 own）⇒ **没有闸就没有闸要断**；`ImgTex` 那半保留闸，其行为**完全由 `MenuDraw.ApplySoftEdges` 承担**，牙口已经在 `Editor/ShellScene.cs` 的 ⑤·d-2/⑤·d-3（"零 softness 也硬裁"，改坏法 = 把那句早退加回去）。③ 退一步的**真行为**断言（不带闸）：照 W2 §四 的夹具开练习窗，断**卡组格内各层并集**不越视口（`UnionQuadRect`）+ 名字条渲染宽 ≤ 框宽 —— 今天就该绿、能抓"整层画到视口外"，但**证明不了闸**。
**⚖️ 我判不了**：要不要为这半开注入口（判别力 vs 往产品类加口）。**请调度台裁** —— 我给的倾向 = 删冗余闸 + 用 ⑤·d-2 / ⑤·d-3 当牙口、**不开口**。

## Q7 · A268②（`MenuScroll.SetOffset` 页签关着也通知）

**现状（现读）**：`:183 SetOffset` → 夹取 → 值没变(<0.01)早退 → `Offset = v; if (OnChanged != null) OnChanged();` ⇒ **同步、立即**；订阅方一律是**整页重建**（`ForgeTab:316 BuildRewardCells` · `CampaignTab:262 RefreshNodes` · `CollectionWindow:626/2221` · `ChatPanel:485 Rebuild` …）。
**原版有没有这条链（判据 = 本地真 uGUI 源码，我现读）**：`ScrollRect.SetNormalizedPosition`（`ScrollRect.cs:1063-1083`）**只改 `m_Content.anchoredPosition`、不发事件**；`m_OnValueChanged.Invoke(...)` 在 **`LateUpdate`**（`:891-896`），条件是"view/content bounds 或 anchoredPosition 与上一帧记录不同"；`OnDisable`（`:585-599`）只反注册 + 清拖拽/速度，**不补发**。
⇒ **原版这条链是帧驱动的：窗口/页签 GO inactive ⇒ `LateUpdate` 不跑 ⇒ 不通知** ⇒「页签关着时值变化照样触发重建」**原版没有**；我们是同步通知，所以有。
**今天真在关着时设过值吗**：**没查到**（`FocusOn`/`SetOffset` 全部站点 = `ForgeTab:685/696` · `CampaignTab:563` · `DailyStreakPopup:313` · `CollectionWindow:961/1258/1471/2446` · `PracticeModePopup:1239`，逐处都发生在**该页/该窗正在开**时；`CollectionWindow:2446` 更是 `SetOffset(自己的 Offset)` ⇒ 被早退吃掉）⇒ **它是"潜伏的形状"，不是今天的缺陷**。
**代价/形状**：① `SetOffset` 里加"宿主不 active 就不通知"——**`MenuScroll` 是纯几何结构（只有 `Viewport` 矩形，没有 Transform/owner）** ⇒ 要加就得给它 owner 或让调用方传 `activeInHierarchy`（动共用件 + 全部构造点）。② 在订阅方（各 `RebuildXxx`）开头加 `if (!activeInHierarchy) return;` —— 分散、易漏。③ 改成"标脏、宿主下次绘制前消费"（最贴近原版帧语义），代价最大、全宿主都要动。④ 不改，只记账。
**⚖️ 我判不了**：要不要动（判据齐、代价齐，取舍权在调度台）。
⚠️ **文件状态**：`Shell/MenuScroll.cs` 现读**带未提交改动**（`git diff --numstat` = 22/10，`A269` 戳在 `:185`，`mtime 2026-10-04 23:52`）—— 我**只读没碰**；批里若还有写手在动它，先确认再派。`MenuDraw.cs` 同样带未提交改动（85/35）。
