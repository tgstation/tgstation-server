import React, { ChangeEvent, FormEvent, ReactNode } from "react";
import Button from "react-bootstrap/Button";
import Col from "react-bootstrap/Col";
import Form from "react-bootstrap/Form";
import { FormattedMessage } from "react-intl";
import { RouteComponentProps, withRouter } from "react-router-dom";

import { Components } from "../../../ApiClient/generatedcode/_generated";
import InternalError, { ErrorCode } from "../../../ApiClient/models/InternalComms/InternalError";
import { StatusCode } from "../../../ApiClient/models/InternalComms/InternalStatus";
import ServerClient from "../../../ApiClient/ServerClient";
import UserClient from "../../../ApiClient/UserClient";
import { AppCategories, AppRoutes } from "../../../utils/routes";
import ErrorAlert from "../../utils/ErrorAlert";
import Loading from "../../utils/Loading";

interface IState {
    errors: Array<InternalError<ErrorCode> | undefined>;
    password1: string;
    password2: string;
    username: string;
    sysuser: string;
    matchError?: boolean;
    lengthError?: boolean;
    serverInfo?: Components.Schemas.ServerInformation;
    loading: boolean;
    creating?: boolean;
    redirect?: boolean;
}
interface IProps extends RouteComponentProps {}

export default withRouter(
    class UserCreate extends React.Component<IProps, IState> {
        public constructor(props: IProps) {
            super(props);

            this.state = {
                errors: [],
                password1: "",
                password2: "",
                username: "",
                sysuser: "",
                loading: true
            };

            this.submitTGS = this.submitTGS.bind(this);
            this.submitSYS = this.submitSYS.bind(this);
        }

        public async componentDidMount(): Promise<void> {
            const res = await ServerClient.getServerInfo();

            switch (res.code) {
                case StatusCode.ERROR: {
                    this.addError(res.error!);
                    break;
                }
                case StatusCode.OK: {
                    this.setState({
                        serverInfo: res.payload!
                    });
                    break;
                }
            }

            this.setState({
                loading: false
            });
        }

        // noinspection DuplicatedCode
        private validate(): boolean {
            let err = false;
            if (this.state.password1.length < this.state.serverInfo!.minimumPasswordLength) {
                err = true;
                this.setState({
                    lengthError: true
                });
            } else {
                this.setState({
                    lengthError: false
                });
            }
            if (this.state.password2 !== this.state.password1) {
                err = true;
                this.setState({
                    matchError: true
                });
            } else {
                this.setState({
                    matchError: false
                });
            }
            return err;
        }

        private async submitTGS(event: FormEvent<HTMLFormElement>) {
            event.preventDefault();

            //validation
            if (this.validate()) return;
            if (!this.state.username) return;

            this.setState({
                creating: true
            });

            const user = await UserClient.createUser({
                name: this.state.username,
                password: this.state.password1
            });
            // noinspection DuplicatedCode
            if (user.code == StatusCode.OK) {
                if (!AppCategories.user.data) AppCategories.user.data = {};
                AppCategories.user.data.selectedid = user.payload!.id!;
                this.props.history.push(AppRoutes.useredit.link || AppRoutes.useredit.route);
            } else {
                this.addError(user.error!);
                this.setState({
                    creating: false
                });
            }
        }

        private async submitSYS(event: FormEvent<HTMLFormElement>) {
            event.preventDefault();

            //validation
            if (!this.state.sysuser) return;

            this.setState({
                creating: true
            });

            const user = await UserClient.createUser({
                systemIdentifier: this.state.sysuser
            });
            // noinspection DuplicatedCode
            if (user.code == StatusCode.OK) {
                if (!AppCategories.user.data) AppCategories.user.data = {};
                AppCategories.user.data.selectedid = user.payload!.id!;
                this.props.history.push(AppRoutes.useredit.link || AppRoutes.useredit.route);
            } else {
                this.addError(user.error!);
                this.setState({
                    creating: false
                });
            }
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

        public render(): ReactNode {
            if (this.state.loading) {
                return <Loading text="loading.info" />;
            }
            if (this.state.creating) {
                return <Loading text="loading.user.create" />;
            }

            const handleUsernameInput = (event: ChangeEvent<HTMLInputElement>) =>
                this.setState({ username: event.target.value });
            const handleSysuserInput = (event: ChangeEvent<HTMLInputElement>) =>
                this.setState({ sysuser: event.target.value });
            const handlePwd1Input = (event: ChangeEvent<HTMLInputElement>) =>
                this.setState({ password1: event.target.value });
            const handlePwd2Input = (event: ChangeEvent<HTMLInputElement>) =>
                this.setState({ password2: event.target.value });

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
                        <FormattedMessage id="routes.usercreate" />
                    </h3>
                    <Col className="mx-auto" lg={5} md={8}>
                        <Form onSubmit={this.submitTGS}>
                            <Form.Group controlId="username">
                                <Form.Label>
                                    <FormattedMessage id="login.username" />
                                </Form.Label>
                                <Form.Control
                                    required
                                    onChange={handleUsernameInput}
                                    value={this.state.username}
                                />
                            </Form.Group>
                            <Form.Group controlId="password1">
                                <Form.Label>
                                    <FormattedMessage id="login.password" />
                                </Form.Label>
                                <Form.Control
                                    type="password"
                                    onChange={handlePwd1Input}
                                    value={this.state.password1}
                                    isInvalid={this.state.matchError || this.state.lengthError}
                                />
                                <Form.Control.Feedback type="invalid">
                                    {this.state.lengthError ? (
                                        <React.Fragment>
                                            <FormattedMessage id="login.password.repeat.short" />
                                            {this.state.serverInfo!.minimumPasswordLength}
                                        </React.Fragment>
                                    ) : (
                                        ""
                                    )}
                                </Form.Control.Feedback>
                            </Form.Group>
                            <Form.Group controlId="password2">
                                <Form.Label>
                                    <FormattedMessage id="login.password.repeat" />
                                </Form.Label>
                                <Form.Control
                                    type="password"
                                    onChange={handlePwd2Input}
                                    value={this.state.password2}
                                    isInvalid={this.state.matchError || this.state.lengthError}
                                />
                                <Form.Control.Feedback type="invalid">
                                    {this.state.matchError ? (
                                        <FormattedMessage id="login.password.repeat.match" />
                                    ) : (
                                        ""
                                    )}
                                </Form.Control.Feedback>
                            </Form.Group>
                            <Button type="submit">
                                <FormattedMessage id="view.user.create.tgs" />
                            </Button>
                        </Form>
                        <hr />
                        <Form onSubmit={this.submitSYS}>
                            <Form.Group controlId="sysuser">
                                <Form.Label>
                                    <FormattedMessage id="generic.systemidentifier" />
                                </Form.Label>
                                <Form.Control
                                    required
                                    onChange={handleSysuserInput}
                                    value={this.state.sysuser}
                                />
                            </Form.Group>
                            <Button type="submit">
                                <FormattedMessage id="view.user.create.sys" />
                            </Button>
                        </Form>
                    </Col>
                </div>
            );
        }
    }
);
