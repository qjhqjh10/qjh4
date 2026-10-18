# W_red_E_MainMenu —— 批次 E 写手回执（14 条红）

> 范围 = `DIAG_red_E_MainMenu.md` 的 14 条（全部 δ 夹具前提，0 条实现缺陷）。
> ⛔ 没跑 Unity / `*.Run` / player；只跑了秒级类型检查。
> 白名单 = `Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs`（只改这一个文件）。

## §一 改了什么（文件:行）

**唯一改动**：`Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs:2914-2931`
（插在 ㉒④ 那个 `{}` 收口 `:2912` 之后、「文字吃软边」注释 `:2932` 之前 —— 18 行，其中 11 行是说明注释）

```csharp
pw.ArmyIndex = -1;
pw.RebuildDeckListForTest();          // 9 套 ⇒ 5 行 ⇒ 内容高 816 / MaxOffset 275.21
CheckNear(pw.DeckScrollContentH, 816f, 0.6f,
          "（夹具复位）阵营筛选清回「不限」⇒ 9 套 ⇒ 5 行 ⇒ **816**"
          + "（= 上面 ㉒④ 那一条的前提值；不复位 ⇒ 这里会是 657）");
pw.ArmyScroll.OnChanged();            // `Highlight` 回到「一格都不亮」（同 :2478）
```

- 依据 = 诊断 §二 推荐修法：`PracticeModePopup.PickArmy` 已按原版
  `PracticeModePopup__ChangeSelectedArmy.c:30-33` 改成「同阵营再点 ⇒ 早返回」，
  而测试 `:2908-2910` 那次点击之后筛选**停在 `Factions()[0]`**，下面几节却全按「不过滤」写。
- `pw.ArmyIndex = -1` 是 public 字段直写（`Shell/PracticeModePopup.cs:773`），本文件已有先例（`:2472/:2478/:2493`）
  ⇒ 没有新增任何生产代码口。
- `pw.ArmyScroll.OnChanged()` 是诊断标「可选」的那句（只为 `Highlight` 回到「一格都不亮」、状态自洽）；
  写法照 `:2478`，触发的是 `RebuildArmyCells`（已在本测试里被调用过 4 次，同一条路）。
- **14 条全部由这一处复位转绿**（逐条依据见诊断 §一/§二）：
  #1–#4 滚 140 → `MaxOffset` 回到 275.21；#5–#7 恢复 4 套后 2 行 339 且 `DeckRow_0` 存在、`DeckIndex` 归 0；
  #8–#9 `DeckInfoPopup` 以 `DeckIndex=0` 开（4 张那套 ⇒ `Practice Deck` 不可点 ⇒ 不再开选卡组窗）；
  #10–#13 窗口栈干净（没有遗留 `Deck Selection Popup with Tabs`）；#14 `:3216` 那块重新执行 ⇒ `Select(0)`。
- **实现侧一字未改**：`Shell/PracticeModePopup.cs` 的同阵营早返回保持原样（诊断 §二 明令不许回滚）。
- ⛔ 没有把 `SetOffset(140f)` 改成 116.21、也没有把期望 276.54 改成 300.33（那会让软边断言失去鉴别力）。
- 代价：断言总数 **+1**（新增的 816 那条）。

## §二 复位后的预期验收点（照诊断 §三 / §四）

只动了 `Editor/MainMenuScene.cs` 一个宿主 ⇒ 复跑**只需要** `MainMenuScene.Run`。

1. 新增那条 `816` ✓，随后：滚 140 ✓、名字条中心 `276.54` ✓、在渐隐带内 ✓、`faded > 0` ✓（#1–#4）。
2. 恢复 4 套夹具后内容高 `339` ✓、`DeckRow_0` 有点击区 ✓、`DeckIndex=0` ✓（#5–#7）。
3. **「点第 2 套卡组 / 换到空卡组 ⇒ 0 行 / `Battle!` 如实拒绝 / 点回第 1 套」四条这次应重新出现**
   （本 run 因 `DeckRows.Count == 1` 被 `:3216` 的 `if` 整块静默跳过 —— 覆盖回归）。
4. `:13171`（4 张卡组 ⇒ `Practice Deck` 不可点）与 `:13437`（点它 ⇒ 什么都没发生）转绿；
   日志里**不应再出现** `[DeckInfo] Practice Deck ⇒ 开选卡组窗挑对手`。
5. 练习窗吸收层四条全绿（`:17892/:17924/:17940/:17993`）。
6. `:29636` 卡组名 = 「菜单测试卡组 1」。
7. ⚠️ `:3216` 那块**很久没被覆盖**（本 run 被跳过）；复跑时若那边冒新红，先按「覆盖回归」查，
   **不是**本修法引入的（诊断 §三末段同一口径）。

## §三 顺手发现（假绿是否改了）—— 未改

**没有改**，原因不是判断它会红，而是**它不在本批白名单里**：

- 那条判定的实体在 **`Shell/MenuDraw.cs:2770`**（公共件 `MenuDraw.CheckAbsorbRule` 的「面板矩形以内找得到一个点、
  它的命中是吸收层」）。`Editor/MainMenuScene.cs:73-79` 在 2026-10-16 已收口成**纯转调包装**
  （`MenuDraw.CheckAbsorbRule(CheckTrue, CheckNear, …)`），本文件里**已经没有**同族副本可改。
- 建议的强化（候选扫描里把 `hb != null && hb.absorbOnly` 收成
  `… && hb.transform.IsChildOf(winRoot) && MenuDraw.WasAbsorb(hb.transform)`，同 `:2799` 的口径）
  必须改 `Shell/MenuDraw.cs` —— ⛔ 该文件被 **5 个宿主共用**（MainMenu / Rewards / Collection / Settings / Shop），
  且不在我的文件所有权内 ⇒ 未动。
- **静态判断（未运行验证）**：根修（#1）之后窗口栈干净，那几个候选点命中的会是练习窗自己的 `AbsorbHit`
  （`IsChildOf(pw.transform)` ∧ `WasAbsorb` 都成立）⇒ **收严之后预期仍绿**，即不会因为实现侧有洞而变红；
  它当前只是「**在别的窗顶上来时分辨不出**」的弱断言（本 run 就是这样被遗留的
  `Deck Selection Popup with Tabs > AbsorbHit` 骗过的）。
- 建议：主对话把 `Shell/MenuDraw.cs:2770` 收严单独派给下一批（或并进公共件收口），
  并配一条改坏法（同矩形内叠一扇别家窗 ⇒ 该条必须红）。

## §四 typecheck

```
TMPDIR=/tmp/wf_redE bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

- 行尾：改后仍 **LF（`\r\n` = 0）**，与改前一致。
- `git diff` 本文件只多出 `:2914` 这一处插入（其余 hunk 是本会话之前就已在盘上的改动）。
