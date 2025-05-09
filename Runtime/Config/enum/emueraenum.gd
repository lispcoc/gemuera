class_name EmueraEnum

enum InternalEnum {

}

static func get_string(code : int):
    for key in InternalEnum.keys():
        if InternalEnum.get(key) == code:
            return key
    return ""
