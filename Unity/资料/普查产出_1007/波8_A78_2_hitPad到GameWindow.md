# 波8 · A78② —— `hitPad` / 软边的状态搬到 `GameWindow`

> 执行代理报告（全权限）。工程 = `d:/4/Unity/MyGame`。
> 判据 → `资料/待办判据_1006.md` §A78② · 原始出处 `资料/普查产出_1004/W6审查_共用件.md`。
> 白名单原写的是 `Shell/GameWindow.cs` —— **该文件不存在**（本工程有过这种先例）；
> `GameWindow` 类实际住在 **`Shell/WindowsManager.cs:70` 起**，改动就落在那里。⚠️ 全工程没有第二个 `GameWindow` 声明。

---

## 1. 结论（四条验收逐条）

| # | 验收标准 | 结论 |
|---|---|---|
| ① | `GameWindow` 族（非 `MenuWindowBase` 家族）的任意一扇窗**能喂到非零 `hitPad`（以及软边 `clipSoftness`）**，机制在**共同基类**上 / 等价的一份共享实现，⛔ 不是每扇窗各抄一段 | ✅ **达成**。`Clip` / `ClipSoftness` / `ClipPad` 三兄弟 + 两份转发（`RenderClip` / `AddHit`）+ 两份渲染转发（`DrawRect` / `DrawNine`）**上移到 `GameWindow` 并只声明一份**（`Shell/WindowsManager.cs:95-282`）；`MenuWindowBase` 里那三份声明**整段删掉**（只留指针注释，`Shell/MenuWindowBase.cs:74-82`），不留第二份。 |
| ② | **零行为变化**（现存窗口的渲染 / 命中结果一字不变，用既有断言证明） | ✅ **达成**（逐条静态对账，见 §2.3；既有断言清单见 §2.4 —— **自检由调度台在同步点跑**，本代理按铁律 12 不跑 Unity）。 |
| ③ | 补一条**真能区分两种状态**的断言：造一个 `GameWindow` 族（非该家族）的窗、喂非零 `hitPad`，断言命中区真的变了 | ✅ **达成**：`Editor/ShellScene.cs:1475-1624` 新开一节 **⑤·i**（旧断言一条没动）。结构判据（反射问**声明处**）+ 行为判据（拿一扇真窗 `PromptPopup : GameWindow`，五态 pad + 两态软边，期望值全部由**原版字面量 + 探针字面量**算出）。 |
| ④ | 若「搬家会改行为」⇒ 停手写报告 | 🟡 **不适用**：逐条核过没有一处行为变化（§2.3），所以做了；**但有一处语义边界我如实记下来没动**（`ClipPad` 非零而 `Clip == null` 时照样缩命中区 —— 见 §3 第 1 条）。 |

---

## 2. 证据

### 2.1 改了什么（结构）

**`Shell/WindowsManager.cs`**（`class GameWindow`，`:70` 起）：新增一段 `====== 裁切状态：Clip / ClipSoftness / ClipPad ======`（`:95` 起），含

| 成员 | 行 |
|---|---|
| `public PxRect? Clip;` | `:132` |
| `public Vector2 ClipSoftness;` | `:192` |
| `public Vector4 ClipPad;` | `:217` |
| `public PxRect? RenderClip { get { return MenuDraw.PaddedClip(Clip, ClipPad); } }` | `:230` |
| `public ImageQuad DrawRect(...)`（→ `MenuDraw.Rect`，收 `RenderClip` + `ClipSoftness`） | `:240` |
| `public GameObject DrawNine(...)`（→ `MenuDraw.Nine`，同上） | `:248` |
| `public Transform AddHit(...)`（→ `MenuDraw.Hit`，转发**裸 `Clip` + `ClipPad`**） | `:277` |

**`Shell/MenuWindowBase.cs`**：三份声明 + `RenderClip` + `AddHit` **删除**，原处留一段指针注释（`:74-82`）；`Rect(...)` / `Nine(...)` 的转发收口到基类的 `DrawRect` / `DrawNine`（`:191` / `:217`，参数表逐一对应、**逐字同值**）；`AddHit` 原位留指针（`:221-224`）。

- 🔴 **`AddHit` 是「上移」不是「改写」**：签名逐字未动
  （`AddHit(Transform parent, string name, PxRect r, int q, System.Action onClick, ImageQuad target = null, string art = null, string hoverArt = null, string pressedArt = null)`）
  ⇒ `MenuWindowBase` 家族与各页（`CollectionWindow` / `ForgeTab` / `CampaignTab` / `MissionsTab` 的 `_win.AddHit(...)` 与不限定名调用）**全部解析到同一份实现**。
- 🔴 **没有引入任何新的「同一条规则两处写」**：`PaddedClip` / `PaddedHitRect` 仍是 `MenuDraw` 那一份唯一实现；`DrawRect` / `DrawNine` 只是把「本窗状态 → 那两个形参」这一句收口。

### 2.2 新断言（⑤·i，`Editor/ShellScene.cs:1475-1624`）

判据纪律：期望值**全部**是原版字面量或由它们算出的算式结果，⛔ 不读被测实现里的数。

**① 结构判据**（反射问**声明处**，`DeclaredOnly`）：
```csharp
foreach (var fld in new[] { "Clip", "ClipSoftness", "ClipPad" })
{
    CheckTrue(typeof(GameWindow).GetField(fld, Decl) != null,
              $"★ `GameWindow` **自己声明**了 `{fld}`（判据原文：「把状态挪到 `GameWindow`」）");
    CheckTrue(typeof(MainMenuSubmenuWindow).GetField(fld, Decl) == null,
              $"★ …而 `MainMenuSubmenuWindow` **没有**自己的副本（「每族/每窗各抄一段」⇒ 这条立刻红）");
}
CheckTrue(typeof(GameWindow).GetProperty("RenderClip", Decl) != null
          && typeof(GameWindow).GetMethod("AddHit", Decl) != null, "★ 两份转发也在基类上 ……");
// 全族扫一遍：每一个具体子类都继承得到 + 谁也不许再声明一份
foreach (var t in typeof(GameWindow).Assembly.GetTypes())
{
    if (!typeof(GameWindow).IsAssignableFrom(t) || t.IsAbstract) continue;
    nWin++;
    if (t.GetField("Clip", Inher) == null || … ) nBad++;
    if (t != typeof(GameWindow) && (t.GetField("Clip", Decl) != null || …)) nOwn++;
}
CheckTrue(nWin >= 20, "（前提）具体 `GameWindow` 子类 ≥ 20 个");
Check(nBad, 0, "★ 每一个 `GameWindow` 子类**都继承得到**那三样");
Check(nOwn, 0, "★ **没有任何**子类再声明一份（= 「每扇窗各抄一段」的判据）");
```
> 实点：本程序集里具体 `GameWindow` 子类 30+ 个（`PromptPopup` · `SettingsWindow` · `LeaderboardWindow` · `SkirmishEventWindow` · `CollectionWindow` … 逐个过）。

**② 行为判据 —— 真命中区（五态）**：探针窗 = `PromptPopup.Create(shell.Windows, …)`（**`GameWindow` 族，非 `MenuWindowBase`**），
探针矩形 `(100,200)-(300,400)`，两档 pad 都是**原版 mask 实读值**：

| 态 | 喂什么 | 断言（命中区 quad 的渲染矩形，`QuadPxRect`，容差 0.5px） |
|---|---|---|
| ① 基线 | `ClipPad = 0` | 100 / 200 / 300 / 400 **一字不动** |
| ② 缩小 | `(10,0,0,0)`（锻造轨道 `…/Forge Tab/Rewards Scroll View/Viewport` 实读） | 左沿 **110**；其余三边不动 |
| ③ 扩大 | `(−8,−5,−8,−5)`（输入框 `/Text Area` 那一族 ×38 实读） | **92 / 195 / 308 / 405** |
| ④ 反向 | `ClipPad = 0` | 同一条 `AddHit` 回 **100**（②③ 那几 px 位移**真由 pad 引起**） |
| ⑤ 三件套 | `Clip` 左沿 150（> 命中区 100） | 命中区**截到 150**、右沿仍 300（`Clip` 也在基类上） |

**③ 行为判据 —— 软边（两态）**：同一扇窗走 `DrawRect`（`Clip` = 探针视口 `(1000,200)-(1600,800)`）：
- 喂 `ClipSoftness = (0,22)`（原版 `Chat Tab/Viewport` 的 `m_Softness` 实读值）⇒ `CheckSoftCuts` 切线必须落在
  **200+22 = 222** / **800−22 = 778**；且**一条竖切线都没有**（`x = 0` ⇒ 左右硬边）；
- 清 0 ⇒ **一条切线都没有**（两态合起来才证明那两条切线真由软边引起）；
- `MenuDraw.SoftEdgeUvDrifts` 不涨。

改坏法（会红的具体形状）：
- 把 `AddHit` 里的 `ClipPad` 去掉（只转发 `Clip`）⇒ ②③ 四条全红；
- 把 `DrawRect` 里的 `ClipSoftness` 去掉 ⇒ ⑤·i 的 `CheckSoftCuts` 报「**空表 = 这条软边没接上**」；
- 把三兄弟搬回 `MainMenuSubmenuWindow`（或给某一扇窗再抄一份）⇒ ① 的结构判据红（且 `DrawRect` / `AddHit` 会直接编不过）。

### 2.3 「零行为变化」怎么证的（静态逐条）

1. **声明处上移，读法不变**：三兄弟的名字 / 类型 / 访问级（都 `public`）一字未改，只是**声明类**从 `MainMenuSubmenuWindow` 换成 `GameWindow` ⇒ 所有 `_win.ClipPad` / `ClipSoftness` / `Clip` / `RenderClip` 靠继承解析到**同一份**（`ForgeTab` · `CampaignTab` · `AvatarTab` · `CollectionWindow` · `Editor/{RewardsScene,ShellScene}.cs` 等，全工程 `grep` 过）。
2. **`AddHit` 上移**：签名逐字相同 ⇒ 调用点绑定不变。
3. **`Rect` / `Nine` 改走 `DrawRect` / `DrawNine`**：实参表**逐项相同**
   （`parent, tex, new PxRect(x1,y1,x2,y2), name, q, tint, keepAspect && art != null, RenderClip, ClipSoftness` / Nine 那一条含 `texW/texH` 兜底与 `borderOutPx`）⇒ 转发结果完全一致。
4. **没有发生「遮蔽 / 隐藏」**：全 `Assets/CardPresentation/` 逐个核过 —— 在今天之前，**没有任何 `GameWindow` 子类**声明过 `Clip` / `ClipSoftness` / `ClipPad` / `RenderClip` / `AddHit`
   （同名声明只有：`MenuWindowBase`（本次删除）· `ProfilePage.Clip` / `ProfilePage.ClipSoftness` · `SocialPage.Clip` · `ItemDrawerStyle.Clip` · `RowCtx.Clip` —— 后四个**都不是** `GameWindow` 的派生类（`WindowTabBase : MonoBehaviour` / 普通 class / struct），互不影响）。
5. **序列化面**：`Clip` 是 `PxRect?`，而 `PxRect`（`Core/UguiRect.cs:27`）**没有 `[System.Serializable]`** ⇒ Unity 从来不序列化它；`ClipSoftness` / `ClipPad` 虽然可序列化，但**建场景时全是零值**、且 Unity 按**字段名**匹配（基类字段照样读得到）⇒ 场景 YAML 不因这次搬家改义。
6. **语义面**：`RenderedClip` / `PaddedHitRect` 的**算式一个字没改**（都在 `MenuDraw` 那一份里），搬的只是「谁持有这份状态」。

### 2.4 既有断言（调度台在同步点跑；本代理按铁律 12 不跑 Unity）

本件动的是**共用基类**（`grep` 谁在用 ⇒ 下面这几条直接引用过这三兄弟）：

| 自检 | 证据 |
|---|---|
| `ShellScene.Run` | ⑤·b（`:776-868`）：`padWin.RenderClip == (1010,200,1600,800)` + 横跨框沿的图停在 **1010** + 反向清 pad 回 **1000**；⑤·c / ⑤·f / ⑤·h：软边切线 |
| `RewardsScene.Run` | `:1511,1513`（锻造轨道命中宽 190.762 = 原版 200.762 − pad.L 10）· `:1520`（`PaddedHitDegenerates == 0`）· `:2817`（战役轨道命中区不许被缩） |
| `CollectionScene.Run` | Deck 格命中区位置（`:505-513` 那一族）|
| `MainMenuScene.Run` | 5 处引用（`ClipSoftness` 相关注释与断言）|

⚠️ **Battle 侧 / Deck 侧零依赖**：`grep GameWindow / MenuWindowBase / ClipPad` 在 `Battle/` `Deck/` `Core/` 下**只命中注释**，没有代码引用 ⇒ `BattleScene.Run` / `DeckScene.Run` 不受本件影响。
**建议复跑清单**（共用件 + 跨到「所有 shell 窗」）：`ShellScene` · `MainMenuScene` · `RewardsScene` · `CollectionScene` · `ShopScene` · `SettingsScene`。
⚠️ 后三条里 `SettingsScene` / `MainMenuScene` 正被别的写手动（**跑之前先确认他们那批已经落定**，否则红了分不清是谁的）。

### 2.5 原版把这类状态放在哪一层（本件顺手核过一遍）

- **不是「窗口的字段」**，而是**每个视口节点自己挂的 `RectMask2D` 组件**（`m_Padding` / `m_Softness` / `m_Enabled` 三样都在组件上）。
  · 本轮实读（现读现核）：`d:/2/新解包资源/assets_full/bundle_mainmenualwaysloaded_assets_all/MonoBehaviour/MonoBehaviour_-7904774033703794794.json` = `m_Softness {"x":0,"y":22}` + `m_Padding` + `m_Enabled` 都在 —— 这就是挂在 `Chat Tab/Viewport` 上的那一个 mask。
  · ⚠️ `RectMask2D` 是 **Unity/uGUI 内置组件**（不在原版自己的 `Assembly-CSharp` 里）⇒ 「第一权威」在这里是**解包资源（组件实例）+ 本工程 `PackageCache` 里的 uGUI 源码**，不是 `d:/2/tools/decomp_full/`。
  · uGUI 源码本轮现读：`Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/RectMask2D.cs:51`（`m_Padding` 字段）+ `Culling/Clipping.cs:25-30`（`xMin + offset.x` / `xMax − offset.z` / … ⇒ **渲染那一面也读 padding**）。
- ⇒ **推论**（本件采用）：既然原版那份状态跟着**视口节点**走、而任何一扇窗里都可能有视口 ⇒ 我们的等价物**必须能挂在任意一扇窗上** —— 这正是它属于 `GameWindow` 而不是某个子家族的理由。

---

## 3. 没查清的部分（⛔ 没猜，如实列）

1. **`ClipPad` 非零而 `Clip == null` 时，命中区照样被缩** —— 这条既有语义**与原版模型不同**（原版 padding 长在 mask 组件上，**没有 mask 就没有 padding**）。今天生产上**没有这种站点**（唯一非零的 `ForgeTab` 与 `Clip` 成对设置），我**没改**它（改它就是改行为，属验收 ④ 的停手条件）。⇒ 留作待裁：要不要把 `MenuDraw.Hit` / `AddHit` 改成「无 `Clip` ⇒ 不缩」。
2. **原版是「逐节点」的 mask，而我们是「逐窗一份」状态** —— 同一扇窗里两个视口 pad / 软边不同时，这一套表达不了（今天是「谁设谁还原」的纪律在兜）。已写进 `GameWindow.ClipPad` / `ClipSoftness` 的注释里标成「还没查清的那半」。要彻底对齐得让状态**可入栈**（不是再抄字段）—— 本件没做。
3. **`Deck Cell` 那条 `hitPad` 没有新转发口** —— `MenuDraw.DeckCell(..., hitPad)` 是 public static，任何窗都能传；本件只是把**状态**搬上基类，调用点要写 `hitPad: ClipPad`（一行）。**没有**给它加 `GameWindow` 包装（`clip == null` 与「用本窗 Clip」两种意图分不开，硬包会改语义）⇒ 如果调度台认为该有，请另开一件定口径。
4. §2.4 那张表里的三条自检**本代理没跑**（铁律 12：Unity 批处理只有主对话能跑）。

---

## 4. 改动清单

| 文件 | `git diff --numstat` | 说明 |
|---|---|---|
| `Unity/MyGame/Assets/CardPresentation/Shell/WindowsManager.cs` | **+189 / −0**（本件开始前是干净的 ⇒ 这个数**全是本件**） | `GameWindow` 新增裁切状态 + 4 个转发成员 |
| `Unity/MyGame/Assets/CardPresentation/Shell/MenuWindowBase.cs` | +32 / −122（**该文件开工前已被 A9/A140 等改动过** ⇒ 这个数含别人的量） | 删三份声明 + `RenderClip` + `AddHit`，留指针；`Rect` / `Nine` 转调基类 |
| `Unity/MyGame/Assets/CardPresentation/Editor/ShellScene.cs` | +378 / −8（同前：**开工前已是 M**，−8 那几行是 A140② 的在飞改动，不是本件删的） | 新开 ⑤·i 一节（`:1475-1624`），别人的断言一条没动 |
| `Unity/资料/普查产出_1007/波8_A78_2_hitPad到GameWindow.md` | 新增（本报告） | —— |

**验证过的**：
- `TMPDIR=/tmp/wf_w8j bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（首跑时运行时那遍报的是 `Shell/PracticeModePopup.cs(845,87) CS0103 DeckNameH` —— 那是 A77 批四正在写的**半成品**，不是本件的错；隔一会儿重跑即 0/0）。
- 三个文件**行尾都是纯 LF**（`CRLF=0`；`git diff --numstat` 数字接近改动量、没有整篇翻行）。

**没跑**：Unity 批处理（铁律 12 —— 由调度台在同步点按 §2.4 的清单跑）。

---

## 5. 顺手发现（⛔ 本代理一个字没改 —— 交给调度台分流）

**A. 三处注释已过期**（都写「三兄弟**只长在 `MenuWindowBase` 上** / 够不着」—— 本件之后**不成立**了；本件的裁定是「够得着了，但**仍走逐件传**」，属 A78① 的口径，不是回归）：

| 文件:行 | 现在写的是 | 该改成 |
|---|---|---|
| `Shell/ChatPanel.cs:417-419` | 「`ChatPanel : GameWindowWithTabs` ⇒ 那三兄弟**一个都够不着**（它们只长在 `MenuWindowBase` 上）」 | 「2026-10-07（A78②）起三兄弟长在 `GameWindow` 上 ⇒ **够得着**；但本窗**仍走逐件传**（A78① 的裁定）」 |
| `Shell/LiveOpsEventWindow.cs:130` 与 `:619` | 同上（「本窗够不着 `MenuWindowBase.ClipSoftness`」） | 同上 |
| `Editor/ShellScene.cs:1299`（⑤·f）与 `:1421`（⑤·h） | 「三兄弟**只长在 `MenuWindowBase` 上** / ⇒ 够不着」 | 同上（这两段是**别人的节**，简报明令「一条都不许动」⇒ 我没碰） |

**B. `MenuWindowBase.Nine` 的注释里有一句仍然成立但要留意**：`:202`「而**只有本层有 `Clip`**（各页拿不到）」—— 今天指「各**页**（`WindowTabBase` 家族）不是 `GameWindow` ⇒ 拿不到窗上的字段」，**成立**（页仍然只该走 `_win.Nine`）；别把它误读成「只有 MenuWindowBase 家族够得着」。

**C. 没有发现别的缺陷**：本件动过的三处没有引入静默失败（`PaddedClip` / `PaddedHitRect` 的退化告警一个都没变），也没有新增 TODO。
