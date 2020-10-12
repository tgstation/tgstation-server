import React, { ReactNode } from "react";
import Badge from "react-bootstrap/Badge";
import Button from "react-bootstrap/Button";
import Table from "react-bootstrap/Table";
import { FormattedMessage } from "react-intl";
import { RouteComponentProps, withRouter } from "react-router-dom";

import { InstanceManagerRights } from "../../../ApiClient/generatedcode/_enums";
import { Components } from "../../../ApiClient/generatedcode/_generated";
import InstanceClient from "../../../ApiClient/InstanceClient";
import InstanceUserClient from "../../../ApiClient/InstanceUserClient";
import InternalError, { ErrorCode } from "../../../ApiClient/models/InternalComms/InternalError";
import { StatusCode } from "../../../ApiClient/models/InternalComms/InternalStatus";
import UserClient from "../../../ApiClient/UserClient";
import { AppCategories, AppRoutes } from "../../../utils/routes";
import ErrorAlert from "../../utils/ErrorAlert";
import Loading from "../../utils/Loading";

type Instance = Components.Schemas.Instance & {
    canAccess: boolean;
};

interface IState {
    instances: Instance[];
    loading?: boolean;
    errors: Array<InternalError<ErrorCode> | undefined>;
    //isnt directly used but is used to make react rerender when the selected insance is changed
    instanceid?: number;
    canOnline: boolean;
}
interface IProps extends RouteComponentProps {}

export default withRouter(
    class InstanceList extends React.Component<IProps, IState> {
        public constructor(props: IProps) {
            super(props);

            this.setOnline = this.setOnline.bind(this);

            const actualid =
                AppCategories.instance.data?.instanceid !== undefined
                    ? parseInt(AppCategories.instance.data.instanceid as string)
                    : undefined;

            this.state = {
                loading: true,
                instances: [],
                errors: [],
                instanceid: actualid,
                canOnline: false
            };
        }

        private addError(error: InternalError<ErrorCode>): void {
            this.setState(prevState => {
                const errors = Array.from(prevState.errors);
                errors.push(error);
                return {
                    errors
                };
            });
        }

        private async loadInstances(): Promise<void> {
            const instancelist = await InstanceClient.listInstances();
            const modifiedlist: Array<Instance> = [];

            const work: Array<Promise<void>> = [];
            for (const instance of instancelist.payload!) {
                const modifiedinstance = instance as Instance;
                if (instance.online) {
                    work.push(
                        InstanceUserClient.getCurrentInstanceUser(instance.id).then(
                            instanceuser => {
                                if (instanceuser.code == StatusCode.OK) {
                                    modifiedinstance.canAccess = true;
                                } else {
                                    modifiedinstance.canAccess = false;
                                    if (instanceuser.error!.code !== ErrorCode.HTTP_ACCESS_DENIED) {
                                        this.addError(instanceuser.error!);
                                    }
                                }
                                modifiedlist.push(modifiedinstance);
                            }
                        )
                    );
                } else {
                    modifiedinstance.canAccess = false;
                    modifiedlist.push(modifiedinstance);
                }
            }

            await Promise.all(work);

            if (instancelist.code == StatusCode.OK) {
                this.setState({
                    instances: modifiedlist.sort((a, b) => a.id - b.id)
                });
            } else {
                this.addError(instancelist.error!);
            }
        }

        public async componentDidMount(): Promise<void> {
            await this.loadInstances();

            await UserClient.getCurrentUser().then(userinfo => {
                if (userinfo.code === StatusCode.OK) {
                    this.setState({
                        canOnline: !!(
                            userinfo.payload!.instanceManagerRights! &
                            InstanceManagerRights.SetOnline
                        )
                    });
                } else {
                    this.addError(userinfo.error!);
                }
            });

            this.setState({
                loading: false
            });
        }

        private async setOnline(instance: Instance) {
            //Yes this is desynchronized and will use the last known state of the instance
            // to determine what state we should put it in, thats intentional, if the user clicks Set Online, it needs
            // to be online, no matter what it previously was
            const desiredState = !instance.online;
            const instanceedit = await InstanceClient.editInstance(({
                id: instance.id,
                online: desiredState
            } as unknown) as Components.Schemas.Instance);
            if (instanceedit.code === StatusCode.OK) {
                await this.loadInstances();
            } else {
                this.addError(instanceedit.error!);
            }
        }

        public render(): ReactNode {
            if (this.state.loading) {
                return <Loading text="loading.instance.list" />;
            }

            return (
                <div className="text-center">
                    {this.state.errors.map((err, index) => {
                        if (!err) return;
                        return (
                            <ErrorAlert
                                key={index}
                                error={err}
                                onClose={() =>
                                    this.setState(prev => {
                                        const newarr = Array.from(prev.errors);
                                        newarr[index] = undefined;
                                        return {
                                            errors: newarr
                                        };
                                    })
                                }
                            />
                        );
                    })}
                    <h3>
                        <FormattedMessage id="view.instance.list.title" />
                    </h3>
                    <Table striped bordered hover variant="dark" responsive className="mb-4">
                        <thead>
                            <tr>
                                <th>#</th>
                                <th>
                                    <FormattedMessage id="generic.name" />
                                </th>
                                <th>
                                    <FormattedMessage id="generic.online" />
                                </th>
                                <th>
                                    <FormattedMessage id="generic.path" />
                                </th>
                                <th>
                                    <FormattedMessage id="generic.configmode" />
                                </th>
                                <th>
                                    <FormattedMessage id="generic.action" />
                                </th>
                            </tr>
                        </thead>
                        <tbody>
                            {this.state.instances.map(value => {
                                return (
                                    <tr
                                        key={value.id}
                                        className={
                                            value.id === AppCategories.instance.data?.instanceid
                                                ? "font-weight-bold"
                                                : ""
                                        }>
                                        <td>{value.id}</td>
                                        <td>{value.name}</td>
                                        <td>
                                            {value.online! ? (
                                                <Badge variant="success">
                                                    <FormattedMessage id="generic.online" />
                                                </Badge>
                                            ) : (
                                                <Badge variant="danger">
                                                    <FormattedMessage id="generic.offline" />
                                                </Badge>
                                            )}
                                        </td>
                                        <td>
                                            {value.moveJob ? (
                                                <FormattedMessage id="view.instance.moving" />
                                            ) : (
                                                value.path
                                            )}
                                        </td>
                                        <td>
                                            <FormattedMessage
                                                id={`view.instance.configmode.${value.configurationType!.toString()}`}
                                            />
                                        </td>
                                        <td className="align-middle p-0">
                                            <Button
                                                className="mr-1"
                                                onClick={() => {
                                                    if (!AppCategories.instance.data)
                                                        AppCategories.instance.data = {};
                                                    AppCategories.instance.data.instanceid = value.id.toString();
                                                    this.setState({
                                                        instanceid: value.id
                                                    });
                                                }}
                                                disabled={!value.canAccess}>
                                                <FormattedMessage id="generic.select" />
                                            </Button>
                                            <Button
                                                variant={value.online ? "danger" : "success"}
                                                onClick={() => this.setOnline(value)}
                                                disabled={!this.state.canOnline}>
                                                <FormattedMessage
                                                    id={`view.instance.list.set.${
                                                        value.online ? "offline" : "online"
                                                    }`}
                                                />
                                            </Button>
                                        </td>
                                    </tr>
                                );
                            })}
                        </tbody>
                    </Table>
                    <Button
                        className="mr-2"
                        onClick={() => {
                            this.props.history.push(
                                AppRoutes.instancecode.link || AppRoutes.instancecode.route
                            );
                        }}
                        disabled={this.state.instanceid === undefined}>
                        <FormattedMessage id="routes.instancecode" />
                    </Button>
                    <Button
                        className="mr-2"
                        onClick={() => {
                            this.props.history.push(
                                AppRoutes.instancehosting.link || AppRoutes.instancehosting.route
                            );
                        }}
                        disabled={this.state.instanceid === undefined}>
                        <FormattedMessage id="routes.instancehosting" />
                    </Button>
                    <Button
                        className="mr-2"
                        onClick={() => {
                            this.props.history.push(
                                AppRoutes.instancejobs.link || AppRoutes.instancejobs.route
                            );
                        }}
                        disabled={this.state.instanceid === undefined}>
                        <FormattedMessage id="routes.instancejobs" />
                    </Button>
                    <Button
                        onClick={() => {
                            this.props.history.push(
                                AppRoutes.instanceconfig.link || AppRoutes.instanceconfig.route
                            );
                        }}
                        disabled={this.state.instanceid === undefined}>
                        <FormattedMessage id="routes.instanceconfig" />
                    </Button>
                </div>
            );
        }
    }
);
