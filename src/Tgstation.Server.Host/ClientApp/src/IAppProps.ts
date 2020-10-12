import ITranslationFactory from "./translations/ITranslationFactory";

interface IAppProps {
    readonly locale: string;
    readonly translationFactory?: ITranslationFactory;
}

export default IAppProps;
