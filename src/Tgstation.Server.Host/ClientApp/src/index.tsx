import * as React from "react";
import * as ReactDOM from "react-dom";

import ControlPanel from "tgstation-server-control-panel";

import registerServiceWorker from "./registerServiceWorker";

import "./index.css";

const contentElement = document.getElementById("root");

if (!contentElement)
    throw new Error("Missing root div, cannot render app!");

const legacyUserLanguageKey = "userLanguage";
const userLang = navigator.language || navigator[legacyUserLanguageKey] || "en-CA";
const serverAddress = window.location.href;
const controlPanel = <ControlPanel serverAddress={serverAddress} locale={userLang} />;

registerServiceWorker();

ReactDOM.render(controlPanel, contentElement);
