# Checklista testowa — Match State Reconnect Sync

Testy manualne w edytorze z **ParrelSync** (dwa klony = dwóch graczy).
Zaznaczaj `[x]` po przejściu kroku.

---

## 0. Konfiguracja jednorazowa (PRZED testami)

- [x] **Utwórz `TurretRegistry`**: PPM w `Assets` → Create → *Tower Defense/Reconnect/Turret Registry*.
- [x] Zapisz go w folderze **`Resources`** pod nazwą **`TurretRegistry`** (ścieżka: `Resources/TurretRegistry.asset`).
- [x] W inspektorze assetu → menu kontekstowe (⋮) → **„Auto-Populate From Project"**. Sprawdź w logu: `Auto-populated N TurretData assets` (N > 0).
- [ ] Sprawdź SO sabotaży **`Sabotage_StealGold`** i **`Sabotage_BankRun`** → `durationType = Instant`.
- [ ] Potwierdź zgodność nazw scen: `NetworkManager.gameSceneName` == `ReconnectPromptController.gameSceneName` == nazwa sceny gry (domyślnie `GameScene`).
- [ ] Karty są w `Resources/Cards`, sabotaże w `Resources/Cards/Sabotages`, decki w `Resources/Decks` (po nazwie SO).
- [ ] ParrelSync: utworzony klon projektu i otwarty obok oryginału.

---

## 1. Smoke test — czy nic nie zepsute (bez reconnectu)

- [ ] Oryginał + klon: zaloguj obu (różne konta PlayFab), wejdź w matchmaking, rozpocznij mecz.
- [ ] Faza decku/draftu startowego przechodzi normalnie u obu.
- [ ] Fale spawnują się, wieże można stawiać, złoto rośnie/maleje.
- [ ] Po skończeniu fali pojawia się „WAITING FOR OTHER PLAYER…" do czasu aż drugi skończy (bariera `wave_index`).
- [ ] Draft mid-game i sabotaż w odpowiednich falach działają.
- [ ] Mecz kończy się normalnie (śmierć jednego gracza → Victory/Defeat).
- [ ] W logu po zakończeniu: snapshot wyczyszczony (`PendingMatchState` + `MatchSnapshot Clear`).

> Jeśli ten blok nie przechodzi — zatrzymaj się, to regresja w istniejącym flow, nie w reconnektcie.

---

## 2. Zapis snapshotu (save pointy)

- [ ] W trakcie meczu, na **starcie każdej fali** w logu: `[MatchSnapshot] Saved (wave=…, gold=…, turrets=…, cards=…)`.
- [ ] Po **wyborze karty** w drafcie mid-game: kolejny `[MatchSnapshot] Saved`.
- [ ] Po **wyborze sabotażu**: kolejny `[MatchSnapshot] Saved`.
- [ ] Wartości w logu (gold, liczba wież) zgadzają się ze stanem na ekranie.

---

## 3. Reconnect podstawowy — rozłączenie w trakcie WALKI

Scenariusz: gracz A w fali np. 5, w połowie walki.

- [ ] Dojdź jako gracz A do ~fali 5, postaw kilka wież, zapamiętaj złoto i HP.
- [ ] Zasymuluj rozłączenie A (zamknij klon **albo** wyłącz sieć / `PhotonNetwork.Disconnect`).
- [ ] Gracz B: kończy swoją bieżącą falę i zatrzymuje się na „WAITING FOR OTHER PLAYER…" (nie idzie dalej).
- [ ] Gracz A: uruchom ponownie / wróć do menu → pojawia się popup **Reconnect** w oknie czasowym.
- [ ] Kliknij **Reconnect** → ładuje `GameScene`.
- [ ] W logu A: `Reconnect snapshot accepted (wave=5)` → `[MatchRestore] Begin restore` → `Restore complete`.
- [ ] **Wieże** wróciły na te same pozycje (sprawdź w `TurretRegistry` że typy się zgadzają).
- [ ] **Złoto** i **HP** zgodne z wartością sprzed rozłączenia (z początku fali 5).
- [ ] Gra wznawia się **od początku fali 5** (nie od 0).
- [ ] Po ukończeniu fali 5 przez A → bariera u B puszcza, obaj idą do fali 6.

---

## 4. Reconnect w oknie WYBORU KARTY

- [ ] Doprowadź A do fali z draftem mid-game; otwórz wybór karty.
- [ ] **Wybierz kartę** (czekaj na log `Saved` po wyborze), potem rozłącz A zanim fala wystartuje.
- [ ] Reconnect A.
- [ ] Po powrocie **nie ma** ponownego okna wyboru tej samej karty.
- [ ] Wybrana karta jest aktywna (sprawdź modyfikatory/efekt).
- [ ] Drugi gracz nie utknął.

---

## 5. Reconnect w oknie WYBORU SABOTAŻU (kluczowy)

- [ ] Doprowadź do fali z sabotażem; **wybierz sabotaż** (log `Saved`).
- [ ] Rozłącz A zaraz po wyborze.
- [ ] Reconnect A.
- [ ] Sabotaż **nie jest oferowany ponownie** ani wysyłany drugi raz do przeciwnika.
- [ ] Gracz B dostał sabotaż **dokładnie raz** (sprawdź jego efekt/log — brak podwojenia).
- [ ] Brak desync: obie areny idą dalej zgodnie.

---

## 6. Idempotencja sabotaży (re-aplikacja)

- [ ] **AllIn**: zagraj self-sabotaż All-In (złoto spada o 50%). Rozłącz w trakcie wyzwania, reconnect.
  - [ ] Złoto po powrocie = wartość PO ofierze (NIE ściągnięte drugi raz).
  - [ ] Log: `skipping Apply on restore (one-time effect already in snapshot)`.
  - [ ] Po przetrwaniu fali nagroda za wyzwanie nadal się nalicza.
- [ ] **GlassCannon** (jeśli dostępny): HP=1 utrzymane po reconnektcie, mnożnik dmg działa, brak dobicia.
- [ ] **Tax/Skim** (sabotaż od przeciwnika, czasowy): po reconnektcie drenaż kontynuuje przez *pozostały* czas, nie od początku.
- [ ] **WaveBoss/Elite/Titan**: boss/elita pojawia się w re-rozegranej fali (re-kolejkowane), nie znika.

---

## 7. Anti-tamper (Warstwa 1 + 2)

- [ ] Podczas meczu rozłącz A. Znajdź klucz PlayerPrefs `ed_match_snapshot::{playFabId}`
      (Windows: `HKCU\Software\<Company>\<Product>` w regedit, lub przez kod debug).
- [ ] **Zepsuj wartość** (zmień kilka znaków blobu).
- [ ] Reconnect A → oczekiwane: log `Decrypt/verify failed` LUB `Hash mismatch` → **forfeit** (A przegrywa, B wygrywa).
- [ ] Przywróć/wyczyść klucz po teście.

---

## 8. Scenariusze brzegowe

- [ ] **Reboot PC w oknie TTL**: rozłącz A, policz < `PlayerTtl` (90s), wróć → reconnect działa, stan odtworzony.
- [ ] **Po przekroczeniu TTL**: rozłącz A, odczekaj > 90s → pokój zamyka slot, B dostaje zwycięstwo (forfeit), A nie może wrócić. Popup po stronie A → auto-forfeit.
- [ ] **Rozłączenie PRZED pierwszym save pointem** (np. w fazie decku): reconnect → brak snapshotu → normalny flow (fallback), bez błędów.
- [ ] **Rozłączenie tuż przed śmiercią**: po reconnektcie HP zgodne; jeśli było 1 HP, gra wznawia się z 1 HP.
- [ ] **Timeout bariery**: jeśli przeciwnik nie wróci, czekający po `BarrierTimeoutSeconds` (120s) nie wisi w nieskończoność (i tak `MatchOpponentWatcher` powinien wcześniej przyznać zwycięstwo).

---

## 9. Sanity logów (czego szukać)

| Log | Znaczenie |
|---|---|
| `[MatchSnapshot] Saved (...)` | zadziałał save point |
| `[MatchSnapshot] Published wave_index=N` / `Restore published wave_index=` | bariera licznikowa |
| `Reconnect snapshot accepted (wave=N)` | wykryto i zaakceptowano snapshot |
| `[MatchRestore] Begin restore` / `Restore complete` | przebieg odtwarzania |
| `[WaveManager] StartWaves suppressed — restore pending` | guard przeciw startowi od 0 zadziałał |
| `Hash mismatch` / `Decrypt/verify failed` | wykryto manipulację → forfeit |
| `[TurretRegistry] Unknown TurretData '…'` | brak wpisu w rejestrze (uzupełnij Auto-Populate) |

---

## 10. Znane ograniczenia (NIE są bugami)

- Wrogowie z połowy przerwanej fali nie wracają — walka od początku fali (zaprojektowane).
- Reconnect tylko na tej samej maszynie (snapshot w PlayerPrefs).
- Reconnect niemożliwy po wygaśnięciu `PlayerTtl`.
- Anti-tamper nie jest kryptograficznie nieprzełamywalny (klucz w binarce) — blokuje casualowe edycje.
