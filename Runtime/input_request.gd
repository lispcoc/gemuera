class_name InputRequest

enum InputType
{
	EnterKey = 1, #Enterキーかクリック
	AnyKey = 2,#なんでもいいから入力
	IntValue = 3,#整数値。OneInputかどうかは別の変数で
	StrValue = 4,#文字列。
	Void = 5,#入力不能。待つしかない→スキップ中orマクロ中ならなかったことになる
	#region EE_INPUTANY
	AnyValue = 6,#数値or文字列
	#endregion
	#region EE_BINPUT
	IntButton = 7,
	StrButton = 8,
	#endregion
	#1823
	PrimitiveMouseKey = 11,
}

var ID : int
var inputType : InputType
var NeedValue : bool:
	get():
		return inputType == InputType.IntValue || inputType == InputType.StrValue || \
			inputType == InputType.PrimitiveMouseKey || inputType == InputType.AnyValue || \
			inputType == InputType.IntButton || inputType == InputType.StrButton

var MouseInput : bool
var OneInput : bool
var StopMesskip : bool
var IsSystemInput : bool
var HasDefValue : bool
var DefIntValue : int
var DefStrValue : String
var Timelimit : int = -1
var DisplayTime : bool
var TimeUpMes : String
var LastRequestID : int
