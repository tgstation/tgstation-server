export type ConfigValue = number | string | boolean;

export type ConfigOption = BaseConfigOption &
    (NumConfigOption | StrConfigOption | PwdConfigOption | BoolConfigOption | EnumConfigOption);

export interface BaseConfigOption {
    id: string;
}

export interface NumConfigOption extends BaseConfigOption {
    type: "num";
    value: number;
}
export interface StrConfigOption extends BaseConfigOption {
    type: "str";
    value: string;
}
export interface PwdConfigOption extends BaseConfigOption {
    type: "pwd";
    value: string;
}
export interface BoolConfigOption extends BaseConfigOption {
    type: "bool";
    value: boolean;
}
export interface EnumConfigOption extends BaseConfigOption {
    type: "enum";
    possibleValues: Record<string, string>;
    value: string;
}

export type ConfigMap = {
    [key: string]: ConfigOption;
};

export enum jobsWidgetOptions {
    ALWAYS = "always",
    AUTO = "auto",
    NEVER = "never"
}

const configOptions: ConfigMap = {
    githubtoken: {
        id: "config.githubtoken",
        type: "pwd",
        value: ""
    },
    apipath: {
        id: "config.apipath",
        type: "str",
        value: "/"
    },
    jobpollinactive: {
        id: "config.jobpollinactive",
        type: "num",
        value: 15
    },
    jobpollactive: {
        id: "config.jobpollactive",
        type: "num",
        value: 5
    },
    jobswidgetdisplay: {
        id: "config.jobswidgetdisplay",
        type: "enum",
        possibleValues: jobsWidgetOptions,
        value: jobsWidgetOptions.AUTO
    }
};

export default configOptions;
