# A817 · 锻造页「青色八边形徽 + 金框狼徽」—— Unity 腿 dump 结果

> 2026-10-16 · 主对话执行（用户拍板「你走 Unity 腿 dump」）· 原版游戏 + SceneJumpShot mod。
> 产物目录：`资料/原版实拍/menu_forge_1016/`（树 dump ×4 · 子树 ×2 · 图集 ×2 · 真渲 PNG ×4 · 搜捕表 ×1）。
> mod 改动：`d:/2/Warpforge_tools/scenejumpshot/SceneJumpShot.cs` 新增一档 `menu_open`（备份 `SceneJumpShot_pre1016_A817.cs.bak`）·
> 配置 `d:/2/unity_run_ref/UserData/sjs_dump_cfg.txt` 四行 = `scenes_scenes_mainmenuwarpforge.bundle / 25 / D:/4/Unity/资料/原版实拍/menu_forge_1016 / menu_open`。

---

## ① 结论（两条，一好一坏）

### ✅ 金框狼徽 —— **认出来了**
**`Content Area/Tabs/Forge Tab/Selected Army Info/Army Icon`**
- **rect = 125.28 × 125.28**（子件 dump 实读）· 与它同排的还有 `ArmyText`（文本 `Ultramarines`，本机默认阵营）· `LevelText`（`Level 1/50`）· `Xp Points Icon`（52.96²，**出厂 `activeSelf=False`**）· `TotalXp Points`（`154748`）· `Help Icon`（52.19²，`40K_generic_bt_info`）。
- **sprite = `40k_DeckSelection_icon_Faction<阵营>`**（本机 = `…_FactionUM`），**金/银框是画在图里的**（已开图核：`D:\2\Warpforge_tools\data\ui_extract\armyicons_assets_all\Sprite\40k_DeckSelection_icon_FactionUM.png` —— 圆环 + 阵营徽一体）⇒ **用户那张的「金框狼徽」= 同一颗节点、只是他那局阵营是太空野狼**。
- 复现证据：`menu_forge_tab_subtree.tsv` 第 41–46 行 · 渲染图 `shot_menu_forge_forced.png`（左上角那颗带翼徽 + `Ultramarines / Level 1/50`）。

### ⚠️ 青色八边形「0」—— **本机原版复现不出来（判据仍缺）**
- 全场景搜捕（`menu_hunt_badge.tsv`，**82 条候选**：贴图名含 octagon 的 Image + 短数字 TMP(宽<160) + rect 45~75 的 Image，**含所有未激活节点**）⇒ **只有卡牌的 `Level Up Effect` 用 `OctagonUI Filled/Border SDF`**，奖励窗/锻造页那一片**一个都没有**。
- 我渲出来的同位置（按相对几何对齐：柱子右缘 28.0% vs 28.3%）是**纯黑**，用户那张那里有「金圈上 + 青色八边形 0 下」的一竖列。
- **它叠在背景机械柱之上**（不是柱子贴图自带），且**不在 `menus_assets_all` 那份 `Rewards Base Submenu Variant` 的任何子件里** ⇒ 与**第 4 页签同族**（运行时按 LiveOps/服务端数据装配），**离线拿不到判据**。
- ⇒ 按铁律 2/3 **如实标「未识别」**；⛔ 别据此发明一个节点。

---

## ② 这趟腿是怎么跑通的（可复用）

| 步 | 做法 | 关键坑 |
|---|---|---|
| 1 | mod 加 `menu_open` 档：`FindInactiveByName("Main Menu Navigation Button - Rewards")` → 取 `Il2Cpp.OpenWindowButton` → 反射 `OpenWindow()` | 🔴 **`go.GetComponents<UnityEngine.Component>()` 拿到的包装器 `GetType()` 只报基类 `Component`** ⇒ 按名字匹配永远失配（第一版白跑一趟）。必须 `GetComponent<Il2Cpp.OpenWindowButton>()` **按具体类型取** |
| 2 | A 路（反射开窗）**抛异常**：`OpenWindow()` 内部依赖服务端载荷 | 结果：窗没开出来 |
| 3 | **B 路（成功）**：`_bundles` 里 `LoadAsset<GameObject>("32024b169f2f0844288f6f547d8c459b")`（**容器键 = GUID**）→ `Instantiate` 到 `3 - PopUp Holder` | 🔴 `Addressables.LoadAssetAsync(key)` 在本机**取不到**（`Result = null`）；**bundle 兜底那条路成** ⇒ 与「`LoadAsset<T>(名字)` 对本 build 恒 null，要按容器键」那条老结论一致 |
| 4 | tab 点击：`Forge Button` 上的 `Toggle.isOn = true` **不足以切页**（我们手搓的实例没接上切换器）⇒ **手工 `SetActive`** 把 `Forge Tab` 打开、兄弟关掉 | ⛔ 这只为「把版面渲出来比」，**不代表原版显示条件** |
| 5 | 子树 dump（`DumpSubtree`）：path/name/activeSelf/activeInHierarchy/**rect(数字)**/pos/sprite/color/text | 🔴 `t as RectTransform` 在 Il2Cpp 包装器上**恒 null** ⇒ rect 全变 `?`；要 `TryCast<RectTransform>()`（第一版白跑一趟） |

---

## ③ 产物清单（`资料/原版实拍/menu_forge_1016/`）

| 文件 | 是什么 |
|---|---|
| `runtime_ui_dump_menu_open.tsv`（141 KB） | 主菜单**静态**树（569 行 / 5 根）—— 与 0927 那份同构，可当基线 |
| `runtime_ui_dump_menu_open_b.tsv`（328 KB） | **B 路 instantiate 之后**的整树（奖励窗进来了） |
| `runtime_ui_dump_menu_forge_forced.tsv`（328 KB） | 再强制激活 Forge Tab 之后 |
| `menu_forge_tab_subtree.tsv`（60 行） | **锻造页那一支**（含 rect 数字 + sprite 名）—— A817 的正判据 |
| `menu_tab_buttons_subtree.tsv`（33 行） | `Tab Buttons` 那一列：`MissionsRewardsButton / CampaignRewardsButton / Forge Button / Menu Navigation Panel Button`，**四个都有 `Badge Highlight`（35²，sprite `40K_notification_number` = 红圆，已开图核）+ `OneText`**；母版那颗 `OneText` 出厂 `active=True` |
| `menu_open_images.tsv`（83 KB） | 全场景带 sprite 的 Image 表（含 rect/pos） |
| `menu_hunt_badge.tsv`（18.7 KB） | 定向搜捕的 82 条候选 |
| `shot_menu_open.png` · `shot_menu_open_b.png` · `shot_menu_forge.png` · `shot_menu_forge_forced.png` | 真渲图（最后一张 = 锻造页版面） |
| `menu_open_btn_components.tsv` | 那颗入口钮的组件字段（⚠️ 字段是包装器内部字段 —— 见 ② 的坑） |

---

## ④ 顺手查到 / 待用

1. **第 4 页签在 prefab 里确实不存在**：`Tab Buttons` 的四个子件 = `MissionsRewardsButton`（布道所）· `CampaignRewardsButton`（活动）· `Forge Button`（锻造厂）· `Menu Navigation Panel Button`（**母版**，Icon `40K_shop_bt_boosters`，Label `Booster Packs`）—— **与 R5 的结论逐条吻合**，**A815 的「自建第 4 页签」口径得到第二条独立佐证**。
2. **tab 按钮几何**：每个 165×180；`Tab Buttons` 整列 165×1009.06；`Badge Highlight` 在按钮左缘（世界 x 四颗相同）。
3. **`Selected Army Info`** 整块 rect = 621.29×122.72；`Ready for level up` = 194.42×302.76（带 700×700 的 `Glow UI W40K`）。
4. **锻造页背景 = `Background Elements`**：左右各一根 358.63×1396.94 的机械柱（`40k_rewards_forge_decoration_*`）+ 蜡烛 + `Decoration Top`。
5. ⚠️ **本机跑的是离线态**：`Forge Army Selector` 的 `Army Content` **宽 0**（没数据）· `TotalXp Points` 显示 154748（本地默认档）· 交付窗里「太空野狼」那种数据依赖项**本机看不到**。

---

## ⑤ 还欠 / 下一步

- **青色八边形**：三条路 —— ① 请用户补一张**更清楚/悬停出 tooltip** 的实拍（最省）② 等「真 Play 时连上服务器」那一趟（后端可达时再看）③ 就此如实标「未识别」。⛔ **别派写手凭空造**。
- mod 那条 `menu_open` 档**留着**（以后要 dump 任何「运行时才装出来的窗」都用它，步骤见 ②）。
