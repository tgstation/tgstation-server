import ITranslation from "./ITranslation";
import ITranslationFactory from "./ITranslationFactory";
import Locales from "./Locales";
import Translation from "./Translation";

class TranslationFactory implements ITranslationFactory {
    private static readonly fallbackLocale: string = Locales.en;

    private static getShortHandedLocale(locale: string): string {
        return locale.split("-")[0];
    }

    public async loadTranslation(locale: string): Promise<ITranslation> {
        //fancy type annotations but its just load the json file in this variable as a map of strings to strings
        const localization: { [key: string]: string } = (await import(
            `./locales/${locale}.json`
        )) as { [key: string]: string };

        if (!localization) {
            let shortHandedLocale = TranslationFactory.getShortHandedLocale(locale);
            if (shortHandedLocale === locale) {
                if (shortHandedLocale === TranslationFactory.fallbackLocale)
                    throw new Error("Invalid locale: " + locale);
                shortHandedLocale = TranslationFactory.fallbackLocale;
            }
            return await this.loadTranslation(shortHandedLocale);
        }

        let model: ITranslation | null = null;
        try {
            model = new Translation(locale, localization);
        } catch (e) {
            throw Error(`Error loading localization for locale '${locale}': ${JSON.stringify(e)}`);
        }

        return model;
    }
}

export default TranslationFactory;
