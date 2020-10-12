import React, { ReactNode } from "react";
import Alert from "react-bootstrap/Alert";
import Button from "react-bootstrap/Button";
import { FormattedMessage } from "react-intl";
import { RouteComponentProps, withRouter } from "react-router";

interface IProps extends RouteComponentProps {}

interface IState {
    auth: boolean;
}

class AccessDenied extends React.Component<IProps, IState> {
    public render(): ReactNode {
        const goBack = () => {
            this.props.history.goBack();
        };
        return (
            <Alert className="clearfix" variant="error">
                <FormattedMessage id="generic.accessdenied" />
                <hr />

                <Button variant="danger" className="float-right" onClick={goBack}>
                    <FormattedMessage id="generic.goback" />
                </Button>
            </Alert>
        );
    }
}

export default withRouter(AccessDenied);
