import { Components } from "./generatedcode/_generated";
import InternalError, { ErrorCode, GenericErrors } from "./models/InternalComms/InternalError";
import InternalStatus, { StatusCode } from "./models/InternalComms/InternalStatus";
import ServerClient from "./ServerClient";

export type ListInstancesErrors = GenericErrors;
export type EditInstanceErrors = GenericErrors | ErrorCode.INSTANCE_NO_DB_ENTITY;

export default new (class InstanceClient {
    public async listInstances(): Promise<
        InternalStatus<Components.Schemas.Instance[], ListInstancesErrors>
    > {
        await ServerClient.wait4Init();

        let response;
        try {
            response = await ServerClient.apiClient!.InstanceController_List();
        } catch (stat) {
            return new InternalStatus({
                code: StatusCode.ERROR,
                error: stat as InternalError<GenericErrors>
            });
        }

        switch (response.status) {
            case 200: {
                const payload = (response.data as Components.Schemas.Instance[]).sort(
                    (a, b) => a.id - b.id
                );

                return new InternalStatus({
                    code: StatusCode.OK,
                    payload
                });
            }
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

    public async editInstance(
        instance: Components.Schemas.Instance
    ): Promise<InternalStatus<Components.Schemas.Instance, EditInstanceErrors>> {
        await ServerClient.wait4Init();

        let response;
        try {
            response = await ServerClient.apiClient!.InstanceController_Update(null, instance);
        } catch (stat) {
            return new InternalStatus({
                code: StatusCode.ERROR,
                error: stat as InternalError<GenericErrors>
            });
        }
        switch (response.status) {
            case 200:
            case 202: {
                const instance = response.data as Components.Schemas.Instance;

                return new InternalStatus({
                    code: StatusCode.OK,
                    payload: instance
                });
            }
            case 410:
                return new InternalStatus({
                    code: StatusCode.ERROR,
                    error: new InternalError(ErrorCode.INSTANCE_NO_DB_ENTITY, {
                        errorMessage: response.data as Components.Schemas.ErrorMessage
                    })
                });
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
