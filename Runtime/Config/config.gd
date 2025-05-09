class_name Config

var Encode : Encoding = EncodingHandler.UTF8BOMEncoding
var SaveEncode : Encoding = EncodingHandler.UTF8BOMEncoding
var nameDic : Dictionary[ConfigCode, String]

func GetConfigName(code : ConfigCode) -> String:
    return nameDic[code];

func SetConfig(instance : ConfigData) -> void:
    nameDic = instance.GetConfigNameDic()
    IgnoreCase = instance.GetConfigValueBool(ConfigCode.IgnoreCase)
    if IgnoreCase:
        StringComparison = StringComparison.OrdinalIgnoreCase;
        StrComper = StringComparer.OrdinalIgnoreCase;
    else:
        StringComparison = StringComparison.Ordinal;
        StrComper = StringComparer.Ordinal;
    UseRenameFile = instance.GetConfigValueBool(ConfigCode.UseRenameFile);
    UseReplaceFile = instance.GetConfigValueBool(ConfigCode.UseReplaceFile);
    UseMouse = instance.GetConfigValueBool(ConfigCode.UseMouse);
    UseMenu = instance.GetConfigValueBool(ConfigCode.UseMenu);
    UseDebugCommand = instance.GetConfigValueBool(ConfigCode.UseDebugCommand);
    AllowMultipleInstances = instance.GetConfigValueBool(ConfigCode.AllowMultipleInstances);
    AutoSave = instance.GetConfigValueBool(ConfigCode.AutoSave);
    UseKeyMacro = instance.GetConfigValueBool(ConfigCode.UseKeyMacro);
    SizableWindow = instance.GetConfigValueBool(ConfigCode.SizableWindow);
    TextDrawingMode = instance.GetConfigValue<TextDrawingMode>(ConfigCode.TextDrawingMode);
    WindowX = instance.GetConfigValueInt(ConfigCode.WindowX);
    WindowY = instance.GetConfigValueInt(ConfigCode.WindowY);
    WindowPosX = instance.GetConfigValueInt(ConfigCode.WindowPosX);
    WindowPosY = instance.GetConfigValueInt(ConfigCode.WindowPosY);
    SetWindowPos = instance.GetConfigValueBool(ConfigCode.SetWindowPos);
    MaxLog = instance.GetConfigValueInt(ConfigCode.MaxLog);
    PrintCPerLine = instance.GetConfigValueInt(ConfigCode.PrintCPerLine);
    PrintCLength = instance.GetConfigValueInt(ConfigCode.PrintCLength);
    ForeColor = instance.GetConfigValueColor(ConfigCode.ForeColor);
    BackColor = instance.GetConfigValueColor(ConfigCode.BackColor);
    FocusColor = instance.GetConfigValueColor(ConfigCode.FocusColor);
    LogColor = instance.GetConfigValueColor(ConfigCode.LogColor);
    FontSize = instance.GetConfigValueInt(ConfigCode.FontSize);
    FontName = instance.GetConfigValueString(ConfigCode.FontName);
    LineHeight = instance.GetConfigValueInt(ConfigCode.LineHeight);
    FPS = instance.GetConfigValueInt(ConfigCode.FPS);
    ScrollHeight = instance.GetConfigValueInt(ConfigCode.ScrollHeight);
    InfiniteLoopAlertTime = instance.GetConfigValueInt(ConfigCode.InfiniteLoopAlertTime);
    SaveDataNos = instance.GetConfigValueInt(ConfigCode.SaveDataNos);
    WarnBackCompatibility = instance.GetConfigValueBool(ConfigCode.WarnBackCompatibility);
    WindowMaximixed = instance.GetConfigValueBool(ConfigCode.WindowMaximixed);
    WarnNormalFunctionOverloading = instance.GetConfigValueBool(ConfigCode.WarnNormalFunctionOverloading);
    SearchSubdirectory = instance.GetConfigValueBool(ConfigCode.SearchSubdirectory);
    SortWithFilename = instance.GetConfigValueBool(ConfigCode.SortWithFilename);

    AllowFunctionOverloading = instance.GetConfigValueBool(ConfigCode.AllowFunctionOverloading);
    if (!AllowFunctionOverloading):
        WarnFunctionOverloading = true
    else:
        WarnFunctionOverloading = instance.GetConfigValueBool(ConfigCode.WarnFunctionOverloading);

    DisplayWarningLevel = instance.GetConfigValueInt(ConfigCode.DisplayWarningLevel);
    DisplayReport = instance.GetConfigValueBool(ConfigCode.DisplayReport);
    ReduceArgumentOnLoad = instance.GetConfigValueReduceArgumentOnLoadFlag(ConfigCode.ReduceArgumentOnLoad);
    IgnoreUncalledFunction = instance.GetConfigValueBool(ConfigCode.IgnoreUncalledFunction);
    FunctionNotFoundWarning = instance.GetConfigValueDisplayWarningFlag(ConfigCode.FunctionNotFoundWarning);
    FunctionNotCalledWarning = instance.GetConfigValueDisplayWarningFlag(ConfigCode.FunctionNotCalledWarning);


    ChangeMasterNameIfDebug = instance.GetConfigValueBool(ConfigCode.ChangeMasterNameIfDebug);
    LastKey = instance.GetConfigValueLong(ConfigCode.LastKey);
    ButtonWrap = instance.GetConfigValueBool(ConfigCode.ButtonWrap);

    TextEditor = instance.GetConfigValueString(ConfigCode.TextEditor);
    EditorType = instance.GetConfigValueTextEditorType(ConfigCode.EditorType);
    EditorArg = instance.GetConfigValueString(ConfigCode.EditorArgument);

    CompatiErrorLine = instance.GetConfigValueBool(ConfigCode.CompatiErrorLine);
    CompatiCALLNAME = instance.GetConfigValueBool(ConfigCode.CompatiCALLNAME);
    UseSaveFolder = instance.GetConfigValueBool(ConfigCode.UseSaveFolder);
    CompatiRAND = instance.GetConfigValueBool(ConfigCode.CompatiRAND);
    CompatiLinefeedAs1739 = instance.GetConfigValueBool(ConfigCode.CompatiLinefeedAs1739);
    SystemAllowFullSpace = instance.GetConfigValueBool(ConfigCode.SystemAllowFullSpace);
    SystemSaveInBinary = instance.GetConfigValueBool(ConfigCode.SystemSaveInBinary);
    SystemIgnoreTripleSymbol = instance.GetConfigValueBool(ConfigCode.SystemIgnoreTripleSymbol);
    SystemIgnoreStringSet = instance.GetConfigValueBool(ConfigCode.SystemIgnoreStringSet);

    CompatiFuncArgAutoConvert = instance.GetConfigValueBool(ConfigCode.CompatiFuncArgAutoConvert);
    CompatiFuncArgOptional = instance.GetConfigValueBool(ConfigCode.CompatiFuncArgOptional);
    CompatiCallEvent = instance.GetConfigValueBool(ConfigCode.CompatiCallEvent);
    CompatiSPChara = instance.GetConfigValueBool(ConfigCode.CompatiSPChara);

    AllowLongInputByMouse = instance.GetConfigValueBool(ConfigCode.AllowLongInputByMouse);

    TimesNotRigorousCalculation = instance.GetConfigValueBool(ConfigCode.TimesNotRigorousCalculation);
    SystemNoTarget = instance.GetConfigValueBool(ConfigCode.SystemNoTarget);

    #region EE版_UPDATECHECK
    ForbidUpdateCheck = instance.GetConfigValueBool(ConfigCode.ForbidUpdateCheck);
    #endregion
    #region EE版_ERDConfig
    UseERD = instance.GetConfigValueBool(ConfigCode.UseERD);
    #endregion
    #region EE_ERDNAME
    VarsizeDimConfig = instance.GetConfigValueBool(ConfigCode.VarsizeDimConfig);
    #endregion
    #region EE_重複定義の確認
    CheckDuplicateIdentifier = instance.GetConfigValueBool(ConfigCode.CheckDuplicateIdentifier);
    #endregion
    #region EE_行連結の改行コード置換
    ReplaceContinuationBR = instance.GetConfigValueString(ConfigCode.ReplaceContinuationBR);
    #endregion

    #region EM_私家版_LoadText＆SaveText機能拡張
    ValidExtension = instance.GetConfigValuePackedStringArray(ConfigCode.ValidExtension);
    #endregion
    #region EM_私家版_セーブ圧縮
    ZipSaveData = instance.GetConfigValueBool(ConfigCode.ZipSaveData);
    #endregion
    #region EM_私家版_Emuera多言語化改造
    EnglishConfigOutput = instance.GetConfigValueBool(ConfigCode.EnglishConfigOutput);
    EmueraLang = instance.GetConfigValueString(ConfigCode.EmueraLang);
    #endregion
    #region EM_私家版_Icon指定機能
    EmueraIcon = instance.GetConfigValueString(ConfigCode.EmueraIcon);
    #endregion
    #region EE_AnchorのCB機能移植
    CBUseClipboard = instance.GetConfigValueBool(ConfigCode.CBUseClipboard);
    CBIgnoreTags = instance.GetConfigValueBool(ConfigCode.CBIgnoreTags);
    CBReplaceTags = instance.GetConfigValueString(ConfigCode.CBReplaceTags);
    CBNewLinesOnly = instance.GetConfigValueBool(ConfigCode.CBNewLinesOnly);
    CBClearBuffer = instance.GetConfigValueBool(ConfigCode.CBClearBuffer);
    CBTriggerLeftClick = instance.GetConfigValueBool(ConfigCode.CBTriggerLeftClick);
    CBTriggerMiddleClick = instance.GetConfigValueBool(ConfigCode.CBTriggerMiddleClick);
    CBTriggerDoubleLeftClick = instance.GetConfigValueBool(ConfigCode.CBTriggerDoubleLeftClick);
    CBTriggerAnyKeyWait = instance.GetConfigValueBool(ConfigCode.CBTriggerAnyKeyWait);
    CBTriggerInputWait = instance.GetConfigValueBool(ConfigCode.CBTriggerInputWait);
    CBMaxCB = instance.GetConfigValueInt(ConfigCode.CBMaxCB);
    CBBufferSize = instance.GetConfigValueInt(ConfigCode.CBBufferSize);
    CBScrollCount = instance.GetConfigValueInt(ConfigCode.CBScrollCount);
    CBMinTimer = instance.GetConfigValueInt(ConfigCode.CBMinTimer);
    RikaiEnabled = instance.GetConfigValueBool(ConfigCode.RikaiEnabled);
    RikaiFilename = instance.GetConfigValueString(ConfigCode.RikaiFilename);
    RikaiColorBack = instance.GetConfigValueColor(ConfigCode.RikaiColorBack);
    RikaiColorText = instance.GetConfigValueColor(ConfigCode.RikaiColorText);
    RikaiUseSeparateBoxes = instance.GetConfigValueBool(ConfigCode.RikaiUseSeparateBoxes);
    Ctrl_Z_Enabled = instance.GetConfigValueBool(ConfigCode.Ctrl_Z_Enabled);


    var lang : UseLanguage = instance.GetConfigValueUseLanguage(ConfigCode.useLanguage)
    match lang:
        UseLanguage.JAPANESE:
            Language = 0x0411; LangManager.setEncode(932)
        UseLanguage.KOREAN:
            Language = 0x0412; LangManager.setEncode(949)
        UseLanguage.CHINESE_HANS:
            Language = 0x0804; LangManager.setEncode(936)
        UseLanguage.CHINESE_HANT:
            Language = 0x0404; LangManager.setEncode(950)

    if (FontSize < 8):
        Dialog.Show(trmb.ConfigError.Text, trmb.TooSmallFontSize.Text);
        FontSize = 8;
    if (LineHeight < FontSize):
        Dialog.Show(trmb.ConfigError.Text, trmb.LineHeightLessThanFontSize.Text);
        LineHeight = FontSize;
    if (SaveDataNos < 20):
        Dialog.Show(trmb.ConfigError.Text, trmb.TooSmallDisplaySaveData.Text);
        SaveDataNos = 20;
    if (SaveDataNos > 80):
        Dialog.Show(trmb.ConfigError.Text, trmb.TooLargeDisplaySaveData.Text);
        SaveDataNos = 80;
    if (MaxLog < 500):
        Dialog.Show(trmb.ConfigError.Text, trmb.TooSmallLogSize.Text);
        MaxLog = 500;
    if (TextDrawingMode == TextDrawingMode.WINAPI):
        MessageBox.Show(trmb.DoNotSupportWINAPI.Text);
        TextDrawingMode = TextDrawingMode.TEXTRENDERER;

    DrawingParam_ShapePositionShift = 0;
    if (TextDrawingMode != TextDrawingMode.WINAPI):
        DrawingParam_ShapePositionShift = Math.Max(2, FontSize / 6);
    DrawableWidth = WindowX - DrawingParam_ShapePositionShift;
    ForceSavDir = Program.ExeDir + "sav" + Path.DirectorySeparatorChar;
    if (UseSaveFolder):
        SavDir = Program.ExeDir + "sav" + Path.DirectorySeparatorChar;
    else:
        SavDir = Program.ExeDir;
    if (UseSaveFolder && !Directory.Exists(SavDir)):
        createSavDirAndMoveFiles()

func UpdateLangSetting(instance : ConfigData) -> void:
    EnglishConfigOutput = instance.GetConfigValueBool(ConfigCode.EnglishConfigOutput);
    EmueraLang = instance.GetConfigValueString(ConfigCode.EmueraLang);

func SetLanguageSetting(instance : ConfigData, lang : String) -> void:
    instance.GetConfigItem(ConfigCode.EmueraLang).SetValue(lang);
    UpdateLangSetting(instance);
    instance.SaveConfig()

var DefaultFont : Font:
    get:
        return FontFactory.GetFont("", FontStyle.Regular)

func ForceCreateSavDir() -> void:
    if (!Directory.Exists(ForceSavDir)):
        Directory.CreateDirectory(ForceSavDir);

func CreateSavDir():
    if (UseSaveFolder && !Directory.Exists(SavDir)):
        Directory.CreateDirectory(SavDir);

func createSavDirAndMoveFiles():
    if not Directory.CreateDirectory(SavDir):
        Dialog.Show(trmb.FolderCreationFailure.Text, trmb.FailedCreateSavFolder.Text);
        return
    var existGlobal : bool = File.Exists(Program.ExeDir + "global.sav")
    var savFiles : Array[String] = Directory.GetFiles(Program.ExeDir, "save*.sav", SearchOption.TopDirectoryOnly)
    if (!existGlobal && savFiles.Length == 0):
        return;
    var result = Dialog.ShowPrompt(trmb.SavFolderCreated.Text, trmb.DataTransfer.Text);
    if (result == false):
        return;
    if (!Directory.Exists(SavDir)):
        Dialog.Show(trmb.DataTransferFailure.Text, trmb.MissingSavFolder.Text);
        return;
    if (File.Exists(Program.ExeDir + "global.sav")):
        File.Move(Program.ExeDir + "global.sav", SavDir + "global.sav");
    savFiles = Directory.GetFiles(Program.ExeDir, "save*.sav", SearchOption.TopDirectoryOnly);
    for oldpath : string in savFiles:
        File.Move(oldpath, SavDir + Path.GetFileName(oldpath))


func CheckUpdate() -> bool:
    if (ReduceArgumentOnLoad != ReduceArgumentOnLoadFlag.ONCE):
        if (ReduceArgumentOnLoad == ReduceArgumentOnLoadFlag.YES):
            NeedReduceArgumentOnLoad = true;
        elif (ReduceArgumentOnLoad == ReduceArgumentOnLoadFlag.NO):
            NeedReduceArgumentOnLoad = false;
        return false

    var key : int = getUpdateKey();
    var updated : bool = LastKey != key;
    LastKey = key
    return updated

func getUpdateKey() -> int:
    var option : SearchOption = SearchOption.TopDirectoryOnly
    if (SearchSubdirectory):
        option = SearchOption.AllDirectories
    var erbFiles := Directory.GetFiles(Program.ErbDir, "*.ERB", option);
    var csvFiles := Directory.GetFiles(Program.CsvDir, "*.CSV", option);
    var writetimes : Array[int]
    writetimes.resize(erbFiles.Length + csvFiles.Length)

    for i in erbFiles.Length:
        if (Path.GetExtension(erbFiles[i]).Equals(".ERB", StringComparison.OrdinalIgnoreCase)):
            writetimes[i] = File.GetLastWriteTime(erbFiles[i]).ToBinary()
    for i in csvFiles.Length:
        if (Path.GetExtension(csvFiles[i]).Equals(".CSV", StringComparison.OrdinalIgnoreCase)):
            writetimes[i + erbFiles.Length] = File.GetLastWriteTime(csvFiles[i]).ToBinary()
    var key := 0;
    for i in writetimes.Length:
        key ^= writetimes[i] * 1103515245 + 12345;
    return key

func GetFiles(dir : String, rootdir : String,  pattern : String) -> Array[KeyValuePair]:
    return _getFiles(dir, rootdir, pattern, !SearchSubdirectory, SortWithFilename);

func _getFiles(dir : String, rootdir : String, pattern : String, toponly : bool, sort : bool) -> Array[KeyValuePair]:
    var retList : Array[KeyValuePair]
    var RelativePath : String
    if (string.Equals(dir, rootdir, StringComparison.OrdinalIgnoreCase)):
        RelativePath = "";
    else:
        if (!dir.StartsWith(rootdir, StringComparison.OrdinalIgnoreCase)):
            RelativePath = dir;
        else:
            RelativePath = dir.substr(rootdir.Length)
        if (!RelativePath.EndsWith('\\') && !RelativePath.EndsWith('/')):
            RelativePath += "\\"
    var filepaths : PackedStringArray = Directory.GetFiles(dir, pattern, SearchOption.TopDirectoryOnly)
    if (sort):
        filepaths.sort()
    for i in filepaths.Length:
        if (Path.GetExtension(filepaths[i]).Length <= 4):
            retList.append(
                KeyValuePair.new(
                    RelativePath.path_join(filepaths[i].get_basename()), filepaths[i]
                )
            )
    if (!toponly):
        var dirList : PackedStringArray = Directory.GetDirectories(dir, "*", SearchOption.TopDirectoryOnly)
        if (dirList.Length > 0):
            if (sort):
                dirList.sort()
            for i in dirList.size():
                retList.append_array((getFiles(dirList[i], rootdir, pattern, toponly, sort)))
    return retList

var IgnoreCase : bool
var StringComparison : StringComparison
const SCIgnoreCase : StringComparison = StringComparison.OrdinalIgnoreCase
const SCExpression : StringComparison = StringComparison.Ordinal
var DrawingParam_ShapePositionShift : int
var UseRenameFile : bool
var UseReplaceFile : bool
var UseMouse : bool
var UseMenu : bool
var UseDebugCommand : bool
var AllowMultipleInstances : bool
var AutoSave : bool
var UseKeyMacro : bool
var SizableWindow : bool
var TextDrawingMode : TextDrawingMode
var WindowX : int
var DrawableWidth : int
var WindowY : int
var WindowPosX : int
var WindowPosY : int
var SetWindowPos : bool
var MaxLog : int
var PrintCPerLine : int
var PrintCLength : int
var ForeColor : Color
var BackColor : Color
var FocusColor : Color
var LogColor : Color

var FontSize : int
var FontName : String
var LineHeight : int
var FPS : int
var ScrollHeight : int
var InfiniteLoopAlertTime : int
var SaveDataNos : int
var WarnBackCompatibility : bool
var WindowMaximixed : bool
var WarnNormalFunctionOverloading : bool
var SearchSubdirectory : bool
var SortWithFilename : bool

var AllowFunctionOverloading : bool
var WarnFunctionOverloading : bool

var DisplayWarningLevel : int
var DisplayReport : bool
var ReduceArgumentOnLoad : ReduceArgumentOnLoadFlag
var IgnoreUncalledFunction : bool
var FunctionNotFoundWarning : DisplayWarningFlag
var FunctionNotCalledWarning : DisplayWarningFlag

var ChangeMasterNameIfDebug : bool
var LastKey : int
var ButtonWrap : bool

var TextEditor : String
var EditorType : TextEditorType
var EditorArg : String

var CompatiErrorLine : bool
var CompatiCALLNAME : bool
var UseSaveFolder : bool
var CompatiRAND : bool
var CompatiLinefeedAs1739 : bool
var SystemAllowFullSpace : bool
var SystemSaveInBinary : bool
var CompatiFuncArgAutoConvert : bool
var CompatiFuncArgOptional : bool
var CompatiCallEvent : bool
var CompatiSPChara : bool
var SystemIgnoreTripleSymbol : bool
var SystemNoTarget : bool
var SystemIgnoreStringSet : bool

var Language : int

var SavDir : String
var ForceSavDir : String

var NeedReduceArgumentOnLoad : bool
var AllowLongInputByMouse : bool
var TimesNotRigorousCalculation : bool


func SetDebugConfig(instance : ConfigData) -> void:
    DebugShowWindow = instance.GetConfigValueBool(ConfigCode.DebugShowWindow);
    DebugWindowTopMost = instance.GetConfigValueBool(ConfigCode.DebugWindowTopMost);
    DebugWindowWidth = instance.GetConfigValueInt(ConfigCode.DebugWindowWidth);
    DebugWindowHeight = instance.GetConfigValueInt(ConfigCode.DebugWindowHeight);
    DebugSetWindowPos = instance.GetConfigValueBool(ConfigCode.DebugSetWindowPos);
    DebugWindowPosX = instance.GetConfigValueInt(ConfigCode.DebugWindowPosX);
    DebugWindowPosY = instance.GetConfigValueInt(ConfigCode.DebugWindowPosY);

var DebugShowWindow : bool
var DebugWindowTopMost : bool
var DebugWindowWidth : int
var DebugWindowHeight : int
var DebugSetWindowPos : bool
var DebugWindowPosX : int
var DebugWindowPosY : int

func SetReplace(instance : ConfigData) -> void:
    MoneyLabel = instance.GetConfigValueString(ConfigCode.MoneyLabel);
    MoneyFirst = instance.GetConfigValueBool(ConfigCode.MoneyFirst);
    LoadLabel = instance.GetConfigValueString(ConfigCode.LoadLabel);
    MaxShopItem = instance.GetConfigValueInt(ConfigCode.MaxShopItem);
    DrawLineString = instance.GetConfigValueString(ConfigCode.DrawLineString);
    if (string.IsNullOrEmpty(DrawLineString)):
        DrawLineString = "-";
    BarChar1 = instance.GetConfigValue<char>(ConfigCode.BarChar1);
    BarChar2 = instance.GetConfigValue<char>(ConfigCode.BarChar2);
    TitleMenuString0 = instance.GetConfigValueString(ConfigCode.TitleMenuString0);
    TitleMenuString1 = instance.GetConfigValueString(ConfigCode.TitleMenuString1);
    ComAbleDefault = instance.GetConfigValueInt(ConfigCode.ComAbleDefault);
    StainDefault = instance.GetConfigValue<List<long>>(ConfigCode.StainDefault);
    TimeupLabel = instance.GetConfigValueString(ConfigCode.TimeupLabel);
    ExpLvDef = instance.GetConfigValue<List<long>>(ConfigCode.ExpLvDef);
    PalamLvDef = instance.GetConfigValue<List<long>>(ConfigCode.PalamLvDef);
    PbandDef = instance.GetConfigValue<long>(ConfigCode.pbandDef);
    RelationDef = instance.GetConfigValue<long>(ConfigCode.RelationDef)

var MoneyLabel : String
var MoneyFirst : bool
var LoadLabel : String
var MaxShopItem : int
var DrawLineString : String
var BarChar1 : String
var BarChar2 : String
var TitleMenuString0 : String
var TitleMenuString1 : String
var ComAbleDefault : int
var StainDefault : PackedInt64Array
var TimeupLabel : String
var ExpLvDef : PackedInt64Array
var PalamLvDef : PackedInt64Array
var PbandDef : int
var RelationDef : int

var StrComper : StringComparer = StringComparer.OrdinalIgnoreCase;
var ForbidUpdateCheck : bool
var UseERD : bool
var VarsizeDimConfig : bool
var CheckDuplicateIdentifier : bool
var ReplaceContinuationBR : String
var ValidExtension : PackedStringArray
var ZipSaveData : bool
var EnglishConfigOutput : bool
var EmueraLang : String
var EmueraIcon : String
var CBUseClipboard : bool
var CBIgnoreTags : bool
var CBReplaceTags : String
var CBNewLinesOnly : bool
var CBClearBuffer : bool
var CBTriggerLeftClick : bool
var CBTriggerMiddleClick : bool
var CBTriggerDoubleLeftClick : bool
var CBTriggerAnyKeyWait : bool
var CBTriggerInputWait : bool
var CBMaxCB : int
var CBBufferSize : int
var CBScrollCount : int
var CBMinTimer : int
var RikaiEnabled : bool
var RikaiFilename : String
var RikaiColorBack : Color
var RikaiColorText : Color
var RikaiUseSeparateBoxes : bool
var Ctrl_Z_Enabled : bool

