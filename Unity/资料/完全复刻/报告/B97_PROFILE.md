# B97_PROFILE 报告（账 `个人资料_成就_排行.tsv` 7 条：56 / 57 / 97 / 197(C) / 198 / 221 / 293）

> **代理**：`B97_PROFILE`（执行）· **日期**：2026-10-29 · **判据正本**：`判据/B97_个人资料7条_施工单.md`（七列施工单 + 附录 A pid 表）
> **typecheck**：`TMPDIR=/tmp/wf_prof bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（最后一轮，在上述全部改动落盘之后）
> **账补丁**：`账/_parts/B97_PROFILE.tsv`（11 列 · 首行表头 · 7 行 · `方法` 列全带 `(...)` · `check_patch_conflicts.py` = 无跨补丁重复键）

## 1 一句话结论

7 条**一条不落**：**2 条 A**（221 粒子树的 ③④ 补齐）· **2 条 B**（197/198 奖励活动卡已实现、差最后 1 行接线）· **3 条 D**（56/57 = 原版死类；97/293 = 上游在服务器）—— 三条 D 都写了「原版调用链断在哪一行」的硬证据，不是逃生口。

## 2 做了什么（逐条：账行 → 改了什么文件.方法 → 验证方式）

| # | 账行 | 结论 | 改了什么 | 验证 |
|---|---|---|---|---|
| 1 | **56** `AvatarBorderController.Initialize()` | **B → D**（原版死类） | **零代码**（只在账里改判 + 补第三条证据） | 见 §4「56/57 的三条证据」 |
| 2 | **57** `Initialize(PlayerInfo playerInfo)` | **B → D**（同上 + `playerInfo` 死形参） | **零代码** | 同上 + `B97ProfileAssert` 第 ④ 节（边框取图口 == 已装备那条） |
| 3 | **97** `CosmeticItemAvatarBorder.HandleOnCollect(int)` | **B → D**（REMOTE：上游「获得边框」= liveops） | **零代码**；补断言把「装备那一半」钉住 | `B97ProfileAssert` 第 ⑤ 节（不存在的 id ⇒ false 且不改下标；真实 id ⇒ true 且下标跟着换；跑完还原） |
| 4 | **197** `RankedArmyBonusContainer.Clean()` | **C → B**（已实现，未接线） | **新建** `Shell/RankedArmyBonusCard.cs`（卡的整族 + `Clean()`） | `B97ProfileAssert` 第 ③ 节 |
| 5 | **198** `RankedArmyBonusContainer.OnInitialize(LiveOpsEvent)` | **B → B**（已实现，未接线；数据 = 🔶 本地模拟表） | 同上（`Initialize`）+ **新建** `Resources/Art/ranked_army_bonus.json`（🔶） | `B97ProfileAssert` 第 ③ 节（含 2 vs 3 个阵营的判别式） |
| 6 | **221** `ChangeParticleEffectAndSpriteRank(Image, ParticleSystem, Sprite)` | **B → A** | **新建** `Shell/RankedDivisionParticles.cs` + 判据 JSON `Resources/Art/ranked_division_particles.json` + 生成器 `工具/gen_ranked_division_particles.py`；**改** `Shell/RankedDivisionChangeWindow.cs`（`:339-341` 就地换 3 行、`:344-347` 的 `NoteNoArt` 换注释、`:836` 就地两语句、类尾追加 `ApplyParticleAndGlow()`）；**改** `工具/import_mainmenu_effect_mats.py` 的 MAP（+2 材质） | `B97ProfileAssert` 第 ①②节（结构 + ③④ 的前后判别式） |
| 7 | **293** `RankedV2Event.Expire(Action)` | **B → D**（REMOTE：拉榜 P3 + `EventComponents[21]` 不可解析） | **零代码**；把「闸不是同一条 + 为什么不是一行改」写死进差异列 | 见 §5「293 为什么不改闸」 |

## 3 改动清单（文件 + 行数增减 + 新增成员必须在类尾）

### 3.1 新建（4 个）

| 文件 | 行数 | 说明 |
|---|---|---|
| `CardPresentation/Shell/RankedDivisionParticles.cs` | 453 | 两棵粒子树的运行期建件器（DTO + 坐标桥 + 模块 applier） |
| `CardPresentation/Shell/RankedArmyBonusCard.cs` | 332 | `Reward Event Container 1x1` 一族 + `Initialize` / `Clean` + 🔶 `RankedArmyBonusData` |
| `CardPresentation/Editor/B97_PROFILE_Assert.cs` | 293 | 断言集（宿主待接线，见 §4） |
| `工具/gen_ranked_division_particles.py` | 320 | 判据 JSON 生成器（**默认只打印用法**；`--dry` 打印树；`--apply` 才写盘） |

### 3.2 新建的资源（`Resources/Art/` 在 `.gitignore` 里，与既有 `card_display_effects.json` 同族）

- `Resources/Art/ranked_division_particles.json`（4,876 行 · 16 节点 / 14 颗 `ParticleSystem`）+ `.meta`
- `Resources/Art/ranked_army_bonus.json`（🔶 本地模拟表）+ `.meta`
- `Resources/Art/vfx/Up Rays Additive.mat` + `.meta`（来源 = `WarpforgeVFX/Materials/Up Rays Additive.mat` 字节拷贝）
- `Resources/Art/vfx/Glow Additive Extra Color.mat` + `.meta`（来源 = `WarpforgeVFX/Materials/Glow Additive Extra Color__80c1155a.mat`；**两个变体 `diff` 只差 `m_Name` 一行**，取名字干净那份）

### 3.3 修改（2 个，**行数守恒**已机检）

| 文件 | 改动 | 行数 |
|---|---|---|
| `CardPresentation/Shell/RankedDivisionChangeWindow.cs` | `:339-341` 3 行 → 3 行（改调 `RankedDivisionParticles.Build`）· `:344-347` 4 行 → 4 行（`NoteNoArt` → 注释）· `:836` 1 行 → 1 行（**同一行**两条语句）· **类尾**追加 89 行（`_divisionPs/_rankPs/_rankIconSprite/_warnedNoGlowQuad` 四个字段 + `RankIconSprite` 属性 + 4 个自检读口 + `ApplyParticleAndGlow()`） | **1514 → 1603**（+89，全部在类尾 = 最后一条账落点 `:1459` 之后） |
| `工具/import_mainmenu_effect_mats.py` | `MAP` **+2 条**（`Up Rays Additive` / `Glow Additive Extra Color`）+ 4 行注释 | **221 → 228**（+7） |

> 🔴 **行号守恒机检**：改完立刻跑 `python -I -X utf8 工具/check_ledger_points.py` ——
> `个人资料_成就_排行.tsv  A=222 通过=222 漂移=0 告警=10 失败=0`。
> ⚠️ 第一次改的时候我把 `:344-347` 那 4 行 `NoteNoArt` 换成了 **3 行**注释 ⇒ 闸立刻报 `:753/:1409/:1432/:1458` 四条漂移（−1）。
> 补回一行后归零 —— 这条记在这里，**下次就地改注释一定要先数行**。

## 4 验了什么

### 4.1 typecheck（唯一实际跑得动的闸）

`TMPDIR=/tmp/wf_prof bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时错误数 0 / 编辑器错误数 0**（在全部改动落盘之后跑的最后一次）。
⚠️ 过程中真拦下两个错：`ParticleSystem.TextureSheetAnimationModule` **没有** `animationType`（真名是 `animation`）· `MenuDraw.Rect/Nine` 的实参位置（`tint, keepAspect` 而不是 `tint, null, false`）。

### 4.2 断言（新建 `Editor/B97_PROFILE_Assert.cs`；⚠️ **本批没有跑 Unity**，所以这些断言**尚未执行过一次**）

**建议接线位置**（调度台做）：`Editor/ShellScene.cs` **第 10637 行那一整行之后**插一行

```csharp
        B97ProfileAssert.Run(_sink);   // 🆕 2026-10-29（B97_PROFILE · 账 个人资料/成就/排行 56/57/97/197/198/221）
```

理由：`:10637` 是 `MenuCheck.RaycastFilterAsserts(_sink); MenuCheck.ToastChannelAsserts(_sink);`（既有「追加在同一行末 / 合计行之前」口径的那一处），而 **`ShellScene.cs` 的最大账落点是 `:10351`** ⇒ 在这里插行下游零漂移。

覆盖：
- 第 ① 节：16 节点 / 14 颗 PS / **`Particles Rank Up` 没有 `RectTransform`** / 根 `scale = 164.23738/108` 与 `1/108` / 子树 `scale` 不除 108（`250.00007`）/ 三组子件**名集合逐字相等** / 5 颗 `shape.enabled = false` / 4 颗 Noise / 2 颗 Trail + `renderMode = 5`
- 第 ② 节：**前后判别式** —— 同一张窗，`rankIconSprite == null` 时 `Renderer.enabled=false` 且 `Rank Icon Glow` 关；注入 `Sprite` 的那一张 ⇒ `enabled=true`、`textureSheetAnimation.GetSprite(0) == sprite`、`Rank Icon Glow` 开
- 第 ③ 节：3 个阵营 ⇒ 3 颗且**顺序一致** + `AutoScrollEnabled=true`；**2 个阵营 ⇒ false**（判别式）；`Clean()` ⇒ `childCount 0` / `Count 0`；`null` / 空数组 ⇒ **卡被销毁**
- 第 ④ 节：`AvatarTab.BorderArtName() == ProfileData.EquippedBorderArtName`（空则 `ArtBorder`）+ 金框反证
- 第 ⑤ 节：`SetEquippedBorderById` 的正/负路径 + 还原

### 4.3 56 / 57 的三条证据（本代理**现跑**，不是抄）

1. `grep -rl AvatarBorderController d:/2/tools/decomp_full/` ⇒ **2 个文件**（`AvatarBorderController__Initialize.c` / `__OnEnable.c`，都是本类自身）⇒ 全库**零调用点**
2. `grep -rln AvatarBorderController cpp2il/out_cpp2il_dev_ilspy/` ⇒ **1 个文件**（它自己那份 `.cs`）
3. 脚本 guid `176dcf275560eed9ec19eaa0baaa5f0e` 在 AssetRipper 导出工程 `*.prefab` / `*.unity` / `*.asset` 中引用 **0**（`grep -rl` 现跑无输出）⇒ **没有任何挂该控制器的序列化实例**

⇒ 按方案 §4.1「**死类一律判 D，不许判 A**」+ 铁律 11 例外① 结案。⛔ 仍不落地「恒画金框」（`B92-N4b` 裁定不变）。

## 5 没查清 / 判不了的部分

1. **🔴 与简报的一处口径分歧（请调度台裁）**：简报对 **97 / 293** 写的是「同样走『客户端 UI 照原版 + 数据本地模拟 + 出声 + 🔶 标』」，而我判了 **D · 零代码**。理由（两条都已写进账的差异列）：
   - **97**：本地模拟「获得边框」会**改可见装备态**（4 条 `aa=1` 一装上，边框图就从 `Default` 变掉），而**原版离线此刻的装备态就是 `Default`**（4 条 `aa=1` 全带父活动、父活动全不在期）⇒ 模拟反而是「造出原版没有的状态」。装备那一半已有 A 级落点 + 断言。
   - **293**：原版**没有**任何「season 变了 ⇒ 开 NewSeason 窗」的映射（落哪扇窗由不可解析的 `EventComponents[21]` 决定）⇒ 造本地「开窗映射」＝**自造原版不存在的映射**（施工单本身写着 ⛔ 别自造）。拉榜 = P3。
   ⇒ **若调度台仍要按简报做**，请明确「模拟什么数据、模拟到哪一步」，我可以只做**装备/开窗之外**的那一半（§7 给了一行钩子）。
2. **293 的判定闸与原版不是同一条**（我们 `_points != 0 && _lastPlayedSeason == 0`，原版 `LastPlayedSeason == seasonNumber`）。**不是一行改**：改公式必须同时改本地赛季默认值（`RankedData.ResetForTest` 的 `_lastPlayedSeason = 1` / `AdvanceSeason` 的 `= 0`），否则**新档会恒弹**（`1 == 1`）。产品侧这半句**只有断言在读**（`Editor/B94B10_Assert.cs` F 组三条），⇒ 本批不动，等裁定。
3. **`Reward Event Container 1x1` 的 `Background Image` / `Foreground Image`**：原版是 `UIParallaxImage`，**贴图由活动数据运行时喂**（prefab 里 `m_Sprite = 0`）⇒ 我们只建节点 + 出声，**没有图可画**。活动图在服务端。
4. **`TimerDisplay` 没有等价类**：原版那是一件 `Everguild` 计时组件（`extraText` / `lastMinutesText` / `layoutGroupContentFixer` …），我们用一句 `Timer Text` + `Clock Icon` 承载，格式照 prefab 的 baked 样例 `'5d 20h 15m'`。⛔ 没做「按 60 秒切格式」之外的任何行为。
5. **`Simple Army Icon` 的布局高度**：原版 `sizeDelta (254.90341, 0)`，高度由 `Content` 的 `HorizontalLayoutGroup`（`ctrlH = 1` / `expandH = 1`）撑满。我们**没有 uGUI 布局组** ⇒ 手算（宽度 254.90341 / spacing 20 / 垂直居中）。**间距与宽度是判据值，垂直拉伸是布局组语义的等价物**。
6. **`SubModule`（子发射器）没接**：实测这 14 颗的 `SubModule.enabled` **全部 = false**（数据里也没有内容）⇒ 接了也是空转，本批不写（已写进 `RankedDivisionParticles.cs` 文件头）。
7. **`Trails R/L` 的第二张材质（`m_Materials[1]`）没接**：它们 `renderMode = 5 (None)` 不画粒子本体，拖尾用 `m_Materials[0]`。**已核：无行为差别**。
8. **`uv.rowMode` 没映射**：`Promote Letter Effect` 两份是 `animationType = SingleRow` / `rowMode = 1 (MeshIndex)` / `rowIndex = 0`；Billboard 渲染下 MeshIndex 与 Custom 都取第 0 行 ⇒ **本批无行为差别**，已写进文件头。

## 6 顺手发现（⛔ 不只放在对话里）

1. 🔴 **`CardDisplayEffects.ApplyParticle`（卡片展示窗那 15 条光效的建件器）有两处与判据不符，本批**没改**（怕与在飞的特效线撞车）：
   - 它**无条件** `shape.enabled = true`（原版有 `false` 的）；
   - 它**没有** Noise / Trail 模块；
   - 它的 `Grad()` **没有 `mode 4`（RandomColor）那一档** ⇒ 落到 `default` 只取 min 色（粒子本该在 min/max 两色间随机）。
   ⇒ **待收口**：`Shell/CardDisplayEffects.ApplyParticle` 与 `Shell/RankedDivisionParticles.Apply` 应合并成**一个**共用 builder（CLAUDE.md §三「两处写同一条规则 = 迟早不一致」）。本批为了零撞车选择了「新开一份 + 在文件头写明」。
2. 🔴 **`Particles Rank Up` 一族是裸 `Transform`**（`Particles Rank Up` / `Wave Shine` / `Wave Shine R` / `Trails R` / `Trails L`）—— 这解释了 `MainMenuSubmenuWindow.NewPlainTransform` 注释里「全工程唯一一处」那句话**已过期**（它只统计了 `Shell/` 里手写的节点，没算运行期按判据建的粒子件）。本批是**第二处**调用它。
3. 🔴 **同名子件必须按 `m_Father` 链配对**：`Sparks Burn` / `Glow Big` / `Glow below` / `Promote Letter Effect` / `Sparks fast` **各 2 份**，分属两棵树。施工单附录 A 里给的 pid 表**是对的**，但**文件名后缀 `_<pid>` 是导出器给同名 GO 的序号、与树无关** —— 按后缀猜会配错一棵树（本批用 `Transform.m_Children` 逐级下钻确认）。
4. 🔴 **`armyPrefab` 的 PPtr 指向的是【组件】不是 GameObject**：`5516551289399767058` 是 `Simple Army Icon` 那颗 `SimpleArmyImage` 的 **MB pid**（也在它的 `m_Component` 里）。施工单的 §未取到 ② 那条「`armyPrefab` 的 Prefab pathID 未反查」**现已解**：GO pid = `-3315707156309839854`，RT `sizeDelta (254.90341, 0)`、anchors/pivot `(0,0)/(0.5,0.5)`、`SimpleArmyImage.armyIcon` = 白图 `Image`（`m_Sprite = 0`、`preserveAspect = 1`）、`armyText = null`。
5. `MenuDraw.Nine(…)` 的实参顺序是 `(parent, tex, r, border, texW, texH, q, tint?, fillCenter, name)` —— 与 `MenuDraw.Rect` 的 `(…, q, tint?, keepAspect, clip?)` **不同形**，容易写错（本批被 typecheck 拦了一次）。
6. **`Resources/Art/` 整目录在 `.gitignore` 里**（`.gitignore:93`）⇒ 本批新建的 4 个资源件（JSON + 2 张 `.mat` + `.meta`）**不会进 git**，与既有 `card_display_effects.json` 同族；重跑生成器 / 导入脚本即可复现。

---

## 7 `Shell/MainMenuRuntime.cs` 的可粘贴补丁（197 / 198 的接线；⛔ 本批**没有**动那个文件）

> 背景：`MainMenuRuntime.cs` 本批被 `B97_MENU` 占用 ⇒ 按「一个文件同一时刻只有一个写手」交回补丁。
> **应插哪一节**：`_placeAllCards` 那个委托里、**五张手挑卡最后一行 `Put("Base Game Mode Container 1x1 - Tutorial", …)` 之后**（即 `};  // ← _placeAllCards 委托结束` 的**前一行之后**）。
> **为什么在这儿**：`used`（网格占位表）与 `_modeOffsetPx` 都是委托内的局部/字段，只有在这儿才拿得到；且第一次摆放与滚动重建走同一份代码（本文件 §T12 那条纪律）。

### 7.1 要加的字段（**追加在类尾**，⛔ 别插进字段区 —— 本文件有账按行号引用）

```csharp
        /// <summary>🆕 2026-10-29（`B97_PROFILE` · 账 `个人资料_成就_排行:197/198`）：奖励活动卡
        /// （原版 prefab `Reward Event Container 1x1` = 类 `RankedArmyBonusContainer`）。
        /// 数据 = `Resources/Art/ranked_army_bonus.json`（🔶 自己设计：服务器已关服，判据在远端）。</summary>
        RankedArmyBonusCard _rankedArmyBonusCard;
```

### 7.2 要插的那一段（五张 `Put` 之后、`};` 之前）

```csharp
            // 🆕 2026-10-29（`B97_PROFILE` · 账 `个人资料_成就_排行:197/198`）：**奖励活动卡**。
            //   判据：原版 `RankedArmyBonusContainer.OnInitialize(LiveOpsEvent)`（`decomp_full/…__OnInitialize.c:30-186`）
            //   —— `evt == null` / 类型闸不等 ⇒ `Destroy(this)`；否则按 `RankedArmySpotlightData.AffectedArmies`
            //   逐个 `Instantiate(armyPrefab=Simple Army Icon, armyContentAnchor=Content)`。
            //   🔶 数据 = `Resources/Art/ranked_army_bonus.json`（服务器已关服、判据在远端，CLAUDE.md §18·0）。
            //   ⚠️ 没有数据 ⇒ **这张卡根本不建**（= 原版「没数据就毁卡」，⛔ 不留空卡）。
            {
                var bonusEvent = RankedArmyBonusData.FirstValid(RankedArmyBonusData.Load());
                if (bonusEvent != null)
                {
                    int colB = -1;
                    for (int i = 0; i < MaxCols && colB < 0; i++) if (!used[i, 0]) colB = i;
                    if (colB < 0) Debug.LogWarning("[Menu] 🔶 奖励活动卡在网格里排不下（最多 " + MaxCols + " 列）⇒ 不建");
                    else
                    {
                        used[colB, 0] = true;                       // 原版它是 1×1 ⇒ 只占 row0（⛔ 别占整列，会留洞）
                        _rankedArmyBonusCard = RankedArmyBonusCard.Create(content);
                        if (_rankedArmyBonusCard != null)
                        {
                            MenuDraw.ApplyPxRect(_rankedArmyBonusCard.transform, content,
                                new PxRect(CardCol0 + colB * ColStep - _modeOffsetPx, CardRow0Top,
                                           CardCol0 + colB * ColStep - _modeOffsetPx + CardW, CardRow0Top + CardH));
                            _rankedArmyBonusCard.Initialize(bonusEvent);   // 内部会自己 `Clean()` 再填
                        }
                    }
                }
            }
```

### 7.3 调用链自证（不跑引擎也能核）

1. `RankedArmyBonusCard.Create(content)` ⇒ `Build()`：先按 §E-1 把 `TextDarkening` / `Event Title` / `Timer` / `Border` / `Feature Badge` / **`Dynamic Content ▸ Viewport ▸ Content`** 建出来（`Content` 是 `Clean()`/`Initialize()` 唯一的落点，原版 `armyContentAnchor` = `4818649679952315391` = 这个 `Content`）。
2. `Initialize(bonusEvent)` ⇒ ① 非空（否则毁卡）；② `Clean()`；③ 按 `affectedArmies` **原序**建 N 颗 `Simple Army Icon`（图标 = `ArmyIcons.IconName(army)`）；④ `AutoScrollEnabled = 2 < N`。
3. 滚动重建时这个委托整段重跑 ⇒ 旧卡随 `content` 一起销毁重建；**若 `CleanupContainers` 不是整棵清空**，请在重建前的复位段（`_draftStateNode = null; …` 那一行）顺手补 `_rankedArmyBonusCard = null;`。

### 7.4 一行钩子（97 上游到账时用；**本批不落**）

```csharp
ProfileData.SetEquippedBorderById(rewardId);   // 原版 `CosmeticItemAvatarBorder.HandleOnCollect` 为真 ⇒ `SetCurrentItem(this,0,0,0)`
```

---

## 8 交接给下一个代理（若需要）

- **本批全部完成**：4 个新文件 + 2 个改文件 + 账补丁 + 本报告，typecheck 0/0，`check_ledger_points` 本域 0 漂移 0 失败。
- **剩给别人的两件**：① `Editor/ShellScene.cs:10637` 之后接一行 `B97ProfileAssert.Run(_sink);`；② `Shell/MainMenuRuntime.cs` 应用 §7 的补丁（文件释放后）。
- **坑**：改 `RankedDivisionChangeWindow.cs` 的注释**必须先数行**（本文件 `:753/:1409/:1432/:1458` 有账落点，少写一行就是四条漂移）。
