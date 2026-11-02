using Cantina.Api.Workers;
using Cantina.Application.Services;
using Cantina.Contracts.Dtos;
using Cantina.Domain.Exceptions;
using Cantina.Infrastructure;
using Cantina.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Cantina.Api;

public static class CantinaApiHost
{
    public static WebApplication CreateWebApplication(string[] args, string? explicitUrl = null)
    {
        var webRootCandidate = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        var options = Directory.Exists(webRootCandidate)
            ? new WebApplicationOptions
            {
                Args = args,
                WebRootPath = webRootCandidate
            }
            : new WebApplicationOptions { Args = args };

        var builder = WebApplication.CreateBuilder(options);

        // Suporte automático à variável PORT de provedores gratuitos (Render, Railway, Fly.io, Koyeb)
        if (!string.IsNullOrWhiteSpace(explicitUrl))
        {
            builder.WebHost.UseUrls(explicitUrl);
        }
        else
        {
            var cloudPort = Environment.GetEnvironmentVariable("PORT");
            if (!string.IsNullOrWhiteSpace(cloudPort) && int.TryParse(cloudPort, out var portNumber))
            {
                builder.WebHost.UseUrls($"http://0.0.0.0:{portNumber}");
            }
        }

        builder.Services.AddCantinaInfrastructure(builder.Configuration);
        builder.Services.AddHostedService<QueimaEstoqueBackgroundService>();
        builder.Services.AddHealthChecks();
        builder.Services.AddProblemDetails();

        builder.Services.AddCors(cors =>
        {
            cors.AddDefaultPolicy(policy =>
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod());
        });

        var app = builder.Build();

        app.UseExceptionHandler(errApp =>
        {
            errApp.Run(async context =>
            {
                var feature = context.Features.Get<IExceptionHandlerFeature>();
                var ex = feature?.Error;

                if (ex is DomainException domainEx)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Validação de regra de negócio",
                        Detail = domainEx.Message
                    });
                    return;
                }

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Erro interno no servidor",
                    Detail = "Ocorreu um erro inesperado ao processar a solicitação."
                });
            });
        });

        app.UseCors();
        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.MapHealthChecks("/health");

        MapPublicStorefrontEndpoints(app);
        MapBackofficeManagementEndpoints(app);

        return app;
    }

    public static async Task InitializeDatabaseAsync(WebApplication app, CancellationToken ct = default)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CantinaDbContext>();
        await CantinaDatabaseSeeder.InitializeAndSeedAsync(db, ct);
    }

    private static void MapPublicStorefrontEndpoints(WebApplication app)
    {
        var pub = app.MapGroup("/api/v1");

        pub.MapGet("/cardapio", async (ICatalogoService catalogo, CancellationToken ct) =>
            Results.Ok(await catalogo.ObterCardapioPublicoAsync(ct)));

        pub.MapPost("/pedidos", async (CriarPedidoWebRequest req, IPedidoService pedidos, CancellationToken ct) =>
        {
            var criado = await pedidos.CriarPedidoWebAsync(req, ct);
            return Results.Created($"/api/v1/pedidos/{criado.CodigoRetirada}", criado);
        });

        pub.MapGet("/pedidos/{codigo}", async (string codigo, IPedidoService pedidos, CancellationToken ct) =>
        {
            var encontrado = await pedidos.ObterPorCodigoAsync(codigo, ct);
            return encontrado is not null
                ? Results.Ok(encontrado)
                : Results.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Pedido não localizado",
                    Detail = $"Nenhum pedido encontrado com o código '{codigo}'."
                });
        });
    }

    private static void MapBackofficeManagementEndpoints(WebApplication app)
    {
        var admin = app.MapGroup("/api/v1/gestao")
            .AddEndpointFilter(async (context, next) =>
            {
                var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                var expectedKey = config["Security:AdminApiKey"];

                if (!string.IsNullOrWhiteSpace(expectedKey))
                {
                    var provided = context.HttpContext.Request.Headers["X-Api-Key"].ToString();
                    if (!string.Equals(expectedKey, provided, StringComparison.Ordinal))
                    {
                        return Results.Json(
                            new ProblemDetails
                            {
                                Status = StatusCodes.Status401Unauthorized,
                                Title = "Acesso restrito ao Backoffice",
                                Detail = "Informe um cabeçalho X-Api-Key válido para gerenciar a cantina."
                            },
                            statusCode: StatusCodes.Status401Unauthorized);
                    }
                }

                return await next(context);
            });

        // Dashboard
        admin.MapGet("/dashboard", async (IGestaoCantinaService gestao, CancellationToken ct) =>
            Results.Ok(await gestao.ObterDashboardAsync(ct)));

        // Categorias e Produtos
        admin.MapGet("/categorias", async (ICatalogoService catalogo, CancellationToken ct) =>
            Results.Ok(await catalogo.ListarCategoriasAsync(ct)));

        admin.MapGet("/produtos", async (ICatalogoService catalogo, CancellationToken ct) =>
            Results.Ok(await catalogo.ListarProdutosAsync(apenasAtivos: false, ct)));

        admin.MapPost("/produtos", async (SalvarProdutoRequest req, ICatalogoService catalogo, CancellationToken ct) =>
        {
            var criado = await catalogo.CriarProdutoAsync(req, ct);
            return Results.Created($"/api/v1/gestao/produtos/{criado.Id}", criado);
        });

        admin.MapPut("/produtos/{id:int}", async (int id, SalvarProdutoRequest req, ICatalogoService catalogo, CancellationToken ct) =>
            Results.Ok(await catalogo.AtualizarProdutoAsync(id, req, ct)));

        admin.MapDelete("/produtos/{id:int}", async (int id, ICatalogoService catalogo, CancellationToken ct) =>
        {
            await catalogo.RemoverProdutoAsync(id, ct);
            return Results.NoContent();
        });

        admin.MapPatch("/produtos/{id:int}/estoque", async (int id, AjustarEstoqueRequest req, ICatalogoService catalogo, CancellationToken ct) =>
            Results.Ok(await catalogo.AjustarEstoqueAsync(id, req, ct)));

        admin.MapPost("/produtos/{id:int}/desconto", async (int id, AplicarDescontoRequest req, ICatalogoService catalogo, CancellationToken ct) =>
            Results.Ok(await catalogo.AplicarDescontoProdutoAsync(id, req, ct)));

        admin.MapDelete("/produtos/{id:int}/desconto", async (int id, ICatalogoService catalogo, CancellationToken ct) =>
            Results.Ok(await catalogo.ZerarDescontoProdutoAsync(id, ct)));

        admin.MapPost("/produtos/descontos-lote", async (AplicarDescontoRequest req, ICatalogoService catalogo, CancellationToken ct) =>
            Results.Ok(await catalogo.AplicarDescontoLoteAsync(req, ct)));

        admin.MapDelete("/produtos/descontos-lote", async (ICatalogoService catalogo, CancellationToken ct) =>
        {
            await catalogo.ZerarTodosDescontosAsync(ct);
            return Results.NoContent();
        });

        // Queima de Estoque 20:45 + Disparo WhatsApp
        admin.MapPost("/queima-estoque", async (ExecutarQueimaEstoqueRequest req, ICatalogoService catalogo, CancellationToken ct) =>
            Results.Ok(await catalogo.ExecutarQueimaEstoqueAsync(req, ct)));

        // Pedidos Online e PDV Balcão
        admin.MapGet("/pedidos", async (bool? apenasFila, IPedidoService pedidos, CancellationToken ct) =>
            Results.Ok(await pedidos.ListarPedidosAsync(apenasFila ?? false, ct)));

        admin.MapPost("/pedidos/venda-balcao", async (RegistrarVendaBalcaoRequest req, IPedidoService pedidos, CancellationToken ct) =>
            Results.Ok(await pedidos.RegistrarVendaBalcaoAsync(req, ct)));

        admin.MapPatch("/pedidos/{id:int}/status", async (int id, AtualizarStatusPedidoRequest req, IPedidoService pedidos, CancellationToken ct) =>
            Results.Ok(await pedidos.AtualizarStatusAsync(id, req, ct)));

        // Alunos, Grupos WhatsApp, Logs e Fechamento Diário
        admin.MapGet("/alunos", async (IGestaoCantinaService gestao, CancellationToken ct) =>
            Results.Ok(await gestao.ListarAlunosAsync(ct)));

        admin.MapPost("/alunos", async (SalvarAlunoRequest req, IGestaoCantinaService gestao, CancellationToken ct) =>
            Results.Ok(await gestao.CriarAlunoAsync(req, ct)));

        admin.MapDelete("/alunos/{id:int}", async (int id, IGestaoCantinaService gestao, CancellationToken ct) =>
        {
            await gestao.RemoverAlunoAsync(id, ct);
            return Results.NoContent();
        });

        admin.MapGet("/grupos-whatsapp", async (IGestaoCantinaService gestao, CancellationToken ct) =>
            Results.Ok(await gestao.ListarGruposWhatsAppAsync(ct)));

        admin.MapPost("/grupos-whatsapp", async (SalvarGrupoWhatsAppRequest req, IGestaoCantinaService gestao, CancellationToken ct) =>
            Results.Ok(await gestao.SalvarGrupoWhatsAppAsync(null, req, ct)));

        admin.MapPut("/grupos-whatsapp/{id:int}", async (int id, SalvarGrupoWhatsAppRequest req, IGestaoCantinaService gestao, CancellationToken ct) =>
            Results.Ok(await gestao.SalvarGrupoWhatsAppAsync(id, req, ct)));

        admin.MapGet("/notificacoes-whatsapp", async (IGestaoCantinaService gestao, CancellationToken ct) =>
            Results.Ok(await gestao.ListarNotificacoesWhatsAppAsync(ct)));

        admin.MapGet("/movimentacoes-estoque", async (IGestaoCantinaService gestao, CancellationToken ct) =>
            Results.Ok(await gestao.ListarMovimentacoesEstoqueAsync(ct)));

        admin.MapGet("/fechamentos", async (IGestaoCantinaService gestao, CancellationToken ct) =>
            Results.Ok(await gestao.ListarFechamentosDiariosAsync(ct)));

        admin.MapPost("/fechamentos", async (GerarFechamentoRequest req, IGestaoCantinaService gestao, CancellationToken ct) =>
            Results.Ok(await gestao.GerarFechamentoDiarioAsync(req, ct)));
    }
}
