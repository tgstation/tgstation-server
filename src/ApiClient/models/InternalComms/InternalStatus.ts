import InternalError, { ErrorCode } from "./InternalError";

export enum StatusCode {
    OK,
    ERROR
}

declare interface argsErr<Codes extends ErrorCode> {
    code: StatusCode.ERROR;
    error: InternalError<Codes>;
}

declare interface argsOk<T> {
    code: StatusCode.OK;
    payload: T;
}

declare type args<T, Codes extends ErrorCode> = argsErr<Codes> | argsOk<T>;

class InternalStatus<T, Codes extends ErrorCode> {
    public code: StatusCode;
    public payload?: T;
    public error?: InternalError<Codes>;

    public constructor(args: args<T, Codes>) {
        this.code = args.code;
        switch (args.code) {
            case StatusCode.OK:
                this.payload = args.payload;
                break;
            case StatusCode.ERROR:
                this.error = args.error;
                break;
        }
    }
}

export default InternalStatus;
