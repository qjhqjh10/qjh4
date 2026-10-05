# F1 · `BattlePostFx` 把共享 profile 资产当自己的副本在写（A295）—— 修复报告

> 写手：F1（批次 1）· 2026-10-11 · 白名单：`MyGame/Assets/CardPresentation/Battle/BattlePostFx.cs` ·
> `资料/AnimFX_实现与接线.md`（只改了这一行：§八表格 `WFModulePostProcess` 那一行）
> **没跑自检**（红线）· 跑过 `工具/typecheck.sh`（读数见 §⑥）

---

## ① 结论

**诊断成立，已按三条要求全部改到位，并且比「只改一行」多做了两层收口。**

| 要求 | 做了什么 |
|---|---|
| 1. `:83` 真建副本 | 改成 `vol.profile = vol.profile;`（**RHS 先过 getter** ⇒ 副本先建出来）＋ 紧跟一条**身份判据**：`ReferenceEquals(prof, vol.sharedProfile)` ⇒ 出声 + 计数 + **拒绝往下写** |
| 2. `Detach` 按进场态还回 | `Attach` 记下进场那一刻的 `active` / `texture.overrideState` / `contribution.overrideState` / `contribution.value`，`Detach` 原样写回（⛔ 不再无条件 `false`）；**我们补的那个 `ColorLookup` 连组件一起收掉** |
| 3. 订正错的注释 | `BattlePostFx.cs:38-57` 文件头改为「更正：原来写 X、实际 Y、错因 Z」；`资料/AnimFX_实现与接线.md:170` 同源的错话就地改掉 |

**修完后 13 场的静态推演 = 全绿**（逐场见 §附一）：`bad = 0` · `withLut = 7` ⇒ `BattleScene.cs:8770` 那条 ★ 汇总应转绿，
`battlearenaaeldari` / `battlearenaemperorschildren` 两条 `✗` 也不再出现。

⚠️ **两点如实说明**：
1. **改坏法与「哪条红」变了**（见 §附二）。修完后运行时再也写不到资产上 ⇒ 那条 ★ 逐场对账**不再是本缺陷的第一道防线**；
   第一道防线换成我加在 `Attach` 里的那条**身份判据**（它更早、且在写脏之前就挡住）。两者都在，改坏法都说得清。
2. 我**没有**动 `Editor/BattleScene.cs` 的任何期望值（红线），也**没有**给它加断言（不在白名单）。§附三里列了两处**该加而未加**的（只报不改）。

---

## ② 证据（全部亲读，不是推的）

**引擎源码（本机真源码，`MyGame/Library/PackageCache/`）**

| 事实 | 出处 |
|---|---|
| **建副本的是 getter**：`m_InternalProfile == null` ⇒ `CreateInstance<VolumeProfile>()` + 把 `sharedProfile.components` 逐个 `Instantiate` | `com.unity.render-pipelines.core@0bb36005e9ba/Runtime/Volume/Volume.cs:77-98` |
| **setter 是裸赋值**：`set => m_InternalProfile = value;` ⇒ 原来那句 `vol.profile = vol.sharedProfile` 一份副本都没建 | 同上 `:99` |
| 副本**保真**（关键：改前从没走过这条路，这次是第一次真走） | `m_OverrideState` 是 `[SerializeField]` → `VolumeParameter.cs:46-47`；`m_Value` 是 `[SerializeField]` → `:168-174`；`parameterList` **不是**序列化字段、由 `OnEnable` 用反射从**公开字段**重建 ⇒ 值只存在字段里、`Instantiate` 一定带上 → `VolumeComponent.cs:215-231` |
| 谁在读 profile 的组件表 | `VolumeManager.cs:640-655` `OverrideData` **每帧现读** `volume.profileRef.components` 并解引用 `component.active`（⇒ 表里留一个**已销毁**条目 = 下一帧 MissingReference） |
| `VolumeComponent.Override` 只搬 `overrideState == true` 的参数 | `VolumeComponent.cs:255-300`（`if (!toParam.overrideState) continue;`） |

**原版判据（⛔ 不是我们自己的常量）**

- `d:/2/解包整理/07_场景/<场>/MonoBehaviour/ColorLookup*.json`：**13/13 场** `texture.m_OverrideState` 与
  `contribution.m_OverrideState` **全 = 1**；`texture.m_Value.m_FileID = 0`（= 包内自带 LUT）的正好 **7 场**。
- 我**逐份核过盘上 13 份 `Assets/WarpforgeArena1/arenas/<场>/Profiles/<场>_PostFx.asset`**：
  · 7 场有 `ColorLookup`，`m_OverrideState: 1`（texture 与 contribution 都是）、贴的纹理名与
    `LUT <manifest.postFx.profile − " PostProcessing">` **逐字相同**、`contribution = 1`（与 manifest 一致）；
  · 另 6 场（arena1/2/3 · astramilitarum · blacklegion · darkangels）**资产里根本没有 `ColorLookup` 组件**
    （`grep m_Name:` 只有 Bloom / Vignette）⇒ 与 `ArenasWithOwnLut` 的 7/6 两档自洽。

**日志实证（`d:/4/_tmp_view/battle.log`，主对话那一跑）**

- `§27 战场：…` 行：本进程**只跑过 4 个战场**（`battlearena1` 多次 · `battlearena2` · `battlearenaaeldari` · `battlearenaemperorschildren`）。
- `:36099` / `:36223` 两条 `✗` = aeldari / emperorschildren（**跑过实战 ∩ 有静态 LUT** 这个交集，一个不多一个不少）；
  `:36375` 是那条 ★ 汇总（`bad 2` · `withLut 7`）。
- 反面校验：同为「跑过实战」的 arena1/arena2 那条**不红** —— 这条下文 §④ 专门解释（与诊断无关，但当时确实是个疑点）。

---

## ③ 改动清单

### `MyGame/Assets/CardPresentation/Battle/BattlePostFx.cs`（运行时程序集 · 99 增 / 14 删 · 258 → 343 行）

| 行 | 改了什么 |
|---|---|
| `:38-57` | 文件头「为什么能安全改 profile」**整段重写**：标明「2026-10-11 更正」、保留原文（错话）、给出真源码 `Volume.cs:79-98/99`、说清它是**怎么被抓出来的**（`BattleScene.cs:8749-8774` 的 A243 逐场对账）以及**为什么半年没人发现**（只脏内存、不落盘、重启就"好了"） |
| `:75-77` | 新增诊断计数 `public int SharedProfileRefused`（正常恒 0） |
| `:91-96` | 新增字段 `_prof`（我们那份副本）· `_addedCl`（槽是我们补的）· `_clActive0`/`_texOverride0`/`_contributionOverride0`/`_contribution0`（**进场态**） |
| `:111` | 🔴 **`if (vol.sharedProfile != null) vol.profile = vol.sharedProfile;` → `vol.profile = vol.profile;`** |
| `:115-130` | 🔴 新增**身份判据**：`ReferenceEquals(prof, vol.sharedProfile)` ⇒ `SharedProfileRefused++` + `Debug.LogError` + `return false`（**拒绝往资产上写**）。判据本身来自引擎源码（getter 必 `CreateInstance` ⇒ 两者恒不同引用），不是我们的常量 |
| `:133-153` | `_cl` 拿到之后**先记进场态再写**（原来是先写 `active = true`、`Detach` 又无条件 `false`）；`Add<ColorLookup>` 的路径置 `_addedCl = true` |
| `:302-338` | `Detach` 重写：`texture.value` 交还 `_original`；`overrides`/`contribution`/`active` **按进场态写回**；我们补的那个槽走 `_prof.Remove(typeof(ColorLookup))`（公开 API，顺带置 `dirtyState`）+ `DestroyImmediate`；清 `_prof`/`_addedCl` |

### `资料/AnimFX_实现与接线.md:170`

原文：「…在**运行时副本**上补一个（`vol.profile = vol.sharedProfile` 强制出副本，**不弄脏资产**）…」
改为：**标 2026-10-11 更正**，写明那句**本身是错的、而且就是缺陷所在**，给出 `Volume.cs:99/:79-98` 两处出处，
说明现改成 `vol.profile = vol.profile` + 身份判据，并注明「**不弄脏资产**这句现在才成立」＋抓出它的断言位置与判据。
（该行其余内容一字未动；行尾仍是 LF，`git diff --numstat` = `1 1`。）

---

## ④ 没查清的部分（如实标注）

1. **⚠️ 一个仍未坐实的机制（但不影响修复）**：为什么那 6 场（`Add<ColorLookup>` 的那批，含 arena1）**没有**被
   `BattleScene.cs:8750-8752` 判成「这场原版指的是共享的**恒等** `LUT Normal` ⇒ 我们不该接，现在接了一个」。
   · 事实是：第一趟 arena1 的 attach 日志写「`ColorLookup` 是运行时补出来的 **1 次**」，而第二趟起全是 **0 次**
     ⇒ `TryGet<ColorLookup>` 之后能命中 ⇒ **那个补出来的组件当时确实进过某份 profile 的 `components`**；
   · 而 `:35948`（同一份实例、同一条 `Volume`）读到的却是「没有 ColorLookup」⇒ 二者只能靠「**列表里那条已失效**」调和。
   · **最像的解释（未坐实）**：`Add()` 造出来的是 `CreateInstance` 的**运行时物体**（不是资产的子资产、也没 `DontSave`）
     ⇒ 切场景时被 Unity 收走，只在 `components` 里留一个**假 null** 条目 ⇒ `TryGet` 仍返回 true（`comp.GetType()` 匹配）
     而 `cl != null` 为假 ⇒ `has = false`（= 绿）。旁证：`VolumeProfile.OnEnable` 专门写了
     `components.RemoveAll(x => x == null)`（`VolumeProfile.cs:56`）—— URP 自己就防这一手。
   · ⛔ **不写成已证结论**。它的意义是：**当时资产上确实被塞进过一个没人管的条目**（那一刻若发生 `SaveAssets` 就是坏引用）；
     **修完后这条路不存在**（`Detach` 自己 `Remove` + 销毁）。要坐实只需一条实验（本次不跑）：在批处理里连打两次
     `Attach`/`Detach` 并跨一次 `EditorSceneManager.NewScene`，看 `TryGet` 与 `== null` 是否背离。
2. 原版 `LUTBlender.originalLUTTexture` 的**赋值点**仍然没查到（老账，文件头第 ① 条如实标注**不改**）。
3. `ArenasWithOwnLut`（7 场）与 `ArenaBuilder.AllArenas`（13 场）的清单本身我只做了**静态核对**
   （与 `d:/2/解包整理/07_场景/*/MonoBehaviour/ColorLookup*.json` 的 `m_FileID` 两档逐场吻合），
   **没有**在运行时逐场实跑 13 局（那是主对话的活，且要 `WF_ARENA` 逐场跑 13 次）。

---

## ⑤ 顺手发现（**只报不改**）

1. 🔴 **`Editor/BattleScene.cs:8949` 的 `has` 写法会把「列表里有个已销毁的条目」判成 `false`**：
   `prof.TryGet(out cl) && cl != null && cl.active` —— `TryGet` 返回 true 时 `cl != null`（Unity 重载的 `==`）**仍可能是假**
   ⇒ 两种完全不同的状态（「本来没有这个槽」与「槽在但对象没了」）**长得一样**。这正是 §④-1 那个疑点能藏住的原因，
   也是本次缺陷**没被它抓出来**的原因之一（当时它是绿）。建议：`TryGet` 真而 `cl == null` 时**单独出一声**（A2xx）。
2. **`BattleDriver.cs:1344-1346` 那条日志的读数会变**（口径变化，不是回归）：修完后，6 场「原版没有 ColorLookup 槽」的战场
   **每次 attach 都会报「补出来 1 次」**（`Detach` 把补的槽收掉了、下一局还得补）；旧代码里第二次起会报 0 次（因为留在了资产上）。
   那个计数现在的含义是「**按局计的补槽次数**」—— 若自检/文档里有谁按「本进程第一次」理解它，需要跟着改口径（我没找到这样的地方）。
3. 全仓只有 `BattlePostFx` 一个**运行时**写 `Volume` profile 的地方（`grep sharedProfile|VolumeProfile` 共 26 处，
   其余全在 `ArenaBuilder` 建场期或 `BattleScene` 断言里）⇒ 这条修完，**没有第二处同类**。
4. 改前 `Attach` 的 `vol.profile = vol.sharedProfile` 还有个**没被记过的副作用**：那句让 6 场的 `Add<ColorLookup>(true)`
   （`SetAllOverridesTo(true)`，连 `texture` 都打勾、值是 null）有机会落在**共享资产**上 ⇒ 一旦落上，
   `VolumeComponent.Override` 会把「`texture = null` + `contribution = 1`」推上栈。视觉上与默认值等价 ⇒ 看不出来，
   但它是**资产污染**。修完后一律落在副本上，且 `Detach` 会摘掉。

---

## ⑥ 跑过的检查

| 检查 | 读数 |
|---|---|
| `TMPDIR=/tmp/wf_f1 bash d:/4/Unity/工具/typecheck.sh`（第 1 次） | 运行时 **0 错** / 编辑器 **6 错** —— **全部**在 `Editor/SettingsScene.cs:610,628,1502,1775,1936,2056`（`bar` / `area` 未定义），**不是我负责的文件**（别的写手正在改它，简报里点名的禁区）⇒ 按 CLAUDE.md 13·3「错误全在别的文件上 = 不是你的问题」处理 |
| 同上（隔 ~100 秒重跑） | 运行时 **0 错** / 编辑器 **0 错** ✅ 交回前两条都 0 |
| Unity 自检 | **没跑**（红线：自检由主对话在同步点跑） |

**建议主对话在同步点跑哪几条**（铁律 12 第 1 条：只动一个宿主就只跑那一条）：本次只动了
`CardPresentation/Battle/BattlePostFx.cs`；它的宿主是 `Editor/BattleScene.cs`（`BattleDriver` 也用它，但 `BattleDriver` 只由这个宿主覆盖）
⇒ **只需 `BattleScene.Run`**。判据看日志末的「断言合计」（`1296/1` → 应变成 `1297/0` 或 `1296/0`，**以实际数字为准**），
⛔ 别拿退出码当判据（那条是已知间歇 139）。

---

## 附一 · 修完后 13 场的静态推演（逐场，判据 = 盘上资产 + manifest + 原版资源）

逐场走的是 `BattleScene.cs:8756-8773` 那段（`withLut` 计数 + `ColorLookupMismatch` 的六个条件）：

| # | 场 | 盘上资产 | 原版档 | 六个条件 | 该场结果 |
|---|---|---|---|---|---|
| 1 | `battlearena1` | 无 `ColorLookup` | 指共享恒等 `LUT Normal` ⇒ **不该接** | `has = false` ⇒ `why = null` | ✅ 绿 |
| 2 | `battlearena2` | 无 | 同上 | 同上 | ✅ 绿 |
| 3 | `battlearena3` | 无 | 同上 | 同上 | ✅ 绿 |
| 4 | `battlearenaaeldari` | 有，`texture.ovr=1`、`contribution.ovr=1`、值 `LUT Battle Arena Aeldari`、`contribution=1` | **有专属** | 六条全过（名字与 manifest 逐字一致） | ✅ 绿（修前 ✗） |
| 5 | `battlearenaastramilitarum` | 无 | 不该接 | `has = false` | ✅ 绿 |
| 6 | `battlearenablacklegion` | 无（manifest 的 profile 名是 `Battle Arena 4`） | 不该接 | `has = false` | ✅ 绿 |
| 7 | `battlearenadarkangels` | 无 | 不该接 | `has = false` | ✅ 绿 |
| 8 | `battlearenaemperorschildren` | 有，`…/LUT Battle Arena Emperors Children`、两个 `ovr=1` | **有专属** | 六条全过 | ✅ 绿（修前 ✗） |
| 9 | `battlearenagenestealers` | 有，`…/LUT Battle Arena Genestaler Cults`（原版拼写就是 `Genestaler`） | **有专属** | 六条全过 | ✅ 绿 |
| 10 | `battlearenaleviathan` | 有 | **有专属** | 六条全过 | ✅ 绿 |
| 11 | `battlearenasororitas` | 有 | **有专属** | 六条全过 | ✅ 绿 |
| 12 | `battlearenaspacewolves` | 有 | **有专属** | 六条全过 | ✅ 绿 |
| 13 | `battlearenatauviorla` | 有 | **有专属** | 六条全过 | ✅ 绿 |

⇒ **`bad = 0` · `withLut = 7`**（= `ArenasWithOwnLut.Length`）；★ 汇总由 `✗` 转 `✓`，两条逐场 `✗` 消失。
**这条推演的前提就是本次修复**：修完后运行时**一条写都不会落到 `arenas/*/Profiles/*_PostFx.asset` 上**
（`Attach` 的身份判据 + `Detach` 只写副本），所以那段循环读到的**恒等于盘上的样子**（= 上表）。
修前那种「红不红取决于**这一跑跑过哪几场实战**」的不确定性消失 —— 这正是本缺陷最坏的地方（**断言的结果依赖执行顺序**）。

---

## 附二 · 判别力 / 改坏法（三档，全说清）

| 改坏法（改哪一句） | 谁红 | 红在哪一步 |
|---|---|---|
| **A** `:111` 退回 `vol.profile = vol.sharedProfile;`，**保留**身份判据 | `SharedProfileRefused` ≠ 0 · `Debug.LogError` · `Attach` 返回 false ⇒ **`BattleScene.cs:3449`「★ 后期下游就绪」红** | **在写脏之前**（拒写）—— 这是现在最靠前的一道 |
| **B** `:111` 退回旧句，**并删掉**身份判据（= 退回缺陷态） | **`BattleScene.cs:8770`「★ 13 场的 `ColorLookup` 都跟原版那一档对得上」红** ＋ `:8768` 那两条逐场 `✗`（aeldari / emperorschildren 报「没打 override」）—— 就是这次抓到它的那条 | 跑过实战**且**有静态 LUT 的那几场（顺序相关：不跑就不红） |
| **C** `Detach` 的 `_cl.texture.overrideState = _texOverride0` 改回 `= false` | ⚠️ **如实地讲：改完不会红** | 它现在只写**副本** ⇒ 资产不受影响；这条改坏法**由 A 兜底**（真指回资产时 A 先响）。**不变的判据**是「`_cl` 的来源在 `Attach` 那一趟就锁定了」（要么在副本上找到、要么补在副本上）⇒ `Detach` 的写**天然落不到资产上**，这也是 `Detach` 不必再来一次身份判据的原因 |

**为什么我认为「现成的 ★ 汇总」单独不够、必须补 A**：
修完 B 那条路之后，★ 汇总**只在 A 也一起被删掉时才红** ⇒ 它是「**末端现象**」的断言（资产已经被写脏才看得见），
而且**依赖跑过哪几场**（这次 arena1/2 跑过却没红，就说明它对这个缺陷本身并不敏感）。
A 是**起因侧**的断言（「我手里这份到底是不是工程资产」），在 `.asset` 上一条写都还没发生时就把局面钉住。

---

## 附三 · 建议主对话另行安排的（⛔ 我不改，不在白名单）

1. 给 `Editor/BattleScene.cs` 那段逐场对账补**两条**（D1 §A-6 也提过）：
   · **身份**：跑一局后 `!ReferenceEquals(gv.sharedProfile, gv.profile)`（= `HasInstantiatedProfile()` 为真）
     ＋（编辑器里）`AssetDatabase.GetAssetPath(gv.profile) == ""`；
   · **零脏位**：跑完 `EditorUtility.GetDirtyCount(<profile asset>) == 0` 对**13 份**资产各查一次
     （专治「只脏内存、不落盘」这一类 —— 现在全仓只有 `SettingsScene.cs:2035-2044` 会清自己的脏位）。
2. `ColorLookupMismatch` 的 `has` 加一声（见 §⑤-1）：`TryGet` 真而 `cl == null` ⇒ 出声，别合并进「没有这个槽」。
