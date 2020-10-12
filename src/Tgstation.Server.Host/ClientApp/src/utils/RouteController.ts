import { TypedEmitter } from "tiny-typed-emitter/lib";

import LoginHooks from "../ApiClient/util/LoginHooks";
import { AppCategories, AppRoute, AppRoutes, UnpopulatedAppCategory } from "./routes";

interface IEvents {
    refresh: (routes: Array<AppRoute>) => void; //auth
    refreshAll: (routes: Array<AppRoute>) => void; //noauth+auth
}

//helper class to process AppRoutes
class RouteController extends TypedEmitter<IEvents> {
    private refreshing = false;

    public constructor() {
        super();
        this.refreshRoutes = this.refreshRoutes.bind(this);

        LoginHooks.addHook(this.refreshRoutes);
        this.refreshRoutes().catch(console.error);

        //process categories
        console.time("Category mapping");
        const catmap = new Map<string, UnpopulatedAppCategory>();

        for (const val of Object.values(AppCategories)) {
            val.routes = [];
            //null asserted the name because that one is everywhere, even if the rest is partial
            catmap.set(val.name!, val);
        }

        for (const route of Object.values(AppRoutes)) {
            if (!route.category) continue;

            const cat = catmap.get(route.category);
            if (!cat) {
                console.error("Route has invalid category", route);
                continue;
            }

            //this is guaranteed to be an array as its set in the loop above
            cat.routes!.push(route);

            if (route.catleader) {
                if (cat.leader) {
                    console.error("Category has two leaders", cat.leader, route);
                    continue;
                }
                cat.leader = route;
            }
        }
        console.log("Categories mapped", catmap);
        console.timeEnd("Category mapping");
    }

    public async refreshRoutes() {
        if (this.refreshing) {
            console.log("Already refreshing");
            return;
        } //no need to refresh twice

        this.refreshing = true;

        const work = []; //    we get all hidden routes no matter the authentification without waiting for the refresh
        const routes = this.getImmediateRoutes(false);

        for (const route of routes) {
            route.cachedAuth = undefined;
            if (route.isAuthorized) {
                work.push(
                    route.isAuthorized().then(auth => {
                        route.cachedAuth = auth;
                    })
                );
            }
        }

        await Promise.all(work); //wait for all the authorized calls to complete

        this.emit("refresh", this.getImmediateRoutes(true));
        const routesNoAuth = this.getImmediateRoutes(false);
        this.emit("refreshAll", routesNoAuth);
        this.refreshing = false;

        console.log("Routes refreshed", routesNoAuth);
        return await this.getRoutes();
    }

    private wait4refresh() {
        return new Promise<void>(resolve => {
            if (!this.refreshing) {
                resolve();
                return;
            }
            this.on("refresh", () => {
                resolve();
            });
        });
    }

    public async getRoutes(auth = true): Promise<AppRoute[]> {
        await this.wait4refresh();

        return this.getImmediateRoutes(auth);
    }

    public getImmediateRoutes(auth = true) {
        const results: Array<AppRoute> = [];

        const propNames = Object.getOwnPropertyNames(AppRoutes);
        for (let i = 0; i < propNames.length; i++) {
            const name = propNames[i];
            const val = AppRoutes[name];

            //we check for isauthorized here without calling because routes that lack the function are public
            if (val.isAuthorized && !val.cachedAuth && auth) continue; //if not authorized and we only show authorized routes

            results.push(val);
        }

        return results;
    }

    //this is to ensure that the constructor(and that the categories are populated) is finished running before we try to access categories
    public getCategories() {
        //the second AppCategories here is a type that refers to { [key: string]: AppCategory }
        return AppCategories as AppCategories;
    }
}

export default new RouteController();
