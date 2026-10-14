# 术语库

**状态：独立维护。** 当前公共库为 `2026.10.14.1`，含 40 个概念、139 条语言映射。可直接查阅 [中日韩英对照表](../../MULTILINGUAL.md)；维护者编辑 [四语源数据](../../multilingual.json)，再生成 [客户端公共词库](../../limbus-company.ggprofile.json)。本目录记录维护范围，并保留空的 `terms.json` 用于后续候选条目。

旧公共集合已移除，新库按人物、称号、组织、地名和缩写逐条整理。参考包括 [游戏官网](https://limbuscompany.com/)、[英文角色资料](https://limbuscompany.wiki.gg/wiki/List_of_Characters)、[日文攻略](https://wikiwiki.jp/lcbwiki/) 与公开的游戏数据索引；准确页面链接逐条记录在源数据的 `sources` 中。仅选取名称事实并自行对齐，不复制人物介绍或剧情段落。

中文映射是本项目选定的写法，不宣称为官方中文或全社区唯一译名，也不表示取得第三方词库授权。贾环、亚细亚、仇甫、耐莉、亚哈、赫尔曼、狄亚斯、霍恩海姆采用维护者指定中文名；色彩称号的异译在备注中说明。贾环保留日语旧名，英文称号兼容省略冠词的形式；反向翻译选用表中的规范名。更多专名逐步核对后收录。

## 取得资料后放在哪里

- `source.json`：在 `provider` 中填写资料提供方，再填写来源、版本、收到日期、允许的用途及署名要求。
- `terms.json`：将允许公开的术语整理到 `terms` 数组中。无需改动 `schemaVersion` 和 `sourceId`。

如果收到 Excel、CSV 或其他格式，可以先在本机保留原件，再转换成这里的条目格式。原件是否公开，以提供方允许的范围为准。

## 来源记录怎么填写

`source.json` 是维护记录，客户端不会读取。未收到资料时保持 `status: "reserved"`；收到需要授权的资料后改为 `awaiting-permission`。确认后改为 `permission-confirmed`，并按实际允许的范围填写 `permission`：

| 字段 | 含义 |
|---|---|
| `reference`、`confirmedAt` | 授权记录编号或公开链接，以及确认日期；不用公开私人邮件全文。 |
| `publicRepository` | 是否允许把术语资料放入公开 GitHub 仓库。 |
| `bundledDistribution` | 是否允许随安装版和便携版分发。 |
| `onlineUpdates` | 是否允许客户端下载词库更新。 |
| `adaptation` | 是否允许转换格式、整理条目或修改内容。 |
| `usageSummary`、`license` | 使用条件、限制和提供方指定的许可。 |
| `attribution` | 对方要求的署名文字。 |

这些字段记录实际约定，填写本身不构成授权。计划采用的公开保存、随包分发和在线更新都应先确认；没有许可名称时可以保留 `license: null`，在 `usageSummary` 中写明双方确认的条件。

资料中的 `provider`、`sourceUrl`、`sourceVersion`、`receivedAt` 分别记录提供方、来源链接、提供方版本和收到日期。发布后在 `publishedLibraryVersion` 中记录收录这些资料的公共词库版本。来自其他提供方或用户的条目，在各条目的 `note` 和 `sourceUrl` 中注明对应来源；使用条件也应分别记录。

## 术语条目格式

每条术语采用公共词库现有的字段。以下只是填写示例；请把真实资料写进 `terms.json` 的数组中：

```json
{
  "language": "ja",
  "source": "原文写法",
  "target": "中文译名",
  "forbidden": [],
  "note": "来源：资料提供方；称号、姓名或其他术语；适用场景",
  "sourceUrl": "https://example.com/reference",
  "reviewStatus": "待核对",
  "enabled": false
}
```

- `language` 为原文语言：`en` 英语、`ja` 日语、`ko` 韩语、`zh` 中文。`target` 保存中文规范译名；同一术语的不同语言原文分别建立条目。
- `source` 保留游戏中的准确写法；不要把称号、姓名与整句正文合成一个条目。
- `forbidden` 可填写需要纠正的错误译名；不确定时保持空数组，避免替换普通单词。
- `sourceUrl` 为可公开核对的来源链接，没有链接时填 `null`，在 `note` 中记录章节、资料版本等依据。
- 未确认的条目保持 `enabled: false`。核对完成后填写实际核对结果并启用。

## 怎么让用户收到

维护者核对后，将条目补入 `../../multilingual.json`，增加源数据版本并导出客户端词库、四语表及校验清单。具体命令见 [发布说明](../../README.md#维护者如何发布词库)。候选文件不会直接发布给客户端。

这个预留文件本身不是可导入的 `.ggprofile.json` 档案。完成公共词库发布后：

- **现有用户：** 在 边译“支持”页检查并应用词库更新；勾选自动应用的用户在检查到更新后自动应用。
- **新下载用户：** 新版安装包或便携包自带打包时的词库，无需手动导入。已发布的旧安装包不会随 GitHub 文件变化而改写，其中的用户仍需检查更新。
- **提交者：** 通过 [术语反馈表单](https://github.com/wl8695573-blip/BianYi/issues/new?template=terminology.yml) 提交补充或纠错，由维护者确认并发布。
