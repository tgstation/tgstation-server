import ILocalization from "./ILocalization";
import ITranslation from "./ITranslation";

export default class Translation implements ITranslation {
    public constructor(public readonly locale: string, public readonly messages: ILocalization) {}
}
