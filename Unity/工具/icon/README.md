# `工具/icon/` —— 卡面图标「记号 → sprite」这一族的可复用脚本

> 2026-10-10（`A1416`）收进来的。三个脚本**逐字节照搬**（md5 与 `_tmp_a1400/` 那份相同），
> **没改一行**；下面写清各干什么、怎么跑、哪一批用过它。

| 脚本 | 干什么 |
|---|---|
| `sim_rewrite.py` | 把 `CardPresentation/Core/CardIcons.cs` 的 `Rewrite` **逐分支静态复刻**，拿一份计划表对**全池 1126 张 × `desc`/`descZh`** 各铺一遍 → 落一份 JSON（`rendered` + `not_idempotent`）。**改计划表前后各跑一次、两份对拍**，就是「误伤面」那一层（渲染层）的读数 |
| `cmp_plan.py` | 两份计划表**严格对账**：条目增删 / 新增逐条（按 sprite 分档）/ 消失或被改的条目 / 两份 `cards` 数组的 md5 |
| `conflicts.py` | **子串冲突枚举**（一次性探针，见下面的 ⚠️） |

## 怎么跑

```bash
cd d:/4/Unity

# ① 渲染层复刻（cwd 无关；ROOT 写死在脚本里的 "D:/4/Unity"）
PYTHONIOENCODING=utf-8 python -I 工具/icon/sim_rewrite.py \
    数据/游戏数据/card_icon_plan.json  _tmp/sim_after.json     # → "wrote … rendered: N not_idempotent: M"

# ② 计划表对账（cwd 无关）
PYTHONIOENCODING=utf-8 python -I 工具/icon/cmp_plan.py  _tmp/plan_before.json 数据/游戏数据/card_icon_plan.json

# ③ 子串冲突枚举（⚠️ **必须 cd d:/4** —— 它里面的路径是相对的 "Unity/数据/…"）
cd d:/4 && PYTHONIOENCODING=utf-8 python -I Unity/工具/icon/conflicts.py
```

- ⚠️ **三个都是 `python -I`**（隔离模式吃掉 `PYTHONIOENCODING`）—— 脚本里自己 `reconfigure(encoding="utf-8")`，
  所以**必须带 `-I`** 跑（不带的话在 GBK 控制台上会 `UnicodeEncodeError`）。
- ⚠️ `sim_rewrite.py` 的 docstring 里写的是 `<out.txt>`，**实际写的是 JSON**（`io.open(..., "w", encoding="utf-8")`
  + `json.dump`）—— 别被那两行误导，扩展名给 `.json`。

## 两个脚本的已知边界（别当「没查」）

| 边界 | 说明 |
|---|---|
| `sim_rewrite.py` 是**静态复刻**，不是 Unity 实跑 | 它不模拟 `<link>` 那一层的运行时行为（`Badges.KeyOf` 只静态复刻了 4 条反向映射 + 小写化）。**它证明的是「计划表 → 渲染串」这一步**，不是「卡面真渲染成什么样」 |
| `conflicts.py` 的 **B 段中文那一半会误判** | 它用 `[A-Za-z一-鿿]*词[A-Za-z一-鿿]*` 找「更长词」，**汉字之间没有词边界** ⇒ 相邻汉字必然被吃进来（例如 `无敌直到你的下个回合` 被判成「更长词」）。**中文侧要看它打出来的上下文自己判**，别只看计数 |
| `conflicts.py` 是**一次性探针** | `P` / `CARDS` 两个路径写死、B/C/D/E 段写死查 `Invulnerable` / `无敌`。要查别的词得改脚本；**它的价值是「手法存档」**（子串冲突该怎么枚举），不是通用工具 |
| `cmp_plan.py` 的键是 `(卡, 字段, token)` | 同 `(卡, 字段, token)` 出现两次时**后者静默盖前者** —— 脚本本身不查重复键。查重复键要用 `object_pairs_hook`（见 `资料/卡面图标_现状与缺口.md` 那条纪律） |

## 哪一批用过它

| 批 | 用的哪个 | 读数（当次） |
|---|---|---|
| **`A1400`**（2026-10-10，第十四会话 `/tmp` 原始出处） | 三个全用了 | `sim_rewrite` 对「改动前 / 改动后」两份计划表各铺一遍、**全池 1126×2 逐字比**；`cmp_plan` 比两份计划表；`conflicts` 枚举子串 |
| **`A1376`**（2026-10-10，图标族 8 词） | `sim_rewrite` 同法 | 见 `资料/普查产出_第十四会话/W_图标族8词.md` §三 |
| **`A1366`**（`Stun` 那 30 张） | 同上手法 | 见 `资料/普查产出_第十四会话/W_icon链两笔.md` |
| **`A1411` / `A1378`**（2026-10-10，本批） | 三个全用 | 见 `资料/普查产出_第十四会话/W_icon工具侧三笔.md` §三 |

## 和别的脚本的分工

- **生成计划表** → `工具/gen_icon_plan.py`（`--write` 落两份 json；不带 = 只报告）
- **生成人看的文档** → `工具/gen_icon_doc.py`（读计划表 → `资料/卡面图标_对照与缺口.md`）
- **本目录这三个** → **只读地验**（复刻渲染 / 对账 / 枚举），**一个都不写工程里的文件**
