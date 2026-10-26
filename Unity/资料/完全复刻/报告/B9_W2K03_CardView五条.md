# B9 · W2K03 · `Core/CardView.cs` 五条（HUD:108 · HUD:153 · HUD:148 · HUD:189 · 卡组:148）

> 执行型写手 **W2K03** · 2026-10-26 · 独占文件 = `CardPresentation/Core/CardView.cs`（**只这一份 .cs**）
> 派活单 = `判据/B9_P0剩余C_派活包.md` §2 · K-03 · 逐条判据 = `判据/B9_P0剩余C_施工单.tsv`
> 账补丁 = `账/_parts/B9_W2K03.tsv`（11 列 × 4 行，表头原样；`(原版类, 方法)` 四条键各命中主表**唯一 1 行**，见 §4 校验）
> ⛔ 本代理**没跑 Unity**、**没动 git**、**没改两张正本**、**没改主账表**、没碰白名单外文件。

---

## §一 结论（四句话）

1. **HUD:108 → A**：传说档溶解材质做完了。判据 = `HasToUseLegendarySummonEffect()` = **非伏击 && 稀有度 4**；两份原版材质**只差 `_BorderColor` / `_BorderColor2` 两项颜色**（其余 25 项逐位相同，已逐项比过两份材质 JSON）⇒ 我们同一份实例按档写那两项颜色 + 换材质名（`Card 3d WH40K Dissolve` / `Card 3d WH40K Dissolve Legendary`）。
2. **HUD:153 → A**：残骸态「露出原卡 3D 卡身」做完了 —— 三条件 `show && isRemnant && 阵营 ∈ {SaimHann, Sautekh}` 收口一处；原版那 4 个调用点在我们这侧**早就收口在 `CardHighlight.CardBoardHighlight.ShowOriginalRemnantCard` 这个回调**上，本包把这个回调**接上**并给了实现（**没有改 `CardHighlight.cs`**）。
3. **HUD:148 → 仍 C（写口已就位）**：卡面「只重设描述那颗 TMP」的写口 `SetDescriptionSuffix` 做完了（`Data.keywords + "<i>" + 后缀 + "</i>"`，走 `BuildTextLayers` 重拟合；`null` = 复原）。**剩下的是抬牌那一刻的调用点 + 目标池**（`Hand/CardInteraction.cs` = K-04 的文件）⇒ 已写成改动请求（§三·①）。
4. **HUD:189 → 仍 C（卡面那一半已就位）**：`RefreshLocalizedText()` 做完了（按当前语档重取卡名/描述 + 重排兵种行/阵营行/手牌文字态，**不碰数值**）。**剩下的是广播订户**（`Battle/BattleDriver.cs` 的 `HookHudLanguageChanged` 扩到卡视图 = K-01）⇒ 改动请求（§三·②）。
5. **卡组:148 → 不施工（等 K-07）**：详情窗那一半在 `Shell/PracticeModePopup.cs`（K-07）；`Core/CardView.cs` 这一侧**没有任何依赖**，本包不碰、账不新增行（避免把主表那行的证据覆盖成空）。

---

## §二 逐条证据（判据 → 落点 → 观测量）

### 2·1 HUD:108 `BattleCardUI.ChooseDissolveMaterial()`

| 项 | 内容 |
|---|---|
| 原版方法体 | `d:/2/tools/decomp_full/BattleCardUI__ChooseDissolveMaterial.c`（**全文 16 行，亲读**）：`CardScript__HasToUseLegendarySummonEffect(cardScript)` 为真 ⇒ `return *(this + 0x198)`；否则 `return *(this + 0x400)` |
| 判据那一半 | `d:/2/tools/decomp_full/CardScript__HasToUseLegendarySummonEffect.c`（**全文 15 行**）：`if (EntityScript__HasCurrentTrait(card, 0x4b0, 0) == 0) return EntityScript__get_cardRarity(card) == 4; else return false;` ⇒ **不挂伏击词条 && 稀有度 == 4** |
| `4` 是什么 | `d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardRarity.cs`：`None=0 / Common=1 / Rare=2 / Epic=3 / **Legendary=4** / Special=5` |
| 两份材质 | `bundle_battleprefabs_vfxandmisc_assets_all/Material/Material_-6651017123104198842.json`（`m_Name = Card 3d WH40K Dissolve`）与 `…/Material_-1409023286357856989.json`（`m_Name = **Card 3d WH40K Dissolve Legendary**`）：**同一个 shader `m_PathID = −8335403570401031012`**；`m_Floats` 逐项相同（含 `_BorderWidth 0.23` / `_CountersIntensity 2.0` / `_AlphaClipThreshold 0.145` / `_DISSOLVE_CHANNEL 2` / `_DissolveUVScale (3,3,0,0)` / `_ClampRange`）⇒ **唯一差别** = 那两项颜色 |
| 两项颜色的原值 | 传说 `_BorderColor` = **(2.6390157, 2.4975879, 0.7184754, 0)** · `_BorderColor2` = **(0.5764706, 0.1012588, 0, 0)**（金 / 橙红）；普通 `_BorderColor` = (0.714087, 1.91458, 2.639016, 0) · `_BorderColor2` = (0, 0.028592, 0.576471, 0)（蓝） |
| 我们的落点 | `CardView.cs:14571（BeginDissolveMaterial）` + `:14558（ApplyDissolveVariant）` + `:14550（HasToUseLegendarySummonEffect）`；调用点 `:6986（BeginDissolve）`；建材质那一跳 `:6943` 同一行追加 `ApplyDissolveVariant(_dissolveMat)`（⇒ `DissolveMatView` 读到的名字/颜色与真挂上去的那份一致） |
| 观测量 | 传说卡 `BeginDissolve()` 后 `MeshRenderer.sharedMaterial.name` **含 `Legendary`**；**非传说**与**面朝下（伏击）**两条反例都必须落到普通那份（`Card 3d WH40K Dissolve`） |
| 生产路径 | `Core/CardFeel.cs:1881`（HandToBoard/ToBoard）· `:2120`（销毁溶散）—— 传说卡真会走到，不是死路 |

### 2·2 HUD:153 `BattleCardUI.ShowOriginalRemnantCard(bool show)`

| 项 | 内容 |
|---|---|
| 原版方法体 | `decomp_full/BattleCardUI__ShowOriginalRemnantCard.c`：`body3D.gameObject.SetActive(show && SupportMethods__ShouldShowOriginalRemnantCard(cardScript))` |
| 三条件 | `decomp_full/SupportMethods__ShouldShowOriginalRemnantCard.c` + ILSpy `SupportMethods.cs:1234-1259`：`cardArmy == **Sautekh** && isRemnant ⇒ true`；`cardArmy != **SaimHann** ⇒ false`；否则 `return isRemnant` ⇒ 等价于 **`isRemnant && cardArmy ∈ {Sautekh(40), SaimHann(30)}`**（枚举值出自 `Assembly-CSharp/CardArmy.cs`；这两族正是**有残骸体**的灵族 / 太空死灵） |
| 我们这侧的 `isRemnant` | 原版是实体级 `isRemnant(+0x65)`；我们的等价物 = **残骸体已建且可见**（`RemnantBodyVisible`）—— 与 `BattleDriver.BoardTouchInput` 里既有的 `h.IsRemnant = v.RemnantBodyVisible` 是**同一份映射**（不另立第二条判据） |
| 调用点（原版 4 处） | `CardHighlight__ToggleCombatPreviewHighlight.c:126`（开）/`:149`（收）· `CardScript__TryCardHighlight.c:45`（悬停）· `CardScript._UnitDeath…MoveNext.c:351`。我们这侧前三条**已全部走 `CardHighlight.CardBoardHighlight.ShowOriginalRemnantCard` 回调**（声明 `CardHighlight.cs:501`；调用 `:642` / `:662` / `:698`），第 4 条（单位死亡）在原始实现里也是 `ShowOriginalRemnantCard(false)`，而我们的死亡/离场路径走 `SetRemnantBody(false)` → `HideRemnantBodyNow()`（同一效果，见下） |
| 我们的落点 | `CardView.cs:14607（ShowOriginalRemnantCard）`；回调接线 `CardView.cs:14410`（`BoardHighlight` getter 里一次性挂上）；`SetRemnantBody` 那一句 `:2646/:2647` 加 `&& !_originalRemnantCardShown`；`HideRemnantBodyNow` `:2758` 清标志 |
| 观测量 | 灵族 / 太空死灵残骸悬停 ⇒ `body3D.activeSelf == true`；其它阵营残骸、非残骸 ⇒ `false`（四格都读）；`OriginalRemnantCardShown` 读口（`:14626`） |
| 两处开关不互相覆盖 | `BattleDriver.SyncBoard` 每次都对残骸调 `SetRemnantBody(true, …)`（它末尾会关卡身）⇒ 那句改成「**看见 `_originalRemnantCardShown` 就跳过**」，否则悬停刚亮出来的原卡会被下一次棋盘刷新**静默关回去** |

### 2·3 HUD:148 `BattleCardUI.ShowAvailableTargets()`（只做描述写口）

| 项 | 内容 |
|---|---|
| 原版末句 | ILSpy `BattleCardUI.cs:1342`：`string description = text2 + "<i>" + text + "</i>";  SetDescription(description);` ⇒ **无空格 + 富文本斜体** |
| `text` 两档 | ① `maxTargets == 1 && targetsAvailable.Count > 0` ⇒ `SupportMethods.GetTargetAvailableText(target)`，其实现体（`SupportMethods.cs:1494-1502`）**就是** `"(" + target + ")"`；对面那张是 `SpellType.Trap` ⇒ 换 `Card_Trait/trap`、`SpellType.SecretOrder` ⇒ `Card_Trait/secretOrder`；② 否则 `GetTargetsAvailableText(n)` = `Battle/HUD/TargetsAvailable` 把 `{0}` 换成 n（我们已有同一个口 `CardText.TargetsAvailable(n)`，⛔ 没另拼文案） |
| `richText` | `bundle_staticgeneralassets_assets_all/GameObject/DescTextUnit.json` → `MonoBehaviour_1130198714162661774.json` 里 **`m_isRichText = 1`**（施工单 ④ 要求「落地前确认」——已确认） |
| 我们的落点 | `CardView.cs:14658（SetDescriptionSuffix）` + `:14673（DescTextWithSuffix）`；拼进文本的两处 `:5006`（初始态）/ `:5247`（简化态） |
| 幂等 / 拟合 | 后缀存 `_descSuffix`，**每次重建都从 `Data.keywords` 现拼**（不是往 TMP 上叠字符串）；重建走 `BuildTextLayers`（唯一的排版/拟合口）⇒ 长出来的那一段**照样被 FitToBox 收进原版那个矩形**；`SetData`（战场每次刷新）也会自动带上当前后缀 |
| 观测量 | 抬牌后 `KeywordsText.text` 尾部 = `Data.keywords + "<i>(名字)</i>"` 或 `…"<i>{n} available</i>"`；`SetDescriptionSuffix(null)` 后 `KeywordsText.text == Data.keywords`；`DescriptionSuffix` 读口 |
| ⛔ 与技能面板不同口径 | `ActiveSkillDisplay.SetupActiveSkill` 那条是 `desc + " " + 后缀`（空格 + 纯文本）⇒ **没有抽共用函数**（账里既有提醒照办） |

### 2·4 HUD:189 `BattleHud.BroadcastChangedLanguage()`（只做卡面重排字那一半）

| 项 | 内容 |
|---|---|
| 原版实现者 | ILSpy `BattleCardUI.cs:992-1008`：`SetCardTextsExcludingStats(rawCardData)` → 若 `cardState == inHand` 再 `SetCardTextInHand(true)`；`BasicCardUI.cs:465-488` 同形（`.c` 两份对照一致） |
| 四颗 TMP 是什么 | `BasicCardUI.SetCardTextsExcludingStats`：`GetLocalizedCardName()` / `GetLocalizedCardDescWithLineBreak()` / `GameStaticData.CardRaceToString(card)` / `ArmyToLocalizedString(cardArmy)` ⇒ **卡名 / 描述 / 兵种行 / 阵营行**；方法名里的 `ExcludingStats` 就是「⛔ 不动数值」 |
| 我们的落点 | `CardView.cs:14706（RefreshLocalizedText）` + `:14737（LocalizationSourceDef）`：① 卡名 ← `CardText.Name(CardDef)`；② 描述 ← `BattleDriver.FaceTextFull(CardDef).body`（**同一个口**，⛔ 没另写一份文案）；③④ + 手牌文字态 ← 重走 `BuildTextLayers(Data)`（兵种行走 `SubtypeLine → RaceTerm → Loc`、阵营行走 `CardText.Faction`，两条本来就随语档） |
| 观测量 | 设置窗切语言后：卡面 `TitleText.text` / `KeywordsText.text` 变、**数值四件（攻/血/甲/费）逐字节不变**；日志那一半此前已由 `BattleDriver.RefreshBattleLog` 接上 |
| 源卡怎么来 | 本类手上只有 `CardData`（`title`/`keywords` 是**建卡那一刻**算好的串）⇒ 按 `Data.id`（= 原版英文卡名，`ToCardData` 的 `id = u.Name`）+ 阵营查 `CardDatabase`，命中后**缓存在 `_locSource`**（同一张卡反复切语言只查一次） |
| 副产物（顺手对齐原版） | 原版这条路写的是**裸描述** ⇒ 这里把「可用目标」后缀一起清掉（`_descSuffix = null`），与 `SetDescriptionSuffix` 同一条状态 |

### 2·5 卡组:148 `DeckGeneralInfoDemo.WarlordInfoButtonClick()`

- 判据：`out_cpp2il_dev_ilspy/DeckGeneralInfoDemo.cs:417-427` + `decomp_full/DeckGeneralInfoDemo__WarlordInfoButtonClick.c`：`CardDisplayWindow.Instance.ShowCard(deckHero, tier: null, showOptions: false)`。
- **本包不施工**：整条落点在 `Shell/PracticeModePopup.cs`（督军名那颗 Label 加命中区）+ `Shell/CardDetailPopup.cs`（只读接口）= **K-07 的独占文件**。`Core/CardView.cs` 这一侧**没有任何依赖**（详情窗自己建卡视图）。
- ⇒ 账**不新增/不改**这一行（补丁只做「有实质变化」的四行，避免把主表那行已有的证据覆盖掉）：**等 K-07**。

---

## §三 没查清 / 边界（如实记，⛔ 不拿猜测填空）

1. **HUD:148 的两步调用点不在本包**（`Hand/CardInteraction.cs` = K-04）：
   ① 抬牌进入支调 `cardView.SetDescriptionSuffix(那段)`、放回支调 `SetDescriptionSuffix(null)`；
   ② 「那段」的**内容**按原版两档拼（单目标 `"(" + 名字 + ")"`，其中对面 Trap / SecretOrder 战术卡换成 `Card_Trait/trap` / `Card_Trait/secretOrder`；否则 `CardText.TargetsAvailable(n)`）；
   ③ 目标池直接用引擎现成的 `RuleCore.PlayTargetPool(ctx, me, def)`（⛔ 别在表现层另写一份「谁能点」）。
2. **HUD:189 的广播订户不在本包**（`Battle/BattleDriver.cs` = K-01）：`HookHudLanguageChanged` 里除 `RefreshBattleLog` 外，再对**在册卡视图**逐张调 `view.RefreshLocalizedText()`（幂等：先摘再挂，`DetachStaticHooks` 里摘掉）。⚠️ 建议用 `Object.FindObjectsByType<CardView>(FindObjectsInactive.Include, …)` 现扫，⛔ 别自己维护一张易漏的注册表（`CardView.RefreshAllHandTextStates` 已经是这个写法）。
3. **`_faceDown` ≙ 原版 `HasCurrentTrait(ambush)` 这一步没有独立复核**：本文件里 `_faceDown` 的 doc 写明「原版 = 身上挂着伏击词条」，且 `SetCardImageTo3DBase` 用的就是同一条（喂伏击图）。但**「伏击被移除后 `_faceDown` 是否复位」**取决于宿主（`BattleDriver.SyncBoard` 唯一的 `SetAmbush` 调用点）—— 本代理没跑 Unity，**没有现场验过**。若某张传说卡翻面后 `_faceDown` 仍为 true，它会用普通溶解材质（原版此时是传说那份）。**建议**：`BattleScene` 自检加一条「`SetAmbush(true,false)` → `BeginDissolve` ⇒ 普通；`SetAmbush(false,false)` → 再 `BeginDissolve` ⇒ 传说」。
4. **自设计 26 张 / 自检夹具的「描述」在切语言后不会变**（`RefreshLocalizedText` 的 🔶 分支）：它们的描述没有官方词条、也不在 `cards_engine.json`，`CardData.keywords` 又是建卡时算好的串 ⇒ 只能保持原样（卡名那一档仍会按 `Card_Name/<名字>` 词条变）。**这是如实记的边界，不是漏做**。
5. **源卡解析的口径（已按「不出噪声」定死）**：`LocalizationSourceDef` 先按「同名 + 同阵营」扫池，找不到再退「只按名字」；**没有**用 `CardDatabase.Find(pool,name,faction)` 那个三参重载 —— 它**每次未命中都出声**（自设计 26 张 / 自检夹具会各喊一次），而「查不到 ⇒ 保持原样」是这条链的**正常分支**，不该报警。⇒ 语言切换时**零新增告警**。若将来要「查不到也出声」，正确做法不是在卡面这层喊，而是在 `ToCardData` 里把源卡带进视图（见 §五·④）。
6. **HUD:153 的「单位死亡」那条调用点**（`CardScript._UnitDeath…MoveNext.c:351` = `ShowOriginalRemnantCard(false)`）我们没有逐条对照实现，而是由 `SetRemnantBody(false) → HideRemnantBodyNow()` 覆盖（它按形态恢复卡身）。语义相同，但**不是同一个函数**，如实记。
7. **原版描述取的是 `GetLocalizedCardDescWithLineBreak()`（带换行那一版）**，我们 `Data.keywords` 里没有那条换行（这是**既存**差异，与本条无关）⇒ 后缀接在尾部视觉位置一致，但整段换行规则仍然不同。⛔ 只报不改（属 `FaceTextFull` 那条链）。
8. **`typecheck` = 0/0（最终拿到）**：本代理一共跑了 6 次 —— 前 5 次挡住的是**别的代理正在写的文件**（依次 `Core/CostCurveDrawer.cs` → `RuleEngine/Core/TargetCriteriaEval.cs` → `Shell/LiveOpsEvent.cs` → `RuleEngine/Core/EffectResolver.cs` + `Battle/CardInteraction` 族 → `Shell/LiveOpsFilter.cs` → `Battle/BattleDriver.cs`），**`CardView.cs` 一次错都没有**（按铁律 13·3 第 2 条当作「不是我的问题」并记下读数不可信）；第 6 次全绿：`运行时错误数: 0` / `编辑器错误数: 0`。
   **`ruleprobe.sh play check` 读数 = `16db33fa634ab9d958b968688def69ff`**（= 本包简报里给的那条基线 `16db33fa…`，逐字节相同；①-a / ①-b 两段逐卡差异为空）。rc=1 是**已知 P-06**（基线文件那条哨兵 `af8e8178…` 是「源码常量」的另一条闸，不是结算指纹），按简报「别追」。⚠️ 本包**没动任何引擎文件**（只在 `CardPresentation/Core/CardView.cs`），所以指纹不可能变 —— 这条读数是**回归确认**，不是新行为。

---

## §四 改动清单（净行数）

| 文件 | 改动 | 净行数 |
|---|---|---|
| `CardPresentation/Core/CardView.cs` | 类尾新增一节（HUD:108/153/148/189 四条的常量 · 读口 · 写口）：`DissolveMaterialNameNormal/Legendary` · `DissolveBorderColor(Legendary)` · `DissolveBorderColor2Legendary` · `HasToUseLegendarySummonEffect` · `ApplyDissolveVariant` · `BeginDissolveMaterial` · `HasOriginalRemnantCardArmy` · `ShowOriginalRemnantCard` · `OriginalRemnantCardShown` · `_originalRemnantCardShown` · `DescriptionSuffix` · `_descSuffix` · `SetDescriptionSuffix` · `DescTextWithSuffix` · `RefreshLocalizedText` · `LocalizationSourceDef` · `_locSource` | **+245**（全部追加在类尾 `:14509-14753`，**类里其余行一个都没挪**：现读 `lf=14755`、改前 `14510`、`14755−14510=245`） |
| 同上 · 就地改 6 行（**净行数 0**，零漂移） | ① `:6943` 建材质那一跳追加 `ApplyDissolveVariant(_dissolveMat)`；② `:6986` 改调 `BeginDissolveMaterial()`；③④ `:2646/:2647` 加 `&& !_originalRemnantCardShown`（注释并在原来那一行里）；⑤ `:2758` 清 `_originalRemnantCardShown`；⑥ `:5006` / `:5247` 描述文本改走 `DescTextWithSuffix(d.keywords)`；⑦ `:14410` `BoardHighlight` getter 里挂上 `ShowOriginalRemnantCard` 回调 | **0** |
| `账/_parts/B9_W2K03.tsv` | 新建（11 列表头 + 4 行：HUD:108 **A** · HUD:153 **A** · HUD:148 **C（写口就位）** · HUD:189 **C（卡面半就位）**） | 新文件 |
| `报告/B9_W2K03_CardView五条.md` | 本文件 | 新文件 |

**校验（read-only，都跑过）**：
- `python -I -X utf8 工具/check_ledger_points.py 资料/完全复刻/账/卡牌面_渲染.tsv` ⇒ **A=114 / 通过 114 / 漂移 0 / 失败 0**（这一份账里有大量 `CardView.cs:<行号>` 落点 ⇒ **我的改动零漂移**）。
- `… 账/战斗界面_HUD.tsv` ⇒ **失败 0**（117 条漂移全在 `BattleDriver.cs` —— 别的代理在建，与本包无关）。
- `python -I -X utf8 工具/check_patch_conflicts.py` ⇒ 4 份补丁**无跨补丁重复键**。
- 补丁 4 条键 `(BattleCardUI, ChooseDissolveMaterial())` / `(BattleCardUI, ShowOriginalRemnantCard(bool show))` / `(BattleCardUI, ShowAvailableTargets())` / `(BattleHud, BroadcastChangedLanguage())` 在主表里**各命中唯一 1 行**（108 / 153 / 148 / 189），不会新增行、不会歧义。
- `TMPDIR=/tmp/wf_W2K03 bash 工具/typecheck.sh` ⇒ **0 / 0**（第 6 次；前 5 次挡住的全是别的代理正在写的文件，详见 §三·8）。
- `bash 工具/ruleprobe.sh play check` ⇒ 结算指纹 **`16db33fa634ab9d958b968688def69ff`**，与简报基线 `16db33fa…` **逐字节相同**（①-a / ①-b 逐卡差异为空；rc=1 = 已知 P-06，不追）。
- 行尾：`CardView.cs` 改完仍是**纯 LF**（`crlf=0 / lf=14755 / 裸 CR=0`，与改前一致）；⛔ 全程没用 `sed -i`。

---

## §五 🔴 顺手发现（只报不改）

1. **账文「两份材质资产都在工程里」不成立**（`账/战斗界面_HUD.tsv:108` 的备注末段）：`Assets` 下**没有** `Card 3d WH40K Dissolve(.Legendary)` 这两份 `.mat`（`find Assets -iname "*dissolve*"` 只命中 `WarpforgeVFX/Materials/` 那一批粒子材质）。我们一直是**运行时按 shader 名 + 原版序列化值建实例**。⇒ 已在补丁那一行的备注里就地订正（铁律 5）。**不影响本条判据**（shader 是原件，值逐项可查）。
2. **`DissolveMaterialProto()` 的 proto 名字还是 `"Card3DDissolve"`**（原版资产名是 `Card 3d WH40K Dissolve`）：我只把**每卡实例**的名字按原版设，proto 没动（怕撞到别处对 proto 名的断言）。建议以后一并正名。
3. **`HUD:107`（`BattleCardUI.ChangedLanguage()`，现 A）的落点是 `BuildTextLayers`**：现在有了**可调用的** `RefreshLocalizedText`，建议合并补丁时把那一行落点收紧成 `CardView.cs:14706（RefreshLocalizedText）`（补丁备注里已写这条请求；本代理不改主表）。
4. **`BattleDriver.ToCardData` 不带 `CardDef`**（只带 `id/title/keywords` 这些**已本地化**的串）⇒ 语言切换只能靠 `CardDatabase` 反查（原版手上有 `rawCardData`）。若 K-01 顺手在 `CardData` 上多带一个源卡引用，`RefreshLocalizedText` 就能省掉反查、也能覆盖「自设计卡描述无从重取」那一档。**本包选择不扩 `CardData`**：那个 struct 在 `CardView.cs` 开头（行 29-147），加字段会让文件下游 **225 条账落点整体 +N 行**（铁律 18·b 的坑），不值得。
5. **`CardFeel` 那三条溶解调用点**（`Core/CardFeel.cs:1881 / 2120 / 2159`）里有一处旧注释写着「靠下次登场 `HandToBoard` 再 `BeginDissolve` 才兜住」—— 现在传说档每次 `BeginDissolve` 都会重贴颜色/名字，那条注释仍然成立，但**如果谁把 `BeginDissolve` 改成「已经在溶解材质上就早退」，传说档就会在「面朝下→翻开→再溶解」时静默用错档**。⛔ 别做那个早退优化。
6. **`BattleScene` / `CardBaseDemo` 侧目前没有这四条的自检断言**（本包是写手，不写宿主）：建议在同步点补 ① 传说/非传说/伏击三格材质名；② 残骸悬停四格 `body3D.activeSelf`；③ `SetDescriptionSuffix` 往返（含 `KeywordsText.text` 逐字节）；④ 切语言后卡名/描述变、四件数值不变。
