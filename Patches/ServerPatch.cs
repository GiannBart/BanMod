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
        public string VanillaDescription;
        public string ModdedButton;
        public string VanillaButton;
        public string PrivateFooter;

        public string VanillaTitle;
        public string VanillaIntro;
        public string VanillaWarning;
        public string ConfirmButton;
        public string BackButton;

    }

    public static class BanModServerLocalization
    {
        public static BanModServerTexts Get()
        {
            string language = GetLanguageId();

            if (Has(language, "italian", "italiano"))
                return Italian();

            if (Has(language, "french", "franÃ§ais", "francais"))
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
                "espaÃ±ollatam"
            ))
                return SpanishLatam();

            if (Has(
                language,
                "spanish",
                "spanisheU",
                "spanish_eu",
                "espanol",
                "espaÃ±ol"
            ))
                return Spanish();

            if (Has(
                language,
                "brazilian",
                "brazilianportuguese",
                "portuguesebrazil",
                "portuguesebr",
                "portuguÃªsbr",
                "portuguesbr"
            ))
                return BrazilianPortuguese();

            if (Has(
                language,
                "portuguese",
                "portugueseeu",
                "portuguese_eu",
                "portuguÃªs",
                "portugues"
            ))
                return Portuguese();

            if (Has(language, "dutch", "nederlands"))
                return Dutch();

            if (Has(language, "russian", "Ñ€ÑƒÑÑÐºÐ¸Ð¹"))
                return Russian();

            if (Has(language, "japanese", "æ—¥æœ¬èªž"))
                return Japanese();

            if (Has(language, "korean", "í•œêµ­ì–´"))
                return Korean();

            if (Has(
                language,
                "schinese",
                "simplifiedchinese",
                "chinesesimplified",
                "chinese_cn",
                "ç®€ä½“ä¸­æ–‡"
            ))
                return SimplifiedChinese();

            if (Has(
                language,
                "tchinese",
                "traditionalchinese",
                "chinesetraditional",
                "chinese_tw",
                "ç¹é«”ä¸­æ–‡",
                "ç¹ä½“ä¸­æ–‡"
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
                    "Choose the lobby mode before creating this lobby.",
                ModdedDescription =
                    "Host a lobby with gameplay changes.",
                VanillaDescription =
                    "Host a lobby without gameplay changes.",
                ModdedButton =
                    "MODDED +25\nRECOMMENDED",
                VanillaButton =
                    "VANILLA",
                PrivateFooter =
                    "Use BanMod responsibly and select the mode that matches the type of lobby you are creating.",
                VanillaTitle =
                    "IMPORTANT - VANILLA MODE",
                VanillaIntro =
                    "You selected Vanilla mode.",
                VanillaWarning =
                    "Please do not use BanMod to annoy or disturb other players, and do not enable options that change gameplay, provide unfair advantages, or alter the experience of other players.\n\nImproper, unauthorized, or rule-breaking use may result in warnings, restrictions, suspensions, bans, or other sanctions from Innersloth or other services involved.\n\nBanMod and its developers are not responsible for any consequences resulting from improper, unlawful, or unauthorized use of the mod.\n\nUse only features compatible with Vanilla mode and respect the rules of Among Us and any other services being used.",
                ConfirmButton =
                    "I AGREE",
                BackButton =
                    "I DECLINE"
            };
        }

        private static BanModServerTexts Italian()
        {
            return new BanModServerTexts
            {
                SelectTitle =
                    "BANMOD - SELEZIONA MODALITÀ LOBBY",
                SelectDescription =
                    "Scegli la modalità della lobby prima di crearla.",
                ModdedDescription =
                    "Ospita una lobby con modifiche al gameplay.",
                VanillaDescription =
                    "Ospita una lobby senza modifiche al gameplay.",
                ModdedButton =
                    "MODDED +25\nCONSIGLIATO",
                VanillaButton =
                    "VANILLA",
                PrivateFooter =
                    "Usa BanMod responsabilmente e seleziona la modalità che corrisponde al tipo di lobby che stai creando.",
                VanillaTitle =
                    "IMPORTANTE - MODALITÀ VANILLA",
                VanillaIntro =
                    "Hai scelto la modalità Vanilla.",
                VanillaWarning =
                    "Per favore, non utilizzare BanMod per infastidire o disturbare altri giocatori e non attivare opzioni che modificano il gameplay, forniscono vantaggi sleali o alterano l’esperienza degli altri giocatori.\n\nUn utilizzo improprio, non consentito o contrario alle regole può provocare avvertimenti, restrizioni, sospensioni, ban o altre sanzioni da parte di Innersloth o di altri servizi coinvolti.\n\nBanMod e i suoi sviluppatori non sono responsabili per eventuali conseguenze derivanti da un utilizzo improprio, illecito o non consentito della mod.\n\nAssicurati di utilizzare esclusivamente funzionalità compatibili con la modalità Vanilla e di rispettare le regole di Among Us e degli altri servizi utilizzati.",
                ConfirmButton =
                    "ACCETTO",
                BackButton =
                    "NEGO"
            };
        }

        private static BanModServerTexts French()
        {
            return new BanModServerTexts
            {
                SelectTitle =
                    "BANMOD - SÉLECTION DU MODE DU SALON",
                SelectDescription =
                    "Choisissez le mode du salon avant de le créer.",
                ModdedDescription =
                    "Hébergez un salon avec des modifications du gameplay.",
                VanillaDescription =
                    "Hébergez un salon sans modifications du gameplay.",
                ModdedButton =
                    "MODDED +25\nRECOMMANDÉ",
                VanillaButton =
                    "VANILLA",
                PrivateFooter =
                    "Utilisez BanMod de manière responsable et choisissez le mode correspondant au type de salon que vous créez.",
                VanillaTitle =
                    "IMPORTANT - MODE VANILLA",
                VanillaIntro =
                    "Vous avez choisi le mode Vanilla.",
                VanillaWarning =
                    "Veuillez ne pas utiliser BanMod pour déranger ou perturber les autres joueurs, et n’activez pas d’options qui modifient le gameplay, donnent des avantages injustes ou changent l’expérience des autres joueurs.\n\nUne utilisation abusive, non autorisée ou contraire aux règles peut entraîner des avertissements, restrictions, suspensions, bannissements ou autres sanctions de la part d’Innersloth ou d’autres services concernés.\n\nBanMod et ses développeurs ne sont pas responsables des conséquences résultant d’une utilisation abusive, illégale ou non autorisée du mod.\n\nUtilisez uniquement des fonctionnalités compatibles avec le mode Vanilla et respectez les règles d’Among Us et des autres services utilisés.",
                ConfirmButton =
                    "J’ACCEPTE",
                BackButton =
                    "JE REFUSE"
            };
        }

        private static BanModServerTexts German()
        {
            return new BanModServerTexts
            {
                SelectTitle =
                    "BANMOD - LOBBY-MODUS AUSWÄHLEN",
                SelectDescription =
                    "Wähle den Lobby-Modus, bevor du diese Lobby erstellst.",
                ModdedDescription =
                    "Hoste eine Lobby mit Gameplay-Änderungen.",
                VanillaDescription =
                    "Hoste eine Lobby ohne Gameplay-Änderungen.",
                ModdedButton =
                    "MODDED +25\nEMPFOHLEN",
                VanillaButton =
                    "VANILLA",
                PrivateFooter =
                    "Verwende BanMod verantwortungsvoll und wähle den Modus, der zu deiner Lobby passt.",
                VanillaTitle =
                    "WICHTIG - VANILLA-MODUS",
                VanillaIntro =
                    "Du hast den Vanilla-Modus ausgewählt.",
                VanillaWarning =
                    "Bitte verwende BanMod nicht, um andere Spieler zu stören oder zu belästigen, und aktiviere keine Optionen, die das Gameplay verändern, unfaire Vorteile geben oder die Erfahrung anderer Spieler verändern.\n\nEine unsachgemäße, nicht autorisierte oder regelwidrige Nutzung kann zu Verwarnungen, Einschränkungen, Sperren, Bans oder anderen Maßnahmen durch Innersloth oder andere beteiligte Dienste führen.\n\nBanMod und seine Entwickler sind nicht für Folgen verantwortlich, die aus einer unsachgemäßen, rechtswidrigen oder nicht autorisierten Nutzung der Mod entstehen.\n\nVerwende nur mit dem Vanilla-Modus kompatible Funktionen und halte die Regeln von Among Us sowie der verwendeten Dienste ein.",
                ConfirmButton =
                    "ICH STIMME ZU",
                BackButton =
                    "ICH LEHNE AB"
            };
        }

        private static BanModServerTexts Spanish()
        {
            return new BanModServerTexts
            {
                SelectTitle =
                    "BANMOD - SELECCIONAR MODO DE SALA",
                SelectDescription =
                    "Elige el modo de la sala antes de crearla.",
                ModdedDescription =
                    "Organiza una sala con modificaciones del gameplay.",
                VanillaDescription =
                    "Organiza una sala sin modificaciones del gameplay.",
                ModdedButton =
                    "MODDED +25\nRECOMENDADO",
                VanillaButton =
                    "VANILLA",
                PrivateFooter =
                    "Usa BanMod de forma responsable y selecciona el modo que corresponda al tipo de sala que vas a crear.",
                VanillaTitle =
                    "IMPORTANTE - MODO VANILLA",
                VanillaIntro =
                    "Has elegido el modo Vanilla.",
                VanillaWarning =
                    "Por favor, no uses BanMod para molestar o perturbar a otros jugadores y no actives opciones que cambien el gameplay, proporcionen ventajas injustas o alteren la experiencia de otros jugadores.\n\nEl uso indebido, no autorizado o contrario a las reglas puede provocar advertencias, restricciones, suspensiones, baneos u otras sanciones por parte de Innersloth u otros servicios involucrados.\n\nBanMod y sus desarrolladores no son responsables de las consecuencias derivadas de un uso indebido, ilegal o no autorizado del mod.\n\nUtiliza únicamente funciones compatibles con el modo Vanilla y respeta las reglas de Among Us y de los demás servicios utilizados.",
                ConfirmButton =
                    "ACEPTO",
                BackButton =
                    "NO ACEPTO"
            };
        }

        private static BanModServerTexts SpanishLatam()
        {
            return new BanModServerTexts
            {
                SelectTitle =
                    "BANMOD - SELECCIONAR MODO DE SALA",
                SelectDescription =
                    "Elige el modo de la sala antes de crearla.",
                ModdedDescription =
                    "Crea una sala con modificaciones del gameplay.",
                VanillaDescription =
                    "Crea una sala sin modificaciones del gameplay.",
                ModdedButton =
                    "MODDED +25\nRECOMENDADO",
                VanillaButton =
                    "VANILLA",
                PrivateFooter =
                    "Usa BanMod de forma responsable y selecciona el modo que corresponda al tipo de sala que vas a crear.",
                VanillaTitle =
                    "IMPORTANTE - MODO VANILLA",
                VanillaIntro =
                    "Has elegido el modo Vanilla.",
                VanillaWarning =
                    "Por favor, no uses BanMod para molestar o perturbar a otros jugadores y no actives opciones que cambien el gameplay, den ventajas injustas o alteren la experiencia de otros jugadores.\n\nEl uso indebido, no autorizado o contrario a las reglas puede provocar advertencias, restricciones, suspensiones, baneos u otras sanciones por parte de Innersloth u otros servicios involucrados.\n\nBanMod y sus desarrolladores no son responsables de las consecuencias derivadas de un uso indebido, ilegal o no autorizado del mod.\n\nUtiliza únicamente funciones compatibles con el modo Vanilla y respeta las reglas de Among Us y de los demás servicios utilizados.",
                ConfirmButton =
                    "ACEPTO",
                BackButton =
                    "NO ACEPTO"
            };
        }

        private static BanModServerTexts BrazilianPortuguese()
        {
            return new BanModServerTexts
            {
                SelectTitle =
                    "BANMOD - SELECIONAR MODO DA SALA",
                SelectDescription =
                    "Escolha o modo da sala antes de criá-la.",
                ModdedDescription =
                    "Hospede uma sala com alterações no gameplay.",
                VanillaDescription =
                    "Hospede uma sala sem alterações no gameplay.",
                ModdedButton =
                    "MODDED +25\nRECOMENDADO",
                VanillaButton =
                    "VANILLA",
                PrivateFooter =
                    "Use o BanMod com responsabilidade e selecione o modo correspondente ao tipo de sala que você está criando.",
                VanillaTitle =
                    "IMPORTANTE - MODO VANILLA",
                VanillaIntro =
                    "Você escolheu o modo Vanilla.",
                VanillaWarning =
                    "Por favor, não use o BanMod para incomodar ou perturbar outros jogadores e não ative opções que alterem o gameplay, forneçam vantagens injustas ou mudem a experiência de outros jogadores.\n\nO uso indevido, não autorizado ou contrário às regras pode resultar em avisos, restrições, suspensões, banimentos ou outras sanções da Innersloth ou de outros serviços envolvidos.\n\nO BanMod e seus desenvolvedores não são responsáveis por quaisquer consequências decorrentes do uso indevido, ilegal ou não autorizado do mod.\n\nUse apenas recursos compatíveis com o modo Vanilla e respeite as regras de Among Us e dos outros serviços utilizados.",
                ConfirmButton =
                    "ACEITO",
                BackButton =
                    "NÃO ACEITO"
            };
        }

        private static BanModServerTexts Portuguese()
        {
            return new BanModServerTexts
            {
                SelectTitle =
                    "BANMOD - SELECIONAR MODO DA SALA",
                SelectDescription =
                    "Escolhe o modo da sala antes de a criares.",
                ModdedDescription =
                    "Aloja uma sala com alterações ao gameplay.",
                VanillaDescription =
                    "Aloja uma sala sem alterações ao gameplay.",
                ModdedButton =
                    "MODDED +25\nRECOMENDADO",
                VanillaButton =
                    "VANILLA",
                PrivateFooter =
                    "Usa o BanMod de forma responsável e seleciona o modo correspondente ao tipo de sala que estás a criar.",
                VanillaTitle =
                    "IMPORTANTE - MODO VANILLA",
                VanillaIntro =
                    "Escolheste o modo Vanilla.",
                VanillaWarning =
                    "Por favor, não uses o BanMod para incomodar ou perturbar outros jogadores e não atives opções que alterem o gameplay, deem vantagens injustas ou mudem a experiência dos outros jogadores.\n\nUma utilização indevida, não autorizada ou contrária às regras pode resultar em avisos, restrições, suspensões, banimentos ou outras sanções da Innersloth ou de outros serviços envolvidos.\n\nO BanMod e os seus desenvolvedores não são responsáveis por quaisquer consequências resultantes de uma utilização indevida, ilegal ou não autorizada da mod.\n\nUtiliza apenas funcionalidades compatíveis com o modo Vanilla e respeita as regras de Among Us e dos outros serviços utilizados.",
                ConfirmButton =
                    "ACEITO",
                BackButton =
                    "NÃO ACEITO"
            };
        }

        private static BanModServerTexts Dutch()
        {
            return new BanModServerTexts
            {
                SelectTitle =
                    "BANMOD - LOBBYMODUS SELECTEREN",
                SelectDescription =
                    "Kies de lobbymodus voordat je deze lobby maakt.",
                ModdedDescription =
                    "Host een lobby met gameplaywijzigingen.",
                VanillaDescription =
                    "Host een lobby zonder gameplaywijzigingen.",
                ModdedButton =
                    "MODDED +25\nAANBEVOLEN",
                VanillaButton =
                    "VANILLA",
                PrivateFooter =
                    "Gebruik BanMod verantwoord en kies de modus die past bij het type lobby dat je maakt.",
                VanillaTitle =
                    "BELANGRIJK - VANILLA-MODUS",
                VanillaIntro =
                    "Je hebt de Vanilla-modus gekozen.",
                VanillaWarning =
                    "Gebruik BanMod niet om andere spelers te ergeren of te hinderen en schakel geen opties in die de gameplay veranderen, oneerlijke voordelen geven of de ervaring van andere spelers aanpassen.\n\nOnjuist, ongeoorloofd of regelstrijdig gebruik kan leiden tot waarschuwingen, beperkingen, schorsingen, bans of andere sancties van Innersloth of andere betrokken diensten.\n\nBanMod en de ontwikkelaars zijn niet verantwoordelijk voor gevolgen die voortkomen uit onjuist, onwettig of ongeoorloofd gebruik van de mod.\n\nGebruik alleen functies die compatibel zijn met de Vanilla-modus en respecteer de regels van Among Us en de gebruikte diensten.",
                ConfirmButton =
                    "IK GA AKKOORD",
                BackButton =
                    "IK WEIGER"
            };
        }

        private static BanModServerTexts Russian()
        {
            return new BanModServerTexts
            {
                SelectTitle =
                    "BANMOD - ВЫБОР РЕЖИМА ЛОББИ",
                SelectDescription =
                    "Выберите режим лобби перед его созданием.",
                ModdedDescription =
                    "Создайте лобби с изменениями игрового процесса.",
                VanillaDescription =
                    "Создайте лобби без изменений игрового процесса.",
                ModdedButton =
                    "MODDED +25\nРЕКОМЕНДУЕТСЯ",
                VanillaButton =
                    "VANILLA",
                PrivateFooter =
                    "Используйте BanMod ответственно и выбирайте режим, соответствующий создаваемому лобби.",
                VanillaTitle =
                    "ВАЖНО - РЕЖИМ VANILLA",
                VanillaIntro =
                    "Вы выбрали режим Vanilla.",
                VanillaWarning =
                    "Пожалуйста, не используйте BanMod для того, чтобы мешать или раздражать других игроков, и не включайте функции, которые изменяют игровой процесс, дают нечестные преимущества или влияют на опыт других игроков.\n\nНеправильное, несанкционированное или нарушающее правила использование может привести к предупреждениям, ограничениям, приостановке, блокировке или другим санкциям со стороны Innersloth или других сервисов.\n\nBanMod и его разработчики не несут ответственности за последствия неправильного, незаконного или несанкционированного использования мода.\n\nИспользуйте только функции, совместимые с режимом Vanilla, и соблюдайте правила Among Us и других используемых сервисов.",
                ConfirmButton =
                    "ПРИНИМАЮ",
                BackButton =
                    "ОТКАЗЫВАЮСЬ"
            };
        }

        private static BanModServerTexts Japanese()
        {
            return new BanModServerTexts
            {
                SelectTitle =
                    "BANMOD - ロビーモードを選択",
                SelectDescription =
                    "ロビーを作成する前にモードを選択してください。",
                ModdedDescription =
                    "ゲームプレイを変更するロビーをホストします。",
                VanillaDescription =
                    "ゲームプレイを変更しないロビーをホストします。",
                ModdedButton =
                    "MODDED +25\n推奨",
                VanillaButton =
                    "VANILLA",
                PrivateFooter =
                    "BanModを責任を持って使用し、作成するロビーに合ったモードを選択してください。",
                VanillaTitle =
                    "重要 - VANILLAモード",
                VanillaIntro =
                    "VANILLAモードを選択しました。",
                VanillaWarning =
                    "他のプレイヤーを困らせたり妨害したりするためにBanModを使用しないでください。また、ゲームプレイを変更したり、不公平な優位性を与えたり、他のプレイヤーの体験を変えたりするオプションを有効にしないでください。\n\n不適切、許可されていない、またはルールに反する使用は、Innerslothや関係する他のサービスによる警告、制限、停止、BAN、その他の措置につながる可能性があります。\n\nBanModおよび開発者は、Modの不適切、違法、または許可されていない使用によって生じた結果について責任を負いません。\n\nVANILLAモードと互換性のある機能のみを使用し、Among Usおよび利用する他のサービスのルールを守ってください。",
                ConfirmButton =
                    "同意する",
                BackButton =
                    "拒否する"
            };
        }

        private static BanModServerTexts Korean()
        {
            return new BanModServerTexts
            {
                SelectTitle =
                    "BANMOD - 로비 모드 선택",
                SelectDescription =
                    "로비를 만들기 전에 로비 모드를 선택하세요.",
                ModdedDescription =
                    "게임플레이가 변경된 로비를 호스트합니다.",
                VanillaDescription =
                    "게임플레이가 변경되지 않은 로비를 호스트합니다.",
                ModdedButton =
                    "MODDED +25\n권장",
                VanillaButton =
                    "VANILLA",
                PrivateFooter =
                    "BanMod를 책임감 있게 사용하고 생성하려는 로비에 맞는 모드를 선택하세요.",
                VanillaTitle =
                    "중요 - VANILLA 모드",
                VanillaIntro =
                    "VANILLA 모드를 선택했습니다.",
                VanillaWarning =
                    "다른 플레이어를 괴롭히거나 방해하기 위해 BanMod를 사용하지 말고, 게임플레이를 변경하거나 부당한 이점을 제공하거나 다른 플레이어의 경험을 바꾸는 옵션을 활성화하지 마세요.\n\n부적절하거나 승인되지 않았거나 규칙을 위반하는 사용은 Innersloth 또는 관련 서비스에서 경고, 제한, 정지, 밴 또는 기타 제재를 받을 수 있습니다.\n\nBanMod와 개발자는 모드의 부적절하거나 불법적이거나 승인되지 않은 사용으로 인해 발생하는 결과에 대해 책임을 지지 않습니다.\n\nVANILLA 모드와 호환되는 기능만 사용하고 Among Us 및 사용하는 다른 서비스의 규칙을 준수하세요.",
                ConfirmButton =
                    "동의",
                BackButton =
                    "거부"
            };
        }

        private static BanModServerTexts SimplifiedChinese()
        {
            return new BanModServerTexts
            {
                SelectTitle =
                    "BANMOD - 选择大厅模式",
                SelectDescription =
                    "创建大厅前请选择大厅模式。",
                ModdedDescription =
                    "创建一个包含游戏玩法修改的大厅。",
                VanillaDescription =
                    "创建一个不修改游戏玩法的大厅。",
                ModdedButton =
                    "MODDED +25\n推荐",
                VanillaButton =
                    "VANILLA",
                PrivateFooter =
                    "请负责任地使用 BanMod，并选择与当前大厅类型相符的模式。",
                VanillaTitle =
                    "重要 - VANILLA 模式",
                VanillaIntro =
                    "你选择了 Vanilla 模式。",
                VanillaWarning =
                    "请不要使用 BanMod 骚扰或干扰其他玩家，也不要启用会修改游戏玩法、提供不公平优势或改变其他玩家游戏体验的选项。\n\n不当、未经授权或违反规则的使用可能导致 Innersloth 或其他相关服务发出警告、限制、暂停、封禁或其他处罚。\n\n对于因不当、违法或未经授权使用该 Mod 而产生的任何后果，BanMod 及其开发者不承担责任。\n\n请仅使用与 Vanilla 模式兼容的功能，并遵守 Among Us 及其他所使用服务的规则。",
                ConfirmButton =
                    "接受",
                BackButton =
                    "拒绝"
            };
        }

        private static BanModServerTexts TraditionalChinese()
        {
            return new BanModServerTexts
            {
                SelectTitle =
                    "BANMOD - 選擇大廳模式",
                SelectDescription =
                    "建立大廳前請選擇大廳模式。",
                ModdedDescription =
                    "建立一個包含遊戲玩法修改的大廳。",
                VanillaDescription =
                    "建立一個不修改遊戲玩法的大廳。",
                ModdedButton =
                    "MODDED +25\n推薦",
                VanillaButton =
                    "VANILLA",
                PrivateFooter =
                    "請負責任地使用 BanMod，並選擇符合目前大廳類型的模式。",
                VanillaTitle =
                    "重要 - VANILLA 模式",
                VanillaIntro =
                    "你選擇了 Vanilla 模式。",
                VanillaWarning =
                    "請不要使用 BanMod 騷擾或干擾其他玩家，也不要啟用會修改遊戲玩法、提供不公平優勢或改變其他玩家遊戲體驗的選項。\n\n不當、未經授權或違反規則的使用可能導致 Innersloth 或其他相關服務發出警告、限制、暫停、封禁或其他處分。\n\n對於因不當、違法或未經授權使用此 Mod 而產生的任何後果，BanMod 及其開發者不承擔責任。\n\n請僅使用與 Vanilla 模式相容的功能，並遵守 Among Us 及其他使用中服務的規則。",
                ConfirmButton =
                    "接受",
                BackButton =
                    "拒絕"
            };
        }

        private static BanModServerTexts Filipino()
        {
            return new BanModServerTexts
            {
                SelectTitle =
                    "BANMOD - PILIIN ANG LOBBY MODE",
                SelectDescription =
                    "Piliin ang lobby mode bago gawin ang lobby.",
                ModdedDescription =
                    "Mag-host ng lobby na may gameplay changes.",
                VanillaDescription =
                    "Mag-host ng lobby na walang gameplay changes.",
                ModdedButton =
                    "MODDED +25\nINIREREKOMENDA",
                VanillaButton =
                    "VANILLA",
                PrivateFooter =
                    "Gamitin ang BanMod nang responsable at piliin ang mode na tumutugma sa lobby na iyong ginagawa.",
                VanillaTitle =
                    "MAHALAGA - VANILLA MODE",
                VanillaIntro =
                    "Pinili mo ang Vanilla mode.",
                VanillaWarning =
                    "Huwag gamitin ang BanMod para manggulo o mang-abala ng ibang manlalaro, at huwag i-enable ang mga option na nagbabago ng gameplay, nagbibigay ng hindi patas na advantage, o binabago ang experience ng ibang manlalaro.\n\nAng maling paggamit, hindi awtorisadong paggamit, o paggamit na labag sa rules ay maaaring magresulta sa warnings, restrictions, suspensions, bans, o iba pang sanctions mula sa Innersloth o iba pang serbisyong kasangkot.\n\nHindi mananagot ang BanMod at ang mga developer nito sa anumang kahihinatnan mula sa maling paggamit, ilegal, o hindi awtorisadong paggamit ng mod.\n\nGamitin lamang ang mga feature na compatible sa Vanilla mode at sundin ang rules ng Among Us at ng iba pang serbisyong ginagamit.",
                ConfirmButton =
                    "SUMASANG-AYON AKO",
                BackButton =
                    "HINDI AKO SUMASANG-AYON"
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
            new Vector2(950f, 720f);

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

                CloseMenu();

                BMLogger.LogInfo(
                    "[BanMod Server] Create Game screen closed. Selected mode preserved for the current lobby."
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

            GUILayout.Label(
                t.VanillaDescription,
                textStyle
            );

            GUILayout.Space(20);

            if (GUILayout.Button(
                t.VanillaButton,
                buttonStyle,
                GUILayout.Height(90f)
            ))
            {
                BanModServerSelection.Mode =
                    BanModServerMode.None;

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
                    "[BanMod Server] Vanilla warning accepted. Vanilla mode selected."
                );

                CloseMenu();
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
                    BanModServerMode.Modded25;

                BanModServerSelection.VanillaAcknowledged =
                    false;

                BMLogger.LogWarning(
                    "[BanMod Server] Vanilla warning declined. Falling back to MODDED +25."
                );

                CloseMenu();
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
}