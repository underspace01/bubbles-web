# HDR背景の出典

Poly HavenのRadiance HDRパノラマ原本（2048×1024）を同梱しています。リニアHDRテクスチャーとして取り込み、低ダイナミックレンジのサムネイルは使用しません。実行時にPoly Havenへ通信しません。

| 表示名 | 素材 | 制作者 | ライセンス |
| --- | --- | --- | --- |
| 公園 | [Rooitou Park](https://polyhaven.com/a/rooitou_park) | Greg Zaal | CC0 |
| 夜明けの山 | [Kiara 1 Dawn](https://polyhaven.com/a/kiara_1_dawn) | Greg Zaal | CC0 |
| 室内 | [Lebombo](https://polyhaven.com/a/lebombo) | Greg Zaal | CC0 |
| 夜景 | [Shanghai Bund](https://polyhaven.com/a/shanghai_bund) | Greg Zaal | CC0 |

素材の保存先： `Assets/Bubbles/Environments/`. ライセンス： https://polyhaven.com/license

ダウンロード時に公式ファイルAPIのMD5と照合しています：

- `rooitou_park_2k.hdr`: `56cbcf65f66a7168bb643fb1ca807a83`
- `kiara_1_dawn_2k.hdr`: `c3b5f496629ab4707c6554e4761cc7cf`
- `lebombo_2k.hdr`: `64e113e1206870e7683c4c519d4fc61a`
- `shanghai_bund_2k.hdr`: `7514519fbe8a917aa2e4829f3b52cdde`

`EnvironmentSampling.hlsl`でパノラマの投影・向き・露出・トーンマッピングを背景と膜の間で共用します。`SoapFilm.shader`は屈折率1.333の`refract`、RGBの色分散、視認性を補った屈折、パノラマの反射と薄膜干渉を使います。タッチ画面での見やすさを考慮した近似です。

膜は生成順（古い玉から）にアルファ合成します。屈折ではHDRパノラマを参照し、他の玉の描画結果は屈折させません。
