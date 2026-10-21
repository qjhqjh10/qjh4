# W62 · `DeckScene` 自检夹具修复（执行代理 X · 2026-10-22）

> 输入 = `资料/普查产出_1022/W62_预组名与演示卡组修复.md` §②③（代理 U 的行为改动）。
> 白名单内动手；**未跑 Unity、未动 git、未改正本**；`DeckRuntime.cs` 一个字节没碰。

---

## 一、结论

**新不变式（= 本笔的牙齿）**：**打开卡组编辑器 ⇒ 一个字节都不写存档；按 `Done`（合法）⇒ 才入库 + 落盘。**
夹具侧从「**依赖生产替你建库**」改成「**夹具自己显式建一套**」，而且**走的就是生产那条 `Done` 路**
（`DeckRuntime.CommitDeck` → `DeckLibrary.Insert` = 原版 `UploadUnsyncedDeck`）——
所以夹具本身也在验这条行为，不是另开一条只有自检用的旁路。

改了四处：

| # | 位置 | 改了什么 |
|---|---|---|
| 1 | `DeckScene.cs:812-817`（`RunCore`） | 明确写死「这一拍 `Build` 的前提 = **库空 + 存档不存在**」，⛔ 不许在它之前预建卡组 |
| 2 | `DeckScene.cs:827-832` + `:843-849` | **调用顺序**：`TestLibraryWiring()`（断空库那一拍）→ `TestLibraryAfterDone()`（建夹具）→ **然后**才抓 `origDeckName/origLibIndex/origLibCount`（收尾复位用）；原位置的 `TestLibraryWiring();` 删掉、留指针 |
| 3 | `DeckScene.cs:2516-2554`（`TestLibraryWiring` 重写） | 旧三条（「空库时会建一套 / 有选中 / 建出来的不是空的」）断的是**我们自己的旧副作用** ⇒ 换成 8 条**新不变式**断言（见 §二） |
| 4 | `DeckScene.cs:2556-2612`（新 `TestLibraryAfterDone`） | 16 条：**按 `Done` 才入库/才有文件** · **库里已有卡组时再开一次编辑器 mtime 不变** · 原来那串 `Create/reload/Delete` 落盘往返（**挪到夹具这套上**） |

**`:6756` 与 A397 那一节（`:7090` 起）一行断言都没改** —— 它们要的是「库里有一套」这个**前提**，
夹具把那前提还回来了（`_rt.Library.CurrentIndex == 0`、`Current != null`），断言正文原样保留。

**用户可感知的行为差异（本笔的总目标）**：改前「一进卡组编辑，玩家存档里就多出一副演示卡组『复仇者之刃』
（还会把默认名覆盖掉）」；改后「进编辑器什么都不写，按 `Done` 才出现那一副」。⇒ 全新玩家的卡组库
**真的是 0 套**（原版靠 FTUE 发牌，我们那条链还没接 —— 见 §五·4）。

---

## 二、新不变式逐条（`TestLibraryWiring`，8 条）

> 只在**空库那一拍**调用一次（`RunCore` 里**必须赶在建夹具之前**）。
> 🧨 改坏法：`DeckRuntime.Build` 里加回 `Library.Create` + `CommitCurrent` ⇒ ①②③④⑤ 红。

| # | 断言 | 判据 |
|---|---|---|
| ① | `DeckLibrary.Load().Count == 0` | 原版 `SelectDecksTab__CreateDeck.c:17,26,28` —— 空库只开窗 |
| ② | `Load().Current == null` | 同上（空库没有「当前那套」） |
| ③ | **`WarpforgeDecks.json` 不存在**（一个字节都没写） | 把 U 查到的副作用钉死（改前 Birth=00:09:26） |
| ④ | `_rt.Library.Count == 0` | **运行期**那一份与盘上一致（不是「只在盘上没写」） |
| ⑤ | `_rt.Library.Current == null` | 同上 |
| ⑥ | `_rt.State.Deck` **非空**（有牌或有督军） | **另一半**：缺了它，「不写盘」可以靠「干脆什么都不建」蒙过去 |
| ⑦ | `State.Deck` **不是** `Library.Current` 那个对象 | A397 那条不变式（`LoadDeck` 装的是副本） |
| ⑧ | `State.DeckCount == DeckRules.ClassicCards`（30） | ⚠️ **特意不断「`DeckCount == Deck.CardIds.Count`」** —— 那是同一个表达式的同义反复（`DeckState.DeckCount` 就是 `CardIds.Count`），零鉴别力 |

## 三、`TestLibraryAfterDone`（16 条，并负责建夹具）

| 组 | 断言 | 说明 |
|---|---|---|
| 前提（3） | 文件仍不存在 · 库 0 套 · 演示牌 `Validate() == None` | 最后一条是「下面按 `Done` 会放行」的前提（原版 `__TrySaveDeck.c:80` 那道闸） |
| ①（4） | 按 `Done`（`UiPressDone()`）⇒ `Library.Count == 1` · **这时才** `File.Exists` · 重读存档 1 套 · 盘上名字 == 编辑器那份 | `Library.Insert` = 原版 `UploadUnsyncedDeck` |
| ②（3） | 记 `mtime` → **再开一次编辑器**（新 `DeckRuntime.Build`）→ **mtime 一字未变** · 库没多没少 | 这就是「打开不写」的**第二面**（库非空那一档）；借的也是生产入口 |
| ③（6） | `Create("第二套")` ⇒ 内存 +1 / 重读 +1 / 选中项落盘 / `_rt.Library.Delete()` 删回 ⇒ 盘上 -1 / 当前回到夹具那套 | 原 `TestLibraryWiring` 的落盘往返，**前提从「生产替你建的那套」换成夹具这套** |

**⚠️ 顺带修掉一处旧夹具的坑**：原来那段用的是 `lib2.Select(0); lib2.Delete(0);` —— `lib2` 是**从盘上重读**的
另一个对象，删完**盘上**少了一套、**`_rt.Library`（内存）里还在** ⇒ 内存与盘长期不一致（后面各节全受影响）。
现在改成用 `_rt.Library` 自己删（内存与盘一起回）。

---

## 四、主对话要跑的验证 + 期望

**跑**：`DeckScene.Run`（⚠️ 该宿主 `NewScene` + `SaveScene`，会**重写 `DeckEditor.unity`** —— 跑前留副本）。
**覆盖**：本笔只动了 `Editor/DeckScene.cs` 一个文件（不碰共用件、不碰引擎）⇒ 按铁律 12「只动一个宿主就只跑那一条」。

**期望通过数变化（这一段）**：

| | 断言数 |
|---|---|
| 改前 `TestLibraryWiring` | **8** |
| 改后 `TestLibraryWiring` | **8** |
| 改后新增 `TestLibraryAfterDone` | **+16** |
| **合计** | **8 → 24（净 +16）** |

**改前那条链是断的**：`TestLibraryWiring` 在 `lib.Current.CardIds` 上抛 `NullReferenceException`
⇒ **`RunCore` 从那里整轮中断**（后面的节一条都没跑），所以改前那次运行**根本不会给出有意义的合计**。
**改后期望**：整轮跑到底；`:6756`（A565 的 Discard 回滚对照）与 A397 那一节（原版「编辑的是副本」14 条）
**恢复前提、回到绿**（这两处的**断言正文一个字没改**）。

---

## 五、没查清 · 顺手发现（**只报不改**）

1. ⚠️ **没跑 Unity**（红线），所以「通过数」是按断言条数**静态**算的；实跑若有个别红，先看它是不是
   「演示牌合法性」那一族（见 2）。
2. **夹具依赖「演示牌 = 合法满编 30 张」**：`PlayerDeckForDemo` 按池序挑**第一个 hero** + 同阵营 cost 最低的
   defence + 30 张同阵营 unit。我按 `cards_engine.json`（1127 张）**静态复算了一遍**：
   warlord = `ASH1 Anvirr Keltoc`（SaimHann）· defensive = `ASH56 Inspiring Legacy`（cost 0）·
   30 张、**0 张被 `CannotAddToDeck` 拦**、**0 张超同名上限** ⇒ `RemoveInvalidCards` 摘 0 张、`Validate() == None`。
   ⚠️ 但这是**静态复算**，池子换号/改名之后这条前提会静默变脆 —— 所以在 `TestLibraryAfterDone` 里
   加了一条**出声的前提断言**（`Validate() == None`），前提一破就当场红，不会变成一串莫名其妙的级联红。
3. **`MenuDeck/DefaultDeckName`（「我的卡组」）今天没有生产消费点** —— 按调度台口径**保留词条 + 保留断言**，
   并在 `DeckScene.cs:1231-1234` 就地注明「原版键，当前无生产消费点 / A1025 钉的是『表里有这条键』」。
4. **起始卡组仍然没人发**（U 已报的 §六·4，本笔之后**更显眼**）：全新玩家库 = 0 套，而
   `Shell/FTUESelectDeckWindow.cs` 那一跳明写「做不到」（只出声 + 关窗）。⇒ 「新账号怎么拿到第一副牌」
   仍是一条**待接的账**（原版靠 FTUE，判据不冲突）。
5. **`DeckRuntime.cs:8543` 的注释**还写着「`MenuDeck/DefaultDeckName`（**空库时替玩家建的那套**）」
   —— 「空库时替玩家建的那套」这个前提**已随 U 的改动过期**；两键两值本身仍成立。
   ⛔ 不在本笔白名单（`Deck/**` 是 U 的地盘），**没动**，留指针给下一笔。
6. **`TestLibraryAfterDone` 会多建一棵编辑器 UI 树**（`W62ReopenSmoke`）再 `DestroyImmediate` ——
   与 `RunCore:857-862` 那条 `PlayEntrySmoke` 同一个套路。批处理下没有帧循环，销毁是即时的。

---

## 六、改动清单

| 文件 | 改了什么 | 行尾 |
|---|---|---|
| `d:/4/Unity/MyGame/Assets/CardPresentation/Editor/DeckScene.cs` | ① `RunCore` 顺序（`:812-817` · `:827-832` · `:843-849`）② `TestLibraryWiring` 重写（`:2516-2554`，8 条新不变式）③ 新增 `TestLibraryAfterDone`（`:2556-2612`，16 条）④ `MenuDeck/DefaultDeckName` 那条如实注（`:1231-1234`） | **CRLF 9454 / 9454** ✅（改前 9361/9361，未翻行尾） |

`git diff --numstat`（只列我的这一个受控文件）：

```
113  20  Unity/MyGame/Assets/CardPresentation/Editor/DeckScene.cs
```

（不是整篇重写 —— 行尾没翻。）

---

## 七、typecheck

```
TMPDIR=/tmp/wf_x23 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---   运行时错误数: 0
--- 编辑器程序集 ---   编辑器错误数: 0
```

（U 报告 §七 里那 2 个 `CS0103`（`CardBaseDemo.cs` 的 `TmpRowCenterY`）**已经没了** —— 那是代理 T 的活，
本轮现跑是 0/0。）
