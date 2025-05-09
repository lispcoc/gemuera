class_name Program

static var ExeDir : String
static var WorkingDir : String

var CsvDir : String
var ErbDir : String
static var DebugDir : String
var DatDir : String
var ContentDir : String
var ExeName : String

var FontDir : String

var rebootFlag : bool

#var RebootWinState : FormWindowState =  FormWindowState.Normal
var AnalysisMode : bool
var AnalysisFiles : PackedStringArray

var DebugMode : bool

func Main(args : PackedStringArray):
    # memo: Shift-JISを扱うためのおまじない
    #System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

    CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

    var rootCommand := RootCommand.new("Emuera")

    WorkingDir = AssemblyData.WorkingDir

    var exeDirOption = OptionString.new({
        name: "--ExeDir",
        description: "与えられたフォルダのEraを起動します"
    })
    rootCommand.AddOption(exeDirOption)

    var debugModeOption := OptionBool.new({
        name: "-Debug",
        description: "デバッグモード"
    })
    debugModeOption.AddAlias("-debug");
    debugModeOption.AddAlias("-DEBUG");
    rootCommand.AddOption(debugModeOption);

    var genLangOption = OptionBool.new({
        name: "-GenLang",
        description: "言語ファイルテンプレ生成"
    })
    rootCommand.AddOption(genLangOption);

    var filesArg = Argument.new(
        "解析するファイル"
    )
    Arity = ArgumentArity.ZeroOrMore
    rootCommand.AddArgument(filesArg);

    var result = rootCommand.Parse(args);

    var exeDir := result.GetValueForOption(exeDirOption)
    if (exeDir != null):
        SetDirPaths(exeDir);

    ExeName = Path.GetFileNameWithoutExtension(AssemblyData.ExeName);


    var debugMode := result.GetValueForOption(debugModeOption);
    DebugMode = debugMode;

    var genLang := result.GetValueForOption(genLangOption);
    if (genLang):
        Lang.GenerateDefaultLangFile();

    var otherArgs : PackedStringArray = []

    var fileArgs := result.GetValueForArgument(filesArg)
    var analysisRequestPaths := fileArgs
    if (analysisRequestPaths.Length > 0):
            AnalysisMode = true

    Application.SetCompatibleTextRenderingDefault(false);

    if exeDir: ProfileOptimization.SetProfileRoot(exeDir);
    else: ProfileOptimization.SetProfileRoot(ExeDir);
    ProfileOptimization.StartProfile("profile");

    ConfigData.Instance.LoadConfig();
    JSONConfig.Load();

    FunctionIdentifier.bgm.close();
    for i in FunctionIdentifier.sound.size():
        if (FunctionIdentifier.sound[i] != null):
            FunctionIdentifier.sound[i].close()

    Lang.LoadLanguageFiles();
    Lang.SetLanguage();
    var icon : Icon = null
    var bmp := Utils.LoadImage(Utils.GetValidPath(Config.EmueraIcon));
    if (bmp != null):
        icon = Utils.MakeIconFromBmpFile(bmp);
        bmp.Dispose();

    if ((!Config.AllowMultipleInstances) && AssemblyData.PrevInstance()):
        Dialog.Show(Lang.UI.MainWindow.MsgBox.InstaceExists.Text, Lang.UI.MainWindow.MsgBox.MultiInstanceInfo.Text)
        return;
    if (!Directory.Exists(CsvDir)):
        Dialog.Show(Lang.UI.MainWindow.MsgBox.FolderNotFound.Text, Lang.UI.MainWindow.MsgBox.NoCsvFolder.Text)
        return;
    if (!Directory.Exists(ErbDir)):
        Dialog.Show(Lang.UI.MainWindow.MsgBox.FolderNotFound.Text, Lang.UI.MainWindow.MsgBox.NoErbFolder.Text);
        return;
    if (Directory.Exists(FontDir)):
        for fontFile : String in Directory.GetFiles(FontDir, "*.ttf", SearchOption.AllDirectories):
            GlobalStatic.Pfc.AddFontFile(fontFile)

        for fontFile : String in Directory.GetFiles(FontDir, "*.otf", SearchOption.AllDirectories):
            GlobalStatic.Pfc.AddFontFile(fontFile);
    if (DebugMode):
        ConfigData.Instance.LoadDebugConfig();
        if (!Directory.Exists(DebugDir)):
            if not Directory.CreateDirectory(DebugDir):
                Dialog.Show(Lang.UI.MainWindow.MsgBox.FolderNotFound.Text, Lang.UI.MainWindow.MsgBox.FailedCreateDebugFolder.Text);
                return;

    if (AnalysisMode):
        AnalysisFiles = [];
        for path in analysisRequestPaths:
            if (!File.Exists(path) && !Directory.Exists(path)):
                MessageBox.Show(Lang.UI.MainWindow.MsgBox.ArgPathNotExists.Text);
                return;
            if ((File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory):
                var fnames : Array[KeyValuePair] = Config.GetFiles(path + "\\", "*.ERB")
                for j in fnames.Count:
                    AnalysisFiles.append(fnames[j].Value);
            else:
                if (!Path.GetExtension(path).Equals(".ERB", StringComparison.OrdinalIgnoreCase)):
                    MessageBox.Show(Lang.UI.MainWindow.MsgBox.InvalidArg.Text);
                    return;
                AnalysisFiles.append(path)

    ApplicationConfiguration.Initialize();

    var win = Forms.MainWindow.new(args);
    win.TranslateUI();
    if (icon != null):
        win.SetupIcon(icon);
    Application.Run(win)

func SetDirPaths(exeDir : String) -> void:
    ExeDir = Path.GetFullPath(DirectoryInfo.new(exeDir).FullName + Path.DirectorySeparatorChar);

    CsvDir = Path.Combine(ExeDir, "csv") + Path.DirectorySeparatorChar;
    ErbDir = Path.Combine(ExeDir, "erb") + Path.DirectorySeparatorChar;
    DebugDir = Path.Combine(ExeDir, "debug") + Path.DirectorySeparatorChar;
    DatDir = Path.Combine(ExeDir, "dat") + Path.DirectorySeparatorChar;
    ContentDir = Path.Combine(ExeDir, "resources") + Path.DirectorySeparatorChar;
    #region EE_フォントファイル対応
    FontDir = Path.Combine(ExeDir, "font") + Path.DirectorySeparatorChar;
    #endregion

func _init() -> void:
    var baseDirectory := AppContext.BaseDirectory;
    if (Directory.Exists(Path.Combine(baseDirectory, "Data", "erb"))):
        baseDirectory = Path.Combine(baseDirectory, "Data")
    SetDirPaths(baseDirectory);
