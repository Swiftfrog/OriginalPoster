# Original Poster 原语言海报插件 Emby Plugin
### **自动获取电影/剧集原语言海报与 Logo**

> ✅ **基于 Emby 4.9.1.x** | ✅ **支持 TMDB 原语言海报与 Logo**

---

## 📌 插件简介

这是一个专为 **Emby 媒体服务器** 开发的插件，**自动识别电影和剧集的原产国家或语言**，并从 **TMDB（The Movie Database）** 获取对应的**原语言海报（Poster）和原语言 Logo（ClearLogo）**。

- **不再使用默认(元数据语言)海报** —— 《魔女》显示韩语海报，《千与千寻》显示日语海报，《鹿鼎记》显示中文海报。
- **支持中文地区统一**：无论电影是港产（`HK`）、台产（`TW`）还是国产（`CN`），均请求 TMDB 的 `zh` 语言海报，**避免因海报缺失导致的显示混乱**。
- **支持剧集与播出季**（Series & Season）。
- **可配置语言偏好**，兼容 Emby 的“首选元数据语言”设置。
- **轻量、稳定、无依赖**，仅需一个 TMDB API Key。

---

## ✅ 功能亮点

| 功能 | 说明 |
|------|------|
| 🎬 **自动语言识别** | 根据 `origin_country`（制片国）自动匹配语言，如 `JP` → `ja-JP`，`FR` → `fr-FR` |
| 🇨🇳 **中文地区统一支持** | **港/台/中** 电影统一请求 `zh` 语言海报，确保海报存在性与一致性 |
| 🖼️ **原语言海报 + Logo** | 同时获取海报（Primary）和透明 Logo（ClearLogo），提升媒体库视觉统一性 |
| ⚙️ **智能语言映射** | 内置 150+ 语言/地区映射表，支持 `en-US`, `fr-FR`, `ko-KR` 等 BCP 47 标准 |
| 📺 **支持剧集与播出季** | 自动从剧集获取原语言，为每一季匹配对应海报 |
| 🔧 **用户自定义配置** | 可开启/关闭插件、Logo、测试模式，设置默认语言 |
| 🔄 **兼容 Emby 语言偏好** | 当您设置“首选元数据语言 = 中文”时，插件会自动将海报语言“伪装”为 `zh`，提高被 Emby 采纳率 |
| 🚫 **无冗余依赖** | 不依赖第三方库，仅使用 Emby 官方 `IHttpClient` 和 `IJsonSerializer` |

---

## 📥 安装步骤

### 1. 获取 TMDB API Key
- 访问 [https://www.themoviedb.org/settings/api](https://www.themoviedb.org/settings/api)
- 登录后创建一个 **API Key（v3 auth）**
- 保存 `API Key`（如 `xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`）

### 2. 下载插件文件
从 Releases 页面下载最新版：
```
OriginalPoster.dll
```
> ⚠️ 如果没有发布版本，请编译本项目（见下文）。

### 3. 安装到 Emby
#### Windows：
```bash
copy OriginalPoster.dll "%AppData%\Emby-Server\programdata\plugins\"
```

#### Linux / Docker：
```bash
cp OriginalPoster.dll /var/lib/emby/plugins/
# 或映射到容器的 plugins 目录
```

### 4. 重启 Emby 服务器
- 在 Emby 管理后台 → **“插件”** → 等待插件加载
- 成功后会显示：**“OrigianlPoster”**

### 5. 配置插件
进入 **“插件设置” → “Original Poster”**：

| 配置项 | 建议值 | 说明 |
|--------|--------|------|
| **启用插件** | ✅ 开启 | 启用整个插件功能 |
| **启用原语言 Logo** | ✅ 开启 | 同时获取透明 Logo（推荐） |
| **元数据语言** | `zh` | 与您 Emby 的“首选元数据语言”一致（如 `zh`, `en`, `ja`） |
| **海报选择策略** | `优先原语言` | 推荐选项，优先显示原语言海报 |
| **TMDB API KEY** | `您的API密钥` | **必填**，从 TMDB 获取 |
| **测试模式** | ❌ 关闭 | 仅调试时开启，会返回固定测试图 |

> 💡 **提示**：“首选图像下载语言”留空，留空，留空。

---

## 🛠️ 高级配置（可选）

### 如何让插件更“智能”？
| 场景 | 解决方案 |
|------|----------|
| **电影是香港制作，但想显示简体中文海报** | 设置 `元数据语言 = zh-CN`，插件会自动将 `zh` 海报“伪装”为 `zh-CN` |
| **想优先显示无文字 Logo** | 选择 **“优先无文字海报”** 策略 |
| **想完全按评分选图** | 选择 **“优先高评分”**，忽略语言 |
| **TMDB 无原语言海报？** | 插件自动回退到 `null`（无文字）海报，确保总有图可用 |

---

## 🧪 测试示例

| 电影 | 制片国 | 插件行为 |
|------|--------|----------|
| 《拯救大兵瑞恩》 | `US` | → `en-US` 海报 |
| 《千与千寻》 | `JP` | → `ja-JP` 海报 |
| 《魔女》 | `KR` | → `ko-KR` 海报 |
| 《鹿鼎记》 | `HK` | → `zh` 海报（港产片） |
| 《英雄》 | `CN` | → `zh` 海报（国产片） |
| 《大话西游》 | `HK` | → `zh` 海报（港产片） |

> ✅ **所有海报语言均与您设置的“元数据语言”保持一致**，确保 Emby 优先采纳。

---

## 📚 技术原理

1. **获取 TMDB ID**：从 Emby 媒体元数据中读取 `tmdb://xxx`
2. **查询原产国**：调用 `/movie/{id}` 或 `/tv/{id}` 获取 `origin_country`
3. **语言映射**：使用 `LanguageMapper` 将国家代码（如 `HK`）映射为 BCP 47 语言标签（如 `zh`）
4. **请求图像**：调用 `/images?include_image_language=zh,null`
5. **智能排序**：根据策略（原语言优先 / 高评分优先）排序图像
6. **语言伪装**：将返回的 `Language` 字段统一设为用户设置的 `MetadataLanguage`（如 `zh`），提高 Emby 采纳率
7. **返回图像**：Emby 自动下载并缓存最佳海报

> ✅ **完全兼容 Emby 4.9.1.x API**，无任何非官方调用。

---

## 📦 项目结构（开发者参考）

```
OriginalPoster/
├── Models/
│   └── TmdbModels.cs          # TMDB 数据模型
├── Services/
│   ├── LanguageMapper.cs      # 国家 → 语言映射（支持 150+ 地区）
│   └── TmdbClient.cs          # 封装 TMDB API 调用
├── Providers/
│   └── OriginalPosterProvider.cs # 核心图像提供者（支持 Poster + Logo）
├── OriginalPosterConfig.cs    # 插件配置类（自动生成 UI）
├── Plugin.cs                  # 插件入口
└── OriginalPoster.csproj      # .NET 8 项目文件
```

---

## ⚠️ 常见问题与解答

### ❓ 为什么我看到的还是英文海报？
- 检查 **“元数据语言”** 是否与 Emby 设置一致（如都设为 `zh`）
- 检查 **TMDB API Key** 是否填写正确
- 检查 **是否开启插件**
- 尝试 **刷新元数据**（勾选“替换所有图像”）

### ❓ 《鹿鼎记》是香港电影，为什么显示的海报语言是 `zh` 而不是 `zh-HK`？
- 这是 **插件的优化设计**：为避免 TMDB `zh-HK`/`zh-TW` 海报缺失，统一请求 `zh` 海报
- 您看到的海报内容仍是**香港版海报**（如果存在），只是语言标签统一了，提高了 Emby 采纳率。需要严格区分，还请自行fork修订。

### ❓ 插件支持剧集吗？
✅ **支持！**  
插件自动识别剧集（Series）和播出季（Season），并从剧集获取原语言。

### ❓ 我能自定义语言映射吗？
目前不支持，但插件内置完整映射表。如需扩展，可修改 `LanguageMapper.cs`。

### ❓ 插件会频繁调用 TMDB API 吗？
不会。  
- 首次刷新时调用一次
- 后续刷新使用缓存（Emby 内部机制）
- 每个媒体项最多调用 2 次 API（详情 + 图像）

---

## 🚀 开发者说明

- **开发语言**：C# (.NET 8)
- **目标平台**：Emby Server 4.9.1.80
- **依赖库**：仅 `mediabrowser.server.core` (4.9.1.80)
- **调试建议**：
  - 使用 `Plugin.Instance.Configuration` 访问配置
  - 日志路径：Emby → 设置 → 日志 → 搜索 `[OriginalPoster]`
  - 启用 `DebugLogging` 查看详细语言识别过程

---

## 📜 许可证

[![License: GPL v3](https://img.shields.io/badge/License-GPL%20v3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)

> 本项目采用 **GPL-3.0 许可证**。任何基于本项目代码的分发（包括商业用途）**必须以相同许可证开源全部源代码**。  
> 欢迎使用、改进和分享，但请勿闭源售卖！

---

## 💬 联系作者

如有问题、建议或贡献，请提交 Issue 或联系：  

⭐ 如果喜欢，请给项目一个 Star！

---

> **让您的 Emby 库，真正属于世界。🌍**  
> *—— 原语言，才是电影的灵魂。*
