import React, { ChangeEvent, FormEvent, ReactNode } from "react";
import Button from "react-bootstrap/Button";
import Col from "react-bootstrap/Col";
import Form from "react-bootstrap/Form";
import { FormattedMessage } from "react-intl";
import { RouteComponentProps } from "react-router";
import { withRouter } from "react-router-dom";

import InternalError from "../../ApiClient/models/InternalComms/InternalError";
import { StatusCode } from "../../ApiClient/models/InternalComms/InternalStatus";
import ServerClient, { LoginErrors } from "../../ApiClient/ServerClient";
import { getSavedCreds } from "../../utils/misc";
import ErrorAlert from "../utils/ErrorAlert";
import Loading from "../utils/Loading";

interface IProps extends RouteComponentProps {}
interface IState {
    busy: boolean;
    validated: boolean;
    username: string;
    password: string;
    save: boolean;
    error?: InternalError<LoginErrors>;
}

export default withRouter(
    class Login extends React.Component<IProps, IState> {
        public constructor(props: IProps) {
            super(props);
            this.submit = this.submit.bind(this);

            const [usr, pwd] = getSavedCreds() || [undefined, undefined];

            this.state = {
                busy: false,
                validated: false,
                username: usr || "",
                password: pwd || "",
                save: !!(usr && pwd)
            };
        }

        public render(): ReactNode {
            const handleUsrInput = (event: ChangeEvent<HTMLInputElement>) =>
                this.setState({ username: event.target.value });
            const handlePwdInput = (event: ChangeEvent<HTMLInputElement>) =>
                this.setState({ password: event.target.value });
            const handleSaveInput = (event: ChangeEvent<HTMLInputElement>) =>
                this.setState({ save: event.target.checked });

            if (this.state.busy) {
                return <Loading text="loading.login" />;
            }
            return (
                <Form validated={this.state.validated} onSubmit={this.submit}>
                    <Col className="mx-auto" lg={5} md={8}>
                        <ErrorAlert
                            error={this.state.error}
                            onClose={() => this.setState({ error: undefined })}
                        />
                        <Form.Group controlId="username">
                            <Form.Label>
                                <FormattedMessage id="login.username" />
                            </Form.Label>
                            <Form.Control
                                type="text"
                                onChange={handleUsrInput}
                                value={this.state.username}
                                required
                            />
                        </Form.Group>
                        <Form.Group controlId="password">
                            <Form.Label>
                                <FormattedMessage id="login.password" />
                            </Form.Label>
                            <Form.Control
                                type="password"
                                onChange={handlePwdInput}
                                value={this.state.password}
                                required
                            />
                        </Form.Group>
                        <Form.Group controlId="save">
                            <Form.Check
                                type="checkbox"
                                label="Save password"
                                onChange={handleSaveInput}
                                checked={this.state.save}
                            />
                        </Form.Group>
                        <Button type="submit">
                            <FormattedMessage id="login.submit" />
                        </Button>
                    </Col>
                </Form>
            );
        }

        private async submit(event: FormEvent<HTMLFormElement>) {
            event.preventDefault();
            this.setState({
                busy: true
            });
            const response = await ServerClient.login(
                {
                    userName: this.state.username,
                    password: this.state.password
                },
                this.state.save
            );
            if (response.code == StatusCode.ERROR) {
                this.setState({
                    busy: false,
                    error: response.error
                });
            }
        }
    }
);
