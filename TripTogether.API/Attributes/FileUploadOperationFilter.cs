using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TripTogether.API.Attributes;

public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var fileParameters = context.ApiDescription.ParameterDescriptions
            .Where(p => p.ModelMetadata?.ModelType == typeof(IFormFile))
            .ToList();

        if (!fileParameters.Any())
            return;

        // Clear existing parameters that are files
        foreach (var fileParam in fileParameters)
        {
            var paramToRemove = operation.Parameters
                .FirstOrDefault(p => p.Name == fileParam.Name);
            
            if (paramToRemove != null)
            {
                operation.Parameters.Remove(paramToRemove);
            }
        }

        // Create multipart/form-data request body
        var properties = new Dictionary<string, OpenApiSchema>();
        var required = new HashSet<string>();

        foreach (var fileParam in fileParameters)
        {
            properties[fileParam.Name] = new OpenApiSchema
            {
                Type = "string",
                Format = "binary"
            };
            
            if (fileParam.IsRequired)
            {
                required.Add(fileParam.Name);
            }
        }

        operation.RequestBody = new OpenApiRequestBody
        {
            Required = required.Any(),
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = properties,
                        Required = required
                    }
                }
            }
        };
    }
}
