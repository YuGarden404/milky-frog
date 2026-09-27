# 奶蛙桌宠

一个运行在 Windows 桌面上的奶蛙桌宠应用，使用 .NET 8 和 WPF 开发。

[![发布构建](https://github.com/YuGarden404/milky-frog/actions/workflows/release.yml/badge.svg)](https://github.com/YuGarden404/milky-frog/actions/workflows/release.yml)

## 立即下载

前往 **[GitHub Releases 下载页面](https://github.com/YuGarden404/milky-frog/releases/latest)**。

在最新版本的 Assets 中选择：

- **MilkyFrog-Setup-x.x.x.exe**：推荐普通用户使用。双击安装，可创建开始菜单和桌面快捷方式，也支持卸载。
- **MilkyFrog-win-x64-x.x.x.zip**：免安装便携版。解压后运行 MilkyFrog.exe，适合临时体验或不想安装到系统的用户。
- **SHA256SUMS.txt**：可选，用于校验下载文件是否完整。

本项目目前提供 Windows x64 版本，不需要另外安装 .NET 运行时。

## 功能

- 奶蛙常驻桌面，并保持在其他窗口上方。
- 奶蛙只能在屏幕工作区域内拖动，不会进入任务栏或超出屏幕边界。
- 鼠标移动时，奶蛙会跟随鼠标移动视线。
- 双击奶蛙会播放大笑动画和笑声。
- 右键奶蛙可以隐藏奶蛙或退出应用。
- 系统托盘提供显示、隐藏、静音和音频输出设备选项。
- 托盘图标左键单击时，在奶蛙隐藏状态下可以重新显示奶蛙。
- 支持单实例运行，重复启动不会打开多个奶蛙。
- 支持透明像素动画和自定义应用图标。

## 安装与运行

### 安装版

1. 在 [Releases](https://github.com/YuGarden404/milky-frog/releases/latest) 下载 MilkyFrog-Setup-x.x.x.exe。
2. 双击安装包，按向导选择安装位置和快捷方式。
3. 安装完成后启动“奶蛙桌宠”。

首次下载未签名的安装包时，Edge 或 Windows SmartScreen 可能显示安全提示。确认文件来自本仓库后，在下载项中选择“保留”，在 SmartScreen 窗口中选择“更多信息 → 仍要运行”。

### 便携版

下载 ZIP 后解压，直接运行目录中的 MilkyFrog.exe。便携版不写入安装目录，但应用运行所需的动画、音频和图标文件必须与程序保持在同一目录结构中。

## 项目结构

~~~text
src/
  MilkyFrog.Core/              与平台无关的动画模型和服务接口
  MilkyFrog.Platform.Windows/  Windows 托盘、音频、鼠标和单实例服务
  MilkyFrog.App/               WPF 窗口、动画播放、资源目录和程序入口
tests/
  MilkyFrog.Core.Tests/        Core 层单元测试
tools/
  GeneratePixelFrogAssets.ps1  Windows 素材生成入口
  generate_video_pixel_frog.py 视频抽帧和透明像素处理
  create_icon_from_image.py   透明 Windows 图标生成
installer/
  MilkyFrog.iss                Inno Setup 安装包脚本
~~~

## 开发者构建

~~~powershell
dotnet restore
dotnet test .\MilkyFrog.sln
dotnet build .\MilkyFrog.sln
dotnet run --project .\src\MilkyFrog.App\MilkyFrog.App.csproj
~~~

本地构建安装包需要安装 [Inno Setup 6](https://jrsoftware.org/isinfo.php)，然后执行：

~~~powershell
dotnet publish .\src\MilkyFrog.App\MilkyFrog.App.csproj -c Release -r win-x64 --self-contained true -o .\artifacts\publish
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "/DMyAppVersion=0.1.0" .\installer\MilkyFrog.iss
~~~

安装包会输出到 artifacts/installer。

推送版本标签即可触发 GitHub Actions 自动发布：

~~~powershell
git add .
git commit -m "描述本次修改"
git push
git tag v0.1.1
git push origin v0.1.1
~~~
