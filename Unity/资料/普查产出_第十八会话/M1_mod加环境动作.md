# M1 · mod 加 `env` 动作（含编译）

**结论**：新动作 `env:<SO 名>` 已加进 mod，**编译通过（0 错）**，产物 `d:/4/Unity/工具/mod_scenejumpshot/bin/Release/SceneJumpShot.dll`（125,952 B）。⛔ 没启动游戏、没跑 Unity —— 结论全部来自**读源码 / 签名桩 / 离线资源 + 编译**。

## 我加的代码

**唯一改动的文件**：`d:/4/Unity/工具/mod_scenejumpshot/SceneJumpShot.cs`（2977 → 3296 行）。与只读原件 `d:/2/Warpforge_tools/scenejumpshot/SceneJumpShot.cs`（改前逐字节一致，167,841 B / 2977 行）做 difflib：**`+319 行 / −0 行`** ⇒ **纯新增，既有代码一行未动**（`drive`/`panel*`/`menu_open`/空动作四支一字未改）。行尾保持 **CRLF**（2977/2977 → 3296/3296）。
⚠️ 「贴出完整新增块」与「报告 ≤150 行」冲突（块共 319 行）⇒ **正本以源码为准**，这里给精确坐标 + 承重段落原文。

### ① 分发分支 —— `SceneJumpShot.cs:2518-2526`（`menu_open` 之后、`string sceneName...` 之前）

```csharp
            // 2026-10-11 M1: `env:<SO 名>` —— 把战场停在指定的【环境条件 SO】上再拍一张真渲图。
            //   ⛔ 不是 `panel*` / `drive` / `menu_open` / 空动作 那一批，**独立新分支**，原有行为一字未改。
            //   判据 / 自证办法见 EnvMode() 头部注释。
            if (multi != null && multi.StartsWith("env:", StringComparison.Ordinal))
            {
                yield return EnvMode(multi.Substring(4).Trim(), outDir);
                yield break;
            }
```
原来 `env:X` 会落到下面 `float.TryParse` 那条路、每个 token 都 parse 失败 ⇒ **静默什么都不做**；这一支是纯新增能力。

### ② 新区域 —— `SceneJumpShot.cs:2552-2861`（`Boot()` 收尾 `}` 之后、`menu_open` 区域注释之前）

`2553-2575` 头部判据注释（签名桩出处 / 取 SO 三条路只有第三条通 / ⛔ 不许静默失败 / ✅ 自证办法）· `2577` `EnvSafeName(string)` · `2589` `ObjName(Object)` · `2594` `EnvSoFloat(object,string,float)`（**反射读私有 `blendTime`**）· `2610` `EnsureBundlesLoaded()`（cfg 第 1 行空时自补，只在本动作里调）· `2627` `ScanEnvSos(bool,List,List)` · `2658` `PickEnvSo(...,string want,out string how)` · `2682` `AsEnvSo(Object)` · **`2699-2861` `IEnumerator EnvMode(string soName, string outDir)` ← 主流程**

`EnvMode` 五段（每步 `LoggerInstance.Msg`；**任一失败 ⇒ 出声 + `yield break`，⛔ 不拍图**）：
```
① FindObjectsOfType<Il2Cpp.ScenarioEnvironmentConditionsManager>(true) → 兜底 bm.EnvironmentConditionsManager
   找不到 ⇒ ❌ 停手。打印「调用前 CurrentEnvironment」+「调用前 RenderSettings ambient/fog/fogColor/fogDensity」
② 取 SO：a) CurrentEnvironment 已是它 · b) Resources.FindObjectsOfTypeAll(type)
        · c) 逐 bundle LoadAllAssets(type) 按 Object.name 匹配（先只扫 *cardanims* 包，没中再全量 84 包）
        · d) Addressables 按名字（离线判据说该失败，只为留实证）
   没找到 ⇒ ❌ 停手 + **把看过的前 60 个候选名全列出来**（让人能核 cfg 写错没有）
   子串命中 >1 ⇒ 判**有歧义、拒绝猜**并列出候选
③ CaptureShot(..._BEFORE.png)                    ← 调用前先拍一张（同进程/同相机 ⇒ 后面的对照是干净的）
④ mgr.ApplyEnvironment(so, false)   ← **typed 调用**；抛异常 ⇒ 打印 + 停手（不拍 AFTER）
   打印「调用后 CurrentEnvironment」（应 == 目标名）
⑤ 等 blendTime+1s（中途 4 次采样 fogDensity/ambient，看得出在补间）
   打印「调用后 RenderSettings ...」→ CaptureShot(shot_env_<SO名安全串>.png)
```
**cfg `env:` 后给的名字** = `Object.name` = 离线表 `EnvironmentConditions.json` 的 `so` 字段。匹配三档：精确 → 忽略大小写 → 唯一子串。

## 类型名/方法名的出处（🔴 逐条给证据）

**你给的名字逐条现核过，全部为真、一字不用改。** 两层证据：

**第一层 = 签名桩**（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/`；搜过的词：`ApplyEnvironment` · `EnviromentalEffect` · `EnvironmentalEffect` · `EnvironmentConditionsController` · `ScenarioEnvironmentConditions` · `EnvironmentCondition`）：

| 我用的 | 出处（原文） |
|---|---|
| 管理器真名 | `ScenarioEnvironmentConditionsManager.cs:4` `public class ScenarioEnvironmentConditionsManager : MonoBehaviour` |
| `CurrentEnvironment` | 同上 `:11` `public ScenarioEnvironmentConditionSO CurrentEnvironment { get; private set; }` |
| **`ApplyEnvironment` 真签名** | 同上 `:17` `public void ApplyEnvironment(ScenarioEnvironmentConditionSO environmentConditions, bool instant)` —— **2 参 / public / void** |
| 兜底入口 · SO 真名 · `EnableEnvironment` | 同上 `:21` `public void ApplyDefaultEnvironment(bool instant)` · `ScenarioEnvironmentConditionSO.cs:6` `public class ScenarioEnvironmentConditionSO : ScriptableObject` · 同上 `:65` `public ScenarioBlendOptions EnableEnvironment(bool instant)` |
| `blendTime` | `ScenarioEnvironmentConditionSO.cs:24-26` `[SerializeField][Tooltip("Time it will take fade to the new environment")] private float blendTime;`（**private ⇒ 只能反射**） |
| 它在 `BattleManager` 上 | `BattleManager.cs:4378` 私有字段 + `:4677` `public ScenarioEnvironmentConditionsManager EnvironmentConditionsManager => null;` |

⚠️ **纠正一条**：简报里的 `EnvironmentConditionsController` —— **全库不存在这个名字**，真名是 `ScenarioEnvironmentConditionsManager`。

**第二层（更强）= 编译期类型检查**：`mgr.ApplyEnvironment(so, false)` 是 **typed 调用不是反射**（interop 程序集 `d:/2/unity_run_ref/MelonLoader/Il2CppAssemblies/Assembly-CSharp.dll`）⇒ 名字 / 参数个数 / 可见性任一处不对就编不过。产物 DLL 里确认有这 4 个名字的 TypeRef/MemberRef（`#Strings` 堆 ASCII 各 1 命中；`env:` / `blendTime` 是字面量、在 `#US` 堆，UTF-16 各 43 / 2 命中）。

**SO 住哪里**：`工具/gen_environment_conditions.py:52` 扫 `bundle_*cardanims*_assets_all/MonoBehaviour/*.json` 按**内容**认（有 `blendTime`+`scenarioObjects`+`ambientColor`）；运行时包名校验过（`aa/StandaloneWindows64/` 里 14 个 `*cardanims_assets_all.bundle`，与 assets_full 目录名逐一对应）。`Object.name` == 那些 `.json` 的文件名（`gen_environment_conditions.py:112` `basename(f)[:-5]`）。

## 我**没**查到 / 只能猜的部分（⛔ 如实列）

1. 🔴 **「三条路只有第三条通」的前两条是实测、不是猜**：`LoadAsset(名字)` ✗ —— 本 build `GetAllAssetNames()` 返回**容器键(GUID)**（mod 旧注释 + `menu_open` 实测）；Addressables 按名字 ✗ —— **我现核**：`aa/catalog.bin`（1,305,148 B）搜 `EnvironmentalCondition` = **0 命中**（`EnviromentalEffectCardsSO` = 1）⇒ 环境 SO **不是 addressable 条目**；✅ `LoadAllAssets(type)` —— 非容器资产也拿得到（mod 原有 `FindSoInBundles()` 就是这个写法）。
2. ⚠️ `Resources.FindObjectsOfTypeAll(Il2CppSystem.Type)` 的**运行时返回范围我没能离线验证**（只知道 CoreModule 里有该符号且**编译通过**）。它是 ②-a 的加分路，拿不到不影响（②-c 是主力）。
3. ⚠️ **没跑过游戏** ⇒ 以下全是**静态判据、未实况验证**：① arena 场景里真能 `FindObjectsOfType` 到它（唯一旁证 = 只读代理查到的 `runtime_ui_dump_Battle_Arena_1.tsv:91`，active）；② `LoadAllAssets` 运行时在 14 个 cardanims 包里真能捞到；③ `ApplyEnvironment` 是否真把 `CurrentEnvironment` 置成目标（不置的话日志会打出来，我没静默）。
4. ⚠️ `blendTime` 反射失败时**兜底 3s**（日志会打 `blendTime=3.00`）—— **这是我的选择、不是原版值**（离线表 55 条 SO 的 `blendTime` 是 2.0/3.0/4.0/5.0）。
5. ⚠️ `instant` 传 **`false`**（照原版打进攻卡那条路）⇒ 雾是补间的、必须等；但「等 `blendTime+1s` 一定够」**没实况验证过**。

## 编译

```bash
cd "d:/4/Unity/工具/mod_scenejumpshot" && unset ELECTRON_RUN_AS_NODE && dotnet build SceneJumpShot.csproj -c Release
```
结果 `已成功生成。 3 个警告 0 个错误`；3 个警告是**既有的** `CS0169`（`_blitRt/_blitArmed/_blitPath` 未用字段），与本次改动无关。第一次报过 1 个错（`CS0120`：`AsEnvSo` 我写成 `static` 却用了 `LoggerInstance`），改成实例方法后即过。
**DLL 完整路径**：`d:/4/Unity/工具/mod_scenejumpshot/bin/Release/SceneJumpShot.dll`（125,952 B · 2026-10-11 17:31；同目录另有 `.pdb` / `.deps.json`）
**目标部署目录**（照现成的库报，不是猜的）：`d:/2/unity_run_ref/Mods/SceneJumpShot.dll`（现 115,712 B · 2026-10-06 19:56）。⛔ **我不写 `d:/2/`** ⇒ **请主对话把产物拷过去覆盖**（`Mods/` 下另有 4 个 `SceneJumpShot_*.bak` 旧备份）。

## cfg 怎么写才能触发

cfg = `d:/2/unity_run_ref/UserData/sjs_dump_cfg.txt`（mod 常量 `CfgPath` @ `SceneJumpShot.cs:17`）。**4 行**：① bundle 文件名（**带 `.bundle`**；mod 用 `Path.GetFileName(f) == 第1行` 匹配）② 等几秒 ③ 输出目录 ④ 动作。完整四行示例（Ultramarines 雷暴 = `battlearena1`）：
```
scenes_scenes_battlearena1.bundle
12
d:/4/_tmp_view/envmod
env:EnvironmentalCondition Ultramarines Thunderstorm
```
换环境只改第 4 行（55 条清单见 `MyGame/Assets/Resources/EnvironmentConditions.json` 的 `so`）。`env:` 后留空 ⇒ 出声拒绝、不拍图。⚠️ 第 1 行别留空（留空时 Boot 不加载 bundle；我加了 `EnsureBundlesLoaded()` 自补，但那要重扫 84 个包，慢）。产物两张：`shot_env_<SO名安全串>_BEFORE.png`（调用前）· `shot_env_<SO名安全串>.png`（等够 blendTime 后）。

## 怎么自证它真的生效了（**不是**拍了一张默认态）

三条**互相独立**的判据，缺一条不算：

1. **日志** `grep '\[SJS\] env:'` 必须出现 `✅ 目标 SO = '<名>' blendTime=… ambientColor=(…) fogColor=(…) fogDensity=… (取法: 精确)` · `ApplyEnvironment(so, instant:false) 已调` · `调用后 CurrentEnvironment='<名>' （应 == '<名>'）` ← **不等就是没生效** · `调用后 RenderSettings ambient=(…)/fogDensity=…` 与上面「**调用前** RenderSettings」**不相等**。
2. **两张图逐像素差**（同进程 / 同相机 ⇒ 干净对照）。**参照带**（我现算的，判据 = `工具/sweep_env_states.py`，数据 = `_tmp_view/env52/` 里已有 55 态图）：battlearena1 四态 **97.90 / 99.40 / 99.72 / 99.73 %**；battlearenaastramilitarum 三态 **99.16 / 98.74 / 98.84 %**；battlearenadarkangels **99.43 / 99.65 / 100.00 %**。⇒ 我们自己工程里 42 个效果态对默认态全在 **97.9%–100%**；原版只会更明显（还多 `Instantiate` 的 prefab）。**实测 < 0.5%（脚本的「动没动」阈值）= 没生效**；0.5%–90% 算**可疑**（可能只补了环境光、没装 prefab）。
3. **别看整图 `mean`** —— `sweep_env_states.py:91` 已踩过：`EC 1 Green` **64.97% 像素变了而 mean 只挪 +0.04**。**判据一律用逐像素差占比**。

## 顺手发现

1. 🔴 **`AssetBundle.LoadAllAssets(Il2CppSystem.Type)` 能捞到「非 addressable」的资产** —— 本仓已知铁律是「bundle 里非 addressable 的 prefab **三条路全不通、要重打包**」⇒ **这是第四条路、且不用重打包**（环境 SO 正是这种）。代价：把该包**所有**资产载进来 ⇒ 只适合小包或兜底。**建议记进 `资料/已知的坑.md`。**
2. 🔴 `_tmp_view/env52/battlearenadarkangels__Dark_Angels_Orbiting.png` 相对本场默认态 = **100.00%**（同场另两条只有 99.43 / 99.65）；100.00% = 每个像素都差 >2 ⇒ 值得 arena 线的会话看一眼是不是整屏全换、或拍到了空/黑帧。**不是我的线，我没动它。**
3. ⚠️ 我**没有改动任何 git 跟踪的文件**（`git status` 里那些 `M` 全是别的代理 / 别的线的活）。
