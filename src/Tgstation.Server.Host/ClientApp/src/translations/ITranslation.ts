import ILocalization from "./ILocalization";

interface ITranslation {
    readonly locale: string;
    readonly messages: ILocalization;
}

export default ITranslation;
