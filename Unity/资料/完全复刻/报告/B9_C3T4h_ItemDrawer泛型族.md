# B9_C3T4h · ItemDrawer 泛型族（解 #9 / #12 两条「判据缺」）—— 报告

> 子代理 **C3T4**（查判据型 · 只读）· 2026-10-27
> 目标：把 `收藏.tsv` 里两条「判据缺」—— `#9 ItemDrawer.Draw(ObtainableItem,int,ItemDrawerOptions)`、`#12 ItemDrawer.GetItemName(T)` —— 用泛型重导路解出来。
> 产出：`判据/ItemDrawer泛型族_判据.md` · 本报告。

## 一、先说最重要的一条：**不用重跑 Ghidra**

泛型全量导出（G7）**已经做完了**：`D:/2/tools/decomp_generic_trial/out/` 有 **640 个 `.c`**（G6 28 + G7 612），索引 `out/_MANIFEST_generic_all.tsv`（615 行），`ItemDrawer<object>` **28/28 方法全在**。本轮**直接读产物**，`-readOnly` / 唯一后缀 / 不删 ghidra_proj 这些约束因此**没有被触碰**（没起 Ghidra）。

## 二、最关键纠错：`#9` 的「20+ NoteDecompilerIssue」是 **ILSpy** 的噪声

- ILSpy `out_cpp2il_dev_ilspy/ItemDrawer.cs`：**168 处** `NoteDecompilerIssue`（账里记的「20+」就是从这来的）。
- **Ghidra 泛型产物** `out/ItemDrawer_object___Draw__1ae9cc0.c`：**155 行、`NoteDecompilerIssue` = 0** ⇒ **完全可读**。
- 结论：`#9` 从「判据缺」应改判为「**可读**（Ghidra）」—— 这正是 CLAUDE.md「判读不出来先看 Ghidra `.c`，别拿 ILSpy 的 `IL_xxxx` 注解当证据」那条教训的又一实例。

## 三、`ItemDrawer<object>__Draw` 读出的语义（155 行，逐段）

签名（`.`）：`void ItemDrawer<object>__Draw(this, item, int amount, options, ...)`。逐段：
1. 先调 `this` 的两条虚方法（vtable `+0x1d8` / `+0x1e8`，都传 0）= **复位两件子控件**（`Toggle*` 那一族）。
2. 写 `item`（this+0x38）/ `options`（this+0x28）/ `amount`（int，this+0x40）。
3. 取 item 侧「类型→label」链（`(+0x20)+0xc0`）拿到 **label 组件**，`label.setText(amount.ToString())`（label vtable `+0x558`）。
4. 取 image 链（`(+0x28)+0xc0`）拿 **image** → `SetActive(true)` → 调 `this` 虚方法 `+0x278`（取图标 sprite）→ `image.sprite = …`（image vtable `+0x558`）；再取 label 文字，`IsNullOrEmpty`：
   - **非空** ⇒ 调子类 hook `this+0x288`（传 `item, amount`）后收尾；
   - **空** ⇒ `image.gameObject.SetActive(false)`。
5. 收尾那两跳（`this+0x1a8`/`+0x1b8` 复位）也读得出。

**剩余（不是没判据，是没反查）**：`+0x1d8 / +0x1e8 / +0x278 / +0x288` 四个 **vtable 槽的具体目标**要按 `ItemDrawer<T>` 的 vtable 表反查（未做）。

## 四、`GetItemName` 基类 + 23 子类（逐条读出）

- **基类 `ItemDrawer<object>.GetItemName`**：体 = **`return item.LocalizedName`**（委托 item 的虚方法 `item vtable +0x1a8`）。⚠️ 该地址与 `MissionsTab.<>c.<RefreshRefillCount>b__9_0` **ICF 同码**（首行是后者；**体是对的**）。
- **23 个子类 `xxxDrawer.GetItemName` 的返回值规则**（全部读出，详见 `判据/ItemDrawer泛型族_判据.md` §二 的表）：
  - **返回序列化 I2 `LocalizedString` 字段**（7 条）：AvatarBorder(+0x58) · Avatar(+0x58) · CampaignPoint(+0x60) · ExpansionPassPoint(+0x60) · ForgePoint(+0x60) · RandomCard(+0x78) · Title(+0x68)。
  - **条件选两档**：ContainerDrawer `(+0x40==1) ? +0x58 : +0x70`。
  - **静态常量串**（空）：CardDrawer · CardAlternateArtDrawer · CosmeticDrawer。
  - **`Currency.DisplayName`**：CurrencyDrawer。
  - **`Loc.GetTranslation("PrebuiltDecks/" + deckId)`**：DeckDrawer（已解，与账一致）。
  - **`Format(LocalizedString, CardRarityToString(rarity))`**：WildcardDrawer。
  - **抛 `NotImplementedException`**：WildcardIconDrawer（**原版这档未实现**）。
  - **ICF 折叠（8 条，体可读、首行是他名）**：AllianceBadge / Cardback / ExpansionPassPremium / Premium / XSollaOffer 折进 MissionsTab lambda；ForgePointIcon 折进匿名类型 `GetHashCode`（`return 0`）；RandomCardIcon / TitleIcon 折进 `CustomDropTableDefinition.get_LocalizedName`。

## 五、结论 + 残余

- **#9 `ItemDrawer.Draw`**：判据**可从「判据缺」改判「可读」**；残余 = 4 个 vtable 槽目标未反查 + 逐跳核。
- **#12 `ItemDrawer.GetItemName`**：基类体「可读」（=`item.LocalizedName`），23 子类规则**全部读出**；残余 = ICF 折叠的 8 条若需精确到「取 item 哪个字段」再 VA 反汇编（本轮未做）。
- **顺带**：`ItemDrawer<object>` 其余 26 个方法也都在（`out/` 里），账里其它 ItemDrawer 泛型族「判据缺」都能按同一读法解。
- 说明：本轮**只读、未写 `Assets`、未起 Unity、未动 git、未改正本/主表**，也没删/改 `D:/2/tools/ghidra_proj/`。

---

## 第二批（2026-10-27 · C3T4h 续）：vtable 槽 + 23 子类 Draw + 8 条 ICF 复核

（仍只读既有产物，未起 Ghidra。逐条详表 → `判据/ItemDrawer泛型族_判据.md` §二·甲/乙/丙。）

### 1. `Draw` 的 4 个 vtable 槽（2 坐实 + 2 候选）
- **`+0x278` = `GetItemName(T)`（Slot 20）—— 坐实**：返回值是 **string**、被写进 name 文本（TMP set_text），随后再读该文本判 `IsNullOrEmpty` ⇒ 是「取名字」不是取 Sprite。
- **`+0x288` = `Draw(T,int)`（Slot 21，abstract 子类钩子）—— 坐实**：实参 = `(item, int)`；23 子类 override 的就是它。
- **`+0x1d8` / `+0x1e8` = 两个「复位」虚调用（传 0）—— 未坐实**（候选 `ToggleClaimedWarning/TogglePremiumHighlight/ToggleHighlight/ToggleLock/SetEphemeralDisplay` 中的两个）。原因：Ghidra 偏移是相对**运行期 vtable 指针**，`dump.cs` 的 `Slot:` 是相对**类型方法表**，两者基址（MonoBehaviour 那串继承虚方法）无现成对照 ⇒ 不能 `offset/8` 反推。要坐实需 ItemDrawer<T> 运行期 vtable 数组 / VA 反汇编，本轮未做。

### 2. 23 个子类 `Draw(T,int)` 覆写表（全读出）
已逐类给出「覆写哪几步 + 与基类差」（表在判据文件 §二·乙）。要点：
- **只设 `image.sprite`**：AvatarBorder / Cardback / Cosmetic（=`CosmeticItem.Image`/`CosmeticSprite`）
- **图标 + 阵营名**：Avatar(多 `label.set_text(ArmyName)`) / ExpansionPassPremium(背景图+name) / Premium(背景+name) / Title(图标+称号图+称号名)
- **稀有度背景 + 阵营图标 + name**：RandomCard（+两件显隐）/ Wildcard（背景=list[rarity-1]）
- **委托 `DrawForArmy(army)`**：CampaignPoint / ForgePoint
- **特殊**：Deck(阵营图标+英雄图+模式图标+enabled) · Container(容器图+阵营图标件) · CardAlternateArt(异画克隆卡资源) · AllianceBadge(默认奖杯兜底+SetTierFrame) · XSollaOffer(开关两件+固定文案) · WildcardIcon(仅图+越界兜底) · **RandomCardIcon / TitleIcon = no-op（体折进空方法 `NetworkingPeer.OnMessage`）**
- 旁支 5 个（DeckAndCardback / GenericArmyItem / PremiumIcon / ItemDrawer 静态工厂 / GizmoTypeDrawer）也读到了，非这 23。

### 3. 8 条 ICF 折叠 `GetItemName` —— 逐条确认体语义
- **5 条**（AllianceBadge / Cardback / ExpansionPassPremium / Premium / XSollaOffer）折进 MissionsTab lambda：体 = `return item.LocalizedName`（item vtable +0x1a8）⇒ **确实是 GetItemName 该有的**（不改名，等同基类）。**不是解错**。
- **ForgePointIcon** 折进匿名 `GetHashCode`：`return 0` ⇒ 空名（icon 不显示名），合理。
- **RandomCardIcon / TitleIcon** 折进 `CustomDropTableDefinition.get_LocalizedName`：`return **(DAT+0xb8)` = **固定/静态配置名**，不是逐 item；要精确到读哪个配置需 VA。
> 复核结论：8 条 ICF 的体都与「返回 item/静态名」自洽，**没有一条是解错成别的方法**（ICF = 链接器折叠相同机器码，首行他名 ≠ 解错）。

### 4. 顺带：`资料/全量反编译_入口与用法.md`「泛型类方法体的导出」节 —— **建议补一句**
现在那节有「G7 全量已完成 ok=612/fail=3」和「产物在 `decomp_generic_trial/out/`」，但**没醒目标出「此后直接读、别重跑」+ 索引文件名 + `name_class` 列含义**。建议原文（你落）：

> ✅ **可直接读、⛔ 别重跑**：产物 `D:/2/tools/decomp_generic_trial/out/`（640 个 `.c`）；索引 `out/_MANIFEST_generic_all.tsv`（615 行，列 `addr / requested_name / file / topline_name / name_class`）。`name_class = ICF_other_name` = **同码折叠**（体是对的、首行是别人名，⛔ 别当解错）。

## 第二批残余（如实）
- `0x1d8/0x1e8` 两个复位槽未坐实（需 ItemDrawer<T> 运行期 vtable / VA）。
- RandomCardIcon / TitleIcon 的 GetItemName 是「静态配置名」，要精确到哪个配置需 VA。
- 23 子类 Draw 表已全读出，但**逐条与「我们 ItemSpec 数据层」的等价映射未做**（我们不走逐子类 override）。
