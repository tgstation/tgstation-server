import { retry } from "@octokit/plugin-retry";
import { throttling } from "@octokit/plugin-throttling";
import { RequestError } from "@octokit/request-error";
import { Octokit } from "@octokit/rest";
import { TypedEmitter } from "tiny-typed-emitter/lib";

import InternalError, { ErrorCode } from "../ApiClient/models/InternalComms/InternalError";
import InternalStatus, { StatusCode } from "../ApiClient/models/InternalComms/InternalStatus";
import configOptions from "../ApiClient/util/config";

export interface TGSVersion {
    version: string;
    body: string;
    current: boolean;
    old: boolean;
}

interface IEvents {}

/* eslint-disable */

async function hook(request: any, route: any, parameters?: any): Promise<any> {
    const endpoint = request.endpoint.merge(route as string, parameters);

    if (configOptions.githubtoken.value) {
        endpoint.headers.authorization = `token ${configOptions.githubtoken.value}`;
    }

    return request(endpoint);
}

async function auth(): Promise<any> {
    if (configOptions.githubtoken.value) {
        return {
            type: "token",
            tokenType: "pat",
            token: configOptions.githubtoken.value
        };
    } else {
        return {
            type: "unauthenticated"
        };
    }
}

const authStrategy = () => {
    return Object.assign(auth.bind(null), {
        hook: hook.bind(null)
    });
};

/* eslint-enable */

const e = new (class GithubClient extends TypedEmitter<IEvents> {
    private readonly apiClient: Octokit;

    public constructor() {
        super();

        const octo = Octokit.plugin(retry, throttling);

        this.apiClient = new octo({
            authStrategy,
            userAgent: "tgstation-server-control-panel/" + VERSION,
            baseUrl: "https://api.github.com",
            throttle: {
                onRateLimit: (
                    retryAfter: number,
                    options: { method: string; url: string; request: { retryCount: number } }
                ) => {
                    console.warn(
                        `Request quota exhausted for request ${options.method} ${options.url}`
                    );

                    if (options.request.retryCount === 0) {
                        // only retries once
                        console.log(`Retrying after ${retryAfter} seconds!`);
                        return true;
                    }
                    return false;
                },
                onAbuseLimit: (retryAfter: number, options: { method: string; url: string }) => {
                    // does not retry, only logs a warning
                    console.warn(`Abuse detected for request ${options.method} ${options.url}`);
                }
            }
        });
    }

    public async getVersions({
        owner,
        repo,
        current,
        all
    }: {
        owner: string;
        repo: string;
        current: string;
        all?: boolean;
    }): Promise<InternalStatus<TGSVersion[], ErrorCode.GITHUB_FAIL>> {
        let payload: TGSVersion[];
        let oldversions = 0;
        try {
            payload = await this.apiClient.paginate(
                this.apiClient.repos.listReleases,
                { owner, repo },
                (response, done) => {
                    return response.data.reduce((result, release) => {
                        const match = /tgstation-server-v([\d.]+)/.exec(release.name);
                        if (!match) return result;
                        if (match[1][0] !== "4") return result;

                        const version = match[1];
                        let old = false;

                        //show 3 outdated versions(2 if you count the current one)
                        if (version <= current) {
                            if (oldversions >= 3 && !all) {
                                (done as () => void)();
                                return result;
                            }
                            oldversions++;
                            old = true;
                        }

                        result.push({
                            version,
                            body: release.body,
                            current: version === current,
                            old
                        });
                        return result;
                    }, [] as TGSVersion[]);
                }
            );
        } catch (e) {
            return new InternalStatus<TGSVersion[], ErrorCode.GITHUB_FAIL>({
                code: StatusCode.ERROR,
                error: new InternalError(ErrorCode.GITHUB_FAIL, {
                    jsError: e as RequestError
                })
            });
        }
        return new InternalStatus({
            code: StatusCode.OK,
            payload
        });
    }
})();
export default e;
