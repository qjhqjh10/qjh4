# AUD07 补丁包 · BattleDriver / DeckRuntime / 自检宿主

> 由 `B103_R3` 生成（2026-10-30）。事实来源 = 同目录 `AUD07_loc_keys.tsv`（逐条判定）与 `AUD07_loc_keys.md`（结论）。
> ⛔ 本文件只是**补丁清单/可粘贴代码**，本代理**没有改**这些文件（白名单之外）。
> 🔴 **行号会漂**（`Editor/MainMenuScene.cs` 此刻就在别人手上改）⇒ 一律按**锚文本 / 键名**定位，⛔ 别用行号。
> 改法统一：把该行里的 `Loc.HasEntry(` 换成 `Loc.HasAnyTerm(`，**行数不变**。若该行有行末 `//` 注释，替换只动谓词、别动注释。
> 🔴 **2026-10-30 状态更新（第 3 版 · **全部落地**）**：**§一（`BattleDriver` 5 处）✅** · **§二（`DeckRuntime:6022`）✅ 已与 `DeckScene:7184` 同刻改** · **§三 宿主 103 处 ✅** · **§五 5 条镜像断言 ✅ 全部改完**。⛔ 本文件现在**只是留档**，别再照它重复改。

## 一、`Battle/BattleDriver.cs`（5 处，全部改宽）

| # | 锚（该行） | 改后 | 说明 |
|---|---|---|---|
| 1 | `if (Loc.HasEntry(HandFullTerm)) return CardText.Term(HandFullTerm);` | `if (Loc.HasAnyTerm(HandFullTerm)) return CardText.Term(HandFullTerm);` | `HandFullText`：手牌已满提示；兜底是硬编码句。else 分支本身没有 `Loc.T` 出声（原实现就没有），保留不动。 |
| 2 | `if (Loc.HasEntry(key)) return string.Format(CardText.Term(key), args);` | `if (Loc.HasAnyTerm(key)) return string.Format(CardText.Term(key), args);` | `LogTerm`：**下一行 `Loc.T(key);` 是缺键出声的唯一通道 —— ⛔ 必须保留**（就在 `if` 之后那句）。 |
| 3 | `return (key != null && Loc.HasEntry(key)) ? Loc.T(key) : RuleCodes.Describe(rc);` | `return (key != null && Loc.HasAnyTerm(key)) ? Loc.T(key) : RuleCodes.Describe(rc);` | `HintForCodeWithKey`：⚠️ **镜像断言 `Editor/BattleScene.cs`** 的 `bool costInTable = Loc.HasEntry(keyCost);` 与 `Check(Loc.HasEntry(keyInTable), …)` 两条必须**同刻**改成 `HasAnyTerm`，否则一红一绿。 |
| 4 | `string s = (key != null && Loc.HasEntry(key))` | `string s = (key != null && Loc.HasAnyTerm(key))` | `DamageFatigueTextWithKey`：⚠️ **镜像断言** `BattleScene` 里 `Check(Loc.HasEntry(keyInTable), "★ 前提：词条 … 在语言表里")` 与 `Check(!Loc.HasEntry("Battle/Tips/__SelftestNotInTable__") && viaFallback != viaTerm …)` 同刻改。 |
| 5 | `return Loc.HasEntry(GuestTerm) ? Loc.T(GuestTerm) : "";` | `return Loc.HasAnyTerm(GuestTerm) ? Loc.T(GuestTerm) : "";` | `PlateName`：名牌兜底 `MenuLogin/Guest`（今天在 Table+官方全表 ⇒ 行为不变）。 |

> 🔴 改完 `BattleDriver` 后，**同一批**必须改 `Editor/BattleScene.cs` 的 4 条镜像断言（同一谓词造期望值）：
> `bool costInTable = Loc.HasEntry(keyCost);` · `Check(Loc.HasEntry(keyInTable), …)`（两处文字不同：一处是「（前提）…在 `Loc` 表里（它有值 ⇒ 能验有键态）」、一处是「★ 前提：词条 … 在语言表里」）· `Check(!Loc.HasEntry("Battle/Tips/__SelftestNotInTable__") …)`。

## 二、`Deck/DeckRuntime.cs`（1 处）

```csharp
// MenuDeckErrorKey(DeckError e) 末句：由
return Loc.HasEntry(key) ? key : MenuDeckErrorKeyFallback;
// 改为
return Loc.HasAnyTerm(key) ? key : MenuDeckErrorKeyFallback;
```
- 语义：这里选的是「**用哪个键**」；键在 bulk/官方全表里也算「取得到」⇒ 宽化才对得上原版 `ToRawLocalizationString` 的「表里查不到才换兜底键」。
- 现读 `MenuDeck/Error/{2,4,5}` **三档皆无** ⇒ 宽化后**行为零变化**（仍落 `MenuDeck/Error/InvalidDeck`）。
- ⚠️ **镜像断言** `Editor/DeckScene.cs`：`CheckTrue(!Loc.HasEntry(numKey5), …)`（「真前提：数字键不在 Loc 表里」）要**同刻**改成 `CheckTrue(!Loc.HasAnyTerm(numKey5), …)` —— 更忠实地表达「官方也没有这条键」。

## 三、自检宿主（`Editor/*.cs`）· 共 108 处该宽 / 28 处该窄（⛔ 别动）

> 全部改法相同：**该行 `Loc.HasEntry(` → `Loc.HasAnyTerm(`，行数不变**。下面每一条给的是**锚文本**（去重靠它），行号故意不写。

### `Editor/MainMenuScene.cs`（调用点 36：宽 34 / 窄 2）

**改宽**（34）：
- `CheckTrue(Loc.HasEntry("MainMenu/RankedWindow/ChangeDeck"),`
- `CheckTrue(Loc.HasEntry("Demo/CardCostBalance"), "（前提）词条 `Demo/CardCostBalance` 在表里");`
- `CheckTrue(Loc.HasEntry("MainMenu/MainButtons/ButtonLabel/Back"),`
- `CheckTrue(Loc.HasEntry("Demo/ToBattle"), "（前提）词条 `Demo/ToBattle` 在表里");`
- `CheckTrue(Loc.HasEntry("Demo/DeckSelectionDemo/SelectDeck"), "（前提）词条 `Demo/DeckSelectionDemo/SelectDeck` 在表里");`
- `CheckTrue(Loc.HasEntry("Event_Description/Generic/GameMode"), "（前提）词条 `Event_Description/Generic/GameMode` 在表里");`
- `CheckTrue(Loc.HasEntry("Demo/AvatarMenu/SelectAvatar"),`
- `CheckTrue(Loc.HasEntry("MainMenu/Register/PleaseChooseName"),`
- `if (Loc.HasEntry(achKey))`
- `CheckTrue(i < socialTabTerms.Length && Loc.HasEntry(socialTabTerms[i]),`
- `CheckTrue(Loc.HasEntry("SocialMenu/Alliances/Join") && Loc.HasEntry("SocialMenu/Alliances/Create"),`
- `CheckTrue(Loc.HasEntry("MenuDeck/HUD/SearchFilter"),`
- `CheckTrue(Loc.HasEntry("SocialMenu/Alliances/OpenAlliances"),`
- `CheckTrue(Loc.HasEntry("SocialMenu/Alliances/CreateAlliance"),`
- `CheckTrue(Loc.HasEntry("SocialMenu/Alliances/NameInput")`
- `&& Loc.HasEntry("SocialMenu/Alliances/DescriptionInput"),`
- `CheckTrue(Loc.HasEntry("SocialMenu/Alliances/SelectLanguage")`
- `&& Loc.HasEntry("SocialMenu/Alliances/SelectPrivacy"),`
- `CheckTrue(Loc.HasEntry("SocialMenu/Alliances/Dismiss"),`
- `CheckTrue(Loc.HasEntry("Demo/FriendsMenu/EnterPlayerName"),`
- `CheckTrue(Loc.HasEntry("Demo/FriendsMenu/SearchPlayer"),`
- `CheckTrue(Loc.HasEntry("Demo/FriendsMenu/YourFriends"),`
- `CheckTrue(Loc.HasEntry("SocialMenu/Alliances/Members")`
- `&& Loc.HasEntry("SocialMenu/Alliances/Ranking"),`
- `CheckTrue(Loc.HasEntry("Settings/General/Title")`
- `&& Loc.HasEntry("SocialMenu/Alliances/Trophies"),`
- `CheckTrue(Loc.HasEntry("SocialMenu/Alliances/Trophies/NextTier"),`
- `CheckTrue(Loc.HasEntry("SocialMenu/Alliances/Trophies/FeaturedTrophyLabel"),`
- `CheckTrue(Loc.HasEntry("MainMenu/MainButtons/ButtonLabel/Back"),`
- `CheckTrue(Loc.HasEntry("MenuDeck/HUD/SearchFilter"),`
- `CheckTrue(Loc.HasEntry("SocialMenu/Alliances/JointToEarnRewards")`
- `&& Loc.HasEntry("MainMenu/RankedWindow/Leaderboard"),`
- `if (Loc.HasEntry(key)) return Loc.T(key);`
- `if (Loc.HasEntry(key)) return Loc.T(key);`

**⛔ 保留窄（2）**：
- `CheckTrue(Loc.HasEntry(k), $"★（A1025）本批键都在表里：`{k}`");` — A1025 族：后面 ③ 用 Loc.EnOf ⇒ 必须窄
- `if (!Loc.HasEntry(k)) continue;      // 官方有、我们没建这个键 ⇒ 不在本条范围` — 覆盖度统计：只统计「官方有、我们手写表也建了」的键（比 ZhOf 与官方 zh 列）；宽化会把统计口径改成「只要官方有」⇒ 自我循环

### `Editor/BattleScene.cs`（调用点 29：宽 22 / 窄 7）

**改宽**（22）：
- `Check(Loc.HasEntry(ChoosePanel.TitleTerm),`
- `Check(Loc.HasEntry(ChoosePanel.SelectTerm) && Loc.HasEntry(ChoosePanel.TitleTerm),`
- `bool costInTable = Loc.HasEntry(keyCost);`
- `Check(Loc.HasEntry(keyInTable), $"（前提）`{keyInTable}` 在 `Loc` 表里（它有值 ⇒ 能验有键态）");`
- `Check(Loc.HasEntry(WaitBanner.WaitTerm),`
- `Check(Loc.HasEntry("Battle/Effect/ChangeMeleeAttackOneTurn"),`
- `Check(Loc.HasEntry("Battle/HUD/TargetsAvailable"),`
- `Check(Loc.HasEntry(SettingsPanel.ResignTerm),`
- `&& Loc.HasEntry(SettingsPanel.AutoZoomTermKey)`
- `Check(Loc.HasEntry(k), $"★ §W6（坑表 #18）`Loc` 表里**真有** `{k}`");`
- `Check(Loc.HasEntry(BattleDriver.OvertimeTerm),`
- `Check(Loc.HasEntry("Battle/Tips/DamageFatigue")`
- `&& Loc.HasEntry("Battle/Tips/DamageFatigueEnemy"),`
- `Check(Loc.HasEntry(keyInTable), $"★ 前提：词条 `{keyInTable}` 在语言表里");`
- `Check(!Loc.HasEntry("Battle/Tips/__SelftestNotInTable__") && viaFallback != viaTerm`
- `Check(Loc.HasEntry(MulliganPanel.UndoTerm),`
- `Check(Loc.HasEntry(MulliganPanel.PromptTerm) && Loc.HasEntry(MulliganPanel.ReplaceTerm)`
- `&& Loc.HasEntry(MulliganPanel.DoneTerm) && Loc.HasEntry(MulliganPanel.TurnFirstTerm)`
- `&& Loc.HasEntry(MulliganPanel.TurnSecondTerm) && Loc.HasEntry(MulliganPanel.UndoTerm),`
- `Check(Loc.HasEntry(ChoosePanel.SelectTerm) && Loc.HasEntry(ChoosePanel.TitleTerm)`
- `&& Loc.HasEntry(MulliganPanel.DoneTerm),`
- `Check(Loc.HasEntry(TutorialOverlay.ContinueTerm),`

**⛔ 保留窄（7）**：
- `if (!Loc.HasEntry("Battle/Tips/" + k)) missTips.Add(k);` — 注册表完备哨兵：期望值写死「缺 CantAttackCoward/CantAttackTainted」，判的是手写表登记面（官方同样没有那两条）
- `if (Loc.HasEntry(hintPrefix + deadHintKeys[i])) stillThere.Add(deadHintKeys[i]);` — 删除哨兵：判「我们自拟的 Battle/Hint/* 已从手写表删掉」；紧邻的 44837-44842 已另断「官方 13 语言表按前缀 0 条」⇒ 两档分开断，别合并
- `if (Loc.HasEntry(logPrefix + deadLogKeys[i])) stillThere.Add(deadLogKeys[i]);` — 删除哨兵：判「我们自拟的 Battle/Log/* 已从手写表删掉」；紧邻的 44837-44842 已另断「官方 13 语言表按前缀 0 条」⇒ 两档分开断，别合并
- `Check(!Loc.HasEntry("Battle" + "/Settings/Title")` — 删除哨兵：判「W62 那四条配套键没被建回我们的手写表」
- `&& !Loc.HasEntry("Battle" + "/Settings/AiDifficulty")` — 删除哨兵：判「W62 那四条配套键没被建回我们的手写表」
- `&& !Loc.HasEntry("Battle" + "/MultiCard/TapToClose")` — 删除哨兵：判「W62 那四条配套键没被建回我们的手写表」
- `&& !Loc.HasEntry("Battle" + "/CardWindow/TapToClose"),` — 删除哨兵：判「W62 那四条配套键没被建回我们的手写表」

### `Editor/DeckScene.cs`（调用点 24：宽 19 / 窄 5）

**改宽**（19）：
- `CheckTrue(Loc.HasEntry("MenuDeck/HUD/SearchFilter"),`
- `CheckTrue(Loc.HasEntry("MenuDeck/Filters/Army"),`
- `CheckTrue(Loc.HasEntry("MenuDeck/HUD/SearchFilter"),`
- `CheckTrue(Loc.HasEntry("MenuDeck/Filters/Army") && Loc.HasEntry("MenuDeck/HUD/Rarity")`
- `&& Loc.HasEntry("MenuDeck/Filters/Type") && Loc.HasEntry("Battle/Tips/EnergyCost"),`
- `CheckTrue(Loc.HasEntry("MenuDeck/HUD/DeckDescription/Minions")`
- `&& Loc.HasEntry("MenuDeck/HUD/DeckDescription/Spells"),`
- `CheckTrue(Loc.HasEntry(kFew),`
- `CheckTrue(Loc.HasEntry(DeckRules.Describe(DeckError.WrongFaction))`
- `&& Loc.HasEntry(DeckRules.Describe(DeckError.EffectOnlyCard)),`
- `CheckTrue(Loc.HasEntry(DeckRuntime.MenuDeckErrorKeyFallback),`
- `CheckTrue(!Loc.HasEntry(numKey5),`
- `CheckTrue(Loc.HasEntry("MenuDeck/NewDeckName"),`
- `CheckTrue(Loc.HasEntry(zhTerm ?? ""),`
- `CheckTrue(Loc.HasEntry(DeckRuntime.TermSaveFailed),`
- `CheckTrue(Loc.HasEntry("MenuDeck/HUD/EditDeckName"),`
- `CheckTrue(Loc.HasEntry("MenuDeck/Filters/Army"),`
- `CheckTrue(Loc.HasEntry("MenuCollection/NoCardsFound"),`
- `CheckTrue(!Loc.HasEntry(ghostKey),`

**⛔ 保留窄（5）**：
- `CheckTrue(Loc.HasEntry(k), $"★（A1025）本批键都在表里：`{k}`");` — A1025 族：后面用 Loc.EnOf（只读 Table）⇒ 必须窄
- `CheckTrue(Loc.HasEntry(kImp), $"★（A1852）`{kImp}` 在表里（不在 ⇒ `Loc.T` 返回键名 = 界面印键名）");` — A1852：紧接两条 Loc.EnOf/Loc.ZhOf（只读 Table）⇒ 必须窄
- `CheckTrue(!Loc.HasEntry("MenuDeck/Error/EffectOnlyCard"),` — 删除哨兵：判「我们那条自造键 MenuDeck/Error/EffectOnlyCard 不在手写表里」
- `try { got = Loc.HasEntry(null); }` — 测的是 HasEntry 自己的契约：null 不抛、返回 false（宽谓词另有 null 守卫，但这条钉的是 HasEntry）
- `CheckTrue(!Loc.HasEntry("MenuDeck/Error/__没有这条键__"),` — 对照哨兵：虚构键查不到 ⇒ 判的是 HasEntry 的实现契约

### `Editor/CollectionScene.cs`（调用点 24：宽 23 / 窄 1）

**改宽**（23）：
- `CheckTrue(Loc.HasEntry(term),`
- `CheckTrue(Loc.HasEntry(eKeys[pi]),`
- `CheckTrue(Loc.HasEntry("MenuDeck/MenuButtons/ImportDeck")`
- `&& Loc.HasEntry("MenuDeck/MenuButtons/CreateDeck"),`
- `CheckTrue(Loc.HasEntry(zTitle) && Loc.HasEntry(zAlli) && Loc.HasEntry(zGlob),`
- `CheckTrue(Loc.HasEntry("MenuCollection/NoDecksFound"),`
- `CheckTrue(Loc.HasEntry("MenuDeck/Error/NoWarlord"),`
- `CheckTrue(Loc.HasEntry("MenuDeck/Error/NoWarlord"),`
- `CheckTrue(Loc.HasEntry("MenuDeck/Error/HiddenCards"),`
- `CheckTrue(Loc.HasEntry("MenuDeck/Error/ImportEmpty")`
- `&& Loc.HasEntry("MenuDeck/Error/ImportBadString"),`
- `CheckTrue(Loc.HasEntry("MainMenu/MainButtons/ButtonLabel/Back"),`
- `CheckTrue(Loc.HasEntry("MenuDeck/Filters/Army") && Loc.HasEntry("MenuDeck/HUD/Rarity")`
- `&& Loc.HasEntry("MenuDeck/Filters/Type") && Loc.HasEntry("Battle/Tips/EnergyCost"),`
- `CheckTrue(Loc.HasEntry("MenuDeck/HUD/SearchFilter"),`
- `CheckTrue(Loc.HasEntry("Item_Description/CardbackGeneric"),`
- `CheckTrue(Loc.HasEntry("MenuCollection/Label/Cosmetics"),`
- `CheckTrue(Loc.HasEntry("MenuDeck/Filters/Filters"),`
- `CheckTrue(Loc.HasEntry("MenuDeck/Filters/ClearFilters"),`
- `CheckTrue(Loc.HasEntry("MenuDeck/Filters/ShowOwnedOnly"),`
- `CheckTrue(Loc.HasEntry("MainMenu/Chat/TypeMessage"),`
- `CheckTrue(Loc.HasEntry("MenuDeck/Error/ImportEmpty")`
- `&& Loc.HasEntry("MenuDeck/Error/ImportBadString"),`

**⛔ 保留窄（1）**：
- `CheckTrue(Loc.HasEntry(k), $"★（A1025）本批键都在表里：`{k}`");` — A1025 族：后面 ③ 用 Loc.EnOf（只读 Table）⇒ 必须窄；同族 DeckScene:1266 / MainMenuScene:16917 / SettingsScene:5584 / ShellScene:8249 / ShopScene:5796

### `Editor/SettingsScene.cs`（调用点 7：宽 2 / 窄 5）

**改宽**（2）：
- `CheckTrue(Loc.HasEntry(probes[i].Key),`
- `CheckTrue(Loc.HasEntry("Settings/Online/TitleNoteBody"),`

**⛔ 保留窄（5）**：
- `CheckTrue(Loc.HasEntry("Settings/General/Title")` — 「键名照原版 Localize.mTerm」的登记哨兵：判的是我们手写表里登记的是原版键名（不是「取不取得到文案」）
- `&& Loc.HasEntry("MainMenu/Settings/LanguageName/Simplified Chinese")` — 「键名照原版 Localize.mTerm」的登记哨兵：判的是我们手写表里登记的是原版键名（不是「取不取得到文案」）
- `&& Loc.HasEntry("MainMenu/Settings/ButtonLabel/SelectLanguage"),` — 「键名照原版 Localize.mTerm」的登记哨兵：判的是我们手写表里登记的是原版键名（不是「取不取得到文案」）
- `bool devKeyExists = Loc.HasEntry("Settings/Online/TitleNote");` — 孤儿键哨兵：判「我们表里还留着 Settings/Online/TitleNote 这条开发说明」→ 据此断言它没被画到屏上；宽化会改哨兵语义
- `CheckTrue(Loc.HasEntry(k), $"★（A1025）本批键都在表里：`{k}`");` — A1025 族：后面用 Loc.EnOf ⇒ 必须窄

### `Editor/ShopScene.cs`（调用点 4：宽 3 / 窄 1）

**改宽**（3）：
- `CheckTrue(Loc.HasEntry("MenuShop/RefreshCounter"),`
- `CheckTrue(Loc.HasEntry("MenuCollection/NoDecksFound"),`
- `CheckTrue(Loc.HasEntry(moTerms[mi]),`

**⛔ 保留窄（1）**：
- `CheckTrue(Loc.HasEntry(k), $"★（A1025）本批键都在表里：`{k}`");` — A1025 族：后面用 Loc.EnOf ⇒ 必须窄

### `Editor/ShellScene.cs`（调用点 3：宽 2 / 窄 1）

**改宽**（2）：
- `foreach (var n in need) if (!Loc.HasEntry(KB + n)) missKey++;`
- `CheckTrue(Loc.HasEntry("Battle/Mulligan/secondTurn") && Loc.HasEntry("Battle/Mulligan/Instructions"),`

**⛔ 保留窄（1）**：
- `CheckTrue(Loc.HasEntry(k), $"★（A1025）本批键都在表里：`{k}`");` — A1025 族：后面用 Loc.EnOf ⇒ 必须窄

### `Editor/CardBaseDemo.cs`（调用点 3：宽 0 / 窄 3）

**⛔ 保留窄（3）**：
- `if (!Loc.HasEntry(CardText.CardNameTermPrefix + c.Name))` — 注册表完备哨兵：判「起始卡每张都建了 Card_Name/* 词条」；官方表 1198 条 ⇒ 宽化会把这条断言变成空转（假绿）
- `foreach (var a in armies) if (!Loc.HasEntry(CardText.ArmyTermPrefix + a)) missA++;` — 同上：判 16 个阵营都建了 Armies/* 词条
- `foreach (var s in printed) if (!Loc.HasEntry(RaceKeyPrefix + s)) missing++;` — 被测实现 CardView.RaceTerm 不吃第三档（bulk→Table→老键），宽化会让「会印的都有词条」假绿

### `Editor/RewardsScene.cs`（调用点 2：宽 2 / 窄 0）

**改宽**（2）：
- `CheckTrue(Loc.HasEntry("Missions/Completed"),`
- `CheckTrue(Loc.HasEntry("MainMenu/Forge/Help") && !string.IsNullOrEmpty(helpOfficial),`

### `Editor/NetSelfTest.cs`（调用点 2：宽 0 / 窄 2）

**⛔ 保留窄（2）**：
- `for (int i = 0; i < keys.Length; i++) if (!Loc.HasEntry(keys[i])) miss.Add(keys[i]);` — P6 双语族：后面 ② 用 Loc.EnOf（只读 Table）⇒ 必须窄（Settings/Online/* 135 条本来也只在 Table）
- `if (Loc.HasEntry(keys[i]) && Loc.T(keys[i]) == keys[i]) c2.Add(keys[i]);` — 同族反档判据（键在 Table 且值==键名）；宽化会让权威取值口掺进第三档

### `Editor/B94B9_Assert.cs`（调用点 1：宽 1 / 窄 0）

**改宽**（1）：
- `Tr(Loc.HasBulk("Card_Trait/markOfKhorne") || Loc.HasEntry("Card_Trait/markOfKhorne"),`

### `Editor/B99A3_Assert.cs`（调用点 1：宽 0 / 窄 1）

**⛔ 保留窄（1）**：
- `Check(Loc.HasEntry(key), $"§E② `Loc` 里登记了 `{key}`（别名键；官方那条叫 `Card_Trait/sneak`）");` — 别名键登记哨兵 + 紧跟 KeywordZh（Loc.ZhOf 只读 Table）⇒ 必须窄

## 四、待授权（白名单之外的运行时宽化，本代理**没动**）

| 位置 | 现状 | 建议 |
|---|---|---|
| `Battle/CardDisplayWindow.cs` 的 `MaskKey`（`if (Loc.HasEntry(key)) return Loc.T(key);`） | 窄 | 打码词条取值口 ⇒ 宽。今天三档皆无那三条键 ⇒ 零行为变化 |
| `Battle/ChatPopupPanel.cs` 的 `LabelFor`（`if (Loc.HasEntry(key)) return Loc.T(key);`） | 窄 | 聊天钮标签 ⇒ 宽（`Battle/Chat/*` 6 条都在 Table，零变化） |
| `Battle/TipPrebattle.cs` 的 `GetNextTip`（`string text = Loc.HasEntry(key) ? Loc.T(key) : raw;`） | 窄 | 提示条 ⇒ 宽（`PrebattleTips/*` 三档皆 0，零变化） |
| `Net/NetProtocol.cs` 的 `Unpack`（`if (!Loc.HasEntry(key)) return s;`） | 窄 | 收侧认键 ⇒ 宽（否则第三档有词的走线串会原样回显） |

> ✅ **上述 4 处已于 2026-10-30 由 `B103_R3` 落地**（另一次点名授权；numstat 全等：3/3 · 2/2 · 1/1 · 1/1）。

## 五、镜像断言 5 处（**`BattleScene` 那 4 条 ✅ 已随 §一 同刻改完；只剩 `DeckScene:7184`**）

| 文件 | 锚（该行） | 与谁同刻 |
|---|---|---|
| `Editor/BattleScene.cs` | `bool costInTable = Loc.HasEntry(keyCost);` | `BattleDriver.HintForCodeWithKey`（`:16573`） |
| `Editor/BattleScene.cs` | `Check(Loc.HasEntry(keyInTable), $"（前提）\`{keyInTable}\` 在 \`Loc\` 表里（它有值 ⇒ 能验有键态）");` | 同上（`:7273`） |
| `Editor/BattleScene.cs` | `Check(Loc.HasEntry(keyInTable), $"★ 前提：词条 \`{keyInTable}\` 在语言表里");` | `BattleDriver.DamageFatigueTextWithKey`（`:30773`） |
| `Editor/BattleScene.cs` | `Check(!Loc.HasEntry("Battle/Tips/__SelftestNotInTable__") && viaFallback != viaTerm` | 同上（虚构键 ⇒ 宽/窄同结果，但口径要一致） |
| `Editor/DeckScene.cs` | `CheckTrue(!Loc.HasEntry(numKey5),` | `DeckRuntime.MenuDeckErrorKey`（`:6022`） |

> 这 5 行**本次没动**（`B103_R3` 已核对仍在原位：`BattleScene.cs:7245/7273/27362/27369`、`DeckScene.cs:7184`）。改宽时**必须与对应运行时口同一批**，否则期望值与实现分叉 ⇒ 一红一绿。

