using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MinimalAPI.Data;
using MinimalAPI.Models;
using MiniValidation;
using NetDevPack.Identity;
using NetDevPack.Identity.Jwt;

namespace MinimalAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddAuthorization();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddDbContext<MinimalContextDb>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
            );

            builder.Services.AddIdentityEntityFrameworkContextConfiguration(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly("MinimalAPI")));

            builder.Services.AddIdentityConfiguration();
            builder.Services.AddJwtConfiguration(builder.Configuration, "AppSettings");

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthConfiguration();
            app.UseHttpsRedirection();

            #region Get
            app.MapGet("/fornecedor", async (
                MinimalContextDb context) =>
                await context.Fornecedores.ToListAsync())
                .WithName("GetFornecedor")
                .WithTags("Fornecedor");
            #endregion

            #region GetById
            app.MapGet("/fornecedor/{id}", async (
                Guid id,
                MinimalContextDb context) =>

                await context.Fornecedores.FindAsync(id)
                    is Fornecedor fornecedor
                    ? Results.Ok(fornecedor)
                    : Results.NotFound())
                .Produces<Fornecedor>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .WithName("GetFornecedorPorId")
                .WithTags("Fornecedor");
            #endregion

            #region
            app.MapPost("/fornecedor", async (
                MinimalContextDb context,
                Fornecedor fornecedor) =>
                {
                    if (!MiniValidator.TryValidate(fornecedor, out var errors))
                        return Results.ValidationProblem(errors);

                    context.Fornecedores.Add(fornecedor);
                    var result = await context.SaveChangesAsync();

                    return result > 0
                        //? Results.Created($"/fornecedor/{fornecedor.Id}", fornecedor)
                        ? Results.CreatedAtRoute("GetFornecedorPorId", new { id = fornecedor.Id }, fornecedor)
                        : Results.BadRequest("Houve um problema ao salvar o registro");
                }).ProducesValidationProblem()
                .Produces<Fornecedor>(StatusCodes.Status201Created)
                .Produces(StatusCodes.Status404NotFound)
                .WithName("PostFornecedor")
                .WithTags("Fornecedor");
            #endregion

            #region Put
            app.MapPut("/fornecedor/{id}", async (
                Guid id,
                MinimalContextDb context,
                Fornecedor fornecedor) =>
                {
                    var fornecedorBanco = await context.Fornecedores.AsNoTracking<Fornecedor>().FirstOrDefaultAsync(f => f.Id == id);
                    if (fornecedorBanco == null) return Results.NotFound();

                    if (!MiniValidator.TryValidate(fornecedor, out var errors))
                        return Results.ValidationProblem(errors);

                    context.Fornecedores.Update(fornecedor);
                    var result = await context.SaveChangesAsync();

                    return result > 0
                        ? Results.NoContent()
                        : Results.BadRequest("Houve um problema ao salvar o registro");

                }).ProducesValidationProblem()
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status400BadRequest)
                .WithName("PutFornecedor")
                .WithTags("Fornecedor");
            #endregion

            #region Delete
            app.MapDelete("/fornecedor/{id}", async (
                Guid id,
                MinimalContextDb context) =>
            {
                var fornecedor = await context.Fornecedores.FindAsync(id);
                if (fornecedor == null) return Results.NotFound();

                context.Fornecedores.Remove(fornecedor);
                var result = await context.SaveChangesAsync();

                return result > 0
                    ? Results.NoContent()
                    : Results.BadRequest("Houve um problema ao deletar o registro");
            }).Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("DeleteFornecedor")
            .WithTags("Fornecedor");
            #endregion

            app.Run();
        }
    }
}