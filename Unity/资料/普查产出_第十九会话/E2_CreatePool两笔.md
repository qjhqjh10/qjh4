# E2 — `CreatePool` 两笔（`A1424` 复算 · `A1470` 收口）

> 会话：第十九会话 · 2026-10-18 · 写手 E2 · 独占文件 = `RuleEngine/Core/CreatePool.cs`
> 报告路径 = `资料/普查产出_第十九会话/E2_CreatePool两笔.md`
> ⛔ 没跑 Unity · 没跑 `typecheck.sh` · 没动 git · 没改正本。

---

## 〇、怎么复算的（**用真代码**，⛔ 不是简化复刻）

铁律 10 的教训（「先查后做 ⇒ 零返工」）在这里的正解是：**别写第二份实现**。

做法 = 照 `工具/ruleprobe` 的成文路子（`RuleEngine/Core/` 只碰 `UnityEngine.Debug`/`Random`
⇒ 一个 30 行桩就能把整个 Core 编成 net8 控制台），**在自己的一次性目录里**建了一个探针：

```
D:/tmp/wf_e2_probe/          # 复算器（收口后的实现）
D:/tmp/wf_e2_orig/           # 同一份 Program.cs + 【收口前】的 CreatePool.cs
D:/tmp/wf_e2_prea1078/       # 同上 + 把 A1078 那半段停掉（模拟 2026-10-18 之前）
```

- `<Compile Include="D:/4/Unity/MyGame/Assets/RuleEngine/Core/*.cs" />` —— **源码只读引用，一个字没改**；
  只实现卡池装载 + 调用，**一行规则都没重写**。
- 口径逐字照 `RuleEngineTest.TestMentionedCards` ⑤ / ⑤·b：`pool` = 全池 1126 张（现读），
  `self` = 该卡，`text` = `c.Desc`（英文档）/ `c.DescZh`（中文档），`max = 999`；
  统计 = Σ 命中数（`mentions`）+ 去重后的被点名卡数（`distinct`）。
- 顺带把 `RuleEngineTest` ⑤ 里那 **9 个逐张期望数**当保真自检跑了：

| 卡 | 我实测（en / zh） | `RuleEngineTest` 那个数 | |
|---|---|---|---|
| `Shock Trooper` | 9 / 9 | 9 | ✅ |
| `Dark Pact of Excess` | 8 / 8 | 8 | ✅ |
| `Necron Warrior` | 4 / 4 | 4 | ✅ |
| `Grey Hunter` | 4 / 4 | 4 | ✅ |
| `Chosen` | 0 / 0 | 0 | ✅ |
| `Abaddons Chosen` | 3 / 3 | 3 | ✅ |
| `Chosen of the Four` | 0 / 1 | 0（英文）/ 中文那 1 处见 `:17545` | ✅ |
| `Chosen of Slaanesh` | 1 / 1 | 1 | ✅ |
| `Reanimate` | 8 / 12 | 8（英文）/ 12（中文） | ✅ |

⇒ 探针与引擎**同口径**（这不是「我觉得对」：9 个数逐字吻合，且其中 3 个数与**被测代码无关**、
是数据字面推出来的）。

---

## 一、`A1424`——「英文侧 128 处」复算不出来

### 1·1 现算（**真代码**）

```
### EN max=999 => mentions=125 distinct=64
### ZH max=999 => mentions=132 distinct=67
```

与 `RuleEngineTest` 里钉住的那四个数**逐字相同**（`125`/`64` → `RuleEngineTest.cs:17547,17552`；
中文 `132`/`67` → `:17665,17666`）。

### 1·2 「128 vs 122」到底是哪一种 —— **两个都错，但错法不同**

| 数 | 谁说的 | 判定 |
|---|---|---|
| **128** | `CreatePool.cs:727` 那句注释（本轮之前的原文） | 🔴 **过期**，不是错 —— 它是 `A1078` **之前**的英文侧值 |
| **122** | 第十四会话 `A1405` 写手那份**简化**复算器 | 🔴 **简化造成**的假读数（真值 125，差 3） |
| **125 / 64** | 本轮用真代码现算 | ✅ 当前真值 |

**「过期」这一半是【证出来的】、不是推的**：把 `StripTalentSegments` 末尾那句
`return StripBareTalentSegment(s, self);` 改成 `return s;`（＝**恰好**把 `A1078` 补的那半段停掉）
重编重跑 ⇒

```
### EN max=999 => mentions=128 distinct=67      ← 真的退回 128 / 67
### ZH max=999 => mentions=132 distinct=67      ← 中文侧不动（A1078 那一支只认英文段首大写）
```

与 `RuleEngineTest.cs:17337` 记的那句「英文档全池 `128 / 67` → `125 / 64`」**逐字吻合**。
⇒ 结论：**128 是 `A1078` 之前的数、当天就被那半段改掉了**（英文侧 −3：
`Azrael` / `Aun'Va` / `Abaddon the Despoiler` 各少一处假命中）。**账上那个数该就地订正。**

### 1·3 顺手订正的第二处：那句注释里还有两个说法不成立

原话：「全池实测**唯一一处可见差**：`复生`…⇒ 全池 **+4** 处命中（**128** → 132）」。

逐张 dump 出来的真实差（**收口前**那版实现，也就是账上那句话写下时的行为）：

```
D DA3 Azrael                en[1]=Supreme Grand Master   zh[0]=
D SAU9 Flayed One           en[0]=   zh[1]=Reanimate
D SAU10 Gauss Reaper Warrior en[0]=  zh[1]=Reanimate
D SAU72 Immortals Phalanx   en[0]=   zh[1]=Reanimate
D SAU29 Lokhust Heavy Destroyer en[0]= zh[1]=Reanimate
D GOF17 Burna Boy           en[1]=Mekboy Gazmek   zh[2]=Ork Spanner,Mekboy Gazmek
D TAU1 Aun'Va               en[1]=Ethereal Supreme   zh[0]=
D SAU65 Awakened Obelisk    en[0]=   zh[1]=Extermination Protocols
```

- **「唯一一处可见差」不成立**：逐张看是 **8 张**（收口后 7 张，见下），不是 1 处。
- **「+4」只是净值、不是某一族**：`复生` 那族确实是 +4（`SAU9/10/29/72`），
  但另有两族是 **−1**（`Azrael` / `Aun'Va` 在英文侧有假命中、中文侧没有，把
  `Burna Boy` / `Awakened Obelisk` 的 +1 抵掉了）⇒ 净值看着正好 +4。
- **现状是 +7**（`125 → 132`）：`A1078` 之后 `Azrael`/`Aun'Va` 不再差，而
  `BL1 Abaddon the Despoiler` 冒出来（中文 `四神宠儿` **没有** `天赋：` 前缀 ⇒ 仍命中
  `Chosen of the Four`）。

### 1·4 改了什么

`RuleEngine/Core/CreatePool.cs:725-749`（`MentionedCards` 的 doc 注释）——
**就地订正 + 保留更正痕迹**（铁律 5）：写清「原来写 X（128/+4/唯一一处）、实际是 Y（125/64、+7、4 族 7 张）、
错因是 Z（`A1078` 当天落地 + 拿净值当单族）」，并把复算办法留在注释里。

⚠️ **正本那一行我没法改**（不在白名单）⇒ 请调度台落盘：

- `资料/任务_卡组卡牌战斗.md:47`（`A1424` 那一行）—— 里面「英文侧 128 处」「算得 122」两个数都该订正成
  **125 / 64**，并把「要查清是哪一种」换成结论：**128 是 `A1078` 之前的过期值（已证）、122 是简化假读数**。

---

## 二、`A1470`——`CreatePool` 两处天赋段剥离，收口成一处

### 2·1 收口前是什么样

```
StripTalentSegments(text, self)        // :837  ← 管前缀：IndexOf("Talent:"/"天赋：") → 挖到下一个 `.`/`。`
    └─ return StripBareTalentSegment(s, self)
StripBareTalentSegment(s, self)        // :879  ← 管裸写：逐段扫（`.`/`。`/换行）
```

两处**都在实现同一条规则**（「这一段归天赋那一层 ⇒ 不算正文点名」），而且
**两处的「段」定义还不一样**（前者只认句号、后者认换行）—— 正是本仓那条
「两处写同一条规则 = 迟早不一致」的活样本。

### 2·2 收口后

```
StripTalentSegments(text, self)        // 唯一入口：逐段扫一次，段口径 = `.` / `。` / 换行
    └─ TalentCutAt(seg, talentName)    // 唯一判据：返回 -1（不是天赋段）/ ≥0（从这下标起挖）
```

- `TalentCutAt` 两种写法共用：前缀（`IndexOf`，与 `ExtractTalent` 同口径，**不是** `StartsWith`）
  ＋ 裸写（`CardDef.ExtractBareTalentName(seg) == talentName`）。
- `StripBareTalentSegment` **删掉了**（它已并入）。

### 2·3 与 `CardDef.HandledByOtherLayer` 的关系（**如实写清楚，别含糊**）

那边 `:1936`（前缀）/ `:1944`（裸写）**确实是同一条规则的另一处落笔**。但**不能直接拿来当判据**：

1. 它连**伴生 / 光环 / 事件 / 开局上手**一起认 ⇒ 按它挖会把 `Companion 1: <卡名>` 里那个
   **正经点名**一起挖掉（静默少算）；
2. 它**不认中文 `天赋：`**（`ExtractTalent` 只认 `Talent:`）—— 本文件跑中英两趟，不能丢这一支；
3. 它认 `[Talent]:` 方括号写法（`ExtractTalent` 先归一），本文件不认 —— 现扫数据 **0 处**
   （`S13` 已把全池唯一那处 `UM84` 改成裸 `Talent:`，成因记在 `RuleEngineTest.cs:17529`）。

⇒ 收口的是 **CreatePool 内部那两处**（本笔的判据原文）；与 `CardDef` 那边共用的是
**同一个抽取函数**（`ExtractBareTalentName`），上面三条差别已逐条写进注释，
下一个人改「天赋段」口径时**看一处即可**（`TalentCutAt`），三条差别会把他挡回来。

### 2·4 行为证据（⛔ 不是「应该没问题」）

| 判据 | 读数 |
|---|---|
| 全池逐张 dump（1126 张 × `desc`/`descZh` 两趟，含每张命中清单与顺序） | **收口前 vs 收口后逐字节相同**（`diff` 空） |
| 四个总量 | 收口前 `125/64` · `132/67` → 收口后 **一模一样** |
| 编译 | `csc`（net8，编的是同一批 `Core/*.cs`）**0 错 0 警**（清过 `obj/bin` 重编三次） |
| 合成夹具 9 例（`mid` / `head` / `twice` / `bare` / `bare2` / `plain` / `zh` / `zhmid` / `nl`） | 前 8 例**逐字相同**；第 9 例见下 |

**唯一的（故意的）行为差 —— 已在源码注释里写明、⛔ 不是静默改的**：

```
夹具 "Talent: Secret Technique\nDeploy a Shock Trooper."
  收口前 hits=0      ← 段尾只认句号 ⇒ 连【下一行】一起挖掉，把 Shock Trooper 也吃了（少算）
  收口后 hits=1      ← 段尾认换行 ⇒ 只挖天赋那一段
```

- **真池 `0 处`**这种文本（现扫 `desc` + `descZh`：`Talent:`/`天赋：` 共 177 处、每条最多 1 处、
  **落在非段首的 0 处**、**切口跨换行的 0 处**）⇒ **真池行为逐字节不变**。
- 留的那份「段」定义 = 裸写那一支从前的口径，也是 `CardDef` 引用的 `EffectText.Split` 的口径；
  两个写法本来就是同一条规则，「段」的定义只能留一份。
- `RuleEngineTest.TestMentionedCards` 全函数内**没有任何含换行转义的夹具文本**（`grep -F '\n'` 空）
  ⇒ 这条差**不可能**翻掉那里任何一条断言（那 4 个总量数也逐字未变）。

---

## 三、没查清 / 没做的

1. ⛔ **正本 `资料/任务_卡组卡牌战斗.md:47`（`A1424` 那行）我没改** —— 不在白名单，需调度台落盘（订正文案见 §1·4）。
2. ⛔ **另有一处「同一条规则」我没动**：`CardDef.HandledByOtherLayer:1936/1944`（覆盖率报表那一支）
   仍与 `TalentCutAt` 平行。**不能合并**（三条实质差别见 §2·3），且 `CardDef.cs` 不在我的白名单。
   ⇒ 已把三条差别写进 `TalentCutAt` 的注释，**这是能做到的最强收口**。
3. ⚠️ **`RuleEngineTest.Run` 我没有跑**（红线：不跑 Unity）⇒ 「那四条断言仍然绿」这句**不由我下**；
   我能给的只是「它们钉的 4 个数逐字未变 + 断言用的夹具口径未变」。

## 四、顺手发现的（一条一句 + 出处）

1. `CreatePool.cs:609` 那句「别顺手收紧到 3：实测会掉 **21** 处命中（132 → 111）」—— 现核 **132 是对的**
   （中文侧本轮未变），但那句依赖的 `111` 我没复算（要改 `MinCjkNameChars` 才能量），**只报不动**。
2. 那句话里 `RuleEngineTest.cs:17337` 记的 `128 / 67 → 125 / 64` 与我这轮独立复算**逐字吻合**
   ⇒ 那份注释可以当「英文侧历史值」的判据用，以后别再另找。
3. 中文侧那 **+7** 里有 **3 族**（`BL1` 裸写天赋名残留 / `GOF17` 多识出 `Ork Spanner` /
   `SAU65` 的 `Extermination Protocols`）**从来没人记过** —— 账上那句「唯一一处可见差」把它们盖住了。
   出处 = 本报告 §1·3 的逐张 dump；要不要单独立账由调度台裁。
