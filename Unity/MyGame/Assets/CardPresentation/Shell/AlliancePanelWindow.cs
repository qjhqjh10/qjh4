// AlliancePanelWindow.cs — A985③：原版 `FrontCanvas/Alliance Panel`（组件类 `BattleAlliancePanel`）整扇窗
//
// ============================ 这是什么 ============================
// 对局里点 HUD 上那颗联盟钮（原版 `BattleHud.AlliancePanelOpenButton`）弹出来的**对手档案**窗：
// 头像框 + `Name:` / 名字 + `Title:` / 称号 + `Alliance:` / 联盟名 + 徽章 + 评级；
// **对手没有联盟时**改成一行「这个玩家还没有加入任何联盟」的提示。
//
// ============================ 判据（第一权威：解包资源逐字段亲读）============================
// 包 = `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/`（13 个战场各一份；**逐字段比对过
// arena1 vs arena2**：根框五元组、子件 rect、贴图 pid **逐位相同** ⇒ 13 场共用同一份 prefab）。
//
//   · 根  `GameObject/Alliance Panel.json`（GO 1130，`m_IsActive = 0`）+ `RectTransform_2859.json`：
//         `m_AnchorMin = m_AnchorMax = (0,1)` · `m_Pivot = (0.13, 0.825)` ·
//         `m_AnchoredPosition = (-1.044435, -25.271729)` · `m_SizeDelta = (815.044006, 475.469971)` ·
//         `m_LocalScale = 0.9999718070030212`
//         父链 = `Canvas(RT 2759) → FrontCanvas(2900) → Safe area FrontCanvas(2862)`（两条 stretch、零偏移）
//         ⇒ **绝对矩形 = x[-107.00, 708.02] y[-57.93, 417.52]**（**顶出屏 57.9 px —— 原版就长这样**）。
//         ⚠️ 父链三级的 `m_SizeDelta` 都是 `0`（stretch 到 1920×1080）⇒ 按 1920×1080 解；
//            `Canvas` 自己 `m_LocalScale = 0` 是**序列化残留**（ScreenSpaceOverlay 的 Canvas 由引擎驱动）
//            ⇒ 按 `1` 解，⛔ 别拿那个 0 去乘。根自己那个 `0.9999718` 同理按 `1` 解（差 0.0028%）。
//   · 控制器 `MonoBehaviour_4117.json`（`m_Script = BattleAlliancePanel`）的 12 个字段，按偏移顺序：
//     0x20 `avatarDisplay` · 0x28 `playerName` · 0x30 `playerTitle` · 0x38 `playerTitleHolder` ·
//     0x40 `playerScore` · 0x48 `allianceGO` · 0x50 `playerAllianceName` · 0x58 `allianceRatingDisplay` ·
//     0x60 `backgroundCloseButton` · 0x68 `canvasGroup` · 0x70 `allianceBadgeDrawer` ·
//     0x78 `notInAnAllianceMessage`（= GO 762 / RT 2660 / `MonoBehaviour_4475`）
//   · 方法体（`d:/2/tools/decomp_full/`，第一权威）：
//     `BattleAlliancePanel__Awake.c`  ⇒ **开局 `SetActive(false)`**，并给 `backgroundCloseButton` 挂
//                                       「点了就 `CloseButtonClick`」；`…__CloseButtonClick.c` = `SetActive(false)`。
//     `BattleAlliancePanel__Open.c`   ⇒ 显隐/赋值全在里面（逐句等价见 `Apply`）。
//     `BattleHud__AlliancePanelOpenButton.c` ⇒ HUD 取 `BattleManager.matchData` 的
//                                      `+0x10` 名 / `+0x50` 称号 / `+0x38` 评级 / `+0x40`,`+0x48` 头像 id /
//                                      `+0x68` 联盟名 / `+0x7c` 联盟评级 / `+0x80` 徽章 id 调 `Open(...)`。
//   · 节点树逐行核对表：`资料/战斗规格/战斗重建_0827/子代理读报_front弹层_0827.md` §五
//     （⚠️ 它给的是「**未乘 `localScale`** 的 rect」；本文件用**乘过的实绘矩形**，两者中心相同）。
//
// ============================ 🔴 三个「一个值 ≠ 全部情况」的坑 ============================
//  ① **`PlayerInfo` 身上有 `VerticalLayoutGroup`**（`MonoBehaviour_4213`：`m_Spacing = 0` ·
//     `m_ChildAlignment = 0`(UpperLeft) · `m_ChildControlWidth = 1` · **`m_ChildControlHeight = 0`**）⇒
//     `NameHolder` / `TitleHolder` / `Player Rating Display` 三个 holder 的序列化 rect **全是 0×0、
//     且都落在 `PlayerInfo` 的锚点上 = 「布局跑之前的模板位」** ⇒ 照抄会让三行叠成一坨。
//     ✅ 正确值按 `Core/UguiRect.cs` 的 `UguiLayout.VerticalChild`（那一条刚好就是本组合）重算 —— 见 `Build`。
//     ⚠️ 与 CLAUDE.md §三「权威坐标表里的 0×0 / 越界 rect 是模板位」是同一条，本件是**第二个实例**。
//  ② **文字那一族的行内 `m_LocalScale = 0.8`**（头像框 1.25 · 头像图 2.0）⇒ 字号与框都要乘它。
//     序列化 `m_fontSize` 是**没乘的 em**。
//  ③ **`Close Background` 的 rect 是 11668×8012**（远超屏幕、中心在面板右上）⇒ 那是原版「铺满全屏的
//     熄灯层」，⛔ 别当它是排版错误。
//
// ============================ 🔴 分层：只用渲染队列 ============================
//  全部件在**透明队列 3000**（`ImageQuad` 材质的默认档）⇒ 同队列下谁压谁由「到相机的 3D 距离」定，
//  而 `Close Background` 中心在 x≈+650 px（**离相机比面板本体近**）⇒ **靠 z 排会把熄灯层画到面板上面**。
//  ⇒ 逐层显式写队列（`QShade`…`QText`），并把「压暗层档 < 内容最低档」这条交给 `MenuDraw.ShadeHit` 自己校验。
//
// ============================ 🔴 2026-10-16（`T09` 批 3 · `OverHand` 组）：已迁 uGUI（双模式）============
//  配方与全部口径 → `资料/任务线/战斗线/任务/T09_整层换uGUI.md` **§五·补⑨ / §五·补⑩**；
//  映射那一行 → `资料/普查产出_1016战斗判据/T09c_面板到原版宿主映射.md` §一（`Alliance Panel` @
//  `…/Safe area FrontCanvas` · **`OverHand`/order 0** · 兄弟序 **13/14**）；
//  跨线裁定全文 → `资料/普查产出_1016并行/S2_AlliancePanel跨线裁定_报告.md` §⑨（**路 B：就地翻、⛔ 不新建第二份**）。
//   · **子画布** = `HudCanvas.EnsureSubCanvas(null, SubCanvasName, OverHand, 0)` —— 原版那一格**自身没有
//     `Canvas` 组件**、随 `FrontCanvas`（`OverHand`/0）渲染 ⇒ 我们给它一块**同层同序 + `overrideSorting`**
//     的子画布（继承根 canvas 那个唯一的 `CanvasScaler`；⛔ 子画布不加 `CanvasScaler`）。
//   · 🔴 **双模式（`uguiHost == null` ⇒ 与改本件之前【逐位相同】）**：`Create(parent, name, uguiHost)` ——
//     `null` ⇒ 今天的世界档（`MenuDraw.*` + `renderQueue` 阶梯，**那些数一个都没动**）；非 null ⇒ 画布档。
//     先例 = `Tooltip.UseHost(host)` / `ErrorMessageBanner.Create(…, uguiHost)` / `UnitChatPanel.Create(parent, uguiHost)`。
//     （上游调用点 = `Battle/BattleDriver.cs` **现核 `CardPresentation/Battle/BattleDriver.cs`** —— ✅ **收口波 `G1` 已接线**；
//       本条原写作 `:15084`，那份文件随后被 `G1`/`G3` 改过 ⇒ 行号已漂，⛔ 别照旧号找。）
//       ⚠️ 本文件末尾「入口」那一节原来写着「待接线」—— ✅ **已就地订正**（见那一节）。
//   · 🔴🔴 **层深度：队列 → 兄弟序【要取负】** —— 本件最要命的一处换算，⛔ 别「照搬队列值」：
//     世界档靠 `renderQueue` 分层（**队列大的画在【上面】**），而 `HudCanvas.Add` 的 z 是**兄弟序**
//     （**z 大的画在【底下】** —— 判据 = `HudCanvas.Group.Add` 的「按 z 降序插入」＋ `UnitChatPanel.ZPortrait`
//     与 `BattleLogPanel.ZShade` 两条注释都写着「z 大的在后面/最底下」）⇒ **两者极性相反**。
//     照搬的后果：熄灯层 3980 变成**最小的 z** ⇒ 被排到最后、**画在面板上面**（整屏 α0.4627 黑罩住面板）。
//     ⇒ 画布档一律走 **`ZOf(q) = −q`**（唯一一处换算，见那个助手）：相对先后**逐件不变** ✓。
//   · ⚠️ **命中**：`MenuDraw.ShadeHit` / `MenuDraw.Absorb` 走的是 `WindowButton` + 世界空间 quad
//     （只有壳侧 `PointerLayer` 会派发 —— 本文件 `Build` ① 那段「⛔ 不建 `MenuDraw.ShadeHit`」
//      与 `HitBody` 的 doc 各自自陈一遍）⇒ **画布档不建它们**
//      ⚠️ 本条原来写的是**本文件行号**（`:370-372`/`:486-488`）—— 本件注释一改就会漂，
//      ⇒ **改成按内容指路**（判据在本文件里、⛔ 别再补行号）。
//     （本轮口径：**不建 `EventSystem`、不开 `raycastTarget`** —— 判据 → `Battle/HudCanvas.cs` 的 `EventSystem`）。
//     战斗里「点窗外关窗」照旧由 `BattleDriver.HandleAlliancePanel` 用本类 `HitBody` 接 ⇒ `HitBody`
//     **已按档分支**（画布档走 `RectTransform.InverseTransformPoint` + `RootCenterLocal`，任何宽高比都对）。
//     🆕 **2026-10-20（`T09` A3-e）**：画布档已补**原版的 uGUI 射线靶**（压暗层点击 = 关，见下一节）；
//     驱动那条手搓路**本波一个字没动**（合并配方见下一节末）。
//   · ✅ **2026-10-16（`A14` · `T09` 批 3 锚点补正）：原来那一处「如实记的口径分歧」已按【原版】裁掉。**
//     原版那一级 = `aMin = aMax = (0,1)` **顶左点锚** + `piv(0.13,0.825)` + `ap(-1.0444,−25.2717)` +
//     `sd(815.044,475.470)`（**不拉伸**）；画布档原先是「中心锚 + 固定设计 px」
//     （`HudCanvas.SetPxSize` + `PosPx`，批 2/3 共用那一套）⇒ **16:9 下逐位相同**、**非 16:9 整扇跑偏**
//     （4:3 差 **135 屏 px**；通式 = `(屏高 − 0.5625 × 屏宽) / 2`）。
//     ⇒ 现在画布档**逐值照抄那五元组**（`SetRootAnchor` + `RootAnchor*`/`RootPivot`/`RootAnchoredPos`/
//     `RootSizeDelta`），`HitBody` 同批补了「枢轴 ≠ 中心」那一项（`RootCenterLocal`）。
//     **世界档逐字节未动**（判据 → `资料/普查产出_1016并行/T09_AlliancePanel_锚点补正_报告.md` §③/§④）。
//   · ⚠️ **⛔ 没顺手做的四件老缺口**（与迁 uGUI 无关，见下面「已知缺口」①②③④）：头像立绘 / 两处评级 /
//     徽章 `Frame`·`Badge` / 出场 `DOFade`+`DOScale`（三常量没读）。
//     🔴🔴 **2026-10-17（`S11` 现核，铁律 5）就地订正 —— 下面 ①③④ 三条的【理由】都不成立，只有 ② 成立**：
//     ① 头像**资源全在**（`bundle_cosmeticavatarsimages_assets_all` 的 `Avatar_*`，**本仓 `Resources/Art/avatars/` 已导入 944 文件**），
//        而且**我们 HUD 早就在画同一套**（`AvatarImage_Me/Foe`：我方 = `ProfileData.AvatarArt`、对手 = 按本局种子从本地清单挑）
//        ⇒ `m_Sprite = 0` **只是出厂态**（`AvatarDisplay__ChangeAvatar.c:53-70` 有图就开、取不到还回落 `DefaultAvatarItem`）
//        ⇒ **能做**；唯一近似 = bot 的**允许清单** `AIBotsConfig` 在 LiveOps 远端。
//     ③ 徽章**图和 SO 两层都在本地**（22 张 `Alliance_Trophies_<阵营>` + **Frame 四档 `TIER1..4`** + `cosmeticsso` 的 16 个 `*_Badge.json`）；
//        真原因是 **`Open` 里 `!IsNullOrEmpty(allianceName)` 才做那一支**（`BattleAlliancePanel__Open.c:93-108`）
//        ⇒ **走不到 `Draw`**，⛔ **不是「原版出厂态没有」**。⚠️ 我们连那份 `*_Badge` 都还没接（零代码引用）。
//     ④ **三个 DOTween 常量已读出**：`0x1834b2bb8 = 1.0`（目标 alpha）· `0x1834b31d4 = 0.35`（共用时长）·
//        `0x1834b2e84 = 0.75`（起始缩放系数）⇒ **能做，属「欠做」**。
//     ✅ **只有 ② 两处评级仍归「等远端」**（`eloRating@0x38` / `allianceRating@0x7C` 来自服务端 profile、图标来自 LiveOps）。
//     🔴 **2026-10-19（`R01` · 第五十七会话）就地订正（铁律 5）**：② 的**布局/图标位那一半已经做掉** ——
//        `Player Rating Display` / `Alliance Rating Display` 两节点（含 `Main Icon` + `Individual rating value`
//        + 原版 `HorizontalLayoutGroup`）已建（判据见下面那组 `PlayerRatingR` 的注释）；
//        **仍然等远端的只剩：数值本身（服务端 profile）＋ LiveOps 活动图标（key=6）的覆盖**。
//        ⚠️ `Main Icon` 的出厂图 **不是** LiveOps 图标 —— 是 prefab 自带的 `40k_UI_icon_ranked_Skirmish`
//        （`AllianceRatingDisplay__Initialize` 只在传入非空图标时才覆盖）⇒ 那一格现在显示的就是**原版离线/无活动态**。
//     判据全文 → `资料/普查产出_1016并行/S11_四件缺口与PlayedThirdCard_报告.md`。
//     落地报告 → `资料/普查产出_1019/W57_远端线_R01布局与图标位.md`。
//
// ============================ 🆕 2026-10-20（W57 · `T09` 步骤 A3-e）：输入层换 uGUI ============
//  判据 = **2026-10-20 现读** `bundle_scenes_scenes_battlearena1` / `…battlearena2`（UnityPy 直读 typetree，
//  两场逐字段同值）—— 这扇窗**全场只有一处点击处理器**，其余件分成「射线靶」与「非射线靶」两类：
//   · **`Close Background`**（GO 377 / Image `MB_5220`）：`m_RaycastTarget = 1` · `m_RaycastPadding = (0,0,0,0)`
//     ＋ **`BackgroundCloseButton`**（`MB_4577`，`IPointerClickHandler.OnPointerClick`）
//     —— 事件 = **Click（松手那一帧）**，⛔ 不是 PointerDown。
//     `BattleAlliancePanel__Awake.c` 把 `CloseButtonClick`（= `SetActive(false)`）加进它的 `onClick`
//     （`BackgroundCloseButton__…OnPointerClick.c`：`window.Close()` + `onClick.Invoke()`）⇒ **点压暗层 = 关窗**。
//   · 🔴 **`BG` / `BGFrame` 四块 / `Border` 全是 `m_RaycastTarget = 0`**
//     （`MB_4162` / `4691` / `5261` / `4301` / `4338` / `4114`）⇒ ⛔ **它们不是射线靶**。
//     **本文件旧注释与 `BattleDriver.HandleAlliancePanel` 的 doc 都写「BG / BGFrame rt=1 ⇒ 点窗内不关」——
//     那条前提【是错的】**（2026-10-20 现读推翻；铁律 5 就地订正）。原版真正吃射线的是：
//       · **7 颗 TMP**（`PlayerLabel` / `Name Text` / `TitleLabel` / `Title Text` / `Alliance Label` /
//         `Alliance Name` / `NotInaAllianceText` —— 全 `rt=1` · pad `(0,0,0,0)`）；
//       · **两处评级的 `Main Icon`**（rt=1；`Secondary Icon` 也 rt=1 但 `m_IsActive=false`）；
//       · **头像 `Image`**（rt=1，但父 `Image Container` 的 `CanvasGroup.m_BlocksRaycasts = false` ⇒ 实际打不到）。
//       · `Individual rating value` = **rt=0**；`Highlight` / `Badge` / `Frame` = 0。
//   · ⇒ 原版的「关」**不是只在窗外**：面板底（BG/BGFrame）不吃射线 ⇒ 点**面板空白处**也会落到压暗层 = 关；
//     只有点在那 7 行字 / 两颗评级图标上才什么都不发生。本波**照这个落地**（⚠️ 与 `BattleDriver` 现有
//     `!HitBody(...)` 那条手搓路在「点窗内空白」这一格**语义不同** —— 那是**旧偏离**，合并时随手动路一起删）。
//   · 本波**只加输入路径，世界档一个字节不动**：画布档把压暗层变成射线靶 + `BattleHudButton`
//     （`IPointerClickHandler`，与 `BackgroundCloseButton` 同一条链）；**关窗动作延迟到 `LateUpdate`**
//     （`ApplyPendingClose`）—— 理由与 `ChatPopupPanel` 逐字相同：`EventSystem.Update` 与 `BattleDriver.Update`
//     同序，当场 `Close()` 会让驱动以为窗本来就关着 ⇒ 这一下继续落到棋盘/手牌（**静默的「点一下做两件事」**）。
//   · ⚠️ **如实记**：我们的 `Label` 的 TMP 只占**文字 bbox**（`Label.RefreshBounds` 写的是文字尺寸），
//     而原版 TMP 吃射线的矩形是**节点 rect** ⇒ 我们挡射线的那块**比原版小**（同 `MultiCardDisplay` 标题那条）。
//   · **合并配方（⛔ 本波不执行 —— `BattleDriver` 不在白名单）**：`CloseRequested` / `ApplyPendingClose` 已就位后，
//     ① `BattleDriver.HandleAlliancePanel` 里 `ReleasedThisFrame() && !HitBody(...)` 整段删掉，只留「窗开着 ⇒
//     `return true`」的模态闸；② `A985③` 那条「点窗内 ⇒ 不关」的断言改成「点窗内**空白** ⇒ 关 / 点在那 7 行字上 ⇒ 不关」
//     （判据 = 本文件 `BuildUguiClose` 与 `BlockRaycasts` 的 doc）；③ `HitBody` 若届时无人再用，可一并删。
//
// ============================ 🆕 相对原版的**已知缺口**（如实记，⛔ 不静默）============================
//  ① **头像立绘不画**：`Avatar Item Small/Image Container/Image` 的 `m_Sprite` 是 `0`（运行期由
//     `AvatarDisplay.Initialize` 灌）⇒ **原版出厂态本来就不画**（同本仓名牌那两处的口径）；
//     我们单机的 bot 也没有头像数据源 ⇒ 这一格保持原版出厂态（**只摆框**）。
//     🔴 **2026-10-17 就地订正（铁律 5）—— 上面三句【全不成立】**（`S11` 现核）：
//     · 「原版出厂态本来就不画」**错**：`AvatarDisplay__ChangeAvatar.c:53-70` = `sprite = CosmeticItem.get_Image(item)`
//       然后 **`set_enabled(img, sprite != null)`（有图就开）**，且 `Initialize` 的实参取不到时**回落 `get_DefaultAvatarItem()`**
//       ⇒ **原版永远有图**；`m_Sprite = 0` 只是**序列化出厂态**。
//     · 「我们单机的 bot 也没有头像数据源」**错**：HUD 里 `AvatarImage_Foe` **早就在画**（按本局种子从 `ProfileData.Avatars` 确定性挑）。
//     · ⇒ **本格【可做】**：把 HUD 那套搬进来即可。唯一真实差异 = 原版 bot 的**允许清单** `AIBotsConfig.GetRandomAvatar`
//       在 **LiveOps 远端** ⇒ 我们「从本地清单挑」是**已如实标注的近似**。
//  ② **两处评级**：`Player Rating Display` / `Alliance Rating Display` 里的 `Main Icon` /
//     `Individual rating value` 序列化都是 **0×0**，尺寸由 `RatingDisplay` 脚本运行时算
//     （两个节点身上各挂一个 `HorizontalLayoutGroup`、`m_ChildControlWidth/Height = 1`
//     ⇒ 尺寸 =「TMP/LayoutElement 首选宽」× 容器高，**静态取不到**）；而且我们单机**没有对手评级数据**。
//     ⇒ 不建那两格（要补：先有一个真评级数据源 + 一次运行期读数）。
//     🔴 **2026-10-19（`R01`）就地订正（铁律 5）—— 本条已全部作废，两格【已建】**：
//     · 「静态取不到」只是**尺寸层面**：`HorizontalLayoutGroup` 那套算法（`HorizontalOrVerticalLayoutGroup.cs`
//       `SetChildrenAlongAxis`）在**容器宽已知**时是**确定**的 —— 图标 = `LayoutElement` 首选宽 60（×0.8 = 48）、
//       文本 = 剩余宽（`Lerp(0, 5000, (size−48)/5000)` = `size − 48`）⇒ 我们直接按实绘 rect 建、再挂上真布局组，
//       **不需要运行期读数**。判据见 `PlayerRatingR` 那段注释与落地报告。
//     · 「没有评级数据」仍然成立 ⇒ 两格显示**原版 prefab 自带的空态串**（7 / 6 个连字符）+ **出声一次**；
//       图标用 prefab 自带的 `40k_UI_icon_ranked_Skirmish`（LiveOps 只是覆盖）。⛔ 不编数值。
//  ③ **徽章 `Frame` / `Badge` 不建**：两张 `m_Sprite` 也是 `0`（`AllianceBadgeDrawer.Draw(id)` 运行期灌）
//     ⇒ 同样是「出厂态就没有」；且我们单机恒走「无联盟」那一支，那两格根本不显。
//     🔴 **2026-10-17 就地订正（铁律 5）—— 前半句错、后半句对**（`S11` 现核）：
//     · 「出厂态就没有」**错**：**图和 SO 两层都在本地** ——
//       `bundle_alliancesbadgesimages_assets_all/Sprite|Texture2D/` 的 **22 张 `Alliance_Trophies_<阵营>`**
//       ＋ **Frame 四档 `Alliance_Trophies_TIER1..TIER4`**（`AllianceBadgeDrawer__SetTierFrame` → `LoadTierFrame`，索引 `Max(0, tier-1)`）
//       ＋ `bundle_cosmeticsso_assets_all/MonoBehaviour/` 的 **16 个 `*_Badge.json`**；
//       **`Badge`** 走 `GroupBadge.featuredTrophy@0x10 → AllianceTrophyData.reward@0x58 → AllianceBadge.badgeImage@0x38`（`AllianceBadgeDrawer__Draw.c:24-30`，null 回落 `DefaultTrophy`）。
//     · 🔴 **真原因是「走不到那一支」**：`BattleAlliancePanel__Open.c:93-108` 是 **`bVar9 = !IsNullOrEmpty(allianceName)`** 才做联盟那一块
//       ⇒ 我们传 `Open(name, "", "")` ⇒ **`allianceGO` 关、`Draw` 压根不执行** ⇒ ⛔ **不是「原版没有」**。
//     · ✅ **2026-10-28（B88-B7 · 账 187③）就地订正（铁律 5）—— 上面两条【都已解决】**：
//       ① `Frame` / `Badge` 两层**已建**（`Alliance` 组第 3 子件，`AllianceBadgeDrawerR`）；
//       ② 「徽章 GUID → sprite」那份映射**已有** —— 16 份 SO 的 `badgeImage.m_AssetGUID` → `AssetBundle.m_Container`
//          → Sprite 名，16/16 解出（判据 `B88_R8_安卓包徽章挖掘.md` §二），落成 `Shell/AllianceBadgeArt.cs`。
//       ⚠️ 仍然成立的是：**单机恒走「无联盟」那一支**（`BattleDriver` 传 `Open(name,"","")`）⇒ 那两层建了但整组是关的。
//  ④ **出场动画（`DOFade` + `DOScale`）没做**：原版 `Open` 末尾是 `CanvasGroup.alpha = 0` → `DOFade`
//     + 「先缩到 `×DAT_1834b2e84` 再 `DOScale` 弹回」。本件**直接显/隐**（本仓其它窗同此）；
//     ⚠️ 三个 DOTween 常量（目标 alpha / 时长 / 缩放系数）**没读**（要在 `GameAssembly.dll` 里取值）。
//     🔴 **2026-10-17 就地订正（铁律 5）—— 最后那句【已过期】：三个常量【已读出】**（`S11` 现核，`BattleAlliancePanel__Open.c:99-125`）：
//     `0x1834b2bb8` = **1.0**（`DOFade` 目标 alpha；4B float —— 判据 = 形参类型 `float`）
//     · `0x1834b31d4` = **0.35**（`DOFade` 与 `DOScale` **共用的时长**，秒）
//     · `0x1834b2e84` = **0.75**（**起始缩放系数**）。
//     ⇒ **本件属「欠做」（能做、只是没做）**，⛔ 别再写成「判据拿不到」。
//     （读法：`python -I -X utf8 工具/read_literal.py "D:/2/unity_run_ref/GameAssembly.dll" <地址>`；⚠️ 要 `PYTHONIOENCODING=utf-8`。）
//  ⑤ ~~**HUD 上那颗钮我们还没有**（见文件末「入口」一节）~~ ⇒ ✅ **2026-10-18（A985③）已接**：
//     `Battle/BattleDriver.cs` 的 `BuildHudExtras` 里建了那颗钮，点击入口 = `HandleAlliancePanel`。
//     🔴 **那颗钮【没有贴图】** —— 原版它的 `m_TargetGraphic` 类是 `NonDrawingGraphic`
//     （`OnPopulateMesh` 方法体为空）、节点就是 `LeftArea/EnemyInfo` **本身** ⇒ 纯命中区。
//     判据全文（rect / padding / 13 场同构 / 点击沿）**只写在那一处**，本文件不重复。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `FrontCanvas/Alliance Panel`（`BattleAlliancePanel`）—— 对局内的**对手档案**窗。
    /// <para>⚠️ **它是「面板」不是「窗」**：根组件是普通 `MonoBehaviour`、**不是 `GameWindow`**
    /// ⇒ 没有 `type` / `placement` / `closeOnESC` 可填、也⛔ **不注册进 `WindowsManager`**
    /// （同 `Shell/AllianceEventScorePanel.cs` 的口径；别为了「凑一条注册」给它编一个窗身份）。</para></summary>
    public class AlliancePanelWindow : MonoBehaviour
    {
        // ============================================================ 分层（渲染队列，⛔ 不是 z）
        //  全部 ≥ 3980 ⇒ 都在战斗 HUD（默认档 3000）之上；`QShade` 是唯一低于内容的档。
        public const int QShade = 3980;     // 熄灯层 `Close Background`（**必须严格低于 `QContentMin`**）
        public const int QBg = 4000;        // `Background` / `BG`
        public const int QFrame = 4010;     // `BGFrame` 四块
        public const int QAvatar = 4020;    // 头像框 `Border`
        public const int QText = 4030;      // 全部文字
        /// <summary>传给 `MenuDraw.ShadeHit` / `MenuDraw.Absorb` 的「本窗内容最低档」——
        /// 那两个口靠它算「压暗/吸收命中区用哪一档」并**当场校验**（不合法会告警）。</summary>
        public const int QContentMin = QBg;

        // ============================================================ 🆕 uGUI 档（`T09` 批 3）
        /// <summary>本扇窗那块 **uGUI 子画布**的名字（= 原版宿主名 + `" Canvas"`，`T09` §五·补⑩ 第 1 步）。
        /// 判据 = `T09c` §一 我这一行：`Alliance Panel` @ `…/Safe area FrontCanvas` · `OverHand` / order 0 ·
        /// 兄弟序 13/14。⛔ **只此一份**（建它、自检找它都取这里）。</summary>
        public const string SubCanvasName = "Alliance Panel Canvas";

        /// <summary>🔴 **队列 → 画布档的 z（唯一一处换算）**：`HudCanvas.Add` 按 z **降序**排兄弟序
        /// （**z 大的画在【底下】**），而 `renderQueue` 恰好相反（**队列大的画在【上面】**）⇒ **取负**，
        /// 相对先后才逐件不变。⛔ 别「照搬」队列值 —— 那样熄灯层 3980 会变成最小 z、**画到面板上面**。
        /// 判据 → 文件头那条 + `HudCanvas.Group.Add` 的 doc + `UnitChatPanel.ZPortrait`（z 大 = 在底下）。</summary>
        static float ZOf(int q) { return -q; }

        // ============================================================ 根框（画布像素 · 左上原点 · 1920×1080）
        //  ⚠️ 这是原版矩形的**实绘**值（乘过根那个 `m_LocalScale = 0.9999718`；与 `_probe_node_rect.py` 的
        //     原始输出 `x [-107.000, 708.044] y [-57.936, 417.534]` 差 ≤0.024 px）。
        //     用途 = ① 世界档的摆位 ② 面板内**所有子件**的「相对父中心」参照（`LocalIn` 的 `parentR`）。
        static readonly PxRect RootR = new PxRect(-107.00f, -57.93f, 708.02f, 417.52f);

        // 🔴🔴 **2026-10-16（`A14` · `T09` 批 3 锚点补正）：根那一级的【五元组】= 原版 `RectTransform_3052`**
        //   出处 = `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena2/RectTransform/RectTransform_3052.json`
        //   （⚠️ arena1 那份的 pid 是 `2859` —— **13 场逐字段比对过、五元组逐位相同**，本文件头那一节）。
        //   · `m_AnchorMin = m_AnchorMax = (0,1)` ⇒ **顶左【点锚】**（父链两跳都是 stretch ⇒ 屏幕左上角）。
        //     🔴 **点锚只管「尺寸固定」；它的【位置】由「锚点落在父件的哪个角/边」定** ——
        //     只有锚点 = 父件**中心** `(0.5,0.5)` 时，「点锚 + 固定 px」才等价于「中心锚 + 固定 px」。
        //     **本件原来就是被这条坑到的**（画布档写成中心锚 ⇒ 16:9 逐位相同、非 16:9 整扇跑偏）。
        //   · `pivot` / `anchoredPosition` / `sizeDelta` 逐值照抄（⛔ 一个数都没改）。
        //   · `m_LocalScale = 0.9999718070030212` **不设**（与 `A9` 那档一致）：0.0028% ⇒ 实绘差 ≤0.024 px，
        //     而设它会把**整棵子树**一起缩（判据 → 报告 §④ 那条「16:9 自证」）。
        static readonly Vector2 RootAnchorMin = new Vector2(0f, 1f);
        static readonly Vector2 RootAnchorMax = new Vector2(0f, 1f);
        static readonly Vector2 RootPivot = new Vector2(0.13f, 0.825f);
        static readonly Vector2 RootAnchoredPos = new Vector2(-1.04443359375f, -25.271728515625f);
        static readonly Vector2 RootSizeDelta = new Vector2(815.0440063476562f, 475.470970703125f);

        // ============================================================ 逐件几何（**全 = 实绘矩形**，已乘 localScale）
        //  命名 = 原版节点名；括号里的 pid = `RectTransform` 的 PathID（可逐条复查）。
        //  ⚠️ 这一组**不是从报告里抄的**，是按 `RectTransform_*.json` 的五元组 + 父链 + `m_LocalScale` 现算的
        //     （算式 = 文件头那三条；与 `子代理读报_front弹层_0827.md` §五 那张表**中心逐一吻合**，
        //      差别只在「它没乘 `localScale`」）。

        // 熄灯层 `Close Background`（RT 2698）：图 = 同族底图 + 纯黑 α0.4627；`m_RaycastTarget = 1`
        static readonly PxRect ShadeR = new PxRect(-5181.741f, -3867.769f, 6486.258f, 4144.042f);
        const float ShadeAlpha = 0.4627f;                       // 实读 `m_Color = (0,0,0,0.4627)`

        // 面板本体 `Background`（RT 3166）。⚠️ 它自己**没有** Image（只有 `BG` 那张有）—— 见 `Build` 吸收层那条
        static readonly PxRect BackgroundR = new PxRect(-97.327f, 9.970f, 625.312f, 413.089f);
        // 内容底 `BG`（RT 3225）：贴图 `40K_shop_offer_bg_Sororitas_0`、染色 (0.349,0.349,0.349,1)、Type=Simple
        static readonly PxRect BgR = new PxRect(14.158f, 55.246f, 571.457f, 356.583f);
        static readonly Color BgTint = new Color(0.349f, 0.349f, 0.349f, 1f);
        // `BGFrame`（RT 3339）与它四块（RT 2734/3407/3414/3122）；图 = `40k_UnitChat_Background_*`
        static readonly PxRect FrameR = new PxRect(-92.217f, 13.557f, 624.645f, 412.646f);
        static readonly PxRect FrameLeftR = new PxRect(-92.673f, 25.022f, 24.645f, 411.994f);
        static readonly PxRect FrameRightR = new PxRect(541.475f, 13.985f, 625.123f, 385.038f);
        static readonly PxRect FrameTopR = new PxRect(23.750f, 24.840f, 541.422f, 58.762f);
        static readonly PxRect FrameBottomR = new PxRect(24.487f, 348.664f, 542.159f, 385.195f);

        // 头像框 `Border`（RT 3040）：图 `Player Profile Border` 256×286、`m_PreserveAspect = 1` ⇒ 等比内接
        static readonly PxRect AvatarBorderR = new PxRect(-11.283f, 60.573f, 201.246f, 209.590f);

        // 🆕 2026-10-29（`B95_MISC` · 本批①）：头像立绘 `Avatar Item Small/Image Container/Image` 的**视觉框**。
        //   判据 = `python 工具/menu_dump.py bundle_scenes_scenes_battlearena1 "Alliance Panel" --depth 6 --no-sprite` 现读：
        //   `Image Container` = `10.0,63.6 → 180.0,182.8`（170.0×119.2 · `localScale 0.8`）；`Image` 是它的 stretch 子件、`apos.y = 2.7` ⇒ 布局框 = 同尺寸**上移 2.7**（中心 `(95.0,120.5)`）。
        //   🔴 **2026-10-30（`B101_W3` · 铁律 5 就地订正）**：原来这里写「视觉框 ×2 是 `EverguildButtonMaterialModifier` 的**运行时**视觉缩放、本窗不复制」——**不成立**：① ×2 是 `RectTransform.m_LocalScale` **序列化在 prefab 里**的（`报告/B101_P1_prefab视觉参数普查.md` §2·1，9/9 实例同值，不需要运行期）；
        //   ② 那个类全文 `localScale` **0 命中**（只有 `GetModifiedMaterial`/`ChangeCanvasColor`/`ToogleGreyScale`/`SetMaterialDirty`）⇒ 本窗**按 ×2 复刻**。
        //   ⇒ 视觉 340.0×238.4（原版普查 340.05×238.43，差 0.05 = 容器宽 170.0 vs 170.02）；⚠️ 它会**戳出头像框**（原版就是这样）。贴图 `preserveAspect` 内接。
        public static readonly PxRect AvatarImageR = AvatarTab.ScaleAboutCenter(new PxRect(10.0f, 60.9f, 180.0f, 180.1f), AvatarTab.ScaleImage);

        // `Name:` / 名字（`NameHolder` = VLG 第 0 格，高 42.573 ⇒ y[62.498, 105.071]）
        static readonly PxRect NameRowR = new PxRect(180.000f, 62.498f, 556.959f, 105.071f);
        static readonly PxRect NameLabelR = new PxRect(180.000f, 63.436f, 273.941f, 103.636f);
        static readonly PxRect NameTextR = new PxRect(276.220f, 63.436f, 555.228f, 103.636f);
        // `Title:` / 称号（`TitleHolder` = VLG 第 1 格，高 38.442 ⇒ y[105.071, 143.513]）
        static readonly PxRect TitleRowR = new PxRect(180.000f, 105.071f, 556.959f, 143.513f);
        static readonly PxRect TitleLabelR = new PxRect(179.995f, 104.586f, 274.429f, 145.386f);
        static readonly PxRect TitleTextR = new PxRect(276.215f, 104.102f, 555.223f, 145.166f);
        // `Alliance` 组（RT 3495）与它那两行字（组里**没有**布局组 ⇒ 用它自己的 rect）
        static readonly PxRect AllianceGroupR = new PxRect(23.094f, 220.581f, 574.688f, 356.530f);
        static readonly PxRect AllianceLabelR = new PxRect(183.090f, 244.780f, 278.002f, 285.579f);
        static readonly PxRect AllianceNameR = new PxRect(278.940f, 244.780f, 535.827f, 285.579f);
        /// <summary>🆕 **2026-10-28（B88-B7 · 账 187③）**：`Alliance Badge Drawer`（RT **3503** / GO 同名）
        /// —— `Alliance` 组的**第 3 个子件**（`m_Children = [2635(Label), 3272(Name), 3503(BadgeDrawer), 3110(Rating)]`）。
        /// <para>逐字段实读（`bundle_scenes_scenes_battlearena1`）：`m_AnchorMin/Max = (0.5, 0.5)` ·
        /// `m_AnchoredPosition = (-197.459930, 8.970734)` · `m_SizeDelta = (139.705994, 131.104004)` ·
        /// `m_Pivot = (0.5, 0.5)` · 🔴 **`m_LocalScale = 1`（不是 0.8）**。</para>
        /// <para>换算：兄弟节点的位置偏移**不被自己的 scale 缩放**（那是尺寸的事），故
        /// 中心 = `AllianceGroupR` 中心 + (−197.4599, −8.9707↓) = **(101.439, 279.587)**；
        /// **尺寸不乘 0.8**（本件 scale = 1）⇒ 半宽 69.853 / 半高 65.552。
        /// 反证：同组 `AllianceNameR` 用同一条通式算出来是 (278.940, 325.38×0.8…) 与既有值逐位吻合
        /// （差值 0.01 px = 字段位数），⇒ 这条通式就是本文件一直在用的那条。
        /// ⚠️ 它**上边缘溢出父组 6.55 px**（214.035 &lt; 220.581）—— 原版就这样（组上没有遮罩）。</para></summary>
        static readonly PxRect AllianceBadgeDrawerR = new PxRect(31.586f, 214.035f, 171.292f, 345.139f);
        // `NotInaAllianceText`（RT 2660）—— 🔴 原版那句 `is is` 双 is 是笔误，照抄勿改
        static readonly PxRect NotInAllianceR = new PxRect(55.765f, 257.013f, 529.877f, 293.972f);
        const float NotInAllianceAlpha = 0.772549f;             // 实读 `m_fontColor.a`

        // ============================================================ 两处评级（🆕 2026-10-19 · 远端线 `R01`）
        //  🔴 **判据 = 原版 prefab 逐字段实读**（`bundle_scenes_scenes_battlearena1`）：
        //   · `Player Rating Display`   RT **3111** / GO 662 · `m_LocalScale = 0.8` · father = `PlayerInfo`(RT 3363)
        //     的 `VerticalLayoutGroup` **第 2 格**（`m_ChildControlWidth = 1` / `m_ChildControlHeight = 0`
        //     ⇒ 宽 = PlayerInfo 宽 376.959、高 = 自己 `m_SizeDelta.y` 60.539）。
        //   · `Alliance Rating Display` RT **3110** / GO 661 · `m_LocalScale = 0.8` · father = `Alliance`(RT 3495)
        //     的**第 4 个子件**（`m_Children = [2635, 3272, 3503, 3110]`）·
        //     `m_AnchorMin/Max = (0,0)` · `m_AnchoredPosition = (230.542755, 40.429642)` ·
        //     `m_SizeDelta = (184.184006, 60.539001)` · `m_Pivot = (0.5, 0.5)`（**不在任何布局组里**，字段即真值）。
        //   · 两个容器身上各挂一个 **`HorizontalLayoutGroup`**（MB **4788** / **4785**，同类同值）：
        //     `m_Padding 0` · `m_ChildAlignment 3`(MiddleLeft) · `m_Spacing 0` · `m_ChildForceExpandWidth 0`
        //     · `m_ChildForceExpandHeight 1` · **`m_ChildControlWidth 1`** · **`m_ChildControlHeight 1`**
        //     · `m_ChildScaleWidth/Height 0` · `m_ReverseArrangement 0`。
        //   · 子件（`m_Children` 序 = Secondary Icon(act **F**) → Main Icon → Individual rating value）：
        //       `Main Icon` = Image（`m_Sprite` 跨包 PPtr → **`40k_UI_icon_ranked_Skirmish`**，`m_PreserveAspect = 1`）
        //         + `LayoutElement{m_MinWidth 60, m_PreferredWidth 60, m_FlexibleWidth 0}`；
        //       `Individual rating value` = TMP（`Pragati-Regular SDF` · `m_fontSize 45` · `m_fontSizeBase 31.379999`
        //         · `auto[18, 45]` · `Left/Midline` · 折行=1）+ `LayoutElement{m_PreferredWidth 5000}`。
        //   ⚠️ **两条容器都是 `m_LocalScale = 0.8`** ⇒ 本文件照 `NameLabelPx` 那一族的同一条换算**按实绘 px 建**
        //      （尺寸 ×0.8、位置按枢轴换算；`LayoutElement` 那个 60 也是**局部单位** ⇒ 实绘 48）。
        //      ⇒ 这里不设 `localScale`，所以 `HorizontalLayoutGroup` 拿到的容器宽/高**就是实绘值**。
        static readonly PxRect PlayerRatingR   = new PxRect(180.000f, 149.567f, 481.567f, 197.998f);
        static readonly PxRect AllianceRatingR = new PxRect(179.963f, 291.885f, 327.310f, 340.316f);
        /// <summary>`Main Icon` 宽 = 原版 `LayoutElement.m_PreferredWidth = 60` × 0.8（实绘 px）。</summary>
        const float RatingIconW = 48f;
        // `Individual rating value` 的字号三件套（原版值 × 0.8 —— 见 `NameLabelPx` 那条换算）：
        const float RatingValuePx = 36f;          // 45    × 0.8
        const float RatingValueMaxPx = 36f;       // 45    × 0.8（`m_fontSizeMax`）
        const float RatingValueBasePx = 25.104f;  // 31.38 × 0.8（`m_fontSizeBase`）

        /// <summary>原版 `Main Icon` 的贴图 —— **`m_Sprite` 跨包 PPtr**（`m_FileID 13` / `m_PathID -6552398156213418268`）
        /// ⇒ UnityPy 现解 `armyicons_assets_all.bundle` = **`40k_UI_icon_ranked_Skirmish`**（128×128）。
        /// <para>🔴 **它不是「LiveOps 图标」**：`AllianceRatingDisplay__Initialize.c` 只在**传入非空图标**时
        /// 才 `Image.sprite = icon`（末尾那句 `op_Implicit` 判空）—— LiveOps 的活动资产（key=6）是**覆盖**，
        /// 本地没有那个资产时**原版就显示这张 prefab 自带的图**（离线/无活动态的逐句等价物）。</para></summary>
        public const string RatingIconArt = "40k_UI_icon_ranked_Skirmish";

        /// <summary>🆕 **2026-10-19（R01）：两格评级的空态串 = 原版 prefab 自己写在那一格里的字面量**。
        /// <para>判据 = `MonoBehaviour_5118.json`（GO 958 → RT 3323 → father RT **3111** = `Player Rating Display`）
        /// 的 `m_text` **`len = 7`**；`MonoBehaviour_5121.json`（GO 955 → RT 3322 → father RT **3110**）的
        /// `m_text` **`len = 6`** —— 用 python 逐字节数过（⛔ 目测会数错，两格都长这样）。同一次 `menu_dump` 的
        /// 「文字」列同值（`'-------'` 在上、`'------'` 在下）。</para>
        /// <para>⚠️ **两格【不同长】**，⛔ 别互相套用；同族先例各取各的 —— `Shell/RankedTab.ScoreEmpty` = **6**、
        /// `Shell/AllianceMemberTab.RateEmpty` = **7**（那是另一族节点，**不是**同一份判据）。</para></summary>
        public const string PlayerRateEmpty = "-------";     // 7 个连字符
        public const string AllianceRateEmpty = "------";    // 6 个连字符

        // 字号 = 原版 `m_fontSize` **× 该节点累计 localScale**（这一族全是 0.8）。
        //  ⚠️ 这几颗都 `m_enableAutoSizing = 1`，但序列化 `m_fontSize` **就是**上一次算出来的拟合值
        //     ⇒ 直接用它（`MenuDraw.Text` 走 `SetGlyphHeight`，不做二次 autosize —— 见 `Text` 的注释）。
        const float NameLabelPx = 28.00f;      // 35   × 0.8
        const float NameTextPx = 32.00f;       // 40   × 0.8
        const float TitleLabelPx = 28.00f;     // 35   × 0.8
        const float TitleTextPx = 28.00f;      // 35   × 0.8
        const float AllianceLabelPx = 26.72f;  // 33.4 × 0.8
        const float AllianceNamePx = 28.00f;   // 35   × 0.8
        const float NotInAlliancePx = 26.32f;  // 32.9 × 0.8

        // 🔴 **2026-10-09（A1126 · A2 档）**：上面这七颗原版 **全是 `m_enableAutoSizing = 1`**
        //   （现读 `scenes_battlearena1 ▸ Alliance Panel` 那 7 颗 TMP：
        //    `PlayerLabel` / `Name Text` / `TitleLabel` / `Title Text` / `Alliance Label` / `Alliance Name` /
        //    `NotInaAllianceText`）—— 我们原来**一个 autosize 实参都没传** ⇒ 固定字号。
        //   四格真值：`min = 18` · `base = 36` · **折行 = 0** · `max = 35`（六颗）／**40**（只有 `Name Text`）。
        //   🔴 **三格都要乘本族那个 `localScale = 0.8`** —— 与上面那几个 `*Px` 是**同一条换算**
        //   （原版 `m_fontSizeMin/Max/Base` 是**未缩放的设计空间原值**）。
        //   判据 = 本仓既有先例 `Shell/MissionsTab.cs` 的 `FS()`：**「自适应上下限也要一起乘」**
        //   （落地在 `CardPresentation/Shell/MissionsTab.cs` `FS(autoMinPx)` 与 `CardPresentation/Shell/MissionsTab.cs` `FS(minPx), FS(maxPx), FS(basePx)`；说明见 `CardPresentation/Shell/MissionsTab.cs`）。
        //   ⛔ **不是我们挑的**：换算是「照原版实绘」，裸传原值反而会让上限比原版高 25%。
        const float AutoMinPx = 18f * 0.8f;       // 14.4（七颗同值）
        const float AutoBasePx = 36f * 0.8f;      // 28.8（七颗同值）
        const float AutoMaxPx = 35f * 0.8f;       // 28  （六颗）
        const float AutoMaxNamePx = 40f * 0.8f;   // 32  （只有 `Name Text` 是 40 —— ⛔ 别一刀切）

        // 原版那 4 颗 TMP 的**静态原文**（= prefab 出厂值，也是运行期 `GetTranslation` 的兜底）。
        //  🔴 运行期那 4 条真值在**远端 I2 表**（本仓没有，见 `资料/普查产出_1018/G2b_战斗本地化收口.md`）
        //     ⇒ 先查 `Loc` 表；`Loc` 里没这一条就用 prefab 原文（= 原版出厂就是那句英文）。
        const string NameLabelTerm = "Battle/AlliancePanel/PlayerLabel";
        const string TitleLabelTerm = "Battle/AlliancePanel/TitleLabel";
        const string AllianceLabelTerm = "Battle/AlliancePanel/AllianceLabel";
        const string NotInAllianceTerm = "Battle/AlliancePanel/NotInAnAlliance";
        const string NameLabelEn = "Name:";
        const string TitleLabelEn = "Title:";
        const string AllianceLabelEn = "Alliance:";
        // 🔴 **2026-10-14 就地订正（铁律 5 · `W4`/`A1803`）**：原来写 `"This player is is still not part of an Alliance"`
        //   —— 那个 `is is` 是**我们抄错的**（当时的旧注还写着「真值在远端 I2 表」）：官方
        //   `Battle/AlliancePanel/NotInAnAlliance` = **`This player is still not part of an Alliance`**
        //   （中文列「该玩家仍未加入联盟」）。⛔ 这是**死兜底**（正常走 `Loc.T`），但值要对。
        const string NotInAllianceEn = "This player is still not part of an Alliance";

        // ============================================================ 建出来的东西
        Transform _root, _background, _allianceGo, _titleHolder, _notInAlliance;
        /// <summary>🆕 2026-10-19（`R01`）：两处评级的容器（原版 `Player Rating Display` / `Alliance Rating Display`）。</summary>
        Transform _playerRating, _allianceRating;
        Label _nameLb, _titleLb, _allianceLb;
        readonly List<string> _missArt = new List<string>();
        bool _visible;
        bool _ratingWarned;
        /// <summary>本扇窗是不是建在 **uGUI 子画布**上（`false` = 今天的世界档，逐位不变）。
        /// ⚠️ 必须在 `Build()` **之前**赋值（`Build` 里按它选建件口与单位）。</summary>
        bool _ugui;

        // ============================================================ 🆕 2026-10-20（`T09` A3-e）输入层
        //  ⚠️ 下面这几个字段**只在画布档有意义**（世界档恒 null / 空表）—— 判据与口径见文件头那一节。
        HudPiece _shade;                       // `Close Background`（原版 rt=1 + `BackgroundCloseButton`）
        HudPiece _bg;                          // `BG`（原版 rt=0 —— **只给自检读**，⛔ 别改成射线靶）
        // 🆕 2026-10-29（`B95_MISC` · 本批①）：头像那两件 —— `Avatar Item Small/Image Container/{Border, Image}`。
        //   `_avatarImg` **惰性建**（没有头像数据就一个节点都不建 = 原版 `m_Sprite = 0` 那一态）；
        //   `_avatarBorderImg` 恒建（出厂那张 `Player_Profile_Border`）。
        HudPiece _avatarBorderImg, _avatarImg;
        BattleHudButton _closeBtn;             // 压暗层上的 `IPointerClickHandler`（= 原版 `BackgroundCloseButton`）
        /// <summary>原版 `m_RaycastTarget = 1` 的那 7 颗 TMP（挡住射线、**零处理器** —— 点它们不关窗）。</summary>
        readonly List<Label> _blockLabels = new List<Label>();
        /// <summary>两处评级的 `Main Icon`（原版 rt=1 —— 同上：挡射线、不响应）。</summary>
        readonly List<HudPiece> _ratingIcons = new List<HudPiece>();
        /// <summary>两处评级的 `Individual rating value`（原版 **rt=0** —— 保持出厂 false，⛔ 别顺手打开）。</summary>
        readonly List<Label> _ratingValues = new List<Label>();
        bool _closePending;
        int _uguiCloseClicks, _uguiCloseApplied;

        /// <summary>🆕 2026-10-20（`T09` A3-e）：画布档的「压暗层被点 ⇒ 请求关窗」**已落账**时发一次。
        /// <para>⚠️ **本波没有订阅者**（`BattleDriver` 不在白名单）：关窗动作由本类**自己**在
        /// `LateUpdate → ApplyPendingClose` 里落（`Close()` 幂等 ⇒ 与驱动那条手搓路并存安全）。</para>
        /// <para>合并之后（手动路删掉）这个事件仍有意义：联机/壳侧要挂钩「窗关了」的副作用时挂这里。</para></summary>
        public event Action CloseRequested;

        public bool Visible { get { return _visible; } }
        /// <summary>自检用（`T09` 批 3 的【灭自证】）：本扇窗的图与字**是不是都建在 uGUI 子画布上**
        /// （而不是有条漏网、退回世界空间 quad）。⛔ 别用「`Label` 非 null」顶替（那个**两档都真**）。
        /// <para>🔴 **2026-10-16（`F8` · 修波）就地订正（铁律 5）**：本口原来是
        /// `public bool OnCanvas { get { return _ugui; } }` —— `_ugui` 是**建件开关本身**
        /// （`Build` / `Node` / `Img` / `Txt` 都按它分支）⇒ 拿它当「灭自证」= **自证**
        /// （出处 = `R1_面板迁移审查_报告.md` §②-9 · `_收口台账_第四十二会话.md` `F8`）。
        /// 现在改成**逐件走父链**，形状照同批另三扇
        /// （`SkillPanel.OnCanvas` · `UnitChatPanel.OnCanvas` · `ChatPopupPanel.OnCanvas`）：
        /// 每一件拿**它自己的节点**去 `GetComponentInParent<Canvas>()`，**⛔ 不读 `_ugui`**。</para>
        /// <para>🔴 **为什么是「根 + 底下那一图一字」这三件，⛔ 不是面板里那 14 件逐颗 `Find`**：
        /// ① **判别力一分不差** —— `Build` 里那 7 图 + 7 字**全建在 `_root` 这棵子树里**（每个
        ///    `Node`/`Img`/`Txt` 的 `parent` 都是 `_root` 或它的子件）⇒ 它们**共用同一条父链**：
        ///    根不在画布上，那 14 件一件都不可能是在。② 而 `TitleHolder` / `Alliance` /
        ///    `NotInaAllianceText` 那三棵（含其 4 颗字）的显隐是**数据驱动**的（`Apply` 按
        ///    「有没有称号 / 有没有联盟」关它们，单机 bot 恒走空串那一支）；`GetComponentInParent`
        ///    的**无参**重载默认 `includeInactive = false`（判据 = 本机 `UnityEngine.CoreModule.dll`
        ///    里那个重载的 IL：给该形参压的是 `ldc.i4.0`；⚠️ 它的**默认值语义**在本机 XML 文档里
        ///    **查不到** ⇒ **本件不拿它当判据**）⇒ 对**没激活**的件，这个口答的是「此刻可不可见」、
        ///    **不是**「建在哪个空间」。把那三棵一并纳入这条判据，`OnCanvas` 就会在「对手没称号」时变假，
        ///    而唯一的消费者（`Editor/BattleScene.cs` 里那句 `left985`，现核 `CardPresentation/Editor/BattleScene.cs`）正是拿它**选左缘
        ///    公式的档**、另有一句拿它当闸决定跑不跑那两条子画布结构断言（`if (panel985.OnCanvas)`，
        ///    现核 `CardPresentation/Editor/BattleScene.cs`；⚠️ 那份文件本会话还在被 `G2` 改 ⇒ 两个号都会漂，**按那两句内容对**）
        ///    ⇒ 会把**画布档判成世界档**（假红 + 静默跳过）。③ 余下 11 件与这 3 件同父链 ⇒ 见 ①。</para>
        /// <para>⚠️ 任一件**该有的**没建出来（贴图/字体缺失）⇒ 也返 `false`，如实回答「不成立」
        /// （同 `UnitChatPanel.OnCanvas` 的 doc）。两档答得都对：世界档根挂在裸 `GameObject`
        /// （`HudRoot`，不在任何 `Canvas` 下）⇒ 假；画布档那三条父链都通到子画布 ⇒ 真。</para></summary>
        public bool OnCanvas
        {
            get { return OnCanvasOf(_root) && OnCanvasOf(_background) && OnCanvasOf(_nameLb); }
        }
        /// <summary>逐件判据（**只此一份**）：这个节点在不在某块 `Canvas` 的父链下。
        /// 与 `SkillPanel.IsUgui` / `UnitChatPanel.IsUguiOnCanvas` / `ChatPopupPanel.IsUguiOnCanvas`
        /// 同一个口、同一条式子（那三处多一项「本件是 uGUI 后端」= `HudPiece` / `Label` 的 `IsUgui`；
        /// 本扇手上的图件是裸 `Transform`、没有 `HudPiece` ⇒ 只留父链那一项）。</summary>
        static bool OnCanvasOf(Transform t)
        {
            return t != null && t.GetComponentInParent<Canvas>() != null;
        }
        static bool OnCanvasOf(Label l)
        {
            return l != null && l.transform.GetComponentInParent<Canvas>() != null;
        }
        public Transform Root { get { return _root; } }
        /// <summary>取不到的美术名（`MenuDraw.Rect` 取不到图会**静默 return null** ⇒ 整层消失，所以自己记账）。</summary>
        public List<string> MissingArt { get { return _missArt; } }
        /// <summary>`NotInaAllianceText` 那个节点（自检：无联盟那一档该显、有联盟时该藏）。</summary>
        public Transform NotInAllianceNode { get { return _notInAlliance; } }
        /// <summary>`Alliance:` 那一组（原版字段 `allianceGO`）。</summary>
        public Transform AllianceGroupNode { get { return _allianceGo; } }
        /// <summary>`Title:` 那一组（原版字段 `playerTitleHolder`）。</summary>
        public Transform TitleHolderNode { get { return _titleHolder; } }
        /// <summary>🆕 2026-10-19（`R01`）：两处评级的容器（`Player Rating Display` / `Alliance Rating Display`）。
        /// 自检用 —— 逐颗结构断言都从这两个口往下找（⛔ 别从面板根按名字找：两棵子树里同名件有两份）。</summary>
        public Transform PlayerRatingNode { get { return _playerRating; } }
        public Transform AllianceRatingNode { get { return _allianceRating; } }
        /// <summary>🆕 2026-10-19（`R01`）：**空值态出过声**（= 不静默）。
        /// 数值（服务端 profile）与 LiveOps 活动图标（key=6）本地都没有 ⇒ `Build` 末尾 `LogWarning` 一次；
        /// 自检拿这一颗判「没静默失败」。</summary>
        public bool RatingEmptyWarned { get { return _ratingWarned; } }
        /// <summary>名字那一段（原版字段 `playerName`）。</summary>
        public Label NameLabel { get { return _nameLb; } }

        // ============================================================ 建
        /// <summary>在 <paramref name="parent"/> 下建出整扇窗（**默认关着**，照原版 `Awake` 的
        /// `SetActive(false)`）。根框位置/尺寸 = 原版那一级（含「顶出屏 57.9 px」那一条）——
        /// 世界档按**绝对矩形** `RootR` 摆（`MenuDraw.ApplyPxRect` ⇒ 在 `LayoutSpace` 那个固定设计帧里，
        /// 非 16:9 会怎样**另论**，不属本件）；画布档按原版**五元组**（`aMin/aMax` + `pivot` + `ap` + `sd`，
        /// 见 `SetRootAnchor`）⇒ 两者**只在 16:9 下等价**；非 16:9 下「点锚跟屏幕左上角走」这条
        /// **只有画布档**表达得出来（差 `(屏高 − 0.5625×屏宽)/2`）。
        /// <para>🔴 **2026-10-16（`T09` 批 3）：第三个实参 `uguiHost` = 双模式开关**（见文件头那一节）。
        /// `null`（默认，= 今天唯一的调用形态 `AlliancePanelWindow.Create(root)`）⇒ **世界档**，
        /// 与加本档之前**逐位相同**；非 null ⇒ **画布档**（建在给定宿主下，走设计 px + 兄弟序）。</para>
        /// <para>⚠️ `uguiHost` 由**调用方**建/取（`HudCanvas.EnsureSubCanvas(null, <see cref="SubCanvasName"/>,
        /// HudCanvas.SortingLayer.OverHand, 0)` 的返回值）——它的名字/层/order 都取本类那两个常量，
        /// ⛔ 别在调用点另写一份。宿主不在任何 `Canvas` 下 ⇒ **出声并且不建**（半迁移态比不迁移更坏：
        /// 一张 `Image` 挂在没有 `Canvas` 的父链下**一个像素都不画**，而调用点会以为建成功了）。</para></summary>
        public static AlliancePanelWindow Create(Transform parent, string name = "Alliance Panel",
                                                 Transform uguiHost = null)
        {
            bool ugui = uguiHost != null;
            if (ugui && uguiHost.GetComponentInParent<Canvas>() == null)
            {
                Debug.LogError("[AlliancePanel] `uguiHost` 不在任何 `Canvas` 下 ⇒ **本扇窗不建**"
                             + "（⛔ 不退回世界空间 quad —— 半迁移态在 16:9 下看不出来）");
                return null;
            }
            // 🔴 **两档的根节点不同**：世界档 = **裸 `Transform`**（与改本件之前**逐字相同** —— `ApplyPxRect`
            //    里那句 `SetPxSize` 对非 `RectTransform` 是空操作，所以今天根上**没有** `sizeDelta`）；
            //    画布档 = **`RectTransform`**（画布下必须有它，否则 `RectTransform` 链会断）。
            var go = ugui ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);
            var c = go.AddComponent<AlliancePanelWindow>();
            c._ugui = ugui;                       // ⚠️ 必须在 `Build()` **之前**赋值
            c._root = go.transform;
            // 🆕 2026-10-27（E9 · 账 187①）：原版 `canvasGroup`（字段 `+0x68`）就挂在这根节点上
            //   ⇒ 我们同样在根上加一颗（`Open` 的 `DOFade` 目标就是它，见 `StartAppear`）。
            //   ⚠️ 世界档（`ugui=false`）那几颗是 `ImageQuad`（MeshRenderer），**`CanvasGroup` 管不到它们**
            //      ⇒ 淡入只在画布档看得见；缩放两档都有效（如实记，⛔ 不假装世界档也淡入）。
            c._cg = go.AddComponent<CanvasGroup>();
            c._baseScale = go.transform.localScale;
            if (ugui)
            {
                go.transform.SetParent(uguiHost, false);
                // 根框 = 原版 `RectTransform_3052` 那**五元组**（见上面那组 `RootAnchor*` / `RootPivot` /
                // `RootAnchoredPos` / `RootSizeDelta`）—— **锚点语义逐值照抄**。
                // ⛔ **不是** `HudCanvas.SetPxSize` + `PosPx`（那是「中心锚 + 固定设计 px」，与批 2/3 各扇
                //    同一套）：16:9 下与锚点档**逐位相同**，非 16:9 会**整扇跑偏**（4:3 差 **135 屏 px**）。
                // ⛔ **不是** `FillParent`（那是「铺满父件」；本件根有**自己的** rect）·
                // ⛔ **不是** `MenuDraw.ApplyPxRect`（它体内 `LayoutSpace.Px` = **÷108** ⇒ 缩成 1/108、不报错）。
                // ✅ **换枢轴【不动】子件**：下面那十几个件一律 `anchorMin = anchorMax = (0.5,0.5)`
                //    ⇒ 它们的锚点落在**父件中心**（本地坐标 `(0.5 − 父pivot) × 父尺寸`，**与父 pivot 无关**）
                //    ⇒ 换 pivot 只动「父件这块矩形摆在画布哪儿」，子件的相对落点逐件不变（`HudCanvas` 那三个
                //    `Make*InParent` 口算的正是「相对父中心」）。唯一**受**影响的是 `HitBody`（它读的是
                //    `InverseTransformPoint` = 相对**枢轴**）—— 那条已同批补了 `RootCenterLocal`。
                SetRootAnchor((RectTransform)go.transform);
            }
            else
            {
                go.transform.SetParent(parent, false);
                MenuDraw.ApplyPxRect(go.transform, parent, RootR);
            }
            c.Build();
            c.Close();
            return c;
        }

        /// <summary>🔴 把**根那一级**摆成原版 `RectTransform_3052` 的五元组（**只画布档用**）；
        /// 世界档不动（它走 `MenuDraw.ApplyPxRect` + `RootR`，逐位不变）。
        /// <para>⚠️ 父件（子画布 `Alliance Panel Canvas`）是**铺满根 canvas** 的 stretch 矩形、枢轴居中 ⇒
        /// 锚点 `(0,1)` 落在**屏幕左上角**。🔴 `Expand` 档下画布**宽恒 1920 单位**、**高随屏变**
        /// （4:3 ⇒ 1440）⇒ 「屏幕左上角」与「画布中心」**不是一回事** —— 后者正是旧写法隐含的假设，
        /// 也是那 135 屏 px 的来源。</para>
        /// <para>✔ **自证**（算式 → 报告 §④）：`(CW,CH) = (1920,1080)` 时本式给出
        /// `x[-107.000, 708.044] y[-57.936, 417.534]`，与旧写法那份 `RootR`（`x[-107.00,708.02]
        /// y[-57.93,417.52]`）**逐边差 ≤0.024 px**（`RootR` 乘过根那个 `0.9999718`、`sizeDelta` 没乘）。</para></summary>
        static void SetRootAnchor(RectTransform rt)
        {
            if (rt == null) return;
            rt.anchorMin = RootAnchorMin;
            rt.anchorMax = RootAnchorMax;
            rt.pivot = RootPivot;
            rt.anchoredPosition = RootAnchoredPos;
            rt.sizeDelta = RootSizeDelta;
        }

        /// <summary>🔴 **面板【中心】在根本地坐标里的偏移**（本地原点 = **枢轴**、`y` **向上**、单位 = 设计 px）。
        /// <para>给 <see cref="HitBody"/> 用：`RectTransform.InverseTransformPoint` 给的是**相对枢轴**的坐标，
        /// 而原版这根 `RootPivot = (0.13, 0.825)` **不是**中心 ⇒ 这一项 = `(0.5 − pivot) × sizeDelta`
        /// = `(+301.566, −154.528)` px，**非零**。⛔ 别当成 0 —— 那是「枢轴居中」那一档的隐含假设
        /// （旧写法正是那样，所以旧代码里没有这一项）。</para>
        /// <para>⚠️ 与 `RootPivot` / `RootSizeDelta` **同一份判据**（⛔ 别在 `HitBody` 里另抄一遍数）。</para></summary>
        static Vector2 RootCenterLocal
        {
            get
            {
                return new Vector2((0.5f - RootPivot.x) * RootSizeDelta.x,
                                   (0.5f - RootPivot.y) * RootSizeDelta.y);
            }
        }

        void Build()
        {
            _missArt.Clear();
            // 🆕 2026-10-20（A3-e）：重建时把输入层的账也清掉（⛔ 不清 = 老件的探针读到已销毁对象）
            _blockLabels.Clear();
            _ratingIcons.Clear();
            _ratingValues.Clear();
            _shade = null;
            _bg = null;
            _closeBtn = null;
            _closePending = false;

            // ---- ① 熄灯层（`Close Background`）：铺满全屏的黑 + **点它 = 关窗** ----
            // 原版：那颗 `Image` 的 `m_RaycastTarget = 1`，而「关窗」是 `Awake` 给它的
            // `BackgroundCloseButton.onClick` 挂上 `CloseButtonClick`（`BattleAlliancePanel__Awake.c` 逐句）
            // ⇒ 语义 = 「点压暗层就关」。⚠️ 它**不是**「只在窗外」——面板底（`BG`/`BGFrame`）`rt=0`，
            //    点在面板空白处同样落到这一层（判据 → 文件头那一节，2026-10-20 现读）。
            // ⚠️ 那张 Image 的 `m_Sprite` 是 **0**（无贴图）、染色**纯黑 α0.4627** ⇒ 用实底画等价。
            if (_ugui)
            {
                // 画布档：同一块矩形、同一份染色，只是改由 `HudCanvas` 建（设计 px + 兄弟序）。
                // 🔴 **⛔ 不建 `MenuDraw.ShadeHit` 那颗命中区**：它是 `WindowButton` + 世界空间 quad
                //    （只有壳侧 `PointerLayer` 派发）⇒ 画布档改由**原版那条链**接：这颗 `Image`
                //    自己 `raycastTarget = true` + `BattleHudButton`（`IPointerClickHandler`）。
                _shade = Img(_root, CardArt.Solid(), ShadeR, RootR, "Close Background", QShade,
                             new Color(0f, 0f, 0f, ShadeAlpha));
                BuildUguiClose();
            }
            else
            {
                // 世界档：走公共件 `MenuDraw.ShadeHit`（它自己校验档 < `QContentMin`，不合法当场告警）。
                var shade = MenuDraw.Rect(_root, CardArt.Solid(), ShadeR, "Close Background", QShade,
                                          new Color(0f, 0f, 0f, ShadeAlpha));
                if (shade != null)
                    MenuDraw.ShadeHit(shade.transform, ShadeR, QShade, QContentMin, Close, "CloseHit");
            }

            // ---- ② 面板本体：`Background`（空节点）+ `BG`（内容底）+ `BGFrame` 四块 ----
            _background = Node(_root, RootR, "Background", BackgroundR, QBg);
            // 🔴 **2026-10-20（A3-e）**：原版 `BG` 的 Image `m_RaycastTarget = **0**`（`MB_4162`，arena1/2 同值）
            //    ⇒ ⛔ **它不是射线靶**（本文件旧注释写 1 是错的，已订正）。`MakeImageInParent` 出厂就是
            //    `raycastTarget = false` ⇒ 保持默认；`_bg` 只为自检留一个「它确实是 false」的读口。
            _bg = Img(_background, Art("40K_shop_offer_bg_Sororitas_0"), BgR, BackgroundR, "BG", QBg, BgTint);
            var frame = Node(_background, BackgroundR, "BGFrame", FrameR, QFrame);
            Img(frame, Art("40k_UnitChat_Background_Left"), FrameLeftR, FrameR, "BGFrame Left", QFrame);
            Img(frame, Art("40k_UnitChat_Background_Right"), FrameRightR, FrameR, "BGFrame Right", QFrame);
            Img(frame, Art("40k_UnitChat_Background_Top"), FrameTopR, FrameR, "BGFrame Top", QFrame);
            Img(frame, Art("40k_UnitChat_Background_Bottom"), FrameBottomR, FrameR, "BGFrame Bottom", QFrame);

            // ---- ③ 吸收层（**只有世界档**）：点窗内空白处**什么都不该发生** ----
            // 🔴 **2026-10-20（A3-e）就地订正（铁律 5）**：本段原写「原版 `BG` 那张图 `m_RaycastTarget = 1`，
            //    父链上没有处理器 ⇒ 射线打到面板自己 ⇒ 无事发生」—— **前提是错的**：`BG` / `BGFrame` 四块
            //    在 prefab 里全是 **rt=0**（`MB_4162` / `4691` / `5261` / `4301` / `4338`，arena1/2 同值）
            //    ⇒ 原版点**面板空白处**会落到压暗层 = **关窗**（这正是文件头那一节的结论）。
            //    下面这块 `MenuDraw.Absorb` 是**世界档的壳侧口径**（唯一消费者 = 壳 `PointerLayer`，
            //    它的窗口语义是「点窗内空白不关」）⇒ **世界档照旧保留、逐位不变**；⛔ 别把它当成原版判据。
            //    画布档不建它：那边的射线语义已由 ① 的压暗层 + 那 7 颗 TMP/两颗图标（rt=1）表达，与原版同构。
            if (!_ugui) MenuDraw.Absorb(_root, "BackgroundAbsorb", BackgroundR, QShade, QContentMin);

            // ---- ④ 头像框（`Avatar Item Small` → `Image Container` → `Border`）----
            // `Avatar Item Small` 自己是个 `Button` 但 `m_interactable = 0` ⇒ 不可点、不加命中区。
            // ⚠️ `Image`（立绘）`m_Sprite = 0`（运行期灌）· `Highlight` `m_Enabled = 0` · `Raycast Target` `act=F`
            // 🔴 **2026-10-29（`B95_MISC` · 本批①）就地订正（铁律 5）**：原写「本件只摆框（缺口①）」——**缺口① 现补上**。
            //   判据 `AvatarDisplay__ChangeAvatar.c:34` 的 `op_Equality(param_2, 0)` ⇒ **传 null 整段跳过、不画**；
            //   `:53-70` ⇒ 有图才 `sprite = get_Image(item)` + `set_enabled(img, sprite != null)`。
            //   ⛔ 「没数据就画 DefaultAvatar」是**错口径**（那条回落只在「拿到 Avatar 对象但图取不到」时走）。
            //   立绘节点**惰性建**（`Apply` 里第一次拿到真贴图才建）⇒ 没数据时一个节点都不建，与原版 `m_Sprite = 0` 同态。
            _avatarBorderImg = Img(_root, Art("Player_Profile_Border"), AvatarBorderR, RootR, "Border", QAvatar,
                null, keepAspect: true);

            // ---- ⑤ `EnemyInfo`：`Name:` / 名字 · `Title:` / 称号 ----
            //  🔴 那三个 holder 在 prefab 里是 **0×0 且都落在 `PlayerInfo` 的锚点上** = `VerticalLayoutGroup`
            //     **跑之前的模板位**（`MonoBehaviour_4213`：spacing 0 · UpperLeft · `childControlWidth = 1` ·
            //      **`childControlHeight = 0`**）⇒ 按 `UguiLayout.VerticalChild` 重算：
            //       `PlayerInfo` 实绘框 = x[180.000, 556.959] y[62.498, 213.723]（RT 3363）
            //       第 0 格 `NameHolder`  高 42.573 ⇒ y[62.498, 105.071]
            //       第 1 格 `TitleHolder` 高 38.442 ⇒ y[105.071, 143.513]
            //       第 2 格 `Player Rating Display` 高 60.539 ⇒ y[143.513, 204.052]（✅ 2026-10-19 `R01` 已建，见 ⑤b）
            //     ⛔ 别照抄报告里那三个重叠的 y（那是模板位）。
            var nameRow = Node(_root, RootR, "NameHolder", NameRowR, QText);
            // 🔴 **A1024③（2026-10-17）换面**：原版这七颗（`PlayerLabel` / `Name Text` / `TitleLabel` /
            //    `Title Text` / `Alliance Label` / `Alliance Name` / `NotInaAllianceText`）的
            //    `m_fontAsset` **全是 `Asar-Regular SDF`**（判据 = `menu_dump … bundle_scenes_scenes_battlearena1
            //    "Alliance Panel"` 逐颗现读；§2·5 表第一行同结论）⇒ 走 `LatinUi`。
            //    `Avatar Name` / `Individual rating value` 是 Pragati —— 前者 `m_IsActive=false` 本扇不建；
            //    后者 ✅ 2026-10-19（`R01`）已建（`RatingDisplay` 里用 `LatinCardBody`）。
            // 🆕 **2026-10-20（A3-e）**：这七颗在原版是 **`m_RaycastTarget = 1`**（`MB_4073` / `4118` / `4646` /
            //    `4901` / `4098` / `5236` / `4475`，全部 pad 0）⇒ 画布档显式打开（**零处理器**：挡射线、不响应）。
            //    ⚠️ 我们的 TMP 只占文字 bbox（见文件头），挡射线的那块比原版节点 rect 小。
            BlockRaycasts(Txt(nameRow, NameRowR, NameLabelR, LocOr(NameLabelTerm, NameLabelEn), Color.white,
                              "PlayerLabel", NameLabelPx, Label.VAlign.Capline, face: TmpFont.Face.LatinUi));
            // ⚠️ **只有这一颗**原版 `m_fontSizeMax` 是 **40**（其余六颗都是 35）⇒ 显式给上限，⛔ 别一刀切
            _nameLb = Txt(nameRow, NameRowR, NameTextR, "", Color.white, "Name Text", NameTextPx,
                          Label.VAlign.Capline, AutoMaxNamePx, TmpFont.Face.LatinUi);
            BlockRaycasts(_nameLb);

            _titleHolder = Node(_root, RootR, "TitleHolder", TitleRowR, QText);
            BlockRaycasts(Txt(_titleHolder, TitleRowR, TitleLabelR, LocOr(TitleLabelTerm, TitleLabelEn),
                              Color.white, "TitleLabel", TitleLabelPx, Label.VAlign.Capline,
                              face: TmpFont.Face.LatinUi));
            _titleLb = Txt(_titleHolder, TitleRowR, TitleTextR, "", Color.white, "Title Text", TitleTextPx,
                           Label.VAlign.Midline, face: TmpFont.Face.LatinUi);
            BlockRaycasts(_titleLb);

            // ---- ⑤b 🆕 2026-10-19（`R01`）：`Player Rating Display`（原版 `PlayerInfo` 的 VLG **第 2 格**）----
            //  判据见上面那一组 `PlayerRatingR` 的注释。⚠️ `PlayerInfo` 这个容器本文件**不建**（同
            //  `NameHolder` / `TitleHolder` 的先例：VLG 那一层的排版已折进各自的绝对 rect）⇒ 父 = `_root`。
            _playerRating = RatingDisplay(_root, RootR, "Player Rating Display", PlayerRatingR,
                                          PlayerRateEmpty);

            // ---- ⑥ `Alliance` 组：`Alliance:` / 联盟名 ----
            //  `allianceGO`（原版字段 0x48）—— **无联盟时整组关掉**（`BattleAlliancePanel__Open.c`）。
            //  ⚠️ 组里的 `Alliance Badge Drawer/{Frame,Badge}` 两张图 `m_Sprite` 也是 `0`（`AllianceBadgeDrawer.Draw`
            //     运行期灌）⇒ 🆕 **2026-10-28（B88-B7 · 账 187③）：这两层现在建了**（原版第 3 子件，见下）。
            _allianceGo = Node(_root, RootR, "Alliance", AllianceGroupR, QText);
            BlockRaycasts(Txt(_allianceGo, AllianceGroupR, AllianceLabelR, LocOr(AllianceLabelTerm, AllianceLabelEn),
                              Color.white, "Alliance Label", AllianceLabelPx, Label.VAlign.Capline,
                              face: TmpFont.Face.LatinUi));
            _allianceLb = Txt(_allianceGo, AllianceGroupR, AllianceNameR, "", Color.white, "Alliance Name",
                              AllianceNamePx, Label.VAlign.Capline, face: TmpFont.Face.LatinUi);
            BlockRaycasts(_allianceLb);
            // ---- ⑥b 🆕 2026-10-28（B88-B7 · 账 187③）：`Alliance Badge Drawer`（`Frame` + `Badge` 两层）----
            //   位置在**兄弟序第 3**（`Alliance Label` → `Alliance Name` → **本件** → `Alliance Rating Display`），
            //   与原版 `m_Children = [2635, 3272, 3503, 3110]` 逐位一致。
            //   出厂画的是**默认那一枚**（`Default_Badge` + `TIER1`）= 原版 `Draw(null)` 的回落支
            //   （`AllianceBadgeDrawer__Draw.c`：徽章取不到 ⇒ `get_DefaultTrophy()`）；
            //   有真 id 时由 `Apply(..., badgeId, tier)` 换图（⛔ 不再重建节点）。
            var badgeGo = Node(_allianceGo, AllianceGroupR, "Alliance Badge Drawer", AllianceBadgeDrawerR, QAvatar);
            _allyBadgeFrame = Img(badgeGo, AllianceTrophyArt.GetTierFrame(1), AllianceBadgeDrawerR, AllianceGroupR,
                                  "Frame", QAvatar, null, true);
            if (_allyBadgeFrame == null) Node(badgeGo, AllianceGroupR, "Frame", AllianceBadgeDrawerR, QAvatar);
            _allyBadge = Img(badgeGo, AllianceBadgeArt.Get(null), AllianceBadgeDrawerR, AllianceGroupR,
                             "Badge", QAvatar + 1, null, true);
            if (_allyBadge == null) Node(badgeGo, AllianceGroupR, "Badge", AllianceBadgeDrawerR, QAvatar + 1);
            // 🆕 2026-10-19（`R01`）：`Alliance Rating Display` —— 原版 `Alliance` 组的**第 4 个子件**
            //  （我们原来排第 3，因为 `Alliance Badge Drawer` 没建；2026-10-28 起本件补上 ⇒ 恢复第 4）。
            //  ⚠️ 原版只在**联盟名非空**时 `Initialize` 这一格（`BattleAlliancePanel__Open.c` 的 `if (bVar9)` 支）
            //  ⇒ 显隐跟着 `_allianceGo`（本方法 ⑥ 已按 `hasAlliance` 开关它），这里**不再单判一次**。
            _allianceRating = RatingDisplay(_allianceGo, AllianceGroupR, "Alliance Rating Display",
                                            AllianceRatingR, AllianceRateEmpty);

            // ---- ⑦ `NotInaAllianceText`：无联盟那一行提示（**原文含 `is is` 双 is = 原版笔误，勿改**）----
            var msg = Txt(_root, RootR, NotInAllianceR, LocOr(NotInAllianceTerm, NotInAllianceEn),
                          new Color(1f, 1f, 1f, NotInAllianceAlpha), "NotInaAllianceText",
                          NotInAlliancePx, Label.VAlign.Midline, face: TmpFont.Face.LatinUi);
            BlockRaycasts(msg);            // 原版 `MB_4475` rt=1（见文件头那一节）
            _notInAlliance = msg != null ? msg.transform : null;

            // 🆕 2026-10-19（`R01`）：两格评级的**数值/活动图标**在远端 ⇒ 出厂空态 + **出声一次**（⛔ 不静默）
            WarnRatingOnce();

            Apply("", "", "");      // 出厂态 = 原版 `Awake` 那一支（`Create` 末尾再关一次）
        }

        /// <summary>🆕 2026-10-19（`R01`）：两处评级的**空值态出声**（每扇窗一次）。
        /// <para>为什么必须出声：① 数值是服务端 profile（`DeckAndWarlordData.eloRating@0x38` /
        /// `allianceRating@0x7C`，经 `BattleHud__AlliancePanelOpenButton.c:25-46` 喂进 `Open`）；
        /// ② 图标是 LiveOps 活动资产 key=6（`LiveOpsAssetUtility.HasAsset/LoadAsset(ev, 6)`，
        /// `BattleAlliancePanel__Open.c` 那一段）—— 本地 13 个活动 SO 的 `EventAssets` **都没有 key=6**。
        /// ⇒ 我们照原版**离线态的逐句等价物**做：空态串取 prefab 自带字面量、图标取 prefab 自带的
        /// `40k_UI_icon_ranked_Skirmish`（`Initialize` 只在传入非空图标时才覆盖它）—— 但**必须说出来**，
        /// 别让玩家以为那串破折号是「真的 0 分」。⛔ 不编数值、不自造图标。</para></summary>
        void WarnRatingOnce()
        {
            if (_ratingWarned) return;
            _ratingWarned = true;
            Debug.LogWarning("[AlliancePanel] 两处评级的**数值与活动图标都在远端** ⇒ 本窗显示原版 prefab 自带的空态："
                           + "数值 = `" + PlayerRateEmpty + "` / `" + AllianceRateEmpty + "`（服务端 profile "
                           + "`DeckAndWarlordData.eloRating@0x38` / `allianceRating@0x7C`，本地无源）；"
                           + "图标 = prefab 自带的 `" + RatingIconArt + "`（LiveOps 活动资产 key=6 本地没有，原版只在有它时才覆盖）。"
                           + "⛔ 没有编造数值/图标。");
        }

        /// <summary>🆕 2026-10-19（`R01`）：建一处评级 —— 原版两格的**结构逐字段相同**：
        /// 容器（`HorizontalLayoutGroup`）+ `Main Icon`（Image，`preserveAspect`）+ `Individual rating value`（TMP）。
        /// <para>⚠️ `Secondary Icon`（原版 `m_Children[0]`）**不建** —— 它 `m_IsActive = false`（act **F**），
        /// 原版出厂态不显示、也不进布局组的 `rectChildren`（同 `AllianceMemberTab` 那颗的处理）。</para>
        /// <para>子件顺序 = **Main Icon → Individual rating value**（原版 `m_Children` 的 2/3 位，第 1 位那颗不建）
        /// ⇒ 布局组的「从左到右」与实际渲染顺序一致（`HudCanvas.Add` 对同一 z 档是按创建序追加的）。</para></summary>
        Transform RatingDisplay(Transform parent, PxRect parentR, string name, PxRect r, string emptyText)
        {
            var box = Node(parent, parentR, name, r, QText);
            if (box == null) return null;
            var iconR = new PxRect(r.x1, r.y1, r.x1 + RatingIconW, r.y2);
            var valR = new PxRect(r.x1 + RatingIconW, r.y1, r.x2, r.y2);

            var icon = Img(box, Art(RatingIconArt), iconR, r, "Main Icon", QText, null, keepAspect: true);
            // 🆕 2026-10-20（A3-e）：原版 `Main Icon` 的 Image `m_RaycastTarget = 1`
            //   （`MB_4325` / `MB_4327`，pad 0；`Secondary Icon` 那颗 `m_IsActive = false` ⇒ 不参与）
            //   ⇒ **挡射线、零处理器**：点在这颗图标上不关窗（与那 7 颗 TMP 同一条口径）。
            if (icon != null) _ratingIcons.Add(icon);
            if (_ugui && icon != null && !icon.SetRaycastTarget(true))
                Debug.LogError("[AlliancePanel] `Main Icon` 的 `raycastTarget` **没写进去**（原版 rt=1，见 BuildUguiClose 同族）");
            if (_ugui && icon != null && icon.Rect != null)
            {
                // 原版 `m_PreserveAspect = 1`：贴图 128×128、格子 48×48.43 ⇒ 等比画 48×48（居中）。
                // ⚠️ 布局组会把格子的 `sizeDelta` 钉成 48×48.43（这正是原版布局跑出来的那一档），
                //    ⇒ 等比**必须由 `Image.preserveAspect` 表达**，⛔ 不能只靠上面 `keepAspect` 那一下的静态内缩。
                var img = icon.Rect.GetComponent<UnityEngine.UI.Image>();
                if (img != null) img.preserveAspect = true;
                // 原版 `LayoutElement{m_MinWidth 60, m_PreferredWidth 60, m_FlexibleWidth 0}` × 0.8。
                AddLayoutElement(icon.Rect, RatingIconW, RatingIconW, 0f, -1f, -1f, -1f);
            }

            var val = Txt(box, r, valR, emptyText, Color.white, "Individual rating value",
                          RatingValuePx, Label.VAlign.Midline, RatingValueMaxPx,
                          face: TmpFont.Face.LatinCardBody, wrap: true, autoBasePx: RatingValueBasePx);
            // 🆕 2026-10-20（A3-e）：原版这一颗的 TMP `m_RaycastTarget = **0**`（`MB_5118` / `MB_5121`）
            //   ⇒ ⛔ **不要**调 `BlockRaycasts`（保持 `TmpFont.NewText` 的出厂 false）—— 与同组那颗
            //   `Main Icon`（rt=1）**一开一关**，这是原版逐值，别一刀切。
            _ratingValues.Add(val);
            if (_ugui && val != null)
            {
                // 🔴 **枢轴必须改成左缘（0, 0.5）** —— 否则布局组一跑，字会从格子的**中心**起笔：
                //    `Label.RefreshBounds` 把整块字墨摆在**本节点的枢轴**上（`Label.anchor.x = 0` 那条算式），
                //    而 `LayoutGroup.SetChildAlongAxisWithScale` 只写 anchor / anchoredPosition / sizeDelta、
                //    **不动 pivot** ⇒ 枢轴留在中心时，布局把格子摆到 `[x1, x2]`，字墨却从 `x1 + W/2` 开始。
                //    ⚠️ `MakeText` 给的 `localPosition` 语义已经是「左缘」（`r.x1`）⇒ 改枢轴**不用再挪**。
                ((RectTransform)val.transform).pivot = new Vector2(0f, 0.5f);
                // 原版 `LayoutElement{m_PreferredWidth 5000}`（其余三格 = -1 照抄）。
                // ⚠️ 5000 **就是原值**、不乘 0.8：它只起「大过容器 ⇒ 吃掉剩余宽」的作用，
                //    布局算式 `Lerp(min, preferred, (size − totalMin)/(totalPreferred − totalMin))`
                //    对剩余宽那一件的**结果与它无关**（= size − 图标宽）。⛔ 别「顺手改成 4000」。
                AddLayoutElement((RectTransform)val.transform, -1f, 5000f, -1f, -1f, -1f, -1f);
            }

            AddRatingLayout(box);
            return box;
        }

        /// <summary>🆕 2026-10-19（`R01`）：原版 `HorizontalLayoutGroup` 逐字段照抄（MB 4788 / 4785）。
        /// <para>🔴 **为什么只建在画布档**：世界档整棵树挂在**裸 `Transform`** 下（没有 `Canvas`）——
        /// 那个 `RectTransform` 只用来带 rect，**uGUI 的布局重建不会跑**（`LayoutRebuilder` 由 canvas 的
        /// layout 重建驱动）⇒ 挂上去是死件、只会让「字段看着对、版面没动」，比不挂更糟。
        /// 世界档按算好的实绘 rect 静态摆放（结果逐位相同）。</para>
        /// <para>⚠️ `childControlWidth/Height = 1` 是**原版真的就是这么摆的**（不是装饰）：
        /// 宽度由 `LayoutElement` 的首选宽算（图标 48 / 文本吃剩余），高度恒 = 容器高（`ForceExpandHeight = 1`）。
        /// 自检会把这四个开关读回来 —— 改掉任何一个都会红。</para></summary>
        void AddRatingLayout(Transform node)
        {
            if (!_ugui) return;                       // 世界档没有 uGUI 布局系统（见 doc）
            var rt = node as RectTransform;
            if (rt == null) return;
            var h = rt.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            h.padding = new RectOffset(0, 0, 0, 0);     // m_Padding 0,0,0,0
            h.childAlignment = TextAnchor.MiddleLeft;  // m_ChildAlignment 3
            h.spacing = 0f;                            // m_Spacing 0
            h.childForceExpandWidth = false;           // m_ChildForceExpandWidth 0
            h.childForceExpandHeight = true;           // m_ChildForceExpandHeight 1
            h.childControlWidth = true;                // m_ChildControlWidth 1
            h.childControlHeight = true;               // m_ChildControlHeight 1
            h.childScaleWidth = false;                 // m_ChildScaleWidth 0
            h.childScaleHeight = false;                // m_ChildScaleHeight 0
            h.reverseArrangement = false;              // m_ReverseArrangement 0
        }

        /// <summary>🆕 2026-10-19（`R01`）：给评级子件补 `LayoutElement`（值照原版，仅 60→48 那一步按实绘换算）。</summary>
        static void AddLayoutElement(RectTransform rt, float minW, float prefW, float flexW,
                                     float minH, float prefH, float flexH)
        {
            if (rt == null) return;
            var le = rt.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            le.minWidth = minW; le.preferredWidth = prefW; le.flexibleWidth = flexW;
            le.minHeight = minH; le.preferredHeight = prefH; le.flexibleHeight = flexH;
            le.layoutPriority = 1;                     // m_LayoutPriority 1
        }

        // ============================================================ 状态 → 参数（原版 `Open` 的逐句等价物）
        /// <summary>原版 `BattleAlliancePanel.Open(name, title, rank, avatarItem, avatarItem2, allianceName, …)`
        /// 的**等价物**（`BattleAlliancePanel__Open.c` 逐句读过；头像/评级/徽章那三条见文件头缺口①②）。
        /// <para>**显隐三支**（原版就是这三条 `SetActive`）：</para>
        /// · `playerTitleHolder` —— **称号为空 ⇒ 关**；<br/>
        /// · `allianceGO` —— **联盟名为空 ⇒ 关**；<br/>
        /// · `notInAnAllianceMessage`（那个 TMP 的 `gameObject`）—— **联盟名为空 ⇒ 开**（并把文案设成
        ///   `GetTranslation("Battle/AlliancePanel/NotInAnAlliance")`），否则关。<br/>
        /// ⚠️ 原版判「称号空不空」用的是**称号**那一个实参（`param_3`）、判「有没有联盟」用的是**联盟名**
        /// （`param_7`）—— 本方法逐条照抄，⛔ 别把两者合并成一个「有没有档案」。
        /// <para>🆕 **2026-10-28（B88-B7 · 账 187③）**：带联盟徽章的那一版 —— 原版 `Open` 的第 8 个实参
        /// `GroupBadge allianceBadge`（`BattleAlliancePanel.cs:55`）。我们收的是**拆平后的两格**：
        /// `badgeId` = `GroupBadge.featuredTrophy.reward.uniqueId`、`badgeTier` = `GroupBadge.tierLevel`
        /// （id → 图 见 `AllianceBadgeArt`）。`badgeId` 为空 ⇒ **原版同一条回落**（`DefaultTrophy`），⛔ 不是我们挑的图。
        /// <para>🆕 **2026-10-29（`B95_MISC` · 本批①）**：原「三实参版 + 七实参版」两个重载**合并成这一个**（全可选形参），
        /// 末尾追加两格头像 —— `avatarArt` / `avatarBorderArt` 都是**我们自己的美术键**（`Avatar_&lt;阵营>_&lt;名>`，
        /// 走 `CardArt.Cosmetics`，见 `Resources/Art/avatars/`）；⛔ 本仓**没有** `PlayerAvatar` / `CosmeticItemAvatarBorder`
        /// 这两个类（全仓 0 命中），所以不照原版那两个类型签名。老调用点（3 实参 / 7 实参）**零改动**。</para></summary>
        public void Open(string playerName, string title, string allianceName,
                         string badgeId = null, int badgeTier = 0,
                         int playerScore = -1, int allianceScore = -1,
                         string avatarArt = null, string avatarBorderArt = null)
        {
            // 🆕 2026-10-20（A3-e）：上一扇（或上一次点击）留下的「待关」**不许**落到刚开的这一扇上
            //   —— 没有这一句时，「关窗点击落账 → 同帧有人又 Open」会让新窗在 `LateUpdate` 立刻被关掉。
            _closePending = false;
            gameObject.SetActive(true);
            Apply(playerName, title, allianceName, badgeId, badgeTier, playerScore, allianceScore, avatarArt, avatarBorderArt);
            StartAppear();          // 🆕 2026-10-27（E9 · 账 187①）：原版末尾的 DOFade + DOScale
            _visible = true;
        }

        /// <summary>原版 `BattleAlliancePanel.CloseButtonClick`（= `Close Background` 那颗钮的 `onClick`）：
        /// **只 `SetActive(false)`** —— 不清数据、不动别的窗。</summary>
        public void Close()
        {
            gameObject.SetActive(false);
            _visible = false;
        }

        // ============================================================ 🆕 2026-10-20（`T09` A3-e）输入层
        /// <summary>画布档的压暗层输入：`raycastTarget = true` + `BattleHudButton`（= 原版 `BackgroundCloseButton`）。
        /// <para>判据 → 文件头那一节：原版 `Close Background` 的 Image `rt=1` · pad `(0,0,0,0)`，
        /// 事件 = `IPointerClickHandler`（**松手**），回调 = `BattleAlliancePanel.CloseButtonClick`（`SetActive(false)`）。</para>
        /// <para>⚠️ 任一步失败都 `LogError`（⛔ 不静默：那会变成「看得见的压暗层、点不动」）。</para></summary>
        void BuildUguiClose()
        {
            if (!_ugui) return;
            if (_shade == null)
            {
                Debug.LogError("[AlliancePanel] `Close Background` 没建出来 ⇒ **点压暗层关不了窗**"
                             + "（建件口返回 null —— 贴图/画布问题，见 `Img` 的日志）");
                return;
            }
            _closeBtn = BattleHudButton.Attach(_shade, OnUguiCloseClick);
            if (_closeBtn == null)
            {
                Debug.LogError("[AlliancePanel] 压暗层没挂上 uGUI 点击 ⇒ **点窗外/空白处关不了窗**"
                             + "（`BattleHudButton.Attach` 已说明原因）");
                return;
            }
            // 原版 `m_RaycastPadding = (0,0,0,0)` —— 显式写一次，⛔ 别让它吃到 `Image` 的出厂别的值。
            if (!_shade.SetRaycastPadding(0f, 0f, 0f, 0f))
                Debug.LogError("[AlliancePanel] 压暗层的 `raycastPadding` **没写进去**（原版四边全 0）");
        }

        /// <summary>🆕 2026-10-20（A3-e）：把一颗文字变成**只挡射线、不响应**的件
        /// （原版那 7 颗 TMP `m_RaycastTarget = 1` 且**父链零处理器**）。
        /// <para>⚠️ 只在画布档做（世界档的 `TextMeshPro` 吃不到 uGUI 射线，且世界档承诺逐位不变）。</para>
        /// <para>⚠️ 我们的 TMP 节点只占**文字 bbox**（`Label.RefreshBounds`），原版吃射线的是**节点 rect**
        /// ⇒ 我们挡射线的那块比原版小（文件头已如实记）。</para></summary>
        void BlockRaycasts(Label lb)
        {
            if (!_ugui || lb == null) return;
            _blockLabels.Add(lb);
            // `Label` 的 TMP 既可能是同节点组件、也可能挂在子节点上（`RefreshBounds` 只搬它的 rect）
            // ⇒ 用 `GetComponentInChildren(..., includeInactive: true)` 取，与 `MultiCardDisplay` 标题同一条路。
            var g = lb.GetComponentInChildren<UnityEngine.UI.Graphic>(true);
            if (g == null)
            {
                Debug.LogWarning("[AlliancePanel] `" + lb.name + "` 没有 `Graphic` 可取 ⇒ 这颗字**不吃射线**"
                               + "（原版 TMP 是 rt=1 —— 点它会把射线漏给压暗层 = 关窗）");
                return;
            }
            g.raycastTarget = true;
        }

        /// <summary>uGUI 压暗层被点（`BattleHudButton.OnPointerClick`，= 原版 `BackgroundCloseButton`）。
        /// <para>🔴 **只记「待关」，不当场 `Close()`** —— `EventSystem.Update` 与 `BattleDriver.Update` 同序；
        /// 当场关掉会让驱动那一帧看到「窗本来就关着」⇒ 这一下继续落到棋盘/手牌（静默的「点一下做两件事」）。
        /// 实际落账在 `LateUpdate`（Unity 保证所有 `Update` 跑完才跑它 ⇒ 两种顺序都安全，且同帧）。</para></summary>
        void OnUguiCloseClick()
        {
            if (!_visible) return;               // 窗关着 ⇒ 这一下不算（根节点本来也收不到射线，这是双保险）
            _uguiCloseClicks++;
            _closePending = true;
        }

        /// <summary>把挂起的「关窗」落账（幂等）。**批处理没有帧循环** ⇒ 自检手动调它；
        /// 真机由 `LateUpdate` 每帧调（见 `OnUguiCloseClick` 的理由）。</summary>
        /// <returns>真 = 这次确实落了一次挂起的关。</returns>
        public bool ApplyPendingClose()
        {
            if (!_closePending) return false;
            _closePending = false;
            _uguiCloseApplied++;
            if (_visible) Close();               // 驱动那条手搓路可能已经在同一帧关过 ⇒ 不再重复
            if (CloseRequested != null) CloseRequested();
            return true;
        }

        void LateUpdate()
        {
            ApplyPendingClose();
            TickAppear();          // 🆕 2026-10-27（E9 · 账 187①）：出场动画（DOFade + DOScale 的等价物）
        }

        // ============================================================ 🆕 2026-10-27（E9 · 账 187①）出场动画
        /// <summary>原版 `BattleAlliancePanel.Open` 末尾那两跳（判据 `BattleAlliancePanel__Open.c:99-125`）：
        /// `canvasGroup.alpha = 0` → `DOFade(1.0, 0.35s)`；`transform.localScale = base × 0.75` → `DOScale(base, 0.35s)`。
        /// 三个常量**不是我们挑的**（`工具/read_literal.py` 现读，见文件头 ④）：
        /// 目标 alpha **1.0**（`0x1834b2bb8`）· 时长 **0.35 s**（`0x1834b31d4`，两跳共用）· 起始缩放系数
        /// **0.75**（`0x1834b2e84`）。
        /// <para>⚠️ **只在真机跑**（`Application.isPlaying`）：批处理/自检**没有帧循环** ⇒ 动画会永远停在
        /// `alpha = 0`（整扇看不见，而断言照旧读矩形 ⇒ **静默的假绿**）⇒ 非播放态**直接落到终态**。
        /// 这不是「我们简化了动画」，是「没有帧就没有动画」这条事实的显式处理。</para>
        /// <para>缓动 = DOTween `DOFade(float,float)` / `DOScale` 的**默认** `Ease.OutQuad` = `1−(1−k)²`；
        /// 时钟 = `Time.deltaTime`（DOTween 默认吃 `Time.timeScale` ⇒ 与原版同一条口径，⛔ 别换成 unscaled）。</para></summary>
        CanvasGroup _cg;
        Vector3 _baseScale = Vector3.one;
        float _animT;
        bool _animating;
        /// <summary>原版三个 DOTween 常量的落地值（见上一条 doc 的出处）。</summary>
        public const float AppearDuration = 0.35f, AppearFromScale = 0.75f, AppearToAlpha = 1f;

        /// <summary>自检读口：出场动画进度 0..1（不动画时恒 1）。</summary>
        public float AppearProgress { get { return _animating ? Mathf.Clamp01(_animT / AppearDuration) : 1f; } }

        /// <summary>`Open` 末两步：`alpha = 0` + `scale = base × 0.75`，然后交给 `LateUpdate` 每帧推进。</summary>
        void StartAppear()
        {
            if (_cg == null) return;                     // 根上没挂 CanvasGroup（不该发生）
            if (!Application.isPlaying) { ApplyAppear(1f); return; }   // 批处理/自检：没有帧（见 doc）
            _animT = 0f;
            _animating = true;
            ApplyAppear(0f);
        }

        /// <summary>把进度 `k`(0..1) 落到 alpha / scale（OutQuad 缓动）。`k >= 1` 时收尾并复位。</summary>
        void ApplyAppear(float k)
        {
            float e = 1f - (1f - k) * (1f - k);          // Ease.OutQuad
            if (_cg != null) _cg.alpha = Mathf.Lerp(0f, AppearToAlpha, e);
            if (_root != null) _root.localScale = _baseScale * Mathf.Lerp(AppearFromScale, 1f, e);
            if (k >= 1f)
            {
                _animating = false;
                if (_cg != null) _cg.alpha = AppearToAlpha;
                if (_root != null) _root.localScale = _baseScale;
            }
        }

        /// <summary>每帧推进一步（`LateUpdate` 调；没有动画时零开销）。</summary>
        void TickAppear()
        {
            if (!_animating) return;
            _animT += Time.deltaTime;
            ApplyAppear(Mathf.Clamp01(_animT / AppearDuration));
        }

        /// <summary>世界坐标落在**面板本体**上吗（**不含**那张铺满全屏的熄灯层）。
        /// <para>判据 = `Build` ③ 那层**吸收区**用的**同一个矩形** `BackgroundR`（⛔ 别在这儿另写一份；
        /// 那块吸收区是**世界档（壳侧）的口径**、范围取整块 `Background` 是**我们挑的**，见 `Build` ③ 的注释）。</para>
        /// <para>🔴 **2026-10-20（A3-e）就地订正（铁律 5）**：本段原写「原版 `BG` / `BGFrame` 几张 Image 的
        /// `m_RaycastTarget = 1` 把射线吃掉」—— **前提是错的**：那几张在 prefab 里全是 **rt=0**
        /// （`MB_4162` / `4691` / `5261` / `4301` / `4338`，arena1/2 同值）⇒ 原版**点面板空白处会落到压暗层 = 关窗**；
        /// 只有那 7 颗 TMP 与两颗 `Main Icon`（rt=1）吃射线。判据全文 → 文件头那一节。
        /// ⚠️ 本方法**按任务要求保持原样**（`BattleDriver` 那条手搓路本波不动）⇒ 它与原版有**已知偏离**：
        /// 「点窗内 ⇒ 什么都不做」比原版多保住了面板空白处。合并时随手动路一起删（配方见文件头）。</para>
        /// <para>用途：战斗侧 `BattleDriver.HandleAlliancePanel` 靠它分「点窗内（什么都不做）」与
        /// 「点窗外（= 原版那颗 `Close Background` ⇒ 关窗）」。⚠️ 那扇窗在 `Build` 里挂的
        /// `MenuDraw.ShadeHit` 上是个 `WindowButton`，**只有外壳那套 `PointerLayer` 会派发它**
        /// —— 战斗里不走那条路 ⇒ 关窗那一手由调用方接。</para>
        /// <para>🔴 **2026-10-16（`T09` 批 3）：本方法已按档分支**（⛔ 别只改一半 —— 那是「看得见的地方
        /// 点不到」那类静默错）：
        /// · **世界档**（`false`）= 与改本件之前**逐字相同**（`LayoutSpace.ToPixel` 折成绝对 px 再比 `BackgroundR`）；
        /// · **画布档**：面板已挂到子画布上（分辨率一变，世界 ↔ px 那条换算就**不是** `LayoutSpace` 那一套了）
        ///   ⇒ 走 **`RectTransform.InverseTransformPoint`**：把世界点投进**本面板自己的局部坐标**
        ///   （单位 = 设计 px），再与「`BackgroundR` 相对 `RootR` 的那个矩形」比。
        ///   🔴 **2026-10-16（`A14` · 锚点补正）**：那个局部坐标的**原点 = 枢轴**，而原版这根
        ///   `RootPivot = (0.13, 0.825)` **不是中心** ⇒ 先减 `RootCenterLocal`（= `(0.5−pivot)×sizeDelta`）
        ///   才是「相对面板中心」。⛔ 漏了这一项 = 命中区整体偏 `(+301.6, −154.5)` px（**静默**：
        ///   16:9 下也不对，只是「窗内点空 / 窗外点中」要正好落在边界附近才看得出来）。
        ///   判据 = 建件那一侧也是同一套（`SetRootAnchor` 的锚点语义 + 那三个 `Make*InParent` 的「相对父中心」）
        ///   ⇒ 量的就是**真正画出来的那块**。</para></summary>
        public bool HitBody(Vector3 world)
        {
            if (_ugui)
            {
                var rt = _root as RectTransform;
                if (rt == null) return false;                  // 没建起来 ⇒ 如实答「不中」
                var local = rt.InverseTransformPoint(world);   // 相对本面板**枢轴**、y **向上**、设计 px
                var cc = RootCenterLocal;                      // 🔴 枢轴 ≠ 中心 ⇒ 先挪到「相对面板中心」
                float lx = local.x - cc.x, ly = local.y - cc.y;
                float cx = BackgroundR.CX - RootR.CX;          // `BackgroundR` 相对面板中心（y 向下）
                float cy = BackgroundR.CY - RootR.CY;
                float hw = BackgroundR.W * 0.5f, hh = BackgroundR.H * 0.5f;
                return lx >= cx - hw && lx <= cx + hw
                    && ly >= -cy - hh && ly <= -cy + hh;
            }
            var p = LayoutSpace.ToPixel(world);
            return p.x >= BackgroundR.x1 && p.x <= BackgroundR.x2
                && p.y >= BackgroundR.y1 && p.y <= BackgroundR.y2;
        }

        void Apply(string playerName, string title, string allianceName,
                   string badgeId = null, int badgeTier = 0,
                   int playerScore = -1, int allianceScore = -1,
                   string avatarArt = null, string avatarBorderArt = null)
        {
            // ============================================================ 🆕 2026-10-29（`B95_MISC` · 本批①）
            //  原版 `AvatarDisplay.Initialize(PlayerAvatar, CosmeticItemAvatarBorder)` → `ChangeAvatar(item)`：
            //   · `AvatarDisplay__ChangeAvatar.c:34` `op_Equality(param_2, 0)` ⇒ **传 null 整段跳过**（`sprite` 不换、`enabled` 不动）
            //     ⇒ 我们这一档 = **不建/隐藏节点、不换图**（⛔ 「没数据就画 DefaultAvatar」是错口径）
            //   · `:53-70` ⇒ 有图才 `sprite = CosmeticItem.get_Image(item)` + `set_enabled(img, sprite != null)`
            //  头像框：`avatarBorderArt` 空 ⇒ **保持出厂那张** `Player_Profile_Border`（不改）。
            if (!string.IsNullOrEmpty(avatarBorderArt))
            {
                var bt = Art(avatarBorderArt);
                if (bt != null && _avatarBorderImg != null) _avatarBorderImg.SetTexture(bt);
            }
            if (string.IsNullOrEmpty(avatarArt))
            {
                if (_avatarImg != null) _avatarImg.gameObject.SetActive(false);   // = 原版 `param_2 == 0` 那支：不画
            }
            else
            {
                var at = CardArt.Cosmetics(avatarArt);
                if (at == null)
                {
                    Debug.LogWarning("[AlliancePanel] 头像立绘 `" + avatarArt + "` 取不到（`Resources/Art/avatars/`）"
                                   + " ⇒ 这一层**不画**（⛔ 不回落 DefaultAvatar；判据 `AvatarDisplay__ChangeAvatar.c:34`）");
                    if (_avatarImg != null) _avatarImg.gameObject.SetActive(false);
                }
                else
                {
                    // 节点**惰性建**（`Img` 的 `tex == null` 会直接返回 null ⇒ 不能拿 null 贴图建）
                    if (_avatarImg == null)
                        _avatarImg = Img(_root, at, AvatarImageR, RootR, "Image", QAvatar + 1, null, keepAspect: true);
                    else _avatarImg.SetTexture(at);
                    if (_avatarImg != null) _avatarImg.gameObject.SetActive(true);
                    else Debug.LogWarning("[AlliancePanel] 头像立绘节点建不出来（`Img` 返回 null）—— 如实说，不静默");
                }
            }
            if (_nameLb != null) _nameLb.SetText(playerName ?? "");
            bool hasTitle = !string.IsNullOrEmpty(title);
            if (_titleHolder != null) _titleHolder.gameObject.SetActive(hasTitle);
            if (_titleLb != null) _titleLb.SetText(title ?? "");

            bool hasAlliance = !string.IsNullOrEmpty(allianceName);
            if (_allianceGo != null) _allianceGo.gameObject.SetActive(hasAlliance);
            if (_allianceLb != null) _allianceLb.SetText(allianceName ?? "");
            if (_notInAlliance != null) _notInAlliance.gameObject.SetActive(!hasAlliance);
            // 🆕 2026-10-28（B88-B7 · 账 187③）：徽章两层换图 = 原版联盟支里那句
            //   `allianceBadgeDrawer.Draw(allianceBadge)` —— 先按 `tierLevel` 取档位框（`Max(0, tier-1)`），
            //   再取徽章图（id 查不到 ⇒ `AllianceBadgeArt` 走原版同一条 `DefaultTrophy` 回落）。
            //   ⚠️ 原版整段只在**联盟名非空**时执行（`BattleAlliancePanel__Open.c:93-108` 的 `if (bVar9)` 支）
            //   ⇒ 这里同样只在 `hasAlliance` 时换图（否则保持出厂那两张，反正 `_allianceGo` 已经关了）。
            if (hasAlliance)
            {
                if (_allyBadgeFrame != null)
                {
                    var ft = AllianceTrophyArt.GetTierFrame(badgeTier);
                    if (ft != null) _allyBadgeFrame.SetTexture(ft);
                }
                if (_allyBadge != null)
                {
                    var bt = AllianceBadgeArt.Get(badgeId);
                    if (bt != null) _allyBadge.SetTexture(bt);
                }
            }

            // 🔴 换过字就要**重新左对齐一次**：`Label.SetText` 会重排，而 `RefreshBounds` 把字形块
            //    **居中**到锚点上（见 `Battle/CardDisplayWindow.cs` 的 `PlaceLeft` 那条同族教训）。
            //    ⚠️ 本句**只在窗口已激活时**才有效（TMP 在未激活时量不出宽 ⇒ `AlignLeftOn` 会自己早退）
            //    ⇒ `Open` 先 `SetActive(true)` 再调 `Apply`，顺序是死的。
            // 🔴 **2026-10-16（`T09` 批 3）**：画布档**不需要**这一手、也**不许**调它 —— 那七颗字的
            //    `anchor` 是 **(0, 0.5)**（见 `Txt`）⇒ `RefreshBounds` 让**字形块左缘恒贴节点 x**
            //    （同一个函数里 `-anchor.x*W` 那一项），换字重排也自动贴住。⛔ 调 `MenuDraw.AlignLeft`
            //    反而会把节点整颗挪走（那个口喂的是**世界单位**）。
            // 🆕 **2026-10-28（`B90-N5` · 账 187④）**：两处评级的**数值** —— 原版 `Open` 里那两句
            //   `AllianceRatingDisplay__Initialize(playerRating 0x40, param_4 = playerScore, …, 0,1,1)` /
            //   `(allianceRating 0x58, param_8 = allianceScore, 0, 0,0,0,1)`（`BattleAlliancePanel__Open.c:78-81`）。
            //   `Initialize` 的取值分支（`decomp_full/AllianceRatingDisplay__Initialize.c`）：
            //   · `.c:57-59` 有活跃 LiveOps 活动 ⇒ 走「段位 + 档」的**格式化串**（远端；我们 13 个活动 SO 都没有 key=6 资产）；
            //   · `.c:60-66`（`LAB_1807ac910`）**否则 `text = score.ToString()`** ⇒ **本地恒走这一支**，
            //     且联盟那一格 `param_5 = 0` 直接跳到同一支（`.c:69`）⇒ 两格都是「int 转串」。
            //   ⚠️ 形参 **-1 = 我们不知道**（`BattleDriver` 今天那两处就是）⇒ 保留 prefab 的空态串 + 由
            //      `WarnRatingOnce` 出声一次，⛔ 不编数值。**`0` 是合法值**（原版会印 `"0"`）⇒ 别拿 0 当哨兵。
            //   ⚠️ 索引 0/1 = `Build` 里 `RatingDisplay(...)` 的**创建序**（先 `Player Rating Display`、后 `Alliance`），
            //      与 `RatingValueForTest(i)` 同一个口径。
            bool scoredPlayer = playerScore >= 0 && _ratingValues.Count > 0 && _ratingValues[0] != null;
            bool scoredAlliance = allianceScore >= 0 && _ratingValues.Count > 1 && _ratingValues[1] != null;
            if (scoredPlayer) _ratingValues[0].SetText(playerScore.ToString());
            if (scoredAlliance) _ratingValues[1].SetText(allianceScore.ToString());
            if (!_ugui && gameObject.activeInHierarchy && (scoredPlayer || scoredAlliance))
            {
                // 与下面那三颗同一个理由：`Label.SetText` 会重排，世界档的字墨是**居中**到锚点上的
                // ⇒ 换过字必须重新左对齐一次（画布档枢轴已是 (0,0.5)，⛔ 别调）。
                if (scoredPlayer)
                    MenuDraw.AlignLeft(_ratingValues[0],
                        new PxRect(PlayerRatingR.x1 + RatingIconW, PlayerRatingR.y1, PlayerRatingR.x2, PlayerRatingR.y2));
                if (scoredAlliance)
                    MenuDraw.AlignLeft(_ratingValues[1],
                        new PxRect(AllianceRatingR.x1 + RatingIconW, AllianceRatingR.y1, AllianceRatingR.x2, AllianceRatingR.y2));
            }

            if (!_ugui && gameObject.activeInHierarchy)
            {
                MenuDraw.AlignLeft(_nameLb, NameTextR);
                MenuDraw.AlignLeft(_titleLb, TitleTextR);
                MenuDraw.AlignLeft(_allianceLb, AllianceNameR);
            }
        }

        // ============================================================ 助手
        //  🔴 **2026-10-16（`T09` 批 3）：下面四个助手各有两个分支**（世界档 / 画布档），
        //     ⛔ **判据各只此一份** —— 面板里十几处建件全走它们，别在 `Build` 里各写各的。
        //  ⚠️ 形参里那个 `q` 一律 = **渲染队列档**（`QShade`…`QText`）；画布档由 <see cref="ZOf"/> 取负
        //     换成兄弟序的 z（理由 → 文件头那条「队列 → 兄弟序要取负」）。
        //  ⚠️ 那个 `parentR` = **本件父节点自己的矩形**（`PxRect`，绝对 px、左上原点）：画布档要拿它算
        //     「相对父中心」的偏移；世界档用不上（那边 `MenuDraw.Local` 自己会减父件的世界位置）。

        /// <summary>面板内**绝对 px 矩形 → 相对某个父矩形的中心的偏移**（**y 向下**，单位 = 设计 px）。
        /// 画布档所有子件的坐标都由它算出来 —— 与建件口成对（给图用 <see cref="HudCanvas.MakeImageInParent"/>，
        /// 它的 `yPxFromTop` 就是**向下**；给字要取负，`MakeText` 的 py 是**向上**）。
        /// <para>⚠️ **别用 `HudCanvas.MakeImage` / `QuadFromTopPx`**：那两个把 `x/y` 当**屏幕绝对 px**
        /// （锚点 = `x/1920`、`1 − y/1080`）—— 只有在**父件恰好是整屏矩形**时才等于「相对父中心」，
        /// 而本件的 `Background` / `BGFrame` / `NameHolder` 那几个容器**都不是** ⇒ 套错了会
        /// **静默摆到别处**（`T09` §五·补⑩ 那条；先例 = `MulliganPanel.Img` / `UnitChatPanel.Quad`）。</para></summary>
        static Vector2 LocalIn(PxRect r, PxRect parentR)
        {
            return new Vector2(r.CX - parentR.CX, r.CY - parentR.CY);
        }

        /// <summary>空节点（**两档同名同 rect**）。世界档 = `MenuDraw.Node`（`ApplyPxRect`，写世界单位）；
        /// 画布档 = 中心锚 + 设计 px + 登记兄弟序。原版每个容器节点都有自己的 rect，两档都要带上。</summary>
        Transform Node(Transform parent, PxRect parentR, string name, PxRect r, int q)
        {
            if (!_ugui) return MenuDraw.Node(parent, name, r);
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            var l = LocalIn(r, parentR);
            HudCanvas.SetPxSize(rt, r.W, r.H);            // ⛔ 不是 `MenuDraw.SetPxSize`（那个 ÷108）
            // 🔴 **用 `anchoredPosition`（⛔ 不是 `localPosition`）**：锚点/枢轴都是 `(0.5,0.5)`
            //    ⇒ 锚点落在**父件中心**、与**父件的 pivot 无关**（uGUI 自己算那项 `(0.5−父pivot)×父尺寸`）。
            //    `localPosition` 的原点是**父件枢轴** ⇒ 根那一级一改 pivot（`(0.13,0.825)`），
            //    直接挂在 `_root` 下的 `Background` / `NameHolder` / `TitleHolder` / `Alliance` 四块
            //    会**整体挪 301.6 × 154.5 px**（判据 → 报告 §①「连带必修②」与 §⑥·1；那正是本批差点漏掉的一处）。
            rt.anchoredPosition = new Vector2(l.x, -l.y);
            HudCanvas.Add(rt, ZOf(q));                    // 容器的档位决定**它整棵子树**落在哪一段
            return go.transform;
        }

        /// <summary>一张图（**两档同名同 rect 同染色**）。
        /// 世界档 = `MenuDraw.Rect`（`ImageQuad` + `renderQueue`）；画布档 = `HudCanvas.MakeImageInParent`
        /// （中心锚 + 设计 px + 兄弟序）。
        /// <para>⚠️ `keepAspect` 那一支**两档共用同一份判据** `CardbackFace.Fit`（原版 `m_PreserveAspect`：
        /// 先按 sprite 的 `m_Rect` 定框、再按 `padding` 内缩并挪位）—— 算式与 `MenuDraw.Rect` 里那一段
        /// **逐字一致**（`ccx = 中心 + dox` · `ccy = 中心 − doy`），⛔ 别自己再推一遍。</para></summary>
        HudPiece Img(Transform parent, Texture2D tex, PxRect r, PxRect parentR, string name, int q,
                     Color? tint = null, bool keepAspect = false)
        {
            if (tex == null) return null;                 // 取不到图 ⇒ 那一层不画（`Art()` 已记账 + 出声）
            if (!_ugui) return HudPiece.Of(MenuDraw.Rect(parent, tex, r, name, q, tint, keepAspect));

            float w = r.W, h = r.H, dx = 0f, dyUp = 0f;
            if (keepAspect) CardbackFace.Fit(tex, r.W, r.H, out w, out h, out dx, out dyUp);
            float ccx = r.CX + dx, ccy = r.CY - dyUp;     // `dyUp` 是 uGUI 口径（向上为正）⇒ 减
            var box = new PxRect(ccx - w * 0.5f, ccy - h * 0.5f, ccx + w * 0.5f, ccy + h * 0.5f);
            var l = LocalIn(box, parentR);
            return HudCanvas.MakeImageInParent(parent, tex, name, l.x, l.y, w, h, ZOf(q),
                                               tint.HasValue ? tint.Value : Color.white);
        }

        /// <summary>一段**左对齐**的文字（原版这几颗全是 `m_HorizontalAlignment = 1`=Left）。
        /// 字号 = `px`（**已乘过 localScale 的实绘 em**）；垂直档 = 原版 `m_VerticalAlignment`。
        /// <para>🔴 **两档的单位不同、⛔ 别混**：世界档 `px` 会被 `MenuDraw` 折成**世界单位**（÷108）；
        /// 画布档 `px` **就是设计 px**（canvas 单位 = 设计 px）。喂错 = 字小 108 倍、**不报警**。</para></summary>
        /// <remarks>🔴 **2026-10-09（A1126 · A2 档）**：本函数原来**连 `autoMinPx` 都没有**（= 固定字号），
        /// 而原版这七颗 **全是 `m_enableAutoSizing = 1`**。现在补上三格 —— 取值见上面
        /// `AutoMinPx` / `AutoMaxPx` / `AutoMaxNamePx` / `AutoBasePx` 那一段（**已乘 0.8**）。
        /// ⚠️ **折行 0**：原版这七颗 `m_TextWrappingMode = 0`，而 `SetAutoFitBox` 内部那句 `SetWrapWidth`
        /// 会**无条件**把它开成 `Normal` ⇒ 传 `wrapPx = r.W` 之后**紧接着**关掉（⚠️ 必须在 ①②③ 之前 ——
        /// `SetWrapping` 会推版面，排在对齐后面会把刚摆好的位置抹掉）。
        /// ⚠️ `wrapPx = r.W` 不只是为了折行：那道闸是
        /// `wrapPx &gt; 0 ∧ autoMinPx &gt; 0 ∧ fontPx &gt; autoMinPx` **三条全真**才调 `SetAutoFitBox`
        /// ⇒ **只传 `autoMinPx` 不传 `wrapPx` = 死实参**。
        /// <para>🆕 **2026-10-19（`R01`）**：补两个**仅评级那一格用**的可选实参 —— `wrap`（原版
        /// `Individual rating value` 是 **`折行=1`**，其余七颗仍是 0）与 `autoBasePx`（原版那一格
        /// `m_fontSizeBase = 31.379999` × 0.8 = **25.104**，不是本扇窗七颗共用的 `AutoBasePx`）。
        /// 默认值 = 改本段之前的行为 ⇒ 既有 7 个调用点**零变化**。</para></remarks>
        Label Txt(Transform parent, PxRect parentR, PxRect r, string text, Color col, string name,
                  float px, Label.VAlign v, float autoMaxPx = AutoMaxPx,
                  TmpFont.Face face = TmpFont.Face.Cjk, bool wrap = false,
                  float autoBasePx = AutoBasePx)
        {
            Label lb;
            if (_ugui)
            {
                // 🔴 **画布档那颗字的 `anchor` 是 (0, 0.5)、不是在 `MenuDraw` 那一档的 (0.5,0.5) + `AlignLeft`**：
                //    `RefreshBounds` 的算式是 `-anchor.x*W − b.min.x` ⇒ 锚点 x 取 0 时**字形块左缘恒贴节点 x**
                //    （换字重排也自动贴住，`Apply` 里那次补对齐因此**不需要** —— 见那里的分支）。
                //    ⇒ 节点 x 直接给**框左缘**（`r.x1` 相对父中心），y 给**框中心**（与 `anchor.y = 0.5` 配套）。
                //    ⛔ 先例 = `UnitChatPanel` 那三颗字（`new Vector2(0f, 0.5f)`）。
                var l = LocalIn(r, parentR);
                // 🔴 **`localPosition` 那一路要补一项**（⛔ 别删）：`Label.CreateCore` 的 uGUI 档写的是
                //    `go.transform.localPosition = pos`，而 `localPosition` 的原点是**父件【枢轴】**
                //    —— 我们的入参语义却是「相对父件**中心**」（`LocalIn` / `anchoredPosition` 那一套）。
                //    父件枢轴居中时两项同值（改本件之前就是这样）⇒ **只有 `_root` 这一个父件**（原版
                //    `pivot (0.13,0.825)`）需要加 `RootCenterLocal`。本扇窗直接挂在 `_root` 下的字**只有
                //    `NotInaAllianceText` 这一颗**；漏了它，那句提示会整块挪到面板左缘外 301.6 px、下移 154.5 px。
                //    ⚠️ 图与容器**不走这条路**（`MakeImageInParent` / `Node` 都用 `anchoredPosition`，与父 pivot 无关）
                //    —— 也正是**不能**靠 `_tmp.rectTransform` 之外的地方补的原因：一处漏、一处不错最难查。
                var fix = (parent == _root) ? RootCenterLocal : Vector2.zero;
                lb = HudCanvas.MakeText(parent, text, r.x1 - parentR.CX + fix.x, -l.y + fix.y, 5, col,
                                        new Vector2(0f, 0.5f), ZOf(QText), name, face);
                if (lb == null) return null;              // 拿不到 TMP 字体：`CreateUgui` 已出声（⛔ 不退化成点阵）
                // 字号：uGUI 档 = **设计 px**（`Label` 内部按 `PxPerUnit = 1` 算，见它那一节）
                lb.SetGlyphHeight(px);
                // 框：**设计 px** —— ⛔ 不走 `LayoutSpace.Px`（那个 ÷108 ⇒ 框变 1/108、字号收敛到荒谬的小值，不报错）。
                // ⚠️ 里面那句 `SetWrapWidth` 会**无条件**把折行开成 `Normal` ⇒ 下面紧接 `SetWrapping(false)` 还原。
                // ⚠️ min/max/base 三个实参**两档都是画布 px**（`Label.NominalPx()` 按档折算）⇒ 原样传。
                lb.SetAutoFitBox(r.W, r.H, AutoMinPx, autoMaxPx, autoBasePx);
            }
            else
            {
                lb = MenuDraw.Text(parent, r, text, col, name, px, QText, r.W, AutoMinPx, autoMaxPx, autoBasePx,
                                   face: face);
                if (lb == null) return null;
            }
            // ⓪ 折行档：本扇窗那七颗字是原版 `m_TextWrappingMode = 0`（`wrap = false`，默认）；
            //    🆕 2026-10-19（`R01`）两处评级的 `Individual rating value` 原版是 **`折行=1`** ⇒ 传 `wrap: true`。
            //    ⚠️ `SetAutoFitBox` 内部会**无条件**开成 `Normal` ⇒ 这里必须显式回写调用方那一档。
            lb.SetWrapping(wrap);               // ⓪ 原版折行档（见上）
            lb.SetAlignLeft();                  // ① 逐行左对齐（真折行时才看得见）
            // ② 垂直档（⚠️ 必须排在 ① 后面：`Left` 自带 V=Middle）。框高两个单位的换算是**两个口**
            //    （世界档走 `MenuDraw.SetVAlign` 折 ÷108；画布档直接给设计 px），⛔ 别互相套用。
            if (_ugui) lb.SetVAlign(v, r.H);
            else MenuDraw.SetVAlign(lb, v, r);
            // ③ 整块左缘贴框左缘（⚠️ 只调 ① 不够，见 `Apply` 那条注释）—— **只有世界档需要**：
            //    画布档靠上面那个 `anchor = (0, 0.5)` 表达同一件事（`AlignLeftOn` 喂的也是世界单位，⛔ 别调）。
            if (!_ugui) MenuDraw.AlignLeft(lb, r);
            return lb;
        }

        /// <summary>词条 → 文案。**远端 I2 表本地没有** ⇒ `Loc` 里没这一条时用 prefab 的静态原文
        /// （= 原版出厂就是那句英文）。⛔ **别把「查不到」写成猜测** —— 这里显式回落到原版原文。</summary>
        static string LocOr(string term, string prefabEn)
            => Loc.HasEntry(term) ? Loc.T(term) : prefabEn;

        /// <summary>取图 + 记账（`MenuDraw.Rect` 取不到图会**静默 return null** ⇒ 整层消失）。</summary>
        Texture2D Art(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            // 三级兜底 `ui/ → ui_menu/ → ui_deck/`（与 `Shell/AllianceEventScorePanel.cs` 同一条路）
            var t = CardArt.Ui(name) ?? CardArt.MenuUi(name) ?? CardArt.DeckUi(name);
            if (t == null && !_missArt.Contains(name))
            {
                _missArt.Add(name);
                Debug.LogWarning($"[AlliancePanel] 取不到美术 `{name}` ⇒ 那一层**不画**（⛔ 不静默："
                               + "缺的是哪张见本行；名单也记在 `MissingArt` 里）。");
            }
            return t;
        }

        // ============================================================ 🆕 2026-10-20（`T09` A3-e）只读探针
        //  ⚠️ 只在编辑器程序集里存在；播放器里一个都不编。全部**只读**（⛔ 没有一个会改状态）。
        //  判据 → 文件头那一节（原版 rt 值逐件写在 `BuildUguiClose` / `BlockRaycasts` / `RatingDisplay` 的 doc）。
#if UNITY_EDITOR
        /// <summary>压暗层的 `BattleHudButton` 挂上了吗（= 原版 `BackgroundCloseButton` 就位）。</summary>
        public bool UguiInputReadyForTest { get { return _closeBtn != null; } }
        /// <summary>压暗层上的点击组件（自检可直接 `ClickForTest()`）。</summary>
        public BattleHudButton CloseClickComponentForTest { get { return _closeBtn; } }
        /// <summary>压暗层的 `Image.raycastTarget`（原版 = 1）。</summary>
        public bool CloseRaycastTargetForTest { get { return RaycastTargetOf(_shade); } }
        /// <summary>压暗层的 `Image.raycastPadding`（原版 = `(0,0,0,0)`；取不到 ⇒ 返回 `(−1,−1,−1,−1)`）。</summary>
        public Vector4 CloseRaycastPaddingForTest
        {
            get
            {
                var img = ImageOf(_shade);
                return img != null ? img.raycastPadding : new Vector4(-1f, -1f, -1f, -1f);
            }
        }
        /// <summary>uGUI 压暗层被点了几次（`OnPointerClick` / `ClickForTest` 都算）。</summary>
        public int UguiCloseClickCountForTest { get { return _uguiCloseClicks; } }
        /// <summary>`ApplyPendingClose` 落了几次账（**灭自证**：点一次 = 1，⛔ 不是「点了就关」）。</summary>
        public int UguiCloseAppliedCountForTest { get { return _uguiCloseApplied; } }
        /// <summary>有没有挂起的「待关」（真机由 `LateUpdate` 落，批处理由调用方调 `ApplyPendingClose`）。</summary>
        public bool ClosePendingForTest { get { return _closePending; } }
        /// <summary>压暗层的 `HudPiece`（自检读 rect / 父链用）。</summary>
        public HudPiece ClosePieceForTest { get { return _shade; } }
        /// <summary>`BG` 的 `HudPiece`（原版 **rt=0** —— 自检断它确实是 false）。</summary>
        public HudPiece BackgroundPieceForTest { get { return _bg; } }
        /// <summary>原版 rt=1 的那 7 颗 TMP 的只读名单（顺序 = 建件序）。</summary>
        public int BlockingLabelCountForTest { get { return _blockLabels.Count; } }
        public Label BlockingLabelForTest(int i)
        {
            return (i >= 0 && i < _blockLabels.Count) ? _blockLabels[i] : null;
        }
        public bool BlockingLabelRaycastTargetForTest(int i)
        {
            return RaycastTargetOf(BlockingLabelForTest(i));
        }
        /// <summary>两处评级的 `Main Icon`（0 = `Player Rating Display`，1 = `Alliance Rating Display`）。</summary>
        public int RatingIconCountForTest { get { return _ratingIcons.Count; } }
        public HudPiece RatingIconForTest(int i) { return (i >= 0 && i < _ratingIcons.Count) ? _ratingIcons[i] : null; }
        /// <summary>两处评级的 `Individual rating value`（原版 **rt=0**）。</summary>
        public Label RatingValueForTest(int i) { return (i >= 0 && i < _ratingValues.Count) ? _ratingValues[i] : null; }

        /// <summary>一颗 uGUI 件的 `Image.raycastTarget`（世界空间件 / 没建 ⇒ false；⛔ 不抛）。</summary>
        public bool RaycastTargetOfForTest(HudPiece p) { return RaycastTargetOf(p); }
        /// <summary>一颗文字的 TMP `raycastTarget`（世界空间 TMP / 没建 ⇒ false）。</summary>
        public bool RaycastTargetOfForTest(Label l) { return RaycastTargetOf(l); }

        static UnityEngine.UI.Image ImageOf(HudPiece p)
        {
            if (p == null || p.Rect == null) return null;
            return p.Rect.GetComponentInChildren<UnityEngine.UI.Image>(true);
        }
        static bool RaycastTargetOf(HudPiece p)
        {
            var img = ImageOf(p);
            return img != null && img.raycastTarget;
        }
        static bool RaycastTargetOf(Label l)
        {
            if (l == null) return false;
            var g = l.GetComponentInChildren<UnityEngine.UI.Graphic>(true);
            return g != null && g.raycastTarget;
        }
#endif

        // 🆕 2026-10-28（B88-B7 · 账 187③）：`Alliance Badge Drawer` 的两层（原版 `AllianceBadgeDrawer`
        //   在 `Open` 的联盟支里灌图；我们建件时就画默认那一枚，`Apply` 再按 id 换）。
        //   ⚠️ 追加在**类尾**（铁律：新成员别插在已有成员之间 —— 会让下游账的落点整体漂）。
        HudPiece _allyBadgeFrame;              // `Frame`（`Alliance_Trophies_TIER1..4`）
        HudPiece _allyBadge;                   // `Badge`（`Alliance_Trophies_<阵营>`，见 `AllianceBadgeArt`）
    }

    // ============================================================ 入口（✅ 2026-10-18 · A985③ 已接）
    //  原版：HUD 那颗 `BattleHud.alliancePanelOpenButton` 点了 → `BattleAlliancePanel.Open(...)`。
    //  🔴 **那颗钮就是敌方名牌本身**（`BackCanvas/…/LeftArea/EnemyInfo`，13 场同构）——
    //     判据（rect · `m_RaycastPadding` 外扩 · 「它没有贴图」· 抬起沿）**全部写在**
    //     `Battle/BattleDriver.cs` 的 `BuildHudExtras` 建它那一段 + `HandleAlliancePanel`。
    //  ✅ 我们这边：钮建在 `BattleDriver.BuildHudExtras`、窗建在同一处（`AlliancePanelWindow.Create(root)`）、
    //     点击链 = `BattleDriver.HandleAlliancePanel`（模态，接在 `Update` 的 `HandleBattleLog` 之后）。
    //     名字那一格 = `BattleDriver.AlliancePanelName`（联机取对端真名 / 单机取名牌上那一串；
    //     ⛔ 不编造 · 称号与联盟名恒空 = **原版单机形态**）。
    //  ⚠️ **战斗里不走 `PointerLayer`** ⇒ 本窗 `Build` 里那两个 `MenuDraw` 命中节点在战场里**不会被派发**，
    //     所以「点窗外关窗」由 `BattleDriver` 那一侧接（它用本类的 `HitBody` 分窗内 / 窗外）。
    //  ✅ **2026-10-16（第四十二会话 · 收口波）：uGUI 档【已接线】—— 🔴 就地订正（铁律 5）**：
    //     本条原来写着「🔴 **待接线**（`T09` 批 3，`BattleDriver.cs` 不在本件白名单）…… 只需在
    //     建点（`Battle/BattleDriver.cs`）前面加两行」⇒ **那三件今天全办完了**，⛔ 别再照它接一次。
    //     ① **接线**（`G1` · 台账 `W50`）→ `Battle/BattleDriver.cs` **现核 `CardPresentation/Battle/BattleDriver.cs`**：
    //        `HudCanvas.Ensure(LayoutSpace.Cam)` → `EnsureRankedSubCanvas(AlliancePanelWindow.SubCanvasName,
    //        HudCanvas.SortingLayer.OverHand, 0)` → `AlliancePanelWindow.Create(root, uguiHost: allianceCv)`；
    //        `cv == null` 那一支 = **出声 + 本扇窗不建**（⛔ 不退回世界档 —— 与同批
    //        `MulliganPanel` / `CardDisplayWindow` / `MultiCardDisplay` 三处逐字同口径）。
    //        ⚠️ 原句里那个行号 `:15084` 是**当时的**建点，后来 `G1` 改过那份文件 ⇒ 行号已漂（现核见上）。
    //        ⚠️ 配方里那句 `HudCanvas.EnsureSubCanvas(null, …)` 也**已被 `G3` 取代**：现在统一走
    //        `BattleDriver` 的私有口 `EnsureRankedSubCanvas`（按名字幂等 + **建完读回来验一次**名字）。
    //     ② **`SubCanvasRank` 补一条** → `Battle/HudCanvas.cs` 的 `SubCanvasRank` 表里那条
    //        `"Alliance Panel Canvas"`（行尾注 `// 13/14`；**现核 ≈ `CardPresentation/Battle/HudCanvas.cs`** —— ⚠️ 那张表连同表头 doc
    //        本会话被 `G3` 改过，行号会漂，**认「`// 13/14` 那一行」最稳**）。
    //        那条行尾注还写着：将来迁 `DebugButtons`(7)/`PopupAnchor`(9)/`Tutorial`(11)/`LoadingMenu`(12)
    //        时**一律插在它前面**。
    //     ③ **断言两处改画布档**（`G2` · 台账 `W52`）→ `Editor/BattleScene.cs` **现核 `CardPresentation/Editor/BattleScene.cs`**（`left985`
    //        的左缘：画布档那颗字 `anchor = (0, 0.5)` ⇒ **不再减半个 `WorldW`**）与 **`CardPresentation/Editor/BattleScene.cs`**
    //        （`RW1192` 按 `lb.IsUgui` 取尺 —— 画布档不乘 `lossyScale × 108`）。另加了三条（子画布
    //        `overrideSorting`/层/order + **不带 `CanvasScaler`** + 逐件 `GetComponentInParent<Canvas>()`
    //        灭自证），**现核 `:21927-21980`**。
    //     ⚠️ **上面几个行号全是【现核】**，而且 `BattleScene.cs` / `HudCanvas.cs` / `BattleDriver.cs`
    //        本会话都还在被别的收口代理改（`G1`/`G2`/`G3`）⇒ 它们**还会漂**。
    //        🔴 找这几处时**按内容对**（「`left985` 那一句」「`RW1192` 那个按档 `if`」「`// 13/14` 那一行」
    //        「`EnsureRankedSubCanvas(AlliancePanelWindow.SubCanvasName` 那一句」），⛔ 别照号找。
}
