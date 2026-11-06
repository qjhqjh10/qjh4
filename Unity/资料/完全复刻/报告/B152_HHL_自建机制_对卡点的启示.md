# B152 · HH:L 自建机制 → 对我们「主菜单场景加载失败」的启示

> 调查对象：`E:/2hhl`（`LegionsOffline离线版` + `The Horus Heresy Legions原版`）
> 调查方式：**纯静态阅读**，未执行 `E:/2hhl` 内任何 `.exe/.cmd/.py/.ps1/.js`，未改 `d:/2` 一个字节。
> 已先读并只补差额：`报告/B150_同工作室离线版_机制剖析.md`、`报告/B150_同工作室离线版_内容与数据.md`、`判据/B151_原版离线版_可行性方案.md` §4。
> 日期：2026-11-06 · 只读查判据代理

---

## 0. 一句话结论

**机制不能照搬，思路可以上移一层。**

- **不能搬的原因**：HH:L 的客户端**根本没有 Addressables**（`global-metadata.dat` 里 `Addressables` / `UnityEngine.ResourceManagement` **各 0 次命中**），它的全部资源都走同一个 `AssetBundleManager.GetLoadedBundle` 出口，所以他们只要在那一个点上换文件即可（`probe.js:194-210`）。
- **我们这边**：Warpforge 1.35.0 的 `global-metadata.dat` 里 `Addressables` **112** 次、`UnityEngine.ResourceManagement` **165** 次；而 `AssetBundleManager.GetLoadedBundle` 在**两套全量反编译里都是 0 个外部调用方**（我们现场探针也证实它一次没被调）⇒ **HH:L 那个挂点在我们这儿是死代码，挂钩不会有任何效果**。
- **可以搬的**：把「**不去伪造服务器，直接在加载器出口把远端换成磁盘文件**」这条思路**上移一层**，落到 Addressables 的官方钩子 **`Addressables.InternalIdTransformFunc`**（本 build 确实有：`d:/2/Warpforge_code/Scripts/Unity.Addressables/UnityEngine/AddressableAssets/Addressables.cs:50`）。
- **附带的硬结论（本轮挖到的真因，不是 HHL 的）**：我们 `GroupOperation failed` 的**最内层异常**已经写明是 `RemoteProviderException … /allcards_assets_all.bundle`（`E:/warpforge/07_产物/phase0/probe.log:3375`）——**不是依赖解析坏，是这一组包本来就登记成远端 CCD、我们本地没有**。

---

## 1. HH:L 的加载链（逐跳，带 `文件:行号`）

| # | 跳 | 证据 |
|---|---|---|
| 1 | `启动单机版.cmd` → 包内 `runtime\python.exe` 跑 `OfflineLab\start_offline.py` | `启动单机版.cmd:3,9` |
| 2 | `start_offline.py` 把 `sys.argv` 改成 `probe.py --play`，`runpy` 同进程执行 | `OfflineLab/start_offline.py:30-33` |
| 3 | `probe.py` 把 `assets.json` 的**相对 path 改写成绝对 path**（`BASE.parent/'assets'/名字`），再塞进 JS 模板的 `__BUNDLES_JSON__` | `OfflineLab/analysis_runtime/probe.py:61-64` |
| 4 | Frida **挂起 spawn → attach → create_script → load → resume**（脚本在进程恢复前就位） | `probe.py:82-93,131,153,154` |
| 5 | 等 `gameassembly.dll` 载入，挂 `il2cpp_init` 的 `onLeave` 才装全部钩子 | `probe.js:2644-2690` |
| 6 | `SteamManager.Awake/Update/OnDestroy` 换空桩；`LoginManager.Start` **整个替换成空函数**（原版登录流程根本不跑） | `probe.js:132-145`、`probe.js:150-152` |
| 7 | `PlayerDataManager.Start` 用 attach，`onLeave` 里 `Invoke("NCGIEJIIBAK", 0.25f)` | `probe.js:1000-1010` |
| 8 | `LoginManager.NCGIEJIIBAK` 被替换成 `offlineBoot`（原方法体不执行） | `probe.js:997-999` |
| 9 | `offlineBoot` 里：`InitializeFeatureStatus` → 填字段 → 载 `scriptable_objects` 本地包 → 伪造 `AssetBundleData` 并调 3 个内部方法注册 → `UpdateCardCollection` → 选起始卡组 → **`SetTestData(null, Action(LoginManager.GMCJIADAJIG))`** | `probe.js:826-887` |
| 10 | 之后**所有场景流程都是原版自己在跑** —— HH:L 全仓 `grep -n "LoadScene\|SceneManager\|SceneChanger"` 在 `*.js/*.py` 里 **0 命中** | 现场 grep |
| 11 | 运行期任何 `AssetBundleManager.GetLoadedBundle(key)` 未命中 → 剥 `PCWin/` 前缀 → 在 `assets.json` 里找 → `AssetBundle.LoadFromFile` → 写回**原版的 link 字典** | `probe.js:194-210` |

> 关键对照：HH:L 的客户端**自带 24.5 MB 的 `StreamingAssets/localbundle`**（`E:/2hhl/The Horus Heresy Legions原版/HorusHeresyLegions_Data/StreamingAssets/localbundle`，24,517,352 B）；而 **Warpforge 两个安装（我们的 `E:/warpforge/game` 和原版 `D:/2/unity_run_ref`）都【没有】这个文件**。Warpforge 的 `AssetBundleManager.LoadLocalAssetBundle()` 里那句 `Path.Combine(Application.streamingAssetsPath, "localbundle")`（`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/AssetBundleManager.cs:834-854`）因此没有东西可载 —— 这是「同一套祖宗代码、后作把资源层换成了 Addressables」的直接物证。

---

## 2. 他们自己造的东西清单

| # | 造了什么 | 落在哪个文件:行号 | 作用 | 我们能照搬吗 |
|---|---|---|---|---|
| 1 | `GetLoadedBundle` attach：未命中就本地 `LoadFromFile`，再写回原版字典 | `probe.js:194-210` | 一个出口兜住**全部**远端 bundle | ❌ **不能**：Warpforge 这个方法 **0 调用方**（两套反编译 + 现场探针都证实）。思路可上移到 `Addressables.InternalIdTransformFunc` |
| 2 | bundle 名归一：`name.replace(/\d+$/,'')` 再按 id 找 | `probe.js:171-176` | 原版会按 `<名><版本号>` 要包，索引里只有 `<名>` | ✅ **能**，而且我们有同样问题（catalog 里既有 `<名>.bundle` 又有 `<名>_<32位hash>.bundle`） |
| 3 | 载入后 `gchandle_new` 钉住 bundle + `op_Implicit` 判「原生对象还活着」 | `probe.js:167-170,182-193` | 只判托管 `!= null` 会把**已卸载**的原生对象当成可用 | ✅ **能**（我们踩过同族坑：Unity 对象 wrapper 在、原生没了） |
| 4 | 「**永不 Unload 我们亲自载的 bundle / 正在播的 clip**」 | `probe.js:225-231` 注释原文 | 场景/活对象自己持有资源所有权，提前卸会炸 | ✅ **能**，而且我们若手工载场景包**必须**照做 |
| 5 | 未命中/失败都**发事件出声**（`asset_unavailable` / `asset_load_failed`） | `probe.js:202,208` | 抗「原版只 LogError、界面白着」 | ✅ **能**（对应我们铁律：不许静默失败） |
| 6 | 启动时**预载「定义包」**并把它注册进 `assetBundleLinks`（伪造 `AssetBundleData{version,id}` 后调 3 个内部方法） | `probe.js:863-877` | 让后续 `GetLoadedBundle("scriptable_objects")` 直接命中 | ⚠️ **半能**：思路能（预载 + 登记），但那 3 个混淆方法（`PAJKAPBJHHD` / `LNEIIFHDCAI` / `NAGAKINKCBE`）是 HHL 二进制的名字，Warpforge 侧对应物不同且走 Addressables |
| 7 | `loginClass.GMCJIADAJIG` 包成 `System.Action` 交给原版 `SetTestData` | `probe.js:834-840,885-886` | **不伪造登录链**，直接走原版自带「测试数据」路径把登录推完 | ❌ **不能**：Warpforge 的 `PlayerDataManager.SetTestData` 是**空方法**（`dump.cs:86683` RVA `0x4B33B0` = 全局 `ret 0` 空桩）。我们的对应物是 `TryCompleteLogin()`（已在用） |
| 8 | `PlayerDataManager.get_IsLoggedIn` 恒 `true`；`NetworkCustomManager.StartConnection` 空桩 | `probe.js:263-268` | 让原版上层以为已登录/已连 | ✅ **能**（我们已在登录用同类打法；但要留意「恒真」会不会让别的分支提前跑起来） |
| 9 | `ws2_32` 的 `connect/WSAConnect/sendto/WSASendTo` 全部返回 −1 | `probe.js:2662-2668` | 物理掐断一切出站；**他们不伪造 PlayFab** | ⚠️ **不建议**：我们要回环假 PlayFab，全掐会把我们自己的服务端也断掉（B151 §4.3 已记） |
| 10 | `persistentDataPath` / `temporaryCachePath` 替换 + Win32 `SHGetFolderPathW` 重定向 + 改 `app.info` 公司名 | `probe.js:74-79,2669-2684`；`probe.py:44-45` | 三层把存档隔离到 `OfflineLab/profile` | ⚠️ **部分能**：我们目前直接吃 `LocalLow/Everguild/Warpforge`，不改更省事 |

---

## 3. 对六个问题的逐条回答

### 3.1 HH:L 怎么解决「离线加载场景/bundle」？——**第三种：他们的游戏压根没有 Addressables**

- 证据（本机二进制级）：`E:/2hhl/The Horus Heresy Legions原版/HorusHeresyLegions_Data/il2cpp_data/Metadata/global-metadata.dat` 字节计数 —— `Addressables` **0**、`UnityEngine.ResourceManagement` **0**、`Unity.ResourceManager` **0**；`AssetBundleManager` **2**、`AssetBundlesData` **5**。客户端 `StreamingAssets/` 里**没有 `aa/`、没有 catalog**，只有 `localbundle`（24.5 MB）与 `localbundle.manifest`。
- 所以他们的链是：**原版 `AssetBundleManager` 照常工作，只在「取不到包」的那一个出口把磁盘文件塞回它的字典**（`probe.js:194-210`）。**没有**「自己 `SceneManager.LoadSceneAsync`」这一路（全仓 0 处场景加载代码）。

### 3.2 有没有改/替换游戏的 `AssetBundleManager`？——**没有替换，只有 1 个 attach + 3 个调用 + 1 个字段写**

- **替换的方法：0 个。**
- **attach 改返回值：只有 `GetLoadedBundle`**（`probe.js:194`）。
- **调用（不是替换）**：`PAJKAPBJHHD(AssetBundleData)` 造 key、`LNEIIFHDCAI(info)`、`NAGAKINKCBE()` 收尾（`probe.js:871,875,876`）。
- **写字段**：把 `PlayerDataManager` 回填进 bundle manager（`probe.js:867`）；把 bundle 写进它的 links 字典（`probe.js:204-206`，字典偏移 `0x24`）。
- **没碰** `LoadLocalAssetBundle` / `CheckIfThereAreNewBundles` / `assetBundleCollection`（全仓 grep 0 命中）。
- ⚠️ 旁证：**Warpforge 同一个类里 `public Dictionary<string, AssetBundle> assetBundleLinks;`**（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/AssetBundleManager.cs:100`）——两者是同一套祖宗代码；但 Warpforge 的这个类现在只被 `AiScripted` 拿去取战役音效（`out_cpp2il_dev_ilspy/AiScripted.cs:1926,3566` 的 `bundlesManager`），**场景/卡牌资源完全不走它**。

### 3.3 怎么处理 PlayFab TitleData 的 bundle 清单？——**完全不处理**

- 全仓 `grep -rni "titledata\|playfab\|AssetBundlesData"`：只有 2 条命中，且都是无关的（`probe.js:122` 是 `fillDefaults` 跳过 `PlayFab` 命名空间的过滤；`probe.js:2262` 是回放 JSON 用了 `PlayFab.Json.JsonWrapper`）。
- **不伪造清单、也不让某个检查恒 false —— 而是把整套联网砍掉**（`ws2_32` 打 −1，`probe.js:2662-2668`），于是原版永远不会去问 TitleData；需要的数据包由他们**本地按名字直接给**。
- 唯一「伪造清单形状」的一处：启动时手搓一条 `AssetBundleData{version,id}` 塞给 `PAJKAPBJHHD`（`probe.js:866-874`）——**只为了定义包能命中，不是为了走下载流程**。

### 3.4 怎么跳过/伪造登录后那条链？——**整段不跑 + 调原版内部「完成」方法**

```
LoginManager.Start → 空桩（probe.js:150-152）
PlayerDataManager.Start(onLeave) → Invoke("NCGIEJIIBAK", 0.25)（probe.js:1000-1010）
LoginManager.NCGIEJIIBAK → offlineBoot（probe.js:997-999）
offlineBoot 末尾 → SetTestData(null, Action(LoginManager.GMCJIADAJIG))（probe.js:834-840,885-886）
```
- **这是「直接强推状态」的先例**：不碰 PlayFab 的任何一响应，登录状态靠**调原版自己的完成回调**推进。
- **但我们不能照抄**：Warpforge 的 `SetTestData` 已编译成空桩（`dump.cs:86683`）。我们的等价物 = `LoginManager.TryCompleteLogin()`（**已经在用**，阶段 2 报告：它能触发 `GameBootController.StartGame`）。
- ⇒ **结论：登录这一段我们的做法（伪造 PlayFab + 调 `TryCompleteLogin`）不比他们差，不用改**。剩下的卡点不在登录。

### 3.5 资源路径怎么定的？——**只重定向「用户数据」路径，不动资源路径**

- 改的是：`Application.get_persistentDataPath` / `get_temporaryCachePath`（`probe.js:74-79`）+ Win32 `SHGetFolderPathW` / `SHGetKnownFolderPath`（`probe.js:2669-2684`）+ `app.info` 公司/产品名（`probe.py:44-45` 注释：*The clone has its own company/product identifier*）。
- **没有**改 `Application.streamingAssetsPath`，**没有**改 `Addressables.RuntimePath`（他们根本没这变量）。
- 资源路径的唯一来源 = `assets.json` 的 `path`，由 `probe.py:63` 在**启动脚本里**拼成绝对路径后传进 JS。
- ⇒ 我们能直接借的只有一条：**「包路径由我们外部配置提供、不写死在游戏代码里」**。

### 3.6 有没有和我们症状对应的已知坑？——**HH:L 那边没有（他们没有 Addressables）；但我们自己这边本轮把真因挖到底了**

- **HH:L 的坑**（同族症状：包找不到时界面白着）：
  - 「本地找不到 bundle 时原版**只 LogError 不抛**」⇒ 他们挂 `GetLoadedBundle` 兜（`probe.js:194-210`），并在未命中时发 `asset_unavailable`。
  - 「托管 wrapper 还在、**原生对象已被卸载**」⇒ 一律用 `op_Implicit` 判活（`probe.js:169`、`:246-250` 注释）。
  - 「**绝不** Unload 我们自己载的包」（`probe.js:225-231`）。
- **我们的真因（本轮现读 `E:/warpforge/07_产物/phase0/probe.log:3375`）**：
  ```
  ChainOperation failed because dependent operation failed
   >>[0] GroupOperation failed because one of its dependencies failed
   >>[1] RemoteProviderException : Unable to load asset bundle from :
       https://d03469d2-…client-api.unity3dusercontent.com/…/entry_by_path/content/?path=/allcards_assets_all.bundle
  ```
  **最内层是「远端 CCD 下载失败」，不是「依赖图坏」**。
- **远端组到底有几个包**（现读 `E:/warpforge/02_解包资源/关服前存档/Warpforge/com.unity.addressables/catalog_main.json` 的 `m_InternalIds`，索引 **10486–10489**）：
  `allcards_assets_all.bundle` · `alternateartstyles_assets_all.bundle` · `localization_assets_all.bundle` · `remotemisccontentdata_assets_all.bundle`
  → 其中 **前三个**在 1.35.0 客户端的 `catalog.bin` 里各出现 1 次（第 4 个不在，属另一版 catalog）。
- ⚠️ **`B152_离线版_阶段2_进主菜单_进展.md` §二 那句「catalog 里 0 个 http」是错的**（建议调度台就地订正）：`catalog.bin` 里 `https` / `client-api` / `entry_by_path` / `content/?path=` **各正好 1 次** —— 因为 Addressables 的 catalog 对 internal id 做了**前缀压缩**（JSON catalog 里有 `m_InternalIdPrefixes` 字段；二进制这份显然启用了 —— 否则 4 条长 URL 不会只留 1 份前缀），4 条远端 id 共用同一段前缀、只各留一个后缀（`/allcards_assets_all.bundle` 等）。**按「grep http 的命中数」判「有没有远端」必然误判**。

---

## 4. 针对我们当前卡点的具体建议（可照做）

> 顺序 = 先证、再补、再绕。**每一步都给验收判据**。

### R1（最便宜，先做）把「依赖链」和「远端 id」变成日志

在现有 `ModMain` 的 Addressables 探针里加一段（`WF_OFFLINE_OPTEST` 那条路旁边）：

1. `Addressables.LoadResourceLocationsAsync("MainMenu Warpforge")` 拿到 location；
2. **递归**打印 `loc.InternalId` / `loc.ProviderId` / `loc.Dependencies`（深度≤3）；
3. 全文再扫一遍 `internalId.Contains("unity3dusercontent")` 的 location。

**验收**：日志里能看见 `allcards_assets_all.bundle` 是场景 location 的（直接或间接）依赖，且另有 `alternateartstyles` / `localization` 两条远端 id。
**为什么先做**：确认「只缺 3 个包」而不是更多；这是后面两步的前提。

### R2（正式修法）用官方的 `InternalIdTransformFunc` 把远端 id 顶到本地文件

本 build **确实有这个公开静态属性**：`UnityEngine.AddressableAssets.Addressables.InternalIdTransformFunc`（`d:/2/Warpforge_code/Scripts/Unity.Addressables/UnityEngine/AddressableAssets/Addressables.cs:50`；`ResourceManager` 侧还有一份：`Unity.ResourceManager/UnityEngine/ResourceManagement/ResourceManager.cs:151`）。

做法：**在 MelonLoader 初始化时就设**（早于任何 Addressables 操作）：
```csharp
Addressables.InternalIdTransformFunc = loc => {
    var id = loc.InternalId; int i = id.IndexOf("?path=/", StringComparison.Ordinal);
    if (i < 0) return id;
    var p = Path.Combine(bundleDir, id.Substring(i + 7));   // allcards_assets_all.bundle …
    return File.Exists(p) ? p : id;                          // 找不到就保持原 URL，并 LogError
};
```
- **分支覆盖**：`RemoteProviderException` 那条路走的就是 `AssetBundleProvider`，而它拿到**本地绝对路径**时会走 `AssetBundle.LoadFromFile`（不用下载）。
- **验收**：同一条 `Addressables.LoadSceneAsync("MainMenu Warpforge")` 从 `Failed` 变 `Succeeded`；`heartbeat.tsv` 的 `scene=` 列从 `Simple Transition` 变 `MainMenu Warpforge`。
- **注意**：失败分支**必须出声**（`MelonLogger.Error` + 文件清单），⛔ 不许 `return id` 之后静默让原版去联网。

### R3（补包）三个远端包本地有没有？——**有 3 份安卓副本，但跨平台可用性未验**

现读 `D:/2/关服前存档/安卓版本warpforge，包含远端资料/Everguild_Warpforge_Unity_Cache/Everguild_Warpforge/`：

| 缓存条目（`<x>/<y>/__data`） | 大小 | 对应包 | 判据 |
|---|---|---|---|
| `54bdb7b1…/ea8bca4f5c6909d3124fd4975dc96173` | 531,962 B | `allcards_assets_all` | 哈希在 `catalog.bin` 里出现 2 次 |
| `7416b641…/8657f84dd6401b7f761195881a7bb0cd` | 69,582 B | `alternateartstyles_assets_all` | 同上（2 次） |
| `c9187397…/dca1724a3a849f02cbbf815c0a94c405` | 10,764,942 B | **未能对上名字**（1.35.0 `catalog.bin` 里 0 次命中；疑似另一版本的 localization） | 见 §5 |

- ⚠️ **这三份都来自安卓 XAPK 的 Unity 缓存，`UnityFS` 头写的是 `6000.2.6f2`，与 Windows 客户端同 Unity 版本但目标平台不同**。跨平台能不能加载**我无法静态判定**（没有运行任何东西）。
- ⚠️ 本轮 12:07 已有人把第 1 份**改名为 `allcards_assets_all.bundle` 放进** `E:/warpforge/game/.../aa/StandaloneWindows64/`（md5 = `178db0098ad88a4b3888cfbdd4a6e486`，与缓存 `__data` **逐字节相同**）。**那一份能不能用，必须先验**：
  ```csharp
  var ab = AssetBundle.LoadFromFile(path);
  ab.GetAllAssetNames();          // 抛错 ⇒ 平台不兼容，直接否掉
  ab.LoadAllAssets();             // 纹理/着色器可能在这一步或渲染时坏
  ```
- 若安卓包不兼容，后备顺序：① 找 Windows 副本（`D:/2/`、`D:/4/`、`E:/warpforge` 已全盘按名搜过：**Windows 副本 0 个**）；② 用 `d:/2/tools/assetripper_out/ExportedProject`（**Windows 工程**）自己打 Windows 包 —— ⚠️ 本机只有 Unity **6000.3.23f1**，与 build 的 **6000.2.6f2** 不同，跨小版本打出来的包很可能被拒载，属高风险路线。

### R4（拿不到包时的退路）把「依赖边」去掉，而不是把包补上

这正是 HH:L 思路的**等价上移**：他们拿不到的原版远端包就不让它成为硬依赖。

- 机制：`Addressables.LoadResourceLocationsAsync("MainMenu Warpforge")` 拿到的 location 是 `ResourceLocationBase`，其依赖是 `m_Dependencies` / `Dependencies`（`IList<IResourceLocation>`）。在场景加载**之前**把它清空（或只删掉 internal id 含 `unity3dusercontent` 那几条）。
- **验收**：`GroupOperation` 不再因依赖失败；场景能进。
- **风险（必须写在账里）**：场景里指向这些包内资产的 PPtr 会解析成 null ⇒ 可能「菜单能出、卡图/本地化是空」。**先量再决定**：R1 的依赖 dump 能告诉我们这些依赖是不是「整组强依赖」（是的话内容一定缺）。

### R5（最后的兜底）绕开 Addressables 直接载场景 —— 注意：比 HH:L 难得多

- 游戏**本来就有一条非 Addressables 路**：`EverguildSceneManager.WaitSceneLoadInBuild` 用的是 `SceneManager.LoadSceneAsync`（`d:/2/tools/decomp_full/EverguildSceneManager._WaitSceneLoadInBuild_d__31__MoveNext.c`），由 `IsAddressableScene()` 分流（`…__IsAddressableScene.c`）。
- 但 **Addressables 路失败时不会自动回落**：`WaitSceneLoadAddressables` 在 `Status != 1` 时**只 `CustomDebug.LogError` 就结束协程**（`…_WaitSceneLoadAddressables_d__30__MoveNext.c` 第 2 跳）。
- 要做就得改**协程状态机**，并且还要伪造 `AsyncOperationHandle<SceneInstance>`（这需要有 `AsyncOperationBase<SceneInstance>` 的子类，Il2CppInterop 下很难）。**性价比明显低于 R2/R4**，建议只作为「R2 和 R4 都失败」时的最后手段。

### R6（账）要同步纠正/记录的三条

1. `B152_离线版_阶段2_进主菜单_进展.md` §二「0 个 http」→ 改为「**3 条远端 CCD 位置，前缀压缩**」（判据见 §3.6）。
2. 入库一条坑：**判「catalog 里有没有远端」不能 grep `http` 命中数**（前缀压缩）。
3. 入库一条事实：**Warpforge 没有 `StreamingAssets/localbundle`**（原版安装同样没有）⇒ 同工作室那套 `LoadLocalAssetBundle` 在本作里是空转，别照抄。

---

## 5. 没查清的部分

| 项 | 状态 | 搜过/做过什么 |
|---|---|---|
| 安卓 bundle 能否在 Windows Player 加载 | **未验（不可能静态判定）** | 只看了 `UnityFS` 头（`6000.2.6f2`）与缓存来源（XAPK 的 Unity 缓存）；未运行任何东西 |
| `dca1724a…`（10.7 MB）到底是哪个包 | **不确定** | 在 1.35.0 `catalog.bin` 里搜该哈希 = **0 命中**；在 `catalog_main.json`（另一版本）里也 0 命中。只能按大小猜是 localization，**未证实** |
| `remotemisccontentdata_assets_all` 有没有本地副本 | **没找到** | 已 `find D:/2 D:/4 E:/warpforge E:/2hhl -iname "*remotemisccontentdata*"` = 0；是否随 1.35.0 一起下发给某个 profile 缓存，未查到 |
| 1.35.0 二进制 catalog 的**逐条依赖图** | **只到「名字级」** | `catalog.bin` 是 Addressables 1.21 的 `BinaryStorageBuffer` 格式，无 `m_InternalIds` 等 JSON 字段（`grep` 全 0）；我用**同版本的 JSON catalog**（`02_解包资源/关服前存档/…/catalog_main.json`，1.7 MB，**另一 build**）取到了 4 条远端 id，但**没有解出 1.35.0 这份的依赖边** |
| HHL 的 `PAJKAPBJHHD` / `LNEIIFHDCAI` / `NAGAKINKCBE` 语义 | **按调用上下文推断** | 本地没有 HH:L 的反编译（`Managed/` 目录 0 个文件），只能从 `probe.js:866-876` 的用法推断为「造 key / 登记 / 收尾」；**不能断言** |
| HHL 到底怎么从 `SetTestData` 走到主菜单（原版回调里做了什么） | **未展开** | `probe.js:834-886` 只给了调用面，原版 `GMCJIADAJIG` 的方法体读不到（无 HH:L 反编译） |
| Warpforge 的 `InternalIdTransformFunc` 在 `ProvideScene` 路上是否也生效 | **未验** | 只有 stub 定义（`Addressables.cs:50`、`ResourceManager.cs:151`），方法体读不到；需在 R2 里实测 |
| 我们有没有其它路径已经在用 `AssetBundleManager` | **已查，结论是「只剩战役音效」** | `out_cpp2il_dev_ilspy` 全树只有 `AiScripted.cs:1926,3566` 两处 `bundlesManager`；`GetLoadedBundle(` 全树 1 处（定义本身） |

---

## 附：本轮用到的关键读数（可复核）

- Warpforge `global-metadata.dat`（`D:/2/unity_run_ref/Warpforge_Data/il2cpp_data/Metadata/`）：`Addressables` 112 · `UnityEngine.ResourceManagement` 165 · `AssetBundleManager` 4 · `AssetBundlesData` 5 · 文件 20,777,944 B。
- HH:L `global-metadata.dat`（`E:/2hhl/The Horus Heresy Legions原版/HorusHeresyLegions_Data/il2cpp_data/Metadata/`）：`Addressables` **0** · `UnityEngine.ResourceManagement` **0** · `AssetBundleManager` 2 · `AssetBundlesData` 5 · 文件 11,924,732 B。
- 本地 bundle 目录：84 个（12:07 之后 85 个，多的那个就是被改名塞进去的安卓 `allcards`）。
- 缺的远端包（1.35.0 口径）：`allcards_assets_all` · `alternateartstyles_assets_all` · `localization_assets_all`。
