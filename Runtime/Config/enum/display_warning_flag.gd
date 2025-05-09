class_name DisplayWarningFlag

enum InternalEnum
{
    IGNORE = 0,
    LATER = 1,
    ONCE = 2,
    DISPLAY = 3,
}

var value : InternalEnum

func _init(_value) -> void:
    value = _value

func _to_string() -> String:
    for key in InternalEnum.keys():
        if InternalEnum.get(key) == value:
            return key
    return ""

static func from_string(string : String):
    return new(InternalEnum.get(string))
