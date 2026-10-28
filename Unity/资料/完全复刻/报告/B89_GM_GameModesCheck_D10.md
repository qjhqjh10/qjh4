# B89 收尾 · `GameModesCheck` D10 那 1 条红

代理 `GM` · 2026-10-28 · 唯一改动文件 = `MyGame/Assets/CardPresentation/Editor/GameModesCheck.cs`（**产品代码一行未动**）

## 一、判档：**α — 断言期望值算错**（实现与原版一致，⛔ 不是 β 实现缺陷、也不是 δ 夹具摆错）

- 被测实现 `Shell/GameModesHandler.cs:263 GetActiveEvent(Type)` 没改：三条支（拿不到配置 ⇒ null · 递归 · 普通 LINQ）与 `.c` 逐句一致。
- 红的原因：**D10 期望值算错了** —— 写断言时按「type 路径只剩 Classic 那几条」取 `e2`，忘了夹具里 `e4`（`PlayModes.Duel`, 优先级 **50**）**也是** `FakePlayEvent` 的实例，而**原版 type 路径没有任何模式闸**。

## 二、判据（原版，逐句）

`d:/2/tools/decomp_full/GameModes__GetActiveEvent.c`：

```
if (TypeReference.get_Type(...) != null && 该 Type != typeof(IRankedEvent)) {   // 两个 op_Inequality
    r = GameModes__GetActiveEvent(this, 该Type);  if (r != null) return r;      // ★ 递归
}
// 落到这里 = ① 问的不是排位接口 ② mainRankedType 就是接口本身
GetEvents().Where(ReflectionHelper.Is(g, gameModeType))   // ← 只有**类型**闸，**没有** EventPlayMode 闸
          .Where(g => g.EventState == Active)
          .OrderByDescending(g => g.Priority)
          .FirstOrDefault();
```

⇒ **`mainRankedType == typeof(IRankedEvent)` 时不递归、直接落回普通 LINQ**（我们这条实现是对的）；
⇒ **那条 LINQ 不带模式闸** ⇒ 优先级最高的匹配者胜，与它属于哪个 `PlayModes` 无关。

夹具事实（`GameModesCheck.cs:194-198`）：`e1` Classic p5 · `e2` Classic p9 · `e3` Classic p99 **已过期（被 Active 闸刷掉）** · `e4` **Duel p50、Permanent ⇒ Active、是 `FakePlayEvent`** · `e5` `FakeOtherEvent` p1。
⇒ `GetActiveEvent(typeof(FakePlayEvent))` 的正确答案 = **`e4`**（p50 > p9）。实跑首轮就是返回 `e4`，所以 `ReferenceEquals(…, e2)` 必红。

## 三、改了什么（零行数变化 · 就地改写，行号 227-229 未移位）

```csharp
GameModesConfigSource.MainRankedType = typeof(FakePlayEvent);   // = 接口本身 ⇒ 不递归，走普通 LINQ（该支**无模式闸**）
Ok(ReferenceEquals(h.GetActiveEvent(typeof(FakePlayEvent)), e4),
   "D10 `mainRankedType` 就是接口本身 ⇒ 不递归、落回普通 LINQ（原版 type 路径只有 Is(g,t) ∧ Active ∧ 优先级降序 ⇒ Duel 的 e4(p50) 也是 FakePlayEvent、胜 Classic 的 e2(p9)）");
```

- **只改了期望值 + 消息**，夹具一个字没动（⛔ 没把 `e4` 的优先级调低 —— 那会把这个「type 路径无模式闸」的原版性质藏起来；也⛔ 没把 `e4` 换类 —— 换类会连带打红 D5/D6）。
- 三种改坏法都会被这条抓到：① 该支改成「`mainRankedType == 接口` ⇒ 返回 null」⇒ 得 null ≠ e4 红；② 改成「拿不到配置也落回 LINQ」⇒ D8 红；③ 真去递归 ⇒ 同一类型无限递归 ⇒ 进程栈溢出（比红更响）。
- **顺带**：末尾合计行原来是 `结果：pass=N fail=M`（不是全仓通行的 `=== 断言合计：N 通过 / M 失败 ===` 格式，所以全量日志里 grep 不到、看着像没跑）⇒ 就地换成标准格式（同一行、零行数变化）。

## 四、读数

| 项 | 改前 | 改后 |
|---|---|---|
| `GameModesCheck.Run` | **71 通过 / 1 失败**（红 = D10） | **72 通过 / 0 失败** |
| `typecheck.sh`（`TMPDIR=/tmp/wf_B89GM`） | — | 运行时 **0** / 编辑器 **0** |
| 日志 | `_tmp_view/gamemodes.log` | `_tmp_view/gamemodes_B89GM.log:1557`（D10 ✓）·`:1933`（合计行） |

## 五、红线遵守

⛔ 未改 `账/*.tsv` 主表 · ⛔ 未动 git / `d:/2` · `GameModesHandler.cs` **零改动**（账行 217/218/219 的落点 `:228/:263/:301` 全部不动）。
`GameModesCheck.cs` 被 `判据/功能点索引_轮抽_part.tsv` 引用的 `:286`（F1 ★灭自证那行）**行号未移位**（文件仍 493 行，CRLF/裸 CR = 0/0）。
