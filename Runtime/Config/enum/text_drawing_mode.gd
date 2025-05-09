class_name TextDrawingMode

var value : InternalEnum

enum InternalEnum
{
    GRAPHICS = 0,
    TEXTRENDERER = 1,
    WINAPI = 2,
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
