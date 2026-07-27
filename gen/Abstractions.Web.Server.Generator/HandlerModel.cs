namespace Norse.Abstractions.Web.Server.Generator;

sealed record HandlerModel(
	string HandlerTypeName,      // global::-qualified
	string RequestTypeName,      // global::-qualified
	string ResponseTypeName,     // global::-qualified payload
	string[] ValidatorTypeNames); // global::-qualified, may be empty
