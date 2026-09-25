# GitHub 大文件上传

GitHub 普通 Git 单文件上限为 100 MiB（104857600 字节）。超限的原始资产使用 Git LFS：工作目录保留完整文件，Git 提交保存小型指针，原文件由 LFS 上传。

当前 `.gitattributes` 将 PNG、JPG、GIF、WAV、OGG、M4A、MP4、Blender、FBX 和 TTF 二进制资产交给 Git LFS，另保留两个最初超限文件的路径规则。文件内容、Unity GUID 和引用保持不变。新增其他类型的大文件时需补充 LFS 规则。

2026-09-25 上传检查发现：虽然单文件限制已解决，原本待上传提交的 Git pack 仍约为 2478 MiB，超过 GitHub 单次推送 2 GiB 限制。因此仅对尚未上传的一次本地提交迁移二进制资产；已发布历史保持不变。原提交保留在本地备份分支 `codex/backup-before-lfs-upload-20260925`，该分支不需要推送。

## 开发电脑准备

安装 [Git LFS](https://git-lfs.com/)，然后在项目根目录运行：

```bash
git lfs install --local
git lfs pull
```

本机 Git LFS 安装于 `~/.local/bin/git-lfs`，该目录需要在 Git 客户端的 PATH 中。新的开发电脑同样需要安装 Git LFS；克隆后打开 Unity 前，先确认 `git lfs pull` 成功，避免将指针当成资产导入。

## 日常提交与检查

选择本次要提交的文件并 `git add` 后运行：

```bash
git lfs status
python3 Tools/check_git_file_sizes.py
git diff --cached --stat
```

检查通过并审核暂存内容后正常 commit 和 push。LFS 的 pre-push hook 会自动上传 LFS 对象。检查脚本读取实际暂存的 Git blob；它不检查未暂存的修改或未跟踪文件，必须在 add 之后执行。

需要核查当前提交历史时运行：

```bash
python3 Tools/check_git_file_sizes.py --revision HEAD
```

不要把 Codex 的内部快照 refs 当作开发分支推送；日常使用普通的分支 push 即可。

## 新增大文件

对需要版本管理的原始资产，先配置 LFS，再暂存：

```bash
git lfs track --filename "相对于仓库根目录的文件路径"
git add .gitattributes
git add -- "相对于仓库根目录的文件路径"
python3 Tools/check_git_file_sizes.py
```

已经暂存成普通 blob 的新文件需要重新 `git add`。如果超限 blob 已进入待推送的历史提交，仅添加 LFS 规则不够；应先备份并判断哪些提交需要迁移，避免直接改写共享历史。

LFS 不减少原始资产体积，也不优化游戏安装包。远端 LFS 配额和权限是否可用，以实际 push 结果为准。

参考：[GitHub 大文件限制](https://docs.github.com/en/repositories/working-with-files/managing-large-files/about-large-files-on-github)、[Git LFS 配置](https://docs.github.com/en/repositories/working-with-files/managing-large-files/configuring-git-large-file-storage)。
