class_name ConfigCode

enum
{
	IgnoreCase = 0,
	UseRenameFile = 1,
	UseReplaceFile = 2,
	UseMouse = 3,
	UseMenu = 4,
	UseDebugCommand = 5,
	AllowMultipleInstances = 6,
	AutoSave = 7,
	SizableWindow = 8,
	TextDrawingMode = 9,
	UseImageBuffer = 10,
	WindowX = 11,
	WindowY = 12,
	MaxLog = 13,
	PrintCPerLine = 14,
	PrintCLength = 15,
	FontName = 16,
	FontSize = 17,
	LineHeight = 18,
	ForeColor = 19,
	BackColor = 20,
	FocusColor = 21,
	LogColor = 22,
	FPS = 23,
	SkipFrame = 24,
	InfiniteLoopAlertTime = 25,
	DisplayWarningLevel = 26,
	DisplayReport = 27,
	ReduceArgumentOnLoad = 28,
	IgnoreUncalledFunction = 30,
	FunctionNotFoundWarning = 31,
	FunctionNotCalledWarning = 32,
	ChangeMasterNameIfDebug = 34,
	LastKey = 35,
	ButtonWrap = 36,
	SearchSubdirectory = 37,
	SortWithFilename = 38,
	SetWindowPos = 39,
	WindowPosX = 40,
	WindowPosY = 41,
	ScrollHeight = 42,
	SaveDataNos = 43,
	WarnBackCompatibility = 44,
	AllowFunctionOverloading = 45,
	WarnFunctionOverloading = 46,
	WindowMaximixed = 47,
	TextEditor = 48,
	EditorType = 99,
	EditorArgument = 49,
	WarnNormalFunctionOverloading = 50,
	CompatiErrorLine = 51,
	CompatiCALLNAME = 52,
	DebugShowWindow = 53,
	DebugWindowTopMost = 54,
	DebugWindowWidth = 55,
	DebugWindowHeight = 56,
	DebugSetWindowPos = 57,
	DebugWindowPosX = 58,
	DebugWindowPosY = 59,
	UseSaveFolder = 60,
	CompatiRAND = 61,
	CompatiDRAWLINE = 62,
	CompatiFunctionNoignoreCase,
	SystemAllowFullSpace,
	CompatiLinefeedAs1739,
	useLanguage,
	SystemSaveInBinary,
	CompatiFuncArgAutoConvert,
	CompatiFuncArgOptional,
	AllowLongInputByMouse,
	CompatiCallEvent,
	SystemIgnoreTripleSymbol,
	CompatiSPChara,
	TimesNotRigorousCalculation,
	SystemNoTarget,
	SystemIgnoreStringSet,

	MoneyLabel = 100,
	MoneyFirst = 101,
	LoadLabel = 102,
	MaxShopItem = 103,
	DrawLineString = 104,
	BarChar1 = 105,
	BarChar2 = 106,
	TitleMenuString0 = 107,
	TitleMenuString1 = 108,
	ComAbleDefault = 109,
	StainDefault = 110,
	TimeupLabel = 111,
	ExpLvDef = 112,
	PalamLvDef = 113,
	pbandDef = 114,
	RelationDef = 115,

	UseKeyMacro = 162,

	#region EE_UPDATECHECK
	ForbidUpdateCheck,
	#endregion
	#region EE_ERDConfig
	UseERD,
	#endregion
	#region EE_ERDNAME
	VarsizeDimConfig,
	#endregion
	#region EE_重複定義の確認
	CheckDuplicateIdentifier,
	#endregion
	#region EE_行連結の改行コード置換
	ReplaceContinuationBR,
	#endregion

	#region EM_私家版_LoadText＆SaveText機能拡張
	ValidExtension = 200,
	#endregion
	#region EM_私家版_セーブ圧縮
	ZipSaveData,
	#endregion
	#region EmuEra-Rikaichan related settings
	RikaiEnabled = 250,
	RikaiFilename,
	RikaiColorBack,
	RikaiColorText,
	RikaiUseSeparateBoxes,
	#endregion
	Ctrl_Z_Enabled,
	#region EM_私家版_Emuera多言語化改造
	EnglishConfigOutput,
	EmueraLang,
	#endregion
	#region EM_私家版_Icon指定機能
	EmueraIcon,
	#endregion
	#region EE_AnchorのCB機能移植
	CBUseClipboard,
	CBIgnoreTags,
	CBReplaceTags,
	CBNewLinesOnly,
	CBClearBuffer,
	CBTriggerLeftClick,
	CBTriggerMiddleClick,
	CBTriggerDoubleLeftClick,
	CBTriggerAnyKeyWait,
	CBTriggerInputWait,
	CBMaxCB,
	CBBufferSize,
	CBScrollCount,
	CBMinTimer,
	#endregion
}


enum DisplayWarningFlag
{
    IGNORE = 0,
    LATER = 1,
    ONCE = 2,
    DISPLAY = 3,
}

enum ReduceArgumentOnLoadFlag
{
    YES = 0,
    ONCE = 1,
    NO = 2,
}

enum UseLanguage
{
    JAPANESE = 0,
    KOREAN = 1,
    CHINESE_HANS = 2,
    CHINESE_HANT = 3,
}

enum TextEditorType
{
    SAKURA = 0,
    TERAPAD = 1,
    EMEDITOR = 2,
    USER_SETTING = 3,
}
