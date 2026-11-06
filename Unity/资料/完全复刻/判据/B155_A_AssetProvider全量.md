# B155_A · AssetProvider 全量普查（只读代理报告 · 2026-11-06 19:26 快照）

> 工作区当时正被实时改写；Q3 分「历史 / 当前」两段。
> 路径别名：`ILSPY` = `E:/warpforge/03_反编译/cpp2il/out_cpp2il_dev_ilspy` · `DEC` = `E:/warpforge/03_反编译/decomp_full` · `AC` = `E:/warpforge/03_反编译/il2cpp_out/dump.cs` · `RESP` = `E:/warpforge/05_工具/server/responses` · `PH0` = `E:/warpforge/07_产物/phase0`

## Q1 清单

口径：`grep -rl ": AssetProvider<"` 在 ILSPY 递归命中 **26 个文件**；`assetripper_out/…/Assembly-CSharp` 与 `code_stubs/Assembly-CSharp` 交叉核对同为这 26 个。
**误译核验**：ILSpy 的 `return (IEnumerable<T>)new NullReferenceException()` 一律是 `throw` —— `DEC/DLCMessageAssetProvider__GetAssets.c:24-26` 与 `DEC/TemplateAssetProvider__GetAssets.c:17-19` 末句均为 `WARNING: Subroutine does not return` + `FUN_1803f47a0()`；`DEC/PointAssetProvider__GetAssets.c`、`DEC/CurrencyAssetProvider__GetAssets.c`、`DEC/AllianceBadgeAssetProvider__GetAssets.c`、`DEC/PackAssetProvider__GetAssets.c` 同型。

| 类 | 泛型 | GetAssets 语义 | 依赖配置 | ConfigKey | 判据 |
|---|---|---|---|---|---|
| AllianceBadgeAssetProvider | AllianceBadge | `OfType<AllianceBadge>(CollectionManager.Instance.FullAllianceItemCollection)`；Instance null→抛 | — | — | `AllianceBadgeAssetProvider.cs:5,9,14` |
| AllianceTrophyAssetProvider | AllianceTrophyData | `config.trophies`；config null→抛 | AlliancesConfig | AlliancesConfig | `…:5,9,14` |
| AlternateArtAssetProvider | AlternateArtCard | `CollectionManager.Instance.alternateArtCollection`；null→抛 | — | — | `…:4,6,13` |
| CardAssetProvider | RawCardScript | `CollectionManager.Instance.AllCardCollection`；null→抛 | — | — | `…:4,6,13` |
| ChallengeListLocator | ChallengeList | `config.challengeLists`；null→抛 | MissionsConfig | MissionsConfig | `ChallengeListLocator.cs:5,9,14` |
| CosmeticAssetProvider | CosmeticItem | `CollectionManager.Instance.FullCosmeticCollection`；null→抛 | — | — | `…:4,6,13` |
| CurrencyAssetProvider | Currency | `config.currencies`；null→抛 | CurrencyConfig | CurrencyConfig | `…:5,9,14` |
| DeckAssetProvider | CardDeck | `InventoryManager.Instance.GetInventory<CardDeck>()` 再经 vtable+0x288 间接取枚举；manager/inventory null→抛，**缺 CardDeck 库存会抛 KeyNotFoundException** | 无（库存） | — | `DeckAssetProvider.cs:7,15-26`；`DEC/DeckAssetProvider__GetAssets.c:20-28` |
| DlcBundleAssetProvider | DlcBundle | `config.DlcItems`(=OfType<DlcBundle>(containers))；null→抛 | ShopConfig | ShopConfig | `…:5,9,14` |
| DLCMessageAssetProvider | DLCMessageData | `OfType<DLCMessageData>(config.Templates.Values)`；config 或 Templates null→**抛** | TemplateConfig | Templates | `DLCMessageAssetProvider.cs:7,9-17`+DEC |
| EventDataAssetProvider | LiveOpsEventData | `LiveOpsManager.GetAllEvents().Select(e=>e.GetBaseData())`；不依赖配置 | — | — | `EventDataAssetProvider.cs:34-51` |
| EventGroupAssetProvider | EventGroup | `config.groups`；null→抛 | EventGroupConfig | EventGroups | `…:5,9,14` |
| PrebuiltDeckAssetProvider | PrebuiltDeck | `PlayerDataManager.singletonManager.PrebuiltDeckCollection.decks`→List 缓存；manager/collection null→抛 | — | — | `Everguild.LiveOps.Config/PrebuiltDeckAssetProvider.cs:6,10-25` |
| CustomDroptableAssetProvider | CustomDropTableDefinition | `config.customDropTables`；null→抛 | ShopConfig | ShopConfig | `Everguild.LiveOps/CustomDroptableAssetProvider.cs:7,11,16` |
| DroptableAssetProvider | DropTableItem | `config.dropTables`；null→抛 | ShopConfig | ShopConfig | `Everguild.LiveOps/DroptableAssetProvider.cs:7,11,16` |
| ShopContainerAssetProvider | ShopContainerBase | `config.containers`；null→抛 | ShopConfig | ShopConfig | `Everguild.LiveOps/ShopContainerAssetProvider.cs:7,11,16` |
| ShopOfferAssetProvider | ShopOfferBase | `LiveOpsManager.GetHandler<Shops>()`；无 handler→`Enumerable.Empty`（不抛） | — | — | `Everguild.LiveOps/ShopOfferAssetProvider.cs:36-68`+DEC |
| GenericArmyItemAssetProvider | GenericArmyItem | `config.genericArmyItems`；null→抛 | ShopConfig | ShopConfig | `…:5,9,14` |
| MissionChallengeProvider | MissionChallenge | `config.challenges`；null→抛 | MissionsConfig | MissionsConfig | `MissionChallengeProvider.cs:5,9,14` |
| PackAssetProvider | PrebuiltPack | `CollectionManager.Instance.FullPackCollection`；null→抛 | — | — | `…:4,6,13` |
| PointAssetProvider | Points | `config.points`；null→抛 | PointsConfig | PointsConfig | `…:5,9,14` |
| PremiumItemAssetprovider | PremiumItem | `config.premiumItems`；null→抛 | PremiumItemsConfig | PremiumConfig | `PremiumItemAssetprovider.cs:5,9,14` |
| ShopOfferListLocator | ShopOfferList | `config.offerLists`；null→抛 | ShopConfig | ShopConfig | `ShopOfferListLocator.cs:6,10,15` |
| TemplateAssetProvider | IDataTemplate | `config.Templates.Values`；null→抛 | TemplateConfig | Templates | `TemplateAssetProvider.cs:6,10-15`+DEC |
| WildcardAssetProvider | Wildcard | `config.wildcards`；null→抛 | WildCardConfig | WildcardConfig | `…:5,9,14` |
| XSollaBundleAssetProvider | XSollaBundleItem | `config.xsollaItems`；null→抛 | ShopConfig | ShopConfig | `…:5,9,14` |

ConfigKey 判据：`Everguild.LiveOps/TemplateConfig.cs:20`、`Everguild.LiveOps.Config/PointsConfig.cs:40`、`WildCardConfig.cs:20`、`Everguild.LiveOps.Config/ShopConfig.cs:48`、`EventGroupConfig.cs:18`、`PremiumItemsConfig.cs:18`、`AlliancesConfig.cs:21`、`CurrencyConfig.cs:50`、`MissionsConfig.cs:41`、`Everguild.LiveOps.Config/DailyStoresConfig.cs:24`。
另：`EventGroupConfig` 自身也实现 `IAssetProvider<EventGroup>`（`ILSPY/EventGroupConfig.cs:4,24-27`），但它不是 `AssetProvider<T>` MonoBehaviour，不会注册进 AssetLocator。

## Q2 AssetLocator

- 注册：`AssetProvider<T>.Awake` → `AssetLocator.RegisterProvider<T>(this)`（`ILSPY/AssetProvider.cs:8-11`；`AC:55100-55104` 目标 RVA=0x180C031E0）。`RegisterProvider` 写 `typedProviders[typeof(T)] = provider`（`ILSPY/AssetLocator.cs:624-637`；`DEC/AssetLocator__RegisterProvider_object_.c`）。
- `GetAssetsInternal<T>`（`ILSPY/AssetLocator.cs:856-913`）：先 `typedProviders.TryGetValue(typeof(T))`；**命中→直接 `provider.GetAssets()`**；**未命中→`TryGetAssetAbstract<T>()`**（`DEC/AssetLocator__GetAssetsInternal_object_.c:28-74`）。
- `TryGetAssetAbstract<T>`（`:963-990` + 嵌套 `d__7.MoveNext`）：遍历 `typedProviders` **全部**键值；`ReflectionHelper.Is(providerKey, targetType)` = `targetType.IsAssignableFrom(providerKey)`（`DEC/AssetLocator._TryGetAssetAbstract_d__7_object___MoveNext.c:104`；`DEC/ReflectionHelper__Is.c:7-10`）；命中则 `provider.GetAssets()`→`OfType<T>`→按 `GetID()` 去重后 yield。**因此 T=IIdentifiable 时会把所有 provider 的 GetAssets 都跑一遍。**
- `GetAsset<T>(id)`（`:774-825`）：`FirstOrDefault(…, t => t.GetID() == id)`；查不到 null；`AssetLocator.Instance` null 时抛。
- `GetAssets<T>()`（`:841-853`）：`ToList(GetAssetsInternal<T>())`。
- `GetUniqueAsset<T>()`（`:638-771`）：只查 `uniqueAssets`，谓词 `asset.GetType() == typeof(T)`；**不走 provider**。
- `TryGetProvider<T>`（`:992-1099`）：精确 `typedProviders.TryGetValue`。
- 失败语义：provider 抛→冒泡；provider 返回 **null**→`Enumerable.OfType(null)` 抛 `ArgumentNullException`。

**全仓调用点**（`grep -rn "AssetLocator\." ILSPY`，排除自身，共 47 处）：
`AlliancesTrophyController.cs:321` · `AlternateArtInventory.cs:144,163` · `AlternateArtPanel.cs:578,600` · `BattleHud.cs:507` · `CardAlternateArtDrawer.cs:17` · `CardDeck.cs:198,1737` · `CardDisplayWindow.cs:534` · `CardServices.cs:597` · `CardTraitCollection.cs:83` · `ChallengeManager.cs:417` · `DebugRewardWindow.cs:106` · `DeckAndWarlordData.cs:431,649` · `DeckInfoControls.cs:187` · `DeckInventory.cs:742` · `Everguild.LiveOps/OfferData.cs:500` · `Everguild.LiveOps/PlayEvent.cs:1148,1184` · `FastModeBaseEvent.cs:785,801` · `InventoryManager.cs:551,1349,1407` · `ItemDrawer.cs:27,79` · `MissionChallengeProgress.cs:253,554,701,729,757,851` · `MissionContainer.cs:21,142` · `MissionCounterDisplay.cs:43` · `MissionReRollButton.cs:26,107` · `PlayerDataManager.cs:5733,7162,7791` · `PlayerInfo.cs:432` · `ProfileNameTitleSection.cs:74,79` · `WarlordMasteryContainer.cs:155,160`。mod/server 侧 0 处调用。

## Q3 会连炸的点（按触发顺序，分「19:22 前」与「19:24 后」两轮实测）

开窗链：`OpenWindowButton.OpenWindow`（`ILSPY/OpenWindowButton.cs:191-199`，第 199 行读 `windowPayload.Reference`）→ `LoadableReference<IIdentifiable>.Reference`（`ILSPY/LoadableReference.cs:33-…`，调用目标 RVA 0x180C02B60 = `AssetLocator.GetAsset<object>`，`AC:55118-55121`）→ `AssetLocator.GetAsset<IIdentifiable>`（`:774`）→ `GetAssetsInternal`（`:856`）→ `TryGetAssetAbstract<IIdentifiable>`（`:963`）。

| # | 触发点 | 需要什么 | 缺了会怎样 | 我们现在喂了没有 |
|---|---|---|---|---|
| 1（历史，已修） | `ConfigManager.Unpack` → `DailyStoresConfigProvider.UnpackInternal` | `TitleData["DailyStoresConfig"]`（值要能反序列化成 `List<string>`，且每个元素还会被再当 titleData 键查一次） | **抛 KeyNotFoundException**，Unpack 半途中止：只登记 4 个 provider | 19:21/19:22 没喂（`PH0/config.tsv` UNPACK-EXC）；**19:24 起已喂 `"DailyStoresConfig": "[]"`，UNPACK-OUT count=20 无异常** |
| 2（历史，已修） | `DLCMessageAssetProvider.GetAssets` | `GetConfig<TemplateConfig>()` 非 null 且 `.Templates` 非 null | 抛 NRE | 19:22:25 崩溃；19:24 起 `tplCfg=config=ok Templates=count=0` ⇒ 已解除 |
| 3（当前真实阻塞） | `DeckAssetProvider.GetAssets` → `InventoryManager.GetInventory<CardDeck>()` | `InventoryManager.inventories` 里要有 `typeof(CardDeck)`（由 `DeserializeInventory`→`CreateInventory` 逐类型创建） | 抛 KeyNotFoundException 'CardDeck' | 19:25:08 实测（`PH0/windows.tsv`） |
| 4（配置地雷，未触发但必炸） | `AllianceTrophyAssetProvider.GetAssets` | `AlliancesConfig.trophies` 非 null | 返回 null → `Enumerable.OfType(null)` 抛 ArgumentNullException | 当前 `"AlliancesConfig": "{}"`，构造器只设 `maxMemberCount=20`，trophies 保持 null |
| 5（配置地雷，未触发但必炸） | `MissionChallengeProvider.GetAssets` / `ChallengeListLocator.GetAssets` | `MissionsConfig.challenges` / `.challengeLists` 非 null | 同上 | 当前 `"MissionsConfig": "{}"`，构造器只初始化 `configs` |
| 6（已安全） | 其余配置型 provider：ShopConfig 系 7 个、Point、Wildcard、Currency、EventGroups、PremiumItems、Template | 各自 config 非 null 且集合字段非 null | 若集合字段 null 就会变成 #4 型 | 对应 Config 构造器都初始化了实际用到的集合 ⇒ 返回空表，不抛 |
| 7 | 非配置型 provider：Deck(#3)、PrebuiltDeck、Pack/Cosmetic/Card/AlternateArt/AllianceBadge、ShopOffer、EventData | 单例/InventoryManager/LiveOps handler | 单例/handler 为 null 才抛 | 与配置无关 |

补充：开窗之后的收藏内容层，静态检查 `CollectionScreen.cs`、`CollectionManager.cs`、`Card/Deck/Cardback/AlternateArtCardCollectionTab.cs`、`CollectionDisplay.cs`、`CollectionCard.cs`、`CollectionCosmetic.cs`、`CollectionItem.cs`、`CollectionTab.cs`、`CardsCollection.cs`、`DeckCollection.cs` 全部 0 处 `ConfigManager.GetConfig` / `AssetLocator.*` 调用。

## 还差什么 / 查不到的部分

1. **`ConfigProvider<T>.Key` 的确切取键字符串没查实**：ILSpy 里 mangled；`out_il2csharp` 同样 `Method body was not recovered`；`DEC` 下没有 `ConfigProvider<object>.get_Key` 的 .c。但 `IConfigData` 上唯一的字符串属性就是 `ConfigKey`，与全部实测一致，未 100% 证实。
2. **`InventoryManager.inventories` 为什么缺 CardDeck 未查实**（报告写时新探针刚装）。后续实测：`PH0/inventory.tsv` 只有 `inventories=count=0`，且 `DeserializeInventory` 一次都没进过（`PDM-IN` 无线）⇒ 登录状态机 20 阶段在它之前就断了。
3. **探针 `Configs={…config=null}` 这一列不可信**：同一时刻 `initstat.tsv` 的 `GetConfig<TemplateConfig>()` 读到 `config=ok Templates=count=0` ⇒ `DumpStaticConfigs` 读私有字段 `config` 失败。
4. 扫过但 0 命中的关键词/目录：`E:/warpforge/03_反编译` 全树 `find -iname "*AssetLocator*" / "*AssetProvider*"`；`grep "TryGetAssetAbstract"` 在 mod/server 下；`grep "Couldn't find Config of type"` 在 `PH0/probe.log`/`unity.log` 下；`grep "ConfigKey"` 在 `DEC` 下只命中各 `*__get_ConfigKey.c` 与 `PlayerDataManager`。
