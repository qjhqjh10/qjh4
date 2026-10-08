# W_AlliancePanel 接线（A985③ 后半 + 素材腿）· 2026-10-18

## ① 那颗钮的原版判据（rect / 贴图 —— 逐字段给出处）· 建在哪

**结论：原版那颗钮【没有独立节点】，它就是敌方名牌 `EnemyInfo` 本身；而且它【没有贴图】。**

- **它是什么**：`bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4934.json`（= `BattleHud`）
  的字段 `"alliancePanelOpenButton" = {m_FileID 0, m_PathID 4686}`；`4686` = 挂在
  **`BackCanvas/Safe area BackCanvas/LeftArea/EnemyInfo`（GO 280）** 上的那颗 uGUI `Button`
  （`MonoBehaviour_4686.json`：`m_Interactable 1` · `m_Transition 0` · **`m_OnClick` 里 0 条** ——
  运行时挂：`BattleHud__Initialize.c` 用 `UIGenericEventCatcher.SourceDelegate` 绑 `BattleHud.AlliancePanelOpenButton`）。
  **13/13 战场同构**（13 份 `BattleHud` 的该字段逐场核过，都落在**自己那份** `EnemyInfo` 上）。
- **rect**（`RectTransform_2687.json`，13 场逐位相同）：`anchorMin=anchorMax=(0,1)` · `pivot=(0,1)` ·
  `m_AnchoredPosition=(50,−28)` · `m_SizeDelta=(260,75)`；父链
  `2687→2849(LeftArea)→3498(Safe area BackCanvas)→2684(BackCanvas)→2759(Canvas)`（中间三级 stretch+零偏移）
  ⇒ **绝对 x[50,310] · y[28,103]**（1920×1080 · 左上原点）。
  独立旁证：同链上 `ShowCemeteryBtn` 的 `ap (34.2,−140)` 反推出的中心 (84.2,168) 与我们**已验**的
  `_cemeteryBtn` 那几个数逐位吻合。
- **贴图 = 没有**（⚠️ 不是「查不到」）：`m_TargetGraphic` = `MonoBehaviour_4095.json`，它的 `m_Script`
  解析到 `bundle_Waprforge_monoscripts/MonoScript/MonoScript_-2844744054636863780.json` =
  **`UnityEngine.UI.Extensions.NonDrawingGraphic`**，而 `NonDrawingGraphic__OnPopulateMesh.c`
  **方法体是空的** ⇒ **一个像素都不画**（同族的 GO 名就叫 `UI Collider`）。⛔ 别去补图标。
- **命中区** = rect 按 `m_RaycastPadding = (−24.780000686645508, −18.450000762939453, −25.290000915527344, 0)`
  （UGUI 分量序 **L,B,R,T**、**负 = 往外扩**）⇒ **x[25.22, 335.29] · y[28, 121.45] = 310.07×93.45**，
  中心 **(180.255, 74.725)**。13 场这一格**逐位相同**。
- **显隐**：`BattleHud.Initialize` **没有**给它 `SetActive(false)`（与同族 `offensiveCardButton` /
  `resetCameraZoomButton` 那两颗**不同**）⇒ **开局就亮着**。
- **建在哪**：`Battle/BattleDriver.cs` 的 `BuildHudExtras`（`_offensiveBtn` 之后）；判据全文写在那一处。

## ① 三件接线逐件：改了哪（符号）· 名字传什么 + 依据

| # | 改哪 | 符号 |
|---|---|---|
| 1 | `Battle/BattleDriver.cs` 字段 | `ImageQuad _allianceBtn;` · `AlliancePanelWindow _alliancePanel;` |
| 1 | `Battle/BattleDriver.cs:BuildHudExtras` | 建那颗**全透明占位 quad**（`CardArt.Solid()` + `SetAspect(310.07/93.45)` + `SetTint((1,1,1,0))`，名字 `AlliancePanelOpenButton`）；⚠️ 等价物 = 原版那个 `NonDrawingGraphic`（同族先例 `Battle/ChatPopupPanel.cs` 的 `_close`，那里逐字写着同一句话：本工程点击一律自己算 `Contains`，`ImageQuad` 没有 `raycastTarget` 这个口子） |
| 1/2 | `Battle/BattleDriver.cs` 新块 | 常量 `AllianceBtnXPx/YPx/WPx/HPx` + `AllianceBtnPad{L,B,R,T}` · `public static PxRect AllianceButtonHitRectPx`（**算式只此一份**：建 quad / 真命中 / 自检三处都走它）· `AllianceButtonHit(Vector3)` |
| 2 | `Battle/BattleDriver.cs:BuildHudExtras` | `_alliancePanel = AlliancePanelWindow.Create(root);` = 原版 `FrontCanvas/Alliance Panel`（出厂关着，照 `Awake` 的 `SetActive(false)`） |
| 3 | `Battle/BattleDriver.cs` | `bool HandleAlliancePanel()`：**开着 ⇒ 模态接管**（`ReleasedThisFrame() && !HitBody(ptr)` ⇒ `Close()`）；关着 ⇒ 命中那颗钮 + 抬起 ⇒ `Open(AlliancePanelName, "", "")`。接到 `Update` 的 `HandleBattleLog()` 之后（两块命中区不重叠：日志钮 y[135.9,200.1] vs 名牌 y[28,121.45]）；自检口 `TickAllianceInputForTest()` |
| 3 | `Shell/AlliancePanelWindow.cs`（**小改**） | `public bool HitBody(Vector3)` —— 判据 = `Build` ③ 那层吸收区用的**同一个矩形** `BackgroundR`（⛔ 没另写一份）。为什么需要：本窗在战场里**不走 `PointerLayer`**，`Build` 里那两个 `MenuDraw.ShadeHit`/`Absorb` 上的 `WindowButton` **没人派发** ⇒ 关窗那一手必须由调用方接 |
| — | `Shell/AlliancePanelWindow.cs`（**改注释、铁律 5**） | 已知缺口 ⑤「HUD 上那颗钮我们还没有」与文件末「入口（⛔ 本笔没接）」两处**已被本次推翻** ⇒ 就地改成「✅ 2026-10-18 已接」+ 判据指针 |

**名字传什么**：新增 `public string AlliancePanelName`（**只此一份**，真实点击与自检都读它）：
`(_net != null && !IsNullOrEmpty(NetMatchmaking.FoeName)) ? NetMatchmaking.FoeName : FoePlateText`。
依据：① 原版那一格 = `BattleManager.matchData` 里**对手的名字**（`…AlliancePanelOpenButton.c` 取 `+0x10`）
⇒ 联机局取对端真名，**逐字等价**；② **单机局我们没有 bot 的名字数据源** ⇒ 取**名牌上印的那一串**
（`FoePlateText`，单机时 = 阵营名）= **玩家刚刚点下去的那块牌上写的同一个字符串**，⛔ **不编造**。
③ ⚠️ 谓词**与 `UpdateHud` 写 `_enemyText` 那一句同一个**（别简化成「只要 `_net != null`」）。
④ 称号 / 联盟名**恒空** ⇒ 称号组关 + `Alliance` 组关 + 「还没有加入任何联盟」那行开 = **原版单机形态**，⛔ 不给 bot 编。

**纠错（就地记）**：`W_AlliancePanel.md` §三 写「单机时 `_enemyText` 是空串」—— **不成立**：
`BattleDriver` 的 `UpdateHud` 里那句 `_enemyText.SetText(...)`（**现读 `:12609-12610`**，本笔插入之后的行号）
是 `(_net != null && FoeName 非空 ? FoeName + "   " : "") + CardText.Faction(_faction)`
⇒ 单机时它是**阵营名**（它引用的 `:12390` 那一行也已移位，现在是 `CountText(HandCountTerm,…)`）。

## ① 断言：名字 · 两态 · 🧨 改坏法 · 灭自证（宿主 `Editor/BattleScene.cs`）

落在整轮末尾、`DestroyImmediate(driver)` **之前**（那已是 `driver` 还活着的最后一处），共 **15 条** `Check`；
整段包在 `if (panel985 == null || panel985.NameLabel == null) { 出声 } else { … }` 里（照 A964 那节的写法：
前提不成立 ⇒ 整体跳过，⛔ 不 NRE 把整轮打挂、也⛔ 不假绿）：

- **前提**：钮 + 窗 + `NameLabel` 都建起来了。
- **几何（4 条）**：命中矩形 = `x[25.22,335.29] y[28,121.45]`（🧨 把负 padding 当「往里缩」⇒ `x[74.78,284.71] y[46.45,84.55]`）·
  **画出来的 == 命中矩形**（🧨 漏 `SetAspect` ⇒ 变成 93.45² 正方形）· 染色 **α=0**（🧨 去掉 ⇒ 名牌上多一块白板）·
  中心 = `(180.255, 74.725)px`。
- **灭自证（有界）**：中心必中 ∧ **正上方 60px 打不中**（命中半高才 46.73px）⇒ 🧨 命中写成恒真就红。
- **点击链（走真实输入闸 `TickAllianceInputForTest`，⛔ 不直调 `Open`）**：点名牌 ⇒ 开（🧨 把 `HandleAlliancePanel` 从
  `Update` 链里摘掉）· 点**窗外** ⇒ 关（🧨 删掉关窗那一段）· 点**窗内** ⇒ **不关**（🧨 把 `!HitBody(...)` 的 `!` 去掉）。
- **两态**：`Open(n,"","")` ⇒ 无联盟行开 / `Alliance` 组关 / `Title:` 组关 / 名字 = 传入串；
  `Open(n,"t","a")` ⇒ 三条**反过来**。
- **灭自证 · 名字左缘**：断**落点** `|PxX(左缘) − 276.220| < 0.75`（⛔ 不是断「`AlignLeft` 被调过」）；
  276.220 = **原版值**（`GameObject/Name Text.json` 的 `RectTransform_2779`（ap 235.724 · size 348.760×51 ·
  localScale 0.8）+ `NameHolder`（VLG 第 0 格）+ 父链现算），**不是我们的常量**；
  🧨 删掉 `Apply` 里 `if (gameObject.activeInHierarchy)` 那段前置对齐 ⇒ 左缘偏左约半个名字宽。
- **灭自证 · 结构上不可能同时满足**：`NotInAllianceNode.activeSelf != AllianceGroupNode.activeSelf`（恒一开一关）。

## ② 素材：源 → 目的地 · Resources 里的名字 · 谁引用

- **源**：`d:/2/Warpforge_tools/data/ui_extract/boosterpacks_assets_all/Sprite/40K_shop_offer_bg_Sororitas_0.png`
  （md5 `fa2e49341963fe9f1db5704b20195efc`，208,291 B；`find` 全库**只有这一份** ⇒ 没有「同名不同图」的坑）。
- **怎么补的**：**走本仓现成的导入路** —— 往 `工具/import_original_art.py` 的 `MENU_IMAGES` 加**一条**
  `('40K_shop_offer_bg_Sororitas_0', 'boosterpacks_assets_all')`，再跑
  `python -I 工具/import_original_art.py --only-menu`（✅ 763/763）。**只加了这一张**，没顺手导别的。
  （⚠️ 必须登记：`Resources/Art/**` 在 `.gitignore`，不登记的话下次一跑导入器就没了 —— A808 那条教训。）
- **目的地 / 名字**：`Unity/MyGame/Assets/CardPresentation/Resources/Art/ui_menu/40K_shop_offer_bg_Sororitas_0.png`
  （落盘名**原样**，不需要 `' '→'_'`）。取法 = **`CardArt.MenuUi("40K_shop_offer_bg_Sororitas_0")**
  → `Resources/Art/ui_menu/<名>` ⇒ `_alliancePanel` 的 `BG` 那一层现在**能取到**（不再 `MissingArt` / 不再告警）。
- **谁引用它**：`Shell/AlliancePanelWindow.cs:208`（`BG` 内容底）· `Shell/RankedRewardEventWindow.cs:171`
  （`ArtCardBg`，阵营卡底）· `Shell/ShopData.cs` 的「商品主图」槽（`Editor/ShopScene.cs:4369` 那条
  「图取不到」告警说的就是它）⇒ **一处补上，三处一起通**。
- ⚠️ **没有 `.meta`**：该目录 292 张 PNG / 291 份 `.meta`，缺的那一份就是新加的这张 —— 与**本仓既有做法一致**
  （`import_original_art.py` 不写菜单图的 `.meta`；`Tutorial_Background.png` 等也是上次开 Unity 时才生成的）
  ⇒ **下次跑 Unity 批处理时会自动生成并导入**，不用手写。

## 类型检查 · 行尾 · 没查清

- **秒级类型检查**：`TMPDIR=/tmp/wf_ap2 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0 ✅**
  （中途红过一轮 12 条，全是**我自己**新写断言里的 3 处转义 / 1 处漏逗号，已当场修完再跑绿）。
  **本笔一律没跑 Unity**（白名单禁）⇒ 上面那 17 条断言**尚未被执行过**。
- **行尾**：`git diff --numstat` —— `BattleDriver.cs` **629/83**（13550 行，CRLF 13550/13550）·
  `BattleScene.cs` **791/17**（18667 行，CRLF 18667/18667）· `工具/import_original_art.py` **6/0**（CRLF）·
  `Shell/AlliancePanelWindow.cs` **LF**（0 CRLF / 385 LF，未跟踪）。**没有一处整篇翻行尾**。
  （⚠️ 前两个 numstat 含**本笔之前**就存在的未提交改动，不是本笔全部。）
- **没查清（3 条，都不猜）**：
  ① `RankedRewardEventWindow.cs:169-171` 的注释仍写着「工程里没有 / 本仓 `Resources/` 下**一张都没有**」
     —— **现在已过期**（这张图已进 `Resources/`）。⛔ 该文件**不在本笔白名单**（`Shell/` 下别的），**没动**，
     如实报上来请调度台落盘 / 另行派活。
  ② 原版 `Close Background` 之外，**面板本体哪些像素会「吃」掉射线**没逐块核（`BGFrame` 四块的
     `m_RaycastTarget` 没读）⇒ 我们的吸收范围取**整块 `Background`**（`AlliancePanelWindow.Build` ③ 早就
     标着「我们挑的」）；本笔的 `HitBody` 照这条既有口径，**没有改它**。
  ③ 面板出场动画（`DOFade` + `DOScale`）与三个 DOTween 常量仍未读 —— 属**窗那一半的既有缺口**，本笔没碰。
