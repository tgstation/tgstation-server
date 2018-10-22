import * as React from 'react';
import * as ReactDOM from 'react-dom';
import ControlPanel from 'tgstation-server-control-panel';
import registerServiceWorker from './registerServiceWorker';

import './index.css';

const contentElement = document.getElementById('root') as HTMLElement;
const legacyUserLanguageKey = 'userLanguage';
const userLang = navigator.language || navigator[legacyUserLanguageKey];
const serverAddress = window.location.href;

ReactDOM.render(
<ControlPanel serverAddress={serverAddress} locale={userLang} />
, contentElement);
registerServiceWorker();
