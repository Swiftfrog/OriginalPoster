# Original Poster
> **Emby Plugin**
### **自动获取TMDB的 `电影` / `剧集` / `合集` / `LOGO` 原语言海报**

![OriginalPosterLogo](https://raw.githubusercontent.com/Swiftfrog/swiftfrog.github.io/master/OriginalPostLogo.png)

---

## 功能

| 功能 | 说明 |
|------|------|
| 🎬 **自动语言识别** | 根据 `origin_language`自动匹配语言，如 `JP` → `ja-JP`，`FR` → `fr-FR` |
| 🖼️ **原语言海报 + Logo** | 同时获取海报和Logo，提升媒体库视觉统一性 |
| 📺 **支持剧集，播出季，合集** | 自动获取原语言的剧集，播出季，合集的海报 |
| 🇨🇳 **中文地区统一支持** | **港/台/中** 电影统一请求 `zh` 语言海报（港澳台很多电影海报并未被区分为zh-HK，zh-TW） |
| ⚙️ **智能语言映射** | 内置主流语言/地区映射表，支持 `en-US`, `fr-FR`, `ko-KR` 等 BCP 47 标准 |
| 🔧 **用户自定义配置** | 可开启/关闭插件、Logo、测试模式 |
| 🚫 **无冗余依赖** | 完全基于Emby和TMDB的API |

## 安装

1. 下载 `OriginalPoster.dll`。
2.  `OriginalPoster.dll` 放到插件目录，Emby Server的plugins下。
3.  重启 Emby 服务器。
4.  `设置` -> `插件` -> `OriginalPoster`, 启用插件。
5.  配置插件

进入 **“插件设置” → “Original Poster”**：

| 配置项 | 建议值 | 说明 |
|--------|--------|------|
| **启用插件** | 开启 | 启用整个插件功能 |
| **启用原语言 Logo** | 开启 | 同时获取透明 Logo（推荐） |
| **TMDB API KEY** | `您的API密钥` | **必填**，从 TMDB 获取 |
| **海报选择策略** | `优先原语言` | 推荐选项，优先显示原语言海报 |
| **测试模式** | 关闭 | 仅调试时开启，会返回固定测试图 |

> 💡 **提示**：“首选图像下载语言” 留空，留空，留空。

## 测试示例

| 电影 | 制片国 | 插件行为 |
|------|--------|----------|
| 《拯救大兵瑞恩》 | `US` | → `en-US` 海报 |
| 《千与千寻》 | `JP` | → `ja-JP` 海报 |
| 《魔女》 | `KR` | → `ko-KR` 海报 |
| 《鹿鼎记》 | `HK` | → `zh` 海报（港产片） |
| 《英雄》 | `CN` | → `zh` 海报（国产片） |
| 《大话西游》 | `HK` | → `zh` 海报（港产片） |

## 技术原理

1. **获取 TMDB ID**：从 Emby 媒体元数据中读取 `tmdb://xxx`
2. **查询原产国**：调用 `/movie/{id}` 或 `/tv/{id}` 获取 `origin_country`
3. **语言映射**：使用 `LanguageMapper` 将国家代码（如 `HK`）映射为 BCP 47 语言标签（如 `zh`）
4. **请求图像**：调用 `/images?api_key=AIP KEY&language=zh,null`
5. **智能排序**：根据策略（原语言优先 / 高评分优先）排序图像
6. **语言伪装**：将返回的 `Language` 字段统一设为 `Library`的元数据语言，确保 `OriginalPoster` 返回的海报优先
7. **返回图像**：Emby 自动下载并缓存最佳海报

> **完全基于 Emby API**，无任何非官方调用。

## ⚠️ 常见问题与解答

### ❓ 为什么我看到的还是英文海报？
- 检查 **TMDB API Key** 是否填写正确
- 检查 **是否开启插件**
- 尝试 **刷新元数据**（勾选“替换所有图像”）

### ❓ 《鹿鼎记》是香港电影，为什么显示的海报语言是 `zh` 而不是 `zh-HK`？
- 这是 **妥协的设计**：很多港澳台海报在TMDB都被归类为了`zh-CN`,而不是详细区分为`zh-HK`/`zh-TW`/`zh-SG`。
- 需要严格区分，还请自行fork，切换注释代码。

### ❓ 插件支持剧集吗？
- **支持！**  
- 插件自动识别剧集（Series）和播出季（Season），并从剧集获取原语言。

### ❓ 我能自定义语言映射吗？
- 目前不支持，但插件内置完整映射表。如需扩展，可修改 `LanguageMapper.cs`。

### ❓ 插件会频繁调用 TMDB API 吗？
不会。  
- 首次刷新时调用一次
- 后续刷新使用缓存（Emby 内部机制）
- 每个媒体项最多调用 2 次 API（详情 + 图像）

## 开发者说明

- **开发语言**：C# (.NET 8)
- **目标平台**：Emby Server 4.9.1.x
- **依赖库**：`MediaBrowser.Server.Core`
- **调试建议**：
  - 日志路径：Emby → 设置 → 日志 → 搜索 `[OriginalPoster]`
  - 启用 `DebugLogging` 查看详细语言识别过程

## 许可证

[![License: GPL v3](https://img.shields.io/badge/License-GPL%20v3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)

> 本项目采用 **GPL-3.0 许可证**。任何基于本项目代码的分发（包括商业用途）**必须以相同许可证开源全部源代码**。  
> 欢迎使用、改进和分享，但请勿闭源售卖！

## 联系作者

如有问题、建议或贡献，请提交 Issue 或联系： 

如果喜欢，请给项目一个 Star！

---

> **让您的 Emby 库，真正属于世界。**  
> *原语言，才是电影的灵魂。*
