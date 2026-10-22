# P7 / 把原版 `CardTraitCollection` 那批 SO 导出成工程可用的 JSON

> 执行代理 P7。日期 2026-10-22。判据来源 = `资料/普查产出_1022/P3_M9_CardTraitCollection.md`（下称 **P3 报告**）。
> ⛔ 本轮**没跑 Unity**、**没改 `.cs`**、**没动 git**、**没改 `d:/2/` 下任何文件**。

---

## 〇、结论

| 东西 | 位置 | 读数 |
|---|---|---|
| 工具 | **`Unity/工具/export_trait_collection.py`** | 789 行 · 可重复跑 · 幂等 · 支持 `--check` |
| 产物 | **`Unity/数据/游戏数据/trait_collection.json`** | **73,214 B** · md5 **`57053e954b6f7fabe6b4479629f2466d`** · LF 2858 / CRLF 0 |
| 内容 | — | **77 条 `TraitPairs`（含 2 条 `traitConfig: null`）· 73 个 CardTrait 对象 · 75 条 traitEffect · 10 条 overrideByArmies · 8 个 Sprite** |
| 运行耗时 | — | ≈ 1.1 s（扫 14 个 cardanims 包也是这个量级） |

五条验收全过：**①73/73 字节断言** · **②`effectAnim` 全是 CardAnim 语义名（没有一条裸 GUID）** · **③77 条 TraitPairs 全在** · **④两次生成 md5 相同** · **⑤字段来源见 §二**。

---

## 一、数据源（坐标都是现读的）

| 用途 | 文件 | 关键坐标 |
|---|---|---|
| 集合 + 73 个 trait + 全部图标 Sprite | `d:/2/Warhammer 40k Warpforge/Warpforge_Data/sharedassets0.assets`<br>md5 **`3b87529c8a3bda945a00b8b2ddf82202`** · 8,407,132 B | `CardTraitCollection` = **pathID 531**（`m_Name = TraitsCollection`，`byte_size = 2416`）；CardTrait 派生 = **458–530** |
| MonoScript → 类名 | 同目录 `globalgamemanagers.assets` | CardTrait **1795** / CardTraitCollection **3970** / TraitEffect **4115** / CardTraitData **4371** / CardTraitOath **231** / CardTraitDuty **1586** / CardTraitFerocity **3039** / BoardTraitIcon **3671** |
| effectAnim GUID → CardAnim 名 | 同目录 `StreamingAssets/aa/StandaloneWindows64/*cardanims*_assets_all.bundle` | **14 个包**（⚠️ P3 报告写的 15 —— 见 §四），container 的键就是 GUID |

⚠️ **这批 MonoBehaviour 没有 type tree** ⇒ `obj.read()` 必失败，本工具走的是「从 `byte_start` 抠 `byte_size` 字节 + 手写布局」那条路（布局判据 = P3 报告 §三）。

---

## 二、每个字段怎么来的

`traits[]` 一条 = **一条 `TraitPairs`**（按集合里的顺序），所以是 **77 条**，不是 73 条 ——
`oathDouble` / `oathInAllTurns` / `oathTripleActivation` 三条指向同一个对象 `OathAdditionalEffect`(498)，
在 JSON 里就是三条各自完整、`pathID` 都是 498。**对象本体只有 73 个**，JSON 里 `collection.cardTraitObjectCount = 73` 记着。

| 字段 | 取自 | 说明 |
|---|---|---|
| `name` | `TraitPairs.name` | string |
| `definedTrait` | `TraitPairs.traitType` | int（`enum DefinedTrait`） |
| `definedTraitName` | 同上 int，按 `DefinedTrait.cs` 的枚举名 | ⚠️ **P7 加的**（与 `triggerName` 同一性质）；**77/77 全命中** |
| `traitConfig` | `TraitPairs.traitConfig` | PPtr 原样 `{fileID, pathID}`；**2 条是 `null`**（`berzerk` 360 / `dodge` 240 —— 原版就没配） |
| `pathID` | 同上 PPtr 的 pathID | 扁平化，方便 join |
| `m_Name` | `CardTrait.m_Name` | 注意和 `name` **不是一回事**：`dontReturnFerocity` → `m_Name = NotFerocity`、`oathDouble` → `OathAdditionalEffect` |
| `traitIcon` | `CardTrait._traitIcon` PPtr → 同文件 Sprite 的 `m_Name` | **7 条是 `null`**（`combo` / `daemonicAllegiance` / `firstStrike` / `large` / `mekaniak` / `piercing` / `unstoppable`）—— 原版就是空 |
| `traitIconPathID` | 同一个 PPtr | ⚠️ **这一条是 P7 加的**（P3/简报的形状里没有）：把图标 trace 回 `sharedassets0` 的 Sprite 对象用 |
| `stackable` | `CardTrait._stackable` | `TraitStackType`：0=replace / 10=add / 20=count |
| `stackableName` | 同上，按 `CardTrait.cs` 的枚举名 | ⚠️ **P7 加的**（与 `triggerName` 同一性质） |
| `aIScore` | `CardTrait._aIScore` | float，**73 个全是 0.0** |
| `inPlayAnim` | `CardTrait._hasInPlayAnim` | bool（序列化是 1 B + 3 B 填充） |
| `hasIcon` | `CardTrait._hasIcon` | bool |
| `isInPlayIcon` | `CardTrait._isInPlayIcon` | bool |
| `customObject` | `CardTrait.customObject`（`AssetReferenceTyped<Object>`，3 个 string） | 另加 `name`：**能换到 CardAnim 名的才写**，换不到写 `null`（⚠️ P7 加的，见 §四 #3） |
| `traitEffects[].name` | `TraitEffect.name` | |
| `traitEffects[].triggerType` / `triggerName` | `TraitEffect.TriggerType` int + `CardTrait.EffectTriggerType` 名 | 0=Start 1=End 2=OnPlay 3=Trigger 4=OnHand 5=FromCode 6=TriggerOnPlay |
| `traitEffects[].playIfWarlord` | `TraitEffect.playIfWarlord` | bool |
| `traitEffects[].effectAnimGuid` | `TraitEffect.effectAnim` 的 guid | **7 条本身就是空串**（原版没配），见 §三.2 |
| `traitEffects[].effectAnim` | 上者经 cardanims 包的 container 键换成 **CardAnim 的 `m_Name`** | **换不到直接抛错退出**，⛔ 不写 null 糊过去 |
| `traitEffects[].overrideByArmies[]` | `TraitEffectOverrideByArmy[]` | `{army, armyName, effectAnimGuid, effectAnim}`；`armyName` ← `CardArmy.cs`（Neutral 0 / Ultramarines 10 / Goff 20 / SaimHann 30 / Sautekh 40 / BlackLegion 50 / Leviathan 60 / TauEmpire 70 / Sororitas 80 / Genestealers 90 / AstraMilitarum 100 / DarkAngels 110 / EmperorsChildren 120 / SpaceWolves 130） |
| `spriteIds.<8 个字段>` | `CardTraitCollection` 的 8 个 Sprite PPtr | `{pathID, name}` |
| `source` | 现算 | 源文件路径 / md5 / 字节数 / cardanims 目录 / 包数 / MonoScript 表 |
| `generated` | **固定常量 `"2026-10-22"`** | ⛔ 不是运行日期 —— 用 `datetime.now()` 会让每天跑出来的文件都不一样，破坏幂等 |
| `collection` | 现算 | `pathID / m_Name / traitPairCount / cardTraitObjectCount / nullTraitConfigPairs / unresolvedCustomObjectGuids / bytesRead / byteSize` |

**没写进产物的两个字段**：`effectAnim.subObjectName` / `subObjectType`（`AssetReferenceTyped` 另外两个 string）。
实测 **75 + 10 条全是空串**，写进去也没有信息量。要写的话判据在解析器里（`traitEffects[].effectAnimSubObjectName` / `...SubObjectType`）。

---

## 三、校验结果

### 3.1 ① 73/73 字节断言

工具对**每个对象**（含集合那个）解析完都断言 `r.p == len(raw)`，不匹配就抛 `AssertionError`。
现跑读数（每次运行都打）：

```
[parse] collection pathID=531 m_Name='TraitsCollection' pairs=77 objects=73 (null traitConfig: berzerk, dodge)
[parse] bytesRead == byte_size: 73/73 CardTrait 对象全部通过
```

集合对象本身：`bytesRead = byteSize = 2416`（产物 `collection` 里记着）。

此外工具还现场复核了 7 件事，任一不成立就退出：
1. `globalgamemanagers.assets` 里那 8 个 MonoScript pathID **真的**是那 8 个类名（现场 `obj.read()` 读 `m_ClassName`）；
2. `CardTraitCollection` 只有 1 个、而且是 **531**；
3. CardTrait 派生对象 **正好 73 个**；
4. 每个 `_traitIcon` / 8 个 Sprite 的 PPtr 指向的**真的是 `Sprite` 类型**（不是别的对象）；
5. 遇到未知的 `TraitStackType` / `EffectTriggerType` / `CardArmy` / `DefinedTrait` 值直接抛错（⛔ 不静默写 null）；
6. **`TraitPairs.name` 字符串 == 同一个 int 在 `DefinedTrait.cs` 里的枚举名** ——
   两条来源完全独立（二进制里的 string vs 反编译枚举），现测 **77/77 一致**；
7. 用到的每个 effectAnim GUID 都必须在 P3 §4.3 表里，且**名字逐条相符**（见 3.2）。

### 3.2 ② `effectAnim` 是语义名 —— 67/67 全换到

**数对得上 P3 的「67」**：

| 口径 | 条数 |
|---|---|
| `traitEffects` 条目 | 75 |
| 其中 `effectAnimGuid` **非空** | **68** |
| 其中 `effectAnimGuid` **本身就是空串**（原版没配） | 7 |
| `traitEffects` 里 **distinct** 的 GUID | **67** ←／就是简报说的那 67 个 |
| `overrideByArmies` 条目 / 其中 GUID 只在 override 里出现 | 10 / **10** |
| 数据里用到的 distinct GUID 合计 | **77**（67 + 10） |

- **67/67 + 10/10 全部换到名字**；工具对「换不到」是**硬失败**，本轮一次都没触发。
- 产物里 `effectAnim` **没有一条**是 32 位十六进制的裸 GUID（已用正则 `^[0-9a-f]{32}$` 扫过，0 命中）。
- **第二条独立路径对账**：工具内嵌了 P3 报告 §4.3 那张表（79 行）作为 `EXPECTED_GUID_MAP`，
  把「现场扫 14 个包」得到的名字**逐条**与它比 —— **0 条不符、0 条缺失**。

**那 7 条空 GUID 是哪几条**（原版数据就是空，不是解析失败）：
`combo/OnPlay` · `daemonicAllegiance/OnPlay` · `firstStrike/Start` · `large/Trigger` · `mekaniak/Trigger` · `piercing/Start` · `resolution/Trigger`。
⚠️ 前 6 个的 `_traitIcon` **也是 null** —— 同一批「原版没配」的 trait；`resolution` 有图标、只是没配演出。

### 3.3 ③ 77 条 TraitPairs 全在

```
[json ] traits(77 条 TraitPairs 展开)=77  traitEffects=75  overrideByArmies=10  sprites=8
```

2 条 `traitConfig: null` = `berzerk` / `dodge`（产物里 `collection.nullTraitConfigPairs` 也记着）。

**独立逐行对账**：另外写了一份一次性脚本，把 P3 报告 §4.1 那张 **77 行真值表**解析出来，
与产物按 **`name` / `definedTrait` / `pathID` / `m_Name` / `traitIcon` / `stackable` / 效果三元组 `(triggerName, triggerType, playIfWarlord)`**
逐列比：

```
P3 §4.1 表行数 = 77
JSON traits = 77
✓ 77 行真值表逐列一致（name/definedTrait/pathID/m_Name/traitIcon/stackable/效果三元组）
```

### 3.4 ④ 幂等 —— 两次逐字节相同

```
第一次生成: [write] ... (73214 B, LF=2858, CRLF=0)     md5 57053e954b6f7fabe6b4479629f2466d
第二次生成:                                             md5 57053e954b6f7fabe6b4479629f2466d
--check   : [check] ✓ 与盘上逐字节相同 (73214 B, md5 57053e954b6f7fabe6b4479629f2466d)
二进制复核: size 73214 CRLF 0 LF 2858  tail b'...rait_icon_Ranged"\n    }\n  }\n}\n'
```

幂等的三个保证：`generated` 是常量 · 所有 pathID 遍历前 `sorted()` · 包列表 `sorted()`。
行尾：产物**纯 LF**（`newline='\n'`），写完**二进制读回**数一遍 `CRLF == 0` 才放过。

### 3.5 `--check` 与落盘的安全写法

- `--check` = 只解析 + 跑全部断言 + 与盘上逐字节比，**不写文件**；不一致/缺失退出码 1。
- 落盘走 `io.open(..., 'w', encoding='utf-8', newline='\n')`，且**先把内容算进 `text` 变量**再写
  （避开 `io.open(p,'wb').write(<表达式>)`「先截断再求值」那个坑）。

---

## 四、与 P3 报告不一致 / 需要更正的地方（铁律 5）

| # | P3 报告写的 | 实测 | 处置 |
|---|---|---|---|
| 1 | §4.3 「打开 **15 个** `*cardanims*.bundle`」 | **14 个**。`find StreamingAssets -iname "*cardanim*"` 全树只有 14 个 `.bundle`，没有第 15 个 | 工具里按 14 断言并在注释写明；真正的守卫是「用到的每个 GUID 都必须换到名字」，本轮 77/77 通过 |
| 2 | §4.3 表头写「**67 个** `effectAnim` GUID」，但表体实际 **79 行** | **79 = 77（数据里用到的）+ 2（不是 `effectAnim`，是 `customObject`）**。那 2 行是 `50b0ea0f…` = `Sentry_Effect`（`Sentry.customObject`）与 `cc61d807…` = `Eldar_Shuriken_Short_Trait`（`Shuriken.customObject`） | 表没错、只是标题口径偏窄；已在 §三.2 把口径拆清楚 |
| 3 | §七 #3「`TraitEffect` 里 4 个 `customObject` GUID 未解析（`2415b1…` / `564290…` / `6fdb2c…` / `fb07f5…`）」 | **仍然未解析，且正好就是这 4 个**（分别在 `Ambush` / `Cruelty` / `Swarm` / `BloodThirst` 上）。同时**另外 3 个能解析**：`ecstasy` → `EcstasyTraitTrigger` · `sentry` → `Sentry_Effect` · `shuriken` → `Eldar_Shuriken_Short_Trait` | 已把「3 个解析结果」写进产物 `customObject.name`，4 个未解析的**在产物 `collection.unresolvedCustomObjectGuids` 里点名**（⛔ 不静默） |

§一 #3/#8 那两处字节数写法不一致（表里写 `sharedassets0.assets(188 MB)`、#8 写 `8,407,132 B`）——
**8,407,132 B 那个是对的**（本机现读 + md5 `3b87529c…` 对上）。188 MB 应是笔误，不影响结论。

---

## 五、没查清的 / 留给下一手

1. **`agenda` 的 `effectAnim` = `DutyAnim`**（GUID `a8c3f891601e9ca4c9e3b7c8cd0f37a3`）。
   两条独立路径（现场扫包 + P3 §4.3 表）**一致**，所以**数据是这么写的**，不是解析错；
   但**为什么 Agenda 共用 Duty 的演出**没查（不在本任务范围）。接演出时按这个值接即可。
2. **4 个 `customObject` GUID 没解析**（`2415b1…` / `564290…` / `6fdb2c…` / `fb07f5…`）——
   与 P3 §七 #3 一致。`catalog.bin` 里有这几个 GUID，但**不在 14 个 cardanims 包里** ⇒ 得扫其它包。
   本轮**没扫**（超出「导出 TraitCollection」的范围），但它已经在产物里点名、不会丢。
3. ~~`definedTrait` 只给数字~~ —— **已解决**：`DefinedTrait.cs`（137 行，`d:/2/Warpforge_code/Scripts/Assembly-CSharp/`）
   本来就在，本轮把整张枚举抄进了工具 ⇒ 产物多一个 `definedTraitName`，**77/77 全命中**。
   （⚠️ 我第一版报告里写「本地没有 `DefinedTrait.cs`」是**看漏了** —— 它就在同一个目录里，铁律 5 就地更正。
   顺带更正：`Unity/数据/索引/enum_names.json` **确实不是**枚举表，它是「文件 → 资源名」清单，93 条。）
4. **P3 §七 #1 / #2 仍成立**（`GetUniqueAsset` 最后一跳没验 · 7 个 null 图标是否在别处另有）——本轮没碰。
5. **`atlasindividual_assets_40ktraiticonatlas.bundle` 里 78 个 `Atlas_trait_icon_*` 与这 77 条的对应关系没核**
   （P3 §五「下一步」第 3 条）。产物给的是 **Sprite 名**（`Atlas_trait_icon_*`），对表可以直接做，本轮没做。

**顺手发现的（已写进产物，不需要再做）**

- `Ecstasy` 的 `customObject.guid` **与它自己的 `effectAnimGuid` 是同一个**（`42abf1e9…`）。
  「多用途对象」在这里就是它那条触发演出。
- 产物 `stackable` 分布：`add`(10) **74 条** / `count`(20) **1 条**（`markOfChaos`）/ `null` 2 条。
  （74 比 73 多 1 是因为 3 条 oath 共用 1 个对象。）
- `traitEffects` 条数分布（按对象）：0 条 **13** 个 · 1 条 **55** 个 · 2 条 4 个 · 3 条 1 个 · 4 条 1 个 · 5 条 1 个。

---

## 六、复跑方法

```bash
# 生成（覆盖写）
"D:/2/Warpforge_tools/py312/python.exe" -X utf8 -I d:/4/Unity/工具/export_trait_collection.py

# 只校验（跑全部断言 + 与盘上逐字节比，不写文件；不一致退出码 1）
"D:/2/Warpforge_tools/py312/python.exe" -X utf8 -I d:/4/Unity/工具/export_trait_collection.py --check

# 换输出路径
... --out <另一个路径>
```

⚠️ 依赖 `UnityPy 1.24.2`（`D:/2/Warpforge_tools/py312` 里已装）。
⚠️ 换 build 之后**必须重新验二进制布局**（P3 §三 那个反推法），否则「字节数断言」会先炸 —— 这是好事，不会静默错。
