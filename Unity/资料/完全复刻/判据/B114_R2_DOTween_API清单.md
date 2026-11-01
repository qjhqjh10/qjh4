# B114 · R2 —— 我们的 DOTween.dll 与原版是不是同一版（API 清单 + 可否替换）

> 只读查证代理 `R-2` · 2026-11-01 · 工作目录 `d:/4`
> **本文件只写证据与结论，⛔ 不写「已完成/未完成」**（状态见 `活状态表.md`）。
> **全程未执行/加载任何 DLL**：所有读取都是「PE + CLI metadata 静态解析」或 `ilspycmd` 的**元数据反编译**（只读文本，不运行）。

---

## 一、两张 DLL 的事实表

### 1.1 **我们的**

| 项 | 值 |
|---|---|
| 路径 | `d:/4/Unity/MyGame/Assets/Plugins/Demigiant/DOTween/DOTween.dll` |
| 大小 | **178,176 B** |
| MD5 | `ab4484dbe1b3f3356bfb08445ded3688` |
| AssemblyName / AssemblyVersion | `DOTween` / `1.0.0.0` |
| **`DOTween.Version` 字面量** | **`"1.3.030"`**（`d_ours/DG.Tweening/DOTween.cs:13`，`public static readonly string Version = "1.3.030";`） |
| TypeDef / MethodDef / Field / TypeRef | **202 / 1,195 / 689 / 105** |
| 方法体 | **真实**（可反编译出完整逻辑） |
| 同目录其它件 | `DOTween.XML` 231,628 B · `Editor/DOTweenEditor.dll` **68,608 B** · `Editor/DOTweenUpgradeManager.dll` 11,264 B · `Modules/*.cs` **9 个 `.cs` 源码**（不是 DLL） |

### 1.2 **原版的** —— 三份同尺寸副本，**API 面完全一致**

| 项 | 值 |
|---|---|
| 路径 ①（首选） | `d:/2/tools/assetripper_out/AuxiliaryFiles/GameAssemblies/DOTween.dll` — MD5 `5fcf5fd557cc92e4d5e48862e1e1e3ff` |
| 路径 ② | `d:/2/tools/assetripper_out/ExportedProject/Assets/Plugins/DOTween.dll` — MD5 `5fcf5fd5…`（与 ① 逐字节相同） |
| 路径 ③ | `d:/2/Warpforge_code/Assemblies/DOTween.dll` — MD5 `c8270988467016f7613c3b7b112b2818`（**逐字节不同，但 TypeDef/MethodDef/Field 计数与 API 面与 ① 完全一致**，只差在元数据排布） |
| 大小 | **83,968 B**（三份都一样） |
| AssemblyName / AssemblyVersion | `DOTween` / `1.0.0.0` |
| `DOTween.Version` 字面量 | **查不到** —— 字段在（`public static readonly string Version;`），但**初始化器丢了**（见 1.3） |
| TypeDef / MethodDef / Field / TypeRef | **201 / 1,180 / 687 / 62** |
| 方法体 | 🔴 **是 stub：所有方法体是 `return null;` / `{ }`** —— 见 1.3 |

### 1.3 🔴 最重要的一条事实：**83,968 B 那份是「参考程序集（stub）」，不是能跑的 DOTween**

实证（`ilspycmd` 静态反编译，`d_orig/DG.Tweening/DOTween.cs:150-220`）：

```csharp
public static TweenerCore<double, double, NoOptions> To(DOGetter<double> getter, DOSetter<double> setter, double endValue, float duration)
{
    return null;   // ← 原版文件里就是这个
}
```

⇒ 它**只保留了「原版对外 API 的表面」**（类型、方法名、参数、默认值），**没有任何实现**。
⇒ 拿它当运行时 DLL 换上去 = **所有补间直接返回 null、全部动画静默失效**（不会报错，只会「什么都没动」）。
⇒ **它能证明的只有一件事：原版的 API 面长什么样。** 不能当替换品。

### 1.4 原版 DLL 的**其它候选位置**（都查过了，都不是可用的真身）

| 位置 | 大小 | 状态 |
|---|---|---|
| `d:/2/unity_run_ref/WarpforgePlayer_Data/Managed/` | — | **不存在**。运行目录真名是 `d:/2/unity_run_ref/Warpforge_Data/`，且**没有 `Managed/` 文件夹**（IL2CPP 构建，托管代码全在 `GameAssembly.dll`） |
| `d:/2/新解包资源/**/Managed` | — | **不存在**（全盘 `find -name Managed` 0 命中） |
| 安卓 XAPK `Warhammer+40,000_+Warpforge_1.35.0_APKPure.xapk`（813 MB） | — | 主 APK `com.Everguild.WarhammerWarpforge.apk`（30.4 MB）里 `assets/bin/Data/Managed/` 只有 **`Metadata/global-metadata.dat` + `Resources/*.dll-resources.dat`**，**没有任何 `DOTween.dll`**（Android IL2CPP 不发布托管程序集） |
| `d:/2/tools/cpp2il/out_cpp2il/DOTween.dll` | 75,264 | 重建件（cpp2il，更残缺） |
| `d:/2/tools/cpp2il/out_cpp2il_dev/DOTween.dll` | 596,480 | 重建件 |
| `d:/2/tools/il2cpp_out/DummyDll/DOTween.dll` | 222,720 | Il2CppDumper DummyDll（**缺默认参数值、缺一批成员**，比 83,968 那份还残缺） |
| `d:/2/unity_run_ref/MelonLoader/…/Cpp2IL/cpp2il_out/DOTween.dll` | 223,744 | 同上，Cpp2IL 版 |
| `d:/4/Unity/MyGame/Library/Bee/artifacts/…/ManagedStripped/DOTween.dll` | 178,176 | **就是我们自己那份的构建产物** |

**全盘 `find d:/2 d:/4 -iname "DOTween*.dll"` 共 11 个，就只有这 11 个**（已逐个列过）。
⇒ **本机没有「原版真正的 DOTween 运行时 DLL」**。原版实现只存在于 `GameAssembly.dll`（IL2CPP AOT）。

### 1.5 怎么读的（可复现）

- **PE / CLI 元数据**：自写 Python 静态解析器（读 `#~` 表流、`#Strings`、`#Blob`、`#US`；**不运行、不加壳、不用反射加载**）。
- **签名/类型/字段**：`ilspycmd`（`d:/2/tools/ilspycmd/ilspycmd.exe`）`-p -o <dir>` 静态反编译两份，得到 `d_ours/`、`d_orig/`（各 100 个 `.cs`）。
- **交叉验证**：原版游戏自己的 `Assembly-CSharp-firstpass`（`d:/2/tools/cpp2il/out_il2csharp/`）与 `decomp_full/` 里 DOTween 模块的用法，逐条对回 83,968 B 那份的 API 面。

---

## 二、API 清单表

> 「原版有吗」这一列的**权威来源 = 两边的 MethodDef/Field 表逐键比对**（穷举，不是抽样），
> 不是靠 grep。见 §三 · 3.1 的方法说明。
> `文件:行` 一律相对 `d:/4/Unity/MyGame/Assets/CardPresentation/`（已去掉公共前缀）。
> **⛔ 不含** `Assets/Plugins/Demigiant/`（那是 DOTween 包自己的源码，不是「我们代码」）。

### 2.1 扩展方法（`DO*`）—— 这些**不在 DOTween.dll 里**，由 `Modules/*.cs` 源码编进 Assembly-CSharp

| API 名 | 我们代码里用在哪（全部） | 原版有吗 | 备注 |
|---|---|---|---|
| `DOFade` | 1 · `Battle/BattleDriver.cs:10253` | ✅ | 原版 DLL 里也没有（同属 Modules 源码），**原版 Assembly-CSharp 里有同名实现**（`DG.Tweening.DOTweenModuleUI__DOFade.c`） |
| `DOKill` | 13 · `Battle/AttackSelector.cs:1757` · `Battle/BattleDriver.cs:27812` · `Battle/MulliganPanel.cs:737` · `Battle/MulliganPanel.cs:1051` · `Core/CardView.cs:13639` · `Core/CardView.cs:13670` · `Core/CardView.cs:14706` · `Core/CardView.cs:15344` · `Editor/CardBaseDemo.cs:5602` · `Editor/CardBaseDemo.cs:5612` · `Editor/CardBaseDemo.cs:5616` · `Hand/HandLayout.cs:847` · `Shell/CardDetailPopup.cs:1044` | ✅ | 同上（Modules 源码） |
| `DOLocalMove` | 8 · `Battle/BattleDriver.cs:26336` · `Board/DeploySequence.cs:212` · `Board/DeploySequence.cs:293` · `Core/CardFeel.cs:1147` · `Core/CardFeel.cs:2286` · `Core/CardFeel.cs:2292` · `Core/CardFeel.cs:3309` · `Core/UnitTweenTable.cs:446` | ✅ | 同上 |
| `DOLocalMoveY` | 1 · `Shell/CollectionWindow.cs:1471` | ✅ | 同上 |
| `DOLocalRotate` | 2 · `Battle/BattleDriver.cs:26337` · `Core/UnitTweenTable.cs:476` | ✅ | 同上 |
| `DOLocalRotateQuaternion` | 1 · `Core/CardFeel.cs:4046` | ✅ | 同上 |
| `DOMove` | 21 · `Battle/BattleDriver.cs:10235` · `Battle/BattleDriver.cs:24040` · `Battle/BattleDriver.cs:26122` · `Board/DeploySequence.cs:204` · `Board/DeploySequence.cs:218` · `Board/DeploySequence.cs:282` · `Board/DeploySequence.cs:301` · `Core/CardFeel.cs:1144` · `Core/CardFeel.cs:1862` · `Core/CardFeel.cs:1864` · `Core/CardFeel.cs:1909` · `Core/CardFeel.cs:3158` · `Core/CardFeel.cs:3344` · `Core/CardFeel.cs:3386` · `Core/CardFeel.cs:3421` · `Core/CardFeel.cs:4044` · `Core/UnitTweenTable.cs:434` · `Core/UnitTweenTable.cs:520` · `Editor/DOTweenSmokeTest.cs:32` · `Editor/DOTweenSmokeTest.cs:51` · `Shell/DraftModeWindow.cs:4051` | ✅ | 同上 |
| `DOMoveX` | 1 · `Battle/BattleDriver.cs:24030` | ✅ | 同上 |
| `DOMoveY` | 1 · `Battle/BattleDriver.cs:24044` | ✅ | 同上 |
| `DOMoveZ` | 1 · `Battle/BattleDriver.cs:24033` | ✅ | 同上 |
| `DOPunchPosition` | 3 · `Core/CardFeel.cs:1177` · `Core/CardFeel.cs:3872` · `Core/UnitTweenTable.cs:500` | ✅ | 同上 |
| `DOPunchRotation` | 1 · `Core/UnitTweenTable.cs:498` | ✅ | 同上 |
| `DOPunchScale` | 11 · `Battle/BattleDriver.cs:37736` · `Battle/TargetReticle.cs:670` · `Core/CardView.cs:13641` · `Core/CardView.cs:13672` · `Core/HuntMarkController.cs:104` · `Core/MarkerlightController.cs:153` · `Core/UnitTweenTable.cs:499` · `Editor/BattleScene.cs:38168` · `Shell/AvatarTab.cs:859` · `Shell/CollectionWindow.cs:5218` · `Shell/DraftModeWindow.cs:2600` | ✅ | 同上 |
| `DORestart` | 1 · `Battle/TargetReticle.cs:668` | ✅ | 同上 |
| `DORotate` | 11 · `Battle/BattleDriver.cs:26121` · `Battle/BattleDriver.cs:26342` · `Battle/BattleDriver.cs:26352` · `Board/DeploySequence.cs:221` · `Board/DeploySequence.cs:310` · `Core/CardFeel.cs:1863` · `Core/CardFeel.cs:1865` · `Core/CardFeel.cs:3278` · `Core/CardFeel.cs:3387` · `Core/CardFeel.cs:3430` · `Core/UnitTweenTable.cs:475` | ✅ | 同上 |
| `DORotateQuaternion` | 4 · `Core/CardFeel.cs:1145` · `Core/CardFeel.cs:1148` · `Core/CardFeel.cs:2287` · `Core/UnitTweenTable.cs:521` | ✅ | 同上 |
| `DOScale` | 29 · `Battle/AttackSelector.cs:1734` · `Battle/BattleDriver.cs:14165` · `Battle/BattleDriver.cs:26118` · `Battle/BattleDriver.cs:26356` · `Battle/BattleDriver.cs:35472` · `Battle/PopNumber.cs:335` · `Core/CardFeel.cs:2552` · `Core/CardFeel.cs:3159` · `Core/CardFeel.cs:3431` · `Core/CardFeel.cs:4045` · `Core/CardView.cs:4116` · `Core/CardView.cs:9115` · `Core/CardView.cs:9320` · `Core/CardView.cs:15346` · `Core/UnitTweenTable.cs:417` · `Core/UnitTweenTable.cs:522` · `Editor/CardBaseDemo.cs:5569` · `Editor/CardBaseDemo.cs:5898` · `Editor/CardBaseDemo.cs:5944` · `Editor/CardBaseDemo.cs:5952` · `Editor/HandAssertE8.cs:154` · `Shell/BoosterPackOpenWindow.cs:1106` · `Shell/DraftModeWindow.cs:2577` · `Shell/DraftModeWindow.cs:2697` · `Shell/DraftModeWindow.cs:2713` · `Shell/DraftModeWindow.cs:3943` · `Shell/DraftModeWindow.cs:3970` · `Shell/DraftModeWindow.cs:4061` · `Shell/RankedDivisionChangeWindow.cs:1199` | ✅ | 同上 |
| `DOShakePosition` | 4 · `Core/CardFeel.cs:2497` · `Core/CardFeel.cs:3832` · `Core/UnitTweenTable.cs:511` · `Shell/GachaEventWindow.cs:1593` | ✅ | 同上 |
| `DOShakeRotation` | 1 · `Core/UnitTweenTable.cs:509` | ✅ | 同上 |
| `DOShakeScale` | 1 · `Core/UnitTweenTable.cs:510` | ✅ | 同上 |
| `DOVector` | 1 · `Shell/AvatarTab.cs:849` | ✅ | 同上 |

### 2.2 `DOTween` / `DOVirtual` 静态入口

| API 名 | 我们代码里用在哪（全部） | 原版有吗 | 备注 |
|---|---|---|---|
| `DOTween.To` | **104 处**（全项目，`grep -ho "DOTween\.To"` 计数）· 例：`Battle/BattleDriver.cs:26117-26360` 那一段全是 | ✅ | 签名逐个类型一一对应（1.2/1.3 都没有增删重载） |
| `DOTween.ToAlpha` | **0** | ✅ | 未使用 |
| `DOTween.ToArray` | **0**（`.ToArray(` 的 521 处全是 LINQ） | ✅ | 未使用 |
| `DOTween.Sequence` | 33 处紧写 + 42 处 `DOTween.Sequence` 文本命中 · `Battle/BattleDriver.cs:14164` · `:24027` · `:26117` · `:26335` · `:26341` · `Battle/PopNumber.cs:330` · `Board/DeploySequence.cs:203` · `:268` · `:280` · `Core/CardFeel.cs:1143` · `:1496` · `:1860` · `:1904` · `:2085` · `:2284` · `:2488` · `:3157` · `:3269` · `:3343` · `:3385` · `:3420` · `:3603` · `:3731` · `:4043` · `:4326` · `Core/CardView.cs:9112` · `Core/UnitTweenTable.cs:190` · `:518` · `Shell/CollectionWindow.cs:1460` · `Shell/DraftModeWindow.cs:3706` · `:3937` · `:3964` · `:4047` | ✅ | `Sequence()` 与 `Sequence(object target)` 两个重载两边都在 |
| `DOVirtual.DelayedCall` | 13 处（文本）· 4 处 tween-typed 接收者：`Battle/BattleDriver.cs:34249` · `Core/CardFeel.cs:3928` · `Editor/BattleScene.cs:49576` · `Hand/CardInteraction.cs:460` | ✅ | 签名一致 |
| `DOTween.Kill`（静态） | 66 处（文本） | ✅ | `Kill(object,bool)` / `Kill(object,object,bool)` 两个重载两边都在 |
| `DOTween.KillAll` | 2 处 tween 站点 · `Editor/DOTweenSmokeTest.cs:58` · `Editor/TraitEffectAssert.cs:83`（文本 5 处） | ✅ | `KillAll(bool)` / `KillAll(bool, params object[])` 两边都在 |
| `DOTween.SetTweensCapacity` | 1 · `Battle/BattleDriver.cs:36678` | ✅ | 签名一致 |
| `DOTween.ManualUpdate` | 4 · `Battle/BattleCameraSreenSize.cs:418` · `Core/CardTween.cs:87` · `Editor/DOTweenSmokeTest.cs:39` · `:52`（文本 12 处） | ✅ | 签名一致 |
| `DOTween.TweensByTarget` | 2 · `Editor/CollectionScene.cs:7386` · `:7408` | ✅ | 签名一致 |
| `DOTween.IsTweening` | 3 · `Battle/BattleDriver.cs:16122` · `Battle/TargetReticle.cs:668` · `Core/CardView.cs:14728`（文本 6 处） | ✅ | 签名一致 |
| `DOTween.Punch` | 1 · `Shell/RewardWindow.cs:970`（文本 5 处） | ✅ | 签名一致 |
| `DOTween.Shake` | **0**（我们只用了扩展 `DOShake*`） | ✅ | 两边都在 |
| `DOTween.Init` | 3 处（文本） | ✅ | 签名一致 |
| `DOTween.defaultEaseType` | 17 处（读默认缓动） | ✅ | 静态字段，两边都在 |
| `DOTween.All`（其它） | — | ✅ | 见 §三 · 3.2 的「反方向缺口 = 0」 |

### 2.3 `Tween` / `Sequence` 成员与 `TweenSettingsExtensions` / `TweenExtensions`

| API 名 | 我们代码里用在哪（全部） | 原版有吗 | 备注 |
|---|---|---|---|
| `Append` | 33 · `Battle/BattleDriver.cs:14165` · `:24040` · `:26118` · `:26342` · `:26352` · `Battle/PopNumber.cs:331` · `:337` · `Board/DeploySequence.cs:204` · `:212` · `:218` · `:282` · `:293` · `:301` · `Core/CardFeel.cs:1144` · `:1147` · `:1497` · `:1862` · `:1864` · `:2286` · `:2292` · `:3158` · `:3270` · `:3344` · `:3386` · `:3421` · `:3732` · `:4044` · `Core/CardView.cs:9115` · `Shell/CollectionWindow.cs:1465` · `Shell/DraftModeWindow.cs:3708` · `:3941` · `:3966` · `:4056` | ✅ | 已排除 `StringBuilder.Append`（全仓 `.Append(` 文本命中 1,642 处，绝大多数是 `StringBuilder`） |
| `AppendCallback` | 5 · `Battle/BattleDriver.cs:26345` · `Core/CardFeel.cs:2500` · `:2512` · `Shell/DraftModeWindow.cs:3939` · `:4066` | ✅ | |
| `AppendInterval` | 22 · `Battle/BattleDriver.cs:26126` · `Battle/PopNumber.cs:336` · `Board/DeploySequence.cs:206` · `:211` · `:217` · `:224` · `:269` · `:284` · `:291` · `:298` · `:315` · `Core/CardFeel.cs:1146` · `:1905` · `:2086` · `:2291` · `:2494` · `:2510` · `:3604` · `Core/CardView.cs:9120` · `Shell/CollectionWindow.cs:1462` · `Shell/DraftModeWindow.cs:3938` · `:4050` | ✅ | |
| `Insert` | 17 · `Battle/BattleDriver.cs:26122` · `:26336` · `:26337` · `:26353` · `Battle/PopNumber.cs:335` · `:342` · `Board/DeploySequence.cs:221` · `:310` · `Core/CardFeel.cs:1909` · `:1914` · `:1918` · `:1929` · `:2130` · `:2497` · `:3611` · `:3615` · `:4327` | ✅ | |
| `InsertCallback` | 18 · `Battle/BattleDriver.cs:26365` · `:26373` · `Board/DeploySequence.cs:210` · `:289` · `Core/CardFeel.cs:1917` · `:1928` · `:2094` · `:2113` · `:2120` · `:2121` · `:2134` · `:2174` · `:2181` · `:3364` · `:3365` · `:3614` · `:3621` · `Core/UnitTweenTable.cs:233` | ✅ | |
| `Join` | 29 · `Battle/BattleDriver.cs:14166` · `:26121` · `Core/CardFeel.cs:1145` · `:1148` · `:1863` · `:1865` · `:2287` · `:3159` · `:3165` · `:3171` · `:3357` · `:3387` · `:3392` · `:3430` · `:3431` · `:3432` · `:4045` · `:4046` · `:4330` · `Core/UnitTweenTable.cs:520` · `:521` · `:522` · `Shell/CollectionWindow.cs:1470` · `Shell/DraftModeWindow.cs:3713` · `:3946` · `:3969` · `:4049` · `:4058` · `:4064` | ✅ | 已排除 `string.Join`（全仓 568 处） |
| `Prepend` / `PrependInterval` | **0**（`thresholds.Prepend(0)` 是 LINQ，**不是** DOTween） | ✅ | 两边都在，未使用 |
| `SetEase` | 63 · `Battle/BattleDriver.cs:14165` · `:24031` · `:24034` · `:24045` · `:26118` · `:26122` · `:26336` · `:26343` · `:26352` · `:26356` · `:39634` · `Battle/PopNumber.cs:331` · `:335` · `:338` · `:343` · `:346` · `Board/DeploySequence.cs:204` · `:212` · `:218` · `:221` · `:282` · `:293` · `:301` · `:310` · `Core/CardFeel.cs:1144` · `:1147` · `:1497` · `:1862` · `:1864` · `:1909` · `:1915` · `:1919` · `:1930` · `:2131` · `:2153` · `:2169` · `:2210` · `:2237` · `:3158` · `:3159` · `:3278` · `:3344` · `:3386` · `:3387` · `:3421` · `:3430` · `:3431` · `:4044` · `:4045` · `:4046` · `Core/CardTween.cs:57` · `Core/UnitTweenTable.cs:267` · `:277` · `:280` · `:520` · `:521` · `:522` · `Editor/DOTweenSmokeTest.cs:32` · `:51` · `Shell/BoosterPackOpenWindow.cs:1441` · `Shell/CollectionWindow.cs:5254` · `Shell/DraftModeWindow.cs:3942` · `Shell/GachaEventWindow.cs:1595` | ✅ | 5 个重载两边一一对应 |
| `SetDelay` | 22 · `Battle/BattleDriver.cs:24060` · `:34244` · `Core/CardFeel.cs:1152` · `:1499` · `:1866` · `:1933` · `:2308` · `:2518` · `:3173` · `:3272` · `:3316` · `:3368` · `:3393` · `:3433` · `:3834` · `:4332` · `Core/UnitTweenTable.cs:265` · `Hand/HandLayout.cs:907` · `Shell/DraftModeWindow.cs:3685` · `:3693` · `Shell/RankedDivisionChangeWindow.cs:1200` · `Shell/RewardWindow.cs:973` | ✅ | |
| `SetId` | 1 · `Core/CardView.cs:9998` | ✅ | |
| `SetLoops` | 2 · `Core/UnitTweenTable.cs:266` · `Shell/GachaEventWindow.cs:1594` | ✅ | |
| `SetRelative` | 3 · `Core/UnitTweenTable.cs:435` · `:449` · `:477` | ✅ | |
| `SetTarget` | 4 · `Core/CardView.cs:9993` · `:12588` · `Shell/BoosterPackOpenWindow.cs:1440` · `Shell/CollectionWindow.cs:5253`（后两处走 `TweenSettingsExtensions.SetTarget(...)` 静态形式） | ✅ | |
| `SetUpdate` | 19 · `Battle/BattleCameraSreenSize.cs:415` · `:416` · `Battle/BattleDriver.cs:25947` · `:26036` · `:37937` · `Core/CardFeel.cs:3278` · `:3612` · `:3616` · `:3733` · `:4044` · `:4045` · `:4046` · `Core/CardTween.cs:57` · `Core/CardView.cs:8848` · `Editor/DOTweenSmokeTest.cs:32` · `:51` · `Hand/CardInteraction.cs:1486` · `Shell/CollectionWindow.cs:5254` · `Shell/RewardWindow.cs:974` | ✅ | 3 个重载一一对应 |
| `SetLink` | 7 · `Battle/AttackSelector.cs:1737` · `Battle/BattleDriver.cs:6366` · `Battle/SkillPanel.cs:1637` · `:1738` · `Core/CardTween.cs:58` · `Hand/CardInteraction.cs:462` · `Shell/RewardWindow.cs:975` | ✅ | `SetLink(GameObject)` / `SetLink(GameObject, LinkBehaviour)` 两边都在 |
| `From` | 5 · `Core/UnitTweenTable.cs:418` · `:451` · `:478` · `Shell/DraftModeWindow.cs:2602` · `Shell/RankedDivisionChangeWindow.cs:1201` | ✅ | 7 个重载两边一一对应 |
| `OnComplete` | 39 · `Battle/BattleDriver.cs:14169` · `:24050` · `:26128` · `:26389` · `:27936` · `:27961` · `:39635` · `Battle/CardDisplayWindow.cs:1584` · `:1665` · `Battle/PopNumber.cs:361` · `Board/DeploySequence.cs:226` · `:271` · `:325` · `Core/CardFeel.cs:1941` · `:2194` · `:2301` · `:2519` · `:3178` · `:3625` · `:3677` · `:3744` · `:4048` · `:4335` · `Core/CardView.cs:8522` · `:9122` · `:12589` · `Core/UnitTweenRuntime.cs:100` · `Shell/AvatarTab.cs:865` · `Shell/CardDetailPopup.cs:1447` · `Shell/CollectionWindow.cs:1507` · `:5223` · `Shell/DraftModeWindow.cs:2605` · `:2699` · `:2714` · `:3696` · `:3949` · `:4062` · `Shell/PracticeModePopup.cs:3759` · `Shell/RankedDivisionChangeWindow.cs:1206` | ✅ | |
| `OnKill` | 17 · `Core/CardFeel.cs:1113` · `:1503` · `:1948` · `:2193` · `:3371` · `:3397` · `:3434` · `:3682` · `Core/UnitTweenRuntime.cs:101` · `Shell/AvatarTab.cs:866` · `Shell/CollectionWindow.cs:5224` · `Shell/DraftModeWindow.cs:2606` · `:2700` · `:2715` · `:3697` · `Shell/PracticeModePopup.cs:3760` · `Shell/RankedDivisionChangeWindow.cs:1207` | ✅ | |
| `OnPlay` | 2 · `Editor/BattleScene.cs:41682` · `:41994`（文本 8 处） | ✅ | |
| `OnStart` | 3 · `Shell/DraftModeWindow.cs:3695` · `:4052` · `Shell/RankedDivisionChangeWindow.cs:1203` | ✅ | |
| `OnUpdate` | 3 · `Battle/BattleDriver.cs:24079` · `Core/CardFeel.cs:1104` · `Shell/CollectionWindow.cs:1504` | ✅ | |
| `Kill`（实例扩展） | 77 · `Battle/BattleCameraSreenSize.cs:257` · `:277` · `:301` · `:426` · `Battle/BattleDriver.cs:4414` · `:4420` · `:4430` · `:4436` · `:4497` · `:4504` · `:6358` · `:34238` · `Battle/BattlePostFx.cs:419`×2 · `:468` · `:514` · `Battle/SkillPanel.cs:626` · `:627` · `:1550` · `:1633` · `:1718` · `:1719` · `:1731` · `Core/CardView.cs:4113` · `:6517` · `:8253` · `:8275` · `:8334` · `:8510` · `:8649` · `:8663` · `:8832` · `:9104` · `:9132` · `:9316` · `:10039` · `:10040` · `:10513` · `:10551` · `:12612` · `:15078` · `:15092` · `Editor/B104_CardFeel_Assert.cs:298` · `:320` · `:395` · `Editor/B110_S1_Assert.cs:326` · `Editor/BattleScene.cs:3665` · `:12004` · `:13779` · `:13780` · `:17604` · `:21247` · `:23074` · `:23122` · `:23168` · `:23382` · `:23383` · `:24569` · `:24588` · `:49438` · `:49450` · `Editor/CardBaseDemo.cs:5607`×2 · `:5618` · `Editor/CollectionScene.cs:8654` · `Hand/CardInteraction.cs:459` · `Shell/AvatarTab.cs:845` · `:856` · `Shell/BoosterPackOpenWindow.cs:1105` · `:1433` · `Shell/CardDetailPopup.cs:1045` · `Shell/CollectionWindow.cs:5216` · `:5244` · `Shell/DraftModeWindow.cs:3908` · `Shell/GachaEventWindow.cs:1577` · `Shell/PracticeModePopup.cs:3755` · `Shell/RewardWindow.cs:1065` | ✅ | `Kill()` / `Kill(bool)` 两边都在 |
| `Play` | 1 · `Battle/BattleDriver.cs:38636`（`seq.Play()`，在 `ScheduleAction` lambda 里） | ✅ | 全仓其它 `.Play()` 全是 `AudioSource.Play()` / `Animation.Play()` / `ParticleSystem.Play()` / `TraitFrames.Play()` |
| `Pause` | 1 · `Battle/BattleDriver.cs:38635`（`seq.Pause()`） | ✅ | `BattleMusic.cs:141` 的 `s.Pause()` 是 `AudioSource`，**不是** DOTween |
| `Restart` / `Rewind` / `Flip` | **0** | ✅ | `Core/CardFeel.cs:3270/3271/3276` 的 `Flip(...)` 是**我们自己的私有静态方法**（`:3276` 就是它的定义），不是 `TweenExtensions.Flip` |
| `Complete` | 3 · `Battle/BattleCameraSreenSize.cs:419` · `Battle/BattlePostFx.cs:536` · `:538` | ✅ | |
| `Goto` | 2 · `Editor/BattleScene.cs:23064` · `:23069`（走 `DG.Tweening.TweenExtensions.Goto(qtw, …)` 静态形式） | ✅ | |
| `IsActive` | 26 · `Battle/BattleCameraSreenSize.cs:402` · `:415` · `:419` · `Battle/BattlePostFx.cs:536` · `:538` · `:552` · `Core/CardView.cs:3319` · `:4113` · `:6517` · `:8146` · `:8253` · `:8275` · `:8334` · `:8510` · `:8649` · `:8663` · `:8832` · `:9316` · `:10040` · `:10513` · `:10551` · `:12612` · `:15078` · `:15092` · `Hand/CardInteraction.cs:437` · `Shell/CollectionWindow.cs:5216` | ✅ | |
| `IsComplete` | 2 · `Editor/DOTweenSmokeTest.cs:37` · `:46` | ✅ | |
| `IsPlaying` / `CompletedLoops` / `ElapsedPercentage` / `ElapsedDirectionalPercentage` | **0** | ✅ | 原版自己用了 `CompletedLoops` / `ElapsedPercentage` / `ElapsedDirectionalPercentage`，都在 |
| `Duration` | 11 · `Core/CardFeel.cs:3513` · `Editor/BattleScene.cs:12001` · `:48697` · `Editor/CardFaceProbe.cs:1023` · `:1025` · `Editor/CollectionScene.cs:7393` · `:7396` · `:8611` · `:8637` · `Editor/DOTweenSmokeTest.cs:33` · `Hand/CardInteraction.cs:448` | ✅ | 已排除 `EventTiming.DurationOf`（我们自己的） |
| `Elapsed` | 4 · `Battle/BattlePostFx.cs:553` · `Core/CardFeel.cs:1109` · `Core/CardView.cs:3319` · `Hand/CardInteraction.cs:448` | ✅ | |
| `SetAutoKill` / `SetOptions` / `SetRecyclable` / `SetInverted` / `SetAs` / `SetSpeedBased` / `SetLookAt` / `SetPathForwardDirection` | **0** | ✅ | 两边都在，未使用 |

### 2.4 **Modules 源码**（`Assets/Plugins/Demigiant/DOTween/Modules/*.cs`）对核心 DLL 的依赖

这 9 个 `.cs` 会**编进 Assembly-CSharp**（和原版一样：原版的 `DG.Tweening.DOTweenModuleUI.*` 在 `Assembly-CSharp-firstpass` 里），所以它们能不能编译，直接决定「换核心 DLL」可不可行。

- 逐条比过：Modules 引用到的 84 个核心成员，**在原版 DLL 里全部存在**；
- 唯二两个「疑似缺」的经人工核实都是**假阳性**：
  - `SetOrientationOnPath` —— 是 `DOTweenModuleUtils.cs:95` **自己定义**的静态方法；它注册的 `DOTweenExternalCommand.SetOrientationOnPath` 事件**两边签名完全相同**（`Action<PathOptions, Tween, Quaternion, Transform>`）。反编译里原版把事件展开成 `add/remove` 两个访问器，才让脚本误判。
  - `Vector2` —— 命中全是 `new Vector2(...)`，不是 `DOVirtual.Vector2`。
- **`DOTweenModuleUIToolkit.cs` / `DOTweenModuleEPOOutline.cs` 是我们多出来的两个模块**（原版没有），但它们**不引用任何原版缺失的 API**。

> ⚠️ 这两个模块的 `.cs.meta` 有毛病（见 §五 · 顺手发现 3），**跟 DLL 版本无关**。

### 2.5 **`DOTweenEditor.dll`（编辑器侧）对核心 DLL 的依赖**

| 引用到的、原版缺的成员 | 次数 | 影响 |
|---|---|---|
| `DOTweenSettings.uiToolkitEnabled` | **2** | 🔴 只换运行时 `DOTween.dll`、不换 `DOTweenEditor.dll` ⇒ 编辑器侧**引用一个不存在的字段** ⇒ `MissingFieldException`（Inspector / 菜单 / 导入时报错） |

其余 10 个 delta 成员在 `DOTweenEditor.dll` 里 **0 引用**（逐条 grep 过）。

---

## 三、缺口汇总 + 结论

### 3.1 方法（穷举，不是抽样）

1. 用自写解析器把**两边 DLL 的 TypeDef / MethodDef / Field 表全部导出**，按 `(类型全名, 成员名)` 做多重集比对。
2. 对签名再做一遍：把每条 MethodDef 的**原始签名 blob** 逐字节比对。差异只出现在 `TypeRef` 行号重排（两边 TypeRef 行数 105 vs 62）造成的编码差异上，**已用反编译出的 C# 声明逐条复核**。
3. 用 `ilspycmd` 反编译两份，按「多行签名感知」的方式提取 `public/protected` 声明，做集合差。
4. **交叉验证「83,968 B 那份是不是完整的原版 API 面」**：拿**原版游戏自己**（`d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp{,-firstpass}`、`d:/2/tools/decomp_full/`）实际调用的 DOTween 成员逐条核对 —— **原版自己用的每一个成员，那份 stub 里都有**（含 `DOTween.To/ToAlpha/Sequence/Kill/KillAll/Shake/Punch/Goto/Flip/Complete/Restart/Rewind/SmoothRewind/TogglePause/Play/PlayForward/PlayBackwards/Pause/IsTweening/Init`，`TweenSettingsExtensions.*` 18 个，`TweenExtensions.*` 11 个，`DOVirtual.DelayedCall/EasedValue`，`ShortcutExtensions.*` 29 个，以及 `DOTweenModuleUI/Audio/Sprite/Physics/Physics2D/UnityVersion/Utils` 的全部扩展方法）。

### 3.2 **缺口汇总**

**A. 我们代码（含 Modules 源码）引用、而原版 DLL 没有的 API：0 个。** ✅

**B. 原版 DLL 没有、我们 DLL 有的：11 条**（这是唯一的版本差异面）：

| # | 成员 | 种类 | 我们代码用了吗 |
|---|---|---|---|
| 1 | `DOTween.tweenersCapacity`（get/set） | 公共属性 | ⛔ 0 引用 |
| 2 | `DOTween.sequencesCapacity`（get/set） | 公共属性 | ⛔ 0 引用 |
| 3 | `DOTween.Version = "1.3.030"` | 只读静态字段 | ⚠️ 仅 `Editor/DOTweenSmokeTest.cs:27` 打印用；**原版字段在、只是没有初始值** |
| 4 | `DOTween.RuntimeOnLoad()` | private static | ⛔ 0 引用（内部） |
| 5 | `EaseManager.EvaluateUnclamped(...)` | public static | ⛔ 0 引用 |
| 6 | `DOVirtual.Vector2(Vector2, Vector2, float, TweenCallback<Vector2>)` | public static | ⛔ 0 引用 |
| 7 | `TweenExtensions.Done<T>(this T)` | 公共扩展 | ⛔ 0 引用 |
| 8 | `TweenExtensions.IsLoopingOrExecutingBackwards(this Tween)` | 公共扩展 | ⛔ 0 引用 |
| 9 | `TweenExtensions.IsTimeScaleIndependent(this Tween)` | 公共扩展 | ⛔ 0 引用 |
| 10 | `TweenSettingsExtensions.JoinCallback(this Sequence, TweenCallback)` | 公共扩展 | ⛔ 0 引用 |
| 11 | `DOTweenSettings.uiToolkitEnabled` | 序列化字段 | 🔴 **`DOTweenEditor.dll` 引用了 2 次**；`Assets/Resources/DOTweenSettings.asset` 里有 `uiToolkitEnabled: 0` |

**C. 反方向（原版有、我们没有的 API）：0 条。**

> 唯一一处「原版多出」的成员是 `TweenManager._isQuittingFrame`（**private 字段**，内部实现细节，不构成 API）。
> `Tween.hasLoops` / `Tween.intId` / `TweenParams.Params` / 各 `DOTweenSettings.*` 字段**两边都有**——它们在差集里出现，只是**原版 stub 丢了字段初始化器**、以及反编译把属性写成 `=> false` 这一种形式，属**误报**，已逐条人工排除。

### 3.3 一句话结论

> **「可以谈替换」—— API 层面零缺口；但「能不能真换」不在 API 上，在别处。**
>
> - ✅ **API 安全性 = 成立**：我们代码 + 9 个 Modules 源码 + 原版游戏自己用到的 DOTween 成员，**100% 都在原版 DLL 里**，没有一条会因为「API 不存在」编不过。
> - 🔴 **但本机的 83,968 B 那份不是可运行的 DOTween**：它是**方法体全空的参考程序集（stub）**。直接换上去，`DOTween.To(...)` 全返回 `null` ⇒ **整个表现层的补间静默失效**（不报错、只是不动）——正是 B-96 当初担心的那个风险面，而且**一定会发生**。
> - 🔴 **要换就得整包换**：`DOTween.dll` + `DOTweenEditor.dll` +（可选）`DOTween.XML` / `DOTweenSettings.asset` 一起。只换运行时那一个 ⇒ 编辑器侧 `MissingFieldException`。
> - ⛔ **本机根本没有「原版真正的 DOTween 运行时 DLL」**（8 个候选位置全查过，见 §1.4）。要拿到原版行为，只能从 `GameAssembly.dll` 把 DOTween 核心方法反编译出来 —— 见 §五 · 顺手发现 1。

---

## 四、没查清的部分

1. **原版 DOTween 的确切版本号 —— 查不到。**
   - 三份 83,968 B 的 `DOTween.Version` 字段**初始化器都丢了**（stub 特性）；`#US` 堆里也扫不到 `1.x.yyy` 形式的 UTF-16 串；`il2cpp_out/DummyDll` 与 `cpp2il_out` 那两份同样扫不到。
   - 只能给**边界**：原版**早于** `1.3.030`，且 `DOTweenModuleUIToolkit` 此时还不存在（原版 Assembly-CSharp-firstpass 里没有该模块）。
   - **搜过的位置**：`d:/2` 全盘的 `DOTween*.dll`（11 个）· `d:/4` 全盘 · 安卓 XAPK 主 APK · `d:/2/新解包资源`。
2. **原版 DOTween 的运行时行为 —— 完全没查。** 核心方法（`TweenManager` / `Tween` / `Tweener` / `Sequence` / 各 Plugin）在 `decomp_full` 里**一个都没有**，见 §五 · 顺手发现 1。所以「原版的缓动曲线/边界行为/安全模式与 1.3.030 有没有差别」**没有判据**。
3. **`DOTweenSettings.asset` 换旧版 DLL 后 Unity 会怎么处理 `uiToolkitEnabled`** —— 没实测（红线：不跑 Unity）。按 Unity 的常规行为应是「丢弃未知字段」，但这只是**推断**，没验证。
4. **`DOTweenUpgradeManager.dll`（11,264 B）** 只查了名字，没查它对核心 DLL 的依赖（它只在编辑器里跑升级向导，风险低）。
5. **`Modules/*.cs` 能不能真的对旧核心编译通过** —— 只做了「成员存在性 + 签名一致性」的静态判定，**没有真跑编译器**（红线：不跑 Unity / `typecheck` 是主对话的活）。

---

## 五、顺手发现（⛔ 没动手改）

1. 🔴🔴 **原版 DOTween 的核心实现从来没被反编译过 —— 1,847 个方法躺在 `GameAssembly.dll` 里，`decomp_full` 里 0 个。**
   实证：`grep -P "^\d+\tDG\.Tweening\." d:/2/tools/all_methods.txt` 命中 **2,182** 条；剔掉 `Module` / `CYInstruction` 还剩 **1,847** 条。而 `ls d:/2/tools/decomp_full | grep -c TweenManager` = **0**。
   `decomp_full` 里只有 **Module 源码**（它们本来就在 `Assembly-CSharp-firstpass`，是 C# 编进去的，不是 DOTween.dll）+ `DOTweenCYInstruction`。
   头部类型量：`Tweener` 161 · `DOTween` 111 · `ShortcutExtensions` 102 · `Plugins.Core` 88 · `TweenSettingsExtensions` 82 · `TweenManager` 64 · `DOTweenComponent` 55 · `Easing` 52 · `TweenerCore<Vector3,…>` 51 · `TweenExtensions` 40。
   ⇒ **「B-96 想换成原版」的真正前提 = 先把这 1,847 个方法解出来**，否则无论换哪份 DLL 都不是原版行为。这是 `完全复刻` 口径下的一条实打实的缺口，建议立账。
2. **`Modules/DOTweenModuleUIToolkit.cs.meta` 是坏的** —— 只有 59 B（`fileFormatVersion` + `guid` 两行，**没有 `MonoImporter:` 块**），而同目录其它 `.cs.meta` 是 178 B 带完整 `MonoImporter`。同一目录的 `DOTweenModuleEPOOutline.cs.meta` 也是 262 B 正常。
   现在没出事大概是因为 Unity 会重写 meta；但**这是「5 行 meta」那类静默隐患的同族**。
3. **`DOTweenModuleEPOOutline.cs` + `DOTweenModuleUIToolkit.cs` 是我们比原版多出来的两个模块**（原版 Assembly-CSharp-firstpass 里没有）。若将来做「按原版裁剪」，这两个是候选；**但它们现在不造成任何差异**，不用动。
4. **`DOTween.dll` 的 `AssemblyVersion` 是 `1.0.0.0`，两边一样** —— ⇒ **⛔ 绝不能靠 AssemblyVersion 判「是不是同一版」**（会得出「完全相同」的假结论）。唯一可靠的判据是 `DOTween.Version` 字面量或 API 差集。
5. **`d:/2/Warpforge_code/Assemblies/DOTween.dll`（83,968 B，MD5 与 assetripper 那份不同）API 面完全一致** —— 即 **MD5 不同的两份可以是同一版本**（元数据排布/重建顺序不同）。⇒ 以后别拿 MD5 判版本。
6. **`d:/4/Unity/MyGame/Library/Bee/artifacts/…/ManagedStripped/DOTween.dll`（178,176 B）** 就是我们自己那份的构建拷贝 —— 全盘搜 `DOTween*.dll` 时会命中它，**别把它当成「第三份原版」**。
