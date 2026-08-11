using System;
using HarmonyLib;
using InnerNet;
using UnityEngine;

namespace BanMod
{
    public enum BanModServerMode
    {
        None,
        Modded25,
        Vanilla
    }

    public static class BanModServerSelection
    {
        public static BanModServerMode Mode = BanModServerMode.None;
        public static bool VanillaAcknowledged = false;

        public static bool IsModded25 =>
            Mode == BanModServerMode.Modded25;

        public static bool IsVanilla =>
            Mode == BanModServerMode.Vanilla;

        public static bool HasSelectedMode =>
            Mode != BanModServerMode.None;

        public static void Reset()
        {
            Mode = BanModServerMode.None;
            VanillaAcknowledged = false;
        }
    }

    public class BanModServerTexts
    {
        public string SelectTitle;
        public string SelectDescription;
        public string ModdedDescription;
        public string ModdedButton;
        public string VanillaButton;
        public string PrivateFooter;

        public string VanillaTitle;
        public string VanillaIntro;
        public string VanillaWarning;
        public string ConfirmButton;
        public string BackButton;

        public string ConfirmPopupTitle;
        public string ConfirmPopupText;

        public string PrivatePopupTitle;
        public string PrivatePopupText;
    }

    public static class BanModServerLocalization
    {
        public static BanModServerTexts Get()
        {
            string language = GetLanguageId();

            if (Has(language, "italian", "italiano"))
                return Italian();

            if (Has(language, "french", "français", "francais"))
                return French();

            if (Has(language, "german", "deutsch"))
                return German();

            if (Has(
                language,
                "latam",
                "spanishlatam",
                "spanish_la",
                "spanishla",
                "espanollatam",
                "españollatam"
            ))
                return SpanishLatam();

            if (Has(
                language,
                "spanish",
                "spanisheU",
                "spanish_eu",
                "espanol",
                "español"
            ))
                return Spanish();

            if (Has(
                language,
                "brazilian",
                "brazilianportuguese",
                "portuguesebrazil",
                "portuguesebr",
                "portuguêsbr",
                "portuguesbr"
            ))
                return BrazilianPortuguese();

            if (Has(
                language,
                "portuguese",
                "portugueseeu",
                "portuguese_eu",
                "português",
                "portugues"
            ))
                return Portuguese();

            if (Has(language, "dutch", "nederlands"))
                return Dutch();

            if (Has(language, "russian", "русский"))
                return Russian();

            if (Has(language, "japanese", "日本語"))
                return Japanese();

            if (Has(language, "korean", "한국어"))
                return Korean();

            if (Has(
                language,
                "schinese",
                "simplifiedchinese",
                "chinesesimplified",
                "chinese_cn",
                "简体中文"
            ))
                return SimplifiedChinese();

            if (Has(
                language,
                "tchinese",
                "traditionalchinese",
                "chinesetraditional",
                "chinese_tw",
                "繁體中文",
                "繁体中文"
            ))
                return TraditionalChinese();

            if (Has(
                language,
                "filipino",
                "bisaya",
                "cebuano"
            ))
                return Filipino();

            return English();
        }

        private static string GetLanguageId()
        {
            try
            {
                if (TranslationController.Instance != null &&
                    TranslationController.Instance.currentLanguage != null)
                {
                    string id =
                        TranslationController.Instance
                            .currentLanguage
                            .languageID
                            .ToString();

                    if (!string.IsNullOrEmpty(id))
                        return id.ToLowerInvariant();
                }
            }
            catch (Exception ex)
            {
                BMLogger.LogWarning(
                    "[BanMod Server] Could not detect Among Us language: " +
                    ex.Message
                );
            }

            return "english";
        }

        private static bool Has(
            string value,
            params string[] names
        )
        {
            if (string.IsNullOrEmpty(value))
                return false;

            for (int i = 0; i < names.Length; i++)
            {
                if (value.Equals(
                    names[i].ToLowerInvariant(),
                    StringComparison.OrdinalIgnoreCase
                ))
                    return true;
            }

            return false;
        }

        private static BanModServerTexts English()
        {
            return new BanModServerTexts
            {
                SelectTitle =
                    "BANMOD - SELECT LOBBY MODE",

                SelectDescription =
                    "Choose the networking mode to use before creating this lobby.",

                ModdedDescription =
                    "MODDED +25\n" +
                    "Uses the modded host networking flag.",

                ModdedButton =
                    "MODDED +25\nRECOMMENDED",

                VanillaButton =
                    "VANILLA",

                PrivateFooter =
                    "BanMod must be used responsibly. " +
                    "When BanMod is enabled, create only PRIVATE lobbies.",

                VanillaTitle =
                    "IMPORTANT - VANILLA MODE",

                VanillaIntro =
                    "You selected VANILLA networking while BanMod remains enabled.",

                VanillaWarning =
                    "BanMod must be used ONLY in PRIVATE lobbies.\n\n" +

                    "Do not use BanMod in public lobbies or to disturb normal public games.\n\n" +

                    "If you want to play or host normally without BanMod, disable the mod " +
                    "using the dedicated Disable Mod option in BanMod settings.\n\n" +

                    "If you continue with BanMod enabled, you must create a PRIVATE lobby " +
                    "and use the modification legitimately and responsibly.\n\n" +

                    "You are responsible for respecting the Among Us Terms of Use, " +
                    "Mod Policy, Code of Conduct, applicable community rules, " +
                    "and the consent of the other players.\n\n" +

                    "Do not use BanMod to cheat, grief, gain an unfair advantage, " +
                    "interfere with other players, bypass restrictions, or disrupt services.\n\n" +

                    "Use of modifications may still be subject to enforcement decisions " +
                    "made by Innersloth. BanMod cannot guarantee that an account will never " +
                    "receive warnings, restrictions, suspensions, bans, or other sanctions.\n\n" +

                    "By pressing the confirmation button below, you confirm that you have " +
                    "read and understood this notice and that you take responsibility for " +
                    "using BanMod legitimately.",

                ConfirmButton =
                    "I HAVE READ AND TAKEN NOTE",

                BackButton =
                    "BACK",

                ConfirmPopupTitle =
                    "BanMod",

                ConfirmPopupText =
                    "Vanilla networking selected.\n\n" +
                    "BanMod remains enabled.\n\n" +
                    "Create ONLY PRIVATE lobbies while using BanMod.\n\n" +
                    "To play normally without BanMod, disable the mod " +
                    "from the dedicated option in BanMod settings.",

                PrivatePopupTitle =
                    "BanMod - Private Lobby Required",

                PrivatePopupText =
                    "BanMod is enabled.\n\n" +
                    "Only PRIVATE lobbies are allowed while using BanMod.\n\n" +
                    "The request to make this lobby public has been blocked.\n\n" +
                    "If you want to host a normal public lobby, disable BanMod " +
                    "from the dedicated option in BanMod settings."
            };
        }

        private static BanModServerTexts Italian()
        {
            return new BanModServerTexts
            {
                SelectTitle =
                    "BANMOD - SELEZIONA MODALITÀ LOBBY",

                SelectDescription =
                    "Scegli la modalità di rete da utilizzare prima di creare questa lobby.",

                ModdedDescription =
                    "MODDED +25\n" +
                    "Utilizza il flag di rete per le mod host.",

                ModdedButton =
                    "MODDED +25\nCONSIGLIATO",

                VanillaButton =
                    "VANILLA",

                PrivateFooter =
                    "BanMod deve essere utilizzata responsabilmente. " +
                    "Quando BanMod è attiva, crea SOLO lobby PRIVATE.",

                VanillaTitle =
                    "IMPORTANTE - MODALITÀ VANILLA",

                VanillaIntro =
                    "Hai selezionato la rete VANILLA mentre BanMod rimane attiva.",

                VanillaWarning =
                    "BanMod deve essere utilizzata SOLO in lobby PRIVATE.\n\n" +

                    "Non utilizzare BanMod nelle lobby pubbliche e non disturbare le normali partite pubbliche.\n\n" +

                    "Se vuoi giocare o hostare normalmente senza BanMod, disattiva la mod " +
                    "utilizzando l'apposita opzione Disabilita Mod nelle impostazioni di BanMod.\n\n" +

                    "Se continui con BanMod attiva, devi creare una lobby PRIVATA " +
                    "e utilizzare la modifica in modo legittimo e responsabile.\n\n" +

                    "Sei responsabile del rispetto dei Termini di utilizzo di Among Us, " +
                    "della Mod Policy, del Codice di condotta, delle regole applicabili " +
                    "della community e del consenso degli altri giocatori.\n\n" +

                    "Non utilizzare BanMod per barare, disturbare le partite, ottenere " +
                    "vantaggi sleali, interferire con altri giocatori, aggirare restrizioni " +
                    "o disturbare i servizi.\n\n" +

                    "L'utilizzo di modifiche può comunque essere soggetto alle decisioni " +
                    "di applicazione delle regole da parte di Innersloth. BanMod non può " +
                    "garantire che un account non riceva avvertimenti, restrizioni, " +
                    "sospensioni, ban o altre sanzioni.\n\n" +

                    "Premendo il pulsante di conferma qui sotto dichiari di aver letto e " +
                    "compreso questo avviso e di assumerti la responsabilità di utilizzare " +
                    "BanMod in modo legittimo.",

                ConfirmButton =
                    "HO LETTO E PRESO NOTA",

                BackButton =
                    "INDIETRO",

                ConfirmPopupTitle =
                    "BanMod",

                ConfirmPopupText =
                    "Rete Vanilla selezionata.\n\n" +
                    "BanMod rimane attiva.\n\n" +
                    "Crea SOLO lobby PRIVATE mentre utilizzi BanMod.\n\n" +
                    "Per giocare normalmente senza BanMod, disattiva la mod " +
                    "dall'apposita opzione nelle impostazioni di BanMod.",

                PrivatePopupTitle =
                    "BanMod - Lobby privata obbligatoria",

                PrivatePopupText =
                    "BanMod è attiva.\n\n" +
                    "Durante l'utilizzo di BanMod sono consentite SOLO lobby PRIVATE.\n\n" +
                    "La richiesta di rendere pubblica questa lobby è stata bloccata.\n\n" +
                    "Se vuoi creare una normale lobby pubblica, disattiva BanMod " +
                    "dall'apposita opzione nelle impostazioni."
            };
        }

        private static BanModServerTexts French()
        {
            return new BanModServerTexts
            {
                SelectTitle =
                    "BANMOD - SÉLECTION DU MODE DU SALON",

                SelectDescription =
                    "Choisissez le mode réseau avant de créer ce salon.",

                ModdedDescription =
                    "MODDED +25\n" +
                    "Utilise le drapeau réseau pour les mods de l'hôte.",

                ModdedButton =
                    "MODDED +25\nRECOMMANDÉ",

                VanillaButton =
                    "VANILLA",

                PrivateFooter =
                    "BanMod doit être utilisé de manière responsable. " +
                    "Lorsque BanMod est actif, créez uniquement des salons PRIVÉS.",

                VanillaTitle =
                    "IMPORTANT - MODE VANILLA",

                VanillaIntro =
                    "Vous avez sélectionné le réseau VANILLA alors que BanMod reste actif.",

                VanillaWarning =
                    "BanMod doit être utilisé UNIQUEMENT dans des salons PRIVÉS.\n\n" +

                    "N'utilisez pas BanMod dans des salons publics et ne perturbez pas " +
                    "les parties publiques normales.\n\n" +

                    "Si vous souhaitez jouer normalement sans BanMod, désactivez le mod " +
                    "à l'aide de l'option dédiée dans les paramètres BanMod.\n\n" +

                    "Si BanMod reste actif, vous devez créer un salon PRIVÉ et utiliser " +
                    "la modification de manière légitime et responsable.\n\n" +

                    "Vous êtes responsable du respect des Conditions d'utilisation " +
                    "d'Among Us, de la politique concernant les mods, du Code de conduite, " +
                    "des règles communautaires applicables et du consentement des autres joueurs.\n\n" +

                    "N'utilisez pas BanMod pour tricher, perturber des parties, obtenir " +
                    "un avantage injuste, interférer avec d'autres joueurs, contourner " +
                    "des restrictions ou perturber des services.\n\n" +

                    "L'utilisation de modifications peut être soumise aux décisions " +
                    "d'Innersloth. BanMod ne peut pas garantir qu'un compte ne recevra " +
                    "jamais d'avertissements, restrictions, suspensions, bannissements " +
                    "ou autres sanctions.\n\n" +

                    "En confirmant, vous déclarez avoir lu et compris cet avertissement " +
                    "et accepter la responsabilité d'utiliser BanMod de manière légitime.",

                ConfirmButton =
                    "J'AI LU ET PRIS NOTE",

                BackButton =
                    "RETOUR",

                ConfirmPopupTitle =
                    "BanMod",

                ConfirmPopupText =
                    "Réseau Vanilla sélectionné.\n\n" +
                    "BanMod reste actif.\n\n" +
                    "Créez UNIQUEMENT des salons PRIVÉS avec BanMod.\n\n" +
                    "Pour jouer normalement sans BanMod, désactivez le mod " +
                    "dans les paramètres BanMod.",

                PrivatePopupTitle =
                    "BanMod - Salon privé requis",

                PrivatePopupText =
                    "BanMod est actif.\n\n" +
                    "Seuls les salons PRIVÉS sont autorisés avec BanMod.\n\n" +
                    "La demande de rendre ce salon public a été bloquée.\n\n" +
                    "Pour créer un salon public normal, désactivez BanMod."
            };
        }

        private static BanModServerTexts German()
        {
            return new BanModServerTexts
            {
                SelectTitle =
                    "BANMOD - LOBBY-MODUS AUSWÄHLEN",

                SelectDescription =
                    "Wähle den Netzwerkmodus aus, bevor du diese Lobby erstellst.",

                ModdedDescription =
                    "MODDED +25\n" +
                    "Verwendet das Netzwerk-Flag für Host-Mods.",

                ModdedButton =
                    "MODDED +25\nEMPFOHLEN",

                VanillaButton =
                    "VANILLA",

                PrivateFooter =
                    "BanMod muss verantwortungsvoll verwendet werden. " +
                    "Wenn BanMod aktiv ist, erstelle nur PRIVATE Lobbys.",

                VanillaTitle =
                    "WICHTIG - VANILLA-MODUS",

                VanillaIntro =
                    "Du hast VANILLA ausgewählt, während BanMod weiterhin aktiv ist.",

                VanillaWarning =
                    "BanMod darf NUR in PRIVATEN Lobbys verwendet werden.\n\n" +

                    "Verwende BanMod nicht in öffentlichen Lobbys und störe keine " +
                    "normalen öffentlichen Spiele.\n\n" +

                    "Wenn du normal ohne BanMod spielen möchtest, deaktiviere die Mod " +
                    "über die entsprechende Option in den BanMod-Einstellungen.\n\n" +

                    "Wenn BanMod aktiviert bleibt, musst du eine PRIVATE Lobby erstellen " +
                    "und die Mod legitim und verantwortungsvoll verwenden.\n\n" +

                    "Du bist für die Einhaltung der Among Us-Nutzungsbedingungen, " +
                    "Mod-Richtlinie, Verhaltensregeln, Community-Regeln und der Zustimmung " +
                    "anderer Spieler verantwortlich.\n\n" +

                    "Verwende BanMod nicht zum Cheaten, Griefen, Erlangen unfairer Vorteile, " +
                    "Stören anderer Spieler, Umgehen von Beschränkungen oder Stören von Diensten.\n\n" +

                    "Die Verwendung von Mods kann weiterhin Maßnahmen von Innersloth " +
                    "unterliegen. BanMod kann nicht garantieren, dass ein Konto niemals " +
                    "Warnungen, Einschränkungen, Sperren oder andere Sanktionen erhält.\n\n" +

                    "Mit der Bestätigung erklärst du, diesen Hinweis gelesen und verstanden " +
                    "zu haben und die Verantwortung für die legitime Verwendung von BanMod zu übernehmen.",

                ConfirmButton =
                    "GELESEN UND ZUR KENNTNIS GENOMMEN",

                BackButton =
                    "ZURÜCK",

                ConfirmPopupTitle =
                    "BanMod",

                ConfirmPopupText =
                    "Vanilla-Netzwerk ausgewählt.\n\n" +
                    "BanMod bleibt aktiviert.\n\n" +
                    "Erstelle mit BanMod NUR PRIVATE Lobbys.\n\n" +
                    "Für normales Spielen deaktiviere BanMod in den Einstellungen.",

                PrivatePopupTitle =
                    "BanMod - Private Lobby erforderlich",

                PrivatePopupText =
                    "BanMod ist aktiviert.\n\n" +
                    "Mit BanMod sind nur PRIVATE Lobbys erlaubt.\n\n" +
                    "Der Versuch, diese Lobby öffentlich zu machen, wurde blockiert.\n\n" +
                    "Für eine normale öffentliche Lobby deaktiviere BanMod."
            };
        }

        private static BanModServerTexts Spanish()
        {
            return new BanModServerTexts
            {
                SelectTitle =
                    "BANMOD - SELECCIONAR MODO DE SALA",

                SelectDescription =
                    "Elige el modo de red antes de crear esta sala.",

                ModdedDescription =
                    "MODDED +25\n" +
                    "Utiliza el indicador de red para mods del anfitrión.",

                ModdedButton =
                    "MODDED +25\nRECOMENDADO",

                VanillaButton =
                    "VANILLA",

                PrivateFooter =
                    "BanMod debe utilizarse de forma responsable. " +
                    "Con BanMod activo, crea únicamente salas PRIVADAS.",

                VanillaTitle =
                    "IMPORTANTE - MODO VANILLA",

                VanillaIntro =
                    "Has seleccionado VANILLA mientras BanMod sigue activado.",

                VanillaWarning =
                    "BanMod debe utilizarse ÚNICAMENTE en salas PRIVADAS.\n\n" +

                    "No utilices BanMod en salas públicas ni para molestar partidas públicas normales.\n\n" +

                    "Si quieres jugar normalmente sin BanMod, desactiva el mod utilizando " +
                    "la opción correspondiente en los ajustes de BanMod.\n\n" +

                    "Si continúas con BanMod activo, debes crear una sala PRIVADA y utilizar " +
                    "la modificación de forma legítima y responsable.\n\n" +

                    "Eres responsable de respetar los Términos de uso de Among Us, " +
                    "la Política de mods, el Código de conducta, las reglas de la comunidad " +
                    "y el consentimiento de los demás jugadores.\n\n" +

                    "No utilices BanMod para hacer trampas, molestar, obtener ventajas injustas, " +
                    "interferir con otros jugadores, eludir restricciones o perturbar servicios.\n\n" +

                    "El uso de modificaciones puede estar sujeto a medidas de Innersloth. " +
                    "BanMod no puede garantizar que una cuenta nunca reciba advertencias, " +
                    "restricciones, suspensiones, bloqueos u otras sanciones.\n\n" +

                    "Al confirmar declaras que has leído y comprendido este aviso y que " +
                    "asumes la responsabilidad de utilizar BanMod legítimamente.",

                ConfirmButton =
                    "HE LEÍDO Y TOMADO NOTA",

                BackButton =
                    "ATRÁS",

                ConfirmPopupTitle =
                    "BanMod",

                ConfirmPopupText =
                    "Red Vanilla seleccionada.\n\n" +
                    "BanMod continúa activo.\n\n" +
                    "Crea SOLO salas PRIVADAS mientras utilizas BanMod.\n\n" +
                    "Para jugar normalmente, desactiva BanMod en los ajustes.",

                PrivatePopupTitle =
                    "BanMod - Sala privada obligatoria",

                PrivatePopupText =
                    "BanMod está activo.\n\n" +
                    "Solo se permiten salas PRIVADAS con BanMod.\n\n" +
                    "Se ha bloqueado la solicitud de hacer pública esta sala.\n\n" +
                    "Para crear una sala pública normal, desactiva BanMod."
            };
        }

        private static BanModServerTexts SpanishLatam()
        {
            return new BanModServerTexts
            {
                SelectTitle =
                    "BANMOD - SELECCIONAR MODO DE SALA",

                SelectDescription =
                    "Elige el modo de red antes de crear esta sala.",

                ModdedDescription =
                    "MODDED +25\n" +
                    "Usa el indicador de red para mods del anfitrión.",

                ModdedButton =
                    "MODDED +25\nRECOMENDADO",

                VanillaButton =
                    "VANILLA",

                PrivateFooter =
                    "BanMod debe usarse responsablemente. " +
                    "Con BanMod activo, crea solamente salas PRIVADAS.",

                VanillaTitle =
                    "IMPORTANTE - MODO VANILLA",

                VanillaIntro =
                    "Seleccionaste VANILLA mientras BanMod sigue activado.",

                VanillaWarning =
                    "BanMod debe usarse ÚNICAMENTE en salas PRIVADAS.\n\n" +

                    "No uses BanMod en salas públicas ni para molestar partidas públicas normales.\n\n" +

                    "Si quieres jugar normalmente sin BanMod, desactiva el mod usando " +
                    "la opción correspondiente en la configuración de BanMod.\n\n" +

                    "Si continúas con BanMod activo, debes crear una sala PRIVADA y utilizar " +
                    "la modificación de forma legítima y responsable.\n\n" +

                    "Eres responsable de respetar los Términos de uso de Among Us, " +
                    "la Política de mods, el Código de conducta, las reglas de la comunidad " +
                    "y el consentimiento de los demás jugadores.\n\n" +

                    "No uses BanMod para hacer trampa, molestar, obtener ventajas injustas, " +
                    "interferir con otros jugadores, evadir restricciones o interrumpir servicios.\n\n" +

                    "El uso de modificaciones puede estar sujeto a decisiones de Innersloth. " +
                    "BanMod no puede garantizar que una cuenta nunca reciba advertencias, " +
                    "restricciones, suspensiones, bloqueos u otras sanciones.\n\n" +

                    "Al confirmar declaras que leíste y comprendiste este aviso y que " +
                    "asumes la responsabilidad de usar BanMod legítimamente.",

                ConfirmButton =
                    "HE LEÍDO Y TOMADO NOTA",

                BackButton =
                    "VOLVER",

                ConfirmPopupTitle =
                    "BanMod",

                ConfirmPopupText =
                    "Red Vanilla seleccionada.\n\n" +
                    "BanMod continúa activado.\n\n" +
                    "Crea SOLO salas PRIVADAS mientras utilizas BanMod.\n\n" +
                    "Para jugar normalmente, desactiva BanMod en la configuración.",

                PrivatePopupTitle =
                    "BanMod - Se requiere sala privada",

                PrivatePopupText =
                    "BanMod está activado.\n\n" +
                    "Solo se permiten salas PRIVADAS con BanMod.\n\n" +
                    "Se bloqueó la solicitud para hacer pública esta sala.\n\n" +
                    "Para crear una sala pública normal, desactiva BanMod."
            };
        }

        private static BanModServerTexts BrazilianPortuguese()
        {
            return new BanModServerTexts
            {
                SelectTitle =
                    "BANMOD - SELECIONAR MODO DA SALA",

                SelectDescription =
                    "Escolha o modo de rede antes de criar esta sala.",

                ModdedDescription =
                    "MODDED +25\n" +
                    "Usa o sinalizador de rede para mods do host.",

                ModdedButton =
                    "MODDED +25\nRECOMENDADO",

                VanillaButton =
                    "VANILLA",

                PrivateFooter =
                    "BanMod deve ser usado com responsabilidade. " +
                    "Com BanMod ativo, crie somente salas PRIVADAS.",

                VanillaTitle =
                    "IMPORTANTE - MODO VANILLA",

                VanillaIntro =
                    "Você selecionou VANILLA enquanto BanMod continua ativo.",

                VanillaWarning =
                    "BanMod deve ser usado SOMENTE em salas PRIVADAS.\n\n" +

                    "Não use BanMod em salas públicas nem para atrapalhar partidas públicas normais.\n\n" +

                    "Se quiser jogar normalmente sem BanMod, desative o mod usando " +
                    "a opção dedicada nas configurações do BanMod.\n\n" +

                    "Se continuar com BanMod ativo, você deve criar uma sala PRIVADA " +
                    "e usar a modificação de forma legítima e responsável.\n\n" +

                    "Você é responsável por respeitar os Termos de Uso de Among Us, " +
                    "a Política de Mods, o Código de Conduta, as regras da comunidade " +
                    "e o consentimento dos outros jogadores.\n\n" +

                    "Não use BanMod para trapacear, perturbar partidas, obter vantagem injusta, " +
                    "interferir com jogadores, contornar restrições ou interromper serviços.\n\n" +

                    "O uso de modificações pode estar sujeito às decisões da Innersloth. " +
                    "BanMod não pode garantir que uma conta nunca receberá avisos, restrições, " +
                    "suspensões, banimentos ou outras sanções.\n\n" +

                    "Ao confirmar, você declara que leu e compreendeu este aviso e assume " +
                    "a responsabilidade pelo uso legítimo do BanMod.",

                ConfirmButton =
                    "LI E ESTOU CIENTE",

                BackButton =
                    "VOLTAR",

                ConfirmPopupTitle =
                    "BanMod",

                ConfirmPopupText =
                    "Rede Vanilla selecionada.\n\n" +
                    "BanMod continua ativo.\n\n" +
                    "Crie SOMENTE salas PRIVADAS enquanto estiver usando BanMod.\n\n" +
                    "Para jogar normalmente, desative BanMod nas configurações.",

                PrivatePopupTitle =
                    "BanMod - Sala privada obrigatória",

                PrivatePopupText =
                    "BanMod está ativo.\n\n" +
                    "Somente salas PRIVADAS são permitidas com BanMod.\n\n" +
                    "A tentativa de tornar esta sala pública foi bloqueada.\n\n" +
                    "Para criar uma sala pública normal, desative BanMod."
            };
        }

        private static BanModServerTexts Portuguese()
        {
            return new BanModServerTexts
            {
                SelectTitle =
                    "BANMOD - SELECIONAR MODO DA SALA",

                SelectDescription =
                    "Escolhe o modo de rede antes de criares esta sala.",

                ModdedDescription =
                    "MODDED +25\n" +
                    "Utiliza o indicador de rede para mods do anfitrião.",

                ModdedButton =
                    "MODDED +25\nRECOMENDADO",

                VanillaButton =
                    "VANILLA",

                PrivateFooter =
                    "BanMod deve ser utilizado de forma responsável. " +
                    "Com BanMod ativo, cria apenas salas PRIVADAS.",

                VanillaTitle =
                    "IMPORTANTE - MODO VANILLA",

                VanillaIntro =
                    "Selecionaste VANILLA enquanto BanMod permanece ativo.",

                VanillaWarning =
                    "BanMod deve ser utilizado APENAS em salas PRIVADAS.\n\n" +

                    "Não utilizes BanMod em salas públicas nem para perturbar partidas públicas normais.\n\n" +

                    "Se quiseres jogar normalmente sem BanMod, desativa o mod através " +
                    "da opção dedicada nas definições do BanMod.\n\n" +

                    "Se continuares com BanMod ativo, tens de criar uma sala PRIVADA " +
                    "e utilizar a modificação de forma legítima e responsável.\n\n" +

                    "És responsável por respeitar os Termos de Utilização de Among Us, " +
                    "a Política de Mods, o Código de Conduta, as regras da comunidade " +
                    "e o consentimento dos outros jogadores.\n\n" +

                    "Não utilizes BanMod para fazer batota, perturbar partidas, obter vantagens " +
                    "injustas, interferir com jogadores, contornar restrições ou perturbar serviços.\n\n" +

                    "A utilização de modificações pode estar sujeita às decisões da Innersloth. " +
                    "BanMod não pode garantir que uma conta nunca receba avisos, restrições, " +
                    "suspensões, banimentos ou outras sanções.\n\n" +

                    "Ao confirmar, declaras que leste e compreendeste este aviso e assumes " +
                    "a responsabilidade pela utilização legítima do BanMod.",

                ConfirmButton =
                    "LI E TOMEI CONHECIMENTO",

                BackButton =
                    "VOLTAR",

                ConfirmPopupTitle =
                    "BanMod",

                ConfirmPopupText =
                    "Rede Vanilla selecionada.\n\n" +
                    "BanMod permanece ativo.\n\n" +
                    "Cria APENAS salas PRIVADAS enquanto utilizares BanMod.\n\n" +
                    "Para jogar normalmente, desativa BanMod nas definições.",

                PrivatePopupTitle =
                    "BanMod - Sala privada obrigatória",

                PrivatePopupText =
                    "BanMod está ativo.\n\n" +
                    "Apenas são permitidas salas PRIVADAS com BanMod.\n\n" +
                    "O pedido para tornar esta sala pública foi bloqueado.\n\n" +
                    "Para criar uma sala pública normal, desativa BanMod."
            };
        }

        private static BanModServerTexts Dutch()
        {
            return new BanModServerTexts
            {
                SelectTitle =
                    "BANMOD - LOBBYMODUS SELECTEREN",

                SelectDescription =
                    "Kies de netwerkmodus voordat je deze lobby maakt.",

                ModdedDescription =
                    "MODDED +25\n" +
                    "Gebruikt de netwerkmarkering voor hostmods.",

                ModdedButton =
                    "MODDED +25\nAANBEVOLEN",

                VanillaButton =
                    "VANILLA",

                PrivateFooter =
                    "Gebruik BanMod verantwoordelijk. " +
                    "Maak alleen PRIVÉLOBBY'S wanneer BanMod actief is.",

                VanillaTitle =
                    "BELANGRIJK - VANILLA-MODUS",

                VanillaIntro =
                    "Je hebt VANILLA geselecteerd terwijl BanMod actief blijft.",

                VanillaWarning =
                    "BanMod mag ALLEEN in PRIVÉLOBBY'S worden gebruikt.\n\n" +

                    "Gebruik BanMod niet in openbare lobby's en verstoor geen normale openbare spellen.\n\n" +

                    "Wil je normaal zonder BanMod spelen, schakel de mod dan uit via " +
                    "de speciale optie in de BanMod-instellingen.\n\n" +

                    "Als BanMod actief blijft, moet je een PRIVÉLOBBY maken en de modificatie " +
                    "legitiem en verantwoordelijk gebruiken.\n\n" +

                    "Je bent verantwoordelijk voor het naleven van de Among Us-gebruiksvoorwaarden, " +
                    "het modbeleid, de gedragscode, communityregels en de toestemming van andere spelers.\n\n" +

                    "Gebruik BanMod niet om vals te spelen, te griefen, oneerlijke voordelen te verkrijgen, " +
                    "andere spelers te hinderen, beperkingen te omzeilen of diensten te verstoren.\n\n" +

                    "Het gebruik van modificaties kan onderworpen zijn aan maatregelen van Innersloth. " +
                    "BanMod kan niet garanderen dat een account nooit waarschuwingen, beperkingen, " +
                    "schorsingen, bans of andere sancties ontvangt.\n\n" +

                    "Door te bevestigen verklaar je dat je deze melding hebt gelezen en begrepen " +
                    "en verantwoordelijkheid neemt voor het legitieme gebruik van BanMod.",

                ConfirmButton =
                    "GELEZEN EN BEGREPEN",

                BackButton =
                    "TERUG",

                ConfirmPopupTitle =
                    "BanMod",

                ConfirmPopupText =
                    "Vanilla-netwerk geselecteerd.\n\n" +
                    "BanMod blijft actief.\n\n" +
                    "Maak ALLEEN PRIVÉLOBBY'S wanneer je BanMod gebruikt.\n\n" +
                    "Schakel BanMod uit om normaal te spelen.",

                PrivatePopupTitle =
                    "BanMod - Privélobby vereist",

                PrivatePopupText =
                    "BanMod is actief.\n\n" +
                    "Alleen PRIVÉLOBBY'S zijn toegestaan met BanMod.\n\n" +
                    "Het verzoek om deze lobby openbaar te maken is geblokkeerd.\n\n" +
                    "Schakel BanMod uit om een normale openbare lobby te maken."
            };
        }

        private static BanModServerTexts Russian()
        {
            return new BanModServerTexts
            {
                SelectTitle =
                    "BANMOD - ВЫБОР РЕЖИМА ЛОББИ",

                SelectDescription =
                    "Выберите сетевой режим перед созданием лобби.",

                ModdedDescription =
                    "MODDED +25\n" +
                    "Использует сетевой флаг модифицированного хоста.",

                ModdedButton =
                    "MODDED +25\nРЕКОМЕНДУЕТСЯ",

                VanillaButton =
                    "VANILLA",

                PrivateFooter =
                    "Используйте BanMod ответственно. " +
                    "При включённом BanMod создавайте только ПРИВАТНЫЕ лобби.",

                VanillaTitle =
                    "ВАЖНО - РЕЖИМ VANILLA",

                VanillaIntro =
                    "Вы выбрали VANILLA, при этом BanMod остаётся включённым.",

                VanillaWarning =
                    "BanMod разрешается использовать ТОЛЬКО в ПРИВАТНЫХ лобби.\n\n" +

                    "Не используйте BanMod в публичных лобби и не мешайте обычным публичным играм.\n\n" +

                    "Если вы хотите играть без BanMod, отключите мод специальной " +
                    "кнопкой в настройках BanMod.\n\n" +

                    "Если BanMod остаётся включённым, создайте ПРИВАТНОЕ лобби " +
                    "и используйте модификацию законно и ответственно.\n\n" +

                    "Вы несёте ответственность за соблюдение Условий использования Among Us, " +
                    "политики модификаций, Кодекса поведения, правил сообщества " +
                    "и согласия других игроков.\n\n" +

                    "Не используйте BanMod для читерства, гриферства, получения несправедливого " +
                    "преимущества, вмешательства в игру других пользователей, обхода ограничений " +
                    "или нарушения работы сервисов.\n\n" +

                    "Использование модификаций может привести к мерам со стороны Innersloth. " +
                    "BanMod не гарантирует отсутствие предупреждений, ограничений, блокировок " +
                    "или других санкций для аккаунта.\n\n" +

                    "Нажимая кнопку подтверждения, вы подтверждаете, что прочитали и поняли " +
                    "это уведомление и принимаете ответственность за законное использование BanMod.",

                ConfirmButton =
                    "Я ПРОЧИТАЛ И ПРИНЯЛ К СВЕДЕНИЮ",

                BackButton =
                    "НАЗАД",

                ConfirmPopupTitle =
                    "BanMod",

                ConfirmPopupText =
                    "Выбран режим Vanilla.\n\n" +
                    "BanMod остаётся включённым.\n\n" +
                    "Создавайте ТОЛЬКО ПРИВАТНЫЕ лобби с BanMod.\n\n" +
                    "Для обычной игры отключите BanMod.",

                PrivatePopupTitle =
                    "BanMod - Требуется приватное лобби",

                PrivatePopupText =
                    "BanMod включён.\n\n" +
                    "С BanMod разрешены только ПРИВАТНЫЕ лобби.\n\n" +
                    "Попытка сделать это лобби публичным была заблокирована.\n\n" +
                    "Чтобы создать обычное публичное лобби, отключите BanMod."
            };
        }

        private static BanModServerTexts Japanese()
        {
            return new BanModServerTexts
            {
                SelectTitle =
                    "BANMOD - ロビーモードを選択",

                SelectDescription =
                    "ロビーを作成する前にネットワークモードを選択してください。",

                ModdedDescription =
                    "MODDED +25\nホストMod用のネットワークフラグを使用します。",

                ModdedButton =
                    "MODDED +25\n推奨",

                VanillaButton =
                    "VANILLA",

                PrivateFooter =
                    "BanModは責任を持って使用してください。" +
                    "BanModが有効な場合はプライベートロビーのみ作成してください。",

                VanillaTitle =
                    "重要 - VANILLAモード",

                VanillaIntro =
                    "BanModを有効にしたままVANILLAを選択しました。",

                VanillaWarning =
                    "BanModはプライベートロビーでのみ使用してください。\n\n" +

                    "公開ロビーでBanModを使用したり、通常の公開ゲームを妨害したりしないでください。\n\n" +

                    "BanModなしで通常プレイする場合は、BanMod設定の専用オプションからModを無効にしてください。\n\n" +

                    "BanModを有効のまま続行する場合は、プライベートロビーを作成し、" +
                    "適切かつ責任を持ってModを使用してください。\n\n" +

                    "Among Usの利用規約、Modポリシー、行動規範、コミュニティルール、" +
                    "および他のプレイヤーの同意を守る責任があります。\n\n" +

                    "チート、荒らし、不公平な利益の取得、他のプレイヤーへの妨害、" +
                    "制限の回避、サービスへの妨害にBanModを使用しないでください。\n\n" +

                    "Modの使用はInnerslothによる措置の対象となる場合があります。" +
                    "BanModは警告、制限、停止、BANその他の制裁が発生しないことを保証できません。\n\n" +

                    "確認ボタンを押すことで、この通知を読み理解し、BanModを適切に使用する" +
                    "責任を負うことを確認します。",

                ConfirmButton =
                    "内容を読み、確認しました",

                BackButton =
                    "戻る",

                ConfirmPopupTitle =
                    "BanMod",

                ConfirmPopupText =
                    "Vanillaネットワークが選択されました。\n\n" +
                    "BanModは有効なままです。\n\n" +
                    "BanMod使用中はプライベートロビーのみ作成してください。\n\n" +
                    "通常プレイする場合はBanModを無効にしてください。",

                PrivatePopupTitle =
                    "BanMod - プライベートロビーが必要です",

                PrivatePopupText =
                    "BanModが有効です。\n\n" +
                    "BanMod使用中はプライベートロビーのみ許可されています。\n\n" +
                    "ロビーを公開する要求はブロックされました。\n\n" +
                    "通常の公開ロビーを作成する場合はBanModを無効にしてください。"
            };
        }

        private static BanModServerTexts Korean()
        {
            return new BanModServerTexts
            {
                SelectTitle =
                    "BANMOD - 로비 모드 선택",

                SelectDescription =
                    "로비를 만들기 전에 네트워크 모드를 선택하세요.",

                ModdedDescription =
                    "MODDED +25\n호스트 모드용 네트워크 플래그를 사용합니다.",

                ModdedButton =
                    "MODDED +25\n권장",

                VanillaButton =
                    "VANILLA",

                PrivateFooter =
                    "BanMod를 책임감 있게 사용하세요. " +
                    "BanMod가 활성화된 경우 비공개 로비만 만드세요.",

                VanillaTitle =
                    "중요 - VANILLA 모드",

                VanillaIntro =
                    "BanMod가 활성화된 상태에서 VANILLA를 선택했습니다.",

                VanillaWarning =
                    "BanMod는 비공개 로비에서만 사용해야 합니다.\n\n" +

                    "공개 로비에서 BanMod를 사용하거나 일반 공개 게임을 방해하지 마세요.\n\n" +

                    "BanMod 없이 정상적으로 플레이하려면 BanMod 설정의 전용 옵션에서 " +
                    "모드를 비활성화하세요.\n\n" +

                    "BanMod를 활성화한 채 계속하려면 비공개 로비를 만들고 " +
                    "합법적이고 책임감 있게 사용해야 합니다.\n\n" +

                    "Among Us 이용 약관, 모드 정책, 행동 강령, 커뮤니티 규칙 및 " +
                    "다른 플레이어의 동의를 준수할 책임이 있습니다.\n\n" +

                    "치팅, 고의적인 방해, 부당한 이점 획득, 다른 플레이어 방해, " +
                    "제한 우회 또는 서비스 방해에 BanMod를 사용하지 마세요.\n\n" +

                    "모드 사용은 Innersloth의 제재 결정 대상이 될 수 있습니다. " +
                    "BanMod는 경고, 제한, 정지, 차단 또는 기타 제재가 발생하지 않는다고 " +
                    "보장할 수 없습니다.\n\n" +

                    "확인 버튼을 누르면 이 안내를 읽고 이해했으며 BanMod를 적법하게 " +
                    "사용할 책임을 인정하는 것입니다.",

                ConfirmButton =
                    "읽고 확인했습니다",

                BackButton =
                    "뒤로",

                ConfirmPopupTitle =
                    "BanMod",

                ConfirmPopupText =
                    "Vanilla 네트워크가 선택되었습니다.\n\n" +
                    "BanMod는 계속 활성화되어 있습니다.\n\n" +
                    "BanMod 사용 중에는 비공개 로비만 만드세요.\n\n" +
                    "일반 플레이를 하려면 BanMod를 비활성화하세요.",

                PrivatePopupTitle =
                    "BanMod - 비공개 로비 필요",

                PrivatePopupText =
                    "BanMod가 활성화되어 있습니다.\n\n" +
                    "BanMod 사용 중에는 비공개 로비만 허용됩니다.\n\n" +
                    "이 로비를 공개로 변경하는 요청이 차단되었습니다.\n\n" +
                    "일반 공개 로비를 만들려면 BanMod를 비활성화하세요."
            };
        }

        private static BanModServerTexts SimplifiedChinese()
        {
            return new BanModServerTexts
            {
                SelectTitle =
                    "BANMOD - 选择大厅模式",

                SelectDescription =
                    "创建大厅前请选择网络模式。",

                ModdedDescription =
                    "MODDED +25\n使用仅主机模组的网络标记。",

                ModdedButton =
                    "MODDED +25\n推荐",

                VanillaButton =
                    "VANILLA",

                PrivateFooter =
                    "请负责任地使用 BanMod。启用 BanMod 时只能创建私人大厅。",

                VanillaTitle =
                    "重要 - VANILLA 模式",

                VanillaIntro =
                    "你在 BanMod 仍启用的情况下选择了 VANILLA。",

                VanillaWarning =
                    "BanMod 只能在私人大厅中使用。\n\n" +

                    "请勿在公开大厅使用 BanMod，也不要干扰正常的公开游戏。\n\n" +

                    "如果你想在没有 BanMod 的情况下正常游戏，请通过 BanMod 设置中的专用选项禁用模组。\n\n" +

                    "如果继续启用 BanMod，你必须创建私人大厅，并合法且负责任地使用该模组。\n\n" +

                    "你有责任遵守 Among Us 使用条款、模组政策、行为准则、社区规则以及其他玩家的同意。\n\n" +

                    "请勿使用 BanMod 作弊、恶意干扰游戏、获得不公平优势、干扰其他玩家、" +
                    "绕过限制或破坏服务。\n\n" +

                    "使用模组仍可能受到 Innersloth 的执法决定影响。BanMod 无法保证账号不会收到" +
                    "警告、限制、暂停、封禁或其他处罚。\n\n" +

                    "按下确认按钮即表示你已阅读并理解本通知，并愿意为合法使用 BanMod 承担责任。",

                ConfirmButton =
                    "我已阅读并知悉",

                BackButton =
                    "返回",

                ConfirmPopupTitle =
                    "BanMod",

                ConfirmPopupText =
                    "已选择 Vanilla 网络模式。\n\n" +
                    "BanMod 仍保持启用。\n\n" +
                    "使用 BanMod 时只能创建私人大厅。\n\n" +
                    "如需正常游戏，请先禁用 BanMod。",

                PrivatePopupTitle =
                    "BanMod - 必须使用私人大厅",

                PrivatePopupText =
                    "BanMod 已启用。\n\n" +
                    "使用 BanMod 时仅允许私人大厅。\n\n" +
                    "将此大厅设为公开的请求已被阻止。\n\n" +
                    "如需创建普通公开大厅，请先禁用 BanMod。"
            };
        }

        private static BanModServerTexts TraditionalChinese()
        {
            return new BanModServerTexts
            {
                SelectTitle =
                    "BANMOD - 選擇大廳模式",

                SelectDescription =
                    "建立大廳前請選擇網路模式。",

                ModdedDescription =
                    "MODDED +25\n使用僅主機模組的網路標記。",

                ModdedButton =
                    "MODDED +25\n建議",

                VanillaButton =
                    "VANILLA",

                PrivateFooter =
                    "請負責任地使用 BanMod。啟用 BanMod 時只能建立私人房間。",

                VanillaTitle =
                    "重要 - VANILLA 模式",

                VanillaIntro =
                    "你在 BanMod 仍啟用的情況下選擇了 VANILLA。",

                VanillaWarning =
                    "BanMod 只能在私人房間中使用。\n\n" +

                    "請勿在公開房間使用 BanMod，也不要干擾正常的公開遊戲。\n\n" +

                    "如果你想在沒有 BanMod 的情況下正常遊玩，請透過 BanMod 設定中的專用選項停用模組。\n\n" +

                    "如果繼續啟用 BanMod，你必須建立私人房間，並合法且負責任地使用此模組。\n\n" +

                    "你有責任遵守 Among Us 使用條款、模組政策、行為準則、社群規則以及其他玩家的同意。\n\n" +

                    "請勿使用 BanMod 作弊、惡意干擾遊戲、取得不公平優勢、干擾其他玩家、" +
                    "規避限制或破壞服務。\n\n" +

                    "使用模組仍可能受到 Innersloth 的執法決定影響。BanMod 無法保證帳號不會收到" +
                    "警告、限制、停權、封禁或其他處分。\n\n" +

                    "按下確認按鈕即表示你已閱讀並理解本通知，並願意為合法使用 BanMod 承擔責任。",

                ConfirmButton =
                    "我已閱讀並知悉",

                BackButton =
                    "返回",

                ConfirmPopupTitle =
                    "BanMod",

                ConfirmPopupText =
                    "已選擇 Vanilla 網路模式。\n\n" +
                    "BanMod 仍保持啟用。\n\n" +
                    "使用 BanMod 時只能建立私人房間。\n\n" +
                    "如需正常遊玩，請先停用 BanMod。",

                PrivatePopupTitle =
                    "BanMod - 必須使用私人房間",

                PrivatePopupText =
                    "BanMod 已啟用。\n\n" +
                    "使用 BanMod 時僅允許私人房間。\n\n" +
                    "將此房間設為公開的要求已被阻止。\n\n" +
                    "如需建立普通公開房間，請先停用 BanMod。"
            };
        }

        private static BanModServerTexts Filipino()
        {
            return new BanModServerTexts
            {
                SelectTitle =
                    "BANMOD - PILIIN ANG LOBBY MODE",

                SelectDescription =
                    "Piliin ang networking mode bago gumawa ng lobby.",

                ModdedDescription =
                    "MODDED +25\nGumagamit ng modded host networking flag.",

                ModdedButton =
                    "MODDED +25\nINIREREKOMENDA",

                VanillaButton =
                    "VANILLA",

                PrivateFooter =
                    "Gamitin ang BanMod nang responsable. " +
                    "Kapag naka-enable ang BanMod, gumawa lamang ng PRIVATE lobby.",

                VanillaTitle =
                    "MAHALAGA - VANILLA MODE",

                VanillaIntro =
                    "Pinili mo ang VANILLA habang naka-enable pa rin ang BanMod.",

                VanillaWarning =
                    "Ang BanMod ay dapat gamitin LAMANG sa PRIVATE lobby.\n\n" +

                    "Huwag gamitin ang BanMod sa public lobby o para manggulo ng normal na public games.\n\n" +

                    "Kung gusto mong maglaro nang normal nang walang BanMod, i-disable ang mod " +
                    "gamit ang nakalaang option sa BanMod settings.\n\n" +

                    "Kung magpapatuloy ka nang naka-enable ang BanMod, dapat kang gumawa ng PRIVATE lobby " +
                    "at gamitin ang modification nang lehitimo at responsable.\n\n" +

                    "Responsibilidad mong sundin ang Among Us Terms of Use, Mod Policy, Code of Conduct, " +
                    "community rules, at pahintulot ng ibang players.\n\n" +

                    "Huwag gamitin ang BanMod para mandaya, manggulo, magkaroon ng unfair advantage, " +
                    "manghimasok sa ibang players, umiwas sa restrictions, o manggulo ng services.\n\n" +

                    "Ang paggamit ng modifications ay maaari pa ring saklawin ng enforcement ng Innersloth. " +
                    "Hindi magagarantiya ng BanMod na ang account ay hindi makakatanggap ng warning, " +
                    "restriction, suspension, ban, o ibang sanction.\n\n" +

                    "Sa pagpindot sa confirmation button, kinukumpirma mong nabasa at naunawaan mo " +
                    "ang notice na ito at tinatanggap mo ang responsibilidad sa lehitimong paggamit ng BanMod.",

                ConfirmButton =
                    "NABASA KO AT NAUNAWAAN",

                BackButton =
                    "BUMALIK",

                ConfirmPopupTitle =
                    "BanMod",

                ConfirmPopupText =
                    "Napili ang Vanilla networking.\n\n" +
                    "Naka-enable pa rin ang BanMod.\n\n" +
                    "Gumawa lamang ng PRIVATE lobby habang ginagamit ang BanMod.\n\n" +
                    "Para sa normal na laro, i-disable ang BanMod.",

                PrivatePopupTitle =
                    "BanMod - Kailangan ng Private Lobby",

                PrivatePopupText =
                    "Naka-enable ang BanMod.\n\n" +
                    "PRIVATE lobby lamang ang pinapayagan habang ginagamit ang BanMod.\n\n" +
                    "Na-block ang request na gawing public ang lobby.\n\n" +
                    "Para gumawa ng normal na public lobby, i-disable ang BanMod."
            };
        }
    }

    public class ServerSelectionMenu : MonoBehaviour
    {
        public static ServerSelectionMenu Instance;

        private bool showMenu = false;
        private bool showVanillaWarning = false;

        private bool wasCreateScreenOpen = false;
        private bool selectionShownForCurrentScreen = false;

        private float createScreenOpenTime = -1f;

        private Rect windowRect;

        private readonly Vector2 normalWindowSize =
            new Vector2(850f, 600f);

        private readonly Vector2 warningWindowSize =
            new Vector2(950f, 880f);

        private GUIStyle titleStyle;
        private GUIStyle textStyle;
        private GUIStyle buttonStyle;
        private GUIStyle warningStyle;

        public ServerSelectionMenu(IntPtr ptr) : base(ptr)
        {
        }

        void Awake()
        {
            Instance = this;
        }

        void Update()
        {
            if (BanMod.IsBanModDisabled)
            {
                CloseMenu();
                return;
            }

            bool createScreenOpen = false;

            try
            {
                GameObject createScreen =
                    GameObject.Find("CreateGameScreen");

                createScreenOpen =
                    createScreen != null &&
                    createScreen.activeInHierarchy;
            }
            catch
            {
                createScreenOpen = false;
            }

            if (createScreenOpen && !wasCreateScreenOpen)
            {
                wasCreateScreenOpen = true;
                selectionShownForCurrentScreen = false;

                createScreenOpenTime = Time.time;

                BanModServerSelection.Reset();

                BMLogger.LogInfo(
                    "[BanMod Server] Create Game screen opened."
                );
            }

            if (!createScreenOpen && wasCreateScreenOpen)
            {
                wasCreateScreenOpen = false;
                selectionShownForCurrentScreen = false;

                createScreenOpenTime = -1f;

                BanModServerSelection.Reset();

                CloseMenu();

                BMLogger.LogInfo(
                    "[BanMod Server] Create Game screen closed. State reset."
                );

                return;
            }

            if (!createScreenOpen)
                return;

            if (selectionShownForCurrentScreen)
                return;

            if (createScreenOpenTime < 0f)
                return;

            if (Time.time - createScreenOpenTime < 0.35f)
                return;

            selectionShownForCurrentScreen = true;

            OpenMenu();
        }

        public void OpenMenu()
        {
            showVanillaWarning = false;
            showMenu = true;

            CenterWindow();

            BMLogger.LogInfo(
                "[BanMod Server] Server selection menu opened."
            );
        }

        public void CloseMenu()
        {
            showMenu = false;
            showVanillaWarning = false;
        }

        public bool IsOpen()
        {
            return showMenu;
        }

        private void CenterWindow()
        {
            Vector2 size =
                showVanillaWarning
                    ? warningWindowSize
                    : normalWindowSize;

            windowRect =
                new Rect(
                    Screen.width / 2f - size.x / 2f,
                    Screen.height / 2f - size.y / 2f,
                    size.x,
                    size.y
                );
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
                return;

            titleStyle =
                new GUIStyle(GUI.skin.label)
                {
                    fontSize = 28,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };

            titleStyle.normal.textColor =
                Color.white;

            textStyle =
                new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    alignment = TextAnchor.UpperLeft,
                    wordWrap = true
                };

            textStyle.normal.textColor =
                Color.white;

            warningStyle =
                new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperLeft,
                    wordWrap = true
                };

            warningStyle.normal.textColor =
                new Color(
                    1f,
                    0.65f,
                    0.20f,
                    1f
                );

            buttonStyle =
                new GUIStyle(GUI.skin.button)
                {
                    fontSize = 21,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };

            buttonStyle.normal.textColor =
                Color.white;
        }

        void OnGUI()
        {
            if (!showMenu)
                return;

            if (BanMod.IsBanModDisabled)
                return;

            if (Event.current.isMouse)
            {
                Event.current.Use();
            }

            EnsureStyles();

            GUI.backgroundColor =
                Color.black;

            if (showVanillaWarning)
            {
                windowRect =
                    GUI.Window(
                        42101,
                        windowRect,
                        (GUI.WindowFunction)DrawVanillaWarning,
                        "",
                        BanModUiStyles.BlackWindow
                    );
            }
            else
            {
                windowRect =
                    GUI.Window(
                        42100,
                        windowRect,
                        (GUI.WindowFunction)DrawServerSelection,
                        "",
                        BanModUiStyles.BlackWindow
                    );
            }

            GUI.backgroundColor =
                Color.white;
        }

        private void DrawServerSelection(int id)
        {
            BanModServerTexts t =
                BanModServerLocalization.Get();

            GUILayout.Space(10);

            GUILayout.Label(
                t.SelectTitle,
                titleStyle
            );

            GUILayout.Space(20);

            GUILayout.Label(
                t.SelectDescription,
                textStyle
            );

            GUILayout.Space(15);

            GUILayout.Label(
                t.ModdedDescription,
                textStyle
            );

            GUILayout.Space(20);

            GUI.backgroundColor =
                new Color(
                    0.05f,
                    0.55f,
                    0.12f,
                    1f
                );

            if (GUILayout.Button(
                t.ModdedButton,
                buttonStyle,
                GUILayout.Height(90f)
            ))
            {
                BanModServerSelection.Mode =
                    BanModServerMode.Modded25;

                BanModServerSelection.VanillaAcknowledged =
                    false;

                BMLogger.LogInfo(
                    "[BanMod Server] MODDED +25 selected."
                );

                CloseMenu();
            }

            GUILayout.Space(20);

            GUI.backgroundColor =
                new Color(
                    0.75f,
                    0.38f,
                    0.05f,
                    1f
                );

            if (GUILayout.Button(
                t.VanillaButton,
                buttonStyle,
                GUILayout.Height(90f)
            ))
            {
                BanModServerSelection.Mode =
                    BanModServerMode.Vanilla;

                BanModServerSelection.VanillaAcknowledged =
                    false;

                showVanillaWarning =
                    true;

                CenterWindow();

                BMLogger.LogWarning(
                    "[BanMod Server] VANILLA selected. Awaiting acknowledgement."
                );
            }

            GUILayout.Space(25);

            GUI.backgroundColor =
                Color.white;

            GUILayout.Label(
                t.PrivateFooter,
                warningStyle
            );

            GUI.DragWindow();
        }

        private void DrawVanillaWarning(int id)
        {
            BanModServerTexts t =
                BanModServerLocalization.Get();

            GUILayout.Space(10);

            GUILayout.Label(
                t.VanillaTitle,
                titleStyle
            );

            GUILayout.Space(15);

            GUILayout.Label(
                t.VanillaIntro,
                warningStyle
            );

            GUILayout.Space(15);

            GUILayout.Label(
                t.VanillaWarning,
                textStyle
            );

            GUILayout.Space(10);

            GUI.backgroundColor =
                new Color(
                    0.05f,
                    0.50f,
                    0.12f,
                    1f
                );

            if (GUILayout.Button(
                t.ConfirmButton,
                buttonStyle,
                GUILayout.Height(75f)
            ))
            {
                BanModServerSelection.Mode =
                    BanModServerMode.Vanilla;

                BanModServerSelection.VanillaAcknowledged =
                    true;

                BMLogger.LogWarning(
                    "[BanMod Server] Vanilla warning read and acknowledged."
                );

                CloseMenu();

                BanModPopup.CreateMessagePopup(
                    t.ConfirmPopupTitle,
                    t.ConfirmPopupText
                );
            }

            GUILayout.Space(10);

            GUI.backgroundColor =
                new Color(
                    0.30f,
                    0.30f,
                    0.30f,
                    1f
                );

            if (GUILayout.Button(
                t.BackButton,
                buttonStyle,
                GUILayout.Height(55f)
            ))
            {
                BanModServerSelection.Mode =
                    BanModServerMode.None;

                BanModServerSelection.VanillaAcknowledged =
                    false;

                showVanillaWarning =
                    false;

                CenterWindow();

                BMLogger.LogInfo(
                    "[BanMod Server] Returned to server selection."
                );
            }

            GUI.backgroundColor =
                Color.white;

            GUI.DragWindow();
        }
    }

    [HarmonyPatch(
        typeof(Constants),
        nameof(Constants.GetBroadcastVersion)
    )]
    public static class BanModBroadcastVersionPatch
    {
        public static void Postfix(ref int __result)
        {
            if (!BanModServerSelection.IsModded25)
                return;

            int original =
                __result;

            __result += 25;

            BMLogger.LogInfo(
                "[BanMod Server] Protocol version: " +
                original +
                " -> " +
                __result +
                " (+25 Modded Flag)"
            );
        }
    }
    [HarmonyPatch(
    typeof(Constants),
    nameof(Constants.IsVersionModded)
)]
    public static class BanModIsVersionModdedPatch
    {
        public static bool Prefix(ref bool __result)
        {
            if (!BanModServerSelection.IsModded25)
                return true;

            __result = true;

            BMLogger.LogInfo(
                "[BanMod Server] Constants.IsVersionModded = TRUE (+25 mode)."
            );

            return false;
        }
    }

    [HarmonyPatch(
        typeof(InnerNetClient),
        nameof(InnerNetClient.ChangeGamePublic)
    )]
    public static class BanModPrivateLobbyPatch
    {
        public static void Prefix(ref bool __0)
        {
            if (!BanModServerSelection.HasSelectedMode)
                return;

            if (!__0)
                return;

            __0 = false;

            BanModServerTexts t =
                BanModServerLocalization.Get();

            BMLogger.LogWarning(
                "[BanMod Server] Attempt to make lobby public blocked."
            );

            BanModPopup.CreateMessagePopup(
                t.PrivatePopupTitle,
                t.PrivatePopupText
            );
        }
    }
}
