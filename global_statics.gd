class_name GlobalStatic

var Console : EmueraConsole
var Process : Process
var GameBaseData : GameBase
var ConstantData : ConstantData
var VariableData : VariableData
var VEvaluator : VariableEvaluator
var IdentifierDictionary : IdentifierDictionary
var EMediator : ExpressionMediator
var LabelDictionary : LabelDictionary
var tempDic : Dictionary[String, int]
var ForceQuitAndRestart : bool
var Pfc : PrivateFontCollection
var ctrlZ : CtrlZ
var StackList : Array[FunctionLabelLine]

func Reset() -> vood:
    Process = null
    ConstantData = null
    GameBaseData = null
    EMediator = null
    VEvaluator = null
    VariableData = null
    Console = null
    LabelDictionary = null
    IdentifierDictionary = null
    tempDic.clear()
