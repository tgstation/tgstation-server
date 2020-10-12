import React, { Component } from "react";
import Button from "react-bootstrap/Button";
import OverlayTrigger from "react-bootstrap/OverlayTrigger";
import Table from "react-bootstrap/Table";
import Tooltip from "react-bootstrap/Tooltip";
import { FormattedMessage, FormattedRelativeTime } from "react-intl";
import { RouteComponentProps, withRouter } from "react-router";
import { Link } from "react-router-dom";

import AdminClient from "../../../ApiClient/AdminClient";
import { Components } from "../../../ApiClient/generatedcode/_generated";
import InternalError, { ErrorCode } from "../../../ApiClient/models/InternalComms/InternalError";
import { StatusCode } from "../../../ApiClient/models/InternalComms/InternalStatus";
import { download } from "../../../utils/misc";
import { AppRoutes } from "../../../utils/routes";
import ErrorAlert from "../../utils/ErrorAlert";
import Loading from "../../utils/Loading";

interface IProps extends RouteComponentProps<{ name: string | undefined }> {}

interface LogEntry {
    time: string;
    content: string;
}

interface Log {
    logFile: Components.Schemas.LogFile;
    entries: LogEntry[];
}

interface IState {
    logs: Components.Schemas.LogFile[];
    viewedLog?: Log;
    errors: Array<InternalError<ErrorCode> | undefined>;
    loading: boolean;
}

export default withRouter(
    class Logs extends Component<IProps, IState> {
        public constructor(props: IProps) {
            super(props);

            this.state = {
                errors: [],
                loading: true,
                logs: []
            };
        }
        public async componentDidMount(): Promise<void> {
            const param = this.props.match.params.name;
            if (param) {
                const res = await AdminClient.getLog(param);

                switch (res.code) {
                    case StatusCode.OK: {
                        const regex = RegExp(
                            /(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}.\d{7}-\d{2}:\d{2}) {2}(.*?)(?=(?:\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}.\d{7}-\d{2}:\d{2}))/,
                            "gs"
                        );
                        let match;
                        const entries: LogEntry[] = [];
                        while ((match = regex.exec(atob(res.payload!.content!))) !== null) {
                            entries.push({
                                time: match[1],
                                content: match[2]
                            });
                        }
                        this.setState({
                            viewedLog: {
                                logFile: res.payload!,
                                entries: entries
                            }
                        });
                        break;
                    }
                    case StatusCode.ERROR: {
                        this.addError(res.error!);
                        break;
                    }
                }
            }
            const response = await AdminClient.getLogs();

            switch (response.code) {
                case StatusCode.OK: {
                    this.setState({
                        logs: response.payload!
                    });
                    break;
                }
                case StatusCode.ERROR: {
                    this.addError(response.error!);
                    break;
                }
            }
            this.setState({
                loading: false
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

        private async downloadLog(name: string): Promise<void> {
            const res = await AdminClient.getLog(name);
            switch (res.code) {
                case StatusCode.OK: {
                    download(name, atob(res.payload!.content!));
                    break;
                }
                case StatusCode.ERROR: {
                    this.addError(res.error!);
                    break;
                }
            }
        }

        public render(): React.ReactNode {
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
                    {this.state.loading ? (
                        <Loading text="loading.logs" />
                    ) : this.props.match.params.name && this.state.viewedLog ? (
                        <React.Fragment>
                            <h3>{this.props.match.params.name}</h3>
                            <Button
                                className="mr-1"
                                as={Link}
                                to={AppRoutes.admin_logs.link || AppRoutes.admin_logs.route}>
                                <FormattedMessage id="generic.goback" />
                            </Button>
                            <Button
                                onClick={() => {
                                    download(
                                        this.props.match.params.name!,
                                        atob(this.state.viewedLog!.logFile.content!)
                                    );
                                }}>
                                <FormattedMessage id="generic.download" />
                            </Button>
                            <hr />
                            <Table responsive striped hover variant="dark" className="text-left">
                                <thead>
                                    <th>
                                        <FormattedMessage id="generic.datetime" />
                                    </th>
                                    <th>
                                        <FormattedMessage id="generic.entry" />
                                    </th>
                                </thead>
                                <tbody>
                                    {this.state.viewedLog.entries.map(value => {
                                        return (
                                            <tr key={value.time}>
                                                <td className="py-1">{value.time}</td>
                                                <td className="py-1">
                                                    <pre className="mb-0">{value.content}</pre>
                                                </td>
                                            </tr>
                                        );
                                    })}
                                </tbody>
                            </Table>
                        </React.Fragment>
                    ) : (
                        <Table striped bordered hover variant="dark" responsive>
                            <thead>
                                <tr>
                                    <th>#</th>
                                    <th>
                                        <FormattedMessage id="generic.name" />
                                    </th>
                                    <th>
                                        <FormattedMessage id="generic.datetime" />
                                    </th>
                                    <th>
                                        <FormattedMessage id="generic.action" />
                                    </th>
                                </tr>
                            </thead>
                            <tbody>
                                {this.state.logs.map((value, index) => {
                                    const logdate = new Date(value.lastModified);
                                    const logdiff = (logdate.getTime() - Date.now()) / 1000;

                                    return (
                                        //yes hello this shouldnt be nullable apparently
                                        <tr key={value.name!}>
                                            <td>{index}</td>
                                            <td>{value.name}</td>
                                            <OverlayTrigger
                                                overlay={
                                                    <Tooltip id={`${value.name!}-tooltip`}>
                                                        {logdate.toLocaleString()}
                                                    </Tooltip>
                                                }>
                                                {({ ref, ...triggerHandler }) => (
                                                    <td {...triggerHandler}>
                                                        <span
                                                            ref={ref as React.Ref<HTMLSpanElement>}>
                                                            <FormattedRelativeTime
                                                                value={logdiff}
                                                                numeric="auto"
                                                                updateIntervalInSeconds={1}
                                                            />
                                                        </span>
                                                    </td>
                                                )}
                                            </OverlayTrigger>
                                            <td className="align-middle p-0">
                                                <Button
                                                    className="mr-1"
                                                    onClick={() => {
                                                        this.props.history.push(
                                                            (AppRoutes.admin_logs.link ||
                                                                AppRoutes.admin_logs.route) +
                                                                value.name! +
                                                                "/",
                                                            {
                                                                reload: true
                                                            }
                                                        );
                                                    }}>
                                                    <FormattedMessage id="generic.view" />
                                                </Button>
                                                <Button
                                                    onClick={() => {
                                                        this.downloadLog(value.name!).catch(
                                                            (e: Error) => {
                                                                this.addError(
                                                                    new InternalError<
                                                                        ErrorCode.APP_FAIL
                                                                    >(ErrorCode.APP_FAIL, {
                                                                        jsError: e
                                                                    })
                                                                );
                                                            }
                                                        );
                                                    }}>
                                                    <FormattedMessage id="generic.download" />
                                                </Button>
                                            </td>
                                        </tr>
                                    );
                                })}
                            </tbody>
                        </Table>
                    )}
                </div>
            );
        }
    }
);
