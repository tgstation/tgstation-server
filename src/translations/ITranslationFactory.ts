import ITranslation from "./ITranslation";

interface ITranslationFactory {
    loadTranslation(locale: string): Promise<ITranslation>;
}

export default ITranslationFactory;
