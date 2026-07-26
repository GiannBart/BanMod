<div align="center">

<img src="docs/images/banmod-banner.jpg" alt="Banner BanMod" width="100%">

# BanMod

**Moderazione delle lobby, strumenti anti-abuso, controlli host, ruoli personalizzati e modalità configurabili per Among Us.**

[![Licenza codice: GPL-3.0](https://img.shields.io/badge/codice-GPL--3.0-blue.svg)](LICENSE)
[![Piattaforma: Windows](https://img.shields.io/badge/piattaforma-Windows-0078D4.svg)](#requisiti)
[![Sito ufficiale](https://img.shields.io/badge/sito-banmod.online-7A5CFA.svg)](https://banmod.online)

[English](README.md) · **Italiano**

[Sito](https://banmod.online) · [Download](https://banmod.online/downloads) · [Istruzioni](https://banmod.online/instructions) · [Note di rilascio](https://banmod.online/releases) · [Segnala un problema](https://github.com/GiannBart/BanMod/issues)

</div>

> [!IMPORTANT]
> BanMod è una modifica non ufficiale realizzata dalla community. Usala soltanto in lobby corrette, consensuali o chiaramente moddate. Gli strumenti host e di test non devono essere usati per ingannare gli altri, disturbare partite pubbliche o ottenere vantaggi sleali.

## Descrizione

BanMod è una mod Windows per **Among Us** basata su **BepInEx IL2CPP**. Il nucleo distribuito sotto GPL è dedicato a moderazione persistente, protezioni anti-abuso, amministrazione host, gameplay configurabile, ruoli personalizzati e interfacce di supporto.

Il repository pubblico contiene soltanto il **nucleo della mod**. Alcune funzioni aggiuntive—storicamente chiamate “Premium” anche se attualmente gratuite—sono componenti opzionali forniti separatamente dai server ufficiali BanMod dopo controlli di idoneità, compatibilità e sicurezza. Non sono necessarie per il funzionamento del nucleo e non sono incluse in questo repository.

## Funzioni principali

- **Moderazione lobby:** strumenti di ban e blocco persistenti, liste cheater/sospetti, controlli su nomi e parole vietate, protezione dallo spam, gestione AFK e menu di amministrazione dei giocatori.
- **Controlli host:** autostart, regole per meeting e votazioni, task e sabotaggi, porte, mappe, messaggi lobby, riepiloghi e azioni configurabili sui giocatori.
- **Ruoli e modalità:** ruoli personalizzati o modificati, configurazione dei ruoli, miglioramenti Hide and Seek, modalità di test e preset aggiuntivi.
- **Strumenti client e grafici:** tasti configurabili, zoom negli stati consentiti, decorazioni lobby, tema scuro, interfacce personalizzate, menu outfit/skin e opzioni locali.
- **Strumenti di test per lobby private:** alcune funzioni host/debug sono destinate esclusivamente a test controllati o partite in cui tutti i partecipanti conoscono e accettano le regole.
- **Servizi collegati:** verifica online opzionale, report, messaggi server, servizi lobby, sistemi anti-abuso e funzioni opzionali distribuite separatamente.

Le funzioni possono cambiare tra una release e l'altra. Il sito ufficiale e le note di rilascio sono il riferimento per la build attualmente supportata.

## Immagini

<p align="center">
  <img src="docs/images/main-menu.png" alt="Menu principale BanMod" width="48%">
  <img src="docs/images/options-menu.png" alt="Menu opzioni BanMod" width="48%">
</p>

<p align="center">
  <img src="docs/images/game-settings.png" alt="Impostazioni di gioco BanMod" width="82%">
</p>

> Branding BanMod, artwork, screenshot, skin e risorse grafiche non sono sotto GPL, salvo indicazione esplicita nel singolo file. Consulta [Panoramica delle licenze](LICENSES.md).

## Requisiti

- Una copia legittima di **Among Us** per PC Windows.
- La versione del gioco supportata dalla release BanMod corrente.
- Il pacchetto corretto per **Steam** o **Epic Games**.
- I permessi per estrarre file nella cartella che contiene `Among Us.exe`.

Gli aggiornamenti del gioco possono interrompere la compatibilità delle mod. Controlla sempre la [pagina di download ufficiale](https://banmod.online/downloads) prima di installare o segnalare un problema.

## Installazione

1. Scarica il pacchetto corrente dalla [pagina download ufficiale](https://banmod.online/downloads).
2. Seleziona la versione Steam oppure Epic Games.
3. Apri la cartella principale del gioco, cioè quella che contiene `Among Us.exe`.
4. Estrai tutti i file dello ZIP BanMod in quella cartella.
5. Verifica che `Among Us.exe` e la cartella `BepInEx` si trovino allo stesso livello.
6. Avvia Among Us. Dopo il caricamento di BepInEx, BanMod dovrebbe apparire nel menu principale.

### Trovare la cartella del gioco

**Steam:** Libreria → click destro su **Among Us** → **Gestisci** → **Sfoglia i file locali**.

**Epic Games:** Libreria → menu con i tre puntini accanto ad **Among Us** → **Gestisci** → icona della cartella.

<p align="center">
  <img src="docs/images/install-folder.png" alt="Esempio cartella di installazione Among Us" width="360">
</p>

### Aggiornamento

Esegui un backup della cartella `BAN_DATA` quando indicato nelle note di rilascio. Rimuovi DLL BanMod obsolete o duplicate da `BepInEx/plugins`, poi installa il nuovo pacchetto ufficiale. Non mischiare file appartenenti a release diverse.

### Disinstallazione

Usa la verifica dei file della piattaforma:

- **Steam:** Proprietà → File installati → Verifica integrità dei file di gioco.
- **Epic Games:** Gestisci → Verifica.

Prima della verifica salva eventuali preset o configurazioni personali di BanMod che desideri conservare.

## Comandi predefiniti

- `Delete`: apre il menu principale di BanMod.
- `F10`: apre il menu di configurazione dei tasti rapidi.

I tasti e i menu disponibili possono cambiare in base alla release o ai permessi host. Consulta l'aiuto nel gioco e le [istruzioni ufficiali](https://banmod.online/instructions).

## Servizi ufficiali e funzioni opzionali

Il nucleo GPL e i servizi online gestiti da BanMod sono livelli separati:

- Il codice sorgente del nucleo può essere studiato, modificato e redistribuito secondo la GPLv3.
- API, verifica, token, report, lobby, messaggi server e altri servizi ufficiali sono gestiti dal progetto BanMod e soggetti alle rispettive regole di servizio e privacy.
- Le funzioni opzionali remote **non sono presenti nel repository**. Possono essere scaricate durante l'esecuzione soltanto dopo che il server ufficiale ha verificato il rispetto dei requisiti.
- Le funzioni opzionali remote sono componenti proprietari distribuiti con licenza separata. Questo repository non concede il permesso di copiarli, ripubblicarli, redistribuirli, sublicenziarli, estrarli in un altro progetto o inserirli in un fork.
- Le funzioni opzionali sono attualmente fornite senza pagamento, ma la disponibilità non è garantita e l'accesso può essere modificato, sospeso o revocato.
- Un client modificato o compilato autonomamente non è una release ufficiale BanMod e, di norma, non è idoneo alle funzioni opzionali ufficiali.

Le regole dei servizi non eliminano i diritti concessi dalla GPLv3 sul codice coperto dalla GPL. Allo stesso modo, la GPLv3 non concede il diritto di usare server privati, credenziali, branding o componenti con licenza separata.

## Regole di correttezza e dei servizi

Durante l'uso di una build ufficiale o dei servizi BanMod:

1. Non usare cheat menu, client malevoli, exploit, unlocker, manipolazione delle richieste, abuso delle API, spam, report falsi, strumenti di bypass o sistemi destinati a danneggiare il gioco, il progetto o gli altri utenti.
2. Le altre mod legittime non sono automaticamente vietate, ma controlli di compatibilità o sicurezza possono limitare le funzioni collegate. Contatta l'amministratore prima di test di sviluppo o configurazioni multi-mod particolari.
3. Non inviare richieste automatiche, malformate, eccessive o non autorizzate agli endpoint ufficiali BanMod.
4. Non riutilizzare token, credenziali, identificatori di build o dettagli privati degli endpoint ufficiali nei fork.
5. Possono essere applicate limitazioni tecniche a client, token, mod ID, friend code, player ID, account, indirizzi IP o altri identificatori collegati ad abusi o incompatibilità.
6. Le segnalazioni degli utenti sono elementi da verificare e non devono essere considerate prove senza controlli ragionevoli.
7. Rispetta i Termini di utilizzo e la Mod Policy di Among Us, le regole della community, la legge applicabile e il consenso degli altri giocatori.

Prima di attivare funzioni collegate, leggi le **Regole importanti** e la **Privacy Policy** aggiornate nella [pagina delle policy ufficiali](https://banmod.online/policies).

## Fork e build modificate

È permesso creare fork e modificare il codice coperto dalla GPL. Un fork pubblico conforme dovrebbe:

- mantenere GPLv3, copyright, attribuzioni e avvisi di assenza di garanzia;
- indicare chiaramente che il progetto è stato modificato e riportare la data delle modifiche;
- fornire il sorgente corrispondente completo per ogni binario distribuito;
- distribuire sotto GPLv3 il codice derivato coperto dalla GPL senza aggiungere restrizioni ulteriori;
- identificarsi come non ufficiale e non suggerire approvazione da parte di GianniBart, BanMod, Among Us o Innersloth;
- rimuovere o sostituire loghi BanMod, skin personalizzate, artwork, screenshot e altre risorse proprietarie, salvo autorizzazione scritta;
- disattivare o sostituire le integrazioni con le API BanMod ufficiali, senza usare infrastruttura ufficiale senza autorizzazione;
- non includere mai moduli server privati, componenti remoti opzionali, chiavi, token, dati personali, report o file del gioco;
- fornire assistenza al fork in modo indipendente, senza indirizzare i problemi specifici del fork ai canali ufficiali BanMod.

Un fork può restare pienamente conforme alla GPL usando un proprio backend oppure nessun backend. L'idoneità ai servizi ufficiali è una decisione operativa separata e non limita il diritto di modificare il codice.

## Compilazione dal sorgente

Il progetto usa **.NET 6** e pacchetti BepInEx IL2CPP.

```bash
git clone https://github.com/GiannBart/BanMod.git
cd BanMod
dotnet restore
dotnet build -c Release
```

Prima della compilazione controlla `BanMod.csproj`:

- sostituisci o rimuovi i percorsi Windows assoluti specifici dello sviluppatore;
- aggiorna i percorsi dell'assembly IL2CPP e dei metadata usando la tua installazione legittima;
- modifica o elimina il target di copia post-build locale;
- non pubblicare `BanMod.BuildCode.txt`, sorgenti di segreti generati, credenziali API o configurazioni locali;
- non pubblicare o redistribuire binari di Among Us, `Among Us_Data`, `GameAssembly.dll` o altri file del gioco.

La DLL viene normalmente prodotta in `bin/Release/net6.0/`. Le build compilate autonomamente sono non ufficiali e potrebbero non collegarsi ai servizi BanMod ufficiali.

## Release e modifiche

- Le principali modifiche pubbliche sono riepilogate in [CHANGELOG.md](CHANGELOG.md).
- Le note ufficiali e gli avvisi di compatibilità sono pubblicati su [banmod.online/releases](https://banmod.online/releases).
- I numeri di versione nel repository o nel file di progetto possono indicare sviluppo in corso ed essere più recenti dell'ultima release pubblica. Soltanto un pacchetto pubblicato esplicitamente come ufficiale deve essere considerato una release.

## Contributi

Issue e pull request per il nucleo GPL sono benvenute quando rispettano legge, persone, licenze e obiettivi del progetto.

Inviando codice confermi di avere il diritto di contribuire e che il contributo può essere distribuito sotto GPLv3, salvo diverso accordo scritto. Non inviare moduli opzionali proprietari, risorse private BanMod, codice di gioco ottenuto senza autorizzazione, endpoint segreti, credenziali, dati personali o codice copiato senza licenza compatibile e attribuzione.

Per problemi di sicurezza usa [SECURITY.md](SECURITY.md), senza aprire una issue pubblica.

## Crediti

BanMod contiene lavoro originale e parti ispirate o derivate da progetti open source della community. Mantieni tutti gli avvisi presenti nei file sorgente e in `Resources/Credits and License.txt`.

Principali progetti accreditati:

- [Town of Host](https://github.com/tukasa0001/TownOfHost)
- [Town of Host Enhanced](https://github.com/EnhancedNetwork/TownofHost-Enhanced)
- [EndlessHostRoles](https://github.com/Gurge44/EndlessHostRoles)
- [AmongUsRevamped](https://github.com/ApeMV/AmongUsRevamped)
- [MalumMenu](https://github.com/scp222thj/MalumMenu)
- [TheOtherRoles](https://github.com/TheOtherRolesAU/TheOtherRoles) / TheOtherHats
- [BetterAmongUs](https://github.com/D1GQ/BetterAmongUs-Public)
- Componenti e contributori NLayer, sotto licenza MIT dove indicato

I crediti non implicano affiliazione o approvazione.

## Panoramica delle licenze

- **Codice sorgente del nucleo:** GNU General Public License v3.0, salvo file con un diverso avviso compatibile.
- **Codice di terze parti:** resta soggetto agli avvisi e agli obblighi delle licenze originali.
- **Componenti opzionali distribuiti dal server:** licenza proprietaria/privata; non inclusi nel repository e non coperti dalla concessione GPL del repository.
- **Skin personalizzate, loghi, branding, artwork, screenshot e media:** © 2026 GianniBart. Tutti i diritti riservati, salvo indicazione esplicita.
- **Proprietà intellettuale di Among Us:** appartiene a Innersloth LLC e/o ai suoi licenzianti.

Consulta [LICENSE](LICENSE) per la GPLv3 e [LICENSES.md](LICENSES.md) per il dettaglio dei componenti. Questo riepilogo è informativo e non sostituisce i testi di licenza.

## Non affiliazione e dichiarazione legale

BanMod è una modifica non ufficiale realizzata dai fan per Among Us. Non è affiliata, approvata, sponsorizzata o autorizzata in altro modo da Innersloth LLC. Among Us, nomi, loghi, personaggi e materiali collegati appartengono a Innersloth LLC o ai rispettivi titolari.

La mod deve mantenere il timbro identificativo in-game richiesto dalla [Among Us Mod Policy](https://www.innersloth.com/among-us-mod-policy/). Le release BanMod non devono contenere il gioco base Among Us né copie non autorizzate dei file del gioco.

Il software è fornito **così com'è**, senza garanzie. Nei limiti consentiti dalla legge, gli autori non sono responsabili per ban, limitazioni dell'account, perdita di dati, incompatibilità, crash, interruzioni del servizio o danni dovuti a uso improprio, configurazioni non supportate, build modificate o violazioni di regole di terze parti.

## Supporto

- Sito: [banmod.online](https://banmod.online)
- Email: `banmod.giannibart@gmail.com`
- Discord e contatti aggiornati: usa i link presenti nella [pagina contatti ufficiale](https://banmod.online/contacts)
- Bug nel nucleo GPL pubblico: [GitHub Issues](https://github.com/GiannBart/BanMod/issues)

In una segnalazione indica versione BanMod, versione Among Us, piattaforma, passaggi per riprodurre il problema e log ripuliti. Non pubblicare token, friend code, identificatori dei giocatori, email, messaggi privati o altri dati personali.
