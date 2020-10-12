import ReferenceResolver from "./referenceResolver";
import SchemaConvertor from "./schemaConvertor";
export default class DtsGenerator {
    private resolver;
    private convertor;
    private currentSchema;
    constructor(resolver: ReferenceResolver, convertor: SchemaConvertor);
    generate(): Promise<string>;
    private walk;
    private walkSchema;
    private normalizeContent;
    private normalizeSchemaContent;
    private generateDeclareType;
    private generateAnyTypeModel;
    private generateTypeCollection;
    private generateProperties;
    private generateTypeProperty;
    private generateArrayedType;
    private generateArrayTypeProperty;
    private generateType;
    private generateTypeName;
}
