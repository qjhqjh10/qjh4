# -*- coding: utf-8 -*-
"""把第三十会话新踩的坑追加进 `资料/已知的坑.md`（跨线正本）。
用法：python -I -X utf8 d:/4/Unity/工具/_append_pits_1014.py
"""
import io

P = r'D:\4\Unity\资料\已知的坑.md'

ADD = """

---

## 🔴 2026-10-14（第三十会话）：**`_tmp_view` 有【两份】** —— 找产物只查一边会**假阴性**

`D:/4/_tmp_view/` 与 `D:/4/Unity/_tmp_view/` —— **同名**，产物住哪一份**取决于哪个工具写的**：
- `D:/4/_tmp_view/`：`w15_scan.txt` · `our_scan.txt` · `card_ids_made.txt`（**探针/脚本**写的）
- `D:/4/Unity/_tmp_view/`：`arena3/run_battle_1014.log`（**自检宿主**写的）· `scene_bak/`

⇒ 🔴 **找产物时两边都必须看**。只查一边 ⇒ `find`/`ls` 说「没有」而它其实在另一份 ⇒ **假阴性**。
（本会话因它白跑过一轮；`A1969` 那次我转给代理的日志行号就是这么错的。）

## 🔴 2026-10-14（第三十会话）：**改同一个文件的多处时，「插入」与「删除」⛔ 不能混在一趟里按索引做**

**我踩的**：脚本先**插入**一行新账（`A2005`）、再按**旧行号**删另外几段 ⇒ **插入把后面所有行整体后移** ⇒
「按旧行号」删到的是**别的行** ⇒ **误删了 §29·c 里 `A577`·`A586`·`A367`（等真 Play）那一行**，而目标行反倒**没删掉**。

✅ **纪律：先删 → 再改 → 最后插**；**每一步都【重新定位】**（⛔ 别缓存行号、别用同一次扫描的下标做多次编辑）。
✅ 改完**必须做完整性核查**：① 该搬走的还在不在 ② 该留的少没少（⚠️ **核查判据本身也可能写漏** ——
我这趟第一版核查只查「粗体编号」的行，而 §29·c 的行**不带粗体** ⇒ 差点漏过去）。
✅ 兜底：`git show HEAD:<路径>` 能取回**本会话没改过**的行（先 `git diff` 确认那几行确实没动过）。

## 🔴 2026-10-14（第三十会话）：**「已在」判据不能只看子串，要看【行首】**

**同一个错犯了两次**：`'**\\`A2002\\`**' in text` **匹配到了别人行里的【提及】**（正文里一句「⇒ 新账 `A2002`」）
⇒ 守卫以为「已经插过了」⇒ **两条账根本没进表**（而且**没报错**）。
✅ **判据：`line.startswith(...)` / 正则加 `^`** —— 尤其是**编号类**的标记（`| 🆕 **\\`A####\\`** |`）。

## 🔴 2026-10-14（第三十会话）：**⛔ 别用 `bash python -c "…带反引号…"` 改正本**

bash 会把**反引号当命令替换**，把内容吃掉 —— 症状是日志里冒 `desc: command not found`，
而**写进文件的那一段已经缺了字**（本会话把「改 `desc`（英文），`descZh` 是对的」写成了「改 （英文）， 是对的」）。
✅ **要改就写成文件再跑**；✅ 事后**加体检断言**（例如「不许出现空括号 `（）`」）。

## 🔴 2026-10-14（第三十会话）：**`✅` 标记【不可信】—— 判「开/闭」一律读行内自陈**

本会话做了一次全表审计，**同一张表里两种反向错都有**：
- **`✅` 但其实还开着**：`A1088`（行内写「**我们没做**」）· `A1694`（「**还差最后一步**」）· `A1431`（「🟡 …**【仍开着】**」）
- **`🆕` 但其实已闭**：`A1590`（只有 ③ 活着）· `A1662`（§四那行已结案、§五那行是存档）

⇒ 🔴 **判据只有一条：行内有没有明写「还剩什么」**。✅ **别按标记统计条数**（本会话因此查出 10 条「已完成却还列着」的假账）。
✅ 附带：**同一个编号可能在文件里出现两行**（`A1662` 就是）⇒ 按编号定位时要**断言命中数 == 1**。

## 🔧 2026-10-14（第三十会话 · `A650` 现做）：**读【没有 type tree】的 `.assets` —— `read_typetree()` 全失败 ≠ 文件坏了**

`d:/2/unity_run_ref/Warpforge_Data/resources.assets`（36.8 MB）：`UnityPy` 的 `read_typetree()`
**616 / 616 全失败**、连 `o.read()` 都拿不到 `m_Script` ⇒ **这就是「该文件没有 type tree」的症状**，不是数据坏。

✅ **三步做法**：
1. `env = UnityPy.load(<文件>)` ⇒ `env.files` 是 dict（取那个 `SerializedFile`）；看 `externals`
   （`resources.assets` = `[globalgamemanagers.assets, Library/unity default resources, sharedassets0.assets]`）。
2. 对每个对象 `obj.get_raw_data()` 拿**原始字节**，按**标准 MB 头**切：
   ```
   offset  0: m_GameObject.m_FileID (int32)      offset 12: m_Enabled (uint8 + 3 pad)
   offset  4: m_GameObject.m_PathID (int64)      offset 16: m_Script.m_FileID (int32)   ← 通常 1 = externals[0]
                                                 offset 20: m_Script.m_PathID (int64)
                                                 offset 28: m_Name 长度 (int32) + 字节
   ```
   ⚠️ **本 build 是 `m_Name` 从 offset 32 起** —— ⛔ **别把「固定 32」套到别的文件上**（不同版本头长不一样）。
3. 🔴 **把 `m_Script.m_PathID` 翻成类名**：查
   `d:/2/新解包资源/assets_full/globalgamemanagers/MonoScript/MonoScript_<pathID>.json` 的 **`m_ClassName`**
   （该表现读 **4,453 份**）⇒ **这是「不知道类名也能解」的通用口子**，比 `menu_dump.mono_index()` 直接、也不依赖任何索引文件。

✅ **产物**：`工具/_a650_unpack_resources.py` · `工具/_a650_probe2.py`。
🔴 **本会话用它得到的结论**：那 616 个对象**全是 Unity 自己的运行时调试面板（`DebugUIHandler*` ×~100）＋
第三方库的 Resources 资产**（`Image`×174 · `Text`×149 · `Toggle`×66 · `TMP_*` · `LanguageSourceAsset` · `DOTweenSettings` …），
**没有一个游戏自己的 UI 组件** ⇒ 它**不是**「解不出来的游戏资产」。
"""


def main():
    b = io.open(P, 'rb').read()
    assert b.count(b'\r\n') == 0, 'must be LF'
    t = b.decode('utf-8')
    if '第三十会话' in t:
        print('  = 已经追加过，跳过')
        return
    t = t.rstrip('\n') + ADD
    assert '\x00' not in t and '\r\n' not in t
    io.open(P, 'wb').write(t.encode('utf-8'))
    print('OK 已知的坑.md %d -> %d 字节' % (len(b), len(t.encode('utf-8'))))


if __name__ == '__main__':
    main()
