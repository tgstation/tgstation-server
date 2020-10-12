"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.searchAllSubSchema = exports.getId = exports.getSubSchema = exports.parseSchema = void 0;
var tslib_1 = require("tslib");
var JsonPointer = tslib_1.__importStar(require("../jsonPointer"));
var schemaId_1 = tslib_1.__importDefault(require("./schemaId"));
function parseSchema(content, url) {
    var _a = selectSchemaType(content),
        type = _a.type,
        openApiVersion = _a.openApiVersion;
    if (url != null) {
        setId(type, content, url);
    }
    var id = getId(type, content);
    return {
        type: type,
        openApiVersion: openApiVersion,
        id: id ? new schemaId_1.default(id) : schemaId_1.default.empty,
        content: content
    };
}
exports.parseSchema = parseSchema;
function getSubSchema(rootSchema, pointer, id) {
    var content = JsonPointer.get(rootSchema.content, JsonPointer.parse(pointer));
    if (id == null) {
        var subId = getId(rootSchema.type, content);
        var getParentIds_1 = function (s, result) {
            result.push(s.id.getAbsoluteId());
            return s.rootSchema == null ? result : getParentIds_1(s.rootSchema, result);
        };
        if (subId) {
            id = new schemaId_1.default(subId, getParentIds_1(rootSchema, []));
        } else {
            id = new schemaId_1.default(pointer, getParentIds_1(rootSchema, []));
        }
    }
    return {
        type: rootSchema.type,
        id: id,
        content: content,
        rootSchema: rootSchema
    };
}
exports.getSubSchema = getSubSchema;
function getId(type, content) {
    return content[getIdPropertyName(type)];
}
exports.getId = getId;
function setId(type, content, id) {
    var key = getIdPropertyName(type);
    if (content[key] == null) {
        content[key] = id;
    }
}
function getIdPropertyName(type) {
    switch (type) {
        case "Draft04":
            return "id";
        case "Draft07":
            return "$id";
    }
}
function searchAllSubSchema(schema, onFoundSchema, onFoundReference) {
    var walkArray = function (array, paths, parentIds) {
        if (array == null) {
            return;
        }
        array.forEach(function (item, index) {
            walk(item, paths.concat(index.toString()), parentIds);
        });
    };
    var walkObject = function (obj, paths, parentIds) {
        if (obj == null) {
            return;
        }
        Object.keys(obj).forEach(function (key) {
            var sub = obj[key];
            if (sub != null) {
                walk(sub, paths.concat(key), parentIds);
            }
        });
    };
    var walkMaybeArray = function (item, paths, parentIds) {
        if (Array.isArray(item)) {
            walkArray(item, paths, parentIds);
        } else {
            walk(item, paths, parentIds);
        }
    };
    var walk = function (s, paths, parentIds) {
        if (s == null || typeof s !== "object") {
            return;
        }
        var id = getId(schema.type, s);
        if (id && typeof id === "string") {
            var schemaId = new schemaId_1.default(id, parentIds);
            var subSchema = {
                type: schema.type,
                id: schemaId,
                content: s,
                rootSchema: schema
            };
            onFoundSchema(subSchema);
            parentIds = parentIds.concat([schemaId.getAbsoluteId()]);
        }
        if (typeof s.$ref === "string") {
            var schemaId = new schemaId_1.default(s.$ref, parentIds);
            s.$ref = schemaId.getAbsoluteId();
            onFoundReference(schemaId);
        }
        walkArray(s.allOf, paths.concat("allOf"), parentIds);
        walkArray(s.anyOf, paths.concat("anyOf"), parentIds);
        walkArray(s.oneOf, paths.concat("oneOf"), parentIds);
        walk(s.not, paths.concat("not"), parentIds);
        walkMaybeArray(s.items, paths.concat("items"), parentIds);
        walk(s.additionalItems, paths.concat("additionalItems"), parentIds);
        walk(s.additionalProperties, paths.concat("additionalProperties"), parentIds);
        walkObject(s.definitions, paths.concat("definitions"), parentIds);
        walkObject(s.properties, paths.concat("properties"), parentIds);
        walkObject(s.patternProperties, paths.concat("patternProperties"), parentIds);
        walkMaybeArray(s.dependencies, paths.concat("dependencies"), parentIds);
        if (schema.type === "Draft07") {
            if ("propertyNames" in s) {
                walk(s.propertyNames, paths.concat("propertyNames"), parentIds);
                walk(s.contains, paths.concat("contains"), parentIds);
                walk(s.if, paths.concat("if"), parentIds);
                walk(s.then, paths.concat("then"), parentIds);
                walk(s.else, paths.concat("else"), parentIds);
            }
        }
    };
    function searchOpenApiSubSchema(openApi) {
        function createId(paths) {
            return "#/" + paths.map(convertKeyToTypeName).join("/");
        }
        function convertKeyToTypeName(key) {
            key = key.replace(/\/(.)/g, function (_match, p1) {
                return p1.toUpperCase();
            });
            return key
                .replace(/}/g, "")
                .replace(/{/g, "$")
                .replace(/^\//, "")
                .replace(/[^0-9A-Za-z_$]+/g, "_");
        }
        function setSubIdToAnyObject(f, obj, keys) {
            if (obj == null) {
                return;
            }
            Object.keys(obj).forEach(function (key) {
                var item = obj[key];
                f(item, keys.concat(convertKeyToTypeName(key)));
            });
        }
        var setSubIdToParameterObject = function (obj, keys) {
            return setSubIdToAnyObject(setSubIdToParameter, obj, keys);
        };
        function setSubIdToParameter(param, keys) {
            if ("schema" in param) {
                setSubId(param.schema, keys.concat(param.name));
            }
        }
        var setSubIdToParameterObjectNoName = function (obj, keys) {
            return setSubIdToAnyObject(setSubIdToParameterNoName, obj, keys);
        };
        function setSubIdToParameterNoName(param, keys) {
            if ("schema" in param) {
                setSubId(param.schema, keys);
            }
        }
        function setSubIdToParameters(array, keys) {
            if (array == null) {
                return;
            }
            var params = new Map();
            var refs = new Map();
            array.forEach(function (item) {
                var _a, _b, _c;
                if ("schema" in item) {
                    setSubIdToParameter(item, keys);
                    var work = params.get(item.in);
                    if (work == null) {
                        work = [];
                        params.set(item.in, work);
                    }
                    work.push(item);
                } else if ("$ref" in item) {
                    var result = /\/([^\/]*)$/.exec(item.$ref)[1];
                    if (
                        ((_a = item.$ref) === null || _a === void 0
                            ? void 0
                            : _a.includes("Api")) ||
                        ((_b = item.$ref) === null || _b === void 0
                            ? void 0
                            : _b.includes("User-Agent"))
                    ) {
                        return;
                    }
                    setSubId(item, keys.concat(result));
                    var work = void 0;
                    if (
                        (_c = item.$ref) === null || _c === void 0
                            ? void 0
                            : _c.includes("Instance")
                    ) {
                        work = refs.get("header");
                        if (work == null) {
                            work = [];
                            refs.set("header", work);
                        }
                    } else {
                        work = refs.get("path");
                        if (work == null) {
                            work = [];
                            refs.set("path", work);
                        }
                    }
                    work.push(item);
                }
            });
            addParameterSchema(params, refs, keys);
        }
        function addParameterSchema(params, refs, keys) {
            var e_1, _a, e_2, _b;
            try {
                for (
                    var params_1 = tslib_1.__values(params), params_1_1 = params_1.next();
                    !params_1_1.done;
                    params_1_1 = params_1.next()
                ) {
                    var _c = tslib_1.__read(params_1_1.value, 2),
                        key = _c[0],
                        param = _c[1];
                    var _d = tslib_1.__read(buildParameterSchema(key, param, keys), 2),
                        paths = _d[0],
                        obj = _d[1];
                    setSubId(obj, paths);
                }
            } catch (e_1_1) {
                e_1 = { error: e_1_1 };
            } finally {
                try {
                    if (params_1_1 && !params_1_1.done && (_a = params_1.return)) _a.call(params_1);
                } finally {
                    if (e_1) throw e_1.error;
                }
            }
            try {
                for (
                    var refs_1 = tslib_1.__values(refs), refs_1_1 = refs_1.next();
                    !refs_1_1.done;
                    refs_1_1 = refs_1.next()
                ) {
                    var _e = tslib_1.__read(refs_1_1.value, 2),
                        key = _e[0],
                        ref = _e[1];
                    var _f = tslib_1.__read(buildParameterSchemaRefs(key, ref, keys), 2),
                        paths = _f[0],
                        obj = _f[1];
                    setSubId(obj, paths);
                }
            } catch (e_2_1) {
                e_2 = { error: e_2_1 };
            } finally {
                try {
                    if (refs_1_1 && !refs_1_1.done && (_b = refs_1.return)) _b.call(refs_1);
                } finally {
                    if (e_2) throw e_2.error;
                }
            }
        }
        function buildParameterSchema(inType, params, keys) {
            var paths = keys.slice(0, keys.length - 1).concat(inType + "Parameters");
            var properties = {};
            params.forEach(function (item) {
                properties[item.name] = { $ref: createId(keys.concat(item.name)) };
            });
            return [
                paths,
                {
                    id: createId(paths),
                    type: "object",
                    properties: properties,
                    required: params
                        .filter(function (item) {
                            return item.required === true;
                        })
                        .map(function (item) {
                            return item.name;
                        })
                }
            ];
        }
        function buildParameterSchemaRefs(inType, refs, keys) {
            var paths = keys.slice(0, keys.length - 1).concat(inType + "Parameters");
            var properties = {};
            refs.forEach(function (item) {
                if (item.$ref != null) {
                    var result = /\/([^\/]*)$/.exec(item.$ref)[1];
                    properties[result] = { $ref: createId(keys.concat(result)) };
                }
            });
            return [
                paths,
                {
                    id: createId(paths),
                    type: "object",
                    properties: properties,
                    required: refs.map(function (item) {
                        return /\/([^\/]*)$/.exec(item.$ref)[1];
                    })
                }
            ];
        }
        var setSubIdToResponsesV2 = function (responses, keys) {
            return setSubIdToAnyObject(setSubIdToResponseV2, responses, keys);
        };
        function setSubIdToResponseV2(response, keys) {
            if (response == null) {
                return;
            }
            if ("schema" in response) {
                var s = response.schema;
                if (s != null && s.type === "file") {
                    return;
                }
                setSubId(s, keys);
            }
        }
        function setSubIdToOperationV2(ops, keys) {
            if (ops == null) {
                return;
            }
            var operationId = ops.operationId;
            if (operationId) {
                keys = [keys[0], convertKeyToTypeName(operationId)];
            }
            setSubIdToParameters(ops.parameters, keys.concat("parameters"));
            setSubIdToResponsesV2(ops.responses, keys.concat("responses"));
        }
        var setSubIdToPathsV2 = function (paths, keys) {
            return setSubIdToAnyObject(setSubIdToPathItemV2, paths, keys);
        };
        function setSubIdToPathItemV2(pathItem, keys) {
            setSubIdToParameters(pathItem.parameters, keys.concat("parameters"));
            setSubIdToOperationV2(pathItem.get, keys.concat("get"));
            setSubIdToOperationV2(pathItem.put, keys.concat("put"));
            setSubIdToOperationV2(pathItem.post, keys.concat("post"));
            setSubIdToOperationV2(pathItem.delete, keys.concat("delete"));
            setSubIdToOperationV2(pathItem.options, keys.concat("options"));
            setSubIdToOperationV2(pathItem.head, keys.concat("head"));
            setSubIdToOperationV2(pathItem.patch, keys.concat("patch"));
        }
        function setSubIdToMediaTypes(types, keys) {
            var e_3, _a;
            if (types == null) {
                return;
            }
            try {
                for (
                    var _b = tslib_1.__values(Object.keys(types)), _c = _b.next();
                    !_c.done;
                    _c = _b.next()
                ) {
                    var mime = _c.value;
                    if (
                        /^text\/|^(?:application\/x-www-form-urlencoded|application\/([a-z0-9-_]+\+)?json)$/.test(
                            mime
                        )
                    ) {
                        var mt = types[mime];
                        setSubId(mt.schema, keys);
                    }
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
        var setSubIdToRequestBodies = function (bodys, keys) {
            return setSubIdToAnyObject(setSubIdToRequestBody, bodys, keys);
        };
        function setSubIdToRequestBody(body, keys) {
            if (body == null) {
                return;
            }
            if ("content" in body) {
                setSubIdToMediaTypes(body.content, keys);
            } else if ("$ref" in body) {
                setSubId(body, keys);
            } else {
                setSubId({}, keys);
            }
        }
        var setSubIdToResponsesV3 = function (responses, keys) {
            return setSubIdToAnyObject(setSubIdToResponseV3, responses, keys);
        };
        function setSubIdToResponseV3(response, keys) {
            if (response == null) {
                return;
            }
            if ("content" in response) {
                setSubIdToMediaTypes(response.content, keys);
            } else if ("$ref" in response) {
                setSubId(response, keys);
            } else {
                setSubId({}, keys);
            }
        }
        function setSubIdToOperationV3(ops, keys) {
            if (ops == null) {
                return;
            }
            var operationId = ops.operationId;
            if (operationId) {
                keys = [keys[0], convertKeyToTypeName(operationId)];
            }
            setSubIdToParameters(ops.parameters, keys.concat("parameters"));
            setSubIdToRequestBody(ops.requestBody, keys.concat("requestBody"));
            setSubIdToResponsesV3(ops.responses, keys.concat("responses"));
        }
        var setSubIdToPathsV3 = function (paths, keys) {
            return setSubIdToAnyObject(setSubIdToPathItemV3, paths, keys);
        };
        function setSubIdToPathItemV3(pathItem, keys) {
            setSubIdToParameters(pathItem.parameters, keys.concat("parameters"));
            setSubIdToOperationV3(pathItem.get, keys.concat("get"));
            setSubIdToOperationV3(pathItem.put, keys.concat("put"));
            setSubIdToOperationV3(pathItem.post, keys.concat("post"));
            setSubIdToOperationV3(pathItem.delete, keys.concat("delete"));
            setSubIdToOperationV3(pathItem.options, keys.concat("options"));
            setSubIdToOperationV3(pathItem.head, keys.concat("head"));
            setSubIdToOperationV3(pathItem.patch, keys.concat("patch"));
            setSubIdToOperationV3(pathItem.trace, keys.concat("trace"));
        }
        function setSubIdToObject(obj, paths) {
            if (obj == null) {
                return;
            }
            Object.keys(obj).forEach(function (key) {
                var sub = obj[key];
                setSubId(sub, paths.concat(key));
            });
        }
        function setSubId(s, paths) {
            if (typeof s !== "object") {
                return;
            }
            if (typeof s.$ref === "string") {
                var thing = "#" + s.$ref.slice(1).split("/").map(convertKeyToTypeName).join("/");
                var schemaId = new schemaId_1.default(thing);
                s.$ref = schemaId.getAbsoluteId();
                onFoundReference(schemaId);
            }
            var id = createId(paths);
            setId(schema.type, s, id);
            walk(s, paths, []);
        }
        if ("swagger" in openApi) {
            setSubIdToObject(openApi.definitions, ["definitions"]);
            setSubIdToParameterObject(openApi.parameters, ["parameters"]);
            setSubIdToResponsesV2(openApi.responses, ["responses"]);
            setSubIdToPathsV2(openApi.paths, ["paths"]);
        } else {
            if (openApi.components) {
                var components = openApi.components;
                setSubIdToObject(components.schemas, ["components", "schemas"]);
                setSubIdToResponsesV3(components.responses, ["components", "responses"]);
                setSubIdToParameterObjectNoName(components.parameters, [
                    "components",
                    "parameters"
                ]);
                setSubIdToRequestBodies(components.requestBodies, ["components", "requestBodies"]);
            }
            if (openApi.paths) {
                setSubIdToPathsV3(openApi.paths, ["paths"]);
            }
        }
    }
    if (schema.openApiVersion != null) {
        var obj = schema.content;
        searchOpenApiSubSchema(obj);
        return;
    }
    walk(schema.content, ["#"], []);
}
exports.searchAllSubSchema = searchAllSubSchema;
function selectSchemaType(content) {
    if (content.$schema) {
        var schema = content.$schema;
        var match = schema.match(/http\:\/\/json-schema\.org\/draft-(\d+)\/schema#?/);
        if (match) {
            var version = Number(match[1]);
            if (version <= 4) {
                return { type: "Draft04" };
            } else {
                return { type: "Draft07" };
            }
        }
    }
    if (content.swagger === "2.0") {
        return {
            type: "Draft04",
            openApiVersion: 2
        };
    }
    if (content.openapi) {
        var openapi = content.openapi;
        if (/^3\.\d+\.\d+$/.test(openapi)) {
            return {
                type: "Draft07",
                openApiVersion: 3
            };
        }
    }
    return { type: "Draft04" };
}
//# sourceMappingURL=jsonSchema.js.map
