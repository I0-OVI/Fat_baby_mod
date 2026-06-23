# Beta 编译依赖

用于 `workspace/mod-beta/`（Steam **beta 测试分支**）的引用 DLL。

从 **已切换到 beta 分支** 的 Slay the Spire 2 安装目录复制下列文件到本文件夹（文件名需一致）：

| 文件 | macOS 常见来源 |
|------|----------------|
| `sts2.dll` | `SlayTheSpire2.app/Contents/Resources/data_sts2_macos_arm64/sts2.dll` |
| `BaseLib.dll` | `mods/【001】必装前置/BaseLib（x.y.z）/BaseLib.dll`（须与 beta 游戏匹配） |
| `0Harmony.dll` | 同上 `data_sts2_macos_arm64/0Harmony.dll` |

Windows：从 `data_sts2_win64`（或当前平台数据目录）与 `mods` 对应路径复制。

也可设置环境变量 `STS2_BETA_GAME_DIR` 指向 beta 游戏根目录，由 `workspace/mod-beta/GameRefs.props` 自动解析。

**注意：** beta 与稳定版 `deps/game/` 不要混用；游戏更新 beta 后需重新复制 DLL。
