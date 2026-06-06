# 编译依赖

从本机已安装的 **Slay the Spire 2** 复制下列文件到 `deps/game/`（文件名需一致）：


| 文件             | macOS 常见来源                                                            |
| -------------- | --------------------------------------------------------------------- |
| `sts2.dll`     | `SlayTheSpire2.app/Contents/Resources/data_sts2_macos_arm64/sts2.dll` |
| `BaseLib.dll`  | `mods/【001】必装前置/.../BaseLib.dll`                                      |
| `0Harmony.dll` | 同上 `data_sts2_macos_arm64/0Harmony.dll`                               |


Windows 开发者：从 Steam 安装目录的 `data_sts2_win64`（或当前平台数据目录）与 `mods` 里对应路径复制到同一文件夹。

也可设置环境变量 `STS2_STABLE_GAME_DIR` 指向稳定版游戏根目录，由 `mod.csproj` 自动解析（见仓库 README）。`STS2_GAME_DIR` 仍可作为兼容 fallback。

Beta 分支开发见 `deps/game-beta/README.md` 与 `workspace/mod-beta/`。
