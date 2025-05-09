class_name ConfigData

var configPath : String = Program.ExeDir + "emuera.config"
const configdebugPath : String = Program.DebugDir + "debug.config";

func _init() -> void:
    setDefault()

static var instance : ConfigData  = new()
var Instance:
    get: return instance

var configArray : Array[AConfigItem] = [];
var replaceArray : Array[AConfigItem] = [];
var debugArray : Array[AConfigItem] = [];

func setDefault() -> void:
    configArray.append(ConfigItem.new(ConfigCode.IgnoreCase, "大文字小文字の違いを無視する", "Ignore case", true));
    configArray.append(ConfigItem.new(ConfigCode.UseRenameFile, "_Rename.csvを利用する", "Use _Rename.csv file", false));
    configArray.append(ConfigItem.new(ConfigCode.UseReplaceFile, "_Replace.csvを利用する", "Use _Replace.csv file", true));
    configArray.append(ConfigItem.new(ConfigCode.UseMouse, "マウスを使用する", "Use mouse", true));
    configArray.append(ConfigItem.new(ConfigCode.UseMenu, "メニューを使用する", "Show menu", true));
    configArray.append(ConfigItem.new(ConfigCode.UseDebugCommand, "デバッグコマンドを使用する", "Allow debug commands", false));
    configArray.append(ConfigItem.new(ConfigCode.AllowMultipleInstances, "多重起動を許可する", "Allow multiple instances", true));
    configArray.append(ConfigItem.new(ConfigCode.AutoSave, "オートセーブを行なう", "Make autosaves", true));
    configArray.append(ConfigItem.new(ConfigCode.UseKeyMacro, "キーボードマクロを使用する", "Use keyboard macros", true));
    configArray.append(ConfigItem.new(ConfigCode.SizableWindow, "ウィンドウの高さを可変にする", "Changeable window height", true))
    configArray.append(ConfigItem.new(ConfigCode.TextDrawingMode, "描画インターフェース", "Drawing interface", TextDrawingMode.TEXTRENDERER))

    configArray.append(ConfigItem.new(ConfigCode.WindowX, "ウィンドウ幅", "Window width", 760))
    configArray.append(ConfigItem.new(ConfigCode.WindowY, "ウィンドウ高さ", "Window height", 480));
    configArray.append(ConfigItem.new(ConfigCode.WindowPosX, "ウィンドウ位置X", "Window X position", 0));
    configArray.append(ConfigItem.new(ConfigCode.WindowPosY, "ウィンドウ位置Y", "Window Y position", 0));
    configArray.append(ConfigItem.new(ConfigCode.SetWindowPos, "起動時のウィンドウ位置を指定する", "Fixed window starting position", false));
    configArray.append(ConfigItem.new(ConfigCode.WindowMaximixed, "起動時にウィンドウを最大化する", "Maximize window on startup", false));
    configArray.append(ConfigItem.new(ConfigCode.MaxLog, "履歴ログの行数", "Max history log lines", 5000));
    configArray.append(ConfigItem.new(ConfigCode.PrintCPerLine, "PRINTCを並べる数", "Items per line for PRINTC", 3));
    configArray.append(ConfigItem.new(ConfigCode.PrintCLength, "PRINTCの文字数", "Number of Item characters for PRINTC", 25));
    configArray.append(ConfigItem.new(ConfigCode.FontName, "フォント名", "Font name", "ＭＳ ゴシック"));
    configArray.append(ConfigItem.new(ConfigCode.FontSize, "フォントサイズ", "Font size", 18));
    configArray.append(ConfigItem.new(ConfigCode.LineHeight, "一行の高さ", "Line height", 19));
    configArray.append(ConfigItem.new(ConfigCode.ForeColor, "文字色", "Text color", Color.new(192, 192, 192)))
    configArray.append(ConfigItem.new(ConfigCode.BackColor, "背景色", "Background color", Color.new(0, 0, 0)))
    configArray.append(ConfigItem.new(ConfigCode.FocusColor, "選択中文字色", "Highlight color", Color.new(255, 255, 0)))
    configArray.append(ConfigItem.new(ConfigCode.LogColor, "履歴文字色", "History log color", Color.new(192, 192, 192)))
    configArray.append(ConfigItem.new(ConfigCode.FPS, "フレーム毎秒", "FPS", 5));
    configArray.append(ConfigItem.new(ConfigCode.SkipFrame, "最大スキップフレーム数", "Skip frames", 3))
    configArray.append(ConfigItem.new(ConfigCode.ScrollHeight, "スクロール行数", "Lines per scroll", 1))
    configArray.append(ConfigItem.new(ConfigCode.InfiniteLoopAlertTime, "無限ループ警告までのミリ秒数", "Milliseconds for infinite loop warning", 5000))
    configArray.append(ConfigItem.new(ConfigCode.DisplayWarningLevel, "表示する最低警告レベル", "Minimum warning level", 1))
    configArray.append(ConfigItem.new(ConfigCode.DisplayReport, "ロード時にレポートを表示する", "Display loading report", false));
    configArray.append(ConfigItem.new(ConfigCode.ReduceArgumentOnLoad, "ロード時に引数を解析する", "Reduce argument on load", ReduceArgumentOnLoadFlag.NO));

    configArray.append(ConfigItem.new(ConfigCode.IgnoreUncalledFunction, "呼び出されなかった関数を無視する", "Ignore uncalled functions", true));
    configArray.append(ConfigItem.new(ConfigCode.FunctionNotFoundWarning, "関数が見つからない警告の扱い", "Function is not found warning", DisplayWarningFlag.IGNORE));
    configArray.append(ConfigItem.new(ConfigCode.FunctionNotCalledWarning, "関数が呼び出されなかった警告の扱い", "Function not called warning", DisplayWarningFlag.IGNORE));


    configArray.append(ConfigItem.new(ConfigCode.ChangeMasterNameIfDebug, "デバッグコマンドを使用した時にMASTERの名前を変更する", "Change MASTER mame in debug", true));
    configArray.append(ConfigItem.new(ConfigCode.ButtonWrap, "ボタンの途中で行を折りかえさない", "Button wrapping", false));
    configArray.append(ConfigItem.new(ConfigCode.SearchSubdirectory, "サブディレクトリを検索する", "Search subfolders", false));
    configArray.append(ConfigItem.new(ConfigCode.SortWithFilename, "読み込み順をファイル名順にソートする", "Sort filenames", false));
    configArray.append(ConfigItem.new(ConfigCode.LastKey, "最終更新コード", "Latest identify code", 0));
    configArray.append(ConfigItem.new(ConfigCode.SaveDataNos, "表示するセーブデータ数", "Save data count per page", 20));
    configArray.append(ConfigItem.new(ConfigCode.WarnBackCompatibility, "eramaker互換性に関する警告を表示する", "Eramaker compatibility warning", true));
    configArray.append(ConfigItem.new(ConfigCode.AllowFunctionOverloading, "システム関数の上書きを許可する", "Allow overriding system functions", true));
    configArray.append(ConfigItem.new(ConfigCode.WarnFunctionOverloading, "システム関数が上書きされたとき警告を表示する", "System function override warning", true));
    configArray.append(ConfigItem.new(ConfigCode.TextEditor, "関連づけるテキストエディタ", "Text editor", "notepad"));
    configArray.append(ConfigItem.new(ConfigCode.EditorType, "テキストエディタコマンドライン指定", "Text editor command line setting", TextEditorType.USER_SETTING));
    configArray.append(ConfigItem.new(ConfigCode.EditorArgument, "エディタに渡す行指定引数", "Text editor command line arguments", ""));
    configArray.append(ConfigItem.new(ConfigCode.WarnNormalFunctionOverloading, "同名の非イベント関数が複数定義されたとき警告する", "Duplicated functions warning", false));
    configArray.append(ConfigItem.new(ConfigCode.CompatiErrorLine, "解釈不可能な行があっても実行する", "Execute error lines", false));
    configArray.append(ConfigItem.new(ConfigCode.CompatiCALLNAME, "CALLNAMEが空文字列の時にNAMEを代入する", "Use NAME if CALLNAME is empty", false));
    configArray.append(ConfigItem.new(ConfigCode.UseSaveFolder, "セーブデータをsavフォルダ内に作成する", "Use sav folder", false));
    configArray.append(ConfigItem.new(ConfigCode.CompatiRAND, "擬似変数RANDの仕様をeramakerに合わせる", "Imitate behavior for RAND", false));
    configArray.append(ConfigItem.new(ConfigCode.CompatiDRAWLINE, "DRAWLINEを常に新しい行で行う", "Always start DRAWLINE in a new line", false));
    configArray.append(ConfigItem.new(ConfigCode.CompatiFunctionNoignoreCase, "関数・属性については大文字小文字を無視しない", "Do not ignore case for functions and attributes", false));
    configArray.append(ConfigItem.new(ConfigCode.SystemAllowFullSpace, "全角スペースをホワイトスペースに含める", "Whitespace includes full-width space", true));
    configArray.append(ConfigItem.new(ConfigCode.CompatiLinefeedAs1739, "ver1739以前の非ボタン折り返しを再現する", "Reproduce wrapping behavior like in pre ver1739", false));
    configArray.append(ConfigItem.new(ConfigCode.useLanguage, "内部で使用する東アジア言語", "Default ANSI encoding", UseLanguage.JAPANESE));
    configArray.append(ConfigItem.new(ConfigCode.AllowLongInputByMouse, "ONEINPUT系命令でマウスによる2文字以上の入力を許可する", "Allow long input by mouse for ONEINPUT", false));
    configArray.append(ConfigItem.new(ConfigCode.CompatiCallEvent, "イベント関数のCALLを許可する", "Allow CALL on event functions", false));
    configArray.append(ConfigItem.new(ConfigCode.CompatiSPChara, "SPキャラを使用する", "Allow SP characters", false));

    configArray.append(ConfigItem.new(ConfigCode.SystemSaveInBinary, "セーブデータをバイナリ形式で保存する", "Use the binary format for saving data", false));
    configArray.append(ConfigItem.new(ConfigCode.CompatiFuncArgOptional, "ユーザー関数の全ての引数の省略を許可する", "Allow arguments omission for user functions", false));
    configArray.append(ConfigItem.new(ConfigCode.CompatiFuncArgAutoConvert, "ユーザー関数の引数に自動的にTOSTRを補完する", "Auto TOSTR conversion for user function arguments", false));
    configArray.append(ConfigItem.new(ConfigCode.SystemIgnoreTripleSymbol, "FORM中の三連記号を展開しない", "Do not process triple symbols inside FORM", false));
    configArray.append(ConfigItem.new(ConfigCode.TimesNotRigorousCalculation, "TIMESの計算をeramakerにあわせる", "Imitate behavior for TIMES", false));

    configArray.append(ConfigItem.new(ConfigCode.SystemNoTarget, "キャラクタ変数の引数を補完しない", "Do not auto-complete arguments for character variables", false));
    configArray.append(ConfigItem.new(ConfigCode.SystemIgnoreStringSet, "文字列変数の代入に文字列式を強制する", "String variable assignment on valid with string expression", false));

    configArray.append(ConfigItem.new(ConfigCode.ForbidUpdateCheck, "UPDATECHECKを許可しない", "Disallow UPDATECHECK", false));
    configArray.append(ConfigItem.new(ConfigCode.UseERD, "ERD機能を利用する", "Use ERD", true));
    configArray.append(ConfigItem.new(ConfigCode.VarsizeDimConfig, "VARSIZEの次元指定をERD機能に合わせる", "Imitate ERD to VARSIZE dimension specification", false));
    configArray.append(ConfigItem.new(ConfigCode.CheckDuplicateIdentifier, "ERDで定義した識別子とローカル変数の重複を確認する", "Check duplicate ERD identifier and private variablea", false));
    configArray.append(ConfigItem.new(ConfigCode.ReplaceContinuationBR, "行連結の改行コードの置換文字列", "String of replacing new line code inside continuation", "\" \""));
    configArray.append(ConfigItem.new(ConfigCode.ValidExtension, "LOADTEXTとSAVETEXTで使える拡張子", "Valid extensions for LOADTEXT and SAVETEXT", PackedStringArray.new(["txt"])));
    configArray.append(ConfigItem.new(ConfigCode.ZipSaveData, "セーブデータを圧縮して保存する", "Compress save data", false));
    configArray.append(ConfigItem.new(ConfigCode.EnglishConfigOutput, "CONFIGファイルの内容を英語で保存する", "Output English items in the config file", false));
    configArray.append(ConfigItem.new(ConfigCode.EmueraLang, "Emueraの表示言語", "Emuera interface language", string.Empty));
    configArray.append(ConfigItem.new(ConfigCode.EmueraIcon, "Emueraのアイコンのパス", "Path to a custom window icon", string.Empty));
    configArray.append(ConfigItem.new(ConfigCode.CBUseClipboard, "表示したテキストをクリップボードにコピーする", "Clipboard- Copy text to Clipboard during Game", false));
    configArray.append(ConfigItem.new(ConfigCode.CBIgnoreTags, "テキスト中の<>タグを無視する", "Clipboard- ignore <> tags in text", false));
    configArray.append(ConfigItem.new(ConfigCode.CBReplaceTags, "<>を次の文で置き換える", "Clipboard- Replace <> with this", "."));
    configArray.append(ConfigItem.new(ConfigCode.CBNewLinesOnly, "新しい行のみコピーする", "Clipboard- Show new lines only", true));
    configArray.append(ConfigItem.new(ConfigCode.CBClearBuffer, "画面のリフレッシュ時にクリップボードとバッファを消去する", "Clipboard- Clear Buffer when game clears screen", false));
    configArray.append(ConfigItem.new(ConfigCode.CBTriggerLeftClick, "左クリックをトリガーにする", "Clipboard- LeftClick Trigger", true));
    configArray.append(ConfigItem.new(ConfigCode.CBTriggerMiddleClick, "ホイールクリックをトリガーにする", "Clipboard- MiddleClick Trigger", false));
    configArray.append(ConfigItem.new(ConfigCode.CBTriggerDoubleLeftClick, "ダブルクリックをトリガーにする", "Clipboard- Double Left Click Trigger", false));
    configArray.append(ConfigItem.new(ConfigCode.CBTriggerAnyKeyWait, "WAITをトリガーにする", "Clipboard- AnyKey Wait Trigger ", false));
    configArray.append(ConfigItem.new(ConfigCode.CBTriggerInputWait, "INPUTをトリガーにする", "Clipboard- Wait for Input Trigger", true));
    configArray.append(ConfigItem.new(ConfigCode.CBMaxCB, "クリップボードに貼り付ける行数", "Clipboard- Length of Clipboard", 25));
    configArray.append(ConfigItem.new(ConfigCode.CBBufferSize, "総バッファサイズ", "Clipboard- Buffer Size", 300));
    configArray.append(ConfigItem.new(ConfigCode.CBScrollCount, "スクロールの行数", "Clipboard- Scrolled Lines per Key", 5));
    configArray.append(ConfigItem.new(ConfigCode.CBMinTimer, "クリップボードの更新間隔(ミリ秒)", "Clipboard- min time between pastes", 800));
    configArray.append(ConfigItem.new(ConfigCode.RikaiEnabled, "Rikaichanを使用する", "Rikai- Enabled", false));
    configArray.append(ConfigItem.new(ConfigCode.RikaiFilename, "Rikaichanのファイルパス", "Rikai- Dictionary Filename", "Emuera-Rikai-edict.txt-eucjp"));
    configArray.append(ConfigItem.new(ConfigCode.RikaiColorBack, "ポップアップの背景色", "Rikai- Back Color", Color.new(0, 0, 0x8B)))
    configArray.append(ConfigItem.new(ConfigCode.RikaiColorText, "ポップアップの文字色", "Rikai- Text Color", Color.new(0xFF, 0xFF, 0xFF)))
    configArray.append(ConfigItem.new(ConfigCode.RikaiUseSeparateBoxes, "翻訳中の語句を強調表示する", "Rikai- Use Separate Boxes", true));

    configArray.append(ConfigItem.new(ConfigCode.Ctrl_Z_Enabled, "Ctrl-Zで元に戻す機能を有効にする", "Enable undo with ctrl-z", false));

    debugArray.Add(ConfigItem.new(ConfigCode.DebugShowWindow, "起動時にデバッグウインドウを表示する", "Show debug window on startup", true));
    debugArray.Add(ConfigItem.new(ConfigCode.DebugWindowTopMost, "デバッグウインドウを最前面に表示する", "Debug window always on top", true));
    debugArray.Add(ConfigItem.new(ConfigCode.DebugWindowWidth, "デバッグウィンドウ幅", "Debug window width", 400));
    debugArray.Add(ConfigItem.new(ConfigCode.DebugWindowHeight, "デバッグウィンドウ高さ", "Debug window height", 300));
    debugArray.Add(ConfigItem.new(ConfigCode.DebugSetWindowPos, "デバッグウィンドウ位置を指定する", "Fixed debug window starting position", false));
    debugArray.Add(ConfigItem.new(ConfigCode.DebugWindowPosX, "デバッグウィンドウ位置X", "Debug window X position", 0));
    debugArray.Add(ConfigItem.new(ConfigCode.DebugWindowPosY, "デバッグウィンドウ位置Y", "Debug window Y position", 0));

    replaceArray.Add(ConfigItem.new(ConfigCode.MoneyLabel, "お金の単位", "Currency symbol", "$"));
    replaceArray.Add(ConfigItem.new(ConfigCode.MoneyFirst, "単位の位置", "Currency symbol position", true));
    replaceArray.Add(ConfigItem.new(ConfigCode.LoadLabel, "起動時簡略表示", "Loading message", "Now Loading..."));
    replaceArray.Add(ConfigItem.new(ConfigCode.MaxShopItem, "販売アイテム数", "Max shop item storage", 100));
    replaceArray.Add(ConfigItem.new(ConfigCode.DrawLineString, "DRAWLINE文字", "DRAWLINE character", "-"));
    replaceArray.Add(ConfigItem.new(ConfigCode.BarChar1, "BAR文字1", "BAR character 1", '*'));
    replaceArray.Add(ConfigItem.new(ConfigCode.BarChar2, "BAR文字2", "BAR character 2", '.'));
    replaceArray.Add(ConfigItem.new(ConfigCode.TitleMenuString0, "システムメニュー0", "System menu 0", "最初からはじめる"));
    replaceArray.Add(ConfigItem.new(ConfigCode.TitleMenuString1, "システムメニュー1", "System menu 1", "ロードしてはじめる"));
    replaceArray.Add(ConfigItem.new(ConfigCode.ComAbleDefault, "COM_ABLE初期値", "Default COM_ABLE", 1));

    replaceArray.Add(ConfigItem.new(ConfigCode.StainDefault, "汚れの初期値", "Default Stain", PackedInt64Array([0, 0, 2, 1, 8])))
    replaceArray.Add(ConfigItem.new(ConfigCode.TimeupLabel, "時間切れ表示", "Time up message", "時間切れ"));
    replaceArray.Add(ConfigItem.new(ConfigCode.ExpLvDef, "EXPLVの初期値", "Default EXPLV", PackedInt64Array([0, 1, 4, 20, 50, 200])));

    replaceArray.Add(ConfigItem.new(ConfigCode.PalamLvDef, "PALAMLVの初期値", "Default PALAMLV", PackedInt64Array([0, 100, 500, 3000, 10000, 30000, 60000, 100000, 150000, 250000])));
    replaceArray.Add(ConfigItem.new(ConfigCode.pbandDef, "PBANDの初期値", "Default PBAND", 4));
    replaceArray.Add(ConfigItem.new(ConfigCode.RelationDef, "RELATIONの初期値", "Default RELATION", 0));

func Copy():
    var config := new();
    for i in configArray.size():
        if ((configArray[i] != null) && (config.configArray[i] != null)):
            configArray[i].CopyTo(config.configArray[i]);
    for i in debugArray.size():
        if ((debugArray[i] != null) && (config.debugArray[i] != null)):
            debugArray[i].CopyTo(config.debugArray[i]);
    for i in replaceArray.size():
        if ((replaceArray[i] != null) && (config.replaceArray[i] != null)):
            replaceArray[i].CopyTo(config.replaceArray[i]);
    return config;

func GetConfigNameDic() -> Dictionary[ConfigCode, string]:
    var ret : Dictionary[ConfigCode, string]
    for item : ConfigItem.Base in configArray:
        if item:
            ret[item.Code] = "%s/%s" % [item.Text, item.EngText]
    return ret;

func GetConfigValue(code : ConfigCode):
    var item : ConfigItem.Base  = GetItem(code)
    return item.Value;

func GetConfigValueBool(code : ConfigCode):
    var item : ConfigItem.Base  = GetItem(code)
    return item.Value as Bool;

func GetItem(code : ConfigCode) -> ConfigItem.Base:
    if code is ConfigCode:
        var item : ConfigItem.Base  = GetConfigItem(code)
        if not item:
            item = GetReplaceItem(code);
            if not item:
                item = GetDebugItem(code);
        return item
    if not item:
        item = GetReplaceItemByString(code);
        if not item:
            item = GetDebugItemByString(code);
    return item

func GetConfigItem(code) -> ConfigItem.Base:
    if code is ConfigCode:
        for item : ConfigItem.Base in configArray:
            if item and item.Code == code:
                return item
        return null
    var key = code.to_upper()
    for item : ConfigItem.Base in configArray:
        if not item: continue
        if item.Name == key: return item
        if item.Text == key: return item
        if item.EngText == key: return item
    return null

func GetReplaceItem(code) -> ConfigItem.Base:
    if code is ConfigCode:
        for item : ConfigItem.Base in replaceArray:
            if item and item.Code == code:
                return item
        return null
    var key = code.to_upper()
    for item : ConfigItem.Base in replaceArray:
        if not item: continue
        if item.Name == key: return item
        if item.Text == key: return item
        if item.EngText == key: return item
    return null

func GetDebugItem(code) -> ConfigItem.Base:
    if code is ConfigCode:
        for item : ConfigItem.Base in debugArray:
            if item and item.Code == code:
                return item
        return null
    var key = code.to_upper()
    for item : ConfigItem.Base in debugArray:
        if not item: continue
        if item.Name == key: return item
        if item.Text == key: return item
        if item.EngText == key: return item
    return null

static func GetConfigValueInERB(text : String) -> SingleTerm:
    var item : ConfigItem.Base = Instance.GetItem(text)
    if not item:
        #errMes = string.Format(trerror.InvalidConfigName.Text, text);
        return null;
    var term : SingleTerm
    match item.Code:
        ConfigCode.AutoSave:
            if (item.Value is bool):
                term = SingleLongTerm.new(1);
            else:
                term = SingleLongTerm.new(0);
        ConfigCode.MoneyFirst:
            if (item.Value is bool):
                term = SingleLongTerm.new(1);
            else:
                term = SingleLongTerm.new(0);
        ConfigCode.WindowX:
        ConfigCode.PrintCPerLine:
        ConfigCode.PrintCLength:
        ConfigCode.FontSize:
        ConfigCode.LineHeight:
        ConfigCode.SaveDataNos:
        ConfigCode.MaxShopItem:
        ConfigCode.ComAbleDefault:
            term = SingleLongTerm.new(item.GetValue());
        case ConfigCode.ForeColor:
        case ConfigCode.BackColor:
        case ConfigCode.FocusColor:
        case ConfigCode.LogColor:
            var color : Color = item.GetValue();
            term = SingleLongTerm.new(((color.r * 256) + color.g) * 256 + color.b)
        case ConfigCode.pbandDef:
        case ConfigCode.RelationDef:
				term = SingleLongTerm.new(item.GetValue<long>());
        case ConfigCode.FontName:
        case ConfigCode.MoneyLabel:
        case ConfigCode.LoadLabel:
        case ConfigCode.DrawLineString:
        case ConfigCode.TitleMenuString0:
        case ConfigCode.TitleMenuString1:
        case ConfigCode.TimeupLabel:
            term = SingleStrTerm.new(item.GetValue());
        
        case ConfigCode.BarChar1:
        case ConfigCode.BarChar2:
            term = SingleStrTerm.new(item.GetValue());
        case ConfigCode.TextDrawingMode:
            term = SingleStrTerm.new(str(item.GetValue()));
        default:
            if (Enum.IsDefined(typeof(ConfigCode), item.Code)):
                match item.ValueToString():
                    "YES":
                        term = SingleLongTerm.new (1);
                    "NO":
                        term = SingleLongTerm.new (0);
                    default:
                        var val = item.ValueToString()
                        if (long.TryParse(val, out long i)):
                            term = SingleLongTerm.new (i);
                        else:
                            term = SingleStrTerm.new(val);
            else:
                #errMes = string.Format(trerror.NotAllowGetConfigValue.Text, text);
                return null;

func SaveConfig() -> bool
    var writer : StreamWriter = null;
    writer = StreamWriter.new(configPath, false, Config.Encode);
    for i in configArray.size():
        var item := configArray[i];
        if not item: continue;
        if (item.Code == ConfigCode.CompatiDRAWLINE): continue;
        if ((item.Code == ConfigCode.ChangeMasterNameIfDebug) && item.GetValue<bool>()): continue;
        if ((item.Code == ConfigCode.LastKey) && (item.GetValue<long>() == 0)): continue;
        if (item.Code == ConfigCode.ValidExtension):
            var ex = item;
            var sb = ""
            if Config.EnglishConfigOutput:
                sb = ex.EngText
            else:
                sb = ex.text
            sb += ":"
            sb += ",".join(ex.Value)
            writer.WriteLine(sb.ToString());
            continue;
        writer.WriteLine(item.ToString());

	public bool ReLoadConfig()
	{
		
		foreach (AConfigItem item in configArray)
		{
			if (item == null)
				continue;
			if (item.Fixed)
				item.Fixed = false;
		}
		LoadConfig();
		return true;
	}

	public bool LoadConfig()
	{
		string defaultConfigPath = Program.CsvDir + "_default.config";
		string fixedConfigPath = Program.CsvDir + "_fixed.config";
		if (!File.Exists(defaultConfigPath))
			defaultConfigPath = Program.CsvDir + "default.config";
		if (!File.Exists(fixedConfigPath))
			fixedConfigPath = Program.CsvDir + "fixed.config";

		loadConfig(defaultConfigPath, false);
		loadConfig(configPath, false);
		loadConfig(fixedConfigPath, true);

		Config.SetConfig(this);
		bool needSave = false;
		if (!File.Exists(configPath))
			needSave = true;
		if (Config.CheckUpdate())
		{
			GetItem(ConfigCode.LastKey).SetValue(Config.LastKey);
			needSave = true;
		}
		if (needSave)
			SaveConfig();
		return true;
	}

	private bool loadConfig(string confPath, bool fix)
	{
		if (!File.Exists(confPath))
			return false;
		using var eReader = new EraStreamReader(false);
		if (!eReader.Open(confPath))
			return false;
		ScriptPosition? pos = null;
		try
		{
			string line = null;
			
			while ((line = eReader.ReadLine()) != null)
			{
				if ((line.Length == 0) || (line[0] == ';'))
					continue;
				pos = new ScriptPosition(eReader.Filename, eReader.LineNo);
				string[] tokens = line.Split([':']);
				if (tokens.Length < 2)
					continue;
				#region EM_私家版_Emuera多言語化改造
				AConfigItem item = GetConfigItem(tokens[0].Trim());
				#endregion
				if (item != null)
				{
					
					if (item.Code == ConfigCode.CompatiDRAWLINE)
					{
						item = GetConfigItem(ConfigCode.CompatiLinefeedAs1739);
					}
					if (item.Code == ConfigCode.TextEditor)
					{
						
						if (tokens.Length > 2)
						{
							if (tokens[2].StartsWith('\\'))
								tokens[1] += ":" + tokens[2];
							if (tokens.Length > 3)
							{
								for (int i = 3; i < tokens.Length; i++)
								{
									tokens[1] += ":" + tokens[i];
								}
							}
						}
					}
					if (item.Code == ConfigCode.EditorArgument)
					{
						
						((ConfigItem<string>)item).Value = tokens[1];
						continue;
					}
					if (item.Code == ConfigCode.MaxLog && Program.AnalysisMode)
					{
						
						tokens[1] = "10000";
					}
					if (item.TryParse(tokens[1]) && fix)
						item.Fixed = true;
				}
			}
		}
		catch (EmueraException ee)
		{
			ParserMediator.ConfigWarn(ee.Message, pos, 1, null);
		}
		catch (Exception exc)
		{
			ParserMediator.ConfigWarn(exc.GetType().ToString() + ":" + exc.Message, pos, 1, exc.StackTrace);
		}
		finally { eReader.Dispose(); }
		return true;
	}

	#region replace
	
	public void LoadReplaceFile(string filename)
	{
		using var eReader = new EraStreamReader(false);
		if (!eReader.Open(filename))
			return;
		ScriptPosition? pos = null;
		try
		{
			string line = null;
			while ((line = eReader.ReadLine()) != null)
			{
				line = line.Trim();
				if (line.Length == 0)
					continue;
				if (line[0] == ';')
					continue;
				pos = new ScriptPosition(eReader.Filename, eReader.LineNo);
				string[] tokens = line.Split(',', ':');
				if (tokens.Length < 2)
					continue;
				string itemName = tokens[0].Trim();
				tokens[1] = line[(tokens[0].Length + 1)..];
				if (string.IsNullOrEmpty(tokens[1].Trim()))
					continue;
				AConfigItem item = GetReplaceItem(itemName);
				if (item != null)
					item.TryParse(tokens[1]);
			}
		}
		catch (EmueraException ee)
		{
			ParserMediator.Warn(ee.Message, pos, 1);
		}
		catch (Exception exc)
		{
			ParserMediator.Warn(exc.GetType().ToString() + ":" + exc.Message, pos, 1, exc.StackTrace);
		}
		finally { eReader.Dispose(); }
	}

	#endregion

	#region debug


	public bool SaveDebugConfig()
	{
		StreamWriter writer = null;
		try
		{
			#region EM_私家版_Emuera多言語化改造
			
			writer = new StreamWriter(configdebugPath, false, Config.Encode);

			
			for (int i = 0; i < debugArray.Count; i++)
			#endregion
			{
				AConfigItem item = debugArray[i];
				if (item == null)
					continue;
				writer.WriteLine(item.ToString());
			}
		}
		catch (Exception)
		{
			return false;
		}
		finally
		{
			if (writer != null)
				writer.Close();
		}
		return true;
	}

	public bool LoadDebugConfig()
	{
		using var eReader = new EraStreamReader(false);
		if (!File.Exists(configdebugPath))
			goto err;
		if (!eReader.Open(configdebugPath))
			goto err;
		ScriptPosition? pos = null;
		try
		{
			string line = null;
			while ((line = eReader.ReadLine()) != null)
			{
				if ((line.Length == 0) || (line[0] == ';'))
					continue;
				pos = new ScriptPosition(eReader.Filename, eReader.LineNo);
				string[] tokens = line.Split([':']);
				if (tokens.Length < 2)
					continue;
				AConfigItem item = GetDebugItem(tokens[0].Trim());
				if (item != null)
				{
					item.TryParse(tokens[1]);
				}
#if DEBUG
				
				
#endif
			}
		}
		catch (EmueraException ee)
		{
			ParserMediator.ConfigWarn(ee.Message, pos, 1, null);
			goto err;
		}
		catch (Exception exc)
		{
			ParserMediator.ConfigWarn(exc.GetType().ToString() + ":" + exc.Message, pos, 1, exc.StackTrace);
			goto err;
		}
		finally { eReader.Dispose(); }
		Config.SetDebugConfig(this);
		return true;
	err:
		Config.SetDebugConfig(this);
		return false;
	}

	#endregion