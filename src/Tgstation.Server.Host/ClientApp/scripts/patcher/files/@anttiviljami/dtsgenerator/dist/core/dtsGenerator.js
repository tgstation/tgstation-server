"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var tslib_1 = require("tslib");
var debug_1 = tslib_1.__importDefault(require("debug"));
var jsonPointer_1 = require("../jsonPointer");
var jsonSchema_1 = require("./jsonSchema");
var utils = tslib_1.__importStar(require("./utils"));
var debug = debug_1.default("dtsgen");
var typeMarker = Symbol();
var DtsGenerator = (function () {
    function DtsGenerator(resolver, convertor) {
        this.resolver = resolver;
        this.convertor = convertor;
    }
    DtsGenerator.prototype.generate = function () {
        return tslib_1.__awaiter(this, void 0, void 0, function () {
            var map, result;
            return tslib_1.__generator(this, function (_a) {
                switch (_a.label) {
                    case 0:
                        debug("generate type definition files.");
                        return [4, this.resolver.resolve()];
                    case 1:
                        _a.sent();
                        map = this.convertor.buildSchemaMergedMap(
                            this.resolver.getAllRegisteredSchema(),
                            typeMarker
                        );
                        this.convertor.start();
                        this.walk(map);
                        result = this.convertor.end();
                        return [2, result];
                }
            });
        });
    };
    DtsGenerator.prototype.walk = function (map) {
        var e_1, _a;
        var keys = Object.keys(map).sort();
        try {
            for (
                var keys_1 = tslib_1.__values(keys), keys_1_1 = keys_1.next();
                !keys_1_1.done;
                keys_1_1 = keys_1.next()
            ) {
                var key = keys_1_1.value;
                var value = map[key];
                if (value.hasOwnProperty(typeMarker)) {
                    var schema = value[typeMarker];
                    debug(
                        "  walk doProcess: key=" + key + " schemaId=" + schema.id.getAbsoluteId()
                    );
                    this.walkSchema(schema);
                    delete value[typeMarker];
                }
                if (typeof value === "object" && Object.keys(value).length > 0) {
                    this.convertor.startNest(key);
                    this.walk(value);
                    this.convertor.endNest();
                }
            }
        } catch (e_1_1) {
            e_1 = { error: e_1_1 };
        } finally {
            try {
                if (keys_1_1 && !keys_1_1.done && (_a = keys_1.return)) _a.call(keys_1);
            } finally {
                if (e_1) throw e_1.error;
            }
        }
    };
    DtsGenerator.prototype.walkSchema = function (schema) {
        var normalized = this.normalizeContent(schema);
        this.currentSchema = normalized;
        this.convertor.outputComments(normalized);
        var type = normalized.content.type;
        switch (type) {
            case "any":
                return this.generateAnyTypeModel(normalized);
            case "array":
                return this.generateTypeCollection(normalized);
            case "object":
            default:
                return this.generateDeclareType(normalized);
        }
    };
    DtsGenerator.prototype.normalizeContent = function (schema, pointer) {
        if (pointer != null) {
            schema = jsonSchema_1.getSubSchema(schema, pointer);
        }
        var content = this.normalizeSchemaContent(schema.content);
        return Object.assign({}, schema, { content: content });
    };
    DtsGenerator.prototype.normalizeSchemaContent = function (content) {
        var e_2, _a;
        if (typeof content === "boolean") {
            content = content ? {} : { not: {} };
        } else {
            if (content.allOf) {
                var work = content;
                try {
                    for (
                        var _b = tslib_1.__values(content.allOf), _c = _b.next();
                        !_c.done;
                        _c = _b.next()
                    ) {
                        var sub = _c.value;
                        if (typeof sub === "object" && sub.$ref && false) {
                            var ref = this.resolver.dereference(sub.$ref);
                            var normalized = this.normalizeContent(ref).content;
                            utils.mergeSchema(work, normalized);
                        } else if (typeof sub === "object") {
                            var normalized = this.normalizeSchemaContent(sub);
                            utils.mergeSchema(work, normalized);
                        } else {
                            utils.mergeSchema(work, sub);
                        }
                    }
                } catch (e_2_1) {
                    e_2 = { error: e_2_1 };
                } finally {
                    try {
                        if (_c && !_c.done && (_a = _b.return)) _a.call(_b);
                    } finally {
                        if (e_2) throw e_2.error;
                    }
                }
                delete content.allOf;
                content = work;
            }
            if (
                content.type === undefined &&
                (content.properties || content.additionalProperties)
            ) {
                content.type = "object";
            }
            if (content.nullable) {
                var type = content.type;
                if (type == null) {
                    content.type = "null";
                } else if (!Array.isArray(type)) {
                    content.type = [type, "null"];
                } else {
                    type.push("null");
                }
            }
            var types = content.type;
            if (Array.isArray(types)) {
                var reduced = utils.reduceTypes(types);
                content.type = reduced.length === 1 ? reduced[0] : reduced;
            }
        }
        return content;
    };
    DtsGenerator.prototype.generateDeclareType = function (schema) {
        var content = schema.content;
        if (
            content.$ref ||
            content.oneOf ||
            content.anyOf ||
            content.enum ||
            "const" in content ||
            content.type !== "object"
        ) {
            this.convertor.outputExportType(schema.id);
            this.generateTypeProperty(schema, true);
        } else {
            this.convertor.startInterfaceNest(schema.id);
            this.generateProperties(schema);
            this.convertor.endInterfaceNest();
        }
    };
    DtsGenerator.prototype.generateAnyTypeModel = function (schema) {
        this.convertor.startInterfaceNest(schema.id);
        this.convertor.outputRawValue("[name: string]: any; // any", true);
        this.convertor.endInterfaceNest();
    };
    DtsGenerator.prototype.generateTypeCollection = function (schema) {
        this.convertor.outputExportType(schema.id);
        this.generateArrayTypeProperty(schema, true);
    };
    DtsGenerator.prototype.generateProperties = function (baseSchema) {
        var e_3, _a;
        var content = baseSchema.content;
        if (content.additionalProperties) {
            this.convertor.outputRawValue("[name: string]: ");
            var schema = this.normalizeContent(baseSchema, "/additionalProperties");
            if (content.additionalProperties === true) {
                this.convertor.outputStringTypeName(schema, "any", true);
            } else {
                this.generateTypeProperty(schema, true);
            }
        }
        if (content.properties) {
            try {
                for (
                    var _b = tslib_1.__values(Object.keys(content.properties)), _c = _b.next();
                    !_c.done;
                    _c = _b.next()
                ) {
                    var propertyName = _c.value;
                    var schema = this.normalizeContent(
                        baseSchema,
                        "/properties/" + jsonPointer_1.tilde(propertyName)
                    );
                    this.convertor.outputComments(schema);
                    this.convertor.outputPropertyAttribute(schema);
                    this.convertor.outputPropertyName(
                        schema,
                        propertyName,
                        !!schema.content.nullable
                    );
                    this.generateTypeProperty(schema);
                }
            } catch (e_3_1) {
                e_3 = { error: e_3_1 };
            } finally {
                try {
                    if (_c && !_c.done && (_a = _b.return)) _a.call(_b);
                } finally {
                    if (e_3) throw e_3.error;
                }
            }
        }
    };
    DtsGenerator.prototype.generateTypeProperty = function (schema, terminate) {
        var _this = this;
        if (terminate === void 0) {
            terminate = true;
        }
        var content = schema.content;
        if (content.$ref) {
            var ref = this.resolver.dereference(content.$ref);
            if (ref.id == null) {
                throw new Error("target referenced id is nothing: " + content.$ref);
            }
            var refSchema = this.normalizeContent(ref);
            return this.convertor.outputTypeIdName(
                refSchema,
                this.currentSchema,
                terminate,
                !!content.nullable
            );
        }
        if (content.anyOf || content.oneOf) {
            this.generateArrayedType(schema, content.anyOf, "/anyOf/", terminate);
            this.generateArrayedType(schema, content.oneOf, "/oneOf/", terminate);
            return;
        }
        if (content.enum) {
            this.convertor.outputArrayedType(
                schema,
                content.enum,
                function (value) {
                    if (content.type === "integer" || content.type === "number") {
                        _this.convertor.outputRawValue("" + value);
                    } else {
                        _this.convertor.outputRawValue('"' + value + '"');
                    }
                },
                terminate
            );
        } else if ("const" in content) {
            var value = content.const;
            if (content.type === "integer" || content.type === "number") {
                this.convertor.outputStringTypeName(schema, "" + value, terminate);
            } else {
                this.convertor.outputStringTypeName(schema, '"' + value + '"', terminate);
            }
        } else {
            this.generateType(schema, terminate);
        }
    };
    DtsGenerator.prototype.generateArrayedType = function (baseSchema, contents, path, terminate) {
        var _this = this;
        if (contents) {
            this.convertor.outputArrayedType(
                baseSchema,
                contents,
                function (_content, index) {
                    var schema = _this.normalizeContent(baseSchema, path + index);
                    if (schema.id.isEmpty()) {
                        _this.generateTypeProperty(schema, false);
                    } else {
                        _this.convertor.outputTypeIdName(schema, _this.currentSchema, false);
                    }
                },
                terminate
            );
        }
    };
    DtsGenerator.prototype.generateArrayTypeProperty = function (schema, terminate) {
        if (terminate === void 0) {
            terminate = true;
        }
        var items = schema.content.items;
        var minItems = schema.content.minItems;
        var maxItems = schema.content.maxItems;
        if (items == null) {
            this.convertor.outputStringTypeName(schema, "any[]", terminate);
        } else if (!Array.isArray(items)) {
            this.generateTypeProperty(this.normalizeContent(schema, "/items"), false);
            this.convertor.outputStringTypeName(schema, "[]", terminate);
        } else if (items.length === 0 && minItems === undefined && maxItems === undefined) {
            this.convertor.outputStringTypeName(schema, "any[]", terminate);
        } else if (minItems != null && maxItems != null && maxItems < minItems) {
            this.convertor.outputStringTypeName(schema, "never", terminate);
        } else {
            this.convertor.outputRawValue("[");
            var itemCount = Math.max(minItems || 0, maxItems || 0, items.length);
            if (maxItems != null) {
                itemCount = Math.min(itemCount, maxItems);
            }
            for (var i = 0; i < itemCount; i++) {
                if (i > 0) {
                    this.convertor.outputRawValue(", ");
                }
                if (i < items.length) {
                    var type = this.normalizeContent(schema, "/items/" + i);
                    if (type.id.isEmpty()) {
                        this.generateTypeProperty(type, false);
                    } else {
                        this.convertor.outputTypeIdName(type, this.currentSchema, false);
                    }
                } else {
                    this.convertor.outputStringTypeName(schema, "any", false, false);
                }
                if (minItems == null || i >= minItems) {
                    this.convertor.outputRawValue("?");
                }
            }
            if (maxItems == null) {
                if (itemCount > 0) {
                    this.convertor.outputRawValue(", ");
                }
                this.convertor.outputStringTypeName(schema, "...any[]", false, false);
            }
            this.convertor.outputRawValue("]");
            this.convertor.outputStringTypeName(schema, "", terminate);
        }
    };
    DtsGenerator.prototype.generateType = function (schema, terminate, outputOptional) {
        var _this = this;
        if (outputOptional === void 0) {
            outputOptional = true;
        }
        var type = schema.content.type;
        if (type == null) {
            this.convertor.outputPrimitiveTypeName(schema, "void", terminate, outputOptional);
        } else if (typeof type === "string") {
            this.generateTypeName(schema, type, terminate, outputOptional);
        } else {
            var types = utils.reduceTypes(type);
            if (types.length <= 1) {
                schema.content.type = types[0];
                this.generateType(schema, terminate, outputOptional);
            } else {
                this.convertor.outputArrayedType(
                    schema,
                    types,
                    function (t) {
                        _this.generateTypeName(schema, t, false, false);
                    },
                    terminate
                );
            }
        }
    };
    DtsGenerator.prototype.generateTypeName = function (schema, type, terminate, outputOptional) {
        if (outputOptional === void 0) {
            outputOptional = true;
        }
        var tsType = utils.toTSType(type, schema.content);
        if (tsType) {
            this.convertor.outputPrimitiveTypeName(schema, tsType, terminate, outputOptional);
        } else if (type === "object") {
            this.convertor.startTypeNest();
            this.generateProperties(schema);
            this.convertor.endTypeNest(terminate);
        } else if (type === "array") {
            this.generateArrayTypeProperty(schema, terminate);
        } else {
            throw new Error("unknown type: " + type);
        }
    };
    return DtsGenerator;
})();
exports.default = DtsGenerator;
//# sourceMappingURL=dtsGenerator.js.map
