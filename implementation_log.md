# 人物 Mod 实现日志

用于记录当前已经实现的内容、临时测试方式，以及还没有实现/需要确认的部分。

## 当前工作区

- 根目录: `/Users/carambola/Desktop/cursor+codex/mod`
- Mod 工程: `/Users/carambola/Desktop/cursor+codex/mod/workspace/mod`
- 参考 Mod: `/Users/carambola/Desktop/cursor+codex/mod/reference_mods/extracted_EW`
- 卡牌设计文档: `/Users/carambola/Desktop/cursor+codex/mod/cards.md`

## 已实现

### 工程基础

- 建立了 `workspace/mod/mod.csproj`，可以编译 C# mod DLL。
- 建立了 `workspace/mod/mod.json`，当前 mod id 是 `mod`。
- 建立了 `ModCode/MainFile.cs`，用于 mod 初始化和 Harmony Patch。
- `dotnet build` 已验证通过，当前输出 DLL:
  `/Users/carambola/Desktop/cursor+codex/mod/workspace/mod/.godot/mono/temp/bin/Debug/mod.dll`

### 自定义人物

- 已新增 `ScarletAcolyte` / 胖宝宝。
- 当前是最小可测试人物壳:
  - 72 初始生命。
  - 使用独立的 `ScarletCardPool`。
  - 使用独立的 `ScarletRelicPool`。
  - 使用独立的 `ScarletPotionPool`。
  - 战斗视觉当前使用 `fat_baby_combat_visual.tscn` 无脚本场景 + `fat_baby_combat_idle.png` 静态立绘，由 `ScarletAcolyte.CustomVisualPath` 接入并交给 BaseLib 自动转换为 `NCreatureVisuals`。
  - 选人界面已使用胖宝宝洋葱骑士举杯图。
- 2026-05-29 修复开始旅途黑屏:
  - 日志显示进入 Run 时 `ScarletAcolyte.CustomIcon` 把字符串路径传给 BaseLib `NodeFactory<Control>.CreateFromResource(...)`，触发 `Node factory for Control does not support generation from resource type String`。
  - 现在 `CustomIcon` 直接创建 `TextureRect`，并通过 `ResourceLoader.Load<Texture2D>(...)` 加载 `fat_baby_onion_icon.svg`。
- 2026-05-29 调整顶栏小 UI 头像:
  - 截图确认进入旅途后左上角头像过大且偏左上。
  - 已将头像 `TextureRect` 设置为 `ExpandMode = IgnoreSize`、`StretchMode = KeepAspectCentered`，让 SVG 在头像框内按比例居中缩放。
- 选人界面资源:
  - 原始参考图: `/Users/carambola/Desktop/cursor+codex/mod/assets/character_covers/onion_knight_toast_cover_v5_brighter_medium.png`
  - 工程内图片: `/Users/carambola/Desktop/cursor+codex/mod/workspace/mod/mod/images/character_select/fat_baby_cover.png`
  - 选人背景场景: `/Users/carambola/Desktop/cursor+codex/mod/workspace/mod/mod/scenes/screens/char_select/fat_baby_select_bg.tscn`
  - 角色按钮图标: `/Users/carambola/Desktop/cursor+codex/mod/workspace/mod/mod/images/character_select/fat_baby_select_icon.png`
  - 角色 UI 小图标: `/Users/carambola/Desktop/cursor+codex/mod/workspace/mod/mod/images/character_ui/fat_baby_onion_icon.svg`
    - 当前为白色正面洋葱头盔，透明背景，轮廓已调成偏扁圆。
  - 战斗静态立绘: `/Users/carambola/Desktop/cursor+codex/mod/workspace/mod/mod/images/character_combat/fat_baby_combat_idle.png`
  - 战斗视觉场景: `/Users/carambola/Desktop/cursor+codex/mod/workspace/mod/mod/scenes/character/fat_baby_combat_visual.tscn`
  - 商店待机立绘: `mod/images/merchant/fat_baby_merchant_idle_027_pct54.png`（源帧 `merchant_screen/027_pct54`）；通过 `ModCode/Patches/ScarletMerchantCharacterPatch.cs` 在商店房间程序化创建静态 `NMerchantCharacter`，避免原版商店节点强制播放 Spine 动画。
  - 篝火坐姿立绘: `mod/images/rest_site/fat_baby_rest_site_sit_34_pct68.png`（源帧 `rest_site_sit/34_pct68`）
  - 篝火角色场景: `scenes/rest_site/characters/scarletacolyte_rest_site.tscn`（静态 `TextureRect`，无 Spine，无 C# 脚本引用）
  - 篝火节点创建: `ModCode/Patches/ScarletRestSiteCharacterPatch.cs` patch `NRestSiteCharacter.Create`，对 `ScarletAcolyte` 程序化创建 `NRestSiteCharacter`、坐姿贴图、`%Hitbox`、`%SelectionReticle`、`%ThoughtBubbleLeft/Right`；避免 mod PCK 场景脚本无法关联时根节点退化为 `Node2D`，导致 `InvalidCastException` 并中断选项加载。
  - `ScarletAcolyte.CustomRestSiteAnimPath` 指向 mod 篝火场景（此前缺此项，游戏会去读不存在的原版 `scarletacolyte_rest_site`）
  - 胖宝宝池内 `CardRarity.Ancient`（蓄力重击、小圆盾格挡）改用与普通/罕见/稀有相同的横版卡框与立绘区，底部横幅材质为银色 `card_banner_ancient_silver_mat.tres`（稀有为金色 HSV）；通过 `ModAncientCardVisualPatch` 覆盖原版古卷 UI。
    - 无脚本场景，节点包括 `Visuals`、`Bounds`、`CenterPos`、`IntentPos`，由 BaseLib 自动转换为 `NCreatureVisuals`。
- 起始卡组:
  - `PRAYER_STRIKE` x5
  - `FLAME_PURIFY` x4
  - `SCARLET_COST` x1
- 起始遗物:
  - `SCARLET_EMBER_RELIC` / 猩红余烬。
  - 当前负责在战斗中给敌人补上失衡值。

### 猩红腐败机制

- 已实现 `ScarletCorruptionPower`。
- 类型: Debuff。
- 计数显示: 显示当前层数。
- 上限: 3 层。
- 触发时机: 你的回合开始时。
- 效果: 失去等同层数的生命值。
- 当前口径: 失血无视格挡，不自然衰减。
- 图标: 临时使用游戏原生 `corruption_power` 图标。

### 猩红腐败工具方法

- 已实现 `ScarletCorruption.Apply(...)`。
- 已实现 `ScarletCorruption.Reduce(...)`。
- 后续卡牌应优先通过这两个方法增减腐败，方便统一维护规则。

### 失衡系统

- 已实现 `ImbalancePower` / 失衡。
- 当前口径:
  - 普通怪: 10 失衡值。
  - Boss/精英 召唤爪牙: 10 失衡值（`MinionPower` / 次要敌人 / 感染虫死后刷新的 `Wriggler`），不按房间类型抬高。
  - 精英: 单体精英 20，群怪精英 15。
  - Boss: 25。
  - 削韧会降低目标失衡值。
  - 失衡值被削到 0 时，调用游戏自带 `CreatureCmd.Stun(...)` 使目标眩晕一回合。
  - 眩晕触发后，失衡值重置为该敌人的初始失衡值。
- 当前实现入口:
  - 起始遗物 `SCARLET_EMBER_RELIC` 在玩家回合开始前给所有敌人补上失衡值。
  - `PrayerStrike` / 祈祷一击 现在会造成削韧 2。
- 当前技术说明:
  - 失衡暂时作为敌人身上的可见 Power 显示。
  - 失衡图标已改为法环「岩刺破露滴」物品图（`mod/images/powers/imbalance_power.png`，参考源图 `assets/reference/stonebarb_cracked_tear_source.png`）。
  - 2026-05-31 修复进入第二层后失衡/猩红腐败 buff 图标消失：
    - `LoadActAssets` 会卸载未列入 Run 预加载集的 mod 纹理；`ModRunAssets` + `ScarletAcolyte.ExtraAssetPaths` 常驻预加载 PNG 与 `.tres` 图集。
    - `NPower` 通过 `PowerModel.Icon` 取图，不会走 BaseLib 的 `CustomPackedIconPath`；已加 `NPowerModIconPatch` 在 `Reload` 时用 `PreloadManager.Cache` 强制刷新自定义图标。
  - 因为角色系统仍是原型，失衡初始化暂时挂在起始遗物的战斗钩子上。
  - 当前还没有逐个敌人配置失衡值，先按房间类型和敌人数量粗分。

### 已实现卡牌

- `SCARLET_COST` / 猩红代价
  - 0 费技能。
  - 抽 3 张牌，获得 1 层猩红腐败。
  - 升级: 抽 4 张牌。

- `FLAME_PURIFY` / 火焰啊，净化
  - 1 费普通技能。
  - 移除 2 层猩红腐败，获得 8 点格挡。
  - 升级: 移除 3 层猩红腐败，获得 10 点格挡。

- `PRAYER_STRIKE` / 祈祷一击
  - 1 费罕见攻击。
  - 移除 1 层猩红腐败，造成 4 点伤害，削韧 2。
  - 升级: 移除 2 层猩红腐败，造成 5 点伤害，削韧 2。

- `SCARLET_CREDIT` / 赊账
  - 0 费技能。
  - 获得 2 层猩红腐败，获得 2 点能量。
  - 升级: 获得 3 点能量。

- `CORRUPTION_STRENGTH` / 腐败力量
  - 1 费能力。
  - 获得 2 点力量，获得 2 层猩红腐败。
  - 升级: 获得 5 点力量。

- `CORRUPTION_SENSE` / 腐败感知
  - 1 费能力。
  - 获得猩红腐败时抽 1 张牌。
  - 升级: 获得猩红腐败时抽 2 张牌。
  - 当前只在猩红腐败实际增加层数时触发；被 3 层上限完全挡住时不触发。

- `SCARLET_TEMPTATION` / 猩红诱惑
  - 2 费能力。
  - 打出时获得 3 层猩红腐败。
  - 每回合额外抽 2 张牌，然后选择 1 张手牌消耗。
  - 升级: 固有。

- `GARBAGE_KING_BLESSING` / 垃圾王的庇佑
  - 2 费能力。
  - 虚无。
  - 你的回合开始时获得 4 点力量，并获得 3 层猩红腐败。
  - 升级: 每回合获得 5 点力量。

- `RETURN_PRINCIPLE` / 回归性原理
  - 2 费技能，消耗。
  - 移除自身所有负面效果，获得 2 点力量。
  - 升级: 1 费。

- `JACK_WINE` / 杰克的酒
  - 1 费技能。
  - 回复 5 点生命，下回合获得 1 点能量。
  - 升级: 回复 8 点生命。

- `PLUNDERING_FIRE` / 掠夺之火
  - 1 费技能，消耗。
  - 本回合通过攻击牌造成未被格挡的伤害时，回复等量生命。
  - 升级: 0 费。

- `BLOOD_LEVY` / 鲜血征收
  - 1 费攻击。
  - 造成 2 点伤害 3 次，回复 3 点生命，削韧 1。
  - 升级: 每段 3 点伤害，回复 4 点生命。

- `WILL_TO_WIN` / 好胜心
  - 3 费能力。
  - 本场战斗生命值不会低于 1。
  - 升级: 保留。

### 已实现削韧流卡牌

- `BASIC_ATTACK` / 攻击：1 费基础攻击，6 伤害，削韧 2；升级 9 伤害。
- `BASIC_DEFENSE` / 防御：1 费基础技能，获得 5 格挡；升级 8 格挡。
- `CHARGED_ATTACK` / 蓄力攻击：2 费基础攻击，15 伤害，削韧 5；升级 18 伤害。
- `GUARD_COUNTER` / 反击：0 费攻击，充能 5；直到下回合开始受伤降低 50%，6 伤害，削韧 5。
- `STORMCALLER` / 唤起风暴：X 费普通攻击，对所有敌人造成 3 点伤害 X+1 次，削韧 2；升级 4 伤害、X+2 次。
- `ERDTREE_SHOCK` / 黄金树坠落震击：1 费普通攻击，AOE 7 伤害，抽 1 张牌，削韧 2；升级抽 2。
- `CAESTUS_STRIKE` / 拳套打击：2 费普通攻击，4 伤害，削韧 8；升级 6 伤害、削韧 10。
- `SHIELD_CRASH` / 盾牌冲击：1 费普通攻击，获得 6 格挡，AOE 4 伤害，削韧 2；升级 8 格挡、6 伤害。
- `FATAL_STRIKE` / 致命一击：0 费普通攻击，目标眩晕时造成 9 伤害 2 次；升级保留。
- `GIANT_HUNT` / 特大跳劈：1 费普通攻击，5 伤害 2 次，虚弱 1，削韧 3；升级 8 伤害。
- `CARIAN_SLICER` / 卡利亚迅剑：1 费普通攻击，5 魔法伤害，打出后将这张牌返回手牌，削韧 1；升级 9 伤害。
- `STAMP_UPPERCUT` / 箭步-上砍：2 费普通攻击，获得 4 格挡，6 伤害，削韧 6；升级 6 格挡、8 伤害。
- `READY_STANCE` / 准备架势：1 费罕见攻击，4 伤害，获得 1 力量，削韧 4；升级 2 力量、削韧 5。
- `CARIAN_PIERCER` / 卡利亚贯刺：2 费罕见攻击，AOE 9 魔法伤害，虚弱 1，削韧 2；升级 12 伤害、虚弱 2。
- `LION_CLAW` / 狮子斩：2 费罕见攻击，20 伤害，削韧 6；升级 27 伤害。
- `TOTEM_TABLET` / 图腾碑石：1 费稀有攻击，充能 10，AOE 10 伤害并击晕所有敌人；升级 13 伤害、充能 8。
- `ROCK_BLADE` / 岩石剑：1 费普通技能，下一张攻击牌额外削韧 3；升级 0 费、额外削韧 4。
- `GLINTBLADE_PHALANX` / 巨剑阵：3 费罕见技能，下回合 3 个辉石巨剑各造成 7 魔法伤害并削韧 5；升级 2 费。
- `VICTORY_RUSH` / 乘胜追击：2 费罕见能力，击晕敌人时获得 1 能量，并使下一张攻击造成双倍伤害；升级 1 费。

### 卡牌流派盘点

- 2026-05-29 已生成削韧流与猩红流卡牌制作状态 Canvas:
  `/Users/carambola/.cursor/projects/Users-carambola-Desktop-cursor-codex-mod/canvases/scarlet-card-status.canvas.tsx`
- 2026-05-30 已将英文卡名调整为偏《艾尔登法环 / 黑夜君临》风格:
  - `Bloodboon Bargain`
  - `Flame, Cleanse Me`
  - `Prayerful Strike`
  - `Blood Debt`
  - `Bloodflame Exultation`
  - `Sanguine Insight`
  - `Scarlet Aeonia`
  - `Blessing of the Dung Eater`
  - `Law of Regression`
  - `Jack's Draught`
  - `Taker's Flames`
  - `Blood Tax`
  - `Endure`
- 2026-05-31 第二轮英文卡名 FromSoft 化（`workspace/mod/mod/localization/eng/cards.json`）:
  - 修复 `WILL_TO_WIN` 与 `ENDURE` 都叫 Endure 的冲突：`好胜心` → `Death's Door`，`忍耐` 保留 `Endure`
  - 泛化/误用改名示例：`Stormcaller` → `Storm Blade`，`Royal Knight's Resolve` → `Square Off`（对齐准备架势），`Stamp (Upward Cut)` → `Quickstep (Upward Cut)`（对齐箭步-上砍），`War Cry` → `Braggart's Roar`（对齐夸耀咆哮），`Cinder` → `Ember`（对齐余火）
  - 其余 26 张标题见 git diff；已保留官方向名称如 `Law of Regression`、`Carian Slicer`、`Giant Hunt`、`Glintblade Phalanx` 等
- 当前盘点口径:
  - 基础牌和升级牌合并为一张卡牌组统计。
  - 削韧流包含削韧、眩晕/击晕、击晕收益、下一张攻击增加削韧的卡牌。
  - 猩红流包含获得/移除/触发猩红腐败的卡牌，并补充生命回复/低血线保险类支撑牌。
- 当前结果:
  - 削韧流卡牌组 24 个，已制作 21 个；未制作的 3 个为草案牌：`斧柄突刺`、`古神月牙`、`懒散架势`。
  - 猩红流卡牌组 13 个，已制作 13 个。
  - 注意：`GARBAGE_KING_BLESSING` 已接入塔二已有 `Ethereal` 关键词；`SCARLET_TEMPTATION` 已改为使用塔二选牌界面选择手牌消耗。
  - 注意：`GUARD_COUNTER`、`TOTEM_TABLET` 已接入充能循环；`VICTORY_RUSH` 当前以“下一张攻击双倍伤害”实现“额外打出一次”的收益。

### 测试入口

- 已移除铁甲战士临时卡池入口。
- 上面已实现卡牌现在属于 `ScarletCardPool`，由胖宝宝使用。
- 控制台可用 `card SCARLET_COST`、`card SCARLET_CREDIT`、`card CORRUPTION_SENSE`、`card STORMCALLER`、`card ROCK_BLADE` 等命令单独生成卡牌。
- 2026-05-30 已新增百科全书卡牌解锁补丁:
  - `ScarletCardLibraryUnlockPatch` 会让 `ProgressState.DiscoveredCards` 自动包含胖宝宝全部卡牌。
  - `UnlockState.Cards` 自动补入胖宝宝全部卡牌，方便依赖解锁集合的界面读取。
  - 打开卡牌库时会把胖宝宝全部卡牌标记为已发现并保存进进度。
  - 卡牌库可见性对胖宝宝卡牌强制返回 `Visible`，用于显示完整卡牌效果和详情。
  - 验证通过: `./scripts/sts2_mod_dev.sh test`、`./scripts/sts2_mod_dev.sh code`。需要重启游戏加载新的 DLL。
- 2026-05-30 已新增不进游戏的卡牌静态测试入口:
  - `tests/card_expectations.json`: 当前卡牌费用、类型、稀有度、目标、关键词、动态变量和升级后的基线。
  - `scripts/test_cards.mjs`: 校验 `ModCard` 类、`ScarletCardPool` 注册、起始牌组、中英文卡牌本地化、动态变量占位符、JSON 解析，以及卡牌未被 `MaxUpgradeLevel <= 0` 静态排除出篝火敲牌池。
  - `scripts/test_cards.sh`: 先跑静态测试，再执行 `dotnet build workspace/mod/mod.sln`。
  - `scripts/sts2_mod_dev.sh test`: 已接入统一开发脚本。
  - `testing.md`: 记录卡牌系统测试分层方案、敲牌/房间测试口径和后续战斗结算自动化方向。
  - 验证通过: `./scripts/test_cards.sh`、`./scripts/sts2_mod_dev.sh test`。

## 未实现

- 人物正式命名和背景设定。
- 起始遗物正式效果。
- 自定义药水。
- 自定义人物正式卡池继续扩充。
- 人物战斗贴图、战斗动画、能量图标。
- 卡牌正式卡面贴图。
- 猩红腐败相关联动:
  - 当前 `腐败感知` 只在猩红腐败实际增加层数时触发抽牌；被上限挡住的腐败不算“获得腐败”。
  - 猩红腐败是否可被 Artifact 或免疫负面效果抵挡。
  - 猩红腐败失血是否触发所有“受到伤害/失去生命”联动。
- 失衡相关待确认:
  - 失衡是否应被 Artifact 或免疫负面效果影响。
  - 每个敌人是否需要单独失衡值表。
  - Boss 是否应该有眩晕抗性，或破失衡后重置为更高失衡值。
  - 多段攻击削韧已经在设计口径中说明“每段都触发”，但当前只有 `PrayerStrike` 单段接入，后续实现多段攻击时需要每段后调用一次 `Imbalance.Reduce(...)`。

## 当前测试方式

### 快速调试脚本

已新增脚本:

```bash
/Users/carambola/Desktop/cursor+codex/mod/scripts/sts2_mod_dev.sh
```

常用命令:

```bash
cd /Users/carambola/Desktop/cursor+codex/mod
./scripts/sts2_mod_dev.sh test
```

用途: 不进入游戏，自动校验卡牌定义/注册/本地化/起始牌组并执行 C# 构建。适合每次改卡牌后先跑一遍。

```bash
cd /Users/carambola/Desktop/cursor+codex/mod
./scripts/sts2_mod_dev.sh code
```

用途: 只编译 C# 并复制 `mod.json` + `mod.dll`，适合只改卡牌逻辑、数值、机制代码。执行后需要重启游戏，因为 DLL 在游戏启动时加载。

```bash
./scripts/sts2_mod_dev.sh all
```

用途: 同步 `BaseLib`、编译 DLL、导出 PCK、安装全部文件。适合改了本地化、图片、资源、`mod.json` 或需要完整重新导出时使用。

```bash
./scripts/sts2_mod_dev.sh log
```

用途: 快速查看最近的 mod 加载日志，排查 `Card not found`、前置未加载、DLL 异常等问题。

## 最近一次导出

- 时间: 2026-05-30
- 已生成 PCK:
  `/Users/carambola/Desktop/cursor+codex/mod/workspace/mod/build/mod.pck`
- 已生成 DLL:
  `/Users/carambola/Desktop/cursor+codex/mod/workspace/mod/.godot/mono/temp/bin/Debug/mod.dll`
- 已安装到游戏 mods 目录:
  `/Users/carambola/Library/Application Support/Steam/steamapps/common/Slay the Spire 2/SlayTheSpire2.app/Contents/MacOS/mods/mod`
- 已安装文件:
  - `mod.json`
  - `mod.dll`
  - `mod.pck`
- 已同步前置:
  `/Users/carambola/Library/Application Support/Steam/steamapps/common/Slay the Spire 2/SlayTheSpire2.app/Contents/MacOS/mods/【001】必装前置/BaseLib（0.2.6）`
- 导出补充:
  - 新增了 `workspace/mod/export_presets.cfg`。
  - 新增了 `workspace/mod/mod.sln`，方便 Godot 识别 C# 工程。
  - PCK 当前只用于资源与本地化；C# 逻辑由 `mod.dll` 提供。
  - 2026-05-28 发现游戏实际扫描路径是 app 包内部的 `Contents/MacOS/mods`；外层 `Slay the Spire 2/mods` 不会被当前游戏日志中的 ModManager 读取。
  - 控制台报 `Card 'SCARLET_COST' not found` 时，日志显示原因是 `BaseLib` 没有出现在实际扫描目录中，导致本 mod 被跳过加载。
  - 2026-05-29 执行 `./scripts/sts2_mod_dev.sh all` 已重新构建、导出并安装。Godot headless 导出期间会报一次 `sts2.dll` 加载提示，但 PCK 打包和安装完成，脚本最终成功退出。
  - 2026-05-29 已将胖宝宝选人界面图打进 PCK，安装后的 `mod.pck` 大小约 1.4 MB。
  - 2026-05-29 已新增选人按钮小图和角色 UI 小洋葱头图标，并重新导出安装。
  - 2026-05-29 已把角色 UI 小图标改成白色正面洋葱头盔，并重新导出安装。
  - 2026-05-29 已把角色 UI 小图标顶部压低，改成更扁圆的洋葱头盔轮廓，并重新导出安装。
  - 2026-05-29 已修复点击开始旅途黑屏的顶栏头像异常，执行 `dotnet build mod.sln` 和 `./scripts/sts2_mod_dev.sh code` 通过；需要重启游戏加载新 DLL。
  - 2026-05-29 已修正顶栏小 UI 头像缩放方式，执行 `dotnet build workspace/mod/mod.sln` 和 `./scripts/sts2_mod_dev.sh code` 通过；需要重启游戏加载新 DLL。
  - 2026-05-30 已实现非草案猩红流卡牌并补充本地化，执行 `dotnet build workspace/mod/mod.sln`、JSON 校验和 `./scripts/sts2_mod_dev.sh all` 通过；需要重启游戏加载新 DLL/PCK。
  - 2026-05-30 已调整英文卡名为偏《艾尔登法环 / 黑夜君临》风格，接入 `CardKeyword.Ethereal`，并把 `猩红诱惑` 改为使用塔二选牌界面选择手牌消耗；执行 `dotnet build workspace/mod/mod.sln`、JSON 校验和 `./scripts/sts2_mod_dev.sh all` 通过。
  - 2026-05-30 已将 `PRAYER_STRIKE` / 祈祷一击从 `CardRarity.Common` 改为 `CardRarity.Uncommon`，与 `cards.md` 的罕见设计一致。
  - 2026-05-30 已实现非草案削韧流卡牌和支撑 Power，执行 JSON 校验、`dotnet build workspace/mod/mod.sln` 和 `./scripts/sts2_mod_dev.sh all` 通过；需要重启游戏加载新 DLL/PCK。
  - 2026-05-30 已将 `FLAME_PURIFY` / 火焰啊，净化调整为普通牌，新增 `BASIC_DEFENSE` / 防御，并将起始牌组改为 4 张攻击、4 张防御、1 张蓄力攻击、1 张反击。
  - 2026-05-30 已将 `GUARD_COUNTER_POWER` / 反击架势从本回合结束移除改为玩家下回合开始移除，使反击减伤覆盖敌方回合；排查其他自定义 Buff 后未发现同类持续时间问题。
  - 2026-05-30 已新增胖宝宝卡牌库解锁补丁，执行 `./scripts/sts2_mod_dev.sh test` 与 `./scripts/sts2_mod_dev.sh code` 通过；需要重启游戏加载新 DLL 后打开百科全书确认。
  - 2026-05-30 已实现通用充能循环：充能牌战斗开始进入耗尽牌堆，打出指定数量牌后回手，再次打出后重新进入充能；`GUARD_COUNTER` / 反击和 `TOTEM_TABLET` / 图腾碑石已接入。
  - 2026-05-30 修复充能循环：计数归零与重新充能改到 `AfterCardPlayedLate` 执行，避免回手时画面卡住；充能 Buff 仅在牌位于耗尽堆时显示；反击与图腾碑石仍用独立充能 Power 类型分开计数；`GUARD_COUNTER` 升级改为削韧 +1、充能 -1（与 `cards.md` 反击+ 一致）。
  - 2026-05-30 修复充能计数叠加/乱跳：`StartChargeCycle` 不再在未移除旧 Buff 时清空引用；计数改为 `ModifyAmount(silent)` 单步调层，避免反复 Remove+Apply；回手使用 `skipVisuals`。
  - 2026-05-30 修复充能归零卡屏：回手从 `AfterCardPlayedLate` 改为等触发充能的那张牌离开 `Play` 区后，在 `AfterCardChangedPilesLate` 再移入手牌，避免与打牌队列/动画死锁。
  - 2026-05-30 修复充能归零不回手：回手改走正常 `CardPileCmd.Add`，由触发充能归零的那张牌离开 `Play` 区后再回手；撤销直接操作 `NPlayerHand` / `NCard` 的 UI 补丁，避免破坏战斗场景节点；充能牌增加 `Retain` 以满足回合结束未打出保留在手中。
  - 2026-05-31 按用户确认重做充能循环：反击作为起始牌仍存在于卡堆，战斗开始移入消耗牌堆并设置充能；其他牌让计数递减，计数归零时立刻从消耗牌堆回到手牌；回手后因 `Retain` 保留且计数保持 0，直到打出反击并从 `Play` 进入 `Exhaust` 后重新设置充能值。同步补充静态测试检查充能开局入消耗、归零回手、打出后重置，以及充能牌 `Exhaust + Retain` 关键字基线。执行 `./scripts/sts2_mod_dev.sh test` 和 `./scripts/sts2_mod_dev.sh code` 通过并已安装。
  - 2026-05-30 继续回退不安全的资源/场景改动：移除自定义选人背景、封面资源和自定义战斗模型，避免覆盖整套选人界面或破坏战斗站位；充能回手移除 `BeforeFlush` 兜底，避免结束回合并行 hook 中移牌导致卡住。导出日志确认不再打包 `fat_baby_cover`、`fat_baby_select_bg`、`fat_baby_combat_idle`。
  - 2026-05-30 按用户给图重新接入角色美术：图一写入 `fat_baby_cover.png` 并作为 `CustomCharacterSelectBg` 与 `CustomCharacterSelectIconPath` 使用；图四写入 `fat_baby_combat_idle.png` 并由 `ScarletAcolyte.CreateCustomVisuals()` 直接创建普通 `NCreatureVisuals`，避免额外 Godot 全局脚本类缓存。
  - 2026-05-30 修复角色美术导入：用户给的图一文件扩展名为 `.png` 但内容实际是 JPEG，导致 Godot `.import` 为 `valid=false`、选人界面不显示；已转存为真正 RGBA PNG 并重新导入。图四用 Pillow 做边缘连通暗背景透明化后作为战斗立绘；静态测试和 C# 构建通过。
  - 2026-05-30 修复选人详情大图和头像裁切：`fat_baby_select_icon.png` 改为从 `fat_baby_cover.png` 中间裁切出的 132x195 竖版头像；`fat_baby_select_bg.tscn` 改用官方选人背景坐标系，避免详情页只显示暗化背景；战斗左上角 `CustomIconTexturePath` 按要求保持原 SVG 小 UI 资源。执行 `./scripts/sts2_mod_dev.sh test`、`./scripts/sts2_mod_dev.sh all` 与 `./scripts/sts2_mod_dev.sh code` 通过；需要重启游戏加载新 DLL/PCK。
  - 2026-05-30 二次修复选人大图叠层：按用户截图确认 `Backdrop` 和 `HeroPortrait` 同时引用同一张大图导致两层画面叠加；已将 `fat_baby_select_bg.tscn` 收敛为单一 `Cover` 大图层，底部角色小框继续使用 `fat_baby_select_icon.png` 裁切头像，战斗左上角小 UI 保持原 SVG 资源不变。执行 `./scripts/sts2_mod_dev.sh test` 和 `./scripts/sts2_mod_dev.sh all` 通过并已安装。
  - 2026-05-30 修复战斗人物本体显示：将 `fat_baby_combat_idle.png` 改为从 `onion_knight_ingame_sprite_v2_chromakey.png` 抠出的透明底局内立绘，并在 `ScarletAcolyte.CreateCustomVisuals()` 中把战斗人物缩放调整为 `0.35`、位置调整为 `(0, -180)`，同步调整 `Bounds`、`CenterPos` 和 `IntentPos`；战斗左上角小 UI 保持原 SVG 资源不变。执行 `./scripts/sts2_mod_dev.sh test`、`dotnet build workspace/mod/mod.sln` 和 `./scripts/sts2_mod_dev.sh all` 通过并已安装。
  - 2026-05-31 按用户要求暂时回退战斗人物本体：移除 `ScarletAcolyte.CreateCustomVisuals()` 自定义战斗立绘覆盖，让胖宝宝战斗中回到 `PlaceholderCharacterModel` 默认铁甲战士占位形态；选人大图、底部头像和战斗左上角小 UI 保持不变。执行 `./scripts/sts2_mod_dev.sh test` 和 `./scripts/sts2_mod_dev.sh code` 通过并已安装。
  - 2026-06-02 初次重新接入战斗静态立绘时，`ScarletAcolyte.CreateCustomVisuals()` 直接创建 `NCreatureVisuals`，但游戏内出现人物居中、敌人不显示，判断直接创建节点会破坏战斗房间布局流程。
  - 2026-06-02 已改为 `CustomVisualPath` + 无脚本 `fat_baby_combat_visual.tscn`：场景只包含 `Sprite2D`、`Bounds`、`CenterPos`、`IntentPos`，由 BaseLib `SceneConversionPatch` 自动转换为 `NCreatureVisuals`；避免场景绑定原版 `NCreatureVisuals.cs` 导出报错，也避免直接创建节点破坏站位。已将场景和战斗 PNG 加入运行期预加载列表，执行 `./scripts/sts2_mod_dev.sh test` 和 `./scripts/sts2_mod_dev.sh all` 通过并已安装。
  - 2026-05-30 已修复 `CARIAN_SLICER` / 卡利亚迅剑回手逻辑：不再生成自身复制牌，改为最后一次结算后、默认结果为弃牌时，把打出的原牌从 `Play` 移回手牌；同步中英文描述并补充静态测试基线。执行 `./scripts/sts2_mod_dev.sh test` 与 `./scripts/sts2_mod_dev.sh all` 通过；需要重启游戏加载新 DLL/PCK。
  - 2026-05-30 已统一中文卡牌描述标点：中间分句用 `，`，最后收尾用 `。`；`BLOOD_LEVY` / 鲜血征收、唤起风暴、特大跳劈等多段攻击牌统一写作 `削韧 n`，并在失衡侧栏说明“削韧 n 指每段攻击造成 n 点失衡值”。已补充静态测试规则防止回退，执行 `./scripts/sts2_mod_dev.sh test` 与 `./scripts/sts2_mod_dev.sh all` 通过；需要重启游戏加载新 DLL/PCK。
  - 2026-05-31 已补齐 `cards.md` 当前 47 张非升级卡：新增并注册 `TWIN_STING_POISON_FLOWER`（原 `BLOOM_TWICE` / 花开两度）、`TRUCE`、`INTIMIDATION`、`DETERMINATION`、`SEARCH`、`BATTLE_CRY`、`EASY_HANDLING`、`ENDURE`、`UNBURDEN`、`GOOD_LUCK`、`EMBER`、`WAIT_FOR_ME_TO_START`、`PERFECT_ROLL`、`MAGIC_REALM`、`TRAVEL_LIGHT`；同步中英文卡牌/Power 本地化、`cards.md`、卡牌状态 Canvas 和静态测试基线。执行 `./scripts/test_cards.sh` 与 `./scripts/sts2_mod_dev.sh all` 通过并已安装。

### 1. 编译 DLL

在终端运行:

```bash
cd /Users/carambola/Desktop/cursor+codex/mod/workspace/mod
dotnet build mod.sln
```

成功后 DLL 位于:

```text
/Users/carambola/Desktop/cursor+codex/mod/workspace/mod/.godot/mono/temp/bin/Debug/mod.dll
```

### 2. 用 Godot 导出 PCK

用 Godot 打开:

```text
/Users/carambola/Desktop/cursor+codex/mod/workspace/mod/project.godot
```

然后导出资源包，文件名需要是:

```text
mod.pck
```

当前没有正式卡图，卡牌会使用游戏自带缺省卡图，不影响机制测试。

### 3. 安装到游戏 mods 文件夹

在游戏的 `mods` 目录下建一个文件夹，例如:

```text
/Users/carambola/Library/Application Support/Steam/steamapps/common/Slay the Spire 2/SlayTheSpire2.app/Contents/MacOS/mods/mod
```

放入三个文件:

```text
mod.json
mod.dll
mod.pck
```

来源:

- `mod.json`: `/Users/carambola/Desktop/cursor+codex/mod/workspace/mod/mod.json`
- `mod.dll`: `/Users/carambola/Desktop/cursor+codex/mod/workspace/mod/.godot/mono/temp/bin/Debug/mod.dll`
- `mod.pck`: Godot 导出的 PCK。

### 4. 游戏内测试

启动游戏，在选人界面选择 `胖宝宝` 进入战斗。

打开控制台: 通常是反引号键 `` ` ``，如果不行可以试 `'`。

添加测试牌:

```text
card SCARLET_COST
card FLAME_PURIFY
card PRAYER_STRIKE
```

升级最左边手牌:

```text
upgrade_card 0
```

验证重点:

- `SCARLET_COST` 是否抽牌并叠腐败。
- 腐败是否最多 3 层。
- 下个你的回合开始时是否按层数失血。
- `FLAME_PURIFY` 是否移除腐败并给格挡。
- `PRAYER_STRIKE` 是否移除腐败并造成伤害。
- 敌人身上是否出现 `失衡` 计数。
- `PRAYER_STRIKE` 是否让目标失衡值减少 2。
- 失衡值被削到 0 时，敌人是否眩晕一回合，然后失衡值是否重置。
