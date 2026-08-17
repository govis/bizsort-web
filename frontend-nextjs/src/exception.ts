// @ts-nocheck
export enum ErrorMessageType {
    Unknown = 0,
    Argument_Invalid = 1,
    Argument_Null = 2,
    Argument_OutOfRange = 3,
    Session_Expired = 4,
    Permission_Denied = 5,
    Not_Found = 6,
    Duplicate = 7,
    Concurrency = 8,
    Format_Invalid = 9
}

export class SessionException {
    constructor(public type: SessionExceptionType) {}
}

export enum SessionExceptionType {
    Expired = 1,
    Invalid = 2
}
