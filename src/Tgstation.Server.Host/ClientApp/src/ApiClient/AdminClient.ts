import { TypedEmitter } from "tiny-typed-emitter/lib";

import { Components } from "./generatedcode/_generated";
import InternalError, { ErrorCode, GenericErrors } from "./models/InternalComms/InternalError";
import InternalStatus, { StatusCode } from "./models/InternalComms/InternalStatus";
import ServerClient from "./ServerClient";

interface IEvents {
    loadAdminInfo: (
        user: InternalStatus<Components.Schemas.Administration, AdminInfoErrors>
    ) => void;
}

export type AdminInfoErrors =
    | GenericErrors
    | ErrorCode.ADMIN_GITHUB_RATE
    | ErrorCode.ADMIN_GITHUB_ERROR;

export type RestartErrors = GenericErrors | ErrorCode.ADMIN_WATCHDOG_UNAVAIL;

export type UpdateErrors =
    | GenericErrors
    | ErrorCode.ADMIN_WATCHDOG_UNAVAIL
    | ErrorCode.ADMIN_VERSION_NOT_FOUND
    | ErrorCode.ADMIN_GITHUB_RATE
    | ErrorCode.ADMIN_GITHUB_ERROR;

export type LogsErrors = GenericErrors | ErrorCode.ADMIN_LOGS_IO_ERROR;

export type LogErrors = GenericErrors;

export default new (class AdminClient extends TypedEmitter<IEvents> {
    private _cachedAdminInfo?: InternalStatus<Components.Schemas.Administration, ErrorCode.OK>;
    public get cachedAdminInfo() {
        return this._cachedAdminInfo;
    }
    private loadingAdminInfo = false;

    public constructor() {
        super();
        ServerClient.on("purgeCache", () => {
            this._cachedAdminInfo = undefined;
        });
    }

    public async getAdminInfo(): Promise<
        InternalStatus<Components.Schemas.Administration, AdminInfoErrors>
    > {
        await ServerClient.wait4Init();
        if (this._cachedAdminInfo) {
            return this._cachedAdminInfo;
        }

        if (this.loadingAdminInfo) {
            return await new Promise(resolve => {
                const resolver = (
                    user: InternalStatus<Components.Schemas.Administration, AdminInfoErrors>
                ) => {
                    resolve(user);
                    this.removeListener("loadAdminInfo", resolver);
                };
                this.on("loadAdminInfo", resolver);
            });
        }

        this.loadingAdminInfo = true;

        let response;
        try {
            response = await ServerClient.apiClient!.AdministrationController_Read();
        } catch (stat) {
            const res = new InternalStatus<Components.Schemas.Administration, AdminInfoErrors>({
                code: StatusCode.ERROR,
                error: stat as InternalError<AdminInfoErrors>
            });
            this.emit("loadAdminInfo", res);
            this.loadingAdminInfo = false;
            return res;
        }

        switch (response.status) {
            case 200: {
                const thing = new InternalStatus<Components.Schemas.Administration, ErrorCode.OK>({
                    code: StatusCode.OK,
                    payload: response.data as Components.Schemas.Administration
                });

                this._cachedAdminInfo = thing;
                this.emit("loadAdminInfo", thing);
                this.loadingAdminInfo = false;
                return thing;
            }
            case 424: {
                const errorMessage = response.data as Components.Schemas.ErrorMessage;
                const thing = new InternalStatus<
                    Components.Schemas.Administration,
                    ErrorCode.ADMIN_GITHUB_RATE
                >({
                    code: StatusCode.ERROR,
                    error: new InternalError(
                        ErrorCode.ADMIN_GITHUB_RATE,
                        { errorMessage },
                        response
                    )
                });
                this.emit("loadAdminInfo", thing);
                this.loadingAdminInfo = false;
                return thing;
            }
            case 429: {
                const errorMessage = response.data as Components.Schemas.ErrorMessage;
                const thing = new InternalStatus<
                    Components.Schemas.Administration,
                    ErrorCode.ADMIN_GITHUB_ERROR
                >({
                    code: StatusCode.ERROR,
                    error: new InternalError(
                        ErrorCode.ADMIN_GITHUB_ERROR,
                        { errorMessage },
                        response
                    )
                });
                this.emit("loadAdminInfo", thing);
                this.loadingAdminInfo = false;
                return thing;
            }
            default: {
                const res = new InternalStatus<
                    Components.Schemas.Administration,
                    ErrorCode.UNHANDLED_RESPONSE
                >({
                    code: StatusCode.ERROR,
                    error: new InternalError(
                        ErrorCode.UNHANDLED_RESPONSE,
                        { axiosResponse: response },
                        response
                    )
                });
                this.emit("loadAdminInfo", res);
                this.loadingAdminInfo = false;
                return res;
            }
        }
    }

    public async restartServer(): Promise<InternalStatus<null, RestartErrors>> {
        await ServerClient.wait4Init();

        let response;
        try {
            response = await ServerClient.apiClient!.AdministrationController_Delete();
        } catch (stat) {
            return new InternalStatus({
                code: StatusCode.ERROR,
                error: stat as InternalError<RestartErrors>
            });
        }

        switch (response.status) {
            case 204: {
                return new InternalStatus({ code: StatusCode.OK, payload: null });
            }
            case 422: {
                const errorMessage = response.data as Components.Schemas.ErrorMessage;
                return new InternalStatus({
                    code: StatusCode.ERROR,
                    error: new InternalError(
                        ErrorCode.ADMIN_WATCHDOG_UNAVAIL,
                        { errorMessage },
                        response
                    )
                });
            }
            default: {
                return new InternalStatus<null, ErrorCode.UNHANDLED_RESPONSE>({
                    code: StatusCode.ERROR,
                    error: new InternalError(
                        ErrorCode.UNHANDLED_RESPONSE,
                        { axiosResponse: response },
                        response
                    )
                });
            }
        }
    }

    public async updateServer(newVersion: string): Promise<InternalStatus<null, UpdateErrors>> {
        await ServerClient.wait4Init();

        let response;
        try {
            response = await ServerClient.apiClient!.AdministrationController_Update(null, {
                windowsHost: true,
                newVersion,
                latestVersion: null,
                trackedRepositoryUrl: null
            });
        } catch (stat) {
            return new InternalStatus({
                code: StatusCode.ERROR,
                error: stat as InternalError<UpdateErrors>
            });
        }

        switch (response.status) {
            case 202: {
                return new InternalStatus({ code: StatusCode.OK, payload: null });
            }
            case 410: {
                const errorMessage = response.data as Components.Schemas.ErrorMessage;
                return new InternalStatus({
                    code: StatusCode.ERROR,
                    error: new InternalError(
                        ErrorCode.ADMIN_VERSION_NOT_FOUND,
                        { errorMessage },
                        response
                    )
                });
            }
            case 422: {
                const errorMessage = response.data as Components.Schemas.ErrorMessage;
                return new InternalStatus({
                    code: StatusCode.ERROR,
                    error: new InternalError(
                        ErrorCode.ADMIN_WATCHDOG_UNAVAIL,
                        { errorMessage },
                        response
                    )
                });
            }
            case 424: {
                const errorMessage = response.data as Components.Schemas.ErrorMessage;
                return new InternalStatus<null, ErrorCode.ADMIN_GITHUB_RATE>({
                    code: StatusCode.ERROR,
                    error: new InternalError(
                        ErrorCode.ADMIN_GITHUB_RATE,
                        { errorMessage },
                        response
                    )
                });
            }
            case 429: {
                const errorMessage = response.data as Components.Schemas.ErrorMessage;
                return new InternalStatus<null, ErrorCode.ADMIN_GITHUB_ERROR>({
                    code: StatusCode.ERROR,
                    error: new InternalError(
                        ErrorCode.ADMIN_GITHUB_ERROR,
                        { errorMessage },
                        response
                    )
                });
            }
            default: {
                return new InternalStatus<null, ErrorCode.UNHANDLED_RESPONSE>({
                    code: StatusCode.ERROR,
                    error: new InternalError(
                        ErrorCode.UNHANDLED_RESPONSE,
                        { axiosResponse: response },
                        response
                    )
                });
            }
        }
    }

    public async getLogs(): Promise<InternalStatus<Components.Schemas.LogFile[], LogsErrors>> {
        await ServerClient.wait4Init();

        let response;
        try {
            response = await ServerClient.apiClient!.AdministrationController_ListLogs();
        } catch (stat) {
            return new InternalStatus({
                code: StatusCode.ERROR,
                error: stat as InternalError<LogsErrors>
            });
        }

        switch (response.status) {
            case 200: {
                return new InternalStatus({
                    code: StatusCode.OK,
                    payload: response.data as Components.Schemas.LogFile[]
                });
            }
            /*case 409: {
                //not handled here as its a global error but instead handled in the interceptor as a snowflaky response, thanks for using 409 for two things cyber.

            }*/
            default: {
                return new InternalStatus({
                    code: StatusCode.ERROR,
                    error: new InternalError(
                        ErrorCode.UNHANDLED_RESPONSE,
                        { axiosResponse: response },
                        response
                    )
                });
            }
        }
    }

    public async getLog(
        logName: string
    ): Promise<InternalStatus<Components.Schemas.LogFile, LogErrors>> {
        await ServerClient.wait4Init();

        let response;
        try {
            response = await ServerClient.apiClient!.AdministrationController_GetLog({
                path: logName
            });
        } catch (stat) {
            return new InternalStatus({
                code: StatusCode.ERROR,
                error: stat as InternalError<GenericErrors>
            });
        }
        switch (response.status) {
            case 200: {
                return new InternalStatus({
                    code: StatusCode.OK,
                    payload: response.data as Components.Schemas.LogFile
                });
            }
            /*case 409: {
                //not handled here as its a global error but instead handled in the interceptor as a snowflaky response, thanks for using 409 for two things cyber.
            }*/
            default: {
                return new InternalStatus({
                    code: StatusCode.ERROR,
                    error: new InternalError(
                        ErrorCode.UNHANDLED_RESPONSE,
                        { axiosResponse: response },
                        response
                    )
                });
            }
        }
    }
})();
