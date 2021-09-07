using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using MarcaDeTiempo.Functions.Entities;
using MarcaDeTiempo.Common.Responses;

namespace MarcaDeTiempo.Functions.Functions
{
    public static class ConsolidadoApi
    {
        [FunctionName(nameof(ObtenerConsolidadoSegunFecha))]
        public static async Task<IActionResult> ObtenerConsolidadoSegunFecha(
            [HttpTrigger(AuthorizationLevel.Anonymous, methods: "post", Route = "consolidadosSegunFecha")] HttpRequest req,
            [Table("consolidado", Connection = "AzureWebJobsStorage")] CloudTable consolidadoTable,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            DateTime fechaHoy = JsonConvert.DeserializeObject<DateTime>(requestBody);
            log.LogInformation($"Se va a obtener los consolidados del dia: {fechaHoy}");

            // Validar registro existente segun fecha
            string filter = TableQuery.GenerateFilterConditionForDate("dateTime", QueryComparisons.Equal, fechaHoy);
            log.LogInformation($"{filter}");
            TableQuery<ConsolidadoEntity> query = new TableQuery<ConsolidadoEntity>().Where(filter);

            TableQuerySegment<ConsolidadoEntity> findResult = await consolidadoTable.ExecuteQuerySegmentedAsync(query, null);

            string message = "Se obtuvo el listado de los consolidados";
            log.LogInformation(message);

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = findResult.Results
            });
        }

    }
}
