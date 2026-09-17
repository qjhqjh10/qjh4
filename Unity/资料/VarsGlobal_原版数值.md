# `VarsGlobal` 原版数值表（2026-09-17 解出）

> 🔴 **这张表以前被记成「⛔ 永久拿不到」—— 那是错的。**
> 它一直在本地，只是**抽取管线漏了这个资产**：`VarsGlobal`（`m_Name = GlobalVariables`，PathID 451）
> 存在 **`Warpforge_Data/sharedassets0.assets`** 里，而那个 .assets **没有 type tree**
> ⇒ UnityPy 读不出字段名 ⇒ 全树 grep 字段名 0 命中。
>
> **怎么读出来的**（两件事拼起来，缺一不可）：
> ① **字段名与顺序** ← `d:/2/Warpforge_code/Scripts/Assembly-CSharp/VarsGlobal.cs`（签名桩，声明顺序就是序列化顺序）
> ② **值** ← 直接从 `sharedassets0.assets` 的原始字节按序解（int/float/bool/Vector/数组）
> 脚本：**`工具/read_varsglobal.py`**（跑一下就把整表打出来）。
>
> ⚠️ **自检办法**（别信印象）：脚本用
> `deckSize=30 · 1.7 · 2.5 · 30.0 · 25.0 · 18.0 · maxSecToReconnect=30` 这串字节当锚点 ——
> 它在两份安装副本（`d:/2/unity_run_ref/` 与 `d:/2/Warhammer 40k Warpforge/`）里**各命中一次、偏移都是 0x7FA8C0**。
> 另一组交叉验证：`targettingAnimTime(+0x88)=0.5`、`cardInHandMovingScale(+0x44)=1.1`、
> `minionInPlayScale(+0x68)=0.75`、`enemyUnitsScaleRatio(+0x74)=0.86` —— 四条独立的偏移反推都对得上。

## 一、整表（48 个字段，按声明顺序）

| 字段 | 值 | 我们哪儿在用 / 该不该用 |
|---|---|---|
| `deckSize` | **30** | 牌组张数（对上了） |
| `tipDuration` | 1.7 | — |
| `introStageStepTime` | 2.5 | — |
| `waitForHumanMatchMaking` | 30.0 | 联网匹配等待（我们单机用不上） |
| **`mulliganTimeLimit`** | **25.0** | 🔴 **换牌倒计时总秒数**（`BattleDriver.mulliganSeconds`，2026-09-17 已改成 25） |
| `offenceTimeLimit` | 18.0 | — |
| `maxSecToReconnect` | 30 | 断线重连（我们没做） |
| `deathTimeWarlordDuration` | **0.5** | 督军阵亡时长 —— 特效线 §七 一直缺的那个数 |
| `deathTimeMinionDuration` | **0.2** | 小兵阵亡时长（我们原来「拍」的是 0.4） |
| `chatBoxDefaultDuration` | 2.5 | 聊天条停留时长（第 4 行「单位语音条」要用） |
| `spellPlayedScale` | 1.0 | — |
| `cardInHandMovingScale` | 1.1 | 手牌移动缩放 |
| `enemyCardDisplayScale` | 1.4 | 敌方展示卡缩放 |
| `enemyCardScaleMobileMultiplier` | 1.3 | — |
| `enemyCardDisplayYoffset` / `Xoffset` / `MobileXoffset` | 24 / −26 / 25 | 敌方展示卡偏移 |
| `offensiveCardDisplayYoffset` / `Xoffset` / `MobileXoffset` | 30 / −25 / −25 | 进攻展示卡偏移 |
| `minionInPlayScale` | 0.75 | 场上小兵缩放 |
| `addCardToDeckShowScale` | 1.2 | 「往牌库加牌」的展示缩放 |
| `cardDrawnScale` | 1.4 | 抽到手的牌放大到多少 |
| `enemyUnitsScaleRatio` | 0.86 | 敌方单位相对缩放 |
| `timeToScaleIntoMoving` | 0.1 | 手牌「起身」用时 |
| `minMovementToPlayCard` | 0.3 | 判「拖动够不够远」的阈值 |
| `maxHeightOfUnplayableCard` | 25.0 | — |
| `cardInHandShownHeight` | 20.0 | — |
| `targettingAnimTime` | 0.5 | 选择目标时的动画时长 |
| `minionDeathTime` | 0.2 | 小兵死亡用时（与 `deathTimeMinionDuration` 同值） |
| `growInCemeteryTime` | 0.3 | 进墓地的长大用时 |
| `cardInCemeteryScale` | 0.65 | 墓地里那张卡的缩放 |
| `cardInCemeteryPos` | (−8.14, −0.2) | 墓地卡的位置 |
| `minionReassembleTime` | 0.15 | — |
| **`timeToPositionCard`** | **0.2** | 🔴 **手牌重排用时**（我们原来「借」的 0.18） |
| **`timeToDrawPlayerCard`** | **0.3** | 🔴 **抽牌（我方）用时**（我们原来「借」的 0.55） |
| `timeToDrawEnemyCard` | 0.15 | 抽牌（敌方） |
| `timeToDissolveCard` | 0.2 | 溶解用时 |
| `playerActionPriority` | 0 | 事件优先级（`EventTiming` 的时间线该照这个排） |
| `instantPriority` | 1 | 同上 |
| `deathPriority` | 1 | 同上 |
| `backlashPriority` | 1 | 同上 |
| `turnEndPriority` | 0 | 同上 |
| `tipQueueSize` | 3 | — |
| `timeDisplayAbility` | 0.4 | 技能展示时长 |
| `heroLvl2XP` | 100 | — |
| `maxHeroLevel` | 100 | — |
| `divisionsValues` | [0, 500, 1000, 1750, 2500] | 段位分段（排位用） |

## 二、还剩没解的

- **`VarsDevice`**（`VarsDesktop` / `VarsMobile`）在 `解包整理/09_游戏数据/去重定义/MonoBehaviour/` 里 —— 那是**另一张表**
  （分端参数），没跟 `VarsGlobal` 一起读。要用再读一次（同一个办法）。
- 本表**只有一份来源**（两份安装副本内容相同）。改动前照旧先跑 `工具/read_varsglobal.py` 复核。
