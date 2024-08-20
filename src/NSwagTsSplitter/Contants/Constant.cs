
using System.Collections.Generic;

namespace NSwagTsSplitter.Contants
{
    public static class Constant
    {
        public static readonly List<string> IgnoreModules = new List<string>()
        {
            "jQuery"
        };

        public static readonly List<string> UtilitiesModules = new List<string>()
        {
            "throwException",
            "FileParameter",
            "FileResponse",
            "SwaggerException",
            "ServiceBase",
            "blobToText",
            "jsonParse",
            "createInstance",
            "parseDateOnly"
        };

        public static List<string> TsBaseType = new()
        {
            "string","number","Date","undefined","any","boolean","void","{ [key: string]: any; }","{ [key: string]: string; }"
        };

        public static string OAuth2OpenAPI = "{\"openapi\":\"3.0.3\",\"info\":{\"title\":\"OAuth2 API\",\"description\":\"包含所有授权模式的OAuth2 API的OpenAPI 3.0模型\",\"version\":\"1.0.0\"},\"paths\":{\"/connect/authorize\":{\"get\":{\"tags\":[\"OAuth2\"],\"summary\":\"Authorization Endpoint\",\"description\":\"获取授权码的Endpoint\",\"parameters\":[{\"name\":\"response_type\",\"in\":\"query\",\"required\":true,\"schema\":{\"$ref\":\"#/components/schemas/ResponseType\"}},{\"name\":\"client_id\",\"in\":\"query\",\"required\":true,\"schema\":{\"type\":\"string\"}},{\"name\":\"redirect_uri\",\"in\":\"query\",\"required\":true,\"schema\":{\"type\":\"string\"}},{\"name\":\"scope\",\"in\":\"query\",\"required\":true,\"schema\":{\"type\":\"string\"}},{\"name\":\"state\",\"in\":\"query\",\"required\":false,\"schema\":{\"type\":\"string\"}}],\"responses\":{\"302\":{\"description\":\"Redirect to the client with authorization code\"}}}},\"/connect/token\":{\"post\":{\"tags\":[\"OAuth2\"],\"summary\":\"Token Endpoint\",\"description\":\"获取访问令牌的Endpoint\",\"requestBody\":{\"content\":{\"application/x-www-form-urlencoded\":{\"schema\":{\"$ref\":\"#/components/schemas/TokenRequest\"}}}},\"responses\":{\"200\":{\"description\":\"返回访问令牌\",\"content\":{\"application/json\":{\"schema\":{\"$ref\":\"#/components/schemas/TokenResponse\"}}}},\"400\":{\"description\":\"错误的请求\"}}}},\"/device\":{\"post\":{\"tags\":[\"OAuth2\"],\"summary\":\"Device Authorization Endpoint\",\"description\":\"获取设备代码和用户代码的Endpoint\",\"requestBody\":{\"content\":{\"application/x-www-form-urlencoded\":{\"schema\":{\"$ref\":\"#/components/schemas/DeviceRequest\"}}}},\"responses\":{\"200\":{\"description\":\"返回设备代码和用户代码\",\"content\":{\"application/json\":{\"schema\":{\"$ref\":\"#/components/schemas/DeviceResponse\"}}}},\"400\":{\"description\":\"错误的请求\"}}}},\"/connect/revocat\":{\"post\":{\"tags\":[\"OAuth2\"],\"summary\":\"Token Revocation Endpoint\",\"description\":\"撤销令牌的Endpoint\",\"requestBody\":{\"content\":{\"application/x-www-form-urlencoded\":{\"schema\":{\"$ref\":\"#/components/schemas/RevocatRequest\"}}}},\"responses\":{\"200\":{\"description\":\"令牌已撤销\"},\"400\":{\"description\":\"错误的请求\"}}}},\"/connect/introspect\":{\"post\":{\"tags\":[\"OAuth2\"],\"summary\":\"Token Introspection Endpoint\",\"description\":\"校验令牌的Endpoint\",\"requestBody\":{\"content\":{\"application/x-www-form-urlencoded\":{\"schema\":{\"$ref\":\"#/components/schemas/IntrospectRequest\"}}}},\"responses\":{\"200\":{\"description\":\"返回令牌信息\",\"content\":{\"application/json\":{\"schema\":{\"$ref\":\"#/components/schemas/IntrospectResponse\"}}}},\"400\":{\"description\":\"错误的请求\"}}}}},\"components\":{\"schemas\":{\"ResponseType\":{\"type\":\"string\",\"enum\":[\"code\"]},\"GrantType\":{\"type\":\"string\",\"enum\":[\"authorization_code\",\"client_credentials\",\"password\",\"urn:ietf:params:oauth:grant-type:device_code\"]},\"TokenRequest\":{\"type\":\"object\",\"properties\":{\"grant_type\":{\"schema\":{\"$ref\":\"#/components/schemas/GrantType\"}},\"code\":{\"type\":\"string\"},\"redirect_uri\":{\"type\":\"string\"},\"client_id\":{\"type\":\"string\"},\"client_secret\":{\"type\":\"string\"},\"username\":{\"type\":\"string\"},\"password\":{\"type\":\"string\"},\"device_code\":{\"type\":\"string\"}}},\"TokenResponse\":{\"type\":\"object\",\"properties\":{\"access_token\":{\"type\":\"string\"},\"token_type\":{\"type\":\"string\"},\"expires_in\":{\"type\":\"integer\"},\"refresh_token\":{\"type\":\"string\"}}},\"DeviceRequest\":{\"type\":\"object\",\"properties\":{\"client_id\":{\"type\":\"string\"},\"scope\":{\"type\":\"string\"}}},\"RevocatRequest\":{\"type\":\"object\",\"properties\":{\"token\":{\"type\":\"string\"},\"token_type_hint\":{\"type\":\"string\"}}},\"IntrospectRequest\":{\"type\":\"object\",\"properties\":{\"token\":{\"type\":\"string\"}}},\"IntrospectResponse\":{\"type\":\"object\",\"properties\":{\"active\":{\"type\":\"boolean\"},\"scope\":{\"type\":\"string\"},\"client_id\":{\"type\":\"string\"},\"username\":{\"type\":\"string\"},\"token_type\":{\"type\":\"string\"},\"exp\":{\"type\":\"integer\"},\"iat\":{\"type\":\"integer\"},\"nbf\":{\"type\":\"integer\"},\"sub\":{\"type\":\"string\"},\"aud\":{\"type\":\"string\"},\"iss\":{\"type\":\"string\"},\"jti\":{\"type\":\"string\"}}},\"DeviceResponse\":{\"type\":\"object\",\"properties\":{\"device_code\":{\"type\":\"string\"},\"user_code\":{\"type\":\"string\"},\"verification_uri\":{\"type\":\"string\"},\"verification_uri_complete\":{\"type\":\"string\"},\"expires_in\":{\"type\":\"integer\"},\"interval\":{\"type\":\"integer\"}}}},\"securitySchemes\":{\"OAuth2\":{\"type\":\"oauth2\",\"flows\":{\"authorizationCode\":{\"authorizationUrl\":\"/connect/authorize\",\"tokenUrl\":\"/connect/token\",\"scopes\":{\"read\":\"Grants read access\",\"write\":\"Grants write access\",\"admin\":\"Grants access to admin operations\"}},\"clientCredentials\":{\"tokenUrl\":\"/connect/token\",\"scopes\":{\"read\":\"Grants read access\",\"write\":\"Grants write access\",\"admin\":\"Grants access to admin operations\"}},\"password\":{\"tokenUrl\":\"/connect/token\",\"scopes\":{\"read\":\"Grants read access\",\"write\":\"Grants write access\",\"admin\":\"Grants access to admin operations\"}},\"deviceCode\":{\"tokenUrl\":\"/connect/token\",\"deviceAuthorizationUrl\":\"/device\",\"scopes\":{\"read\":\"Grants read access\",\"write\":\"Grants write access\",\"admin\":\"Grants access to admin operations\"}}}}}},\"security\":[{\"OAuth2\":[]}]}";
    }
}