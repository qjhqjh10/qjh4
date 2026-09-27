# `Image.m_PreserveAspect` 普查（五块并派）— 结论与已修清单

> **起因**：2026-09-27 用户报「顶栏那面盾画宽了」—— 原版那一格 `m_PreserveAspect = 1`、我们按拉伸画。
> 那**不是孤例**，于是同一天立了这条普查（`项目任务.md` §〇 A ①），**五路并行**把五个界面块盘了一遍。
> **本文是结论正本。** 已完成的一律只留索引（铁律 6）。
>
> **尺子**：`python d:/4/Unity/工具/menu_dump.py <bundle> "<根窗名>" --depth N --md`
> —— `describe()` 在 `m_PreserveAspect` 为真时于「颜色贴图模式」列打 `preserveAspect`，**grep 即可**。
> 🔴 **不要另写 JSON 解析器**（一条规则只写一处）。**工具本身没改**。

---

## 〇、两条判据（**先读这两条，否则会把「等价」当「差异」**）

### 1. 🔴 `Image.m_Type = Sliced` 时 **PA 完全不生效** —— 反编译实证

`Image.GenerateSlicedSprite(VertexHelper toFill)` 的签名里**没有** preserveAspect
（`d:/2/tools/il2cpp_out/dump.cs:923286`），而 `GenerateSimpleSprite(VertexHelper vh, bool lPreserveAspect)` **有**（`:923280`）。
⇒ **「Sliced + PA=1」的行一律等价于拉伸**，我们拉伸**没有错**。
（本批里被这条救回来的：`40K_button` 一族 · 设置窗 `Popup BG`/`Toggle` · `Campaign Reward Window` 的两处
`Glow` · 卡面 `Buy Original Card Button` 的九宫 `UI_Button_Mulligan`。）

### 2. ⚠️ **待复核**：Unity 的 PA 是「内接后按 `rectTransform.pivot` 偏移」，我们一律居中

Unity 走 `Image.GetDrawingDimensions(bool shouldPreserveAspect)`（`dump.cs:923241`），收窄后按 pivot 偏移；
`MenuDraw.Rect:68-73` 是**恒居中**。**只从签名 + 通行 uGUI 行为推的，实现体没读到**（dump 只有签名）。
**实测影响 ≤3px**（左栏 `Icon` pivot (0.5,0.7) 差 3px · 登录卡 image (0.5,0) 差 2px · 弹窗 `Army Icon` (1,0.5) 差 0）。
**没到要改的程度，但别把它当已证实。**

> 另：**我方接口不是一套**
> —— 菜单侧是显式 `keepAspect`（`MenuDraw.Rect:64` · `MenuWindowBase.Rect:172` · `MainMenuRuntime.Rect:73` · `SettingsWindow.Rect:719` · `DeckRuntime.Img:1911`）；
> **战斗侧没有这个形参** —— `ImageQuad.Create` 里 `_aspect = tex.width/tex.height`（`Core/ImageQuad.cs:50`）
> ⇒ **战斗 HUD 每一张图等效 `keepAspect = true`**（按高给宽），要拉伸只能显式 `SetAspect(...)`。
> **这是战斗侧反过来出错的原因**（见 §一 · 牌堆底板 / 骷髅底条 / 换牌底条）。

---

## 一、五块结果

| 块 | 判定 | 真缺陷（肉眼看得见） | 反向差 |
|---|---|---|---|
| **战斗 HUD** | 🔴 **最脏的一块**（也是此前**零 PA 底子**的一块） | 见下面独立表 | **3 处**（原版 PA=0、我们等比） |
| **日常 + 设置** | 🟡 4 处 | 每日奖励窗左轨两枚图标（**2.13× / 1.40×**）· 奖格里程碑（1.29×，**我们内部还不一致**）· 设置页签（13%） | 未发现 |
| **档案馆 + 排行榜** | ✅ 排行榜 4 棵**全对**；档案馆 8 处 | 万能卡计数图标 4 枚（**同一件在 4 个窗口都漏传**）· 卡组编辑 Footer 图（1.25×） | **零** |
| **主菜单** | 🟢 基本干净 | `Upper bar/SettingsBtn`（**1.42×**）· `InboxBtn`（6.5%）· 卡片详情窗万能卡 4 枚（同 §档案馆那族） | **零** |
| **商店 + 锻造厂 + 战役** | ✅ **基本干净** | 无（`Counter`/价格钮/四处阵营徽记/格内图标/左栏页签**全部已对齐**） | 1 处（`ForgeTab.cs:289`，**框方图方 ⇒ 看不见**） |

### 战斗 HUD 明细（这一块此前**没有 PA 底子**，是本轮信息最少、查出最多的一块）

**A. 原版 PA=1 而我们按拉伸（等效等比，但框给错）**

| 件 | 节点 | 原版 | 我方 | 差 |
|---|---|---|---|---|
| 牌堆底板 | `RightArea/{Player,Enemy}Deck/DeckAndEnergyImage` | PA=1 · 图 `UI_Deck_Background` **364×346**（1.0520）塞 230×229.85 | 按高给 230 | **宽 +11.96 / 高 +11.4px（+5.2%）** |

> ⚠️ 这一条**不是「少传参数」** —— 战斗侧恒等比，但**没做「内接裁剪」**（只给高、宽可以超出原版框）。属结构性差异。

**B. 🔴 原版 PA=0（硬拉伸）而我们等比 —— 三处，两处非常显眼**

| 件 | 节点 | 原版 | 我方 | 差 |
|---|---|---|---|---|
| 结算·骷髅底条 | `EndBattlePanel/AllRewardsHolder/SkullsHolder` | **PA=0** · Simple · `40k_main_bt_nametag` **109×41** 拉满 **648.1×52.4** | 等比 ⇒ 139.3×52.4 | 🔴 **窄 508.8 px（只剩 21%）** |
| 换牌·完成底条 | `Mulligan/ButtonsGroup/MulliganContinueButton/Button` | **PA=0** · Simple · `40k_bt_underbutton` **485×83** 拉满 **577.5×63.84** | 等比 ⇒ 372.8×63.84 | 🔴 **窄 204.7 px** |
| 攻击选择器三钮 / 黄圈 / 能量水晶 / 牌堆张数底板 | — | PA=0 | 等比 | ⚠️ 全部 **<2.4px**，可忽略 |

> 两处大的**已修**（见 §二）。另：`ReplayBar` 四钮 · `BattleLogPanel` 行底 · `UnitChatPanel` 气泡/波形
> —— 这三处**我方本来就显式 `SetAspect(...)` 对齐了 PA=0**，做得对，**别再动**。
> `LeftArea/CemeteryLogPanel/Frame/{Left,Right}` —— 原版 PA=1（图 98×601 / 66×608）**长度取不到 571.24/578.65**，
> 我们用的是面板锚高 654.5。**这是常数取错件，不是 PA 语义问题**，未修（见 §三）。

---

## 二、已修（2026-09-27 当天；每条都留了断言/实据在代码注释里）
| # | 件 | 改法 | 出处 |
|---|---|---|---|
| 1 | 顶栏那面盾 | （**本轮之前**已修） | — |
| 2 | 结算·骷髅底条 | `EndPanel.cs` 加 `SetAspect(648.1/52.4)` | RT 2616 / GO 391 / MB 4938，直读 `PA=0`、`m_Type=0` |
| 3 | 换牌·完成底条 | `MulliganPanel.cs` 加 `SetAspect(BarW/BarH)`（**照 `CardChoicePanel:161` 现成写法**） | RT 2659 / GO 121 / MB 5292 |
| 4 | 万能卡计数图标 **×4 个窗口** | `CollectionWindow.cs:318` · `CardDetailPopup.cs:602` · `DeckRuntime.cs`（`Img(..., keepAspect:true)`） | 原版 30×44 框 + 42×51 图 ⇒ 实绘 30×36.4 |
| 5 | 卡组编辑 Footer 图 | `DeckRuntime.cs` `foot_ic` 传 `true` | 64×64 塞 50×40 ⇒ 原版 40×40 |
| 6 | 卡组格 `Game Mode Icon` / `DificultyLevel` | `MenuDraw.cs` 两处 `false → true` | 只在 `DeckSelectionPopup` 露（收藏窗不传这两个参数） |
| 7 | 每日奖励窗左轨三件 + 奖格里程碑 + 锁 | `DailyRewardPopup.cs` 5 处传 `true` | Premium 图标 **2.13×**（与顶栏盾同一类错） |
| 8 | 设置窗页签图标 | `SettingsWindow.cs`（先给 `Rect` 加 `keepAspect` 形参） | 122×104 塞 141.41×106.82 |
| 9 | 主菜单齿轮 / 收件箱 | `MainMenuRuntime.cs` 两处传 `true` | RT1560·MB2526 / RT1561·MB2529 |

---

### 📌 修完之后抓到的一件事：**自检在替错值背书**（跨会话教训）

`DeckScene.Run` 第一次复跑挂了 **5 条断言**，而它们**期望的正是错值**：

| 断言原文 | 期望 | 实得（= 修好的原版值） |
|---|---|---|
| `foot_ic` 的 rect = 权威值 | 50×40（拉伸） | **40×40** ✓ |
| 第 1 个图标 | 30×**44** | **30×36**（原版 36.4）✓ |
| 第 2/3/4 个图标 | 30×**44** | **30×37**（原版 37.3）✓ |

⇒ 这几条断言当初是**照着我们自己画错的样子写下来的**，所以画错时它们**全绿**。
**已把期望值改成从原版算出来的值**（`30 × 51/42` 与 `30 × 51/41`，`DeckScene.cs`），
并在注释里写明出处。**这就是「自检绿 ≠ 口径对」的又一个实例**（同 `资料/已知的坑.md` 那条）。

> 另：`EndPanel` 的骷髅底板与 `MulliganPanel` 的底条**此前一条断言都没有** ⇒ 已补两条
> **量渲染尺寸**的断言（不是量框 —— 框一直是对的，错的是往里画多大），并给两个面板加了
> `SkullPlateWorldW/H`、`BarWorldW/H` 访问器。

## 三、还开着的（**未修**，都写明为什么）

1. ~~战斗侧 `ImageQuad` 没有「框 + 内接」入口~~ ✅ **2026-09-27 已修**：
   新增 `ImageQuad.FitHeight(boxW, boxH, sprAspect)`（= uGUI `Image.GetDrawingDimensions` 那条：
   **图比框宽 ⇒ 按宽定，否则按高定**），牌堆底板改用它。
   验收：我方 **230.00×218.63**、敌方 **200.00×190.11**（原版值），原来 +5.2% 的溢出没了。
2. ~~日志面板 `Frame Left/Right` 的高度常量取错件~~ ✅ **2026-09-27 已修，而且【四条全错】**：
   竖条取**面板锚高 654.5**（多 14%）、横条取**面板全宽 794.1**（多 12%）、y 一律用 `panelCy`（与四条各自中心差 13~55px）。
   已逐值照原版重摆：Left **571.24** · Right **578.65** · Top **707.30** · Bottom **707.30**，各自带回自己的中心。
   验收：**571.2 / 578.7** 与 **708.0 / 706.6**。四条边框此前**一条断言都没有** ⇒ 已补 2 条。
3. ~~`CardDetailPopup` 的 `Craft/WC icon` 取图与原版不同~~ ✅ **2026-09-27 已修** ——**性质从「待裁决」改成「缺陷」**。
   原版那颗叫 **`WC icon`**，`menu_dump` 实读 `…/Crafting Panel/…/Craft/WC icon` = `40k_general_wildcard_epic_small`
   （41×51 · Simple + preserveAspect）⇒ 实绘 40.7×50.66；同格那行字是 `This will consume a wildcard`，
   而 `CraftingPanel` 上有 **`wildcardIcon` / `wildcardSprites`** 两个字段 ⇒ **运行时按卡的稀有度换图**
   （prefab 里存的 `_epic_small` 只是默认值）。我们原来画 `40k_main_collection_icon`（卡池/收藏图标）
   —— 与「消耗一张万能卡」完全不搭。
   **修法**：四档稀有度 → `40k_general_wildcard_{common,rare,epic,legendary}_small`；**判据只此一处** =
   `CardDetailPopup.WildcardIcons` / `WildcardIconFor`（通配符计数条与「创建副本」共用）。
   🔴 **口径更正（用户 2026-09-27 指出）**：**稀有度就是四档** —— 卡池里那个 `special`
   （**防御卡 39 + 战术卡 11**，`cards_engine.json` 实测）**不是第五档**，是「这张卡没有稀有度」被源数据塞的**占位值**。
   那 50 张运行时画哪张**关服查不到** ⇒ 按**原版 prefab 的原值** `_epic_small` 画（不猜），自检里如实报。
   已配 **4 条断言**（`CollectionScene`，那个窗的**第一条**自检：四档各拿一张真卡开窗 → 查 `Craft Icon` 贴图）。
4. **原版 PA=1 而我们根本没画的**（缺件，不是 PA 差）：收藏 Cards 页 `Army Icon`（80×80）·
   骷髅卡页内 `body`/`image` · 周常卡 `body.image` · 周常里程碑 `CheckMark` ·
   设置窗 General/Account/Support 三页（我们只建 Graphics/Audio/Online）· 商店 `Card Drawer` 整棵。
5. **`LiveOpsEventWindow.cs:343` 的 `keepAspect=true` 是「我们挑的」**（原版那处 `sprite=0` 无图）—— **已知有意，别改**。

---

## 四、查不到的（**不猜**）

- **`UI Extensions/*` 三张 shader 不在 `assets_full` 里**（见战场线那条 ⑥ 的记录）。
- 主菜单 `Card Display/Cards/CardUI*/Ban Icon` ×4 —— 原版 PA=1、图 512²，但**框算不出**
  （卡面子树 218 个 RT 在导出里**父链断**，工具走不到）。
- 设置窗 6 个页签 `Icon` 的 **pid 取不到**（同尺寸 6 个候选，分不出哪一份；但尺寸一致）。
- 收藏 `Card Filters` / `Deck Filters` 的 `Army Filter` 各格 `Background` —— 原版 `<无图>`（运行期赋图）。
- `Campaign Points Drawer/Content/Campaign Glow` 落到屏上的**实际框**（drawer 缩放系数没查到）。
- `Daily Streak Popup` 的奖格**不在窗预制体树里**（运行期实例化），已单独滚 `Daily Streak Reward Popup Entry`。

---

## 五、📌 一条口径更正（`项目任务.md` §〇 A ① 引的那个数）

原文写「**主菜单一个场景 289 个 Image 里 72 个是 PA=1**」—— **复现不出来**。
本块实测：`bundle_scenes_scenes_mainmenuwarpforge` 里带 `m_PreserveAspect` 的 Image 组件 **208 个 / PA=1 共 50 个**
（= 主菜单树 41 + 场景内 2 个 prefab 根 2 + 卡面子树 7）。
**错因**：`解包整理/07_场景/mainmenuwarpforge/` 那份是**双份导出**（416 = 2×208 / 100 = 2×50）⇒ 289/72 是拿错口径数出来的。

> 🔴 **复现命令必须带 `--root-size 1920x1080`** —— `Game UI` 的 RT 是 a(0,0)-(0,0)/sd(0,0) 的 **Canvas 本体**，
> 不加的话整棵树全算成 0×0（这个坑记在 `资料/已知的坑.md`）。
