import { pathToRegexp } from "path-to-regexp";

function getSavedCreds(): string[] | null {
    let usr: string | null = null;
    let pwd: string | null = null;
    try {
        //private browsing on safari can throw when using storage
        usr = window.localStorage.getItem("username");
        pwd = window.localStorage.getItem("password");
    } catch (e) {
        // eslint-disable-next-line @typescript-eslint/no-empty-function
        (() => {})(); //noop
    }

    if (usr && pwd) {
        return [usr, pwd];
    } else {
        return null;
    }
}

function download(filename: string, text: string): void {
    const element = document.createElement("a");
    element.setAttribute("href", "data:text/plain;charset=utf-8," + encodeURIComponent(text));
    element.setAttribute("download", filename);

    element.style.display = "none";
    document.body.appendChild(element);

    element.click();

    document.body.removeChild(element);
}

function replaceAll(str: string, find: string, replace: string, ignore?: boolean): string {
    return str.replace(
        new RegExp(find.replace(/([/,!\\^${}[\]().*+?|<>\-&])/g, "\\$&"), ignore ? "gi" : "g"),
        replace.replace(/\$/g, "$$$$")
    );
}

function matchesPath(path: string, target: string, exact = false): boolean {
    //remove trailing slashes
    if (path.slice(-1) === "/") path = path.slice(0, -1);
    if (target.slice(-1) === "/") target = target.slice(0, -1);

    return pathToRegexp(target, undefined, { end: exact }).test(path);
}

export { getSavedCreds, download, replaceAll, matchesPath };
