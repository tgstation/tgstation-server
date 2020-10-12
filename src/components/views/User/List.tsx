import React from "react";
import Alert from "react-bootstrap/Alert";
import Badge from "react-bootstrap/Badge";
import Button from "react-bootstrap/Button";
import OverlayTrigger from "react-bootstrap/OverlayTrigger";
import Table from "react-bootstrap/Table";
import Tooltip from "react-bootstrap/Tooltip";
import { FormattedMessage, FormattedRelativeTime } from "react-intl";
import { Link, RouteComponentProps, withRouter } from "react-router-dom";

import { AdministrationRights } from "../../../ApiClient/generatedcode/_enums";
import { Components } from "../../../ApiClient/generatedcode/_generated";
import InternalError, { ErrorCode } from "../../../ApiClient/models/InternalComms/InternalError";
import { StatusCode } from "../../../ApiClient/models/InternalComms/InternalStatus";
import UserClient from "../../../ApiClient/UserClient";
import { AppCategories, AppRoutes } from "../../../utils/routes";
import ErrorAlert from "../../utils/ErrorAlert";
import Loading from "../../utils/Loading";

interface IProps extends RouteComponentProps {}

interface IState {
    errors: Array<InternalError<ErrorCode> | undefined>;
    users: Components.Schemas.User[];
    loading: boolean;
    canList: boolean;
}

export default withRouter(
    class UserList extends React.Component<IProps, IState> {
        public constructor(props: IProps) {
            super(props);

            this.state = {
                errors: [],
                users: [],
                loading: true,
                canList: false
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

        public async componentDidMount(): Promise<void> {
            const response = await UserClient.getCurrentUser();
            if (response.code == StatusCode.OK) {
                const canList = !!(
                    response.payload!.administrationRights! & AdministrationRights.ReadUsers
                );
                this.setState({
                    canList
                });

                if (canList) {
                    const res = await UserClient.listUsers();
                    switch (res.code) {
                        case StatusCode.OK: {
                            this.setState({
                                users: res.payload!
                            });
                            break;
                        }
                        case StatusCode.ERROR: {
                            this.addError(res.error!);
                        }
                    }
                } else {
                    //if we cant list users, add our own user to the list
                    this.setState({
                        users: [response.payload!]
                    });
                }
            } else {
                this.addError(response.error!);
            }
            this.setState({
                loading: false
            });
        }

        public render(): React.ReactNode {
            if (this.state.loading) {
                return <Loading text="loading.userlist" />;
            }
            return (
                <div className="text-center">
                    {!this.state.canList ? (
                        <Alert className="clearfix" variant="error">
                            <FormattedMessage id="view.user.list.cantlist" />
                        </Alert>
                    ) : (
                        ""
                    )}
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
                    <Table striped bordered hover variant="dark" responsive>
                        <thead>
                            <tr>
                                <th>#</th>
                                <th>
                                    <FormattedMessage id="generic.name" />
                                </th>
                                <th>
                                    <FormattedMessage id="generic.details" />
                                </th>
                                <th>
                                    <FormattedMessage id="generic.created" />
                                </th>
                                <th>
                                    <FormattedMessage id="generic.createdby" />
                                </th>
                                <th>
                                    <FormattedMessage id="generic.action" />
                                </th>
                            </tr>
                        </thead>
                        <tbody>
                            {this.state.users.map(value => {
                                const createddate = new Date(value.createdAt!);
                                const createddiff = (createddate.getTime() - Date.now()) / 1000;

                                return (
                                    <tr key={value.id!}>
                                        <td>{value.id!}</td>
                                        <td>{value.name}</td>
                                        <td>
                                            {value.systemIdentifier! ? (
                                                <Badge variant="primary">
                                                    <FormattedMessage id="generic.system.short" />
                                                </Badge>
                                            ) : (
                                                <Badge variant="primary">
                                                    <FormattedMessage id="generic.tgs" />
                                                </Badge>
                                            )}{" "}
                                            {value.enabled! ? (
                                                <Badge variant="success">
                                                    <FormattedMessage id="generic.enabled" />
                                                </Badge>
                                            ) : (
                                                <Badge variant="danger">
                                                    <FormattedMessage id="generic.disabled" />
                                                </Badge>
                                            )}
                                        </td>
                                        <OverlayTrigger
                                            overlay={
                                                <Tooltip id={`${value.name}-tooltip`}>
                                                    {createddate.toLocaleString()}
                                                </Tooltip>
                                            }>
                                            {({ ref, ...triggerHandler }) => (
                                                <td {...triggerHandler}>
                                                    <span ref={ref as React.Ref<HTMLSpanElement>}>
                                                        <FormattedRelativeTime
                                                            value={createddiff}
                                                            numeric="auto"
                                                            updateIntervalInSeconds={1}
                                                        />
                                                    </span>
                                                </td>
                                            )}
                                        </OverlayTrigger>
                                        <OverlayTrigger
                                            overlay={
                                                <Tooltip id={`${value.name}-tooltip-createdby`}>
                                                    <FormattedMessage id="generic.userid" />
                                                    {value.createdBy!.id}
                                                </Tooltip>
                                            }>
                                            {({ ref, ...triggerHandler }) => (
                                                <td {...triggerHandler}>
                                                    <span ref={ref as React.Ref<HTMLSpanElement>}>
                                                        {value.createdBy!.name}
                                                    </span>
                                                </td>
                                            )}
                                        </OverlayTrigger>
                                        <td className="align-middle p-0">
                                            <Button
                                                onClick={() => {
                                                    if (!AppCategories.user.data)
                                                        AppCategories.user.data = {};
                                                    AppCategories.user.data.selectedid = value.id!.toString();
                                                    this.props.history.push(
                                                        AppRoutes.useredit.link ||
                                                            AppRoutes.useredit.route
                                                    );
                                                }}>
                                                <FormattedMessage id="generic.edit" />
                                            </Button>
                                        </td>
                                    </tr>
                                );
                            })}
                        </tbody>
                    </Table>
                    <Button as={Link} to={AppRoutes.usercreate.link || AppRoutes.usercreate.route}>
                        <FormattedMessage id="routes.usercreate" />
                    </Button>
                </div>
            );
        }
    }
);
