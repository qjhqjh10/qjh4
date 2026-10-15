# R9 · 卡牌 id 真值挖掘（2026-10-15 第三十三会话）

> 缘起：用户要求「**彻底解决卡牌ID问题，应该是各个阵营的卡牌都能对应上**」。
> 方法：**UnityPy 1.24.2**（`d:/2/Warpforge_tools/py312/`）直接读**现客户端**（Steam，2026-08-15）
> `StreamingAssets/aa/StandaloneWindows64/` 的 **84 个 bundle**，把里面的 **PPtr（`m_FileID` + `m_PathID`）逐个落到本地对象上**。

---

## 一、🔑 找到的那把钥匙

原版有一套 **`{targetId: "<卡id>", reference: PPtr}`** 形式的引用；当那个 PPtr **落到本地对象**上时，就等于**直接把「卡 id」和「一个带卡名的对象」配起来了**。

实测命中 **两处**：

| 出处 | 形状 | 解出什么 |
|---|---|---|
| **`prebuiltdecks_assets_all`** 的卡组 SO：`deckAvatar` + `cardArmy` | `{m_FileID:2, m_PathID:…}` | 落到 **`cosmeticsso_assets_all`** 的 **`Avatar_<阵营>_<卡名>`** 对象身上 ⇒ **该卡组的【督军】是谁** |
| **`cosmeticsso_assets_all`** 的掉落表 SO（`DT …`）的 `obtainableItems` | `{targetId:"SW4", reference:…}` | ⛔ 这些 PPtr **落不到本地**（目标是卡 SO，在远端 `allcards` 里） |

⇒ **能彻底解决的是【督军 id】；普通卡的 id 仍然只在 `allcards`。**

---

## 二、✅ 督军 id —— **彻底解决**

**对 236 副原版预组卡组**逐副解 `deckAvatar` → `Avatar_<阵营>_<督军名>`，得到 **`heroId → 督军名` 全表**
（落盘 → `_heroId_truth.json`，62 个 id）。

**用它逐阵营校验我们池的督军卡**：**归一化前缀（`Warlord_`/`Presale_`/`Starter_`）与空格下划线之后**，

- **一致的：53 条**
- **近似（立绘用简称/头衔、我们用正式全名）：15 条 —— 不算错**
  （例 `Warlord_Haarken` ↔ `Haarken Worldclaimer` · `Presale_Abaddon` ↔ `Abaddon the Despoiler`）
- 🔴 **真不一致：9 条**

### 2·1 🔴 真不一致（9 条）

| id | 原版督军 | 我们池 | 核 PnP 后的判定 |
|---|---|---|---|
| **`SW2`** | **Njal Stormcaller** | `High King of Fenris`（type=**tactic**） | 🔴 **真错** —— PnP `Space Wolves/1督军/` 只有 3 位：**Logan Grimnar · Njal Stormcaller · Ragnar Blackmane** |
| **`SW3`** | **Ragnar Blackmane** | `Tyrnak and Fenrir`（type=**unit**） | 🔴 **真错**（同上） |
| **`AM3`** | **Commissar Deckler** | `Commissar Elan` | 🔴 **真错** —— PnP 该位写的是 **`01_Commissar-Denkler`**（拼作 Denkler）；**「Commissar Elan」两边都不是** |
| **`ASH76`** | **Jain Zar** | `Lhykhis` | 🔴 原版这一位是 Jain Zar（`ASH74` 也是）⇒ 两个号都是 Jain Zar 系 |
| **`GOF75`** | **Ghazghkull**（Presale） | `Warboss Gordrang` | 🔴 `GOF28` 也是 Ghazghkull ⇒ 75 是变体号 |
| **`UM65`** | **Marneus Calgar**（Presale） | `Chaplain Letharius` | 🔴 `UM46` 也是 Calgar ⇒ 65 是变体号 |
| **`TL74`** | **Neurogaunt** | `Terror of Vardenghast` | 🔴 待复核（PnP `Tyranid/1督军/` 确有 `1_Terror-of-Vardenghast`） |
| `SAU36` | `Nemesor Zhandrekh` | `Nemesor Zahndrekh` | ⚠️ **原版立绘拼 `Zhandrekh`、PnP 印 `Zahndrekh`（h 的位置）** ⇒ 两边不一致，**以 PnP 为准**（铁律 7） |
| `SOR65` | `Celestian Sacresanct` | `Celestian Sacresant Aveline` | ⚠️ 立绘用**职业名**、我们用**人名** ⇒ **不算错** |

### 2·2 由此定死的三条硬结论
1. **`SW1` = Logan Grimnar · `SW2` = Njal Stormcaller · `SW3` = Ragnar Blackmane**
   —— 我们错误的根源：**SW 的 id 用了 PnP 文件号**（督军分别在 `1督军/Warpforge_01/04/09`，于是被编成 SW1/SW4/SW9）。
2. **`DA75` = Master Zacharial** —— 由 `DA_SK_4`（"The Vengeful Brethren"）的 `deckAvatar → Avatar_DA_Master Zacharial` **独立确证**。
3. **`cardArmy` 数值表补全**：`… 120 = EmperorsChildren · 130 = SpaceWolves`（130 是在 Avatar SO 上实读到的）。

---

## 三、⚠️ 用【原版预组卡组】当真值，逐阵营核对 id 集合

**236 副卡组的 `cardLibraryIds`（= 真值 id）逐阵营比对**：

| | |
|---|---|
| **原版用了、我们没有的 id** | **只有 10 个：`DA65`–`DA74`** |
| 我们有、原版预组没用过的 id | 89 个（**多数正常** —— 预组本来就不会用到每一张，各阵营普遍差 3–5 个） |

**异常的那几组**：
- **`DA76`–`DA86`（11 个）** + `DA59`/`DA60`/`DA61`（防御卡，预组不用）
- **`UM87`–`UM101`（15 个）** + `UM52`–`UM54`
- **`SW59`–`SW68`（10 个）**
- **`GOF76`** 等

⇒ **`DA65`–`DA74` 是原版【真有】、我们池里【完全没有】的 10 个号**；而 **PnP 的 Dark Angels 目录里没有这 10 张卡**（PnP 78 个名字 vs 我们池 83，**我们比 PnP 还多**）⇒ 它们是**数字版专有**、PnP 未收录。

---

## 四、⛔ 没解决的：普通卡的 id（判据边界，写清楚）

**逐条试过、全部排除：**

| 试过什么 | 结果 |
|---|---|
| **84 个本地 bundle 全扫**，找带 `uniqueId` + `cardName` 的 MonoBehaviour | **0 命中** ⇒ 本地**一个卡 SO 都没有** |
| 掉落表 `DT …` 的 `obtainableItems`（`{targetId:"SW4", reference:PPtr}`） | PPtr **落不到本地**（目标是卡 SO，在 `allcards` 里） |
| 卡组 SO 的 `deckHero.genericObjectReference` / `reference` | 同上，落不到本地 |
| `Avatar_SW_*` SO 自己的字段 | 只有 `uniqueId` = **Avatar 自己的 GUID**，**不带卡 id** |
| `catalog.bin`（现客户端 1.3 MB） | 键全是 GUID，搜 `SW1`/`Master Zacharial`/`Card_` **各 0 命中** |
| CCD 下载（拼 URL） | **主机活着**（HTTP 400，0.44s），但 URL 模板要 `{EnvironmentName}/{BucketId}/{Badge}` —— **三个都是 UUID、运行时从后端下发**（`AddressablesManager__SetupCCDManager` 从运行时对象拷进静态字段，代码无硬编码），**后端已关 ⇒ 拼不出来**；两边 `aa/settings.json` 的 `m_CcdManagedData` **四个字段全空** |
| 旧 profile 的 `Player.log` / `RemoteConfigCache.json` | 无 CCD 参数 |

⇒ **普通卡（含 SW 全族 65 张非督军、新 UM 15 张、新 DA 11 张）的 id 真值，本机拿不到，唯一判据仍是一份更新的 `allcards`。**

---

## 五、SW 那 68 张的**结构**已经对出来了（可用于将来一键修）

由「3 督军 + 65 其他」与 PnP 的 `3+6+36+20+3 = 68` 两边吻合，推出原版 SW 的**分块**：

| 号段 | 张数 | 内容 |
|---|---|---|
| **`SW1`–`SW3`** | 3 | **督军**（Logan Grimnar · Njal Stormcaller · Ragnar Blackmane） |
| **`SW4`–`SW39`** | 36 | **部队** |
| **`SW40`–`SW65`** | 26 | **计策**（含天赋） |
| **`SW66`–`SW68`** | 3 | **防御卡** |

我们现在的编号是 **`SW1`(督军) + `SW4`(督军) + `SW9`(督军) + 混排**（= PnP 文件号）⇒ **全族需重编**。
⚠️ **块内的顺序仍未知**（那要 `allcards` 里的 `m_Name` 序号）。

---

## 六、产出物
- **`_heroId_truth.json`** —— `heroId → 督军名` 真值表（62 条，由 236 副卡组的 `deckAvatar` 解出）
- **`_cardid_truth.json`** —— `targetId → 对象名`（159 条；**其中 7 条是 `Avatar_<阵营>_<卡名>`，其余是掉落表/商品名，不是卡**）
- 本文件


---

# 补：引擎版本三处对照（2026-10-15 现读 · 用户提醒后补核）

| 谁 | Unity 版本 | 判据 |
|---|---|---|
| **原版·现客户端** | **`6000.2.6f2`** | `LocalLow/Everguild/Warpforge/Player.log` 的 `Initialize engine version: 6000.2.6f2 (4a4dcaec6541)` ＋ `UnityPlayer.dll` 内的版本串 |
| **原版·那份旧 profile 那一次运行** | **`2022.3.38f1`** | `d:/2/远端缓存_旧profile_1013/Everguild/Player.log` 的同一行 |
| **我们工程** | **`6000.3.23f1`** | `CLAUDE.md:686` 的 `UNITY=` |

🔴 **由此得出一条此前没记过的事实**：**同一个 Steam 安装，在两个时间点跑的是两个不同引擎**（`2022.3.38f1` → `6000.2.6f2`）
⇒ **这个游戏做过一次【大版本重建】**，而那份旧 profile 的缓存（含 `allcards` 1,195 张）是**重建之前**的构建。
⇒ 这是「我们池里 118 张 join 不上」的**根本原因**（不是"包没到手"，是**两份快照相隔一次大版本重建**）。

⚠️ **口径订正（用户 2026-10-15 提出「这些卡牌其实是我们后来加进去的」）**：
**证据不支持"是我们加的"** —— 那 118 张（尤其 SW 全族）**在原版里处处都在**：
现客户端有 SW 三个专包 · 原版**预组卡组包**里有 **14 副 SW 卡组**（`SW_SK_1/2/3` · `SpaceWolvesDeck1–6` · `Deck_SpaceWolves_Starter`，`cardLibraryIds` 用 `SW1`–`SW58`）·
原版**掉落表**里有 `DT SpaceWolves R2` · 原版 `cosmeticsso` 里有 **41 个 `Avatar_SW_*`**。
⇒ 准确说法是「**原版官方后来才加进游戏的**」，不是我们加的。


---

# 补：这 118 张卡的「发动效果」在本机有什么（用户 2026-10-15 问）

**分三层，答案不一样：**

| 层 | 是什么 | 载体 | 118 张本机有没有 |
|---|---|---|---|
| **① 通用执行器（代码）** | **51 种 `AbilityEffect` 枚举**的执行代码（`doDamage` · `buffOthers` · `buffSelf` · `summonUnit` · `stun` · `drawCard` …） | `Assembly-CSharp`（反编译 `d:/2/tools/decomp_full/`，25,096 个方法体） | ✅ **有** —— 但是**机制层、全卡共用**。🔴 **一张卡都没有专属 C# 类**（按 `wolve`/`njal`/`ragnar`/`logan` 命名的方法体全库只 1 个、还是 `GameAnalytics__LogAnalyticsEvent` 撞词） |
| **② 表现层** | `animInfo`：**粒子 AssetGUID + 起止位置选项 + 时长 + 缓动曲线 + 移动偏移** | `*cardanims_assets_all` | ✅ **有** —— `spacemarinesspacewolvescardanims_assets_all` **38 个**（`Atk_Cut Wulfen` · `Buff_SW_Blizzard_Board Center` · `Space Wolves Summon` · `Environmental Condition Space Wolves ×4` …） |
| **③ 规则层** | 卡 SO 的 **`cardAbilities[].abilityLogic`**（**内联对象、不是指针**）：`chosenAbility` 枚举 + `playersAffected` + `targetCriteria` + `multiplier` + `cardAnimations`(GUID 列表) + … | **`allcards`（远端）** | ⛔ **没有** |

**🔑 关键结论：效果是【数据驱动】的 ⇒ 这 118 张卡【不需要一行新代码】。**
它们用的是同一套 51 种效果执行器；**只要拿到它们的 `abilityLogic` 数据，现有引擎就能跑**。

**🔑 另一个有用的东西：效果词典本机有** —— `d:/2/远端缓存_旧profile_1013/导出/AbilityEffect_枚举词典.tsv`（**52 条**，`value / name / 出现次数`，
例 `0 drawCard 86` · **`10 doDamage 251`** · `30 buffOthers 503` · `40 buffSelf 174` · `70 summonUnit 84` · `130 stun 30`）。
**校验**：`SOR36 Exorcist` 的 `chosenAbility = 10` = **`doDamage`**，与它卡面 `Deal 5 damage to all enemies` **对得上**。

**⇒ 所以本机能给这 118 张卡的只有「效果长什么样」（表现层，38 个）；「效果做什么」（规则层）还差那份 `allcards`。**
