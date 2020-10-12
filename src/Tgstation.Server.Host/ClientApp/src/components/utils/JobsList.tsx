import React, { ReactNode } from "react";
import { FormattedMessage } from "react-intl";
import { Rnd } from "react-rnd";

import InternalError, { ErrorCode } from "../../ApiClient/models/InternalComms/InternalError";
import configOptions, { jobsWidgetOptions } from "../../ApiClient/util/config";
import JobsController, { CanCancelJob } from "../../ApiClient/util/JobsController";
import { AppCategories } from "../../utils/routes";
import ErrorAlert from "./ErrorAlert";
import JobCard from "./JobCard";
import Loading from "./Loading";

interface IProps {
    width?: string;
    widget: boolean;
}

interface IState {
    jobs: Map<number, CanCancelJob>;
    errors: InternalError<ErrorCode>[];
    ownerrors: Array<InternalError<ErrorCode> | undefined>;
    loading: boolean;
}

export default class JobsList extends React.Component<IProps, IState> {
    public static defaultProps = {
        widget: true
    };

    private widgetRef = React.createRef<HTMLDivElement>();

    public constructor(props: IProps) {
        super(props);

        this.handleUpdate = this.handleUpdate.bind(this);
        this.onCancelorClose = this.onCancelorClose.bind(this);

        this.state = {
            jobs: new Map<number, CanCancelJob>(),
            errors: [],
            ownerrors: [],
            loading: true
        };
    }

    private addError(error: InternalError<ErrorCode>): void {
        this.setState(prevState => {
            const ownerrors = Array.from(prevState.ownerrors);
            ownerrors.push(error);
            if (this.widgetRef.current) {
                this.widgetRef.current.scrollTop = 0;
            }
            return {
                ownerrors
            };
        });
    }

    public componentDidMount(): void {
        JobsController.restartLoop();
        JobsController.on("jobsLoaded", this.handleUpdate);
    }

    public componentWillUnmount(): void {
        JobsController.removeListener("jobsLoaded", this.handleUpdate);
    }

    public handleUpdate(): void {
        this.setState({
            jobs: JobsController.jobs,
            errors: JobsController.errors,
            loading: false
        });
    }

    private async onCancelorClose(job: CanCancelJob) {
        const cancelling = !job.stoppedAt;
        const status = await JobsController.cancelOrClear(job.id, error => this.addError(error));

        if (!status) {
            return;
        }
        //Jobs changed, might as well refresh
        if (cancelling) {
            JobsController.fastmode = 5;
        } else {
            JobsController.restartLoop();
        }
    }

    public render(): ReactNode {
        if (AppCategories.instance.data?.instanceid === undefined) return "";

        if (!this.props.widget) return this.nested();
        return (
            <div
                style={{
                    position: "fixed",
                    top: 0,
                    bottom: 0,
                    right: 0,
                    left: 0,
                    pointerEvents: "none"
                }}>
                <Rnd
                    className={`jobswidget ${
                        //Ensure the option ISNT never, then either see if theres something to display(for auto) or if its just set to always in which case we display it
                        configOptions.jobswidgetdisplay.value !== jobsWidgetOptions.NEVER &&
                        (configOptions.jobswidgetdisplay.value === jobsWidgetOptions.ALWAYS ||
                            this.state.jobs.size ||
                            this.state.errors.length)
                            ? ""
                            : "d-none"
                    }`}
                    style={{
                        pointerEvents: "auto",
                        bottom: 0,
                        right: 0
                    }}
                    default={{
                        width: "30vw",
                        height: "50vh",
                        x:
                            document.documentElement.clientWidth -
                            Math.min(document.documentElement.clientWidth * 0.3, 350),
                        y:
                            document.documentElement.clientHeight -
                            document.documentElement.clientHeight * 0.5
                    }}
                    maxWidth={350}
                    minHeight={50}
                    minWidth={110}
                    bounds="parent">
                    <div className="fancyscroll overflow-auto h-100" ref={this.widgetRef}>
                        <h5 className="text-center text-darker font-weight-bold">
                            <FormattedMessage id="view.instance.jobs.title" />
                        </h5>
                        {this.nested()}
                    </div>
                </Rnd>
            </div>
        );
    }

    private nested(): ReactNode {
        return (
            <div className={this.props.widget ? "d-none d-sm-block" : ""}>
                {this.state.loading ? <Loading text="loading.instance.jobs.list" /> : ""}
                {this.state.ownerrors.map((err, index) => {
                    if (!err) return;
                    return (
                        <ErrorAlert
                            key={index}
                            error={err}
                            onClose={() =>
                                this.setState(prev => {
                                    const newarr = Array.from(prev.ownerrors);
                                    newarr[index] = undefined;
                                    return {
                                        ownerrors: newarr
                                    };
                                })
                            }
                        />
                    );
                })}
                {this.state.errors.map((error, index) => {
                    return (
                        <div key={index} style={{ maxWidth: this.props.widget ? 350 : "unset" }}>
                            <ErrorAlert error={error} />
                        </div>
                    );
                })}
                {Array.from(this.state.jobs, ([, job]) => job)
                    .sort((a, b) => b.id - a.id)
                    .map(job => (
                        <JobCard
                            job={job}
                            width={this.props.width}
                            key={job.id}
                            onClose={this.onCancelorClose}
                        />
                    ))}
            </div>
        );
    }
}
