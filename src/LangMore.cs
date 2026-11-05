using System;
using System.Collections.Generic;

namespace DiskMate
{
    /// <summary>日本語 / 한국어 界面文本（清理项正文回退到英文）。</summary>
    internal static class LangData
    {
        public static void LoadMore(Dictionary<string, Dictionary<string, string>> data, Action<string, string> add)
        {
            add("ja-JP", Ja);
            add("ko-KR", Ko);
            add("de-DE", De);
            add("fr-FR", Fr);
            add("es-ES", Es);
            add("pt-BR", Pt);
            add("ru-RU", Ru);
        }

        private const string Ja =
@"app.title|DiskMate · C ドライブ クリーナー
app.tagline|C ドライブのクリーンアップ
nav.clean|スキャンと整理
nav.big|大きいファイル
nav.log|ログ
page.clean.sub|まずスキャン、次に選択。スキャンは容量を測るだけで、何も削除しません。
page.big.sub|ディスク上の大きなファイルを一覧表示します。ダブルクリックでエクスプローラーに表示。削除はしません。
page.log.sub|すべてのスキャンとクリーンアップの記録。ローカルのログファイルにも保存されます。
welcome.title|まずスキャン、削除はそれから
welcome.desc|スキャンすると、C ドライブ上のキャッシュ・一時ファイル・古いドライバーが 1 件ずつ一覧になります。それぞれのリスクと「削除するとどうなるか」も表示します。チェックを入れるまで、ファイルは 1 つも削除されません。
welcome.features|容量を測るだけ  ·  リスクと結果を表示  ·  選んだ項目だけ削除  ·  すべてログに記録
welcome.start|スキャン開始
welcome.hint|スキャンはファイルサイズを読むだけです。削除・移動・変更は行いません。F5 でも開始できます。
scan.title|C ドライブをスキャン中
scan.preparing|準備中…
scan.current|計測中：{0}
scan.cancel|スキャン中止
scan.cancelling|停止しています…
scan.cancelled|スキャンを中止しました。変更はありません。
scan.done|スキャン完了：{0} 件、合計 {1}
scan.failed|スキャン失敗：{0}
work.title|クリーンアップ中
work.preparing|準備中…
work.current|削除中：{0}
work.hint|完了までウィンドウを閉じないでください。解放された容量を表示します。
clean.title|この {0} 件を削除しますか？
clean.sub|約 {0} 解放されます · 削除は元に戻せません。内容を確認してください
clean.stay|やめる
clean.go|削除する
clean.warnHigh|うち {0} 件は高リスク：削除すると復元できません。
clean.warnMid|うち {0} 件は中リスク：副作用があります。説明を確認してください。
clean.allLow|すべて低リスクです（キャッシュと一時ファイルのみ）。
clean.tip|各手順はログに記録され、「ログ」ページで確認できます。
sel.none|何も選択されていません
sel.some|{0} 件選択 · {1}
btn.selectLow|低リスクを選択
btn.selectNone|選択解除
btn.clean|選択項目を削除
btn.cleanN|選択項目を削除（{0}）
btn.rescan|再スキャン
search.ph|項目を検索
col.name|項目
col.cat|種類
col.size|サイズ
col.risk|リスク
empty.none|削除できる項目はありません
empty.filter|一致する項目がありません。キーワードを変えるか絞り込みを解除してください。
det.title|項目の詳細
det.impact|削除するとどうなるか
det.advice|アドバイス
det.path|場所
det.add|削除対象に追加
det.remove|選択を解除
det.open|フォルダーを開く
det.nothing|項目が選択されていません
det.hint|左の一覧から項目を選ぶと、何が削除され、どんな影響があるかを表示します。
stat.total|削除可能な合計
stat.low|低リスク
stat.mid|中リスク
stat.high|高リスク
stat.items|{0} 件
stat.count|{0} 件
risk.low|低リスク
risk.mid|中リスク
risk.high|高リスク
big.btn|大きいファイルをスキャン
big.scanning|スキャン中…
big.idle|まだスキャンしていません。「大きいファイルをスキャン」を押してください。
big.progress|スキャン中… {0:N0} ファイル確認、{1} 件一致
big.done|{0:N0} ファイルを確認し、{2} を超える {1} 件が見つかりました（表示のみ、削除しません）
big.cancel|スキャンを中止しました
big.open|場所を開く
big.colsize|サイズ
big.colpath|パス
big.th100|100 MB 以上
big.th200|200 MB 以上
big.th500|500 MB 以上
big.th1g|1 GB 以上
log.copy|すべてコピー
log.open|ログフォルダーを開く
log.clear|表示を消す
log.copied|ログをクリップボードにコピーしました
toast.cleanDone|完了：{0} 解放、空き容量 {1} 増加
toast.pickNone|まだ何も選択されていません。削除する項目にチェックを入れてください。
toast.lang|表示言語を切り替えました
admin.chipYes|管理者
admin.chipNo|制限付き
admin.yes|管理者として実行中
admin.no|標準ユーザー（一部の項目は管理者権限が必要）
lastScan.none|未スキャン
lastScan.prefix|前回のスキャン {0}
lang.label|言語
cat.temp|一時
cat.dev|開発
cat.app|アプリ
cat.browser|ブラウザー
cat.sys|システム
step.temp-user|ユーザーの一時ファイル
step.temp-win|システムの一時ファイル
step.pkg|パッケージマネージャーのキャッシュ
step.dev|Python / .NET / Java のキャッシュ
step.crash|クラッシュダンプとエラー報告
step.updaters|更新プログラムの残骸
step.chrome|Chrome のキャッシュ
step.edge|Edge のキャッシュ
step.gfx|グラフィックとサムネイルのキャッシュ
step.sys|プリフェッチとフォントキャッシュ
step.recycle|ごみ箱
step.hiber|休止状態ファイル
step.drivers|古いドライバー（時間がかかります）";

        private const string Ko =
@"app.title|DiskMate · C 드라이브 정리
app.tagline|C 드라이브 정리 도구
nav.clean|검사 및 정리
nav.big|큰 파일
nav.log|로그
page.clean.sub|먼저 검사하고, 그다음 선택하세요. 검사는 용량만 확인하며 아무것도 삭제하지 않습니다.
page.big.sub|디스크에서 가장 큰 파일을 보여 줍니다. 두 번 클릭하면 탐색기에서 위치를 엽니다. 삭제하지 않습니다.
page.log.sub|모든 검사와 정리 작업의 기록이며 로컬 로그 파일에도 저장됩니다.
welcome.title|먼저 검사, 삭제는 그다음
welcome.desc|검사하면 C 드라이브의 캐시, 임시 파일, 오래된 드라이버가 하나씩 나열되고 각 항목의 위험도와 삭제 결과가 표시됩니다. 직접 체크하기 전에는 어떤 파일도 삭제되지 않습니다.
welcome.features|용량만 측정  ·  위험도와 결과 표시  ·  선택한 항목만 정리  ·  모든 과정 기록
welcome.start|검사 시작
welcome.hint|검사는 파일 크기만 읽습니다. 삭제, 이동, 수정은 하지 않습니다. F5 로도 시작할 수 있습니다.
scan.title|C 드라이브 검사 중
scan.preparing|준비 중…
scan.current|측정 중: {0}
scan.cancel|검사 취소
scan.cancelling|중지하는 중…
scan.cancelled|검사를 취소했습니다. 변경된 내용은 없습니다.
scan.done|검사 완료: {0}개, 합계 {1}
scan.failed|검사 실패: {0}
work.title|정리 중
work.preparing|준비 중…
work.current|삭제 중: {0}
work.hint|완료될 때까지 창을 닫지 마세요. 확보된 공간을 보여 드립니다.
clean.title|이 {0}개 항목을 정리할까요?
clean.sub|약 {0} 확보 · 삭제는 되돌릴 수 없습니다. 목록을 확인하세요
clean.stay|돌아가기
clean.go|정리 시작
clean.warnHigh|이 중 {0}개는 고위험: 삭제하면 복구할 수 없습니다.
clean.warnMid|이 중 {0}개는 중위험: 부작용이 있습니다. 설명을 확인하세요.
clean.allLow|모두 저위험 항목이며 캐시와 임시 파일뿐입니다.
clean.tip|모든 과정은 로그에 기록되며 로그 페이지에서 확인할 수 있습니다.
sel.none|선택된 항목 없음
sel.some|{0}개 선택 · {1}
btn.selectLow|저위험 모두 선택
btn.selectNone|선택 해제
btn.clean|선택 항목 정리
btn.cleanN|선택 항목 정리({0})
btn.rescan|다시 검사
search.ph|항목 검색
col.name|항목
col.cat|분류
col.size|크기
col.risk|위험도
empty.none|정리할 항목이 없습니다
empty.filter|일치하는 항목이 없습니다. 다른 검색어를 입력하거나 필터를 해제하세요.
det.title|항목 정보
det.impact|삭제 결과
det.advice|권장 사항
det.path|위치
det.add|정리 목록에 추가
det.remove|선택 해제
det.open|폴더 열기
det.nothing|선택된 항목 없음
det.hint|왼쪽 목록에서 항목을 선택하면 무엇이 삭제되고 어떤 영향이 있는지 표시됩니다.
stat.total|정리 가능 용량
stat.low|저위험
stat.mid|중위험
stat.high|고위험
stat.items|{0}개 항목
stat.count|{0}개
risk.low|저위험
risk.mid|중위험
risk.high|고위험
big.btn|큰 파일 검사
big.scanning|검사 중…
big.idle|아직 검사하지 않았습니다. '큰 파일 검사'를 눌러 주세요.
big.progress|검사 중… {0:N0}개 파일 확인, {1}개 발견
big.done|{0:N0}개 파일을 확인해 {2} 초과 {1}개를 찾았습니다(표시만 하며 삭제하지 않음)
big.cancel|검사를 취소했습니다
big.open|위치 열기
big.colsize|크기
big.colpath|경로
big.th100|100 MB 이상
big.th200|200 MB 이상
big.th500|500 MB 이상
big.th1g|1 GB 이상
log.copy|전체 복사
log.open|로그 폴더 열기
log.clear|화면 지우기
log.copied|로그를 클립보드에 복사했습니다
toast.cleanDone|정리 완료: {0} 확보, 여유 공간 {1} 증가
toast.pickNone|아직 선택된 항목이 없습니다. 정리할 항목을 먼저 체크하세요.
toast.lang|인터페이스 언어를 변경했습니다
admin.chipYes|관리자
admin.chipNo|제한됨
admin.yes|관리자 권한으로 실행 중
admin.no|일반 권한(일부 항목은 관리자 필요)
lastScan.none|검사 안 함
lastScan.prefix|마지막 검사 {0}
lang.label|언어
cat.temp|임시
cat.dev|개발
cat.app|앱
cat.browser|브라우저
cat.sys|시스템
step.temp-user|사용자 임시 파일
step.temp-win|시스템 임시 파일
step.pkg|패키지 관리자 캐시
step.dev|Python / .NET / Java 캐시
step.crash|크래시 덤프와 오류 보고서
step.updaters|설치 파일 잔여물
step.chrome|Chrome 캐시
step.edge|Edge 캐시
step.gfx|그래픽 및 썸네일 캐시
step.sys|프리페치와 글꼴 캐시
step.recycle|휴지통
step.hiber|최대 절전 파일
step.drivers|오래된 드라이버 패키지(느림)";

        private const string De =
@"app.title|DiskMate · C: bereinigen
app.tagline|C-Laufwerk aufräumen
nav.clean|Scannen & Reinigen
nav.big|Große Dateien
nav.log|Protokoll
page.clean.sub|Erst scannen, dann auswählen. Der Scan misst nur Größen und löscht nichts.
page.big.sub|Listet die größten Dateien auf. Doppelklick zeigt sie im Explorer. Nur Anzeige, kein Löschen.
page.log.sub|Vollständiges Protokoll aller Scans und Bereinigungen, zusätzlich in einer Logdatei.
welcome.title|Erst scannen, dann entscheiden
welcome.desc|Ein Scan listet Caches, temporäre Dateien und alte Treiber auf Laufwerk C: einzeln auf – mit Risikostufe und den Folgen des Löschens. Nichts wird entfernt, bevor Sie es anhaken.
welcome.features|Nur messen  ·  Risiko und Folgen  ·  Nur Ausgewähltes löschen  ·  Alles protokolliert
welcome.start|Scan starten
welcome.hint|Der Scan liest nur Dateigrößen. Nichts wird gelöscht, verschoben oder geändert. F5 funktioniert auch.
scan.title|Laufwerk C: wird gescannt
scan.preparing|Vorbereiten…
scan.current|Messe: {0}
scan.cancel|Scan abbrechen
scan.cancelling|Wird gestoppt…
scan.cancelled|Scan abgebrochen. Es wurde nichts geändert.
scan.done|Scan fertig: {0} Einträge, insgesamt {1}
scan.failed|Scan fehlgeschlagen: {0}
work.title|Wird bereinigt
work.preparing|Vorbereiten…
work.current|Lösche: {0}
work.hint|Fenster bitte offen lassen. Am Ende wird der freigegebene Speicher angezeigt.
clean.title|Diese {0} Einträge bereinigen?
clean.sub|Etwa {0} werden frei · Löschen ist endgültig, bitte Liste prüfen
clean.stay|Zurück
clean.go|Bereinigung starten
clean.warnHigh|{0} davon sind hohes Risiko: gelöschte Daten sind nicht wiederherstellbar.
clean.warnMid|{0} davon sind mittleres Risiko: Löschen hat Nebenwirkungen. Bitte Hinweise lesen.
clean.allLow|Alle Einträge sind niedriges Risiko: nur Caches und temporäre Dateien.
clean.tip|Jeder Schritt landet im Protokoll, einsehbar auf der Seite „Protokoll“.
sel.none|Nichts ausgewählt
sel.some|{0} ausgewählt · {1}
btn.selectLow|Niedriges Risiko wählen
btn.selectNone|Auswahl aufheben
btn.clean|Auswahl bereinigen
btn.cleanN|Auswahl bereinigen ({0})
btn.rescan|Erneut scannen
search.ph|Einträge suchen
col.name|Eintrag
col.cat|Kategorie
col.size|Größe
col.risk|Risiko
empty.none|Nichts zu bereinigen
empty.filter|Keine Treffer. Anderer Suchbegriff oder Filter zurücksetzen.
det.title|Details
det.impact|Folgen des Löschens
det.advice|Empfehlung
det.path|Speicherort
det.add|Zur Bereinigung
det.remove|Auswahl entfernen
det.open|Ordner öffnen
det.nothing|Kein Eintrag gewählt
det.hint|Eintrag links wählen, um zu sehen, was gelöscht wird und was das bedeutet.
stat.total|Bereinigbar
stat.low|Niedriges Risiko
stat.mid|Mittleres Risiko
stat.high|Hohes Risiko
stat.items|{0} Einträge
stat.count|{0} Einträge
risk.low|Niedriges Risiko
risk.mid|Mittleres Risiko
risk.high|Hohes Risiko
big.btn|Große Dateien scannen
big.scanning|Scannen…
big.idle|Noch nicht gescannt. Auf „Große Dateien scannen“ klicken.
big.progress|Scannen… {0:N0} Dateien geprüft, {1} Treffer
big.done|{0:N0} Dateien geprüft, {1} größer als {2} gefunden (nur Anzeige, nichts gelöscht)
big.cancel|Scan abgebrochen
big.open|Anzeigen
big.colsize|Größe
big.colpath|Pfad
big.th100|Größer als 100 MB
big.th200|Größer als 200 MB
big.th500|Größer als 500 MB
big.th1g|Größer als 1 GB
log.copy|Alles kopieren
log.open|Protokollordner öffnen
log.clear|Ansicht leeren
log.copied|Protokoll in die Zwischenablage kopiert
toast.cleanDone|Fertig: {0} frei, {1} mehr Speicherplatz
toast.pickNone|Noch nichts ausgewählt. Zuerst Einträge anhaken.
toast.lang|Sprache geändert
admin.chipYes|Admin
admin.chipNo|Eingeschränkt
admin.yes|Läuft als Administrator
admin.no|Standardbenutzer (einige Einträge brauchen Admin)
lastScan.none|Noch nicht gescannt
lastScan.prefix|Letzter Scan {0}
lang.label|Sprache
cat.temp|Temporär
cat.dev|Entwicklung
cat.app|Apps
cat.browser|Browser
cat.sys|System
step.temp-user|Temporäre Benutzerdateien
step.temp-win|Temporäre Systemdateien
step.pkg|Paketmanager-Caches
step.dev|Python / .NET / Java-Caches
step.crash|Absturzdumps und Fehlerberichte
step.updaters|Übrig gebliebene Installer
step.chrome|Chrome-Cache
step.edge|Edge-Cache
step.gfx|Grafik- und Miniaturen-Cache
step.sys|Prefetch und Schriftcache
step.recycle|Papierkorb
step.hiber|Ruhezustandsdatei
step.drivers|Alte Treiberpakete (langsam)";

        private const string Fr =
@"app.title|DiskMate · Nettoyage du disque C:
app.tagline|Nettoyage du lecteur C:
nav.clean|Analyse et nettoyage
nav.big|Gros fichiers
nav.log|Journal
page.clean.sub|Analysez d'abord, choisissez ensuite. L'analyse mesure seulement la taille, sans rien supprimer.
page.big.sub|Liste les plus gros fichiers du disque. Double-cliquez pour les afficher dans l'Explorateur. Aucune suppression.
page.log.sub|Journal complet de chaque analyse et nettoyage, également écrit dans un fichier local.
welcome.title|Analysez d'abord, décidez ensuite
welcome.desc|L'analyse liste un par un les caches, fichiers temporaires et anciens pilotes du lecteur C:, avec un niveau de risque et les conséquences exactes de la suppression. Rien n'est supprimé tant que vous n'avez pas coché.
welcome.features|Mesure uniquement  ·  Risque et conséquences  ·  Seulement ce que vous cochez  ·  Tout est journalisé
welcome.start|Lancer l'analyse
welcome.hint|L'analyse lit seulement les tailles. Rien n'est supprimé, déplacé ni modifié. F5 fonctionne aussi.
scan.title|Analyse du lecteur C:
scan.preparing|Préparation…
scan.current|Mesure : {0}
scan.cancel|Annuler l'analyse
scan.cancelling|Arrêt en cours…
scan.cancelled|Analyse annulée. Aucune modification.
scan.done|Analyse terminée : {0} éléments, {1} au total
scan.failed|Échec de l'analyse : {0}
work.title|Nettoyage en cours
work.preparing|Préparation…
work.current|Suppression : {0}
work.hint|Ne fermez pas la fenêtre. L'espace libéré s'affichera à la fin.
clean.title|Nettoyer ces {0} éléments ?
clean.sub|Environ {0} seront libérés · La suppression est définitive, vérifiez la liste
clean.stay|Retour
clean.go|Lancer le nettoyage
clean.warnHigh|{0} éléments à risque élevé : les données supprimées sont irrécupérables.
clean.warnMid|{0} éléments à risque moyen : la suppression a des effets. Lisez les notes.
clean.allLow|Tous les éléments sont à faible risque : uniquement caches et fichiers temporaires.
clean.tip|Chaque étape est journalisée et consultable dans la page « Journal ».
sel.none|Aucune sélection
sel.some|{0} sélectionnés · {1}
btn.selectLow|Sélectionner faible risque
btn.selectNone|Tout désélectionner
btn.clean|Nettoyer la sélection
btn.cleanN|Nettoyer la sélection ({0})
btn.rescan|Réanalyser
search.ph|Rechercher
col.name|Élément
col.cat|Catégorie
col.size|Taille
col.risk|Risque
empty.none|Rien à nettoyer
empty.filter|Aucun résultat. Essayez un autre mot-clé ou retirez le filtre.
det.title|Détails
det.impact|Conséquences
det.advice|Conseil
det.path|Emplacement
det.add|Ajouter au nettoyage
det.remove|Retirer
det.open|Ouvrir le dossier
det.nothing|Aucun élément sélectionné
det.hint|Sélectionnez un élément à gauche pour voir ce qu'il supprime et ce que cela implique.
stat.total|Nettoyable
stat.low|Risque faible
stat.mid|Risque moyen
stat.high|Risque élevé
stat.items|{0} éléments
stat.count|{0} éléments
risk.low|Risque faible
risk.mid|Risque moyen
risk.high|Risque élevé
big.btn|Analyser les gros fichiers
big.scanning|Analyse…
big.idle|Pas encore analysé. Cliquez sur « Analyser les gros fichiers ».
big.progress|Analyse… {0:N0} fichiers examinés, {1} trouvés
big.done|{0:N0} fichiers examinés, {1} de plus de {2} (affichage seul, rien n'est supprimé)
big.cancel|Analyse annulée
big.open|Afficher
big.colsize|Taille
big.colpath|Chemin
big.th100|Plus de 100 Mo
big.th200|Plus de 200 Mo
big.th500|Plus de 500 Mo
big.th1g|Plus de 1 Go
log.copy|Tout copier
log.open|Ouvrir le dossier des journaux
log.clear|Effacer l'affichage
log.copied|Journal copié dans le presse-papiers
toast.cleanDone|Nettoyage terminé : {0} libérés, {1} d'espace libre en plus
toast.pickNone|Aucun élément coché. Cochez d'abord ce que vous voulez nettoyer.
toast.lang|Langue de l'interface modifiée
admin.chipYes|Admin
admin.chipNo|Limité
admin.yes|Exécuté en administrateur
admin.no|Utilisateur standard (certains éléments exigent l'admin)
lastScan.none|Pas encore analysé
lastScan.prefix|Dernière analyse {0}
lang.label|Langue
cat.temp|Temporaire
cat.dev|Développement
cat.app|Applications
cat.browser|Navigateur
cat.sys|Système
step.temp-user|Fichiers temporaires utilisateur
step.temp-win|Fichiers temporaires système
step.pkg|Caches des gestionnaires de paquets
step.dev|Caches Python / .NET / Java
step.crash|Vidages de plantage et rapports
step.updaters|Installateurs résiduels
step.chrome|Cache Chrome
step.edge|Cache Edge
step.gfx|Caches graphiques et miniatures
step.sys|Prefetch et cache de polices
step.recycle|Corbeille
step.hiber|Fichier d'hibernation
step.drivers|Anciens pilotes (lent)";

        private const string Es =
@"app.title|DiskMate · Limpieza del disco C:
app.tagline|Limpieza del disco C:
nav.clean|Analizar y limpiar
nav.big|Archivos grandes
nav.log|Registro
page.clean.sub|Primero analiza y luego elige. El análisis solo mide tamaños, no borra nada.
page.big.sub|Muestra los archivos más grandes del disco. Doble clic para verlo en el Explorador. No se borra nada.
page.log.sub|Registro completo de cada análisis y limpieza, también guardado en un archivo local.
welcome.title|Analiza primero, decide después
welcome.desc|El análisis enumera uno por uno las cachés, archivos temporales y controladores antiguos del disco C:, con su nivel de riesgo y qué ocurre si se eliminan. No se borra nada hasta que lo marques.
welcome.features|Solo mide  ·  Riesgo y consecuencias  ·  Solo lo que marques  ·  Todo queda registrado
welcome.start|Iniciar análisis
welcome.hint|El análisis solo lee tamaños. No borra, mueve ni modifica nada. F5 también funciona.
scan.title|Analizando el disco C:
scan.preparing|Preparando…
scan.current|Midiendo: {0}
scan.cancel|Cancelar análisis
scan.cancelling|Deteniendo…
scan.cancelled|Análisis cancelado. No se cambió nada.
scan.done|Análisis completado: {0} elementos, {1} en total
scan.failed|Error de análisis: {0}
work.title|Limpiando
work.preparing|Preparando…
work.current|Eliminando: {0}
work.hint|No cierres la ventana. Al terminar verás cuánto espacio se liberó.
clean.title|¿Limpiar estos {0} elementos?
clean.sub|Se liberarán unos {0} · El borrado no se puede deshacer, revisa la lista
clean.stay|Volver
clean.go|Iniciar limpieza
clean.warnHigh|{0} son de riesgo alto: los datos borrados no se pueden recuperar.
clean.warnMid|{0} son de riesgo medio: borrarlos tiene efectos. Lee las notas.
clean.allLow|Todos son de riesgo bajo: solo cachés y archivos temporales.
clean.tip|Cada paso se registra y puedes consultarlo en la página Registro.
sel.none|Nada seleccionado
sel.some|{0} seleccionados · {1}
btn.selectLow|Seleccionar riesgo bajo
btn.selectNone|Quitar selección
btn.clean|Limpiar selección
btn.cleanN|Limpiar selección ({0})
btn.rescan|Analizar de nuevo
search.ph|Buscar elementos
col.name|Elemento
col.cat|Categoría
col.size|Tamaño
col.risk|Riesgo
empty.none|Nada que limpiar
empty.filter|Sin resultados. Prueba otra palabra o quita el filtro.
det.title|Detalles
det.impact|Qué ocurre al borrar
det.advice|Consejo
det.path|Ubicación
det.add|Añadir a la limpieza
det.remove|Quitar de la limpieza
det.open|Abrir carpeta
det.nothing|Ningún elemento seleccionado
det.hint|Elige un elemento a la izquierda para ver qué borra y qué consecuencias tiene.
stat.total|Limpiable
stat.low|Riesgo bajo
stat.mid|Riesgo medio
stat.high|Riesgo alto
stat.items|{0} elementos
stat.count|{0} elementos
risk.low|Riesgo bajo
risk.mid|Riesgo medio
risk.high|Riesgo alto
big.btn|Analizar archivos grandes
big.scanning|Analizando…
big.idle|Aún no se ha analizado. Pulsa «Analizar archivos grandes».
big.progress|Analizando… {0:N0} archivos revisados, {1} encontrados
big.done|{0:N0} archivos revisados, {1} mayores de {2} (solo se muestran, no se borran)
big.cancel|Análisis cancelado
big.open|Ver ubicación
big.colsize|Tamaño
big.colpath|Ruta
big.th100|Más de 100 MB
big.th200|Más de 200 MB
big.th500|Más de 500 MB
big.th1g|Más de 1 GB
log.copy|Copiar todo
log.open|Abrir carpeta de registros
log.clear|Limpiar vista
log.copied|Registro copiado al portapapeles
toast.cleanDone|Limpieza terminada: {0} liberados, {1} más de espacio libre
toast.pickNone|Todavía no hay nada marcado. Marca lo que quieras limpiar.
toast.lang|Idioma de la interfaz cambiado
admin.chipYes|Admin
admin.chipNo|Limitado
admin.yes|Ejecutando como administrador
admin.no|Usuario estándar (algunos elementos requieren admin)
lastScan.none|Sin analizar
lastScan.prefix|Último análisis {0}
lang.label|Idioma
cat.temp|Temporal
cat.dev|Desarrollo
cat.app|Aplicaciones
cat.browser|Navegador
cat.sys|Sistema
step.temp-user|Archivos temporales del usuario
step.temp-win|Archivos temporales del sistema
step.pkg|Cachés de gestores de paquetes
step.dev|Cachés de Python / .NET / Java
step.crash|Volcados y reportes de errores
step.updaters|Instaladores sobrantes
step.chrome|Caché de Chrome
step.edge|Caché de Edge
step.gfx|Cachés gráficas y de miniaturas
step.sys|Prefetch y caché de fuentes
step.recycle|Papelera
step.hiber|Archivo de hibernación
step.drivers|Controladores antiguos (lento)";

        private const string Pt =
@"app.title|DiskMate · Limpeza do disco C:
app.tagline|Limpeza do disco C:
nav.clean|Analisar e limpar
nav.big|Arquivos grandes
nav.log|Registro
page.clean.sub|Analise primeiro, escolha depois. A análise só mede tamanhos e não apaga nada.
page.big.sub|Lista os maiores arquivos do disco. Dê dois cliques para abrir no Explorador. Apenas exibe, não apaga.
page.log.sub|Registro completo de cada análise e limpeza, também gravado em um arquivo local.
welcome.title|Analise primeiro, decida depois
welcome.desc|A análise lista um a um os caches, arquivos temporários e drivers antigos do disco C:, com o nível de risco e o efeito exato de apagar cada item. Nada é removido antes de você marcar.
welcome.features|Só mede  ·  Risco e consequências  ·  Só o que você marcar  ·  Tudo registrado
welcome.start|Iniciar análise
welcome.hint|A análise só lê tamanhos. Nada é apagado, movido ou alterado. F5 também funciona.
scan.title|Analisando o disco C:
scan.preparing|Preparando…
scan.current|Medindo: {0}
scan.cancel|Cancelar análise
scan.cancelling|Parando…
scan.cancelled|Análise cancelada. Nada foi alterado.
scan.done|Análise concluída: {0} itens, {1} no total
scan.failed|Falha na análise: {0}
work.title|Limpando
work.preparing|Preparando…
work.current|Apagando: {0}
work.hint|Não feche a janela. Ao terminar você verá quanto espaço foi liberado.
clean.title|Limpar estes {0} itens?
clean.sub|Cerca de {0} serão liberados · A exclusão não pode ser desfeita, confira a lista
clean.stay|Voltar
clean.go|Iniciar limpeza
clean.warnHigh|{0} são de risco alto: os dados apagados não podem ser recuperados.
clean.warnMid|{0} são de risco médio: apagar tem efeitos. Leia as notas.
clean.allLow|Todos são de risco baixo: apenas caches e arquivos temporários.
clean.tip|Cada etapa vai para o registro, disponível na página Registro.
sel.none|Nada selecionado
sel.some|{0} selecionados · {1}
btn.selectLow|Selecionar risco baixo
btn.selectNone|Limpar seleção
btn.clean|Limpar seleção
btn.cleanN|Limpar seleção ({0})
btn.rescan|Analisar de novo
search.ph|Buscar itens
col.name|Item
col.cat|Categoria
col.size|Tamanho
col.risk|Risco
empty.none|Nada para limpar
empty.filter|Nenhum item encontrado. Tente outra palavra ou remova o filtro.
det.title|Detalhes
det.impact|O que acontece
det.advice|Recomendação
det.path|Local
det.add|Adicionar à limpeza
det.remove|Remover da limpeza
det.open|Abrir pasta
det.nothing|Nenhum item selecionado
det.hint|Escolha um item à esquerda para ver o que ele apaga e quais são as consequências.
stat.total|Limpável
stat.low|Risco baixo
stat.mid|Risco médio
stat.high|Risco alto
stat.items|{0} itens
stat.count|{0} itens
risk.low|Risco baixo
risk.mid|Risco médio
risk.high|Risco alto
big.btn|Analisar arquivos grandes
big.scanning|Analisando…
big.idle|Ainda não analisado. Clique em “Analisar arquivos grandes”.
big.progress|Analisando… {0:N0} arquivos verificados, {1} encontrados
big.done|{0:N0} arquivos verificados, {1} maiores que {2} (apenas exibição, nada é apagado)
big.cancel|Análise cancelada
big.open|Mostrar local
big.colsize|Tamanho
big.colpath|Caminho
big.th100|Maior que 100 MB
big.th200|Maior que 200 MB
big.th500|Maior que 500 MB
big.th1g|Maior que 1 GB
log.copy|Copiar tudo
log.open|Abrir pasta de registros
log.clear|Limpar exibição
log.copied|Registro copiado para a área de transferência
toast.cleanDone|Limpeza concluída: {0} liberados, {1} a mais de espaço livre
toast.pickNone|Nada marcado ainda. Marque os itens que deseja limpar.
toast.lang|Idioma da interface alterado
admin.chipYes|Admin
admin.chipNo|Limitado
admin.yes|Executando como administrador
admin.no|Usuário padrão (alguns itens exigem admin)
lastScan.none|Não analisado
lastScan.prefix|Última análise {0}
lang.label|Idioma
cat.temp|Temporário
cat.dev|Desenvolvimento
cat.app|Aplicativos
cat.browser|Navegador
cat.sys|Sistema
step.temp-user|Arquivos temporários do usuário
step.temp-win|Arquivos temporários do sistema
step.pkg|Caches de gerenciadores de pacotes
step.dev|Caches de Python / .NET / Java
step.crash|Despejos de falha e relatórios
step.updaters|Instaladores restantes
step.chrome|Cache do Chrome
step.edge|Cache do Edge
step.gfx|Caches gráficos e de miniaturas
step.sys|Prefetch e cache de fontes
step.recycle|Lixeira
step.hiber|Arquivo de hibernação
step.drivers|Drivers antigos (lento)";

        private const string Ru =
@"app.title|DiskMate · Очистка диска C:
app.tagline|Очистка диска C:
nav.clean|Сканирование и очистка
nav.big|Большие файлы
nav.log|Журнал
page.clean.sub|Сначала сканирование, потом выбор. Сканирование только измеряет размеры и ничего не удаляет.
page.big.sub|Показывает самые большие файлы. Двойной щелчок открывает их в Проводнике. Только просмотр.
page.log.sub|Полный журнал всех сканирований и очисток, дублируется в локальный файл.
welcome.title|Сначала сканирование, потом решение
welcome.desc|Сканирование по очереди выводит кэши, временные файлы и старые драйверы на диске C:, с уровнем риска и последствиями удаления. Пока вы не отметите пункт, ничего не удаляется.
welcome.features|Только измерение  ·  Риск и последствия  ·  Удаляется лишь выбранное  ·  Всё в журнале
welcome.start|Начать сканирование
welcome.hint|Сканирование только читает размеры. Ничего не удаляется и не изменяется. F5 тоже работает.
scan.title|Сканирование диска C:
scan.preparing|Подготовка…
scan.current|Измерение: {0}
scan.cancel|Отменить сканирование
scan.cancelling|Остановка…
scan.cancelled|Сканирование отменено. Ничего не изменено.
scan.done|Готово: {0} элементов, всего {1}
scan.failed|Ошибка сканирования: {0}
work.title|Очистка
work.preparing|Подготовка…
work.current|Удаление: {0}
work.hint|Не закрывайте окно. По завершении будет показано, сколько места освобождено.
clean.title|Очистить эти элементы ({0})?
clean.sub|Освободится около {0} · Удаление необратимо, проверьте список
clean.stay|Назад
clean.go|Начать очистку
clean.warnHigh|{0} из них с высоким риском: удалённые данные не восстановить.
clean.warnMid|{0} из них со средним риском: удаление имеет последствия. Прочитайте описание.
clean.allLow|Все элементы с низким риском: только кэши и временные файлы.
clean.tip|Каждый шаг записывается в журнал на странице «Журнал».
sel.none|Ничего не выбрано
sel.some|Выбрано {0} · {1}
btn.selectLow|Выбрать низкий риск
btn.selectNone|Снять выбор
btn.clean|Очистить выбранное
btn.cleanN|Очистить выбранное ({0})
btn.rescan|Сканировать снова
search.ph|Поиск элементов
col.name|Элемент
col.cat|Категория
col.size|Размер
col.risk|Риск
empty.none|Нечего очищать
empty.filter|Нет совпадений. Измените запрос или снимите фильтр.
det.title|Подробности
det.impact|Что будет при удалении
det.advice|Совет
det.path|Расположение
det.add|Добавить к очистке
det.remove|Убрать из очистки
det.open|Открыть папку
det.nothing|Элемент не выбран
det.hint|Выберите элемент слева, чтобы увидеть, что он удаляет и какие будут последствия.
stat.total|Можно очистить
stat.low|Низкий риск
stat.mid|Средний риск
stat.high|Высокий риск
stat.items|{0} элементов
stat.count|{0} элементов
risk.low|Низкий риск
risk.mid|Средний риск
risk.high|Высокий риск
big.btn|Искать большие файлы
big.scanning|Сканирование…
big.idle|Ещё не сканировалось. Нажмите «Искать большие файлы».
big.progress|Сканирование… проверено {0:N0} файлов, найдено {1}
big.done|Проверено {0:N0} файлов, найдено {1} больше {2} (только показ, ничего не удаляется)
big.cancel|Сканирование отменено
big.open|Показать
big.colsize|Размер
big.colpath|Путь
big.th100|Больше 100 МБ
big.th200|Больше 200 МБ
big.th500|Больше 500 МБ
big.th1g|Больше 1 ГБ
log.copy|Копировать всё
log.open|Открыть папку журналов
log.clear|Очистить вид
log.copied|Журнал скопирован в буфер обмена
toast.cleanDone|Очистка завершена: освобождено {0}, свободного места больше на {1}
toast.pickNone|Пока ничего не отмечено. Отметьте элементы для очистки.
toast.lang|Язык интерфейса изменён
admin.chipYes|Админ
admin.chipNo|Ограничено
admin.yes|Запущено от администратора
admin.no|Обычный пользователь (часть пунктов требует админа)
lastScan.none|Не сканировалось
lastScan.prefix|Прошлое сканирование {0}
lang.label|Язык
cat.temp|Временные
cat.dev|Разработка
cat.app|Приложения
cat.browser|Браузер
cat.sys|Система
step.temp-user|Временные файлы пользователя
step.temp-win|Временные файлы системы
step.pkg|Кэши менеджеров пакетов
step.dev|Кэши Python / .NET / Java
step.crash|Дампы и отчёты об ошибках
step.updaters|Остатки установщиков
step.chrome|Кэш Chrome
step.edge|Кэш Edge
step.gfx|Кэши графики и эскизов
step.sys|Prefetch и кэш шрифтов
step.recycle|Корзина
step.hiber|Файл гибернации
step.drivers|Старые драйверы (медленно)";
    }
}
