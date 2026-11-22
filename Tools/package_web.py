"""ローカルのUnity WebビルドをGitHub Pages用のdocs/へコピーする。"""

import json
from pathlib import Path
import shutil

REQUIRED_FILES = (
    "index.html",
    "build-info.json",
    "Build/Web.loader.js",
    "Build/Web.data.unityweb",
    "Build/Web.framework.js.unityweb",
    "Build/Web.wasm.unityweb",
)
MAXIMUM_FILE_BYTES = 100 * 1024 * 1024


def prepare_web(project: Path) -> str:
    """入力を検証して配信用ファイルを置き換え、ビルドIDを返す。"""
    source = project / "Build/Web"
    output = project / "docs"

    # 既存の配信用ビルドを置き換える前に、入力を確認する。
    for name in REQUIRED_FILES:
        if not (source / name).is_file():
            raise ValueError(f"{name}がありません。先にUnityでBubbles > Build Webを実行してください。")

    version = json.loads((source / "build-info.json").read_text())["buildId"]
    for path in source.rglob("*"):
        if path.is_file() and path.stat().st_size >= MAXIMUM_FILE_BYTES:
            raise ValueError(f"GitHubリポジトリのファイル上限を超えています: {path}")

    if output.exists():
        shutil.rmtree(output)
    shutil.copytree(source, output, ignore=shutil.ignore_patterns("*DoNotShip*", "*.pdb"))
    (output / ".nojekyll").touch()
    return version


def main() -> None:
    """プロジェクト直下を基準に準備する。外部への送信は行わない。"""
    project = Path(__file__).resolve().parents[1]
    try:
        version = prepare_web(project)
    except (OSError, ValueError, KeyError) as error:
        raise SystemExit(str(error)) from error
    print(f"{project / 'docs'}を準備しました（ビルド{version}）。アップロードは行っていません。")


if __name__ == "__main__":
    main()
