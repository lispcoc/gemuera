class_name UseLanguage

var value : InternalEnum

enum InternalEnum
{
    JAPANESE = 0,
    KOREAN = 1,
    CHINESE_HANS = 2,
    CHINESE_HANT = 3,
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
