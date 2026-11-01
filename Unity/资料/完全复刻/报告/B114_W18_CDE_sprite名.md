# `B-248` · `card_display_effects.json` 的 `CdeImage` 补 `sprite`（原版 `Image.m_Sprite` 的**资产名**）

> 写手 `W-18` · 2026-11-01（**两轮**：第一轮做生成器 + JSON + 断言；第二轮 `W-17` 交回后，调度台把
> `Shell/CardDisplayEffects.cs` 划给本写手，补上 C# 读口）。**没跑 Unity**（`-executeMethod` 全禁）· **没动 git**。
> 改/建的文件只有四处：
>
> | 文件 | 动作 |
> |---|---|
> | `d:/4/Unity/工具/gen_mainmenu_effects.py` | 改（**+86 / −1**，`git diff --numstat`；行尾仍 LF：`CRLF=0 / 裸 CR=0 / 无 BOM`） |
> | `MyGame/Assets/CardPresentation/Resources/Art/card_display_effects.json` | **重生成**（只由生成器写；`Resources/Art/` 在 `.gitignore:99` ⇒ 盘上件，改动前后各留了一份副本在 `_tmp_view/b114_w18/`） |
> | `MyGame/Assets/CardPresentation/Shell/CardDisplayEffects.cs` | 改（第二轮，**+45 行**：644 → 689；行尾仍 LF）—— 只加 `sprite` / `hasSpriteName` 两列 + `SpriteFor`/`WarnSpriteFallback`，⛔ 没推翻 `W-8`/`W-17` 的任何一处（§3·5 逐条核） |
> | 新建 `MyGame/Assets/CardPresentation/Editor/B114_W18_Assert.cs`(+`.meta`) | 断言（274 行） |
>
> ⛔ **没碰**：`Editor/BattleScene.cs`（**接线仍由调度台统一追加**，见 §4·1）· `Core/ImageQuad.cs` ·
> `Shell/ItemDrawer*.cs` · `RuleEngine/**` · `工具/_sfx_meta.py` · `ArenaBuilder.cs` ·
> `D:/2/**`（**只读**）· 两份正本 · `账/*.tsv` · `Scenes/*.unity`。

---

## 〇 一句话

生成器现在把 `Image.m_Sprite` 那个 **PPtr 的资产名**读出来，写进 DTO 的**新列 `sprite`**（配套显式布尔 `hasSpriteName`）。
两条真条目解出来是 **`OctagonUI Filled SDF`（`Level Up Effect`）** 与 **`OctagonUI Border SDF`（`Level Up Effect Down`）**
—— 与 `W-8` 用 sprite guid 反查出来的一致（两条互相独立的路线，同一个答案）。
重生成后**旧列 0 处差异**、**`mainmenu_effects.json` 逐字节相同**。

---

## 一 新列怎么设计的（以及为什么 `JsonUtility` 安全）

### 1·1 两条新列（**追加在每个图件对象的末尾**）

```jsonc
   "ampFloat2W": 0.0,
   "sprite": "OctagonUI Filled SDF",     // ← 新
   "hasSpriteName": true                 // ← 新
```

| 列 | 类型 | 取值 |
|---|---|---|
| `sprite` | **`string`** | 原版 `Image.m_Sprite` 的**资产名**（逐字，含空格）。**原版没有 sprite ⇒ `""`**；**有 sprite 但名字没反查出来 ⇒ 也是 `""`**（靠下一列区分） |
| `hasSpriteName` | `bool` | = `sprite != ""`，即「这一件**拿到了**名字」 |

三档因此可以完全分开（这正是不用 `null` 的原因）：

| `hasSprite` | `hasSpriteName` | 含义 | 消费方该做什么 |
|---|---|---|---|
| `false` | `false` | 原版 `m_Sprite = 0` = **纯色块**（`Side Border*` 那两条） | 照旧画 `Solid()` |
| `true` | `true` | 是贴图、名字已知 | **按 `sprite` 取图** |
| `true` | `false` | 是贴图、名字没查到 | **出声 + 回退**（⛔ 不许静默） |

### 1·2 为什么**不写 `null`**、也**不省掉这个键**（`JsonUtility` 安全性的完整论证）

1. **`string` 是叶子类型，没有「物化成整棵子树」这回事。** 工程里那条坑
   （`JsonUtility` 把 `null` 物化成整棵子树、`!= null` 恒真）**只发生在自定义 `class` / `struct` 字段上** ——
   它用**无参构造**造一个实例再往里填。`string` 没有这个行为：JSON 里是 `null` 或**缺键**，C# 侧读出来都是 `null`。
   ⇒ 即使将来谁手改了 JSON 写成 `null`，也**不会**长出一棵子树。
2. **写 `""` 而不是 `null`，额外赚到一个「免依赖 null 语义」的判据。**
   缺键 ⇒ `null`；有键但没 sprite ⇒ `""`。两者**可区分**。
   而 `JsonUtility` 恰恰**分不清**「字段缺失」和「字段是 `null`」——
   ⇒ 要判「这份 JSON 有没有新列」本来就不能靠 `null`，但有了 `""` 这层区别，**至少不用把二者混为一谈**。
3. **真正的判据是显式布尔 `hasSpriteName`**，与工程里既有的 `hasGlow` / `hasSprite` / `hasAmp` 完全同一套路
   （`Shell/CardDisplayEffects.cs:522-533` 那三行的注释就写着「★ 不能靠 `xxx` 猜」）。
   「缺这一列」（= 旧文件）在 C# 侧的表现是 `hasSpriteName == false` ⇒ **自然落到「回退 + 出声」那一档**，
   不需要任何版本号。
4. **新列对现有代码零影响**：`CdeImage` 现在**还没有**这两个字段，
   `JsonUtility` 读到 JSON 里多出来的键是**直接忽略**的 ⇒ 重生成后的 JSON 立刻可用，不必等 DTO 那一步。

### 1·3 名字是**反查**出来的，不是从节点名抄的（这是整件事的关键）

`Image.m_Sprite` 给的是跨包 PPtr：`Level Up Effect` = `{m_FileID: 6, m_PathID: 1588741661991143589}`。
而**新解包 `assets_full` 的 `Sprite/` 是按资产名命名的**（`Sprite/OctagonUI Border SDF.json`）、
**文件名里没有 pathID** ⇒ 在新解包里**没有 pid→名 的反查入口**（`m_Externals` 那张表没被导出成文件）。

可用的是**旧解包** `d:/2/解包整理/**/Sprite/<资产名>_<pathID>.json` —— 文件名正好带 pid。
生成器新增 `sprite_name_of(pid)`：

1. 扫旧解包 `**/Sprite/` 的**文件名**建 `pid → (名字, 文件)` 索引（**只读文件名**，6,917 个条目，一次）；
2. 命中后读那一份 json 的 `m_Name`，**必须与文件名前缀逐字相同**，否则判失败；
3. 再拿**新解包**的全部 Sprite 资产名交叉验证（名字必须在新解包里也存在）；
4. 任一步不成立 ⇒ 返回 `("", 原因)` 并打印，**⛔ 不猜**（写进 JSON 就是 `""` + `hasSpriteName=false`）。

⚠️ **只用旧解包的元数据，⛔ 不碰它的 PNG**（旧那批图有 650 张与新版不同 —— CLAUDE.md 铁律 2）。

现读一次 `--apply` 的两行自证：

```
  sprite: Level Up Effect        pid=1588741661991143589  -> 'OctagonUI Filled SDF'  (ok)
  sprite: Level Up Effect Down   pid=8880629346401627699  -> 'OctagonUI Border SDF'  (ok)
```

（`ok` = 旧解包命中 + `m_Name` 一致 + 新解包里有同名资产，三重都过。）

---

## 二 重生成前后的逐条比对（**这是验收第 2 条**）

比对脚本：`_tmp_view/b114_w18/cmp.py`（产物 `cmp.out.txt`；**退出码 0**）· 副本：`_tmp_view/b114_w18/*.before.json`。

### 2·1 文件级

| | before | after |
|---|---|---|
| 字节 | 36,779 | 36,993（**+214**） |
| 行数（`\n`） | 2,199 | 2,207（**+8**） |
| 行尾 | `CRLF=0 / 裸 CR=0 / 无 BOM` | **同左**（没被翻） |
| 顶层键 | `note / particles / images` | **同左、同序** |
| 计数 | `particles 10 / images 4` | **同左** |

### 2·2 旧列逐条（值 + 类型 + 键序）

对两棵解析后的 JSON 树做**深度递归比对**（`dict` 逐键比类型与值、`list` 逐元素，含浮点精确值）：

> **旧列全部恒等 —— 0 处差异。**
> 唯一的「新增键」出现在 4 个 `$.images[i]` 对象的**末尾**：`['sprite', 'hasSpriteName']`。

### 2·3 行级 diff

```
~ old ['   "ampFloat2W": 0.0']
  new ['   "ampFloat2W": 0.0,', '   "sprite": "",', '   "hasSpriteName": false']
~ old ['   "ampFloat2W": 0.0']
  new ['   "ampFloat2W": 0.0,', '   "sprite": "",', '   "hasSpriteName": false']
~ old ['   "ampFloat2W": 0.0']
  new ['   "ampFloat2W": 0.0,', '   "sprite": "OctagonUI Filled SDF",', '   "hasSpriteName": true']
~ old ['   "ampFloat2W": 0.0']
  new ['   "ampFloat2W": 0.0,', '   "sprite": "OctagonUI Border SDF",', '   "hasSpriteName": true']
合计: 删除 0 行 / 替换 4 块（每块 1 行 → 3 行）
替换行的差异**只有行尾逗号**：是
```

⇒ **严格说「一个字节都不许变」有一处例外的物理形态**：每个图件对象**原先的最后一行**
（` "ampFloat2W": 0.0`）末尾**多了一个 `,`** —— 这是 JSON 语法要求（后面还有键），
去掉逗号再看，那一行**逐字相同**。除此之外：**0 删除、0 内容变化**，8 行全部是新增。

### 2·4 第二份产物也必须不变

`--apply` 同时重写 `Resources/Art/mainmenu_effects.json`：

> before 275,854 字节 / after 275,854 字节 ⇒ **逐字节相同**。

### 2·5 生成器的「不带显式参数就什么都不做」没被破坏

- `--help` ⇒ 只打印用法、`rc=0`；
- 不带参 ⇒ 真扫一遍打印预览，**md5 前后一致**（`61a0b4c9…` 两次相同），末行 `（本次未带 --apply ⇒ 什么都没做）`。

---

## 三 C# 读口：**已落地**（第二轮 · `W-17` 交回后调度台放行）

> 🔴 2026-11-01 第二轮：调度台把 `Shell/CardDisplayEffects.cs` 划给本写手。**按现读对齐 `W-17` 的版本**（他交回时 644 行），
> 现读 **689 行**（本写手 **+45**）。⛔ 没推翻 `W-8` 的两处、⛔ 没推翻 `W-17` 的两处（逐条核对见 §3·5）。
> ⚠️ 本文件**不在任何 `账/*.tsv` 的落点里**（`grep -rn "CardDisplayEffects\.cs:" 账/*.tsv` = 0 命中）⇒ 行号变化不会造成账漂移。

| 位置（**现读行号**） | 改动 |
|---|---|
| `Shell/CardDisplayEffects.cs:549` / `:553` | `CdeImage` 类尾新增 `public string sprite;` 与 `public bool hasSpriteName;`（带判据注释） |
| `:310`（原 `:309`） | `spr = im.hasSprite ? BorderSprite(im.name) : null;` → **`SpriteFor(im)`** |
| `:305-309` 注释块 | 就地订正：不再说「本链只认识升级那两件」，改成「判据里的 `sprite` 名字优先，旧判据才回退硬编码」 |
| `:336-343` 出声文案 | 「本链没有它的 sprite 表」→「判据里的 `sprite` 名字与硬编码表**都给不出**」，并指向新列 + 重生成入口 |
| `:656` | 新 `static bool _warnedSpriteName;`（只出声一次的闸，与既有 `_warnedNoSprite` 同做法） |
| `:664-679` | 新 `static Texture2D SpriteFor(CdeImage im)` |
| `:681-689` | 新 `static void WarnSpriteFallback(string why)` |

### 3·1 DTO：`CdeImage` 类尾两个字段（**已落地 `:549` / `:553`**）

```csharp
            /// <summary>原版 `Image.m_Sprite` 的**资产名**（账 `B-248` · 生成器写的 `sprite` 列）。
            /// `""` = 原版没有 sprite **或**名字没查到（两者靠 <see cref="hasSpriteName"/> 区分）。
            /// ⚠️ 旧 JSON（没这一列）读出来是 `null` —— ⛔ 别用 `!= null` 当「有名字」的判据。</summary>
            public string sprite;
            /// <summary>这一件**拿到了**名字（= `sprite != ""`）—— 显式布尔，与 `hasGlow` / `hasSprite` 同一套路。</summary>
            public bool hasSpriteName;
```

### 3·2 `BuildImage`：把「按节点名硬编码」改成「**优先读判据、缺失才回退 + 出声**」（已落地 `:310`）

`Shell/CardDisplayEffects.cs:310`（`W-17` 那一版是 `:309`，他加的行把它推后了一位）：

```csharp
                spr = im.hasSprite ? BorderSprite(im.name) : null;
```
改成
```csharp
                // 🔴 2026-11-01（B-248）：名字**优先读判据**（`sprite` = 原版 `Image.m_Sprite` 的资产名），
                //   判据里没有这一列 / 名字没查到时才回退按节点名硬编码的 `BorderSprite`，并且**出声**（⛔ 不静默）。
                spr = im.hasSprite ? SpriteFor(im) : null;
```

### 3·3 新方法 `SpriteFor` + `WarnSpriteFallback`（**已落地类尾 `:664-689`**）

> ⚠️ 落地版与下面这版**有一处刻意不同**：出声**统一走 `WarnSpriteFallback(why)`** 一个口（两条失败原因各写一句），
> 而不是在两个分支里各写一条 `if (!_warnedSpriteName)`。这样「回退必须出声」只有**一处可被删的地方**（断言 `D2b/D3b` 就盯着它）。

```csharp
        /// <summary>这一件该用哪张图（`B-248`）。
        /// <para>① 判据里有名字（`hasSpriteName`）⇒ 按资产名查（**空格换下划线** = `Art/ui_menu/` 的落盘规矩）。</para>
        /// <para>② 判据里没这一列（旧 JSON）/ 有 sprite 但名字没查到 ⇒ 回退 <see cref="BorderSprite"/>（按节点名硬编码那张表），
        /// 并且**出声一次**。⛔ 不许静默 —— 回退是兼容旧文件，不是常态。</para></summary>
        static Texture2D SpriteFor(CdeImage im)
        {
            if (im.hasSpriteName)
            {
                string res = im.sprite.Replace(' ', '_');
                var t = CardArt.MenuUi(res);
                if (t != null) return t;
                if (!_warnedSpriteName) { _warnedSpriteName = true;
                    Debug.LogWarning("[CardDisplayEffects] 判据给的 sprite `" + im.sprite + "` 取不到（`Art/ui_menu/"
                                   + res + "`）⇒ 回退按节点名硬编码（`BorderSprite`）"); }
            }
            else if (im.hasSprite && !_warnedSpriteName) { _warnedSpriteName = true;
                Debug.LogWarning("[CardDisplayEffects] `" + im.name + "` 的判据里没有 `sprite` 名字（旧 JSON？）⇒ "
                               + "回退按节点名硬编码；重生成 = `工具/gen_mainmenu_effects.py --apply`"); }
            return BorderSprite(im.name);
        }
```
（配套一个 `static bool _warnedSpriteName;` 字段，与既有的 `_warnedNoSprite` 同一个做法 —— 放到类尾。）

**调用链自证**：`CardDisplayEffects.Build(...)` → `BuildImage(root, im, s)`（`:286`）
→ `spr = im.hasSprite ? SpriteFor(im) : null` → `ImageQuad.Create(..., spr ...)` / `quad.SetTexture(spr, true)`。
回退那一支调用的就是 `W-8` 本波刚接好的 `BorderSprite`，**行为与今天逐字一致** ⇒ 即使判据文件还是旧的也不会退化。

### 3·4 顺带把 `:335` 那句过期的出声文案一起改了（已落地 `:340-343`）

原文写「本链没有它的 sprite 表 … 判据 → … 的 `hasSprite`」——
在新列**已经存在**之后这句就指错了。现改成「判据里的 `sprite` 名字与 `BorderSprite` 硬编码表**都给不出**那张图」，
并指向 `sprite` / `hasSpriteName` 两列 + 重生成入口。⛔ 行为不变（同一档才触发），只改文案。

### 3·5 没推翻别人的东西（逐条核对）

| 别人的改动 | 现读位置 | 状态 |
|---|---|---|
| `W-8`：`hasAmp` 那两件走 `SetVertexTint` | `:327` | ✅ 原样 |
| `W-8`：`AnimateUIMaterialProperty.Attach` 之后 `quad.SetTexture(spr, true)` | `:414` | ✅ 原样 |
| `W-17`：`BorderNineSlice(...) ⇒ quad.SetNineSlice(nbW, nbUv, true)` | `:332-333` | ✅ 原样（仍在 `SetVertexTint` 之后） |
| `W-17`：类尾 `BorderNineSlice` | `:633-652` | ✅ 原样（我的两个新成员追加在它**后面**） |

---

## 四 断言 `Editor/B114_W18_Assert.cs`（新建 · **274 行**）

前 6 段只读判据文件文本（**不经过 `CdeDoc`** —— 图件那两个字段是第二轮才进 DTO 的），自带一个「一键一行」的小扫描器；
第 `⑥` 段（第二轮加的）用**反射**直调生产代码的私有 `SpriteFor`，量「三档优先级」与「回退真的出声」。

| 编号 | 断的是什么 | 怎么才能红 |
|---|---|---|
| `A0` | **前提**：`Resources/Art/card_display_effects.json` 取得到 | 删掉那个文件（⛔ 不是 `if (json) Check(...)` 静默跳过） |
| `A1` | **前提**：`images` 段解析出 **4** 条 | 文件被截断 / 换成压缩成一行 |
| `A2` | ★★ **前提（新列存在）**：每条图件都带 `sprite` **和** `hasSpriteName` | 拿回 2026-10-27 之前那份 JSON |
| `B0/B1/B1b` | `Level Up Effect`：`hasSprite=true` · `sprite == "OctagonUI Filled SDF"` · `hasSpriteName=true` | 写成节点名 / 两件写反 |
| `B2/B2b` | `Level Up Effect Down`：`sprite == "OctagonUI Border SDF"` | Filled/Border 对调 |
| `B3~B3f` | `Side Border Left/Right`：`hasSprite=false` · `sprite == ""` · `hasSpriteName=false` | 生成器改写 `null` / 省掉键 |
| `B4/B4b/B4c` | **前提**：两个名字在我们盘上真取得回 `Texture2D`（`CardArt.MenuUi` 空格换下划线），且**是两个不同实例** | 名字打错一个字母 / 两件同图 |
| `C1` | ★★ **灭自证**：两件升级件的 `sprite` **必须不同** | 生成器「一律写同一张」 |
| `C2` | ★★ **灭自证**：`sprite` **不得等于节点名**（名字必须来自 PPtr，不是从 `name` 复读） | 生成器写成 `"sprite": name` —— **正是本波要根治的那个病** |
| `C3` | ★★ **灭自证**：两档并存（有「`hasSprite=false` + 空串」，也有「`true` + 非空」） | 这一列被写成常量 |
| `C4` | **灭自证**：`sprite` 里**带空格**（= 资产名原样） | 下游把它转成文件名下划线版再写回 |
| `D0/D0b` | **前提**：私有 `SpriteFor` 与出声闸 `_warnedSpriteName` 都还在（反射拿到） | 谁把回退逻辑/出声闸删掉 |
| `D1` | ★★ **判据优先**：条目名 `Level Up Effect` 但判据写 `OctagonUI Border SDF` ⇒ 取的是**判据那张** | 把 `SpriteFor` 改回「先查 `BorderSprite(im.name)`」⇒ 拿到 Filled ⇒ 红 |
| `D2/D2b` | ★★ **回退 + 真的出声**：判据没名字 ⇒ 回退到 `BorderSprite`，且 `_warnedSpriteName` 被翻成 `true`（= 那一支 `Debug.LogWarning` 真执行过） | 把 `WarnSpriteFallback` 改成静默 return ⇒ 红 |
| `D3/D3b` | ★ 判据给了名字但图取不到 ⇒ 也回退、也出声 | 同上 |

用 Python 复刻了一遍同一个扫描器（4 条、字段值如上），`C3 = True` ⇒ 前 14 条断言在当前数据上**应当全绿**。
`D1~D3b` 是**行为结构**断言（不依赖 JSON 内容），在本波这份代码上同样应当全绿。
⚠️ **没实跑**（红线不跑 Unity）—— 只有 `typecheck` 证据。

> 🔴 **「回退分支有没有真的出声」怎么答**：`D2b/D3b` 就是这条的落地闸 ——
> 断言不看日志文本，而是反射读 `_warnedSpriteName` 这个**只被 `WarnSpriteFallback` 翻转**的闸：
> 回退走过 ⇒ 它必然为 `true`；有人把 `Debug.LogWarning` 那一句删掉但留下翻闸，也会因为**闸跟着删不掉**而保留可观测点
> （真要“静默化”，必须把整个 `WarnSpriteFallback` 摘掉 ⇒ `D0b` 直接红）。
> ⚠️ **今天这份 JSON 下回退分支在**生产路径**不会被执行**（4 条里两条有名字、两条压根没 sprite）
> ⇒ 「出声」只有靠 `D2b/D3b` 这两条合成条目来验，**在 Unity 里跑一次 `BattleScene.Run` 才会印出结论**。

### 4·1 接线（**本波没加** —— 调度台统一追加）

宿主 `Assets/CardPresentation/Editor/BattleScene.cs` 那条聚合行，**同一物理行、第一个 `//` 之前**追加：

```csharp
B114_W18_Assert.Run(); _sink.Pass += B114_W18_Assert.Pass; _sink.Fail += B114_W18_Assert.Fail;
```

> ⚠️ 我**没有**往 `BattleScene.cs` 加任何东西（`W-14` 刚在 `:46896` 加过、`W-17` 也要加）。
> 追加位置 = **那一行已有的 `Debug.Log` / 聚合调用之前**，与 `W-14` 那句并列。

---

## 五 没查清的部分（如实写，⛔ 没猜测）

1. **旧解包 pid 索引的覆盖率没有全量统计。** 本波只验证了这两条命中（6,917 个文件名里正好有它们）。
   将来 `IMAGE_NODES` 加进来新的 sprite 时，若旧解包**没导出**那一份，生成器会写 `""` + `hasSpriteName=false`
   并打印原因 ⇒ **消费方回退 + 出声**，不会静默画错；但「有多少 sprite 会落进这一档」**没数**。
2. **新解包 `Sprite/` 的「同名不同资产」无法从文件名区分。** 本波靠「新解包里有这个名」做交叉验证，
   但同名两份（比如 `…_2` 之外的意外撞名）在文件名层面看不出。这两条名字另有 `W-8` 的 **sprite guid** 独立复核
   （`517922d5…` / `0fd9569a…`）⇒ 对本波的两条是双重证据；**通用性没有保证**。
3. **没跑 Unity** ⇒ 断言（`B114_W18_Assert`）一次都没执行过，`CardArt.MenuUi` 的两张图在运行期是否仍取得到
   （`CardArt.Get` 会缓存 null 那条坑）**只有静态证据**（两张 PNG 与 `.meta` 都在 `Resources/Art/ui_menu/`）。
4. **`m_Externals` 那条正路在本机确实走不通**：我只确认了「新解包没导出 externals 表」，
   没有去翻 `d:/2/Warpforge_tools` 的导出器源码找别的出口（超出本波范围）。
   若将来把 externals 导出来，`sprite_name_of` 应当改成走 `assets_full` 一处，⛔ 不再依赖旧解包。

---

## 六 顺手发现（⛔ 一条都没动手改）

1. 🔴 **「新解包 `Sprite/` 的文件名 = 资产名、不含 pathID」这件事本身是个坑**：
   与 CLAUDE.md §三「判资产有没有 ⛔ 不能按名字搜 —— 必须按脚本/组件反查」是**同一族的另一面** ——
   这里的**句柄**是 pathID，而 `assets_full` 把它**从文件名里丢掉了**。
   ⇒ 以后凡是「PPtr → 资产」的反查，**不要**以为翻 `assets_full` 就能解，先看有没有 pid 索引。
2. **旧解包 `d:/2/解包整理/**/Sprite/` 是本机唯一能 pid→sprite名 反查的索引**（6,917 条）。
   ⚠️ 它**图**不能信（650 张与新解包不同），但**名字/元数据**仍可用 —— 这条最好写进「解包资源使用地图」。
3. **生成器 `--apply` 会同时重写两份产物**（`mainmenu_effects.json` 的 275,854 字节本波未变）。
   ⇒ 以后谁只想改运行时那份，仍然要整跑一次（两份必须同一次产出，`OUT_RT` 上面那条注释就是这么定的）。
4. 🔴 **第一次 `typecheck` 报 292 条编辑器错误、全部落在别人的文件上**
   （`B94B17_Assert` / `B97_W2_Assert` / `B98G6_Assert` / `B111_S3_Assert` / `B99T2_Assert` /
   `B102EFF_Assert` / `B103W2_Assert` / `B112_G_Assert`，报的是 `LiveOps*` / `CardDef.CountSetSpec` / `ScoreBars` 找不到），
   **紧接着重跑 = 0 / 0**（`TMPDIR=/tmp/wf_w18`，连跑两次都是 0/0）。
   ⇒ 按 CLAUDE.md §13·3 第 2 条：**错误全集中在不是你负责的文件上 ⇒ 不是你的问题**（并发写手正在写的半成品）。
   记录在这里，免得下一个会话拿它当本波的错。
