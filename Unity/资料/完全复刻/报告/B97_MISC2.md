# B97_MISC2 —— `未分类_其他.tsv` 的 19 条 B（第九十七会话 · 2026-10-29）

> 执行代理 `B97_MISC2`。派活判据 = `判据/B96_菜单未分类44_分诊.md` §三（19 条）。
> 结论：**12 条 A / 4 条仍 B**（另有 7 条开工时**已经是 A**，本代理逐条复核后确认成立）。

---

## 〇、条数对账（先看这条）

| 口径 | 数 | 明细 |
|---|---|---|
| 派活简报 | 19 | `B96_菜单未分类44_分诊.md` §三 那 19 行 |
| **开工时现读** | **12** | 分诊 §4·1 那 7 条「可推 A」**已经被前面批次改成 A 了**（`:178` `:179` `:204` `:273` `:396` `:708` `:756`，逐条现读 `状态=A` 确认） |
| 本轮改判为 A | **8** | `:99` `:220` `:321` `:322` `:348` `:398` `:399` `:667` |
| 本轮仍 B | **4** | `:295`（宿主归别的写手）· `:312`（等一句裁定）· `:430`（缺服务端活动数据 + 两扇窗已有手工等价族）· `:584`（缺口重新定位 = 战斗 HUD 的 UI 件未建） |

---

## 一、逐条结论（19 条一条不落）

> 「证据」列给的是**本轮现读**的坐标；「账」列是补丁里怎么写的。

| 账行 | 原版 类.方法 | 本轮档 | 证据（本轮现读） | 账 |
|---|---|---|---|---|
| `:99` | `BoardCriteria.DeepCopy()` | **A** | 三源同结论：`decomp_full` 的 `*__DeepCopy.c` 共 **11 个**、全树 `grep -li DeepCopy` 只命中这 11 个定义；`out_cpp2il_dev_ilspy` 的 `.DeepCopy(` 共 **11 处**且全在家族内（`AbilityLogic.cs:199/207/267` · `ActiveAbility.cs:116/119/133` · `BoardCriteria.cs:57` · `CardAbility.cs:356/363/394` · `HandEffect.cs:84`）；`out_il2csharp` 同。⇒ 家族外 0 调用 = 死代码 ⇒ 深/浅拷不可观测 ⇒ 铁律 11 例外① | B→A |
| `:178` | `ConfigProvider.OnDestroy()` | A（开工即 A） | 复核成立：`Shell/ConfigProvider.cs:156` | 不动 |
| `:179` | `ConfigProvider.UnpackInternal(...)` | A（开工即 A） | 复核成立：`Shell/ConfigProvider.cs:135`（JsonUtility 顶 Newtonsoft 已写进 `差异`） | 不动 |
| `:204` | `Countdown.Activate(float,int)` | A（开工即 A） | 复核成立：`Shell/TimerDisplay.cs:504` | 不动 |
| `:220` | `CursorByArmySO.GetCursor(...)` | **A** | **「19 张图未 import」已经不成立**：`Resources/Art/ui_menu/` 现读 **21 个 `Cursor_*.png`**（全 128×128，逐个读 PNG 头），且**逐字节对比 = 21/21 与 `assets_full/.../armycursors/Texture2D/` 的源图 md5 全等**；加载是 `CardArt.MenuUi` → `Resources.Load<Texture2D>`（`Core/CardArt.cs:890`）⇒ 已有那两张 meta 的 `textureType: 0 / spriteMode: 0` 正是对的，新图由 Unity 首次打开自动 import | B→A |
| `:273` | `Enchantments.TryGetCard(T, out …)` | A（开工即 A） | 复核成立（与 `:756` 同一条，两行都已是 A） | 不动 |
| `:295` | `FactionScore.Initialize(CardArmy,int,int)` | **B（未动）** | 组件本体齐（`Shell/FactionScore.cs:172/103`，断言 F 段）。真缺 = 宿主接线，而两个宿主 `Shell/RankedTab.cs`（`Top4` Big×4 + `AllFactions` Small×4）与 `Shell/RankedEventWindow.cs`（footer Small×1）**都不在本代理白名单**（`RankedTab.cs` 此刻归 `B97_W2` 在飞）⇒ 台账保持 B；可粘贴补丁见 §四·1 | 不动（避免与 W2 同键） |
| `:312` | `GameObjectUniqueIdentifier.FindComponent(string)` | **B（未动）** | **0 消费者**：全仓 `FindComponent("CardCollectionReferenceCardPointer")` 只出现在断言 `Editor/B94B18_Assert.cs:74/137` 的「已知缺」清单；矩形已用常量复刻并**在跑**（`Shell/CollectionWindow.cs:5118` + `CheckFocusedArmy:5128-5129` + `Editor/CollectionScene.cs:5886`）。两条路二选一，需调度台一句话（前两轮都被「⛔ 别为凑 A 建空节点」挡住）⇒ 补丁见 §四·2 | 不动（只加证据） |
| `:321` | `GenericArmyItem.GetItem()` | **A** | 方法体逐句等价（`.c`）+ 断言 I3–I5 已有；**0 生产消费点**（`new GenericArmyItem(` 全仓只 1 处 = 断言 `B93S3Assert.cs:472`）。原版消费点 = `GenericArmyItemDrawer`（LiveOps 商店 drawer）= **另一个类** ⇒ 不是本行的缺口 | B→A |
| `:322` | `GenericArmyItem.GetItem(CardArmy)` | **A** | 同上（`ItemDrawer.cs:2339`；断言 I1/I2；表由调用方喂 = 原版同形状） | B→A |
| `:348` | `HealCriteria.DeepCopy()` | **A** | 与 `:99` 同一条证据链（同一家族）；`AiHealCriteriaDto` 全仓 0 写点仍成立 ⇒ 与 `:99` 同批同档 | B→A |
| `:396` | `ListExtensions.SpliceWhere<T>(…)` | A（开工即 A） | 复核成立：`Shell/OfferContainer.cs:1097-1105` 内联等价 | 不动 |
| `:398` | `ListExtensions.GetMax<T,TU>(…)` | **A** | 🔴 **上一条备注说 B96_MENU「新建了 `Shell/ListExtensions.cs`」—— 现读该文件根本不存在**（`grep -rn "class ListExtensions" Assets` = 0）。本批**真建**：`CardPresentation/Shell/ListExtensions.cs`，语义按 `.c`（`Append`+`Aggregate` 无种子；同分取**后者**，判据 = `…DisplayClass6_0…GetMax_b__0.c` 的 `if (0 < iVar2) param_3 = param_2;`）；断言 A1–A4。0 本地调用方（原版 3 处调用点本地均无等价件） | B→A |
| `:399` | `ListExtensions.GetMin<T,TU>(…)` | **A** | 同一新文件；方向按 `…DisplayClass7_0_object__long____GetMin_b__0.c`（`if (iVar2 < 0) param_3 = param_2;` ⇒ 同分取后者）；断言 B1–B4（含 `TU = long`） | B→A |
| `:430` | `MilestoneScoreBar.Initialize(int, GenericScoringMilestone[], Sprite)` | **B（未动）** | 两扇宿主 `Shell/SkirmishEventWindow.cs`（`BuildScoreBar:501` / `BuildMilestones:536`）与 `Shell/EnergySinglePlayerOnlyEventWindow.cs:1179` **都是手工建的等价族**（几何/字号逐值照 prefab），`MilestoneScoreBar` 组件**零实例化**；而 `GenericScoringMilestone[]` 本地**没有数据源**（唯一来源 = LiveOps 事件服务端；`Shell/LiveOpsLocalCatalog.cs` 无 scoringMilestones）⇒ 接上只能拿合成数据跑，等于把已验收的静态几何重写一遍。**要调度台定口径** | 只加证据 |
| `:584` | `SimpleRotation.Update()` | **B（缺口重新定位）** | 按 `m_Script` pathID `-6857224376968460714` 现读**全库 42 实例**：13 场 × 3（`speed` 全 `(0,0,100)`）+ 主菜单 `Cog` 1 + `menus_assets_all` 的 `Cog` 2（`-20`）。🔴 **那 39 个不是 3D 场景物体**：`battlearena1` 的 `Highlight_396/180` = `RectTransform` + `CanvasRenderer` + Image(`MonoBehaviour_4108`) + SimpleRotation ⇒ **战斗场景里的 UI 图**；我们的战场 prefab（`Resources/ArenaPrefabs/battlearena1.prefab`，306 个命名节点，只有网格/粒子）**不含**这些 UI 件 ⇒ 缺口 = **HUD 那颗会自转的 `Highlight` 图本身没建**，不是「给战场 prefab 挂驱动物」。数学那一半已落地并有 L1–L4b | 只加证据（改方向） |
| `:667` | `UICornersGradient.ModifyMesh(VertexHelper)` | **A** | 6 个实例全是**四顶点 uGUI Image**（`m_Script` 全 = `8733114785328696827`；`bundle_menus_assets_all` 5 + `mainmenuwarpforge` 1），字段四角逐实例现读（例 `MonoBehaviour_1941` TL(0.066,0.027,0.003)；`-2135209279831517756` TL(0.868,0.274,0.176)=队伍 A 红；`-2930347684145025530` TL(0.176,0.541,0.867)=队伍 B 蓝）。**四顶点上 `ModifyMesh` 的双线性退化成「每个顶点取自己那个角」** ⇒ 与 `ImageQuad.SetCornerColors(bl,br,tr,tl)`（`Battle/ImageQuad.cs:517`）**数学恒等**；实例侧 `Editor/MainMenuScene.cs:18032-18038` 已按 MB 实读值断言（±0.002）。残差：6 个实例的父链 prefab 名未解出（父 pathID 跨 CAB）⇒ 6↔我们挂载点未逐个对号 | B→A |
| `:708` | `UIResourceCounter.SetQuantity(int,bool)` | A（开工即 A） | 复核成立：`Shell/TopBar.cs:567/682/616/1052` | 不动 |
| `:756` | `Enchantments.TryGetCard<T>(T, out …)` | A（开工即 A） | 复核成立（与 `:273` 同一条方法，两行都 A） | 不动 |

---

## 二、代码改动（本代理只碰了自己新建的两个文件）

| 文件 | 动作 | 说明 |
|---|---|---|
| `CardPresentation/Shell/ListExtensions.cs` | **新建**（≈120 行含注释） | `GetMax<T,TU>` / `GetMin<T,TU>`，`.c` 逐句；签名**不带 `this`**（原版 `ListExtensions.cs:2527/2603`） |
| `CardPresentation/Editor/B97_MISC2_Assert.cs` | **新建**（200 行） | A `GetMax` 4 条 · B `GetMin` 4 条 · C 指针图 5 条 · D `DeepCopy` 家族 2 条 = **15 条断言** |
| `账/_parts/B97_MISC2.tsv` | **新建** | 11 行更新（8 条 B→A + 3 条 B 的复核备注），`--dry --only B97_MISC2` 实跑：**更新 11 · 新增 0 · 无变化 0**；`check_patch_conflicts.py` = **无跨补丁重复键** |

⛔ **没有碰**：`Editor/RuleEngineTest.cs` · 账主表 · 正本 · `Pinned*` · `gen_cards_engine.py` · git · Unity 批处理 · 别人名下文件（`RankedTab.cs` / `FactionScore.cs` / `GachaEventWindow.cs` / `DeckRuntime.cs` 等一个字节未动）。

`typecheck`（`TMPDIR=/tmp/wf_misc2`）：**运行时 0 / 编辑器 0**。
（中途有一次跑出 3 个错，全在 `CardPresentation/Deck/DeckRuntime.cs` —— `ClearPoolBanIcons` / `ShowPoolBanIcon` / `SetPoolBanIcon` 未定义，**那是别人在飞的 Ban Icon 活**，不是本代理；重跑已 0/0。）

---

## 三、断言接法（⛔ 本代理不改宿主）

宿主建议 = **`CardPresentation/Editor/ShellScene.cs`**（`Shell/*.cs` 全是它建的，`B93S3Assert` 也在这儿）。
把下面这句**追加到 `ShellScene.cs:11023` 那一行的 `Debug.Log(P + $"=== 合计` 之前**（同一行追加 ⇒ **行数零变化**，不会让账落点漂移）：

```csharp
B97MISC2Assert.Run(); _sink.Pass += B97MISC2Assert.Pass; _sink.Fail += B97MISC2Assert.Fail;
```

**断言清单**（15 条，逐条的「什么改动能让它红」写在文件头）：

| 段 | 条数 | 盯什么 |
|---|---|---|
| A | 4 | `GetMax` 返回**元素**不是投影值 · 单元素 · **同分取后者** · 空序列抛 `InvalidOperationException` |
| B | 4 | `GetMin` 方向 · **同分取后者** · 空序列抛 · `TU = long` 也成立 |
| C | 5 | 11 张指针 + 10 张高光真的 `Resources.Load` 得出来 · 表 11 行 · 只有 Dark Angels 的 Glow 是 null · 阵营 0 返 false 且两 out 都 null |
| D | 2 | `RuleEngine` 程序集里 `DeepCopy` 成员 = **0**（钉住「原版那家族外 0 调用 ⇒ 本仓不实现深拷」） |

---

## 四、待接线 / 可粘贴补丁

### 4·1 `:295` `FactionScore.Initialize` —— 宿主在别人名下（`B97_W2`）

`Shell/RankedTab.cs` 的 `BuildRow` 里（内联等价那一段）改成走组件本体：

```csharp
var fs = CardPresentation.ScoreBars.FactionScore.Create(
             row, new PxRect(rr.x1, rr.y1, rr.x1 + 438.74f, rr.y1 + 117.71f),
             CardPresentation.ScoreBars.FactionScoreVariant.Small, "FactionScoreSmall");
fs.Initialize((CardArmy)army, /*currentScore*/ 0, /*highestScore*/ 0);   // 分数是服务端（P3）⇒ 0 = 原版空态
fs.RatingDisplay?.Bind(<`Individual rating value` Label>, <`Main Icon` quad>);
fs.HighestRatingDisplay?.Bind(<第二行那两个>);
```
（`Shell/RankedEventWindow.cs` 的 footer Small×1 同法；⚠️ 三个宿主矩形**逐实例被覆盖**，见 `报告/B94_B17.md` §5·2 五元组。）

### 4·2 `:312` `CardCollectionReferenceCardPointer` —— **形状派**补丁（只等一句裁定）

在 `Shell/CollectionWindow.cs` 的 `var holder = Node(page, "Scroll View", CardsViewport);`（**:1121**）**下一行**插：

```csharp
// 原版 `CardCollectionReferenceCardPointer` 的载体：`Reference Card Pointer`（`m_IsActive=0`，只当矩形用）
// 判据 = `menu_dump.py bundle_menus_assets_all "CardsTab" --depth 8`：402.3,236.4 → 596.1,281.4（193.84×45.09），
//        与 `Scroll View` 同级、挂在 `Collection Display` 下。矩形与 `WcRefL/T/R/B` 同为一份，断言守漂移。
var refPt = Node(page, "Reference Card Pointer", new PxRect(WcRefL, WcRefT, WcRefR, WcRefB));
if (refPt != null)
{
    refPt.gameObject.SetActive(false);                                  // 原版 `m_IsActive = 0`
    MenuTutorialFlow.RegisterUnique("CardCollectionReferenceCardPointer", refPt);
}
```

⚠️ 两个坑：① 这是**中段插行** ⇒ `CollectionWindow.cs` 的账落点（含 `:5118`）会整体漂移，同步点要跑 `check_ledger_points --fix`；② 若调度台选**等价派**（常量 AABB 已等价），则**不要**建，直接判 A —— 二者⛔ 别都做。

### 4·3 `:430` / `:584` —— 需要调度台定口径（不是「没查」）

- `:430`：要么为「服务端活动数据」重写两扇窗的记分条（≈80 行几何），要么维持「手工等价族 + 组件本体备而不用」。本代理**判据给齐、零改动**。
- `:584`：缺口不在战场线，在**战斗 HUD**——先要建那颗会自转的 `Highlight` UI 图（Image + `SimpleRotation`，speed `(0,0,100)`），再谈驱动。归 HUD 线的账。

---

## 五、没查清 / 顺手发现

1. **`Shell/ListExtensions.cs` 从来没被建出来**（账 `:398/:399` 的备注说 B96_MENU 建了 —— 不成立）。本批已真建，并在新账备注里写明。
2. **`CursorByArmySO` 的 19 张图其实早就拷全了**（21 个 PNG、128×128 现读），缺的只是 Unity 首次打开时的自动 import；且 `CardArt.MenuUi` 走 **Texture2D** ⇒ ⛔ 不需要「导成 Sprite 类型」。
3. **`SimpleRotation` 的 39 个竞技场实例是战斗场景里的 UI 图**（RectTransform+CanvasRenderer+Image），不是 3D 景物 ⇒ 账上「战场 prefab 没驱动」的方向要改（已写进 `:584` 的差异列）。
4. **`UICornersGradient` 不需要「6 处挂组件」**：6 个实例全是四顶点 Image，双线性在四顶点上退化成「每顶点取自己那一角」，与 `ImageQuad.SetCornerColors` 恒等；主菜单队伍 A/B 两处已按 MB 实读值断言。B93_N6 判「必须挂组件才 A」过严（已写进 `:667` 的差异列）。
5. **`out_cpp2il_dev_ilspy` 的 `ListExtensions` 里两个 lambda 都被降级成「两支都 `return this`」** ⇒ 方向**只能**从 `.c` 读（这也解释了 B94_B6 当初为什么记反）。
6. **没解出来的格子**（如实记，不猜）：`UICornersGradient` 那 6 个实例的**父链 prefab 名**（父 pathID 是大整数、不在小序号命名里，需按 `m_FileID` 跨 CAB 解）；13 场每场第 3 个 `SimpleRotation` 宿主的 GameObject 名（`go=112/95/106/…` 一批小 pathID 找不到同名文件）。

---

## 六、出处（本报告引的坐标都是本轮现读）

- 分诊：`资料/完全复刻/判据/B96_菜单未分类44_分诊.md` §三 / §4·1
- 判据：`d:/2/tools/decomp_full/ListExtensions__{GetMax_object__int_,GetMin_object__long_}.c` ·
  `ListExtensions.__c__DisplayClass{6_0_object__int____GetMax,7_0_object__long____GetMin}_b__0.c` ·
  `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/ListExtensions.cs:2527/2603`
- 资源：`d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_{1941,-2135209279831517756,-2930347684145025530,-6144089960178395519,1002312900167314950,8800391613077463723}.json` ·
  同一批的 `m_Script` = `8733114785328696827`（`dump.cs:114216-114227` = `UICornersGradient` 四字段）
- `SimpleRotation`：`dump.cs:17327-17340`（唯一字段 `speed` @ +0x20）· `bundle_Waprforge_monoscripts/MonoScript/MonoScript_-6857224376968460714.json` · `bundle_scenes_scenes_battlearena1/GameObject/Highlight_{396,180}.json`
- 本仓：`Shell/ListExtensions.cs`（新）· `Editor/B97_MISC2_Assert.cs`（新）· `Core/CardArt.cs:890` · `Battle/ImageQuad.cs:517` ·
  `Shell/CollectionWindow.cs:1121/5118/5128` · `Shell/SkirmishEventWindow.cs:501/536` · `Shell/EnergySinglePlayerOnlyEventWindow.cs:1179` ·
  `Shell/ItemDrawer.cs:2289-2355` · `Editor/MainMenuScene.cs:18032-18038`
