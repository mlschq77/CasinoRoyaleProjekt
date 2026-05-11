$ErrorActionPreference = "Stop"

$outPath = Join-Path $PSScriptRoot "Blackjack-dzialanie-projektu.pdf"

$sections = @(
    @{ Title = "Blackjack w projekcie CasinoRoyale"; Body = @(
        "Ten dokument opisuje pelne dzialanie modulu Blackjack w aplikacji CasinoRoyale.",
        "Opis obejmuje backend ASP.NET Core, Entity Framework, model bazy danych, API, logike gry, obsluge salda, split, double face up, double face down oraz warstwe frontendowa.",
        "Stan aktualny po poprawkach: gra domyka przegrane rundy, pozwala wystartowac nowa rozgrywke, pokazuje aktywna reke przy splicie, rozlicza split osobno per reka i prowadzi wolniejsze animacje."
    )},
    @{ Title = "Glowne pliki"; Body = @(
        "Controllers/BlackjackController.cs - API HTTP dla startu gry i akcji gracza.",
        "Services/BlackjackService.cs - cala logika talii, rozdan, punktacji, splitu, double i rozliczen.",
        "Models/BlackjackGame.cs - encja zapisywana w tabeli BlackjackGames.",
        "Models/BlackjackCard.cs - karta z kolorem, ranga i flaga FaceDown.",
        "Data/Automaty.cs - DbContext oraz konfiguracja tabeli BlackjackGames.",
        "wwwroot/js/games/blackjack.js - renderowanie kart, akcje przyciskow, animacje i sekwencja konca rundy.",
        "wwwroot/css/blackjack.css - wyglad stolu, kart, aktywnej reki i wynikow."
    )},
    @{ Title = "Model danych BlackjackGame"; Body = @(
        "Kazda partia jest zapisywana jako wiersz w tabeli BlackjackGames.",
        "DeckJson przechowuje pozostale karty w talii po rozdaniu i kolejnych dobieraniach.",
        "PlayerHandJson przechowuje pierwsza reke gracza.",
        "DealerHandJson przechowuje reke krupiera; druga karta startowa jest ukryta przez FaceDown.",
        "SplitHandJson przechowuje druga reke po wykonaniu splitu.",
        "BetAmount to stawka glownej reki.",
        "SplitBetAmount to stawka drugiej reki po splicie.",
        "IsActive mowi, czy partia nadal trwa.",
        "IsPlayerTurn mowi, czy gracz moze klikac akcje.",
        "IsSplitActive mowi, czy split zostal wlaczony.",
        "IsPlayingSplitHand mowi, czy obecnie grana jest druga reka.",
        "PayoutProcessed zabezpiecza przed wielokrotnym wyplaceniem tej samej rundy.",
        "Result i WinAmount przechowuja wynik calosci rundy.",
        "CreatedAt oraz FinishedAt pozwalaja sledzic czas partii."
    )},
    @{ Title = "Start rundy"; Body = @(
        "Frontend wysyla POST /api/blackjack/start z polem bet.",
        "Kontroler pobiera identyfikator uzytkownika z ciasteczka logowania.",
        "Serwis sprawdza, czy stawka jest wieksza od zera.",
        "Jesli uzytkownik ma aktywna niedokonczona gre, zostaje ona oznaczona jako abandoned.",
        "BalanceService.PlaceBetAsync pobiera stawke z salda.",
        "Tworzona jest potasowana talia 52 kart.",
        "Gracz dostaje dwie karty, krupier dostaje jedna jawna i jedna zakryta.",
        "Stan gry jest serializowany do JSON i zapisywany w bazie.",
        "API zwraca state oraz aktualne saldo."
    )},
    @{ Title = "Punktacja"; Body = @(
        "Karty 2-10 maja wartosc zgodna z ranga.",
        "J, Q i K maja wartosc 10.",
        "As startowo ma wartosc 11.",
        "Jesli suma przekracza 21, asy sa kolejno obnizane z 11 do 1.",
        "Karty FaceDown nie licza sie do widocznej punktacji.",
        "Blackjack to dwie karty o lacznej wartosci 21.",
        "Bust oznacza wartosc wieksza niz 21."
    )},
    @{ Title = "Hit"; Body = @(
        "POST /api/blackjack/hit dobiera jedna karte do aktualnie granej reki.",
        "Jesli split nie jest aktywny, karta trafia do PlayerHandJson.",
        "Jesli aktywna jest druga reka splitu, karta trafia do SplitHandJson.",
        "Gdy pierwsza reka po splicie przekroczy 21, gra automatycznie przechodzi na druga reke.",
        "Gdy ostatnia aktywna reka przekroczy 21, backend od razu domyka runde.",
        "To naprawia blad, w ktorym gracz po przegranej nie mogl zaczac kolejnej gry."
    )},
    @{ Title = "Stand"; Body = @(
        "POST /api/blackjack/stand konczy ture aktualnej reki.",
        "Jesli gracz stoi na pierwszej rece po splicie, backend przelacza na druga reke.",
        "Jesli to ostatnia reka, backend przekazuje ture krupierowi.",
        "Krupier odslania zakryta karte i dobiera do wartosci co najmniej 17.",
        "Po dobraniu backend rozlicza wynik i zapisuje koniec rundy."
    )},
    @{ Title = "Double"; Body = @(
        "POST /api/blackjack/double podwaja stawke glownej reki.",
        "Double jest dostepny tylko gdy glowna reka ma dokladnie dwie karty.",
        "Backend pobiera druga stawke z salda przez BalanceService.PlaceBetAsync.",
        "Do reki gracza trafia dokladnie jedna karta.",
        "Po tej karcie tura gracza sie konczy i krupier rozgrywa swoja reke.",
        "Frontend ma dwa tryby: Face up i Face down.",
        "Face up pokazuje karte double od razu, potem czeka i powoli przechodzi do krupiera.",
        "Face down najpierw pokazuje karte zakryta, trzyma ja chwile, potem odslania i dopiero po pauzie pokazuje ruch krupiera.",
        "Sam backend rozlicza identycznie oba tryby; roznica jest prezentacyjna."
    )},
    @{ Title = "Split"; Body = @(
        "POST /api/blackjack/split jest dostepny, gdy dwie startowe karty maja taka sama wartosc.",
        "Backend pobiera druga stawke rowna BetAmount.",
        "Pierwsza karta zostaje w glownej rece i dostaje jedna nowa karte.",
        "Druga karta przechodzi do SplitHandJson i tez dostaje jedna nowa karte.",
        "Frontend podswietla aktualnie grana reke etykieta w grze.",
        "Po zakonczeniu rundy kazda reka dostaje osobny wynik.",
        "Jesli jedna reka wygra, a druga przegra, globalny baner nie mowi juz po prostu przegrana.",
        "Zamiast tego pokazuje Split rozliczony osobno, a przy kazdej rece widac Wygrana, Przegrana albo Remis."
    )},
    @{ Title = "Rozliczenie wygranych"; Body = @(
        "Bez splitu blackjack wyplaca stake plus 1.5x stake jako zysk, czyli razem 2.5x stawki.",
        "Zwykla wygrana wyplaca 2x stawki, czyli zwrot stawki plus zysk.",
        "Remis wyplaca 1x stawki, czyli zwrot.",
        "Przegrana wyplaca 0.",
        "Przy splicie kazda reka jest rozliczana osobno funkcja ResolveHand.",
        "WinAmount jest suma wyplat obu rak.",
        "BalanceService.PayoutAsync dopisuje wyplate tylko raz po zakonczeniu rundy.",
        "PayoutProcessed jest ustawiane na true, aby uniknac podwojnej wyplaty."
    )},
    @{ Title = "API"; Body = @(
        "GET /api/blackjack/balance zwraca saldo uzytkownika.",
        "POST /api/blackjack/start przyjmuje { bet }.",
        "POST /api/blackjack/hit przyjmuje { gameId }.",
        "POST /api/blackjack/stand przyjmuje { gameId }.",
        "POST /api/blackjack/double przyjmuje { gameId, faceDown }.",
        "POST /api/blackjack/split przyjmuje { gameId }.",
        "Kazda akcja zwraca state i opcjonalnie balance.",
        "State zawiera karty, wartosci punktowe, flagi aktywnosci, mozliwe akcje, wynik i handResults."
    )},
    @{ Title = "Frontend i animacje"; Body = @(
        "blackjack.js renderuje karty jako elementy HTML z klasami bj-card, bj-card-face albo bj-card-back.",
        "Nowe tempo jest celowo wolniejsze: akcja gracza, pauza, odsloniecie krupiera, dobieranie kart krupiera po jednej, pauza, wynik.",
        "TIMING.doublePeek steruje czasem trzymania karty double.",
        "TIMING.revealDealer steruje pauza po odslonieciu krupiera.",
        "TIMING.dealerCard steruje tempem dobierania kolejnych kart krupiera.",
        "CSS wydluza animacje rozdania i odsloniecia, aby calosc byla plynniejsza.",
        "Przy splicie JS dodaje klasy bj-hand-active, bj-hand-win, bj-hand-lose lub bj-hand-push."
    )},
    @{ Title = "Migracje i baza"; Body = @(
        "Tabela BlackjackGames jest dodana migracja AddBlackjackGames.",
        "DbContext Automaty zawiera DbSet<BlackjackGame>.",
        "Pola pieniezne BetAmount, SplitBetAmount i WinAmount maja decimal(18,2).",
        "Aplikacja uruchamia dbContext.Database.Migrate() przy starcie, wiec brakujace migracje powinny wejsc automatycznie.",
        "Do lokalnego uruchomienia baza SQL Server moze byc odpalona przez docker compose."
    )},
    @{ Title = "Najwazniejsze poprawki"; Body = @(
        "1. BlackjackService jest zarejestrowany w DI, wiec kontroler API moze dzialac.",
        "2. Dodano tabele BlackjackGames i zastosowano migracje.",
        "3. Hit po bust domyka runde, zamiast zostawiac aktywna gre bez tury gracza.",
        "4. Split zwraca handResults dla obu rak.",
        "5. Frontend pokazuje wynik per reka przy splicie.",
        "6. Double face up i face down maja plynniejsza sekwencje i wolniejszy ruch krupiera.",
        "7. Globalny wynik splitu nie ukrywa czesciowej wygranej."
    )}
)

function Escape-PdfText([string]$text) {
    return $text.Replace("\", "\\").Replace("(", "\(").Replace(")", "\)")
}

function Wrap-Line([string]$text, [int]$width) {
    $words = $text -split "\s+"
    $lines = New-Object System.Collections.Generic.List[string]
    $line = ""

    foreach ($word in $words) {
        if (($line.Length + $word.Length + 1) -gt $width) {
            if ($line.Length -gt 0) { $lines.Add($line) }
            $line = $word
        } else {
            if ($line.Length -eq 0) { $line = $word } else { $line += " " + $word }
        }
    }

    if ($line.Length -gt 0) { $lines.Add($line) }
    return $lines
}

$pages = New-Object System.Collections.Generic.List[object]
$current = New-Object System.Collections.Generic.List[object]
$y = 760

foreach ($section in $sections) {
    if ($y -lt 120) {
        $pages.Add($current)
        $current = New-Object System.Collections.Generic.List[object]
        $y = 760
    }

    $current.Add(@{ Text = $section.Title; Size = 16; Y = $y; Bold = $true })
    $y -= 28

    foreach ($paragraph in $section.Body) {
        foreach ($line in (Wrap-Line $paragraph 92)) {
            if ($y -lt 70) {
                $pages.Add($current)
                $current = New-Object System.Collections.Generic.List[object]
                $y = 760
            }
            $current.Add(@{ Text = $line; Size = 10; Y = $y; Bold = $false })
            $y -= 15
        }
        $y -= 6
    }

    $y -= 10
}

if ($current.Count -gt 0) { $pages.Add($current) }

$objects = New-Object System.Collections.Generic.List[string]
$objects.Add("<< /Type /Catalog /Pages 2 0 R >>")

$kids = New-Object System.Collections.Generic.List[string]
$pageStart = 3
$fontObjectId = $pageStart + ($pages.Count * 2)

for ($i = 0; $i -lt $pages.Count; $i++) {
    $kids.Add("$($pageStart + ($i * 2)) 0 R")
}

$objects.Add("<< /Type /Pages /Kids [ $($kids -join ' ') ] /Count $($pages.Count) >>")

for ($i = 0; $i -lt $pages.Count; $i++) {
    $pageObjectId = $pageStart + ($i * 2)
    $contentObjectId = $pageObjectId + 1
    $objects.Add("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 $fontObjectId 0 R >> >> /Contents $contentObjectId 0 R >>")

    $streamLines = New-Object System.Collections.Generic.List[string]
    $streamLines.Add("BT")
    foreach ($entry in $pages[$i]) {
        $font = "/F1 $($entry.Size) Tf"
        $text = Escape-PdfText $entry.Text
        $streamLines.Add($font)
        $streamLines.Add("50 $($entry.Y) Td ($text) Tj")
        $streamLines.Add("-50 -$($entry.Y) Td")
    }
    $streamLines.Add("ET")
    $stream = ($streamLines -join "`n")
    $objects.Add("<< /Length $([Text.Encoding]::ASCII.GetByteCount($stream)) >>`nstream`n$stream`nendstream")
}

$objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>")

$builder = New-Object System.Text.StringBuilder
[void]$builder.Append("%PDF-1.4`n")
$offsets = @(0)

for ($i = 0; $i -lt $objects.Count; $i++) {
    $offsets += [Text.Encoding]::ASCII.GetByteCount($builder.ToString())
    [void]$builder.Append("$($i + 1) 0 obj`n$($objects[$i])`nendobj`n")
}

$xrefOffset = [Text.Encoding]::ASCII.GetByteCount($builder.ToString())
[void]$builder.Append("xref`n0 $($objects.Count + 1)`n")
[void]$builder.Append("0000000000 65535 f `n")

for ($i = 1; $i -lt $offsets.Count; $i++) {
    [void]$builder.Append(("{0:D10} 00000 n `n" -f $offsets[$i]))
}

[void]$builder.Append("trailer`n<< /Size $($objects.Count + 1) /Root 1 0 R >>`nstartxref`n$xrefOffset`n%%EOF")
[IO.File]::WriteAllText($outPath, $builder.ToString(), [Text.Encoding]::ASCII)

Write-Output $outPath
