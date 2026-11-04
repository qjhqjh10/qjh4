# B143 · `ReplayStore` 随机游标两列 · 第三方复核

> 复核人 `B143-W2`（2026-11-05）· **只读复核**：⛔ 没改 `ReplayStore.cs`（归 `B143-W1`）· ⛔ 没改断言文件 · ⛔ 没跑 Unity。
> 复核对象：`CardPresentation/Battle/ReplayStore.cs` 文件尾的 `partial ReplayRecord`（`randomCursorBefore/After` + `ValidateRandomCursors`，`Load:725` 调用）+ 断言 `RuleEngine/Editor/B143_W1b_Assert.cs`。
> 施工单 = `判据/B143_回放随机流契约.md` §3·1 / §3·4 / §3·5 / §4。

---

## 〇、判定

**🟠 需改（两处小改，都不碰运行语义；补丁可直接粘）**

| # | 改哪 | 为什么 | 阻塞吗 |
|---|---|---|---|
| 补丁 1 | `RuleEngine/Editor/B143_W1b_Assert.cs` 的 **A1b**（+ 新增 A1d） | A1b 断的是「缺键 ⇒ **`null`**」，但本仓自己有一条**相反**的运行时观测（缺键的**集合**字段被填成**空表**）。若 `int[]` 同形，A1b 会**假红**，而 A1c（`missing`）照样绿 | **接线前要改**（一行） |
| 补丁 2 | `ReplayStore.cs`：加一格 `[NonSerialized] randomCursorVerdict` | `half` / `count-mismatch` 把两列置 null 之后，**再调一次只会得到 `missing`** —— 结论**降档**，「当初为什么坏」丢了 | 不阻塞（可选） |

**运行语义一条都没问题**：今天 `ValidateRandomCursors` 只在 `Load` 一处调，而两列的**生产者/消费者都还没落地** ⇒ **加载行为逐字不变**，只多一行日志。

---

## 一、四问

### 1 · 老档缺键返回 `missing` + warning（不是 pass）—— ✅ 符合「未验不算通过」

* `missing` 是**独立返回档**（四档 `ok` / `missing` / `half` / `count-mismatch`，`ReplayStore.cs:1086`），不是 `ok` 的变体；
* 文案自己写死「**⛔ 这不是「通过」**」（`:1096-1098`），并说明「回放不恢复游标、也不比它」；
* 断言 A1c（`B143_W1b_Assert.cs:72-73`）断的就是 `"missing"`，⛔ 不是 `"ok"`；
* 回放侧将来接上时，这一档的等价物是 `traceFull` 那条 `NoExpectedTrace`（`BattleDriver.cs:1141` 用 `!= null && Length > 0`）—— 同一个「没记 ≠ 通过」的形状。✅

**「半列 / 条数不符 ⇒ 两列置 null」会不会掩盖真实不一致？—— 安全方向对，但有两处要留意：**

1. ✅ **不静默**：两档都 `Debug.LogError`，并把两列长度打进日志（`:1100-1105` / `:1110-1114`）；
2. ✅ **安全方向对**：留着半截数组才会让回放**逐条拿错游标且不报错**，置 null 是**退回老行为**（老行为本身由 `finalHash` 兜底）；
3. 🟠 **结论会降档（可观测的掩盖）**：置 null 之后**再调一次 `ValidateRandomCursors` 会返回 `missing`** —— 「记坏了」被降级成「没记」。今天没有第二个调用点，所以是**潜在**问题；一旦将来在 `RecFinish`/自检/诊断 UI 里再调一次就会现形。→ 补丁 2。
4. 🟠 **录制端保存前不体检**：本方法只在 `Load` 调；等录制端接上（契约 §3·2 第 5 步），**坏数组会先落盘**、下次读才报。契约第 5 步自己有「条数不符 ⇒ 写 null + error」的等价逻辑，所以**不算缺口**，但落地时**建议 `RecFinish` 里也调一次本方法**（一处调用，零风险）。

### 2 · `randomCursorAfter` 是否真有必要 —— ✅ 设计成立；⚠️ 目前**没有消费端**，代码上无法证伪

* **断言侧（它能验的部分）**：`A2d`（`B143_W1b_Assert.cs:86-91`）拿**刻意不同**的两组值（before `{3,4,7}` / after `{5,5,9}`）分别逐元素比 ⇒ `after` **不是**从 `before` 推出来的，也不是「全 0 蒙混」。✅
* **代码侧（现读）**：全仓 `randomCursorAfter` 的出现处只有——字段声明（`ReplayStore.cs:1073`）、文档注释、`ValidateRandomCursors` 的长度检查、以及上面那条断言。**没有任何生产者、也没有任何消费者**（录制端五处 + 回放端断言都在 `BattleDriver.cs`，尚未落地）。⇒ 「这条列是必需的」目前只是**设计论证**（契约 §3·3 末段：`before` 恢复完再比 `before` 是恒等，只有执行后读数能发现多/少掷），**代码里证不了也证不伪**。
* 结论：**同意保留**（设计论证成立、成本 1 个 int/动作）。⚠️ 消费端落地时必须**只用它做「落地后独立读数」的比较**，⛔ 别写成 `after[i] == before[i] + 期望消耗` 之类的推算（那就又不独立了）。

### 3 · 序列化形状：`null` 数组会不会写成 `[]` 或漏键 —— 判定**稳**，但 **A1b 的前提在本仓有反例**

**先说结论：无论 Unity 给哪种形状，`ValidateRandomCursors` 的分类都不受影响** —— 因为它用的是

```csharp
bool hasB = randomCursorBefore != null && randomCursorBefore.Length > 0;   // ReplayStore.cs:1089-1090
```

`null` / `[]` / 缺键（值 = CLR 默认）**三种形状一律 `hasB = false`** ⇒ 都进 `missing` ✅。这是这段实现**最结实的一点**。

**写入形状**：`Save` = `JsonUtility.ToJson(rec)` 紧凑格式（`ReplayStore.cs:476`），字段是 `public int[] randomCursorBefore;`（`:1064` / `:1073`，**无初始化器**）。

**本仓已有的三条证据（互相不完全一致，如实列）：**

| # | 出处 | 说了什么 | 对 `int[]` 的推论 |
|---|---|---|---|
| ① | `ReplayStore.cs:182-186`（`traceFull`，同样是 `int[]`，**已在产线跑了很久**） | 「缺键的 JSON 读出 `null`（`int[]` 的 CLR 默认值 = 初始化器的值 = `null`，两条路相同）」 | 支持 **null** |
| ② | `ReplayStore.cs:206-219` + `BattleScene.cs:36476`（`★ A8 JsonUtility 初始化器` 封印断言） | `FromJson` **会跑无参构造 ⇒ 初始化器一起跑**；**没有初始化器**就落 `default` | ⇒ **null**（本字段无初始化器） |
| ③ | ⚠️ `RuleEngine/Data/DeckStore.cs:174-179` | **两条独立运行时日志**观测到：「`JsonUtility` 把**缺键**的集合字段给成**空表、不是 `null`**」（`Dto.decks` = `List<PlayerDeck>`、**裸字段无初始化器**） | 若 `int[]` 同 `List<T>` ⇒ **`[]`** |

③ 是**反例**，而且它自己也说「机制本身没读到序列化后端的源码级判据」。三条放一起：**`int[]` 到底是 `null` 还是 `[]`，本机验不了（要跑 Unity）**；但**对判定无影响**，只影响 A1b 这条**断言**会不会假红。

⇒ **建议**：A1b 放宽成「`null` **或** 空表」，并**另加一条**显式 `[]` 形状的夹具（补丁 1）。这样三种形状都有覆盖，且**形状差异本身**会被 A1b 如实记录下来。

**其它形状**：`count-mismatch` 只看 `Length`，与形状无关；`half` 的判据是「一列有数据、一列没有」，`[]` 与 `null` 都算「没有」⇒ 若录制端真写出 `before=[]` + `after=[1,2]`，会得到 `half`（而不是 `ok`）—— 正确。

### 4 · `version` 仍是 5 —— ✅ 没有别的路径会因此拒旧档

* 唯一一处版本闸 = `Load` 的 `if (rec.version != new ReplayRecord().version)`（`ReplayStore.cs:717-723`）：不认识就 **warning + `return null`**；
* `public int version = 5;`（`:134`）**没动**；全仓没有第二处读 `ReplayRecord.version` 做判定的地方（`ReplayBar.cs` / `BattleDriver.cs` 0 命中）；
* 两条新字段是**纯新增**，老 JSON 里没有 ⇒ 不影响解析，也不进任何「字段齐全性」检查（没有这种检查）；
* 契约 §3·4 那句「**同批改随机流语义时必须 5→6**」是**另一条**，本半没碰随机流语义 ⇒ 现在不 bump 正确。✅

---

## 二、可粘贴补丁

### 补丁 1（建议必做）· 断言加固 —— `RuleEngine/Editor/B143_W1b_Assert.cs`

把 A1b 那两行（现读 `:70-71`）

```csharp
            CheckTrue(old.randomCursorBefore == null && old.randomCursorAfter == null,
                      "A1b ★ 旧档缺键 ⇒ 两列都是 `null`（`int[]` 的 CLR 默认值 = 初始化器的值）");
```

换成（并顺手在 A1c 之后加 A1d）：

```csharp
            // 🟠 2026-11-05（B143-W2 复核）：放宽成「null **或** 空表」——
            //   本仓 `RuleEngine/Data/DeckStore.cs:174-179` 有两条运行时观测：
            //   「缺键的**集合**字段被填成**空表**、不是 null」。`int[]` 是否同形本机验不了（要 Unity 跑）；
            //   而 `ValidateRandomCursors` 的 `hasB/hasA` 用的是 `Length > 0` ⇒ 两种形状都归 `missing`（判定不变）。
            bool noB = old.randomCursorBefore == null || old.randomCursorBefore.Length == 0;
            bool noA = old.randomCursorAfter == null || old.randomCursorAfter.Length == 0;
            CheckTrue(noB && noA,
                      "A1b ★ 旧档缺键 ⇒ 两列都**没有数据**（`null` 或空表皆可；下游判据一律看 `Length > 0`）");
```

```csharp
            // ---- A1d：显式空表形状（`ToJson` 对 null 数组可能写 `[]`）----
            const string emptyArr = "{\"version\":5,\"savedAt\":\"x\",\"actions\":[{\"seq\":0}],"
                                  + "\"randomCursorBefore\":[],\"randomCursorAfter\":[]}";
            var eaOld = JsonUtility.FromJson<ReplayRecord>(emptyArr);
            CheckTrue(eaOld != null && eaOld.ValidateRandomCursors("B143W1b:empty-array") == "missing",
                      "A1d ★ `[]` 形状（键在、表空）同样 ⇒ `missing`（与 A1c 同档，⛔ 不许判成 `ok`）");
```

> 灭自证不变：把字段写成 `= new int[0]` 初始化器 ⇒ A1d 仍绿（空表本来就该 `missing`），但 A1c 的**缺键**路径会跟着变 —— 那正是 A1c/A1d **两条分开**的意义。

### 补丁 2（可选）· 结论留存 —— `ReplayStore.cs` 的 `partial ReplayRecord`

字段区（紧跟 `randomCursorAfter` 之后加一格）：

```csharp
        /// <summary>最近一次体检的结论（`ok` / `missing` / `half` / `count-mismatch`）。
        /// ⚠️ `half` / `count-mismatch` 会把两列置 `null` ⇒ **再调一次只会得到 `missing`**（结论降档）；
        /// 想知道「当初是为什么坏的」读这一格。⛔ 不进 JSON（`[NonSerialized]`）。</summary>
        [System.NonSerialized] public string randomCursorVerdict;
```

四个 `return` 之前各写一格：`randomCursorVerdict = "missing";` / `"half";` / `"count-mismatch";` / `"ok";`（**不改返回值、不改 null 行为**）。

> ⚠️ **必须带 `[System.NonSerialized]`** —— `JsonUtility` 只序列化 public 字段，不加这一格会**进录像文件**（老代码读新档虽不影响，但白占体积、还让「字段清单」多一条）。

### 补丁 3（等录制端落地再做，不是本半）· `RecFinish` 保存前也调一次 `ValidateRandomCursors`

契约 §3·2 第 5 步已有「条数不符 ⇒ error + 写 null」的等价逻辑；接上后**再调一次本方法**可让「半列」也在保存前被拦（今天没有半列生产者，所以不急）。

---

## 三、证据清单（逐条 文件:行号）

| 事项 | 出处 |
|---|---|
| 两列字段（无初始化器） | `CardPresentation/Battle/ReplayStore.cs:1064` / `:1073` |
| 体检口四档 + `Length > 0` 判据 | `ReplayStore.cs:1086-1119`（`hasB/hasA` = `:1089-1090`） |
| 唯一的产线调用点 | `ReplayStore.cs:725`（`Load` 里，紧跟版本闸之后） |
| 版本闸（唯一） | `ReplayStore.cs:717-723`；版本字段 `:134` |
| 写盘形状 | `ReplayStore.cs:467-477`（`JsonUtility.ToJson` 紧凑） |
| `int[]` 缺键 ⇒ null 的既有先例 | `ReplayStore.cs:182-186`（`traceFull`）+ `BattleDriver.cs:1141`（`!= null && Length > 0`） |
| `FromJson` 跑初始化器（A8 结案） | `ReplayStore.cs:206-219` + 封印断言 `Editor/BattleScene.cs:36476` |
| ⚠️ 反例：缺键集合 ⇒ 空表 | `RuleEngine/Data/DeckStore.cs:174-179`（两条运行时日志） |
| 断言 A1/A2d/A3/A4/A6 | `RuleEngine/Editor/B143_W1b_Assert.cs:58-119` |
| 今天无任何生产者/消费者 | 全仓 `randomCursorBefore/After` 的命中只有：上述字段、注释、`ValidateRandomCursors`、断言文件 |

---

## 四、没查清 / 要给 Unity 的

1. **`int[]` 缺键到底是 `null` 还是 `[]`** —— 三条证据里有一条相反（见 §一·3），**本机验不了**。补丁 1 就是为此而设（两种形状都过）；真想知道形状，在 A1b 里加一句 `Debug.Log` 打出 `randomCursorBefore == null ? "null" : "len=" + Length`，接线跑一次就有答案。
2. **`ToJson` 对 `null` 数组写 `[]` 还是省略键** —— 同上，跑一次 A2（新档往返）后看落盘 JSON 即可。
3. 本复核**没有跑** `RuleEngineTest` / 任何宿主 ⇒ 断言是否真绿未知。

---

## 五、给 `B143-W1` 的交接

* 数据模型 + 体检这一段**判定正确、方向安全**（`missing` ≠ 通过 ✅ · 半列置 null 是**退回老行为**而不是掩盖 ✅ · 版本不 bump ✅）。
* 请落 **补丁 1**（防 A1b 假红）；**补丁 2** 可选（防结论降档）；**补丁 3** 等录制端。
* 回放端落地时那条 `after` 断言（契约 §4·2）请**只用 `after` 做独立读数**，⛔ 别拿 `before` 推。
