# 安卓关服前存档 · CardTrait SO 普查

> 只读代理。日期 2026-10-23。⛔ 未改 `D:/2`、未改工程、未动 git、未跑 Unity。
> 目标：`D:/2/关服前存档/安卓版本warpforge，包含远端资料/` 里有没有 `CardTrait` SO，能否补齐"600+ 条推断 trait 效果"。

---

## 〇、一句话结论

**有。** 在这份 Android 1.35.0 包的 **`com.Everguild.WarhammerWarpforge.apk`（base APK，30 MB）** 里，
不在 `UnityDataAssetPack.apk`（那里面只有 Addressables）。

- `assets/bin/Data/sharedassets0.assets.split0..21`（22 片，拼起来 **22,727,260 B**，md5 `204bddf8c499a9baaf5db2ad3c43c092`）
- 里面：**1 个 `CardTraitCollection`**（`m_Name = "TraitsCollection"`，pathID **532**，byte_size **2416**）
  ＋ **73 个 `CardTrait` 派生 SO**（69 `CardTrait` + 2 `CardTraitOath` + 1 `CardTraitDuty` + 1 `CardTraitFerocity`）
  ＋ **75 条 `TraitEffect`** ＋ **77 条 `TraitPairs`**。
- **与 PC 关服前 build 的 100% 语义相同**（73 个 trait 逐字段比，**0 处差异**）。

🔴 **所以这份存档对 trait 这件事【没有新增信息】** —— 因为 PC 那份 2026-10-22 就已经挖出来了
（`数据/游戏数据/trait_collection.json`，生成器 `工具/export_trait_collection.py`，
判据 `资料/普查产出_1022/P3_M9_CardTraitCollection.md`）。

🔴 **同时它推翻了一条过期结论**：`工具/gen_card_vfx_by_card.py` 的 `limits` 里写着
「两个 build 的本地束里都没有 `CardTrait` 束 ⇒ `inferred-keyword` 那 600+ 条升不了级」。
**前半句对（确实没有 `cardtraits` 束）、后半句错** —— 数据不在束里，在 **player 自带的 `sharedassets0.assets`** 里，
**已经导出**。按它实测：**638 条 `inferred-keyword` 里 633 条可当场升权威**（见 §四）。

---

## 一、目录摘要

```
D:/2/关服前存档/安卓版本warpforge，包含远端资料/          787 MB
├─ Warhammer+40,000_+Warpforge_1.35.0_APKPure.xapk      813,572,269 B（5 个条目）
│    ├─ com.Everguild.WarhammerWarpforge.apk             30,392,506 B  ← 647 条目；**内建 player 数据在这**
│    ├─ config.arm64_v8a.apk                             43,684,991 B
│    ├─ UnityDataAssetPack.apk                          739,423,703 B  ← 186 条目 / 84 个 Addressables 束 / 705.1 MB
│    ├─ icon.png / manifest.json
├─ Everguild_Warpforge_Unity_Cache/                      11 MB（7 个文件 = 3×__data + 4×__info）
│    └─ Everguild_Warpforge/<3 个哈希>/<哈希>/__data
└─ *.torrent / *_files.xml / *_meta.sqlite（种子元数据，无内容）
```

| 缓存 `__data` | 大小 | UnityPy 读数 | 是什么 |
|---|---|---|---|
| `54bdb7b1…/ea8bca4f…` | 531,962 | 1,359 MonoBehaviour | **allcards**（= 2026-10-18 已并入的那份） |
| `c9187397…/dca1724a…` | 10,764,942 | 2 MonoBehaviour | **I2Languages + LanguageRemoteAsset**（= K07 已并入的本地化） |
| `7416b641…/8657f84d…` | 69,582 | 2 Sprite + 2 Texture2D | `WarpForgeLogo Style` / `Cell Shaded Alternative Art Style`（各 512×128） |

🔴 **这份 `Unity_Cache` 与 `D:/2/关服前存档/新建文件夹/Everguild_Warpforge/` 【逐字节相同】**
（7 个文件 md5 多重集完全一致，new-only = 0）⇒ 用户说的"包含远端资料"那部分，**就是之前已经挖过的那三个包**。

---

## 二、四问之一：有没有 `CardTrait` SO？

**有。坐标（全部实测，非推断）：**

| 东西 | 位置 | 计数 |
|---|---|---|
| `CardTraitCollection` SO | base APK → `assets/bin/Data/sharedassets0.assets.split0..21` | **1**（pathID 532 · `TraitsCollection` · 2416 B） |
| `CardTrait` 派生 SO | 同上 | **73**（69/2/1/1，四类脚本） |
| `TraitEffect`（在 SO 内的列表项） | 同上 | **75** |
| `TraitPairs`（集合里的条目） | 同上 | **77**（其中 `berzerk` / `dodge` 两条 `traitConfig = null`，原版本来就空） |

**为什么之前找不到（这次的根因与 P3 一致，换个平台再验一遍）：**

| 搜法 | 结果 | 说明 |
|---|---|---|
| `UnityDataAssetPack.apk` 84 个束逐个 UnityPy 遍历 | **0** 个 trait SO | 束里没有 `cardtraits`；monoscripts 束 722 个脚本里**连 `CardTrait` 这个 MonoScript 都没有**（只有 `BoardTraitIcon` / `EverguildTraitTooltipItem`） |
| 同上，按 `_traitEffects`/`TraitCollection`/`traitConfig`/`effectAnim` 等字段签名扫 | **0** | 对照：allcards 包同样方法能读出 117 个字段 ⇒ 方法有效，不是假阴性 |
| 全 186 条目原始字节 grep `CardTrait` | 只有 `link.xml`(1) 与 `battleprefabs…`(TraitEffect×3) | 束是压缩的 ⇒ **这个否定无效**，别用它 |
| base APK 逐条目原始字节 grep | ✅ `sharedassets0.assets.split21` 命中 `TraitsCollection` ×1；`globalgamemanagers.assets.split0` 命中 `CardTrait` ×12 | SerializedFile 不压缩 ⇒ **这个否定/肯定都有效** |
| `Unity/数据/索引/guid_map.tsv`（10,324 行）与 `普查产出_第二十七会话/guid_map.tsv`（4,997 行，有 `className` 列） | `CardTrait` **0 行** | ✅ **这是对的、不是缺口** —— 它根本不是 Addressables 资产，永远进不了这张表 |

**Android 侧 MonoScript 表**（`globalgamemanagers.assets.split0..8`，8,654,992 B，4302 个 MonoScript，全部读通）：

```
CardTrait 1732 · CardTraitCollection 3842 · TraitEffect 3982 · CardTraitData 4235
CardTraitDuty 1530 · CardTraitFerocity 2942 · CardTraitOath 227 · BoardTraitIcon 3553
```

（PC 那份是 4453 个脚本，pathID 是 1795/3970/4115/4371/1586/3039/231 —— **两 build 的 pathID 不同，不能混用**。）

---

## 三、四问之二：计数与对账（Android vs 我们的 `trait_collection.json`）

**解析方式**：`sharedassets0.assets` 的 MonoBehaviour **没有 type tree**（`obj.read()` 会抛
`Expected to read 2416 bytes, but only read 48 bytes`）⇒ 复用现成的手工解析器
（`工具/export_trait_collection.py` 的 `Reader` / `parse_collection` / `parse_cardtrait`），
**每个对象断言 `读到的字节 == byte_size`，全部通过**（collection 2416/2416）。

| 项 | Android 1.35.0 | PC 关服前 | 对账 |
|---|---|---|---|
| `CardTraitCollection` | 1（pathID **532**） | 1（pathID **531**） | pathID 差 +1（前面多一个对象），**内容一致** |
| `TraitPairs` | **77** | **77** | ✅ 相同 |
| `CardTrait` SO | **73** | **73** | ✅ 相同 |
| `TraitEffect` 总数 | **75** | **75** | ✅ 相同 |
| `traitConfig = null` 的 pair | `berzerk` / `dodge` | `berzerk` / `dodge` | ✅ 相同 |
| 有 anim 的 trait | 55 | 55 | ✅ 相同 |
| **逐 trait 语义比对**（图标有无 / stackable / aIScore / hasInPlayAnim / hasIcon / isInPlayIcon / customObject GUID / 每条 effect 的 name+GUID+subObject+overrideByArmies+triggerType+playIfWarlord） | —— | —— | 🔴 **0 处差异**（73 个 trait 全部逐字段相等） |
| effect GUID 能否在 cardanim 束里解开 | **77 / 77** | 77 / 77（P3） | ✅ Android 侧照样全通 |

⇒ **与我们的 `trait_collection.json`（77 pairs / 73 traits）能对上 100%，一条不多一条不少。**

`customObject` 非空的 7 条（Android 与 PC 相同）：
`Ambush 2415b100…` · `BloodThirst fb07f522…` · `Cruelty 564290f6…` · `Ecstasy 42abf1e9…` ·
`Sentry 50b0ea0f…` · `Shuriken cc61d807…` · `Swarm 6fdb2ceb…`

**`sharedassets0.assets` 全类直方图，Android 与 PC 完全同形**（188 个 MonoBehaviour：
CardTrait 69 · CosmeticItemCardback 13 · Image 11 · RawCardScript 8 · …）。
Android 那份 22.7 MB vs PC 8.4 MB **不是因为内容多** —— PC 把流式数据拆在 `.resS`(14.4 MB) + `.resource`(5 MB)，
Android 内联进同一个 `.assets`（8.4 + 14.4 ≈ 22.7）。

---

## 四、四问之三：能不能补上"600+ 条推断 trait 效果"？

**能，而且是大头。** 先纠正口径：

- `guid_map_bundles.tsv` 的 `CardTrait` 0 行 **不是缺口**（那是 Addressables 表，trait SO 本来就不在束里）。
- "600+ 条"实际是 `数据/游戏数据/card_vfx_by_card.json` 的 `entriesByConf`：
  `authoritative 3,949 · inferred-keyword **638** · inferred-name 59`。
- 那 638 条的判据（`CardTrait.TraitEffects[].effectAnim` + `overrideByArmies`）**已经在手**。

**逐条用权威数据复核这 638 条（含按阵营覆盖 `overrideByArmies`）：**

| 结论 | 条数 | 说明 |
|---|---|---|
| ✅ **可升权威（动画名完全吻合）** | **633** | 卡的关键词 → 原版天赋动画名，一字不差 |
| ❌ **权威数据里没有依据** | **5** | 全是关键词 `penitence`：卡 `SOR8 / SOR21 / SOR10 / SOR32 / SOR27` → 推的 `PenitenceTraitTrigger`。`DefinedTrait.cs` 里**没有 `penitence`**，73 个 CardTrait 里也没有 ⇒ 这条从哪来**没查清** |
| ⚠️ 动画名与权威不符 | **0** | —— |

⇒ **99.2%（633/638）可以从 `inferred-keyword` 升成 `authoritative`**；
剩 5 条应改成"未知/不播"，别静默留着当推断（铁律：不许静默失败）。
升级还顺带补两样现在推断里没有的信息：**`triggerType`（7 档原始枚举）** 与 **按阵营的 override**。

---

## 五、四问之四：有没有"其他远端资料"值得并进来？

**基本没有。** 逐项核过：

| 候选 | 状态 |
|---|---|
| `allcards`（1,351 张卡 SO） | 缓存 `__data` 与已并入那份**逐字节相同** ⇒ 无新增 |
| `I2Languages` 本地化 | 同上（10,764,942 B 那个包） ⇒ 无新增（K07 已并） |
| `WarpForgeLogo Style` / `Cell Shaded Alternative Art Style` | 我们已有（`Assets/CardPresentation/Resources/Art/ui_menu/WarpForgeLogo_Style.png`，`CollectionWindow.cs:1787` 也记过这张包的容器键） |
| 卡包外观 / 活动配置（`boosterpacks` / `liveopsmenuimages` / `specialofferscontentlocal` / `trophies` …） | Android 的 84 个 Addressables 束**与 PC 那份名字集合完全相同**（only-android = 0，only-pc = 0）⇒ PC 侧本来就都有 |
| Addressables catalog | 大小 1,299,203（Android）vs 1,305,148（PC），ASCII 串集合高度重合；**没做逐条语义 diff**（见 §六） |

---

## 六、没查清 / 读到哪一步

1. **两份 catalog.bin 没做逐条语义比对** —— 只比了大小与 ASCII 串集合（差异全是抽取时首尾字符错位的噪声）。
   若要确认"Android 有没有 PC 没有的资产"，得按 catalog 结构正式解一遍。
2. **`global-metadata.dat`（base APK 内，20,857,080 B）没挖** —— Android IL2CPP 元数据，
   能给 Android 侧的枚举/字段名。我们已有 PC 全量反编译，**判断价值低**，但没验。
3. **Sprite / Texture 级别没逐张 diff** —— Android 与 PC 的 `sharedassets0.assets` 只比到
   "类直方图同形 + trait 对象逐字段相等"，**240 个 Sprite / 56 个贴图的字节没逐一比**。
4. **`penitence` 那 5 条的来源没查清** —— `PenitenceTraitTrigger` 这个 CardAnim 确实存在，
   但既不是 DefinedTrait、也不在 73 个 CardTrait 里。是代码里直接播的？还是别的 SO 配的？**未查**。
5. **`UnityDataAssetPack.apk` 的 84 个束只做了"字段签名 + 脚本表"两层扫**，
   没有逐对象按类名统计（没有 trait 的 MonoScript ⇒ 逻辑上也不可能有 trait 实例，但这是推理不是穷举）。

---

## 七、建议怎么并入（给主对话）

1. **不需要为 trait 并入任何东西** —— `trait_collection.json` 已是权威、且与 Android 逐字段一致。
   这份存档的价值是**"第二个 build 的交叉验证"**：证明那 73 条不是某个平台特例。
2. 🔴 **改掉 `工具/gen_card_vfx_by_card.py` 里那段过期结论**（`limits` 数组，约 `:1344`）：
   现在写的是「两个 build 的本地束里都没有 `CardTrait` 束 ⇒ 600+ 条升不了级」。
   应改成：「束里没有是对的；数据在 **player 自带 `sharedassets0.assets`**，已由 `export_trait_collection.py` 导出
   ⇒ 判据在手，可升级」。⛔ 别留着它继续误导下一个会话（铁律 5）。
3. **把 633 条 `inferred-keyword` 升成 `authoritative`**（`conf` 改名 + 记 `src: cardtrait-so` +
   带上 `traitTriggerType` 与 `overrideByArmies`）。这需要一个生成器改动，建议单开一条任务。
4. **那 5 条 `penitence`** 改标 `unsupported`（或另查来源），⛔ 不要继续当推断播。
5. 其余（allcards / 本地化 / 卡包 / 活动）**没有新增，不必动**；缓存目录与 `新建文件夹` 那份重复，
   若要省盘可以只留一份（**属于删文件，需用户拍板**）。
