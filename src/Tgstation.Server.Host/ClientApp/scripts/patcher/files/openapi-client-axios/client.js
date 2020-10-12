"use strict";
var __assign =
    (this && this.__assign) ||
    function () {
        __assign =
            Object.assign ||
            function (t) {
                for (var s, i = 1, n = arguments.length; i < n; i++) {
                    s = arguments[i];
                    for (var p in s) if (Object.prototype.hasOwnProperty.call(s, p)) t[p] = s[p];
                }
                return t;
            };
        return __assign.apply(this, arguments);
    };
var __awaiter =
    (this && this.__awaiter) ||
    function (thisArg, _arguments, P, generator) {
        return new (P || (P = Promise))(function (resolve, reject) {
            function fulfilled(value) {
                try {
                    step(generator.next(value));
                } catch (e) {
                    reject(e);
                }
            }
            function rejected(value) {
                try {
                    step(generator["throw"](value));
                } catch (e) {
                    reject(e);
                }
            }
            function step(result) {
                result.done
                    ? resolve(result.value)
                    : new P(function (resolve) {
                          resolve(result.value);
                      }).then(fulfilled, rejected);
            }
            step((generator = generator.apply(thisArg, _arguments || [])).next());
        });
    };
var __generator =
    (this && this.__generator) ||
    function (thisArg, body) {
        var _ = {
                label: 0,
                sent: function () {
                    if (t[0] & 1) throw t[1];
                    return t[1];
                },
                trys: [],
                ops: []
            },
            f,
            y,
            t,
            g;
        return (
            (g = { next: verb(0), throw: verb(1), return: verb(2) }),
            typeof Symbol === "function" &&
                (g[Symbol.iterator] = function () {
                    return this;
                }),
            g
        );
        function verb(n) {
            return function (v) {
                return step([n, v]);
            };
        }
        function step(op) {
            if (f) throw new TypeError("Generator is already executing.");
            while (_)
                try {
                    if (
                        ((f = 1),
                        y &&
                            (t =
                                op[0] & 2
                                    ? y["return"]
                                    : op[0]
                                    ? y["throw"] || ((t = y["return"]) && t.call(y), 0)
                                    : y.next) &&
                            !(t = t.call(y, op[1])).done)
                    )
                        return t;
                    if (((y = 0), t)) op = [op[0] & 2, t.value];
                    switch (op[0]) {
                        case 0:
                        case 1:
                            t = op;
                            break;
                        case 4:
                            _.label++;
                            return { value: op[1], done: false };
                        case 5:
                            _.label++;
                            y = op[1];
                            op = [0];
                            continue;
                        case 7:
                            op = _.ops.pop();
                            _.trys.pop();
                            continue;
                        default:
                            if (
                                !((t = _.trys), (t = t.length > 0 && t[t.length - 1])) &&
                                (op[0] === 6 || op[0] === 2)
                            ) {
                                _ = 0;
                                continue;
                            }
                            if (op[0] === 3 && (!t || (op[1] > t[0] && op[1] < t[3]))) {
                                _.label = op[1];
                                break;
                            }
                            if (op[0] === 6 && _.label < t[1]) {
                                _.label = t[1];
                                t = op;
                                break;
                            }
                            if (t && _.label < t[2]) {
                                _.label = t[2];
                                _.ops.push(op);
                                break;
                            }
                            if (t[2]) _.ops.pop();
                            _.trys.pop();
                            continue;
                    }
                    op = body.call(thisArg, _);
                } catch (e) {
                    op = [6, e];
                    y = 0;
                } finally {
                    f = t = 0;
                }
            if (op[0] & 5) throw op[1];
            return { value: op[0] ? op[1] : void 0, done: true };
        }
    };
var __importDefault =
    (this && this.__importDefault) ||
    function (mod) {
        return mod && mod.__esModule ? mod : { default: mod };
    };
Object.defineProperty(exports, "__esModule", { value: true });
var lodash_1 = __importDefault(require("lodash"));
var axios_1 = __importDefault(require("axios"));
var bath_es5_1 = __importDefault(require("bath-es5"));
var openapi_schema_validation_1 = require("openapi-schema-validation");
var swagger_parser_1 = __importDefault(require("swagger-parser"));
var query_string_1 = __importDefault(require("query-string"));
var json_schema_deref_sync_1 = __importDefault(require("json-schema-deref-sync"));
var client_1 = require("./types/client");
/**
 * Main class and the default export of the 'openapi-client-axios' module
 *
 * @export
 * @class OpenAPIClientAxios
 */
var OpenAPIClientAxios = /** @class */ (function () {
    /**
     * Creates an instance of OpenAPIClientAxios.
     *
     * @param opts - constructor options
     * @param {Document | string} opts.definition - the OpenAPI definition, file path or Document object
     * @param {boolean} opts.strict - strict mode, throw errors or warn on OpenAPI spec validation errors (default: false)
     * @param {boolean} opts.validate - whether to validate the input document document (default: true)
     * @param {boolean} opts.axiosConfigDefaults - default axios config for the instance
     * @memberof OpenAPIClientAxios
     */
    function OpenAPIClientAxios(opts) {
        var _this = this;
        /**
         * Returns the instance of OpenAPIClient
         *
         * @returns
         * @memberof OpenAPIClientAxios
         */
        this.getClient = function () {
            return __awaiter(_this, void 0, void 0, function () {
                return __generator(this, function (_a) {
                    if (!this.initalized) {
                        return [2 /*return*/, this.init()];
                    }
                    return [2 /*return*/, this.instance];
                });
            });
        };
        /**
         * Initalizes OpenAPIClientAxios and creates a member axios client instance
         *
         * The init() method should be called right after creating a new instance of OpenAPIClientAxios
         *
         * @returns AxiosInstance
         * @memberof OpenAPIClientAxios
         */
        this.init = function () {
            return __awaiter(_this, void 0, void 0, function () {
                var _a, _b;
                return __generator(this, function (_c) {
                    switch (_c.label) {
                        case 0:
                            // parse the document
                            _a = this;
                            return [
                                4 /*yield*/,
                                swagger_parser_1.default.parse(this.inputDocument)
                            ];
                        case 1:
                            // parse the document
                            _a.document = _c.sent();
                            try {
                                if (this.validate) {
                                    // validate the document
                                    this.validateDefinition();
                                }
                            } catch (err) {
                                if (this.strict) {
                                    // in strict-mode, fail hard and re-throw the error
                                    throw err;
                                } else {
                                    // just emit a warning about the validation errors
                                    console.warn(err);
                                }
                            }
                            // dereference the document into definition
                            _b = this;
                            return [
                                4 /*yield*/,
                                swagger_parser_1.default.dereference(this.inputDocument)
                            ];
                        case 2:
                            // dereference the document into definition
                            _b.definition = _c.sent();
                            // create axios instance
                            this.instance = this.createAxiosInstance();
                            // we are now initalized
                            this.initalized = true;
                            return [2 /*return*/, this.instance];
                    }
                });
            });
        };
        /**
         * Synchronous version of .init()
         *
         * Note: Only works when the input definition is a valid OpenAPI v3 object and doesn't contain remote $refs.
         *
         * @memberof OpenAPIClientAxios
         */
        this.initSync = function () {
            if (typeof _this.inputDocument !== "object") {
                throw new Error(
                    ".initSync() can't be called with a non-object definition. Please use .init()"
                );
            }
            // set document
            _this.document = _this.inputDocument;
            try {
                if (_this.validate) {
                    // validate the document
                    _this.validateDefinition();
                }
            } catch (err) {
                if (_this.strict) {
                    // in strict-mode, fail hard and re-throw the error
                    throw err;
                } else {
                    // just emit a warning about the validation errors
                    console.warn(err);
                }
            }
            // dereference the document into definition
            _this.definition = json_schema_deref_sync_1.default(_this.inputDocument);
            // create axios instance
            _this.instance = _this.createAxiosInstance();
            // we are now initalized
            _this.initalized = true;
            return _this.instance;
        };
        /**
         * Creates a new axios instance, extends it and returns it
         *
         * @memberof OpenAPIClientAxios
         */
        this.createAxiosInstance = function () {
            // create axios instance
            var instance = axios_1.default.create(_this.axiosConfigDefaults);
            // set baseURL to the one found in the definition servers (if not set in axios defaults)
            var baseURL = _this.getBaseURL();
            if (baseURL && !_this.axiosConfigDefaults.baseURL) {
                instance.defaults.baseURL = baseURL;
            }
            // create methods for operationIds
            var operations = _this.getOperations();
            for (var _i = 0, operations_1 = operations; _i < operations_1.length; _i++) {
                var operation = operations_1[_i];
                var operationId = operation.operationId;
                if (operationId) {
                    instance[operationId] = _this.createOperationMethod(operation);
                }
            }
            // create paths dictionary
            // Example: api.paths['/pets/{id}'].get({ id: 1 });
            instance.paths = {};
            for (var path in _this.definition.paths) {
                if (_this.definition.paths[path]) {
                    if (!instance.paths[path]) {
                        instance.paths[path] = {};
                    }
                    var methods = _this.definition.paths[path];
                    for (var m in methods) {
                        if (
                            methods[m] &&
                            lodash_1.default.includes(Object.values(client_1.HttpMethod), m)
                        ) {
                            var method = m;
                            var operation = lodash_1.default.find(_this.getOperations(), {
                                path: path,
                                method: method
                            });
                            instance.paths[path][method] = _this.createOperationMethod(operation);
                        }
                    }
                }
            }
            // add reference to parent class instance
            instance.api = _this;
            return instance;
        };
        /**
         * Validates this.document, which is the parsed OpenAPI document. Throws an error if validation fails.
         *
         * @returns {Document} parsed document
         * @memberof OpenAPIClientAxios
         */
        this.validateDefinition = function () {
            var _a = openapi_schema_validation_1.validate(_this.document, 3),
                valid = _a.valid,
                errors = _a.errors;
            if (!valid) {
                var prettyErrors = JSON.stringify(errors, null, 2);
                throw new Error(
                    "Document is not valid OpenAPI. " +
                        errors.length +
                        " validation errors:\n" +
                        prettyErrors
                );
            }
            return _this.document;
        };
        /**
         * Gets the API baseurl defined in the first OpenAPI specification servers property
         *
         * @returns string
         * @memberof OpenAPIClientAxios
         */
        this.getBaseURL = function (operation) {
            if (!_this.definition) {
                return undefined;
            }
            if (operation) {
                if (typeof operation === "string") {
                    operation = _this.getOperation(operation);
                }
                if (operation.servers && operation.servers[0]) {
                    return operation.servers[0].url;
                }
            }
            if (_this.definition.servers && _this.definition.servers[0]) {
                return _this.definition.servers[0].url;
            }
            return undefined;
        };
        /**
         * Creates an axios config object for operation + arguments
         * @memberof OpenAPIClientAxios
         */
        this.getAxiosConfigForOperation = function (operation, args) {
            if (typeof operation === "string") {
                operation = _this.getOperation(operation);
            }
            var request = _this.getRequestConfigForOperation(operation, args);
            // construct axios request config
            var axiosConfig = {
                method: request.method,
                url: request.path,
                data: request.payload,
                params: request.query,
                headers: request.headers
            };
            // allow overriding baseURL with operation / path specific servers
            var servers = operation.servers;
            if (servers && servers[0]) {
                axiosConfig.baseURL = servers[0].url;
            }
            // allow overriding any parameters in AxiosRequestConfig
            var config = args[2];
            return config ? lodash_1.default.merge(axiosConfig, config) : axiosConfig;
        };
        /**
         * Creates a generic request config object for operation + arguments.
         *
         * This function contains the logic that handles operation method parameters.
         *
         * @memberof OpenAPIClientAxios
         */
        this.getRequestConfigForOperation = function (operation, args) {
            if (typeof operation === "string") {
                operation = _this.getOperation(operation);
            }
            var pathParams = {};
            var query = {};
            var headers = {};
            var cookies = {};
            var setRequestParam = function (name, value, type) {
                switch (type) {
                    case client_1.ParamType.Path:
                        pathParams[name] = value;
                        break;
                    case client_1.ParamType.Query:
                        query[name] = value;
                        break;
                    case client_1.ParamType.Header:
                        headers[name] = value;
                        break;
                    case client_1.ParamType.Cookie:
                        cookies[name] = value;
                        break;
                }
            };
            var getParamType = function (paramName) {
                var param = lodash_1.default.find(operation.parameters, { name: paramName });
                if (param) {
                    return param.in;
                }
                // default all params to query if operation doesn't specify param
                return client_1.ParamType.Query;
            };
            var getFirstOperationParam = function () {
                var firstRequiredParam = lodash_1.default.find(operation.parameters, {
                    required: true
                });
                if (firstRequiredParam) {
                    return firstRequiredParam;
                }
                var firstParam = lodash_1.default.first(operation.parameters);
                if (firstParam) {
                    return firstParam;
                }
            };
            var paramsArg = args[0],
                payload = args[1];
            if (lodash_1.default.isArray(paramsArg)) {
                // ParamsArray
                for (var _i = 0, _a = paramsArg; _i < _a.length; _i++) {
                    var param = _a[_i];
                    setRequestParam(param.name, param.value, param.in || getParamType(param.name));
                }
            } else if (typeof paramsArg === "object") {
                // ParamsObject
                for (var name in paramsArg) {
                    if (paramsArg[name] !== undefined) {
                        setRequestParam(name, paramsArg[name], getParamType(name));
                    }
                }
            } else if (!lodash_1.default.isNil(paramsArg)) {
                var firstParam = getFirstOperationParam();
                if (!firstParam) {
                    throw new Error("No parameters found for operation " + operation.operationId);
                }
                setRequestParam(firstParam.name, paramsArg, firstParam.in);
            }
            // path parameters
            var pathBuilder = bath_es5_1.default(operation.path);
            // make sure all path parameters are set
            for (var _b = 0, _c = pathBuilder.names; _b < _c.length; _b++) {
                var name = _c[_b];
                var value = pathParams[name];
                pathParams[name] = "" + value;
            }
            var path = pathBuilder.path(pathParams);
            // query parameters
            var queryString = query_string_1.default.stringify(query, { arrayFormat: "none" });
            // full url with query string
            var url =
                "" + _this.getBaseURL(operation) + path + (queryString ? "?" + queryString : "");
            // construct request config
            var config = {
                method: operation.method,
                url: url,
                path: path,
                pathParams: pathParams,
                query: query,
                queryString: queryString,
                headers: headers,
                cookies: cookies,
                payload: payload
            };
            return config;
        };
        /**
         * Flattens operations into a simple array of Operation objects easy to work with
         *
         * @returns {Operation[]}
         * @memberof OpenAPIBackend
         */
        this.getOperations = function () {
            var paths = lodash_1.default.get(_this.definition, "paths", {});
            return lodash_1.default
                .chain(paths)
                .entries()
                .flatMap(function (_a) {
                    var path = _a[0],
                        pathObject = _a[1];
                    var methods = lodash_1.default.pick(
                        pathObject,
                        lodash_1.default.values(client_1.HttpMethod)
                    );
                    return lodash_1.default.map(lodash_1.default.entries(methods), function (_a) {
                        var method = _a[0],
                            operation = _a[1];
                        operation.operationId = operation.operationId.replace(
                            /[^0-9A-Za-z_$]+/g,
                            "_"
                        );
                        var op = __assign({}, operation, { path: path, method: method });
                        if (pathObject.parameters) {
                            op.parameters = (op.parameters || []).concat(pathObject.parameters);
                        }
                        if (pathObject.servers) {
                            op.servers = (op.servers || []).concat(pathObject.servers);
                        }
                        return op;
                    });
                })
                .value();
        };
        /**
         * Gets a single operation based on operationId
         *
         * @param {string} operationId
         * @returns {Operation}
         * @memberof OpenAPIBackend
         */
        this.getOperation = function (operationId) {
            return lodash_1.default.find(_this.getOperations(), { operationId: operationId });
        };
        /**
         * Creates an axios method for an operation
         * (...pathParams, data?, config?) => Promise<AxiosResponse>
         *
         * @param {Operation} operation
         * @memberof OpenAPIClientAxios
         */
        this.createOperationMethod = function (operation) {
            return function () {
                var args = [];
                for (var _i = 0; _i < arguments.length; _i++) {
                    args[_i] = arguments[_i];
                }
                return __awaiter(_this, void 0, void 0, function () {
                    var axiosConfig;
                    return __generator(this, function (_a) {
                        axiosConfig = this.getAxiosConfigForOperation(operation, args);
                        // do the axios request
                        return [2 /*return*/, this.client.request(axiosConfig)];
                    });
                });
            };
        };
        var optsWithDefaults = __assign({ validate: true, strict: false }, opts, {
            axiosConfigDefaults: __assign(
                {
                    paramsSerializer: function (params) {
                        return query_string_1.default.stringify(params, { arrayFormat: "none" });
                    }
                },
                opts.axiosConfigDefaults || {}
            )
        });
        this.inputDocument = optsWithDefaults.definition;
        this.strict = optsWithDefaults.strict;
        this.validate = optsWithDefaults.validate;
        this.axiosConfigDefaults = optsWithDefaults.axiosConfigDefaults;
    }
    Object.defineProperty(OpenAPIClientAxios.prototype, "client", {
        /**
         * Returns the instance of OpenAPIClient
         *
         * @readonly
         * @type {OpenAPIClient}
         * @memberof OpenAPIClientAxios
         */
        get: function () {
            return this.instance;
        },
        enumerable: true,
        configurable: true
    });
    return OpenAPIClientAxios;
})();
exports.OpenAPIClientAxios = OpenAPIClientAxios;
//# sourceMappingURL=client.js.map
