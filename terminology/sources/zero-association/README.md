# 零协会术语资料（预留）

**状态：等待授权与资料。** 本目录当前没有零协会提供的术语库。

## 取得资料后放在哪里

- `source.json`：填写资料来源、版本、收到日期、允许的用途及署名要求。
- `terms.json`：将允许公开的术语整理到 `terms` 数组中。无需改动 `schemaVersion` 和 `sourceId`。

如果零协会提供 Excel、CSV 或其他格式，可以先在本机保留原件，再转换成这里的条目格式。原件是否公开，以对方允许的范围为准。

## 来源记录怎么填写

`source.json` 是维护记录，客户端不会读取。授权未确认时保持 `status: "awaiting-permission"`。确认后改为 `permission-confirmed`，并按实际允许的范围填写 `permission`：

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

资料中的 `sourceUrl`、`sourceVersion`、`receivedAt` 分别记录来源链接、提供方版本和收到日期。发布后在 `publishedLibraryVersion` 中记录收录这些资料的公共词库版本。

## 术语条目格式

每条术语采用公共词库现有的字段。以下只是填写示例，不是零协会提供的数据；请把真实资料写进 `terms.json` 的数组中：

```json
{
  "language": "ja",
  "source": "原文写法",
  "target": "中文译名",
  "forbidden": [],
  "note": "称号、姓名或其他术语；可补充适用场景",
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

维护者核对后，将条目合并到 `../../limbus-company.ggprofile.json` 的 `profile.terms`，并更新词库版本及 `../../manifest.json`。具体命令和步骤见 [发布说明](../../README.md#维护者如何发布词库)。

这个预留文件本身不是可导入的 `.ggprofile.json` 档案。完成公共词库发布后：

- **现有用户：** 在 LCTA“支持”页检查并应用词库更新；勾选自动应用的用户在检查到更新后自动应用。
- **新下载用户：** 新版安装包或便携包自带打包时的词库，无需手动导入。已发布的旧安装包不会随 GitHub 文件变化而改写，其中的用户仍需检查更新。
- **提交者：** 通过 [术语反馈表单](https://github.com/wl8695573-blip/GuGuGaGaTranslator/issues/new?template=terminology.yml) 提交补充或纠错，由维护者确认并发布。
