# B169/W5 · AssetBundle 版本兼容探针（6000.3 打包 → 6000.2.6f2 客户端加载）

**日期**：2026-11-07 · **代理**：W5（mod 写手）· **状态**：代码已落、已编译，**未跑实机**（本代理不启动游戏）

## 一、改了什么

| 文件 | 动作 |
|---|---|
| `E:/warpforge/05_工具/mod/WfOfflineProbe/Probes/BundleCompatProbe.cs` | **新建**（独占文件）。`Tick()` 由 OnUpdate 每帧调，`_done` 保证只跑一次 |
| `E:/warpforge/05_工具/mod/WfOfflineProbe/ModMain.cs` | **只加 1 行**（`:275`）：`try { BundleCompatProbe.Tick(); } catch { }`，放在 `AltArtProbe.Tick()` 之后；文件其它内容未动 |

## 二、探针做了什么

1. `BEGIN`：记录 `path / exists / size / t / Application.unityVersion`（自动带出客户端版本，用来对照包的版本）。
2. 临时挂 `Application.logMessageReceived`（调用窗口内）→ `AssetBundle.LoadFromFile("E:/warpforge/_tmp_view/bundle_test/out/testbundle")`。
   - 返回 null 时 Unity **不抛异常、只写日志** ⇒ 窗口内的每条 Unity 日志都落成 `EXC  unity-log …` 行，null 才有根因。
   - 真抛异常时记 `EXC  LoadFromFile threw <FullTypeName>: <Message>`。
3. `LOAD result=OK|NULL  exception=…`
4. `ASSETS count=N` + 每条 `GetAllAssetNames()`。
5. `SPRITE`：`testsprite` / `assets/testsprite.asset` **两个名字 × 两种重载**（泛型 `LoadAsset<Sprite>` 与 `LoadAsset(name, Il2CppSystem.Type)`）；命中时额外记 `rect=WxH tex=WxH`。
6. `UNLOAD ok unloadAllLoadedObjects=true`（`ab.Unload(true)`）。
7. 产物：`E:/warpforge/07_产物/phase0/bundlecompat.tsv`（每行 `ts \t kind \t detail`，kind ∈ BEGIN/LOAD/ASSETS/SPRITE/UNLOAD/EXC）。
8. 开关：`WF_PROBE_BUNDLECOMPAT=0` 可关（默认开）；包按**绝对路径**加载，**没有**往游戏目录拷任何东西。

## 三、编译

`bash E:/warpforge/05_工具/mod/WfOfflineProbe/build.sh` → **0 错误**（仅 1 条既有警告 `CurrencyFixProbe._enabled` CS0414，与本批无关）。
产物 `bin/WfOfflineProbe.dll` = **327,168 B**，mtime **2026-11-07 19:21**。

## 四、实机要看哪几行

1. `phase0/bundlecompat.tsv` 的 **`LOAD`** 行：`result=OK` ⇒ 向下兼容成立；`result=NULL` ⇒ 不兼容。
2. 紧随其后的 **`EXC unity-log …`**：若出现 `can't be loaded because it was not built with the same version of the Unity runtime` 之类，即根因。
3. `ASSETS count=`（应 = 2：`assets/testtex.asset` / `assets/testsprite.asset`）与 `SPRITE … -> OK type=UnityEngine.Sprite …`。
4. 对照信息：测试包 `testbundle.manifest` 头部写着 `UnityVersion: 6000.3.23f1`，客户端 = 6000.2.6f2（探针 `BEGIN` 行会打印实际值）。

## 五、待排除的风险（先记，未验）

- `Il2CppClassPointerStore<Sprite>.NativeClassPtr` + 私有 `Il2CppSystem.Type.internal_from_handle` 是反射路径，理论上可能返回 null ⇒ 那种情况探针会写 `typed … SKIP (Il2CppSystem.Type 解析失败)`，**不影响泛型那一路的结论**。
- 挂 `Application.logMessageReceived` 会短暂多一份日志回调，`finally` 里已摘除；`UnityLogProbe` 的订阅不受影响。
