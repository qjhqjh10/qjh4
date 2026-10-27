# B9_W15CVIEW · K-03 五条（`Core/CardView.cs` 剩余 C）

> 执行代理 **B9_W15CVIEW** · 2026-10-27 · 判据包 = `判据/B9_P0剩余C_派活包.md` §K-03 · `判据/B9_P0剩余C_施工单.tsv` 五行
> 红线遵守：⛔ 未跑 Unity · ⛔ 未动 git · ⛔ 未改正本/账主表/别人的文件 · ⛔ 未用 `sed -i`
> 每改一次 `.cs` 都跑了秒级类型检查，末次 **`TMPDIR=/tmp/wf_w15 bash 工具/typecheck.sh` → 运行时 0 / 编辑器 0**（连跑三次稳定）。

---

## 一、结论

**五条里四条已是 A（本轮 = 核验 + 补断言），第五条仍 C（调用点在别人文件，只写了改动请求）。**
`CardView.cs` **没有实现层缺口**；本轮实际动到的产品代码只有两处：① 一处注释按铁律 18 补 `🔶 自己设计` 标记（零行数变化）
② 类尾追加一个**自检读口** `ArmyText`（四颗 TMP 原来只有三颗能读）。

| 账行 | 现等级（本轮现读） | 原版怎么算 | 我们原来怎么算 | 本轮改了什么 | 怎么验 |
|---|---|---|---|---|---|
| `HUD:108` | **A**（主表 `状态=A`，与派活包「现等级 P2」= 优先级，不是状态） | `BattleCardUI.ChooseDissolveMaterial()`：`HasToUseLegendarySummonEffect()` = `!HasCurrentTrait(ambush 0x4B0) && cardRarity == 4` ⇒ 取 `+0x198`（`Card 3d WH40K Dissolve Legendary`），否则 `+0x400`（普通） | 上一批 `W2K03` 已落地：`HasToUseLegendarySummonEffect`(:14575) + `ApplyDissolveVariant`(:14583) 一处选档 + `BeginDissolveMaterial`(:14596)；`BeginDissolve`(:6986) 与建材质(:6943) 都走它 | **无实现改动**。复读两份原版材质 JSON：`_BorderColor`/`_BorderColor2`/`m_Name` 逐值一致（见 §二 表）；**补断言**（原来零断言，`CardBaseDemo` 那条只断普通那份的 4 个标量） | `CardFaceAssertE5.RunK03DissolveLegendary`：传说→传说那份（名字 + 两项颜色写死原版字面量）· 伏击反例→普通 · 普通稀有度反例→普通 · **灭自证**：翻回正面再 `BeginDissolve` ⇒ 回传说档（缓存档位就红） |
| `HUD:153` | **A** | `BattleCardUI.ShowOriginalRemnantCard(bool)`：`body3D.SetActive(show && ShouldShowOriginalRemnantCard())`；后者 = `isRemnant(+0x65) && cardArmy ∈ {Sautekh 40, SaimHann 30}`。⚠️ **`show==false` 那一支原版没有闸**，直接 `SetActive(false)` | 上一批 `W2K03` 已落地：`ShowOriginalRemnantCard`(:14632) + `HasOriginalRemnantCardArmy`(:14622) + 锁存 `_originalRemnantCardShown`(:14653) + 回调接线(:14435)；原版 4 个调用点里 3 个（悬停/战斗预览开/收）走 `CardHighlight.CardBoardHighlight` 回调，第 4 个（`CardScript._UnitDeath`）由 `SetRemnantBody(false)`→`HideRemnantBodyNow`:2756 承担（清锁存 + 恢复卡身） | **无实现改动**；把「非残骸早退」那条注释按铁律 18 就地标 `🔶 自己设计（非原版）`（结论：原版该分支永不可达、纯防御）。**补断言** | `RunK03Remnant`：灵族残骸 `show=true` 亮 / **灭自证**：`SetRemnantBody` 再来一次（= `SyncBoard` 每刷新那一跳）**不许把刚亮的卡身关回去** / `show=false` 收回 / 反例①非残骸卡**一个开关都不动** / 反例②残骸但非那两族**不亮** / 阵营名表逐值（含 `null`） |
| `HUD:148` | **A** | `BattleCardUI.ShowAvailableTargets()` 末句 = `SetDescription(desc + "<i>" + 那段 + "</i>")`（`.c:132`，**无空格**；`desc` = `GetLocalizedCardDescWithLineBreak()`） | 写口 = `CardView.SetDescriptionSuffix`(:14683)，拼装点唯一（`DescTextWithSuffix`:14698，两条建字路 :5006/:5247 都走它）；调用链 = `CardInteraction.SetTargetsSuffix`(:1943 抬牌 / :2045 写口) ← `BattleDriver.Begin` 的 `TargetSuffixFor`(:4984)。上一批 `W3K04`+`W9K01` 已接完 | **无实现改动**（`CardView` 侧只做核验：写口幂等、`SetData` 不吞后缀、场上形态空操作）。**补断言** | `RunK03DescSuffix`：尾部拼装逐字（`bare + "<i>(Boss Nob)</i>"`）/ 幂等 / **灭自证**：`SetData` 之后后缀仍在 / 复原成裸描述 / 场上形态空操作 |
| `HUD:189` | **A** | `BattleHud.BroadcastChangedLanguage()` → `BroadcastMessage("ChangedLanguage")`；卡面实现者 `BattleCardUI.ChangedLanguage()` = `SetCardTextsExcludingStats(rawCardData)`（**四颗 TMP**：卡名/描述/兵种/阵营）**＋** `cardState == inHand ⇒ SetCardTextInHand(true)`；**数值一个字节不动** | 上一批 `W2K03`+`W9K01` 已落地：`CardView.RefreshLocalizedText`(:14731) 重走 `BuildTextLayers`（内含 `ComputeTextState` 重算手牌档位）+ `LocalizationSourceDef`(:14762) 反查源卡；订户 = `BattleDriver.RefreshLocalizedCardTexts`(:33644)（幂等先摘再挂、`:4268` 摘）；广播源 = `SettingsPanel.ChooseLanguage` → `BattleHudLanguage.Notify()`(:1903) | **补一个读口** `CardView.ArmyText`（类尾 :15013）—— 四颗 TMP 里阵营行原来**没有**读口，「四颗都重写」判不了。**补断言** | `RunK03Language`：英文档建卡 → 切中文 → `RefreshLocalizedText()` 真 → **卡名/描述必变**、**数值四格逐字节不变**、兵种行/阵营行没丢；`Loc` 用 `PersistOverride`+`RestoreForTest`（⛔ 不碰 `PlayerPrefs`） |
| `卡组:148` | **C（未变）** | `DeckGeneralInfoDemo.WarlordInfoButtonClick()` → `CardDisplayWindow.Instance.ShowCard(deckHero, tier: null, showOptions: false)` | 整条没接（`Deck info ▸ Warlord Name` 只有 Label、无命中区） | **`CardView` 侧无需改动**（逐条核过，见下）；**改动请求**已写进账补丁备注（目标 = `Shell/PracticeModePopup.cs`，**该文件归 W8，本代理未碰**） | 见 §二 第 5 条：点督军名 ⇒ 详情窗开且无选项条 / 点空白 ⇒ 不开 / 关窗后练习窗仍可用 |

**「`CardView` 侧无需改动」的三条核验**：
1. 详情窗入口现成：`Shell/CardDetailPopup.ShowCard(CardDef)`（`Shell/CardDetailPopup.cs:865`）。
2. 原版 `tier: null` 的语义 = `CardServices.GetCardTier(card)` → 玩家收藏里没有这张 ⇒ **0 = Tier1**
   （`decomp_full/CardDisplayWindow__ShowCard.c:30-32` 的 `if (hasValue==0) tier = RawCardScript.get_CardTier(card)` +
   `decomp_full/CardServices__GetCardTier.c` 的落空支 `local_res8[0] = 0`）；
   我们的 `CardProgress.Level`(:271) 缺省 **1** ⇒ `CardArt.Frame(..., tier:1)` = 第一档 ⇒ **同档**。
3. `showOptions:false` 在我们这侧本来就是空转 —— 原版 `CardDisplayWindow.options` 在 13 个竞技场全 `{0,0}`、
   菜单版才有；`CardDetailPopup` 不建 `CardDisplayOptions` 是**与原版一致**（见该文件头 :74-80 的订正）。

---

## 二、证据（文件:行号）

### 2·1 `HUD:108` 传说溶解材质
- 原版判据：`d:/2/tools/decomp_full/CardScript__HasToUseLegendarySummonEffect.c`（全文 15 行：`HasCurrentTrait(0x4b0)` 为假 ⇒ `cardRarity == 4`）、
  `BattleCardUI__ChooseDissolveMaterial.c`（`+0x198` = 传说 / `+400` = 普通）、`d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardRarity.cs:7`（`Legendary = 4`）。
- 两份材质 JSON（`d:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/Material/`）本轮亲读：

| 文件 | `m_Name` | `_BorderColor` | `_BorderColor2` |
|---|---|---|---|
| `Material_-6651017123104198842.json` | `Card 3d WH40K Dissolve` | (0.7140866518, 1.9145799875, 2.6390156746, 0) | (0, 0.0285916403, 0.5764706135, 0) |
| `Material_-1409023286357856989.json` | `Card 3d WH40K Dissolve Legendary` | (2.6390156746, 2.4975879192, 0.7184754610, 0) | (0.5764706135, 0.1012587994, 0, 0) |

- 我们的实现：`Core/CardView.cs:14548`（判据注释块）· `:14555`/`:14557` 两个材质名常量 · `:14563`/`:14566` 传说两项颜色
  · `:14575` `HasToUseLegendarySummonEffect` = `!_faceDown && Data.rarity == "legendary"` · `:14583` `ApplyDissolveVariant`
  （**唯一选档点**）· `:14596` `BeginDissolveMaterial` · `:6986` `BeginDissolve` 调它 · `:6943` 建材质那一跳也调它。
- 稀有度取值核对：`Assets/RuleEngine/Resources/cards_engine.json` 现读 `common 331 / rare 319 / epic 231 / legendary 197 / special 50`（小写）；
  `RuleEngine/Core/DeckRules.cs:200` 的 `IsLegendary` 同口径（`OrdinalIgnoreCase("legendary")`）。
- 生产调用点：`Core/CardFeel.cs:1885`（登场）/ `:2124` / `:3148` / `:3589` 都 `card.BeginDissolve()` ⇒ 传说卡真会走到。
- `_faceDown` ↔ 原版 `HasCurrentTrait(ambush)` 的映射：`Core/CardView.cs:7134`（`bool _faceDown; // 面朝下（原版 = 身上挂着伏击词条）`）
  与 `:7198`（`SetAmbush` 写它）。**这是我们这侧唯一可用的等价物**（`CardView` 拿不到引擎的运行时 trait 集合）。

### 2·2 `HUD:153` 残骸原卡 3D 卡身
- 原版：`BattleCardUI__ShowOriginalRemnantCard.c`（`param_2 == 0 ⇒ uVar3 = 0`；否则 `ShouldShowOriginalRemnantCard`）、
  `SupportMethods__ShouldShowOriginalRemnantCard.c`（`+0x28 → +0x2c` 比 `0x28`/`0x1e` = 40/30，且 `+0x65 != 0`）。
- 原版 4 个调用点：`CardHighlight__ToggleCombatPreviewHighlight.c`（开/收）· `CardScript__TryCardHighlight.c` ·
  `CardScript._UnitDeath_d__446__MoveNext.c:351`（**传 0**，随后把 `+0x65` 清 0）。
- 我们：`Core/CardView.cs:14632`（实现）· `:14622`（阵营闸）· `:14651`/`:14653`（读口/字段）·
  `:2646`/`:2647`（`SetRemnantBody` 里「锁存为真就跳过关卡身」= 两处开关不互相覆盖）· `:2756`/`:2758`（`HideRemnantBodyNow` 清锁存并恢复 `_body3D`）
  · `:14435`（`CardHighlight.CardBoardHighlight.ShowOriginalRemnantCard = v => ShowOriginalRemnantCard(v)`）。
- 回调三处调用（别人文件，只读核）：`Core/CardHighlight.cs:642`（战斗预览开）/ `:662`（收）/ `:698`（悬停）。

### 2·3 `HUD:148` 描述写口
- 原版：`decomp_full/BattleCardUI__ShowAvailableTargets.c:29/32/36`（三合一早退：`isPlayer` / `cardState == 8` / `ShouldShowTargetsAvailable(false)`）
  · `:60-108`（`GetTargetAvailableText` / `GetTargetsAvailableText` 两支）· `:112` `String.Concat(desc, DAT_184236780, 那段, DAT_1842cf6e8)` + `:113` `SetDescription`
  （两枚 DAT = `<i>` / `</i>`）。
- 我们：`Core/CardView.cs:14683` `SetDescriptionSuffix` · `:14698` `DescTextWithSuffix`（**唯一拼装点**）·
  `:5006`/`:5247` 两条建字路 · `:14673` `DescriptionSuffix` 读口。
- 调用链：`Hand/CardInteraction.cs:1943`（抬牌那一拍 `SetTargetsSuffix`）/ `:2045`（写口）/ `:2060`（`AvailableTargetsSuffix`，目标池走 `RuleCore.PlayTargetPool`）
  ← `Battle/BattleDriver.cs:4984`（`interaction.TargetSuffixFor = v => CardInteraction.AvailableTargetsSuffix(Ctx, _me, DefOf(v))`）。
- 富文本前提：`bundle_staticgeneralassets_assets_all/GameObject/DescTextUnit.json` 那颗 TMP 的 `m_isRichText = 1`（施工单已核），
  且 `SetDescriptionSuffix` 里显式兜一次 `_keywords.richText = true`。

### 2·4 `HUD:189` 换语言重写四颗 TMP
- 原版：`decomp_full/BattleCardUI__ChangedLanguage.c`（`SetCardTextsExcludingStats(+0xe0)` + `cardState == 1 ⇒ SetCardTextInHand(true)`）·
  `BattleHud__BroadcastChangedLanguage.c`（`BroadcastMessage("ChangedLanguage")`）。
- 我们：`Core/CardView.cs:14731` `RefreshLocalizedText`（`_descSuffix = null` → `LocalizationSourceDef` → `d.keywords = FaceTextFull(def).body` → `BuildTextLayers`）·
  `:14762` 源卡反查（先「同名 + 同阵营」再退「只按名字」）· `:4876` `BuildTextLayers` 首句 `_textState = ComputeTextState(d)`（= 原版 `SetCardTextInHand` 那一句）
  · 四层写入：卡名 `:4933`、描述 `:5006`、阵营行 `:5037`、兵种行 `:5068` · **数值层不在 `BuildTextLayers` 里** ⇒ `ExcludingStats` 天然成立。
- 广播链（只读核）：`Battle/SettingsPanel.cs:1897` `ChooseLanguage` → `:1903` `BattleHudLanguage.Notify()`；
  订户 `Battle/BattleDriver.cs:33644`（`RefreshLocalizedCardTexts`，`:33632-33633` 先摘再挂；`:4268` 摘）；
  `_settingsPanel` 在战斗内由 `BattleDriver` 建（`:2256`、`:5881` 有「设置钮」那条）⇒ 局内也能切语言。

### 2·5 `卡组:148` 详情窗依赖
- 原版：`decomp_full/DeckGeneralInfoDemo__WarlordInfoButtonClick.c`（`ShowCard(deckHero, null, false)`）。
- 我们的落点（只读核，全部**不在本代理白名单**）：`Shell/PracticeModePopup.cs:816` `BuildDeckInfoHead`（督军名 = 纯 `Label`，无命中区）、
  `Shell/DeckInfoPopup.cs:2446-2456` `OpenWarlordDetail`（**现成先例**：`CardDetailPopup.Create(Manager)` → `Manager.OpenWindow(w)` → `w.ShowCard(wl)`）、
  `Shell/CollectionData.cs:184` `Warlord(int)` 直接给 `CardDef`、`Shell/CardDetailPopup.cs:865` `ShowCard(CardDef)`。

---

## 三、没查清的部分（⛔ 没有一条用猜测填空）

1. **`_faceDown` ≠ `HasCurrentTrait(ambush)` 的完全等价**（`HUD:108`）：原版查的是**当前 trait 集合**（可被效果运行时加上/移除），
   而 `CardView` 只有「面朝下」这个布尔（我们这侧 ambush 一定伴随面朝下，反向未验：**有没有「挂了 ambush 但不面朝下」的卡**没查）。
   ⇒ 现状是**等价映射**，代码里已写明；**没查清的那部分是「运行时被临时授予 ambush」这一档**（`CardView` 拿不到 trait 集合，判据在引擎侧 `UnitState.Traits`，
   但那属 S0/W9 的 `RuleEngine/Core/UnitState.cs`，本批不许动）。**还差什么**：`CardView` 若将来要精确，需要一个「当前是否挂 ambush」的推入口（由 `BattleDriver.SyncBoard` 喂）。
2. **`HUD:148` 的 Trap / SecretOrder 词条**：官方 13 语言表（10,862 键）里 `Card_Trait/trap` / `Card_Trait/secretOrder` **都不存在**
   （上一批已记，本批复核结论不变）；我们退成「N 个可用目标」并出声一次。**还差什么**：远端表 / 用户裁定自译（主表已记 D 候选，池内 Trap/SecretOrder 0 张 ⇒ 今天不可达）。
3. **`HUD:189` 的「非中英 11 语档」**：`RefreshLocalizedText` 的卡名 / 描述走 `CardText.Name` / `FaceTextFull`，
   两者对非中英档走 `Loc.Lang13Current`（官方 13 语表）。**没验证**（本批断言只切 英↔中）—— 需要那 10 种语言的列都有值时才能断，
   而本批没有跑 Unity ⇒ **如实记：11 语档未验**。
4. **本轮补的断言一条都没跑过**（红线：子代理不许 `-executeMethod`）。它们按「前提硬检查 → 结论」写，前提不成立会**红**而不是静默跳过；
   但**环境敏感点有两条**：① `RemnantBody3D Aeldari` 在效果库里（`Editor/BattleScene.cs:16785` 走过同一条，**应当**在）；② 中文字体资产可用（`CardText.Zh` 前提已断）。
   **还差什么**：调度台同步点跑一次 `CardFaceAssertE5.Run` 定案（判绿红看 `E5 合计` 那行）。

---

## 四、改动清单

| 文件 | 改动 | 行数变化 | 为什么安全 |
|---|---|---|---|
| `Assets/CardPresentation/Core/CardView.cs` | `ShowOriginalRemnantCard` 里那条「非残骸早退」注释按**铁律 18** 就地改成 `🔶 自己设计（非原版）…`（含 ① 是什么 ② 为什么必须自己定 ③ 谁定的+日期） | **0 行**（两行就地改写） | 铁律 18·b：账按 `文件:行号` 引用 185 个文件里的 141 个 ⇒ **加行会报漂移**；就地改写不动行号 |
| 同上 | **类尾**追加自检读口 `public string ArmyText`（阵营行 TMP 的 `.text`） | **+9 行**（全部在 `FadeGroupMemberBuilt` 之后 = 类最末；文件末尾之后只剩两个收尾花括号） | 账里对 `CardView.cs` 的落点最大到 `:14928`，类尾追加不动任何既有行号（改后复核：`SetRemnantBody:2537` · `BeginDissolve:6986` · `ApplyBadgeAlpha:8001` · `MulLayerAlpha:14908` · `ForEachFadeGroupMember:14928` **全部原位**） |
| `Assets/CardPresentation/Editor/CardFaceAssertE5.cs` | 新增 **F 组**：`RunK03Four` + `RunK03DissolveLegendary` / `RunK03Remnant` / `RunK03DescSuffix` / `RunK03Language`（共约 47 条断言，每条都带 🧨 改坏法），并在 `Run()` 里 `RunNestedFadeGroups()` **之后**调一次（它自己 `finally` 恢复 `Loc` 语档，再往下跑 T 组） | +约 330 行 | 该文件**不在账的落点里**（`grep CardFaceAssertE5 账/*.tsv` 只在备注里被提到、无行号落点）⇒ 加行不影响 `check_ledger_points` |
| `资料/完全复刻/账/_parts/B9_W15CVIEW.tsv` | 账补丁：4 条 A 行**只追加备注**（核验结论 + 证据行号 + 断言名）+ `卡组:148` 追加**改动请求**与「CardView 侧无需改动」 | 新文件（表头 + 5 行，11 列） | 生成器 `_parts/_gen_B9_W15CVIEW.py` 从**当前主表逐字复制**该行再只改【备注】/【我们的落点】⇒ 合并时不会把别列修订盖回去；生成时硬校验「11 列 / 方法列是完整签名带 `(...)` / 字段无制表符」 |
| `资料/完全复刻/报告/B9_W15CVIEW_CardView五条.md` | 本报告 | 新文件 | —— |

**⛔ 没碰**：`Shell/PracticeModePopup.cs`（W8）· `Core/CardFeel.cs` / `Editor/BattleScene.cs`（W11）· `Battle/BattleDriver.cs`（W10）·
`Battle/SettingsPanel.cs` · `Core/CardHighlight.cs` / `Hand/CardInteraction.cs` · `RuleEngine/**` · 账主表 · 任何 `*.unity` · `CLAUDE.md` / `项目任务.md`。

---

## 五、顺手发现（⛔ 都没动手改，交调度台分流）

1. **🔴 账里 `CardView.cs` 的落点行号已整体漂 +25 左右**（不是本代理造成的）：主表 `HUD:108` 落点写 `CardView.cs:14571（BeginDissolveMaterial）`，
   现读 **14596**；`HUD:153` 写 `:14607` → 现读 **14632**；`:14626` → **14651**；`:14597` → **14622**；`HUD:189` 备注里 `:14706` → **14731**。
   根因 = 之前那批给同一文件加了 229 行（`ApplyBadgeAlpha` 改 9 组根广播 + `FadeGroup` 枚举）。
   ⇒ **同步点要跑 `check_ledger_points --all-A --fix`**（只能在「无 `.cs` 写手 + 补丁合并完」时跑）。本代理的补丁行照抄了现表数字，不改它们（让 `--fix` 统一重钉）。
2. **`Battle/SettingsPanel.cs:1899-1901` 的注释已过期**（铁律 5 的「就地改掉」那条，但该文件不在本代理白名单 ⇒ 只报不改）：
   它还写着「今天只有广播口，卡面重排字（`Core/CardView.cs`，E7）与日志重拼行（`BattleDriver.RefreshBattleLog`，E5）**还没订** ⇒ 会打出 0」，
   而两者现在**都已订阅**（`BattleDriver.cs:33632`/`:33633`）。⇒ 真机日志里那句 `⚠️ 没人订阅` 的分支现在走不到，注释该跟着改。
3. **`CardView` 的自检读口不对称**：卡名（`TitleText`）/ 描述（`KeywordsText`）/ 兵种（`RaceText`）都有读口，**阵营行原来没有**（本轮已补 `ArmyText`）；
   同族还缺一个「描述那颗 TMP 的 richText 开关」读口 —— `SetDescriptionSuffix` 里兜了一次，但**断言读不到**（本轮靠「文本里出现 `<i>` 原样」间接判断，未单独断）。
4. **`HUD:108` 的断言原来只覆盖普通那份**：`Editor/CardBaseDemo.cs:3866` 那组（W64）断的是普通材质的 4 个活标量 + 两项颜色 + `_DissolveTex`，
   **传说档一个断言都没有**（本轮补在 E5）。⇒ 两处现在各断一半、语义不冲突，**不必合并**（`CardBaseDemo` 那组跟「序列化值」走，E5 这组跟「选档」走）。
5. **`HUD:153` 的原版第 4 个调用点（`CardScript._UnitDeath` 传 `0`）在我们这侧没有同名调用**，由 `SetRemnantBody(false)` → `HideRemnantBodyNow`
   的「清锁存 + 恢复卡身」承担（`:2756-2761`）。**语义等价**，但没有断言直接钉「单位死亡那一拍不会把原卡卡身留在屏上」——
   本轮 `RunK03Remnant` 只钉了 `show=false` 与「刷新不覆盖」两条。若要补齐，需要「死亡 → 残骸 → 撤掉」的整条驱动链（属 `BattleScene` 宿主的活，W11 的地盘）。
6. `卡组:148` 的**先例已存在**（`DeckInfoPopup.OpenWarlordDetail`），所以 W8 那一步只是「照抄四行 + 加一颗命中区」，
   **不需要新设计**；唯一要注意：命中区按 `MenuDraw.PaddedRect` + 原版 `m_RaycastPadding`（⛔ 不是 `m_TargetGraphic`，见 CLAUDE.md 那条记忆）。
