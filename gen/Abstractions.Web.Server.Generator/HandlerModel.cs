namespace Norse.Abstractions.Web.Server.Generator;

sealed record HandlerModel(
	string HandlerTypeName, // global::-qualified
	string RequestTypeName, // global::-qualified
	string ResponseTypeName, // global::-qualified payload
	string[] ValidatorTypeNames, // global::-qualified, may be empty — validators targeting RequestTypeName directly
	string? WrapperWireTypeName, // global::-qualified TRequest of CommandRequest<TRequest,TResponse>, null when RequestTypeName is not a wrapper
	string[] WireValidatorTypeNames); // global::-qualified, may be empty — validators targeting WrapperWireTypeName, wrapper commands only
