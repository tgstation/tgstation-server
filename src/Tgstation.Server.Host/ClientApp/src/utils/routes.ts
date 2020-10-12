import { IconProp } from "@fortawesome/fontawesome-svg-core";

import { AdministrationRights, InstanceManagerRights } from "../ApiClient/generatedcode/_enums";
import { StatusCode } from "../ApiClient/models/InternalComms/InternalStatus";
import UserClient from "../ApiClient/UserClient";
import CredentialsProvider from "../ApiClient/util/CredentialsProvider";
import JobsController from "../ApiClient/util/JobsController";

export interface AppRoute {
    ///Base parameters
    //must be unique, also is the id of the route name message
    name: string;
    //must be unique, url to access
    route: string;
    //link to link to when linking to the route, defaults to the "route"
    link?: string;
    //filename in components/view that the route should display
    file: string;

    ///Path parameters
    //If subpaths should route here
    loose: boolean;
    //If subpaths should light up the navbar button
    navbarLoose: boolean;

    ///Authentication
    //if we can route to it even on the login page
    loginless?: boolean;
    //function to tell if we are authorized
    isAuthorized: () => Promise<boolean>;
    //result of isAuthorized() after RouteController runs it, can be used by components but only set by RouteController
    cachedAuth?: boolean;

    ///Visibility
    //if this shows up on the navbar
    visibleNavbar: boolean;
    //serves two purposes, first one is to give it an icon, the second one is to not display it if the icon is undefined
    homeIcon?: IconProp;

    ///Categories
    //name of the category it belongs to
    category?: string;
    //if this is the main button in the category
    catleader?: boolean;
}

function adminRight(right: AdministrationRights) {
    return async (): Promise<boolean> => {
        if (!CredentialsProvider.isTokenValid()) return false;
        const response = await UserClient.getCurrentUser();

        if (response.code == StatusCode.OK) {
            return !!(response.payload!.administrationRights! & right);
        }
        return false;
    };
}

function instanceManagerRight(right: InstanceManagerRights) {
    return async (): Promise<boolean> => {
        if (!CredentialsProvider.isTokenValid()) return false;
        const response = await UserClient.getCurrentUser();

        if (response.code == StatusCode.OK) {
            return !!(response.payload!.instanceManagerRights! & right);
        }
        return false;
    };
}

const AppRoutes: {
    [id: string]: AppRoute;
} = {
    home: {
        name: "routes.home",
        route: "/",
        file: "Home",

        loose: false,
        navbarLoose: false,

        isAuthorized: (): Promise<boolean> => Promise.resolve(true),

        visibleNavbar: true,
        homeIcon: undefined,

        category: "home",
        catleader: true
    },
    instancelist: {
        name: "routes.instancelist",
        route: "/instances/",
        file: "Instance/List",

        loose: false,
        navbarLoose: true,

        isAuthorized: instanceManagerRight(InstanceManagerRights.List | InstanceManagerRights.Read),

        visibleNavbar: true,
        homeIcon: "hdd",

        category: "instance",
        catleader: true
    },
    instancecode: {
        name: "routes.instancecode",
        route: "/instances/code/:id(\\d+)/",
        file: "Instance/CodeDeployment",

        get link(): string {
            return AppCategories.instance.data?.instanceid !== undefined
                ? `/instances/code/${AppCategories.instance.data.instanceid}/`
                : AppRoutes.instancelist.link || AppRoutes.instancelist.route;
        },

        loose: false,
        navbarLoose: true,

        isAuthorized: (): Promise<boolean> => Promise.resolve(true),

        visibleNavbar: true,
        homeIcon: undefined,

        category: "instance"
    },
    instancehosting: {
        name: "routes.instancehosting",
        route: "/instances/hosting/:id(\\d+)/",
        file: "Instance/Hosting",

        get link(): string {
            return AppCategories.instance.data?.instanceid !== undefined
                ? `/instances/hosting/${AppCategories.instance.data.instanceid}/`
                : AppRoutes.instancelist.link || AppRoutes.instancelist.route;
        },

        loose: false,
        navbarLoose: true,

        isAuthorized: (): Promise<boolean> => Promise.resolve(true),

        visibleNavbar: true,
        homeIcon: undefined,

        category: "instance"
    },
    instancejobs: {
        name: "routes.instancejobs",
        route: "/instances/jobs/:id(\\d+)/:jobid(\\d+)?/",
        file: "Instance/Jobs",

        get link(): string {
            return AppCategories.instance.data?.instanceid !== undefined
                ? `/instances/jobs/${AppCategories.instance.data.instanceid}/${
                      AppCategories.instance.data.lastjob !== undefined
                          ? `${AppCategories.instance.data.lastjob}/`
                          : ""
                  }`
                : AppRoutes.instancelist.link || AppRoutes.instancelist.route;
        },

        loose: false,
        navbarLoose: true,

        isAuthorized: (): Promise<boolean> => Promise.resolve(true),

        visibleNavbar: true,
        homeIcon: undefined,

        category: "instance"
    },
    instanceconfig: {
        name: "routes.instanceconfig",
        route: "/instances/config/:id(\\d+)/",
        file: "Instance/Config",

        get link(): string {
            return AppCategories.instance.data?.instanceid !== undefined
                ? `/instances/config/${AppCategories.instance.data.instanceid}/`
                : AppRoutes.instancelist.link || AppRoutes.instancelist.route;
        },

        loose: false,
        navbarLoose: true,

        isAuthorized: (): Promise<boolean> => Promise.resolve(true),

        visibleNavbar: true,
        homeIcon: undefined,

        category: "instance"
    },
    userlist: {
        name: "routes.usermanager",
        route: "/users/",
        file: "User/List",

        loose: false,
        navbarLoose: true,

        //you can always read your own user
        isAuthorized: (): Promise<boolean> => Promise.resolve(true),

        visibleNavbar: true,
        homeIcon: "user",

        category: "user",
        catleader: true
    },
    useredit: {
        name: "routes.useredit",
        route: "/users/edit/:id(\\d+)/:tab?/",

        //whole lot of bullshit just to make it that if you have an id, link to the edit page, otherwise link to the list page, and if you link to the user page, put the tab in
        get link(): string {
            return AppCategories.user.data?.selectedid !== undefined
                ? `/users/edit/${AppCategories.user.data?.selectedid}/${
                      AppCategories.user.data?.tab !== undefined
                          ? `${AppCategories.user.data?.tab}/`
                          : ""
                  }`
                : AppRoutes.userlist.link || AppRoutes.userlist.route;
        },
        file: "User/Edit",

        loose: true,
        navbarLoose: true,

        //you can always read your own user
        isAuthorized: (): Promise<boolean> => Promise.resolve(true),

        visibleNavbar: true,
        homeIcon: undefined,

        category: "user"
    },
    usercreate: {
        name: "routes.usercreate",
        route: "/users/create/",

        link: "/users/create/",
        file: "User/Create",

        loose: true,
        navbarLoose: true,

        isAuthorized: adminRight(AdministrationRights.WriteUsers),

        visibleNavbar: true,
        homeIcon: undefined,

        category: "user"
    },
    admin: {
        name: "routes.admin",
        route: "/admin/",
        file: "Administration",

        loose: false,
        navbarLoose: true,

        isAuthorized: (): Promise<boolean> => Promise.resolve(true),

        visibleNavbar: true,
        homeIcon: "tools",

        category: "admin",
        catleader: true
    },
    admin_update: {
        name: "routes.admin.update",
        route: "/admin/update/:all?/",
        file: "Admin/Update",

        link: "/admin/update/",

        loose: true,
        navbarLoose: true,

        isAuthorized: adminRight(AdministrationRights.ChangeVersion),
        visibleNavbar: true,
        homeIcon: undefined,

        category: "admin"
    },
    admin_logs: {
        name: "routes.admin.logs",
        route: "/admin/logs/:name?/",
        link: "/admin/logs/",
        file: "Admin/Logs",

        loose: false,
        navbarLoose: true,

        isAuthorized: adminRight(AdministrationRights.DownloadLogs),
        visibleNavbar: true,
        homeIcon: undefined,

        category: "admin"
    },
    passwd: {
        name: "routes.passwd",
        route: "/users/passwd/:id(\\d+)?/",
        link: "/users/passwd/",
        file: "ChangePassword",

        loose: true,
        navbarLoose: true,

        isAuthorized: adminRight(AdministrationRights.EditOwnPassword),

        visibleNavbar: false,
        homeIcon: "key"
    },
    config: {
        name: "routes.config",
        route: "/config/",
        file: "Configuration",

        loose: true,
        navbarLoose: true,

        loginless: true,
        isAuthorized: (): Promise<boolean> => Promise.resolve(true),

        visibleNavbar: false,
        homeIcon: "cogs"
    }
};

export { AppRoutes };

export type UnpopulatedAppCategory = Partial<AppCategory>;

export interface AppCategory {
    name: string; //doesnt really matter, kinda bullshit
    routes: AppRoute[];
    leader: AppRoute;
    data: Record<string, string | number | undefined>;
}

export type UnpopulatedAppCategories = {
    [key: string]: UnpopulatedAppCategory;
};

export type AppCategories = {
    [key: string]: AppCategory;
};

export const AppCategories: UnpopulatedAppCategories = {
    home: {
        name: "home"
    },
    instance: {
        name: "instance",

        data: {
            _instanceid: undefined as number | undefined,
            set instanceid(newval: string | undefined) {
                let id: number | undefined;
                //Undefined as a value is ok
                if (newval === undefined) {
                    id = undefined;
                } else {
                    //check if its a number
                    id = parseInt(newval);
                    if (Number.isNaN(id)) {
                        return;
                    }
                }

                //setting the instance id causes the thing to drop all jobs its aware of, so avoid when possible
                if (this._instanceid == id) {
                    return;
                }

                this._instanceid = id;
                JobsController.instance = id;
            },
            get instanceid(): string | undefined {
                return this._instanceid?.toString();
            }
        }
    },
    user: {
        name: "user"
    },
    admin: {
        name: "admin"
    }
};

//Either pass the instance id or pass an empty string
JobsController.setInstance = instance => {
    AppCategories.instance.data!.instanceid = instance?.toString();
};
