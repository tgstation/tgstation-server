import "./App.css";

import * as React from "react";
import Container from "react-bootstrap/Container";
import { hot } from "react-hot-loader/root";
import { IntlProvider } from "react-intl";
import { BrowserRouter } from "react-router-dom";

import { ErrorCode } from "./ApiClient/models/InternalComms/InternalError";
import { StatusCode } from "./ApiClient/models/InternalComms/InternalStatus";
import ServerClient from "./ApiClient/ServerClient";
import UserClient from "./ApiClient/UserClient";
import configOptions from "./ApiClient/util/config";
import LoginHooks from "./ApiClient/util/LoginHooks";
import AppNavbar from "./components/AppNavbar";
import ErrorBoundary from "./components/utils/ErrorBoundary";
import JobsList from "./components/utils/JobsList";
import Loading from "./components/utils/Loading";
import IAppProps from "./IAppProps";
import Router from "./Router";
import ITranslation from "./translations/ITranslation";
import ITranslationFactory from "./translations/ITranslationFactory";
import TranslationFactory from "./translations/TranslationFactory";
import { getSavedCreds } from "./utils/misc";

interface IState {
    translation?: ITranslation;
    translationError?: string;
    loggedIn: boolean;
    loading: boolean;
    autoLogin: boolean;
    passdownCat?: { name: string; key: string };
}

class App extends React.Component<IAppProps, IState> {
    private readonly translationFactory: ITranslationFactory;

    public constructor(props: IAppProps) {
        super(props);

        this.translationFactory = this.props.translationFactory || new TranslationFactory();

        this.state = {
            loggedIn: false,
            loading: true,
            autoLogin: false
        };
    }
    public async componentDidMount(): Promise<void> {
        LoginHooks.on("loginSuccess", () => {
            console.log("Logging in");

            void UserClient.getCurrentUser(); //preload the user, we dont particularly care about the content, just that its preloaded
            this.setState({
                loggedIn: true,
                loading: false,
                autoLogin: false
            });
        });
        ServerClient.on("logout", () => {
            this.setState({
                loggedIn: false
            });
        });

        await this.loadTranslation();
        await ServerClient.initApi();

        const [usr, pwd] = getSavedCreds() || [undefined, undefined];

        const autoLogin = !!(usr && pwd);
        if (autoLogin) console.log("Logging in with saved credentials");

        this.setState({
            loading: false,
            autoLogin: autoLogin
        });
        if (autoLogin) {
            const res = await ServerClient.login({ userName: usr!, password: pwd! });
            if (res.code == StatusCode.ERROR) {
                this.setState({
                    autoLogin: false
                });
                if (
                    res.error?.code == ErrorCode.LOGIN_DISABLED ||
                    res.error?.code == ErrorCode.LOGIN_FAIL
                ) {
                    try {
                        window.localStorage.removeItem("username");
                        window.localStorage.removeItem("password");
                    } catch (e) {
                        // eslint-disable-next-line @typescript-eslint/no-empty-function
                        (() => {})(); //noop
                    }
                }
            }
        }
    }

    public render(): React.ReactNode {
        if (this.state.translationError != null)
            return <p className="App-error">{this.state.translationError}</p>;

        if (this.state.translation == null) return <Loading>Loading translations...</Loading>;
        return (
            <IntlProvider
                locale={this.state.translation.locale}
                messages={this.state.translation.messages}>
                <BrowserRouter basename={DEFAULT_BASEPATH}>
                    <ErrorBoundary>
                        <AppNavbar category={this.state.passdownCat} />
                        <Container className="mt-5 mb-5">
                            {this.state.loading ? (
                                <Loading text="loading.app" />
                            ) : this.state.autoLogin && !this.state.loggedIn ? (
                                <Loading text="loading.app" />
                            ) : (
                                <Router
                                    loggedIn={this.state.loggedIn}
                                    selectCategory={cat => {
                                        this.setState({
                                            passdownCat: {
                                                name: cat,
                                                key: Math.random().toString()
                                            }
                                        });
                                    }}
                                />
                            )}
                        </Container>
                        <JobsList />
                    </ErrorBoundary>
                </BrowserRouter>
            </IntlProvider>
        );
    }

    private async loadTranslation(): Promise<void> {
        console.time("LoadTranslations");
        try {
            const translation = await this.translationFactory.loadTranslation(this.props.locale);
            this.setState({
                translation
            });
        } catch (error) {
            this.setState({
                translationError: JSON.stringify(error) || "An unknown error occurred"
            });

            return;
        }
        console.timeEnd("LoadTranslations");
    }
}

export default hot(App);
