using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Threading.Tasks;
using System;

public class Pedido
{
    public int Id { get; set; }
    public decimal ValorTotal { get; set; }
    public decimal CustoProduto { get; set; }
    public DateTime DataPedido { get; set; }

}

public static class OrquestradorAprovadorDePedido
{
    [FunctionName("InicioAprovadorDePedido")]
    public static async Task<IActionResult> InicioAprovadorDePedido(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "Aprovador-Pedido")] HttpRequest req,
        [DurableClient] IDurableOrchestrationClient starter,
        ILogger log)
    {

        dynamic dados = JsonConvert.DeserializeObject<Pedido>(await new StreamReader(req.Body).ReadToEndAsync());

        if (dados == null)
        {
            return new BadRequestObjectResult($"Falha ao validar o pedido, o corpo da Requisição não pode ser null");
        }

        string resultado = await starter.StartNewAsync("OrquestradorValidadorAprovacao", dados);

        return new OkObjectResult($"Pedido {dados.Id} enviado para aprovação \n\n IdAnalise: {resultado}");
    }

    [FunctionName("OrquestradorValidadorAprovacao")]
    public static async Task<string> OrquestradorValidadorAprovacao(
        [OrchestrationTrigger] IDurableOrchestrationContext contexto)
    {
        var pedido = contexto.GetInput<dynamic>();

        var result = await contexto.CallActivityAsync<string>("Validador", pedido);

        Console.WriteLine(result);

        return result;
    }



    [FunctionName("Validador")]
    public async static Task<string> Validador([ActivityTrigger] Pedido pedido, ILogger log)
    {
        var margem = (100 - ((pedido.CustoProduto / pedido.ValorTotal) * 100));

        if (pedido.DataPedido > DateTime.Now.AddDays(-7) || margem > 30)
            return "Pedido Aprovado";

        return "Pedido Reprovado";
    }
}