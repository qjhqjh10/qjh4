# R · 卡片详情大图三问（2026-10-18 现核）

## 〇、前提更正（先纠一条）

原版**没有**名为 `Card info Popup` / `CardDisplayWindow` 的 prefab。全量搜过 `assets_full/*/GameObject`
（含 `bundle_menus_assets_all` 16768 个），关键词 0 命中（仅 `Card Info` 这个另一件小 prefab）。
菜单详情窗的真身是**场景对象**：`bundle_scenes_scenes_mainmenuwarpforge/GameObject/Card Displayer Menu For Menu.json`。
层级：`Card Display > Cards > CardUI > 2DCard > Front > CardImage / CardFrame`。

## 一、三问各一句结论

1. **立绘超出卡框：原版有，我们没有。** 原版卡图 rect `2.7484×2.7484`（687×687px @scale250）大于卡框
   `2.2452×3.2572`（561×814px），且不是被卡框矩形裁；卡框上挂 `SoftMaskScript`（`FlipAlphaMask=1`）
   用立绘 alpha 给卡框挖洞 ⇒ 角色/剑/闪电可盖在框的金属上。我们 `UseFrontLayer=false`，卡框整块画在立绘上，
   角色无法盖到框上；差在**遮罩/层序**，不是尺寸。
2. **创建副本/升级两块面板：原版有，我们也有 UI，但机制没接。** 记忆里的「已停掉」是 2026-09-27 旧口径；
   2026-10-12 用户说「请你恢复」后两块已恢复。当前：创建副本钮可点但只 `Debug.Log`；升级钮 `Interactable=false`。
3. **异画切换：原版有，我们也有。** 原版 `Alternate Art Panel` 左右 `Select Art Button`，无 AA 时整块
   `SetActive(false)`；我们 `BuildAltArt` 已建左右圆钮，`p.gameObject.SetActive(has)`，数据表 7 张 AA。

## 二、Q1 证据

### 原版（场景 JSON 逐字段现读）
- `CardImage`：`RectTransform_1480.json` size `2.7484×2.7484`、`m_AnchoredPosition.y=-0.044`、father=RT1168(`Front`)。
  Image=`MonoBehaviour_2343.json`：`m_Type=0(Simple)`、`m_PreserveAspect=0`、`m_Maskable=0`。
- `CardFrame`：`RectTransform_1511.json` size `2.2452×3.2572`、y=`-0.030`、father=RT1168。
  Image=`MonoBehaviour_2368.json`：`UseSpriteMesh=1`、`m_Maskable=1`。
- 裁剪关系：`MonoBehaviour_2339.json` 是挂在 CardFrame 上的 `SoftMaskScript`：`MaskArea=1480`（=卡图 RT）、
  `FlipAlphaMask=1`、`DontClipMaskScalingRect=1`、`CutOff=0`、序列化 `m_Enabled=0`。运行时 `SoftMaskScript.OnEnable`
  会 `ToggleMask(true)`；`Initialize` 还会在父节点补一个 mask 组件（类型符号未解析，见「没查清」）。
- 卡体：RT1323(`2DCard`) `2.0927×3.3313`；RT1168(`Front`) `2.0927×3.4913`。**卡上没有 RectMask2D**：
  全场景唯一一颗在 `GameObject/Background Mask.json`（`MonoBehaviour_1975.json`，padding 61.5/0/-46.41/0）。
- 立绘 alpha 是**角色抠图**：本机 `art_ash1.png`（Anvirr）671×1024，84.4% 像素 alpha=0、仅 6.5% 近不透明；
  卡框 PNG 的窗内透明、金属不透明（`frame_saimhann_tier4.png` 抽样验证）。所以 `FlipAlphaMask=1` 的语义就是
  「框在角色处透明、角色从框下透出」。
- 逐卡 opt-in：`MonoBehaviour_2447.json` = `CardImageController`，`bigSizeMultiplier=1.27`、`frameMask=2339`；
  `CardImageController__ChangeState.c` 状态 1 时缩放 ×1.27 并启用 frameMask；
  `CardDisplayWindow__CardOverdrawHandle.c` 读卡 `+0x58 useOverDraw`，**只对前台 index 0** 生效。
- 结论：立绘层 > 卡框（宽度 1.31×）、不被卡框矩形裁；最终可见范围由「卡体矩形收边 + 卡框 alpha 像素级挖洞」
  共同决定，角色可越出框的金属轮廓，但不越出卡体矩形。

### 我们侧（文件:行号）
- 详情窗：`Shell/CardDetailPopup.cs:621` `CardView.Create(node, cd, …)`（无 `clip`）、`:632` `SetFace(Full)`。
  战斗窗：`Battle/CardDisplayWindow.cs:1092` `CardView.Create(…, use: ZoomWindow)`、`:1107` `SetFace(Full)`；
  战斗窗无这三块面板（`options=null`，文件头 `:16-20`）。
- `Core/CardView.cs:2031` `CardArt.Frame(d.faction,d.rarity,!d.isUnit)`；`:2040` `ArtMeshInFrame`；
  `:2041-2047` 注释与实现 = 立绘在框下、由框 alpha 透出，**出厂不越出**；`:2081` `_art` 用 opaque 材质（忽略 alpha）；
  `:2097` 只有 `UseFrontLayer` 才建 `_artFront`（z=-0.008，真 alpha 盖在框上）；`:5811` `public static bool UseFrontLayer = false`。
- 立绘板尺寸：`:377` `ArtUnitSquare=2.7484`、`ArtUnitY=-0.044`；`:5463-5505` `ArtMeshInFrame` 在越界时
  **显式裁到卡本体** `Width=2.0927, Height=3.3313`，UV 跟着裁。
- 结论：**没有实现「立绘超出卡框」**。裁到卡体这一步与原版一致；缺的是「卡框被立绘 alpha 挖洞/前景抠图层」。
  `_artFront` 是现成近似件，但被 `UseFrontLayer=false` 关掉。
- 另：`useOverDraw` 数据其实有（`RuleEngine/Data/CardDatabase.cs:155-157`；`cards_engine.json` 641 张=1 / 486 张=0），
  但 `CardData` 没这个字段（`CardView.cs:29-105`），`BattleDriver.ToCardData`（`:14113-14137`）也没带；×1.27 未实现。

## 三、Q2 证据 + 两块面板的作用

### 3.1 原版作用（机制，不是版式）
- **创建这张牌的副本**：消耗**同阵营同稀有度 1 个通配符（Wildcard）**，`owned +1`（拥有数无上限），云脚本 900。
  `CardServices__CheckForCardCrafting.c`：先 `RawCardScript.CanCraft`（服务端配置），再按 army+rarity 找通配符且数量>0；
  `CraftingPanel__CheckButtons.c`：拿到空价 ⇒ 整块 `SetActive(false)`；有价但没通配符 ⇒ 钮 `interactable=false`。
  `CraftingPanel__TryCrafting.c`：传奇且 `owned==1` 先弹确认窗；`TryCraftingInternal.c` 调 `CardServices.CraftCard`。
  影响：收藏与可用副本 +1；卡组上限不变。
- **将此张牌升级至等级 N**：消耗**多余副本 `requiredCopies`**（实拍 4）+ **一种货币 `Cost.Amount`**（实拍 2000，金币），
  成功后**内部 tier +1（CardTier{Tier1=0..Tier4=3}，最高 Tier4）**并给 **`pointReward` 阵营锻造点**（实拍 +500）
  与 `xpReward`。判据：`UpgradePanel__CheckUpgradeRequirements.c`（`GetNextTierInfo(t,r)=GetTierInfo(t+1,r)`；
  条件 `owned >= maxInDeck + requiredCopies` 且货币够；Title 写 `tier+2`）· `UpgradePanel__TryUpgrade.c` ·
  `卡牌重复与升级_原版规格.md:30-45`。**升级只改美术**（卡框/高亮框/SDF 软光/matcap），攻血甲费用效果一处不改。
- 等级是什么：**就是卡框档位 CardTier**；显示等级 = 内部值 + 1。「升级至等级 3」= 当前内部 Tier2。
- **显隐**：Crafting 卡不可合成 ⇒ 整块关；Upgrade 有下一档 ⇒ `Content` 开，满级 ⇒ `Content` 关 + `No Upgrade Warning` 开。
  三块只在菜单版出现（战斗版 `options=null`）。
- 版式真值：Crafting `1294,146.71→1744,410.44`；Upgrade `1294,411.12→1744,672.74`；Alt `1294,673.42→1744,903.01`。
  词条：`MenuDeck/Craft/CraftDescription` · `CraftCost` · `Upgrade`（`to level {0}`）· `UpgradeDescription`（`You will obtain: +{0}`）·
  `UpgradeFull`（「Maximum card tier reached」）。图标：按钮底 `UI_Button_Mulligan`；副本数 `40k_general_icon_card_amount`；
  货币 `40k_topmarquee_currency_gold`；通配符 `40k_general_wildcard_{rarity}_small`（默认 epic）。

### 3.2 我们的现状与「记忆」出处
- **没有 `资料/记忆/` 目录**。这条记忆的真实出处是 `Unity/资料/五张参考图_复刻现状_0928.md:24`：
  「2026-09-27 用户拍板停掉」，同一行已有 2026-10-12 订正「用户当天裁定『请你恢复』⇒ 两块面板已恢复（A1699）」。
- 代码现状（留着并已恢复 UI）：`CardDetailPopup.cs:560-565` 调 `BuildCrafting/BuildUpgrade/BuildAltArt`；
  `BuildCrafting :848-916`、`BuildUpgrade :919-1015`；`Craftable :810-817`（common/rare/epic/legendary 可合成，special 不建）；
  `DoCraft :1575`（只 `AddOwned +1`，不扣通配符，未接线）；`DoUpgrade :1591`（检查 `CanUpgrade`、`SetLevel +1`、
  `RebuildKeepingState`）；`OnCraftClicked :1543`（只出声）；升级钮 `:1009` `upWb.Interactable=false`。
- 可复用性：**全部可复用**。`CardProgress`（`NeedCopies/NeedGold/Points :80-84`、`Level/SetLevel :145-146`、
  `Spares/CanUpgrade :149-158`）仍在；缺的是通配符库存/货币/存档与「真扣」口径。

## 四、Q3 异画 + 升级→卡框

### 4.1 异画
- 原版：`AlternateArtPanel__Initialize.c:22-38`：卡为空或 `HasAlternativeArtStyles(card)==false` ⇒ 整块 `SetActive(false)`。
  节点：左右 `Select Art Button Left/Right`（`74.4×75.6`，pos `-177.1/171.9,65.0`）、`Current Style`（`406×101.5`）、
  `Buy Original Card Button`（`328.71×73.38`）。
- 数据：`cards_raw.json` 里 `alternateArts` 非空的正好 **7 张**：`AM2 Ursula Creed / BL34 Abaddon / SAU35 Imotekh /
  GOF28 Ghazghkull / SW1 Logan Grimnar / UM76 Titus / DA1 Azrael`；每张 1 种风格（`style=5 HammerAndBolter`）。
- 我们：`CardDetailPopup.cs:1106 BuildAltArt`、`:1132-1133` 建左右钮、`:1292 BuildAltArrow`、`:1284 SetActive(has)`、
  `:1064 HasAltArtStyle`、`:1086 AltStylesOf`。结论：**有切换口**。

### 4.2 升级 → 卡框档位：**能做**
- 原版已验证：`UpgradePanel.ChangeFrameOnLevelUp.c` → `BasicCardUI.RefreshCardVisualEffects(cardUI, newTier)` →
  `CardTierUIController.SetTier.c` → `CardFramesSO.GetClanFrame(army, tier, cardType)`；
  `BackToOriginalFrame.c` 用 `tier-1` 回退（升级动画）。即「升级档就是卡框档」。
- 我们的 tier 来源：`CardArt.TierOf(rarity,faction)` **无条件 `return MaxTier`**（`CardArt.cs:251-269`、`MaxTier=4`）；
  `Frame :59-69` 与 `Sdf :96-115` 都调它；`CardView.cs:2031/2062` 调用时**没有卡 id / 没有 tier 参数**。
  `:267` 注释已写明可换成 `Mathf.Clamp(CardProgress.Level(id),1,MaxTier)`。
- 数据侧：`cards_raw.json` / `cards_engine.json` 里按 `tier/level/upgrade` 搜 **0 命中**
  （原版把 tier 存在玩家数据 `"CardTier"`，不在卡 SO）。我们 `CardProgress._lvl` 是纯内存静态字典、**无存档**，
  缺省 `MaxLevel`（`CardDetailPopup.cs:145`）。
- 美术：4 档卡框本地全有（`Resources/Art/cards` 下 `frame_*_tier1..4.png`，每档 60 张）。
- 缺的：每题一个**等级存档字段**、把 tier 从 `CardProgress` 传到 `CardArt.Frame/Sdf`、把升级钮打开、
  缺省等级从 4 改成 1（否则永远满级）。**数据/美术不是阻塞项**。

## 五、最小补齐清单

1. **Q1（立绘超出卡框）**：`Core/CardView.cs` 的 `UseFrontLayer` 改为**按卡**生效（卡面是抠图 ⇒ 用 `_artFront`），
   或给卡框做 alpha 挖洞；立绘 rect 照 `2.7484² @ y=-0.044`，卡框 `2.2452×3.2572 @ y=-0.030`，
   收边到卡体 `2.0927×3.3313`。若做 ×1.27：`useOverDraw=1` 且只前台槽 0；给 `CardData` 加 `useOverDraw`
   并在 `BattleDriver.ToCardData` 从 `CardDto.useOverDraw` 填。
2. **Q2**：创建副本若要真做：`BuildCrafting:914` 的 lambda 改回 `DoCraft`，并补通配符库存（当前条恒 99）；
   升级若要可点：删 `CardDetailPopup.cs:1009` 的 `Interactable=false`，`DoUpgrade` 已有「多余副本够不够」判据。
   版式无需再改（rect 已在 `CraftT/UpgT/AltT` 常量，`CardDetailPopup.cs:210-215`）。
3. **Q3 卡框档**：`CardArt.cs` 加 `Frame(faction,rarity,tactic,int tier)` / `Sdf(... ,int tier)`
   （或给 `CardData` 加 `tier`），`CardView.cs:2031/2062` 传 `CardProgress.Level(d.artId)`；
   `CardProgress` 等级改为可存档、缺省 1；保留 `TierOf` 的 MaxTier 给不传 tier 的调用点。

## 六、没查清

1. `SoftMaskScript.Initialize` 在父节点补的 mask 组件类型（RectMask2D 还是 Mask）：符号未解析。
2. SoftMask shader 的方法体未导出（只读到属性 `_AlphaMask/_FlipAlphaMask/_NoOuterClip`），翻转方向的像素级语义按字段名 + 参考图判。
3. `Cost.Currency` 具体资产（服务端 LiveOps）；`CardTierConfig` 除实拍「4/2000/+500」外的其它档数值。
4. `CanCraft` 的服务端配置实际覆盖哪些 special/defence 卡（我们 `Craftable` 只放四档稀有度，是等价替身）。
5. 原版 `useOverDraw` 的服务器真值分布（我们 `cards_engine.json` 有 641/1127=1，但那是本地导出值）。
