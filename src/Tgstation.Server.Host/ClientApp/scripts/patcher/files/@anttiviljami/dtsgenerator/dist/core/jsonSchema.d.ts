import SchemaId from "./schemaId";
export declare type JsonSchema = JsonSchemaOrg.Draft04.Schema | JsonSchemaOrg.Draft07.Schema;
export declare type JsonSchemaObject =
    | JsonSchemaOrg.Draft04.Schema
    | JsonSchemaOrg.Draft07.SchemaObject;
export declare type SchemaType = "Draft04" | "Draft07";
export interface Schema {
    type: SchemaType;
    openApiVersion?: 2 | 3;
    id: SchemaId;
    content: JsonSchema;
    rootSchema?: Schema;
}
export interface NormalizedSchema extends Schema {
    content: JsonSchemaObject;
}
export declare function parseSchema(content: any, url?: string): Schema;
export declare function getSubSchema(rootSchema: Schema, pointer: string, id?: SchemaId): Schema;
export declare function getId(type: SchemaType, content: any): string | undefined;
export declare function searchAllSubSchema(
    schema: Schema,
    onFoundSchema: (subSchema: Schema) => void,
    onFoundReference: (refId: SchemaId) => void
): void;
