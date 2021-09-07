using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MarcaDeTiempo.Common.Models;
using MarcaDeTiempo.Common.Responses;
using MarcaDeTiempo.Functions.Entities;

namespace MarcaDeTiempo.Functions.Functions
{
    public static class ScheduleConsolidado
    {
        [FunctionName(nameof(SheduleConsolidado))]
        public static async Task SheduleConsolidado(
            //SE EJECUTA CADA 5 MINUTOS
            [TimerTrigger("0 */5 * * * *")] TimerInfo miCronometro,
            [Table("registro", Connection = "AzureWebJobsStorage")] CloudTable registroTable,
            [Table("consolidado", Connection = "AzureWebJobsStorage")] CloudTable consolidadoTable,
            ILogger log)
        {
            log.LogInformation($"Se va a ejecutar el consolidado : {DateTime.Now}");

            TableQuerySegment<RegistroEntity> registros = await obtenerRegistrosNoConsolidados(registroTable);

            foreach (RegistroEntity item in registros) {
            }
            List<RegistroEntity> listaRegistros = registros.Results;
            log.LogInformation($"Se obtuvieron {listaRegistros.Count} registros");
            List<int> listaIdEmpleados = obtenerListaIdEmpleados(listaRegistros);
            log.LogInformation($"Se obtuvieron {listaIdEmpleados.Count} idEmpleados");

            foreach (int idEmpleado in listaIdEmpleados)
            {
                log.LogInformation($"idEmpleado: {idEmpleado}");

                List<RegistroEntity> registrosFiltradosSegunId = listaRegistros.FindAll(t => t.idEmpleado == idEmpleado);

                log.LogInformation($"{calcularTiempoTrabajado(registrosFiltradosSegunId[0].dateTime, registrosFiltradosSegunId[1].dateTime)}");
                
                ConsolidadoEntity consolidadoEntity = new ConsolidadoEntity
                {
                    dateTime = DateTime.UtcNow,
                    idEmpleado = idEmpleado,
                    tiempoTrabajado = calcularTiempoTrabajado(registrosFiltradosSegunId[0].dateTime, registrosFiltradosSegunId[1].dateTime),
                    //propiedades de la entidad
                    ETag = "*",
                    PartitionKey = "CONSOLIDADO",
                    RowKey = Guid.NewGuid().ToString(),
                };

                //Se registra el consolidado
                TableOperation addOperationConsolidado = TableOperation.Insert(consolidadoEntity);
                await consolidadoTable.ExecuteAsync(addOperationConsolidado);
                log.LogInformation($"Se realizo el consolidado para el idEmpleado {idEmpleado}");

                //Actualiza consolidado del registro de entrada en TRUE
                registrosFiltradosSegunId[0].consolidado = true;
                TableOperation addOperationEntrada = TableOperation.Replace(registrosFiltradosSegunId[0]);
                await registroTable.ExecuteAsync(addOperationEntrada);
                log.LogInformation($"Se actualizo el registro para la entrada({registrosFiltradosSegunId[0].tipo}) del idEmpleado {idEmpleado}");

                //Actualiza consolidado del registro de salida en TRUE
                registrosFiltradosSegunId[1].consolidado = true;
                TableOperation addOperationSalida = TableOperation.Replace(registrosFiltradosSegunId[1]);
                await registroTable.ExecuteAsync(addOperationSalida);
                log.LogInformation($"Se actualizo el registro para la salida({registrosFiltradosSegunId[1].tipo}) del idEmpleado {idEmpleado}");
            
           }

        }


        private static async Task<TableQuerySegment<RegistroEntity>> obtenerRegistrosNoConsolidados(CloudTable tables) {
            string filter = TableQuery.GenerateFilterConditionForBool("consolidado", QueryComparisons.Equal, false);
            TableQuery<RegistroEntity> query = new TableQuery<RegistroEntity>().Where(filter);

            return await tables.ExecuteQuerySegmentedAsync(query, null);
        }

        private static List<int> obtenerListaIdEmpleados(List<RegistroEntity> registros)
        {
            List<int> listaIdEmpleados = new List<int>();
            foreach (RegistroEntity registro in registros)
            {
                if (listaIdEmpleados.IndexOf(registro.idEmpleado) == -1)
                {
                    listaIdEmpleados.Add(registro.idEmpleado);
                }
            }
            return listaIdEmpleados;
        }

        private static string calcularTiempoTrabajado(DateTime tiempoEntrada, DateTime tiempoSalida) {
            TimeSpan tiempo = tiempoSalida - tiempoEntrada;

            int horas = tiempo.Hours < 0 ? tiempo.Hours * -1 : tiempo.Hours;
            int minutos = tiempo.Minutes < 0 ? tiempo.Minutes * -1 : tiempo.Minutes;

            return horas.ToString()+":"+ minutos.ToString();
        }

        private static async Task<TableQuerySegment<RegistroEntity>> validarExistenciaRegistroSegunIdEmpleado(
            CloudTable registroTable, int idEmpleado)
        {
            string filter = TableQuery.GenerateFilterConditionForInt("idEmpleado", QueryComparisons.Equal, idEmpleado);
            TableQuery<RegistroEntity> query = new TableQuery<RegistroEntity>().Where(filter);

            return await registroTable.ExecuteQuerySegmentedAsync(query, null);

        }

    }
}
