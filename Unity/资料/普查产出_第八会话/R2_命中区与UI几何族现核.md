# R2 · 命中区 / UI 几何族 —— 逐条现核（第八会话 · 只读）

> **谁写的**：只读现核代理 **R2**。**未跑 Unity**、**未改任何生产代码 / 正本 / 白名单 / 断言**、
> **未碰 git 写操作**、**未跑 typecheck**（怕与在飞写手互相污染）。
> 🔴 **行号一律【本轮现读】**（⛔ 没抄 A 表；⚠️ 现核途中 `Editor/BattleScene.cs` **正在被 E5 写**，
> 它的行号在我这一趟里**漂了约 +150 行**，凡涉及它的行号请**按符号名**找）。
> **写权限**：本文件是唯一的落盘产出。

---

## 〇 · 总览（20 个编号 + 8 条零碎）

| 编号 | 今天还开不开 | 一句话 |
|---|---|---|
| **`A848`** 尾巴 | 🔴 **仍开着**（判据齐、离线可量；屏上偏移要 Unity） | 裁切口径靠 `import_original_art.py:1037` 裁 `textureRect`；uGUI 真用的是 `sprite.rect`（`Image.cs:1089/1098`）⇒ `A1011` 那条同源 |
| **`A828`** 尾巴 | 🔴 **仍开着**（①④ 都开） | ① `Broadcasts` 计数**全仓 0 断言**；④ 35 个 def 走 `[ERROR] particle system not assigned`，**没跑过** |
| **`A268①`** | 🔴 **仍开着**（要 Unity 实测） | 多材质支路现读 = `MenuDraw.cs:1248-1250` 的 `meshInfo[ch.materialReferenceIndex]` |
| **`A990`** 尾巴 | ✅ **已不成立**（①② 都收口） | `MenuDraw.cs:553` 与三处命中判定全改走 `PixelOfDesign` |
| **`A992`** 尾巴 | 🔴 **仍开着**（且「在不在哪一层去重」**未裁**） | 叠词来自卡面数据；去重不改行为 |
| **`A1004`** | ✅ **已不成立** | 「一行方案」已找到并落地（`MenuDraw.cs:553` 带订正痕） |
| **`A1010`** | 🔴 **仍开着**（①② 都开；① 是**纪律**不是待办） | ② `CardDef.cs:605-607` 仍写着「见本轮交件报告的『顺手发现』」 |
| **`A1011`** | 🔴 **仍开着 —— 而且是【真·待裁】** | 本轮量出硬数：原版 `m_Rect` **233/233 = 707×1020（比例 0.6931 恒定）**，我们 PNG **233 张比例 0.6188~0.7652** ⇒ 两说**最大差 ≈ ±10%** |
| **`A1041`** | 🟡 **主体已不成立**（army 修法+断言已落）；**A/B/D 三条白名单断言【未被采纳】** | 依据 = `A964 续 ②` 裁定「E3 探针改**报告式**、不做普适断言」 |
| **`A1044`** | 🟡 **部分仍开着**（① ② ⑤ 开 · ③ ④ 已收口） | ① `ShellScene.cs:1199-1207` 内联甲式循环；② `UnionQuadRect` 的 `QuadGate.None` 要不要复核**未裁**；⑤ 措辞 |
| **`A1053`** | 🔴 **仍开着**（剩 5 处，逐条停手理由都还成立） | 见 §A1053 |
| **`A1058`** | ✅ **已不成立**（15 处全改完 + 断言在 `ShellScene.cs:5550`） | 换图层 = 子件 `Background`/`Icon` |
| **`A1066`** | 🟡 **大部分已收口**（25 处里 20 处已改；剩 5 处 = 就是 `A1053` 那 5 处停手） | 行号已整片漂 |
| **`A1067`** | ✅ **已不成立**（5 条红全清，已归档） | `MenuDraw.PaddedRect` 成唯一算式，`ArmyIconPad`/`ClosePad` 全在 |
| **`A1092`** | 🔴 **仍开着**（两处都不许只改一半） | ① `CampaignTab.cs:569/570` ② `SettingsWindow.cs:2427/2431`（A 表写的 `:2401/2405` 已漂） |
| **`A1112`** | 🔴 **仍开着，E8 正在飞** | `Editor/MainMenuScene.cs` 工作区里已见 4 处新断言（未提交） |
| **`A964①`** | ✅ **已不成立**（7 行 **E2①~⑦ 全有断言**，落在 `1667ff6`） | 见 §A964 |
| **`A964②`** | 🔴 **仍开着**（判据一条都没有） | 六处 `Contains` 现读**逐处都在**、**一处都没过 padding** |
| **`A1120`** | 🟡 **E1 已交回、代码已落工作区（未提交）** | 四条红**全判 α（断言侧）**，`Shell/SettingsWindow.cs` 一个字没动 |
| 零碎 8 条 | 见 §三 | 其中 `minTimeBeforeSkip` / `CS1573 已归零` 两条**已被就地订正过** |

---

## 一、逐条明细

### `A848`（尾巴）—— alpha 裁切「裁得正不正」

| 字段 | 内容 |
|---|---|
| **还开不开** | 🔴 **仍开着**。判据齐、**离线可量**；「屏上偏多少」这一半**必须跑 Unity** |
| **落点（现读）** | 裁切代码 = `Unity/工具/import_original_art.py:1037 write_cardback`（读 `Sprite/<名>_Main.json` 的 **`m_RD.textureRect`** 裁）· SDF 版 `:1082 write_cardback_sdf` · `:1108 write_portrait` |
| **判据** | uGUI 真值（**本轮亲读**）= `MyGame/Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Image.cs:1089-1090`（`spriteSize = new Vector2(activeSprite.rect.width, height)`）+ `:1096-1099`（`PreserveSpriteAspectRatio(ref r, spriteSize)`）⇒ **取的是 `sprite.rect`（= `m_Rect`），⛔ 不是 `textureRect`**；`A` 表把它写成「`Image.cs:1089/1098`」**成立**。判据转述 → `资料/待办判据_第四会话.md §A1011` 末段 + `资料/普查产出_第四会话/现核_引擎与战斗族.md` 第 7 条 |
| **要改的文件** | `Unity/工具/import_original_art.py`（**只在量出偏差时**）+ 重导 `MyGame/Assets/CardPresentation/Resources/Art/cardbacks/**`（⚠️ 在 `.gitignore` ⇒ **git 一声不响**）。**无断言宿主要改** |
| **一句话改法** | 拿「我们 PNG 的尺寸」比「原版 `m_Rect`（233/233 = 707×1020）」——**这一个比值就是 `A1011` 要裁的那件事**，两条账**同一个根**，建议**并成一条做** |
| **自检覆盖** | `CardBaseDemo.Run`（并排渲染图）。⚠️ 纯工具改动本身 **0 条**；动了 `Resources/Art/**` 之后要跑卡面那一族 |

### `A828`（尾巴）—— ① 订阅者有没有真被叫起来 · ④ 35 个 def 的 ERROR

| 字段 | 内容 |
|---|---|
| **还开不开** | 🔴 ① 仍开着 · ④ 仍开着 |
| **落点（现读）** | ① 计数口 = `MyGame/Assets/WarpforgeVFX/Runtime/WFModuleParticleCollisionNotifier.cs:51 Broadcasts`（自增 `:93`）—— 🔴 **`grep -rn Broadcasts CardPresentation/Editor/` 现读 = 0 命中** ⇒ **没有任何断言读它**。已有的**相近**断言（⛔ 不是它）＝ `CardPresentation/Editor/BattleScene.cs:5222-5226`（`WFModuleCollisions.CallsBound >= 1`「那条订阅**装上了**」）+ `:5244-5245`（点一下碰撞事件、补间起播）。<br>④ = `WarpforgeVFX/Runtime/WFModuleCollisions.cs:175 Debug.LogError("[ERROR] particle system not assigned to particle VFX")`（上游 `:165 Initialize` → `:189-190` 取/补 notifier） |
| **判据** | `资料/普查产出_第四会话/现核_引擎与战斗族.md` 第 8 条（「① 只差断言；④ 跑一次留日志」）+ `资料/历史/交接_第六会话.md:91`（旧账一批） |
| **要改的文件** | ① `CardPresentation/Editor/BattleScene.cs`（**⚠️ E5 正在写它** —— 切块时要注意）<br>④ **无代码改**，只要跑一次 `BattleScene.Run` 并把日志留下来判 35 个 def |
| **一句话改法** | ① 在现成的碰撞那一节尾巴补一条「`Broadcasts > 0`（点一下真的叫到了订阅者）」+ 灭自证（不点 ⇒ 必须 0）；④ 跑一次、把这 35 条 ERROR 逐条判「原版也这样 / 我们的缺口」 |
| **自检覆盖** | `BattleScene.Run` |

### `A268①` —— `ClipTmpMesh` 的「多材质子件」支路到底走不走得到

| 字段 | 内容 |
|---|---|
| **还开不开** | 🔴 **仍开着**（结论只能由 Unity 实测钉死） |
| **落点（现读）** | `CardPresentation/Shell/MenuDraw.cs:1235 ClipTmpMesh`；多材质支路 = `:1248 int mi = ch.materialReferenceIndex;` → `:1249-1250 if (mi < 0 \|\| mi >= ti.meshInfo.Length) continue; var mesh = ti.meshInfo[mi];`（⚠️ 现读**已经没有**「`for i in 0..subTextObjects`、`i ≥ 1`」这种硬写循环 —— A 表那句「`i ≥ 1`」描述的是**旧写法**的形状） |
| **判据** | `资料/普查产出_1009/A表核对_块1.md:43`（当时记 `Shell/MenuDraw.cs:803-860`，**已漂到 `:1235+`**）· `资料/普查产出_1010/S1_下一批切块普查.md:44`（同句）· 裁定的邻条 `资料/普查产出_1010/调度台_口径裁定_1011.md:50`（那是 `A268②`） |
| **要改的文件** | 实现 `Shell/MenuDraw.cs`（若真要补断言/补口径）；断言宿主 `Editor/ShellScene.cs`（**当前空闲**）或 `Editor/MainMenuScene.cs`（**⚠️ E8 在写**） |
| **一句话改法** | 造一颗**会产出第 2 份 material** 的 TMP（fallback 字体 / `<sprite>` 富文本），看 `textInfo.meshInfo.Length` 是否 `> 1`：**是** ⇒ 把 `mi ≥ 1` 那条支路补断言；**否** ⇒ 按「静态分支、今天不可达」**如实标注**（铁律 11 例外①要写清理由） |
| **自检覆盖** | `ShellScene.Run`（或 `MainMenuScene.Run`，看断言落在哪个宿主） |

### `A990`（尾巴）—— `QuadRectPx`/`PlaceCell` 读写不互逆 + ~90 个 `ToPixel` 调用点

| 字段 | 内容 |
|---|---|
| **还开不开** | ✅ **已不成立**（①② 都收口） |
| **落点（现读）** | ① `Shell/MenuDraw.cs:553` —— `Vector2 cpx = PixelOfDesign(PosInDesignSpace(q.transform));`，注释里明写「🆕 **2026-10-18（A1004）**：位置项从 `LayoutSpace.ToPixel` 换成 `PixelOfDesign`」。<br>② 三处**命中判定**（原账点名的那三处）**现读全改了**：`Shell/PointerLayer.cs:265`（`MenuDraw.PixelOfDesign(wp)`）· `Shell/ViewportClip.cs:302`（`MenuDraw.PixelOfDesign(MenuDraw.PosInDesignSpace(transform))`）· `Shell/SettingsWindow.cs:2507`（`SetFpsFromPointer` 走 `MenuDraw.PixelOfDesign`） |
| **判据** | `资料/待办判据_第四会话.md §A1004`（该节自陈「找不到一行方案」⇒ **已被 A 表 `A990` 行标 ✅**）· 那一行方案的真身 = `资料/普查产出_1018第三会话/W_ClipQuad与量法.md:85` |
| **要改的文件** | **无** |
| **一句话改法** | —— （销账即可；若要真销，跑一次 `ShellScene.Run` + `SettingsScene.Run` 确认零回归） |
| **自检覆盖** | 销账验证用 `ShellScene.Run` · `SettingsScene.Run` |

### `A992`（尾巴）—— 载荷叠词要不要去重

| 字段 | 内容 |
|---|---|
| **还开不开** | 🔴 **仍开着**：叠词确实在（卡面数据），**「在哪一层去重」这一裁定本身也还没做**（`A` 表把它记成「另一笔」） |
| **落点（现读）** | 数据源 = `Unity/数据/游戏数据/cards_engine.json` 的 `ASH44` / `BL51` / `GOF100` / `SW68` 的 `desc`（`[Invulnerable] Invulnerable` 等）；同名成对 = `RuleEngine/Core/CardDef.cs`（52 对里 48 对同名）· `RuleEngine/Core/GivePayload.cs`（55 对里 48 对同名）。⚠️ `GivePayload.cs` 现在是 **`M GivePayload.cs`**（主对话刚落 `A1105`）—— 切块时注意 |
| **判据** | `资料/普查产出_第四会话/现核_引擎与战斗族.md` 第 5 条（含「**去重不改行为**：`GivePayload.cs` 的 `low.StartsWith(pair[0])` 命中即 `return true` ⇒ 第 2 个同名词根本不读」） |
| **要改的文件** | 解析层 `RuleEngine/Core/GivePayload.cs` **或** 载荷串层 `RuleEngine/Core/CardDef.cs` **（二选一，先裁）** |
| **一句话改法** | 先裁「在哪一层去重」（⛔ 别两处都写）；然后只改那一层 + 补一条断言（`"invulnerable invulnerable"` 与 `"invulnerable"` 解析结果逐字相同） |
| **自检覆盖** | `RuleEngineTest.Run`（动手引擎 ⇒ **必跑**） |

### `A1004` —— `A990①` 的「一行方案判据」在仓库里找不到

| 字段 | 内容 |
|---|---|
| **还开不开** | ✅ **已不成立**（已找到 + 已可执行化） |
| **落点（现读）** | `Shell/MenuDraw.cs:553`（`PixelOfDesign(...)`，带「🆕 2026-10-18（A1004）」订正痕） |
| **判据** | `资料/待办判据_第四会话.md §A1004`（该节写「找不到」）+ `资料/普查产出_1018第三会话/W_ClipQuad与量法.md:85`（真身） |
| **要改的文件** | **无** |
| **一句话改法** | 销账 |
| **自检覆盖** | 无（销账验证见 `A990`） |

### `A1010` —— 行号漂 + 一处「顺手发现」疑似没落盘

| 字段 | 内容 |
|---|---|
| **还开不开** | 🔴 ① **仍开着**（⚠️ **它是纪律、不是待办**：那道闸是**一处真偏离**、已如实写进注释、⛔ 别删）；② **仍开着** |
| **落点（现读）** | ① `minTimeBeforeSkip`：doc = `CardPresentation/Battle/BattleDriver.cs:9219-9221`（「**原版那颗钮没有这道闸** …⛔ 本轮不删」）· 实现 = `Battle/TutorialOverlay.cs:161 MinTimeBeforeSkip = 1.0f` + `:535-542`（按早了不算）· 断言 = `Editor/BattleScene.cs` 的「★ 还没到跳过的时候」那一条（⚠️ **E5 在写这个文件、行号当场在漂**；按**符号名/日志原句**找）。<br>② `_numericKeywords`：`RuleEngine/Core/CardDef.cs:605-607` 仍写着「⚠️ **不动 `_numericKeywords`** … 两边的口径要不要对齐，**见本轮交件报告的「顺手发现」**」；字段 `:80` / 填充 `:109` / 读口 `:1837`。⚠️ `CardDef.cs` 现在 **`M`**（有写手） |
| **判据** | `资料/待办判据_第四会话.md §A1010`（① 表上写的 `Editor/BattleScene.cs:14844` **已漂**；② 见 `现核_引擎与战斗族.md` 第 10 条 + 顺手发现 4） |
| **要改的文件** | ② 若裁「对齐」⇒ `RuleEngine/Core/CardDef.cs` + 卡面角标消费面 `CardPresentation/Core/Badges.cs:310`；① **不改任何文件**（只订正引用方式） |
| **一句话改法** | ② 先做**一次并排比**（`Companion` 那条登记进 `_numericKeywords` 会不会多/少画角标）→ 裁「对不对齐」→ 写落盘。① 把 A 表那一行的落点从行号**改成符号名** |
| **自检覆盖** | ② 卡面族 ⇒ `CardBaseDemo.Run`；① 无 |

### `A1011` —— 卡背 `m_Rect` 恒 `707×1020` ⇒ 冲击 `A994③` 的口径

| 字段 | 内容 |
|---|---|
| **还开不开** | 🔴 **仍开着 —— 而且本轮量出硬数，两说【确实不等价】**（见下「本轮现核数据」）。**必须「先裁比例取哪个矩形」再动手** |
| **落点（现读）** | 消费面三处：`Shell/CollectionWindow.cs:1128` 与 `:1156`（`keepAspect` 从 `false` 改成 `true`，带 `A994③` 订正痕）· `Core/DraggableController.cs:319` 与 `:348`（`CosmeticPreview.PreserveAspectSize`）· `Deck/DeckRuntime.cs` 的**卡背格**与 **`Cosmetic Drawer`** 两句 `SetAspect(...)`（`W_卡面版面` §4·4 明列「白名单外、请派工」） |
| **判据** | `资料/待办判据_第四会话.md §A1011`（含「`A848` 的量法已定」那半）· `资料/普查产出_1018第三会话/W_卡面版面.md` §4·1/§4·2/§4·4 · **本轮我现读的两组硬数**（下表） |
| **要改的文件** | `Shell/CollectionWindow.cs` · `Core/DraggableController.cs` · `Deck/DeckRuntime.cs`（+ `A848` 那条的 `Unity/工具/import_original_art.py`） |
| **一句话改法** | 先裁死「`m_PreserveAspect` 比的是**哪个矩形**」：uGUI 源码**只认 `activeSprite.rect`（= 原版 `m_Rect` = 707×1020，恒定）** ⇒ 若照它做，**我们那 233 张按 `textureRect` 裁出来的 PNG 的长宽比就是错的输入**，要么改内接算式（用原版 `m_Rect` 那个恒定 0.6931）、要么改 `write_cardback` 的裁法；裁完再改上面三处 + 补断言 |
| **自检覆盖** | `CollectionScene.Run` · `DeckScene.Run` · `CardBaseDemo.Run`（⚠️ 动了卡背还**必须看渲染图**） |

**🔴 本轮现核的两组硬数（新增，可复查）**

| | 值 | 出处（本轮亲读） |
|---|---|---|
| 原版 `Sprite/*_Main.json` 的 `m_Rect` | **233/233 = 707×1020**（比例 **0.6931** 恒定；pivot .5/.5） | `d:/2/新解包资源/assets_full/bundle_cosmeticscardbacksimages_assets_all/Sprite/*_Main.json` |
| 原版 `m_RD.textureRect` | **逐张不同**（例：`Cold Blood` 706.92×995.90 → 0.7098 · `Drop and Secure` 684.85×946.88 → 0.7233） | 同上 |
| **我们导进工程的 PNG**（= 按 `textureRect` 裁的） | **233 张，比例区间 0.6188 ~ 0.7652**（最小 `Cardback_SAU_Imotekh_AA_HB.png` 620×1002；最大 `Cardback_All_Premium4.png` 707×924） | `MyGame/Assets/CardPresentation/Resources/Art/cardbacks/*.png`（读 PNG 头） |
| ⇒ **两说之差** | **最大 ≈ ±10%** 的绘制尺寸 | 0.7652 / 0.6931 = **1.104** · 0.6931 / 0.6188 = **1.120** |
| uGUI 真取的矩形 | `activeSprite.rect`（= `m_Rect`） | `MyGame/Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Image.cs:1089-1090`、`:1096-1099` |

### `A1041` —— `A964` 的 `E3`（army 那 11 对真重叠）

| 字段 | 内容 |
|---|---|
| **还开不开** | 🟡 **主体已不成立**：① army 命中区**已按真值改**（`1667ff6` 落地）② 断言 **C（灭自证）已在**（`Editor/ShellScene.cs:5481`/`:5500`/`:5515` 三态 + `:5540` 关窗钮）。**A/B/D 三条（E3 白名单封闭/不腐烂 + 判别式夹具）【未被采纳】—— 依据是 `A964 续 ②` 的裁定「E3 探针改【报告式】、不做普适断言」**（`资料/待办判据_1018.md:421-441`） |
| **落点（现读）** | 实现 = `Shell/LeaderboardWindow.cs:575` `MenuDraw.Hit(node, "Hit", MenuDraw.PaddedRect(iconR, ArmyIconPad), QHit, …)`（`ArmyIconPad` 定义 `:188` = `(-11)⁴`；`ArmyBtnSpacing` **仍是 `-14f`（`:177`）**，⛔ 一字未改 ✔）<br>断言 = `Editor/ShellScene.cs:5473-5521`（`A768① 条目级` 态一/态二/态三）+ `:5519-5560`（`A1065③` 关窗钮 `96.86×98.13` + `A1058` 换图层）<br>探针 = `Editor/ShellScene.cs:5725` 起，段头明写「**报告式**：E1/E3 只列候选 + 落点/实绘证据、**不做普适断言**」 |
| **判据** | `资料/待办判据_第四会话.md §A1041` · `资料/待办判据_1018.md §A964 续 ②` · `资料/普查产出_第四会话/瘦身盘点_29条那一块.md:657`（明写「改数字 115.36→137.36」） |
| **要改的文件** | **无（代码）**。若要补 A/B/D 三条 ⇒ 只动 `Editor/ShellScene.cs`（**当前空闲**） |
| **一句话改法** | 销账时**必须写清**「E3 白名单那三条断言**被 `A964 续 ②` 的『报告式』裁定取代**」——否则下一轮会照 `A1041` 原文去补三条永远补不上的断言 |
| **自检覆盖** | `ShellScene.Run` |

### `A1044` —— `A1003` 收口留下的三件（现读其实是五件）

| 子件 | 还开不开 | 落点（现读） | 一句话 |
|---|---|---|---|
| ① 「第 8 份同形副本」 | 🔴 **仍开着** | `Editor/ShellScene.cs:1199-1207` —— 内联**甲式**循环（`LayoutSpace.ToPixel(q.transform.position)` + `WorldW/H × K`）；注释 `:1165-1170` **自陈**「本处这一份**不在 `A1003` 的收口清单里** ⇒ 如实记一笔」（A 表写的 `:1186-1202` **已漂**） | 收口到 `MenuDraw.UnionQuadRectPx(...)`（或 `MenuDraw.QuadRectPx` 逐块并集），⛔ 别顺手把它当别的副本 |
| ② `MainMenuScene.UnionQuadRect`「完全不过滤」 | 🔴 **仍开着（要裁）** | `Editor/MainMenuScene.cs:11439-11444` → `MenuDraw.UnionQuadRectPx(t, MenuDraw.QuadGate.None, true, …)`；`Shell/MenuDraw.cs` 的 `QuadGate.None` doc 写「完全不过滤 —— `Editor/MainMenuScene.cs` 传 `true` 后**没有闸**，那在那边是**有意**的」 | 「若 Unity 对『自身 inactive』对象回 null ⇒ 这档实际退化成 `Self`」这一疑点**仍未复核** ⇒ 要么裁「就这样」，要么补一次实测 |
| ③ `ShellScene.QuadPxRect` 15 个调用点 | ✅ **已不成立** | `Editor/ShellScene.cs:133-134` 现读 = 一行转调 `MenuDraw.QuadRectPx(q, out …)`（`A1003` 落地） | 销账 |
| ④ `ShellScene.GNodeRect` 零调用点 | ✅ **已不成立**（注释已就地订正） | `Editor/ShellScene.cs:5292 GNodeRect`（定义）+ `:5283-5285` 订正痕；调用点**仍 0** | 销账 |
| ⑤ `DeckScene` 的「逐位同值」措辞 | 🔴 **仍开着**（纯措辞，低优先） | `Editor/DeckScene.cs:1935-1937`：断言 `CheckNear(…, 0.01f)` 而文案写「**逐位同值**」 | 改成「在 0.01px 容差内同值」（`A1003` 之后差 ~7e-5px） |
| **判据** | `资料/待办判据_第四会话.md:294-312`（§A1044 整行） | | |
| **要改的文件** | ① `Editor/ShellScene.cs`（**空闲**）· ② `Shell/MenuDraw.cs` + `Editor/MainMenuScene.cs`（**⚠️ E8 在写**）· ⑤ `Editor/DeckScene.cs`（**⚠️ E8 在写**） | | |
| **自检覆盖** | `ShellScene.Run`（①）· `MainMenuScene.Run`（②）· `DeckScene.Run`（⑤） | | |

### `A1053` —— 【通用族】命中区用「父件整矩形」而不是原版 `m_TargetGraphic` 那颗

| 字段 | 内容 |
|---|---|
| **还开不开** | 🔴 **仍开着**，但**只剩 5 处**，且**五条停手理由本轮逐条复核、条条仍成立**（见下） |
| **已做完的部分** | 第六会话 `命中区批2`：**18/23 + 5 处账外**；`A1058` **15 处**；`A1065③` 两条断言落 `Editor/ShellScene.cs:5519-5560`。**16 颗关窗钮 + 7 处多件并集**（`AllianceMemberOptionsPopup:404` · `GenericOptionsPanel:458` · `SearchingMatchPopup:258` · `LeaderboardRow:208` · `DuelPopupWindow:228` · `LiveOpsEventWindow:514` · `TutorialModePopup:644`）**现读全已是「并集/含 pad」形态** ✔ |
| **判据** | `资料/待办判据_第六会话.md §A1053`（整行原文）· `资料/普查产出_第六会话/W_命中区批2.md` §⑦（逐条停手理由）· 口径三条 = `资料/待办判据_1018.md §A964 续`（并集 + pad 负=外扩 + 结构断言） |
| **要改的文件** | 见下表逐条 |

**剩 5 处（本轮逐处现读）**

| # | 落点（**现读**） | 现读证据 | 停手理由今天成立吗 | 一句话改法 |
|---|---|---|---|---|
| 1 | `Shell/ChatPanel.cs:334` | `MenuDraw.Hit(row, "Hit", rowR, QHit, …)` —— `rowR` 整颗；同一函数 `:328` 画的 `Button Text` = `rowR.y1 − 19.65 → rowR.y1 + 57.14`（**高 76.79**，原版 86.17） | ✅ **成立**：要归真值 **得先改文字几何**（动 `MenuDraw.TextBox` 实参）—— 那是另一笔 | 先改 `Button Text` 高 → 再用 `rowR ∪ textR` |
| 2 | `Shell/LiveOpsEventWindow.cs:941` | `MenuDraw.Hit(root, "HelpHit", r, QHit, …)`（整块 `r`） | ✅ **成立**：普查那个 `132.67` **复现不出**（实读 `88.53²` + pad `(-30)⁴` ⇒ `148.53²`）⇒ **判据没定、不猜** | 先弄清 `0.7357` 挂在哪一层、再定真值 |
| 3 | `Shell/RankedEventWindow.cs:130` | `MenuDraw.Hit(tg, "Hit", new PxRect(TgL, TgT, TgR, TgB), QHit, ToggleRankedMode)`（**裸矩形**） | ✅ **成立**：原版那 659.20 的宽来自 **`RankedText`/`UnrankedText` 是 toggle 的子件**，我们**把那两行字当独立文字画** ⇒ **先改排版归属（结构改动）** | 先让两行字成为 toggle 子件，再谈命中区 |
| 4 | `Shell/SettingsWindow.cs:1835` | `Hit(row, "QualityHit", QualL, QualT, QualBoxR, QualB, QOverlay, CycleQuality, …)`（**裸框**，A 表/普查写的 `:1746` **已漂**） | ✅ **成立**：本窗有 `RootScale 0.9` + **两套转调口**（`:1599 Hit3` 与 `:3154 Hit`）⇒ 哪一档/哪个口没核完 | 先把 pad `(-25,-16.7,-25,-25)` 与 0.9 映射逐项对上，再改 |
| 5 | `Shell/ShopWindow.cs:691` | `MenuDraw.Hit(cell, "InfoHit", Rect(r, CellArtBox), QCellInfoHit, …)`（裸矩形） | ✅ **成立、且按铁律 11 例外①【不做】** —— 本窗那族 `raycast target` 那颗 **`m_RaycastTarget = 0` 且出厂 inactive**，普查那个 `339×389` 取自**另一个 prefab** ⇒ **原版判据不存在** | 维持「不做」，但**要在 A 表里写明理由 + 出处**（现在是散在报告里） |

⚠️ **同族两档「普查没覆盖」仍是账**（`W_命中区批2.md` §⑦(二)）：`AddHit(` **31 处** · 本地 `Hit(...)` 包装 **14 个宿主** ⇒ 那两档**今天没有被任何一个编号收着**（见 §本表可能不全 5）。

| **自检覆盖** | `MainMenuScene.Run`（#1 相邻）· `RewardsScene.Run`（#2）· `ShellScene.Run`（#3/#4）· `SettingsScene.Run`（#4）· `ShopScene.Run`（#5） |
|---|---|

### `A1058` —— 关闭键「悬停换图」换错了层

| 字段 | 内容 |
|---|---|
| **还开不开** | ✅ **已不成立**（第六会话做完：**13 处简报点名 + 2 处账外**，**14 次 pid 反查 0 例外**，8 处写反的注释一并订正） |
| **落点（现读）** | 断言 = `Editor/ShellScene.cs:5550-5562`（`A1058`：`clWb.target.Texture.name == "40k_general_bt_yellow"` **且** `target.gameObject.name == "Background"`，第三行还断 `HoverTexForTest.name`）。<br>实现逐处（现读）＝ `Shell/ChatPanel.cs:306` 段注释已按 pid 订正 · `Shell/SettingsWindow.cs:947-967`（`closeIconQ` 是 target，`A1058` 订正痕在 `:957-963`）· `Shell/LiveOpsEventWindow.cs:514`（第 15 处）· `Shell/ImportDeckPopup.cs`（现读订正「本来就对」） |
| **判据** | `资料/待办判据_第六会话.md §A1058` |
| **要改的文件** | **无** |
| **一句话改法** | 销账 |
| **自检覆盖** | `ShellScene.Run`（已有断言） |

### `A1066` —— 全仓命中区普查结果

| 字段 | 内容 |
|---|---|
| **还开不开** | 🟡 **大部分已收口**：普查点名「**要改 25 处**」，本轮逐处现读 ⇒ **20 处已改 / 5 处 = 就是 `A1053` 那 5 处停手**（见 §A1053 表）。另「**不用改 44**」「**判不了 10**（全在设置窗与奖杯窗）」两档**没有任何编号收着** |
| **落点（现读）** | 普查正本 = `资料/普查产出_第四会话/普查_全仓命中区与关闭键族.md`（§① 调用点表在 `:44` 起）· 口径三条在 `:13-19` |
| **判据** | 同上 + `资料/待办判据_第四会话.md §A1066`（整行搬出）· `资料/历史/交接_第六会话.md:45` |
| **要改的文件** | 剩下那 5 处 ⇒ 同 `A1053` 表 |
| **一句话改法** | 把「20/25」这个读数写回 A 表；「判不了 10」单开一小账（否则它会随着设置窗改动**静默过期**） |
| **自检覆盖** | 随 `A1053` 那 5 处各自的宿主 |

### `A1067` —— 冻结窗口自检的 5 条红

| 字段 | 内容 |
|---|---|
| **还开不开** | ✅ **已不成立**（已归档：`资料/历史/A表已收口_第四会话.md:61`「✅ 冻结窗口自检的 5 条红全清」） |
| **落点（现读）** | 三处根因全落地：`Shell/MenuDraw.cs` 的 `PaddedRect`（唯一算式，`Shell/LeaderboardWindow.cs:570` 引用它）· `Shell/LeaderboardWindow.cs:188 ArmyIconPad` / `:199 ClosePad` · 断言 `Editor/ShellScene.cs:5481/5500/5515`（军种条三态）+ `:5540`（关窗钮） |
| **判据** | `资料/待办判据_第四会话.md §A1067`（整行原文自陈「已全部定位并修」） |
| **要改的文件** | **无** |
| **一句话改法** | 销账（`A` 表第 392 行还挂在「仍开着的」里 = **记录过期**） |
| **自检覆盖** | 无（已由 `ShellScene.Run`/`CollectionScene.Run`/`SettingsScene.Run` 覆盖） |

### `A1092` —— 非 16:9 真点偏：两处同族站点

| 字段 | 内容 |
|---|---|
| **还开不开** | 🔴 **仍开着**（两处都在，**而且都不许只改一半**） |
| **落点（现读）** | ① `Shell/CampaignTab.cs:569` `Vector2 midPx = LayoutSpace.ToPixel(a + ab * 0.5f);` + `:570` `float halfLen = ab.magnitude * 108f * 0.5f;`（成对，`:571-572` 拿去比 `_vpR` 的**设计 px** 框）。⚠️ **`:583-585` 有一段注释明写「上面那句视口剔除…**不用改**」—— 与 `A1092` 的方向**相反**，切块时要请调度台裁一下（见 §本表可能不全 2）。<br>② `Shell/SettingsWindow.cs:2427` 与 `:2431`（`UpdateFpsDrag()`，定义 `:2413`）仍是 `LayoutSpace.PxX(wp.x)` / `PxY(wp.y)`；**同一扇窗**的「点选」那半 `:2507 SetFpsFromPointer` 已改走 `MenuDraw.PixelOfDesign` ⇒ **同一扇窗里两半不一致**（`:2498-2504` 的 doc 自陈「后两处不在本件白名单…那一半仍是 16:9-only」）。⚠️ A 表写的 `:2401/:2405` **已漂** |
| **判据** | `资料/待办判据_第六会话.md §A1092`（整行原文，含定量：「4:3 下命中区只剩 **75%**、`SettingsWindow` 的 FPS 点选 983 个采样点里 **161 个取错档**、21:9 **114 个**、16:9 **0 个**」）· 判据报告 = `资料/普查产出_第六会话/W_外壳量法线_四笔.md` |
| **要改的文件** | `Shell/CampaignTab.cs` · `Shell/SettingsWindow.cs`（⚠️ 第六会话那个写手**可能还会回来写它**） |
| **一句话改法** | **中心与半宽必须同时换**（`MenuDraw.PixelOfDesign` + 同一帧的 `halfLen`）；② 把 `UpdateFpsDrag` 那两个 `PxX/PxY` 换成 `MenuDraw.PixelOfDesign(...)`，与 `SetFpsFromPointer` 同源 |
| **自检覆盖** | `ShellScene.Run`（`CampaignTab` 在壳里）· `SettingsScene.Run`（设置窗） |

### `A1112` —— `A1053` 那批改过的「7 处多件并集」没有断言兜

| 字段 | 内容 |
|---|---|
| **还开不开** | 🔴 **仍开着 —— 但 E8 正在做**（`资料/普查产出_第八会话/调度台_日志.md` 波 2 · E8 · 独占 `Editor/{MainMenuScene,RewardsScene,DeckScene,SettingsScene}.cs`） |
| **落点（现读）** | 🔴 **`Editor/MainMenuScene.cs` 的工作区 diff（未提交）里已见 4 处新断言**（写成 `2026-10-09（A1112）`）：`+4666` 段（`SkirmishModeEventWindow` 四颗卡组钮，断 `160.49×133.01` + 「⊇ 实绘」）· `+5107` 段（`CancelHit` 并集的 x，断 `720.83/1199.17/478.34`）· `+5805` 段（`LeaderboardRow`，断 `441.18/645.82/204.64`）· `+6007` 段（`border/Hit` 最高那颗，断 `167.11`）· `+9513` 段（`DuelPopupWindow` 两颗模式钮，断 `350×100`）。**`git diff --numstat` 现读 = `169/5`**（⚠️ 同一文件同时有 **E5** 的痕迹 ⇒ 切块时**按文件独占**，⛔ 别再加第三个写手）。 |
| **判据** | `资料/待办判据_第六会话.md`/A 表 `A1112` 行 · 清单 = `资料/普查产出_第六会话/W_命中区批2.md` §⑥-5（逐条给了「改哪个宿主、断什么」）· 口径 = `A964` 的 `E2`「断『**命中区 ⊇ 实绘矩形**』，别只断『建起来了』」 |
| **要改的文件** | `Editor/MainMenuScene.cs`（**E8 在写**）· `Editor/RewardsScene.cs`（**E8**）· `Editor/DeckScene.cs`（**E8**）· `Editor/SettingsScene.cs`（**E8**） |
| **一句话改法** | 照 §⑥-5 的逐条清单补：**关窗钮断命中区尺寸（`96.86×98.13` / `96.37×94.50` / `139.10×139.68`）+ 换图层（`target.Texture.name` + `target.gameObject.name`）**；`Editor/RewardsScene.cs` 那条**节点名已从 `Icon (X)` 变成 `Hit`**（断言要按新名字找）；⛔ `Editor/DeckScene.cs:2512-2518` 断的是 **`DeckRuntime` 那套**、**不受本批影响** |
| **自检覆盖** | `MainMenuScene.Run` · `RewardsScene.Run` · `DeckScene.Run` · `SettingsScene.Run`（四条） |

### `A964①` —— 战斗侧 `E2` 7 行未覆盖

| 字段 | 内容 |
|---|---|
| **还开不开** | ✅ **已不成立**（七行**全有断言**，且都在 `1667ff6` 这个**已提交**的 commit 里 ⇒ 不是「工作区半成品」） |
| **落点（现读）** | 段头 `Debug.Log(P + "--- A964（E2）：命中区覆盖（报告式）---")`；七条分别在：**E2①**（手牌各张）· **E2②**（场上单位，走新口 `BattleDriver.cs:6847 HitUnitSlotForTest`）· **E2③**（设置面板「关闭」）· **E2④**（多卡窗「继续」）· **E2⑤**（我方牌堆，新口 `BattleDriver.cs:6813 MyDeckPlateWorldPos` / `:6821 MyDeckPlateDrawnSize`）· **E2⑥**（技能面板）· **E2⑦**（结束回合，新口 `:6830 EndTurnWorldPos` / `:6838 EndTurnDrawnPx`）。<br>⚠️ **`Editor/BattleScene.cs` 正在被 E5 写**（`git diff --numstat` 现读 `169/5`）⇒ 我这一趟里它的行号**漂了约 +150** ⇒ **按日志原句找、别按行号**。<br>只读口那一族（⑤⑦）带 `🆕 2026-10-18（A964① ⑤ · ⑦）` 注释。 |
| **判据** | `资料/待办判据_1018.md §A964`（判据一句话 + 「量 quad 不量节点」+ 模型③裁定）· `资料/待办判据_第四会话.md §A1001 / §A1006` |
| **要改的文件** | **无**（销账） |
| **一句话改法** | 销账；顺带把 `A964` 行里那句「只剩 `A964①` 那 7 行」**改掉**（它已不成立） |
| **自检覆盖** | `BattleScene.Run` |

### `A964②` —— 六处 `ImageQuad.Contains` 的命中区该不该过原版 `m_RaycastPadding`

| 字段 | 内容 |
|---|---|
| **还开不开** | 🔴 **仍开着**（**判据一条都没有**；六处**逐处都在**、**一处都没过 pad**） |
| **落点（现读）** | ① `Battle/BattleDriver.cs:3219-3220`（`_offensiveBtn`，`HandleOffensiveButton`）② `:3642-3643`（`_chatBtn`，`HandleChatPopup`）③ `:4028-4032`（`_cemeteryBtn`，`HandleBattleLog`）④ `:6802-6803`（`HitEndTurn` 里 `_endTurnBg.Contains`）⑤ `:13556`（`HitTip` 体内 `q.Contains(wp)`）⑥ `:13861`（`HitSlot` 体内 `v.Contains(world)`）。<br>`ImageQuad.Contains` 本体 = `Battle/ImageQuad.cs:528-533`（**纯矩形、无 pad 参数**）。<br>⚠️ `Battle/BattleDriver.cs` 现在 = **`M`**（E3 在写，`A1110` 跨局状态泄漏）—— 切块时**这份文件有人占着**。 |
| **判据** | `资料/待办判据_第四会话.md §A1006` 的邻条 + `资料/普查产出_第四会话/现核_引擎与战斗族.md` 第 6b 条（明写「逐个读对应节点的 MB JSON 取 padding（`d:/2` 侧一条都没核）」） |
| **要改的文件** | `Battle/BattleDriver.cs`（六处调用点）· 可能 `Battle/ImageQuad.cs`（若给 `Contains` 加 pad 形参）· 断言宿主 `Editor/BattleScene.cs`（**E5 在写**） |
| **一句话改法** | ① 先去 `d:/2/新解包资源/assets_full/` 逐颗读那六颗对应件的 **`m_RaycastPadding`**（**这是今天缺的那份判据**）② 再决定「过 / 不过」③ 过了就改 `Contains` 的调用侧（⛔ 别把 pad 写死进六个 handler） |
| **自检覆盖** | `BattleScene.Run` |

### `A1120` —— `d6c4111` 留下的两条【口径/量法】过期断言

| 字段 | 内容 |
|---|---|
| **还开不开** | 🟡 **E1 已交回，代码已落工作区（未提交）**；四条红**全判 α（断言侧）**，**实现一字未动** |
| **落点（现读）** | **(a) `Editor/MainMenuScene.cs` 的 `CancelHit` 两条** —— 工作区 diff 里已改成 `CheckNear(by1, 561.23f, …)` / `CheckNear(by2, 652.43f, …)`，并新加三条 x/宽（`720.83 / 1199.17 / 478.34`）；`+5064` 起那段新注释写「🔴 **2026-10-09（A1120）就地订正（铁律 5）**」+ 判据命令 `rcpad.py … "Searching Oponent Popup" --substr "Generic UI Button" --ignore-active` ⇒ 并集 **`478.34×91.20`**、y `560.73…651.93`（+ 本仓 0.5 取帧原点 = `561.23 / 652.43`）。<br>**(b) `Editor/SettingsScene.cs` 的关闭钮两条** —— 已落：`:cmd CheckRectS(FindChild(FindChild(Area(root), "Generic Close Button"), "bg"), …, "可见面 bg（75×75）")` + `:498` 新加一条 `…"Hit"` 断 `1548.31,81.86→1644.68,176.35`；`RectOf` 的 doc `:150-155` 补了「**子树里有没有 `Hit`**」这条纪律；**`:495` 那句自称「这就是 `A1120`(b) 的裁断书」**。<br>⚠️ 两份文件都在 **E8 的独占名单**里（`Editor/MainMenuScene.cs` · `Editor/SettingsScene.cs`），⚠️ 且 `Editor/MainMenuScene.cs` 的 diff 里**同时有 E5/E8 的痕迹**。 |
| **判据** | **诊断全文** = `资料/普查产出_第六会话/诊断_收口自检红.md` · **E1 报告** = `资料/普查产出_第八会话/E1_A1120两条红.md`（含两条原版实读命令与输出）· `资料/交接_第七会话.md:29-34` |
| **要改的文件** | `Editor/MainMenuScene.cs` · `Editor/SettingsScene.cs`（**都已改完、待提交**）；🔴 **`Shell/SettingsWindow.cs`【不需要改】**——E1 已判：`Hit()` 入口第一句就过 `Screen()`（`:3157`/`:1603` 是两套转调口），`closeHitR` 给的是**设计 px** ⇒ 屏上 `×0.9 = 86.73×85.04` 与原版逐位吻合 |
| **一句话改法** | 已改完；**收口时按「断言合计」判绿**（这两处之前的读数 = `MainMenuScene` 2594/2 · `SettingsScene` 736/2） |
| **自检覆盖** | `MainMenuScene.Run` + `SettingsScene.Run`（**两条，就是红的那两个宿主**） |

---

## 二、`A964③` / `A1001` 的旁注（**不在我的清单里，但现核顺手撞到**）

- `A964③`（外壳侧 `E1` 实扫）现读 = `Editor/ShellScene.cs:5725` 起那段探针，段头已按 `A1001` 补了「**建窗表**」（把 22 个「连实例都没有」的窗类逐个建一遍再扫），并且明写「**账面那个 16 还是假数，见下面 `ledger` 那道闸**」。⇒ `A1001` 的覆盖率缺口**已推进过一轮**，但**没有编号收口**（见 §本表可能不全 5）。
- `A964 续 ④` 说「阳性样本改用 `CameraResetButtonHit`」⇒ 现读 `Editor/BattleScene.cs:16603-16607` 三条（实绘内必中 / 3 倍半宽必不中）**已在**。

---

## 三、`§29·b`「二、没有独立编号的零碎」8 条（逐条现核）

| 零碎 | 还开不开 | 落点（现读） | 一句话 |
|---|---|---|---|
| 🔴 **`RankedRewardEventWindow.BuildCard` 那颗 `Debug.Log` 文案过期** | 🔴 **仍开着**（一字未改） | `Shell/RankedRewardEventWindow.cs:579-585`：`if (!_warnedCardIcon) { … Debug.Log("…后者那张图本仓没有 ⇒ **两张都只建节点、不画**") }`；而上面 `:553` **`if (bgTex != null) MenuDraw.Rect(go, bgTex, bgR, "Background", QCardBg);`** 真会画 `Background`；`:579` 的闸**只看 `_warnedCardIcon`、不看 `bgTex`**（`:551` 取 `bgTex`） | 把文案改成「`Army Icon` 只建节点不画；`Background` 有图就画」；闸改成 `if (!_warnedCardIcon && bgTex == null)`（或按实情） |
| **`F3` 行剩的第二条**（`AlignLeft` 之外有没有第二因） | 🔴 **仍开着（要 Unity 量）** | 第一条已答（差值全在 x、`y 差 0.00px`）；第二条**要跑一次 `MainMenuScene.Run` 量**。落点 = `Editor/MainMenuScene.cs` 里 `AlignLeft` 那一族（⚠️ **E8 在写**） | 造一个「两个只有居中/左对齐不同的夹具」把第二因分离出来 |
| **引擎侧零碎** ② `_numericKeywords` | 🔴 **仍开着** | 同 §`A1010`② | 并排比 + 裁口径 |
| **引擎侧零碎** ③ `HandOwnerOf` 对分离实例不登记 | ✅ **已不成立**（「如实标」已标） | `RuleEngine/Core/EffectResolver.cs:2960`（实现）+ `:2925-2955`（doc 已按 `W5` 审查结论文） | 销账 |
| **引擎侧零碎** ④ 同一张卡「手里 + 场上」两份同时听 ⇒ 同回合登记两笔 | 🟡 **判不了（缺夹具）** | 两条链 = `EffectResolver.BroadcastWhen`（`CardDef.cs:1520`/`:1799` 两处明写**只扫棋盘单位**）与 `BroadcastHandWhen`；`grep` 全仓**没有任何一处做过这项比对** | 派一件只读活：造「同卡两份」夹具 + 数登记笔数的口 |
| **表现层零碎** ① `Z5` 的两拍秒数 | 🔴 **仍开着（如实标注、原版判据本地读不到）** | `Battle/BattleDriver.cs:8253` 起 `_tutBeatDelay` 的 doc 逐字写「🔴 **我们挑的**：原版那两条 `WaitForSeconds`…」；字段 `:8250-8251` | 销不了；**留注释**（铁律 11 例外①） |
| **表现层零碎** ② `A939` 一处边界 | 🔴 **仍开着（如实标注、判据读不到）** | `Battle/BattleDriver.cs:8867-8869`（「原版那一支在门开时是 **`ExecuteScriptedTurn` 顶掉 `BasicBattleEndSequence`**（`BattleFinished` 里那是 if/else，不是先后）… 那个 `onDone` 回调**本地读不出指谁**」） | 销不了；**留注释** |
| **`Battle/Effect/*` + `Battle/Tips/*` 剩余** | 🔴 **仍开着**（`Effect/*` 21 条 + `Tips/*` 21 条） | 已落 4 + 3：`Core/Loc.cs:948-951`（`Battle/Effect/Change{MeleeAttack,RangedAttack,Health,Armour}OneTurn`）· `:918`（`DragToTarget`）· `:928`（`UnitNotReady`）· `:706`（`HandFull`）+ `:1046-1050`（`NotEnoughMana`/`NotYourTurn`/`NoTargetAvailable`/`NotEnoughRoom` 四条）· `:698`（`Continue`）等 | 消费面已钉死 = `Battle/CardDisplayWindow.RowsOf`；**另 4 条永久改属性 ⇒ 今天没消费点，⛔ 不加空键** |
| **`minTimeBeforeSkip` 那道闸** | 🟡 **仍开着，但它本身就是「如实标注的真偏离」** | 见 §`A1010`① | ⛔ **别删**（有断言钉着，删要两处一起改） |
| **`CS1573` 一族** | 🟡 **仍开着**（A 表数字 `297 / 17`） | ⚠️ **本波【未复核】**（不敢跑 `WF_DOC=1 typecheck`，怕与在飞写手互相污染）—— 数字沿用 A 表 + `W` 报告。🔴 **同格那条「CS1570 已全仓归零」已被就地订正为不成立**（实测 `10` 条：`BattleEvent.cs:188/189` · `UnitState.cs:76` · `RuleCore.cs:1468`）——**本轮未复核这 4 个文件现读** | 与 CS1570 **是两族**，⛔ 别一起清 |
| **九宫格「active 子块并集」内联副本** | 🟡 **量法那一半已收口 · 并集算法那一半仍开** | 量法 = `Editor/ShellScene.cs` 的 `TmpSpanPx` 是**全仓唯一一份**（`Editor/CollectionScene.cs:288` · `Editor/DeckScene.cs:377` · `Editor/CardBaseDemo.cs:764` · `Editor/MainMenuScene.cs` 多处**都是转调**）；**并集算法**那一半仍有内联副本：`Editor/MainMenuScene.cs:7376 A822SpanX` · `:10759 SpanOf`（两者都是本文件内的私有实现，注释自陈「与 `ShellScene.TmpSpanPx` **逐字同一套算法**」）+ `Editor/ShellScene.cs:1199-1207`（= `A1044`① 那份） | 收到 `MenuDraw` 一层（正向 `A1044`① 一并做） |

---

## 【切块建议】（按「两两文件零交集」）

> 🔴 **先看瓶颈**：**`Editor/BattleScene.cs` 是全局串行瓶颈**（`A828`① · `A268①` 的候选宿主 · `A964②` 的断言宿主 · `A1010`① 的断言 · `F3` 第二条 全落它）——
> **而且此刻 E5 正在写它**（`git diff --numstat` 现读 `169/5`）。⇒ **任何要动 `Editor/BattleScene.cs` 的块都必须排到 E5 交回之后，且一次只排一块。**
> 🔴 **第二个瓶颈**：`Editor/{MainMenuScene,SettingsScene,RewardsScene,DeckScene}.cs` 四个**全在 E8（`A1112`）手里** ⇒ `A1044`②⑤ · `F3` 第二条 都得等它。

| 块 | 含哪些编号 | 独占文件 | 跑哪几条自检 |
|---|---|---|---|
| **块 A · Shell 命中区/量法（最干净、文件全空闲）** | `A1092`（①`CampaignTab` ②`SettingsWindow`）· `A1053` #1/#2/#3/#4（`ChatPanel` / `LiveOpsEventWindow` / `RankedEventWindow` / `SettingsWindow:1835`）· `A1044`①（`ShellScene` 的 `Editor/ShellScene.cs`）· 零碎「九宫格并集副本」的 `ShellScene` 那一份 | `Shell/CampaignTab.cs` · `Shell/ChatPanel.cs` · `Shell/LiveOpsEventWindow.cs` · `Shell/RankedEventWindow.cs` · `Shell/SettingsWindow.cs`⚠️ · `Editor/ShellScene.cs` | `ShellScene.Run` + `SettingsScene.Run`（动了两扇窗的两个宿主） |
| **块 B · 纯文档 / 纯注释订正（0 自检）** | `RankedRewardEventWindow` 那句过期 `Debug.Log`（**这是 `.cs`，算代码、要跑 `ShellScene.Run`**；也可以并入块 A）· `A1010`① 的引用方式（`.md`）· `A1044`⑤ 措辞（`Editor/DeckScene.cs` → **等 E8**）· `A1066`/`A1053`#5 的「不做理由 + 出处」写回 A 表 | `资料/*.md`（**只由主对话写**） | **0 条**（纯 `.md`） |
| **块 C · 卡背比例那一族（要调度台先裁）** | `A1011` + `A848` | `Unity/工具/import_original_art.py` · `Shell/CollectionWindow.cs` · `Core/DraggableController.cs` · `Deck/DeckRuntime.cs` | `CollectionScene.Run` + `DeckScene.Run` + `CardBaseDemo.Run`（**并排渲染图**） |
| **块 D · 引擎侧（零交集）** | `A992`（去重层，待裁）· `A1010`②（`_numericKeywords` 口径） | `RuleEngine/Core/GivePayload.cs`⚠️(主对话刚落 `A1105`) · `RuleEngine/Core/CardDef.cs`⚠️(有写手) · `CardPresentation/Core/Badges.cs` | `RuleEngineTest.Run`（**动引擎必跑**）+ 卡面族 `CardBaseDemo.Run` |
| **块 E · 战斗侧命中区（全落瓶颈文件，必须串行且排在 E5 之后）** | `A828`①④ · `A964②` · `A1010`①（只订正引用）· `F3` 第二条 | `CardPresentation/Editor/BattleScene.cs`（🔴 **E5 在写**）· `Battle/BattleDriver.cs`⚠️（E3 在写）· `WarpforgeVFX/Runtime/WFModuleParticleCollisionNotifier.cs`（只读） | `BattleScene.Run` |
| **块 F · 等 E8 的那一批** | `A1112`（E8 自己做）· `A1044`②⑤ · `F3` 第二条 · `A1120` | `Editor/{MainMenuScene,SettingsScene,RewardsScene,DeckScene}.cs`（🔴 **E8 独占**） | `MainMenuScene.Run` + `RewardsScene.Run` + `DeckScene.Run` + `SettingsScene.Run` |
| **块 G · 只需销账、零代码** | `A990` · `A1004` · `A1041`（主体）· `A1058` · `A1066`（主体）· `A1067` · `A964①` · 引擎侧零碎③ | `项目任务.md` + `资料/历史/` | **0 条**（由主对话写正本） |

**⚠️ 全局串行瓶颈（一份文件同一时刻只能一个写手）**：`Editor/BattleScene.cs`（**E5**）· `Battle/BattleDriver.cs`（**E3**）· `Editor/MainMenuScene.cs` + `Editor/SettingsScene.cs` + `Editor/RewardsScene.cs` + `Editor/DeckScene.cs`（**E8**）· `Shell/{WindowsManager,DeckInfoPopup,PopUpGameWindow,SearchingMatchPopup,SearchingOpponentWindow}.cs`（**E7**）· `Deck/DeckRuntime.cs`（**E7**，且我复核了 `W_命中区批2` §⑤-3 那句「`grep -n closeIconQ Shell/SettingsWindow.cs` 应当命」⇒ **命中了，没被静默盖掉**）· `Core/{Loc,Tooltip}.cs`（**块②**）· `RuleEngine/Core/*`（**眩晕批**）。

---

## 【本表可能不全】（本轮顺手发现，逐条带出处；**找到几条报几条**）

1. 🔴 **`A964①`（战斗侧 `E2` 那 7 行）A 表写着「仍开着」，其实【早已收口】** ——
   `E2①~⑦` 七条断言**全在** `Editor/BattleScene.cs`（段头 = `Debug.Log(P + "--- A964（E2）：命中区覆盖（报告式）---")`），
   只读口 `BattleDriver.cs:6813/6821/6830/6838/6847` 也全在，
   且经 `git log -S "A964/E2⑦"` **现核**：落在 **`1667ff6`（已提交）**、**不在工作区**。
   ⇒ 这与 R1 那句「A 表里写着『仍开着』但其实早收口是本仓常见病」**同族**。

2. 🔴 **`A1067` 同样挂着「仍开着」，但已归档为 ✅** ——
   出处 = `资料/历史/A表已收口_第四会话.md:61`（「✅ 冻结窗口自检的 5 条红全清」）+ `资料/普查产出_第四会话/瘦身盘点_29条那一块.md:651`（处置建议就是「搬去历史」）。
   而 `项目任务.md` §29·b 第 392 行今天仍把它列在「**一、仍开着的**」表里。

3. 🔴 **`A1058` 也挂着「仍开着」，但 `判据文件` 里明写「✅ 已做完」** ——
   `资料/待办判据_第六会话.md §A1058` 自己那一段的末尾就是「✅ **2026-10-18 之后·第六会话：已做完**」，
   且断言现读在 `Editor/ShellScene.cs:5550-5562`。⇒ **`项目任务.md` 第 393 行同样没销。**

4. 🔴 **`A1092` 与 `Shell/CampaignTab.cs:583-585` 的注释【方向相反】—— 需要调度台裁一句** ——
   `A1092` 说 `:569/:570` 要去比 `_vpR` 的**设计 px** 框（`ToPixel` 非 16:9 不对）、**要做**；
   而代码里 `:583-585` 现读写着「⚠️ **上面那句视口剔除（`midPx` 比 `_vpR`）不用改**：两边本来就是设计 px/设计世界坐标
   （`ToPixel(a + ab*0.5f)` ↔ `_vpR` 的设计 px），与这里要修的量纲不是同一处。」
   ⇒ 两者**至少有一处是错的**（`A1091` 那类「`ToPixel` 的 doc 自称是 `FromPixel` 的逆、非 16:9 就是错的」已被订正过 ⇒ 我倾向 `A1092` 对，但**没裁之前别动**）。

5. 🔴 **`A1011` 那句「按我们贴图自己的比例内接 = 按原版 sprite 的比例内接」【不成立】—— 本轮量出来了** ——
   `资料/普查产出_1018第三会话/W_卡面版面.md:113` 的 ⭐ 句说两者等价；本轮亲读：
   原版 `m_Rect` **233/233 = 707×1020（0.6931 恒定）**，而**我们导进工程的 PNG（按 `textureRect` 裁的）比例区间 = 0.6188 ~ 0.7652**
   ⇒ **最大差 1.104 / 1.120 倍**。uGUI 用的是 `activeSprite.rect`（`Image.cs:1089-1090`）。
   ⇒ 那句 ⭐ 只在「**不看尺寸、只看形状**」时近似成立；**一旦要内接尺寸就必须裁死取哪个矩形**。**这是 `A1011` 的真正难点，不是「口径表述」问题。**

6. 🔴 **`A1053` 的「两档没覆盖」【没有任何编号收着】** —— `AddHit(` **31 处** · 本地 `Hit(...)` 包装 **14 个宿主**
   （清单在 `资料/普查产出_第六会话/W_命中区批2.md` §⑦(二)）。`A1053` 的行只写「剩 5 处」，
   `A1066` 的行只写「要改 25 · 不用改 44 · 判不了 10」⇒ **这两档掉在编号之外了**（也是 `A1112` 的邻域）。

7. ⚠️ **`A1120` 的诊断链条里有一处【记录会误导人】** —— `资料/交接_第七会话.md:29-34` 写「⚠️ 两边都没定死：我们那颗 `Hit` = `96.37×94.50`、原版射线区 = `86.73×85.05` ⇒ 要先判『量法错还是实现错』」，
   而 **E1 已判清是 α（断言/量法错）**、`Shell/SettingsWindow.cs` **一个字没动**（`资料/普查产出_第八会话/E1_A1120两条红.md` ①）。
   ⇒ **销账时别照「两边都要修」写**；同时 `调度台_日志.md` 建议的 `A1122`（`Shell/SettingsWindow.cs:964-967` 那句注释要标明「设计 px；屏上 ×0.9」）**确实还开着**（我现读确认那句仍只写 96.37×94.50、没标单位）。

8. ⚠️ **`Editor/BattleScene.cs` 的行号【在本次会话进行中漂了约 +150 行】** ——
   我第一趟 grep 到 `A964（E2）` 段头在第 `16578` 行，几分钟后同一句在 `16728` 行；
   `git diff --numstat` 现读 `169 / 5`（E5 在写）。
   ⇒ **凡是这轮给出的 `Editor/BattleScene.cs` 行号，一律按【符号名 / 日志原句】复核**（这正是 `A1039` 立的规矩）。

9. ⚠️ **`A1041` 的三条白名单断言（A/B/D）【被裁定取代了】，但 `A` 表那一行没写** ——
   依据 = `资料/待办判据_1018.md:421-441`（`A964 续 ②`：E3 探针**只列候选、不做普适断言**，
   「确认下来的才补结构性断言」）。若照 `A1041` 原文去派活，写手会去补三条**永远补不上**的断言。
   ⇒ 销账/改行时**必须写清「被 `A964 续 ②` 取代」**。

10. ⚠️ **`A1053` #5（`ShopWindow:691`）属于「原版判据本就不存在」⇒ 按铁律 11 例外①【不做】，
    但这条理由今天只活在写手报告里**（`W_命中区批2.md` §⑦(一) #5），A 表那一行**没写**。

11. ⚠️ **`A1092` ② 的行号在 A 表里就已经错了两次**：A 表写 `Shell/SettingsWindow.cs:2401/2405`，
    `判据文件` 写 `:2401/:2405`（同值），**本轮现读 = `:2427`（`LayoutSpace.PxX(wp.x)`）与 `:2431`（`IsPressed` 那支）**，
    而 `UpdateFpsDrag()` 的方法头在 `:2413`。⇒ 这一类**按行号派活必撞车**的账，建议统一改成符号名。

12. ℹ️ **`A1044`① 的落点也漂了**：A 表/判据写 `Editor/ShellScene.cs:1186-1202`，**现读 `:1199-1207`**；
    同一份注释（`:1165-1170`）**自己就承认**「本处这一份不在 `A1003` 的收口清单里」⇒ 它是个**已知的孤儿副本**，
    不是「忘了改」。

13. ℹ️ **主对话那一句「`grep -n closeIconQ Shell/SettingsWindow.cs` 应当命」已被复核**：
    **命中**（`:942` 定义 · `:969` 当 target）⇒ `W_命中区批2` §⑤-3 担心的「被另一个写手静默盖掉」**没有发生**。

---

## 🆕 追加（现核途中 E8 交回，2026-10-09 12:17）—— `A1112` 的读数要改

> ⚠️ 上面 §A1112 与 §切块建议里「**E8 正在飞**」的措辞**已过期**：**E8 在我写这份报告的过程中交回了**
> （`资料/普查产出_第八会话/E8_A1112命中区断言.md`，**只动了一个文件** = `Editor/MainMenuScene.cs`，**+226 / −6**）。

| `A1112` 的 7 处 | 现在的状态（E8 自报 + 我复核的文件面） |
|---|---|
| `SearchingMatchPopup:258` | ✅ 补了（`Editor/MainMenuScene.cs` 的 `A1120` 块内，补的是**并集的 x 那半边**） |
| `LeaderboardRow:208` | ✅ 补了（喂 1 行 + 喂 7 行两节） |
| `DuelPopupWindow:228` | ✅ 补了 |
| `LiveOpsEventWindow:514` | ✅ 补了（四颗卡组钮，循环 ⇒ 跑起来 58 条） |
| `TutorialModePopup:639` | ✅ 补了 |
| `AllianceMemberOptionsPopup:404` | ❌ **补不了 —— 它的唯一宿主是 `Editor/ShellScene.cs`，当时在黑名单** |
| `GenericOptionsPanel:458` | ❌ **补不了 —— 同上** |

⇒ **对切块的影响（这条比 E8 报告本身更重要）**：
**剩下那 2 条断言的宿主 `Editor/ShellScene.cs`（+ `Editor/ShopScene.cs`）现在是【空闲】的**，
而**块 A 已经要动 `Editor/ShellScene.cs`**（`A1044`①）⇒ **把这 2 条直接并进块 A 一次做完**，
⛔ 不要再单开一块去抢同一个文件。

⇒ 同时：`Editor/MainMenuScene.cs` 的 `A1112` 部分**已落地**（工作区 `+226/−6`，含 E1 的 `A1120` 那 22/6）
⇒ §切块建议 **块 F** 里「`A1112`（E8 自己做）」那一格**可以划掉**，块 F 只剩 `A1044`②⑤ · `F3` 第二条 · `A1120`（待提交）+ 那 2 条并进块 A 的断言。

⚠️ **另一处文件级变化**（我这一趟开始时还没有）：`Shell/DeckInfoPopup.cs` · `Shell/MessageToast.cs`（新建）· `资料/普查产出_第六会话/W_命中区批2.md`
现在都在工作区里被改过 ⇒ **`Shell/DeckInfoPopup.cs` 已被 E7 占用**（我上面给的 `A1053`/`A1058` 落点表里**没有它**，不受影响，
但 §切块建议的「全局串行瓶颈」一格要把它补进去）。

---

## 附：本报告没做的事（如实交代）

- ⛔ **没跑 Unity**、⛔ **没跑 typecheck**（怕污染在飞写手的读数）⇒ 「今天是否全绿」这一列**本报告一条都没给**，
  只给「代码/判据里今天的形态」。
- ⛔ **没核 `d:/2` 侧那六处 `m_RaycastPadding`**（`A964②` 缺的就是它）—— 那是**下一步的只读活**。
- ⛔ **`CS1573` 的现行条数没复核**（需要 `WF_DOC=1 typecheck`）。
- ⛔ **没动 `A1112`**（E8 在飞）—— 只读出了「工作区里已见 4 处新断言」这一事实。
