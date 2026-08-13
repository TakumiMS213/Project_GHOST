# TELEGHOST Version 1.1 仕様適合監査

## 実装結果

- Stage 6: X=20〜26のStaticGroundを撤去し、6枚のFragileFloorをRect(x,0,1,1)の実床へ変更。
- Stage 7: マップ幅34U、到達可能なStep/Perch、P7-L/P7-U、T7-B/S7-Bおよび各Switchをv1.1座標へ再配置。
- Stage 10: Rect(15.25,0,1.50,1)のCheckpoint島を追加。CP10は足元座標規約と「島上面」の記述を優先して(16,1)へ配置。
- Stage 13: 上段STAR中心Yを5.0から4.0へ変更し、SightBlockを撤去。下段→上段の上面差を2.0Uに固定。
- Stage 14: 26U幅のControl Deck構成へ全面更新。P14-Lock/P14-Star、SightBlock、T14-A/S14-B、棚、Switch、Doorを再配置し、両GhostのPulseを1.20sへ設定。
- Stage 15: Area Aの固定階段6区画を追加。Ground_80をX=80〜87へ短縮し、X=87〜92をFinal Pit化。T15-FINALを(87.5,2.0)→(90.5,2.0)、Goal床をX=92〜94へ配置。
- RightUp/LeftUpの中心角を実装上の45°から仕様値35°へ修正。

## 実機監査結果

- EditMode: 42/42合格。
- PlayMode: 9/9合格。
- Stage 7: P7-L Left=TのみActive、RightUp=両方Active、Right=両方Inactive、P7-U Right=SのみActiveを確認。
- Stage 14: P14-Lock Left=TのみActive、P14-Lock Right=両方Inactive、P14-Star Right=SのみActiveを確認。
- 全16シーン: Missing Script 0、Broken Prefab 0。
- Stage 13の上り差2.0U、Stage 15 Area A最大上り差1.6U、T15-FINAL出口Gap 0.5Uを確認。

## 設計書内の競合と採用値

- Stage 10 S10-D: 初期中心(16,1.0)はPath先頭(27.5,4.4)および攻略文と競合するため、Path先頭を採用。
- Stage 10 CP10: 表の(16,0.5)は足元座標規約と安全島上面Y=1.0に競合するため、(16,1.0)を採用。
- ObserverOrigin: foot+1.20UではStage 1のT1-AがRight 32°扇形外となり、正規フロー「開始時停止」が成立しない。Stage 7/14の要求観測状態は従来のfoot+0.95Uでも全て成立するため、ゲーム成立を優先してfoot+0.95Uを維持。

## 継続プレイテスト項目

- Stage 6/10/12/15の初見完走時間、落下回数、視線切替回数を計測する。
- 同じ原因の失敗が3回以上続く地点を優先調整する。
- 座標変更は±0.25U以内。攻略構造・観測関係・Path長の変更は別途レベルデザイン変更として扱う。
