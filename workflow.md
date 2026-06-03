# 人物 Mod 工作流

这个文件用来记录我们每次实现任务时的推进方式，避免因为聊天中断、网络波动或上下文压缩导致工作需要重来。

## 固定流程

每次开始一个实现任务前，先在本文件新增一个任务记录，并写出小任务清单。

流程:

1. 明确本次目标。
2. 读取相关文件，确认当前状态。
3. 把目标拆成多个小任务。
4. 每完成一个小任务，就把对应清单项从 `[ ]` 改成 `[x]`。
5. 实现完成后运行可行的验证命令。
6. 把结果同步到 `implementation_log.md`。
7. 如果发现新问题或待确认规则，记录到本文件的“待确认/后续”。

## 任务记录模板

复制下面这一段作为每次新任务的起点。

```md
## YYYY-MM-DD 任务名

目标:

- 

任务清单:

- [ ] 确认相关文件和当前实现状态
- [ ] 明确本次要改的具体规则/数值/资源
- [ ] 实现代码或资源改动
- [ ] 补充/更新本地化、配置或文档
- [x] 运行构建或其他可行验证
- [x] 更新 `implementation_log.md`

验证:

- [ ] 

结果:

- 

待确认/后续:

- 
```

## 当前任务

## 2026-06-02 修复战斗人物替换站位

目标:

- 修复直接创建 `NCreatureVisuals` 后进入战斗人物居中、敌人不显示的问题。
- 改用 BaseLib 场景自动转换路径，让战斗视觉走更接近参考 mod 的流程。

任务清单:

- [x] 确认战斗房间站位依赖 `Visuals.Bounds.Size`
- [x] 确认 BaseLib 支持无脚本场景自动转换为 `NCreatureVisuals`
- [x] 改为 `CustomVisualPath` + 无脚本战斗视觉场景
- [x] 更新预加载和项目记录
- [x] 运行测试和完整导出

验证:

- [x] `./scripts/sts2_mod_dev.sh test`
- [x] `./scripts/sts2_mod_dev.sh all`

结果:

- 已撤掉直接创建 `NCreatureVisuals` 的方案。
- 已新增无脚本 `fat_baby_combat_visual.tscn`，由 `CustomVisualPath` 接入，并交给 BaseLib 场景自动转换为 `NCreatureVisuals`。
- 已完整导出并安装，需要重启游戏确认战斗布局。

待确认/后续:

- 游戏内确认人物不再居中、敌人正常显示、血条/意图位置正常。

## 2026-06-02 替换战斗人物静态立绘

目标:

- 用现有 `fat_baby_combat_idle.png` 替换战斗中的铁甲战士占位视觉。
- 采用静态 `NCreatureVisuals` 节点结构，先保证进战斗、血条、意图和受击位置稳定。

任务清单:

- [x] 确认相关文件和当前实现状态
- [x] 明确本次要改的具体资源和替换入口
- [x] 新增直接创建的战斗视觉节点并接入角色代码
- [x] 补充资源预加载列表
- [x] 运行构建/导出验证
- [x] 更新 `implementation_log.md`

验证:

- [x] `./scripts/sts2_mod_dev.sh test`
- [x] `./scripts/sts2_mod_dev.sh all`

结果:

- 已用 `ScarletAcolyte.CreateCustomVisuals()` 直接创建静态战斗视觉，使用 `fat_baby_combat_idle.png` 替换铁甲战士占位本体。
- 已移除 `fat_baby_combat_visual.tscn` 方案，避免 Godot 导出 mod PCK 时读取不到原版 `NCreatureVisuals.cs`。
- 已完整导出并安装，需要重启游戏加载新 DLL/PCK。

待确认/后续:

- 游戏内确认战斗人物尺寸、脚底落点、血条/意图位置和透明边缘。

## 2026-05-31 补齐当前卡牌设计

目标:

- 补齐 `cards.md` 当前列出的 47 张非升级卡。
- 注册所有新增卡牌，并同步中英文卡牌/Power 本地化。
- 更新静态测试基线，确保卡池、起始牌组、本地化和关键词规则一致。

任务清单:

- [x] 确认 `cards.md` 当前卡牌清单和已实现状态
- [x] 新增并注册缺失卡牌
- [x] 补充相关 Power 和卡牌效果实现
- [x] 同步中英文本地化和文档状态
- [x] 更新静态测试基线
- [x] 运行测试和完整导出
- [x] 更新 `implementation_log.md`

验证:

- [x] `./scripts/sts2_mod_dev.sh test`
- [x] `./scripts/sts2_mod_dev.sh all`

结果:

- 已实现并注册 47 张当前非升级卡。
- 已新增 `TWIN_STING_POISON_FLOWER`（双刺毒花）、`TRUCE`、`INTIMIDATION`、`DETERMINATION`、`SEARCH`、`BATTLE_CRY`、`EASY_HANDLING`、`ENDURE`、`UNBURDEN`、`GOOD_LUCK`、`EMBER`、`WAIT_FOR_ME_TO_START`、`PERFECT_ROLL`、`MAGIC_REALM`、`TRAVEL_LIGHT`。
- 已同步中英文卡牌/Power 本地化、`cards.md`、卡牌状态 Canvas 和静态测试基线。
- 已完整导出并安装，需要重启游戏加载。

待确认/后续:

- 游戏内抽查新增卡牌效果，尤其是跨回合 Power、选牌/消耗手牌、魔力和手牌保留类效果。

## 2026-05-31 修复反击充能回手

目标:

- 反击作为起始牌存在于卡堆中。
- 战斗开始时将反击移入消耗牌堆并设置充能计数。
- 充能计数归零时，从消耗牌堆移回手牌。
- 反击在手牌中保留且计数保持 0，直到打出后进入消耗牌堆并重新设置充能值。

任务清单:

- [x] 确认当前充能牌实现和反击起始牌状态
- [x] 修复充能归零回手和打出后重置逻辑
- [x] 补充静态测试防止回退
- [x] 运行测试和安装
- [x] 更新 `implementation_log.md`

验证:

- [x] `./scripts/sts2_mod_dev.sh test`
- [x] `./scripts/sts2_mod_dev.sh code`

结果:

- 已确认反击仍在胖宝宝起始牌组中。
- 已将充能归零逻辑改为当场从消耗牌堆回到手牌，不再等待触发牌离开 `Play` 区。
- 已将反击打出后的充能重置改到自身从 `Play` 进入 `Exhaust` 的牌堆变更事件中。
- 已将充能牌测试基线更新为 `消耗 + 保留`，并补充充能生命周期静态检查。
- 已安装新 DLL，需要重启游戏加载。

待确认/后续:

- 游戏内确认反击开局进入消耗堆，充能归零后回手，打出后重新进入消耗堆并重置计数。

## 2026-05-31 回退战斗人物本体

目标:

- 暂时不替换战斗人物本体，等之后和卡面一起统一制作。
- 回到之前的铁甲战士占位战斗人物。
- 保留选人界面资源和战斗左上角小 UI。

任务清单:

- [x] 确认当前战斗人物替换入口
- [x] 移除自定义战斗人物覆盖
- [x] 更新项目记录
- [x] 运行测试和安装

验证:

- [x] `./scripts/sts2_mod_dev.sh test`
- [x] `./scripts/sts2_mod_dev.sh code`

结果:

- 已移除 `ScarletAcolyte.CreateCustomVisuals()` 自定义战斗本体覆盖。
- 胖宝宝战斗人物本体回到 `PlaceholderCharacterModel` 的默认铁甲战士占位形态。
- 选人大图、底部头像、战斗左上角小 UI 均保持不变。
- 已安装新 DLL，需要重启游戏加载。

待确认/后续:

- 后续和卡面一起重新制作战斗人物本体。

## 2026-05-30 修复战斗人物立绘

目标:

- 修复战斗内胖宝宝人物本体显示不正确的问题。
- 只调整战斗人物立绘、缩放、站位和边界，不改战斗左上角小 UI。

任务清单:

- [x] 确认当前战斗人物资源和代码状态
- [x] 重新生成透明底局内人物立绘
- [x] 调整战斗人物缩放、站位和边界
- [x] 运行测试和完整导出
- [x] 更新 `implementation_log.md`

验证:

- [x] `./scripts/sts2_mod_dev.sh test`
- [x] `./scripts/sts2_mod_dev.sh all`

结果:

- 已将战斗人物立绘改为从 `onion_knight_ingame_sprite_v2_chromakey.png` 抠出的透明底 PNG。
- 已调整战斗内人物缩放为 `0.35`，站位为 `(0, -180)`，并同步上移边界、中心点和意图点。
- 战斗左上角小 UI 没有改动。
- 已完整导出并安装，需要重启游戏加载新 DLL/PCK。

待确认/后续:

- 重启游戏后确认战斗人物本体尺寸、落点和透明边缘是否正常。

## 2026-05-30 修复选人大图和头像裁切

目标:

- 修复选人详情页胖宝宝大图看起来为空白/只有暗背景的问题。
- 将底部选人按钮头像改为从大图中间裁出的竖版头像。
- 保持战斗左上角角色小 UI 不变。

任务清单:

- [x] 确认当前角色代码、选人场景和图片资源
- [x] 重新裁切选人按钮头像资源
- [x] 调整选人背景场景，让大图清晰显示
- [x] 保留战斗左上角小 UI 资源
- [x] 运行构建和完整导出
- [x] 更新 `implementation_log.md`

验证:

- [x] `./scripts/sts2_mod_dev.sh test`
- [x] `./scripts/sts2_mod_dev.sh all`
- [x] `./scripts/sts2_mod_dev.sh code`

结果:

- 已将 `fat_baby_select_icon.png` 改为从 `fat_baby_cover.png` 中间裁切出的 132x195 竖版头像。
- 已按要求恢复战斗左上角 `CustomIconTexturePath` 为原 SVG 小 UI 资源，不改这个位置。
- 已调整 `fat_baby_select_bg.tscn`：使用官方选人背景的 2560x1200 坐标系，并改为单一 `Cover` 大图层，避免同一张大图叠两层。
- 底部选人小框继续使用 `fat_baby_select_icon.png`，也就是从大图中间裁切出的竖版头像。
- 已完整导出并安装，需要重启游戏加载新 DLL/PCK。

待确认/后续:

- 重启游戏后确认选人详情页大图和底部选人按钮已更新，战斗左上角小 UI 保持原样。

## 2026-05-30 统一卡牌中文表述和削韧说明

目标:

- 统一中文卡牌描述标点：中间分句用 `，`，最后收尾用 `。`。
- 将多段攻击牌的文案统一成 `削韧 n`，通过侧栏解释“每段攻击造成 n 点失衡值”。
- 在削韧相关卡牌侧栏补足失衡说明。

任务清单:

- [x] 确认中文卡牌和 Power 本地化现状
- [x] 更新中文卡牌描述和鲜血征收表述
- [x] 更新失衡/削韧侧栏说明和提示挂载
- [x] 补充静态测试规则
- [x] 运行测试和完整导出
- [x] 更新 `implementation_log.md`

验证:

- [x] `./scripts/sts2_mod_dev.sh test`
- [x] `./scripts/sts2_mod_dev.sh all`

结果:

- 已统一中文卡牌描述标点，卡牌描述中间分句使用 `，`，最后一行使用 `。`。
- `BLOOD_LEVY` / 鲜血征收、唤起风暴、特大跳劈等多段攻击牌改为 `削韧 n` 表述。
- 失衡侧栏现在解释“削韧 n 指每段攻击造成 n 点失衡值；敌人的失衡值为 0 时进入眩晕状态”。
- 岩石剑和巨剑阵已补上失衡侧栏提示。
- 静态测试已新增中文标点、`每段削韧` 禁用、失衡 tooltip 挂载检查。
- 已完整导出并安装，需要重启游戏加载新 DLL/PCK。

待确认/后续:

- 重启游戏后实际查看卡牌详情和侧栏 tooltip 是否排版正常。

## 2026-05-30 修正基础牌和起始牌组

目标:

- 将 `火焰啊，净化` 调整为普通牌。
- 将其他牌限定为 `攻击`、`防御`、`蓄力攻击`、`反击`。
- 起始牌组调整为 4 张攻击、4 张防御、1 张蓄力攻击、1 张反击。

任务清单:

- [x] 实现 `防御` 基础牌
- [x] 调整 `火焰啊，净化` 稀有度
- [x] 调整出生自带牌组
- [x] 更新文档与本地化
- [x] 构建导出验证

验证:

- [x] `dotnet build workspace/mod/mod.sln`
- [x] JSON 本地化文件校验
- [x] `./scripts/sts2_mod_dev.sh all`

结果:

- 已将 `FLAME_PURIFY` / 火焰啊，净化从基础/其他牌改为普通牌。
- 已新增 `BASIC_DEFENSE` / 防御基础牌，并加入 `ScarletCardPool`。
- 已将胖宝宝起始牌组改为 4 张 `BASIC_ATTACK`、4 张 `BASIC_DEFENSE`、1 张 `CHARGED_ATTACK`、1 张 `GUARD_COUNTER`。
- 已同步更新本地化、`cards.md`、状态 Canvas、`tests/card_expectations.json` 和 `implementation_log.md`。
- 已通过 `./scripts/sts2_mod_dev.sh test` 和 `./scripts/sts2_mod_dev.sh all`，需要重启游戏加载。

## 2026-05-30 建立卡牌系统测试入口

目标:

- 建立不进入游戏也能运行的卡牌自动测试，覆盖卡牌注册、本地化、数值基线和构建。
- 梳理后续战斗结算测试如何从手动点游戏升级为脚本化验证。

任务清单:

- [x] 确认当前卡牌代码结构和可自动验证范围
- [x] 新增卡牌静态测试脚本和统一测试入口
- [x] 跑通自动测试并修复暴露的问题
- [x] 更新测试说明和实现日志

验证:

- [x] `./scripts/test_cards.sh`
- [x] `./scripts/sts2_mod_dev.sh test`

结果:

- 已新增 `tests/card_expectations.json`、`scripts/test_cards.mjs`、`scripts/test_cards.sh` 和 `testing.md`。
- 已接入 `./scripts/sts2_mod_dev.sh test`。
- 首轮自动测试通过，覆盖 31 张卡和中英文卡牌本地化，并执行 `dotnet build workspace/mod/mod.sln`。
- 已补充 `MaxUpgradeLevel` 静态检查，用于确认卡牌不会被篝火敲牌池排除；并在 `testing.md` 记录房间/敲牌/战斗功能烟测口径。

待确认/后续:

- 需要进一步做战斗结算层测试时，优先把猩红腐败、失衡、削韧触发等规则拆出可纯逻辑验证的核心。
- 游戏运行时建议新增 debug-only 控制台命令：`scarlet_test upgrade_all`、`scarlet_test rooms`、`scarlet_test combat_core`。

## 2026-05-30 实现削韧流卡牌

目标:

- 实现 `cards.md` 中削韧流相关卡牌，暂不实现草案区卡牌。
- 优先接入已有失衡/削韧系统和塔二已有关键词、选牌、弃牌/消耗等 API。

任务清单:

- [x] 确认削韧流非草案范围与相关 API
- [x] 实现攻击类削韧卡牌
- [x] 实现技能/能力类削韧支撑卡牌
- [x] 补充本地化并加入 `ScarletCardPool`
- [x] 运行构建和完整导出验证
- [x] 更新 `implementation_log.md`

验证:

- [x] `dotnet build workspace/mod/mod.sln`
- [x] JSON 本地化文件校验
- [x] `./scripts/sts2_mod_dev.sh all`

结果:

- 已实现 `cards.md` 非草案削韧流卡牌并加入 `ScarletCardPool`：基础攻击、蓄力攻击、反击、唤起风暴、黄金树坠落震击、拳套打击、盾牌冲击、致命一击、特大跳劈、卡利亚迅剑、箭步-上砍、准备架势、卡利亚贯刺、狮子斩、图腾碑石、岩石剑、巨剑阵、乘胜追击。
- 已补充中英文卡牌/Power 本地化，并更新 `cards.md`、卡牌状态 Canvas 和 `implementation_log.md`。
- 已完整导出并安装，需要重启游戏加载新的 DLL/PCK。

待确认/后续:

- 充能机制暂未找到现成 API，本次先实现 `反击`、`图腾碑石` 的主体效果，充能回流后续补充。

## 2026-05-30 祈祷一击改为罕见

目标:

- 将 `PRAYER_STRIKE` / 祈祷一击的代码稀有度调整为罕见，与 `cards.md` 设计一致。

任务清单:

- [x] 修改 `PrayerStrike` 的 `CardRarity`
- [x] 同步更新文档和卡牌状态 Canvas
- [x] 运行构建和快速安装验证

验证:

- [x] `dotnet build workspace/mod/mod.sln`
- [x] `./scripts/sts2_mod_dev.sh code`

结果:

- 已将 `PRAYER_STRIKE` / 祈祷一击从 `CardRarity.Common` 改为 `CardRarity.Uncommon`。
- 已快速编译并安装新的 `mod.dll`，需要重启游戏加载。

待确认/后续:

- 无。

## 2026-05-30 英文命名与已有关键词机制

目标:

- 将英文卡名调整为更贴近《艾尔登法环 / 黑夜君临》战技、人物、道具命名的风格。
- 优先接入塔二已有关键词机制，如消耗、虚无、保留、固有等，避免重复造机制。

任务清单:

- [x] 确认塔二已有关键词枚举和当前卡牌需要接入的机制
- [x] 调整英文卡名与描述措辞
- [x] 接入已有消耗、虚无、保留、固有等关键词
- [x] 运行构建和完整导出验证
- [x] 更新 `implementation_log.md`

验证:

- [x] `dotnet build workspace/mod/mod.sln`
- [x] JSON 本地化文件校验
- [x] `./scripts/sts2_mod_dev.sh all`

结果:

- 已将英文卡名改为更贴近《艾尔登法环 / 黑夜君临》风格，如 `Prayerful Strike`、`Flame, Cleanse Me`、`Blood Tax`、`Taker's Flames`、`Law of Regression`、`Endure`。
- 已确认并接入塔二已有关键词：`Exhaust`、`Ethereal`、`Retain`、`Innate`；`垃圾王的庇佑` 现在使用 `CardKeyword.Ethereal`。
- `猩红诱惑` 的每回合消耗手牌改为使用塔二已有选牌界面选择 1 张手牌消耗。
- 已完整导出并安装，需要重启游戏加载新的 DLL/PCK。

待确认/后续:

- 若塔二已有关键词枚举名与预期不同，先用编译结果确认后再落地。

## 2026-05-30 实现猩红流卡牌

目标:

- 实现 `cards.md` 中猩红流相关卡牌，暂不实现草案区卡牌。
- 优先接入已有猩红腐败机制，必要时新增配套 Power。

任务清单:

- [x] 确认猩红流卡牌范围和当前可用 API
- [x] 实现猩红流卡牌与必要 Power/机制
- [x] 补充本地化并加入 `ScarletCardPool`
- [x] 运行构建和快速安装验证
- [x] 更新 `implementation_log.md`

验证:

- [x] `dotnet build workspace/mod/mod.sln`
- [x] JSON 本地化文件校验
- [x] `./scripts/sts2_mod_dev.sh all`

结果:

- 已实现非草案猩红流卡牌并加入 `ScarletCardPool`：`SCARLET_CREDIT`、`CORRUPTION_STRENGTH`、`CORRUPTION_SENSE`、`SCARLET_TEMPTATION`、`GARBAGE_KING_BLESSING`、`RETURN_PRINCIPLE`、`JACK_WINE`、`PLUNDERING_FIRE`、`BLOOD_LEVY`、`WILL_TO_WIN`。
- 已补充中英文卡牌/Power 本地化，并更新 `cards.md`、卡牌状态 Canvas 和 `implementation_log.md`。
- 已完整导出并安装，需要重启游戏加载新的 DLL/PCK。

待确认/后续:

- `垃圾王的庇佑` 的虚无关键词机制暂未实现，本次先实现打出后的持续能力效果。

## 2026-05-29 削韧流与猩红流卡牌盘点

目标:

- 列出削韧流和猩红流相关卡牌，标记哪些已经制作、哪些还停留在设计稿。

任务清单:

- [x] 确认 `cards.md`、已实现卡牌代码和本地化当前状态
- [x] 整理削韧流卡牌制作状态
- [x] 整理猩红流卡牌制作状态
- [x] 生成可查看的卡牌盘点 Canvas
- [x] 更新 `implementation_log.md`

验证:

- [x] 读取卡牌代码与文档交叉确认

结果:

- 已生成 `scarlet-card-status.canvas.tsx`，按削韧流和猩红流列出卡牌制作状态。
- 当前代码注册卡牌仍为 3 张：`SCARLET_COST`、`FLAME_PURIFY`、`PRAYER_STRIKE`。

待确认/后续:

- 后续建议从基础攻击/防御、`斧柄突刺`、`致命一击`、`乘胜追击`、`赊账` 或 `腐败感知` 中确定下一批优先实现。

## 2026-05-29 调整顶栏小 UI 头像

目标:

- 修正进入旅途后胖宝宝左上角小 UI 头像过大、位置偏左上的问题。

任务清单:

- [x] 确认当前 `CustomIcon` 实现和截图表现
- [x] 调整头像控件缩放和居中方式
- [x] 运行构建或其他可行验证
- [x] 更新 `implementation_log.md`

验证:

- [x] `dotnet build workspace/mod/mod.sln`
- [x] `./scripts/sts2_mod_dev.sh code`

结果:

- 已将顶栏头像 `TextureRect` 设置为忽略纹理原始尺寸，并按比例居中缩放，避免 SVG 以原始画布大小挤出头像框。
- 已快速编译并安装新的 `mod.dll`，需要重启游戏后目测确认。

待确认/后续:

- 需要进游戏目测头像是否居中、大小是否合适。

## 2026-05-29 修复开始旅途黑屏

目标:

- 修复选择胖宝宝后点击开始旅途黑屏、无法进入后续流程的问题。

任务清单:

- [x] 确认日志、角色入口、资源路径和当前实现状态
- [x] 定位点击开始旅途后黑屏的直接原因
- [x] 实现最小修复并保持现有工程结构
- [x] 运行构建或其他可行验证
- [x] 更新 `implementation_log.md`

验证:

- [x] `dotnet build mod.sln`
- [x] `./scripts/sts2_mod_dev.sh code`

结果:

- 已修复 `CustomIcon` 使用 BaseLib `NodeFactory` 从字符串路径生成 Control 导致的异常；现在直接创建 `TextureRect` 并加载 `fat_baby_onion_icon.svg`。
- 已快速编译并安装新的 `mod.dll`，需要重启游戏后再测试点击开始旅途。

待确认/后续:

- 若重启后仍黑屏，继续看最新 `godot.log` 中是否还有后续异常。

## 2026-05-29 更新聊天承接文档

目标:

- 更新 `chat_handoff.md`，让切换到新聊天后可以立刻承接并开始执行任务。

任务清单:

- [x] 确认相关文件和当前实现状态
- [x] 重写 `chat_handoff.md`
- [x] 检查承接文档包含路径、当前状态、执行规则和测试方式
- [x] 更新 `workflow.md` 当前任务状态

验证:

- [x] 读取 `chat_handoff.md` 确认内容完整

结果:

- 已重写 `chat_handoff.md`，新聊天可直接阅读后承接当前项目并开始执行任务。

待确认/后续:

- 后续切聊天时，把新任务写在新聊天里即可；新聊天应先读 `chat_handoff.md`、`workflow.md`、`implementation_log.md`。

## 2026-05-29 局内 Sprite 概念 + 卡牌文档

目标:

- 基于母图生成更适合局内的 sprite 概念图（含绿幕抠图版）。
- 整理 `cards.md`：实现状态、纠错、主题牌草案。
- 记录 sprite 进游戏管线。

任务清单:

- [x] 确认相关文件和当前实现状态
- [x] 生成 `onion_knight_ingame_sprite_v1` 与 chromakey 版
- [x] 更新 `cards.md` 与 `sprite_pipeline.md`
- [x] 更新 `chat_handoff.md`

验证:

- [x] 素材已写入 `assets/character_sprites/`

结果:

- 新增局内概念图两张；卡牌文档已补实现状态与 4 组主题牌草案。

待确认/后续:

- 是否用 v1 抠图接 `CustomVisualPath` 静态立绘，或继续迭代 Prompt（内圈兽头、后半锯齿）。
- 从草案中选 2–3 张牌优先写 C#（建议：防御/攻击基础牌 + 斧柄突刺）。

## 2026-05-29 胖宝宝扁平 UI 图标

目标:

- 把胖宝宝白色 UI 洋葱头盔改得更扁平，减少顶部尖锐感。

任务清单:

- [x] 确认相关文件和当前实现状态
- [x] 调整 `fat_baby_onion_icon.svg` 轮廓
- [x] 运行构建/导出验证
- [x] 更新 `implementation_log.md`

验证:

- [x] `dotnet build mod.sln`
- [x] `./scripts/sts2_mod_dev.sh all`

结果:

- 已把 UI 洋葱头盔顶部压低，整体改成更扁圆的轮廓，并重新导出安装。

待确认/后续:

- 如果游戏内仍然显尖，可以继续把顶部起点下移到 55-60，并进一步降低顶部脊线。

## 2026-05-29 胖宝宝白色 UI 图标

目标:

- 把胖宝宝角色 UI 小图标改成正面洋葱头盔。
- 图标改为白色，透明背景，适合游戏深色 UI。

任务清单:

- [x] 确认相关文件和当前实现状态
- [x] 重绘 `fat_baby_onion_icon.svg`
- [x] 运行构建/导出验证
- [x] 更新 `implementation_log.md`

验证:

- [x] `dotnet build mod.sln`
- [x] `./scripts/sts2_mod_dev.sh all`

结果:

- 已将胖宝宝角色 UI 小图标改为白色正面洋葱头盔，并重新导出安装。

待确认/后续:

- 如果游戏内觉得白色细节不够明显，可以再加粗轮廓或改成纯白剪影版。

## 2026-05-29 胖宝宝选人小图标

目标:

- 制作胖宝宝选人按钮小图，让角色列表里一眼能看出是这个角色。
- 制作图二位置使用的小洋葱头图标。

任务清单:

- [x] 确认相关文件和当前实现状态
- [x] 生成选人按钮小图
- [x] 绘制小洋葱头图标
- [x] 在角色模型中接入按钮小图和小图标
- [x] 运行构建/导出验证
- [x] 更新 `implementation_log.md`

验证:

- [x] `dotnet build mod.sln`
- [x] `./scripts/sts2_mod_dev.sh all`

结果:

- 已生成并接入 `fat_baby_select_icon.png` 作为选人按钮小图，接入 `fat_baby_onion_icon.svg` 作为角色 UI 小图标。

待确认/后续:

- 如果游戏内 SVG 图标显示异常，可以再改为透明 PNG 渲染版本。

## 2026-05-29 胖宝宝选人界面立绘

目标:

- 使用 `assets/character_covers/onion_knight_toast_cover_v5_brighter_medium.png` 作为胖宝宝选人界面能看到的版面/立绘。

任务清单:

- [x] 确认相关文件和当前实现状态
- [x] 对照参考 Mod 确认 BaseLib 自定义选人图路径
- [x] 复制图片到 mod 工程资源目录
- [x] 在角色模型中接入选人界面图片/背景
- [x] 补充 Godot 导入资源需要的 `.import` 文件
- [x] 运行构建/导出验证
- [x] 更新 `implementation_log.md`

验证:

- [x] `dotnet build mod.sln`
- [x] `./scripts/sts2_mod_dev.sh all`

结果:

- 已把洋葱骑士举杯图接入胖宝宝的选人界面背景和角色按钮图标，并完成导出安装。

待确认/后续:

- 当前同一张图同时用于选中后的大背景和角色按钮小图；如果按钮裁切效果不理想，后续可以单独做一张头像裁切图。

## 2026-05-29 胖宝宝与失衡系统

目标:

- 把当前人物显示名改为“胖宝宝”。
- 实现失衡系统的第一版：敌人拥有失衡值，卡牌削韧减少失衡值，归零后给予眩晕一回合。
- 对照 `cards.md` 判断还需要补充哪些失衡规则。

任务清单:

- [x] 确认相关文件和当前实现状态
- [x] 明确失衡当前能落地的规则和缺口
- [x] 实现失衡 Power、眩晕 Power 和工具方法
- [x] 让已有攻击牌接入削韧
- [x] 把人物显示名改为胖宝宝
- [x] 补充/更新本地化、配置或文档
- [x] 运行构建或其他可行验证
- [x] 更新 `implementation_log.md`

验证:

- [x] `dotnet build mod.sln`
- [x] `./scripts/sts2_mod_dev.sh all`

结果:

- 已实现胖宝宝显示名与失衡系统第一版，并已构建、导出、安装到游戏实际扫描的 mods 目录。

待确认/后续:

- 需要确认失衡是否应被 Artifact/免疫负面效果影响。
- 需要确认每个敌人的失衡值是否后续要做精确表，而不是按普通/精英/Boss 粗分。
- 需要确认眩晕后重置为满失衡，还是重置为其他值/获得短暂无敌失衡保护。

## 2026-05-29 建立工作流记录

目标:

- 建立一个专门的工作流文档，用来记录后续人物 Mod 任务的拆分、完成状态和验证结果。

任务清单:

- [x] 确认当前工作区和已有文档
- [x] 新建 `workflow.md`
- [x] 写入固定流程和任务记录模板
- [ ] 后续每次具体实现任务前，先在本文件新增任务清单

验证:

- [x] 确认文件已创建

结果:

- 已创建 `workflow.md`，后续人物、卡牌、机制、贴图、导出相关任务都可以按这个文件记录。

待确认/后续:

- 下一个实现任务开始时，把任务拆成小项写到“当前任务”下方，并随着完成逐项勾选。
