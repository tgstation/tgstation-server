import { Component, ReactNode } from "react";
import { RouteComponentProps, withRouter } from "react-router";

interface IProps
    extends RouteComponentProps<
        Record<string, string>,
        {
            statusCode?: number;
        },
        { reload?: boolean }
    > {}
interface IState {
    clear: boolean;
}

class Reload extends Component<IProps, IState> {
    public constructor(props: IProps) {
        super(props);
        this.state = {
            clear: false
        };
    }
    public componentDidUpdate(prevProps: IProps): void {
        if (this.state.clear) {
            this.setState({
                clear: false
            });
            return;
        }
        if (
            prevProps.match.path == this.props.match.path &&
            prevProps.location.key != this.props.location.key &&
            this.props.location.state?.reload
        ) {
            this.setState({
                clear: true
            });
        }
    }

    public render(): ReactNode {
        return this.state.clear ? "" : this.props.children;
    }
}

export default withRouter(Reload);
