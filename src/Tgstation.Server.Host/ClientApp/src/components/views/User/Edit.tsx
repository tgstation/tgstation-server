import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import React from "react";
import Alert from "react-bootstrap/Alert";
import Badge from "react-bootstrap/Badge";
import Button from "react-bootstrap/Button";
import Col from "react-bootstrap/Col";
import Form from "react-bootstrap/Form";
import InputGroup from "react-bootstrap/InputGroup";
import OverlayTrigger from "react-bootstrap/OverlayTrigger";
import Row from "react-bootstrap/Row";
import Tab from "react-bootstrap/Tab";
import Tabs from "react-bootstrap/Tabs";
import Tooltip from "react-bootstrap/Tooltip";
import { FormattedMessage, FormattedRelativeTime } from "react-intl";
import { RouteComponentProps, withRouter } from "react-router";
import { Link } from "react-router-dom";

import {
    AdministrationRights,
    InstanceManagerRights
} from "../../../ApiClient/generatedcode/_enums";
import { Components } from "../../../ApiClient/generatedcode/_generated";
import InternalError, { ErrorCode } from "../../../ApiClient/models/InternalComms/InternalError";
import { StatusCode } from "../../../ApiClient/models/InternalComms/InternalStatus";
import UserClient from "../../../ApiClient/UserClient";
import { AppCategories, AppRoutes } from "../../../utils/routes";
import ErrorAlert from "../../utils/ErrorAlert";
import Loading from "../../utils/Loading";

interface IProps extends RouteComponentProps<{ id: string; tab?: string }> {}

interface IState {
    errors: Array<InternalError<ErrorCode> | undefined>;
    user?: Components.Schemas.User;
    loading: boolean;
    saving: boolean;
    permsadmin: { [key: string]: Permission };
    permsinstance: { [key: string]: Permission };
    canEdit: boolean;
    //override for if its the current user and it can edit its own password
    canEditOwnPassword: boolean;
}

interface Permission {
    readonly bitflag: number;
    readonly currentVal: boolean;
}

export default withRouter(
    class UserEdit extends React.Component<IProps, IState> {
        public constructor(props: IProps) {
            super(props);

            this.state = {
                errors: [],
                loading: true,
                saving: false,
                permsadmin: {},
                permsinstance: {},
                canEdit: false,
                canEditOwnPassword: false
            };

            if (!AppCategories.user.data) AppCategories.user.data = {};
            AppCategories.user.data.selectedid = props.match.params.id;
            AppCategories.user.data.tab = props.match.params.tab;
        }

        public async componentDidMount(): Promise<void> {
            const userid = parseInt(this.props.match.params.id);
            const response = await UserClient.getUser(userid);
            switch (response.code) {
                case StatusCode.ERROR: {
                    this.addError(response.error!);
                    break;
                }
                case StatusCode.OK: {
                    this.loadUser(response.payload!);
                    break;
                }
            }
            const currentuser = await UserClient.getCurrentUser();
            if (currentuser.code == StatusCode.OK) {
                this.setState({
                    canEdit: !!(
                        currentuser.payload!.administrationRights! & AdministrationRights.WriteUsers
                    ),
                    canEditOwnPassword:
                        !!(
                            currentuser.payload!.administrationRights! &
                            AdministrationRights.EditOwnPassword
                        ) && currentuser.payload!.id! === userid
                });
            }
            this.setState({
                loading: false
            });
        }

        private loadUser(user: Components.Schemas.User) {
            this.setState({
                user
            });
            this.loadEnums();
        }

        private loadEnums(): void {
            // noinspection DuplicatedCode
            Object.entries(AdministrationRights).forEach(([k, v]) => {
                /* enums are objects that are reverse mapped, for example, an enum with a = 1 and b = 2 would look like this:
                 * {
                 *   "a": 1,
                 *   "b": 2,
                 *   1: "a",
                 *   2: "b"
                 * }
                 * so we need to drop everything that doesnt resolve to a string because otherwise we end up with twice the values
                 */
                if (!isNaN(parseInt(k))) return;

                const key = k.toLowerCase();
                const val = v as number;

                //we dont care about nothing
                if (key == "none") return;

                const currentVal = !!(this.state.user!.administrationRights! & val);
                this.setState(prevState => {
                    return {
                        permsadmin: {
                            ...prevState.permsadmin,
                            [key]: {
                                currentVal,
                                newVal: currentVal,
                                bitflag: val
                            }
                        }
                    };
                });
            });
            // noinspection DuplicatedCode
            Object.entries(InstanceManagerRights).forEach(([k, v]) => {
                if (!isNaN(parseInt(k))) return;

                const key = k.toLowerCase();
                const val = v as number;

                //we dont care about nothing
                if (key == "none") return;

                const currentVal = !!(this.state.user!.instanceManagerRights! & val);
                this.setState(prevState => {
                    return {
                        permsinstance: {
                            ...prevState.permsinstance,
                            [key]: {
                                currentVal,
                                newVal: currentVal,
                                bitflag: val
                            }
                        }
                    };
                });
            });
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

        public render(): React.ReactNode {
            if (this.state.loading) {
                return <Loading text="loading.user.load" />;
            }
            if (this.state.saving) {
                return <Loading text="loading.user.save" />;
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
                    {this.state.user ? (
                        <React.Fragment>
                            {!this.state.canEdit ? (
                                <Alert className="clearfix" variant="error">
                                    <FormattedMessage id="view.user.edit.cantedit" />
                                </Alert>
                            ) : (
                                ""
                            )}
                            {this.state.user.systemIdentifier ? (
                                <Badge variant="primary">
                                    <FormattedMessage id="generic.system.short" />
                                </Badge>
                            ) : (
                                <Badge variant="primary">
                                    <FormattedMessage id="generic.tgs" />
                                </Badge>
                            )}{" "}
                            {this.state.user.enabled ? (
                                <Badge variant="success">
                                    <FormattedMessage id="generic.enabled" />
                                </Badge>
                            ) : (
                                <Badge variant="danger">
                                    <FormattedMessage id="generic.disabled" />
                                </Badge>
                            )}
                            <h3 className="text-capitalize">{this.state.user.name}</h3>
                            <Button
                                as={Link}
                                to={AppRoutes.userlist.link || AppRoutes.userlist.route}>
                                <FormattedMessage id="generic.goback" />
                            </Button>
                            <Tabs
                                activeKey={this.props.match.params.tab || "info"}
                                onSelect={newkey => {
                                    if (!newkey) return;

                                    if (!AppCategories.user.data) AppCategories.user.data = {};
                                    AppCategories.user.data.selectedid = this.props.match.params.id;
                                    AppCategories.user.data.tab = newkey;
                                    this.props.history.push(
                                        AppRoutes.useredit.link || AppRoutes.useredit.route
                                    );
                                }}
                                id="test"
                                className="justify-content-center mb-3 mt-4 flex-column flex-md-row">
                                <Tab eventKey="info" title={<FormattedMessage id="generic.info" />}>
                                    {this.props.match.params.tab === "info" ||
                                    this.props.match.params.tab === undefined ? (
                                        <Col lg={5} className="text-center text-md-left mx-auto">
                                            <Row xs={1} md={2}>
                                                <Col>
                                                    <h5 className="m-0">
                                                        <FormattedMessage id="generic.userid" />
                                                    </h5>
                                                </Col>
                                                <Col className="text-capitalize mb-2">
                                                    {this.state.user.id}
                                                </Col>
                                            </Row>
                                            {this.state.user.systemIdentifier ? (
                                                <Row xs={1} md={2}>
                                                    <Col>
                                                        <h5 className="m-0">
                                                            <FormattedMessage id="generic.systemidentifier" />
                                                        </h5>
                                                    </Col>
                                                    <Col className="mb-2 text-sm-break">
                                                        {this.state.user.systemIdentifier}
                                                    </Col>
                                                </Row>
                                            ) : (
                                                ""
                                            )}
                                            <Row xs={1} md={2}>
                                                <Col>
                                                    <h5 className="m-0">
                                                        <FormattedMessage id="generic.enabled" />
                                                    </h5>
                                                </Col>
                                                <Col className="text-capitalize mb-2">
                                                    {this.state.user.enabled!.toString()}
                                                </Col>
                                            </Row>
                                            <Row xs={1} md={2}>
                                                <Col>
                                                    <h5 className="m-0">
                                                        <FormattedMessage id="generic.created" />
                                                    </h5>
                                                </Col>
                                                <OverlayTrigger
                                                    overlay={
                                                        <Tooltip
                                                            id={`${this.state.user.name}-tooltip`}>
                                                            {new Date(
                                                                this.state.user.createdAt!
                                                            ).toLocaleString()}
                                                        </Tooltip>
                                                    }>
                                                    {({ ref, ...triggerHandler }) => (
                                                        <Col
                                                            className="text-capitalize mb-2"
                                                            {...triggerHandler}>
                                                            <span
                                                                ref={
                                                                    ref as React.Ref<
                                                                        HTMLSpanElement
                                                                    >
                                                                }>
                                                                <FormattedRelativeTime
                                                                    value={
                                                                        (new Date(
                                                                            this.state.user!.createdAt!
                                                                        ).getTime() -
                                                                            Date.now()) /
                                                                        1000
                                                                    }
                                                                    numeric="auto"
                                                                    updateIntervalInSeconds={1}
                                                                />
                                                            </span>
                                                        </Col>
                                                    )}
                                                </OverlayTrigger>
                                            </Row>
                                            <Row xs={1} md={2}>
                                                <Col>
                                                    <h5 className="m-0">
                                                        <FormattedMessage id="generic.createdby" />
                                                    </h5>
                                                </Col>
                                                <OverlayTrigger
                                                    overlay={
                                                        <Tooltip
                                                            id={`${this.state.user.name}-tooltip-createdby`}>
                                                            <FormattedMessage id="generic.userid" />
                                                            {this.state.user.createdBy!.id}
                                                        </Tooltip>
                                                    }>
                                                    {({ ref, ...triggerHandler }) => (
                                                        <Col
                                                            className="text-capitalize mb-2"
                                                            {...triggerHandler}>
                                                            <span
                                                                ref={
                                                                    ref as React.Ref<
                                                                        HTMLSpanElement
                                                                    >
                                                                }>
                                                                {this.state.user!.createdBy!.name}
                                                            </span>
                                                        </Col>
                                                    )}
                                                </OverlayTrigger>
                                            </Row>
                                            <div className="text-center mt-3">
                                                {this.state.canEdit ||
                                                this.state.canEditOwnPassword ? (
                                                    <Button
                                                        className="mr-2"
                                                        as={Link}
                                                        to={
                                                            (AppRoutes.passwd.link ||
                                                                AppRoutes.passwd.route) +
                                                            this.state.user.id!.toString()
                                                        }>
                                                        <FormattedMessage id="routes.passwd" />
                                                    </Button>
                                                ) : (
                                                    ""
                                                )}
                                                {this.state.canEdit ? (
                                                    <Button
                                                        variant={
                                                            this.state.user.enabled
                                                                ? "danger"
                                                                : "success"
                                                        }
                                                        onClick={async () => {
                                                            this.setState({
                                                                saving: true
                                                            });

                                                            const response = await UserClient.editUser(
                                                                this.state.user!.id!,
                                                                {
                                                                    enabled: !this.state.user!
                                                                        .enabled!
                                                                }
                                                            );
                                                            if (response.code == StatusCode.OK) {
                                                                this.loadUser(response.payload!);
                                                            } else {
                                                                this.addError(response.error!);
                                                            }

                                                            this.setState({
                                                                saving: false
                                                            });
                                                        }}>
                                                        <FormattedMessage
                                                            id={
                                                                this.state.user.enabled
                                                                    ? "generic.disable"
                                                                    : "generic.enable"
                                                            }
                                                        />
                                                    </Button>
                                                ) : (
                                                    ""
                                                )}
                                            </div>
                                        </Col>
                                    ) : (
                                        ""
                                    )}
                                </Tab>
                                <Tab
                                    eventKey="adminperms"
                                    title={<FormattedMessage id="perms.admin" />}>
                                    {this.props.match.params.tab === "adminperms"
                                        ? this.renderPerms("permsadmin", "admin")
                                        : ""}
                                </Tab>
                                <Tab
                                    eventKey="instanceperms"
                                    title={<FormattedMessage id="perms.instance" />}>
                                    {this.props.match.params.tab === "instanceperms"
                                        ? this.renderPerms("permsinstance", "instance")
                                        : ""}
                                </Tab>
                            </Tabs>
                        </React.Fragment>
                    ) : (
                        ""
                    )}
                </div>
            );
        }

        private renderPerms(
            enumname: "permsadmin" | "permsinstance",
            permprefix: string
        ): React.ReactNode {
            const inputs: Record<string, React.RefObject<HTMLInputElement>> = {};
            const setAll = (val: boolean): (() => void) => {
                return () => {
                    for (const ref of Object.values(inputs)) {
                        if (ref.current) ref.current.checked = val;
                    }
                };
            };
            const resetAll = () => {
                for (const [permname, ref] of Object.entries(inputs)) {
                    if (!ref.current) continue;

                    ref.current.checked = this.state[enumname][permname].currentVal;
                }
            };
            const save = async () => {
                this.setState({
                    saving: true
                });
                let bitflag = 0;

                for (const [permname, ref] of Object.entries(inputs)) {
                    if (!ref.current) continue;

                    bitflag += ref.current.checked ? this.state[enumname][permname].bitflag : 0;
                }

                const response = await UserClient.editUser(this.state.user!.id!, {
                    [enumname == "permsadmin"
                        ? "administrationRights"
                        : "instanceManagerRights"]: bitflag
                } as { administrationRights: AdministrationRights } | { instanceManagerRights: InstanceManagerRights });
                if (response.code == StatusCode.OK) {
                    this.loadUser(response.payload!);
                } else {
                    this.addError(response.error!);
                }
                this.setState({
                    saving: false
                });
            };
            return (
                <React.Fragment>
                    {this.state.canEdit ? (
                        <React.Fragment>
                            <h5>
                                <FormattedMessage id="generic.setall" />
                            </h5>
                            <Button onClick={setAll(true)}>
                                <FormattedMessage id="generic.true" />
                            </Button>{" "}
                            <Button onClick={setAll(false)}>
                                <FormattedMessage id="generic.false" />
                            </Button>{" "}
                            <Button onClick={resetAll}>
                                <FormattedMessage id="generic.reset" />
                            </Button>
                        </React.Fragment>
                    ) : (
                        ""
                    )}
                    <Col md={8} lg={7} xl={6} className="mx-auto">
                        <hr />
                        {Object.entries(this.state[enumname]).map(([perm, value]) => {
                            const inputref = React.createRef<HTMLInputElement>();
                            inputs[perm] = inputref;
                            return (
                                <InputGroup key={perm} as="label" htmlFor={perm} className="mb-0">
                                    <InputGroup.Prepend className="flex-grow-1 overflow-auto">
                                        <OverlayTrigger
                                            overlay={
                                                <Tooltip id={`perms.${permprefix}.${perm}.desc`}>
                                                    <FormattedMessage
                                                        id={`perms.${permprefix}.${perm}.desc`}
                                                    />
                                                </Tooltip>
                                            }>
                                            {({ ref, ...triggerHandler }) => (
                                                <InputGroup.Text className="flex-fill">
                                                    <div {...triggerHandler}>
                                                        <FormattedMessage
                                                            id={`perms.${permprefix}.${perm}`}
                                                        />
                                                    </div>
                                                    <div className="ml-auto d-flex align-items-center">
                                                        <Form.Check
                                                            inline
                                                            type="switch"
                                                            custom
                                                            id={perm}
                                                            className="d-flex justify-content-center align-content-center mx-2"
                                                            label=""
                                                            ref={inputref}
                                                            disabled={!this.state.canEdit}
                                                            defaultChecked={value.currentVal}
                                                        />
                                                        <div
                                                            {...triggerHandler}
                                                            ref={ref as React.Ref<HTMLDivElement>}>
                                                            <FontAwesomeIcon
                                                                fixedWidth
                                                                icon="info"
                                                            />
                                                        </div>
                                                    </div>
                                                </InputGroup.Text>
                                            )}
                                        </OverlayTrigger>
                                    </InputGroup.Prepend>
                                </InputGroup>
                            );
                        })}
                        <hr />
                    </Col>
                    {this.state.canEdit ? (
                        <Button onClick={save}>
                            <FormattedMessage id="generic.savepage" />
                        </Button>
                    ) : (
                        ""
                    )}
                </React.Fragment>
            );
        }
    }
);
