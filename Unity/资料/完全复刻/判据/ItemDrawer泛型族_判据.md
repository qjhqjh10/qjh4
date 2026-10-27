# ItemDrawer 泛型族 —— 判据（泛型类方法体的导出与逐方法解）

> 子代理 **C3T4**（查判据型 · 只读）· 2026-10-27
> 目的：把账里两条「判据缺」—— `#9 ItemDrawer.Draw(ObtainableItem,int,ItemDrawerOptions)`、`#12 ItemDrawer.GetItemName(T)` —— 解出来。
> 根因 = **泛型类**：`dump.cs` 对泛型类写 `// RVA: -1`，真地址只在 `GenericInstMethod` 子块 ⇒ 按「RVA 非负」筛名单会静默丢掉全部泛型类方法（判据 → `资料/全量反编译_入口与用法.md` §「泛型类方法体的导出」）。

## 〇、产物来源（**没有重跑 Ghidra**）

- **全量泛型导出（G7）已完成**：`D:/2/tools/decomp_generic_trial/out/` 现有 **640 个 `.c`**（G6 28 + G7 612），索引 `out/_MANIFEST_generic_all.tsv`（615 行）。
- `ItemDrawer<object>` 的 **28/28 方法全部已导出**。本轮**直接读产物**（`out/ItemDrawer_object___*__<addr>.c`），按「声明串 / 地址」定位，不依赖首行名（首行是 `FUN_`/他名 = 名字含 `", "` 或 ICF 折叠，**≠ 解不出**）。
- 关键纠正：账 `#9` 记的「sealed override 体有 20+ 处 `NoteDecompilerIssue`」是 **ILSpy（`out_cpp2il_dev_ilspy/ItemDrawer.cs`，现读 **168** 处）** 的噪声；**Ghidra 泛型产物 `ItemDrawer_object___Draw__1ae9cc0.c` = 155 行、`NoteDecompilerIssue` = 0** ⇒ **完全可读**（同 CLAUDE.md「判读不出来先看 Ghidra `.c`」那条教训）。

## 一、基类 `ItemDrawer<object>`

| 方法 | 能否读出 | 读出内容 | 我们现状 | 还差什么 |
|---|---|---|---|---|
| **`Draw(ObtainableItem item, int amount, ItemDrawerOptions options)`** | ✅ **完全可读**（`out/ItemDrawer_object___Draw__1ae9cc0.c`，155 行，0 NoteDecompilerIssue） | ① 先调两条 `this` 的虚方法（vtable `+0x1d8`/`+0x1e8`，传 0）= **复位两件子控件**；② 写 `item`（this+0x38）、`options`（this+0x28）、`amount`（int, this+0x40）；③ 取 item 侧「类型→label」链（`(+0x20)+0xc0`）拿到 **label 组件**，`label.setText(amount.ToString())`（label vtable `+0x558`）；④ 取 image 链（`(+0x28)+0xc0`）拿 **image**，置 active，再调 `this` 的虚方法 `+0x278`（= 取图标 sprite）灌 `image.sprite`；⑤ 取 label 当前文字，`IsNullOrEmpty`：**非空 ⇒ 走收尾**（调子类 hook `this+0x288` 传 `(item, amount)`，再按需 `image.gameObject.SetActive`）；**空 ⇒ 隐藏 image** | `Shell/ItemDrawer.cs:840 Draw` + `:1849 Dispatch`（结构对得上，账记「逐跳对不上」= 尚未逐跳核） | 逐跳对：`+0x1d8/+0x1e8/+0x278/+0x288` 四个 **vtable 槽的具体目标**未反查（按类 vtable 表定，未做）；其余 ⑤步已可对 |
| **`GetItemName(T item)`（基类体）** | ✅ **可读**（`out/ItemDrawer_object___GetItemName__6e6e80.c`） | 体 = **`return item.LocalizedName`**（委托 item 的虚方法 `item vtable +0x1a8`）；⚠️ 该地址与 `MissionsTab.<>c.<RefreshRefillCount>b__9_0` **ICF 同码**（首行是后者，`name_class=ICF_other_name`；**体是对的**） | `Shell/ItemDrawer.cs:123 ItemSpec`（走**数据层**给 `Label`，不逐子类 override） | 若要对齐「基类 = item.LocalizedName」：我们 `ItemSpec.Name` 已是成品串，等价；逐跳差异见下 23 子类 |

## 二、23 个子类 `xxxDrawer.GetItemName` 的返回值规则（逐条读 `decomp_full/*Drawer__GetItemName.c`）

> 读法：`I2_Loc_LocalizedString__op_Implicit(&field)` = **返回序列化在抽屉上的 I2 `LocalizedString` 字段**（含 `mTerm` 词条键，按当前语言取）；`DAT_1842b80e0` = 一个**静态空串/常量**。

| # | 子类 | 能否读出 | 返回值规则 |
|---|---|---|---|
| 1 | `AvatarBorderDrawer` | ✅ | 返回序列化 `LocalizedString`（抽屉 `+0x58`） |
| 2 | `AvatarDrawer` | ✅ | 返回序列化 `LocalizedString`（`+0x58`；体与 AvatarBorder 逐字同） |
| 3 | `CampaignPointDrawer` | ✅ | 返回序列化 `LocalizedString`（`+0x60`） |
| 4 | `ExpansionPassPointDrawer` | ✅ | 返回序列化 `LocalizedString`（`+0x60`；体=CampaignPoint 同） |
| 5 | `ForgePointDrawer` | ✅ | 返回序列化 `LocalizedString`（`+0x60`；体=CampaignPoint 同） |
| 6 | `RandomCardDrawer` | ✅ | 返回序列化 `LocalizedString`（`+0x78`） |
| 7 | `TitleDrawer` | ✅ | 返回序列化 `LocalizedString`（`+0x68`） |
| 8 | `ContainerDrawer` | ✅ | `(+0x40 == 1) ? LocalizedString(+0x58) : LocalizedString(+0x70)`（布尔选两档名之一） |
| 9 | `CardDrawer` | ✅ | 返回**静态常量串** `DAT_1842b80e0`（空/固定） |
| 10 | `CardAlternateArtDrawer` | ✅ | 返回静态常量串 `DAT_1842b80e0`（空） |
| 11 | `CosmeticDrawer` | ✅ | 返回静态常量串 `DAT_1842b80e0`（空） |
| 12 | `CurrencyDrawer` | ✅ | `Currency.get_DisplayName(item)` |
| 13 | `DeckDrawer` | ✅ | `item==null ⇒ 空`；否则 `Loc.GetTranslation("PrebuiltDecks/" + item.deckId(+0x48))`（已解，与账记一致） |
| 14 | `WildcardDrawer` | ✅ | `string.Format(LocalizedString(+0x68), GameStaticData.CardRarityToString(item.rarity(+0x28)))` |
| 15 | `WildcardIconDrawer` | ✅ | **抛 `NotImplementedException`**（原版这档**未实现**） |
| 16 | `AllianceBadgeDrawer` | ✅(ICF) | 体折进 `MissionsTab.<>c.<RefreshRefillCount>b__9_0`（同码）；语义 = 委托（按 item/本地名），需按 vtable 定 |
| 17 | `CardbackDrawer` | ✅(ICF) | 同 16（折进 MissionsTab lambda） |
| 18 | `ExpansionPassPremiumDrawer` | ✅(ICF) | 同 16 |
| 19 | `PremiumDrawer` | ✅(ICF) | 同 16 |
| 20 | `XSollaOfferDrawer` | ✅(ICF) | 同 16 |
| 21 | `ForgePointIconDrawer` | ✅(ICF) | 折进 `<>f__AnonymousType31.GetHashCode`（`return 0`）⇒ 无独立逻辑 |
| 22 | `RandomCardIconDrawer` | ✅(ICF) | 折进 `CustomDropTableDefinition.get_LocalizedName`（`**(DAT+0xb8)`）= 取静态配置里的本地名 |
| 23 | `TitleIconDrawer` | ✅(ICF) | 同 22（折进 `get_LocalizedName`） |

> 🔴 **ICF 折叠（16–23）**：地址与别的函数共用 ⇒ 首行是「别人」的名字，**但机器码就是被折叠的共享体，语义一致**（「同址多方法」是链接器折叠，不是解失败；判据 → `资料/全量反编译_入口与用法.md` G7 那条）。这 8 条要精确到「对 item 取哪个字段」时，需按 `ObtainableItem.LocalizedName`/各 SO 的字段偏解读，或 VA 反汇编那位（本轮未做）。

> **子类 Draw override**：`decomp_full/*Drawer__Draw.c` 现读 **28 个**（ItemDrawer 族约 23 个，另有 GizmoTypeDrawer / DeckAndCardbackDrawer / GenericArmyItemDrawer / ItemDrawer / PremiumIconDrawer 等旁支）。抽查均可读实（WildcardDrawer 50 行 / CardDrawer 22 / DeckDrawer 64 / PremiumDrawer 64），**不是空壳**。本轮只逐条解了基类 `Draw`（对应账 `#9`）；23 个子类 Draw override 的**逐条语义表尚未做**（若需要，按同一读法扩）。

## 三、泛型族其余已导出方法（顺带）

`ItemDrawer<object>` 28 个方法 **28/28 出产物**，除 `Draw`/`GetItemName` 外还有：`Awake` · `Refresh` · `SetItemName` · `SetConvertedItem` · `SetEphemeralDisplay` · `ToggleClaimedWarning` · `OverridePremiumBadge` · `TogglePremiumHighlight` · `ToggleShowInfoOnClick` · `ToggleHighlight` · `ToggleLock` · `OnPointerClick` · `HandlePointerClick` · `.ctor` · 10 个 `get_*`（Components/background/image/label/quantity/premiumHighlight/premiumBadge/claimedWarning/Item/ObtainableItemTypeToDraw/Amount）。⇒ 账里若还有 ItemDrawer 泛型族的「判据缺」，**都能按本文件的读法解**。

## 四、结论（对本批两条「判据缺」）

- **#9 `ItemDrawer.Draw`**：判据**可从「判据缺」改判为「可读」**（Ghidra 泛型产物 155 行、0 NoteDecompilerIssue）；剩余 = 4 个 vtable 槽目标未反查 + 逐跳核（不是没判据）。
- **#12 `ItemDrawer.GetItemName`**：基类体「可读」（= `return item.LocalizedName`）；23 子类规则**已逐条读出**（上表）；ICF 的 8 条要精确到字段时再 VA。⇒ 也不再是「判据缺」，而是「可读 + 待逐跳核」。

---

# 第二批（2026-10-27 · C3T4h 续）：Draw 的 4 个 vtable 槽 + 23 子类 Draw + 8 条 ICF 复核

> 承接 §一/§二。本轮**仍在读 `decomp_generic_trial/out/` 与 `decomp_full/` 的既有产物**（未起 Ghidra）。

## 二·甲　`ItemDrawer<object>__Draw` 的 4 个 vtable 槽反查

| 槽（字节偏移） | 结论 | 能否坐实 | 依据 |
|---|---|---|---|
| **`+0x278`** | **`GetItemName(T item)`（Slot 20）** | ✅ **坐实** | 调用点：`uVar5 = (*(this+0x278))(this, item)` 的**返回值是 string**，紧接 `(*(组件+0x558))(组件, uVar5)` = 把字符串写进 **name 文本（TMP set_text）**；随后又读该文本 `String.IsNullOrEmpty` 判空 ⇒ **不是取 Sprite，是取名字**。ItemDrawer 里唯一「吃 item、返回 string」的虚方法就是 Slot20 `GetItemName` |
| **`+0x288`** | **`Draw(T item, int quantity)`（Slot 21，abstract 子类钩子）** | ✅ **坐实** | 调用点：`(*(this+0x288))(this, item, amount)`；实参 = `(item, int)`；ItemDrawer 里唯一 `(T, int)` 签名的虚方法 = Slot21 `Draw(T,int)`，23 个子类 override 的就是它 |
| `+0x1d8` | **一个「复位」虚调用（传 0）** | ⚠️ **未坐实**（候选） | `(*(this+0x1d8))(this, 0)`：1 个实参传 0。候选 = `ToggleClaimedWarning(false)`(Slot7) / `TogglePremiumHighlight(false)`(8) / `ToggleHighlight(false)`(9) / `ToggleLock(false)`(13) / `SetEphemeralDisplay(null)`(11) 之一 |
| `+0x1e8` | **另一个「复位」虚调用（传 0）** | ⚠️ **未坐实**（候选） | 同上；`0x1d8` 与 `0x1e8` 相差 16 字节（= 2 槽），说明中间隔了一个槽 |

> 🔴 **为何 0x1d8/0x1e8 未坐实**：Ghidra 的 `+偏移` 是相对**运行期 vtable 指针**的字节偏移，而 `dump.cs` 的 `Slot:` 是相对**类型自身方法表**的序号；两者之间的基址（MonoBehaviour / Behaviour / Component / Object 那一长串继承虚方法）没有现成对照表 ⇒ 不能简单 `offset/8` 反推槽号。要坐实需 **ItemDrawer<T> 的运行期 vtable 数组**（或对这两个调用点做 VA 反汇编取目标地址）。
> 语义上高置信的是：Draw 开头先「复位两件」再填内容（这两件大概率是 `claimedWarning` / `premiumHighlight` 一类复用的 GameObject 开关）。

## 二·乙　23 个子类 `Draw(T,int)` 覆写表（`decomp_full/*Drawer__Draw.c`，全部读出）

| # | 子类 | 覆写了哪几步 | 与基类的差 |
|---|---|---|---|
| 1 | `AllianceBadgeDrawer` | 徽标资源解析（item+0x10 AssetRef.Reference，空则 `AlliancesTrophyController.DefaultTrophy` 兜底）→ 写 this.item → 调 setter(vtable+0x188)(badge,1) → `SetTierFrame`(tier+0x18) | 多「默认奖杯兜底 + 段位框」 |
| 2 | `AvatarBorderDrawer` | `image.sprite = CosmeticItem.Image` | 仅图标 |
| 3 | `AvatarDrawer` | `image.sprite = CosmeticItem.Image` + `label(+0x70).set_text(GetLocalizedArmyName(army+0x2c))` | 图标 + 阵营名 |
| 4 | `CampaignPointDrawer` | `DrawForArmy(item.army+0x2c)` | 委托给 `DrawForArmy` |
| 5 | `CardAlternateArtDrawer` | `this(+0x68)=AssetLocator.GetAsset(item.GetIdForClonedCard())`；组件(+0x58).SetX(asset) | 异画克隆卡资源载入 |
| 6 | `CardDrawer` | (+0x68) 有 → `SetActive(false)`；组件(+0x58).SetX(item) | 关一件 + 设卡绘制 |
| 7 | `CardbackDrawer` | `image.sprite = CosmeticItem.CosmeticSprite` | 仅图标 |
| 8 | `ContainerDrawer` | `image.sprite=container AssetRef.Load`；(+0x88).SetActive(army+0x2c!=0)+sprite=ArmyIcon | 容器图 + 阵营图标件 |
| 9 | `CosmeticDrawer` | `image.sprite = CosmeticItem.CosmeticSprite` | 仅图标 |
| 10 | `CurrencyDrawer` | `image.sprite=Currency.GetIcon(item,1)`；(+0x58).sprite=Currency.GetIcon(item,0) | 两处币图标 |
| 11 | `DeckDrawer` | (+0x58).sprite=ArmyIcon(hero.army)；`image.sprite`=hero.cardImage(+0x50 AssetRef)；(+0x60).sprite=GameModeIcon(Nullable(gameMode+0x78))；(+0x60).enabled=(其+0xe0!=null) | 阵营图标 + 英雄图 + 模式图标 + enabled |
| 12 | `ExpansionPassPointDrawer` | army==0 → 取 `LiveOpsManager.GetHandler<ExpansionPass>().army(+0x7c)`；`label(+0x58).set_text(CardArmyToString)`；`image.sprite=ArmyIcon` | 阵营名 + 图标（含 army==0 兜底） |
| 13 | `ExpansionPassPremiumDrawer` | (+0x58).sprite=item.bg(+0x48 AssetRef).Load；(+0x60).set_text(CardArmyToString(army)) | 背景图 + 阵营名 |
| 14 | `ForgePointDrawer` | `DrawForArmy(item.army+0x2c)` | 委托给 `DrawForArmy` |
| 15 | `ForgePointIconDrawer` | `image.sprite=ArmyUtilities.GetForgeIconForArmy(army+0x2c)` | 按阵营锻造图标 |
| 16 | `PremiumDrawer` | (+0x58).sprite=item.bg(+0x48)[带 active 判定]；(+0x60).set_text(CardArmyToString(army)) | 背景 + 阵营名 |
| 17 | `RandomCardDrawer` | (+0x68).SetActive(rarity+0x28==0)；background.SetActive(rarity>0)+sprite=list[rarity-1](+0x60)；(+0x70).SetActive(army+0x2c==0)；image.SetActive(army!=0)+sprite=ArmyIcon；`label(+0x58).set_text(CardArmyToString)` | 稀有度背景 + 阵营图标 + 阵营名 + 两件显隐 |
| 18 | `RandomCardIconDrawer` | 体折进空方法 `NetworkingPeer.OnMessage(return)` = **no-op** | 无（不画） |
| 19 | `TitleDrawer` | `label(+0x58).set_text(CardArmyToString(army))`；image.sprite=ArmyIcon；(+0x80).sprite=image(+0xe0)；sprite 空 → 隐藏 image 与 (+0x80)；(+0x60).set_text(item.LocalizedName) | 阵营名 + 图标 + 称号图 + 称号名 |
| 20 | `TitleIconDrawer` | 体折进空方法 = **no-op** | 无 |
| 21 | `WildcardDrawer` | background.sprite=list[rarity-1](+0x60)；image.sprite=ArmyIcon(army)；`label(+0x58).set_text(CardArmyToString)` | 稀有度背景 + 阵营图标 + 阵营名 |
| 22 | `WildcardIconDrawer` | rarity-1>=list.Count → image.sprite=`list.FirstOrDefault()`；否则 image.sprite=list[rarity-1] | 仅图标（带越界兜底） |
| 23 | `XSollaOfferDrawer` | Components(+0x60).SetActive(false)；Components(+0x58).SetActive(true)；Components(+0x68).set_text(Loc.GetTranslation(固定词条)) | 开关两件 + 固定文案 |

> **旁支（非这 23，但同族 `Draw` 也导出）**：`DeckAndCardbackDrawer`（先 `DeckDrawer.Draw` 再 (+0x78).SetX(GetDeckCardback)+SetActive）· `GenericArmyItemDrawer`（复用/建内层 ItemDrawer）· `PremiumIconDrawer`（先 `PremiumDrawer.Draw` 再 `background.sprite=PremiumItem.BackgroundSprite`）· `ItemDrawer__Draw`（**静态工厂**：Instantiate drawer prefab → 调 Slot5 Draw，不是 override）· `ExitGames.Client.GUI.GizmoTypeDrawer`（非 ItemDrawer 族，Gizmos）。

## 二·丙　8 条 ICF 折叠的 `GetItemName` —— 逐条确认「体语义确实是 GetItemName 该有的」

| 折叠到的函数 | 体 | 是否 GetItemName 该有的 |
|---|---|---|
| `MissionsTab.<>c.<RefreshRefillCount>b__9_0`（AllianceBadge / Cardback / ExpansionPassPremium / Premium / XSollaOffer，5 条） | `return param.Item.LocalizedName`（item vtable `+0x1a8`，后跟 jumptable） | ✅ **是** —— 与基类 `GetItemName` 同义（这些 drawer 不改名字，直接返回 item 本地名）；ICF 是因为它们与「任何返回 `item.LocalizedName` 的 lambda」机器码相同 |
| `<>f__AnonymousType31.GetHashCode`（ForgePointIconDrawer） | `return 0` | ✅ 合理 —— icon drawer 不显示名，返回 `null/空` |
| `CustomDropTableDefinition.get_LocalizedName`（RandomCardIcon / TitleIcon，2 条） | `return **(DAT+0xb8)`（读全局静态配置的字段） | ⚠️ **语义对上了「返回一个名字」，但它是「固定/静态配置名」，不是逐 `item`** —— 这两个 icon drawer 的 GetItemName 大概率来自某全局配置。要精确到「读哪个配置」需 VA 反汇编那位（本轮未做） |

> 复核结论：**8 条 ICF 折叠的体都与「返回 item / 静态名」自洽，没有一条是「解错成别的方法」**（ICF = 链接器把相同机器码折叠；首行是他名 ≠ 解错）。
