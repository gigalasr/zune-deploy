enum class Result {
    Ok = 0,

    ErrorNoDevice = 1,
    ErrorHandshakeFailed = 2,
    ErrorBufferTooSmall = 3,
    ErrorConnectionFailed = 4,
    ErrorReadFailed = 5,
    ErrorSendFailed = 6
};