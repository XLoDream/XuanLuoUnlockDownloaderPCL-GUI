# XuanLuoUnlockDownloaderPCL-GUI

恢复 PCL2 启动器中被屏蔽的 Minecraft 原版下载、版本修改和整合包安装入口。

**作者：璇洛**

## 特性

- **自动适配**：分析目标 exe 的 IL 结构定位修补点，不依赖固定版本号或文件指纹
- **安全流程**：所有修改写入新副本，原启动器文件不被覆盖
- **双向校验**：修补前后比对 SHA256 + 重新分析确认 ChangeCount 归零
- **整合包入口**：恢复本地导入、拖入和在线安装三条受限路径
- **命令行支持**：`--patch` / `--check` 适合自动化

## 运行

双击 `XuanLuoUnlockDownloaderPCL-GUI.exe`，或通过命令行：

```powershell
# 生成恢复副本
.\XuanLuoUnlockDownloaderPCL-GUI.exe --patch <PCL2.exe> <output.exe>

# 仅检测是否需要恢复（退出码 0=需要，2=不需要）
.\XuanLuoUnlockDownloaderPCL-GUI.exe --check <PCL2.exe>

# GUI，默认自动分析给定路径
.\XuanLuoUnlockDownloaderPCL-GUI.exe [PCL2.exe]
```

## 构建

前置：Windows 自带的 .NET Framework 4.x C# 编译器（`C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`）。

```powershell
.\build.ps1
```

输出：`XuanLuoUnlockDownloaderPCL-GUI.exe`

## 文件结构

```
Patcher/
├── MainForm.cs           # GUI + Program 入口
├── PatcherCore.cs        # IL 分析 + 原版下载 / 版本修改补丁
├── ModpackPatcher.cs     # 整合包安装方法体重建
├── build.ps1             # 一键构建脚本
├── dnlib.dll             # IL 操作库（嵌入到输出 exe）
├── logo.ico              # 应用图标
└── README.md
```

## 许可

仅用于学习与研究目的，严禁用于任何商业用途。
