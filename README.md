# シャボン玉あそび

Unity **6000.5.6f1** / Universal Render Pipeline **17.5**。スマートフォンのタッチとPCのマウスで遊べます。

- 空白をタップすると最小サイズで生成。長押しすると成長します。
- なぞると風が発生し、近くの玉が流れます。影響半径は画面短辺の1/4です。
- 玉をタップすると揺れ、玉ごとにランダムな1〜6回で破裂します。
- 右上の歯車を0.75秒長押しすると、背景と個数上限を設定できます。

起動時は0個。7秒ごとに1個ずつ、ランダムな補充目標1〜3個まで補充します。補充サイズは最大サイズの1/3〜最大サイズです。個数上限は10〜100個（初期値20個）で、追加時に上限を超えると最も古い玉が割れます。音は常に有効です。背景と個数上限はWeb版ではlocalStorageに保存します。

## ディレクトリ

```text
Assets/           Unityのシーン・コード・シェーダー・素材
Packages/         パッケージ定義
ProjectSettings/  Unity設定
AgentScripts/     Editor内で実行する検証・補助スクリプト
Tools/            ローカルでのWeb公開用ファイル準備
Documentation/    アプリ仕様・描画方針・素材出典・検証記録
Build/Web/        Unityのローカル出力（Git管理外）
docs/             GitHub Pagesで配信するWebビルド（Git管理対象）
.github/workflows/ 手動公開用のGitHub Actions
```

`Library/`、`Logs/`、`UserSettings/`、`Captures/`などの生成物はGit管理しません。旧HTML版`bubble-app`は削除し、このリポジトリをUnity版に統一しています。

## 開発・ビルド

Unity Hubでリポジトリ直下を開き、`Assets/Scenes/BubblePlay.unity`を開いて再生します。Webビルドには同じEditorのWeb Build Supportが必要です。

1. Unityメニュー **Bubbles > Build Web** を実行します（出力先`Build/Web`）。
2. リポジトリ直下で以下を実行し、`docs/`を更新します。

```sh
python3 Tools/package_web.py
python3 -m http.server 8080 --bind 127.0.0.1 --directory docs
```

`http://127.0.0.1:8080`で確認できます。準備コマンドは`docs/`を置き換え、デバッグ用DoNotShipフォルダーを除外します。アップロードは行いません。

HDRを含むため、バッチビルドでもグラフィックスを有効にし、`-nographics`を指定しないでください。ビルド成功だけでなく、実際のブラウザーで背景画像とエラーの有無を確認します。Gzip + Decompression Fallbackにより通常の静的サーバーで配信できます。素材は同梱しており、実行時に外部素材サービスへアクセスしません。相対URLのためGitHub Pagesのリポジトリ配下でも動作します。

`docs/build-info.json`と`window.bubbleBuildId`でビルドを識別できます。コードやシェーダー変更後は再ビルドと準備コマンドの両方を実行してください。

## GitHubでの公開

公開操作はまだ行っていません。公開するときに次を実施します。

1. GitHubに公開リポジトリを作成し、このリポジトリをpushします。`docs/`もコミットに含めます。
2. GitHubの **Settings > Pages > Build and deployment > Source** を **GitHub Actions** に設定します。
3. **Actions > Publish Web player to GitHub Pages > Run workflow** から公開するブランチを選んで実行します。
4. 成功後、ワークフローに表示されるPages URLを開きます。

ワークフローは手動実行のみです。pushだけではPagesを更新しません。Unityのビルドはローカルで行うため、ActionsへのUnityライセンス登録は不要です。更新時もローカルでビルド・`docs/`更新・コミット・pushの後、手動で公開します。

設定の参照元: [GitHub公式: Pagesカスタムワークフロー](https://docs.github.com/en/pages/getting-started-with-github-pages/using-custom-workflows-with-github-pages)。

## 描画・検証・素材

専用の薄膜シェーダーがHDR背景から反射・屈折を計算し、干渉模様を描きます。透過はフレネル反射に従い、見やすさを加味して正面で約83%です。古い玉を奥、新しい玉を手前に描きます。大きな玉ほど慣性が強く、風で動き始めにくく、風がやむと長く流れます。

- [仕様](Documentation/BUBBLE_APP_SPEC.md)
- [コード規約と構成](Documentation/CODING_GUIDELINES.md)
- [描画方針](Documentation/ART_DIRECTION.md)
- [HDR素材出典](Documentation/HDR_SOURCES.md)
- [検証記録](Documentation/VALIDATION.md)

`AgentScripts/VerifyPlay.cs`などはEditorの再生中に実行する検証スクリプトです。調整値は`Assets/Bubbles/Generated/PlayTuning.asset`、膜は`Assets/Bubbles/Generated/Soap film.mat`にあります。日本語フォントNoto Sans JPのライセンスは`Assets/Bubbles/Fonts/OFL.txt`を参照してください。
