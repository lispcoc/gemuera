class_name TextEditorType

var value : InternalEnum

enum
{
    SAKURA = 0,
    TERAPAD = 1,
    EMEDITOR = 2,
    USER_SETTING = 3,
}

enum InternalEnum
{
    SAKURA = 0,
    TERAPAD = 1,
    EMEDITOR = 2,
    USER_SETTING = 3,
}

func _init(_value) -> void:
    value = _value

func _to_string() -> String:
    for key in InternalEnum.keys():
        if InternalEnum.get(key) == value:
            return key
    return ""

static func from_string(string : String):
    return new(InternalEnum.get(string))
