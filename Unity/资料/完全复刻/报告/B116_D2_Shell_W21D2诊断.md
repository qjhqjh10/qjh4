# B116_D2 · ShellScene 唯一红点 `W21-D2` 诊断报告

- 会话/代理：`B116_D2`（只读诊断）· 2026-11-02
- 对象：`[B116W21] ✗ W21-D2 ★★ 中央档**一个字节都没动**（仍是哨兵 999…）` —— `ShellScene.Run` 6,028 通过 / 1 失败
- 日志：`d:/4/_tmp_view/shell.log`（失败在 `:1261007`）

## 一、四档判定

**（α）断言本身错** —— 断言把哨兵写进了临时根，却在把 `SaveCenter.OverrideRoot` 清成 `null` **之后**用 `SaveCenter.GetRaw` 去读它；此时 `RootDir` 已指回**玩家真档目录**，那里本来就没有这份文件。被测实现（`B-322` / `UI-15` 活动通行证切档）**完全正确**。

不是 (β)：门真的关着（`W21-D0'` 绿）、`Save()` 真的只写旧档（`W21-D1/D1'/D1''` 绿）、读口真的只读旧档（`W21-D3` 绿=60）。
不是 (γ)：`SaveCenter` 本批只加了类尾的 `B116_W26` 对账段，`OverrideRoot / RootDir / StoreOn / GetRaw` 一个字节没动（`git diff` 仅 `@@ -1173` 与 `@@ -1457` 两处新增；`B116_W38/W40` 报告里 0 处涉及 `SaveCenter`）。
不是 (δ)：门关成、哨兵写成、中央档路径也没错 —— 错的是**读的那一侧指向了别处**。

## 二、证据链（逐跳）

1. **哨兵写进临时根**（日志 `:1260882`）：
   `[SaveCenter] 写 '_ours_ExpansionPass' 成功：v1 · 84 B · C:\Users\qjh36\AppData\Local\Temp\wf_b116w21\save\_ours_ExpansionPass.json`
   —— 对应 `Editor/B116W21_Assert.cs:130-132`（`OverrideRoot = saveRoot` → `SetRaw`，前提断言绿）。
2. **门在这一刻关掉**：`B116W21_Assert.cs:133` `SaveCenter.OverrideRoot = null;`；`:135` `!StoreOn` 断言绿。
3. **写盘确实只落旧档**：`B116W21_Assert.cs:142-148` 全绿；`Shell/ExpansionPassEvent.cs:272` → `SaveCenter.SaveThroughLegacy` 的 `!StoreOn` 支（`Shell/SaveCenter.cs:1112-1116`）只 `AtomicWrite(旧档)`，**不碰中央档**。
4. **红在这条读**：`B116W21_Assert.cs:149` `SaveCenter.GetRaw(...)` → `Shell/SaveCenter.cs:80-88` `RootDir` 在 `OverrideRoot == null` 时取 `Application.persistentDataPath/WarpforgeSave`；`GetRaw`（`:182-195`）**不受 `StoreOn` 约束**（读不设门，设计如此）⇒ 去找玩家真档。
   日志紧邻上一行即证据：`[SaveCenter] 缺 key '_ours_ExpansionPass'（找过 C:/Users/qjh36/AppData/LocalLow/QJHWarpforge/WarpforgeReplica\WarpforgeSave\_ours_ExpansionPass.json）⇒ 返回 null`
   ⇒ `sentinel == null` ⇒ `:150` 的 `Tr` 红。
5. **对照：同一批的 `B116W26` 做对了** —— `Editor/B116W26_Assert.cs:155-166`：清门前先 `ePath = SaveCenter.PathOf(key)` 把**绝对路径**留住，关门后用 `File.ReadAllText(ePath)` 逐字节比，**不经过 `GetRaw`**。`B116W21` 的 D 段没有沿用这个正确模板。

## 三、根因（一句话）

断言把「写在哪」与「从哪读」拆到了两套寻址上：写用 `saveRoot`（临时），读用 `GetRaw`（随 `OverrideRoot` 走），而 `:133` 已把 `OverrideRoot` 复原为 `null` ⇒ 读的是玩家真目录 ⇒ 恒 null ⇒ 这条**在任何机器上都必然红**（与产品行为无关）。

## 四、最小修复方案（⛔ 本代理不动手）

改 `Unity/MyGame/Assets/CardPresentation/Editor/B116W21_Assert.cs:149-151`，**3 行替换**（`saveRoot` 在 `:72` 已声明、仍在作用域）：

```csharp
string centralPath = Path.Combine(saveRoot, SaveKeys.OursExpansionPass + ".json");   // 门关着 ⇒ 别走 GetRaw（它会指回玩家真档）
string sentinel = File.Exists(centralPath) ? File.ReadAllText(centralPath) : null;
Tr(sentinel != null && sentinel.Contains("\"totalScore\":999"),
   "W21-D2 ★★ 中央档**一个字节都没动**（仍是哨兵 999 …）");
```

- 中央档带 `_v` 信封，但 payload 里 `"totalScore":999` 原样在 ⇒ `Contains` 判据不变、语义不变。
- 若想更贴红线，可再加一条**真档字节快照**：写前记 `File.GetLastWriteTimeUtc/字节` of `Path.Combine(Application.persistentDataPath, "WarpforgeSave", SaveKeys.OursExpansionPass + ".json")`，写完比对（模板同 `B116W26_Assert.cs:155-166`）。
- 副作用（正向）：这条 `GetRaw` 每次运行都会往日志打一条**假的**「缺 key」警告（`:1260993` 附近），修好后噪音一并消失。
- ⚠️ 别改成「读完再 `OverrideRoot = saveRoot` 重开」了事：那会在 D3（`:152-153`）之前重新打开写门，且丢了「读的是临时根」这层保护。直接读文件最稳。

## 五、是否本批回归

**不是。** `B116W21_Assert.cs` 是本批**新建未跟踪文件**（`git status` = `??`），`B116_W21` 报告 §自述第 4 条**明写「未跑 Unity（红线）」** ⇒ 这条断言**从未被执行过**，第一次跑就现形。产品侧 `Shell/ExpansionPassEvent.cs`（`:226` 读 / `:272` 写）与 `Shell/SaveCenter.cs` 的门（`:92-95` / `:1112-1116` / `:1165-1175`）都**按设计工作**，无需改动。
