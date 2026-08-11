# Norse.Abstractions.Components

Norse component abstractions: Razor component base types shared across Blazor WASM, Blazor Server, and MAUI consumers — `AsyncComponentBase`, and the outcome-consuming form seam hoisted from Heimdall 2026-08-09 (`OutcomeFormComponentBase` with the stamped-request `EditContextFor` mechanic, plus the `ServerValidation` bridge from `Failed(Problem)` to form UX). No ASP.NET Core server, EF Core, or server-side infrastructure references — this assembly must compile into a client bundle.

Form validation is owned here: `FormValidator` attaches Blazilla's FluentValidation pass and stamps the marker `OutcomeFormComponentBase` requires before dispatch. Taking Blazilla pulls **FluentValidation 12.1.1** into every consumer of this package with no opt-out, and that is the intent — write the request, response, validator, and handler once, and the same validator class runs client-side in the form and server-side in the mediator pipeline. The platform does not own a validation framework and does not own its Blazor integration.

Part of the [Norse Architecture](https://github.com/NorseArchitecture) platform.
