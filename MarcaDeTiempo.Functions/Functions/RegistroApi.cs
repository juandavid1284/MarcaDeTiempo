using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using MarcaDeTiempo.Common.Models;
using MarcaDeTiempo.Common.Responses;
using MarcaDeTiempo.Functions.Entities;

namespace MarcaDeTiempo.Functions.Functions
{
    public static class RegistroApi
    {
        [FunctionName(nameof(CrearRegistro))]
        public static async Task<IActionResult> CrearRegistro(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "registro")] HttpRequest req,
            [Table("registro", Connection = "AzureWebJobsStorage")] CloudTable registroTable,
            ILogger log)
        {
            log.LogInformation("Se va a crear un registro");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Registro registro = JsonConvert.DeserializeObject<Registro>(requestBody);

            if (registro?.idEmpleado == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "El request debe tener el Id de empleado"
                });
            }

            if (registro?.tipo == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "El request debe tener la tipo del registro"
                });
            }

            RegistroEntity registroEntity = new RegistroEntity
            {
                dateTime = DateTime.UtcNow,
                tipo = registro.tipo,
                idEmpleado = registro.idEmpleado,
                consolidado = false,
                //propiedades de la entidad
                ETag = "*",
                PartitionKey = "REGISTRO",
                RowKey = Guid.NewGuid().ToString(),
            };

            TableOperation addOperation = TableOperation.Insert(registroEntity);
            await registroTable.ExecuteAsync(addOperation);

            string message = "El registro fue almacenado en la tabla";
            log.LogInformation(message);

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = registroEntity
            });
        }

        [FunctionName(nameof(ActualizarRegistroSegunId))]
        public static async Task<IActionResult> ActualizarRegistroSegunId(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "registro/{id}")] HttpRequest req,
            [Table("registro", Connection = "AzureWebJobsStorage")] CloudTable registroTable,
            string id,
            ILogger log)
        {
            log.LogInformation($"Se va a actualizar un registro segun el registro {id}");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Registro registro = JsonConvert.DeserializeObject<Registro>(requestBody);

            // Validar registro existente segun rowkey
            TableOperation findOperation = TableOperation.Retrieve<RegistroEntity>("REGISTRO", id);
            TableResult findResult = await registroTable.ExecuteAsync(findOperation);

            if (findResult.Result == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "Registro no fue encontrado"
                });
            }

            // Actualizar registro
            RegistroEntity registroEntity = (RegistroEntity)findResult.Result;
            registroEntity.consolidado = registro.consolidado;
            if (registro?.tipo != null) {
                registroEntity.tipo = registro.tipo;
            }

            TableOperation addOperation = TableOperation.Replace(registroEntity);
            await registroTable.ExecuteAsync(addOperation);

            string message = $"Registro: {id}, fue actualizado.";
            log.LogInformation(message);

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = registroEntity
            });
        }

        [FunctionName(nameof(ListarRegistros))]
        public static async Task<IActionResult> ListarRegistros(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "listarRegistros")] HttpRequest req,
            [Table("registro", Connection = "AzureWebJobsStorage")] CloudTable registroTable,
            ILogger log)
        {
            log.LogInformation("Obtener el listado de todos los registros");

            TableQuery<RegistroEntity> query = new TableQuery<RegistroEntity>();
            TableQuerySegment<RegistroEntity> listado = await registroTable.ExecuteQuerySegmentedAsync(query, null);

            string message = "Se obtuvo el listado de los registros";
            log.LogInformation(message);

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = listado
            });
        }
        
        [FunctionName(nameof(ObtenerRegistroSegunId))]
        public static async Task<IActionResult> ObtenerRegistroSegunId(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "registroSegunId/{id}")] HttpRequest req,
            [Table("registro", Connection = "AzureWebJobsStorage")] CloudTable registroTable,
            string id,
            ILogger log)
        {
            log.LogInformation($"Se va a obtener el registro segun el id: {id}");
            // Validar registro existente segun rowkey
            TableOperation findOperation = TableOperation.Retrieve<RegistroEntity>("REGISTRO", id);
            TableResult findResult = await registroTable.ExecuteAsync(findOperation);

            if (findResult.Result == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "Registro no fue encontrado"
                });
            }

            RegistroEntity registroEntity = (RegistroEntity)findResult.Result;

            string message = $"El registro {id} fue obtenido.";
            log.LogInformation(message);

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = registroEntity
            });
        }

        [FunctionName(nameof(EliminarRegistroSegunId))]
        public static async Task<IActionResult> EliminarRegistroSegunId(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "registroSegunId/{id}")] HttpRequest req,
            [Table("registro", Connection = "AzureWebJobsStorage")] CloudTable registroTable,
            string id,
            ILogger log)
        {
            log.LogInformation($"Se va a eliminar el registro con id: {id}");
            // Validar registro existente segun rowkey
            TableOperation findOperation = TableOperation.Retrieve<RegistroEntity>("REGISTRO", id);
            TableResult findResult = await registroTable.ExecuteAsync(findOperation);

            if (findResult.Result == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "El registro no fue encontrado"
                });
            }

            RegistroEntity registroEntity = (RegistroEntity)findResult.Result;

            await registroTable.ExecuteAsync(TableOperation.Delete(registroEntity));
            string message = $"El registro {id}, fue eliminado";
            log.LogInformation(message);

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = registroEntity
            });
        }

    }
}
