class_name ReduceArgumentOnLoadFlag

var value : InternalEnum

static var YES : ReduceArgumentOnLoadFlag = new(InternalEnum.YES)
static var ONCE : ReduceArgumentOnLoadFlag = new(InternalEnum.ONCE)
static var NO : ReduceArgumentOnLoadFlag = new(InternalEnum.NO)

enum InternalEnum
{
    YES = 0,
    ONCE = 1,
    NO = 2,
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
