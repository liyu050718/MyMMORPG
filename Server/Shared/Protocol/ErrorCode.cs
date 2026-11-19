namespace MyMMORPG.Shared.Protocol;

public enum ErrorCode
{
    Ok = 0,
    NotFound = 1,//id不存在
    InvalidArgument = 2,//非法请求
    AlreadyExists = 3,//玩家名已存在
    UnknownMessageType = 4,//未知信息类型（协议不匹配）
    Internal = 500,//服务端内部错误
}
