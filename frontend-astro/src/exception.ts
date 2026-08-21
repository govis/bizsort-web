// @ts-nocheck
export enum ErrorMessageType {
    Unknown = 0,
    Operation_Invalid = 11,
    Operation_InvalidInput = 12,
    Operation_InvalidInteraction = 13,
    Operation_UnexpectedState = 14,
    Operation_NotSupported = 15,
    Operation_InternalError = 16,
    Data_RecordNotFound = 21,
    Data_DuplicateRecord = 22,
    Data_ReferentialIntegrity = 23,
    Data_StaleRecord = 24,
    Session_Unavailable = 31,
    Session_NotAuthenticated = 32,
    Session_Unauthorized = 33,
    Session_QuotaExceeded = 34,
    Argument_Invalid = 41,
    Argument_ValueRequired = 42,
    Argument_ValueExists = 43
}

export class SessionException {
    constructor(public type: SessionExceptionType, initializer?: (ex: any) => void) { if (initializer) initializer(this); }
}

export enum SessionExceptionType {
    Unavailable = 1,
    NotAuthenticated = 2,
    Unauthorized = 3,
    QuotaExceeded = 4
}
