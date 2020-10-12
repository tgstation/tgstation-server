import { TypedEmitter } from "tiny-typed-emitter";

import { Components } from "./generatedcode/_generated";
import InternalError, { ErrorCode, GenericErrors } from "./models/InternalComms/InternalError";
import InternalStatus, { StatusCode } from "./models/InternalComms/InternalStatus";
import ServerClient from "./ServerClient";

interface IEvents {
    loadInstanceUser: (
        user: InternalStatus<Components.Schemas.InstanceUser, GenericErrors>
    ) => void;
}

export type getCurrentInstanceUserErrors = GenericErrors;

export default new (class InstanceUserClient extends TypedEmitter<IEvents> {
    private _cachedInstanceUser: Map<
        number,
        InternalStatus<Components.Schemas.InstanceUser, ErrorCode.OK>
    > = new Map<number, InternalStatus<Components.Schemas.InstanceUser, ErrorCode.OK>>();
    public get cachedInstanceUser() {
        return this._cachedInstanceUser;
    }

    private loadingInstanceUserInfo: Map<number, boolean> = new Map<number, boolean>();

    public constructor() {
        super();

        ServerClient.on("purgeCache", () => {
            this._cachedInstanceUser.clear();
        });
    }

    public async getCurrentInstanceUser(
        instanceid: number
    ): Promise<InternalStatus<Components.Schemas.InstanceUser, getCurrentInstanceUserErrors>> {
        await ServerClient.wait4Init();

        if (this._cachedInstanceUser.has(instanceid)) {
            return this._cachedInstanceUser.get(instanceid)!;
        }

        if (this.loadingInstanceUserInfo.get(instanceid)) {
            return await new Promise(resolve => {
                const resolver = (
                    user: InternalStatus<Components.Schemas.InstanceUser, GenericErrors>
                ) => {
                    resolve(user);
                    this.removeListener("loadInstanceUser", resolver);
                };
                this.on("loadInstanceUser", resolver);
            });
        }

        this.loadingInstanceUserInfo.set(instanceid, true);

        let response;
        try {
            response = await ServerClient.apiClient!.InstanceUserController_Read({
                Instance: instanceid
            });
        } catch (stat) {
            const res = new InternalStatus<Components.Schemas.InstanceUser, GenericErrors>({
                code: StatusCode.ERROR,
                error: stat as InternalError<GenericErrors>
            });
            this.emit("loadInstanceUser", res);
            this.loadingInstanceUserInfo.set(instanceid, false);
            return res;
        }

        switch (response.status) {
            case 200: {
                const res = new InternalStatus<Components.Schemas.InstanceUser, ErrorCode.OK>({
                    code: StatusCode.OK,
                    payload: response.data as Components.Schemas.InstanceUser
                });

                this._cachedInstanceUser.set(instanceid, res);
                this.emit("loadInstanceUser", res);
                this.loadingInstanceUserInfo.set(instanceid, false);
                return res;
            }
            default: {
                const res = new InternalStatus<Components.Schemas.InstanceUser, GenericErrors>({
                    code: StatusCode.ERROR,
                    error: new InternalError(
                        ErrorCode.UNHANDLED_RESPONSE,
                        { axiosResponse: response },
                        response
                    )
                });
                this.emit("loadInstanceUser", res);
                this.loadingInstanceUserInfo.set(instanceid, false);
                return res;
            }
        }
    }
})();
