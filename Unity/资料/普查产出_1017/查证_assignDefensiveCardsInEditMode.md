# 查证 · `assignDefensiveCardsInEditMode` 是什么（2026-10-17 只读现核）

> 执行：**只读调查代理** · 主对话代落盘。触发：用户 2026-10-17 问「**冲突模式 · `assignDefensiveCardsInEditMode` 两个模式各取什么值。这个是什么？**」
> 全部现读：`d:/2/tools/decomp_full/`（**按函数体内偏移 grep，不按文件名**）· `d:/2/tools/il2cpp_out/{dump.cs,il2cpp.h}` · 签名桩 · `d:/2/新解包资源/assets_full/` · 我们自己的代码。

---

## 一、它是什么（一句话）

**服务器下发的 LiveOps 事件参数**里的一个 `bool`，**唯一作用 = 决定「防御卡」要不要出现在卡组编辑器的左侧卡池里**（`true` = **出现**）。
**它是个只读一处的【显示开关】** —— **既不校验卡组、也不发牌**。

- 字段：`Everguild.LiveOps.GameplayVariablesData.assignDefensiveCardsInEditMode`（**bool，偏移 0x4A，18 个字段里的第 17 个**）
  `dump.cs:130170-130191` · `il2cpp.h:118177` · 桩 `Everguild/LiveOps/GameplayVariablesData.cs:35`
- 宿主：`PlayEventData.gameplayVariables`（**+0x80**），**随 LiveOps 事件载荷下发**

## 二、它在原版怎么用（全客户端**只有一个读点**）

`decomp_full/DeckEditingWindow___GetCardCollection_b__32_1.c:35`
```c
return *(undefined1 *)(*(longlong *)(lVar2 + 0x80) + 0x4a);
```
它是 `DeckEditingWindow.GetCardCollection` 里 5 个 `Where` 之一（`.Where` = 谓词**真则保留**）。谓词逐行读出来：

| 分支 | 条件 | 结果 |
|---|---|---|
| ① | `card.inventoryOptions(+0x4c) != CantAddToDeck(5)` | `return 1` ⇒ **一律保留（根本不碰这个开关）** |
| ② | 不能进卡组、且 `card.spellType(+0x90) != DefensiveCard(210)` | `return 0` ⇒ **剔除** |
| ③ | 不能进卡组、且**是**防御卡 | `return assignDefensiveCardsInEditMode` |

🔴 **关键**：这个开关**只对「被标了 `CantAddToDeck(5)` 的卡」起作用**（`inventoryOptions != 5` 在第一行就 return 1 了）⇒ 管的是**极窄的一格**，**不是**「编辑卡池的全部防御卡」。

🔴 **另一件（账上没写）**：那个 lambda 里还夹着一句 `UnityEngine.Debug__Log(bool)`（`:24-31`，记录「这张卡是不是 DefensiveCard」）—— **原版留下的调试日志**，说明这段当年正在被调试。

取事件的那条链：`editingDeck(+0x118).CustomGameModeEvent` → `CardDeck.get_CustomGameModeEvent` → `LiveOpsManager.GetHandler` + `GameModes.GetActiveEvent(deck.gameMode @0x70)`。

## 三、🔴 「两个模式各取什么值」这个问法本身有问题

| 账上写的 | 实际（现核） |
|---|---|
| 「冲突模式 · **两个模式**各取什么值」 | **原版没有「两个模式」这个概念** —— 是**每个进行中的 LiveOps 事件各带一份** `GameplayVariablesData`；一副卡组按自己的 `gameMode`（`CardDeck.gameMode` @0x70，`Nullable<PlayModes>`）去反查「当前有没有这个模式的活动事件」。**「经典 / 冲突两份」是我们自己的建模**（`GameplayVariables.Classic/Skirmish`） |
| 「18 个值全在服务器」 | 对，而且**更进一步**：客户端**从来没有构造过这个类型** —— `GameplayVariablesData..ctor` 是**空桩**（RVA `0x4B3000`，该地址被 **4494 个类共用** = 什么都不做的默认 ctor）⇒ 就算 `new` 出来，18 个字段也是 **0 / false**；全量反编译里**引用过这个类型的文件只有 2 个**（这个 ctor + `GetMaxCopiesInDeck`），**没有一处带字面量赋值** |

⇒ 所以「两个模式各取什么值」**在原版客户端是个不存在的量**：经典那副卡组**根本没有 custom game mode event**。

**没有兜底、没有默认值**：反编译里这条路走不通时的落点是 `FUN_1803f47a0()`，注释写着 `WARNING: Subroutine does not return`（IL2CPP 的空引用抛出路径）—— **取不到事件载荷就直接抛，不会退化到某个默认档**。
（同形状见 `CardDeck.get_MaxDeckSize.c`（读 +0x24）与 `CardDeck.CanAddCard.c`（读 +0x10）—— 说明「经典模式无事件」在原版这些读点上**本来就是异常态**。**这条是读反编译得的，未实机验证**。）

**本地唯一能看到它的地方 = 冲突模式的说明文案**（逐字，`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_2253370867314273096.json`）：
> `12-card decks · Up to 4 Legendaries (Warlord not included) · Warlords start with 10 less Health · Player 1 starts at 3 Energy, Player 2 at 4 Energy · Gain +2 Maximum Energy each turn · Overtime begins earlier · No Offence card. Random Defence card. No mulligan. Jump straight into action!`

⚠️ 那句 `Random Defence card` **不是这个字段的证据**（我方 §2.7c 已判：它讲的是**战前发放**，不是组卡）⇒ **本地看不到它「哪一档」的表现**。

## 四、我们的现状

| 处 | 内容 |
|---|---|
| `RuleEngine/Core/GameplayVariables.cs:222` | 声明，**默认 `false`** |
| `:271` | 给 `Skirmish` 赋 **`true`**（**`Classic` 保持 `false`**） |
| 🔴 **读者数** | **全仓 = 0**（grep `assignDefensiveCardsInEditMode\|AssignDefensiveCards` 全 `MyGame/Assets`：只有那 2 行，**无任何读取点**） |

⇒ 这在我们这儿是**只写不读的死字段**；我们两种模式都列防御卡，**是因为我们的编辑器根本不看它**，**不是**「两个实例都当 true 用」。

⚠️ **两处账目与实现对不上（本轮已就地订正）**：
- `资料/加时与冲突模式_原版规格.md:788-790` / `:765` 写「两个实例**都当 true 用**」—— 实际 `Classic=false / Skirmish=true` ⇒ **行为对、理由写错**
- `GameplayVariables.cs:217` 的注释又写「遭遇 = true（原版文案 `Random Defence card`…）」，那是**两种读法里的读法②**，与「不臆造值」的口径自相矛盾

**顺带读清一条（账上原来记错）**：`FixSkirmishDeck`（`decomp_full/PlayerDataManager._FixSkirmishDeck_d__466__MoveNext.c`）= 把 `Skirmish` 卡组（读 `defensiveCard +0x48` 的 uniqueId）打包成 `AzureDeckInfo` 列表 → `PlayfabWrapper.GenericCloudScriptHandlerAsync(id 0x45d)` **上传服务器**，**不写卡组、不分配防御卡** ⇒ **它不是「冲突模式卡组从哪来」的答案**。

## 五、结论：要用户拍板什么

- **当前功能上不需要拍板** —— 我们的编辑器不读这个字段，改不改都不影响任何现有行为。
- 它**只有在两个前提同时成立时才会咬人**：①「玩家在编辑器里自建冲突卡组」那条路做起来（`DeckEditorState.Skirmish` 目前**全仓无赋值**）；②我们把原版那个 `inventoryOptions == CantAddToDeck(5)` 过滤也做出来（我们**没做**，用的是 `DeckRules.IsEffectOnly(Subtype)` = 用户 2026-09-27 拍板的口径）。
- ⇒ **建议把待办改写成「要不要做原版的 `inventoryOptions` 卡池过滤」** —— 那才是真问题。
  **不做 ⇒ 本字段在我们这边恒为惰性**，可以从「判据在远端」那一族里**摘出去**。
  ⚖️ **移交用户拍板**（调查代理不替用户定）。

## 六、没查清的部分

1. **两个模式的真值** —— 服务器侧，本地确实没有。**结论：不可能在本地解出，别再花时间。**
2. 🔴 **新暴露的一条关键未知**：**防御卡的 `inventoryOptions` 到底是不是 5**（这决定这个开关**在原版里是不是空转**）。
   - `RawCardScript.inventoryOptions`（`dump.cs:23250`）是**卡资产上的公有字段**（`RawCardScript.cs:15`，**没有 getter/setter**）；全量反编译里**找不到任何运行时写 `+0x4c` 的卡上下文写点**（`grep "0x4c) = "` 命中的全是别的类）⇒ 它是**资产数据**。
   - 而**逐卡真值本地没有**：`inventoryOptions` 在 `assets_full/`、`解包整理/` **0 命中**（13 个 `*cardassets*` 包里只有 Sprite/AudioClip/Texture2D）—— 与 `普查产出_1011/WB1_A330.md:45` 一致。
   - ⇒ **「防御卡是不是被标成 5」在原版本地判不了**；若它是 `InInventory(0)`，这个开关在原版**基本是空转**的（那意味着「两个真值」本来就不重要）。
3. 「无运行时写点」是**按偏移 grep 的间接结论**（`0x4c` 被几十个类共用），**不是 xref** ⇒ 标为**未坐实**。

## 七、搜过的词 / 范围（铁律 2）

**词**：`assignDefensiveCardsInEditMode` · `AssignDefensiveCards` · `GameplayVariablesData` · `gameplayVariables` · `GameplayVariables` · `showMulligan|alternativeTraitLogic|warlordLifeChange` · `inventoryOptions` · `CantAddToDeck` · `CardInventoryOptions` · `DefensiveCard` · `SpellType` · `FixSkirmishDeck` · `CustomGameModeEvent` · `GetActiveEvent` · `CanAddCard` · `CanAddDefensive` · `0x4a` · `0x4c) = ` · `Random Defence card` · `Random Defense`

**范围**：`d:/2` 全盘（`tools/il2cpp_out/{dump.cs,il2cpp.h,script.json}` · `tools/decomp_full/`（**按函数体内偏移 grep，不按文件名**）· `tools/{wanted_full,all_methods}.txt` · `新解包资源/assets_full/`（含 `_stats.json`、`globalgamemanagers/MonoScript/MonoScript_4215.json` = **只有类名无值**）· `解包整理/` · `Warpforge_tools/data/` · `Warpforge_code/Scripts/Assembly-CSharp/`）；`d:/4/Unity/{资料,MyGame/Assets}`。

`0x4a` 全量 grep：**唯一**「经 +0x80 读 0x4a」的就是那个 lambda；其余 0x4a 命中属 **AnimFXModuleTransformModifier / DeckListDrawer / BaseOfferPopup / InAppOfferContainer**（**不同类**，非此字段）。

⚠️ **未搜**：`d:/4/Unity/数据/`（该目录下全盘 grep **超时**、移到后台**未取回结果**）—— 如需可补跑一次。
