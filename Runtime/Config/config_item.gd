class_name ConfigItem
var Code : int
var Name : String
var Text : String
var EngText : String
var Fixed : bool
var Value

func _init(code : int,  text : String, etext : String, _value) -> void:
    Code = code
    Name = ConfigCode.get_string(code)
    Text = text.to_upper()
    EngText = etext.to_upper()
    Value = _value

func SetValue(p):
    Value = p
func GetValue(): return Value

func ValueToString() -> String:
    if Value is bool:
        if Value: return "YES"
        return "NO"
    if Value is Color:
        return "%d, %d, %d" % [Value.r, Value.g, Value.b]
    if Value is PackedStringArray:
        return ",".join(Value)
    return str(Value)

func _to_string() -> String:
    if Config.EnglishConfigOutput:
        return EngText + ValueToString()
    return Text + ":" + ValueToString()

func TryParse (param : String) -> bool:
    if not  param:
        return false;
    if (Fixed):
        return false
    var _str : String = param.trim_suffix(" ").trim_prefix(" ")
    if Value is bool:
        Value = tryStringToBool(_str)
    if Value is Color:
        Value = tryStringsToColor(_str)
    if Value is String:
        Value = _str
    if Value is int:
        Value = _str.to_int()
    if Value is PackedInt64Array:
        Value.clear()
        for s in "/".split(_str):
            Value.append(s.to_int())
    if Value is PackedStringArray:
        Value = tryStringToStringList(_str)
    if Value is TextDrawingMode:
        Value = TextDrawingMode.from_string(param)
    if Value is DisplayWarningFlag:
        Value = DisplayWarningFlag.from_string(param)
    if Value is UseLanguage:
        Value = UseLanguage.from_string(param)
    if Value is TextEditorType:
        Value = TextEditorType.from_string(param)
    if Value is ReduceArgumentOnLoadFlag:
        Value = ReduceArgumentOnLoadFlag.from_string(param)
    return true

func tryStringToStringList(arg : String):
    var ret : PackedStringArray
    for s in ",".split(arg):
        ret.append(s)
    return ret

func tryStringToBool(arg : String):
    if not arg:
        return false
    var _str := arg.trim_suffix(" ").trim_prefix(" ")
    if _str.to_int():
        return true;
    if _str.to_upper() == "NO" || _str.to_upper() == "FALSE" || _str == "後":
        return false;
    if _str.to_upper() == "YES" || _str.to_upper() == "TRUE" || _str == "前":
        return true;
    return false;

func tryStringsToColor(arg : String):
    var tokens := ",".split(arg)
    if (tokens.size() < 3):
        return false
    var r := tokens[0].to_int()
    var g := tokens[1].to_int()
    var b := tokens[2].to_int()
    if r < 0 || r > 255:
        return false
    if g < 0 || g > 255:
        return false
    if b < 0 || b > 255:
        return false
    return Color(r, g, b)
