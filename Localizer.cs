using System.Globalization;

namespace AppVolumeHotkeys;

internal sealed record LanguageOption(string Code, string DisplayName)
{
    public override string ToString() => DisplayName;
}

internal static class Localizer
{
    private static readonly Dictionary<string, Dictionary<string, string>> Languages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ru"] = new()
        {
            ["Language"] = "Язык:",
            ["AddProcess"] = "Добавить процесс:",
            ["VolumeStep"] = "Шаг громкости:",
            ["Refresh"] = "Обновить список",
            ["AddToTargets"] = "Добавить в цели",
            ["StartWithWindows"] = "Запускать с Windows сразу в трей",
            ["HardwareVolume"] = "Специальная кнопка управления звуком управляет активным отмеченным приложением",
            ["LogKeys"] = "Логировать клавиши",
            ["HotkeyUp"] = "Увеличить все цели:",
            ["HotkeyDown"] = "Уменьшить все цели:",
            ["HotkeyMute"] = "Mute все цели:",
            ["Targets"] = "Цели для трех биндов:",
            ["Process"] = "Процесс",
            ["Volume"] = "Громкость",
            ["SessionName"] = "Название сессии",
            ["Open"] = "Открыть",
            ["RefreshSessions"] = "Обновить сессии",
            ["CheckUpdates"] = "Проверить обновления",
            ["About"] = "Об авторе",
            ["AboutText"] = "App Volume Hotkeys\nАвтор: Saba\nЛицензия: MIT",
            ["UpdateAvailableTitle"] = "Доступно обновление",
            ["UpdateAvailableText"] = "Доступна новая версия {0}. Сейчас установлена {1}.\n\nОткрыть страницу релиза?",
            ["NoUpdateTitle"] = "Обновления",
            ["NoUpdateText"] = "Установлена последняя версия {0}.",
            ["UpdateCheckFailed"] = "Не удалось проверить обновления: {0}",
            ["Exit"] = "Выход",
            ["TrayMessage"] = "Приложение продолжает работать в трее.",
            ["NoTargets"] = "Нет целей",
            ["NoTargetsDetail"] = "Приложения не выбраны",
            ["NoTargetsStatus"] = "Сначала отметьте приложения в списке целей.",
            ["NoSession"] = "Сессия не найдена",
            ["NoSessionStatus"] = "У выбранных приложений нет активных аудиосессий.",
            ["FoundSessions"] = "Найдено аудиосессий: {0}. Целей: {1}",
            ["AddedTarget"] = "Добавлено в цели: {0}",
            ["KeyboardLog"] = "Лог клавиатуры: {0}",
            ["HotkeysRegistered"] = "Зарегистрированы 3 общих бинда. Целей: {0}",
            ["HotkeyRegisterFailed"] = "Не удалось зарегистрировать хоткей {0}: {1} (Win32 {2})",
            ["AutostartFailed"] = "Не удалось обновить автозапуск: {0}",
            ["RefreshError"] = "Ошибка обновления аудиосессий: {0}",
            ["VolumeError"] = "Ошибка изменения громкости {0}: {1}",
            ["HardwareError"] = "Ошибка специальной кнопки управления звуком: {0}",
            ["ActionUp"] = "Увеличение",
            ["ActionDown"] = "Уменьшение",
            ["ActionMuteOn"] = "Mute включен",
            ["ActionMuteOff"] = "Mute выключен",
            ["Action"] = "Действие",
            ["HotkeyNone"] = "Не задано",
            ["HotkeyPrompt"] = "Нажмите модификатор + клавишу"
        },
        ["en"] = new()
        {
            ["Language"] = "Language:",
            ["AddProcess"] = "Add process:",
            ["VolumeStep"] = "Volume step:",
            ["Refresh"] = "Refresh list",
            ["AddToTargets"] = "Add to targets",
            ["StartWithWindows"] = "Start with Windows minimized to tray",
            ["HardwareVolume"] = "Special volume control button controls the active checked app",
            ["LogKeys"] = "Log keys",
            ["HotkeyUp"] = "Increase all targets:",
            ["HotkeyDown"] = "Decrease all targets:",
            ["HotkeyMute"] = "Mute all targets:",
            ["Targets"] = "Targets for the three hotkeys:",
            ["Process"] = "Process",
            ["Volume"] = "Volume",
            ["SessionName"] = "Session name",
            ["Open"] = "Open",
            ["RefreshSessions"] = "Refresh sessions",
            ["CheckUpdates"] = "Check for updates",
            ["About"] = "About",
            ["AboutText"] = "App Volume Hotkeys\nAuthor: Saba\nLicense: MIT",
            ["UpdateAvailableTitle"] = "Update available",
            ["UpdateAvailableText"] = "A new version {0} is available. Current version is {1}.\n\nOpen the release page?",
            ["InstallUpdateText"] = "A new version {0} is available. Current version is {1}.\n\nDownload and install it now? The app will restart automatically.",
            ["DownloadingUpdate"] = "Downloading update... {0}%",
            ["UpdateInstallFailed"] = "Could not install update: {0}",
            ["NoUpdateTitle"] = "Updates",
            ["NoUpdateText"] = "You are running the latest version {0}.",
            ["UpdateCheckFailed"] = "Could not check for updates: {0}",
            ["Exit"] = "Exit",
            ["TrayMessage"] = "The app keeps running in the tray.",
            ["NoTargets"] = "No targets",
            ["NoTargetsDetail"] = "No apps selected",
            ["NoTargetsStatus"] = "Check at least one target app first.",
            ["NoSession"] = "Session not found",
            ["NoSessionStatus"] = "The selected apps have no active audio sessions.",
            ["FoundSessions"] = "Audio sessions: {0}. Targets: {1}",
            ["AddedTarget"] = "Added to targets: {0}",
            ["KeyboardLog"] = "Keyboard log: {0}",
            ["HotkeysRegistered"] = "Registered 3 shared hotkeys. Targets: {0}",
            ["HotkeyRegisterFailed"] = "Could not register {0} hotkey: {1} (Win32 {2})",
            ["AutostartFailed"] = "Could not update autostart: {0}",
            ["RefreshError"] = "Could not refresh audio sessions: {0}",
            ["VolumeError"] = "Could not change volume for {0}: {1}",
            ["HardwareError"] = "Special volume control button error: {0}",
            ["HardwareCalibrationTitle"] = "Special volume control button setup",
            ["HardwareCalibrationIntro"] = "Keyboard logging will be enabled temporarily. The app will ask you to use volume up, volume down, and mute actions so it can identify the button codes.",
            ["HardwareCalibrationStep"] = "After pressing OK, use this action a few times: {0}",
            ["HardwareCalibrationFound"] = "Detected {0}: {1} (VK 0x{2:X2}).",
            ["HardwareCalibrationNotFound"] = "Could not detect {0}. Standard Windows volume key will still be used if available.",
            ["HardwareCalibrationDone"] = "Special volume control setup is complete.",
            ["ActionUp"] = "Volume up",
            ["ActionDown"] = "Volume down",
            ["ActionMuteOn"] = "Mute on",
            ["ActionMuteOff"] = "Mute off",
            ["Action"] = "Action",
            ["HotkeyNone"] = "Not set",
            ["HotkeyPrompt"] = "Press modifier + key"
        },
        ["zh"] = new()
        {
            ["Language"] = "语言:",
            ["AddProcess"] = "添加进程:",
            ["VolumeStep"] = "音量步进:",
            ["Refresh"] = "刷新列表",
            ["AddToTargets"] = "添加到目标",
            ["StartWithWindows"] = "随 Windows 启动并最小化到托盘",
            ["HardwareVolume"] = "专用音量控制按钮控制当前已勾选应用",
            ["LogKeys"] = "记录按键",
            ["HotkeyUp"] = "提高所有目标音量:",
            ["HotkeyDown"] = "降低所有目标音量:",
            ["HotkeyMute"] = "静音所有目标:",
            ["Targets"] = "三个快捷键的目标:",
            ["Process"] = "进程",
            ["Volume"] = "音量",
            ["SessionName"] = "会话名称",
            ["Open"] = "打开",
            ["RefreshSessions"] = "刷新会话",
            ["CheckUpdates"] = "检查更新",
            ["About"] = "关于",
            ["AboutText"] = "App Volume Hotkeys\n作者: Saba\n许可证: MIT",
            ["UpdateAvailableTitle"] = "有可用更新",
            ["UpdateAvailableText"] = "新版本 {0} 可用。当前版本为 {1}。\n\n打开发布页面？",
            ["NoUpdateTitle"] = "更新",
            ["NoUpdateText"] = "当前已是最新版本 {0}。",
            ["UpdateCheckFailed"] = "无法检查更新: {0}",
            ["Exit"] = "退出",
            ["TrayMessage"] = "应用仍在托盘中运行。",
            ["NoTargets"] = "没有目标",
            ["NoTargetsDetail"] = "未选择应用",
            ["NoTargetsStatus"] = "请先勾选至少一个目标应用。",
            ["NoSession"] = "未找到会话",
            ["NoSessionStatus"] = "所选应用没有活动音频会话。",
            ["FoundSessions"] = "音频会话: {0}. 目标: {1}",
            ["AddedTarget"] = "已添加到目标: {0}",
            ["KeyboardLog"] = "键盘日志: {0}",
            ["HotkeysRegistered"] = "已注册 3 个通用快捷键。目标: {0}",
            ["HotkeyRegisterFailed"] = "无法注册 {0} 快捷键: {1} (Win32 {2})",
            ["AutostartFailed"] = "无法更新开机启动: {0}",
            ["RefreshError"] = "无法刷新音频会话: {0}",
            ["VolumeError"] = "无法更改 {0} 的音量: {1}",
            ["HardwareError"] = "专用音量控制按钮错误: {0}",
            ["ActionUp"] = "音量提高",
            ["ActionDown"] = "音量降低",
            ["ActionMuteOn"] = "已静音",
            ["ActionMuteOff"] = "已取消静音",
            ["Action"] = "操作",
            ["HotkeyNone"] = "未设置",
            ["HotkeyPrompt"] = "按下修饰键 + 按键"
        },
        ["de"] = new()
        {
            ["Language"] = "Sprache:",
            ["AddProcess"] = "Prozess hinzufügen:",
            ["VolumeStep"] = "Lautstärkeschritt:",
            ["Refresh"] = "Liste aktualisieren",
            ["AddToTargets"] = "Zu Zielen hinzufügen",
            ["StartWithWindows"] = "Mit Windows minimiert im Infobereich starten",
            ["HardwareVolume"] = "Spezielle Lautstärketaste steuert die aktive markierte App",
            ["LogKeys"] = "Tasten protokollieren",
            ["HotkeyUp"] = "Alle Ziele lauter:",
            ["HotkeyDown"] = "Alle Ziele leiser:",
            ["HotkeyMute"] = "Alle Ziele stummschalten:",
            ["Targets"] = "Ziele für die drei Hotkeys:",
            ["Process"] = "Prozess",
            ["Volume"] = "Lautstärke",
            ["SessionName"] = "Sitzungsname",
            ["Open"] = "Öffnen",
            ["RefreshSessions"] = "Sitzungen aktualisieren",
            ["CheckUpdates"] = "Nach Updates suchen",
            ["About"] = "Über",
            ["AboutText"] = "App Volume Hotkeys\nAutor: Saba\nLizenz: MIT",
            ["UpdateAvailableTitle"] = "Update verfügbar",
            ["UpdateAvailableText"] = "Eine neue Version {0} ist verfügbar. Aktuelle Version: {1}.\n\nRelease-Seite öffnen?",
            ["NoUpdateTitle"] = "Updates",
            ["NoUpdateText"] = "Du verwendest die neueste Version {0}.",
            ["UpdateCheckFailed"] = "Update-Prüfung fehlgeschlagen: {0}",
            ["Exit"] = "Beenden",
            ["TrayMessage"] = "Die App läuft im Infobereich weiter.",
            ["NoTargets"] = "Keine Ziele",
            ["NoTargetsDetail"] = "Keine Apps ausgewählt",
            ["NoTargetsStatus"] = "Markiere zuerst mindestens eine Ziel-App.",
            ["NoSession"] = "Sitzung nicht gefunden",
            ["NoSessionStatus"] = "Die ausgewählten Apps haben keine aktiven Audiositzungen.",
            ["FoundSessions"] = "Audiositzungen: {0}. Ziele: {1}",
            ["AddedTarget"] = "Zu Zielen hinzugefügt: {0}",
            ["KeyboardLog"] = "Tastaturprotokoll: {0}",
            ["HotkeysRegistered"] = "3 gemeinsame Hotkeys registriert. Ziele: {0}",
            ["HotkeyRegisterFailed"] = "Hotkey {0} konnte nicht registriert werden: {1} (Win32 {2})",
            ["AutostartFailed"] = "Autostart konnte nicht aktualisiert werden: {0}",
            ["RefreshError"] = "Audiositzungen konnten nicht aktualisiert werden: {0}",
            ["VolumeError"] = "Lautstärke für {0} konnte nicht geändert werden: {1}",
            ["HardwareError"] = "Fehler der speziellen Lautstärketaste: {0}",
            ["ActionUp"] = "Lauter",
            ["ActionDown"] = "Leiser",
            ["ActionMuteOn"] = "Stumm ein",
            ["ActionMuteOff"] = "Stumm aus",
            ["Action"] = "Aktion",
            ["HotkeyNone"] = "Nicht gesetzt",
            ["HotkeyPrompt"] = "Modifikator + Taste drücken"
        },
        ["es"] = new()
        {
            ["Language"] = "Idioma:",
            ["AddProcess"] = "Añadir proceso:",
            ["VolumeStep"] = "Paso de volumen:",
            ["Refresh"] = "Actualizar lista",
            ["AddToTargets"] = "Añadir a objetivos",
            ["StartWithWindows"] = "Iniciar con Windows minimizado en la bandeja",
            ["HardwareVolume"] = "Botón especial de volumen controla la app activa marcada",
            ["LogKeys"] = "Registrar teclas",
            ["HotkeyUp"] = "Subir todos los objetivos:",
            ["HotkeyDown"] = "Bajar todos los objetivos:",
            ["HotkeyMute"] = "Silenciar todos los objetivos:",
            ["Targets"] = "Objetivos para los tres atajos:",
            ["Process"] = "Proceso",
            ["Volume"] = "Volumen",
            ["SessionName"] = "Nombre de sesión",
            ["Open"] = "Abrir",
            ["RefreshSessions"] = "Actualizar sesiones",
            ["CheckUpdates"] = "Buscar actualizaciones",
            ["About"] = "Acerca de",
            ["AboutText"] = "App Volume Hotkeys\nAutor: Saba\nLicencia: MIT",
            ["UpdateAvailableTitle"] = "Actualización disponible",
            ["UpdateAvailableText"] = "Hay una nueva versión {0}. La versión actual es {1}.\n\n¿Abrir la página del lanzamiento?",
            ["NoUpdateTitle"] = "Actualizaciones",
            ["NoUpdateText"] = "Tienes la última versión {0}.",
            ["UpdateCheckFailed"] = "No se pudo buscar actualizaciones: {0}",
            ["Exit"] = "Salir",
            ["TrayMessage"] = "La app sigue ejecutándose en la bandeja.",
            ["NoTargets"] = "Sin objetivos",
            ["NoTargetsDetail"] = "No hay apps seleccionadas",
            ["NoTargetsStatus"] = "Marca primero al menos una app objetivo.",
            ["NoSession"] = "Sesión no encontrada",
            ["NoSessionStatus"] = "Las apps seleccionadas no tienen sesiones de audio activas.",
            ["FoundSessions"] = "Sesiones de audio: {0}. Objetivos: {1}",
            ["AddedTarget"] = "Añadido a objetivos: {0}",
            ["KeyboardLog"] = "Registro de teclado: {0}",
            ["HotkeysRegistered"] = "3 atajos compartidos registrados. Objetivos: {0}",
            ["HotkeyRegisterFailed"] = "No se pudo registrar el atajo {0}: {1} (Win32 {2})",
            ["AutostartFailed"] = "No se pudo actualizar el inicio automático: {0}",
            ["RefreshError"] = "No se pudieron actualizar las sesiones de audio: {0}",
            ["VolumeError"] = "No se pudo cambiar el volumen de {0}: {1}",
            ["HardwareError"] = "Error del botón especial de volumen: {0}",
            ["ActionUp"] = "Subir volumen",
            ["ActionDown"] = "Bajar volumen",
            ["ActionMuteOn"] = "Silencio activado",
            ["ActionMuteOff"] = "Silencio desactivado",
            ["Action"] = "Acción",
            ["HotkeyNone"] = "Sin asignar",
            ["HotkeyPrompt"] = "Pulsa modificador + tecla"
        },
        ["fr"] = new()
        {
            ["Language"] = "Langue :",
            ["AddProcess"] = "Ajouter un processus :",
            ["VolumeStep"] = "Pas de volume :",
            ["Refresh"] = "Actualiser la liste",
            ["AddToTargets"] = "Ajouter aux cibles",
            ["StartWithWindows"] = "Démarrer avec Windows réduit dans la zone de notification",
            ["HardwareVolume"] = "Le bouton spécial de volume contrôle l'app active cochée",
            ["LogKeys"] = "Journaliser les touches",
            ["HotkeyUp"] = "Augmenter toutes les cibles :",
            ["HotkeyDown"] = "Réduire toutes les cibles :",
            ["HotkeyMute"] = "Muet pour toutes les cibles :",
            ["Targets"] = "Cibles pour les trois raccourcis :",
            ["Process"] = "Processus",
            ["Volume"] = "Volume",
            ["SessionName"] = "Nom de session",
            ["Open"] = "Ouvrir",
            ["RefreshSessions"] = "Actualiser les sessions",
            ["CheckUpdates"] = "Rechercher des mises à jour",
            ["About"] = "À propos",
            ["AboutText"] = "App Volume Hotkeys\nAuteur : Saba\nLicence : MIT",
            ["UpdateAvailableTitle"] = "Mise à jour disponible",
            ["UpdateAvailableText"] = "Une nouvelle version {0} est disponible. Version actuelle : {1}.\n\nOuvrir la page de publication ?",
            ["NoUpdateTitle"] = "Mises à jour",
            ["NoUpdateText"] = "Vous utilisez la dernière version {0}.",
            ["UpdateCheckFailed"] = "Impossible de vérifier les mises à jour : {0}",
            ["Exit"] = "Quitter",
            ["TrayMessage"] = "L'application continue dans la zone de notification.",
            ["NoTargets"] = "Aucune cible",
            ["NoTargetsDetail"] = "Aucune app sélectionnée",
            ["NoTargetsStatus"] = "Coche d'abord au moins une app cible.",
            ["NoSession"] = "Session introuvable",
            ["NoSessionStatus"] = "Les apps sélectionnées n'ont pas de session audio active.",
            ["FoundSessions"] = "Sessions audio : {0}. Cibles : {1}",
            ["AddedTarget"] = "Ajouté aux cibles : {0}",
            ["KeyboardLog"] = "Journal clavier : {0}",
            ["HotkeysRegistered"] = "3 raccourcis partagés enregistrés. Cibles : {0}",
            ["HotkeyRegisterFailed"] = "Impossible d'enregistrer le raccourci {0} : {1} (Win32 {2})",
            ["AutostartFailed"] = "Impossible de mettre à jour le démarrage automatique : {0}",
            ["RefreshError"] = "Impossible d'actualiser les sessions audio : {0}",
            ["VolumeError"] = "Impossible de changer le volume de {0} : {1}",
            ["HardwareError"] = "Erreur du bouton spécial de volume : {0}",
            ["ActionUp"] = "Volume +",
            ["ActionDown"] = "Volume -",
            ["ActionMuteOn"] = "Muet activé",
            ["ActionMuteOff"] = "Muet désactivé",
            ["Action"] = "Action",
            ["HotkeyNone"] = "Non défini",
            ["HotkeyPrompt"] = "Appuie sur modificateur + touche"
        },
        ["pt"] = new()
        {
            ["Language"] = "Idioma:",
            ["AddProcess"] = "Adicionar processo:",
            ["VolumeStep"] = "Passo de volume:",
            ["Refresh"] = "Atualizar lista",
            ["AddToTargets"] = "Adicionar aos alvos",
            ["StartWithWindows"] = "Iniciar com o Windows minimizado na bandeja",
            ["HardwareVolume"] = "Botão especial de volume controla o app ativo marcado",
            ["LogKeys"] = "Registrar teclas",
            ["HotkeyUp"] = "Aumentar todos os alvos:",
            ["HotkeyDown"] = "Diminuir todos os alvos:",
            ["HotkeyMute"] = "Silenciar todos os alvos:",
            ["Targets"] = "Alvos para os três atalhos:",
            ["Process"] = "Processo",
            ["Volume"] = "Volume",
            ["SessionName"] = "Nome da sessão",
            ["Open"] = "Abrir",
            ["RefreshSessions"] = "Atualizar sessões",
            ["CheckUpdates"] = "Verificar atualizações",
            ["About"] = "Sobre",
            ["AboutText"] = "App Volume Hotkeys\nAutor: Saba\nLicença: MIT",
            ["UpdateAvailableTitle"] = "Atualização disponível",
            ["UpdateAvailableText"] = "Uma nova versão {0} está disponível. Versão atual: {1}.\n\nAbrir a página da versão?",
            ["NoUpdateTitle"] = "Atualizações",
            ["NoUpdateText"] = "Você está usando a versão mais recente {0}.",
            ["UpdateCheckFailed"] = "Não foi possível verificar atualizações: {0}",
            ["Exit"] = "Sair",
            ["TrayMessage"] = "O app continua em execução na bandeja.",
            ["NoTargets"] = "Sem alvos",
            ["NoTargetsDetail"] = "Nenhum app selecionado",
            ["NoTargetsStatus"] = "Marque pelo menos um app alvo primeiro.",
            ["NoSession"] = "Sessão não encontrada",
            ["NoSessionStatus"] = "Os apps selecionados não têm sessões de áudio ativas.",
            ["FoundSessions"] = "Sessões de áudio: {0}. Alvos: {1}",
            ["AddedTarget"] = "Adicionado aos alvos: {0}",
            ["KeyboardLog"] = "Log do teclado: {0}",
            ["HotkeysRegistered"] = "3 atalhos compartilhados registrados. Alvos: {0}",
            ["HotkeyRegisterFailed"] = "Não foi possível registrar o atalho {0}: {1} (Win32 {2})",
            ["AutostartFailed"] = "Não foi possível atualizar o início automático: {0}",
            ["RefreshError"] = "Não foi possível atualizar as sessões de áudio: {0}",
            ["VolumeError"] = "Não foi possível alterar o volume de {0}: {1}",
            ["HardwareError"] = "Erro do botão especial de volume: {0}",
            ["ActionUp"] = "Aumentar volume",
            ["ActionDown"] = "Diminuir volume",
            ["ActionMuteOn"] = "Mudo ativado",
            ["ActionMuteOff"] = "Mudo desativado",
            ["Action"] = "Ação",
            ["HotkeyNone"] = "Não definido",
            ["HotkeyPrompt"] = "Pressione modificador + tecla"
        }
    };

    private static readonly Dictionary<string, string> EnglishAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["es"] = "es",
        ["fr"] = "fr",
        ["pt"] = "pt",
        ["ja"] = "ja",
        ["ko"] = "ko"
    };

    public static string CurrentLanguage { get; private set; } = "en";

    public static IReadOnlyList<LanguageOption> Options { get; } =
    [
        new("system", "System / Системный"),
        new("en", "English"),
        new("ru", "Русский"),
        new("zh", "中文"),
        new("de", "Deutsch"),
        new("es", "Español"),
        new("fr", "Français"),
        new("pt", "Português")
    ];

    public static void SetLanguage(string code)
    {
        CurrentLanguage = ResolveLanguage(code);
    }

    public static string ResolveLanguage(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Equals("system", StringComparison.OrdinalIgnoreCase))
        {
            code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        }

        if (Languages.ContainsKey(code))
        {
            return code;
        }

        return EnglishAliases.ContainsKey(code) ? "en" : "en";
    }

    public static string T(string key)
    {
        if (Languages.TryGetValue(CurrentLanguage, out var language) && language.TryGetValue(key, out var value))
        {
            return value;
        }

        return Languages["en"].TryGetValue(key, out var fallback) ? fallback : key;
    }

    public static string Format(string key, params object[] args)
    {
        return string.Format(CultureInfo.CurrentCulture, T(key), args);
    }

    public static string AppCount(int count)
    {
        return CurrentLanguage switch
        {
            "ru" => $"{count} {RussianAppWord(count)}",
            "zh" => $"{count} 个应用",
            "de" => count == 1 ? "1 App" : $"{count} Apps",
            "es" => count == 1 ? "1 aplicación" : $"{count} aplicaciones",
            "fr" => count == 1 ? "1 application" : $"{count} applications",
            "pt" => count == 1 ? "1 aplicativo" : $"{count} aplicativos",
            _ => count == 1 ? "1 app" : $"{count} apps"
        };
    }

    private static string RussianAppWord(int count)
    {
        var mod100 = count % 100;
        if (mod100 is >= 11 and <= 14)
        {
            return "приложений";
        }

        return (count % 10) switch
        {
            1 => "приложение",
            >= 2 and <= 4 => "приложения",
            _ => "приложений"
        };
    }
}
