# W16 · CardDisplayWindow 命中改回 UI Collider

## 一、改了什么（文件:搜串 · 改前 → 改后）

| # | 文件 · 搜串 | 改前 | 改后 |
|---|---|---|---|
| 1 | `Core/CardView.cs` · 搜 `第二套矩形：原版 uGUI` | 无（`W13` 只留 `BoxCollider2D` 那套） | 新增常量 `UiColliderHalfW = (Width − 0.2) ÷ 2 = 0.94635` · `UiColliderHalfH = (Height − 0.44) ÷ 2 = 1.44565` · `UiColliderCenterDy = −0.02`（卡局部单位） |
| 2 | `Core/CardView.cs` · 搜 `public bool ContainsUiCollider` | 无 | 新增独立只读口：`HitFrame().InverseTransformPoint(world)` 后判 `abs(x) ≤ UiColliderHalfW ∧ abs(y − UiColliderCenterDy) ≤ UiColliderHalfH` |
| 3 | `Battle/CardDisplayWindow.cs` · 搜 `if (v.Contains(world))` | `v.Contains(world)`（= `BoxCollider2D` 主面 `1.97×3.02 @ 0`） | `v.ContainsUiCollider(world)`（= `UI Collider` `1.8927×2.8913 @ y −0.02`） |
| 4 | `Battle/CardDisplayWindow.cs` · 文件头 `EW12` 那行 + `HitSlot` 的 doc | 写「矩形 = `BoxCollider2D` 主面」「`R5`：`UI Collider` 三事件全空 ⇒ 不是送达路径」 | 改为「矩形 = `UI Collider`」＋就地订正：`UI Collider` 是展示窗的送达路径（依据见下），漏掉的是**运行时 `AddListener`** |

**判据（本轮亲自复读原件，非转述）**：
- `d:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/RectTransform/RectTransform_-5674508438467339328.json`（`UI Collider`）：
  `m_AnchorMin=(0,0)` · `m_AnchorMax=(1,1)` · `m_AnchoredPosition=(0, −0.01999998)` ·
  `m_SizeDelta=(−0.19999999, −0.43999997)` · `m_Pivot=(0.5,0.5)` · `m_LocalRotation` 恒等 · `m_LocalScale=1` ·
  `m_Father.m_PathID = −8834377289179948096`。
- 父件 `RectTransform_-8834377289179948096.json`（`2DCard`）：`m_SizeDelta = 2.0927000045776367 × 3.3313000202178955`。
- ⇒ 在 `2DCard` 局部系里 rect = **1.8927 × 2.8913、中心 (0, −0.02)**（拉伸锚：尺寸 = 父尺寸 + sizeDelta）。
- 送达路径（简报给定、只读诊断已现核）：`BasicCardUI.uiEventCatcher`（`+0xd0`）→ 该节点
  （`BasicCardUI__get_UIEventCatcher.c:5`）· `CardDisplayWindow.AddCardsListeners.c:25/30` 往其 `OnClick` `AddListener` ·
  该节点 `Image.raycastTarget = 1` · `UICollider.OnEnable` 把 `BoxCollider2D.size` 设成 `RT.rect`。
- 复算：前台 scale 250 时四边应为 `723.4125 / 123.5875 / 1196.5875 / 846.4125`（设计 px、y 向下），
  与 `D_新红11条诊断.md` A1 记的断言期望 `723.412 / 123.587 / 1196.588 / 846.413` 逐位吻合。

## 二、为什么「跟卡旋转」没丢（怎么保证的）

- 新口与 `Contains` **用同一个标架函数** `HitFrame()`：它返回内层 `2DCard`（`_tiltRoot`），
  取不到才退回卡根并出声一次。
- 两处旋转都写在这条父链上，因此**同时生效**：扇形角由 `SetPose` 写在**卡根**；倾摆由
  `AutoCardRotation` 写在 `_tiltRoot`（后者是前者的子件）。
- `UI Collider` 原件相对 `2DCard` 是恒等旋转 / 单位缩放（上面 JSON 已核）⇒ 在 `_tiltRoot`
  局部系里量这个矩形是**精确等价**，不是近似。
- 落点：`CardDisplayWindow.HitSlot` 只换了被判的矩形，标架与 `EW12` 完全相同。

## 三、`Contains` 本身没动（证据）

- `Contains` 方法体、`HitHalfW / HitHalfH / HitCenterDy / HitHalfHInHand / HitCenterDyInHand`、
  `HitRect(...)`、`ContainsNear(...)` **一行未改**（我的两次 Edit 均在其外插块）。
- `Battle/CardDisplayWindow.cs` 里 `Contains(` 的调用 **0 处**（grep 只命中新口
  `ContainsUiCollider`，:864）；`ContainsPointer` 经 `HitSlot` 间接走新口，这是预期。
- ⚠️ 如实说明：`git diff` 对 `HEAD` 会同时显示 `W5`/`W13`/`W14` 更早的未提交改动，
  没法只凭它区分「本件改动」；本件的改动就是上表 4 条。
- 断言：`Editor/BattleScene.cs` 未打开、未改动（A1–A6 的窄带判别式原样）。

## 四、类型检查结果

`TMPDIR=/tmp/wf_w16 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时程序集 0 错 / 编辑器程序集 0 错**。

## 五、没能判的

- **A1–A6 是否转绿未验**：按本轮口径不跑 Unity 自检，也不跑 `BattleScene.Run`。
- **运行时几何未实测**：`_tiltRoot` 的实际 lossyScale / 父链沿用 `EW9`/`EW12` 的结论，本轮没有跑起来量。
- **开窗动画期间的命中**（`PlayOpenAnimation` 每帧 `SetPose`）未实机验证；标架路径与 `EW12` 相同 ⇒ 推断仍跟随。
- **展示窗 `CardView` 的 `_faceMode` 取值未查**：对本口无影响（`UI Collider` 没有 in-hand 档，
  `colliderHeightMultiplierInHand` 属于 `2DCard` 上那颗 `BoxCollider2D`）。
- **`Contains` 那套（手牌/棋盘）本轮行为未回归**：没有跑任何宿主。

## 六、顺手发现（⛔ 没动手）

1. `Core/CardFan.cs:151` 仍写着「碰撞按**未旋转**的矩形判（同原版：吃射线的是这颗矩形本身，
   与卡自己的转角无关）」—— 对展示窗已被 `R5`/`W16` 推翻；该文件不在白名单，未改。
2. `Core/CardView.cs`「可点矩形」段末引用的 `CardDisplayWindow.cs:874` 是 `HitSlotNoView` 的旧行号，
   本件插入后已偏移（现约 :889）。同一段的 `CardFan` 旧矩形「仍是展示窗退路」的措辞仍成立，只是行号过期。
3. `CardView.HitFrame()` 的告警文案只提「两颗 `BoxCollider2D` 挂在 `2DCard` 上」；现在它也是
   `ContainsUiCollider` 的标架（`UI Collider` 同样挂 `2DCard`）⇒ 结论仍真，措辞可补一句。
4. `资料/普查产出_1017/BSC62/R5_T09卡体②③与剩余清单.md` 里「`UI Collider` 三事件全空 ⇒ 不是送达路径」
   仍是旧判定，需由调度台在正本里就地订正（本写手不碰 `资料/*.md` 除本报告）。
5. `HitSlotNoView`（`CardView` 没建出来那一格的退路）仍用 `CardFan` 的**轴对齐**矩形 ——
   其矩形数值与 `UI Collider` 同源、只是不跟旋转；现在与主路径的差别只剩「旋转」，口径更干净了（仅记录）。
