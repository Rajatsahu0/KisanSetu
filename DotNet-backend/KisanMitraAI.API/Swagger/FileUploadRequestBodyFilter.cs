using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace KisanMitraAI.API.Swagger;

public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasFormFile = context.ApiDescription.ParameterDescriptions
            .Any(p => p.ModelMetadata?.ModelType == typeof(IFormFile) || 
                     p.ModelMetadata?.ModelType == typeof(List<IFormFile>) ||
                     p.ModelMetadata?.ModelType == typeof(IEnumerable<IFormFile>));

        if (!hasFormFile) return;

        operation.Parameters?.Clear();

        var uploadFileMediaType = new OpenApiMediaType
        {
            Schema = new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>(),
                Required = new HashSet<string>()
            }
        };

        foreach (var param in context.ApiDescription.ParameterDescriptions)
        {
            var paramType = param.ModelMetadata?.ModelType;
            
            if (paramType == typeof(IFormFile))
            {
                uploadFileMediaType.Schema.Properties[param.Name] = new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary"
                };
                if (param.IsRequired)
                {
                    uploadFileMediaType.Schema.Required.Add(param.Name);
                }
            }
            else if (paramType == typeof(List<IFormFile>) || paramType == typeof(IEnumerable<IFormFile>))
            {
                uploadFileMediaType.Schema.Properties[param.Name] = new OpenApiSchema
                {
                    Type = "array",
                    Items = new OpenApiSchema { Type = "string", Format = "binary" }
                };
                if (param.IsRequired)
                {
                    uploadFileMediaType.Schema.Required.Add(param.Name);
                }
            }
            else if (param.Source?.Id == "Form")
            {
                uploadFileMediaType.Schema.Properties[param.Name] = new OpenApiSchema { Type = "string" };
                if (param.IsRequired)
                {
                    uploadFileMediaType.Schema.Required.Add(param.Name);
                }
            }
        }

        operation.RequestBody = new OpenApiRequestBody
        {
            Content = { ["multipart/form-data"] = uploadFileMediaType }
        };
    }
}
