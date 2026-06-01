# Diagramy UML - Casino Royale

Ponizsze diagramy sa uproszczona wersja modelu aplikacji. Bazuja na strukturze projektu ASP.NET MVC: encjach katalogu gier (`AutomatInfo`, `Kategoria`, `AutomatProvider`), kontekscie bazy danych (`Automaty`), kontrolerach oraz serwisach obslugujacych gry i saldo.

## 1. Diagram klas

```plantuml
@startuml
skinparam classAttributeIconSize 0

class User {
  +Id: int
  +Imie: string
  +Nazwisko: string
  +Nazwa: string
  +Email: string
  +IsAdmin: bool
  +Balance: decimal
  +KycStatus: UserKycStatus
}

class AutomatInfo {
  +Id: int
  +Nazwa: string
  +ProviderId: int?
  +Kategorie: ICollection<Kategoria>
}

class AutomatProvider {
  +Id: int
  +Nazwa: string
  +Automaty: ICollection<AutomatInfo>
}

class Kategoria {
  +Id: int
  +Nazwa: string
  +Automaty: ICollection<AutomatInfo>
}

class AutomatKategoria {
  +AutomatId: int
  +KategoriaId: int
}

class GameResult {
  +Id: int
  +UserId: int
  +GameKey: string
  +BetAmount: decimal
  +WinAmount: decimal
  +PlayedAtUtc: DateTime
}

class MinesGame {
  +Id: int
  +UserId: int
  +GridSize: int
  +MineCount: int
  +BetAmount: decimal
  +IsActive: bool
}

class BlackjackGame {
  +Id: int
  +UserId: int
  +BetAmount: decimal
  +IsActive: bool
  +Result: string
  +WinAmount: decimal
}

class Automaty {
  +AutomatyInfo: DbSet<AutomatInfo>
  +Kategorie: DbSet<Kategoria>
  +AutomatProviderzy: DbSet<AutomatProvider>
  +Users: DbSet<User>
  +MinesGames: DbSet<MinesGame>
  +BlackjackGames: DbSet<BlackjackGame>
}

class AutomatyController {
  +Oferta(kategoria): IActionResult
}

class GraController {
  +Graj(id): IActionResult
}

class GamesController {
  +Mines(): IActionResult
  +Blackjack(): IActionResult
  +Plinko(): IActionResult
  +Crash(): IActionResult
  +Roulette(): IActionResult
  +Dice(): IActionResult
  +Keno(): IActionResult
}

class AdminController {
  +Index(): IActionResult
  +Statystyki(): IActionResult
}

interface IBalanceService {
  +GetBalanceAsync(userId): decimal?
  +PlaceBetAsync(userId, amount): BalanceResult
  +PayoutAsync(userId, amount): BalanceResult
}

class BalanceService

AutomatProvider "1" -- "0..*" AutomatInfo : dostarcza
AutomatInfo "0..*" -- "0..*" Kategoria : nalezy do
AutomatKategoria .. AutomatInfo
AutomatKategoria .. Kategoria
User "1" -- "0..*" GameResult : posiada wyniki
User "1" -- "0..*" MinesGame : gra
User "1" -- "0..*" BlackjackGame : gra

AutomatyController --> Automaty : odczytuje katalog
AdminController --> Automaty : administruje danymi
GraController --> GamesController : przekierowuje
BalanceService ..|> IBalanceService
BalanceService --> Automaty : aktualizuje saldo
Automaty --> AutomatInfo
Automaty --> User
@enduml
```

## 2. Diagram stanow - gra w katalogu

Ten diagram opisuje uproszczony cykl zycia gry widocznej w katalogu. W projekcie czesc gier jest dopisywana recznie jako `Originals`, a czesc pochodzi z bazy danych, wiec stan `Robocza` i `OczekujeNaPublikacje` mozna traktowac jako rozsadne rozszerzenie administracyjne.

```plantuml
@startuml
[*] --> Robocza

Robocza --> Walidowana : admin uzupelnia nazwe,\nprovidera i kategorie
Walidowana --> OczekujeNaPublikacje : dane poprawne
Walidowana --> Robocza : blad walidacji

OczekujeNaPublikacje --> Opublikowana : zapis do bazy\nSaveChanges()
Opublikowana --> WidocznaWKatalogu : AutomatyController.Oferta()

WidocznaWKatalogu --> Uruchamiana : uzytkownik klika "Graj"
Uruchamiana --> Aktywna : GraController.Graj(id)\nprzekierowuje do gry
Aktywna --> Zakonczona : runda zakonczona
Zakonczona --> WidocznaWKatalogu : powrot do katalogu

WidocznaWKatalogu --> Ukryta : admin dezaktywuje gre
Ukryta --> WidocznaWKatalogu : admin publikuje ponownie
Ukryta --> Usunieta : admin usuwa gre
Usunieta --> [*]
@enduml
```

## 3. Diagram przebiegu - dodanie gry do katalogu

```plantuml
@startuml
actor Admin
boundary "Widok panelu admina" as View
control "AdminController" as AdminController
database "Automaty DbContext" as Db
entity "AutomatInfo" as Automat
entity "AutomatProvider" as Provider
entity "Kategoria" as Category
boundary "AutomatyController.Oferta" as Oferta
actor Uzytkownik

Admin -> View : otwiera formularz dodania gry
View -> Admin : pokazuje pola: nazwa,\nprovider, kategorie
Admin -> View : wypelnia formularz i zatwierdza
View -> AdminController : POST DodajGre(model)

AdminController -> AdminController : sprawdz uprawnienia admina
AdminController -> AdminController : zwaliduj nazwe i kategorie

alt dane niepoprawne
  AdminController --> View : komunikat bledu
else dane poprawne
  AdminController -> Db : znajdz albo utworz providera
  Db --> AdminController : Provider
  AdminController -> Db : znajdz kategorie
  Db --> AdminController : lista kategorii
  AdminController -> Automat : utworz nowy AutomatInfo
  AdminController -> Automat : przypisz Provider i Kategorie
  AdminController -> Db : Add(AutomatInfo)
  AdminController -> Db : SaveChanges()
  Db --> AdminController : zapisano gre
  AdminController --> View : przekierowanie do katalogu/admina
end

Uzytkownik -> Oferta : wejscie w katalog gier
Oferta -> Db : pobierz AutomatyInfo\nz Provider i Kategorie
Db --> Oferta : lista gier
Oferta --> Uzytkownik : wyswietla nowa gre w katalogu
@enduml
```
