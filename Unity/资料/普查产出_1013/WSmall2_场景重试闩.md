# WSmall2 · 场景侧重试闩（A553）

> 执行写手 **WSmall2** · 2026-10-13 · 「清空 A 表」第四轮 · 账 = **A553**
> 白名单内动了 **1 个文件**：`Battle/ScenarioBlendables.cs` —— 本件 **+180 / −15**
> （该文件累计 `git diff --numstat` = **+336 / −22**，其中 **+156 / −7 是 W-B2（A514）先落的**）。
> ⛔ 没跑 Unity（没跑任何 `-executeMethod`）· ⛔ 没动 git · ⛔ 没改正本 · ⛔ **没碰 `Editor/BattleScene.cs`**
> —— 只跑了**秒级类型检查**（两次，都 0 / 0）。
> 行号一律是**改完之后**的现读值（文件现 2637 行）。

---

## 一、结论

1. 🔴 **本件的全部改动就是一句话的位置**：`_sceneStanTried = true;` 从 `LoadSceneStandalone()` 的
   **第二条语句**（`if (_sceneStanTried) return;` 紧跟着）挪到了**最后一行**（`ScenarioBlendables.cs:2078`）。
   ⇒ 四条失败路（**资源取不到** / **JSON 解析失败** / **缺 `sceneStandalone` 一节** /
   **缺 `sceneStandaloneBuild` 一节**）**都不置位** ⇒ 下一次调用**老老实实再读一遍**（自愈）。
2. ✅ **重报走去重**（新增唯一一口 `NoteSceneStanFail`，**进程内静态 `HashSet`**，key = 理由）
   —— 同一原因只出声一次。⇒ 「自愈」与「不刷屏」**两条同时成立**（§四逐条对账）。
3. 🆕 **为了让「失败可重试」断得出来**，加了两样**只给自检**的东西：
   开关 `ForceSceneStandaloneLoadFailForTest(bool)`（让读走**真实失败路**）
   + 计数口 `SceneStandaloneLoadAttempts`（真读了几次）。
   ⚠️ **没有它们这条改动不可测** —— 只把两节按掉（`ForceSceneStandaloneMissingForTest(false)`）的话，
   下一次调用会**真读成功**，而「改前 / 改后」在那条路上**读数完全一样**（都是 `5`）
   ⇒ 那就是「**弱断言分不出两种状态**」。详见 §五 开头。
4. 🔴 **顺带补掉一个 W-B2 没点到的第 6 处静默口**：`sceneStandalone` 读到了、
   `sceneStandaloneBuild` 缺（消费方 `:2096` 的守卫会因此**整场都不建**）—— 原来**一个字都不留**
   （只有「缺 `sceneStandalone` 一节」才报）。现在出声。它同时是「**成功**」定义的另一半（§二·5）。
5. ✅ **W-B2 那 10 条断言（A514 段，`Editor/BattleScene.cs:10609-10804`）一条都没动、也不受影响**
   —— 逐条核过，理由见 **§六**（含一条**我特意没做**的事：没去重 `:2109` 那条「一条都没建」，
   因为它被 `missB == 2` 钉着）。

**一句给调度台的话**：本件**没有**动 `Editor/BattleScene.cs` ⇒ 断言**由你落地**，
§五 给了**可直接粘贴的代码**（含探针、三个判据、两条 🧨 改坏法）。

---

## 三行速览（判据 → 改法 → 怎么验）

| | |
|---|---|
| **判据** | ① `W-B2_场景AnimFX静默口.md` §六·2（「`_sceneStanTried` 闩在失败时也是**永不重试**」+ 它自己写的修法与代价）· ② `LoadSceneStandalone()` 现读（W-B2 改后的现状）· ③ 同族去重先例 `Battle/Label.cs:935` 的 `_dotAlignNoted` |
| **改法** | 置位挪到最后一行 + 四条失败路各自 `return`（不置位）+ 重报走**进程内去重** |
| **怎么验** | 新开关造**真失败** ⇒ 看「**真读次数**」与「**关掉开关后能不能读回来**」两个数（§五，附仿真读数） |

---

## 二、改动清单（`文件:行` · 改前 · 改后 · 为什么）

文件统一是 `Unity/MyGame/Assets/CardPresentation/Battle/ScenarioBlendables.cs`。

| # | 行（改后） | 改前 | 改后 | 为什么 |
|---|---|---|---|---|
| 1 | `:1990` → **`:2078`** | `_sceneStanTried = true;` 是函数**第二条语句**（`if` 之后立刻） | 挪到**最后一行**（四条失败路都 `return` 之后） | **本件的全部目的**。判据 = 那几条失败路**都是「环境 / 资产」状态、以后可能变**（同 A514 §三那张表：⑤「没读到」**不该认领**、⑥「这一场没条目」**该**认领 —— 这里是 ⑤ 那一类） |
| 2 | `:2043-2048` | `var ta = Resources.Load<TextAsset>("EnvBlendables");` + `Debug.LogError("取不到 …")` + `return` | 同一句前面**多一个自检开关**（`_sceneStanLoadFailForTest ? null : …`）；`LogError` 收进 `NoteSceneStanFail("资源取不到", …)`；`return`（**不置位**） | 出声**保留**（真故障仍是 `LogError`，级别与改前一致）+ 接去重 + **允许重试** |
| 3 | `:2050-2059` | `var f = JsonUtility.FromJson<…>`；`f == null` 时**落到下一跳**（于是被**误报**成「缺 `sceneStandalone` 一节」） | 🆕 单列一档 `NoteSceneStanFail("JSON 解析失败", …)` + `return`（两个静态字段**照旧写 null** —— 逐字等价于改前那一对三元） | **诊断不假**：解析失败与缺节是两回事、修法也不一样（一个查文件坏没坏、一个查生成器）；原来会把人指错方向 |
| 4 | `:2062-2069` | `Debug.LogError("…没有 `sceneStandalone` 一节…")`（**只出声**，**照旧置位**） | 同一段文案收进 `NoteSceneStanFail("缺 sceneStandalone 一节", …)` + `return`（**不置位**） | 同上：缺节可能只是**旁挂旧了 / 重生成到一半**，重读能救 |
| 5 | `:2070-2077` | **什么都没有**（静默 —— 那一档消费方 `:2096` 会**整场都不建**，日志里却是空的） | 🆕 `NoteSceneStanFail("缺 sceneStandaloneBuild 一节", …)` + `return`（**不置位**） | 🔴 **本件之前就存在的第 6 处静默口**（W-B2 点的是 4+2 处，没点到它）。它同时是「**成功**」定义的另一半 —— `BuildSceneAnimFx` 那道守卫是 `_sceneStan == null \|\| _sceneStanBuild == null` |
| 6 | `:2079-2083` | —— | 🆕 失败过之后再读成功 ⇒ `Debug.Log` **一次**「**重试成功**…（此前失败 N 次）」 | 「自愈」**要看得见**（⛔ 不是「悄悄好了」—— 静默的另一半也算失败）。只在 `_sceneStanFails > 0` 时响 ⇒ 正常路径**一个字节都不多** |
| 7 | `:2086-2122` | —— | 🆕 `NoteSceneStanFail(reason, why, fix)`：**失败出声的唯一一口** | 见 §三（key）与 §四（去重） |
| 8 | `:2124-2130` | —— | 🆕 `public static int SceneStandaloneLoadAttempts`（只读） | 「**真读了几次**」= 唯一能把「可重试」与「永久放弃」分开的数（§五） |
| 9 | `:2132-2148` | `SceneStandaloneDataCount()` 里**内联**的计数循环 | 抽成 `SceneStanItemCount()`，**两处共用**（`SceneStandaloneDataCount` 改调它） | 第 6 条那句「重试成功」也要报这个数 ⇒ **同一规则只留一份**（两处写同一条规则 = 迟早不一致） |
| 10 | `:2152-2169` | `ForceSceneStandaloneMissingForTest(bool)` | **行为一字未改**，只补注释：① 它**仍要**继续手动置 `_sceneStanTried`（A514 ② 的前提）② 本件之后它是**唯一**能造出「失败也闩住」的入口 | ⛔ 那一行的存在意义变了（生产路径已经造不出那个状态）⇒ 不写清，下一个人会把它当「多余的一行」删掉，**A514 ② 当场红** |
| 11 | `:2171-2199` | —— | 🆕 `public static void ForceSceneStandaloneLoadFailForTest(bool)` | 见 §五：自检**必须**有它才断得出两种状态 |

**新增的静态位（`:1987-2002`）**：`_sceneStanTried`（语义改了，加了注释）· `_sceneStanAttempts`（新）
· `_sceneStanFails`（新）· `_sceneStanLoadFailForTest`（新）· `_sceneStanNoted`（新 `HashSet<string>`）。

**没改的**：`SceneStandaloneFile` / `AnimFxBuildGroup` 两个 DTO · `BuildSceneAnimFx` 的**行为**
（`:2095` 那句 `LoadSceneStandalone()` 照旧）· `ForceSceneStandaloneMissingForTest` 的**行为** ·
类里其余 2500 行。

---

## 三、去重设计（key 怎么拼 · 与同族已有 key 不撞的核对）

### key 的拼法（只有一份，就在 `NoteSceneStanFail` 里）

```text
key = "LoadSceneStandalone·" + reason            （+ "·自检强制"  —— 仅当自检开关开着）
```

四个 `reason`（**都是常量字面量，拼在调用点上**，`:2046` / `:2055` / `:2064` / `:2072`）：

| reason | 什么时候 | 级别 |
|---|---|---|
| `资源取不到` | `Resources.Load<TextAsset>("EnvBlendables")` 回 null | 真故障 `LogError`；**自检强制**那一档 `LogWarning` |
| `JSON 解析失败` | `JsonUtility.FromJson` 回 null | 同上 |
| `缺 sceneStandalone 一节` | 那一节 null 或长度 0 | 同上 |
| `缺 sceneStandaloneBuild 一节` | 那一节 null（🆕 本件补的） | 同上 |

**为什么 key 要带 `reason`**：这四条**是不同的故障、修法不同**（前两条查文件，后两条查生成器）
⇒ 第 1 条报过**不该**把第 4 条吞掉。**「同一个理由只响一次」才是硬约束**，不是「整个函数只响一次」。

**为什么自检那一档要多一段后缀**（这是**有意的**、不是随手加的）：
`ForceSceneStandaloneLoadFailForTest(true)` 合成的那一次如果与真故障**同 key**，
那么**同一批处理里先跑自检** ⇒ 之后真故障那一行**再也看不到**（静默复发，而且**只在同一个进程里现形** ——
和 `Battle/Label.cs:912-914` 那条警告是同一个坑）。分两段 ⇒ 自检那一次不吞真故障。

### 「与同族已有 key 不撞」的核对（逐条查过，**不是**凭印象）

| 同族已有 | 装在哪 | 键是什么 | 会不会撞 |
|---|---|---|---|
| `_dotAlignNoted`（`Battle/Label.cs:935`，`NoteDotAlign` 的口） | **`Label` 那个类自己的私有静态** `HashSet<string>` | `which + "\|" + 节点全路径`（8 个口：`左对齐` / `右对齐` / `逐行左对齐` / `折行宽` / `换行模式` / `重排` / `字号` / `自适应框`） | ❌ **结构上不可能** —— **两只不同的 `HashSet` 实例**（不同类、不同字段）。撞的前提是「有人把两个口并进同一只集」，那是另一回事 |
| `_warnedClip`（**本类自己的**，现 `ScenarioBlendables.cs:2619`，配合 `ResetClipCache()` `:2610`） | 同一个类里的**另一只** `HashSet<string>` | **clip 的 guid**（`:2555` `if (_warnedClip.Add(guid))`） | ❌ **不可能** —— 又是**另一只集**，且键是 guid；我的键以 `LoadSceneStandalone·` 开头 |
| `Shell/ItemDrawer.cs` 的 `Note` · `Core/CardIcons.cs:91` 的 `_warned` · `Core/Tooltip.cs:392` 的 `_warnedNoEntry`（`Label` 注释里点名的同族先例） | 各自类里的静态集 | 各自 | ❌ 同上，不同集 |

**✅ 结论**：key 与同族已有 key **不撞**，而且**挡这件事的是结构**（每只集是各自类的私静态），
不是「我挑了个别人没用的字符串」。⚠️ **但同一只集内部**（`_sceneStanNoted`）的分段纪律是真的硬约束 ——
已写进 `NoteSceneStanFail` 的头注（「新加理由要么另起一段名字、要么想清楚它该不该被吞」）。

---

## 四、「自愈」与「不刷屏」两条各自怎么满足的

### 「自愈」（= 本件的 A553 正题）

| 一环 | 怎么做的 | 拿什么判 |
|---|---|---|
| 闩只在**成功**时置位 | `:2078` 那一句在**四条失败路之后** | 失败后 `SceneStandaloneLoadAttempts` **继续涨**（每次调用 +1） |
| **四条**失败路都**不置位** | 每条各自 `return`（`:2048` / `:2058` / `:2068` / `:2076`） | 关掉自检开关后再读 ⇒ `SceneStandaloneDataCount() == 5` |
| 自愈**看得见** | 失败过之后再读成功 ⇒ 一次 `Debug.Log`「**重试成功**」（`:2079-2083`） | 同一窗口里该片段 **1 条** |
| **不谎报**（W-B2 那半边的另一半） | 失败路**不写** `_sceneAnimFxRoot`（W-B2 已做，`:2112` 那段注释） | A514 ②-a 的 `claimB == false`（**W-B2 的断言在咬**） |

🔴 **代价（如实记，这一条是「必须一起承担」的）**：失败态下**每次调用**都会
`Resources.Load` + `JsonUtility.FromJson`（那个 JSON **679 KB**）。
- 调用面：`BuildSceneAnimFx`（**每次战场 `Load()` 一次**，生产唯一入口 `Battle/ArenaRuntimeLoader.cs:114`）
  + `SceneStandaloneDataCount()`（自检里多次）。
- ⚠️ **这个代价是自愈的前提**，⛔ 不能靠「只读一次」省掉 —— 按简报口径：本件**两条都要**。
- ⚠️ **只在失败态发生**：一旦读到 ⇒ 闩上 ⇒ 一个字节都不再重读。

### 「不刷屏」（= 简报点名的那条「⛔ 别把『每次重报』当成实现正确」）

| 一环 | 怎么做的 |
|---|---|
| **重报的唯一一口** | `NoteSceneStanFail`（`:2086`）—— 四条失败路**全部**走它，⛔ 别处不再打第二行 |
| **去重** | `if (!_sceneStanNoted.Add(key)) return;`（`:2114`）—— **进程内静态** `HashSet`，同一理由只出声一次 |
| **不吞真故障** | 自检合成那一档**另起一段 key**（上面 §三） |
| **⛔ 没做过头** | `BuildSceneAnimFx:2109` 那条「**一条都没建**」（**实例级**、每次调用都报）**保持原样** —— 理由见 §六（A514 ②-a 明写 `missB == 2`） |

**两条一起看**：失败态下每次调用**仍然真的重读**（自愈），但**日志只留一条**（不刷屏）——
这正是「⛔ 别把每次重报当成实现正确」那条要的形状。

---

## 五、断言规格（断什么 · 怎么分辨两种状态 · 改坏法 · 建议落点）

### 为什么必须有新探针（先回答「弱断言分不出两种状态」）

把两节按掉再读（`ForceSceneStandaloneMissingForTest(false)`）**验不出来** ——
那样下一次调用会**真读成功**，而两种实现的读数**一模一样**（都是 `5`）：

| 做法 | 改前（一进来就置位） | 改后（走到底才置位） | 分得出吗 |
|---|---|---|---|
| 只 `ForceSceneStandaloneMissingForTest(false)` ⇒ `SceneStandaloneDataCount()` | 5 | 5 | ❌ **分不出** |
| 让它**真失败一次**再关掉开关（本件的探针） | **−1** + 只读 **1** 次 | **5** + 读了 **4** 次 | ✅ 分出 |

⇒ 所以加了 `ForceSceneStandaloneLoadFailForTest(bool)`（让 `Resources.Load` 那一步**喂 null**，
走的就是生产那条 `ta == null` 失败路，**不另写一套**）+ `SceneStandaloneLoadAttempts`（可数）。

### 判据（三个数 + 两个片段，**必须一起看**）

**逻辑仿真读数**（我按改前/改后两版闩逻辑跑了一遍模型，判据是这三个数一起变）：

| | 三次读数 | 真读次数 Δ | 「失败（资源取不到）」出声 | 「重试成功」出声 | 关掉开关后读数 |
|---|---|---|---|---|---|
| **改后** | −1 / −1 / −1 | **+4** | **1**（去重） | **1** | **5** ✅ |
| **改前** | −1 / −1 / −1 | **+1** | 1 | 0 | **−1** ❌ |

> ⚠️ **第一条（三次读数）两种实现一样** ⇒ 它**单独**是弱断言，**必须**配上后两个数。
> 这正是本仓 §三 那条「弱断言分不出两种状态」要拦的形状。

### 🧨 改坏法（两条，各打一个改法）

1. **把 `:2078` 那句 `_sceneStanTried = true;` 挪回函数第二条语句**（= 改前）
   ⇒ 真读次数 **+1**（不是 4）· 关掉开关后 **−1**（不是 5）⇒ **同一条 `Check` 红**。
2. **删掉 `:2114` 那句 `if (!_sceneStanNoted.Add(key)) return;`**（去重）
   ⇒ 失败出声 **3 条**（不是 1）⇒ 红。

### 建议落点

`Editor/BattleScene.cs` 的 **A514 段之后**（`10609-10804` 之后）、**整轮收尾之前**。
- ⚠️ **不碰任何既有现场**（不需要 `Instantiate` / `DestroyImmediate` —— 全程只读静态缓存 + 日志窗口）。
- ✅ **自带自证**：本段最后一步必须把状态**还原成「已读到 5 条」**（`backE == 5` 那一条就是自证）
  ⇒ 放它在 A514 段**之前**也能过（两个探针互不依赖）；但**建议排在后面**，日志更干净
  （见 §六 那条「会多一行」的提醒）。
- ⛔ **开关必须放 `finally` 里关掉**（写不写对决定后面所有 `BuildSceneAnimFx` 走不走失败路）。

### 可直接粘贴的代码（`Check(...)` / `List<string>` / `Application.logMessageReceived` 都是本文件现成的）

```csharp
        // ---------------- 🆕 2026-10-13（A553）：场景侧重试闩 —— 「只在成功时置位」 ----------------
        // 判据 = 红线「不许静默失败」+「失败可自愈」：`LoadSceneStandalone()` 的 `_sceneStanTried`
        //   原来在【第二条语句】就置位 ⇒ 读失败也记成「读过了」⇒ 同一个进程内**再也不会重读**
        //   （重生成旁挂 / `AssetDatabase.Refresh` 都救不回来，只能重进编辑器 = 域重载）。
        // ⚠️ **为什么非要造一只新开关**：`ForceSceneStandaloneMissingForTest(false)` 只按掉两节 ——
        //   那样下一次调用会**真读成功**，「改前 / 改后」读数**完全一样**（都是 5）⇒ **分不出两种状态**。
        //   必须让失败**真的发生**（`ForceSceneStandaloneLoadFailForTest`）才验得了「可重试」。
        // 🧨 **改坏法**：① 把 `LoadSceneStandalone()` 里那句 `_sceneStanTried = true;` **挪回第二行**
        //   （= 改前）⇒ 真读次数 **+1**（不是 4）、关掉开关后 `SceneStandaloneDataCount()` 仍是 **-1**（不是 5）⇒ 红；
        //   ② 删掉 `NoteSceneStanFail` 里那一句 `if (!_sceneStanNoted.Add(key)) return;` ⇒ 失败出声 **3 条**（不是 1）⇒ 红。
        {
            int a0 = CardPresentation.ScenarioBlendableFactory.SceneStandaloneLoadAttempts;
            var gotE = new List<string>();
            Application.LogCallback hE = (string m, string st, LogType ty) => { if (m != null) gotE.Add(m); };
            int r1 = 0, r2 = 0, r3 = 0, backE = -99;
            CardPresentation.ScenarioBlendableFactory.ForceSceneStandaloneMissingForTest(false);   // 清闩 + 按掉两节
            CardPresentation.ScenarioBlendableFactory.ForceSceneStandaloneLoadFailForTest(true);  // 让读**真的**失败
            Application.logMessageReceived += hE;
            try
            {
                r1 = CardPresentation.ScenarioBlendableFactory.SceneStandaloneDataCount();
                r2 = CardPresentation.ScenarioBlendableFactory.SceneStandaloneDataCount();
                r3 = CardPresentation.ScenarioBlendableFactory.SceneStandaloneDataCount();
                CardPresentation.ScenarioBlendableFactory.ForceSceneStandaloneLoadFailForTest(false);
                backE = CardPresentation.ScenarioBlendableFactory.SceneStandaloneDataCount();      // 关掉开关 ⇒ 该能读回来
            }
            finally
            {
                Application.logMessageReceived -= hE;
                CardPresentation.ScenarioBlendableFactory.ForceSceneStandaloneLoadFailForTest(false);
            }
            int failE = 0, healE = 0;
            foreach (var m in gotE)
            {
                if (m.Contains("**失败（资源取不到）**")) failE++;
                if (m.Contains("**重试成功**")) healE++;
            }
            int dE = CardPresentation.ScenarioBlendableFactory.SceneStandaloneLoadAttempts - a0;
            Check(r1 == -1 && r2 == -1 && r3 == -1 && dE == 4 && failE == 1 && backE == 5,
                  $"★ A553：场景侧旁挂**失败可重试**（三次各回 {r1} / {r2} / {r3}；**真读次数 +{dE}**（期望 **4**）· "
                + $"「失败（资源取不到）」出声 **{failE}** 条（期望 **1** = 去重）· 关掉开关后读到 **{backE}** 条（期望 **5**））"
                + " —— 改前那一版**进了函数就置位** ⇒ 第 2 次起不再重读（+1）、关掉开关后仍是 **-1**（**永久放弃**）");
            Check(healE == 1,
                  $"★ A553：「**重试成功**」那一句**出声一次**（抓到 {healE} 条，期望 1）—— 自愈必须**看得见**"
                + "（⛔ 不是「悄悄好了」，那也算静默失败）；它只在**失败过之后**第一次读成功时响");
        }
```

> 📌 两条 `Check` 的作用分工：第 1 条咬**闩**（可重试 vs 永久放弃）+ **去重**；
> 第 2 条咬**自愈可观测**。⛔ 第 2 条**不能**单独当判据（改前它只是 0，而改前本来就没自愈），
> 它**必须**跟着第 1 条一起看。

---

## 六、会不会动到 W-B2 那 10 条断言的前提（如实说）

**核过的是 `Editor/BattleScene.cs:10609-10804` 整段**（我**只读**，⛔ 一个字没改）。
结论：**不动它的前提，10 条全不受影响**。逐条理由：

| A514 那几条 | 为什么不受影响 |
|---|---|
| **②-a**（`:10706`：两次各回 0 · `missB == 2` · `claimB == false`） | 它用 `ForceSceneStandaloneMissingForTest(true)` ⇒ **手动**把 `_sceneStanTried` 置 true ⇒ 我的 `LoadSceneStandalone()` **在第一条 `if` 就返回**（不读、不报、`SceneStandaloneLoadAttempts` 不涨）⇒ 两次都走到 `:2096` 那条支路 ⇒ `**一条都没建**` 仍是 **2** 条、`已经建过` 仍不出现。**我特意保留了那条探针的手动置位行为**（并补注释说明别删），见 §二·10 |
| **②-b**（`:10713`：`SceneStandaloneDataCount() == 5`） | `ForceSceneStandaloneMissingForTest(false)` ⇒ 真读成功 ⇒ **5**，与改前一致（`:2078` 的置位只是**时机**变，成功那一档行为逐字相同） |
| **①-a / ①-b / ①-c**（`:10650` / `:10661` / `:10667`，节点复用那三条） | 与加载闩无关；那一段要求**真数据在**（要建 `battlearena2` 的 3 个节点）⇒ 我的改动**保证**收工时状态是「已读到 5 条」（同下） |
| **③**（`:10738`，`battlearena1` 没有条目） | 同上，要求真数据在；那条支路（`:2120`）我一字未动 |
| **④-a / ④-b / ④-c**（`:10769` / `:10776` / `:10797`，`MakeAnimFx`） | 走 `MakeAnimFxForTest`，**不经过** `LoadSceneStandalone()`；我一字未动 `MakeAnimFx` |

⚠️ **两条如实标**：

1. **我这段会多打一行日志**（「重试成功」）**—— 但它在 A514 的窗口之外**：
   A514 ② 的 `hB` 是在 `finally` 里**先摘掉**、然后才 `ForceSceneStandaloneMissingForTest(false)`
   ⇒ 之后 `backB = SceneStandaloneDataCount()` 那次（可能）触发的「重试成功」**不被 `hB` 收**，
   且 `backB` 那条 `Check` **只看数值**。⇒ 若 A553 段排在 A514 **之前**，日志里会多这一行，
   **数值判据一个都不变**。（排在后面就没有这一行 —— 这是我建议排后面的原因。）
2. 🔴 **我特意**没有**去重 `BuildSceneAnimFx:2109` 那条「一条都没建」**（实例级、每次调用都报）：
   A514 ②-a **明写** `missB == 2`（「**每次都出声**」）。若调度台认为那一处也该去重
   （= 简报里「同一**实例**…不刷屏」的那一半），则 **②-a 的 `missB` 得从 2 改成 1** ——
   那是**动 A514 段的判据**，而 `Editor/BattleScene.cs` 不在我的白名单里 ⇒ **留给调度台裁**。
   （我的判断：`场景侧没读到` 是**每次战场 `Load()` 至多一次**的事件，不是逐帧刷屏；
   而它咬的是「不许谎报已经建过」这件事，**每次调用都表态**本身有价值。⇒ 建议**保持原样**。）

---

## 七、没查清 / 没做的

1. **所有断言没实跑**（简报口径：⛔ 不许跑 Unity）。我做到的是：**逐条静态推演** +
   **一个 Python 逻辑模型**（§五那张表就是它的输出，模拟的正是「闩在顶 / 闩在底 + 去重 + 开关」这套逻辑）；
   **仍以实跑为准**。
2. 🔴 **没实测 `Resources.Load` 失败时 Unity 自己会不会缓存这个 miss（负缓存）** ——
   我**读不到判据**：不能跑 Unity（简报禁止），也没在 `d:/2/tools/decomp_full/` 里找到能直接回答它的方法体。
   ⇒ **如实说**：本件修的是**我们这一层的闩**（`_sceneStanTried`），这一层**已经修好且可断**；
   「重生成旁挂后同一进程内能不能读到」还**取决于 Unity 那条行为**（若它负缓存，那还得 `AssetDatabase.Refresh`
   或域重载）。⚠️ **这也正是自检用开关合成失败的原因** —— 它验的是**我们这半边**，不依赖 Unity 那条行为。
3. **没实测重读的开销**（失败态下每次 `JsonUtility.FromJson` 679 KB 的耗时）。量级判断：
   只在**失败态**发生、且生产入口是**每次战场 `Load()` 一次** ⇒ 我判断可接受，但**没量过，不改口**。
4. **没给 `_sceneStanNoted` 加 `Reset*ForTest` 清口**（同族的 `_warnedClip` 有 `ResetClipCache()`）。
   理由：断言用的 `·自检强制` 那段 key **不会被真故障那一档吞**（键不同）⇒ 不需要清；
   加了反而是**多一份可被误用的状态**。若调度台想要（比如将来要在**同一个进程里跑两遍**这段断言），说一声我补。
5. **没普查「还有谁在同一个进程里读这份旁挂」** —— 我只核了本文件内的两个调用点
   （`:2095` 的 `BuildSceneAnimFx` / `:2147` 的 `SceneStandaloneDataCount`）
   + 全仓 grep（`BuildSceneAnimFx` 生产调用点 = `Battle/ArenaRuntimeLoader.cs:114` 一处；断言的若干）。

---

## 八、顺手发现（⛔ 别自己顺手改 —— 我一条都没动）

1. 🔴 **`sceneStandalone` 有、`sceneStandaloneBuild` 缺 ⇒ 消费方整场都不建，而原来日志里是空的** ——
   这是本件之前就存在的**第 6 处静默口**（W-B2 那轮点的是 4+2 处，没点到它）。
   **我在本件里补上了**（它是「成功」定义的另一半，§二·5）—— 记在这里是为了让调度台知道
   **它不在 A514 的点名范围里**，属于**本件顺手扩的一点**，若判定越界请裁。
2. 🟡 **同一件事现在有两处在报**（失败态下一次调用最多 2 行）：
   `LoadSceneStandalone` 的**原因级**（我，去重后只 1 条）+
   `BuildSceneAnimFx:2109` 的**实例级**（W-B2，每次调用）。**我认为这是对的**（一个是「为什么没读到」、
   一个是「这个战场实例这一趟一条都没建」），但**真正失败的批次里日志会比从前多**，值得知道。
3. 🟡 **`AnimFXController` 仍然没有 `[DisallowMultipleComponent]`**（W-B2 报告 §六·1 已记，
   **到今天仍然开着**）—— 本件没动（不在 A553 点名范围）。
4. 🟡 **本类的静态缓存里，只有 `_warnedClip` 有清口**（`ResetClipCache()`，`:2610`）
   —— 其余（含我新加的 `_sceneStanNoted`）都没有。若将来要在**同一进程内**重复验
   「第一次会不会出声」，会需要等价的清口（见 §七·4）。
5. 🟡 **`SceneStandaloneDataCount()` 是个「会触发加载」的只读口**（名字看起来像纯读数）——
   在**失败态**下每调一次就重读一次（本件之后）。已在它的头注里写清；
   若将来有人拿它当「纯查询」在热路径上循环调，会踩到（今天没有这种调用点）。

---

## 九、类型检查结果

命令：`TMPDIR=/tmp/wf_ws2 bash d:/4/Unity/工具/typecheck.sh`（跑了 **三次**：主改动后一次、注释订正后再一次、末次 XML 转义订正后一次）

| 次 | 运行时程序集 | 编辑器程序集 |
|---|---|---|
| 第 1 次 | **0** | **0** |
| 第 2 次（`.cs` 改动之后） | **0** | **0** |
| 第 3 次（最后一次 `.cs` 改动之后） | **0** | **0** |

⚠️ **没有出现「错误全集中在不是我负责的文件上」那种情况**（三次都是干净 0/0）⇒ 无需按简报口径重跑。

**行尾**：`Battle/ScenarioBlendables.cs` = **纯 LF**（`b.count(b'\r\n')==0`，LF **2637** / 2637 行）
—— 改前改后都数过，**没翻**（简报点名 W-B2 刚改过它，所以我每次写盘后都复量）。

**`git diff --numstat`**：`Battle/ScenarioBlendables.cs` = **+336 / −22**
（含 W-B2 先落的 **+156 / −7** ⇒ **本件 = +180 / −15**）。
⛔ 没动 git 的索引 / 历史：只用 `git diff` 读，没 `add` / 没 `commit` / 没 `checkout`。
