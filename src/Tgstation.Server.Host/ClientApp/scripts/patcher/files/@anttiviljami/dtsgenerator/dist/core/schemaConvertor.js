"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var tslib_1 = require("tslib");
var JsonPointer = tslib_1.__importStar(require("../jsonPointer"));
var typeNameConvertor_1 = require("./typeNameConvertor");
var SchemaConvertor = (function () {
    function SchemaConvertor(processor, convertor, namespaceName) {
        if (convertor === void 0) {
            convertor = typeNameConvertor_1.DefaultTypeNameConvertor;
        }
        this.processor = processor;
        this.convertor = convertor;
        this.exportedTypes = [];
        this.replaceLevel = 0;
        this.ns =
            namespaceName == null
                ? undefined
                : namespaceName.split("/").filter(function (s) {
                      return s.length > 0;
                  });
    }
    SchemaConvertor.prototype.getExports = function () {
        return this.exportedTypes;
    };
    SchemaConvertor.prototype.getLastTypeName = function (id) {
        var names = this.convertor(id);
        if (names.length > 0) {
            return names[names.length - 1];
        } else {
            return "";
        }
    };
    SchemaConvertor.prototype.buildSchemaMergedMap = function (schemas, typeMarker) {
        var e_1, _a, e_2, _b, _c;
        var map = {};
        var paths = [];
        var minLevel = Number.MAX_SAFE_INTEGER;
        try {
            for (
                var schemas_1 = tslib_1.__values(schemas), schemas_1_1 = schemas_1.next();
                !schemas_1_1.done;
                schemas_1_1 = schemas_1.next()
            ) {
                var type = schemas_1_1.value;
                var path = this.convertor(type.id);
                minLevel = Math.min(minLevel, path.length);
                paths.push({ path: path, type: type });
            }
        } catch (e_1_1) {
            e_1 = { error: e_1_1 };
        } finally {
            try {
                if (schemas_1_1 && !schemas_1_1.done && (_a = schemas_1.return)) _a.call(schemas_1);
            } finally {
                if (e_1) throw e_1.error;
            }
        }
        this.replaceLevel = minLevel;
        try {
            for (
                var paths_1 = tslib_1.__values(paths), paths_1_1 = paths_1.next();
                !paths_1_1.done;
                paths_1_1 = paths_1.next()
            ) {
                var item = paths_1_1.value;
                var path = item.path;
                this.replaceNamespace(path);
                var parent_1 = JsonPointer.get(map, path, true);
                if (parent_1 == null) {
                    JsonPointer.set(map, path, ((_c = {}), (_c[typeMarker] = item.type), _c));
                } else {
                    parent_1[typeMarker] = item.type;
                }
            }
        } catch (e_2_1) {
            e_2 = { error: e_2_1 };
        } finally {
            try {
                if (paths_1_1 && !paths_1_1.done && (_b = paths_1.return)) _b.call(paths_1);
            } finally {
                if (e_2) throw e_2.error;
            }
        }
        if (Object.keys(map).length === 0) {
            throw new Error("There is no schema in the input contents.");
        }
        return map;
    };
    SchemaConvertor.prototype.replaceNamespace = function (paths) {
        if (this.ns == null) {
            return;
        }
        paths.splice(0, this.replaceLevel - 1);
        if (this.ns.length > 0) {
            paths.unshift.apply(paths, tslib_1.__spread(this.ns));
        }
    };
    SchemaConvertor.prototype.start = function () {
        this.processor.clear();
    };
    SchemaConvertor.prototype.end = function () {
        return this.processor.toDefinition();
    };
    SchemaConvertor.prototype.startNest = function (name) {
        var processor = this.processor;
        if (processor.indentLevel === 0) {
            processor.output("declare ");
        }
        processor.output("namespace ").outputType(name, true).outputLine(" {");
        processor.increaseIndent();
    };
    SchemaConvertor.prototype.endNest = function () {
        var processor = this.processor;
        processor.decreaseIndent();
        processor.outputLine("}");
    };
    SchemaConvertor.prototype.startInterfaceNest = function (id) {
        var processor = this.processor;
        if (processor.indentLevel === 0 && (this.ns == null || this.ns.length > 0)) {
            processor.output("declare ");
        } else {
            processor.output("export ");
        }
        var name = this.getLastTypeName(id);
        processor.output("interface ").outputType(name).output(" ");
        this.startTypeNest();
        this.addExport(id);
    };
    SchemaConvertor.prototype.endInterfaceNest = function () {
        this.endTypeNest(false);
        this.processor.outputLine();
    };
    SchemaConvertor.prototype.outputExportType = function (id) {
        var processor = this.processor;
        if (processor.indentLevel === 0 && (this.ns == null || this.ns.length > 0)) {
            processor.output("declare ");
        } else {
            processor.output("export ");
        }
        var name = this.getLastTypeName(id);
        processor.output("type ").outputType(name).output(" = ");
        this.addExport(id);
    };
    SchemaConvertor.prototype.startTypeNest = function () {
        this.processor.outputLine("{");
        this.processor.increaseIndent();
    };
    SchemaConvertor.prototype.endTypeNest = function (terminate) {
        this.processor.decreaseIndent();
        this.processor.output("}");
        if (terminate) {
            this.processor.outputLine(";");
        }
    };
    SchemaConvertor.prototype.outputRawValue = function (value, isEndOfLine) {
        if (isEndOfLine === void 0) {
            isEndOfLine = false;
        }
        this.processor.output(value);
        if (isEndOfLine) {
            this.processor.outputLine();
        }
    };
    SchemaConvertor.prototype.outputComments = function (schema) {
        var _a;
        var content = schema.content;
        var comments = [];
        if ("$comment" in content) {
            comments.push(content.$comment);
        }
        comments.push(content.title);
        comments.push(content.description);
        if ("example" in content || "examples" in content) {
            comments.push("example:");
            if ("example" in content) {
                comments.push(content.example);
            }
            if ("examples" in content) {
                comments.push.apply(comments, tslib_1.__spread(content.examples));
            }
        }
        (_a = this.processor).outputJSDoc.apply(_a, tslib_1.__spread(comments));
    };
    SchemaConvertor.prototype.outputPropertyName = function (_schema, propertyName, optional) {
        this.processor.outputKey(propertyName, optional).output(": ");
    };
    SchemaConvertor.prototype.outputPropertyAttribute = function (schema) {
        var content = schema.content;
        if ("readOnly" in content && content.readOnly) {
            this.processor.output("readonly ");
        }
    };
    SchemaConvertor.prototype.outputArrayedType = function (
        schema,
        types,
        output,
        terminate,
        outputOptional
    ) {
        var _this = this;
        if (outputOptional === void 0) {
            outputOptional = true;
        }
        if (!terminate) {
            this.processor.output("(");
        }
        types.forEach(function (t, index) {
            output(t, index);
            if (index < types.length - 1) {
                _this.processor.output(" | ");
            }
        });
        if (!terminate) {
            this.processor.output(")");
        }
        this.outputTypeNameTrailer(schema, terminate, outputOptional);
    };
    SchemaConvertor.prototype.outputTypeIdName = function (
        schema,
        currentSchema,
        terminate,
        nullable,
        outputOptional
    ) {
        var _this = this;
        if (terminate === void 0) {
            terminate = true;
        }
        if (nullable === void 0) {
            nullable = false;
        }
        if (outputOptional === void 0) {
            outputOptional = true;
        }
        var typeName = this.getTypename(schema.id, currentSchema);
        typeName.forEach(function (type, index) {
            var isLast = index === typeName.length - 1;
            _this.processor.outputType(type, isLast ? false : true);
            if (!isLast) {
                _this.processor.output(".");
            }
        });
        this.outputTypeNameTrailer(schema, terminate, outputOptional);
    };
    SchemaConvertor.prototype.getTypename = function (id, baseSchema) {
        var e_3, _a;
        var result = this.convertor(id);
        this.replaceNamespace(result);
        var baseId = baseSchema.id;
        if (baseId) {
            var baseTypes = this.convertor(baseId).slice(0, -1);
            try {
                for (
                    var baseTypes_1 = tslib_1.__values(baseTypes),
                        baseTypes_1_1 = baseTypes_1.next();
                    !baseTypes_1_1.done;
                    baseTypes_1_1 = baseTypes_1.next()
                ) {
                    var type = baseTypes_1_1.value;
                    if (result[0] === type) {
                        result.shift();
                    } else {
                        break;
                    }
                }
            } catch (e_3_1) {
                e_3 = { error: e_3_1 };
            } finally {
                try {
                    if (baseTypes_1_1 && !baseTypes_1_1.done && (_a = baseTypes_1.return))
                        _a.call(baseTypes_1);
                } finally {
                    if (e_3) throw e_3.error;
                }
            }
            if (result.length === 0) {
                return [this.getLastTypeName(id)];
            }
        }
        return result;
    };
    SchemaConvertor.prototype.outputPrimitiveTypeName = function (
        schema,
        typeName,
        terminate,
        outputOptional
    ) {
        if (terminate === void 0) {
            terminate = true;
        }
        if (outputOptional === void 0) {
            outputOptional = true;
        }
        this.processor.outputType(typeName, true);
        this.outputTypeNameTrailer(schema, terminate, outputOptional);
    };
    SchemaConvertor.prototype.outputStringTypeName = function (
        schema,
        typeName,
        terminate,
        outputOptional
    ) {
        if (outputOptional === void 0) {
            outputOptional = true;
        }
        if (typeName) {
            this.processor.output(typeName);
        }
        this.outputTypeNameTrailer(schema, terminate, outputOptional);
    };
    SchemaConvertor.prototype.outputTypeNameTrailer = function (
        schema,
        terminate,
        outputOptional,
        nullable
    ) {
        if (nullable === void 0) {
            nullable = false;
        }

        if (nullable) {
            this.processor.output(" | null");
        }
        if (terminate) {
            this.processor.output(";");
        }
        if (outputOptional) {
            this.outputOptionalInformation(schema, terminate);
        }
        if (terminate) {
            this.processor.outputLine();
        }
    };
    SchemaConvertor.prototype.outputOptionalInformation = function (schema, terminate) {
        var format = schema.content.format;
        var pattern = schema.content.pattern;
        if (!format && !pattern) {
            return;
        }
        if (terminate) {
            this.processor.output(" //");
        } else {
            this.processor.output(" /*");
        }
        if (format) {
            this.processor.output(" ").output(format);
        }
        if (pattern) {
            this.processor.output(" ").output(pattern);
        }
        if (!terminate) {
            this.processor.output(" */ ");
        }
    };
    SchemaConvertor.prototype.addExport = function (id) {
        var name = this.getLastTypeName(id);
        var schemaRef = id.getJsonPointerHash();
        var names = this.convertor(id);
        var path = names.join(".");
        this.exportedTypes.push({ name: name, path: path, schemaRef: schemaRef });
    };
    return SchemaConvertor;
})();
exports.default = SchemaConvertor;
//# sourceMappingURL=schemaConvertor.js.map
