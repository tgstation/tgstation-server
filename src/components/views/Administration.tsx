import { faLinux } from "@fortawesome/free-brands-svg-icons/faLinux";
import { faWindows } from "@fortawesome/free-brands-svg-icons/faWindows";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import React, { ReactNode } from "react";
import Button from "react-bootstrap/Button";
import Modal from "react-bootstrap/Modal";
import { FormattedMessage } from "react-intl";
import { RouteComponentProps } from "react-router";
import { withRouter } from "react-router-dom";

import AdminClient from "../../ApiClient/AdminClient";
import { AdministrationRights } from "../../ApiClient/generatedcode/_enums";
import { Components } from "../../ApiClient/generatedcode/_generated";
import InternalError, { ErrorCode } from "../../ApiClient/models/InternalComms/InternalError";
import { StatusCode } from "../../ApiClient/models/InternalComms/InternalStatus";
import ServerClient from "../../ApiClient/ServerClient";
import UserClient from "../../ApiClient/UserClient";
import { AppRoutes } from "../../utils/routes";
import ErrorAlert from "../utils/ErrorAlert";
import Loading from "../utils/Loading";

interface IProps extends RouteComponentProps {}
interface IState {
    adminInfo?: Components.Schemas.Administration;
    serverInfo?: Components.Schemas.ServerInformation;
    error?: InternalError<ErrorCode>;
    busy: boolean;
    canReboot: boolean;
    canUpdate: boolean;
    canLogs: boolean;
    showRebootModal?: boolean;
}

export default withRouter(
    class Administration extends React.Component<IProps, IState> {
        public constructor(props: IProps) {
            super(props);
            this.restart = this.restart.bind(this);

            this.state = {
                busy: false,
                canReboot: false,
                canUpdate: false,
                canLogs: false
            };
        }

        public async componentDidMount(): Promise<void> {
            this.setState({
                busy: true
            });
            const tasks = [];

            console.time("DataLoad");
            tasks.push(this.loadAdminInfo());
            tasks.push(this.loadServerInfo());
            tasks.push(this.checkRebootRights());
            tasks.push(this.checkUpdateRights());
            tasks.push(this.checkLogsRights());

            await Promise.all(tasks);
            console.timeEnd("DataLoad");
            this.setState({
                busy: false
            });
        }

        private async loadServerInfo() {
            console.time("ServerLoad");
            const response = await ServerClient.getServerInfo();
            switch (response.code) {
                case StatusCode.ERROR: {
                    this.setState({
                        error: response.error
                    });
                    break;
                }
                case StatusCode.OK: {
                    this.setState({
                        serverInfo: response.payload
                    });
                    break;
                }
            }
            console.timeEnd("ServerLoad");
        }

        private async loadAdminInfo() {
            console.time("AdminLoad");
            const response = await AdminClient.getAdminInfo();
            switch (response.code) {
                case StatusCode.ERROR: {
                    this.setState({
                        error: response.error
                    });
                    break;
                }
                case StatusCode.OK: {
                    this.setState({
                        adminInfo: response.payload
                    });
                    break;
                }
            }
            console.timeEnd("AdminLoad");
        }

        private async checkRebootRights() {
            const response = await UserClient.getCurrentUser();

            if (response.code === StatusCode.OK) {
                this.setState({
                    canReboot: !!(
                        response.payload!.administrationRights! & AdministrationRights.RestartHost
                    )
                });
            }
        }

        private async checkUpdateRights() {
            const response = await UserClient.getCurrentUser();

            if (response.code === StatusCode.OK) {
                this.setState({
                    canUpdate: !!(
                        response.payload!.administrationRights! & AdministrationRights.ChangeVersion
                    )
                });
            }
        }

        private async checkLogsRights() {
            const response = await UserClient.getCurrentUser();

            if (response.code === StatusCode.OK) {
                this.setState({
                    canLogs: !!(
                        response.payload!.administrationRights! & AdministrationRights.DownloadLogs
                    )
                });
            }
        }

        private async restart() {
            this.setState({
                showRebootModal: false,
                busy: true
            });
            console.time("Reboot");
            const response = await AdminClient.restartServer();
            switch (response.code) {
                case StatusCode.ERROR: {
                    this.setState({
                        error: response.error
                    });
                    break;
                }
                case StatusCode.OK: {
                    window.location.reload();
                }
            }
            this.setState({
                busy: false
            });
            console.timeEnd("Reboot");
        }

        public render(): ReactNode {
            if (this.state.busy) {
                return <Loading text="loading.admin" />;
            }

            const handleClose = () => this.setState({ showRebootModal: false });
            const handleOpen = () => this.setState({ showRebootModal: true });

            return (
                <React.Fragment>
                    <ErrorAlert
                        error={this.state.error}
                        onClose={() => this.setState({ error: undefined })}
                    />
                    {this.state.adminInfo && this.state.serverInfo ? (
                        <div className="text-center">
                            <h3 className=" text-secondary">
                                <FormattedMessage id="view.admin.hostos" />
                                <FontAwesomeIcon
                                    fixedWidth
                                    icon={this.state.adminInfo.windowsHost ? faWindows : faLinux}
                                />
                            </h3>
                            <h5 className="text-secondary">
                                <FormattedMessage id="view.admin.remote" />
                                <a href={this.state.adminInfo.trackedRepositoryUrl!}>
                                    {this.state.adminInfo.trackedRepositoryUrl!}
                                </a>
                            </h5>
                            <h3 className="text-secondary">
                                <FormattedMessage id="view.admin.version.current" />
                                <span
                                    className={
                                        this.state.serverInfo.version! <
                                        this.state.adminInfo.latestVersion!
                                            ? "text-danger"
                                            : ""
                                    }>
                                    {this.state.serverInfo.version!}
                                </span>
                            </h3>
                            <h3 className="text-secondary">
                                <FormattedMessage id="view.admin.version.latest" />
                                <span
                                    className={
                                        this.state.serverInfo.version! <
                                        this.state.adminInfo.latestVersion!
                                            ? "text-danger"
                                            : ""
                                    }>
                                    {this.state.adminInfo.latestVersion!}
                                </span>
                            </h3>
                            <hr />
                            <Button
                                className="mr-2"
                                variant="danger"
                                disabled={!this.state.canReboot}
                                onClick={handleOpen}>
                                <FormattedMessage id="view.admin.reboot.button" />
                            </Button>
                            <Button
                                className="mr-2"
                                variant="primary"
                                disabled={!this.state.canUpdate}
                                onClick={() => {
                                    this.props.history.push(
                                        AppRoutes.admin_update.link || AppRoutes.admin_update.route
                                    );
                                }}>
                                <FormattedMessage id="view.admin.update.button" />
                            </Button>
                            <Button
                                variant="primary"
                                disabled={!this.state.canLogs}
                                onClick={() => {
                                    this.props.history.push(
                                        AppRoutes.admin_logs.link || AppRoutes.admin_logs.route
                                    );
                                }}>
                                <FormattedMessage id="view.admin.logs.button" />
                            </Button>
                            <Modal
                                show={this.state.showRebootModal}
                                onHide={handleClose}
                                size="lg"
                                centered>
                                <Modal.Header closeButton>
                                    <Modal.Title>
                                        <FormattedMessage id="view.admin.reboot.modal.title" />
                                    </Modal.Title>
                                </Modal.Header>
                                <Modal.Body>
                                    <FormattedMessage id="view.admin.reboot.modal.body" />
                                </Modal.Body>
                                <Modal.Footer>
                                    <Button onClick={handleClose}>
                                        <FormattedMessage id="generic.close" />
                                    </Button>
                                    <Button variant="danger" onClick={this.restart}>
                                        <FormattedMessage id="view.admin.reboot.button" />
                                    </Button>
                                </Modal.Footer>
                            </Modal>
                        </div>
                    ) : (
                        ""
                    )}
                </React.Fragment>
            );
        }
    }
);
