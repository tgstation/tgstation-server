import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import * as React from "react";
import Button from "react-bootstrap/Button";
import Dropdown from "react-bootstrap/Dropdown";
import Nav from "react-bootstrap/Nav";
import Navbar from "react-bootstrap/Navbar";
import { FormattedMessage } from "react-intl";
import { RouteComponentProps, withRouter } from "react-router";
import CSSTransition from "react-transition-group/CSSTransition";

import { Components } from "../ApiClient/generatedcode/_generated";
import InternalError, { GenericErrors } from "../ApiClient/models/InternalComms/InternalError";
import ServerClient, { ServerInfoErrors } from "../ApiClient/ServerClient";
import UserClient from "../ApiClient/UserClient";
import LoginHooks from "../ApiClient/util/LoginHooks";
import { matchesPath } from "../utils/misc";
import RouteController from "../utils/RouteController";
import { AppCategories, AppRoute, AppRoutes } from "../utils/routes";
import ErrorAlert from "./utils/ErrorAlert";

interface IProps extends RouteComponentProps {
    category?: {
        name: string;
        key: string;
    };
}

interface IState {
    currentUser?: Components.Schemas.User;
    serverInformation?: Components.Schemas.ServerInformation;
    userNameError?: InternalError<GenericErrors>;
    serverInfoError?: InternalError<ServerInfoErrors>;
    loggedIn: boolean;
    //so we dont actually use the routes but it allows us to make react update the component
    routes: AppRoute[];
    categories: AppCategories;
    focusedCategory: string;
    focusTimer?: number;
}

export default withRouter(
    class AppNavbar extends React.Component<IProps, IState> {
        public constructor(props: IProps) {
            super(props);
            this.logoutClick = this.logoutClick.bind(this);
            this.loadServerInformation = this.loadServerInformation.bind(this);
            this.loadUserInformation = this.loadUserInformation.bind(this);

            this.state = {
                loggedIn: false,
                routes: [],
                categories: RouteController.getCategories(),
                focusedCategory: this.props.category?.name || ""
            };
        }

        //mamamia, memory leaks go brrrrrrrrrrrr dont they?
        //well you see, this component never gets normally unloaded so i dont give a fuck!
        public async componentDidMount(): Promise<void> {
            LoginHooks.addHook(this.loadServerInformation);
            ServerClient.on("loadServerInfo", serverInfo => {
                this.setState({
                    serverInformation: serverInfo.payload
                });
            });

            LoginHooks.addHook(this.loadUserInformation);
            UserClient.on("loadUserInfo", user => {
                this.setState({
                    currentUser: user.payload
                });
            });

            LoginHooks.on("loginSuccess", () => {
                this.setState({
                    loggedIn: true
                });
            });
            ServerClient.on("logout", () => {
                this.setState({
                    loggedIn: false
                });
            });

            this.setState({
                routes: await RouteController.getRoutes()
            });

            RouteController.on("refresh", routes => {
                this.setState({
                    routes
                });
            });
        }

        public componentDidUpdate(prevProps: Readonly<IProps>) {
            if (
                this.props.category !== undefined &&
                this.props.category.key !== prevProps.category?.key
            ) {
                this.setState({
                    focusedCategory: this.props.category.name
                });
            }
        }

        public render(): React.ReactNode {
            return (
                <React.Fragment>
                    <Navbar
                        className="shadow-lg"
                        expand={this.state.loggedIn ? "lg" : undefined}
                        collapseOnSelect
                        variant="dark"
                        bg={
                            this.state.userNameError || this.state.serverInfoError
                                ? "danger"
                                : "primary"
                        }>
                        <Navbar.Brand
                            onClick={() => {
                                this.props.history.push(
                                    AppRoutes.home.link || AppRoutes.home.route,
                                    {
                                        reload: true
                                    }
                                );
                            }}
                            className="mr-auto">
                            {this.renderVersion()}
                        </Navbar.Brand>
                        <Navbar.Toggle className="mr-2" aria-controls="responsive-navbar-nav" />
                        <Navbar.Collapse className="text-right mr-2" style={{ minWidth: "0px" }}>
                            <Nav className="mr-auto overflow-auto fancyscroll">
                                {!this.state.loggedIn ? (
                                    <Nav.Item>
                                        <Nav.Link
                                            onClick={() => {
                                                this.props.history.push(
                                                    AppRoutes.home.link || AppRoutes.home.route,
                                                    { reload: true }
                                                );
                                            }}
                                            active={true}>
                                            <FormattedMessage id="routes.login" />
                                        </Nav.Link>
                                    </Nav.Item>
                                ) : (
                                    Object.values(this.state.categories).map(cat => {
                                        if (!cat.leader.cachedAuth) return;
                                        return (
                                            <div
                                                key={cat.name}
                                                className="d-flex"
                                                onMouseEnter={() => {
                                                    const timer = window.setTimeout(() => {
                                                        this.setState({
                                                            focusedCategory: cat.name,
                                                            focusTimer: undefined
                                                        });
                                                    }, 130);
                                                    this.setState({
                                                        focusTimer: timer
                                                    });
                                                }}
                                                onMouseLeave={() => {
                                                    window.clearTimeout(this.state.focusTimer);
                                                    this.setState({
                                                        focusTimer: undefined
                                                    });
                                                }}
                                                onClick={() => {
                                                    window.clearTimeout(this.state.focusTimer);
                                                    this.setState({
                                                        focusedCategory: cat.name,
                                                        focusTimer: undefined
                                                    });
                                                }}>
                                                <Nav.Item>
                                                    <Nav.Link
                                                        onClick={() => {
                                                            this.props.history.push(
                                                                cat.leader.link || cat.leader.route,
                                                                { reload: true }
                                                            );
                                                        }}
                                                        active={matchesPath(
                                                            this.props.location.pathname,
                                                            cat.leader.route,
                                                            !cat.leader.navbarLoose
                                                        )}>
                                                        <FormattedMessage id={cat.leader.name} />
                                                    </Nav.Link>
                                                </Nav.Item>
                                                {Object.values(cat.routes).filter(
                                                    value => value.cachedAuth
                                                ).length >= 2 ? (
                                                    <CSSTransition
                                                        in={this.state.focusedCategory === cat.name}
                                                        classNames="anim-collapse"
                                                        className="nowrap anim-collapse-all"
                                                        addEndListener={(node, done) => {
                                                            node.addEventListener(
                                                                "transitionend",
                                                                done,
                                                                false
                                                            );
                                                        }}
                                                        onMouseEnter={() => {
                                                            this.setState({
                                                                focusedCategory: cat.name
                                                            });
                                                        }}>
                                                        <div>
                                                            <Nav>
                                                                <div className="py-2 d-none d-lg-inline">
                                                                    <FontAwesomeIcon icon="angle-right" />
                                                                </div>
                                                                {cat.routes.map(val => {
                                                                    if (val.catleader) return; //we already display this but differently
                                                                    if (!val.cachedAuth) return;
                                                                    if (!val.visibleNavbar) return;

                                                                    return (
                                                                        <Nav.Item key={val.name}>
                                                                            <Nav.Link
                                                                                onClick={() => {
                                                                                    this.props.history.push(
                                                                                        val.link ||
                                                                                            val.route,
                                                                                        {
                                                                                            reload: true
                                                                                        }
                                                                                    );
                                                                                }}
                                                                                active={matchesPath(
                                                                                    this.props
                                                                                        .location
                                                                                        .pathname,
                                                                                    val.route,
                                                                                    !val.navbarLoose
                                                                                )}>
                                                                                <FormattedMessage
                                                                                    id={val.name}
                                                                                />
                                                                            </Nav.Link>
                                                                        </Nav.Item>
                                                                    );
                                                                })}
                                                                <div className="py-2 d-none d-lg-inline">
                                                                    <FontAwesomeIcon icon="grip-lines-vertical" />
                                                                </div>
                                                            </Nav>
                                                        </div>
                                                    </CSSTransition>
                                                ) : (
                                                    ""
                                                )}
                                            </div>
                                        );
                                    })
                                )}
                            </Nav>
                            {this.renderUser()}
                        </Navbar.Collapse>
                    </Navbar>
                    {this.state.serverInfoError ? (
                        <ErrorAlert error={this.state.serverInfoError} />
                    ) : (
                        ""
                    )}
                    {this.state.userNameError ? (
                        <ErrorAlert error={this.state.userNameError} />
                    ) : (
                        ""
                    )}
                </React.Fragment>
            );
        }

        private renderVersion(): React.ReactNode {
            if (!this.state.loggedIn) {
                return <FormattedMessage id="generic.appname" />;
            }
            if (this.state.serverInfoError)
                return (
                    <div>
                        <div className="align-top d-inline-block px-1">
                            <FontAwesomeIcon icon="exclamation-circle" />
                        </div>
                        <div className="d-inline-block">
                            <FormattedMessage id="navbar.server_error" />
                        </div>
                    </div>
                );
            if (this.state.serverInformation)
                return (
                    <React.Fragment>
                        <FormattedMessage id="generic.appname" />
                        {" v"}
                        {this.state.serverInformation.version!}
                    </React.Fragment>
                );

            return "loading"; //TODO: add a spinner;
        }

        private renderUser(): React.ReactNode {
            if (!this.state.loggedIn)
                return (
                    <Button
                        onClick={() => {
                            this.props.history.push(
                                AppRoutes.config.link || AppRoutes.config.route,
                                { reload: true }
                            );
                        }}
                        variant={
                            this.state.serverInfoError || this.state.userNameError
                                ? "danger"
                                : "primary"
                        }>
                        <FontAwesomeIcon icon="cogs" />
                    </Button>
                );

            return (
                <Nav.Item className="ml-auto">
                    <Dropdown>
                        <Dropdown.Toggle
                            id="user-dropdown"
                            type="button"
                            variant={
                                this.state.serverInfoError || this.state.userNameError
                                    ? "danger"
                                    : "primary"
                            }
                            data-toggle="dropdown"
                            aria-haspopup="true"
                            aria-expanded="false">
                            {this.state.userNameError ? (
                                <div>
                                    <div className="align-top d-inline-block px-1">
                                        <FontAwesomeIcon icon="exclamation-circle" />
                                    </div>
                                    <div className="d-inline-block">
                                        <FormattedMessage id="navbar.user_error" />
                                    </div>
                                </div>
                            ) : this.state.currentUser ? (
                                this.state.currentUser.name
                            ) : (
                                "loading" //TODO: add spinner
                            )}
                        </Dropdown.Toggle>
                        <Dropdown.Menu alignRight className="text-right">
                            <Dropdown.Item
                                onClick={() => {
                                    this.props.history.push(
                                        AppRoutes.config.link || AppRoutes.config.route,
                                        { reload: true }
                                    );
                                }}>
                                <FormattedMessage id="routes.config" />
                            </Dropdown.Item>
                            {AppRoutes.passwd.cachedAuth ? (
                                <Dropdown.Item
                                    onClick={() => {
                                        this.props.history.push(
                                            AppRoutes.passwd.link || AppRoutes.passwd.route,
                                            { reload: true }
                                        );
                                    }}>
                                    <FormattedMessage id="routes.passwd" />
                                </Dropdown.Item>
                            ) : (
                                ""
                            )}
                            <Dropdown.Item
                                onClick={() => {
                                    ServerClient.emit("purgeCache");
                                }}>
                                <FormattedMessage id="navbar.purgecache" />
                            </Dropdown.Item>
                            <Dropdown.Item
                                onClick={() => {
                                    this.props.history.replace(this.props.location.pathname, {
                                        reload: true
                                    });
                                }}>
                                <FormattedMessage id="navbar.refresh" />
                            </Dropdown.Item>
                            <Dropdown.Item onClick={this.logoutClick}>
                                <FormattedMessage id="navbar.logout" />
                            </Dropdown.Item>
                        </Dropdown.Menu>
                    </Dropdown>
                </Nav.Item>
            );
        }

        private logoutClick(): void {
            ServerClient.logout();
        }

        private async loadServerInformation(): Promise<void> {
            const info = await ServerClient.getServerInfo();
            this.setState({
                serverInformation: info.payload,
                serverInfoError: info.error
            });
        }

        private async loadUserInformation(): Promise<void> {
            const response = await UserClient.getCurrentUser();
            this.setState({
                currentUser: response.payload,
                userNameError: response.error
            });
        }
    }
);
