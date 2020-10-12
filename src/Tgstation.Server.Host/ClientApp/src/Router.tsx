import loadable, { LoadableComponent } from "@loadable/component";
import { Component, ReactNode } from "react";
import * as React from "react";
import { FormattedMessage } from "react-intl";
import { RouteComponentProps } from "react-router";
import { Route, Switch, withRouter } from "react-router-dom";

import AccessDenied from "./components/utils/AccessDenied";
import ErrorBoundary from "./components/utils/ErrorBoundary";
import Loading from "./components/utils/Loading";
import Reload from "./components/utils/Reload";
import Login from "./components/views/Login";
import { matchesPath } from "./utils/misc";
import RouteController from "./utils/RouteController";
import { AppRoute } from "./utils/routes";

interface IState {
    loading: boolean;
    routes: Array<AppRoute>;
    components: Map<string, LoadableComponent<unknown>>;
}
interface IProps extends RouteComponentProps {
    loggedIn: boolean;
    selectCategory: (category: string) => void;
}

const LoadSpin = (page: string) => (
    <Loading text={"loading.page"}>
        <FormattedMessage id={page} />
    </Loading>
);

const NotFound = loadable(() => import("./components/views/NotFound"), {
    fallback: LoadSpin("loading.page.notfound")
});

export default withRouter(
    class Router extends Component<IProps, IState> {
        public constructor(props: IProps) {
            super(props);

            const components = new Map<string, LoadableComponent<unknown>>();

            const routes = RouteController.getImmediateRoutes(false);
            routes.forEach(route => {
                components.set(
                    route.name,
                    //*should* always be a react component
                    // eslint-disable-next-line @typescript-eslint/no-unsafe-return
                    loadable(() => import(`./components/views/${route.file}`), {
                        fallback: LoadSpin(route.name)
                    })
                );
            });

            this.state = {
                loading: false,
                routes: [],
                components: components
            };
        }

        public async componentDidMount() {
            RouteController.on("refreshAll", routes => {
                this.setState({
                    routes
                });
            });

            this.setState({
                routes: await RouteController.getRoutes(false)
            });

            this.props.history.listen(location => {
                void this.listener(location.pathname);
            });
            await this.listener(this.props.location.pathname);
        }

        private async listener(location: string) {
            const routes = await RouteController.getRoutes(false);
            for (const route of routes) {
                if (route.category && route.navbarLoose && matchesPath(location, route.route)) {
                    this.props.selectCategory(route.category);
                    break;
                }
            }
        }

        public render(): ReactNode {
            return (
                <ErrorBoundary>
                    <Reload>
                        <div>
                            <Switch>
                                {this.state.routes.map(route => {
                                    if (!route.loginless && !this.props.loggedIn) return;

                                    return (
                                        <Route
                                            exact={!route.loose}
                                            path={route.route}
                                            key={route.name}
                                            render={props => {
                                                let Comp;

                                                if (!route.cachedAuth) {
                                                    Comp = AccessDenied;
                                                } else {
                                                    Comp = this.state.components.get(route.name)!;
                                                }

                                                //@ts-expect-error //i cant for the life of me make this shit work so it has to stay like this.
                                                return <Comp {...props} />;
                                            }}
                                        />
                                    );
                                })}
                                <Route key="notfound">
                                    {this.props.loggedIn ? <NotFound /> : <Login />}
                                </Route>
                            </Switch>
                        </div>
                    </Reload>
                </ErrorBoundary>
            );
        }
    }
);
