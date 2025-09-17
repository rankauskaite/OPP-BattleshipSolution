# BattleshipSolution

## 1. Projekto struktūra

```
BattleshipSolution/
├─ BattleshipServer/       # Serverio projektas (Console App)
├─ BattleshipClient/       # Kliento projektas (WinForms)
└─ README.md
```

- **Server**: valdo žaidimo logiką, poruoja žaidėjus, priima ir siūlo WebSocket žinutes.
- **Client**: WinForms programa, kurioje žaidėjas gali pasirinkti vardą, padėti laivus, šaudyti, gauti rezultatą ir žaisti iš naujo.

---

## 2. Diegimas

1. Atidaryk `BattleshipSolution.sln` Visual Studio.
2. Build: `Build Solution` arba CLI: `dotnet build`.
3. Užtikrink, kad serveris naudoja WebSocket adresą `ws://localhost:5000/ws/`.

---

## 3. Paleidimas

### 3.1 Serveris

**Visual Studio**:

1. Nustatyk `BattleshipServer` kaip startup projektą.
2. Paspausk `Start (Ctrl+F5)`.

**CLI**:

```bash
cd BattleshipSolution/BattleshipServer
dotnet run
```

Serveris turėtų parašyti: `Server listening on http://localhost:5000/ws/`.

### 3.2 Klientai (WinForms)

**Visual Studio**:

1. Dešiniuoju pelės mygtuku spustelėk `BattleshipClient` → `Debug` → `Start new instance` (paleisk du kartus, kad turėtum du žaidėjus).

**CLI** (vienam klientui):

```bash
cd BattleshipSolution/BattleshipClient
dotnet run
```

Arba sukurk Release build ir paleisk `.exe` du kartus.

---

## 4. Žaidimo eiga

1. Kiekviename kliente įvesk vardą → `Connect`.
2. Kai abu prisijungę, serveris suporins žaidėjus (gausi paired info).
3. Kiekvienas klientas paspaudžia `Randomize ships` → `Ready`.
4. Kai abu pasiruošę, gausi `Game started` ir bus nurodytas pradinio žaidėjo eilės statusas.
5. Pažeidimai: paspausk priešo lentą savo eilės metu, serveris atsakys su `shotResult`.
6. Žaidimo pabaiga: pasirodys `Game Over` pranešimas, galėsi pasirinkti žaisti vėl arba išeiti.

---

## 5. Pastabos

- Visada turi būti 10 laivų prieš spaudžiant `Ready`.
- Pasirinkus naują žaidimą po `Game Over`, tavo klientas atsijungia nuo seno žaidimo ir gali žaisti naują round.
- Serverio duomenų bazė (`battleship.db`) laikoma lokalioje projekto direktorijoje.
