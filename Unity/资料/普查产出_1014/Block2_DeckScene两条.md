# Block 2 · `DeckScene.Run` 两条红（#10 / #11）—— 已改

**日期**：2026-10-13 收口批次 · B2 写手代理
**独占文件**：`Assets/CardPresentation/Editor/DeckScene.cs`（只动了这一个文件）
**判据**：`资料/普查产出_1014/清单_90条红改法.md` §一 第 2 组 · `资料/普查产出_1013/D1013_诊断_块1_Battle与Deck.md` §三 第 10/11 条

---

## 一、逐条结果

| # | 改了什么（文件:行） | 判据 | 做完没有 |
|---|---|---|---|
| **#10** | `Unity/MyGame/Assets/CardPresentation/Editor/DeckScene.cs:3829-3834` —— `Application.LogCallback sink = (cond, msg, type) => logs.Add(msg);` 改成 `(condition, stackTrace, type) => logs.Add(condition);`（**取第 1 个参数**），并在上面补 3 行注释写明签名 | 清单 §一 第 2 组 #10「`Application.LogCallback` 取**第 1 个**参数：`logs.Add(cond)`（现在取第 2 个 = `stackTrace`）」；D1013 §三·10 | ✅ 改完（静态判据闭合，见下 §二） |
| **#11** | **同一行改动**（`:3834`）—— 期望**一个字没动**（`:3850` 仍是 `spoken.Contains(lib.LastError)`） | 清单 §一 第 2 组 #11「**同一行改动**（#10）改完本条自动绿；⛔ **不许**把期望改成「堆栈里有的东西」」 | ✅ 改完（**没有**动期望；静态推演应自动绿，见 §二·2） |

### 改动全文（`git diff` 原文）

```diff
@@ -3829,5 +3829,8 @@ public static class DeckScene
                     // 出声断言走本仓**现成**的那条范式（`Application.logMessageReceived`；先例
                     // `Editor/CollectionScene.cs` 的 A229 那段 —— 那里断的就是「几条警告」）。
-                    Application.LogCallback sink = (cond, msg, type) => logs.Add(msg);
+                    // 🔴 A547：`Application.LogCallback` 的签名是 `(string condition, string stackTrace, LogType type)`
+                    // —— **第 1 个才是消息正文**，第 2 个是**堆栈**（这里原来取的是第 2 个 ⇒ 下面三条
+                    // `Contains` 全在比堆栈）。参数名照签名写死，免得下一个人再把第 2 个当消息用。
+                    Application.LogCallback sink = (condition, stackTrace, type) => logs.Add(condition);
```

> 参数名从 `(cond, msg, type)` 一并改成 `(condition, stackTrace, type)` —— 这是 D1013 §三·10 的最小改法里
> **明写的备选**（「或把参数名改成 `message` / `stack` …… **免得下一个人再踩**」）。旧的 `msg` 名字本身
> 就是误导源（读起来像 message、实际是 stackTrace），所以照签名写死。**语句只有一行**，期望值一个字没动。

---

## 二、静态判据闭合（⛔ 本代理不跑 Unity，以下是**逐行读源码**得到的，不是跑出来的）

**#10 应当转绿**：窗口（`:3835` 订阅 → `:3842` 退订）内 `TryImport` 失败时，消息正文确实含「导入失败」。

- `Deck/DeckRuntime.cs:2520` = `Say("导入失败：卡组串读出来了，但**没写进存档**——" + SaveFailReason() + "（重启就没了）")`；
- `Deck/DeckRuntime.cs:1948` = `Say` 的出口 `Debug.Log("[Deck] " + _noticeText)` ⇒ **`condition` 参数 = 这句正文**；
- 调用链 `DeckScene.cs:3839` → `UiTryImport`（`DeckRuntime.cs:2949`）→ `TryImport`（`:2506`）→ `Say` ⇒ **完全在 sink 窗口内能抓到**。

**#11 应当转绿**：真原因确实在消息正文里。

- `Deck/DeckRuntime.cs:2261-2263` = `SaveFailReason()` 返回 `Library.LastError`（空则兜「写不进存档文件」）；
- `:2520` 把它**拼进消息正文** ⇒ `spoken.Contains(lib.LastError)` 成立；
- `:3850` 读的 `lib` = `:3823` 的 `_rt.Library`，`Library` 是 `private set` 的稳定实例（`DeckRuntime.cs:270`）；
- `:3839` 之后到 `:3850` 之间**只剩两条 `Check`**（`:3077` 的 `Check` 是纯比较 + `Debug.Log/LogError`，见 `DeckScene.cs:77-89`），
  **没有任何落盘/写 `LastError` 的调用** ⇒ 消息里那句与 `:3850` 现读的那句**是同一个字符串**。

**第三条断言（`:3848` `!spoken.Contains("已导入")`）改完后仍成立**（我顺手核过，因为它原来「靠比堆栈」才假绿）：

- 窗口内只有两条消息会被抓到：① `:3835` `UiOpenImport()` → `OpenImport()`（`DeckRuntime.cs:2472`）说
  「把卡组串粘进输入框（Ctrl+V），再点 Confirm」（substring 里**没有**「已导入」）；
  ② 上面那句「导入失败：……」（`"导入失败"` 里也**不含**子串 `"已导入"`）。
- `UiSetImportText`（`:2948`）只赋值 + `RefreshImportText()`，无日志。

⚠️ **以上是静态推演**，真正的绿红要等调度台在同步点跑 `DeckScene.Run`。若 #11 **没转绿**，
请把实得值原样带回来（**别**去改期望 —— 清单 #11 明令禁止）。

---

## 三、顺手发现（⛔ 只报不改）

1. **A533 的弱断言就在本文件**（清单 `资料/普查产出_1014/清单_A表全量.md:59` 记为 `Editor/DeckScene.cs` 约 `:3073`）——
   实际是 **`DeckScene.cs:3076-3077`**（A 表那个 `~:3073` 是**改前**的近似行号，我的改动在它之后，**没有平移它**）：

   ```csharp
   CheckTrue(wm2 == null || !one.gameObject.activeSelf || wm2.popUpWindow != one,
             "（收尾）1 按钮版那一扇收掉了");
   ```

   **正是「三条件命一即可」**：`wm2 == null` 那一支在**收尾信号丢失时也恒真** ⇒ 这条断言在
   「`WindowsManager` 拿不到」这种**最该报的情况**下反而是绿的。**不属我这两条**，⛔ 未动。
   （同族旁证：`:3060-3061` 也有一条 `== null || == ""` 的弱断言。）

2. **A535 指向的同一段**（`清单_A表全量.md:60`：`SaveAndSay()` 成功支的 `HideDeckPopUp()` 失去断言覆盖）——
   标着「**待裁**」，我这份简报没覆盖，**未动**。

3. `:3829-3830` 那两行注释说「走本仓现成的那条范式（`Application.logMessageReceived`；先例 `CollectionScene.cs` 的 A229）」
   —— 我核过 `CollectionScene.cs` 那 8 处（`:2201/2206/2515/2520/5712/5791/5803/5991`）**全部取第 1 个参数**，
   与 D1013 §三·10 的全仓反证一致 ⇒ **那两行注释成立、不必订正**。

---

## 四、没做完的 / 判不了的

- **没跑 Unity**（简报红线）⇒ 两条的**绿红未经实跑验证**，只有上面 §二 的静态判据。这是本批唯一未闭合项。
- **判不了**的：`lib.LastError` 在真跑时是否**逐字**等于消息里拼进去的那句（依赖 `DeckLibrary` 内部在同一个失败
  路径上两次赋的值完全一致）。我按源码判是**同一个字符串**（中间无写点），但 `DeckLibrary` 不在我的白名单里、
  未逐行读它的 `Save()`/`SaveOrWarn()`。若 #11 红，第一嫌疑就是这里，实得值请原样带回。

---

## 五、自检

- **秒级类型检查**：`TMPDIR=/tmp/wf_b2 bash d:/4/Unity/工具/typecheck.sh`
  ⇒ **运行时错误数 0 · 编辑器错误数 0**（一次通过，**没有**出现「错全在别人文件里」那种情况）。
- **行尾**：`git diff --numstat` = `4 1`（4 增 1 删，**不是**整篇重写）；
  二进制数 `CRLF=4023 / LF=4023` ⇒ **纯 CRLF 保住**，一条 LF 都没混进来。
- **越界**：`git status` 只多出本报告；`Editor/DeckScene.cs` 是唯一被改的源文件。
