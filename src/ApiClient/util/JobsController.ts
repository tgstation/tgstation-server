import { TypedEmitter } from "tiny-typed-emitter";

import {
    AdministrationRights,
    ByondRights,
    ChatBotRights,
    ConfigurationRights,
    DreamDaemonRights,
    DreamMakerRights,
    InstanceManagerRights,
    InstanceUserRights,
    RepositoryRights,
    RightsType
} from "../generatedcode/_enums";
import { Components } from "../generatedcode/_generated";
import InstanceUserClient from "../InstanceUserClient";
import JobsClient, { getJobErrors, listJobsErrors } from "../JobsClient";
import InternalError, { ErrorCode } from "../models/InternalComms/InternalError";
import { StatusCode } from "../models/InternalComms/InternalStatus";
import ServerClient from "../ServerClient";
import UserClient from "../UserClient";
import configOptions from "./config";

interface IEvents {
    jobsLoaded: () => unknown;
}

export type CanCancelJob = Components.Schemas.Job & {
    canCancel?: boolean;
};

export default new (class JobsController extends TypedEmitter<IEvents> {
    private _instance: number | undefined;
    public set instance(id: number | undefined) {
        this._instance = id;
        this.reset();
    }

    //This is a special property that gets set by approutes to add a handler for when this needs to change the instance
    // This is here as a hack so that JobsController doesnt have to reference files outside the api client
    // eslint-disable-next-line @typescript-eslint/no-empty-function
    public setInstance: (instance: number | undefined) => void = () => {};

    private fastmodecount = 0;
    public set fastmode(cycles: number) {
        console.log(`JobsController going in fastmode for ${cycles} cycles`);
        this.fastmodecount = cycles;
        this.restartLoop();
    }

    private currentLoop: Date = new Date(0);

    public errors: InternalError<getJobErrors | listJobsErrors>[] = [];
    public jobs: Map<number, Components.Schemas.Job> = new Map<number, Components.Schemas.Job>();

    private reset() {
        this.jobs = new Map<number, Components.Schemas.Job>();
        this.restartLoop();
    }

    public constructor() {
        super();

        this.loop = this.loop.bind(this);
        this.reset = this.reset.bind(this);

        //technically not a "cache" but we might as well reload it
        ServerClient.on("purgeCache", this.reset);
    }

    public restartLoop() {
        //we use an actual date object here because it could help prevent really weird timing
        // issues as two different date objects cannot be equal
        // despite the date being
        const initDate = new Date(Date.now());
        this.currentLoop = initDate;
        this.loop(initDate);
    }

    private loop(loopid: Date) {
        //if we dont got an instance to check, dont check
        // normally we should have an instance id, but this is in case we dont
        if (this._instance === undefined) {
            return;
        }

        //so loops get initialiazed with the current time, it keeps track of which loop to run with
        // that initialization date in currentLoop if the currentLoop isnt equal to the one provided
        // to the loop, it means that the loop was
        // replaced so we dont try to call for another one
        if (loopid !== this.currentLoop) {
            return;
        }

        //time to clear out errors
        this.errors = [];

        //now since this is async, it still possible that a single fire gets done after the new loop started, theres no really much that can be done about it
        JobsClient.listJobs(this._instance)
            .then(async value => {
                //this check is here because the request itself is async and could return after
                // the loop is terminated, we dont want to contaminate the jobs of an instance
                // with the jobs of another even if it is for a single fire and would eventually
                // get fixed on its own after a few seconds
                if (loopid !== this.currentLoop) return;

                if (value.code === StatusCode.OK) {
                    for (const job of value.payload!) {
                        this.jobs.set(job.id, job);
                    }

                    //we check all jobs we have locally against the active jobs we got in the reply so
                    // we can query jobs which we didnt get informed about manually
                    const localids = Array.from(this.jobs, ([, job]) => job.id);
                    const remoteids = value.payload!.map(job => job.id);

                    const manualids = localids.filter(x => !remoteids.includes(x));

                    const work: Promise<void>[] = [];
                    for (const id of manualids) {
                        work.push(
                            JobsClient.getJob(this._instance!, id).then(status => {
                                if (loopid !== this.currentLoop) return;

                                if (status.code === StatusCode.OK) {
                                    this.jobs.set(id, status.payload!);
                                } else {
                                    this.errors.push(status.error!);
                                }
                            })
                        );
                    }
                    //await all jobs to exist
                    await Promise.all(work);

                    if (loopid !== this.currentLoop) return;

                    work.length = 0;
                    for (const _job of this.jobs.values()) {
                        const job = _job as CanCancelJob;
                        if (job.progress === undefined) {
                            work.push(
                                JobsClient.getJob(this._instance!, job.id).then(progressedjob => {
                                    if (loopid !== this.currentLoop) return;
                                    if (progressedjob.code === StatusCode.OK) {
                                        job.progress = progressedjob.payload!.progress;
                                    } else {
                                        this.errors.push(progressedjob.error!);
                                    }
                                })
                            );
                        }

                        work.push(
                            this.canCancel(job, this.errors).then(canCancel => {
                                if (loopid !== this.currentLoop) return;
                                job.canCancel = canCancel;
                            })
                        );
                    }

                    //populate fields on jobs
                    await Promise.all(work);

                    if (loopid !== this.currentLoop) return;

                    if (this.fastmodecount && loopid === this.currentLoop) {
                        window.setTimeout(() => this.loop(loopid), 800);
                        this.fastmodecount--;
                        console.log(
                            `JobsController will remain in fastmode for ${this.fastmodecount} cycles`
                        );
                    } else {
                        window.setTimeout(
                            () => this.loop(loopid),
                            (value.payload!.length
                                ? (configOptions.jobpollactive.value as number)
                                : (configOptions.jobpollinactive.value as number)) * 1000
                        );
                    }
                } else {
                    if (value.error!.code === ErrorCode.JOB_INSTANCE_OFFLINE) {
                        this.setInstance(undefined);
                    }
                    this.errors.push(value.error!);
                    window.setTimeout(() => this.loop(loopid), 10000);
                }

                this.emit("jobsLoaded");
            })
            .catch(reason => {
                console.error(reason);
            });
    }

    private async canCancel(
        job: Readonly<CanCancelJob>,
        errors: InternalError<ErrorCode>[]
    ): Promise<boolean> {
        //shouldnt really occur in normal conditions but this is a safety anyways
        if (this._instance === undefined) return false;

        //we dont need to reevalutate stuff that we already know
        if (job.canCancel !== undefined) return job.canCancel;

        if (job.cancelRightsType === undefined) {
            return true;
        }

        switch (job.cancelRightsType as RightsType) {
            case RightsType.Administration: {
                const userInfo = await UserClient.getCurrentUser();
                if (userInfo.code === StatusCode.OK) {
                    const required = job.cancelRight as AdministrationRights;
                    return !!(userInfo.payload!.administrationRights! & required);
                } else {
                    errors.push(userInfo.error!);
                    return false;
                }
            }
            case RightsType.InstanceManager: {
                const userInfo = await UserClient.getCurrentUser();
                if (userInfo.code === StatusCode.OK) {
                    const required = job.cancelRight as InstanceManagerRights;
                    return !!(userInfo.payload!.instanceManagerRights! & required);
                } else {
                    errors.push(userInfo.error!);
                    return false;
                }
            }
            case RightsType.Byond: {
                const instanceUser = await InstanceUserClient.getCurrentInstanceUser(
                    this._instance
                );
                if (instanceUser.code === StatusCode.OK) {
                    const required = job.cancelRight as ByondRights;
                    return !!(instanceUser.payload!.byondRights! & required);
                } else {
                    errors.push(instanceUser.error!);
                    return false;
                }
            }
            case RightsType.ChatBots: {
                const instanceUser = await InstanceUserClient.getCurrentInstanceUser(
                    this._instance
                );
                if (instanceUser.code === StatusCode.OK) {
                    const required = job.cancelRight as ChatBotRights;
                    return !!(instanceUser.payload!.chatBotRights! & required);
                } else {
                    errors.push(instanceUser.error!);
                    return false;
                }
            }
            case RightsType.Configuration: {
                const instanceUser = await InstanceUserClient.getCurrentInstanceUser(
                    this._instance
                );
                if (instanceUser.code === StatusCode.OK) {
                    const required = job.cancelRight as ConfigurationRights;
                    return !!(instanceUser.payload!.configurationRights! & required);
                } else {
                    errors.push(instanceUser.error!);
                    return false;
                }
            }
            case RightsType.DreamDaemon: {
                const instanceUser = await InstanceUserClient.getCurrentInstanceUser(
                    this._instance
                );
                if (instanceUser.code === StatusCode.OK) {
                    const required = job.cancelRight as DreamDaemonRights;
                    return !!(instanceUser.payload!.dreamDaemonRights! & required);
                } else {
                    errors.push(instanceUser.error!);
                    return false;
                }
            }
            case RightsType.DreamMaker: {
                const instanceUser = await InstanceUserClient.getCurrentInstanceUser(
                    this._instance
                );
                if (instanceUser.code === StatusCode.OK) {
                    const required = job.cancelRight as DreamMakerRights;
                    return !!(instanceUser.payload!.dreamMakerRights! & required);
                } else {
                    errors.push(instanceUser.error!);
                    return false;
                }
            }
            case RightsType.InstanceUser: {
                const instanceUser = await InstanceUserClient.getCurrentInstanceUser(
                    this._instance
                );
                if (instanceUser.code === StatusCode.OK) {
                    const required = job.cancelRight as InstanceUserRights;
                    return !!(instanceUser.payload!.instanceUserRights! & required);
                } else {
                    errors.push(instanceUser.error!);
                    return false;
                }
            }
            case RightsType.Repository: {
                const instanceUser = await InstanceUserClient.getCurrentInstanceUser(
                    this._instance
                );
                if (instanceUser.code === StatusCode.OK) {
                    const required = job.cancelRight as RepositoryRights;
                    return !!(instanceUser.payload!.repositoryRights! & required);
                } else {
                    errors.push(instanceUser.error!);
                    return false;
                }
            }
        }
    }

    public async cancelOrClear(
        jobid: number,
        onError: (error: InternalError<ErrorCode>) => void
    ): Promise<boolean> {
        const job = this.jobs.get(jobid);

        //no we cant cancel jobs we arent aware of yet
        if (!job) return false;

        //just clear out the job
        if (job.stoppedAt) {
            this.jobs.delete(jobid);
            return true;
        } else {
            if (this._instance === undefined) {
                onError(
                    new InternalError(ErrorCode.APP_FAIL, {
                        jsError: new Error("No instance selected during deletion of a job")
                    })
                );
                return false;
            } else {
                const deleteInfo = await JobsClient.deleteJob(this._instance, jobid);
                if (deleteInfo.code === StatusCode.OK) {
                    return true;
                } else {
                    onError(deleteInfo.error!);
                    return false;
                }
            }
        }
    }
})();
